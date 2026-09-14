using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Model thuần của rail (Logic, chạy -nographics): đường nối chết, ô chặn, badge cũ, tầng không vẽ — mọi quyết định mà view rail
    /// chỉ đọc lại ([FD §3.5], 8.2). Health và bộ tổng hợp dựng tay theo số của Hình 4 (2 bị bỏ · 1 mất tiến độ · 2 nên xem ·
    /// 1 chưa kiểm).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class LiveOpsHubRailModelTests
    {
        private static readonly DateTime CheckedAtUtc = new DateTime(2026, 9, 13, 8, 46, 30, DateTimeKind.Utc);

        [Test]
        public void DeadConnector_FromFirstBlockedGate()
        {
            List<FakeHubSection> sections = FakeHubSection.CreateDesignSampleShaped();

            LiveOpsHubRailModel model = Build(sections, DesignSummary(), false);

            Assert.AreEqual(4, model.Stages.Count, "P1 vẽ đúng 4 tầng — CHẠY không có màn nên không vẽ (PD-1)");
            Assert.AreEqual(PipelineStage.Check, model.Stages[model.FirstBlockedGateIndex].Stage,
                "Lịch Blocked ở LÊN LỊCH không phải cổng — đường nối chết từ KIỂM, không từ tầng làm việc");
            Assert.IsFalse(model.Stages[0].IsConnectorDead);
            Assert.IsFalse(model.Stages[1].IsConnectorDead, "tầng Lên lịch Blocked vẫn nối sống: lỗi ở đó chỉ hiện thành phát hiện của Kiểm");
            Assert.IsFalse(model.Stages[2].IsConnectorAboveDead, "nửa trên của dấu tầng cổng đầu tiên còn sống — chỉ đoạn từ dấu trở xuống chết");
            Assert.IsTrue(model.Stages[2].IsConnectorDead);
            Assert.IsTrue(model.Stages[3].IsConnectorAboveDead);
            Assert.IsTrue(model.Stages[3].IsConnectorDead);
        }

        [Test]
        public void NoBlockedGate_NoDeadConnector_NoBlocker()
        {
            List<FakeHubSection> sections = FakeHubSection.CreateRegistryShaped();
            sections[2].Health = SectionHealth.Blocked("2 bị bỏ", "2 đợt bị bỏ");

            LiveOpsHubRailModel model = Build(sections, null, false);

            Assert.AreEqual(-1, model.FirstBlockedGateIndex, "chặn ở tầng làm việc không phải cổng");
            Assert.IsFalse(model.HasBlocker, "ô chặn chỉ hiện khi một tầng cổng Blocked");
            Assert.IsNull(model.BlockerNavigation);
            Assert.AreEqual(string.Empty, model.BlockerTitle);
            foreach (LiveOpsHubRailStageRow stage in model.Stages) Assert.IsFalse(stage.IsConnectorDead);
        }

        [Test]
        public void Blocker_CheckBlocked_DroppedDetailAndValidationFilter()
        {
            LiveOpsHubRailModel model = Build(FakeHubSection.CreateDesignSampleShaped(), DesignSummary(), false);

            Assert.IsTrue(model.HasBlocker);
            Assert.AreEqual("Dừng ở Kiểm lịch", model.BlockerTitle);
            Assert.AreEqual("2 đợt sẽ bị game bỏ khi đọc lịch.", model.BlockerDetail);
            Assert.AreEqual(LiveOpsHubSections.Ids.Validation, model.BlockerNavigation.SectionId);
            Assert.AreEqual(LiveOpsHubNavigation.FilterDropped, model.BlockerNavigation.FilterConsequence,
                "bấm ô chặn mở Kiểm lịch đã lọc nhóm Bị bỏ — thứ đang chặn");
        }

        [Test]
        public void Blocker_ExportBlocked_NavigatesExportWithSectionReason()
        {
            List<FakeHubSection> sections = FakeHubSection.CreateRegistryShaped();
            sections[5].Health = SectionHealth.Blocked("chặn", "Copy JSON bị khoá: 1 thay đổi bắt buộc chưa xem");

            LiveOpsHubRailModel model = Build(sections, null, false);

            Assert.AreEqual(PipelineStage.Export, model.Stages[model.FirstBlockedGateIndex].Stage);
            Assert.AreEqual("Dừng ở Xuất JSON", model.BlockerTitle);
            Assert.AreEqual("Copy JSON bị khoá: 1 thay đổi bắt buộc chưa xem", model.BlockerDetail, "không có bộ tổng hợp thì câu là lý do của màn chặn");
            Assert.AreEqual(LiveOpsHubSections.Ids.Export, model.BlockerNavigation.SectionId);
            Assert.AreEqual(LiveOpsHubNavigation.FilterNone, model.BlockerNavigation.FilterConsequence, "dừng ở Xuất thì mở thẳng Xuất JSON, không lọc");
        }

        [Test]
        public void StaleBadge_KeepsOldCountWithPrefixAndTooltip()
        {
            List<FakeHubSection> sections = FakeHubSection.CreateDesignSampleShaped();
            sections[4].Health = sections[4].Health.AsStale(CheckedAtUtc);

            LiveOpsHubRailModel model = Build(sections, DesignSummary(), true);
            LiveOpsHubRailStageRow check = model.Stages[2];

            Assert.AreEqual(HealthState.NotMeasured, check.State, "kết quả cũ không được trông như vẫn đúng (PD-23)");
            Assert.AreEqual("cũ · 2 bị bỏ", check.Badge);
            Assert.IsTrue(check.IsBadgeStale);
            Assert.AreEqual(HealthState.Blocked, check.BadgeState, "badge cũ vẫn chữ blocked — con số cũ vẫn là thông tin");
            Assert.AreEqual("Kết quả kiểm lúc 08:46:30, lịch đã đổi sau đó: 2 bị bỏ · 1 mất tiến độ · 2 nên xem · 1 chưa kiểm", check.BadgeTooltip);
        }

        [Test]
        public void StaleCheck_NoBlockedGate_BlockerRemindsF5()
        {
            List<FakeHubSection> sections = FakeHubSection.CreateRegistryShaped();
            sections[4].Health = SectionHealth.Blocked("2 bị bỏ", "2 đợt sẽ bị game bỏ khi đọc lịch").AsStale(CheckedAtUtc);

            LiveOpsHubRailModel model = Build(sections, DesignSummary(), true);

            Assert.AreEqual(-1, model.FirstBlockedGateIndex, "Kiểm lịch cũ là NotMeasured, không còn tầng cổng Blocked");
            Assert.IsTrue(model.HasBlocker, "đợt bị bỏ của lần kiểm trước vẫn còn — ô chặn giữ lời nhắc F5 thay vì biến mất như đã ổn");
            Assert.AreEqual("Kết quả cũ có 2 đợt bị bỏ — F5 để kiểm lại.", model.BlockerDetail);
            Assert.AreEqual(LiveOpsHubNavigation.FilterDropped, model.BlockerNavigation.FilterConsequence);
        }

        [Test]
        public void NotMeasuredStage_BadgeIsChuaKiem_TooltipIsReason()
        {
            List<FakeHubSection> sections = FakeHubSection.CreateRegistryShaped();
            sections[5].Health = SectionHealth.NotMeasured("Chưa kiểm lần nào — F5 để kiểm");

            LiveOpsHubRailStageRow export = Build(sections, null, false).Stages[3];

            Assert.AreEqual(HealthState.NotMeasured, export.State);
            Assert.AreEqual("chưa kiểm", export.Badge, "NotMeasured luôn ghi chữ, không bao giờ \"—\"");
            Assert.AreEqual("Chưa kiểm lần nào — F5 để kiểm", export.BadgeTooltip);
            Assert.AreEqual("Chưa kiểm lần nào — F5 để kiểm", export.Rows[0].Tooltip, "tooltip hàng NotMeasured luôn là lý do");
            Assert.IsFalse(export.Rows[0].IsMarkHidden);
        }

        [Test]
        public void OkStage_NoBadge_OkRowHidesMark()
        {
            LiveOpsHubRailModel model = Build(FakeHubSection.CreateRegistryShaped(), null, false);
            LiveOpsHubRailStageRow configure = model.Stages[0];

            Assert.AreEqual(HealthState.Ok, configure.State);
            Assert.AreEqual(string.Empty, configure.Badge, "Ok không có badge");
            Assert.IsTrue(configure.Rows[0].IsMarkHidden, "màn Ok ẩn dấu");
            Assert.AreEqual(LiveOpsHubStrings.ShellOverviewTitle, configure.Rows[0].Title, "ẩn dấu nhưng giữ nhãn");
            Assert.AreEqual(LiveOpsHubStrings.ShellOverviewSubtitle, configure.Rows[0].Tooltip, "không có lý do thì tooltip là subtitle");
        }

        [Test]
        public void ScheduleStage_BadgeFromWorstSection_TooltipJoinsReasons()
        {
            LiveOpsHubRailStageRow schedule = Build(FakeHubSection.CreateDesignSampleShaped(), DesignSummary(), false).Stages[1];

            Assert.AreEqual(HealthState.Blocked, schedule.State, "dấu tầng = mức nặng nhất các màn trong tầng");
            Assert.AreEqual("2 bị bỏ", schedule.Badge, "badge tầng làm việc lấy từ màn mang mức nặng nhất");
            Assert.AreEqual(HealthState.Blocked, schedule.BadgeState);
            StringAssert.Contains("hunt-0916-bonus", schedule.BadgeTooltip);
            StringAssert.Contains("weekly-pass đổi tiền tố", schedule.BadgeTooltip);
        }

        [Test]
        public void CheckStage_WithSummary_BadgeWorstCountFirst()
        {
            LiveOpsHubRailStageRow check = Build(FakeHubSection.CreateDesignSampleShaped(), DesignSummary(), false).Stages[2];

            Assert.AreEqual("2 bị bỏ", check.Badge);
            Assert.AreEqual("2 bị bỏ · 1 mất tiến độ · 2 nên xem · 1 chưa kiểm", check.BadgeTooltip, "tooltip liệt kê đủ, nặng nhất trước, bỏ số 0");
        }

        [Test]
        public void RunStageWithoutSections_NotDrawn_StagesInPipelineOrder()
        {
            List<FakeHubSection> sections = FakeHubSection.CreateRegistryShaped();
            sections.Reverse();

            LiveOpsHubRailModel model = Build(sections, null, false);

            CollectionAssert.AreEqual(new[] { PipelineStage.Configure, PipelineStage.Schedule, PipelineStage.Check, PipelineStage.Export },
                StagesOf(model), "thứ tự tầng theo PipelineStages.All, không theo thứ tự registry");
        }

        [Test]
        public void RailLabel_UsedInsteadOfTitle()
        {
            List<IHubSection> sections = new List<IHubSection>(FakeHubSection.AsSections(FakeHubSection.CreateRegistryShaped()));
            sections[0] = new LabeledSection();

            LiveOpsHubRailModel model = LiveOpsHubRailModel.Build(sections, HealthsOf(sections), null, false);

            Assert.AreEqual("Nhãn ngắn", model.Stages[0].Rows[0].Title);
        }

        [Test]
        public void HealthCountMismatch_Throws()
        {
            List<FakeHubSection> sections = FakeHubSection.CreateRegistryShaped();
            Assert.Throws<ArgumentException>(() => LiveOpsHubRailModel.Build(FakeHubSection.AsSections(sections), new List<SectionHealth>(), null, false),
                "health lệch số màn là lỗi lập trình — rail không được tự gán nhầm health cho màn khác");
        }

        [Test]
        public void CountParts_SkipsZeroes()
        {
            LiveEventCalendarCheckSummary onlyReview = LiveEventCalendarCheckSummary.From(new[]
            {
                LiveEventCalendarRuleResult.Found(LiveEventCalendarRuleIds.ConfigKeyMissing, new[] { ShouldReviewFinding("hunt-0914") }),
            });

            CollectionAssert.AreEqual(new[] { "1 nên xem" }, LiveOpsHubRailModel.CountParts(onlyReview));
            CollectionAssert.IsEmpty(LiveOpsHubRailModel.CountParts(null));
        }

        // ------------------------------------------------------------------------------------------------------------ dựng dữ liệu

        private static LiveOpsHubRailModel Build(IEnumerable<FakeHubSection> fakes, LiveEventCalendarCheckSummary summary, bool isCheckStale)
        {
            IReadOnlyList<IHubSection> sections = FakeHubSection.AsSections(fakes);
            return LiveOpsHubRailModel.Build(sections, HealthsOf(sections), summary, isCheckStale);
        }

        private static List<SectionHealth> HealthsOf(IReadOnlyList<IHubSection> sections)
        {
            List<SectionHealth> healths = new List<SectionHealth>();
            foreach (IHubSection section in sections) healths.Add(section.GetHealth());
            return healths;
        }

        private static List<PipelineStage> StagesOf(LiveOpsHubRailModel model)
        {
            List<PipelineStage> stages = new List<PipelineStage>();
            foreach (LiveOpsHubRailStageRow stage in model.Stages) stages.Add(stage.Stage);
            return stages;
        }

        /// <summary>Số của Hình 4: 2 bị bỏ · 1 mất tiến độ · 2 nên xem · 1 luật chưa kiểm (bản remote).</summary>
        internal static LiveEventCalendarCheckSummary DesignSummary()
        {
            return LiveEventCalendarCheckSummary.From(new[]
            {
                LiveEventCalendarRuleResult.Found(LiveEventCalendarRuleIds.OverlapSameType, new[]
                {
                    new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.OverlapSameType, LiveEventCalendarDetailCodes.OverlapKeptEarlier,
                        LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, "hunt-0916-bonus").Build(),
                }),
                LiveEventCalendarRuleResult.Found(LiveEventCalendarRuleIds.EndBeforeStart, new[]
                {
                    new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.EndBeforeStart, LiveEventCalendarDetailCodes.EndBeforeStart,
                        LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, "lava-quest-2026-10").Build(),
                }),
                LiveEventCalendarRuleResult.Found(LiveEventCalendarRuleIds.RunningEventIdChanged, new[]
                {
                    new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.RunningEventIdChanged, LiveEventCalendarDetailCodes.RunningIdChanged,
                        LiveEventCalendarConsequence.ProgressLost, LiveEventCalendarTargetKind.RecurringRule, "weekly-pass").Build(),
                }),
                LiveEventCalendarRuleResult.Found(LiveEventCalendarRuleIds.ConfigKeyMissing, new[] { ShouldReviewFinding("hunt-0914"), ShouldReviewFinding("lava-quest") }),
                LiveEventCalendarRuleResult.NotMeasured(LiveEventCalendarRuleIds.RemoteSnapshotDrift, "no-remote"),
                LiveEventCalendarRuleResult.Passed(LiveEventCalendarRuleIds.UtcTimeFormat),
            });
        }

        private static LiveEventCalendarFinding ShouldReviewFinding(string targetId)
        {
            return new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.ConfigKeyMissing, LiveEventCalendarDetailCodes.ConfigKeyInheritedDiffers,
                LiveEventCalendarConsequence.ShouldReview, LiveEventCalendarTargetKind.FixedEvent, targetId).Build();
        }

        private sealed class LabeledSection : IHubSection, IHubRailLabel
        {
            public string Id => LiveOpsHubSections.Ids.Overview;
            public string Title => "Tiêu đề rất dài không vừa cột rail";
            public string Subtitle => "Mô tả";
            public PipelineStage Stage => PipelineStage.Configure;
            public string RailLabel => "Nhãn ngắn";
            public IReadOnlyList<string> RequiredElementNames => Array.Empty<string>();
            public SectionHealth GetHealth() => SectionHealth.Ok();
            public UnityEngine.UIElements.VisualElement CreateView() => new UnityEngine.UIElements.VisualElement();
            public void OnShown() { }
        }
    }
}
