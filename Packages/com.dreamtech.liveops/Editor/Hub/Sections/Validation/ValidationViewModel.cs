using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Trạng thái thân màn Kiểm lịch (mục 3, [SD2 §2.8]). Số giá trị giữ nguyên thứ tự mục 3 để đọc chéo kế hoạch.</summary>
    internal enum ValidationBodyState
    {
        /// <summary>Mặc định: đã kiểm, kết quả còn mới, còn phát hiện (Hình 15/16).</summary>
        Default = 0,

        /// <summary>Chưa kiểm lần nào trong phiên — trống KHÔNG phải đạt (Q-12).</summary>
        NeverChecked = 1,

        Running = 2,

        /// <summary>Đã kiểm nhưng lịch đổi hoặc đã qua mốc: số cũ vẫn hiện, kèm HelpBox và tag "cũ" (PD-23).</summary>
        Stale = 3,

        NoErrors = 4,

        /// <summary>Hết Bị bỏ / Mất tiến độ nhưng còn Nên xem ([SD2 §4 mục 23]): vẫn vẽ card, chỉ thêm một dòng note Ok.</summary>
        ShouldReviewOnly = 5,
    }

    /// <summary>Nhóm card của màn, theo đúng thứ tự hậu quả xấu → nhẹ rồi tới ba nhóm "không phải phát hiện" ([SD2 §1.1]).</summary>
    internal enum ValidationGroupKind
    {
        Dropped = 0,
        ProgressLost = 1,
        ShouldReview = 2,
        NotMeasured = 3,
        Passed = 4,
        Ignored = 5,
    }

    /// <summary>
    /// Động từ của khối nút bên phải một hàng ([SD2 §2.4] — bốn động từ KHÔNG TRỘN: Sửa áp ngay, Đề xuất… luôn hỏi,
    /// Quyết định… là menu vì có hai cách đều đúng, Bỏ qua cảnh báo… chỉ có ở Warning).
    /// </summary>
    internal enum ValidationRowAction
    {
        None = 0,
        SafeRepair = 1,
        Proposal = 2,
        ViewDiff = 3,
        DeclareType = 4,
        PasteRunningJson = 5,
        CopyError = 6,

        /// <summary>Hàng vẽ <see cref="DecisionMenu"/> thay cho nút — màn cắm menu vào vì menu cần phiên để áp.</summary>
        Decision = 7,

        /// <summary>Mở popover "Bỏ qua cảnh báo…" ([SD2 §2.7]).</summary>
        IgnoreWarning = 8,

        /// <summary>Nút nhỏ "Bỏ bỏ qua" của hàng trong nhóm "Đã bỏ qua" (V-15).</summary>
        Unignore = 9,
    }

    /// <summary>
    /// Dữ liệu ngữ cảnh mà câu chữ của phát hiện cần (V-22 CC-FT-1): dấu đã đăng mới nhất cho <c>Headline</c>, đồng hồ cho
    /// <c>Meta</c>, tên asset lịch cho <c>PrimaryButtonTooltip</c>. Phiên có đủ cả ba nên màn BẮT BUỘC truyền vào — bản không
    /// ngữ cảnh chỉ dành cho nơi thật sự thiếu dữ liệu.
    /// </summary>
    internal sealed class ValidationTextContext
    {
        internal static readonly ValidationTextContext Empty = new ValidationTextContext(null, default(DateTime), string.Empty);

        internal ValidationTextContext(LiveEventCalendarDocument document, DateTime nowUtc, string calendarAssetName)
        {
            Document = document ?? LiveEventCalendarDocument.Empty;
            NowUtc = nowUtc;
            CalendarAssetName = calendarAssetName ?? string.Empty;
        }

        internal LiveEventCalendarDocument Document { get; }
        internal DateTime NowUtc { get; }
        internal string CalendarAssetName { get; }
        internal PublishedCalendarStamp LatestStamp => Document.LatestStamp;

        /// <summary>Loại event của một phát hiện — để lọc theo loại. "" khi phát hiện không gắn loại nào (bản remote, luật lặp lạ).</summary>
        internal string EventTypeOf(LiveEventCalendarFinding finding)
        {
            if (finding == null) return string.Empty;
            switch (finding.TargetKind)
            {
                case LiveEventCalendarTargetKind.EventType:
                case LiveEventCalendarTargetKind.RecurringRule:
                    return finding.TargetId;
                case LiveEventCalendarTargetKind.FixedEvent:
                {
                    FixedLiveEventEntry entry;
                    if (finding.TargetEntryKey.Length > 0 && Document.TryGetFixedEvent(finding.TargetEntryKey, out entry)) return entry.EventType;
                    return FindTypeByEventId(finding.TargetId);
                }
                default:
                    return string.Empty;
            }
        }

        private string FindTypeByEventId(string eventId)
        {
            if (eventId.Length == 0) return string.Empty;
            // Phát hiện của mục KHÔNG đọc được (id trùng, giờ hỏng) có thể không mang entryKey; tìm theo id là cách còn lại.
            for (int index = 0; index < Document.FixedEvents.Count; index++)
            {
                if (string.Equals(Document.FixedEvents[index].EventId, eventId, StringComparison.Ordinal)) return Document.FixedEvents[index].EventType;
            }
            return string.Empty;
        }
    }

    /// <summary>Một tab lọc của toolbar ([SD2 §2.1]). Số đếm luôn của CẢ báo cáo, không của danh sách đang lọc ([SD2 §1.3]).</summary>
    internal sealed class ValidationTab
    {
        internal ValidationTab(string filter, string label, int count)
        {
            Filter = filter;
            Label = label;
            Count = count;
        }

        /// <summary>Hằng <c>LiveOpsHubNavigation.Filter…</c>; "" = tab "Tất cả".</summary>
        internal string Filter { get; }

        internal string Label { get; }
        internal int Count { get; }
    }

    /// <summary>Một hàng trong card — phát hiện, hoặc kết quả luật không đo được / đã qua.</summary>
    internal sealed class ValidationRow
    {
        internal ValidationRow(LiveEventCalendarFinding finding, LiveEventCalendarRuleResult ruleResult, HealthState state,
            string headline, string metaText, string ruleIdLine, ValidationRowAction action, string actionText, string actionTooltip,
            string linkText, string manualFixSentence)
            : this(finding, ruleResult, state, headline, metaText, ruleIdLine, action, actionText, actionTooltip, linkText,
                manualFixSentence, string.Empty, null)
        {
        }

        internal ValidationRow(LiveEventCalendarFinding finding, LiveEventCalendarRuleResult ruleResult, HealthState state,
            string headline, string metaText, string ruleIdLine, ValidationRowAction action, string actionText, string actionTooltip,
            string linkText, string manualFixSentence, string tagText, IgnoredCalendarWarning ignoredWarning)
        {
            TagText = tagText ?? string.Empty;
            IgnoredWarning = ignoredWarning;
            Finding = finding;
            RuleResult = ruleResult;
            State = state;
            Headline = headline ?? string.Empty;
            MetaText = metaText ?? string.Empty;
            RuleIdLine = ruleIdLine ?? string.Empty;
            Action = action;
            ActionText = actionText ?? string.Empty;
            ActionTooltip = actionTooltip ?? string.Empty;
            LinkText = linkText ?? string.Empty;
            ManualFixSentence = manualFixSentence ?? string.Empty;
        }

        /// <summary>Phát hiện của hàng; null với hàng kết quả luật (nhóm Chưa kiểm, Đã qua).</summary>
        internal LiveEventCalendarFinding Finding { get; }

        internal LiveEventCalendarRuleResult RuleResult { get; }
        internal HealthState State { get; }
        internal string Headline { get; }
        internal string MetaText { get; }
        internal string RuleIdLine { get; }
        internal ValidationRowAction Action { get; }
        internal string ActionText { get; }
        internal string ActionTooltip { get; }

        /// <summary>Link phụ ("Xem trong lịch", "Mở luật"); cũng là đường đi duy nhất của hàng Quyết định… ở W4 (I-7).</summary>
        internal string LinkText { get; }

        /// <summary>(V-21 CC-VALB-3) Câu chỉ cách sửa tay khi luật không đưa ra lệnh sửa nào — "" với hàng có lệnh sửa.</summary>
        internal string ManualFixSentence { get; }

        /// <summary>
        /// Tag nhỏ cạnh headline: "đã tới hẹn" khi ghi chú hẹn giờ hết hạn (V-22 CC-FT-1), "hẹn tới 14/9 00:00" trên hàng của
        /// nhóm "Đã bỏ qua". "" khi hàng không có gì để gắn.
        /// </summary>
        internal string TagText { get; }

        /// <summary>Cảnh báo đã bỏ qua mà hàng này đại diện (nhóm "Đã bỏ qua", V-15); null với mọi hàng khác.</summary>
        internal IgnoredCalendarWarning IgnoredWarning { get; }

        /// <summary>Khoá ổn định để giữ lựa chọn qua mỗi lần dựng lại (dấu vân tay phát hiện, hoặc id luật).</summary>
        internal string SelectionKey => Finding != null ? Finding.Fingerprint : (RuleResult != null ? RuleResult.RuleId : string.Empty);
    }

    /// <summary>Một card nhóm. <see cref="Rows"/> đã lọc; <see cref="TotalCount"/> là số chưa lọc để meta không nói dối.</summary>
    internal sealed class ValidationGroup
    {
        internal ValidationGroup(ValidationGroupKind kind, string title, string blurb, string metaText, HealthState state,
            IReadOnlyList<ValidationRow> rows, int totalCount, string collapsedDetailText)
        {
            Kind = kind;
            Title = title ?? string.Empty;
            Blurb = blurb ?? string.Empty;
            MetaText = metaText ?? string.Empty;
            State = state;
            Rows = rows ?? Array.Empty<ValidationRow>();
            TotalCount = totalCount;
            CollapsedDetailText = collapsedDetailText ?? string.Empty;
        }

        internal ValidationGroupKind Kind { get; }
        internal string Title { get; }
        internal string Blurb { get; }
        internal string MetaText { get; }
        internal HealthState State { get; }
        internal IReadOnlyList<ValidationRow> Rows { get; }
        internal int TotalCount { get; }

        /// <summary>Dòng mono của card thu gọn: id luật đã qua, hoặc meta của mục đã bỏ qua.</summary>
        internal string CollapsedDetailText { get; }

        /// <summary>Hai card cuối ([SD2 §2.1]) mặc định thu gọn và nằm chung một hàng ngang.</summary>
        internal bool IsCollapsible => Kind == ValidationGroupKind.Passed || Kind == ValidationGroupKind.Ignored;
    }

    /// <summary>
    /// Model thuần của màn Kiểm lịch (mục 3, mục 7.5): từ <see cref="LiveOpsHubCheckState"/> + bộ lọc ra tab, dải summary,
    /// card nhóm và hàng. Không một câu nào của phát hiện được viết ở đây — tất cả đi qua <see cref="LiveOpsFindingText"/>
    /// (V-8) với overload CÓ NGỮ CẢNH (V-22 CC-FT-1); model chỉ viết chữ của khung màn (tiêu đề nhóm, meta, summary).
    /// </summary>
    internal sealed class ValidationViewModel
    {
        private static readonly IReadOnlyList<ValidationRow> NoRows = Array.Empty<ValidationRow>();

        private ValidationViewModel(ValidationBodyState bodyState, IReadOnlyList<ValidationTab> tabs, IReadOnlyList<ValidationGroup> groups,
            string staleNotice, string safeRepairButtonText, int safeRepairCount, string progressText, string progressRatioText,
            float progressRatio, string summaryRightText, IReadOnlyList<string> summaryParts, int findingCount)
        {
            BodyState = bodyState;
            Tabs = tabs;
            Groups = groups;
            StaleNotice = staleNotice;
            SafeRepairButtonText = safeRepairButtonText;
            SafeRepairCount = safeRepairCount;
            ProgressText = progressText;
            ProgressRatioText = progressRatioText;
            ProgressRatio = progressRatio;
            SummaryRightText = summaryRightText;
            SummaryParts = summaryParts;
            FindingCount = findingCount;
        }

        internal ValidationBodyState BodyState { get; }
        internal IReadOnlyList<ValidationTab> Tabs { get; }
        internal IReadOnlyList<ValidationGroup> Groups { get; }

        /// <summary>Câu HelpBox "kết quả cũ"; "" khi kết quả còn mới.</summary>
        internal string StaleNotice { get; }

        internal string SafeRepairButtonText { get; }

        /// <summary>Số lệnh sửa an toàn đang chờ — nút chỉ áp ngay khi bằng 1 ở W4 (I-7).</summary>
        internal int SafeRepairCount { get; }

        internal string ProgressText { get; }

        /// <summary>Nhãn "7 / 12" cạnh thanh tiến trình ([SD2 §2.8]) — thanh trơn không đọc được bằng số, chỉ bằng bề rộng.</summary>
        internal string ProgressRatioText { get; }

        internal float ProgressRatio { get; }

        /// <summary>Câu phải của dải summary: "Kiểm lúc 08:46:58 UTC · 12 luật · 1 lỗi sửa nhanh an toàn được".</summary>
        internal string SummaryRightText { get; }

        /// <summary>Năm cặp dấu + số của dải summary, theo thứ tự hậu quả rồi tới "chưa kiểm" và "đã qua".</summary>
        internal IReadOnlyList<string> SummaryParts { get; }

        /// <summary>Số phát hiện chưa bỏ qua của cả báo cáo (không theo bộ lọc) — dùng cho câu "5 phát hiện" của status bar.</summary>
        internal int FindingCount { get; }

        /// <summary>Bản không ngữ cảnh — chỉ cho nơi chưa có tài liệu (test lọc thuần). Màn luôn dùng overload dưới.</summary>
        internal static ValidationViewModel Build(LiveOpsHubCheckState check, ValidationFilter filter, LiveOpsHubFormat format)
        {
            return Build(check, filter, format, ValidationTextContext.Empty);
        }

        internal static ValidationViewModel Build(LiveOpsHubCheckState check, ValidationFilter filter, LiveOpsHubFormat format,
            ValidationTextContext context)
        {
            if (check == null) throw new ArgumentNullException(nameof(check));
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (context == null) throw new ArgumentNullException(nameof(context));

            LiveEventCalendarCheckReport report = check.LastReport;
            if (check.IsRunning) return Running(check, format);
            if (report == null) return NeverChecked(check, format);

            LiveEventCalendarCheckSummary summary = report.Summary;
            int notMeasuredCount = summary.NotMeasuredRuleCount;
            int findingCount = summary.DroppedCount + summary.ProgressLostCount + summary.ShouldReviewCount;

            var groups = new List<ValidationGroup>();
            groups.Add(FindingGroup(ValidationGroupKind.Dropped, LiveEventCalendarConsequence.Dropped, report, filter, format, context));
            groups.Add(FindingGroup(ValidationGroupKind.ProgressLost, LiveEventCalendarConsequence.ProgressLost, report, filter, format, context));
            groups.Add(FindingGroup(ValidationGroupKind.ShouldReview, LiveEventCalendarConsequence.ShouldReview, report, filter, format, context));
            groups.Add(NotMeasuredGroup(report, filter, format, context));
            groups.Add(PassedGroup(report, format));
            groups.Add(IgnoredGroup(report, format));

            string staleNotice = StaleNoticeOf(check, format);
            ValidationBodyState bodyState = BodyStateOf(check, summary, staleNotice.Length > 0);
            int safeRepairCount = SafeRepairCountOf(report);

            return new ValidationViewModel(bodyState, TabsOf(summary, findingCount, notMeasuredCount), groups, staleNotice,
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationSafeRepairButtonFormat, safeRepairCount),
                safeRepairCount, string.Empty, string.Empty, 0f, SummaryRightOf(report, safeRepairCount),
                SummaryPartsOf(summary, notMeasuredCount), findingCount);
        }

        private static ValidationViewModel Running(LiveOpsHubCheckState check, LiveOpsHubFormat format)
        {
            int completed = check.CompletedRuleCount;
            int total = check.RuleCount;
            string progressText = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ValidationProgressFormat), completed, total);
            string progressRatioText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationProgressRatioFormat, completed, total);
            return new ValidationViewModel(ValidationBodyState.Running, Array.Empty<ValidationTab>(), Array.Empty<ValidationGroup>(),
                string.Empty, string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationSafeRepairButtonFormat, 0), 0,
                progressText, progressRatioText, total > 0 ? (float)completed / total : 0f, string.Empty, Array.Empty<string>(), 0);
        }

        private static ValidationViewModel NeverChecked(LiveOpsHubCheckState check, LiveOpsHubFormat format)
        {
            // Kiểm bị cắt giữa chừng vì domain reload (R-25) vẫn là "chưa có kết quả", nhưng câu phải nói đúng lý do.
            string notice = check.StaleReason == LiveOpsHubCheckStaleReason.InterruptedByReload
                ? LiveOpsHubStrings.ValidationStaleInterruptedNotice
                : string.Empty;
            return new ValidationViewModel(ValidationBodyState.NeverChecked, Array.Empty<ValidationTab>(), Array.Empty<ValidationGroup>(),
                notice, string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationSafeRepairButtonFormat, 0), 0,
                string.Empty, string.Empty, 0f, string.Empty, Array.Empty<string>(), 0);
        }

        private static ValidationBodyState BodyStateOf(LiveOpsHubCheckState check, LiveEventCalendarCheckSummary summary, bool isStale)
        {
            if (isStale) return ValidationBodyState.Stale;
            if (summary.DroppedCount > 0 || summary.ProgressLostCount > 0) return ValidationBodyState.Default;
            return summary.ShouldReviewCount > 0 ? ValidationBodyState.ShouldReviewOnly : ValidationBodyState.NoErrors;
        }

        private static string StaleNoticeOf(LiveOpsHubCheckState check, LiveOpsHubFormat format)
        {
            if (!check.IsStale || check.LastReport == null) return string.Empty;
            string checkedAt = ClockWithSeconds(check.LastReport.CheckedAtUtc);
            if (check.StaleReason == LiveOpsHubCheckStaleReason.MilestonePassed && check.PassedMilestoneUtc.HasValue)
            {
                // Mốc là một điểm trên lịch (ngày + giờ), lần kiểm là một thời điểm trong phiên (giây) — hai thứ khác nhau nên
                // hai cách viết khác nhau, đúng câu thiết kế "Đã qua mốc 14/9 00:00 UTC sau lần kiểm 08:46:30".
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationStaleMilestoneFormat,
                    format.ShortDateTimeUtc(check.PassedMilestoneUtc.Value), checkedAt);
            }
            if (check.StaleReason == LiveOpsHubCheckStaleReason.InterruptedByReload) return LiveOpsHubStrings.ValidationStaleInterruptedNotice;
            DateTime editedUtc = check.CalendarEditedUtc ?? check.LastReport.CheckedAtUtc;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationStaleEditedFormat,
                ClockWithSecondsUtc(editedUtc), checkedAt);
        }

        /// <summary>
        /// "08:46:50 UTC" — câu "kết quả cũ" và câu phải của dải summary BẮT BUỘC có giây ([SD2 §2.1] "Kiểm lúc 08:46:58 UTC",
        /// [SD2 §2.8] "đã đổi lúc 08:46:50 UTC, sau lần kiểm 08:46:30"). Hai mốc của một câu thường cách nhau vài chục giây:
        /// in bằng <see cref="LiveOpsHubFormat.ShortDateTimeUtc"/> (chỉ tới phút) sẽ ra hai mốc y hệt nhau và câu mất nghĩa.
        /// Cùng khuôn giờ mà status bar dùng; hằng định dạng nằm ngay đây nên giây không rò sang chỗ khác của màn.
        /// </summary>
        private static string ClockWithSecondsUtc(DateTime utc)
        {
            return ClockWithSeconds(utc) + " " + LiveOpsHubStrings.UtcLabel;
        }

        /// <summary>"08:46:30" — bản không hậu tố, dùng cho vế thứ hai của câu (ngữ cảnh UTC đã nêu ở vế đầu).</summary>
        private static string ClockWithSeconds(DateTime utc)
        {
            return utc.ToString(ClockWithSecondsPattern, CultureInfo.InvariantCulture);
        }

        private const string ClockWithSecondsPattern = "HH:mm:ss";

        private static IReadOnlyList<ValidationTab> TabsOf(LiveEventCalendarCheckSummary summary, int findingCount, int notMeasuredCount)
        {
            return new[]
            {
                new ValidationTab(LiveOpsHubNavigation.FilterNone,
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationTabAllFormat, findingCount + notMeasuredCount),
                    findingCount + notMeasuredCount),
                new ValidationTab(LiveOpsHubNavigation.FilterDropped,
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationTabDroppedFormat, summary.DroppedCount), summary.DroppedCount),
                new ValidationTab(LiveOpsHubNavigation.FilterProgressLost,
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationTabProgressLostFormat, summary.ProgressLostCount),
                    summary.ProgressLostCount),
                new ValidationTab(LiveOpsHubNavigation.FilterShouldReview,
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationTabShouldReviewFormat, summary.ShouldReviewCount),
                    summary.ShouldReviewCount),
                new ValidationTab(LiveOpsHubNavigation.FilterNotMeasured,
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationTabNotMeasuredFormat, notMeasuredCount), notMeasuredCount),
            };
        }

        private static IReadOnlyList<string> SummaryPartsOf(LiveEventCalendarCheckSummary summary, int notMeasuredCount)
        {
            return new[]
            {
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationSummaryDroppedFormat, summary.DroppedCount),
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationSummaryProgressLostFormat, summary.ProgressLostCount),
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationSummaryShouldReviewFormat, summary.ShouldReviewCount),
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationSummaryNotMeasuredFormat, notMeasuredCount),
                LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ValidationSummaryPassedFormat), summary.PassedRuleCount),
            };
        }

        private static string SummaryRightOf(LiveEventCalendarCheckReport report, int safeRepairCount)
        {
            return LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ValidationSummaryRightFormat),
                ClockWithSecondsUtc(report.CheckedAtUtc), report.RuleResults.Count, safeRepairCount);
        }

        private static int SafeRepairCountOf(LiveEventCalendarCheckReport report)
        {
            int count = 0;
            for (int index = 0; index < report.Findings.Count; index++)
            {
                LiveEventCalendarFinding finding = report.Findings[index];
                if (!finding.IsIgnored && finding.RepairKind == LiveEventCalendarRepairKind.SafeRepair) count++;
            }
            return count;
        }

        private static ValidationGroup FindingGroup(ValidationGroupKind kind, LiveEventCalendarConsequence consequence,
            LiveEventCalendarCheckReport report, ValidationFilter filter, LiveOpsHubFormat format, ValidationTextContext context)
        {
            var rows = new List<ValidationRow>();
            int totalCount = 0;
            int safeRepairCount = 0;
            for (int index = 0; index < report.Findings.Count; index++)
            {
                LiveEventCalendarFinding finding = report.Findings[index];
                if (finding.IsIgnored || finding.Consequence != consequence) continue;
                totalCount++;
                if (finding.RepairKind == LiveEventCalendarRepairKind.SafeRepair) safeRepairCount++;
                if (filter.Accepts(finding, context.EventTypeOf(finding))) rows.Add(RowOf(finding, format, context));
            }

            string meta = safeRepairCount > 0
                ? LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ValidationGroupMetaSafeRepairFormat), totalCount, safeRepairCount, totalCount)
                : LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ValidationGroupMetaFindingsFormat), totalCount);
            return new ValidationGroup(kind, TitleOf(kind), BlurbOf(kind), meta, LiveOpsHubFindingRouting.StateOf(consequence), rows,
                totalCount, string.Empty);
        }

        private static ValidationGroup NotMeasuredGroup(LiveEventCalendarCheckReport report, ValidationFilter filter, LiveOpsHubFormat format,
            ValidationTextContext context)
        {
            var rows = new List<ValidationRow>();
            int totalCount = 0;
            for (int index = 0; index < report.RuleResults.Count; index++)
            {
                LiveEventCalendarRuleResult result = report.RuleResults[index];
                if (!LiveOpsHubFindingRouting.IsNotMeasured(result.Outcome)) continue;
                totalCount++;
                if (filter.AcceptsRuleResult(result)) rows.Add(RuleResultRowOf(result, format, context));
            }

            string meta = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ValidationGroupMetaNotMeasuredFormat), totalCount);
            return new ValidationGroup(ValidationGroupKind.NotMeasured, LiveOpsHubStrings.ValidationGroupNotMeasuredTitle, string.Empty, meta,
                HealthState.NotMeasured, rows, totalCount, string.Empty);
        }

        private static ValidationGroup PassedGroup(LiveEventCalendarCheckReport report, LiveOpsHubFormat format)
        {
            var ruleIds = new List<string>();
            for (int index = 0; index < report.RuleResults.Count; index++)
            {
                LiveEventCalendarRuleResult result = report.RuleResults[index];
                if (result.Outcome == LiveEventCalendarRuleOutcome.Passed) ruleIds.Add(result.RuleId);
                else if (result.Outcome == LiveEventCalendarRuleOutcome.NotApplicable)
                {
                    // Luật không áp dụng nằm cùng card "đã qua" nhưng phải nói rõ lý do, nếu không người đọc tưởng nó đã chạy.
                    ruleIds.Add(result.RuleId + " " + LiveOpsFindingText.RuleResultNotApplicableLabel(result));
                }
            }

            string title = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ValidationGroupPassedTitleFormat), report.Summary.PassedRuleCount);
            return new ValidationGroup(ValidationGroupKind.Passed, title, string.Empty, string.Empty, HealthState.Ok, NoRows,
                report.Summary.PassedRuleCount, string.Join(RuleIdSeparator, ruleIds.ToArray()));
        }

        /// <summary>
        /// Nhóm "Đã bỏ qua (n) ▸" (V-15, [SD2 §2.7] ô 6): mở ra là một hàng cho MỖI mục — meta "luật · đích · khoảng", ghi chú
        /// rút gọn và nút nhỏ "Bỏ bỏ qua". Nút nhỏ có vì menu chuột phải không chụp được (S-24): một tính năng chỉ tới được
        /// bằng chuột phải là một tính năng không ai chứng minh được là còn sống.
        /// </summary>
        private static ValidationGroup IgnoredGroup(LiveEventCalendarCheckReport report, LiveOpsHubFormat format)
        {
            var rows = new List<ValidationRow>();
            var parts = new List<string>();
            for (int index = 0; index < report.IgnoredFindings.Count; index++)
            {
                LiveEventCalendarFinding finding = report.IgnoredFindings[index];
                parts.Add(LiveOpsFindingText.PlainText(LiveOpsFindingText.RuleIdLine(finding)));
                rows.Add(IgnoredRowOf(finding, format));
            }

            string title = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationGroupIgnoredTitleFormat, report.IgnoredFindings.Count);
            return new ValidationGroup(ValidationGroupKind.Ignored, title, string.Empty, string.Empty, HealthState.Ok, rows,
                report.IgnoredFindings.Count, string.Join(RuleIdSeparator, parts.ToArray()));
        }

        /// <summary>
        /// Một hàng đã bỏ qua. Khoảng do <see cref="LiveOpsFindingText.IgnoredWarningRangeText"/> viết (V-21 CC-VALB-4): ghi chú
        /// hẹn giờ đọc "hẹn tới 14/9 00:00", không "14/9 → mọi" — với người đọc đó là một HẠN, không phải khoảng bị ẩn. Hẹn giờ
        /// được gắn TAG mang đúng chữ đó, và khi có tag thì meta BỎ vế khoảng: hàng chỉ nói một hạn một lần (Q-W5-4, user chốt
        /// 17/9/2026). Meta dựng MỘT lần theo nhánh có-tag / không-tag, không nối rồi cắt.
        /// </summary>
        private static ValidationRow IgnoredRowOf(LiveEventCalendarFinding finding, LiveOpsHubFormat format)
        {
            IgnoredCalendarWarning warning = finding.IgnoredBy;
            string rangeText = LiveOpsFindingText.IgnoredWarningRangeText(warning, format);
            string ruleText = LiveOpsFindingText.NoParse(warning.RuleId);
            string targetText = LiveOpsFindingText.NoParse(warning.TargetId);
            string meta = warning.IsReminder
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationDepthIgnoredMetaWithTagFormat,
                    ruleText, targetText)
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationDepthIgnoredMetaFormat,
                    ruleText, targetText, rangeText);
            string note = IgnoredNoteShortText(warning);
            return new ValidationRow(finding, null, HealthState.Ok, note, meta, LiveOpsFindingText.RuleIdLine(finding),
                ValidationRowAction.Unignore, LiveOpsHubStrings.ValidationDepthUnignoreButton, string.Empty,
                string.Empty, string.Empty, warning.IsReminder ? rangeText : string.Empty, warning);
        }

        /// <summary>
        /// Ghi chú ĐỦ của một cảnh báo đã bỏ qua — chữ của hover card ghim "Xem ghi chú". Không rút gọn: thẻ ghim tồn tại
        /// đúng để đọc trọn ghi chú mà hàng không đủ chỗ.
        /// </summary>
        internal static string IgnoredNoteFullText(IgnoredCalendarWarning warning)
        {
            if (warning == null) throw new ArgumentNullException(nameof(warning));
            return warning.Note.Length > 0
                ? LiveOpsFindingText.NoParse(warning.Note)
                : LiveOpsHubStrings.ValidationDepthIgnoredNoteEmpty;
        }

        /// <summary>
        /// Ghi chú RÚT GỌN của hàng (mục 7.5 "ghi chú rút gọn"). Vì sao cắt bằng KÝ TỰ chứ không chỉ bằng USS ellipsis: chữ
        /// của hàng phải khác chữ của thẻ ghim thì "Xem ghi chú" mới còn lý do tồn tại, và một test đọc được chữ mới khoá
        /// được luật đó — ellipsis của USS chỉ đổi cách vẽ, không đổi chữ.
        /// </summary>
        internal static string IgnoredNoteShortText(IgnoredCalendarWarning warning)
        {
            if (warning == null) throw new ArgumentNullException(nameof(warning));
            if (warning.Note.Length == 0) return LiveOpsHubStrings.ValidationDepthIgnoredNoteEmpty;
            if (warning.Note.Length <= IgnoredNoteMaximumCharacters) return LiveOpsFindingText.NoParse(warning.Note);
            string head = warning.Note.Substring(0, IgnoredNoteMaximumCharacters).TrimEnd();
            return LiveOpsFindingText.NoParse(head) + LiveOpsHubStrings.ValidationDepthIgnoredNoteEllipsis;
        }

        /// <summary>
        /// Số ký tự ghi chú mà một hàng giữ lại. 72 ký tự là bề ngang của hàng ở cột card mở hết (756px, chữ 10px) — dài hơn
        /// thì hàng đẩy nút "Bỏ bỏ qua" ra khỏi tầm mắt.
        /// </summary>
        private const int IgnoredNoteMaximumCharacters = 72;

        private const string RuleIdSeparator = " · ";

        /// <summary>
        /// Ánh xạ MỘT phát hiện thành hàng — lối vào cho test câu chữ và cho ca không dựng nổi bằng validator thật (vd luật 9
        /// không có lệnh sửa nào, CC-VALB-3). Cùng hàm mà <see cref="Build(LiveOpsHubCheckState, ValidationFilter, LiveOpsHubFormat, ValidationTextContext)"/> dùng.
        /// </summary>
        internal static ValidationRow RowFor(LiveEventCalendarFinding finding, LiveOpsHubFormat format, ValidationTextContext context)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return RowOf(finding, format, context);
        }

        private static ValidationRow RowOf(LiveEventCalendarFinding finding, LiveOpsHubFormat format, ValidationTextContext context)
        {
            ValidationRowAction action = ActionOf(finding);
            string actionText = action == ValidationRowAction.None ? string.Empty : LiveOpsFindingText.PrimaryButtonText(finding);
            string linkText = LiveOpsFindingText.LinkText(finding);
            if (action == ValidationRowAction.None && linkText.Length == 0)
            {
                // Hàng luật 9 không có lệnh sửa nào (CC-VALB-3): chữ nút chính LÀ link — không có động từ nào để bấm.
                linkText = LiveOpsFindingText.PrimaryButtonText(finding);
            }

            // (V-22 CC-FT-1) Ghi chú hẹn giờ đã tới hạn: phát hiện quay lại danh sách kèm tag, nếu không nó trông y hệt một
            // phát hiện mới và người đọc không nhớ chính mình đã hẹn nó tháng trước.
            string tagText = finding.DueReminder != null ? LiveOpsFindingText.DueReminderTag : string.Empty;

            return new ValidationRow(finding, null, LiveOpsHubFindingRouting.StateOf(finding.Consequence),
                LiveOpsFindingText.Headline(finding, format, context.LatestStamp),
                LiveOpsFindingText.Meta(finding, format, context.NowUtc),
                LiveOpsFindingText.RuleIdLine(finding), action, actionText,
                LiveOpsFindingText.PrimaryButtonTooltip(finding, format, context.CalendarAssetName), linkText,
                LiveOpsFindingText.ManualFixSentence(finding, format), tagText, null);
        }

        /// <summary>Động từ của hàng — bốn động từ của [SD2 §2.4] ánh xạ thẳng từ <c>RepairKind</c> mà luật đã chọn.</summary>
        private static ValidationRowAction ActionOf(LiveEventCalendarFinding finding)
        {
            switch (finding.RepairKind)
            {
                case LiveEventCalendarRepairKind.SafeRepair: return ValidationRowAction.SafeRepair;
                case LiveEventCalendarRepairKind.Proposal: return ValidationRowAction.Proposal;
                case LiveEventCalendarRepairKind.Decision: return ValidationRowAction.Decision;
                case LiveEventCalendarRepairKind.Ignorable: return ValidationRowAction.IgnoreWarning;
            }

            if (string.Equals(finding.RuleId, LiveEventCalendarRuleIds.RemoteSnapshotDrift, StringComparison.Ordinal) &&
                string.Equals(finding.DetailCode, LiveEventCalendarDetailCodes.RemoteDiffers, StringComparison.Ordinal))
            {
                return ValidationRowAction.ViewDiff;
            }
            if (string.Equals(finding.RuleId, LiveEventCalendarRuleIds.UnknownEventType, StringComparison.Ordinal)) return ValidationRowAction.DeclareType;
            return ValidationRowAction.None;
        }

        private static ValidationRow RuleResultRowOf(LiveEventCalendarRuleResult result, LiveOpsHubFormat format, ValidationTextContext context)
        {
            string actionText = LiveOpsFindingText.RuleResultButtonText(result);
            ValidationRowAction action = ValidationRowAction.None;
            if (actionText.Length > 0)
            {
                action = result.Outcome == LiveEventCalendarRuleOutcome.Failed ? ValidationRowAction.CopyError : ValidationRowAction.PasteRunningJson;
            }

            return new ValidationRow(null, result, HealthState.NotMeasured, LiveOpsFindingText.RuleResultHeadline(result),
                LiveOpsFindingText.RuleResultMeta(result, format, context.LatestStamp), LiveOpsFindingText.NoParse(result.RuleId),
                action, actionText, string.Empty, string.Empty, string.Empty);
        }

        private static string TitleOf(ValidationGroupKind kind)
        {
            switch (kind)
            {
                case ValidationGroupKind.Dropped: return LiveOpsHubStrings.ValidationGroupDroppedTitle;
                case ValidationGroupKind.ProgressLost: return LiveOpsHubStrings.ValidationGroupProgressLostTitle;
                default: return LiveOpsHubStrings.ValidationGroupShouldReviewTitle;
            }
        }

        private static string BlurbOf(ValidationGroupKind kind)
        {
            switch (kind)
            {
                case ValidationGroupKind.Dropped: return LiveOpsHubStrings.ValidationGroupDroppedBlurb;
                case ValidationGroupKind.ProgressLost: return LiveOpsHubStrings.ValidationGroupProgressLostBlurb;
                // "Nên xem" không có blurb trong thiết kế ([SD2 §1.1]) — thêm câu ở đây là tự viết chữ mới.
                default: return string.Empty;
            }
        }
    }
}
