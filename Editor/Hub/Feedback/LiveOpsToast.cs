using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Toast cho thao tác sửa lịch Undo được (8.5, [FD §3.9]): con cuối của cột nội dung (left 8, bottom 8, cao 24, max-width 520),
    /// icon info · câu (ellipsis cuối, tooltip đủ) · nút Hoàn tác/Làm lại · nút đóng. Hiện 6 giây, hover dừng đếm, toast mới thay cũ.
    /// <para>
    /// Toast không tự sửa gì: nó chỉ giữ số group lúc tạo (<see cref="LiveOpsToastModel.UndoGroup"/>) và hỏi
    /// <see cref="LiveOpsHubUndoTracker.IsGroupOnTop"/>. Hết đỉnh → nút khoá kèm "Đã có thao tác khác sau đó" — bấm Hoàn tác lúc đó
    /// sẽ gỡ thao tác của người khác/màn khác, không phải câu đang hiện. ⌘Z của Unity gỡ đúng group này thì toast đổi chữ như khi
    /// bấm nút (nghe <see cref="LiveOpsHubUndoTracker.UndoRedoPerformed"/>), để hai đường Undo không nói hai câu khác nhau.
    /// </para>
    /// Đồng hồ tiêm được (giây, đơn điệu) để test đếm 6 giây và dừng khi hover mà không chờ thời gian thật.
    /// </summary>
    internal sealed class LiveOpsToast : VisualElement
    {
        public const double VisibleSeconds = 6.0;

        /// <summary>Nhịp đếm lùi và làm mới trạng thái nút (Undo stack đổi không có sự kiện cho mọi trường hợp).</summary>
        internal const long TickMilliseconds = 100;

        /// <summary>= thời lượng transition ra trong liveops-hub-motion.uss: ẩn hẳn (display none) sau khi mờ xong.</summary>
        internal const long HideAfterFadeMilliseconds = 90;

        internal const string ElementName = "hub-toast";
        internal const string MessageElementName = "hub-toast-message";
        internal const string ActionButtonElementName = "hub-toast-action";
        internal const string CloseButtonElementName = "hub-toast-close";

        private const string InfoIconName = "console.infoicon.sml";
        private const string CloseIconName = "clear";
        private const int InfoIconSize = 16;
        private const int CloseIconSize = 10;

        private readonly LiveOpsHubUndoTracker _undoTracker;
        private readonly Func<double> _clockSeconds;
        private readonly Label _message;
        private readonly Button _actionButton;
        private readonly LiveOpsButtonSlot _actionSlot;
        private readonly Button _closeButton;

        private LiveOpsToastModel _model;
        private double _remainingSeconds;
        private double _lastTickSeconds;
        private bool _isHovered;
        private int _hideVersion;
        private IVisualElementScheduledItem _ticker;

        public LiveOpsToast(LiveOpsHubUndoTracker undoTracker) : this(undoTracker, () => EditorApplication.timeSinceStartup)
        {
        }

        /// <param name="clockSeconds">Giây đơn điệu; test truyền đồng hồ tay để đếm 6 giây không cần chờ.</param>
        internal LiveOpsToast(LiveOpsHubUndoTracker undoTracker, Func<double> clockSeconds)
        {
            _undoTracker = undoTracker ?? throw new ArgumentNullException(nameof(undoTracker));
            _clockSeconds = clockSeconds ?? throw new ArgumentNullException(nameof(clockSeconds));
            name = ElementName;
            AddToClassList(LiveOpsHubClassNames.Toast);
            AddToClassList(LiveOpsHubClassNames.ToastHidden);

            Image icon = LiveOpsHubIcons.CreateImage(InfoIconName, InfoIconSize);
            icon.AddToClassList(LiveOpsHubClassNames.ToastIcon);
            Add(icon);

            _message = new Label { name = MessageElementName };
            _message.AddToClassList(LiveOpsHubClassNames.ToastMessage);
            Add(_message);

            _actionButton = new Button(HandleActionClicked) { name = ActionButtonElementName };
            _actionButton.AddToClassList(LiveOpsHubClassNames.ToastActionButton);
            _actionSlot = new LiveOpsButtonSlot(_actionButton);
            _actionSlot.AddToClassList(LiveOpsHubClassNames.ToastAction);
            Add(_actionSlot);

            _closeButton = new Button(Hide) { name = CloseButtonElementName, tooltip = LiveOpsHubStrings.FeedbackToastCloseTooltip };
            _closeButton.AddToClassList(LiveOpsHubClassNames.ToastClose);
            _closeButton.Add(LiveOpsHubIcons.CreateImage(CloseIconName, CloseIconSize));
            Add(_closeButton);

            RegisterCallback<PointerEnterEvent>(pointerEvent => SetHovered(true));
            RegisterCallback<PointerLeaveEvent>(pointerEvent => SetHovered(false));
            RegisterCallback<AttachToPanelEvent>(panelEvent => OnAttached());
            RegisterCallback<DetachFromPanelEvent>(panelEvent => OnDetached());
        }

        /// <summary>Toast đang hiện; null khi đã ẩn.</summary>
        public LiveOpsToastModel Model => IsVisible ? _model : null;

        public bool IsVisible { get; private set; }

        internal double RemainingSeconds => _remainingSeconds;
        internal Label MessageLabel => _message;
        internal Button ActionButton => _actionButton;
        internal LiveOpsButtonSlot ActionSlot => _actionSlot;
        internal Button CloseButton => _closeButton;

        /// <summary>Toast mới thay toast cũ và đếm lại 6 giây.</summary>
        public void Show(LiveOpsToastModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _hideVersion++;
            _remainingSeconds = VisibleSeconds;
            _lastTickSeconds = _clockSeconds();
            IsVisible = true;
            _message.text = model.DisplayMessage;
            tooltip = model.IsUndone ? LiveOpsHubStrings.KitToastUndonePrefix + model.Tooltip : model.Tooltip;
            RemoveFromClassList(LiveOpsHubClassNames.ToastHidden);
            AddToClassList(LiveOpsHubClassNames.ToastVisible);
            RefreshActionState();
            EnsureTicker();
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            RemoveFromClassList(LiveOpsHubClassNames.ToastVisible);
            _ticker?.Pause();
            // Mờ dần 90ms rồi mới bỏ khỏi layout; toast mới hiện trong lúc mờ thì phiên bản đổi và lệnh ẩn cũ bị bỏ qua.
            int version = ++_hideVersion;
            if (panel == null)
            {
                AddToClassList(LiveOpsHubClassNames.ToastHidden);
                return;
            }
            schedule.Execute(() =>
            {
                if (version == _hideVersion && !IsVisible) AddToClassList(LiveOpsHubClassNames.ToastHidden);
            }).StartingIn(HideAfterFadeMilliseconds);
        }

        /// <summary>Một nhịp đồng hồ: đếm lùi (trừ khi đang hover) và làm mới nút theo stack Undo.</summary>
        internal void Tick()
        {
            if (!IsVisible) return;
            double now = _clockSeconds();
            double elapsed = Math.Max(0.0, now - _lastTickSeconds);
            _lastTickSeconds = now;
            if (!_isHovered) _remainingSeconds -= elapsed;
            if (_remainingSeconds <= 0.0)
            {
                Hide();
                return;
            }
            RefreshActionState();
        }

        /// <summary>Hover dừng đếm; rời chuột đếm tiếp phần còn lại (không đếm lại từ đầu).</summary>
        internal void SetHovered(bool isHovered)
        {
            if (_isHovered == isHovered) return;
            // Chốt thời gian tới lúc đổi trạng thái: đoạn trước khi vào hover vẫn bị trừ, đoạn đang hover thì không.
            Tick();
            _isHovered = isHovered;
            _lastTickSeconds = _clockSeconds();
        }

        internal void RefreshActionState()
        {
            if (_model == null || !_model.HasUndo)
            {
                _actionSlot.AddToClassList(LiveOpsHubClassNames.ToastActionHidden);
                return;
            }
            _actionSlot.RemoveFromClassList(LiveOpsHubClassNames.ToastActionHidden);
            string label = _model.IsUndone ? RedoLabel() : UndoLabel();
            if (!string.Equals(_actionButton.text, label, StringComparison.Ordinal)) _actionButton.text = label;
            bool isOnTop = _undoTracker.IsGroupOnTop(_model.UndoGroup);
            // Không đổi gì thì không ghi lại chữ/tooltip mỗi nhịp 100ms (mỗi lần ghi làm panel dựng lại text).
            if (isOnTop == _actionButton.enabledSelf && isOnTop != _actionSlot.IsBlocked) return;
            _actionSlot.SetEnabledWithReason(isOnTop, isOnTop ? string.Empty : LiveOpsHubStrings.FeedbackToastUndoUnavailableReason);
            if (!isOnTop) _actionSlot.tooltip = LiveOpsHubStrings.FeedbackToastUndoUnavailableTooltip;
        }

        internal static string UndoLabel()
        {
            return WithKey(LiveOpsHubStrings.FeedbackToastUndoLabel, LiveOpsHubKeyLabels.Undo);
        }

        internal static string RedoLabel()
        {
            return WithKey(LiveOpsHubStrings.FeedbackToastRedoLabel, LiveOpsHubKeyLabels.Redo);
        }

        private static string WithKey(string label, string keyLabel)
        {
            return keyLabel.Length == 0 ? label : label + " (" + keyLabel + ")";
        }

        /// <summary>Handler của nút Hoàn tác/Làm lại (Button.clicked) — internal để test bấm không cần panel.</summary>
        internal void HandleActionClicked()
        {
            if (_model == null || !_model.HasUndo || !IsVisible) return;
            // Kiểm lại ngay lúc bấm: trạng thái nút có thể cũ tới 100ms, và gỡ nhầm thao tác khác là mất việc của người dùng.
            if (!_undoTracker.IsGroupOnTop(_model.UndoGroup))
            {
                RefreshActionState();
                return;
            }
            bool wasUndone = _model.IsUndone;
            LiveOpsToastModel target = wasUndone ? _model.AsRedone() : _model.AsUndone();
            if (wasUndone) _undoTracker.PerformRedo();
            else _undoTracker.PerformUndo();
            // Sự kiện Undo thường đã đổi toast; đổi thêm ở đây để toast đúng cả khi bản Unity không bắn sự kiện đồng bộ.
            if (_model.IsUndone == wasUndone) Show(target);
        }

        private void OnUndoRedoPerformed()
        {
            if (!IsVisible || _model == null || !_model.HasUndo)
            {
                return;
            }
            // So bằng số group của sự kiện (không so tên): hai thao tác cùng câu toast vẫn là hai bước khác nhau.
            bool isThisGroup = _undoTracker.IsLastUndoRedoOf(_model.UndoGroup);
            // ⌘Z gỡ đúng thao tác của toast → "Đã hoàn tác: …" + Làm lại; ⌘⇧Z làm lại nó → câu gốc + Hoàn tác, đếm lại 6 giây.
            if (isThisGroup && !_undoTracker.LastUndoRedoWasRedo && !_model.IsUndone) Show(_model.AsUndone());
            else if (isThisGroup && _undoTracker.LastUndoRedoWasRedo && _model.IsUndone) Show(_model.AsRedone());
            else RefreshActionState();
        }

        private void EnsureTicker()
        {
            if (panel == null) return;
            if (_ticker == null) _ticker = schedule.Execute(Tick).Every(TickMilliseconds);
            else _ticker.Resume();
        }

        private void OnAttached()
        {
            _undoTracker.UndoRedoPerformed -= OnUndoRedoPerformed;
            _undoTracker.UndoRedoPerformed += OnUndoRedoPerformed;
            if (IsVisible) EnsureTicker();
        }

        private void OnDetached()
        {
            _undoTracker.UndoRedoPerformed -= OnUndoRedoPerformed;
            _ticker?.Pause();
        }

        /// <summary>Test nối sự kiện Undo mà không gắn panel (toast chỉ nghe tracker khi đang ở trên panel).</summary>
        internal void ListenToUndoForTest()
        {
            _undoTracker.UndoRedoPerformed -= OnUndoRedoPerformed;
            _undoTracker.UndoRedoPerformed += OnUndoRedoPerformed;
        }
    }
}
