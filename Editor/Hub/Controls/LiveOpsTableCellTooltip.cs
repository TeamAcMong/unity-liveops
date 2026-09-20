using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Tooltip "đủ chữ" cho ô bảng có BỀ RỘNG CỐ ĐỊNH — điều kiện (a) của luật rút gọn có điều kiện mà user chốt 21/9/2026:
    /// ô của cột đã khai bề rộng được phép rút chữ, nhưng chỉ khi chữ đầy đủ còn đọc được ở tooltip VÀ ở một chỗ khác trong màn.
    /// <para>
    /// Vì sao tooltip đặt theo SỐ ĐO chứ không đặt luôn cho mọi ô: tooltip lặp lại đúng thứ đang nhìn thấy là nhiễu — người
    /// dùng rê chuột lên một ô đọc được và nhận lại y hệt chữ ấy. Ô chỉ mang tooltip khi chữ THẬT SỰ không nằm vừa ô, tức đúng
    /// lúc tooltip là đường đọc duy nhất.
    /// </para>
    /// <para>
    /// Vì sao phải có cả <see cref="Attach"/> lẫn lượt đặt trong <see cref="Bind"/>: <c>MultiColumnListView</c> tái dùng ô
    /// (bindCell chạy lại trên một ô đã có bố cục — lúc ấy đo được ngay), nhưng lần dựng ĐẦU tiên thì bindCell chạy TRƯỚC khi
    /// ô có bố cục, và cột co giãn theo bề rộng cửa sổ nên cùng một chuỗi lúc vừa lúc không. Một lượt đo ở bind cộng một lượt
    /// đo mỗi lần hình học đổi là đủ phủ cả ba đường ấy.
    /// </para>
    /// </summary>
    internal static class LiveOpsTableCellTooltip
    {
        /// <summary>
        /// Sai số của phép đo chữ không được cộng vào đây. Chữ cần 100,4px trong ô 100px là chữ NGƯỜI DÙNG đọc thiếu — dù
        /// lưới của cổng bỏ qua mức ấy như nhiễu đo (<c>UxLayoutAuditor.TextMeasureTolerance</c>). Đặt tooltip rộng tay hơn
        /// lưới là đúng chiều an toàn: chỗ nào cổng kêu thì chắc chắn đã có tooltip.
        /// </summary>
        private const float OverflowEpsilon = 0f;

        /// <summary>Gắn lượt đo lại theo hình học. Gọi MỘT lần lúc dựng ô (<c>makeCell</c>), không gọi lại ở mỗi lần bind.</summary>
        internal static void Attach(Label cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            cell.RegisterCallback<GeometryChangedEvent>(geometryEvent => Apply((Label)geometryEvent.currentTarget));
        }

        /// <summary>Đặt chữ của ô rồi đo lại ngay: ô tái dùng đã có bố cục nên không phải chờ lượt hình học nào.</summary>
        internal static void Bind(Label cell, string text)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            cell.text = text ?? string.Empty;
            Apply(cell);
        }

        /// <summary>
        /// Ô có chứa hết chữ của mình không; chưa có bố cục thì trả false và KHÔNG đụng tooltip — lượt
        /// <c>GeometryChangedEvent</c> sau sẽ trả lời.
        /// </summary>
        private static void Apply(Label cell)
        {
            float available = cell.contentRect.width;
            if (float.IsNaN(available) || available <= 0f) return;
            string text = cell.text ?? string.Empty;
            if (text.Length == 0)
            {
                cell.tooltip = string.Empty;
                return;
            }
            float needed = cell.MeasureTextSize(text, 0f, VisualElement.MeasureMode.Undefined, 0f,
                VisualElement.MeasureMode.Undefined).x;
            cell.tooltip = needed > available + OverflowEpsilon ? text : string.Empty;
        }
    }
}
