namespace DreamTech.LiveOps.Editor
{
    // INTERIM(G-SHELLPOLISH): màn giữ chỗ kế thừa lớp gốc tạm (mục 12 I-2) — G-EVENTTYPES (W4) thay thân lớp này bằng màn thật.
    /// <summary>Màn <c>event-types</c> ở tầng Configure — registry, rail và điều hướng đã dùng id/tiêu đề/tầng thật từ bây giờ.</summary>
    internal sealed class EventTypesSection : InterimPlaceholderSection
    {
        public EventTypesSection()
            : base(LiveOpsHubSections.Ids.EventTypes, LiveOpsHubStrings.ShellEventTypesTitle, LiveOpsHubStrings.ShellEventTypesSubtitle, PipelineStage.Configure)
        {
        }
    }
}
