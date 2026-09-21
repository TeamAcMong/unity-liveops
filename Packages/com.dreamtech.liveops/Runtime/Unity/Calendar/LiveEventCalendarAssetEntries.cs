using System;
using UnityEngine;

// Các lớp lưu trữ thuần của LiveEventCalendarAsset. Vì sao tách khỏi kiểu tài liệu core: core là noEngineReferences và bất
// biến (không [SerializeField] được), còn Unity cần field có thể ghi để serialize YAML. Tên field ở đây LÀ định dạng file
// asset của người dùng (mục 4.1) — đổi tên field là làm mất dữ liệu asset đã commit, test SerializedFieldNames_Stable chặn.
// Thứ tự field trong lớp = thứ tự dòng trong YAML, để git diff đọc được từ trên xuống.
// Chỉ đọc từ ngoài: mọi ghi đi qua LiveEventCalendarAsset.ApplyDocument, để asset luôn được ghi trọn một tài liệu.

namespace DreamTech.LiveOps.Unity
{
    /// <summary>Một loại event lưu trong asset — tương ứng <see cref="LiveEventTypeDefinition"/>.</summary>
    [Serializable]
    public sealed class LiveEventTypeAssetEntry
    {
        [SerializeField] private string typeId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private int colorSlot;
        [SerializeField] private bool requiresJoin;
        [SerializeField] private string defaultConfigKey = string.Empty;

        public LiveEventTypeAssetEntry()
        {
        }

        internal LiveEventTypeAssetEntry(LiveEventTypeDefinition type)
        {
            typeId = type.TypeId;
            displayName = type.DisplayName;
            colorSlot = type.ColorSlot;
            requiresJoin = type.RequiresJoin;
            defaultConfigKey = type.DefaultConfigKey;
        }

        public string TypeId => typeId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public int ColorSlot => colorSlot;
        public bool RequiresJoin => requiresJoin;
        public string DefaultConfigKey => defaultConfigKey ?? string.Empty;
    }

    /// <summary>Một luật lặp lưu trong asset — tương ứng <see cref="RecurringLiveEventRule"/>; neo giữ chuỗi nguyên văn (PD-2).</summary>
    [Serializable]
    public sealed class RecurringRuleAssetEntry
    {
        [SerializeField] private string eventType = string.Empty;
        [SerializeField] private string anchorUtc = string.Empty;
        [SerializeField] private string idPrefix = string.Empty;
        [SerializeField] private int periodHours;
        [SerializeField] private int activeHours;
        [SerializeField] private string configKey = string.Empty;

        public RecurringRuleAssetEntry()
        {
        }

        internal RecurringRuleAssetEntry(RecurringLiveEventRule rule)
        {
            eventType = rule.EventType;
            anchorUtc = rule.AnchorUtcText;
            idPrefix = rule.IdPrefix;
            periodHours = rule.PeriodHours;
            activeHours = rule.ActiveHours;
            configKey = rule.ConfigKey;
        }

        public string EventType => eventType ?? string.Empty;
        public string AnchorUtc => anchorUtc ?? string.Empty;
        public string IdPrefix => idPrefix ?? string.Empty;
        public int PeriodHours => periodHours;
        public int ActiveHours => activeHours;
        public string ConfigKey => configKey ?? string.Empty;
    }

    /// <summary>Một đợt cố định lưu trong asset — tương ứng <see cref="FixedLiveEventEntry"/>; giờ giữ chuỗi nguyên văn (PD-2).</summary>
    [Serializable]
    public sealed class FixedEventAssetEntry
    {
        [SerializeField] private string entryKey = string.Empty;
        [SerializeField] private string eventId = string.Empty;
        [SerializeField] private string eventType = string.Empty;
        [SerializeField] private string startUtc = string.Empty;
        [SerializeField] private string endUtc = string.Empty;
        [SerializeField] private string configKey = string.Empty;

        public FixedEventAssetEntry()
        {
        }

        internal FixedEventAssetEntry(FixedLiveEventEntry entry)
        {
            entryKey = entry.EntryKey;
            eventId = entry.EventId;
            eventType = entry.EventType;
            startUtc = entry.StartUtcText;
            endUtc = entry.EndUtcText;
            configKey = entry.ConfigKey;
        }

        public string EntryKey => entryKey ?? string.Empty;
        public string EventId => eventId ?? string.Empty;
        public string EventType => eventType ?? string.Empty;
        public string StartUtc => startUtc ?? string.Empty;
        public string EndUtc => endUtc ?? string.Empty;
        public string ConfigKey => configKey ?? string.Empty;
    }

    /// <summary>Một dấu "đã đăng" lưu trong asset — tương ứng <see cref="PublishedCalendarStamp"/>.</summary>
    [Serializable]
    public sealed class PublishedStampAssetEntry
    {
        [SerializeField] private string publishedUtc = string.Empty;
        [SerializeField] private string publisher = string.Empty;
        [SerializeField] private string sha256 = string.Empty;
        [SerializeField] private int byteCount;
        [SerializeField] private int formatVersion;
        [SerializeField] private string note = string.Empty;
        [SerializeField] private string snapshotJson = string.Empty;

        public PublishedStampAssetEntry()
        {
        }

        internal PublishedStampAssetEntry(PublishedCalendarStamp stamp)
        {
            publishedUtc = stamp.PublishedUtcText;
            publisher = stamp.Publisher;
            sha256 = stamp.Sha256Hex;
            byteCount = stamp.ByteCount;
            formatVersion = stamp.FormatVersion;
            note = stamp.Note;
            snapshotJson = stamp.SnapshotJson;
        }

        public string PublishedUtc => publishedUtc ?? string.Empty;
        public string Publisher => publisher ?? string.Empty;
        public string Sha256 => sha256 ?? string.Empty;
        public int ByteCount => byteCount;
        public int FormatVersion => formatVersion;
        public string Note => note ?? string.Empty;
        public string SnapshotJson => snapshotJson ?? string.Empty;
    }

    /// <summary>Một cảnh báo Kiểm lịch đã bỏ qua lưu trong asset — tương ứng <see cref="IgnoredCalendarWarning"/>.</summary>
    [Serializable]
    public sealed class IgnoredWarningAssetEntry
    {
        [SerializeField] private string ruleId = string.Empty;
        [SerializeField] private string targetId = string.Empty;
        [SerializeField] private string rangeStartUtc = string.Empty;
        [SerializeField] private string rangeEndUtc = string.Empty;
        [SerializeField] private string note = string.Empty;
        [SerializeField] private string expiresUtc = string.Empty;

        public IgnoredWarningAssetEntry()
        {
        }

        internal IgnoredWarningAssetEntry(IgnoredCalendarWarning warning)
        {
            ruleId = warning.RuleId;
            targetId = warning.TargetId;
            rangeStartUtc = warning.RangeStartUtcText;
            rangeEndUtc = warning.RangeEndUtcText;
            note = warning.Note;
            expiresUtc = warning.ExpiresUtcText;
        }

        public string RuleId => ruleId ?? string.Empty;
        public string TargetId => targetId ?? string.Empty;
        public string RangeStartUtc => rangeStartUtc ?? string.Empty;
        public string RangeEndUtc => rangeEndUtc ?? string.Empty;
        public string Note => note ?? string.Empty;
        public string ExpiresUtc => expiresUtc ?? string.Empty;
    }
}
