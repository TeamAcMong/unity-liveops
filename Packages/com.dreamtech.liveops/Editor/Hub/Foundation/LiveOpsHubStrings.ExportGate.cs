namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng ExportGate (G-EXPORTGATE): năm dòng cổng xuất, dạng gọn (PD-11), lý do cạnh nút và tooltip ba nút, card lỗi (h),
    /// dòng parser lệch (V-6), note (i)/(j), badge health màn Xuất và cột "chặn Copy JSON" của Tổng quan. Microcopy nguyên văn
    /// theo [SD2 §3.2], [SD2 §3.4], [SD2 §3.10]. Mọi hằng mang tiền tố <c>ExportGate</c> vì lớp <c>partial</c> dùng chung với mọi
    /// vùng khác; câu có số là chuỗi định dạng <c>{0}</c> để nơi gọi chèn số đã qua <see cref="LiveOpsHubFormat"/>.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Card cổng — meta header [SD2 §3.4].
        internal static string ExportGateCardMetaBlocked => LiveOpsHubStringCatalog.Text(nameof(ExportGateCardMetaBlocked));
        internal static string ExportGateCardMetaReady => LiveOpsHubStringCatalog.Text(nameof(ExportGateCardMetaReady));

        // Dòng 1 — Không còn đợt bị bỏ.
        internal static string ExportGateDroppedOk => LiveOpsHubStringCatalog.Text(nameof(ExportGateDroppedOk));
        internal static string ExportGateDroppedBlockedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateDroppedBlockedFormat));
        internal static string ExportGateDroppedNeverChecked => LiveOpsHubStringCatalog.Text(nameof(ExportGateDroppedNeverChecked));
        internal static string ExportGateDroppedRunning => LiveOpsHubStringCatalog.Text(nameof(ExportGateDroppedRunning));
        internal static string ExportGateDroppedStale => LiveOpsHubStringCatalog.Text(nameof(ExportGateDroppedStale));
        internal static string ExportGateDroppedStaleMetaFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateDroppedStaleMetaFormat));

        // Dòng 2 — Parser của game đọc lại. Lý do bỏ gom theo nhóm như meta Hình 18 "1 không đọc được · 1 bị bỏ vì chồng giờ".
        internal static string ExportGateReadBackOkFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReadBackOkFormat));
        // Bản "bộ biên dịch" dùng khi Kiểm lịch cũ/chưa chạy hoặc số Bị bỏ của Kiểm lịch khác số bộ biên dịch bỏ (luật 8, phát hiện
        // đã bỏ qua) — nói "Kiểm lịch" lúc đó là mâu thuẫn với dòng 1.
        internal static string ExportGateReadBackOkCompilerFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReadBackOkCompilerFormat));
        internal static string ExportGateReadBackMismatchKeptCompilerFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReadBackMismatchKeptCompilerFormat));
        internal static string ExportGateReadBackMismatchSameCountCompilerFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReadBackMismatchSameCountCompilerFormat));
        internal static string ExportGateReadBackMismatchEntryCountCompilerFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReadBackMismatchEntryCountCompilerFormat));
        internal static string ExportGateReadBackOkNarrowFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReadBackOkNarrowFormat));
        internal static string ExportGateReadBackMismatchKeptFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReadBackMismatchKeptFormat));
        internal static string ExportGateReadBackMismatchSameCountFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReadBackMismatchSameCountFormat));
        internal static string ExportGateReadBackMismatchEntryCountFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReadBackMismatchEntryCountFormat));
        internal static string ExportGateReadBackMismatchNarrow => LiveOpsHubStringCatalog.Text(nameof(ExportGateReadBackMismatchNarrow));
        internal static string ExportGateReadBackMismatchMetaFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReadBackMismatchMetaFormat));
        internal static string ExportGateReadBackFailed => LiveOpsHubStringCatalog.Text(nameof(ExportGateReadBackFailed));
        internal static string ExportGateReadBackFailedNarrow => LiveOpsHubStringCatalog.Text(nameof(ExportGateReadBackFailedNarrow));
        internal static string ExportGateEntryKeptFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateEntryKeptFormat));
        internal static string ExportGateEntryDroppedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateEntryDroppedFormat));
        internal static string ExportGateEntryAbsent => LiveOpsHubStringCatalog.Text(nameof(ExportGateEntryAbsent));
        internal static string ExportGateEntryNameFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateEntryNameFormat));
        internal static string ExportGateReasonCountFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReasonCountFormat));
        internal static string ExportGateCopyErrorAction => LiveOpsHubStringCatalog.Text(nameof(ExportGateCopyErrorAction));

        internal static string ExportGateDropReasonUnreadable => LiveOpsHubStringCatalog.Text(nameof(ExportGateDropReasonUnreadable));
        internal static string ExportGateDropReasonInvalidIdentifier => LiveOpsHubStringCatalog.Text(nameof(ExportGateDropReasonInvalidIdentifier));
        internal static string ExportGateDropReasonEndNotAfterStart => LiveOpsHubStringCatalog.Text(nameof(ExportGateDropReasonEndNotAfterStart));
        internal static string ExportGateDropReasonDuplicateEventId => LiveOpsHubStringCatalog.Text(nameof(ExportGateDropReasonDuplicateEventId));
        internal static string ExportGateDropReasonOverlapsSameType => LiveOpsHubStringCatalog.Text(nameof(ExportGateDropReasonOverlapsSameType));
        internal static string ExportGateDropReasonShadowedByRecurring => LiveOpsHubStringCatalog.Text(nameof(ExportGateDropReasonShadowedByRecurring));
        internal static string ExportGateDropReasonInvalidRecurringRule => LiveOpsHubStringCatalog.Text(nameof(ExportGateDropReasonInvalidRecurringRule));
        internal static string ExportGateDropReasonDuplicateRecurringType => LiveOpsHubStringCatalog.Text(nameof(ExportGateDropReasonDuplicateRecurringType));

        // Bản chép khi bấm "Copy lỗi" ở dòng parser lệch — người dùng dán cho dev, nên nêu sha và từng vị trí lệch.
        internal static string ExportGateMismatchReportHeaderFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateMismatchReportHeaderFormat));
        internal static string ExportGateMismatchReportCountsFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateMismatchReportCountsFormat));

        // Dòng 3 — Kiểm lịch chạy sau lần sửa cuối.
        internal static string ExportGateFreshnessOkFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateFreshnessOkFormat));
        internal static string ExportGateFreshnessStaleChangedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateFreshnessStaleChangedFormat));
        internal static string ExportGateFreshnessStaleMilestoneFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateFreshnessStaleMilestoneFormat));
        // Không rõ vì sao cũ (PD-23: có thể do domain reload, không phải sửa) thì không khẳng định "lịch đã đổi".
        internal static string ExportGateFreshnessStaleFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateFreshnessStaleFormat));
        internal static string ExportGateFreshnessStaleNarrow => LiveOpsHubStringCatalog.Text(nameof(ExportGateFreshnessStaleNarrow));
        internal static string ExportGateFreshnessNeverChecked => LiveOpsHubStringCatalog.Text(nameof(ExportGateFreshnessNeverChecked));
        internal static string ExportGateFreshnessRunningFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateFreshnessRunningFormat));
        internal static string ExportGateFreshnessRunningMeta => LiveOpsHubStringCatalog.Text(nameof(ExportGateFreshnessRunningMeta));
        internal static string ExportGateStartCheckAction => LiveOpsHubStringCatalog.Text(nameof(ExportGateStartCheckAction));

        // Dòng 4 — Bản remote: không bao giờ chặn [SD2 §3.4].
        internal static string ExportGateRemoteNotPasted => LiveOpsHubStringCatalog.Text(nameof(ExportGateRemoteNotPasted));
        internal static string ExportGateRemoteNotPastedNarrow => LiveOpsHubStringCatalog.Text(nameof(ExportGateRemoteNotPastedNarrow));
        internal static string ExportGateRemoteNotVerifiedAfterMark => LiveOpsHubStringCatalog.Text(nameof(ExportGateRemoteNotVerifiedAfterMark));
        internal static string ExportGateRemoteMatchesFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateRemoteMatchesFormat));
        internal static string ExportGateRemoteDiffersFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateRemoteDiffersFormat));
        internal static string ExportGateRemoteVerifiedSuffixFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateRemoteVerifiedSuffixFormat));
        internal static string ExportGateRemoteWithoutStamp => LiveOpsHubStringCatalog.Text(nameof(ExportGateRemoteWithoutStamp));
        internal static string ExportGateRemotePasteAction => LiveOpsHubStringCatalog.Text(nameof(ExportGateRemotePasteAction));
        internal static string ExportGateRemoteCompareAction => LiveOpsHubStringCatalog.Text(nameof(ExportGateRemoteCompareAction));
        internal static string ExportGateRemoteStillUnchecked => LiveOpsHubStringCatalog.Text(nameof(ExportGateRemoteStillUnchecked));

        // Dòng 5 — Đã xem mục bắt buộc.
        internal static string ExportGateReviewedOkFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReviewedOkFormat));
        internal static string ExportGateReviewedNothingRequired => LiveOpsHubStringCatalog.Text(nameof(ExportGateReviewedNothingRequired));
        internal static string ExportGateReviewedBlockedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReviewedBlockedFormat));
        internal static string ExportGateReviewedItemsSuffixFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReviewedItemsSuffixFormat));
        internal static string ExportGateReviewedDiffMissing => LiveOpsHubStringCatalog.Text(nameof(ExportGateReviewedDiffMissing));
        internal static string ExportGateItemFieldFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateItemFieldFormat));
        internal static string ExportGateListSeparator => LiveOpsHubStringCatalog.Text(nameof(ExportGateListSeparator));

        // Nhãn field trong ngoặc của dòng 5 và tooltip Copy ("weekly-pass tiền tố") — chỉ những field người dùng sửa được.
        internal static string ExportGateFieldIdPrefix => LiveOpsHubStringCatalog.Text(nameof(ExportGateFieldIdPrefix));
        internal static string ExportGateFieldAnchor => LiveOpsHubStringCatalog.Text(nameof(ExportGateFieldAnchor));
        internal static string ExportGateFieldPeriod => LiveOpsHubStringCatalog.Text(nameof(ExportGateFieldPeriod));
        internal static string ExportGateFieldActiveDuration => LiveOpsHubStringCatalog.Text(nameof(ExportGateFieldActiveDuration));
        internal static string ExportGateFieldStart => LiveOpsHubStringCatalog.Text(nameof(ExportGateFieldStart));
        internal static string ExportGateFieldEnd => LiveOpsHubStringCatalog.Text(nameof(ExportGateFieldEnd));
        internal static string ExportGateFieldType => LiveOpsHubStringCatalog.Text(nameof(ExportGateFieldType));

        // Dạng gọn (PD-11): chỉ đếm dòng có thể chặn.
        internal static string ExportGateCompactFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateCompactFormat));

        // Lý do cạnh nút [SD2 §3.2].
        internal static string ExportGateReasonBlockedPrefix => LiveOpsHubStringCatalog.Text(nameof(ExportGateReasonBlockedPrefix));
        internal static string ExportGateReasonPartSeparator => LiveOpsHubStringCatalog.Text(nameof(ExportGateReasonPartSeparator));
        internal static string ExportGateReasonReadBackFailed => LiveOpsHubStringCatalog.Text(nameof(ExportGateReasonReadBackFailed));
        internal static string ExportGateReasonReadBackMismatch => LiveOpsHubStringCatalog.Text(nameof(ExportGateReasonReadBackMismatch));
        internal static string ExportGateReasonNeverChecked => LiveOpsHubStringCatalog.Text(nameof(ExportGateReasonNeverChecked));
        internal static string ExportGateReasonStale => LiveOpsHubStringCatalog.Text(nameof(ExportGateReasonStale));
        internal static string ExportGateReasonRunning => LiveOpsHubStringCatalog.Text(nameof(ExportGateReasonRunning));
        internal static string ExportGateReasonDroppedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReasonDroppedFormat));
        internal static string ExportGateReasonUnreviewedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateReasonUnreviewedFormat));
        internal static string ExportGateReasonDiffMissing => LiveOpsHubStringCatalog.Text(nameof(ExportGateReasonDiffMissing));
        internal static string ExportGateReasonMissingForMark => LiveOpsHubStringCatalog.Text(nameof(ExportGateReasonMissingForMark));
        internal static string ExportGateReasonNoChanges => LiveOpsHubStringCatalog.Text(nameof(ExportGateReasonNoChanges));

        // Tooltip ba nút — đặt trên slot vì nút disabled không nhận hover [SD2 §3.2].
        internal static string ExportGateCopyTooltipBlockedPrefix => LiveOpsHubStringCatalog.Text(nameof(ExportGateCopyTooltipBlockedPrefix));
        internal static string ExportGateCopyTooltipLastSeparator => LiveOpsHubStringCatalog.Text(nameof(ExportGateCopyTooltipLastSeparator));
        internal static string ExportGateCopyTooltipDroppedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateCopyTooltipDroppedFormat));
        internal static string ExportGateCopyTooltipUnreviewedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateCopyTooltipUnreviewedFormat));
        internal static string ExportGateCopyTooltipReadBackFailed => LiveOpsHubStringCatalog.Text(nameof(ExportGateCopyTooltipReadBackFailed));
        internal static string ExportGateCopyTooltipReadBackMismatch => LiveOpsHubStringCatalog.Text(nameof(ExportGateCopyTooltipReadBackMismatch));
        internal static string ExportGateCopyTooltipNeverChecked => LiveOpsHubStringCatalog.Text(nameof(ExportGateCopyTooltipNeverChecked));
        internal static string ExportGateCopyTooltipStale => LiveOpsHubStringCatalog.Text(nameof(ExportGateCopyTooltipStale));
        internal static string ExportGateCopyTooltipRunning => LiveOpsHubStringCatalog.Text(nameof(ExportGateCopyTooltipRunning));
        internal static string ExportGateCopyTooltipDiffMissing => LiveOpsHubStringCatalog.Text(nameof(ExportGateCopyTooltipDiffMissing));
        internal static string ExportGateCopyTooltipReadyFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateCopyTooltipReadyFormat));
        internal static string ExportGateSaveTooltipBlocked => LiveOpsHubStringCatalog.Text(nameof(ExportGateSaveTooltipBlocked));
        internal static string ExportGateSaveTooltipReadyFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateSaveTooltipReadyFormat));
        internal static string ExportGateMarkTooltipNotExported => LiveOpsHubStringCatalog.Text(nameof(ExportGateMarkTooltipNotExported));
        internal static string ExportGateMarkTooltipDraftChangedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateMarkTooltipDraftChangedFormat));
        internal static string ExportGateMarkTooltipReadBackUnconfirmed => LiveOpsHubStringCatalog.Text(nameof(ExportGateMarkTooltipReadBackUnconfirmed));
        internal static string ExportGateMarkTooltipReadyFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateMarkTooltipReadyFormat));

        // (h) card lỗi đầu thân [SD2 §3.10].
        internal static string ExportGateFailureSymptom => LiveOpsHubStringCatalog.Text(nameof(ExportGateFailureSymptom));
        internal static string ExportGateFailurePositionFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateFailurePositionFormat));
        internal static string ExportGateFailureOpenLineFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateFailureOpenLineFormat));
        internal static string ExportGateFailureNote => LiveOpsHubStringCatalog.Text(nameof(ExportGateFailureNote));
        internal static string ExportGateFailureReportHeaderFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateFailureReportHeaderFormat));

        // Thân Xuất: diff trống (f)/(g), note (i), HelpBox (j).
        internal static string ExportGateNoChangesFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateNoChangesFormat));
        internal static string ExportGateSameCalendarDifferentJsonFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateSameCalendarDifferentJsonFormat));
        internal static string ExportGateFirstPublish => LiveOpsHubStringCatalog.Text(nameof(ExportGateFirstPublish));
        internal static string ExportGateRestoringFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateRestoringFormat));
        internal static string ExportGateUndoRestoreAction => LiveOpsHubStringCatalog.Text(nameof(ExportGateUndoRestoreAction));
        internal static string ExportGateFormat1NoticeFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateFormat1NoticeFormat));

        // Health màn Xuất (6.4) và cột Tổng quan [SD1 §1.1].
        internal static string ExportGateHealthBlockedBadge => LiveOpsHubStringCatalog.Text(nameof(ExportGateHealthBlockedBadge));
        internal static string ExportGateHealthNeedsReviewBadgeFormat => LiveOpsHubStringCatalog.Text(nameof(ExportGateHealthNeedsReviewBadgeFormat));
        internal static string ExportGateCopyBlockedColumn => LiveOpsHubStringCatalog.Text(nameof(ExportGateCopyBlockedColumn));

        // Lỗi lập trình của model (ArgumentException) — không bao giờ do dữ liệu lịch hỏng.
        internal static string ExportGateErrorJsonMissing => LiveOpsHubStringCatalog.Text(nameof(ExportGateErrorJsonMissing));
        internal static string ExportGateErrorCompilationMissing => LiveOpsHubStringCatalog.Text(nameof(ExportGateErrorCompilationMissing));
        internal static string ExportGateErrorFormatMissing => LiveOpsHubStringCatalog.Text(nameof(ExportGateErrorFormatMissing));
        internal static string ExportGateErrorReadBackMissing => LiveOpsHubStringCatalog.Text(nameof(ExportGateErrorReadBackMissing));
        internal static string ExportGateErrorInputMissing => LiveOpsHubStringCatalog.Text(nameof(ExportGateErrorInputMissing));
        internal static string ExportGateErrorNegativeCount => LiveOpsHubStringCatalog.Text(nameof(ExportGateErrorNegativeCount));
    }
}
