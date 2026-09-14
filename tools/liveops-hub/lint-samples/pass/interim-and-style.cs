// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Services/InterimUnavailableHubActions.cs
// Mẫu đạt: nhánh tạm có comment INTERIM trỏ gói có thật; style inline có số chỗ được phép.
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    // INTERIM(G-PASTE): action dán JSON chưa dựng — G-PASTE xoá file này.
    internal sealed class InterimUnavailableHubActions
    {
        public bool CanPasteRunningJson => false;
        public string Reason => LiveOpsHubStrings.InterimPasteNotBuiltReason;

        public static void Place(VisualElement bar, float leftPixels)
        {
            bar.style.left = leftPixels; // style-inline-allowed: 3
        }
    }
}
