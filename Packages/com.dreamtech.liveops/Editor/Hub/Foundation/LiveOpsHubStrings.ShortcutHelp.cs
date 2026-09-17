namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng ShortcutHelp (G-OPT-SHORTCUTHELP, W6 — P1-lùi): popover "Hiện hướng dẫn phím tắt" của menu ⋮ ([FD §3.3],
    /// ma trận 9.5 ảnh <c>ho-shortcut-help</c>).
    /// <para>
    /// Vì sao một vùng riêng chứ không nhét vào Shell: popover kể HAI họ phím đến từ hai cơ chế khác nhau — phím tắt đăng ký
    /// qua <c>ShortcutManager</c> (bảng 8.7) và phím một ký tự do chính element timeline xử lý (SP-7b). Câu của nó phải sống
    /// cạnh nhau để không ai sửa một nửa; chủ là popover, không phải khung.
    /// </para>
    /// <para>
    /// Tên lệnh của họ thứ nhất KHÔNG khai lại ở đây: chúng đã có ở <c>LiveOpsHubStrings.ShellShortcut…</c> (PD-15) và
    /// popover đọc thẳng từ đó — khai lại là hai câu cho cùng một lệnh, sửa một chỗ thì chỗ kia trôi.
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // ----- Khung popover -----

        /// <summary>Popover không dựng được vì thiếu UXML — nêu đúng đường dẫn để sửa được, cùng khuôn với câu của popover dán.</summary>
        internal static string ShortcutHelpMissingLayoutFormat => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpMissingLayoutFormat));

        /// <summary>Mục menu ⋮ mở popover ([FD §3.3] — chữ của thiết kế, đứng sau "Tắt chuyển động").</summary>
        internal static string ShortcutHelpMenuItem => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpMenuItem));

        /// <summary>Tiêu đề popover — nói rõ đây là phím của HUB, không phải bảng phím của cả Unity.</summary>
        internal static string ShortcutHelpPopoverTitle => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpPopoverTitle));

        // ----- Hai nhóm phím -----

        /// <summary>
        /// Tiêu đề nhóm phím <c>ShortcutManager</c>. Nói "khi cửa sổ hub đang có focus" chứ không nói "toàn hub": mọi
        /// <c>[Shortcut]</c> của hub khai <c>context = typeof(LiveOpsHubWindow)</c> nên chúng im lặng khi người dùng đang ở
        /// cửa sổ khác — người đọc cần biết điều kiện đó, không thì họ bấm ⌘S ở Scene view và tưởng hub hỏng.
        /// </summary>
        internal static string ShortcutHelpWindowGroupTitle => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpWindowGroupTitle));

        /// <summary>
        /// Tiêu đề nhóm phím một ký tự của timeline (SP-7b). Điều kiện "timeline đang có focus" là điều kiện THẬT của
        /// <c>LiveOpsTimelineElement.OnKeyDown</c>, không phải cách nói cho gọn.
        /// </summary>
        internal static string ShortcutHelpTimelineGroupTitle => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpTimelineGroupTitle));

        /// <summary>
        /// Thay cho nhãn phím khi người dùng đã gỡ phím của một lệnh trong profile của họ. In chữ này chứ không để ô trống:
        /// ô trống đọc như "hàng này chưa vẽ xong", còn đây là một trạng thái có thật và sửa được ở cửa sổ Shortcuts.
        /// </summary>
        internal static string ShortcutHelpUnboundKey => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpUnboundKey));

        /// <summary>
        /// Dòng cuối popover: nói đổi phím ở đâu, và vì sao nhóm timeline không có mặt trong cửa sổ Shortcuts — không nói thì
        /// người dùng đi tìm "Go To Today" trong bảng phím của Unity và không bao giờ thấy.
        /// </summary>
        internal static string ShortcutHelpNote => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpNote));

        // ----- Hàng nút -----

        /// <summary>Nút chính: mở cửa sổ Shortcuts của Unity — chỗ DUY NHẤT đổi được phím của nhóm thứ nhất.</summary>
        internal static string ShortcutHelpOpenShortcutManagerButton => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpOpenShortcutManagerButton));

        internal static string ShortcutHelpCloseButton => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpCloseButton));

        /// <summary>
        /// Lý do IN THÀNH CHỮ khi không mở được cửa sổ Shortcuts (SP-3). Đường mở đi qua kiểu nội bộ của UnityEditor nên một
        /// bản Unity đổi tên kiểu là nút không làm gì được — im lặng lúc đó là người dùng bấm hoài mà không hiểu vì sao.
        /// </summary>
        internal static string ShortcutHelpOpenShortcutManagerFailedReason => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpOpenShortcutManagerFailedReason));

        // ----- Nhãn phím của nhóm timeline -----
        // Chín hàng dưới đây là phím CỐ ĐỊNH trong code của timeline (SP-7b: không qua ShortcutManager), nên nhãn phím là hằng
        // chữ chứ không đọc được từ profile của người dùng. Chúng vào catalog dạng AddShared: ký hiệu phím không dịch.

        /// <summary>Nhãn phím "← →".</summary>
        internal static string ShortcutHelpKeyArrowsLeftRight => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpKeyArrowsLeftRight));

        /// <summary>Nhãn phím "⇧ ← →".</summary>
        internal static string ShortcutHelpKeyShiftArrowsLeftRight => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpKeyShiftArrowsLeftRight));

        /// <summary>Nhãn phím "⌥ ← →".</summary>
        internal static string ShortcutHelpKeyOptionArrowsLeftRight => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpKeyOptionArrowsLeftRight));

        /// <summary>Nhãn phím "⌥⇧ ← →".</summary>
        internal static string ShortcutHelpKeyOptionShiftArrowsLeftRight => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpKeyOptionShiftArrowsLeftRight));

        /// <summary>
        /// Nhãn phím "{0}↑ {0}↓" — {0} là phím lệnh theo nền tảng (⌘ trên macOS, Ctrl chỗ khác), lấy từ
        /// <c>TimelineActionKeyMac</c> / <c>TimelineActionKeyOther</c> mà dòng gợi ý timeline đã dùng.
        /// </summary>
        internal static string ShortcutHelpKeyActionArrowsUpDownFormat => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpKeyActionArrowsUpDownFormat));

        /// <summary>Nhãn phím "A".</summary>
        internal static string ShortcutHelpKeyFrameAll => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpKeyFrameAll));

        /// <summary>Nhãn phím "+ −".</summary>
        internal static string ShortcutHelpKeyZoom => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpKeyZoom));

        /// <summary>Nhãn phím "Esc".</summary>
        internal static string ShortcutHelpKeyEscape => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpKeyEscape));

        /// <summary>Nhãn phím menu ngữ cảnh của bàn phím (phím "Menu" trên bàn phím Windows).</summary>
        internal static string ShortcutHelpKeyContextMenu => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpKeyContextMenu));

        // ----- Câu mô tả của nhóm timeline -----

        /// <summary>Mũi tên trần: nhích đợt đang chọn đúng một bước lưới đang hiệu lực (cùng bước với cử chỉ kéo).</summary>
        internal static string ShortcutHelpTimelineNudgeByStep => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpTimelineNudgeByStep));

        /// <summary>Shift + mũi tên: bước một ngày, không phụ thuộc lưới.</summary>
        internal static string ShortcutHelpTimelineNudgeByDay => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpTimelineNudgeByDay));

        /// <summary>Alt + mũi tên: kéo riêng cạnh KẾT THÚC — đây cũng là thao tác duy nhất còn làm được trên đợt đang chạy.</summary>
        internal static string ShortcutHelpTimelineMoveEndEdge => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpTimelineMoveEndEdge));

        /// <summary>Alt + Shift + mũi tên: kéo riêng cạnh BẮT ĐẦU.</summary>
        internal static string ShortcutHelpTimelineMoveStartEdge => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpTimelineMoveStartEdge));

        /// <summary>Phím lệnh + mũi tên dọc: nhảy chọn sang đợt ở làn trên/dưới mà không đụng chuột.</summary>
        internal static string ShortcutHelpTimelineSelectAdjacentLane => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpTimelineSelectAdjacentLane));

        /// <summary>A: thu cả lịch vào khung nhìn.</summary>
        internal static string ShortcutHelpTimelineFrameAll => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpTimelineFrameAll));

        /// <summary>+ / −: đổi bậc zoom.</summary>
        internal static string ShortcutHelpTimelineZoom => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpTimelineZoom));

        /// <summary>Esc: huỷ cử chỉ kéo đang làm, trả thanh về chỗ cũ.</summary>
        internal static string ShortcutHelpTimelineCancelDrag => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpTimelineCancelDrag));

        /// <summary>Phím Menu: mở đúng menu chuột phải của đợt đang chọn.</summary>
        internal static string ShortcutHelpTimelineContextMenu => LiveOpsHubStringCatalog.Text(nameof(ShortcutHelpTimelineContextMenu));
    }
}
