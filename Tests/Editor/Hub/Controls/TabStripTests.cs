using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>Tab tự làm: đúng một tab bật, bấm lại tab đang bật không tắt nó, khoá tab bắt buộc có lý do.</summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class TabStripTests
    {
        private ControlsTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            _panel?.Dispose();
            _panel = null;
        }

        [UnityTest]
        public IEnumerator TabStrip_ExclusiveNeverEmpty()
        {
            _panel = ControlsTestPanel.Open();
            VisualElement root = _panel.CreateRoot(false);
            LiveOpsTabStrip strip = new LiveOpsTabStrip { Choices = "Ngày|3 tuần|Tháng" };
            List<int> changes = new List<int>();
            List<int> pressedAgain = new List<int>();
            strip.SelectedIndexChanged += index => changes.Add(index);
            strip.SelectedTabPressedAgain += index => pressedAgain.Add(index);
            root.Add(strip);
            yield return ControlsTestPanel.WaitForLayout(strip);

            Assert.AreEqual(3, strip.ChoiceCount);
            Assert.AreEqual(0, strip.SelectedIndex, "dải tab mới dựng chọn sẵn tab đầu");
            AssertOnlyTabOn(strip, 0);

            // Người dùng bấm tab "3 tuần": toggle tự bật → dải chuyển chọn, tắt tab cũ.
            strip.TabAt(1).value = true;
            yield return null;
            Assert.AreEqual(1, strip.SelectedIndex);
            AssertOnlyTabOn(strip, 1);
            CollectionAssert.AreEqual(new[] { 1 }, changes);

            // Bấm lại tab đang bật: toggle tự tắt → dải bật lại ngay, không đổi chọn, báo "bấm lại".
            strip.TabAt(1).value = false;
            yield return null;
            Assert.AreEqual(1, strip.SelectedIndex, "bấm lại tab đang bật không bao giờ bỏ chọn hết");
            AssertOnlyTabOn(strip, 1);
            CollectionAssert.AreEqual(new[] { 1 }, changes, "bấm lại không phải đổi tab");
            CollectionAssert.AreEqual(new[] { 1 }, pressedAgain);

            // Gán ngoài khoảng bị kẹp; gán đúng tab đang bật không bắn.
            strip.SelectedIndex = 99;
            Assert.AreEqual(2, strip.SelectedIndex);
            AssertOnlyTabOn(strip, 2);
            strip.SelectedIndex = 2;
            CollectionAssert.AreEqual(new[] { 1, 2 }, changes);
            strip.SelectedIndex = -5;
            Assert.AreEqual(0, strip.SelectedIndex);
            strip.SetSelectedIndexWithoutNotify(1);
            Assert.AreEqual(1, strip.SelectedIndex);
            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, changes, "đổi không báo thì không bắn sự kiện");

            // Dựng lại nhãn giữ chỉ số (kẹp khi ít tab hơn); rỗng thì -1.
            strip.Choices = "Nháp|Bản đã đăng";
            yield return null;
            Assert.AreEqual(2, strip.ChoiceCount);
            Assert.AreEqual(1, strip.SelectedIndex, "đổi nhãn không được làm nhảy tab");
            AssertOnlyTabOn(strip, 1);
            strip.Choices = "Một";
            AssertOnlyTabOn(strip, 0);
            strip.Choices = string.Empty;
            Assert.AreEqual(0, strip.ChoiceCount);
            Assert.AreEqual(-1, strip.SelectedIndex);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TabStrip_DisabledChoice_RequiresReasonOnSlot()
        {
            _panel = ControlsTestPanel.Open();
            VisualElement root = _panel.CreateRoot(false);
            LiveOpsTabStrip strip = new LiveOpsTabStrip { Choices = "Nháp|Bản đã đăng" };
            root.Add(strip);
            yield return ControlsTestPanel.WaitForLayout(strip);

            Assert.Throws<ArgumentException>(() => strip.SetChoiceEnabled(1, false, string.Empty), "tab khoá không nói vì sao là người dùng kẹt");
            Assert.Throws<ArgumentOutOfRangeException>(() => strip.SetChoiceEnabled(5, true, null));

            strip.SetChoiceEnabled(1, false, "Chưa có dấu đã đăng");
            yield return null;
            Assert.IsFalse(strip.TabAt(1).enabledSelf);
            Assert.AreEqual("Chưa có dấu đã đăng", strip.SlotAt(1).tooltip, "lý do nằm trên slot — toggle disabled có thể không nhận hover");
            Assert.AreEqual(0, strip.SelectedIndex, "khoá tab khác không đổi chọn");

            strip.SetChoiceEnabled(1, true, null);
            Assert.IsTrue(strip.TabAt(1).enabledSelf);
            Assert.AreEqual(string.Empty, strip.SlotAt(1).tooltip, "mở tab thì xoá lý do");
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertOnlyTabOn(LiveOpsTabStrip strip, int selected)
        {
            for (int index = 0; index < strip.ChoiceCount; index++)
            {
                Assert.AreEqual(index == selected, strip.TabAt(index).value, "tab " + index + " — đúng một tab bật");
            }
        }
    }
}
