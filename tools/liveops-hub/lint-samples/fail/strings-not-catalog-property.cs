// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Foundation/LiveOpsHubStrings.Sample.cs
// Mẫu vi phạm G-I18N §1.1: hằng chữ cũ, chuỗi viết thẳng, và property có khoá lệch tên thành viên.
// lint-expect: strings-must-be-catalog-property
// lint-expect: display-text-only-in-catalog
// lint-expect: display-text-only-in-catalog
// lint-expect: strings-must-be-catalog-property
// lint-expect: strings-must-be-catalog-property
namespace DreamTech.LiveOps.Editor
{
    internal static partial class LiveOpsHubStrings
    {
        internal const string CalendarTitle = "Lịch";
        internal static string ExportTitle => "Xuất JSON";
        internal static string ShellWindowTitle => LiveOpsHubStringCatalog.Text(nameof(CalendarTitle));
    }
}
