namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Validation (G-VALIDATION, mục 7.5): màn Kiểm lịch — nhãn tab lọc, dải summary, header card theo hậu quả,
    /// bốn trạng thái của thân màn và popover Đề xuất…. Câu của TỪNG phát hiện KHÔNG nằm ở đây: chúng đến từ
    /// <see cref="LiveOpsFindingText"/> (V-8) để một phát hiện chỉ có một câu trong cả hub. Câu hai ngôn ngữ ở
    /// <c>Language/LiveOpsHubStringCatalog.Validation.cs</c>; comment "vì sao" ở lại đây cạnh property.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        /// <summary>UXML của màn thiếu trên đĩa = package hỏng: card lỗi của khung nêu đúng đường dẫn (8.1 bước 2).</summary>
        internal static string ValidationMissingLayoutFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationMissingLayoutFormat));

        // Trạng thái chung "không có asset" (mục 7 luật chung): mọi màn trừ Tổng quan nói cùng một câu và chỉ về Tổng quan.
        internal static string ValidationNoAssetTitle => LiveOpsHubStringCatalog.Text(nameof(ValidationNoAssetTitle));
        internal static string ValidationNoAssetBody => LiveOpsHubStringCatalog.Text(nameof(ValidationNoAssetBody));
        internal static string ValidationNoAssetActionButton => LiveOpsHubStringCatalog.Text(nameof(ValidationNoAssetActionButton));

        // Nút section header: "Sửa các lỗi an toàn (n)…" rồi nút chính "Kiểm lại".
        internal static string ValidationSafeRepairButtonFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationSafeRepairButtonFormat));
        internal static string ValidationSafeRepairNothingReason => LiveOpsHubStringCatalog.Text(nameof(ValidationSafeRepairNothingReason));

        internal static string ValidationRecheckButton => LiveOpsHubStringCatalog.Text(nameof(ValidationRecheckButton));

        /// <summary>Nút Kiểm lại lúc đang chạy: đổi chữ chứ không chỉ disabled, để biết vì sao bấm không được.</summary>
        internal static string ValidationRecheckRunningButton => LiveOpsHubStringCatalog.Text(nameof(ValidationRecheckRunningButton));

        // Tab lọc [SD2 §2.1] — số đếm đến từ cùng bộ tổng hợp với rail và Tổng quan ([SD2 §1.3]).
        internal static string ValidationTabAllFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationTabAllFormat));
        internal static string ValidationTabDroppedFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationTabDroppedFormat));
        internal static string ValidationTabProgressLostFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationTabProgressLostFormat));
        internal static string ValidationTabShouldReviewFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationTabShouldReviewFormat));
        internal static string ValidationTabNotMeasuredFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationTabNotMeasuredFormat));

        // Lọc theo loại event + ô tìm.
        internal static string ValidationTypeMenuAll => LiveOpsHubStringCatalog.Text(nameof(ValidationTypeMenuAll));
        internal static string ValidationTypeMenuFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationTypeMenuFormat));
        internal static string ValidationTypeMenuAllItem => LiveOpsHubStringCatalog.Text(nameof(ValidationTypeMenuAllItem));
        internal static string ValidationSearchPlaceholder => LiveOpsHubStringCatalog.Text(nameof(ValidationSearchPlaceholder));

        // Dải summary: năm cặp dấu + số, rồi câu phải nói lần kiểm.
        internal static string ValidationSummaryDroppedFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationSummaryDroppedFormat));
        internal static string ValidationSummaryProgressLostFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationSummaryProgressLostFormat));
        internal static string ValidationSummaryShouldReviewFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationSummaryShouldReviewFormat));
        internal static string ValidationSummaryNotMeasuredFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationSummaryNotMeasuredFormat));
        internal static string ValidationSummaryPassedFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationSummaryPassedFormat));
        internal static string ValidationSummaryRightFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationSummaryRightFormat));

        // Header card theo hậu quả [SD2 §1.1] — blurb nói hậu quả với NGƯỜI CHƠI, không nói tên luật.
        internal static string ValidationGroupDroppedTitle => LiveOpsHubStringCatalog.Text(nameof(ValidationGroupDroppedTitle));
        internal static string ValidationGroupDroppedBlurb => LiveOpsHubStringCatalog.Text(nameof(ValidationGroupDroppedBlurb));
        internal static string ValidationGroupProgressLostTitle => LiveOpsHubStringCatalog.Text(nameof(ValidationGroupProgressLostTitle));
        internal static string ValidationGroupProgressLostBlurb => LiveOpsHubStringCatalog.Text(nameof(ValidationGroupProgressLostBlurb));
        internal static string ValidationGroupShouldReviewTitle => LiveOpsHubStringCatalog.Text(nameof(ValidationGroupShouldReviewTitle));
        internal static string ValidationGroupNotMeasuredTitle => LiveOpsHubStringCatalog.Text(nameof(ValidationGroupNotMeasuredTitle));
        internal static string ValidationGroupPassedTitleFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationGroupPassedTitleFormat));
        internal static string ValidationGroupIgnoredTitleFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationGroupIgnoredTitleFormat));

        // Meta header card: nhóm phát hiện nói "sửa nhanh được k/n" khi có lệnh sửa an toàn, nhóm Chưa kiểm nói rõ không phải "đã qua".
        internal static string ValidationGroupMetaFindingsFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationGroupMetaFindingsFormat));
        internal static string ValidationGroupMetaSafeRepairFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationGroupMetaSafeRepairFormat));
        internal static string ValidationGroupMetaNotMeasuredFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationGroupMetaNotMeasuredFormat));

        /// <summary>Tag "cũ" trên header card + hàng mờ đi: người đọc phải thấy ngay đây là số của bản trước (PD-23).</summary>
        internal static string ValidationStaleTag => LiveOpsHubStringCatalog.Text(nameof(ValidationStaleTag));

        // HelpBox "kết quả cũ": hai lý do khác nhau nên hai câu khác nhau — lịch đổi, hoặc thời gian đã qua một mốc.
        internal static string ValidationStaleEditedFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationStaleEditedFormat));
        internal static string ValidationStaleMilestoneFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationStaleMilestoneFormat));

        /// <summary>Kiểm dở dang mất vì Unity nạp lại script (R-25) — nói thẳng lý do thay vì im lặng hiện "chưa kiểm".</summary>
        internal static string ValidationStaleInterruptedNotice => LiveOpsHubStringCatalog.Text(nameof(ValidationStaleInterruptedNotice));

        // Trạng thái chưa kiểm lần nào: trống KHÔNG phải đạt, nên câu nói rõ điều đó rồi mới tới nút.
        internal static string ValidationNeverCheckedTitle => LiveOpsHubStringCatalog.Text(nameof(ValidationNeverCheckedTitle));
        internal static string ValidationNeverCheckedBodyFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationNeverCheckedBodyFormat));
        internal static string ValidationNeverCheckedActionFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationNeverCheckedActionFormat));

        // Đang kiểm: chữ trong hộp + nhãn đọc được của thanh tiến trình tự vẽ.
        internal static string ValidationProgressFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProgressFormat));
        internal static string ValidationProgressRatioFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProgressRatioFormat));

        // Không còn lỗi: tiêu đề nói hết Bị bỏ / Mất tiến độ, thân nhắc "chưa kiểm không tính là đã qua".
        internal static string ValidationNoErrorsTitle => LiveOpsHubStringCatalog.Text(nameof(ValidationNoErrorsTitle));
        internal static string ValidationNoErrorsBodyFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationNoErrorsBodyFormat));

        /// <summary>Còn Nên xem mà hết Bị bỏ/Mất tiến độ ([SD2 §4 mục 23]): không dùng empty, chỉ thêm một dòng note Ok trên đầu.</summary>
        internal static string ValidationShouldReviewOnlyNote => LiveOpsHubStringCatalog.Text(nameof(ValidationShouldReviewOnlyNote));

        // Toast sau khi áp một lệnh sửa an toàn — cũng là tên Undo group (luật toast chung [SD2 §2.6]).
        internal static string ValidationSafeRepairUndoFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationSafeRepairUndoFormat));

        // Pane Chi tiết: key–value rồi ba khối chữ theo đúng thứ tự "người chơi trước, vì sao sau".
        internal static string ValidationDetailTitle => LiveOpsHubStringCatalog.Text(nameof(ValidationDetailTitle));
        internal static string ValidationDetailCloseTooltip => LiveOpsHubStringCatalog.Text(nameof(ValidationDetailCloseTooltip));
        internal static string ValidationDetailEmpty => LiveOpsHubStringCatalog.Text(nameof(ValidationDetailEmpty));
        internal static string ValidationDetailEventLabel => LiveOpsHubStringCatalog.Text(nameof(ValidationDetailEventLabel));
        internal static string ValidationDetailTypeLabel => LiveOpsHubStringCatalog.Text(nameof(ValidationDetailTypeLabel));
        internal static string ValidationDetailRuleLabel => LiveOpsHubStringCatalog.Text(nameof(ValidationDetailRuleLabel));
        internal static string ValidationDetailWhatHappensHeading => LiveOpsHubStringCatalog.Text(nameof(ValidationDetailWhatHappensHeading));
        internal static string ValidationDetailWhyHeading => LiveOpsHubStringCatalog.Text(nameof(ValidationDetailWhyHeading));
        internal static string ValidationDetailHowToFixHeading => LiveOpsHubStringCatalog.Text(nameof(ValidationDetailHowToFixHeading));
        internal static string ValidationDetailDocumentationLink => LiveOpsHubStringCatalog.Text(nameof(ValidationDetailDocumentationLink));

        // Popover Đề xuất… [SD2 §2.6]: Enter là Quay lại, "Áp" chỉ chạy khi click — chữ phải nói rõ điều đó.
        internal static string ValidationProposalHeaderFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalHeaderFormat));
        internal static string ValidationProposalQuickCheckOk => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalQuickCheckOk));
        internal static string ValidationProposalQuickCheckOverlapFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalQuickCheckOverlapFormat));
        internal static string ValidationProposalEnterHint => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalEnterHint));
        internal static string ValidationProposalApplyFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalApplyFormat));

        /// <summary>
        /// Nút áp của hai cách sửa DỜI đợt ([SD2 §2.6] "Áp: dời hunt-0916-bonus"): nút nêu ĐỘNG TỪ của lựa chọn đang chọn, không
        /// chỉ id — bấm "Áp: hunt-0916-bonus" thì người bấm không biết mình sắp dời hay sắp xoá. Cách sửa chưa được thiết kế đặt
        /// tên động từ vẫn dùng khuôn trung tính <see cref="ValidationProposalApplyFormat"/>.
        /// </summary>
        internal static string ValidationProposalApplyShiftFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalApplyShiftFormat));

        /// <summary>
        /// (V-34, nợ Findings từ cổng W4) Bảy khuôn còn lại của nút áp — một cho mỗi <c>RepairId</c> mà hub biết. Mockup
        /// [SD2 §2.6] đã đặt "đổi" cho <c>rename-with-suggested-id</c>; sáu khuôn dưới nó thiết kế CHƯA đặt tên động từ, gói viết
        /// tạm cả hai ngôn ngữ để nút chạy được và liệt kê nguyên văn vào báo cáo gói cho user chốt ở cổng W5.
        /// </summary>
        internal static string ValidationProposalApplyRenameFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalApplyRenameFormat));

        internal static string ValidationProposalApplyNormalizeFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalApplyNormalizeFormat));
        internal static string ValidationProposalApplySetDurationFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalApplySetDurationFormat));
        internal static string ValidationProposalApplySwapFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalApplySwapFormat));
        internal static string ValidationProposalApplySetActiveFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalApplySetActiveFormat));
        internal static string ValidationProposalApplyRevertFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalApplyRevertFormat));
        internal static string ValidationProposalApplyDeferFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalApplyDeferFormat));

        /// <summary>
        /// Dòng 10px dưới mỗi lựa chọn ([SD2 §2.6] "bắt đầu 16/9 12:00 → 17/9 00:00 · dài 24 giờ"): giờ NGƯỜI ĐỌC hiểu, không
        /// phải chuỗi ISO thô của <c>LiveEventCalendarRepair.BeforeText/AfterText</c> (core để thô có chủ đích — câu là việc của Editor).
        /// </summary>
        internal static string ValidationProposalOptionDetailFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalOptionDetailFormat));

        /// <summary>
        /// Câu hậu quả của popover ([SD2 §2.6] "Đợt sẽ xuất hiện với người chơi từ 17/9 00:00 UTC."): nói kết quả SAU KHI ÁP của
        /// đúng lựa chọn đang chọn. Câu hậu quả của phát hiện (<c>LiveOpsFindingText.ConsequenceSentence</c>) nói chuyện khác —
        /// chuyện đang xảy ra nếu KHÔNG sửa — nên không thay thế được và không đổi theo lựa chọn.
        /// </summary>
        internal static string ValidationProposalConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalConsequenceFormat));
        internal static string ValidationProposalBackButton => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalBackButton));

        /// <summary>Toast + tên Undo group sau khi Áp một đề xuất: nêu id và cách sửa đã chọn (luật toast chung [SD2 §2.6]).</summary>
        internal static string ValidationProposalAppliedFormat => LiveOpsHubStringCatalog.Text(nameof(ValidationProposalAppliedFormat));
    }
}
