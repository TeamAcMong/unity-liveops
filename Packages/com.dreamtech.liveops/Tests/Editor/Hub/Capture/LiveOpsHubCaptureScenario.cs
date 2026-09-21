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

        /// <summary>Việc dọn sau khi chụp xong (xoá asset tạm…); null = không có gì để dọn.</summary>
        public Action Cleanup { get; private set; }

        /// <summary>
        /// Việc chạy MỘT LẦN sau khi cửa sổ đã có layout và trước khi chụp; null = không có.
        /// <para>
        /// Cần cho trạng thái chỉ dựng được KHI ĐÃ BIẾT kích thước thật: cuộn tới một card nằm dưới lằn cuộn là ví dụ —
        /// <c>ScrollView.ScrollTo</c> gọi trong <see cref="OpenWindow"/> không làm gì cả vì lúc đó viewport còn cao 0. Lệnh
        /// chụp chờ thêm <c>AfterLayoutSettleFrames</c> khung sau khi chạy nó để lượt dựng lại do nó gây ra kịp xong.
        /// </para>
        /// </summary>
        public Action<EditorWindow> AfterLayout { get; private set; }

        public LiveOpsHubCaptureScenario WithAfterLayout(Action<EditorWindow> afterLayout)
        {
            AfterLayout = afterLayout;
            return this;
        }

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

        /// <summary>
        /// Việc dọn chạy SAU khi ảnh đã chụp và cửa sổ đã đóng — chỗ duy nhất kịch bản xoá được thứ nó tạo ra trên đĩa. Lượt chụp
        /// không có <c>TearDown</c> của NUnit, nên trước đây kịch bản phải xoá NGAY lúc dựng trạng thái, và mọi thứ phụ thuộc vào
        /// file đó (xung đột đĩa) có thể chết trong lúc chờ layout mà không ai thấy.
        /// </summary>
        public LiveOpsHubCaptureScenario WithCleanup(Action cleanup)
        {
            Cleanup = cleanup;
            return this;
        }
    }

    /// <summary>Một dòng <c>expectedFrames</c> của JSON số đo: element (tên hoặc class) + bề rộng/chiều cao thiết kế (0 = không đo chiều đó).</summary>
    internal sealed class LiveOpsHubCaptureExpectedFrame
    {
        public LiveOpsHubCaptureExpectedFrame(string element, float width, float height)
            : this(element, width, height, 0f)
        {
        }

        public LiveOpsHubCaptureExpectedFrame(string element, float width, float height, float tolerance)
        {
            Element = element ?? throw new ArgumentNullException(nameof(element));
            Width = width;
            Height = height;
            Tolerance = tolerance;
        }

        public string Element { get; }
        public float Width { get; }
        public float Height { get; }

        /// <summary>
        /// Sai số RIÊNG của khung này (px); 0 = dùng sai số chung <c>--tolerance</c> của <c>measure-capture.py</c>.
        /// <para>
        /// Chỉ khai khi con số đo được là số của FONT chứ không phải số thiết kế — chiều cao một dòng chữ là ví dụ duy nhất
        /// hiện có. Khai sai số riêng cho một khung như vậy vẫn giữ được câu khẳng định thật ("đúng MỘT dòng, và nó đang
        /// hiện") trong khi không phải nới sai số chung cho cả bộ ảnh, thứ sẽ làm mọi khung thiết kế khác dễ dãi theo.
        /// </para>
        /// </summary>
        public float Tolerance { get; }

        /// <summary>Ngưỡng bề rộng CỬA SỔ mà dưới đó khung có số thiết kế khác; 0 = khung không đổi theo cỡ cửa sổ.</summary>
        public float NarrowWindowWidth { get; private set; }

        /// <summary>Chiều cao thiết kế khi cửa sổ hẹp hơn <see cref="NarrowWindowWidth"/>.</summary>
        public float NarrowHeight { get; private set; }

        /// <summary>
        /// Khai luật "dưới bề rộng cửa sổ này thì chiều cao thiết kế là số khác" (W9-01). Đây là HAI số thiết kế, không
        /// phải một số với sai số rộng ra: cả hai đều phải đúng, chỉ khác nhau ở bên nào của ngưỡng. Header màn là chỗ
        /// đầu tiên cần nó — dưới 1100px phụ đề xuống dòng và nhóm nút xuống dòng riêng nên header cao 61 thay vì 36.
        /// </summary>
        public LiveOpsHubCaptureExpectedFrame WithHeightBelowWindowWidth(float windowWidth, float height)
        {
            if (windowWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(windowWidth));
            if (height <= 0f) throw new ArgumentOutOfRangeException(nameof(height));
            return new LiveOpsHubCaptureExpectedFrame(Element, Width, Height, Tolerance)
            {
                NarrowWindowWidth = windowWidth,
                NarrowHeight = height,
            };
        }
    }
}
