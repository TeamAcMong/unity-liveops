using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Một mục miễn trừ của kiểm bố cục: màn, selector (tên element hoặc class USS của CHÍNH element bị phát hiện), loại phát hiện
    /// và LÝ DO. Lý do là bắt buộc vì đây là chỗ duy nhất cổng được phép im lặng — không có lý do thì người soát sau không biết
    /// mục này là thiết kế hay là lỗi ai đó giấu đi.
    /// </summary>
    internal sealed class UxLayoutAllowEntry
    {
        public UxLayoutAllowEntry(string screenId, string kind, string selector, string reason, bool matchAncestor = false)
        {
            if (string.IsNullOrEmpty(selector)) throw new ArgumentNullException(nameof(selector));
            if (string.IsNullOrEmpty(reason)) throw new ArgumentNullException(nameof(reason));
            ScreenId = screenId;
            Kind = kind;
            Selector = selector;
            Reason = reason;
            MatchAncestor = matchAncestor;
        }

        /// <summary>Màn áp dụng; <see cref="UxLayoutAllowList.AnyScreen"/> = mọi màn (phần tử của khung dùng chung).</summary>
        public string ScreenId { get; }

        /// <summary>Loại phát hiện (<see cref="UxLayoutFindingKinds"/>) — miễn trừ KHÔNG bao giờ mở cho mọi loại cùng lúc.</summary>
        public string Kind { get; }

        public string Selector { get; }
        public string Reason { get; }

        /// <summary>
        /// true = so khớp cả CHA của element bị phát hiện. Chỉ dùng khi class nằm trên control bọc ngoài còn phát hiện rơi vào
        /// element chữ bên trong nó (TextField: class của ô nằm trên TextField, chữ nằm ở <c>unity-text-element</c> bên trong).
        /// Mặc định false vì mỗi lần mở lên cha là một lần miễn trừ tha rộng hơn ý định.
        /// </summary>
        public bool MatchAncestor { get; }
    }

    /// <summary>
    /// Danh sách cắt CÓ CHỦ ĐÍCH của kiểm bố cục (§3.3). Luật của đợt W8: lỗi thật thì mở gói sửa, KHÔNG nhét vào đây; danh sách
    /// này chỉ dành cho chỗ thiết kế đã chọn cắt và người dùng vẫn đọc được thông tin ở nơi khác (hover card, inspector).
    /// <para>
    /// Cố tình để ngắn: mỗi dòng thêm vào là một lỗi cổng sẽ không bao giờ thấy nữa. Ba mục đầu là ba chỗ [SD1]/[SD2] nói rõ
    /// "rút gọn có ellipsis", và cả ba đều có đường đọc đầy đủ khác (hover card của thanh, tooltip chip, ô tìm tự cuộn khi gõ).
    /// </para>
    /// <para>
    /// So khớp theo ĐỊNH DANH của chính element (kiểu + tên + class), không theo cả câu chẩn đoán: câu chẩn đoán còn kèm mô tả
    /// cha cắt và chữ đang hiển thị, nên một mục miễn trừ khớp phần cha hoặc phần chữ sẽ im lặng tha luôn phát hiện của element
    /// khác (R-15).
    /// </para>
    /// </summary>
    internal static class UxLayoutAllowList
    {
        /// <summary>Mục áp dụng cho mọi màn — phần tử của khung (rail, status bar, toast) hiện ở màn nào cũng vậy.</summary>
        internal const string AnyScreen = "*";

        private static readonly UxLayoutAllowEntry[] Entries =
        {
            // (J2-03, 21/9/2026) Mục "liveops-hub-timeline-bar__label" ĐÃ RỜI khỏi đây sang UxTruncationExemption. Nó tha
            // đúng chỗ ấy, nhưng tha bằng LỜI KHAI: câu "tên đầy đủ đọc ở hover card và ở inspector" không được đo lại lượt
            // nào, nên gỡ tooltip của thanh đi thì cổng vẫn im lặng. Bản mới tha cùng chỗ mà đo cả hai điều kiện mỗi lượt.
            // Đừng khai lại ở đây: allow-list được hỏi TRƯỚC (xem UxLayoutAuditor.Add), nên một dòng ở đây làm điều kiện
            // của bản mới không bao giờ được hỏi tới.
            new UxLayoutAllowEntry(AnyScreen, UxLayoutFindingKinds.TextCut, "liveops-hub-chip-text",
                "Chip có max-width 240px kèm text-overflow: ellipsis ([SD1] §2.6) — chip là nhãn tóm tắt, câu đầy đủ nằm ở "
                + "status bar và ở card tương ứng."),
            new UxLayoutAllowEntry(AnyScreen, UxLayoutFindingKinds.TextCut, "liveops-hub-calendar-search",
                "Ô tìm cắt chữ đang gõ khi vượt bề rộng ô là hành vi của TextField: con trỏ tự cuộn theo, người dùng vẫn thấy "
                + "phần mình đang gõ. Khớp lên cha vì class nằm trên TextField còn chữ nằm ở unity-text-element bên trong.",
                true),
        };

        /// <summary>Mọi mục — dùng cho test tự kiểm danh sách và cho báo cáo của cổng người.</summary>
        public static IReadOnlyList<UxLayoutAllowEntry> All => Entries;

        /// <summary>
        /// Phát hiện loại <paramref name="kind"/> trên <paramref name="element"/> của màn <paramref name="screenId"/> có được
        /// miễn trừ không.
        /// </summary>
        public static bool Allows(string screenId, string kind, VisualElement element)
        {
            if (element == null) return false;
            foreach (UxLayoutAllowEntry allowed in Entries)
            {
                if (!string.Equals(allowed.Kind, kind, StringComparison.Ordinal)) continue;
                if (!string.Equals(allowed.ScreenId, AnyScreen, StringComparison.Ordinal)
                    && !string.Equals(allowed.ScreenId, screenId, StringComparison.Ordinal)) continue;
                if (Matches(allowed, element)) return true;
            }
            return false;
        }

        private static bool Matches(UxLayoutAllowEntry allowed, VisualElement element)
        {
            if (Identifies(allowed.Selector, element)) return true;
            if (!allowed.MatchAncestor) return false;
            for (VisualElement ancestor = element.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
            {
                if (Identifies(allowed.Selector, ancestor)) return true;
            }
            return false;
        }

        /// <summary>
        /// So khớp một selector (tên element hoặc class USS, có hay không có dấu <c>.</c>/<c>#</c> đứng trước) với một phần tử.
        /// <c>internal</c> vì <see cref="UxTruncationExemption"/> phải hỏi ĐÚNG câu hỏi này: hai luật miễn trừ dùng hai cách
        /// so khớp khác nhau là hai bảng nói hai thứ tiếng, và chỗ lệch nhau sẽ chỉ lộ ra ở một màn nào đó.
        /// </summary>
        internal static bool Identifies(string selector, VisualElement element)
        {
            string bare = selector[0] == '.' || selector[0] == '#' ? selector.Substring(1) : selector;
            if (string.Equals(element.name, bare, StringComparison.Ordinal)) return true;
            foreach (string className in element.GetClasses())
            {
                if (string.Equals(className, bare, StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
