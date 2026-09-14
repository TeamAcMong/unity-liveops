using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class UnknownEventTypeRuleTests
    {
        [Test]
        public void Passes_WhenConditionAbsent()
        {
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed,
                ValidationTestFixtures.Evaluate(new UnknownEventTypeRule(), LiveOpsDesignSample.Document).Outcome);
        }

        [Test]
        public void Finds_UndeclaredTypeInDraft_OneFindingPerType()
        {
            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new UnknownEventTypeRule(), ValidationTestFixtures.UndeclaredTypeDocument());

            Assert.AreEqual(1, result.Findings.Count, "Đếm theo loại, không theo đợt.");
            LiveEventCalendarFinding finding = result.Findings[0];
            Assert.AreEqual("lucky-spin", finding.TargetId);
            Assert.AreEqual(LiveEventCalendarDetailCodes.UnknownTypeInDraft, finding.DetailCode);
            Assert.AreEqual("2", finding.FoundText, "Số đợt/luật dùng loại — \"có 2 đợt … chưa có loại\".");
            Assert.AreEqual("lucky-spin-0914", finding.RelatedId);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, finding.Consequence);
            Assert.IsFalse(finding.IsAboutRemoteSnapshot);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-14T00:00:00Z"), finding.RangeStartUtc);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-21T00:00:00Z"), finding.RangeEndUtc);
        }

        [Test]
        public void UnknownType_RemoteOnly_ShouldReviewNotDropped()
        {
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(LiveOpsDesignSample.Document, LiveOpsDesignSample.NowUtc)
                .WithRemoteSnapshot(ValidationTestFixtures.RemoteWithUnknownType(), "remote-sha")
                .Build();

            LiveEventCalendarCheckReport report = new LiveEventCalendarValidator(new ILiveEventCalendarRule[] { new UnknownEventTypeRule() }).Check(context);

            Assert.AreEqual(1, report.Findings.Count, "treasure-hunt đã khai trong asset → chỉ gold-rush.");
            LiveEventCalendarFinding finding = report.Findings[0];
            Assert.AreEqual("gold-rush", finding.TargetId);
            Assert.AreEqual(LiveEventCalendarDetailCodes.UnknownTypeInRemote, finding.DetailCode);
            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, finding.Consequence);
            Assert.IsTrue(finding.IsAboutRemoteSnapshot);
            Assert.AreEqual(0, report.Summary.DroppedCount, "V-17: không vào Bị bỏ của nháp.");
            Assert.AreEqual(0, report.Summary.NeedsActionCount, "…không chặn Copy JSON.");
            Assert.AreEqual(1, report.Summary.RemoteSnapshotFindingCount);
        }

        [Test]
        public void UnknownTypeInBothDraftAndRemote_OneDraftFinding()
        {
            LiveEventCalendarDocument remote = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(ValidationTestFixtures.Entry("lucky-spin-0901", "lucky-spin", "2026-09-01T00:00:00Z", "2026-09-02T00:00:00Z"))
                .Build();
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(ValidationTestFixtures.UndeclaredTypeDocument(), LiveOpsDesignSample.NowUtc)
                .WithRemoteSnapshot(remote, "remote-sha")
                .Build();

            LiveEventCalendarRuleResult result = new UnknownEventTypeRule().Evaluate(context);

            Assert.AreEqual(1, result.Findings.Count);
            Assert.AreEqual(LiveEventCalendarDetailCodes.UnknownTypeInDraft, result.Findings[0].DetailCode);
        }

        [Test]
        public void UndeclaredRecurringRuleType_Found()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", string.Empty, 24, 20, string.Empty))
                .Build();

            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new UnknownEventTypeRule(), document).Findings[0];

            Assert.AreEqual("sky-race", finding.TargetId);
            Assert.AreEqual("sky-race-", finding.RelatedId, "Luật không có id cố định — nêu tiền tố hiệu lực.");
        }

        [Test]
        public void InvalidTypeString_NotReported()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(ValidationTestFixtures.Entry("a", string.Empty, "2026-09-01T00:00:00Z", "2026-09-02T00:00:00Z"))
                .WithFixedEvent(ValidationTestFixtures.Entry("b", "bad#type", "2026-09-01T00:00:00Z", "2026-09-02T00:00:00Z"))
                .Build();

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, ValidationTestFixtures.Evaluate(new UnknownEventTypeRule(), document).Outcome,
                "Loại sai quy tắc đã có phát hiện của luật 3 — thêm 'loại lạ' là nhiễu.");
        }

        [Test]
        public void OnlyEventType_FiltersOtherTypes()
        {
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(ValidationTestFixtures.UndeclaredTypeDocument(), LiveOpsDesignSample.NowUtc)
                .WithOnlyEventType("treasure-hunt")
                .Build();

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, new UnknownEventTypeRule().Evaluate(context).Outcome);
        }

        [Test]
        public void Finding_HasTargetAndRepairKind()
        {
            LiveEventCalendarFinding finding = ValidationTestFixtures.Evaluate(new UnknownEventTypeRule(), ValidationTestFixtures.UndeclaredTypeDocument()).Findings[0];

            Assert.AreEqual(LiveEventCalendarTargetKind.EventType, finding.TargetKind);
            Assert.AreEqual("lucky-spin", finding.TargetEntryKey);
            Assert.AreEqual(LiveEventCalendarRepairKind.None, finding.RepairKind, "Sửa = nút Khai báo ở Loại event, không phải lệnh tự động.");
        }
    }
}
