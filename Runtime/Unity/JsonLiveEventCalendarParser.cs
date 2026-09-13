using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DreamTech.LiveOps.Unity
{
    /// <summary>Kết quả đọc lịch JSON: lịch đã lọc và danh sách lý do các mục bị bỏ.</summary>
    public sealed class LiveEventCalendarParseResult
    {
        internal LiveEventCalendarParseResult(FixedLiveEventCalendar calendar, IReadOnlyList<string> problems)
        {
            Calendar = calendar;
            Problems = problems;
        }

        public FixedLiveEventCalendar Calendar { get; }
        public IReadOnlyList<string> Problems { get; }
        public bool HasProblems => Problems.Count > 0;
    }

    /// <summary>
    /// Đọc lịch event từ JSON — thường là giá trị của một key remote config. Định dạng:
    /// <code>
    /// { "events": [
    ///   { "id": "lava-quest-2026-09", "type": "lava-quest",
    ///     "startUtc": "2026-09-14T00:00:00Z", "endUtc": "2026-09-17T00:00:00Z", "configKey": "lava_quest_v2" }
    /// ] }
    /// </code>
    /// Giờ theo ISO 8601; không ghi múi giờ thì hiểu là UTC. <c>configKey</c> không bắt buộc.
    ///
    /// <para>Không ném exception: JSON sai trên remote config không được làm game crash. Mục hỏng bị bỏ và ghi lý do vào
    /// <see cref="LiveEventCalendarParseResult.Problems"/> (gồm cả trùng id và chồng giờ mà <see cref="FixedLiveEventCalendar"/> phát hiện).</para>
    /// </summary>
    public static class JsonLiveEventCalendarParser
    {
        private static readonly string[] DateFormats =
        {
            "yyyy-MM-dd'T'HH:mm:ssK",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
            "yyyy-MM-dd'T'HH:mmK",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
            "yyyy-MM-dd'T'HH:mm",
        };

#pragma warning disable 0649 // Field do JsonUtility gán.
        [Serializable]
        private sealed class CalendarDocument
        {
            public EventEntry[] events;
        }

        [Serializable]
        private sealed class EventEntry
        {
            public string id;
            public string type;
            public string startUtc;
            public string endUtc;
            public string configKey;
        }
#pragma warning restore 0649

        public static LiveEventCalendarParseResult Parse(string json)
        {
            var problems = new List<string>();
            if (string.IsNullOrWhiteSpace(json)) return new LiveEventCalendarParseResult(FixedLiveEventCalendar.Empty, problems);

            CalendarDocument document;
            try
            {
                document = JsonUtility.FromJson<CalendarDocument>(json);
            }
            catch (ArgumentException exception)
            {
                problems.Add("JSON lịch event hỏng: " + exception.Message);
                return new LiveEventCalendarParseResult(FixedLiveEventCalendar.Empty, problems);
            }
            if (document == null || document.events == null)
            {
                problems.Add("JSON lịch event thiếu mảng \"events\".");
                return new LiveEventCalendarParseResult(FixedLiveEventCalendar.Empty, problems);
            }

            var instances = new List<LiveEventInstance>(document.events.Length);
            for (int index = 0; index < document.events.Length; index++)
            {
                EventEntry entry = document.events[index];
                string label = "Mục thứ " + (index + 1).ToString(CultureInfo.InvariantCulture);
                if (entry == null)
                {
                    problems.Add(label + " rỗng — bỏ qua.");
                    continue;
                }
                if (!TryParseUtc(entry.startUtc, out DateTime startUtc))
                {
                    problems.Add(label + " ('" + entry.id + "'): startUtc không phải giờ ISO 8601: '" + entry.startUtc + "' — bỏ qua.");
                    continue;
                }
                if (!TryParseUtc(entry.endUtc, out DateTime endUtc))
                {
                    problems.Add(label + " ('" + entry.id + "'): endUtc không phải giờ ISO 8601: '" + entry.endUtc + "' — bỏ qua.");
                    continue;
                }
                try
                {
                    instances.Add(new LiveEventInstance(entry.id, entry.type, startUtc, endUtc, entry.configKey));
                }
                catch (ArgumentException exception)
                {
                    problems.Add(label + ": " + exception.Message + " — bỏ qua.");
                }
            }

            var calendar = new FixedLiveEventCalendar(instances);
            problems.AddRange(calendar.Problems);
            return new LiveEventCalendarParseResult(calendar, problems);
        }

        private static bool TryParseUtc(string text, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrEmpty(text)) return false;
            if (!DateTimeOffset.TryParseExact(text.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                                              out DateTimeOffset parsed))
            {
                return false;
            }
            utc = parsed.UtcDateTime;
            return true;
        }
    }
}
