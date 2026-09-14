using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Biên dịch một <see cref="LiveEventCalendarDocument"/> (có thể chứa mục hỏng) thành <see cref="LiveEventCalendarCompilation"/>
    /// — không bao giờ ném; mục hỏng bị bỏ và ghi lý do, y hệt cách <see cref="FixedLiveEventCalendar"/> và
    /// <see cref="RecurringLiveEventCalendar"/> xử lý dữ liệu remote config hỏng.
    /// </summary>
    public static class LiveEventCalendarCompiler
    {
        private static readonly LiveEventCalendarDropReason[] EmptyReasons = Array.Empty<LiveEventCalendarDropReason>();

        /// <summary>Biên dịch ĐÚNG thứ tự đầu vào — dùng cho JSON đọc vào (parser game, bản remote đã dán).</summary>
        public static LiveEventCalendarCompilation Compile(LiveEventCalendarDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return CompileCore(document);
        }

        /// <summary>
        /// (V-6) = <c>Compile(LiveEventCalendarExportOrder.Apply(document))</c>. Dùng cho MỌI tài liệu nháp/asset (hub,
        /// <c>LiveEventCalendarAsset.Compile/ToParseResult</c>), để kết quả trùng khớp với việc game đọc JSON đã xuất.
        /// <see cref="LiveEventCalendarEntryOutcome.SourceIndex"/> là vị trí trong thứ tự xuất (= "mục thứ N" của JSON).
        /// </summary>
        public static LiveEventCalendarCompilation CompileInExportOrder(LiveEventCalendarDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return CompileCore(LiveEventCalendarExportOrder.Apply(document));
        }

        private static LiveEventCalendarCompilation CompileCore(LiveEventCalendarDocument document)
        {
            var recurringEntries = new List<LiveEventCalendarEntryOutcome>(document.RecurringRules.Count);
            var recurringCalendars = new List<RecurringLiveEventCalendar>();
            var recurringCalendarByType = new Dictionary<string, RecurringLiveEventCalendar>(StringComparer.Ordinal);
            var problems = new List<string>();

            CompileRecurringRules(document, recurringEntries, recurringCalendars, recurringCalendarByType, problems);

            var fixedEntries = new LiveEventCalendarEntryOutcome[document.FixedEvents.Count];
            var fixedInstances = new List<LiveEventInstance>();
            CompileFixedEvents(document, recurringCalendarByType, fixedEntries, fixedInstances, problems);

            var fixedCalendar = new FixedLiveEventCalendar(fixedInstances);

            var allCalendars = new List<ILiveEventCalendar>(recurringCalendars.Count + 1);
            allCalendars.AddRange(recurringCalendars);
            allCalendars.Add(fixedCalendar);
            var calendar = new CompositeLiveEventCalendar(allCalendars);

            var allEntries = new List<LiveEventCalendarEntryOutcome>(recurringEntries.Count + fixedEntries.Length);
            allEntries.AddRange(recurringEntries);
            allEntries.AddRange(fixedEntries);

            return new LiveEventCalendarCompilation(calendar, fixedCalendar, recurringCalendars, allEntries, problems);
        }

        // ----- Luật lặp -----

        private static void CompileRecurringRules(LiveEventCalendarDocument document, List<LiveEventCalendarEntryOutcome> entries,
            List<RecurringLiveEventCalendar> keptCalendars, Dictionary<string, RecurringLiveEventCalendar> keptCalendarByType,
            List<string> problems)
        {
            var seenTypes = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<RecurringLiveEventRule> rules = document.RecurringRules;

            for (int index = 0; index < rules.Count; index++)
            {
                RecurringLiveEventRule rule = rules[index];
                string label = "Luật lặp thứ " + (index + 1).ToString(CultureInfo.InvariantCulture) + " ('" + rule.EventType + "')";

                if (!IsValidRecurringRule(rule, out DateTime anchorUtc, out string invalidReasonText))
                {
                    entries.Add(new LiveEventCalendarEntryOutcome(LiveEventCalendarEntryKind.RecurringRule, index, rule.EventType,
                        string.Empty, rule.EventType, false, LiveEventCalendarDropReason.InvalidRecurringRule, EmptyReasons,
                        string.Empty, null, null, label + ": " + invalidReasonText + " — bỏ qua."));
                    problems.Add(label + ": " + invalidReasonText + " — bỏ qua.");
                    continue;
                }

                if (!seenTypes.Add(rule.EventType))
                {
                    string problemText = label + ": trùng luật cho loại '" + rule.EventType + "' — giữ luật đứng trước.";
                    entries.Add(new LiveEventCalendarEntryOutcome(LiveEventCalendarEntryKind.RecurringRule, index, rule.EventType,
                        string.Empty, rule.EventType, false, LiveEventCalendarDropReason.DuplicateRecurringType, EmptyReasons,
                        rule.EventType, null, null, problemText));
                    problems.Add(problemText);
                    continue;
                }

                var calendar = new RecurringLiveEventCalendar(rule.EventType, anchorUtc, TimeSpan.FromHours(rule.PeriodHours),
                    TimeSpan.FromHours(rule.ActiveHours), rule.EffectiveIdPrefix, document.EffectiveConfigKeyOf(rule));
                keptCalendars.Add(calendar);
                keptCalendarByType[rule.EventType] = calendar;

                entries.Add(new LiveEventCalendarEntryOutcome(LiveEventCalendarEntryKind.RecurringRule, index, rule.EventType,
                    string.Empty, rule.EventType, true, LiveEventCalendarDropReason.None, EmptyReasons, string.Empty, null, null,
                    string.Empty));
            }
        }

        private static bool IsValidRecurringRule(RecurringLiveEventRule rule, out DateTime anchorUtc, out string reasonText)
        {
            anchorUtc = default;
            if (!rule.TryGetAnchorUtc(out anchorUtc))
            {
                reasonText = "neo không đọc được: '" + rule.AnchorUtcText + "'";
                return false;
            }
            if (rule.PeriodHours <= 0)
            {
                reasonText = "chu kỳ phải > 0 giờ";
                return false;
            }
            if (rule.ActiveHours <= 0 || rule.ActiveHours > rule.PeriodHours)
            {
                reasonText = "thời gian chạy phải trong (0, chu kỳ]";
                return false;
            }
            if (!IsValidIdentifierCharset(rule.EffectiveIdPrefix))
            {
                reasonText = "tiền tố id chứa '#' hoặc xuống dòng";
                return false;
            }
            if (!IsValidIdentifier(rule.EventType))
            {
                reasonText = "loại event sai quy tắc";
                return false;
            }
            reasonText = string.Empty;
            return true;
        }

        // ----- Đợt cố định -----

        private static void CompileFixedEvents(LiveEventCalendarDocument document,
            Dictionary<string, RecurringLiveEventCalendar> recurringCalendarByType, LiveEventCalendarEntryOutcome[] outcomes,
            List<LiveEventInstance> keptInstances, List<string> problems)
        {
            IReadOnlyList<FixedLiveEventEntry> fixedEvents = document.FixedEvents;
            var startUtcBySourceIndex = new DateTime[fixedEvents.Count];
            var endUtcBySourceIndex = new DateTime[fixedEvents.Count];
            var itemLevelValidSourceIndexes = new List<int>(fixedEvents.Count);

            for (int index = 0; index < fixedEvents.Count; index++)
            {
                FixedLiveEventEntry entry = fixedEvents[index];
                string label = "Mục thứ " + (index + 1).ToString(CultureInfo.InvariantCulture) + " ('" + entry.EventId + "')";

                bool startReadable = entry.TryGetStartUtc(out DateTime startUtc);
                bool endReadable = entry.TryGetEndUtc(out DateTime endUtc);
                var reasons = new List<LiveEventCalendarDropReason>(4);
                if (!startReadable) reasons.Add(LiveEventCalendarDropReason.UnreadableStartUtc);
                if (!endReadable) reasons.Add(LiveEventCalendarDropReason.UnreadableEndUtc);
                if (!IsValidIdentifier(entry.EventId) || !IsValidIdentifier(entry.EventType))
                {
                    reasons.Add(LiveEventCalendarDropReason.InvalidIdentifier);
                }
                if (startReadable && endReadable && endUtc <= startUtc) reasons.Add(LiveEventCalendarDropReason.EndNotAfterStart);

                if (reasons.Count > 0)
                {
                    IReadOnlyList<LiveEventCalendarDropReason> additional = reasons.Count > 1
                        ? reasons.GetRange(1, reasons.Count - 1)
                        : EmptyReasons;
                    string problemText = label + ": " + DescribeItemLevelReason(reasons[0]) + " — bỏ qua.";
                    outcomes[index] = new LiveEventCalendarEntryOutcome(LiveEventCalendarEntryKind.FixedEvent, index, entry.EntryKey,
                        entry.EventId, entry.EventType, false, reasons[0], additional, string.Empty, null, null, problemText);
                    problems.Add(problemText);
                    continue;
                }

                startUtcBySourceIndex[index] = startUtc;
                endUtcBySourceIndex[index] = endUtc;
                itemLevelValidSourceIndexes.Add(index);
            }

            // Trùng id: đúng thứ tự nhận (given order), giữ mục xuất hiện trước — như FixedLiveEventCalendar 0.1.0.
            var seenIds = new Dictionary<string, int>(StringComparer.Ordinal);
            var dedupSurvivorIndexes = new List<int>(itemLevelValidSourceIndexes.Count);
            for (int listIndex = 0; listIndex < itemLevelValidSourceIndexes.Count; listIndex++)
            {
                int sourceIndex = itemLevelValidSourceIndexes[listIndex];
                FixedLiveEventEntry entry = fixedEvents[sourceIndex];
                if (seenIds.TryGetValue(entry.EventId, out int firstSourceIndex))
                {
                    string relatedId = fixedEvents[firstSourceIndex].EventId;
                    string problemText = "Trùng id '" + entry.EventId + "' ở mục thứ " + (sourceIndex + 1).ToString(CultureInfo.InvariantCulture) +
                                          " — giữ mục xuất hiện trước.";
                    outcomes[sourceIndex] = new LiveEventCalendarEntryOutcome(LiveEventCalendarEntryKind.FixedEvent, sourceIndex,
                        entry.EntryKey, entry.EventId, entry.EventType, false, LiveEventCalendarDropReason.DuplicateEventId, EmptyReasons,
                        relatedId, null, null, problemText);
                    problems.Add(problemText);
                    continue;
                }
                seenIds[entry.EventId] = sourceIndex;
                dedupSurvivorIndexes.Add(sourceIndex);
            }

            // Chồng giờ cùng loại: sắp theo start rồi quét, giữ đợt đang mở sớm hơn — như FixedLiveEventCalendar 0.1.0.
            var sortedSurvivorIndexes = new List<int>(dedupSurvivorIndexes);
            sortedSurvivorIndexes.Sort((left, right) =>
            {
                int byStart = startUtcBySourceIndex[left].CompareTo(startUtcBySourceIndex[right]);
                return byStart != 0 ? byStart : left.CompareTo(right);
            });

            var latestKeptIndexByType = new Dictionary<string, int>(StringComparer.Ordinal);
            var overlapSurvivorIndexes = new List<int>(sortedSurvivorIndexes.Count);
            for (int listIndex = 0; listIndex < sortedSurvivorIndexes.Count; listIndex++)
            {
                int sourceIndex = sortedSurvivorIndexes[listIndex];
                FixedLiveEventEntry entry = fixedEvents[sourceIndex];
                if (latestKeptIndexByType.TryGetValue(entry.EventType, out int keptIndex) &&
                    startUtcBySourceIndex[sourceIndex] < endUtcBySourceIndex[keptIndex])
                {
                    FixedLiveEventEntry keptEntry = fixedEvents[keptIndex];
                    DateTime overlapStart = startUtcBySourceIndex[sourceIndex];
                    DateTime keptEnd = endUtcBySourceIndex[keptIndex];
                    DateTime overlapEnd = endUtcBySourceIndex[sourceIndex] < keptEnd ? endUtcBySourceIndex[sourceIndex] : keptEnd;
                    string problemText = "Đợt '" + entry.EventId + "' chồng giờ với '" + keptEntry.EventId + "' cùng loại '" +
                                          entry.EventType + "' — bỏ '" + entry.EventId + "'.";
                    outcomes[sourceIndex] = new LiveEventCalendarEntryOutcome(LiveEventCalendarEntryKind.FixedEvent, sourceIndex,
                        entry.EntryKey, entry.EventId, entry.EventType, false, LiveEventCalendarDropReason.OverlapsSameType, EmptyReasons,
                        keptEntry.EventId, overlapStart, overlapEnd, problemText);
                    problems.Add(problemText);
                    continue;
                }
                overlapSurvivorIndexes.Add(sourceIndex);
                latestKeptIndexByType[entry.EventType] = sourceIndex;
            }

            // Bị luật lặp che: composite ưu tiên lịch lặp — đợt vẫn ở trong FixedCalendar (đúng như component thật),
            // chỉ đánh dấu IsKept=false ở outcome để hub/validator biết game sẽ không thấy đợt này.
            for (int listIndex = 0; listIndex < overlapSurvivorIndexes.Count; listIndex++)
            {
                int sourceIndex = overlapSurvivorIndexes[listIndex];
                FixedLiveEventEntry entry = fixedEvents[sourceIndex];
                DateTime startUtc = startUtcBySourceIndex[sourceIndex];
                DateTime endUtc = endUtcBySourceIndex[sourceIndex];

                if (recurringCalendarByType.TryGetValue(entry.EventType, out RecurringLiveEventCalendar recurringCalendar))
                {
                    IReadOnlyList<LiveEventInstance> shadowing = recurringCalendar.GetInstances(entry.EventType, startUtc, endUtc);
                    if (shadowing.Count > 0)
                    {
                        string relatedId = shadowing[0].EventId;
                        string problemText = "Đợt '" + entry.EventId + "' bị luật lặp '" + entry.EventType + "' che — game giữ '" +
                                              relatedId + "'.";
                        outcomes[sourceIndex] = new LiveEventCalendarEntryOutcome(LiveEventCalendarEntryKind.FixedEvent, sourceIndex,
                            entry.EntryKey, entry.EventId, entry.EventType, false, LiveEventCalendarDropReason.ShadowedByRecurring,
                            EmptyReasons, relatedId, startUtc, endUtc, problemText);
                        problems.Add(problemText);
                        continue;
                    }
                }

                outcomes[sourceIndex] = new LiveEventCalendarEntryOutcome(LiveEventCalendarEntryKind.FixedEvent, sourceIndex,
                    entry.EntryKey, entry.EventId, entry.EventType, true, LiveEventCalendarDropReason.None, EmptyReasons,
                    string.Empty, null, null, string.Empty);
            }

            // FixedCalendar dựng từ mọi mục qua kiểm mục + chồng giờ — KỂ CẢ mục bị luật lặp che (component này không
            // biết luật lặp, giống hệt FixedLiveEventCalendar thật; Composite mới là nơi che động lúc hỏi).
            for (int listIndex = 0; listIndex < overlapSurvivorIndexes.Count; listIndex++)
            {
                int sourceIndex = overlapSurvivorIndexes[listIndex];
                FixedLiveEventEntry entry = fixedEvents[sourceIndex];
                keptInstances.Add(new LiveEventInstance(entry.EventId, entry.EventType, startUtcBySourceIndex[sourceIndex],
                    endUtcBySourceIndex[sourceIndex], document.EffectiveConfigKeyOf(entry)));
            }
        }

        private static string DescribeItemLevelReason(LiveEventCalendarDropReason reason)
        {
            switch (reason)
            {
                case LiveEventCalendarDropReason.UnreadableStartUtc: return "startUtc không phải giờ ISO 8601";
                case LiveEventCalendarDropReason.UnreadableEndUtc: return "endUtc không phải giờ ISO 8601";
                case LiveEventCalendarDropReason.InvalidIdentifier: return "id hoặc loại không hợp lệ";
                case LiveEventCalendarDropReason.EndNotAfterStart: return "kết thúc không sau khi bắt đầu";
                default: return "không hợp lệ";
            }
        }

        private static bool IsValidIdentifier(string value)
        {
            return !string.IsNullOrEmpty(value) && IsValidIdentifierCharset(value);
        }

        /// <summary>Không rỗng đã kiểm riêng ở nơi cần (tiền tố được phép rỗng nếu <c>EffectiveIdPrefix</c> tự thay).</summary>
        private static bool IsValidIdentifierCharset(string value)
        {
            if (value == null) return false;
            return value.IndexOf(LiveEventInstance.ReservedSeparator) < 0 && value.IndexOf('\n') < 0 && value.IndexOf('\r') < 0;
        }
    }
}
