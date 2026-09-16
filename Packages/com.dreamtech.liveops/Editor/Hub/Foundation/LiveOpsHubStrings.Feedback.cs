namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ của vùng Feedback (G-FEEDBACK): toast Hoàn tác, outcome, palette ⌘K, hộp xác nhận cấp 1/2, popover và thông điệp lỗi
    /// lập trình của các view này. Mọi hằng mang tiền tố <c>Feedback</c> vì lớp <c>partial</c> dùng chung với mọi vùng — tiền tố
    /// giữ cho gói song song không khai trùng tên. Microcopy nguyên văn theo [FD §3.8–§3.10].
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Toast (8.5, [FD §3.9]): nhãn nút ghép với nhãn phím đọc từ profile thật — không có phím thì không in cặp ngoặc rỗng.
        internal static string FeedbackToastUndoLabel => LiveOpsHubStringCatalog.Text(nameof(FeedbackToastUndoLabel));
        internal static string FeedbackToastRedoLabel => LiveOpsHubStringCatalog.Text(nameof(FeedbackToastRedoLabel));
        // Lý do ngắn in cạnh nút Hoàn tác bị khoá; tooltip nói thêm chỗ xem lại vì Undo là stack chung của cả Editor.
        internal static string FeedbackToastUndoUnavailableReason => LiveOpsHubStringCatalog.Text(nameof(FeedbackToastUndoUnavailableReason));
        internal static string FeedbackToastUndoUnavailableTooltip => LiveOpsHubStringCatalog.Text(nameof(FeedbackToastUndoUnavailableTooltip));
        internal static string FeedbackToastCloseTooltip => LiveOpsHubStringCatalog.Text(nameof(FeedbackToastCloseTooltip));

        // Outcome (8.5): kết quả xuất/đăng ở lại tới khi làm việc khác — câu chân nói rõ để người dùng không chờ nó tự tắt.
        internal static string FeedbackOutcomeFootnote => LiveOpsHubStringCatalog.Text(nameof(FeedbackOutcomeFootnote));

        // Palette ⌘K (8.7, [FD §3.8]): chỉ điều hướng, không bao giờ có lệnh.
        internal static string FeedbackPalettePlaceholder => LiveOpsHubStringCatalog.Text(nameof(FeedbackPalettePlaceholder));
        internal static string FeedbackPaletteFooterFormat => LiveOpsHubStringCatalog.Text(nameof(FeedbackPaletteFooterFormat));
        // Shortcut mở palette do G-HOSTUI đăng ký (W4): trước đó profile không có phím, chân vẫn phải nói được cách mở Unity Search.
        internal static string FeedbackPaletteFooterWithoutKey => LiveOpsHubStringCatalog.Text(nameof(FeedbackPaletteFooterWithoutKey));
        internal static string FeedbackPaletteNoMatchFormat => LiveOpsHubStringCatalog.Text(nameof(FeedbackPaletteNoMatchFormat));
        internal static string FeedbackPaletteRuleIdCaption => LiveOpsHubStringCatalog.Text(nameof(FeedbackPaletteRuleIdCaption));
        internal static string FeedbackPaletteRuleSeparator => LiveOpsHubStringCatalog.Text(nameof(FeedbackPaletteRuleSeparator));
        internal static string FeedbackPaletteBadgeSeparator => LiveOpsHubStringCatalog.Text(nameof(FeedbackPaletteBadgeSeparator));
        internal static string FeedbackPaletteReasonSeparator => LiveOpsHubStringCatalog.Text(nameof(FeedbackPaletteReasonSeparator));

        // Hộp xác nhận (8.6, [FD §3.10]).
        internal static string FeedbackConfirmTypePrefix => LiveOpsHubStringCatalog.Text(nameof(FeedbackConfirmTypePrefix));
        internal static string FeedbackConfirmTypeSuffix => LiveOpsHubStringCatalog.Text(nameof(FeedbackConfirmTypeSuffix));
        internal static string FeedbackConfirmTypeHintFormat => LiveOpsHubStringCatalog.Text(nameof(FeedbackConfirmTypeHintFormat));
        internal static string FeedbackConfirmLockedTooltip => LiveOpsHubStringCatalog.Text(nameof(FeedbackConfirmLockedTooltip));
        // Thiếu UXML thì hộp chỉ còn nút an toàn: không dựng được câu hậu quả thì không được cho bấm phá huỷ.
        internal static string FeedbackConfirmLayoutMissingFormat => LiveOpsHubStringCatalog.Text(nameof(FeedbackConfirmLayoutMissingFormat));

        // Popover (8.7, [FD §7]).
        internal static string FeedbackPopoverCloseTooltip => LiveOpsHubStringCatalog.Text(nameof(FeedbackPopoverCloseTooltip));

        // Cảnh báo/lỗi lập trình — không bao giờ do dữ liệu lịch hỏng.
        internal static string FeedbackWarningStyleSheetMissingFormat => LiveOpsHubStringCatalog.Text(nameof(FeedbackWarningStyleSheetMissingFormat));
        internal static string FeedbackErrorPopoverSizeInvalid => LiveOpsHubStringCatalog.Text(nameof(FeedbackErrorPopoverSizeInvalid));
        internal static string FeedbackErrorPopoverWithoutContent => LiveOpsHubStringCatalog.Text(nameof(FeedbackErrorPopoverWithoutContent));
        internal static string FeedbackErrorConfirmRequestMissing => LiveOpsHubStringCatalog.Text(nameof(FeedbackErrorConfirmRequestMissing));
        internal static string FeedbackErrorPolicyNotAllowed => LiveOpsHubStringCatalog.Text(nameof(FeedbackErrorPolicyNotAllowed));
        internal static string FeedbackErrorPolicyDedicatedDialog => LiveOpsHubStringCatalog.Text(nameof(FeedbackErrorPolicyDedicatedDialog));
        internal static string FeedbackErrorPolicyLevelMismatchFormat => LiveOpsHubStringCatalog.Text(nameof(FeedbackErrorPolicyLevelMismatchFormat));
        internal static string FeedbackErrorPolicyTypeTextMismatchFormat => LiveOpsHubStringCatalog.Text(nameof(FeedbackErrorPolicyTypeTextMismatchFormat));
        internal static string FeedbackErrorHoverCardContentMissing => LiveOpsHubStringCatalog.Text(nameof(FeedbackErrorHoverCardContentMissing));
    }
}
