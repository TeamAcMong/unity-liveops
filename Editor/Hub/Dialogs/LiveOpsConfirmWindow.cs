using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Cửa sổ modal của hộp xác nhận phá huỷ (8.6, [FD §3.10]): 400×212 (cấp 1) hoặc 400×290 (cấp 2), tiêu đề "LiveOps Hub", nội dung
    /// <see cref="LiveOpsConfirmContent"/>. Mọi section gọi qua <c>services.Confirmation</c> (<see cref="ModalLiveOpsHubConfirmationPresenter"/>),
    /// không gọi thẳng lớp này — test dùng <c>ScriptedLiveOpsHubConfirmationPresenter</c> vì <c>ShowModalUtility</c> chặn batchmode.
    /// Đóng bằng nút × của hệ điều hành = <see cref="LiveOpsConfirmResult.Safe"/> (kết quả mặc định không bao giờ là phá huỷ).
    /// </summary>
    internal sealed class LiveOpsConfirmWindow : EditorWindow
    {
        internal const float Width = 400f;
        internal const float Level1Height = 212f;
        internal const float TypeToConfirmHeight = 290f;

        private LiveOpsConfirmContent _content;

        internal LiveOpsConfirmContent Content => _content;
        internal LiveOpsConfirmResult Result { get; private set; } = LiveOpsConfirmResult.Safe;

        /// <summary>Mở modal và chờ người dùng; trả Safe khi Enter/Esc/nút an toàn/đóng cửa sổ. Không gọi trong test (chặn batchmode).</summary>
        public static LiveOpsConfirmResult Show(LiveOpsConfirmRequest request)
        {
            LiveOpsConfirmWindow window = Create(request, null);
            window.ShowModalUtility();
            // Cửa sổ đã đóng (Unity object == null) nhưng instance C# vẫn giữ Result đã chốt.
            return window.Result;
        }

        /// <summary>
        /// Mở KHÔNG modal (test UI + kịch bản chụp <c>hf-confirm-level*-layout</c>): cùng nội dung, cùng kích thước, cửa sổ thường để
        /// batchmode không bị chặn. Người gọi đóng cửa sổ.
        /// </summary>
        internal static LiveOpsConfirmWindow OpenForTest(LiveOpsConfirmRequest request, ILiveOpsHubLayoutLoader layoutLoader = null)
        {
            LiveOpsConfirmWindow window = Create(request, layoutLoader);
            window.Show();
            // SP-16: kích thước đặt SAU Show mới giữ (đặt trước bị kẹp ≈ 401×202).
            Vector2 size = SizeFor(request.Level);
            window.position = new Rect(0f, 0f, size.x, size.y);
            window.Focus();
            return window;
        }

        internal static Vector2 SizeFor(LiveOpsConfirmLevel level)
        {
            return new Vector2(Width, level == LiveOpsConfirmLevel.TypeToConfirm ? TypeToConfirmHeight : Level1Height);
        }

        private static LiveOpsConfirmWindow Create(LiveOpsConfirmRequest request, ILiveOpsHubLayoutLoader layoutLoader)
        {
            if (request == null) throw new ArgumentNullException(nameof(request), LiveOpsHubStrings.FeedbackErrorConfirmRequestMissing);
            LiveOpsConfirmWindow window = CreateInstance<LiveOpsConfirmWindow>();
            window.titleContent = new GUIContent(LiveOpsHubStrings.ShellWindowTitle);
            Vector2 size = SizeFor(request.Level);
            window.minSize = size;
            window.maxSize = size;
            window.CenterOnMainWindow(size);
            window._content = layoutLoader == null ? new LiveOpsConfirmContent(request) : new LiveOpsConfirmContent(request, layoutLoader);
            window._content.Completed += window.OnCompleted;
            window.rootVisualElement.Add(window._content);
            // Focus chỉ giữ khi element đã có panel + layout: chờ GeometryChangedEvent đầu tiên rồi mới focus nút an toàn / ô gõ.
            window._content.RegisterCallback<GeometryChangedEvent>(window.FocusInitialOnce);
            return window;
        }

        private void CenterOnMainWindow(Vector2 size)
        {
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            if (main.width <= 0f || main.height <= 0f) return;
            position = new Rect(main.x + (main.width - size.x) * 0.5f, main.y + (main.height - size.y) * 0.5f, size.x, size.y);
        }

        private void FocusInitialOnce(GeometryChangedEvent geometryEvent)
        {
            _content.UnregisterCallback<GeometryChangedEvent>(FocusInitialOnce);
            _content.FocusInitial();
        }

        private void OnCompleted(LiveOpsConfirmResult result)
        {
            Result = result;
            // Đóng ngay (không delayCall): vòng modal của ShowModalUtility không chắc chạy delayCall, và hộp còn mở sau khi đã chọn
            // là người dùng bấm được lần nữa. Nút Close() trong callback click là cách cửa sổ UI Toolkit của Unity vẫn làm.
            Close();
        }

        private void OnDisable()
        {
            if (_content != null) _content.Completed -= OnCompleted;
        }
    }
}
