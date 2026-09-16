using System;
using System.Collections.Generic;
using NUnit.Framework;

using DreamTech.LiveOps.Tests;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Ba bước của popover Thêm đợt [SD1 §3.11]: lọc loại (loại sinh từ luật bị tắt), chọn giờ, xem lại có id đề nghị và kiểm
    /// nhanh chỉ trên LÀN của loại đó.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class AddEventFlowModelTests
    {
        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
        }

        [Test]
        public void AddEvent_ThreeSteps_SuggestedId_QuickCheckOverlap()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventFlowModel flow = AddEventFlowModel.Create(services.Session, services.Clock.UtcNow);
            Assert.AreEqual(AddEventFlowModel.StepChooseType, flow.Step);
            Assert.IsFalse(flow.CanAdvance(), "chưa chọn loại thì nút Tiếp tắt");

            flow = flow.WithType("treasure-hunt");
            Assert.AreEqual(AddEventFlowModel.StepChooseType, flow.Step,
                "bấm một hàng loại là ĐANG TRỎ, chưa đi tiếp [SD1 §3.11 bước 1]");
            Assert.IsTrue(flow.CanAdvance());
            flow = flow.Next();
            Assert.AreEqual(AddEventFlowModel.StepChooseTimes, flow.Step, "Enter / nút Tiếp mới mở bước 2");

            // 15/9 00:00 + 72 giờ chồng hẳn lên hunt-0914 (14/9 → 17/9) — đúng biến thể "chồng giờ" của Hình 13b.
            flow = flow.WithTimes("2026-09-15", "00:00", 72);
            Assert.AreEqual(AddEventFlowModel.StepChooseTimes, flow.Step, "gõ giờ cũng không tự sang bước sau");
            flow = flow.Next();
            Assert.AreEqual(AddEventFlowModel.StepReview, flow.Step);
            Assert.IsNotEmpty(flow.SuggestedEventId, "bước xem lại luôn có id đề nghị (PD-20)");
            Assert.AreEqual(new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc), flow.EndUtc, "kết thúc tự tính từ Dài");
            Assert.IsTrue(flow.WillBeDropped, "kiểm nhanh thấy chồng giờ nên game sẽ bỏ đợt sắp thêm");
            Assert.IsNotNull(flow.DropFinding, "có phát hiện để dựng câu — câu chữ lấy từ LiveOpsFindingText, không tự viết");
            Assert.IsNotNull(flow.ToEdit(), "vẫn thêm được: nút danger 'Vẫn thêm' chỉ chạy khi click");
        }

        [Test]
        public void AddEvent_NoOverlap_QuickCheckClean()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventFlowModel flow = AddEventFlowModel.Create(services.Session, services.Clock.UtcNow)
                .WithType("treasure-hunt")
                .Next()
                .WithTimes("2026-11-02", "00:00", 24)
                .Next();
            Assert.IsFalse(flow.WillBeDropped, "tháng 11 chưa có đợt treasure-hunt nào — không chồng");
            AddFixedEventEdit edit = flow.ToEdit();
            Assert.IsNotNull(edit);
            Assert.AreEqual("treasure-hunt", edit.Entry.EventType);
            Assert.AreEqual("2026-11-02T00:00:00Z", edit.Entry.StartUtcText);
            Assert.AreEqual(string.Empty, edit.Entry.ConfigKey, "để trống = kế thừa mặc định của loại khi Xuất JSON");
            Assert.AreEqual("hunt_default", flow.EffectiveConfigKey, "chữ dẫn nêu đúng giá trị Xuất sẽ ghi");
        }

        [Test]
        public void TypeChoices_RecurringDisabledWithReason()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventFlowModel flow = AddEventFlowModel.Create(services.Session, services.Clock.UtcNow);
            IReadOnlyList<AddEventTypeChoice> choices = flow.TypeChoices(string.Empty);
            Assert.AreEqual(5, choices.Count, "lịch mẫu có 5 loại — loại sinh từ luật vẫn hiện để biết nó tồn tại");
            AddEventTypeChoice weeklyPass = FindChoice(choices, "weekly-pass");
            Assert.IsFalse(weeklyPass.IsEnabled, "loại có luật lặp không tạo đợt cố định được");
            Assert.AreEqual(LiveOpsHubStrings.CalendarAddTypeRecurringTag, weeklyPass.TagText, "nhãn nói rõ sửa ở đâu");
            AddEventTypeChoice treasureHunt = FindChoice(choices, "treasure-hunt");
            Assert.IsTrue(treasureHunt.IsEnabled);
            Assert.AreEqual(LiveOpsHubStrings.CalendarAddTypeOptInTag, treasureHunt.TagText, "treasure-hunt phải bấm tham gia");

            IReadOnlyList<AddEventTypeChoice> filtered = flow.TypeChoices("lava");
            Assert.AreEqual(1, filtered.Count, "lọc theo chuỗi gõ");
            Assert.AreEqual("lava-quest", filtered[0].TypeId);
        }

        [Test]
        public void Back_FromReview_ReturnsToTimes()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventFlowModel flow = AddEventFlowModel.Create(services.Session, services.Clock.UtcNow)
                .WithType("lava-quest")
                .Next()
                .WithTimes("2026-11-02", "00:00", 24)
                .Next();
            AddEventFlowModel back = flow.Back();
            Assert.AreEqual(AddEventFlowModel.StepChooseTimes, back.Step);
            Assert.AreEqual("2026-11-02", back.StartDateText, "Quay lại giữ nguyên giờ đã gõ");
            Assert.AreEqual(AddEventFlowModel.StepChooseType, back.Back().Step);
            Assert.AreEqual(AddEventFlowModel.StepChooseType, back.Back().Back().Step, "ở bước 1 Quay lại không đóng popover");
        }

        [Test]
        public void CreateAt_StartsAtTimesStepWithLaneAndTime()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            DateTime startUtc = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc);
            AddEventFlowModel flow = AddEventFlowModel.CreateAt(services.Session, services.Clock.UtcNow, "treasure-hunt", startUtc, 72);
            Assert.AreEqual(AddEventFlowModel.StepChooseTimes, flow.Step, "nhấp đúp chỗ trống đã biết loại và giờ");
            Assert.AreEqual("treasure-hunt", flow.EventType);
            Assert.AreEqual("2026-09-21", flow.StartDateText);
            Assert.AreEqual(72, flow.DurationHours);
        }

        [Test]
        public void Next_WithoutTypeStaysOnFirstStep()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventFlowModel flow = AddEventFlowModel.Create(services.Session, services.Clock.UtcNow);
            Assert.AreSame(flow, flow.Next(), "chưa đủ dữ liệu thì Tiếp không đi đâu cả — nút đã tắt, model cũng không đổi");
        }

        [Test]
        public void TypeChoicesOf_SharedWithInspector_OnlyFixedTypesAreEnabled()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            IReadOnlyList<AddEventTypeChoice> choices = AddEventFlowModel.TypeChoicesOf(services.Session.Document, string.Empty);
            AddEventTypeChoice weeklyPass = FindChoice(choices, "weekly-pass");
            Assert.IsFalse(weeklyPass.IsEnabled,
                "ô Loại của inspector đọc chính danh sách này — hai đường vào một quyết định thì không được lệch nhau");
        }

        private static AddEventTypeChoice FindChoice(IReadOnlyList<AddEventTypeChoice> choices, string typeId)
        {
            for (int index = 0; index < choices.Count; index++)
            {
                if (string.Equals(choices[index].TypeId, typeId, StringComparison.Ordinal)) return choices[index];
            }
            Assert.Fail("không thấy loại '" + typeId + "' trong danh sách bước 1");
            return null;
        }
    }
}
