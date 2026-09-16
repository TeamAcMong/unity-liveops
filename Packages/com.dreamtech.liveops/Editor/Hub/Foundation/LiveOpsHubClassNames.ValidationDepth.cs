namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class USS riêng của vùng ValidationDepth (V-5, G-VALIDATION-DEPTH): card xem trước sửa hàng loạt ([SD2 §2.5]), popover
    /// Bỏ qua cảnh báo ([SD2 §2.7]) và hàng của nhóm "Đã bỏ qua". Chỉ khai những thứ KHÔNG có ở bảng class khung (8.10) và
    /// không có ở vùng Validation — card dùng lại <see cref="LiveOpsHubClassNames.Card"/>, hàng dùng lại
    /// <see cref="LiveOpsHubClassNames.FindingRow"/>, tag dùng lại <see cref="LiveOpsHubClassNames.Tag"/>.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        // Card xem trước sửa hàng loạt — chen NGAY DƯỚI dải summary, không phải popover, không phải hộp modal ([SD2 §2.5]).
        internal const string ValidationBulkPreview = "liveops-hub-validation-bulk-preview";
        internal const string ValidationBulkPreviewMeta = "liveops-hub-validation-bulk-preview-meta";

        /// <summary>Một hàng thay đổi: min-height 22, Toggle + chữ mono; khe 6px giữa hai hàng ([SD2 §2.5]).</summary>
        internal const string ValidationBulkPreviewRow = "liveops-hub-validation-bulk-preview-row";

        internal const string ValidationBulkPreviewChange = "liveops-hub-validation-bulk-preview-change";

        /// <summary>Dòng 10px opacity .7 thụt 20px nói TÊN Undo group sẽ tạo — người đọc biết trước mình hoàn tác cái gì.</summary>
        internal const string ValidationBulkPreviewUndo = "liveops-hub-validation-bulk-preview-undo";

        internal const string ValidationBulkPreviewFooter = "liveops-hub-validation-bulk-preview-footer";

        // Popover Bỏ qua cảnh báo… — 300px theo mockup, hẹp hơn popover Đề xuất… 320px vì chỉ có một Toggle và một ô ghi chú.
        internal const string ValidationIgnorePopover = "liveops-hub-validation-ignore-popover";
        internal const string ValidationIgnoreScope = "liveops-hub-validation-ignore-scope";
        internal const string ValidationIgnoreNoteLabel = "liveops-hub-validation-ignore-note-label";
        internal const string ValidationIgnoreFooter = "liveops-hub-validation-ignore-footer";
        internal const string ValidationIgnoreHint = "liveops-hub-validation-ignore-hint";

        /// <summary>
        /// Hàng ngang headline + tag của một hàng phát hiện — chỉ dựng khi CÓ tag ("đã tới hẹn"), để hàng không tag giữ nguyên
        /// cây của W4 và ảnh đã chụp không đổi.
        /// </summary>
        internal const string ValidationRowHeadlineLine = "liveops-hub-validation-row-headline-line";

        // Nhóm "Đã bỏ qua (n) ▸" mở ra (V-15, [SD2 §2.7] ô 6): mỗi mục một hàng có nút nhỏ "Bỏ bỏ qua".
        internal const string ValidationIgnoredRow = "liveops-hub-validation-ignored-row";
        internal const string ValidationIgnoredNote = "liveops-hub-validation-ignored-note";
        internal const string ValidationIgnoredNoteCard = "liveops-hub-validation-ignored-note-card";
    }
}
