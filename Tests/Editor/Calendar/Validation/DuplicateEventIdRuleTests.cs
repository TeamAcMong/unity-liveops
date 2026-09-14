using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class DuplicateEventIdRuleTests
    {
        [Test]
        public void Passes_WhenConditionAbsent()
        {
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed,
                ValidationTestFixtures.Evaluate(new DuplicateEventIdRule(), LiveOpsDesignSample.Document).Outcome);
        }

        [Test]
        public void Finds_LaterEntryInExportOrder_RelatedIsKept_SuggestsRename()
        {
            // Thứ tự asset: đợt 20/9 trước đợt 10/9 — thứ tự xuất (game nhận) ngược lại, nên đợt 20/9 mới là đợt bị bỏ (V-6).
            FixedLiveEventEntry later = ValidationTestFixtures.Entry("hunt-a", "hunt", "2026-09-20T00:00:00Z", "2026-09-21T00:00:00Z");
            FixedLiveEventEntry earlier = ValidationTestFixtures.Entry("hunt-a", "hunt", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z");
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(later, earlier);

            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new DuplicateEventIdRule(), document);

            Assert.AreEqual(1, result.Findings.Count);
            LiveEventCalendarFinding finding = result.Findings[0];
            Assert.AreEqual(later.EntryKey, finding.TargetEntryKey);
            Assert.AreEqual(LiveEventCalendarDetailCodes.DuplicateFixedId, finding.DetailCode);
            Assert.AreEqual("hunt-a", finding.RelatedId);
            Assert.AreEqual("hunt-a-2", finding.ExpectedText);
            Assert.AreEqual(DuplicateEventIdRule.RenameRepairId, finding.Repairs[0].RepairId);

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, finding.Repairs[0].Edit, out LiveEventCalendarDocument repaired));
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new DuplicateEventIdRule(), repaired).Outcome);
        }

        [Test]
        public void SuggestedId_SkipsExistingIds()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("hunt-a", "hunt", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z"),
                ValidationTestFixtures.Entry("hunt-a-2", "hunt", "2026-09-12T00:00:00Z", "2026-09-13T00:00:00Z"),
                ValidationTestFixtures.Entry("hunt-a", "hunt", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"),
                ValidationTestFixtures.Entry("hunt-a", "hunt", "2026-09-16T00:00:00Z", "2026-09-17T00:00:00Z"));

            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new DuplicateEventIdRule(), document);

            Assert.AreEqual(2, result.Findings.Count);
            Assert.AreEqual("hunt-a-3", result.Findings[0].ExpectedText);
            Assert.AreEqual("hunt-a-4", result.Findings[1].ExpectedText, "Hai đợt trùng không được đề xuất cùng một id mới.");
        }

        [Test]
        public void DuplicateOfDroppedEarlierEntry_NotDuplicate()
        {
            // Đợt đứng trước bị bỏ vì loại sai quy tắc → runtime không bao giờ thấy nó, đợt sau cùng id được giữ (V-7).
            FixedLiveEventEntry droppedEarlier = ValidationTestFixtures.Entry("reused", "bad#type", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z");
            FixedLiveEventEntry laterSameId = ValidationTestFixtures.Entry("reused", "hunt", "2026-09-12T00:00:00Z", "2026-09-13T00:00:00Z");
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(droppedEarlier, laterSameId);
            LiveEventCalendarCheckContext context = ValidationTestFixtures.ContextFor(document);

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, new DuplicateEventIdRule().Evaluate(context).Outcome);
            Assert.IsTrue(context.Compilation.TryGetFixedOutcome(laterSameId.EntryKey, out LiveEventCalendarEntryOutcome outcome));
            Assert.IsTrue(outcome.IsKept);
            Assert.AreEqual(1, new InvalidIdentifierRule().Evaluate(context).Findings.Count, "Đợt hỏng có phát hiện riêng của luật 3.");
        }

        [Test]
        public void DuplicateAndOverlap_OnlyDuplicateFinds()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("hunt-a", "hunt", "2026-09-10T00:00:00Z", "2026-09-12T00:00:00Z"),
                ValidationTestFixtures.Entry("hunt-a", "hunt", "2026-09-11T00:00:00Z", "2026-09-13T00:00:00Z"));

            Assert.AreEqual(1, ValidationTestFixtures.Evaluate(new DuplicateEventIdRule(), document).Findings.Count);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new OverlapSameTypeRule(), document).Outcome,
                "V-7: trùng id là lý do chính, không kiểm chồng giờ nữa.");
        }

        [Test]
        public void Finding_HasTargetAndRepairKind()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("hunt-a", "hunt", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z"),
                ValidationTestFixtures.Entry("hunt-a", "hunt", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"));

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new DuplicateEventIdRule(), document).Findings[0];

            Assert.AreEqual(LiveEventCalendarTargetKind.FixedEvent, finding.TargetKind);
            Assert.AreEqual("hunt-a", finding.TargetId);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, finding.Consequence);
            Assert.AreEqual(LiveEventCalendarRepairKind.Proposal, finding.RepairKind);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-14T00:00:00Z"), finding.RangeStartUtc);
        }
    }
}
