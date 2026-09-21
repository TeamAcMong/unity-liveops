// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Foundation/LiveOpsHubStrings.Sample.cs
// Mẫu đạt G-I18N §1.1: mọi thành viên LiveOpsHubStrings là property đọc catalog, khoá là nameof của chính nó; không còn
// const, không còn chuỗi viết thẳng. Phần partial không nhắc lại static vẫn hợp lệ khi phần khác có.
namespace DreamTech.LiveOps.Editor
{
    internal static partial class LiveOpsHubStrings
    {
        internal static string CalendarTitle => LiveOpsHubStringCatalog.Text(nameof(CalendarTitle));
        // INTERIM(G-PASTE): lý do tạm của nút dán JSON — G-PASTE xoá thành viên này.
        internal static string InterimPasteNotBuiltReason => LiveOpsHubStringCatalog.Text(nameof(InterimPasteNotBuiltReason));
    }

    internal partial class LiveOpsHubStrings
    {
        internal static string ExportTitle => LiveOpsHubStringCatalog.Text(nameof(ExportTitle));
    }
}
