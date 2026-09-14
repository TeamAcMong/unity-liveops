using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật 8 <c>unknown-event-type</c>: một phát hiện mỗi LOẠI có đợt/luật mà tài liệu không khai định nghĩa — lịch vẫn giữ mục,
    /// nhưng <c>LiveOpsSystem</c> không đăng ký loại đó nên game bỏ. (V-17) Hai nguồn tách bạch: loại lạ trong NHÁP là Bị bỏ
    /// (chặn Copy — JSON sắp đăng sẽ bị bỏ); loại lạ CHỈ có trong JSON đang chạy đã dán là Nên xem, không đếm vào nháp, không
    /// chặn Copy (nháp sạch sắp thay chính bản đang chạy đó — khoá Copy là chặn đường sửa).
    /// </summary>
    internal sealed class UnknownEventTypeRule : ILiveEventCalendarRule
    {
        public string RuleId => LiveEventCalendarRuleIds.UnknownEventType;
        public LiveEventCalendarConsequence Consequence => LiveEventCalendarConsequence.Dropped;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            LiveEventCalendarDocument document = context.Document;
            List<TypeUsage> draftUsages = CollectUndeclaredUsages(document, document, context);

            var findings = new List<LiveEventCalendarFinding>();
            for (int index = 0; index < draftUsages.Count; index++)
            {
                findings.Add(BuildFinding(draftUsages[index], LiveEventCalendarDetailCodes.UnknownTypeInDraft, Consequence, false));
            }

            if (context.RemoteSnapshot != null)
            {
                // Loại đã được nháp nhắc tới (dù khai hay chưa) không sinh thêm phát hiện "remote": loại có ở cả hai → một phát hiện
                // "draft"; loại nháp đã khai thì bản remote dùng nó cũng không bị bỏ khi game build với asset này.
                HashSet<string> typesUsedInDraft = CollectTypesUsed(document);
                List<TypeUsage> remoteUsages = CollectUndeclaredUsages(context.RemoteSnapshot, document, context);
                for (int index = 0; index < remoteUsages.Count; index++)
                {
                    TypeUsage usage = remoteUsages[index];
                    if (typesUsedInDraft.Contains(usage.EventType)) continue;
                    findings.Add(BuildFinding(usage, LiveEventCalendarDetailCodes.UnknownTypeInRemote, LiveEventCalendarConsequence.ShouldReview, true));
                }
            }

            return findings.Count > 0 ? LiveEventCalendarRuleResult.Found(RuleId, findings) : LiveEventCalendarRuleResult.Passed(RuleId);
        }

        private LiveEventCalendarFinding BuildFinding(TypeUsage usage, string detailCode, LiveEventCalendarConsequence consequence,
            bool isAboutRemoteSnapshot)
        {
            return new LiveEventCalendarFindingBuilder(RuleId, detailCode, consequence, LiveEventCalendarTargetKind.EventType, usage.EventType)
                .WithTargetEntryKey(usage.EventType)
                .WithRelatedId(usage.FirstItemId)
                // Số mục (đợt + luật) dùng loại này — giá trị thô cho câu "có 2 đợt trong JSON đã dán nhưng chưa có loại".
                .WithTexts(usage.ItemCount.ToString(CultureInfo.InvariantCulture), string.Empty)
                .WithRange(usage.EarliestStartUtc, usage.LatestEndUtc)
                .WithAnchor(usage.EarliestStartUtc)
                .WithRemoteSnapshotSubject(isAboutRemoteSnapshot)
                .Build();
        }

        /// <summary>
        /// Loại dùng trong <paramref name="source"/> mà <paramref name="declarations"/> không khai, theo thứ tự gặp đầu tiên (luật
        /// lặp rồi đợt). Loại sai quy tắc (rỗng, có '#') bỏ qua: mục đó đã bị bỏ và có phát hiện của luật 3/6 — thêm "loại lạ ''"
        /// chỉ là nhiễu.
        /// </summary>
        private static List<TypeUsage> CollectUndeclaredUsages(LiveEventCalendarDocument source, LiveEventCalendarDocument declarations,
            LiveEventCalendarCheckContext context)
        {
            var usages = new List<TypeUsage>();
            var usageByType = new Dictionary<string, TypeUsage>(StringComparer.Ordinal);

            IReadOnlyList<RecurringLiveEventRule> rules = source.RecurringRules;
            for (int index = 0; index < rules.Count; index++)
            {
                RecurringLiveEventRule rule = rules[index];
                TypeUsage usage = UsageFor(rule.EventType, declarations, context, usages, usageByType);
                if (usage == null) continue;
                usage.ItemCount++;
                if (usage.FirstItemId.Length == 0) usage.FirstItemId = rule.EffectiveIdPrefix;
                if (rule.TryGetAnchorUtc(out DateTime anchorUtc)) usage.IncludeStart(anchorUtc);
            }

            IReadOnlyList<FixedLiveEventEntry> fixedEvents = source.FixedEvents;
            for (int index = 0; index < fixedEvents.Count; index++)
            {
                FixedLiveEventEntry entry = fixedEvents[index];
                TypeUsage usage = UsageFor(entry.EventType, declarations, context, usages, usageByType);
                if (usage == null) continue;
                usage.ItemCount++;
                if (usage.FirstItemId.Length == 0) usage.FirstItemId = entry.EventId;
                if (entry.TryGetStartUtc(out DateTime startUtc)) usage.IncludeStart(startUtc);
                if (entry.TryGetEndUtc(out DateTime endUtc)) usage.IncludeEnd(endUtc);
            }
            return usages;
        }

        private static TypeUsage UsageFor(string eventType, LiveEventCalendarDocument declarations, LiveEventCalendarCheckContext context,
            List<TypeUsage> usages, Dictionary<string, TypeUsage> usageByType)
        {
            if (InvalidIdentifierRule.TryDescribeDefect(eventType, out _)) return null;
            if (!context.IncludesEventType(eventType)) return null;
            if (declarations.TryGetEventType(eventType, out _)) return null;

            if (!usageByType.TryGetValue(eventType, out TypeUsage usage))
            {
                usage = new TypeUsage(eventType);
                usageByType.Add(eventType, usage);
                usages.Add(usage);
            }
            return usage;
        }

        private static HashSet<string> CollectTypesUsed(LiveEventCalendarDocument document)
        {
            var types = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < document.RecurringRules.Count; index++) types.Add(document.RecurringRules[index].EventType);
            for (int index = 0; index < document.FixedEvents.Count; index++) types.Add(document.FixedEvents[index].EventType);
            return types;
        }

        private sealed class TypeUsage
        {
            public TypeUsage(string eventType)
            {
                EventType = eventType;
            }

            public string EventType { get; }
            public int ItemCount { get; set; }
            public string FirstItemId { get; set; } = string.Empty;
            public DateTime? EarliestStartUtc { get; private set; }
            public DateTime? LatestEndUtc { get; private set; }

            public void IncludeStart(DateTime startUtc)
            {
                if (!EarliestStartUtc.HasValue || startUtc < EarliestStartUtc.Value) EarliestStartUtc = startUtc;
            }

            public void IncludeEnd(DateTime endUtc)
            {
                if (!LatestEndUtc.HasValue || endUtc > LatestEndUtc.Value) LatestEndUtc = endUtc;
            }
        }
    }
}
