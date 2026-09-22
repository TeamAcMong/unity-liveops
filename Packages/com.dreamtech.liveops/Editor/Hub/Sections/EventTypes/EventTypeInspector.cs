using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Inspector loại ([SD1 §2.1], rộng 300, sibling ngoài split): id loại (khoá khi loại đã có đợt), tên hiển thị, dãy tám ô màu
    /// 18×18, dòng phụ trùng màu / ghi đè theo băm, "Phải bấm tham gia", config key mặc định và card "Dùng ở đâu".
    /// <para>
    /// Inspector chỉ PHÁT yêu cầu sửa (sự kiện) — màn mới gọi phiên, để một chỗ duy nhất dựng Undo group và toast. Hai ô chữ đặt
    /// <c>isDelayed</c>: mỗi lần commit (Enter hoặc rời ô) là MỘT Undo group, gõ từng ký tự không thành một trăm bước hoàn tác.
    /// </para>
    /// </summary>
    internal sealed class EventTypeInspector
    {
        internal const string TitleElementName = "event-types-inspector-title";
        internal const string MenuButtonName = "event-types-inspector-menu";
        internal const string TypeIdFieldName = "event-types-type-id";
        internal const string DisplayNameFieldName = "event-types-display-name";
        internal const string ColorSlotsElementName = "event-types-color-slots";
        internal const string ColorNoteElementName = "event-types-color-note";
        internal const string ResetColorButtonName = "event-types-reset-color";
        internal const string RequiresJoinToggleName = "event-types-requires-join";
        internal const string ConfigKeyFieldName = "event-types-config-key";
        internal const string UsageCardElementName = "event-types-usage";
        internal const string UsageTextElementName = "event-types-usage-text";
        internal const string ShowInCalendarButtonName = "event-types-show-in-calendar";
        internal const string EmptyElementName = "event-types-inspector-empty";

        /// <summary>Icon khoá 12px cạnh ô id ([SD1 §2.1]) — tên có trong <see cref="LiveOpsHubIcons.AllDesignNames"/>.</summary>
        internal const string LockIconName = "InspectorLock";

        /// <summary>Icon menu ⋮ 14px của pane-title.</summary>
        internal const string MenuIconName = "_Menu";

        private readonly VisualElement _root;
        private readonly VisualElement _titleSwatch;
        private readonly Label _titleLabel;
        private readonly Button _menuButton;
        private readonly Label _emptyLabel;
        private readonly VisualElement _fields;
        private readonly TextField _typeIdField;
        private readonly VisualElement _typeIdRow;
        private readonly Image _typeIdLockIcon;
        private readonly TextField _displayNameField;
        private readonly VisualElement _colorSlots;
        private readonly ToolbarToggle[] _colorToggles = new ToolbarToggle[LiveEventTypeDefinition.ColorSlotCount];
        private readonly Label _colorNote;
        private readonly Button _resetColorButton;
        private readonly Toggle _requiresJoinToggle;
        private readonly TextField _configKeyField;
        private readonly VisualElement _usageCard;
        private readonly Label _usageText;
        private readonly Button _showInCalendarButton;

        private string _typeId = string.Empty;

        /// <summary>Ô màu của loại đang hiện (-1 = chưa hiện loại nào) — để nhận ra "bấm lại chính ô đang bật".</summary>
        private int _appliedColorSlot = -1;
        private bool _isApplyingModel;

        internal EventTypeInspector(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));

            VisualElement title = new VisualElement { name = TitleElementName };
            title.AddToClassList(LiveOpsHubClassNames.EventTypesInspectorTitle);
            _titleSwatch = new VisualElement();
            _titleSwatch.AddToClassList(LiveOpsHubClassNames.Swatch);
            title.Add(_titleSwatch);
            _titleLabel = new Label();
            _titleLabel.AddToClassList(LiveOpsHubClassNames.Mono);
            _titleLabel.AddToClassList(LiveOpsHubClassNames.EventTypesInspectorTitleText);
            title.Add(_titleLabel);
            _menuButton = new Button(OnMenuClicked) { name = MenuButtonName };
            _menuButton.AddToClassList(LiveOpsHubClassNames.Button);
            _menuButton.Add(LiveOpsHubIcons.CreateImage(MenuIconName, 14));
            title.Add(_menuButton);
            _root.Add(title);

            _emptyLabel = new Label(LiveOpsHubStrings.EventTypesInspectorNoSelection) { name = EmptyElementName };
            _emptyLabel.AddToClassList(LiveOpsHubClassNames.EmptyBody);
            _root.Add(_emptyLabel);

            _fields = new VisualElement();
            _root.Add(_fields);

            _typeIdField = new TextField { name = TypeIdFieldName, isDelayed = true };
            _typeIdField.AddToClassList(LiveOpsHubClassNames.Mono);
            _typeIdField.RegisterValueChangedCallback(OnTypeIdChanged);
            _typeIdRow = BuildRow(LiveOpsHubStrings.EventTypesInspectorTypeIdLabel, _typeIdField);
            _typeIdLockIcon = LiveOpsHubIcons.CreateImage(LockIconName, 12);
            _typeIdRow.Add(_typeIdLockIcon);
            _fields.Add(_typeIdRow);

            _displayNameField = new TextField { name = DisplayNameFieldName, isDelayed = true };
            _displayNameField.RegisterValueChangedCallback(OnDisplayNameChanged);
            _fields.Add(BuildRow(LiveOpsHubStrings.EventTypesInspectorDisplayNameLabel, _displayNameField));

            _colorSlots = new VisualElement { name = ColorSlotsElementName };
            _colorSlots.AddToClassList(LiveOpsHubClassNames.EventTypesColorSlots);
            for (int slot = 0; slot < _colorToggles.Length; slot++)
            {
                _colorToggles[slot] = BuildColorToggle(slot);
                _colorSlots.Add(_colorToggles[slot]);
            }
            _fields.Add(BuildRow(LiveOpsHubStrings.EventTypesInspectorColorLabel, _colorSlots));

            _colorNote = new Label { name = ColorNoteElementName };
            _colorNote.AddToClassList(LiveOpsHubClassNames.EventTypesFieldSub);
            _fields.Add(_colorNote);

            _resetColorButton = new Button(OnResetColorClicked) { name = ResetColorButtonName, text = LiveOpsHubStrings.EventTypesResetColorButton };
            _resetColorButton.AddToClassList(LiveOpsHubClassNames.Button);
            _resetColorButton.AddToClassList(LiveOpsHubClassNames.EventTypesFieldSub);
            // (R-F2) Cột field của inspector xếp DỌC: nút "Về màu theo băm" đo được 188px ở ảnh 1280 skin tối, tức trọn
            // cột, trong khi chữ của nó nằm gọn MỘT dòng. EventTypesFieldSub có white-space: normal nên lượt đầu nó rơi
            // vào miễn trừ của lưới mà không hề gói dòng.
            _resetColorButton.AddToClassList(LiveOpsHubClassNames.ButtonSelfStart);
            _fields.Add(_resetColorButton);

            _requiresJoinToggle = new Toggle { name = RequiresJoinToggleName };
            _requiresJoinToggle.RegisterValueChangedCallback(OnRequiresJoinChanged);
            _fields.Add(BuildRow(LiveOpsHubStrings.EventTypesInspectorRequiresJoinLabel, _requiresJoinToggle));

            _configKeyField = new TextField { name = ConfigKeyFieldName, isDelayed = true, tooltip = LiveOpsHubStrings.EventTypesInspectorConfigKeyTooltip };
            _configKeyField.AddToClassList(LiveOpsHubClassNames.Mono);
            _configKeyField.RegisterValueChangedCallback(OnConfigKeyChanged);
            _fields.Add(BuildRow(LiveOpsHubStrings.EventTypesInspectorConfigKeyLabel, _configKeyField));
            Label configKeySub = new Label(LiveOpsHubStrings.EventTypesInspectorConfigKeySubText);
            configKeySub.AddToClassList(LiveOpsHubClassNames.EventTypesFieldSub);
            _fields.Add(configKeySub);

            _usageCard = new VisualElement { name = UsageCardElementName };
            _usageCard.AddToClassList(LiveOpsHubClassNames.Card);
            _usageCard.AddToClassList(LiveOpsHubClassNames.EventTypesUsageCard);
            Label usageTitle = new Label(LiveOpsHubStrings.EventTypesUsageCardTitle);
            usageTitle.AddToClassList(LiveOpsHubClassNames.Caption);
            _usageCard.Add(usageTitle);
            _usageText = new Label { name = UsageTextElementName };
            _usageText.AddToClassList(LiveOpsHubClassNames.EventTypesUsageText);
            _usageCard.Add(_usageText);
            _showInCalendarButton = new Button(OnShowInCalendarClicked)
            {
                name = ShowInCalendarButtonName,
                text = LiveOpsHubStrings.EventTypesOpenInCalendarButton,
            };
            _showInCalendarButton.AddToClassList(LiveOpsHubClassNames.Button);
            // (J3-01) Thẻ usage xếp DỌC — không có class này nút "Xem trên lịch" rộng trọn thẻ (đo được 200px ở 700x560).
            _showInCalendarButton.AddToClassList(LiveOpsHubClassNames.ButtonSelfStart);
            _usageCard.Add(_showInCalendarButton);
            _fields.Add(_usageCard);
        }

        /// <summary>Đổi id loại — chỉ bắn được khi loại chưa có đợt/luật (ô bị khoá khi có).</summary>
        internal event Action<string, string> TypeIdCommitted;

        internal event Action<string, string> DisplayNameCommitted;
        internal event Action<string, int> ColorSlotPicked;
        internal event Action<string> ResetColorRequested;
        internal event Action<string, bool> RequiresJoinChanged;
        internal event Action<string, string> ConfigKeyCommitted;
        internal event Action<string> ShowInCalendarRequested;

        /// <summary>Bấm icon ⋮ — màn mở menu ngữ cảnh của loại tại <see cref="Rect"/> của nút.</summary>
        internal event Action<string, Rect> MenuRequested;

        /// <summary>Loại đang hiện; "" khi chưa chọn hàng nào.</summary>
        internal string TypeId
        {
            get { return _typeId; }
        }

        /// <summary>
        /// Vẽ lại theo model. Mọi <c>SetValueWithoutNotify</c> nằm trong cờ <see cref="_isApplyingModel"/>: nếu để callback bắn
        /// lúc này thì mỗi lần vẽ lại sẽ tự gửi một lệnh sửa y hệt và sinh một Undo group rỗng.
        /// </summary>
        internal void Show(EventTypesModel model, string typeId, DateTime nowUtc, LiveOpsHubFormat format)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (format == null) throw new ArgumentNullException(nameof(format));

            _isApplyingModel = true;
            try
            {
                EventTypeRow row = FindRow(model, typeId);
                _typeId = row != null && row.IsDeclared ? row.TypeId : string.Empty;
                bool hasType = _typeId.Length > 0;
                EventTypesVisibility.SetHidden(_emptyLabel, hasType);
                EventTypesVisibility.SetHidden(_fields, !hasType);
                EventTypesVisibility.SetHidden(_titleLabel, !hasType);
                EventTypesVisibility.SetHidden(_titleSwatch, !hasType);
                EventTypesVisibility.SetHidden(_menuButton, !hasType);
                if (!hasType)
                {
                    _appliedColorSlot = -1;
                    return;
                }

                _titleLabel.text = row.TypeId;
                for (int slot = 0; slot < _colorToggles.Length; slot++)
                {
                    _titleSwatch.EnableInClassList(EventTypeColorClassNames.Of(slot), slot == row.ColorSlot);
                }
                ApplyColorToggles(model, row);

                _typeIdField.SetValueWithoutNotify(row.TypeId);
                bool canEditTypeId = model.CanEditTypeId(row.TypeId, out string lockReason);
                _typeIdField.SetEnabled(canEditTypeId);
                // Tooltip đặt trên CẢ HÀNG: element disabled không nhận hover nên tooltip trên chính ô có thể không bao giờ hiện (R-16).
                // Câu thiết kế nói phải làm gì thay vì chỉ nói "khoá"; lý do đếm được ("đang có 3 đợt") nằm trên icon khoá.
                _typeIdRow.tooltip = canEditTypeId ? string.Empty : LiveOpsHubStrings.EventTypesInspectorTypeIdLockedTooltip;
                _typeIdLockIcon.tooltip = lockReason;
                EventTypesVisibility.SetHidden(_typeIdLockIcon, canEditTypeId);

                _displayNameField.SetValueWithoutNotify(row.DisplayName);
                _requiresJoinToggle.SetValueWithoutNotify(row.RequiresJoin);
                _configKeyField.SetValueWithoutNotify(row.ConfigKey);

                string collision = model.ColorCollisionText(row.TypeId);
                string hashOverride = model.HashOverrideText(row.TypeId);
                _colorNote.text = collision.Length > 0 ? collision : hashOverride;
                EventTypesVisibility.SetHidden(_colorNote, _colorNote.text.Length == 0);
                // Trùng màu là cảnh báo (chữ warning); ghi đè theo băm đã được xử lý nên chỉ là chữ quiet [SD1 §2.1] mục 4.
                _colorNote.EnableInClassList(LiveOpsHubClassNames.TextWarning, collision.Length > 0);
                _colorNote.EnableInClassList(LiveOpsHubClassNames.TextQuiet, collision.Length == 0);

                EventTypesVisibility.SetHidden(_resetColorButton, !model.CanResetColorToHash(row.TypeId));
                _usageText.text = model.UsageText(row.TypeId, nowUtc, format);
                EventTypesVisibility.SetHidden(_showInCalendarButton, row.UsageCount == 0);
            }
            finally
            {
                _isApplyingModel = false;
            }
        }

        private void ApplyColorToggles(EventTypesModel model, EventTypeRow row)
        {
            _appliedColorSlot = row.ColorSlot;
            IReadOnlyList<int> suggestions = model.SuggestedFreeSlots(row.TypeId);
            for (int slot = 0; slot < _colorToggles.Length; slot++)
            {
                ToolbarToggle toggle = _colorToggles[slot];
                toggle.SetValueWithoutNotify(slot == row.ColorSlot);
                bool isSuggested = false;
                for (int index = 0; index < suggestions.Count; index++)
                {
                    if (suggestions[index] == slot) isSuggested = true;
                }
                toggle.EnableInClassList(LiveOpsHubClassNames.EventTypesColorSlotSuggested, isSuggested);
            }
        }

        private static EventTypeRow FindRow(EventTypesModel model, string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return null;
            IReadOnlyList<EventTypeRow> rows = model.Rows;
            for (int index = 0; index < rows.Count; index++)
            {
                if (string.Equals(rows[index].TypeId, typeId, StringComparison.Ordinal)) return rows[index];
            }
            return null;
        }

        /// <summary>Tên element của ô màu thứ <paramref name="colorSlot"/> — lệnh chụp chỉ ghi số đo element CÓ TÊN.</summary>
        internal static string ColorSlotElementName(int colorSlot)
        {
            return ColorSlotsElementName + "-" + colorSlot.ToString(CultureInfo.InvariantCulture);
        }

        private ToolbarToggle BuildColorToggle(int colorSlot)
        {
            ToolbarToggle toggle = new ToolbarToggle
            {
                name = ColorSlotElementName(colorSlot),
                tooltip = ColorSlotTooltip(colorSlot),
            };
            toggle.AddToClassList(LiveOpsHubClassNames.EventTypesColorSlot);
            VisualElement swatch = new VisualElement();
            swatch.AddToClassList(LiveOpsHubClassNames.Swatch);
            swatch.AddToClassList(LiveOpsHubClassNames.EventTypesColorSwatch);
            swatch.AddToClassList(EventTypeColorClassNames.Of(colorSlot));
            toggle.Add(swatch);
            toggle.RegisterValueChangedCallback(changed => OnColorToggleChanged(colorSlot, changed.newValue));
            return toggle;
        }

        private static string ColorSlotTooltip(int colorSlot)
        {
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesColorSlotTooltipFormat,
                EventTypesModel.SlotNamedText(colorSlot));
        }

        private void OnColorToggleChanged(int colorSlot, bool isOn)
        {
            if (_isApplyingModel || _typeId.Length == 0) return;
            if (!isOn)
            {
                // Tám ô màu là MỘT nhóm, luôn có đúng một ô bật ([SD1 §2.1] mục 3). ToolbarToggle là Toggle nên bấm lại ô đang
                // bật sẽ tự lật về false; thoát im lặng ở đây là tài liệu không đổi → không có DocumentChanged → Show() không
                // chạy lại → tâm ô về "không ô nào bật" và không bao giờ được vẽ lại. Trả lại ngay tại chỗ.
                if (colorSlot == _appliedColorSlot) _colorToggles[colorSlot].SetValueWithoutNotify(true);
                return;
            }
            ColorSlotPicked?.Invoke(_typeId, colorSlot);
        }

        private void OnTypeIdChanged(ChangeEvent<string> changed)
        {
            if (_isApplyingModel || _typeId.Length == 0) return;
            TypeIdCommitted?.Invoke(_typeId, changed.newValue);
        }

        private void OnDisplayNameChanged(ChangeEvent<string> changed)
        {
            if (_isApplyingModel || _typeId.Length == 0) return;
            DisplayNameCommitted?.Invoke(_typeId, changed.newValue);
        }

        private void OnRequiresJoinChanged(ChangeEvent<bool> changed)
        {
            if (_isApplyingModel || _typeId.Length == 0) return;
            RequiresJoinChanged?.Invoke(_typeId, changed.newValue);
        }

        private void OnConfigKeyChanged(ChangeEvent<string> changed)
        {
            if (_isApplyingModel || _typeId.Length == 0) return;
            ConfigKeyCommitted?.Invoke(_typeId, changed.newValue);
        }

        private void OnResetColorClicked()
        {
            if (_typeId.Length == 0) return;
            ResetColorRequested?.Invoke(_typeId);
        }

        private void OnShowInCalendarClicked()
        {
            if (_typeId.Length == 0) return;
            ShowInCalendarRequested?.Invoke(_typeId);
        }

        private void OnMenuClicked()
        {
            if (_typeId.Length == 0) return;
            MenuRequested?.Invoke(_typeId, _menuButton.worldBound);
        }

        private static VisualElement BuildRow(string labelText, VisualElement field)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.EventTypesField);
            Label label = new Label(labelText);
            label.AddToClassList(LiveOpsHubClassNames.EventTypesFieldLabel);
            row.Add(label);
            field.AddToClassList(LiveOpsHubClassNames.EventTypesFieldValue);
            row.Add(field);
            return row;
        }
    }
}
