using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Một hàng của pane trái: swatch màu loại · id loại · "mỗi 7 ngày · chạy 7 ngày" · dấu mức xấu nhất của loại đó.</summary>
    internal sealed class RecurringRuleListRow
    {
        internal RecurringRuleListRow(string eventType, int colorSlot, string metaText, HealthState? severity)
        {
            EventType = eventType ?? string.Empty;
            ColorSlot = colorSlot;
            MetaText = metaText ?? string.Empty;
            Severity = severity;
        }

        public string EventType { get; }
        public int ColorSlot { get; }
        public string MetaText { get; }

        /// <summary>null = không có phát hiện nào về luật này (không vẽ dấu, không vẽ vòng rỗng gây hiểu nhầm "chưa kiểm").</summary>
        public HealthState? Severity { get; }
    }

    /// <summary>
    /// Pane trái 240px của màn Luật lặp ([SD1 §4.1]): danh sách luật, một hàng một loại. Bấm hoặc ↑↓ để chọn; điều hướng tới từ
    /// Lịch ("Mở luật weekly-pass") gọi <see cref="Flash"/> để hàng chớp nền một nhịp cho mắt bắt được chỗ vừa nhảy tới (7.4).
    /// </summary>
    internal sealed class RecurringRuleList : VisualElement
    {
        /// <summary>Đủ để transition 300ms của <c>liveops-hub-row--flash</c> chạy hết rồi mới gỡ class (motion USS).</summary>
        private const long FlashMilliseconds = 300;

        private readonly ScrollView _scroll;
        private readonly List<Button> _rowButtons = new List<Button>();
        private readonly List<string> _eventTypes = new List<string>();

        internal RecurringRuleList()
        {
            AddToClassList(LiveOpsHubClassNames.RecurringList);
            focusable = true;
            _scroll = new ScrollView(ScrollViewMode.Vertical);
            Add(_scroll);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<FocusInEvent>(OnFocusIn);
            RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        /// <summary>Người dùng chọn một luật khác (bấm hoặc ↑↓). Không bắn khi <see cref="SetRows"/> dựng lại danh sách.</summary>
        public event Action<string> SelectionChanged;

        public string SelectedEventType { get; private set; } = string.Empty;

        public int RowCount => _rowButtons.Count;

        public void SetRows(IReadOnlyList<RecurringRuleListRow> rows, string selectedEventType)
        {
            _scroll.contentContainer.Clear();
            _rowButtons.Clear();
            _eventTypes.Clear();
            SelectedEventType = selectedEventType ?? string.Empty;
            if (rows == null) return;

            for (int index = 0; index < rows.Count; index++)
            {
                RecurringRuleListRow row = rows[index];
                string eventType = row.EventType;
                Button button = new Button(() => Select(eventType, true)) { name = RowElementName(eventType) };
                button.AddToClassList(LiveOpsHubClassNames.RecurringListRow);
                button.EnableInClassList(LiveOpsHubClassNames.RecurringListRowSelected,
                    string.Equals(eventType, SelectedEventType, StringComparison.Ordinal));

                VisualElement swatch = new VisualElement();
                swatch.AddToClassList(LiveOpsHubClassNames.Swatch);
                LiveOpsHubStyle.SetEventColor(swatch, row.ColorSlot);
                button.Add(swatch);

                Label name = new Label(eventType);
                name.AddToClassList(LiveOpsHubClassNames.RecurringListName);
                button.Add(name);

                Label meta = new Label(row.MetaText);
                meta.AddToClassList(LiveOpsHubClassNames.RecurringListMeta);
                button.Add(meta);

                if (row.Severity.HasValue)
                {
                    LiveOpsStateMark mark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
                    mark.SetHealth(row.Severity.Value);
                    button.Add(mark);
                }

                _scroll.contentContainer.Add(button);
                _rowButtons.Add(button);
                _eventTypes.Add(eventType);
            }
        }

        /// <summary>Đặt lựa chọn từ code (khôi phục trạng thái view, điều hướng) — KHÔNG bắn <see cref="SelectionChanged"/>.</summary>
        public void SetSelectionWithoutNotify(string eventType)
        {
            Select(eventType, false);
        }

        /// <summary>Chớp nền hàng một nhịp 300ms rồi gỡ class — transition chỉ chạy một lần, không lặp (7.4).</summary>
        public void Flash(string eventType)
        {
            Button row = FindRow(eventType);
            if (row == null) return;
            row.RemoveFromClassList(LiveOpsHubClassNames.RowFlash);
            row.AddToClassList(LiveOpsHubClassNames.RowFlash);
            row.schedule.Execute(() => row.RemoveFromClassList(LiveOpsHubClassNames.RowFlash)).StartingIn(FlashMilliseconds);
        }

        internal Button FindRow(string eventType)
        {
            for (int index = 0; index < _eventTypes.Count; index++)
            {
                if (string.Equals(_eventTypes[index], eventType, StringComparison.Ordinal)) return _rowButtons[index];
            }
            return null;
        }

        internal static string RowElementName(string eventType)
        {
            return RowElementPrefix + eventType;
        }

        internal const string RowElementPrefix = "recurring-row-";

        private void Select(string eventType, bool notify)
        {
            string value = eventType ?? string.Empty;
            if (string.Equals(value, SelectedEventType, StringComparison.Ordinal) && !notify) return;
            SelectedEventType = value;
            for (int index = 0; index < _rowButtons.Count; index++)
            {
                _rowButtons[index].EnableInClassList(LiveOpsHubClassNames.RecurringListRowSelected,
                    string.Equals(_eventTypes[index], value, StringComparison.Ordinal));
            }
            if (notify && SelectionChanged != null) SelectionChanged(value);
        }

        private void OnKeyDown(KeyDownEvent keyDownEvent)
        {
            int step = keyDownEvent.keyCode == KeyCode.DownArrow ? 1 : (keyDownEvent.keyCode == KeyCode.UpArrow ? -1 : 0);
            if (step == 0 || _eventTypes.Count == 0) return;
            int current = IndexOfEventType(SelectedEventType);
            int next = current < 0 ? 0 : current + step;
            if (next < 0 || next >= _eventTypes.Count) return;
            Select(_eventTypes[next], true);
            keyDownEvent.StopPropagation();
        }

        private int IndexOfEventType(string eventType)
        {
            for (int index = 0; index < _eventTypes.Count; index++)
            {
                if (string.Equals(_eventTypes[index], eventType, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        private void OnFocusIn(FocusInEvent focusInEvent)
        {
            AddToClassList(LiveOpsHubClassNames.ListHasFocus);
        }

        private void OnFocusOut(FocusOutEvent focusOutEvent)
        {
            RemoveFromClassList(LiveOpsHubClassNames.ListHasFocus);
        }
    }
}
