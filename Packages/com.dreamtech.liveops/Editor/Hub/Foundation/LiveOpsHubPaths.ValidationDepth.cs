namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Phần vùng ValidationDepth của <see cref="LiveOpsHubPaths"/> (V-5, G-VALIDATION-DEPTH): tên element của ba mảnh chiều sâu
    /// màn Kiểm lịch — card xem trước sửa hàng loạt ([SD2 §2.5], Hình 17 ô 1), popover Bỏ qua cảnh báo ([SD2 §2.7], ô 5) và
    /// hàng của nhóm "Đã bỏ qua" (ô 6). Đường dẫn UXML của popover đã có ở file gốc (G-SKELETON) nên file này chỉ khai TÊN.
    /// <para>
    /// Card xem trước và popover là hai thứ CHỈ tồn tại sau một cú bấm, nên chúng KHÔNG nằm trong
    /// <c>RequiredValidationElementNames</c>: probe và <c>HubWindowTests</c> duyệt màn ở trạng thái vừa mở, đòi chúng có mặt
    /// ngay là đòi màn tự mở sẵn một hộp mà người dùng chưa gọi. Chỗ CẮM card thì luôn có (<see cref="ValidationDepthElementNames.BulkPreviewHost"/>).
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubPaths
    {
        internal static class ValidationDepthElementNames
        {
            /// <summary>Chỗ cắm card xem trước — luôn có trong cây, rỗng khi chưa bấm "Sửa các lỗi an toàn (n)…".</summary>
            internal const string BulkPreviewHost = "validation-bulk-preview-host";

            /// <summary>Card xem trước sửa hàng loạt; có TÊN để lệnh chụp ghi được số đo của nó (9.5).</summary>
            internal const string BulkPreviewCard = "validation-bulk-preview";

            internal const string BulkPreviewTitle = "validation-bulk-preview-title";
            internal const string BulkPreviewUndoLine = "validation-bulk-preview-undo";
            internal const string BulkPreviewApply = "validation-bulk-preview-apply";
            internal const string BulkPreviewCancel = "validation-bulk-preview-cancel";

            /// <summary>Một hàng thay đổi trong card: Toggle + chữ mono "id field \"trước\" → \"sau\"".</summary>
            internal const string BulkPreviewRowPrefix = "validation-bulk-preview-row-";

            // Popover Bỏ qua cảnh báo… ([SD2 §2.7]) — tên viết tay trong IgnoreWarningPopover.uxml.
            internal const string IgnoreBody = "ignore-warning-body";
            internal const string IgnoreHeader = "ignore-warning-header";
            internal const string IgnoreScopeLine = "ignore-warning-scope-line";
            internal const string IgnoreScope = "ignore-warning-scope";
            internal const string IgnoreScopeLabel = "ignore-warning-scope-label";
            internal const string IgnoreNoteLabel = "ignore-warning-note-label";
            internal const string IgnoreNote = "ignore-warning-note";
            internal const string IgnoreFooterHint = "ignore-warning-hint";
            internal const string IgnoreConfirm = "ignore-warning-confirm";
            internal const string IgnoreCancel = "ignore-warning-cancel";

            /// <summary>Hover card ghim của "Xem ghi chú" (ô 6) — ghi chú đủ, không rút gọn.</summary>
            internal const string IgnoredNoteCard = "validation-ignored-note-card";
        }
    }
}
