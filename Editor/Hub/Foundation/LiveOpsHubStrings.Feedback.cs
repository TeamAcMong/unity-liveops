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
        internal const string FeedbackToastUndoLabel = "Hoàn tác";
        internal const string FeedbackToastRedoLabel = "Làm lại";
        // Lý do ngắn in cạnh nút Hoàn tác bị khoá; tooltip nói thêm chỗ xem lại vì Undo là stack chung của cả Editor.
        internal const string FeedbackToastUndoUnavailableReason = "Đã có thao tác khác sau đó";
        internal const string FeedbackToastUndoUnavailableTooltip = "Đã có thao tác khác sau đó — mở Edit → Undo History";
        internal const string FeedbackToastCloseTooltip = "Đóng";

        // Outcome (8.5): kết quả xuất/đăng ở lại tới khi làm việc khác — câu chân nói rõ để người dùng không chờ nó tự tắt.
        internal const string FeedbackOutcomeFootnote = "còn đến khi bạn làm việc khác";

        // Palette ⌘K (8.7, [FD §3.8]): chỉ điều hướng, không bao giờ có lệnh.
        internal const string FeedbackPalettePlaceholder = "Gõ tên màn, tầng hoặc id luật…";
        internal const string FeedbackPaletteFooterFormat = "↑ ↓ di chuyển · Enter mở · Esc đóng · {0} lần nữa: Unity Search";
        // Shortcut mở palette do G-HOSTUI đăng ký (W4): trước đó profile không có phím, chân vẫn phải nói được cách mở Unity Search.
        internal const string FeedbackPaletteFooterWithoutKey = "↑ ↓ di chuyển · Enter mở · Esc đóng · phím mở palette lần nữa: Unity Search";
        internal const string FeedbackPaletteNoMatchFormat = "Không có màn nào khớp \"{0}\"";
        internal const string FeedbackPaletteRuleIdCaption = "id luật";
        internal const string FeedbackPaletteRuleSeparator = " / ";
        internal const string FeedbackPaletteBadgeSeparator = " · ";
        internal const string FeedbackPaletteReasonSeparator = " — ";

        // Hộp xác nhận (8.6, [FD §3.10]).
        internal const string FeedbackConfirmTypePrefix = "Gõ";
        internal const string FeedbackConfirmTypeSuffix = "để xác nhận";
        internal const string FeedbackConfirmTypeHintFormat = "Enter trong ô không chạy nút nào · bấm {0}, hoặc Tab tới nút rồi Space";
        internal const string FeedbackConfirmLockedTooltip = "Gõ đúng id đợt đang chạy để mở khoá";
        // Thiếu UXML thì hộp chỉ còn nút an toàn: không dựng được câu hậu quả thì không được cho bấm phá huỷ.
        internal const string FeedbackConfirmLayoutMissingFormat = "Không tải được bố cục hộp xác nhận: thiếu {0}";

        // Popover (8.7, [FD §7]).
        internal const string FeedbackPopoverCloseTooltip = "Đóng (Esc)";

        // Cảnh báo/lỗi lập trình — không bao giờ do dữ liệu lịch hỏng.
        internal const string FeedbackWarningStyleSheetMissingFormat = "LiveOps Hub: thiếu stylesheet {0} — hộp vẫn dùng được nhưng mất màu và khoảng cách.";
        internal const string FeedbackErrorPopoverSizeInvalid = "Popover phải có kích thước dương (rộng 320 theo [FD §7]).";
        internal const string FeedbackErrorPopoverWithoutContent = "Popover phải dựng nội dung (BuildContent trả null).";
        internal const string FeedbackErrorConfirmRequestMissing = "Hộp xác nhận cần request — section dựng câu trước khi hỏi.";
        internal const string FeedbackErrorPolicyNotAllowed = "Chính sách xác nhận không cho thao tác này — nút phải bị khoá kèm lý do, không được tới hộp.";
        internal const string FeedbackErrorPolicyDedicatedDialog = "Thao tác này dùng hộp riêng (MarkPublishedWindow), không đi qua hộp cấp 1/2.";
        internal const string FeedbackErrorPolicyLevelMismatchFormat = "Chính sách đòi {0} nhưng request là {1} — hộp nhẹ hơn chính sách là lỗi của code dựng hộp.";
        internal const string FeedbackErrorPolicyTypeTextMismatchFormat = "Hộp gõ id phải gõ đúng id đang chạy '{0}' của chính sách, request lại đòi '{1}'.";
        internal const string FeedbackErrorHoverCardContentMissing = "Hover card cần nội dung (hàm dựng trả null).";
    }
}
