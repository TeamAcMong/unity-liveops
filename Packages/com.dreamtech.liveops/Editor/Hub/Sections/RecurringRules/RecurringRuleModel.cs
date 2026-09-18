using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Tên field của form Luật lặp — khoá để tra lỗi field, nối token với ô, và nói nháp đang ở ô nào.</summary>
    internal static class RecurringRuleFields
    {
        internal const string IdPrefix = "id-prefix";
        internal const string Anchor = "anchor";
        internal const string PeriodHours = "period-hours";
        internal const string ActiveHours = "active-hours";
    }

    /// <summary>Kiểu hiện của một token trong câu đọc ([SD1 §4.1]).</summary>
    internal enum RecurringTokenState
    {
        Normal = 0,

        /// <summary>Field tương ứng đang focus — token viền <c>--highlighted</c>.</summary>
        Highlighted = 1,

        /// <summary>Giá trị đang gây hậu quả "người chơi mất tiến độ" — chữ token đổi sang warning-text.</summary>
        Warning = 2,
    }

    /// <summary>Một mẩu của câu "Mỗi [7 ngày], neo từ […], mỗi đợt chạy […], id = […] + số thứ tự." — chữ trơn hoặc token bấm được.</summary>
    internal sealed class RecurringSentenceToken
    {
        internal RecurringSentenceToken(string text, string fieldName, RecurringTokenState state, bool isMono)
        {
            Text = text ?? string.Empty;
            FieldName = fieldName ?? string.Empty;
            State = state;
            IsMono = isMono;
        }

        public string Text { get; }

        /// <summary>"" = chữ trơn; khác rỗng = token, bấm vào thì focus đúng field đó.</summary>
        public string FieldName { get; }

        public bool IsToken => FieldName.Length > 0;
        public RecurringTokenState State { get; }

        /// <summary>Token tiền tố id in mono vì đó là chuỗi máy đọc, không phải chữ người đọc.</summary>
        public bool IsMono { get; }
    }

    /// <summary>Một hàng của bảng "n đợt kế tiếp" ([SD1 §4.1]).</summary>
    internal sealed class RecurringOccurrenceRow
    {
        internal RecurringOccurrenceRow(string eventId, string publishedEventId, DateTime startUtc, DateTime endUtc,
            LiveEventPhase phase, string startText, string endText, string deviceTimeText, string nowText)
        {
            EventId = eventId ?? string.Empty;
            PublishedEventId = publishedEventId ?? string.Empty;
            StartUtc = startUtc;
            EndUtc = endUtc;
            Phase = phase;
            StartText = startText ?? string.Empty;
            EndText = endText ?? string.Empty;
            DeviceTimeText = deviceTimeText ?? string.Empty;
            NowText = nowText ?? string.Empty;
        }

        public string EventId { get; }

        /// <summary>Id người chơi đang giữ (bản đã đăng); "" khi không đổi — cột Id in "cũ → mới" bằng ba Label.</summary>
        public string PublishedEventId { get; }

        public bool IdChanges => PublishedEventId.Length > 0;
        public DateTime StartUtc { get; }
        public DateTime EndUtc { get; }
        public LiveEventPhase Phase { get; }
        public string StartText { get; }
        public string EndText { get; }
        public string DeviceTimeText { get; }

        /// <summary>Cột "Lúc này": "Đang chạy" / "Sắp tới · sau 15 giờ 13 phút" / "Đã khép".</summary>
        public string NowText { get; }
    }

    /// <summary>
    /// Model thuần của màn Luật lặp (mục 3, 7.4): từ phiên lịch + nháp tại ô ra câu token, câu chu kỳ, bảng đợt kế tiếp, chỉ số
    /// mẫu, lỗi field và hai câu chú thích (neo giữa ngày, hậu quả còn lại sau khi ghi). View chỉ vẽ lại những gì ở đây.
    /// <para>
    /// Luật hiện trên form = luật trong tài liệu GHÉP nháp tại ô (nếu nháp thuộc đúng loại này): thiết kế nói rõ nháp phải hiện
    /// ngay trong câu đọc và bảng đợt kế tiếp ("theo nháp"), trong khi Main.asset chưa đổi.
    /// </para>
    /// </summary>
    internal sealed class RecurringRuleModel
    {
        /// <summary>Card mở ra với 5 đợt ([SD1 §4.1]); nút "Thêm 5" nối thêm từng 5 lần.</summary>
        internal const int DefaultOccurrenceCount = 5;

        internal const int OccurrenceCountStep = 5;

        /// <summary>Trần 50 đợt: quá số này bảng dài hơn màn hình mà không nói thêm gì.</summary>
        internal const int MaximumOccurrenceCount = 50;

        private readonly Dictionary<string, string> _fieldErrors;

        private RecurringRuleModel(string eventType, RecurringLiveEventRule rule, RecurringLiveEventRule writtenRule, int colorSlot,
            IReadOnlyList<RecurringSentenceToken> sentenceTokens, string cycleText, IReadOnlyList<RecurringOccurrenceRow> nextOccurrences,
            int presetIndex, Dictionary<string, string> fieldErrors, string anchorNotice, string anchorDeviceLine, string afterWriteNotice,
            string afterWriteRevertPrefix)
        {
            EventType = eventType ?? string.Empty;
            Rule = rule;
            WrittenRule = writtenRule;
            ColorSlot = colorSlot;
            SentenceTokens = sentenceTokens;
            CycleText = cycleText ?? string.Empty;
            NextOccurrences = nextOccurrences;
            PresetIndex = presetIndex;
            _fieldErrors = fieldErrors;
            AnchorNotice = anchorNotice ?? string.Empty;
            AnchorDeviceLine = anchorDeviceLine ?? string.Empty;
            AfterWriteNotice = afterWriteNotice ?? string.Empty;
            AfterWriteRevertPrefix = afterWriteRevertPrefix ?? string.Empty;
        }

        /// <param name="session">Phiên lịch — tài liệu nháp, bản so đã đăng và đồng hồ.</param>
        /// <param name="eventType">Loại của luật đang chọn; "" hoặc loại không có luật → model rỗng (<see cref="HasRule"/> false).</param>
        /// <param name="draft">Nháp tại ô đang mở (<see cref="RecurringPrefixDraft.None"/> khi không có).</param>
        /// <param name="occurrenceCount">Số đợt của bảng; kẹp vào [1, <see cref="MaximumOccurrenceCount"/>].</param>
        public static RecurringRuleModel Build(LiveOpsHubCalendarSession session, string eventType, RecurringPrefixDraft draft,
            int occurrenceCount, LiveOpsHubFormat format)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (format == null) throw new ArgumentNullException(nameof(format));
            RecurringPrefixDraft activeDraft = draft ?? RecurringPrefixDraft.None;
            LiveEventCalendarDocument document = session.Document;
            DateTime nowUtc = session.Clock.UtcNow;

            RecurringLiveEventRule writtenRule;
            if (string.IsNullOrEmpty(eventType) || document == null || !document.TryGetRecurringRule(eventType, out writtenRule))
            {
                return Empty(eventType);
            }

            // Nháp chỉ ghép khi nó thuộc đúng luật đang xem: nháp của luật khác không được đổi câu đọc ở đây.
            RecurringLiveEventRule rule = activeDraft.HasDraft && string.Equals(activeDraft.EventType, eventType, StringComparison.Ordinal)
                ? activeDraft.DraftRule
                : writtenRule;

            int colorSlot = ColorSlotOf(document, eventType);
            Dictionary<string, string> fieldErrors = BuildFieldErrors(rule);
            LiveEventCalendarDocument baseline = session.Publish != null ? session.Publish.ActiveBaseline : null;

            string afterWriteRevertPrefix = string.Empty;
            string afterWriteNotice = BuildAfterWriteNotice(writtenRule, baseline, nowUtc, activeDraft, eventType, format, out afterWriteRevertPrefix);
            bool prefixWarns = afterWriteNotice.Length > 0
                || (activeDraft.HasDraft && string.Equals(activeDraft.EventType, eventType, StringComparison.Ordinal) && activeDraft.ChangesRunningId);

            IReadOnlyList<RecurringSentenceToken> tokens = BuildSentence(rule, format, prefixWarns);
            string cycleText = BuildCycleText(rule, format);
            IReadOnlyList<RecurringOccurrenceRow> occurrences = BuildOccurrences(rule, baseline, eventType, nowUtc, occurrenceCount, format);
            int presetIndex = LiveOpsRulePresets.IndexMatching(rule, LiveOpsRulePresets.Resolve());
            string anchorNotice = BuildAnchorNotice(rule);
            string anchorDeviceLine = BuildAnchorDeviceLine(rule, format);

            return new RecurringRuleModel(eventType, rule, writtenRule, colorSlot, tokens, cycleText, occurrences, presetIndex,
                fieldErrors, anchorNotice, anchorDeviceLine, afterWriteNotice, afterWriteRevertPrefix);
        }

        /// <summary>Model của "chưa chọn luật nào" — view dùng để hiện trạng thái trống mà không phải kiểm null ở từng chỗ.</summary>
        public static RecurringRuleModel Empty(string eventType)
        {
            return new RecurringRuleModel(eventType, null, null, 0, Array.Empty<RecurringSentenceToken>(), string.Empty,
                Array.Empty<RecurringOccurrenceRow>(), LiveOpsRulePresets.CustomIndex, new Dictionary<string, string>(StringComparer.Ordinal),
                string.Empty, string.Empty, string.Empty, string.Empty);
        }

        public string EventType { get; }

        /// <summary>Luật form đang hiện = luật trong tài liệu ghép nháp tại ô; null khi không có luật.</summary>
        public RecurringLiveEventRule Rule { get; }

        /// <summary>Luật thật trong Main.asset (chưa ghép nháp) — nút "Hoàn về…" và câu "vẫn thấy …" so với cái này.</summary>
        public RecurringLiveEventRule WrittenRule { get; }

        public bool HasRule => Rule != null;
        public int ColorSlot { get; }
        public IReadOnlyList<RecurringSentenceToken> SentenceTokens { get; }
        public string CycleText { get; }
        public IReadOnlyList<RecurringOccurrenceRow> NextOccurrences { get; }

        /// <summary><see cref="LiveOpsRulePresets.CustomIndex"/> = "Tùy chỉnh…".</summary>
        public int PresetIndex { get; }

        public string AnchorNotice { get; }

        /// <summary>
        /// Chữ phụ của ô Neo: "thứ Hai · 07:00 5/1 giờ máy" ([SD1 §4.1]). Thứ trong tuần đứng trước vì neo tuần là thứ người
        /// vận hành nghĩ theo ("thứ Hai"), còn giờ máy là thứ họ đối chiếu với đồng hồ trước mặt. "" khi neo không đọc được.
        /// </summary>
        public string AnchorDeviceLine { get; }

        /// <summary>
        /// Vì sao bảng "đợt kế tiếp" trống. Luật lỗi = game bỏ hẳn luật, nên 0 đợt là HẬU QUẢ chứ không phải kết quả đạt
        /// (mục 7: "trống = vì sao · trống không phải đạt"). "" khi bảng trống vì lý do khác hoặc không trống.
        /// </summary>
        public string OccurrencesEmptyReason
        {
            get
            {
                return HasRule && !IsValid && NextOccurrences.Count == 0 ? LiveOpsHubStrings.RecurringOccurrencesEmptyReason : string.Empty;
            }
        }

        /// <summary>HelpBox ở lại dưới ô tiền tố tới khi lần lặp cũ khép (mục 7.4); "" khi không còn hậu quả nào.</summary>
        public string AfterWriteNotice { get; }

        /// <summary>Tiền tố của bản đã đăng, cho nút "Hoàn về tiền tố …"; "" khi không có gì để hoàn.</summary>
        public string AfterWriteRevertPrefix { get; }

        /// <summary>Luật ghi ra được: mọi field hợp lệ (game sẽ không bỏ luật này).</summary>
        public bool IsValid => HasRule && _fieldErrors.Count == 0;

        /// <summary>Lỗi của một field (<see cref="RecurringRuleFields"/>); "" khi field đó không lỗi.</summary>
        public string FieldErrorText(string fieldName)
        {
            string error;
            return fieldName != null && _fieldErrors.TryGetValue(fieldName, out error) ? error : string.Empty;
        }

        private static int ColorSlotOf(LiveEventCalendarDocument document, string eventType)
        {
            LiveEventTypeDefinition definition;
            return document.TryGetEventType(eventType, out definition) ? definition.ColorSlot : 0;
        }

        private static Dictionary<string, string> BuildFieldErrors(RecurringLiveEventRule rule)
        {
            Dictionary<string, string> errors = new Dictionary<string, string>(StringComparer.Ordinal);
            DateTime anchorUtc;
            if (!rule.TryGetAnchorUtc(out anchorUtc)) errors.Add(RecurringRuleFields.Anchor, LiveOpsHubStrings.RecurringAnchorUnreadableError);
            if (rule.PeriodHours <= 0) errors.Add(RecurringRuleFields.PeriodHours, LiveOpsHubStrings.RecurringPeriodMustBePositiveError);
            if (rule.ActiveHours <= 0)
            {
                errors.Add(RecurringRuleFields.ActiveHours, LiveOpsHubStrings.RecurringActiveMustBePositiveError);
            }
            else if (rule.PeriodHours > 0 && rule.ActiveHours > rule.PeriodHours)
            {
                // Runtime ném "Thời gian chạy phải trong (0, chu kỳ]" nên parser 0.2.0 bỏ cả luật — nói hậu quả, không nói ràng buộc.
                errors.Add(RecurringRuleFields.ActiveHours, LiveOpsHubStrings.RecurringActiveLongerThanPeriodError);
            }
            return errors;
        }

        private static IReadOnlyList<RecurringSentenceToken> BuildSentence(RecurringLiveEventRule rule, LiveOpsHubFormat format, bool prefixWarns)
        {
            List<RecurringSentenceToken> tokens = new List<RecurringSentenceToken>();
            tokens.Add(Text(LiveOpsHubStrings.RecurringSentenceEveryPrefix));
            tokens.Add(Token(PeriodText(rule.PeriodHours, format), RecurringRuleFields.PeriodHours, RecurringTokenState.Normal, false));
            tokens.Add(Text(LiveOpsHubStrings.RecurringSentenceAnchorPrefix));
            tokens.Add(Token(AnchorText(rule, format), RecurringRuleFields.Anchor, RecurringTokenState.Normal, false));
            tokens.Add(Text(LiveOpsHubStrings.RecurringSentenceActivePrefix));
            tokens.Add(Token(HoursText(rule.ActiveHours, format), RecurringRuleFields.ActiveHours, RecurringTokenState.Normal, false));
            tokens.Add(Text(LiveOpsHubStrings.RecurringSentenceIdPrefix));
            tokens.Add(Token(rule.EffectiveIdPrefix, RecurringRuleFields.IdPrefix,
                prefixWarns ? RecurringTokenState.Warning : RecurringTokenState.Normal, true));
            tokens.Add(Text(LiveOpsHubStrings.RecurringSentenceIdSuffix));
            return tokens;
        }

        private static RecurringSentenceToken Text(string text)
        {
            return new RecurringSentenceToken(text, string.Empty, RecurringTokenState.Normal, false);
        }

        private static RecurringSentenceToken Token(string text, string fieldName, RecurringTokenState state, bool isMono)
        {
            return new RecurringSentenceToken(text, fieldName, state, isMono);
        }

        /// <summary>
        /// (UX-32, T6) NHỊP của luật quy đổi bằng ĐÚNG luật của header làn màn Lịch
        /// (<c>LiveOpsTimelineModel.HoursText</c>): chỉ đọc theo ngày khi chẵn ngày VÀ dài hơn một ngày. Nhờ vậy 168 giờ
        /// đọc "7 ngày" ở cả hai màn, còn 24 giờ đọc "24 giờ" ở cả hai màn — đó mới là cái T6 đòi.
        /// <para>
        /// Gốc thật của T6 là HAI NGƯỠNG khác nhau: <see cref="LiveOpsHubFormat.Duration"/> đổi sang ngày từ ĐỦ 24 giờ,
        /// header làn thì phải HƠN 24 giờ. Không gọi thẳng hàm bên Timeline được — nó private và file đó thuộc gói khác —
        /// nên luật được chép lại ở đây, và <c>RecurringRuleModelTests.PeriodText_ReadsSameUnitAsTimelineLaneHeader</c>
        /// so trực tiếp chuỗi của hai màn để hai bản chép không trôi khỏi nhau.
        /// </para>
        /// </summary>
        internal static string PeriodText(int hours, LiveOpsHubFormat format)
        {
            if (hours <= 0) return format.Integer(hours);
            if (hours > HoursPerDay && hours % HoursPerDay == 0) return format.Duration(TimeSpan.FromHours(hours), false);
            // Qua Catalog.Format chứ không qua string.Format: khoá này mang dấu số nhiều {0|hour|hours} của bản tiếng Anh,
            // và dấu đó chỉ được giải khi đi đúng đường (EnglishPluralTests gác chỗ này).
            return LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.RecurringHoursFormat), format.Integer(hours));
        }

        /// <summary>Ngưỡng quy đổi giờ → ngày, dùng chung với header làn của màn Lịch.</summary>
        private const int HoursPerDay = 24;

        /// <summary>
        /// (UX-25) Id hiển thị trong một câu: gạch nối thường là chỗ UI Toolkit được phép ngắt dòng, nên "weekly-pass-35"
        /// vỡ thành "weekly-" / "pass-35" giữa HelpBox và người đọc tưởng đó là hai id. U+2011 là gạch nối KHÔNG ngắt —
        /// nhìn y hệt, chỉ khác ở chỗ được phép xuống dòng hay không.
        /// </summary>
        internal static string NonBreakingId(string id)
        {
            return string.IsNullOrEmpty(id) ? string.Empty : id.Replace('-', NonBreakingHyphen);
        }

        private const char NonBreakingHyphen = '\u2011';

        /// <summary>"7 ngày" / "20 giờ" — giờ âm hoặc 0 giữ nguyên số để câu đọc không nói dối khi luật đang hỏng.</summary>
        internal static string HoursText(int hours, LiveOpsHubFormat format)
        {
            if (hours <= 0) return format.Integer(hours);
            return format.Duration(TimeSpan.FromHours(hours), false);
        }

        /// <summary>"thứ Hai 5/1/2026 00:00 UTC"; neo không đọc được thì in nguyên văn chữ trong asset.</summary>
        internal static string AnchorText(RecurringLiveEventRule rule, LiveOpsHubFormat format)
        {
            DateTime anchorUtc;
            if (!rule.TryGetAnchorUtc(out anchorUtc)) return rule.AnchorUtcText;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringAnchorTokenFormat,
                format.DayOfWeek(anchorUtc), format.DateWithYear(anchorUtc), anchorUtc.ToString("HH:mm", CultureInfo.InvariantCulture),
                LiveOpsHubStrings.UtcLabel);
        }

        private static string BuildCycleText(RecurringLiveEventRule rule, LiveOpsHubFormat format)
        {
            if (rule.PeriodHours <= 0 || rule.ActiveHours <= 0) return string.Empty;
            string activeText = HoursText(rule.ActiveHours, format);
            if (rule.ActiveHours > rule.PeriodHours)
            {
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringCycleOverflowFormat,
                    activeText, HoursText(rule.PeriodHours, format));
            }
            if (rule.ActiveHours == rule.PeriodHours)
            {
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringCycleSeamlessFormat, activeText);
            }
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringCycleWithRestFormat,
                activeText, HoursText(rule.PeriodHours - rule.ActiveHours, format));
        }

        private static string BuildAnchorDeviceLine(RecurringLiveEventRule rule, LiveOpsHubFormat format)
        {
            DateTime anchorUtc;
            if (!rule.TryGetAnchorUtc(out anchorUtc)) return string.Empty;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringAnchorDeviceLineFormat,
                format.DayOfWeek(anchorUtc), format.DeviceTimeLine(anchorUtc));
        }

        private static string BuildAnchorNotice(RecurringLiveEventRule rule)
        {
            DateTime anchorUtc;
            if (!rule.TryGetAnchorUtc(out anchorUtc)) return string.Empty;
            return anchorUtc.TimeOfDay == TimeSpan.Zero ? string.Empty : LiveOpsHubStrings.RecurringAnchorNotice;
        }

        private static IReadOnlyList<RecurringOccurrenceRow> BuildOccurrences(RecurringLiveEventRule rule,
            LiveEventCalendarDocument baseline, string eventType, DateTime nowUtc, int occurrenceCount, LiveOpsHubFormat format)
        {
            int count = occurrenceCount < 1 ? 1 : (occurrenceCount > MaximumOccurrenceCount ? MaximumOccurrenceCount : occurrenceCount);
            IReadOnlyList<LiveEventInstance> instances = RecurringOccurrences.Next(rule, nowUtc, count);
            RecurringLiveEventRule baselineRule = null;
            if (baseline != null) baseline.TryGetRecurringRule(eventType, out baselineRule);

            List<RecurringOccurrenceRow> rows = new List<RecurringOccurrenceRow>(instances.Count);
            for (int index = 0; index < instances.Count; index++)
            {
                LiveEventInstance instance = instances[index];
                LiveEventPhase phase = instance.PhaseAt(nowUtc);
                rows.Add(new RecurringOccurrenceRow(instance.EventId, PublishedIdOf(baselineRule, instance, nowUtc, phase),
                    instance.StartUtc, instance.EndUtc, phase, format.ShortDateTime(instance.StartUtc),
                    format.ShortDateTime(instance.EndUtc), format.DeviceClock(instance.StartUtc), NowTextOf(phase, instance, nowUtc, format)));
            }
            return rows;
        }

        /// <summary>
        /// Id người chơi đang giữ, CHỈ cho lần lặp đang chạy: đó là lần duy nhất có người đang chơi dở, nên chỉ ở đó "cũ → mới"
        /// mới là một mất mát. Đợt sắp tới đổi id thì chưa ai có gì để mất — in một id là đủ ([SD1 §4.1]).
        /// </summary>
        private static string PublishedIdOf(RecurringLiveEventRule baselineRule, LiveEventInstance instance, DateTime nowUtc, LiveEventPhase phase)
        {
            if (baselineRule == null || phase != LiveEventPhase.Active) return string.Empty;
            LiveEventInstance published;
            if (!RecurringOccurrences.TryGetOccurrenceAt(baselineRule, nowUtc, out published)) return string.Empty;
            return string.Equals(published.EventId, instance.EventId, StringComparison.Ordinal) ? string.Empty : published.EventId;
        }

        private static string NowTextOf(LiveEventPhase phase, LiveEventInstance instance, DateTime nowUtc, LiveOpsHubFormat format)
        {
            switch (phase)
            {
                case LiveEventPhase.Active:
                    return LiveOpsHubStrings.RecurringPhaseRunning;
                case LiveEventPhase.Ended:
                    return LiveOpsHubStrings.RecurringPhaseEnded;
                default:
                    return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringPhaseWithRelativeFormat,
                        LiveOpsHubStrings.RecurringPhaseUpcoming, format.Relative(nowUtc, instance.StartUtc));
            }
        }

        /// <summary>
        /// (UX-32) Có câu "sau khi ghi" còn treo cho loại này hay không — hàng trong pane trái hỏi cái này để đừng nói
        /// "không sao" trong lúc form ngay cạnh đang nói "vẫn còn weekly-pass-35 đang chạy".
        /// <para>
        /// Hàng danh sách KHÔNG im khi đang có nháp (khác <see cref="BuildAfterWriteNotice"/>, chỗ đó im vì khối nháp đã
        /// là câu đang nói ngay dưới ô): pane trái ở xa khối nháp, bỏ dấu đi là hàng nói "không sao" trong lúc đợt cũ vẫn
        /// đang chạy. Vì thế nó hỏi thẳng phần QUYẾT ĐỊNH, không dựng câu — gọi mỗi lần vẽ danh sách vẫn rẻ.
        /// </para>
        /// </summary>
        internal static bool HasAfterWriteNotice(LiveOpsHubCalendarSession session, string eventType)
        {
            if (session == null || string.IsNullOrEmpty(eventType)) return false;
            LiveEventCalendarDocument document = session.Document;
            RecurringLiveEventRule writtenRule;
            if (document == null || !document.TryGetRecurringRule(eventType, out writtenRule)) return false;
            LiveEventCalendarDocument baseline = session.Publish != null ? session.Publish.ActiveBaseline : null;
            RecurringLiveEventRule baselineRule;
            LiveEventInstance published;
            LiveEventInstance current;
            return TryGetAfterWriteMismatch(writtenRule, baseline, session.Clock.UtcNow, eventType,
                out baselineRule, out published, out current);
        }

        /// <summary>
        /// Phần QUYẾT ĐỊNH của câu "sau khi ghi", tách khỏi phần dựng chữ: bản đã đăng còn một lần lặp đang chạy mang id
        /// CŨ, mà luật trong Main.asset nay sinh id khác cho đúng lúc đó. Trả về hai lần lặp để nơi gọi khỏi tính lại —
        /// <see cref="HasAfterWriteNotice"/> chỉ cần câu trả lời có/không nên không trả giá cho việc format chuỗi.
        /// </summary>
        private static bool TryGetAfterWriteMismatch(RecurringLiveEventRule writtenRule, LiveEventCalendarDocument baseline,
            DateTime nowUtc, string eventType, out RecurringLiveEventRule baselineRule, out LiveEventInstance published,
            out LiveEventInstance current)
        {
            baselineRule = null;
            published = null;
            current = null;
            if (baseline == null || !baseline.TryGetRecurringRule(eventType, out baselineRule)) return false;
            if (!RecurringOccurrences.TryGetOccurrenceAt(baselineRule, nowUtc, out published)) return false;
            if (!RecurringOccurrences.TryGetOccurrenceAt(writtenRule, nowUtc, out current)) return false;
            return !string.Equals(published.EventId, current.EventId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Câu ở lại sau khi đã ghi: bản đã đăng còn một lần lặp đang chạy mang id CŨ, mà luật trong Main.asset nay sinh id
        /// khác cho đúng lúc đó. Câu biến mất khi lần lặp cũ khép — không cần ai bấm tắt (mục 7.4).
        /// Đang có nháp tại ô của chính luật này thì im: lúc đó khối cảnh báo của nháp mới là câu đang nói.
        /// </summary>
        private static string BuildAfterWriteNotice(RecurringLiveEventRule writtenRule, LiveEventCalendarDocument baseline,
            DateTime nowUtc, RecurringPrefixDraft draft, string eventType, LiveOpsHubFormat format, out string revertPrefix)
        {
            revertPrefix = string.Empty;
            if (draft.HasDraft && string.Equals(draft.EventType, eventType, StringComparison.Ordinal)) return string.Empty;
            RecurringLiveEventRule baselineRule;
            LiveEventInstance published;
            LiveEventInstance current;
            if (!TryGetAfterWriteMismatch(writtenRule, baseline, nowUtc, eventType, out baselineRule, out published, out current))
            {
                return string.Empty;
            }

            revertPrefix = baselineRule.EffectiveIdPrefix;
            // Câu ở lại là HelpBox đụng đợt đang chạy nên phải mang cả hai mệnh đề của PD-17 như câu nháp và câu hộp (mục 7.0).
            // (UX-25) Id in bằng gạch nối KHÔNG ngắt: HelpBox xuống dòng giữa "weekly-" và "pass-35" thì người đọc
            // thấy hai mảnh và tưởng đó là hai id khác nhau. Thân hộp xác nhận thì KHÔNG dùng — ở đó người dùng phải gõ
            // lại đúng id, mà U+2011 copy ra không khớp với id thật (đánh đổi ghi ở mục 14 kế hoạch).
            return RecurringPrefixDraft.WithPlayerCountCaveat(string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.RecurringAfterWriteNoticeFormat, NonBreakingId(published.EventId),
                format.ShortDateTimeUtc(published.EndUtc), NonBreakingId(current.EventId)));
        }
    }

    /// <summary>
    /// Sinh lần lặp từ một luật bằng ĐÚNG lớp lịch của game (<see cref="RecurringLiveEventCalendar"/>) — hub không tự nhân
    /// chu kỳ, vì id và mốc phải trùng từng ký tự với thứ game sẽ tính. Luật không hợp lệ (neo hỏng, chu kỳ ≤ 0, chạy ngoài
    /// (0, chu kỳ], loại có ký tự cấm) thì lớp lịch ném — ở đây trả rỗng: game cũng sẽ bỏ luật đó, nên bảng "đợt kế tiếp"
    /// phải trống chứ không được vẽ ra những đợt sẽ không bao giờ chạy.
    /// </summary>
    internal static class RecurringOccurrences
    {
        public static bool TryBuildCalendar(RecurringLiveEventRule rule, out RecurringLiveEventCalendar calendar)
        {
            calendar = null;
            if (rule == null) return false;
            DateTime anchorUtc;
            if (!rule.TryGetAnchorUtc(out anchorUtc)) return false;
            if (rule.PeriodHours <= 0 || rule.ActiveHours <= 0 || rule.ActiveHours > rule.PeriodHours) return false;
            try
            {
                calendar = new RecurringLiveEventCalendar(rule.EventType, anchorUtc, TimeSpan.FromHours(rule.PeriodHours),
                    TimeSpan.FromHours(rule.ActiveHours), rule.EffectiveIdPrefix, rule.ConfigKey);
                return true;
            }
            catch (ArgumentException)
            {
                // Loại rỗng hay chứa ký tự cấm: LiveEventInstance.ValidateIdentifier ném — cùng nghĩa "game sẽ bỏ luật này".
                calendar = null;
                return false;
            }
        }

        /// <summary>Lần lặp đang chạy tại <paramref name="utc"/> (start ≤ utc &lt; end); false khi đang ở khoảng nghỉ.</summary>
        public static bool TryGetOccurrenceAt(RecurringLiveEventRule rule, DateTime utc, out LiveEventInstance occurrence)
        {
            occurrence = null;
            RecurringLiveEventCalendar calendar;
            if (!TryBuildCalendar(rule, out calendar)) return false;
            LiveEventInstance candidate = calendar.GetOccurrence(calendar.OccurrenceIndexAt(utc));
            if (candidate.PhaseAt(utc) != LiveEventPhase.Active) return false;
            occurrence = candidate;
            return true;
        }

        /// <summary>
        /// Lần lặp của CHU KỲ đang bao <paramref name="utc"/> — kể cả khi nó đã khép trước đó (đang ở khoảng nghỉ). Khác
        /// <see cref="TryGetOccurrenceAt"/> ở đúng chỗ đó: rút ngắn thời gian chạy làm đợt khép trước "lúc này", và câu hộp
        /// vẫn phải nêu được giờ khép mới của chính đợt ấy.
        /// </summary>
        public static bool TryGetOccurrenceOfCycleAt(RecurringLiveEventRule rule, DateTime utc, out LiveEventInstance occurrence)
        {
            occurrence = null;
            RecurringLiveEventCalendar calendar;
            if (!TryBuildCalendar(rule, out calendar)) return false;
            try
            {
                occurrence = calendar.GetOccurrence(calendar.OccurrenceIndexAt(utc));
            }
            catch (ArgumentOutOfRangeException)
            {
                // Chu kỳ khổng lồ đẩy lần lặp ra ngoài khoảng DateTime — cùng nghĩa "không tính được", không ném lên view.
                return false;
            }
            return true;
        }

        /// <summary>
        /// Lần lặp đang chạy (nếu có) rồi tới các lần kế tiếp, tổng <paramref name="count"/> đợt — đúng thứ tự bảng của thiết kế,
        /// bắt đầu bằng "Đang chạy".
        /// </summary>
        public static IReadOnlyList<LiveEventInstance> Next(RecurringLiveEventRule rule, DateTime fromUtc, int count)
        {
            List<LiveEventInstance> instances = new List<LiveEventInstance>();
            RecurringLiveEventCalendar calendar;
            if (count <= 0 || !TryBuildCalendar(rule, out calendar)) return instances;
            long index = calendar.OccurrenceIndexAt(fromUtc);
            while (instances.Count < count)
            {
                LiveEventInstance occurrence;
                try
                {
                    occurrence = calendar.GetOccurrence(index);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Chu kỳ khổng lồ đẩy lần lặp ra ngoài khoảng DateTime: dừng ở những đợt còn tính được, không ném lên view.
                    break;
                }
                // Đang ở khoảng nghỉ: lần lặp của chu kỳ hiện tại đã khép, bảng bắt đầu từ lần sau.
                if (occurrence.EndUtc > fromUtc) instances.Add(occurrence);
                index++;
            }
            return instances;
        }
    }
}
