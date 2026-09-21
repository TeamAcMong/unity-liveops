// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Controls/LiveOpsSampleControl.cs
// Mẫu đạt: control đăng ký hai đường đúng nhánh #if, PreventDefault/IgnoreEvent và sortingMode/sortingEnabled đúng bản.
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
#endif
    public sealed partial class LiveOpsSampleControl : VisualElement
    {
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("zoom-level")]
#endif
        public int ZoomLevel { get; set; }

#if !UNITY_2023_2_OR_NEWER
        public new sealed class UxmlFactory : UxmlFactory<LiveOpsSampleControl, UxmlTraits> { }

        public new sealed class UxmlTraits : VisualElement.UxmlTraits { }
#endif

        private void OnNavigationMove(NavigationMoveEvent navigationEvent)
        {
#if UNITY_2023_2_OR_NEWER
            navigationEvent.StopPropagation();
            focusController.IgnoreEvent(navigationEvent);
#else
            navigationEvent.PreventDefault();
#endif
        }

        private static void ConfigureSorting(MultiColumnListView table)
        {
#if UNITY_6000_0_OR_NEWER
            table.sortingMode = ColumnSortingMode.Default;
#elif UNITY_2022_3_OR_NEWER
            table.sortingEnabled = true;
#endif
        }
    }
}
