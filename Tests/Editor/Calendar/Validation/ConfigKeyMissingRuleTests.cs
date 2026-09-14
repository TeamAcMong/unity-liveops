using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class ConfigKeyMissingRuleTests
    {
        [Test]
        public void Passes_WhenConditionAbsent()
        {
            // Mọi mục tự khai configKey — không có gì kế thừa âm thầm.
            LiveEventCalendarDocument draft = WithHuntConfigKey(LiveOpsDesignSample.Document, "hunt_v1");

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed,
                new ConfigKeyMissingRule().Evaluate(PublishedBaselineSample.ContextWithBaseline(draft)).Outcome);
        }

        [Test]
        public void Finds_DesignSampleCase()
        {
            LiveEventCalendarRuleResult result = new ConfigKeyMissingRule().Evaluate(PublishedBaselineSample.ContextWithBaseline(LiveOpsDesignSample.Document));

            Assert.AreEqual(1, result.Findings.Count, "Q-4 (b): đúng một phát hiện trên mẫu — hunt-0914.");
            LiveEventCalendarFinding finding = result.Findings[0];
            Assert.AreEqual("hunt-0914", finding.TargetId);
            Assert.AreEqual(LiveEventCalendarDetailCodes.ConfigKeyInheritedDiffers, finding.DetailCode);
            Assert.AreEqual("hunt_default", finding.FoundText, "\"xuất sẽ ghi hunt_default\" [SD2 §2.3 hàng 4].");
            Assert.AreEqual("hunt_v1", finding.ExpectedText, "\"bản đã đăng dùng hunt_v1\".");
            Assert.AreEqual("treasure-hunt", finding.RelatedId);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-14T00:00:00Z"), finding.AnchorUtc, "\"đợt chưa bắt đầu (14/9 00:00 UTC)\".");
        }

        [Test]
        public void Finding_HasTargetAndRepairKind()
        {
            LiveEventCalendarFinding finding = new ConfigKeyMissingRule().Evaluate(PublishedBaselineSample.ContextWithBaseline(LiveOpsDesignSample.Document)).Findings[0];

            Assert.AreEqual(LiveEventCalendarTargetKind.FixedEvent, finding.TargetKind);
            Assert.AreEqual(LiveOpsDesignSample.HuntEarlyEntryKey, finding.TargetEntryKey);
            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, finding.Consequence);
            Assert.AreEqual(LiveEventCalendarRepairKind.None, finding.RepairKind, "6.1: chỉ link Xem trong lịch, không lệnh sửa.");
            Assert.AreEqual(0, finding.Repairs.Count);
            Assert.IsFalse(finding.IsAboutRemoteSnapshot);
        }

        [Test]
        public void NoBaseline_InheritingItem_InheritedNew()
        {
            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new ConfigKeyMissingRule(), LiveOpsDesignSample.DocumentWithoutPublishedStamp());

            Assert.AreEqual(1, result.Findings.Count);
            Assert.AreEqual(LiveEventCalendarDetailCodes.ConfigKeyInheritedNew, result.Findings[0].DetailCode, "V-2: không có bản so vẫn bắn.");
            Assert.AreEqual(string.Empty, result.Findings[0].ExpectedText);
        }

        [Test]
        public void InheritedKeyEqualsBaseline_Passes()
        {
            // Bản so cũng dùng hunt_default (JSON ghi configKey hiệu lực) → khoá tới người chơi không đổi.
            LiveEventCalendarDocument baseline = ValidationTestFixtures.FixedDocument(
                new FixedLiveEventEntry("published", "hunt-0914", "treasure-hunt", "2026-09-14T00:00:00Z", "2026-09-17T00:00:00Z", "hunt_default"));
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(LiveOpsDesignSample.Document, LiveOpsDesignSample.NowUtc)
                .WithPublishedBaseline(baseline)
                .Build();

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, new ConfigKeyMissingRule().Evaluate(context).Outcome);
        }

        [Test]
        public void BaselineHasSameIdOtherType_InheritedNew()
        {
            LiveEventCalendarDocument baseline = ValidationTestFixtures.FixedDocument(
                new FixedLiveEventEntry("published", "hunt-0914", "lava-quest", "2026-09-14T00:00:00Z", "2026-09-17T00:00:00Z", "hunt_default"));
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(LiveOpsDesignSample.Document, LiveOpsDesignSample.NowUtc)
                .WithPublishedBaseline(baseline)
                .Build();

            LiveEventCalendarFinding finding = new ConfigKeyMissingRule().Evaluate(context).Findings[0];

            Assert.AreEqual(LiveEventCalendarDetailCodes.ConfigKeyInheritedNew, finding.DetailCode, "Danh tính = id + loại, như diff.");
        }

        [Test]
        public void TypeWithoutDefault_EmptyDefault()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("quest", "Quest", 0, false, string.Empty))
                .WithFixedEvent(new FixedLiveEventEntry("key-quest", "quest-0920", "quest", "2026-09-20T00:00:00Z", "2026-09-22T00:00:00Z", string.Empty))
                .Build();

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new ConfigKeyMissingRule(), document).Findings[0];

            Assert.AreEqual(LiveEventCalendarDetailCodes.ConfigKeyEmpty, finding.DetailCode, "Game nhận configKey rỗng — không tra được cấu hình.");
            Assert.AreEqual(string.Empty, finding.FoundText);
        }

        [Test]
        public void EndedOrDroppedInheritingEntries_Skipped()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("quest", "Quest", 0, false, "quest_default"))
                // Đã khép trước now 13/9 08:47: khoá thưởng không còn tới ai.
                .WithFixedEvent(new FixedLiveEventEntry("key-ended", "quest-0901", "quest", "2026-09-01T00:00:00Z", "2026-09-03T00:00:00Z", string.Empty))
                // Bị bỏ (giờ kết thúc hỏng): đã có phát hiện Bị bỏ riêng.
                .WithFixedEvent(new FixedLiveEventEntry("key-dropped", "quest-1001", "quest", "2026-10-01T00:00:00Z", "2026-10-3", string.Empty))
                .Build();

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new ConfigKeyMissingRule(), document).Outcome);
        }

        [Test]
        public void UndeclaredTypeEntry_SkippedBecauseUnknownTypeRuleDropsIt()
        {
            // lucky-spin chưa khai loại: bộ biên dịch giữ mục nhưng LiveOpsSystem không đăng ký loại nên game bỏ (luật 8 báo Bị bỏ).
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("quest", "Quest", 0, false, "quest_default"))
                .WithFixedEvent(new FixedLiveEventEntry("key-spin", "spin-0920", "lucky-spin", "2026-09-20T00:00:00Z", "2026-09-21T00:00:00Z", string.Empty))
                .Build();
            LiveEventCalendarCheckContext context = ValidationTestFixtures.ContextFor(document);
            Assert.IsTrue(context.Compilation.TryGetFixedOutcome("key-spin", out LiveEventCalendarEntryOutcome outcome));
            Assert.IsTrue(outcome.IsKept, "Tiền đề: bộ biên dịch không biết loại — chỉ luật 8 mới nói game bỏ.");

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, new ConfigKeyMissingRule().Evaluate(context).Outcome,
                "Một mục game bỏ không được ra thêm hàng Nên xem configKey rỗng.");

            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(context);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, report.RuleResults[7].Outcome, "Luật 8 vẫn báo loại lạ.");
            Assert.AreEqual(1, report.Findings.Count, "Đúng một phát hiện cho mục này.");
        }

        [Test]
        public void RecurringRuleInheriting_FoundOnRule()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("sky-race", "Đua trên trời", 4, false, "sky_race_v5"))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "sky-race-", 24, 20, string.Empty))
                .Build();
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(document, LiveOpsDesignSample.NowUtc)
                .WithPublishedBaseline(PublishedBaselineSample.Document())
                .Build();

            LiveEventCalendarFinding finding = new ConfigKeyMissingRule().Evaluate(context).Findings[0];

            Assert.AreEqual(LiveEventCalendarTargetKind.RecurringRule, finding.TargetKind);
            Assert.AreEqual("sky-race", finding.TargetId);
            Assert.AreEqual("sky-race", finding.TargetEntryKey);
            Assert.AreEqual(LiveEventCalendarDetailCodes.ConfigKeyInheritedDiffers, finding.DetailCode);
            Assert.AreEqual("sky_race_v5", finding.FoundText);
            Assert.AreEqual("sky_race_v4", finding.ExpectedText);
        }

        [Test]
        public void CheckLane_OtherType_NoFinding()
        {
            LiveEventCalendarCheckContext context = PublishedBaselineSample.ContextWithBaseline(LiveOpsDesignSample.Document).ToBuilder()
                .WithOnlyEventType("lava-quest")
                .Build();

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, new ConfigKeyMissingRule().Evaluate(context).Outcome);
        }

        private static LiveEventCalendarDocument WithHuntConfigKey(LiveEventCalendarDocument document, string configKey)
        {
            Assert.IsTrue(document.TryGetFixedEvent(LiveOpsDesignSample.HuntEarlyEntryKey, out FixedLiveEventEntry entry));
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new ReplaceFixedEventEdit(entry.WithConfigKey(configKey)), out LiveEventCalendarDocument result));
            return result;
        }
    }
}
