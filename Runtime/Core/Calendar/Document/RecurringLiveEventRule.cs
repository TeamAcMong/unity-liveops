using System;
using System.Globalization;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật lặp NHÁP: mô tả cách sinh ra một <see cref="RecurringLiveEventCalendar"/>, nhưng chưa kiểm hợp lệ ở ctor
    /// (neo, chu kỳ, thời gian chạy đều giữ nguyên văn/nguyên số) — validator và bộ biên dịch mới phán.
    /// </summary>
    public sealed class RecurringLiveEventRule
    {
        public RecurringLiveEventRule(string eventType, string anchorUtcText, string idPrefix, int periodHours, int activeHours,
            string configKey)
        {
            EventType = eventType ?? string.Empty;
            AnchorUtcText = anchorUtcText ?? string.Empty;
            IdPrefix = idPrefix ?? string.Empty;
            PeriodHours = periodHours;
            ActiveHours = activeHours;
            ConfigKey = configKey ?? string.Empty;
        }

        /// <summary>Khoá danh tính của luật — cùng lúc chỉ một luật mỗi loại.</summary>
        public string EventType { get; }

        public string AnchorUtcText { get; }

        /// <summary>"" = mặc định <c>eventType + "-"</c>, như <see cref="RecurringLiveEventCalendar"/>.</summary>
        public string IdPrefix { get; }

        public string EffectiveIdPrefix => IdPrefix.Length > 0 ? IdPrefix : EventType + "-";

        public int PeriodHours { get; }
        public int ActiveHours { get; }
        public string ConfigKey { get; }
        public bool HasOwnConfigKey => ConfigKey.Length > 0;

        public bool TryGetAnchorUtc(out DateTime anchorUtc) => LiveEventUtcText.TryParse(AnchorUtcText, out anchorUtc);

        public string CanonicalText =>
            EventType + "\n" + AnchorUtcText + "\n" + EffectiveIdPrefix + "\n" +
            PeriodHours.ToString(CultureInfo.InvariantCulture) + "\n" +
            ActiveHours.ToString(CultureInfo.InvariantCulture) + "\n" + ConfigKey;

        public RecurringLiveEventRule WithAnchor(string anchorUtcText)
        {
            return new RecurringLiveEventRule(EventType, anchorUtcText, IdPrefix, PeriodHours, ActiveHours, ConfigKey);
        }

        public RecurringLiveEventRule WithIdPrefix(string idPrefix)
        {
            return new RecurringLiveEventRule(EventType, AnchorUtcText, idPrefix, PeriodHours, ActiveHours, ConfigKey);
        }

        public RecurringLiveEventRule WithPeriodHours(int periodHours)
        {
            return new RecurringLiveEventRule(EventType, AnchorUtcText, IdPrefix, periodHours, ActiveHours, ConfigKey);
        }

        public RecurringLiveEventRule WithActiveHours(int activeHours)
        {
            return new RecurringLiveEventRule(EventType, AnchorUtcText, IdPrefix, PeriodHours, activeHours, ConfigKey);
        }

        public RecurringLiveEventRule WithConfigKey(string configKey)
        {
            return new RecurringLiveEventRule(EventType, AnchorUtcText, IdPrefix, PeriodHours, ActiveHours, configKey);
        }
    }
}
