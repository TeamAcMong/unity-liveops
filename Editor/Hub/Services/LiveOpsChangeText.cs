using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// (V-8) Nguồn câu DUY NHẤT cho một hàng diff (<see cref="LiveEventCalendarChange"/>): dòng tiêu đề ("weekly-pass: tiền tố weekly-pass- →
    /// pass-"), câu hậu quả ("weekly-pass-35 đang chạy tới 14/9 00:00 UTC; người chơi có điểm bắt đầu lại từ 0.") và tooltip thanh
    /// <c>--changed</c> ("Khác bản đã đăng: kết thúc 19/9 → 20/9") cho mọi hàng bảng 6.3 [SD2 §3.8], [SD1 §3.4].
    /// <para>Hai tầng: chữ ký V-8 (chỉ hàng diff) luôn ra câu đúng cho mọi hàng nhưng không biết giai đoạn đợt hay lý do bỏ — hàng diff
    /// không mang giờ của mục khi giờ không đổi. Overload nhận <see cref="LiveOpsChangeTextContext"/> (bản so + nháp + BÂY GIỜ) nói đủ chữ
    /// thiết kế: "đợt chưa bắt đầu, không ai mất gì", "chồng 12 giờ với hunt-0914", và nửa đổi id đợt đã khép (CC-DIFF-3) nêu cả id cũ lẫn
    /// id mới. Presenter có phiên (G-EXPORT, G-CALENDAR) dùng overload có ngữ cảnh.</para>
    /// </summary>
    internal static class LiveOpsChangeText
    {
        private const string FieldId = "id";
        private const string FieldType = "type";
        private const string FieldStartUtc = "startUtc";
        private const string FieldEndUtc = "endUtc";
        private const string FieldConfigKey = "configKey";
        private const string FieldAnchorUtc = "anchorUtc";
        private const string FieldIdPrefix = "idPrefix";
        private const string FieldPeriodHours = "periodHours";
        private const string FieldActiveHours = "activeHours";
        private const string FieldDisplayName = "displayName";
        private const string FieldColorSlot = "colorSlot";
        private const string FieldRequiresJoin = "requiresJoin";
        private const string FieldDefaultConfigKey = "defaultConfigKey";
        private const string FieldOrder = "order";
        private const string TrueText = "true";
        private const string SubjectAndFieldsFormat = "{0} {1}";

        /// <summary>Dòng 1 của hàng diff (không gồm ký hiệu <c>+ ~ !</c> — view vẽ riêng cột mono).</summary>
        public static string RowText(LiveEventCalendarChange change, LiveOpsHubFormat format)
        {
            return RowCore(change, null, format);
        }

        /// <summary>Như trên; biết ngữ cảnh thì nửa đổi id đợt đã khép (CC-DIFF-3) hiện "quest-0901 → quest-0901-renamed (đổi id đợt đã khép)".</summary>
        public static string RowText(LiveEventCalendarChange change, LiveOpsChangeTextContext context, LiveOpsHubFormat format)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return RowCore(change, context, format);
        }

        /// <summary>Dòng 2 của hàng diff — điều xảy ra với người chơi.</summary>
        public static string ConsequenceSentence(LiveEventCalendarChange change, LiveOpsHubFormat format)
        {
            return ConsequenceCore(change, null, format);
        }

        /// <summary>Như trên, đủ chữ thiết kế nhờ biết giai đoạn đợt, lý do bị bỏ và nửa còn lại của một lần đổi id đợt đã khép.</summary>
        public static string ConsequenceSentence(LiveEventCalendarChange change, LiveOpsChangeTextContext context, LiveOpsHubFormat format)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return ConsequenceCore(change, context, format);
        }

        /// <summary>"Khác bản đã đăng: kết thúc 19/9 → 20/9" — tooltip vuông 5×5 của thanh <c>--changed</c> (presenter Lịch điền, D-5).</summary>
        public static string ChangedTooltip(LiveEventCalendarChange change, LiveOpsHubFormat format)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            if (format == null) throw new ArgumentNullException(nameof(format));

            string body;
            if (change.Kind == LiveEventCalendarChangeKind.Added) body = LiveOpsHubStrings.ChangeTooltipAdded;
            else if (change.Kind == LiveEventCalendarChangeKind.Removed) body = LiveOpsHubStrings.ChangeTooltipRemoved;
            else if (change.Fields.Count == 0) body = change.WillBeDropped ? LiveOpsHubStrings.ChangeTooltipDroppedByOther : LiveOpsHubStrings.ChangeTooltipReturnedByOther;
            else body = FieldsText(change, format);
            return LiveOpsFindingText.Format(LiveOpsHubStrings.ChangedTooltipFormat, body);
        }

        /// <summary>
        /// Như trên; biết ngữ cảnh thì hai nửa của một lần đổi id đợt đã khép (V-20 CC-DIFF-3) cùng nói "Khác bản đã đăng: đổi id đợt đã khép
        /// quest-0901 → quest-0901-renamed" — không phải "thêm mới" ở nửa này và "đã xoá" ở nửa kia, trái với hàng diff của cùng mục.
        /// </summary>
        public static string ChangedTooltip(LiveEventCalendarChange change, LiveOpsChangeTextContext context, LiveOpsHubFormat format)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (format == null) throw new ArgumentNullException(nameof(format));

            LiveEventCalendarChange partner = context.FindEndedRenamePartner(change);
            if (partner == null) return ChangedTooltip(change, format);
            LiveEventCalendarChange removedHalf = change.Kind == LiveEventCalendarChangeKind.Removed ? change : partner;
            LiveEventCalendarChange addedHalf = change.Kind == LiveEventCalendarChangeKind.Removed ? partner : change;
            return LiveOpsFindingText.Format(LiveOpsHubStrings.ChangedTooltipFormat, LiveOpsFindingText.Format(LiveOpsHubStrings.ChangeTooltipEndedRenameFormat,
                LiveOpsFindingText.IdText(removedHalf.ItemId), LiveOpsFindingText.IdText(addedHalf.ItemId)));
        }

        // =============================================================================================================== dòng 1

        private static string RowCore(LiveEventCalendarChange change, LiveOpsChangeTextContext context, LiveOpsHubFormat format)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            if (format == null) throw new ArgumentNullException(nameof(format));

            LiveEventCalendarChange partner = context != null ? context.FindEndedRenamePartner(change) : null;
            if (partner != null)
            {
                LiveEventCalendarChange removedHalf = change.Kind == LiveEventCalendarChangeKind.Removed ? change : partner;
                LiveEventCalendarChange addedHalf = change.Kind == LiveEventCalendarChangeKind.Removed ? partner : change;
                return LiveOpsFindingText.Format(LiveOpsHubStrings.ChangeEndedRenameRowFormat,
                    LiveOpsFindingText.IdText(removedHalf.ItemId), LiveOpsFindingText.IdText(addedHalf.ItemId));
            }

            string itemId = LiveOpsFindingText.IdText(change.ItemId);
            switch (change.Kind)
            {
                case LiveEventCalendarChangeKind.Added:
                    return LiveOpsFindingText.Format(LiveOpsHubStrings.ChangeAddedRowFormat, itemId);
                case LiveEventCalendarChangeKind.Removed:
                    return LiveOpsFindingText.Format(LiveOpsHubStrings.ChangeRemovedRowFormat, itemId);
            }

            // (V-20 CC-DIFF-2) Không field nào đổi mà trạng thái giữ/bỏ đổi: nói lý do, không in danh sách field rỗng.
            if (change.Fields.Count == 0)
            {
                return LiveOpsFindingText.Format(change.WillBeDropped
                    ? LiveOpsHubStrings.ChangeDroppedByOtherRowFormat
                    : LiveOpsHubStrings.ChangeReturnedByOtherRowFormat, itemId);
            }

            string subject = change.ItemKind == LiveEventCalendarItemKind.RecurringRule
                ? LiveOpsFindingText.Format(LiveOpsHubStrings.ChangeRecurringSubjectFormat, itemId)
                : itemId;
            // Hàng Bị bỏ nêu tên field JSON (mono) — giá trị hỏng đi xuống dòng 2 nguyên văn [SD2 §3.8 "lava-quest-2026-10 endUtc"].
            string fields = change.Consequence == LiveEventCalendarConsequence.Dropped ? DroppedFieldNames(change) : FieldsText(change, format);
            return LiveOpsFindingText.Format(SubjectAndFieldsFormat, subject, fields);
        }

        private static string DroppedFieldNames(LiveEventCalendarChange change)
        {
            var names = new string[change.Fields.Count];
            for (int index = 0; index < names.Length; index++) names[index] = LiveOpsFindingText.NoParse(change.Fields[index].FieldName);
            return string.Join(LiveOpsHubStrings.FindingListJoin, names);
        }

        private static string FieldsText(LiveEventCalendarChange change, LiveOpsHubFormat format)
        {
            var parts = new string[change.Fields.Count];
            for (int index = 0; index < parts.Length; index++)
            {
                LiveEventCalendarFieldChange field = change.Fields[index];
                parts[index] = LiveOpsFindingText.Format(LiveOpsHubStrings.ChangeFieldFormat, FieldLabel(field.FieldName),
                    FieldValue(field.FieldName, field.BeforeText, format), FieldValue(field.FieldName, field.AfterText, format));
            }
            return string.Join(LiveOpsHubStrings.FindingListJoin, parts);
        }

        private static string FieldLabel(string fieldName)
        {
            switch (fieldName)
            {
                case FieldId: return LiveOpsHubStrings.ChangeFieldLabelId;
                case FieldType: return LiveOpsHubStrings.ChangeFieldLabelType;
                case FieldStartUtc: return LiveOpsHubStrings.ChangeFieldLabelStart;
                case FieldEndUtc: return LiveOpsHubStrings.ChangeFieldLabelEnd;
                case FieldAnchorUtc: return LiveOpsHubStrings.ChangeFieldLabelAnchor;
                case FieldIdPrefix: return LiveOpsHubStrings.ChangeFieldLabelIdPrefix;
                case FieldPeriodHours: return LiveOpsHubStrings.ChangeFieldLabelPeriod;
                case FieldActiveHours: return LiveOpsHubStrings.ChangeFieldLabelActive;
                case FieldDisplayName: return LiveOpsHubStrings.ChangeFieldLabelDisplayName;
                case FieldColorSlot: return LiveOpsHubStrings.ChangeFieldLabelColorSlot;
                case FieldRequiresJoin: return LiveOpsHubStrings.ChangeFieldLabelRequiresJoin;
                case FieldDefaultConfigKey: return LiveOpsHubStrings.ChangeFieldLabelDefaultConfigKey;
                case FieldOrder: return LiveOpsHubStrings.ChangeFieldLabelOrder;
                // "configKey" và tên field lạ: in đúng tên JSON — người dùng thấy đúng chữ đó trong JSON viewer.
                default: return LiveOpsFindingText.NoParse(fieldName);
            }
        }

        /// <summary>
        /// Giờ đọc được: "19/9" khi đúng nửa đêm, "16/9 12:00" khi không — pane So với đã đăng và tooltip thiết kế viết "kết thúc 19/9 → 20/9".
        /// Giờ hỏng/giá trị rỗng: nguyên văn trong ngoặc kép, để "2026-10-3" hiện đúng cái người dùng gõ.
        /// </summary>
        private static string FieldValue(string fieldName, string text, LiveOpsHubFormat format)
        {
            if (IsTimeField(fieldName))
            {
                if (!LiveEventUtcText.TryParse(text, out DateTime utc)) return LiveOpsFindingText.Quoted(text);
                return utc.TimeOfDay == TimeSpan.Zero ? LiveOpsFindingText.DayMonth(utc) : format.ShortDateTime(utc);
            }
            if (string.Equals(fieldName, FieldPeriodHours, StringComparison.Ordinal) || string.Equals(fieldName, FieldActiveHours, StringComparison.Ordinal))
            {
                return LiveOpsFindingText.Format(LiveOpsHubStrings.FindingHoursFormat, LiveOpsFindingText.NoParse(text));
            }
            return text.Length == 0 ? LiveOpsFindingText.Quoted(text) : LiveOpsFindingText.NoParse(text);
        }

        private static bool IsTimeField(string fieldName)
        {
            return string.Equals(fieldName, FieldStartUtc, StringComparison.Ordinal) || string.Equals(fieldName, FieldEndUtc, StringComparison.Ordinal) ||
                   string.Equals(fieldName, FieldAnchorUtc, StringComparison.Ordinal);
        }

        // =============================================================================================================== dòng 2

        private static string ConsequenceCore(LiveEventCalendarChange change, LiveOpsChangeTextContext context, LiveOpsHubFormat format)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            if (format == null) throw new ArgumentNullException(nameof(format));

            switch (change.Consequence)
            {
                case LiveEventCalendarConsequence.Dropped: return DroppedConsequence(change, context, format);
                case LiveEventCalendarConsequence.ProgressLost: return ProgressLostConsequence(change, format);
                case LiveEventCalendarConsequence.ShouldReview: return ShouldReviewConsequence(change, context, format);
                default: return SafeConsequence(change, context);
            }
        }

        private static string DroppedConsequence(LiveEventCalendarChange change, LiveOpsChangeTextContext context, LiveOpsHubFormat format)
        {
            // Field đổi làm mục hỏng: dòng 2 là giá trị trước → sau, giá trị hỏng nguyên văn trong ngoặc kép [SD2 §3.8].
            if (change.Kind == LiveEventCalendarChangeKind.Changed && change.Fields.Count > 0)
            {
                var parts = new string[change.Fields.Count];
                for (int index = 0; index < parts.Length; index++)
                {
                    LiveEventCalendarFieldChange field = change.Fields[index];
                    parts[index] = LiveOpsFindingText.Format(LiveOpsHubStrings.FindingRangeFormat, DroppedValue(field.FieldName, field.BeforeText),
                        DroppedValue(field.FieldName, field.AfterText));
                }
                string values = string.Join(LiveOpsHubStrings.ChangeClauseJoin, parts);
                // Giá trị giờ hỏng tự nói vì sao bị bỏ ("2026-10-3"); mục bị bỏ vì chồng giờ/trùng id/bị che sau khi đổi một field thì "trước →
                // sau" không nói gì về lý do — người dùng thấy nhóm BỊ BỎ mà chỉ đọc được "quest_v1 → quest_v2". Nối lý do của bộ biên dịch.
                if (context == null || IsDropReasonShownByChangedValue(change, context.DropReasonOf(change))) return values;
                string droppedReason = context.DropReasonText(change, format);
                return droppedReason.Length == 0 ? values : values + LiveOpsHubStrings.ChangeClauseJoin + droppedReason;
            }

            bool byOtherEntry = change.Kind == LiveEventCalendarChangeKind.Changed;
            string reason = context != null ? context.DropReasonText(change, format) : string.Empty;
            if (reason.Length == 0) return byOtherEntry ? LiveOpsHubStrings.ChangeDroppedByOtherConsequence : LiveOpsHubStrings.ChangeDroppedConsequence;
            return byOtherEntry ? reason + LiveOpsHubStrings.ChangeByOtherSuffix : reason;
        }

        /// <summary>true khi chính giá trị sau đổi là chỗ hỏng mà dòng 2 đã in nguyên văn (giờ/neo không đọc được), hoặc không biết lý do.</summary>
        private static bool IsDropReasonShownByChangedValue(LiveEventCalendarChange change, LiveEventCalendarDropReason reason)
        {
            switch (reason)
            {
                case LiveEventCalendarDropReason.None: return true;
                case LiveEventCalendarDropReason.UnreadableStartUtc: return HasUnreadableTimeAfter(change, FieldStartUtc);
                case LiveEventCalendarDropReason.UnreadableEndUtc: return HasUnreadableTimeAfter(change, FieldEndUtc);
                case LiveEventCalendarDropReason.InvalidRecurringRule: return HasUnreadableTimeAfter(change, FieldAnchorUtc);
                default: return false;
            }
        }

        private static bool HasUnreadableTimeAfter(LiveEventCalendarChange change, string fieldName)
        {
            LiveEventCalendarFieldChange field = FindField(change, fieldName);
            return field != null && !LiveEventUtcText.TryParse(field.AfterText, out _);
        }

        private static LiveEventCalendarFieldChange FindField(LiveEventCalendarChange change, string fieldName)
        {
            for (int index = 0; index < change.Fields.Count; index++)
            {
                if (string.Equals(change.Fields[index].FieldName, fieldName, StringComparison.Ordinal)) return change.Fields[index];
            }
            return null;
        }

        /// <summary>true khi cả hai giá trị đọc được và <paramref name="laterText"/> sau <paramref name="earlierText"/>.</summary>
        private static bool IsLater(string laterText, string earlierText)
        {
            return LiveEventUtcText.TryParse(laterText, out DateTime laterUtc) && LiveEventUtcText.TryParse(earlierText, out DateTime earlierUtc) &&
                   laterUtc > earlierUtc;
        }

        private static string DroppedValue(string fieldName, string text)
        {
            bool unreadableTime = IsTimeField(fieldName) && !LiveEventUtcText.TryParse(text, out _);
            return unreadableTime || text.Length == 0 ? LiveOpsFindingText.Quoted(text) : LiveOpsFindingText.NoParse(text);
        }

        /// <summary>
        /// Id mới thay vào trên cùng loại → bản ghi mới từ 0 ("người chơi có điểm bắt đầu lại từ 0"); cùng id nhưng khép ngay → khép sớm;
        /// không có đợt thay vào (xoá, đổi loại, dời ra ngoài khung) → bản ghi cũ dừng cộng điểm và khép theo giờ đã lưu.
        /// </summary>
        private static string ProgressLostConsequence(LiveEventCalendarChange change, LiveOpsHubFormat format)
        {
            string runningId = change.RunningEventIdBefore.Length > 0 ? change.RunningEventIdBefore : change.ItemId;
            string running = LiveOpsFindingText.IdText(runningId);
            string until = change.RunningEventEndUtc.HasValue ? format.ShortDateTimeUtc(change.RunningEventEndUtc.Value) : string.Empty;

            if (change.RunningEventIdAfter.Length == 0)
            {
                return LiveOpsFindingText.Format(LiveOpsHubStrings.FindingRunningStopConsequenceFormat, running, until);
            }
            if (string.Equals(change.RunningEventIdAfter, change.RunningEventIdBefore, StringComparison.Ordinal))
            {
                return LiveOpsFindingText.Format(LiveOpsHubStrings.FindingRunningEndsNowConsequenceFormat, running, until);
            }
            return LiveOpsFindingText.Format(LiveOpsHubStrings.FindingRunningRestartConsequenceFormat, running, until);
        }

        private static string ShouldReviewConsequence(LiveEventCalendarChange change, LiveOpsChangeTextContext context, LiveOpsHubFormat format)
        {
            LiveEventCalendarChange partner = context != null ? context.FindEndedRenamePartner(change) : null;
            if (partner != null)
            {
                LiveEventCalendarChange removedHalf = change.Kind == LiveEventCalendarChangeKind.Removed ? change : partner;
                LiveEventCalendarChange addedHalf = change.Kind == LiveEventCalendarChangeKind.Removed ? partner : change;
                return LiveOpsFindingText.Format(LiveOpsHubStrings.ChangeEndedRenameConsequenceFormat,
                    LiveOpsFindingText.IdText(removedHalf.ItemId), LiveOpsFindingText.IdText(addedHalf.ItemId));
            }

            if (change.RunningEventIdBefore.Length > 0)
            {
                string until = change.RunningEventEndUtc.HasValue ? format.ShortDateTimeUtc(change.RunningEventEndUtc.Value) : string.Empty;
                return LiveOpsFindingText.Format(LiveOpsHubStrings.ChangeRunningReviewFormat, LiveOpsFindingText.IdText(change.RunningEventIdBefore), until,
                    string.Join(LiveOpsHubStrings.ChangeClauseJoin, RunningClauses(change, context)));
            }

            LiveOpsChangeTextContext.EntryPhase phase = context != null && change.ItemKind == LiveEventCalendarItemKind.FixedEvent
                ? context.PhaseBefore(change)
                : LiveOpsChangeTextContext.EntryPhase.Unknown;

            // Đổi id đợt đã khép ở diff "chưa lưu" (một hàng Changed): cùng câu với hai nửa xoá + thêm của diff bản đã đăng (6.3 hàng 8) —
            // đợt đã khép không ai "thấy" id mới; điều cần xem là bản ghi người đã chơi vẫn mang id cũ. Không nối "mở lại id này": id mới
            // không nằm trong RetiredEventIds, dù khung mới ở tương lai.
            LiveEventCalendarFieldChange idField = change.Kind == LiveEventCalendarChangeKind.Changed ? FindField(change, FieldId) : null;
            if (phase == LiveOpsChangeTextContext.EntryPhase.Ended && idField != null)
            {
                return LiveOpsFindingText.Format(LiveOpsHubStrings.ChangeEndedRenameConsequenceFormat,
                    LiveOpsFindingText.IdText(idField.BeforeText), LiveOpsFindingText.IdText(idField.AfterText));
            }

            List<string> clauses = NotRunningClauses(change, context, phase);
            if (change.ItemKind == LiveEventCalendarItemKind.EventType) return string.Join(LiveOpsHubStrings.ChangeClauseJoin, clauses);

            string prefix = LiveOpsHubStrings.ChangeNotRunningPrefix;
            if (phase == LiveOpsChangeTextContext.EntryPhase.Upcoming) prefix = LiveOpsHubStrings.ChangeUpcomingPrefix;
            else if (phase == LiveOpsChangeTextContext.EntryPhase.Ended) prefix = LiveOpsHubStrings.ChangeEndedPrefix;
            clauses.Insert(0, prefix);
            return string.Join(LiveOpsHubStrings.ChangeClauseJoin, clauses);
        }

        /// <summary>
        /// Mệnh đề cho đợt đang chạy theo CHIỀU thay đổi, không theo tên field: diff xếp Nên xem khi đợt còn theo được mà (a) khép sớm hơn,
        /// (b) mở lại sau BÂY GIỜ, hoặc (c) đổi configKey (<c>ConsequenceOfFollowing</c>). Kéo dài kết thúc hay dời bắt đầu về trước cùng lúc đổi
        /// configKey không được nói "còn ít thời gian hơn"/"tạm dừng cộng điểm" — chỉ nêu điều thật sự làm hàng phải xem.
        /// </summary>
        private static List<string> RunningClauses(LiveEventCalendarChange change, LiveOpsChangeTextContext context)
        {
            var clauses = new List<string>();
            LiveEventCalendarFieldChange endField = FindField(change, FieldEndUtc);
            bool endShortened = endField != null && IsLater(endField.BeforeText, endField.AfterText);
            bool configKeyChanged = FindField(change, FieldConfigKey) != null;
            for (int index = 0; index < change.Fields.Count; index++)
            {
                LiveEventCalendarFieldChange field = change.Fields[index];
                switch (field.FieldName)
                {
                    case FieldEndUtc:
                        if (endShortened) AddOnce(clauses, LiveOpsHubStrings.ChangeClauseShortened);
                        break;
                    case FieldStartUtc:
                        if (StartsAfterNow(field, context, endShortened || configKeyChanged)) AddOnce(clauses, LiveOpsHubStrings.ChangeClauseStartAfterNow);
                        break;
                    case FieldConfigKey:
                        AddOnce(clauses, LiveOpsFindingText.Format(LiveOpsHubStrings.ChangeClauseRunningConfigKeyFormat, ConfigKeyValue(field.AfterText)));
                        break;
                    case FieldActiveHours: AddOnce(clauses, LiveOpsHubStrings.ChangeClauseActiveHours); break;
                    case FieldAnchorUtc:
                    case FieldIdPrefix:
                    case FieldPeriodHours: AddOnce(clauses, LiveOpsHubStrings.ChangeClauseRecurringIdsShift); break;
                }
            }
            if (clauses.Count == 0) clauses.Add(LiveOpsHubStrings.ChangeClauseWindowOrKey);
            return clauses;
        }

        /// <summary>
        /// Bắt đầu mới nằm sau BÂY GIỜ (đợt tạm dừng tới lúc mở lại). Có ngữ cảnh thì so thẳng với đồng hồ. Không có đồng hồ thì chỉ khẳng định khi
        /// bắt đầu dời về sau VÀ không còn nguyên nhân Nên xem nào khác (<paramref name="otherCauseExists"/>) — khi đó đây là nguyên nhân duy nhất
        /// diff có thể dùng; còn lại thì im lặng thay vì đoán.
        /// </summary>
        private static bool StartsAfterNow(LiveEventCalendarFieldChange startField, LiveOpsChangeTextContext context, bool otherCauseExists)
        {
            if (context != null) return LiveEventUtcText.TryParse(startField.AfterText, out DateTime startUtc) && startUtc > context.NowUtc;
            return !otherCauseExists && IsLater(startField.AfterText, startField.BeforeText);
        }

        private static List<string> NotRunningClauses(LiveEventCalendarChange change, LiveOpsChangeTextContext context, LiveOpsChangeTextContext.EntryPhase phase)
        {
            var clauses = new List<string>();
            if (change.Kind == LiveEventCalendarChangeKind.Removed)
            {
                clauses.Add(change.ItemKind == LiveEventCalendarItemKind.RecurringRule
                    ? LiveOpsHubStrings.ChangeClauseRuleRemoved
                    : LiveOpsHubStrings.ChangeClauseRemoved);
                return clauses;
            }
            // Mục mới Nên xem mà không đổi field nào: chỉ có thể là dùng lại id của một đợt đã khép (bảng 6.3 hàng 8).
            if (change.Kind == LiveEventCalendarChangeKind.Added)
            {
                clauses.Add(LiveOpsHubStrings.ChangeClauseReusedEndedId);
                return clauses;
            }

            bool timeChanged = false;
            for (int index = 0; index < change.Fields.Count; index++)
            {
                LiveEventCalendarFieldChange field = change.Fields[index];
                switch (field.FieldName)
                {
                    case FieldId:
                        AddOnce(clauses, LiveOpsFindingText.Format(LiveOpsHubStrings.ChangeClauseNewIdFormat, LiveOpsFindingText.IdText(field.AfterText)));
                        break;
                    case FieldType:
                        AddOnce(clauses, LiveOpsFindingText.Format(LiveOpsHubStrings.ChangeClauseNewTypeFormat, LiveOpsFindingText.IdText(field.AfterText)));
                        break;
                    case FieldConfigKey:
                        AddOnce(clauses, context != null && context.InheritsConfigKey(change)
                            ? LiveOpsHubStrings.ChangeClauseInheritedConfigKey
                            : LiveOpsFindingText.Format(LiveOpsHubStrings.ChangeClauseConfigKeyFormat, ConfigKeyValue(field.AfterText)));
                        break;
                    case FieldStartUtc:
                    case FieldEndUtc:
                        timeChanged = true;
                        break;
                    case FieldAnchorUtc:
                    case FieldIdPrefix:
                    case FieldPeriodHours:
                        AddOnce(clauses, LiveOpsHubStrings.ChangeClauseRecurringIdsShift);
                        break;
                    case FieldRequiresJoin:
                        AddOnce(clauses, string.Equals(field.AfterText, TrueText, StringComparison.Ordinal)
                            ? LiveOpsHubStrings.ChangeClauseRequiresJoinOn
                            : LiveOpsHubStrings.ChangeClauseRequiresJoinOff);
                        break;
                }
            }
            // Đổi giờ chỉ tự làm hàng Nên xem khi mở lại id của một đợt đã khép cho tương lai (6.3 hàng 8); đợt chưa bắt đầu đổi giờ là an toàn, nên
            // khi hàng Nên xem vì configKey/id/loại thì mệnh đề "mở lại id" là sai. Biết giai đoạn thì hỏi thẳng; không biết thì chỉ khi không field
            // nào khác giải thích được hậu quả (khi đó mở lại id là nguyên nhân duy nhất còn lại).
            if (timeChanged)
            {
                bool reopens = phase != LiveOpsChangeTextContext.EntryPhase.Unknown ? context.ReopensEndedEntry(change) : clauses.Count == 0;
                if (reopens) AddOnce(clauses, LiveOpsHubStrings.ChangeClauseReopenedEndedId);
            }
            if (clauses.Count == 0) clauses.Add(LiveOpsHubStrings.ChangeClauseWindowOrKey);
            return clauses;
        }

        private static string SafeConsequence(LiveEventCalendarChange change, LiveOpsChangeTextContext context)
        {
            if (change.ItemKind == LiveEventCalendarItemKind.EventType) return LiveOpsHubStrings.ChangeTypeDefinitionSafe;
            if (change.Kind == LiveEventCalendarChangeKind.Added) return LiveOpsHubStrings.ChangeAddedSafe;
            if (change.Kind == LiveEventCalendarChangeKind.Changed && change.Fields.Count == 0 && !change.WillBeDropped) return LiveOpsHubStrings.ChangeReturnedConsequence;
            if (change.RunningEventIdBefore.Length > 0) return LiveOpsHubStrings.ChangeRunningSafe;
            if (context != null && change.ItemKind == LiveEventCalendarItemKind.FixedEvent)
            {
                LiveOpsChangeTextContext.EntryPhase phase = context.PhaseBefore(change);
                if (phase == LiveOpsChangeTextContext.EntryPhase.Upcoming) return LiveOpsHubStrings.ChangeUpcomingSafe;
                if (phase == LiveOpsChangeTextContext.EntryPhase.Ended) return LiveOpsHubStrings.ChangeEndedSafe;
            }
            return LiveOpsHubStrings.ChangeNobodyLoses;
        }

        private static string ConfigKeyValue(string configKey)
        {
            return LiveOpsFindingText.IdText(configKey);
        }

        private static void AddOnce(List<string> clauses, string clause)
        {
            if (!clauses.Contains(clause)) clauses.Add(clause);
        }
    }

    /// <summary>
    /// Ngữ cảnh cho câu hàng diff: bản so, nháp, BÂY GIỜ và kết quả diff đã tính. Hàng diff cố ý không mang giờ/lý do bỏ (core giữ nó
    /// gọn và khớp thuật toán); câu thiết kế cần các thứ đó nên presenter truyền đúng hai tài liệu nó vừa so. Không tự so lại — dùng
    /// <paramref name="diff"/> presenter đã có để câu và hàng luôn cùng một lần so.
    /// </summary>
    internal sealed class LiveOpsChangeTextContext
    {
        internal enum EntryPhase
        {
            Unknown = 0,
            Upcoming = 1,
            Running = 2,
            Ended = 3,
        }

        private readonly LiveEventCalendarDocument _baseline;
        private readonly LiveEventCalendarDocument _draft;
        private readonly DateTime _nowUtc;
        private readonly LiveEventCalendarDiffResult _diff;
        private readonly bool _isComparedByEntryKey;
        private LiveEventCalendarCompilation _draftCompilation;

        /// <param name="isComparedByEntryKey">true khi <paramref name="diff"/> đến từ <c>CompareByEntryKey</c> ("chưa lưu") — danh tính theo
        /// EntryKey nên không có cặp xoá + thêm của đổi id đợt đã khép.</param>
        public LiveOpsChangeTextContext(LiveEventCalendarDocument baseline, LiveEventCalendarDocument draft, DateTime nowUtc,
            LiveEventCalendarDiffResult diff, bool isComparedByEntryKey)
        {
            _baseline = baseline ?? LiveEventCalendarDocument.Empty;
            _draft = draft ?? LiveEventCalendarDocument.Empty;
            _nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            _diff = diff;
            _isComparedByEntryKey = isComparedByEntryKey;
        }

        public DateTime NowUtc => _nowUtc;

        /// <summary>Biên dịch nháp theo thứ tự xuất (V-6) một lần, lười: chỉ hàng Bị bỏ cần.</summary>
        private LiveEventCalendarCompilation DraftCompilation
        {
            get
            {
                if (_draftCompilation == null) _draftCompilation = LiveEventCalendarCompiler.CompileInExportOrder(_draft);
                return _draftCompilation;
            }
        }

        /// <summary>Giai đoạn của mục ở BẢN SO lúc BÂY GIỜ (đợt cố định); mục mới hoặc giờ hỏng → Unknown.</summary>
        internal EntryPhase PhaseBefore(LiveEventCalendarChange change)
        {
            return PhaseOf(FixedBefore(change));
        }

        /// <summary>Mục nháp để trống configKey (xuất ghi mặc định của loại).</summary>
        internal bool InheritsConfigKey(LiveEventCalendarChange change)
        {
            if (change.ItemKind == LiveEventCalendarItemKind.RecurringRule)
            {
                return _draft.TryGetRecurringRule(change.ItemId, out RecurringLiveEventRule rule) && !rule.HasOwnConfigKey;
            }
            FixedLiveEventEntry after = FixedAfter(change);
            return after != null && !after.HasOwnConfigKey;
        }

        /// <summary>
        /// Lý do mục nháp bị game bỏ theo bộ biên dịch: "chồng 12 giờ với hunt-0914", "giờ kết thúc sai định dạng", "bị luật lặp che"…;
        /// "" khi không tìm được outcome (mục không còn trong nháp) hoặc mục được giữ.
        /// </summary>
        internal string DropReasonText(LiveEventCalendarChange change, LiveOpsHubFormat format)
        {
            LiveEventCalendarEntryOutcome outcome = DroppedOutcome(change);
            if (outcome == null) return string.Empty;

            if (outcome.DropReason == LiveEventCalendarDropReason.OverlapsSameType && outcome.OverlapStartUtc.HasValue && outcome.OverlapEndUtc.HasValue)
            {
                return LiveOpsFindingText.Format(LiveOpsHubStrings.FindingOverlapDetailFormat,
                    format.Duration(outcome.OverlapEndUtc.Value - outcome.OverlapStartUtc.Value, false), LiveOpsFindingText.IdText(outcome.RelatedEventId));
            }
            return LiveOpsFindingText.DropReasonPhrase(outcome.DropReason, outcome);
        }

        /// <summary>Lý do chính bộ biên dịch bỏ mục nháp; <c>None</c> khi mục được giữ hoặc không tìm được outcome.</summary>
        internal LiveEventCalendarDropReason DropReasonOf(LiveEventCalendarChange change)
        {
            LiveEventCalendarEntryOutcome outcome = DroppedOutcome(change);
            return outcome != null ? outcome.DropReason : LiveEventCalendarDropReason.None;
        }

        /// <summary>
        /// Mục ở bản so đã khép lúc BÂY GIỜ và mục nháp cùng danh tính chưa khép — mở lại id của đợt đã khép cho tương lai (6.3 hàng 8,
        /// <c>RetiredEventIds</c>). Đổi giờ vẫn nằm trong quá khứ thì false.
        /// </summary>
        internal bool ReopensEndedEntry(LiveEventCalendarChange change)
        {
            if (PhaseOf(FixedBefore(change)) != EntryPhase.Ended) return false;
            FixedLiveEventEntry after = FixedAfter(change);
            return after != null && after.TryGetEndUtc(out DateTime endUtc) && endUtc > _nowUtc;
        }

        private LiveEventCalendarEntryOutcome DroppedOutcome(LiveEventCalendarChange change)
        {
            LiveEventCalendarEntryOutcome outcome = null;
            if (change.ItemKind == LiveEventCalendarItemKind.FixedEvent)
            {
                if (change.EntryKey.Length == 0 || !DraftCompilation.TryGetFixedOutcome(change.EntryKey, out outcome)) return null;
            }
            else if (change.ItemKind == LiveEventCalendarItemKind.RecurringRule)
            {
                if (!DraftCompilation.TryGetRecurringOutcome(change.ItemId, out outcome)) return null;
            }
            return outcome == null || outcome.IsKept ? null : outcome;
        }

        /// <summary>
        /// (V-20 CC-DIFF-3) Nửa còn lại của một lần đổi id đợt đã khép ở diff với bản đã đăng: hàng Xoá (mục chỉ ở bản so, đã khép) ghép
        /// với hàng Thêm cùng loại + cùng nguyên văn startUtc/endUtc — đúng dấu hiệu <c>LiveEventCalendarDiff</c> dùng để xếp cả hai nửa Nên xem.
        /// Ghép một-một theo thứ tự hàng để hai lần đổi id cùng khung không dồn vào một nửa. null khi không phải ca đó.
        /// </summary>
        internal LiveEventCalendarChange FindEndedRenamePartner(LiveEventCalendarChange change)
        {
            if (_isComparedByEntryKey || _diff == null || change.ItemKind != LiveEventCalendarItemKind.FixedEvent) return null;
            if (change.Consequence != LiveEventCalendarConsequence.ShouldReview) return null;
            if (change.Kind != LiveEventCalendarChangeKind.Removed && change.Kind != LiveEventCalendarChangeKind.Added) return null;

            var usedAdded = new HashSet<LiveEventCalendarChange>();
            IReadOnlyList<LiveEventCalendarChange> changes = _diff.Changes;
            for (int removedIndex = 0; removedIndex < changes.Count; removedIndex++)
            {
                LiveEventCalendarChange removed = changes[removedIndex];
                if (!IsRenameCandidate(removed, LiveEventCalendarChangeKind.Removed)) continue;
                FixedLiveEventEntry before = FixedBefore(removed);
                if (before == null || PhaseOf(before) != EntryPhase.Ended) continue;

                for (int addedIndex = 0; addedIndex < changes.Count; addedIndex++)
                {
                    LiveEventCalendarChange added = changes[addedIndex];
                    if (!IsRenameCandidate(added, LiveEventCalendarChangeKind.Added) || usedAdded.Contains(added)) continue;
                    FixedLiveEventEntry after = FixedAfter(added);
                    if (after == null || !SameTypeAndWindow(before, after)) continue;

                    usedAdded.Add(added);
                    if (ReferenceEquals(removed, change)) return added;
                    if (ReferenceEquals(added, change)) return removed;
                    break;
                }
            }
            return null;
        }

        private static bool IsRenameCandidate(LiveEventCalendarChange change, LiveEventCalendarChangeKind kind)
        {
            return change.Kind == kind && change.ItemKind == LiveEventCalendarItemKind.FixedEvent && change.Consequence == LiveEventCalendarConsequence.ShouldReview;
        }

        private static bool SameTypeAndWindow(FixedLiveEventEntry before, FixedLiveEventEntry after)
        {
            return string.Equals(before.EventType, after.EventType, StringComparison.Ordinal) &&
                   string.Equals(before.StartUtcText, after.StartUtcText, StringComparison.Ordinal) &&
                   string.Equals(before.EndUtcText, after.EndUtcText, StringComparison.Ordinal);
        }

        /// <summary>Mục của bản so: theo EntryKey khi so "chưa lưu", theo id (mục đứng trước trong thứ tự xuất) khi so với bản đã đăng.</summary>
        private FixedLiveEventEntry FixedBefore(LiveEventCalendarChange change)
        {
            if (change.ItemKind != LiveEventCalendarItemKind.FixedEvent || change.Kind == LiveEventCalendarChangeKind.Added) return null;
            if (_isComparedByEntryKey && change.EntryKey.Length > 0)
            {
                return _baseline.TryGetFixedEvent(change.EntryKey, out FixedLiveEventEntry byKey) ? byKey : null;
            }
            IReadOnlyList<FixedLiveEventEntry> entries = LiveEventCalendarExportOrder.Apply(_baseline).FixedEvents;
            for (int index = 0; index < entries.Count; index++)
            {
                if (string.Equals(entries[index].EventId, change.ItemId, StringComparison.Ordinal)) return entries[index];
            }
            return null;
        }

        private FixedLiveEventEntry FixedAfter(LiveEventCalendarChange change)
        {
            if (change.ItemKind != LiveEventCalendarItemKind.FixedEvent || change.EntryKey.Length == 0) return null;
            return _draft.TryGetFixedEvent(change.EntryKey, out FixedLiveEventEntry entry) ? entry : null;
        }

        private EntryPhase PhaseOf(FixedLiveEventEntry entry)
        {
            if (entry == null || !entry.TryGetStartUtc(out DateTime startUtc) || !entry.TryGetEndUtc(out DateTime endUtc)) return EntryPhase.Unknown;
            if (_nowUtc < startUtc) return EntryPhase.Upcoming;
            return endUtc <= _nowUtc ? EntryPhase.Ended : EntryPhase.Running;
        }
    }
}
