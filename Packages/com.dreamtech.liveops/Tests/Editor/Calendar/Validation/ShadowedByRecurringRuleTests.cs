using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class ShadowedByRecurringRuleTests
    {
        [Test]
        public void Passes_WhenConditionAbsent()
        {
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed,
                ValidationTestFixtures.Evaluate(new ShadowedByRecurringRule(), LiveOpsDesignSample.Document).Outcome);
        }

        [Test]
        public void FixedOverlapsOccurrence_DetailOverlapOccurrence()
        {
            IReadOnlyList<LiveEventCalendarFinding> findings =
                ValidationTestFixtures.Evaluate(new ShadowedByRecurringRule(), ValidationTestFixtures.ShadowedDocument()).Findings;

            LiveEventCalendarFinding finding = FindingFor(findings, "race-special");
            Assert.AreEqual(LiveEventCalendarDetailCodes.ShadowedOverlap, finding.DetailCode);
            Assert.AreEqual("race-252", finding.RelatedId, "Lần lặp thứ 252 (14/9 00:00 → 20:00) che đợt 01:00 → 02:00.");
        }

        [Test]
        public void FixedIdCollidesWithOccurrence_DetailIdCollision()
        {
            IReadOnlyList<LiveEventCalendarFinding> findings =
                ValidationTestFixtures.Evaluate(new ShadowedByRecurringRule(), ValidationTestFixtures.ShadowedDocument()).Findings;

            LiveEventCalendarFinding finding = FindingFor(findings, "race-252");
            Assert.AreEqual(LiveEventCalendarDetailCodes.ShadowedIdCollision, finding.DetailCode, "21:00 → 22:00 không chồng lần lặp nhưng trùng id.");
            Assert.AreEqual(2, findings.Count);
        }

        [Test]
        public void FixedOfTypeWithoutRule_NotShadowed()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.Document(
                new[] { new RecurringLiveEventRule("race", "2026-01-05T00:00:00Z", string.Empty, 24, 20, string.Empty) },
                ValidationTestFixtures.Entry("hunt-1", "hunt", "2026-09-14T01:00:00Z", "2026-09-14T02:00:00Z"));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new ShadowedByRecurringRule(), document).Outcome);
        }

        [Test]
        public void Finding_HasTargetAndRepairKind()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.ShadowedDocument();
            LiveEventCalendarFinding finding = FindingFor(ValidationTestFixtures.Evaluate(new ShadowedByRecurringRule(), document).Findings, "race-special");

            Assert.AreEqual(LiveEventCalendarTargetKind.FixedEvent, finding.TargetKind);
            Assert.AreEqual(document.FixedEvents[0].EntryKey, finding.TargetEntryKey);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, finding.Consequence);
            Assert.AreEqual(LiveEventCalendarRepairKind.None, finding.RepairKind, "Gỡ che là đổi ý đồ lịch — Editor chỉ mở luật.");
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-14T01:00:00Z"), finding.RangeStartUtc);
        }

        private static LiveEventCalendarFinding FindingFor(IReadOnlyList<LiveEventCalendarFinding> findings, string targetId)
        {
            for (int index = 0; index < findings.Count; index++)
            {
                if (findings[index].TargetId == targetId) return findings[index];
            }
            Assert.Fail("Không có phát hiện cho " + targetId);
            return null;
        }
    }
}
