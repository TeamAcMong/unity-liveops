using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật 1 <c>utc-time-format</c>: đợt cố định bị bỏ vì giờ bắt đầu/kết thúc không đọc được. Đọc lý do CHÍNH của outcome
    /// (V-7) chứ không đọc chuỗi thô — đợt vừa hỏng giờ vừa sai id chỉ sinh MỘT phát hiện ở đây, lỗi id nêu thêm qua
    /// <see cref="LiveEventCalendarEntryOutcome.AdditionalItemReasons"/>.
    /// </summary>
    internal sealed class UtcTimeFormatRule : ILiveEventCalendarRule
    {
        /// <summary>Dạng cần khi chuỗi không tự chuẩn hoá được — giá trị thô cho ô "cần …", không phải câu.</summary>
        internal const string CanonicalPatternText = "yyyy-MM-ddTHH:mm:ssZ";

        public const string NormalizeRepairId = "normalize-utc";

        public string RuleId => LiveEventCalendarRuleIds.UtcTimeFormat;
        public LiveEventCalendarConsequence Consequence => LiveEventCalendarConsequence.Dropped;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            List<LiveEventCalendarEntryOutcome> outcomes = context.DroppedOutcomes(LiveEventCalendarEntryKind.FixedEvent,
                LiveEventCalendarDropReason.UnreadableStartUtc, LiveEventCalendarDropReason.UnreadableEndUtc);
            if (outcomes.Count == 0) return LiveEventCalendarRuleResult.Passed(RuleId);

            var findings = new List<LiveEventCalendarFinding>(outcomes.Count);
            for (int index = 0; index < outcomes.Count; index++)
            {
                findings.Add(BuildFinding(context.FixedEventOf(outcomes[index])));
            }
            return LiveEventCalendarRuleResult.Found(RuleId, findings);
        }

        private LiveEventCalendarFinding BuildFinding(FixedLiveEventEntry entry)
        {
            bool startReadable = entry.TryGetStartUtc(out DateTime startUtc);
            bool endReadable = entry.TryGetEndUtc(out DateTime endUtc);

            // Mỗi phía hỏng: thử chuẩn hoá an toàn (không đổi thời điểm). Phía đọc được giữ nguyên văn.
            string normalizedStart = null;
            string normalizedEnd = null;
            bool startNormalizable = startReadable || LiveEventUtcText.TryNormalize(entry.StartUtcText, out normalizedStart);
            bool endNormalizable = endReadable || LiveEventUtcText.TryNormalize(entry.EndUtcText, out normalizedEnd);

            var foundValues = new List<string>(2);
            var expectedValues = new List<string>(2);
            if (!startReadable)
            {
                foundValues.Add(entry.StartUtcText);
                expectedValues.Add(startNormalizable ? normalizedStart : CanonicalPatternText);
            }
            if (!endReadable)
            {
                foundValues.Add(entry.EndUtcText);
                expectedValues.Add(endNormalizable ? normalizedEnd : CanonicalPatternText);
            }

            DateTime? rangeStart = startReadable ? startUtc : ParseOrNull(normalizedStart);
            DateTime? rangeEnd = endReadable ? endUtc : ParseOrNull(normalizedEnd);

            var builder = new LiveEventCalendarFindingBuilder(RuleId,
                    startReadable ? LiveEventCalendarDetailCodes.EndUnreadable : LiveEventCalendarDetailCodes.StartUnreadable,
                    Consequence, LiveEventCalendarTargetKind.FixedEvent, entry.EventId)
                .WithTargetEntryKey(entry.EntryKey)
                .WithTexts(string.Join(LiveEventCalendarFindingBuilder.ValueSeparator, foundValues),
                    string.Join(LiveEventCalendarFindingBuilder.ValueSeparator, expectedValues))
                .WithRange(rangeStart, rangeEnd)
                .WithAnchor(rangeStart ?? rangeEnd);

            // Sửa an toàn chỉ khi MỌI phía hỏng đều chuẩn hoá được: sửa một nửa vẫn để đợt bị bỏ, nút "Sửa" sẽ nói dối.
            if (startNormalizable && endNormalizable)
            {
                string repairedStart = startReadable ? entry.StartUtcText : normalizedStart;
                string repairedEnd = endReadable ? entry.EndUtcText : normalizedEnd;
                var repair = new LiveEventCalendarRepair(NormalizeRepairId, LiveEventCalendarRepairKind.SafeRepair,
                    new ReplaceFixedEventEdit(entry.WithTimes(repairedStart, repairedEnd)),
                    string.Join(LiveEventCalendarFindingBuilder.ValueSeparator, foundValues),
                    string.Join(LiveEventCalendarFindingBuilder.ValueSeparator, expectedValues),
                    ParseOrNull(repairedStart), ParseOrNull(repairedEnd));
                builder.WithRepairs(LiveEventCalendarRepairKind.SafeRepair, new[] { repair });
            }
            return builder.Build();
        }

        private static DateTime? ParseOrNull(string utcText)
        {
            if (utcText == null) return null;
            return LiveEventUtcText.TryParse(utcText, out DateTime utc) ? utc : (DateTime?)null;
        }
    }
}
