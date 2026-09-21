using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Phép đo tương phản của màu chữ ĐÃ HỢP THÀNH — màu khai × opacity của MỌI tổ tiên, hợp lên nền đục gần nhất.
    /// <para>
    /// Vì sao tách khỏi <see cref="UxContrastTokenTests"/>: cùng một phép đo nay chạy ở HAI chỗ — trong lượt EditMode (skin
    /// đang chạy của cổng, hiện là skin TỐI) và trong lượt chụp của <c>capture.sh</c> (skin SÁNG, phiếu W9-27). Hai bản sao
    /// của cùng công thức là hai con số khác nhau về cùng một cửa sổ, đúng loại sai mà phát hiện A-01 đã tốn một đợt để dọn.
    /// </para>
    /// </summary>
    // Category là dấu cho code-lint (luật test-ui-category): phép đo đọc resolvedStyle nên chỉ có nghĩa khi Unity CÓ đồ hoạ.
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static class UxComposedContrast
    {
        /// <summary>Bậc WCAG 2.1 AA cho chữ thường — chốt của USER 19/9/2026.</summary>
        internal const float TextContrastRatio = 4.5f;

        /// <summary>
        /// Bậc WCAG 2.1 AA cho chữ to và cho thành phần đồ hoạ / thành phần giao diện (viền, ô màu, đường kẻ) — nửa còn lại
        /// của chốt USER 19/9/2026. Ở đây cùng với bậc chữ vì từ cổng đợt vét W10 cả hai bậc đều có chỗ dùng trong phép đo
        /// màu ĐÃ HỢP THÀNH (phiếu W10-09: viền swatch loại chưa khai báo nằm trong ô mờ 0,7).
        /// </summary>
        internal const float ShapeContrastRatio = 3f;

        /// <summary>
        /// Mọi chữ ĐANG HIỆN trong <paramref name="root"/> phải đạt bậc chữ sau khi nhân opacity của tổ tiên. Phần tử không
        /// hoạt động được bỏ qua theo WCAG 2.1 §1.4.3 ("thành phần giao diện không hoạt động" không có yêu cầu tương phản);
        /// nơi gọi có trách nhiệm khẳng định riêng rằng thứ nó quan tâm KHÔNG nằm trong diện miễn trừ đó.
        /// </summary>
        /// <returns>Số đoạn chữ THẬT SỰ được đo — nơi gọi dùng nó để không kết luận từ một vòng lặp rỗng.</returns>
        internal static int CollectFailures(VisualElement root, string place, List<string> failures)
        {
            if (root == null) return 0;
            int measuredCount = 0;
            List<TextElement> texts = new List<TextElement>();
            root.Query<TextElement>().ToList(texts);
            for (int index = 0; index < texts.Count; index++)
            {
                TextElement text = texts[index];
                if (string.IsNullOrEmpty(text.text)) continue;
                if (!UxLayoutAuditor.IsShownOnScreen(text)) continue;
                if (!text.enabledInHierarchy) continue;
                measuredCount++;

                Color declared = text.resolvedStyle.color;
                float opacity = EffectiveOpacity(text);
                Color faded = new Color(declared.r, declared.g, declared.b, declared.a * opacity);
                // Nền của CHỮ tính cả nền của chính Label (cờ "bây giờ" của thước tự khai nền đục) — xem TextBackdropOf.
                Color background = UxLayoutAuditor.TextBackdropOf(text);
                Color seen = UxLayoutAuditor.CompositeOver(faded, background);
                float ratio = UxLayoutAuditor.ContrastRatio(seen, background);
                if (ratio >= TextContrastRatio) continue;
                failures.Add(place + " · \"" + text.text + "\": " + Number(ratio) + ":1 — màu " + HexText(declared)
                    + " nhân opacity " + Number(opacity) + " hợp thành ra " + HexText(seen) + " trên nền "
                    + HexText(background) + " (cần ≥ " + Number(TextContrastRatio) + ":1)");
            }

            return measuredCount;
        }

        /// <summary>
        /// Opacity mà mắt người thật sự thấy trên một phần tử: tích opacity của chính nó và của MỌI tổ tiên. UI Toolkit nhân
        /// opacity theo từng lớp lúc vẽ, nên đọc mỗi <c>resolvedStyle.opacity</c> của element là đọc thiếu đúng phần mà
        /// <c>:disabled</c> của Unity đặt lên khối cha.
        /// </summary>
        internal static float EffectiveOpacity(VisualElement element)
        {
            float opacity = 1f;
            for (VisualElement current = element; current != null; current = current.hierarchy.parent)
            {
                opacity *= Mathf.Clamp01(current.resolvedStyle.opacity);
            }
            return opacity;
        }

        internal static string Number(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        internal static string HexText(Color color)
        {
            Color32 bytes = color;
            return "#" + bytes.r.ToString("X2", CultureInfo.InvariantCulture)
                + bytes.g.ToString("X2", CultureInfo.InvariantCulture)
                + bytes.b.ToString("X2", CultureInfo.InvariantCulture)
                + " a" + Number(color.a);
        }
    }
}
