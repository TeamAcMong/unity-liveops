using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Lịch lặp lại: từ một mốc, cứ mỗi <see cref="Period"/> lại có một đợt dài <see cref="ActiveDuration"/> (phần còn lại của chu
    /// kỳ là thời gian nghỉ). Vd mốc thứ Hai 00:00 UTC, chu kỳ 7 ngày, chạy 3 ngày = event 3 ngày đầu mỗi tuần.
    ///
    /// <para>Id đợt = tiền tố + số thứ tự lần lặp (âm nếu trước mốc), nên mọi máy và mọi lần cài lại đều ra cùng id. Không cần mạng.</para>
    /// </summary>
    public sealed class RecurringLiveEventCalendar : ILiveEventCalendar
    {
        /// <summary>Chặn vòng lặp khi hỏi một khung giờ quá rộng so với chu kỳ.</summary>
        public const int MaximumInstancesPerQuery = 512;

        private readonly string[] _eventTypes;

        /// <param name="idPrefix">Mặc định <c>eventType + "-"</c>. Đổi tiền tố = mọi đợt mang id mới (người chơi bắt đầu lại).</param>
        public RecurringLiveEventCalendar(string eventType, DateTime anchorUtc, TimeSpan period, TimeSpan activeDuration,
                                          string idPrefix = null, string configKey = null)
        {
            LiveEventInstance.ValidateIdentifier(eventType, nameof(eventType), "Loại event");
            if (period <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(period), "Chu kỳ phải dài hơn 0.");
            if (activeDuration <= TimeSpan.Zero || activeDuration > period)
            {
                throw new ArgumentOutOfRangeException(nameof(activeDuration), "Thời gian chạy phải trong (0, chu kỳ].");
            }

            EventType = eventType;
            AnchorUtc = DateTime.SpecifyKind(anchorUtc, DateTimeKind.Utc);
            Period = period;
            ActiveDuration = activeDuration;
            IdPrefix = idPrefix ?? eventType + "-";
            ConfigKey = configKey ?? string.Empty;
            _eventTypes = new[] { eventType };
        }

        public string EventType { get; }
        public DateTime AnchorUtc { get; }
        public TimeSpan Period { get; }
        public TimeSpan ActiveDuration { get; }
        public string IdPrefix { get; }
        public string ConfigKey { get; }
        public IReadOnlyCollection<string> EventTypes => _eventTypes;

        /// <summary>Số thứ tự của chu kỳ chứa <paramref name="utc"/> (âm nếu trước mốc).</summary>
        public long OccurrenceIndexAt(DateTime utc)
        {
            long elapsedTicks = utc.Ticks - AnchorUtc.Ticks;
            long periodTicks = Period.Ticks;
            return elapsedTicks >= 0 ? elapsedTicks / periodTicks : -((-elapsedTicks + periodTicks - 1) / periodTicks);
        }

        public LiveEventInstance GetOccurrence(long index)
        {
            if (!TryGetOccurrence(index, out LiveEventInstance occurrence))
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Lần lặp nằm ngoài khoảng DateTime.");
            }
            return occurrence;
        }

        public IReadOnlyList<LiveEventInstance> GetInstances(string eventType, DateTime fromUtc, DateTime toUtc)
        {
            var instances = new List<LiveEventInstance>();
            if (!string.Equals(eventType, EventType, StringComparison.Ordinal) || toUtc <= fromUtc) return instances;

            long index = OccurrenceIndexAt(fromUtc);
            while (instances.Count < MaximumInstancesPerQuery && TryGetOccurrence(index, out LiveEventInstance occurrence))
            {
                if (occurrence.StartUtc >= toUtc) break;
                if (occurrence.EndUtc > fromUtc) instances.Add(occurrence);
                index++;
            }
            return instances;
        }

        private bool TryGetOccurrence(long index, out LiveEventInstance occurrence)
        {
            occurrence = null;
            long startTicks;
            long endTicks;
            try
            {
                checked
                {
                    startTicks = AnchorUtc.Ticks + index * Period.Ticks;
                    endTicks = startTicks + ActiveDuration.Ticks;
                }
            }
            catch (OverflowException)
            {
                return false;
            }
            if (startTicks < DateTime.MinValue.Ticks || endTicks > DateTime.MaxValue.Ticks) return false;

            occurrence = new LiveEventInstance(IdPrefix + index.ToString(CultureInfo.InvariantCulture), EventType,
                                               new DateTime(startTicks, DateTimeKind.Utc), new DateTime(endTicks, DateTimeKind.Utc),
                                               ConfigKey);
            return true;
        }
    }
}
