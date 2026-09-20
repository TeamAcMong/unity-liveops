using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Class cửa sổ hẹp trên root ([FD §4.2], 8.8): <c>--medium</c> dưới 1100, <c>--snug</c> dưới 1000, <c>--narrow</c> dưới 900,
    /// <c>--compact</c> dưới 720, bật CỘNG DỒN (820 px = medium + snug + narrow). Vì sao class thay cho <c>@media</c>: USS của
    /// UI Toolkit không có media query; C# chỉ đổi class trong <c>GeometryChangedEvent</c>, mọi thay đổi bố cục nằm ở USS của
    /// thành phần (chỗ 2 của [FD §2.14]).
    /// </summary>
    internal static class LiveOpsHubBreakpoints
    {
        public const float MediumBelowWidth = 1100f;

        /// <summary>
        /// Bậc giữa <c>--medium</c> và <c>--narrow</c> (W9-20). Vì sao cần: <c>--medium</c> trải 900…1099 — 200px cho MỘT
        /// mức nhường duy nhất. Thanh công cụ màn Lịch nhường menu "Bắt lưới" ngay từ 1099 để bản tiếng Việt ở 1024 không bị
        /// cắt, nhưng ở 950 thanh vẫn chật thêm một control nữa; không có bậc này thì chỗ nhường kế tiếp phải lùi tới 900 và
        /// dải 900…999 cắt chữ trong im lặng (không cỡ nào của ma trận cổng rơi vào đó trước W9-20).
        /// </summary>
        public const float SnugBelowWidth = 1000f;

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
            // --narrow chỉ BẬT CLASS; rail 36 px, menu tầng và nút ghim nằm ở USS + LiveOpsHubRail (mục 12 I-8, [FD §3.7]) —
            // lớp này không bao giờ biết một breakpoint làm gì, chỉ biết bề rộng nào thuộc lớp nào.
            root.EnableInClassList(LiveOpsHubClassNames.Medium, width < MediumBelowWidth);
            root.EnableInClassList(LiveOpsHubClassNames.Snug, width < SnugBelowWidth);
            root.EnableInClassList(LiveOpsHubClassNames.Narrow, width < NarrowBelowWidth);
            root.EnableInClassList(LiveOpsHubClassNames.Compact, width < CompactBelowWidth);
        }
    }
}
