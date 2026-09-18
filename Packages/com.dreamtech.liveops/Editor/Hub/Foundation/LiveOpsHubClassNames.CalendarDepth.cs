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

        // Toolbar rút gọn ở --narrow (8.8 [FD §4.2]): tab zoom ⇄ menu zoom, menu ⋮ của việc ít dùng, dải chú giải bật lại từ menu.
        internal const string CalendarDepthZoomTabs = "liveops-hub-calendar-zoom-tabs";
        internal const string CalendarDepthZoomMenu = "liveops-hub-calendar-zoom-menu";
        internal const string CalendarDepthOverflowMenu = "liveops-hub-calendar-overflow-menu";
        internal const string CalendarDepthTodayButton = "liveops-hub-calendar-today-button";

        /// <summary>Dải chú giải được bật lại từ menu ⋮ tuy cửa sổ đang hẹp — thắng luật ẩn của <c>--narrow</c>.</summary>
        internal const string CalendarDepthLegendShown = "liveops-hub-calendar-legend-shown";

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

        /// <summary>Hàng bọc dòng lỗi: dấu 12px + câu, [SD1 §3.8] "icon err 12 + blocked-text 10px".</summary>
        internal const string CalendarDepthHoverProblemRow = "liveops-hub-calendar-hover-problem-row";
        internal const string CalendarDepthHoverProblemMark = "liveops-hub-calendar-hover-problem-mark";
        internal const string CalendarDepthHoverFooter = "liveops-hub-calendar-hover-footer";
        internal const string CalendarDepthHoverPinnedRow = "liveops-hub-calendar-hover-pinned-row";
        internal const string CalendarDepthHoverCounter = "liveops-hub-calendar-hover-counter";

        /// <summary>Nút đóng của drawer inspector — USS chỉ hiện nó ở <c>--medium</c> [SD1 §3.9].</summary>
        internal const string CalendarDepthDrawerClose = "liveops-hub-calendar-drawer-close";

        // ----- Đợt W8-UX (gói A) -----

        /// <summary>
        /// (UX-01, C1/C2/UJ-26) Mắt xích giãn của chuỗi gốc màn. <c>calendar-root</c> trong UXML và gốc do section dựng đều
        /// không có class nào, nên <c>.liveops-hub-calendar-main {flex-grow:1}</c> không có cha nào để giãn theo và cả thân màn
        /// sụp về chiều cao nội tại (79px, hoặc 0 khi inspector thành drawer absolute).
        /// </summary>
        internal const string CalendarDepthFillHeight = "liveops-hub-calendar-fill-height";

        /// <summary>
        /// (UX-07, lệch thiết kế V-39) Drawer inspector đang mở ở <c>--medium</c>: cột timeline nhận lề phải bằng bề rộng
        /// drawer thay vì bị phủ — chip "Đợt tới", nhãn "chồng n giờ" và đuôi gợi ý neo phải phải còn thấy và bấm được.
        /// </summary>
        internal const string CalendarDepthDrawerOpen = "liveops-hub-calendar-drawer-open";

        /// <summary>
        /// (UX-11, C8/V8) Chân màn Lịch cao hơn một dòng chú giải (minimap + chú giải + gợi ý): toast phải nâng cao hơn bậc
        /// mặc định. Đo chân thật rồi chọn bậc, thay cho hằng 64px từng đè lên hai dòng chú giải.
        /// </summary>
        internal const string CalendarDepthContentRaisedToastTall = "liveops-hub-content--raised-toast-tall";

        /// <summary>(UX-29) Menu Bắt lưới — class riêng để luật <c>--narrow</c> đưa nó vào menu ⋮.</summary>
        internal const string CalendarDepthSnapMenu = "liveops-hub-calendar-snap-menu";

        /// <summary>(UX-12) Chip làn ẩn tách thành nhãn + nút, mỗi phần một class.</summary>
        internal const string CalendarDepthHiddenLanesLabel = "liveops-hub-calendar-hidden-lanes-label";
        internal const string CalendarDepthHiddenLanesShowButton = "liveops-hub-calendar-hidden-lanes-show";
    }
}
