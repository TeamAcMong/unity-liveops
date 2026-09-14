namespace DreamTech.LiveOps
{
    /// <summary>
    /// Hậu quả với người chơi khi một luật Kiểm lịch tìm thấy vấn đề. Thứ tự số = độ nặng giảm dần = thứ tự nhóm trên màn
    /// Kiểm lịch và thứ tự nhảy F8 — nên chỉ thêm giá trị vào cuối, không đổi số.
    /// </summary>
    public enum LiveEventCalendarConsequence
    {
        Dropped = 0,
        ProgressLost = 1,
        ShouldReview = 2,
        Safe = 3,
    }

    /// <summary>
    /// Kết quả chạy MỘT luật. Tách "không kiểm được" (NotMeasured, Failed) khỏi "đã qua" (Passed) vì bỏ qua không phải là
    /// đạt: hub đếm hai nhóm này riêng, không bao giờ gộp thành xanh.
    /// </summary>
    public enum LiveEventCalendarRuleOutcome
    {
        Passed = 0,
        Found = 1,
        NotMeasured = 2,
        NotApplicable = 3,
        Failed = 4,
    }

    /// <summary>Đích của một phát hiện — quyết định dấu section nào trên rail sáng lên (mục 6.4).</summary>
    public enum LiveEventCalendarTargetKind
    {
        FixedEvent = 0,
        RecurringRule = 1,
        EventType = 2,
        RemoteSnapshot = 3,
    }

    /// <summary>Kiểu sửa kèm phát hiện — quyết định nút chính trên hàng phát hiện ("Sửa", "Đề xuất…", "Quyết định…", "Bỏ qua…").</summary>
    public enum LiveEventCalendarRepairKind
    {
        None = 0,
        SafeRepair = 1,
        Proposal = 2,
        Decision = 3,
        Ignorable = 4,
    }
}
