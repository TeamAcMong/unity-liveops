using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Một chỗ duy nhất bật "sắp theo cột, bảng tự sắp dữ liệu" cho <see cref="MultiColumnListView"/> ở hai bản Unity ([API §5]):
    /// 2022.3 chỉ có <c>sortingEnabled</c>; 6000.6 đánh dấu nó Obsolete (CS0618, có thể bị gỡ ở bản sau) và thay bằng
    /// <c>sortingMode = ColumnSortingMode.Custom</c> — enum chỉ có ở 6000.x. Viết <c>#if</c> rải ở từng bảng (Loại event, Lịch dạng
    /// danh sách, 5 đợt kế tiếp) thì một bảng quên nhánh là lỗi biên dịch ở một bản; gom về đây. Không dùng <c>Column.comparison</c>
    /// (chỉ có ở 6000.x): bảng tự sắp <c>itemsSource</c> trong <c>columnSortingChanged</c> rồi <c>RefreshItems()</c>.
    /// </summary>
    public static class LiveOpsTableSorting
    {
        /// <summary>Bật sắp khi bấm header cột; bảng nhận <c>columnSortingChanged</c> và tự sắp dữ liệu của mình.</summary>
        public static void EnableCustomSorting(MultiColumnListView table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
#if UNITY_6000_0_OR_NEWER
            table.sortingMode = ColumnSortingMode.Custom;
#else
            table.sortingEnabled = true;
#endif
        }

        /// <summary>Bảng đã bật sắp theo cột chưa — đọc đúng property của bản đang chạy.</summary>
        public static bool IsCustomSortingEnabled(MultiColumnListView table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
#if UNITY_6000_0_OR_NEWER
            return table.sortingMode == ColumnSortingMode.Custom;
#else
            return table.sortingEnabled;
#endif
        }
    }
}
