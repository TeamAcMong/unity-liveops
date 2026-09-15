using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Lớp gốc mọi popover của hub (Thêm đợt, Đề xuất…, Bỏ qua cảnh báo, Dán JSON) — [FD §7], [API §5.2, §12.6], SP-12 đã kiểm chứng:
    /// <list type="bullet">
    /// <item>Cửa sổ <c>UnityEditor.PopupWindow</c> riêng nên không bị cắt ở mép hub; kích thước 320×N.</item>
    /// <item>Dựng UI trong <c>OnOpen</c> (không override <c>CreateGUI</c> — internal ở 2022.3); lúc đó <c>panel == null</c>.</item>
    /// <item><c>PopupWindow</c> KHÔNG tự đóng khi nhận Escape (kể cả <c>SendEvent(Escape)</c>): nội dung tự đăng ký <c>KeyDownEvent</c>
    /// TrickleDown trên root rồi <c>editorWindow.Close()</c> — đóng đồng bộ trong batch ở hai bản.</item>
    /// <item>Esc chỉ tới root khi đã có element focus: focus <see cref="InitialFocus"/> ở <c>GeometryChangedEvent</c> đầu tiên (đã có panel).</item>
    /// <item>Không dựa vào "mất focus tự đóng" (6000.6 đóng, 2022.3 không): <see cref="ShowSingle"/> tự đóng popover cũ trước.</item>
    /// </list>
    /// Lớp trừu tượng có chủ đích (họ popover đóng): mỗi popover con là <c>sealed</c>.
    /// </summary>
    internal abstract class LiveOpsPopoverContent : PopupWindowContent
    {
        internal const string RootElementName = "hub-popover-root";

        private static LiveOpsPopoverContent _current;

        private VisualElement _built;
        private bool _isInitialFocusDone;

        /// <summary>Kích thước cửa sổ popover — rộng 320 ([FD §7]), cao theo nội dung.</summary>
        protected abstract Vector2 PopoverSize { get; }

        /// <summary>Dựng nội dung; gọi một lần trong <c>OnOpen</c> (hoặc <see cref="BuildForTest"/>).</summary>
        protected abstract VisualElement BuildContent();

        /// <summary>Element nhận focus sau khi popover có layout — không có focus thì Esc không tới handler.</summary>
        protected abstract Focusable InitialFocus { get; }

        /// <summary>Popover đang mở do <see cref="ShowSingle"/>; null khi không có.</summary>
        internal static LiveOpsPopoverContent Current => _current;

        /// <summary>Số lần popover này đã đóng (OnClose) — test chứng minh Esc đóng thật chứ không chỉ ẩn.</summary>
        internal int CloseCount { get; private set; }

        internal bool IsOpen => editorWindow != null && _current == this && CloseCount == 0;

        /// <summary>Mở popover dưới <paramref name="activatorWorldBound"/>; popover cũ (nếu còn) đóng trước — không bao giờ hai popover.</summary>
        public static void ShowSingle(Rect activatorWorldBound, LiveOpsPopoverContent content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            CloseCurrent();
            _current = content;
            // Viết đầy đủ UnityEditor.PopupWindow: UnityEngine.UIElements.PopupWindow trùng tên (CS0104) [API §5.2].
            UnityEditor.PopupWindow.Show(activatorWorldBound, content);
        }

        /// <summary>Đóng popover đang mở (điều hướng sang màn khác, mở popover khác).</summary>
        internal static void CloseCurrent()
        {
            LiveOpsPopoverContent previous = _current;
            _current = null;
            if (previous != null && previous.editorWindow != null) previous.editorWindow.Close();
        }

        public sealed override Vector2 GetWindowSize()
        {
            Vector2 size = PopoverSize;
            if (size.x <= 0f || size.y <= 0f) throw new InvalidOperationException(LiveOpsHubStrings.FeedbackErrorPopoverSizeInvalid);
            return size;
        }

        public sealed override void OnGUI(Rect rect)
        {
            // UI Toolkit vẽ toàn bộ nội dung; OnGUI rỗng có chủ đích (PopupWindowContent bắt buộc có hàm này).
        }

        public sealed override void OnOpen()
        {
            VisualElement root = editorWindow.rootVisualElement;
            root.Add(BuildOnce());
            root.RegisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);
            root.RegisterCallback<GeometryChangedEvent>(FocusInitialOnce);
        }

        public sealed override void OnClose()
        {
            CloseCount++;
            if (_current == this) _current = null;
            OnClosed();
        }

        /// <summary>Popover con dọn trạng thái riêng khi đóng (huỷ xem trước…). Mặc định không làm gì.</summary>
        protected virtual void OnClosed()
        {
        }

        /// <summary>Đóng popover từ nút trong nội dung (Huỷ, Áp xong).</summary>
        protected void ClosePopover()
        {
            if (editorWindow != null) editorWindow.Close();
        }

        /// <summary>Dựng nội dung (gốc có token + sheet hub) mà không mở cửa sổ — test đọc cây và gửi phím qua <see cref="HandleKeyDownForTest"/>.</summary>
        internal VisualElement BuildForTest()
        {
            return BuildOnce();
        }

        internal void HandleKeyDownForTest(KeyDownEvent keyDown)
        {
            if (keyDown == null) throw new ArgumentNullException(nameof(keyDown));
            OnRootKeyDown(keyDown);
        }

        /// <summary>Test: Escape đã được xử lý (cả khi chưa có cửa sổ để đóng).</summary>
        internal bool EscapeHandled { get; private set; }

        private VisualElement BuildOnce()
        {
            if (_built != null) return _built;
            VisualElement content = BuildContent();
            if (content == null) throw new InvalidOperationException(LiveOpsHubStrings.FeedbackErrorPopoverWithoutContent);
            // Cửa sổ popover là panel riêng: token khai trên .liveops-hub-root và sheet gắn ở root của cửa sổ hub không tới đây.
            VisualElement root = LiveOpsFeedbackStyleSheets.CreateThemedRoot(RootElementName, LiveOpsFeedbackStyleSheets.PopoverSheets);
            root.AddToClassList(LiveOpsHubClassNames.Popover);
            root.Add(content);
            _built = root;
            return root;
        }

        private void FocusInitialOnce(GeometryChangedEvent geometryEvent)
        {
            if (_isInitialFocusDone) return;
            _isInitialFocusDone = true;
            editorWindow?.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(FocusInitialOnce);
            InitialFocus?.Focus();
        }

        private void OnRootKeyDown(KeyDownEvent keyDown)
        {
            if (keyDown.keyCode != KeyCode.Escape) return;
            keyDown.StopPropagation();
            EscapeHandled = true;
            ClosePopover();
        }
    }

    /// <summary>
    /// Gốc có token cho cửa sổ riêng của hub (popover, hộp xác nhận): mỗi cửa sổ là một panel riêng nên class
    /// <c>liveops-hub-root</c> + sheet theme/components phải gắn lại ở đây — không có thì màu trạng thái và khoảng cách rơi về mặc
    /// định của Unity. Sheet thiếu chỉ cảnh báo một lần mỗi đường dẫn: hộp phá huỷ vẫn phải mở được dù package thiếu file.
    /// </summary>
    internal static class LiveOpsFeedbackStyleSheets
    {
        internal static readonly string[] PopoverSheets =
        {
            LiveOpsHubPaths.ThemeUss, LiveOpsHubPaths.ComponentsUss, LiveOpsHubPaths.FeedbackUss, LiveOpsHubPaths.MotionUss,
        };

        internal static readonly string[] ConfirmSheets =
        {
            LiveOpsHubPaths.ThemeUss, LiveOpsHubPaths.ComponentsUss, LiveOpsHubPaths.ConfirmWindowUss,
        };

        private static readonly System.Collections.Generic.HashSet<string> WarnedPaths =
            new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        internal static VisualElement CreateThemedRoot(string elementName, string[] sheetPaths)
        {
            return CreateThemedRoot(elementName, sheetPaths, new AssetDatabaseLiveOpsHubLayoutLoader());
        }

        internal static VisualElement CreateThemedRoot(string elementName, string[] sheetPaths, ILiveOpsHubLayoutLoader loader)
        {
            VisualElement root = new VisualElement { name = elementName };
            root.AddToClassList(LiveOpsHubClassNames.Root);
            AddStyleSheets(root, sheetPaths, loader);
            // Bật class skin theo isProSkin + probe màu nền (đổi Theme khi hộp đang mở vẫn đúng skin) — cùng một luật với cửa sổ hub.
            LiveOpsHubSkin.Attach(root);
            root.EnableInClassList(LiveOpsHubClassNames.NoMotion, EditorPrefs.GetBool(LiveOpsHubWindow.ReduceMotionPreferenceKey, false));
            return root;
        }

        internal static void AddStyleSheets(VisualElement root, string[] sheetPaths, ILiveOpsHubLayoutLoader loader)
        {
            foreach (string path in sheetPaths)
            {
                StyleSheet sheet = loader.LoadStyleSheet(path);
                if (sheet != null)
                {
                    root.styleSheets.Add(sheet);
                    continue;
                }
                if (WarnedPaths.Add(path))
                {
                    Debug.LogWarning(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        LiveOpsHubStrings.FeedbackWarningStyleSheetMissingFormat, path));
                }
            }
        }
    }
}
