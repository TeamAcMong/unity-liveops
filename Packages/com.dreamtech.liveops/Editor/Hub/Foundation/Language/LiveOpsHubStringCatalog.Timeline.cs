namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Timeline — bản tiếng Việt là bản gốc (chép nguyên văn từ <c>LiveOpsHubStrings.Timeline.cs</c>, không đổi một ký tự),
    /// bản tiếng Anh do gói dịch G-I18N bước 2 điền vào chỗ <c>english: null</c>. Hai ngôn ngữ nằm trong cùng một lệnh đăng ký để
    /// chỗ giữ chỗ <c>{0}</c>/<c>{1}</c> soát được bằng mắt và không bao giờ thêm khoá ở bản này mà quên bản kia.
    /// Comment "vì sao" của từng câu ở lại <c>LiveOpsHubStrings.Timeline.cs</c> cạnh property — đó là chỗ người đọc code tìm tới.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterTimeline(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.TimelineUntypedLaneName),
                vietnamese: "(chưa ghi loại)",
                english: "(no type set)");
            table.Add(nameof(LiveOpsHubStrings.TimelineUnplaceableChipFormat),
                vietnamese: "Không đặt được ({0})",
                english: "Cannot place ({0})");
            table.Add(nameof(LiveOpsHubStrings.TimelineUnplaceableChipTooltip),
                vietnamese: "Đợt có giờ không đọc được nên không đặt được lên trục — bấm để chọn đợt đó",
                english: "The event has times that cannot be read, so it cannot be placed on the axis — click to select it");
            table.Add(nameof(LiveOpsHubStrings.TimelineLaneChipTooltipFormat),
                vietnamese: "{0} phát hiện trong làn này",
                english: "{0} findings in this lane");
            table.Add(nameof(LiveOpsHubStrings.TimelineRecurringLaneTooltip),
                vietnamese: "Làn lặp — sửa ở Luật lặp",
                english: "Recurring lane — edit it in Recurring rules");

            table.Add(nameof(LiveOpsHubStrings.TimelineNextOutsideChipFormat),
                vietnamese: "Đợt tới: {0} · {1} · {2}",
                english: "Next event: {0} · {1} · {2}");
            table.Add(nameof(LiveOpsHubStrings.TimelineNextOutsideChipTooltipFormat),
                vietnamese: "Bấm để căn khung tới {0} ({1} UTC)",
                english: "Click to bring the view to {0} ({1} UTC)");

            table.AddShared(nameof(LiveOpsHubStrings.TimelineRenamedLabelFormat), "{0} → {1}");
            table.Add(nameof(LiveOpsHubStrings.TimelineStripLabelFormat),
                vietnamese: "{0} · {1} đợt",
                english: "{0} · {1} events");
            table.Add(nameof(LiveOpsHubStrings.TimelineStripTooltipFormat),
                vietnamese: "{0} · {1} đợt · bấm để zoom vào {2} → {3} UTC",
                english: "{0} · {1} events · click to zoom into {2} → {3} UTC");
            table.Add(nameof(LiveOpsHubStrings.TimelineBarTooltipFormat),
                vietnamese: "{0} · {1} → {2} UTC",
                english: "{0} · {1} → {2} UTC");
            table.Add(nameof(LiveOpsHubStrings.TimelineDroppedTag),
                vietnamese: "bị bỏ",
                english: "dropped");
            table.Add(nameof(LiveOpsHubStrings.TimelineWillDropTag),
                vietnamese: "sẽ bị bỏ",
                english: "will be dropped");
            table.Add(nameof(LiveOpsHubStrings.TimelineOverlapLabelFormat),
                vietnamese: "chồng {0}",
                english: "overlap {0}");
            table.Add(nameof(LiveOpsHubStrings.TimelineClippedStartTooltip),
                vietnamese: "Bắt đầu trước khung nhìn",
                english: "Starts before the view");
            table.Add(nameof(LiveOpsHubStrings.TimelineClippedEndTooltip),
                vietnamese: "Kết thúc sau khung nhìn",
                english: "Ends after the view");
            table.Add(nameof(LiveOpsHubStrings.TimelineRunningLockTooltipFormat),
                vietnamese: "Bắt đầu: khoá — đã chạy từ {0} UTC",
                english: "Start: locked — running since {0} UTC");

            table.Add(nameof(LiveOpsHubStrings.TimelineRulerCornerTitle),
                vietnamese: "UTC",
                english: "UTC");
            table.Add(nameof(LiveOpsHubStrings.TimelineRulerCornerSubtitleFormat),
                vietnamese: "giờ máy {0} trong tooltip",
                english: "device time {0} in the tooltip");
            table.Add(nameof(LiveOpsHubStrings.TimelineNowFlagTooltipFormat),
                vietnamese: "{0} UTC · {1} giờ máy",
                english: "{0} UTC · {1} device time");
            table.Add(nameof(LiveOpsHubStrings.TimelinePublishedFlagTooltipFormat),
                vietnamese: "Đã đăng {0} · sha {1}",
                english: "Published {0} · sha {1}");
            table.Add(nameof(LiveOpsHubStrings.TimelineDeviceTierTooltipFormat),
                vietnamese: "giờ máy {0}",
                english: "device time {0}");

            table.AddShared(nameof(LiveOpsHubStrings.TimelineReadoutSeparator), " · ");
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutRangeFormat),
                vietnamese: "{0} → {1} UTC",
                english: "{0} → {1} UTC");
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutStartEdgeFormat),
                vietnamese: "Bắt đầu {0} UTC",
                english: "Start {0} UTC");
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutEndEdgeFormat),
                vietnamese: "Kết thúc {0} UTC",
                english: "End {0} UTC");
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutDeviceFormat),
                vietnamese: "{0} giờ máy",
                english: "{0} device time");
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutShiftFormat),
                vietnamese: "dời {0}{1}",
                english: "shift {0}{1}");
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutLengthFormat),
                vietnamese: "dài {0}",
                english: "length {0}");
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutOverlapFormat),
                vietnamese: "chồng {0} với {1}",
                english: "overlap {0} with {1}");
            table.AddShared(nameof(LiveOpsHubStrings.TimelinePositiveSign), "+");
            table.AddShared(nameof(LiveOpsHubStrings.TimelineNegativeSign), "−");

            table.Add(nameof(LiveOpsHubStrings.TimelineMinimapLegendTooltipFormat),
                vietnamese: "Minimap {0} → {1} · vạch đỏ cao hết dải = đợt bị bỏ · vạch vàng nửa trên = cảnh báo · cờ = lần đăng · khung = khoảng đang xem · dải màu = loại event",
                english: "Minimap {0} → {1} · full-height red tick = dropped event · upper-half yellow tick = warning · flag = publish · frame = the range in view · color band = event type");

            table.AddShared(nameof(LiveOpsHubStrings.TimelineMinimapMarkFormat), "{0} · {1} {2}");
            table.AddShared(nameof(LiveOpsHubStrings.TimelineMinimapMarkNoFindingFormat), "{0} · {1} ({2})");
            table.Add(nameof(LiveOpsHubStrings.TimelineMinimapDroppedLabel),
                vietnamese: "Bị bỏ",
                english: "Dropped");
            table.Add(nameof(LiveOpsHubStrings.TimelineMinimapProgressLostLabel),
                vietnamese: "Cảnh báo",
                english: "Warning");
            table.Add(nameof(LiveOpsHubStrings.TimelineMinimapShouldReviewLabel),
                vietnamese: "Nên xem",
                english: "Should review");
            table.Add(nameof(LiveOpsHubStrings.TimelineMinimapUnplaceableReason),
                vietnamese: "không đặt được",
                english: "cannot be placed");

            table.Add(nameof(LiveOpsHubStrings.TimelineLegendFixed),
                vietnamese: "thanh nổi = cố định",
                english: "raised bar = fixed");
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendRecurring),
                vietnamese: "thanh phẳng = sinh từ luật",
                english: "flat bar = generated by a rule");
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendEnded),
                vietnamese: "nhạt = đã khép",
                english: "faded = ended");
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendOverlap),
                vietnamese: "gạch chéo đỏ = chồng giờ",
                english: "red hatching = overlap");
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendChanged),
                vietnamese: "vuông góc phải = khác bản đã đăng",
                english: "square right corner = differs from the published baseline");
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendDropped),
                vietnamese: "gạch ngang + tag = bị bỏ",
                english: "strikethrough + tag = dropped");
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendClipped),
                vietnamese: "dấu ở mép = đợt tràn khỏi khung",
                english: "marker at the edge = event runs past the view");
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendUnplaceable),
                vietnamese: "\"Không đặt được\" ở header làn",
                english: "\"Cannot place\" in the lane header");

            table.Add(nameof(LiveOpsHubStrings.TimelineHintTooltip),
                vietnamese: "Dòng gợi ý đổi theo ngữ cảnh: nghỉ, đang chọn, đang kéo",
                english: "The hint line follows context: idle, selected, dragging");
            table.Add(nameof(LiveOpsHubStrings.TimelineHintIdleFormat),
                vietnamese: "Kéo thanh nổi để dời · kéo mép để đổi giờ · nhấp đúp chỗ trống để thêm đợt · {0}+bánh xe để zoom",
                english: "Drag a raised bar to move it · drag an edge to change times · double-click empty space to add an event · {0}+wheel to zoom");
            table.Add(nameof(LiveOpsHubStrings.TimelineHintFixedSelected),
                vietnamese: "· ← → nhích theo lưới · Alt+← → mép cuối · Alt+Shift+← → mép đầu",
                english: "· ← → nudge by grid · Alt+← → end edge · Alt+Shift+← → start edge");
            table.Add(nameof(LiveOpsHubStrings.TimelineHintDroppedFormat),
                vietnamese: "· bị bỏ vì {0} — F8 xem lỗi · ← → nhích theo lưới · Alt+← → mép cuối",
                english: "· dropped because of {0} — F8 shows the error · ← → nudge by grid · Alt+← → end edge");
            table.Add(nameof(LiveOpsHubStrings.TimelineHintRunningSelected),
                vietnamese: "· đang chạy — mép đầu khoá · Alt+← → mép cuối",
                english: "· running — start edge locked · Alt+← → end edge");
            table.Add(nameof(LiveOpsHubStrings.TimelineHintEndedSelected),
                vietnamese: "· đã khép — không dời được",
                english: "· ended — cannot be moved");
            table.Add(nameof(LiveOpsHubStrings.TimelineHintRecurringSelected),
                vietnamese: "· đợt sinh từ luật — sửa ở Luật lặp",
                english: "· event generated by a rule — edit it in Recurring rules");
            table.Add(nameof(LiveOpsHubStrings.TimelineHintStripSelected),
                vietnamese: "· dải gom nhiều đợt — bấm để zoom vào",
                english: "· strip of several events — click to zoom in");
            table.AddShared(nameof(LiveOpsHubStrings.TimelineHintShortcutFormat), " · {0} {1}");
            table.Add(nameof(LiveOpsHubStrings.TimelineHintDuplicate),
                vietnamese: "nhân bản",
                english: "duplicate");
            table.Add(nameof(LiveOpsHubStrings.TimelineHintDelete),
                vietnamese: "xoá",
                english: "delete");
            table.Add(nameof(LiveOpsHubStrings.TimelineHintDragging),
                vietnamese: "Alt: bỏ bắt lưới · Esc: huỷ",
                english: "Alt: ignore grid snap · Esc: cancel");
            table.AddShared(nameof(LiveOpsHubStrings.TimelineActionKeyMac), "⌘");
            table.Add(nameof(LiveOpsHubStrings.TimelineActionKeyOther),
                vietnamese: "Ctrl",
                english: "Ctrl");
        }
    }
}
