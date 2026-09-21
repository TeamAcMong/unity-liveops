namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class thành phần của vùng Foundation (V-5, G-HUBBASE) không có trong bảng 8.10: cỡ icon, spinner, hướng chevron,
    /// slot nút đang khoá. Cỡ và hướng là class (không phải style inline) vì [FD §2.14] chỉ cho C# gán hình học suy từ
    /// dữ liệu — cỡ icon và hướng mũi tên là quyết định trình bày.
    /// </summary>
    internal static partial class LiveOpsHubClassNames
    {
        // Icon — kích thước cố định + scaleMode ScaleToFit vì icon dựng sẵn có cỡ lệch (Search Icon 64×64, FolderOpened 256×256).
        internal const string Icon = "liveops-hub-icon";
        internal const string IconSize10 = "liveops-hub-icon--10";
        internal const string IconSize12 = "liveops-hub-icon--12";
        internal const string IconSize14 = "liveops-hub-icon--14";
        internal const string IconSize16 = "liveops-hub-icon--16";

        internal const string Spinner = "liveops-hub-spinner";

        internal const string ChevronRight = "liveops-hub-chevron--right";
        internal const string ChevronLeft = "liveops-hub-chevron--left";
        internal const string ChevronDown = "liveops-hub-chevron--down";
        internal const string ChevronUp = "liveops-hub-chevron--up";

        /// <summary>Slot có nút bị khoá: hiện Label lý do bên trái nút (USS), tooltip nằm trên slot.</summary>
        internal const string ButtonSlotBlocked = "liveops-hub-button-slot--blocked";
    }
}
