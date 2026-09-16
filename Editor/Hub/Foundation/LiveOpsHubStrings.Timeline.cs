namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Timeline (G-TIMELINE-VIEW): chữ mà control timeline tự hiện — header làn, chip, tag trên thanh, thước, readout khi
    /// kéo, minimap, chú giải 8 mục, dòng gợi ý theo ngữ cảnh [SD1 §3.2–3.7]. Chữ model thuần (meta làn, nhãn thước, id dải) ở vùng
    /// TimelineModel; câu phát hiện lấy từ <c>LiveOpsFindingText</c> (V-8) — ở đây chỉ có khung ghép quanh câu đó.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Header làn [SD1 §3.2]. (V-22 CC-TLMODEL-2) làn TypeId "" không có id để in: tên giữ chỗ thay ô tên trống.
        internal const string TimelineUntypedLaneName = "(chưa ghi loại)";
        internal const string TimelineUnplaceableChipFormat = "Không đặt được ({0})";
        internal const string TimelineUnplaceableChipTooltip = "Đợt có giờ không đọc được nên không đặt được lên trục — bấm để chọn đợt đó";
        internal const string TimelineLaneChipTooltipFormat = "{0} phát hiện trong làn này";
        internal const string TimelineRecurringLaneTooltip = "Làn lặp — sửa ở Luật lặp";

        // Chip "Đợt tới" ở mép phải làn không có thanh trong khoảng [SD1 §3.2].
        internal const string TimelineNextOutsideChipFormat = "Đợt tới: {0} · {1} · {2}";
        internal const string TimelineNextOutsideChipTooltipFormat = "Bấm để căn khung tới {0} ({1} UTC)";

        // Thanh [SD1 §3.5].
        internal const string TimelineRenamedLabelFormat = "{0} → {1}";
        internal const string TimelineStripLabelFormat = "{0} · {1} đợt";
        internal const string TimelineStripTooltipFormat = "{0} · {1} đợt · bấm để zoom vào {2} → {3} UTC";
        internal const string TimelineBarTooltipFormat = "{0} · {1} → {2} UTC";
        internal const string TimelineDroppedTag = "bị bỏ";
        internal const string TimelineWillDropTag = "sẽ bị bỏ";
        internal const string TimelineOverlapLabelFormat = "chồng {0}";
        internal const string TimelineClippedStartTooltip = "Bắt đầu trước khung nhìn";
        internal const string TimelineClippedEndTooltip = "Kết thúc sau khung nhìn";
        internal const string TimelineRunningLockTooltipFormat = "Bắt đầu: khoá — đã chạy từ {0} UTC";

        // Thước [SD1 §3.2, §3.13]: góc trục ghi UTC, giờ máy nằm trong tooltip.
        internal const string TimelineRulerCornerTitle = "UTC";
        internal const string TimelineRulerCornerSubtitleFormat = "giờ máy {0} trong tooltip";
        internal const string TimelineNowFlagTooltipFormat = "{0} UTC · {1} giờ máy";
        internal const string TimelinePublishedFlagTooltipFormat = "Đã đăng {0} · sha {1}";
        internal const string TimelineDeviceTierTooltipFormat = "giờ máy {0}";

        // Readout khi kéo [SD1 §3.7]: mỗi phần có chữ dẫn riêng, nối bằng " · ".
        internal const string TimelineReadoutSeparator = " · ";
        internal const string TimelineReadoutRangeFormat = "{0} → {1} UTC";
        internal const string TimelineReadoutStartEdgeFormat = "Bắt đầu {0} UTC";
        internal const string TimelineReadoutEndEdgeFormat = "Kết thúc {0} UTC";
        internal const string TimelineReadoutDeviceFormat = "{0} giờ máy";
        internal const string TimelineReadoutShiftFormat = "dời {0}{1}";
        internal const string TimelineReadoutLengthFormat = "dài {0}";
        internal const string TimelineReadoutOverlapFormat = "chồng {0} với {1}";
        internal const string TimelinePositiveSign = "+";
        internal const string TimelineNegativeSign = "−";

        // Minimap [SD1 §3.6]: vạch dựng câu từ LiveOpsFindingText.ShortLabel (CC-FT-1); vạch không có phát hiện nêu lý do riêng.
        internal const string TimelineMinimapLegendTooltipFormat =
            "Minimap {0} → {1} · vạch đỏ cao hết dải = đợt bị bỏ · vạch vàng nửa trên = cảnh báo · cờ = lần đăng · khung = khoảng đang xem · dải màu = loại event";
        // Câu vạch nối bằng khoảng trắng như [SD1 §3.6] ("Cảnh báo · weekly-pass-35 đổi id", "Nên xem · hunt-0914 không tự khai
        // configKey"); riêng lý do KHÔNG đến từ phát hiện mới bọc ngoặc ("Bị bỏ · lava-quest-2026-10 (không đặt được)").
        internal const string TimelineMinimapMarkFormat = "{0} · {1} {2}";
        internal const string TimelineMinimapMarkNoFindingFormat = "{0} · {1} ({2})";
        internal const string TimelineMinimapDroppedLabel = "Bị bỏ";
        internal const string TimelineMinimapProgressLostLabel = "Cảnh báo";
        internal const string TimelineMinimapShouldReviewLabel = "Nên xem";
        internal const string TimelineMinimapUnplaceableReason = "không đặt được";

        // Chú giải 8 mục [SD1 §3.6] — giải mã mọi ký hiệu đang có trên màn.
        internal const string TimelineLegendFixed = "thanh nổi = cố định";
        internal const string TimelineLegendRecurring = "thanh phẳng = sinh từ luật";
        internal const string TimelineLegendEnded = "nhạt = đã khép";
        internal const string TimelineLegendOverlap = "gạch chéo đỏ = chồng giờ";
        internal const string TimelineLegendChanged = "vuông góc phải = khác bản đã đăng";
        internal const string TimelineLegendDropped = "gạch ngang + tag = bị bỏ";
        internal const string TimelineLegendClipped = "dấu ở mép = đợt tràn khỏi khung";
        internal const string TimelineLegendUnplaceable = "\"Không đặt được\" ở header làn";

        // Dòng gợi ý [SD1 §3.6]: đổi theo nghỉ / đang chọn / đang kéo; không nhắc phím chưa có ở bản này (I-5).
        internal const string TimelineHintTooltip = "Dòng gợi ý đổi theo ngữ cảnh: nghỉ, đang chọn, đang kéo";
        internal const string TimelineHintIdleFormat = "Kéo thanh nổi để dời · kéo mép để đổi giờ · nhấp đúp chỗ trống để thêm đợt · {0}+bánh xe để zoom";
        internal const string TimelineHintFixedSelected = "· ← → nhích theo lưới · Alt+← → mép cuối · Alt+Shift+← → mép đầu";
        internal const string TimelineHintDroppedFormat = "· bị bỏ vì {0} — F8 xem lỗi · ← → nhích theo lưới · Alt+← → mép cuối";
        internal const string TimelineHintRunningSelected = "· đang chạy — mép đầu khoá · Alt+← → mép cuối";
        internal const string TimelineHintEndedSelected = "· đã khép — không dời được";
        internal const string TimelineHintRecurringSelected = "· đợt sinh từ luật — sửa ở Luật lặp";
        internal const string TimelineHintStripSelected = "· dải gom nhiều đợt — bấm để zoom vào";
        internal const string TimelineHintShortcutFormat = " · {0} {1}";
        internal const string TimelineHintDuplicate = "nhân bản";
        internal const string TimelineHintDelete = "xoá";
        internal const string TimelineHintDragging = "Alt: bỏ bắt lưới · Esc: huỷ";
        internal const string TimelineActionKeyMac = "⌘";
        internal const string TimelineActionKeyOther = "Ctrl";
    }
}
