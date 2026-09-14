using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Mã biến thể trong từng luật (PD-32) — danh sách ĐÓNG của cả mười hai luật, khai đủ ngay ở gói hạ tầng. Vì sao khai
    /// trước cả luật chưa viết (9–12): gói luật 9–12 và gói câu chữ phía Editor làm song song; cả hai dựa cùng một danh
    /// sách thì câu tiếng Việt chọn theo cặp (RuleId, DetailCode) mà không phải parse chuỗi, và test Editor duyệt được mọi
    /// cặp đều có câu. <see cref="LiveEventCalendarFindingBuilder"/> từ chối mã không có ở đây.
    /// </summary>
    public static class LiveEventCalendarDetailCodes
    {
        // utc-time-format (AnchorUnreadable dùng chung với recurring-rule-invalid)
        public const string StartUnreadable = "start";
        public const string EndUnreadable = "end";
        public const string AnchorUnreadable = "anchor";

        // end-before-start
        public const string EndBeforeStart = "end-before-start";
        public const string EndEqualsStart = "end-equals-start";

        // invalid-identifier
        public const string EmptyIdentifier = "empty";
        public const string HashInIdentifier = "hash";
        public const string NewlineInIdentifier = "newline";

        // duplicate-event-id
        public const string DuplicateFixedId = "fixed";

        // overlap-same-type
        public const string OverlapKeptEarlier = "overlap";

        // recurring-rule-invalid (+ AnchorUnreadable)
        public const string PeriodNotPositive = "period";
        public const string ActiveNotPositive = "active";
        public const string ActiveLongerThanPeriod = "active-longer-than-period";
        public const string PrefixInvalid = "prefix";
        public const string TypeInvalid = "type";
        public const string DuplicateRuleForType = "duplicate-type";

        // shadowed-by-recurring
        public const string ShadowedOverlap = "overlap-occurrence";
        public const string ShadowedIdCollision = "id-collision";

        // unknown-event-type
        public const string UnknownTypeInDraft = "draft";
        public const string UnknownTypeInRemote = "remote";

        // running-event-id-changed
        public const string RunningIdChanged = "prefix-changed";
        public const string RunningRemoved = "removed";
        public const string RunningRetyped = "retyped";
        public const string RunningMovedOutOfWindow = "moved-out";
        public const string RunningEndsNow = "ends-now";

        // config-key-missing
        public const string ConfigKeyInheritedDiffers = "inherited-differs";
        public const string ConfigKeyInheritedNew = "inherited-new";
        public const string ConfigKeyEmpty = "empty-default";

        // long-gap-between-events
        public const string LongGap = "gap";

        // remote-snapshot-drift
        public const string RemoteDiffers = "differs";
        public const string RemoteWithoutStamp = "no-stamp";

        private static readonly Dictionary<string, string[]> CodesByRuleId = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { LiveEventCalendarRuleIds.UtcTimeFormat, new[] { StartUnreadable, EndUnreadable, AnchorUnreadable } },
            { LiveEventCalendarRuleIds.EndBeforeStart, new[] { EndBeforeStart, EndEqualsStart } },
            { LiveEventCalendarRuleIds.InvalidIdentifier, new[] { EmptyIdentifier, HashInIdentifier, NewlineInIdentifier } },
            { LiveEventCalendarRuleIds.DuplicateEventId, new[] { DuplicateFixedId } },
            { LiveEventCalendarRuleIds.OverlapSameType, new[] { OverlapKeptEarlier } },
            {
                LiveEventCalendarRuleIds.RecurringRuleInvalid,
                new[] { AnchorUnreadable, PeriodNotPositive, ActiveNotPositive, ActiveLongerThanPeriod, PrefixInvalid, TypeInvalid, DuplicateRuleForType }
            },
            { LiveEventCalendarRuleIds.ShadowedByRecurring, new[] { ShadowedOverlap, ShadowedIdCollision } },
            { LiveEventCalendarRuleIds.UnknownEventType, new[] { UnknownTypeInDraft, UnknownTypeInRemote } },
            {
                LiveEventCalendarRuleIds.RunningEventIdChanged,
                new[] { RunningIdChanged, RunningRemoved, RunningRetyped, RunningMovedOutOfWindow, RunningEndsNow }
            },
            { LiveEventCalendarRuleIds.ConfigKeyMissing, new[] { ConfigKeyInheritedDiffers, ConfigKeyInheritedNew, ConfigKeyEmpty } },
            { LiveEventCalendarRuleIds.LongGapBetweenEvents, new[] { LongGap } },
            { LiveEventCalendarRuleIds.RemoteSnapshotDrift, new[] { RemoteDiffers, RemoteWithoutStamp } },
        };

        /// <summary>Mã của một luật theo thứ tự khai báo; rỗng khi id lạ. Trả bản sao — người gọi sửa không làm hỏng danh sách đóng.</summary>
        public static IReadOnlyList<string> ForRule(string ruleId)
        {
            if (ruleId == null || !CodesByRuleId.TryGetValue(ruleId, out string[] codes)) return Array.Empty<string>();
            return (string[])codes.Clone();
        }

        /// <summary>Kiểm nhanh không cấp phát cho builder (gọi mỗi phát hiện).</summary>
        internal static bool IsDeclared(string ruleId, string detailCode)
        {
            if (ruleId == null || detailCode == null || !CodesByRuleId.TryGetValue(ruleId, out string[] codes)) return false;
            for (int index = 0; index < codes.Length; index++)
            {
                if (string.Equals(codes[index], detailCode, StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
