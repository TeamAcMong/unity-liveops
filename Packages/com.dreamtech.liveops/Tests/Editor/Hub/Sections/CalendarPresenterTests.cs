using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

using DreamTech.LiveOps.Tests;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// <see cref="CalendarTimelinePresenter"/>: ý định của timeline → lệnh sửa, một bước Undo trùng tên câu toast, và luồng
    /// hộp xác nhận của SPIKE-B SP-2 (hộp mở SAU khi nhả chuột, bước Undo ghi TRƯỚC khi hỏi, chọn nút an toàn thì gỡ đúng bước đó).
    /// Chạy ở mức model nên không cần panel: hộp đi qua <see cref="ScriptedLiveOpsHubConfirmationPresenter"/>, hoãn và Undo đi qua
    /// hai móc của presenter.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class CalendarPresenterTests
    {
        private static readonly DateTime RangeStartUtc = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime WhileMidQuestRunningUtc = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);
        private const float DesignTrackWidth = 635f;

        private ScriptedLiveOpsHubConfirmationPresenter _confirmation;
        private ManualLiveOpsClock _clock;

        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
        }

        [Test]
        public void Drag_OneUndoGroupNamedWithShortStepName()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarTimelinePresenter presenter = CreatePresenter(services, LiveOpsTimelineZoom.ThreeWeeks);
            List<LiveOpsToastModel> toasts = new List<LiveOpsToastModel>();
            presenter.ToastRequested += toast => toasts.Add(toast);

            DateTime newStartUtc = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
            DateTime newEndUtc = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, newStartUtc, newEndUtc,
                LiveOpsTimelineGesturePhase.Preview));
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, newStartUtc, newEndUtc,
                LiveOpsTimelineGesturePhase.Commit));

            Assert.AreEqual(1, toasts.Count, "một thao tác kéo = đúng một toast");
            LiveOpsToastModel toast = toasts[0];
            Assert.IsTrue(toast.HasUndo, "kéo xong luôn có nút Hoàn tác");
            // Q-W5-5: Undo History đọc CÂU NGẮN của thiết kế, toast giữ câu dài đủ trước/sau.
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarMoveUndoStepFormat, "hunt-0916-bonus"),
                toast.UndoGroupName, "kéo cả thanh = \"Dời …\"");
            Assert.AreNotEqual(toast.Message, toast.UndoGroupName, "toast giữ câu dài, Undo History không");
            StringAssert.Contains("hunt-0916-bonus", toast.Message);

            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey,
                out FixedLiveEventEntry moved));
            Assert.AreEqual("2026-09-17T00:00:00Z", moved.StartUtcText);
            Assert.AreEqual("2026-09-18T12:00:00Z", moved.EndUtcText);
            Assert.AreEqual(0, _confirmation.Requests.Count, "đợt chưa bắt đầu: dời không hỏi");
        }

        [Test]
        public void Drag_CancelLeavesDocumentUntouched()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarTimelinePresenter presenter = CreatePresenter(services, LiveOpsTimelineZoom.ThreeWeeks);
            List<LiveOpsToastModel> toasts = new List<LiveOpsToastModel>();
            presenter.ToastRequested += toast => toasts.Add(toast);

            DateTime newStartUtc = new DateTime(2026, 9, 19, 0, 0, 0, DateTimeKind.Utc);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, newStartUtc, newStartUtc.AddHours(36),
                LiveOpsTimelineGesturePhase.Preview));
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, newStartUtc, newStartUtc.AddHours(36),
                LiveOpsTimelineGesturePhase.Cancel));

            Assert.AreEqual(0, toasts.Count, "Esc huỷ kéo thì không có toast và không tạo bước Undo");
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey,
                out FixedLiveEventEntry entry));
            Assert.AreEqual("2026-09-16T12:00:00Z", entry.StartUtcText, "tài liệu về đúng lúc bắt đầu kéo");
        }

        [Test]
        public void ShortenRunning_KeepCancelsEdit()
        {
            LiveOpsHubServices services = CreateServices();
            _clock.Set(WhileMidQuestRunningUtc);
            CalendarTimelinePresenter presenter = CreatePresenter(services, LiveOpsTimelineZoom.ThreeWeeks);
            List<Action> deferred = new List<Action>();
            presenter.DeferConfirmation = action => deferred.Add(action);
            _confirmation.Enqueue(LiveOpsConfirmResult.Safe);

            DateTime startUtc = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
            DateTime shorterEndUtc = new DateTime(2026, 9, 19, 0, 0, 0, DateTimeKind.Utc);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, startUtc, shorterEndUtc,
                LiveOpsTimelineGesturePhase.Preview));
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, startUtc, shorterEndUtc,
                LiveOpsTimelineGesturePhase.Commit));

            Assert.AreEqual(1, deferred.Count, "hộp chỉ mở SAU khi chuột đã nhả, không bao giờ giữa lúc kéo (SP-2 (a))");
            Assert.AreEqual(0, _confirmation.Requests.Count, "chưa chạy phần hoãn thì chưa hỏi");
            // (W8-UX UX-06, UJ-11) Đổi hợp đồng của SP-2 (b): nháp KHÔNG được gộp thành bước Undo trước khi hỏi nữa — toast và
            // status bar báo "Đã dời" trong lúc hộp còn đang hỏi là thứ làm hộp mất hết ý nghĩa. Tài liệu vẫn mang bản xem
            // trước vì đó chính là nháp người dùng đang nhìn.
            Assert.IsTrue(services.Session.IsContinuousEditOpen, "chưa trả lời hộp thì nháp còn mở, chưa có bước Undo nào");

            deferred[0]();
            Assert.AreEqual(1, _confirmation.Requests.Count);
            LiveOpsConfirmRequest request = _confirmation.Requests[0];
            Assert.AreEqual(LiveOpsConfirmLevel.Level1, request.Level, "rút ngắn đợt đang chạy là hộp cấp 1");
            StringAssert.Contains("lava-quest-2026-09b", request.Title);
            StringAssert.Contains(LiveOpsHubStrings.CalendarUnknownPlayerCountSentence, request.Body);
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                out FixedLiveEventEntry afterKeep));
            Assert.AreEqual("2026-09-20T00:00:00Z", afterKeep.EndUtcText, "chọn Giữ thì giờ kết thúc trở về giá trị cũ");
        }

        [Test]
        public void ExtendRunning_DoesNotAsk()
        {
            LiveOpsHubServices services = CreateServices();
            _clock.Set(WhileMidQuestRunningUtc);
            CalendarTimelinePresenter presenter = CreatePresenter(services, LiveOpsTimelineZoom.ThreeWeeks);
            List<Action> deferred = new List<Action>();
            presenter.DeferConfirmation = action => deferred.Add(action);

            DateTime startUtc = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
            DateTime longerEndUtc = new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, startUtc, longerEndUtc,
                LiveOpsTimelineGesturePhase.Preview));
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, startUtc, longerEndUtc,
                LiveOpsTimelineGesturePhase.Commit));

            Assert.AreEqual(0, deferred.Count, "kéo dài đợt đang chạy không hỏi (bảng 7.0)");
        }

        [Test]
        public void RunningStartEdge_StaysLockedWhileDragging()
        {
            LiveOpsHubServices services = CreateServices();
            _clock.Set(WhileMidQuestRunningUtc);
            CalendarTimelinePresenter presenter = CreatePresenter(services, LiveOpsTimelineZoom.ThreeWeeks);
            DateTime movedStartUtc = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
            DateTime endUtc = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, movedStartUtc, endUtc,
                LiveOpsTimelineGesturePhase.Preview));
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, movedStartUtc, endUtc,
                LiveOpsTimelineGesturePhase.Commit));

            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                out FixedLiveEventEntry entry));
            Assert.AreEqual("2026-09-17T00:00:00Z", entry.StartUtcText,
                "mép đầu đợt đang chạy khoá: dời nó là đổi lịch sử người chơi đã trải qua");
        }

        [Test]
        public void EndedEvent_NotDraggable()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarTimelinePresenter presenter = CreatePresenter(services, LiveOpsTimelineZoom.ThreeWeeks);
            DateTime startUtc = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestEarlyEntryKey, startUtc, startUtc.AddDays(3),
                LiveOpsTimelineGesturePhase.Preview));
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestEarlyEntryKey, startUtc, startUtc.AddDays(3),
                LiveOpsTimelineGesturePhase.Commit));

            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestEarlyEntryKey,
                out FixedLiveEventEntry entry));
            Assert.AreEqual("2026-09-10T00:00:00Z", entry.StartUtcText, "đợt đã khép không dời được — không mở cả Undo group");
        }

        [Test]
        public void StripBar_ClickZoomsInsteadOfSelecting()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarTimelinePresenter presenter = CreatePresenter(services, LiveOpsTimelineZoom.Month);
            int zoomRequests = 0;
            presenter.ZoomRequested += (startUtc, endUtc) => zoomRequests++;

            LiveOpsTimelineBarModel strip = FindStrip(presenter.Model);
            Assert.IsNotNull(strip, "ở zoom Tháng sky-race gom thành dải — fixture cần có dải để kiểm luật này");
            presenter.HandleIntent(new SelectBarIntent(strip.BarKey));

            Assert.AreEqual(1, zoomRequests, "bấm dải là zoom vào, không phải chọn đợt (V-22 CC-TLMODEL-1)");
            Assert.AreEqual(string.Empty, presenter.SelectedBarKey, "dải không ánh xạ ra đợt nên không được coi khoá là EntryKey");
        }

        [Test]
        public void RecurringBar_NotDraggable()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarTimelinePresenter presenter = CreatePresenter(services, LiveOpsTimelineZoom.ThreeWeeks);
            LiveOpsTimelineBarModel recurring = FindRecurring(presenter.Model);
            Assert.IsNotNull(recurring, "lịch mẫu có làn weekly-pass sinh từ luật");
            int documentEdits = 0;
            presenter.DocumentEdited += () => documentEdits++;

            presenter.HandleIntent(new MoveBarIntent(recurring.BarKey, recurring.StartUtc.AddDays(1), recurring.EndUtc.AddDays(1),
                LiveOpsTimelineGesturePhase.Preview));
            presenter.HandleIntent(new MoveBarIntent(recurring.BarKey, recurring.StartUtc.AddDays(1), recurring.EndUtc.AddDays(1),
                LiveOpsTimelineGesturePhase.Commit));
            Assert.AreEqual(0, documentEdits, "đợt sinh từ luật chỉ đọc — sửa ở Luật lặp");
        }

        [Test]
        public void DepthIntents_IgnoredWithoutTouchingDocument()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarTimelinePresenter presenter = CreatePresenter(services, LiveOpsTimelineZoom.ThreeWeeks);
            int documentEdits = 0;
            presenter.DocumentEdited += () => documentEdits++;

            presenter.HandleIntent(new MoveLaneIntent(string.Empty, MoveLaneIntent.Up));
            presenter.HandleIntent(new MoveLaneIntent("lava-quest", MoveLaneIntent.Down));
            presenter.HandleIntent(new HideLaneIntent("lava-quest"));
            presenter.HandleIntent(new ShowAllLanesIntent());
            presenter.HandleIntent(new DuplicateBarIntent(LiveOpsDesignSample.HuntBonusEntryKey));
            presenter.HandleIntent(new CopyBarIntent(LiveOpsDesignSample.HuntBonusEntryKey));
            presenter.HandleIntent(new PasteAtTimeIntent("lava-quest", RangeStartUtc));
            presenter.HandleIntent(new CreateByDragIntent(string.Empty, RangeStartUtc, RangeStartUtc.AddDays(1),
                LiveOpsTimelineGesturePhase.Commit));
            presenter.HandleIntent(new ShowFindingIntent(ShowFindingIntent.Next));

            Assert.AreEqual(0, documentEdits,
                "nhánh chiều sâu của W5 bỏ qua có chủ ý (mục 12 I-5) — không được lặng lẽ sửa tài liệu");
        }

        /// <summary>
        /// 7.3 bắt mỗi bước xem trước chạy "kiểm nhanh làn này" trên nháp vừa đổi. Dời hunt-0916-bonus +12 giờ là vừa đủ hết chồng
        /// với hunt-0914, nên tag phải đổi sang câu Ok NGAY lúc xem trước — không đợi thả, không đọc lại báo cáo kiểm cũ.
        /// </summary>
        [Test]
        public void PreviewDrag_RunsLaneQuickCheck()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarTimelinePresenter presenter = CreatePresenter(services, LiveOpsTimelineZoom.ThreeWeeks);
            LiveEventCalendarCheckReport reportBeforeDrag = services.Session.Check.LastReport;
            Assert.IsNull(presenter.PreviewLaneCheckReport, "chưa kéo thì không có kết quả kiểm nhanh nào");

            DateTime overlappingStartUtc = new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, overlappingStartUtc,
                overlappingStartUtc.AddHours(36), LiveOpsTimelineGesturePhase.Preview));
            Assert.IsNotNull(presenter.PreviewLaneCheckReport, "mỗi bước xem trước phải gọi CheckLane (7.3)");
            Assert.AreNotEqual(LiveOpsHubStrings.CalendarQuickCheckOkTag, presenter.PreviewQuickCheckText,
                "đợt vẫn chồng hunt-0914 nên kiểm nhanh phải nêu phát hiện Bị bỏ");

            DateTime clearStartUtc = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, clearStartUtc,
                clearStartUtc.AddHours(36), LiveOpsTimelineGesturePhase.Preview));
            Assert.AreEqual(LiveOpsHubStrings.CalendarQuickCheckOkTag, presenter.PreviewQuickCheckText,
                "dời +12 giờ là hết chồng — tag đổi ngay ở bước xem trước [SD1 §3.8 khung 5]");

            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, clearStartUtc,
                clearStartUtc.AddHours(36), LiveOpsTimelineGesturePhase.Commit));
            Assert.IsNull(presenter.PreviewLaneCheckReport, "thả chuột xong không còn cử chỉ nào đang mở");
            Assert.AreSame(reportBeforeDrag, services.Session.Check.LastReport,
                "kiểm nhanh một làn KHÔNG bao giờ ghi vào trạng thái Kiểm lịch (mục 2159)");
        }

        [Test]
        public void EditRunningEvent_ConfirmSaysWhatItDoes()
        {
            LiveOpsHubServices services = CreateServices();
            _clock.Set(WhileMidQuestRunningUtc);
            CalendarTimelinePresenter presenter = CreatePresenter(services, LiveOpsTimelineZoom.ThreeWeeks);
            _confirmation.Enqueue(LiveOpsConfirmResult.Safe);
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                out FixedLiveEventEntry entry));

            presenter.ApplyEdit(new ReplaceFixedEventEdit(entry.WithEventId("lava-quest-2026-09b-doi")),
                LiveOpsEditOperation.RenameOrRetypeFixedEvent, entry.EntryKey, "toast", string.Empty);

            Assert.AreEqual(1, _confirmation.Requests.Count, "đổi id đợt đang chạy phải hỏi");
            LiveOpsConfirmRequest request = _confirmation.Requests[0];
            StringAssert.Contains("lava-quest-2026-09b", request.Title, "tiêu đề là câu hỏi về chính đợt đang chạy");
            Assert.IsTrue(request.Title.EndsWith("?", StringComparison.Ordinal),
                "tiêu đề hộp là CÂU HỎI, không phải câu quá khứ của toast");
            Assert.AreNotEqual(LiveOpsHubStrings.CalendarShortenDestructiveLabel, request.DestructiveLabel,
                "nút phá huỷ không được nói 'Rút ngắn đợt' khi việc sắp làm là đổi id");
            StringAssert.Contains(LiveOpsHubStrings.CalendarUnknownPlayerCountSentence, request.Body);
            StringAssert.Contains("lava-quest-2026-09b", request.Body, "thân hộp có câu PD-17 nêu Editor chưa có bản ghi của đợt");
        }

        [Test]
        public void Delete_RaisesToastAndClearsSelection()
        {
            LiveOpsHubServices services = CreateServices();
            CalendarTimelinePresenter presenter = CreatePresenter(services, LiveOpsTimelineZoom.ThreeWeeks);
            presenter.SetSelectedBarKey(LiveOpsDesignSample.HuntBonusEntryKey);
            List<LiveOpsToastModel> toasts = new List<LiveOpsToastModel>();
            presenter.ToastRequested += toast => toasts.Add(toast);

            Assert.IsTrue(presenter.RequestDelete(LiveOpsDesignSample.HuntBonusEntryKey));
            Assert.AreEqual(0, _confirmation.Requests.Count, "chưa bắt đầu và chưa đăng: xoá ngay, không hộp");
            Assert.AreEqual(1, toasts.Count);
            Assert.IsTrue(toasts[0].HasUndo);
            Assert.AreEqual(string.Empty, presenter.SelectedBarKey, "xoá xong không giữ lại lựa chọn trỏ vào đợt đã mất");
            Assert.IsFalse(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey,
                out FixedLiveEventEntry _));
        }

        private LiveOpsHubServices CreateServices()
        {
            _clock = LiveOpsHubTestServices.CreateClock();
            _confirmation = new ScriptedLiveOpsHubConfirmationPresenter();
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(_clock)
                .WithConfirmation(_confirmation)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            services.Session.RunCheckToCompletion();
            return services;
        }

        private static CalendarTimelinePresenter CreatePresenter(LiveOpsHubServices services, LiveOpsTimelineZoom zoom)
        {
            CalendarTimelinePresenter presenter = new CalendarTimelinePresenter(services);
            DateTime rangeEndUtc = LiveOpsTimelineGeometry.AddTicksClamped(RangeStartUtc,
                LiveOpsTimelineGeometry.RangeLengthOf(zoom).Ticks);
            presenter.BuildModel(RangeStartUtc, rangeEndUtc, DesignTrackWidth);
            return presenter;
        }

        private static LiveOpsTimelineBarModel FindStrip(LiveOpsTimelineModel model)
        {
            return FindBar(model, true, false);
        }

        private static LiveOpsTimelineBarModel FindRecurring(LiveOpsTimelineModel model)
        {
            return FindBar(model, false, true);
        }

        private static LiveOpsTimelineBarModel FindBar(LiveOpsTimelineModel model, bool wantStrip, bool wantRecurring)
        {
            IReadOnlyList<LiveOpsTimelineLaneModel> lanes = model.Lanes;
            for (int laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
            {
                IReadOnlyList<LiveOpsTimelineBarModel> bars = lanes[laneIndex].Bars;
                for (int barIndex = 0; barIndex < bars.Count; barIndex++)
                {
                    LiveOpsTimelineBarModel bar = bars[barIndex];
                    if (wantStrip && bar.IsStrip) return bar;
                    if (wantRecurring && !bar.IsStrip && bar.Source == LiveOpsTimelineBarSource.Recurring) return bar;
                }
            }
            return null;
        }
    }
}
