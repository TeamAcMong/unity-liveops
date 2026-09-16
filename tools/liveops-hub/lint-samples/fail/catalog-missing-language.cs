// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Foundation/Language/LiveOpsHubStringCatalog.Sample.cs
// Mẫu vi phạm G-I18N §4: dòng đăng ký thiếu một bản, và dòng gọi theo vị trí (không theo tên) nên không soát được bản nào là bản nào.
// lint-expect: catalog-entry-needs-both-languages
// lint-expect: catalog-entry-needs-both-languages
namespace DreamTech.LiveOps.Editor
{
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterSample(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.SampleOnlyVietnamese),
                vietnamese: "Chỉ có bản gốc");
            table.Add(nameof(LiveOpsHubStrings.SamplePositional), "Bản gốc", "Source text");
        }
    }
}
