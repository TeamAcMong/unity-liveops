using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// View status bar 20 px ([FD §3.4]): trái = dấu 7 px + câu về lần kiểm gần nhất, phải = giờ UTC + tooltip giờ máy. Chữ do
    /// <c>LiveOpsHubStatusBarModel</c> (G-HOSTUI, W4) dựng; G-SHELL chỉ có view. Chưa có phiên lịch (W2) thì status bar để trống
    /// và ẩn dấu — một dấu vòng rỗng không kèm câu lý do sẽ bị đọc thành trạng thái thật.
    /// </summary>
    internal sealed class LiveOpsHubStatusBar
    {
        public LiveOpsHubStatusBar(VisualElement hubRoot)
        {
            if (hubRoot == null) throw new ArgumentNullException(nameof(hubRoot));
            Element = hubRoot.Q(LiveOpsHubPaths.ShellElementNames.StatusBar);
            LeftContainer = hubRoot.Q(LiveOpsHubPaths.ShellElementNames.StatusLeft);
            LeftLabel = hubRoot.Q<Label>(LiveOpsHubPaths.ShellElementNames.StatusLeftText);
            RightLabel = hubRoot.Q<Label>(LiveOpsHubPaths.ShellElementNames.StatusRight);

            // Dấu dựng bằng C# (cấu trúc của custom control — chỗ 8 [FD §2.14]) để UXML khung không phụ thuộc thẻ control dual-path.
            LeftMark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
            LeftMark.AddToClassList(LiveOpsHubClassNames.StatusMark);
            LeftContainer.Insert(0, LeftMark);
            Clear();
        }

        public VisualElement Element { get; }
        public VisualElement LeftContainer { get; }
        public LiveOpsStateMark LeftMark { get; }
        public Label LeftLabel { get; }
        public Label RightLabel { get; }

        /// <param name="mark">null = không dấu (câu tự đứng được, vd "Chưa kiểm lần nào — F5 để kiểm" đã nói lý do).</param>
        public void SetLeft(HealthState? mark, string text, string tooltip)
        {
            // Ẩn bằng class, không bằng setter visible (setter ghi style.visibility inline — trạng thái luôn bằng class [FD §2.14]).
            // visibility: hidden giữ chỗ 7 px để chữ không nhảy khi dấu bật/tắt.
            LeftMark.EnableInClassList(LiveOpsHubClassNames.StatusMarkHidden, !mark.HasValue);
            if (mark.HasValue) LeftMark.SetHealth(mark.Value);
            LeftLabel.text = text ?? string.Empty;
            LeftLabel.tooltip = string.IsNullOrEmpty(tooltip) ? LeftLabel.text : tooltip;
        }

        public void SetRight(string text, string tooltip)
        {
            RightLabel.text = text ?? string.Empty;
            RightLabel.tooltip = tooltip ?? string.Empty;
        }

        public void Clear()
        {
            SetLeft(null, string.Empty, string.Empty);
            SetRight(string.Empty, string.Empty);
        }
    }
}
