namespace DreamTech.LiveOps.Editor
{
    // INTERIM(G-SHELLPOLISH): màn giữ chỗ kế thừa lớp gốc tạm (mục 12 I-2) — G-RECURRING (W4) thay thân lớp này bằng màn thật.
    /// <summary>Màn <c>recurring</c> ở tầng Schedule — registry, rail và điều hướng đã dùng id/tiêu đề/tầng thật từ bây giờ.</summary>
    internal sealed class RecurringRulesSection : InterimPlaceholderSection
    {
        public RecurringRulesSection(LiveOpsHubServices services)
            : base(LiveOpsHubSections.Ids.RecurringRules, LiveOpsHubStrings.ShellRecurringRulesTitle, LiveOpsHubStrings.ShellRecurringRulesSubtitle, PipelineStage.Schedule)
        {
            // Màn giữ chỗ chưa đọc dữ liệu, nhưng registry đã truyền services như màn thật W4 sẽ nhận — đổi thân lớp không đổi registry.
            Services = services ?? throw new System.ArgumentNullException(nameof(services));
        }

        /// <summary>Services của cửa sổ (G-SESSION) — màn W4 đọc phiên lịch từ đây.</summary>
        internal LiveOpsHubServices Services { get; }
    }
}
