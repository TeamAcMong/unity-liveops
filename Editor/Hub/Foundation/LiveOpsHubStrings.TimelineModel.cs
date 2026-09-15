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
        internal const string TimelineRecurringLaneMetaFormat = "lặp mỗi {0} · chạy {1}";
        internal const string TimelineFixedLaneMeta = "cố định";
        internal const string TimelineFixedLaneMetaCountFormat = "cố định · {0} đợt";
        internal const string TimelineFixedLaneMetaOutsideRangeFormat = "cố định · {0} đợt, ngoài khung";
        internal const string TimelineFixedLaneMetaEmpty = "cố định · chưa có đợt";
        internal const string TimelineLaneOverlapPolicyMeta = "Chồng giờ: giữ đợt sớm hơn";

        // Làn TypeId "" gom đợt/luật chưa ghi loại (game bỏ — luật 3): tên làn trống nên meta nói lý do làn tồn tại.
        internal const string TimelineUntypedLaneMeta = "chưa ghi loại";
        internal const string TimelineUntypedLaneMetaCountFormat = "chưa ghi loại · {0} đợt";

        // Id dải gom "sky-race-252…258" và dấu cắt giữa của nhãn thanh (thuật toán barLabel [SD1 §3.5]).
        internal const string TimelineStripIdSeparator = "…";
        internal const string TimelineBarLabelEllipsis = "…";

        // Thước [SD1 §3.2]: tầng 1 "THÁNG 9 2026" (viết HOA sẵn vì USS không có text-transform), tầng 2 thứ Hai "T2 14".
        internal const string TimelineRulerMonthFormat = "THÁNG {0} {1}";
        internal const string TimelineRulerMondayPrefix = "T2";

        // Lỗi lập trình khi dựng input — presenter truyền sai là bug, không phải dữ liệu xấu của người dùng.
        internal const string TimelineDocumentRequired = "Timeline cần tài liệu lịch — presenter phải truyền tài liệu nháp (kể cả tài liệu rỗng).";
        internal const string TimelineRangeInvalid = "Khoảng timeline phải có kết thúc sau bắt đầu — khoảng rỗng làm px/giờ vô hạn.";
        internal const string TimelineTrackWidthInvalid = "Bề rộng track phải lớn hơn 0 — layout chưa xong (NaN/0) thì chưa được dựng model.";
    }
}
