using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Dựng <see cref="LiveEventCalendarDocument"/> từng bước — dùng để đọc JSON/asset vào tài liệu lần đầu. Sửa một
    /// tài liệu đã có thì dùng <see cref="LiveEventCalendarEdits"/> (giữ nguyên phần không đổi, rẻ hơn dựng lại).
    /// </summary>
    public sealed class LiveEventCalendarDocumentBuilder
    {
        private string _remoteConfigKey = LiveEventCalendarDocument.DefaultRemoteConfigKey;
        private readonly List<LiveEventTypeDefinition> _eventTypes = new List<LiveEventTypeDefinition>();
        private readonly List<RecurringLiveEventRule> _recurringRules = new List<RecurringLiveEventRule>();
        private readonly List<FixedLiveEventEntry> _fixedEvents = new List<FixedLiveEventEntry>();
        private readonly List<PublishedCalendarStamp> _publishedStamps = new List<PublishedCalendarStamp>();
        private readonly List<IgnoredCalendarWarning> _ignoredWarnings = new List<IgnoredCalendarWarning>();

        private readonly HashSet<string> _eventTypeIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _fixedEntryKeys = new HashSet<string>(StringComparer.Ordinal);

        public LiveEventCalendarDocumentBuilder()
        {
        }

        public LiveEventCalendarDocumentBuilder(LiveEventCalendarDocument source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            _remoteConfigKey = source.RemoteConfigKey;
            for (int index = 0; index < source.EventTypes.Count; index++) WithEventType(source.EventTypes[index]);
            for (int index = 0; index < source.RecurringRules.Count; index++) WithRecurringRule(source.RecurringRules[index]);
            for (int index = 0; index < source.FixedEvents.Count; index++) WithFixedEvent(source.FixedEvents[index]);
            for (int index = 0; index < source.PublishedStamps.Count; index++) WithPublishedStamp(source.PublishedStamps[index]);
            for (int index = 0; index < source.IgnoredWarnings.Count; index++) WithIgnoredWarning(source.IgnoredWarnings[index]);
        }

        public LiveEventCalendarDocumentBuilder WithRemoteConfigKey(string remoteConfigKey)
        {
            _remoteConfigKey = remoteConfigKey ?? string.Empty;
            return this;
        }

        public LiveEventCalendarDocumentBuilder WithEventType(LiveEventTypeDefinition type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!_eventTypeIds.Add(type.TypeId)) throw new ArgumentException("Trùng loại event '" + type.TypeId + "'.", nameof(type));

            _eventTypes.Add(type);
            return this;
        }

        /// <summary>Không ném khi trùng loại — tài liệu dán vào có thể trùng, validator báo (luật recurring-rule-invalid).</summary>
        public LiveEventCalendarDocumentBuilder WithRecurringRule(RecurringLiveEventRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            _recurringRules.Add(rule);
            return this;
        }

        public LiveEventCalendarDocumentBuilder WithFixedEvent(FixedLiveEventEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (!_fixedEntryKeys.Add(entry.EntryKey)) throw new ArgumentException("Trùng entryKey '" + entry.EntryKey + "'.", nameof(entry));

            _fixedEvents.Add(entry);
            return this;
        }

        public LiveEventCalendarDocumentBuilder WithPublishedStamp(PublishedCalendarStamp stamp)
        {
            if (stamp == null) throw new ArgumentNullException(nameof(stamp));

            _publishedStamps.Add(stamp);
            return this;
        }

        public LiveEventCalendarDocumentBuilder WithIgnoredWarning(IgnoredCalendarWarning warning)
        {
            if (warning == null) throw new ArgumentNullException(nameof(warning));

            _ignoredWarnings.Add(warning);
            return this;
        }

        public LiveEventCalendarDocument Build()
        {
            return new LiveEventCalendarDocument(_remoteConfigKey, new List<LiveEventTypeDefinition>(_eventTypes),
                new List<RecurringLiveEventRule>(_recurringRules), new List<FixedLiveEventEntry>(_fixedEvents),
                new List<PublishedCalendarStamp>(_publishedStamps), new List<IgnoredCalendarWarning>(_ignoredWarnings));
        }
    }
}
