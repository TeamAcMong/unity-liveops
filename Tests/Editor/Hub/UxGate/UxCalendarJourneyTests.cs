using System;
using System.Collections;
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

        private UxHubWindowFixture _fixture;
        private ScriptedLiveOpsHubConfirmationPresenter _confirmation;

        [TearDown]
        public void TearDown()
        {
            if (_fixture != null) _fixture.Dispose();
            _fixture = null;
            _confirmation = null;
        }

        // ================================================================================================ UX-02 zoom/cuộn

        /// <summary>UX-02: ⌘+lăn phải ĐỔI mức thu phóng và VẼ LẠI trục — trước đợt W8 model đổi mà thước giữ nguyên nét.</summary>
        [UnityTest]
        public IEnumerator CommandWheel_ZoomsAndRedrawsRuler()
        {
            yield return OpenCalendar(UxHubWindowFixture.AllSizes[3]);
            LiveOpsTimelineElement timeline = _fixture.Calendar.Timeline;
            double pixelsPerHourBefore = timeline.PixelsPerHour;
            int rulerTickCountBefore = RulerTickCount();

            yield return UxEventSender.Wheel(_fixture.Window, timeline.worldBound.center, -3, UxEventSender.ActionModifier);

            Assert.AreNotEqual(pixelsPerHourBefore, timeline.PixelsPerHour,
                "⌘+lăn không đổi mức thu phóng — bánh xe với phím hành động chưa tới control");
            Assert.AreNotEqual(rulerTickCountBefore, RulerTickCount(),
                "mức thu phóng đổi nhưng thước giữ nguyên số nét: trục không được vẽ lại sau zoom (UX-02)");
        }

        /// <summary>UX-02: Shift+lăn cuộn khoảng thời gian và thước phải chạy theo.</summary>
        [UnityTest]
        public IEnumerator ShiftWheel_ScrollsRange_AndRulerFollows()
        {
            yield return OpenCalendar(UxHubWindowFixture.AllSizes[3]);
            LiveOpsTimelineElement timeline = _fixture.Calendar.Timeline;
            DateTime rangeStartBefore = timeline.RangeStartUtc;
            string firstTickBefore = FirstRulerTickText();

            yield return UxEventSender.Wheel(_fixture.Window, timeline.worldBound.center, 3, EventModifiers.Shift);

            Assert.AreNotEqual(rangeStartBefore, timeline.RangeStartUtc, "Shift+lăn không cuộn khoảng thời gian");
            Assert.AreNotEqual(firstTickBefore, FirstRulerTickText(),
                "khoảng thời gian cuộn nhưng nhãn thước đầu tiên không đổi — thước không vẽ lại (UX-02)");
        }

        /// <summary>UX-03: kéo cửa sổ từ 700 lên 1920 thì track thước phải bám bề rộng cột, không kẹt ở bề rộng dự phòng.</summary>
        [UnityTest]
        public IEnumerator ResizeWindow_RulerTrackFollowsColumnWidth()
        {
            yield return OpenCalendar(UxHubWindowFixture.AllSizes[0]);
            foreach (UxWindowSize size in UxHubWindowFixture.AllSizes)
            {
                yield return _fixture.Resize(size);
                VisualElement track = _fixture.Root.Q(className: LiveOpsHubClassNames.TimelineRulerTrack);
                VisualElement column = _fixture.Root.Q(LiveOpsHubPaths.CalendarElementNames.TimelineColumn);
                Assert.IsNotNull(track, "không có track thước ở cỡ " + size);
                Assert.IsNotNull(column, "không có cột timeline ở cỡ " + size);
                Assert.Greater(track.worldBound.width, column.worldBound.width * 0.5f,
                    "ở cỡ " + size + " track thước rộng " + UxLayoutAuditor.Number(track.worldBound.width)
                    + " trong cột rộng " + UxLayoutAuditor.Number(column.worldBound.width) + " — trục không giãn theo cửa sổ (UX-03)");
            }
        }

        // ================================================================================================ UX-04 ô giờ

        /// <summary>
        /// UX-04: gõ giờ mới rồi Enter phải GHI — thanh dời và tài liệu đổi. Trước đợt W8 ô giờ chỉ nghe sự kiện nội bộ nên gõ
        /// xong Enter không có gì xảy ra.
        /// </summary>
        [UnityTest]
        public IEnumerator TimeField_TypeThenEnter_CommitsAndMovesBar()
        {
            yield return OpenCalendarWithSelection(UxHubWindowFixture.AllSizes[4]);
            LiveOpsUtcDateTimeField field = FirstDateTimeField();
            float barLeftBefore = SelectedBar().worldBound.xMin;
            int revisionBefore = _fixture.Services.Session.DocumentRevision;

            yield return UxEventSender.ReplaceText(_fixture.Window, field.TimeInput, "23:00");
            yield return UxEventSender.PressEnter(_fixture.Window);

            Assert.AreNotEqual(revisionBefore, _fixture.Services.Session.DocumentRevision,
                "gõ giờ rồi Enter không ghi gì vào tài liệu — ô giờ không nhận phím thật (UX-04)");
            Assert.AreNotEqual(barLeftBefore, SelectedBar().worldBound.xMin,
                "tài liệu đổi nhưng thanh trên trục không dời — trục không vẽ lại sau khi ghi (UX-04)");
        }

        /// <summary>UX-04: gõ ngày thiếu số thì GIỮ nguyên giờ đang có và báo lỗi, không im lặng nuốt mất giá trị.</summary>
        [UnityTest]
        public IEnumerator DateField_MissingDigits_KeepsValueAndShowsError()
        {
            yield return OpenCalendarWithSelection(UxHubWindowFixture.AllSizes[4]);
            LiveOpsUtcDateTimeField field = FirstDateTimeField();
            DateTime valueBefore = field.value;

            yield return UxEventSender.ReplaceText(_fixture.Window, field.DateInput, "2026-09-1");
            yield return UxEventSender.PressEnter(_fixture.Window);

            Assert.IsTrue(field.HasParseError, "ngày thiếu số mà ô không báo lỗi (UX-04)");
            Assert.AreEqual(valueBefore, field.value, "ngày gõ sai đã ghi đè giá trị đang có (UX-04)");
            Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(field.ErrorLabel), "không có câu lỗi nào hiện cho người dùng (UX-04)");
        }

        // ================================================================================================ UX-05/UX-14 kéo

        /// <summary>UX-05: đang kéo thanh thì KHÔNG được bật hover card — card che đúng chỗ người dùng đang nhắm.</summary>
        [UnityTest]
        public IEnumerator DraggingBar_DoesNotOpenHoverCard()
        {
            yield return OpenCalendar(UxHubWindowFixture.AllSizes[4]);
            LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
            Vector2 from = bar.worldBound.center;
            Vector2 to = from + new Vector2(60f, 0f);
            bool hoverCardSeenDuringDrag = false;

            yield return UxEventSender.Drag(_fixture.Window, from, to, DragSteps, EventModifiers.None, () => HoldStep());
            // Chờ hết hẹn giờ hiện card SAU khi nhả: lỗi UX-05 gồm cả "vừa thả ra là card bật lên ngay chỗ vừa kéo".
            yield return UxEventSender.Settle(UxEventSender.SettleFrames, HoverCardWaitMilliseconds);

            Assert.IsFalse(hoverCardSeenDuringDrag, "hover card hiện TRONG lúc kéo (UX-05)");
            Assert.IsFalse(_fixture.Calendar.HoverCardHost.IsVisible, "hover card bật lên ngay sau khi thả chuột (UX-05)");

            IEnumerator HoldStep()
            {
                yield return UxEventSender.Settle(UxEventSender.SettleFrames, HoverCardWaitMilliseconds);
                hoverCardSeenDuringDrag = _fixture.Calendar.HoverCardHost.IsVisible;
            }
        }

        /// <summary>UX-14: kiểm nhanh trong lúc kéo chỉ được nói về ĐỢT ĐANG KÉO, không kể lỗi của đợt khác.</summary>
        [UnityTest]
        public IEnumerator DragBar_QuickCheckMentionsOnlyDraggedEntry()
        {
            yield return OpenCalendar(UxHubWindowFixture.AllSizes[4]);
            LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
            string quickCheckText = null;

            yield return UxEventSender.Drag(_fixture.Window, bar.worldBound.center, bar.worldBound.center + new Vector2(90f, 0f),
                DragSteps, EventModifiers.None, () => ReadQuickCheck());

            Assert.IsNotNull(quickCheckText, "kéo thanh không bật readout kiểm nhanh nào (UX-14)");
            Assert.IsFalse(quickCheckText.IndexOf(LiveOpsDesignSample.StarTournamentEntryKey, StringComparison.Ordinal) >= 0,
                "kiểm nhanh khi kéo nhắc tới đợt KHÔNG kéo: \"" + quickCheckText + "\" (UX-14)");

            IEnumerator ReadQuickCheck()
            {
                yield return UxEventSender.Settle(UxEventSender.SettleFrames, UxEventSender.SettleMilliseconds);
                Label readout = _fixture.Calendar.Timeline.ReadoutQuickCheckText;
                quickCheckText = readout == null ? null : readout.text;
            }
        }

        /// <summary>
        /// UX-06: kéo rút ngắn một đợt ĐANG CHẠY phải hỏi TRƯỚC khi áp. Trả "Giữ" (Safe) thì tài liệu không được đổi và không
        /// để lại bước Undo — trước đợt W8 thay đổi đã ghi xong rồi hộp mới hiện.
        /// </summary>
        [UnityTest]
        public IEnumerator DragShorteningRunningEvent_AsksBeforeApplying()
        {
            yield return OpenCalendar(UxHubWindowFixture.AllSizes[4], LiveOpsConfirmResult.Safe);
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
        }

        // ================================================================================================ UX-07/09/12/13

        /// <summary>UX-07: ở 820 (breakpoint --medium) chưa chọn đợt nào, người dùng vẫn phải bấm được thanh trên trục.</summary>
        [UnityTest]
        public IEnumerator Medium820_NoSelection_BarIsClickable()
        {
            yield return OpenCalendar(new UxWindowSize(820, 560));
            LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
            Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, bar),
                "ở 820 chưa chọn đợt, con trỏ tại tâm thanh không chạm tới thanh — có lớp nào đè lên trục (UX-07)");

            yield return UxEventSender.Click(_fixture.Window, bar);

            Assert.AreEqual(LiveOpsDesignSample.LavaQuestEarlyEntryKey, _fixture.Calendar.Timeline.SelectedBarKey,
                "bấm thanh ở 820 không chọn được đợt (UX-07)");
        }

        /// <summary>UX-09: nút sửa trong card Vấn đề của inspector phải bấm được bằng chuột, không chỉ "có trong cây".</summary>
        [UnityTest]
        public IEnumerator InspectorIssueCard_FixButton_IsClickable()
        {
            yield return OpenCalendarWithSelection(UxHubWindowFixture.AllSizes[4]);
            VisualElement issues = _fixture.Root.Q(className: LiveOpsHubClassNames.CalendarInspectorIssues);
            Assert.IsNotNull(issues, "inspector không có card Vấn đề để bấm (UX-09)");
            Button fix = issues.Q<Button>();
            Assert.IsNotNull(fix, "card Vấn đề không có nút sửa nào (UX-09)");
            Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(fix), "nút sửa của card Vấn đề không hiện ra (UX-09)");
            Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, fix),
                "nút sửa có trong cây nhưng con trỏ không chạm được — bị lớp khác đè hoặc cha PickingMode.Ignore (UX-09)");
            yield return UxEventSender.Click(_fixture.Window, fix);
        }

        /// <summary>UX-12: chip "Hiện" của làn đang ẩn phải bấm được và trả làn về — chip dựng bằng Label thì không bấm được.</summary>
        [UnityTest]
        public IEnumerator HiddenLanesChip_IsClickable()
        {
            yield return OpenCalendar(UxHubWindowFixture.AllSizes[4]);
            Label chip = _fixture.Calendar.Toolbar.HiddenLanesChip;
            Assert.IsNotNull(chip, "toolbar không có chip làn ẩn");
            if (!UxLayoutAuditor.IsShownOnScreen(chip)) Assert.Ignore("mẫu thiết kế không có làn nào đang ẩn ở lượt này");
            Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, chip),
                "chip \"Hiện\" không nhận được con trỏ — chip là Label không bấm được (UX-12)");
            yield return UxEventSender.Click(_fixture.Window, chip);
        }

        /// <summary>UX-13: chevron thu gọn làn phải bấm được bằng chuột (trước đợt W8 nó để PickingMode.Ignore).</summary>
        [UnityTest]
        public IEnumerator LaneChevron_IsClickable_AndCollapsesLane()
        {
            yield return OpenCalendar(UxHubWindowFixture.AllSizes[4]);
            VisualElement chevron = _fixture.Root.Q(className: LiveOpsHubClassNames.TimelineLaneChevron);
            Assert.IsNotNull(chevron, "làn không có chevron thu gọn (UX-13)");
            Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, chevron),
                "chevron làn không nhận được con trỏ — PickingMode.Ignore hoặc bị cha nuốt sự kiện (UX-13)");
            LiveOpsTimelineLane lane = _fixture.Calendar.Timeline.LaneAt(0);
            bool collapsedBefore = lane.ClassListContains(LiveOpsHubClassNames.TimelineLaneCollapsed);

            yield return UxEventSender.Click(_fixture.Window, chevron);

            Assert.AreNotEqual(collapsedBefore, lane.ClassListContains(LiveOpsHubClassNames.TimelineLaneCollapsed),
                "bấm chevron không thu gọn/mở làn (UX-13)");
        }

        // ================================================================================================ UX-10 popover

        /// <summary>
        /// UX-10: lọc còn đúng một loại rồi Enter phải sang bước 2. Popover là cửa sổ RIÊNG, nên sự kiện phải gửi vào cửa sổ đó —
        /// gửi vào hub là thao tác không có thật.
        /// </summary>
        [UnityTest]
        public IEnumerator AddEventPopover_FilterOneType_EnterGoesToNextStep()
        {
            yield return OpenCalendar(UxHubWindowFixture.AllSizes[4]);
            Button addButton = FindAddEventButton();
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
        }

        // ================================================================================================ UX-26 Undo

        /// <summary>UX-26: kéo xong rồi ⌘Z thì status bar phải MỜI làm lại (⌘⇧Z), không im lặng.</summary>
        [UnityTest]
        public IEnumerator DragThenUndo_StatusBarOffersRedo()
        {
            yield return OpenCalendar(UxHubWindowFixture.AllSizes[4]);
            LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
            yield return UxEventSender.Drag(_fixture.Window, bar.worldBound.center,
                bar.worldBound.center + new Vector2(80f, 0f), DragSteps, EventModifiers.None);
            string statusBefore = _fixture.Window.StatusBar.RightLabel.text;

            yield return UxEventSender.PerformUndo(_fixture.Window);

            Assert.AreNotEqual(statusBefore, _fixture.Window.StatusBar.RightLabel.text,
                "hoàn tác xong status bar không đổi câu — người dùng không được mời làm lại (UX-26)");
        }

        // ================================================================================================ trợ giúp

        private IEnumerator OpenCalendar(UxWindowSize size, LiveOpsConfirmResult? confirmationResult = null)
        {
            _confirmation = confirmationResult.HasValue
                ? new ScriptedLiveOpsHubConfirmationPresenter(confirmationResult.Value)
                : new ScriptedLiveOpsHubConfirmationPresenter();
            LiveOpsHubServices services = UxHubWindowFixture.DesignServices(_confirmation, DesignNowUtc);
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, size, LiveOpsHubLanguageId.Vietnamese, services);
            yield return _fixture.WaitForLayout();
        }

        private IEnumerator OpenCalendarWithSelection(UxWindowSize size)
        {
            yield return OpenCalendar(size);
            LiveOpsTimelineBar bar = _fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
            yield return UxEventSender.Click(_fixture.Window, bar);
        }

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

        private Button FindAddEventButton()
        {
            VisualElement actions = _fixture.Root.Q(LiveOpsHubPaths.ShellElementNames.SectionActions);
            return actions == null ? null : actions.Q<Button>();
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
