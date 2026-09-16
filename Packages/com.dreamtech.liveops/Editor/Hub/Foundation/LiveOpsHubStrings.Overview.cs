namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Overview (G-OVERVIEW → G-SHELLPOLISH): 4 metric, card "Việc cần làm trước khi đăng", card "Đường đi của lịch",
    /// card "7 ngày tới", note cuối trang và trạng thái (a) chưa có asset. Microcopy nguyên văn theo [SD1 §1.1–§1.4]; câu của
    /// từng phát hiện KHÔNG viết ở đây mà lấy từ <see cref="LiveOpsFindingText"/> (V-8) — màn Tổng quan chỉ viết câu của
    /// chính nó (tiêu đề card, cột, ghi chú), không viết lại câu của luật.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Header màn — nút chính "Kiểm lại tất cả" = StartCheck + InvalidateAll (7.1).
        internal static string OverviewRecheckAllButton => LiveOpsHubStringCatalog.Text(nameof(OverviewRecheckAllButton));

        /// <summary>Dấu nối giữa các mảnh đếm ("2 bị bỏ · 1 mất tiến độ") — ký hiệu trung tính, một bản cho mọi ngôn ngữ.</summary>
        internal static string OverviewPartSeparator => LiveOpsHubStringCatalog.Text(nameof(OverviewPartSeparator));

        /// <summary>
        /// Dấu nối riêng cho DANH SÁCH id ("weekly-pass-35, sky-race-251" — [SD1 §1.1]). Tách khỏi
        /// <see cref="OverviewPartSeparator"/> vì " · " ngăn các mảnh KHÁC loại của một dòng phụ; dùng lẫn thì một danh sách id
        /// đọc thành hai cột.
        /// </summary>
        internal static string OverviewIdListSeparator => LiveOpsHubStringCatalog.Text(nameof(OverviewIdListSeparator));

        internal static string OverviewDroppedCountFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewDroppedCountFormat));
        internal static string OverviewProgressLostCountFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewProgressLostCountFormat));
        internal static string OverviewShouldReviewCountFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewShouldReviewCountFormat));

        // Metric — caption viết HOA sẵn vì USS không có text-transform ([FD §2.9]).
        internal static string OverviewMetricRunningCaption => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricRunningCaption));
        internal static string OverviewMetricRunningUnit => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricRunningUnit));

        /// <summary>
        /// Dạng số ít của <see cref="OverviewMetricRunningUnit"/>. Tiếng Việt không đổi ("đợt"), nhưng bản tiếng Anh in ngay
        /// trên ảnh mẫu người dùng đọc — "1 events" là lỗi chính tả mà catalog phải chịu trách nhiệm, không phải nơi gọi.
        /// </summary>
        internal static string OverviewMetricRunningUnitSingle => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricRunningUnitSingle));
        internal static string OverviewMetricRunningEmptyFoot => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricRunningEmptyFoot));

        internal static string OverviewMetricNeedsActionCaption => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricNeedsActionCaption));

        /// <summary>Tooltip nói metric đếm theo phát hiện — cùng bộ tổng hợp với Kiểm lịch, không phải phép đếm riêng của màn này.</summary>
        internal static string OverviewMetricNeedsActionTooltip => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricNeedsActionTooltip));

        /// <summary>Chưa kiểm lần nào: vòng rỗng + câu này. Không bao giờ in 0 hay "—" thay cho "không đo được" (7.1).</summary>
        internal static string OverviewMetricNeverCheckedFoot => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricNeverCheckedFoot));

        internal static string OverviewMetricCheckingFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricCheckingFormat));
        internal static string OverviewMetricNeedsActionNoneFoot => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricNeedsActionNoneFoot));

        internal static string OverviewMetricNotCheckedCaption => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricNotCheckedCaption));

        /// <summary>(PD-1) P1 không có màn tầng CHẠY nên "chưa kiểm" chỉ còn đếm luật, không có mảnh "2 màn".</summary>
        internal static string OverviewMetricNotCheckedRulesFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricNotCheckedRulesFormat));

        /// <summary>Dạng số ít của <see cref="OverviewMetricNotCheckedRulesFormat"/> ("1 rule", không "1 rules").</summary>
        internal static string OverviewMetricNotCheckedSingleRuleFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricNotCheckedSingleRuleFormat));

        internal static string OverviewMetricNotCheckedRemoteRulesFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricNotCheckedRemoteRulesFormat));

        /// <summary>Dạng số ít của <see cref="OverviewMetricNotCheckedRemoteRulesFormat"/> — ngữ cảnh thường gặp nhất (đúng một luật bản remote).</summary>
        internal static string OverviewMetricNotCheckedSingleRemoteRuleFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricNotCheckedSingleRemoteRuleFormat));
        internal static string OverviewMetricNotCheckedNoneFoot => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricNotCheckedNoneFoot));

        internal static string OverviewMetricPublishedCaption => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricPublishedCaption));
        internal static string OverviewMetricPublishedFootFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricPublishedFootFormat));
        internal static string OverviewMetricPublishedFootUnchangedFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricPublishedFootUnchangedFormat));
        internal static string OverviewMetricPublishedNoStampFoot => LiveOpsHubStringCatalog.Text(nameof(OverviewMetricPublishedNoStampFoot));

        // Card "Việc cần làm trước khi đăng"
        internal static string OverviewNeedsActionCardTitle => LiveOpsHubStringCatalog.Text(nameof(OverviewNeedsActionCardTitle));
        internal static string OverviewNeedsActionCardSubtitle => LiveOpsHubStringCatalog.Text(nameof(OverviewNeedsActionCardSubtitle));
        internal static string OverviewNeedsActionCardNoBlockersSubtitle => LiveOpsHubStringCatalog.Text(nameof(OverviewNeedsActionCardNoBlockersSubtitle));

        /// <summary>Cột 96px của hàng việc cần làm: trạng thái CỔNG (dấu Blocked 7px), tách khỏi mức của phát hiện [SD1 §1.1].</summary>
        internal static string OverviewBlocksCopyLabel => LiveOpsHubStringCatalog.Text(nameof(OverviewBlocksCopyLabel));

        internal static string OverviewNoBlockersEmptyFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewNoBlockersEmptyFormat));

        /// <summary>Dạng số ít của <see cref="OverviewNoBlockersEmptyFormat"/> ("1 rule not measured").</summary>
        internal static string OverviewNoBlockersEmptySingleRuleFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewNoBlockersEmptySingleRuleFormat));
        internal static string OverviewNoBlockersEmptyNoRules => LiveOpsHubStringCatalog.Text(nameof(OverviewNoBlockersEmptyNoRules));

        /// <summary>Caption dưới danh sách (c): chưa kiểm vẫn liệt kê, vì "chưa kiểm không có nghĩa là ổn" [SD1 §1.4].</summary>
        internal static string OverviewNotCheckedStillListedCaption => LiveOpsHubStringCatalog.Text(nameof(OverviewNotCheckedStillListedCaption));

        // Hàng việc cần làm — nút và câu của chính màn (câu phát hiện lấy từ LiveOpsFindingText).
        internal static string OverviewRowDroppedTitleFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewRowDroppedTitleFormat));
        internal static string OverviewRowDroppedDetail => LiveOpsHubStringCatalog.Text(nameof(OverviewRowDroppedDetail));
        internal static string OverviewRowProgressLostDetailFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewRowProgressLostDetailFormat));
        internal static string OverviewRowShouldReviewTitleFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewRowShouldReviewTitleFormat));
        internal static string OverviewRowRemoteTitle => LiveOpsHubStringCatalog.Text(nameof(OverviewRowRemoteTitle));

        /// <summary>{0} = id luật, truyền từ <c>LiveEventCalendarRuleIds</c> — id luật không được viết thẳng vào câu (6.1).</summary>
        internal static string OverviewRowRemoteDetailFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewRowRemoteDetailFormat));

        internal static string OverviewRowStaleCheckEditedTitleFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewRowStaleCheckEditedTitleFormat));
        internal static string OverviewRowStaleCheckMilestoneTitleFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewRowStaleCheckMilestoneTitleFormat));
        internal static string OverviewRowStaleCheckTitle => LiveOpsHubStringCatalog.Text(nameof(OverviewRowStaleCheckTitle));
        internal static string OverviewRowNoStampTitle => LiveOpsHubStringCatalog.Text(nameof(OverviewRowNoStampTitle));

        internal static string OverviewOpenValidationButton => LiveOpsHubStringCatalog.Text(nameof(OverviewOpenValidationButton));
        internal static string OverviewOpenValidationShouldReviewButton => LiveOpsHubStringCatalog.Text(nameof(OverviewOpenValidationShouldReviewButton));
        internal static string OverviewOpenExportButton => LiveOpsHubStringCatalog.Text(nameof(OverviewOpenExportButton));
        internal static string OverviewOpenRuleButtonFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewOpenRuleButtonFormat));
        internal static string OverviewOpenEventButtonFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewOpenEventButtonFormat));
        internal static string OverviewOpenEventTypesButton => LiveOpsHubStringCatalog.Text(nameof(OverviewOpenEventTypesButton));
        internal static string OverviewRecheckButton => LiveOpsHubStringCatalog.Text(nameof(OverviewRecheckButton));
        internal static string OverviewPasteRunningJsonButton => LiveOpsHubStringCatalog.Text(nameof(OverviewPasteRunningJsonButton));

        // "Ở đâu" — tên màn + tầng, viết sẵn vì tầng đọc bằng caption HOA sẽ sai giọng trong câu.
        internal static string OverviewLocationEventTypes => LiveOpsHubStringCatalog.Text(nameof(OverviewLocationEventTypes));
        internal static string OverviewLocationCalendar => LiveOpsHubStringCatalog.Text(nameof(OverviewLocationCalendar));
        internal static string OverviewLocationRecurring => LiveOpsHubStringCatalog.Text(nameof(OverviewLocationRecurring));
        internal static string OverviewLocationValidation => LiveOpsHubStringCatalog.Text(nameof(OverviewLocationValidation));
        internal static string OverviewLocationExport => LiveOpsHubStringCatalog.Text(nameof(OverviewLocationExport));

        // Card "Đường đi của lịch"
        internal static string OverviewFlowCardTitle => LiveOpsHubStringCatalog.Text(nameof(OverviewFlowCardTitle));
        internal static string OverviewFlowCardSubtitle => LiveOpsHubStringCatalog.Text(nameof(OverviewFlowCardSubtitle));
        internal static string OverviewFlowNoteOk => LiveOpsHubStringCatalog.Text(nameof(OverviewFlowNoteOk));

        /// <summary>Tầng cổng chặn ĐẦU TIÊN: đường nối sau nó chết — mắt thấy lịch dừng ở đâu [SD1 §1.1].</summary>
        internal static string OverviewFlowNoteStopsHere => LiveOpsHubStringCatalog.Text(nameof(OverviewFlowNoteStopsHere));

        internal static string OverviewFlowNoteBlocked => LiveOpsHubStringCatalog.Text(nameof(OverviewFlowNoteBlocked));
        internal static string OverviewFlowNoteWarning => LiveOpsHubStringCatalog.Text(nameof(OverviewFlowNoteWarning));
        internal static string OverviewFlowNoteNotMeasured => LiveOpsHubStringCatalog.Text(nameof(OverviewFlowNoteNotMeasured));
        internal static string OverviewFlowTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewFlowTooltipFormat));

        // Card "7 ngày tới"
        internal static string OverviewUpcomingCardTitleDraft => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingCardTitleDraft));
        internal static string OverviewUpcomingCardTitlePublished => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingCardTitlePublished));
        internal static string OverviewUpcomingRangeFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingRangeFormat));
        internal static string OverviewUpcomingTabDraft => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingTabDraft));
        internal static string OverviewUpcomingTabPublished => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingTabPublished));
        internal static string OverviewUpcomingTabPublishedDisabledReason => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingTabPublishedDisabledReason));
        internal static string OverviewUpcomingColumnTime => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingColumnTime));
        internal static string OverviewUpcomingColumnEvent => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingColumnEvent));
        internal static string OverviewUpcomingColumnType => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingColumnType));
        internal static string OverviewUpcomingColumnKind => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingColumnKind));
        internal static string OverviewUpcomingColumnNote => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingColumnNote));
        internal static string OverviewUpcomingKindOpen => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingKindOpen));
        internal static string OverviewUpcomingKindClose => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingKindClose));
        internal static string OverviewUpcomingKindDropped => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingKindDropped));

        /// <summary>Loại lặp dày (sky-race) gom MỘT dòng: "7 đợt" thay vì 7 dòng gần giống nhau [SD1 §1.1].</summary>
        internal static string OverviewUpcomingKindGroupedFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingKindGroupedFormat));

        internal static string OverviewUpcomingGroupedIdsFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingGroupedIdsFormat));
        internal static string OverviewUpcomingGroupedTimeFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingGroupedTimeFormat));
        internal static string OverviewUpcomingGroupedNoteFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingGroupedNoteFormat));
        internal static string OverviewUpcomingDraftOnlyTag => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingDraftOnlyTag));
        internal static string OverviewUpcomingNotePendingResult => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingNotePendingResult));
        internal static string OverviewUpcomingNoteRequiresJoin => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingNoteRequiresJoin));
        internal static string OverviewUpcomingNoteClosesAtFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingNoteClosesAtFormat));
        internal static string OverviewUpcomingNotePublishedIdFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingNotePublishedIdFormat));
        internal static string OverviewUpcomingNoteRunningIdChangedFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingNoteRunningIdChangedFormat));
        internal static string OverviewUpcomingEmptyTitle => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingEmptyTitle));
        internal static string OverviewUpcomingEmptyBody => LiveOpsHubStringCatalog.Text(nameof(OverviewUpcomingEmptyBody));

        /// <summary>Note cuối trang [SD1 §1.1] — nói thẳng vòng rỗng không phải "đạt" và hub không gửi gì lên Firebase.</summary>
        internal static string OverviewFooterNote => LiveOpsHubStringCatalog.Text(nameof(OverviewFooterNote));

        // (a) chưa có asset — empty ba bước [SD1 §1.2]
        internal static string OverviewNoAssetTitle => LiveOpsHubStringCatalog.Text(nameof(OverviewNoAssetTitle));
        internal static string OverviewNoAssetBody => LiveOpsHubStringCatalog.Text(nameof(OverviewNoAssetBody));
        internal static string OverviewNoAssetStepCreate => LiveOpsHubStringCatalog.Text(nameof(OverviewNoAssetStepCreate));
        internal static string OverviewNoAssetStepDeclareTypes => LiveOpsHubStringCatalog.Text(nameof(OverviewNoAssetStepDeclareTypes));
        internal static string OverviewNoAssetStepFirstCheck => LiveOpsHubStringCatalog.Text(nameof(OverviewNoAssetStepFirstCheck));
        internal static string OverviewCreateAssetButton => LiveOpsHubStringCatalog.Text(nameof(OverviewCreateAssetButton));
        internal static string OverviewImportRunningJsonButton => LiveOpsHubStringCatalog.Text(nameof(OverviewImportRunningJsonButton));
        internal static string OverviewSelectAssetButton => LiveOpsHubStringCatalog.Text(nameof(OverviewSelectAssetButton));
        internal static string OverviewCreateAssetDialogTitle => LiveOpsHubStringCatalog.Text(nameof(OverviewCreateAssetDialogTitle));
        internal static string OverviewCreateAssetFailedFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewCreateAssetFailedFormat));

        /// <summary>UXML của màn thiếu trên đĩa: câu đi vào card lỗi của khung, nêu đúng đường dẫn để sửa được.</summary>
        internal static string OverviewMissingLayoutFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewMissingLayoutFormat));

        /// <summary>(b) nhiều asset: câu của model; HelpBox + menu "Đổi…" là G-SHELLPOLISH (mục 12 I-9).</summary>
        internal static string OverviewMultipleAssetsNoticeFormat => LiveOpsHubStringCatalog.Text(nameof(OverviewMultipleAssetsNoticeFormat));
    }
}
