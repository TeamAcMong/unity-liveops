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
        internal const string UtcFieldDatePlaceholder = "yyyy-MM-dd";
        internal const string UtcFieldTimePlaceholder = "HH:mm";

        /// <summary>{0} = chuỗi gõ nguyên văn, {1} = cùng ngày viết đúng dạng ("2026-10-3" → "2026-10-03").</summary>
        internal const string UtcFieldDateUnreadableWithSuggestionFormat = "Không đọc được \"{0}\". Ô ngày cần dạng {1} (yyyy-MM-dd).";
        internal const string UtcFieldDateUnreadableFormat = "Không đọc được \"{0}\". Ô ngày cần dạng yyyy-MM-dd, ví dụ 2026-09-16.";
        internal const string UtcFieldDateEmpty = "Ô ngày còn trống. Ô ngày cần dạng yyyy-MM-dd, ví dụ 2026-09-16.";
        internal const string UtcFieldTimeEmpty = "Ô giờ còn trống. Ô giờ cần dạng HH:mm, ví dụ 07:00.";
        internal const string UtcFieldTimeUnreadableFormat = "Không đọc được \"{0}\". Ô giờ cần dạng HH:mm, ví dụ 07:00.";

        // Tab strip
        internal const string TabStripChoiceOutOfRangeFormat = "Tab thứ {0} không tồn tại — dải tab chỉ có {1} lựa chọn.";

        // JSON viewer (PD-12: hai cách xem cùng một nội dung)
        internal const string JsonViewFormattedTab = "Đã định dạng";
        internal const string JsonViewSingleLineTab = "Một dòng";

        // Lề: + mục thêm mới · ~ field đổi so với bản so · – dòng có ở bản so mà nháp đã bỏ. Thiết kế vẽ dấu trừ U+2212, nhưng ký
        // hiệu viết vào Label chỉ lấy từ danh sách đóng HubGlyphs (có đủ trong Inter hai bản [API §12.3]) — dùng gạch ngắn U+2013.
        internal const string JsonGutterAdded = "+";
        internal const string JsonGutterModified = "~";
        internal const string JsonGutterRemoved = "–";

        // Tooltip vạch dải tổng quan ([SD2 §3.6]): "dòng 54 · bị bỏ".
        internal const string JsonOverviewDroppedTooltipFormat = "dòng {0} · bị bỏ";
        internal const string JsonOverviewWarningTooltipFormat = "dòng {0} · cảnh báo";
        internal const string JsonOverviewChangeTooltipFormat = "dòng {0} · thay đổi";
        internal const string JsonAnnotationLineOutOfRangeFormat = "Chú thích trỏ dòng {0} — số dòng bắt đầu từ 1.";
    }
}
