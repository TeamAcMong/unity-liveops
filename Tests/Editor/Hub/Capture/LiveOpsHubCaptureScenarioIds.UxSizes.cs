namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Id ảnh của ma trận cỡ cửa sổ W8-UX (§3.4): màn Lịch (chưa chọn + đã chọn) và màn Luật lặp ở SÁU cỡ user chốt 17/9/2026.
    /// <para>
    /// Vì sao thêm một bộ id thay vì chụp lại h01/h13/h14 ở nhiều cỡ: ma trận 9.5 so ảnh với hình thiết kế ở MỘT cỡ và không
    /// được đổi; bộ này chỉ để so TRƯỚC/SAU của đợt sửa, nên nó sống riêng và xoá được khi đợt khép mà không đụng ma trận cũ.
    /// </para>
    /// Id có dạng <c>ux-&lt;màn&gt;-&lt;rộng&gt;x&lt;cao&gt;</c> — <c>capture.sh</c> chỉ nhận id gồm [a-z0-9_-] nên chữ "x" là
    /// dấu nhân, không phải ký hiệu nào khác.
    /// </summary>
    internal static partial class LiveOpsHubCaptureScenarioIds
    {
        internal const string UxCalendar700 = "ux-calendar-700x560";
        internal const string UxCalendar820 = "ux-calendar-820x560";
        internal const string UxCalendar1024 = "ux-calendar-1024x700";
        internal const string UxCalendar1280 = "ux-calendar-1280x760";
        internal const string UxCalendar1440 = "ux-calendar-1440x900";
        internal const string UxCalendar1920 = "ux-calendar-1920x1040";

        internal const string UxCalendarSelected700 = "ux-calendar-selected-700x560";
        internal const string UxCalendarSelected820 = "ux-calendar-selected-820x560";
        internal const string UxCalendarSelected1024 = "ux-calendar-selected-1024x700";
        internal const string UxCalendarSelected1280 = "ux-calendar-selected-1280x760";
        internal const string UxCalendarSelected1440 = "ux-calendar-selected-1440x900";
        internal const string UxCalendarSelected1920 = "ux-calendar-selected-1920x1040";

        internal const string UxRecurring700 = "ux-recurring-700x560";
        internal const string UxRecurring820 = "ux-recurring-820x560";
        internal const string UxRecurring1024 = "ux-recurring-1024x700";
        internal const string UxRecurring1280 = "ux-recurring-1280x760";
        internal const string UxRecurring1440 = "ux-recurring-1440x900";
        internal const string UxRecurring1920 = "ux-recurring-1920x1040";
    }
}
