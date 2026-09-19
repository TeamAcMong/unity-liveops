using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Màn <c>recurring</c> ở tầng Lên lịch (mục 7.4, [SD1 §4]): pane trái là danh sách luật, pane phải là form đọc được như
    /// một câu. Đây là chỗ sửa nguy hiểm nhất của hub — đổi tiền tố, neo hay chu kỳ khi một lần lặp đang chạy là người chơi
    /// mất tiến độ — nên mọi thay đổi như thế đi qua hai bước: nháp tại ô (<see cref="RecurringPrefixDraft"/>) rồi mới tới
    /// hộp xác nhận do <see cref="LiveOpsConfirmationPolicy"/> quyết cấp.
    /// <para>
    /// Section không tự nghĩ chữ, không tự quyết mức hỏi và không tự sinh lần lặp: chữ ở catalog, mức hỏi ở policy, lần lặp
    /// ở <see cref="RecurringLiveEventCalendar"/> của runtime — ba chỗ đó là nguồn duy nhất cho cả hub.
    /// </para>
    /// </summary>
    internal sealed class RecurringRulesSection : IHubSection, IHubHostAware, IHubSectionActions, IHubSectionViewState,
        IHubSectionNavigation, IHubSectionFindings
    {
        internal const string BodyElementName = "recurring-body";
        internal const string ListElementName = "recurring-list";
        internal const string FormHostElementName = "recurring-form-host";
        internal const string EmptyElementName = "recurring-empty";
        internal const string EmptyTitleElementName = "recurring-empty-title";
        internal const string EmptyBodyElementName = "recurring-empty-body";
        internal const string EmptyActionElementName = "recurring-empty-action";
        internal const string AddPopoverElementName = "recurring-add";
        internal const string AddTypeElementName = "recurring-add-type";
        internal const string AddPresetElementName = "recurring-add-preset";
        internal const string AddConfirmElementName = "recurring-add-confirm";
        internal const string AddCancelElementName = "recurring-add-cancel";
        internal const string AddRuleButtonElementName = "recurring-add-rule";

        /// <summary>Chờ 250ms sau lần gõ cuối rồi mới tính lại bảng đợt ([SD1 §4.1]) — gõ 168 không tính lại 3 lần.</summary>
        private const long OccurrenceDebounceMilliseconds = 250;

        private static readonly IReadOnlyList<string> ElementNames = Array.AsReadOnly(new[]
        {
            BodyElementName, ListElementName, FormHostElementName, EmptyElementName, EmptyActionElementName, AddPopoverElementName,
            RecurringRuleForm.SentenceElementName, RecurringRuleForm.PrefixFieldElementName, RecurringRuleForm.AnchorFieldElementName,
            RecurringRuleForm.PeriodFieldElementName, RecurringRuleForm.ActiveFieldElementName, RecurringRuleForm.CycleBarElementName,
            RecurringNextOccurrencesTable.ElementName,
        });

        private readonly RecurringRuleList _list = new RecurringRuleList { name = ListElementName };
        private readonly RecurringRuleForm _form = new RecurringRuleForm();

        private IHubHost _host;
        private VisualElement _root;
        private VisualElement _empty;
        private Label _emptyTitle;
        private Label _emptyBody;
        private Button _emptyAction;
        private VisualElement _addPopover;
        private DropdownField _addTypeField;
        private DropdownField _addPresetField;
        private LiveOpsButtonSlot _addRuleSlot;
        private IVisualElementScheduledItem _occurrenceDebounce;

        private RecurringPrefixDraft _draft = RecurringPrefixDraft.None;
        private string _selectedEventType = string.Empty;
        private int _occurrenceCount = RecurringRuleModel.DefaultOccurrenceCount;
        private bool _jsonFoldoutOpen;

        public RecurringRulesSection(LiveOpsHubServices services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            // Nối MỘT LẦN ở đây, không trong CreateView: khung gọi CreateView mỗi lần mở lại màn, mà _list/_form sống suốt
            // đời section — nối lại mỗi lượt thì vào-ra-vào ba lần là "Thêm 5" cộng 15 và Copy JSON bắn ba toast.
            _list.SelectionChanged += OnListSelectionChanged;
            _form.ValueChangeRequested += RequestFieldChange;
            _form.PresetSelected += OnPresetSelected;
            _form.DraftCancelRequested += OnDraftCancelRequested;
            _form.DraftWriteRequested += OnDraftWriteRequested;
            _form.RevertPrefixRequested += OnRevertPrefixRequested;
            _form.DecideInValidationRequested += OnDecideInValidationRequested;
            _form.AddMoreOccurrencesRequested += OnAddMoreOccurrencesRequested;
            _form.CopyJsonRequested += OnCopyJsonRequested;
            _form.ApplyJsonRequested += OnApplyJsonRequested;
            _form.DeleteRequested += OnDeleteRequested;
            _form.JsonFoldout.RegisterValueChangedCallback(changeEvent => _jsonFoldoutOpen = changeEvent.newValue);
            // Foldout JSON đọc lại bằng ĐÚNG adapter của phiên (V-16): kịch bản chụp và test thay adapter thì ô JSON phải
            // thấy cùng một kết quả với cổng Xuất và luồng Dán, không đi một parser riêng.
            _form.JsonFoldout.JsonReadBack = Services.JsonReadBack;
        }

        /// <summary>Services của cửa sổ (G-SESSION) — phiên lịch, đồng hồ, clipboard, hộp xác nhận, bus.</summary>
        internal LiveOpsHubServices Services { get; }

        public string Id => LiveOpsHubSections.Ids.RecurringRules;
        public string Title => LiveOpsHubStrings.ShellRecurringRulesTitle;
        public string Subtitle => LiveOpsHubStrings.ShellRecurringRulesSubtitle;
        public PipelineStage Stage => PipelineStage.Schedule;
        public IReadOnlyList<string> RequiredElementNames => ElementNames;

        internal RecurringRuleList List => _list;
        internal RecurringRuleForm Form => _form;
        internal RecurringPrefixDraft Draft => _draft;
        internal string SelectedEventType => _selectedEventType;

        public SectionHealth GetHealth()
        {
            LiveOpsHubCalendarSession session = Services.Session;
            return LiveOpsHubFindingRouting.HealthFor(LiveOpsHubHealthTarget.RecurringRules, session.Asset != null,
                session.Check, session.Document, Services.Format);
        }

        public VisualElement CreateView()
        {
            VisualTreeAsset layout = Services.LayoutLoader.LoadVisualTree(LiveOpsHubPaths.RecurringRulesSectionUxml);
            if (layout == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.RecurringMissingLayoutFormat, LiveOpsHubPaths.RecurringRulesSectionUxml));
            }
            _root = layout.Instantiate();
            _root.name = BodyElementName;
            // (UX-21) Instantiate() trả về một TemplateContainer trần: không class thì nó cao đúng bằng nội dung, và
            // flex-grow của hai pane bên trong không có gì để giãn theo — viền pane trái dừng giữa cửa sổ.
            _root.AddToClassList(LiveOpsHubClassNames.RecurringBody);
            StyleSheet sheet = Services.LayoutLoader.LoadStyleSheet(LiveOpsHubPaths.RecurringRulesSectionUss);
            if (sheet != null) _root.styleSheets.Add(sheet);

            VisualElement listHost = _root.Q(ListHostElementName);
            if (listHost != null) listHost.Add(_list);
            VisualElement formHost = _root.Q(FormHostElementName);
            if (formHost != null) formHost.Add(_form);
            // (RC-06b) ScrollView của pane form mọc THANH CUỘN NGANG trong khi nội dung rộng ĐÚNG BẰNG viewport
            // (387/387, 551/551, 564/564): thanh thừa ăn mất chiều cao và nói dối rằng "còn nội dung bên phải".
            // Form đã có max-width 640 + min-width 0 nên nó không bao giờ cần cuộn ngang — tắt hẳn thanh đó, cùng
            // cách pane danh sách đã làm trong RecurringRuleList. Đặt từ C# chứ không đặt bằng thuộc tính UXML vì
            // setter `mode` của ScrollView ghi đè lại hai thuộc tính scroller, mà thứ tự áp thuộc tính UXML là
            // chuyện nội bộ của UI Toolkit — đặt sau khi cây đã dựng thì không có cửa cho thứ tự đó phá.
            ScrollView formScroll = _root.Q<ScrollView>(className: LiveOpsHubClassNames.RecurringFormScroll);
            if (formScroll != null) formScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            _empty = _root.Q(EmptyElementName);
            _emptyTitle = _root.Q<Label>(EmptyTitleElementName);
            _emptyBody = _root.Q<Label>(EmptyBodyElementName);
            _emptyAction = _root.Q<Button>(EmptyActionElementName);
            if (_emptyAction != null) _emptyAction.clicked += OnEmptyActionClicked;
            _addPopover = _root.Q(AddPopoverElementName);
            BuildAddPopover();

            Services.Session.DocumentChanged += Refresh;
            Services.Session.CheckChanged += Refresh;
            _root.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            Refresh();
            return _root;
        }

        internal const string ListHostElementName = "recurring-list-host";

        public void OnShown()
        {
            Refresh();
        }

        public void Bind(IHubHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>Nút chính của section header: "Thêm luật" (icon Toolbar Plus); khoá thì in lý do thành chữ cạnh nút (SP-3).</summary>
        public void PopulateHeaderActions(VisualElement container)
        {
            // (UX-24) Button KHÔNG giữ text: chữ của Button do chính TextElement của nó vẽ, nằm DƯỚI mọi con, nên icon
            // chèn vào đè lên giữa chữ ("Th+m luật"). Icon và chữ là hai con riêng — đúng cách màn Loại event đang làm.
            Button button = new Button(ToggleAddPopover) { name = AddRuleButtonElementName };
            button.AddToClassList(LiveOpsHubClassNames.Button);
            button.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            button.AddToClassList(LiveOpsHubClassNames.RecurringAddRuleButton);
            Image icon = LiveOpsHubIcons.CreateImage(AddRuleIconName, AddRuleIconSize);
            icon.AddToClassList(LiveOpsHubClassNames.RecurringAddRuleButtonIcon);
            button.Add(icon);
            Label label = new Label(LiveOpsHubStrings.RecurringAddRuleButton);
            label.AddToClassList(LiveOpsHubClassNames.RecurringAddRuleButtonLabel);
            button.Add(label);
            // Nút sống trong section header của shell — cây KHÁC với thân màn nơi CreateView nạp sheet — nên ba class trên
            // không có style nếu không nạp thêm ở đây (cùng lý do EventTypesSection đã ghi).
            StyleSheet headerSheet = Services.LayoutLoader.LoadStyleSheet(LiveOpsHubPaths.RecurringRulesSectionUss);
            if (headerSheet != null) button.styleSheets.Add(headerSheet);
            _addRuleSlot = new LiveOpsButtonSlot(button);
            container.Add(_addRuleSlot);
            RefreshAddRuleButton();
        }

        private const string AddRuleIconName = "Toolbar Plus";
        private const int AddRuleIconSize = 16;

        public string CaptureViewState()
        {
            ViewState state = new ViewState
            {
                selectedEventType = _selectedEventType,
                occurrenceCount = _occurrenceCount,
                jsonFoldoutOpen = _jsonFoldoutOpen,
            };
            return JsonUtility.ToJson(state);
        }

        public void RestoreViewState(string viewStateJson)
        {
            if (string.IsNullOrEmpty(viewStateJson)) return;
            ViewState state;
            try
            {
                state = JsonUtility.FromJson<ViewState>(viewStateJson);
            }
            catch (ArgumentException)
            {
                // JSON của bản trước không đọc được: về mặc định, không làm sập màn (hợp đồng IHubSectionViewState).
                return;
            }
            if (state == null) return;
            _selectedEventType = state.selectedEventType ?? string.Empty;
            _occurrenceCount = state.occurrenceCount > 0 ? state.occurrenceCount : RecurringRuleModel.DefaultOccurrenceCount;
            _jsonFoldoutOpen = state.jsonFoldoutOpen;
            _form.JsonFoldout.SetValueWithoutNotify(_jsonFoldoutOpen);
            Refresh();
        }

        /// <summary>Tới từ Lịch / Tổng quan bằng "Mở luật weekly-pass": chọn hàng rồi chớp nền một nhịp (7.4).</summary>
        public void ApplyNavigation(LiveOpsHubNavigation navigation)
        {
            if (navigation == null || navigation.EventType.Length == 0) return;
            SelectRule(navigation.EventType);
            if (navigation.Flash) _list.Flash(navigation.EventType);
        }

        /// <summary>F8 / Shift F8: đi qua các luật CÓ phát hiện, theo thứ tự danh sách; false khi màn không có phát hiện nào.</summary>
        public bool TryMoveToFinding(int direction)
        {
            List<string> withFindings = EventTypesWithFindings();
            if (withFindings.Count == 0) return false;
            int current = -1;
            for (int index = 0; index < withFindings.Count; index++)
            {
                if (string.Equals(withFindings[index], _selectedEventType, StringComparison.Ordinal)) current = index;
            }
            int step = direction >= 0 ? 1 : -1;
            int next = current < 0 ? (step > 0 ? 0 : withFindings.Count - 1) : current + step;
            if (next < 0 || next >= withFindings.Count) return false;
            SelectRule(withFindings[next]);
            _list.Flash(withFindings[next]);
            return true;
        }

        internal void Refresh()
        {
            if (_root == null) return;
            LiveOpsHubCalendarSession session = Services.Session;
            LiveEventCalendarDocument document = session.Document;
            bool hasAsset = session.Asset != null;
            IReadOnlyList<RecurringLiveEventRule> rules = document != null ? document.RecurringRules : Array.Empty<RecurringLiveEventRule>();

            EnsureSelection(rules);
            _list.SetRows(BuildListRows(document, rules), _selectedEventType);
            _list.SetSelectionWithoutNotify(_selectedEventType);

            bool isEmpty = !hasAsset || rules.Count == 0;
            _empty.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, !isEmpty);
            _emptyTitle.text = hasAsset ? LiveOpsHubStrings.RecurringEmptyTitle : LiveOpsHubStrings.RecurringNoAssetTitle;
            _emptyBody.text = hasAsset ? LiveOpsHubStrings.RecurringEmptyBody : LiveOpsHubStrings.RecurringNoAssetBody;
            // Trống phải có chỗ đi tiếp (mục 7): có lịch thì mở popover "Thêm luật", chưa có lịch thì sang Tổng quan tạo lịch.
            if (_emptyAction != null)
            {
                _emptyAction.text = hasAsset ? LiveOpsHubStrings.RecurringAddRuleButton : LiveOpsHubStrings.RecurringNoAssetActionButton;
            }

            _form.SetPresetNames(PresetNames());
            RecurringRuleModel model = RecurringRuleModel.Build(session, _selectedEventType, _draft, _occurrenceCount, Services.Format);
            _form.Bind(model, _draft, JsonOf(document, model), OccurrencesSubtitle(),
                _occurrenceCount >= RecurringRuleModel.MaximumOccurrenceCount);
            RefreshAddRuleButton();
        }

        private void EnsureSelection(IReadOnlyList<RecurringLiveEventRule> rules)
        {
            for (int index = 0; index < rules.Count; index++)
            {
                if (string.Equals(rules[index].EventType, _selectedEventType, StringComparison.Ordinal)) return;
            }
            _selectedEventType = rules.Count > 0 ? rules[0].EventType : string.Empty;
        }

        private IReadOnlyList<RecurringRuleListRow> BuildListRows(LiveEventCalendarDocument document,
            IReadOnlyList<RecurringLiveEventRule> rules)
        {
            List<RecurringRuleListRow> rows = new List<RecurringRuleListRow>(rules.Count);
            for (int index = 0; index < rules.Count; index++)
            {
                RecurringLiveEventRule rule = rules[index];
                LiveEventTypeDefinition definition;
                int colorSlot = document != null && document.TryGetEventType(rule.EventType, out definition) ? definition.ColorSlot : 0;
                // (UX-32) "mỗi …" là NHỊP nên quy đổi bằng đúng luật của header làn màn Lịch (PeriodText); "chạy …" là
                // độ dài MỘT đợt, không phải nhịp để đối chiếu giữa hai màn, nên giữ cách quy đổi thường (HoursText).
                string meta = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringListMetaFormat,
                    RecurringRuleModel.PeriodText(rule.PeriodHours, Services.Format),
                    RecurringRuleModel.HoursText(rule.ActiveHours, Services.Format));
                rows.Add(new RecurringRuleListRow(rule.EventType, colorSlot, meta, SeverityOf(rule.EventType)));
            }
            return rows;
        }

        /// <summary>
        /// Mức của DẤU trên hàng danh sách. (UX-32) Câu "vẫn còn weekly-pass-35 đang chạy" treo dưới ô là một cảnh báo
        /// THẬT về chính luật này: bỏ nó ra ngoài thì pane trái nói "không sao" trong lúc form ngay cạnh nói "có chuyện",
        /// cùng một luật, cùng một màn.
        /// <para>
        /// Tách khỏi <see cref="FindingSeverityOf"/> vì F8 đi qua tập PHÁT HIỆN của Kiểm lịch: gộp hai thứ lại thì F8
        /// dừng cả ở luật không có phát hiện nào, và người dùng bấm F8 để tìm việc phải sửa lại rơi vào một hàng mà màn
        /// Kiểm lịch không hề nhắc tới.
        /// </para>
        /// </summary>
        private HealthState? SeverityOf(string eventType)
        {
            HealthState? worst = FindingSeverityOf(eventType);
            if (!RecurringRuleModel.HasAfterWriteNotice(Services.Session, eventType)) return worst;
            bool findingIsWorse = worst.HasValue && SectionHealth.RankOf(worst.Value) >= SectionHealth.RankOf(HealthState.Warning);
            return findingIsWorse ? worst : HealthState.Warning;
        }

        /// <summary>Mức lấy TỪ phát hiện của Kiểm lịch cho luật này — đúng tập mà F8 / Shift F8 đi qua.</summary>
        private HealthState? FindingSeverityOf(string eventType)
        {
            LiveEventCalendarCheckReport report = Services.Session.Check.LastReport;
            if (report == null) return null;
            HealthState? worst = null;
            IReadOnlyList<LiveEventCalendarFinding> findings = report.Findings;
            for (int index = 0; index < findings.Count; index++)
            {
                LiveEventCalendarFinding finding = findings[index];
                if (finding.IsIgnored || !LiveOpsHubFindingRouting.BelongsTo(finding, LiveOpsHubHealthTarget.RecurringRules)) continue;
                if (!string.Equals(finding.TargetId, eventType, StringComparison.Ordinal)) continue;
                HealthState state = LiveOpsHubFindingRouting.StateOf(finding.Consequence);
                if (!worst.HasValue || SectionHealth.RankOf(state) > SectionHealth.RankOf(worst.Value)) worst = state;
            }
            return worst;
        }

        private List<string> EventTypesWithFindings()
        {
            List<string> eventTypes = new List<string>();
            LiveEventCalendarDocument document = Services.Session.Document;
            if (document == null) return eventTypes;
            IReadOnlyList<RecurringLiveEventRule> rules = document.RecurringRules;
            for (int index = 0; index < rules.Count; index++)
            {
                if (FindingSeverityOf(rules[index].EventType).HasValue) eventTypes.Add(rules[index].EventType);
            }
            return eventTypes;
        }

        private IReadOnlyList<string> PresetNames()
        {
            IReadOnlyList<LiveOpsRulePresetLibrary.Preset> presets = LiveOpsRulePresets.Resolve();
            List<string> names = new List<string>(presets.Count);
            for (int index = 0; index < presets.Count; index++) names.Add(presets[index].DisplayName);
            return names;
        }

        private string OccurrencesSubtitle()
        {
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringOccurrencesSubtitleFormat,
                Services.Format.ShortDateTimeUtc(Services.Session.Clock.UtcNow));
        }

        /// <summary>
        /// JSON đổ vào ô là của luật ĐANG GHI, không phải của nháp: ô tự so chữ trong nó với chuỗi này để biết "chưa đổi gì",
        /// và <see cref="OnApplyJsonRequested"/> cũng so ứng viên với luật đang ghi (<c>TryGetWrittenRule</c>). Lấy
        /// <c>model.Rule</c> thì trong lúc còn nháp hai chỗ hiểu "đang ghi" theo hai nghĩa khác nhau, và câu "JSON chưa đổi so
        /// với luật đang ghi" nói sai — nó thực ra đang so với JSON của NHÁP.
        /// </summary>
        private static string JsonOf(LiveEventCalendarDocument document, RecurringRuleModel model)
        {
            if (document == null || model.WrittenRule == null) return string.Empty;
            return LiveEventCalendarJsonWriter.WriteRecurringRuleObject(document, model.WrittenRule);
        }

        private void SelectRule(string eventType)
        {
            if (string.Equals(_selectedEventType, eventType, StringComparison.Ordinal)) return;
            // Đổi luật đang xem là bỏ nháp của luật cũ: nháp sống ở MỘT ô, không theo người dùng sang luật khác.
            _draft = RecurringPrefixDraft.None;
            _selectedEventType = eventType ?? string.Empty;
            _occurrenceCount = RecurringRuleModel.DefaultOccurrenceCount;
            Refresh();
        }

        private void OnListSelectionChanged(string eventType)
        {
            SelectRule(eventType);
        }

        private void OnEmptyActionClicked()
        {
            if (Services.Session.Asset != null)
            {
                ToggleAddPopover();
                return;
            }
            Services.Bus.Navigate(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Overview));
        }

        /// <summary>
        /// Một field vừa commit (hoặc mẫu vừa áp): quyết theo policy rồi ghi thẳng hay giữ nháp tại ô. Là lối vào DUY NHẤT
        /// của mọi thay đổi trên form, nên test và kịch bản chụp dựng trạng thái nháp qua đúng đường người dùng đi.
        /// </summary>
        internal void RequestFieldChange(string fieldName, RecurringLiveEventRule candidate)
        {
            RecurringLiveEventRule written;
            if (!TryGetWrittenRule(candidate.EventType, out written)) return;
            if (IsSameAsWritten(written, candidate)) return;
            CommitOrHoldDraft(candidate, BuildDraft(fieldName, OperationOfField(fieldName), candidate));
        }

        /// <summary>Ô nào quyết định id của lần lặp (tiền tố, neo, chu kỳ) và ô nào chỉ quyết định giờ khép (bảng 7.0).</summary>
        private static LiveOpsEditOperation OperationOfField(string fieldName)
        {
            return string.Equals(fieldName, RecurringRuleFields.ActiveHours, StringComparison.Ordinal)
                ? LiveOpsEditOperation.ChangeRecurringActiveHours
                : LiveOpsEditOperation.ChangeRecurringIdentity;
        }

        private void OnPresetSelected(int presetIndex)
        {
            IReadOnlyList<LiveOpsRulePresetLibrary.Preset> presets = LiveOpsRulePresets.Resolve();
            if (presetIndex < 0 || presetIndex >= presets.Count) return;
            RecurringLiveEventRule written;
            if (!TryGetWrittenRule(_selectedEventType, out written)) return;
            LiveOpsRulePresetLibrary.Preset preset = presets[presetIndex];
            // Mẫu chỉ áp nhịp (neo, chu kỳ, thời gian chạy) — tiền tố và loại là danh tính, mẫu không đụng tới.
            RecurringLiveEventRule candidate = written.WithAnchor(preset.AnchorUtcText)
                .WithPeriodHours(preset.PeriodHours)
                .WithActiveHours(preset.ActiveHours);
            if (IsSameAsWritten(written, candidate)) return;
            // Một mẫu đổi CẢ mốc sinh id LẪN thời gian chạy, nên hỏi policy hai lần rồi giữ mức nặng hơn: mẫu giữ nguyên id
            // mà rút ngắn đợt đang chạy vẫn phải qua hộp cấp 1 (7.0), chứ không được ghi thẳng chỉ vì id không đổi.
            RecurringPrefixDraft identityDraft = BuildDraft(RecurringRuleFields.Anchor,
                LiveOpsEditOperation.ChangeRecurringIdentity, candidate);
            RecurringPrefixDraft activeHoursDraft = BuildDraft(RecurringRuleFields.ActiveHours,
                LiveOpsEditOperation.ChangeRecurringActiveHours, candidate);
            CommitOrHoldDraft(candidate, HeavierDraft(identityDraft, activeHoursDraft));
        }

        private bool TryGetWrittenRule(string eventType, out RecurringLiveEventRule written)
        {
            written = null;
            LiveEventCalendarDocument document = Services.Session.Document;
            return document != null && document.TryGetRecurringRule(eventType, out written);
        }

        /// <summary>Giá trị vừa commit trùng cái đang ghi (gõ lại đúng chữ cũ): bỏ nháp, vẽ lại, không ghi và không hỏi.</summary>
        private bool IsSameAsWritten(RecurringLiveEventRule written, RecurringLiveEventRule candidate)
        {
            if (!string.Equals(written.CanonicalText, candidate.CanonicalText, StringComparison.Ordinal)) return false;
            _draft = RecurringPrefixDraft.None;
            Refresh();
            return true;
        }

        private RecurringPrefixDraft BuildDraft(string fieldName, LiveOpsEditOperation operation, RecurringLiveEventRule candidate)
        {
            LiveOpsHubCalendarSession session = Services.Session;
            return RecurringPrefixDraft.For(session.Document, session.Publish != null ? session.Publish.ActiveBaseline : null,
                session.Clock.UtcNow, session.AssetFileName, fieldName, operation, candidate, Services.Format);
        }

        /// <summary>Mức nặng hơn thắng (<see cref="LiveOpsConfirmRequirement"/> tăng dần theo mức nguy hiểm).</summary>
        private static RecurringPrefixDraft HeavierDraft(RecurringPrefixDraft first, RecurringPrefixDraft second)
        {
            return second.Requirement > first.Requirement ? second : first;
        }

        private void CommitOrHoldDraft(RecurringLiveEventRule candidate, RecurringPrefixDraft candidateDraft)
        {
            if (candidateDraft.NeedsConfirmation)
            {
                // Bước 1: giữ nháp tại ô. Main.asset chưa đổi nên Lịch, Kiểm lịch và Xuất JSON vẫn thấy giá trị cũ.
                _draft = candidateDraft;
                Refresh();
                return;
            }
            _draft = RecurringPrefixDraft.None;
            WriteRule(candidate, candidateDraft.HasDraft
                ? candidateDraft.ToastText
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringWriteToastFormat, candidate.EventType));
        }

        private void OnDraftCancelRequested()
        {
            _draft = RecurringPrefixDraft.None;
            Refresh();
        }

        private void OnDraftWriteRequested()
        {
            if (!_draft.NeedsConfirmation) return;
            LiveOpsConfirmResult result = Services.Confirmation.Confirm(_draft.BuildConfirmRequest());
            if (result != LiveOpsConfirmResult.Destructive)
            {
                // Esc hay "Giữ tiền tố cũ" đưa người dùng về đúng bước 1 với nháp CÒN NGUYÊN ([SD1 §4.2]).
                Refresh();
                return;
            }
            RecurringLiveEventRule rule = _draft.DraftRule;
            string toast = _draft.ToastText;
            _draft = RecurringPrefixDraft.None;
            WriteRule(rule, toast);
        }

        private void OnRevertPrefixRequested()
        {
            LiveEventCalendarDocument document = Services.Session.Document;
            RecurringLiveEventRule written;
            if (document == null || !document.TryGetRecurringRule(_selectedEventType, out written)) return;
            RecurringRuleModel model = RecurringRuleModel.Build(Services.Session, _selectedEventType, RecurringPrefixDraft.None,
                _occurrenceCount, Services.Format);
            if (model.AfterWriteRevertPrefix.Length == 0) return;
            RequestFieldChange(RecurringRuleFields.IdPrefix, written.WithIdPrefix(model.AfterWriteRevertPrefix));
        }

        private void OnDecideInValidationRequested()
        {
            Services.Bus.Navigate(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Validation)
                .WithEventType(_selectedEventType, true));
        }

        private void OnAddMoreOccurrencesRequested()
        {
            int next = _occurrenceCount + RecurringRuleModel.OccurrenceCountStep;
            _occurrenceCount = next > RecurringRuleModel.MaximumOccurrenceCount ? RecurringRuleModel.MaximumOccurrenceCount : next;
            ScheduleOccurrenceRefresh();
        }

        /// <summary>Debounce 250ms: card chỉ nói "đang tính…" trong lúc chờ, không lộ số mili giây ([SD1 §4.1]).</summary>
        private void ScheduleOccurrenceRefresh()
        {
            _form.Occurrences.ShowComputing(true);
            if (_occurrenceDebounce != null) _occurrenceDebounce.Pause();
            _occurrenceDebounce = _form.schedule.Execute(Refresh).StartingIn(OccurrenceDebounceMilliseconds);
        }

        /// <summary>
        /// "Áp" trong foldout JSON. Một object JSON đổi được CẢ mốc sinh id LẪN thời gian chạy trong một lần, nên hỏi policy
        /// hai thao tác rồi giữ mức nặng hơn — y như áp "Mẫu". Suy thao tác theo một ô duy nhất thì JSON chỉ rút ngắn đợt
        /// đang chạy sẽ lọt qua không hộp nào (bảng 7.0 cấm), còn JSON đổi tiền tố lại bị hỏi nhẹ hơn mức phải hỏi.
        /// </summary>
        internal void OnApplyJsonRequested(RecurringLiveEventRule candidate)
        {
            if (candidate == null) return;
            RecurringLiveEventRule written;
            if (!TryGetWrittenRule(candidate.EventType, out written)) return;
            if (IsSameAsWritten(written, candidate)) return;
            RecurringPrefixDraft identityDraft = BuildDraft(RecurringRuleFields.Anchor,
                LiveOpsEditOperation.ChangeRecurringIdentity, candidate);
            RecurringPrefixDraft activeHoursDraft = BuildDraft(RecurringRuleFields.ActiveHours,
                LiveOpsEditOperation.ChangeRecurringActiveHours, candidate);
            CommitOrHoldDraft(candidate, HeavierDraft(identityDraft, activeHoursDraft));
        }

        private void OnCopyJsonRequested()
        {
            LiveEventCalendarDocument document = Services.Session.Document;
            RecurringLiveEventRule rule;
            if (document == null || !document.TryGetRecurringRule(_selectedEventType, out rule)) return;
            Services.Clipboard.Text = LiveEventCalendarJsonWriter.WriteRecurringRuleObject(document, rule);
            Services.Bus.ShowToast(LiveOpsToastModel.Info(string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.RecurringJsonCopiedToastFormat, _selectedEventType)));
        }

        private void OnDeleteRequested()
        {
            LiveOpsHubCalendarSession session = Services.Session;
            LiveEventCalendarDocument document = session.Document;
            RecurringLiveEventRule rule;
            if (document == null || !document.TryGetRecurringRule(_selectedEventType, out rule)) return;

            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.RemoveRecurringRule,
                document, null, session.Publish != null ? session.Publish.ActiveBaseline : null, session.Clock.UtcNow, _selectedEventType);
            if (decision.Requirement != LiveOpsConfirmRequirement.None)
            {
                LiveOpsConfirmRequest request = new LiveOpsConfirmRequest.Builder()
                    .WithTitle(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringDeleteConfirmTitleFormat, _selectedEventType))
                    .WithBody(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringDeleteConfirmBodyFormat,
                        decision.RunningEventId, decision.RunningEndUtc.HasValue
                            ? Services.Format.ShortDateTimeUtc(decision.RunningEndUtc.Value)
                            : string.Empty))
                    .WithHelpBoxWarning()
                    .WithButtons(LiveOpsHubStrings.RecurringDeleteConfirmDestructive, LiveOpsHubStrings.KitConfirmKeepLabel)
                    .WithTypeToConfirm(decision.TypeToConfirmText)
                    .Build();
                if (Services.Confirmation.Confirm(request) != LiveOpsConfirmResult.Destructive) return;
            }

            string toast = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringDeleteToastFormat, _selectedEventType);
            LiveOpsHubEditOutcome outcome = session.Apply(new RemoveRecurringRuleEdit(_selectedEventType), toast);
            _draft = RecurringPrefixDraft.None;
            _selectedEventType = string.Empty;
            AnnounceOutcome(outcome, toast);
        }

        private void WriteRule(RecurringLiveEventRule rule, string toast)
        {
            LiveOpsHubEditOutcome outcome = Services.Session.Apply(new SetRecurringRuleEdit(rule), toast);
            AnnounceOutcome(outcome, toast);
        }

        private void AnnounceOutcome(LiveOpsHubEditOutcome outcome, string toast)
        {
            if (outcome.Applied)
            {
                Services.Bus.ShowToast(LiveOpsToastModel.ForEdit(toast, outcome.UndoGroup));
                Services.Bus.InvalidateHealth();
            }
            Refresh();
        }

        private void BuildAddPopover()
        {
            if (_addPopover == null) return;
            _addTypeField = new DropdownField { name = AddTypeElementName, label = LiveOpsHubStrings.RecurringAddTypeLabel };
            _addPresetField = new DropdownField { name = AddPresetElementName, label = LiveOpsHubStrings.RecurringAddPresetLabel };
            _addPopover.Add(_addTypeField);
            _addPopover.Add(_addPresetField);

            VisualElement actions = new VisualElement();
            actions.AddToClassList(LiveOpsHubClassNames.RecurringAddActions);
            Button cancel = new Button(CloseAddPopover) { name = AddCancelElementName, text = LiveOpsHubStrings.RecurringAddCancelButton };
            cancel.AddToClassList(LiveOpsHubClassNames.Button);
            cancel.AddToClassList(LiveOpsHubClassNames.ButtonFirst);
            actions.Add(cancel);
            Button confirm = new Button(ConfirmAddRule) { name = AddConfirmElementName, text = LiveOpsHubStrings.RecurringAddConfirmButton };
            confirm.AddToClassList(LiveOpsHubClassNames.Button);
            confirm.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            actions.Add(confirm);
            _addPopover.Add(actions);
            _addPopover.AddToClassList(LiveOpsHubClassNames.RecurringHidden);
        }

        private void ToggleAddPopover()
        {
            if (_addPopover == null) return;
            bool willOpen = _addPopover.ClassListContains(LiveOpsHubClassNames.RecurringHidden);
            if (!willOpen)
            {
                CloseAddPopover();
                return;
            }
            List<string> typesWithoutRule = TypesWithoutRule();
            if (typesWithoutRule.Count == 0) return;
            _addTypeField.choices = typesWithoutRule;
            _addTypeField.SetValueWithoutNotify(typesWithoutRule[0]);
            List<string> presetNames = new List<string>(PresetNames());
            _addPresetField.choices = presetNames;
            if (presetNames.Count > 0) _addPresetField.SetValueWithoutNotify(presetNames[0]);
            _addPopover.RemoveFromClassList(LiveOpsHubClassNames.RecurringHidden);
        }

        private void CloseAddPopover()
        {
            if (_addPopover != null) _addPopover.AddToClassList(LiveOpsHubClassNames.RecurringHidden);
        }

        private void ConfirmAddRule()
        {
            IReadOnlyList<LiveOpsRulePresetLibrary.Preset> presets = LiveOpsRulePresets.Resolve();
            string eventType = _addTypeField.value;
            if (string.IsNullOrEmpty(eventType) || presets.Count == 0) return;
            LiveOpsRulePresetLibrary.Preset preset = presets[0];
            for (int index = 0; index < presets.Count; index++)
            {
                if (string.Equals(presets[index].DisplayName, _addPresetField.value, StringComparison.Ordinal)) preset = presets[index];
            }
            // Tiền tố để trống = runtime tự ghép eventType + "-": luật mới không bao giờ mang tiền tố của một luật khác.
            RecurringLiveEventRule rule = new RecurringLiveEventRule(eventType, preset.AnchorUtcText, string.Empty,
                preset.PeriodHours, preset.ActiveHours, string.Empty);
            CloseAddPopover();
            string toast = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringAddToastFormat, eventType);
            LiveOpsHubEditOutcome outcome = Services.Session.Apply(new SetRecurringRuleEdit(rule), toast);
            if (outcome.Applied) _selectedEventType = eventType;
            AnnounceOutcome(outcome, toast);
        }

        private List<string> TypesWithoutRule()
        {
            List<string> types = new List<string>();
            LiveEventCalendarDocument document = Services.Session.Document;
            if (document == null) return types;
            IReadOnlyList<LiveEventTypeDefinition> definitions = document.EventTypes;
            for (int index = 0; index < definitions.Count; index++)
            {
                RecurringLiveEventRule existing;
                if (!document.TryGetRecurringRule(definitions[index].TypeId, out existing)) types.Add(definitions[index].TypeId);
            }
            return types;
        }

        private void RefreshAddRuleButton()
        {
            if (_addRuleSlot == null) return;
            bool hasAsset = Services.Session.Asset != null;
            if (!hasAsset)
            {
                _addRuleSlot.SetEnabledWithReason(false, LiveOpsHubStrings.RecurringAddRuleNoAssetReason);
                return;
            }
            bool hasFreeType = TypesWithoutRule().Count > 0;
            _addRuleSlot.SetEnabledWithReason(hasFreeType, hasFreeType ? string.Empty : LiveOpsHubStrings.RecurringAddRuleNoTypeReason);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent detachEvent)
        {
            Services.Session.DocumentChanged -= Refresh;
            Services.Session.CheckChanged -= Refresh;
            if (_emptyAction != null) _emptyAction.clicked -= OnEmptyActionClicked;
            _emptyAction = null;
            if (_occurrenceDebounce != null) _occurrenceDebounce.Pause();
            _root = null;
        }

        /// <summary>Trạng thái view sống qua domain reload (8.1): luật đang chọn, số đợt đã mở, foldout JSON.</summary>
        [Serializable]
        private sealed class ViewState
        {
            public string selectedEventType;
            public int occurrenceCount;
            public bool jsonFoldoutOpen;
        }
    }
}
