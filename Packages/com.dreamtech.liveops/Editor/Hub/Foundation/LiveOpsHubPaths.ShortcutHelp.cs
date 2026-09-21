namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Phần vùng ShortcutHelp của <see cref="LiveOpsHubPaths"/> (V-5, G-OPT-SHORTCUTHELP): sheet riêng của popover hướng dẫn
    /// phím tắt và tên element của <c>ShortcutHelpPopover.uxml</c>.
    /// <para>
    /// Đường dẫn UXML đã có ở file gốc (G-SKELETON khai từ W0); file này thêm đường dẫn USS vì popover là cửa sổ panel riêng —
    /// bốn sheet chung (<c>LiveOpsFeedbackStyleSheets.PopoverSheets</c>) không biết bố cục hai cột của bảng phím.
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubPaths
    {
        /// <summary>Sheet riêng của popover phím tắt — nạp thêm lên cây popover, cùng cách popover dán nạp sheet của nó.</summary>
        internal const string ShortcutHelpPopoverUss = HubRoot + "/Dialogs/ShortcutHelpPopover.uss";

        internal static class ShortcutHelpElementNames
        {
            /// <summary>Thân popover — có TÊN để lệnh chụp ghi được bề rộng 320px của nó (9.5).</summary>
            internal const string Body = "shortcut-help-body";

            internal const string Header = "shortcut-help-header";

            /// <summary>Khung cuộn của danh sách phím — có TÊN để test bố cục chứng minh hàng nút không bị đẩy ra ngoài.</summary>
            internal const string Scroll = "shortcut-help-scroll";

            /// <summary>Hộp chứa hai nhóm; hàng do C# dựng vào đây.</summary>
            internal const string Groups = "shortcut-help-groups";

            internal const string Note = "shortcut-help-note";
            internal const string Footer = "shortcut-help-footer";
            internal const string Close = "shortcut-help-close";
            internal const string OpenShortcutManager = "shortcut-help-open-shortcut-manager";

            /// <summary>Dòng lý do khi không mở được cửa sổ Shortcuts — ẩn cho tới lúc thật sự hỏng.</summary>
            internal const string OpenFailedReason = "shortcut-help-open-failed-reason";
        }
    }
}
