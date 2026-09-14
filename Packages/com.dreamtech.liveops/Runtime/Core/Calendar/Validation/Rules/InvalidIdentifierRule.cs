using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật 3 <c>invalid-identifier</c>: đợt có id hoặc loại rỗng / chứa '#' / xuống dòng (<c>LiveEventInstance.ValidateIdentifier</c>)
    /// — game bỏ đợt vì '#' là dấu ngăn trong grant id. Không có sửa tự động: id mới là quyết định của người dùng (ô Id báo lỗi tại chỗ).
    /// </summary>
    internal sealed class InvalidIdentifierRule : ILiveEventCalendarRule
    {
        /// <summary>Tên field JSON sai — ghi vào <c>ExpectedText</c> để Editor biết ô nào báo lỗi mà không phải so lại chuỗi.</summary>
        internal const string EventIdFieldName = "id";

        internal const string EventTypeFieldName = "type";

        public string RuleId => LiveEventCalendarRuleIds.InvalidIdentifier;
        public LiveEventCalendarConsequence Consequence => LiveEventCalendarConsequence.Dropped;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            List<LiveEventCalendarEntryOutcome> outcomes = context.DroppedFixedOutcomes(LiveEventCalendarDropReason.InvalidIdentifier);
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
            // Id kiểm trước loại — cùng thứ tự với ctor LiveEventInstance, nên phát hiện nói đúng lỗi mà câu Problems của game nói.
            bool eventIdInvalid = TryDescribeDefect(entry.EventId, out string detailCode);
            if (!eventIdInvalid) TryDescribeDefect(entry.EventType, out detailCode);

            entry.TryGetStartUtc(out DateTime startUtc);
            entry.TryGetEndUtc(out DateTime endUtc);

            return new LiveEventCalendarFindingBuilder(RuleId, detailCode, Consequence, LiveEventCalendarTargetKind.FixedEvent, entry.EventId)
                .WithTargetEntryKey(entry.EntryKey)
                .WithTexts(eventIdInvalid ? entry.EventId : entry.EventType, eventIdInvalid ? EventIdFieldName : EventTypeFieldName)
                .WithRange(startUtc, endUtc)
                .WithAnchor(startUtc)
                .Build();
        }

        /// <summary>true khi giá trị sai quy tắc, kèm mã biến thể. Rỗng trước, rồi '#', rồi xuống dòng.</summary>
        internal static bool TryDescribeDefect(string value, out string detailCode)
        {
            detailCode = LiveEventCalendarDetailCodes.EmptyIdentifier;
            if (string.IsNullOrEmpty(value)) return true;
            if (ContainsCharacter(value, LiveEventInstance.ReservedSeparator))
            {
                detailCode = LiveEventCalendarDetailCodes.HashInIdentifier;
                return true;
            }
            if (ContainsCharacter(value, '\n') || ContainsCharacter(value, '\r'))
            {
                detailCode = LiveEventCalendarDetailCodes.NewlineInIdentifier;
                return true;
            }
            return false;
        }

        private static bool ContainsCharacter(string value, char character)
        {
            for (int index = 0; index < value.Length; index++)
            {
                if (value[index] == character) return true;
            }
            return false;
        }
    }
}
