namespace DreamTech.LiveOps.Editor
{
    // INTERIM(G-SHELLPOLISH): màn giữ chỗ kế thừa lớp gốc tạm (mục 12 I-2) — G-VALIDATION (W4) thay thân lớp này bằng màn thật.
    /// <summary>Màn <c>validation</c> ở tầng Check — registry, rail và điều hướng đã dùng id/tiêu đề/tầng thật từ bây giờ.</summary>
    internal sealed class ValidationSection : InterimPlaceholderSection
    {
        public ValidationSection(LiveOpsHubServices services)
            : base(LiveOpsHubSections.Ids.Validation, LiveOpsHubStrings.ShellValidationTitle, LiveOpsHubStrings.ShellValidationSubtitle, PipelineStage.Check)
        {
            // Màn giữ chỗ chưa đọc dữ liệu, nhưng registry đã truyền services như màn thật W4 sẽ nhận — đổi thân lớp không đổi registry.
            Services = services ?? throw new System.ArgumentNullException(nameof(services));
        }

        /// <summary>Services của cửa sổ (G-SESSION) — màn W4 đọc phiên lịch từ đây.</summary>
        internal LiveOpsHubServices Services { get; }
    }
}
