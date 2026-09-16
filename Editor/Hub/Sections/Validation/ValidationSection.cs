using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Màn <c>validation</c> ở tầng Kiểm (mục 7.5, [SD2 §2]): kết quả 12 luật nhóm theo HẬU QUẢ VỚI NGƯỜI CHƠI, không theo
    /// "error / warning". Ba luật sống của màn:
    /// <list type="bullet">
    /// <item>Không câu nào của phát hiện được viết ở đây — <see cref="LiveOpsFindingText"/> là nguồn duy nhất (V-8), và luôn
    /// gọi overload CÓ NGỮ CẢNH vì phiên biết dấu đã đăng, đồng hồ và tên asset (V-22 CC-FT-1).</item>
    /// <item>"Sửa" (an toàn) áp ngay; "Đề xuất…" (đổi điều người chơi thấy) LUÔN mở popover xem trước, từng cái một, và không
    /// bao giờ nằm trong nút sửa hàng loạt ([SD2 §2.4]).</item>
    /// <item>Sửa xong thì tự kiểm lại (PD-10): kết quả cũ không được phép trông như kết quả mới.</item>
    /// </list>
    /// </summary>
    internal sealed class ValidationSection : IHubSection, IHubHostAware, IHubSectionActions, IHubSectionViewState,
        IHubSectionNavigation, IHubSectionFindings
    {
        internal const string SafeRepairButtonElementName = "validation-safe-repair";
        internal const string RecheckButtonElementName = "validation-recheck";
        internal const string EmptyTitleElementName = "validation-empty-title";
        internal const string EmptyBodyElementName = "validation-empty-body";
        internal const string EmptyActionElementName = "validation-empty-action";
        internal const string NoticeButtonElementName = "validation-notice-recheck";
        internal const string ShouldReviewNoteElementName = "validation-should-review-note";
        internal const string ProgressRatioElementName = "validation-progress-ratio";

        private static readonly IReadOnlyList<string> ElementNames = LiveOpsHubPaths.RequiredValidationElementNames;

        private readonly ValidationDetailPane _detail = new ValidationDetailPane();
        private readonly List<ValidationFindingRow> _visibleRows = new List<ValidationFindingRow>();
        private readonly List<ValidationGroupCard> _cards = new List<ValidationGroupCard>();

        private IHubHost _host;
        private VisualElement _root;
        private VisualElement _notice;
        private VisualElement _toolbar;
        private LiveOpsTabStrip _tabs;
        private VisualElement _typeMenuHost;
        private ToolbarMenu _typeMenu;
        private TextField _search;
        private Label _searchPlaceholder;
        private VisualElement _summary;
        private VisualElement _progress;
        private VisualElement _progressFill;
        private Label _progressLabel;
        private Label _progressRatioLabel;
        private LiveOpsSpinner _spinner;
        private VisualElement _content;
        private ScrollView _groups;
        private VisualElement _empty;
        private Label _emptyTitle;
        private Label _emptyBody;
        private Button _emptyAction;
        private LiveOpsButtonSlot _safeRepairSlot;
        private Button _recheckButton;
        private VisualElement _bulkPreviewHost;
        private SafeRepairPreviewCard _bulkPreview;
        private LiveOpsHoverCardHost _hoverCardHost;

        /// <summary>Dấu vân tay của tập lệnh sửa an toàn lúc mở card xem trước; lệch = báo cáo đã đổi, card phải đóng.</summary>
        private string _bulkPreviewKeys = string.Empty;

        private ValidationFilter _filter = ValidationFilter.All;
        private string _selectedKey = string.Empty;
        private bool _isDrawerOpen;

        internal ValidationSection(LiveOpsHubServices services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            // Nối MỘT LẦN ở đây: khung gọi CreateView mỗi lần mở lại màn, mà pane Chi tiết sống suốt đời section — nối lại mỗi
            // lượt thì vào-ra-vào ba lần là mỗi lần bấm "CÁCH SỬA" mở ba popover.
            _detail.RepairRequested += OnDetailRepairRequested;
            _detail.CloseRequested += CloseDrawer;
        }

        /// <summary>Services của cửa sổ (G-SESSION): phiên lịch, đồng hồ, clipboard, bus.</summary>
        internal LiveOpsHubServices Services { get; }

        public string Id => LiveOpsHubSections.Ids.Validation;
        public string Title => LiveOpsHubStrings.ShellValidationTitle;
        public string Subtitle => LiveOpsHubStrings.ShellValidationSubtitle;
        public PipelineStage Stage => PipelineStage.Check;
        public IReadOnlyList<string> RequiredElementNames => ElementNames;

        internal ValidationDetailPane Detail => _detail;
        internal ValidationFilter Filter => _filter;
        internal IReadOnlyList<ValidationFindingRow> VisibleRows => _visibleRows;
        internal IReadOnlyList<ValidationGroupCard> Cards => _cards;
        internal string SelectedKey => _selectedKey;
        internal LiveOpsButtonSlot SafeRepairSlot => _safeRepairSlot;
        internal Button RecheckButton => _recheckButton;

        /// <summary>Card xem trước sửa hàng loạt đang mở; null khi chưa bấm "Sửa các lỗi an toàn (n)…" ([SD2 §2.5]).</summary>
        internal SafeRepairPreviewCard BulkPreview => _bulkPreview;

        /// <summary>Card "Đã bỏ qua (n) ▸" (V-15); null khi chưa kiểm lần nào.</summary>
        internal ValidationGroupCard IgnoredCard
        {
            get
            {
                for (int index = 0; index < _cards.Count; index++)
                {
                    if (_cards[index].Group.Kind == ValidationGroupKind.Ignored) return _cards[index];
                }
                return null;
            }
        }

        /// <summary>Hover card ghim của "Xem ghi chú"; null khi chưa ai mở lần nào trong lượt dựng này.</summary>
        internal LiveOpsHoverCardHost HoverCardHost => _hoverCardHost;

        public void Bind(IHubHost host)
        {
            _host = host;
        }

        public SectionHealth GetHealth()
        {
            LiveOpsHubCalendarSession session = Services.Session;
            return LiveOpsHubFindingRouting.HealthFor(LiveOpsHubHealthTarget.Validation, session.Asset != null,
                session.Check, session.Document, Services.Format);
        }

        public VisualElement CreateView()
        {
            VisualTreeAsset layout = Services.LayoutLoader.LoadVisualTree(LiveOpsHubPaths.ValidationSectionUxml);
            if (layout == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.ValidationMissingLayoutFormat, LiveOpsHubPaths.ValidationSectionUxml));
            }
            // Bỏ vỏ TemplateContainer: vỏ không mang class nào nên nó không giãn, và thân màn sẽ cao 0 trong cột section body.
            _root = layout.Instantiate().Q(LiveOpsHubPaths.ValidationElementNames.Body) ?? layout.Instantiate();
            _root.name = LiveOpsHubPaths.ValidationElementNames.Body;
            StyleSheet sheet = Services.LayoutLoader.LoadStyleSheet(LiveOpsHubPaths.ValidationSectionUss);
            if (sheet != null) _root.styleSheets.Add(sheet);

            _notice = _root.Q(LiveOpsHubPaths.ValidationElementNames.Notice);
            _toolbar = _root.Q(LiveOpsHubPaths.ValidationElementNames.Toolbar);
            _tabs = _root.Q<LiveOpsTabStrip>(LiveOpsHubPaths.ValidationElementNames.Tabs);
            if (_tabs != null) _tabs.SelectedIndexChanged += OnTabSelected;
            _typeMenuHost = _root.Q(LiveOpsHubPaths.ValidationElementNames.TypeMenu);
            BuildTypeMenu();
            _search = _root.Q<TextField>(LiveOpsHubPaths.ValidationElementNames.Search);
            BuildSearch();
            _summary = _root.Q(LiveOpsHubPaths.ValidationElementNames.Summary);
            _bulkPreviewHost = _root.Q(LiveOpsHubPaths.ValidationDepthElementNames.BulkPreviewHost);
            _progress = _root.Q(LiveOpsHubPaths.ValidationElementNames.Progress);
            BuildProgress();
            _content = _root.Q(LiveOpsHubPaths.ValidationElementNames.Content);
            _groups = _root.Q<ScrollView>(LiveOpsHubPaths.ValidationElementNames.Groups);
            if (_content != null) _content.Add(_detail);
            _empty = _root.Q(LiveOpsHubPaths.ValidationElementNames.Empty);
            BuildEmpty();

            Services.Session.DocumentChanged += Refresh;
            Services.Session.CheckChanged += Refresh;
            _root.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel, TrickleDown.TrickleDown);
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _root.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);

            Refresh();
            return _root;
        }

        public void OnShown()
        {
            Refresh();
        }

        public void PopulateHeaderActions(VisualElement container)
        {
            Button safeRepair = new Button(OnSafeRepairClicked) { name = SafeRepairButtonElementName };
            safeRepair.AddToClassList(LiveOpsHubClassNames.Button);
            _safeRepairSlot = new LiveOpsButtonSlot(safeRepair);
            container.Add(_safeRepairSlot);

            _recheckButton = new Button(OnRecheckClicked) { name = RecheckButtonElementName, text = LiveOpsHubStrings.ValidationRecheckButton };
            _recheckButton.AddToClassList(LiveOpsHubClassNames.Button);
            _recheckButton.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            _recheckButton.Insert(0, LiveOpsHubIcons.CreateImage(RecheckIconName, RecheckIconSize));
            container.Add(_recheckButton);
            RefreshHeaderActions(null);
        }

        private const string RecheckIconName = "Refresh";
        private const int RecheckIconSize = 16;

        public string CaptureViewState()
        {
            ViewState state = new ViewState
            {
                consequenceFilter = _filter.ConsequenceFilter,
                eventType = _filter.EventType,
                searchText = _filter.SearchText,
                selectedKey = _selectedKey,
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
                // JSON của bản trước không được làm sập màn: trạng thái hỏng = trạng thái mặc định.
                return;
            }
            if (state == null) return;
            _filter = new ValidationFilter(IsKnownFilter(state.consequenceFilter) ? state.consequenceFilter : LiveOpsHubNavigation.FilterNone,
                state.eventType, state.searchText);
            _selectedKey = state.selectedKey ?? string.Empty;
            if (_root != null) Refresh();
        }

        public void ApplyNavigation(LiveOpsHubNavigation navigation)
        {
            if (navigation == null) return;
            if (navigation.FilterConsequence.Length > 0) _filter = _filter.WithConsequence(navigation.FilterConsequence);
            // Id luật từ rail/palette đi vào ô tìm: một id luật LÀ một chuỗi tìm hợp lệ, không cần bộ lọc thứ tư.
            if (navigation.RuleId.Length > 0) _filter = _filter.WithSearch(navigation.RuleId);
            if (navigation.EventType.Length > 0) _filter = _filter.WithEventType(navigation.EventType);
            if (_root != null) Refresh();
        }

        /// <summary>
        /// Chọn hàng của một đích (id đợt / id loại / id luật lặp) mà không cần toạ độ chuột — kịch bản chụp Hình 15 vẽ hàng
        /// <c>hunt-0916-bonus</c> đang chọn kèm pane Chi tiết của nó. Trả false khi không có hàng nào của đích đó.
        /// </summary>
        internal bool TrySelectFinding(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return false;
            for (int index = 0; index < _visibleRows.Count; index++)
            {
                LiveEventCalendarFinding finding = _visibleRows[index].Row.Finding;
                if (finding == null || !string.Equals(finding.TargetId, targetId, StringComparison.Ordinal)) continue;
                SelectRow(_visibleRows[index].Row);
                return true;
            }
            return false;
        }

        public bool TryMoveToFinding(int direction)
        {
            if (_visibleRows.Count == 0) return false;
            int current = IndexOfSelected();
            int next = current < 0 ? (direction >= 0 ? 0 : _visibleRows.Count - 1) : current + (direction >= 0 ? 1 : -1);
            if (next < 0 || next >= _visibleRows.Count) return false;
            SelectRow(_visibleRows[next].Row);
            _visibleRows[next].Focus();
            return true;
        }

        // ------------------------------------------------------------------------------------------------------ dựng lại thân

        private void Refresh()
        {
            if (_root == null) return;
            bool hasAsset = Services.Session.Asset != null;
            ValidationViewModel model = hasAsset
                ? ValidationViewModel.Build(Services.Session.Check, _filter, Services.Format, BuildTextContext())
                : null;

            RefreshHeaderActions(model);
            SetVisible(_toolbar, hasAsset && model.BodyState != ValidationBodyState.NeverChecked && model.BodyState != ValidationBodyState.Running);
            SetVisible(_summary, hasAsset && model.BodyState != ValidationBodyState.NeverChecked && model.BodyState != ValidationBodyState.Running);
            SetVisible(_progress, hasAsset && model.BodyState == ValidationBodyState.Running);
            SetVisible(_content, hasAsset && ShowsGroups(model));
            SetVisible(_empty, !hasAsset || ShowsEmpty(model));

            if (!hasAsset)
            {
                RefreshNotice(string.Empty);
                FillEmpty(LiveOpsHubStrings.ValidationNoAssetTitle, LiveOpsHubStrings.ValidationNoAssetBody,
                    LiveOpsHubStrings.ValidationNoAssetActionButton, OpenOverview);
                _visibleRows.Clear();
                _cards.Clear();
                return;
            }

            RefreshNotice(model.StaleNotice);
            RefreshTabs(model);
            RefreshTypeMenu();
            RefreshSearch();
            RefreshSummary(model);
            RefreshBulkPreview();
            RefreshProgress(model);
            RefreshGroups(model);
            RefreshEmptyState(model);
        }

        private ValidationTextContext BuildTextContext()
        {
            return new ValidationTextContext(Services.Session.Document, Services.Clock.UtcNow, Services.Session.AssetFileName);
        }

        /// <summary>
        /// Thân card có được vẽ không. "Không còn lỗi" VẪN vẽ: ba card phát hiện rỗng tự biến mất, nhưng card "Chưa kiểm" và
        /// hàng ngang hai card thu gọn phải ở lại ([SD2 §2.8]: "empty + hàng Chưa kiểm vẫn ở lại") — nếu không, người đọc mất
        /// đúng chỗ nói "luật này KHÔNG tính là đã qua" và mất nút "Dán JSON đang chạy…".
        /// </summary>
        private static bool ShowsGroups(ValidationViewModel model)
        {
            return model != null && model.BodyState != ValidationBodyState.NeverChecked && model.BodyState != ValidationBodyState.Running;
        }

        /// <summary>
        /// Khối <c>liveops-hub-empty</c> chỉ thuộc hai trạng thái có câu để nói ([SD2 §2.8]). Đang kiểm KHÔNG dùng nó: khối rỗng
        /// mà vẫn bật thì nút của lần trước ở lại giữa thân màn với chữ rỗng — một ô xám không ai bấm được.
        /// </summary>
        private static bool ShowsEmpty(ValidationViewModel model)
        {
            return model != null
                && (model.BodyState == ValidationBodyState.NeverChecked || model.BodyState == ValidationBodyState.NoErrors);
        }

        private void RefreshHeaderActions(ValidationViewModel model)
        {
            if (_safeRepairSlot != null)
            {
                int count = model != null ? model.SafeRepairCount : 0;
                _safeRepairSlot.Button.text = model != null
                    ? model.SafeRepairButtonText
                    : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationSafeRepairButtonFormat, 0);
                // Nhãn đếm theo số mục ĐANG BẬT của card xem trước khi card đang mở ([SD2 §2.5]); còn lại theo báo cáo.
                if (_bulkPreview != null) count = _bulkPreview.SelectedCount;
                if (_bulkPreview != null)
                {
                    _safeRepairSlot.Button.text = string.Format(CultureInfo.InvariantCulture,
                        LiveOpsHubStrings.ValidationSafeRepairButtonFormat, count);
                }
                // SPIKE-B SP-3: lý do khoá in thành chữ cạnh nút, tooltip chỉ phụ.
                _safeRepairSlot.SetEnabledWithReason(count > 0,
                    count > 0 ? string.Empty : LiveOpsHubStrings.ValidationSafeRepairNothingReason);
            }
            if (_recheckButton != null)
            {
                bool isRunning = Services.Session.Asset != null && Services.Session.Check.IsRunning;
                _recheckButton.text = isRunning ? LiveOpsHubStrings.ValidationRecheckRunningButton : LiveOpsHubStrings.ValidationRecheckButton;
                _recheckButton.SetEnabled(Services.Session.Asset != null && !isRunning);
            }
        }

        private void RefreshNotice(string notice)
        {
            if (_notice == null) return;
            _notice.Clear();
            SetVisible(_notice, notice.Length > 0);
            if (notice.Length == 0) return;
            HelpBox helpBox = new HelpBox(notice, HelpBoxMessageType.Info);
            _notice.Add(helpBox);
            Button recheck = new Button(OnRecheckClicked) { name = NoticeButtonElementName, text = LiveOpsHubStrings.ValidationRecheckButton };
            recheck.AddToClassList(LiveOpsHubClassNames.Button);
            _notice.Add(recheck);
        }

        private void RefreshTabs(ValidationViewModel model)
        {
            if (_tabs == null) return;
            var labels = new List<string>(model.Tabs.Count);
            int selected = 0;
            for (int index = 0; index < model.Tabs.Count; index++)
            {
                labels.Add(model.Tabs[index].Label);
                if (string.Equals(model.Tabs[index].Filter, _filter.ConsequenceFilter, StringComparison.Ordinal)) selected = index;
            }
            _tabs.Choices = string.Join(LiveOpsTabStrip.ChoiceSeparator.ToString(), labels.ToArray());
            _tabs.SetSelectedIndexWithoutNotify(selected);
        }

        private void RefreshTypeMenu()
        {
            if (_typeMenu == null) return;
            _typeMenu.text = _filter.EventType.Length == 0
                ? LiveOpsHubStrings.ValidationTypeMenuAll
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationTypeMenuFormat, _filter.EventType);
            _typeMenu.menu.ClearItems();
            _typeMenu.menu.AppendAction(LiveOpsHubStrings.ValidationTypeMenuAllItem, action => SetEventTypeFilter(string.Empty),
                action => _filter.EventType.Length == 0 ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            IReadOnlyList<LiveEventTypeDefinition> types = Services.Session.Document.EventTypes;
            for (int index = 0; index < types.Count; index++)
            {
                string typeId = types[index].TypeId;
                _typeMenu.menu.AppendAction(typeId, action => SetEventTypeFilter(typeId),
                    action => string.Equals(_filter.EventType, typeId, StringComparison.Ordinal)
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal);
            }
        }

        private void RefreshSummary(ValidationViewModel model)
        {
            if (_summary == null) return;
            _summary.Clear();
            HealthState[] states = { HealthState.Blocked, HealthState.Warning, HealthState.Warning, HealthState.NotMeasured, HealthState.Ok };
            for (int index = 0; index < model.SummaryParts.Count && index < states.Length; index++)
            {
                VisualElement item = new VisualElement();
                item.AddToClassList(LiveOpsHubClassNames.ValidationSummaryItem);
                LiveOpsStateMark mark = new LiveOpsStateMark();
                mark.SetHealth(states[index]);
                mark.Size = LiveOpsStateMark.MarkSize.Small;
                item.Add(mark);
                item.Add(new Label(model.SummaryParts[index]));
                _summary.Add(item);
            }
            VisualElement spacer = new VisualElement();
            spacer.AddToClassList(LiveOpsHubClassNames.ValidationToolbarSpacer);
            _summary.Add(spacer);
            Label right = new Label(model.SummaryRightText);
            right.AddToClassList(LiveOpsHubClassNames.ValidationSummaryRight);
            _summary.Add(right);
        }

        private void RefreshProgress(ValidationViewModel model)
        {
            if (_progress == null) return;
            bool isRunning = model.BodyState == ValidationBodyState.Running;
            if (_spinner != null)
            {
                if (isRunning) _spinner.Start();
                else _spinner.Stop();
            }
            if (_progressLabel != null) _progressLabel.text = model.ProgressText;
            if (_progressRatioLabel != null) _progressRatioLabel.text = model.ProgressRatioText;
            // ProgressBar tự làm: bề rộng phần đã chạy là % suy từ dữ liệu — đúng mục 5 của danh sách 10 chỗ.
            if (_progressFill != null) _progressFill.style.width = new Length(model.ProgressRatio * 100f, LengthUnit.Percent); // style-inline-allowed: 5
        }

        private void RefreshGroups(ValidationViewModel model)
        {
            if (_groups == null) return;
            _groups.Clear();
            _cards.Clear();
            _visibleRows.Clear();

            if (model.BodyState == ValidationBodyState.ShouldReviewOnly)
            {
                Label note = new Label(LiveOpsHubStrings.ValidationShouldReviewOnlyNote) { name = ShouldReviewNoteElementName };
                note.AddToClassList(LiveOpsHubClassNames.Note);
                _groups.Add(note);
            }

            VisualElement collapsedRow = new VisualElement();
            collapsedRow.AddToClassList(LiveOpsHubClassNames.ValidationCardsRow);
            bool isStale = model.StaleNotice.Length > 0;
            for (int index = 0; index < model.Groups.Count; index++)
            {
                ValidationGroup group = model.Groups[index];
                // Nhóm phát hiện rỗng biến mất; hai card thu gọn luôn ở lại vì chúng là bằng chứng "đã chạy tới đâu".
                if (!group.IsCollapsible && group.Rows.Count == 0) continue;
                ValidationGroupCard card = new ValidationGroupCard(group, isStale);
                card.RowActionRequested += OnRowAction;
                card.RowLinkRequested += OnRowLink;
                card.RowSelectionRequested += SelectRow;
                card.RowCopyDescriptionRequested += OnRowCopyDescription;
                card.RowOpenRuleDocumentationRequested += OnRowOpenRuleDocumentation;
                card.IgnoredNoteRequested += ShowIgnoredNoteCard;
                if (group.IsCollapsible) collapsedRow.Add(card);
                else _groups.Add(card);
                _cards.Add(card);
                for (int rowIndex = 0; rowIndex < card.Rows.Count; rowIndex++)
                {
                    _visibleRows.Add(card.Rows[rowIndex]);
                }
            }
            _groups.Add(collapsedRow);
            AttachDecisionMenus();
            ApplySelection();
        }

        /// <summary>
        /// Hàng "Quyết định… ▾" nhận menu SAU khi card dựng xong: menu phải áp được lệnh sửa nên nó cần phiên, mà hàng cố ý
        /// không biết gì về phiên (test hàng không cần asset). Màn là chỗ duy nhất có cả hai.
        /// </summary>
        private void AttachDecisionMenus()
        {
            for (int index = 0; index < _visibleRows.Count; index++)
            {
                ValidationFindingRow rowView = _visibleRows[index];
                if (rowView.Row.Action != ValidationRowAction.Decision || rowView.Row.Finding == null) continue;
                LiveEventCalendarFinding finding = rowView.Row.Finding;
                DecisionMenu menu = new DecisionMenu(finding, Services.Format, repair => ApplyDecision(finding, repair));
                rowView.SetDecisionMenu(menu.Element);
            }
        }

        /// <summary>Chọn một mục của "Quyết định… ▾": cùng ba việc với mọi lệnh sửa (một Undo group, một toast, tự kiểm lại).</summary>
        private void ApplyDecision(LiveEventCalendarFinding finding, LiveEventCalendarRepair repair)
        {
            string undoName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationDepthDecisionAppliedFormat,
                finding.TargetId, LiveOpsFindingText.PlainText(LiveOpsFindingText.RepairOptionText(finding, repair, Services.Format)));
            ApplyEdit(repair.Edit, undoName);
        }

        private void OpenIgnorePopover(LiveEventCalendarFinding finding)
        {
            if (finding == null) return;
            IgnoreWarningPopover popover = new IgnoreWarningPopover(finding, Services.Format, Services.LayoutLoader,
                Services.Session.AssetFileName, warning => ApplyIgnoreWarning(finding, warning));
            LiveOpsPopoverContent.ShowSingle(ActivatorBoundsOf(RowOf(finding)), popover);
        }

        /// <summary>Bỏ qua = ghi một <see cref="IgnoredCalendarWarning"/> vào asset lịch ([SD2 §2.7]) — hoàn tác được như mọi lệnh sửa.</summary>
        private void ApplyIgnoreWarning(LiveEventCalendarFinding finding, IgnoredCalendarWarning warning)
        {
            string undoName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationDepthIgnoreUndoFormat,
                finding.RuleId, finding.TargetId);
            ApplyEdit(new AddIgnoredWarningEdit(warning), undoName);
        }

        private void RefreshEmptyState(ValidationViewModel model)
        {
            if (_empty == null) return;
            switch (model.BodyState)
            {
                case ValidationBodyState.NeverChecked:
                    FillEmpty(LiveOpsHubStrings.ValidationNeverCheckedTitle,
                        string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationNeverCheckedBodyFormat,
                            Services.Session.Check.RuleCount),
                        string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationNeverCheckedActionFormat,
                            CheckAllKeyLabel()), OnRecheckClicked);
                    break;
                case ValidationBodyState.NoErrors:
                    FillEmpty(LiveOpsHubStrings.ValidationNoErrorsTitle,
                        string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationNoErrorsBodyFormat,
                            PassedCountOf(model), NotMeasuredCountOf(model)), string.Empty, null);
                    break;
            }
        }

        private static string CheckAllKeyLabel()
        {
            string label = LiveOpsHubKeyLabels.For(LiveOpsHubShortcuts.CheckAllId);
            return label.Length > 0 ? label : FallbackCheckAllKeyLabel;
        }

        /// <summary>Người dùng gỡ phím F5 khỏi lệnh: nút vẫn phải có chữ, và F5 là nhãn mặc định trong tài liệu.</summary>
        private const string FallbackCheckAllKeyLabel = "F5";

        private static int PassedCountOf(ValidationViewModel model)
        {
            for (int index = 0; index < model.Groups.Count; index++)
            {
                if (model.Groups[index].Kind == ValidationGroupKind.Passed) return model.Groups[index].TotalCount;
            }
            return 0;
        }

        private static int NotMeasuredCountOf(ValidationViewModel model)
        {
            for (int index = 0; index < model.Groups.Count; index++)
            {
                if (model.Groups[index].Kind == ValidationGroupKind.NotMeasured) return model.Groups[index].TotalCount;
            }
            return 0;
        }

        // ------------------------------------------------------------------------------------------------------------ thao tác

        private void OnRecheckClicked()
        {
            if (Services.Session.Asset == null) return;
            Services.Session.StartCheck();
            Services.Bus.InvalidateHealth();
            Refresh();
        }

        /// <summary>
        /// Nút section header MỞ card xem trước ([SD2 §2.5]) — kể cả khi chỉ có một thay đổi: "Sửa" áp ngay là động từ của
        /// HÀNG, còn nút hàng loạt luôn cho soát danh sách trước. Bấm lần nữa khi card đang mở = đóng card (nút là công tắc).
        /// </summary>
        private void OnSafeRepairClicked()
        {
            if (_bulkPreview != null)
            {
                CloseBulkPreview();
                return;
            }
            IReadOnlyList<SafeRepairPreviewItem> pending = PendingSafeRepairs();
            if (pending.Count == 0) return;
            OpenBulkPreview(pending);
        }

        /// <summary>
        /// Mọi lệnh sửa AN TOÀN đang chờ, theo thứ tự hàng của báo cáo. "Đề xuất…" không bao giờ lọt vào đây ([SD2 §2.4]):
        /// nó đổi điều người chơi thấy nên phải hỏi riêng từng cái, kể cả khi có mười đề xuất giống nhau.
        /// </summary>
        private IReadOnlyList<SafeRepairPreviewItem> PendingSafeRepairs()
        {
            var items = new List<SafeRepairPreviewItem>();
            LiveEventCalendarCheckReport report = Services.Session.Check.LastReport;
            if (report == null) return items;
            for (int index = 0; index < report.Findings.Count; index++)
            {
                LiveEventCalendarFinding finding = report.Findings[index];
                if (finding.IsIgnored || finding.RepairKind != LiveEventCalendarRepairKind.SafeRepair) continue;
                if (finding.Repairs.Count == 0) continue;
                items.Add(new SafeRepairPreviewItem(finding, finding.Repairs[0]));
            }
            return items;
        }

        private void OpenBulkPreview(IReadOnlyList<SafeRepairPreviewItem> items)
        {
            if (_bulkPreviewHost == null) return;
            _bulkPreview = new SafeRepairPreviewCard(items, Services.Format);
            _bulkPreview.ApplyRequested += ApplyBulkRepair;
            _bulkPreview.CancelRequested += CloseBulkPreview;
            _bulkPreview.SelectedCountChanged += selectedCount => RefreshHeaderActions(null);
            _bulkPreviewKeys = KeysOf(items);
            _bulkPreviewHost.Add(_bulkPreview);
            RefreshHeaderActions(null);
        }

        private void CloseBulkPreview()
        {
            if (_bulkPreview != null) _bulkPreview.RemoveFromHierarchy();
            _bulkPreview = null;
            _bulkPreviewKeys = string.Empty;
            Refresh();
        }

        /// <summary>
        /// Card đang mở mà báo cáo đã đổi (kiểm lại, sửa chỗ khác, Undo) thì danh sách trong card là danh sách của bản cũ —
        /// bấm Áp sẽ áp lệnh sửa cho một lịch không còn tồn tại. Đóng card là cách duy nhất không nói dối.
        /// </summary>
        private void RefreshBulkPreview()
        {
            if (_bulkPreview == null) return;
            if (string.Equals(_bulkPreviewKeys, KeysOf(PendingSafeRepairs()), StringComparison.Ordinal)) return;
            _bulkPreview.RemoveFromHierarchy();
            _bulkPreview = null;
            _bulkPreviewKeys = string.Empty;
        }

        private static string KeysOf(IReadOnlyList<SafeRepairPreviewItem> items)
        {
            var keys = new List<string>(items.Count);
            for (int index = 0; index < items.Count; index++)
            {
                keys.Add(items[index].Finding.Fingerprint + KeyPartSeparator + items[index].Repair.RepairId);
            }
            return string.Join(KeySeparator, keys.ToArray());
        }

        private const string KeyPartSeparator = "#";
        private const string KeySeparator = "|";

        /// <summary>Nhiều lệnh sửa an toàn = MỘT lệnh ghép = MỘT Undo group "LiveOps: Sửa nhanh n lỗi" ([SD2 §2.5]).</summary>
        private void ApplyBulkRepair(IReadOnlyList<SafeRepairPreviewItem> items)
        {
            if (items.Count == 0) return;
            var edits = new List<LiveEventCalendarEdit>(items.Count);
            for (int index = 0; index < items.Count; index++)
            {
                edits.Add(items[index].Repair.Edit);
            }

            string undoName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationSafeRepairUndoFormat, items.Count);
            if (_bulkPreview != null) _bulkPreview.RemoveFromHierarchy();
            _bulkPreview = null;
            _bulkPreviewKeys = string.Empty;
            ApplyEdit(new CompositeCalendarEdit(edits), undoName);
        }

        /// <summary>Một lệnh sửa = một Undo group + một toast + tự kiểm lại (PD-10): ba việc luôn đi cùng nhau.</summary>
        private void ApplyRepair(LiveEventCalendarFinding finding, LiveEventCalendarRepair repair, string undoName)
        {
            ApplyEdit(repair.Edit, undoName);
        }

        /// <summary>
        /// Cùng ba việc đó cho MỌI lệnh sửa lịch của màn — sửa an toàn, đề xuất, sửa hàng loạt, bỏ qua cảnh báo, bỏ bỏ qua.
        /// Một lối ra duy nhất: chỗ nào quên toast hoặc quên kiểm lại thì kết quả cũ sẽ trông như kết quả mới.
        /// </summary>
        private void ApplyEdit(LiveEventCalendarEdit edit, string undoName)
        {
            LiveOpsHubEditOutcome outcome = Services.Session.Apply(edit, undoName);
            if (outcome.Applied)
            {
                Services.Bus.ShowToast(LiveOpsToastModel.ForEdit(undoName, outcome.UndoGroup));
                Services.Bus.InvalidateHealth();
                Services.Session.StartCheck();
            }
            Refresh();
        }

        private void OnRowAction(ValidationRow row)
        {
            switch (row.Action)
            {
                case ValidationRowAction.SafeRepair:
                    if (row.Finding.Repairs.Count > 0)
                    {
                        ApplyRepair(row.Finding, row.Finding.Repairs[0],
                            string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationSafeRepairUndoFormat, 1));
                    }
                    break;
                case ValidationRowAction.Proposal:
                    OpenProposalPopover(row.Finding, 0);
                    break;
                case ValidationRowAction.ViewDiff:
                    // (V-13) "Xem diff" của bản remote lệch đi thẳng sang Xuất JSON với bản so là remote.
                    Navigate(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Export).WithCompareSource(LiveOpsHubCompareSource.Remote));
                    break;
                case ValidationRowAction.DeclareType:
                    Navigate(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.EventTypes).WithEventType(row.Finding.TargetId, true));
                    break;
                case ValidationRowAction.PasteRunningJson:
                    Services.Actions.PasteRunningJson(ActivatorBoundsOf(row));
                    break;
                case ValidationRowAction.CopyError:
                    Services.Clipboard.Text = LiveOpsFindingText.PlainText(row.Headline + RuleErrorSeparator + row.MetaText);
                    break;
                case ValidationRowAction.IgnoreWarning:
                    OpenIgnorePopover(row.Finding);
                    break;
                case ValidationRowAction.Unignore:
                    Unignore(row.IgnoredWarning);
                    break;
            }
        }

        private const string RuleErrorSeparator = " · ";

        private Rect ActivatorBoundsOf(ValidationRow row)
        {
            for (int index = 0; index < _visibleRows.Count; index++)
            {
                if (ReferenceEquals(_visibleRows[index].Row, row)) return _visibleRows[index].worldBound;
            }
            return _root != null ? _root.worldBound : default(Rect);
        }

        /// <summary>
        /// "Bỏ bỏ qua" (V-15): một Undo group "Bỏ bỏ qua &lt;luật&gt; · &lt;đích&gt;" + toast, KHÔNG hỏi lại (bảng 7.0 — việc
        /// hoàn tác được trong một cú Hoàn tác thì hỏi chỉ làm chậm), rồi tự kiểm lại (PD-10) để phát hiện quay lại nhóm Nên xem.
        /// </summary>
        private void Unignore(IgnoredCalendarWarning warning)
        {
            if (warning == null) return;
            string undoName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationDepthUnignoreUndoFormat,
                warning.RuleId, warning.TargetId);
            ApplyEdit(new RemoveIgnoredWarningEdit(warning), undoName);
        }

        private void OnRowCopyDescription(ValidationRow row)
        {
            Services.Clipboard.Text = LiveOpsFindingText.PlainText(
                row.Headline + RuleErrorSeparator + row.MetaText + RuleErrorSeparator + row.RuleIdLine);
        }

        private void OnRowOpenRuleDocumentation(ValidationRow row)
        {
            if (row.Finding == null) return;
            Application.OpenURL(LiveOpsHubPaths.RuleDocumentationUrl(row.Finding.RuleId));
        }

        /// <summary>
        /// "Xem ghi chú" (V-15): hover card GHIM trên chính hàng đó. Thẻ chỉ có khoảng + hạn + ghi chú — asset KHÔNG lưu ai bỏ
        /// qua và lúc nào, nên thêm hai dòng đó là bịa ra một thứ chỉ git mới trả lời được.
        /// </summary>
        private void ShowIgnoredNoteCard(VisualElement rowElement, ValidationRow row)
        {
            if (_root == null || row.IgnoredWarning == null) return;
            if (_hoverCardHost == null) _hoverCardHost = new LiveOpsHoverCardHost(_root);

            VisualElement content = new VisualElement { name = LiveOpsHubPaths.ValidationDepthElementNames.IgnoredNoteCard };
            content.AddToClassList(LiveOpsHubClassNames.ValidationIgnoredNoteCard);
            Label title = new Label(string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ValidationDepthIgnoredNoteCardTitleFormat, row.MetaText)) { enableRichText = true };
            title.AddToClassList(LiveOpsHubClassNames.ValidationDetailTitle);
            content.Add(title);
            Label note = new Label(row.Headline) { enableRichText = true };
            note.AddToClassList(LiveOpsHubClassNames.ValidationIgnoredNote);
            content.Add(note);
            _hoverCardHost.ShowPinned(rowElement, content);
        }

        private void OnRowLink(ValidationRow row)
        {
            LiveEventCalendarFinding finding = row.Finding;
            if (finding == null) return;
            if (finding.TargetKind == LiveEventCalendarTargetKind.RecurringRule)
            {
                Navigate(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.RecurringRules).WithEventType(finding.TargetId, true));
                return;
            }
            if (finding.TargetKind == LiveEventCalendarTargetKind.EventType)
            {
                Navigate(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Calendar).WithEventType(finding.TargetId, true));
                return;
            }
            Navigate(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Calendar).WithEntry(finding.TargetEntryKey));
        }

        private void OnDetailRepairRequested(LiveEventCalendarFinding finding, int repairIndex)
        {
            if (finding.RepairKind == LiveEventCalendarRepairKind.SafeRepair && repairIndex < finding.Repairs.Count)
            {
                ApplyRepair(finding, finding.Repairs[repairIndex],
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationSafeRepairUndoFormat, 1));
                return;
            }
            // "Đề xuất…" đổi điều người chơi thấy: nút ở pane Chi tiết mở ĐÚNG popover của hàng, chọn sẵn lựa chọn vừa bấm.
            OpenProposalPopover(finding, repairIndex);
        }

        private void OpenProposalPopover(LiveEventCalendarFinding finding, int repairIndex)
        {
            if (finding == null || finding.Repairs.Count == 0) return;
            ProposalPopover popover = new ProposalPopover(finding, Services.Format, Services.LayoutLoader,
                repair => QuickCheckRemainingCount(finding, repair), repair => ApplyProposal(finding, repair), repairIndex);
            LiveOpsPopoverContent.ShowSingle(ActivatorBoundsOf(RowOf(finding)), popover);
        }

        private ValidationRow RowOf(LiveEventCalendarFinding finding)
        {
            for (int index = 0; index < _visibleRows.Count; index++)
            {
                if (ReferenceEquals(_visibleRows[index].Row.Finding, finding)) return _visibleRows[index].Row;
            }
            return null;
        }

        private void ApplyProposal(LiveEventCalendarFinding finding, LiveEventCalendarRepair repair)
        {
            string undoName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationProposalAppliedFormat,
                finding.TargetId, LiveOpsFindingText.PlainText(LiveOpsFindingText.RepairOptionText(finding, repair, Services.Format)));
            ApplyRepair(finding, repair, undoName);
        }

        /// <summary>
        /// Kiểm nhanh CHỈ làn của đợt đang sửa ([SD2 §2.6]) trên một nháp xem trước — không đụng lịch thật và không làm màn
        /// Kiểm lịch chuyển sang Ok. Trả SỐ vấn đề còn lại (âm = lệnh sửa không áp được lên nháp nên không kiểm được gì);
        /// popover dựng câu và chọn màu dấu từ con số này.
        /// </summary>
        private int QuickCheckRemainingCount(LiveEventCalendarFinding finding, LiveEventCalendarRepair repair)
        {
            LiveEventCalendarDocument preview;
            if (!LiveEventCalendarEdits.TryApply(Services.Session.Document, repair.Edit, out preview)) return NotMeasuredQuickCheck;
            string eventType = new ValidationTextContext(preview, Services.Clock.UtcNow, string.Empty).EventTypeOf(finding);
            LiveEventCalendarCheckReport report = Services.Session.CheckLane(eventType, preview);
            int remaining = 0;
            for (int index = 0; index < report.Findings.Count; index++)
            {
                if (!report.Findings[index].IsIgnored) remaining++;
            }
            return remaining;
        }

        private const int NotMeasuredQuickCheck = -1;

        private void SelectRow(ValidationRow row)
        {
            _selectedKey = row != null ? row.SelectionKey : string.Empty;
            ApplySelection();
            if (IsDrawerWidth()) OpenDrawer();
        }

        private void ApplySelection()
        {
            for (int index = 0; index < _cards.Count; index++)
            {
                _cards[index].SetSelectedRow(_selectedKey);
            }
            LiveEventCalendarFinding finding = SelectedFinding();
            _detail.Show(finding, Services.Format, Services.Session.Document);
        }

        private LiveEventCalendarFinding SelectedFinding()
        {
            for (int index = 0; index < _visibleRows.Count; index++)
            {
                if (string.Equals(_visibleRows[index].Row.SelectionKey, _selectedKey, StringComparison.Ordinal)) return _visibleRows[index].Row.Finding;
            }
            return null;
        }

        private int IndexOfSelected()
        {
            for (int index = 0; index < _visibleRows.Count; index++)
            {
                if (string.Equals(_visibleRows[index].Row.SelectionKey, _selectedKey, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        // ------------------------------------------------------------------------------------------------------------- toolbar

        private void BuildTypeMenu()
        {
            if (_typeMenuHost == null) return;
            _typeMenu = new ToolbarMenu { text = LiveOpsHubStrings.ValidationTypeMenuAll };
            _typeMenuHost.Add(_typeMenu);
        }

        private void BuildSearch()
        {
            if (_search == null) return;
            // Icon search 10px bên trong ô ([SD2 §2.1]) — ô tìm không có nhãn nên icon là thứ duy nhất nói ô này để làm gì.
            Image icon = LiveOpsHubIcons.CreateImage(SearchIconName, SearchIconSize);
            icon.AddToClassList(LiveOpsHubClassNames.ValidationSearchIcon);
            _search.Add(icon);
            // 2022.3 không có `placeholderText` ([API §12]): nhãn mờ là một Label phủ lên ô, ẩn khi có chữ.
            _searchPlaceholder = new Label(LiveOpsHubStrings.ValidationSearchPlaceholder);
            _searchPlaceholder.AddToClassList(LiveOpsHubClassNames.Placeholder);
            _search.Add(_searchPlaceholder);
            _searchPlaceholder.EnableInClassList(LiveOpsHubClassNames.PlaceholderHidden, _search.value.Length > 0);
            _search.RegisterValueChangedCallback(changeEvent =>
            {
                _searchPlaceholder.EnableInClassList(LiveOpsHubClassNames.PlaceholderHidden, changeEvent.newValue.Length > 0);
                _filter = _filter.WithSearch(changeEvent.newValue);
                Refresh();
            });
        }

        private const string SearchIconName = "Search Icon";
        private const int SearchIconSize = 10;

        /// <summary>
        /// Ô tìm phải nói đúng bộ lọc đang chạy: chữ tìm sống trong trạng thái view (qua domain reload, 7.0) và điều hướng từ
        /// rail/palette nhét id luật vào bộ lọc (7.5). Không đổ ngược ra ô thì người dùng thấy danh sách đã lọc mà ô trống.
        /// <c>SetValueWithoutNotify</c> để không bắn lại vòng lọc → Refresh → gán giá trị.
        /// </summary>
        private void RefreshSearch()
        {
            if (_search == null) return;
            if (!string.Equals(_search.value, _filter.SearchText, StringComparison.Ordinal)) _search.SetValueWithoutNotify(_filter.SearchText);
            if (_searchPlaceholder != null) _searchPlaceholder.EnableInClassList(LiveOpsHubClassNames.PlaceholderHidden, _filter.SearchText.Length > 0);
        }

        private void BuildProgress()
        {
            if (_progress == null) return;
            _spinner = new LiveOpsSpinner();
            _progress.Add(_spinner);
            _progressLabel = new Label();
            _progressLabel.AddToClassList(LiveOpsHubClassNames.ValidationProgressLabel);
            _progress.Add(_progressLabel);
            VisualElement track = new VisualElement();
            track.AddToClassList(LiveOpsHubClassNames.ValidationProgressTrack);
            _progressFill = new VisualElement();
            _progressFill.AddToClassList(LiveOpsHubClassNames.ValidationProgressFill);
            track.Add(_progressFill);
            _progress.Add(track);
            // Thanh trơn chỉ đọc được bằng bề rộng; nhãn "7 / 12" là con số đọc được cạnh nó ([SD2 §2.8]).
            _progressRatioLabel = new Label { name = ProgressRatioElementName };
            _progressRatioLabel.AddToClassList(LiveOpsHubClassNames.ValidationProgressLabel);
            _progress.Add(_progressRatioLabel);
        }

        private void BuildEmpty()
        {
            if (_empty == null) return;
            _emptyTitle = new Label { name = EmptyTitleElementName };
            _emptyTitle.AddToClassList(LiveOpsHubClassNames.EmptyTitle);
            _empty.Add(_emptyTitle);
            _emptyBody = new Label { name = EmptyBodyElementName };
            _emptyBody.AddToClassList(LiveOpsHubClassNames.EmptyBody);
            _empty.Add(_emptyBody);
            _emptyAction = new Button { name = EmptyActionElementName };
            _emptyAction.AddToClassList(LiveOpsHubClassNames.Button);
            _emptyAction.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            _empty.Add(_emptyAction);
        }

        private void FillEmpty(string title, string body, string actionText, Action action)
        {
            if (_empty == null) return;
            _emptyTitle.text = title;
            _emptyBody.text = body;
            _emptyAction.text = actionText;
            SetVisible(_emptyAction, actionText.Length > 0);
            _emptyAction.clickable = action == null ? null : new Clickable(() => action());
        }

        private void OnTabSelected(int index)
        {
            ValidationViewModel model = Services.Session.Asset != null
                ? ValidationViewModel.Build(Services.Session.Check, _filter, Services.Format, BuildTextContext())
                : null;
            if (model == null || index < 0 || index >= model.Tabs.Count) return;
            _filter = _filter.WithConsequence(model.Tabs[index].Filter);
            Refresh();
        }

        private void SetEventTypeFilter(string eventType)
        {
            _filter = _filter.WithEventType(eventType);
            Refresh();
        }

        // -------------------------------------------------------------------------------------------------- drawer và vòng đời

        /// <summary>
        /// Drawer bật theo bề rộng CỬA SỔ, không theo bề rộng thân màn ([SD2 §2.1]: "dưới 1100px (<c>liveops-hub--medium</c>)").
        /// Thân màn luôn hẹp hơn cửa sổ đúng bằng rail 196px + lề, nên đo chính nó thì ở cửa sổ 1280 (thân 1084) màn cũng tưởng
        /// mình đang hẹp và giấu luôn pane Chi tiết 300px. <see cref="LiveOpsHubBreakpoints"/> đã gắn class trên root hub — đọc
        /// lại class đó là cách duy nhất khớp với USS của khung.
        /// </summary>
        private bool IsDrawerWidth()
        {
            VisualElement hubRoot = FindHubRoot();
            if (hubRoot != null) return hubRoot.ClassListContains(LiveOpsHubClassNames.Medium);
            // Không nằm trong khung hub (test dựng section trần): bề rộng thân là số đo duy nhất còn lại.
            return _root != null && _root.resolvedStyle.width > 0f && _root.resolvedStyle.width < LiveOpsHubBreakpoints.MediumBelowWidth;
        }

        private VisualElement FindHubRoot()
        {
            for (VisualElement element = _root; element != null; element = element.parent)
            {
                if (element.ClassListContains(LiveOpsHubClassNames.Root)) return element;
            }
            return null;
        }

        private void OpenDrawer()
        {
            _isDrawerOpen = true;
            _detail.SetDrawer(true);
            SetVisible(_detail, true);
        }

        private void CloseDrawer()
        {
            _isDrawerOpen = false;
            SetVisible(_detail, !IsDrawerWidth());
        }

        private void OnGeometryChanged(GeometryChangedEvent geometryEvent)
        {
            bool isDrawer = IsDrawerWidth();
            _detail.SetDrawer(isDrawer);
            SetVisible(_detail, !isDrawer || _isDrawerOpen);
        }

        private void OnNavigationCancel(NavigationCancelEvent cancelEvent)
        {
            if (!_isDrawerOpen) return;
            cancelEvent.StopPropagation();
            CloseDrawer();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent detachEvent)
        {
            Services.Session.DocumentChanged -= Refresh;
            Services.Session.CheckChanged -= Refresh;
            if (_spinner != null) _spinner.Stop();
            _hoverCardHost = null;
            _root = null;
        }

        private void Navigate(LiveOpsHubNavigation navigation)
        {
            Services.Bus.Navigate(navigation);
        }

        private void OpenOverview()
        {
            if (_host != null) _host.Navigate(LiveOpsHubSections.Ids.Overview);
            else Navigate(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Overview));
        }

        private static bool IsKnownFilter(string value)
        {
            return string.Equals(value, LiveOpsHubNavigation.FilterNone, StringComparison.Ordinal)
                || string.Equals(value, LiveOpsHubNavigation.FilterDropped, StringComparison.Ordinal)
                || string.Equals(value, LiveOpsHubNavigation.FilterProgressLost, StringComparison.Ordinal)
                || string.Equals(value, LiveOpsHubNavigation.FilterShouldReview, StringComparison.Ordinal)
                || string.Equals(value, LiveOpsHubNavigation.FilterNotMeasured, StringComparison.Ordinal);
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element == null) return;
            element.EnableInClassList(LiveOpsHubClassNames.ValidationHidden, !visible);
        }

        [Serializable]
        private sealed class ViewState
        {
            public string consequenceFilter;
            public string eventType;
            public string searchText;
            public string selectedKey;
        }
    }
}
