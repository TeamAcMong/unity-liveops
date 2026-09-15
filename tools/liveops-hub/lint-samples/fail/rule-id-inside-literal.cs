// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Sections/Validation/ValidationRowSample.cs
// Mẫu vi phạm CC-FT-3: chuỗi CHỨA id luật (không bằng đúng id) ngoài LiveOpsFindingText.cs/LiveOpsHubStrings.Findings.cs.
// Hai dòng đầu phải bị bắt; hai dòng sau chỉ giống id (tiền tố/hậu tố cùng loại ký tự) nên không báo.
// lint-expect: rule-id-literal
// lint-expect: rule-id-literal
namespace DreamTech.LiveOps.Editor
{
    internal sealed class ValidationRowSample
    {
        public static string Describe(string target)
        {
            string failed = "Rule overlap-same-type failed: " + target;
            string detail = "{0} · invalid-identifier/empty";
            string ussClass = "liveops-overlap-same-type-row";
            string plural = "overlap-same-types";
            return failed + detail + ussClass + plural;
        }
    }
}
