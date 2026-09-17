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
    /// Thao tác kéo theo SPIKE-B SP-2 (quyết định an toàn 16/9/2026): hộp xác nhận CHỈ mở sau khi chuột đã nhả (qua
    /// <see cref="DeferConfirmation"/>, mặc định <c>EditorApplication.delayCall</c>); lúc nhả chuột kéo đã ghi MỘT bước Undo rồi mới
    /// hỏi; chọn nút an toàn thì gọi Undo đúng bước đó (<see cref="UndoLastStep"/>) chứ không dựng lại trạng thái bằng tay.
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

        public CalendarTimelinePresenter(LiveOpsHubServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            DeferConfirmation = DeferWithDelayCall;
            UndoLastStep = Undo.PerformUndo;
        }

        /// <summary>Chọn đổi (bấm thanh, bấm chỗ trống, tìm theo id) — section cập nhật inspector theo đây.</summary>
        public event Action<string> SelectionChanged;

        /// <summary>
        /// (G-OPT-TIMELINE, Hình 12 khung 9) Tập chọn NHIỀU đợt đổi. Tách khỏi <see cref="SelectionChanged"/> vì nơi nghe câu
        /// đó (màn) trả lời bằng <c>timeline.Select(barKey)</c> — tức là thu tập về đúng một thanh, đúng thứ vừa bị phá.
        /// Tập một phần tử hoặc rỗng KHÔNG phát qua đây; đường cũ lo trọn hai ca đó.
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

        /// <summary>Cách hoãn hộp xác nhận tới sau khi chuột nhả; test thay bằng chạy đồng bộ (SP-2 (a)).</summary>
        internal Action<Action> DeferConfirmation { get; set; }

        /// <summary>Gỡ đúng bước Undo vừa ghi khi người dùng chọn nút an toàn (SP-2 (b)).</summary>
        internal Action UndoLastStep { get; set; }

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
            // Đang chọn nhiều thì PHẢI phát dù khoá mốc không đổi: inspector đang vẽ trạng thái (c) và chỉ câu này gọi nó về
            // trạng thái một đợt.
            if (!wasMultiple && string.Equals(next, _selectedBarKey, StringComparison.Ordinal)) return;
            _selectedBarKey = next;
            _selectedBarKeys.Clear();
            if (next.Length > 0) _selectedBarKeys.Add(next);
            if (wasMultiple) SelectionSetChanged?.Invoke(_selectedBarKeys);
            SelectionChanged?.Invoke(_selectedBarKey);
        }

        /// <summary>
        /// (Hình 12 khung 9) Đặt cả tập chọn. Tập từ hai phần tử trở lên chỉ phát <see cref="SelectionSetChanged"/> — phát
        /// <see cref="SelectionChanged"/> ở đây là tự tay bảo màn thu tập về một thanh.
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
            SelectionSetChanged?.Invoke(_selectedBarKeys);
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
            if (_dragGroup == LiveOpsHubEditOutcome.NoUndoGroup)
            {
                // Đợt đã khép không dời được (bảng 7.0): chặn ngay ở bước xem trước, không mở Undo group rỗng.
                if (PhaseOf(entry) == LiveEventPhase.Ended) return;
                _dragStartDocument = document;
                _dragStartEntry = entry;
                _dragBarKey = intent.BarKey;
                _dragGroup = session.BeginContinuousEdit(ProvisionalUndoName(entry));
                if (_dragGroup == LiveOpsHubEditOutcome.NoUndoGroup) return;
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

        /// <summary>Dấu đi theo KẾT QUẢ: có phát hiện Bị bỏ trên làn thì Blocked, không thì Ok (không bao giờ gán cứng Ok).</summary>
        private static HealthState QuickCheckHealthOf(LiveEventCalendarCheckReport report)
        {
            if (report == null) return HealthState.NotMeasured;
            IReadOnlyList<LiveEventCalendarFinding> findings = report.Findings;
            for (int index = 0; index < findings.Count; index++)
            {
                if (findings[index].Consequence == LiveEventCalendarConsequence.Dropped) return HealthState.Blocked;
            }
            return HealthState.Ok;
        }

        private void ClearPreviewQuickCheck()
        {
            bool hadText = _previewQuickCheckText.Length > 0;
            _previewLaneCheckReport = null;
            _previewQuickCheckText = string.Empty;
            if (hadText) QuickCheckTagChanged?.Invoke(string.Empty, HealthState.Ok);
        }

        /// <summary>Không có phát hiện Bị bỏ nào trên làn = câu Ok; có thì nêu đúng câu của phát hiện nặng nhất (V-8).</summary>
        private string QuickCheckTextOf(LiveEventCalendarCheckReport report)
        {
            if (report == null) return string.Empty;
            IReadOnlyList<LiveEventCalendarFinding> findings = report.Findings;
            for (int index = 0; index < findings.Count; index++)
            {
                LiveEventCalendarFinding finding = findings[index];
                if (finding.Consequence != LiveEventCalendarConsequence.Dropped) continue;
                LiveEventCalendarDocument document = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
                return LiveOpsFindingText.Headline(finding, _services.Format, document.LatestStamp);
            }
            return LiveOpsHubStrings.CalendarQuickCheckOkTag;
        }

        private void CommitDrag(LiveOpsHubCalendarSession session, FixedLiveEventEntry entry, MoveBarIntent intent)
        {
            if (_dragGroup == LiveOpsHubEditOutcome.NoUndoGroup) return;
            FixedLiveEventEntry after = ClampedEntry(_dragStartEntry ?? entry, intent);
            session.UpdateContinuousEdit(BuildDragEdit(_dragStartEntry ?? entry, intent));
            LiveOpsHubEditOutcome outcome = session.CommitContinuousEdit(_dragGroup);
            LiveEventCalendarDocument before = _dragStartDocument;
            FixedLiveEventEntry beforeEntry = _dragStartEntry;
            string barKey = _dragBarKey;
            ResetDragState();
            if (!outcome.Applied) return;

            string message = DragToastMessage(beforeEntry, after) + FollowerToastSuffix(intent);
            // Tên bước Undo chỉ biết được lúc nhả chuột (giờ cuối), nên đổi tên ngay sau khi gộp: Undo History và toast phải nói
            // cùng một câu để người dùng tìm lại được bước đó khi toast đã tắt (8.5).
            Undo.SetCurrentGroupName(message);
            // Tên bước ngắn để toast sau ⌘Z đọc "Đã hoàn tác: Dời hunt-0916-bonus" [SD1 §3.8 khung 14] thay vì hai lần "Đã".
            string undoneStepName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarMoveUndoStepFormat, after.EventId);
            ToastRequested?.Invoke(LiveOpsToastModel.ForEdit(message, outcome.UndoGroup, string.Empty, undoneStepName));
            DocumentEdited?.Invoke();

            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.ChangeFixedEventTimes, before,
                session.Document, BaselineDocument(), _services.Clock.UtcNow, barKey);
            if (decision.Requirement != LiveOpsConfirmRequirement.Level1) return;
            LiveOpsConfirmRequest request = BuildShortenRequest(decision, beforeEntry, after);
            DeferConfirmation(() => AskAndUndoIfSafe(request));
        }

        /// <summary>
        /// (G-OPT-TIMELINE) Lệnh sửa của một bước kéo. Không giữ Shift thì vẫn đúng một <see cref="ReplaceFixedEventEdit"/> như
        /// W4; giữ Shift thì gộp cả đợt đi theo vào MỘT <see cref="CompositeCalendarEdit"/> — phiên áp lệnh lên tài liệu lúc bắt
        /// đầu kéo (không cộng dồn qua từng bước), nên gửi hai lệnh rời nhau thì lệnh sau xoá mất lệnh trước.
        /// Đợt đang chạy và đợt đã khép bị loại khỏi tập đi theo: bảng 7.0 không cho dời chúng bằng cử chỉ nào cả.
        /// </summary>
        private LiveEventCalendarEdit BuildDragEdit(FixedLiveEventEntry entry, MoveBarIntent intent)
        {
            var main = new ReplaceFixedEventEdit(ClampedEntry(entry, intent));
            if (intent.Followers.Count == 0) return main;
            LiveEventCalendarDocument document = _dragStartDocument ?? _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            var edits = new List<LiveEventCalendarEdit> { main };
            for (int index = 0; index < intent.Followers.Count; index++)
            {
                LiveOpsTimelineBarMove follower = intent.Followers[index];
                if (!document.TryGetFixedEvent(follower.BarKey, out FixedLiveEventEntry followerEntry)) continue;
                if (PhaseOf(followerEntry) != LiveEventPhase.Upcoming) continue;
                edits.Add(new ReplaceFixedEventEdit(followerEntry.WithTimes(LiveEventUtcText.Format(follower.NewStartUtc),
                    LiveEventUtcText.Format(follower.NewEndUtc))));
            }
            return edits.Count == 1 ? (LiveEventCalendarEdit)main : new CompositeCalendarEdit(edits);
        }

        /// <summary>Vế "· 2 đợt sau đi theo" của toast; "" khi không giữ Shift. Toast phải nói ra thứ người dùng vừa dời KÈM.</summary>
        private string FollowerToastSuffix(MoveBarIntent intent)
        {
            int count = CountMovableFollowers(intent);
            return count == 0
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineDragFollowersToastSuffixFormat, count);
        }

        private int CountMovableFollowers(MoveBarIntent intent)
        {
            LiveEventCalendarDocument document = _dragStartDocument ?? _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            int count = 0;
            for (int index = 0; index < intent.Followers.Count; index++)
            {
                if (!document.TryGetFixedEvent(intent.Followers[index].BarKey, out FixedLiveEventEntry followerEntry)) continue;
                if (PhaseOf(followerEntry) == LiveEventPhase.Upcoming) count++;
            }
            return count;
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
            string hoursText = hours.ToString(SignedHoursFormat, CultureInfo.InvariantCulture);
            string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineMultiSelectShiftToastFormat,
                edits.Count, hoursText);
            string undoneStepName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineMultiSelectShiftUndoStepFormat,
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
            string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineMultiSelectDeleteToastFormat, edits.Count);
            string undoneStepName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineMultiSelectDeleteUndoStepFormat,
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

        /// <summary>Câu toast nêu đúng mép đã đổi: chỉ kết thúc, chỉ bắt đầu, hay cả hai (kéo thân).</summary>
        internal string DragToastMessage(FixedLiveEventEntry before, FixedLiveEventEntry after)
        {
            LiveOpsHubFormat format = _services.Format;
            after.TryGetStartUtc(out DateTime afterStartUtc);
            after.TryGetEndUtc(out DateTime afterEndUtc);
            bool hasBefore = before != null && before.TryGetStartUtc(out DateTime _) && before.TryGetEndUtc(out DateTime _);
            if (hasBefore)
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
                format.ShortDateTime(afterStartUtc), format.ShortDateTime(afterEndUtc));
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

        private void AskAndUndoIfSafe(LiveOpsConfirmRequest request)
        {
            if (_services.Confirmation.Confirm(request) == LiveOpsConfirmResult.Destructive) return;
            UndoLastStep();
            DocumentEdited?.Invoke();
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
        private const string SignedHoursFormat = "+0.##;-0.##;0";

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
