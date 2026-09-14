using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật 7 <c>shadowed-by-recurring</c>: đợt cố định có loại đang có luật lặp và bị <see cref="CompositeLiveEventCalendar"/> che
    /// (PD-3: luật lặp ghép trước đợt cố định) — chồng giờ với một lần lặp cùng loại, hoặc trùng id với một lần lặp. Không sửa
    /// tự động: gỡ che là đổi ý đồ lịch (đổi luật hay đổi đợt), Editor chỉ mở luật.
    /// </summary>
    internal sealed class ShadowedByRecurringRule : ILiveEventCalendarRule
    {
        public string RuleId => LiveEventCalendarRuleIds.ShadowedByRecurring;
        public LiveEventCalendarConsequence Consequence => LiveEventCalendarConsequence.Dropped;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            List<LiveEventCalendarEntryOutcome> outcomes = context.DroppedFixedOutcomes(LiveEventCalendarDropReason.ShadowedByRecurring);
            if (outcomes.Count == 0) return LiveEventCalendarRuleResult.Passed(RuleId);

            var findings = new List<LiveEventCalendarFinding>(outcomes.Count);
            for (int index = 0; index < outcomes.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = outcomes[index];
                FixedLiveEventEntry entry = context.FixedEventOf(outcome);
                entry.TryGetStartUtc(out DateTime startUtc);
                entry.TryGetEndUtc(out DateTime endUtc);

                string detailCode = OverlapsAnyOccurrence(context.Compilation, entry.EventType, startUtc, endUtc)
                    ? LiveEventCalendarDetailCodes.ShadowedOverlap
                    : LiveEventCalendarDetailCodes.ShadowedIdCollision;

                findings.Add(new LiveEventCalendarFindingBuilder(RuleId, detailCode, Consequence, LiveEventCalendarTargetKind.FixedEvent, entry.EventId)
                    .WithTargetEntryKey(entry.EntryKey)
                    .WithRelatedId(outcome.RelatedEventId)
                    .WithTexts(entry.EventId, outcome.RelatedEventId)
                    .WithRange(startUtc, endUtc)
                    .WithAnchor(startUtc)
                    .Build());
            }
            return LiveEventCalendarRuleResult.Found(RuleId, findings);
        }

        /// <summary>
        /// Hỏi thẳng lịch lặp đã dựng của loại (không qua Composite — Composite trả cả đợt cố định khi không bị che). Khung hỏi
        /// dài đúng thời lượng đợt nên chỉ cần biết "có ít nhất một lần lặp", trần số lần mỗi truy vấn không làm sai kết quả.
        /// </summary>
        private static bool OverlapsAnyOccurrence(LiveEventCalendarCompilation compilation, string eventType, DateTime startUtc, DateTime endUtc)
        {
            IReadOnlyList<RecurringLiveEventCalendar> recurringCalendars = compilation.RecurringCalendars;
            for (int index = 0; index < recurringCalendars.Count; index++)
            {
                RecurringLiveEventCalendar recurringCalendar = recurringCalendars[index];
                if (!string.Equals(recurringCalendar.EventType, eventType, StringComparison.Ordinal)) continue;
                return recurringCalendar.GetInstances(eventType, startUtc, endUtc).Count > 0;
            }
            return false;
        }
    }
}
