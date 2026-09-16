using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Tương tác của timeline trên panel thật (UI, không <c>-nographics</c>): kéo thân/mép + xem trước "sẽ chồng" trước khi thả, Esc
    /// huỷ, ⌘/Ctrl + kéo tạo, phím (bỏ qua khi gõ trong TextField, Tab không bẫy focus), lệnh Edit, HitTest, hover, chuột phải không
    /// dựng menu (V-10), bánh xe zoom tại con trỏ / Shift cuộn ngang / trơn không nuốt, thước ba tầng + bubble con trỏ, con trỏ chuột
    /// theo vùng (V-11), dải cố định bấm = zoom (CC-TLMODEL-1), làn chưa ghi loại (CC-TLMODEL-2).
    ///
    /// Element dựng trần trên cửa sổ thử với model từ <see cref="LiveOpsDesignSample"/> + validator/diff core (W3-DEPS: không dùng
    /// phiên, không <c>OpenWithServices</c>); mọi chuột/phím đi qua <c>EditorWindow.SendEvent</c> như người dùng thật, không gọi handler.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class TimelineInteractionTests
    {
        private static readonly DateTime HuntBonusStartUtc = new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);

        private TimelineTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            _panel?.Dispose();
            _panel = null;
        }

        private IEnumerator OpenDesignSample(LiveEventCalendarDocument document = null, LiveOpsTimelineZoom zoom = LiveOpsTimelineZoom.ThreeWeeks)
        {
            _panel = TimelineTestPanel.Open();
            VisualElement root = _panel.CreateRoot(false);
            TimelineHarness harness = TimelineHarness.Create(root, TimelineViewInputs.Checked(document ?? LiveOpsDesignSample.Document));
            harness.Start(LiveOpsTimelineGeometry.RangeStartFor(LiveOpsDesignSample.NowUtc, zoom), zoom);
            _panel.Harness = harness;
            yield return harness.WaitReady();
        }

        private TimelineHarness Harness => _panel.Harness;
        private LiveOpsTimelineElement Element => _panel.Harness.Element;

        // ------------------------------------------------------------------------------------------------ kéo (V-10, Hình 12 khung 5–7)

        [UnityTest]
        public IEnumerator Drag_BodyPreviewCommit_RaisesOneCommitIntent()
        {
            yield return OpenDesignSample();
            LiveOpsTimelineBar bonus = Element.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            Assert.IsNotNull(bonus, "thanh hunt-0916-bonus phải có trên trục 3 tuần");
            Vector2 start = bonus.worldBound.center;
            float twelveHours = (float)(Element.PixelsPerHour * 12d);

            _panel.SendMouse(EventType.MouseDown, start);
            Assert.AreEqual(1, Harness.IntentsOf<SelectBarIntent>().Count, "nhấn chuột lên thanh chọn thanh");
            _panel.SendMouse(EventType.MouseDrag, start + new Vector2(twelveHours / 2f, 0f));
            _panel.SendMouse(EventType.MouseDrag, start + new Vector2(twelveHours, 0f));
            Assert.IsTrue(Element.DragController.IsDragging, "vượt ngưỡng 4px là đang kéo");
            Assert.IsFalse(Element.Readout.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "readout hiện trong lúc kéo");
            Assert.AreEqual("17/9 00:00 → 18/9 12:00 UTC · dời +12 giờ · dài 36 giờ", Element.ReadoutText.text,
                "readout kéo thân Hình 12 khung 5: khoảng, độ dời và độ dài, bắt lưới 1 giờ ở zoom 3 tuần");
            Assert.AreEqual(LiveOpsHubStrings.TimelineHintDragging, Element.HintLine.Description.text, "dòng gợi ý đổi sang lúc kéo");
            _panel.SendMouse(EventType.MouseUp, start + new Vector2(twelveHours, 0f));

            List<MoveBarIntent> moves = Harness.IntentsOf<MoveBarIntent>();
            Assert.Greater(TimelineTestQueries.Count(moves, move => move.IsPreview), 0, "có Preview trước khi thả");
            Assert.AreEqual(1, TimelineTestQueries.Count(moves, move => move.IsCommit), "một cử chỉ = đúng một Commit (một bước Undo ở presenter)");
            Assert.AreEqual(0, TimelineTestQueries.Count(moves, move => move.IsCancel));
            MoveBarIntent commit = TimelineTestQueries.Single(moves, move => move.IsCommit);
            Assert.AreEqual(LiveOpsDesignSample.HuntBonusEntryKey, commit.BarKey);
            Assert.AreEqual(HuntBonusStartUtc.AddHours(12), commit.NewStartUtc, "dời +12 giờ, bắt lưới 1 giờ");
            Assert.AreEqual(HuntBonusStartUtc.AddHours(48), commit.NewEndUtc, "giữ độ dài 36 giờ");
            Assert.IsFalse(Element.DragController.IsActive);
            Assert.IsTrue(Element.Readout.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "thả thì readout ẩn");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Drag_Escape_CancelsWithoutCommit()
        {
            yield return OpenDesignSample();
            LiveOpsTimelineBar bonus = Element.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            Vector2 start = bonus.worldBound.center;
            _panel.SendMouse(EventType.MouseDown, start);
            _panel.SendMouse(EventType.MouseDrag, start + new Vector2(40f, 0f));
            Assert.IsTrue(Element.DragController.IsDragging);
            _panel.SendKey("escape");
            Assert.IsFalse(Element.DragController.IsActive, "Esc kết thúc cử chỉ");
            _panel.SendMouse(EventType.MouseUp, start + new Vector2(40f, 0f));

            List<MoveBarIntent> moves = Harness.IntentsOf<MoveBarIntent>();
            Assert.AreEqual(1, TimelineTestQueries.Count(moves, move => move.IsCancel), "Esc phát đúng một Cancel");
            Assert.AreEqual(0, TimelineTestQueries.Count(moves, move => move.IsCommit), "Esc không để lại Commit — không bước Undo (M-14)");
            Assert.IsTrue(bonus.ClassListContains(LiveOpsHubClassNames.TimelineBarDragging) == false, "thanh trả về chỗ cũ");
            Assert.AreEqual(bonus.Left, bonus.layout.x, 0.5f, "không còn translate xem trước");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Drag_EndEdge_ReadoutHasDeviceTimeShiftAndLength()
        {
            // Hình 12 khung 6: lava-quest-2026-09b đang kết thúc 19/9 00:00, kéo mép cuối sang 20/9 00:00. Nhánh mép cuối là nhánh
            // duy nhất in đủ bốn vế (mép · giờ máy · dời · dài) nên khoá cả chuỗi ở đây; hai vế đo thời lượng khoá ở CẢ HAI đơn vị.
            LiveEventCalendarDocument document = TimelineViewInputs.WithFixedTimes(LiveOpsDesignSample.Document,
                LiveOpsDesignSample.LavaQuestMidEntryKey, "2026-09-17T00:00:00Z", "2026-09-19T00:00:00Z");
            yield return OpenDesignSample(document);
            LiveOpsTimelineBar mid = Element.FindBar(LiveOpsDesignSample.LavaQuestMidEntryKey);
            Assert.IsNotNull(mid, "thanh lava-quest-2026-09b phải có trên trục 3 tuần");
            Vector2 endEdge = new Vector2(mid.worldBound.xMax - 1f, mid.worldBound.center.y);
            Assert.AreEqual(LiveOpsTimelineBarRegion.EndEdge, Element.HitTest(Element.WorldToLocal(endEdge)).BarRegion, "tiền đề: bấm đúng mép cuối");
            float oneDay = (float)(Element.PixelsPerHour * 24d);
            float twelveHours = (float)(Element.PixelsPerHour * 12d);

            _panel.SendMouse(EventType.MouseDown, endEdge);
            _panel.SendMouse(EventType.MouseDrag, endEdge + new Vector2(oneDay, 0f));
            Assert.IsTrue(Element.DragController.IsDragging, "vượt ngưỡng 4px là đang kéo");
            Assert.AreEqual("Kết thúc 20/9 00:00 UTC · 07:00 giờ máy · dời +1 ngày · dài 3 ngày", Element.ReadoutText.text,
                "readout kéo mép Hình 12 khung 6: tròn ngày thì đo bằng NGÀY, không phải \"+24 giờ\" / \"72 giờ\"");
            Assert.AreEqual(string.Empty, Element.ReadoutOverlap.text, "làn lava-quest không chồng giờ nên chỉ có vế chính");

            // Cùng nhánh, độ dài lẻ ngày: 36 giờ ở lại đơn vị giờ (Hình 12 khung 5) — không cuộn thành "1 ngày 12 giờ".
            _panel.SendMouse(EventType.MouseDrag, endEdge + new Vector2(-twelveHours, 0f));
            Assert.AreEqual("Kết thúc 18/9 12:00 UTC · 19:00 giờ máy · dời −12 giờ · dài 36 giờ", Element.ReadoutText.text,
                "độ dài lẻ ngày giữ đơn vị giờ, độ dời cũng vậy");
            _panel.SendMouse(EventType.MouseUp, endEdge + new Vector2(-twelveHours, 0f));

            List<MoveBarIntent> moves = Harness.IntentsOf<MoveBarIntent>();
            Assert.AreEqual(1, TimelineTestQueries.Count(moves, move => move.IsCommit), "một cử chỉ = đúng một Commit");
            MoveBarIntent commit = TimelineTestQueries.Single(moves, move => move.IsCommit);
            Assert.AreEqual(new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc), commit.NewStartUtc, "mép đầu không đổi khi kéo mép cuối");
            Assert.AreEqual(new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc), commit.NewEndUtc, "mép cuối theo lần kéo cuối cùng");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Drag_WillOverlap_PreviewBeforeRelease()
        {
            // Hình 12 khung 7: bonus đã dời sang 17/9 00:00 → 18/9 12:00 (khung 5); kéo mép cuối hunt-0914 tới 17/9 12:00.
            LiveEventCalendarDocument document = TimelineViewInputs.WithFixedTimes(LiveOpsDesignSample.Document, LiveOpsDesignSample.HuntBonusEntryKey,
                "2026-09-17T00:00:00Z", "2026-09-18T12:00:00Z");
            yield return OpenDesignSample(document);
            LiveOpsTimelineBar early = Element.FindBar(LiveOpsDesignSample.HuntEarlyEntryKey);
            LiveOpsTimelineBar bonus = Element.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            Assert.IsFalse(bonus.Model.IsDropped, "tiền đề: sau khi dời, bonus không còn bị bỏ trong model");
            LiveOpsTimelineLane lane = Element.FindLaneElement("treasure-hunt");
            Assert.AreEqual(0, lane.DrawnOverlaps.Count, "tiền đề: chưa chồng giờ");

            Vector2 endEdge = new Vector2(early.worldBound.xMax - 1f, early.worldBound.center.y);
            Assert.AreEqual(LiveOpsTimelineBarRegion.EndEdge, Element.HitTest(Element.WorldToLocal(endEdge)).BarRegion, "tiền đề: bấm đúng mép cuối");
            float twelveHours = (float)(Element.PixelsPerHour * 12d);
            _panel.SendMouse(EventType.MouseDown, endEdge);
            _panel.SendMouse(EventType.MouseDrag, endEdge + new Vector2(twelveHours, 0f));

            Assert.AreEqual(0, TimelineTestQueries.Count(Harness.IntentsOf<MoveBarIntent>(), move => move.IsCommit), "chưa thả");
            Assert.IsTrue(bonus.ClassListContains(LiveOpsHubClassNames.TimelineBarWillDrop), "bonus --will-drop TRƯỚC khi thả (không đợi sau)");
            CollectionAssert.Contains(Element.DragController.WillDropBarKeys, LiveOpsDesignSample.HuntBonusEntryKey);
            Assert.AreEqual(1, lane.DrawnOverlaps.Count, "vùng chồng xem trước vẽ ngay");
            Assert.AreEqual(new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc), lane.DrawnOverlaps[0].startUtc);
            Assert.AreEqual(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc), lane.DrawnOverlaps[0].endUtc);
            Assert.AreEqual("Kết thúc 17/9 12:00 UTC", Element.ReadoutText.text, "readout Hình 12 khung 7 (vế giờ)");
            Assert.AreEqual(" · chồng 12 giờ với hunt-0916-bonus", Element.ReadoutOverlap.text, "vế chồng giờ chữ blocked");
            Assert.IsTrue(Element.ReadoutOverlap.ClassListContains(LiveOpsHubClassNames.TextBlocked));
            VisualElement willDropTag = TimelineTestQueries.Single(lane.Query<VisualElement>(className: LiveOpsHubClassNames.TimelineDroppedTagWillDrop).ToList(),
                tag => !tag.ClassListContains(LiveOpsHubClassNames.TimelineHidden));
            Assert.AreEqual(LiveOpsHubStrings.TimelineWillDropTag, willDropTag.Q<Label>().text, "tag \"sẽ bị bỏ\"");

            _panel.SendMouse(EventType.MouseUp, endEdge + new Vector2(twelveHours, 0f));
            MoveBarIntent commit = TimelineTestQueries.Single(Harness.IntentsOf<MoveBarIntent>(), move => move.IsCommit);
            Assert.AreEqual(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc), commit.NewEndUtc);
            Assert.AreEqual(early.Model.StartUtc, commit.NewStartUtc, "kéo mép cuối giữ giờ bắt đầu");
            Assert.IsFalse(bonus.ClassListContains(LiveOpsHubClassNames.TimelineBarWillDrop), "thả thì bỏ xem trước — model mới của presenter quyết");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CommandDragOnEmptyLane_RaisesCreateByDragPreviewCommit()
        {
            yield return OpenDesignSample();
            LiveOpsTimelineLane lava = Element.FindLaneElement("lava-quest");
            DateTime anchorUtc = new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc);
            Vector2 anchor = TimelineHarness.WorldPointOf(lava, anchorUtc, lava.PaddingTop + 14f);
            Vector2 end = TimelineHarness.WorldPointOf(lava, anchorUtc.AddDays(2), lava.PaddingTop + 14f);
            Assert.AreEqual(LiveOpsTimelineHitKind.EmptyLane, Element.HitTest(Element.WorldToLocal(anchor)).Kind, "tiền đề: chỗ trống làn cố định");

            _panel.SendMouse(EventType.MouseDown, anchor, 0, TimelineTestPanel.ActionModifier);
            _panel.SendMouse(EventType.MouseDrag, end, 0, TimelineTestPanel.ActionModifier);
            _panel.SendMouse(EventType.MouseUp, end, 0, TimelineTestPanel.ActionModifier);

            List<CreateByDragIntent> creates = Harness.IntentsOf<CreateByDragIntent>();
            Assert.Greater(TimelineTestQueries.Count(creates, create => create.IsPreview), 0, "thanh ma có Preview");
            CreateByDragIntent commit = TimelineTestQueries.Single(creates, create => create.IsCommit);
            Assert.AreEqual("lava-quest", commit.LaneTypeId);
            Assert.AreEqual(anchorUtc, commit.StartUtc);
            Assert.AreEqual(anchorUtc.AddDays(2), commit.EndUtc);
            Assert.AreEqual(0, Harness.IntentsOf<MoveBarIntent>().Count, "tạo không phải dời");

            // Làn lặp tắt tạo đợt: ⌘ + kéo trên sky-race không phát gì.
            Harness.Intents.Clear();
            LiveOpsTimelineLane skyRace = Element.FindLaneElement("sky-race");
            Vector2 gap = TimelineHarness.WorldPointOf(skyRace, anchorUtc.AddHours(22), skyRace.PaddingTop + 14f);
            _panel.SendMouse(EventType.MouseDown, gap, 0, TimelineTestPanel.ActionModifier);
            _panel.SendMouse(EventType.MouseDrag, gap + new Vector2(60f, 0f), 0, TimelineTestPanel.ActionModifier);
            _panel.SendMouse(EventType.MouseUp, gap + new Vector2(60f, 0f), 0, TimelineTestPanel.ActionModifier);
            Assert.AreEqual(0, Harness.IntentsOf<CreateByDragIntent>().Count, "làn lặp — sửa ở Luật lặp");
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------------------------------------ phím + lệnh Edit

        [UnityTest]
        public IEnumerator Keys_IgnoredWhenTextFieldFocused()
        {
            yield return OpenDesignSample();
            TextField field = new TextField();
            Element.Add(field);
            yield return null;
            Element.Select(LiveOpsDesignSample.HuntBonusEntryKey, false);
            yield return _panel.WaitForFocus(field, "ô gõ không nhận focus");
            int rangeChanges = Harness.RangeChangedCount;
            LiveOpsTimelineZoom zoom = Element.Zoom;

            foreach (string key in new[] { "right", "left", "a", "=", "-", "t" })
            {
                _panel.SendKey(key);
            }
            Assert.AreEqual(0, Harness.IntentsOf<MoveBarIntent>().Count, "← → trong ô gõ không nhích thanh");
            Assert.AreEqual(rangeChanges, Harness.RangeChangedCount, "A = − trong ô gõ không đổi khung");
            Assert.AreEqual(zoom, Element.Zoom);
            Assert.IsTrue(IsFocused(field), "focus vẫn ở ô gõ");

            // Đối chứng: focus timeline thì → nhích thanh đang chọn một bước lưới.
            yield return _panel.WaitForFocus(Element, "timeline không nhận focus");
            _panel.SendKey("right");
            MoveBarIntent nudge = TimelineTestQueries.Single(Harness.IntentsOf<MoveBarIntent>(), move => move.IsCommit);
            Assert.AreEqual(HuntBonusStartUtc.AddHours(1), nudge.NewStartUtc, "→ nhích 1 giờ (lưới zoom 3 tuần)");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Tab_FromLastBar_FocusLeavesTimeline()
        {
            yield return OpenDesignSample();
            Button after = new Button { text = "after" };
            Element.parent.Add(after);
            yield return null;
            string lastKey = LastBarKey(Element.Model);
            Element.Select(lastKey, true);
            yield return _panel.WaitForFocus(Element, "timeline không nhận focus");

            _panel.SendKey("tab");
            yield return null;
            Assert.IsFalse(IsFocused(Element), "hết đợt: Tab để focus rời timeline — không bẫy focus");
            Assert.AreEqual(lastKey, Element.SelectedBarKey, "Tab ở đợt cuối không đổi lựa chọn");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Tab_WithNextBar_KeepsFocus()
        {
            yield return OpenDesignSample();
            Button after = new Button { text = "after" };
            Element.parent.Add(after);
            yield return null;
            Element.Select(LiveOpsDesignSample.LavaQuestEarlyEntryKey, true);
            yield return _panel.WaitForFocus(Element, "timeline không nhận focus");

            _panel.SendKey("tab");
            yield return null;
            Assert.IsTrue(IsFocused(Element), "còn đợt kế: Tab giữ focus trong timeline");
            Assert.AreEqual(LiveOpsDesignSample.LavaQuestMidEntryKey, Element.SelectedBarKey, "Tab chọn đợt kế trong làn");
            Assert.AreEqual(LiveOpsDesignSample.LavaQuestMidEntryKey, TimelineTestQueries.Single(Harness.IntentsOf<SelectBarIntent>(), select => true).BarKey);
            Assert.IsTrue(Element.FindBar(LiveOpsDesignSample.LavaQuestMidEntryKey).ClassListContains(LiveOpsHubClassNames.TimelineBarFocused),
                "focus bàn phím = viền focus");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EditCommands_ValidateCopyOnlyWithFixedSelection()
        {
            yield return OpenDesignSample();
            yield return _panel.WaitForFocus(Element, "timeline không nhận focus");

            Assert.IsFalse(Element.CanExecuteCommand(LiveOpsTimelineElement.CopyCommand), "chưa chọn gì: Copy không hợp lệ");
            _panel.SendCommand(LiveOpsTimelineElement.CopyCommand);
            Assert.AreEqual(0, Harness.IntentsOf<CopyBarIntent>().Count);

            Element.Select("weekly-pass#35", true);
            Assert.IsFalse(Element.CanExecuteCommand(LiveOpsTimelineElement.CopyCommand), "đợt sinh từ luật: không copy đợt");
            Assert.IsFalse(Element.CanExecuteCommand(LiveOpsTimelineElement.DuplicateCommand));
            _panel.SendCommand(LiveOpsTimelineElement.CopyCommand);
            Assert.AreEqual(0, Harness.IntentsOf<CopyBarIntent>().Count);

            Element.Select(LiveOpsDesignSample.HuntEarlyEntryKey, true);
            Assert.IsTrue(Element.CanExecuteCommand(LiveOpsTimelineElement.CopyCommand), "thanh cố định đang chọn: Copy hợp lệ");
            Assert.IsFalse(Element.CanExecuteCommand(LiveOpsTimelineElement.PasteCommand), "chưa copy đợt nào: Paste không hợp lệ");
            _panel.SendCommand(LiveOpsTimelineElement.CopyCommand);
            Assert.AreEqual(LiveOpsDesignSample.HuntEarlyEntryKey, TimelineTestQueries.Single(Harness.IntentsOf<CopyBarIntent>(), copy => true).BarKey);
            _panel.SendCommand(LiveOpsTimelineElement.SoftDeleteCommand);
            Assert.AreEqual(LiveOpsDesignSample.HuntEarlyEntryKey, TimelineTestQueries.Single(Harness.IntentsOf<DeleteBarIntent>(), delete => true).BarKey);
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------------------------------------ HitTest, hover, chuột phải (V-10)

        [UnityTest]
        public IEnumerator HitTest_BarBodyEdgesHeaderEmptyLane()
        {
            yield return OpenDesignSample();
            LiveOpsTimelineBar early = Element.FindBar(LiveOpsDesignSample.HuntEarlyEntryKey);
            Rect bound = early.worldBound;

            AssertHit(new Vector2(bound.center.x, bound.center.y), LiveOpsTimelineHitKind.Bar, LiveOpsTimelineBarRegion.Body, "tâm thanh");
            AssertHit(new Vector2(bound.xMin + 1f, bound.center.y), LiveOpsTimelineHitKind.Bar, LiveOpsTimelineBarRegion.StartEdge, "4px trong mép đầu");
            AssertHit(new Vector2(bound.xMin - 3f, bound.center.y), LiveOpsTimelineHitKind.Bar, LiveOpsTimelineBarRegion.StartEdge, "4px ngoài mép đầu");
            AssertHit(new Vector2(bound.xMax - 1f, bound.center.y), LiveOpsTimelineHitKind.Bar, LiveOpsTimelineBarRegion.EndEdge, "mép cuối");
            Assert.AreEqual(LiveOpsDesignSample.HuntEarlyEntryKey, Element.HitTest(Element.WorldToLocal(bound.center)).BarKey);

            LiveOpsTimelineBar pass = Element.FindBar("weekly-pass#36");
            Rect passBound = pass.worldBound;
            AssertHit(new Vector2(passBound.xMin + 1f, passBound.center.y), LiveOpsTimelineHitKind.Bar, LiveOpsTimelineBarRegion.Body,
                "thanh lặp không có mép kéo");

            LiveOpsTimelineHit header = Element.HitTest(Element.WorldToLocal(Element.FindHeader("treasure-hunt").worldBound.center));
            Assert.AreEqual(LiveOpsTimelineHitKind.LaneHeader, header.Kind);
            Assert.AreEqual("treasure-hunt", header.LaneTypeId);

            LiveOpsTimelineLane lane = Element.FindLaneElement("treasure-hunt");
            Vector2 empty = TimelineHarness.WorldPointOf(lane, new DateTime(2026, 9, 25, 12, 20, 0, DateTimeKind.Utc), lane.PaddingTop + 14f);
            LiveOpsTimelineHit emptyHit = Element.HitTest(Element.WorldToLocal(empty));
            Assert.AreEqual(LiveOpsTimelineHitKind.EmptyLane, emptyHit.Kind);
            Assert.AreEqual("treasure-hunt", emptyHit.LaneTypeId);
            Assert.AreEqual(new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc), emptyHit.TimeUtc, "giờ chỗ trống đã bắt lưới 1 giờ");

            Assert.AreEqual(LiveOpsTimelineHitKind.Ruler, Element.HitTest(Element.WorldToLocal(Element.Ruler.Track.worldBound.center)).Kind);
            Assert.AreEqual(LiveOpsTimelineHitKind.None, Element.HitTest(Element.WorldToLocal(Element.HintLine.worldBound.center)).Kind);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (nợ W4 D-3(b), Hình 12 khung 13) Con trỏ đứng trên CHỖ TRỐNG của một làn cố định: dòng gợi ý nêu tên làn, khoảng đang
        /// xem và microcopy "nhấp đúp chỗ trống để thêm đợt". Nhấp đúp là đường duy nhất tạo đợt bằng chuột ở chỗ trống; trước W5
        /// dòng gợi ý chỉ có ba trạng thái nên khung 13 vẽ y hệt khung 1 và cử chỉ đó không được nói ra ở đâu.
        /// </summary>
        [UnityTest]
        public IEnumerator HintLine_EmptyLane_ShowsDoubleClickHint()
        {
            yield return OpenDesignSample();
            LiveOpsTimelineLane lane = Element.FindLaneElement("star-tournament");
            Assert.IsNotNull(lane, "làn star-tournament phải có trên trục 3 tuần");
            Vector2 empty = TimelineHarness.WorldPointOf(lane, new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc),
                lane.PaddingTop + 14f);

            _panel.SendMouse(EventType.MouseMove, empty);

            Assert.AreEqual("star-tournament", Element.HintLine.Subject.text, "gợi ý nói về LÀN đang trỏ tới");
            StringAssert.Contains("nhấp đúp", Element.HintLine.Description.text,
                "và nêu đúng cử chỉ tạo đợt ở chỗ trống [SD1 §3.8 khung 13]");

            _panel.SendMouse(EventType.MouseMove, Element.HintLine.worldBound.center);
            Assert.AreEqual(string.Empty, Element.HintLine.Subject.text, "rời làn thì gợi ý về trạng thái nghỉ");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (nợ W4 D-3(a), Hình 12 khung 5) Vế thứ ba của readout: câu kiểm nhanh mà presenter tính. Element chỉ VẼ câu được đưa
        /// xuống — nó không gọi kiểm, nên test bơm thẳng câu và xem nó có tới được nhãn không.
        /// </summary>
        [UnityTest]
        public IEnumerator Readout_ShowsQuickCheckTagFromPresenter()
        {
            yield return OpenDesignSample();
            Assert.IsTrue(Element.ReadoutQuickCheck.ClassListContains(LiveOpsHubClassNames.TimelineHidden),
                "không có câu thì không vẽ một vế rỗng");

            Element.SetDragQuickCheckTag(LiveOpsHubStrings.CalendarQuickCheckOkTag, HealthState.Ok);
            Assert.IsFalse(Element.ReadoutQuickCheck.ClassListContains(LiveOpsHubClassNames.TimelineHidden));
            Assert.AreEqual(LiveOpsHubStrings.CalendarQuickCheckOkTag, Element.ReadoutQuickCheckText.text);
            Assert.AreEqual(HealthState.Ok, Element.ReadoutQuickCheckMark.HealthValue);

            Element.SetDragQuickCheckTag(string.Empty, HealthState.Ok);
            Assert.IsTrue(Element.ReadoutQuickCheck.ClassListContains(LiveOpsHubClassNames.TimelineHidden));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Hover_RaisesEnterLeaveOnce()
        {
            yield return OpenDesignSample();
            Rect bound = Element.FindBar(LiveOpsDesignSample.HuntEarlyEntryKey).worldBound;
            _panel.SendMouse(EventType.MouseMove, bound.center);
            _panel.SendMouse(EventType.MouseMove, bound.center + new Vector2(3f, 0f));
            _panel.SendMouse(EventType.MouseMove, bound.center - new Vector2(3f, 0f));
            Assert.AreEqual(1, Harness.Hovers.Count, "rê trong cùng một thanh: vào đúng một lần");
            Assert.AreEqual(LiveOpsDesignSample.HuntEarlyEntryKey, Harness.Hovers[0].BarKey);
            Assert.AreEqual(bound.width, Harness.Hovers[0].BarWorldBound.width, 0.5f, "hover mang worldBound thanh để host đặt hover card");
            Assert.IsTrue(Element.FindBar(LiveOpsDesignSample.HuntEarlyEntryKey).ClassListContains(LiveOpsHubClassNames.TimelineBarHover));

            LiveOpsTimelineLane lane = Element.FindLaneElement("treasure-hunt");
            Vector2 empty = TimelineHarness.WorldPointOf(lane, new DateTime(2026, 9, 26, 0, 0, 0, DateTimeKind.Utc), lane.PaddingTop + 14f);
            _panel.SendMouse(EventType.MouseMove, empty);
            _panel.SendMouse(EventType.MouseMove, empty + new Vector2(5f, 0f));
            Assert.AreEqual(2, Harness.Hovers.Count, "rời thanh: đúng một lần");
            Assert.IsTrue(Harness.Hovers[1].IsLeave);
            Assert.IsFalse(Element.FindBar(LiveOpsDesignSample.HuntEarlyEntryKey).ClassListContains(LiveOpsHubClassNames.TimelineBarHover));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ContextClick_RaisesRequestWithHit_NoMenuBuilt()
        {
            yield return OpenDesignSample();
            int menuItems = 0;
            Element.RegisterCallback<ContextualMenuPopulateEvent>(populateEvent => menuItems += populateEvent.menu.MenuItems().Count);
            Vector2 center = Element.FindBar(LiveOpsDesignSample.HuntBonusEntryKey).worldBound.center;

            _panel.SendMouse(EventType.MouseDown, center, 1);
            _panel.SendMouse(EventType.MouseUp, center, 1);

            LiveOpsTimelineContextRequest request = TimelineTestQueries.Single(Harness.ContextRequests, candidate => true);
            Assert.AreEqual(LiveOpsTimelineHitKind.Bar, request.Hit.Kind);
            Assert.AreEqual(LiveOpsDesignSample.HuntBonusEntryKey, request.Hit.BarKey);
            Assert.AreEqual("treasure-hunt", request.Hit.LaneTypeId);
            Assert.AreEqual(center.x, request.WorldPosition.x, 0.5f);
            Assert.AreEqual(0, menuItems, "element không tự dựng menu — presenter W5 dựng từ CalendarContextMenus");
            Assert.AreEqual(0, Harness.IntentsOf<MoveBarIntent>().Count, "chuột phải không bắt đầu kéo");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator LaneHooks_RaiseHideShowAllMoveIntents()
        {
            yield return OpenDesignSample();
            Element.RequestHideLane(string.Empty);
            Element.RequestShowAllLanes();
            Element.RequestMoveLane("lava-quest", -1);
            Assert.AreEqual(string.Empty, TimelineTestQueries.Single(Harness.IntentsOf<HideLaneIntent>(), hide => true).TypeId, "ẩn được làn chưa ghi loại");
            Assert.AreEqual(1, Harness.IntentsOf<ShowAllLanesIntent>().Count);
            MoveLaneIntent move = TimelineTestQueries.Single(Harness.IntentsOf<MoveLaneIntent>(), candidate => true);
            Assert.AreEqual("lava-quest", move.TypeId);
            Assert.AreEqual(MoveLaneIntent.Up, move.Direction);
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------------------------------------ bánh xe (V-11)

        [UnityTest]
        public IEnumerator Wheel_ActionModifier_ZoomsAtCursor_TimeUnderCursorStable()
        {
            yield return OpenDesignSample();
            int scrollViewWheels = CountBodyWheels();
            LiveOpsTimelineLane lane = Element.FindLaneElement("lava-quest");
            Vector2 pointer = TimelineHarness.WorldPointOf(lane, HuntBonusStartUtc, lane.PaddingTop + 14f);
            float anchorX = Element.Ruler.Track.WorldToLocal(pointer).x;
            DateTime timeBefore = TimeAtTrack(anchorX);
            double scaleBefore = Element.PixelsPerHour;
            int rangeChanges = Harness.RangeChangedCount;

            _panel.SendWheel(pointer, new Vector2(0f, -LiveOpsTimelineElement.WheelDeltaPerNotch), TimelineTestPanel.ActionModifier);

            Assert.Greater(Harness.RangeChangedCount, rangeChanges, "zoom phát RangeChanged để presenter dựng lại model");
            Assert.IsTrue(Element.IsContinuousScale, "⌘/Ctrl + bánh xe = zoom liên tục");
            Assert.AreEqual(scaleBefore * LiveOpsTimelineGeometry.ZoomFactorPerWheelNotch, Element.PixelsPerHour, scaleBefore * 0.001, "một nấc × 1,15");
            Assert.AreEqual(timeBefore.Ticks, TimeAtTrack(anchorX).Ticks, TimeSpan.FromMinutes(1).Ticks, "thời điểm dưới con trỏ đứng yên");
            Assert.AreEqual(LiveOpsTimelineZoom.ThreeWeeks, Element.Zoom, "tab zoom giữ preset vừa dùng");
            Assert.AreEqual(scrollViewWheels, _bodyWheelCount, "sự kiện zoom không tới ScrollView (không cuộn dọc cùng lúc)");

            // Bấm lại preset = về đúng preset.
            Element.ZoomLevel = (int)LiveOpsTimelineZoom.ThreeWeeks;
            Assert.IsFalse(Element.IsContinuousScale);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Wheel_ClampedTo_0_25_And_48()
        {
            yield return OpenDesignSample();
            Vector2 pointer = Element.Ruler.Track.worldBound.center + new Vector2(0f, 60f);
            for (int notch = 0; notch < 80; notch++)
            {
                _panel.SendWheel(pointer, new Vector2(0f, -LiveOpsTimelineElement.WheelDeltaPerNotch), TimelineTestPanel.ActionModifier);
            }
            Assert.AreEqual(LiveOpsTimelineGeometry.MaximumPixelsPerHour, Element.PixelsPerHour, 1e-9, "kẹp trên 48 px/giờ");
            for (int notch = 0; notch < 160; notch++)
            {
                _panel.SendWheel(pointer, new Vector2(0f, LiveOpsTimelineElement.WheelDeltaPerNotch), TimelineTestPanel.ActionModifier);
            }
            Assert.AreEqual(LiveOpsTimelineGeometry.MinimumPixelsPerHour, Element.PixelsPerHour, 1e-9, "kẹp dưới 0,25 px/giờ");
            Assert.AreEqual(Element.PixelsPerHour, Element.Model.Geometry.PixelsPerHour, 0.01, "model dựng lại theo zoom mới");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Wheel_Shift_ScrollsHorizontally_RaisesRangeChanged()
        {
            yield return OpenDesignSample();
            CountBodyWheels();
            Vector2 pointer = Element.Ruler.Track.worldBound.center + new Vector2(0f, 60f);
            DateTime startBefore = Element.RangeStartUtc;
            double scale = Element.PixelsPerHour;
            int rangeChanges = Harness.RangeChangedCount;

            _panel.SendWheel(pointer, new Vector2(0f, LiveOpsTimelineElement.WheelDeltaPerNotch), EventModifiers.Shift);

            Assert.Greater(Harness.RangeChangedCount, rangeChanges, "Shift + bánh xe phát RangeChanged");
            double expectedHours = LiveOpsTimelineGeometry.ScrollPixelsPerWheelNotch / scale;
            Assert.AreEqual(expectedHours, (Element.RangeStartUtc - startBefore).TotalHours, 0.02, "một nấc dời 40px sang tương lai");
            Assert.AreEqual(scale, Element.PixelsPerHour, 1e-6, "cuộn ngang không đổi zoom");
            Assert.AreEqual(0, _bodyWheelCount, "không để ScrollView cuộn thêm");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Wheel_Plain_NotConsumed()
        {
            yield return OpenDesignSample();
            CountBodyWheels();
            Vector2 pointer = Element.Ruler.Track.worldBound.center + new Vector2(0f, 60f);
            DateTime startBefore = Element.RangeStartUtc;
            int rangeChanges = Harness.RangeChangedCount;

            _panel.SendWheel(pointer, new Vector2(0f, LiveOpsTimelineElement.WheelDeltaPerNotch), EventModifiers.None);

            Assert.AreEqual(rangeChanges, Harness.RangeChangedCount, "bánh xe trơn không đổi khung");
            Assert.AreEqual(startBefore, Element.RangeStartUtc);
            Assert.AreEqual(1, _bodyWheelCount, "bánh xe trơn tới ScrollView — cuộn dọc danh sách làn như thường");
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------------------------------------ thước + bubble + con trỏ (V-11)

        [UnityTest]
        public IEnumerator Ruler_DayZoom_HasDeviceTimeTier()
        {
            yield return OpenDesignSample(null, LiveOpsTimelineZoom.Day);
            LiveOpsTimelineRuler ruler = Element.Ruler;
            Assert.IsTrue(ruler.HasDeviceTier, "zoom Ngày có tầng 3 giờ máy");
            Assert.AreEqual(DisplayStyle.Flex, ruler.DeviceTier.resolvedStyle.display);
            Assert.AreEqual(LiveOpsTimelineRuler.DeviceTierHeight, ruler.DeviceTier.resolvedStyle.height, 0.5f, "tầng 3 cao 14px");
            Assert.AreEqual(60f, ruler.resolvedStyle.height, 0.5f, "thước zoom Ngày 60px = 18 + 18 + 10 + 14");
            List<string> deviceTexts = TimelineTestQueries.Map(ruler.VisibleDeviceLabels, label => label.text);
            CollectionAssert.AreEqual(new[] { "07:00", "10:00", "13:00", "16:00", "19:00", "22:00", "01:00", "04:00" }, deviceTexts,
                "giờ máy +7 mỗi 3 giờ, cùng x với giờ UTC");
            CollectionAssert.AreEqual(new[] { "00", "03", "06", "09", "12", "15", "18", "21" },
                TimelineTestQueries.Map(ruler.VisibleDayLabels, label => label.text), "tầng 2 là giờ UTC");
            Assert.AreEqual("giờ máy UTC+7", ruler.VisibleDeviceLabels[0].tooltip);
            Assert.AreEqual(ruler.VisibleDayLabels[1].resolvedStyle.left, ruler.VisibleDeviceLabels[1].resolvedStyle.left, 0.5f, "cùng x với vạch UTC");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Ruler_ThreeWeeks_TwoTiersOnly()
        {
            yield return OpenDesignSample();
            LiveOpsTimelineRuler ruler = Element.Ruler;
            Assert.IsFalse(ruler.HasDeviceTier);
            Assert.AreEqual(DisplayStyle.None, ruler.DeviceTier.resolvedStyle.display, "tầng giờ máy chỉ ở zoom Ngày");
            Assert.AreEqual(0, ruler.VisibleDeviceLabels.Count);
            Assert.AreEqual(46f, ruler.resolvedStyle.height, 0.5f, "thước 3 tuần 46px [SD1 §3.2]");
            List<string> monthTexts = TimelineTestQueries.Map(ruler.VisibleMonthLabels, label => label.text);
            CollectionAssert.Contains(monthTexts, "THÁNG 9 2026");
            // Khung Hình 1 bắt đầu trước thứ Hai nên "Tuần 38" cách nhãn tháng đủ xa và vẫn hiện. Khung nào bắt đầu ĐÚNG thứ Hai
            // (mini timeline Hình 12) thì hai nhãn cùng x = 0 và thước bỏ nhãn đứng sau — phiếu D-2 của cổng W4.
            CollectionAssert.Contains(monthTexts, "Tuần 38");
            List<string> dayTexts = TimelineTestQueries.Map(ruler.VisibleDayLabels, label => label.text);
            CollectionAssert.Contains(dayTexts, "T2 14", "thứ Hai ghi \"T2 14\"");
            Assert.IsTrue(TimelineTestQueries.Single(ruler.VisibleDayLabels, label => label.text == "T2 14")
                .ClassListContains(LiveOpsHubClassNames.TimelineRulerLabelEmphasized), "thứ Hai đậm");
            Assert.AreEqual("08:47", ruler.NowFlag.text, "cờ bây giờ ở tầng 1");
            Assert.AreEqual("08:47 UTC · 15:47 giờ máy", ruler.NowFlag.tooltip);
            Assert.IsFalse(ruler.PublishedFlag.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "cờ đã đăng 11/9 16:20 trong khung");
            Assert.AreEqual("Đã đăng 11/9 16:20 · sha " + LiveOpsDesignSample.PublishedSha256Hex.Substring(0, 6), ruler.PublishedFlag.tooltip);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Phiếu D-2 của cổng W4: khung bắt đầu ĐÚNG thứ Hai (mini timeline Hình 12 mở từ 14/9) đặt nhãn tháng và nhãn tuần
        /// vào cùng x = 0 — trước khi sửa, hai chuỗi vẽ đè lên nhau và đọc ra "THÁNG9 2026".
        /// </summary>
        [UnityTest]
        public IEnumerator Ruler_RangeStartsOnMonday_DropsOverlappingWeekLabel()
        {
            _panel = TimelineTestPanel.Open();
            VisualElement root = _panel.CreateRoot(false);
            TimelineHarness harness = TimelineHarness.Create(root, TimelineViewInputs.Checked(LiveOpsDesignSample.Document));
            harness.Start(new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc), LiveOpsTimelineZoom.ThreeWeeks);
            _panel.Harness = harness;
            yield return harness.WaitReady();

            List<string> monthTexts = TimelineTestQueries.Map(Element.Ruler.VisibleMonthLabels, label => label.text);
            CollectionAssert.Contains(monthTexts, "THÁNG 9 2026", "nhãn tháng giữ chỗ — mốc duy nhất nói năm");
            CollectionAssert.DoesNotContain(monthTexts, "Tuần 38", "nhãn tuần ở x = 0 bị bỏ vì chồng lên nhãn tháng");
            CollectionAssert.Contains(monthTexts, "Tuần 39", "thứ Hai sau đó đủ chỗ nên vẫn hiện");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CursorBubble_FollowsPointer_HiddenOnLeave()
        {
            yield return OpenDesignSample();
            Button outside = new Button { text = "outside" };
            Element.parent.Add(outside);
            yield return TimelineTestPanel.WaitUntil(() => TimelineTestPanel.HasLayout(outside), "nút ngoài không có layout");
            LiveOpsTimelineRuler ruler = Element.Ruler;
            LiveOpsTimelineLane lane = Element.FindLaneElement("lava-quest");

            _panel.SendMouse(EventType.MouseMove, TimelineHarness.WorldPointOf(lane, HuntBonusStartUtc.AddMinutes(10), lane.PaddingTop + 14f));
            Assert.IsFalse(ruler.Bubble.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "bubble hiện khi con trỏ trên track");
            Assert.AreEqual("16/9 12:00", ruler.Bubble.text, "giờ UTC đã bắt lưới 1 giờ");
            Assert.AreEqual("19:00 16/9 giờ máy", ruler.Bubble.tooltip, "tooltip giờ máy");
            Assert.AreEqual(Element.Model.Geometry.XOf(HuntBonusStartUtc), ruler.Bubble.style.left.value.value, 0.5f);
            Assert.IsFalse(Element.CursorLine.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "vạch con trỏ đi cùng bubble");

            _panel.SendMouse(EventType.MouseMove, TimelineHarness.WorldPointOf(lane, HuntBonusStartUtc.AddDays(2), lane.PaddingTop + 14f));
            Assert.AreEqual("18/9 12:00", ruler.Bubble.text, "bubble theo con trỏ");

            _panel.SendMouse(EventType.MouseMove, Element.Legend.worldBound.center);
            Assert.IsTrue(ruler.Bubble.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "rời track (xuống chú giải): ẩn");
            _panel.SendMouse(EventType.MouseMove, TimelineHarness.WorldPointOf(lane, HuntBonusStartUtc, lane.PaddingTop + 14f));
            Assert.IsFalse(ruler.Bubble.ClassListContains(LiveOpsHubClassNames.TimelineHidden));
            _panel.SendMouse(EventType.MouseMove, outside.worldBound.center);
            Assert.IsTrue(ruler.Bubble.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "rời timeline: ẩn");
            Assert.IsTrue(Element.CursorLine.ClassListContains(LiveOpsHubClassNames.TimelineHidden));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BarRegions_CursorStyle()
        {
            yield return OpenDesignSample();
            LiveOpsTimelineBar fixedBar = Element.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            LiveOpsTimelineBar endedBar = Element.FindBar(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
            LiveOpsTimelineBar recurringBar = Element.FindBar("weekly-pass#36");
            Assert.IsTrue(endedBar.Model.IsEnded, "tiền đề: 09a đã khép");

            // Vùng bắt là element riêng: mép chỉ có ở thanh kéo được — đã khép và thanh lặp không có mép.
            Assert.IsFalse(fixedBar.StartEdge.ClassListContains(LiveOpsHubClassNames.TimelineHidden));
            Assert.IsFalse(fixedBar.EndEdge.ClassListContains(LiveOpsHubClassNames.TimelineHidden));
            Assert.AreEqual(LiveOpsTimelineGeometry.EdgeGrabWidth, fixedBar.EndEdge.resolvedStyle.width, 0.01f, "vùng bắt mép 8px");
            Assert.AreEqual(DisplayStyle.None, endedBar.EndEdge.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, recurringBar.StartEdge.resolvedStyle.display);

            int? fixedCursor = CursorId(fixedBar);
            if (!fixedCursor.HasValue)
            {
                Assert.Ignore("resolvedStyle không lộ cursor ở bản Unity này — kiểm tay M-15 (thân move-arrow, mép resize-horizontal, lặp/khép arrow)");
            }
            Assert.AreEqual((int)MouseCursor.MoveArrow, fixedCursor.Value, "thân thanh cố định: move-arrow");
            Assert.AreEqual((int)MouseCursor.ResizeHorizontal, CursorId(fixedBar.EndEdge), "mép 8px: resize-horizontal");
            Assert.AreEqual((int)MouseCursor.Arrow, CursorId(endedBar), "thanh đã khép: arrow");
            Assert.AreEqual((int)MouseCursor.Arrow, CursorId(recurringBar), "thanh lặp: arrow");
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------------------------------------ V-22

        [UnityTest]
        public IEnumerator FixedStrip_DrawnAsStrip_ClickZooms_NoDragIntent()
        {
            LiveEventCalendarDocument document = TimelineViewInputs.WithDenseFixedBurst(LiveOpsDesignSample.Document);
            _panel = TimelineTestPanel.Open();
            VisualElement root = _panel.CreateRoot(false);
            TimelineHarness harness = TimelineHarness.Create(root, TimelineViewInputs.Unchecked(document));
            _panel.Harness = harness;
            harness.Start(TimelineViewInputs.DailyYearStartUtc, LiveOpsTimelineZoom.Month);
            yield return harness.WaitReady();
            double scaleBeforeClick = Element.PixelsPerHour;

            LiveOpsTimelineLane lane = Element.FindLaneElement(TimelineViewInputs.DenseBurstType);
            LiveOpsTimelineBar strip = null;
            for (int index = 0; index < lane.BarCount && strip == null; index++)
            {
                if (lane.BarAt(index).Model.Source == LiveOpsTimelineBarSource.FixedStrip) strip = lane.BarAt(index);
            }
            Assert.IsNotNull(strip, "tiền đề: làn cố định dày ở zoom Tháng gom thành dải FixedStrip");
            Assert.IsTrue(strip.ClassListContains(LiveOpsHubClassNames.TimelineBarMerged), "vẽ như dải lặp: một element, class --merged");
            Assert.IsTrue(strip.StartEdge.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "không tay cầm kéo");
            Assert.IsTrue(strip.EndEdge.ClassListContains(LiveOpsHubClassNames.TimelineHidden));
            Assert.IsTrue(strip.LoopIcon.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "không icon lặp");
            DateTime stripStart = strip.Model.StartUtc;
            DateTime stripEnd = strip.Model.EndUtc;

            harness.Intents.Clear();
            Vector2 center = strip.worldBound.center;
            _panel.SendMouse(EventType.MouseDown, center);
            _panel.SendMouse(EventType.MouseDrag, center + new Vector2(30f, 0f));
            _panel.SendMouse(EventType.MouseUp, center + new Vector2(30f, 0f));

            Assert.AreEqual(0, harness.IntentsOf<MoveBarIntent>().Count, "dải chỉ đọc: không MoveBarIntent/ResizeBarIntent");
            Assert.AreEqual(0, harness.IntentsOf<SelectBarIntent>().Count, "BarKey của dải không phải EntryKey — không chọn như đợt");
            Assert.IsTrue(Element.IsContinuousScale);
            Assert.LessOrEqual(Element.RangeStartUtc, stripStart, "bấm dải = zoom vào [StartUtc, EndUtc]");
            Assert.GreaterOrEqual(Element.RangeEndUtc, stripEnd);
            Assert.Greater(Element.PixelsPerHour, scaleBeforeClick, "đã zoom VÀO — px/giờ lớn hơn trước khi bấm");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator UntypedLane_HeaderShowsPlaceholderName()
        {
            LiveEventCalendarDocumentBuilder builder = TimelineDesignSampleInput.CopyOf(LiveOpsDesignSample.Document);
            builder.WithFixedEvent(new FixedLiveEventEntry("entry-untyped", "quest-0915", string.Empty, "2026-09-15T00:00:00Z", "2026-09-16T00:00:00Z",
                string.Empty));
            yield return OpenDesignSample(builder.Build());

            LiveOpsTimelineLaneHeader header = Element.FindHeader(string.Empty);
            Assert.IsNotNull(header, "đợt chưa ghi loại có làn TypeId \"\"");
            Assert.AreEqual("(chưa ghi loại)", header.NameLabel.text, "tên giữ chỗ thay ô tên trống");
            Assert.IsTrue(header.NameLabel.ClassListContains(LiveOpsHubClassNames.TimelineLaneNamePlaceholder));
            Assert.AreEqual("chưa ghi loại · 1 đợt", header.MetaLabel.text, "meta của model");
            Assert.IsTrue(header.Swatch.ClassListContains(LiveOpsHubClassNames.TimelineHidden), "không có loại thì không swatch màu loại");
            Assert.Greater(header.NameLabel.resolvedStyle.width, 0f);
            Assert.IsNotNull(Element.FindBar("entry-untyped"), "thanh của đợt chưa ghi loại vẫn vẽ");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MinimapAndBarTooltips_UseFindingText()
        {
            yield return OpenDesignSample();
            LiveOpsTimelineMinimapMark bonusMark = TimelineTestQueries.Single(Element.Model.Minimap.Marks,
                mark => mark.TargetEntryKey == LiveOpsDesignSample.HuntBonusEntryKey && mark.Finding != null && mark.State == HealthState.Blocked);
            // [SD1 §3.6] nối bằng khoảng trắng ("Cảnh báo · weekly-pass-35 đổi id"); ngoặc chỉ dành cho vạch không có phát hiện.
            string expected = "Bị bỏ · hunt-0916-bonus " + LiveOpsFindingText.PlainText(LiveOpsFindingText.ShortLabel(bonusMark.Finding));
            Assert.AreEqual(expected, LiveOpsTimelineMinimap.MarkTooltip(bonusMark), "vạch minimap: câu ngắn của LiveOpsFindingText (CC-FT-1)");
            float markX = Element.Model.Minimap.XOf(bonusMark.AtUtc) * Element.Minimap.contentRect.width / Element.Model.Minimap.Width;
            Assert.AreEqual(expected, Element.Minimap.TooltipAt(new Vector2(markX + 1f, 5f)));
            StringAssert.StartsWith("Minimap 14/8 → 13/10 · vạch đỏ", Element.Minimap.TooltipAt(new Vector2(2f, 5f)), "chỗ trống: câu chú giải");

            string barTooltip = Element.FindBar(LiveOpsDesignSample.HuntBonusEntryKey).tooltip;
            StringAssert.StartsWith("hunt-0916-bonus · 16/9 12:00 → 18/9 00:00 UTC", barTooltip);
            StringAssert.Contains(expected, barTooltip, "tooltip thanh dùng cùng câu với vạch minimap");
            Assert.IsFalse(barTooltip.Contains("<noparse>"), "tooltip không rich text");
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------------------------------------ hỗ trợ

        private int _bodyWheelCount;

        private int CountBodyWheels()
        {
            _bodyWheelCount = 0;
            Element.Body.RegisterCallback<WheelEvent>(wheelEvent => _bodyWheelCount++);
            return 0;
        }

        private DateTime TimeAtTrack(float trackPosition)
        {
            return Element.RangeStartUtc.AddHours(trackPosition / Element.PixelsPerHour);
        }

        private void AssertHit(Vector2 world, LiveOpsTimelineHitKind kind, LiveOpsTimelineBarRegion region, string context)
        {
            LiveOpsTimelineHit hit = Element.HitTest(Element.WorldToLocal(world));
            Assert.AreEqual(kind, hit.Kind, context);
            Assert.AreEqual(region, hit.BarRegion, context);
        }

        private static bool IsFocused(Focusable focusable)
        {
            VisualElement element = focusable as VisualElement;
            return element?.panel?.focusController?.focusedElement == focusable;
        }

        private static string LastBarKey(LiveOpsTimelineModel model)
        {
            LiveOpsTimelineLaneModel lastLane = model.Lanes[model.Lanes.Count - 1];
            LiveOpsTimelineBarModel last = null;
            foreach (LiveOpsTimelineBarModel bar in lastLane.Bars)
            {
                if (bar.IsStrip) continue;
                if (last == null || bar.StartUtc >= last.StartUtc) last = bar;
            }
            Assert.IsNotNull(last, "làn cuối phải có thanh");
            return last.BarKey;
        }

        /// <summary>
        /// Id con trỏ đã tính của element qua reflection (<c>computedStyle.cursor</c> nội bộ): <c>resolvedStyle</c> không có cursor ở cả hai
        /// bản. null khi bản Unity đổi cấu trúc — test chuyển sang kiểm tay M-15 thay vì báo sai.
        /// </summary>
        private static int? CursorId(VisualElement element)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            object computed;
            try
            {
                // 6000.6: computedStyle là property trả `ref readonly` — Reflection từ chối gọi (NotSupportedException), không có
                // đường nào khác đọc cursor đã tính. 2022.3 cũng không đọc được (cấu trúc style khác), nên test Ignore ở CẢ HAI bản
                // và con trỏ chuyển hẳn sang kiểm tay M-15 — đúng dự liệu của V-11 ("resolvedStyle.cursor nếu đọc được; không thì M-15").
                computed = typeof(VisualElement).GetProperty("computedStyle", flags)?.GetValue(element);
            }
            catch (NotSupportedException)
            {
                return null;
            }
            if (computed == null) return null;
            object cursor = computed.GetType().GetProperty("cursor", flags)?.GetValue(computed);
            if (cursor == null) return null;
            PropertyInfo defaultCursor = cursor.GetType().GetProperty("defaultCursorId", flags);
            if (defaultCursor != null) return (int)defaultCursor.GetValue(cursor);
            FieldInfo defaultCursorField = cursor.GetType().GetField("m_DefaultCursorId", flags) ?? cursor.GetType().GetField("defaultCursorId", flags);
            return defaultCursorField != null ? (int?)(int)defaultCursorField.GetValue(cursor) : null;
        }
    }

    /// <summary>
    /// Cửa sổ thử có root kiểu hub (thường hoặc <c>liveops-hub--skin-light</c>) nạp theme → components → controls → timeline cho test UI
    /// và kịch bản chụp của thư mục Timeline. Không mở cửa sổ hub: skin probe của hub tự bật class theo nền thật, còn timeline không cần
    /// khung. Gửi chuột/bánh xe/phím/lệnh qua <c>EditorWindow.SendEvent</c>. Vòng chờ fail khi quá CẢ 60 khung lẫn 5 giây (V-23).
    /// </summary>
    [Category(LiveOpsHubTestCategories.UI)]
    internal sealed class TimelineTestPanel : IDisposable
    {
        internal const int MaximumLayoutFrames = 60;
        internal const double MaximumWaitSeconds = 5d;
        internal const float DefaultWidth = 1000f;
        internal const float DefaultHeight = 560f;

        private TimelineProbeWindow _window;

        private TimelineTestPanel(TimelineProbeWindow window)
        {
            _window = window;
        }

        public EditorWindow Window => _window;
        public TimelineHarness Harness { get; set; }

        /// <summary>⌘ trên macOS, Ctrl nơi khác — đúng <c>actionKey</c> của sự kiện UI Toolkit.</summary>
        public static EventModifiers ActionModifier => Application.platform == RuntimePlatform.OSXEditor ? EventModifiers.Command : EventModifiers.Control;

        public static TimelineTestPanel Open(float width = DefaultWidth, float height = DefaultHeight)
        {
            TimelineProbeWindow window = ScriptableObject.CreateInstance<TimelineProbeWindow>();
            window.Show();
            // Đặt kích thước SAU Show (SP-16) — đặt trước bị kẹp ≈ 401×202.
            window.position = new Rect(0f, 0f, width, height);
            window.rootVisualElement.style.flexDirection = FlexDirection.Row;
            return new TimelineTestPanel(window);
        }

        public VisualElement CreateRoot(bool lightSkin)
        {
            VisualElement root = CreateStyledRoot(lightSkin);
            _window.rootVisualElement.Add(root);
            return root;
        }

        /// <summary>Root có token hai skin + bốn stylesheet theo thứ tự nạp của khung (không gắn vào cây — nơi gọi tự Add).</summary>
        public static VisualElement CreateStyledRoot(bool lightSkin)
        {
            VisualElement root = new VisualElement();
            root.AddToClassList(LiveOpsHubClassNames.Root);
            root.EnableInClassList(LiveOpsHubClassNames.SkinLight, lightSkin);
            root.style.flexGrow = 1f;
            root.style.flexBasis = 0f;
            foreach (string path in new[] { LiveOpsHubPaths.ThemeUss, LiveOpsHubPaths.ComponentsUss, LiveOpsHubPaths.ControlsUss, LiveOpsHubPaths.TimelineUss })
            {
                StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                Assert.IsNotNull(sheet, "thiếu " + path);
                root.styleSheets.Add(sheet);
            }
            return root;
        }

        public void SendMouse(EventType type, Vector2 position, int button = 0, EventModifiers modifiers = EventModifiers.None, int clickCount = 1)
        {
            _window.SendEvent(new Event { type = type, mousePosition = position, button = button, modifiers = modifiers, clickCount = clickCount });
        }

        public void SendWheel(Vector2 position, Vector2 delta, EventModifiers modifiers)
        {
            _window.SendEvent(new Event { type = EventType.ScrollWheel, mousePosition = position, delta = delta, modifiers = modifiers });
        }

        public void SendKey(string key)
        {
            _window.SendEvent(Event.KeyboardEvent(key));
        }

        /// <summary>Validate rồi Execute như Edit menu của Unity ([API §12.1]).</summary>
        public void SendCommand(string commandName)
        {
            _window.SendEvent(new Event { type = EventType.ValidateCommand, commandName = commandName });
            _window.SendEvent(EditorGUIUtility.CommandEvent(commandName));
        }

        /// <summary>
        /// Chờ <paramref name="target"/> giữ focus, ĐÒI LẠI focus mỗi khung. Vì sao không gọi <c>Focus()</c> một lần rồi chờ: trong
        /// batchmode 2022.3 cửa sổ thử có thể chưa là cửa sổ nhận phím ở khung đầu (nhất là khi máy đang chạy hai Unity song song),
        /// nên lần Focus() đầu rơi vào hư không và vòng chờ hết hạn oan (V-23).
        /// </summary>
        public IEnumerator WaitForFocus(Focusable target, string failureMessage)
        {
            _window.Focus();
            target.Focus();
            yield return WaitUntil(() =>
            {
                VisualElement element = target as VisualElement;
                if (element?.panel?.focusController?.focusedElement == target) return true;
                target.Focus();
                return false;
            }, failureMessage);
        }

        public static IEnumerator WaitUntil(Func<bool> condition, string failureMessage)
        {
            int frames = 0;
            double deadline = EditorApplication.timeSinceStartup + MaximumWaitSeconds;
            while (!condition())
            {
                if (++frames > MaximumLayoutFrames && EditorApplication.timeSinceStartup > deadline)
                {
                    Assert.Fail(failureMessage + " (sau " + MaximumLayoutFrames + " khung và " + MaximumWaitSeconds + " giây — test UI phải chạy không -nographics)");
                }
                yield return null;
            }
        }

        public static bool HasLayout(VisualElement element)
        {
            if (element == null || element.panel == null) return false;
            Rect layout = element.layout;
            return !float.IsNaN(layout.width) && layout.width > 0f && !float.IsNaN(layout.height) && layout.height > 0f;
        }

        /// <summary>Vẽ ngay một khung (reflection <c>m_Parent.RepaintImmediately</c> như lệnh chụp) để generateVisualContent chạy trong batch.</summary>
        public void RepaintImmediately()
        {
            object hostView = typeof(EditorWindow).GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_window);
            MethodInfo repaint = hostView?.GetType().GetMethod("RepaintImmediately", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null, Type.EmptyTypes, null);
            if (repaint != null) repaint.Invoke(hostView, null);
            else _window.Repaint();
        }

        public void Dispose()
        {
            if (_window != null) _window.Close();
            _window = null;
        }

        private sealed class TimelineProbeWindow : EditorWindow
        {
        }
    }

    /// <summary>
    /// Vai presenter tối thiểu của test: nghe <see cref="LiveOpsTimelineElement.RangeChanged"/> rồi dựng lại model từ input (khoảng + bề
    /// rộng track thật của element), gom intent/hover/yêu cầu menu để assert. Không sửa tài liệu — intent chỉ được ghi lại.
    /// </summary>
    internal sealed class TimelineHarness
    {
        private readonly Func<LiveOpsTimelineInput> _inputFactory;

        private TimelineHarness(LiveOpsTimelineElement element, Func<LiveOpsTimelineInput> inputFactory)
        {
            Element = element;
            _inputFactory = inputFactory;
            element.IntentRaised += intent => Intents.Add(intent);
            element.HoverChanged += hover => Hovers.Add(hover);
            element.ContextRequested += request => ContextRequests.Add(request);
            element.RangeChanged += (startUtc, endUtc) =>
            {
                RangeChangedCount++;
                Rebuild();
            };
        }

        public LiveOpsTimelineElement Element { get; }
        public List<LiveOpsTimelineIntent> Intents { get; } = new List<LiveOpsTimelineIntent>();
        public List<LiveOpsTimelineHover> Hovers { get; } = new List<LiveOpsTimelineHover>();
        public List<LiveOpsTimelineContextRequest> ContextRequests { get; } = new List<LiveOpsTimelineContextRequest>();
        public int RangeChangedCount { get; private set; }

        public static TimelineHarness Create(VisualElement parent, Func<LiveOpsTimelineInput> inputFactory)
        {
            LiveOpsTimelineElement element = new LiveOpsTimelineElement();
            parent.Add(element);
            element.SetDeviceOffset(LiveOpsDesignSample.DeviceOffset);
            return new TimelineHarness(element, inputFactory);
        }

        public void Start(DateTime rangeStartUtc, LiveOpsTimelineZoom zoom)
        {
            Element.SetRange(rangeStartUtc, zoom);
        }

        public void Rebuild()
        {
            LiveOpsTimelineInput input = _inputFactory()
                .WithRange(Element.RangeStartUtc, Element.RangeEndUtc)
                .WithTrackWidth(Element.TrackWidth);
            Element.SetModel(input.Build());
        }

        /// <summary>Chờ layout thật và model đã dựng theo đúng bề rộng track sau layout, rồi thêm hai khung cho style/geometry event.</summary>
        public IEnumerator WaitReady()
        {
            yield return TimelineTestPanel.WaitUntil(() =>
                    Element.Model != null && TimelineTestPanel.HasLayout(Element.Ruler.Track) &&
                    Math.Abs(Element.Model.Geometry.TrackWidth - Element.Ruler.Track.layout.width) < 0.5f &&
                    Element.LaneCount > 0 && TimelineTestPanel.HasLayout(Element.LaneAt(0)),
                "timeline chưa có layout/model theo bề rộng track");
            yield return null;
            yield return null;
        }

        public List<TIntent> IntentsOf<TIntent>() where TIntent : LiveOpsTimelineIntent
        {
            var matches = new List<TIntent>();
            foreach (LiveOpsTimelineIntent intent in Intents)
            {
                if (intent is TIntent typed) matches.Add(typed);
            }
            return matches;
        }

        /// <summary>Toạ độ panel của giờ <paramref name="utc"/> trong làn, ở độ cao <paramref name="lanePosition"/> tính từ đỉnh làn.</summary>
        public static Vector2 WorldPointOf(LiveOpsTimelineLane lane, DateTime utc, float lanePosition)
        {
            return lane.LocalToWorld(new Vector2(lane.Geometry.XOf(utc), lanePosition));
        }
    }

    /// <summary>Đầu vào model cho test/chụp timeline: tài liệu bất kỳ + bản đã đăng mẫu đọc bằng parser thật + validator/diff core.</summary>
    internal static class TimelineViewInputs
    {
        public const string DailyFixedType = "daily-login";
        public const string DenseBurstType = "golden-hour";

        /// <summary>240 đợt × 2 giờ = 20 ngày — hẹp hơn khung Tháng (42 ngày) nên bấm dải còn chỗ để zoom vào.</summary>
        public const int DenseBurstCount = 240;
        public static readonly DateTime DailyYearStartUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Có báo cáo kiểm + diff với bản đã đăng 11/9 16:20 (tô lỗi, vuông "khác bản đã đăng", cờ đã đăng) — tính một lần.</summary>
        public static Func<LiveOpsTimelineInput> Checked(LiveEventCalendarDocument document)
        {
            DateTime nowUtc = LiveOpsDesignSample.NowUtc;
            LiveEventCalendarDocumentParseResult baseline = JsonLiveEventCalendarParser.ParseDocument(LiveOpsDesignSample.PublishedSnapshotJson);
            Assert.IsTrue(baseline.IsReadable, "JSON đã đăng mẫu phải đọc được");
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.CompileInExportOrder(document);
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(new LiveEventCalendarCheckContextBuilder(document, nowUtc)
                .WithCompilation(compilation)
                .WithPublishedBaseline(baseline.Document)
                .WithLatestStampBaseline(baseline.Document)
                .Build());
            LiveEventCalendarDiffResult diff = LiveEventCalendarDiff.Compare(baseline.Document, document, nowUtc);
            return () => new LiveOpsTimelineInput()
                .WithDocument(document)
                .WithCompilation(compilation)
                .WithCheckReport(report)
                .WithPublishedDiff(diff)
                .WithNowUtc(nowUtc);
        }

        /// <summary>Không báo cáo, không diff — lịch lớn (một năm) dựng nhanh; test ngân sách vertex không cần tô lỗi.</summary>
        public static Func<LiveOpsTimelineInput> Unchecked(LiveEventCalendarDocument document)
        {
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.CompileInExportOrder(document);
            return () => new LiveOpsTimelineInput().WithDocument(document).WithCompilation(compilation).WithNowUtc(LiveOpsDesignSample.NowUtc);
        }

        public static LiveEventCalendarDocument WithFixedTimes(LiveEventCalendarDocument document, string entryKey, string startUtcText, string endUtcText)
        {
            var builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(document.RemoteConfigKey);
            foreach (LiveEventTypeDefinition type in document.EventTypes) builder.WithEventType(type);
            foreach (RecurringLiveEventRule rule in document.RecurringRules) builder.WithRecurringRule(rule);
            foreach (FixedLiveEventEntry entry in document.FixedEvents)
            {
                builder.WithFixedEvent(entry.EntryKey == entryKey ? entry.WithTimes(startUtcText, endUtcText) : entry);
            }
            foreach (PublishedCalendarStamp stamp in document.PublishedStamps) builder.WithPublishedStamp(stamp);
            return builder.Build();
        }

        /// <summary>
        /// Dữ liệu mẫu + một loại cố định có HAI đợt mỗi ngày (00:00–10:00 và 12:00–22:00) suốt một năm từ 1/9/2026 — 730 thanh.
        /// Vì sao hai chứ không một: ở 0,25 px/giờ một track ≈ 830px chỉ chứa ≈ 139 ngày, mà ≈ 139 thanh vẫn lọt ngân sách một làn
        /// (20.000 vertex) nên model không phải gom dải — tức là không dựng được cảnh "làn dày phải gom" của R-10 / CC-TLMODEL-1.
        /// </summary>
        /// <summary>
        /// Dữ liệu mẫu + một loại cố định chạy 1 giờ MỖI 2 GIỜ trong 20 ngày từ 1/9/2026 (240 thanh trong một đợt liền).
        /// Vì sao không dùng fixture cả năm cho test dải: ở zoom xa cả năm gom thành MỘT dải rộng hơn cả khung nhìn, nên "bấm dải =
        /// zoom vào" không kiểm được (khung đã ở 0,25 px/giờ, không zoom vào nổi). Đợt dày nhưng có biên (20 ngày trong khung Tháng
        /// 42 ngày) cho đúng cảnh của CC-TLMODEL-1: dải hẹp hơn khung, bấm vào là zoom vào.
        /// </summary>
        public static LiveEventCalendarDocument WithDenseFixedBurst(LiveEventCalendarDocument document)
        {
            LiveEventCalendarDocumentBuilder builder = TimelineDesignSampleInput.CopyOf(document);
            builder.WithEventType(new LiveEventTypeDefinition(DenseBurstType, "Giờ vàng", 6, false, "golden_hour_v1"));
            DateTime slot = DailyYearStartUtc;
            for (int index = 0; index < DenseBurstCount; index++, slot = slot.AddHours(2))
            {
                string stamp = index.ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
                builder.WithFixedEvent(new FixedLiveEventEntry("entry-golden-" + stamp, "golden-" + stamp, DenseBurstType,
                    LiveEventUtcText.Format(slot), LiveEventUtcText.Format(slot.AddHours(1)), string.Empty));
            }
            return builder.Build();
        }

        public static LiveEventCalendarDocument WithDailyFixedYear(LiveEventCalendarDocument document)
        {
            LiveEventCalendarDocumentBuilder builder = TimelineDesignSampleInput.CopyOf(document);
            builder.WithEventType(new LiveEventTypeDefinition(DailyFixedType, "Đăng nhập mỗi ngày", 5, false, "daily_login_v1"));
            DateTime day = DailyYearStartUtc;
            for (int index = 0; index < 365; index++, day = day.AddDays(1))
            {
                string stamp = day.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                builder.WithFixedEvent(new FixedLiveEventEntry("entry-login-" + stamp + "-a", "login-" + stamp + "-a", DailyFixedType,
                    LiveEventUtcText.Format(day), LiveEventUtcText.Format(day.AddHours(10)), string.Empty));
                builder.WithFixedEvent(new FixedLiveEventEntry("entry-login-" + stamp + "-b", "login-" + stamp + "-b", DailyFixedType,
                    LiveEventUtcText.Format(day.AddHours(12)), LiveEventUtcText.Format(day.AddHours(22)), string.Empty));
            }
            return builder.Build();
        }
    }
}
