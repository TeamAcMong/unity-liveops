namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Hằng tên class USS dùng chung của LiveOps Hub — bảng 8.10 của kế hoạch, do G-SKELETON khai đủ từ W0 và
    /// không sửa về sau (V-5). <c>TOOLS/check-class-names.py</c> đối chiếu: mọi literal <c>liveops-hub-*</c> trong
    /// USS/UXML/C# phải bằng giá trị một hằng ở đây hoặc ở một file vùng <c>LiveOpsHubClassNames.&lt;Vùng&gt;.cs</c>
    /// (cùng chủ với <c>LiveOpsHubStrings.&lt;Vùng&gt;.cs</c>, bảng vùng → gói mục 10.4); không hai hằng cùng giá trị.
    /// Class riêng của một màn (tiền tố <c>liveops-hub-&lt;id màn&gt;-…</c>) và thành phần mới ngoài bảng này khai
    /// ở file vùng của gói tạo thành phần đó, không khai ở đây.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        // Root
        internal const string Root = "liveops-hub-root";
        internal const string SkinLight = "liveops-hub--skin-light";
        internal const string Medium = "liveops-hub--medium";
        internal const string Narrow = "liveops-hub--narrow";
        internal const string Compact = "liveops-hub--compact";
        internal const string NoMotion = "liveops-hub--no-motion";

        // Frame
        internal const string Header = "liveops-hub-header";
        internal const string Goto = "liveops-hub-goto";
        internal const string Chip = "liveops-hub-chip";
        internal const string ChipKey = "liveops-hub-chip-key";
        internal const string ChipDivider = "liveops-hub-chip-divider";
        internal const string Rail = "liveops-hub-rail";
        internal const string RailHasFocus = "liveops-hub-rail--has-focus";
        internal const string RailCaption = "liveops-hub-rail-caption";
        internal const string RailStageRow = "liveops-hub-rail-stage-row";
        internal const string RailRow = "liveops-hub-rail-row";
        internal const string RailRowActive = "liveops-hub-rail-row--active";
        internal const string RailBadge = "liveops-hub-rail-badge";
        internal const string RailBadgeStale = "liveops-hub-rail-badge--stale";
        internal const string Gutter = "liveops-hub-gutter";
        internal const string GutterLine = "liveops-hub-gutter-line";
        internal const string GutterLineDead = "liveops-hub-gutter-line--dead";
        internal const string RailBlocker = "liveops-hub-rail-blocker";
        internal const string SectionHeader = "liveops-hub-section-header";
        internal const string SectionTitle = "liveops-hub-section-title";
        internal const string SectionSubtitle = "liveops-hub-section-subtitle";
        internal const string SectionActions = "liveops-hub-section-actions";
        internal const string SectionBody = "liveops-hub-section-body";
        internal const string Status = "liveops-hub-status";
        internal const string StatusLeft = "liveops-hub-status-left";
        internal const string StatusRight = "liveops-hub-status-right";
        internal const string Toast = "liveops-hub-toast";
        internal const string ToastVisible = "liveops-hub-toast--visible";
        internal const string ContentRaisedToast = "liveops-hub-content--raised-toast";
        internal const string Scrim = "liveops-hub-scrim";
        internal const string Palette = "liveops-hub-palette";
        internal const string PaletteRow = "liveops-hub-palette-row";
        internal const string PaletteRowSelected = "liveops-hub-palette-row--selected";
        internal const string HoverCard = "liveops-hub-hover-card";
        internal const string Drawer = "liveops-hub-drawer";
        internal const string Failure = "liveops-hub-failure";
        internal const string Empty = "liveops-hub-empty";
        internal const string Note = "liveops-hub-note";

        // State
        internal const string StateMark = "liveops-hub-state-mark";
        internal const string StateMarkOk = "liveops-hub-state-mark--ok";
        internal const string StateMarkWarning = "liveops-hub-state-mark--warning";
        internal const string StateMarkBlocked = "liveops-hub-state-mark--blocked";
        internal const string StateMarkNotMeasured = "liveops-hub-state-mark--not-measured";
        internal const string StateMarkSmall = "liveops-hub-state-mark--small";
        internal const string StateMarkLarge = "liveops-hub-state-mark--large";
        internal const string StateMarkBar = "liveops-hub-state-mark__bar";
        internal const string PhaseRunning = "liveops-hub-phase--running";
        internal const string PhaseUpcoming = "liveops-hub-phase--upcoming";
        internal const string PhasePending = "liveops-hub-phase--pending";
        internal const string PhaseEnded = "liveops-hub-phase--ended";
        internal const string FillOk = "liveops-hub-fill--ok";
        internal const string FillWarning = "liveops-hub-fill--warning";
        internal const string FillBlocked = "liveops-hub-fill--blocked";
        internal const string FillNotMeasured = "liveops-hub-fill--not-measured";
        internal const string TextOk = "liveops-hub-text--ok";
        internal const string TextWarning = "liveops-hub-text--warning";
        internal const string TextBlocked = "liveops-hub-text--blocked";
        internal const string TextQuiet = "liveops-hub-text--quiet";

        // Components
        internal const string Button = "liveops-hub-button";
        internal const string ButtonFirst = "liveops-hub-button--first";
        internal const string ButtonPrimary = "liveops-hub-button--primary";
        internal const string ButtonDanger = "liveops-hub-button--danger";
        /// <summary>
        /// (J3-01) Nút KHÔNG dãn theo hộp chứa — dành cho nút nằm trong hộp xếp DỌC, nơi align-items mặc định của UI Toolkit
        /// là stretch nên nút nhận trọn bề ngang pane. Không đặt luật này thẳng lên <see cref="Button"/>: trong một HÀNG
        /// ngang có align-items: center thì align-self: flex-start kéo nút lên mép trên hàng.
        /// </summary>
        internal const string ButtonSelfStart = "liveops-hub-button--self-start";
        internal const string ButtonSlot = "liveops-hub-button-slot";
        internal const string ButtonReason = "liveops-hub-button-reason";
        internal const string Card = "liveops-hub-card";
        internal const string CardHeader = "liveops-hub-card-header";
        internal const string CardCollapsed = "liveops-hub-card--collapsed";
        internal const string CardHeaderClickable = "liveops-hub-card-header--clickable";
        internal const string Metric = "liveops-hub-metric";
        internal const string MetricCaption = "liveops-hub-metric-caption";
        internal const string MetricValue = "liveops-hub-metric-value";
        internal const string MetricUnit = "liveops-hub-metric-unit";
        internal const string MetricFoot = "liveops-hub-metric-foot";
        internal const string KeyValue = "liveops-hub-key-value";
        internal const string KeyValueKey = "liveops-hub-key-value-key";
        internal const string Tag = "liveops-hub-tag";
        internal const string FindingRow = "liveops-hub-finding-row";
        internal const string FindingStripeBlocked = "liveops-hub-finding-stripe--blocked";
        internal const string FindingStripeWarning = "liveops-hub-finding-stripe--warning";
        internal const string FindingStripeNotMeasured = "liveops-hub-finding-stripe--not-measured";
        internal const string RowLast = "liveops-hub-row--last";
        internal const string RowFlash = "liveops-hub-row--flash";
        internal const string RowActive = "liveops-hub-row--active";
        internal const string ListHasFocus = "liveops-hub-list--has-focus";
        internal const string Placeholder = "liveops-hub-placeholder";
        internal const string PlaceholderHidden = "liveops-hub-placeholder--hidden";
        internal const string Mono = "liveops-hub-mono";
        internal const string Caption = "liveops-hub-caption";
        internal const string Chevron = "liveops-hub-chevron";
        internal const string Swatch = "liveops-hub-swatch";
        internal const string EventColor0 = "liveops-hub-event-color-0";
        internal const string EventColor1 = "liveops-hub-event-color-1";
        internal const string EventColor2 = "liveops-hub-event-color-2";
        internal const string EventColor3 = "liveops-hub-event-color-3";
        internal const string EventColor4 = "liveops-hub-event-color-4";
        internal const string EventColor5 = "liveops-hub-event-color-5";
        internal const string EventColor6 = "liveops-hub-event-color-6";
        internal const string EventColor7 = "liveops-hub-event-color-7";
        internal const string HelpboxActions = "liveops-hub-helpbox-actions";

        // Calendar
        internal const string Timeline = "liveops-hub-timeline";
        internal const string TimelineHasFocus = "liveops-hub-timeline--has-focus";
        internal const string TimelineLane = "liveops-hub-timeline-lane";
        internal const string TimelineBar = "liveops-hub-timeline-bar";
        internal const string TimelineBarHover = "liveops-hub-timeline-bar--hover";
        internal const string TimelineBarSelected = "liveops-hub-timeline-bar--selected";
        internal const string TimelineBarFocused = "liveops-hub-timeline-bar--focused";
        internal const string TimelineBarRunning = "liveops-hub-timeline-bar--running";
        internal const string TimelineBarEnded = "liveops-hub-timeline-bar--ended";
        internal const string TimelineBarDragging = "liveops-hub-timeline-bar--dragging";
        internal const string TimelineBarWillDrop = "liveops-hub-timeline-bar--will-drop";
        internal const string TimelineBarDropped = "liveops-hub-timeline-bar--dropped";
        internal const string TimelineBarChanged = "liveops-hub-timeline-bar--changed";
        internal const string TimelineBarClippedStart = "liveops-hub-timeline-bar--clipped-start";
        internal const string TimelineBarClippedEnd = "liveops-hub-timeline-bar--clipped-end";
        internal const string TimelineBarRecurring = "liveops-hub-timeline-bar--recurring";
        internal const string TimelineBarMerged = "liveops-hub-timeline-bar--merged";
        internal const string TimelineOverlap = "liveops-hub-timeline-overlap";
        internal const string TimelineReadout = "liveops-hub-timeline-readout";
        internal const string TimelineRuler = "liveops-hub-timeline-ruler";
        internal const string TimelineMinimap = "liveops-hub-timeline-minimap";
        internal const string Inspector = "liveops-hub-inspector";
        internal const string CreateFlow = "liveops-hub-create-flow";

        // Recurring
        internal const string RuleSentence = "liveops-hub-rule-sentence";
        internal const string RuleToken = "liveops-hub-rule-token";

        /// <summary>
        /// (J2-06) Chip token mà mảnh chữ đứng ngay sau bắt đầu bằng DẤU CÂU (", neo từ ", ", id = "). Padding 0 3px là khoảng
        /// thở của chip, không phải khoảng cách của câu — để nguyên thì câu đọc thành "Mỗi [7 ngày] , neo từ". Chỉ gắn khi
        /// sau nó là dấu câu: mảnh " + số thứ tự." bắt đầu bằng KHOẢNG TRẮNG thật, kéo nó lại là dính "pass-+".
        /// </summary>
        internal const string RuleTokenFollowedByPunctuation = "liveops-hub-rule-token--followed-by-punctuation";
        internal const string RuleTokenHighlighted = "liveops-hub-rule-token--highlighted";
        internal const string RuleTokenWarning = "liveops-hub-rule-token--warning";
        internal const string CycleBar = "liveops-hub-cycle-bar";

        // Json
        internal const string JsonView = "liveops-hub-json-view";
        internal const string JsonGutter = "liveops-hub-json-gutter";
        internal const string JsonOverview = "liveops-hub-json-overview";
    }
}
