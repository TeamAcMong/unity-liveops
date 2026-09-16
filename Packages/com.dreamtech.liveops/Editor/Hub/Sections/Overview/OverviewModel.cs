using System;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Unity;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Việc của nút trên một hàng "Việc cần làm" — hàng không phải lúc nào cũng dẫn sang màn khác (Kiểm lại, Dán JSON).</summary>
    internal enum OverviewRowAction
    {
        None = 0,
        Navigate = 1,
        StartCheck = 2,
        PasteRunningJson = 3,
    }

    /// <summary>Loại dòng của bảng "7 ngày tới": một đợt mở · một đợt khép · một đợt bị game bỏ · nhiều đợt lặp gom một dòng.</summary>
    internal enum OverviewUpcomingKind
    {
        Open = 0,
        Close = 1,
        Dropped = 2,
        Grouped = 3,
    }

    /// <summary>Một ô metric của hàng đầu [SD1 §1.1]. <see cref="Value"/> null = vòng rỗng (không đo được), KHÔNG in 0 hay "—".</summary>
    internal sealed class OverviewMetric
    {
        internal OverviewMetric(string caption, string value, string unit, string foot, string tooltip, HealthState? valueState, bool isChecking)
        {
            Caption = caption ?? string.Empty;
            Value = value;
            Unit = unit ?? string.Empty;
            Foot = foot ?? string.Empty;
            Tooltip = tooltip ?? string.Empty;
            ValueState = valueState;
            IsChecking = isChecking;
        }

        public string Caption { get; }

        /// <summary>null = vòng rỗng kèm một câu lý do ở <see cref="Foot"/> ([FD §2.4]).</summary>
        public string Value { get; }

        public string Unit { get; }
        public string Foot { get; }
        public string Tooltip { get; }

        /// <summary>null = màu chữ thường; có giá trị = họ chữ theo trạng thái (CẦN XỬ LÝ đỏ khi còn đợt bị bỏ).</summary>
        public HealthState? ValueState { get; }

        /// <summary>Đang chạy lần kiểm: view gắn spinner 16px cạnh dòng phụ.</summary>
        public bool IsChecking { get; }
    }

    /// <summary>Một hàng "Việc cần làm trước khi đăng" [SD1 §1.1]: mức → câu → "ở đâu" → cột cổng 96px → nút 150px nêu đích.</summary>
    internal sealed class OverviewNeedsActionRow
    {
        internal OverviewNeedsActionRow(HealthState state, string title, string detail, string detailTooltip, bool blocksCopy,
            string buttonText, OverviewRowAction action, LiveOpsHubNavigation navigation, bool isButtonEnabled, string disabledReason)
        {
            State = state;
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
            DetailTooltip = detailTooltip ?? string.Empty;
            BlocksCopy = blocksCopy;
            ButtonText = buttonText ?? string.Empty;
            Action = action;
            Navigation = navigation;
            IsButtonEnabled = isButtonEnabled;
            DisabledReason = disabledReason ?? string.Empty;
        }

        public HealthState State { get; }
        public string Title { get; }
        public string Detail { get; }

        /// <summary>
        /// Câu meta đầy đủ của phát hiện (<see cref="LiveOpsFindingText.Meta"/> với đồng hồ — V-22 CC-FT-1); "" khi hàng không
        /// sinh từ một phát hiện. Dòng "ở đâu" chỉ đủ chỗ cho tên màn, còn khoảng giờ và id liên quan nằm ở tooltip.
        /// </summary>
        public string DetailTooltip { get; }

        /// <summary>Trạng thái CỔNG xuất, không phải mức của phát hiện — cột 96px có dấu Blocked riêng.</summary>
        public bool BlocksCopy { get; }

        public string ButtonText { get; }
        public OverviewRowAction Action { get; }

        /// <summary>null khi <see cref="Action"/> không phải <see cref="OverviewRowAction.Navigate"/>.</summary>
        public LiveOpsHubNavigation Navigation { get; }

        public bool IsButtonEnabled { get; }

        /// <summary>Bắt buộc khi nút khoá — lý do in thành chữ cạnh nút là đường chính (SPIKE-B SP-3).</summary>
        public string DisabledReason { get; }
    }

    /// <summary>Một nút tầng của card "Đường đi của lịch" (4 tầng P1 — PD-1: tầng CHẠY không vẽ).</summary>
    internal sealed class OverviewFlowNode
    {
        internal OverviewFlowNode(PipelineStage stage, string caption, HealthState state, string note, bool hasConnector,
            bool isConnectorDead, string tooltip)
        {
            Stage = stage;
            Caption = caption ?? string.Empty;
            State = state;
            Note = note ?? string.Empty;
            HasConnector = hasConnector;
            IsConnectorDead = isConnectorDead;
            Tooltip = tooltip ?? string.Empty;
        }

        public PipelineStage Stage { get; }
        public string Caption { get; }
        public HealthState State { get; }
        public string Note { get; }

        /// <summary>Tầng cuối không có đường nối (P1 dừng ở XUẤT).</summary>
        public bool HasConnector { get; }

        public bool IsConnectorDead { get; }
        public string Tooltip { get; }
    }

    /// <summary>Một dòng bảng "7 ngày tới" [SD1 §1.1]: cột Lúc (UTC tuyệt đối + dòng phụ tương đối) | Đợt | Loại | Sự kiện | Ghi chú.</summary>
    internal sealed class OverviewUpcomingRow
    {
        internal OverviewUpcomingRow(DateTime sortUtc, string timeText, string relativeText, string eventIdText, string eventType,
            int colorSlot, OverviewUpcomingKind kind, string kindText, string noteText, bool isDraftOnly, HealthState? noteState)
        {
            SortUtc = sortUtc;
            TimeText = timeText ?? string.Empty;
            RelativeText = relativeText ?? string.Empty;
            EventIdText = eventIdText ?? string.Empty;
            EventType = eventType ?? string.Empty;
            ColorSlot = colorSlot;
            Kind = kind;
            KindText = kindText ?? string.Empty;
            NoteText = noteText ?? string.Empty;
            IsDraftOnly = isDraftOnly;
            NoteState = noteState;
        }

        public DateTime SortUtc { get; }
        public string TimeText { get; }
        public string RelativeText { get; }
        public string EventIdText { get; }
        public string EventType { get; }
        public int ColorSlot { get; }
        public OverviewUpcomingKind Kind { get; }
        public string KindText { get; }
        public string NoteText { get; }

        /// <summary>Đợt chỉ có trong nháp (chưa có ở bản đã đăng) — tag "chỉ trong nháp" cao 15.</summary>
        public bool IsDraftOnly { get; }

        /// <summary>Họ chữ của ghi chú (Warning cho "đổi id khi đang chạy"); null = chữ thường.</summary>
        public HealthState? NoteState { get; }
    }

    /// <summary>
    /// Model thuần của màn Tổng quan (7.1): metric, hàng việc cần làm (xấu nhất trước), 4 nút tầng, bảng 7 ngày tới. Không chạm
    /// UIElements, không chạm <c>EditorApplication</c> — test chạy ở <c>[Category("LiveOpsHub.Logic")]</c>.
    /// <para>
    /// Hai luật xuyên suốt: (1) câu của phát hiện KHÔNG viết ở đây mà lấy từ <see cref="LiveOpsFindingText"/> bằng overload có ngữ
    /// cảnh (V-22 CC-FT-1: <c>Headline(…, latestStamp)</c>, <c>Meta(…, nowUtc)</c>); (2) dòng "chặn Copy JSON" lấy từ
    /// <see cref="ExportGateState"/> của G-EXPORTGATE (V-9) — Tổng quan không tự kết luận cổng, nếu không hai chỗ sẽ lệch nhau.
    /// </para>
    /// </summary>
    internal sealed class OverviewModel
    {
        /// <summary>Thân mặc định (Hình 4/5).</summary>
        internal const int BodyStateDefault = 0;

        /// <summary>(a) chưa có asset — empty ba bước thay CẢ thân [SD1 §1.2].</summary>
        internal const int BodyStateNoAsset = 1;

        /// <summary>(c) không còn việc chặn — card việc cần làm thành empty, vẫn liệt kê hàng NotMeasured [SD1 §1.4].</summary>
        internal const int BodyStateNoBlockers = 2;

        /// <summary>Chưa kiểm lần nào — metric CẦN XỬ LÝ là vòng rỗng, không phải số 0.</summary>
        internal const int BodyStateNeverChecked = 3;

        /// <summary>Đang kiểm — metric CẦN XỬ LÝ có spinner + "Đang kiểm 7/12 luật…".</summary>
        internal const int BodyStateChecking = 4;

        /// <summary>Khung "7 ngày tới" [SD1 §1.1].</summary>
        internal const int UpcomingDays = 7;

        /// <summary>Số đợt lặp trong khung từ đây trở lên thì gom một dòng (sky-race 7 đợt); dưới đó vẫn liệt kê từng dòng.</summary>
        internal const int GroupRecurringThreshold = 3;

        private readonly List<OverviewUpcomingRow> _draftUpcomingRows;
        private readonly List<OverviewUpcomingRow> _publishedUpcomingRows;

        private OverviewModel(int bodyState, List<OverviewMetric> metrics, List<OverviewNeedsActionRow> needsActionRows,
            List<OverviewFlowNode> flowNodes, List<OverviewUpcomingRow> draftUpcomingRows,
            List<OverviewUpcomingRow> publishedUpcomingRows, string multipleAssetsNotice, bool hasPublishedStamp,
            int notMeasuredRuleCount, DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            BodyState = bodyState;
            Metrics = metrics;
            NeedsActionRows = needsActionRows;
            FlowNodes = flowNodes;
            _draftUpcomingRows = draftUpcomingRows;
            _publishedUpcomingRows = publishedUpcomingRows;
            MultipleAssetsNotice = multipleAssetsNotice ?? string.Empty;
            HasPublishedStamp = hasPublishedStamp;
            NotMeasuredRuleCount = notMeasuredRuleCount;
            RangeStartUtc = rangeStartUtc;
            RangeEndUtc = rangeEndUtc;
        }

        public int BodyState { get; }
        public IReadOnlyList<OverviewMetric> Metrics { get; }

        /// <summary>Xấu nhất trước: Bị bỏ → Mất tiến độ → Nên xem → chưa đo được.</summary>
        public IReadOnlyList<OverviewNeedsActionRow> NeedsActionRows { get; }

        public IReadOnlyList<OverviewFlowNode> FlowNodes { get; }

        /// <summary>"" khi chỉ có một asset; HelpBox + menu "Đổi…" là G-SHELLPOLISH (mục 12 I-9).</summary>
        public string MultipleAssetsNotice { get; }

        /// <summary>Tab "Bản đã đăng" chỉ bật khi có dấu — không có dấu thì không có bản nào để so.</summary>
        public bool HasPublishedStamp { get; }

        /// <summary>Số luật chưa kiểm được — câu empty của (c) nêu đúng số này.</summary>
        public int NotMeasuredRuleCount { get; }

        public DateTime RangeStartUtc { get; }
        public DateTime RangeEndUtc { get; }

        /// <param name="publishedSource">true = bảng đọc bản đã đăng thay vì nháp (tab "Bản đã đăng").</param>
        public IReadOnlyList<OverviewUpcomingRow> UpcomingRows(bool publishedSource)
        {
            return publishedSource ? _publishedUpcomingRows : _draftUpcomingRows;
        }

        /// <param name="sectionHealths">Health của MỌI màn theo đúng thứ tự registry (<see cref="LiveOpsHubSections.Create(LiveOpsHubServices)"/>).</param>
        /// <param name="exportGate">Cổng xuất đã tính sẵn (V-9) — nguồn duy nhất của dòng "chặn Copy JSON".</param>
        public static OverviewModel Build(LiveOpsHubCalendarSession session, IReadOnlyList<SectionHealth> sectionHealths,
            ExportGateState exportGate, bool canPasteRunningJson, bool canImportRunningJson, LiveOpsHubFormat format)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (format == null) throw new ArgumentNullException(nameof(format));
            // canImportRunningJson thuộc chữ ký mục 3 nhưng chỉ có nghĩa ở trạng thái (a): nút "Nhập JSON đang chạy…" nằm trong
            // empty do view dựng (nút hỏi thẳng port), không sinh hàng model nào. Giữ tham số để chữ ký mục 3 không đổi.
            DateTime nowUtc = session.Clock.UtcNow;
            DateTime rangeEndUtc = nowUtc.AddDays(UpcomingDays);
            bool hasAsset = session.Asset != null;
            LiveEventCalendarCheckReport report = session.Check.LastReport;
            PublishedCalendarStamp latestStamp = session.Document.LatestStamp;

            if (!hasAsset)
            {
                return new OverviewModel(BodyStateNoAsset, new List<OverviewMetric>(), new List<OverviewNeedsActionRow>(),
                    new List<OverviewFlowNode>(), new List<OverviewUpcomingRow>(), new List<OverviewUpcomingRow>(),
                    string.Empty, false, 0, nowUtc, rangeEndUtc);
            }

            int notMeasuredRuleCount = CountNotMeasuredRules(report, out int remoteNotMeasuredRuleCount);
            List<OverviewNeedsActionRow> rows = BuildNeedsActionRows(session, exportGate, canPasteRunningJson, format, nowUtc,
                report, latestStamp);
            List<OverviewMetric> metrics = BuildMetrics(session, format, nowUtc, report, latestStamp, notMeasuredRuleCount,
                remoteNotMeasuredRuleCount);
            List<OverviewFlowNode> flowNodes = BuildFlowNodes(sectionHealths);
            List<OverviewUpcomingRow> draftRows = BuildUpcomingRows(session, format, nowUtc, rangeEndUtc, false);
            List<OverviewUpcomingRow> publishedRows = BuildUpcomingRows(session, format, nowUtc, rangeEndUtc, true);

            int bodyState = ResolveBodyState(session, report, rows);
            string notice = session.CalendarAssetCount > 1
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewMultipleAssetsNoticeFormat,
                    session.CalendarAssetCount, session.AssetFileName)
                : string.Empty;

            return new OverviewModel(bodyState, metrics, rows, flowNodes, draftRows, publishedRows, notice, latestStamp != null,
                notMeasuredRuleCount, nowUtc, rangeEndUtc);
        }

        // ============================================================================================================ trạng thái thân

        private static int ResolveBodyState(LiveOpsHubCalendarSession session, LiveEventCalendarCheckReport report,
            List<OverviewNeedsActionRow> rows)
        {
            if (session.Check.IsRunning) return BodyStateChecking;
            if (report == null) return BodyStateNeverChecked;
            foreach (OverviewNeedsActionRow row in rows)
            {
                // "Việc chặn" = hàng có mức thật (Bị bỏ / Mất tiến độ / Nên xem). Hàng NotMeasured KHÔNG phải việc chặn nhưng
                // vẫn được liệt kê ở (c) — chưa kiểm không có nghĩa là ổn [SD1 §1.4].
                if (row.State != HealthState.NotMeasured) return BodyStateDefault;
            }
            return BodyStateNoBlockers;
        }

        // ============================================================================================================ metric

        private static List<OverviewMetric> BuildMetrics(LiveOpsHubCalendarSession session, LiveOpsHubFormat format, DateTime nowUtc,
            LiveEventCalendarCheckReport report, PublishedCalendarStamp latestStamp, int notMeasuredRuleCount,
            int remoteNotMeasuredRuleCount)
        {
            List<OverviewMetric> metrics = new List<OverviewMetric>();
            List<LiveEventInstance> running = CollectRunningInstances(session, nowUtc);
            string runningFoot = running.Count > 0 ? JoinEventIds(running) : LiveOpsHubStrings.OverviewMetricRunningEmptyFoot;
            // Dạng số ít/số nhiều chọn ở đây, không ở chỗ dựng view: tiếng Việt hai bản giống nhau nhưng tiếng Anh thì không
            // ("1 event" chứ không "1 events"), và ảnh mẫu tiếng Anh là thứ user đọc để duyệt bản dịch.
            string runningUnit = running.Count == 1
                ? LiveOpsHubStrings.OverviewMetricRunningUnitSingle
                : LiveOpsHubStrings.OverviewMetricRunningUnit;
            metrics.Add(new OverviewMetric(LiveOpsHubStrings.OverviewMetricRunningCaption, format.Integer(running.Count),
                runningUnit, runningFoot, runningFoot, null, false));

            metrics.Add(BuildNeedsActionMetric(session, format, report));

            string notCheckedValue = report == null ? null : format.Integer(notMeasuredRuleCount);
            string notCheckedFoot;
            if (report == null) notCheckedFoot = LiveOpsHubStrings.OverviewMetricNeverCheckedFoot;
            else if (notMeasuredRuleCount == 0) notCheckedFoot = LiveOpsHubStrings.OverviewMetricNotCheckedNoneFoot;
            else if (remoteNotMeasuredRuleCount == notMeasuredRuleCount)
            {
                notCheckedFoot = string.Format(CultureInfo.InvariantCulture,
                    notMeasuredRuleCount == 1
                        ? LiveOpsHubStrings.OverviewMetricNotCheckedSingleRemoteRuleFormat
                        : LiveOpsHubStrings.OverviewMetricNotCheckedRemoteRulesFormat, notMeasuredRuleCount);
            }
            else
            {
                notCheckedFoot = string.Format(CultureInfo.InvariantCulture,
                    notMeasuredRuleCount == 1
                        ? LiveOpsHubStrings.OverviewMetricNotCheckedSingleRuleFormat
                        : LiveOpsHubStrings.OverviewMetricNotCheckedRulesFormat, notMeasuredRuleCount);
            }
            metrics.Add(new OverviewMetric(LiveOpsHubStrings.OverviewMetricNotCheckedCaption, notCheckedValue, string.Empty,
                notCheckedFoot, notCheckedFoot, report == null ? (HealthState?)null : HealthState.NotMeasured, false));

            metrics.Add(BuildPublishedMetric(session, format, latestStamp));
            return metrics;
        }

        private static OverviewMetric BuildNeedsActionMetric(LiveOpsHubCalendarSession session, LiveOpsHubFormat format,
            LiveEventCalendarCheckReport report)
        {
            string caption = LiveOpsHubStrings.OverviewMetricNeedsActionCaption;
            string tooltip = LiveOpsHubStrings.OverviewMetricNeedsActionTooltip;
            if (session.Check.IsRunning)
            {
                string checkingFoot = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewMetricCheckingFormat,
                    session.Check.CompletedRuleCount, session.Check.RuleCount);
                return new OverviewMetric(caption, null, string.Empty, checkingFoot, tooltip, null, true);
            }
            if (report == null)
            {
                // Vòng rỗng + "chưa kiểm": không bao giờ in 0 thay cho "không đo được" (7.1).
                return new OverviewMetric(caption, null, string.Empty, LiveOpsHubStrings.OverviewMetricNeverCheckedFoot, tooltip,
                    null, false);
            }

            LiveOpsHubFindingCounts counts = LiveOpsHubFindingRouting.CountsFor(report, LiveOpsHubHealthTarget.Validation);
            int total = counts.Dropped + counts.ProgressLost + counts.ShouldReview;
            List<string> parts = new List<string>();
            if (counts.Dropped > 0)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewDroppedCountFormat, counts.Dropped));
            }
            if (counts.ProgressLost > 0)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewProgressLostCountFormat, counts.ProgressLost));
            }
            if (counts.ShouldReview > 0)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewShouldReviewCountFormat, counts.ShouldReview));
            }
            string foot = parts.Count > 0
                ? string.Join(LiveOpsHubStrings.OverviewPartSeparator, parts.ToArray())
                : LiveOpsHubStrings.OverviewMetricNeedsActionNoneFoot;
            HealthState? state = counts.Dropped > 0
                ? HealthState.Blocked
                : (total > 0 ? HealthState.Warning : (HealthState?)null);
            return new OverviewMetric(caption, format.Integer(total), string.Empty, foot, tooltip, state, false);
        }

        private static OverviewMetric BuildPublishedMetric(LiveOpsHubCalendarSession session, LiveOpsHubFormat format,
            PublishedCalendarStamp latestStamp)
        {
            string caption = LiveOpsHubStrings.OverviewMetricPublishedCaption;
            if (latestStamp == null || !latestStamp.TryGetPublishedUtc(out DateTime publishedUtc))
            {
                // Chưa đăng lần nào là "chưa biết", không phải "không có thay đổi" — vòng rỗng kèm lý do.
                return new OverviewMetric(caption, null, string.Empty, LiveOpsHubStrings.OverviewMetricPublishedNoStampFoot,
                    LiveOpsHubStrings.OverviewMetricPublishedNoStampFoot, null, false);
            }
            int changeCount = session.Publish.PublishedDiff.Changes.Count;
            string foot = changeCount > 0
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewMetricPublishedFootFormat,
                    latestStamp.ShortSha, changeCount)
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewMetricPublishedFootUnchangedFormat,
                    latestStamp.ShortSha);
            return new OverviewMetric(caption, format.ShortDateTime(publishedUtc), string.Empty, foot, foot, null, false);
        }

        private static List<LiveEventInstance> CollectRunningInstances(LiveOpsHubCalendarSession session, DateTime nowUtc)
        {
            List<LiveEventInstance> running = new List<LiveEventInstance>();
            // Khung một tick quanh "bây giờ": GetInstancesInRange trả đợt GIAO với khung, nên đúng các đợt đang chạy — không phải
            // quét ngược một khoảng đoán trước (đợt dài hơn khoảng đoán sẽ biến mất khỏi metric).
            foreach (LiveEventTypeDefinition type in session.Document.EventTypes)
            {
                IReadOnlyList<LiveEventInstance> instances = session.Compilation.GetInstancesInRange(type.TypeId, nowUtc, nowUtc.AddTicks(1));
                foreach (LiveEventInstance instance in instances)
                {
                    if (instance.PhaseAt(nowUtc) == LiveEventPhase.Active) running.Add(instance);
                }
            }
            running.Sort((left, right) => left.StartUtc.CompareTo(right.StartUtc));
            return running;
        }

        private static string JoinEventIds(List<LiveEventInstance> instances)
        {
            List<string> ids = new List<string>();
            foreach (LiveEventInstance instance in instances) ids.Add(instance.EventId);
            // Danh sách id nối bằng dấu phẩy ("weekly-pass-35, sky-race-251" — [SD1 §1.1]); " · " là dấu ngăn các MẢNH khác
            // loại của một dòng phụ, dùng lẫn thì mắt đọc thành hai cột.
            return string.Join(LiveOpsHubStrings.OverviewIdListSeparator, ids.ToArray());
        }

        private static int CountNotMeasuredRules(LiveEventCalendarCheckReport report, out int remoteRuleCount)
        {
            remoteRuleCount = 0;
            if (report == null) return 0;
            int count = 0;
            foreach (LiveEventCalendarRuleResult result in report.RuleResults)
            {
                if (!LiveOpsHubFindingRouting.IsNotMeasured(result.Outcome)) continue;
                count++;
                if (string.Equals(result.RuleId, LiveEventCalendarRuleIds.RemoteSnapshotDrift, StringComparison.Ordinal)) remoteRuleCount++;
            }
            return count;
        }

        // ============================================================================================================ việc cần làm

        private static List<OverviewNeedsActionRow> BuildNeedsActionRows(LiveOpsHubCalendarSession session, ExportGateState exportGate,
            bool canPasteRunningJson, LiveOpsHubFormat format, DateTime nowUtc, LiveEventCalendarCheckReport report,
            PublishedCalendarStamp latestStamp)
        {
            List<OverviewNeedsActionRow> rows = new List<OverviewNeedsActionRow>();
            if (report != null)
            {
                AddDroppedRow(rows, report, exportGate);
                AddProgressLostRows(rows, report, exportGate, format, nowUtc, latestStamp);
                AddShouldReviewRow(rows, report, format, nowUtc);
            }
            AddRemoteRow(rows, session, canPasteRunningJson);
            AddStaleCheckRow(rows, session, exportGate, format);
            AddNoStampRow(rows, latestStamp);
            return rows;
        }

        private static void AddDroppedRow(List<OverviewNeedsActionRow> rows, LiveEventCalendarCheckReport report,
            ExportGateState exportGate)
        {
            int dropped = 0;
            foreach (LiveEventCalendarFinding finding in report.Findings)
            {
                if (finding.IsIgnored || finding.IsAboutRemoteSnapshot) continue;
                if (finding.Consequence == LiveEventCalendarConsequence.Dropped) dropped++;
            }
            if (dropped == 0) return;
            string title = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewRowDroppedTitleFormat, dropped);
            rows.Add(new OverviewNeedsActionRow(HealthState.Blocked, title, LiveOpsHubStrings.OverviewRowDroppedDetail, string.Empty,
                BlocksCopy(exportGate, LiveEventCalendarConsequence.Dropped, false), LiveOpsHubStrings.OverviewOpenValidationButton,
                OverviewRowAction.Navigate,
                LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Validation).WithFilter(LiveOpsHubNavigation.FilterDropped), true,
                string.Empty));
        }

        private static void AddProgressLostRows(List<OverviewNeedsActionRow> rows, LiveEventCalendarCheckReport report,
            ExportGateState exportGate, LiveOpsHubFormat format, DateTime nowUtc, PublishedCalendarStamp latestStamp)
        {
            bool blocksCopy = BlocksCopy(exportGate, LiveEventCalendarConsequence.ProgressLost, false);
            foreach (LiveEventCalendarFinding finding in report.Findings)
            {
                if (finding.IsIgnored || finding.IsAboutRemoteSnapshot) continue;
                if (finding.Consequence != LiveEventCalendarConsequence.ProgressLost) continue;
                // (V-22 CC-FT-1) overload có ngữ cảnh ở chỗ có dữ liệu: dấu đã đăng cho headline, đồng hồ cho meta.
                string title = LiveOpsFindingText.Headline(finding, format, latestStamp);
                string detail = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewRowProgressLostDetailFormat,
                    LocationOf(finding));
                string detailTooltip = LiveOpsFindingText.PlainText(LiveOpsFindingText.Meta(finding, format, nowUtc));
                rows.Add(new OverviewNeedsActionRow(HealthState.Warning, title, detail, detailTooltip, blocksCopy,
                    ButtonTextOf(finding), OverviewRowAction.Navigate, NavigationOf(finding), true, string.Empty));
            }
        }

        private static void AddShouldReviewRow(List<OverviewNeedsActionRow> rows, LiveEventCalendarCheckReport report,
            LiveOpsHubFormat format, DateTime nowUtc)
        {
            List<LiveEventCalendarFinding> shouldReview = new List<LiveEventCalendarFinding>();
            foreach (LiveEventCalendarFinding finding in report.Findings)
            {
                if (finding.IsIgnored || finding.IsAboutRemoteSnapshot) continue;
                if (finding.Consequence == LiveEventCalendarConsequence.ShouldReview) shouldReview.Add(finding);
            }
            if (shouldReview.Count == 0) return;
            List<string> labels = new List<string>();
            foreach (LiveEventCalendarFinding finding in shouldReview) labels.Add(LiveOpsFindingText.ShortLabel(finding));
            string title = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewRowShouldReviewTitleFormat,
                shouldReview.Count, string.Join(LiveOpsHubStrings.OverviewPartSeparator, labels.ToArray()));
            List<string> metas = new List<string>();
            foreach (LiveEventCalendarFinding finding in shouldReview)
            {
                metas.Add(LiveOpsFindingText.PlainText(LiveOpsFindingText.Meta(finding, format, nowUtc)));
            }
            rows.Add(new OverviewNeedsActionRow(HealthState.Warning, title, LocationOf(shouldReview[0]),
                string.Join(LiveOpsHubStrings.OverviewPartSeparator, metas.ToArray()), false,
                LiveOpsHubStrings.OverviewOpenValidationShouldReviewButton, OverviewRowAction.Navigate,
                LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Validation).WithFilter(LiveOpsHubNavigation.FilterShouldReview), true,
                string.Empty));
        }

        private static void AddRemoteRow(List<OverviewNeedsActionRow> rows, LiveOpsHubCalendarSession session, bool canPasteRunningJson)
        {
            if (session.Remote.HasSnapshot) return;
            string detail = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewRowRemoteDetailFormat,
                LiveEventCalendarRuleIds.RemoteSnapshotDrift);
            rows.Add(new OverviewNeedsActionRow(HealthState.NotMeasured, LiveOpsHubStrings.OverviewRowRemoteTitle, detail,
                string.Empty, false, LiveOpsHubStrings.OverviewPasteRunningJsonButton, OverviewRowAction.PasteRunningJson, null,
                canPasteRunningJson, string.Empty));
        }

        private static void AddStaleCheckRow(List<OverviewNeedsActionRow> rows, LiveOpsHubCalendarSession session,
            ExportGateState exportGate, LiveOpsHubFormat format)
        {
            if (session.Check.LastReport == null || !session.Check.IsStale) return;
            string title;
            if (session.Check.StaleReason == LiveOpsHubCheckStaleReason.MilestonePassed && session.Check.PassedMilestoneUtc.HasValue)
            {
                title = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewRowStaleCheckMilestoneTitleFormat,
                    format.ShortDateTimeUtc(session.Check.PassedMilestoneUtc.Value));
            }
            else if (session.Check.CalendarEditedUtc.HasValue)
            {
                title = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewRowStaleCheckEditedTitleFormat,
                    format.ShortDateTimeUtc(session.Check.CalendarEditedUtc.Value));
            }
            else
            {
                title = LiveOpsHubStrings.OverviewRowStaleCheckTitle;
            }
            // Hàng chưa đo được để trống cột "chặn Copy JSON" (Hình 4 ghi "-"): cột đó nói hàng này LÀ lý do cổng đang chặn,
            // mà kết quả kiểm cũ chỉ làm cổng "chưa đo được". Chữ của cột do ExportGateState quyết (V-9), không suy ở đây.
            rows.Add(new OverviewNeedsActionRow(HealthState.NotMeasured, title, LiveOpsHubStrings.OverviewLocationValidation,
                string.Empty, false, LiveOpsHubStrings.OverviewRecheckButton,
                OverviewRowAction.StartCheck, null, true, string.Empty));
        }

        private static void AddNoStampRow(List<OverviewNeedsActionRow> rows, PublishedCalendarStamp latestStamp)
        {
            if (latestStamp != null) return;
            rows.Add(new OverviewNeedsActionRow(HealthState.NotMeasured, LiveOpsHubStrings.OverviewRowNoStampTitle,
                LiveOpsHubStrings.OverviewLocationExport, string.Empty, false, LiveOpsHubStrings.OverviewOpenExportButton,
                OverviewRowAction.Navigate, LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Export), true, string.Empty));
        }

        /// <summary>
        /// Hàng này có phải lý do cổng đang chặn Copy JSON không (V-9). Hỏi đúng
        /// <see cref="ExportGateState.CopyBlockColumnTextFor"/> — helper mà G-EXPORTGATE viết RIÊNG cho cột 96px này — thay vì
        /// đọc <c>ExportGateRow.BlocksCopy</c>: <c>BlocksCopy</c> còn bật cả khi dòng cổng "chưa đo được", nên hai màn sẽ nói
        /// khác nhau ở mọi lần kiểm đã cũ.
        /// </summary>
        private static bool BlocksCopy(ExportGateState exportGate, LiveEventCalendarConsequence consequence, bool isAboutRemoteSnapshot)
        {
            if (exportGate == null) return false;
            return exportGate.CopyBlockColumnTextFor(consequence, isAboutRemoteSnapshot).Length > 0;
        }

        private static string LocationOf(LiveEventCalendarFinding finding)
        {
            switch (finding.TargetKind)
            {
                case LiveEventCalendarTargetKind.RecurringRule: return LiveOpsHubStrings.OverviewLocationRecurring;
                case LiveEventCalendarTargetKind.EventType: return LiveOpsHubStrings.OverviewLocationEventTypes;
                case LiveEventCalendarTargetKind.FixedEvent: return LiveOpsHubStrings.OverviewLocationCalendar;
                default: return LiveOpsHubStrings.OverviewLocationValidation;
            }
        }

        private static string ButtonTextOf(LiveEventCalendarFinding finding)
        {
            switch (finding.TargetKind)
            {
                case LiveEventCalendarTargetKind.RecurringRule:
                    return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewOpenRuleButtonFormat, finding.TargetId);
                case LiveEventCalendarTargetKind.FixedEvent:
                    return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewOpenEventButtonFormat, finding.TargetId);
                case LiveEventCalendarTargetKind.EventType:
                    return LiveOpsHubStrings.OverviewOpenEventTypesButton;
                default:
                    return LiveOpsHubStrings.OverviewOpenValidationButton;
            }
        }

        private static LiveOpsHubNavigation NavigationOf(LiveEventCalendarFinding finding)
        {
            switch (finding.TargetKind)
            {
                case LiveEventCalendarTargetKind.RecurringRule:
                    // Chớp hàng luật đích để mắt tìm được chỗ vừa nhảy tới [SD1 §4.1].
                    return LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.RecurringRules).WithEventType(finding.TargetId, true);
                case LiveEventCalendarTargetKind.FixedEvent:
                    return LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Calendar).WithEntry(finding.TargetEntryKey);
                case LiveEventCalendarTargetKind.EventType:
                    return LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.EventTypes).WithEventType(finding.TargetId, true);
                default:
                    return LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Validation);
            }
        }

        // ============================================================================================================ đường đi của lịch

        /// <summary>Tầng của từng màn theo thứ tự registry — Tổng quan luôn Ok nên không kéo tầng CẤU HÌNH xuống (6.4).</summary>
        private static readonly PipelineStage[] SectionStages =
        {
            PipelineStage.Configure, PipelineStage.Configure, PipelineStage.Schedule, PipelineStage.Schedule,
            PipelineStage.Check, PipelineStage.Export,
        };

        /// <summary>
        /// Chỉ đọc, dành cho test ghim: mảng này song song VỊ TRÍ với thứ tự màn của
        /// <see cref="LiveOpsHubSections.Create(LiveOpsHubServices)"/>. Thêm hay đổi chỗ một màn mà quên mảng này thì health
        /// sẽ gắn nhầm tầng và im lặng (<c>Math.Min</c> nuốt sai lệch), nên thứ tự phải có test ghim chứ không chỉ có comment.
        /// </summary>
        internal static IReadOnlyList<PipelineStage> SectionStagesByRegistryOrder { get; } = Array.AsReadOnly(SectionStages);

        /// <summary>Bốn tầng P1 (PD-1: tầng CHẠY chưa có màn nào nên không vẽ).</summary>
        private static readonly PipelineStage[] FlowStages =
        {
            PipelineStage.Configure, PipelineStage.Schedule, PipelineStage.Check, PipelineStage.Export,
        };

        private static List<OverviewFlowNode> BuildFlowNodes(IReadOnlyList<SectionHealth> sectionHealths)
        {
            SectionHealth[] byStage = new SectionHealth[FlowStages.Length];
            string[] reasonByStage = new string[FlowStages.Length];
            for (int index = 0; index < FlowStages.Length; index++) byStage[index] = SectionHealth.Ok();

            if (sectionHealths != null)
            {
                int count = Math.Min(sectionHealths.Count, SectionStages.Length);
                for (int index = 0; index < count; index++)
                {
                    int stageIndex = IndexOfStage(SectionStages[index]);
                    if (stageIndex < 0) continue;
                    byStage[stageIndex] = SectionHealth.Worse(byStage[stageIndex], sectionHealths[index]);
                    string reason = sectionHealths[index].Reason;
                    if (reason.Length > 0 && string.IsNullOrEmpty(reasonByStage[stageIndex])) reasonByStage[stageIndex] = reason;
                }
            }

            int firstBlockingGate = -1;
            for (int index = 0; index < FlowStages.Length; index++)
            {
                if (!PipelineStages.IsGate(FlowStages[index])) continue;
                if (byStage[index].State != HealthState.Blocked) continue;
                firstBlockingGate = index;
                break;
            }

            List<OverviewFlowNode> nodes = new List<OverviewFlowNode>();
            for (int index = 0; index < FlowStages.Length; index++)
            {
                PipelineStage stage = FlowStages[index];
                string caption = PipelineStages.CaptionOf(stage);
                bool isFirstBlockingGate = index == firstBlockingGate;
                // Đường nối SAU tầng cổng chặn đầu tiên chết, và mọi đường nối sau đó cũng chết — mắt thấy lịch dừng ở đâu.
                bool connectorDead = firstBlockingGate >= 0 && index >= firstBlockingGate;
                bool hasConnector = index < FlowStages.Length - 1;
                string note = NoteOf(byStage[index], isFirstBlockingGate, firstBlockingGate >= 0 && index > firstBlockingGate);
                string reason = reasonByStage[index];
                string tooltip = string.IsNullOrEmpty(reason)
                    ? caption
                    : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewFlowTooltipFormat, caption, reason);
                nodes.Add(new OverviewFlowNode(stage, caption, byStage[index].State, note, hasConnector, connectorDead, tooltip));
            }
            return nodes;
        }

        private static int IndexOfStage(PipelineStage stage)
        {
            for (int index = 0; index < FlowStages.Length; index++)
            {
                if (FlowStages[index] == stage) return index;
            }
            return -1;
        }

        private static string NoteOf(SectionHealth health, bool isFirstBlockingGate, bool isAfterBlockingGate)
        {
            if (isFirstBlockingGate) return LiveOpsHubStrings.OverviewFlowNoteStopsHere;
            if (isAfterBlockingGate && health.State == HealthState.Blocked) return LiveOpsHubStrings.OverviewFlowNoteBlocked;
            switch (health.State)
            {
                case HealthState.Blocked:
                    return health.Badge.Length > 0 ? health.Badge : LiveOpsHubStrings.OverviewFlowNoteBlocked;
                case HealthState.Warning:
                    return health.Badge.Length > 0 ? health.Badge : LiveOpsHubStrings.OverviewFlowNoteWarning;
                case HealthState.NotMeasured:
                    return LiveOpsHubStrings.OverviewFlowNoteNotMeasured;
                default:
                    return LiveOpsHubStrings.OverviewFlowNoteOk;
            }
        }

        // ============================================================================================================ 7 ngày tới

        private static List<OverviewUpcomingRow> BuildUpcomingRows(LiveOpsHubCalendarSession session, LiveOpsHubFormat format,
            DateTime nowUtc, DateTime rangeEndUtc, bool publishedSource)
        {
            List<OverviewUpcomingRow> rows = new List<OverviewUpcomingRow>();
            bool hasStamp = session.Document.LatestStamp != null;
            if (publishedSource && !hasStamp) return rows;

            LiveEventCalendarDocument document = publishedSource ? session.Publish.ActiveBaseline : session.Document;
            LiveEventCalendarCompilation compilation = publishedSource
                ? LiveEventCalendarCompiler.CompileInExportOrder(document)
                : session.Compilation;
            // Bản so chỉ cần khi đang đọc nháp: tag "chỉ trong nháp" và câu "bản đã đăng: …" nói nháp KHÁC bản đã đăng chỗ nào.
            LiveEventCalendarCompilation baseline = !publishedSource && hasStamp
                ? LiveEventCalendarCompiler.CompileInExportOrder(session.Publish.ActiveBaseline)
                : null;

            foreach (LiveEventTypeDefinition type in document.EventTypes)
            {
                AddRowsForType(rows, document, compilation, baseline, type, format, nowUtc, rangeEndUtc);
            }
            AddDroppedRows(rows, document, compilation, baseline, format, nowUtc, rangeEndUtc);
            rows.Sort(CompareUpcomingRows);
            return rows;
        }

        private static int CompareUpcomingRows(OverviewUpcomingRow left, OverviewUpcomingRow right)
        {
            int byTime = left.SortUtc.CompareTo(right.SortUtc);
            if (byTime != 0) return byTime;
            // Cùng mốc giờ thì xếp theo id để bảng không đổi thứ tự giữa hai lần dựng (mắt người đọc bảng theo hàng, không theo số).
            return string.Compare(left.EventIdText, right.EventIdText, StringComparison.Ordinal);
        }

        private static void AddRowsForType(List<OverviewUpcomingRow> rows, LiveEventCalendarDocument document,
            LiveEventCalendarCompilation compilation, LiveEventCalendarCompilation baseline, LiveEventTypeDefinition type,
            LiveOpsHubFormat format, DateTime nowUtc, DateTime rangeEndUtc)
        {
            IReadOnlyList<LiveEventInstance> instances = compilation.GetInstancesInRange(type.TypeId, nowUtc, rangeEndUtc);
            if (instances.Count == 0) return;

            List<LiveEventInstance> starts = new List<LiveEventInstance>();
            foreach (LiveEventInstance instance in instances)
            {
                if (instance.StartUtc >= nowUtc && instance.StartUtc < rangeEndUtc) starts.Add(instance);
            }

            bool isRecurring = document.TryGetRecurringRule(type.TypeId, out RecurringLiveEventRule rule);
            bool grouped = isRecurring && starts.Count >= GroupRecurringThreshold;
            if (grouped)
            {
                // Loại lặp dày (sky-race 7 đợt/tuần) gom MỘT dòng: bảy dòng gần giống nhau đẩy mọi thứ khác ra khỏi tầm mắt.
                LiveEventInstance first = starts[0];
                LiveEventInstance last = starts[starts.Count - 1];
                // Cột Lúc rộng 170px và cắt phần thừa: mảnh "· 7 đợt" từng làm mất luôn mốc cuối trên ảnh, mà cột "Sự kiện"
                // của chính dòng này đã ghi "7 đợt" rồi — nói hai lần để rồi bị cắt là tệ nhất.
                string timeText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewUpcomingGroupedTimeFormat,
                    format.ShortDateTime(first.StartUtc), format.ShortDateTime(last.StartUtc));
                string idText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewUpcomingGroupedIdsFormat,
                    first.EventId, last.EventId);
                string kindText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewUpcomingKindGroupedFormat,
                    starts.Count);
                // Dạng đầy đủ ("20 giờ"), không dạng gọn ("20g"): cùng bảng, dòng phụ cột Lúc đã in "sau 15 giờ 13 phút".
                string noteText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewUpcomingGroupedNoteFormat,
                    format.Duration(TimeSpan.FromHours(rule.ActiveHours), false));
                rows.Add(new OverviewUpcomingRow(first.StartUtc, timeText, format.Relative(nowUtc, first.StartUtc), idText,
                    type.TypeId, type.ColorSlot, OverviewUpcomingKind.Grouped, kindText, noteText, false, null));
            }

            foreach (LiveEventInstance instance in instances)
            {
                bool startsInRange = instance.StartUtc >= nowUtc && instance.StartUtc < rangeEndUtc;
                bool endsInRange = instance.EndUtc >= nowUtc && instance.EndUtc < rangeEndUtc;
                // Dòng gom đã nói hết phần "mở" của loại này; đợt bắt đầu TRƯỚC khung (đang chạy) vẫn phải có dòng khép riêng,
                // vì đó là mốc người chơi thật sự thấy trong bảy ngày tới.
                if (startsInRange && !grouped)
                {
                    rows.Add(BuildInstanceRow(baseline, type, instance, format, nowUtc, true));
                }
                if (endsInRange && (!grouped || !startsInRange))
                {
                    rows.Add(BuildInstanceRow(baseline, type, instance, format, nowUtc, false));
                }
            }
        }

        private static OverviewUpcomingRow BuildInstanceRow(LiveEventCalendarCompilation baseline, LiveEventTypeDefinition type,
            LiveEventInstance instance, LiveOpsHubFormat format, DateTime nowUtc, bool isOpen)
        {
            DateTime momentUtc = isOpen ? instance.StartUtc : instance.EndUtc;
            string kindText = isOpen ? LiveOpsHubStrings.OverviewUpcomingKindOpen : LiveOpsHubStrings.OverviewUpcomingKindClose;
            string noteText = isOpen
                ? (type.RequiresJoin
                    ? LiveOpsHubStrings.OverviewUpcomingNoteRequiresJoin
                    : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.OverviewUpcomingNoteClosesAtFormat,
                        format.ShortDateTimeUtc(instance.EndUtc)))
                : LiveOpsHubStrings.OverviewUpcomingNotePendingResult;
            HealthState? noteState = null;
            bool isDraftOnly = false;

            if (baseline != null)
            {
                LiveEventInstance published = FindInstanceStartingAt(baseline, type.TypeId, instance.StartUtc);
                if (published == null)
                {
                    isDraftOnly = !ContainsEventId(baseline, type.TypeId, instance.EventId, nowUtc);
                }
                else if (!string.Equals(published.EventId, instance.EventId, StringComparison.Ordinal))
                {
                    isDraftOnly = true;
                    bool wasRunning = published.PhaseAt(nowUtc) == LiveEventPhase.Active
                        || instance.PhaseAt(nowUtc) == LiveEventPhase.Active;
                    // Đổi id của một đợt ĐANG CHẠY = người chơi mất tiến độ; nói thẳng trong bảng, không chỉ ở Kiểm lịch.
                    noteText = string.Format(CultureInfo.InvariantCulture,
                        wasRunning
                            ? LiveOpsHubStrings.OverviewUpcomingNoteRunningIdChangedFormat
                            : LiveOpsHubStrings.OverviewUpcomingNotePublishedIdFormat, published.EventId);
                    noteState = wasRunning ? HealthState.Warning : (HealthState?)null;
                }
            }

            return new OverviewUpcomingRow(momentUtc, format.ShortDateTime(momentUtc), format.Relative(nowUtc, momentUtc),
                instance.EventId, type.TypeId, type.ColorSlot, isOpen ? OverviewUpcomingKind.Open : OverviewUpcomingKind.Close,
                kindText, noteText, isDraftOnly, noteState);
        }

        private static void AddDroppedRows(List<OverviewUpcomingRow> rows, LiveEventCalendarDocument document,
            LiveEventCalendarCompilation compilation, LiveEventCalendarCompilation baseline, LiveOpsHubFormat format,
            DateTime nowUtc, DateTime rangeEndUtc)
        {
            foreach (LiveEventCalendarEntryOutcome outcome in compilation.Entries)
            {
                if (outcome.IsKept || outcome.Kind != LiveEventCalendarEntryKind.FixedEvent) continue;
                if (!document.TryGetFixedEvent(outcome.EntryKey, out FixedLiveEventEntry entry)) continue;
                if (!entry.TryGetStartUtc(out DateTime startUtc)) continue;
                if (startUtc < nowUtc || startUtc >= rangeEndUtc) continue;
                int colorSlot = document.TryGetEventType(entry.EventType, out LiveEventTypeDefinition type) ? type.ColorSlot : 0;
                bool isDraftOnly = baseline != null && !ContainsEventId(baseline, entry.EventType, entry.EventId, nowUtc);
                // Lý do bỏ lấy từ nguồn câu duy nhất (V-8) — bảng không tự viết "chồng 12 giờ với hunt-0914".
                string noteText = DropReasonText(outcome, format);
                rows.Add(new OverviewUpcomingRow(startUtc, format.ShortDateTime(startUtc), format.Relative(nowUtc, startUtc),
                    entry.EventId, entry.EventType, colorSlot, OverviewUpcomingKind.Dropped,
                    LiveOpsHubStrings.OverviewUpcomingKindDropped, noteText, isDraftOnly, HealthState.Blocked));
            }
        }

        /// <summary>
        /// Câu lý do đầy đủ cho cột Ghi chú của dòng "bị bỏ": có khoảng chồng và id đối thủ thì dùng dạng chi tiết
        /// ("chồng 12 giờ với hunt-0914" — Hình 4), không dùng nhãn ngắn "chồng giờ" vốn để xếp cạnh nhãn khác trong một danh
        /// sách. Cả hai câu đều của <see cref="LiveOpsFindingText"/> (V-8) — bảng không tự viết chữ nào.
        /// </summary>
        private static string DropReasonText(LiveEventCalendarEntryOutcome outcome, LiveOpsHubFormat format)
        {
            if (outcome.DropReason == LiveEventCalendarDropReason.OverlapsSameType && outcome.OverlapStartUtc.HasValue
                && outcome.OverlapEndUtc.HasValue)
            {
                return LiveOpsFindingText.Format(LiveOpsHubStrings.FindingOverlapDetailFormat,
                    format.Duration(outcome.OverlapEndUtc.Value - outcome.OverlapStartUtc.Value, false),
                    LiveOpsFindingText.IdText(outcome.RelatedEventId));
            }
            return LiveOpsFindingText.DropReasonPhrase(outcome.DropReason, outcome);
        }

        private static LiveEventInstance FindInstanceStartingAt(LiveEventCalendarCompilation compilation, string eventType,
            DateTime startUtc)
        {
            IReadOnlyList<LiveEventInstance> instances = compilation.GetInstancesInRange(eventType, startUtc, startUtc.AddTicks(1));
            foreach (LiveEventInstance instance in instances)
            {
                if (instance.StartUtc == startUtc) return instance;
            }
            return null;
        }

        private static bool ContainsEventId(LiveEventCalendarCompilation compilation, string eventType, string eventId, DateTime aroundUtc)
        {
            return compilation.TryFindInstanceById(eventType, eventId, aroundUtc, out LiveEventInstance _);
        }
    }
}
