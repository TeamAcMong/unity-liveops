using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// "Quyết định… ▾" ([SD2 §2.4]) — <c>ToolbarMenu</c> có chevron THAY CHỖ nút khi phát hiện có hai cách đều đúng
    /// (<c>running-event-id-changed</c>: giữ tiền tố cũ, hoặc để sau khi đợt đang chạy khép). Vì sao là menu chứ không phải
    /// hai nút: hai cách đúng không có cái nào là mặc định, mà hai nút cạnh nhau luôn đọc thành "cái bên phải là cái nên bấm".
    /// <para>
    /// Menu KHÔNG tự sửa lịch: nó gọi lại <c>apply</c> của màn để đi đúng một đường với mọi lệnh sửa khác (một Undo group,
    /// một toast, rồi tự kiểm lại — PD-10). Chữ từng mục lấy nguyên từ <see cref="LiveOpsFindingText"/> (V-8) ở dạng phẳng:
    /// menu gốc của Unity không vẽ rich text.
    /// </para>
    /// </summary>
    internal sealed class DecisionMenu
    {
        private readonly List<string> _itemLabels = new List<string>();

        internal DecisionMenu(LiveEventCalendarFinding finding, LiveOpsHubFormat format, Action<LiveEventCalendarRepair> apply)
            : this(finding, format, string.Empty, apply)
        {
        }

        /// <param name="actionTooltip">
        /// Câu tooltip của hàng (<c>ValidationRow.ActionTooltip</c>, tức <c>LiveOpsFindingText.PrimaryButtonTooltip</c>).
        /// </param>
        internal DecisionMenu(LiveEventCalendarFinding finding, LiveOpsHubFormat format, string actionTooltip,
            Action<LiveEventCalendarRepair> apply)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            if (format == null) throw new ArgumentNullException(nameof(format));

            Element = new ToolbarMenu { text = LiveOpsFindingText.PrimaryButtonText(finding) };
            // Tooltip của hàng Quyết định ([SD2 §2.3] bảng luật 3: "Hai cách đúng: giữ tiền tố weekly-pass-, hoặc để sau khi
            // weekly-pass-35 khép (14/9 00:00 UTC)") đi theo NÚT. Nút đổi thành ToolbarMenu ở W5 thì câu đó rơi mất vì không
            // element nào mang nó nữa (soát W5 F-10) — menu là chỗ duy nhất còn lại để gắn.
            Element.tooltip = actionTooltip ?? string.Empty;
            Element.AddToClassList(LiveOpsHubClassNames.Button);
            for (int index = 0; index < finding.Repairs.Count; index++)
            {
                LiveEventCalendarRepair repair = finding.Repairs[index];
                string label = LiveOpsFindingText.PlainText(LiveOpsFindingText.RepairOptionText(finding, repair, format));
                _itemLabels.Add(label);
                Element.menu.AppendAction(label, action => apply?.Invoke(repair), DropdownMenuAction.AlwaysEnabled);
            }
        }

        /// <summary>Element cắm vào khối nút của hàng; hàng không biết gì về phiên nên màn là nơi dựng menu này.</summary>
        internal ToolbarMenu Element { get; }

        /// <summary>Nhãn từng mục theo thứ tự — menu gốc không chụp được (S-24) nên test đọc danh sách này.</summary>
        internal IReadOnlyList<string> ItemLabels => _itemLabels;
    }
}
