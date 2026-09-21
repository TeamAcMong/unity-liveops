using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Gợi ý trong ô nhập: Label phủ lên vùng nhập của TextField ([FD §2.11]). 2022.3 không có placeholder cho TextField
    /// (<c>textEdition.placeholder</c> chỉ có từ Unity 6), nên phủ Label <c>pickingMode = Ignore</c> — chặn picking bằng USS
    /// không có tác dụng (picking-mode là thuộc tính UXML/C#), Label bắt click sẽ chặn cả IME tiếng Việt. Placeholder chỉ
    /// ẩn khi ô có giá trị: ô đang focus mà còn trống thì vẫn hiện gợi ý.
    /// </summary>
    internal sealed class LiveOpsPlaceholder : Label
    {
        private TextField _field;

        public LiveOpsPlaceholder(string text) : base(text)
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList(LiveOpsHubClassNames.Placeholder);
        }

        public TextField Field => _field;

        /// <summary>Tạo placeholder, gắn vào vùng nhập của <paramref name="field"/> và theo dõi giá trị của ô.</summary>
        public static LiveOpsPlaceholder Attach(TextField field, string text)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            LiveOpsPlaceholder placeholder = new LiveOpsPlaceholder(text);
            VisualElement input = field.Q(className: TextField.inputUssClassName) ?? field;
            input.Add(placeholder);
            placeholder.Track(field);
            return placeholder;
        }

        /// <summary>
        /// Theo dõi ô cho một Label placeholder khai trong UXML (<c>class="liveops-hub-placeholder" picking-mode="Ignore"</c>):
        /// ép lại pickingMode phòng khi UXML quên thuộc tính, rồi ẩn/hiện theo giá trị.
        /// </summary>
        public static void TrackExisting(TextField field, VisualElement placeholder)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (placeholder == null) throw new ArgumentNullException(nameof(placeholder));
            placeholder.pickingMode = PickingMode.Ignore;
            placeholder.AddToClassList(LiveOpsHubClassNames.Placeholder);
            field.RegisterValueChangedCallback(changeEvent => UpdateVisibility(placeholder, changeEvent.newValue));
            UpdateVisibility(placeholder, field.value);
        }

        /// <summary>Gọi sau <c>SetValueWithoutNotify</c> — gán không bắn ChangeEvent nên placeholder không tự biết.</summary>
        public void Refresh()
        {
            if (_field != null) UpdateVisibility(this, _field.value);
        }

        private void Track(TextField field)
        {
            _field = field;
            field.RegisterValueChangedCallback(changeEvent => UpdateVisibility(this, changeEvent.newValue));
            UpdateVisibility(this, field.value);
        }

        private static void UpdateVisibility(VisualElement placeholder, string value)
        {
            placeholder.EnableInClassList(LiveOpsHubClassNames.PlaceholderHidden, !string.IsNullOrEmpty(value));
        }
    }
}
