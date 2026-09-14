using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Kết quả biên dịch một <see cref="LiveEventCalendarDocument"/> thành lịch chạy được (<see cref="ILiveEventCalendar"/>)
    /// cộng thông tin từng mục — dùng để game đọc đợt (<see cref="Calendar"/>) và để hub/validator giải thích mục nào
    /// bị bỏ vì sao (<see cref="Entries"/>) mà không phải tự đoán lại luật runtime.
    /// </summary>
    public sealed class LiveEventCalendarCompilation
    {
        private readonly Dictionary<string, int> _fixedOutcomeIndexByEntryKey;
        private readonly Dictionary<string, int> _recurringOutcomeIndexByType;
        private readonly Dictionary<string, RecurringLiveEventCalendar> _recurringCalendarByType;

        internal LiveEventCalendarCompilation(ILiveEventCalendar calendar, FixedLiveEventCalendar fixedCalendar,
            List<RecurringLiveEventCalendar> recurringCalendars, List<LiveEventCalendarEntryOutcome> entries, List<string> problems)
        {
            Calendar = calendar;
            FixedCalendar = fixedCalendar;
            RecurringCalendars = recurringCalendars;
            Entries = entries;
            Problems = problems;

            int keptCount = 0;
            _fixedOutcomeIndexByEntryKey = new Dictionary<string, int>(StringComparer.Ordinal);
            _recurringOutcomeIndexByType = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < entries.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = entries[index];
                if (outcome.IsKept) keptCount++;
                if (outcome.Kind == LiveEventCalendarEntryKind.FixedEvent) _fixedOutcomeIndexByEntryKey[outcome.EntryKey] = index;
                // Nhiều luật cùng loại: outcome của luật ĐỨNG TRƯỚC — cùng luật mà LiveEventCalendarDocument.TryGetRecurringRule
                // trả và SetRecurringRuleEdit thay, nên hub đọc lý do bỏ/giữ của đúng luật đang hiện.
                else if (!_recurringOutcomeIndexByType.ContainsKey(outcome.EntryKey)) _recurringOutcomeIndexByType.Add(outcome.EntryKey, index);
            }
            KeptCount = keptCount;
            DroppedCount = entries.Count - keptCount;

            _recurringCalendarByType = new Dictionary<string, RecurringLiveEventCalendar>(StringComparer.Ordinal);
            for (int index = 0; index < recurringCalendars.Count; index++)
            {
                _recurringCalendarByType[recurringCalendars[index].EventType] = recurringCalendars[index];
            }
        }

        /// <summary>Composite(recurring…, fixed) — PD-3. Đây là lịch game thật sự chạy.</summary>
        public ILiveEventCalendar Calendar { get; }

        public FixedLiveEventCalendar FixedCalendar { get; }
        public IReadOnlyList<RecurringLiveEventCalendar> RecurringCalendars { get; }

        /// <summary>Recurring trước, rồi fixed, theo <see cref="LiveEventCalendarEntryOutcome.SourceIndex"/>.</summary>
        public IReadOnlyList<LiveEventCalendarEntryOutcome> Entries { get; }

        public int EntryCount => Entries.Count;
        public int KeptCount { get; }
        public int DroppedCount { get; }

        /// <summary>Câu Problems tương thích 0.1.0 — thứ tự lỗi cấp mục theo SourceIndex (lặp rồi đợt), sau đó trùng/chồng/che.</summary>
        public IReadOnlyList<string> Problems { get; }

        public bool TryGetFixedOutcome(string entryKey, out LiveEventCalendarEntryOutcome outcome)
        {
            outcome = null;
            if (entryKey == null || !_fixedOutcomeIndexByEntryKey.TryGetValue(entryKey, out int index)) return false;
            outcome = Entries[index];
            return true;
        }

        public bool TryGetRecurringOutcome(string eventType, out LiveEventCalendarEntryOutcome outcome)
        {
            outcome = null;
            if (eventType == null || !_recurringOutcomeIndexByType.TryGetValue(eventType, out int index)) return false;
            outcome = Entries[index];
            return true;
        }

        /// <summary>
        /// Không bị cắt bởi <see cref="RecurringLiveEventCalendar.MaximumInstancesPerQuery"/>: khung rộng được chia
        /// nhỏ theo chu kỳ của luật rồi ghép lại, nên hỏi cả năm một loại lặp hằng giờ vẫn ra đủ.
        /// </summary>
        public IReadOnlyList<LiveEventInstance> GetInstancesInRange(string eventType, DateTime fromUtc, DateTime toUtc)
        {
            if (eventType == null || toUtc <= fromUtc) return Array.Empty<LiveEventInstance>();
            if (!_recurringCalendarByType.TryGetValue(eventType, out RecurringLiveEventCalendar recurringCalendar))
            {
                return Calendar.GetInstances(eventType, fromUtc, toUtc);
            }

            var result = new List<LiveEventInstance>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            // Mỗi khung dài nửa trần số đợt × chu kỳ nên một lượt hỏi không bao giờ chạm MaximumInstancesPerQuery. Chu kỳ rất
            // dài (tới cả khoảng DateTime) thì phép nhân tràn long — khi đó một khung đã phủ hết mọi khoảng hỏi được.
            long periodTicks = recurringCalendar.Period.Ticks;
            int periodsPerChunk = RecurringLiveEventCalendar.MaximumInstancesPerQuery / 2;
            long chunkTicks = periodTicks > long.MaxValue / periodsPerChunk ? long.MaxValue : periodTicks * periodsPerChunk;

            DateTime chunkStart = fromUtc;
            while (chunkStart < toUtc)
            {
                DateTime chunkEnd;
                long remainingTicks = toUtc.Ticks - chunkStart.Ticks;
                chunkEnd = remainingTicks > chunkTicks ? chunkStart.AddTicks(chunkTicks) : toUtc;

                IReadOnlyList<LiveEventInstance> chunkInstances = Calendar.GetInstances(eventType, chunkStart, chunkEnd);
                for (int index = 0; index < chunkInstances.Count; index++)
                {
                    LiveEventInstance instance = chunkInstances[index];
                    if (seenIds.Add(instance.EventId)) result.Add(instance);
                }
                chunkStart = chunkEnd;
            }

            result.Sort((left, right) => left.StartUtc.CompareTo(right.StartUtc));
            return result;
        }

        public bool TryFindInstanceById(string eventType, string eventId, DateTime aroundUtc, out LiveEventInstance instance)
        {
            instance = null;
            if (eventType == null || eventId == null) return false;

            DateTime from = SafeAddYears(aroundUtc, -2);
            DateTime to = SafeAddYears(aroundUtc, 2);
            IReadOnlyList<LiveEventInstance> candidates = GetInstancesInRange(eventType, from, to);
            for (int index = 0; index < candidates.Count; index++)
            {
                if (string.Equals(candidates[index].EventId, eventId, StringComparison.Ordinal))
                {
                    instance = candidates[index];
                    return true;
                }
            }
            return false;
        }

        private static DateTime SafeAddYears(DateTime value, int years)
        {
            try
            {
                return value.AddYears(years);
            }
            catch (ArgumentOutOfRangeException)
            {
                return years < 0 ? DateTime.MinValue : DateTime.MaxValue;
            }
        }
    }
}
