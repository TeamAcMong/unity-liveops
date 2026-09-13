using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Lịch là một danh sách đợt cho sẵn — thường đọc từ remote config (xem <c>JsonLiveEventCalendarParser</c> của
    /// <c>DreamTech.LiveOps.Unity</c>).
    ///
    /// <para>Dữ liệu từ xa có thể sai, nên lịch KHÔNG ném lỗi mà bỏ mục hỏng và ghi lý do vào <see cref="Problems"/>: mục rỗng, trùng id
    /// (giữ mục xuất hiện trước), chồng giờ với đợt cùng loại (giữ đợt bắt đầu sớm hơn; bắt đầu cùng lúc thì giữ mục đứng trước).</para>
    /// </summary>
    public sealed class FixedLiveEventCalendar : ILiveEventCalendar
    {
        public static readonly FixedLiveEventCalendar Empty = new FixedLiveEventCalendar(null);

        private readonly List<LiveEventInstance> _instances = new List<LiveEventInstance>();
        private readonly List<string> _problems = new List<string>();
        private readonly HashSet<string> _eventTypes = new HashSet<string>(StringComparer.Ordinal);

        public FixedLiveEventCalendar(IEnumerable<LiveEventInstance> instances)
        {
            var candidates = new List<KeyValuePair<int, LiveEventInstance>>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            if (instances != null)
            {
                int position = 0;
                foreach (LiveEventInstance instance in instances)
                {
                    position++;
                    if (instance == null)
                    {
                        _problems.Add("Mục thứ " + position + " rỗng — bỏ qua.");
                        continue;
                    }
                    if (!seenIds.Add(instance.EventId))
                    {
                        _problems.Add("Trùng id '" + instance.EventId + "' ở mục thứ " + position + " — giữ mục xuất hiện trước.");
                        continue;
                    }
                    candidates.Add(new KeyValuePair<int, LiveEventInstance>(position, instance));
                }
            }

            // Sắp theo giờ bắt đầu, cùng giờ thì theo thứ tự trong danh sách — để kết quả không phụ thuộc thuật toán sort.
            candidates.Sort((left, right) =>
            {
                int byStart = left.Value.StartUtc.CompareTo(right.Value.StartUtc);
                return byStart != 0 ? byStart : left.Key.CompareTo(right.Key);
            });

            var latestKeptByType = new Dictionary<string, LiveEventInstance>(StringComparer.Ordinal);
            foreach (KeyValuePair<int, LiveEventInstance> candidate in candidates)
            {
                LiveEventInstance instance = candidate.Value;
                if (latestKeptByType.TryGetValue(instance.EventType, out LiveEventInstance kept) && instance.StartUtc < kept.EndUtc)
                {
                    _problems.Add("Đợt '" + instance.EventId + "' chồng giờ với '" + kept.EventId + "' cùng loại '" +
                                  instance.EventType + "' — bỏ '" + instance.EventId + "'.");
                    continue;
                }
                _instances.Add(instance);
                _eventTypes.Add(instance.EventType);
                latestKeptByType[instance.EventType] = instance;
            }
        }

        /// <summary>Các đợt được giữ lại, sắp theo giờ bắt đầu.</summary>
        public IReadOnlyList<LiveEventInstance> Instances => _instances;

        public IReadOnlyList<string> Problems => _problems;
        public bool HasProblems => _problems.Count > 0;
        public IReadOnlyCollection<string> EventTypes => _eventTypes;

        public IReadOnlyList<LiveEventInstance> GetInstances(string eventType, DateTime fromUtc, DateTime toUtc)
        {
            var result = new List<LiveEventInstance>();
            if (eventType == null || toUtc <= fromUtc) return result;
            foreach (LiveEventInstance instance in _instances)
            {
                if (instance.StartUtc >= toUtc) break;
                if (instance.EndUtc > fromUtc && string.Equals(instance.EventType, eventType, StringComparison.Ordinal)) result.Add(instance);
            }
            return result;
        }
    }
}
