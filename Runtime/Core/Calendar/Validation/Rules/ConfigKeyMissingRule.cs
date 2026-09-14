using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật 10 <c>config-key-missing</c> (Q-4 phương án b): đợt/luật không tự khai configKey — JSON xuất sẽ ghi mặc định của loại —
    /// VÀ (không có bản so, hoặc bản so không có mục cùng danh tính, hoặc configKey hiệu lực khác bản so). Vì sao không bắn cho mọi
    /// mục kế thừa: mục kế thừa đúng khoá game đang dùng là ý đồ bình thường; chỉ khi khoá tới người chơi sẽ ĐỔI (hoặc chưa ai
    /// kiểm) thì kế thừa âm thầm mới đáng xem — khớp mẫu thiết kế ra đúng một phát hiện (hunt-0914: bản so dùng hunt_v1).
    /// <para>Bỏ qua mục game không nhận (đã có phát hiện Bị bỏ riêng) và đợt đã khép (khoá thưởng không còn tới ai).</para>
    /// <para>Giá trị thô: Found = configKey hiệu lực sẽ xuất ("hunt_default", "" khi loại không có mặc định); Expected = configKey
    /// của bản so ("hunt_v1"; "" khi không có bản so / mục mới). <c>RelatedId</c> = loại cho khoá kế thừa.</para>
    /// </summary>
    internal sealed class ConfigKeyMissingRule : ILiveEventCalendarRule
    {
        public string RuleId => LiveEventCalendarRuleIds.ConfigKeyMissing;
        public LiveEventCalendarConsequence Consequence => LiveEventCalendarConsequence.ShouldReview;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            LiveEventCalendarDocument document = context.Document;
            LiveEventCalendarDocument baseline = context.PublishedBaseline;
            var findings = new List<LiveEventCalendarFinding>();

            IReadOnlyList<LiveEventCalendarEntryOutcome> entries = context.Compilation.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = entries[index];
                if (!outcome.IsKept || !context.IncludesEventType(outcome.EventType)) continue;

                LiveEventCalendarFinding finding = outcome.Kind == LiveEventCalendarEntryKind.RecurringRule
                    ? EvaluateRecurringRule(document, baseline, context.RecurringRuleOf(outcome))
                    : EvaluateFixedEvent(document, baseline, context.FixedEventOf(outcome), context.NowUtc);
                if (finding != null) findings.Add(finding);
            }

            return findings.Count > 0 ? LiveEventCalendarRuleResult.Found(RuleId, findings) : LiveEventCalendarRuleResult.Passed(RuleId);
        }

        private LiveEventCalendarFinding EvaluateFixedEvent(LiveEventCalendarDocument document, LiveEventCalendarDocument baseline,
            FixedLiveEventEntry entry, DateTime nowUtc)
        {
            if (entry.HasOwnConfigKey) return null;
            entry.TryGetStartUtc(out DateTime startUtc);
            entry.TryGetEndUtc(out DateTime endUtc);
            // Mục được giữ luôn đọc được hai giờ; đợt đã khép thì đổi khoá thưởng không còn tới người chơi nào.
            if (endUtc <= nowUtc) return null;

            string effectiveConfigKey = document.EffectiveConfigKeyOf(entry);
            FixedLiveEventEntry baselineEntry = baseline != null ? FindBaselineEntry(baseline, entry) : null;
            string baselineConfigKey = baselineEntry != null ? baseline.EffectiveConfigKeyOf(baselineEntry) : string.Empty;
            string detailCode = DetailCodeFor(effectiveConfigKey, baselineEntry != null, baselineConfigKey);
            if (detailCode == null) return null;

            return new LiveEventCalendarFindingBuilder(RuleId, detailCode, Consequence, LiveEventCalendarTargetKind.FixedEvent, entry.EventId)
                .WithTargetEntryKey(entry.EntryKey)
                .WithRelatedId(entry.EventType)
                .WithTexts(effectiveConfigKey, baselineConfigKey)
                .WithRange(startUtc, endUtc)
                .WithAnchor(startUtc)
                .Build();
        }

        private LiveEventCalendarFinding EvaluateRecurringRule(LiveEventCalendarDocument document, LiveEventCalendarDocument baseline,
            RecurringLiveEventRule rule)
        {
            if (rule.HasOwnConfigKey) return null;

            string effectiveConfigKey = document.EffectiveConfigKeyOf(rule);
            RecurringLiveEventRule baselineRule = null;
            bool hasBaselineRule = baseline != null && baseline.TryGetRecurringRule(rule.EventType, out baselineRule);
            string baselineConfigKey = hasBaselineRule ? baseline.EffectiveConfigKeyOf(baselineRule) : string.Empty;
            string detailCode = DetailCodeFor(effectiveConfigKey, hasBaselineRule, baselineConfigKey);
            if (detailCode == null) return null;

            DateTime? anchorUtc = rule.TryGetAnchorUtc(out DateTime parsedAnchorUtc) ? parsedAnchorUtc : (DateTime?)null;
            return new LiveEventCalendarFindingBuilder(RuleId, detailCode, Consequence, LiveEventCalendarTargetKind.RecurringRule, rule.EventType)
                .WithTargetEntryKey(rule.EventType)
                .WithRelatedId(rule.EventType)
                .WithTexts(effectiveConfigKey, baselineConfigKey)
                .WithAnchor(anchorUtc)
                .Build();
        }

        /// <summary>
        /// <c>null</c> = không phát hiện (bản so có mục và cùng khoá hiệu lực). Khoá rỗng xếp trước: game nhận configKey "" thì
        /// không tra được cấu hình nào — nặng hơn chuyện khoá mới hay khoá đổi.
        /// </summary>
        private static string DetailCodeFor(string effectiveConfigKey, bool hasBaselineItem, string baselineConfigKey)
        {
            if (hasBaselineItem && string.Equals(effectiveConfigKey, baselineConfigKey, StringComparison.Ordinal)) return null;
            if (effectiveConfigKey.Length == 0) return LiveEventCalendarDetailCodes.ConfigKeyEmpty;
            return hasBaselineItem ? LiveEventCalendarDetailCodes.ConfigKeyInheritedDiffers : LiveEventCalendarDetailCodes.ConfigKeyInheritedNew;
        }

        /// <summary>Danh tính như diff với bản đã đăng: cùng id VÀ cùng loại (đổi loại là mục khác với người chơi).</summary>
        private static FixedLiveEventEntry FindBaselineEntry(LiveEventCalendarDocument baseline, FixedLiveEventEntry entry)
        {
            IReadOnlyList<FixedLiveEventEntry> baselineEvents = baseline.FixedEvents;
            for (int index = 0; index < baselineEvents.Count; index++)
            {
                FixedLiveEventEntry candidate = baselineEvents[index];
                if (string.Equals(candidate.EventId, entry.EventId, StringComparison.Ordinal) &&
                    string.Equals(candidate.EventType, entry.EventType, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }
            return null;
        }
    }
}
