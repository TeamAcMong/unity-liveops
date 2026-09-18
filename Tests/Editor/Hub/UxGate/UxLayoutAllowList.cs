using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Một mục miễn trừ của kiểm bố cục: màn, selector (tên element hoặc class USS xuất hiện trong câu chẩn đoán), loại phát hiện
    /// và LÝ DO. Lý do là bắt buộc vì đây là chỗ duy nhất cổng được phép im lặng — không có lý do thì người soát sau không biết
    /// mục này là thiết kế hay là lỗi ai đó giấu đi.
    /// </summary>
    internal sealed class UxLayoutAllowEntry
    {
        public UxLayoutAllowEntry(string screenId, string kind, string selector, string reason)
        {
            if (string.IsNullOrEmpty(selector)) throw new ArgumentNullException(nameof(selector));
            if (string.IsNullOrEmpty(reason)) throw new ArgumentNullException(nameof(reason));
            ScreenId = screenId;
            Kind = kind;
            Selector = selector;
            Reason = reason;
        }

        /// <summary>Màn áp dụng; <see cref="UxLayoutAllowList.AnyScreen"/> = mọi màn (phần tử của khung dùng chung).</summary>
        public string ScreenId { get; }

        /// <summary>Loại phát hiện (<see cref="UxLayoutFindingKinds"/>) — miễn trừ KHÔNG bao giờ mở cho mọi loại cùng lúc.</summary>
        public string Kind { get; }

        public string Selector { get; }
        public string Reason { get; }
    }

    /// <summary>
    /// Danh sách cắt CÓ CHỦ ĐÍCH của kiểm bố cục (§3.3). Luật của đợt W8: lỗi thật thì mở gói sửa, KHÔNG nhét vào đây; danh sách
    /// này chỉ dành cho chỗ thiết kế đã chọn cắt và người dùng vẫn đọc được thông tin ở nơi khác (hover card, inspector).
    /// <para>
    /// Cố tình để ngắn: mỗi dòng thêm vào là một lỗi cổng sẽ không bao giờ thấy nữa. Ba mục đầu là ba chỗ [SD1]/[SD2] nói rõ
    /// "rút gọn có ellipsis", và cả ba đều có đường đọc đầy đủ khác (hover card của thanh, tooltip chip, ô tìm tự cuộn khi gõ).
    /// </para>
    /// </summary>
    internal static class UxLayoutAllowList
    {
        /// <summary>Mục áp dụng cho mọi màn — phần tử của khung (rail, status bar, toast) hiện ở màn nào cũng vậy.</summary>
        internal const string AnyScreen = "*";

        private static readonly UxLayoutAllowEntry[] Entries =
        {
            new UxLayoutAllowEntry(AnyScreen, UxLayoutFindingKinds.TextCut, "liveops-hub-timeline-bar__label",
                "Nhãn trong thanh timeline rút gọn theo bề rộng thanh là thiết kế ([SD2] §3.2, LiveOpsTimelineGeometry cắt bằng "
                + "TimelineBarLabelEllipsis): một đợt 2 giờ ở mức thu nhỏ tháng không thể chứa id đầy đủ. Tên đầy đủ đọc ở hover "
                + "card và ở inspector."),
            new UxLayoutAllowEntry(AnyScreen, UxLayoutFindingKinds.TextCut, "liveops-hub-chip-text",
                "Chip có max-width 240px kèm text-overflow: ellipsis ([SD1] §2.6) — chip là nhãn tóm tắt, câu đầy đủ nằm ở "
                + "status bar và ở card tương ứng."),
            new UxLayoutAllowEntry(AnyScreen, UxLayoutFindingKinds.TextCut, "liveops-hub-calendar-search",
                "Ô tìm cắt chữ đang gõ khi vượt bề rộng ô là hành vi của TextField: con trỏ tự cuộn theo, người dùng vẫn thấy "
                + "phần mình đang gõ."),
        };

        /// <summary>Mọi mục — dùng cho test tự kiểm danh sách và cho báo cáo của cổng người.</summary>
        public static IReadOnlyList<UxLayoutAllowEntry> All => Entries;

        /// <summary>
        /// Câu chẩn đoán <paramref name="entry"/> của màn <paramref name="screenId"/> có được miễn trừ không. So khớp bằng "câu
        /// chứa selector": câu chẩn đoán luôn ghi kèm <c>#tên</c> và các class của element, nên một selector đủ định danh.
        /// </summary>
        public static bool Allows(string screenId, string kind, string entry)
        {
            if (string.IsNullOrEmpty(entry)) return false;
            foreach (UxLayoutAllowEntry allowed in Entries)
            {
                if (!string.Equals(allowed.Kind, kind, StringComparison.Ordinal)) continue;
                if (!string.Equals(allowed.ScreenId, AnyScreen, StringComparison.Ordinal)
                    && !string.Equals(allowed.ScreenId, screenId, StringComparison.Ordinal)) continue;
                if (entry.IndexOf(allowed.Selector, StringComparison.Ordinal) >= 0) return true;
            }
            return false;
        }
    }
}
