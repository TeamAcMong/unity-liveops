using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class cửa sổ hẹp trên root ([FD §4.2], 8.8): <c>--medium</c> dưới 1100, <c>--narrow</c> dưới 900, <c>--compact</c> dưới 720,
    /// bật CỘNG DỒN (820 px = medium + narrow). Vì sao class thay cho <c>@media</c>: USS của UI Toolkit không có media query;
    /// C# chỉ đổi class trong <c>GeometryChangedEvent</c>, mọi thay đổi bố cục nằm ở USS của thành phần (chỗ 2 của [FD §2.14]).
    /// </summary>
    internal static class LiveOpsHubBreakpoints
    {
        public const float MediumBelowWidth = 1100f;
        public const float NarrowBelowWidth = 900f;
        public const float CompactBelowWidth = 720f;

        /// <summary>Đăng ký trên root; trả hàm gỡ để OnDisable không để lại callback trên cây cũ.</summary>
        public static Action Attach(VisualElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            EventCallback<GeometryChangedEvent> callback = geometryEvent => Apply(root, geometryEvent.newRect.width);
            root.RegisterCallback(callback);
            return () => root.UnregisterCallback(callback);
        }

        /// <summary>
        /// Bề rộng NaN hoặc ≤ 0 (chưa layout, <c>-nographics</c>) không đổi class: không có số đo thì không kết luận cửa sổ hẹp.
        /// </summary>
        public static void Apply(VisualElement root, float width)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (float.IsNaN(width) || width <= 0f) return;
            // INTERIM(G-SHELLPOLISH): --narrow chỉ bật class; rail vẫn 196 px, rail 36 px + menu tầng làm ở G-SHELLPOLISH (mục 12 I-8).
            root.EnableInClassList(LiveOpsHubClassNames.Medium, width < MediumBelowWidth);
            root.EnableInClassList(LiveOpsHubClassNames.Narrow, width < NarrowBelowWidth);
            root.EnableInClassList(LiveOpsHubClassNames.Compact, width < CompactBelowWidth);
        }
    }
}
