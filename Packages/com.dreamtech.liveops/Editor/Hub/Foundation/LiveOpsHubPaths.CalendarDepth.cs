using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Phần vùng CalendarDepth của <see cref="LiveOpsHubPaths"/> (V-5): tên element của pane Danh sách, pane "So với đã đăng" và
    /// hover card. Ba mảnh này dựng từ C# (không có UXML riêng) nhưng vẫn khai tên element ở đây vì test và kịch bản chụp Q theo
    /// tên — đặt tên rải trong code là cách chắc chắn nhất để một lần đổi tên làm ảnh im lặng chụp nhầm phần tử.
    /// </summary>
    internal static partial class LiveOpsHubPaths
    {
        internal static class CalendarDepthElementNames
        {
            /// <summary>Bảng của pane Danh sách; pane bao ngoài là <c>calendar-list-pane</c> của vùng Calendar.</summary>
            internal const string ListTable = "calendar-list-table";

            internal const string ListEmpty = "calendar-list-empty";

            /// <summary>Pane "So với đã đăng" — sibling của inspector, hai pane loại trừ nhau [SD1 §3.4].</summary>
            internal const string ComparePane = "calendar-compare-pane";

            internal const string CompareTitle = "calendar-compare-title";
            internal const string CompareBody = "calendar-compare-body";
            internal const string CompareNote = "calendar-compare-note";

            /// <summary>Gốc nội dung hover card của thanh (nằm trong thẻ dùng chung của cửa sổ).</summary>
            internal const string HoverCard = "calendar-hover-card";

            internal const string HoverProblem = "calendar-hover-problem";
            internal const string HoverQuickFix = "calendar-hover-quick-fix";
            internal const string HoverCounter = "calendar-hover-counter";

            /// <summary>Nút đóng của drawer inspector — chỉ hiện ở <c>--medium</c> [SD1 §3.9].</summary>
            internal const string InspectorDrawerClose = "calendar-inspector-drawer-close";
        }

        /// <summary>Element sống còn của phần chiều sâu màn Lịch — <c>CalendarDepthTests</c> Q từng tên sau khi mở pane.</summary>
        internal static readonly IReadOnlyList<string> RequiredCalendarDepthElementNames = Array.AsReadOnly(new[]
        {
            CalendarDepthElementNames.ListTable, CalendarDepthElementNames.ComparePane, CalendarDepthElementNames.CompareTitle,
            CalendarDepthElementNames.CompareBody, CalendarDepthElementNames.CompareNote,
        });
    }
}
