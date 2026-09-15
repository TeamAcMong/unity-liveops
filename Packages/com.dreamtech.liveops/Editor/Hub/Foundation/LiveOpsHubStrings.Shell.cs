namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Shell (G-SHELL → G-HOSTUI → G-SHELLPOLISH): tiêu đề cửa sổ, rail, card lỗi Hình 28, note đang biên dịch, tiêu đề +
    /// subtitle của 6 màn trong registry. Microcopy nguyên văn theo [FD §3], [FD §4.1]; subtitle dưới 60 ký tự (hợp đồng
    /// <see cref="IHubSection.Subtitle"/>).
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Cửa sổ
        internal const string ShellWindowTitle = "LiveOps Hub";
        internal const string ShellReduceMotionMenu = "Tắt chuyển động";

        // Registry — tiêu đề và subtitle của 6 màn (7.1–7.6).
        internal const string ShellOverviewTitle = "Tổng quan";
        internal const string ShellOverviewSubtitle = "Lịch đang kẹt ở đâu và việc gì cần làm trước khi đăng";
        internal const string ShellEventTypesTitle = "Loại event";
        internal const string ShellEventTypesSubtitle = "Mỗi loại một làn trên lịch: màu, cách vào, config mặc định";
        internal const string ShellCalendarTitle = "Lịch";
        internal const string ShellCalendarSubtitle = "Mỗi loại một làn · kéo để dời · UTC là giờ thật của dữ liệu";
        internal const string ShellRecurringRulesTitle = "Luật lặp";
        internal const string ShellRecurringRulesSubtitle = "Đợt tự sinh theo chu kỳ — id, giờ mở và giờ khép";
        internal const string ShellValidationTitle = "Kiểm lịch";

        /// <summary>
        /// Thiết kế ghi "Game sẽ làm gì với lịch này — nhóm theo hậu quả với người chơi" (62 ký tự) trong khi hợp đồng subtitle là
        /// dưới 60 ([FD §3.6]); bỏ "này" để giữ hợp đồng mà không đổi nghĩa.
        /// </summary>
        internal const string ShellValidationSubtitle = "Game sẽ làm gì với lịch — nhóm theo hậu quả với người chơi";

        internal const string ShellExportTitle = "Xuất JSON";
        internal const string ShellExportSubtitle = "JSON cho key liveops_calendar, so với lần đăng";

        // INTERIM(G-SHELLPOLISH): lý do health + dòng đầu thân của màn giữ chỗ (mục 12 I-2) — xoá cùng InterimPlaceholderSection.
        internal const string InterimPlaceholderReason = "Màn này chưa dựng ở bản dev";

        // Rail — [FD §3.5]
        internal const string ShellRailCaption = "ĐƯỜNG ĐI CỦA LỊCH";
        internal const string ShellRailNotMeasuredBadge = "chưa kiểm";
        internal const string ShellRailStaleBadgePrefix = "cũ · ";
        internal const string ShellRailPartSeparator = " · ";
        internal const string ShellRailDroppedCountFormat = "{0} bị bỏ";
        internal const string ShellRailProgressLostCountFormat = "{0} mất tiến độ";
        internal const string ShellRailShouldReviewCountFormat = "{0} nên xem";
        internal const string ShellRailNotMeasuredCountFormat = "{0} chưa kiểm";
        internal const string ShellRailStaleTooltipPrefixFormat = "Kết quả kiểm lúc {0}, lịch đã đổi sau đó: ";
        internal const string ShellRailStaleTooltipPrefixWithoutTime = "Kết quả kiểm đã cũ, lịch đã đổi sau đó: ";
        internal const string ShellRailBlockerTitleFormat = "Dừng ở {0}";
        internal const string ShellRailBlockerDroppedDetailFormat = "{0} đợt sẽ bị game bỏ khi đọc lịch.";
        internal const string ShellRailBlockerStaleDetailFormat = "Kết quả cũ có {0} đợt bị bỏ — F5 để kiểm lại.";
        internal const string ShellRailHealthCountMismatchFormat = "Rail nhận {0} health cho {1} màn — mỗi màn đúng một health theo cùng thứ tự registry.";

        // Health
        internal const string ShellHealthThrewReason = "Kiểm của màn này ném lỗi — xem Console";
        internal const string ShellHealthThrewLogFormat = "LiveOps Hub: GetHealth của một màn ném {0}: {1} — màn hiện NotMeasured tới lần tính kế.\n{2}";

        // Màn ném khi dựng — Hình 28 khung 1
        internal const string ShellSectionFailureTitleFormat = "Không hiện được màn {0}";
        internal const string ShellSectionFailureFootnoteFormat = "Các màn khác vẫn mở được từ rail. Health của {0} là NotMeasured \"{1}\".";
        internal const string ShellSectionFailedHealthReason = "Màn ném lỗi khi dựng";
        internal const string ShellSectionThrewLogFormat = "LiveOps Hub: màn '{0}' ném lỗi khi dựng — cửa sổ hiện card lỗi, các màn khác vẫn mở được.\n{1}";
        internal const string ShellNullViewMessageFormat = "CreateView của màn '{0}' trả null — màn phải trả một VisualElement.";
        internal const string ShellViewStateThrewLogFormat = "LiveOps Hub: màn '{0}' ném khi chụp trạng thái view — bỏ trạng thái của màn này: {1}";
        internal const string ShellCopyErrorButton = "Copy lỗi";
        internal const string ShellRetryBuildButton = "Thử dựng lại";

        // Thiếu bố cục — Hình 28 khung 2
        internal const string ShellMissingLayoutTitleFormat = "Không tải được bố cục LiveOps Hub: thiếu {0}";
        internal const string ShellMissingLayoutBodyFormat = "Tìm ở {0}. Package có thể đang import dở hoặc thư mục Editor bị xoá.";
        internal const string ShellMissingElementsTitle = "Không dựng được khung LiveOps Hub: bố cục thiếu element";
        internal const string ShellMissingElementsBodyFormat = "Thiếu: {0}. Kiểm {1} — tên element phải khớp LiveOpsHubPaths.RequiredShellElementNames.";
        internal const string ShellOpenPackageFolderButton = "Mở thư mục package";
        internal const string ShellMissingStyleSheetWarningFormat = "LiveOps Hub: không nạp được stylesheet {0} — khung vẫn chạy nhưng thiếu style.";

        // Đang biên dịch — Hình 28 khung 3
        internal const string ShellCompilingNote = "Unity đang biên dịch — hub sẽ dựng lại sau khi xong";

        // Điều hướng
        internal const string ShellUnknownSectionWarningFormat = "LiveOps Hub: không có màn id '{0}'. Id hợp lệ ở LiveOpsHubSections.Ids — hiện Tổng quan.";
    }
}
