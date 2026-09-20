namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class riêng của màn Tổng quan (V-5): tiền tố <c>liveops-hub-overview-…</c> cho thân màn, hàng việc cần làm, nút tầng của
    /// "Đường đi của lịch", bảng 7 ngày tới và step marker của empty (a). Thành phần dùng chung (card, metric, hàng phát hiện,
    /// dấu trạng thái, tag) lấy từ bảng 8.10 ở <c>LiveOpsHubClassNames.cs</c> — chỉ những gì THẬT SỰ mới của màn mới khai ở đây.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        // Thân màn
        internal const string OverviewPage = "liveops-hub-overview-page";
        internal const string OverviewMetricRow = "liveops-hub-overview-metric-row";

        /// <summary>Khối thân mặc định (4 card) — ẩn nguyên khối khi chưa có asset, để empty (a) thay CẢ thân [SD1 §1.2].</summary>
        internal const string OverviewDefaultBody = "liveops-hub-overview-default";

        /// <summary>
        /// ScrollView DỌC bọc thân mặc định (Q-W5-1, user chốt 17/9/2026). Có ở MỌI bề rộng, không gắn theo breakpoint: đổi
        /// hành vi theo bề rộng là đẻ hai bố cục phải chụp và phải đo riêng.
        /// </summary>
        internal const string OverviewScroll = "liveops-hub-overview-scroll";

        /// <summary>Khoảng giãn trong card header, đẩy tab "Nháp | Bản đã đăng" sang phải.</summary>
        internal const string OverviewCardSpacer = "liveops-hub-overview-card-spacer";

        /// <summary>Khối bị ẩn hoàn toàn (empty (a) hoặc thân mặc định) — display:none, không chỉ trong suốt.</summary>
        internal const string OverviewHidden = "liveops-hub-overview-hidden";

        // (b) nhiều asset lịch [SD1 §1.3] — HelpBox info ở đầu thân, item căn giữa dọc, nút "Đổi…" ở mép phải.
        internal const string OverviewMultipleAssets = "liveops-hub-overview-multiple-assets";
        internal const string OverviewMultipleAssetsText = "liveops-hub-overview-multiple-assets-text";

        /// <summary>Metric cuối hàng: bỏ margin-right (USS không có <c>:last-child</c>, class bật từ C#).</summary>
        internal const string OverviewMetricLast = "liveops-hub-overview-metric--last";

        internal const string OverviewMetricValueRow = "liveops-hub-overview-metric-value-row";
        internal const string OverviewMetricFootRow = "liveops-hub-overview-metric-foot-row";
        internal const string OverviewCardTitle = "liveops-hub-overview-card-title";
        internal const string OverviewCardSubtitle = "liveops-hub-overview-card-subtitle";
        internal const string OverviewCardBody = "liveops-hub-overview-card-body";

        // Hàng việc cần làm — cột 96px "chặn Copy JSON" + nút 150px [SD1 §1.1]
        internal const string OverviewNeedRow = "liveops-hub-overview-need-row";
        internal const string OverviewNeedText = "liveops-hub-overview-need-text";
        internal const string OverviewNeedTitle = "liveops-hub-overview-need-title";
        internal const string OverviewNeedDetail = "liveops-hub-overview-need-detail";
        internal const string OverviewNeedBlocks = "liveops-hub-overview-need-blocks";
        internal const string OverviewNeedButton = "liveops-hub-overview-need-button";

        // Đường đi của lịch — thân 48px, 4 nút tầng P1 chia đều
        internal const string OverviewFlow = "liveops-hub-overview-flow";
        internal const string OverviewFlowNode = "liveops-hub-overview-flow-node";
        internal const string OverviewFlowCaption = "liveops-hub-overview-flow-caption";
        internal const string OverviewFlowNote = "liveops-hub-overview-flow-note";
        internal const string OverviewFlowConnector = "liveops-hub-overview-flow-connector";

        /// <summary>Đường nối sau tầng cổng chặn đầu tiên: "chết" (opacity 0,12) thay vì sống (0,35).</summary>
        internal const string OverviewFlowConnectorDead = "liveops-hub-overview-flow-connector--dead";

        // Bảng 7 ngày tới — cột 170 / 250 / 140 / 130 / giãn
        internal const string OverviewUpcomingTable = "liveops-hub-overview-upcoming-table";
        internal const string OverviewUpcomingHeadRow = "liveops-hub-overview-upcoming-head-row";
        internal const string OverviewUpcomingRow = "liveops-hub-overview-upcoming-row";
        internal const string OverviewUpcomingCell = "liveops-hub-overview-upcoming-cell";
        internal const string OverviewUpcomingCellTime = "liveops-hub-overview-upcoming-cell--time";
        internal const string OverviewUpcomingCellEvent = "liveops-hub-overview-upcoming-cell--event";
        internal const string OverviewUpcomingCellType = "liveops-hub-overview-upcoming-cell--type";
        internal const string OverviewUpcomingCellKind = "liveops-hub-overview-upcoming-cell--kind";
        internal const string OverviewUpcomingCellNote = "liveops-hub-overview-upcoming-cell--note";
        internal const string OverviewUpcomingRelative = "liveops-hub-overview-upcoming-relative";

        // Empty (a) — danh sách ba bước, marker 18×18 viền 1,5px, số dùng MÀU CHỮ (không màu nền) [SD1 §1.2]
        internal const string OverviewStepList = "liveops-hub-overview-step-list";
        internal const string OverviewStepRow = "liveops-hub-overview-step-row";
        internal const string OverviewStepMarker = "liveops-hub-overview-step-marker";
        internal const string OverviewStepLabel = "liveops-hub-overview-step-label";
        internal const string OverviewEmptyActions = "liveops-hub-overview-empty-actions";

        // Nút header "[Refresh] Kiểm lại tất cả" (7.1): Button có con thì KHÔNG tự đo theo `text` nữa, nên chữ phải là một
        // Label con và nút phải xếp hàng ngang — nếu không nút co về min-width 54px và icon đè lên chữ.
        /// <summary>
        /// Nút chỉ có icon (⋮) ở section header (UX-30): nút chữ có <c>min-width: 54px</c> nên một icon 12 px nằm lệch trái
        /// trong một ô rộng rỗng, đọc thành nút hỏng. Class này gỡ min-width và căn icon vào giữa ô vuông.
        /// </summary>
        internal const string OverviewIconButton = "liveops-hub-overview-icon-button";

        internal const string OverviewRecheckAllButton = "liveops-hub-overview-recheck-all";
        internal const string OverviewRecheckAllLabel = "liveops-hub-overview-recheck-all-label";

        /// <summary>
        /// Câu chú cuối thân màn (W9-03). Label đeo class khung <c>liveops-hub-note</c> — class đó vẽ khung hộp chú thích và
        /// KHÔNG khai <c>white-space</c>, nên mặc định <c>nowrap</c> của UI Toolkit làm câu dài bị cắt ở mọi cửa sổ dưới
        /// 1100px. Class riêng này chỉ bật xuống dòng; đổi ở class khung sẽ đụng mọi màn nên phải là class của màn.
        /// </summary>
        internal const string OverviewFooterNote = "liveops-hub-overview-footer-note";
    }
}
