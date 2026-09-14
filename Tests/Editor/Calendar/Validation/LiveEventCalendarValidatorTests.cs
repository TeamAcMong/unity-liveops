using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class LiveEventCalendarValidatorTests
    {
        // ----- Thứ tự luật, chạy từng bước -----

        [Test]
        public void RuleOrder_MatchesRuleIdsAll()
        {
            IReadOnlyList<ILiveEventCalendarRule> rules = LiveEventCalendarValidator.Default.Rules;
            IReadOnlyList<string> expectedOrder = LiveEventCalendarRuleIds.All;

            Assert.AreEqual(LiveEventCalendarValidator.RuleCount, rules.Count);
            Assert.AreEqual(LiveEventCalendarValidator.RuleCount, expectedOrder.Count);
            for (int index = 0; index < rules.Count; index++)
            {
                Assert.AreEqual(expectedOrder[index], rules[index].RuleId, "Luật thứ " + (index + 1) + " lệch thứ tự RuleIds.All.");
            }
        }

        [Test]
        public void CheckRun_StepsOneRulePerCall()
        {
            var validator = new LiveEventCalendarValidator(new ILiveEventCalendarRule[]
            {
                ValidationTestFixtures.PassingRule(LiveEventCalendarRuleIds.UtcTimeFormat),
                ValidationTestFixtures.PassingRule(LiveEventCalendarRuleIds.EndBeforeStart),
                ValidationTestFixtures.PassingRule(LiveEventCalendarRuleIds.InvalidIdentifier),
            });
            LiveEventCalendarCheckRun run = validator.BeginCheck(ValidationTestFixtures.ContextFor(LiveOpsDesignSample.Document));

            Assert.AreEqual(3, run.RuleCount);
            Assert.AreEqual(0, run.CompletedRuleCount);
            Assert.AreEqual(LiveEventCalendarRuleIds.UtcTimeFormat, run.CurrentRuleId);
            Assert.IsNull(run.Report);

            Assert.IsTrue(run.Step());
            Assert.AreEqual(1, run.CompletedRuleCount);
            Assert.AreEqual(LiveEventCalendarRuleIds.EndBeforeStart, run.CurrentRuleId);
            Assert.IsFalse(run.IsComplete);
            Assert.IsNull(run.Report, "Báo cáo chỉ có khi đã chạy hết — hub không hiện kết quả nửa vời.");

            Assert.IsTrue(run.Step());
            Assert.IsTrue(run.Step());
            Assert.IsTrue(run.IsComplete);
            Assert.AreEqual(string.Empty, run.CurrentRuleId);
            Assert.IsNotNull(run.Report);
            Assert.AreEqual(3, run.Report.RuleResults.Count);
            Assert.AreEqual(LiveOpsDesignSample.NowUtc, run.Report.CheckedAtUtc);

            Assert.IsFalse(run.Step(), "Đã chạy hết thì Step không làm gì.");
            Assert.AreEqual(3, run.CompletedRuleCount);
        }

        [Test]
        public void ThrowingRule_BecomesFailed_OtherRulesStillRun()
        {
            var validator = new LiveEventCalendarValidator(new ILiveEventCalendarRule[]
            {
                ValidationTestFixtures.PassingRule(LiveEventCalendarRuleIds.UtcTimeFormat),
                new FakeCalendarRule(LiveEventCalendarRuleIds.LongGapBetweenEvents, LiveEventCalendarConsequence.ShouldReview,
                    context => throw new NullReferenceException("giả lập luật hỏng")),
                ValidationTestFixtures.PassingRule(LiveEventCalendarRuleIds.RemoteSnapshotDrift),
            });

            LiveEventCalendarCheckReport report = validator.Check(ValidationTestFixtures.ContextFor(LiveOpsDesignSample.Document));

            Assert.AreEqual(3, report.RuleResults.Count);
            LiveEventCalendarRuleResult failed = report.RuleResults[1];
            Assert.AreEqual(LiveEventCalendarRuleIds.LongGapBetweenEvents, failed.RuleId);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Failed, failed.Outcome);
            Assert.AreEqual("NullReferenceException", failed.ExceptionTypeName);
            Assert.AreEqual("giả lập luật hỏng", failed.ExceptionMessage);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, report.RuleResults[2].Outcome, "Luật sau luật ném vẫn chạy.");
            Assert.AreEqual(1, report.Summary.NotMeasuredRuleCount, "Luật ném đếm vào chưa kiểm, không vào đã qua.");
            Assert.AreEqual(2, report.Summary.PassedRuleCount);
        }

        [Test]
        public void NullResultRule_BecomesFailed()
        {
            var validator = new LiveEventCalendarValidator(new ILiveEventCalendarRule[]
            {
                new FakeCalendarRule(LiveEventCalendarRuleIds.UtcTimeFormat, LiveEventCalendarConsequence.Dropped, context => null),
            });

            LiveEventCalendarCheckReport report = validator.Check(ValidationTestFixtures.ContextFor(LiveOpsDesignSample.Document));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Failed, report.RuleResults[0].Outcome);
        }

        [Test]
        public void Constructor_DuplicateRuleIds_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LiveEventCalendarValidator(new ILiveEventCalendarRule[]
            {
                ValidationTestFixtures.PassingRule(LiveEventCalendarRuleIds.UtcTimeFormat),
                ValidationTestFixtures.PassingRule(LiveEventCalendarRuleIds.UtcTimeFormat),
            }));
        }

        // ----- Kiểm nhanh một làn -----

        [Test]
        public void CheckLane_OnlyTargetsThatType_DoesNotIncludeRemote()
        {
            LiveEventCalendarDocument remote = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(ValidationTestFixtures.Entry("lucky-spin-1", "lucky-spin", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"))
                .Build();
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(LiveOpsDesignSample.Document, LiveOpsDesignSample.NowUtc)
                .WithRemoteSnapshot(remote, "remote-sha")
                .Build();

            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.CheckLane(context, "treasure-hunt");

            // Chỉ đợt của làn treasure-hunt (hunt-0914 có thể có phát hiện configKey khi luật 10 có thật); lava-quest hỏng giờ không thuộc làn.
            bool foundOverlap = false;
            for (int index = 0; index < report.Findings.Count; index++)
            {
                LiveEventCalendarFinding finding = report.Findings[index];
                CollectionAssert.Contains(new[] { "hunt-0914", "hunt-0916-bonus" }, finding.TargetId, finding.RuleId);
                Assert.IsFalse(finding.IsAboutRemoteSnapshot);
                if (finding.RuleId == LiveEventCalendarRuleIds.OverlapSameType && finding.TargetId == "hunt-0916-bonus") foundOverlap = true;
            }
            Assert.IsTrue(foundOverlap, "Kiểm nhanh làn vẫn báo đợt sẽ bị bỏ vì chồng giờ.");
            Assert.AreEqual(1, report.Summary.DroppedCount);
            Assert.AreEqual(0, report.Summary.RemoteSnapshotFindingCount);
            for (int index = 0; index < report.RuleResults.Count; index++)
            {
                string ruleId = report.RuleResults[index].RuleId;
                Assert.AreNotEqual(LiveEventCalendarRuleIds.UnknownEventType, ruleId);
                Assert.AreNotEqual(LiveEventCalendarRuleIds.RemoteSnapshotDrift, ruleId);
                Assert.AreNotEqual(LiveEventCalendarRuleIds.LongGapBetweenEvents, ruleId);
                Assert.AreNotEqual(LiveEventCalendarRuleIds.RunningEventIdChanged, ruleId);
            }
            Assert.AreEqual(string.Empty, context.OnlyEventType, "Kiểm nhanh không đổi ngữ cảnh gốc.");
        }

        // ----- Bỏ qua, ghi chú hẹn giờ -----

        [Test]
        public void IgnoredWarning_MatchingRange_MovedToIgnored()
        {
            var warning = new IgnoredCalendarWarning(LiveEventCalendarRuleIds.LongGapBetweenEvents, "lava-quest",
                "2026-09-20T00:00:00Z", "2026-10-01T00:00:00Z", "tháng 9 nghỉ", string.Empty);

            LiveEventCalendarCheckReport report = ValidationTestFixtures.CheckWithLongGapFinding(warning,
                ValidationTestFixtures.Utc("2026-09-20T00:00:00Z"), ValidationTestFixtures.Utc("2026-10-01T00:00:00Z"));

            Assert.AreEqual(0, report.Findings.Count);
            Assert.AreEqual(1, report.IgnoredFindings.Count);
            Assert.IsTrue(report.IgnoredFindings[0].IsIgnored);
            Assert.AreSame(warning, report.IgnoredFindings[0].IgnoredBy, "V-15: Bỏ bỏ qua dùng thẳng mục cảnh báo đã khớp.");
            Assert.AreEqual(1, report.Summary.IgnoredCount);
            Assert.AreEqual(0, report.Summary.NeedsActionCount);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, report.RuleResults[0].Outcome, "Bỏ qua không biến luật thành đã qua.");
        }

        [Test]
        public void IgnoredWarning_RangeChanged_ShowsAgain()
        {
            var warning = new IgnoredCalendarWarning(LiveEventCalendarRuleIds.LongGapBetweenEvents, "lava-quest",
                "2026-09-20T00:00:00Z", "2026-09-30T00:00:00Z", "tháng 9 nghỉ", string.Empty);

            LiveEventCalendarCheckReport report = ValidationTestFixtures.CheckWithLongGapFinding(warning,
                ValidationTestFixtures.Utc("2026-09-20T00:00:00Z"), ValidationTestFixtures.Utc("2026-10-01T00:00:00Z"));

            Assert.AreEqual(1, report.Findings.Count, "Khoảng đổi → phát hiện hiện lại.");
            Assert.IsFalse(report.Findings[0].IsIgnored);
            Assert.IsNull(report.Findings[0].IgnoredBy);
            Assert.AreEqual(0, report.Summary.IgnoredCount);
        }

        [Test]
        public void IgnoredWarning_EmptyRange_MatchesAnyRange()
        {
            var warning = new IgnoredCalendarWarning(LiveEventCalendarRuleIds.LongGapBetweenEvents, "lava-quest",
                string.Empty, string.Empty, "mọi khoảng", string.Empty);

            LiveEventCalendarCheckReport report = ValidationTestFixtures.CheckWithLongGapFinding(warning,
                ValidationTestFixtures.Utc("2026-09-20T00:00:00Z"), ValidationTestFixtures.Utc("2026-10-01T00:00:00Z"));

            Assert.AreEqual(1, report.IgnoredFindings.Count);
        }

        [Test]
        public void IgnoredWarning_DroppedFinding_NeverIgnored()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.Document)
                .WithIgnoredWarning(new IgnoredCalendarWarning(LiveEventCalendarRuleIds.OverlapSameType, "hunt-0916-bonus",
                    string.Empty, string.Empty, "cố tình", string.Empty))
                .Build();

            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(ValidationTestFixtures.ContextFor(document));

            Assert.AreEqual(0, report.Summary.IgnoredCount, "Bị bỏ không bao giờ ẩn được — game vẫn bỏ đợt.");
            Assert.AreEqual(2, report.Summary.DroppedCount);
        }

        [Test]
        public void ExpiredReminder_ResurfacesFinding()
        {
            var expiredReminder = new IgnoredCalendarWarning(LiveEventCalendarRuleIds.LongGapBetweenEvents, "lava-quest",
                "2026-09-20T00:00:00Z", "2026-10-01T00:00:00Z", "xem lại sau khi weekly-pass-35 khép", "2026-09-13T00:00:00Z");

            LiveEventCalendarCheckReport report = ValidationTestFixtures.CheckWithLongGapFinding(expiredReminder,
                ValidationTestFixtures.Utc("2026-09-20T00:00:00Z"), ValidationTestFixtures.Utc("2026-10-01T00:00:00Z"));

            Assert.AreEqual(1, report.Findings.Count, "Hẹn đã tới (13/9 00:00 < now 13/9 08:47) → phát hiện hiện lại.");
            Assert.IsFalse(report.Findings[0].IsIgnored);
            Assert.AreSame(expiredReminder, report.Findings[0].DueReminder, "Cờ nhắc để Editor gắn tag 'đã tới hẹn'.");
            Assert.AreEqual(1, report.Summary.ShouldReviewCount);
        }

        [Test]
        public void PendingReminder_StillIgnored()
        {
            var pendingReminder = new IgnoredCalendarWarning(LiveEventCalendarRuleIds.LongGapBetweenEvents, "lava-quest",
                "2026-09-20T00:00:00Z", "2026-10-01T00:00:00Z", "chưa tới hẹn", "2026-09-14T00:00:00Z");

            LiveEventCalendarCheckReport report = ValidationTestFixtures.CheckWithLongGapFinding(pendingReminder,
                ValidationTestFixtures.Utc("2026-09-20T00:00:00Z"), ValidationTestFixtures.Utc("2026-10-01T00:00:00Z"));

            Assert.AreEqual(0, report.Findings.Count);
            Assert.AreEqual(1, report.IgnoredFindings.Count);
            Assert.IsNull(report.IgnoredFindings[0].DueReminder);
        }

        // ----- Mã biến thể, bất biến V-7 -----

        [Test]
        public void DetailCodes_EveryFindingUsesDeclaredCode()
        {
            var contexts = new List<LiveEventCalendarCheckContext>
            {
                ValidationTestFixtures.ContextFor(LiveOpsDesignSample.Document),
                ValidationTestFixtures.ContextFor(ValidationTestFixtures.MultiFaultDocument()),
                ValidationTestFixtures.ContextFor(ValidationTestFixtures.BrokenRecurringDocument()),
                ValidationTestFixtures.ContextFor(ValidationTestFixtures.ShadowedDocument()),
                new LiveEventCalendarCheckContextBuilder(ValidationTestFixtures.UndeclaredTypeDocument(), LiveOpsDesignSample.NowUtc)
                    .WithRemoteSnapshot(ValidationTestFixtures.RemoteWithUnknownType(), "sha")
                    .Build(),
            };

            int findingCount = 0;
            for (int contextIndex = 0; contextIndex < contexts.Count; contextIndex++)
            {
                LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(contexts[contextIndex]);
                for (int resultIndex = 0; resultIndex < report.RuleResults.Count; resultIndex++)
                {
                    IReadOnlyList<LiveEventCalendarFinding> findings = report.RuleResults[resultIndex].Findings;
                    for (int findingIndex = 0; findingIndex < findings.Count; findingIndex++)
                    {
                        LiveEventCalendarFinding finding = findings[findingIndex];
                        CollectionAssert.Contains(LiveEventCalendarDetailCodes.ForRule(finding.RuleId), finding.DetailCode,
                            finding.RuleId + " · " + finding.TargetId);
                        findingCount++;
                    }
                }
            }
            Assert.Greater(findingCount, 15, "Bộ fixture phải chạm nhiều biến thể, không chỉ vài phát hiện của mẫu.");
        }

        [Test]
        public void DetailCodes_ForRule_CoversAllTwelveRules_NoDuplicateWithinRule()
        {
            IReadOnlyList<string> ruleIds = LiveEventCalendarRuleIds.All;
            for (int index = 0; index < ruleIds.Count; index++)
            {
                IReadOnlyList<string> codes = LiveEventCalendarDetailCodes.ForRule(ruleIds[index]);
                Assert.Greater(codes.Count, 0, ruleIds[index]);
                CollectionAssert.AllItemsAreUnique(codes, ruleIds[index]);
            }
            Assert.AreEqual(0, LiveEventCalendarDetailCodes.ForRule("no-such-rule").Count);
            CollectionAssert.Contains(LiveEventCalendarDetailCodes.ForRule(LiveEventCalendarRuleIds.RecurringRuleInvalid),
                LiveEventCalendarDetailCodes.AnchorUnreadable);
        }

        [Test]
        public void Dropped_Rules1To7_EqualCompilationDroppedCount()
        {
            LiveEventCalendarCheckContext context = ValidationTestFixtures.ContextFor(LiveOpsDesignSample.Document);

            LiveEventCalendarCheckReport report = ValidationTestFixtures.RulesOneToSeven().Check(context);

            Assert.AreEqual(2, context.Compilation.DroppedCount);
            Assert.AreEqual(context.Compilation.DroppedCount, report.Summary.DroppedCount);
            Assert.AreEqual(context.Compilation.DroppedCount, report.Findings.Count);
        }

        [Test]
        public void MultiFaultEntries_OneFindingPerDroppedEntry()
        {
            LiveEventCalendarCheckContext context = ValidationTestFixtures.ContextFor(ValidationTestFixtures.MultiFaultDocument());

            LiveEventCalendarCheckReport report = ValidationTestFixtures.RulesOneToSeven().Check(context);

            Assert.AreEqual(context.Compilation.DroppedCount, report.Findings.Count, "Tổng phát hiện luật 1–7 = số mục bị bỏ.");
            IReadOnlyList<LiveEventCalendarEntryOutcome> entries = context.Compilation.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = entries[index];
                if (outcome.IsKept) continue;
                LiveEventCalendarTargetKind targetKind = outcome.Kind == LiveEventCalendarEntryKind.FixedEvent
                    ? LiveEventCalendarTargetKind.FixedEvent
                    : LiveEventCalendarTargetKind.RecurringRule;
                Assert.AreEqual(1, report.FindingsForTarget(targetKind, outcome.EntryKey).Count,
                    "Mục '" + outcome.EventId + "' (" + outcome.DropReason + ") phải có đúng một phát hiện.");
            }
        }

        [Test]
        public void FindingBuilder_RejectsUndeclaredDetailCode()
        {
            Assert.Throws<ArgumentException>(() => new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.UtcTimeFormat,
                LiveEventCalendarDetailCodes.OverlapKeptEarlier, LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, "a").Build());
            Assert.Throws<ArgumentException>(() => new LiveEventCalendarFindingBuilder("no-such-rule", "start",
                LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, "a").Build());

            LiveEventCalendarFinding finding = new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.RunningEventIdChanged,
                    LiveEventCalendarDetailCodes.RunningRemoved, LiveEventCalendarConsequence.ProgressLost, LiveEventCalendarTargetKind.FixedEvent, "hunt-0914")
                .Build();
            Assert.AreEqual(LiveEventCalendarDetailCodes.RunningRemoved, finding.DetailCode, "Luật 9–12 dựng được phát hiện giả trước khi luật có thật (V-8).");
        }

        [Test]
        public void FindingBuilder_RepairCountMustMatchKind()
        {
            var repair = new LiveEventCalendarRepair("normalize-utc", LiveEventCalendarRepairKind.SafeRepair,
                new RemoveFixedEventEdit("key"), "a", "b", null, null);

            Assert.Throws<ArgumentException>(() => ValidationTestFixtures.OverlapBuilder()
                .WithRepairs(LiveEventCalendarRepairKind.SafeRepair, new[] { repair, repair }).Build());
            Assert.Throws<ArgumentException>(() => ValidationTestFixtures.OverlapBuilder()
                .WithRepairs(LiveEventCalendarRepairKind.Proposal, Array.Empty<LiveEventCalendarRepair>()).Build());
            Assert.Throws<ArgumentException>(() => ValidationTestFixtures.OverlapBuilder()
                .WithRepairs(LiveEventCalendarRepairKind.Ignorable, new[] { repair }).Build());
            Assert.DoesNotThrow(() => ValidationTestFixtures.OverlapBuilder()
                .WithRepairs(LiveEventCalendarRepairKind.SafeRepair, new[] { repair }).Build());
        }

        [Test]
        public void Finding_Fingerprint_IsRuleTargetAndCanonicalRange()
        {
            LiveEventCalendarFinding finding = ValidationTestFixtures.OverlapBuilder()
                .WithRange(ValidationTestFixtures.Utc("2026-09-16T12:00:00Z"), ValidationTestFixtures.Utc("2026-09-17T00:00:00Z"))
                .Build();

            Assert.AreEqual(LiveEventCalendarRuleIds.OverlapSameType + "|hunt-0916-bonus|2026-09-16T12:00:00Z|2026-09-17T00:00:00Z", finding.Fingerprint);
        }

        // ----- Ngữ cảnh, báo cáo -----

        [Test]
        public void ContextBuilder_DocumentInExportOrder_DefaultCompilationMatches()
        {
            LiveEventCalendarDocument outOfOrder = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("late", "hunt", "2026-09-20T00:00:00Z", "2026-09-21T00:00:00Z"),
                ValidationTestFixtures.Entry("early", "hunt", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z"));

            LiveEventCalendarCheckContext context = ValidationTestFixtures.ContextFor(outOfOrder);

            Assert.IsTrue(LiveEventCalendarExportOrder.IsInExportOrder(context.Document));
            Assert.AreEqual("early", context.Document.FixedEvents[0].EventId);
            Assert.AreEqual(context.Document.FixedEvents[0].EntryKey, context.Compilation.Entries[0].EntryKey);
        }

        [Test]
        public void ContextBuilder_CompilationNotInExportOrder_Throws()
        {
            LiveEventCalendarDocument outOfOrder = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("late", "hunt", "2026-09-20T00:00:00Z", "2026-09-21T00:00:00Z"),
                ValidationTestFixtures.Entry("early", "hunt", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z"));

            var builder = new LiveEventCalendarCheckContextBuilder(outOfOrder, LiveOpsDesignSample.NowUtc)
                .WithCompilation(LiveEventCalendarCompiler.Compile(outOfOrder));
            Assert.Throws<ArgumentException>(() => builder.Build());

            Assert.DoesNotThrow(() => new LiveEventCalendarCheckContextBuilder(outOfOrder, LiveOpsDesignSample.NowUtc)
                .WithCompilation(LiveEventCalendarCompiler.CompileInExportOrder(outOfOrder)).Build());
        }

        [Test]
        public void Report_FindingsSortedByConsequenceThenAnchor_FindingsForTarget()
        {
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(ValidationTestFixtures.ContextFor(LiveOpsDesignSample.Document));

            Assert.AreEqual(2, report.Findings.Count);
            Assert.AreEqual("hunt-0916-bonus", report.Findings[0].TargetId, "Cùng Bị bỏ: 16/9 trước 1/10.");
            Assert.AreEqual("lava-quest-2026-10", report.Findings[1].TargetId);
            Assert.AreEqual(1, report.FindingsForTarget(LiveEventCalendarTargetKind.FixedEvent, LiveOpsDesignSample.HuntBonusEntryKey).Count);
            Assert.AreEqual(0, report.FindingsForTarget(LiveEventCalendarTargetKind.RecurringRule, LiveOpsDesignSample.HuntBonusEntryKey).Count);
        }
    }

    /// <summary>Luật giả cho test hạ tầng — hành vi truyền bằng hàm để một fixture dựng được luật ném, trả null, trả phát hiện.</summary>
    internal sealed class FakeCalendarRule : ILiveEventCalendarRule
    {
        private readonly Func<LiveEventCalendarCheckContext, LiveEventCalendarRuleResult> _evaluate;

        public FakeCalendarRule(string ruleId, LiveEventCalendarConsequence consequence,
            Func<LiveEventCalendarCheckContext, LiveEventCalendarRuleResult> evaluate)
        {
            RuleId = ruleId;
            Consequence = consequence;
            _evaluate = evaluate;
        }

        public string RuleId { get; }
        public LiveEventCalendarConsequence Consequence { get; }

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            return _evaluate(context);
        }
    }

    /// <summary>Dữ liệu dùng chung cho test Kiểm lịch (10 fixture của gói) — tài liệu nhỏ dựng tại chỗ, không đụng mẫu thiết kế.</summary>
    internal static class ValidationTestFixtures
    {
        public static DateTime Utc(string utcText)
        {
            Assert.IsTrue(LiveEventUtcText.TryParse(utcText, out DateTime utc), utcText);
            return utc;
        }

        public static FixedLiveEventEntry Entry(string eventId, string eventType, string startUtcText, string endUtcText)
        {
            return new FixedLiveEventEntry(FixedLiveEventEntry.CreateEntryKey(), eventId, eventType, startUtcText, endUtcText, "config");
        }

        /// <summary>Tài liệu chỉ có đợt cố định; khai sẵn mọi loại hợp lệ được dùng để luật 8 không chen vào test luật khác.</summary>
        public static LiveEventCalendarDocument FixedDocument(params FixedLiveEventEntry[] entries)
        {
            var builder = new LiveEventCalendarDocumentBuilder();
            DeclareTypes(builder, entries, Array.Empty<RecurringLiveEventRule>());
            for (int index = 0; index < entries.Length; index++) builder.WithFixedEvent(entries[index]);
            return builder.Build();
        }

        public static LiveEventCalendarDocument Document(RecurringLiveEventRule[] rules, params FixedLiveEventEntry[] entries)
        {
            var builder = new LiveEventCalendarDocumentBuilder();
            DeclareTypes(builder, entries, rules);
            for (int index = 0; index < rules.Length; index++) builder.WithRecurringRule(rules[index]);
            for (int index = 0; index < entries.Length; index++) builder.WithFixedEvent(entries[index]);
            return builder.Build();
        }

        private static void DeclareTypes(LiveEventCalendarDocumentBuilder builder, FixedLiveEventEntry[] entries, RecurringLiveEventRule[] rules)
        {
            var declared = new HashSet<string>(StringComparer.Ordinal);
            var types = new List<string>();
            for (int index = 0; index < rules.Length; index++) types.Add(rules[index].EventType);
            for (int index = 0; index < entries.Length; index++) types.Add(entries[index].EventType);
            for (int index = 0; index < types.Count; index++)
            {
                if (InvalidIdentifierRule.TryDescribeDefect(types[index], out _) || !declared.Add(types[index])) continue;
                builder.WithEventType(new LiveEventTypeDefinition(types[index], types[index], 0, false, string.Empty));
            }
        }

        public static LiveEventCalendarCheckContext ContextFor(LiveEventCalendarDocument document)
        {
            return new LiveEventCalendarCheckContextBuilder(document, LiveOpsDesignSample.NowUtc).Build();
        }

        public static LiveEventCalendarRuleResult Evaluate(ILiveEventCalendarRule rule, LiveEventCalendarDocument document)
        {
            return rule.Evaluate(ContextFor(document));
        }

        public static FakeCalendarRule PassingRule(string ruleId)
        {
            return new FakeCalendarRule(ruleId, LiveEventCalendarConsequence.Dropped, context => LiveEventCalendarRuleResult.Passed(ruleId));
        }

        public static LiveEventCalendarValidator RulesOneToSeven()
        {
            return new LiveEventCalendarValidator(new ILiveEventCalendarRule[]
            {
                new UtcTimeFormatRule(), new EndBeforeStartRule(), new InvalidIdentifierRule(), new DuplicateEventIdRule(),
                new OverlapSameTypeRule(), new RecurringRuleInvalidRule(), new ShadowedByRecurringRule(),
            });
        }

        public static LiveEventCalendarFindingBuilder OverlapBuilder()
        {
            return new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.OverlapSameType, LiveEventCalendarDetailCodes.OverlapKeptEarlier,
                LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, "hunt-0916-bonus");
        }

        /// <summary>Chạy một luật giả phát MỘT phát hiện long-gap (Nên xem, Ignorable) trên mẫu có thêm cảnh báo cho trước.</summary>
        public static LiveEventCalendarCheckReport CheckWithLongGapFinding(IgnoredCalendarWarning warning, DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.Document).WithIgnoredWarning(warning).Build();
            LiveEventCalendarFinding finding = new LiveEventCalendarFindingBuilder(LiveEventCalendarRuleIds.LongGapBetweenEvents,
                    LiveEventCalendarDetailCodes.LongGap, LiveEventCalendarConsequence.ShouldReview, LiveEventCalendarTargetKind.EventType, "lava-quest")
                .WithTargetEntryKey("lava-quest")
                .WithRange(rangeStartUtc, rangeEndUtc)
                .WithAnchor(rangeStartUtc)
                .WithRepairs(LiveEventCalendarRepairKind.Ignorable, null)
                .Build();
            var validator = new LiveEventCalendarValidator(new ILiveEventCalendarRule[]
            {
                new FakeCalendarRule(LiveEventCalendarRuleIds.LongGapBetweenEvents, LiveEventCalendarConsequence.ShouldReview,
                    context => LiveEventCalendarRuleResult.Found(LiveEventCalendarRuleIds.LongGapBetweenEvents, new[] { finding })),
            });
            return validator.Check(ContextFor(document));
        }

        /// <summary>
        /// Fixture V-7: id có '#' + kết thúc ≤ bắt đầu · giờ hỏng + id rỗng · trùng id với đợt đứng trước đã bị bỏ · trùng id + chồng giờ
        /// · một luật lặp hỏng + luật thứ hai cùng loại.
        /// </summary>
        public static LiveEventCalendarDocument MultiFaultDocument()
        {
            return Document(
                new[]
                {
                    new RecurringLiveEventRule("race", "2026-01-05T00:00:00Z", "race-", 24, 30, string.Empty),
                    new RecurringLiveEventRule("race", "2026-01-05T00:00:00Z", "race-", 24, 20, string.Empty),
                    new RecurringLiveEventRule("pass", "2026-01-05T00:00:00Z", "pass-", 168, 168, string.Empty),
                    new RecurringLiveEventRule("pass", "2026-01-05T00:00:00Z", "pass-", 168, 168, string.Empty),
                },
                Entry("hash#id", "hunt", "2026-09-10T00:00:00Z", "2026-09-09T00:00:00Z"),
                Entry(string.Empty, "hunt", "2026-09-11T00:00:00Z", "2026-9-12"),
                Entry("reused", "bad#type", "2026-09-12T00:00:00Z", "2026-09-13T00:00:00Z"),
                Entry("reused", "hunt", "2026-09-13T00:00:00Z", "2026-09-14T00:00:00Z"),
                Entry("kept", "hunt", "2026-09-20T00:00:00Z", "2026-09-22T00:00:00Z"),
                Entry("kept", "hunt", "2026-09-21T00:00:00Z", "2026-09-23T00:00:00Z"),
                Entry("overlapping", "hunt", "2026-09-21T12:00:00Z", "2026-09-24T00:00:00Z"));
        }

        public static LiveEventCalendarDocument BrokenRecurringDocument()
        {
            return Document(
                new[]
                {
                    new RecurringLiveEventRule("anchor-broken", "2026-1-5", string.Empty, 24, 20, string.Empty),
                    new RecurringLiveEventRule("anchor-ambiguous", "5/1/2026", string.Empty, 24, 20, string.Empty),
                    new RecurringLiveEventRule("period-zero", "2026-01-05T00:00:00Z", string.Empty, 0, 20, string.Empty),
                    new RecurringLiveEventRule("active-zero", "2026-01-05T00:00:00Z", string.Empty, 24, 0, string.Empty),
                    new RecurringLiveEventRule("too-long", "2026-01-05T00:00:00Z", string.Empty, 24, 30, string.Empty),
                    new RecurringLiveEventRule("prefix-broken", "2026-01-05T00:00:00Z", "bad#", 24, 20, string.Empty),
                    new RecurringLiveEventRule("type#broken", "2026-01-05T00:00:00Z", "ok-", 24, 20, string.Empty),
                });
        }

        public static LiveEventCalendarDocument ShadowedDocument()
        {
            return Document(
                new[] { new RecurringLiveEventRule("race", "2026-01-05T00:00:00Z", string.Empty, 24, 20, string.Empty) },
                Entry("race-special", "race", "2026-09-14T01:00:00Z", "2026-09-14T02:00:00Z"),
                Entry("race-252", "race", "2026-09-14T21:00:00Z", "2026-09-14T22:00:00Z"));
        }

        /// <summary>Mẫu thiết kế thêm hai đợt lucky-spin (loại chưa khai) vào nháp.</summary>
        public static LiveEventCalendarDocument UndeclaredTypeDocument()
        {
            return new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.Document)
                .WithFixedEvent(Entry("lucky-spin-0914", "lucky-spin", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"))
                .WithFixedEvent(Entry("lucky-spin-0920", "lucky-spin", "2026-09-20T00:00:00Z", "2026-09-21T00:00:00Z"))
                .Build();
        }

        /// <summary>JSON đang chạy đã dán (không có định nghĩa loại) có loại gold-rush không có trong nháp lẫn asset.</summary>
        public static LiveEventCalendarDocument RemoteWithUnknownType()
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(Entry("hunt-0914", "treasure-hunt", "2026-09-14T00:00:00Z", "2026-09-17T00:00:00Z"))
                .WithFixedEvent(Entry("gold-rush-1", "gold-rush", "2026-09-12T00:00:00Z", "2026-09-13T00:00:00Z"))
                .WithFixedEvent(Entry("gold-rush-2", "gold-rush", "2026-09-15T00:00:00Z", "2026-09-16T00:00:00Z"))
                .Build();
        }
    }
}
