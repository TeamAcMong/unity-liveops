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

        /// <summary>
        /// Nhánh KHOÁ của "Nhân bản" (giờ bắt đầu không đọc được, vd lava-quest-2026-10 với "2026-10-3"): nhãn không được là cái
        /// khuôn "{0}/{1}" và phải nói lý do thành chữ (SPIKE-B SP-3). Nhánh này chưa từng có test — đúng chỗ lỗi lọt qua W5.
        /// </summary>
        [Test]
        public void FixedBarMenu_DuplicateDisabled_SaysWhyWithoutFormatHolders()
        {
            CalendarMenuContext context = FullContext();
            context.CanDuplicate = false;

            CalendarMenuItem duplicate = ItemOf(CalendarContextMenus.ForFixedBar(context), CalendarMenuItemId.Duplicate);

            Assert.IsFalse(duplicate.IsEnabled);
            StringAssert.DoesNotContain("{0}", duplicate.Text, "nhãn mục khoá không được in chỗ giữ chỗ của khuôn");
            StringAssert.DoesNotContain("{1}", duplicate.Text);
            StringAssert.Contains(LiveOpsHubStrings.CalendarDepthMenuDuplicateUnreadableStartReason, duplicate.Text,
                "mục khoá luôn nói VÌ SAO thành chữ");
        }

        /// <summary>
        /// <see cref="UnityEditor.GenericMenu"/> coi '/' là dấu PHÂN CẤP, mà mọi nhãn có ngày giờ của hub đều mang '/' ("24/9
        /// 00:00"). Không né thì hai mục quan trọng nhất của [FD §3.9] bị bẻ thành menu con và biến mất khỏi tầng một. Test đi qua
        /// đúng đường dựng menu THẬT (<c>PopulateGenericMenu</c>) chứ không chỉ đọc danh sách dữ liệu.
        /// </summary>
        [Test]
        public void GenericMenu_LabelsKeepDatesOnOneLevel()
        {
            IReadOnlyList<CalendarMenuItem> items = CalendarContextMenus.ForFixedBar(FullContext());
            StringAssert.Contains("/", ItemOf(items, CalendarMenuItemId.Duplicate).Text,
                "nhãn gốc VẪN viết ngày kiểu 24/9 — dạng ngày giờ của cả hub [SD1 §3.13]");

            UnityEditor.GenericMenu menu = new UnityEditor.GenericMenu();
            CalendarContextMenus.PopulateGenericMenu(menu, items, _ => { });

            Assert.AreEqual(items.Count, menu.GetItemCount(), "mỗi mục dữ liệu đúng MỘT mục menu, không mục nào bị bẻ đôi");
            for (int index = 0; index < items.Count; index++)
            {
                StringAssert.DoesNotContain("/", CalendarContextMenus.EscapeMenuLabel(items[index].Text),
                    "nhãn đẩy vào GenericMenu không được còn dấu phân cấp: " + items[index].Text);
            }
        }

        [Test]
        public void RecurringBarMenu_HasDisabledReasonAndNoSimulation()
        {
            IReadOnlyList<CalendarMenuItem> items = CalendarContextMenus.ForRecurringBar(FullContext());

            CollectionAssert.AreEqual(new[]
            {
                CalendarMenuItemId.OpenRule, CalendarMenuItemId.Frame, CalendarMenuItemId.CopyId,
                CalendarMenuItemId.None, CalendarMenuItemId.RecurringReadOnly,
            }, IdsOf(items), "thanh sinh từ luật: ba mục dùng được, mục ngăn cách, rồi mục nói vì sao không sửa được"
                + " ([SD1 §3.8] Hình 12 khung 10; PD-1: không có Mô phỏng)");
            Assert.IsTrue(items[3].IsSeparator, "mục thứ tư là dấu ngăn cách, không phải một lệnh");
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

        /// <summary>
        /// (G-OPT-TIMELINE, W6) "Thu gọn / Mở làn" nay có thật và đứng ĐẦU menu header làn. Trước W6 test này khoá chiều ngược
        /// lại ("chưa có thì không hiện") — mục đã làm xong nên câu khoá đổi theo, không phải nới ra.
        /// </summary>
        [Test]
        public void LaneHeaderMenu_HasCollapseItemFirst()
        {
            IReadOnlyList<CalendarMenuItem> items = CalendarContextMenus.ForLaneHeader(FullContext());

            CollectionAssert.AreEqual(new[]
            {
                CalendarMenuItemId.ToggleLaneCollapsed, CalendarMenuItemId.HideLane, CalendarMenuItemId.MoveLaneUp,
                CalendarMenuItemId.MoveLaneDown, CalendarMenuItemId.ShowAllLanes, CalendarMenuItemId.None,
                CalendarMenuItemId.AddForLane, CalendarMenuItemId.OpenEventTypes,
            }, IdsOf(items));
        }

        /// <summary>Một mục, hai nhãn: làn đang mở đọc "Thu gọn làn", làn đã thu gọn đọc "Mở làn".</summary>
        [Test]
        public void LaneHeaderMenu_CollapseItemLabelFollowsLaneState()
        {
            CalendarMenuContext expanded = FullContext();
            CalendarMenuContext collapsed = FullContext();
            collapsed.IsLaneCollapsed = true;

            Assert.AreEqual(LiveOpsHubStrings.TimelineMenuCollapseLane,
                ItemOf(CalendarContextMenus.ForLaneHeader(expanded), CalendarMenuItemId.ToggleLaneCollapsed).Text);
            Assert.AreEqual(LiveOpsHubStrings.TimelineMenuExpandLane,
                ItemOf(CalendarContextMenus.ForLaneHeader(collapsed), CalendarMenuItemId.ToggleLaneCollapsed).Text);
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
