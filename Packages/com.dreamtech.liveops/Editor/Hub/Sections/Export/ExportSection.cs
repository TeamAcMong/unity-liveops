namespace DreamTech.LiveOps.Editor
{
    // INTERIM(G-SHELLPOLISH): màn giữ chỗ kế thừa lớp gốc tạm (mục 12 I-2) — G-EXPORT (W4) thay thân lớp này bằng màn thật.
    /// <summary>Màn <c>export</c> ở tầng Export — registry, rail và điều hướng đã dùng id/tiêu đề/tầng thật từ bây giờ.</summary>
    internal sealed class ExportSection : InterimPlaceholderSection
    {
        public ExportSection()
            : base(LiveOpsHubSections.Ids.Export, LiveOpsHubStrings.ShellExportTitle, LiveOpsHubStrings.ShellExportSubtitle, PipelineStage.Export)
        {
        }
    }
}
