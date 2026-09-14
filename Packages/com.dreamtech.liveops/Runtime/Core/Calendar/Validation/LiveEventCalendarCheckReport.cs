using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>Báo cáo của một lần Kiểm lịch đã chạy xong — bất biến, dựng bởi <see cref="LiveEventCalendarCheckRun"/>.</summary>
    public sealed class LiveEventCalendarCheckReport
    {
        internal LiveEventCalendarCheckReport(DateTime checkedAtUtc, IReadOnlyList<LiveEventCalendarRuleResult> ruleResults)
        {
            CheckedAtUtc = checkedAtUtc;
            RuleResults = ruleResults;

            var findings = new List<LiveEventCalendarFinding>();
            var ignoredFindings = new List<LiveEventCalendarFinding>();
            for (int resultIndex = 0; resultIndex < ruleResults.Count; resultIndex++)
            {
                IReadOnlyList<LiveEventCalendarFinding> ruleFindings = ruleResults[resultIndex].Findings;
                for (int findingIndex = 0; findingIndex < ruleFindings.Count; findingIndex++)
                {
                    LiveEventCalendarFinding finding = ruleFindings[findingIndex];
                    if (finding.IsIgnored) ignoredFindings.Add(finding);
                    else findings.Add(finding);
                }
            }
            Findings = SortForDisplay(findings);
            IgnoredFindings = SortForDisplay(ignoredFindings);
            Summary = LiveEventCalendarCheckSummary.From(ruleResults);
        }

        public DateTime CheckedAtUtc { get; }
        public IReadOnlyList<LiveEventCalendarRuleResult> RuleResults { get; }

        /// <summary>Chưa bỏ qua; sắp: hậu quả → AnchorUtc (không có xếp cuối) → thứ tự luật → TargetId.</summary>
        public IReadOnlyList<LiveEventCalendarFinding> Findings { get; }

        public IReadOnlyList<LiveEventCalendarFinding> IgnoredFindings { get; }
        public LiveEventCalendarCheckSummary Summary { get; }

        /// <summary>Phát hiện chưa bỏ qua của một đích (inspector "Vấn đề (n)", dấu trên thanh timeline).</summary>
        public IReadOnlyList<LiveEventCalendarFinding> FindingsForTarget(LiveEventCalendarTargetKind kind, string targetEntryKey)
        {
            var matches = new List<LiveEventCalendarFinding>();
            if (targetEntryKey == null) return matches;
            for (int index = 0; index < Findings.Count; index++)
            {
                LiveEventCalendarFinding finding = Findings[index];
                if (finding.TargetKind == kind && string.Equals(finding.TargetEntryKey, targetEntryKey, StringComparison.Ordinal))
                {
                    matches.Add(finding);
                }
            }
            return matches;
        }

        /// <summary>
        /// Sắp ổn định (List.Sort không ổn định — giữ vị trí gốc làm khoá hoà) để hai lần kiểm cùng tài liệu ra đúng cùng thứ tự
        /// hàng, không nhảy khi người dùng bấm F5 lại. Thứ tự luật dùng vị trí trong <see cref="LiveEventCalendarRuleIds.All"/>
        /// (thứ tự người dùng thấy ở "Đang kiểm n/12"), id lạ xếp sau theo ordinal.
        /// </summary>
        private static IReadOnlyList<LiveEventCalendarFinding> SortForDisplay(List<LiveEventCalendarFinding> findings)
        {
            var indexed = new List<KeyValuePair<int, LiveEventCalendarFinding>>(findings.Count);
            for (int index = 0; index < findings.Count; index++)
            {
                indexed.Add(new KeyValuePair<int, LiveEventCalendarFinding>(index, findings[index]));
            }

            indexed.Sort((left, right) =>
            {
                LiveEventCalendarFinding leftFinding = left.Value;
                LiveEventCalendarFinding rightFinding = right.Value;

                int byConsequence = leftFinding.Consequence.CompareTo(rightFinding.Consequence);
                if (byConsequence != 0) return byConsequence;

                if (leftFinding.AnchorUtc.HasValue != rightFinding.AnchorUtc.HasValue) return leftFinding.AnchorUtc.HasValue ? -1 : 1;
                if (leftFinding.AnchorUtc.HasValue)
                {
                    int byAnchor = leftFinding.AnchorUtc.Value.CompareTo(rightFinding.AnchorUtc.Value);
                    if (byAnchor != 0) return byAnchor;
                }

                int byRule = CompareRuleIds(leftFinding.RuleId, rightFinding.RuleId);
                if (byRule != 0) return byRule;

                int byTarget = string.CompareOrdinal(leftFinding.TargetId, rightFinding.TargetId);
                return byTarget != 0 ? byTarget : left.Key.CompareTo(right.Key);
            });

            var sorted = new LiveEventCalendarFinding[indexed.Count];
            for (int index = 0; index < indexed.Count; index++) sorted[index] = indexed[index].Value;
            return sorted;
        }

        private static int CompareRuleIds(string leftRuleId, string rightRuleId)
        {
            int leftIndex = LiveEventCalendarRuleIds.PositionOf(leftRuleId);
            int rightIndex = LiveEventCalendarRuleIds.PositionOf(rightRuleId);
            if (leftIndex >= 0 && rightIndex >= 0) return leftIndex.CompareTo(rightIndex);
            if ((leftIndex >= 0) != (rightIndex >= 0)) return leftIndex >= 0 ? -1 : 1;
            return string.CompareOrdinal(leftRuleId, rightRuleId);
        }
    }
}
