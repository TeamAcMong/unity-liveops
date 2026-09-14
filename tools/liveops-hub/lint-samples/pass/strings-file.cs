// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Foundation/LiveOpsHubStrings.Sample.cs
// Mẫu đạt: chữ tiếng Việt chỉ nằm trong LiveOpsHubStrings.*.cs; phần partial không nhắc lại static vẫn hợp lệ khi phần khác có.
namespace DreamTech.LiveOps.Editor
{
    internal static partial class LiveOpsHubStrings
    {
        public const string CalendarTitle = "Lịch";
        // INTERIM(G-PASTE): lý do tạm của nút dán JSON — G-PASTE xoá hằng này.
        public const string InterimPasteNotBuiltReason = "Chưa có trong bản dev này";
    }

    internal partial class LiveOpsHubStrings
    {
        public const string ExportTitle = "Xuất JSON";
    }
}
