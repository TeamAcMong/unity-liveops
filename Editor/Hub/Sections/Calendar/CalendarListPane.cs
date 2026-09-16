using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Pane Danh sách của màn Lịch [SD1 §3.12]: <see cref="MultiColumnListView"/> năm cột id / loại / bắt đầu / kết thúc /
    /// trạng thái, nằm bên trái trong <c>TwoPaneSplitView</c>.
    /// <para>
    /// Lý do pane tồn tại: trục thời gian chỉ vẽ được đợt có giờ ĐỌC ĐƯỢC. Đợt có ngày hỏng ("2026-10-3") không đặt lên trục
    /// được — nếu chỉ có trục thì nó biến mất khỏi màn hình trong khi vẫn nằm trong asset và vẫn làm game bỏ đợt. Ở đây nó có
    /// một dòng đầy đủ, cột kết thúc in nguyên văn chuỗi hỏng bằng chữ blocked, và chip "Không đặt được (n)" của header làn mở
    /// đúng pane này rồi chọn đúng dòng đó.
    /// </para>
    /// </summary>
    internal sealed class CalendarListPane
    {
        internal const string IdColumnName = "id";
        internal const string TypeColumnName = "type";
        internal const string StartColumnName = "start";
        internal const string EndColumnName = "end";
        internal const string StateColumnName = "state";

        private readonly LiveOpsHubFormat _format;
        private readonly List<CalendarListRow> _rows = new List<CalendarListRow>();
        private readonly Label _empty;

        private bool _isApplyingSelection;

        public CalendarListPane(VisualElement host, LiveOpsHubFormat format)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            _format = format ?? throw new ArgumentNullException(nameof(format));

            View = new MultiColumnListView
            {
                name = LiveOpsHubPaths.CalendarDepthElementNames.ListTable,
                itemsSource = _rows,
                selectionType = SelectionType.Single,
            };
            View.AddToClassList(LiveOpsHubClassNames.CalendarDepthListTable);
            AddColumns();
            View.selectedIndicesChanged += OnSelectedIndicesChanged;
            host.Add(View);

            _empty = new Label(LiveOpsHubStrings.CalendarDepthListEmpty) { name = LiveOpsHubPaths.CalendarDepthElementNames.ListEmpty };
            _empty.AddToClassList(LiveOpsHubClassNames.CalendarDepthListEmpty);
            _empty.AddToClassList(LiveOpsHubClassNames.CalendarHidden);
            host.Add(_empty);
        }

        /// <summary>Dòng đang chọn đổi vì NGƯỜI DÙNG bấm; "" khi bỏ chọn. Không phát khi màn tự đồng bộ lựa chọn.</summary>
        public event Action<string> SelectionChanged;

        internal MultiColumnListView View { get; }

        internal IReadOnlyList<CalendarListRow> Rows => _rows;

        /// <summary>Nạp lại danh sách từ tài liệu; giữ nguyên dòng đang chọn nếu đợt đó còn.</summary>
        public void SetDocument(LiveEventCalendarDocument document, DateTime nowUtc, string selectedEntryKey)
        {
            _rows.Clear();
            IReadOnlyList<FixedLiveEventEntry> entries = (document ?? LiveEventCalendarDocument.Empty).FixedEvents;
            for (int index = 0; index < entries.Count; index++) _rows.Add(CalendarListRow.For(entries[index], nowUtc, _format));
            View.itemsSource = _rows;
            View.Rebuild();
            _empty.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, _rows.Count > 0);
            View.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, _rows.Count == 0);
            Select(selectedEntryKey);
        }

        /// <summary>Đồng bộ lựa chọn từ ngoài (chọn trên trục, chip "Không đặt được") mà không phát ngược sự kiện.</summary>
        public void Select(string entryKey)
        {
            int index = IndexOf(entryKey);
            _isApplyingSelection = true;
            try
            {
                if (index < 0) View.ClearSelection();
                else View.SetSelection(index);
            }
            finally
            {
                _isApplyingSelection = false;
            }
        }

        private int IndexOf(string entryKey)
        {
            if (string.IsNullOrEmpty(entryKey)) return -1;
            for (int index = 0; index < _rows.Count; index++)
            {
                if (string.Equals(_rows[index].EntryKey, entryKey, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        private void OnSelectedIndicesChanged(IEnumerable<int> indices)
        {
            if (_isApplyingSelection) return;
            int index = View.selectedIndex;
            SelectionChanged?.Invoke(index >= 0 && index < _rows.Count ? _rows[index].EntryKey : string.Empty);
        }

        private void AddColumns()
        {
            View.columns.Add(BuildColumn(IdColumnName, LiveOpsHubStrings.CalendarDepthListColumnId, 150, true));
            View.columns.Add(BuildColumn(TypeColumnName, LiveOpsHubStrings.CalendarDepthListColumnType, 120, true));
            View.columns.Add(BuildColumn(StartColumnName, LiveOpsHubStrings.CalendarDepthListColumnStart, 100, true));
            View.columns.Add(BuildColumn(EndColumnName, LiveOpsHubStrings.CalendarDepthListColumnEnd, 100, true));
            View.columns.Add(BuildColumn(StateColumnName, LiveOpsHubStrings.CalendarDepthListColumnState, 96, false));
        }

        private Column BuildColumn(string columnName, string title, float width, bool isMono)
        {
            string capturedName = columnName;
            return new Column
            {
                name = columnName,
                title = title,
                width = width,
                minWidth = 48,
                makeCell = () => MakeCell(isMono),
                bindCell = (element, index) => BindCell((Label)element, capturedName, index),
            };
        }

        private static VisualElement MakeCell(bool isMono)
        {
            Label cell = new Label();
            cell.AddToClassList(LiveOpsHubClassNames.CalendarDepthListCell);
            if (isMono) cell.AddToClassList(LiveOpsHubClassNames.Mono);
            return cell;
        }

        /// <summary>
        /// Ô chuỗi giờ HỎNG in nguyên văn bằng chữ blocked [SD1 §3.12]. Không "sửa hộ" và không để trống: người dùng phải thấy
        /// đúng chuỗi đang nằm trong asset thì mới sửa được nó.
        /// </summary>
        private void BindCell(Label cell, string columnName, int index)
        {
            if (index < 0 || index >= _rows.Count) return;
            CalendarListRow row = _rows[index];
            bool isBlocked = false;
            switch (columnName)
            {
                case IdColumnName:
                    cell.text = row.EventId;
                    break;
                case TypeColumnName:
                    cell.text = row.EventType;
                    break;
                case StartColumnName:
                    cell.text = row.StartText;
                    isBlocked = !row.HasReadableStart;
                    break;
                case EndColumnName:
                    cell.text = row.EndText;
                    isBlocked = !row.HasReadableEnd;
                    break;
                default:
                    cell.text = row.StateText;
                    isBlocked = row.IsUnplaceable;
                    break;
            }
            cell.EnableInClassList(LiveOpsHubClassNames.TextBlocked, isBlocked);
        }
    }

    /// <summary>Một dòng của pane Danh sách — chữ tính sẵn để bảng chỉ còn việc vẽ (và test Logic đọc thẳng dòng).</summary>
    internal sealed class CalendarListRow
    {
        private CalendarListRow(string entryKey, string eventId, string eventType, string startText, string endText,
            string stateText, bool hasReadableStart, bool hasReadableEnd)
        {
            EntryKey = entryKey;
            EventId = eventId;
            EventType = eventType;
            StartText = startText;
            EndText = endText;
            StateText = stateText;
            HasReadableStart = hasReadableStart;
            HasReadableEnd = hasReadableEnd;
        }

        public string EntryKey { get; }
        public string EventId { get; }
        public string EventType { get; }
        public string StartText { get; }
        public string EndText { get; }
        public string StateText { get; }
        public bool HasReadableStart { get; }
        public bool HasReadableEnd { get; }

        public bool IsUnplaceable => !HasReadableStart || !HasReadableEnd;

        public static CalendarListRow For(FixedLiveEventEntry entry, DateTime nowUtc, LiveOpsHubFormat format)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (format == null) throw new ArgumentNullException(nameof(format));
            bool hasStart = entry.TryGetStartUtc(out DateTime startUtc);
            bool hasEnd = entry.TryGetEndUtc(out DateTime endUtc);
            string stateText = !hasStart || !hasEnd
                ? LiveOpsHubStrings.CalendarPhaseUnplaceable
                : nowUtc < startUtc
                    ? LiveOpsHubStrings.CalendarPhaseUpcoming
                    : nowUtc < endUtc ? LiveOpsHubStrings.CalendarPhaseRunning : LiveOpsHubStrings.CalendarPhaseEnded;
            return new CalendarListRow(entry.EntryKey, entry.EventId, entry.EventType,
                hasStart ? format.ShortDateTime(startUtc) : entry.StartUtcText,
                hasEnd ? format.ShortDateTime(endUtc) : entry.EndUtcText,
                stateText, hasStart, hasEnd);
        }
    }
}
