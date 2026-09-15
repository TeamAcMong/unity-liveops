using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Palette ⌘K chỉ để đi tới màn (8.7, [FD §3.8]): scrim phủ cả cửa sổ (kể cả rail) + panel 440px cách đỉnh 60px, ô nhập có gợi ý,
    /// hàng 22px (dấu · tiêu đề đậm phần khớp · badge — lý do · tầng), chân phím. Không bao giờ có lệnh: gõ nhầm ba chữ không thể xoá dữ
    /// liệu, gõ id luật chỉ dẫn tới Kiểm lịch đã lọc.
    /// <para>
    /// (V-21 D-4) Palette không biết cửa sổ: nhận danh sách mục qua <c>entries</c> (đọc lại mỗi lần mở — health đổi giữa hai lần mở) và
    /// trả mục chọn qua <c>open</c>. Host (G-HOSTUI, W4) dựng <see cref="LiveOpsPaletteMatcher.Entry"/> từ registry + health và điều hướng.
    /// </para>
    /// </summary>
    internal sealed class LiveOpsHubPalette
    {
        internal const string ScrimElementName = "hub-palette-scrim";
        internal const string PanelElementName = "hub-palette";
        internal const string FieldElementName = "hub-palette-field";
        internal const string ListElementName = "hub-palette-list";
        internal const string EmptyElementName = "hub-palette-empty";
        internal const string FooterElementName = "hub-palette-footer";

        /// <summary>Id shortcut mở palette (PD-15) — G-HOSTUI đăng ký; chưa có thì nhãn phím rỗng và chân dùng câu không phím.</summary>
        internal const string OpenPaletteShortcutId = "LiveOps Hub/Open Palette";

        /// <summary>Menu Unity Search (SP-7 kiểm đường menu ở hai bản).</summary>
        internal const string UnitySearchMenuPath = "Edit/Search/Search All...";

        private readonly VisualElement _root;
        private readonly Func<IReadOnlyList<LiveOpsPaletteMatcher.Entry>> _entries;
        private readonly Action<LiveOpsPaletteMatcher.Entry> _open;
        private readonly Action _openUnitySearch;
        private readonly VisualElement _scrim;
        private readonly VisualElement _panel;
        private readonly TextField _field;
        private readonly LiveOpsPlaceholder _placeholder;
        private readonly ScrollView _list;
        private readonly Label _empty;
        private readonly Label _footer;
        private readonly List<VisualElement> _rows = new List<VisualElement>();

        private IReadOnlyList<LiveOpsPaletteMatcher.Entry> _currentEntries = Array.Empty<LiveOpsPaletteMatcher.Entry>();
        private IReadOnlyList<LiveOpsPaletteMatcher.Match> _matches = Array.Empty<LiveOpsPaletteMatcher.Match>();
        private int _selectedIndex = -1;

        public LiveOpsHubPalette(VisualElement root, Func<IReadOnlyList<LiveOpsPaletteMatcher.Entry>> entries, Action<LiveOpsPaletteMatcher.Entry> open)
            : this(root, entries, open, OpenUnitySearch)
        {
        }

        /// <param name="openUnitySearch">Test thay để không mở cửa sổ Unity Search thật.</param>
        internal LiveOpsHubPalette(VisualElement root, Func<IReadOnlyList<LiveOpsPaletteMatcher.Entry>> entries, Action<LiveOpsPaletteMatcher.Entry> open,
            Action openUnitySearch)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
            _open = open ?? throw new ArgumentNullException(nameof(open));
            _openUnitySearch = openUnitySearch ?? throw new ArgumentNullException(nameof(openUnitySearch));

            _scrim = new VisualElement { name = ScrimElementName };
            _scrim.AddToClassList(LiveOpsHubClassNames.Scrim);
            _scrim.AddToClassList(LiveOpsHubClassNames.ScrimHidden);
            // Bấm ra ngoài panel = đóng: palette chỉ điều hướng nên đóng nhầm không mất gì.
            _scrim.RegisterCallback<PointerDownEvent>(pointerEvent => Close());

            _panel = new VisualElement { name = PanelElementName };
            _panel.AddToClassList(LiveOpsHubClassNames.Palette);
            _panel.AddToClassList(LiveOpsHubClassNames.PaletteHidden);
            _panel.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            // Mũi tên/Enter/Esc đã xử lý ở KeyDown: chặn Navigation event đi kèm để rail phía sau không chạy lần hai (8.7).
            _panel.RegisterCallback<NavigationMoveEvent>(navigationEvent => navigationEvent.StopPropagation(), TrickleDown.TrickleDown);
            _panel.RegisterCallback<NavigationSubmitEvent>(navigationEvent => navigationEvent.StopPropagation(), TrickleDown.TrickleDown);
            _panel.RegisterCallback<NavigationCancelEvent>(navigationEvent => navigationEvent.StopPropagation(), TrickleDown.TrickleDown);

            _field = new TextField { name = FieldElementName };
            _field.AddToClassList(LiveOpsHubClassNames.PaletteField);
            _placeholder = LiveOpsPlaceholder.Attach(_field, LiveOpsHubStrings.FeedbackPalettePlaceholder);
            _field.RegisterValueChangedCallback(changeEvent => RefreshMatches(changeEvent.newValue));
            _panel.Add(_field);

            _list = new ScrollView { name = ListElementName };
            _list.AddToClassList(LiveOpsHubClassNames.PaletteList);
            _panel.Add(_list);

            _empty = new Label { name = EmptyElementName, enableRichText = false };
            _empty.AddToClassList(LiveOpsHubClassNames.PaletteEmpty);
            _empty.AddToClassList(LiveOpsHubClassNames.PaletteEmptyHidden);
            _panel.Add(_empty);

            _footer = new Label { name = FooterElementName, enableRichText = false };
            _footer.AddToClassList(LiveOpsHubClassNames.PaletteFooter);
            _panel.Add(_footer);

            _root.Add(_scrim);
            _root.Add(_panel);
        }

        public bool IsOpen { get; private set; }

        internal IReadOnlyList<LiveOpsPaletteMatcher.Match> Matches => _matches;
        internal int SelectedIndex => _selectedIndex;
        internal TextField Field => _field;
        internal VisualElement Panel => _panel;
        internal VisualElement Scrim => _scrim;
        internal Label EmptyLabel => _empty;
        internal Label FooterLabel => _footer;
        internal IReadOnlyList<VisualElement> Rows => _rows;

        /// <summary>Mở trống, chọn sẵn hàng đầu, focus ô nhập ở frame sau (ô chưa có layout thì Focus không giữ).</summary>
        public void Open()
        {
            _currentEntries = _entries() ?? Array.Empty<LiveOpsPaletteMatcher.Entry>();
            IsOpen = true;
            _field.SetValueWithoutNotify(string.Empty);
            _placeholder.Refresh();
            string keyLabel = LiveOpsHubKeyLabels.For(OpenPaletteShortcutId);
            _footer.text = keyLabel.Length == 0
                ? LiveOpsHubStrings.FeedbackPaletteFooterWithoutKey
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.FeedbackPaletteFooterFormat, keyLabel);
            RefreshMatches(string.Empty);
            _scrim.RemoveFromClassList(LiveOpsHubClassNames.ScrimHidden);
            _panel.RemoveFromClassList(LiveOpsHubClassNames.PaletteHidden);
            _panel.AddToClassList(LiveOpsHubClassNames.PaletteVisible);
            // Chỗ 10 của [FD §2.14]: scrim rồi panel lên trên cùng (USS không có z-index; hover card/toast mở sau vẫn nằm dưới).
            _scrim.BringToFront();
            _panel.BringToFront();
            if (_root.panel != null) _root.schedule.Execute(FocusFieldIfOpen);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _scrim.AddToClassList(LiveOpsHubClassNames.ScrimHidden);
            _panel.RemoveFromClassList(LiveOpsHubClassNames.PaletteVisible);
            _panel.AddToClassList(LiveOpsHubClassNames.PaletteHidden);
            _field.Blur();
        }

        /// <summary>⌘K: đóng thì mở; đang mở thì đóng và mở Unity Search (⌘K vốn là phím của Unity Search).</summary>
        public void ToggleOrSearch()
        {
            if (!IsOpen)
            {
                Open();
                return;
            }
            Close();
            _openUnitySearch();
        }

        /// <summary>Gõ câu tìm không cần panel (test Logic): như người dùng gõ vào ô.</summary>
        internal void ApplyQuery(string query)
        {
            _field.SetValueWithoutNotify(query ?? string.Empty);
            _placeholder.Refresh();
            RefreshMatches(query);
        }

        internal void MoveSelection(int direction)
        {
            if (_matches.Count == 0) return;
            int next = Mathf.Clamp(_selectedIndex + direction, 0, _matches.Count - 1);
            Select(next);
        }

        /// <summary>Enter: đóng rồi mở mục chọn (đóng trước để màn mới nhận focus, không phải ô palette đang ẩn).</summary>
        internal void SubmitSelected()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _matches.Count) return;
            LiveOpsPaletteMatcher.Entry entry = _matches[_selectedIndex].Entry;
            Close();
            _open(entry);
        }

        internal void HandleKey(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.UpArrow:
                    MoveSelection(-1);
                    break;
                case KeyCode.DownArrow:
                    MoveSelection(1);
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    SubmitSelected();
                    break;
                case KeyCode.Escape:
                    Close();
                    break;
            }
        }

        private static void OpenUnitySearch()
        {
            // SP-7: đường menu chạy ở hai bản; menu bị đổi tên ở bản sau thì mở thẳng cửa sổ Search với tham số mặc định.
            if (!EditorApplication.ExecuteMenuItem(UnitySearchMenuPath)) UnityEditor.Search.SearchService.ShowWindow();
        }

        private void OnKeyDown(KeyDownEvent keyDown)
        {
            if (!IsOpen) return;
            KeyCode keyCode = keyDown.keyCode;
            if (keyCode != KeyCode.UpArrow && keyCode != KeyCode.DownArrow && keyCode != KeyCode.Return && keyCode != KeyCode.KeypadEnter
                && keyCode != KeyCode.Escape)
            {
                return;
            }
            keyDown.StopPropagation();
            HandleKey(keyCode);
        }

        private void FocusFieldIfOpen()
        {
            if (IsOpen) _field.Focus();
        }

        private void RefreshMatches(string query)
        {
            _matches = LiveOpsPaletteMatcher.Find(_currentEntries, query);
            _list.Clear();
            _rows.Clear();
            for (int index = 0; index < _matches.Count; index++)
            {
                VisualElement row = CreateRow(_matches[index], index);
                _rows.Add(row);
                _list.Add(row);
            }
            bool isEmpty = _matches.Count == 0;
            string trimmed = (query ?? string.Empty).Trim();
            _empty.text = isEmpty && trimmed.Length > 0
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.FeedbackPaletteNoMatchFormat, trimmed)
                : string.Empty;
            _empty.EnableInClassList(LiveOpsHubClassNames.PaletteEmptyHidden, !(isEmpty && trimmed.Length > 0));
            _selectedIndex = -1;
            Select(isEmpty ? -1 : 0);
        }

        private VisualElement CreateRow(LiveOpsPaletteMatcher.Match match, int index)
        {
            LiveOpsPaletteMatcher.Entry entry = match.Entry;
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.PaletteRow);

            LiveOpsStateMark mark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
            mark.SetHealth(entry.Health.State);
            mark.AddToClassList(LiveOpsHubClassNames.PaletteRowMark);
            row.Add(mark);

            Label title = new Label(RowText(match)) { enableRichText = true };
            title.AddToClassList(LiveOpsHubClassNames.PaletteRowTitle);
            row.Add(title);

            Label stage = new Label(entry.IsRule ? LiveOpsHubStrings.FeedbackPaletteRuleIdCaption : entry.StageCaption) { enableRichText = false };
            stage.AddToClassList(LiveOpsHubClassNames.PaletteRowStage);
            row.Add(stage);

            row.RegisterCallback<PointerDownEvent>(pointerEvent =>
            {
                pointerEvent.StopPropagation();
                Select(index);
                SubmitSelected();
            });
            return row;
        }

        /// <summary>
        /// "<b>Kiểm</b> lịch · 2 bị bỏ" · "Kiểm lịch / overlap-same-type" · "Trực tiếp · chưa kiểm — Không ở Play Mode": hàng không Ok in
        /// badge và lý do sau tên vì palette không có tooltip. Mọi chữ thô (id, badge, lý do) đi qua noparse.
        /// </summary>
        internal static string RowText(LiveOpsPaletteMatcher.Match match)
        {
            LiveOpsPaletteMatcher.Entry entry = match.Entry;
            string text = match.RichTitle;
            if (entry.IsRule) text += LiveOpsPaletteMatcher.Escape(LiveOpsHubStrings.FeedbackPaletteRuleSeparator + entry.RuleId);
            if (entry.Health.State == HealthState.Ok) return text;
            string badge = entry.Health.Badge;
            if (badge.Length == 0 && entry.Health.State == HealthState.NotMeasured) badge = LiveOpsHubStrings.ShellRailNotMeasuredBadge;
            if (badge.Length > 0) text += LiveOpsPaletteMatcher.Escape(LiveOpsHubStrings.FeedbackPaletteBadgeSeparator + badge);
            if (entry.ReasonText.Length > 0) text += LiveOpsPaletteMatcher.Escape(LiveOpsHubStrings.FeedbackPaletteReasonSeparator + entry.ReasonText);
            return text;
        }

        private void Select(int index)
        {
            if (_selectedIndex >= 0 && _selectedIndex < _rows.Count) _rows[_selectedIndex].RemoveFromClassList(LiveOpsHubClassNames.PaletteRowSelected);
            _selectedIndex = index;
            if (index < 0 || index >= _rows.Count) return;
            VisualElement row = _rows[index];
            row.AddToClassList(LiveOpsHubClassNames.PaletteRowSelected);
            if (row.panel != null) _list.ScrollTo(row);
        }
    }
}
