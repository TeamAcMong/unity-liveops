using System;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Một đợt cố định trong tài liệu lịch NHÁP — chưa phải <see cref="LiveEventInstance"/> mà game sẽ chạy. Giờ giữ
    /// nguyên văn (không parse ở ctor): tài liệu phải chứa được đợt hỏng ("2026-10-3") để hub và bộ biên dịch cùng thấy
    /// và báo lỗi; validator mới là nơi phán đợt này hợp lệ hay không.
    /// </summary>
    public sealed class FixedLiveEventEntry
    {
        public FixedLiveEventEntry(string entryKey, string eventId, string eventType, string startUtcText, string endUtcText,
            string configKey)
        {
            if (string.IsNullOrEmpty(entryKey)) throw new ArgumentException("entryKey không được rỗng.", nameof(entryKey));

            EntryKey = entryKey;
            EventId = eventId ?? string.Empty;
            EventType = eventType ?? string.Empty;
            StartUtcText = startUtcText ?? string.Empty;
            EndUtcText = endUtcText ?? string.Empty;
            ConfigKey = configKey ?? string.Empty;
        }

        /// <summary>32 hex, ổn định qua đổi id — dùng cho chọn, Undo, diff "chưa lưu".</summary>
        public string EntryKey { get; }

        public string EventId { get; }
        public string EventType { get; }
        public string StartUtcText { get; }
        public string EndUtcText { get; }

        /// <summary>"" = kế thừa <see cref="LiveEventTypeDefinition.DefaultConfigKey"/> của loại.</summary>
        public string ConfigKey { get; }

        public bool HasOwnConfigKey => ConfigKey.Length > 0;

        public bool TryGetStartUtc(out DateTime startUtc) => LiveEventUtcText.TryParse(StartUtcText, out startUtc);
        public bool TryGetEndUtc(out DateTime endUtc) => LiveEventUtcText.TryParse(EndUtcText, out endUtc);

        /// <summary>Dấu vân dùng cho "Đã xem" — không gồm <see cref="EntryKey"/> vì id đó không đi vào JSON.</summary>
        public string CanonicalText => EventId + "\n" + EventType + "\n" + StartUtcText + "\n" + EndUtcText + "\n" + ConfigKey;

        public FixedLiveEventEntry WithEventId(string eventId)
        {
            return new FixedLiveEventEntry(EntryKey, eventId, EventType, StartUtcText, EndUtcText, ConfigKey);
        }

        public FixedLiveEventEntry WithEventType(string eventType)
        {
            return new FixedLiveEventEntry(EntryKey, EventId, eventType, StartUtcText, EndUtcText, ConfigKey);
        }

        public FixedLiveEventEntry WithTimes(string startUtcText, string endUtcText)
        {
            return new FixedLiveEventEntry(EntryKey, EventId, EventType, startUtcText, endUtcText, ConfigKey);
        }

        public FixedLiveEventEntry WithConfigKey(string configKey)
        {
            return new FixedLiveEventEntry(EntryKey, EventId, EventType, StartUtcText, EndUtcText, configKey);
        }

        public static string CreateEntryKey() => Guid.NewGuid().ToString("N");
    }
}
