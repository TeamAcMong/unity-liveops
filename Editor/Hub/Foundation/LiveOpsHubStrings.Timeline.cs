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
        internal static string TimelineUntypedLaneName => LiveOpsHubStringCatalog.Text(nameof(TimelineUntypedLaneName));
        internal static string TimelineUnplaceableChipFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineUnplaceableChipFormat));
        internal static string TimelineUnplaceableChipTooltip => LiveOpsHubStringCatalog.Text(nameof(TimelineUnplaceableChipTooltip));
        internal static string TimelineLaneChipTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineLaneChipTooltipFormat));
        internal static string TimelineRecurringLaneTooltip => LiveOpsHubStringCatalog.Text(nameof(TimelineRecurringLaneTooltip));

        // Chip "Đợt tới" ở mép phải làn không có thanh trong khoảng [SD1 §3.2].
        internal static string TimelineNextOutsideChipFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineNextOutsideChipFormat));
        internal static string TimelineNextOutsideChipTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineNextOutsideChipTooltipFormat));

        // Thanh [SD1 §3.5].
        internal static string TimelineRenamedLabelFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineRenamedLabelFormat));
        internal static string TimelineStripLabelFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineStripLabelFormat));
        internal static string TimelineStripTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineStripTooltipFormat));
        internal static string TimelineBarTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineBarTooltipFormat));
        internal static string TimelineDroppedTag => LiveOpsHubStringCatalog.Text(nameof(TimelineDroppedTag));
        internal static string TimelineWillDropTag => LiveOpsHubStringCatalog.Text(nameof(TimelineWillDropTag));
        internal static string TimelineOverlapLabelFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineOverlapLabelFormat));
        internal static string TimelineClippedStartTooltip => LiveOpsHubStringCatalog.Text(nameof(TimelineClippedStartTooltip));
        internal static string TimelineClippedEndTooltip => LiveOpsHubStringCatalog.Text(nameof(TimelineClippedEndTooltip));
        internal static string TimelineRunningLockTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineRunningLockTooltipFormat));

        // Thước [SD1 §3.2, §3.13]: góc trục ghi UTC, giờ máy nằm trong tooltip.
        internal static string TimelineRulerCornerTitle => LiveOpsHubStringCatalog.Text(nameof(TimelineRulerCornerTitle));
        internal static string TimelineRulerCornerSubtitleFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineRulerCornerSubtitleFormat));
        internal static string TimelineNowFlagTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineNowFlagTooltipFormat));
        internal static string TimelinePublishedFlagTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(TimelinePublishedFlagTooltipFormat));
        internal static string TimelineDeviceTierTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineDeviceTierTooltipFormat));

        // Readout khi kéo [SD1 §3.7]: mỗi phần có chữ dẫn riêng, nối bằng " · ".
        internal static string TimelineReadoutSeparator => LiveOpsHubStringCatalog.Text(nameof(TimelineReadoutSeparator));
        internal static string TimelineReadoutRangeFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineReadoutRangeFormat));
        internal static string TimelineReadoutStartEdgeFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineReadoutStartEdgeFormat));
        internal static string TimelineReadoutEndEdgeFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineReadoutEndEdgeFormat));
        internal static string TimelineReadoutDeviceFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineReadoutDeviceFormat));
        internal static string TimelineReadoutShiftFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineReadoutShiftFormat));
        internal static string TimelineReadoutLengthFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineReadoutLengthFormat));
        internal static string TimelineReadoutOverlapFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineReadoutOverlapFormat));
        internal static string TimelinePositiveSign => LiveOpsHubStringCatalog.Text(nameof(TimelinePositiveSign));
        internal static string TimelineNegativeSign => LiveOpsHubStringCatalog.Text(nameof(TimelineNegativeSign));

        // Minimap [SD1 §3.6]: vạch dựng câu từ LiveOpsFindingText.ShortLabel (CC-FT-1); vạch không có phát hiện nêu lý do riêng.
        internal static string TimelineMinimapLegendTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineMinimapLegendTooltipFormat));
        // Câu vạch nối bằng khoảng trắng như [SD1 §3.6] ("Cảnh báo · weekly-pass-35 đổi id", "Nên xem · hunt-0914 không tự khai
        // configKey"); riêng lý do KHÔNG đến từ phát hiện mới bọc ngoặc ("Bị bỏ · lava-quest-2026-10 (không đặt được)").
        internal static string TimelineMinimapMarkFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineMinimapMarkFormat));
        internal static string TimelineMinimapMarkNoFindingFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineMinimapMarkNoFindingFormat));
        internal static string TimelineMinimapDroppedLabel => LiveOpsHubStringCatalog.Text(nameof(TimelineMinimapDroppedLabel));
        internal static string TimelineMinimapProgressLostLabel => LiveOpsHubStringCatalog.Text(nameof(TimelineMinimapProgressLostLabel));
        internal static string TimelineMinimapShouldReviewLabel => LiveOpsHubStringCatalog.Text(nameof(TimelineMinimapShouldReviewLabel));
        internal static string TimelineMinimapUnplaceableReason => LiveOpsHubStringCatalog.Text(nameof(TimelineMinimapUnplaceableReason));

        // Chú giải 8 mục [SD1 §3.6] — giải mã mọi ký hiệu đang có trên màn.
        internal static string TimelineLegendFixed => LiveOpsHubStringCatalog.Text(nameof(TimelineLegendFixed));
        internal static string TimelineLegendRecurring => LiveOpsHubStringCatalog.Text(nameof(TimelineLegendRecurring));
        internal static string TimelineLegendEnded => LiveOpsHubStringCatalog.Text(nameof(TimelineLegendEnded));
        internal static string TimelineLegendOverlap => LiveOpsHubStringCatalog.Text(nameof(TimelineLegendOverlap));
        internal static string TimelineLegendChanged => LiveOpsHubStringCatalog.Text(nameof(TimelineLegendChanged));
        internal static string TimelineLegendDropped => LiveOpsHubStringCatalog.Text(nameof(TimelineLegendDropped));
        internal static string TimelineLegendClipped => LiveOpsHubStringCatalog.Text(nameof(TimelineLegendClipped));
        internal static string TimelineLegendUnplaceable => LiveOpsHubStringCatalog.Text(nameof(TimelineLegendUnplaceable));

        // Dòng gợi ý [SD1 §3.6]: đổi theo nghỉ / đang chọn / đang kéo; không nhắc phím chưa có ở bản này (I-5).
        internal static string TimelineHintTooltip => LiveOpsHubStringCatalog.Text(nameof(TimelineHintTooltip));
        internal static string TimelineHintIdleFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineHintIdleFormat));
        internal static string TimelineHintFixedSelected => LiveOpsHubStringCatalog.Text(nameof(TimelineHintFixedSelected));
        internal static string TimelineHintDroppedFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineHintDroppedFormat));
        internal static string TimelineHintRunningSelected => LiveOpsHubStringCatalog.Text(nameof(TimelineHintRunningSelected));
        internal static string TimelineHintEndedSelected => LiveOpsHubStringCatalog.Text(nameof(TimelineHintEndedSelected));
        internal static string TimelineHintRecurringSelected => LiveOpsHubStringCatalog.Text(nameof(TimelineHintRecurringSelected));
        internal static string TimelineHintStripSelected => LiveOpsHubStringCatalog.Text(nameof(TimelineHintStripSelected));
        internal static string TimelineHintShortcutFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineHintShortcutFormat));
        internal static string TimelineHintDuplicate => LiveOpsHubStringCatalog.Text(nameof(TimelineHintDuplicate));
        internal static string TimelineHintDelete => LiveOpsHubStringCatalog.Text(nameof(TimelineHintDelete));
        internal static string TimelineHintDragging => LiveOpsHubStringCatalog.Text(nameof(TimelineHintDragging));

        // (G-OPT-TIMELINE, Hình 12 khung 9) Chọn nhiều: dòng gợi ý nói SỐ đợt, làn, và hai phím làm ra tập đó. Không nói tên đợt
        // nào cả — chọn nhiều thì không có "đợt đang chọn" để nêu, và dòng gợi ý phải trả lời "tôi đang giữ cái gì trong tay".
        internal static string TimelineHintMultiSelectedFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineHintMultiSelectedFormat));

        /// <summary>Titlebar inspector khi chọn nhiều đợt cùng loại: "2 đợt · lava-quest" [SD1 §3.10 (c)].</summary>
        internal static string TimelineMultiSelectTitleFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineMultiSelectTitleFormat));

        /// <summary>Tập chọn trải nhiều loại: không nêu được một tên làn nào nên nêu số loại ("3 loại") vào đúng chỗ tên làn.</summary>
        internal static string TimelineMultiSelectMixedTypesFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineMultiSelectMixedTypesFormat));

        internal static string TimelineMultiSelectShiftFieldLabel => LiveOpsHubStringCatalog.Text(nameof(TimelineMultiSelectShiftFieldLabel));
        internal static string TimelineMultiSelectApplyButton => LiveOpsHubStringCatalog.Text(nameof(TimelineMultiSelectApplyButton));
        internal static string TimelineMultiSelectDeleteButtonFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineMultiSelectDeleteButtonFormat));
        internal static string TimelineMultiSelectHelpText => LiveOpsHubStringCatalog.Text(nameof(TimelineMultiSelectHelpText));
        internal static string TimelineMultiSelectShiftToastFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineMultiSelectShiftToastFormat));
        internal static string TimelineMultiSelectShiftUndoStepFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineMultiSelectShiftUndoStepFormat));
        internal static string TimelineMultiSelectDeleteToastFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineMultiSelectDeleteToastFormat));
        internal static string TimelineMultiSelectDeleteUndoStepFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineMultiSelectDeleteUndoStepFormat));

        // (G-OPT-TIMELINE, Hình 12 khung 12) Mục menu header làn — I-5 của W5 giấu mục này vì tính năng chưa có; nay có thật.
        internal static string TimelineMenuCollapseLane => LiveOpsHubStringCatalog.Text(nameof(TimelineMenuCollapseLane));
        internal static string TimelineMenuExpandLane => LiveOpsHubStringCatalog.Text(nameof(TimelineMenuExpandLane));

        // (nợ D-3(b), Hình 12 khung 13) Con trỏ đứng trên chỗ trống của một làn cố định. Nhấp đúp là đường DUY NHẤT tạo đợt
        // bằng chuột ở chỗ trống; không nói ra thì cử chỉ đó chỉ ai đọc tài liệu mới biết.
        internal static string TimelineHintEmptyLaneFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineHintEmptyLaneFormat));

        /// <summary>Khoảng đang xem trong dòng gợi ý: "14/9 → 19/9" — không giờ, không năm (khung 13).</summary>
        internal static string TimelineHintRangeFormat => LiveOpsHubStringCatalog.Text(nameof(TimelineHintRangeFormat));
        internal static string TimelineActionKeyMac => LiveOpsHubStringCatalog.Text(nameof(TimelineActionKeyMac));
        internal static string TimelineActionKeyOther => LiveOpsHubStringCatalog.Text(nameof(TimelineActionKeyOther));
    }
}
