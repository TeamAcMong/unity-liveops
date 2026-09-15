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

        /// <summary>
        /// (V-4) Định nghĩa DUY NHẤT của "tương đương theo JSON": <paramref name="actual"/> (thường là
        /// <c>ParseDocument(Write(expected)).Document</c>) nói với game đúng điều <paramref name="expected"/> nói, dù bộ ghi đã
        /// chủ ý làm mất thông tin. Vì sao không so field thô: bộ ghi sắp <c>events</c> theo thứ tự xuất (luật 8), ghi configKey
        /// hiệu lực (luật 5), ghi tiền tố hiệu lực (luật 6), chuẩn hoá giờ đọc được (luật 4) và không ghi EntryKey, loại, dấu,
        /// cảnh báo (luật 9) — so thô sẽ đỏ ở mọi tài liệu thật.
        /// <para>So <c>LiveEventCalendarExportOrder.Apply(expected)</c> với <paramref name="actual"/> THEO THỨ TỰ. Đợt: id, loại, giờ
        /// đầu/cuối, configKey hiệu lực. Luật: loại, neo, tiền tố hiệu lực, chu kỳ, thời gian chạy, configKey hiệu lực. Giờ đọc
        /// được so theo tick (V-19: <see cref="LiveEventUtcText.Format"/> cắt phần lẻ giây nên so chuỗi đã format coi ".2Z" =
        /// ".7Z"); giờ không đọc được so nguyên văn (đó là thứ game đọc rồi bỏ). Định dạng 1 không có mảng recurring nên
        /// <paramref name="actual"/> phải không có luật nào; luật của <paramref name="expected"/> không được so.</para>
        /// Lệch thì ném <see cref="InvalidOperationException"/> nêu mục thứ N (1-based, theo thứ tự trong JSON) + field + hai giá trị.
        /// </summary>
        public static void AreJsonEquivalent(LiveEventCalendarDocument expected, LiveEventCalendarDocument actual, LiveEventCalendarJsonFormat format)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));
            if (format != LiveEventCalendarJsonFormat.Version1 && format != LiveEventCalendarJsonFormat.Version2)
            {
                throw new ArgumentOutOfRangeException(nameof(format), "Định dạng JSON lịch chỉ có 1 hoặc 2.");
            }

            LiveEventCalendarDocument ordered = LiveEventCalendarExportOrder.Apply(expected);

            if (format == LiveEventCalendarJsonFormat.Version2)
            {
                RequireSameCount("recurring", ordered.RecurringRules.Count, actual.RecurringRules.Count);
                for (int index = 0; index < ordered.RecurringRules.Count; index++)
                {
                    RecurringLiveEventRule expectedRule = ordered.RecurringRules[index];
                    RecurringLiveEventRule actualRule = actual.RecurringRules[index];
                    string label = "recurring mục thứ " + (index + 1).ToString(CultureInfo.InvariantCulture) + " ('" + expectedRule.EventType + "')";
                    RequireEqual(label + ".type", expectedRule.EventType, actualRule.EventType);
                    RequireSameInstant(label + ".anchorUtc", expectedRule.AnchorUtcText, actualRule.AnchorUtcText);
                    RequireEqual(label + ".idPrefix (hiệu lực)", expectedRule.EffectiveIdPrefix, actualRule.EffectiveIdPrefix);
                    RequireEqual(label + ".periodHours", expectedRule.PeriodHours, actualRule.PeriodHours);
                    RequireEqual(label + ".activeHours", expectedRule.ActiveHours, actualRule.ActiveHours);
                    RequireEqual(label + ".configKey (hiệu lực)", ordered.EffectiveConfigKeyOf(expectedRule), actual.EffectiveConfigKeyOf(actualRule));
                }
            }
            else
            {
                // Định dạng 1 không ghi recurring: mảng vắng → parser đọc null → không có luật (SP-11). Có luật ở đây là parser
                // hoặc bộ ghi đã để lọt mảng recurring vào JSON của game 0.1.0.
                RequireSameCount("recurring (định dạng 1 không ghi)", 0, actual.RecurringRules.Count);
            }

            RequireSameCount("events", ordered.FixedEvents.Count, actual.FixedEvents.Count);
            for (int index = 0; index < ordered.FixedEvents.Count; index++)
            {
                FixedLiveEventEntry expectedEntry = ordered.FixedEvents[index];
                FixedLiveEventEntry actualEntry = actual.FixedEvents[index];
                string label = "events mục thứ " + (index + 1).ToString(CultureInfo.InvariantCulture) + " ('" + expectedEntry.EventId + "')";
                RequireEqual(label + ".id", expectedEntry.EventId, actualEntry.EventId);
                RequireEqual(label + ".type", expectedEntry.EventType, actualEntry.EventType);
                RequireSameInstant(label + ".startUtc", expectedEntry.StartUtcText, actualEntry.StartUtcText);
                RequireSameInstant(label + ".endUtc", expectedEntry.EndUtcText, actualEntry.EndUtcText);
                RequireEqual(label + ".configKey (hiệu lực)", ordered.EffectiveConfigKeyOf(expectedEntry), actual.EffectiveConfigKeyOf(actualEntry));
            }
        }

        /// <summary>
        /// Cùng thời điểm theo TICK nếu cả hai đọc được; cùng nguyên văn nếu cả hai không đọc được; một bên đọc được một bên không
        /// là lệch (game sẽ giữ đợt ở bản này và bỏ ở bản kia).
        /// </summary>
        private static void RequireSameInstant(string label, string expectedText, string actualText)
        {
            bool expectedReadable = LiveEventUtcText.TryParse(expectedText, out DateTime expectedUtc);
            bool actualReadable = LiveEventUtcText.TryParse(actualText, out DateTime actualUtc);
            if (expectedReadable != actualReadable)
            {
                throw new InvalidOperationException(label + ": mong '" + expectedText + "' (" + ReadableWord(expectedReadable) + "), thấy '" +
                                                     actualText + "' (" + ReadableWord(actualReadable) + ").");
            }
            if (!expectedReadable)
            {
                RequireEqual(label + " (nguyên văn, không đọc được)", expectedText, actualText);
                return;
            }
            if (expectedUtc.Ticks != actualUtc.Ticks)
            {
                throw new InvalidOperationException(label + ": khác thời điểm — mong '" + expectedText + "' (" +
                                                     expectedUtc.Ticks.ToString(CultureInfo.InvariantCulture) + " tick), thấy '" + actualText + "' (" +
                                                     actualUtc.Ticks.ToString(CultureInfo.InvariantCulture) + " tick).");
            }
        }

        private static string ReadableWord(bool readable)
        {
            return readable ? "đọc được" : "không đọc được";
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
