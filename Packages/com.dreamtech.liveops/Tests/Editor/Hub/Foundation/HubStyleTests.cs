using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class HubStyleTests
    {
        [Test]
        public void SetState_RemovesBothFamilies()
        {
            Label label = new Label();
            label.AddToClassList(LiveOpsHubClassNames.TextBlocked);
            label.AddToClassList(LiveOpsHubClassNames.FillWarning);

            LiveOpsHubStyle.SetState(label, HealthState.Ok);

            CollectionAssert.AreEquivalent(new[] { LiveOpsHubClassNames.FillOk }, StateFamilyClasses(label),
                "SetState phải gỡ cả họ text lẫn họ fill cũ — sót class cũ thì một dấu mang hai màu, thứ tự rule USS quyết định màu thắng");

            LiveOpsHubStyle.SetStateText(label, HealthState.NotMeasured);
            CollectionAssert.AreEquivalent(new[] { LiveOpsHubClassNames.TextQuiet }, StateFamilyClasses(label),
                "SetStateText gỡ họ fill; NotMeasured dùng màu quiet");
        }

        [Test]
        public void SetEventColor_OnlyOneSlotClass()
        {
            VisualElement swatch = new VisualElement();
            LiveOpsHubStyle.SetEventColor(swatch, 4);
            LiveOpsHubStyle.SetEventColor(swatch, 6);

            List<string> slotClasses = ClassesWithPrefix(swatch, "liveops-hub-event-color-");
            CollectionAssert.AreEqual(new[] { LiveOpsHubClassNames.EventColor6 }, slotClasses,
                "đổi ô màu phải bỏ ô cũ — swatch mang hai class màu thì màu hiện phụ thuộc thứ tự khai USS");

            LiveOpsHubStyle.SetEventColor(swatch, 9);
            CollectionAssert.AreEqual(new[] { LiveOpsHubClassNames.EventColor1 }, ClassesWithPrefix(swatch, "liveops-hub-event-color-"),
                "ô ngoài 0–7 (asset sửa tay) quay vòng thay vì ném — view không sập vì dữ liệu hỏng");
        }

        [Test]
        public void SetPhase_EndedWithPendingResult_IsPending()
        {
            VisualElement element = new VisualElement();
            LiveOpsHubStyle.SetPhase(element, LiveEventPhase.Active, false);
            LiveOpsHubStyle.SetPhase(element, LiveEventPhase.Ended, true);

            CollectionAssert.AreEqual(new[] { LiveOpsHubClassNames.PhasePending }, ClassesWithPrefix(element, "liveops-hub-phase--"));

            LiveOpsHubStyle.SetPhase(element, LiveEventPhase.None, false);
            CollectionAssert.IsEmpty(ClassesWithPrefix(element, "liveops-hub-phase--"));
        }

        [Test]
        public void SetSeverityStripe_NullOrOk_RemovesStripe()
        {
            VisualElement row = new VisualElement();
            LiveOpsHubStyle.SetSeverityStripe(row, HealthState.Blocked);
            Assert.IsTrue(row.ClassListContains(LiveOpsHubClassNames.FindingStripeBlocked));

            LiveOpsHubStyle.SetSeverityStripe(row, HealthState.Warning);
            CollectionAssert.AreEqual(new[] { LiveOpsHubClassNames.FindingStripeWarning }, ClassesWithPrefix(row, "liveops-hub-finding-stripe--"));

            LiveOpsHubStyle.SetSeverityStripe(row, HealthState.Ok);
            CollectionAssert.IsEmpty(ClassesWithPrefix(row, "liveops-hub-finding-stripe--"));

            LiveOpsHubStyle.SetSeverityStripe(row, HealthState.NotMeasured);
            LiveOpsHubStyle.SetSeverityStripe(row, null);
            CollectionAssert.IsEmpty(ClassesWithPrefix(row, "liveops-hub-finding-stripe--"));
        }

        [Test]
        public void SetEnabledWithReason_DisabledRequiresReason_ReasonOnSlotTooltip()
        {
            LiveOpsButtonSlot slot = new LiveOpsButtonSlot(new Button());

            Assert.Throws<ArgumentException>(() => LiveOpsHubStyle.SetEnabledWithReason(slot, false, ""),
                "khoá nút mà không có lý do là người dùng kẹt — phải chặn ngay lúc gọi");

            LiveOpsHubStyle.SetEnabledWithReason(slot, false, "Chặn: 2 đợt bị bỏ");
            Assert.IsFalse(slot.Button.enabledSelf);
            Assert.IsTrue(slot.IsBlocked);
            Assert.AreEqual("Chặn: 2 đợt bị bỏ", slot.tooltip, "tooltip nằm trên slot vì nút disabled không nhận hover (R-16)");
            Assert.AreEqual("Chặn: 2 đợt bị bỏ", slot.ReasonLabel.text, "lý do in cạnh nút luôn hiện khi khoá");

            LiveOpsHubStyle.SetEnabledWithReason(slot, true, null);
            Assert.IsTrue(slot.Button.enabledSelf);
            Assert.IsFalse(slot.IsBlocked);
            Assert.AreEqual(string.Empty, slot.tooltip);
            Assert.AreEqual(string.Empty, slot.Reason);
        }

        [Test]
        public void SectionHealth_WorseRanksNotMeasuredAboveOk_ReasonRequired()
        {
            SectionHealth ok = SectionHealth.Ok("6 luật");
            SectionHealth notMeasured = SectionHealth.NotMeasured("Chưa kiểm");
            SectionHealth warning = SectionHealth.Warning("1 mất tiến độ", "weekly-pass đổi tiền tố");
            SectionHealth blocked = SectionHealth.Blocked("2 bị bỏ", "2 đợt sẽ bị game bỏ");

            Assert.AreEqual(HealthState.NotMeasured, SectionHealth.Worse(ok, notMeasured).State, "chưa kiểm không bao giờ được gộp thành ổn");
            Assert.AreEqual(HealthState.Warning, SectionHealth.Worse(warning, notMeasured).State);
            Assert.AreEqual(HealthState.Blocked, SectionHealth.Worse(warning, blocked).State);
            Assert.AreEqual(string.Empty, default(SectionHealth).Badge);
            Assert.Throws<ArgumentException>(() => SectionHealth.NotMeasured(""));

            SectionHealth stale = blocked.AsStale(new DateTime(2026, 9, 13, 8, 46, 30, DateTimeKind.Utc));
            Assert.AreEqual(HealthState.NotMeasured, stale.State);
            Assert.IsTrue(stale.IsStale);
            Assert.AreEqual("2 bị bỏ", stale.Badge, "bản cũ giữ badge của lần đo trước");
            Assert.IsNotEmpty(ok.AsStale(new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Utc)).Reason, "NotMeasured luôn có lý do");
        }

        [Test]
        public void SectionHealth_Counts_DefaultEmpty_KeptByStaleAndWorse_AddSums()
        {
            Assert.IsTrue(default(SectionHealth).Counts.IsEmpty, "màn không đếm phát hiện (Tổng quan, Xuất) không phải khai số");
            Assert.IsTrue(SectionHealth.Blocked("chặn", "Copy JSON bị chặn").Counts.IsEmpty);

            var calendarCounts = new LiveOpsHubFindingCounts(2, 0, 2, 0);
            SectionHealth calendar = SectionHealth.Blocked("2 bị bỏ", "2 đợt sẽ bị game bỏ").WithCounts(calendarCounts);
            Assert.AreEqual(HealthState.Blocked, calendar.State, "WithCounts không đổi trạng thái");
            Assert.AreEqual("2 bị bỏ", calendar.Badge);
            Assert.AreEqual("2 đợt sẽ bị game bỏ", calendar.Reason);
            Assert.AreEqual(2, calendar.Counts.ShouldReview, "số đếm không phụ thuộc chữ badge — badge chỉ nói loại nặng nhất");

            SectionHealth stale = calendar.AsStale(new DateTime(2026, 9, 13, 8, 46, 30, DateTimeKind.Utc));
            Assert.AreEqual(calendarCounts, stale.Counts, "bản cũ giữ số của lần đo trước như giữ badge");
            Assert.IsTrue(stale.WithCounts(new LiveOpsHubFindingCounts(0, 1, 0, 0)).IsStale, "WithCounts giữ cờ cũ");

            SectionHealth recurring = SectionHealth.Warning("1 mất tiến độ", "weekly-pass đổi tiền tố").WithCounts(new LiveOpsHubFindingCounts(0, 1, 0, 0));
            Assert.AreEqual(calendarCounts, SectionHealth.Worse(recurring, calendar).Counts, "Worse chọn nguyên một bên, không trộn số");

            // Tầng LÊN LỊCH = Lịch + Luật lặp: cộng từng loại (8.2) — nguồn của "2 bị bỏ · 1 mất tiến độ · 2 nên xem".
            LiveOpsHubFindingCounts schedule = calendar.Counts.Add(recurring.Counts);
            Assert.AreEqual(2, schedule.Dropped);
            Assert.AreEqual(1, schedule.ProgressLost);
            Assert.AreEqual(2, schedule.ShouldReview);
            Assert.AreEqual(0, schedule.NotMeasured);
            Assert.AreEqual(5, schedule.Total);
            Assert.IsFalse(schedule.IsEmpty);

            Assert.Throws<ArgumentOutOfRangeException>(() => new LiveOpsHubFindingCounts(0, 0, -1, 0));
        }

        [Test]
        public void PipelineStages_OrderGatesCaptions()
        {
            CollectionAssert.AreEqual(
                new[] { PipelineStage.Configure, PipelineStage.Schedule, PipelineStage.Check, PipelineStage.Export, PipelineStage.Run },
                PipelineStages.All);
            Assert.IsFalse(PipelineStages.IsGate(PipelineStage.Schedule));
            Assert.IsTrue(PipelineStages.IsGate(PipelineStage.Check));
            Assert.AreEqual("LÊN LỊCH", PipelineStages.CaptionOf(PipelineStage.Schedule));
            Assert.AreEqual(LiveOpsHubPaths.CalendarIconName, PipelineStages.IconNameOf(PipelineStage.Schedule));
        }

        [Test]
        public void Placeholder_HiddenOnlyWhenFieldHasValue_RefreshAfterSetValueWithoutNotify()
        {
            TextField field = new TextField();
            LiveOpsPlaceholder placeholder = LiveOpsPlaceholder.Attach(field, "vd: weekly-pass");

            Assert.AreSame(field, placeholder.Field);
            Assert.IsTrue(field.Contains(placeholder), "placeholder phải nằm trong ô để phủ đúng vùng nhập");
            Assert.AreEqual(PickingMode.Ignore, placeholder.pickingMode, "Label bắt click sẽ chặn focus và IME của ô");
            Assert.IsFalse(placeholder.ClassListContains(LiveOpsHubClassNames.PlaceholderHidden), "ô trống thì hiện gợi ý");

            // SetValueWithoutNotify không bắn ChangeEvent — placeholder chỉ biết khi nơi gán gọi Refresh.
            field.SetValueWithoutNotify("weekly-pass");
            Assert.IsFalse(placeholder.ClassListContains(LiveOpsHubClassNames.PlaceholderHidden));
            placeholder.Refresh();
            Assert.IsTrue(placeholder.ClassListContains(LiveOpsHubClassNames.PlaceholderHidden), "ô có giá trị thì ẩn gợi ý — không chồng chữ");

            field.SetValueWithoutNotify(string.Empty);
            placeholder.Refresh();
            Assert.IsFalse(placeholder.ClassListContains(LiveOpsHubClassNames.PlaceholderHidden), "xoá hết giá trị thì gợi ý quay lại");

            TextField prefilledField = new TextField { value = "summer-2026" };
            Label uxmlPlaceholder = new Label("vd: weekly-pass");
            LiveOpsPlaceholder.TrackExisting(prefilledField, uxmlPlaceholder);
            Assert.AreEqual(PickingMode.Ignore, uxmlPlaceholder.pickingMode, "UXML quên picking-mode thì vẫn ép Ignore");
            Assert.IsTrue(uxmlPlaceholder.ClassListContains(LiveOpsHubClassNames.Placeholder));
            Assert.IsTrue(uxmlPlaceholder.ClassListContains(LiveOpsHubClassNames.PlaceholderHidden), "ô đã có giá trị lúc gắn thì ẩn ngay");
            Assert.Throws<ArgumentNullException>(() => LiveOpsPlaceholder.Attach(null, "gợi ý"));
        }

        [Test]
        public void Chevron_DirectionChange_RemovesPreviousClass()
        {
            LiveOpsChevron chevron = new LiveOpsChevron(LiveOpsChevron.ChevronDirection.Down);
            Assert.IsTrue(chevron.ClassListContains(LiveOpsHubClassNames.Chevron));
            Assert.AreEqual(PickingMode.Ignore, chevron.pickingMode, "mũi tên không được chặn click của hàng chứa nó");
            CollectionAssert.AreEqual(new[] { LiveOpsHubClassNames.ChevronDown }, ClassesWithPrefix(chevron, "liveops-hub-chevron--"));

            chevron.Direction = LiveOpsChevron.ChevronDirection.Left;
            Assert.AreEqual(LiveOpsChevron.ChevronDirection.Left, chevron.Direction);
            CollectionAssert.AreEqual(new[] { LiveOpsHubClassNames.ChevronLeft }, ClassesWithPrefix(chevron, "liveops-hub-chevron--"),
                "đổi hướng phải gỡ class cũ — mang hai class hướng thì góc xoay do thứ tự rule USS quyết");

            chevron.Direction = LiveOpsChevron.ChevronDirection.Left;
            CollectionAssert.AreEqual(new[] { LiveOpsHubClassNames.ChevronLeft }, ClassesWithPrefix(chevron, "liveops-hub-chevron--"), "gán lại cùng hướng không nhân đôi class");
            Assert.AreEqual(LiveOpsHubClassNames.ChevronRight, ClassesWithPrefix(new LiveOpsChevron(), "liveops-hub-chevron--")[0], "mặc định chỉ sang phải");
        }

        private static List<string> StateFamilyClasses(VisualElement element)
        {
            List<string> result = ClassesWithPrefix(element, "liveops-hub-fill--");
            result.AddRange(ClassesWithPrefix(element, "liveops-hub-text--"));
            return result;
        }

        private static List<string> ClassesWithPrefix(VisualElement element, string prefix)
        {
            List<string> result = new List<string>();
            foreach (string className in element.GetClasses())
            {
                if (className.StartsWith(prefix, StringComparison.Ordinal)) result.Add(className);
            }
            return result;
        }
    }
}
