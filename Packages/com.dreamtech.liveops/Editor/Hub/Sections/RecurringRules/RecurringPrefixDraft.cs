using System;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Nháp tại ô của màn Luật lặp ([SD1 §4.2], mục 7.4) — lớp thuần, không đụng UI. Gõ tiền tố / neo / chu kỳ / thời gian chạy
    /// làm đổi id hay giờ khép của lần lặp ĐANG CHẠY thì giá trị mới chưa được ghi: nó sống ở đây, form vẽ viền warning + câu
    /// hậu quả, và Main.asset (nên cả Lịch, Kiểm lịch, Xuất JSON) vẫn thấy giá trị cũ tới khi người dùng đi hết bước hai.
    /// <para>
    /// Mức xác nhận KHÔNG tự nghĩ ra ở đây: gọi <see cref="LiveOpsConfirmationPolicy.Decide"/> như mọi màn khác (PD-24) —
    /// đổi id lần lặp đang chạy = cấp 2 gõ tên, rút ngắn thời gian chạy = cấp 1, không ai đang chạy = ghi thẳng. Nhờ đó luật
    /// C-6 ("lấy lần lặp đang chạy từ BẢN ĐÃ ĐĂNG nếu có") nằm ở đúng một chỗ cho cả Lịch lẫn màn này.
    /// </para>
    /// </summary>
    internal sealed class RecurringPrefixDraft
    {
        private static readonly RecurringPrefixDraft NoDraft = new RecurringPrefixDraft();

        private readonly LiveOpsConfirmDecision _decision;
        private readonly LiveOpsHubFormat _format;
        private readonly string _assetFileName;
        private readonly string _writtenIdPrefix;

        private RecurringPrefixDraft()
        {
            EventType = string.Empty;
            FieldName = string.Empty;
            _assetFileName = string.Empty;
            _writtenIdPrefix = string.Empty;
            RunningEventId = string.Empty;
            NewRunningEventId = string.Empty;
        }

        private RecurringPrefixDraft(string fieldName, RecurringLiveEventRule draftRule, RecurringLiveEventRule writtenRule,
            LiveOpsConfirmDecision decision, LiveEventInstance newRunning, string assetFileName, LiveOpsHubFormat format)
        {
            FieldName = fieldName;
            DraftRule = draftRule;
            WrittenRule = writtenRule;
            EventType = draftRule.EventType;
            _decision = decision;
            _format = format;
            _assetFileName = assetFileName ?? string.Empty;
            _writtenIdPrefix = writtenRule.EffectiveIdPrefix;
            RunningEventId = decision.RunningEventId;
            RunningEndUtc = decision.RunningEndUtc;
            NewRunningEventId = newRunning != null ? newRunning.EventId : string.Empty;
            NewRunningEndUtc = newRunning != null ? newRunning.EndUtc : (DateTime?)null;
        }

        /// <summary>Không có nháp nào đang mở.</summary>
        public static RecurringPrefixDraft None => NoDraft;

        /// <summary>
        /// Dựng nháp cho một giá trị vừa gõ. Luôn trả về một đối tượng: <see cref="NeedsConfirmation"/> false nghĩa là không
        /// ai đang chạy bị ảnh hưởng nên nơi gọi ghi ngay (mục 7.4), true nghĩa là phải giữ nháp tại ô rồi mới hỏi.
        /// </summary>
        /// <param name="draftDocument">Nháp TRƯỚC khi sửa — "đang chạy" luôn tính trên bản trước khi sửa (PD-24).</param>
        /// <param name="publishedBaseline">Bản so đã đăng; null = chưa có dấu đã đăng.</param>
        /// <param name="assetFileName">Tên file lịch để câu "chưa ghi vào Main.asset" nói đúng file người dùng đang mở.</param>
        /// <param name="fieldName">Một hằng <see cref="RecurringRuleFields"/> — ô nào đang giữ nháp.</param>
        /// <param name="draftRule">Luật SAU khi áp giá trị vừa gõ.</param>
        public static RecurringPrefixDraft For(LiveEventCalendarDocument draftDocument, LiveEventCalendarDocument publishedBaseline,
            DateTime nowUtc, string assetFileName, string fieldName, RecurringLiveEventRule draftRule, LiveOpsHubFormat format)
        {
            if (draftDocument == null) throw new ArgumentNullException(nameof(draftDocument));
            if (draftRule == null) throw new ArgumentNullException(nameof(draftRule));
            if (format == null) throw new ArgumentNullException(nameof(format));

            RecurringLiveEventRule writtenRule;
            if (!draftDocument.TryGetRecurringRule(draftRule.EventType, out writtenRule)) return None;

            LiveEventCalendarDocument documentAfter;
            if (!LiveEventCalendarEdits.TryApply(draftDocument, new SetRecurringRuleEdit(draftRule), out documentAfter)) return None;

            LiveOpsEditOperation operation = string.Equals(fieldName, RecurringRuleFields.ActiveHours, StringComparison.Ordinal)
                ? LiveOpsEditOperation.ChangeRecurringActiveHours
                : LiveOpsEditOperation.ChangeRecurringIdentity;
            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(operation, draftDocument, documentAfter,
                publishedBaseline, nowUtc, draftRule.EventType);

            LiveEventInstance newRunning;
            RecurringOccurrences.TryGetOccurrenceAt(draftRule, nowUtc, out newRunning);
            return new RecurringPrefixDraft(fieldName ?? string.Empty, draftRule, writtenRule, decision, newRunning, assetFileName, format);
        }

        public bool HasDraft => DraftRule != null;

        /// <summary>Loại của luật đang giữ nháp; "" khi không có nháp.</summary>
        public string EventType { get; }

        /// <summary>Ô đang giữ nháp (<see cref="RecurringRuleFields"/>).</summary>
        public string FieldName { get; }

        /// <summary>Luật sau khi áp giá trị vừa gõ — form và bảng "đợt kế tiếp" hiện cái này, asset thì chưa.</summary>
        public RecurringLiveEventRule DraftRule { get; }

        /// <summary>Luật đang nằm trong asset — nút "Huỷ (Esc)" quay về đây.</summary>
        public RecurringLiveEventRule WrittenRule { get; }

        public LiveOpsConfirmRequirement Requirement => _decision != null ? _decision.Requirement : LiveOpsConfirmRequirement.None;

        /// <summary>Phải đi qua bước hai (giữ nháp tại ô rồi mới hỏi) hay ghi được ngay.</summary>
        public bool NeedsConfirmation => HasDraft && Requirement != LiveOpsConfirmRequirement.None;

        /// <summary>Hậu quả là ĐỔI ID của lần lặp đang chạy (khác với chỉ rút ngắn giờ khép).</summary>
        public bool ChangesRunningId => NeedsConfirmation && Requirement == LiveOpsConfirmRequirement.TypeToConfirm;

        /// <summary>Id lần lặp người chơi đang giữ — lấy từ bản đã đăng khi có (C-6); "" khi không ai đang chạy.</summary>
        public string RunningEventId { get; }

        public string NewRunningEventId { get; }
        public DateTime? RunningEndUtc { get; }
        public DateTime? NewRunningEndUtc { get; }

        /// <summary>Dòng phụ ngay dưới ô: nói rõ nháp chỉ ở đây, các màn khác vẫn thấy giá trị cũ ([SD1 §4.2]).</summary>
        public string CellNotice
        {
            get
            {
                if (!NeedsConfirmation) return string.Empty;
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringDraftNoticeFormat,
                    _assetFileName, WrittenValueText());
            }
        }

        /// <summary>Câu hậu quả trong HelpBox warning (PD-17: nói người chơi mất gì, và nói thẳng hub không biết số người chơi).</summary>
        public string ConsequenceText
        {
            get
            {
                if (!NeedsConfirmation) return string.Empty;
                if (string.Equals(FieldName, RecurringRuleFields.ActiveHours, StringComparison.Ordinal))
                {
                    return WithPlayerCountCaveat(string.Format(CultureInfo.InvariantCulture,
                        LiveOpsHubStrings.RecurringActiveHoursConsequenceFormat, RunningEventId, NewEndText(), OldEndText()));
                }
                if (string.Equals(FieldName, RecurringRuleFields.IdPrefix, StringComparison.Ordinal))
                {
                    return WithPlayerCountCaveat(string.Format(CultureInfo.InvariantCulture,
                        LiveOpsHubStrings.RecurringPrefixConsequenceFormat, RunningEventId, OldEndText(), NewRunningEventId));
                }
                return WithPlayerCountCaveat(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.RecurringIdentityConsequenceFormat, RunningEventId, OldEndText(), NewRunningEventId));
            }
        }

        /// <summary>Nút danger của hàng nút dưới ô.</summary>
        public string WriteButtonText => string.Equals(FieldName, RecurringRuleFields.IdPrefix, StringComparison.Ordinal)
            ? LiveOpsHubStrings.RecurringDraftWritePrefixButton
            : LiveOpsHubStrings.RecurringDraftWriteValueButton;

        /// <summary>Câu toast = tên Undo group của lệnh ghi (8.5).</summary>
        public string ToastText
        {
            get
            {
                if (!HasDraft) return string.Empty;
                if (string.Equals(FieldName, RecurringRuleFields.IdPrefix, StringComparison.Ordinal))
                {
                    return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringPrefixToastFormat,
                        _writtenIdPrefix, DraftRule.EffectiveIdPrefix);
                }
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringWriteToastFormat, EventType);
            }
        }

        /// <summary>
        /// Hộp của bước hai. Cấp 2 (đổi id) gõ đúng id người chơi đang giữ; cấp 1 (rút ngắn) chỉ nêu giờ khép mới.
        /// Enter trong ô gõ không chạy nút nào — hộp lo phần đó, ở đây chỉ dựng nội dung.
        /// </summary>
        public LiveOpsConfirmRequest BuildConfirmRequest()
        {
            if (!NeedsConfirmation) throw new InvalidOperationException(LiveOpsHubStrings.RecurringErrorNoConfirmationNeeded);
            if (Requirement == LiveOpsConfirmRequirement.Level1)
            {
                return new LiveOpsConfirmRequest.Builder()
                    .WithLevel(LiveOpsConfirmLevel.Level1)
                    .WithTitle(LiveOpsHubStrings.RecurringConfirmActiveTitle)
                    .WithBody(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringConfirmActiveBodyFormat,
                        RunningEventId, NewEndText(), OldEndText()))
                    .WithHelpBoxWarning()
                    .WithKeyHint(LiveOpsHubStrings.RecurringConfirmActiveKeyHint)
                    .WithButtons(LiveOpsHubStrings.RecurringConfirmActiveDestructive, LiveOpsHubStrings.KitConfirmKeepLabel)
                    .Build();
            }

            bool isPrefix = string.Equals(FieldName, RecurringRuleFields.IdPrefix, StringComparison.Ordinal);
            // WithTypeToConfirm tự đặt cấp 2 + HelpBox cảnh báo (8.6) — không gọi WithLevel thêm.
            return new LiveOpsConfirmRequest.Builder()
                .WithTitle(isPrefix ? LiveOpsHubStrings.RecurringConfirmPrefixTitle : LiveOpsHubStrings.RecurringConfirmIdentityTitle)
                .WithBody(WithPlayerCountCaveat(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.RecurringConfirmPrefixBodyFormat, RunningEventId, NewRunningEventId, OldEndText())))
                .WithHelpBoxWarning()
                .WithKeyHint(LiveOpsHubStrings.RecurringConfirmPrefixKeyHint)
                .WithButtons(isPrefix ? LiveOpsHubStrings.RecurringConfirmPrefixDestructive : LiveOpsHubStrings.RecurringConfirmIdentityDestructive,
                    LiveOpsHubStrings.RecurringConfirmPrefixSafe)
                .WithTypeToConfirm(_decision.TypeToConfirmText)
                .Build();
        }

        /// <summary>
        /// Nối hai câu dùng chung của hub (PD-17): hub KHÔNG biết số người chơi toàn cục, và bản này chưa đọc dữ liệu thử.
        /// Nói thẳng chỗ mình không biết còn hơn để người đọc tự suy ra một con số không có thật.
        /// </summary>
        private static string WithPlayerCountCaveat(string sentence)
        {
            return sentence + " " + LiveOpsHubStrings.KitUnknownPlayerCountSentence + " " + LiveOpsHubStrings.KitNoTestDataSentence;
        }

        /// <summary>Giá trị mà các màn khác vẫn thấy (trong asset) — in vào câu "vẫn thấy …".</summary>
        private string WrittenValueText()
        {
            if (string.Equals(FieldName, RecurringRuleFields.IdPrefix, StringComparison.Ordinal)) return WrittenRule.EffectiveIdPrefix;
            if (string.Equals(FieldName, RecurringRuleFields.Anchor, StringComparison.Ordinal)) return WrittenRule.AnchorUtcText;
            if (string.Equals(FieldName, RecurringRuleFields.PeriodHours, StringComparison.Ordinal))
            {
                return RecurringRuleModel.HoursText(WrittenRule.PeriodHours, _format);
            }
            return RecurringRuleModel.HoursText(WrittenRule.ActiveHours, _format);
        }

        private string OldEndText()
        {
            return RunningEndUtc.HasValue ? _format.ShortDateTimeUtc(RunningEndUtc.Value) : string.Empty;
        }

        private string NewEndText()
        {
            return NewRunningEndUtc.HasValue ? _format.ShortDateTimeUtc(NewRunningEndUtc.Value) : string.Empty;
        }
    }
}
