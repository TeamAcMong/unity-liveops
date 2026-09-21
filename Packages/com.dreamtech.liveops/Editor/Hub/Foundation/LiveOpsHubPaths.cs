namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Hằng đường dẫn dự án (<c>AssetDatabase</c>) của mọi UXML/USS/PNG của LiveOps Hub đã liệt kê ở mục 2 của kế
    /// hoạch — do G-SKELETON khai đủ từ W0, biết trước cấu trúc thư mục và không đổi về sau (V-5). Đường dẫn file
    /// UXML/USS mới không có sẵn ở mục 2 (nếu một gói buộc phải thêm) khai ở
    /// <c>LiveOpsHubPaths.&lt;Vùng&gt;.cs</c> (cùng chủ với <c>LiveOpsHubStrings.&lt;Vùng&gt;.cs</c>) — không khai
    /// ở đây. Danh sách tên element sống còn của khung (<c>RequiredShellElementNames</c>) thuộc
    /// <c>LiveOpsHubPaths.Shell.cs</c> của G-SHELL, không thuộc file này.
    /// </summary>
    internal static partial class LiveOpsHubPaths
    {
        private const string HubRoot = "Packages/com.dreamtech.liveops/Editor/Hub";

        // Dialogs — Hub/Dialogs/
        internal const string ConfirmWindowUxml = HubRoot + "/Dialogs/LiveOpsConfirmWindow.uxml";
        internal const string ConfirmWindowUss = HubRoot + "/Dialogs/LiveOpsConfirmWindow.uss";
        internal const string PasteRunningJsonPopoverUxml = HubRoot + "/Dialogs/PasteRunningJsonPopover.uxml";
        internal const string ShortcutHelpPopoverUxml = HubRoot + "/Dialogs/ShortcutHelpPopover.uxml";

        // Sections/Overview — Hub/Sections/Overview/
        internal const string OverviewSectionUxml = HubRoot + "/Sections/Overview/OverviewSection.uxml";
        internal const string OverviewSectionUss = HubRoot + "/Sections/Overview/OverviewSection.uss";

        // Sections/EventTypes — Hub/Sections/EventTypes/
        internal const string EventTypesSectionUxml = HubRoot + "/Sections/EventTypes/EventTypesSection.uxml";
        internal const string EventTypesSectionUss = HubRoot + "/Sections/EventTypes/EventTypesSection.uss";

        // Sections/Calendar — Hub/Sections/Calendar/
        internal const string CalendarSectionUxml = HubRoot + "/Sections/Calendar/CalendarSection.uxml";
        internal const string CalendarSectionUss = HubRoot + "/Sections/Calendar/CalendarSection.uss";
        internal const string AddEventPopoverUxml = HubRoot + "/Sections/Calendar/AddEventPopover.uxml";

        // Sections/RecurringRules — Hub/Sections/RecurringRules/
        internal const string RecurringRulesSectionUxml = HubRoot + "/Sections/RecurringRules/RecurringRulesSection.uxml";
        internal const string RecurringRulesSectionUss = HubRoot + "/Sections/RecurringRules/RecurringRulesSection.uss";

        // Sections/Validation — Hub/Sections/Validation/
        internal const string ValidationSectionUxml = HubRoot + "/Sections/Validation/ValidationSection.uxml";
        internal const string ValidationSectionUss = HubRoot + "/Sections/Validation/ValidationSection.uss";
        internal const string ProposalPopoverUxml = HubRoot + "/Sections/Validation/ProposalPopover.uxml";
        internal const string IgnoreWarningPopoverUxml = HubRoot + "/Sections/Validation/IgnoreWarningPopover.uxml";

        // Sections/Export — Hub/Sections/Export/
        internal const string ExportSectionUxml = HubRoot + "/Sections/Export/ExportSection.uxml";
        internal const string ExportSectionUss = HubRoot + "/Sections/Export/ExportSection.uss";
        internal const string MarkPublishedWindowUxml = HubRoot + "/Sections/Export/MarkPublishedWindow.uxml";
        internal const string MarkPublishedWindowUss = HubRoot + "/Sections/Export/MarkPublishedWindow.uss";

        // UI dùng chung — Hub/UI/
        internal const string ThemeUss = HubRoot + "/UI/liveops-hub-theme.uss";
        internal const string ComponentsUss = HubRoot + "/UI/liveops-hub-components.uss";
        internal const string MotionUss = HubRoot + "/UI/liveops-hub-motion.uss";
        internal const string ShellUxml = HubRoot + "/UI/LiveOpsHub.uxml";
        internal const string ShellUss = HubRoot + "/UI/liveops-hub-shell.uss";
        internal const string FeedbackUss = HubRoot + "/UI/liveops-hub-feedback.uss";
        internal const string ControlsUss = HubRoot + "/UI/liveops-hub-controls.uss";
        internal const string TimelineUss = HubRoot + "/UI/liveops-hub-timeline.uss";

        // Icons — Hub/Icons/ (Texture Type = Editor GUI, không mipmap)
        internal const string CalendarIconPng = HubRoot + "/Icons/liveops-hub-calendar.png";
        internal const string CalendarIconDarkPng = HubRoot + "/Icons/d_liveops-hub-calendar.png";
        internal const string CalendarIconPng2x = HubRoot + "/Icons/liveops-hub-calendar@2x.png";
        internal const string CalendarIconDarkPng2x = HubRoot + "/Icons/d_liveops-hub-calendar@2x.png";
        internal const string ClipboardIconDarkPng = HubRoot + "/Icons/d_liveops-hub-clipboard.png";
        internal const string ClipboardIconDarkPng2x = HubRoot + "/Icons/d_liveops-hub-clipboard@2x.png";
    }
}
