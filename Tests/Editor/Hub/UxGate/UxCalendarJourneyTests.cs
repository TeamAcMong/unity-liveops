using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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

        /// <summary>
        /// Mốc "bây giờ" của ca UX-06: nằm GIỮA lava-quest-2026-09b (17/9 00:00 → 20/9 00:00) nên đợt đó đang chạy thật — cùng
        /// đợt mà hợp đồng của gói A dùng. Mốc thiết kế 13/9 01:47 không có đợt nào đang chạy, không dựng được cảnh "rút ngắn
        /// đợt đang chạy" (G-FIX-UX-5).
        /// </summary>
        private static readonly DateTime RunningNowUtc = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);

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

        /// <summary>
        /// Đợt dùng cho MỌI ca kéo của màn Lịch — phải là đợt KÉO ĐƯỢC ở mốc <see cref="DesignNowUtc"/>.
        /// <para>
        /// Sản phẩm CỐ Ý khoá kéo với đợt đã khép, ở ba chỗ độc lập: <c>LiveOpsTimelineBar.IsDraggable</c> (dòng 80),
        /// <c>LiveOpsTimelineDragController.BeginBar</c> (dòng 104 trả <c>false</c> khi <c>bar.IsEnded</c>) và
        /// <c>CalendarTimelinePresenter.PreviewDrag</c> (dòng 356, "Đợt đã khép không dời được (bảng 7.0)").
        /// lava-quest-2026-09a khép lúc 13/9 00:00 mà "bây giờ" của test là 13/9 01:47, nên mọi ca kéo dùng nó đều kéo một thanh
        /// mà màn không cho kéo: không có cử chỉ, không readout, không bước Undo — MÀN ĐÚNG, test trách nhầm (G-UX2-DRAG).
        /// </para>
        /// <para>
        /// lava-quest-2026-09b (17/9 → 20/9) chưa bắt đầu ở mốc đó nên kéo được, và nó giữ nguyên sức nặng của ca UX-14: làn
        /// lava-quest CÓ SẴN một phát hiện Bị bỏ ghi cho đợt KHÁC (lava-quest-2026-10 sai định dạng giờ kết thúc) — đúng cái bẫy
        /// mà readout kiểm nhanh không được đọc lại.
        /// </para>
        /// </summary>
        private const string DraggableEntryKey = LiveOpsDesignSample.LavaQuestMidEntryKey;

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
                string rulerBefore = RulerSignature();

                yield return WheelOneNotch(timeline, -1, UxEventSender.ActionModifier);

                double expected = LiveOpsTimelineGeometry.ClampScale(
                    pixelsPerHourBefore * LiveOpsTimelineGeometry.ZoomFactorPerWheelNotch);
                Assert.AreEqual(expected, timeline.PixelsPerHour, ZoomComparisonTolerance,
                    "một nấc ⌘+lăn phải đổi mức thu phóng đúng một bậc (" + expected + "), đang là " + timeline.PixelsPerHour
                    + " — bánh xe quy đổi sai số nấc (UX-02)");
                Assert.AreNotEqual(rulerBefore, RulerSignature(),
                    "mức thu phóng đổi nhưng thước giữ nguyên chữ và chỗ đặt mọi nhãn: trục không được vẽ lại sau zoom (UX-02)");
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
                string rulerBefore = RulerSignature();

                yield return WheelOneNotch(timeline, 1, EventModifiers.Shift);

                Assert.AreNotEqual(rangeStartBefore, timeline.RangeStartUtc, "Shift+lăn không cuộn khoảng thời gian");
                Assert.AreNotEqual(rulerBefore, RulerSignature(),
                    "khoảng thời gian cuộn nhưng thước giữ nguyên chữ và chỗ đặt mọi nhãn — thước không vẽ lại (UX-02)");
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
                    // Cột timeline = header làn 168px + track [SD1 §3.2]; track chỉ với tới phần CÒN LẠI, nên ngưỡng tính
                    // trên phần đó (G-FIX-UX-4). So thẳng với cả cột là luật không bao giờ đạt: track đúng cũng "thiếu" 168px.
                    float usableWidth = column.worldBound.width - TimelineLaneHeaderWidth;
                    Assert.Greater(track.worldBound.width, usableWidth * RulerTrackMinimumRatio,
                        "ở cỡ " + size + " track thước rộng " + UxLayoutAuditor.Number(track.worldBound.width)
                        + " trong phần dùng được " + UxLayoutAuditor.Number(usableWidth) + " của cột rộng "
                        + UxLayoutAuditor.Number(column.worldBound.width) + " — trục không giãn theo cửa sổ (UX-03)");
                }
                DisposeFixture();
            }
        }

        /// <summary>Track thước phải lấp ≥ 95% bề rộng cột — con số của kế hoạch cho <c>Calendar_RulerAndBarsSpanTrack</c>.</summary>
        private const float RulerTrackMinimumRatio = 0.95f;

        /// <summary>Header làn 168px của timeline [SD1 §3.2] — phần cột mà track không bao giờ với tới (G-FIX-UX-4).</summary>
        private const float TimelineLaneHeaderWidth = 168f;

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
                LiveOpsTimelineBar bar = _fixture.BarOf(DraggableEntryKey);
                Vector2 from = bar.worldBound.center;
                Vector2 to = from + new Vector2(60f, 0f);
                bool hoverCardSeenDuringDrag = false;
                bool dragWasRealDuringHold = false;

                yield return UxEventSender.Drag(_fixture.Window, from, to, DragSteps, EventModifiers.None, () => HoldStep());
                // Chờ hết hẹn giờ hiện card SAU khi nhả: lỗi UX-05 gồm cả "vừa thả ra là card bật lên ngay chỗ vừa kéo".
                yield return UxEventSender.Settle(UxEventSender.SettleFrames, HoverCardWaitMilliseconds);

                // Khẳng định CÓ cử chỉ kéo trước khi kết luận "không có hover card": kéo một thanh màn không cho kéo thì hai câu
                // dưới đúng một cách rỗng tuếch — đúng loại xanh giả mà cổng sinh ra để diệt (G-UX2-DRAG).
                Assert.IsTrue(dragWasRealDuringHold,
                    "giữ chuột giữa cú kéo mà trục không bật readout — chưa có cú kéo nào thì câu \"không có hover card\" không"
                    + " kiểm được gì (UX-05)");
                Assert.IsFalse(hoverCardSeenDuringDrag, "hover card hiện TRONG lúc kéo (UX-05)");
                Assert.IsFalse(_fixture.Calendar.HoverCardHost.IsVisible, "hover card bật lên ngay sau khi thả chuột (UX-05)");
                DisposeFixture();

                IEnumerator HoldStep()
                {
                    yield return UxEventSender.Settle(UxEventSender.SettleFrames, HoverCardWaitMilliseconds);
                    hoverCardSeenDuringDrag = _fixture.Calendar.HoverCardHost.IsVisible;
                    dragWasRealDuringHold = UxLayoutAuditor.IsShownOnScreen(_fixture.Calendar.Timeline.Readout);
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
                LiveOpsTimelineBar bar = _fixture.BarOf(DraggableEntryKey);
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
                    if (string.Equals(eventId, LavaQuestMidEventId, StringComparison.Ordinal)) continue;
                    Assert.IsFalse(quickCheckText.IndexOf(eventId, StringComparison.Ordinal) >= 0,
                        "kiểm nhanh khi kéo '" + LavaQuestMidEventId + "' lại nhắc tới đợt '" + eventId + "': \""
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
                LiveOpsTimelineBar bar = _fixture.BarOf(DraggableEntryKey);
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
                // "Đang chạy" phải ĐÚNG NGHĨA: ở "bây giờ" của thiết kế (13/9 01:47) KHÔNG đợt nào đang chạy — lava-quest-2026-09a
                // kết thúc lúc 13/9 00:00 — nên bản đầu tiên của test kéo một đợt ĐÃ XONG rồi trách màn "không hỏi gì", trong khi
                // màn im lặng là ĐÚNG. Lượt này lấy "bây giờ" là RunningNowUtc = 18/9 12:00, nằm giữa lava-quest-2026-09b
                // (17/9 → 20/9) — ĐÚNG đợt mà ca này kéo — và khẳng định điều kiện đó trước khi kéo, để test không lặng lẽ trôi
                // thành ca khác khi dữ liệu mẫu đổi (G-FIX-UX-5; comment cũ nêu nhầm hunt-0914, sửa ở G-UX2-DRAG soát R6).
                yield return OpenCalendar(UxHubWindowFixture.AllSizes[4], language, LiveOpsConfirmResult.Safe, RunningNowUtc);
                Assert.IsTrue(_fixture.Services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                    out FixedLiveEventEntry running), "mẫu thiết kế phải còn đợt lava-quest-2026-09b để kéo");
                Assert.IsTrue(DateTime.Parse(running.StartUtcText, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal
                    | DateTimeStyles.AssumeUniversal) <= RunningNowUtc
                    && DateTime.Parse(running.EndUtcText, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal
                    | DateTimeStyles.AssumeUniversal) > RunningNowUtc,
                    "điều kiện của ca này: đợt được kéo phải ĐANG CHẠY ở mốc bây giờ của test (UX-06)");
                LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.LavaQuestMidEntryKey);
                VisualElement handle = bar.Q(className: LiveOpsHubClassNames.TimelineBarEdgeEnd)
                    ?? bar.Q(className: LiveOpsHubClassNames.TimelineBarHandle);
                Assert.IsNotNull(handle, "thanh không có tay cầm mép để kéo rút ngắn");
                string endBefore = running.EndUtcText;
                Vector2 from = handle.worldBound.center;

                yield return UxEventSender.Drag(_fixture.Window, from, from - new Vector2(70f, 0f), DragSteps, EventModifiers.None);

                Assert.Greater(_confirmation.Requests.Count, 0, "kéo rút ngắn đợt đang chạy mà không hỏi gì (UX-06)");
                // Đo thứ người dùng thấy — GIỜ KẾT THÚC — chứ không đo số hiệu bản sửa: lần kéo liên tục có ghi nháp rồi trả
                // lại, nên bộ đếm bản sửa nhúc nhích cả khi chọn "Giữ" (đúng hợp đồng gói A:
                // CalendarUxFixTests.Ux06_ShortenRunningDrag_KeepLeavesNoToastNoUndoStep khẳng định giờ kết thúc TRỞ VỀ giá trị
                // cũ). Đo nội dung là đo đúng lời hứa "hỏi TRƯỚC khi áp" (G-FIX-UX-6).
                Assert.IsTrue(_fixture.Services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                    out FixedLiveEventEntry afterKeep), "đợt vừa kéo phải còn trong tài liệu");
                Assert.AreEqual(endBefore, afterKeep.EndUtcText,
                    "người dùng chọn Giữ mà giờ kết thúc vẫn đổi — hộp hỏi SAU khi đã áp (UX-06)");
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
        /// <para>
        /// Cảnh dựng lại ở G-UX2-DRAG. Đợt duy nhất của mẫu thiết kế có cách sửa ÁP ĐƯỢC ngay trong inspector là
        /// lava-quest-2026-10 (<c>endUtc "2026-10-3"</c>: <c>LiveEventUtcText.TryParse</c> đòi có chữ T và phần giờ nên chuỗi đó
        /// KHÔNG đọc được). Vì đọc không ra giờ, <c>LiveOpsTimelineModel.BuildLane</c> (dòng 367) đếm nó vào "không đặt được" rồi
        /// bỏ qua, KHÔNG vẽ thanh nào — ở BẤT KỲ khoảng xem hay mức thu phóng nào. Nên bản trước (đổi mức thu phóng rồi
        /// <c>BarOf("entry-lava-quest-2026-10")</c>) đòi một thanh mà sản phẩm không bao giờ vẽ; đó không phải lỗi khung nhìn.
        /// Đường vào THẬT của người dùng là chip "Không đặt được (n)" ở header làn:
        /// <c>CalendarSection.OnUnplaceableRequested</c> mở pane Danh sách và chọn đúng đợt đó.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator InspectorIssueCard_FixButton_AppliesRepair()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendar(UxHubWindowFixture.AllSizes[4], language);
                VisualElement unplaceableChip = _fixture.Root.Q(className: LiveOpsHubClassNames.TimelineUnplaceableChip);
                Assert.IsNotNull(unplaceableChip,
                    "trục không có chip \"Không đặt được\" — mẫu thiết kế có lava-quest-2026-10 giờ kết thúc không đọc được, nên"
                    + " làn lava-quest phải mang chip đó (UX-09)");
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(unplaceableChip),
                    "chip \"Không đặt được\" không hiện ra — mất đường vào duy nhất tới đợt hỏng (UX-09)");
                Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, unplaceableChip),
                    "con trỏ không chạm được chip \"Không đặt được\" — bị lớp khác đè hoặc cha PickingMode.Ignore (UX-09)");

                yield return UxEventSender.Click(_fixture.Window, unplaceableChip);

                Assert.AreEqual(LiveOpsDesignSample.LavaQuestLateEntryKey, _fixture.Calendar.Presenter.SelectedBarKey,
                    "bấm chip \"Không đặt được\" không chọn đợt hỏng nào (UX-09)");
                // Chờ HẾT một lượt layout trước khi đo: cú bấm chip vừa đổi pane phải sang Danh sách vừa dựng lại cả inspector,
                // nên các nút trong card Vấn đề là element MỚI và worldBound của chúng chưa có thật ngay sau Settle ngắn của Click.
                yield return _fixture.WaitForLayout();
                Foldout issues = _fixture.Root.Q<Foldout>(className: LiveOpsHubClassNames.CalendarInspectorIssues);
                Assert.IsNotNull(issues, "inspector không có card Vấn đề để bấm (UX-09)");
                yield return OpenIssuesCard(issues);
                // MỌI nút trong card đều phải hiện ra và chạm được bằng chuột, không riêng nút áp được: đợt lava-quest-2026-10
                // còn mang nút Đề xuất của hàng phát hiện, và chính hàng ngang đó là chỗ C4 từng đẩy nút ra ngoài pane 280px.
                // Kiểm cả hai ngay trong hàm tìm nút, nếu không độ phủ của UX-09 với nút Đề xuất tụt về 0 (soát R2).
                Button fix = ApplyRepairButtonIn(issues, _fixture.Window);
                Assert.IsNotNull(fix, "card Vấn đề không có nút nào ÁP được cách sửa (UX-09)");
                int revisionBefore = _fixture.Services.Session.DocumentRevision;

                yield return UxEventSender.Click(_fixture.Window, fix);

                Assert.AreNotEqual(revisionBefore, _fixture.Services.Session.DocumentRevision,
                    "bấm nút sửa của card Vấn đề không đổi gì trong tài liệu — cách sửa không được áp (UX-09)");
                // Đo thứ NGƯỜI DÙNG cần — giờ kết thúc đọc được — chứ không chỉ đo số hiệu bản sửa: một cú ghi bất kỳ cũng làm
                // bộ đếm nhúc nhích mà đợt vẫn hỏng y nguyên.
                Assert.IsTrue(_fixture.Services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestLateEntryKey,
                    out FixedLiveEventEntry repaired), "đợt vừa sửa phải còn trong tài liệu (UX-09)");
                Assert.IsTrue(repaired.TryGetEndUtc(out DateTime _),
                    "tài liệu đổi nhưng giờ kết thúc vẫn không đọc được — nút sửa ghi sai chỗ (UX-09)");
                DisposeFixture();
            }
        }

        /// <summary>
        /// Mở card Vấn đề BẰNG CHUỘT khi nó đang gập — đúng cử chỉ người dùng làm, và là bước bắt buộc để ca UX-09 đo được các
        /// nút bên trong.
        /// <para>
        /// Vì sao card có thể đang gập dù đợt đang chọn CÓ vấn đề: <c>CalendarEventInspector.BuildIssuesFoldout</c> đặt
        /// <c>value = Findings.Count &gt; 0</c> nhưng cũng đặt <c>viewDataKey</c>, mà viewData của Unity khôi phục trạng thái
        /// gập/mở SAU khi element gắn vào panel và ghi đè giá trị khởi tạo. Khoá viewData lại dùng CHUNG cho mọi đợt, nên chỉ
        /// cần trước đó xem một đợt SẠCH (card gập đúng) là đợt hỏng kế tiếp cũng mở ra ở trạng thái gập.
        /// </para>
        /// <para>
        /// NỢ W9-UX09-FOLDOUT (mở phiếu, KHÔNG sửa ở gói này): <c>CalendarEventInspector.cs</c> nằm ngoài quyền ghi của
        /// G-UX2-DRAG (ownership.tsv: G-CALENDAR, G-CALENDAR-DEPTH, G-OPT-TIMELINE). Ý định "có vấn đề thì mở sẵn" đang bị
        /// viewData nuốt im lặng — phiếu W9 phải quyết: bỏ viewDataKey, hay đổi sang khoá theo từng đợt.
        /// </para>
        /// </summary>
        private IEnumerator OpenIssuesCard(Foldout issues)
        {
            if (issues.value) yield break;
            Toggle header = issues.Q<Toggle>();
            Assert.IsNotNull(header, "card Vấn đề đang gập mà không có tiêu đề nào để bấm mở (UX-09)");
            Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(header), "tiêu đề card Vấn đề không hiện ra (UX-09)");
            Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, header),
                "con trỏ không chạm được tiêu đề card Vấn đề — card gập rồi thì không còn đường nào tới nút sửa (UX-09)");

            yield return UxEventSender.Click(_fixture.Window, header);

            yield return _fixture.WaitForLayout();
            Assert.IsTrue(issues.value, "bấm tiêu đề card Vấn đề không mở được card (UX-09)");
        }

        /// <summary>
        /// Nút trong card Vấn đề thực sự ÁP cách sửa. Nút của một phát hiện (Proposal) nằm TRONG card phát hiện
        /// (<see cref="LiveOpsHubClassNames.FindingRow"/>) và CỐ Ý không áp gì — nó mở popover Đề xuất của màn Kiểm lịch
        /// (mục 12 I-4, <c>CalendarEventInspector.BuildRepairButton</c>). Nút áp được đặt thẳng trong foldout
        /// (<c>CalendarEventInspector.BuildUnreadableFixButton</c>). Phân biệt bằng CHA chứ không bằng thứ tự: thêm một phát
        /// hiện nữa cho đợt đó là thứ tự đổi ngay.
        /// <para>
        /// Vừa duyệt vừa KHẲNG ĐỊNH: mọi nút gặp trên đường phải hiện ra và con trỏ phải chạm tới — lời hứa "bấm được BẰNG
        /// CHUỘT" của UX-09 nói về cả nút Đề xuất lẫn nút áp được. Bản trước chỉ <c>continue</c> qua nút Đề xuất nên nó không
        /// còn được kiểm ở đâu nữa (soát R2).
        /// </para>
        /// <para>
        /// Leo CHA tới gốc chứ không hỏi mỗi cha trực tiếp: nút Đề xuất hôm nay là con thẳng của card, nhưng chỉ cần bọc thêm
        /// một hàng nút là câu hỏi "cha có phải FindingRow không" trả lời sai và test đi bấm nhầm nút không áp gì.
        /// </para>
        /// </summary>
        private static Button ApplyRepairButtonIn(VisualElement issues, EditorWindow window)
        {
            Button applyButton = null;
            foreach (Button candidate in issues.Query<Button>().ToList())
            {
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(candidate),
                    "nút \"" + candidate.text + "\" trong card Vấn đề không hiện ra (UX-09) — "
                    + AncestryDiagnostic(candidate));
                Assert.IsTrue(UxEventSender.PickReaches(window, candidate),
                    "nút \"" + candidate.text + "\" của card Vấn đề có trong cây nhưng con trỏ không chạm được — bị lớp khác"
                    + " đè hoặc cha PickingMode.Ignore (UX-09)");
                if (applyButton == null && !IsInsideFindingRow(candidate, issues)) applyButton = candidate;
            }
            return applyButton;
        }

        /// <summary>
        /// Chuỗi chẩn đoán "vì sao không hiện": kích thước + display/visibility/opacity của element và MỌI cha của nó. Không có
        /// nó thì câu đỏ chỉ nói "không hiện ra" và người đọc phải mở lại Unity bằng tay mới biết tầng nào tắt.
        /// </summary>
        private static string AncestryDiagnostic(VisualElement element)
        {
            StringBuilder builder = new StringBuilder();
            for (VisualElement current = element; current != null; current = current.hierarchy.parent)
            {
                Rect bound = current.worldBound;
                builder.Append(UxLayoutAuditor.Describe(current)).Append(" [")
                    .Append(UxLayoutAuditor.Number(bound.width)).Append('x').Append(UxLayoutAuditor.Number(bound.height))
                    .Append(" display=").Append(current.resolvedStyle.display)
                    .Append(" visibility=").Append(current.resolvedStyle.visibility)
                    .Append(" opacity=").Append(UxLayoutAuditor.Number(current.resolvedStyle.opacity))
                    .Append("] < ");
            }
            return builder.ToString();
        }

        /// <summary>Element có nằm trong một card phát hiện nào không — leo cha tới <paramref name="root"/> rồi dừng.</summary>
        private static bool IsInsideFindingRow(VisualElement element, VisualElement root)
        {
            for (VisualElement ancestor = element.parent; ancestor != null && ancestor != root; ancestor = ancestor.parent)
            {
                if (ancestor.ClassListContains(LiveOpsHubClassNames.FindingRow)) return true;
            }
            return false;
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
                VisualElement chip = _fixture.Calendar.Toolbar.HiddenLanesChip;
                Assert.IsNotNull(chip, "toolbar không có chip làn ẩn");
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(chip),
                    "đang ẩn một làn mà chip \"Hiện\" không hiện ra — người dùng không biết mình đang giấu gì (UX-12)");

                // Cổng đợt: gói A tách chip thành NHÃN + NÚT, nên phần phải bấm được là chính cái nút, không phải cả chip.
                // Vẫn hỏi qua cây con của chip (Q) chứ không chỉ qua ô nhớ của toolbar: nút phải THẬT SỰ nằm trong chip
                // người dùng nhìn thấy, chứ không phải một nút mồ côi ở chỗ khác.
                Button show = chip.Q<Button>();
                Assert.IsNotNull(show, "chip làn ẩn không có nút bấm nào — chữ \"Hiện\" nằm trong Label thì không bấm được (UX-12)");
                Assert.AreSame(_fixture.Calendar.Toolbar.HiddenLanesShowButton, show,
                    "nút trong chip không phải nút \"Hiện\" mà màn nối vào ShowAllLanes (UX-12)");
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(show), "nút \"Hiện\" của chip làn ẩn không hiện ra (UX-12)");
                Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, show),
                    "nút \"Hiện\" không nhận được con trỏ — bị lớp khác đè hoặc cha PickingMode.Ignore (UX-12)");

                yield return UxEventSender.Click(_fixture.Window, show);

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
        /// UX-31: ở cỡ hẹp, đợt SẼ BỊ BỎ vẫn phải ĐỌC ĐƯỢC id của nó — người dùng không sửa được thứ mình không đọc được tên.
        /// <para>
        /// Cảnh dựng lại ở G-UX2-DRAG, vì bản trước sai hai chỗ độc lập. (1) Nó tìm lava-quest-2026-10, mà đợt đó giờ kết thúc
        /// không đọc được nên <c>LiveOpsTimelineModel.BuildLane</c> xếp vào "không đặt được" và KHÔNG vẽ thanh; đợt "bị bỏ" CÓ
        /// thanh của mẫu thiết kế là hunt-0916-bonus (chồng 12 giờ với hunt-0914 nên biên dịch trả <c>IsKept == false</c>).
        /// (2) Nó hỏi tag qua <c>droppedBar.Q(TimelineDroppedTag)</c>, nhưng tag là con của LÀN
        /// (<c>LiveOpsTimelineLane.DroppedTagAt</c> gọi <c>Add(tag)</c> trên chính làn), không phải con của thanh — câu hỏi đó
        /// không bao giờ tìm thấy gì.
        /// </para>
        /// <para>
        /// Hợp đồng THẬT nằm ở <c>LiveOpsTimelineLane.DroppedTagTextFor</c>: thanh đủ rộng thì tự mang nhãn id và tag chỉ nói
        /// trạng thái; thanh hẹp (nhãn thân rỗng) thì TAG phải nói luôn id. Nên assert đúng là "id đọc được ở MỘT trong hai
        /// chỗ", chứ không phải "nhãn thân thanh khác rỗng" — ở 700 thanh hunt-0916-bonus chỉ rộng ~34px, nhãn thân rỗng là
        /// ĐÚNG thiết kế.
        /// </para>
        /// <para>
        /// GHIM thanh hunt-0916-bonus và khẳng định nó thật sự HẸP trước khi đo (soát R3). Bản trước lấy "thanh bị bỏ đầu tiên
        /// gặp được" nên ca này trôi theo dữ liệu mẫu: thanh rộng ≥ <c>LabelFullIdMinimumBarWidth</c> (64px) thì nhãn thân đã
        /// mang id đầy đủ và nhánh tag không bao giờ chạy — ca xanh mà chẳng kiểm gì.
        /// </para>
        /// <para>
        /// NỢ W9-UX31-NUMERIC (mở phiếu, KHÔNG sửa ở đợt này): dải 24px ≤ rộng &lt; 64px với id có hậu tố SỐ
        /// (<c>LiveOpsTimelineGeometry.BarLabel</c> trả "0916", "pass-38" → "38") thì nhãn thân KHÁC RỖNG nên
        /// <c>DroppedTagTextFor</c> bỏ id khỏi tag — người dùng chỉ đọc được con số, không đọc được id. Mẫu thiết kế hiện
        /// không có thanh bị bỏ nào rơi vào dải đó nên ca này không dựng được cảnh; ghi phiếu thay vì nới assert.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator NarrowDroppedBar_HasReadableIdLabel()
        {
            yield return OpenCalendar(new UxWindowSize(700, 560), LiveOpsHubLanguageId.Vietnamese);
            LiveOpsTimelineBar droppedBar = _fixture.BarOf(LiveOpsDesignSample.HuntBonusEntryKey);
            Assert.IsNotNull(droppedBar.Model, "thanh hunt-0916-bonus chưa gắn model (UX-31)");
            Assert.IsTrue(droppedBar.Model.IsDropped,
                "điều kiện của ca này: hunt-0916-bonus chồng 12 giờ với hunt-0914 nên biên dịch phải xếp nó vào nhóm bị bỏ"
                + " (UX-31)");
            Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(droppedBar),
                "ở 700 thanh của đợt bị bỏ không vẽ ra — người dùng không sửa được thứ mình không thấy (UX-31)");
            Assert.Less(droppedBar.Width, LiveOpsTimelineGeometry.LabelFullIdMinimumBarWidth,
                "điều kiện của ca này: thanh phải HẸP (< " + UxLayoutAuditor.Number(LiveOpsTimelineGeometry.LabelFullIdMinimumBarWidth)
                + "px) thì nhãn thân mới không mang id và tag mới phải nói thay — thanh đang rộng "
                + UxLayoutAuditor.Number(droppedBar.Width) + "px (UX-31)");
            string eventId = droppedBar.Model.EventId;
            Assert.IsNotEmpty(eventId, "thanh bị bỏ không mang id nào để đọc (UX-31)");

            Label barLabel = droppedBar.Q<Label>(className: LiveOpsHubClassNames.TimelineBarLabel);
            bool idReadableInBar = barLabel != null && UxLayoutAuditor.IsShownOnScreen(barLabel)
                && barLabel.text.IndexOf(eventId, StringComparison.Ordinal) >= 0;
            LiveOpsTimelineLane lane = droppedBar.GetFirstAncestorOfType<LiveOpsTimelineLane>();
            Assert.IsNotNull(lane, "thanh bị bỏ không nằm trong làn nào (UX-31)");
            bool idReadableInTag = false;
            StringBuilder shownTagTexts = new StringBuilder();
            foreach (VisualElement tag in lane.Query(className: LiveOpsHubClassNames.TimelineDroppedTag).ToList())
            {
                if (!UxLayoutAuditor.IsShownOnScreen(tag)) continue;
                Label tagLabel = tag.Q<Label>();
                string tagText = tagLabel == null ? string.Empty : tagLabel.text;
                shownTagTexts.Append('"').Append(tagText).Append("\" ");
                if (tagText.IndexOf(eventId, StringComparison.Ordinal) >= 0) idReadableInTag = true;
            }
            Assert.IsTrue(idReadableInBar || idReadableInTag,
                "ở 700 id \"" + eventId + "\" của đợt bị bỏ không đọc được ở đâu cả — nhãn thân thanh đang là \""
                + (barLabel == null ? string.Empty : barLabel.text) + "\", tag bị bỏ đang hiện: "
                + (shownTagTexts.Length == 0 ? "(không có)" : shownTagTexts.ToString()) + "(UX-31)");
        }

        // ================================================================================================ UX-26 Undo

        /// <summary>
        /// UX-26: kéo xong rồi ⌘Z thì status bar phải MỜI làm lại (⌘⇧Z), không im lặng.
        /// <para>
        /// Câu thao tác gần nhất nằm ở vế TRÁI của status bar, không phải vế phải: <c>LiveOpsHubStatusBarModel.Build</c> nối nó
        /// vào <c>leftText</c> qua <c>AppendRecentAction</c>, còn <c>BuildRightText</c> chỉ dựng "giờ UTC · đã đăng … · sha …"
        /// và không đọc thao tác nào. Bản trước của test đọc <c>StatusBar.RightLabel</c> nên nó đo một ô KHÔNG BAO GIỜ đổi theo
        /// Undo — chữ nó in ra khi đỏ ("13/9 01:47 UTC · đã đăng 11/9 16:20 · sha 5eecb8") đúng là vế phải (G-UX2-DRAG).
        /// </para>
        /// <para>
        /// Và đo THỂ CÂU chứ không chỉ đo "chữ có đổi": vế trái còn mang câu về lần kiểm, mà lần kiểm chạy lại sau mỗi lần sửa
        /// nên chữ đổi cả khi lời mời làm lại không bao giờ hiện. Hai mốc so là phần chữ cố định của chính hai chuỗi định dạng
        /// trong catalog, nên ca này chạy đúng ở cả vi lẫn en.
        /// </para>
        /// <para>
        /// Riêng tiền tố "Vừa hoàn tác: " KHÔNG đủ (soát R1): hai chuỗi <c>ShellStatusUndoneActionFormat</c> và
        /// <c>ShellStatusUndoneActionWithKeyFormat</c> mở đầu GIỐNG HỆT nhau, mà <c>LiveOpsHubStatusBarModel.BuildRecentAction</c>
        /// rơi về bản KHÔNG phím khi nhãn phím Làm lại rỗng. Mất hẳn "(⌘⇧Z để làm lại)" mà ca vẫn xanh — đúng thứ UX-26 hứa.
        /// Nên đo thêm phần chữ cố định ĐUÔI của bản có phím và chính nhãn phím mà cửa sổ truyền vào
        /// (<c>LiveOpsHubKeyLabels.Redo</c>).
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator DragThenUndo_StatusBarOffersRedo()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenCalendar(UxHubWindowFixture.AllSizes[4], language);
                LiveOpsTimelineBar bar = _fixture.BarOf(DraggableEntryKey);
                int revisionBeforeDrag = _fixture.Services.Session.DocumentRevision;
                yield return UxEventSender.Drag(_fixture.Window, bar.worldBound.center,
                    bar.worldBound.center + new Vector2(80f, 0f), DragSteps, EventModifiers.None);
                // Có GHI thì ⌘Z mới có việc để làm: kéo một thanh màn không cho kéo thì mọi câu dưới đây trách status bar vì một
                // bước Undo chưa từng tồn tại (G-UX2-DRAG).
                Assert.AreNotEqual(revisionBeforeDrag, _fixture.Services.Session.DocumentRevision,
                    "kéo xong mà tài liệu không đổi — chưa có gì để hoàn tác thì ca này không kiểm được lời mời làm lại (UX-26)");
                Label statusLeft = _fixture.Window.StatusBar.LeftLabel;
                string statusBefore = statusLeft.text;
                string justDidPrefix = FixedPrefixOf(LiveOpsHubStrings.ShellStatusRecentActionFormat);
                string undonePrefix = FixedPrefixOf(LiveOpsHubStrings.ShellStatusUndoneActionFormat);
                Assert.IsTrue(statusBefore.IndexOf(justDidPrefix, StringComparison.Ordinal) >= 0,
                    "kéo xong status bar không nói vừa làm gì: \"" + statusBefore + "\" (UX-26)");

                yield return UxEventSender.PerformUndo(_fixture.Window);

                string statusAfter = statusLeft.text;
                Assert.AreNotEqual(statusBefore, statusAfter,
                    "hoàn tác xong status bar không đổi câu — người dùng không được mời làm lại (UX-26)");
                Assert.IsTrue(statusAfter.IndexOf(undonePrefix, StringComparison.Ordinal) >= 0,
                    "hoàn tác xong status bar vẫn không nói vừa hoàn tác: \"" + statusAfter + "\" (UX-26)");
                string redoKeyLabel = LiveOpsHubKeyLabels.Redo;
                Assert.IsNotEmpty(redoKeyLabel,
                    "Unity này không gán phím nào cho Làm lại nên không có lời mời nào để kiểm — ca UX-26 mất nghĩa, xem lại"
                    + " môi trường chứ đừng nới assert");
                Assert.IsTrue(statusAfter.IndexOf(redoKeyLabel, StringComparison.Ordinal) >= 0,
                    "hoàn tác xong status bar không nêu phím Làm lại \"" + redoKeyLabel + "\": \"" + statusAfter + "\" (UX-26)");
                string redoInvitationTail = FixedTailOf(LiveOpsHubStrings.ShellStatusUndoneActionWithKeyFormat);
                Assert.IsTrue(statusAfter.IndexOf(redoInvitationTail, StringComparison.Ordinal) >= 0,
                    "hoàn tác xong status bar vẫn không MỜI làm lại (thiếu \"" + redoInvitationTail + "\"): \"" + statusAfter
                    + "\" (UX-26)");
                DisposeFixture();
            }
        }

        /// <summary>
        /// Phần chữ cố định đứng trước <c>{0}</c> của một chuỗi định dạng — dùng để nhận ra THỂ CÂU (vừa làm / vừa hoàn tác) mà
        /// không cần dựng lại cả câu và không phụ thuộc ngôn ngữ đang chạy.
        /// </summary>
        private static string FixedPrefixOf(string format)
        {
            int placeholderIndex = format.IndexOf("{0}", StringComparison.Ordinal);
            return placeholderIndex <= 0 ? format : format.Substring(0, placeholderIndex);
        }

        /// <summary>
        /// Phần chữ cố định đứng SAU chỗ điền cuối cùng của một chuỗi định dạng (" để làm lại)" / " to redo)") — thứ phân biệt
        /// bản CÓ nhãn phím với bản không, mà tiền tố thì không phân biệt được vì hai bản mở đầu giống hệt nhau.
        /// </summary>
        private static string FixedTailOf(string format)
        {
            int closingIndex = format.LastIndexOf('}');
            return closingIndex < 0 || closingIndex + 1 >= format.Length ? format : format.Substring(closingIndex + 1);
        }

        // ================================================================================================ trợ giúp

        private void DisposeFixture()
        {
            if (_fixture != null) _fixture.Dispose();
            _fixture = null;
        }

        private IEnumerator OpenCalendar(UxWindowSize size, LiveOpsHubLanguageId language,
            LiveOpsConfirmResult? confirmationResult = null, DateTime? nowUtc = null)
        {
            _confirmation = confirmationResult.HasValue
                ? new ScriptedLiveOpsHubConfirmationPresenter(confirmationResult.Value)
                : new ScriptedLiveOpsHubConfirmationPresenter();
            LiveOpsHubServices services = UxHubWindowFixture.DesignServices(_confirmation, nowUtc ?? DesignNowUtc);
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, size, language, services);
            yield return _fixture.WaitForLayout();
        }

        private IEnumerator OpenCalendarWithSelection(UxWindowSize size, LiveOpsHubLanguageId language,
            string entryKey = LiveOpsDesignSample.LavaQuestEarlyEntryKey)
        {
            yield return OpenCalendar(size, language);
            LiveOpsTimelineBar bar = _fixture.BarOf(entryKey);
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

        /// <summary>
        /// Chữ ký của thước: chữ + vị trí x của TỪNG nhãn. Đếm nhãn hay đọc nhãn đầu tiên đều là thước đo quá thô — nhãn được
        /// dùng lại (pool) nên số lượng giữ nguyên khi vẽ lại, và nhãn đầu vẫn có thể trùng chữ sau một nấc cuộn; chữ ký đổi khi
        /// BẤT KỲ nhãn nào đổi chữ hoặc dời chỗ, nên nó nhạy hơn cả hai cách cũ chứ không dễ dãi hơn (G-FIX-UX-5).
        /// </summary>
        private string RulerSignature()
        {
            VisualElement track = _fixture.Root.Q(className: LiveOpsHubClassNames.TimelineRulerTrack);
            if (track == null) return string.Empty;
            List<Label> labels = track.Query<Label>().ToList();
            StringBuilder signature = new StringBuilder();
            for (int index = 0; index < labels.Count; index++)
            {
                signature.Append(labels[index].text).Append('@')
                    .Append(Mathf.RoundToInt(labels[index].worldBound.x)).Append('|');
            }
            return signature.ToString();
        }
    }
}
