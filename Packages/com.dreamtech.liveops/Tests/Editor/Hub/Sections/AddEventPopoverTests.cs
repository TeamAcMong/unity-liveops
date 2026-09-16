using NUnit.Framework;
using UnityEngine.UIElements;

using DreamTech.LiveOps.Tests;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Popover Thêm đợt [SD1 §3.11]: cây UXML đủ tên element, ba bước chỉ hiện đúng một bước, và Enter ở bước xem lại khi kiểm
    /// nhanh báo chồng giờ là "Quay lại sửa giờ" — KHÔNG phải "Vẫn thêm" (nút phá huỷ không bao giờ là nút mặc định, SP-2 (c)).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class AddEventPopoverTests
    {
        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
        }

        [Test]
        public void Build_RequiredElementsPresent()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventPopover popover = new AddEventPopover(services,
                AddEventFlowModel.Create(services.Session, services.Clock.UtcNow), _ => { });
            VisualElement root = popover.Build();
            foreach (string elementName in LiveOpsHubPaths.RequiredAddEventPopoverElementNames)
            {
                Assert.IsNotNull(root.Q(elementName),
                    "popover Thêm đợt thiếu element '" + elementName + "' — UXML và hằng tên lệch nhau");
            }
        }

        [Test]
        public void Steps_ShowExactlyOneStep()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventPopover popover = new AddEventPopover(services,
                AddEventFlowModel.Create(services.Session, services.Clock.UtcNow), _ => { });
            VisualElement root = popover.Build();
            AssertVisibleStep(root, LiveOpsHubPaths.AddEventPopoverElementNames.StepType);

            popover.OnKeyDown(KeyDownEvent.GetPooled('\n', UnityEngine.KeyCode.Return, UnityEngine.EventModifiers.None));
            Assert.AreEqual(AddEventFlowModel.StepChooseType, popover.Flow.Step,
                "chưa chọn loại thì Enter không sang bước sau");
        }

        [Test]
        public void EnterOnOverlappingReview_GoesBackInsteadOfAdding()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            int submits = 0;
            AddEventFlowModel flow = AddEventFlowModel.Create(services.Session, services.Clock.UtcNow)
                .WithType("treasure-hunt")
                .WithTimes("2026-09-15", "00:00", 72);
            AddEventPopover popover = new AddEventPopover(services, flow, _ => submits++);
            popover.Build();
            Assert.IsTrue(popover.Flow.WillBeDropped, "fixture phải là biến thể chồng giờ của Hình 13b");

            popover.OnKeyDown(KeyDownEvent.GetPooled('\n', UnityEngine.KeyCode.Return, UnityEngine.EventModifiers.None));
            Assert.AreEqual(0, submits, "Enter không được thêm đợt mà game sẽ bỏ — nút đó chỉ chạy khi click");
            Assert.AreEqual(AddEventFlowModel.StepChooseTimes, popover.Flow.Step, "Enter đưa về bước sửa giờ");
        }

        private static void AssertVisibleStep(VisualElement root, string visibleElementName)
        {
            string[] stepNames =
            {
                LiveOpsHubPaths.AddEventPopoverElementNames.StepType,
                LiveOpsHubPaths.AddEventPopoverElementNames.StepTimes,
                LiveOpsHubPaths.AddEventPopoverElementNames.StepReview,
            };
            for (int index = 0; index < stepNames.Length; index++)
            {
                VisualElement step = root.Q(stepNames[index]);
                Assert.IsNotNull(step);
                bool isHidden = step.ClassListContains(LiveOpsHubClassNames.CalendarHidden);
                Assert.AreEqual(stepNames[index] != visibleElementName, isHidden,
                    "đúng một bước hiện tại một lúc: " + stepNames[index]);
            }
        }
    }
}
