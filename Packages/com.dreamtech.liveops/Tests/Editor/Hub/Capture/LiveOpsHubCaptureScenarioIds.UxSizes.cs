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

        // Nhánh TIẾNG ANH của cùng ma trận. Vì sao có: chính báo cáo gói ghi bản en nhiều lỗi hơn bản vi (37 so với 32 ở cỡ
        // 700) — duyệt ảnh chỉ ở bản ít lỗi hơn là duyệt nửa nhẹ của vấn đề (R-10). Id thêm mảnh "-en-" để hai nhánh xếp cạnh
        // nhau khi sắp theo tên.
        internal const string UxCalendarEnglish700 = "ux-calendar-en-700x560";
        internal const string UxCalendarEnglish820 = "ux-calendar-en-820x560";
        internal const string UxCalendarEnglish1024 = "ux-calendar-en-1024x700";
        internal const string UxCalendarEnglish1280 = "ux-calendar-en-1280x760";
        internal const string UxCalendarEnglish1440 = "ux-calendar-en-1440x900";
        internal const string UxCalendarEnglish1920 = "ux-calendar-en-1920x1040";

        internal const string UxCalendarSelectedEnglish700 = "ux-calendar-selected-en-700x560";
        internal const string UxCalendarSelectedEnglish820 = "ux-calendar-selected-en-820x560";
        internal const string UxCalendarSelectedEnglish1024 = "ux-calendar-selected-en-1024x700";
        internal const string UxCalendarSelectedEnglish1280 = "ux-calendar-selected-en-1280x760";
        internal const string UxCalendarSelectedEnglish1440 = "ux-calendar-selected-en-1440x900";
        internal const string UxCalendarSelectedEnglish1920 = "ux-calendar-selected-en-1920x1040";

        internal const string UxRecurringEnglish700 = "ux-recurring-en-700x560";
        internal const string UxRecurringEnglish820 = "ux-recurring-en-820x560";
        internal const string UxRecurringEnglish1024 = "ux-recurring-en-1024x700";
        internal const string UxRecurringEnglish1280 = "ux-recurring-en-1280x760";
        internal const string UxRecurringEnglish1440 = "ux-recurring-en-1440x900";
        internal const string UxRecurringEnglish1920 = "ux-recurring-en-1920x1040";

        /// <summary>
        /// W9-22(b): màn Lịch đang chọn NHIỀU đợt, ở 1024x700, hai ngôn ngữ. Bộ ảnh cũ chỉ có "chưa chọn" và "chọn một" —
        /// không ảnh nào cho thấy thanh hành động hàng loạt và dải tag của nhiều đợt cùng lúc, đúng trạng thái mà
        /// <c>Calendar_LayoutIsUsable_AtEverySize_MultiSelection</c> kiểm bằng máy.
        /// </summary>
        internal const string UxCalendarMulti1024 = "ux-calendar-multi-1024x700";

        internal const string UxCalendarMultiEnglish1024 = "ux-calendar-multi-en-1024x700";
    }
}
