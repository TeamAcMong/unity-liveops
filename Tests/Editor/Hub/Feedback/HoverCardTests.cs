using NUnit.Framework;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Hover card ([FD §2.13]): hiện sau 500ms đứng yên trên đích, rời trước hạn thì huỷ, ẩn sau 100ms rời đích (trừ khi chuột sang thẻ),
    /// F8 ghim không tắt khi rời. Đồng hồ tay + gọi thẳng Tick — test không chờ thời gian thật và không cần panel.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class HoverCardTests
    {
        private double _nowSeconds;
        private VisualElement _root;
        private VisualElement _target;
        private LiveOpsHoverCardHost _host;
        private int _buildCount;

        [SetUp]
        public void SetUp()
        {
            _nowSeconds = 10.0;
            _root = new VisualElement();
            _target = new VisualElement { name = "bar-hunt-0914" };
            _root.Add(_target);
            _host = new LiveOpsHoverCardHost(_root, () => _nowSeconds);
            _buildCount = 0;
            _host.Attach(_target, () =>
            {
                _buildCount++;
                return new Label("hunt-0914 · 14/9 00:00 → 17/9 00:00 UTC");
            });
        }

        private void Advance(int milliseconds)
        {
            _nowSeconds += milliseconds / 1000.0;
            _host.Tick();
        }

        [Test]
        public void HoverCard_ShowsAfter500ms()
        {
            Assert.AreSame(_root, _host.Card.parent, "thẻ là con của root (lớp nổi trên cùng)");
            Assert.IsFalse(_host.IsVisible);

            _host.HandlePointerEnter(_target);
            Advance(LiveOpsHoverCardHost.ShowDelayMilliseconds - 1);
            Assert.IsFalse(_host.IsVisible, "chưa đủ 500ms thì chưa hiện");
            Assert.AreEqual(0, _buildCount, "nội dung chỉ dựng lúc hiện — dữ liệu có thể đổi trong lúc chờ");

            Advance(1);
            Assert.IsTrue(_host.IsVisible, "đủ 500ms thì hiện");
            Assert.AreSame(_target, _host.CurrentTarget);
            Assert.AreEqual(1, _buildCount);
            Assert.AreEqual(1, _host.Card.childCount);
            Assert.IsTrue(_host.Card.ClassListContains(LiveOpsHubClassNames.HoverCardVisible));
            Assert.AreEqual(PickingMode.Position, _host.Card.pickingMode, "thẻ hiện nhận chuột để đi chuột vào thẻ không làm thẻ tắt");
        }

        [Test]
        public void HoverCard_LeaveBeforeDelay_CancelsShow()
        {
            _host.HandlePointerEnter(_target);
            Advance(300);
            _host.HandlePointerLeave(_target);
            Advance(LiveOpsHoverCardHost.ShowDelayMilliseconds);
            Assert.IsFalse(_host.IsVisible, "rời chuột trước 500ms huỷ việc hiện");
            Assert.AreEqual(0, _buildCount);
        }

        [Test]
        public void HoverCard_HidesAfter100ms_UnlessPointerMovesOntoCard()
        {
            _host.HandlePointerEnter(_target);
            Advance(LiveOpsHoverCardHost.ShowDelayMilliseconds);
            Assert.IsTrue(_host.IsVisible);

            _host.HandlePointerLeave(_target);
            Advance(LiveOpsHoverCardHost.HideDelayMilliseconds - 1);
            Assert.IsTrue(_host.IsVisible, "chưa đủ 100ms sau khi rời thì còn");
            // Chuột quay lại thanh trong 100ms (vd lướt qua mép): không tắt. Đi sang chính thẻ dùng cùng đường huỷ ẩn (PointerEnter của thẻ).
            _host.HandlePointerEnter(_target);
            Advance(LiveOpsHoverCardHost.HideDelayMilliseconds * 3);
            Assert.IsTrue(_host.IsVisible, "quay lại đích trong 100ms giữ thẻ");

            _host.HandlePointerLeave(_target);
            Advance(LiveOpsHoverCardHost.HideDelayMilliseconds);
            Assert.IsFalse(_host.IsVisible, "đủ 100ms sau khi rời thì ẩn");
            Assert.AreEqual(0, _host.Card.childCount, "ẩn thì bỏ nội dung cũ");
            Assert.AreEqual(PickingMode.Ignore, _host.Card.pickingMode, "thẻ ẩn không chặn chuột của thanh bên dưới");
        }

        [Test]
        public void HoverCard_ShowPinned_IgnoresLeaveUntilHide()
        {
            _host.ShowPinned(_target, new Label("F8"));
            Assert.IsTrue(_host.IsVisible, "F8 hiện ngay, không chờ 500ms");
            Assert.IsTrue(_host.IsPinned);
            Assert.IsTrue(_host.Card.ClassListContains(LiveOpsHubClassNames.HoverCardPinned));

            _host.HandlePointerLeave(_target);
            Advance(1000);
            Assert.IsTrue(_host.IsVisible, "thẻ ghim không tắt khi rời chuột");

            _host.Hide();
            Assert.IsFalse(_host.IsVisible);
            Assert.IsFalse(_host.IsPinned);
            Assert.IsFalse(_host.Card.ClassListContains(LiveOpsHubClassNames.HoverCardPinned));
        }
    }
}
