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
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineUnplaceableChipFormat),
                vietnamese: "Không đặt được ({0})",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineUnplaceableChipTooltip),
                vietnamese: "Đợt có giờ không đọc được nên không đặt được lên trục — bấm để chọn đợt đó",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineLaneChipTooltipFormat),
                vietnamese: "{0} phát hiện trong làn này",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineRecurringLaneTooltip),
                vietnamese: "Làn lặp — sửa ở Luật lặp",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.TimelineNextOutsideChipFormat),
                vietnamese: "Đợt tới: {0} · {1} · {2}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineNextOutsideChipTooltipFormat),
                vietnamese: "Bấm để căn khung tới {0} ({1} UTC)",
                english: null);

            table.AddShared(nameof(LiveOpsHubStrings.TimelineRenamedLabelFormat), "{0} → {1}");
            table.Add(nameof(LiveOpsHubStrings.TimelineStripLabelFormat),
                vietnamese: "{0} · {1} đợt",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineStripTooltipFormat),
                vietnamese: "{0} · {1} đợt · bấm để zoom vào {2} → {3} UTC",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineBarTooltipFormat),
                vietnamese: "{0} · {1} → {2} UTC",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineDroppedTag),
                vietnamese: "bị bỏ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineWillDropTag),
                vietnamese: "sẽ bị bỏ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineOverlapLabelFormat),
                vietnamese: "chồng {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineClippedStartTooltip),
                vietnamese: "Bắt đầu trước khung nhìn",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineClippedEndTooltip),
                vietnamese: "Kết thúc sau khung nhìn",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineRunningLockTooltipFormat),
                vietnamese: "Bắt đầu: khoá — đã chạy từ {0} UTC",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.TimelineRulerCornerTitle),
                vietnamese: "UTC",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineRulerCornerSubtitleFormat),
                vietnamese: "giờ máy {0} trong tooltip",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineNowFlagTooltipFormat),
                vietnamese: "{0} UTC · {1} giờ máy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelinePublishedFlagTooltipFormat),
                vietnamese: "Đã đăng {0} · sha {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineDeviceTierTooltipFormat),
                vietnamese: "giờ máy {0}",
                english: null);

            table.AddShared(nameof(LiveOpsHubStrings.TimelineReadoutSeparator), " · ");
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutRangeFormat),
                vietnamese: "{0} → {1} UTC",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutStartEdgeFormat),
                vietnamese: "Bắt đầu {0} UTC",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutEndEdgeFormat),
                vietnamese: "Kết thúc {0} UTC",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutDeviceFormat),
                vietnamese: "{0} giờ máy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutShiftFormat),
                vietnamese: "dời {0}{1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutLengthFormat),
                vietnamese: "dài {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineReadoutOverlapFormat),
                vietnamese: "chồng {0} với {1}",
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.TimelinePositiveSign), "+");
            table.AddShared(nameof(LiveOpsHubStrings.TimelineNegativeSign), "−");

            table.Add(nameof(LiveOpsHubStrings.TimelineMinimapLegendTooltipFormat),
                vietnamese: "Minimap {0} → {1} · vạch đỏ cao hết dải = đợt bị bỏ · vạch vàng nửa trên = cảnh báo · cờ = lần đăng · khung = khoảng đang xem · dải màu = loại event",
                english: null);

            table.AddShared(nameof(LiveOpsHubStrings.TimelineMinimapMarkFormat), "{0} · {1} {2}");
            table.AddShared(nameof(LiveOpsHubStrings.TimelineMinimapMarkNoFindingFormat), "{0} · {1} ({2})");
            table.Add(nameof(LiveOpsHubStrings.TimelineMinimapDroppedLabel),
                vietnamese: "Bị bỏ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineMinimapProgressLostLabel),
                vietnamese: "Cảnh báo",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineMinimapShouldReviewLabel),
                vietnamese: "Nên xem",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineMinimapUnplaceableReason),
                vietnamese: "không đặt được",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.TimelineLegendFixed),
                vietnamese: "thanh nổi = cố định",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendRecurring),
                vietnamese: "thanh phẳng = sinh từ luật",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendEnded),
                vietnamese: "nhạt = đã khép",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendOverlap),
                vietnamese: "gạch chéo đỏ = chồng giờ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendChanged),
                vietnamese: "vuông góc phải = khác bản đã đăng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendDropped),
                vietnamese: "gạch ngang + tag = bị bỏ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendClipped),
                vietnamese: "dấu ở mép = đợt tràn khỏi khung",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineLegendUnplaceable),
                vietnamese: "\"Không đặt được\" ở header làn",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.TimelineHintTooltip),
                vietnamese: "Dòng gợi ý đổi theo ngữ cảnh: nghỉ, đang chọn, đang kéo",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineHintIdleFormat),
                vietnamese: "Kéo thanh nổi để dời · kéo mép để đổi giờ · nhấp đúp chỗ trống để thêm đợt · {0}+bánh xe để zoom",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineHintFixedSelected),
                vietnamese: "· ← → nhích theo lưới · Alt+← → mép cuối · Alt+Shift+← → mép đầu",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineHintDroppedFormat),
                vietnamese: "· bị bỏ vì {0} — F8 xem lỗi · ← → nhích theo lưới · Alt+← → mép cuối",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineHintRunningSelected),
                vietnamese: "· đang chạy — mép đầu khoá · Alt+← → mép cuối",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineHintEndedSelected),
                vietnamese: "· đã khép — không dời được",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineHintRecurringSelected),
                vietnamese: "· đợt sinh từ luật — sửa ở Luật lặp",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineHintStripSelected),
                vietnamese: "· dải gom nhiều đợt — bấm để zoom vào",
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.TimelineHintShortcutFormat), " · {0} {1}");
            table.Add(nameof(LiveOpsHubStrings.TimelineHintDuplicate),
                vietnamese: "nhân bản",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineHintDelete),
                vietnamese: "xoá",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineHintDragging),
                vietnamese: "Alt: bỏ bắt lưới · Esc: huỷ",
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.TimelineActionKeyMac), "⌘");
            table.Add(nameof(LiveOpsHubStrings.TimelineActionKeyOther),
                vietnamese: "Ctrl",
                english: null);
        }
    }
}
