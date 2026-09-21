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
                english: "Undo");
            table.Add(nameof(LiveOpsHubStrings.FeedbackToastRedoLabel),
                vietnamese: "Làm lại",
                english: "Redo");

            table.Add(nameof(LiveOpsHubStrings.FeedbackToastUndoUnavailableReason),
                vietnamese: "Đã có thao tác khác sau đó",
                english: "Another action happened after it");
            table.Add(nameof(LiveOpsHubStrings.FeedbackToastUndoUnavailableTooltip),
                vietnamese: "Đã có thao tác khác sau đó — mở Edit → Undo History",
                english: "Another action happened after it — open Edit → Undo History");
            table.Add(nameof(LiveOpsHubStrings.FeedbackToastCloseTooltip),
                vietnamese: "Đóng",
                english: "Close");

            table.Add(nameof(LiveOpsHubStrings.FeedbackOutcomeFootnote),
                vietnamese: "còn đến khi bạn làm việc khác",
                english: "stays until you do something else");

            table.Add(nameof(LiveOpsHubStrings.FeedbackPalettePlaceholder),
                vietnamese: "Gõ tên màn, tầng hoặc id luật…",
                english: "Type a screen name, stage or rule id…");
            table.Add(nameof(LiveOpsHubStrings.FeedbackPaletteFooterFormat),
                vietnamese: "↑ ↓ di chuyển · Enter mở · Esc đóng · {0} lần nữa: Unity Search",
                english: "↑ ↓ move · Enter open · Esc close · {0} again: Unity Search");

            table.Add(nameof(LiveOpsHubStrings.FeedbackPaletteFooterWithoutKey),
                vietnamese: "↑ ↓ di chuyển · Enter mở · Esc đóng · phím mở palette lần nữa: Unity Search",
                english: "↑ ↓ move · Enter open · Esc close · the palette key again: Unity Search");
            table.Add(nameof(LiveOpsHubStrings.FeedbackPaletteNoMatchFormat),
                vietnamese: "Không có màn nào khớp \"{0}\"",
                english: "No screen matches \"{0}\"");
            table.Add(nameof(LiveOpsHubStrings.FeedbackPaletteRuleIdCaption),
                vietnamese: "id luật",
                english: "rule id");
            table.AddShared(nameof(LiveOpsHubStrings.FeedbackPaletteRuleSeparator), " / ");
            table.AddShared(nameof(LiveOpsHubStrings.FeedbackPaletteBadgeSeparator), " · ");
            table.AddShared(nameof(LiveOpsHubStrings.FeedbackPaletteReasonSeparator), " — ");

            table.Add(nameof(LiveOpsHubStrings.FeedbackConfirmTypePrefix),
                vietnamese: "Gõ",
                english: "Type");
            table.Add(nameof(LiveOpsHubStrings.FeedbackConfirmTypeSuffix),
                vietnamese: "để xác nhận",
                english: "to confirm");
            table.Add(nameof(LiveOpsHubStrings.FeedbackConfirmTypeHintFormat),
                vietnamese: "Enter trong ô không chạy nút nào · bấm {0}, hoặc Tab tới nút rồi Space",
                english: "Enter in the field runs no button · press {0}, or Tab to the button then Space");
            table.Add(nameof(LiveOpsHubStrings.FeedbackConfirmLockedTooltip),
                vietnamese: "Gõ đúng id đợt đang chạy để mở khoá",
                english: "Type the running event id exactly to unlock");

            table.Add(nameof(LiveOpsHubStrings.FeedbackConfirmLayoutMissingFormat),
                vietnamese: "Không tải được bố cục hộp xác nhận: thiếu {0}",
                english: "Cannot load the confirm dialog layout: {0} is missing");

            table.Add(nameof(LiveOpsHubStrings.FeedbackPopoverCloseTooltip),
                vietnamese: "Đóng (Esc)",
                english: "Close (Esc)");

            table.Add(nameof(LiveOpsHubStrings.FeedbackWarningStyleSheetMissingFormat),
                vietnamese: "LiveOps Hub: thiếu stylesheet {0} — hộp vẫn dùng được nhưng mất màu và khoảng cách.",
                english: "LiveOps Hub: stylesheet {0} is missing — the dialog still works but loses its colors and spacing.");
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorPopoverSizeInvalid),
                vietnamese: "Popover phải có kích thước dương (rộng 320 theo [FD §7]).",
                english: "A popover must have a positive size (width 320 per [FD §7]).");
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorPopoverWithoutContent),
                vietnamese: "Popover phải dựng nội dung (BuildContent trả null).",
                english: "A popover must build content (BuildContent returned null).");
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorConfirmRequestMissing),
                vietnamese: "Hộp xác nhận cần request — section dựng câu trước khi hỏi.",
                english: "The confirm dialog needs a request — the section builds the wording before asking.");
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorPolicyNotAllowed),
                vietnamese: "Chính sách xác nhận không cho thao tác này — nút phải bị khoá kèm lý do, không được tới hộp.",
                english: "The confirm policy does not allow this action — the button must be locked with a reason instead of reaching the dialog.");
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorPolicyDedicatedDialog),
                vietnamese: "Thao tác này dùng hộp riêng (MarkPublishedWindow), không đi qua hộp cấp 1/2.",
                english: "This action uses its own dialog (MarkPublishedWindow), not the level 1/2 dialog.");
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorPolicyLevelMismatchFormat),
                vietnamese: "Chính sách đòi {0} nhưng request là {1} — hộp nhẹ hơn chính sách là lỗi của code dựng hộp.",
                english: "The policy requires {0} but the request is {1} — a dialog weaker than the policy is a bug in the code that builds it.");
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorPolicyTypeTextMismatchFormat),
                vietnamese: "Hộp gõ id phải gõ đúng id đang chạy '{0}' của chính sách, request lại đòi '{1}'.",
                english: "The type-the-id dialog must require the policy's running id '{0}', but the request asks for '{1}'.");
            table.Add(nameof(LiveOpsHubStrings.FeedbackErrorHoverCardContentMissing),
                vietnamese: "Hover card cần nội dung (hàm dựng trả null).",
                english: "A hover card needs content (the builder returned null).");
        }
    }
}
