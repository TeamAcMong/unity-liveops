using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// (V-8) Nguồn câu DUY NHẤT cho một phát hiện Kiểm lịch: headline, meta, mảnh tooltip, câu hậu quả, vì sao, chữ nút, lựa chọn sửa,
    /// "cũng sai: …" và hàng luật không ra phát hiện. Core chỉ trả dữ liệu có mã (<c>RuleId</c>, <c>DetailCode</c>, giá trị thô) nên câu
    /// chọn theo cặp mã ở một bảng — rail, timeline, Lịch, Tổng quan, Kiểm lịch, chú thích JSON cùng đọc bảng này thì một phát hiện nói
    /// cùng một câu ở mọi nơi, và thêm mã mới ở core mà quên câu thì <c>EveryRuleIdAndDetailCode_HasSentence</c> đỏ ngay.
    /// <para>Mọi giá trị thô (id, loại, configKey, chuỗi giờ người dùng gõ, message exception) bọc <c>&lt;noparse&gt;</c>: Label rich text
    /// của hub sẽ hiểu "&lt;b&gt;" trong một id là thẻ và làm mất chữ. Nơi hiện chữ không rich text (tooltip) gọi <see cref="PlainText"/>.</para>
    /// <para>(V-22 CC-FT-1) Bản không ngữ cảnh không bao giờ nói sai nhưng bớt chữ, nên nơi gọi có dữ liệu BẮT BUỘC dùng overload có
    /// ngữ cảnh — không thì chữ lệch thiết kế: <c>Headline(…, latestStamp)</c> khi có dấu đã đăng (G-OVERVIEW, G-VALIDATION),
    /// <c>Meta(…, nowUtc)</c> khi có đồng hồ (G-OVERVIEW, G-VALIDATION, G-CALENDAR "Vấn đề (n)", G-CALENDAR-DEPTH),
    /// <c>PrimaryButtonTooltip(…, calendarAssetName)</c> khi biết asset (G-VALIDATION). Tooltip rail/timeline (G-SESSION
    /// <c>LiveOpsHubFindingRouting</c>, G-TIMELINE-VIEW) dùng <see cref="ShortLabel"/> + <see cref="PlainText"/>.</para>
    /// <para>(V-22 CC-FT-2 chọn (a)) Chữ ở đây là chuẩn, thay chữ thiết kế: headline luật 12 đếm "mục" ("khác dấu 11/9 16:20: 2 mục")
    /// vì <c>ExpectedText</c> đếm cả luật lặp, "đợt" có lúc sai; headline luật 8 không mang tiền tố "Kiểm lịch ·" — tiền tố chỉ thuộc
    /// card tham chiếu của màn Loại event (G-EVENTTYPES) và màn đó tự thêm. (Chữ chuẩn thứ ba — hàng diff không giờ — ở LiveOpsChangeText.)</para>
    /// </summary>
    internal static class LiveOpsFindingText
    {
        // Mã lý do của hàng luật không ra phát hiện. Hằng gốc là public const trong lớp luật INTERNAL của core (Editor không có
        // InternalsVisibleTo) nên phải chép chuỗi ở đây; test RemoteDrift_NotMeasuredReasonCodes_HaveSentence chạy luật thật để
        // bắt lệch khi core đổi mã.
        internal const string RemoteNotPastedReasonCode = "remote-not-pasted";
        internal const string LatestStampNotLoadedReasonCode = "latest-stamp-not-loaded";
        internal const string NoPublishedStampReasonCode = "no-published-stamp";

        // Id cách sửa (6.1) — cùng lý do chép như trên: hằng gốc nằm trong lớp luật internal.
        internal const string NormalizeRepairId = "normalize-utc";
        internal const string KeepStartSetDurationRepairId = "keep-start-set-duration-24h";
        internal const string SwapStartEndRepairId = "swap-start-end";
        internal const string RenameRepairId = "rename-with-suggested-id";
        internal const string ShiftStartKeepEndRepairId = "shift-start-keep-end";
        internal const string ShiftWholeKeepDurationRepairId = "shift-whole-keep-duration";
        internal const string SetActiveToPeriodRepairId = "set-active-to-period";
        internal const string RevertRepairId = "revert";
        internal const string DeferUntilEndRepairId = "defer-until-end";

        /// <summary>Tên field luật 3 ghi vào <c>ExpectedText</c> khi loại (không phải id) sai quy tắc.</summary>
        private const string EventTypeFieldName = "type";

        private const string NoParseOpen = "<noparse>";
        private const string NoParseClose = "</noparse>";
        private const string NoParseTagName = "noparse";

        // Giá trị thô chứa thẻ noparse sẽ đóng vùng noparse giữa chừng và phần sau bị parse thành thẻ. Bộ parse rich text so tên thẻ
        // KHÔNG phân biệt hoa thường (TMP_Text băm tên qua ToUpperFast) nên "</NoParse>" cũng đóng vùng. Chèn một ký tự không bề rộng ngay
        // sau '<' của mọi "<noparse"/"</noparse" (mọi kiểu hoa thường) làm tên thẻ không còn khớp mà chữ nhìn gần như cũ; PlainText gỡ ra lại.
        private const char ZeroWidthSpace = '\u200B';
        private const string ZeroWidthSpaceText = "\u200B";

        private const string KeySeparator = "|";
        private const string PairFormat = "{0} {1}";

        /// <summary>Thời lượng không phụ thuộc lệch giờ máy — một bộ định dạng lệch 0 đủ cho <see cref="ShortLabel"/> (không nhận format).</summary>
        private static readonly LiveOpsHubFormat DurationFormat = new LiveOpsHubFormat(TimeSpan.Zero);

        private static readonly Dictionary<string, DetailSentences> SentencesByPair = BuildSentences();

        // =============================================================================================================== API

        /// <summary>"lava-quest trống 11 ngày (20/9 → 1/10)" — dòng chính của hàng phát hiện, card Vấn đề, hàng Việc cần làm.</summary>
        public static string Headline(LiveEventCalendarFinding finding, LiveOpsHubFormat format)
        {
            return Headline(finding, format, null);
        }

        /// <summary>
        /// Như trên, kèm dấu đã đăng mới nhất để luật 12 nêu giờ dấu ("Bản remote khác dấu 11/9 16:20: 2 mục"). (V-22 CC-FT-1) BẮT BUỘC khi
        /// nơi gọi có dấu đã đăng (G-OVERVIEW hàng Việc cần làm, G-VALIDATION); <paramref name="latestStamp"/> null = bản không ngữ cảnh.
        /// </summary>
        public static string Headline(LiveEventCalendarFinding finding, LiveOpsHubFormat format, PublishedCalendarStamp latestStamp)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            if (format == null) throw new ArgumentNullException(nameof(format));
            var input = new SentenceInput(format, null, latestStamp);
            DetailSentences sentences = Find(finding);
            return sentences != null ? sentences.Headline(finding, input) : Fallback(finding);
        }

        /// <summary>"không có đợt lava-quest nào trong 20/9 00:00 → 1/10 00:00 UTC" — dòng meta dưới headline.</summary>
        public static string Meta(LiveEventCalendarFinding finding, LiveOpsHubFormat format)
        {
            return MetaCore(finding, format, null);
        }

        /// <summary>
        /// Như trên, biết BÂY GIỜ: câu nêu được giai đoạn đợt ("đợt chưa bắt đầu (14/9 00:00 UTC)" thay cho "đợt bắt đầu 14/9 00:00 UTC").
        /// Tách overload thay vì đổi chữ ký V-8: nơi chưa có đồng hồ (tooltip dựng sớm) vẫn có câu đúng, chỉ bớt một chữ.
        /// (V-22 CC-FT-1) BẮT BUỘC khi nơi gọi có đồng hồ: G-OVERVIEW, G-VALIDATION, G-CALENDAR (card "Vấn đề (n)"), G-CALENDAR-DEPTH.
        /// </summary>
        public static string Meta(LiveEventCalendarFinding finding, LiveOpsHubFormat format, DateTime nowUtc)
        {
            return MetaCore(finding, format, nowUtc);
        }

        /// <summary>
        /// Mảnh ngắn cho tooltip rail/Lịch/Tổng quan: "chồng giờ" · "giờ kết thúc sai định dạng" · "không tự khai configKey" · "trống 11 ngày".
        /// Không có id — nơi ghép tự đặt id ("hunt-0916-bonus (chồng giờ)", "lava-quest trống 11 ngày"), nên không có giá trị thô.
        /// </summary>
        public static string ShortLabel(LiveEventCalendarFinding finding)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            DetailSentences sentences = Find(finding);
            return sentences != null ? sentences.ShortLabel(finding, new SentenceInput(DurationFormat, null, null)) : NoParse(finding.RuleId);
        }

        /// <summary>"ĐIỀU XẢY RA VỚI NGƯỜI CHƠI" của pane Chi tiết — nói hậu quả trước, vì sao sau.</summary>
        public static string ConsequenceSentence(LiveEventCalendarFinding finding, LiveOpsHubFormat format)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            if (format == null) throw new ArgumentNullException(nameof(format));
            DetailSentences sentences = Find(finding);
            return sentences != null ? sentences.Consequence(finding, new SentenceInput(format, null, null)) : Fallback(finding);
        }

        /// <summary>"VÌ SAO" — một câu mỗi luật; "" với id lạ.</summary>
        public static string WhySentence(string ruleId)
        {
            switch (ruleId)
            {
                case LiveEventCalendarRuleIds.UtcTimeFormat: return LiveOpsHubStrings.FindingWhyUtcTimeFormat;
                case LiveEventCalendarRuleIds.EndBeforeStart: return LiveOpsHubStrings.FindingWhyEndBeforeStart;
                case LiveEventCalendarRuleIds.InvalidIdentifier: return LiveOpsHubStrings.FindingWhyInvalidIdentifier;
                case LiveEventCalendarRuleIds.DuplicateEventId: return LiveOpsHubStrings.FindingWhyDuplicateEventId;
                case LiveEventCalendarRuleIds.OverlapSameType: return LiveOpsHubStrings.FindingWhyOverlapSameType;
                case LiveEventCalendarRuleIds.RecurringRuleInvalid: return LiveOpsHubStrings.FindingWhyRecurringRuleInvalid;
                case LiveEventCalendarRuleIds.ShadowedByRecurring: return LiveOpsHubStrings.FindingWhyShadowedByRecurring;
                case LiveEventCalendarRuleIds.UnknownEventType: return LiveOpsHubStrings.FindingWhyUnknownEventType;
                case LiveEventCalendarRuleIds.RunningEventIdChanged: return LiveOpsHubStrings.FindingWhyRunningEventIdChanged;
                case LiveEventCalendarRuleIds.ConfigKeyMissing: return LiveOpsHubStrings.FindingWhyConfigKeyMissing;
                case LiveEventCalendarRuleIds.LongGapBetweenEvents: return LiveOpsHubStrings.FindingWhyLongGapBetweenEvents;
                case LiveEventCalendarRuleIds.RemoteSnapshotDrift: return LiveOpsHubStrings.FindingWhyRemoteSnapshotDrift;
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Nút chính của hàng: "Sửa" · "Đề xuất…" · "Quyết định…" · "Bỏ qua cảnh báo…" theo <c>RepairKind</c>; không có lệnh sửa thì
        /// "Xem diff" (luật 12 lệch), "Khai báo" (luật 8), và (V-21 CC-VALB-3) luật 9 không hoàn về được → "Xem trong lịch"/"Mở luật" — không
        /// bao giờ "Quyết định…" rỗng hay "Xem diff" (của luật 12). "" = hàng không có nút chính, chỉ có <see cref="LinkText"/>.
        /// </summary>
        public static string PrimaryButtonText(LiveEventCalendarFinding finding)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            switch (finding.RepairKind)
            {
                case LiveEventCalendarRepairKind.SafeRepair: return LiveOpsHubStrings.FindingButtonSafeRepair;
                case LiveEventCalendarRepairKind.Proposal: return LiveOpsHubStrings.FindingButtonProposal;
                case LiveEventCalendarRepairKind.Decision: return LiveOpsHubStrings.FindingButtonDecision;
                case LiveEventCalendarRepairKind.Ignorable: return LiveOpsHubStrings.FindingButtonIgnorable;
            }

            switch (finding.RuleId)
            {
                case LiveEventCalendarRuleIds.RemoteSnapshotDrift:
                    return IsDetail(finding, LiveEventCalendarDetailCodes.RemoteDiffers) ? LiveOpsHubStrings.FindingButtonViewDiff : string.Empty;
                case LiveEventCalendarRuleIds.UnknownEventType:
                    return LiveOpsHubStrings.FindingButtonDeclareType;
                case LiveEventCalendarRuleIds.RunningEventIdChanged:
                    return TargetLink(finding);
                default:
                    return string.Empty;
            }
        }

        /// <summary>Link phụ cạnh nút ("Xem trong lịch", "Mở luật"); "" khi nút chính đã là link đó hoặc đích không nằm trên lịch.</summary>
        public static string LinkText(LiveEventCalendarFinding finding)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            if (finding.IsAboutRemoteSnapshot) return string.Empty;
            if (IsRule(finding, LiveEventCalendarRuleIds.RunningEventIdChanged) && finding.RepairKind == LiveEventCalendarRepairKind.None) return string.Empty;
            return TargetLink(finding);
        }

        /// <summary>Tooltip nút chính — "Hai cách đúng: giữ tiền tố weekly-pass-, hoặc để sau khi weekly-pass-35 khép (14/9 00:00 UTC)".</summary>
        public static string PrimaryButtonTooltip(LiveEventCalendarFinding finding, LiveOpsHubFormat format)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            if (format == null) throw new ArgumentNullException(nameof(format));
            switch (finding.RepairKind)
            {
                case LiveEventCalendarRepairKind.SafeRepair: return LiveOpsHubStrings.FindingTooltipSafeRepair;
                case LiveEventCalendarRepairKind.Proposal: return LiveOpsHubStrings.FindingTooltipProposal;
                case LiveEventCalendarRepairKind.Ignorable: return LiveOpsHubStrings.FindingTooltipIgnorable;
                case LiveEventCalendarRepairKind.Decision:
                    if (finding.Repairs.Count < 2) return LiveOpsHubStrings.FindingTooltipProposal;
                    return Format(LiveOpsHubStrings.FindingTooltipDecisionFormat, DecisionPiece(finding, finding.Repairs[0], format),
                        DecisionPiece(finding, finding.Repairs[1], format));
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Như trên, biết tên file asset lịch đang mở: tooltip "Bỏ qua cảnh báo…" nói đúng chữ thiết kế "…lưu trong Main.asset" [SD2 §2.3 hàng 5].
        /// Tách overload vì tên asset tuỳ dự án và lớp câu không tự đọc đường dẫn; <paramref name="calendarAssetName"/> rỗng = câu chung.
        /// (V-22 CC-FT-1) BẮT BUỘC khi nơi gọi biết asset lịch đang mở (G-VALIDATION).
        /// </summary>
        public static string PrimaryButtonTooltip(LiveEventCalendarFinding finding, LiveOpsHubFormat format, string calendarAssetName)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (finding.RepairKind == LiveEventCalendarRepairKind.Ignorable && !string.IsNullOrEmpty(calendarAssetName))
            {
                return Format(LiveOpsHubStrings.FindingTooltipIgnorableInAssetFormat, NoParse(calendarAssetName));
            }
            return PrimaryButtonTooltip(finding, format);
        }

        /// <summary>
        /// Chữ một lựa chọn sửa (popover Đề xuất…, menu Quyết định…, pane Chi tiết "CÁCH SỬA"): "Dời tới 17/9 00:00 (24 giờ)…",
        /// "Giữ 36 giờ: tới 18/9 12:00…", "Giữ tiền tố weekly-pass- (hoàn về)", "Để sau khi weekly-pass-35 khép (14/9 00:00 UTC)…".
        /// </summary>
        public static string RepairOptionText(LiveEventCalendarFinding finding, LiveEventCalendarRepair repair, LiveOpsHubFormat format)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            if (repair == null) throw new ArgumentNullException(nameof(repair));
            if (format == null) throw new ArgumentNullException(nameof(format));

            bool hasWindow = repair.NewStartUtc.HasValue && repair.NewEndUtc.HasValue;
            switch (repair.RepairId)
            {
                case NormalizeRepairId:
                    return Format(LiveOpsHubStrings.FindingRepairNormalizeFormat, QuotedValues(repair.BeforeText), RawValues(repair.AfterText));
                case KeepStartSetDurationRepairId:
                    if (!hasWindow) break;
                    return Format(LiveOpsHubStrings.FindingRepairKeepStartFormat, EventLength(repair.NewEndUtc.Value - repair.NewStartUtc.Value, format),
                        format.ShortDateTime(repair.NewEndUtc.Value));
                case SwapStartEndRepairId:
                    if (!hasWindow) break;
                    return Format(LiveOpsHubStrings.FindingRepairSwapFormat, RangeUtc(format, repair.NewStartUtc.Value, repair.NewEndUtc.Value));
                case RenameRepairId:
                    return Format(LiveOpsHubStrings.FindingRepairRenameFormat, IdText(repair.AfterText));
                case ShiftStartKeepEndRepairId:
                    if (!hasWindow) break;
                    return Format(LiveOpsHubStrings.FindingRepairShiftStartFormat, format.ShortDateTime(repair.NewStartUtc.Value),
                        EventLength(repair.NewEndUtc.Value - repair.NewStartUtc.Value, format));
                case ShiftWholeKeepDurationRepairId:
                    if (!hasWindow) break;
                    return Format(LiveOpsHubStrings.FindingRepairShiftWholeFormat, EventLength(repair.NewEndUtc.Value - repair.NewStartUtc.Value, format),
                        format.ShortDateTime(repair.NewEndUtc.Value));
                case SetActiveToPeriodRepairId:
                    return Format(LiveOpsHubStrings.FindingRepairSetActiveFormat, Hours(repair.AfterText));
                case RevertRepairId:
                {
                    string keptPrefix = RevertedPrefix(finding, repair);
                    return keptPrefix != null
                        ? Format(LiveOpsHubStrings.FindingRepairRevertPrefixFormat, NoParse(keptPrefix))
                        : LiveOpsHubStrings.FindingRepairRevertPublished;
                }
                case DeferUntilEndRepairId:
                    return Format(LiveOpsHubStrings.FindingRepairDeferFormat, IdText(finding.RelatedId), MomentUtc(format, finding.RangeEndUtc));
            }
            // Id cách sửa lạ, hoặc lệnh thiếu khung giờ: vẫn nêu giá trị sau sửa thay vì bỏ trống nút.
            return RawValues(repair.AfterText.Length > 0 ? repair.AfterText : repair.RepairId);
        }

        /// <summary>
        /// (V-21 CC-VALB-3) Câu sửa tay cho phát hiện KHÔNG có lệnh sửa mà người dùng vẫn phải xử lý — hiện là luật 9 khi không lệnh
        /// hoàn về nào giữ được đợt đang chạy. Câu chỉ đúng việc phải làm (khôi phục id + khung đợt đang chạy trong Lịch/luật, gỡ mục chắn).
        /// "" với phát hiện có lệnh sửa hoặc đã có link đủ nghĩa.
        /// </summary>
        public static string ManualFixSentence(LiveEventCalendarFinding finding, LiveOpsHubFormat format)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (!IsRule(finding, LiveEventCalendarRuleIds.RunningEventIdChanged) || finding.RepairKind != LiveEventCalendarRepairKind.None) return string.Empty;

            string runningWindow = OptionalRangeUtc(format, finding.RangeStartUtc, finding.RangeEndUtc);
            return finding.TargetKind == LiveEventCalendarTargetKind.RecurringRule
                ? Format(LiveOpsHubStrings.FindingRunningManualFixRecurringFormat, IdText(finding.RelatedId), NoParse(finding.TargetId), runningWindow)
                : Format(LiveOpsHubStrings.FindingRunningManualFixFixedFormat, IdText(finding.RelatedId), runningWindow);
        }

        /// <summary>"overlap-same-type · hunt-0916-bonus" — dòng id luật 9px mono; phát hiện không đích (bản remote) chỉ có id luật.</summary>
        public static string RuleIdLine(LiveEventCalendarFinding finding)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            return finding.TargetId.Length == 0
                ? NoParse(finding.RuleId)
                : Format(LiveOpsHubStrings.FindingRuleIdLineFormat, NoParse(finding.RuleId), NoParse(finding.TargetId));
        }

        /// <summary>
        /// (V-7) "cũng sai: id chứa '#'" — lỗi cấp mục khác của cùng mục bị bỏ, lấy từ <c>AdditionalItemReasons</c> (không phải phát hiện
        /// riêng). "" khi mục không có lỗi phụ.
        /// </summary>
        public static string AlsoWrongText(LiveEventCalendarEntryOutcome outcome)
        {
            if (outcome == null || outcome.AdditionalItemReasons.Count == 0) return string.Empty;
            var parts = new List<string>(outcome.AdditionalItemReasons.Count);
            for (int index = 0; index < outcome.AdditionalItemReasons.Count; index++)
            {
                string phrase = DropReasonPhrase(outcome.AdditionalItemReasons[index], outcome);
                if (phrase.Length > 0 && !parts.Contains(phrase)) parts.Add(phrase);
            }
            return parts.Count == 0 ? string.Empty : LiveOpsHubStrings.FindingAlsoWrongPrefix + string.Join(LiveOpsHubStrings.FindingListJoin, parts);
        }

        /// <summary>Mảnh ngắn cho một lý do bỏ mục (dùng chung cho "cũng sai" và hàng diff Bị bỏ): "giờ kết thúc sai định dạng", "id chứa '#'"…</summary>
        public static string DropReasonPhrase(LiveEventCalendarDropReason reason, LiveEventCalendarEntryOutcome outcome)
        {
            switch (reason)
            {
                case LiveEventCalendarDropReason.UnreadableStartUtc: return LiveOpsHubStrings.FindingUtcStartShortLabel;
                case LiveEventCalendarDropReason.UnreadableEndUtc: return LiveOpsHubStrings.FindingUtcEndShortLabel;
                case LiveEventCalendarDropReason.InvalidIdentifier: return IdentifierDefectPhrase(outcome);
                case LiveEventCalendarDropReason.EndNotAfterStart: return LiveOpsHubStrings.FindingReasonEndNotAfterStart;
                case LiveEventCalendarDropReason.DuplicateEventId: return LiveOpsHubStrings.FindingDuplicateShortLabel;
                case LiveEventCalendarDropReason.OverlapsSameType: return LiveOpsHubStrings.FindingOverlapShortLabel;
                case LiveEventCalendarDropReason.ShadowedByRecurring: return LiveOpsHubStrings.FindingShadowedOverlapShortLabel;
                case LiveEventCalendarDropReason.InvalidRecurringRule: return LiveOpsHubStrings.FindingReasonInvalidRecurringRule;
                case LiveEventCalendarDropReason.DuplicateRecurringType: return LiveOpsHubStrings.FindingRecurringDuplicateTypeReason;
                default: return string.Empty;
            }
        }

        /// <summary>
        /// (V-21 CC-VALB-4) Meta khoảng của một cảnh báo đã bỏ qua trong nhóm "Đã bỏ qua": ghi chú hẹn giờ (<c>defer-until-end</c>, khoảng
        /// <c>[lúc khép, mở)</c>) hiện "hẹn tới 14/9 00:00" — không "14/9 → mọi", vì với người đọc đó là một hạn chứ không phải khoảng bị ẩn.
        /// </summary>
        public static string IgnoredWarningRangeText(IgnoredCalendarWarning warning, LiveOpsHubFormat format)
        {
            if (warning == null) throw new ArgumentNullException(nameof(warning));
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (warning.IsReminder) return Format(LiveOpsHubStrings.FindingReminderUntilFormat, MomentText(format, warning.ExpiresUtcText));
            if (warning.RangeStartUtcText.Length > 0 && warning.RangeEndUtcText.Length > 0)
            {
                return Format(LiveOpsHubStrings.FindingRangeFormat, MomentText(format, warning.RangeStartUtcText), MomentText(format, warning.RangeEndUtcText));
            }
            if (warning.RangeStartUtcText.Length > 0) return Format(LiveOpsHubStrings.FindingIgnoredFromFormat, MomentText(format, warning.RangeStartUtcText));
            return LiveOpsHubStrings.FindingIgnoredEveryRange;
        }

        /// <summary>Tag trên phát hiện có <c>DueReminder</c> (ghi chú hẹn giờ đã tới hạn).</summary>
        public static string DueReminderTag => LiveOpsHubStrings.FindingDueReminderTag;

        /// <summary>true khi cặp (luật, biến thể) có đủ câu — test duyệt mọi cặp của <c>LiveEventCalendarDetailCodes</c>.</summary>
        public static bool HasSentence(string ruleId, string detailCode)
        {
            return ruleId != null && detailCode != null && SentencesByPair.ContainsKey(ruleId + KeySeparator + detailCode);
        }

        // ----- Hàng luật không ra phát hiện (Chưa kiểm / Không áp dụng / luật ném) -----

        /// <summary>true khi hàng luật NotMeasured/NotApplicable/Failed có câu riêng (không phải câu chung "Luật x chưa kiểm được").</summary>
        public static bool HasRuleResultSentence(string ruleId, LiveEventCalendarRuleOutcome outcome, string reasonCode)
        {
            if (outcome == LiveEventCalendarRuleOutcome.Failed) return true;
            if (outcome == LiveEventCalendarRuleOutcome.NotMeasured)
            {
                return string.Equals(ruleId, LiveEventCalendarRuleIds.RemoteSnapshotDrift, StringComparison.Ordinal) &&
                    (string.Equals(reasonCode, RemoteNotPastedReasonCode, StringComparison.Ordinal) ||
                     string.Equals(reasonCode, LatestStampNotLoadedReasonCode, StringComparison.Ordinal));
            }
            if (outcome == LiveEventCalendarRuleOutcome.NotApplicable)
            {
                return string.Equals(ruleId, LiveEventCalendarRuleIds.RunningEventIdChanged, StringComparison.Ordinal) &&
                    string.Equals(reasonCode, NoPublishedStampReasonCode, StringComparison.Ordinal);
            }
            return false;
        }

        /// <summary>"So với bản đang chạy trên remote config" · "Luật long-gap-between-events không chạy được: NullReferenceException".</summary>
        public static string RuleResultHeadline(LiveEventCalendarRuleResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            switch (result.Outcome)
            {
                case LiveEventCalendarRuleOutcome.Failed:
                    return Format(LiveOpsHubStrings.FindingRuleFailedHeadlineFormat, NoParse(result.RuleId), NoParse(result.ExceptionTypeName));
                case LiveEventCalendarRuleOutcome.NotMeasured:
                    return HasRuleResultSentence(result.RuleId, result.Outcome, result.ReasonCode)
                        ? LiveOpsHubStrings.FindingRemoteNotMeasuredHeadline
                        : Format(LiveOpsHubStrings.FindingRuleNotMeasuredHeadlineFormat, NoParse(result.RuleId));
                case LiveEventCalendarRuleOutcome.NotApplicable:
                    return HasRuleResultSentence(result.RuleId, result.Outcome, result.ReasonCode)
                        ? LiveOpsHubStrings.FindingRunningNotApplicableHeadline
                        : Format(LiveOpsHubStrings.FindingRuleNotApplicableHeadlineFormat, NoParse(result.RuleId));
                default:
                    return NoParse(result.RuleId);
            }
        }

        /// <summary>
        /// Meta hàng luật: "Chưa dán JSON đang chạy · luật này không tính là đã qua" · (V-21 CC-VALB-2) "Chưa so được với dấu đã đăng mới nhất
        /// 11/9 16:20 — bản so đang chọn là dấu khác." (<paramref name="latestStamp"/> null thì bỏ giờ).
        /// </summary>
        public static string RuleResultMeta(LiveEventCalendarRuleResult result, LiveOpsHubFormat format, PublishedCalendarStamp latestStamp)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (format == null) throw new ArgumentNullException(nameof(format));
            switch (result.Outcome)
            {
                case LiveEventCalendarRuleOutcome.Failed:
                    return Format(LiveOpsHubStrings.FindingRuleFailedMetaFormat, NoParse(result.ExceptionMessage));
                case LiveEventCalendarRuleOutcome.NotMeasured:
                    if (string.Equals(result.ReasonCode, RemoteNotPastedReasonCode, StringComparison.Ordinal)) return LiveOpsHubStrings.FindingRemoteNotPastedMeta;
                    if (string.Equals(result.ReasonCode, LatestStampNotLoadedReasonCode, StringComparison.Ordinal))
                    {
                        return latestStamp != null && latestStamp.TryGetPublishedUtc(out DateTime publishedUtc)
                            ? Format(LiveOpsHubStrings.FindingLatestStampNotLoadedMetaFormat, format.ShortDateTime(publishedUtc))
                            : LiveOpsHubStrings.FindingLatestStampNotLoadedMeta;
                    }
                    return Format(LiveOpsHubStrings.FindingRuleNotMeasuredMetaFormat, NoParse(result.ReasonCode));
                case LiveEventCalendarRuleOutcome.NotApplicable:
                    return string.Equals(result.ReasonCode, NoPublishedStampReasonCode, StringComparison.Ordinal)
                        ? LiveOpsHubStrings.FindingNoPublishedStampMeta
                        : Format(LiveOpsHubStrings.FindingRuleNotApplicableMetaFormat, NoParse(result.ReasonCode));
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// (PD-9) Nhãn của hàng luật Không áp dụng trong card "Đã qua": "không áp dụng: chưa có dấu đã đăng" cho luật 9 khi chưa có dấu; mã lạ
        /// "không áp dụng: lý do …"; "" với kết quả không phải Không áp dụng. Màn không tự viết nhãn này (V-8).
        /// </summary>
        public static string RuleResultNotApplicableLabel(LiveEventCalendarRuleResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.Outcome != LiveEventCalendarRuleOutcome.NotApplicable) return string.Empty;
            return HasRuleResultSentence(result.RuleId, result.Outcome, result.ReasonCode)
                ? LiveOpsHubStrings.FindingRunningNotApplicableLabel
                : Format(LiveOpsHubStrings.FindingRuleNotApplicableLabelFormat, NoParse(result.ReasonCode));
        }

        /// <summary>Nút của hàng luật: "Dán JSON đang chạy…" khi chưa dán, "Copy lỗi" khi luật ném; "" còn lại.</summary>
        public static string RuleResultButtonText(LiveEventCalendarRuleResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.Outcome == LiveEventCalendarRuleOutcome.Failed) return LiveOpsHubStrings.FindingButtonCopyError;
            if (result.Outcome == LiveEventCalendarRuleOutcome.NotMeasured && string.Equals(result.ReasonCode, RemoteNotPastedReasonCode, StringComparison.Ordinal))
            {
                return LiveOpsHubStrings.FindingButtonPasteRunningJson;
            }
            return string.Empty;
        }

        // ----- Rich text -----

        /// <summary>Bọc giá trị thô vào <c>&lt;noparse&gt;</c> (rỗng → rỗng). Xuống dòng hiện thành "\n" để một id hỏng không làm vỡ hàng 22px.</summary>
        public static string NoParse(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return string.Empty;
            string escaped = EscapeNoParseTags(rawValue)
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
            return NoParseOpen + escaped + NoParseClose;
        }

        /// <summary>Gỡ vỏ noparse cho nơi hiện chữ không rich text (tooltip, toast, clipboard "Copy mô tả lỗi").</summary>
        public static string PlainText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return UnescapeNoParseTags(text
                .Replace(NoParseOpen, string.Empty)
                .Replace(NoParseClose, string.Empty));
        }

        private static string EscapeNoParseTags(string rawValue)
        {
            if (rawValue.IndexOf('<') < 0) return rawValue;
            var builder = new StringBuilder(rawValue.Length + 8);
            for (int index = 0; index < rawValue.Length; index++)
            {
                builder.Append(rawValue[index]);
                if (rawValue[index] == '<' && IsNoParseTagNameAt(rawValue, index + 1)) builder.Append(ZeroWidthSpace);
            }
            return builder.ToString();
        }

        private static string UnescapeNoParseTags(string text)
        {
            if (text.IndexOf(ZeroWidthSpaceText, StringComparison.Ordinal) < 0) return text;
            var builder = new StringBuilder(text.Length);
            for (int index = 0; index < text.Length; index++)
            {
                bool isEscapeMark = text[index] == ZeroWidthSpace && index > 0 && text[index - 1] == '<' && IsNoParseTagNameAt(text, index + 1);
                if (!isEscapeMark) builder.Append(text[index]);
            }
            return builder.ToString();
        }

        /// <summary>true khi tại <paramref name="start"/> là tên thẻ noparse (có hoặc không '/' đứng trước), không phân biệt hoa thường.</summary>
        private static bool IsNoParseTagNameAt(string text, int start)
        {
            if (start < text.Length && text[start] == '/') start++;
            return start + NoParseTagName.Length <= text.Length &&
                   string.Compare(text, start, NoParseTagName, 0, NoParseTagName.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        // =============================================================================================================== bảng câu

        private delegate string FindingSentence(LiveEventCalendarFinding finding, SentenceInput input);

        /// <summary>Bộ câu của một cặp (RuleId, DetailCode). Một cặp thiếu bộ câu = <see cref="HasSentence"/> false.</summary>
        private sealed class DetailSentences
        {
            public DetailSentences(FindingSentence headline, FindingSentence meta, FindingSentence shortLabel, FindingSentence consequence)
            {
                Headline = headline;
                Meta = meta;
                ShortLabel = shortLabel;
                Consequence = consequence;
            }

            public FindingSentence Headline { get; }
            public FindingSentence Meta { get; }
            public FindingSentence ShortLabel { get; }
            public FindingSentence Consequence { get; }
        }

        private sealed class SentenceInput
        {
            public SentenceInput(LiveOpsHubFormat format, DateTime? nowUtc, PublishedCalendarStamp latestStamp)
            {
                Format = format;
                NowUtc = nowUtc;
                LatestStamp = latestStamp;
            }

            public LiveOpsHubFormat Format { get; }
            public DateTime? NowUtc { get; }
            public PublishedCalendarStamp LatestStamp { get; }
        }

        private static Dictionary<string, DetailSentences> BuildSentences()
        {
            var table = new Dictionary<string, DetailSentences>(StringComparer.Ordinal);

            // 1 utc-time-format
            var utcTime = new DetailSentences(UtcTimeHeadline, UtcTimeMeta, UtcTimeShortLabel, DroppedEntryConsequence);
            Register(table, LiveEventCalendarRuleIds.UtcTimeFormat, LiveEventCalendarDetailCodes.StartUnreadable, utcTime);
            Register(table, LiveEventCalendarRuleIds.UtcTimeFormat, LiveEventCalendarDetailCodes.EndUnreadable, utcTime);
            Register(table, LiveEventCalendarRuleIds.UtcTimeFormat, LiveEventCalendarDetailCodes.AnchorUnreadable, utcTime);

            // 2 end-before-start
            var endBeforeStart = new DetailSentences(EndBeforeStartHeadline, EndBeforeStartMeta, EndBeforeStartShortLabel, DroppedEntryConsequence);
            Register(table, LiveEventCalendarRuleIds.EndBeforeStart, LiveEventCalendarDetailCodes.EndBeforeStart, endBeforeStart);
            Register(table, LiveEventCalendarRuleIds.EndBeforeStart, LiveEventCalendarDetailCodes.EndEqualsStart, endBeforeStart);

            // 3 invalid-identifier
            var identifier = new DetailSentences(IdentifierHeadline, IdentifierMeta, IdentifierShortLabel, DroppedEntryConsequence);
            Register(table, LiveEventCalendarRuleIds.InvalidIdentifier, LiveEventCalendarDetailCodes.EmptyIdentifier, identifier);
            Register(table, LiveEventCalendarRuleIds.InvalidIdentifier, LiveEventCalendarDetailCodes.HashInIdentifier, identifier);
            Register(table, LiveEventCalendarRuleIds.InvalidIdentifier, LiveEventCalendarDetailCodes.NewlineInIdentifier, identifier);

            // 4 duplicate-event-id
            Register(table, LiveEventCalendarRuleIds.DuplicateEventId, LiveEventCalendarDetailCodes.DuplicateFixedId,
                new DetailSentences(DuplicateHeadline, DuplicateMeta, (finding, input) => LiveOpsHubStrings.FindingDuplicateShortLabel, DuplicateConsequence));

            // 5 overlap-same-type
            Register(table, LiveEventCalendarRuleIds.OverlapSameType, LiveEventCalendarDetailCodes.OverlapKeptEarlier,
                new DetailSentences(OverlapHeadline, OverlapMeta, (finding, input) => LiveOpsHubStrings.FindingOverlapShortLabel, OverlapConsequence));

            // 6 recurring-rule-invalid
            var recurring = new DetailSentences(RecurringHeadline, RecurringMeta, RecurringReason, RecurringConsequence);
            foreach (string detailCode in new[]
                     {
                         LiveEventCalendarDetailCodes.AnchorUnreadable, LiveEventCalendarDetailCodes.PeriodNotPositive, LiveEventCalendarDetailCodes.ActiveNotPositive,
                         LiveEventCalendarDetailCodes.ActiveLongerThanPeriod, LiveEventCalendarDetailCodes.PrefixInvalid, LiveEventCalendarDetailCodes.TypeInvalid,
                         LiveEventCalendarDetailCodes.DuplicateRuleForType,
                     })
            {
                Register(table, LiveEventCalendarRuleIds.RecurringRuleInvalid, detailCode, recurring);
            }

            // 7 shadowed-by-recurring
            var shadowed = new DetailSentences(ShadowedHeadline, ShadowedMeta, ShadowedShortLabel, ShadowedConsequence);
            Register(table, LiveEventCalendarRuleIds.ShadowedByRecurring, LiveEventCalendarDetailCodes.ShadowedOverlap, shadowed);
            Register(table, LiveEventCalendarRuleIds.ShadowedByRecurring, LiveEventCalendarDetailCodes.ShadowedIdCollision, shadowed);

            // 8 unknown-event-type
            var unknownType = new DetailSentences(UnknownTypeHeadline, UnknownTypeMeta, UnknownTypeShortLabel, UnknownTypeConsequence);
            Register(table, LiveEventCalendarRuleIds.UnknownEventType, LiveEventCalendarDetailCodes.UnknownTypeInDraft, unknownType);
            Register(table, LiveEventCalendarRuleIds.UnknownEventType, LiveEventCalendarDetailCodes.UnknownTypeInRemote, unknownType);

            // 9 running-event-id-changed
            var running = new DetailSentences(RunningHeadline, RunningMeta, RunningShortLabel, RunningConsequence);
            foreach (string detailCode in new[]
                     {
                         LiveEventCalendarDetailCodes.RunningIdChanged, LiveEventCalendarDetailCodes.RunningRemoved, LiveEventCalendarDetailCodes.RunningRetyped,
                         LiveEventCalendarDetailCodes.RunningMovedOutOfWindow, LiveEventCalendarDetailCodes.RunningEndsNow,
                     })
            {
                Register(table, LiveEventCalendarRuleIds.RunningEventIdChanged, detailCode, running);
            }

            // 10 config-key-missing
            var configKey = new DetailSentences(ConfigKeyHeadline, ConfigKeyMeta, ConfigKeyShortLabel, ConfigKeyConsequence);
            Register(table, LiveEventCalendarRuleIds.ConfigKeyMissing, LiveEventCalendarDetailCodes.ConfigKeyInheritedDiffers, configKey);
            Register(table, LiveEventCalendarRuleIds.ConfigKeyMissing, LiveEventCalendarDetailCodes.ConfigKeyInheritedNew, configKey);
            Register(table, LiveEventCalendarRuleIds.ConfigKeyMissing, LiveEventCalendarDetailCodes.ConfigKeyEmpty, configKey);

            // 11 long-gap-between-events
            Register(table, LiveEventCalendarRuleIds.LongGapBetweenEvents, LiveEventCalendarDetailCodes.LongGap,
                new DetailSentences(LongGapHeadline, LongGapMeta, LongGapShortLabel, LongGapConsequence));

            // 12 remote-snapshot-drift
            var remote = new DetailSentences(RemoteHeadline, RemoteMeta, RemoteShortLabel, RemoteConsequence);
            Register(table, LiveEventCalendarRuleIds.RemoteSnapshotDrift, LiveEventCalendarDetailCodes.RemoteDiffers, remote);
            Register(table, LiveEventCalendarRuleIds.RemoteSnapshotDrift, LiveEventCalendarDetailCodes.RemoteWithoutStamp, remote);

            return table;
        }

        private static void Register(Dictionary<string, DetailSentences> table, string ruleId, string detailCode, DetailSentences sentences)
        {
            table.Add(ruleId + KeySeparator + detailCode, sentences);
        }

        private static DetailSentences Find(LiveEventCalendarFinding finding)
        {
            return SentencesByPair.TryGetValue(finding.RuleId + KeySeparator + finding.DetailCode, out DetailSentences sentences) ? sentences : null;
        }

        /// <summary>Cặp chưa có câu (mã mới ở core): vẫn hiện được id luật + đích để không có hàng trống; test chặn trước khi phát hành.</summary>
        private static string Fallback(LiveEventCalendarFinding finding)
        {
            return RuleIdLine(finding);
        }

        private static string MetaCore(LiveEventCalendarFinding finding, LiveOpsHubFormat format, DateTime? nowUtc)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            if (format == null) throw new ArgumentNullException(nameof(format));
            DetailSentences sentences = Find(finding);
            if (sentences == null) return string.Empty;

            string meta = sentences.Meta(finding, new SentenceInput(format, nowUtc, null));
            // (V-17) Phát hiện về JSON đang chạy của một luật không tự nói điều đó (luật 8/12 tự nói) vẫn phải nêu là không về nháp.
            bool describesRemoteItself = IsRule(finding, LiveEventCalendarRuleIds.UnknownEventType) || IsRule(finding, LiveEventCalendarRuleIds.RemoteSnapshotDrift);
            if (finding.IsAboutRemoteSnapshot && !describesRemoteItself) meta = JoinParts(meta, LiveOpsHubStrings.FindingRemoteSubjectMeta);
            return meta;
        }

        // =============================================================================================================== luật 1–3

        private static string UtcTimeHeadline(LiveEventCalendarFinding finding, SentenceInput input)
        {
            string target = IdText(finding.TargetId);
            if (IsDetail(finding, LiveEventCalendarDetailCodes.AnchorUnreadable)) return Format(LiveOpsHubStrings.FindingUtcAnchorHeadlineFormat, target);
            if (IsDetail(finding, LiveEventCalendarDetailCodes.EndUnreadable)) return Format(LiveOpsHubStrings.FindingUtcEndHeadlineFormat, target);
            // Mã "start" cũng dùng khi CẢ HAI giờ hỏng (FoundText mang hai giá trị) — luật chỉ sinh một phát hiện mỗi đợt.
            return SplitValues(finding.FoundText).Length > 1
                ? Format(LiveOpsHubStrings.FindingUtcBothHeadlineFormat, target)
                : Format(LiveOpsHubStrings.FindingUtcStartHeadlineFormat, target);
        }

        private static string UtcTimeShortLabel(LiveEventCalendarFinding finding, SentenceInput input)
        {
            if (IsDetail(finding, LiveEventCalendarDetailCodes.AnchorUnreadable)) return LiveOpsHubStrings.FindingAnchorShortLabel;
            if (IsDetail(finding, LiveEventCalendarDetailCodes.EndUnreadable)) return LiveOpsHubStrings.FindingUtcEndShortLabel;
            return SplitValues(finding.FoundText).Length > 1 ? LiveOpsHubStrings.FindingUtcBothShortLabel : LiveOpsHubStrings.FindingUtcStartShortLabel;
        }

        /// <summary>"tìm thấy "2026-10-3" · cần 2026-10-03T00:00:00Z" [SD2 §2.3 hàng 1] — giá trị hỏng trong ngoặc kép, giá trị cần thì không.</summary>
        private static string UtcTimeMeta(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return FoundExpected(finding.FoundText, finding.ExpectedText);
        }

        private static string DroppedEntryConsequence(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return Format(LiveOpsHubStrings.FindingDroppedEntryConsequenceFormat, IdText(finding.TargetId));
        }

        private static string EndBeforeStartHeadline(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return Format(IsDetail(finding, LiveEventCalendarDetailCodes.EndEqualsStart)
                ? LiveOpsHubStrings.FindingEndEqualsStartHeadlineFormat
                : LiveOpsHubStrings.FindingEndBeforeStartHeadlineFormat, IdText(finding.TargetId));
        }

        private static string EndBeforeStartShortLabel(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return IsDetail(finding, LiveEventCalendarDetailCodes.EndEqualsStart)
                ? LiveOpsHubStrings.FindingEndEqualsStartShortLabel
                : LiveOpsHubStrings.FindingEndBeforeStartShortLabel;
        }

        /// <summary>"tìm thấy 16/9 12:00 → 15/9 00:00 UTC · cần kết thúc sau 16/9 12:00" (khuôn hàng 1, giờ đọc được nên định dạng).</summary>
        private static string EndBeforeStartMeta(LiveEventCalendarFinding finding, SentenceInput input)
        {
            if (!finding.RangeStartUtc.HasValue || !finding.RangeEndUtc.HasValue) return FoundExpected(finding.FoundText, string.Empty);
            return JoinParts(
                Format(LiveOpsHubStrings.FindingFoundFormat, RangeUtc(input.Format, finding.RangeStartUtc.Value, finding.RangeEndUtc.Value)),
                Format(LiveOpsHubStrings.FindingEndAfterStartExpectedFormat, input.Format.ShortDateTime(finding.RangeStartUtc.Value)));
        }

        private static string IdentifierHeadline(LiveEventCalendarFinding finding, SentenceInput input)
        {
            string field = IdentifierField(finding);
            if (IsDetail(finding, LiveEventCalendarDetailCodes.HashInIdentifier))
            {
                return Format(LiveOpsHubStrings.FindingIdentifierHashHeadlineFormat, IdText(finding.TargetId), field);
            }
            if (IsDetail(finding, LiveEventCalendarDetailCodes.NewlineInIdentifier))
            {
                return Format(LiveOpsHubStrings.FindingIdentifierNewlineHeadlineFormat, IdText(finding.TargetId), field);
            }
            // Id rỗng thì không có gì để gọi tên đợt — câu riêng thay vì một cặp ngoặc kép rỗng đứng đầu câu.
            if (finding.TargetId.Length == 0) return LiveOpsHubStrings.FindingIdentifierEmptyWithoutIdHeadline;
            return Format(LiveOpsHubStrings.FindingIdentifierEmptyHeadlineFormat, IdText(finding.TargetId), field);
        }

        private static string IdentifierShortLabel(LiveEventCalendarFinding finding, SentenceInput input)
        {
            string field = IdentifierField(finding);
            if (IsDetail(finding, LiveEventCalendarDetailCodes.HashInIdentifier)) return Format(LiveOpsHubStrings.FindingIdentifierHashShortLabelFormat, field);
            if (IsDetail(finding, LiveEventCalendarDetailCodes.NewlineInIdentifier)) return Format(LiveOpsHubStrings.FindingIdentifierNewlineShortLabelFormat, field);
            return Format(LiveOpsHubStrings.FindingIdentifierEmptyShortLabelFormat, field);
        }

        private static string IdentifierMeta(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return JoinParts(OptionalRangeUtc(input.Format, finding.RangeStartUtc, finding.RangeEndUtc), LiveOpsHubStrings.FindingIdentifierMeta);
        }

        private static string IdentifierField(LiveEventCalendarFinding finding)
        {
            return string.Equals(finding.ExpectedText, EventTypeFieldName, StringComparison.Ordinal)
                ? LiveOpsHubStrings.FindingIdentifierFieldType
                : LiveOpsHubStrings.FindingIdentifierFieldId;
        }

        private static string IdentifierDefectPhrase(LiveEventCalendarEntryOutcome outcome)
        {
            if (outcome == null) return LiveOpsHubStrings.FindingIdentifierMeta;
            // Cùng thứ tự kiểm với luật 3 (id trước loại) để "cũng sai" nói đúng ô sai.
            string defect = DescribeIdentifierDefect(outcome.EventId);
            string field = LiveOpsHubStrings.FindingIdentifierFieldId;
            if (defect == null)
            {
                defect = DescribeIdentifierDefect(outcome.EventType);
                field = LiveOpsHubStrings.FindingIdentifierFieldType;
            }
            if (defect == null) return LiveOpsHubStrings.FindingIdentifierMeta;
            return Format(defect, field);
        }

        /// <summary>Khuôn mảnh ngắn cho một định danh sai; null khi hợp lệ.</summary>
        private static string DescribeIdentifierDefect(string value)
        {
            if (string.IsNullOrEmpty(value)) return LiveOpsHubStrings.FindingIdentifierEmptyShortLabelFormat;
            if (value.IndexOf(LiveEventInstance.ReservedSeparator.ToString(), StringComparison.Ordinal) >= 0) return LiveOpsHubStrings.FindingIdentifierHashShortLabelFormat;
            if (value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0) return LiveOpsHubStrings.FindingIdentifierNewlineShortLabelFormat;
            return null;
        }

        // =============================================================================================================== luật 4–5

        private static string DuplicateHeadline(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return Format(LiveOpsHubStrings.FindingDuplicateHeadlineFormat, IdText(finding.TargetId));
        }

        private static string DuplicateMeta(LiveEventCalendarFinding finding, SentenceInput input)
        {
            string suggestion = finding.ExpectedText.Length > 0
                ? Format(LiveOpsHubStrings.FindingDuplicateSuggestionFormat, NoParse(finding.ExpectedText))
                : string.Empty;
            return JoinParts(OptionalRangeUtc(input.Format, finding.RangeStartUtc, finding.RangeEndUtc), suggestion);
        }

        private static string DuplicateConsequence(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return Format(LiveOpsHubStrings.FindingDuplicateConsequenceFormat, IdText(finding.RelatedId.Length > 0 ? finding.RelatedId : finding.TargetId));
        }

        /// <summary>"hunt-0916-bonus chồng 12 giờ với hunt-0914" [SD2 §2.3 hàng 2] — thời lượng là khoảng chồng, không phải độ dài đợt.</summary>
        private static string OverlapHeadline(LiveEventCalendarFinding finding, SentenceInput input)
        {
            if (!finding.RangeStartUtc.HasValue || !finding.RangeEndUtc.HasValue)
            {
                return Format(LiveOpsHubStrings.FindingOverlapWithoutRangeHeadlineFormat, IdText(finding.TargetId), IdText(finding.RelatedId));
            }
            return Format(LiveOpsHubStrings.FindingOverlapHeadlineFormat, IdText(finding.TargetId),
                input.Format.Duration(finding.RangeEndUtc.Value - finding.RangeStartUtc.Value, false), IdText(finding.RelatedId));
        }

        /// <summary>"16/9 12:00 → 17/9 00:00 UTC · game giữ đợt bắt đầu sớm hơn".</summary>
        private static string OverlapMeta(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return JoinParts(OptionalRangeUtc(input.Format, finding.RangeStartUtc, finding.RangeEndUtc), LiveOpsHubStrings.FindingOverlapKeepsEarlier);
        }

        /// <summary>"Không thấy đợt hunt-0916-bonus (16/9 12:00); hunt-0914 chạy bình thường." — theo câu pane Chi tiết [SD2 §2.3 overlap].</summary>
        private static string OverlapConsequence(LiveEventCalendarFinding finding, SentenceInput input)
        {
            if (!finding.AnchorUtc.HasValue)
            {
                return Format(LiveOpsHubStrings.FindingOverlapWithoutAnchorConsequenceFormat, IdText(finding.TargetId), IdText(finding.RelatedId));
            }
            return Format(LiveOpsHubStrings.FindingOverlapConsequenceFormat, IdText(finding.TargetId), input.Format.ShortDateTime(finding.AnchorUtc.Value),
                IdText(finding.RelatedId));
        }

        // =============================================================================================================== luật 6–8

        private static string RecurringHeadline(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return Format(LiveOpsHubStrings.FindingRecurringHeadlineFormat, IdText(finding.TargetId), RecurringReason(finding, input));
        }

        private static string RecurringReason(LiveEventCalendarFinding finding, SentenceInput input)
        {
            switch (finding.DetailCode)
            {
                case LiveEventCalendarDetailCodes.AnchorUnreadable: return LiveOpsHubStrings.FindingAnchorShortLabel;
                case LiveEventCalendarDetailCodes.PeriodNotPositive: return LiveOpsHubStrings.FindingRecurringPeriodReason;
                case LiveEventCalendarDetailCodes.ActiveNotPositive: return LiveOpsHubStrings.FindingRecurringActiveReason;
                case LiveEventCalendarDetailCodes.ActiveLongerThanPeriod: return LiveOpsHubStrings.FindingRecurringActiveLongerReason;
                case LiveEventCalendarDetailCodes.PrefixInvalid: return LiveOpsHubStrings.FindingRecurringPrefixReason;
                case LiveEventCalendarDetailCodes.TypeInvalid: return LiveOpsHubStrings.FindingRecurringTypeReason;
                default: return LiveOpsHubStrings.FindingRecurringDuplicateTypeReason;
            }
        }

        private static string RecurringMeta(LiveEventCalendarFinding finding, SentenceInput input)
        {
            switch (finding.DetailCode)
            {
                case LiveEventCalendarDetailCodes.AnchorUnreadable:
                    return FoundExpected(finding.FoundText, finding.ExpectedText);
                case LiveEventCalendarDetailCodes.PeriodNotPositive:
                case LiveEventCalendarDetailCodes.ActiveNotPositive:
                    return Format(LiveOpsHubStrings.FindingRecurringHoursMetaFormat, Hours(finding.FoundText));
                case LiveEventCalendarDetailCodes.ActiveLongerThanPeriod:
                    return Format(LiveOpsHubStrings.FindingRecurringActiveLongerMetaFormat, Hours(finding.FoundText), Hours(finding.ExpectedText));
                case LiveEventCalendarDetailCodes.PrefixInvalid:
                case LiveEventCalendarDetailCodes.TypeInvalid:
                    return Format(LiveOpsHubStrings.FindingRecurringIdentifierMetaFormat, Quoted(finding.FoundText));
                default:
                    return LiveOpsHubStrings.FindingRecurringDuplicateTypeMeta;
            }
        }

        private static string RecurringConsequence(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return Format(LiveOpsHubStrings.FindingRecurringConsequenceFormat, IdText(finding.TargetId));
        }

        private static string ShadowedHeadline(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return Format(IsDetail(finding, LiveEventCalendarDetailCodes.ShadowedIdCollision)
                ? LiveOpsHubStrings.FindingShadowedIdHeadlineFormat
                : LiveOpsHubStrings.FindingShadowedOverlapHeadlineFormat, IdText(finding.TargetId), IdText(finding.RelatedId));
        }

        private static string ShadowedShortLabel(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return IsDetail(finding, LiveEventCalendarDetailCodes.ShadowedIdCollision)
                ? LiveOpsHubStrings.FindingShadowedIdShortLabel
                : LiveOpsHubStrings.FindingShadowedOverlapShortLabel;
        }

        private static string ShadowedMeta(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return JoinParts(OptionalRangeUtc(input.Format, finding.RangeStartUtc, finding.RangeEndUtc),
                IsDetail(finding, LiveEventCalendarDetailCodes.ShadowedIdCollision)
                    ? LiveOpsHubStrings.FindingShadowedIdMetaDetail
                    : LiveOpsHubStrings.FindingShadowedOverlapMetaDetail);
        }

        private static string ShadowedConsequence(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return Format(LiveOpsHubStrings.FindingShadowedConsequenceFormat, IdText(finding.TargetId), IdText(finding.RelatedId));
        }

        /// <summary>(V-17) Nguồn là cờ <c>IsAboutRemoteSnapshot</c> hoặc mã <c>remote</c> — một trong hai là đủ để câu nói về JSON đang chạy.</summary>
        private static bool IsAboutRemoteType(LiveEventCalendarFinding finding)
        {
            return finding.IsAboutRemoteSnapshot || IsDetail(finding, LiveEventCalendarDetailCodes.UnknownTypeInRemote);
        }

        private static string UnknownTypeHeadline(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return Format(IsAboutRemoteType(finding)
                ? LiveOpsHubStrings.FindingUnknownTypeRemoteHeadlineFormat
                : LiveOpsHubStrings.FindingUnknownTypeDraftHeadlineFormat, NoParse(finding.FoundText), IdText(finding.TargetId));
        }

        private static string UnknownTypeShortLabel(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return IsAboutRemoteType(finding) ? LiveOpsHubStrings.FindingUnknownTypeRemoteShortLabel : LiveOpsHubStrings.FindingUnknownTypeDraftShortLabel;
        }

        private static string UnknownTypeMeta(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return IsAboutRemoteType(finding) ? LiveOpsHubStrings.FindingRemoteSubjectMeta : LiveOpsHubStrings.FindingUnknownTypeDraftMeta;
        }

        /// <summary>Bản remote: đúng câu dưới bảng Loại event Hình 10b [SD1 §2.2] "lucky-spin: có 2 đợt trong JSON đã dán nhưng chưa có loại — game sẽ bỏ".</summary>
        private static string UnknownTypeConsequence(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return IsAboutRemoteType(finding)
                ? Format(LiveOpsHubStrings.FindingUnknownTypeRemoteConsequenceFormat, IdText(finding.TargetId), NoParse(finding.FoundText))
                : Format(LiveOpsHubStrings.FindingUnknownTypeDraftConsequenceFormat, NoParse(finding.FoundText), IdText(finding.TargetId));
        }

        // =============================================================================================================== luật 9

        private static string RunningHeadline(LiveEventCalendarFinding finding, SentenceInput input)
        {
            string running = IdText(finding.RelatedId);
            string target = IdText(finding.TargetId);
            bool isRecurring = finding.TargetKind == LiveEventCalendarTargetKind.RecurringRule;
            switch (finding.DetailCode)
            {
                case LiveEventCalendarDetailCodes.RunningIdChanged:
                    return Format(LiveOpsHubStrings.FindingRunningPrefixChangedHeadlineFormat, target, running);
                case LiveEventCalendarDetailCodes.RunningRemoved:
                    return isRecurring
                        ? Format(LiveOpsHubStrings.FindingRunningRemovedRecurringHeadlineFormat, target, running)
                        : Format(LiveOpsHubStrings.FindingRunningRemovedFixedHeadlineFormat, running);
                case LiveEventCalendarDetailCodes.RunningRetyped:
                    return Format(LiveOpsHubStrings.FindingRunningRetypedHeadlineFormat, running);
                case LiveEventCalendarDetailCodes.RunningMovedOutOfWindow:
                {
                    DateTime? newStartUtc = ParseValue(finding.FoundText, 0);
                    if (!newStartUtc.HasValue)
                    {
                        return isRecurring
                            ? Format(LiveOpsHubStrings.FindingRunningMovedOutWithoutTimeHeadlineFormat, target, running)
                            : Format(PairFormat, running, LiveOpsHubStrings.FindingRunningMovedOutShortLabel);
                    }
                    string newStart = input.Format.ShortDateTime(newStartUtc.Value);
                    // Luật ra moved-out cho mọi khung mới không giao khung đang chạy — gồm cả dời về TRƯỚC; "dời ra sau 1/9" cho đợt đang chạy
                    // từ 12/9 là câu sai nghĩa, nên so với lúc bắt đầu đang chạy (RangeStartUtc).
                    bool movedEarlier = finding.RangeStartUtc.HasValue && newStartUtc.Value < finding.RangeStartUtc.Value;
                    if (movedEarlier)
                    {
                        return isRecurring
                            ? Format(LiveOpsHubStrings.FindingRunningMovedEarlierRecurringHeadlineFormat, target, running, newStart)
                            : Format(LiveOpsHubStrings.FindingRunningMovedEarlierFixedHeadlineFormat, running, newStart);
                    }
                    return isRecurring
                        ? Format(LiveOpsHubStrings.FindingRunningMovedOutRecurringHeadlineFormat, target, running, newStart)
                        : Format(LiveOpsHubStrings.FindingRunningMovedOutFixedHeadlineFormat, running, newStart);
                }
                default:
                    return isRecurring
                        ? Format(LiveOpsHubStrings.FindingRunningEndsNowRecurringHeadlineFormat, target, running)
                        : Format(LiveOpsHubStrings.FindingRunningEndsNowFixedHeadlineFormat, running);
            }
        }

        private static string RunningShortLabel(LiveEventCalendarFinding finding, SentenceInput input)
        {
            switch (finding.DetailCode)
            {
                case LiveEventCalendarDetailCodes.RunningIdChanged: return LiveOpsHubStrings.FindingRunningPrefixChangedShortLabel;
                case LiveEventCalendarDetailCodes.RunningRemoved: return LiveOpsHubStrings.FindingRunningRemovedShortLabel;
                case LiveEventCalendarDetailCodes.RunningRetyped: return LiveOpsHubStrings.FindingRunningRetypedShortLabel;
                case LiveEventCalendarDetailCodes.RunningMovedOutOfWindow: return LiveOpsHubStrings.FindingRunningMovedOutShortLabel;
                default: return LiveOpsHubStrings.FindingRunningEndsNowShortLabel;
            }
        }

        /// <summary>"weekly-pass-35 → pass-35 · đang chạy tới 14/9 00:00 UTC" [SD2 §2.3 hàng 3]; không lệnh sửa thì thêm "sửa tay trong Lịch".</summary>
        private static string RunningMeta(LiveEventCalendarFinding finding, SentenceInput input)
        {
            string until = finding.RangeEndUtc.HasValue
                ? Format(LiveOpsHubStrings.FindingRunningUntilFormat, input.Format.ShortDateTimeUtc(finding.RangeEndUtc.Value))
                : string.Empty;
            string running = IdText(finding.RelatedId);
            string meta;
            switch (finding.DetailCode)
            {
                case LiveEventCalendarDetailCodes.RunningIdChanged:
                    meta = JoinParts(Format(LiveOpsHubStrings.FindingRangeFormat, running,
                        finding.FoundText.Length > 0 ? NoParse(finding.FoundText) : LiveOpsHubStrings.FindingRunningNoOccurrenceNow), until);
                    break;
                case LiveEventCalendarDetailCodes.RunningRemoved:
                    meta = JoinParts(until, LiveOpsHubStrings.FindingRunningNotFollowed);
                    break;
                case LiveEventCalendarDetailCodes.RunningRetyped:
                    meta = JoinParts(Format(LiveOpsHubStrings.FindingRunningRetypedDetailFormat, IdText(finding.ExpectedText), IdText(finding.FoundText)), until);
                    break;
                case LiveEventCalendarDetailCodes.RunningMovedOutOfWindow:
                    meta = JoinParts(Format(LiveOpsHubStrings.FindingRunningNewWindowFormat, TimeValuesRange(input.Format, finding.FoundText)), until);
                    break;
                default:
                {
                    DateTime? newEndUtc = ParseValue(finding.FoundText, 1);
                    string newEnd = newEndUtc.HasValue ? input.Format.ShortDateTimeUtc(newEndUtc.Value) : QuotedValues(finding.FoundText);
                    meta = JoinParts(Format(LiveOpsHubStrings.FindingRunningNewEndFormat, newEnd), until);
                    break;
                }
            }
            if (finding.RepairKind != LiveEventCalendarRepairKind.None) return meta;
            // Chỉ đúng màn sửa như nút chính và ManualFixSentence: đợt cố định → Lịch, lần lặp → Luật lặp ("Mở luật").
            return JoinParts(meta, finding.TargetKind == LiveEventCalendarTargetKind.RecurringRule
                ? LiveOpsHubStrings.FindingRunningManualFixRecurringMeta
                : LiveOpsHubStrings.FindingRunningManualFixMeta);
        }

        private static string RunningConsequence(LiveEventCalendarFinding finding, SentenceInput input)
        {
            string running = IdText(finding.RelatedId);
            string until = MomentUtc(input.Format, finding.RangeEndUtc);
            if (IsDetail(finding, LiveEventCalendarDetailCodes.RunningEndsNow))
            {
                return Format(LiveOpsHubStrings.FindingRunningEndsNowConsequenceFormat, running, until);
            }
            // Id mới thay vào trên cùng loại = bản ghi mới từ 0; không có đợt thay vào = bản ghi cũ dừng cộng điểm và khép theo giờ đã lưu.
            bool restarts = IsDetail(finding, LiveEventCalendarDetailCodes.RunningIdChanged) && finding.FoundText.Length > 0;
            return Format(restarts ? LiveOpsHubStrings.FindingRunningRestartConsequenceFormat : LiveOpsHubStrings.FindingRunningStopConsequenceFormat,
                running, until);
        }

        private static string DecisionPiece(LiveEventCalendarFinding finding, LiveEventCalendarRepair repair, LiveOpsHubFormat format)
        {
            if (string.Equals(repair.RepairId, RevertRepairId, StringComparison.Ordinal))
            {
                string keptPrefix = RevertedPrefix(finding, repair);
                return keptPrefix != null
                    ? Format(LiveOpsHubStrings.FindingDecisionKeepPrefixFormat, NoParse(keptPrefix))
                    : LiveOpsHubStrings.FindingDecisionRevertPublished;
            }
            if (string.Equals(repair.RepairId, DeferUntilEndRepairId, StringComparison.Ordinal))
            {
                return Format(LiveOpsHubStrings.FindingDecisionDeferFormat, IdText(finding.RelatedId), MomentUtc(format, finding.RangeEndUtc));
            }
            return RepairOptionText(finding, repair, format);
        }

        /// <summary>Tiền tố mà lệnh hoàn về giữ lại, khi phát hiện là đổi tiền tố của luật lặp; null khi lệnh không phải đặt lại luật.</summary>
        private static string RevertedPrefix(LiveEventCalendarFinding finding, LiveEventCalendarRepair repair)
        {
            if (!IsDetail(finding, LiveEventCalendarDetailCodes.RunningIdChanged)) return null;
            SetRecurringRuleEdit ruleEdit = repair.Edit as SetRecurringRuleEdit;
            return ruleEdit != null && ruleEdit.Rule != null ? ruleEdit.Rule.EffectiveIdPrefix : null;
        }

        // =============================================================================================================== luật 10–12

        private static bool IsEmptyConfigKey(LiveEventCalendarFinding finding)
        {
            return IsDetail(finding, LiveEventCalendarDetailCodes.ConfigKeyEmpty);
        }

        /// <summary>"hunt-0914 không tự khai configKey — xuất sẽ ghi hunt_default" [SD2 §2.3 hàng 4].</summary>
        private static string ConfigKeyHeadline(LiveEventCalendarFinding finding, SentenceInput input)
        {
            bool isRecurring = finding.TargetKind == LiveEventCalendarTargetKind.RecurringRule;
            if (IsEmptyConfigKey(finding))
            {
                return isRecurring
                    ? Format(LiveOpsHubStrings.FindingConfigKeyEmptyRecurringHeadlineFormat, IdText(finding.TargetId))
                    : Format(LiveOpsHubStrings.FindingConfigKeyEmptyFixedHeadlineFormat, IdText(finding.TargetId), IdText(finding.RelatedId));
            }
            return Format(isRecurring ? LiveOpsHubStrings.FindingConfigKeyRecurringHeadlineFormat : LiveOpsHubStrings.FindingConfigKeyFixedHeadlineFormat,
                IdText(finding.TargetId), NoParse(finding.FoundText));
        }

        private static string ConfigKeyShortLabel(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return IsEmptyConfigKey(finding) ? LiveOpsHubStrings.FindingConfigKeyEmptyShortLabel : LiveOpsHubStrings.FindingConfigKeyShortLabel;
        }

        /// <summary>"đợt chưa bắt đầu (14/9 00:00 UTC) · bản đã đăng dùng hunt_v1" — giai đoạn chỉ nêu được khi biết BÂY GIỜ.</summary>
        private static string ConfigKeyMeta(LiveEventCalendarFinding finding, SentenceInput input)
        {
            string subject = string.Empty;
            if (finding.TargetKind == LiveEventCalendarTargetKind.RecurringRule)
            {
                subject = LiveOpsHubStrings.FindingConfigKeyRecurringSubject;
            }
            else if (finding.RangeStartUtc.HasValue)
            {
                DateTime startUtc = finding.RangeStartUtc.Value;
                if (input.NowUtc.HasValue && input.NowUtc.Value < startUtc)
                {
                    subject = Format(LiveOpsHubStrings.FindingConfigKeyUpcomingFormat, input.Format.ShortDateTimeUtc(startUtc));
                }
                else if (input.NowUtc.HasValue && finding.RangeEndUtc.HasValue)
                {
                    subject = Format(LiveOpsHubStrings.FindingConfigKeyRunningFormat, input.Format.ShortDateTimeUtc(finding.RangeEndUtc.Value));
                }
                else
                {
                    subject = Format(LiveOpsHubStrings.FindingConfigKeyStartsFormat, input.Format.ShortDateTimeUtc(startUtc));
                }
            }

            string comparison;
            if (IsEmptyConfigKey(finding)) comparison = LiveOpsHubStrings.FindingConfigKeyEmptyMeta;
            else if (IsDetail(finding, LiveEventCalendarDetailCodes.ConfigKeyInheritedDiffers)) comparison = Format(LiveOpsHubStrings.FindingConfigKeyPublishedUsesFormat, NoParse(finding.ExpectedText));
            else comparison = LiveOpsHubStrings.FindingConfigKeyNotInPublished;
            return JoinParts(subject, comparison);
        }

        private static string ConfigKeyConsequence(LiveEventCalendarFinding finding, SentenceInput input)
        {
            if (IsEmptyConfigKey(finding)) return Format(LiveOpsHubStrings.FindingConfigKeyEmptyConsequenceFormat, IdText(finding.TargetId));
            if (IsDetail(finding, LiveEventCalendarDetailCodes.ConfigKeyInheritedDiffers))
            {
                return Format(LiveOpsHubStrings.FindingConfigKeyDiffersConsequenceFormat, NoParse(finding.FoundText), NoParse(finding.ExpectedText));
            }
            return Format(LiveOpsHubStrings.FindingConfigKeyNewConsequenceFormat, NoParse(finding.FoundText), IdText(finding.RelatedId));
        }

        /// <summary>"lava-quest trống 11 ngày (20/9 → 1/10)" [SD2 §2.3 hàng 5].</summary>
        private static string LongGapHeadline(LiveEventCalendarFinding finding, SentenceInput input)
        {
            if (!finding.RangeStartUtc.HasValue || !finding.RangeEndUtc.HasValue)
            {
                return Format(LiveOpsHubStrings.FindingLongGapWithoutRangeHeadlineFormat, IdText(finding.TargetId));
            }
            return Format(LiveOpsHubStrings.FindingLongGapHeadlineFormat, IdText(finding.TargetId),
                input.Format.Duration(finding.RangeEndUtc.Value - finding.RangeStartUtc.Value, false),
                DayMonth(finding.RangeStartUtc.Value), DayMonth(finding.RangeEndUtc.Value));
        }

        private static string LongGapShortLabel(LiveEventCalendarFinding finding, SentenceInput input)
        {
            if (!finding.RangeStartUtc.HasValue || !finding.RangeEndUtc.HasValue) return LiveOpsHubStrings.FindingLongGapShortLabelWithoutRange;
            return Format(LiveOpsHubStrings.FindingLongGapShortLabelFormat, input.Format.Duration(finding.RangeEndUtc.Value - finding.RangeStartUtc.Value, false));
        }

        private static string LongGapMeta(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return Format(LiveOpsHubStrings.FindingLongGapMetaFormat, IdText(finding.TargetId), OptionalRangeUtc(input.Format, finding.RangeStartUtc, finding.RangeEndUtc));
        }

        private static string LongGapConsequence(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return Format(LiveOpsHubStrings.FindingLongGapConsequenceFormat, IdText(finding.TargetId), OptionalRangeUtc(input.Format, finding.RangeStartUtc, finding.RangeEndUtc));
        }

        private static string RemoteHeadline(LiveEventCalendarFinding finding, SentenceInput input)
        {
            if (IsDetail(finding, LiveEventCalendarDetailCodes.RemoteWithoutStamp))
            {
                return Format(LiveOpsHubStrings.FindingRemoteNoStampHeadlineFormat, NoParse(finding.FoundText));
            }
            PublishedCalendarStamp stamp = input.LatestStamp;
            return stamp != null && stamp.TryGetPublishedUtc(out DateTime publishedUtc)
                ? Format(LiveOpsHubStrings.FindingRemoteDiffersHeadlineFormat, input.Format.ShortDateTime(publishedUtc), NoParse(finding.ExpectedText))
                : Format(LiveOpsHubStrings.FindingRemoteDiffersWithoutStampHeadlineFormat, NoParse(finding.ExpectedText));
        }

        private static string RemoteShortLabel(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return IsDetail(finding, LiveEventCalendarDetailCodes.RemoteWithoutStamp)
                ? LiveOpsHubStrings.FindingRemoteNoStampShortLabel
                : LiveOpsHubStrings.FindingRemoteDiffersShortLabel;
        }

        private static string RemoteMeta(LiveEventCalendarFinding finding, SentenceInput input)
        {
            string meta = IsDetail(finding, LiveEventCalendarDetailCodes.RemoteWithoutStamp)
                ? LiveOpsHubStrings.FindingRemoteNoStampMeta
                : Format(LiveOpsHubStrings.FindingRemoteDiffersMetaFormat, RawList(finding.FoundText));
            return finding.IsAboutRemoteSnapshot ? JoinParts(meta, LiveOpsHubStrings.FindingRemoteNotBlockingCopy) : meta;
        }

        private static string RemoteConsequence(LiveEventCalendarFinding finding, SentenceInput input)
        {
            return IsDetail(finding, LiveEventCalendarDetailCodes.RemoteWithoutStamp)
                ? LiveOpsHubStrings.FindingRemoteNoStampConsequence
                : Format(LiveOpsHubStrings.FindingRemoteDiffersConsequenceFormat, NoParse(finding.ExpectedText));
        }

        // =============================================================================================================== mảnh dùng chung

        private static bool IsRule(LiveEventCalendarFinding finding, string ruleId)
        {
            return string.Equals(finding.RuleId, ruleId, StringComparison.Ordinal);
        }

        private static bool IsDetail(LiveEventCalendarFinding finding, string detailCode)
        {
            return string.Equals(finding.DetailCode, detailCode, StringComparison.Ordinal);
        }

        private static string TargetLink(LiveEventCalendarFinding finding)
        {
            switch (finding.TargetKind)
            {
                case LiveEventCalendarTargetKind.FixedEvent: return LiveOpsHubStrings.FindingLinkViewInCalendar;
                case LiveEventCalendarTargetKind.RecurringRule: return LiveOpsHubStrings.FindingLinkOpenRule;
                default: return string.Empty;
            }
        }

        internal static string Format(string format, params object[] values)
        {
            return string.Format(CultureInfo.InvariantCulture, format, values);
        }

        /// <summary>Id/loại để gọi tên trong câu; rỗng thì in cặp ngoặc kép rỗng — câu "đợt  bị bỏ" với hai dấu cách là lỗi đọc được ngay.</summary>
        internal static string IdText(string rawValue)
        {
            return string.IsNullOrEmpty(rawValue) ? Format(LiveOpsHubStrings.FindingQuotedFormat, string.Empty) : NoParse(rawValue);
        }

        internal static string Quoted(string rawValue)
        {
            return Format(LiveOpsHubStrings.FindingQuotedFormat, NoParse(rawValue));
        }

        /// <summary>Tách giá trị core đã nối bằng <see cref="LiveEventCalendarFindingBuilder.ValueSeparator"/> (V-19 CC-VALA-2).</summary>
        internal static string[] SplitValues(string joinedValues)
        {
            if (string.IsNullOrEmpty(joinedValues)) return Array.Empty<string>();
            return joinedValues.Split(new[] { LiveEventCalendarFindingBuilder.ValueSeparator }, StringSplitOptions.None);
        }

        private static string QuotedValues(string joinedValues)
        {
            string[] values = SplitValues(joinedValues);
            var quoted = new string[values.Length];
            for (int index = 0; index < values.Length; index++) quoted[index] = Quoted(values[index]);
            return string.Join(LiveOpsHubStrings.FindingValueJoin, quoted);
        }

        private static string RawValues(string joinedValues)
        {
            string[] values = SplitValues(joinedValues);
            var raw = new string[values.Length];
            for (int index = 0; index < values.Length; index++) raw[index] = NoParse(values[index]);
            return string.Join(LiveOpsHubStrings.FindingValueJoin, raw);
        }

        private static string RawList(string joinedValues)
        {
            string[] values = SplitValues(joinedValues);
            var raw = new string[values.Length];
            for (int index = 0; index < values.Length; index++) raw[index] = NoParse(values[index]);
            return string.Join(LiveOpsHubStrings.FindingListJoin, raw);
        }

        private static string FoundExpected(string foundText, string expectedText)
        {
            string found = foundText.Length > 0 ? Format(LiveOpsHubStrings.FindingFoundFormat, QuotedValues(foundText)) : string.Empty;
            string expected = expectedText.Length > 0 ? Format(LiveOpsHubStrings.FindingExpectedFormat, RawValues(expectedText)) : string.Empty;
            return JoinParts(found, expected);
        }

        /// <summary>
        /// Độ dài một đợt trong lựa chọn sửa: "24 giờ", "36 giờ" như popover thiết kế ("Dời tới 17/9 00:00 (24 giờ)…") — người dùng so độ dài
        /// đợt theo giờ; <see cref="LiveOpsHubFormat.Duration"/> gộp thành "1 ngày" làm mất phép so "36 → 24 giờ". Lẻ phút thì về dạng đủ.
        /// </summary>
        private static string EventLength(TimeSpan length, LiveOpsHubFormat format)
        {
            if (length > TimeSpan.Zero && length.Ticks % TimeSpan.TicksPerHour == 0)
            {
                return Format(LiveOpsHubStrings.FindingHoursFormat, ((long)length.TotalHours).ToString(CultureInfo.InvariantCulture));
            }
            return format.Duration(length, false);
        }

        private static string Hours(string rawHours)
        {
            return Format(LiveOpsHubStrings.FindingHoursFormat, NoParse(rawHours));
        }

        /// <summary>Nối hai mảnh bằng " · ", bỏ mảnh rỗng để không ra " ·  · ".</summary>
        internal static string JoinParts(string first, string second)
        {
            if (string.IsNullOrEmpty(first)) return second ?? string.Empty;
            if (string.IsNullOrEmpty(second)) return first;
            return first + LiveOpsHubStrings.FindingPartSeparator + second;
        }

        private static string RangeUtc(LiveOpsHubFormat format, DateTime startUtc, DateTime endUtc)
        {
            return Format(LiveOpsHubStrings.FindingRangeUtcFormat, format.ShortDateTime(startUtc), format.ShortDateTime(endUtc));
        }

        private static string OptionalRangeUtc(LiveOpsHubFormat format, DateTime? startUtc, DateTime? endUtc)
        {
            if (startUtc.HasValue && endUtc.HasValue) return RangeUtc(format, startUtc.Value, endUtc.Value);
            if (startUtc.HasValue) return format.ShortDateTimeUtc(startUtc.Value);
            return endUtc.HasValue ? format.ShortDateTimeUtc(endUtc.Value) : string.Empty;
        }

        private static string MomentUtc(LiveOpsHubFormat format, DateTime? utc)
        {
            return utc.HasValue ? format.ShortDateTimeUtc(utc.Value) : string.Empty;
        }

        /// <summary>Giờ đã lưu dạng chữ: đọc được thì định dạng "14/9 00:00", không thì in nguyên văn trong ngoặc kép.</summary>
        private static string MomentText(LiveOpsHubFormat format, string utcText)
        {
            return LiveEventUtcText.TryParse(utcText, out DateTime utc) ? format.ShortDateTime(utc) : Quoted(utcText);
        }

        private static DateTime? ParseValue(string joinedValues, int index)
        {
            string[] values = SplitValues(joinedValues);
            if (index >= values.Length) return null;
            return LiveEventUtcText.TryParse(values[index], out DateTime utc) ? utc : (DateTime?)null;
        }

        /// <summary>"16/9 00:00 → 18/9 00:00 UTC" từ giá trị thô "start · end"; phía không đọc được giữ nguyên văn trong ngoặc kép.</summary>
        private static string TimeValuesRange(LiveOpsHubFormat format, string joinedValues)
        {
            DateTime? startUtc = ParseValue(joinedValues, 0);
            DateTime? endUtc = ParseValue(joinedValues, 1);
            if (startUtc.HasValue && endUtc.HasValue) return RangeUtc(format, startUtc.Value, endUtc.Value);
            return QuotedValues(joinedValues);
        }

        /// <summary>
        /// "20/9" — ngày/tháng không giờ cho headline khoảng trống. <see cref="LiveOpsHubFormat"/> (gói G-HUBBASE) chỉ có dạng kèm giờ hoặc
        /// kèm năm; viết lại đúng quy tắc [FD §6.1] (không số 0 đầu) ở đây thay vì sửa file của gói khác.
        /// </summary>
        internal static string DayMonth(DateTime utc)
        {
            return utc.Day.ToString(CultureInfo.InvariantCulture) + "/" + utc.Month.ToString(CultureInfo.InvariantCulture);
        }
    }
}
