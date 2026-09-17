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
        /// Shift nhấn giữa chừng: đợt bắt đầu TỪ mép cuối gốc trở đi dời đúng bằng khoảng vừa dời, và đi cùng MỘT intent Commit
        /// (một bước Undo). Thả Shift ra thì tập kéo theo rỗng ngay bước sau.
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
            Assert.AreEqual(1, commits.Count, "một cử chỉ = một Commit, đợt đi theo nằm TRONG intent đó");
            Assert.AreEqual(1, commits[0].Followers.Count);
            Assert.AreEqual(LiveOpsDesignSample.HuntBonusEntryKey, commits[0].Followers[0].BarKey);
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
}
