// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Services/LiveOpsFindingText.cs
// Mẫu đạt CC-FT-3: nguồn câu duy nhất LiveOpsFindingText.cs được viết chuỗi chứa id luật (6.1, V-8).
namespace DreamTech.LiveOps.Editor
{
    internal static class LiveOpsFindingText
    {
        public static string RuleIdLineSample(string target)
        {
            return "overlap-same-type · " + target;
        }
    }
}
