using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Số đếm của một lần Kiểm lịch — MỘT hàm (<see cref="From"/>) cho mọi nơi hiện số (tab, rail, cổng xuất, Tổng quan), để
    /// hai màn không bao giờ đếm khác nhau cho cùng một báo cáo.
    /// </summary>
    public sealed class LiveEventCalendarCheckSummary
    {
        private LiveEventCalendarCheckSummary(int droppedCount, int remoteSnapshotFindingCount, int progressLostCount, int shouldReviewCount,
            int notMeasuredRuleCount, int passedRuleCount, int notApplicableRuleCount, int ignoredCount, int safeRepairCount, int ruleCount,
            LiveEventCalendarConsequence? worstConsequence)
        {
            DroppedCount = droppedCount;
            RemoteSnapshotFindingCount = remoteSnapshotFindingCount;
            ProgressLostCount = progressLostCount;
            ShouldReviewCount = shouldReviewCount;
            NeedsActionCount = droppedCount + progressLostCount + shouldReviewCount;
            NotMeasuredRuleCount = notMeasuredRuleCount;
            PassedRuleCount = passedRuleCount;
            NotApplicableRuleCount = notApplicableRuleCount;
            IgnoredCount = ignoredCount;
            SafeRepairCount = safeRepairCount;
            RuleCount = ruleCount;
            WorstConsequence = worstConsequence;
        }

        /// <summary>Phát hiện Bị bỏ chưa bỏ qua, CHỈ về nháp (V-17).</summary>
        public int DroppedCount { get; }

        /// <summary>(V-17) Phát hiện về JSON đang chạy đã dán (chưa bỏ qua); không vào <see cref="NeedsActionCount"/>, không chặn Copy.</summary>
        public int RemoteSnapshotFindingCount { get; }

        public int ProgressLostCount { get; }
        public int ShouldReviewCount { get; }

        /// <summary>Bị bỏ + Mất tiến độ + Nên xem — "CẦN XỬ LÝ 5".</summary>
        public int NeedsActionCount { get; }

        /// <summary>NotMeasured + Failed — "1 chưa kiểm": luật ném cũng là chưa kiểm được, không phải đã qua.</summary>
        public int NotMeasuredRuleCount { get; }

        public int PassedRuleCount { get; }
        public int NotApplicableRuleCount { get; }

        /// <summary>"1 đã bỏ qua" — không đếm vào rail.</summary>
        public int IgnoredCount { get; }

        /// <summary>"Sửa các lỗi an toàn (1)…" — phát hiện về nháp, chưa bỏ qua, có Sửa an toàn.</summary>
        public int SafeRepairCount { get; }

        public int RuleCount { get; }

        /// <summary>Nặng nhất trong phát hiện chưa bỏ qua (cả phát hiện về bản remote); <c>null</c> khi không có phát hiện nào.</summary>
        public LiveEventCalendarConsequence? WorstConsequence { get; }

        public static LiveEventCalendarCheckSummary From(IReadOnlyList<LiveEventCalendarRuleResult> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            int droppedCount = 0;
            int remoteSnapshotFindingCount = 0;
            int progressLostCount = 0;
            int shouldReviewCount = 0;
            int notMeasuredRuleCount = 0;
            int passedRuleCount = 0;
            int notApplicableRuleCount = 0;
            int ignoredCount = 0;
            int safeRepairCount = 0;
            LiveEventCalendarConsequence? worstConsequence = null;

            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                LiveEventCalendarRuleResult result = results[resultIndex];
                switch (result.Outcome)
                {
                    case LiveEventCalendarRuleOutcome.Passed:
                        passedRuleCount++;
                        break;
                    case LiveEventCalendarRuleOutcome.NotMeasured:
                    case LiveEventCalendarRuleOutcome.Failed:
                        notMeasuredRuleCount++;
                        break;
                    case LiveEventCalendarRuleOutcome.NotApplicable:
                        notApplicableRuleCount++;
                        break;
                }

                IReadOnlyList<LiveEventCalendarFinding> findings = result.Findings;
                for (int findingIndex = 0; findingIndex < findings.Count; findingIndex++)
                {
                    LiveEventCalendarFinding finding = findings[findingIndex];
                    if (finding.IsIgnored)
                    {
                        ignoredCount++;
                        continue;
                    }

                    if (!worstConsequence.HasValue || finding.Consequence < worstConsequence.Value) worstConsequence = finding.Consequence;

                    if (finding.IsAboutRemoteSnapshot)
                    {
                        remoteSnapshotFindingCount++;
                        continue;
                    }

                    switch (finding.Consequence)
                    {
                        case LiveEventCalendarConsequence.Dropped:
                            droppedCount++;
                            break;
                        case LiveEventCalendarConsequence.ProgressLost:
                            progressLostCount++;
                            break;
                        case LiveEventCalendarConsequence.ShouldReview:
                            shouldReviewCount++;
                            break;
                    }
                    if (finding.RepairKind == LiveEventCalendarRepairKind.SafeRepair) safeRepairCount++;
                }
            }

            return new LiveEventCalendarCheckSummary(droppedCount, remoteSnapshotFindingCount, progressLostCount, shouldReviewCount,
                notMeasuredRuleCount, passedRuleCount, notApplicableRuleCount, ignoredCount, safeRepairCount, results.Count, worstConsequence);
        }
    }
}
