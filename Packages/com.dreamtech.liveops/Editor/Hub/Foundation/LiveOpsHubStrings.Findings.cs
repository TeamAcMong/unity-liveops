namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Findings (G-FINDINGTEXT, V-8): mọi mảnh câu nói về một phát hiện Kiểm lịch hoặc một hàng diff. Chỉ
    /// <see cref="LiveOpsFindingText"/> và <see cref="LiveOpsChangeText"/> ghép các hằng này — màn nào cũng lấy câu qua hai lớp đó,
    /// không đọc thẳng hằng ở đây, để một phát hiện nói cùng một câu ở rail, timeline, Lịch, Tổng quan, Kiểm lịch và Xuất JSON.
    /// Microcopy nguyên văn theo [SD2 §2.3], [SD2 §3.8], [SD1 §2.2], [FD §3.1]; câu cho biến thể thiết kế chưa vẽ theo khuôn
    /// hàng mẫu gần nhất (mục 6.1 cột Microcopy). <c>{n}</c> là chỗ giá trị đã bọc <c>&lt;noparse&gt;</c> hoặc giờ đã định dạng.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // ----- Mảnh chung -----
        internal static string FindingPartSeparator => LiveOpsHubStringCatalog.Text(nameof(FindingPartSeparator));
        internal static string FindingValueJoin => LiveOpsHubStringCatalog.Text(nameof(FindingValueJoin));
        internal static string FindingListJoin => LiveOpsHubStringCatalog.Text(nameof(FindingListJoin));
        internal static string FindingQuotedFormat => LiveOpsHubStringCatalog.Text(nameof(FindingQuotedFormat));
        internal static string FindingFoundFormat => LiveOpsHubStringCatalog.Text(nameof(FindingFoundFormat));
        internal static string FindingExpectedFormat => LiveOpsHubStringCatalog.Text(nameof(FindingExpectedFormat));
        internal static string FindingRangeFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRangeFormat));
        internal static string FindingRangeUtcFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRangeUtcFormat));
        internal static string FindingRuleIdLineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRuleIdLineFormat));
        internal static string FindingAlsoWrongPrefix => LiveOpsHubStringCatalog.Text(nameof(FindingAlsoWrongPrefix));
        internal static string FindingHoursFormat => LiveOpsHubStringCatalog.Text(nameof(FindingHoursFormat));

        // ----- Nút và link trên hàng -----
        internal static string FindingButtonSafeRepair => LiveOpsHubStringCatalog.Text(nameof(FindingButtonSafeRepair));
        internal static string FindingButtonProposal => LiveOpsHubStringCatalog.Text(nameof(FindingButtonProposal));
        internal static string FindingButtonDecision => LiveOpsHubStringCatalog.Text(nameof(FindingButtonDecision));
        internal static string FindingButtonIgnorable => LiveOpsHubStringCatalog.Text(nameof(FindingButtonIgnorable));
        internal static string FindingButtonViewDiff => LiveOpsHubStringCatalog.Text(nameof(FindingButtonViewDiff));
        internal static string FindingButtonDeclareType => LiveOpsHubStringCatalog.Text(nameof(FindingButtonDeclareType));
        internal static string FindingButtonPasteRunningJson => LiveOpsHubStringCatalog.Text(nameof(FindingButtonPasteRunningJson));
        internal static string FindingButtonCopyError => LiveOpsHubStringCatalog.Text(nameof(FindingButtonCopyError));
        internal static string FindingLinkViewInCalendar => LiveOpsHubStringCatalog.Text(nameof(FindingLinkViewInCalendar));
        internal static string FindingLinkOpenRule => LiveOpsHubStringCatalog.Text(nameof(FindingLinkOpenRule));
        internal static string FindingTooltipSafeRepair => LiveOpsHubStringCatalog.Text(nameof(FindingTooltipSafeRepair));
        internal static string FindingTooltipProposal => LiveOpsHubStringCatalog.Text(nameof(FindingTooltipProposal));
        internal static string FindingTooltipIgnorable => LiveOpsHubStringCatalog.Text(nameof(FindingTooltipIgnorable));

        /// <summary>{0} = tên file asset lịch đang mở ("Main.asset") — đúng chữ tooltip [SD2 §2.3 hàng 5] khi nơi gọi biết asset.</summary>
        internal static string FindingTooltipIgnorableInAssetFormat => LiveOpsHubStringCatalog.Text(nameof(FindingTooltipIgnorableInAssetFormat));
        internal static string FindingTooltipDecisionFormat => LiveOpsHubStringCatalog.Text(nameof(FindingTooltipDecisionFormat));

        // ----- Lựa chọn sửa (popover Đề xuất…, menu Quyết định…, xem trước Sửa hàng loạt) -----
        internal static string FindingRepairNormalizeFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRepairNormalizeFormat));
        internal static string FindingRepairKeepStartFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRepairKeepStartFormat));
        internal static string FindingRepairSwapFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRepairSwapFormat));
        internal static string FindingRepairRenameFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRepairRenameFormat));
        internal static string FindingRepairShiftStartFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRepairShiftStartFormat));
        internal static string FindingRepairShiftWholeFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRepairShiftWholeFormat));
        internal static string FindingRepairSetActiveFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRepairSetActiveFormat));
        internal static string FindingRepairRevertPrefixFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRepairRevertPrefixFormat));
        internal static string FindingRepairRevertPublished => LiveOpsHubStringCatalog.Text(nameof(FindingRepairRevertPublished));
        internal static string FindingRepairDeferFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRepairDeferFormat));
        internal static string FindingDecisionKeepPrefixFormat => LiveOpsHubStringCatalog.Text(nameof(FindingDecisionKeepPrefixFormat));
        internal static string FindingDecisionRevertPublished => LiveOpsHubStringCatalog.Text(nameof(FindingDecisionRevertPublished));
        internal static string FindingDecisionDeferFormat => LiveOpsHubStringCatalog.Text(nameof(FindingDecisionDeferFormat));

        // ----- Ghi chú bỏ qua / hẹn giờ (nhóm Đã bỏ qua, CC-VALB-4) -----
        internal static string FindingReminderUntilFormat => LiveOpsHubStringCatalog.Text(nameof(FindingReminderUntilFormat));
        internal static string FindingIgnoredFromFormat => LiveOpsHubStringCatalog.Text(nameof(FindingIgnoredFromFormat));
        internal static string FindingIgnoredEveryRange => LiveOpsHubStringCatalog.Text(nameof(FindingIgnoredEveryRange));
        internal static string FindingDueReminderTag => LiveOpsHubStringCatalog.Text(nameof(FindingDueReminderTag));

        // ----- Tên field định danh -----
        internal static string FindingIdentifierFieldId => LiveOpsHubStringCatalog.Text(nameof(FindingIdentifierFieldId));
        internal static string FindingIdentifierFieldType => LiveOpsHubStringCatalog.Text(nameof(FindingIdentifierFieldType));

        // ----- Luật 1 utc-time-format -----
        internal static string FindingUtcStartHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingUtcStartHeadlineFormat));
        internal static string FindingUtcEndHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingUtcEndHeadlineFormat));
        internal static string FindingUtcBothHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingUtcBothHeadlineFormat));
        internal static string FindingUtcAnchorHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingUtcAnchorHeadlineFormat));
        internal static string FindingUtcStartShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingUtcStartShortLabel));
        internal static string FindingUtcEndShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingUtcEndShortLabel));
        internal static string FindingUtcBothShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingUtcBothShortLabel));
        internal static string FindingAnchorShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingAnchorShortLabel));
        internal static string FindingDroppedEntryConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingDroppedEntryConsequenceFormat));

        // ----- Luật 2 end-before-start -----
        internal static string FindingEndBeforeStartHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingEndBeforeStartHeadlineFormat));
        internal static string FindingEndEqualsStartHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingEndEqualsStartHeadlineFormat));
        internal static string FindingEndBeforeStartShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingEndBeforeStartShortLabel));
        internal static string FindingEndEqualsStartShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingEndEqualsStartShortLabel));
        internal static string FindingEndAfterStartExpectedFormat => LiveOpsHubStringCatalog.Text(nameof(FindingEndAfterStartExpectedFormat));

        // ----- Luật 3 invalid-identifier -----
        internal static string FindingIdentifierEmptyHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingIdentifierEmptyHeadlineFormat));
        internal static string FindingIdentifierEmptyWithoutIdHeadline => LiveOpsHubStringCatalog.Text(nameof(FindingIdentifierEmptyWithoutIdHeadline));
        internal static string FindingIdentifierHashHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingIdentifierHashHeadlineFormat));
        internal static string FindingIdentifierNewlineHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingIdentifierNewlineHeadlineFormat));
        internal static string FindingIdentifierEmptyShortLabelFormat => LiveOpsHubStringCatalog.Text(nameof(FindingIdentifierEmptyShortLabelFormat));
        internal static string FindingIdentifierHashShortLabelFormat => LiveOpsHubStringCatalog.Text(nameof(FindingIdentifierHashShortLabelFormat));
        internal static string FindingIdentifierNewlineShortLabelFormat => LiveOpsHubStringCatalog.Text(nameof(FindingIdentifierNewlineShortLabelFormat));
        internal static string FindingIdentifierMeta => LiveOpsHubStringCatalog.Text(nameof(FindingIdentifierMeta));

        // ----- Luật 4 duplicate-event-id -----
        internal static string FindingDuplicateHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingDuplicateHeadlineFormat));
        internal static string FindingDuplicateShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingDuplicateShortLabel));
        internal static string FindingDuplicateSuggestionFormat => LiveOpsHubStringCatalog.Text(nameof(FindingDuplicateSuggestionFormat));
        internal static string FindingDuplicateConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingDuplicateConsequenceFormat));

        // ----- Luật 5 overlap-same-type -----
        internal static string FindingOverlapHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingOverlapHeadlineFormat));
        internal static string FindingOverlapWithoutRangeHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingOverlapWithoutRangeHeadlineFormat));
        internal static string FindingOverlapShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingOverlapShortLabel));
        internal static string FindingOverlapKeepsEarlier => LiveOpsHubStringCatalog.Text(nameof(FindingOverlapKeepsEarlier));
        internal static string FindingOverlapConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingOverlapConsequenceFormat));
        internal static string FindingOverlapWithoutAnchorConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingOverlapWithoutAnchorConsequenceFormat));
        internal static string FindingOverlapDetailFormat => LiveOpsHubStringCatalog.Text(nameof(FindingOverlapDetailFormat));

        // ----- Luật 6 recurring-rule-invalid -----
        internal static string FindingRecurringHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRecurringHeadlineFormat));
        internal static string FindingRecurringPeriodReason => LiveOpsHubStringCatalog.Text(nameof(FindingRecurringPeriodReason));
        internal static string FindingRecurringActiveReason => LiveOpsHubStringCatalog.Text(nameof(FindingRecurringActiveReason));
        internal static string FindingRecurringActiveLongerReason => LiveOpsHubStringCatalog.Text(nameof(FindingRecurringActiveLongerReason));
        internal static string FindingRecurringPrefixReason => LiveOpsHubStringCatalog.Text(nameof(FindingRecurringPrefixReason));
        internal static string FindingRecurringTypeReason => LiveOpsHubStringCatalog.Text(nameof(FindingRecurringTypeReason));
        internal static string FindingRecurringDuplicateTypeReason => LiveOpsHubStringCatalog.Text(nameof(FindingRecurringDuplicateTypeReason));
        internal static string FindingRecurringHoursMetaFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRecurringHoursMetaFormat));
        internal static string FindingRecurringActiveLongerMetaFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRecurringActiveLongerMetaFormat));
        internal static string FindingRecurringIdentifierMetaFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRecurringIdentifierMetaFormat));
        internal static string FindingRecurringDuplicateTypeMeta => LiveOpsHubStringCatalog.Text(nameof(FindingRecurringDuplicateTypeMeta));
        internal static string FindingRecurringConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRecurringConsequenceFormat));

        // ----- Luật 7 shadowed-by-recurring -----
        internal static string FindingShadowedOverlapHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingShadowedOverlapHeadlineFormat));
        internal static string FindingShadowedIdHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingShadowedIdHeadlineFormat));
        internal static string FindingShadowedOverlapShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingShadowedOverlapShortLabel));
        internal static string FindingShadowedIdShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingShadowedIdShortLabel));
        internal static string FindingShadowedOverlapMetaDetail => LiveOpsHubStringCatalog.Text(nameof(FindingShadowedOverlapMetaDetail));
        internal static string FindingShadowedIdMetaDetail => LiveOpsHubStringCatalog.Text(nameof(FindingShadowedIdMetaDetail));
        internal static string FindingShadowedConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingShadowedConsequenceFormat));

        // ----- Luật 8 unknown-event-type -----
        // (V-22 CC-FT-2 (a)) Không tiền tố "Kiểm lịch ·": tiền tố chỉ thuộc card tham chiếu màn Loại event, màn đó tự thêm.
        internal static string FindingUnknownTypeDraftHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingUnknownTypeDraftHeadlineFormat));
        internal static string FindingUnknownTypeRemoteHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingUnknownTypeRemoteHeadlineFormat));
        internal static string FindingUnknownTypeDraftShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingUnknownTypeDraftShortLabel));
        internal static string FindingUnknownTypeRemoteShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingUnknownTypeRemoteShortLabel));
        internal static string FindingUnknownTypeDraftMeta => LiveOpsHubStringCatalog.Text(nameof(FindingUnknownTypeDraftMeta));
        internal static string FindingRemoteSubjectMeta => LiveOpsHubStringCatalog.Text(nameof(FindingRemoteSubjectMeta));
        internal static string FindingUnknownTypeDraftConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingUnknownTypeDraftConsequenceFormat));
        internal static string FindingUnknownTypeRemoteConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingUnknownTypeRemoteConsequenceFormat));

        // ----- Luật 9 running-event-id-changed -----
        internal static string FindingRunningPrefixChangedHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningPrefixChangedHeadlineFormat));
        internal static string FindingRunningRemovedRecurringHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningRemovedRecurringHeadlineFormat));
        internal static string FindingRunningRemovedFixedHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningRemovedFixedHeadlineFormat));
        internal static string FindingRunningRetypedHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningRetypedHeadlineFormat));
        internal static string FindingRunningMovedOutFixedHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningMovedOutFixedHeadlineFormat));
        internal static string FindingRunningMovedOutRecurringHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningMovedOutRecurringHeadlineFormat));
        internal static string FindingRunningMovedEarlierFixedHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningMovedEarlierFixedHeadlineFormat));
        internal static string FindingRunningMovedEarlierRecurringHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningMovedEarlierRecurringHeadlineFormat));
        internal static string FindingRunningMovedOutWithoutTimeHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningMovedOutWithoutTimeHeadlineFormat));
        internal static string FindingRunningEndsNowFixedHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningEndsNowFixedHeadlineFormat));
        internal static string FindingRunningEndsNowRecurringHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningEndsNowRecurringHeadlineFormat));
        internal static string FindingRunningPrefixChangedShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingRunningPrefixChangedShortLabel));
        internal static string FindingRunningRemovedShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingRunningRemovedShortLabel));
        internal static string FindingRunningRetypedShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingRunningRetypedShortLabel));
        internal static string FindingRunningMovedOutShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingRunningMovedOutShortLabel));
        internal static string FindingRunningEndsNowShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingRunningEndsNowShortLabel));
        internal static string FindingRunningUntilFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningUntilFormat));
        internal static string FindingRunningNoOccurrenceNow => LiveOpsHubStringCatalog.Text(nameof(FindingRunningNoOccurrenceNow));
        internal static string FindingRunningNotFollowed => LiveOpsHubStringCatalog.Text(nameof(FindingRunningNotFollowed));
        internal static string FindingRunningRetypedDetailFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningRetypedDetailFormat));
        internal static string FindingRunningNewWindowFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningNewWindowFormat));
        internal static string FindingRunningNewEndFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningNewEndFormat));
        internal static string FindingRunningManualFixMeta => LiveOpsHubStringCatalog.Text(nameof(FindingRunningManualFixMeta));
        internal static string FindingRunningManualFixRecurringMeta => LiveOpsHubStringCatalog.Text(nameof(FindingRunningManualFixRecurringMeta));
        internal static string FindingRunningRestartConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningRestartConsequenceFormat));
        internal static string FindingRunningStopConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningStopConsequenceFormat));
        internal static string FindingRunningEndsNowConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningEndsNowConsequenceFormat));

        /// <summary>
        /// (V-21 CC-VALB-3, điều kiện user đặt) Luật 9 không có lệnh hoàn về nào giữ được đợt: câu phải chỉ đúng việc tay cần làm, không
        /// chỉ báo "không sửa tự động được". {0} = id đợt đang chạy, {1} = khung đang chạy.
        /// </summary>
        internal static string FindingRunningManualFixFixedFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningManualFixFixedFormat));

        /// <summary>{0} = id lần lặp đang chạy, {1} = loại của luật, {2} = khung đang chạy.</summary>
        internal static string FindingRunningManualFixRecurringFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRunningManualFixRecurringFormat));

        // ----- Luật 10 config-key-missing -----
        internal static string FindingConfigKeyFixedHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyFixedHeadlineFormat));
        internal static string FindingConfigKeyRecurringHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyRecurringHeadlineFormat));
        internal static string FindingConfigKeyEmptyFixedHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyEmptyFixedHeadlineFormat));
        internal static string FindingConfigKeyEmptyRecurringHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyEmptyRecurringHeadlineFormat));
        internal static string FindingConfigKeyShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyShortLabel));
        internal static string FindingConfigKeyEmptyShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyEmptyShortLabel));
        internal static string FindingConfigKeyUpcomingFormat => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyUpcomingFormat));
        internal static string FindingConfigKeyRunningFormat => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyRunningFormat));
        internal static string FindingConfigKeyStartsFormat => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyStartsFormat));
        internal static string FindingConfigKeyRecurringSubject => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyRecurringSubject));
        internal static string FindingConfigKeyPublishedUsesFormat => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyPublishedUsesFormat));
        internal static string FindingConfigKeyNotInPublished => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyNotInPublished));
        internal static string FindingConfigKeyEmptyMeta => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyEmptyMeta));
        internal static string FindingConfigKeyDiffersConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyDiffersConsequenceFormat));
        internal static string FindingConfigKeyNewConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyNewConsequenceFormat));
        internal static string FindingConfigKeyEmptyConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingConfigKeyEmptyConsequenceFormat));

        // ----- Luật 11 long-gap-between-events -----
        internal static string FindingLongGapHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingLongGapHeadlineFormat));
        internal static string FindingLongGapShortLabelFormat => LiveOpsHubStringCatalog.Text(nameof(FindingLongGapShortLabelFormat));
        internal static string FindingLongGapShortLabelWithoutRange => LiveOpsHubStringCatalog.Text(nameof(FindingLongGapShortLabelWithoutRange));
        internal static string FindingLongGapWithoutRangeHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingLongGapWithoutRangeHeadlineFormat));
        internal static string FindingLongGapMetaFormat => LiveOpsHubStringCatalog.Text(nameof(FindingLongGapMetaFormat));
        internal static string FindingLongGapConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingLongGapConsequenceFormat));

        // ----- Luật 12 remote-snapshot-drift -----
        // (V-22 CC-FT-2 (a)) "mục" chứ không "đợt": số đếm lấy từ ExpectedText gồm cả luật lặp.
        internal static string FindingRemoteDiffersHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRemoteDiffersHeadlineFormat));
        internal static string FindingRemoteDiffersWithoutStampHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRemoteDiffersWithoutStampHeadlineFormat));
        internal static string FindingRemoteNoStampHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRemoteNoStampHeadlineFormat));
        internal static string FindingRemoteDiffersShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingRemoteDiffersShortLabel));
        internal static string FindingRemoteNoStampShortLabel => LiveOpsHubStringCatalog.Text(nameof(FindingRemoteNoStampShortLabel));
        internal static string FindingRemoteDiffersMetaFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRemoteDiffersMetaFormat));
        internal static string FindingRemoteNoStampMeta => LiveOpsHubStringCatalog.Text(nameof(FindingRemoteNoStampMeta));
        internal static string FindingRemoteNotBlockingCopy => LiveOpsHubStringCatalog.Text(nameof(FindingRemoteNotBlockingCopy));
        internal static string FindingRemoteDiffersConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRemoteDiffersConsequenceFormat));
        internal static string FindingRemoteNoStampConsequence => LiveOpsHubStringCatalog.Text(nameof(FindingRemoteNoStampConsequence));

        // ----- Hàng luật không ra phát hiện (Chưa kiểm / Không áp dụng / luật ném) -----
        internal static string FindingRemoteNotMeasuredHeadline => LiveOpsHubStringCatalog.Text(nameof(FindingRemoteNotMeasuredHeadline));
        internal static string FindingRemoteNotPastedMeta => LiveOpsHubStringCatalog.Text(nameof(FindingRemoteNotPastedMeta));

        /// <summary>(V-21 CC-VALB-2) {0} = giờ đăng của dấu mới nhất.</summary>
        internal static string FindingLatestStampNotLoadedMetaFormat => LiveOpsHubStringCatalog.Text(nameof(FindingLatestStampNotLoadedMetaFormat));

        internal static string FindingLatestStampNotLoadedMeta => LiveOpsHubStringCatalog.Text(nameof(FindingLatestStampNotLoadedMeta));
        internal static string FindingRunningNotApplicableHeadline => LiveOpsHubStringCatalog.Text(nameof(FindingRunningNotApplicableHeadline));
        internal static string FindingNoPublishedStampMeta => LiveOpsHubStringCatalog.Text(nameof(FindingNoPublishedStampMeta));

        /// <summary>(PD-9) Nhãn của luật 9 trong card "Đã qua" khi chưa có dấu đã đăng — Không áp dụng, không đếm vào đã qua.</summary>
        internal static string FindingRunningNotApplicableLabel => LiveOpsHubStringCatalog.Text(nameof(FindingRunningNotApplicableLabel));

        internal static string FindingRuleNotApplicableLabelFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRuleNotApplicableLabelFormat));
        internal static string FindingRuleFailedHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRuleFailedHeadlineFormat));
        internal static string FindingRuleFailedMetaFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRuleFailedMetaFormat));
        internal static string FindingRuleNotMeasuredHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRuleNotMeasuredHeadlineFormat));
        internal static string FindingRuleNotMeasuredMetaFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRuleNotMeasuredMetaFormat));
        internal static string FindingRuleNotApplicableHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRuleNotApplicableHeadlineFormat));
        internal static string FindingRuleNotApplicableMetaFormat => LiveOpsHubStringCatalog.Text(nameof(FindingRuleNotApplicableMetaFormat));

        // ----- VÌ SAO (pane Chi tiết) -----
        internal static string FindingWhyUtcTimeFormat => LiveOpsHubStringCatalog.Text(nameof(FindingWhyUtcTimeFormat));
        internal static string FindingWhyEndBeforeStart => LiveOpsHubStringCatalog.Text(nameof(FindingWhyEndBeforeStart));
        internal static string FindingWhyInvalidIdentifier => LiveOpsHubStringCatalog.Text(nameof(FindingWhyInvalidIdentifier));
        internal static string FindingWhyDuplicateEventId => LiveOpsHubStringCatalog.Text(nameof(FindingWhyDuplicateEventId));
        internal static string FindingWhyOverlapSameType => LiveOpsHubStringCatalog.Text(nameof(FindingWhyOverlapSameType));
        internal static string FindingWhyRecurringRuleInvalid => LiveOpsHubStringCatalog.Text(nameof(FindingWhyRecurringRuleInvalid));
        internal static string FindingWhyShadowedByRecurring => LiveOpsHubStringCatalog.Text(nameof(FindingWhyShadowedByRecurring));
        internal static string FindingWhyUnknownEventType => LiveOpsHubStringCatalog.Text(nameof(FindingWhyUnknownEventType));
        internal static string FindingWhyRunningEventIdChanged => LiveOpsHubStringCatalog.Text(nameof(FindingWhyRunningEventIdChanged));
        internal static string FindingWhyConfigKeyMissing => LiveOpsHubStringCatalog.Text(nameof(FindingWhyConfigKeyMissing));
        internal static string FindingWhyLongGapBetweenEvents => LiveOpsHubStringCatalog.Text(nameof(FindingWhyLongGapBetweenEvents));
        internal static string FindingWhyRemoteSnapshotDrift => LiveOpsHubStringCatalog.Text(nameof(FindingWhyRemoteSnapshotDrift));

        // ----- Lý do bỏ mục (AlsoWrongText V-7, hàng diff Bị bỏ) -----
        internal static string FindingReasonEndNotAfterStart => LiveOpsHubStringCatalog.Text(nameof(FindingReasonEndNotAfterStart));
        internal static string FindingReasonInvalidRecurringRule => LiveOpsHubStringCatalog.Text(nameof(FindingReasonInvalidRecurringRule));

        // ----- Hàng diff (LiveOpsChangeText) -----
        internal static string ChangeAddedRowFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeAddedRowFormat));
        internal static string ChangeRemovedRowFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeRemovedRowFormat));
        internal static string ChangeRecurringSubjectFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeRecurringSubjectFormat));
        internal static string ChangeFieldFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldFormat));
        internal static string ChangeDroppedByOtherRowFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeDroppedByOtherRowFormat));
        internal static string ChangeReturnedByOtherRowFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeReturnedByOtherRowFormat));
        internal static string ChangeEndedRenameRowFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeEndedRenameRowFormat));
        internal static string ChangeFieldLabelId => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldLabelId));
        internal static string ChangeFieldLabelType => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldLabelType));
        internal static string ChangeFieldLabelStart => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldLabelStart));
        internal static string ChangeFieldLabelEnd => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldLabelEnd));
        internal static string ChangeFieldLabelAnchor => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldLabelAnchor));
        internal static string ChangeFieldLabelIdPrefix => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldLabelIdPrefix));
        internal static string ChangeFieldLabelPeriod => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldLabelPeriod));
        internal static string ChangeFieldLabelActive => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldLabelActive));
        internal static string ChangeFieldLabelDisplayName => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldLabelDisplayName));
        internal static string ChangeFieldLabelColorSlot => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldLabelColorSlot));
        internal static string ChangeFieldLabelRequiresJoin => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldLabelRequiresJoin));
        internal static string ChangeFieldLabelDefaultConfigKey => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldLabelDefaultConfigKey));
        internal static string ChangeFieldLabelOrder => LiveOpsHubStringCatalog.Text(nameof(ChangeFieldLabelOrder));
        internal static string ChangeClauseJoin => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseJoin));
        internal static string ChangeDroppedConsequence => LiveOpsHubStringCatalog.Text(nameof(ChangeDroppedConsequence));
        internal static string ChangeDroppedByOtherConsequence => LiveOpsHubStringCatalog.Text(nameof(ChangeDroppedByOtherConsequence));
        internal static string ChangeByOtherSuffix => LiveOpsHubStringCatalog.Text(nameof(ChangeByOtherSuffix));
        internal static string ChangeReturnedConsequence => LiveOpsHubStringCatalog.Text(nameof(ChangeReturnedConsequence));
        internal static string ChangeNobodyLoses => LiveOpsHubStringCatalog.Text(nameof(ChangeNobodyLoses));
        internal static string ChangeUpcomingSafe => LiveOpsHubStringCatalog.Text(nameof(ChangeUpcomingSafe));
        internal static string ChangeRunningSafe => LiveOpsHubStringCatalog.Text(nameof(ChangeRunningSafe));
        internal static string ChangeEndedSafe => LiveOpsHubStringCatalog.Text(nameof(ChangeEndedSafe));
        internal static string ChangeAddedSafe => LiveOpsHubStringCatalog.Text(nameof(ChangeAddedSafe));
        internal static string ChangeTypeDefinitionSafe => LiveOpsHubStringCatalog.Text(nameof(ChangeTypeDefinitionSafe));
        internal static string ChangeUpcomingPrefix => LiveOpsHubStringCatalog.Text(nameof(ChangeUpcomingPrefix));
        internal static string ChangeEndedPrefix => LiveOpsHubStringCatalog.Text(nameof(ChangeEndedPrefix));
        internal static string ChangeNotRunningPrefix => LiveOpsHubStringCatalog.Text(nameof(ChangeNotRunningPrefix));
        internal static string ChangeRunningReviewFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeRunningReviewFormat));
        internal static string ChangeClauseRemoved => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseRemoved));
        internal static string ChangeClauseRuleRemoved => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseRuleRemoved));
        internal static string ChangeClauseNewIdFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseNewIdFormat));
        internal static string ChangeClauseNewTypeFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseNewTypeFormat));
        internal static string ChangeClauseConfigKeyFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseConfigKeyFormat));
        internal static string ChangeClauseInheritedConfigKey => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseInheritedConfigKey));
        internal static string ChangeClauseRecurringIdsShift => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseRecurringIdsShift));
        internal static string ChangeClauseReusedEndedId => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseReusedEndedId));
        internal static string ChangeClauseReopenedEndedId => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseReopenedEndedId));
        internal static string ChangeClauseRequiresJoinOn => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseRequiresJoinOn));
        internal static string ChangeClauseRequiresJoinOff => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseRequiresJoinOff));
        internal static string ChangeClauseShortened => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseShortened));
        internal static string ChangeClauseStartAfterNow => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseStartAfterNow));
        internal static string ChangeClauseRunningConfigKeyFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseRunningConfigKeyFormat));
        internal static string ChangeClauseActiveHours => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseActiveHours));
        internal static string ChangeClauseWindowOrKey => LiveOpsHubStringCatalog.Text(nameof(ChangeClauseWindowOrKey));
        internal static string ChangeEndedRenameConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeEndedRenameConsequenceFormat));
        internal static string ChangedTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(ChangedTooltipFormat));
        internal static string ChangeTooltipAdded => LiveOpsHubStringCatalog.Text(nameof(ChangeTooltipAdded));
        internal static string ChangeTooltipRemoved => LiveOpsHubStringCatalog.Text(nameof(ChangeTooltipRemoved));
        internal static string ChangeTooltipDroppedByOther => LiveOpsHubStringCatalog.Text(nameof(ChangeTooltipDroppedByOther));
        internal static string ChangeTooltipReturnedByOther => LiveOpsHubStringCatalog.Text(nameof(ChangeTooltipReturnedByOther));
        internal static string ChangeTooltipEndedRenameFormat => LiveOpsHubStringCatalog.Text(nameof(ChangeTooltipEndedRenameFormat));
    }
}
