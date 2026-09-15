namespace DreamTech.LiveOps.Editor
{
    // INTERIM(G-SHELLPOLISH): màn giữ chỗ kế thừa lớp gốc tạm (mục 12 I-2) — G-OVERVIEW (W4) thay thân lớp này bằng màn thật.
    /// <summary>Màn <c>overview</c> ở tầng Configure — registry, rail và điều hướng đã dùng id/tiêu đề/tầng thật từ bây giờ.</summary>
    internal sealed class OverviewSection : InterimPlaceholderSection
    {
        public OverviewSection()
            : base(LiveOpsHubSections.Ids.Overview, LiveOpsHubStrings.ShellOverviewTitle, LiveOpsHubStrings.ShellOverviewSubtitle, PipelineStage.Configure)
        {
        }
    }
}
