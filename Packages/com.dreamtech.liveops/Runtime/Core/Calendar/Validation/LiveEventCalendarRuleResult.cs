using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>Kết quả chạy một luật — chỉ dựng bằng các hàm tạo có tên, để mỗi outcome mang đúng phần dữ liệu của nó.</summary>
    public sealed class LiveEventCalendarRuleResult
    {
        private static readonly LiveEventCalendarFinding[] NoFindings = Array.Empty<LiveEventCalendarFinding>();

        private LiveEventCalendarRuleResult(string ruleId, LiveEventCalendarRuleOutcome outcome, IReadOnlyList<LiveEventCalendarFinding> findings,
            string reasonCode, string exceptionTypeName, string exceptionMessage)
        {
            RuleId = ruleId ?? string.Empty;
            Outcome = outcome;
            Findings = findings;
            ReasonCode = reasonCode ?? string.Empty;
            ExceptionTypeName = exceptionTypeName ?? string.Empty;
            ExceptionMessage = exceptionMessage ?? string.Empty;
        }

        public static LiveEventCalendarRuleResult Passed(string ruleId)
        {
            return new LiveEventCalendarRuleResult(ruleId, LiveEventCalendarRuleOutcome.Passed, NoFindings, null, null, null);
        }

        /// <summary>Ném khi danh sách rỗng: "tìm thấy" mà không có phát hiện nào sẽ làm luật vừa không đạt vừa không có gì để sửa.</summary>
        public static LiveEventCalendarRuleResult Found(string ruleId, IReadOnlyList<LiveEventCalendarFinding> findings)
        {
            if (findings == null || findings.Count == 0)
            {
                throw new ArgumentException("Found cần ít nhất một phát hiện — không có thì trả Passed.", nameof(findings));
            }
            var copied = new LiveEventCalendarFinding[findings.Count];
            for (int index = 0; index < findings.Count; index++)
            {
                copied[index] = findings[index] ?? throw new ArgumentException("Danh sách phát hiện không được chứa null.", nameof(findings));
            }
            return new LiveEventCalendarRuleResult(ruleId, LiveEventCalendarRuleOutcome.Found, copied, null, null, null);
        }

        /// <summary>vd "remote-not-pasted" — luật không đo được nên không tính là đã qua.</summary>
        public static LiveEventCalendarRuleResult NotMeasured(string ruleId, string reasonCode)
        {
            return new LiveEventCalendarRuleResult(ruleId, LiveEventCalendarRuleOutcome.NotMeasured, NoFindings, reasonCode, null, null);
        }

        /// <summary>vd "no-published-stamp" — điều kiện của luật không tồn tại (PD-9), không đếm vào đã qua.</summary>
        public static LiveEventCalendarRuleResult NotApplicable(string ruleId, string reasonCode)
        {
            return new LiveEventCalendarRuleResult(ruleId, LiveEventCalendarRuleOutcome.NotApplicable, NoFindings, reasonCode, null, null);
        }

        public static LiveEventCalendarRuleResult Failed(string ruleId, string exceptionTypeName, string exceptionMessage)
        {
            return new LiveEventCalendarRuleResult(ruleId, LiveEventCalendarRuleOutcome.Failed, NoFindings, null, exceptionTypeName, exceptionMessage);
        }

        public string RuleId { get; }
        public LiveEventCalendarRuleOutcome Outcome { get; }
        public IReadOnlyList<LiveEventCalendarFinding> Findings { get; }
        public string ReasonCode { get; }
        public string ExceptionTypeName { get; }
        public string ExceptionMessage { get; }

        /// <summary>Cùng outcome, thay danh sách phát hiện (đã gắn trạng thái bỏ qua) — chỉ CheckRun dùng.</summary>
        internal LiveEventCalendarRuleResult WithFindings(IReadOnlyList<LiveEventCalendarFinding> findings)
        {
            return new LiveEventCalendarRuleResult(RuleId, Outcome, findings, ReasonCode, ExceptionTypeName, ExceptionMessage);
        }
    }
}
