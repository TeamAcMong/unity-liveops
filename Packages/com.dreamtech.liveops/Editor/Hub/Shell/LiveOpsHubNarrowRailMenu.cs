using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Menu tầng của rail 36 px ([FD §3.7], mục 12 I-8): bấm một ô icon tầng mở <see cref="GenericMenu"/> liệt kê các màn của
    /// tầng đó, mỗi mục là "tên màn — lý do" ("Kiểm lịch — 2 bị bỏ"). Rail thu gọn không còn chỗ in lý do, nên lý do đi vào
    /// chính nhãn menu — không có tooltip nào trong <c>GenericMenu</c> để nói thêm.
    /// <para>
    /// Tách khỏi <see cref="LiveOpsHubRail"/> vì <c>GenericMenu.ShowAsContext</c> không chạy trong test batch: phần quyết ĐỊNH
    /// (mục nào, chữ gì, mục nào đang bật) nằm ở <see cref="BuildItems"/> — thuần, test đọc thẳng — còn phần MỞ cửa sổ nằm ở
    /// <see cref="Show"/>.
    /// </para>
    /// </summary>
    internal static class LiveOpsHubNarrowRailMenu
    {
        /// <summary>
        /// Một mục menu. <see cref="IsOn"/> = màn đang mở (GenericMenu vẽ dấu tích) — người dùng ở rail thu gọn không thấy
        /// hàng active nào nên dấu tích là chỗ duy nhất nói "bạn đang ở đây".
        /// </summary>
        internal readonly struct Item
        {
            internal Item(string sectionId, string text, bool isOn)
            {
                SectionId = sectionId;
                Text = text;
                IsOn = isOn;
            }

            internal string SectionId { get; }
            internal string Text { get; }
            internal bool IsOn { get; }
        }

        /// <param name="stage">Tầng của ô icon vừa bấm.</param>
        /// <param name="activeSectionId">Màn đang mở; "" khi chưa có.</param>
        internal static IReadOnlyList<Item> BuildItems(LiveOpsHubRailStageRow stage, string activeSectionId)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            List<Item> items = new List<Item>();
            IReadOnlyList<LiveOpsHubRailSectionRow> rows = stage.Rows;
            for (int index = 0; index < rows.Count; index++)
            {
                LiveOpsHubRailSectionRow row = rows[index];
                items.Add(new Item(row.SectionId, TextOf(row),
                    string.Equals(row.SectionId, activeSectionId ?? string.Empty, StringComparison.Ordinal)));
            }
            return items;
        }

        /// <summary>
        /// Nhãn một mục. Dấu '/' trong chữ sẽ biến mục thành menu con của Unity ("Kiểm lịch / overlap" thành hai tầng), nên
        /// thay bằng '∕' (U+2215) — cùng chữ với mắt người đọc, khác ký tự với bộ dựng menu.
        /// </summary>
        internal static string TextOf(LiveOpsHubRailSectionRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            string reason = row.Reason ?? string.Empty;
            string text = reason.Length == 0
                ? row.Title
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellRailNarrowMenuItemFormat, row.Title, reason);
            return text.Replace('/', DivisionSlash);
        }

        /// <summary>Mở menu ngay dưới ô icon vừa bấm; <paramref name="navigate"/> nhận id màn người dùng chọn.</summary>
        internal static void Show(Rect activatorWorldBound, LiveOpsHubRailStageRow stage, string activeSectionId, Action<string> navigate)
        {
            if (navigate == null) throw new ArgumentNullException(nameof(navigate));
            GenericMenu menu = new GenericMenu();
            IReadOnlyList<Item> items = BuildItems(stage, activeSectionId);
            for (int index = 0; index < items.Count; index++)
            {
                Item item = items[index];
                string sectionId = item.SectionId;
                menu.AddItem(new GUIContent(item.Text), item.IsOn, () => navigate(sectionId));
            }
            menu.DropDown(activatorWorldBound);
        }

        private const char DivisionSlash = '∕';
    }
}
