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

        // Rail 36 px của cửa sổ hẹp ([FD §3.7], mục 12 I-8).

        /// <summary>Tooltip nút ‹› trên đầu rail — một câu cho cả hai chiều: chiều chevron đã nói rõ đang mở hay đang thu.</summary>
        internal static string ShellRailPinTooltip => LiveOpsHubStringCatalog.Text(nameof(ShellRailPinTooltip));

        /// <summary>Mục menu của ô tầng: "Kiểm lịch — 2 bị bỏ" ([FD §3.7]); màn không có lý do thì chỉ còn tên màn.</summary>
        internal static string ShellRailNarrowMenuItemFormat => LiveOpsHubStringCatalog.Text(nameof(ShellRailNarrowMenuItemFormat));

        /// <summary>Tooltip ô icon lỗi ở đáy rail thu gọn: "Dừng ở Kiểm lịch: 2 đợt sẽ bị game bỏ khi đọc lịch" ([FD §3.7]).</summary>
        internal static string ShellRailNarrowBlockerTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(ShellRailNarrowBlockerTooltipFormat));

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

        // Header 26 px — ô "Đi tới màn…" và hai chip ([FD §3.3], G-HOSTUI W4)
        internal static string ShellGoToPlaceholder => LiveOpsHubStringCatalog.Text(nameof(ShellGoToPlaceholder));
        internal static string ShellGoToTooltip => LiveOpsHubStringCatalog.Text(nameof(ShellGoToTooltip));

        /// <summary>Khoá con 10 px của chip trái — luôn đi kèm tên file, không bao giờ đứng một mình.</summary>
        internal static string ShellChipCalendarKey => LiveOpsHubStringCatalog.Text(nameof(ShellChipCalendarKey));

        internal static string ShellChipNoCalendar => LiveOpsHubStringCatalog.Text(nameof(ShellChipNoCalendar));
        internal static string ShellChipNoCalendarDraft => LiveOpsHubStringCatalog.Text(nameof(ShellChipNoCalendarDraft));
        internal static string ShellChipNoCalendarTooltip => LiveOpsHubStringCatalog.Text(nameof(ShellChipNoCalendarTooltip));
        internal static string ShellChipAssetTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(ShellChipAssetTooltipFormat));

        /// <summary>Dạng (a) — không có dấu trạng thái vì tab đã mang "*" ([FD §3.3]).</summary>
        internal static string ShellChipUnsaved => LiveOpsHubStringCatalog.Text(nameof(ShellChipUnsaved));

        internal static string ShellChipUnsavedWithKeyFormat => LiveOpsHubStringCatalog.Text(nameof(ShellChipUnsavedWithKeyFormat));
        internal static string ShellChipUnsavedTooltip => LiveOpsHubStringCatalog.Text(nameof(ShellChipUnsavedTooltip));
        internal static string ShellChipPublishedDiffFormat => LiveOpsHubStringCatalog.Text(nameof(ShellChipPublishedDiffFormat));
        internal static string ShellChipPublishedDiffTooltip => LiveOpsHubStringCatalog.Text(nameof(ShellChipPublishedDiffTooltip));
        internal static string ShellChipDiffersFromPublishedFormat => LiveOpsHubStringCatalog.Text(nameof(ShellChipDiffersFromPublishedFormat));
        internal static string ShellChipNeverPublished => LiveOpsHubStringCatalog.Text(nameof(ShellChipNeverPublished));
        internal static string ShellChipMatchesPublishedFormat => LiveOpsHubStringCatalog.Text(nameof(ShellChipMatchesPublishedFormat));
        internal static string ShellChipMatchesPublishedWithoutTime => LiveOpsHubStringCatalog.Text(nameof(ShellChipMatchesPublishedWithoutTime));

        // Status bar 20 px ([FD §3.4], G-HOSTUI W4)
        internal static string ShellStatusCheckedFormat => LiveOpsHubStringCatalog.Text(nameof(ShellStatusCheckedFormat));
        internal static string ShellStatusCheckingFormat => LiveOpsHubStringCatalog.Text(nameof(ShellStatusCheckingFormat));
        internal static string ShellStatusNeverChecked => LiveOpsHubStringCatalog.Text(nameof(ShellStatusNeverChecked));

        /// <summary>
        /// Chưa có asset lịch: câu này thay cho "Chưa kiểm lần nào — F5 để kiểm". Không có asset thì F5
        /// (<c>StartCheckFromShortcut</c>) không làm gì và mục "Kiểm lại tất cả (F5)" của menu ⋮ đã disabled — mời bấm một phím
        /// không làm gì là hai bề mặt nói hai điều khác nhau về cùng một phím (L-1 của soát 16/9, câu lấy từ 7.0).
        /// </summary>
        internal static string ShellStatusNoCalendarAsset => LiveOpsHubStringCatalog.Text(nameof(ShellStatusNoCalendarAsset));
        internal static string ShellStatusStaleEditedFormat => LiveOpsHubStringCatalog.Text(nameof(ShellStatusStaleEditedFormat));
        internal static string ShellStatusStaleMilestoneFormat => LiveOpsHubStringCatalog.Text(nameof(ShellStatusStaleMilestoneFormat));
        internal static string ShellStatusStaleInterrupted => LiveOpsHubStringCatalog.Text(nameof(ShellStatusStaleInterrupted));

        /// <summary>"(⌘Z)" chỉ còn khi bước Undo đó vẫn trên đỉnh — xem <see cref="LiveOpsHubStatusBarModel"/>.</summary>
        internal static string ShellStatusRecentActionWithKeyFormat => LiveOpsHubStringCatalog.Text(nameof(ShellStatusRecentActionWithKeyFormat));

        internal static string ShellStatusRecentActionFormat => LiveOpsHubStringCatalog.Text(nameof(ShellStatusRecentActionFormat));
        internal static string ShellStatusPublishedFormat => LiveOpsHubStringCatalog.Text(nameof(ShellStatusPublishedFormat));
        internal static string ShellStatusShaFormat => LiveOpsHubStringCatalog.Text(nameof(ShellStatusShaFormat));
        internal static string ShellStatusDeviceTimeFormat => LiveOpsHubStringCatalog.Text(nameof(ShellStatusDeviceTimeFormat));

        // Menu ⋮ (8.3) — mục "Kiểm lại tất cả (F5)" chỉ có khi cửa sổ đã có phiên lịch.
        internal static string ShellCheckAllMenuFormat => LiveOpsHubStringCatalog.Text(nameof(ShellCheckAllMenuFormat));
        internal static string ShellCheckAllMenuWithoutKey => LiveOpsHubStringCatalog.Text(nameof(ShellCheckAllMenuWithoutKey));

        /// <summary>Mở README của package — cùng địa chỉ với link "Vì sao? (tài liệu luật)" của Kiểm lịch ([FD §3.3]).</summary>
        internal static string ShellOpenDocumentationMenu => LiveOpsHubStringCatalog.Text(nameof(ShellOpenDocumentationMenu));

        /// <summary>Mở cửa sổ hub thứ hai chạy trên tài liệu mẫu — ngoặc "(chỉ để xem giao diện)" là của thiết kế, giữ nguyên.</summary>
        internal static string ShellShowDesignSampleMenu => LiveOpsHubStringCatalog.Text(nameof(ShellShowDesignSampleMenu));

        // Băng "tệp đã đổi trên đĩa" (Hình 28 khung 4, 4.3, SPIKE-B SP-8b) — hub KHÔNG BAO GIỜ tự đè nháp; ba nút là ba quyết
        // định của người dùng, nên băng phải nói đủ ba câu: đổi bao nhiêu mục · Tải lại mất gì · Giữ bản trong Editor ghi đè gì.

        /// <summary>Câu 1 — tên file, giờ đổi, số mục khác nhau và chính các id đó.</summary>
        internal static string ShellDiskBannerHeadFormat => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerHeadFormat));

        /// <summary>Câu 1 khi diff không nêu id nào (vd chỉ khác key remote): không in cặp ngoặc rỗng.</summary>
        internal static string ShellDiskBannerHeadWithoutItemsFormat => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerHeadWithoutItemsFormat));

        /// <summary>Câu 2 — hậu quả của "Tải lại".</summary>
        internal static string ShellDiskBannerReloadLineFormat => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerReloadLineFormat));

        internal static string ShellDiskBannerReloadLineWithoutItemsFormat => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerReloadLineWithoutItemsFormat));

        /// <summary>Câu 3 — hậu quả của "Giữ bản trong Editor": lần lưu tới sẽ ghi đè.</summary>
        internal static string ShellDiskBannerKeepLineFormat => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerKeepLineFormat));

        /// <summary>Tên hiển thị của một LUẬT LẶP trong danh sách mục ("luật weekly-pass") — Hình 28 khung 4 gọi tên loại mục
        /// chứ không để id trần lẫn với id đợt.</summary>
        internal static string ShellDiskBannerRuleItemFormat => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerRuleItemFormat));

        /// <summary>Động từ của một thay đổi chưa lưu trong câu "Tải lại": mục MỚI thêm.</summary>
        internal static string ShellDiskBannerChangeAddedFormat => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerChangeAddedFormat));

        /// <summary>Động từ: mục đã xoá khỏi nháp.</summary>
        internal static string ShellDiskBannerChangeRemovedFormat => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerChangeRemovedFormat));

        /// <summary>Động từ: đổi giờ bắt đầu/kết thúc — "Dời lava-quest-2026-09b" của Hình 28 khung 4.</summary>
        internal static string ShellDiskBannerChangeMovedFormat => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerChangeMovedFormat));

        /// <summary>Động từ chung cho mọi thay đổi còn lại (config key, tiền tố luật, thuộc tính loại…).</summary>
        internal static string ShellDiskBannerChangeEditedFormat => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerChangeEditedFormat));

        internal static string ShellDiskBannerSentenceSeparator => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerSentenceSeparator));
        internal static string ShellDiskBannerItemSeparator => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerItemSeparator));
        internal static string ShellDiskBannerReloadButton => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerReloadButton));
        internal static string ShellDiskBannerDiffButton => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerDiffButton));
        internal static string ShellDiskBannerKeepButton => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerKeepButton));
        internal static string ShellDiskBannerReloadTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(ShellDiskBannerReloadTooltipFormat));

        // Hộp cấp 1 khi ⌘S sau khi đã chọn "Giữ bản trong Editor" (Hình 28 khung 4 dòng cuối, 8.6 bảng 7.0
        // LiveOpsEditOperation.OverwriteDiskChanges). Tiêu đề là câu thiết kế; ba câu còn lại CHƯA có trong thiết kế (chờ user).
        internal static string ShellOverwriteDiskConfirmTitleFormat => LiveOpsHubStringCatalog.Text(nameof(ShellOverwriteDiskConfirmTitleFormat));
        internal static string ShellOverwriteDiskConfirmBodyFormat => LiveOpsHubStringCatalog.Text(nameof(ShellOverwriteDiskConfirmBodyFormat));
        internal static string ShellOverwriteDiskConfirmBodyWithoutItemsFormat => LiveOpsHubStringCatalog.Text(nameof(ShellOverwriteDiskConfirmBodyWithoutItemsFormat));
        internal static string ShellOverwriteDiskConfirmKeyHint => LiveOpsHubStringCatalog.Text(nameof(ShellOverwriteDiskConfirmKeyHint));
        internal static string ShellOverwriteDiskConfirmDestructive => LiveOpsHubStringCatalog.Text(nameof(ShellOverwriteDiskConfirmDestructive));
        internal static string ShellOverwriteDiskConfirmSafe => LiveOpsHubStringCatalog.Text(nameof(ShellOverwriteDiskConfirmSafe));

        /// <summary>Nhãn nút của outcome (c′) "vừa lưu file" — khung giữ câu vì khung là nơi gọi Reveal (mục 12 I-11).</summary>
        internal static string ShellOutcomeRevealFileButton => LiveOpsHubStringCatalog.Text(nameof(ShellOutcomeRevealFileButton));

        /// <summary>Console giữ dấu vết lần file đổi ngoài — băng trả lời xong thì Console vẫn còn lịch sử để đối chiếu với git.</summary>
        internal static string ShellDiskConflictLogFormat => LiveOpsHubStringCatalog.Text(nameof(ShellDiskConflictLogFormat));

        // Phím tắt toàn hub (8.7) — displayName tiếng Việt trong Edit → Shortcuts (PD-15).
        internal static string ShellShortcutOpenPalette => LiveOpsHubStringCatalog.Text(nameof(ShellShortcutOpenPalette));
        internal static string ShellShortcutSaveCalendar => LiveOpsHubStringCatalog.Text(nameof(ShellShortcutSaveCalendar));
        internal static string ShellShortcutCheckAll => LiveOpsHubStringCatalog.Text(nameof(ShellShortcutCheckAll));
        internal static string ShellShortcutNextFinding => LiveOpsHubStringCatalog.Text(nameof(ShellShortcutNextFinding));
        internal static string ShellShortcutPreviousFinding => LiveOpsHubStringCatalog.Text(nameof(ShellShortcutPreviousFinding));
        internal static string ShellShortcutGoToSectionFormat => LiveOpsHubStringCatalog.Text(nameof(ShellShortcutGoToSectionFormat));
    }
}
