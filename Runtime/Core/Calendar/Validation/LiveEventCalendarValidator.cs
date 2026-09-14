using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Chạy các luật Kiểm lịch trên một <see cref="LiveEventCalendarCheckContext"/>. Validator mặc định có đủ mười hai luật
    /// theo <see cref="LiveEventCalendarRuleIds.All"/>; game/test thay bộ luật bằng constructor.
    /// </summary>
    public sealed class LiveEventCalendarValidator
    {
        public const int RuleCount = 12;

        /// <summary>
        /// Luật có đích là đợt/luật của MỘT loại (1–7, 10) — "kiểm nhanh làn này" chỉ chạy các luật này. Luật về định nghĩa loại,
        /// khoảng trống, bản so và bản remote không trả lời được câu "thanh vừa kéo có bị bỏ không".
        /// </summary>
        private static readonly string[] LaneRuleIds =
        {
            LiveEventCalendarRuleIds.UtcTimeFormat,
            LiveEventCalendarRuleIds.EndBeforeStart,
            LiveEventCalendarRuleIds.InvalidIdentifier,
            LiveEventCalendarRuleIds.DuplicateEventId,
            LiveEventCalendarRuleIds.OverlapSameType,
            LiveEventCalendarRuleIds.RecurringRuleInvalid,
            LiveEventCalendarRuleIds.ShadowedByRecurring,
            LiveEventCalendarRuleIds.ConfigKeyMissing,
        };

        private static readonly LiveEventCalendarValidator DefaultValidator = new LiveEventCalendarValidator(new ILiveEventCalendarRule[]
        {
            new UtcTimeFormatRule(),
            new EndBeforeStartRule(),
            new InvalidIdentifierRule(),
            new DuplicateEventIdRule(),
            new OverlapSameTypeRule(),
            new RecurringRuleInvalidRule(),
            new ShadowedByRecurringRule(),
            new UnknownEventTypeRule(),
            // INTERIM(G-VALIDATOR-B): luật 9–12 giữ chỗ NotMeasured("rule-not-built") tới khi có luật thật.
            new NotYetImplementedRule(LiveEventCalendarRuleIds.RunningEventIdChanged, LiveEventCalendarConsequence.ProgressLost),
            new NotYetImplementedRule(LiveEventCalendarRuleIds.ConfigKeyMissing, LiveEventCalendarConsequence.ShouldReview),
            new NotYetImplementedRule(LiveEventCalendarRuleIds.LongGapBetweenEvents, LiveEventCalendarConsequence.ShouldReview),
            new NotYetImplementedRule(LiveEventCalendarRuleIds.RemoteSnapshotDrift, LiveEventCalendarConsequence.ShouldReview),
        });

        private readonly ILiveEventCalendarRule[] _rules;

        /// <summary>Ném khi có luật null hoặc hai luật cùng id — lỗi lập trình: báo cáo khoá phát hiện theo id luật.</summary>
        public LiveEventCalendarValidator(IEnumerable<ILiveEventCalendarRule> rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));

            var copied = new List<ILiveEventCalendarRule>();
            var seenRuleIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ILiveEventCalendarRule rule in rules)
            {
                if (rule == null) throw new ArgumentException("Danh sách luật không được chứa null.", nameof(rules));
                string ruleId = rule.RuleId;
                if (string.IsNullOrEmpty(ruleId)) throw new ArgumentException("Luật phải có RuleId.", nameof(rules));
                if (!seenRuleIds.Add(ruleId)) throw new ArgumentException("Hai luật cùng id '" + ruleId + "'.", nameof(rules));
                copied.Add(rule);
            }
            _rules = copied.ToArray();
        }

        public static LiveEventCalendarValidator Default => DefaultValidator;

        public IReadOnlyList<ILiveEventCalendarRule> Rules => (ILiveEventCalendarRule[])_rules.Clone();

        public LiveEventCalendarCheckRun BeginCheck(LiveEventCalendarCheckContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return new LiveEventCalendarCheckRun(_rules, context);
        }

        public LiveEventCalendarCheckReport Check(LiveEventCalendarCheckContext context)
        {
            return RunToCompletion(BeginCheck(context));
        }

        /// <summary>
        /// "Kiểm nhanh làn này" trên tài liệu xem trước: chỉ luật có đích thuộc một loại, ngữ cảnh lọc theo loại đó. Trả báo cáo
        /// riêng — không bao giờ ghi vào trạng thái Kiểm lịch (kết quả kiểm một làn không được làm "mới" kết quả cả lịch).
        /// </summary>
        public LiveEventCalendarCheckReport CheckLane(LiveEventCalendarCheckContext context, string eventType)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrEmpty(eventType)) throw new ArgumentException("eventType không được rỗng.", nameof(eventType));

            LiveEventCalendarCheckContext laneContext = context.ToBuilder().WithOnlyEventType(eventType).Build();
            var laneRules = new List<ILiveEventCalendarRule>(LaneRuleIds.Length);
            for (int index = 0; index < _rules.Length; index++)
            {
                if (IsLaneRule(_rules[index].RuleId)) laneRules.Add(_rules[index]);
            }
            return RunToCompletion(new LiveEventCalendarCheckRun(laneRules.ToArray(), laneContext));
        }

        private static bool IsLaneRule(string ruleId)
        {
            for (int index = 0; index < LaneRuleIds.Length; index++)
            {
                if (string.Equals(LaneRuleIds[index], ruleId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static LiveEventCalendarCheckReport RunToCompletion(LiveEventCalendarCheckRun run)
        {
            while (run.Step())
            {
            }
            return run.Report;
        }
    }
}
