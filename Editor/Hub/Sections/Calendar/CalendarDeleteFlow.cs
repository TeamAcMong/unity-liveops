using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Ba mức xoá một đợt cố định [FD §3.10], đọc mức từ <see cref="LiveOpsConfirmationPolicy"/> chứ không tự phán: chưa bắt đầu
    /// và chưa có trong bản đã đăng (hoặc đã khép, Q-14) → xoá ngay + toast Hoàn tác; đã đăng chưa bắt đầu → hộp cấp 1 nêu hậu
    /// quả và đợt hết chồng giờ nhờ xoá; đang chạy → hộp cấp 2 gõ id.
    /// <para>
    /// Lớp thuần: dựng câu, dựng <see cref="LiveOpsConfirmRequest"/> và trả lệnh sửa, không mở cửa sổ và không gọi
    /// <c>UnityEditor</c> — nhờ vậy <c>CalendarDeleteFlowTests</c> chạy được <c>-nographics</c> và câu toast dạng ngắn kiểm được
    /// ở mức model (9.2), không phải bằng ảnh.
    /// </para>
    /// </summary>
    internal sealed class CalendarDeleteFlow
    {
        /// <summary>Dưới bề rộng này cửa sổ ở <c>--medium</c>: toast xoá rút còn ngày, giờ đủ nằm ở tooltip (PD của 7.3).</summary>
        internal const float ShortToastWidthThreshold = 1100f;

        private readonly LiveOpsHubCalendarSession _session;
        private readonly LiveOpsHubFormat _format;
        private readonly FixedLiveEventEntry _entry;
        private readonly DateTime _nowUtc;

        private CalendarDeleteFlow(LiveOpsHubCalendarSession session, LiveOpsHubFormat format, FixedLiveEventEntry entry,
            LiveOpsConfirmDecision decision, DateTime nowUtc)
        {
            _session = session;
            _format = format;
            _entry = entry;
            _nowUtc = nowUtc;
            Decision = decision;
        }

        /// <summary>Mức xác nhận đã quyết; <c>null</c> khi không tìm thấy đợt (đợt vừa bị xoá ở nơi khác).</summary>
        public LiveOpsConfirmDecision Decision { get; }

        public bool HasTarget => _entry != null;

        public LiveOpsConfirmRequirement Requirement => Decision == null
            ? LiveOpsConfirmRequirement.None
            : Decision.Requirement;

        /// <summary>Có hỏi trước khi xoá không — nhãn nút mang "…" đúng khi giá trị này là <c>true</c>.</summary>
        public bool AsksBeforeDeleting => Requirement != LiveOpsConfirmRequirement.None;

        public string ButtonText => AsksBeforeDeleting
            ? LiveOpsHubStrings.CalendarDeleteButtonWithDialog
            : LiveOpsHubStrings.CalendarDeleteButton;

        public string ButtonTooltip => AsksBeforeDeleting
            ? LiveOpsHubStrings.CalendarDeleteButtonConfirmTooltip
            : LiveOpsHubStrings.CalendarDeleteButtonImmediateTooltip;

        public static CalendarDeleteFlow For(LiveOpsHubCalendarSession session, string entryKey, DateTime nowUtc, LiveOpsHubFormat format)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (format == null) throw new ArgumentNullException(nameof(format));
            DateTime now = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            LiveEventCalendarDocument document = session.Document ?? LiveEventCalendarDocument.Empty;
            if (string.IsNullOrEmpty(entryKey) || !document.TryGetFixedEvent(entryKey, out FixedLiveEventEntry entry))
            {
                return new CalendarDeleteFlow(session, format, null, null, now);
            }
            LiveEventCalendarDocument baseline = session.Publish == null ? null : session.Publish.ActiveBaseline;
            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.DeleteFixedEvent, document, null,
                baseline, now, entryKey);
            return new CalendarDeleteFlow(session, format, entry, decision, now);
        }

        /// <summary>Câu toast sau khi xoá; <paramref name="contentWidth"/> dưới ngưỡng thì rút còn ngày.</summary>
        public string ToastMessage(float contentWidth)
        {
            if (_entry == null) return string.Empty;
            bool hasStart = _entry.TryGetStartUtc(out DateTime startUtc);
            bool hasEnd = _entry.TryGetEndUtc(out DateTime endUtc);
            if (!hasStart || !hasEnd) return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDeleteToastShortFormat,
                _entry.EventId, _entry.StartUtcText, _entry.EndUtcText);
            if (contentWidth > 0f && contentWidth < ShortToastWidthThreshold)
            {
                // "16/9" viết bằng CHÍNH hàm mà mọi câu ngắn khác của hub dùng: khuôn "d/M" riêng ở đây in theo quy ước ngày/tháng
                // kiểu Việt cả khi hub đang chạy tiếng Anh, và không theo luật "không số 0 đầu" của [FD §6.1].
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDeleteToastShortFormat, _entry.EventId,
                    LiveOpsFindingText.DayMonth(startUtc), LiveOpsFindingText.DayMonth(endUtc));
            }
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDeleteToastFormat, _entry.EventId,
                _format.ShortDateTime(startUtc), _format.ShortDateTime(endUtc));
        }

        /// <summary>Tooltip của toast luôn có giờ đủ — dạng ngắn mất giờ thì người đọc vẫn lấy lại được bằng chuột.</summary>
        public string ToastTooltip()
        {
            if (_entry == null) return string.Empty;
            if (!_entry.TryGetStartUtc(out DateTime startUtc) || !_entry.TryGetEndUtc(out DateTime endUtc)) return string.Empty;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarTimeRangeFormat,
                _format.ShortDateTime(startUtc), _format.ShortDateTime(endUtc));
        }

        /// <summary>Hộp xác nhận của mức hiện tại; <c>null</c> khi mức là "không hỏi".</summary>
        public LiveOpsConfirmRequest BuildConfirmRequest(string undoKeyLabel)
        {
            if (_entry == null || !AsksBeforeDeleting) return null;
            // Đợt đang chạy / đã đăng luôn đọc được giờ (giờ hỏng thì policy xếp vào "không đặt được"), nhưng vẫn khởi tạo mặc định
            // để hộp dựng được thay vì ném giữa luồng xoá.
            DateTime startUtc = default;
            DateTime endUtc = default;
            _entry.TryGetStartUtc(out startUtc);
            _entry.TryGetEndUtc(out endUtc);
            string times = _format.ShortDateTime(startUtc);
            string endTimes = _format.ShortDateTime(endUtc);
            string undoHint = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarUndoHintFormat, undoKeyLabel);

            if (Requirement == LiveOpsConfirmRequirement.TypeToConfirm)
            {
                string runningBody = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDeleteRunningBodyFormat,
                    times, endTimes);
                return new LiveOpsConfirmRequest.Builder()
                    .WithTitle(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDeleteRunningConfirmTitleFormat,
                        _entry.EventId))
                    .WithBody(JoinSentences(runningBody, LiveOpsHubStrings.CalendarUnknownPlayerCountSentence, NoRecordSentence(), undoHint))
                    .WithTypeToConfirm(_entry.EventId)
                    .WithButtons(LiveOpsHubStrings.CalendarDeleteButton, LiveOpsHubStrings.KitConfirmKeepLabel)
                    .Build();
            }

            // Phiên chưa có dấu đã đăng đọc được → biến thể câu KHÔNG chừa chỗ cho giờ đăng; ghép chuỗi rỗng vào khuôn có {2} cho
            // ra câu cụt "…đã có trong bản đăng : lần đăng tới…".
            string publishedStamp = PublishedStampText();
            string body = publishedStamp.Length == 0
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDeletePublishedNoStampBodyFormat, times, endTimes)
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDeletePublishedBodyFormat, times,
                    endTimes, publishedStamp);
            return new LiveOpsConfirmRequest.Builder()
                .WithTitle(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDeleteConfirmTitleFormat, _entry.EventId))
                .WithBody(JoinSentences(body, OverlapClearedSentence(), undoHint))
                .WithButtons(LiveOpsHubStrings.CalendarDeleteButton, LiveOpsHubStrings.KitConfirmKeepLabel)
                .Build();
        }

        /// <summary>Lệnh sửa của luồng — gọi sau khi hộp (nếu có) trả Destructive.</summary>
        public LiveEventCalendarEdit BuildEdit()
        {
            return _entry == null ? null : new RemoveFixedEventEdit(_entry.EntryKey);
        }

        /// <summary>
        /// Đợt hết chồng giờ nhờ xoá đợt này: kiểm nhanh làn trước và sau khi xoá rồi lấy phần chênh. Nói được hệ quả thật thay vì
        /// hứa suông là lý do hộp cấp 1 đáng mở.
        /// </summary>
        internal string OverlapClearedSentence()
        {
            if (_entry == null) return string.Empty;
            LiveEventCalendarDocument document = _session.Document;
            if (document == null) return string.Empty;
            LiveEventCalendarCheckReport before = _session.CheckLane(_entry.EventType, document);
            if (!LiveEventCalendarEdits.TryApply(document, new RemoveFixedEventEdit(_entry.EntryKey), out LiveEventCalendarDocument after))
            {
                return string.Empty;
            }
            LiveEventCalendarCheckReport afterReport = _session.CheckLane(_entry.EventType, after);
            HashSet<string> stillDropped = new HashSet<string>(StringComparer.Ordinal);
            CollectDroppedIds(afterReport, stillDropped);
            List<string> cleared = new List<string>();
            CollectClearedIds(before, stillDropped, cleared);
            if (cleared.Count == 0) return string.Empty;
            string clearedId = cleared[0];
            if (!TryFindStartUtc(after, clearedId, out DateTime clearedStartUtc)) return string.Empty;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarOverlapClearedFormat, clearedId,
                _format.ShortDateTime(clearedStartUtc));
        }

        private static bool TryFindStartUtc(LiveEventCalendarDocument document, string eventId, out DateTime startUtc)
        {
            startUtc = default;
            IReadOnlyList<FixedLiveEventEntry> entries = document.FixedEvents;
            for (int index = 0; index < entries.Count; index++)
            {
                if (!string.Equals(entries[index].EventId, eventId, StringComparison.Ordinal)) continue;
                return entries[index].TryGetStartUtc(out startUtc);
            }
            return false;
        }

        private static void CollectDroppedIds(LiveEventCalendarCheckReport report, HashSet<string> result)
        {
            if (report == null) return;
            IReadOnlyList<LiveEventCalendarFinding> findings = report.Findings;
            for (int index = 0; index < findings.Count; index++)
            {
                LiveEventCalendarFinding finding = findings[index];
                if (finding.Consequence == LiveEventCalendarConsequence.Dropped) result.Add(finding.TargetId);
            }
        }

        private void CollectClearedIds(LiveEventCalendarCheckReport before, HashSet<string> stillDropped, List<string> cleared)
        {
            if (before == null) return;
            IReadOnlyList<LiveEventCalendarFinding> findings = before.Findings;
            for (int index = 0; index < findings.Count; index++)
            {
                LiveEventCalendarFinding finding = findings[index];
                if (finding.Consequence != LiveEventCalendarConsequence.Dropped) continue;
                // Chính đợt sắp xoá thì không phải "hết chồng giờ" — nó biến mất, không phải được cứu.
                if (string.Equals(finding.TargetId, _entry.EventId, StringComparison.Ordinal)) continue;
                if (stillDropped.Contains(finding.TargetId)) continue;
                if (!cleared.Contains(finding.TargetId)) cleared.Add(finding.TargetId);
            }
        }

        private string PublishedStampText()
        {
            PublishedCalendarStamp stamp = _session.Publish == null ? null : _session.Publish.ActiveStamp;
            if (stamp == null || !stamp.TryGetPublishedUtc(out DateTime publishedUtc)) return string.Empty;
            return _format.ShortDateTime(publishedUtc);
        }

        /// <summary>Câu PD-17 cho Editor chưa có bản ghi của đợt: nêu thiếu số, KHÔNG in 0 (7.0).</summary>
        private string NoRecordSentence()
        {
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarNoEditorRecordFormat, _entry.EventId);
        }

        private static string JoinSentences(params string[] sentences)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int index = 0; index < sentences.Length; index++)
            {
                string sentence = sentences[index];
                if (string.IsNullOrEmpty(sentence)) continue;
                if (builder.Length > 0) builder.Append(' ');
                builder.Append(sentence);
            }
            return builder.ToString();
        }

        /// <summary>Giờ đã dùng để quyết mức — test đọc lại để chắc luồng không tự đi tìm đồng hồ.</summary>
        internal DateTime NowUtcForTests => _nowUtc;
    }
}
