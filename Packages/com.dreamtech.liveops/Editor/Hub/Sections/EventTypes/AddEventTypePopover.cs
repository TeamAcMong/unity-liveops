using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Popover nhỏ của nút "Thêm loại" (7.2): hai ô — id loại và tên hiển thị. Ô màu KHÔNG hỏi ở đây: loại mới lấy
    /// <c>LiveEventTypeColorSlots.DefaultSlotFor</c> rồi đổi được ngay trong inspector, hỏi thêm một câu ở bước tạo chỉ làm chậm.
    /// <para>
    /// Nút chính khoá kèm lý do in thành chữ khi id rỗng / sai ký tự / trùng loại đã có (SPIKE-B SP-3: lý do là chữ, tooltip chỉ
    /// là phụ). Esc đóng popover do lớp gốc lo; Enter trong ô id nhảy xuống ô tên (ô <c>isDelayed</c> commit trước).
    /// </para>
    /// </summary>
    internal sealed class AddEventTypePopover : LiveOpsPopoverContent
    {
        internal const string TitleElementName = "add-event-type-title";
        internal const string TypeIdFieldName = "add-event-type-id";
        internal const string DisplayNameFieldName = "add-event-type-name";
        internal const string ConfirmButtonName = "add-event-type-confirm";
        internal const string CancelButtonName = "add-event-type-cancel";

        private const int PopoverWidth = 320;
        private const int PopoverHeight = 132;

        private readonly HashSet<string> _existingTypeIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Action<string, string> _confirm;

        private TextField _typeIdField;
        private TextField _displayNameField;
        private LiveOpsButtonSlot _confirmSlot;

        /// <param name="existingTypeIds">Id đã có trong nháp — dùng để báo trùng ngay khi gõ, không đợi bấm Thêm.</param>
        /// <param name="confirm">(id, tên hiển thị) — màn dựng lệnh sửa, popover không đụng phiên.</param>
        internal AddEventTypePopover(IReadOnlyList<string> existingTypeIds, Action<string, string> confirm)
        {
            if (existingTypeIds != null)
            {
                for (int index = 0; index < existingTypeIds.Count; index++) _existingTypeIds.Add(existingTypeIds[index]);
            }
            _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        }

        protected override Vector2 PopoverSize
        {
            get { return new Vector2(PopoverWidth, PopoverHeight); }
        }

        protected override Focusable InitialFocus
        {
            get { return _typeIdField; }
        }

        /// <summary>Test đọc trạng thái nút chính mà không mở cửa sổ (<c>BuildForTest</c> rồi gõ vào ô).</summary>
        internal LiveOpsButtonSlot ConfirmSlot
        {
            get { return _confirmSlot; }
        }

        internal TextField TypeIdField
        {
            get { return _typeIdField; }
        }

        internal TextField DisplayNameField
        {
            get { return _displayNameField; }
        }

        protected override VisualElement BuildContent()
        {
            // Không class riêng: cửa sổ popover là panel riêng chỉ nạp theme + components + feedback + motion
            // (LiveOpsFeedbackStyleSheets.PopoverSheets), nên EventTypesSection.uss không tới đây — padding lấy từ
            // .liveops-hub-popover mà lớp gốc đã gắn.
            VisualElement content = new VisualElement();

            Label title = new Label(LiveOpsHubStrings.EventTypesAddPopoverTitle) { name = TitleElementName };
            title.AddToClassList(LiveOpsHubClassNames.Caption);
            content.Add(title);

            _typeIdField = new TextField(LiveOpsHubStrings.EventTypesAddPopoverIdLabel) { name = TypeIdFieldName };
            _typeIdField.AddToClassList(LiveOpsHubClassNames.Mono);
            _typeIdField.RegisterValueChangedCallback(OnFieldChanged);
            content.Add(_typeIdField);

            _displayNameField = new TextField(LiveOpsHubStrings.EventTypesAddPopoverNameLabel) { name = DisplayNameFieldName };
            content.Add(_displayNameField);

            VisualElement actions = new VisualElement();
            actions.AddToClassList(LiveOpsHubClassNames.HelpboxActions);
            Button cancelButton = new Button(ClosePopover)
            {
                name = CancelButtonName,
                text = LiveOpsHubStrings.EventTypesAddPopoverCancelButton,
            };
            cancelButton.AddToClassList(LiveOpsHubClassNames.Button);
            actions.Add(cancelButton);

            Button confirmButton = new Button(OnConfirmClicked)
            {
                name = ConfirmButtonName,
                text = LiveOpsHubStrings.EventTypesAddPopoverConfirmButton,
            };
            confirmButton.AddToClassList(LiveOpsHubClassNames.Button);
            confirmButton.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            _confirmSlot = new LiveOpsButtonSlot(confirmButton);
            actions.Add(_confirmSlot);
            content.Add(actions);

            RefreshConfirmState();
            return content;
        }

        /// <summary>Id gõ vào hợp lệ chưa; <paramref name="reason"/> là câu in cạnh nút khi chưa.</summary>
        internal bool TryValidate(out string reason)
        {
            string typeId = _typeIdField == null ? string.Empty : _typeIdField.value;
            if (string.IsNullOrEmpty(typeId))
            {
                reason = LiveOpsHubStrings.EventTypesAddPopoverIdEmptyReason;
                return false;
            }
            for (int index = 0; index < typeId.Length; index++)
            {
                char character = typeId[index];
                if (character != LiveEventInstance.ReservedSeparator && character != '\n' && character != '\r') continue;
                reason = LiveOpsHubStrings.EventTypesAddPopoverIdCharsetReason;
                return false;
            }
            if (_existingTypeIds.Contains(typeId))
            {
                reason = LiveOpsHubStrings.EventTypesAddPopoverIdDuplicateReason;
                return false;
            }
            reason = string.Empty;
            return true;
        }

        private void OnFieldChanged(ChangeEvent<string> changed)
        {
            RefreshConfirmState();
        }

        private void RefreshConfirmState()
        {
            bool isValid = TryValidate(out string reason);
            _confirmSlot.SetEnabledWithReason(isValid, reason);
        }

        private void OnConfirmClicked()
        {
            if (!TryValidate(out _)) return;
            string typeId = _typeIdField.value;
            string displayName = _displayNameField.value;
            ClosePopover();
            _confirm(typeId, displayName);
        }
    }
}
