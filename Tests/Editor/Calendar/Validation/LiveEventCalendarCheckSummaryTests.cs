using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class LiveEventCalendarCheckSummaryTests
    {
        [Test]
        public void From_EmptyResults_AllZero_WorstNull()
        {
            LiveEventCalendarCheckSummary summary = LiveEventCalendarCheckSummary.From(Array.Empty<LiveEventCalendarRuleResult>());

            Assert.AreEqual(0, summary.RuleCount);
            Assert.AreEqual(0, summary.NeedsActionCount);
            Assert.AreEqual(0, summary.PassedRuleCount);
            Assert.IsNull(summary.WorstConsequence);
        }

        [Test]
        public void From_CountsRuleOutcomes_FailedIsNotMeasured()
        {
            LiveEventCalendarCheckSummary summary = LiveEventCalendarCheckSummary.From(new[]
            {
                LiveEventCalendarRuleResult.Passed(LiveEventCalendarRuleIds.UtcTimeFormat),
                LiveEventCalendarRuleResult.Passed(LiveEventCalendarRuleIds.EndBeforeStart),
                LiveEventCalendarRuleResult.NotMeasured(LiveEventCalendarRuleIds.RemoteSnapshotDrift, "remote-not-pasted"),
                LiveEventCalendarRuleResult.Failed(LiveEventCalendarRuleIds.LongGapBetweenEvents, "NullReferenceException", "hỏng"),
                LiveEventCalendarRuleResult.NotApplicable(LiveEventCalendarRuleIds.RunningEventIdChanged, "no-published-stamp"),
            });

            Assert.AreEqual(5, summary.RuleCount);
            Assert.AreEqual(2, summary.PassedRuleCount);
            Assert.AreEqual(2, summary.NotMeasuredRuleCount, "NotMeasured + Failed — luật ném cũng là chưa kiểm được.");
            Assert.AreEqual(1, summary.NotApplicableRuleCount);
            Assert.IsNull(summary.WorstConsequence);
        }

        [Test]
        public void From_CountsFindingsByConsequence_NeedsActionIsSum()
        {
            LiveEventCalendarCheckSummary summary = LiveEventCalendarCheckSummary.From(new[]
            {
                Found(LiveEventCalendarRuleIds.UtcTimeFormat, Dropped("a"), Dropped("b")),
                Found(LiveEventCalendarRuleIds.RunningEventIdChanged, ProgressLost("weekly-pass")),
                Found(LiveEventCalendarRuleIds.LongGapBetweenEvents, ShouldReview("lava-quest"), ShouldReview("treasure-hunt")),
            });

            Assert.AreEqual(2, summary.DroppedCount);
            Assert.AreEqual(1, summary.ProgressLostCount);
            Assert.AreEqual(2, summary.ShouldReviewCount);
            Assert.AreEqual(5, summary.NeedsActionCount);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, summary.WorstConsequence);
        }

        [Test]
        public void From_RemoteSnapshotFindings_NotInDroppedOrNeedsAction()
        {
            LiveEventCalendarFinding remoteFinding = new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.UnknownEventType,
                    LiveEventCalendarDetailCodes.UnknownTypeInRemote, LiveEventCalendarConsequence.ShouldReview, LiveEventCalendarTargetKind.EventType, "gold-rush")
                .WithRemoteSnapshotSubject(true)
                .Build();

            LiveEventCalendarCheckSummary summary = LiveEventCalendarCheckSummary.From(new[]
            {
                Found(LiveEventCalendarRuleIds.UnknownEventType, remoteFinding),
            });

            Assert.AreEqual(1, summary.RemoteSnapshotFindingCount);
            Assert.AreEqual(0, summary.DroppedCount);
            Assert.AreEqual(0, summary.ShouldReviewCount);
            Assert.AreEqual(0, summary.NeedsActionCount, "V-17: phát hiện về bản remote không chặn Copy JSON của nháp.");
            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, summary.WorstConsequence);
        }

        [Test]
        public void From_IgnoredFindings_CountedSeparately_NotInNeedsActionOrWorst()
        {
            var warning = new IgnoredCalendarWarning(LiveEventCalendarRuleIds.LongGapBetweenEvents, "lava-quest", string.Empty, string.Empty, "nghỉ", string.Empty);
            LiveEventCalendarFinding ignored = ShouldReview("lava-quest").WithIgnoreState(warning, null);

            LiveEventCalendarCheckSummary summary = LiveEventCalendarCheckSummary.From(new[]
            {
                Found(LiveEventCalendarRuleIds.LongGapBetweenEvents, ignored),
            });

            Assert.AreEqual(1, summary.IgnoredCount);
            Assert.AreEqual(0, summary.ShouldReviewCount);
            Assert.AreEqual(0, summary.NeedsActionCount);
            Assert.IsNull(summary.WorstConsequence);
        }

        [Test]
        public void From_SafeRepairCount_OnlyDraftFindingsWithSafeRepair()
        {
            var repair = new LiveEventCalendarRepair("normalize-utc", LiveEventCalendarRepairKind.SafeRepair, new RemoveFixedEventEdit("key"),
                "2026-10-3", "2026-10-03T00:00:00Z", null, null);
            LiveEventCalendarFinding withSafeRepair = new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.UtcTimeFormat,
                    LiveEventCalendarDetailCodes.EndUnreadable, LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, "lava-quest-2026-10")
                .WithRepairs(LiveEventCalendarRepairKind.SafeRepair, new[] { repair })
                .Build();

            LiveEventCalendarCheckSummary summary = LiveEventCalendarCheckSummary.From(new[]
            {
                Found(LiveEventCalendarRuleIds.UtcTimeFormat, withSafeRepair),
                Found(LiveEventCalendarRuleIds.OverlapSameType, Dropped("hunt-0916-bonus")),
            });

            Assert.AreEqual(1, summary.SafeRepairCount);
            Assert.AreEqual(2, summary.DroppedCount);
        }

        [Test]
        public void DesignSample_DefaultValidator_Dropped2_SafeRepair1_Passed6()
        {
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(ValidationTestFixtures.ContextFor(LiveOpsDesignSample.Document));
            LiveEventCalendarCheckSummary summary = report.Summary;

            Assert.AreEqual(LiveEventCalendarValidator.RuleCount, summary.RuleCount);
            Assert.AreEqual(2, summary.DroppedCount, "utc-time-format · lava-quest-2026-10 và overlap-same-type · hunt-0916-bonus (mục 6.2).");
            Assert.AreEqual(1, summary.SafeRepairCount, "Chỉ lava-quest-2026-10 chuẩn hoá được — 'Sửa các lỗi an toàn (1)…'.");
            Assert.AreEqual(6, summary.PassedRuleCount, "Luật 2, 3, 4, 6, 7, 8 đã qua trên mẫu.");
            Assert.AreEqual(0, summary.RemoteSnapshotFindingCount);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, summary.WorstConsequence);
        }

        private static LiveEventCalendarRuleResult Found(string ruleId, params LiveEventCalendarFinding[] findings)
        {
            return LiveEventCalendarRuleResult.Found(ruleId, new List<LiveEventCalendarFinding>(findings));
        }

        private static LiveEventCalendarFinding Dropped(string targetId)
        {
            return new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.OverlapSameType, LiveEventCalendarDetailCodes.OverlapKeptEarlier,
                LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, targetId).Build();
        }

        private static LiveEventCalendarFinding ProgressLost(string targetId)
        {
            return new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.RunningEventIdChanged, LiveEventCalendarDetailCodes.RunningIdChanged,
                LiveEventCalendarConsequence.ProgressLost, LiveEventCalendarTargetKind.RecurringRule, targetId).Build();
        }

        private static LiveEventCalendarFinding ShouldReview(string targetId)
        {
            return new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.LongGapBetweenEvents, LiveEventCalendarDetailCodes.LongGap,
                    LiveEventCalendarConsequence.ShouldReview, LiveEventCalendarTargetKind.EventType, targetId)
                .WithRepairs(LiveEventCalendarRepairKind.Ignorable, null)
                .Build();
        }
    }
}
