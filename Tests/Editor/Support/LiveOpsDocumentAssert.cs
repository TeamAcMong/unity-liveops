using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Tests
{
    /// <summary>
    /// So hai <see cref="LiveEventCalendarDocument"/> từng field — không tham chiếu NUnit (asmdef hỗ trợ này không có
    /// <c>nunit.framework.dll</c>): mọi mismatch ném <see cref="InvalidOperationException"/> có thông điệp rõ ràng, để
    /// bài test gọi trong thân test và NUnit tự báo fail từ exception đó.
    /// </summary>
    public static class LiveOpsDocumentAssert
    {
        /// <summary>So CHÍNH XÁC mọi field — dùng cho khứ hồi asset ↔ document (không mất thông tin).</summary>
        public static void AssertDocumentsEqual(LiveEventCalendarDocument expected, LiveEventCalendarDocument actual)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));

            RequireEqual("RemoteConfigKey", expected.RemoteConfigKey, actual.RemoteConfigKey);
            RequireSameCount("EventTypes", expected.EventTypes.Count, actual.EventTypes.Count);
            for (int index = 0; index < expected.EventTypes.Count; index++)
            {
                AssertEventTypesEqual("EventTypes[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                    expected.EventTypes[index], actual.EventTypes[index]);
            }

            RequireSameCount("RecurringRules", expected.RecurringRules.Count, actual.RecurringRules.Count);
            for (int index = 0; index < expected.RecurringRules.Count; index++)
            {
                AssertRecurringRulesEqual("RecurringRules[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                    expected.RecurringRules[index], actual.RecurringRules[index]);
            }

            RequireSameCount("FixedEvents", expected.FixedEvents.Count, actual.FixedEvents.Count);
            for (int index = 0; index < expected.FixedEvents.Count; index++)
            {
                AssertFixedEventsEqual("FixedEvents[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                    expected.FixedEvents[index], actual.FixedEvents[index]);
            }

            RequireSameCount("PublishedStamps", expected.PublishedStamps.Count, actual.PublishedStamps.Count);
            for (int index = 0; index < expected.PublishedStamps.Count; index++)
            {
                AssertPublishedStampsEqual("PublishedStamps[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                    expected.PublishedStamps[index], actual.PublishedStamps[index]);
            }

            RequireSameCount("IgnoredWarnings", expected.IgnoredWarnings.Count, actual.IgnoredWarnings.Count);
            for (int index = 0; index < expected.IgnoredWarnings.Count; index++)
            {
                AssertIgnoredWarningsEqual("IgnoredWarnings[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                    expected.IgnoredWarnings[index], actual.IgnoredWarnings[index]);
            }
        }

        public static void AssertEventTypesEqual(string label, LiveEventTypeDefinition expected, LiveEventTypeDefinition actual)
        {
            RequireEqual(label + ".TypeId", expected.TypeId, actual.TypeId);
            RequireEqual(label + ".DisplayName", expected.DisplayName, actual.DisplayName);
            RequireEqual(label + ".ColorSlot", expected.ColorSlot, actual.ColorSlot);
            RequireEqual(label + ".RequiresJoin", expected.RequiresJoin, actual.RequiresJoin);
            RequireEqual(label + ".DefaultConfigKey", expected.DefaultConfigKey, actual.DefaultConfigKey);
        }

        public static void AssertRecurringRulesEqual(string label, RecurringLiveEventRule expected, RecurringLiveEventRule actual)
        {
            RequireEqual(label + ".EventType", expected.EventType, actual.EventType);
            RequireEqual(label + ".AnchorUtcText", expected.AnchorUtcText, actual.AnchorUtcText);
            RequireEqual(label + ".IdPrefix", expected.IdPrefix, actual.IdPrefix);
            RequireEqual(label + ".PeriodHours", expected.PeriodHours, actual.PeriodHours);
            RequireEqual(label + ".ActiveHours", expected.ActiveHours, actual.ActiveHours);
            RequireEqual(label + ".ConfigKey", expected.ConfigKey, actual.ConfigKey);
        }

        public static void AssertFixedEventsEqual(string label, FixedLiveEventEntry expected, FixedLiveEventEntry actual)
        {
            RequireEqual(label + ".EntryKey", expected.EntryKey, actual.EntryKey);
            RequireEqual(label + ".EventId", expected.EventId, actual.EventId);
            RequireEqual(label + ".EventType", expected.EventType, actual.EventType);
            RequireEqual(label + ".StartUtcText", expected.StartUtcText, actual.StartUtcText);
            RequireEqual(label + ".EndUtcText", expected.EndUtcText, actual.EndUtcText);
            RequireEqual(label + ".ConfigKey", expected.ConfigKey, actual.ConfigKey);
        }

        public static void AssertPublishedStampsEqual(string label, PublishedCalendarStamp expected, PublishedCalendarStamp actual)
        {
            RequireEqual(label + ".PublishedUtcText", expected.PublishedUtcText, actual.PublishedUtcText);
            RequireEqual(label + ".Publisher", expected.Publisher, actual.Publisher);
            RequireEqual(label + ".Sha256Hex", expected.Sha256Hex, actual.Sha256Hex);
            RequireEqual(label + ".ByteCount", expected.ByteCount, actual.ByteCount);
            RequireEqual(label + ".FormatVersion", expected.FormatVersion, actual.FormatVersion);
            RequireEqual(label + ".Note", expected.Note, actual.Note);
            RequireEqual(label + ".SnapshotJson", expected.SnapshotJson, actual.SnapshotJson);
        }

        public static void AssertIgnoredWarningsEqual(string label, IgnoredCalendarWarning expected, IgnoredCalendarWarning actual)
        {
            RequireEqual(label + ".RuleId", expected.RuleId, actual.RuleId);
            RequireEqual(label + ".TargetId", expected.TargetId, actual.TargetId);
            RequireEqual(label + ".RangeStartUtcText", expected.RangeStartUtcText, actual.RangeStartUtcText);
            RequireEqual(label + ".RangeEndUtcText", expected.RangeEndUtcText, actual.RangeEndUtcText);
            RequireEqual(label + ".Note", expected.Note, actual.Note);
            RequireEqual(label + ".ExpiresUtcText", expected.ExpiresUtcText, actual.ExpiresUtcText);
        }

        /// <summary>So dãy (EventId, IsKept, DropReason) của hai lần biên dịch — dùng cho test "tương đương đã biên dịch".</summary>
        public static void AssertCompiledOutcomesEqual(string label, IReadOnlyList<LiveEventCalendarEntryOutcome> expected,
            IReadOnlyList<LiveEventCalendarEntryOutcome> actual)
        {
            RequireSameCount(label, expected.Count, actual.Count);
            for (int index = 0; index < expected.Count; index++)
            {
                string itemLabel = label + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
                RequireEqual(itemLabel + ".EventId", expected[index].EventId, actual[index].EventId);
                RequireEqual(itemLabel + ".IsKept", expected[index].IsKept, actual[index].IsKept);
                RequireEqual(itemLabel + ".DropReason", expected[index].DropReason, actual[index].DropReason);
            }
        }

        private static void RequireSameCount(string label, int expectedCount, int actualCount)
        {
            if (expectedCount != actualCount)
            {
                throw new InvalidOperationException(label + ": số phần tử khác nhau — mong " + expectedCount.ToString(CultureInfo.InvariantCulture) +
                                                     ", thấy " + actualCount.ToString(CultureInfo.InvariantCulture) + ".");
            }
        }

        private static void RequireEqual<T>(string label, T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(label + ": mong '" + FormatValue(expected) + "', thấy '" + FormatValue(actual) + "'.");
            }
        }

        private static string FormatValue<T>(T value)
        {
            return value == null ? "<null>" : value.ToString();
        }
    }
}
