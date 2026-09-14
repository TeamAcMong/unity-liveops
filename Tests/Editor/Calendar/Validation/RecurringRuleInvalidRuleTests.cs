using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class RecurringRuleInvalidRuleTests
    {
        private const string Anchor = "2026-01-05T00:00:00Z";

        [Test]
        public void Passes_WhenConditionAbsent()
        {
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed,
                ValidationTestFixtures.Evaluate(new RecurringRuleInvalidRule(), LiveOpsDesignSample.Document).Outcome);
        }

        [Test]
        public void ActiveLongerThanPeriod_ProposalSetsActiveToPeriod()
        {
            LiveEventCalendarDocument document = RulesOnly(new RecurringLiveEventRule("race", Anchor, string.Empty, 24, 30, string.Empty));

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new RecurringRuleInvalidRule(), document).Findings[0];

            Assert.AreEqual(LiveEventCalendarDetailCodes.ActiveLongerThanPeriod, finding.DetailCode);
            Assert.AreEqual("30", finding.FoundText);
            Assert.AreEqual("24", finding.ExpectedText);
            Assert.AreEqual(LiveEventCalendarRepairKind.Proposal, finding.RepairKind);
            Assert.AreEqual(RecurringRuleInvalidRule.SetActiveToPeriodRepairId, finding.Repairs[0].RepairId);

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, finding.Repairs[0].Edit, out LiveEventCalendarDocument repaired));
            Assert.AreEqual(24, repaired.RecurringRules[0].ActiveHours);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new RecurringRuleInvalidRule(), repaired).Outcome);
        }

        [Test]
        public void AnchorNormalizable_SafeRepair()
        {
            LiveEventCalendarDocument document = RulesOnly(new RecurringLiveEventRule("race", "2026-1-5", string.Empty, 24, 20, string.Empty));

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new RecurringRuleInvalidRule(), document).Findings[0];

            Assert.AreEqual(LiveEventCalendarDetailCodes.AnchorUnreadable, finding.DetailCode);
            Assert.AreEqual("2026-01-05T00:00:00Z", finding.ExpectedText);
            Assert.AreEqual(LiveEventCalendarRepairKind.SafeRepair, finding.RepairKind);
            Assert.AreEqual(UtcTimeFormatRule.NormalizeRepairId, finding.Repairs[0].RepairId);
            Assert.AreEqual(ValidationTestFixtures.Utc(Anchor), finding.AnchorUtc);

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, finding.Repairs[0].Edit, out LiveEventCalendarDocument repaired));
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new RecurringRuleInvalidRule(), repaired).Outcome);
        }

        [Test]
        public void AnchorAmbiguous_NoRepair()
        {
            LiveEventCalendarDocument document = RulesOnly(new RecurringLiveEventRule("race", "5/1/2026", string.Empty, 24, 20, string.Empty));

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new RecurringRuleInvalidRule(), document).Findings[0];

            Assert.AreEqual(LiveEventCalendarDetailCodes.AnchorUnreadable, finding.DetailCode);
            Assert.AreEqual(LiveEventCalendarRepairKind.None, finding.RepairKind);
            Assert.IsNull(finding.AnchorUtc);
        }

        [Test]
        public void EachDefect_HasItsDetailCode()
        {
            LiveEventCalendarDocument document = RulesOnly(
                new RecurringLiveEventRule("period-zero", Anchor, string.Empty, 0, 20, string.Empty),
                new RecurringLiveEventRule("active-zero", Anchor, string.Empty, 24, 0, string.Empty),
                new RecurringLiveEventRule("prefix-broken", Anchor, "bad#", 24, 20, string.Empty),
                new RecurringLiveEventRule("type#broken", Anchor, "ok-", 24, 20, string.Empty));

            IReadOnlyList<LiveEventCalendarFinding> findings = ValidationTestFixtures.Evaluate(new RecurringRuleInvalidRule(), document).Findings;

            Assert.AreEqual(4, findings.Count);
            Assert.AreEqual(LiveEventCalendarDetailCodes.PeriodNotPositive, findings[0].DetailCode);
            Assert.AreEqual(LiveEventCalendarDetailCodes.ActiveNotPositive, findings[1].DetailCode);
            Assert.AreEqual(LiveEventCalendarDetailCodes.PrefixInvalid, findings[2].DetailCode);
            Assert.AreEqual("bad#", findings[2].FoundText);
            Assert.AreEqual(LiveEventCalendarDetailCodes.TypeInvalid, findings[3].DetailCode);
        }

        [Test]
        public void SecondRuleSameType_DuplicateType_FirstKept()
        {
            LiveEventCalendarDocument document = RulesOnly(
                new RecurringLiveEventRule("race", Anchor, string.Empty, 24, 20, string.Empty),
                new RecurringLiveEventRule("race", Anchor, "race2-", 48, 20, string.Empty));

            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new RecurringRuleInvalidRule(), document);

            Assert.AreEqual(1, result.Findings.Count, "Luật đứng trước được giữ — chỉ luật thứ hai có phát hiện.");
            Assert.AreEqual(LiveEventCalendarDetailCodes.DuplicateRuleForType, result.Findings[0].DetailCode);
        }

        [Test]
        public void Finding_HasTargetAndRepairKind()
        {
            LiveEventCalendarDocument document = RulesOnly(new RecurringLiveEventRule("race", Anchor, string.Empty, 0, 20, string.Empty));

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new RecurringRuleInvalidRule(), document).Findings[0];

            Assert.AreEqual(LiveEventCalendarTargetKind.RecurringRule, finding.TargetKind);
            Assert.AreEqual("race", finding.TargetId);
            Assert.AreEqual("race", finding.TargetEntryKey, "Đích của luật lặp là loại của luật.");
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, finding.Consequence);
            Assert.AreEqual(LiveEventCalendarRepairKind.None, finding.RepairKind);
        }

        private static LiveEventCalendarDocument RulesOnly(params RecurringLiveEventRule[] rules)
        {
            return ValidationTestFixtures.Document(rules);
        }
    }
}
