using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Một kịch bản chụp của ma trận 9.5: id (hằng ở <see cref="LiveOpsHubCaptureScenarioIds"/>), kích thước cửa sổ, cách mở cửa sổ và
    /// (tuỳ chọn) element cần cắt ảnh. Kịch bản chỉ DỰNG trạng thái — lệnh chụp lo đặt kích thước sau Show, chờ layout, chụp, cắt,
    /// ghi PNG + JSON số đo và đóng cửa sổ, để mọi gói đăng ký kịch bản cùng một đường chụp.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal sealed class LiveOpsHubCaptureScenario
    {
        /// <param name="id">Id kịch bản — tên file ảnh <c>&lt;id&gt;-&lt;skin&gt;.png</c>.</param>
        /// <param name="windowWidth">Bề rộng cửa sổ (point) đặt SAU Show (SP-16).</param>
        /// <param name="windowHeight">Chiều cao cửa sổ (point).</param>
        /// <param name="openWindow">Mở cửa sổ ở trạng thái của hình (CreateInstance + Show, không GetWindow).</param>
        /// <param name="captureTarget">Element cắt ảnh; null = <c>rootVisualElement</c> của cửa sổ.</param>
        public LiveOpsHubCaptureScenario(string id, int windowWidth, int windowHeight, Func<EditorWindow> openWindow,
            Func<EditorWindow, VisualElement> captureTarget = null)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (windowWidth <= 0) throw new ArgumentOutOfRangeException(nameof(windowWidth));
            if (windowHeight <= 0) throw new ArgumentOutOfRangeException(nameof(windowHeight));
            Id = id;
            WindowWidth = windowWidth;
            WindowHeight = windowHeight;
            OpenWindow = openWindow ?? throw new ArgumentNullException(nameof(openWindow));
            CaptureTarget = captureTarget;
            ExpectedFrames = Array.Empty<LiveOpsHubCaptureExpectedFrame>();
        }

        public string Id { get; }
        public int WindowWidth { get; }
        public int WindowHeight { get; }
        public Func<EditorWindow> OpenWindow { get; }
        public Func<EditorWindow, VisualElement> CaptureTarget { get; }

        /// <summary>Số khung mong đợi riêng của kịch bản (ghi đè bảng mặc định của measure-capture.py); rỗng = bảng mặc định.</summary>
        public IReadOnlyList<LiveOpsHubCaptureExpectedFrame> ExpectedFrames { get; private set; }

        /// <summary>Số khung layout tối thiểu phải chờ trước khi chụp (spinner, transition vào); lệnh chụp luôn chờ layout có trước.</summary>
        public int MinimumSettleFrames { get; private set; } = 5;

        /// <summary>
        /// Ngôn ngữ chụp. Mặc định TIẾNG VIỆT vì ma trận 9.5 so ảnh với hình thiết kế (chữ tiếng Việt): hub đổi ngôn ngữ mặc
        /// định sang English không được làm lệch một ảnh nào đã đạt số đo. Kịch bản nào cố tình chụp bản dịch thì tự khai.
        /// </summary>
        public LiveOpsHubLanguageId Language { get; private set; } = LiveOpsHubLanguageId.Vietnamese;

        public LiveOpsHubCaptureScenario WithExpectedFrames(params LiveOpsHubCaptureExpectedFrame[] frames)
        {
            ExpectedFrames = frames ?? Array.Empty<LiveOpsHubCaptureExpectedFrame>();
            return this;
        }

        public LiveOpsHubCaptureScenario WithMinimumSettleFrames(int frames)
        {
            MinimumSettleFrames = Math.Max(0, frames);
            return this;
        }

        public LiveOpsHubCaptureScenario WithLanguage(LiveOpsHubLanguageId language)
        {
            Language = language;
            return this;
        }
    }

    /// <summary>Một dòng <c>expectedFrames</c> của JSON số đo: element (tên hoặc class) + bề rộng/chiều cao thiết kế (0 = không đo chiều đó).</summary>
    internal sealed class LiveOpsHubCaptureExpectedFrame
    {
        public LiveOpsHubCaptureExpectedFrame(string element, float width, float height)
        {
            Element = element ?? throw new ArgumentNullException(nameof(element));
            Width = width;
            Height = height;
        }

        public string Element { get; }
        public float Width { get; }
        public float Height { get; }
    }
}
