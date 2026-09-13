using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Ghép nhiều lịch thành một, vd lịch lặp lại cho battle pass + lịch JSON từ remote config cho event đặc biệt.
    ///
    /// <para>Lịch đứng trước được ưu tiên: đợt của lịch sau bị bỏ nếu chồng giờ với bất kỳ đợt cùng loại nào của lịch trước, hoặc
    /// trùng id với đợt đã giữ. Việc kiểm tra chồng giờ hỏi lịch trước theo đúng khung giờ của đợt đó, nên một đợt được giữ hay bị
    /// bỏ không phụ thuộc khung giờ người gọi hỏi.</para>
    /// </summary>
    public sealed class CompositeLiveEventCalendar : ILiveEventCalendar
    {
        private readonly List<ILiveEventCalendar> _calendars = new List<ILiveEventCalendar>();

        public CompositeLiveEventCalendar(IEnumerable<ILiveEventCalendar> calendars)
        {
            if (calendars == null) return;
            foreach (ILiveEventCalendar calendar in calendars)
            {
                if (calendar != null) _calendars.Add(calendar);
            }
        }

        public CompositeLiveEventCalendar(params ILiveEventCalendar[] calendars) : this((IEnumerable<ILiveEventCalendar>)calendars)
        {
        }

        public IReadOnlyList<ILiveEventCalendar> Calendars => _calendars;

        public IReadOnlyCollection<string> EventTypes
        {
            get
            {
                var types = new HashSet<string>(StringComparer.Ordinal);
                foreach (ILiveEventCalendar calendar in _calendars)
                {
                    foreach (string eventType in calendar.EventTypes) types.Add(eventType);
                }
                return types;
            }
        }

        public IReadOnlyList<LiveEventInstance> GetInstances(string eventType, DateTime fromUtc, DateTime toUtc)
        {
            var kept = new List<LiveEventInstance>();
            var keptIds = new HashSet<string>(StringComparer.Ordinal);
            for (int calendarIndex = 0; calendarIndex < _calendars.Count; calendarIndex++)
            {
                foreach (LiveEventInstance candidate in _calendars[calendarIndex].GetInstances(eventType, fromUtc, toUtc))
                {
                    if (candidate == null || keptIds.Contains(candidate.EventId)) continue;
                    if (IsShadowedByEarlierCalendar(calendarIndex, eventType, candidate)) continue;
                    kept.Add(candidate);
                    keptIds.Add(candidate.EventId);
                }
            }
            kept.Sort((left, right) => left.StartUtc.CompareTo(right.StartUtc));
            return kept;
        }

        private bool IsShadowedByEarlierCalendar(int calendarIndex, string eventType, LiveEventInstance candidate)
        {
            for (int earlier = 0; earlier < calendarIndex; earlier++)
            {
                if (_calendars[earlier].GetInstances(eventType, candidate.StartUtc, candidate.EndUtc).Count > 0) return true;
            }
            return false;
        }
    }
}
