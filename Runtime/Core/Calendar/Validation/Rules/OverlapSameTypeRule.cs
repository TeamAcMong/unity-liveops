using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật 5 <c>overlap-same-type</c>: đợt bị <see cref="FixedLiveEventCalendar"/> bỏ vì chồng giờ với đợt cùng loại mở sớm hơn.
    /// Hai đề xuất theo thiết kế: dời bắt đầu tới lúc đợt giữ lại khép (giữ kết thúc), hoặc dời cả đợt giữ nguyên thời lượng.
    /// </summary>
    internal sealed class OverlapSameTypeRule : ILiveEventCalendarRule
    {
        public const string ShiftStartKeepEndRepairId = "shift-start-keep-end";
        public const string ShiftWholeKeepDurationRepairId = "shift-whole-keep-duration";

        public string RuleId => LiveEventCalendarRuleIds.OverlapSameType;
        public LiveEventCalendarConsequence Consequence => LiveEventCalendarConsequence.Dropped;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            List<LiveEventCalendarEntryOutcome> outcomes = context.DroppedFixedOutcomes(LiveEventCalendarDropReason.OverlapsSameType);
            if (outcomes.Count == 0) return LiveEventCalendarRuleResult.Passed(RuleId);

            var findings = new List<LiveEventCalendarFinding>(outcomes.Count);
            for (int index = 0; index < outcomes.Count; index++)
            {
                findings.Add(BuildFinding(context, outcomes[index]));
            }
            return LiveEventCalendarRuleResult.Found(RuleId, findings);
        }

        private LiveEventCalendarFinding BuildFinding(LiveEventCalendarCheckContext context, LiveEventCalendarEntryOutcome outcome)
        {
            FixedLiveEventEntry entry = context.FixedEventOf(outcome);
            entry.TryGetStartUtc(out DateTime startUtc);
            entry.TryGetEndUtc(out DateTime endUtc);

            var repairs = new List<LiveEventCalendarRepair>(2);
            string beforeText = entry.StartUtcText + LiveEventCalendarFindingBuilder.ValueSeparator + entry.EndUtcText;
            string expectedText = string.Empty;

            if (TryFindKeptEnd(context, outcome, out DateTime keptEndUtc))
            {
                expectedText = LiveEventUtcText.Format(keptEndUtc);

                // "Dời tới 17/9 00:00 (24 giờ)": chỉ khi đợt giữ lại khép trước khi đợt này kết thúc — không thì đợt thành dài ≤ 0.
                if (keptEndUtc < endUtc)
                {
                    repairs.Add(BuildRepair(ShiftStartKeepEndRepairId, entry, beforeText, keptEndUtc, endUtc));
                }

                // "Giữ 36 giờ: tới 18/9 12:00": dời cả đợt sang ngay sau đợt giữ lại.
                TimeSpan duration = endUtc - startUtc;
                if (keptEndUtc.Ticks <= DateTime.MaxValue.Ticks - duration.Ticks)
                {
                    repairs.Add(BuildRepair(ShiftWholeKeepDurationRepairId, entry, beforeText, keptEndUtc, keptEndUtc.Add(duration)));
                }
            }

            return new LiveEventCalendarFindingBuilder(RuleId, LiveEventCalendarDetailCodes.OverlapKeptEarlier, Consequence,
                    LiveEventCalendarTargetKind.FixedEvent, entry.EventId)
                .WithTargetEntryKey(entry.EntryKey)
                .WithRelatedId(outcome.RelatedEventId)
                .WithTexts(beforeText, expectedText)
                .WithRange(outcome.OverlapStartUtc, outcome.OverlapEndUtc)
                .WithAnchor(startUtc)
                .WithRepairs(repairs.Count > 0 ? LiveEventCalendarRepairKind.Proposal : LiveEventCalendarRepairKind.None, repairs)
                .Build();
        }

        private static LiveEventCalendarRepair BuildRepair(string repairId, FixedLiveEventEntry entry, string beforeText,
            DateTime newStartUtc, DateTime newEndUtc)
        {
            string newStartText = LiveEventUtcText.Format(newStartUtc);
            string newEndText = LiveEventUtcText.Format(newEndUtc);
            return new LiveEventCalendarRepair(repairId, LiveEventCalendarRepairKind.Proposal,
                new ReplaceFixedEventEdit(entry.WithTimes(newStartText, newEndText)), beforeText,
                newStartText + LiveEventCalendarFindingBuilder.ValueSeparator + newEndText,
                DateTime.SpecifyKind(newStartUtc, DateTimeKind.Utc), DateTime.SpecifyKind(newEndUtc, DateTimeKind.Utc));
        }

        /// <summary>
        /// Kết thúc của đợt được giữ khi quét chồng giờ: cùng loại, id = <see cref="LiveEventCalendarEntryOutcome.RelatedEventId"/>,
        /// đã qua kiểm cấp mục + trùng id (giữ, hoặc về sau mới bị luật lặp che). Sau bước trùng id, mỗi id còn đúng một đợt nên
        /// tìm theo id là duy nhất. Tìm lại thay vì dùng <c>OverlapEndUtc</c> vì khi đợt nằm trọn trong đợt giữ, khoảng chồng khép
        /// ở kết thúc của CHÍNH đợt này chứ không phải của đợt giữ.
        /// </summary>
        private static bool TryFindKeptEnd(LiveEventCalendarCheckContext context, LiveEventCalendarEntryOutcome outcome, out DateTime keptEndUtc)
        {
            keptEndUtc = default;
            IReadOnlyList<LiveEventCalendarEntryOutcome> entries = context.Compilation.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                LiveEventCalendarEntryOutcome candidate = entries[index];
                if (candidate.Kind != LiveEventCalendarEntryKind.FixedEvent) continue;
                if (candidate.DropReason != LiveEventCalendarDropReason.None && candidate.DropReason != LiveEventCalendarDropReason.ShadowedByRecurring) continue;
                if (!string.Equals(candidate.EventId, outcome.RelatedEventId, StringComparison.Ordinal)) continue;
                if (!string.Equals(candidate.EventType, outcome.EventType, StringComparison.Ordinal)) continue;
                return context.FixedEventOf(candidate).TryGetEndUtc(out keptEndUtc);
            }
            return false;
        }
    }
}
