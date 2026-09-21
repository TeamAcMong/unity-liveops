namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Đường dẫn và khoá lưu riêng của vùng Export (V-5). UXML/USS của màn và của hộp Đánh dấu đã đăng đã khai ở
    /// <c>LiveOpsHubPaths.cs</c> (mục 2); file này giữ thư mục lưu file JSON, tên file theo sha và khoá EditorPrefs nhớ thư
    /// mục lần trước — không phải chữ hiển thị nên không đi qua catalog, nhưng cũng không được rải thành literal trong màn.
    /// </summary>
    internal static partial class LiveOpsHubPaths
    {
        /// <summary>Khoá EditorPrefs nhớ thư mục lưu file gần nhất [SD2 §3.10] (c′).</summary>
        internal const string ExportDirectoryPreferenceKey = "LiveOpsHub.ExportDirectory";

        /// <summary>Thư mục mặc định dưới <c>~/Documents</c> khi chưa lưu lần nào (tạo nếu thiếu).</summary>
        internal const string ExportDefaultDirectoryName = "LiveOps";

        /// <summary>Tên file xuất: <c>liveops_calendar-&lt;sha 6 ký tự&gt;.json</c> — sha trong tên để đối chiếu với dấu đã đăng.</summary>
        internal const string ExportFileNameFormat = "liveops_calendar-{0}";

        internal const string ExportFileExtension = "json";
    }
}
