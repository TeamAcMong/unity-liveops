namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Export (G-EXPORT, W4): tiêu đề ba card của màn Xuất JSON, bốn metric, card diff theo hậu quả, card Các lần đã
    /// đăng, hai hộp xác nhận cấp 1 (khôi phục vào nháp · gỡ dấu), outcome sau Copy / Lưu file / Ghi dấu và toàn bộ hộp
    /// <c>MarkPublishedWindow</c>. Microcopy nguyên văn theo [SD2 §3.2], [SD2 §3.5], [SD2 §3.8], [SD2 §3.9], [SD2 §3.11].
    /// <para>
    /// Năm dòng cổng, lý do cạnh nút, tooltip ba nút, card lỗi (h), note (i) và HelpBox (j) KHÔNG nằm ở đây: chúng thuộc vùng
    /// ExportGate của G-EXPORTGATE (V-9) và màn chỉ vẽ lại <see cref="ExportGateState"/>. Câu của một thay đổi trong card diff
    /// cũng không nằm ở đây — <c>LiveOpsChangeText</c> (G-FINDINGTEXT, V-8) là nguồn duy nhất.
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // ---------------------------------------------------------------------------------------------- khung màn + header
        internal static string ExportMissingLayoutFormat => LiveOpsHubStringCatalog.Text(nameof(ExportMissingLayoutFormat));
        internal static string ExportNoAssetTitle => LiveOpsHubStringCatalog.Text(nameof(ExportNoAssetTitle));
        internal static string ExportNoAssetBody => LiveOpsHubStringCatalog.Text(nameof(ExportNoAssetBody));
        internal static string ExportNoAssetActionButton => LiveOpsHubStringCatalog.Text(nameof(ExportNoAssetActionButton));

        internal static string ExportCopyButton => LiveOpsHubStringCatalog.Text(nameof(ExportCopyButton));
        internal static string ExportSaveFileButton => LiveOpsHubStringCatalog.Text(nameof(ExportSaveFileButton));
        internal static string ExportMarkPublishedButton => LiveOpsHubStringCatalog.Text(nameof(ExportMarkPublishedButton));

        /// <summary>Chân màn [SD2 §3.3] — D5 nói thẳng ra màn hình: hub không gọi Firebase.</summary>
        internal static string ExportFooterNote => LiveOpsHubStringCatalog.Text(nameof(ExportFooterNote));

        // ---------------------------------------------------------------------------------------------------------- card
        internal static string ExportGateCardTitle => LiveOpsHubStringCatalog.Text(nameof(ExportGateCardTitle));
        internal static string ExportJsonCardTitle => LiveOpsHubStringCatalog.Text(nameof(ExportJsonCardTitle));

        /// <summary>Meta card JSON khi parser đọc lại khớp; lệch/thất bại thì lấy thẳng chữ dòng cổng 2 (không viết câu thứ hai).</summary>
        internal static string ExportJsonCardMetaFormat => LiveOpsHubStringCatalog.Text(nameof(ExportJsonCardMetaFormat));

        // ------------------------------------------------------------------------------------------------------ card diff
        internal static string ExportDiffCardTitleFormat => LiveOpsHubStringCatalog.Text(nameof(ExportDiffCardTitleFormat));

        /// <summary>(V-13) Nguồn bản so là JSON đang chạy đã dán — header nói rõ "dán lúc" để không ai tưởng đó là dấu đã đăng.</summary>
        internal static string ExportDiffCardTitleRemoteFormat => LiveOpsHubStringCatalog.Text(nameof(ExportDiffCardTitleRemoteFormat));
        internal static string ExportDiffCardTitleNoStamp => LiveOpsHubStringCatalog.Text(nameof(ExportDiffCardTitleNoStamp));
        internal static string ExportDiffMetaFormat => LiveOpsHubStringCatalog.Text(nameof(ExportDiffMetaFormat));
        internal static string ExportDiffChipAddedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportDiffChipAddedFormat));
        internal static string ExportDiffChipChangedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportDiffChipChangedFormat));
        internal static string ExportDiffChipRemovedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportDiffChipRemovedFormat));
        internal static string ExportDiffChipKeptFormat => LiveOpsHubStringCatalog.Text(nameof(ExportDiffChipKeptFormat));

        internal static string ExportDiffGroupDroppedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportDiffGroupDroppedFormat));
        internal static string ExportDiffGroupProgressLostFormat => LiveOpsHubStringCatalog.Text(nameof(ExportDiffGroupProgressLostFormat));
        internal static string ExportDiffGroupShouldReviewFormat => LiveOpsHubStringCatalog.Text(nameof(ExportDiffGroupShouldReviewFormat));
        internal static string ExportDiffGroupSafeFormat => LiveOpsHubStringCatalog.Text(nameof(ExportDiffGroupSafeFormat));

        internal static string ExportDiffReviewedToggle => LiveOpsHubStringCatalog.Text(nameof(ExportDiffReviewedToggle));
        internal static string ExportDiffRequiredTag => LiveOpsHubStringCatalog.Text(nameof(ExportDiffRequiredTag));

        /// <summary>Hàng Bị bỏ KHÔNG có Toggle "Đã xem" — tick không mở khoá gì, mục phải được sửa [SD2 §3.8].</summary>
        internal static string ExportDiffFixInValidationLink => LiveOpsHubStringCatalog.Text(nameof(ExportDiffFixInValidationLink));

        /// <summary>Ký hiệu mono đầu hàng diff — dịch ra vẫn y hệt nên khai chung một bản.</summary>
        internal static string ExportDiffSymbolAdded => LiveOpsHubStringCatalog.Text(nameof(ExportDiffSymbolAdded));
        internal static string ExportDiffSymbolChanged => LiveOpsHubStringCatalog.Text(nameof(ExportDiffSymbolChanged));
        internal static string ExportDiffSymbolProgressLost => LiveOpsHubStringCatalog.Text(nameof(ExportDiffSymbolProgressLost));

        /// <summary>(V-13) Chip trả nguồn bản so về dấu đã đăng khi đang so với bản remote.</summary>
        internal static string ExportCompareBackToPublishedChip => LiveOpsHubStringCatalog.Text(nameof(ExportCompareBackToPublishedChip));

        // --------------------------------------------------------------------------------------- card Các lần đã đăng
        internal static string ExportHistoryCardTitle => LiveOpsHubStringCatalog.Text(nameof(ExportHistoryCardTitle));
        internal static string ExportHistoryCardMeta => LiveOpsHubStringCatalog.Text(nameof(ExportHistoryCardMeta));
        internal static string ExportHistoryColumnTime => LiveOpsHubStringCatalog.Text(nameof(ExportHistoryColumnTime));
        internal static string ExportHistoryColumnPublisher => LiveOpsHubStringCatalog.Text(nameof(ExportHistoryColumnPublisher));
        internal static string ExportHistoryColumnNote => LiveOpsHubStringCatalog.Text(nameof(ExportHistoryColumnNote));
        internal static string ExportHistoryColumnSha => LiveOpsHubStringCatalog.Text(nameof(ExportHistoryColumnSha));

        /// <summary>Đuôi ghi chú của hàng Active: viền trái 3px nói "đang là bản so", chữ nói lại cho người đọc bằng chữ [SD2 §3.9].</summary>
        internal static string ExportHistoryActiveSuffix => LiveOpsHubStringCatalog.Text(nameof(ExportHistoryActiveSuffix));
        internal static string ExportHistoryEmpty => LiveOpsHubStringCatalog.Text(nameof(ExportHistoryEmpty));
        internal static string ExportHistoryNoNote => LiveOpsHubStringCatalog.Text(nameof(ExportHistoryNoNote));
        internal static string ExportHistoryMenuCompare => LiveOpsHubStringCatalog.Text(nameof(ExportHistoryMenuCompare));
        internal static string ExportHistoryMenuRestore => LiveOpsHubStringCatalog.Text(nameof(ExportHistoryMenuRestore));
        internal static string ExportHistoryMenuRemoveStamp => LiveOpsHubStringCatalog.Text(nameof(ExportHistoryMenuRemoveStamp));
        internal static string ExportHistoryCompareToastFormat => LiveOpsHubStringCatalog.Text(nameof(ExportHistoryCompareToastFormat));

        // -------------------------------------------------------------- hộp cấp 1: khôi phục vào nháp · gỡ dấu đã đăng
        internal static string ExportRestoreConfirmTitleFormat => LiveOpsHubStringCatalog.Text(nameof(ExportRestoreConfirmTitleFormat));
        internal static string ExportRestoreConfirmBodyFormat => LiveOpsHubStringCatalog.Text(nameof(ExportRestoreConfirmBodyFormat));
        internal static string ExportRestoreConfirmBodyNoChanges => LiveOpsHubStringCatalog.Text(nameof(ExportRestoreConfirmBodyNoChanges));

        /// <summary>Góc trái hộp: Enter và Esc CÙNG là hành động an toàn (SPIKE-B SP-2, quyết định an toàn 16/9/2026).</summary>
        internal static string ExportRestoreConfirmKeyHint => LiveOpsHubStringCatalog.Text(nameof(ExportRestoreConfirmKeyHint));
        internal static string ExportRestoreConfirmDestructive => LiveOpsHubStringCatalog.Text(nameof(ExportRestoreConfirmDestructive));
        internal static string ExportRestoreConfirmSafe => LiveOpsHubStringCatalog.Text(nameof(ExportRestoreConfirmSafe));
        internal static string ExportRestoreUndoNameFormat => LiveOpsHubStringCatalog.Text(nameof(ExportRestoreUndoNameFormat));

        internal static string ExportRemoveStampConfirmTitleFormat => LiveOpsHubStringCatalog.Text(nameof(ExportRemoveStampConfirmTitleFormat));
        internal static string ExportRemoveStampConfirmBodyFormat => LiveOpsHubStringCatalog.Text(nameof(ExportRemoveStampConfirmBodyFormat));
        internal static string ExportRemoveStampConfirmBodyNoBaselineFormat => LiveOpsHubStringCatalog.Text(nameof(ExportRemoveStampConfirmBodyNoBaselineFormat));
        internal static string ExportRemoveStampConfirmDestructive => LiveOpsHubStringCatalog.Text(nameof(ExportRemoveStampConfirmDestructive));
        internal static string ExportRemoveStampConfirmSafe => LiveOpsHubStringCatalog.Text(nameof(ExportRemoveStampConfirmSafe));
        internal static string ExportRemoveStampToastFormat => LiveOpsHubStringCatalog.Text(nameof(ExportRemoveStampToastFormat));

        // -------------------------------------------------------------------------------------------------------- metric
        internal static string ExportMetricSizeCaption => LiveOpsHubStringCatalog.Text(nameof(ExportMetricSizeCaption));
        internal static string ExportMetricSizeUnit => LiveOpsHubStringCatalog.Text(nameof(ExportMetricSizeUnit));
        internal static string ExportMetricSizeFootFormat => LiveOpsHubStringCatalog.Text(nameof(ExportMetricSizeFootFormat));
        internal static string ExportMetricFormatCaption => LiveOpsHubStringCatalog.Text(nameof(ExportMetricFormatCaption));
        internal static string ExportMetricFormatUnitVersion2 => LiveOpsHubStringCatalog.Text(nameof(ExportMetricFormatUnitVersion2));
        internal static string ExportMetricFormatUnitVersion1 => LiveOpsHubStringCatalog.Text(nameof(ExportMetricFormatUnitVersion1));
        internal static string ExportMetricFormatChoice1 => LiveOpsHubStringCatalog.Text(nameof(ExportMetricFormatChoice1));
        internal static string ExportMetricFormatChoice2 => LiveOpsHubStringCatalog.Text(nameof(ExportMetricFormatChoice2));
        internal static string ExportMetricShaCaption => LiveOpsHubStringCatalog.Text(nameof(ExportMetricShaCaption));
        internal static string ExportMetricCompareCaption => LiveOpsHubStringCatalog.Text(nameof(ExportMetricCompareCaption));
        internal static string ExportMetricCompareNone => LiveOpsHubStringCatalog.Text(nameof(ExportMetricCompareNone));
        internal static string ExportMetricCompareFootEmpty => LiveOpsHubStringCatalog.Text(nameof(ExportMetricCompareFootEmpty));

        // ------------------------------------------------------------------------------------------------------- outcome
        internal static string ExportOutcomeCopiedHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(ExportOutcomeCopiedHeadlineFormat));
        internal static string ExportOutcomeCopiedDetail => LiveOpsHubStringCatalog.Text(nameof(ExportOutcomeCopiedDetail));
        internal static string ExportOutcomeSavedHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(ExportOutcomeSavedHeadlineFormat));
        internal static string ExportOutcomeSavedDetail => LiveOpsHubStringCatalog.Text(nameof(ExportOutcomeSavedDetail));
        internal static string ExportOutcomeMarkedHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(ExportOutcomeMarkedHeadlineFormat));
        internal static string ExportOutcomeMarkedDetailFormat => LiveOpsHubStringCatalog.Text(nameof(ExportOutcomeMarkedDetailFormat));
        internal static string ExportSaveFilePanelTitle => LiveOpsHubStringCatalog.Text(nameof(ExportSaveFilePanelTitle));

        // ------------------------------------------------------------------------------- hộp Đánh dấu đã đăng [SD2 §3.11]
        internal static string ExportMarkWindowTitle => LiveOpsHubStringCatalog.Text(nameof(ExportMarkWindowTitle));
        internal static string ExportMarkHeadingReadyFormat => LiveOpsHubStringCatalog.Text(nameof(ExportMarkHeadingReadyFormat));

        /// <summary>Nháp đã đổi sau lần copy: heading nêu GIỜ COPY, không mời ghi sha mới [SD2 §3.11].</summary>
        internal static string ExportMarkHeadingDraftChangedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportMarkHeadingDraftChangedFormat));
        internal static string ExportMarkBodyFormat => LiveOpsHubStringCatalog.Text(nameof(ExportMarkBodyFormat));
        internal static string ExportMarkBodyDraftChangedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportMarkBodyDraftChangedFormat));
        internal static string ExportMarkShaOkFormat => LiveOpsHubStringCatalog.Text(nameof(ExportMarkShaOkFormat));
        internal static string ExportMarkShaBlockedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportMarkShaBlockedFormat));
        internal static string ExportMarkShaCopiedFormat => LiveOpsHubStringCatalog.Text(nameof(ExportMarkShaCopiedFormat));
        internal static string ExportMarkCopyNewButtonFormat => LiveOpsHubStringCatalog.Text(nameof(ExportMarkCopyNewButtonFormat));
        internal static string ExportMarkConfirmToggle => LiveOpsHubStringCatalog.Text(nameof(ExportMarkConfirmToggle));
        internal static string ExportMarkNoteLabel => LiveOpsHubStringCatalog.Text(nameof(ExportMarkNoteLabel));
        internal static string ExportMarkNotePlaceholder => LiveOpsHubStringCatalog.Text(nameof(ExportMarkNotePlaceholder));
        internal static string ExportMarkCancelButton => LiveOpsHubStringCatalog.Text(nameof(ExportMarkCancelButton));
        internal static string ExportMarkConfirmButton => LiveOpsHubStringCatalog.Text(nameof(ExportMarkConfirmButton));

        /// <summary>"Còn thiếu: " + danh sách phần còn thiếu nối bằng ", " [SD2 §3.11] — lý do in THÀNH CHỮ cạnh nút (SP-3).</summary>
        internal static string ExportMarkMissingPrefix => LiveOpsHubStringCatalog.Text(nameof(ExportMarkMissingPrefix));
        internal static string ExportMarkMissingSeparator => LiveOpsHubStringCatalog.Text(nameof(ExportMarkMissingSeparator));

        /// <summary>Dấu nối danh sách id mục trong hộp Khôi phục — khoá riêng, không mượn dấu nối của câu "Còn thiếu…".</summary>
        internal static string ExportChangedItemSeparator => LiveOpsHubStringCatalog.Text(nameof(ExportChangedItemSeparator));
        internal static string ExportMarkMissingConfirm => LiveOpsHubStringCatalog.Text(nameof(ExportMarkMissingConfirm));
        internal static string ExportMarkMissingNote => LiveOpsHubStringCatalog.Text(nameof(ExportMarkMissingNote));
        internal static string ExportMarkMissingCopy => LiveOpsHubStringCatalog.Text(nameof(ExportMarkMissingCopy));

        // Lỗi lập trình (ArgumentException) — không bao giờ do dữ liệu lịch hỏng.
        internal static string ExportErrorServicesMissing => LiveOpsHubStringCatalog.Text(nameof(ExportErrorServicesMissing));
    }
}
