using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật 6 <c>recurring-rule-invalid</c>: luật lặp không dựng được <see cref="RecurringLiveEventCalendar"/> (neo hỏng, chu kỳ/thời
    /// gian chạy sai, tiền tố hoặc loại sai quy tắc) hoặc là luật thứ hai của cùng loại — game bỏ cả luật, mất mọi lần lặp.
    /// </summary>
    internal sealed class RecurringRuleInvalidRule : ILiveEventCalendarRule
    {
        public const string SetActiveToPeriodRepairId = "set-active-to-period";

        /// <summary>Trần chu kỳ của bộ biên dịch (cả khoảng DateTime) — chu kỳ quá trần bị bỏ, báo cùng mã "period".</summary>
        private static readonly long MaximumPeriodHours = DateTime.MaxValue.Ticks / TimeSpan.TicksPerHour;

        public string RuleId => LiveEventCalendarRuleIds.RecurringRuleInvalid;
        public LiveEventCalendarConsequence Consequence => LiveEventCalendarConsequence.Dropped;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            List<LiveEventCalendarEntryOutcome> outcomes = context.DroppedOutcomes(LiveEventCalendarEntryKind.RecurringRule,
                LiveEventCalendarDropReason.InvalidRecurringRule, LiveEventCalendarDropReason.DuplicateRecurringType);
            if (outcomes.Count == 0) return LiveEventCalendarRuleResult.Passed(RuleId);

            var findings = new List<LiveEventCalendarFinding>(outcomes.Count);
            for (int index = 0; index < outcomes.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = outcomes[index];
                findings.Add(BuildFinding(context.RecurringRuleOf(outcome), outcome));
            }
            return LiveEventCalendarRuleResult.Found(RuleId, findings);
        }

        private LiveEventCalendarFinding BuildFinding(RecurringLiveEventRule rule, LiveEventCalendarEntryOutcome outcome)
        {
            bool anchorReadable = rule.TryGetAnchorUtc(out DateTime anchorUtc);
            string detailCode;
            string foundText;
            string expectedText = string.Empty;
            LiveEventCalendarRepair repair = null;
            LiveEventCalendarRepairKind repairKind = LiveEventCalendarRepairKind.None;

            if (outcome.DropReason == LiveEventCalendarDropReason.DuplicateRecurringType)
            {
                detailCode = LiveEventCalendarDetailCodes.DuplicateRuleForType;
                foundText = rule.EventType;
            }
            // Cùng thứ tự kiểm với bộ biên dịch: neo → chu kỳ → thời gian chạy → tiền tố → loại. Mã nói đúng lỗi làm game bỏ luật.
            else if (!anchorReadable)
            {
                detailCode = LiveEventCalendarDetailCodes.AnchorUnreadable;
                foundText = rule.AnchorUtcText;
                if (LiveEventUtcText.TryNormalize(rule.AnchorUtcText, out string normalizedAnchor))
                {
                    expectedText = normalizedAnchor;
                    LiveEventUtcText.TryParse(normalizedAnchor, out anchorUtc);
                    repair = new LiveEventCalendarRepair(UtcTimeFormatRule.NormalizeRepairId, LiveEventCalendarRepairKind.SafeRepair,
                        new SetRecurringRuleEdit(rule.WithAnchor(normalizedAnchor)), rule.AnchorUtcText, normalizedAnchor, anchorUtc, null);
                    repairKind = LiveEventCalendarRepairKind.SafeRepair;
                }
                else
                {
                    expectedText = UtcTimeFormatRule.CanonicalPatternText;
                }
            }
            else if (rule.PeriodHours <= 0 || rule.PeriodHours > MaximumPeriodHours)
            {
                detailCode = LiveEventCalendarDetailCodes.PeriodNotPositive;
                foundText = FormatHours(rule.PeriodHours);
            }
            else if (rule.ActiveHours <= 0)
            {
                detailCode = LiveEventCalendarDetailCodes.ActiveNotPositive;
                foundText = FormatHours(rule.ActiveHours);
            }
            else if (rule.ActiveHours > rule.PeriodHours)
            {
                detailCode = LiveEventCalendarDetailCodes.ActiveLongerThanPeriod;
                foundText = FormatHours(rule.ActiveHours);
                expectedText = FormatHours(rule.PeriodHours);
                repair = new LiveEventCalendarRepair(SetActiveToPeriodRepairId, LiveEventCalendarRepairKind.Proposal,
                    new SetRecurringRuleEdit(rule.WithActiveHours(rule.PeriodHours)), FormatHours(rule.ActiveHours),
                    FormatHours(rule.PeriodHours), null, null);
                repairKind = LiveEventCalendarRepairKind.Proposal;
            }
            else if (InvalidIdentifierRule.TryDescribeDefect(rule.EffectiveIdPrefix, out _) && rule.EffectiveIdPrefix.Length > 0)
            {
                detailCode = LiveEventCalendarDetailCodes.PrefixInvalid;
                foundText = rule.IdPrefix;
            }
            else
            {
                detailCode = LiveEventCalendarDetailCodes.TypeInvalid;
                foundText = rule.EventType;
            }

            // Neo hỏng mà chuẩn hoá được vẫn có mốc để căn khung timeline (anchorUtc đã đọc lại từ bản chuẩn hoá ở trên).
            bool anchorKnown = anchorReadable || (detailCode == LiveEventCalendarDetailCodes.AnchorUnreadable && repair != null);
            var builder = new LiveEventCalendarFindingBuilder(RuleId, detailCode, Consequence, LiveEventCalendarTargetKind.RecurringRule, rule.EventType)
                .WithTargetEntryKey(rule.EventType)
                .WithRelatedId(outcome.RelatedEventId)
                .WithTexts(foundText, expectedText)
                .WithAnchor(anchorKnown ? anchorUtc : (DateTime?)null);
            if (repair != null) builder.WithRepairs(repairKind, new[] { repair });
            return builder.Build();
        }

        private static string FormatHours(int hours)
        {
            return hours.ToString(CultureInfo.InvariantCulture);
        }
    }
}
