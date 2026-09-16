namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class thành phần của vùng Inspector (V-5, G-INSPECTOR) không có trong bảng 8.10: inspector gọn của asset lịch sống trong
    /// panel của cửa sổ Inspector, không phải trong cửa sổ hub, nên nó có stylesheet riêng và class riêng.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        /// <summary>Một dòng chữ của inspector — mang luật xuống dòng ([FD §2.14] không cho gán white-space inline).</summary>
        internal const string InspectorLine = "liveops-hub-inspector-line";
    }
}
