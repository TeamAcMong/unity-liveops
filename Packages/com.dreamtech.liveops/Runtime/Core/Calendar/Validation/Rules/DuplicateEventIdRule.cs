using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật 4 <c>duplicate-event-id</c>: đợt qua kiểm cấp mục mang id đã có ở một đợt được giữ đứng trước trong thứ tự xuất —
    /// game giữ mục xuất hiện trước. Đọc lý do chính của outcome (V-7): trùng id với đợt đứng trước ĐÃ bị bỏ vì lỗi cấp mục
    /// không phải trùng (bộ biên dịch, như runtime, không bao giờ thấy đợt hỏng đó), nên không có phát hiện ở đây.
    /// </summary>
    internal sealed class DuplicateEventIdRule : ILiveEventCalendarRule
    {
        public const string RenameRepairId = "rename-with-suggested-id";

        /// <summary>Hậu tố số bắt đầu từ 2 ("hunt-0914-2") — core chỉ đề xuất dạng đơn giản; Editor có bộ gợi ý id theo mẫu riêng.</summary>
        private const int FirstSuggestedSuffix = 2;

        public string RuleId => LiveEventCalendarRuleIds.DuplicateEventId;
        public LiveEventCalendarConsequence Consequence => LiveEventCalendarConsequence.Dropped;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            List<LiveEventCalendarEntryOutcome> outcomes = context.DroppedFixedOutcomes(LiveEventCalendarDropReason.DuplicateEventId);
            if (outcomes.Count == 0) return LiveEventCalendarRuleResult.Passed(RuleId);

            HashSet<string> usedEventIds = CollectEventIds(context.Document);
            var findings = new List<LiveEventCalendarFinding>(outcomes.Count);
            for (int index = 0; index < outcomes.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = outcomes[index];
                FixedLiveEventEntry entry = context.FixedEventOf(outcome);
                string suggestedId = SuggestId(entry.EventId, usedEventIds);
                // Hai đợt cùng trùng một id không được đề xuất cùng một id mới — đề xuất sau lại trùng đề xuất trước.
                usedEventIds.Add(suggestedId);

                entry.TryGetStartUtc(out DateTime startUtc);
                entry.TryGetEndUtc(out DateTime endUtc);

                var repair = new LiveEventCalendarRepair(RenameRepairId, LiveEventCalendarRepairKind.Proposal,
                    new ReplaceFixedEventEdit(entry.WithEventId(suggestedId)), entry.EventId, suggestedId, startUtc, endUtc);

                findings.Add(new LiveEventCalendarFindingBuilder(RuleId, LiveEventCalendarDetailCodes.DuplicateFixedId, Consequence,
                        LiveEventCalendarTargetKind.FixedEvent, entry.EventId)
                    .WithTargetEntryKey(entry.EntryKey)
                    .WithRelatedId(outcome.RelatedEventId)
                    .WithTexts(entry.EventId, suggestedId)
                    .WithRange(startUtc, endUtc)
                    .WithAnchor(startUtc)
                    .WithRepairs(LiveEventCalendarRepairKind.Proposal, new[] { repair })
                    .Build());
            }
            return LiveEventCalendarRuleResult.Found(RuleId, findings);
        }

        private static HashSet<string> CollectEventIds(LiveEventCalendarDocument document)
        {
            var eventIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<FixedLiveEventEntry> fixedEvents = document.FixedEvents;
            for (int index = 0; index < fixedEvents.Count; index++) eventIds.Add(fixedEvents[index].EventId);
            return eventIds;
        }

        internal static string SuggestId(string eventId, HashSet<string> usedEventIds)
        {
            for (int suffix = FirstSuggestedSuffix; suffix < int.MaxValue; suffix++)
            {
                string candidate = eventId + "-" + suffix.ToString(CultureInfo.InvariantCulture);
                if (!usedEventIds.Contains(candidate)) return candidate;
            }
            return eventId + "-" + FixedLiveEventEntry.CreateEntryKey();
        }
    }
}
