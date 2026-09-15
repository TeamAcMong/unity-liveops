namespace DreamTech.LiveOps.Editor
{
    // INTERIM(G-SHELLPOLISH): màn giữ chỗ kế thừa lớp gốc tạm (mục 12 I-2) — G-RECURRING (W4) thay thân lớp này bằng màn thật.
    /// <summary>Màn <c>recurring</c> ở tầng Schedule — registry, rail và điều hướng đã dùng id/tiêu đề/tầng thật từ bây giờ.</summary>
    internal sealed class RecurringRulesSection : InterimPlaceholderSection
    {
        public RecurringRulesSection()
            : base(LiveOpsHubSections.Ids.RecurringRules, LiveOpsHubStrings.ShellRecurringRulesTitle, LiveOpsHubStrings.ShellRecurringRulesSubtitle, PipelineStage.Schedule)
        {
        }
    }
}
