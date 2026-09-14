using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    /// <summary>
    /// (V-7) Bất biến "mỗi mục bị bỏ sinh đúng một phát hiện Bị bỏ" trên 200 tài liệu ngẫu nhiên seed cố định
    /// (<see cref="LiveOpsRandomDocuments"/>). Fixture tay (<c>MultiFaultEntries_OneFindingPerDroppedEntry</c>) chỉ khoá vài tổ hợp
    /// đã nghĩ ra; fuzz khoá mọi tổ hợp lý do chính + lỗi đi kèm + trùng id lệch thứ tự mà bộ sinh tạo được — đếm đôi ở đây là
    /// rail và dòng cổng Xuất báo sai số mục game sẽ bỏ.
    /// </summary>
    [TestFixture]
    public sealed class ValidationInvariantFuzzTests
    {
        // Luật 1–7 là cách giải thích DropReason của bộ biên dịch; luật 8 (loại chưa khai) bỏ vì LiveOpsSystem, không vì lịch.
        private static readonly HashSet<string> RulesOneToSeven = new HashSet<string>(StringComparer.Ordinal)
        {
            LiveEventCalendarRuleIds.UtcTimeFormat,
            LiveEventCalendarRuleIds.EndBeforeStart,
            LiveEventCalendarRuleIds.InvalidIdentifier,
            LiveEventCalendarRuleIds.DuplicateEventId,
            LiveEventCalendarRuleIds.OverlapSameType,
            LiveEventCalendarRuleIds.RecurringRuleInvalid,
            LiveEventCalendarRuleIds.ShadowedByRecurring,
        };

        [Test]
        public void RandomDocuments_DroppedFindingsRules1To7_EqualDroppedCount()
        {
            int totalDroppedCount = 0;
            for (int documentIndex = 0; documentIndex < LiveOpsRandomDocuments.DocumentCount; documentIndex++)
            {
                LiveEventCalendarDocument document = LiveOpsRandomDocuments.Create(documentIndex);
                LiveEventCalendarCheckContext context = ContextFor(document);
                LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(context);
                string label = LiveOpsRandomDocuments.Label(documentIndex);

                AssertNoRuleFailed(label, report, document);

                int rulesOneToSevenFindingCount = 0;
                for (int resultIndex = 0; resultIndex < report.RuleResults.Count; resultIndex++)
                {
                    LiveEventCalendarRuleResult result = report.RuleResults[resultIndex];
                    if (!RulesOneToSeven.Contains(result.RuleId)) continue;
                    for (int findingIndex = 0; findingIndex < result.Findings.Count; findingIndex++)
                    {
                        LiveEventCalendarFinding finding = result.Findings[findingIndex];
                        Assert.AreEqual(LiveEventCalendarConsequence.Dropped, finding.Consequence,
                            label + ": phát hiện " + finding.RuleId + " · " + finding.TargetId + " của luật 1–7 phải là Bị bỏ.");
                        rulesOneToSevenFindingCount++;
                    }
                }

                Assert.AreEqual(context.Compilation.DroppedCount, rulesOneToSevenFindingCount,
                    label + ": tổng phát hiện luật 1–7 phải bằng số mục bộ biên dịch bỏ — lệch là đếm đôi hoặc sót mục.\n" +
                    LiveOpsRandomDocuments.Describe(document));
                totalDroppedCount += context.Compilation.DroppedCount;
            }

            Assert.Greater(totalDroppedCount, LiveOpsRandomDocuments.DocumentCount,
                "Bộ sinh phải tạo đủ mục bị bỏ để bất biến có nghĩa (trung bình hơn một mục mỗi tài liệu).");
        }

        [Test]
        public void RandomDocuments_EveryDroppedEntryHasExactlyOneFinding()
        {
            for (int documentIndex = 0; documentIndex < LiveOpsRandomDocuments.DocumentCount; documentIndex++)
            {
                LiveEventCalendarDocument document = LiveOpsRandomDocuments.Create(documentIndex);
                LiveEventCalendarCheckContext context = ContextFor(document);
                LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(context);
                string label = LiveOpsRandomDocuments.Label(documentIndex);

                // Đích → số mục bị bỏ, và đích → (luật, số phát hiện). Đợt cố định có EntryKey riêng nên mỗi đích đúng 1; luật lặp khoá
                // theo loại nên hai luật cùng loại cùng bị bỏ (hỏng + trùng loại) là 2 mục cùng một đích — so số lượng theo đích.
                var droppedCountByTarget = new Dictionary<string, int>(StringComparer.Ordinal);
                var expectedRuleByTarget = new Dictionary<string, string>(StringComparer.Ordinal);
                IReadOnlyList<LiveEventCalendarEntryOutcome> entries = context.Compilation.Entries;
                for (int index = 0; index < entries.Count; index++)
                {
                    LiveEventCalendarEntryOutcome outcome = entries[index];
                    if (outcome.IsKept) continue;
                    string target = TargetKey(outcome.Kind == LiveEventCalendarEntryKind.FixedEvent
                        ? LiveEventCalendarTargetKind.FixedEvent
                        : LiveEventCalendarTargetKind.RecurringRule, outcome.EntryKey);
                    droppedCountByTarget[target] = CountOf(droppedCountByTarget, target) + 1;
                    expectedRuleByTarget[target] = RuleIdFor(outcome.DropReason);
                }

                var findingCountByTarget = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int resultIndex = 0; resultIndex < report.RuleResults.Count; resultIndex++)
                {
                    LiveEventCalendarRuleResult result = report.RuleResults[resultIndex];
                    if (!RulesOneToSeven.Contains(result.RuleId)) continue;
                    for (int findingIndex = 0; findingIndex < result.Findings.Count; findingIndex++)
                    {
                        LiveEventCalendarFinding finding = result.Findings[findingIndex];
                        string target = TargetKey(finding.TargetKind, finding.TargetEntryKey);
                        Assert.IsTrue(expectedRuleByTarget.ContainsKey(target),
                            label + ": " + finding.RuleId + " bắn cho '" + finding.TargetId + "' nhưng bộ biên dịch GIỮ mục đó.\n" +
                            LiveOpsRandomDocuments.Describe(document));
                        Assert.AreEqual(expectedRuleByTarget[target], finding.RuleId,
                            label + ": '" + finding.TargetId + "' bị bỏ vì lý do chính của luật khác — mỗi luật chỉ bắn cho DropReason chính của nó.");
                        findingCountByTarget[target] = CountOf(findingCountByTarget, target) + 1;
                    }
                }

                foreach (KeyValuePair<string, int> dropped in droppedCountByTarget)
                {
                    Assert.AreEqual(dropped.Value, CountOf(findingCountByTarget, dropped.Key),
                        label + ": mục bị bỏ '" + dropped.Key + "' phải có đúng một phát hiện mỗi mục (luật " + expectedRuleByTarget[dropped.Key] + ").\n" +
                        LiveOpsRandomDocuments.Describe(document));
                }
            }
        }

        /// <summary>
        /// Hai bất biến trên chỉ có nghĩa khi bộ sinh thật sự chạm các ca khó. Test này chặn bộ sinh bị "làm dễ" về sau (vd sửa
        /// kịch bản bắt buộc) mà hai fuzz vẫn xanh vì không còn gì để bắt.
        /// </summary>
        [Test]
        public void RandomDocuments_CoverEveryDropReasonMultiFaultAndOutOfOrderDuplicates()
        {
            var seenReasons = new HashSet<LiveEventCalendarDropReason>();
            int multiFaultCount = 0;
            int laterStartFirstDuplicateCount = 0;
            int duplicateOfDroppedKeptCount = 0;
            int fractionalTimeCount = 0;
            int offsetTimeCount = 0;
            int whitespaceOnlyTimeCount = 0;
            int emptyDocumentCount = 0;

            for (int documentIndex = 0; documentIndex < LiveOpsRandomDocuments.DocumentCount; documentIndex++)
            {
                LiveEventCalendarDocument document = LiveOpsRandomDocuments.Create(documentIndex);
                Assert.AreEqual(LiveOpsRandomDocuments.Describe(document), LiveOpsRandomDocuments.Describe(LiveOpsRandomDocuments.Create(documentIndex)),
                    LiveOpsRandomDocuments.Label(documentIndex) + ": cùng seed + chỉ số phải ra cùng tài liệu.");

                LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.CompileInExportOrder(document);
                for (int index = 0; index < compilation.Entries.Count; index++)
                {
                    LiveEventCalendarEntryOutcome outcome = compilation.Entries[index];
                    seenReasons.Add(outcome.DropReason);
                    if (outcome.AdditionalItemReasons.Count > 0) multiFaultCount++;
                }

                if (document.FixedEvents.Count == 0 && document.RecurringRules.Count == 0) emptyDocumentCount++;
                laterStartFirstDuplicateCount += CountLaterStartFirstDuplicates(document, compilation);
                duplicateOfDroppedKeptCount += CountDuplicatesOfDroppedEntriesKept(compilation);
                for (int index = 0; index < document.FixedEvents.Count; index++)
                {
                    FixedLiveEventEntry entry = document.FixedEvents[index];
                    CountTimeShapes(entry.StartUtcText, ref fractionalTimeCount, ref offsetTimeCount, ref whitespaceOnlyTimeCount);
                    CountTimeShapes(entry.EndUtcText, ref fractionalTimeCount, ref offsetTimeCount, ref whitespaceOnlyTimeCount);
                }
            }

            foreach (LiveEventCalendarDropReason reason in (LiveEventCalendarDropReason[])Enum.GetValues(typeof(LiveEventCalendarDropReason)))
            {
                Assert.IsTrue(seenReasons.Contains(reason), "Bộ sinh không tạo mục nào có DropReason " + reason + ".");
            }
            Assert.Greater(multiFaultCount, 0, "Bộ sinh phải tạo mục nhiều lỗi (AdditionalItemReasons).");
            Assert.Greater(laterStartFirstDuplicateCount, 0, "Bộ sinh phải tạo trùng id mà đợt bắt đầu muộn đứng trước trong asset.");
            Assert.Greater(duplicateOfDroppedKeptCount, 0, "Bộ sinh phải tạo trùng id với đợt đã bị bỏ vì lỗi cấp mục (đợt sau được giữ).");
            Assert.Greater(fractionalTimeCount, 0, "Bộ sinh phải tạo giờ có phần lẻ giây (V-19).");
            Assert.Greater(offsetTimeCount, 0, "Bộ sinh phải tạo giờ có múi khác Z.");
            Assert.Greater(whitespaceOnlyTimeCount, 0, "Bộ sinh phải tạo giờ chỉ có khoảng trắng.");
            Assert.Greater(emptyDocumentCount, 0, "Bộ sinh phải tạo tài liệu không có đợt lẫn luật.");
        }

        private static LiveEventCalendarCheckContext ContextFor(LiveEventCalendarDocument document)
        {
            return new LiveEventCalendarCheckContextBuilder(document, LiveOpsDesignSample.NowUtc).Build();
        }

        /// <summary>
        /// Luật ném bị CheckRun nuốt thành Failed với 0 phát hiện — số đếm sẽ tụt mà không ai thấy. Chặn cho CẢ 12 luật: dữ liệu
        /// lạ của fuzz là nơi luật 9–12 thật (G-VALIDATOR-B) dễ ném nhất.
        /// </summary>
        private static void AssertNoRuleFailed(string label, LiveEventCalendarCheckReport report, LiveEventCalendarDocument document)
        {
            for (int resultIndex = 0; resultIndex < report.RuleResults.Count; resultIndex++)
            {
                LiveEventCalendarRuleResult result = report.RuleResults[resultIndex];
                Assert.AreNotEqual(LiveEventCalendarRuleOutcome.Failed, result.Outcome,
                    label + " · " + result.RuleId + " ném " + result.ExceptionTypeName + ": " + result.ExceptionMessage + "\n" +
                    LiveOpsRandomDocuments.Describe(document));
            }
        }

        private static string RuleIdFor(LiveEventCalendarDropReason reason)
        {
            switch (reason)
            {
                case LiveEventCalendarDropReason.UnreadableStartUtc:
                case LiveEventCalendarDropReason.UnreadableEndUtc:
                    return LiveEventCalendarRuleIds.UtcTimeFormat;
                case LiveEventCalendarDropReason.InvalidIdentifier:
                    return LiveEventCalendarRuleIds.InvalidIdentifier;
                case LiveEventCalendarDropReason.EndNotAfterStart:
                    return LiveEventCalendarRuleIds.EndBeforeStart;
                case LiveEventCalendarDropReason.DuplicateEventId:
                    return LiveEventCalendarRuleIds.DuplicateEventId;
                case LiveEventCalendarDropReason.OverlapsSameType:
                    return LiveEventCalendarRuleIds.OverlapSameType;
                case LiveEventCalendarDropReason.ShadowedByRecurring:
                    return LiveEventCalendarRuleIds.ShadowedByRecurring;
                case LiveEventCalendarDropReason.InvalidRecurringRule:
                case LiveEventCalendarDropReason.DuplicateRecurringType:
                    return LiveEventCalendarRuleIds.RecurringRuleInvalid;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), "Mục bị bỏ không có lý do chính: " + reason + ".");
            }
        }

        private static string TargetKey(LiveEventCalendarTargetKind kind, string entryKey)
        {
            return ((int)kind).ToString(CultureInfo.InvariantCulture) + "|" + entryKey;
        }

        private static int CountOf(Dictionary<string, int> counts, string key)
        {
            return counts.TryGetValue(key, out int count) ? count : 0;
        }

        /// <summary>
        /// Cặp đợt qua kiểm cấp mục, cùng id, đợt bắt đầu muộn đứng TRƯỚC trong asset — và thứ tự xuất giữ đúng đợt bắt đầu sớm.
        /// </summary>
        private static int CountLaterStartFirstDuplicates(LiveEventCalendarDocument document, LiveEventCalendarCompilation compilation)
        {
            int count = 0;
            IReadOnlyList<FixedLiveEventEntry> fixedEvents = document.FixedEvents;
            for (int laterIndex = 0; laterIndex < fixedEvents.Count; laterIndex++)
            {
                FixedLiveEventEntry later = fixedEvents[laterIndex];
                if (!IsItemLevelValid(later, out DateTime laterStartUtc)) continue;
                for (int earlierIndex = laterIndex + 1; earlierIndex < fixedEvents.Count; earlierIndex++)
                {
                    FixedLiveEventEntry earlier = fixedEvents[earlierIndex];
                    if (!string.Equals(later.EventId, earlier.EventId, StringComparison.Ordinal)) continue;
                    if (!IsItemLevelValid(earlier, out DateTime earlierStartUtc) || earlierStartUtc >= laterStartUtc) continue;
                    if (compilation.TryGetFixedOutcome(later.EntryKey, out LiveEventCalendarEntryOutcome laterOutcome) &&
                        laterOutcome.DropReason == LiveEventCalendarDropReason.DuplicateEventId)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private static int CountDuplicatesOfDroppedEntriesKept(LiveEventCalendarCompilation compilation)
        {
            var droppedItemLevelIds = new HashSet<string>(StringComparer.Ordinal);
            int count = 0;
            IReadOnlyList<LiveEventCalendarEntryOutcome> entries = compilation.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = entries[index];
                if (outcome.Kind != LiveEventCalendarEntryKind.FixedEvent) continue;
                bool itemLevelDropped = outcome.DropReason == LiveEventCalendarDropReason.UnreadableStartUtc ||
                                        outcome.DropReason == LiveEventCalendarDropReason.UnreadableEndUtc;
                if (itemLevelDropped)
                {
                    droppedItemLevelIds.Add(outcome.EventId);
                    continue;
                }
                if (outcome.IsKept && droppedItemLevelIds.Contains(outcome.EventId)) count++;
            }
            return count;
        }

        private static bool IsItemLevelValid(FixedLiveEventEntry entry, out DateTime startUtc)
        {
            bool startReadable = entry.TryGetStartUtc(out startUtc);
            bool endReadable = entry.TryGetEndUtc(out DateTime endUtc);
            return startReadable && endReadable && endUtc > startUtc && !InvalidIdentifierRule.TryDescribeDefect(entry.EventId, out _) &&
                   !InvalidIdentifierRule.TryDescribeDefect(entry.EventType, out _);
        }

        private static void CountTimeShapes(string text, ref int fractionalCount, ref int offsetCount, ref int whitespaceOnlyCount)
        {
            if (text.Length > 0 && text.Trim().Length == 0)
            {
                whitespaceOnlyCount++;
                return;
            }
            if (!LiveEventUtcText.TryParse(text, out DateTime utc)) return;
            if (utc.Ticks % TimeSpan.TicksPerSecond != 0) fractionalCount++;
            if (text.IndexOf("+07:00", StringComparison.Ordinal) >= 0) offsetCount++;
        }
    }
}
