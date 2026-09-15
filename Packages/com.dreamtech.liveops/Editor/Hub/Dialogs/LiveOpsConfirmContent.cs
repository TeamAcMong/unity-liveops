using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Nội dung hộp xác nhận cấp 1 / cấp 2 (8.6, [FD §3.10]) — tách khỏi <see cref="LiveOpsConfirmWindow"/> để test gửi phím mà không
    /// cần <c>ShowModalUtility</c>. Luật an toàn là lý do của lớp này:
    /// <list type="bullet">
    /// <item>Enter, KeypadEnter và Esc đều ra <see cref="LiveOpsConfirmResult.Safe"/> — <c>EditorUtility.DisplayDialog</c> coi ok là
    /// Enter còn Esc/× trả cancel, nên đặt "Giữ lại" ở đâu cũng có một phím chạy hành động phá huỷ.</item>
    /// <item>Nút phá huỷ chỉ chạy khi click, hoặc Space lúc chính nó đang focus. <c>NavigationSubmitEvent</c> (Enter/Space sinh ra) bị
    /// chặn ở TrickleDown để Button không tự "bấm" nút phá huỷ đang focus khi người dùng nhấn Enter.</item>
    /// <item>Cấp 2: ô gõ focus sẵn; Enter trong ô KHÔNG làm gì kể cả khi đã gõ khớp; nút phá huỷ khoá tới khi giá trị bằng đúng
    /// <see cref="LiveOpsConfirmRequest.TypeToConfirmText"/> (Ordinal — "Weekly-pass-35" hay thừa khoảng trắng đều không mở).</item>
    /// <item>Thiếu UXML: chỉ còn câu lỗi + nút an toàn — không dựng được câu hậu quả thì không cho bấm phá huỷ.</item>
    /// </list>
    /// </summary>
    internal sealed class LiveOpsConfirmContent : VisualElement
    {
        internal const string RootElementName = "confirm-root";
        internal const string ContentElementName = "confirm-content";
        internal const string TitleElementName = "confirm-title";
        internal const string BodyElementName = "confirm-body";
        internal const string WarningElementName = "confirm-warning";
        internal const string TypeAreaElementName = "confirm-type";
        internal const string TypePrefixElementName = "confirm-type-prefix";
        internal const string TypeIdElementName = "confirm-type-id";
        internal const string TypeSuffixElementName = "confirm-type-suffix";
        internal const string TypeFieldElementName = "confirm-type-field";
        internal const string TypeHintElementName = "confirm-type-hint";
        internal const string KeyHintElementName = "confirm-key-hint";
        internal const string DestructiveHostElementName = "confirm-destructive-host";
        internal const string DestructiveButtonElementName = "confirm-destructive";
        internal const string SafeButtonElementName = "confirm-safe";
        internal const string MissingLayoutElementName = "confirm-missing-layout";

        private readonly Button _safeButton;
        private readonly Button _destructiveButton;
        private readonly LiveOpsButtonSlot _destructiveSlot;
        private readonly TextField _typeField;

        public LiveOpsConfirmContent(LiveOpsConfirmRequest request) : this(request, new AssetDatabaseLiveOpsHubLayoutLoader())
        {
        }

        /// <param name="layoutLoader">Test/chụp thay loader (V-16) — vd thiếu UXML.</param>
        internal LiveOpsConfirmContent(LiveOpsConfirmRequest request, ILiveOpsHubLayoutLoader layoutLoader)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request), LiveOpsHubStrings.FeedbackErrorConfirmRequestMissing);
            if (layoutLoader == null) throw new ArgumentNullException(nameof(layoutLoader));
            name = RootElementName;
            AddToClassList(LiveOpsHubClassNames.Root);
            // Cửa sổ hộp là panel riêng: gắn lại token + sheet và class skin như root của cửa sổ hub.
            LiveOpsFeedbackStyleSheets.AddStyleSheets(this, LiveOpsFeedbackStyleSheets.ConfirmSheets, layoutLoader);
            LiveOpsHubSkin.Attach(this);

            RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit, TrickleDown.TrickleDown);

            VisualTreeAsset layout = layoutLoader.LoadVisualTree(LiveOpsHubPaths.ConfirmWindowUxml);
            if (layout == null)
            {
                IsLayoutMissing = true;
                _safeButton = BuildMissingLayout(request);
                return;
            }
            layout.CloneTree(this);
            VisualElement content = this.Q(ContentElementName);
            content?.EnableInClassList(LiveOpsHubClassNames.ConfirmLevel2, request.Level == LiveOpsConfirmLevel.TypeToConfirm);

            this.Q<Label>(TitleElementName).text = request.Title;
            BindBody(request);

            _safeButton = this.Q<Button>(SafeButtonElementName);
            _safeButton.text = request.SafeLabel;
            _safeButton.clicked += () => Complete(LiveOpsConfirmResult.Safe);

            _destructiveButton = new Button { name = DestructiveButtonElementName, text = request.DestructiveLabel };
            _destructiveButton.AddToClassList(LiveOpsHubClassNames.Button);
            _destructiveButton.AddToClassList(LiveOpsHubClassNames.ButtonDanger);
            _destructiveButton.clicked += OnDestructiveClicked;
            _destructiveSlot = new LiveOpsButtonSlot(_destructiveButton);
            this.Q(DestructiveHostElementName).Add(_destructiveSlot);

            this.Q<Label>(KeyHintElementName).text = request.KeyHint;

            VisualElement typeArea = this.Q(TypeAreaElementName);
            _typeField = this.Q<TextField>(TypeFieldElementName);
            bool isTypeToConfirm = request.Level == LiveOpsConfirmLevel.TypeToConfirm;
            typeArea.EnableInClassList(LiveOpsHubClassNames.ConfirmTypeHidden, !isTypeToConfirm);
            if (isTypeToConfirm)
            {
                this.Q<Label>(TypePrefixElementName).text = LiveOpsHubStrings.FeedbackConfirmTypePrefix;
                this.Q<Label>(TypeIdElementName).text = request.TypeToConfirmText;
                this.Q<Label>(TypeSuffixElementName).text = LiveOpsHubStrings.FeedbackConfirmTypeSuffix;
                this.Q<Label>(TypeHintElementName).text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.FeedbackConfirmTypeHintFormat,
                    request.DestructiveLabel);
                _typeField.SetValueWithoutNotify(string.Empty);
                _typeField.RegisterValueChangedCallback(changeEvent => UpdateDestructiveLock(changeEvent.newValue));
                UpdateDestructiveLock(string.Empty);
            }
        }

        /// <summary>Kết quả chốt đúng một lần: Enter/Esc/nút an toàn → Safe; nút phá huỷ (click hoặc Tab + Space) → Destructive.</summary>
        public event Action<LiveOpsConfirmResult> Completed;

        public LiveOpsConfirmRequest Request { get; }

        internal bool IsCompleted { get; private set; }
        internal LiveOpsConfirmResult Result { get; private set; }
        internal bool IsLayoutMissing { get; }
        internal Button SafeButton => _safeButton;

        /// <summary>null khi thiếu UXML (hộp chỉ còn nút an toàn).</summary>
        internal Button DestructiveButton => _destructiveButton;

        internal LiveOpsButtonSlot DestructiveSlot => _destructiveSlot;

        /// <summary>null ở cấp 1 và khi thiếu UXML.</summary>
        internal TextField TypeField => Request.Level == LiveOpsConfirmLevel.TypeToConfirm ? _typeField : null;

        internal bool IsDestructiveEnabled => _destructiveButton != null && _destructiveButton.enabledSelf;

        /// <summary>Cấp 1: nút an toàn focus sẵn. Cấp 2: ô gõ focus sẵn (nút an toàn vẫn là nút chính, chỉ đậm).</summary>
        public void FocusInitial()
        {
            if (TypeField != null) TypeField.Focus();
            else _safeButton?.Focus();
        }

        private void BindBody(LiveOpsConfirmRequest request)
        {
            Label body = this.Q<Label>(BodyElementName);
            HelpBox warning = this.Q<HelpBox>(WarningElementName);
            bool isWarning = request.BodyStyle == HelpBoxMessageType.Warning;
            body.text = isWarning ? string.Empty : request.Body;
            body.tooltip = isWarning ? string.Empty : request.BodyTooltip;
            body.EnableInClassList(LiveOpsHubClassNames.ConfirmBodyHidden, isWarning || request.Body.Length == 0);
            warning.text = isWarning ? request.Body : string.Empty;
            warning.tooltip = isWarning ? request.BodyTooltip : string.Empty;
            warning.messageType = HelpBoxMessageType.Warning;
            warning.EnableInClassList(LiveOpsHubClassNames.ConfirmWarningHidden, !isWarning);
        }

        private Button BuildMissingLayout(LiveOpsConfirmRequest request)
        {
            VisualElement missing = new VisualElement { name = MissingLayoutElementName };
            missing.AddToClassList(LiveOpsHubClassNames.ConfirmMissingLayout);
            missing.Add(new Label(request.Title));
            missing.Add(new Label(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.FeedbackConfirmLayoutMissingFormat,
                LiveOpsHubPaths.ConfirmWindowUxml)));
            Button safe = new Button(() => Complete(LiveOpsConfirmResult.Safe)) { name = SafeButtonElementName, text = request.SafeLabel };
            safe.AddToClassList(LiveOpsHubClassNames.Button);
            safe.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            missing.Add(safe);
            Add(missing);
            return safe;
        }

        private void UpdateDestructiveLock(string typed)
        {
            bool matches = string.Equals(typed ?? string.Empty, Request.TypeToConfirmText, StringComparison.Ordinal);
            _destructiveButton.SetEnabled(matches);
            // Tooltip nằm trên slot: element disabled có thể không nhận hover (R-16). Không in lý do cạnh nút — dòng gợi ý dưới ô đã nói.
            _destructiveSlot.tooltip = matches ? string.Empty : LiveOpsHubStrings.FeedbackConfirmLockedTooltip;
        }

        private void OnDestructiveClicked()
        {
            if (!IsDestructiveEnabled) return;
            Complete(LiveOpsConfirmResult.Destructive);
        }

        private void OnKeyDown(KeyDownEvent keyDown)
        {
            VisualElement target = keyDown.target as VisualElement;
            switch (keyDown.keyCode)
            {
                case KeyCode.Escape:
                    keyDown.StopPropagation();
                    Complete(LiveOpsConfirmResult.Safe);
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    keyDown.StopPropagation();
                    // Enter trong ô gõ không chạy nút nào, kể cả đã gõ khớp: người đang gõ id hay nhấn Enter theo thói quen.
                    if (IsInside(target, TypeField)) return;
                    Complete(LiveOpsConfirmResult.Safe);
                    break;
                case KeyCode.Space:
                    if (_destructiveButton != null && IsInside(target, _destructiveButton))
                    {
                        keyDown.StopPropagation();
                        if (IsDestructiveEnabled) Complete(LiveOpsConfirmResult.Destructive);
                    }
                    else if (_safeButton != null && IsInside(target, _safeButton))
                    {
                        keyDown.StopPropagation();
                        Complete(LiveOpsConfirmResult.Safe);
                    }
                    break;
            }
        }

        private void OnNavigationSubmit(NavigationSubmitEvent submitEvent)
        {
            // Phím đã được KeyDown quyết; để Button tự nhận submit thì Enter lúc nút phá huỷ đang focus sẽ chạy phá huỷ.
            submitEvent.StopPropagation();
#if !UNITY_2023_2_OR_NEWER
            // 2022.3: StopPropagation không chặn hành động mặc định của Button tại target — phải PreventDefault.
            submitEvent.PreventDefault();
#endif
        }

        private void Complete(LiveOpsConfirmResult result)
        {
            if (IsCompleted) return;
            IsCompleted = true;
            Result = result;
            Completed?.Invoke(result);
        }

        private static bool IsInside(VisualElement element, VisualElement container)
        {
            if (container == null) return false;
            for (VisualElement current = element; current != null; current = current.hierarchy.parent)
            {
                if (current == container) return true;
            }
            return false;
        }
    }
}
