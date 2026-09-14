// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Controls/LiveOpsTimelineBarSample.cs
// Mẫu đạt: `style.X` trần trong custom control có đánh dấu chỗ được phép; biến tên chứa "style" không bị bắt nhầm.
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    internal sealed class LiveOpsTimelineBarSample : VisualElement
    {
        public void Place(float leftPixels, float widthPixels)
        {
            style.left = leftPixels; // style-inline-allowed: 3
            style.width = widthPixels; // style-inline-allowed: 4
            bool isWide = style.width == widthPixels;
        }
    }
}
