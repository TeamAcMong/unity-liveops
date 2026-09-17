using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Màn <c>export</c> ở tầng Xuất (mục 7.6, [SD2 §3]): hub tạo đúng chuỗi dán vào key <c>liveops_calendar</c>, đọc lại
    /// chuỗi đó bằng parser của game, và cho thấy khác gì bản đã đăng.
    /// <para>
    /// Ba nút theo bước — Copy JSON hoặc Lưu file… → người dùng tự dán lên Firebase → Đánh dấu đã đăng… — và điều kiện của
    /// cả ba do <see cref="ExportGateModel"/> (G-EXPORTGATE, V-9) quyết, không phải màn: Tổng quan đọc cùng cổng đó nên hai
    /// nơi không bao giờ nói khác nhau. Màn chỉ vẽ lại state, nối việc thật và giữ trạng thái view.
    /// </para>
    /// <para>
    /// Lý do nút bị khoá LUÔN in thành chữ cạnh nút qua <see cref="LiveOpsButtonSlot"/> (SPIKE-B SP-3, quyết định an toàn
    /// 16/9/2026); tooltip trên slot chỉ là phụ và không test nào assert nó.
    /// </para>
    /// </summary>
    internal sealed class ExportSection : IHubSection, IHubHostAware, IHubSectionActions, IHubSectionViewState, IHubSectionNavigation
    {
        internal const string BodyElementName = "export-body";
        internal const string EmptyElementName = "export-empty";
        internal const string EmptyTitleElementName = "export-empty-title";
        internal const string EmptyBodyElementName = "export-empty-body";
        internal const string EmptyActionElementName = "export-empty-action";
        internal const string DefaultBodyElementName = "export-default";
        internal const string NoticesElementName = "export-notices";
        internal const string GateHostElementName = "export-gate-host";
        internal const string MetricsHostElementName = "export-metrics-host";
        internal const string JsonCardElementName = "export-json-card";
        internal const string JsonTitleElementName = "export-json-title";
        internal const string JsonMetaElementName = "export-json-meta";
        internal const string JsonHostElementName = "export-json-host";
        internal const string DiffHostElementName = "export-diff-host";
        internal const string HistoryHostElementName = "export-history-host";
        internal const string FooterNoteElementName = "export-footer-note";

        internal const string CopyButtonElementName = "export-copy";
        internal const string SaveFileButtonElementName = "export-save-file";
        internal const string MarkPublishedButtonElementName = "export-mark-published";
        internal const string Format1NoticeElementName = "export-format1-notice";
        internal const string RestoringNoticeElementName = "export-restoring-note";
        internal const string UndoRestoreButtonElementName = "export-undo-restore";
        internal const string FailureCardElementName = "export-failure-card";
        internal const string FailureCopyButtonElementName = "export-failure-copy";
        internal const string FailureOpenLineButtonElementName = "export-failure-open-line";

        private const string CopyIconName = "Clipboard";
        private const string SaveIconName = "SaveAs";
        private const int HeaderIconSize = 16;
        private const string ClockPattern = "HH:mm";

        /// <summary>
        /// Id nút của outcome (c′) "vừa lưu file"; đối số là đường dẫn file. Internal vì <c>LiveOpsHubWindow</c> đọc id này để
        /// biết nhãn nút và để gọi <c>ILiveOpsHubFileDialog.Reveal</c> — khung là nơi duy nhất chạm tới port hệ điều hành,
        /// màn chỉ dựng bản ghi (mục 12 I-11).
        /// </summary>
        internal const string RevealFileActionId = "reveal-file";

        private static readonly IReadOnlyList<string> ElementNames = Array.AsReadOnly(new[]
        {
            EmptyElementName, DefaultBodyElementName, NoticesElementName, GateHostElementName, MetricsHostElementName,
            JsonHostElementName, DiffHostElementName, HistoryHostElementName, FooterNoteElementName,
        });

        private readonly ExportGateCard _gateCard = new ExportGateCard();
        private readonly ExportDiffCard _diffCard = new ExportDiffCard();
        private readonly ExportHistoryCard _historyCard = new ExportHistoryCard();
        private readonly LiveOpsJsonView _jsonView = new LiveOpsJsonView();

        private ExportMetrics _metrics;
        private ExportGateInput _gateInput;
        private IHubHost _host;
        private VisualElement _root;
        private VisualElement _empty;
        private VisualElement _defaultBody;
        private VisualElement _notices;
        private Label _jsonMeta;
        private LiveOpsButtonSlot _copySlot;
        private LiveOpsButtonSlot _saveFileSlot;
        private LiveOpsButtonSlot _markPublishedSlot;
        private ExportSessionState _viewState = new ExportSessionState();
        private bool _isSubscribed;

        public ExportSection(LiveOpsHubServices services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services), LiveOpsHubStrings.ExportErrorServicesMissing);
            // Nối MỘT LẦN ở đây, không trong CreateView: khung gọi CreateView mỗi lần mở lại màn, mà card sống suốt đời
            // section — nối lại mỗi lượt thì vào-ra-vào ba lần là một cú bấm chạy ba lần.
            _gateCard.RowActivated += OnGateRowActivated;
            _diffCard.ReviewToggled += OnReviewToggled;
            _diffCard.FixRequested += OnFixRequested;
            _diffCard.BackToPublishedRequested += OnBackToPublishedRequested;
            _historyCard.CompareRequested += OnCompareWithStampRequested;
            _historyCard.RestoreRequested += OnRestoreRequested;
            _historyCard.RemoveStampRequested += OnRemoveStampRequested;
            _jsonView.ModeTabs.SelectedIndexChanged += OnJsonModeChanged;
        }

        /// <summary>Services của cửa sổ (G-SESSION) — phiên lịch, đồng hồ, clipboard, hộp file, bus.</summary>
        internal LiveOpsHubServices Services { get; }

        public string Id => LiveOpsHubSections.Ids.Export;
        public string Title => LiveOpsHubStrings.ShellExportTitle;
        public string Subtitle => LiveOpsHubStrings.ShellExportSubtitle;
        public PipelineStage Stage => PipelineStage.Export;
        public IReadOnlyList<string> RequiredElementNames => ElementNames;

        internal ExportGateCard GateCard => _gateCard;
        internal ExportDiffCard DiffCard => _diffCard;
        internal ExportHistoryCard HistoryCard => _historyCard;
        internal LiveOpsJsonView JsonView => _jsonView;
        internal ExportMetrics Metrics => _metrics;
        internal LiveOpsButtonSlot CopySlot => _copySlot;
        internal LiveOpsButtonSlot SaveFileSlot => _saveFileSlot;
        internal LiveOpsButtonSlot MarkPublishedSlot => _markPublishedSlot;

        /// <summary>Meta của card "JSON sẽ đăng" ("parser của game giữ 4/6 mục · khớp Kiểm lịch") — test đọc thẳng.</summary>
        internal Label JsonMetaLabel => _jsonMeta;

        /// <summary>Model diff đang vẽ — test đọc thẳng thay vì suy ngược từ cây element.</summary>
        internal ExportDiffViewModel DiffModel { get; private set; }

        /// <summary>Cổng xuất đang vẽ (có thêm trạng thái khôi phục của màn, khác bản phiên cache cho health).</summary>
        internal ExportGateState Gate { get; private set; }

        public SectionHealth GetHealth()
        {
            return LiveOpsHubFindingRouting.ForSection(Id, Services);
        }

        public VisualElement CreateView()
        {
            VisualTreeAsset layout = Services.LayoutLoader.LoadVisualTree(LiveOpsHubPaths.ExportSectionUxml);
            if (layout == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.ExportMissingLayoutFormat, LiveOpsHubPaths.ExportSectionUxml));
            }
            _root = layout.Instantiate();
            _root.name = BodyElementName;
            StyleSheet sheet = Services.LayoutLoader.LoadStyleSheet(LiveOpsHubPaths.ExportSectionUss);
            if (sheet != null) _root.styleSheets.Add(sheet);

            _empty = _root.Q(EmptyElementName);
            _defaultBody = _root.Q(DefaultBodyElementName);
            _notices = _root.Q(NoticesElementName);
            _root.Q<Label>(EmptyTitleElementName).text = LiveOpsHubStrings.ExportNoAssetTitle;
            _root.Q<Label>(EmptyBodyElementName).text = LiveOpsHubStrings.ExportNoAssetBody;
            Button emptyAction = _root.Q<Button>(EmptyActionElementName);
            emptyAction.text = LiveOpsHubStrings.ExportNoAssetActionButton;
            emptyAction.clicked += OnOpenOverviewClicked;

            _root.Q<Label>(JsonTitleElementName).text = LiveOpsHubStrings.ExportJsonCardTitle;
            _jsonMeta = _root.Q<Label>(JsonMetaElementName);
            _root.Q<Label>(FooterNoteElementName).text = LiveOpsHubStrings.ExportFooterNote;

            _root.Q(GateHostElementName).Add(_gateCard);
            _metrics = new ExportMetrics(Services.Format);
            _metrics.FormatSelected += OnFormatSelected;
            _root.Q(MetricsHostElementName).Add(_metrics);
            _root.Q(JsonHostElementName).Add(_jsonView);
            _root.Q(DiffHostElementName).Add(_diffCard);
            _root.Q(HistoryHostElementName).Add(_historyCard);
            _jsonView.IsSingleLine = _viewState.JsonSingleLine;

            Subscribe();
            _root.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            Refresh();
            return _root;
        }

        public void OnShown()
        {
            // "Tự chạy khi mở màn" (7.6): kiểm cũ hay chưa kiểm thì chạy ngay, spinner nằm trong dòng cổng 3 — không khoá cửa sổ.
            LiveOpsHubCheckState check = Services.Session.Check;
            if (Services.Session.Asset != null && !check.IsRunning && (check.LastReport == null || check.IsStale)) Services.Session.StartCheck();
            Refresh();
        }

        public void Bind(IHubHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>Ba nút của section header [SD2 §3.2]; nút chính đổi theo bước, lý do khoá in thành chữ cạnh nút.</summary>
        public void PopulateHeaderActions(VisualElement container)
        {
            _copySlot = BuildHeaderButton(container, CopyButtonElementName, LiveOpsHubStrings.ExportCopyButton, CopyIconName, OnCopyClicked);
            _saveFileSlot = BuildHeaderButton(container, SaveFileButtonElementName, LiveOpsHubStrings.ExportSaveFileButton, SaveIconName,
                OnSaveFileClicked);
            _markPublishedSlot = BuildHeaderButton(container, MarkPublishedButtonElementName, LiveOpsHubStrings.ExportMarkPublishedButton,
                string.Empty, OnMarkPublishedClicked);
            RefreshHeaderButtons();
        }

        public string CaptureViewState()
        {
            _viewState.JsonSingleLine = _jsonView.IsSingleLine;
            return _viewState.ToJson();
        }

        public void RestoreViewState(string viewStateJson)
        {
            // Chuỗi rỗng = cửa sổ CHƯA có trạng thái đã lưu cho màn này, không phải "hãy xoá trạng thái đang có". Khung gọi
            // RestoreViewState("") ngay sau CreateView, nên thay nguyên _viewState ở đó sẽ xoá dấu vừa khôi phục (trạng thái
            // (i) mất note + nút "Hoàn tác khôi phục") của thao tác chạy trước khi cửa sổ dựng.
            if (string.IsNullOrEmpty(viewStateJson)) return;
            _viewState = ExportSessionState.FromJson(viewStateJson);
            _jsonView.IsSingleLine = _viewState.JsonSingleLine;
            Refresh();
        }

        /// <summary>(V-13) Điều hướng có tham số: "Xem diff" của Kiểm lịch mở màn này với nguồn bản so là bản remote đã dán.</summary>
        public void ApplyNavigation(LiveOpsHubNavigation navigation)
        {
            if (navigation == null || navigation.CompareSource == LiveOpsHubCompareSource.None) return;
            Services.Session.Publish.SelectCompareSource(navigation.CompareSource);
            Refresh();
        }

        // ------------------------------------------------------------------------------------------------------ dựng lại

        internal void Refresh()
        {
            if (_root == null) return;
            LiveOpsHubCalendarSession session = Services.Session;
            bool hasAsset = session.Asset != null;
            _empty.EnableInClassList(LiveOpsHubClassNames.ExportHidden, hasAsset);
            _defaultBody.EnableInClassList(LiveOpsHubClassNames.ExportHidden, !hasAsset);
            if (!hasAsset)
            {
                Gate = null;
                DiffModel = null;
                RefreshHeaderButtons();
                return;
            }

            Gate = EvaluateGate();
            // (mục 12 I-3) Nút "Dán JSON đang chạy…" chỉ dùng được khi action thật đã dựng — card phải biết để khoá nút kèm lý
            // do thay vì vẽ nút bật rơi vào no-op (Tổng quan đã đọc cùng hai thuộc tính này).
            _gateCard.Bind(Gate, Services.Actions.CanPasteRunningJson, Services.Actions.PasteRunningJsonUnavailableReason);
            RefreshNotices(Gate);
            RefreshJson(Gate);
            RefreshDiff();
            _historyCard.Bind(ExportHistoryModel.Build(session.Document, session.Publish.ActiveStamp, session.Remote, Services.Format));
            _metrics.Bind(session.Publish.CurrentJson, session.Publish.CompareDiff, session.Publish.ActiveStamp);
            RefreshHeaderButtons();
        }

        /// <summary>
        /// Cổng xuất của MÀN: đầu vào dựng từ phiên (cùng hàm với health nên không lệch) rồi thêm trạng thái khôi phục (i) —
        /// thứ chỉ màn biết, vì "đang khôi phục" là một nhánh của view chứ không phải của lịch.
        /// </summary>
        private ExportGateState EvaluateGate()
        {
            ExportGateInput input = Services.Session.BuildExportGateInput(Services.JsonReadBack, Services.Format);
            PublishedCalendarStamp restored = FindStamp(_viewState.RestoredStampKey);
            if (restored != null) input = input.WithRestoring(restored, CanUndoRestore());
            _gateInput = input;
            return ExportGateModel.Evaluate(input);
        }

        private void RefreshNotices(ExportGateState gate)
        {
            _notices.Clear();
            if (gate.ReadBackFailure != null) _notices.Add(BuildFailureCard(gate.ReadBackFailure));
            if (gate.RestoringNoticeText.Length > 0) _notices.Add(BuildRestoringNote(gate));
            if (gate.Format1NoticeText.Length > 0)
            {
                _notices.Add(new HelpBox(gate.Format1NoticeText, HelpBoxMessageType.Warning) { name = Format1NoticeElementName });
            }
        }

        private VisualElement BuildFailureCard(ExportGateReadBackFailure failure)
        {
            VisualElement card = new VisualElement { name = FailureCardElementName };
            card.AddToClassList(LiveOpsHubClassNames.Card);
            card.AddToClassList(LiveOpsHubClassNames.ExportFailureCard);

            Label symptom = new Label(failure.SymptomText);
            symptom.AddToClassList(LiveOpsHubClassNames.ExportFailureSymptom);
            card.Add(symptom);

            // Message của runtime in NGUYÊN VĂN bằng mono: dev cần đúng chuỗi parser ném ra, không phải bản diễn giải.
            Label message = new Label(failure.RuntimeMessage);
            message.AddToClassList(LiveOpsHubClassNames.ExportFailureMessage);
            message.AddToClassList(LiveOpsHubClassNames.Mono);
            card.Add(message);

            Label position = new Label(failure.PositionText);
            position.AddToClassList(LiveOpsHubClassNames.ExportFailurePosition);
            card.Add(position);

            VisualElement actions = new VisualElement();
            actions.AddToClassList(LiveOpsHubClassNames.ExportFailureActions);
            Button copyError = new Button(() => OnCopyErrorClicked(failure))
            {
                name = FailureCopyButtonElementName,
                text = failure.CopyErrorButtonText,
            };
            copyError.AddToClassList(LiveOpsHubClassNames.Button);
            actions.Add(copyError);
            if (failure.OpenLineButtonText.Length > 0)
            {
                Button openLine = new Button(() => _jsonView.ScrollToLine(failure.ErrorLine))
                {
                    name = FailureOpenLineButtonElementName,
                    text = failure.OpenLineButtonText,
                };
                openLine.AddToClassList(LiveOpsHubClassNames.Button);
                actions.Add(openLine);
            }
            card.Add(actions);

            Label note = new Label(failure.NoteText);
            note.AddToClassList(LiveOpsHubClassNames.Note);
            card.Add(note);
            return card;
        }

        private VisualElement BuildRestoringNote(ExportGateState gate)
        {
            VisualElement note = new VisualElement { name = RestoringNoticeElementName };
            note.AddToClassList(LiveOpsHubClassNames.ExportRestoringNote);
            Label text = new Label(gate.RestoringNoticeText);
            text.AddToClassList(LiveOpsHubClassNames.Note);
            note.Add(text);
            Button undo = new Button(OnUndoRestoreClicked) { name = UndoRestoreButtonElementName, text = gate.UndoRestoreText };
            undo.AddToClassList(LiveOpsHubClassNames.Button);
            undo.SetEnabled(gate.CanUndoRestore);
            note.Add(undo);
            return note;
        }

        private void RefreshJson(ExportGateState gate)
        {
            LiveEventCalendarJsonText json = Services.Session.Publish.CurrentJson;
            _jsonView.SetText(json.Text, LiveEventCalendarJsonWriter.WriteSingleLine(json));
            _jsonView.SetAnnotations(BuildAnnotations(json));
            _jsonView.SetLineChanges(BuildLineChanges(json));

            ExportGateRow readBackRow = gate.Row(ExportGateRowKind.ParserReadBack);
            if (readBackRow == null)
            {
                _jsonMeta.text = string.Empty;
                return;
            }
            // Câu này xưng "parser CỦA GAME giữ x/y mục" nên số phải của bản ĐỌC LẠI, không phải của nháp: định dạng 1 bỏ luật
            // lặp (nháp 8/8 nhưng parser chỉ thấy 4/6) và mọi ca parser đọc khác nháp đều làm hai số lệch nhau.
            LiveEventCalendarCompilation compilation = ReadBackCompilation();
            _jsonMeta.text = readBackRow.State == ExportGateRowState.Ok && compilation != null
                ? LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ExportJsonCardMetaFormat),
                    Services.Format.Integer(compilation.KeptCount), Services.Format.Integer(compilation.EntryCount))
                : readBackRow.Text;
        }

        /// <summary>Bộ biên dịch của bản đọc lại (dãy mục game thật thấy); null khi chưa dựng được đầu vào cổng.</summary>
        private LiveEventCalendarCompilation ReadBackCompilation()
        {
            return _gateInput == null || _gateInput.ReadBack == null ? null : _gateInput.ReadBack.Compilation;
        }

        /// <summary>
        /// Chú thích cuối dòng JSON [SD2 §3.6]: phát hiện của Kiểm lịch (câu từ <see cref="LiveOpsFindingText"/>, V-8) và
        /// thay đổi so với bản so (câu từ <see cref="LiveOpsChangeText"/>). Dòng nào không tìm được mục trong JSON thì bỏ —
        /// chú thích treo ở dòng sai còn tệ hơn không có.
        /// </summary>
        private IReadOnlyList<LiveOpsJsonLineAnnotation> BuildAnnotations(LiveEventCalendarJsonText json)
        {
            List<LiveOpsJsonLineAnnotation> annotations = new List<LiveOpsJsonLineAnnotation>();
            LiveEventCalendarDiffResult diff = Services.Session.Publish.CompareDiff;
            List<int> usedLines = new List<int>();
            LiveEventCalendarCheckReport report = Services.Session.Check.LastReport;
            if (report != null)
            {
                foreach (LiveEventCalendarFinding finding in report.Findings)
                {
                    if (finding.IsIgnored || finding.IsAboutRemoteSnapshot) continue;
                    int line;
                    if (!TryFindFindingLine(json, diff, finding, out line)) continue;
                    annotations.Add(new LiveOpsJsonLineAnnotation(line, LiveOpsHubFindingRouting.StateOf(finding.Consequence),
                        LiveOpsFindingText.ShortLabel(finding)));
                    usedLines.Add(line);
                }
            }

            LiveOpsChangeTextContext context = BuildChangeTextContext(diff);
            foreach (LiveEventCalendarChange change in diff.Changes)
            {
                if (change.Kind != LiveEventCalendarChangeKind.Changed) continue;
                int line;
                if (!TryFindChangeLine(json, change, out line)) continue;
                // Một dòng chỉ mang MỘT chú thích [SD2 §3.6]: phát hiện của Kiểm lịch thắng câu diff — dòng đã có ✕/! nói hậu
                // quả nặng hơn "trước → sau".
                if (usedLines.Contains(line)) continue;
                annotations.Add(new LiveOpsJsonLineAnnotation(line, LiveOpsHubFindingRouting.StateOf(change.Consequence),
                    LiveOpsChangeText.ChangedTooltip(change, context, Services.Format)));
            }
            return annotations;
        }

        /// <summary>Lề ký hiệu: <c>+</c> cho MỌI dòng của mục thêm mới, <c>~</c> cho dòng field đổi so với bản so.</summary>
        private IReadOnlyList<LiveOpsJsonLineChange> BuildLineChanges(LiveEventCalendarJsonText json)
        {
            List<LiveOpsJsonLineChange> changes = new List<LiveOpsJsonLineChange>();
            foreach (LiveEventCalendarChange change in Services.Session.Publish.CompareDiff.Changes)
            {
                if (change.Kind == LiveEventCalendarChangeKind.Added)
                {
                    int firstLine;
                    int lastLine;
                    if (!json.TryGetItemLineRange(change.ItemKind, ItemKeyOf(change), out firstLine, out lastLine)) continue;
                    for (int line = firstLine; line <= lastLine; line++)
                    {
                        changes.Add(new LiveOpsJsonLineChange(line, LiveOpsJsonLineChangeKind.Added));
                    }
                    continue;
                }
                if (change.Kind != LiveEventCalendarChangeKind.Changed) continue;
                foreach (LiveEventCalendarFieldChange field in change.Fields)
                {
                    int fieldLine;
                    if (json.TryGetFieldLine(change.ItemKind, ItemKeyOf(change), field.FieldName, out fieldLine))
                    {
                        changes.Add(new LiveOpsJsonLineChange(fieldLine, LiveOpsJsonLineChangeKind.Modified));
                    }
                }
            }
            return changes;
        }

        private static string ItemKeyOf(LiveEventCalendarChange change)
        {
            return change.EntryKey.Length > 0 ? change.EntryKey : change.ItemId;
        }

        private static bool TryFindChangeLine(LiveEventCalendarJsonText json, LiveEventCalendarChange change, out int line)
        {
            foreach (LiveEventCalendarFieldChange field in change.Fields)
            {
                if (json.TryGetFieldLine(change.ItemKind, ItemKeyOf(change), field.FieldName, out line)) return true;
            }
            int lastLine;
            return json.TryGetItemLineRange(change.ItemKind, ItemKeyOf(change), out line, out lastLine);
        }

        /// <summary>
        /// Dòng mang chú thích của một phát hiện: ưu tiên dòng FIELD, chỉ lùi về dòng mở mục khi không tìm được. Thiết kế đặt
        /// "!" + câu ở dòng 7 <c>"idPrefix"</c>, không phải ở dòng 4 <c>{</c> — dòng mở mục không nói được phát hiện về cái gì.
        /// <para>
        /// Phát hiện KHÔNG khai tên field (<c>LiveEventCalendarFinding</c> chỉ có đích là mục), nên field lấy từ thay đổi của
        /// CÙNG mục trong diff: hai thứ nói về một chỗ (đổi tiền tố của luật đang chạy là thay đổi <c>idPrefix</c>).
        /// </para>
        /// </summary>
        private static bool TryFindFindingLine(LiveEventCalendarJsonText json, LiveEventCalendarDiffResult diff,
            LiveEventCalendarFinding finding, out int line)
        {
            line = 0;
            string itemKey = finding.TargetEntryKey.Length > 0 ? finding.TargetEntryKey : finding.TargetId;
            if (itemKey.Length == 0) return false;
            LiveEventCalendarItemKind kind = finding.TargetKind == LiveEventCalendarTargetKind.RecurringRule
                ? LiveEventCalendarItemKind.RecurringRule
                : LiveEventCalendarItemKind.FixedEvent;
            if (diff != null)
            {
                foreach (LiveEventCalendarChange change in diff.Changes)
                {
                    if (change.Kind != LiveEventCalendarChangeKind.Changed) continue;
                    if (!string.Equals(ItemKeyOf(change), itemKey, StringComparison.Ordinal)) continue;
                    foreach (LiveEventCalendarFieldChange field in change.Fields)
                    {
                        if (json.TryGetFieldLine(change.ItemKind, itemKey, field.FieldName, out line)) return true;
                    }
                }
            }
            int lastLine;
            return json.TryGetItemLineRange(kind, itemKey, out line, out lastLine);
        }

        private void RefreshDiff()
        {
            LiveOpsHubPublishState publish = Services.Session.Publish;
            LiveEventCalendarDiffResult diff = publish.CompareDiff;
            LiveOpsHubCompareSource source = publish.ActiveCompareSource;
            // (g) lần đăng đầu: chưa có dấu nào để so nên KHÔNG có danh sách thay đổi — câu empty của cổng thay cho danh sách.
            // Vẽ 8 mục "thêm mới" ở đây là nói dối rằng có một bản so ([SD2 §3.8] mục Empty).
            bool hasBaseline = source == LiveOpsHubCompareSource.Remote || publish.ActiveStamp != null;
            DiffModel = ExportDiffViewModel.Build(diff, BuildChangeTextContext(diff), Services.Format, source, BuildDiffHeaderText(source),
                Gate == null ? string.Empty : Gate.DiffEmptyText, publish.IsReviewed, hasBaseline);
            _diffCard.Bind(DiffModel, source == LiveOpsHubCompareSource.Remote);
        }

        private string BuildDiffHeaderText(LiveOpsHubCompareSource source)
        {
            LiveOpsHubCalendarSession session = Services.Session;
            if (source == LiveOpsHubCompareSource.Remote)
            {
                DateTime? verified = session.Remote.VerifiedUtc;
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportDiffCardTitleRemoteFormat,
                    verified.HasValue ? Services.Format.ShortDateTime(verified.Value) : string.Empty,
                    MarkPublishedModel.ShortSha(session.Remote.Sha256Hex));
            }
            PublishedCalendarStamp stamp = session.Publish.ActiveStamp;
            if (stamp == null) return LiveOpsHubStrings.ExportDiffCardTitleNoStamp;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportDiffCardTitleFormat, StampTimeText(stamp), stamp.ShortSha);
        }

        private LiveOpsChangeTextContext BuildChangeTextContext(LiveEventCalendarDiffResult diff)
        {
            LiveOpsHubCalendarSession session = Services.Session;
            // Ngữ cảnh dựng từ ĐÚNG hai tài liệu vừa so + diff đã có (V-22 CC-FT-1) — không so lại lần nữa.
            return new LiveOpsChangeTextContext(session.Publish.CompareDocument, session.Document, session.Clock.UtcNow, diff, false);
        }

        private void RefreshHeaderButtons()
        {
            if (_copySlot == null) return;
            ExportGateState gate = Gate;
            if (gate == null)
            {
                // Chưa có asset: ba nút khoá với đúng câu của trạng thái đó, không phải câu của cổng.
                ApplyNoAssetButton(_copySlot, true);
                ApplyNoAssetButton(_saveFileSlot, false);
                ApplyNoAssetButton(_markPublishedSlot, false);
                return;
            }

            // [SD2 §3.2] in lý do cạnh ĐÚNG MỘT nút — nút mà câu đó nói tới. Nút chính đang khoá thì câu là của nó ((a) "Chặn:
            // 2 đợt bị bỏ" cạnh Copy, (f) "Không có thay đổi để ghi" cạnh Đánh dấu). Nút chính đang MỞ thì câu nói về bước sau:
            // (b) "Còn thiếu cho Đánh dấu: copy hoặc lưu file bản này" — nút đang khoá lúc đó là "Đánh dấu đã đăng…".
            bool reasonOnCopy = !gate.Copy.IsEnabled && (gate.Copy.IsPrimary || gate.MarkPublished.IsEnabled);
            ApplyButton(_copySlot, gate.Copy, gate, reasonOnCopy);
            ApplyButton(_saveFileSlot, gate.SaveFile, gate, false);
            ApplyButton(_markPublishedSlot, gate.MarkPublished, gate, !reasonOnCopy);
        }

        /// <summary>Chưa có asset: chỉ nút đầu in lý do thành chữ, hai nút kia mang tooltip — cùng luật "một câu" của [SD2 §3.2].</summary>
        private static void ApplyNoAssetButton(LiveOpsButtonSlot slot, bool showsText)
        {
            slot.SetEnabledWithReason(false, LiveOpsHubStrings.ServicesHealthNoAsset);
            slot.ReasonLabel.EnableInClassList(LiveOpsHubClassNames.ExportReasonTooltipOnly, !showsText);
            slot.ReasonLabel.EnableInClassList(LiveOpsHubClassNames.TextBlocked, showsText);
            slot.ReasonLabel.EnableInClassList(LiveOpsHubClassNames.ExportReasonQuiet, false);
        }

        /// <summary>
        /// Lý do của cổng in thành chữ cạnh đúng một nút; hai nút còn lại chỉ mang tooltip (in cả ba làm chữ chồng lên nhau
        /// trên header). Mức chữ theo <see cref="ExportGateState.ReasonIsBlocked"/>: (b) và (f) là chữ quiet, không phải chặn.
        /// </summary>
        private static void ApplyButton(LiveOpsButtonSlot slot, ExportGateButton button, ExportGateState gate, bool showsGateReason)
        {
            // Nút đang MỞ không in lý do: SetEnabledWithReason xoá chữ khi bật, nên in ở đó là in vào chỗ không hiện.
            bool showsText = showsGateReason && gate.ReasonText.Length > 0 && !button.IsEnabled;
            // Slot vẫn nhận một câu khi nút khoá (hợp đồng "disabled luôn kèm lý do" + tooltip trên slot, R-16); chỉ NHÃN bị ẩn.
            slot.SetEnabledWithReason(button.IsEnabled, showsText ? gate.ReasonText : button.Tooltip);
            slot.ReasonLabel.EnableInClassList(LiveOpsHubClassNames.ExportReasonTooltipOnly, !showsText);
            slot.ReasonLabel.EnableInClassList(LiveOpsHubClassNames.TextBlocked, showsText && gate.ReasonIsBlocked);
            slot.ReasonLabel.EnableInClassList(LiveOpsHubClassNames.ExportReasonQuiet, showsText && !gate.ReasonIsBlocked);
            slot.Button.tooltip = button.Tooltip;
            slot.Button.EnableInClassList(LiveOpsHubClassNames.ButtonPrimary, button.IsPrimary);
        }

        /// <summary>
        /// Nút header: Button LÀ TextElement nên <c>text</c> do chính nút vẽ trên cả hộp nội dung — Image con chỉ CHỒNG lên
        /// chữ ("Co[icon] JSON"). Nút bỏ <c>text</c>, xếp Image rồi Label thành hàng.
        /// </summary>
        private LiveOpsButtonSlot BuildHeaderButton(VisualElement container, string elementName, string text, string iconName, Action clicked)
        {
            Button button = new Button(clicked) { name = elementName };
            button.AddToClassList(LiveOpsHubClassNames.Button);
            button.AddToClassList(LiveOpsHubClassNames.ExportIconButton);
            if (iconName.Length > 0)
            {
                Image icon = LiveOpsHubIcons.CreateImage(iconName, HeaderIconSize);
                icon.AddToClassList(LiveOpsHubClassNames.ExportIconButtonIcon);
                button.Add(icon);
            }
            Label label = new Label(text);
            label.AddToClassList(LiveOpsHubClassNames.ExportIconButtonLabel);
            button.Add(label);

            LiveOpsButtonSlot slot = new LiveOpsButtonSlot(button);
            // Nút sống trong section header của shell, còn ExportSection.uss chỉ nạp lên thân màn (cây khác) — không nạp thêm
            // ở đây thì class icon/lý do của nút không có style (cùng lý do đã ghi ở EventTypesSection.PopulateHeaderActions).
            StyleSheet sheet = Services.LayoutLoader.LoadStyleSheet(LiveOpsHubPaths.ExportSectionUss);
            if (sheet != null) slot.styleSheets.Add(sheet);
            container.Add(slot);
            return slot;
        }

        // ---------------------------------------------------------------------------------------------------------- việc

        private void OnCopyClicked()
        {
            CopyJson();
        }

        /// <summary>Lệnh "Copy JSON" có tên: nút header và kịch bản chụp đi cùng một đường, không ai gọi lại phần thân bằng tay.</summary>
        internal void CopyJson()
        {
            LiveEventCalendarJsonText json = CopyCurrentJsonToClipboard();
            DateTime nowUtc = Services.Session.Clock.UtcNow;
            Services.Bus.ShowOutcome(LiveOpsOutcomeRecord.Ok(
                LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ExportOutcomeCopiedHeadlineFormat),
                    Services.Format.Integer(json.ByteCount), nowUtc.ToString(ClockPattern, CultureInfo.InvariantCulture), json.ShortSha),
                LiveOpsHubStrings.ExportOutcomeCopiedDetail, nowUtc));
            Services.Bus.InvalidateHealth();
            Refresh();
        }

        /// <summary>Copy đúng chuỗi sẽ dán lên Firebase và ghi <c>lastExportedSha</c> — dùng chung với nút copy trong hộp ghi dấu.</summary>
        private LiveEventCalendarJsonText CopyCurrentJsonToClipboard()
        {
            LiveEventCalendarJsonText json = Services.Session.Publish.CurrentJson;
            Services.Clipboard.Text = json.Text;
            Services.Session.Publish.RecordExport(json, false);
            return json;
        }

        private void OnSaveFileClicked()
        {
            SaveJsonToFile();
        }

        /// <summary>Lệnh "Lưu file…" có tên: nút header và kịch bản chụp (c′) đi cùng một đường, không ai dựng outcome bằng tay.</summary>
        internal void SaveJsonToFile()
        {
            LiveEventCalendarJsonText json = Services.Session.Publish.CurrentJson;
            string defaultName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubPaths.ExportFileNameFormat, json.ShortSha);
            string path = Services.FileDialog.SaveFile(LiveOpsHubStrings.ExportSaveFilePanelTitle, ExportDirectory(), defaultName,
                LiveOpsHubPaths.ExportFileExtension);
            if (string.IsNullOrEmpty(path)) return;

            // Ghi BYTE của đúng chuỗi xuất: File.WriteAllText thêm BOM hoặc đổi ký tự xuống dòng theo máy và sha sẽ lệch.
            File.WriteAllBytes(path, json.GetUtf8Bytes());
            string savedDirectory = Path.GetDirectoryName(path) ?? string.Empty;
            if (savedDirectory.Length > 0) EditorPrefs.SetString(LiveOpsHubPaths.ExportDirectoryPreferenceKey, savedDirectory);
            Services.Session.Publish.RecordExport(json, true);

            Services.Bus.ShowOutcome(LiveOpsOutcomeRecord.Ok(
                LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ExportOutcomeSavedHeadlineFormat),
                    Path.GetFileName(path), Services.Format.Integer(json.ByteCount), savedDirectory),
                LiveOpsHubStrings.ExportOutcomeSavedDetail, Services.Session.Clock.UtcNow, RevealFileActionId, path));
            Services.Bus.InvalidateHealth();
            Refresh();
        }

        /// <summary>Thư mục lần trước (EditorPrefs) hoặc <c>~/Documents/LiveOps</c>; tạo nếu thiếu [SD2 §3.10] (c′).</summary>
        private static string ExportDirectory()
        {
            string stored = EditorPrefs.GetString(LiveOpsHubPaths.ExportDirectoryPreferenceKey, string.Empty);
            if (stored.Length > 0 && Directory.Exists(stored)) return stored;
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrEmpty(documents)) return string.Empty;
            string directory = Path.Combine(documents, LiveOpsHubPaths.ExportDefaultDirectoryName);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            return directory;
        }

        private void OnMarkPublishedClicked()
        {
            MarkPublishedWindow window = MarkPublishedWindow.Show(BuildMarkPublishedInput(), CopyFromMarkPublishedWindow);
            if (window.Result != MarkPublishedResult.Confirmed) return;
            MarkPublished(window.Note);
        }

        /// <summary>Nút "Copy JSON &lt;sha mới&gt;" trong hộp: đi ĐÚNG đường Copy của màn rồi trả input đã cập nhật.</summary>
        private MarkPublishedInput CopyFromMarkPublishedWindow()
        {
            LiveEventCalendarJsonText json = CopyCurrentJsonToClipboard();
            RefreshHeaderButtons();
            return BuildMarkPublishedInput().WithCopiedInsideDialog(json.Sha256Hex, Services.Session.Clock.UtcNow);
        }

        internal MarkPublishedInput BuildMarkPublishedInput()
        {
            LiveOpsHubCalendarSession session = Services.Session;
            LiveEventCalendarJsonText json = session.Publish.CurrentJson;
            return new MarkPublishedInput(json.Sha256Hex, session.Publish.LastExportedSha256Hex, session.Publish.LastExportedUtc,
                session.Clock.UtcNow, json.ByteCount, session.AssetFileName, Services.PublisherIdentity.PublisherName,
                Services.PublisherIdentity.SourceLabel, Services.Format);
        }

        /// <summary>Ghi dấu đã đăng: một Undo group + lưu asset (<see cref="LiveOpsHubPublishState.MarkPublished"/>) rồi outcome (e).</summary>
        internal void MarkPublished(string note)
        {
            LiveOpsHubCalendarSession session = Services.Session;
            LiveEventCalendarJsonText json = session.Publish.CurrentJson;
            LiveOpsHubEditOutcome outcome = session.Publish.MarkPublished(note);
            if (!outcome.Applied)
            {
                Services.Bus.ShowOutcome(LiveOpsOutcomeRecord.Blocked(outcome.FailureText, string.Empty, session.Clock.UtcNow));
                Refresh();
                return;
            }

            Services.Bus.ShowOutcome(LiveOpsOutcomeRecord.Ok(
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportOutcomeMarkedHeadlineFormat,
                    Services.Format.ShortDateTime(session.Clock.UtcNow), json.ShortSha, Services.PublisherIdentity.PublisherName),
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportOutcomeMarkedDetailFormat, session.AssetFileName),
                session.Clock.UtcNow));
            Services.Bus.InvalidateHealth();
            Refresh();
        }

        private void OnCopyErrorClicked(ExportGateReadBackFailure failure)
        {
            Services.Clipboard.Text = failure.ErrorReportText;
        }

        private void OnFormatSelected(LiveEventCalendarJsonFormat format)
        {
            Services.Session.Publish.SelectedFormat = format;
            Refresh();
        }

        private void OnJsonModeChanged(int index)
        {
            _viewState.JsonSingleLine = index == LiveOpsJsonView.SingleLineTabIndex;
        }

        private void OnGateRowActivated(ExportGateRow row)
        {
            switch (row.Action)
            {
                case ExportGateRowAction.StartCheck:
                    Services.Session.StartCheck();
                    Refresh();
                    return;
                case ExportGateRowAction.CopyReadBackError:
                    if (Gate != null) Services.Clipboard.Text = Gate.ReadBackErrorReportText;
                    return;
                case ExportGateRowAction.PasteRunningJson:
                    Services.Actions.PasteRunningJson(_gateCard.worldBound);
                    return;
                case ExportGateRowAction.CompareWithRemote:
                    Services.Session.Publish.SelectCompareSource(LiveOpsHubCompareSource.Remote);
                    Refresh();
                    return;
                case ExportGateRowAction.ShowPublishedDiff:
                    Services.Session.Publish.SelectCompareSource(LiveOpsHubCompareSource.Published);
                    Refresh();
                    return;
                default:
                    if (row.Navigation != null) Services.Bus.Navigate(row.Navigation);
                    return;
            }
        }

        private void OnReviewToggled(LiveEventCalendarChange change, bool reviewed)
        {
            Services.Session.Publish.SetReviewed(change, reviewed);
            Services.Bus.InvalidateHealth();
            Refresh();
        }

        private void OnFixRequested(LiveEventCalendarChange change)
        {
            Services.Bus.Navigate(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Validation)
                .WithFilter(LiveOpsHubNavigation.FilterDropped)
                .WithEntry(change == null ? string.Empty : change.EntryKey));
        }

        private void OnBackToPublishedRequested()
        {
            Services.Session.Publish.SelectCompareSource(LiveOpsHubCompareSource.Published);
            Refresh();
        }

        private void OnCompareWithStampRequested(PublishedCalendarStamp stamp)
        {
            if (stamp == null) return;
            // Dấu đang là bản so nằm trong SessionState của phiên (LiveOpsHubPublishState.ActiveStamp) — màn KHÔNG nhớ lại lần
            // hai: hai chỗ nhớ cùng một thứ là hai chỗ lệch nhau (cùng lý do đã ghi ở đầu ExportSessionState).
            Services.Session.Publish.SelectActiveStamp(stamp);
            Services.Session.Publish.SelectCompareSource(LiveOpsHubCompareSource.Published);
            Services.Bus.ShowToast(LiveOpsToastModel.Info(string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ExportHistoryCompareToastFormat, StampTimeText(stamp))));
            Refresh();
        }

        /// <summary>
        /// Khôi phục vào nháp (cấp 1): thay ĐỢT + LUẬT bằng bản chụp của dấu, GIỮ dấu đã đăng, cảnh báo đã bỏ qua và định
        /// nghĩa loại hiện tại (7.6) — bản chụp JSON không mang ba thứ đó, lấy của nó là xoá luôn lịch sử đăng.
        /// </summary>
        private void OnRestoreRequested(PublishedCalendarStamp stamp)
        {
            if (stamp == null) return;
            LiveOpsHubCalendarSession session = Services.Session;
            LiveEventCalendarDocument restored;
            if (!TryBuildRestoredDocument(stamp, session.Document, out restored)) return;

            LiveEventCalendarDiffResult diff = session.Publish.PublishedDiff;
            string body = diff.ChangeCount == 0
                ? LiveOpsHubStrings.ExportRestoreConfirmBodyNoChanges
                : LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ExportRestoreConfirmBodyFormat),
                    Services.Format.Integer(diff.ChangeCount), BuildChangedItemList(diff));
            LiveOpsConfirmRequest request = new LiveOpsConfirmRequest.Builder()
                .WithLevel(LiveOpsConfirmLevel.Level1)
                .WithTitle(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportRestoreConfirmTitleFormat, StampTimeText(stamp)))
                .WithBody(body)
                .WithKeyHint(LiveOpsHubStrings.ExportRestoreConfirmKeyHint)
                .WithButtons(LiveOpsHubStrings.ExportRestoreConfirmDestructive, LiveOpsHubStrings.ExportRestoreConfirmSafe)
                .Build();
            if (Services.Confirmation.Confirm(request) != LiveOpsConfirmResult.Destructive) return;

            string undoName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportRestoreUndoNameFormat, StampTimeText(stamp));
            LiveOpsHubEditOutcome outcome = session.Apply(new ReplaceDocumentEdit(restored), undoName);
            if (outcome.Applied)
            {
                _viewState.RestoredStampKey = ExportSessionState.KeyOf(stamp);
                _viewState.RestoreUndoGroup = outcome.UndoGroup;
                Services.Bus.ShowToast(LiveOpsToastModel.ForEdit(undoName, outcome.UndoGroup));
                Services.Bus.InvalidateHealth();
            }
            Refresh();
        }

        private bool TryBuildRestoredDocument(PublishedCalendarStamp stamp, LiveEventCalendarDocument current,
            out LiveEventCalendarDocument restored)
        {
            restored = null;
            LiveEventCalendarDocumentParseResult parsed = Services.JsonReadBack.ReadBackDocument(stamp.SnapshotJson);
            if (parsed == null || !parsed.IsReadable) return false;

            LiveEventCalendarDocumentBuilder builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(current.RemoteConfigKey);
            foreach (LiveEventTypeDefinition type in current.EventTypes) builder.WithEventType(type);
            foreach (RecurringLiveEventRule rule in parsed.Document.RecurringRules) builder.WithRecurringRule(rule);
            foreach (FixedLiveEventEntry entry in parsed.Document.FixedEvents) builder.WithFixedEvent(entry);
            foreach (PublishedCalendarStamp existing in current.PublishedStamps) builder.WithPublishedStamp(existing);
            foreach (IgnoredCalendarWarning warning in current.IgnoredWarnings) builder.WithIgnoredWarning(warning);
            restored = builder.Build();
            return true;
        }

        private void OnRemoveStampRequested(PublishedCalendarStamp stamp)
        {
            LiveOpsHubCalendarSession session = Services.Session;
            PublishedCalendarStamp latest = session.Document.LatestStamp;
            // Chỉ lần MỚI NHẤT gỡ được: gỡ dấu giữa danh sách làm lịch sử nói dối thứ tự đăng.
            if (stamp == null || !ReferenceEquals(stamp, latest)) return;

            PublishedCalendarStamp previous = PreviousStamp();
            int changeCount = session.Publish.PublishedDiff.ChangeCount;
            string body = previous == null
                ? LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ExportRemoveStampConfirmBodyNoBaselineFormat),
                    Services.Format.Integer(changeCount))
                : LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ExportRemoveStampConfirmBodyFormat),
                    StampTimeText(previous), previous.ShortSha, Services.Format.Integer(changeCount));
            LiveOpsConfirmRequest request = new LiveOpsConfirmRequest.Builder()
                .WithLevel(LiveOpsConfirmLevel.Level1)
                .WithTitle(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportRemoveStampConfirmTitleFormat,
                    StampTimeText(stamp)))
                .WithBody(body)
                .WithButtons(LiveOpsHubStrings.ExportRemoveStampConfirmDestructive, LiveOpsHubStrings.ExportRemoveStampConfirmSafe)
                .Build();
            if (Services.Confirmation.Confirm(request) != LiveOpsConfirmResult.Destructive) return;

            LiveOpsHubEditOutcome outcome = session.Publish.RemoveLatestStamp();
            if (outcome.Applied)
            {
                string toast = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportRemoveStampToastFormat, StampTimeText(stamp));
                Services.Bus.ShowToast(LiveOpsToastModel.ForEdit(toast, outcome.UndoGroup));
                Services.Bus.InvalidateHealth();
            }
            Refresh();
        }

        private void OnUndoRestoreClicked()
        {
            if (!CanUndoRestore()) return;
            Undo.PerformUndo();
            _viewState.RestoredStampKey = string.Empty;
            _viewState.RestoreUndoGroup = LiveOpsHubEditOutcome.NoUndoGroup;
            Services.Bus.InvalidateHealth();
            Refresh();
        }

        private void OnOpenOverviewClicked()
        {
            if (_host != null) _host.Navigate(LiveOpsHubSections.Ids.Overview);
        }

        /// <summary>"Hoàn tác khôi phục" chỉ bật khi bước khôi phục còn ở ĐỈNH ngăn Undo — không thì nó sẽ hoàn tác việc khác.</summary>
        private bool CanUndoRestore()
        {
            return _viewState.RestoreUndoGroup != LiveOpsHubEditOutcome.NoUndoGroup
                && Undo.GetCurrentGroup() == _viewState.RestoreUndoGroup + 1;
        }

        private static string BuildChangedItemList(LiveEventCalendarDiffResult diff)
        {
            List<string> names = new List<string>();
            foreach (LiveEventCalendarChange change in diff.Changes)
            {
                if (change.Kind == LiveEventCalendarChangeKind.Kept) continue;
                if (names.Contains(change.ItemId)) continue;
                names.Add(change.ItemId);
            }
            return string.Join(LiveOpsHubStrings.ExportChangedItemSeparator, names.ToArray());
        }

        private PublishedCalendarStamp PreviousStamp()
        {
            IReadOnlyList<PublishedCalendarStamp> stamps = Services.Session.Document.PublishedStamps;
            return stamps.Count >= 2 ? stamps[stamps.Count - 2] : null;
        }

        private PublishedCalendarStamp FindStamp(string key)
        {
            if (key.Length == 0 || Services.Session.Document == null) return null;
            foreach (PublishedCalendarStamp stamp in Services.Session.Document.PublishedStamps)
            {
                if (string.Equals(ExportSessionState.KeyOf(stamp), key, StringComparison.Ordinal)) return stamp;
            }
            return null;
        }

        private string StampTimeText(PublishedCalendarStamp stamp)
        {
            DateTime publishedUtc;
            return stamp.TryGetPublishedUtc(out publishedUtc) ? Services.Format.ShortDateTime(publishedUtc) : stamp.PublishedUtcText;
        }

        private void Subscribe()
        {
            if (_isSubscribed) return;
            Services.Session.DocumentChanged += Refresh;
            Services.Session.CheckChanged += Refresh;
            _isSubscribed = true;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent detachEvent)
        {
            if (!_isSubscribed) return;
            Services.Session.DocumentChanged -= Refresh;
            Services.Session.CheckChanged -= Refresh;
            _isSubscribed = false;
        }
    }
}
