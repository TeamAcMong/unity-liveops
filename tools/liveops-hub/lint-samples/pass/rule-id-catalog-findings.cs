// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Foundation/Language/LiveOpsHubStringCatalog.Findings.cs
// Mẫu đạt CC-FT-3 + G-I18N: câu có id luật nay nằm ở file catalog vùng Findings (một trong hai nơi 6.1 cho phép), và mỗi
// dòng đăng ký có đủ hai ngôn ngữ gọi theo tên.
namespace DreamTech.LiveOps.Editor
{
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterFindings(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.FindingRuleCrashedSample),
                vietnamese: "Luật long-gap-between-events không chạy được: {0}",
                english: "Rule long-gap-between-events crashed: {0}");
            table.AddShared(nameof(LiveOpsHubStrings.FindingSeparatorSample), " · ");
        }
    }
}
