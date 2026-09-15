using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Lớp gốc popover (SP-12 kiểm chứng hai bản): mở <c>UnityEditor.PopupWindow</c> thật, focus ô đầu sau <c>GeometryChangedEvent</c>, gửi
    /// Escape bằng <c>SendEvent</c> — PopupWindow không tự đóng, handler <c>KeyDownEvent</c> TrickleDown của nội dung đóng đồng bộ. Mở popover thứ
    /// hai tự đóng cái đầu (không dựa "mất focus tự đóng" vì 2022.3 và 6000.6 khác nhau).
    /// </summary>
    [TestFixture]
    public sealed class PopoverTests
    {
        private const int MaximumFrames = 60;

        /// <summary>Popover thử: một ô nhập nhận focus đầu.</summary>
        private sealed class ProbePopover : LiveOpsPopoverContent
        {
            internal const string FieldName = "probe-popover-field";

            private TextField _field;

            protected override Vector2 PopoverSize => new Vector2(320f, 120f);

            protected override VisualElement BuildContent()
            {
                _field = new TextField { name = FieldName };
                return _field;
            }

            protected override Focusable InitialFocus => _field;

            internal TextField Field => _field;
        }

        [TearDown]
        public void TearDown()
        {
            LiveOpsPopoverContent.CloseCurrent();
        }

        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator Popover_EscClosesAfterFocus()
        {
            ProbePopover popover = new ProbePopover();
            LiveOpsPopoverContent.ShowSingle(new Rect(40f, 40f, 10f, 10f), popover);
            Assert.IsNotNull(popover.editorWindow, "Show gọi OnOpen đồng bộ và gắn cửa sổ PopupWindow");
            Assert.AreSame(popover, LiveOpsPopoverContent.Current);

            int frames = 0;
            while (!IsFocusedInside(popover))
            {
                if (++frames > MaximumFrames) Assert.Fail("ô đầu của popover không nhận focus sau " + MaximumFrames + " khung (GeometryChangedEvent)");
                yield return null;
            }

            VisualElement root = popover.editorWindow.rootVisualElement.Q(LiveOpsPopoverContent.RootElementName);
            Assert.IsNotNull(root);
            Assert.IsTrue(root.ClassListContains(LiveOpsHubClassNames.Root), "cửa sổ popover là panel riêng — gốc phải mang token hub");
            Assert.IsTrue(root.ClassListContains(LiveOpsHubClassNames.Popover));

            popover.editorWindow.SendEvent(Event.KeyboardEvent("escape"));

            Assert.AreEqual(1, popover.CloseCount, "Escape đóng popover ngay trong lượt gửi phím (handler TrickleDown + Close)");
            Assert.IsTrue(popover.EscapeHandled);
            Assert.IsNull(LiveOpsPopoverContent.Current);
            yield return null;
            Assert.IsTrue(popover.editorWindow == null, "cửa sổ PopupWindow đã huỷ");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator Popover_OpeningSecondClosesFirst()
        {
            // Chỗ bấm khác test Esc: PopupWindow bỏ qua lệnh mở lại ngay tại đúng chỗ vừa đóng (chống bấm-lại-để-đóng bật lại popup).
            ProbePopover first = new ProbePopover();
            LiveOpsPopoverContent.ShowSingle(new Rect(120f, 160f, 10f, 10f), first);
            Assert.IsTrue(first.editorWindow != null, "popover đầu phải mở thật — không thì phép đóng bên dưới không kiểm gì");
            yield return null;
            Assert.AreEqual(0, first.CloseCount);

            ProbePopover second = new ProbePopover();
            LiveOpsPopoverContent.ShowSingle(new Rect(420f, 160f, 10f, 10f), second);

            Assert.AreEqual(1, first.CloseCount, "mở popover mới đóng popover cũ trước — không bao giờ hai popover");
            Assert.AreEqual(0, second.CloseCount);
            Assert.AreSame(second, LiveOpsPopoverContent.Current);
            yield return null;
            Assert.IsTrue(first.editorWindow == null);
            Assert.IsTrue(second.editorWindow != null);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Popover_BuildForTest_ThemedRoot_EscapeHandledWithoutWindow()
        {
            ProbePopover popover = new ProbePopover();
            VisualElement built = popover.BuildForTest();
            Assert.AreEqual(LiveOpsPopoverContent.RootElementName, built.name);
            Assert.IsTrue(built.ClassListContains(LiveOpsHubClassNames.Root));
            Assert.AreSame(popover.Field, built.Q<TextField>(ProbePopover.FieldName));
            Assert.AreSame(built, popover.BuildForTest(), "dựng một lần");
            Assert.AreEqual(new Vector2(320f, 120f), popover.GetWindowSize());

            using (KeyDownEvent other = KeyDownEvent.GetPooled('a', KeyCode.A, EventModifiers.None))
            {
                popover.HandleKeyDownForTest(other);
            }
            Assert.IsFalse(popover.EscapeHandled, "phím khác không đóng");
            using (KeyDownEvent escape = KeyDownEvent.GetPooled('\0', KeyCode.Escape, EventModifiers.None))
            {
                popover.HandleKeyDownForTest(escape);
            }
            Assert.IsTrue(popover.EscapeHandled);
        }

        private static bool IsFocusedInside(ProbePopover popover)
        {
            if (popover.editorWindow == null || popover.Field == null || popover.Field.panel == null) return false;
            Focusable focused = popover.Field.panel.focusController.focusedElement;
            for (VisualElement current = focused as VisualElement; current != null; current = current.hierarchy.parent)
            {
                if (current == popover.Field) return true;
            }
            return false;
        }
    }
}
