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

        // Số của thiết kế ([SD2 §2.1] "Tất cả 6 | Bị bỏ 2 | Mất tiến độ 1 | Nên xem 2 | Chưa kiểm 1", "6 luật đã qua").
        // Ghim số TUYỆT ĐỐI chứ không so lại với chính báo cáo vừa sinh ra model: so với chính nó thì model đếm sai kiểu gì
        // cũng xanh, và bảng tab của hub sẽ nói dối đúng như validator nói dối.
        private const int DesignAllTabCount = 6;
        private const int DesignDroppedCount = 2;
        private const int DesignProgressLostCount = 1;
        private const int DesignShouldReviewCount = 2;
        private const int DesignNotMeasuredCount = 1;
        private const int DesignPassedRuleCount = 6;

        private const string CleanEventType = "treasure-hunt";
        private const string CleanEventTypeName = "Săn kho báu";
        private const string CleanConfigKey = "hunt_default";
        private const int CleanColorSlot = 2;

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

            Assert.AreEqual(DesignAllTabCount, model.Tabs[0].Count, "[SD2 §2.1] tab \"Tất cả 6\"");
            Assert.AreEqual(DesignDroppedCount, TotalOf(model, ValidationGroupKind.Dropped), "[SD2 §2.1] \"Bị bỏ 2\"");
            Assert.AreEqual(DesignProgressLostCount, TotalOf(model, ValidationGroupKind.ProgressLost), "[SD2 §2.1] \"Mất tiến độ 1\"");
            Assert.AreEqual(DesignShouldReviewCount, TotalOf(model, ValidationGroupKind.ShouldReview), "[SD2 §2.1] \"Nên xem 2\"");
            Assert.AreEqual(DesignNotMeasuredCount, TotalOf(model, ValidationGroupKind.NotMeasured), "[SD2 §2.1] \"Chưa kiểm 1\"");
            Assert.AreEqual(DesignPassedRuleCount, TotalOf(model, ValidationGroupKind.Passed), "[SD2 §2.1] card \"6 luật đã qua\"");
        }

        [Test]
        public void StaleByEdit_NoticeShowsBothMomentsWithSeconds()
        {
            // [SD2 §2.8]: "Lịch đã đổi lúc 08:46:50 UTC, sau lần kiểm 08:46:30." Hai mốc cách nhau 20 GIÂY — in bằng giờ chỉ
            // tới phút thì câu thành "đã đổi lúc 08:46, sau lần kiểm 08:46" và mất hẳn nghĩa "sau".
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.StaleCheckScenario);
            Assume.That(services.Session.Check.StaleReason, Is.EqualTo(LiveOpsHubCheckStaleReason.CalendarEdited));

            ValidationViewModel model = Build(services);

            Assert.AreEqual(ValidationBodyState.Stale, model.BodyState);
            string editedAt = ClockWithSeconds(LiveOpsHubTestServices.EditedAfterCheckUtc);
            string checkedAt = ClockWithSeconds(LiveOpsHubTestServices.CheckedAtUtc);
            Assert.AreNotEqual(editedAt, checkedAt, "fixture phải có hai mốc khác nhau, nếu không test không chứng minh được gì");
            StringAssert.Contains(editedAt, model.StaleNotice, "câu phải nêu giờ lịch đổi, có giây");
            StringAssert.Contains(checkedAt, model.StaleNotice, "câu phải nêu giờ lần kiểm, có giây");
            StringAssert.Contains(checkedAt, model.SummaryRightText, "câu phải của dải summary cũng in giây ([SD2 §2.1])");
        }

        [Test]
        public void NoErrors_KeepsNotMeasuredGroupBesideEmptyBlock()
        {
            // [SD2 §2.8] "Không còn lỗi" = khối empty + hàng Chưa kiểm VẪN Ở LẠI. Nhóm Chưa kiểm là chỗ duy nhất nói
            // "luật này không tính là đã qua" và là chỗ duy nhất có nút "Dán JSON đang chạy…".
            LiveOpsHubServices services = ServicesFor(CleanDocument());
            services.Session.RunCheckToCompletion();

            ValidationViewModel model = Build(services);

            Assert.AreEqual(ValidationBodyState.NoErrors, model.BodyState);
            Assert.AreEqual(0, TotalOf(model, ValidationGroupKind.Dropped));
            Assert.AreEqual(0, TotalOf(model, ValidationGroupKind.ProgressLost));
            Assert.AreEqual(0, TotalOf(model, ValidationGroupKind.ShouldReview));
            Assert.Greater(TotalOf(model, ValidationGroupKind.NotMeasured), 0, "remote-snapshot-drift chưa dán JSON nên vẫn là một luật chưa kiểm");
            Assert.Greater(RowsOf(model, ValidationGroupKind.NotMeasured).Count, 0, "hàng của nhóm Chưa kiểm phải còn để vẽ");
        }

        [Test]
        public void ShouldReviewOnly_KeepsCardsInsteadOfEmptyBlock()
        {
            // [SD2 §4 mục 23]: hết Bị bỏ / Mất tiến độ mà còn Nên xem thì KHÔNG dùng empty — card vẫn vẽ, chỉ thêm dòng note Ok.
            LiveOpsHubServices services = ServicesFor(ShouldReviewOnlyDocument());
            services.Session.RunCheckToCompletion();

            ValidationViewModel model = Build(services);

            Assert.AreEqual(ValidationBodyState.ShouldReviewOnly, model.BodyState);
            Assert.AreEqual(0, TotalOf(model, ValidationGroupKind.Dropped));
            Assert.AreEqual(0, TotalOf(model, ValidationGroupKind.ProgressLost));
            Assert.Greater(RowsOf(model, ValidationGroupKind.ShouldReview).Count, 0, "phải còn hàng Nên xem để vẽ card");
        }

        [Test]
        public void IgnorableFinding_HasNeitherButtonNorLink()
        {
            // Mục 12 I-7: W4 ẩn hẳn "Bỏ qua cảnh báo…". Đổ chữ nút vào link sẽ ra một link mang chính chữ đó mà bấm vào lại
            // nhảy sang màn Lịch — hàng nói một đằng làm một nẻo.
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            ValidationViewModel model = Build(services);
            ValidationRow ignorableRow = FindIgnorableRow(model);

            Assume.That(ignorableRow, Is.Not.Null, "lịch mẫu có một cảnh báo bỏ qua được (long gap)");
            Assert.AreEqual(ValidationRowAction.None, ignorableRow.Action);
            Assert.IsEmpty(ignorableRow.ActionText);
            Assert.IsEmpty(ignorableRow.LinkText, "W4 không có nút lẫn link cho hàng Bỏ qua cảnh báo…");
        }

        private static ValidationRow FindIgnorableRow(ValidationViewModel model)
        {
            foreach (ValidationGroup group in model.Groups)
            {
                foreach (ValidationRow row in group.Rows)
                {
                    if (row.Finding != null && row.Finding.RepairKind == LiveEventCalendarRepairKind.Ignorable) return row;
                }
            }
            return null;
        }

        /// <summary>Cùng khuôn giờ có giây mà status bar và màn Kiểm lịch dùng — test không được tự viết một khuôn khác.</summary>
        private static string ClockWithSeconds(DateTime utc)
        {
            return utc.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Lịch sạch: một loại, một đợt tương lai TỰ khai config key — không luật nào có gì để nói.</summary>
        private static LiveEventCalendarDocument CleanDocument()
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(CleanEventType, CleanEventTypeName, CleanColorSlot, false, CleanConfigKey))
                .WithFixedEvent(new FixedLiveEventEntry("entry-hunt-clean", "hunt-1001", CleanEventType,
                    "2026-10-01T00:00:00Z", "2026-10-02T00:00:00Z", "hunt_v1"))
                .Build();
        }

        /// <summary>Cùng lịch đó nhưng đợt KHÔNG tự khai config key: chỉ còn một phát hiện Nên xem.</summary>
        private static LiveEventCalendarDocument ShouldReviewOnlyDocument()
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(CleanEventType, CleanEventTypeName, CleanColorSlot, false, CleanConfigKey))
                .WithFixedEvent(new FixedLiveEventEntry("entry-hunt-inherit", "hunt-1001", CleanEventType,
                    "2026-10-01T00:00:00Z", "2026-10-02T00:00:00Z", string.Empty))
                .Build();
        }

        private static LiveOpsHubServices ServicesFor(LiveEventCalendarDocument document)
        {
            return LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document)));
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
            // Mốc là một điểm trên lịch (ngày + giờ), lần kiểm là một thời điểm trong phiên nên in có giây ([SD2 §2.8]).
            StringAssert.Contains(ClockWithSeconds(check.LastReport.CheckedAtUtc), model.StaleNotice);
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
