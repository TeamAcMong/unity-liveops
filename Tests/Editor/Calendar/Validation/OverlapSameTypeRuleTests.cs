using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class OverlapSameTypeRuleTests
    {
        [Test]
        public void Passes_WhenConditionAbsent()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("hunt-a", "hunt", "2026-09-10T00:00:00Z", "2026-09-12T00:00:00Z"),
                ValidationTestFixtures.Entry("hunt-b", "hunt", "2026-09-12T00:00:00Z", "2026-09-13T00:00:00Z"),
                ValidationTestFixtures.Entry("race-a", "race", "2026-09-11T00:00:00Z", "2026-09-12T00:00:00Z"));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new OverlapSameTypeRule(), document).Outcome,
                "Chạm mép (kết thúc = bắt đầu) và khác loại không phải chồng giờ.");
        }

        [Test]
        public void Finds_DesignSampleCase()
        {
            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new OverlapSameTypeRule(), LiveOpsDesignSample.Document);

            Assert.AreEqual(1, result.Findings.Count);
            LiveEventCalendarFinding finding = result.Findings[0];
            Assert.AreEqual("hunt-0916-bonus", finding.TargetId);
            Assert.AreEqual("hunt-0914", finding.RelatedId);
            Assert.AreEqual(LiveEventCalendarDetailCodes.OverlapKeptEarlier, finding.DetailCode);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-16T12:00:00Z"), finding.RangeStartUtc, "Khoảng chồng bắt đầu ở đầu đợt bị bỏ.");
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-17T00:00:00Z"), finding.RangeEndUtc, "…và khép khi hunt-0914 khép.");
            Assert.AreEqual("2026-09-17T00:00:00Z", finding.ExpectedText);
        }

        [Test]
        public void OverlapSameType_TwoProposals_ComputeDesignTimes()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new OverlapSameTypeRule(), document).Findings[0];

            Assert.AreEqual(2, finding.Repairs.Count);

            LiveEventCalendarRepair shiftStart = finding.Repairs[0];
            Assert.AreEqual(OverlapSameTypeRule.ShiftStartKeepEndRepairId, shiftStart.RepairId);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-17T00:00:00Z"), shiftStart.NewStartUtc, "Dời tới 17/9 00:00…");
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-18T00:00:00Z"), shiftStart.NewEndUtc, "…còn 24 giờ.");

            LiveEventCalendarRepair shiftWhole = finding.Repairs[1];
            Assert.AreEqual(OverlapSameTypeRule.ShiftWholeKeepDurationRepairId, shiftWhole.RepairId);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-17T00:00:00Z"), shiftWhole.NewStartUtc);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-18T12:00:00Z"), shiftWhole.NewEndUtc, "Giữ 36 giờ: tới 18/9 12:00.");

            for (int index = 0; index < finding.Repairs.Count; index++)
            {
                Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, finding.Repairs[index].Edit, out LiveEventCalendarDocument repaired));
                Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new OverlapSameTypeRule(), repaired).Outcome,
                    finding.Repairs[index].RepairId);
            }
        }

        [Test]
        public void EntryInsideKeptEvent_OnlyShiftWholeProposal()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("long", "hunt", "2026-09-01T00:00:00Z", "2026-09-10T00:00:00Z"),
                ValidationTestFixtures.Entry("inside", "hunt", "2026-09-02T00:00:00Z", "2026-09-03T00:00:00Z"));

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new OverlapSameTypeRule(), document).Findings[0];

            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-03T00:00:00Z"), finding.RangeEndUtc, "Khoảng chồng khép ở kết thúc của chính đợt này.");
            Assert.AreEqual(1, finding.Repairs.Count, "Dời bắt đầu tới 10/9 mà giữ kết thúc 3/9 ra đợt âm — không đề xuất.");
            Assert.AreEqual(OverlapSameTypeRule.ShiftWholeKeepDurationRepairId, finding.Repairs[0].RepairId);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-10T00:00:00Z"), finding.Repairs[0].NewStartUtc);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-11T00:00:00Z"), finding.Repairs[0].NewEndUtc);
        }

        [Test]
        public void Finding_HasTargetAndRepairKind()
        {
            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new OverlapSameTypeRule(), LiveOpsDesignSample.Document).Findings[0];

            Assert.AreEqual(LiveEventCalendarTargetKind.FixedEvent, finding.TargetKind);
            Assert.AreEqual(LiveOpsDesignSample.HuntBonusEntryKey, finding.TargetEntryKey);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, finding.Consequence);
            Assert.AreEqual(LiveEventCalendarRepairKind.Proposal, finding.RepairKind);
        }
    }
}
