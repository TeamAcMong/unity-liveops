namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class riêng màn Lịch (V-5, G-CALENDAR) không có trong bảng 8.10: toolbar màn, cột timeline, inspector đợt và popover
    /// Thêm đợt ba bước. Class dùng chung (<c>liveops-hub-inspector</c>, <c>liveops-hub-create-flow</c>, <c>liveops-hub-card</c>,
    /// <c>liveops-hub-tag</c>…) và class của control timeline đã có ở file gốc / vùng Timeline nên không khai lại.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        // Khung màn: toolbar 21px → split (Danh sách | cột timeline) → inspector sibling 280px.
        internal const string CalendarToolbar = "liveops-hub-calendar-toolbar";
        internal const string CalendarToolbarGroup = "liveops-hub-calendar-toolbar-group";
        internal const string CalendarToolbarSpacer = "liveops-hub-calendar-toolbar-spacer";
        internal const string CalendarSearch = "liveops-hub-calendar-search";
        internal const string CalendarHiddenLanesChip = "liveops-hub-calendar-hidden-lanes-chip";
        internal const string CalendarSplit = "liveops-hub-calendar-split";
        internal const string CalendarListPane = "liveops-hub-calendar-list-pane";
        internal const string CalendarTimelineColumn = "liveops-hub-calendar-timeline-column";

        // Inspector đợt [SD1 §3.1, §3.10]: pane-title + thân field + foldout Vấn đề + nút danger.
        internal const string CalendarInspectorTitle = "liveops-hub-calendar-inspector-title";
        internal const string CalendarInspectorTitleId = "liveops-hub-calendar-inspector-title-id";
        internal const string CalendarInspectorBody = "liveops-hub-calendar-inspector-body";
        internal const string CalendarInspectorSeparator = "liveops-hub-calendar-inspector-separator";
        internal const string CalendarInspectorIssues = "liveops-hub-calendar-inspector-issues";
        internal const string CalendarFieldRow = "liveops-hub-calendar-field-row";
        internal const string CalendarFieldSubline = "liveops-hub-calendar-field-subline";
        internal const string CalendarFieldError = "liveops-hub-calendar-field-error";

        // Popover Thêm đợt [SD1 §3.11]: header có step-dot, danh sách loại, hàng nút có gợi ý phím bên trái.
        internal const string CalendarFlowHeader = "liveops-hub-calendar-flow-header";
        internal const string CalendarFlowStep = "liveops-hub-calendar-flow-step";
        internal const string CalendarStepDot = "liveops-hub-calendar-step-dot";
        internal const string CalendarStepDotActive = "liveops-hub-calendar-step-dot--active";
        internal const string CalendarTypeRow = "liveops-hub-calendar-type-row";
        internal const string CalendarTypeRowActive = "liveops-hub-calendar-type-row--active";
        internal const string CalendarTypeRowDisabled = "liveops-hub-calendar-type-row--disabled";
        internal const string CalendarTypeRowTag = "liveops-hub-calendar-type-row-tag";
        internal const string CalendarFlowButtons = "liveops-hub-calendar-flow-buttons";
        internal const string CalendarFlowKeyHint = "liveops-hub-calendar-flow-key-hint";
    }
}
