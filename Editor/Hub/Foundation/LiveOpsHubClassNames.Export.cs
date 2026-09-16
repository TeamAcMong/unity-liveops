namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class riêng của màn Xuất JSON (V-5): hai hàng bố cục [SD2 §3.3], card cổng xuất, hàng diff theo hậu quả, bảng Các lần
    /// đã đăng và hộp Đánh dấu đã đăng. Card, metric, dấu trạng thái, tag, nút, lý do cạnh nút lấy từ bảng 8.10 ở
    /// <c>LiveOpsHubClassNames.cs</c> — chỉ những gì THẬT SỰ mới của màn mới khai ở đây.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        // Thân màn — hàng trên (cổng + metric), hàng dưới (JSON + cột phải)
        internal const string ExportPage = "liveops-hub-export-page";
        internal const string ExportTopRow = "liveops-hub-export-top-row";
        internal const string ExportBottomRow = "liveops-hub-export-bottom-row";
        internal const string ExportGateColumn = "liveops-hub-export-gate-column";
        internal const string ExportMetricRow = "liveops-hub-export-metric-row";
        internal const string ExportJsonColumn = "liveops-hub-export-json-column";
        internal const string ExportSideColumn = "liveops-hub-export-side-column";

        /// <summary>Khối bị ẩn hoàn toàn (empty "chưa có asset" hoặc thân mặc định) — display:none, không chỉ trong suốt.</summary>
        internal const string ExportHidden = "liveops-hub-export-hidden";

        /// <summary>Metric cuối hàng: bỏ margin-right (USS không có <c>:last-child</c>, class bật từ C#).</summary>
        internal const string ExportMetricLast = "liveops-hub-export-metric--last";
        internal const string ExportMetricValueRow = "liveops-hub-export-metric-value-row";

        // Card cổng xuất — dòng cao 22px, dấu 8px + chữ + meta/nút/chevron [SD2 §3.4]
        internal const string ExportGateRow = "liveops-hub-export-gate-row";
        internal const string ExportGateRowClickable = "liveops-hub-export-gate-row--clickable";
        internal const string ExportGateRowRunning = "liveops-hub-export-gate-row--running";
        internal const string ExportGateRowText = "liveops-hub-export-gate-row-text";
        internal const string ExportGateRowMeta = "liveops-hub-export-gate-row-meta";
        internal const string ExportGateRowSpacer = "liveops-hub-export-gate-row-spacer";
        internal const string ExportGateCompactRow = "liveops-hub-export-gate-compact-row";
        internal const string ExportGateCompactRemote = "liveops-hub-export-gate-compact-remote";

        // Card lỗi (h) — sọc blocked, message runtime mono nguyên văn
        internal const string ExportFailureCard = "liveops-hub-export-failure-card";
        internal const string ExportFailureSymptom = "liveops-hub-export-failure-symptom";
        internal const string ExportFailureMessage = "liveops-hub-export-failure-message";
        internal const string ExportFailurePosition = "liveops-hub-export-failure-position";
        internal const string ExportFailureActions = "liveops-hub-export-failure-actions";

        // Note (i) đang khôi phục + HelpBox (j) định dạng 1
        internal const string ExportRestoringNote = "liveops-hub-export-restoring-note";

        // Card diff — nhóm hậu quả, hàng hai dòng, tint 0,16
        internal const string ExportDiffChipRow = "liveops-hub-export-diff-chip-row";
        internal const string ExportDiffGroup = "liveops-hub-export-diff-group";
        internal const string ExportDiffGroupLater = "liveops-hub-export-diff-group--later";
        internal const string ExportDiffGroupHeader = "liveops-hub-export-diff-group-header";
        internal const string ExportDiffRow = "liveops-hub-export-diff-row";
        internal const string ExportDiffRowDropped = "liveops-hub-export-diff-row--dropped";
        internal const string ExportDiffRowProgressLost = "liveops-hub-export-diff-row--progress-lost";
        internal const string ExportDiffSymbol = "liveops-hub-export-diff-symbol";
        internal const string ExportDiffRowText = "liveops-hub-export-diff-row-text";
        internal const string ExportDiffRowTitle = "liveops-hub-export-diff-row-title";
        internal const string ExportDiffRowConsequence = "liveops-hub-export-diff-row-consequence";
        internal const string ExportDiffRowAction = "liveops-hub-export-diff-row-action";
        internal const string ExportDiffEmpty = "liveops-hub-export-diff-empty";

        // Card Các lần đã đăng — cột 90 / 110 / giãn / 60, hàng Active viền trái 3px
        internal const string ExportHistoryTable = "liveops-hub-export-history-table";
        internal const string ExportHistoryHeadRow = "liveops-hub-export-history-head-row";
        internal const string ExportHistoryRow = "liveops-hub-export-history-row";
        internal const string ExportHistoryRowActive = "liveops-hub-export-history-row--active";
        internal const string ExportHistoryCell = "liveops-hub-export-history-cell";
        internal const string ExportHistoryCellTime = "liveops-hub-export-history-cell--time";
        internal const string ExportHistoryCellPublisher = "liveops-hub-export-history-cell--publisher";
        internal const string ExportHistoryCellNote = "liveops-hub-export-history-cell--note";
        internal const string ExportHistoryCellSha = "liveops-hub-export-history-cell--sha";
        internal const string ExportHistoryActiveSuffix = "liveops-hub-export-history-active-suffix";

        // Hộp Đánh dấu đã đăng (410×330) — hàng nút dính đáy bằng margin-top:auto
        internal const string ExportMarkWindow = "liveops-hub-export-mark-window";
        internal const string ExportMarkHeading = "liveops-hub-export-mark-heading";
        internal const string ExportMarkBody = "liveops-hub-export-mark-body";
        internal const string ExportMarkShaRow = "liveops-hub-export-mark-sha-row";
        internal const string ExportMarkShaText = "liveops-hub-export-mark-sha-text";
        internal const string ExportMarkCopyRow = "liveops-hub-export-mark-copy-row";
        internal const string ExportMarkNoteLabel = "liveops-hub-export-mark-note-label";
        internal const string ExportMarkNoteField = "liveops-hub-export-mark-note-field";
        internal const string ExportMarkButtonRow = "liveops-hub-export-mark-button-row";
        internal const string ExportMarkMissing = "liveops-hub-export-mark-missing";
    }
}
