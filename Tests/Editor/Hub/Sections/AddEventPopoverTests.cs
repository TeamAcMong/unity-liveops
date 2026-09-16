using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

using DreamTech.LiveOps.Tests;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Popover Thêm đợt [SD1 §3.11]: cây UXML đủ tên element, ba bước chỉ hiện đúng một bước, Esc đóng cửa sổ, và Enter ở bước
    /// xem lại khi kiểm nhanh báo chồng giờ là "Quay lại sửa giờ" — KHÔNG phải "Vẫn thêm" (nút phá huỷ không bao giờ là nút mặc
    /// định, SP-2 (c)).
    /// <para>
    /// Bước ẩn/hiện chốt bằng <c>resolvedStyle.display</c> trên một panel THẬT, không bằng tên class: class chỉ ẩn được khi
    /// stylesheet của màn Lịch tới được panel riêng của popover — bản trước mang đúng class mà ba bước vẫn hiện cùng lúc.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class AddEventPopoverTests
    {
        private const int MaximumLayoutFrames = 60;
        private const int MaximumLayoutMilliseconds = 5000;

        private LiveOpsHubWindow _window;

        [TearDown]
        public void TearDown()
        {
            if (_window != null)
            {
                _window.Close();
                _window = null;
            }
            LiveOpsHubTestServices.ReleaseAll();
        }

        [Test]
        public void Build_RequiredElementsPresent()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventPopover popover = new AddEventPopover(services,
                AddEventFlowModel.Create(services.Session, services.Clock.UtcNow), _ => { });
            VisualElement root = popover.BuildForTest();
            foreach (string elementName in LiveOpsHubPaths.RequiredAddEventPopoverElementNames)
            {
                Assert.IsNotNull(root.Q(elementName),
                    "popover Thêm đợt thiếu element '" + elementName + "' — UXML và hằng tên lệch nhau");
            }
            Assert.AreEqual(LiveOpsPopoverContent.RootElementName, root.name,
                "popover dựng qua LiveOpsPopoverContent nên gốc có token của hub — không có nó thì cửa sổ riêng mất màu và class màn");
        }

        [Test]
        public void ChooseType_StaysOnFirstStepUntilNext()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventPopover popover = new AddEventPopover(services,
                AddEventFlowModel.Create(services.Session, services.Clock.UtcNow).WithType("treasure-hunt"), _ => { });
            popover.BuildForTest();
            Assert.AreEqual(AddEventFlowModel.StepChooseType, popover.Flow.Step,
                "bấm một hàng loại là ĐANG TRỎ, chưa đi tiếp [SD1 §3.11 bước 1]");

            popover.OnKeyDown(KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None));
            Assert.AreEqual(AddEventFlowModel.StepChooseTimes, popover.Flow.Step, "Enter mới là đường tiến bước");
        }

        [Test]
        public void Escape_ClosesPopover()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventPopover popover = new AddEventPopover(services,
                AddEventFlowModel.Create(services.Session, services.Clock.UtcNow), _ => { });
            popover.BuildForTest();
            popover.HandleKeyDownForTest(KeyDownEvent.GetPooled('\0', KeyCode.Escape, EventModifiers.None));
            Assert.IsTrue(popover.EscapeHandled, "Esc = đóng popover, không tạo gì [SD1 §3.11]");
        }

        [Test]
        public void EnterOnOverlappingReview_GoesBackInsteadOfAdding()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            int submits = 0;
            AddEventFlowModel flow = AddEventFlowModel.Create(services.Session, services.Clock.UtcNow)
                .WithType("treasure-hunt")
                .Next()
                .WithTimes("2026-09-15", "00:00", 72)
                .Next();
            AddEventPopover popover = new AddEventPopover(services, flow, _ => submits++);
            popover.BuildForTest();
            Assert.IsTrue(popover.Flow.WillBeDropped, "fixture phải là biến thể chồng giờ của Hình 13b");

            popover.OnKeyDown(KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None));
            Assert.AreEqual(0, submits, "Enter không được thêm đợt mà game sẽ bỏ — nút đó chỉ chạy khi click");
            Assert.AreEqual(AddEventFlowModel.StepChooseTimes, popover.Flow.Step, "Enter đưa về bước sửa giờ");
        }

        /// <summary>
        /// Dựng đúng cửa sổ mà lượt chụp Hình 13b dựng, rồi đo trên panel thật: chỉ bước 2 có <c>display</c>, hai bước kia biến
        /// mất hẳn, và gốc popover rộng đúng 320px như [SD1 §3.11].
        /// </summary>
        [UnityTest]
        public IEnumerator Steps_OnlyOneStepIsDisplayed()
        {
            yield return OpenScenario(LiveOpsHubCaptureScenarioIds.H13bAddEventStep2);
            VisualElement root = _window.rootVisualElement.Q(LiveOpsPopoverContent.RootElementName);
            Assert.IsNotNull(root, "cửa sổ chụp phải chứa gốc popover");
            Assert.AreEqual(AddEventPopover.PopoverWidth, root.layout.width, 0.5f, "popover rộng 320px [SD1 §3.11]");
            AssertDisplayed(root, LiveOpsHubPaths.AddEventPopoverElementNames.StepTimes, true);
            AssertDisplayed(root, LiveOpsHubPaths.AddEventPopoverElementNames.StepType, false);
            AssertDisplayed(root, LiveOpsHubPaths.AddEventPopoverElementNames.StepReview, false);
        }

        private static void AssertDisplayed(VisualElement root, string stepElementName, bool isDisplayed)
        {
            VisualElement step = root.Q(stepElementName);
            Assert.IsNotNull(step, "popover thiếu bước '" + stepElementName + "'");
            Assert.AreEqual(isDisplayed ? DisplayStyle.Flex : DisplayStyle.None, step.resolvedStyle.display,
                "đúng một bước hiện tại một lúc — '" + stepElementName + "'");
        }

        private IEnumerator OpenScenario(string scenarioId)
        {
            LiveOpsHubCaptureScenario scenario = LiveOpsHubCaptureScenarios.Find(scenarioId);
            Assert.IsNotNull(scenario, "kịch bản chụp '" + scenarioId + "' chưa đăng ký");
            _window = (LiveOpsHubWindow)scenario.OpenWindow();
            _window.position = new Rect(0, 0, scenario.WindowWidth, scenario.WindowHeight);
            yield return WaitForLayout(_window.rootVisualElement);
            for (int frame = 0; frame < scenario.MinimumSettleFrames; frame++) yield return null;
        }

        /// <summary>(V-23) Chỉ fail khi quá CẢ 60 khung LẪN 5 giây — máy chậm không được biến test UI thành đỏ giả.</summary>
        private static IEnumerator WaitForLayout(VisualElement element)
        {
            int frames = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!LiveOpsHubWindowTestScope.HasLayout(element))
            {
                frames++;
                if (frames > MaximumLayoutFrames && stopwatch.ElapsedMilliseconds > MaximumLayoutMilliseconds)
                {
                    Assert.Fail("cửa sổ hub không có layout sau 60 khung và 5 giây — test UI phải chạy không -nographics");
                }
                yield return null;
            }
            yield return null;
            yield return null;
        }
    }
}
