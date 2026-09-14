using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class UtcTimeFormatRuleTests
    {
        [Test]
        public void Passes_WhenConditionAbsent()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("a", "hunt", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"));

            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new UtcTimeFormatRule(), document);

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, result.Outcome);
            Assert.AreEqual(0, result.Findings.Count);
        }

        [Test]
        public void Finds_DesignSampleCase()
        {
            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new UtcTimeFormatRule(), LiveOpsDesignSample.Document);

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, result.Outcome);
            Assert.AreEqual(1, result.Findings.Count);
            LiveEventCalendarFinding finding = result.Findings[0];
            Assert.AreEqual("lava-quest-2026-10", finding.TargetId);
            Assert.AreEqual(LiveEventCalendarDetailCodes.EndUnreadable, finding.DetailCode);
            Assert.AreEqual("2026-10-3", finding.FoundText, "tìm thấy \"2026-10-3\" [SD2 §2.3 hàng 1].");
            Assert.AreEqual("2026-10-03T00:00:00Z", finding.ExpectedText, "cần 2026-10-03T00:00:00Z.");
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-10-01T00:00:00Z"), finding.AnchorUtc);
        }

        [Test]
        public void Finding_HasTargetAndRepairKind()
        {
            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new UtcTimeFormatRule(), LiveOpsDesignSample.Document).Findings[0];

            Assert.AreEqual(LiveEventCalendarTargetKind.FixedEvent, finding.TargetKind);
            Assert.AreEqual(LiveOpsDesignSample.LavaQuestLateEntryKey, finding.TargetEntryKey);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, finding.Consequence);
            Assert.AreEqual(LiveEventCalendarRepairKind.SafeRepair, finding.RepairKind);
            Assert.AreEqual(1, finding.Repairs.Count);
            Assert.AreEqual(UtcTimeFormatRule.NormalizeRepairId, finding.Repairs[0].RepairId);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-10-03T00:00:00Z"), finding.Repairs[0].NewEndUtc);
        }

        [Test]
        public void SafeRepair_AppliedDocument_RulePasses()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            LiveEventCalendarRepair repair = ValidationTestFixtures.Evaluate(new UtcTimeFormatRule(), document).Findings[0].Repairs[0];

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, repair.Edit, out LiveEventCalendarDocument repaired));
            Assert.IsTrue(repaired.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestLateEntryKey, out FixedLiveEventEntry entry));
            Assert.AreEqual("2026-10-03T00:00:00Z", entry.EndUtcText);
            Assert.AreEqual("2026-10-01T00:00:00Z", entry.StartUtcText, "Phía đọc được giữ nguyên văn.");
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new UtcTimeFormatRule(), repaired).Outcome);
        }

        [Test]
        public void UtcTimeFormat_Ambiguous_NoSafeRepair()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("a", "hunt", "2026-09-14T00:00:00Z", "3/10/2026"));

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new UtcTimeFormatRule(), document).Findings[0];

            Assert.AreEqual(LiveEventCalendarRepairKind.None, finding.RepairKind);
            Assert.AreEqual(0, finding.Repairs.Count);
            Assert.AreEqual(UtcTimeFormatRule.CanonicalPatternText, finding.ExpectedText);
        }

        [Test]
        public void BothTimesBroken_OneFindingListsBoth()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("a", "hunt", "2026-9-1", "2026-9-3"));

            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new UtcTimeFormatRule(), document);

            Assert.AreEqual(1, result.Findings.Count, "Một đợt hỏng cả hai giờ → một phát hiện.");
            LiveEventCalendarFinding finding = result.Findings[0];
            Assert.AreEqual(LiveEventCalendarDetailCodes.StartUnreadable, finding.DetailCode, "Mã theo lý do chính (giờ bắt đầu kiểm trước).");
            Assert.AreEqual("2026-9-1" + LiveEventCalendarFindingBuilder.ValueSeparator + "2026-9-3", finding.FoundText);
            Assert.AreEqual(LiveEventCalendarRepairKind.SafeRepair, finding.RepairKind);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-01T00:00:00Z"), finding.Repairs[0].NewStartUtc);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-03T00:00:00Z"), finding.Repairs[0].NewEndUtc);
        }

        [Test]
        public void OneSideAmbiguous_NoSafeRepair()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("a", "hunt", "2026-9-1", "3/9/2026"));

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new UtcTimeFormatRule(), document).Findings[0];

            Assert.AreEqual(LiveEventCalendarRepairKind.None, finding.RepairKind, "Sửa một nửa vẫn để đợt bị bỏ — không phải Sửa an toàn.");
        }

        [Test]
        public void BrokenTimeAndHashId_OnlyThisRuleFinds()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("a#b", "hunt", "2026-09-14T00:00:00Z", "2026-9-15"));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, ValidationTestFixtures.Evaluate(new UtcTimeFormatRule(), document).Outcome);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new InvalidIdentifierRule(), document).Outcome,
                "V-7: lỗi id là lỗi phụ của mục, không thành phát hiện riêng.");
        }
    }
}
