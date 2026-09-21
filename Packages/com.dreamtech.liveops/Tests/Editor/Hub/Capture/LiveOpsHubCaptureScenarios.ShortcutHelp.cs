using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng ShortcutHelp (G-OPT-SHORTCUTHELP, W6) — một ảnh: <c>ho-shortcut-help</c>, popover "Hiện hướng dẫn
    /// phím tắt" của menu ⋮ (ma trận 9.5, khuôn 320×N).
    /// <para>
    /// Popover là cửa sổ HĐH riêng nên nó không nằm trong ảnh của cửa sổ hub: gắn CHÍNH cây của nó vào root hub trong một
    /// khuôn đúng bằng <c>GetWindowSize()</c> rồi CẮT theo gốc popover — cùng cách ảnh <c>h09f</c> của luồng dán đã làm, và
    /// khuôn là thứ chặn danh sách phím nở quá cửa sổ thật.
    /// </para>
    /// <para>
    /// Nút "Mở cửa sổ Shortcuts…" nhận đường mở GIẢ: ảnh không được kéo theo một cửa sổ Shortcuts thật của Unity vào khung
    /// hình (và lượt chụp chạy batch, không có ai đóng nó lại).
    /// </para>
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        /// <summary>Bề rộng cửa sổ popover của hub ([FD §7]) — đo trên GỐC popover, chỗ duy nhất mang đúng 320px.</summary>
        private const float ShortcutHelpPopoverWidth = 320f;

        /// <summary>Chiều cao cửa sổ popover phím tắt (<c>ShortcutHelpPopover.WindowSize</c>) — ảnh phải đo lại được số này.</summary>
        private const float ShortcutHelpPopoverHeight = 420f;

        /// <summary>Cửa sổ chỉ đủ ôm popover: ảnh cắt theo gốc popover nên khung sườn hub không được chiếm chỗ trong ảnh.</summary>
        private const int ShortcutHelpWindowWidth = 360;
        private const int ShortcutHelpWindowHeight = 470;

        /// <summary>Lề của khuôn trong cửa sổ — đủ để viền popover không dính mép và phép dò cạnh còn cạnh để dò.</summary>
        private const float ShortcutHelpPopoverMargin = 20f;

        private const string ShortcutHelpPopoverHostName = "shortcut-help-popover-host";

        static partial void RegisterShortcutHelp(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.HoShortcutHelp,
                    ShortcutHelpWindowWidth, ShortcutHelpWindowHeight, OpenShortcutHelpPopover,
                    window => window.rootVisualElement.Q(LiveOpsPopoverContent.RootElementName))
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsPopoverContent.RootElementName,
                    ShortcutHelpPopoverWidth, ShortcutHelpPopoverHeight)));
        }

        /// <summary>
        /// Tiêu đề màn lấy từ chính registry (<c>LiveOpsHubSections.Create</c>) chứ không chép tay: ảnh phải cho thấy sáu hàng
        /// ⌘1…⌘6 nói đúng tên màn mà bản này đang có.
        /// </summary>
        private static EditorWindow OpenShortcutHelpPopover()
        {
            List<IHubSection> sections = LiveOpsHubSections.Create();
            List<string> sectionTitles = new List<string>(sections.Count);
            foreach (IHubSection section in sections) sectionTitles.Add(section.Title);

            EditorWindow window = LiveOpsHubWindow.OpenForTest(sections, new ManualLiveOpsHubCompilationState(false), null, sections[0].Id);
            ShortcutHelpPopover popover = new ShortcutHelpPopover(sectionTitles, null, () => true);
            window.rootVisualElement.Add(ShortcutHelpPopoverHost(popover));
            return window;
        }

        /// <summary>
        /// Khuôn đúng bằng cửa sổ popover thật, lấy từ chính <c>GetWindowSize()</c>. Bắt buộc phải chặn: gắn cây popover vào
        /// root hub mà không chặn chiều cao thì danh sách phím nở theo nội dung và ghi chú + hàng nút rơi khỏi ảnh — tức ảnh vẽ
        /// một popover KHÔNG TỒN TẠI (bài học của ảnh h09f ở W5).
        /// </summary>
        private static VisualElement ShortcutHelpPopoverHost(ShortcutHelpPopover popover)
        {
            Vector2 popoverSize = popover.GetWindowSize();
            VisualElement host = new VisualElement { name = ShortcutHelpPopoverHostName };
            host.style.position = Position.Absolute;
            host.style.left = ShortcutHelpPopoverMargin;
            host.style.top = ShortcutHelpPopoverMargin;
            host.style.width = popoverSize.x;
            host.style.height = popoverSize.y;
            host.style.overflow = Overflow.Hidden;
            host.Add(popover.BuildForTest());
            return host;
        }
    }
}
