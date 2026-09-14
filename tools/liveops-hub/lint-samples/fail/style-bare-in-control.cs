// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Shell/LiveOpsRailSample.cs
// Mẫu vi phạm: custom control tự gán `style.X` trần (không có `.` trước style) ngoài 10 chỗ được phép [FD §2.14].
// So sánh `style.width == 196` và đọc `style.left` không phải gán nên không tính.
// lint-expect: ui-inline-style
// lint-expect: ui-inline-style
// lint-expect: ui-inline-style
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    internal sealed class LiveOpsRailSample : VisualElement
    {
        public void Layout(float leftPixels)
        {
            style.width = 196;
            style.left = leftPixels;
            if (style.width == 196)
            {
                style.height += 2;
            }
            StyleLength current = style.left;
        }
    }
}
