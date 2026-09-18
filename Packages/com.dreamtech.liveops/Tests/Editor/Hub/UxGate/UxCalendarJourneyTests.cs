using System;
using System.Collections;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Hành trình màn Lịch của cổng W8-UX (§3.2) — mọi thao tác đi qua <see cref="UxEventSender"/> (chuột/phím THẬT vào cửa sổ
    /// hub), test chỉ ĐỌC trạng thái để assert.
    /// <para>
    /// Vì sao không gọi presenter/model: 26 lỗi của USER-JOURNEY-FINDINGS lọt qua 900 test xanh vì test cũ gọi thẳng hàm đích.
    /// Ô giờ chỉ nghe <c>RawTextCommitted</c>, chevron làn để <c>PickingMode.Ignore</c>, chip "Hiện" là Label — cả ba đều "đúng"
    /// khi gọi hàm và đều KHÔNG dùng được bằng chuột. Ở đây chỉ chuột và phím.
    /// </para>
    /// <para>
    /// Mỗi hành trình chạy ở CẢ vi lẫn en (vòng <see cref="UxHubWindowFixture.AllLanguages"/> bên trong một <c>[UnityTest]</c>,
    /// không dùng test có tham số vì UTF 1.1.33 và 1.8 chạy tham số khác nhau). Bản en có nhiều lỗi bố cục hơn bản vi (37 so với
    /// 32 ở cỡ 700), nên chạy một mình tiếng Việt là bỏ qua đúng nửa nặng hơn (R-10).
    /// </para>
    /// <para>
    /// Các test này ĐỎ trên code trước đợt W8 là có chủ đích: mỗi test khoá một lỗi UX của bảng gộp (ghi ở tóm tắt từng test).
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    [Category(LiveOpsHubTestCategories.UxGate)]
    public sealed class UxCalendarJourneyTests
    {
        /// <summary>Mẫu thiết kế đứng ở 13/9/2026 08:47 UTC+7 — cùng mốc với mọi ảnh của ma trận 9.5.</summary>
        private static readonly DateTime DesignNowUtc = new DateTime(2026, 9, 13, 1, 47, 0, DateTimeKind.Utc);

        private const int DragSteps = 6;

        /// <summary>Hover card hiện sau 500 ms; chờ gấp đôi để chắc chắn nó ĐÃ có cơ hội hiện rồi mới kết luận.</summary>
        private const int HoverCardWaitMilliseconds = LiveOpsHoverCardHost.ShowDelayMilliseconds * 2;

        /// <summary>
        /// Id đợt (EventId) của mẫu thiết kế — KHÁC khoá đợt (EntryKey "entry-…"). Readout kiểm nhanh in EventId
        /// (<c>LiveOpsFindingText</c> đọc <c>outcome.EventId</c>), nên assert theo EntryKey không bao giờ đỏ được: bản đầu tiên
        /// của UX-14 so readout với "entry-star-tournament-2026-10" và XANH ở cả hai bản Unity trong khi chữ thật là
        /// "● lava-quest-2026-10 …" (R-01). Hằng nằm ở đây vì <c>LiveOpsDesignSample</c> thuộc gói khác.
        /// </summary>
        private const string LavaQuestEarlyEventId = "lava-quest-2026-09a";

        private const string LavaQuestMidEventId = "lava-quest-2026-09b";
        private const string LavaQuestLateEventId = "lava-quest-2026-10";
        private const string StarTournamentEventId = "star-tournament-2026-10";
        private const string HuntEarlyEventId = "hunt-0914";
        private const string HuntBonusEventId = "hunt-0916-bonus";

        /// <summary>Mọi EventId của mẫu thiết kế — readout khi kéo chỉ được nhắc tới đợt ĐANG kéo trong danh sách này.</summary>
        private static readonly string[] AllDesignEventIds =
        {
            LavaQuestEarlyEventId, LavaQuestMidEventId, LavaQuestLateEventId,
            StarTournamentEventId, HuntEarlyEventId, HuntBonusEventId,
        };

        /// <summary>Sai số cho phép khi so mức thu phóng sau một nấc (double → float, làm tròn theo anchor).</summary>
        private const double ZoomComparisonTolerance = 0.001;

        private UxHubWindowFixture _fixture;
        private ScriptedLiveOpsHubConfirmationPresenter _confirmation;

        [TearDown]
        public void TearDown()
        {
            DisposeFixture();
            _confirmation = null;
        }

        // ================================================================================================ UX-02 zoom/cuộn

        /// <summary>
        /// UX-02: ⌘+lăn phải ĐỔI mức thu phóng ĐÚNG MỘT NẤC và VẼ LẠI trục — trước đợt W8 model đổi mà thước giữ nguyên nét.
        /// </summary>
        [UnityTest]
        public IEnumerator CommandWheel_ZoomsOneNotch_AndRedrawsRuler()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendar(UxHubWindowFixture.AllSizes[3], language);
                LiveOpsTimelineElement timeline = _fixture.Calendar.Timeline;
                double pixelsPerHourBefore = timeline.PixelsPerHour;
                int rulerTickCountBefore = RulerTickCount();

                yield return WheelOneNotch(timeline, -1, UxEventSender.ActionModifier);

                double expected = LiveOpsTimelineGeometry.ClampScale(
                    pixelsPerHourBefore * LiveOpsTimelineGeometry.ZoomFactorPerWheelNotch);
                Assert.AreEqual(expected, timeline.PixelsPerHour, ZoomComparisonTolerance,
                    "một nấc ⌘+lăn phải đổi mức thu phóng đúng một bậc (" + expected + "), đang là " + timeline.PixelsPerHour
                    + " — bánh xe quy đổi sai số nấc (UX-02)");
                Assert.AreNotEqual(rulerTickCountBefore, RulerTickCount(),
                    "mức thu phóng đổi nhưng thước giữ nguyên số nét: trục không được vẽ lại sau zoom (UX-02)");
                DisposeFixture();
            }
        }

        /// <summary>UX-02: Shift+lăn cuộn khoảng thời gian và thước phải chạy theo.</summary>
        [UnityTest]
        public IEnumerator ShiftWheel_ScrollsRange_AndRulerFollows()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendar(UxHubWindowFixture.AllSizes[3], language);
                LiveOpsTimelineElement timeline = _fixture.Calendar.Timeline;
                DateTime rangeStartBefore = timeline.RangeStartUtc;
                string firstTickBefore = FirstRulerTickText();

                yield return WheelOneNotch(timeline, 1, EventModifiers.Shift);

                Assert.AreNotEqual(rangeStartBefore, timeline.RangeStartUtc, "Shift+lăn không cuộn khoảng thời gian");
                Assert.AreNotEqual(firstTickBefore, FirstRulerTickText(),
                    "khoảng thời gian cuộn nhưng nhãn thước đầu tiên không đổi — thước không vẽ lại (UX-02)");
                DisposeFixture();
            }
        }

        /// <summary>
        /// UX-03: kéo cửa sổ từ 700 lên 1920 thì track thước phải bám bề rộng cột. Ngưỡng 95% đúng như kế hoạch: track kẹt ở bề
        /// rộng dự phòng 635px trong cột ~1000px là 63%, ngưỡng 50% của bản đầu tiên vẫn cho nó XANH (R-05).
        /// </summary>
        [UnityTest]
        public IEnumerator ResizeWindow_RulerTrackFollowsColumnWidth()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendar(UxHubWindowFixture.AllSizes[0], language);
                foreach (UxWindowSize size in UxHubWindowFixture.AllSizes)
                {
                    yield return _fixture.Resize(size);
                    VisualElement track = _fixture.Root.Q(className: LiveOpsHubClassNames.TimelineRulerTrack);
                    VisualElement column = _fixture.Root.Q(LiveOpsHubPaths.CalendarElementNames.TimelineColumn);
                    Assert.IsNotNull(track, "không có track thước ở cỡ " + size);
                    Assert.IsNotNull(column, "không có cột timeline ở cỡ " + size);
                    Assert.Greater(track.worldBound.width, column.worldBound.width * RulerTrackMinimumRatio,
                        "ở cỡ " + size + " track thước rộng " + UxLayoutAuditor.Number(track.worldBound.width)
                        + " trong cột rộng " + UxLayoutAuditor.Number(column.worldBound.width) + " — trục không giãn theo cửa sổ (UX-03)");
                }
                DisposeFixture();
            }
        }

        /// <summary>Track thước phải lấp ≥ 95% bề rộng cột — con số của kế hoạch cho <c>Calendar_RulerAndBarsSpanTrack</c>.</summary>
        private const float RulerTrackMinimumRatio = 0.95f;

        // ================================================================================================ UX-04 ô giờ

        /// <summary>
        /// UX-04: gõ giờ mới rồi Enter phải GHI — thanh dời và tài liệu đổi. Trước đợt W8 ô giờ chỉ nghe sự kiện nội bộ nên gõ
        /// xong Enter không có gì xảy ra.
        /// </summary>
        [UnityTest]
        public IEnumerator TimeField_TypeThenEnter_CommitsAndMovesBar()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendarWithSelection(UxHubWindowFixture.AllSizes[4], language);
                LiveOpsUtcDateTimeField field = FirstDateTimeField();
                float barLeftBefore = SelectedBar().worldBound.xMin;
                int revisionBefore = _fixture.Services.Session.DocumentRevision;

                yield return UxEventSender.ReplaceText(_fixture.Window, field.TimeInput, "23:00");
                yield return UxEventSender.PressEnter(_fixture.Window);

                Assert.AreNotEqual(revisionBefore, _fixture.Services.Session.DocumentRevision,
                    "gõ giờ rồi Enter không ghi gì vào tài liệu — ô giờ không nhận phím thật (UX-04)");
                Assert.AreNotEqual(barLeftBefore, SelectedBar().worldBound.xMin,
                    "tài liệu đổi nhưng thanh trên trục không dời — trục không vẽ lại sau khi ghi (UX-04)");
                DisposeFixture();
            }
        }

        /// <summary>UX-04: gõ ngày thiếu số thì GIỮ nguyên giờ đang có và báo lỗi, không im lặng nuốt mất giá trị.</summary>
        [UnityTest]
        public IEnumerator DateField_MissingDigits_KeepsValueAndShowsError()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendarWithSelection(UxHubWindowFixture.AllSizes[4], language);
                LiveOpsUtcDateTimeField field = FirstDateTimeField();
                DateTime valueBefore = field.value;

                yield return UxEventSender.ReplaceText(_fixture.Window, field.DateInput, "2026-09-1");
                yield return UxEventSender.PressEnter(_fixture.Window);

                Assert.IsTrue(field.HasParseError, "ngày thiếu số mà ô không báo lỗi (UX-04)");
                Assert.AreEqual(valueBefore, field.value, "ngày gõ sai đã ghi đè giá trị đang có (UX-04)");
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(field.ErrorLabel), "không có câu lỗi nào hiện cho người dùng (UX-04)");
                DisposeFixture();
            }
        }

        // ================================================================================================ UX-05/UX-14/UX-18 kéo

        /// <summary>UX-05: đang kéo thanh thì KHÔNG được bật hover card — card che đúng chỗ người dùng đang nhắm.</summary>
        [UnityTest]
        public IEnumerator DraggingBar_DoesNotOpenHoverCard()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendar(UxHubWindowFixture.AllSizes[4], language);
                LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
                Vector2 from = bar.worldBound.center;
                Vector2 to = from + new Vector2(60f, 0f);
                bool hoverCardSeenDuringDrag = false;

                yield return UxEventSender.Drag(_fixture.Window, from, to, DragSteps, EventModifiers.None, () => HoldStep());
                // Chờ hết hẹn giờ hiện card SAU khi nhả: lỗi UX-05 gồm cả "vừa thả ra là card bật lên ngay chỗ vừa kéo".
                yield return UxEventSender.Settle(UxEventSender.SettleFrames, HoverCardWaitMilliseconds);

                Assert.IsFalse(hoverCardSeenDuringDrag, "hover card hiện TRONG lúc kéo (UX-05)");
                Assert.IsFalse(_fixture.Calendar.HoverCardHost.IsVisible, "hover card bật lên ngay sau khi thả chuột (UX-05)");
                DisposeFixture();

                IEnumerator HoldStep()
                {
                    yield return UxEventSender.Settle(UxEventSender.SettleFrames, HoverCardWaitMilliseconds);
                    hoverCardSeenDuringDrag = _fixture.Calendar.HoverCardHost.IsVisible;
                }
            }
        }

        /// <summary>
        /// UX-14: kiểm nhanh trong lúc kéo chỉ được nói về ĐỢT ĐANG KÉO. Assert theo EventId (chữ thật trên màn), và chặt hơn
        /// bản đầu tiên: readout không được nhắc tới BẤT KỲ đợt nào khác, không riêng một đợt.
        /// </summary>
        [UnityTest]
        public IEnumerator DragBar_QuickCheckMentionsOnlyDraggedEntry()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendar(UxHubWindowFixture.AllSizes[4], language);
                LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
                string quickCheckText = null;
                bool quickCheckShown = false;

                yield return UxEventSender.Drag(_fixture.Window, bar.worldBound.center, bar.worldBound.center + new Vector2(90f, 0f),
                    DragSteps, EventModifiers.None, () => ReadQuickCheck());

                // Đòi readout CÓ CHỮ trước khi soi nội dung: một Label rỗng làm mọi câu "không được chứa X" đúng một cách rỗng
                // tuếch, đúng kiểu xanh giả mà R-01 tìm ra ở bản đầu tiên.
                Assert.IsTrue(quickCheckShown, "kéo thanh không bật readout kiểm nhanh nào (UX-14)");
                Assert.IsNotEmpty(quickCheckText ?? string.Empty,
                    "readout kiểm nhanh hiện ra nhưng không có chữ nào — người dùng không đọc được kết quả kiểm khi kéo (UX-14)");
                foreach (string eventId in AllDesignEventIds)
                {
                    if (string.Equals(eventId, LavaQuestEarlyEventId, StringComparison.Ordinal)) continue;
                    Assert.IsFalse(quickCheckText.IndexOf(eventId, StringComparison.Ordinal) >= 0,
                        "kiểm nhanh khi kéo '" + LavaQuestEarlyEventId + "' lại nhắc tới đợt '" + eventId + "': \""
                        + quickCheckText + "\" (UX-14)");
                }
                DisposeFixture();

                IEnumerator ReadQuickCheck()
                {
                    yield return UxEventSender.Settle(UxEventSender.SettleFrames, UxEventSender.SettleMilliseconds);
                    Label readout = _fixture.Calendar.Timeline.ReadoutQuickCheckText;
                    quickCheckShown = UxLayoutAuditor.IsShownOnScreen(readout);
                    quickCheckText = readout == null ? null : readout.text;
                }
            }
        }

        /// <summary>
        /// UX-18: readout khi đang kéo phải nằm TRONG track của trục và không đè lên thước — đọc số giờ mà phải đoán xem nó che
        /// vạch nào là không dùng được.
        /// </summary>
        [UnityTest]
        public IEnumerator DraggingBar_ReadoutStaysInsideTrack_NotOverRuler()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendar(UxHubWindowFixture.AllSizes[4], language);
                LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
                Rect readoutBound = Rect.zero;
                bool readoutSeen = false;

                yield return UxEventSender.Drag(_fixture.Window, bar.worldBound.center, bar.worldBound.center + new Vector2(90f, 0f),
                    DragSteps, EventModifiers.None, () => ReadReadout());

                Assert.IsTrue(readoutSeen, "kéo thanh không bật readout nào để đọc số giờ (UX-18)");
                Rect trackBound = _fixture.Calendar.Timeline.Ruler.Track.worldBound;
                Rect rulerBound = _fixture.Calendar.Timeline.Ruler.worldBound;
                Assert.IsTrue(readoutBound.xMin >= trackBound.xMin - 1f && readoutBound.xMax <= trackBound.xMax + 1f,
                    "readout " + readoutBound + " nằm ngoài track " + trackBound + " — số giờ bị cắt ở mép (UX-18)");
                float overlapHeight = Mathf.Min(readoutBound.yMax, rulerBound.yMax) - Mathf.Max(readoutBound.yMin, rulerBound.yMin);
                Assert.LessOrEqual(overlapHeight, 2f,
                    "readout đè lên thước " + UxLayoutAuditor.Number(overlapHeight) + "px — che mất vạch giờ (UX-18)");
                DisposeFixture();

                IEnumerator ReadReadout()
                {
                    yield return UxEventSender.Settle(UxEventSender.SettleFrames, UxEventSender.SettleMilliseconds);
                    VisualElement readout = _fixture.Calendar.Timeline.Readout;
                    readoutSeen = UxLayoutAuditor.IsShownOnScreen(readout);
                    if (readoutSeen) readoutBound = readout.worldBound;
                }
            }
        }

        /// <summary>
        /// UX-06: kéo rút ngắn một đợt ĐANG CHẠY phải hỏi TRƯỚC khi áp. Trả "Giữ" (Safe) thì tài liệu không được đổi và không
        /// để lại bước Undo — trước đợt W8 thay đổi đã ghi xong rồi hộp mới hiện.
        /// </summary>
        [UnityTest]
        public IEnumerator DragShorteningRunningEvent_AsksBeforeApplying()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendar(UxHubWindowFixture.AllSizes[4], language, LiveOpsConfirmResult.Safe);
                LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
                VisualElement handle = bar.Q(className: LiveOpsHubClassNames.TimelineBarEdgeEnd)
                    ?? bar.Q(className: LiveOpsHubClassNames.TimelineBarHandle);
                Assert.IsNotNull(handle, "thanh không có tay cầm mép để kéo rút ngắn");
                int revisionBefore = _fixture.Services.Session.DocumentRevision;
                Vector2 from = handle.worldBound.center;

                yield return UxEventSender.Drag(_fixture.Window, from, from - new Vector2(70f, 0f), DragSteps, EventModifiers.None);

                Assert.Greater(_confirmation.Requests.Count, 0, "kéo rút ngắn đợt đang chạy mà không hỏi gì (UX-06)");
                Assert.AreEqual(revisionBefore, _fixture.Services.Session.DocumentRevision,
                    "người dùng chọn Giữ mà tài liệu vẫn đổi — hộp hỏi SAU khi đã áp (UX-06)");
                DisposeFixture();
            }
        }

        // ================================================================================================ UX-07/09/12/13

        /// <summary>UX-07: ở 820 (breakpoint --medium) chưa chọn đợt nào, người dùng vẫn phải bấm được thanh trên trục.</summary>
        [UnityTest]
        public IEnumerator Medium820_NoSelection_BarIsClickable()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendar(new UxWindowSize(820, 560), language);
                LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
                Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, bar),
                    "ở 820 chưa chọn đợt, con trỏ tại tâm thanh không chạm tới thanh — có lớp nào đè lên trục (UX-07)");

                yield return UxEventSender.Click(_fixture.Window, bar);

                Assert.AreEqual(LiveOpsDesignSample.LavaQuestEarlyEntryKey, _fixture.Calendar.Timeline.SelectedBarKey,
                    "bấm thanh ở 820 không chọn được đợt (UX-07)");
                DisposeFixture();
            }
        }

        /// <summary>
        /// UX-09: nút sửa trong card Vấn đề của inspector phải bấm được BẰNG CHUỘT và bấm xong phải ÁP được cách sửa — "bấm
        /// được" mà tài liệu không đổi thì người dùng vẫn đứng nguyên chỗ cũ.
        /// </summary>
        [UnityTest]
        public IEnumerator InspectorIssueCard_FixButton_AppliesRepair()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendarWithSelection(UxHubWindowFixture.AllSizes[4], language);
                VisualElement issues = _fixture.Root.Q(className: LiveOpsHubClassNames.CalendarInspectorIssues);
                Assert.IsNotNull(issues, "inspector không có card Vấn đề để bấm (UX-09)");
                Button fix = issues.Q<Button>();
                Assert.IsNotNull(fix, "card Vấn đề không có nút sửa nào (UX-09)");
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(fix), "nút sửa của card Vấn đề không hiện ra (UX-09)");
                Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, fix),
                    "nút sửa có trong cây nhưng con trỏ không chạm được — bị lớp khác đè hoặc cha PickingMode.Ignore (UX-09)");
                int revisionBefore = _fixture.Services.Session.DocumentRevision;

                yield return UxEventSender.Click(_fixture.Window, fix);

                Assert.AreNotEqual(revisionBefore, _fixture.Services.Session.DocumentRevision,
                    "bấm nút sửa của card Vấn đề không đổi gì trong tài liệu — cách sửa không được áp (UX-09)");
                DisposeFixture();
            }
        }

        /// <summary>
        /// UX-12: chip "Hiện" của làn đang ẩn phải bấm được và trả làn về — chip dựng bằng Label thì không bấm được. Trạng thái
        /// "đang ẩn một làn" dựng TRƯỚC khi mở cửa sổ vì làn chỉ ẩn được qua menu chuột phải (GenericMenu của IMGUI,
        /// <c>SendEvent</c> không với tới); bản đầu tiên <c>Assert.Ignore</c> ở CẢ hai bản Unity nên không khoá được gì (R-03).
        /// </summary>
        [UnityTest]
        public IEnumerator HiddenLanesChip_IsClickable_AndRestoresLanes()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendarWithHiddenLane(UxHubWindowFixture.AllSizes[4], language);
                Label chip = _fixture.Calendar.Toolbar.HiddenLanesChip;
                Assert.IsNotNull(chip, "toolbar không có chip làn ẩn");
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(chip),
                    "đang ẩn một làn mà chip \"Hiện\" không hiện ra — người dùng không biết mình đang giấu gì (UX-12)");
                Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, chip),
                    "chip \"Hiện\" không nhận được con trỏ — chip là Label không bấm được (UX-12)");

                yield return UxEventSender.Click(_fixture.Window, chip);

                Assert.AreEqual(0, _fixture.Calendar.Presenter.HiddenLanes.Count,
                    "bấm chip \"Hiện\" không trả làn nào về (UX-12)");
                DisposeFixture();
            }
        }

        /// <summary>UX-13: chevron thu gọn làn phải bấm được bằng chuột (trước đợt W8 nó để PickingMode.Ignore).</summary>
        [UnityTest]
        public IEnumerator LaneChevron_IsClickable_AndCollapsesLane()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendar(UxHubWindowFixture.AllSizes[4], language);
                VisualElement chevron = _fixture.Root.Q(className: LiveOpsHubClassNames.TimelineLaneChevron);
                Assert.IsNotNull(chevron, "làn không có chevron thu gọn (UX-13)");
                Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, chevron),
                    "chevron làn không nhận được con trỏ — PickingMode.Ignore hoặc bị cha nuốt sự kiện (UX-13)");
                LiveOpsTimelineLane lane = _fixture.Calendar.Timeline.LaneAt(0);
                bool collapsedBefore = lane.ClassListContains(LiveOpsHubClassNames.TimelineLaneCollapsed);

                yield return UxEventSender.Click(_fixture.Window, chevron);

                Assert.AreNotEqual(collapsedBefore, lane.ClassListContains(LiveOpsHubClassNames.TimelineLaneCollapsed),
                    "bấm chevron không thu gọn/mở làn (UX-13)");
                DisposeFixture();
            }
        }

        // ================================================================================================ UX-10 popover

        /// <summary>
        /// UX-10: lọc còn đúng một loại rồi Enter phải sang bước 2. Popover là cửa sổ RIÊNG, nên sự kiện phải gửi vào cửa sổ đó —
        /// gửi vào hub là thao tác không có thật.
        /// </summary>
        [UnityTest]
        public IEnumerator AddEventPopover_FilterOneType_EnterGoesToNextStep()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendar(UxHubWindowFixture.AllSizes[4], language);
                Button addButton = _fixture.AddEventButton();
                Assert.IsNotNull(addButton, "màn Lịch không có nút Thêm đợt trong phần hành động của header (UX-10)");

                yield return UxEventSender.Click(_fixture.Window, addButton);
                yield return UxEventSender.WaitUntil(() => LiveOpsPopoverContent.Current != null && LiveOpsPopoverContent.Current.IsOpen,
                    "bấm Thêm đợt không mở popover nào (UX-10)");
                EditorWindow popover = LiveOpsPopoverContent.Current.editorWindow;
                VisualElement popoverRoot = popover.rootVisualElement;

                VisualElement filter = popoverRoot.Q(LiveOpsHubPaths.AddEventPopoverElementNames.TypeFilter);
                Assert.IsNotNull(filter, "popover không có ô lọc loại (UX-10)");
                yield return UxEventSender.ReplaceText(popover, filter, "lava");
                yield return UxEventSender.PressEnter(popover);

                VisualElement stepTimes = popoverRoot.Q(LiveOpsHubPaths.AddEventPopoverElementNames.StepTimes);
                Assert.IsNotNull(stepTimes, "popover không có bước chọn giờ");
                Assert.AreNotEqual(DisplayStyle.None, stepTimes.resolvedStyle.display,
                    "lọc còn một loại rồi Enter vẫn đứng ở bước 1 — Enter không đi tiếp (UX-10)");
                DisposeFixture();
            }
        }

        // ================================================================================================ UX-20 rail hẹp

        /// <summary>
        /// UX-20: rail là đường đi tới màn khác và nó phải BẤM được. Bản đầu tiên của cổng mở thẳng mọi màn bằng
        /// <c>OpenWithServices</c> nên đường bấm rail chưa từng chạy một lần nào (R-06).
        /// </summary>
        [UnityTest]
        public IEnumerator Rail_NavigatesBetweenSections_ByClicking()
        {
            yield return OpenCalendar(UxHubWindowFixture.AllSizes[4], LiveOpsHubLanguageId.Vietnamese);

            yield return _fixture.NavigateByRail(LiveOpsHubSections.Ids.RecurringRules);
            Assert.AreEqual(LiveOpsHubSections.Ids.RecurringRules, _fixture.Window.ActiveSectionId,
                "bấm hàng rail không tới được màn Luật lặp (UX-20)");

            yield return _fixture.NavigateByRail(LiveOpsHubSections.Ids.Calendar);
            Assert.AreEqual(LiveOpsHubSections.Ids.Calendar, _fixture.Window.ActiveSectionId,
                "bấm hàng rail không quay lại được màn Lịch (UX-20)");
        }

        /// <summary>
        /// UX-20: ở 700 và 820, lối vào rail của từng màn phải HIỆN và phải CHẠM ĐƯỢC bằng con trỏ — ô cao 0 hoặc bị lớp khác
        /// đè lên là mất đường đi, đúng triệu chứng của UX-20.
        /// <para>
        /// Ở đây chỉ kiểm chạm được, KHÔNG bấm: ô rail thu gọn mở <c>GenericMenu.DropDown</c> (IMGUI) mà phiên tự động không lái
        /// được, và ở 2022.3 batchmode menu còn ở lại chặn mọi test sau. Phần "chọn được màn trong menu đó" là chỗ cổng máy
        /// chưa phủ — khai ở báo cáo gói và mục 14 kế hoạch.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator NarrowRail_SectionEntriesAreReachable()
        {
            foreach (UxWindowSize size in new[] { new UxWindowSize(700, 560), new UxWindowSize(820, 560) })
            {
                yield return OpenCalendar(size, LiveOpsHubLanguageId.Vietnamese);
                foreach (string sectionId in new[]
                {
                    LiveOpsHubSections.Ids.Overview, LiveOpsHubSections.Ids.Calendar, LiveOpsHubSections.Ids.RecurringRules,
                })
                {
                    VisualElement entry = _fixture.RailEntryFor(sectionId);
                    Assert.IsNotNull(entry, "ở cỡ " + size + " rail không có lối vào nào cho màn '" + sectionId + "' (UX-20)");
                    Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(entry),
                        "ở cỡ " + size + " lối vào rail của màn '" + sectionId + "' không hiện ra (UX-20)");
                    Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, entry),
                        "ở cỡ " + size + " con trỏ không chạm được lối vào rail của màn '" + sectionId
                        + "' — bị lớp khác đè hoặc cha nuốt sự kiện (UX-20)");
                }
                DisposeFixture();
            }
        }

        // ================================================================================================ UX-31 thanh bị bỏ

        /// <summary>
        /// UX-31: ở cỡ hẹp, thanh của đợt SẼ BỊ BỎ vẫn phải đọc được id của nó — người dùng không sửa được thứ mình không đọc
        /// được tên.
        /// </summary>
        [UnityTest]
        public IEnumerator NarrowDroppedBar_HasReadableIdLabel()
        {
            yield return OpenCalendar(new UxWindowSize(700, 560), LiveOpsHubLanguageId.Vietnamese);
            LiveOpsTimelineBar droppedBar = null;
            foreach (LiveOpsTimelineBar bar in _fixture.Root.Query<LiveOpsTimelineBar>().ToList())
            {
                if (bar.Q(className: LiveOpsHubClassNames.TimelineDroppedTag) == null) continue;
                droppedBar = bar;
                break;
            }
            Assert.IsNotNull(droppedBar,
                "ở 700 không có thanh nào mang dấu \"sẽ bị bỏ\" — mẫu thiết kế có lava-quest-2026-10 endUtc hỏng, nên thanh đó"
                + " phải vẽ ra thì người dùng mới sửa được (UX-31)");
            Label label = droppedBar.Q<Label>(className: LiveOpsHubClassNames.TimelineBarLabel);
            Assert.IsNotNull(label, "thanh bị bỏ không có nhãn id nào (UX-31)");
            Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(label), "nhãn id của thanh bị bỏ không hiện ở cỡ hẹp (UX-31)");
            Assert.IsNotEmpty(label.text, "nhãn id của thanh bị bỏ rỗng (UX-31)");
        }

        // ================================================================================================ UX-26 Undo

        /// <summary>UX-26: kéo xong rồi ⌘Z thì status bar phải MỜI làm lại (⌘⇧Z), không im lặng.</summary>
        [UnityTest]
        public IEnumerator DragThenUndo_StatusBarOffersRedo()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendar(UxHubWindowFixture.AllSizes[4], language);
                LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
                yield return UxEventSender.Drag(_fixture.Window, bar.worldBound.center,
                    bar.worldBound.center + new Vector2(80f, 0f), DragSteps, EventModifiers.None);
                string statusBefore = _fixture.Window.StatusBar.RightLabel.text;

                yield return UxEventSender.PerformUndo(_fixture.Window);

                Assert.AreNotEqual(statusBefore, _fixture.Window.StatusBar.RightLabel.text,
                    "hoàn tác xong status bar không đổi câu — người dùng không được mời làm lại (UX-26)");
                DisposeFixture();
            }
        }

        // ================================================================================================ trợ giúp

        private void DisposeFixture()
        {
            if (_fixture != null) _fixture.Dispose();
            _fixture = null;
        }

        private IEnumerator OpenCalendar(UxWindowSize size, LiveOpsHubLanguageId language,
            LiveOpsConfirmResult? confirmationResult = null)
        {
            _confirmation = confirmationResult.HasValue
                ? new ScriptedLiveOpsHubConfirmationPresenter(confirmationResult.Value)
                : new ScriptedLiveOpsHubConfirmationPresenter();
            LiveOpsHubServices services = UxHubWindowFixture.DesignServices(_confirmation, DesignNowUtc);
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, size, language, services);
            yield return _fixture.WaitForLayout();
        }

        private IEnumerator OpenCalendarWithSelection(UxWindowSize size, LiveOpsHubLanguageId language)
        {
            yield return OpenCalendar(size, language);
            LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
            yield return UxEventSender.Click(_fixture.Window, bar);
        }

        private IEnumerator OpenCalendarWithHiddenLane(UxWindowSize size, LiveOpsHubLanguageId language)
        {
            _confirmation = new ScriptedLiveOpsHubConfirmationPresenter();
            LiveOpsHubServices services = UxHubWindowFixture.DesignServices(_confirmation, DesignNowUtc);
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, size, language, services, HideOneLane);
            yield return _fixture.WaitForLayout();
        }

        private static void HideOneLane(IReadOnlyList<IHubSection> sections)
        {
            foreach (IHubSection section in sections)
            {
                if (section is CalendarSection calendar) calendar.Presenter.SetHiddenLanes(new[] { HiddenLaneTypeId });
            }
        }

        /// <summary>Làn ẩn của lượt UX-12: loại "star-tournament" chỉ có một đợt nên ẩn nó không làm trục trống.</summary>
        private const string HiddenLaneTypeId = "star-tournament";

        private LiveOpsTimelineBar SelectedBar()
        {
            return _fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
        }

        private LiveOpsUtcDateTimeField FirstDateTimeField()
        {
            LiveOpsUtcDateTimeField field = _fixture.Root.Q<LiveOpsUtcDateTimeField>();
            Assert.IsNotNull(field, "inspector không có ô ngày-giờ UTC nào để gõ");
            return field;
        }

        /// <summary>
        /// Một nấc bánh xe THẬT: delta lấy từ hằng riêng của bộ gửi sự kiện
        /// (<see cref="UxEventSender.OperatingSystemWheelDeltaPerNotch"/>), không từ hằng của control đang kiểm (R-20).
        /// </summary>
        private IEnumerator WheelOneNotch(LiveOpsTimelineElement timeline, int direction, EventModifiers modifiers)
        {
            yield return UxEventSender.Wheel(_fixture.Window, timeline.worldBound.center, direction, modifiers);
        }

        private int RulerTickCount()
        {
            VisualElement track = _fixture.Root.Q(className: LiveOpsHubClassNames.TimelineRulerTrack);
            return track == null ? 0 : track.Query<Label>().ToList().Count;
        }

        private string FirstRulerTickText()
        {
            VisualElement track = _fixture.Root.Q(className: LiveOpsHubClassNames.TimelineRulerTrack);
            Label first = track == null ? null : track.Q<Label>();
            return first == null ? string.Empty : first.text;
        }
    }
}
