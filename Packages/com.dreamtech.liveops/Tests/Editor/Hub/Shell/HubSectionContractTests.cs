using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Hợp đồng <see cref="IHubSection"/> trên registry thật (Logic, không mở cửa sổ): id, subtitle, tầng, health rẻ và ổn định,
    /// NotMeasured luôn có lý do; cùng <see cref="LiveOpsHealthThrottle"/> (8.4) vì rail đọc health của mọi màn qua throttle.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class HubSectionContractTests
    {
        private const int MaximumSubtitleLength = 60;
        private static readonly Regex KebabCaseId = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$");

        [Test]
        public void Registry_IdsUniqueKebabCase()
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (IHubSection section in LiveOpsHubSections.Create())
            {
                Assert.IsTrue(KebabCaseId.IsMatch(section.Id), "id '" + section.Id + "' phải kebab-case — id nằm trong SessionState và điều hướng");
                Assert.IsTrue(ids.Add(section.Id), "id '" + section.Id + "' trùng — rail và điều hướng sẽ mở nhầm màn");
            }
            CollectionAssert.AreEquivalent(new[]
            {
                LiveOpsHubSections.Ids.Overview, LiveOpsHubSections.Ids.EventTypes, LiveOpsHubSections.Ids.Calendar,
                LiveOpsHubSections.Ids.RecurringRules, LiveOpsHubSections.Ids.Validation, LiveOpsHubSections.Ids.Export,
            }, ids, "P1 đăng ký đúng 6 màn (PD-1)");
        }

        [Test]
        public void Registry_SubtitlePresentUnder60()
        {
            foreach (IHubSection section in LiveOpsHubSections.Create())
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(section.Title), section.Id + ": thiếu tiêu đề");
                Assert.IsFalse(string.IsNullOrWhiteSpace(section.Subtitle), section.Id + ": subtitle bắt buộc — section header luôn nói màn để làm gì");
                Assert.Less(section.Subtitle.Length, MaximumSubtitleLength, section.Id + ": subtitle '" + section.Subtitle + "' phải dưới 60 ký tự ([FD §3.6])");
            }
        }

        [Test]
        public void Registry_StageInAll_OrderMatchesRail()
        {
            List<IHubSection> sections = LiveOpsHubSections.Create();
            int previousStageIndex = -1;
            foreach (IHubSection section in sections)
            {
                int stageIndex = IndexOfStage(section.Stage);
                Assert.GreaterOrEqual(stageIndex, 0, section.Id + ": tầng " + section.Stage + " không có trong PipelineStages.All");
                Assert.GreaterOrEqual(stageIndex, previousStageIndex, section.Id + ": thứ tự registry phải theo tầng — thứ tự registry là ⌘1…6 và thứ tự rail");
                previousStageIndex = stageIndex;
            }
            Assert.AreEqual(LiveOpsHubSections.Ids.Overview, sections[0].Id, "Tổng quan đứng đầu — màn mở mặc định");
        }

        [Test]
        public void Registry_EveryDrawnStageHasSection()
        {
            List<IHubSection> sections = LiveOpsHubSections.Create();
            List<SectionHealth> healths = new List<SectionHealth>();
            foreach (IHubSection section in sections) healths.Add(section.GetHealth());

            LiveOpsHubRailModel model = LiveOpsHubRailModel.Build(sections, healths, null, false);

            Assert.AreEqual(4, model.Stages.Count, "P1 vẽ CẤU HÌNH, LÊN LỊCH, KIỂM, XUẤT — CHẠY không có màn nên không vẽ");
            foreach (LiveOpsHubRailStageRow stage in model.Stages)
            {
                Assert.Greater(stage.Rows.Count, 0, "tầng " + stage.Caption + " được vẽ mà không có màn");
            }
        }

        [Test]
        public void GetHealth_TwiceSameState()
        {
            foreach (IHubSection section in LiveOpsHubSections.Create())
            {
                SectionHealth first = section.GetHealth();
                SectionHealth second = section.GetHealth();
                Assert.AreEqual(first.State, second.State, section.Id + ": GetHealth hai lần khác trạng thái — rail sẽ nhấp nháy");
                Assert.AreEqual(first.Reason, second.Reason, section.Id);
            }
        }

        [Test]
        public void NotMeasured_HasReason()
        {
            foreach (IHubSection section in LiveOpsHubSections.Create())
            {
                SectionHealth health = section.GetHealth();
                if (health.State != HealthState.Ok)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(health.Reason), section.Id + ": " + health.State + " không có lý do — vòng rỗng không lý do bị hiểu là ổn");
                }
            }
            Assert.Throws<ArgumentException>(() => SectionHealth.NotMeasured(string.Empty), "SectionHealth phải chặn NotMeasured không lý do ngay khi dựng");
        }

        [Test]
        public void Worse_RanksNotMeasuredAboveOk()
        {
            SectionHealth ok = SectionHealth.Ok();
            SectionHealth notMeasured = SectionHealth.NotMeasured("chưa kiểm");
            SectionHealth warning = SectionHealth.Warning("1 nên xem", "hunt-0914 không tự khai configKey");
            SectionHealth blocked = SectionHealth.Blocked("2 bị bỏ", "2 đợt bị bỏ");

            Assert.AreEqual(HealthState.NotMeasured, SectionHealth.Worse(ok, notMeasured).State, "chưa kiểm không bao giờ gộp thành ổn");
            Assert.AreEqual(HealthState.NotMeasured, SectionHealth.Worse(notMeasured, ok).State);
            Assert.AreEqual(HealthState.Warning, SectionHealth.Worse(notMeasured, warning).State);
            Assert.AreEqual(HealthState.Blocked, SectionHealth.Worse(warning, blocked).State);
        }

        [Test]
        public void Create_ReturnsNewListAndNewSectionsEachTime()
        {
            List<IHubSection> first = LiveOpsHubSections.Create();
            List<IHubSection> second = LiveOpsHubSections.Create();
            Assert.AreNotSame(first, second);
            for (int index = 0; index < first.Count; index++)
            {
                Assert.AreNotSame(first[index], second[index], "hai cửa sổ hub không được dùng chung instance màn (view và trạng thái riêng)");
            }
        }

        [Test]
        public void Registry_SectionsCreateViewWithRequiredElements()
        {
            foreach (IHubSection section in LiveOpsHubSections.Create())
            {
                VisualElement view = section.CreateView();
                Assert.IsNotNull(view, section.Id + ": CreateView trả null");
                foreach (string elementName in section.RequiredElementNames)
                {
                    Assert.IsNotNull(view.Q(elementName), section.Id + ": thiếu element '" + elementName + "'");
                }
                if (section is IHubSectionViewState viewState)
                {
                    Assert.DoesNotThrow(() => viewState.RestoreViewState(viewState.CaptureViewState()), section.Id + ": khứ hồi trạng thái view ném");
                    Assert.DoesNotThrow(() => viewState.RestoreViewState("{hỏng"), section.Id + ": JSON hỏng phải về mặc định, không ném");
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------ throttle

        [Test]
        public void HealthThrottle_KeepsResultWithinWindow_RecomputesAfter()
        {
            double now = 100.0;
            int calls = 0;
            LiveOpsHealthThrottle throttle = new LiveOpsHealthThrottle(() =>
            {
                calls++;
                return calls == 1 ? SectionHealth.Ok() : SectionHealth.Warning("1 nên xem", "đổi sau lần đầu");
            }, LiveOpsHealthThrottle.DefaultSeconds, () => now);

            Assert.AreEqual(HealthState.Ok, throttle.Get().State);
            now += LiveOpsHealthThrottle.DefaultSeconds - 0.5;
            Assert.AreEqual(HealthState.Ok, throttle.Get().State, "trong cửa sổ 3 giây phải trả bản đã giữ");
            Assert.AreEqual(1, throttle.ComputeCount);
            now += 1.0;
            Assert.AreEqual(HealthState.Warning, throttle.Get().State, "hết cửa sổ phải tính lại");
            Assert.AreEqual(2, throttle.ComputeCount);
        }

        [Test]
        public void HealthThrottle_InvalidateAll_RecomputesImmediately()
        {
            double now = 10.0;
            LiveOpsHealthThrottle throttle = new LiveOpsHealthThrottle(() => SectionHealth.Ok(), LiveOpsHealthThrottle.DefaultSeconds, () => now);
            throttle.Get();
            LiveOpsHealthThrottle.InvalidateAll();
            throttle.Get();
            Assert.AreEqual(2, throttle.ComputeCount, "InvalidateAll sau sửa/kiểm phải làm mọi throttle tính lại ngay, không chờ hết 3 giây");

            throttle.Invalidate();
            throttle.Get();
            Assert.AreEqual(3, throttle.ComputeCount);
        }

        [Test]
        public void HealthThrottle_ComputeThrows_NotMeasuredWithReasonCachedForWindow()
        {
            double now = 50.0;
            LiveOpsHealthThrottle throttle = new LiveOpsHealthThrottle(() => throw new InvalidOperationException("hỏng"), LiveOpsHealthThrottle.DefaultSeconds, () => now);
            LogAssert.Expect(LogType.Warning, new Regex("GetHealth của một màn ném InvalidOperationException"));

            SectionHealth health = throttle.Get();
            throttle.Get();

            Assert.AreEqual(HealthState.NotMeasured, health.State, "màn ném không được làm sập rail và không được trông như ổn");
            Assert.AreEqual(LiveOpsHubStrings.ShellHealthThrewReason, health.Reason);
            Assert.AreEqual(1, throttle.ComputeCount, "giữ kết quả lỗi hết cửa sổ — không spam Console mỗi giây");
        }

        private static int IndexOfStage(PipelineStage stage)
        {
            for (int index = 0; index < PipelineStages.All.Count; index++)
            {
                if (PipelineStages.All[index] == stage) return index;
            }
            return -1;
        }
    }
}
