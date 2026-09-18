using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Cầu giữa control timeline (G-TIMELINE-VIEW, W3) và phiên lịch (G-SESSION, W3): dựng <see cref="LiveOpsTimelineInput"/> từ
    /// phiên, và đổi <see cref="LiveOpsTimelineIntent"/> thành <see cref="LiveEventCalendarEdit"/> + câu toast + mức xác nhận theo
    /// <see cref="LiveOpsConfirmationPolicy"/>. Control không tự sửa tài liệu, presenter không tự dựng UI — hai bên gặp nhau đúng
    /// ở đây.
    /// <para>
    /// Thao tác kéo theo SPIKE-B SP-2 (quyết định an toàn 16/9/2026) + vá W8-UX (UX-06, UJ-11): hộp xác nhận CHỈ mở sau khi
    /// chuột đã nhả (qua <see cref="DeferConfirmation"/>, mặc định <c>EditorApplication.delayCall</c>), và lúc nó mở thì CHƯA có
    /// gì được ghi — nháp continuous edit còn mở, toast và <see cref="DocumentEdited"/> chờ câu trả lời. Chọn nút an toàn = huỷ
    /// nháp (<c>CancelContinuousEdit</c>), không để lại bước Undo nào để phải gỡ.
    /// </para>
    /// </summary>
    internal sealed class CalendarTimelinePresenter
    {
        private readonly LiveOpsHubServices _services;
        private readonly List<string> _hiddenLanes = new List<string>();
        private readonly List<string> _collapsedLanes = new List<string>();
        private readonly List<string> _selectedBarKeys = new List<string>();

        private LiveOpsTimelineModel _model;
        private CalendarCommandHandler _commandHandler;
        private string _selectedBarKey = string.Empty;
        private int _dragGroup = LiveOpsHubEditOutcome.NoUndoGroup;
        private string _dragBarKey = string.Empty;
        private LiveEventCalendarDocument _dragStartDocument;
        private FixedLiveEventEntry _dragStartEntry;
        private LiveEventCalendarCheckReport _previewLaneCheckReport;
        private string _previewQuickCheckText = string.Empty;
        private bool _isDragActive;

        /// <summary>(UX-06) Đang chờ người dùng trả lời hộp xác nhận của một lần kéo — nháp còn mở, chưa commit gì.</summary>
        private bool _isAwaitingDragConfirmation;

        /// <summary>
        /// (UX-14, UJ-13) Chữ ký các phát hiện đã có trên làn TRƯỚC khi bắt đầu kéo. Kiểm nhanh chỉ được nêu phát hiện MỚI sinh
        /// ra vì bước kéo này; nêu lỗi có sẵn của một đợt khác cùng làn làm người dùng tưởng mình vừa làm hỏng thứ đó.
        /// </summary>
        private readonly HashSet<string> _dragStartFindingSignatures = new HashSet<string>(StringComparer.Ordinal);

        public CalendarTimelinePresenter(LiveOpsHubServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            DeferConfirmation = DeferWithDelayCall;
        }

        /// <summary>Chọn đổi (bấm thanh, bấm chỗ trống, tìm theo id) — section cập nhật inspector theo đây.</summary>
        public event Action<string> SelectionChanged;

        /// <summary>
        /// (G-OPT-TIMELINE, Hình 12 khung 9) Tập chọn NHIỀU đợt đổi. Tách khỏi <see cref="SelectionChanged"/> vì nơi nghe câu
        /// đó (màn) trả lời bằng <c>timeline.Select(barKey)</c> — tức là thu tập về đúng một thanh, đúng thứ vừa bị phá.
        /// CHỈ phát khi tập có từ hai phần tử trở lên: thu tập về một đợt hay bỏ chọn đi đường <see cref="SelectionChanged"/>,
        /// và người nghe duy nhất (inspector) bỏ qua mọi tập nhỏ hơn hai. Tham số là BẢN SAO — danh sách nội bộ bị ghi đè ngay
        /// ở lệnh kế tiếp, nên trao thẳng nó ra ngoài là trao một thứ đổi sau lưng người nhận.
        /// </summary>
        public event Action<IReadOnlyList<string>> SelectionSetChanged;

        /// <summary>Yêu cầu điều hướng sang màn khác; section chuyển tiếp lên <see cref="LiveOpsHubSectionBus"/>.</summary>
        public event Action<LiveOpsHubNavigation> NavigationRequested;

        /// <summary>Toast cần hiện; section chuyển tiếp lên bus (test màn assert đúng ở đây, không cần panel).</summary>
        public event Action<LiveOpsToastModel> ToastRequested;

        /// <summary>Mở popover Thêm đợt tại làn + giờ đã biết (nhấp đúp chỗ trống).</summary>
        public event Action<string, DateTime, DateTime?> AddEventRequested;

        /// <summary>Bấm một dải gom (lặp hoặc cố định) = zoom vào khoảng của dải, không phải chọn đợt (V-22 CC-TLMODEL-1).</summary>
        public event Action<DateTime, DateTime> ZoomRequested;

        /// <summary>Tài liệu vừa đổi vì một ý định — section dựng lại view.</summary>
        public event Action DocumentEdited;

        /// <summary>
        /// (nợ D-3(a)) Câu tag kiểm nhanh của bước xem trước vừa chạy ("" = hết cử chỉ kéo) kèm trạng thái sức khoẻ để dấu 7px
        /// đi theo câu. Phát NGAY trong nhánh Preview, trước khi element vẽ lại readout, nên tag và giờ trên readout là của
        /// cùng một bước kéo (Hình 12 khung 5).
        /// </summary>
        public event Action<string, HealthState> QuickCheckTagChanged;

        /// <summary>
        /// (UX-05, UJ-05) Cử chỉ kéo bắt đầu (true) / kết thúc (false). Màn dùng để tắt hover card trong suốt cử chỉ: lúc kéo,
        /// thanh đang hover bị thay bằng element mới sau mỗi bước xem trước nên PointerLeave của nó không bao giờ tới.
        /// </summary>
        public event Action<bool> DragActiveChanged;

        /// <summary>Cách hoãn hộp xác nhận tới sau khi chuột nhả; test thay bằng chạy đồng bộ (SP-2 (a)).</summary>
        internal Action<Action> DeferConfirmation { get; set; }

        public string SelectedBarKey => _selectedBarKey;

        /// <summary>Cả tập đang chọn, theo thứ tự người dùng gom; một phần tử ở ca chọn thường.</summary>
        public IReadOnlyList<string> SelectedBarKeys => _selectedBarKeys;

        /// <summary>
        /// Bộ xử lý lệnh chiều sâu (W5) — nhân bản, copy/dán, ẩn/đưa làn, ⌘+kéo tạo. Presenter chỉ CHUYỂN TIẾP những loại ý định
        /// nó không tự xử lý: giữ luật "họ ý định là họ đóng" (V-10) mà không nhét cả màn vào một lớp.
        /// </summary>
        internal CalendarCommandHandler CommandHandler
        {
            get { return _commandHandler; }
            set { _commandHandler = value; }
        }

        /// <summary>Bề rộng cột nội dung — quyết định toast xoá dùng dạng đủ hay dạng ngắn (PD của 7.3).</summary>
        public float ContentWidth { get; set; }

        /// <summary>Model timeline vừa dựng; presenter tra thanh theo <c>BarKey</c> từ đây.</summary>
        public LiveOpsTimelineModel Model => _model;

        public IReadOnlyList<string> HiddenLanes => _hiddenLanes;

        /// <summary>(Hình 12 khung 12) Làn đang thu gọn — view state, không đụng JSON, không đụng sha.</summary>
        public IReadOnlyList<string> CollapsedLanes => _collapsedLanes;

        /// <summary>
        /// Làn ẩn do màn giữ qua domain reload; presenter là nơi duy nhất cầm danh sách này (trước đây màn giữ một bản thứ hai
        /// và chip "Đang ẩn n làn" đếm bản không ai đọc).
        /// </summary>
        public void SetHiddenLanes(IEnumerable<string> typeIds)
        {
            _hiddenLanes.Clear();
            if (typeIds == null) return;
            foreach (string typeId in typeIds)
            {
                if (!string.IsNullOrEmpty(typeId) && !_hiddenLanes.Contains(typeId)) _hiddenLanes.Add(typeId);
            }
        }

        /// <summary>Thu gọn hoặc mở một làn; trả false khi không đổi gì (mục menu bấm hai lần).</summary>
        public bool SetLaneCollapsed(string typeId, bool collapsed)
        {
            if (string.IsNullOrEmpty(typeId)) return false;
            bool wasCollapsed = _collapsedLanes.Contains(typeId);
            if (wasCollapsed == collapsed) return false;
            if (collapsed) _collapsedLanes.Add(typeId);
            else _collapsedLanes.Remove(typeId);
            return true;
        }

        public bool IsLaneCollapsed(string typeId)
        {
            return !string.IsNullOrEmpty(typeId) && _collapsedLanes.Contains(typeId);
        }

        /// <summary>
        /// Kết quả "kiểm nhanh làn này" của lần xem trước gần nhất (7.3: preview → <c>UpdateContinuousEdit</c> + <c>CheckLane</c>);
        /// <c>null</c> khi không có cử chỉ kéo nào đang mở. KHÔNG bao giờ ghi vào <see cref="LiveOpsHubCheckState"/> — kết quả một
        /// làn không được làm "mới" kết quả cả lịch (mục 2159 của kế hoạch).
        /// </summary>
        internal LiveEventCalendarCheckReport PreviewLaneCheckReport => _previewLaneCheckReport;

        /// <summary>Câu tag của kiểm nhanh khi đang kéo: "kiểm nhanh làn này: không chồng" hoặc headline của phát hiện Bị bỏ.</summary>
        internal string PreviewQuickCheckText => _previewQuickCheckText;

        /// <summary>Dựng đầu vào của model timeline từ phiên: nháp, bản biên dịch, báo cáo kiểm (có thể cũ), diff với bản đã đăng.</summary>
        public LiveOpsTimelineInput BuildInput(DateTime rangeStartUtc, DateTime rangeEndUtc, float trackWidth)
        {
            LiveOpsHubCalendarSession session = _services.Session;
            LiveOpsTimelineInput input = new LiveOpsTimelineInput()
                .WithDocument(session.Document ?? LiveEventCalendarDocument.Empty)
                .WithCompilation(session.Compilation)
                .WithCheckReport(session.Check == null ? null : session.Check.LastReport)
                .WithPublishedDiff(session.Publish == null ? null : session.Publish.PublishedDiff)
                .WithNowUtc(_services.Clock.UtcNow)
                .WithRange(rangeStartUtc, rangeEndUtc)
                .WithTrackWidth(trackWidth)
                .WithHiddenLanes(_hiddenLanes)
                .WithCollapsedLanes(_collapsedLanes);
            return input;
        }

        /// <summary>Dựng model và nhớ lại để tra thanh theo khoá — mọi ý định đi qua model này.</summary>
        public LiveOpsTimelineModel BuildModel(DateTime rangeStartUtc, DateTime rangeEndUtc, float trackWidth)
        {
            _model = BuildInput(rangeStartUtc, rangeEndUtc, trackWidth).Build();
            return _model;
        }

        public void SetSelectedBarKey(string barKey)
        {
            string next = barKey ?? string.Empty;
            bool wasMultiple = _selectedBarKeys.Count > 1;
            // Đang chọn nhiều thì PHẢI đi tiếp dù khoá mốc không đổi: inspector đang vẽ trạng thái (c) và chỉ câu
            // SelectionChanged dưới đây mới gọi nó về trạng thái một đợt.
            if (!wasMultiple && string.Equals(next, _selectedBarKey, StringComparison.Ordinal)) return;
            _selectedBarKey = next;
            _selectedBarKeys.Clear();
            if (next.Length > 0) _selectedBarKeys.Add(next);
            SelectionChanged?.Invoke(_selectedBarKey);
        }

        /// <summary>
        /// (Hình 12 khung 9) Đặt cả tập chọn. Tập từ hai phần tử trở lên chỉ phát <see cref="SelectionSetChanged"/> — phát
        /// <see cref="SelectionChanged"/> ở đây là tự tay bảo màn thu tập về một thanh. Tập nhỏ hơn hai rơi về
        /// <see cref="SetSelectedBarKey"/>, tức đi đường <see cref="SelectionChanged"/> như mọi lần chọn thường.
        /// </summary>
        public void SetSelectedBarKeys(IReadOnlyList<string> barKeys, string primaryBarKey)
        {
            var next = new List<string>();
            if (barKeys != null)
            {
                for (int index = 0; index < barKeys.Count; index++)
                {
                    string barKey = barKeys[index];
                    if (!string.IsNullOrEmpty(barKey) && !next.Contains(barKey)) next.Add(barKey);
                }
            }
            if (next.Count <= 1)
            {
                SetSelectedBarKey(next.Count == 1 ? next[0] : string.Empty);
                return;
            }
            string primary = primaryBarKey ?? string.Empty;
            if (!next.Contains(primary)) primary = next[next.Count - 1];
            _selectedBarKeys.Clear();
            _selectedBarKeys.AddRange(next);
            _selectedBarKey = primary;
            SelectionSetChanged?.Invoke(next.ToArray());
        }

        /// <summary>
        /// Họ ý định của timeline là họ ĐÓNG (V-10): mỗi loại ở đây hoặc được xử lý, hoặc bị bỏ qua CÓ CHỦ Ý kèm dấu INTERIM, để
        /// gói W5 đối chiếu được danh sách <see cref="LiveOpsTimelineIntent.KnownIntentTypeNames"/> với nhánh xử lý.
        /// </summary>
        public void HandleIntent(LiveOpsTimelineIntent intent)
        {
            if (intent == null) return;
            SelectBarIntent select = intent as SelectBarIntent;
            if (select != null)
            {
                HandleSelect(select);
                return;
            }
            SelectManyIntent selectMany = intent as SelectManyIntent;
            if (selectMany != null)
            {
                HandleSelectMany(selectMany);
                return;
            }
            ToggleLaneCollapsedIntent toggleLane = intent as ToggleLaneCollapsedIntent;
            if (toggleLane != null)
            {
                // Thu gọn làn là VIEW STATE: dựng lại view như khi ẩn làn (W5 nối LanesChanged vào cùng chỗ), không ghi tài liệu.
                if (SetLaneCollapsed(toggleLane.TypeId, toggleLane.Collapsed)) DocumentEdited?.Invoke();
                return;
            }
            MoveBarIntent move = intent as MoveBarIntent;
            if (move != null)
            {
                HandleMove(move);
                return;
            }
            AddAtTimeIntent add = intent as AddAtTimeIntent;
            if (add != null)
            {
                HandleAddAtTime(add);
                return;
            }
            DeleteBarIntent delete = intent as DeleteBarIntent;
            if (delete != null)
            {
                RequestDelete(delete.BarKey);
                return;
            }
            OpenRuleIntent openRule = intent as OpenRuleIntent;
            if (openRule != null)
            {
                if (openRule.EventType.Length > 0)
                {
                    NavigationRequested?.Invoke(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.RecurringRules)
                        .WithEventType(openRule.EventType, true));
                }
                return;
            }
            // Mọi loại còn lại đi qua CalendarCommandHandler (W5): nhân bản, copy/dán, ẩn/đưa làn, ⌘+kéo tạo, F8.
            // Hai luật CC-TLMODEL-2: MoveLaneIntent("") và CreateByDragIntent trên làn "" bỏ qua — làn chưa ghi loại không có
            // loại nào để dời hay để tạo đợt.
            _commandHandler?.HandleIntent(intent);
        }

        /// <summary>Chọn thanh; dải gom không ánh xạ ra đợt nên bấm dải là zoom vào (V-22 CC-TLMODEL-1).</summary>
        private void HandleSelect(SelectBarIntent intent)
        {
            LiveOpsTimelineBarModel bar = FindBar(intent.BarKey);
            if (bar != null && bar.IsStrip)
            {
                ZoomRequested?.Invoke(bar.StartUtc, bar.EndUtc);
                return;
            }
            SetSelectedBarKey(intent.BarKey);
        }

        /// <summary>(Hình 12 khung 9) Tập chọn từ ⌘-click / Shift-click / khung chọn; dải gom không ánh xạ ra đợt nên bị loại.</summary>
        private void HandleSelectMany(SelectManyIntent intent)
        {
            var keys = new List<string>();
            for (int index = 0; index < intent.BarKeys.Count; index++)
            {
                string barKey = intent.BarKeys[index];
                LiveOpsTimelineBarModel bar = FindBar(barKey);
                if (bar != null && bar.IsStrip) continue;
                keys.Add(barKey);
            }
            SetSelectedBarKeys(keys, intent.PrimaryBarKey);
        }

        private void HandleAddAtTime(AddAtTimeIntent intent)
        {
            if (intent.LaneTypeId.Length == 0) return;
            LiveEventCalendarDocument document = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            // Làn lặp không tạo đợt cố định được ("Làn lặp — sửa ở Luật lặp", 7.3) — bỏ qua thay vì mở popover rồi báo lỗi.
            if (document.TryGetRecurringRule(intent.LaneTypeId, out RecurringLiveEventRule _)) return;
            AddEventRequested?.Invoke(intent.LaneTypeId, intent.StartUtc, intent.EndUtc);
        }

        // ============================================================================================================ kéo

        private void HandleMove(MoveBarIntent intent)
        {
            LiveOpsTimelineBarModel bar = FindBar(intent.BarKey);
            // Dải gom và thanh sinh từ luật không kéo được: xét IsStrip TRƯỚC khi coi BarKey là EntryKey (V-22 CC-TLMODEL-1).
            if (bar != null && (bar.IsStrip || bar.Source == LiveOpsTimelineBarSource.Recurring)) return;

            LiveOpsHubCalendarSession session = _services.Session;
            LiveEventCalendarDocument document = session.Document ?? LiveEventCalendarDocument.Empty;
            if (!document.TryGetFixedEvent(intent.BarKey, out FixedLiveEventEntry entry)) return;

            if (intent.IsCancel)
            {
                CancelDrag();
                return;
            }
            if (intent.IsPreview)
            {
                PreviewDrag(session, document, entry, intent);
                return;
            }
            CommitDrag(session, entry, intent);
        }

        private void PreviewDrag(LiveOpsHubCalendarSession session, LiveEventCalendarDocument document, FixedLiveEventEntry entry,
            MoveBarIntent intent)
        {
            // (UX-06) Đang chờ người dùng trả lời hộp của lần kéo trước: không mở cử chỉ mới đè lên nháp chưa quyết.
            if (_isAwaitingDragConfirmation) return;
            if (_dragGroup == LiveOpsHubEditOutcome.NoUndoGroup)
            {
                // Đợt đã khép không dời được (bảng 7.0): chặn ngay ở bước xem trước, không mở Undo group rỗng.
                if (PhaseOf(entry) == LiveEventPhase.Ended) return;
                _dragStartDocument = document;
                _dragStartEntry = entry;
                _dragBarKey = intent.BarKey;
                CaptureDragStartFindings(session, document, entry.EventType);
                _dragGroup = session.BeginContinuousEdit(ProvisionalUndoName(entry));
                if (_dragGroup == LiveOpsHubEditOutcome.NoUndoGroup) return;
                _isDragActive = true;
                DragActiveChanged?.Invoke(true);
            }
            session.UpdateContinuousEdit(BuildDragEdit(entry, intent));
            // 7.3: mỗi bước xem trước chạy kiểm nhanh CHÍNH LÀN đó trên nháp vừa đổi. Không có nó thì dấu "bị bỏ" và vế "chồng n
            // giờ" trên khung vẫn là kết quả của lần kiểm cũ — người dùng đã kéo hết chồng giờ mà màn hình còn báo chồng.
            RefreshPreviewQuickCheck(session, entry.EventType);
        }

        private void RefreshPreviewQuickCheck(LiveOpsHubCalendarSession session, string eventType)
        {
            if (string.IsNullOrEmpty(eventType))
            {
                ClearPreviewQuickCheck();
                return;
            }
            _previewLaneCheckReport = session.CheckLane(eventType, session.Document);
            _previewQuickCheckText = QuickCheckTextOf(_previewLaneCheckReport);
            QuickCheckTagChanged?.Invoke(_previewQuickCheckText, QuickCheckHealthOf(_previewLaneCheckReport));
        }

        /// <summary>
        /// (UX-14) Dấu đi theo KẾT QUẢ của chính bước kéo: có phát hiện Bị bỏ MỚI dính đợt đang kéo thì Blocked, không thì Ok.
        /// Trước đây bất kỳ phát hiện Bị bỏ nào trên làn cũng làm dấu đỏ, kể cả lỗi có sẵn của đợt khác.
        /// </summary>
        private HealthState QuickCheckHealthOf(LiveEventCalendarCheckReport report)
        {
            if (report == null) return HealthState.NotMeasured;
            return NewDroppedFindingForDraggedBar(report) == null ? HealthState.Ok : HealthState.Blocked;
        }

        /// <summary>
        /// (UX-14, UJ-13) Phát hiện Bị bỏ dính ĐÚNG đợt đang kéo và CHƯA có trước lúc bắt đầu kéo; không có thì null (tag Ok).
        /// Chữ ký gồm luật + mã chi tiết + đợt đích: cùng một đợt có thể có nhiều lỗi khác nhau, và lỗi cũ không được tính là
        /// hậu quả của cử chỉ này.
        /// </summary>
        private LiveEventCalendarFinding NewDroppedFindingForDraggedBar(LiveEventCalendarCheckReport report)
        {
            IReadOnlyList<LiveEventCalendarFinding> findings = report.Findings;
            for (int index = 0; index < findings.Count; index++)
            {
                LiveEventCalendarFinding finding = findings[index];
                if (finding.Consequence != LiveEventCalendarConsequence.Dropped) continue;
                if (!string.Equals(finding.TargetEntryKey ?? string.Empty, _dragBarKey, StringComparison.Ordinal)) continue;
                if (_dragStartFindingSignatures.Contains(SignatureOf(finding))) continue;
                return finding;
            }
            return null;
        }

        private static string SignatureOf(LiveEventCalendarFinding finding)
        {
            return (finding.RuleId ?? string.Empty) + "|" + (finding.DetailCode ?? string.Empty) + "|"
                + (finding.TargetEntryKey ?? string.Empty);
        }

        /// <summary>(UX-14) Chụp phát hiện của làn NGAY TRƯỚC khi mở cử chỉ kéo — mốc so để biết lỗi nào là lỗi mới.</summary>
        private void CaptureDragStartFindings(LiveOpsHubCalendarSession session, LiveEventCalendarDocument document,
            string eventType)
        {
            _dragStartFindingSignatures.Clear();
            if (string.IsNullOrEmpty(eventType)) return;
            LiveEventCalendarCheckReport report = session.CheckLane(eventType, document);
            if (report == null) return;
            IReadOnlyList<LiveEventCalendarFinding> findings = report.Findings;
            for (int index = 0; index < findings.Count; index++) _dragStartFindingSignatures.Add(SignatureOf(findings[index]));
        }

        private void ClearPreviewQuickCheck()
        {
            bool hadText = _previewQuickCheckText.Length > 0;
            _previewLaneCheckReport = null;
            _previewQuickCheckText = string.Empty;
            if (hadText) QuickCheckTagChanged?.Invoke(string.Empty, HealthState.Ok);
        }

        /// <summary>
        /// (UX-14) Bước kéo này không sinh lỗi mới cho đợt đang kéo = câu Ok; có thì nêu đúng câu của phát hiện ĐÓ (V-8).
        /// </summary>
        private string QuickCheckTextOf(LiveEventCalendarCheckReport report)
        {
            if (report == null) return string.Empty;
            LiveEventCalendarFinding finding = NewDroppedFindingForDraggedBar(report);
            if (finding == null) return LiveOpsHubStrings.CalendarQuickCheckOkTag;
            LiveEventCalendarDocument document = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            return LiveOpsFindingText.Headline(finding, _services.Format, document.LatestStamp);
        }

        /// <summary>
        /// Nhả chuột. (UX-06, UJ-11) Thứ tự MỚI so với SPIKE-B SP-2: mức xác nhận quyết trên NHÁP trước khi commit, cần hỏi thì
        /// giữ nháp và chờ câu trả lời — toast, tên bước Undo và <see cref="DocumentEdited"/> chỉ chạy SAU lựa chọn. Thứ tự cũ
        /// (commit + toast rồi mới hỏi) làm trục, toast và status bar đồng loạt báo "Đã dời" trong lúc hộp còn đang hỏi, và chọn
        /// nút an toàn vẫn để lại một bước Undo cùng dòng "Vừa làm …" trên status bar.
        /// </summary>
        private void CommitDrag(LiveOpsHubCalendarSession session, FixedLiveEventEntry entry, MoveBarIntent intent)
        {
            if (_dragGroup == LiveOpsHubEditOutcome.NoUndoGroup || _isAwaitingDragConfirmation) return;
            FixedLiveEventEntry after = ClampedEntry(_dragStartEntry ?? entry, intent);
            session.UpdateContinuousEdit(BuildDragEdit(_dragStartEntry ?? entry, intent));

            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.ChangeFixedEventTimes,
                _dragStartDocument, session.Document, BaselineDocument(), _services.Clock.UtcNow, _dragBarKey);
            if (decision.Requirement == LiveOpsConfirmRequirement.Level1)
            {
                // Hộp vẫn chỉ mở SAU khi chuột đã nhả (SP-2 (a)) — chỉ khác ở chỗ chưa có gì được ghi khi nó mở.
                LiveOpsConfirmRequest request = BuildShortenRequest(decision, _dragStartEntry, after);
                MoveBarIntent capturedIntent = intent;
                FixedLiveEventEntry capturedAfter = after;
                _isAwaitingDragConfirmation = true;
                DeferConfirmation(() => ConfirmThenFinishDrag(request, capturedAfter, capturedIntent));
                return;
            }
            FinishDrag(session, after, intent);
        }

        /// <summary>(UX-06) Trả lời hộp: phá huỷ ⇒ commit đúng nháp đang giữ; an toàn ⇒ huỷ nháp, không toast, không bước Undo.</summary>
        private void ConfirmThenFinishDrag(LiveOpsConfirmRequest request, FixedLiveEventEntry after, MoveBarIntent intent)
        {
            if (!_isAwaitingDragConfirmation) return;
            _isAwaitingDragConfirmation = false;
            LiveOpsHubCalendarSession session = _services.Session;
            if (_services.Confirmation.Confirm(request) == LiveOpsConfirmResult.Destructive)
            {
                FinishDrag(session, after, intent);
                return;
            }
            session.CancelContinuousEdit(_dragGroup);
            ResetDragState();
            DocumentEdited?.Invoke();
        }

        /// <summary>Gộp nháp thành một bước Undo rồi phát toast + tên bước ngắn; dùng chung cho cả hai nhánh của UX-06.</summary>
        private void FinishDrag(LiveOpsHubCalendarSession session, FixedLiveEventEntry after, MoveBarIntent intent)
        {
            LiveOpsHubEditOutcome outcome = session.CommitContinuousEdit(_dragGroup);
            LiveEventCalendarDocument before = _dragStartDocument;
            FixedLiveEventEntry beforeEntry = _dragStartEntry;
            ResetDragState();
            if (!outcome.Applied) return;

            // Hai vế gộp ở cổng đợt W6: câu toast mang thêm đuôi "đợt sau đi theo" của G-OPT-TIMELINE, còn tên bước
            // Undo/status bar giữ CÂU NGẮN của G-FIX-W6-1 (Q-W5-5).
            string message = DragToastMessage(beforeEntry, after) + FollowerToastSuffix(before, beforeEntry, intent);
            // Câu ngắn của bước chỉ biết được lúc nhả chuột (giờ cuối), nên đặt tên ngay sau khi gộp.
            // Q-W5-5: Undo History và status bar đọc CÂU NGẮN [SD1 §3.4] — hai chỗ đó chỉ có một dòng và người đọc lại sau
            // nhiều thao tác; toast bên cạnh giữ CÂU DÀI vì người vừa làm xong cần đủ trước/sau.
            string undoneStepName = DragUndoStepName(beforeEntry, after);
            Undo.SetCurrentGroupName(undoneStepName);
            ToastRequested?.Invoke(LiveOpsToastModel.ForEdit(message, outcome.UndoGroup, string.Empty, undoneStepName));
            DocumentEdited?.Invoke();
        }

        /// <summary>
        /// (G-OPT-TIMELINE) Lệnh sửa của một bước kéo. Không giữ Shift thì vẫn đúng một <see cref="ReplaceFixedEventEdit"/> như
        /// W4; giữ Shift thì gộp cả đợt đi theo vào MỘT <see cref="CompositeCalendarEdit"/> — phiên áp lệnh lên tài liệu lúc bắt
        /// đầu kéo (không cộng dồn qua từng bước), nên gửi hai lệnh rời nhau thì lệnh sau xoá mất lệnh trước.
        /// Đợt đang chạy và đợt đã khép bị loại khỏi tập đi theo: bảng 7.0 không cho dời chúng bằng cử chỉ nào cả.
        /// </summary>
        private LiveEventCalendarEdit BuildDragEdit(FixedLiveEventEntry entry, MoveBarIntent intent)
        {
            FixedLiveEventEntry after = ClampedEntry(entry, intent);
            var main = new ReplaceFixedEventEdit(after);
            if (!intent.FollowsLaterEvents) return main;
            long shiftTicks = FollowerShiftTicks(entry, after);
            if (shiftTicks == 0L) return main;
            LiveEventCalendarDocument document = _dragStartDocument ?? _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            List<FixedLiveEventEntry> followers = FollowerEntries(document, entry);
            if (followers.Count == 0) return main;
            var edits = new List<LiveEventCalendarEdit> { main };
            for (int index = 0; index < followers.Count; index++)
            {
                FixedLiveEventEntry follower = followers[index];
                if (!follower.TryGetStartUtc(out DateTime startUtc) || !follower.TryGetEndUtc(out DateTime endUtc)) continue;
                edits.Add(new ReplaceFixedEventEdit(follower.WithTimes(
                    LiveEventUtcText.Format(LiveOpsTimelineGeometry.AddTicksClamped(startUtc, shiftTicks)),
                    LiveEventUtcText.Format(LiveOpsTimelineGeometry.AddTicksClamped(endUtc, shiftTicks)))));
            }
            return edits.Count == 1 ? (LiveEventCalendarEdit)main : new CompositeCalendarEdit(edits);
        }

        /// <summary>
        /// (vá F-4) Tập đợt đi theo, tính từ TÀI LIỆU lúc bắt đầu kéo chứ không từ thanh đang vẽ: model timeline chỉ dựng thanh
        /// cho đợt giao với khoảng đang xem, nên lấy theo thanh thì cùng một cử chỉ dời được nhiều hay ít tuỳ mức zoom — ở "3
        /// tuần" đợt đầu tháng sau đứng yên, ở "Tháng" nó đi theo, mà không dòng gợi ý nào nói ra điều kiện đó.
        /// Luật: cùng loại event, bắt đầu từ mép cuối GỐC của đợt đang kéo trở đi, và còn ở giai đoạn Sắp tới — bảng 7.0 không
        /// cho cử chỉ nào dời đợt đang chạy hay đã khép.
        /// </summary>
        private List<FixedLiveEventEntry> FollowerEntries(LiveEventCalendarDocument document, FixedLiveEventEntry draggedEntry)
        {
            var followers = new List<FixedLiveEventEntry>();
            if (!draggedEntry.TryGetEndUtc(out DateTime originalEndUtc)) return followers;
            IReadOnlyList<FixedLiveEventEntry> entries = document.FixedEvents;
            for (int index = 0; index < entries.Count; index++)
            {
                FixedLiveEventEntry other = entries[index];
                if (string.Equals(other.EntryKey, draggedEntry.EntryKey, StringComparison.Ordinal)) continue;
                if (!string.Equals(other.EventType, draggedEntry.EventType, StringComparison.Ordinal)) continue;
                if (!other.TryGetStartUtc(out DateTime startUtc) || !other.TryGetEndUtc(out DateTime _)) continue;
                if (startUtc < originalEndUtc) continue;
                if (PhaseOf(other) != LiveEventPhase.Upcoming) continue;
                followers.Add(other);
            }
            return followers;
        }

        /// <summary>Khoảng dời của cả dãy = khoảng MÉP CUỐI vừa dời (kéo thân và kéo mép cuối dùng chung một mốc).</summary>
        private static long FollowerShiftTicks(FixedLiveEventEntry draggedEntry, FixedLiveEventEntry after)
        {
            if (!draggedEntry.TryGetEndUtc(out DateTime originalEndUtc) || !after.TryGetEndUtc(out DateTime newEndUtc)) return 0L;
            return (newEndUtc - originalEndUtc).Ticks;
        }

        /// <summary>
        /// Vế "· 2 đợt sau đi theo" của toast; "" khi không giữ Shift. Toast phải nói ra thứ người dùng vừa dời KÈM. Đọc tài liệu
        /// TRƯỚC cử chỉ (tham số), không đọc tài liệu hiện hành: lúc gọi thì lệnh sửa đã áp xong, các đợt đi theo đã đứng ở giờ
        /// mới và đếm lại trên đó là đếm nhầm.
        /// </summary>
        private string FollowerToastSuffix(LiveEventCalendarDocument beforeDocument, FixedLiveEventEntry beforeEntry,
            MoveBarIntent intent)
        {
            int count = CountMovableFollowers(beforeDocument, beforeEntry, intent);
            return count == 0
                ? string.Empty
                : LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.TimelineDragFollowersToastSuffixFormat), count);
        }

        private int CountMovableFollowers(LiveEventCalendarDocument beforeDocument, FixedLiveEventEntry beforeEntry,
            MoveBarIntent intent)
        {
            if (!intent.FollowsLaterEvents || beforeEntry == null) return 0;
            LiveEventCalendarDocument document = beforeDocument ?? _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            if (FollowerShiftTicks(beforeEntry, ClampedEntry(beforeEntry, intent)) == 0L) return 0;
            return FollowerEntries(document, beforeEntry).Count;
        }

        // ============================================================================================ chọn nhiều (khung 9)

        /// <summary>
        /// Ô "Dời cả hai (giờ)" + nút Áp của inspector trạng thái (c) [SD1 §3.10]: dời MỌI đợt đang chọn đi cùng một số giờ, một
        /// bước Undo. Đợt đang chạy và đợt đã khép bị bỏ qua (bảng 7.0) — nút không tắt vì tập chọn có thể lẫn cả hai loại, câu
        /// trợ giúp dưới ô nói trước điều đó và toast nói lại đúng số đợt thật sự đã dời.
        /// </summary>
        public bool ShiftSelectedEvents(double hours)
        {
            if (_selectedBarKeys.Count < 2 || hours == 0d) return false;
            LiveEventCalendarDocument document = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            var edits = new List<LiveEventCalendarEdit>();
            for (int index = 0; index < _selectedBarKeys.Count; index++)
            {
                if (!document.TryGetFixedEvent(_selectedBarKeys[index], out FixedLiveEventEntry entry)) continue;
                if (PhaseOf(entry) != LiveEventPhase.Upcoming) continue;
                if (!entry.TryGetStartUtc(out DateTime startUtc) || !entry.TryGetEndUtc(out DateTime endUtc)) continue;
                edits.Add(new ReplaceFixedEventEdit(entry.WithTimes(LiveEventUtcText.Format(startUtc.AddHours(hours)),
                    LiveEventUtcText.Format(endUtc.AddHours(hours)))));
            }
            if (edits.Count == 0) return false;
            // Truyền SỐ, không truyền chuỗi đã nướng dấu: dấu +/- do chính câu trong catalog định dạng, và luật số ít/số nhiều
            // tiếng Anh (Q-W5-2) cần đọc được tham số này như một số để chọn "hour" hay "hours".
            string message = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.TimelineMultiSelectShiftToastFormat),
                edits.Count, hours);
            string undoneStepName = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.TimelineMultiSelectShiftUndoStepFormat),
                edits.Count);
            return ApplyEdit(new CompositeCalendarEdit(edits), LiveOpsEditOperation.ChangeFixedEventTimes, _selectedBarKey, message,
                string.Empty, undoneStepName);
        }

        /// <summary>Nút "Xoá n đợt…" của trạng thái (c): một bước Undo; mức hỏi do <see cref="LiveOpsConfirmationPolicy"/> quyết.</summary>
        public bool DeleteSelectedEvents()
        {
            if (_selectedBarKeys.Count < 2) return false;
            LiveEventCalendarDocument document = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            var edits = new List<LiveEventCalendarEdit>();
            for (int index = 0; index < _selectedBarKeys.Count; index++)
            {
                if (!document.TryGetFixedEvent(_selectedBarKeys[index], out FixedLiveEventEntry _)) continue;
                edits.Add(new RemoveFixedEventEdit(_selectedBarKeys[index]));
            }
            if (edits.Count == 0) return false;
            string message = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.TimelineMultiSelectDeleteToastFormat), edits.Count);
            string undoneStepName = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.TimelineMultiSelectDeleteUndoStepFormat),
                edits.Count);
            if (!ApplyEdit(new CompositeCalendarEdit(edits), LiveOpsEditOperation.DeleteFixedEvent, _selectedBarKey, message,
                string.Empty, undoneStepName))
            {
                return false;
            }
            SetSelectedBarKey(string.Empty);
            return true;
        }

        private void CancelDrag()
        {
            if (_dragGroup == LiveOpsHubEditOutcome.NoUndoGroup) return;
            // Panel biến mất hay Esc giữa lúc còn chờ hộp: bỏ luôn việc hỏi, nháp về giá trị cũ.
            _isAwaitingDragConfirmation = false;
            _services.Session.CancelContinuousEdit(_dragGroup);
            ResetDragState();
            DocumentEdited?.Invoke();
        }

        /// <summary>Panel biến mất giữa lúc kéo (SP-2 (d)): huỷ để phiên không kẹt trong continuous edit.</summary>
        public void AbortDrag()
        {
            CancelDrag();
        }

        private void ResetDragState()
        {
            ClearPreviewQuickCheck();
            _dragGroup = LiveOpsHubEditOutcome.NoUndoGroup;
            _dragBarKey = string.Empty;
            _dragStartDocument = null;
            _dragStartEntry = null;
            _dragStartFindingSignatures.Clear();
            _isAwaitingDragConfirmation = false;
            if (!_isDragActive) return;
            _isDragActive = false;
            DragActiveChanged?.Invoke(false);
        }

        /// <summary>Mép đầu đợt đang chạy khoá: dù control có gửi giờ bắt đầu khác, presenter vẫn giữ giờ cũ (bảng 7.0).</summary>
        private FixedLiveEventEntry ClampedEntry(FixedLiveEventEntry entry, MoveBarIntent intent)
        {
            DateTime startUtc = intent.NewStartUtc;
            if (PhaseOf(entry) == LiveEventPhase.Active && entry.TryGetStartUtc(out DateTime originalStartUtc))
            {
                startUtc = originalStartUtc;
            }
            DateTime endUtc = intent.NewEndUtc > startUtc ? intent.NewEndUtc : startUtc.AddHours(1);
            return entry.WithTimes(LiveEventUtcText.Format(startUtc), LiveEventUtcText.Format(endUtc));
        }

        private string ProvisionalUndoName(FixedLiveEventEntry entry)
        {
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarMoveToastFormat, entry.EventId,
                entry.StartUtcText, entry.EndUtcText);
        }

        /// <summary>
        /// Tên bước NGẮN của một lần kéo (Q-W5-5, [SD1 §3.4]). Ba kiểu kéo chia làm hai lời: kéo cả thanh là "Dời …", kéo một
        /// mép là "Đổi …" — với người đọc lại Undo History đó là hai việc khác nhau. Phân nhánh bằng CÙNG phép đo mép của
        /// <see cref="DragToastMessage"/> để hai câu không bao giờ nói về hai thao tác khác nhau.
        /// </summary>
        internal string DragUndoStepName(FixedLiveEventEntry before, FixedLiveEventEntry after)
        {
            // (UX-26, UJ-10/UJ-22) Ba kiểu kéo, ba động từ THẬT. "Đổi {0}" chung cho cả hai mép làm status bar ("Vừa làm: Đổi
            // hunt-0914") và toast ("Đã dời kết thúc …") đọc như hai thao tác khác nhau về cùng một lần kéo.
            string format;
            switch (EdgeOf(before, after))
            {
                case DragEdge.Start:
                    format = LiveOpsHubStrings.CalendarDepthMoveStartEdgeUndoStepFormat;
                    break;
                case DragEdge.End:
                    format = LiveOpsHubStrings.CalendarDepthMoveEndEdgeUndoStepFormat;
                    break;
                default:
                    format = LiveOpsHubStrings.CalendarMoveUndoStepFormat;
                    break;
            }
            return string.Format(CultureInfo.InvariantCulture, format, after.EventId);
        }

        /// <summary>Mép nào đã đổi: chỉ đầu, chỉ cuối, hay cả hai (kéo cả thanh).</summary>
        private enum DragEdge
        {
            Body = 0,
            Start = 1,
            End = 2,
        }

        private static DragEdge EdgeOf(FixedLiveEventEntry before, FixedLiveEventEntry after)
        {
            if (before == null) return DragEdge.Body;
            if (!before.TryGetStartUtc(out DateTime beforeStartUtc) || !before.TryGetEndUtc(out DateTime beforeEndUtc))
            {
                return DragEdge.Body;
            }
            if (!after.TryGetStartUtc(out DateTime afterStartUtc) || !after.TryGetEndUtc(out DateTime afterEndUtc))
            {
                return DragEdge.Body;
            }
            bool startMoved = beforeStartUtc != afterStartUtc;
            bool endMoved = beforeEndUtc != afterEndUtc;
            if (startMoved && !endMoved) return DragEdge.Start;
            if (endMoved && !startMoved) return DragEdge.End;
            return DragEdge.Body;
        }

        /// <summary>Câu toast nêu đúng mép đã đổi: chỉ kết thúc, chỉ bắt đầu, hay cả hai (kéo thân).</summary>
        internal string DragToastMessage(FixedLiveEventEntry before, FixedLiveEventEntry after)
        {
            LiveOpsHubFormat format = _services.Format;
            bool hasAfterStart = after.TryGetStartUtc(out DateTime afterStartUtc);
            bool hasAfterEnd = after.TryGetEndUtc(out DateTime afterEndUtc);
            bool hasBefore = before != null && before.TryGetStartUtc(out DateTime _) && before.TryGetEndUtc(out DateTime _);
            if (hasBefore && hasAfterStart && hasAfterEnd)
            {
                before.TryGetStartUtc(out DateTime beforeStartUtc);
                before.TryGetEndUtc(out DateTime beforeEndUtc);
                bool startMoved = beforeStartUtc != afterStartUtc;
                bool endMoved = beforeEndUtc != afterEndUtc;
                if (!startMoved && endMoved)
                {
                    return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarMoveEndToastFormat, after.EventId,
                        format.ShortDateTime(beforeEndUtc), format.ShortDateTime(afterEndUtc));
                }
                if (startMoved && !endMoved)
                {
                    return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarMoveStartToastFormat, after.EventId,
                        format.ShortDateTime(beforeStartUtc), format.ShortDateTime(afterStartUtc));
                }
            }
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarMoveToastFormat, after.EventId,
                TimeTextOrUnreadable(format, hasAfterStart, afterStartUtc),
                TimeTextOrUnreadable(format, hasAfterEnd, afterEndUtc));
        }

        /// <summary>
        /// (UX-04, UJ-04) Giờ đọc được thì in giờ; không đọc được thì NÓI là chưa đọc được. Trước đây chỗ này bỏ qua kết quả
        /// <c>TryGet…</c> nên <c>default(DateTime)</c> ra "1/1 00:00" — một mốc người dùng chưa bao giờ gõ, in ngay cạnh câu lỗi
        /// nói rằng hub không đọc được giờ đó.
        /// </summary>
        private static string TimeTextOrUnreadable(LiveOpsHubFormat format, bool hasValue, DateTime utc)
        {
            return hasValue ? format.ShortDateTime(utc) : LiveOpsHubStrings.CalendarDepthUnreadableTimeText;
        }

        /// <summary>Hộp "Rút ngắn đợt đang chạy …?" [SD1 §3.15]: câu hậu quả luôn nói "không biết số người chơi toàn cục" trước (7.0).</summary>
        internal LiveOpsConfirmRequest BuildShortenRequest(LiveOpsConfirmDecision decision, FixedLiveEventEntry before,
            FixedLiveEventEntry after)
        {
            LiveOpsHubFormat format = _services.Format;
            DateTime nowUtc = _services.Clock.UtcNow;
            before.TryGetStartUtc(out DateTime beforeStartUtc);
            before.TryGetEndUtc(out DateTime beforeEndUtc);
            after.TryGetEndUtc(out DateTime afterEndUtc);
            string body = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarShortenConfirmBodyFormat,
                format.ShortDateTime(beforeEndUtc), format.ShortDateTime(afterEndUtc), format.ShortDateTime(beforeStartUtc),
                format.Remaining(nowUtc, afterEndUtc), format.Remaining(nowUtc, beforeEndUtc));
            string noRecord = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarNoEditorRecordFormat,
                decision.RunningEventId);
            string keepLabel = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarKeepEndLabelFormat,
                format.ShortDateTime(beforeEndUtc));
            return new LiveOpsConfirmRequest.Builder()
                .WithTitle(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarShortenConfirmTitleFormat,
                    decision.RunningEventId))
                .WithBody(body + " " + LiveOpsHubStrings.CalendarUnknownPlayerCountSentence + " " + noRecord)
                .WithButtons(LiveOpsHubStrings.CalendarShortenDestructiveLabel, keepLabel)
                .Build();
        }

        // ============================================================================================================ sửa có xác nhận

        /// <summary>
        /// Áp một lệnh sửa từ inspector: quyết mức xác nhận TRƯỚC (trên nháp trước khi sửa), hỏi nếu cần, rồi mới ghi. Khác đường
        /// kéo ở chỗ chưa có gì ghi vào asset nên hỏi trước là an toàn và không tốn một bước Undo thừa.
        /// </summary>
        public bool ApplyEdit(LiveEventCalendarEdit edit, LiveOpsEditOperation operation, string targetKey, string toastMessage,
            string toastTooltip)
        {
            return ApplyEdit(edit, operation, targetKey, toastMessage, toastTooltip, string.Empty);
        }

        /// <summary>
        /// Như trên, kèm TÊN BƯỚC ngắn cho toast sau ⌘Z (phiếu D-5). Câu toast của lệnh thường bắt đầu bằng "Đã …", mà toast sau
        /// Undo in "Đã hoàn tác: " + tên bước — không truyền tên bước riêng thì người dùng đọc "Đã hoàn tác: Đã dán …".
        /// </summary>
        /// <param name="undoneStepName">"" = dùng lại câu toast (giữ nguyên hành vi cũ cho lệnh chưa có tên bước riêng).</param>
        public bool ApplyEdit(LiveEventCalendarEdit edit, LiveOpsEditOperation operation, string targetKey, string toastMessage,
            string toastTooltip, string undoneStepName)
        {
            if (edit == null) return false;
            LiveOpsHubCalendarSession session = _services.Session;
            LiveEventCalendarDocument before = session.Document ?? LiveEventCalendarDocument.Empty;
            LiveEventCalendarDocument after;
            if (!LiveEventCalendarEdits.TryApply(before, edit, out after)) return false;

            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(operation, before, after, BaselineDocument(),
                _services.Clock.UtcNow, targetKey);
            if (decision.Requirement == LiveOpsConfirmRequirement.NotAllowed) return false;
            if (decision.Requirement == LiveOpsConfirmRequirement.TypeToConfirm || decision.Requirement == LiveOpsConfirmRequirement.Level1)
            {
                LiveOpsConfirmRequest request = BuildEditRequest(decision, operation);
                if (_services.Confirmation.Confirm(request) != LiveOpsConfirmResult.Destructive) return false;
            }
            LiveOpsHubEditOutcome outcome = session.Apply(edit, toastMessage);
            if (!outcome.Applied) return false;
            // Q-W5-5: TÊN BƯỚC THẬT trong Undo History phải là câu ngắn, y như đường kéo thanh (CommitDrag). Phiên đặt tên group
            // bằng câu toast vì nó không biết câu ngắn — hợp đồng đóng băng PD-35 khoá chữ ký Apply(edit, undoName) nên đổi tên
            // ngay sau khi gộp là đường duy nhất không phải ra phiếu đổi hợp đồng. Lệnh chưa có câu ngắn thì giữ nguyên câu toast.
            if (!string.IsNullOrEmpty(undoneStepName)) Undo.SetCurrentGroupName(undoneStepName);
            ToastRequested?.Invoke(LiveOpsToastModel.ForEdit(toastMessage, outcome.UndoGroup, toastTooltip, undoneStepName));
            DocumentEdited?.Invoke();
            return true;
        }

        /// <summary>Xoá đợt theo <see cref="CalendarDeleteFlow"/> — ba mức, câu và hộp do luồng dựng.</summary>
        public bool RequestDelete(string barKey)
        {
            LiveOpsTimelineBarModel bar = FindBar(barKey);
            if (bar != null && (bar.IsStrip || bar.Source == LiveOpsTimelineBarSource.Recurring)) return false;
            LiveOpsHubCalendarSession session = _services.Session;
            DateTime nowUtc = _services.Clock.UtcNow;
            CalendarDeleteFlow flow = CalendarDeleteFlow.For(session, barKey, nowUtc, _services.Format);
            if (!flow.HasTarget) return false;
            if (flow.AsksBeforeDeleting)
            {
                LiveOpsConfirmRequest request = flow.BuildConfirmRequest(LiveOpsHubKeyLabels.Undo);
                if (request != null && _services.Confirmation.Confirm(request) != LiveOpsConfirmResult.Destructive) return false;
            }
            string message = flow.ToastMessage(ContentWidth);
            string tooltip = flow.ToastTooltip();
            LiveOpsHubEditOutcome outcome = session.Apply(flow.BuildEdit(), message);
            if (!outcome.Applied) return false;
            if (string.Equals(_selectedBarKey, barKey, StringComparison.Ordinal)) SetSelectedBarKey(string.Empty);
            ToastRequested?.Invoke(LiveOpsToastModel.ForEdit(message, outcome.UndoGroup, tooltip));
            DocumentEdited?.Invoke();
            return true;
        }

        /// <summary>
        /// Hộp của một lệnh sửa từ inspector. Tiêu đề là CÂU HỎI và nhãn nút phá huỷ nói đúng việc sắp làm: dùng "Rút ngắn đợt"
        /// cho mọi lệnh (kể cả đổi id) là mời người dùng bấm một nút nói sai việc. Thân luôn có câu PD-17 ("Editor này chưa có
        /// bản ghi của …") sau câu "không biết số người chơi toàn cục" — cùng cặp câu với hộp rút ngắn (7.0).
        /// </summary>
        private LiveOpsConfirmRequest BuildEditRequest(LiveOpsConfirmDecision decision, LiveOpsEditOperation operation)
        {
            bool isShorten = operation == LiveOpsEditOperation.ChangeFixedEventTimes;
            string titleFormat = isShorten
                ? LiveOpsHubStrings.CalendarShortenConfirmTitleFormat
                : LiveOpsHubStrings.CalendarEditRunningConfirmTitleFormat;
            string destructiveLabel = isShorten
                ? LiveOpsHubStrings.CalendarShortenDestructiveLabel
                : LiveOpsHubStrings.CalendarEditDestructiveLabel;
            string noRecord = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarNoEditorRecordFormat,
                decision.RunningEventId);
            LiveOpsConfirmRequest.Builder builder = new LiveOpsConfirmRequest.Builder()
                .WithTitle(string.Format(CultureInfo.InvariantCulture, titleFormat, decision.RunningEventId))
                .WithBody(LiveOpsHubStrings.CalendarUnknownPlayerCountSentence + " " + noRecord);
            if (decision.Requirement == LiveOpsConfirmRequirement.TypeToConfirm)
            {
                builder = builder.WithTypeToConfirm(decision.RunningEventId);
            }
            return builder.WithButtons(destructiveLabel, LiveOpsHubStrings.KitConfirmKeepLabel).Build();
        }

        private static void DeferWithDelayCall(Action action)
        {
            EditorApplication.delayCall += () => action();
        }

        private LiveEventCalendarDocument BaselineDocument()
        {
            return _services.Session.Publish == null ? null : _services.Session.Publish.ActiveBaseline;
        }

        /// <summary>Số giờ có dấu cho toast: "+12", "-6", "0" — dấu là phần của câu, không phải phần của số.</summary>

        private LiveEventPhase PhaseOf(FixedLiveEventEntry entry)
        {
            if (!entry.TryGetStartUtc(out DateTime startUtc) || !entry.TryGetEndUtc(out DateTime endUtc)) return LiveEventPhase.None;
            DateTime nowUtc = _services.Clock.UtcNow;
            if (nowUtc < startUtc) return LiveEventPhase.Upcoming;
            return nowUtc < endUtc ? LiveEventPhase.Active : LiveEventPhase.Ended;
        }

        internal LiveOpsTimelineBarModel FindBar(string barKey)
        {
            if (_model == null || string.IsNullOrEmpty(barKey)) return null;
            IReadOnlyList<LiveOpsTimelineLaneModel> lanes = _model.Lanes;
            for (int laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
            {
                IReadOnlyList<LiveOpsTimelineBarModel> bars = lanes[laneIndex].Bars;
                for (int barIndex = 0; barIndex < bars.Count; barIndex++)
                {
                    if (string.Equals(bars[barIndex].BarKey, barKey, StringComparison.Ordinal)) return bars[barIndex];
                }
            }
            return null;
        }
    }
}
