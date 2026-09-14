namespace DreamTech.LiveOps
{
    /// <summary>Loại mục trong tài liệu lịch — dùng chung cho diff và bản đồ dòng JSON (mục nào ứng với dòng nào).</summary>
    public enum LiveEventCalendarItemKind
    {
        FixedEvent = 0,
        RecurringRule = 1,
        EventType = 2,
    }
}
