namespace DreamTech.LiveOps
{
    /// <summary>
    /// Một luật Kiểm lịch. Luật chỉ ĐỌC ngữ cảnh (tài liệu đã ở thứ tự xuất + kết quả biên dịch) và trả kết quả có mã — không
    /// sửa tài liệu, không tự đoán lại luật runtime (đọc <see cref="LiveEventCalendarCompilation"/> để biết mục nào bị bỏ).
    /// </summary>
    public interface ILiveEventCalendarRule
    {
        string RuleId { get; }

        /// <summary>Hậu quả khi luật tìm thấy vấn đề (phát hiện riêng có thể nhẹ hơn, vd loại lạ chỉ có trong bản remote).</summary>
        LiveEventCalendarConsequence Consequence { get; }

        /// <summary>Có thể ném — <see cref="LiveEventCalendarCheckRun"/> bọc thành <see cref="LiveEventCalendarRuleOutcome.Failed"/> và chạy tiếp luật sau.</summary>
        LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context);
    }
}
