using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>Thanh chu kỳ: ba con chia bằng flex-grow = số giờ; chạy lâu hơn chu kỳ thì con thứ 3 tô blocked-fill ([SD1 §4.1]).</summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class RuleCycleBarTests
    {
        private const float BarWidth = 242f;
        private const float BorderWidth = 1f;

        private ControlsTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            _panel?.Dispose();
            _panel = null;
        }

        [UnityTest]
        public IEnumerator CycleBar_ActiveLongerThanPeriod_ThirdChildBlocked()
        {
            _panel = ControlsTestPanel.Open();
            VisualElement root = _panel.CreateRoot(false);
            LiveOpsRuleCycleBar overflowing = CreateBar(root, 24, 30);
            LiveOpsRuleCycleBar skyRace = CreateBar(root, 24, 20);
            VisualElement blockedProbe = new VisualElement();
            blockedProbe.AddToClassList(LiveOpsHubClassNames.FillBlocked);
            blockedProbe.style.height = 4f;
            root.Add(blockedProbe);
            yield return ControlsTestPanel.WaitForLayout(overflowing, skyRace, blockedProbe);

            Assert.AreEqual(3, overflowing.childCount, "thanh luôn có đúng ba con: chạy · nghỉ · tràn");
            VisualElement third = overflowing[2];
            Assert.AreSame(overflowing.Overflow, third, "phần tràn phải là con thứ ba");
            Assert.IsTrue(third.ClassListContains(LiveOpsHubClassNames.FillBlocked), "phần tràn tô blocked-fill — đợt sau mở trước khi đợt trước khép");
            Assert.AreEqual(DisplayStyle.Flex, third.resolvedStyle.display);
            Assert.AreEqual(blockedProbe.resolvedStyle.backgroundColor, third.resolvedStyle.backgroundColor, "màu phần tràn phải là token blocked-fill của skin");
            Assert.AreEqual(DisplayStyle.None, overflowing.Rest.resolvedStyle.display, "chạy lâu hơn chu kỳ thì không có phần nghỉ");
            Assert.IsTrue(overflowing.IsOverflowing);

            float contentWidth = BarWidth - 2f * BorderWidth;
            Assert.AreEqual(contentWidth * 24f / 30f, overflowing.Run.layout.width, 1f, "phần chạy = chu kỳ / tổng giờ");
            Assert.AreEqual(contentWidth * 6f / 30f, third.layout.width, 1f, "phần tràn = (chạy − chu kỳ) / tổng giờ");
            Assert.AreEqual(14f, overflowing.layout.height, 0.5f, "thanh cao 14px");

            Assert.AreEqual(DisplayStyle.None, skyRace.Overflow.resolvedStyle.display, "20 giờ trong chu kỳ 24 giờ không có phần tràn");
            Assert.IsFalse(skyRace.IsOverflowing);
            Assert.AreEqual(contentWidth * 20f / 24f, skyRace.Run.layout.width, 1f, "chạy 20 giờ | nghỉ 4 giờ");
            Assert.AreEqual(contentWidth * 4f / 24f, skyRace.Rest.layout.width, 1f);

            overflowing.ActiveHours = 24;
            yield return null;
            yield return null;
            Assert.AreEqual(DisplayStyle.None, overflowing.Overflow.resolvedStyle.display, "hết tràn thì con thứ ba ẩn, không bị gỡ");
            Assert.AreEqual(3, overflowing.childCount);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CycleBar_ColorSlotAndInvalidPeriod()
        {
            LiveOpsRuleCycleBar bar = new LiveOpsRuleCycleBar { PeriodHours = 168, ActiveHours = 168 };
            bar.SetColorSlot(3);
            Assert.IsTrue(bar.Run.ClassListContains(LiveOpsHubClassNames.EventColor3), "phần chạy mang màu loại event");
            bar.SetColorSlot(9);
            Assert.IsFalse(bar.Run.ClassListContains(LiveOpsHubClassNames.EventColor3), "đổi màu phải gỡ màu cũ");
            Assert.IsTrue(bar.Run.ClassListContains(LiveOpsHubClassNames.EventColor1), "ô ngoài 0–7 quay vòng như swatch");

            bar.PeriodHours = 0;
            bar.ActiveHours = 20;
            Assert.IsFalse(bar.IsOverflowing, "chu kỳ 0 là luật chưa hợp lệ — không vẽ tràn (không chia cho 0)");
            foreach (VisualElement part in new[] { bar.Run, bar.Rest, bar.Overflow })
            {
                Assert.IsTrue(part.ClassListContains(LiveOpsHubClassNames.CycleBarPartHidden));
            }
        }

        private static LiveOpsRuleCycleBar CreateBar(VisualElement root, int periodHours, int activeHours)
        {
            LiveOpsRuleCycleBar bar = new LiveOpsRuleCycleBar { PeriodHours = periodHours, ActiveHours = activeHours };
            bar.style.width = BarWidth;
            bar.style.marginBottom = 4f;
            root.Add(bar);
            return bar;
        }
    }
}
