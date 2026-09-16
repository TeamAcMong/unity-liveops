namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Đường dẫn dự án riêng của vùng Overview (V-5). UXML/USS của màn đã khai ở <c>LiveOpsHubPaths.cs</c> (mục 2); file này chỉ
    /// giữ đường dẫn mặc định của hộp "Tạo asset lịch…" — không phải chữ hiển thị nên không đi qua catalog, nhưng cũng không
    /// được rải thành literal trong màn.
    /// </summary>
    internal static partial class LiveOpsHubPaths
    {
        /// <summary>Thư mục mặc định của <c>SaveFilePanelInProject</c> khi chưa có lịch nào (7.1 trạng thái (a)).</summary>
        internal const string DefaultCalendarAssetDirectory = "Assets/LiveOps/Calendars";

        /// <summary>Tên file mặc định (không đuôi) — hộp lưu tự thêm ".asset".</summary>
        internal const string DefaultCalendarAssetName = "Main";

        internal const string CalendarAssetExtension = "asset";
    }
}
