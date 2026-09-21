using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Theo dõi skin Editor bằng probe USS thay vì poll <c>EditorGUIUtility.isProSkin</c> ([FD §2.1], SP-5 đã kiểm chứng hai
    /// bản). Vì sao: đổi Theme khi hub đang dock và hiện nhưng không có focus thì <c>OnFocus</c> không chạy, trong khi
    /// <c>--unity-colors-*</c> đã đổi ngay. Root khai <c>--liveops-hub-skin-probe: var(--unity-colors-window-background)</c>;
    /// mỗi lần <c>CustomStyleResolvedEvent</c> bắn, lớp này đọc màu nền cửa sổ và bật/tắt đúng MỘT class
    /// <c>liveops-hub--skin-light</c> trên root — không đổi skin thật, không tháo/gắn stylesheet (đổi skin bằng code ghi
    /// Preferences toàn máy và treo batchmode — SP-4).
    /// </summary>
    internal sealed class LiveOpsHubSkin
    {
        internal const string SkinProbePropertyName = "--liveops-hub-skin-probe";

        /// <summary>
        /// Ngưỡng độ sáng (0–1, trung bình có trọng số kênh gamma) tách nền tối/sáng. Nền cửa sổ thật: Dark #383838 ≈ 0,22,
        /// Light #C8C8C8 ≈ 0,78 — ngưỡng giữa hai bên để nền tuỳ biến lệch vài mức vẫn phân loại đúng.
        /// </summary>
        internal const float LightBackgroundThreshold = 0.5f;

        private static readonly CustomStyleProperty<Color> SkinProbeProperty = new CustomStyleProperty<Color>(SkinProbePropertyName);

        private readonly VisualElement _root;

        private LiveOpsHubSkin(VisualElement root)
        {
            _root = root;
        }

        /// <summary>Skin vừa đổi (class đã bật/tắt, cache icon đã xoá) — cửa sổ dựng lại phần chrome có icon.</summary>
        public event Action SkinChanged;

        public bool IsLight => _root.ClassListContains(LiveOpsHubClassNames.SkinLight);

        /// <summary>Số lần class skin thật sự đổi — test đọc để chứng minh probe không bật lại vô ích.</summary>
        internal int ChangeCount { get; private set; }

        /// <summary>Bật class theo <c>isProSkin</c> (8.1 bước 4) rồi đăng ký probe. Trả đối tượng để <see cref="Detach"/> ở OnDisable.</summary>
        public static LiveOpsHubSkin Attach(VisualElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            root.EnableInClassList(LiveOpsHubClassNames.SkinLight, !EditorGUIUtility.isProSkin);
            LiveOpsHubSkin skin = new LiveOpsHubSkin(root);
            root.RegisterCallback<CustomStyleResolvedEvent>(skin.OnCustomStyleResolved);
            return skin;
        }

        public void Detach()
        {
            _root.UnregisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        /// <summary>OnFocus của cửa sổ gọi lần nữa làm dự phòng khi probe chưa bắn (vd root chưa có panel lúc đổi Theme).</summary>
        public void ApplyFromEditorSkin()
        {
            SetLight(!EditorGUIUtility.isProSkin);
        }

        /// <summary>Hàm thuần của quyết định sáng/tối — tách để test màu biên mà không cần panel.</summary>
        internal static bool IsLightBackground(Color background)
        {
            float brightness = 0.2126f * background.r + 0.7152f * background.g + 0.0722f * background.b;
            return brightness >= LightBackgroundThreshold;
        }

        internal void ApplyProbeColor(Color probeColor)
        {
            SetLight(IsLightBackground(probeColor));
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent resolvedEvent)
        {
            // Chỉ đọc probe của chính root: sự kiện của con không nổi lên (không bubble), nhưng vẫn kiểm target cho chắc khi
            // một gói sau đăng ký cùng callback trên element khác.
            if (resolvedEvent.target != _root) return;
            if (!resolvedEvent.customStyle.TryGetValue(SkinProbeProperty, out Color probeColor)) return;
            ApplyProbeColor(probeColor);
        }

        private void SetLight(bool isLight)
        {
            if (IsLight == isLight) return;
            _root.EnableInClassList(LiveOpsHubClassNames.SkinLight, isLight);
            ChangeCount++;
            LiveOpsHubIcons.ClearCache();
            SkinChanged?.Invoke();
        }
    }
}
