using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật 11 <c>long-gap-between-events</c> (vá V-1): với mỗi loại khai báo KHÔNG có luật lặp, khoảng trống từ cuối một khối đợt
    /// được giữ tới lúc đợt kế cùng loại bắt đầu dài hơn <see cref="LiveEventCalendarValidationSettings.LongGapThreshold"/> — người
    /// chơi mất thói quen quay lại. Mốc kế lấy trong MỌI đợt cùng loại đọc được <c>startUtc</c>, kể cả đợt bị bỏ: thiết kế đo
    /// "20/9 → 1/10" tới lava-quest-2026-10 dù đợt đó bị bỏ vì <c>endUtc</c> hỏng — lỗi bỏ đã có phát hiện của luật 1, còn
    /// khoảng trống là ý đồ lịch của designer, sửa giờ kết thúc không làm khoảng trống biến mất.
    /// <para>Giá trị thô: Found = "cuối khối · đầu đợt kế" (giờ chuẩn); Expected = "" (không có giá trị đúng để đề xuất).
    /// <c>RelatedId</c> = id đợt kế.</para>
    /// </summary>
    internal sealed class LongGapBetweenEventsRule : ILiveEventCalendarRule
    {
        public string RuleId => LiveEventCalendarRuleIds.LongGapBetweenEvents;
        public LiveEventCalendarConsequence Consequence => LiveEventCalendarConsequence.ShouldReview;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            LiveEventCalendarDocument document = context.Document;
            LiveEventCalendarValidationSettings settings = context.Settings;
            DateTime lookbackStartUtc = SubtractSaturating(context.NowUtc, settings.LongGapLookback);
            var findings = new List<LiveEventCalendarFinding>();

            IReadOnlyList<LiveEventTypeDefinition> eventTypes = document.EventTypes;
            var measuredTypes = new HashSet<string>(StringComparer.Ordinal);
            for (int typeIndex = 0; typeIndex < eventTypes.Count; typeIndex++)
            {
                string eventType = eventTypes[typeIndex].TypeId;
                // Tài liệu dán vào có thể khai một loại hai lần — đo một lần, không nhân đôi phát hiện.
                if (!measuredTypes.Add(eventType) || !context.IncludesEventType(eventType)) continue;
                // Loại có luật lặp tự lấp lịch theo chu kỳ — khoảng trống do chu kỳ/thời gian chạy quyết, không phải do đợt cố định.
                if (document.TryGetRecurringRule(eventType, out _)) continue;

                CollectEvents(context, eventType, out List<TimedEvent> readableStarts, out List<TimedEvent> keptEvents);
                if (readableStarts.Count < 2 || keptEvents.Count == 0) continue;

                keptEvents.Sort(CompareByStart);
                int blockIndex = 0;
                while (blockIndex < keptEvents.Count)
                {
                    // Gộp đợt chồng hoặc chạm nhau (start == end) thành một khối — khoảng trống chỉ tính giữa hai khối rời.
                    DateTime blockEndUtc = keptEvents[blockIndex].EndUtc;
                    int nextIndex = blockIndex + 1;
                    while (nextIndex < keptEvents.Count && keptEvents[nextIndex].StartUtc <= blockEndUtc)
                    {
                        if (keptEvents[nextIndex].EndUtc > blockEndUtc) blockEndUtc = keptEvents[nextIndex].EndUtc;
                        nextIndex++;
                    }
                    blockIndex = nextIndex;

                    TimedEvent nextEvent = FirstStartingAtOrAfter(readableStarts, blockEndUtc);
                    // Không có đợt kế: khoảng trống mở về tương lai không phải "giữa hai đợt" — designer chưa lên lịch, không phải bỏ sót.
                    if (nextEvent == null) continue;
                    if (nextEvent.StartUtc - blockEndUtc <= settings.LongGapThreshold) continue;
                    // Khoảng trống đã xa trong quá khứ không còn sửa được cho người chơi — chỉ nhắc khoảng kết thúc trong cửa sổ nhìn lại.
                    if (blockEndUtc < lookbackStartUtc) continue;

                    findings.Add(BuildFinding(eventType, blockEndUtc, nextEvent));
                }
            }

            return findings.Count > 0 ? LiveEventCalendarRuleResult.Found(RuleId, findings) : LiveEventCalendarRuleResult.Passed(RuleId);
        }

        private LiveEventCalendarFinding BuildFinding(string eventType, DateTime blockEndUtc, TimedEvent nextEvent)
        {
            return new LiveEventCalendarFindingBuilder(RuleId, LiveEventCalendarDetailCodes.LongGap, Consequence,
                    LiveEventCalendarTargetKind.EventType, eventType)
                .WithTargetEntryKey(eventType)
                .WithRelatedId(nextEvent.EventId)
                .WithTexts(LiveEventUtcText.Format(blockEndUtc) + LiveEventCalendarFindingBuilder.ValueSeparator + LiveEventUtcText.Format(nextEvent.StartUtc),
                    string.Empty)
                .WithRange(blockEndUtc, nextEvent.StartUtc)
                .WithAnchor(blockEndUtc)
                // Ignorable: khoảng trống có thể là chủ ý (tháng nghỉ) — "Bỏ qua cảnh báo…" với phạm vi đúng khoảng này.
                .WithRepairs(LiveEventCalendarRepairKind.Ignorable, null)
                .Build();
        }

        /// <summary>
        /// Duyệt theo thứ tự xuất (Document của ngữ cảnh đã sắp) để "đợt kế" khi hai đợt cùng giờ bắt đầu là đợt đứng trước trong JSON.
        /// Đợt được giữ luôn đọc được hai giờ (bộ biên dịch đã kiểm).
        /// </summary>
        private static void CollectEvents(LiveEventCalendarCheckContext context, string eventType, out List<TimedEvent> readableStarts,
            out List<TimedEvent> keptEvents)
        {
            readableStarts = new List<TimedEvent>();
            keptEvents = new List<TimedEvent>();
            IReadOnlyList<LiveEventCalendarEntryOutcome> entries = context.Compilation.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = entries[index];
                if (outcome.Kind != LiveEventCalendarEntryKind.FixedEvent) continue;
                if (!string.Equals(outcome.EventType, eventType, StringComparison.Ordinal)) continue;

                FixedLiveEventEntry entry = context.FixedEventOf(outcome);
                // Đợt không đọc được startUtc không làm mốc: không biết nó mở lúc nào thì không đo được khoảng tới nó.
                if (!entry.TryGetStartUtc(out DateTime startUtc)) continue;
                bool hasEnd = entry.TryGetEndUtc(out DateTime endUtc);
                var timedEvent = new TimedEvent(entry.EventId, startUtc, endUtc);
                readableStarts.Add(timedEvent);
                if (outcome.IsKept && hasEnd) keptEvents.Add(timedEvent);
            }
        }

        private static TimedEvent FirstStartingAtOrAfter(List<TimedEvent> events, DateTime boundaryUtc)
        {
            TimedEvent earliest = null;
            for (int index = 0; index < events.Count; index++)
            {
                TimedEvent candidate = events[index];
                if (candidate.StartUtc < boundaryUtc) continue;
                // So "<" (không "<="): hoà giờ thì giữ đợt gặp trước theo thứ tự xuất.
                if (earliest == null || candidate.StartUtc < earliest.StartUtc) earliest = candidate;
            }
            return earliest;
        }

        private static int CompareByStart(TimedEvent left, TimedEvent right)
        {
            int byStart = left.StartUtc.CompareTo(right.StartUtc);
            return byStart != 0 ? byStart : left.EndUtc.CompareTo(right.EndUtc);
        }

        private static DateTime SubtractSaturating(DateTime value, TimeSpan amount)
        {
            return value.Ticks - DateTime.MinValue.Ticks > amount.Ticks
                ? DateTime.SpecifyKind(value - amount, DateTimeKind.Utc)
                : DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        }

        private sealed class TimedEvent
        {
            public TimedEvent(string eventId, DateTime startUtc, DateTime endUtc)
            {
                EventId = eventId;
                StartUtc = startUtc;
                EndUtc = endUtc;
            }

            public string EventId { get; }
            public DateTime StartUtc { get; }
            public DateTime EndUtc { get; }
        }
    }
}
