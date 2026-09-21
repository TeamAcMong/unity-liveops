using System;
using System.Collections;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// (G-OPT-TIMELINE, W6 — P1-lùi) Ba việc của Hình 12 khung 9 và khung 12:
    /// <list type="bullet">
    /// <item>chọn nhiều: ⌘/Ctrl-click bật tắt, Shift-click chọn dải trong làn, khung chọn kéo trên chỗ trống;</item>
    /// <item>làn thu gọn 22px với dải 6px không nhãn;</item>
    /// <item>Shift nhấn SAU khi đã bắt đầu kéo = kéo theo đợt phía sau.</item>
    /// </list>
    /// Phần cử chỉ chạy trên panel thật (UI) qua <c>EditorWindow.SendEvent</c> như <see cref="TimelineInteractionTests"/>; phần
    /// model và máy trạng thái kéo chạy Logic (<c>-nographics</c>) vì không cần một pixel nào.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class TimelineMultiSelectTests
    {
        private const string HuntLaneTypeId = "treasure-hunt";
        private const float MarqueeStartInset = 5f;

        private TimelineTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            _panel?.Dispose();
            _panel = null;
        }

        private TimelineHarness Harness => _panel.Harness;
        private LiveOpsTimelineElement Element => _panel.Harness.Element;

        private IEnumerator OpenDesignSample(Func<LiveOpsTimelineInput> inputFactory = null)
        {
            _panel = TimelineTestPanel.Open();
            VisualElement root = _panel.CreateRoot(false);
            TimelineHarness harness = TimelineHarness.Create(root,
                inputFactory ?? TimelineViewInputs.Checked(LiveOpsDesignSample.Document));
            harness.Start(LiveOpsTimelineGeometry.RangeStartFor(LiveOpsDesignSample.NowUtc, LiveOpsTimelineZoom.ThreeWeeks),
                LiveOpsTimelineZoom.ThreeWeeks);
            _panel.Harness = harness;
            yield return harness.WaitReady();
        }

        // ------------------------------------------------------------------------------------ chọn nhiều (Hình 12 khung 9)

        /// <summary>⌘/Ctrl-click thêm thanh thứ hai vào tập; bấm lại đúng thanh đó thì bỏ nó ra, tập KHÔNG rỗng theo.</summary>
        [UnityTest]
        public IEnumerator CommandClick_TogglesBarInSelection()
        {
            yield return OpenDesignSample();
            LiveOpsTimelineBar early = Element.FindBar(LiveOpsDesignSample.HuntEarlyEntryKey);
            LiveOpsTimelineBar bonus = Element.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            Assert.IsNotNull(early);
            Assert.IsNotNull(bonus);

            ClickBar(early, EventModifiers.None);
            CollectionAssert.AreEqual(new[] { LiveOpsDesignSample.HuntEarlyEntryKey }, Element.SelectedBarKeys,
                "bấm thường vẫn là chọn đúng một thanh");

            ClickBar(bonus, TimelineTestPanel.ActionModifier);
            CollectionAssert.AreEqual(new[] { LiveOpsDesignSample.HuntEarlyEntryKey, LiveOpsDesignSample.HuntBonusEntryKey },
                Element.SelectedBarKeys, "⌘-click thêm thanh vào tập, giữ nguyên thanh cũ");
            Assert.IsTrue(bonus.ClassListContains(LiveOpsHubClassNames.TimelineBarSelected), "thanh vừa thêm vẽ trạng thái đã chọn");
            Assert.IsTrue(early.ClassListContains(LiveOpsHubClassNames.TimelineBarSelected), "thanh cũ KHÔNG mất trạng thái đã chọn");

            ClickBar(bonus, TimelineTestPanel.ActionModifier);
            CollectionAssert.AreEqual(new[] { LiveOpsDesignSample.HuntEarlyEntryKey }, Element.SelectedBarKeys,
                "⌘-click lần hai bỏ đúng thanh đó ra khỏi tập");
            Assert.IsFalse(bonus.ClassListContains(LiveOpsHubClassNames.TimelineBarSelected));

            List<SelectManyIntent> selections = Harness.IntentsOf<SelectManyIntent>();
            Assert.AreEqual(2, selections.Count, "mỗi lần ⌘-click phát đúng một SelectManyIntent");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Shift-click chọn DẢI trong làn: neo là thanh chạm gần nhất, dải tính theo thứ tự thời gian chứ không theo thứ tự trong
        /// tài liệu. ⌘/Ctrl và Shift KHÔNG mở cử chỉ kéo — đang gom tập chọn thì một cú run tay không được dời đợt.
        /// </summary>
        [UnityTest]
        public IEnumerator ShiftClick_SelectsRangeWithinLane_WithoutStartingADrag()
        {
            yield return OpenDesignSample();
            LiveOpsTimelineBar early = Element.FindBar(LiveOpsDesignSample.HuntEarlyEntryKey);
            LiveOpsTimelineBar bonus = Element.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);

            ClickBar(early, EventModifiers.None);
            ClickBar(bonus, EventModifiers.Shift);

            CollectionAssert.AreEqual(new[] { LiveOpsDesignSample.HuntEarlyEntryKey, LiveOpsDesignSample.HuntBonusEntryKey },
                Element.SelectedBarKeys, "dải từ thanh neo tới thanh vừa bấm, theo thứ tự thời gian");
            Assert.IsFalse(Element.DragController.IsActive, "Shift-click không mở cử chỉ kéo");
            Assert.AreEqual(0, Harness.IntentsOf<MoveBarIntent>().Count, "không có bước kéo nào được phát");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Khung chọn kéo trên chỗ trống (khung 9): vẽ ra khung, chọn mọi thanh nó CHẠM, và dòng gợi ý đổi sang câu
        /// "2 đợt · treasure-hunt · ⌘-click bật tắt · Shift-click chọn dải".
        /// </summary>
        [UnityTest]
        public IEnumerator Marquee_SelectsBarsItTouches_AndHintSaysCountAndLane()
        {
            yield return OpenDesignSample();
            LiveOpsTimelineBar early = Element.FindBar(LiveOpsDesignSample.HuntEarlyEntryKey);
            LiveOpsTimelineBar bonus = Element.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            LiveOpsTimelineLane lane = Element.FindLaneElement(HuntLaneTypeId);
            Assert.IsNotNull(lane);

            Vector2 start = new Vector2(lane.worldBound.xMin + MarqueeStartInset, early.worldBound.center.y);
            Vector2 end = new Vector2(bonus.worldBound.xMax + MarqueeStartInset, bonus.worldBound.center.y);
            _panel.SendMouse(EventType.MouseDown, start);
            _panel.SendMouse(EventType.MouseDrag, new Vector2((start.x + end.x) / 2f, (start.y + end.y) / 2f));
            Assert.IsFalse(Element.Marquee.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "qua ngưỡng 4px thì khung chọn hiện");
            _panel.SendMouse(EventType.MouseDrag, end);
            _panel.SendMouse(EventType.MouseUp, end);

            Assert.IsTrue(Element.Marquee.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "thả chuột thì khung chọn tắt");
            CollectionAssert.AreEquivalent(new[] { LiveOpsDesignSample.HuntEarlyEntryKey, LiveOpsDesignSample.HuntBonusEntryKey },
                Element.SelectedBarKeys, "khung chọn lấy mọi thanh nó chạm");
            Assert.AreEqual("2 đợt · treasure-hunt · " + ActionKeyText + "-click bật tắt · Shift-click chọn dải",
                Element.HintLine.Description.text, "microcopy khung 9 [SD1 §3.8]");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Bấm chỗ trống mà không kéo quá 4px vẫn là "bỏ chọn" như W4 — khung chọn không được cướp cử chỉ đó.</summary>
        [UnityTest]
        public IEnumerator Marquee_ClickWithoutPassingThreshold_StillDeselects()
        {
            yield return OpenDesignSample();
            LiveOpsTimelineBar early = Element.FindBar(LiveOpsDesignSample.HuntEarlyEntryKey);
            LiveOpsTimelineLane lane = Element.FindLaneElement(HuntLaneTypeId);
            ClickBar(early, EventModifiers.None);
            Assert.AreEqual(1, Element.SelectionCount);

            Vector2 empty = new Vector2(lane.worldBound.xMin + MarqueeStartInset, early.worldBound.center.y);
            _panel.SendMouse(EventType.MouseDown, empty);
            _panel.SendMouse(EventType.MouseUp, empty);

            Assert.AreEqual(0, Element.SelectionCount, "bấm chỗ trống = bỏ chọn");
            Assert.IsEmpty(Element.SelectedBarKey);
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------------------------ làn thu gọn (Hình 12 khung 12)

        /// <summary>Làn thu gọn cao 22px, thanh rút còn dải 6px không nhãn, chevron quay sang phải.</summary>
        [UnityTest]
        public IEnumerator CollapsedLane_IsTwentyTwoPixels_WithSixPixelStripsAndNoLabel()
        {
            Func<LiveOpsTimelineInput> checkedInput = TimelineViewInputs.Checked(LiveOpsDesignSample.Document);
            yield return OpenDesignSample(() => checkedInput().WithCollapsedLanes(new[] { HuntLaneTypeId }));

            LiveOpsTimelineLane lane = Element.FindLaneElement(HuntLaneTypeId);
            LiveOpsTimelineLaneHeader header = Element.FindHeader(HuntLaneTypeId);
            LiveOpsTimelineBar early = Element.FindBar(LiveOpsDesignSample.HuntEarlyEntryKey);
            Assert.IsNotNull(lane);
            Assert.IsNotNull(header);
            Assert.IsNotNull(early);

            Assert.IsTrue(lane.Model.IsCollapsed, "model làn mang cờ thu gọn");
            Assert.AreEqual(LiveOpsTimelineLane.CollapsedLaneHeight, lane.LaneHeight, "làn thu gọn cao 22px [SD1 §3.2]");
            Assert.AreEqual(LiveOpsTimelineLane.CollapsedLaneHeight, lane.resolvedStyle.height, 0.5f, "chiều cao thật sau layout");
            Assert.AreEqual(LiveOpsTimelineLane.CollapsedBarHeight, early.DrawnHeight, "dải 6px");
            Assert.AreEqual(LiveOpsTimelineLane.CollapsedBarHeight, early.resolvedStyle.height, 0.5f, "USS rút chiều cao thanh còn 6px");
            Assert.IsTrue(early.IsCollapsed);
            Assert.AreEqual(DisplayStyle.None, early.Label.resolvedStyle.display, "dải không có nhãn");
            Assert.IsTrue(header.Chevron.ClassListContains(LiveOpsHubClassNames.TimelineLaneChevronCollapsed), "chevron gập");

            // Hai thanh của làn nằm CHỒNG nhau theo chiều dọc khi thu gọn: 22px không đủ cho hai hàng phụ, đúng như khung 12.
            LiveOpsTimelineBar bonus = Element.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            Assert.AreEqual(early.Top, bonus.Top, "mọi dải của làn thu gọn nằm trên cùng một đường tâm");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Làn KHÔNG nằm trong danh sách thu gọn giữ nguyên 28px mỗi hàng phụ — thu gọn không lan sang làn khác.</summary>
        [UnityTest]
        public IEnumerator CollapsedLane_DoesNotAffectOtherLanes()
        {
            Func<LiveOpsTimelineInput> checkedInput = TimelineViewInputs.Checked(LiveOpsDesignSample.Document);
            yield return OpenDesignSample(() => checkedInput().WithCollapsedLanes(new[] { HuntLaneTypeId }));

            LiveOpsTimelineLane lavaQuest = Element.FindLaneElement("lava-quest");
            Assert.IsNotNull(lavaQuest);
            Assert.IsFalse(lavaQuest.Model.IsCollapsed);
            Assert.Greater(lavaQuest.LaneHeight, LiveOpsTimelineLane.CollapsedLaneHeight);
            LogAssert.NoUnexpectedReceived();
        }

        private void ClickBar(LiveOpsTimelineBar bar, EventModifiers modifiers)
        {
            Vector2 position = bar.worldBound.center;
            _panel.SendMouse(EventType.MouseDown, position, 0, modifiers);
            _panel.SendMouse(EventType.MouseUp, position, 0, modifiers);
        }

        private static string ActionKeyText => Application.platform == RuntimePlatform.OSXEditor
            ? LiveOpsHubStrings.TimelineActionKeyMac
            : LiveOpsHubStrings.TimelineActionKeyOther;
    }

    /// <summary>
    /// Phần không cần pixel của G-OPT-TIMELINE (Logic, <c>-nographics</c>): cờ thu gọn trong model, họ ý định vẫn đóng sau khi
    /// thêm hai loại mới, và máy trạng thái "Shift giữa chừng = kéo theo đợt sau".
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class TimelineMultiSelectLogicTests
    {
        private const float DesignTrackWidth = 635f;
        private const string HuntLaneTypeId = "treasure-hunt";

        /// <summary>Cờ thu gọn chỉ đậu lên đúng làn được kê tên; làn thu gọn vẫn CÓ MẶT (khác làn ẩn, vốn biến khỏi danh sách).</summary>
        [Test]
        public void CollapsedLanes_MarkOnlyListedLanes_AndKeepThemInTheModel()
        {
            LiveOpsTimelineModel model = TimelineDesignSampleInput.ThreeWeeks(DesignTrackWidth)
                .WithCollapsedLanes(new[] { HuntLaneTypeId })
                .Build();

            Assert.IsTrue(model.FindLane(HuntLaneTypeId).IsCollapsed);
            Assert.IsFalse(model.FindLane("lava-quest").IsCollapsed);
            Assert.AreEqual(5, model.Lanes.Count, "thu gọn KHÔNG bỏ làn khỏi trục — đó là việc của ẩn làn");
        }

        [Test]
        public void Intents_MultiSelectAndCollapse_JoinTheClosedFamily()
        {
            CollectionAssert.Contains(LiveOpsTimelineIntent.KnownIntentTypeNames, nameof(SelectManyIntent));
            CollectionAssert.Contains(LiveOpsTimelineIntent.KnownIntentTypeNames, nameof(ToggleLaneCollapsedIntent));
        }

        /// <summary>
        /// Shift nhấn giữa chừng: đợt bắt đầu TỪ mép cuối gốc trở đi dời đúng bằng khoảng vừa dời (tập XEM TRƯỚC của control),
        /// và cử chỉ phát đúng MỘT intent Commit mang cờ Shift. Thả Shift ra thì tập kéo theo rỗng ngay bước sau.
        /// </summary>
        [Test]
        public void ShiftDuringDrag_MovesLaterEventsAlong_InTheSameCommit()
        {
            var commits = new List<MoveBarIntent>();
            LiveOpsTimelineModel model = SequentialHuntModel();
            LiveOpsTimelineLaneModel lane = model.FindLane(HuntLaneTypeId);
            LiveOpsTimelineBarModel dragged = BarOf(lane, LiveOpsDesignSample.HuntEarlyEntryKey);
            var controller = new LiveOpsTimelineDragController();
            controller.IntentRaised += intent =>
            {
                MoveBarIntent move = intent as MoveBarIntent;
                if (move != null && move.IsCommit) commits.Add(move);
            };

            float grabPosition = model.Geometry.XOf(dragged.StartUtc) + 4f;
            Assert.IsTrue(controller.BeginBar(lane, model.Geometry, dragged, LiveOpsTimelineBarRegion.Body, grabPosition));
            float twelveHours = (float)(model.Geometry.PixelsPerHour * 12d);
            controller.Move(grabPosition + twelveHours, false, true);

            Assert.AreEqual(1, controller.FollowerMoves.Count, "chỉ đợt bắt đầu từ mép cuối gốc trở đi mới đi theo");
            Assert.AreEqual(LiveOpsDesignSample.HuntBonusEntryKey, controller.FollowerMoves[0].BarKey);
            Assert.AreEqual(BonusStartUtc.AddHours(12), controller.FollowerMoves[0].NewStartUtc);
            Assert.AreEqual(BonusEndUtc.AddHours(12), controller.FollowerMoves[0].NewEndUtc);

            controller.Commit();
            Assert.AreEqual(1, commits.Count, "một cử chỉ = một Commit, đợt đi theo gộp vào chính bước Undo đó");
            Assert.IsTrue(commits[0].FollowsLaterEvents,
                "intent mang CỜ Shift; tập đợt thật do presenter tính từ tài liệu (vá F-4) nên không phụ thuộc mức zoom");
        }

        [Test]
        public void ShiftReleasedDuringDrag_ClearsFollowersOnTheNextStep()
        {
            LiveOpsTimelineModel model = SequentialHuntModel();
            LiveOpsTimelineLaneModel lane = model.FindLane(HuntLaneTypeId);
            LiveOpsTimelineBarModel dragged = BarOf(lane, LiveOpsDesignSample.HuntEarlyEntryKey);
            var controller = new LiveOpsTimelineDragController();
            float grabPosition = model.Geometry.XOf(dragged.StartUtc) + 4f;
            controller.BeginBar(lane, model.Geometry, dragged, LiveOpsTimelineBarRegion.Body, grabPosition);
            float twelveHours = (float)(model.Geometry.PixelsPerHour * 12d);

            controller.Move(grabPosition + twelveHours, false, true);
            Assert.AreEqual(1, controller.FollowerMoves.Count);
            controller.Move(grabPosition + twelveHours, false, false);
            Assert.AreEqual(0, controller.FollowerMoves.Count, "thả Shift là nhả ngay tập kéo theo, không đợi thả chuột");
        }

        /// <summary>
        /// Tập đợt kéo theo neo vào mép cuối GỐC: kéo dài thêm không nuốt thêm đợt. Không thế thì người dùng kéo qua một đợt là
        /// tự nhiên dời luôn nó, mà lúc bấm Shift họ đâu có thấy đợt đó nằm trong tầm.
        /// </summary>
        [Test]
        public void FollowerSet_AnchoredOnOriginalEnd_DoesNotGrowWhileDragging()
        {
            LiveOpsTimelineModel model = SequentialHuntModel();
            LiveOpsTimelineLaneModel lane = model.FindLane(HuntLaneTypeId);
            LiveOpsTimelineBarModel dragged = BarOf(lane, LiveOpsDesignSample.HuntEarlyEntryKey);
            var controller = new LiveOpsTimelineDragController();
            float grabPosition = model.Geometry.XOf(dragged.EndUtc);
            controller.BeginBar(lane, model.Geometry, dragged, LiveOpsTimelineBarRegion.EndEdge, grabPosition);

            controller.Move(grabPosition + (float)(model.Geometry.PixelsPerHour * 48d), false, true);
            Assert.AreEqual(1, controller.FollowerMoves.Count, "kéo mép cuối qua hẳn đợt sau vẫn đúng một đợt đi theo");
            Assert.AreEqual(LiveOpsDesignSample.HuntBonusEntryKey, controller.FollowerMoves[0].BarKey);
        }

        private static readonly DateTime BonusStartUtc = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime BonusEndUtc = new DateTime(2026, 9, 19, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Lịch mẫu có hunt-0916-bonus CHỒNG hunt-0914, nên không có đợt nào "phía sau" để mà đi theo. Dời bonus ra sau mép cuối
        /// của hunt-0914 là dựng đúng tình huống mà cử chỉ này sinh ra để giải quyết.
        /// </summary>
        private static LiveOpsTimelineModel SequentialHuntModel()
        {
            LiveEventCalendarDocument document = TimelineViewInputs.WithFixedTimes(LiveOpsDesignSample.Document,
                LiveOpsDesignSample.HuntBonusEntryKey, "2026-09-18T00:00:00Z", "2026-09-19T00:00:00Z");
            return TimelineViewInputs.Unchecked(document)()
                .WithRange(LiveOpsTimelineGeometry.RangeStartFor(LiveOpsDesignSample.NowUtc, LiveOpsTimelineZoom.ThreeWeeks),
                    LiveOpsTimelineZoom.ThreeWeeks)
                .WithTrackWidth(DesignTrackWidth)
                .Build();
        }

        private static LiveOpsTimelineBarModel BarOf(LiveOpsTimelineLaneModel lane, string barKey)
        {
            for (int index = 0; index < lane.Bars.Count; index++)
            {
                if (string.Equals(lane.Bars[index].BarKey, barKey, StringComparison.Ordinal)) return lane.Bars[index];
            }
            throw new AssertionException("không tìm thấy thanh " + barKey);
        }
    }

    /// <summary>
    /// (G-OPT-TIMELINE, việc (b) của 10.3) Hai lệnh của bảng inspector trạng thái (c) và tập đợt kéo theo ở tầng presenter —
    /// những thứ <see cref="TimelineMultiSelectLogicTests"/> không chạm tới vì chúng nằm sau tài liệu, không sau con trỏ.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class CalendarMultiSelectCommandTests
    {
        private static readonly DateTime RangeStartUtc = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
        private const float DesignTrackWidth = 635f;

        private ScriptedLiveOpsHubConfirmationPresenter _confirmation;

        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
        }

        /// <summary>
        /// Ô "Dời cả hai (giờ)": đợt đã khép bị bỏ qua (bảng 7.0), đợt sắp tới dời đủ số giờ, và cả lệnh là MỘT toast có Hoàn tác
        /// — tức một bước Undo, không phải mỗi đợt một bước. Toast đếm số đợt THẬT SỰ dời, không đếm cả tập đang chọn.
        /// </summary>
        [Test]
        public void ShiftSelectedEvents_SkipsEndedEvents_AndCountsOnlyWhatMoved()
        {
            LiveOpsHubServices services = CreateServices(LiveOpsDesignSample.Document);
            CalendarTimelinePresenter presenter = CreatePresenter(services);
            var toasts = new List<LiveOpsToastModel>();
            presenter.ToastRequested += toast => toasts.Add(toast);
            presenter.HandleIntent(new SelectManyIntent(
                new[] { LiveOpsDesignSample.LavaQuestEarlyEntryKey, LiveOpsDesignSample.LavaQuestMidEntryKey },
                LiveOpsDesignSample.LavaQuestMidEntryKey));
            Assert.AreEqual(2, presenter.SelectedBarKeys.Count);

            Assert.IsTrue(presenter.ShiftSelectedEvents(12d));

            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey,
                out FixedLiveEventEntry moved));
            Assert.AreEqual("2026-09-17T12:00:00Z", moved.StartUtcText, "đợt sắp tới dời đủ 12 giờ");
            Assert.AreEqual("2026-09-20T12:00:00Z", moved.EndUtcText);
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestEarlyEntryKey,
                out FixedLiveEventEntry ended));
            Assert.AreEqual("2026-09-10T00:00:00Z", ended.StartUtcText, "đợt đã khép đứng yên — bảng 7.0 không cho dời");

            Assert.AreEqual(1, toasts.Count, "cả lệnh gộp vào một bước Undo nên chỉ một toast");
            Assert.IsTrue(toasts[0].HasUndo);
            Assert.AreEqual("Đã dời 1 đợt +12 giờ", toasts[0].Message, "toast đếm đợt thật sự dời, không đếm cả tập chọn");
            Assert.AreEqual("Dời 1 đợt", toasts[0].UndoneStepName, "tên bước ngắn cho toast sau ⌘Z (8.5)");
        }

        /// <summary>Không đợt nào trong tập dời được thì lệnh không chạy: không toast, không bước Undo rỗng.</summary>
        [Test]
        public void ShiftSelectedEvents_AllEndedOrUnreadable_DoesNothing()
        {
            LiveOpsHubServices services = CreateServices(LiveOpsDesignSample.Document);
            CalendarTimelinePresenter presenter = CreatePresenter(services);
            var toasts = new List<LiveOpsToastModel>();
            presenter.ToastRequested += toast => toasts.Add(toast);
            presenter.HandleIntent(new SelectManyIntent(
                new[] { LiveOpsDesignSample.LavaQuestEarlyEntryKey, LiveOpsDesignSample.LavaQuestLateEntryKey },
                LiveOpsDesignSample.LavaQuestEarlyEntryKey));

            Assert.IsFalse(presenter.ShiftSelectedEvents(12d), "đã khép + giờ không đọc được = không có gì để dời");
            Assert.AreEqual(0, toasts.Count);
        }

        /// <summary>Nút "Xoá n đợt…": xoá đủ tập trong MỘT bước Undo, rồi bỏ chọn (không giữ khoá của đợt vừa biến mất).</summary>
        [Test]
        public void DeleteSelectedEvents_RemovesWholeSelection_InOneUndoStep()
        {
            LiveOpsHubServices services = CreateServices(LiveOpsDesignSample.Document);
            CalendarTimelinePresenter presenter = CreatePresenter(services);
            var toasts = new List<LiveOpsToastModel>();
            presenter.ToastRequested += toast => toasts.Add(toast);
            _confirmation.Enqueue(LiveOpsConfirmResult.Destructive);
            presenter.HandleIntent(new SelectManyIntent(
                new[] { LiveOpsDesignSample.HuntEarlyEntryKey, LiveOpsDesignSample.HuntBonusEntryKey },
                LiveOpsDesignSample.HuntBonusEntryKey));

            Assert.IsTrue(presenter.DeleteSelectedEvents());

            Assert.IsFalse(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntEarlyEntryKey, out FixedLiveEventEntry _));
            Assert.IsFalse(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey, out FixedLiveEventEntry _));
            Assert.AreEqual(1, toasts.Count, "xoá cả tập là một bước Undo");
            Assert.AreEqual("Đã xoá 2 đợt", toasts[0].Message);
            Assert.AreEqual("Xoá 2 đợt", toasts[0].UndoneStepName);
            Assert.AreEqual(0, presenter.SelectedBarKeys.Count, "đợt không còn thì tập chọn phải rỗng theo");
        }

        /// <summary>Tập một đợt KHÔNG đi đường này — hai lệnh của bảng (c) chỉ có nghĩa từ hai đợt trở lên.</summary>
        [Test]
        public void MultiSelectCommands_NeedAtLeastTwoEvents()
        {
            LiveOpsHubServices services = CreateServices(LiveOpsDesignSample.Document);
            CalendarTimelinePresenter presenter = CreatePresenter(services);
            presenter.SetSelectedBarKey(LiveOpsDesignSample.HuntBonusEntryKey);

            Assert.IsFalse(presenter.ShiftSelectedEvents(12d));
            Assert.IsFalse(presenter.DeleteSelectedEvents());
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey, out FixedLiveEventEntry _));
        }

        /// <summary>
        /// (vá F-4) Giữ Shift lúc kéo dời cả đợt phía sau NẰM NGOÀI khung nhìn: tập đi theo tính từ tài liệu, không từ thanh đang
        /// vẽ. Trước khi vá, đúng cử chỉ này ở mức zoom "3 tuần" không dời gì cả còn ở "Tháng" thì dời — mà không dòng chữ nào nói
        /// ra điều kiện đó.
        /// </summary>
        [Test]
        public void DragWithShift_MovesLaterEvent_EvenWhenItHasNoBarInTheVisibleRange()
        {
            LiveEventCalendarDocument document = TimelineViewInputs.WithFixedTimes(LiveOpsDesignSample.Document,
                LiveOpsDesignSample.LavaQuestLateEntryKey, "2026-10-01T00:00:00Z", "2026-10-03T00:00:00Z");
            LiveOpsHubServices services = CreateServices(document);
            CalendarTimelinePresenter presenter = CreatePresenter(services);
            var toasts = new List<LiveOpsToastModel>();
            var deferred = new List<Action>();
            presenter.ToastRequested += toast => toasts.Add(toast);
            presenter.DeferConfirmation = action => deferred.Add(action);
            Assert.IsNull(presenter.FindBar(LiveOpsDesignSample.LavaQuestLateEntryKey),
                "đợt tháng 10 nằm ngoài khoảng ba tuần đang xem nên KHÔNG có thanh nào trên trục");

            DateTime newStartUtc = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
            DateTime newEndUtc = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, newStartUtc, newEndUtc,
                LiveOpsTimelineGesturePhase.Preview, true));
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, newStartUtc, newEndUtc,
                LiveOpsTimelineGesturePhase.Commit, true));

            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestLateEntryKey,
                out FixedLiveEventEntry follower));
            Assert.AreEqual("2026-10-02T00:00:00Z", follower.StartUtcText, "đợt sau dời đúng một ngày như đợt đang kéo");
            Assert.AreEqual("2026-10-04T00:00:00Z", follower.EndUtcText);
            Assert.AreEqual(1, toasts.Count, "đợt đi theo nằm TRONG cùng một bước Undo");
            Assert.IsTrue(toasts[0].Message.EndsWith(" · 1 đợt sau đi theo", StringComparison.Ordinal),
                "toast nói ra đợt vừa bị dời KÈM: " + toasts[0].Message);
        }

        /// <summary>Không giữ Shift thì không đợt nào đi theo, dù dãy phía sau vẫn ở đó.</summary>
        [Test]
        public void DragWithoutShift_LeavesLaterEventsAlone()
        {
            LiveEventCalendarDocument document = TimelineViewInputs.WithFixedTimes(LiveOpsDesignSample.Document,
                LiveOpsDesignSample.LavaQuestLateEntryKey, "2026-10-01T00:00:00Z", "2026-10-03T00:00:00Z");
            LiveOpsHubServices services = CreateServices(document);
            CalendarTimelinePresenter presenter = CreatePresenter(services);
            var toasts = new List<LiveOpsToastModel>();
            presenter.ToastRequested += toast => toasts.Add(toast);
            presenter.DeferConfirmation = action => { };

            DateTime newStartUtc = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
            DateTime newEndUtc = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, newStartUtc, newEndUtc,
                LiveOpsTimelineGesturePhase.Preview));
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.LavaQuestMidEntryKey, newStartUtc, newEndUtc,
                LiveOpsTimelineGesturePhase.Commit));

            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestLateEntryKey,
                out FixedLiveEventEntry untouched));
            Assert.AreEqual("2026-10-01T00:00:00Z", untouched.StartUtcText);
            Assert.AreEqual(1, toasts.Count);
            Assert.IsFalse(toasts[0].Message.Contains("đi theo"), "không giữ Shift thì toast không có vế kéo theo");
        }

        private LiveOpsHubServices CreateServices(LiveEventCalendarDocument document)
        {
            ManualLiveOpsClock clock = LiveOpsHubTestServices.CreateClock();
            _confirmation = new ScriptedLiveOpsHubConfirmationPresenter();
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(clock)
                .WithConfirmation(_confirmation)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document)));
            services.Session.RunCheckToCompletion();
            return services;
        }

        private static CalendarTimelinePresenter CreatePresenter(LiveOpsHubServices services)
        {
            CalendarTimelinePresenter presenter = new CalendarTimelinePresenter(services);
            DateTime rangeEndUtc = LiveOpsTimelineGeometry.AddTicksClamped(RangeStartUtc,
                LiveOpsTimelineGeometry.RangeLengthOf(LiveOpsTimelineZoom.ThreeWeeks).Ticks);
            presenter.BuildModel(RangeStartUtc, rangeEndUtc, DesignTrackWidth);
            return presenter;
        }
    }

    /// <summary>
    /// (G-OPT-TIMELINE, việc (b) của 10.3) Bảng inspector trạng thái (c) [SD1 §3.10]: titlebar nói SỐ đợt và làn, nút xoá nói số
    /// đợt. Dựng inspector thẳng trên hai element rỗng — pane này không cần cả màn Lịch để trả lời "tôi đang giữ cái gì".
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class CalendarMultiSelectInspectorTests
    {
        private static readonly DateTime RangeStartUtc = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
        private const float DesignTrackWidth = 635f;

        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
        }

        [Test]
        public void RefreshMultiple_SameLane_TitleNamesTheEventType()
        {
            VisualElement title = new VisualElement();
            VisualElement body = new VisualElement();
            CalendarEventInspector inspector = BuildInspector(title, body);

            inspector.RefreshMultiple(new[] { LiveOpsDesignSample.LavaQuestEarlyEntryKey, LiveOpsDesignSample.LavaQuestMidEntryKey });

            Assert.AreEqual("2 đợt · lava-quest", title.Q<Label>().text, "cả tập cùng một loại thì titlebar nêu tên loại");
            Assert.AreEqual("Xoá 2 đợt…", DangerButtonText(body), "nút xoá nói đúng số đợt sắp mất");
        }

        [Test]
        public void RefreshMultiple_MixedLanes_TitleCountsTheTypes()
        {
            VisualElement title = new VisualElement();
            VisualElement body = new VisualElement();
            CalendarEventInspector inspector = BuildInspector(title, body);

            inspector.RefreshMultiple(new[] { LiveOpsDesignSample.HuntEarlyEntryKey, LiveOpsDesignSample.LavaQuestMidEntryKey });

            Assert.AreEqual("2 đợt · 2 loại", title.Q<Label>().text, "lẫn loại thì titlebar KHÔNG bịa một tên làn");
        }

        private static string DangerButtonText(VisualElement body)
        {
            foreach (Button button in body.Query<Button>().ToList())
            {
                if (button.ClassListContains(LiveOpsHubClassNames.ButtonDanger)) return button.text;
            }
            return string.Empty;
        }

        private static CalendarEventInspector BuildInspector(VisualElement title, VisualElement body)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            services.Session.RunCheckToCompletion();
            CalendarTimelinePresenter presenter = new CalendarTimelinePresenter(services);
            DateTime rangeEndUtc = LiveOpsTimelineGeometry.AddTicksClamped(RangeStartUtc,
                LiveOpsTimelineGeometry.RangeLengthOf(LiveOpsTimelineZoom.ThreeWeeks).Ticks);
            presenter.BuildModel(RangeStartUtc, rangeEndUtc, DesignTrackWidth);
            return new CalendarEventInspector(services, presenter, title, body);
        }
    }
}
