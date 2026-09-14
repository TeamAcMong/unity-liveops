namespace DreamTech.LiveOps
{
    // INTERIM(G-VALIDATOR-B): giữ chỗ luật 9–12 tới khi gói luật 9–12 (W2) thay bằng luật thật và xoá file này (sổ mục 12, I-1).
    /// <summary>
    /// Giữ chỗ cho một luật chưa viết: luôn trả <see cref="LiveEventCalendarRuleOutcome.NotMeasured"/> "rule-not-built". Vì sao
    /// không bỏ hẳn luật khỏi validator mặc định: số luật và thứ tự "Đang kiểm n/12" phải đúng mười hai từ W1, và luật chưa có
    /// phải hiện là "chưa kiểm" — trả Passed sẽ nói dối rằng lịch đã qua một luật chưa từng chạy.
    /// </summary>
    internal sealed class NotYetImplementedRule : ILiveEventCalendarRule
    {
        public const string NotBuiltReasonCode = "rule-not-built";

        public NotYetImplementedRule(string ruleId, LiveEventCalendarConsequence consequence)
        {
            RuleId = ruleId;
            Consequence = consequence;
        }

        public string RuleId { get; }
        public LiveEventCalendarConsequence Consequence { get; }

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            return LiveEventCalendarRuleResult.NotMeasured(RuleId, NotBuiltReasonCode);
        }
    }
}
