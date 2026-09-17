namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class USS của vùng ShortcutHelp (G-OPT-SHORTCUTHELP, W6) — popover hướng dẫn phím tắt. Popover là cửa sổ panel riêng
    /// nên nó không dùng chung class bố cục nào của màn; mọi class ở đây có selector trong
    /// <c>Dialogs/ShortcutHelpPopover.uss</c> (check-class-names --strict).
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        /// <summary>Thân popover: rộng theo cửa sổ 320px của hub ([FD §7]), xếp dọc, padding 10.</summary>
        internal const string ShortcutHelp = "liveops-hub-shortcut-help";

        /// <summary>Tiêu đề đậm ở đầu popover.</summary>
        internal const string ShortcutHelpHeader = "liveops-hub-shortcut-help-header";

        /// <summary>
        /// Khung cuộn quanh hai nhóm phím: số hàng phụ thuộc profile phím của người dùng (và sẽ dài thêm khi hub có lệnh mới),
        /// nên danh sách phải cuộn được thay vì đẩy hàng nút ra khỏi cửa sổ.
        /// </summary>
        internal const string ShortcutHelpScroll = "liveops-hub-shortcut-help-scroll";

        /// <summary>Hộp chứa hai nhóm — hàng do C# dựng nên chỗ này rỗng trong UXML.</summary>
        internal const string ShortcutHelpGroups = "liveops-hub-shortcut-help-groups";

        /// <summary>Tiêu đề một nhóm ("Khi cửa sổ hub đang có focus").</summary>
        internal const string ShortcutHelpGroupTitle = "liveops-hub-shortcut-help-group-title";

        /// <summary>Một hàng: câu mô tả bên trái, nhãn phím bên phải.</summary>
        internal const string ShortcutHelpRow = "liveops-hub-shortcut-help-row";

        /// <summary>Câu mô tả — giãn hết chỗ còn lại và xuống dòng được, vì bản tiếng Anh dài hơn bản gốc.</summary>
        internal const string ShortcutHelpRowDescription = "liveops-hub-shortcut-help-row-description";

        /// <summary>Nhãn phím — chữ mono, không co, dính mép phải để mắt dò theo một cột thẳng.</summary>
        internal const string ShortcutHelpRowKey = "liveops-hub-shortcut-help-row-key";

        /// <summary>Lệnh bị gỡ phím trong profile của người dùng: chữ "chưa gán phím" phải nhạt hơn nhãn phím thật.</summary>
        internal const string ShortcutHelpRowKeyUnbound = "liveops-hub-shortcut-help-row-key--unbound";

        /// <summary>Dòng cuối: đổi phím ở đâu, và vì sao nhóm timeline không có trong cửa sổ Shortcuts.</summary>
        internal const string ShortcutHelpNote = "liveops-hub-shortcut-help-note";

        /// <summary>Hàng nút dính đáy popover.</summary>
        internal const string ShortcutHelpFooter = "liveops-hub-shortcut-help-footer";

        /// <summary>
        /// Ẩn dòng lý do khi nút mở cửa sổ Shortcuts chưa hỏng. Ẩn bằng CLASS chứ không <c>style.display</c>: trạng thái không
        /// được gán style inline ([FD §2.14]).
        /// </summary>
        internal const string ShortcutHelpHidden = "liveops-hub-shortcut-help-hidden";
    }
}
