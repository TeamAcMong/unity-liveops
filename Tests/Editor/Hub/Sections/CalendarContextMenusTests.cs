using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Nội dung năm menu chuột phải của màn Lịch [FD §3.9]. Menu gốc của Unity KHÔNG chụp ảnh được (S-24), nên đây là thứ duy
    /// nhất chứng minh menu đúng: đủ mục, đúng thứ tự, và mục không dùng được thì Ở LẠI kèm lý do thành chữ (SPIKE-B SP-3).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class CalendarContextMenusTests
    {
        private const string LaneTypeId = "lava-quest";
        private const string BarKey = "entry-lava-quest-2026-09b";

        [Test]
        public void FixedBarMenu_HasEveryItemInDesignOrder()
        {
            IReadOnlyList<CalendarMenuItem> items = CalendarContextMenus.ForFixedBar(FullContext());

            CollectionAssert.AreEqual(new[]
            {
                CalendarMenuItemId.EditInInspector, CalendarMenuItemId.Duplicate, CalendarMenuItemId.Frame,
                CalendarMenuItemId.MoveStart, CalendarMenuItemId.SetDuration, CalendarMenuItemId.RevertToCompare,
                CalendarMenuItemId.CopyId, CalendarMenuItemId.CopyEventJson, CalendarMenuItemId.Delete,
            }, IdsOf(items), "menu thanh cố định phải đủ chín mục đúng thứ tự mockup [FD §3.9]");
        }

        /// <summary>Nhân bản nêu ĐÍCH cụ thể — không có đích thì người dùng phải đoán mục này sẽ tạo đợt ở đâu.</summary>
        [Test]
        public void FixedBarMenu_DuplicateNamesTargetAndOffset()
        {
            CalendarMenuItem duplicate = ItemOf(CalendarContextMenus.ForFixedBar(FullContext()), CalendarMenuItemId.Duplicate);

            Assert.IsTrue(duplicate.IsEnabled);
            StringAssert.Contains("24/9 00:00", duplicate.Text);
            StringAssert.Contains("7 ngày", duplicate.Text);
        }

        [Test]
        public void RecurringBarMenu_HasDisabledReasonAndNoSimulation()
        {
            IReadOnlyList<CalendarMenuItem> items = CalendarContextMenus.ForRecurringBar(FullContext());

            CollectionAssert.AreEqual(new[]
            {
                CalendarMenuItemId.OpenRule, CalendarMenuItemId.Frame, CalendarMenuItemId.CopyId,
                CalendarMenuItemId.RecurringReadOnly,
            }, IdsOf(items), "thanh sinh từ luật: ba mục dùng được + một mục nói vì sao không sửa được (PD-1: không có Mô phỏng)");
            Assert.IsFalse(ItemOf(items, CalendarMenuItemId.RecurringReadOnly).IsEnabled);
            StringAssert.Contains(LaneTypeId, ItemOf(items, CalendarMenuItemId.OpenRule).Text);
        }

        /// <summary>
        /// Làn ở mép: mục Đưa lên/Đưa xuống KHÔNG bị giấu — nó ở lại, disabled, và nhãn tự nói "Đã ở đầu" / "Đã ở cuối". Giấu mục
        /// làm người dùng tưởng bản này không có tính năng đổi thứ tự làn.
        /// </summary>
        [Test]
        public void LaneHeaderMenu_EdgeLaneKeepsMoveItemsWithReasonAsText()
        {
            CalendarMenuContext context = FullContext();
            context.CanMoveLaneUp = false;
            context.CanMoveLaneDown = true;
            IReadOnlyList<CalendarMenuItem> items = CalendarContextMenus.ForLaneHeader(context);

            CalendarMenuItem up = ItemOf(items, CalendarMenuItemId.MoveLaneUp);
            Assert.IsFalse(up.IsEnabled);
            StringAssert.Contains(LiveOpsHubStrings.CalendarDepthMenuLaneAtTopReason, up.Text);
            Assert.IsTrue(ItemOf(items, CalendarMenuItemId.MoveLaneDown).IsEnabled);
            StringAssert.Contains(LaneTypeId, ItemOf(items, CalendarMenuItemId.AddForLane).Text);
        }

        /// <summary>"Thu gọn / Mở làn" là việc của G-OPT-TIMELINE (W6): mục chưa có thật thì KHÔNG hiện, kể cả dạng disabled.</summary>
        [Test]
        public void LaneHeaderMenu_HasNoCollapseItemBeforeOptTimeline()
        {
            IReadOnlyList<CalendarMenuItem> items = CalendarContextMenus.ForLaneHeader(FullContext());

            CollectionAssert.AreEqual(new[]
            {
                CalendarMenuItemId.HideLane, CalendarMenuItemId.MoveLaneUp, CalendarMenuItemId.MoveLaneDown,
                CalendarMenuItemId.ShowAllLanes, CalendarMenuItemId.None, CalendarMenuItemId.AddForLane,
                CalendarMenuItemId.OpenEventTypes,
            }, IdsOf(items));
        }

        [Test]
        public void EmptyLaneMenu_PasteDisabledSaysWhy()
        {
            CalendarMenuContext context = FullContext();
            context.HasCopiedEvent = false;
            IReadOnlyList<CalendarMenuItem> items = CalendarContextMenus.ForEmptyLane(context);

            StringAssert.Contains("18/9 00:00", ItemOf(items, CalendarMenuItemId.AddAtCursor).Text);
            CalendarMenuItem paste = ItemOf(items, CalendarMenuItemId.PasteAtCursor);
            Assert.IsFalse(paste.IsEnabled);
            StringAssert.Contains(LiveOpsHubStrings.CalendarDepthMenuPasteDisabledReason, paste.Text);
        }

        [Test]
        public void EmptyLaneMenu_PasteEnabledAfterCopy()
        {
            CalendarMenuContext context = FullContext();
            context.HasCopiedEvent = true;

            Assert.IsTrue(ItemOf(CalendarContextMenus.ForEmptyLane(context), CalendarMenuItemId.PasteAtCursor).IsEnabled);
        }

        /// <summary>(V-13) Nguồn Disk đổi ĐỘNG TỪ của mục: bản đang so không phải bản đã đăng nên không được nói "Hoàn về bản đã đăng".</summary>
        [Test]
        public void CompareRowMenu_DiskSourceUsesTakeFromDiskVerb()
        {
            CalendarMenuContext context = FullContext();
            context.CompareSource = LiveOpsHubCompareSource.Disk;

            CalendarMenuItem item = ItemOf(CalendarContextMenus.ForCompareRow(context), CalendarMenuItemId.RevertToCompare);
            Assert.AreEqual(LiveOpsHubStrings.CalendarDepthTakeFromDisk, item.Text);

            context.CompareSource = LiveOpsHubCompareSource.Published;
            Assert.AreEqual(LiveOpsHubStrings.CalendarDepthRevertToPublished,
                ItemOf(CalendarContextMenus.ForCompareRow(context), CalendarMenuItemId.RevertToCompare).Text);
        }

        [Test]
        public void CompareRowMenu_WithoutBaselineIsDisabledWithReason()
        {
            CalendarMenuContext context = FullContext();
            context.CanRevertToCompare = false;

            CalendarMenuItem item = ItemOf(CalendarContextMenus.ForCompareRow(context), CalendarMenuItemId.RevertToCompare);
            Assert.IsFalse(item.IsEnabled);
            StringAssert.Contains(LiveOpsHubStrings.CalendarDepthCompareUnavailableReason, item.Text);
        }

        private static CalendarMenuContext FullContext()
        {
            return new CalendarMenuContext
            {
                BarKey = BarKey,
                LaneTypeId = LaneTypeId,
                CursorTimeText = "18/9 00:00",
                CanDuplicate = true,
                DuplicateTargetText = "24/9 00:00",
                DuplicateOffsetText = "7 ngày",
                CanRevertToCompare = true,
                HasCopiedEvent = true,
                CanMoveLaneUp = true,
                CanMoveLaneDown = true,
                CompareSource = LiveOpsHubCompareSource.Published,
            };
        }

        private static CalendarMenuItemId[] IdsOf(IReadOnlyList<CalendarMenuItem> items)
        {
            CalendarMenuItemId[] ids = new CalendarMenuItemId[items.Count];
            for (int index = 0; index < items.Count; index++) ids[index] = items[index].Id;
            return ids;
        }

        private static CalendarMenuItem ItemOf(IReadOnlyList<CalendarMenuItem> items, CalendarMenuItemId id)
        {
            for (int index = 0; index < items.Count; index++)
            {
                if (items[index].Id == id) return items[index];
            }
            Assert.Fail("menu thiếu mục " + id);
            return null;
        }
    }
}
