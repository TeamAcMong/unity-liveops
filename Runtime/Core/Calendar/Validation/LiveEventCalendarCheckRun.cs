using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Một lần Kiểm lịch chạy từng luật một (<see cref="Step"/>) — để Editor chạy một luật mỗi nhịp update và hiện "Đang kiểm
    /// 7/12" mà không đóng băng cửa sổ. Luật ném không bao giờ làm hỏng lần kiểm: thành <see cref="LiveEventCalendarRuleOutcome.Failed"/>
    /// rồi chạy tiếp luật sau (một luật hỏng không được che kết quả của mười một luật còn lại).
    /// </summary>
    public sealed class LiveEventCalendarCheckRun
    {
        private readonly IReadOnlyList<ILiveEventCalendarRule> _rules;
        private readonly LiveEventCalendarCheckContext _context;
        private readonly List<LiveEventCalendarRuleResult> _results;

        internal LiveEventCalendarCheckRun(IReadOnlyList<ILiveEventCalendarRule> rules, LiveEventCalendarCheckContext context)
        {
            _rules = rules;
            _context = context;
            _results = new List<LiveEventCalendarRuleResult>(rules.Count);
            // Bộ luật rỗng (kiểm nhanh một làn với validator tuỳ biến không có luật làn nào) vẫn là lần kiểm đã xong có báo cáo.
            if (rules.Count == 0) Report = new LiveEventCalendarCheckReport(context.NowUtc, Array.Empty<LiveEventCalendarRuleResult>());
        }

        public int RuleCount => _rules.Count;
        public int CompletedRuleCount => _results.Count;
        public bool IsComplete => _results.Count >= _rules.Count;

        /// <summary>Id luật SẮP chạy ở lần <see cref="Step"/> kế; "" khi đã xong.</summary>
        public string CurrentRuleId => IsComplete ? string.Empty : SafeRuleId(_rules[_results.Count], _results.Count);

        /// <summary><c>null</c> tới khi <see cref="IsComplete"/>.</summary>
        public LiveEventCalendarCheckReport Report { get; private set; }

        /// <summary>Chạy đúng một luật; trả <c>false</c> khi đã chạy hết (không làm gì).</summary>
        public bool Step()
        {
            if (IsComplete) return false;

            int ruleIndex = _results.Count;
            ILiveEventCalendarRule rule = _rules[ruleIndex];
            string ruleId = SafeRuleId(rule, ruleIndex);
            LiveEventCalendarRuleResult result;
            try
            {
                result = rule.Evaluate(_context);
                if (result == null)
                {
                    result = LiveEventCalendarRuleResult.Failed(ruleId, typeof(InvalidOperationException).Name, "Luật trả kết quả null.");
                }
            }
            catch (Exception exception)
            {
                result = LiveEventCalendarRuleResult.Failed(ruleId, exception.GetType().Name, exception.Message);
            }

            _results.Add(ApplyIgnoredWarnings(result));
            if (IsComplete) Report = new LiveEventCalendarCheckReport(_context.NowUtc, _results.ToArray());
            return true;
        }

        /// <summary>
        /// Gắn trạng thái bỏ qua theo mục 6.1 "Bỏ qua": chỉ phát hiện KHÔNG phải Bị bỏ mới bỏ qua được (Blocked không bao giờ ẩn
        /// được — game vẫn bỏ mục dù người dùng không muốn thấy). Cảnh báo còn hiệu lực → <c>IgnoredBy</c>; ghi chú hẹn giờ đã
        /// tới hạn → phát hiện hiện lại kèm <c>DueReminder</c>.
        /// </summary>
        private LiveEventCalendarRuleResult ApplyIgnoredWarnings(LiveEventCalendarRuleResult result)
        {
            IReadOnlyList<IgnoredCalendarWarning> warnings = _context.Document.IgnoredWarnings;
            if (result.Findings.Count == 0 || warnings.Count == 0) return result;

            var annotated = new LiveEventCalendarFinding[result.Findings.Count];
            bool changed = false;
            for (int index = 0; index < result.Findings.Count; index++)
            {
                LiveEventCalendarFinding finding = result.Findings[index];
                annotated[index] = finding;
                if (finding.Consequence == LiveEventCalendarConsequence.Dropped) continue;

                IgnoredCalendarWarning activeWarning = null;
                IgnoredCalendarWarning dueReminder = null;
                for (int warningIndex = 0; warningIndex < warnings.Count; warningIndex++)
                {
                    IgnoredCalendarWarning warning = warnings[warningIndex];
                    if (!Matches(warning, finding)) continue;
                    if (warning.IsExpiredAt(_context.NowUtc))
                    {
                        if (dueReminder == null) dueReminder = warning;
                        continue;
                    }
                    activeWarning = warning;
                    break;
                }

                // Còn một cảnh báo hiệu lực thì vẫn ẩn, kể cả khi có hẹn khác đã tới — hẹn tới chỉ nhắc khi không còn gì che.
                LiveEventCalendarFinding withState = activeWarning != null
                    ? finding.WithIgnoreState(activeWarning, null)
                    : finding.WithIgnoreState(null, dueReminder);
                if (!ReferenceEquals(withState, finding)) changed = true;
                annotated[index] = withState;
            }
            return changed ? result.WithFindings(annotated) : result;
        }

        /// <summary>
        /// Khớp theo ruleId + targetId + khoảng đúng như lúc bỏ qua (so thời điểm, không so chuỗi — "2026-09-20T00:00Z" và dạng
        /// canonical là cùng một khoảng). Mốc rỗng ở cảnh báo = không giới hạn phía đó (Toggle phạm vi tắt = mọi khoảng); mốc có
        /// chữ mà không đọc được thì không khớp gì, để cảnh báo hỏng không vô tình ẩn mọi phát hiện.
        /// </summary>
        private static bool Matches(IgnoredCalendarWarning warning, LiveEventCalendarFinding finding)
        {
            if (!string.Equals(warning.RuleId, finding.RuleId, StringComparison.Ordinal)) return false;
            if (!string.Equals(warning.TargetId, finding.TargetId, StringComparison.Ordinal)) return false;
            return BoundMatches(warning.RangeStartUtcText, finding.RangeStartUtc) && BoundMatches(warning.RangeEndUtcText, finding.RangeEndUtc);
        }

        private static bool BoundMatches(string warningBoundText, DateTime? findingBound)
        {
            if (warningBoundText.Length == 0) return true;
            if (!findingBound.HasValue) return false;
            return LiveEventUtcText.TryParse(warningBoundText, out DateTime warningBound) && warningBound == findingBound.Value;
        }

        /// <summary>Id của luật game tự viết có thể ném ngay ở getter — vẫn phải có id để hàng Failed hiện được.</summary>
        private static string SafeRuleId(ILiveEventCalendarRule rule, int ruleIndex)
        {
            try
            {
                string ruleId = rule.RuleId;
                if (!string.IsNullOrEmpty(ruleId)) return ruleId;
            }
            catch (Exception)
            {
                // Rơi xuống id theo vị trí bên dưới.
            }
            return "rule-" + (ruleIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
