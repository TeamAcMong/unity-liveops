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

        /// <summary>
        /// Trần số giờ của chu kỳ luật lặp = cả khoảng <see cref="DateTime"/>. Vì sao: chu kỳ dài hơn thế không bao giờ sinh
        /// được lần lặp thứ hai, và số <c>int</c> từ JSON remote xấu (vd 300000000) làm <c>TimeSpan.FromHours</c> ném
        /// OverflowException — bộ biên dịch hứa không ném nên phải chặn trước khi dựng lịch.
        /// </summary>
        private static readonly long MaximumRecurringPeriodHours = DateTime.MaxValue.Ticks / TimeSpan.TicksPerHour;

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

                // Nhân số nguyên thay vì TimeSpan.FromHours(double): giờ đã kẹp dưới trần nên không tràn, và không lệch làm tròn.
                var calendar = new RecurringLiveEventCalendar(rule.EventType, anchorUtc,
                    TimeSpan.FromTicks(rule.PeriodHours * TimeSpan.TicksPerHour),
                    TimeSpan.FromTicks(rule.ActiveHours * TimeSpan.TicksPerHour), rule.EffectiveIdPrefix, document.EffectiveConfigKeyOf(rule));
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
            if (rule.PeriodHours > MaximumRecurringPeriodHours)
            {
                reasonText = "chu kỳ dài quá " + MaximumRecurringPeriodHours.ToString(CultureInfo.InvariantCulture) + " giờ";
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
                    string problemText = DescribeItemLevelProblem(index, entry, reasons[0]);
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
            // Số thứ tự trong câu Problems đếm theo danh sách ĐÃ LỌC mục hỏng (vị trí 1-based trong itemLevelValidSourceIndexes),
            // không theo SourceIndex: 0.1.0 dựng FixedLiveEventCalendar chỉ từ các instance parser dựng được, nên "mục thứ N"
            // của câu trùng id là vị trí trong danh sách đó (khoá bằng golden G-GOLDEN). SourceIndex của outcome vẫn là vị trí gốc.
            var duplicateSurvivorIndexes = new List<int>(itemLevelValidSourceIndexes.Count);
            for (int listIndex = 0; listIndex < itemLevelValidSourceIndexes.Count; listIndex++)
            {
                int sourceIndex = itemLevelValidSourceIndexes[listIndex];
                FixedLiveEventEntry entry = fixedEvents[sourceIndex];
                if (seenIds.TryGetValue(entry.EventId, out int firstSourceIndex))
                {
                    string relatedId = fixedEvents[firstSourceIndex].EventId;
                    string problemText = "Trùng id '" + entry.EventId + "' ở mục thứ " + (listIndex + 1).ToString(CultureInfo.InvariantCulture) +
                                          " — giữ mục xuất hiện trước.";
                    outcomes[sourceIndex] = new LiveEventCalendarEntryOutcome(LiveEventCalendarEntryKind.FixedEvent, sourceIndex,
                        entry.EntryKey, entry.EventId, entry.EventType, false, LiveEventCalendarDropReason.DuplicateEventId, EmptyReasons,
                        relatedId, null, null, problemText);
                    problems.Add(problemText);
                    continue;
                }
                seenIds[entry.EventId] = sourceIndex;
                duplicateSurvivorIndexes.Add(sourceIndex);
            }

            // Chồng giờ cùng loại: sắp theo start rồi quét, giữ đợt đang mở sớm hơn — như FixedLiveEventCalendar 0.1.0.
            var sortedSurvivorIndexes = new List<int>(duplicateSurvivorIndexes);
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
            // chỉ đánh dấu IsKept=false ở outcome để hub/validator biết game sẽ không thấy đợt này. Hai cách che (6.1 luật 7):
            // chồng giờ với một lần lặp cùng loại, hoặc trùng id với một lần lặp của luật cùng loại dù không chồng giờ —
            // Composite bỏ đợt trùng id mỗi khi khung hỏi chứa cả lần lặp đó, nên đợt chỉ hiện ở vài khung hỏi và bản ghi
            // của người chơi (theo id) dính vào lần lặp; coi là bị bỏ để hub không báo "giữ" một đợt game không tin cậy được.
            for (int listIndex = 0; listIndex < overlapSurvivorIndexes.Count; listIndex++)
            {
                int sourceIndex = overlapSurvivorIndexes[listIndex];
                FixedLiveEventEntry entry = fixedEvents[sourceIndex];
                DateTime startUtc = startUtcBySourceIndex[sourceIndex];
                DateTime endUtc = endUtcBySourceIndex[sourceIndex];

                if (recurringCalendarByType.TryGetValue(entry.EventType, out RecurringLiveEventCalendar recurringCalendar))
                {
                    IReadOnlyList<LiveEventInstance> shadowing = recurringCalendar.GetInstances(entry.EventType, startUtc, endUtc);
                    string relatedId = shadowing.Count > 0 ? shadowing[0].EventId : null;
                    if (relatedId == null && IsOccurrenceIdOf(recurringCalendar, entry.EventId)) relatedId = entry.EventId;
                    if (relatedId != null)
                    {
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

        /// <summary>
        /// Câu Problems của lỗi cấp mục — dựng NGUYÊN VĂN như JsonLiveEventCalendarParser 0.1.0 (khoá bằng golden G-GOLDEN):
        /// giờ hỏng in id trong ngoặc + giá trị thô; id/loại hỏng và giờ ngược in đúng message của
        /// <see cref="LiveEventInstance"/> (ValidateIdentifier kiểm id rồi loại, rồi ctor kiểm end &gt; start). Vì sao dựng lại
        /// thay vì gọi ctor rồi bắt exception: <c>ArgumentException.Message</c> nối thêm đuôi tên tham số khác nhau giữa Mono
        /// 2022.3 và .NET 6000.6 — đuôi đó không phải câu của game, hub hiện câu này cho người dùng.
        /// </summary>
        private static string DescribeItemLevelProblem(int sourceIndex, FixedLiveEventEntry entry, LiveEventCalendarDropReason reason)
        {
            string label = "Mục thứ " + (sourceIndex + 1).ToString(CultureInfo.InvariantCulture);
            switch (reason)
            {
                case LiveEventCalendarDropReason.UnreadableStartUtc:
                    return label + " ('" + entry.EventId + "'): startUtc không phải giờ ISO 8601: '" + entry.StartUtcText + "' — bỏ qua.";
                case LiveEventCalendarDropReason.UnreadableEndUtc:
                    return label + " ('" + entry.EventId + "'): endUtc không phải giờ ISO 8601: '" + entry.EndUtcText + "' — bỏ qua.";
                case LiveEventCalendarDropReason.InvalidIdentifier:
                    string identifierMessage = !IsValidIdentifier(entry.EventId)
                        ? DescribeInvalidIdentifier(entry.EventId, "Event id")
                        : DescribeInvalidIdentifier(entry.EventType, "Loại event");
                    return label + ": " + identifierMessage + " — bỏ qua.";
                case LiveEventCalendarDropReason.EndNotAfterStart:
                    return label + ": Đợt event phải kết thúc sau khi bắt đầu: " + entry.EventId + " — bỏ qua.";
                default:
                    return label + ": không hợp lệ — bỏ qua.";
            }
        }

        /// <summary>Cùng hai câu của <c>LiveEventInstance.ValidateIdentifier</c> (không kèm đuôi tên tham số của runtime).</summary>
        private static string DescribeInvalidIdentifier(string value, string fieldLabel)
        {
            if (string.IsNullOrEmpty(value)) return fieldLabel + " không được rỗng.";
            return fieldLabel + " không được chứa '" + LiveEventInstance.ReservedSeparator + "' hay xuống dòng: " + value;
        }

        /// <summary>
        /// Id có đúng dạng id lần lặp của lịch này không: tiền tố + số thứ tự viết như <c>long.ToString(InvariantCulture)</c>
        /// (không số 0 thừa, không dấu '+', "-0" không phải), và lần lặp đó nằm trong khoảng <see cref="DateTime"/> — cùng điều
        /// kiện lịch lặp dùng khi sinh đợt, nên chỉ trả true với id mà lịch lặp thật sự có thể trả về.
        /// </summary>
        private static bool IsOccurrenceIdOf(RecurringLiveEventCalendar recurringCalendar, string eventId)
        {
            string prefix = recurringCalendar.IdPrefix;
            if (eventId.Length <= prefix.Length || !eventId.StartsWith(prefix, StringComparison.Ordinal)) return false;

            string indexText = eventId.Substring(prefix.Length);
            if (!long.TryParse(indexText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long occurrenceIndex)) return false;
            if (!string.Equals(occurrenceIndex.ToString(CultureInfo.InvariantCulture), indexText, StringComparison.Ordinal)) return false;

            try
            {
                checked
                {
                    long startTicks = recurringCalendar.AnchorUtc.Ticks + occurrenceIndex * recurringCalendar.Period.Ticks;
                    long endTicks = startTicks + recurringCalendar.ActiveDuration.Ticks;
                    return startTicks >= DateTime.MinValue.Ticks && endTicks <= DateTime.MaxValue.Ticks;
                }
            }
            catch (OverflowException)
            {
                return false;
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
