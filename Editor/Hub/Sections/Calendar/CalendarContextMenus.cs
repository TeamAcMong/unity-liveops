using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Nội dung năm menu chuột phải của màn Lịch [FD §3.9] dưới dạng DỮ LIỆU, không phải lời gọi dựng menu: menu gốc của Unity
    /// không chụp ảnh được (S-24), nên thứ duy nhất chứng minh được menu đúng là một danh sách mục có test Logic đọc thẳng.
    /// <para>
    /// Mục không dùng được KHÔNG bị giấu: nó ở lại, <see cref="CalendarMenuItem.IsEnabled"/> = false và nhãn TỰ NÓI lý do
    /// (SPIKE-B SP-3 — lý do luôn in thành chữ). Giấu mục làm người dùng tưởng bản này không có tính năng đó.
    /// </para>
    /// </summary>
    internal static class CalendarContextMenus
    {
        private const string DuplicateShortcutId = "Main Menu/Edit/Duplicate";
        private const string CopyShortcutId = "Main Menu/Edit/Copy";
        private const string PasteShortcutId = "Main Menu/Edit/Paste";
        private const string DeleteShortcutId = "Main Menu/Edit/Delete";
        private const string FrameShortcutId = "Main Menu/Edit/Frame Selected";

        /// <summary>Menu của một thanh đợt CỐ ĐỊNH [FD §3.9] — chín mục, thứ tự đúng mockup.</summary>
        public static IReadOnlyList<CalendarMenuItem> ForFixedBar(CalendarMenuContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            List<CalendarMenuItem> items = new List<CalendarMenuItem>
            {
                Enabled(CalendarMenuItemId.EditInInspector, LiveOpsHubStrings.CalendarDepthMenuEditInInspector),
                DuplicateItem(context),
                WithShortcut(CalendarMenuItemId.Frame, LiveOpsHubStrings.CalendarDepthMenuFrame, FrameShortcutId),
                Enabled(CalendarMenuItemId.MoveStart, LiveOpsHubStrings.CalendarDepthMenuMoveStart),
                Enabled(CalendarMenuItemId.SetDuration, LiveOpsHubStrings.CalendarDepthMenuSetDuration),
                RevertItem(context),
                Enabled(CalendarMenuItemId.CopyId, LiveOpsHubStrings.CalendarDepthMenuCopyId),
                WithShortcut(CalendarMenuItemId.CopyEventJson, LiveOpsHubStrings.CalendarDepthMenuCopyEventJson, CopyShortcutId),
                WithShortcut(CalendarMenuItemId.Delete, LiveOpsHubStrings.CalendarDepthMenuDelete, DeleteShortcutId),
            };
            return items;
        }

        /// <summary>
        /// Menu của một thanh SINH TỪ LUẬT [FD §3.9]: ba mục dùng được + một mục disabled nói vì sao không sửa được. KHÔNG có
        /// "Xem trong Mô phỏng" (PD-1: Mô phỏng là P3, mục trỏ tới thứ chưa có là lời hứa suông).
        /// </summary>
        public static IReadOnlyList<CalendarMenuItem> ForRecurringBar(CalendarMenuContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return new List<CalendarMenuItem>
            {
                Enabled(CalendarMenuItemId.OpenRule, string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.CalendarDepthMenuOpenRuleFormat, context.LaneTypeId)),
                WithShortcut(CalendarMenuItemId.Frame, LiveOpsHubStrings.CalendarDepthMenuFrame, FrameShortcutId),
                Enabled(CalendarMenuItemId.CopyId, LiveOpsHubStrings.CalendarDepthMenuCopyId),
                Disabled(CalendarMenuItemId.RecurringReadOnly, LiveOpsHubStrings.CalendarDepthMenuRecurringReadOnly, string.Empty),
            };
        }

        /// <summary>
        /// Menu của header làn [FD §3.9]. "Thu gọn / Mở làn" chưa có ở P1 (G-OPT-TIMELINE) nên KHÔNG xuất hiện — khác với
        /// "Đưa lên / Đưa xuống" ở mép, vốn có thật nhưng không dùng được lúc này và vì thế in kèm lý do.
        /// </summary>
        public static IReadOnlyList<CalendarMenuItem> ForLaneHeader(CalendarMenuContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return new List<CalendarMenuItem>
            {
                Enabled(CalendarMenuItemId.HideLane, LiveOpsHubStrings.CalendarDepthMenuHideLane),
                MoveLaneItem(CalendarMenuItemId.MoveLaneUp, LiveOpsHubStrings.CalendarDepthMenuMoveLaneUp,
                    context.CanMoveLaneUp, LiveOpsHubStrings.CalendarDepthMenuLaneAtTopReason),
                MoveLaneItem(CalendarMenuItemId.MoveLaneDown, LiveOpsHubStrings.CalendarDepthMenuMoveLaneDown,
                    context.CanMoveLaneDown, LiveOpsHubStrings.CalendarDepthMenuLaneAtBottomReason),
                Enabled(CalendarMenuItemId.ShowAllLanes, LiveOpsHubStrings.CalendarDepthMenuShowAllLanes),
                Separator(),
                Enabled(CalendarMenuItemId.AddForLane, string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.CalendarDepthMenuAddForLaneFormat, context.LaneTypeId)),
                Enabled(CalendarMenuItemId.OpenEventTypes, LiveOpsHubStrings.CalendarDepthMenuOpenEventTypes),
            };
        }

        /// <summary>Menu của chỗ trống trên làn CỐ ĐỊNH [FD §3.9]: giờ trong nhãn là giờ tại con trỏ, đã bắt lưới.</summary>
        public static IReadOnlyList<CalendarMenuItem> ForEmptyLane(CalendarMenuContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return new List<CalendarMenuItem>
            {
                Enabled(CalendarMenuItemId.AddAtCursor, string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.CalendarDepthMenuAddAtFormat, context.CursorTimeText)),
                PasteItem(context),
            };
        }

        /// <summary>Menu của một hàng trong pane "So với đã đăng" [SD1 §3.4]; nguồn Disk đổi động từ theo vá V-13.</summary>
        public static IReadOnlyList<CalendarMenuItem> ForCompareRow(CalendarMenuContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return new List<CalendarMenuItem> { RevertItem(context) };
        }

        /// <summary>Đổ danh sách mục vào menu gốc của Unity; mục ngăn cách và mục disabled giữ nguyên chỗ.</summary>
        public static void Populate(DropdownMenu menu, IReadOnlyList<CalendarMenuItem> items, Action<CalendarMenuItemId> activate)
        {
            if (menu == null) throw new ArgumentNullException(nameof(menu));
            if (items == null) throw new ArgumentNullException(nameof(items));
            for (int index = 0; index < items.Count; index++)
            {
                CalendarMenuItem item = items[index];
                if (item.IsSeparator)
                {
                    menu.AppendSeparator();
                    continue;
                }
                CalendarMenuItemId id = item.Id;
                menu.AppendAction(item.Text, _ => activate?.Invoke(id),
                    item.IsEnabled ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
        }

        private static CalendarMenuItem DuplicateItem(CalendarMenuContext context)
        {
            if (!context.CanDuplicate) return Disabled(CalendarMenuItemId.Duplicate, LiveOpsHubStrings.CalendarDepthMenuDuplicateFormat, string.Empty);
            string text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDepthMenuDuplicateFormat,
                context.DuplicateTargetText, context.DuplicateOffsetText);
            return WithShortcut(CalendarMenuItemId.Duplicate, text, DuplicateShortcutId);
        }

        private static CalendarMenuItem RevertItem(CalendarMenuContext context)
        {
            string label = context.CompareSource == LiveOpsHubCompareSource.Disk
                ? LiveOpsHubStrings.CalendarDepthTakeFromDisk
                : LiveOpsHubStrings.CalendarDepthRevertToPublished;
            return context.CanRevertToCompare
                ? Enabled(CalendarMenuItemId.RevertToCompare, label)
                : Disabled(CalendarMenuItemId.RevertToCompare, label, LiveOpsHubStrings.CalendarDepthCompareUnavailableReason);
        }

        private static CalendarMenuItem PasteItem(CalendarMenuContext context)
        {
            return context.HasCopiedEvent
                ? WithShortcut(CalendarMenuItemId.PasteAtCursor, LiveOpsHubStrings.CalendarDepthMenuPasteAtCursor, PasteShortcutId)
                : Disabled(CalendarMenuItemId.PasteAtCursor, LiveOpsHubStrings.CalendarDepthMenuPasteAtCursor,
                    LiveOpsHubStrings.CalendarDepthMenuPasteDisabledReason);
        }

        private static CalendarMenuItem MoveLaneItem(CalendarMenuItemId id, string label, bool canMove, string reason)
        {
            return canMove ? Enabled(id, label) : Disabled(id, label, reason);
        }

        private static CalendarMenuItem Enabled(CalendarMenuItemId id, string text)
        {
            return new CalendarMenuItem(id, text, true, false);
        }

        /// <summary>Mục khoá: nhãn GỘP lý do vào chính chữ, vì menu gốc của Unity không có chỗ nào khác để in lý do.</summary>
        private static CalendarMenuItem Disabled(CalendarMenuItemId id, string text, string reason)
        {
            string label = reason.Length == 0
                ? text
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDepthMenuDisabledReasonFormat, text, reason);
            return new CalendarMenuItem(id, label, false, false);
        }

        /// <summary>Nhãn kèm phím tắt đọc từ binding THẬT; không có binding thì bỏ hẳn vế phím thay vì in một phím sai.</summary>
        private static CalendarMenuItem WithShortcut(CalendarMenuItemId id, string text, string shortcutId)
        {
            string key = LiveOpsHubKeyLabels.For(shortcutId);
            string label = string.IsNullOrEmpty(key)
                ? text
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDepthMenuShortcutFormat, text, key);
            return new CalendarMenuItem(id, label, true, false);
        }

        private static CalendarMenuItem Separator()
        {
            return new CalendarMenuItem(CalendarMenuItemId.None, string.Empty, false, true);
        }
    }

    /// <summary>Định danh mục menu — test và nơi gọi khớp nhau bằng enum, không bằng chuỗi nhãn (nhãn đổi theo ngôn ngữ).</summary>
    internal enum CalendarMenuItemId
    {
        None = 0,
        EditInInspector = 1,
        Duplicate = 2,
        Frame = 3,
        MoveStart = 4,
        SetDuration = 5,
        RevertToCompare = 6,
        CopyId = 7,
        CopyEventJson = 8,
        Delete = 9,
        OpenRule = 10,
        RecurringReadOnly = 11,
        HideLane = 12,
        MoveLaneUp = 13,
        MoveLaneDown = 14,
        ShowAllLanes = 15,
        AddForLane = 16,
        OpenEventTypes = 17,
        AddAtCursor = 18,
        PasteAtCursor = 19,
    }

    /// <summary>Một mục menu đã tính xong nhãn và trạng thái — view chỉ đổ vào <c>DropdownMenu</c>.</summary>
    internal sealed class CalendarMenuItem
    {
        public CalendarMenuItem(CalendarMenuItemId id, string text, bool isEnabled, bool isSeparator)
        {
            Id = id;
            Text = text ?? string.Empty;
            IsEnabled = isEnabled;
            IsSeparator = isSeparator;
        }

        public CalendarMenuItemId Id { get; }
        public string Text { get; }
        public bool IsEnabled { get; }
        public bool IsSeparator { get; }

        public override string ToString() => (IsSeparator ? "—" : Text) + (IsEnabled ? string.Empty : " [disabled]");
    }

    /// <summary>Dữ liệu ngữ cảnh mà menu cần; presenter tính sẵn để lớp menu thuần và test Logic dựng được không cần phiên.</summary>
    internal sealed class CalendarMenuContext
    {
        public string BarKey { get; set; } = string.Empty;
        public string LaneTypeId { get; set; } = string.Empty;

        /// <summary>Giờ tại con trỏ đã bắt lưới, đã định dạng ngắn ("18/9 00:00").</summary>
        public string CursorTimeText { get; set; } = string.Empty;

        public bool CanDuplicate { get; set; }

        /// <summary>Đích nhân bản ("24/9 00:00") và khoảng nhảy ("7 ngày") — menu nêu đích cụ thể [FD §3.9].</summary>
        public string DuplicateTargetText { get; set; } = string.Empty;

        public string DuplicateOffsetText { get; set; } = string.Empty;
        public bool CanRevertToCompare { get; set; }
        public bool HasCopiedEvent { get; set; }
        public bool CanMoveLaneUp { get; set; }
        public bool CanMoveLaneDown { get; set; }
        public LiveOpsHubCompareSource CompareSource { get; set; } = LiveOpsHubCompareSource.Published;
    }
}
