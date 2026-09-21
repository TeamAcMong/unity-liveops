using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bọc một nút có thể bị khoá: tooltip lý do nằm trên slot (element disabled không nhận hover nên tooltip đặt trên
    /// chính nút có thể không hiện — R-16) và lý do ngắn in ngay bên trái nút ([FD §2.16]: "Chặn: 2 đợt bị bỏ"). Lý do
    /// in cạnh nút luôn hiện khi khoá nên người dùng vẫn biết vì sao kể cả khi SP-3 (G-SPIKE-B) kết luận slot cũng không
    /// nhận tooltip ở một bản Unity; Label lý do mang cùng tooltip làm đường dự phòng.
    /// </summary>
    internal sealed class LiveOpsButtonSlot : VisualElement
    {
        public LiveOpsButtonSlot(Button button)
        {
            Button = button ?? throw new ArgumentNullException(nameof(button));
            AddToClassList(LiveOpsHubClassNames.ButtonSlot);
            ReasonLabel = new Label();
            ReasonLabel.AddToClassList(LiveOpsHubClassNames.ButtonReason);
            ReasonLabel.AddToClassList(LiveOpsHubClassNames.TextBlocked);
            Add(ReasonLabel);
            Add(Button);
        }

        public Button Button { get; }

        public Label ReasonLabel { get; }

        public bool IsBlocked => ClassListContains(LiveOpsHubClassNames.ButtonSlotBlocked);

        /// <summary>Lý do đang khoá; "" khi nút mở.</summary>
        public string Reason => IsBlocked ? ReasonLabel.text : string.Empty;

        /// <summary>
        /// Khoá khi <paramref name="enabled"/> = false thì <paramref name="reason"/> bắt buộc: nút disabled không nói vì
        /// sao là người dùng kẹt ("Disabled luôn kèm lý do"). Mở nút thì xoá lý do và tooltip.
        /// </summary>
        public void SetEnabledWithReason(bool enabled, string reason)
        {
            if (!enabled && string.IsNullOrEmpty(reason))
            {
                throw new ArgumentException(LiveOpsHubStrings.DisabledReasonRequired, nameof(reason));
            }
            Button.SetEnabled(enabled);
            string shownReason = enabled ? string.Empty : reason;
            ReasonLabel.text = shownReason;
            ReasonLabel.tooltip = shownReason;
            tooltip = shownReason;
            EnableInClassList(LiveOpsHubClassNames.ButtonSlotBlocked, !enabled);
        }
    }
}
