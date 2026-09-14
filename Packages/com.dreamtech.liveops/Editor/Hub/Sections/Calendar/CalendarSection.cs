namespace DreamTech.LiveOps.Editor
{
    // INTERIM(G-SHELLPOLISH): màn giữ chỗ kế thừa lớp gốc tạm (mục 12 I-2) — G-CALENDAR (W4) thay thân lớp này bằng màn thật.
    /// <summary>Màn <c>calendar</c> ở tầng Schedule — registry, rail và điều hướng đã dùng id/tiêu đề/tầng thật từ bây giờ.</summary>
    internal sealed class CalendarSection : InterimPlaceholderSection
    {
        public CalendarSection()
            : base(LiveOpsHubSections.Ids.Calendar, LiveOpsHubStrings.ShellCalendarTitle, LiveOpsHubStrings.ShellCalendarSubtitle, PipelineStage.Schedule)
        {
        }
    }
}
