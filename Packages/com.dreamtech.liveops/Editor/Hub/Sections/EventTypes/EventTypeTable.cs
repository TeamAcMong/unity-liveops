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

        /// <summary>
        /// (W9-05) Bề rộng TỐI THIỂU đọc được của từng cột — đo trên bản chữ dài nhất của dữ liệu mẫu cộng đệm ô 8px:
        /// id loại "star_tournament_v1" cần 119px, "Joins automatically" cần 100px, khoá config "star-tournament" cần 99px,
        /// "Recurring rule" cần 73px. Không khai số này thì <see cref="MultiColumnListView"/> chia đều bề rộng còn lại và
        /// bóp MỌI cột xuống 27px ở cửa sổ 700 — tám cột đều thành chữ cụt, đúng 76 chỗ của phiếu W9-05.
        /// </summary>
        private const float ColorColumnMinimumWidth = 44f;

        private const float TypeIdColumnMinimumWidth = 128f;
        private const float DisplayNameColumnMinimumWidth = 120f;
        private const float EntryColumnMinimumWidth = 112f;
        private const float ConfigKeyColumnMinimumWidth = 112f;
        private const float SourceColumnMinimumWidth = 84f;
        private const float EventCountColumnMinimumWidth = 56f;
        private const float StateColumnMinimumWidth = 36f;

        /// <summary>
        /// Chỗ dành cho thanh cuộn dọc của bảng. Trừ sẵn thì tổng bề rộng tối thiểu của các cột đang hiện không bao giờ
        /// vượt viewport, tức bảng không bao giờ mọc thanh cuộn NGANG — cuộn ngang trong một bảng là cách giấu cột đi mà
        /// không nói, cổng bố cục tính nó là lỗi.
        /// </summary>
        private const float VerticalScrollerReserve = 14f;

        /// <summary>
        /// Bốn cột luôn hiện: màu, id loại, tên hiển thị, dấu trạng thái. Đây là phần trả lời "hàng này là loại nào và nó
        /// đang thế nào" — bỏ cột nào trong bốn cột này thì bảng thôi là bảng loại.
        /// </summary>
        private const float AlwaysVisibleColumnsWidth = ColorColumnMinimumWidth + TypeIdColumnMinimumWidth
            + DisplayNameColumnMinimumWidth + StateColumnMinimumWidth;

        /// <summary>
        /// Bốn cột phụ theo thứ tự GIỮ LẠI: cách vào → khoá config → nguồn → số đợt. Cửa sổ hẹp dần thì bỏ từ cuối danh
        /// sách. Bỏ theo TIỀN TỐ (gặp cột đầu tiên không vừa là dừng) chứ không nhặt cột nào vừa thì lấy: nhảy cóc một cột
        /// làm thứ tự cột đổi theo bề rộng cửa sổ và người dùng mất mốc đọc.
        /// </summary>
        private static readonly float[] OptionalColumnMinimumWidths =
        {
            EntryColumnMinimumWidth, ConfigKeyColumnMinimumWidth, SourceColumnMinimumWidth, EventCountColumnMinimumWidth,
        };

        /// <summary>Ba cột đầu (màu, id loại, tên hiển thị) — luôn nằm trước bốn cột phụ.</summary>
        private readonly List<Column> _leadingColumns = new List<Column>();

        /// <summary>Bốn cột phụ theo đúng thứ tự giữ lại của <see cref="OptionalColumnMinimumWidths"/>.</summary>
        private readonly List<Column> _optionalColumns = new List<Column>();

        /// <summary>Cột dấu trạng thái — luôn là cột CUỐI, kể cả khi mọi cột phụ đã bị bỏ.</summary>
        private Column _stateColumn;

        /// <summary>Số cột phụ đang hiện; -1 = chưa dựng lần nào. Chỉ dựng lại bảng cột khi con số này ĐỔI.</summary>
        private int _visibleOptionalColumnCount = -1;

        private readonly List<EventTypeRow> _laneOrder = new List<EventTypeRow>();
        private readonly List<EventTypeRow> _visibleOrder = new List<EventTypeRow>();

        internal EventTypeTable()
        {
            View = new MultiColumnListView { name = LiveOpsHubPaths.EventTypesElementNames.Table, itemsSource = _visibleOrder };
            View.AddToClassList(LiveOpsHubClassNames.EventTypesTable);
            View.selectionType = SelectionType.Single;
            AddColumns();
            // Bề rộng bảng đổi (kéo mép cửa sổ, inspector co lại) thì tính lại xem giữ được bao nhiêu cột phụ.
            View.RegisterCallback<GeometryChangedEvent>(OnViewGeometryChanged);
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
            _leadingColumns.Add(BuildColumn(ColorColumnName, LiveOpsHubStrings.EventTypesColumnColor, 44, ColorColumnMinimumWidth,
                MakeSwatchCell, BindSwatchCell, false));
            _leadingColumns.Add(BuildColumn(TypeIdColumnName, LiveOpsHubStrings.EventTypesColumnTypeId, 140, TypeIdColumnMinimumWidth,
                MakeMonoCell, BindTypeIdCell, true));
            Column displayName = BuildColumn(DisplayNameColumnName, LiveOpsHubStrings.EventTypesColumnDisplayName, 180,
                DisplayNameColumnMinimumWidth, MakeTextCell, BindDisplayNameCell, true);
            displayName.stretchable = true;
            _leadingColumns.Add(displayName);
            _optionalColumns.Add(BuildColumn(EntryColumnName, LiveOpsHubStrings.EventTypesColumnEntry, 120, EntryColumnMinimumWidth,
                MakeTextCell, BindEntryCell, true));
            _optionalColumns.Add(BuildColumn(ConfigKeyColumnName, LiveOpsHubStrings.EventTypesColumnConfigKey, 140, ConfigKeyColumnMinimumWidth,
                MakeMonoCell, BindConfigKeyCell, true));
            _optionalColumns.Add(BuildColumn(SourceColumnName, LiveOpsHubStrings.EventTypesColumnSource, 90, SourceColumnMinimumWidth,
                MakeTextCell, BindSourceCell, true));
            _optionalColumns.Add(BuildColumn(EventCountColumnName, LiveOpsHubStrings.EventTypesColumnEventCount, 84,
                EventCountColumnMinimumWidth, MakeCountCell, BindCountCell, true));
            // Cột 8 không có tiêu đề: chỗ cho dấu trạng thái, tiêu đề "Trạng thái" trên 36px sẽ bị cắt thành chữ vô nghĩa.
            _stateColumn = BuildColumn(StateColumnName, string.Empty, 36, StateColumnMinimumWidth, MakeStateCell, BindStateCell, false);
            ApplyVisibleColumnCount(_optionalColumns.Count);
        }

        private static Column BuildColumn(string columnName, string title, float width, float minimumWidth,
            Func<VisualElement> makeCell, Action<VisualElement, int> bindCell, bool sortable)
        {
            return new Column
            {
                name = columnName,
                title = title,
                width = width,
                minWidth = minimumWidth,
                sortable = sortable,
                makeCell = makeCell,
                bindCell = bindCell,
            };
        }

        private void OnViewGeometryChanged(GeometryChangedEvent geometryEvent)
        {
            int wanted = OptionalColumnCountThatFits(geometryEvent.newRect.width);
            if (wanted < 0 || wanted == _visibleOptionalColumnCount) return;
            // Dựng lại bảng cột là thay đổi CẤU TRÚC cây; làm ngay bên trong một lượt layout là sửa cái đang được đo.
            // Hoãn sang lượt sau — cổng bố cục chờ layout ổn định 9 khung nên vẫn đo đúng bộ cột mới.
            View.schedule.Execute(() => ApplyVisibleColumnCount(wanted));
        }

        /// <summary>
        /// Giữ lại nhiều cột phụ nhất mà bề rộng bảng còn CHỨA ĐƯỢC ở bề rộng tối thiểu đọc được của chúng; trả về -1 khi
        /// chưa có số đo. Đây là "thu gọn có chủ đích": cột bị bỏ không biến mất khỏi hub — mọi giá trị của loại đang chọn
        /// vẫn đọc đủ ở inspector bên phải, vốn là nơi thiết kế đặt chi tiết ([SD1] §2.1). Ngược lại, để tám cột cùng ở lại
        /// trong 423px thì CẢ TÁM đều còn 27px và không cột nào đọc nổi — mất nhiều hơn hẳn.
        /// </summary>
        private int OptionalColumnCountThatFits(float viewWidth)
        {
            if (float.IsNaN(viewWidth) || viewWidth <= 0f) return -1;
            float usableWidth = viewWidth - VerticalScrollerReserve;
            float used = AlwaysVisibleColumnsWidth;
            int count = 0;
            while (count < OptionalColumnMinimumWidths.Length && used + OptionalColumnMinimumWidths[count] <= usableWidth)
            {
                used += OptionalColumnMinimumWidths[count];
                count++;
            }

            return count;
        }

        /// <summary>
        /// Dựng lại danh sách cột với <paramref name="optionalCount"/> cột phụ đầu tiên. Dựng LẠI chứ không dùng
        /// <c>Column.visible</c>: ở Unity 2022.3, một cột ẩn đứng TRƯỚC một cột hiện làm
        /// <c>MultiColumnController.OnColumnResized</c> tra ô theo chỉ số của danh sách ĐẦY ĐỦ trong khi hàng chỉ dựng ô cho
        /// cột đang hiện — ném <c>ArgumentOutOfRangeException</c> ngay lượt layout đầu (đã gặp thật ở lượt EditMode 2022.3
        /// của gói này). Cột dấu trạng thái luôn là cột cuối nên trường hợp "ẩn đứng trước hiện" là không tránh được.
        /// </summary>
        private void ApplyVisibleColumnCount(int optionalCount)
        {
            if (optionalCount == _visibleOptionalColumnCount) return;
            _visibleOptionalColumnCount = optionalCount;
            View.columns.Clear();
            foreach (Column column in _leadingColumns) View.columns.Add(column);
            for (int index = 0; index < optionalCount; index++) View.columns.Add(_optionalColumns[index]);
            View.columns.Add(_stateColumn);
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
