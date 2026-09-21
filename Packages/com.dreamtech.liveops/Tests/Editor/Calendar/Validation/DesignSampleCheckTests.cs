using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    /// <summary>
    /// Bộ tổng hợp mục 6.2: validator mặc định đủ mười hai luật THẬT chạy trên dữ liệu mẫu thiết kế (13/9 08:47, bản so 11/9 16:20)
    /// phải ra đúng số đếm của màn Kiểm lịch [SD2 §1.3] — "Tất cả 6", "CẦN XỬ LÝ 5", "12 luật · 5 phát hiện", "Sửa các lỗi an toàn (1)…".
    /// </summary>
    [TestFixture]
    public sealed class DesignSampleCheckTests
    {
        [Test]
        public void DefaultScenario_CountsMatchDesign()
        {
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(PublishedBaselineSample.ContextWithBaseline(LiveOpsDesignSample.Document));
            LiveEventCalendarCheckSummary summary = report.Summary;

            Assert.AreEqual(12, summary.RuleCount);
            Assert.AreEqual(2, summary.DroppedCount, "utc-time-format · lava-quest-2026-10 và overlap-same-type · hunt-0916-bonus.");
            Assert.AreEqual(1, summary.ProgressLostCount, "running-event-id-changed · weekly-pass.");
            Assert.AreEqual(2, summary.ShouldReviewCount, "config-key-missing · hunt-0914 và long-gap-between-events · lava-quest.");
            Assert.AreEqual(1, summary.NotMeasuredRuleCount, "remote-snapshot-drift chưa dán JSON đang chạy.");
            Assert.AreEqual(6, summary.PassedRuleCount);
            Assert.AreEqual(0, summary.NotApplicableRuleCount);
            Assert.AreEqual(5, summary.NeedsActionCount);
            Assert.AreEqual(1, summary.SafeRepairCount);
            Assert.AreEqual(0, summary.IgnoredCount);
            Assert.AreEqual(0, summary.RemoteSnapshotFindingCount);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, summary.WorstConsequence);

            // Đúng năm hàng, đúng thứ tự hiện: hậu quả → giờ neo (16/9, 1/10 · 7/9 · 14/9, 20/9).
            string[] expectedRows =
            {
                LiveEventCalendarRuleIds.OverlapSameType + " · hunt-0916-bonus",
                LiveEventCalendarRuleIds.UtcTimeFormat + " · lava-quest-2026-10",
                LiveEventCalendarRuleIds.RunningEventIdChanged + " · weekly-pass",
                LiveEventCalendarRuleIds.ConfigKeyMissing + " · hunt-0914",
                LiveEventCalendarRuleIds.LongGapBetweenEvents + " · lava-quest",
            };
            CollectionAssert.AreEqual(expectedRows, RowsOf(report.Findings));

            AssertOutcomes(report,
                LiveEventCalendarRuleOutcome.Found, LiveEventCalendarRuleOutcome.Passed, LiveEventCalendarRuleOutcome.Passed,
                LiveEventCalendarRuleOutcome.Passed, LiveEventCalendarRuleOutcome.Found, LiveEventCalendarRuleOutcome.Passed,
                LiveEventCalendarRuleOutcome.Passed, LiveEventCalendarRuleOutcome.Passed, LiveEventCalendarRuleOutcome.Found,
                LiveEventCalendarRuleOutcome.Found, LiveEventCalendarRuleOutcome.Found, LiveEventCalendarRuleOutcome.NotMeasured);
            Assert.AreEqual(RemoteSnapshotDriftRule.RemoteNotPastedReasonCode, report.RuleResults[11].ReasonCode);
        }

        [Test]
        public void NoBaseline_RunningEventRuleIsNotApplicable_PassedIs6()
        {
            // Vá V-2: không có bản so → luật 9 Không áp dụng; hunt-0914 vẫn bắn luật 10 vì "không có bản so".
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(LiveOpsDesignSample.DocumentWithoutPublishedStamp(),
                LiveOpsDesignSample.NowUtc).Build();

            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(context);
            LiveEventCalendarCheckSummary summary = report.Summary;

            Assert.AreEqual(6, summary.PassedRuleCount, "12 − 4 Found − 1 NotMeasured − 1 NotApplicable.");
            Assert.AreEqual(1, summary.NotApplicableRuleCount);
            Assert.AreEqual(1, summary.NotMeasuredRuleCount);
            Assert.AreEqual(2, summary.DroppedCount);
            Assert.AreEqual(0, summary.ProgressLostCount);
            Assert.AreEqual(2, summary.ShouldReviewCount);
            Assert.AreEqual(4, summary.NeedsActionCount);

            AssertOutcomes(report,
                LiveEventCalendarRuleOutcome.Found, LiveEventCalendarRuleOutcome.Passed, LiveEventCalendarRuleOutcome.Passed,
                LiveEventCalendarRuleOutcome.Passed, LiveEventCalendarRuleOutcome.Found, LiveEventCalendarRuleOutcome.Passed,
                LiveEventCalendarRuleOutcome.Passed, LiveEventCalendarRuleOutcome.Passed, LiveEventCalendarRuleOutcome.NotApplicable,
                LiveEventCalendarRuleOutcome.Found, LiveEventCalendarRuleOutcome.Found, LiveEventCalendarRuleOutcome.NotMeasured);
            Assert.AreEqual(RunningEventIdChangedRule.NoPublishedStampReasonCode, report.RuleResults[8].ReasonCode);

            LiveEventCalendarFinding configKeyFinding = report.RuleResults[9].Findings[0];
            Assert.AreEqual("hunt-0914", configKeyFinding.TargetId);
            Assert.AreEqual(LiveEventCalendarDetailCodes.ConfigKeyInheritedNew, configKeyFinding.DetailCode);
        }

        [Test]
        public void DroppedFindings_EqualCompilationDroppedCount()
        {
            LiveEventCalendarCheckContext context = PublishedBaselineSample.ContextWithBaseline(LiveOpsDesignSample.Document);

            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(context);

            // Luật 9–12 thật không được sinh thêm phát hiện Bị bỏ: tổng Bị bỏ của nháp vẫn đúng bằng số mục bộ biên dịch bỏ.
            Assert.AreEqual(context.Compilation.DroppedCount, report.Summary.DroppedCount);
            int droppedFindingCount = 0;
            for (int index = 0; index < report.Findings.Count; index++)
            {
                if (report.Findings[index].Consequence == LiveEventCalendarConsequence.Dropped) droppedFindingCount++;
            }
            Assert.AreEqual(context.Compilation.DroppedCount, droppedFindingCount);
        }

        [Test]
        public void AfterFixingBothDropped_NeedsAction3_SafeRepair0()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            LiveEventCalendarCheckReport before = LiveEventCalendarValidator.Default.Check(PublishedBaselineSample.ContextWithBaseline(document));

            document = ApplyFirstRepair(document, FindingOf(before, LiveEventCalendarRuleIds.UtcTimeFormat, "lava-quest-2026-10"));
            document = ApplyFirstRepair(document, FindingOf(before, LiveEventCalendarRuleIds.OverlapSameType, "hunt-0916-bonus"));

            LiveEventCalendarCheckReport after = LiveEventCalendarValidator.Default.Check(PublishedBaselineSample.ContextWithBaseline(document));

            Assert.AreEqual(0, after.Summary.DroppedCount);
            Assert.AreEqual(1, after.Summary.ProgressLostCount);
            Assert.AreEqual(2, after.Summary.ShouldReviewCount, "Vá V-1: sửa endUtc của lava-quest-2026-10 không làm khoảng 20/9 → 1/10 biến mất.");
            Assert.AreEqual(3, after.Summary.NeedsActionCount);
            Assert.AreEqual(0, after.Summary.SafeRepairCount);

            LiveEventCalendarFinding gap = FindingOf(after, LiveEventCalendarRuleIds.LongGapBetweenEvents, "lava-quest");
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-20T00:00:00Z"), gap.RangeStartUtc);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-10-01T00:00:00Z"), gap.RangeEndUtc);
        }

        [Test]
        public void RemotePastedMatchingSha_DriftPassed_NotMeasured0()
        {
            LiveEventCalendarCheckContext context = PublishedBaselineSample.ContextWithBaseline(LiveOpsDesignSample.Document,
                PublishedBaselineSample.Document(), LiveOpsDesignSample.PublishedSha256Hex);

            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(context);

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, report.RuleResults[11].Outcome);
            Assert.AreEqual(0, report.Summary.NotMeasuredRuleCount);
            Assert.AreEqual(7, report.Summary.PassedRuleCount);
            Assert.AreEqual(5, report.Summary.NeedsActionCount);
            Assert.AreEqual(0, report.Summary.RemoteSnapshotFindingCount);
        }

        [Test]
        public void IgnoredLongGap_NotCountedOnRail_IgnoredCount1()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.Document)
                .WithIgnoredWarning(new IgnoredCalendarWarning(LiveEventCalendarRuleIds.LongGapBetweenEvents, "lava-quest",
                    "2026-09-20T00:00:00Z", "2026-10-01T00:00:00Z", "tháng 9 nghỉ lava-quest", string.Empty))
                .Build();

            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(PublishedBaselineSample.ContextWithBaseline(document));

            Assert.AreEqual(1, report.Summary.IgnoredCount);
            Assert.AreEqual(1, report.Summary.ShouldReviewCount, "Chỉ còn config-key-missing · hunt-0914.");
            Assert.AreEqual(4, report.Summary.NeedsActionCount);
            Assert.AreEqual(1, report.IgnoredFindings.Count);
            Assert.AreEqual(LiveEventCalendarRuleIds.LongGapBetweenEvents, report.IgnoredFindings[0].RuleId);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, report.RuleResults[10].Outcome, "Bỏ qua không biến luật thành đã qua.");
        }

        [Test]
        public void UnknownTypeOnlyInRemote_ShouldReview_CopyNotBlocked()
        {
            // JSON đang chạy có thêm lucky-spin (chưa khai trong asset, nháp không dùng): nói về bản đang chạy, không về nháp.
            LiveEventCalendarDocument remote = new LiveEventCalendarDocumentBuilder(PublishedBaselineSample.Document())
                .WithFixedEvent(new FixedLiveEventEntry("remote-lucky-spin", "lucky-spin-0915", "lucky-spin", "2026-09-15T00:00:00Z",
                    "2026-09-16T00:00:00Z", "lucky_spin_v1"))
                .Build();
            LiveEventCalendarCheckContext context = PublishedBaselineSample.ContextWithBaseline(LiveOpsDesignSample.Document, remote, "pasted-sha");

            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(context);

            LiveEventCalendarFinding unknownType = FindingOf(report, LiveEventCalendarRuleIds.UnknownEventType, "lucky-spin");
            Assert.AreEqual(LiveEventCalendarDetailCodes.UnknownTypeInRemote, unknownType.DetailCode);
            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, unknownType.Consequence);
            Assert.IsTrue(unknownType.IsAboutRemoteSnapshot);

            LiveEventCalendarFinding drift = FindingOf(report, LiveEventCalendarRuleIds.RemoteSnapshotDrift, string.Empty);
            Assert.AreEqual(LiveEventCalendarDetailCodes.RemoteDiffers, drift.DetailCode);
            Assert.AreEqual("lucky-spin-0915", drift.FoundText);
            Assert.IsTrue(drift.IsAboutRemoteSnapshot);

            // Dòng cổng 1 đọc DroppedCount: vẫn đúng hai mục nháp bị bỏ — phát hiện về bản remote không chặn Copy JSON.
            Assert.AreEqual(2, report.Summary.DroppedCount);
            Assert.AreEqual(context.Compilation.DroppedCount, report.Summary.DroppedCount);
            Assert.AreEqual(5, report.Summary.NeedsActionCount, "Không vào CẦN XỬ LÝ của nháp.");
            Assert.AreEqual(2, report.Summary.RemoteSnapshotFindingCount);
        }

        // ----- Hỗ trợ -----

        private static List<string> RowsOf(IReadOnlyList<LiveEventCalendarFinding> findings)
        {
            var rows = new List<string>(findings.Count);
            for (int index = 0; index < findings.Count; index++) rows.Add(findings[index].RuleId + " · " + findings[index].TargetId);
            return rows;
        }

        private static void AssertOutcomes(LiveEventCalendarCheckReport report, params LiveEventCalendarRuleOutcome[] expectedOutcomes)
        {
            IReadOnlyList<string> ruleIds = LiveEventCalendarRuleIds.All;
            Assert.AreEqual(expectedOutcomes.Length, report.RuleResults.Count);
            for (int index = 0; index < expectedOutcomes.Length; index++)
            {
                LiveEventCalendarRuleResult result = report.RuleResults[index];
                Assert.AreEqual(ruleIds[index], result.RuleId);
                Assert.AreEqual(expectedOutcomes[index], result.Outcome,
                    result.RuleId + " " + result.ReasonCode + " " + result.ExceptionTypeName + " " + result.ExceptionMessage);
            }
        }

        internal static LiveEventCalendarFinding FindingOf(LiveEventCalendarCheckReport report, string ruleId, string targetId)
        {
            var all = new List<LiveEventCalendarFinding>(report.Findings);
            all.AddRange(report.IgnoredFindings);
            for (int index = 0; index < all.Count; index++)
            {
                if (all[index].RuleId == ruleId && all[index].TargetId == targetId) return all[index];
            }
            Assert.Fail("Không có phát hiện " + ruleId + " · " + targetId);
            return null;
        }

        private static LiveEventCalendarDocument ApplyFirstRepair(LiveEventCalendarDocument document, LiveEventCalendarFinding finding)
        {
            Assert.Greater(finding.Repairs.Count, 0, finding.RuleId);
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, finding.Repairs[0].Edit, out LiveEventCalendarDocument repaired), finding.RuleId);
            return repaired;
        }
    }

    /// <summary>
    /// Bản so của mẫu thiết kế (dấu 11/9 16:20) dựng tay đúng như <c>ActiveBaseline</c> — không có định nghĩa loại (V-3), configKey
    /// và tiền tố ghi rõ như JSON. Assembly test core không tham chiếu parser Unity, nên dựng tay rồi khoá bằng bộ ghi: bộ ghi phải ra
    /// ĐÚNG byte <see cref="LiveOpsDesignSample.PublishedSnapshotJson"/> — sửa bản dựng lệch dấu đã đăng là test đỏ ngay ở đây.
    /// </summary>
    internal static class PublishedBaselineSample
    {
        public static LiveEventCalendarDocument Document()
        {
            LiveEventCalendarDocument baseline = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("weekly-pass", "2026-01-05T00:00:00Z", "weekly-pass-", 168, 168, "weekly_pass_s3"))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "sky-race-", 24, 20, "sky_race_v4"))
                .WithFixedEvent(new FixedLiveEventEntry("published-0", "lava-quest-2026-09a", "lava-quest", "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("published-1", "hunt-0914", "treasure-hunt", "2026-09-14T00:00:00Z", "2026-09-17T00:00:00Z", "hunt_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("published-2", "lava-quest-2026-09b", "lava-quest", "2026-09-17T00:00:00Z", "2026-09-19T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("published-3", "lava-quest-2026-10", "lava-quest", "2026-10-01T00:00:00Z", "2026-10-03T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("published-4", "star-tournament-2026-10", "star-tournament", "2026-10-03T00:00:00Z", "2026-10-06T00:00:00Z", "star_tournament_v1"))
                .Build();

            LiveEventCalendarJsonText written = LiveEventCalendarJsonWriter.Write(baseline, LiveEventCalendarJsonFormat.Version2);
            Assert.AreEqual(LiveOpsDesignSample.PublishedSnapshotJson, written.Text, "Bản so dựng tay phải đúng JSON đã đăng 11/9 16:20.");
            Assert.AreEqual(LiveOpsDesignSample.PublishedSha256Hex, written.Sha256Hex);
            return baseline;
        }

        public static LiveEventCalendarCheckContext ContextWithBaseline(LiveEventCalendarDocument draft)
        {
            return new LiveEventCalendarCheckContextBuilder(draft, LiveOpsDesignSample.NowUtc)
                .WithPublishedBaseline(Document())
                .Build();
        }

        public static LiveEventCalendarCheckContext ContextWithBaseline(LiveEventCalendarDocument draft, LiveEventCalendarDocument remote, string remoteSha256Hex)
        {
            return new LiveEventCalendarCheckContextBuilder(draft, LiveOpsDesignSample.NowUtc)
                .WithPublishedBaseline(Document())
                .WithRemoteSnapshot(remote, remoteSha256Hex)
                .Build();
        }
    }
}
