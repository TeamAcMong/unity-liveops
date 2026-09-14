namespace DreamTech.LiveOps
{
    /// <summary>
    /// Lý do CHÍNH một mục (đợt cố định hoặc luật lặp) bị bộ biên dịch bỏ — theo đúng thứ tự kiểm cố định của
    /// <see cref="LiveEventCalendarCompiler"/> (V-7): mỗi mục bị bỏ mang đúng MỘT lý do chính; lỗi cấp mục khác của
    /// cùng mục nằm ở <see cref="LiveEventCalendarEntryOutcome.AdditionalItemReasons"/>, không sinh phát hiện riêng.
    /// </summary>
    public enum LiveEventCalendarDropReason
    {
        None = 0,
        UnreadableStartUtc = 1,
        UnreadableEndUtc = 2,
        InvalidIdentifier = 3,
        EndNotAfterStart = 4,
        DuplicateEventId = 5,
        OverlapsSameType = 6,
        ShadowedByRecurring = 7,
        InvalidRecurringRule = 8,
        DuplicateRecurringType = 9,
    }
}
