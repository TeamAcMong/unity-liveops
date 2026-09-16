using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Popover "Đổi key remote…" của menu ⋮ màn Tổng quan (7.1, Q-1): đúng MỘT ô — khoá remote config mà game đọc lịch từ đó.
    /// <para>
    /// Vì sao key là dữ liệu chứ không phải hằng (Q-1, PD-8): bản game 0.1.0 đọc JSON định dạng 2 sẽ bỏ <c>recurring</c> im
    /// lặng, nên đội nào cần tách bản mới phải đổi được key mà không phải sửa code. Key nằm trong chính asset lịch
    /// (<c>remoteConfigKey</c>), nên đổi nó là một lệnh sửa có Undo như mọi lệnh khác.
    /// </para>
    /// <para>
    /// Nút chính khoá kèm lý do IN THÀNH CHỮ khi key rỗng hoặc có khoảng trắng (SPIKE-B SP-3); ô KHÔNG đặt <c>isDelayed</c> để
    /// lý do đổi theo từng ký tự gõ vào. Esc đóng popover do lớp gốc lo.
    /// </para>
    /// </summary>
    internal sealed class OverviewRemoteKeyPopover : LiveOpsPopoverContent
    {
        internal const string TitleElementName = "overview-remote-key-title";
        internal const string KeyFieldElementName = "overview-remote-key-field";
        internal const string ConfirmButtonElementName = "overview-remote-key-confirm";
        internal const string CancelButtonElementName = "overview-remote-key-cancel";

        private const int PopoverWidth = 320;
        private const int PopoverHeight = 116;

        private readonly string _currentKey;
        private readonly Action<string> _confirm;

        private TextField _keyField;
        private LiveOpsButtonSlot _confirmSlot;

        /// <param name="currentKey">Key đang dùng — ô mở ra đã có sẵn chữ để người dùng sửa, không phải gõ lại từ đầu.</param>
        /// <param name="confirm">Màn dựng lệnh sửa; popover không đụng phiên.</param>
        internal OverviewRemoteKeyPopover(string currentKey, Action<string> confirm)
        {
            _currentKey = currentKey ?? string.Empty;
            _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        }

        protected override Vector2 PopoverSize
        {
            get { return new Vector2(PopoverWidth, PopoverHeight); }
        }

        protected override Focusable InitialFocus
        {
            get { return _keyField; }
        }

        internal TextField KeyField
        {
            get { return _keyField; }
        }

        internal LiveOpsButtonSlot ConfirmSlot
        {
            get { return _confirmSlot; }
        }

        protected override VisualElement BuildContent()
        {
            VisualElement content = new VisualElement();

            Label title = new Label(LiveOpsHubStrings.OverviewRemoteKeyPopoverTitle) { name = TitleElementName };
            title.AddToClassList(LiveOpsHubClassNames.Caption);
            content.Add(title);

            _keyField = new TextField(LiveOpsHubStrings.OverviewRemoteKeyPopoverFieldLabel)
            {
                name = KeyFieldElementName,
                value = _currentKey,
            };
            _keyField.AddToClassList(LiveOpsHubClassNames.Mono);
            _keyField.RegisterValueChangedCallback(changeEvent => RefreshConfirmState());
            content.Add(_keyField);

            VisualElement actions = new VisualElement();
            actions.AddToClassList(LiveOpsHubClassNames.HelpboxActions);

            Button cancelButton = new Button(ClosePopover)
            {
                name = CancelButtonElementName,
                text = LiveOpsHubStrings.OverviewRemoteKeyPopoverCancelButton,
            };
            cancelButton.AddToClassList(LiveOpsHubClassNames.Button);
            actions.Add(cancelButton);

            Button confirmButton = new Button(OnConfirmClicked)
            {
                name = ConfirmButtonElementName,
                text = LiveOpsHubStrings.OverviewRemoteKeyPopoverConfirmButton,
            };
            confirmButton.AddToClassList(LiveOpsHubClassNames.Button);
            confirmButton.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            _confirmSlot = new LiveOpsButtonSlot(confirmButton);
            actions.Add(_confirmSlot);
            content.Add(actions);

            RefreshConfirmState();
            return content;
        }

        /// <summary>Key gõ vào dùng được chưa; <paramref name="reason"/> là câu in cạnh nút khi chưa.</summary>
        internal bool TryValidate(out string reason)
        {
            string key = _keyField == null ? string.Empty : _keyField.value ?? string.Empty;
            if (key.Length == 0)
            {
                // Key rỗng = game không biết đọc lịch ở khoá nào; đó là hỏng im lặng nên chặn ngay tại ô.
                reason = LiveOpsHubStrings.OverviewRemoteKeyEmptyReason;
                return false;
            }
            for (int index = 0; index < key.Length; index++)
            {
                if (!char.IsWhiteSpace(key[index])) continue;
                reason = LiveOpsHubStrings.OverviewRemoteKeyWhitespaceReason;
                return false;
            }
            if (string.Equals(key, _currentKey, StringComparison.Ordinal))
            {
                reason = LiveOpsHubStrings.OverviewRemoteKeyUnchangedReason;
                return false;
            }
            reason = string.Empty;
            return true;
        }

        private void RefreshConfirmState()
        {
            if (_confirmSlot == null) return;
            string reason;
            bool valid = TryValidate(out reason);
            LiveOpsHubStyle.SetEnabledWithReason(_confirmSlot, valid, reason);
        }

        private void OnConfirmClicked()
        {
            string reason;
            if (!TryValidate(out reason)) return;
            string key = _keyField.value;
            ClosePopover();
            _confirm(key);
        }
    }
}
