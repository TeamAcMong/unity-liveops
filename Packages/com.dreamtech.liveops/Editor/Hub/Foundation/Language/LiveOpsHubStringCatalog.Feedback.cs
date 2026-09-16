namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Feedback — bản tiếng Việt là bản gốc (chép nguyên văn từ <c>LiveOpsHubStrings.Feedback.cs</c>, không đổi một ký tự),
    /// bản tiếng Anh do gói dịch G-I18N bước 2 điền vào chỗ <c>english: null</c>. Hai ngôn ngữ nằm trong cùng một lệnh đăng ký để
    /// chỗ giữ chỗ <c>{0}</c>/<c>{1}</c> soát được bằng mắt và không bao giờ thêm khoá ở bản này mà quên bản kia.
    /// Comment "vì sao" của từng câu ở lại <c>LiveOpsHubStrings.Feedback.cs</c> cạnh property — đó là chỗ người đọc code tìm tới.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterFeedback(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.FeedbackToastUndoLabel),
                vietnamese: "Hoàn tác",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackToastRedoLabel),
                vietnamese: "Làm lại",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FeedbackToastUndoUnavailableReason),
                vietnamese: "Đã có thao tác khác sau đó",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackToastUndoUnavailableTooltip),
                vietnamese: "Đã có thao tác khác sau đó — mở Edit → Undo History",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackToastCloseTooltip),
                vietnamese: "Đóng",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FeedbackOutcomeFootnote),
                vietnamese: "còn đến khi bạn làm việc khác",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FeedbackPalettePlaceholder),
                vietnamese: "Gõ tên màn, tầng hoặc id luật…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackPaletteFooterFormat),
                vietnamese: "↑ ↓ di chuyển · Enter mở · Esc đóng · {0} lần nữa: Unity Search",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FeedbackPaletteFooterWithoutKey),
                vietnamese: "↑ ↓ di chuyển · Enter mở · Esc đóng · phím mở palette lần nữa: Unity Search",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackPaletteNoMatchFormat),
                vietnamese: "Không có màn nào khớp \"{0}\"",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackPaletteRuleIdCaption),
                vietnamese: "id luật",
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.FeedbackPaletteRuleSeparator), " / ");
            table.AddShared(nameof(LiveOpsHubStrings.FeedbackPaletteBadgeSeparator), " · ");
            table.AddShared(nameof(LiveOpsHubStrings.FeedbackPaletteReasonSeparator), " — ");

            table.Add(nameof(LiveOpsHubStrings.FeedbackConfirmTypePrefix),
                vietnamese: "Gõ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackConfirmTypeSuffix),
                vietnamese: "để xác nhận",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackConfirmTypeHintFormat),
                vietnamese: "Enter trong ô không chạy nút nào · bấm {0}, hoặc Tab tới nút rồi Space",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackConfirmLockedTooltip),
                vietnamese: "Gõ đúng id đợt đang chạy để mở khoá",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FeedbackConfirmLayoutMissingFormat),
                vietnamese: "Không tải được bố cục hộp xác nhận: thiếu {0}",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FeedbackPopoverCloseTooltip),
                vietnamese: "Đóng (Esc)",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FeedbackWarningStyleSheetMissingFormat),
                vietnamese: "LiveOps Hub: thiếu stylesheet {0} — hộp vẫn dùng được nhưng mất màu và khoảng cách.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorPopoverSizeInvalid),
                vietnamese: "Popover phải có kích thước dương (rộng 320 theo [FD §7]).",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorPopoverWithoutContent),
                vietnamese: "Popover phải dựng nội dung (BuildContent trả null).",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorConfirmRequestMissing),
                vietnamese: "Hộp xác nhận cần request — section dựng câu trước khi hỏi.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorPolicyNotAllowed),
                vietnamese: "Chính sách xác nhận không cho thao tác này — nút phải bị khoá kèm lý do, không được tới hộp.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorPolicyDedicatedDialog),
                vietnamese: "Thao tác này dùng hộp riêng (MarkPublishedWindow), không đi qua hộp cấp 1/2.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorPolicyLevelMismatchFormat),
                vietnamese: "Chính sách đòi {0} nhưng request là {1} — hộp nhẹ hơn chính sách là lỗi của code dựng hộp.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorPolicyTypeTextMismatchFormat),
                vietnamese: "Hộp gõ id phải gõ đúng id đang chạy '{0}' của chính sách, request lại đòi '{1}'.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorHoverCardContentMissing),
                vietnamese: "Hover card cần nội dung (hàm dựng trả null).",
                english: null);
        }
    }
}
