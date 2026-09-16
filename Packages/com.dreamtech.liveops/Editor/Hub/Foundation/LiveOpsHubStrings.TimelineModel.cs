namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng TimelineModel (G-TIMELINE-MODEL): chữ mà model/hình học timeline thuần tự dựng — meta header làn, id dải gom,
    /// dấu cắt giữa của nhãn thanh, nhãn thước, thông báo lỗi lập trình khi dựng input sai. Chữ của control (chú giải, dòng gợi
    /// ý, tooltip thanh, chip "Đợt tới") thuộc vùng Timeline của G-TIMELINE-VIEW; câu "Khác bản đã đăng: …" thuộc
    /// <c>LiveOpsChangeText</c> (V-21 D-5) — model không dựng câu đó.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Meta header làn [SD1 §3.2]: "lặp mỗi 7 ngày · chạy 7 ngày", "cố định · 2 đợt", "cố định · 1 đợt, ngoài khung".
        internal static string TimelineRecurringLaneMetaFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineRecurringLaneMetaFormat));
        internal static string TimelineFixedLaneMeta => LiveOpsHubStringCatalog.Text(nameof(TimelineFixedLaneMeta));
        internal static string TimelineFixedLaneMetaCountFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineFixedLaneMetaCountFormat));
        internal static string TimelineFixedLaneMetaOutsideRangeFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineFixedLaneMetaOutsideRangeFormat));
        internal static string TimelineFixedLaneMetaEmpty => LiveOpsHubStringCatalog.Text(nameof(TimelineFixedLaneMetaEmpty));
        internal static string TimelineLaneOverlapPolicyMeta => LiveOpsHubStringCatalog.Text(nameof(TimelineLaneOverlapPolicyMeta));

        // Làn TypeId "" gom đợt/luật chưa ghi loại (game bỏ — luật 3): tên làn trống nên meta nói lý do làn tồn tại.
        internal static string TimelineUntypedLaneMeta => LiveOpsHubStringCatalog.Text(nameof(TimelineUntypedLaneMeta));
        internal static string TimelineUntypedLaneMetaCountFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineUntypedLaneMetaCountFormat));

        // Id dải gom "sky-race-252…258" và dấu cắt giữa của nhãn thanh (thuật toán barLabel [SD1 §3.5]).
        internal static string TimelineStripIdSeparator => LiveOpsHubStringCatalog.Text(nameof(TimelineStripIdSeparator));
        internal static string TimelineBarLabelEllipsis => LiveOpsHubStringCatalog.Text(nameof(TimelineBarLabelEllipsis));

        // Thước [SD1 §3.2]: tầng 1 "THÁNG 9 2026" (viết HOA sẵn vì USS không có text-transform), tầng 2 thứ Hai "T2 14".
        internal static string TimelineRulerMonthFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineRulerMonthFormat));
        internal static string TimelineRulerMondayPrefix => LiveOpsHubStringCatalog.Text(nameof(TimelineRulerMondayPrefix));

        // Lỗi lập trình khi dựng input — presenter truyền sai là bug, không phải dữ liệu xấu của người dùng.
        internal static string TimelineDocumentRequired => LiveOpsHubStringCatalog.Text(nameof(TimelineDocumentRequired));
        internal static string TimelineRangeInvalid => LiveOpsHubStringCatalog.Text(nameof(TimelineRangeInvalid));
        internal static string TimelineTrackWidthInvalid => LiveOpsHubStringCatalog.Text(nameof(TimelineTrackWidthInvalid));
    }
}
