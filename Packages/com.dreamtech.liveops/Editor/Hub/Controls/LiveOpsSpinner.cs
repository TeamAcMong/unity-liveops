using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Spinner "đang kiểm" 16px: đổi khung WaitSpin00…11 mỗi 83 ms ([FD §2.13]). Icon dựng sẵn của Unity thay cho vòng
    /// quay tự vẽ vì USS không có animation lặp và transition không dùng cho thứ đang hiện. Nhịp chỉ chạy khi element
    /// nằm trong panel — spinner bị gỡ khỏi cây (đổi màn, đóng cửa sổ) không để lại tác vụ lịch chạy mãi.
    /// </summary>
    internal sealed class LiveOpsSpinner : Image
    {
        internal const long FrameIntervalMilliseconds = 83;

        private IVisualElementScheduledItem _tick;
        private int _frameIndex;
        private bool _isSpinning = true;

        public LiveOpsSpinner()
        {
            scaleMode = ScaleMode.ScaleToFit;
            pickingMode = PickingMode.Ignore;
            AddToClassList(LiveOpsHubClassNames.Icon);
            AddToClassList(LiveOpsHubClassNames.IconSize16);
            AddToClassList(LiveOpsHubClassNames.Spinner);
            image = LiveOpsHubIcons.Get(LiveOpsHubIcons.SpinnerFrameName(0));
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        public bool IsSpinning => _isSpinning;

        /// <summary>Khung đang hiện (0–11) — để test đọc mà không phải so texture.</summary>
        internal int FrameIndex => _frameIndex;

        public void Start()
        {
            _isSpinning = true;
            if (panel != null) EnsureTick();
            _tick?.Resume();
        }

        public void Stop()
        {
            _isSpinning = false;
            _tick?.Pause();
        }

        /// <summary>Sang khung kế; tách khỏi lịch để test gọi trực tiếp không cần chờ 83 ms thật.</summary>
        internal void Advance()
        {
            _frameIndex = (_frameIndex + 1) % LiveOpsHubIcons.SpinnerFrames;
            image = LiveOpsHubIcons.Get(LiveOpsHubIcons.SpinnerFrameName(_frameIndex));
        }

        private void OnAttachToPanel(AttachToPanelEvent attachEvent)
        {
            if (!_isSpinning) return;
            EnsureTick();
            _tick.Resume();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent detachEvent)
        {
            _tick?.Pause();
        }

        private void EnsureTick()
        {
            if (_tick != null) return;
            _tick = schedule.Execute(Advance).Every(FrameIntervalMilliseconds);
        }
    }
}
