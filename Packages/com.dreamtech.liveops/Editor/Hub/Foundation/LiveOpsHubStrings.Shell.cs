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
        internal static string ShellWindowTitle => LiveOpsHubStringCatalog.Text(nameof(ShellWindowTitle));
        internal static string ShellReduceMotionMenu => LiveOpsHubStringCatalog.Text(nameof(ShellReduceMotionMenu));

        // Registry — tiêu đề và subtitle của 6 màn (7.1–7.6).
        internal static string ShellOverviewTitle => LiveOpsHubStringCatalog.Text(nameof(ShellOverviewTitle));
        internal static string ShellOverviewSubtitle => LiveOpsHubStringCatalog.Text(nameof(ShellOverviewSubtitle));
        internal static string ShellEventTypesTitle => LiveOpsHubStringCatalog.Text(nameof(ShellEventTypesTitle));
        internal static string ShellEventTypesSubtitle => LiveOpsHubStringCatalog.Text(nameof(ShellEventTypesSubtitle));
        internal static string ShellCalendarTitle => LiveOpsHubStringCatalog.Text(nameof(ShellCalendarTitle));
        internal static string ShellCalendarSubtitle => LiveOpsHubStringCatalog.Text(nameof(ShellCalendarSubtitle));
        internal static string ShellRecurringRulesTitle => LiveOpsHubStringCatalog.Text(nameof(ShellRecurringRulesTitle));
        internal static string ShellRecurringRulesSubtitle => LiveOpsHubStringCatalog.Text(nameof(ShellRecurringRulesSubtitle));
        internal static string ShellValidationTitle => LiveOpsHubStringCatalog.Text(nameof(ShellValidationTitle));

        /// <summary>
        /// Thiết kế ghi "Game sẽ làm gì với lịch này — nhóm theo hậu quả với người chơi" (62 ký tự) trong khi hợp đồng subtitle là
        /// dưới 60 ([FD §3.6]); bỏ "này" để giữ hợp đồng mà không đổi nghĩa.
        /// </summary>
        internal static string ShellValidationSubtitle => LiveOpsHubStringCatalog.Text(nameof(ShellValidationSubtitle));

        internal static string ShellExportTitle => LiveOpsHubStringCatalog.Text(nameof(ShellExportTitle));
        internal static string ShellExportSubtitle => LiveOpsHubStringCatalog.Text(nameof(ShellExportSubtitle));

        // INTERIM(G-SHELLPOLISH): lý do health + dòng đầu thân của màn giữ chỗ (mục 12 I-2) — xoá cùng InterimPlaceholderSection.
        internal static string InterimPlaceholderReason => LiveOpsHubStringCatalog.Text(nameof(InterimPlaceholderReason));

        // Rail — [FD §3.5]
        internal static string ShellRailCaption => LiveOpsHubStringCatalog.Text(nameof(ShellRailCaption));
        internal static string ShellRailNotMeasuredBadge => LiveOpsHubStringCatalog.Text(nameof(ShellRailNotMeasuredBadge));
        internal static string ShellRailStaleBadgePrefix => LiveOpsHubStringCatalog.Text(nameof(ShellRailStaleBadgePrefix));
        internal static string ShellRailPartSeparator => LiveOpsHubStringCatalog.Text(nameof(ShellRailPartSeparator));
        internal static string ShellRailDroppedCountFormat => LiveOpsHubStringCatalog.Text(nameof(ShellRailDroppedCountFormat));
        internal static string ShellRailProgressLostCountFormat => LiveOpsHubStringCatalog.Text(nameof(ShellRailProgressLostCountFormat));
        internal static string ShellRailShouldReviewCountFormat => LiveOpsHubStringCatalog.Text(nameof(ShellRailShouldReviewCountFormat));
        internal static string ShellRailNotMeasuredCountFormat => LiveOpsHubStringCatalog.Text(nameof(ShellRailNotMeasuredCountFormat));
        internal static string ShellRailStaleTooltipPrefixFormat => LiveOpsHubStringCatalog.Text(nameof(ShellRailStaleTooltipPrefixFormat));
        internal static string ShellRailStaleTooltipPrefixWithoutTime => LiveOpsHubStringCatalog.Text(nameof(ShellRailStaleTooltipPrefixWithoutTime));
        internal static string ShellRailBlockerTitleFormat => LiveOpsHubStringCatalog.Text(nameof(ShellRailBlockerTitleFormat));
        internal static string ShellRailBlockerDroppedDetailFormat => LiveOpsHubStringCatalog.Text(nameof(ShellRailBlockerDroppedDetailFormat));
        internal static string ShellRailBlockerStaleDetailFormat => LiveOpsHubStringCatalog.Text(nameof(ShellRailBlockerStaleDetailFormat));
        internal static string ShellRailHealthCountMismatchFormat => LiveOpsHubStringCatalog.Text(nameof(ShellRailHealthCountMismatchFormat));

        // Health
        internal static string ShellHealthThrewReason => LiveOpsHubStringCatalog.Text(nameof(ShellHealthThrewReason));
        internal static string ShellHealthThrewLogFormat => LiveOpsHubStringCatalog.Text(nameof(ShellHealthThrewLogFormat));

        // Màn ném khi dựng — Hình 28 khung 1
        internal static string ShellSectionFailureTitleFormat => LiveOpsHubStringCatalog.Text(nameof(ShellSectionFailureTitleFormat));
        internal static string ShellSectionFailureFootnoteFormat => LiveOpsHubStringCatalog.Text(nameof(ShellSectionFailureFootnoteFormat));
        internal static string ShellSectionFailedHealthReason => LiveOpsHubStringCatalog.Text(nameof(ShellSectionFailedHealthReason));
        internal static string ShellSectionThrewLogFormat => LiveOpsHubStringCatalog.Text(nameof(ShellSectionThrewLogFormat));
        internal static string ShellNullViewMessageFormat => LiveOpsHubStringCatalog.Text(nameof(ShellNullViewMessageFormat));
        internal static string ShellViewStateThrewLogFormat => LiveOpsHubStringCatalog.Text(nameof(ShellViewStateThrewLogFormat));
        internal static string ShellCopyErrorButton => LiveOpsHubStringCatalog.Text(nameof(ShellCopyErrorButton));
        internal static string ShellRetryBuildButton => LiveOpsHubStringCatalog.Text(nameof(ShellRetryBuildButton));

        // Thiếu bố cục — Hình 28 khung 2
        internal static string ShellMissingLayoutTitleFormat => LiveOpsHubStringCatalog.Text(nameof(ShellMissingLayoutTitleFormat));
        internal static string ShellMissingLayoutBodyFormat => LiveOpsHubStringCatalog.Text(nameof(ShellMissingLayoutBodyFormat));
        internal static string ShellMissingElementsTitle => LiveOpsHubStringCatalog.Text(nameof(ShellMissingElementsTitle));
        internal static string ShellMissingElementsBodyFormat => LiveOpsHubStringCatalog.Text(nameof(ShellMissingElementsBodyFormat));
        internal static string ShellOpenPackageFolderButton => LiveOpsHubStringCatalog.Text(nameof(ShellOpenPackageFolderButton));
        internal static string ShellMissingStyleSheetWarningFormat => LiveOpsHubStringCatalog.Text(nameof(ShellMissingStyleSheetWarningFormat));

        // Đang biên dịch — Hình 28 khung 3
        internal static string ShellCompilingNote => LiveOpsHubStringCatalog.Text(nameof(ShellCompilingNote));

        // Điều hướng
        internal static string ShellUnknownSectionWarningFormat => LiveOpsHubStringCatalog.Text(nameof(ShellUnknownSectionWarningFormat));
    }
}
