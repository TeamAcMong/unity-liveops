namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng ValidationDepth (G-VALIDATION-DEPTH, mục 7.5): ba mảnh chiều sâu của màn Kiểm lịch — card xem trước sửa hàng
    /// loạt ([SD2 §2.5]), popover "Bỏ qua cảnh báo…" + nhóm "Đã bỏ qua" ([SD2 §2.7]) và menu chuột phải của hàng.
    /// <para>
    /// Câu của TỪNG phát hiện vẫn không nằm ở đây (V-8): headline, meta, chữ nút chính, meta khoảng của mục đã bỏ qua và tag
    /// "đã tới hẹn" đều đến từ <see cref="LiveOpsFindingText"/>. Vùng này chỉ có chữ của KHUNG ba mảnh đó. Câu hai ngôn ngữ ở
    /// <c>Language/LiveOpsHubStringCatalog.ValidationDepth.cs</c>.
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // ----- Card xem trước sửa hàng loạt ([SD2 §2.5], Hình 17 ô 1) -----

        /// <summary>Header card: "Sẽ áp 1 thay đổi an toàn" — đếm theo số mục ĐANG BẬT, không theo tổng số lệnh sửa.</summary>
        internal static string ValidationDepthBulkPreviewTitleFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthBulkPreviewTitleFormat));

        /// <summary>Meta ngay cạnh header: lời hứa của cả nút "Sửa các lỗi an toàn" — không đổi điều người chơi thấy.</summary>
        internal static string ValidationDepthBulkPreviewMeta => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthBulkPreviewMeta));

        /// <summary>
        /// Chữ mono một hàng thay đổi: id · tên trường JSON · giá trị cũ → giá trị mới. Người đọc phải thấy ĐÚNG thứ sẽ bị ghi
        /// đè trước khi bấm Áp, nên đây là giá trị thô trong ngoặc kép, không phải câu diễn giải.
        /// </summary>
        internal static string ValidationDepthBulkPreviewChangeFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthBulkPreviewChangeFormat));

        /// <summary>Cùng hàng đó khi lệnh sửa không nói được trường nào (luật của game tự viết): bỏ vế tên trường.</summary>
        internal static string ValidationDepthBulkPreviewChangeNoFieldFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthBulkPreviewChangeNoFieldFormat));

        /// <summary>Dòng 10px nói TÊN Undo group sắp tạo — biết trước mình sẽ hoàn tác cái gì là điều kiện để dám bấm.</summary>
        internal static string ValidationDepthBulkPreviewUndoFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthBulkPreviewUndoFormat));

        internal static string ValidationDepthBulkPreviewApplyFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthBulkPreviewApplyFormat));
        internal static string ValidationDepthBulkPreviewCancelButton => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthBulkPreviewCancelButton));

        /// <summary>Lý do nút Áp bị khoá khi người dùng bỏ chọn hết — SPIKE-B SP-3: lý do LUÔN in thành chữ, tooltip chỉ phụ.</summary>
        internal static string ValidationDepthBulkPreviewNothingReason => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthBulkPreviewNothingReason));

        // ----- Popover "Bỏ qua cảnh báo…" ([SD2 §2.7], Hình 17 ô 5) -----

        internal static string ValidationDepthIgnoreHeader => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoreHeader));

        /// <summary>
        /// Nhãn Toggle phạm vi: phạm vi bỏ qua gắn với KHOẢNG (luật + đích + khoảng), nên câu phải nói thẳng "khoảng này đổi thì
        /// cảnh báo hiện lại" — nếu không, người dùng tưởng mình vừa tắt luật vĩnh viễn.
        /// </summary>
        internal static string ValidationDepthIgnoreScopeFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoreScopeFormat));

        /// <summary>Phát hiện không có khoảng (luật lặp, loại event): Toggle không có gì để hẹn, câu nói rõ là mọi khoảng.</summary>
        internal static string ValidationDepthIgnoreScopeEveryRange => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoreScopeEveryRange));

        internal static string ValidationDepthIgnoreNoteLabel => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoreNoteLabel));

        /// <summary>Footer: ghi chú lưu trong chính asset lịch để review bằng git — không phải EditorPrefs của một máy.</summary>
        internal static string ValidationDepthIgnoreFooterFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoreFooterFormat));

        internal static string ValidationDepthIgnoreCancelButton => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoreCancelButton));
        internal static string ValidationDepthIgnoreConfirmButton => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoreConfirmButton));

        /// <summary>Ghi chú là BẮT BUỘC: một cảnh báo bị ẩn mà không nói được vì sao là một cái bẫy cho người đọc lịch sau này.</summary>
        internal static string ValidationDepthIgnoreNoteRequiredReason => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoreNoteRequiredReason));

        /// <summary>Tên Undo group + câu toast sau khi bỏ qua (luật toast chung [SD2 §2.6]): nêu luật và đích.</summary>
        internal static string ValidationDepthIgnoreUndoFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoreUndoFormat));

        // ----- Nhóm "Đã bỏ qua (n) ▸" (V-15, [SD2 §2.7], Hình 17 ô 6) -----

        /// <summary>Nút nhỏ trên từng hàng: menu chuột phải không chụp được (S-24) nên gỡ bỏ qua phải có đường đi thấy được.</summary>
        internal static string ValidationDepthUnignoreButton => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthUnignoreButton));

        internal static string ValidationDepthUnignoreUndoFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthUnignoreUndoFormat));

        /// <summary>Meta một hàng đã bỏ qua: luật · đích · khoảng (khoảng do <c>LiveOpsFindingText.IgnoredWarningRangeText</c> viết).</summary>
        internal static string ValidationDepthIgnoredMetaFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoredMetaFormat));

        /// <summary>
        /// Meta một hàng đã bỏ qua CÓ tag hẹn giờ: luật · đích, KHÔNG có vế khoảng — tag đứng cạnh hàng đã nói đúng mốc đó
        /// rồi (Q-W5-4, user chốt 17/9/2026). In cả hai là đọc một hạn hai lần trong một hàng.
        /// </summary>
        internal static string ValidationDepthIgnoredMetaWithTagFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoredMetaWithTagFormat));

        /// <summary>Ghi chú bắt buộc nên chỉ rỗng với dữ liệu của bản cũ — vẫn phải có chữ, không để hàng trống trơn.</summary>
        internal static string ValidationDepthIgnoredNoteEmpty => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoredNoteEmpty));

        /// <summary>Dấu ba chấm nối vào ghi chú bị cắt của hàng — dấu câu, không dịch (dùng chung hai ngôn ngữ).</summary>
        internal static string ValidationDepthIgnoredNoteEllipsis => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoredNoteEllipsis));

        internal static string ValidationDepthViewNoteMenuItem => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthViewNoteMenuItem));

        /// <summary>
        /// Tiêu đề hover card ghim của "Xem ghi chú": thẻ chỉ có khoảng + hạn + ghi chú vì asset KHÔNG lưu người và giờ bỏ qua —
        /// bịa thêm hai dòng đó là nói dối về một thứ git mới trả lời được.
        /// </summary>
        internal static string ValidationDepthIgnoredNoteCardTitleFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthIgnoredNoteCardTitleFormat));

        // ----- Menu chuột phải của hàng (mục 7.5 "Tương tác", W5) -----

        internal static string ValidationDepthContextQuickFixMenuItem => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthContextQuickFixMenuItem));
        internal static string ValidationDepthContextCopyDescriptionMenuItem => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthContextCopyDescriptionMenuItem));
        internal static string ValidationDepthContextOpenRuleDocumentationFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthContextOpenRuleDocumentationFormat));

        /// <summary>
        /// Mục menu bị khoá ghi lý do NGAY TRONG NHÃN (SPIKE-B SP-3): menu gốc của Unity không có chỗ nào khác để in chữ, và
        /// một mục xám không lý do là chỗ người dùng bấm đi bấm lại rồi tưởng hub hỏng.
        /// </summary>
        internal static string ValidationDepthMenuDisabledReasonFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthMenuDisabledReasonFormat));

        internal static string ValidationDepthContextQuickFixNoRepairReason => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthContextQuickFixNoRepairReason));

        // ----- "Quyết định… ▾" ([SD2 §2.4]) -----

        /// <summary>Tên Undo group + câu toast sau khi chọn một mục của menu Quyết định…: nêu đích và cách đã chọn.</summary>
        internal static string ValidationDepthDecisionAppliedFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationDepthDecisionAppliedFormat));
    }
}
