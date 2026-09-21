using System;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Một cảnh báo Kiểm lịch đã bị "Bỏ qua" cho một phạm vi cụ thể — chỉ luật <c>Ignorable</c> (Warning) mới bỏ qua
    /// được, Blocked thì không bao giờ. Khoảng trống rỗng nghĩa là không giới hạn khoảng/hạn.
    /// </summary>
    public sealed class IgnoredCalendarWarning
    {
        public IgnoredCalendarWarning(string ruleId, string targetId, string rangeStartUtcText, string rangeEndUtcText,
            string note, string expiresUtcText)
        {
            RuleId = ruleId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            RangeStartUtcText = rangeStartUtcText ?? string.Empty;
            RangeEndUtcText = rangeEndUtcText ?? string.Empty;
            Note = note ?? string.Empty;
            ExpiresUtcText = expiresUtcText ?? string.Empty;
        }

        public string RuleId { get; }
        public string TargetId { get; }
        public string RangeStartUtcText { get; }
        public string RangeEndUtcText { get; }
        public string Note { get; }

        /// <summary>"" = không hết hạn. Khác rỗng = ghi chú "Bỏ qua tới khi…" hẹn giờ.</summary>
        public string ExpiresUtcText { get; }

        public bool IsReminder => ExpiresUtcText.Length > 0;

        public bool IsExpiredAt(DateTime nowUtc)
        {
            if (!IsReminder) return false;
            return LiveEventUtcText.TryParse(ExpiresUtcText, out DateTime expiresUtc) && nowUtc >= expiresUtc;
        }
    }
}
