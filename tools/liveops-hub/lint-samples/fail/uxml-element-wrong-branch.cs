// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Controls/LiveOpsWrongBranchControl.cs
// Mẫu vi phạm: [UxmlElement] trong nhánh 2022.3; PreventDefault ngoài #if; sortingEnabled trong nhánh 2023.2+
// (2023.2+ gồm cả 6000.x nơi sortingEnabled là Obsolete); IgnoreEvent trong #elif của nhánh cũ.
// lint-expect: if-branch
// lint-expect: if-branch
// lint-expect: if-branch
// lint-expect: if-branch
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
#if !UNITY_2023_2_OR_NEWER
    [UxmlElement]
#endif
    public sealed partial class LiveOpsWrongBranchControl : VisualElement
    {
        private void OnNavigationMove(NavigationMoveEvent navigationEvent)
        {
            navigationEvent.PreventDefault();
#if UNITY_6000_0_OR_NEWER
            navigationEvent.StopPropagation();
#elif UNITY_2022_3_OR_NEWER
            focusController.IgnoreEvent(navigationEvent);
#endif
        }

        private static void ConfigureSorting(MultiColumnListView table)
        {
#if UNITY_2023_2_OR_NEWER
            table.sortingEnabled = true;
#endif
        }
    }
}
