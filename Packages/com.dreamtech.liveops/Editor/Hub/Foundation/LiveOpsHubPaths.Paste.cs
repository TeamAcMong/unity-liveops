namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Phần vùng Paste của <see cref="LiveOpsHubPaths"/> (V-5, G-PASTE): sheet riêng của popover dán, tên element của
    /// <c>PasteRunningJsonPopover.uxml</c> và chỗ lưu mặc định của luồng "Nhập JSON đang chạy…" (V-14 bước 3).
    /// <para>
    /// Đường dẫn UXML đã có ở file gốc (G-SKELETON khai từ W0); file này thêm đường dẫn USS vì popover là cửa sổ panel
    /// riêng — sheet của màn không tới được nó, và bốn sheet chung (<c>LiveOpsFeedbackStyleSheets.PopoverSheets</c>) không
    /// biết bố cục riêng của luồng dán (ô mono cao, dòng key remote, hàng nút dính đáy).
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubPaths
    {
        /// <summary>Sheet riêng của popover dán — nạp thêm lên cây popover, cùng cách ProposalPopover nạp sheet màn Kiểm.</summary>
        internal const string PasteRunningJsonPopoverUss = HubRoot + "/Dialogs/PasteRunningJsonPopover.uss";

        internal static class PasteElementNames
        {
            /// <summary>Thân popover — có TÊN để lệnh chụp ghi được bề rộng 320px của nó (9.5).</summary>
            internal const string Body = "paste-running-json-body";

            internal const string Header = "paste-running-json-header";
            internal const string InputLabel = "paste-running-json-input-label";
            internal const string Input = "paste-running-json-input";

            /// <summary>Khung cuộn của ô dán — có TÊN để test bố cục và lệnh chụp đo được chính 96px của thiết kế.</summary>
            internal const string InputScroll = "paste-running-json-input-scroll";
            internal const string Status = "paste-running-json-status";
            internal const string RemoteKeyLine = "paste-running-json-remote-key";
            internal const string Mode = "paste-running-json-mode";
            internal const string StampToggle = "paste-running-json-stamp";

            /// <summary>Hàng chứa ô tích + câu — ẨN CẢ HÀNG ở chế độ dán, ẩn mỗi ô tích thì câu ở lại một mình.</summary>
            internal const string StampLine = "paste-running-json-stamp-line";

            internal const string StampLabel = "paste-running-json-stamp-label";
            internal const string Footer = "paste-running-json-footer";
            internal const string Cancel = "paste-running-json-cancel";

            /// <summary>Nút chính: dựng bằng C# vì nó phải nằm trong <c>LiveOpsButtonSlot</c> để in lý do khoá thành chữ (SP-3).</summary>
            internal const string Confirm = "paste-running-json-confirm";
        }

        /// <summary>
        /// Thư mục mặc định của hộp "Chọn nơi lưu…" (V-14 bước 3). Là hằng có tên chứ không chuỗi trần trong luồng: hai chỗ
        /// (hộp lưu và câu gợi ý) phải nói cùng một nơi.
        /// </summary>
        internal const string ImportCalendarAssetDirectory = "Assets/LiveOps/Calendars";

        /// <summary>Tên file mặc định (không đuôi) — "Main.asset" của V-14.</summary>
        internal const string ImportCalendarAssetDefaultName = "Main";

        /// <summary>Đuôi file asset của Unity — <see cref="ILiveOpsHubFileDialog.SaveFile"/> nhận đuôi không có dấu chấm.</summary>
        internal const string ImportCalendarAssetExtension = "asset";
    }
}
