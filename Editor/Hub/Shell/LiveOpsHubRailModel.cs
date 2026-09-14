using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Model thuần của rail (PD-25): mọi quyết định của rail — tầng nào vẽ, dấu/badge/tooltip, đường nối chết từ đâu, ô chặn
    /// đáy nói gì và dẫn tới đâu — nằm ở đây để test Logic chạy <c>-nographics</c>; <see cref="LiveOpsHubRail"/> chỉ đọc model
    /// và gắn class ([FD §3.5], 8.2).
    /// </summary>
    internal sealed class LiveOpsHubRailModel
    {
        private LiveOpsHubRailModel(IReadOnlyList<LiveOpsHubRailStageRow> stages, int firstBlockedGateIndex, string blockerTitle,
            string blockerDetail, LiveOpsHubNavigation blockerNavigation)
        {
            Stages = stages;
            FirstBlockedGateIndex = firstBlockedGateIndex;
            BlockerTitle = blockerTitle;
            BlockerDetail = blockerDetail;
            BlockerNavigation = blockerNavigation;
        }

        /// <summary>Chỉ tầng có ít nhất một màn (PD-1: P1 không vẽ tầng CHẠY), theo thứ tự <see cref="PipelineStages.All"/>.</summary>
        public IReadOnlyList<LiveOpsHubRailStageRow> Stages { get; }

        /// <summary>Chỉ số trong <see cref="Stages"/> của tầng cổng Blocked đầu tiên; -1 = không có. Đường nối chết từ dấu của tầng này.</summary>
        public int FirstBlockedGateIndex { get; }

        /// <summary>"Dừng ở Kiểm lịch" | "" (không có ô chặn).</summary>
        public string BlockerTitle { get; }

        /// <summary>"2 đợt sẽ bị game bỏ khi đọc lịch." | "Kết quả cũ có 2 đợt bị bỏ — F5 để kiểm lại." | "".</summary>
        public string BlockerDetail { get; }

        /// <summary>Đích khi bấm ô chặn; null khi không có ô chặn.</summary>
        public LiveOpsHubNavigation BlockerNavigation { get; }

        public bool HasBlocker => BlockerNavigation != null;

        /// <param name="sections">Registry theo thứ tự rail.</param>
        /// <param name="healths">Health của từng màn, cùng chỉ số với <paramref name="sections"/> (đã qua throttle).</param>
        /// <param name="summary">Bộ tổng hợp của lần kiểm gần nhất; null khi chưa kiểm lần nào (hoặc chưa có phiên — W2).</param>
        /// <param name="isCheckStale">Kết quả kiểm đã cũ: badge tầng Kiểm "cũ · …", ô chặn nhắc F5.</param>
        public static LiveOpsHubRailModel Build(IReadOnlyList<IHubSection> sections, IReadOnlyList<SectionHealth> healths,
            LiveEventCalendarCheckSummary summary, bool isCheckStale)
        {
            if (sections == null) throw new ArgumentNullException(nameof(sections));
            if (healths == null) throw new ArgumentNullException(nameof(healths));
            if (healths.Count != sections.Count)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellRailHealthCountMismatchFormat,
                    healths.Count, sections.Count), nameof(healths));
            }

            List<LiveOpsHubRailStageRow> stages = new List<LiveOpsHubRailStageRow>();
            int firstBlockedGateIndex = -1;
            foreach (PipelineStage stage in PipelineStages.All)
            {
                List<int> indices = new List<int>();
                for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
                {
                    if (sections[sectionIndex].Stage == stage) indices.Add(sectionIndex);
                }
                if (indices.Count == 0) continue;

                bool connectorAboveDead = firstBlockedGateIndex >= 0;
                StageFacts facts = CollectStage(sections, healths, indices);
                bool isGateBlocked = PipelineStages.IsGate(stage) && facts.State == HealthState.Blocked;
                if (isGateBlocked && firstBlockedGateIndex < 0) firstBlockedGateIndex = stages.Count;
                bool connectorDead = firstBlockedGateIndex >= 0;

                BadgeFacts badge = DescribeBadge(stage, facts, summary, isCheckStale);
                stages.Add(new LiveOpsHubRailStageRow(stage, PipelineStages.CaptionOf(stage), facts.State, badge.Text, badge.State, badge.IsStale,
                    badge.Tooltip, connectorDead, connectorAboveDead, facts.Rows));
            }

            string blockerTitle = string.Empty;
            string blockerDetail = string.Empty;
            LiveOpsHubNavigation blockerNavigation = null;
            if (firstBlockedGateIndex >= 0)
            {
                LiveOpsHubRailStageRow gate = stages[firstBlockedGateIndex];
                LiveOpsHubRailSectionRow blockedRow = FirstRowWithState(gate.Rows, HealthState.Blocked);
                blockerTitle = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellRailBlockerTitleFormat, blockedRow.Title);
                if (gate.Stage == PipelineStage.Check && summary != null && summary.DroppedCount > 0)
                {
                    blockerDetail = string.Format(CultureInfo.InvariantCulture,
                        isCheckStale ? LiveOpsHubStrings.ShellRailBlockerStaleDetailFormat : LiveOpsHubStrings.ShellRailBlockerDroppedDetailFormat,
                        summary.DroppedCount);
                }
                else
                {
                    blockerDetail = blockedRow.Reason;
                }
                blockerNavigation = NavigationFor(gate.Stage, blockedRow.SectionId);
            }
            else if (isCheckStale && summary != null && summary.DroppedCount > 0)
            {
                // Kết quả cũ biến Kiểm lịch thành NotMeasured (PD-23) nên không còn tầng cổng Blocked, nhưng các đợt bị bỏ của
                // lần kiểm trước vẫn còn đó cho tới khi kiểm lại — ô chặn giữ lời nhắc F5 thay vì biến mất như thể đã ổn.
                LiveOpsHubRailStageRow checkStage = FindStage(stages, PipelineStage.Check);
                if (checkStage != null)
                {
                    LiveOpsHubRailSectionRow checkRow = checkStage.Rows[0];
                    blockerTitle = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellRailBlockerTitleFormat, checkRow.Title);
                    blockerDetail = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellRailBlockerStaleDetailFormat, summary.DroppedCount);
                    blockerNavigation = NavigationFor(PipelineStage.Check, checkRow.SectionId);
                }
            }

            return new LiveOpsHubRailModel(stages.AsReadOnly(), firstBlockedGateIndex, blockerTitle, blockerDetail, blockerNavigation);
        }

        /// <summary>
        /// "2 bị bỏ · 1 mất tiến độ · 2 nên xem · 1 chưa kiểm" — chỉ phần khác 0, nặng nhất trước. Một hàm cho badge và tooltip để
        /// hai chỗ không bao giờ đếm khác nhau.
        /// </summary>
        internal static IReadOnlyList<string> CountParts(LiveEventCalendarCheckSummary summary)
        {
            List<string> parts = new List<string>();
            if (summary == null) return parts;
            AddCount(parts, summary.DroppedCount, LiveOpsHubStrings.ShellRailDroppedCountFormat);
            AddCount(parts, summary.ProgressLostCount, LiveOpsHubStrings.ShellRailProgressLostCountFormat);
            AddCount(parts, summary.ShouldReviewCount, LiveOpsHubStrings.ShellRailShouldReviewCountFormat);
            AddCount(parts, summary.NotMeasuredRuleCount, LiveOpsHubStrings.ShellRailNotMeasuredCountFormat);
            return parts;
        }

        private static StageFacts CollectStage(IReadOnlyList<IHubSection> sections, IReadOnlyList<SectionHealth> healths, List<int> indices)
        {
            List<LiveOpsHubRailSectionRow> rows = new List<LiveOpsHubRailSectionRow>();
            SectionHealth worst = healths[indices[0]];
            SectionHealth staleSource = default;
            bool hasStale = false;
            foreach (int index in indices)
            {
                IHubSection section = sections[index];
                SectionHealth health = healths[index];
                worst = SectionHealth.Worse(worst, health);
                if (health.IsStale && !hasStale)
                {
                    staleSource = health;
                    hasStale = true;
                }
                string title = section is IHubRailLabel railLabel && !string.IsNullOrEmpty(railLabel.RailLabel) ? railLabel.RailLabel : section.Title;
                // Tooltip = lý do nếu có, không thì subtitle: NotMeasured luôn có lý do (SectionHealth bắt buộc), nên vòng rỗng
                // không bao giờ chỉ hiện một câu mô tả màn.
                string tooltip = string.IsNullOrEmpty(health.Reason) ? section.Subtitle : health.Reason;
                rows.Add(new LiveOpsHubRailSectionRow(section.Id, title, health.State, tooltip, health.Reason, health.State == HealthState.Ok, health.Badge));
            }
            return new StageFacts(worst, rows.AsReadOnly(), hasStale ? staleSource : worst, hasStale);
        }

        private static BadgeFacts DescribeBadge(PipelineStage stage, StageFacts facts, LiveEventCalendarCheckSummary summary, bool isCheckStale)
        {
            IReadOnlyList<string> countParts = stage == PipelineStage.Check ? CountParts(summary) : new List<string>();
            bool useSummary = stage == PipelineStage.Check && summary != null;

            if (useSummary && isCheckStale && countParts.Count > 0)
            {
                // Badge cũ giữ con số của lần kiểm trước kèm "cũ" (chữ theo mức của con số, không phải NotMeasured) — con số cũ
                // vẫn là thông tin, chỉ không còn chắc đúng ([FD §3.5]).
                string prefix = StaleTooltipPrefix(facts.StaleSource);
                return new BadgeFacts(LiveOpsHubStrings.ShellRailStaleBadgePrefix + countParts[0], WorstCountState(summary), true,
                    prefix + string.Join(LiveOpsHubStrings.ShellRailPartSeparator, countParts));
            }

            switch (facts.State)
            {
                case HealthState.Ok:
                    return new BadgeFacts(string.Empty, HealthState.Ok, false, string.Empty);
                case HealthState.NotMeasured:
                {
                    string tooltip = facts.HasStale && !string.IsNullOrEmpty(facts.StaleSource.Badge)
                        ? StaleTooltipPrefix(facts.StaleSource) + facts.StaleSource.Badge
                        : JoinReasons(facts.Rows);
                    if (facts.HasStale && !string.IsNullOrEmpty(facts.StaleSource.Badge))
                    {
                        return new BadgeFacts(LiveOpsHubStrings.ShellRailStaleBadgePrefix + facts.StaleSource.Badge, HealthState.NotMeasured, true, tooltip);
                    }
                    return new BadgeFacts(LiveOpsHubStrings.ShellRailNotMeasuredBadge, HealthState.NotMeasured, false, tooltip);
                }
                default:
                {
                    if (useSummary && countParts.Count > 0)
                    {
                        return new BadgeFacts(countParts[0], WorstCountState(summary), false, string.Join(LiveOpsHubStrings.ShellRailPartSeparator, countParts));
                    }
                    IReadOnlyList<string> sectionParts = StageCountParts(facts.Rows);
                    if (sectionParts.Count == 0) return new BadgeFacts(string.Empty, facts.State, false, JoinReasons(facts.Rows));
                    return new BadgeFacts(sectionParts[0], facts.State, false, string.Join(LiveOpsHubStrings.ShellRailPartSeparator, sectionParts));
                }
            }
        }

        /// <summary>
        /// Danh sách đếm của tầng không có bộ tổng hợp riêng (LÊN LỊCH, XUẤT) gộp từ badge các màn trong tầng — [FD §3.5] "badge tầng
        /// nêu mức xấu nhất trước; tooltip hàng tầng liệt kê đủ '2 bị bỏ · 1 mất tiến độ · 2 nên xem'". Badge đúng một format đếm của
        /// rail ("{0} bị bỏ"…) thì cộng dồn theo loại (Lịch "1 bị bỏ" + Luật lặp "1 bị bỏ" = "2 bị bỏ"); badge khác ("chặn", "1 cần
        /// xem") giữ nguyên, trùng chữ chỉ ghi một lần. Thứ tự: màn Blocked trước Warning trước NotMeasured, trong cùng mức theo thứ
        /// tự rail. Vì sao gộp từ chữ badge: <see cref="SectionHealth"/> (hợp đồng W1) chỉ mang badge dạng chữ, số đếm theo đích nằm ở
        /// phiên (G-SESSION) — màn phát badge bằng chính các format đếm của rail thì tầng cộng đúng.
        /// </summary>
        internal static IReadOnlyList<string> StageCountParts(IReadOnlyList<LiveOpsHubRailSectionRow> rows)
        {
            List<StageCountEntry> entries = new List<StageCountEntry>();
            foreach (HealthState state in StageCountStateOrder)
            {
                foreach (LiveOpsHubRailSectionRow row in rows)
                {
                    if (row.State != state || string.IsNullOrEmpty(row.Badge)) continue;
                    AddStageCount(entries, row.Badge);
                }
            }

            List<string> parts = new List<string>(entries.Count);
            foreach (StageCountEntry entry in entries)
            {
                parts.Add(entry.Format == null ? entry.Text : string.Format(CultureInfo.InvariantCulture, entry.Format, entry.Count));
            }
            return parts;
        }

        private static readonly HealthState[] StageCountStateOrder = { HealthState.Blocked, HealthState.Warning, HealthState.NotMeasured };

        private static readonly string[] StageCountFormats =
        {
            LiveOpsHubStrings.ShellRailDroppedCountFormat,
            LiveOpsHubStrings.ShellRailProgressLostCountFormat,
            LiveOpsHubStrings.ShellRailShouldReviewCountFormat,
            LiveOpsHubStrings.ShellRailNotMeasuredCountFormat,
        };

        private static void AddStageCount(List<StageCountEntry> entries, string badge)
        {
            string matchedFormat = null;
            int count = 0;
            foreach (string format in StageCountFormats)
            {
                if (TryReadCount(badge, format, out count))
                {
                    matchedFormat = format;
                    break;
                }
            }

            foreach (StageCountEntry entry in entries)
            {
                if (matchedFormat != null && string.Equals(entry.Format, matchedFormat, StringComparison.Ordinal))
                {
                    entry.Count += count;
                    return;
                }
                if (matchedFormat == null && entry.Format == null && string.Equals(entry.Text, badge, StringComparison.Ordinal)) return;
            }
            entries.Add(new StageCountEntry(badge, matchedFormat, count));
        }

        /// <summary>"12 bị bỏ" với format "{0} bị bỏ" → 12. Chỉ nhận số nguyên dương viết liền ở đầu, đuôi khớp nguyên văn.</summary>
        private static bool TryReadCount(string badge, string format, out int count)
        {
            count = 0;
            const string placeholder = "{0}";
            if (!format.StartsWith(placeholder, StringComparison.Ordinal)) return false;
            string suffix = format.Substring(placeholder.Length);
            if (!badge.EndsWith(suffix, StringComparison.Ordinal) || badge.Length == suffix.Length) return false;
            string number = badge.Substring(0, badge.Length - suffix.Length);
            foreach (char character in number)
            {
                if (character < '0' || character > '9') return false;
            }
            return int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out count) && count > 0;
        }

        private sealed class StageCountEntry
        {
            public StageCountEntry(string text, string format, int count)
            {
                Text = text;
                Format = format;
                Count = count;
            }

            public string Text { get; }
            public string Format { get; }
            public int Count { get; set; }
        }

        private static string StaleTooltipPrefix(SectionHealth staleSource)
        {
            if (!staleSource.MeasuredAtUtc.HasValue) return LiveOpsHubStrings.ShellRailStaleTooltipPrefixWithoutTime;
            string time = staleSource.MeasuredAtUtc.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellRailStaleTooltipPrefixFormat, time);
        }

        private static HealthState WorstCountState(LiveEventCalendarCheckSummary summary)
        {
            if (summary.DroppedCount > 0) return HealthState.Blocked;
            if (summary.ProgressLostCount > 0 || summary.ShouldReviewCount > 0) return HealthState.Warning;
            return HealthState.NotMeasured;
        }

        private static string JoinReasons(IReadOnlyList<LiveOpsHubRailSectionRow> rows)
        {
            StringBuilder builder = new StringBuilder();
            foreach (LiveOpsHubRailSectionRow row in rows)
            {
                if (row.State == HealthState.Ok || string.IsNullOrEmpty(row.Reason)) continue;
                if (builder.Length > 0) builder.Append(LiveOpsHubStrings.ShellRailPartSeparator);
                builder.Append(row.Reason);
            }
            return builder.ToString();
        }

        private static void AddCount(List<string> parts, int count, string format)
        {
            if (count > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, format, count));
        }

        private static LiveOpsHubRailSectionRow FirstRowWithState(IReadOnlyList<LiveOpsHubRailSectionRow> rows, HealthState state)
        {
            foreach (LiveOpsHubRailSectionRow row in rows)
            {
                if (row.State == state) return row;
            }
            return rows[0];
        }

        private static LiveOpsHubRailStageRow FindStage(IReadOnlyList<LiveOpsHubRailStageRow> stages, PipelineStage stage)
        {
            foreach (LiveOpsHubRailStageRow row in stages)
            {
                if (row.Stage == stage) return row;
            }
            return null;
        }

        private static LiveOpsHubNavigation NavigationFor(PipelineStage stage, string sectionId)
        {
            LiveOpsHubNavigation navigation = LiveOpsHubNavigation.To(sectionId);
            // Dừng ở tầng Kiểm → mở Kiểm lịch lọc sẵn nhóm "Bị bỏ" (thứ đang chặn); dừng ở Xuất → mở thẳng màn Xuất JSON.
            return stage == PipelineStage.Check ? navigation.WithFilter(LiveOpsHubNavigation.FilterDropped) : navigation;
        }

        private readonly struct StageFacts
        {
            public StageFacts(SectionHealth worst, IReadOnlyList<LiveOpsHubRailSectionRow> rows, SectionHealth staleSource, bool hasStale)
            {
                Worst = worst;
                Rows = rows;
                StaleSource = staleSource;
                HasStale = hasStale;
            }

            public SectionHealth Worst { get; }
            public HealthState State => Worst.State;
            public IReadOnlyList<LiveOpsHubRailSectionRow> Rows { get; }
            public SectionHealth StaleSource { get; }
            public bool HasStale { get; }
        }

        private readonly struct BadgeFacts
        {
            public BadgeFacts(string text, HealthState state, bool isStale, string tooltip)
            {
                Text = text;
                State = state;
                IsStale = isStale;
                Tooltip = tooltip;
            }

            public string Text { get; }
            public HealthState State { get; }
            public bool IsStale { get; }
            public string Tooltip { get; }
        }
    }

    /// <summary>Hàng tầng của rail (22 px): dấu tầng = mức nặng nhất các màn trong tầng.</summary>
    internal sealed class LiveOpsHubRailStageRow
    {
        internal LiveOpsHubRailStageRow(PipelineStage stage, string caption, HealthState state, string badge, HealthState badgeState, bool isBadgeStale,
            string badgeTooltip, bool isConnectorDead, bool isConnectorAboveDead, IReadOnlyList<LiveOpsHubRailSectionRow> rows)
        {
            Stage = stage;
            Caption = caption;
            State = state;
            Badge = badge;
            BadgeState = badgeState;
            IsBadgeStale = isBadgeStale;
            BadgeTooltip = badgeTooltip;
            IsConnectorDead = isConnectorDead;
            IsConnectorAboveDead = isConnectorAboveDead;
            Rows = rows;
        }

        public PipelineStage Stage { get; }

        /// <summary>Nhãn HOA viết sẵn ("KIỂM").</summary>
        public string Caption { get; }

        public HealthState State { get; }

        /// <summary>"" = không badge (Ok); NotMeasured luôn "chưa kiểm", không bao giờ "—".</summary>
        public string Badge { get; }

        /// <summary>Họ chữ của badge — badge cũ "cũ · 2 bị bỏ" vẫn chữ blocked dù tầng đang NotMeasured.</summary>
        public HealthState BadgeState { get; }

        public bool IsBadgeStale { get; }

        public string BadgeTooltip { get; }

        /// <summary>Đoạn nối từ dấu tầng này trở xuống (gồm các hàng màn của tầng) đã chết.</summary>
        public bool IsConnectorDead { get; }

        /// <summary>Đoạn nối phía trên dấu tầng đã chết (một tầng cổng phía trước đã chặn).</summary>
        public bool IsConnectorAboveDead { get; }

        public IReadOnlyList<LiveOpsHubRailSectionRow> Rows { get; }
    }

    /// <summary>Hàng màn của rail (22 px).</summary>
    internal sealed class LiveOpsHubRailSectionRow
    {
        internal LiveOpsHubRailSectionRow(string sectionId, string title, HealthState state, string tooltip, string reason, bool isMarkHidden, string badge)
        {
            SectionId = sectionId;
            Title = title;
            State = state;
            Tooltip = tooltip;
            Reason = reason;
            IsMarkHidden = isMarkHidden;
            Badge = badge;
        }

        public string SectionId { get; }
        public string Title { get; }
        public HealthState State { get; }

        /// <summary>Lý do nếu có, không thì subtitle.</summary>
        public string Tooltip { get; }

        public string Reason { get; }

        /// <summary>Màn Ok ẩn dấu nhưng giữ nhãn đậm đủ — Ok yên lặng ([FD §2.4]).</summary>
        public bool IsMarkHidden { get; }

        /// <summary>Badge của màn (vd "2 bị bỏ", "chặn") — nguồn badge tầng khi không có bộ tổng hợp.</summary>
        public string Badge { get; }
    }
}
