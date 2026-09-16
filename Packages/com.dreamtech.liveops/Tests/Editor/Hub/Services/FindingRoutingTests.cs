using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// MỘT hàm từ phát hiện tới health cho mọi màn (6.4): (V-21 CC-SHELL-5 (b)) mỗi health mang số đếm đúng đích để rail cộng số;
    /// (V-8, CC-FT-1) câu tooltip dựng từ <c>LiveOpsFindingText</c> chứ không màn nào tự viết; (V-9) health màn Xuất JSON lấy thẳng từ
    /// cổng xuất; (V-17) loại chỉ có trong JSON đang chạy đã dán chặn màn Loại event nhưng KHÔNG vào số đếm của Kiểm lịch;
    /// (Q-12) luật không đo được thì NotMeasured, không im lặng thành Ok.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class FindingRoutingTests
    {
        private static readonly LiveOpsHubHealthTarget[] AllTargets =
        {
            LiveOpsHubHealthTarget.Calendar, LiveOpsHubHealthTarget.RecurringRules,
            LiveOpsHubHealthTarget.EventTypes, LiveOpsHubHealthTarget.Validation,
        };

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        [Test]
        public void FindingRouting_HealthCountsMatchSummaryPerTarget()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            LiveOpsHubCalendarSession session = services.Session;
            LiveEventCalendarCheckReport report = session.Check.LastReport;
            Assert.IsNotNull(report, "ngữ cảnh mẫu thiết kế đã kiểm xong");

            foreach (LiveOpsHubHealthTarget target in AllTargets)
            {
                LiveOpsHubFindingCounts expected = ExpectedCounts(report, target);
                LiveOpsHubFindingCounts counts = LiveOpsHubFindingRouting.CountsFor(report, target);
                Assert.AreEqual(expected.Dropped, counts.Dropped, target + ": số đợt bị bỏ");
                Assert.AreEqual(expected.ProgressLost, counts.ProgressLost, target + ": số mất tiến độ");
                Assert.AreEqual(expected.ShouldReview, counts.ShouldReview, target + ": số nên xem");
                Assert.AreEqual(expected.NotMeasured, counts.NotMeasured, target + ": số luật chưa kiểm");

                SectionHealth health = LiveOpsHubFindingRouting.HealthFor(target, true, session.Check, session.Document, services.Format);
                Assert.AreEqual(counts.Dropped, health.Counts.Dropped, target + ": (CC-SHELL-5) health phải mang số đếm của chính đích đó");
                Assert.AreEqual(counts.ProgressLost, health.Counts.ProgressLost, target.ToString());
                Assert.AreEqual(counts.ShouldReview, health.Counts.ShouldReview, target.ToString());
                Assert.AreEqual(counts.NotMeasured, health.Counts.NotMeasured, target.ToString());
            }

            // Kiểm lịch nhìn TOÀN BỘ phát hiện không bỏ qua và không nói về bản remote — đúng con số của summary.
            Assert.AreEqual(0, report.IgnoredFindings.Count, "mẫu thiết kế không bỏ qua cảnh báo nào — nếu đổi thì so với summary phải trừ phần bỏ qua");
            Assert.AreEqual(0, report.Summary.RemoteSnapshotFindingCount, "ngữ cảnh này chưa dán JSON đang chạy");
            LiveOpsHubFindingCounts validation = LiveOpsHubFindingRouting.CountsFor(report, LiveOpsHubHealthTarget.Validation);
            Assert.AreEqual(report.Summary.DroppedCount, validation.Dropped, "số đợt bị bỏ của Kiểm lịch = summary");
            Assert.AreEqual(report.Summary.ProgressLostCount, validation.ProgressLost);
            Assert.AreEqual(report.Summary.ShouldReviewCount - report.Summary.RemoteSnapshotFindingCount, validation.ShouldReview,
                "(V-17) phát hiện về bản remote không vào đếm rail của Kiểm lịch");
            Assert.AreEqual(report.Summary.NotMeasuredRuleCount, validation.NotMeasured);
        }

        [Test]
        public void FindingRouting_HealthReasonUsesFindingTextSentences()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            LiveOpsHubCalendarSession session = services.Session;
            LiveEventCalendarFinding finding = FirstFinding(session.Check.LastReport, LiveOpsHubHealthTarget.Calendar);
            Assert.IsNotNull(finding, "mẫu thiết kế phải có ít nhất một phát hiện thuộc màn Lịch (lava-quest-2026-10 có ngày sai)");

            SectionHealth health = LiveOpsHubFindingRouting.HealthFor(LiveOpsHubHealthTarget.Calendar, true, session.Check,
                session.Document, services.Format);

            string shortLabel = LiveOpsFindingText.PlainText(LiveOpsFindingText.ShortLabel(finding));
            StringAssert.Contains(LiveOpsFindingText.PlainText(finding.TargetId), health.Reason, "tooltip phải nêu mục nào");
            StringAssert.Contains(shortLabel, health.Reason, "(CC-FT-1) câu lấy từ LiveOpsFindingText — màn không tự viết");
            Assert.IsFalse(health.Reason.Contains("<b>"), "tooltip không nhận rich text — phải qua PlainText");
            Assert.IsFalse(health.Reason.Contains("<color"), "tooltip không nhận rich text — phải qua PlainText");
        }

        [Test]
        public void FindingRouting_NoAsset_EverySectionIsNotMeasuredWithReason()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario);

            foreach (string sectionId in SectionIds())
            {
                SectionHealth health = LiveOpsHubFindingRouting.ForSection(sectionId, services);
                if (string.Equals(sectionId, LiveOpsHubSections.Ids.Overview, StringComparison.Ordinal))
                {
                    Assert.AreEqual(HealthState.Ok, health.State, "Tổng quan không tự kết luận — mọi dấu đã có ở màn gốc");
                    continue;
                }
                Assert.AreEqual(HealthState.NotMeasured, health.State, sectionId + ": chưa có lịch thì chưa đo được gì");
                Assert.AreEqual(LiveOpsHubStrings.ServicesHealthNoAsset, health.Reason, sectionId + ": NotMeasured phải có lý do (Q-12)");
            }
        }

        [Test]
        public void FindingRouting_NeverChecked_ValidationIsNotMeasuredNotOk()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            Assert.IsNull(services.Session.Check.LastReport, "builder của test KHÔNG tự kiểm khi mở");

            SectionHealth health = LiveOpsHubFindingRouting.ForSection(LiveOpsHubSections.Ids.Validation, services);

            Assert.AreEqual(HealthState.NotMeasured, health.State, "(Q-12) chưa kiểm không có nghĩa là ổn");
            Assert.AreEqual(LiveOpsHubStrings.ServicesHealthNeverChecked, health.Reason, "lý do phải kèm cách làm cho hết chưa kiểm (F5)");
        }

        [Test]
        public void FindingRouting_StaleCheck_ValidationKeepsCountsAndSaysWhy()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.StaleCheckScenario);
            LiveOpsHubCalendarSession session = services.Session;
            Assert.AreEqual(LiveOpsHubCheckStaleReason.CalendarEdited, session.Check.StaleReason);
            LiveOpsHubFindingCounts counts = LiveOpsHubFindingRouting.CountsFor(session.Check.LastReport, LiveOpsHubHealthTarget.Validation);

            SectionHealth health = LiveOpsHubFindingRouting.HealthFor(LiveOpsHubHealthTarget.Validation, true, session.Check,
                session.Document, services.Format);

            Assert.AreEqual(HealthState.NotMeasured, health.State, "(PD-23) kết quả cũ ở Kiểm lịch tụt xuống NotMeasured");
            Assert.IsTrue(health.IsStale, "nhưng vẫn là 'cũ' chứ không phải 'chưa đo' — badge giữ số cũ");
            Assert.AreEqual(session.Check.LastReport.CheckedAtUtc, health.MeasuredAtUtc);
            Assert.AreEqual(counts.Dropped, health.Counts.Dropped, "số cũ vẫn mang theo để rail cộng");
            Assert.IsTrue(health.Reason.StartsWith("Kết quả kiểm lúc ", StringComparison.Ordinal),
                "tooltip nói rõ con số đó đo lúc nào và vì sao cũ; nhận được: " + health.Reason);
        }

        [Test]
        public void FindingRouting_RunningCheck_ValidationShowsProgress()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.RunningCheckScenario);
            LiveOpsHubCalendarSession session = services.Session;
            Assert.IsTrue(session.Check.IsRunning);

            SectionHealth health = LiveOpsHubFindingRouting.HealthFor(LiveOpsHubHealthTarget.Validation, true, session.Check,
                session.Document, services.Format);

            Assert.AreEqual(HealthState.NotMeasured, health.State, "đang chạy thì chưa có kết quả");
            StringAssert.Contains(LiveOpsHubTestServices.RunningCompletedRuleCount.ToString(), health.Reason, "phải nêu 'Đang kiểm N/M luật…'");
        }

        [Test]
        public void FindingRouting_ExportHealthComesFromGate()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            ExportGateState gate = services.Session.EvaluateExportGate(services.JsonReadBack, services.Format);

            SectionHealth health = LiveOpsHubFindingRouting.ForSection(LiveOpsHubSections.Ids.Export, services);

            Assert.AreEqual(gate.Health.State, health.State, "(V-9) health màn Xuất JSON = health của cổng, không tính lại ở chỗ khác");
            Assert.AreEqual(gate.Health.Reason, health.Reason);
            Assert.AreEqual(HealthState.NotMeasured, LiveOpsHubFindingRouting.ExportHealth(false, null).State, "không có lịch thì không có cổng");
        }

        [Test]
        public void FindingRouting_RemoteOnlyFinding_DoesNotCountInValidation()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            LiveOpsHubCalendarSession session = services.Session;
            // JSON đang chạy có một loại mà lịch chưa khai — phát hiện của nó nói về bản remote.
            session.Remote.Set(RemoteJsonWithUnknownType(), LiveOpsDesignSample.NowUtc);
            session.RunCheckToCompletion();
            LiveEventCalendarCheckReport report = session.Check.LastReport;

            int remoteFindings = 0;
            foreach (LiveEventCalendarFinding finding in report.Findings)
            {
                if (!finding.IsIgnored && finding.IsAboutRemoteSnapshot) remoteFindings++;
            }
            Assert.Greater(remoteFindings, 0, "bản remote lệch phải sinh ít nhất một phát hiện, nếu không test này vô nghĩa");

            int nonRemoteFindings = 0;
            foreach (LiveEventCalendarFinding finding in report.Findings)
            {
                if (finding.IsIgnored || finding.IsAboutRemoteSnapshot) continue;
                if (finding.Consequence == LiveEventCalendarConsequence.Dropped
                    || finding.Consequence == LiveEventCalendarConsequence.ProgressLost
                    || finding.Consequence == LiveEventCalendarConsequence.ShouldReview) nonRemoteFindings++;
            }
            LiveOpsHubFindingCounts validation = LiveOpsHubFindingRouting.CountsFor(report, LiveOpsHubHealthTarget.Validation);

            Assert.AreEqual(nonRemoteFindings, validation.Dropped + validation.ProgressLost + validation.ShouldReview,
                "(V-17) phát hiện chỉ có trong JSON đang chạy không được vào đếm rail của Kiểm lịch");
            foreach (LiveEventCalendarFinding finding in report.Findings)
            {
                if (finding.IsAboutRemoteSnapshot)
                {
                    Assert.IsFalse(LiveOpsHubFindingRouting.BelongsTo(finding, LiveOpsHubHealthTarget.Validation),
                        "phát hiện về bản remote không thuộc đích Kiểm lịch");
                }
            }
        }

        [Test]
        public void FindingRouting_StateOfAndIsNotMeasured_FollowConsequenceTable()
        {
            Assert.AreEqual(HealthState.Blocked, LiveOpsHubFindingRouting.StateOf(LiveEventCalendarConsequence.Dropped));
            Assert.AreEqual(HealthState.Warning, LiveOpsHubFindingRouting.StateOf(LiveEventCalendarConsequence.ProgressLost));
            Assert.AreEqual(HealthState.Warning, LiveOpsHubFindingRouting.StateOf(LiveEventCalendarConsequence.ShouldReview));

            Assert.IsTrue(LiveOpsHubFindingRouting.IsNotMeasured(LiveEventCalendarRuleOutcome.NotMeasured));
            Assert.IsTrue(LiveOpsHubFindingRouting.IsNotMeasured(LiveEventCalendarRuleOutcome.Failed), "luật ném cũng là chưa đo được");
            Assert.IsFalse(LiveOpsHubFindingRouting.IsNotMeasured(LiveEventCalendarRuleOutcome.Passed));
        }

        [Test]
        public void FindingRouting_ForSection_UnknownId_Throws()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario);

            Assert.Throws<ArgumentOutOfRangeException>(() => LiveOpsHubFindingRouting.ForSection("khong-co-man-nay", services));
            Assert.Throws<ArgumentNullException>(() => LiveOpsHubFindingRouting.ForSection(LiveOpsHubSections.Ids.Overview, null));
        }

        // ------------------------------------------------------------------------------------------------------------ hỗ trợ

        private static IEnumerable<string> SectionIds()
        {
            yield return LiveOpsHubSections.Ids.Overview;
            yield return LiveOpsHubSections.Ids.EventTypes;
            yield return LiveOpsHubSections.Ids.Calendar;
            yield return LiveOpsHubSections.Ids.RecurringRules;
            yield return LiveOpsHubSections.Ids.Validation;
            yield return LiveOpsHubSections.Ids.Export;
        }

        /// <summary>Đếm lại bằng tay theo đúng định nghĩa 6.4 — nếu bộ định tuyến đổi cách đếm thì test này đỏ ngay.</summary>
        private static LiveOpsHubFindingCounts ExpectedCounts(LiveEventCalendarCheckReport report, LiveOpsHubHealthTarget target)
        {
            int dropped = 0;
            int progressLost = 0;
            int shouldReview = 0;
            foreach (LiveEventCalendarFinding finding in report.Findings)
            {
                if (finding.IsIgnored || !LiveOpsHubFindingRouting.BelongsTo(finding, target)) continue;
                if (finding.Consequence == LiveEventCalendarConsequence.Dropped) dropped++;
                else if (finding.Consequence == LiveEventCalendarConsequence.ProgressLost) progressLost++;
                else if (finding.Consequence == LiveEventCalendarConsequence.ShouldReview) shouldReview++;
            }
            int notMeasured = 0;
            foreach (LiveEventCalendarRuleResult result in report.RuleResults)
            {
                if (LiveOpsHubFindingRouting.IsNotMeasured(result.Outcome) && ConcernsTarget(result.RuleId, target)) notMeasured++;
            }
            return new LiveOpsHubFindingCounts(dropped, progressLost, shouldReview, notMeasured);
        }

        /// <summary>Cột "Đích" của bảng 6.1 chép lại trong test: luật chưa đo được phải đếm vào đúng màn mà nó có thể nói tới.</summary>
        private static bool ConcernsTarget(string ruleId, LiveOpsHubHealthTarget target)
        {
            switch (target)
            {
                case LiveOpsHubHealthTarget.Calendar:
                    return ruleId == LiveEventCalendarRuleIds.UtcTimeFormat || ruleId == LiveEventCalendarRuleIds.EndBeforeStart
                        || ruleId == LiveEventCalendarRuleIds.InvalidIdentifier || ruleId == LiveEventCalendarRuleIds.DuplicateEventId
                        || ruleId == LiveEventCalendarRuleIds.OverlapSameType || ruleId == LiveEventCalendarRuleIds.ShadowedByRecurring
                        || ruleId == LiveEventCalendarRuleIds.UnknownEventType || ruleId == LiveEventCalendarRuleIds.RunningEventIdChanged
                        || ruleId == LiveEventCalendarRuleIds.ConfigKeyMissing;
                case LiveOpsHubHealthTarget.RecurringRules:
                    return ruleId == LiveEventCalendarRuleIds.UtcTimeFormat || ruleId == LiveEventCalendarRuleIds.RecurringRuleInvalid
                        || ruleId == LiveEventCalendarRuleIds.RunningEventIdChanged || ruleId == LiveEventCalendarRuleIds.ConfigKeyMissing;
                case LiveOpsHubHealthTarget.EventTypes:
                    return ruleId == LiveEventCalendarRuleIds.UnknownEventType;
                default:
                    return true;
            }
        }

        private static LiveEventCalendarFinding FirstFinding(LiveEventCalendarCheckReport report, LiveOpsHubHealthTarget target)
        {
            foreach (LiveEventCalendarFinding finding in report.Findings)
            {
                if (!finding.IsIgnored && LiveOpsHubFindingRouting.BelongsTo(finding, target)) return finding;
            }
            return null;
        }

        private static string RemoteJsonWithUnknownType()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.DocumentWithoutPublishedStamp())
                .WithFixedEvent(new FixedLiveEventEntry("entry-remote-only", "remote-only-0919", "mystery-mode",
                    "2026-09-19T00:00:00Z", "2026-09-20T00:00:00Z", "mystery_v1"))
                .Build();
            return LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2).Text;
        }
    }
}
