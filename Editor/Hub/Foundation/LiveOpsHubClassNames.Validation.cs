namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class USS riêng của vùng Validation (V-5, G-VALIDATION): màn Kiểm lịch (mục 7.5, [SD2 §2]). Chỉ khai những thành phần
    /// KHÔNG có ở bảng class khung (8.10) — hàng phát hiện dùng lại <see cref="LiveOpsHubClassNames.FindingRow"/> và ba sọc
    /// mức độ của khung, card dùng <see cref="LiveOpsHubClassNames.Card"/>.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        // Khung màn: thân, toolbar lọc, dải summary.
        /// <summary>Ẩn/hiện của màn đi qua CLASS chứ không qua <c>style.display</c> ([FD §2.14]: trạng thái luôn bằng class).</summary>
        internal const string ValidationHidden = "liveops-hub-validation-hidden";

        internal const string ValidationPage = "liveops-hub-validation-page";
        internal const string ValidationToolbar = "liveops-hub-validation-toolbar";
        internal const string ValidationToolbarSpacer = "liveops-hub-validation-toolbar-spacer";

        /// <summary>Vùng bọc ToolbarMenu "Loại event: …" — khe 6px sau nhóm tab ([SD2 §2.1]) nằm ở đây, không ở từng tab.</summary>
        internal const string ValidationTypeMenu = "liveops-hub-validation-type-menu";

        internal const string ValidationSearch = "liveops-hub-validation-search";

        /// <summary>Icon search 10px trong ô tìm ([SD2 §2.1]): ô không có nhãn nên icon là thứ duy nhất nói ô để làm gì.</summary>
        internal const string ValidationSearchIcon = "liveops-hub-validation-search-icon";
        /// <summary>
        /// Nút "[Refresh] Kiểm lại" của section header (W9-06). Button LÀ một TextElement: thêm một Image con thì nút không
        /// còn tự đo theo <c>text</c> nữa, nó co về <c>min-width: 54px</c> và chữ bị cắt ở MỌI cỡ cửa sổ. Class này xếp nút
        /// thành hàng ngang để icon và Label chữ đứng cạnh nhau — cùng cách đã dùng cho nút "Kiểm lại tất cả" của Tổng quan.
        /// <para>
        /// Hậu tố <c>Row</c> là CỐ Ý (soát W9 R-12): không có nó thì hằng TÊN CLASS này trùng tên với hằng CHỮ HIỂN THỊ
        /// <c>LiveOpsHubStrings.ValidationRecheckButton</c>, và hai thứ đó đứng cách nhau bảy dòng trong
        /// <c>ValidationSection.cs</c> — đọc lướt không phân biệt được cái nào là class, cái nào là câu chữ.
        /// </para>
        /// </summary>
        internal const string ValidationRecheckButtonRow = "liveops-hub-validation-recheck-button-row";

        /// <summary>Label chữ của nút "Kiểm lại" — xem <see cref="ValidationRecheckButtonRow"/>.</summary>
        internal const string ValidationRecheckButtonLabel = "liveops-hub-validation-recheck-button-label";

        internal const string ValidationSummary = "liveops-hub-validation-summary";
        internal const string ValidationSummaryItem = "liveops-hub-validation-summary-item";
        internal const string ValidationSummaryRight = "liveops-hub-validation-summary-right";

        /// <summary>Hộp spinner + thanh tiến trình của trạng thái đang kiểm ([SD2 §2.8]); thanh tự vẽ vì ProgressBar của Unity không đủ mỏng.</summary>
        internal const string ValidationProgress = "liveops-hub-validation-progress";
        internal const string ValidationProgressTrack = "liveops-hub-validation-progress-track";
        internal const string ValidationProgressFill = "liveops-hub-validation-progress-fill";
        internal const string ValidationProgressLabel = "liveops-hub-validation-progress-label";

        // Thân chia hai: cột card nhóm bên trái, pane Chi tiết 300px bên phải (sibling NGOÀI split để thành drawer ở --medium).
        internal const string ValidationContent = "liveops-hub-validation-content";
        internal const string ValidationGroups = "liveops-hub-validation-groups";
        internal const string ValidationCardsRow = "liveops-hub-validation-cards-row";

        // Card nhóm theo hậu quả.
        internal const string ValidationGroupCard = "liveops-hub-validation-group";
        internal const string ValidationGroupTitle = "liveops-hub-validation-group-title";
        internal const string ValidationGroupMeta = "liveops-hub-validation-group-meta";
        internal const string ValidationGroupBlurb = "liveops-hub-validation-group-blurb";
        internal const string ValidationGroupRows = "liveops-hub-validation-group-rows";
        internal const string ValidationGroupDropped = "liveops-hub-validation-group--dropped";
        internal const string ValidationGroupProgressLost = "liveops-hub-validation-group--progress-lost";
        internal const string ValidationGroupShouldReview = "liveops-hub-validation-group--should-review";
        internal const string ValidationGroupNotMeasured = "liveops-hub-validation-group--not-measured";
        internal const string ValidationGroupPassed = "liveops-hub-validation-group--passed";
        internal const string ValidationGroupIgnored = "liveops-hub-validation-group--ignored";

        /// <summary>Dòng mono 9px liệt kê id luật đã qua bên trong card thu gọn.</summary>
        internal const string ValidationPassedIds = "liveops-hub-validation-passed-ids";

        /// <summary>Tag "cũ" trên header card khi kết quả không còn là bằng chứng (PD-23).</summary>
        internal const string ValidationStaleTag = "liveops-hub-validation-stale-tag";

        // Hàng phát hiện: bốn phần của anatomy [SD2 §2.2].
        internal const string ValidationRowStripe = "liveops-hub-validation-row-stripe";
        internal const string ValidationRowIcon = "liveops-hub-validation-row-icon";
        internal const string ValidationRowText = "liveops-hub-validation-row-text";
        internal const string ValidationRowHeadline = "liveops-hub-validation-row-headline";
        internal const string ValidationRowMeta = "liveops-hub-validation-row-meta";
        internal const string ValidationRowRuleId = "liveops-hub-validation-row-rule-id";
        internal const string ValidationRowActions = "liveops-hub-validation-row-actions";
        internal const string ValidationRowLink = "liveops-hub-validation-row-link";
        internal const string ValidationRowManualFix = "liveops-hub-validation-row-manual-fix";
        internal const string ValidationRowStale = "liveops-hub-validation-row--stale";
        internal const string ValidationRowSelected = "liveops-hub-validation-row--selected";

        // Pane Chi tiết: key–value rồi ba khối chữ, cuối là link tài liệu luật.
        internal const string ValidationDetail = "liveops-hub-validation-detail";
        internal const string ValidationDetailDrawer = "liveops-hub-validation-detail--drawer";
        internal const string ValidationDetailTitle = "liveops-hub-validation-detail-title";
        internal const string ValidationDetailClose = "liveops-hub-validation-detail-close";
        internal const string ValidationDetailHeading = "liveops-hub-validation-detail-heading";
        internal const string ValidationDetailParagraph = "liveops-hub-validation-detail-paragraph";
        internal const string ValidationDetailFixButton = "liveops-hub-validation-detail-fix-button";
        internal const string ValidationDetailHelpRow = "liveops-hub-validation-detail-help-row";

        // Popover Đề xuất… ([SD2 §2.6]) — PopupWindow 320px, dùng lại ở inspector Lịch (G-CALENDAR-DEPTH).
        internal const string ValidationProposal = "liveops-hub-validation-proposal";
        internal const string ValidationProposalHeader = "liveops-hub-validation-proposal-header";
        internal const string ValidationProposalOption = "liveops-hub-validation-proposal-option";
        internal const string ValidationProposalOptionDetail = "liveops-hub-validation-proposal-option-detail";
        internal const string ValidationProposalQuickCheck = "liveops-hub-validation-proposal-quick-check";
        internal const string ValidationProposalConsequence = "liveops-hub-validation-proposal-consequence";
        internal const string ValidationProposalFooter = "liveops-hub-validation-proposal-footer";
        internal const string ValidationProposalHint = "liveops-hub-validation-proposal-hint";
    }
}
