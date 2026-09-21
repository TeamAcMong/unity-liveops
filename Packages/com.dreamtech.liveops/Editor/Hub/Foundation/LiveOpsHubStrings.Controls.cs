namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Controls (G-CONTROLS): gợi ý và câu lỗi của ô ngày giờ UTC, nhãn tab của JSON viewer, tooltip dải tổng quan, thông
    /// báo lỗi lập trình của control dùng chung. Câu của phát hiện (chú thích cuối dòng JSON) KHÔNG ở đây — presenter truyền câu
    /// dựng sẵn từ <c>LiveOpsFindingText</c> (V-8, V-21 D-5).
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Ô ngày giờ UTC — gợi ý là đúng dạng cần gõ, không phải ví dụ ngày (ví dụ dễ bị đọc nhầm thành giá trị có sẵn).
        internal static string UtcFieldDatePlaceholder => LiveOpsHubStringCatalog.Text(nameof(UtcFieldDatePlaceholder));
        internal static string UtcFieldTimePlaceholder => LiveOpsHubStringCatalog.Text(nameof(UtcFieldTimePlaceholder));

        /// <summary>{0} = chuỗi gõ nguyên văn, {1} = cùng ngày viết đúng dạng ("2026-10-3" → "2026-10-03").</summary>
        internal static string UtcFieldDateUnreadableWithSuggestionFormat => LiveOpsHubStringCatalog.Text(nameof(UtcFieldDateUnreadableWithSuggestionFormat));
        internal static string UtcFieldDateUnreadableFormat => LiveOpsHubStringCatalog.Text(nameof(UtcFieldDateUnreadableFormat));
        internal static string UtcFieldDateEmpty => LiveOpsHubStringCatalog.Text(nameof(UtcFieldDateEmpty));
        internal static string UtcFieldTimeEmpty => LiveOpsHubStringCatalog.Text(nameof(UtcFieldTimeEmpty));
        internal static string UtcFieldTimeUnreadableFormat => LiveOpsHubStringCatalog.Text(nameof(UtcFieldTimeUnreadableFormat));

        // Tab strip
        internal static string TabStripChoiceOutOfRangeFormat => LiveOpsHubStringCatalog.Text(nameof(TabStripChoiceOutOfRangeFormat));

        // JSON viewer (PD-12: hai cách xem cùng một nội dung)
        internal static string JsonViewFormattedTab => LiveOpsHubStringCatalog.Text(nameof(JsonViewFormattedTab));
        internal static string JsonViewSingleLineTab => LiveOpsHubStringCatalog.Text(nameof(JsonViewSingleLineTab));

        // Lề: + mục thêm mới · ~ field đổi so với bản so · – dòng có ở bản so mà nháp đã bỏ. Thiết kế vẽ dấu trừ U+2212, nhưng ký
        // hiệu viết vào Label chỉ lấy từ danh sách đóng HubGlyphs (có đủ trong Inter hai bản [API §12.3]) — dùng gạch ngắn U+2013.
        internal static string JsonGutterAdded => LiveOpsHubStringCatalog.Text(nameof(JsonGutterAdded));
        internal static string JsonGutterModified => LiveOpsHubStringCatalog.Text(nameof(JsonGutterModified));
        internal static string JsonGutterRemoved => LiveOpsHubStringCatalog.Text(nameof(JsonGutterRemoved));

        // Tooltip vạch dải tổng quan ([SD2 §3.6]): "dòng 54 · bị bỏ".
        internal static string JsonOverviewDroppedTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(JsonOverviewDroppedTooltipFormat));
        internal static string JsonOverviewWarningTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(JsonOverviewWarningTooltipFormat));
        internal static string JsonOverviewChangeTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(JsonOverviewChangeTooltipFormat));
        internal static string JsonAnnotationLineOutOfRangeFormat => LiveOpsHubStringCatalog.Text(nameof(JsonAnnotationLineOutOfRangeFormat));
    }
}
