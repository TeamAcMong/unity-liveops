using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class EndBeforeStartRuleTests
    {
        [Test]
        public void Passes_WhenConditionAbsent()
        {
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed,
                ValidationTestFixtures.Evaluate(new EndBeforeStartRule(), LiveOpsDesignSample.Document).Outcome);
        }

        [Test]
        public void Finds_EndBeforeStart_TwoProposals()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("backwards", "hunt", "2026-09-25T00:00:00Z", "2026-09-24T00:00:00Z"));

            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new EndBeforeStartRule(), document);

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, result.Outcome);
            LiveEventCalendarFinding finding = result.Findings[0];
            Assert.AreEqual(LiveEventCalendarDetailCodes.EndBeforeStart, finding.DetailCode);
            Assert.AreEqual(2, finding.Repairs.Count);
            Assert.AreEqual(EndBeforeStartRule.KeepStartSetDurationRepairId, finding.Repairs[0].RepairId);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-25T00:00:00Z"), finding.Repairs[0].NewStartUtc);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-26T00:00:00Z"), finding.Repairs[0].NewEndUtc);
            Assert.AreEqual(EndBeforeStartRule.SwapStartEndRepairId, finding.Repairs[1].RepairId);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-24T00:00:00Z"), finding.Repairs[1].NewStartUtc);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-25T00:00:00Z"), finding.Repairs[1].NewEndUtc);

            for (int index = 0; index < finding.Repairs.Count; index++)
            {
                Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, finding.Repairs[index].Edit, out LiveEventCalendarDocument repaired));
                Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new EndBeforeStartRule(), repaired).Outcome,
                    finding.Repairs[index].RepairId);
            }
        }

        [Test]
        public void EndEqualsStart_OnlyKeepStartProposal()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("zero", "hunt", "2026-09-25T00:00:00Z", "2026-09-25T00:00:00Z"));

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new EndBeforeStartRule(), document).Findings[0];

            Assert.AreEqual(LiveEventCalendarDetailCodes.EndEqualsStart, finding.DetailCode);
            Assert.AreEqual(1, finding.Repairs.Count, "Đổi chỗ hai giờ bằng nhau vẫn ra đợt dài 0.");
            Assert.AreEqual(EndBeforeStartRule.KeepStartSetDurationRepairId, finding.Repairs[0].RepairId);
        }

        [Test]
        public void Finding_HasTargetAndRepairKind()
        {
            FixedLiveEventEntry entry = ValidationTestFixtures.Entry("backwards", "hunt", "2026-09-25T00:00:00Z", "2026-09-24T00:00:00Z");
            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new EndBeforeStartRule(),
                ValidationTestFixtures.FixedDocument(entry)).Findings[0];

            Assert.AreEqual(LiveEventCalendarTargetKind.FixedEvent, finding.TargetKind);
            Assert.AreEqual("backwards", finding.TargetId);
            Assert.AreEqual(entry.EntryKey, finding.TargetEntryKey);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, finding.Consequence);
            Assert.AreEqual(LiveEventCalendarRepairKind.Proposal, finding.RepairKind);
        }

        [Test]
        public void HashIdAndEndBeforeStart_OnlyInvalidIdentifierFinds()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("a#b", "hunt", "2026-09-25T00:00:00Z", "2026-09-24T00:00:00Z"));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new EndBeforeStartRule(), document).Outcome,
                "V-7: lý do chính là id sai — giờ ngược chỉ là lỗi phụ.");
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, ValidationTestFixtures.Evaluate(new InvalidIdentifierRule(), document).Outcome);
        }
    }
}
