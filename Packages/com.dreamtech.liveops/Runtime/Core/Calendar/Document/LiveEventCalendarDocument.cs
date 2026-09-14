using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Toàn bộ nội dung một lịch NHÁP: định nghĩa loại, luật lặp, đợt cố định, lịch sử đăng, cảnh báo đã bỏ qua. Bất
    /// biến — mọi sửa đi qua <see cref="LiveEventCalendarDocumentBuilder"/> hoặc <see cref="LiveEventCalendarEdits"/>.
    /// Có thể chứa mục hỏng (giờ đọc không được, id rỗng…): tài liệu KHÔNG kiểm hợp lệ, để hub và bộ biên dịch cùng
    /// thấy đúng cái người dùng đã gõ — validator mới là nơi phán.
    /// </summary>
    public sealed class LiveEventCalendarDocument
    {
        public const string DefaultRemoteConfigKey = "liveops_calendar";

        private static readonly LiveEventCalendarDocument EmptyDocument = new LiveEventCalendarDocumentBuilder().Build();

        private readonly List<LiveEventTypeDefinition> _eventTypes;
        private readonly List<RecurringLiveEventRule> _recurringRules;
        private readonly List<FixedLiveEventEntry> _fixedEvents;
        private readonly List<PublishedCalendarStamp> _publishedStamps;
        private readonly List<IgnoredCalendarWarning> _ignoredWarnings;

        private readonly Dictionary<string, LiveEventTypeDefinition> _eventTypeById;
        private readonly Dictionary<string, RecurringLiveEventRule> _recurringRuleByType;
        private readonly Dictionary<string, int> _fixedEventIndexByKey;

        internal LiveEventCalendarDocument(string remoteConfigKey, List<LiveEventTypeDefinition> eventTypes,
            List<RecurringLiveEventRule> recurringRules, List<FixedLiveEventEntry> fixedEvents,
            List<PublishedCalendarStamp> publishedStamps, List<IgnoredCalendarWarning> ignoredWarnings)
        {
            RemoteConfigKey = remoteConfigKey;
            _eventTypes = eventTypes;
            _recurringRules = recurringRules;
            _fixedEvents = fixedEvents;
            _publishedStamps = publishedStamps;
            _ignoredWarnings = ignoredWarnings;

            _eventTypeById = new Dictionary<string, LiveEventTypeDefinition>(_eventTypes.Count, StringComparer.Ordinal);
            for (int index = 0; index < _eventTypes.Count; index++) _eventTypeById[_eventTypes[index].TypeId] = _eventTypes[index];

            _recurringRuleByType = new Dictionary<string, RecurringLiveEventRule>(_recurringRules.Count, StringComparer.Ordinal);
            for (int index = 0; index < _recurringRules.Count; index++) _recurringRuleByType[_recurringRules[index].EventType] = _recurringRules[index];

            _fixedEventIndexByKey = new Dictionary<string, int>(_fixedEvents.Count, StringComparer.Ordinal);
            for (int index = 0; index < _fixedEvents.Count; index++) _fixedEventIndexByKey[_fixedEvents[index].EntryKey] = index;
        }

        public static LiveEventCalendarDocument Empty => EmptyDocument;

        public string RemoteConfigKey { get; }

        /// <summary>Thứ tự = thứ tự làn hiện trên Lịch / bảng Loại event.</summary>
        public IReadOnlyList<LiveEventTypeDefinition> EventTypes => _eventTypes;

        /// <summary>Thứ tự trong asset — KHÔNG phải thứ tự xuất JSON (mục 5.2 luật 7, giữ nguyên khi đưa làn lên/xuống).</summary>
        public IReadOnlyList<RecurringLiveEventRule> RecurringRules => _recurringRules;

        /// <summary>Thứ tự trong asset — dùng <see cref="LiveEventCalendarExportOrder"/> để lấy thứ tự game sẽ nhận.</summary>
        public IReadOnlyList<FixedLiveEventEntry> FixedEvents => _fixedEvents;

        /// <summary>Cũ → mới.</summary>
        public IReadOnlyList<PublishedCalendarStamp> PublishedStamps => _publishedStamps;

        public IReadOnlyList<IgnoredCalendarWarning> IgnoredWarnings => _ignoredWarnings;

        /// <summary><c>null</c> khi chưa đăng lần nào.</summary>
        public PublishedCalendarStamp LatestStamp => _publishedStamps.Count > 0 ? _publishedStamps[_publishedStamps.Count - 1] : null;

        public bool TryGetEventType(string typeId, out LiveEventTypeDefinition type)
        {
            if (typeId != null) return _eventTypeById.TryGetValue(typeId, out type);
            type = null;
            return false;
        }

        public bool TryGetRecurringRule(string eventType, out RecurringLiveEventRule rule)
        {
            if (eventType != null) return _recurringRuleByType.TryGetValue(eventType, out rule);
            rule = null;
            return false;
        }

        public bool TryGetFixedEvent(string entryKey, out FixedLiveEventEntry entry)
        {
            entry = null;
            if (entryKey == null || !_fixedEventIndexByKey.TryGetValue(entryKey, out int index)) return false;
            entry = _fixedEvents[index];
            return true;
        }

        public int IndexOfFixedEvent(string entryKey)
        {
            if (entryKey != null && _fixedEventIndexByKey.TryGetValue(entryKey, out int index)) return index;
            return -1;
        }

        /// <summary>configKey riêng của đợt, hoặc mặc định của loại, hoặc "" nếu cả hai rỗng.</summary>
        public string EffectiveConfigKeyOf(FixedLiveEventEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (entry.HasOwnConfigKey) return entry.ConfigKey;
            return TryGetEventType(entry.EventType, out LiveEventTypeDefinition type) ? type.DefaultConfigKey : string.Empty;
        }

        public string EffectiveConfigKeyOf(RecurringLiveEventRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (rule.HasOwnConfigKey) return rule.ConfigKey;
            return TryGetEventType(rule.EventType, out LiveEventTypeDefinition type) ? type.DefaultConfigKey : string.Empty;
        }
    }
}
