using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Id của mười hai luật Kiểm lịch. Id nằm trong UI, anchor README và trong ghi chú "Bỏ qua" lưu ở asset người dùng — đổi
    /// chuỗi sau khi phát hành làm mọi cảnh báo đã bỏ qua mất hiệu lực, nên đây là nơi DUY NHẤT viết các chuỗi này (lint chặn
    /// literal id luật ở nơi khác).
    /// </summary>
    public static class LiveEventCalendarRuleIds
    {
        public const string UtcTimeFormat = "utc-time-format";
        public const string EndBeforeStart = "end-before-start";
        public const string InvalidIdentifier = "invalid-identifier";
        public const string DuplicateEventId = "duplicate-event-id";
        public const string OverlapSameType = "overlap-same-type";
        public const string RecurringRuleInvalid = "recurring-rule-invalid";
        public const string ShadowedByRecurring = "shadowed-by-recurring";
        public const string UnknownEventType = "unknown-event-type";
        public const string RunningEventIdChanged = "running-event-id-changed";
        public const string ConfigKeyMissing = "config-key-missing";
        public const string LongGapBetweenEvents = "long-gap-between-events";
        public const string RemoteSnapshotDrift = "remote-snapshot-drift";

        private static readonly string[] AllRuleIds =
        {
            UtcTimeFormat,
            EndBeforeStart,
            InvalidIdentifier,
            DuplicateEventId,
            OverlapSameType,
            RecurringRuleInvalid,
            ShadowedByRecurring,
            UnknownEventType,
            RunningEventIdChanged,
            ConfigKeyMissing,
            LongGapBetweenEvents,
            RemoteSnapshotDrift,
        };

        /// <summary>
        /// Đúng thứ tự khai báo trên = thứ tự validator chạy = thứ tự "Đang kiểm 7/12". Trả bản sao để người gọi sửa mảng
        /// không làm lệch thứ tự chạy của validator mặc định.
        /// </summary>
        public static IReadOnlyList<string> All => (string[])AllRuleIds.Clone();

        /// <summary>Vị trí của luật trong <see cref="All"/>; -1 khi id lạ (luật do game tự thêm).</summary>
        internal static int PositionOf(string ruleId)
        {
            if (ruleId == null) return -1;
            for (int index = 0; index < AllRuleIds.Length; index++)
            {
                if (string.Equals(AllRuleIds[index], ruleId, System.StringComparison.Ordinal)) return index;
            }
            return -1;
        }
    }
}
