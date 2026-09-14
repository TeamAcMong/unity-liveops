using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật 2 <c>end-before-start</c>: đợt đọc được cả hai giờ, id/loại hợp lệ, nhưng kết thúc không sau bắt đầu —
    /// <see cref="LiveEventInstance"/> từ chối nên game bỏ đợt. Hai đề xuất vì hub không đoán được người dùng gõ nhầm phía nào.
    /// </summary>
    internal sealed class EndBeforeStartRule : ILiveEventCalendarRule
    {
        public const string KeepStartSetDurationRepairId = "keep-start-set-duration-24h";
        public const string SwapStartEndRepairId = "swap-start-end";

        /// <summary>Thời lượng đề xuất khi giữ bắt đầu — một ngày là đợt ngắn nhất thường gặp, người dùng chỉnh tiếp ở inspector.</summary>
        private static readonly TimeSpan SuggestedDuration = TimeSpan.FromHours(24);

        public string RuleId => LiveEventCalendarRuleIds.EndBeforeStart;
        public LiveEventCalendarConsequence Consequence => LiveEventCalendarConsequence.Dropped;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            List<LiveEventCalendarEntryOutcome> outcomes = context.DroppedFixedOutcomes(LiveEventCalendarDropReason.EndNotAfterStart);
            if (outcomes.Count == 0) return LiveEventCalendarRuleResult.Passed(RuleId);

            var findings = new List<LiveEventCalendarFinding>(outcomes.Count);
            for (int index = 0; index < outcomes.Count; index++)
            {
                FixedLiveEventEntry entry = context.FixedEventOf(outcomes[index]);
                LiveEventCalendarFinding finding = BuildFinding(entry);
                if (finding != null) findings.Add(finding);
            }
            return findings.Count > 0 ? LiveEventCalendarRuleResult.Found(RuleId, findings) : LiveEventCalendarRuleResult.Passed(RuleId);
        }

        private LiveEventCalendarFinding BuildFinding(FixedLiveEventEntry entry)
        {
            // Lý do chính EndNotAfterStart đảm bảo cả hai giờ đọc được; không đọc được thì là outcome không khớp tài liệu (lỗi lập trình).
            if (!entry.TryGetStartUtc(out DateTime startUtc) || !entry.TryGetEndUtc(out DateTime endUtc))
            {
                throw new InvalidOperationException("Outcome EndNotAfterStart của '" + entry.EventId + "' nhưng giờ không đọc được.");
            }

            var repairs = new List<LiveEventCalendarRepair>(2);
            string beforeText = entry.StartUtcText + LiveEventCalendarFindingBuilder.ValueSeparator + entry.EndUtcText;

            if (TryAdd(startUtc, SuggestedDuration, out DateTime suggestedEnd))
            {
                repairs.Add(new LiveEventCalendarRepair(KeepStartSetDurationRepairId, LiveEventCalendarRepairKind.Proposal,
                    new ReplaceFixedEventEdit(entry.WithTimes(entry.StartUtcText, LiveEventUtcText.Format(suggestedEnd))),
                    beforeText, entry.StartUtcText + LiveEventCalendarFindingBuilder.ValueSeparator + LiveEventUtcText.Format(suggestedEnd),
                    startUtc, suggestedEnd));
            }
            // Bằng nhau thì đổi chỗ vẫn ra đợt dài 0 — không phải cách sửa.
            if (endUtc < startUtc)
            {
                repairs.Add(new LiveEventCalendarRepair(SwapStartEndRepairId, LiveEventCalendarRepairKind.Proposal,
                    new ReplaceFixedEventEdit(entry.WithTimes(entry.EndUtcText, entry.StartUtcText)),
                    beforeText, entry.EndUtcText + LiveEventCalendarFindingBuilder.ValueSeparator + entry.StartUtcText,
                    endUtc, startUtc));
            }

            var builder = new LiveEventCalendarFindingBuilder(RuleId,
                    endUtc < startUtc ? LiveEventCalendarDetailCodes.EndBeforeStart : LiveEventCalendarDetailCodes.EndEqualsStart,
                    Consequence, LiveEventCalendarTargetKind.FixedEvent, entry.EventId)
                .WithTargetEntryKey(entry.EntryKey)
                .WithTexts(beforeText, entry.StartUtcText)
                .WithRange(startUtc, endUtc)
                .WithAnchor(startUtc);
            builder.WithRepairs(repairs.Count > 0 ? LiveEventCalendarRepairKind.Proposal : LiveEventCalendarRepairKind.None, repairs);
            return builder.Build();
        }

        /// <summary>Sát mép <see cref="DateTime.MaxValue"/> thì không có "bắt đầu + 24 giờ" — bỏ đề xuất đó thay vì ném.</summary>
        private static bool TryAdd(DateTime value, TimeSpan duration, out DateTime result)
        {
            result = default;
            if (value.Ticks > DateTime.MaxValue.Ticks - duration.Ticks) return false;
            result = DateTime.SpecifyKind(value.Add(duration), DateTimeKind.Utc);
            return true;
        }
    }
}
