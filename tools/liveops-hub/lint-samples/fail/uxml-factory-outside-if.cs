// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Controls/LiveOpsBrokenControl.cs
// Mẫu vi phạm: UxmlFactory ngoài #if (lỗi compile 6000.6) và UxmlTraits trong nhánh 2023.2+.
// lint-expect: if-branch
// lint-expect: if-branch
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    public sealed partial class LiveOpsBrokenControl : VisualElement
    {
        public new sealed class UxmlFactory : UxmlFactory<LiveOpsBrokenControl, VisualElement.UxmlTraits> { }

#if UNITY_2023_2_OR_NEWER
        public new sealed class UxmlTraits : VisualElement.UxmlTraits { }
#endif
    }
}
