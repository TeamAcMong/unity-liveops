using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Số đo khung trên cửa sổ thật 1280×760 ([FD §3.1]): rail 196 · header 26 · section header 36 · status 20 · cột nội dung 1084 ·
    /// thân 678, và class breakpoint cộng dồn theo bề rộng ([FD §4.2]). Cùng số với measure-capture.py đo trên ảnh chụp — test đọc
    /// <c>worldBound</c> sau layout, ảnh chụp đo cạnh trên PNG gốc; hai đường phải cùng đạt.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class HubLayoutTests
    {
        private const float Tolerance = 0.5f;

        private LiveOpsHubWindowTestScope _scope;

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
            _scope = null;
        }

        [UnityTest]
        public IEnumerator Shell_1280x760_Measurements()
        {
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();
            VisualElement root = _scope.Window.rootVisualElement;

            Assert.AreEqual(1280f, root.layout.width, Tolerance, "cửa sổ phải giữ 1280 sau Show (SP-16) — mọi số đo dưới dựa trên kích thước này");
            Assert.AreEqual(760f, root.layout.height, Tolerance);

            Rect rail = BoundOf(root, LiveOpsHubPaths.ShellElementNames.Rail);
            Rect header = BoundOf(root, LiveOpsHubPaths.ShellElementNames.Header);
            Rect sectionHeader = BoundOf(root, LiveOpsHubPaths.ShellElementNames.SectionHeader);
            Rect status = BoundOf(root, LiveOpsHubPaths.ShellElementNames.StatusBar);
            Rect content = BoundOf(root, LiveOpsHubPaths.ShellElementNames.Content);
            Rect body = BoundOf(root, LiveOpsHubPaths.ShellElementNames.SectionBody);

            Assert.AreEqual(196f, rail.width, Tolerance, "rail 196 px");
            Assert.AreEqual(26f, header.height, Tolerance, "header 26 px");
            Assert.AreEqual(1280f, header.width, Tolerance, "header trải cả rail lẫn cột nội dung");
            Assert.AreEqual(36f, sectionHeader.height, Tolerance, "section header 36 px");
            Assert.AreEqual(20f, status.height, Tolerance, "status bar 20 px");
            Assert.AreEqual(1084f, content.width, Tolerance, "cột nội dung = 1280 − 196");
            Assert.AreEqual(678f, body.height, Tolerance, "thân = 760 − 26 − 36 − 20");
            Assert.AreEqual(760f - 26f - 20f, rail.height, Tolerance, "rail chạy từ dưới header tới trên status bar");
            Assert.AreEqual(header.yMax, rail.y, Tolerance);
            Assert.AreEqual(rail.xMax, content.x, Tolerance, "cột nội dung bắt đầu ngay sau rail");
            Assert.AreEqual(root.worldBound.yMax, status.yMax, Tolerance, "status bar sát đáy cửa sổ");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Breakpoints_ClassesAtWidths()
        {
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();
            VisualElement hubRoot = _scope.Window.HubRoot;

            yield return ResizeAndWait(1280, 760);
            AssertBreakpoints(hubRoot, 1280, false, false, false);
            yield return ResizeAndWait(1050, 760);
            AssertBreakpoints(hubRoot, 1050, true, false, false);
            yield return ResizeAndWait(820, 560);
            AssertBreakpoints(hubRoot, 820, true, true, false);
            yield return ResizeAndWait(700, 520);
            AssertBreakpoints(hubRoot, 700, true, true, true);
            yield return ResizeAndWait(1280, 760);
            AssertBreakpoints(hubRoot, 1280, false, false, false);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Breakpoints_Apply_BoundariesAndUnmeasuredWidth()
        {
            VisualElement root = new VisualElement();
            LiveOpsHubBreakpoints.Apply(root, 1100f);
            AssertBreakpoints(root, 1100, false, false, false);
            LiveOpsHubBreakpoints.Apply(root, 1099f);
            AssertBreakpoints(root, 1099, true, false, false);
            LiveOpsHubBreakpoints.Apply(root, 899f);
            AssertBreakpoints(root, 899, true, true, false);
            LiveOpsHubBreakpoints.Apply(root, 719f);
            AssertBreakpoints(root, 719, true, true, true);
            LiveOpsHubBreakpoints.Apply(root, float.NaN);
            AssertBreakpoints(root, 719, true, true, true);
        }

        private IEnumerator ResizeAndWait(int width, int height)
        {
            _scope.Window.position = new Rect(0, 0, width, height);
            int frames = 0;
            while (Mathf.Abs(_scope.Window.rootVisualElement.layout.width - width) > Tolerance)
            {
                if (++frames > LiveOpsHubWindowTestScope.MaximumLayoutFrames) Assert.Fail("cửa sổ không đổi sang bề rộng " + width);
                yield return null;
            }
            // GeometryChangedEvent bắn sau lượt layout kế tiếp.
            yield return null;
            yield return null;
        }

        private static void AssertBreakpoints(VisualElement root, int width, bool medium, bool narrow, bool compact)
        {
            Assert.AreEqual(medium, root.ClassListContains(LiveOpsHubClassNames.Medium), width + " px: --medium (< 1100)");
            Assert.AreEqual(narrow, root.ClassListContains(LiveOpsHubClassNames.Narrow), width + " px: --narrow (< 900)");
            Assert.AreEqual(compact, root.ClassListContains(LiveOpsHubClassNames.Compact), width + " px: --compact (< 720)");
        }

        private static Rect BoundOf(VisualElement root, string elementName)
        {
            VisualElement element = root.Q(elementName);
            Assert.IsNotNull(element, "thiếu element " + elementName);
            return element.worldBound;
        }
    }
}
