namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class riêng phần chiều sâu màn Lịch (V-5, G-CALENDAR-DEPTH): hai nút toolbar W5, pane Danh sách, pane "So với đã đăng" và
    /// hover card của thanh. Class dùng chung (<c>liveops-hub-card</c>, <c>liveops-hub-hover-card</c>, <c>liveops-hub-mono</c>…)
    /// đã có ở file gốc / vùng Feedback nên không khai lại; class khung màn Lịch ở vùng Calendar.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        // Toolbar W5 [SD1 §3.1].
        internal const string CalendarDepthListToggle = "liveops-hub-calendar-list-toggle";
        internal const string CalendarDepthCompareToggle = "liveops-hub-calendar-compare-toggle";

        /// <summary>Nhãn lý do in cạnh nút bị khoá (SPIKE-B SP-3) — chữ thật, không phải tooltip.</summary>
        internal const string CalendarDepthDisabledReason = "liveops-hub-calendar-disabled-reason";

        // Pane Danh sách [SD1 §3.12].
        internal const string CalendarDepthListTable = "liveops-hub-calendar-list-table";
        internal const string CalendarDepthListCell = "liveops-hub-calendar-list-cell";
        internal const string CalendarDepthListEmpty = "liveops-hub-calendar-list-empty";

        // Pane "So với đã đăng" [SD1 §3.4]: pane-title + nhãn nhóm + hàng 22px ba cột.
        internal const string CalendarDepthComparePane = "liveops-hub-compare-pane";
        internal const string CalendarDepthCompareTitle = "liveops-hub-calendar-compare-title";
        internal const string CalendarDepthCompareCount = "liveops-hub-calendar-compare-count";
        internal const string CalendarDepthCompareBody = "liveops-hub-calendar-compare-body";
        internal const string CalendarDepthCompareGroupLabel = "liveops-hub-calendar-compare-group-label";
        internal const string CalendarDepthCompareRow = "liveops-hub-calendar-compare-row";
        internal const string CalendarDepthCompareRowSelected = "liveops-hub-calendar-compare-row--selected";
        internal const string CalendarDepthCompareSymbol = "liveops-hub-calendar-compare-symbol";
        internal const string CalendarDepthCompareText = "liveops-hub-calendar-compare-text";
        internal const string CalendarDepthCompareNote = "liveops-hub-calendar-compare-note";

        // Hover card của thanh [SD1 §3.8]: hàng key–value cột key 58px, dòng lỗi, chân 10px.
        internal const string CalendarDepthHoverCard = "liveops-hub-calendar-hover-card";
        internal const string CalendarDepthHoverHeader = "liveops-hub-calendar-hover-header";
        internal const string CalendarDepthHoverRow = "liveops-hub-calendar-hover-row";
        internal const string CalendarDepthHoverKey = "liveops-hub-calendar-hover-key";
        internal const string CalendarDepthHoverValue = "liveops-hub-calendar-hover-value";
        internal const string CalendarDepthHoverProblem = "liveops-hub-calendar-hover-problem";
        internal const string CalendarDepthHoverFooter = "liveops-hub-calendar-hover-footer";
        internal const string CalendarDepthHoverPinnedRow = "liveops-hub-calendar-hover-pinned-row";
        internal const string CalendarDepthHoverCounter = "liveops-hub-calendar-hover-counter";
    }
}
