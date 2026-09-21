namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// (W11) Id của hai ảnh bằng chứng cho nợ bố cục dữ liệu dài. Sống ở file riêng như bộ id ma trận cỡ cửa sổ của W8-UX:
    /// hai ảnh này KHÔNG thuộc ma trận truy vết 9.5 (chúng không so với hình thiết kế nào), nên chúng xoá được nguyên cụm
    /// khi đợt W11 đóng nợ mà không đụng bảng hằng đóng từ W0.
    /// </summary>
    internal static partial class LiveOpsHubCaptureScenarioIds
    {
        internal const string W11CalendarWorstData = "w11-calendar-worst-data-1280x760";

        internal const string W11EventTypesWorstData = "w11-event-types-worst-data-1280x760";
    }
}
