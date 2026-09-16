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

        private LiveOpsTimelineModel _model;
        private string _selectedBarKey = string.Empty;
        private int _dragGroup = LiveOpsHubEditOutcome.NoUndoGroup;
        private string _dragBarKey = string.Empty;
        private LiveEventCalendarDocument _dragStartDocument;
        private FixedLiveEventEntry _dragStartEntry;

        public CalendarTimelinePresenter(LiveOpsHubServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            DeferConfirmation = DeferWithDelayCall;
            UndoLastStep = Undo.PerformUndo;
        }

        /// <summary>Chọn đổi (bấm thanh, bấm chỗ trống, tìm theo id) — section cập nhật inspector theo đây.</summary>
        public event Action<string> SelectionChanged;

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

        /// <summary>Cách hoãn hộp xác nhận tới sau khi chuột nhả; test thay bằng chạy đồng bộ (SP-2 (a)).</summary>
        internal Action<Action> DeferConfirmation { get; set; }

        /// <summary>Gỡ đúng bước Undo vừa ghi khi người dùng chọn nút an toàn (SP-2 (b)).</summary>
        internal Action UndoLastStep { get; set; }

        public string SelectedBarKey => _selectedBarKey;

        /// <summary>Bề rộng cột nội dung — quyết định toast xoá dùng dạng đủ hay dạng ngắn (PD của 7.3).</summary>
        public float ContentWidth { get; set; }

        /// <summary>Model timeline vừa dựng; presenter tra thanh theo <c>BarKey</c> từ đây.</summary>
        public LiveOpsTimelineModel Model => _model;

        public IReadOnlyList<string> HiddenLanes => _hiddenLanes;

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
                .WithHiddenLanes(_hiddenLanes);
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
            if (string.Equals(next, _selectedBarKey, StringComparison.Ordinal)) return;
            _selectedBarKey = next;
            SelectionChanged?.Invoke(_selectedBarKey);
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
            // INTERIM(G-CALENDAR-DEPTH): W4 chưa có nhân bản, copy/dán, hover card F8, ⌘+kéo tạo, ẩn/đổi thứ tự làn (mục 12 I-5).
            // Bỏ qua có chủ ý, KHÔNG hiện nút disabled: nút trỏ tới thứ chưa có còn khó hiểu hơn là không có nút.
            // W5 nối các nhánh này; hai luật CC-TLMODEL-2 đã chốt sẵn: MoveLaneIntent("") và CreateByDragIntent trên làn "" bỏ qua
            // vì làn chưa ghi loại không có loại nào để dời hay để tạo đợt.
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
            session.UpdateContinuousEdit(new ReplaceFixedEventEdit(ClampedEntry(entry, intent)));
        }

        private void CommitDrag(LiveOpsHubCalendarSession session, FixedLiveEventEntry entry, MoveBarIntent intent)
        {
            if (_dragGroup == LiveOpsHubEditOutcome.NoUndoGroup) return;
            FixedLiveEventEntry after = ClampedEntry(_dragStartEntry ?? entry, intent);
            session.UpdateContinuousEdit(new ReplaceFixedEventEdit(after));
            LiveOpsHubEditOutcome outcome = session.CommitContinuousEdit(_dragGroup);
            LiveEventCalendarDocument before = _dragStartDocument;
            FixedLiveEventEntry beforeEntry = _dragStartEntry;
            string barKey = _dragBarKey;
            ResetDragState();
            if (!outcome.Applied) return;

            string message = DragToastMessage(beforeEntry, after);
            // Tên bước Undo chỉ biết được lúc nhả chuột (giờ cuối), nên đổi tên ngay sau khi gộp: Undo History và toast phải nói
            // cùng một câu để người dùng tìm lại được bước đó khi toast đã tắt (8.5).
            Undo.SetCurrentGroupName(message);
            ToastRequested?.Invoke(LiveOpsToastModel.ForEdit(message, outcome.UndoGroup));
            DocumentEdited?.Invoke();

            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.ChangeFixedEventTimes, before,
                session.Document, BaselineDocument(), _services.Clock.UtcNow, barKey);
            if (decision.Requirement != LiveOpsConfirmRequirement.Level1) return;
            LiveOpsConfirmRequest request = BuildShortenRequest(decision, beforeEntry, after);
            DeferConfirmation(() => AskAndUndoIfSafe(request));
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
                LiveOpsConfirmRequest request = BuildEditRequest(decision, toastMessage);
                if (_services.Confirmation.Confirm(request) != LiveOpsConfirmResult.Destructive) return false;
            }
            LiveOpsHubEditOutcome outcome = session.Apply(edit, toastMessage);
            if (!outcome.Applied) return false;
            ToastRequested?.Invoke(LiveOpsToastModel.ForEdit(toastMessage, outcome.UndoGroup, toastTooltip));
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

        private LiveOpsConfirmRequest BuildEditRequest(LiveOpsConfirmDecision decision, string title)
        {
            LiveOpsConfirmRequest.Builder builder = new LiveOpsConfirmRequest.Builder()
                .WithTitle(title)
                .WithBody(LiveOpsHubStrings.CalendarUnknownPlayerCountSentence);
            if (decision.Requirement == LiveOpsConfirmRequirement.TypeToConfirm)
            {
                builder = builder.WithTypeToConfirm(decision.RunningEventId);
            }
            return builder.WithButtons(LiveOpsHubStrings.CalendarShortenDestructiveLabel, LiveOpsHubStrings.KitConfirmKeepLabel).Build();
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
