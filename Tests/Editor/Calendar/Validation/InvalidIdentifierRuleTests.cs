using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class InvalidIdentifierRuleTests
    {
        [Test]
        public void Passes_WhenConditionAbsent()
        {
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed,
                ValidationTestFixtures.Evaluate(new InvalidIdentifierRule(), LiveOpsDesignSample.Document).Outcome);
        }

        [Test]
        public void Finds_HashInEventId()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("hunt#1", "hunt", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"));

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new InvalidIdentifierRule(), document).Findings[0];

            Assert.AreEqual(LiveEventCalendarDetailCodes.HashInIdentifier, finding.DetailCode);
            Assert.AreEqual("hunt#1", finding.FoundText);
            Assert.AreEqual(InvalidIdentifierRule.EventIdFieldName, finding.ExpectedText);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-14T00:00:00Z"), finding.AnchorUtc);
        }

        [Test]
        public void EmptyEventId_DetailEmpty()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry(string.Empty, "hunt", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"));

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new InvalidIdentifierRule(), document).Findings[0];

            Assert.AreEqual(LiveEventCalendarDetailCodes.EmptyIdentifier, finding.DetailCode);
            Assert.AreEqual(string.Empty, finding.TargetId);
        }

        [Test]
        public void NewlineInEventType_DetailNewline_FieldIsType()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("hunt-1", "hu\nnt", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"));

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new InvalidIdentifierRule(), document).Findings[0];

            Assert.AreEqual(LiveEventCalendarDetailCodes.NewlineInIdentifier, finding.DetailCode);
            Assert.AreEqual("hu\nnt", finding.FoundText);
            Assert.AreEqual(InvalidIdentifierRule.EventTypeFieldName, finding.ExpectedText);
        }

        [Test]
        public void BothIdAndTypeInvalid_ReportsIdFirst()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry(string.Empty, "bad#type", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"));

            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new InvalidIdentifierRule(), document);

            Assert.AreEqual(1, result.Findings.Count);
            Assert.AreEqual(LiveEventCalendarDetailCodes.EmptyIdentifier, result.Findings[0].DetailCode, "Cùng thứ tự với ctor LiveEventInstance.");
        }

        [Test]
        public void Finding_HasTargetAndRepairKind()
        {
            FixedLiveEventEntry entry = ValidationTestFixtures.Entry("hunt#1", "hunt", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z");
            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new InvalidIdentifierRule(),
                ValidationTestFixtures.FixedDocument(entry)).Findings[0];

            Assert.AreEqual(LiveEventCalendarTargetKind.FixedEvent, finding.TargetKind);
            Assert.AreEqual(entry.EntryKey, finding.TargetEntryKey);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, finding.Consequence);
            Assert.AreEqual(LiveEventCalendarRepairKind.None, finding.RepairKind, "Id mới là quyết định của người dùng — sửa ở inspector.");
            Assert.AreEqual(0, finding.Repairs.Count);
        }
    }
}
