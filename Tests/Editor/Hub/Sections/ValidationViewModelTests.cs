using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Model thuần của màn Kiểm lịch (mục 7.5, [SD2 §2]). Trọng tâm: mọi con số của màn đến từ CÙNG bộ tổng hợp với rail và
    /// Tổng quan ([SD2 §1.3]), và không một câu nào của phát hiện được màn tự viết — tất cả phải trùng
    /// <see cref="LiveOpsFindingText"/> tới từng ký tự (V-8).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class ValidationViewModelTests
    {
        private const string RemoteOnlyType = "lucky-spin";
        private const string WeeklyPassType = "weekly-pass";
        private const string RunningEventId = "weekly-pass-35";
        private const string RenamedRunningEventId = "pass-35";

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
        public void Groups_MatchSummary()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarCheckReport report = services.Session.Check.LastReport;
            ValidationViewModel model = Build(services);

            Assert.AreEqual(report.Summary.DroppedCount, TotalOf(model, ValidationGroupKind.Dropped), "card Bị bỏ phải đếm đúng bộ tổng hợp");
            Assert.AreEqual(report.Summary.ProgressLostCount, TotalOf(model, ValidationGroupKind.ProgressLost));
            Assert.AreEqual(report.Summary.ShouldReviewCount, TotalOf(model, ValidationGroupKind.ShouldReview));
            Assert.AreEqual(report.Summary.NotMeasuredRuleCount, TotalOf(model, ValidationGroupKind.NotMeasured));
            Assert.AreEqual(report.Summary.PassedRuleCount, TotalOf(model, ValidationGroupKind.Passed));

            int findingCount = report.Summary.DroppedCount + report.Summary.ProgressLostCount + report.Summary.ShouldReviewCount;
            Assert.AreEqual(findingCount + report.Summary.NotMeasuredRuleCount, model.Tabs[0].Count,
                "tab \"Tất cả\" = phát hiện + luật chưa kiểm ([SD2 §1.3])");
            Assert.AreEqual(report.Summary.DroppedCount, model.Tabs[1].Count);
            Assert.AreEqual(report.RuleResults.Count, report.Summary.PassedRuleCount + report.Summary.NotMeasuredRuleCount
                + report.Summary.NotApplicableRuleCount + RulesWithFindings(report), "mỗi luật chỉ được đếm một lần");
            Assert.AreEqual(5, model.SummaryParts.Count, "dải summary đúng năm cặp dấu + số");
        }

        [Test]
        public void NavigationFilter_Dropped()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            ValidationViewModel filtered = ValidationViewModel.Build(services.Session.Check,
                ValidationFilter.All.WithConsequence(LiveOpsHubNavigation.FilterDropped), services.Format, ContextOf(services));

            Assert.Greater(RowsOf(filtered, ValidationGroupKind.Dropped).Count, 0, "lọc Bị bỏ phải còn hàng Bị bỏ");
            Assert.AreEqual(0, RowsOf(filtered, ValidationGroupKind.ShouldReview).Count, "lọc Bị bỏ thì nhóm Nên xem không còn hàng nào");
            Assert.AreEqual(0, RowsOf(filtered, ValidationGroupKind.NotMeasured).Count, "luật chưa kiểm không thuộc nhóm Bị bỏ");
            Assert.AreEqual(TotalOf(Build(services), ValidationGroupKind.ShouldReview), TotalOf(filtered, ValidationGroupKind.ShouldReview),
                "bộ lọc đổi HÀNG hiện ra, không đổi CON SỐ của bộ tổng hợp");
        }

        [Test]
        public void Rows_UseLiveOpsFindingTextOnly()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            ValidationTextContext context = ContextOf(services);
            ValidationViewModel model = Build(services);
            int checkedRows = 0;

            foreach (ValidationGroup group in model.Groups)
            {
                foreach (ValidationRow row in group.Rows)
                {
                    if (row.Finding == null) continue;
                    checkedRows++;
                    Assert.AreEqual(LiveOpsFindingText.Headline(row.Finding, services.Format, context.LatestStamp), row.Headline,
                        "(V-8) headline phải là CHÍNH câu của LiveOpsFindingText, có dấu đã đăng (CC-FT-1)");
                    Assert.AreEqual(LiveOpsFindingText.Meta(row.Finding, services.Format, context.NowUtc), row.MetaText,
                        "(CC-FT-1) meta phải dùng overload có đồng hồ");
                    Assert.AreEqual(LiveOpsFindingText.RuleIdLine(row.Finding), row.RuleIdLine);
                    Assert.AreEqual(LiveOpsFindingText.PrimaryButtonTooltip(row.Finding, services.Format, services.Session.AssetFileName),
                        row.ActionTooltip, "(CC-FT-1) tooltip nút phải biết tên asset lịch đang mở");
                }
            }
            Assert.Greater(checkedRows, 0, "lịch mẫu phải có hàng phát hiện để so câu");
        }

        [Test]
        public void RemoteDriftRow_UsesViewDiffAction()
        {
            // Hàng "Xem diff" chỉ có nghĩa khi bản remote đã dán và LỆCH bản đã đăng; model phải đưa ra đúng động từ đó.
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            services.Session.Remote.Set(RemoteJsonWithLuckySpin(), LiveOpsDesignSample.NowUtc);
            services.Session.RunCheckToCompletion();

            ValidationRow driftRow = FindRow(Build(services), LiveEventCalendarRuleIds.RemoteSnapshotDrift);

            Assert.IsNotNull(driftRow, "dán JSON lệch phải ra một hàng của luật remote-snapshot-drift");
            Assert.AreEqual(ValidationRowAction.ViewDiff, driftRow.Action, "(V-13) động từ của hàng lệch bản remote là \"Xem diff\"");
            Assert.AreEqual(LiveOpsHubStrings.FindingButtonViewDiff, driftRow.ActionText);
        }

        [Test]
        public void RemoteOnlyUnknownType_InShouldReviewWithRemoteMeta()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            services.Session.Remote.Set(RemoteJsonWithLuckySpin(), LiveOpsDesignSample.NowUtc);
            services.Session.RunCheckToCompletion();

            ValidationViewModel model = Build(services);
            ValidationRow unknownTypeRow = FindRow(model, LiveEventCalendarRuleIds.UnknownEventType);

            Assert.IsNotNull(unknownTypeRow, "(V-17) loại chỉ có trong bản dán vẫn phải thành một hàng của Kiểm lịch");
            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, unknownTypeRow.Finding.Consequence,
                "(V-17) loại lạ CHỈ có ở bản remote là Nên xem — nháp không có đợt nào bị bỏ vì nó");
            Assert.Contains(unknownTypeRow, new List<ValidationRow>(RowsOf(model, ValidationGroupKind.ShouldReview)),
                "(V-17) hàng phải nằm trong card Nên xem");
            Assert.AreEqual(LiveOpsFindingText.Meta(unknownTypeRow.Finding, services.Format, LiveOpsDesignSample.NowUtc),
                unknownTypeRow.MetaText, "meta nói rõ đây là chuyện của bản remote");
            StringAssert.Contains(RemoteOnlyType, unknownTypeRow.RuleIdLine);
        }

        [Test]
        public void StaleByMilestone_ShowsMilestoneNotice()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveOpsHubCheckState check = services.Session.Check;
            // Thời gian đi qua mốc mở/khép của một đợt: luật 9 và 11 phụ thuộc "bây giờ" nên kết quả cũ không còn là bằng chứng.
            check.Reevaluate(LiveOpsDesignSample.NowUtc.AddDays(MilestoneScanDays));

            Assume.That(check.StaleReason, Is.EqualTo(LiveOpsHubCheckStaleReason.MilestonePassed));
            ValidationViewModel model = Build(services);

            Assert.AreEqual(ValidationBodyState.Stale, model.BodyState);
            Assert.IsNotEmpty(model.StaleNotice);
            StringAssert.Contains(services.Format.ShortDateTimeUtc(check.PassedMilestoneUtc.Value), model.StaleNotice,
                "câu phải nêu đúng mốc đã qua, không chỉ nói chung chung là cũ");
            StringAssert.Contains(services.Format.ShortDateTimeUtc(check.LastReport.CheckedAtUtc), model.StaleNotice);
        }

        /// <summary>Đủ xa để vượt mốc gần nhất của lịch mẫu (đợt 14/9 và 16/9 nằm trong tầm này).</summary>
        private const int MilestoneScanDays = 30;

        [Test]
        public void RunningIdChanged_NoRepair_NoDecisionButton_LinkToCalendar()
        {
            // (V-21 CC-VALB-3) Luật 9 có thể ra phát hiện KHÔNG lệnh sửa nào. Hàng khi đó không được có nút chết: chỉ link + câu sửa tay.
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarFinding finding = RunningIdChangedWithoutRepair();
            ValidationRow row = ValidationViewModel.RowFor(finding, services.Format, ContextOf(services));

            Assert.AreEqual(ValidationRowAction.None, row.Action, "không lệnh sửa thì không có động từ nào bấm được");
            Assert.IsEmpty(row.ActionText);
            Assert.AreEqual(LiveOpsFindingText.PrimaryButtonText(finding), row.LinkText, "hàng vẫn phải có đường đi tới chỗ sửa tay");
            Assert.IsNotEmpty(row.LinkText);
            Assert.AreEqual(LiveOpsFindingText.ManualFixSentence(finding, services.Format), row.ManualFixSentence);
            Assert.IsNotEmpty(row.ManualFixSentence, "(CC-VALB-3) câu phải chỉ cách sửa tay cụ thể");
        }

        private static LiveEventCalendarFinding RunningIdChangedWithoutRepair()
        {
            return new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.RunningEventIdChanged,
                    LiveEventCalendarDetailCodes.RunningIdChanged, LiveEventCalendarConsequence.ProgressLost,
                    LiveEventCalendarTargetKind.RecurringRule, WeeklyPassType)
                .WithRelatedId(RunningEventId)
                .WithTexts(RunningEventId, RenamedRunningEventId)
                .WithRange(LiveOpsDesignSample.NowUtc.AddHours(-1), LiveOpsDesignSample.NowUtc.AddHours(1))
                .Build();
        }

        private static ValidationViewModel Build(LiveOpsHubServices services)
        {
            return ValidationViewModel.Build(services.Session.Check, ValidationFilter.All, services.Format, ContextOf(services));
        }

        private static ValidationTextContext ContextOf(LiveOpsHubServices services)
        {
            return new ValidationTextContext(services.Session.Document, services.Clock.UtcNow, services.Session.AssetFileName);
        }

        private static int TotalOf(ValidationViewModel model, ValidationGroupKind kind)
        {
            foreach (ValidationGroup group in model.Groups)
            {
                if (group.Kind == kind) return group.TotalCount;
            }
            return -1;
        }

        private static IReadOnlyList<ValidationRow> RowsOf(ValidationViewModel model, ValidationGroupKind kind)
        {
            foreach (ValidationGroup group in model.Groups)
            {
                if (group.Kind == kind) return group.Rows;
            }
            return Array.Empty<ValidationRow>();
        }

        private static ValidationRow FindRow(ValidationViewModel model, string ruleId)
        {
            foreach (ValidationGroup group in model.Groups)
            {
                foreach (ValidationRow row in group.Rows)
                {
                    if (row.Finding != null && string.Equals(row.Finding.RuleId, ruleId, StringComparison.Ordinal)) return row;
                }
            }
            return null;
        }

        private static int RulesWithFindings(LiveEventCalendarCheckReport report)
        {
            int count = 0;
            foreach (LiveEventCalendarRuleResult result in report.RuleResults)
            {
                if (result.Outcome == LiveEventCalendarRuleOutcome.Found) count++;
            }
            return count;
        }

        /// <summary>Bản dán có hai đợt loại <c>lucky-spin</c> mà nháp không khai (Hình 10b) — cùng fixture với EventTypesModelTests.</summary>
        private static string RemoteJsonWithLuckySpin()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.DocumentWithoutPublishedStamp())
                .WithFixedEvent(new FixedLiveEventEntry("entry-lucky-1", "lucky-spin-0919", RemoteOnlyType,
                    "2026-09-19T00:00:00Z", "2026-09-20T00:00:00Z", "lucky_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-lucky-2", "lucky-spin-0926", RemoteOnlyType,
                    "2026-09-26T00:00:00Z", "2026-09-27T00:00:00Z", "lucky_v1"))
                .Build();
            return LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2).Text;
        }
    }
}
