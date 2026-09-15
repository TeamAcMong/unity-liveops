using System;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class LongGapBetweenEventsRuleTests
    {
        [Test]
        public void Passes_WhenConditionAbsent()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-a", "quest", "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z"),
                ValidationTestFixtures.Entry("quest-b", "quest", "2026-09-16T00:00:00Z", "2026-09-20T00:00:00Z"));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new LongGapBetweenEventsRule(), document).Outcome);
        }

        [Test]
        public void Finds_DesignSampleCase()
        {
            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new LongGapBetweenEventsRule(), LiveOpsDesignSample.Document);

            Assert.AreEqual(1, result.Findings.Count, "lava-quest: 13/9 → 17/9 là 4 ngày (không); treasure-hunt không có đợt kế; star-tournament chỉ 1 đợt.");
            LiveEventCalendarFinding finding = result.Findings[0];
            Assert.AreEqual("lava-quest", finding.TargetId);
            Assert.AreEqual(LiveEventCalendarDetailCodes.LongGap, finding.DetailCode);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-20T00:00:00Z"), finding.RangeStartUtc, "\"lava-quest trống 11 ngày (20/9 → 1/10)\".");
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-10-01T00:00:00Z"), finding.RangeEndUtc);
            Assert.AreEqual(finding.RangeStartUtc, finding.AnchorUtc);
            Assert.AreEqual("lava-quest-2026-10", finding.RelatedId);
            Assert.AreEqual("2026-09-20T00:00:00Z" + LiveEventCalendarFindingBuilder.ValueSeparator + "2026-10-01T00:00:00Z", finding.FoundText);
        }

        [Test]
        public void Finding_HasTargetAndRepairKind()
        {
            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new LongGapBetweenEventsRule(), LiveOpsDesignSample.Document).Findings[0];

            Assert.AreEqual(LiveEventCalendarTargetKind.EventType, finding.TargetKind);
            Assert.AreEqual("lava-quest", finding.TargetEntryKey);
            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, finding.Consequence);
            Assert.AreEqual(LiveEventCalendarRepairKind.Ignorable, finding.RepairKind, "Bỏ qua cảnh báo với phạm vi = khoảng.");
            Assert.AreEqual(0, finding.Repairs.Count);
            Assert.AreEqual(LiveEventCalendarRuleIds.LongGapBetweenEvents + "|lava-quest|2026-09-20T00:00:00Z|2026-10-01T00:00:00Z", finding.Fingerprint);
        }

        [Test]
        public void LongGap_NextEventDroppedButStartReadable_UsesItsStart()
        {
            // V-1: lava-quest-2026-10 bị bỏ vì endUtc "2026-10-3" nhưng startUtc đọc được → vẫn là mốc kế.
            LiveEventCalendarCheckContext context = ValidationTestFixtures.ContextFor(LiveOpsDesignSample.Document);
            Assert.IsTrue(context.Compilation.TryGetFixedOutcome(LiveOpsDesignSample.LavaQuestLateEntryKey, out LiveEventCalendarEntryOutcome outcome));
            Assert.IsFalse(outcome.IsKept, "Tiền đề: đợt kế bị bỏ.");

            LiveEventCalendarFinding finding = new LongGapBetweenEventsRule().Evaluate(context).Findings[0];

            Assert.AreEqual(ValidationTestFixtures.Utc("2026-10-01T00:00:00Z"), finding.RangeEndUtc);
            Assert.AreEqual("lava-quest-2026-10", finding.RelatedId);
        }

        [Test]
        public void LongGap_NextEventStartUnreadable_NotABoundary()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-a", "quest", "2026-09-10T00:00:00Z", "2026-09-12T00:00:00Z"),
                // Không đọc được startUtc: không biết mở lúc nào → không cắt khoảng trống.
                ValidationTestFixtures.Entry("quest-b", "quest", "2026-9-15", "2026-09-16T00:00:00Z"),
                ValidationTestFixtures.Entry("quest-c", "quest", "2026-09-30T00:00:00Z", "2026-10-02T00:00:00Z"));

            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new LongGapBetweenEventsRule(), document);

            Assert.AreEqual(1, result.Findings.Count);
            Assert.AreEqual("quest-c", result.Findings[0].RelatedId);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-12T00:00:00Z"), result.Findings[0].RangeStartUtc);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-30T00:00:00Z"), result.Findings[0].RangeEndUtc);
        }

        [Test]
        public void LongGap_OnlyUnreadableNextEvent_NotFound()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-a", "quest", "2026-09-10T00:00:00Z", "2026-09-12T00:00:00Z"),
                ValidationTestFixtures.Entry("quest-b", "quest", "2026-9-30", "2026-10-02T00:00:00Z"));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new LongGapBetweenEventsRule(), document).Outcome,
                "Chỉ một đợt đọc được startUtc — không đủ hai mốc.");
        }

        [Test]
        public void LongGap_BlocksMergedAcrossOverlap()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-a", "quest", "2026-09-10T00:00:00Z", "2026-09-14T00:00:00Z"),
                // Chồng quest-a → bị bỏ; bắt đầu trước cuối khối nên không làm mốc kế.
                ValidationTestFixtures.Entry("quest-overlap", "quest", "2026-09-12T00:00:00Z", "2026-09-25T00:00:00Z"),
                // Chạm quest-a (start == end) → cùng khối, cuối khối = 16/9.
                ValidationTestFixtures.Entry("quest-touch", "quest", "2026-09-14T00:00:00Z", "2026-09-16T00:00:00Z"),
                ValidationTestFixtures.Entry("quest-late", "quest", "2026-09-30T00:00:00Z", "2026-10-02T00:00:00Z"));

            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new LongGapBetweenEventsRule(), document);

            Assert.AreEqual(1, result.Findings.Count, "Không có khoảng trống 14/9 → 14/9 giữa hai đợt chạm nhau.");
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-16T00:00:00Z"), result.Findings[0].RangeStartUtc);
            Assert.AreEqual("quest-late", result.Findings[0].RelatedId);
        }

        [Test]
        public void LongGap_ExactlyThreshold_NotFound()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-a", "quest", "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z"),
                ValidationTestFixtures.Entry("quest-b", "quest", "2026-09-20T00:00:00Z", "2026-09-22T00:00:00Z"));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new LongGapBetweenEventsRule(), document).Outcome,
                "Đúng 7 ngày không phải \"dài hơn\" ngưỡng.");

            LiveEventCalendarDocument oneTickLonger = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-a", "quest", "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z"),
                ValidationTestFixtures.Entry("quest-b", "quest", "2026-09-20T00:00:01Z", "2026-09-22T00:00:00Z"));
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, ValidationTestFixtures.Evaluate(new LongGapBetweenEventsRule(), oneTickLonger).Outcome);
        }

        [Test]
        public void LongGap_EndedBeforeLookback_NotFound()
        {
            // Cuối khối 1/8 < now (13/9) − 30 ngày = 14/8: khoảng trống đã qua, không còn sửa được cho người chơi.
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-a", "quest", "2026-07-25T00:00:00Z", "2026-08-01T00:00:00Z"),
                ValidationTestFixtures.Entry("quest-b", "quest", "2026-08-20T00:00:00Z", "2026-08-22T00:00:00Z"));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new LongGapBetweenEventsRule(), document).Outcome);
        }

        [Test]
        public void LongGap_CustomSettings_UsesThresholdAndLookback()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-a", "quest", "2026-07-25T00:00:00Z", "2026-08-01T00:00:00Z"),
                ValidationTestFixtures.Entry("quest-b", "quest", "2026-08-05T00:00:00Z", "2026-08-07T00:00:00Z"));
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(document, LiveOpsDesignSample.NowUtc)
                .WithSettings(new LiveEventCalendarValidationSettings(TimeSpan.FromDays(3), TimeSpan.FromDays(60)))
                .Build();

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, new LongGapBetweenEventsRule().Evaluate(context).Outcome);
        }

        [Test]
        public void LongGap_TypeWithRecurringRule_Skipped()
        {
            LiveEventCalendarDocument document = ValidationTestFixtures.Document(
                new[] { new RecurringLiveEventRule("quest", "2026-01-05T00:00:00Z", string.Empty, 720, 24, "quest_rule") },
                ValidationTestFixtures.Entry("quest-a", "quest", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z"),
                ValidationTestFixtures.Entry("quest-b", "quest", "2026-09-30T00:00:00Z", "2026-10-01T00:00:00Z"));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new LongGapBetweenEventsRule(), document).Outcome);
        }

        [Test]
        public void LongGap_SingleEventOrOpenFuture_NotFound()
        {
            LiveEventCalendarDocument single = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-a", "quest", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z"));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new LongGapBetweenEventsRule(), single).Outcome,
                "Khoảng trống mở về tương lai không phải \"giữa hai đợt\".");
        }

        [Test]
        public void LongGap_UndeclaredType_NotMeasuredHere()
        {
            // Loại chưa khai đã là Bị bỏ ở luật 8 — V-1 chỉ đo loại trong EventTypes.
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(ValidationTestFixtures.Entry("quest-a", "quest", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z"))
                .WithFixedEvent(ValidationTestFixtures.Entry("quest-b", "quest", "2026-09-30T00:00:00Z", "2026-10-01T00:00:00Z"))
                .Build();

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new LongGapBetweenEventsRule(), document).Outcome);
        }
    }
}
