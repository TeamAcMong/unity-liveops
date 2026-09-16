using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Model thuần của inspector đợt trên màn Lịch [SD1 §3.1, §3.10] — mọi quyết định (trạng thái pane, khoá mép đầu, nhãn nút
    /// xoá, câu nguồn, chữ dẫn config key, danh sách phát hiện của đợt) nằm ở đây; <see cref="CalendarEventInspector"/> chỉ đọc
    /// và gắn class. Không <c>VisualElement</c>, không <c>UnityEditor</c> nên test chạy được <c>-nographics</c> (7.0).
    /// </summary>
    internal sealed class CalendarInspectorModel
    {
        /// <summary>(a) chưa chọn gì — pane nói cách có nội dung và tóm tắt lịch.</summary>
        internal const int StateNothingSelected = 0;

        /// <summary>Đợt cố định đọc được giờ — đủ field sửa được.</summary>
        internal const int StateFixedEvent = 1;

        /// <summary>(b) đợt sinh từ luật lặp — field chỉ đọc, chỉ đường sang Luật lặp.</summary>
        internal const int StateRecurringEvent = 2;

        /// <summary>(d) đợt cố định có giờ không đọc được — ô viền lỗi, giữ nguyên chuỗi gốc.</summary>
        internal const int StateUnreadableTimes = 3;

        /// <summary>Ngăn giữa loại và chỉ số lần lặp trong <c>BarKey</c> của timeline (fixed: chính EntryKey, không có ngăn này).</summary>
        private const char BarKeyTypeSeparator = '#';

        /// <summary>Giờ trong nhãn nút "Sửa thành …" — cùng dạng ô giờ của <see cref="LiveOpsUtcDateTimeField"/>.</summary>
        private const string FixButtonTimeFormat = "HH:mm";

        private static readonly IReadOnlyList<LiveEventCalendarFinding> NoFindings = Array.Empty<LiveEventCalendarFinding>();

        private CalendarInspectorModel(int state)
        {
            State = state;
            Findings = NoFindings;
            EventId = string.Empty;
            EventTypeId = string.Empty;
            StartLockReason = string.Empty;
            DeleteButtonText = string.Empty;
            DeleteButtonTooltip = string.Empty;
            SummaryText = string.Empty;
            SourceText = string.Empty;
            PhaseTagText = string.Empty;
            ConfigKeyPlaceholder = string.Empty;
            ConfigKeyResetTooltip = string.Empty;
            RequiresOptInText = string.Empty;
            RecurringNoteText = string.Empty;
            IssuesFoldoutText = string.Empty;
            UnreadableFieldErrorText = string.Empty;
            UnreadableFixButtonText = string.Empty;
            ChangedTooltip = string.Empty;
        }

        public int State { get; private set; }

        /// <summary>Đợt cố định đang chọn; <c>null</c> ở (a) và (b).</summary>
        public FixedLiveEventEntry Entry { get; private set; }

        public string EventId { get; private set; }
        public string EventTypeId { get; private set; }

        /// <summary>Định nghĩa loại của đợt; <c>null</c> khi loại chưa khai báo (JSON đã dán) — view vẫn vẽ được hàng.</summary>
        public LiveEventTypeDefinition EventTypeDefinition { get; private set; }

        public LiveEventPhase Phase { get; private set; }
        public string PhaseTagText { get; private set; }

        /// <summary>Đang chạy: mép đầu khoá vì người chơi đã vào theo giờ bắt đầu cũ (bảng 7.0).</summary>
        public bool IsStartLocked { get; private set; }

        public string StartLockReason { get; private set; }

        /// <summary>"Xoá đợt" (xoá ngay) hoặc "Xoá đợt…" (sẽ hỏi) theo <see cref="CalendarDeleteFlow"/>.</summary>
        public string DeleteButtonText { get; private set; }

        public string DeleteButtonTooltip { get; private set; }

        /// <summary>Phát hiện có đích là chính đợt này, theo thứ tự trong báo cáo (nặng nhất trước đã do bộ kiểm sắp).</summary>
        public IReadOnlyList<LiveEventCalendarFinding> Findings { get; private set; }

        /// <summary>"6 đợt cố định · 2 luật lặp · 1 không đặt được" — chỉ dùng ở (a).</summary>
        public string SummaryText { get; private set; }

        public string SourceText { get; private set; }

        /// <summary>Chữ dẫn nghiêng trong ô config key rỗng: nêu giá trị Xuất JSON sẽ ghi (không để ô trống bị đọc là "chưa cấu hình").</summary>
        public string ConfigKeyPlaceholder { get; private set; }

        public string ConfigKeyResetTooltip { get; private set; }
        public bool HasOwnConfigKey { get; private set; }
        public string RequiresOptInText { get; private set; }

        /// <summary>(b) câu chỉ chỗ sửa được; "" ở trạng thái khác.</summary>
        public string RecurringNoteText { get; private set; }

        /// <summary>Loại của luật sinh ra đợt (b) — nút "Mở luật" điều hướng theo đây.</summary>
        public string RuleEventType { get; private set; }

        public string IssuesFoldoutText { get; private set; }

        /// <summary>(d) câu lỗi của ô không đọc được, lấy từ <see cref="LiveOpsUtcDateTimeField.DescribeParseError"/>.</summary>
        public string UnreadableFieldErrorText { get; private set; }

        /// <summary>(d) nhãn nút sửa nhanh; "" khi không đoán được giá trị nào.</summary>
        public string UnreadableFixButtonText { get; private set; }

        /// <summary>(d) giá trị nút sửa nhanh sẽ ghi vào ô (dạng chuẩn UTC); "" khi không có.</summary>
        public string UnreadableFixValueText { get; private set; }

        /// <summary>(d) <c>true</c> khi ô hỏng là Kết thúc, <c>false</c> khi là Bắt đầu.</summary>
        public bool IsUnreadableEnd { get; private set; }

        /// <summary>Số giờ của đợt, làm tròn xuống; 0 khi giờ không đọc được.</summary>
        public int DurationHours { get; private set; }

        /// <summary>(V-22 CC-FT-1) "Khác bản đã đăng: …" lấy từ <see cref="LiveOpsChangeText"/> khi có dấu đã đăng; "" khi không.</summary>
        public string ChangedTooltip { get; private set; }

        /// <summary>
        /// Dựng từ phiên: tài liệu nháp, báo cáo kiểm (có thể cũ), bản so đã đăng và giờ hiện tại. <paramref name="selectedBarKey"/>
        /// là <c>BarKey</c> của timeline — đợt cố định dùng chính <c>EntryKey</c>, đợt sinh từ luật có dạng
        /// <c>&lt;loại&gt;#&lt;chỉ số&gt;</c>, nên tra EntryKey trước rồi mới cắt phần trước dấu ngăn.
        /// </summary>
        public static CalendarInspectorModel Build(LiveOpsHubCalendarSession session, string selectedBarKey, DateTime nowUtc,
            LiveOpsHubFormat format)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (format == null) throw new ArgumentNullException(nameof(format));
            DateTime now = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            LiveEventCalendarDocument document = session.Document ?? LiveEventCalendarDocument.Empty;
            string barKey = selectedBarKey ?? string.Empty;

            if (barKey.Length > 0 && document.TryGetFixedEvent(barKey, out FixedLiveEventEntry entry))
            {
                return BuildFixed(session, document, entry, now, format);
            }
            if (barKey.Length > 0)
            {
                string eventType = TypeIdOfBarKey(barKey);
                if (eventType.Length > 0 && document.TryGetRecurringRule(eventType, out RecurringLiveEventRule rule))
                {
                    return BuildRecurring(document, rule, barKey);
                }
            }
            return BuildNothingSelected(document, now);
        }

        /// <summary>Loại của một <c>BarKey</c> sinh từ luật; "" khi khoá không có dấu ngăn (tức là EntryKey của đợt cố định).</summary>
        internal static string TypeIdOfBarKey(string barKey)
        {
            if (string.IsNullOrEmpty(barKey)) return string.Empty;
            int separatorIndex = barKey.IndexOf(BarKeyTypeSeparator.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
            return separatorIndex <= 0 ? string.Empty : barKey.Substring(0, separatorIndex);
        }

        private static CalendarInspectorModel BuildNothingSelected(LiveEventCalendarDocument document, DateTime nowUtc)
        {
            CalendarInspectorModel model = new CalendarInspectorModel(StateNothingSelected);
            int unplaceableCount = 0;
            IReadOnlyList<FixedLiveEventEntry> fixedEvents = document.FixedEvents;
            for (int index = 0; index < fixedEvents.Count; index++)
            {
                if (!IsPlaceable(fixedEvents[index])) unplaceableCount++;
            }
            model.SummaryText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarInspectorSummaryFormat,
                fixedEvents.Count, document.RecurringRules.Count, unplaceableCount);
            model.Phase = LiveEventPhase.None;
            model.NowUtcForTests = nowUtc;
            return model;
        }

        private static CalendarInspectorModel BuildFixed(LiveOpsHubCalendarSession session, LiveEventCalendarDocument document,
            FixedLiveEventEntry entry, DateTime nowUtc, LiveOpsHubFormat format)
        {
            bool hasStart = entry.TryGetStartUtc(out DateTime startUtc);
            bool hasEnd = entry.TryGetEndUtc(out DateTime endUtc);
            CalendarInspectorModel model = new CalendarInspectorModel(hasStart && hasEnd ? StateFixedEvent : StateUnreadableTimes);
            model.Entry = entry;
            model.EventId = entry.EventId;
            model.EventTypeId = entry.EventType;
            model.NowUtcForTests = nowUtc;
            if (document.TryGetEventType(entry.EventType, out LiveEventTypeDefinition typeDefinition))
            {
                model.EventTypeDefinition = typeDefinition;
            }

            model.Phase = PhaseOf(hasStart, startUtc, hasEnd, endUtc, nowUtc);
            model.PhaseTagText = PhaseTextOf(model.Phase, model.State == StateUnreadableTimes);
            if (model.Phase == LiveEventPhase.Active)
            {
                model.IsStartLocked = true;
                model.StartLockReason = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarStartLockedTooltipFormat,
                    format.ShortDateTime(startUtc));
            }
            if (hasStart && hasEnd && endUtc > startUtc)
            {
                model.DurationHours = (int)(endUtc - startUtc).TotalHours;
            }

            CalendarDeleteFlow deleteFlow = CalendarDeleteFlow.For(session, entry.EntryKey, nowUtc, format);
            model.DeleteButtonText = deleteFlow.ButtonText;
            model.DeleteButtonTooltip = deleteFlow.ButtonTooltip;

            model.SourceText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarSourceFixedFormat,
                session.AssetFileName);
            model.HasOwnConfigKey = entry.HasOwnConfigKey;
            string defaultConfigKey = model.EventTypeDefinition == null ? string.Empty : model.EventTypeDefinition.DefaultConfigKey;
            model.ConfigKeyPlaceholder = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarConfigKeyPlaceholderFormat,
                defaultConfigKey);
            model.ConfigKeyResetTooltip = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarConfigKeyResetTooltipFormat,
                defaultConfigKey);
            model.RequiresOptInText = RequiresOptInTextOf(model.EventTypeDefinition, entry.EventType);

            model.Findings = FindingsOf(session.Check, entry);
            model.IssuesFoldoutText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarIssuesFoldoutFormat,
                model.Findings.Count);
            model.ChangedTooltip = ChangedTooltipOf(session, entry, nowUtc, format);

            if (model.State == StateUnreadableTimes) FillUnreadable(model, entry, hasStart, hasEnd, startUtc);
            return model;
        }

        private static CalendarInspectorModel BuildRecurring(LiveEventCalendarDocument document, RecurringLiveEventRule rule, string barKey)
        {
            CalendarInspectorModel model = new CalendarInspectorModel(StateRecurringEvent);
            model.EventTypeId = rule.EventType;
            model.RuleEventType = rule.EventType;
            model.EventId = barKey;
            if (document.TryGetEventType(rule.EventType, out LiveEventTypeDefinition typeDefinition))
            {
                model.EventTypeDefinition = typeDefinition;
            }
            model.RecurringNoteText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarRecurringNoteFormat,
                rule.EventType);
            model.SourceText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarSourceRecurringFormat, rule.EventType);
            model.RequiresOptInText = RequiresOptInTextOf(model.EventTypeDefinition, rule.EventType);
            model.IssuesFoldoutText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarIssuesFoldoutFormat, 0);
            return model;
        }

        /// <summary>Giờ trong nháp lưu nguyên văn nên ô hỏng vẫn phải vẽ được: câu lỗi và giá trị đề nghị tính từ chính chuỗi đó.</summary>
        private static void FillUnreadable(CalendarInspectorModel model, FixedLiveEventEntry entry, bool hasStart, bool hasEnd,
            DateTime startUtc)
        {
            model.IsUnreadableEnd = hasStart && !hasEnd;
            string rawText = model.IsUnreadableEnd ? entry.EndUtcText : entry.StartUtcText;
            string dateText = DatePartOf(rawText);
            string timeText = TimePartOf(rawText);
            model.UnreadableFieldErrorText = LiveOpsUtcDateTimeField.DescribeParseError(dateText, timeText);
            if (!TryRepairDate(dateText, out DateTime repairedDate)) return;
            // Chỉ đề nghị giá trị khi ĐOÁN ĐƯỢC ngày: nút "Sửa thành …" mà đoán sai còn tệ hơn không có nút.
            DateTime repaired = DateTime.SpecifyKind(repairedDate, DateTimeKind.Utc);
            if (model.IsUnreadableEnd && hasStart && repaired <= startUtc) repaired = startUtc.AddHours(1);
            model.UnreadableFixValueText = LiveEventUtcText.Format(repaired);
            model.UnreadableFixButtonText = string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarUnreadableFixButtonFormat, FixButtonValueText(repaired));
        }

        private static string FixButtonValueText(DateTime repaired)
        {
            return repaired.ToString(LiveOpsUtcDateTimeField.DateFormat, CultureInfo.InvariantCulture) + " "
                + repaired.ToString(FixButtonTimeFormat, CultureInfo.InvariantCulture);
        }

        private static bool TryRepairDate(string dateText, out DateTime repairedDate)
        {
            return DateTime.TryParseExact(dateText, LiveOpsUtcDateTimeField.DateFormat, CultureInfo.InvariantCulture,
                       DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out repairedDate)
                   || DateTime.TryParse(dateText, CultureInfo.InvariantCulture,
                       DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out repairedDate);
        }

        private static string DatePartOf(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return string.Empty;
            int timeIndex = rawText.IndexOf('T');
            return timeIndex < 0 ? rawText : rawText.Substring(0, timeIndex);
        }

        private static string TimePartOf(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return string.Empty;
            int timeIndex = rawText.IndexOf('T');
            if (timeIndex < 0 || timeIndex + 1 >= rawText.Length) return string.Empty;
            string time = rawText.Substring(timeIndex + 1).TrimEnd('Z');
            return time.Length > 5 ? time.Substring(0, 5) : time;
        }

        private static string RequiresOptInTextOf(LiveEventTypeDefinition typeDefinition, string typeId)
        {
            string format = typeDefinition != null && typeDefinition.RequiresJoin
                ? LiveOpsHubStrings.CalendarRequiresOptInYesFormat
                : LiveOpsHubStrings.CalendarRequiresOptInNoFormat;
            return string.Format(CultureInfo.InvariantCulture, format, typeId);
        }

        private static IReadOnlyList<LiveEventCalendarFinding> FindingsOf(LiveOpsHubCheckState check, FixedLiveEventEntry entry)
        {
            if (check == null || check.LastReport == null) return NoFindings;
            List<LiveEventCalendarFinding> findings = new List<LiveEventCalendarFinding>();
            IReadOnlyList<LiveEventCalendarFinding> reported = check.LastReport.Findings;
            for (int index = 0; index < reported.Count; index++)
            {
                LiveEventCalendarFinding finding = reported[index];
                bool matchesEntry = finding.TargetEntryKey.Length > 0
                    && string.Equals(finding.TargetEntryKey, entry.EntryKey, StringComparison.Ordinal);
                bool matchesId = finding.TargetEntryKey.Length == 0
                    && string.Equals(finding.TargetId, entry.EventId, StringComparison.Ordinal);
                if (matchesEntry || matchesId) findings.Add(finding);
            }
            return findings;
        }

        /// <summary>(V-22 CC-FT-1) Câu "khác bản đã đăng" luôn dựng bằng overload có ngữ cảnh khi phiên đã có đủ hai tài liệu và diff.</summary>
        private static string ChangedTooltipOf(LiveOpsHubCalendarSession session, FixedLiveEventEntry entry, DateTime nowUtc,
            LiveOpsHubFormat format)
        {
            LiveEventCalendarDocument baseline = session.Publish == null ? null : session.Publish.ActiveBaseline;
            if (baseline == null) return string.Empty;
            LiveEventCalendarDiffResult diff = session.Publish.PublishedDiff;
            if (diff == null) return string.Empty;
            IReadOnlyList<LiveEventCalendarChange> changes = diff.Changes;
            for (int index = 0; index < changes.Count; index++)
            {
                LiveEventCalendarChange change = changes[index];
                if (!string.Equals(change.ItemId, entry.EventId, StringComparison.Ordinal)) continue;
                LiveOpsChangeTextContext context = new LiveOpsChangeTextContext(baseline, session.Document, nowUtc, diff, false);
                return LiveOpsChangeText.ChangedTooltip(change, context, format);
            }
            return string.Empty;
        }

        private static bool IsPlaceable(FixedLiveEventEntry entry)
        {
            return entry.TryGetStartUtc(out DateTime _) && entry.TryGetEndUtc(out DateTime _);
        }

        private static LiveEventPhase PhaseOf(bool hasStart, DateTime startUtc, bool hasEnd, DateTime endUtc, DateTime nowUtc)
        {
            if (!hasStart || !hasEnd) return LiveEventPhase.None;
            if (nowUtc < startUtc) return LiveEventPhase.Upcoming;
            return nowUtc < endUtc ? LiveEventPhase.Active : LiveEventPhase.Ended;
        }

        private static string PhaseTextOf(LiveEventPhase phase, bool isUnplaceable)
        {
            if (isUnplaceable) return LiveOpsHubStrings.CalendarPhaseUnplaceable;
            switch (phase)
            {
                case LiveEventPhase.Upcoming: return LiveOpsHubStrings.CalendarPhaseUpcoming;
                case LiveEventPhase.Active: return LiveOpsHubStrings.CalendarPhaseRunning;
                case LiveEventPhase.Ended: return LiveOpsHubStrings.CalendarPhaseEnded;
                default: return LiveOpsHubStrings.CalendarPhaseUnplaceable;
            }
        }

        /// <summary>Giờ đã dùng để dựng model — test đọc lại để chắc model không tự đi tìm đồng hồ.</summary>
        internal DateTime NowUtcForTests { get; private set; }
    }
}
