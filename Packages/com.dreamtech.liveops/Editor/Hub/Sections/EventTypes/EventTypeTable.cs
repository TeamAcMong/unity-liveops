using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng loại của màn Loại event ([SD1 §2.1]): <see cref="MultiColumnListView"/> tám cột 44 / 140 / giãn / 120 / 140 / 90 / 84 / 36.
    /// Bảng chỉ VẼ <see cref="EventTypeRow"/> do <see cref="EventTypesModel"/> dựng — không tự tính chữ, không đọc tài liệu.
    /// <para>
    /// Thứ tự mặc định là thứ tự làn trên Lịch (V-12); bấm tiêu đề cột mới sắp lại, và bỏ sắp thì về đúng thứ tự làn. Kéo hàng để
    /// đổi thứ tự làn KHÔNG có ở P1 (V-12: chỉ menu header làn của màn Lịch), nên bảng không đăng ký đường kéo nào.
    /// </para>
    /// </summary>
    internal sealed class EventTypeTable
    {
        internal const string ColorColumnName = "color";
        internal const string TypeIdColumnName = "type-id";
        internal const string DisplayNameColumnName = "display-name";
        internal const string EntryColumnName = "entry";
        internal const string ConfigKeyColumnName = "config-key";
        internal const string SourceColumnName = "source";
        internal const string EventCountColumnName = "event-count";
        internal const string StateColumnName = "state";

        private readonly List<EventTypeRow> _laneOrder = new List<EventTypeRow>();
        private readonly List<EventTypeRow> _visibleOrder = new List<EventTypeRow>();

        internal EventTypeTable()
        {
            View = new MultiColumnListView { name = LiveOpsHubPaths.EventTypesElementNames.Table, itemsSource = _visibleOrder };
            View.AddToClassList(LiveOpsHubClassNames.EventTypesTable);
            View.selectionType = SelectionType.Single;
            AddColumns();
            LiveOpsTableSorting.EnableCustomSorting(View);
            View.columnSortingChanged += ApplySorting;
            View.selectedIndicesChanged += OnSelectedIndicesChanged;
        }

        /// <summary>Loại đang chọn đổi (bấm hàng, ↑↓) — "" khi bỏ chọn.</summary>
        internal event Action<string> SelectionChanged;

        internal MultiColumnListView View { get; }

        /// <summary>Loại đang chọn; "" khi không có hàng nào được chọn.</summary>
        internal string SelectedTypeId
        {
            get
            {
                int index = View.selectedIndex;
                return index >= 0 && index < _visibleOrder.Count ? _visibleOrder[index].TypeId : string.Empty;
            }
        }

        /// <summary>Nạp hàng mới rồi giữ nguyên loại đang chọn nếu nó còn (sửa một field không được nhảy mất lựa chọn).</summary>
        internal void SetRows(IReadOnlyList<EventTypeRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            string previousTypeId = SelectedTypeId;
            _laneOrder.Clear();
            for (int index = 0; index < rows.Count; index++) _laneOrder.Add(rows[index]);
            ApplySorting();
            SelectType(previousTypeId, false);
        }

        /// <summary>Chọn một loại theo id; id không còn trong bảng thì bỏ chọn.</summary>
        internal void SelectType(string typeId, bool notify)
        {
            int index = IndexOf(typeId);
            if (index < 0)
            {
                View.ClearSelection();
            }
            else
            {
                View.SetSelection(index);
            }
            if (notify) SelectionChanged?.Invoke(SelectedTypeId);
        }

        /// <summary>Thứ tự đang hiện — test đọc để chứng minh bảng theo thứ tự làn khi chưa bật sort cột (V-12).</summary>
        internal IReadOnlyList<EventTypeRow> VisibleRows
        {
            get { return _visibleOrder; }
        }

        private void AddColumns()
        {
            View.columns.Add(BuildColumn(ColorColumnName, LiveOpsHubStrings.EventTypesColumnColor, 44, MakeSwatchCell, BindSwatchCell, false));
            View.columns.Add(BuildColumn(TypeIdColumnName, LiveOpsHubStrings.EventTypesColumnTypeId, 140, MakeMonoCell, BindTypeIdCell, true));
            Column displayName = BuildColumn(DisplayNameColumnName, LiveOpsHubStrings.EventTypesColumnDisplayName, 180, MakeTextCell,
                BindDisplayNameCell, true);
            displayName.stretchable = true;
            displayName.minWidth = 120;
            View.columns.Add(displayName);
            View.columns.Add(BuildColumn(EntryColumnName, LiveOpsHubStrings.EventTypesColumnEntry, 120, MakeTextCell, BindEntryCell, true));
            View.columns.Add(BuildColumn(ConfigKeyColumnName, LiveOpsHubStrings.EventTypesColumnConfigKey, 140, MakeMonoCell, BindConfigKeyCell, true));
            View.columns.Add(BuildColumn(SourceColumnName, LiveOpsHubStrings.EventTypesColumnSource, 90, MakeTextCell, BindSourceCell, true));
            View.columns.Add(BuildColumn(EventCountColumnName, LiveOpsHubStrings.EventTypesColumnEventCount, 84, MakeCountCell, BindCountCell, true));
            // Cột 8 không có tiêu đề: chỗ cho dấu trạng thái, tiêu đề "Trạng thái" trên 36px sẽ bị cắt thành chữ vô nghĩa.
            View.columns.Add(BuildColumn(StateColumnName, string.Empty, 36, MakeStateCell, BindStateCell, false));
        }

        private static Column BuildColumn(string columnName, string title, float width, Func<VisualElement> makeCell,
            Action<VisualElement, int> bindCell, bool sortable)
        {
            return new Column
            {
                name = columnName,
                title = title,
                width = width,
                sortable = sortable,
                makeCell = makeCell,
                bindCell = bindCell,
            };
        }

        private static VisualElement MakeSwatchCell()
        {
            VisualElement cell = new VisualElement();
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCell);
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCellCenter);
            VisualElement swatch = new VisualElement();
            swatch.AddToClassList(LiveOpsHubClassNames.Swatch);
            cell.Add(swatch);
            return cell;
        }

        private static VisualElement MakeTextCell()
        {
            Label cell = new Label();
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCell);
            return cell;
        }

        private static VisualElement MakeMonoCell()
        {
            Label cell = new Label();
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCell);
            cell.AddToClassList(LiveOpsHubClassNames.Mono);
            return cell;
        }

        private static VisualElement MakeCountCell()
        {
            Label cell = new Label();
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCell);
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCellRight);
            return cell;
        }

        private static VisualElement MakeStateCell()
        {
            VisualElement cell = new VisualElement();
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCell);
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCellCenter);
            LiveOpsStateMark mark = new LiveOpsStateMark();
            cell.Add(mark);
            return cell;
        }

        private void BindSwatchCell(VisualElement cell, int rowIndex)
        {
            EventTypeRow row = _visibleOrder[rowIndex];
            ApplyRowClasses(cell, row);
            VisualElement swatch = cell[0];
            for (int slot = 0; slot < LiveEventTypeDefinition.ColorSlotCount; slot++)
            {
                swatch.EnableInClassList(EventTypeColorClassNames.Of(slot), slot == row.ColorSlot);
            }
            swatch.EnableInClassList(LiveOpsHubClassNames.EventTypesSwatchUndeclared, !row.IsDeclared);
        }

        private void BindTypeIdCell(VisualElement cell, int rowIndex)
        {
            BindLabel(cell, rowIndex, _visibleOrder[rowIndex].TypeId);
        }

        private void BindDisplayNameCell(VisualElement cell, int rowIndex)
        {
            BindLabel(cell, rowIndex, _visibleOrder[rowIndex].DisplayName);
        }

        private void BindEntryCell(VisualElement cell, int rowIndex)
        {
            BindLabel(cell, rowIndex, _visibleOrder[rowIndex].EntryText);
        }

        private void BindConfigKeyCell(VisualElement cell, int rowIndex)
        {
            BindLabel(cell, rowIndex, _visibleOrder[rowIndex].ConfigKey);
        }

        private void BindSourceCell(VisualElement cell, int rowIndex)
        {
            BindLabel(cell, rowIndex, _visibleOrder[rowIndex].SourceText);
        }

        private void BindCountCell(VisualElement cell, int rowIndex)
        {
            EventTypeRow row = _visibleOrder[rowIndex];
            BindLabel(cell, rowIndex, row.EventCountText);
            // "luật" là chữ thay cho số nên hạ sáng (opacity 0,7 của class quiet) — không so chuỗi, đọc cờ của model.
            cell.EnableInClassList(LiveOpsHubClassNames.TextQuiet, row.IsEventCountRuleText);
        }

        private void BindStateCell(VisualElement cell, int rowIndex)
        {
            EventTypeRow row = _visibleOrder[rowIndex];
            ApplyRowClasses(cell, row);
            LiveOpsStateMark mark = (LiveOpsStateMark)cell[0];
            mark.SetHealth(HealthState.Blocked);
            mark.visible = !row.IsDeclared;
        }

        private void BindLabel(VisualElement cell, int rowIndex, string text)
        {
            EventTypeRow row = _visibleOrder[rowIndex];
            ApplyRowClasses(cell, row);
            ((Label)cell).text = text;
        }

        private static void ApplyRowClasses(VisualElement cell, EventTypeRow row)
        {
            cell.EnableInClassList(LiveOpsHubClassNames.EventTypesRowUndeclared, !row.IsDeclared);
        }

        private void OnSelectedIndicesChanged(IEnumerable<int> indices)
        {
            SelectionChanged?.Invoke(SelectedTypeId);
        }

        /// <summary>
        /// Sắp lại theo cột đang chọn; không cột nào đang sắp thì về THỨ TỰ LÀN. Bảng tự sắp <c>itemsSource</c> rồi
        /// <c>RefreshItems</c> thay vì <c>Column.comparison</c> — property đó không có ở 2022.3 ([API §5]).
        /// </summary>
        private void ApplySorting()
        {
            string previousTypeId = SelectedTypeId;
            _visibleOrder.Clear();
            for (int index = 0; index < _laneOrder.Count; index++) _visibleOrder.Add(_laneOrder[index]);

            List<SortColumnDescription> sorted = new List<SortColumnDescription>();
            foreach (SortColumnDescription description in View.sortedColumns) sorted.Add(description);
            if (sorted.Count > 0)
            {
                _visibleOrder.Sort((left, right) => CompareBySortedColumns(left, right, sorted));
            }
            View.RefreshItems();
            SelectType(previousTypeId, false);
        }

        private static int CompareBySortedColumns(EventTypeRow left, EventTypeRow right, List<SortColumnDescription> sorted)
        {
            for (int index = 0; index < sorted.Count; index++)
            {
                SortColumnDescription description = sorted[index];
                int comparison = CompareByColumn(left, right, description.columnName);
                if (comparison == 0) continue;
                return description.direction == SortDirection.Descending ? -comparison : comparison;
            }
            // Cột bằng nhau thì giữ thứ tự làn: sắp "ổn định" để hai lần bấm cùng cột không đảo hàng ngẫu nhiên.
            return left.LaneIndex.CompareTo(right.LaneIndex);
        }

        private static int CompareByColumn(EventTypeRow left, EventTypeRow right, string columnName)
        {
            switch (columnName)
            {
                case TypeIdColumnName: return string.CompareOrdinal(left.TypeId, right.TypeId);
                case DisplayNameColumnName: return string.CompareOrdinal(left.DisplayName, right.DisplayName);
                case EntryColumnName: return string.CompareOrdinal(left.EntryText, right.EntryText);
                case ConfigKeyColumnName: return string.CompareOrdinal(left.ConfigKey, right.ConfigKey);
                case SourceColumnName: return string.CompareOrdinal(left.SourceText, right.SourceText);
                case EventCountColumnName: return left.UsageCount.CompareTo(right.UsageCount);
                default: return 0;
            }
        }

        private int IndexOf(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return -1;
            for (int index = 0; index < _visibleOrder.Count; index++)
            {
                if (string.Equals(_visibleOrder[index].TypeId, typeId, StringComparison.Ordinal)) return index;
            }
            return -1;
        }
    }

    /// <summary>
    /// Ẩn/hiện một phần của màn Loại event bằng class (UI Toolkit không có <c>VisualElement.hidden</c>, còn
    /// <c>style.display</c> là style inline ngoài 10 chỗ được phép [FD §2.14]).
    /// </summary>
    internal static class EventTypesVisibility
    {
        internal static void SetHidden(VisualElement element, bool isHidden)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.EnableInClassList(LiveOpsHubClassNames.EventTypesHidden, isHidden);
        }
    }

    /// <summary>Ô màu → class nền của bảng 8.10; gom một chỗ để bảng và inspector không tự nối chuỗi tên class.</summary>
    internal static class EventTypeColorClassNames
    {
        /// <summary>"" khi số ô nằm ngoài [0, 7] — hàng chưa khai báo (-1) không gắn class màu nào.</summary>
        internal static string Of(int colorSlot)
        {
            switch (colorSlot)
            {
                case 0: return LiveOpsHubClassNames.EventColor0;
                case 1: return LiveOpsHubClassNames.EventColor1;
                case 2: return LiveOpsHubClassNames.EventColor2;
                case 3: return LiveOpsHubClassNames.EventColor3;
                case 4: return LiveOpsHubClassNames.EventColor4;
                case 5: return LiveOpsHubClassNames.EventColor5;
                case 6: return LiveOpsHubClassNames.EventColor6;
                case 7: return LiveOpsHubClassNames.EventColor7;
                default: return string.Empty;
            }
        }
    }
}
