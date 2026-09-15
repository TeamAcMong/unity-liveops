using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class RunningEventIdChangedRuleTests
    {
        // Đợt cố định đang chạy lúc NOW mẫu (13/9 08:47) trong bản so.
        private const string RunningStart = "2026-09-12T00:00:00Z";
        private const string RunningEnd = "2026-09-15T00:00:00Z";

        [Test]
        public void Passes_WhenConditionAbsent()
        {
            // Nháp giữ tiền tố weekly-pass-: lần lặp đang chạy weekly-pass-35 vẫn còn, sky-race không đổi.
            LiveEventCalendarDocument draft = ReplaceRule(LiveOpsDesignSample.Document, "weekly-pass", rule => rule.WithIdPrefix("weekly-pass-"));

            LiveEventCalendarRuleResult result = new RunningEventIdChangedRule().Evaluate(PublishedBaselineSample.ContextWithBaseline(draft));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, result.Outcome);
        }

        [Test]
        public void RunningEventIdChanged_NoBaseline_NotApplicable()
        {
            LiveEventCalendarRuleResult result = ValidationTestFixtures.Evaluate(new RunningEventIdChangedRule(), LiveOpsDesignSample.Document);

            Assert.AreEqual(LiveEventCalendarRuleOutcome.NotApplicable, result.Outcome, "PD-9: không có bản so thì không có đợt đang chạy để so.");
            Assert.AreEqual(RunningEventIdChangedRule.NoPublishedStampReasonCode, result.ReasonCode);
            Assert.AreEqual(0, result.Findings.Count);
        }

        [Test]
        public void Finds_DesignSampleCase()
        {
            LiveEventCalendarRuleResult result = new RunningEventIdChangedRule().Evaluate(PublishedBaselineSample.ContextWithBaseline(LiveOpsDesignSample.Document));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, result.Outcome);
            Assert.AreEqual(1, result.Findings.Count, "Chỉ weekly-pass: sky-race giữ id, lava-quest-2026-09a đã khép 13/9 00:00.");
            LiveEventCalendarFinding finding = result.Findings[0];
            Assert.AreEqual(LiveEventCalendarDetailCodes.RunningIdChanged, finding.DetailCode);
            Assert.AreEqual("weekly-pass", finding.TargetId);
            Assert.AreEqual("weekly-pass-35", finding.RelatedId);
            Assert.AreEqual("pass-35", finding.FoundText, "\"weekly-pass-35 → pass-35\" [SD2 §2.3 hàng 3].");
            Assert.AreEqual("weekly-pass-35", finding.ExpectedText);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-07T00:00:00Z"), finding.RangeStartUtc);
            Assert.AreEqual(ValidationTestFixtures.Utc("2026-09-14T00:00:00Z"), finding.RangeEndUtc, "\"đang chạy tới 14/9 00:00 UTC\".");
            Assert.AreEqual(LiveEventCalendarConsequence.ProgressLost, finding.Consequence);
        }

        [Test]
        public void Finding_HasTargetAndRepairKind()
        {
            LiveEventCalendarFinding finding = new RunningEventIdChangedRule().Evaluate(PublishedBaselineSample.ContextWithBaseline(LiveOpsDesignSample.Document)).Findings[0];

            Assert.AreEqual(LiveEventCalendarTargetKind.RecurringRule, finding.TargetKind);
            Assert.AreEqual("weekly-pass", finding.TargetEntryKey);
            Assert.IsFalse(finding.IsAboutRemoteSnapshot);
            Assert.AreEqual(LiveEventCalendarRepairKind.Decision, finding.RepairKind);
            // CC-VALA-3: số lựa chọn của luật 9 do gói này tự khoá — đúng hai: hoàn về, để sau khi khép.
            Assert.AreEqual(2, finding.Repairs.Count);
            Assert.AreEqual(RunningEventIdChangedRule.RevertRepairId, finding.Repairs[0].RepairId);
            Assert.AreEqual(RunningEventIdChangedRule.DeferUntilEndRepairId, finding.Repairs[1].RepairId);
            Assert.AreEqual(LiveEventCalendarRepairKind.Decision, finding.Repairs[0].Kind);
            Assert.AreEqual(LiveEventCalendarRepairKind.Decision, finding.Repairs[1].Kind);
        }

        [Test]
        public void DesignSample_RevertRestoresPrefix_RulePasses()
        {
            LiveEventCalendarDocument draft = LiveOpsDesignSample.Document;
            LiveEventCalendarFinding finding = new RunningEventIdChangedRule().Evaluate(PublishedBaselineSample.ContextWithBaseline(draft)).Findings[0];

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(draft, finding.Repairs[0].Edit, out LiveEventCalendarDocument reverted));

            Assert.IsTrue(reverted.TryGetRecurringRule("weekly-pass", out RecurringLiveEventRule rule));
            Assert.AreEqual("weekly-pass-", rule.IdPrefix);
            Assert.AreEqual("weekly_pass_s3", rule.ConfigKey, "Hoàn về chỉ đụng field quyết định id + khung, giữ configKey của nháp.");
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed,
                new RunningEventIdChangedRule().Evaluate(PublishedBaselineSample.ContextWithBaseline(reverted)).Outcome);
        }

        [Test]
        public void DesignSample_DeferUntilEnd_RevertsAndAddsReminderAtEnd()
        {
            LiveEventCalendarDocument draft = LiveOpsDesignSample.Document;
            LiveEventCalendarFinding finding = new RunningEventIdChangedRule().Evaluate(PublishedBaselineSample.ContextWithBaseline(draft)).Findings[0];

            CompositeCalendarEdit composite = finding.Repairs[1].Edit as CompositeCalendarEdit;
            Assert.IsNotNull(composite, "Để sau khi khép = hoàn về + ghi chú hẹn giờ trong MỘT Undo group.");
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(draft, composite, out LiveEventCalendarDocument deferred));

            Assert.AreEqual(1, deferred.IgnoredWarnings.Count);
            IgnoredCalendarWarning reminder = deferred.IgnoredWarnings[0];
            Assert.AreEqual(LiveEventCalendarRuleIds.RunningEventIdChanged, reminder.RuleId);
            Assert.AreEqual("weekly-pass", reminder.TargetId);
            Assert.AreEqual("2026-09-14T00:00:00Z", reminder.ExpiresUtcText, "Hẹn đúng lúc weekly-pass-35 khép.");
            Assert.AreEqual("2026-09-14T00:00:00Z", reminder.RangeStartUtcText, "Khoảng bắt đầu ở lúc khép — không phải khung đang chạy 7/9.");
            Assert.AreEqual(string.Empty, reminder.RangeEndUtcText);
            Assert.IsTrue(deferred.TryGetRecurringRule("weekly-pass", out RecurringLiveEventRule rule));
            Assert.AreEqual("weekly-pass-", rule.IdPrefix);
        }

        [Test]
        public void DeferUntilEnd_RedoBeforeEnd_StillProgressLost()
        {
            // "Để sau khi khép" rồi 13/9 09:00 (trước 14/9) đổi lại tiền tố: ghi chú hẹn không được ẩn phát hiện Mất tiến độ.
            LiveEventCalendarDocument deferred = ApplyDeferUntilEnd(LiveOpsDesignSample.Document);
            LiveEventCalendarDocument redone = ReplaceRule(deferred, "weekly-pass", rule => rule.WithIdPrefix("pass-"));
            System.DateTime beforeEndUtc = ValidationTestFixtures.Utc("2026-09-13T09:00:00Z");

            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(
                new LiveEventCalendarCheckContextBuilder(redone, beforeEndUtc).WithPublishedBaseline(PublishedBaselineSample.Document()).Build());

            Assert.AreEqual(1, report.Summary.ProgressLostCount);
            Assert.AreEqual(0, report.Summary.IgnoredCount);
            LiveEventCalendarFinding finding = DesignSampleCheckTests.FindingOf(report, LiveEventCalendarRuleIds.RunningEventIdChanged, "weekly-pass");
            Assert.IsFalse(finding.IsIgnored, "Luật 9 là Decision, không Ignorable — ghi chú hẹn chỉ nhắc, không che.");
            Assert.IsNull(finding.DueReminder, "Chưa tới hẹn.");
            CollectionAssert.Contains(report.Findings, finding, "Phát hiện vẫn ở Cần xử lý, vẫn chặn Copy JSON.");
        }

        [Test]
        public void DeferUntilEnd_RedoAfterEnd_NextOccurrenceCarriesDueReminder()
        {
            // Qua 14/9 00:00 mới làm lại: lần lặp nối tiếp weekly-pass-36 đang chạy vẫn Mất tiến độ, kèm cờ "đã tới hẹn" (S-22).
            LiveEventCalendarDocument deferred = ApplyDeferUntilEnd(LiveOpsDesignSample.Document);
            IgnoredCalendarWarning reminder = deferred.IgnoredWarnings[0];
            LiveEventCalendarDocument redone = ReplaceRule(deferred, "weekly-pass", rule => rule.WithIdPrefix("pass-"));
            System.DateTime afterEndUtc = ValidationTestFixtures.Utc("2026-09-14T01:00:00Z");

            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(
                new LiveEventCalendarCheckContextBuilder(redone, afterEndUtc).WithPublishedBaseline(PublishedBaselineSample.Document()).Build());

            LiveEventCalendarFinding finding = DesignSampleCheckTests.FindingOf(report, LiveEventCalendarRuleIds.RunningEventIdChanged, "weekly-pass");
            Assert.AreEqual("weekly-pass-36", finding.RelatedId);
            Assert.IsFalse(finding.IsIgnored);
            Assert.AreSame(reminder, finding.DueReminder);
        }

        [Test]
        public void RunningFixedRenamed_RevertRestoresIdOnSameEntry_NothingDropped()
        {
            LiveEventCalendarDocument baseline = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, RunningEnd));
            FixedLiveEventEntry renamed = ValidationTestFixtures.Entry("quest-0912b", "quest", RunningStart, RunningEnd);
            LiveEventCalendarDocument draft = ValidationTestFixtures.FixedDocument(renamed);

            LiveEventCalendarFinding finding = SingleFinding(draft, baseline);

            Assert.AreEqual(LiveEventCalendarDetailCodes.RunningRemoved, finding.DetailCode);
            ReplaceFixedEventEdit revert = finding.Repairs[0].Edit as ReplaceFixedEventEdit;
            Assert.IsNotNull(revert, "Đổi id tại chỗ: trả id cũ về chính mục đó — thêm bản sao sẽ bị bỏ vì chồng giờ với mục đổi tên.");
            Assert.AreEqual(renamed.EntryKey, revert.Entry.EntryKey);
            Assert.AreEqual("quest-0912", revert.Entry.EventId);
            AssertRevertPasses(draft, baseline, finding);

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(draft, revert, out LiveEventCalendarDocument reverted));
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(ContextFor(reverted, baseline));
            Assert.AreEqual(0, report.Summary.DroppedCount, "Hoàn về không được sinh thêm lỗi Bị bỏ.");
            Assert.AreEqual(0, report.Summary.ProgressLostCount);
        }

        [Test]
        public void RunningFixedDroppedAsDuplicateOfOtherType_NoRevertPromised()
        {
            LiveEventCalendarDocument baseline = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, RunningEnd));
            // Mục loại khác mang cùng id, bắt đầu sớm hơn nên đứng trước ở thứ tự xuất: game giữ nó, bỏ đợt đang chạy.
            LiveEventCalendarDocument draft = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, RunningEnd),
                ValidationTestFixtures.Entry("quest-0912", "hunt", "2026-09-11T00:00:00Z", "2026-09-12T00:00:00Z"));

            LiveEventCalendarRuleResult result = new RunningEventIdChangedRule().Evaluate(ContextFor(draft, baseline));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, result.Outcome);
            LiveEventCalendarFinding finding = result.Findings[0];
            Assert.AreEqual(LiveEventCalendarDetailCodes.RunningRemoved, finding.DetailCode);
            Assert.AreEqual(LiveEventCalendarConsequence.ProgressLost, finding.Consequence, "Vẫn Mất tiến độ, vẫn chặn.");
            Assert.AreEqual(LiveEventCalendarRepairKind.None, finding.RepairKind, "Thay giờ y nguyên không cứu được — không hứa hoàn về.");
            Assert.AreEqual(0, finding.Repairs.Count);
        }

        [Test]
        public void RunningEventIdChanged_NoRevertRecoversRunning_RepairKindNone_StillBlocksThroughValidator()
        {
            // V-21 CC-VALB-3: không lệnh hoàn về nào làm bản ghi tìm lại được đợt đang chạy → RepairKind None, nhưng qua validator
            // mặc định phát hiện vẫn Mất tiến độ, không bị bỏ qua, vẫn ở Cần xử lý (chặn Copy JSON) — không vì thiếu lệnh sửa mà nhẹ đi.
            LiveEventCalendarDocument baseline = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, RunningEnd));
            LiveEventCalendarDocument draft = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, RunningEnd),
                ValidationTestFixtures.Entry("quest-0912", "hunt", "2026-09-11T00:00:00Z", "2026-09-12T00:00:00Z"));

            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(ContextFor(draft, baseline));

            LiveEventCalendarFinding finding = DesignSampleCheckTests.FindingOf(report, LiveEventCalendarRuleIds.RunningEventIdChanged, "quest-0912");
            Assert.AreEqual(LiveEventCalendarConsequence.ProgressLost, finding.Consequence);
            Assert.AreEqual(LiveEventCalendarRepairKind.None, finding.RepairKind);
            Assert.AreEqual(0, finding.Repairs.Count, "Không có \"Quyết định…\" rỗng — câu sửa tay là việc của G-FINDINGTEXT.");
            Assert.IsFalse(finding.IsIgnored);
            Assert.IsNull(finding.DueReminder);
            // Đếm đúng một Mất tiến độ ở bộ tổng hợp (nguồn của tab Cần xử lý và cổng Copy JSON): thiếu lệnh sửa không làm phát hiện
            // rơi khỏi bộ đếm chặn. So bằng, không ">= 1", để phát hiện trùng hay rơi mất đều đỏ.
            Assert.AreEqual(1, report.Summary.ProgressLostCount);
        }

        [Test]
        public void RunningFixedRemoved_DeferUntilEnd_ReminderRangeStartsAtEndOpen_ExpiresAtEnd()
        {
            // V-21 CC-VALB-4 ở nhánh đợt cố định: ghi chú hẹn có khoảng [lúc khép, mở) và hạn = lúc khép; làm lại thay đổi trước giờ
            // khép thì ghi chú không che phát hiện.
            LiveEventCalendarDocument baseline = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, RunningEnd));
            LiveEventCalendarDocument draft = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0920", "quest", "2026-09-20T00:00:00Z", "2026-09-22T00:00:00Z"));
            LiveEventCalendarFinding finding = SingleFinding(draft, baseline);

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(draft, finding.Repairs[1].Edit, out LiveEventCalendarDocument deferred));

            Assert.AreEqual(1, deferred.IgnoredWarnings.Count);
            IgnoredCalendarWarning reminder = deferred.IgnoredWarnings[0];
            Assert.AreEqual(LiveEventCalendarRuleIds.RunningEventIdChanged, reminder.RuleId);
            Assert.AreEqual("quest-0912", reminder.TargetId);
            Assert.AreEqual(RunningEnd, reminder.RangeStartUtcText, "Khoảng bắt đầu ở lúc khép, không phải khung đang chạy 12/9.");
            Assert.AreEqual(string.Empty, reminder.RangeEndUtcText, "Khoảng mở phía sau.");
            Assert.AreEqual(RunningEnd, reminder.ExpiresUtcText, "Hạn = lúc khép.");
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, new RunningEventIdChangedRule().Evaluate(ContextFor(deferred, baseline)).Outcome,
                "Để sau khi khép cũng hoàn về ngay.");

            LiveEventCalendarDocument redone = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0920", "quest", "2026-09-20T00:00:00Z", "2026-09-22T00:00:00Z"));
            redone = new LiveEventCalendarDocumentBuilder(redone).WithIgnoredWarning(reminder).Build();
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(ContextFor(redone, baseline));
            LiveEventCalendarFinding redoneFinding = DesignSampleCheckTests.FindingOf(report, LiveEventCalendarRuleIds.RunningEventIdChanged, "quest-0912");
            Assert.IsFalse(redoneFinding.IsIgnored, "Trước giờ khép ghi chú hẹn không khớp khung đang chạy.");
            Assert.AreEqual(0, report.Summary.IgnoredCount);
        }

        [Test]
        public void RunningEventIdChanged_RunningFixedRemoved_Found()
        {
            LiveEventCalendarDocument baseline = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, RunningEnd));
            LiveEventCalendarDocument draft = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0920", "quest", "2026-09-20T00:00:00Z", "2026-09-22T00:00:00Z"));

            LiveEventCalendarFinding finding = SingleFinding(draft, baseline);

            Assert.AreEqual(LiveEventCalendarDetailCodes.RunningRemoved, finding.DetailCode);
            Assert.AreEqual(LiveEventCalendarTargetKind.FixedEvent, finding.TargetKind);
            Assert.AreEqual("quest-0912", finding.TargetId);
            Assert.AreEqual(string.Empty, finding.TargetEntryKey, "Nháp không còn mục mang id này.");
            Assert.AreEqual(string.Empty, finding.FoundText);
            Assert.AreEqual("quest-0912", finding.ExpectedText);
            Assert.IsInstanceOf<AddFixedEventEdit>(finding.Repairs[0].Edit, "Hoàn về = thêm lại đúng đợt của bản so.");

            AssertRevertPasses(draft, baseline, finding);
        }

        [Test]
        public void RunningFixedRetyped_Found()
        {
            LiveEventCalendarDocument baseline = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, RunningEnd));
            FixedLiveEventEntry retyped = ValidationTestFixtures.Entry("quest-0912", "hunt", RunningStart, RunningEnd);
            LiveEventCalendarDocument draft = ValidationTestFixtures.FixedDocument(retyped);

            LiveEventCalendarFinding finding = SingleFinding(draft, baseline);

            Assert.AreEqual(LiveEventCalendarDetailCodes.RunningRetyped, finding.DetailCode);
            Assert.AreEqual(retyped.EntryKey, finding.TargetEntryKey);
            Assert.AreEqual("hunt", finding.FoundText);
            Assert.AreEqual("quest", finding.ExpectedText);

            AssertRevertPasses(draft, baseline, finding);
        }

        [Test]
        public void RunningFixedMovedOut_Found()
        {
            LiveEventCalendarDocument baseline = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, RunningEnd));
            // Bắt đầu mới 16/9 > max(end cũ 15/9, now): bản ghi tìm trong [12/9, 15/9] không thấy.
            LiveEventCalendarDocument draft = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", "2026-09-16T00:00:00Z", "2026-09-18T00:00:00Z"));

            LiveEventCalendarFinding finding = SingleFinding(draft, baseline);

            Assert.AreEqual(LiveEventCalendarDetailCodes.RunningMovedOutOfWindow, finding.DetailCode);
            Assert.AreEqual("2026-09-16T00:00:00Z" + LiveEventCalendarFindingBuilder.ValueSeparator + "2026-09-18T00:00:00Z", finding.FoundText);
            Assert.AreEqual(RunningStart + LiveEventCalendarFindingBuilder.ValueSeparator + RunningEnd, finding.ExpectedText);

            AssertRevertPasses(draft, baseline, finding);
        }

        [Test]
        public void RunningFixedEndsNow_Found()
        {
            LiveEventCalendarDocument baseline = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, RunningEnd));
            // Kết thúc mới 13/9 08:00 ≤ now 08:47: bản ghi thấy đợt nhưng Refresh khép ngay.
            LiveEventCalendarDocument draft = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, "2026-09-13T08:00:00Z"));

            LiveEventCalendarFinding finding = SingleFinding(draft, baseline);

            Assert.AreEqual(LiveEventCalendarDetailCodes.RunningEndsNow, finding.DetailCode);
            AssertRevertPasses(draft, baseline, finding);
        }

        [Test]
        public void RunningFixedDroppedInDraft_Removed()
        {
            LiveEventCalendarDocument baseline = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, RunningEnd));
            // Cùng id + loại nhưng giờ kết thúc hỏng → game bỏ mục: với bản ghi là đợt biến mất.
            LiveEventCalendarDocument draft = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, "2026-09-1"));

            LiveEventCalendarFinding finding = SingleFinding(draft, baseline);

            Assert.AreEqual(LiveEventCalendarDetailCodes.RunningRemoved, finding.DetailCode);
            Assert.AreNotEqual(string.Empty, finding.TargetEntryKey, "Còn mục trong nháp — hàng trỏ về đúng mục để sửa.");
            AssertRevertPasses(draft, baseline, finding);
        }

        [Test]
        public void RunningFixedExtendedOrStartMovedBeforeNow_Passes()
        {
            LiveEventCalendarDocument baseline = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", RunningStart, RunningEnd));
            LiveEventCalendarDocument draft = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", "2026-09-11T00:00:00Z", "2026-09-20T00:00:00Z"));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, new RunningEventIdChangedRule().Evaluate(ContextFor(draft, baseline)).Outcome,
                "Kéo dài / dời bắt đầu vẫn trước now: bản ghi theo khung mới, không mất tiến độ.");
        }

        [Test]
        public void EndedOrUpcomingInBaseline_NotRunning_Passes()
        {
            LiveEventCalendarDocument baseline = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0901", "quest", "2026-09-01T00:00:00Z", "2026-09-13T08:47:00Z"),
                ValidationTestFixtures.Entry("quest-0920", "quest", "2026-09-20T00:00:00Z", "2026-09-22T00:00:00Z"));

            LiveEventCalendarRuleResult result = new RunningEventIdChangedRule().Evaluate(ContextFor(ValidationTestFixtures.FixedDocument(), baseline));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, result.Outcome, "end == now là đã khép; đợt sắp tới không có bản ghi.");
        }

        [Test]
        public void RecurringRuleRemoved_Found()
        {
            LiveEventCalendarDocument baseline = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("weekly-pass", "2026-01-05T00:00:00Z", "weekly-pass-", 168, 168, "weekly_pass_s3"))
                .Build();

            LiveEventCalendarFinding finding = SingleFinding(ValidationTestFixtures.FixedDocument(), baseline);

            Assert.AreEqual(LiveEventCalendarDetailCodes.RunningRemoved, finding.DetailCode);
            Assert.AreEqual(LiveEventCalendarTargetKind.RecurringRule, finding.TargetKind);
            Assert.AreEqual("weekly-pass-35", finding.RelatedId);
            SetRecurringRuleEdit restore = finding.Repairs[0].Edit as SetRecurringRuleEdit;
            Assert.IsNotNull(restore);
            Assert.AreEqual("weekly-pass-", restore.Rule.EffectiveIdPrefix);
        }

        [Test]
        public void RecurringActiveHoursShortenedToBeforeNow_EndsNow()
        {
            LiveEventCalendarDocument baseline = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "sky-race-", 24, 20, "sky_race_v4"))
                .Build();
            // Chạy 8 giờ: lần lặp 13/9 khép 08:00, trước now 08:47 — cùng id nhưng Refresh khép ngay.
            LiveEventCalendarDocument draft = ValidationTestFixtures.Document(new[]
            {
                new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "sky-race-", 24, 8, "sky_race_v4"),
            });

            LiveEventCalendarFinding finding = SingleFinding(draft, baseline);

            Assert.AreEqual(LiveEventCalendarDetailCodes.RunningEndsNow, finding.DetailCode);
            AssertRevertPasses(draft, baseline, finding);
        }

        [Test]
        public void CheckLane_OtherType_NoFinding()
        {
            LiveEventCalendarCheckContext context = PublishedBaselineSample.ContextWithBaseline(LiveOpsDesignSample.Document).ToBuilder()
                .WithOnlyEventType("treasure-hunt")
                .Build();

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, new RunningEventIdChangedRule().Evaluate(context).Outcome);
        }

        // ----- Hỗ trợ -----

        private static LiveEventCalendarCheckContext ContextFor(LiveEventCalendarDocument draft, LiveEventCalendarDocument baseline)
        {
            return new LiveEventCalendarCheckContextBuilder(draft, LiveOpsDesignSample.NowUtc).WithPublishedBaseline(baseline).Build();
        }

        private static LiveEventCalendarFinding SingleFinding(LiveEventCalendarDocument draft, LiveEventCalendarDocument baseline)
        {
            LiveEventCalendarRuleResult result = new RunningEventIdChangedRule().Evaluate(ContextFor(draft, baseline));
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, result.Outcome);
            Assert.AreEqual(1, result.Findings.Count);
            LiveEventCalendarFinding finding = result.Findings[0];
            Assert.AreEqual(LiveEventCalendarRepairKind.Decision, finding.RepairKind);
            Assert.AreEqual(2, finding.Repairs.Count);
            return finding;
        }

        /// <summary>Lựa chọn "hoàn về" phải thật sự làm bản ghi tìm lại được đợt — không thì nút Quyết định là lời hứa rỗng.</summary>
        private static void AssertRevertPasses(LiveEventCalendarDocument draft, LiveEventCalendarDocument baseline, LiveEventCalendarFinding finding)
        {
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(draft, finding.Repairs[0].Edit, out LiveEventCalendarDocument reverted), finding.DetailCode);
            LiveEventCalendarRuleResult after = new RunningEventIdChangedRule().Evaluate(ContextFor(reverted, baseline));
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, after.Outcome, finding.DetailCode + " sau khi hoàn về vẫn còn phát hiện.");
        }

        private static LiveEventCalendarDocument ApplyDeferUntilEnd(LiveEventCalendarDocument draft)
        {
            LiveEventCalendarFinding finding = new RunningEventIdChangedRule().Evaluate(PublishedBaselineSample.ContextWithBaseline(draft)).Findings[0];
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(draft, finding.Repairs[1].Edit, out LiveEventCalendarDocument deferred));
            return deferred;
        }

        private static LiveEventCalendarDocument ReplaceRule(LiveEventCalendarDocument document, string eventType,
            System.Func<RecurringLiveEventRule, RecurringLiveEventRule> change)
        {
            Assert.IsTrue(document.TryGetRecurringRule(eventType, out RecurringLiveEventRule rule));
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new SetRecurringRuleEdit(change(rule)), out LiveEventCalendarDocument result));
            return result;
        }
    }
}
