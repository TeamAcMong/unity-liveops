using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Phần vùng Calendar của <see cref="LiveOpsHubPaths"/> (V-5): tên element viết tay trong <c>CalendarSection.uxml</c> và
    /// <c>AddEventPopover.uxml</c>. Đường dẫn UXML/USS của màn đã có sẵn ở file gốc (mục 2) nên không khai lại ở đây.
    /// Probe CLI và <c>HubWindowTests.EverySection_RequiredElementsPresent</c> Q từng tên sau khi dựng — đổi tên trong UXML mà
    /// quên C# thì lỗi lộ ngay ở cổng, không im lặng thành màn trống (7.0).
    /// </summary>
    internal static partial class LiveOpsHubPaths
    {
        internal static class CalendarElementNames
        {
            internal const string Root = "calendar-root";
            internal const string Toolbar = "calendar-toolbar";
            /// <summary>Thân màn (split + inspector) — ẩn cả khối khi chưa có asset lịch.</summary>
            internal const string Main = "calendar-main";

            internal const string Split = "calendar-split";

            /// <summary>Pane trái của split — W4 luôn thu lại; G-CALENDAR-DEPTH (W5) đổ <c>CalendarListPane</c> vào đây.</summary>
            internal const string ListPane = "calendar-list-pane";

            internal const string TimelineColumn = "calendar-timeline-column";
            internal const string Timeline = "calendar-timeline";
            internal const string Inspector = "calendar-inspector";
            internal const string InspectorTitle = "calendar-inspector-title";
            internal const string InspectorBody = "calendar-inspector-body";

            /// <summary>Trạng thái chung "chưa có asset lịch" (7.0) — luôn có trong cây, ẩn khi đã có asset.</summary>
            internal const string Empty = "calendar-empty";
        }

        internal static class AddEventPopoverElementNames
        {
            internal const string Root = "add-event-root";
            internal const string Header = "add-event-header";
            internal const string HeaderTitle = "add-event-header-title";
            internal const string StepDots = "add-event-step-dots";
            internal const string StepType = "add-event-step-type";
            internal const string StepTimes = "add-event-step-times";
            internal const string StepReview = "add-event-step-review";
            internal const string TypeFilter = "add-event-type-filter";
            internal const string TypeList = "add-event-type-list";
            internal const string Buttons = "add-event-buttons";
        }

        /// <summary>Element sống còn của màn Lịch — trùng <c>CalendarSection.RequiredElementNames</c>.</summary>
        internal static readonly IReadOnlyList<string> RequiredCalendarElementNames = Array.AsReadOnly(new[]
        {
            CalendarElementNames.Root, CalendarElementNames.Toolbar, CalendarElementNames.Main, CalendarElementNames.Split,
            CalendarElementNames.ListPane,
            CalendarElementNames.TimelineColumn, CalendarElementNames.Timeline, CalendarElementNames.Inspector,
            CalendarElementNames.InspectorTitle, CalendarElementNames.InspectorBody, CalendarElementNames.Empty,
        });

        /// <summary>Element sống còn của popover Thêm đợt — <c>AddEventPopoverTests</c> Q từng tên.</summary>
        internal static readonly IReadOnlyList<string> RequiredAddEventPopoverElementNames = Array.AsReadOnly(new[]
        {
            AddEventPopoverElementNames.Root, AddEventPopoverElementNames.Header, AddEventPopoverElementNames.HeaderTitle,
            AddEventPopoverElementNames.StepDots, AddEventPopoverElementNames.StepType, AddEventPopoverElementNames.StepTimes,
            AddEventPopoverElementNames.StepReview, AddEventPopoverElementNames.TypeFilter, AddEventPopoverElementNames.TypeList,
            AddEventPopoverElementNames.Buttons,
        });
    }
}
