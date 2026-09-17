namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class thành phần của vùng Timeline (V-5, G-TIMELINE-VIEW) không có trong bảng 8.10: phần con của thanh, header làn, thước ba
    /// tầng + bubble con trỏ (V-11), lớp phủ vạch bây giờ, chú giải và dòng gợi ý. Class gốc <c>liveops-hub-timeline</c>,
    /// <c>-timeline-lane</c>, <c>-timeline-bar</c> (+ biến thể trạng thái), <c>-timeline-overlap</c>, <c>-timeline-readout</c>,
    /// <c>-timeline-ruler</c>, <c>-timeline-minimap</c> đã ở file gốc nên không khai lại. Thiếu hằng thì <c>check-class-names.py</c>
    /// chặn class gõ tay trong USS/C#.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        // Khung control: vùng chính (thước + thân cuộn + lớp phủ) → minimap → chú giải → dòng gợi ý.
        internal const string TimelineMain = "liveops-hub-timeline-main";
        internal const string TimelineBody = "liveops-hub-timeline-body";
        internal const string TimelineOverlay = "liveops-hub-timeline-overlay";
        internal const string TimelineNowLine = "liveops-hub-timeline-now-line";
        internal const string TimelineCursorLine = "liveops-hub-timeline-cursor-line";
        internal const string TimelineHidden = "liveops-hub-timeline--hidden";

        // Hàng làn = header 168px + track (LiveOpsTimelineLane).
        internal const string TimelineLaneRow = "liveops-hub-timeline-lane-row";
        // (G-OPT-TIMELINE, Hình 12 khung 12) Làn thu gọn: cao 22px, dải 6px không nhãn; chevron ở dòng tên nói làn đang gập hay mở.
        internal const string TimelineLaneCollapsed = "liveops-hub-timeline-lane--collapsed";
        internal const string TimelineLaneChevron = "liveops-hub-timeline-lane-chevron";
        internal const string TimelineLaneChevronShape = "liveops-hub-timeline-lane-chevron-shape";
        internal const string TimelineLaneChevronCollapsed = "liveops-hub-timeline-lane-chevron--collapsed";
        internal const string TimelineLaneHeader = "liveops-hub-timeline-lane-header";
        internal const string TimelineLaneNameRow = "liveops-hub-timeline-lane-name-row";
        internal const string TimelineLaneSwatch = "liveops-hub-timeline-lane-swatch";
        internal const string TimelineLaneName = "liveops-hub-timeline-lane-name";
        internal const string TimelineLaneNamePlaceholder = "liveops-hub-timeline-lane-name--placeholder";
        internal const string TimelineLaneLoopIcon = "liveops-hub-timeline-lane-loop-icon";
        internal const string TimelineLaneChip = "liveops-hub-timeline-lane-chip";
        internal const string TimelineLaneMetaRow = "liveops-hub-timeline-lane-meta-row";
        internal const string TimelineLaneMeta = "liveops-hub-timeline-lane-meta";
        internal const string TimelineLaneMetaSecondary = "liveops-hub-timeline-lane-meta--secondary";
        internal const string TimelineLaneRecurring = "liveops-hub-timeline-lane--recurring";
        internal const string TimelineUnplaceableChip = "liveops-hub-timeline-unplaceable-chip";
        internal const string TimelineNextChip = "liveops-hub-timeline-next-chip";
        internal const string TimelineOverlapLabel = "liveops-hub-timeline-overlap-label";
        internal const string TimelineDroppedTag = "liveops-hub-timeline-dropped-tag";
        internal const string TimelineDroppedTagWillDrop = "liveops-hub-timeline-dropped-tag--will-drop";

        // Phần con của thanh [SD1 §3.5]: dải màu đáy 3px, nhãn, vuông "khác bản đã đăng", gạch bị bỏ, dấu cắt mép, vùng bắt mép 8px.
        internal const string TimelineBarStripe = "liveops-hub-timeline-bar__stripe";
        internal const string TimelineBarLabel = "liveops-hub-timeline-bar__label";
        internal const string TimelineBarChangedSquare = "liveops-hub-timeline-bar__changed";
        internal const string TimelineBarStrike = "liveops-hub-timeline-bar__strike";
        internal const string TimelineBarClipStart = "liveops-hub-timeline-bar__clip-start";
        internal const string TimelineBarClipEnd = "liveops-hub-timeline-bar__clip-end";
        internal const string TimelineBarClipShape = "liveops-hub-timeline-bar__clip-shape";
        internal const string TimelineBarEdge = "liveops-hub-timeline-bar__edge";
        internal const string TimelineBarEdgeStart = "liveops-hub-timeline-bar__edge--start";
        internal const string TimelineBarEdgeEnd = "liveops-hub-timeline-bar__edge--end";
        internal const string TimelineBarHandle = "liveops-hub-timeline-bar__handle";
        internal const string TimelineBarLoopIcon = "liveops-hub-timeline-bar__loop-icon";
        internal const string TimelineBarLockIcon = "liveops-hub-timeline-bar__lock-icon";
        internal const string TimelineBarGhost = "liveops-hub-timeline-bar-ghost";

        // (G-OPT-TIMELINE) Thanh trong làn thu gọn: dải 6px bo 1, không nhãn, không tay nắm (Hình 12 khung 12).
        internal const string TimelineBarCollapsed = "liveops-hub-timeline-bar--collapsed";

        // (G-OPT-TIMELINE, Hình 12 khung 9) Khung chọn kéo trên chỗ trống: nền highlight 0,2 + viền 1px highlight.
        internal const string TimelineMarquee = "liveops-hub-timeline-marquee";

        // Thước ba tầng (V-11): tầng 1 tháng/tuần/cờ bây giờ · tầng 2 ngày hoặc giờ UTC + bubble · hàng dấu (cờ đã đăng) · tầng 3 giờ máy.
        internal const string TimelineRulerDayZoom = "liveops-hub-timeline-ruler--day-zoom";
        internal const string TimelineRulerCorner = "liveops-hub-timeline-ruler-corner";
        internal const string TimelineRulerCornerTitle = "liveops-hub-timeline-ruler-corner-title";
        internal const string TimelineRulerCornerSubtitle = "liveops-hub-timeline-ruler-corner-subtitle";
        internal const string TimelineRulerTrack = "liveops-hub-timeline-ruler-track";
        internal const string TimelineRulerTier = "liveops-hub-timeline-ruler-tier";
        internal const string TimelineRulerTierMonth = "liveops-hub-timeline-ruler-tier--month";
        internal const string TimelineRulerTierDay = "liveops-hub-timeline-ruler-tier--day";
        internal const string TimelineRulerTierMarks = "liveops-hub-timeline-ruler-tier--marks";
        internal const string TimelineRulerTierDevice = "liveops-hub-timeline-ruler-tier--device";
        internal const string TimelineRulerLabel = "liveops-hub-timeline-ruler-label";
        internal const string TimelineRulerLabelEmphasized = "liveops-hub-timeline-ruler-label--emphasized";
        internal const string TimelineRulerNowFlag = "liveops-hub-timeline-ruler-now-flag";
        internal const string TimelineRulerBubble = "liveops-hub-timeline-ruler-bubble";
        internal const string TimelineRulerPublishedFlag = "liveops-hub-timeline-ruler-published-flag";

        // Readout bám mép đang kéo; vế "chồng … với …" chữ blocked.
        internal const string TimelineReadoutText = "liveops-hub-timeline-readout-text";
        internal const string TimelineReadoutOverlap = "liveops-hub-timeline-readout-overlap";

        // (nợ D-3(a)) Vế thứ ba của readout: dấu 7px + câu kiểm nhanh của làn đang kéo (Hình 12 khung 5).
        internal const string TimelineReadoutQuickCheck = "liveops-hub-timeline-readout-quick-check";
        internal const string TimelineReadoutQuickCheckText = "liveops-hub-timeline-readout-quick-check-text";

        // Chú giải 8 mục (mẫu 18×10 + chữ) và dòng gợi ý 18px.
        internal const string TimelineLegend = "liveops-hub-timeline-legend";
        internal const string TimelineLegendItem = "liveops-hub-timeline-legend-item";
        internal const string TimelineLegendSample = "liveops-hub-timeline-legend-sample";
        internal const string TimelineLegendSampleFixed = "liveops-hub-timeline-legend-sample--fixed";
        internal const string TimelineLegendSampleRecurring = "liveops-hub-timeline-legend-sample--recurring";
        internal const string TimelineLegendSampleEnded = "liveops-hub-timeline-legend-sample--ended";
        internal const string TimelineLegendSampleOverlap = "liveops-hub-timeline-legend-sample--overlap";
        internal const string TimelineLegendSampleMark = "liveops-hub-timeline-legend-sample-mark";
        internal const string TimelineLegendText = "liveops-hub-timeline-legend-text";
        internal const string TimelineHint = "liveops-hub-timeline-hint";
        internal const string TimelineHintSubject = "liveops-hub-timeline-hint-subject";
        internal const string TimelineHintText = "liveops-hub-timeline-hint-text";
    }
}
