namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Recurring (G-RECURRING, mục 7.4): màn Luật lặp — câu token đọc được như một câu, nhãn field, thanh chu kỳ,
    /// bảng "5 đợt kế tiếp", và toàn bộ luồng hai bước đổi tiền tố / neo / chu kỳ / thời gian chạy khi có lần lặp đang chạy.
    /// Câu của hai ngôn ngữ ở <c>Language/LiveOpsHubStringCatalog.Recurring.cs</c>; comment "vì sao" ở lại đây cạnh property.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Nút của section header và lý do khi bị khoá (SP-3: lý do in thành chữ cạnh nút, tooltip chỉ phụ).
        internal static string RecurringAddRuleButton => LiveOpsHubStringCatalog.Text(nameof(RecurringAddRuleButton));
        internal static string RecurringAddRuleNoTypeReason => LiveOpsHubStringCatalog.Text(nameof(RecurringAddRuleNoTypeReason));
        internal static string RecurringAddRuleNoAssetReason => LiveOpsHubStringCatalog.Text(nameof(RecurringAddRuleNoAssetReason));
        internal static string RecurringDeleteRuleButton => LiveOpsHubStringCatalog.Text(nameof(RecurringDeleteRuleButton));

        // Trạng thái trống [SD1 §4.3]: dòng đầu nói vì sao trống, dòng sau nói luật lặp là gì để người mới biết nên bấm gì.
        internal static string RecurringEmptyTitle => LiveOpsHubStringCatalog.Text(nameof(RecurringEmptyTitle));
        internal static string RecurringEmptyBody => LiveOpsHubStringCatalog.Text(nameof(RecurringEmptyBody));
        internal static string RecurringNoAssetTitle => LiveOpsHubStringCatalog.Text(nameof(RecurringNoAssetTitle));
        internal static string RecurringNoAssetBody => LiveOpsHubStringCatalog.Text(nameof(RecurringNoAssetBody));

        /// <summary>UXML của màn thiếu trên đĩa = package hỏng: card lỗi của khung nêu đúng đường dẫn (8.1 bước 2).</summary>
        internal static string RecurringMissingLayoutFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringMissingLayoutFormat));

        // Hàng danh sách luật: "mỗi 7 ngày · chạy 7 ngày".
        internal static string RecurringListMetaFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringListMetaFormat));

        // Câu token [SD1 §4.1] — bốn mẩu chữ xen giữa bốn token; bấm token thì focus field tương ứng.
        internal static string RecurringSentenceEveryPrefix => LiveOpsHubStringCatalog.Text(nameof(RecurringSentenceEveryPrefix));
        internal static string RecurringSentenceAnchorPrefix => LiveOpsHubStringCatalog.Text(nameof(RecurringSentenceAnchorPrefix));
        internal static string RecurringSentenceActivePrefix => LiveOpsHubStringCatalog.Text(nameof(RecurringSentenceActivePrefix));
        internal static string RecurringSentenceIdPrefix => LiveOpsHubStringCatalog.Text(nameof(RecurringSentenceIdPrefix));
        internal static string RecurringSentenceIdSuffix => LiveOpsHubStringCatalog.Text(nameof(RecurringSentenceIdSuffix));

        // Field của form.
        internal static string RecurringPresetLabel => LiveOpsHubStringCatalog.Text(nameof(RecurringPresetLabel));
        internal static string RecurringPresetCustom => LiveOpsHubStringCatalog.Text(nameof(RecurringPresetCustom));
        internal static string RecurringEventTypeLabel => LiveOpsHubStringCatalog.Text(nameof(RecurringEventTypeLabel));

        /// <summary>Loại khoá sau khi tạo: đổi loại = luật khác hẳn (id, người chơi, dữ liệu) nên phải tạo luật mới.</summary>
        internal static string RecurringEventTypeLockedTooltip => LiveOpsHubStringCatalog.Text(nameof(RecurringEventTypeLockedTooltip));
        internal static string RecurringIdPrefixLabel => LiveOpsHubStringCatalog.Text(nameof(RecurringIdPrefixLabel));

        /// <summary>Tiền tố để trống = runtime tự ghép <c>eventType + "-"</c> — nói thẳng giá trị thật thay vì để người dùng đoán.</summary>
        internal static string RecurringIdPrefixDefaultFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringIdPrefixDefaultFormat));
        internal static string RecurringAnchorLabel => LiveOpsHubStringCatalog.Text(nameof(RecurringAnchorLabel));
        internal static string RecurringPeriodHoursLabel => LiveOpsHubStringCatalog.Text(nameof(RecurringPeriodHoursLabel));
        internal static string RecurringActiveHoursLabel => LiveOpsHubStringCatalog.Text(nameof(RecurringActiveHoursLabel));

        /// <summary>Chữ phụ đổi giờ sang đơn vị người đọc được: "168" → "= 7 ngày".</summary>
        internal static string RecurringEqualsFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringEqualsFormat));
        internal static string RecurringAnchorDeviceLineFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringAnchorDeviceLineFormat));

        /// <summary>Token neo trong câu đọc: thứ · ngày có năm · giờ · "UTC" — bốn mẩu ghép lại "thứ Hai 5/1/2026 00:00 UTC".</summary>
        internal static string RecurringAnchorTokenFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringAnchorTokenFormat));

        // Câu dưới thanh chu kỳ [SD1 §4.1].
        internal static string RecurringCycleSeamlessFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringCycleSeamlessFormat));
        internal static string RecurringCycleWithRestFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringCycleWithRestFormat));
        internal static string RecurringCycleOverflowFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringCycleOverflowFormat));

        // Lỗi field + chú thích neo [SD1 §4.3].
        internal static string RecurringActiveLongerThanPeriodError => LiveOpsHubStringCatalog.Text(nameof(RecurringActiveLongerThanPeriodError));
        internal static string RecurringPeriodMustBePositiveError => LiveOpsHubStringCatalog.Text(nameof(RecurringPeriodMustBePositiveError));
        internal static string RecurringActiveMustBePositiveError => LiveOpsHubStringCatalog.Text(nameof(RecurringActiveMustBePositiveError));
        internal static string RecurringAnchorUnreadableError => LiveOpsHubStringCatalog.Text(nameof(RecurringAnchorUnreadableError));
        internal static string RecurringAnchorNotice => LiveOpsHubStringCatalog.Text(nameof(RecurringAnchorNotice));

        // Card "5 đợt kế tiếp".
        internal static string RecurringOccurrencesTitleFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringOccurrencesTitleFormat));
        internal static string RecurringOccurrencesSubtitleFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringOccurrencesSubtitleFormat));
        internal static string RecurringColumnId => LiveOpsHubStringCatalog.Text(nameof(RecurringColumnId));
        internal static string RecurringColumnStartUtc => LiveOpsHubStringCatalog.Text(nameof(RecurringColumnStartUtc));
        internal static string RecurringColumnEndUtc => LiveOpsHubStringCatalog.Text(nameof(RecurringColumnEndUtc));
        internal static string RecurringColumnDeviceTime => LiveOpsHubStringCatalog.Text(nameof(RecurringColumnDeviceTime));
        internal static string RecurringColumnNow => LiveOpsHubStringCatalog.Text(nameof(RecurringColumnNow));
        internal static string RecurringPhaseRunning => LiveOpsHubStringCatalog.Text(nameof(RecurringPhaseRunning));
        internal static string RecurringPhaseUpcoming => LiveOpsHubStringCatalog.Text(nameof(RecurringPhaseUpcoming));
        internal static string RecurringPhaseEnded => LiveOpsHubStringCatalog.Text(nameof(RecurringPhaseEnded));
        internal static string RecurringPhaseWithRelativeFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringPhaseWithRelativeFormat));

        /// <summary>Tag riêng có thoi Warning ở cột "Lúc này" — không trộn vào dấu giai đoạn ([SD1 §4.1]).</summary>
        internal static string RecurringIdChangeTag => LiveOpsHubStringCatalog.Text(nameof(RecurringIdChangeTag));

        /// <summary>Mũi tên trong cột Id là Label Inter riêng: RobotoMono không có glyph → ([SD1 §4.1]).</summary>
        internal static string RecurringIdArrow => LiveOpsHubStringCatalog.Text(nameof(RecurringIdArrow));
        internal static string RecurringAddMoreButtonFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringAddMoreButtonFormat));

        /// <summary>Trong lúc chờ debounce card chỉ nói "đang tính…" — không lộ số mili giây cho người dùng.</summary>
        internal static string RecurringComputingNote => LiveOpsHubStringCatalog.Text(nameof(RecurringComputingNote));
        internal static string RecurringOccurrenceLimitNoteFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringOccurrenceLimitNoteFormat));

        // Luồng hai bước [SD1 §4.2]: bước 1 là nháp tại ô, Main.asset chưa đổi nên các màn khác vẫn thấy giá trị cũ.
        internal static string RecurringDraftNoticeFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringDraftNoticeFormat));
        internal static string RecurringPrefixConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringPrefixConsequenceFormat));
        internal static string RecurringIdentityConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringIdentityConsequenceFormat));
        internal static string RecurringActiveHoursConsequenceFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringActiveHoursConsequenceFormat));
        internal static string RecurringDraftCancelButton => LiveOpsHubStringCatalog.Text(nameof(RecurringDraftCancelButton));
        internal static string RecurringDraftWritePrefixButton => LiveOpsHubStringCatalog.Text(nameof(RecurringDraftWritePrefixButton));
        internal static string RecurringDraftWriteValueButton => LiveOpsHubStringCatalog.Text(nameof(RecurringDraftWriteValueButton));

        /// <summary>Lỗi lập trình: dựng hộp xác nhận cho một nháp không cần hỏi — không bao giờ hiện trên UI.</summary>
        internal static string RecurringErrorNoConfirmationNeeded => LiveOpsHubStringCatalog.Text(nameof(RecurringErrorNoConfirmationNeeded));

        // Bước 2: hộp xác nhận (cấp 2 gõ tên với đổi id, cấp 1 với rút ngắn thời gian chạy).
        internal static string RecurringConfirmPrefixTitle => LiveOpsHubStringCatalog.Text(nameof(RecurringConfirmPrefixTitle));
        internal static string RecurringConfirmPrefixBodyFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringConfirmPrefixBodyFormat));
        internal static string RecurringConfirmPrefixDestructive => LiveOpsHubStringCatalog.Text(nameof(RecurringConfirmPrefixDestructive));
        internal static string RecurringConfirmPrefixSafe => LiveOpsHubStringCatalog.Text(nameof(RecurringConfirmPrefixSafe));
        internal static string RecurringConfirmPrefixKeyHint => LiveOpsHubStringCatalog.Text(nameof(RecurringConfirmPrefixKeyHint));
        internal static string RecurringConfirmIdentityTitle => LiveOpsHubStringCatalog.Text(nameof(RecurringConfirmIdentityTitle));
        internal static string RecurringConfirmIdentityDestructive => LiveOpsHubStringCatalog.Text(nameof(RecurringConfirmIdentityDestructive));
        internal static string RecurringConfirmActiveTitle => LiveOpsHubStringCatalog.Text(nameof(RecurringConfirmActiveTitle));
        internal static string RecurringConfirmActiveBodyFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringConfirmActiveBodyFormat));
        internal static string RecurringConfirmActiveDestructive => LiveOpsHubStringCatalog.Text(nameof(RecurringConfirmActiveDestructive));
        internal static string RecurringConfirmActiveKeyHint => LiveOpsHubStringCatalog.Text(nameof(RecurringConfirmActiveKeyHint));

        // Xoá luật (bảng 7.0: có lần lặp đang chạy → cấp 2, không thì xoá thẳng kèm Hoàn tác).
        internal static string RecurringDeleteConfirmTitleFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringDeleteConfirmTitleFormat));
        internal static string RecurringDeleteConfirmBodyFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringDeleteConfirmBodyFormat));
        internal static string RecurringDeleteConfirmDestructive => LiveOpsHubStringCatalog.Text(nameof(RecurringDeleteConfirmDestructive));
        internal static string RecurringDeleteToastFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringDeleteToastFormat));

        // Sau khi ghi: HelpBox ở lại dưới ô tới khi lần lặp cũ khép (mục 7.4).
        internal static string RecurringAfterWriteNoticeFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringAfterWriteNoticeFormat));
        internal static string RecurringRevertPrefixButtonFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringRevertPrefixButtonFormat));
        internal static string RecurringDecideInValidationLink => LiveOpsHubStringCatalog.Text(nameof(RecurringDecideInValidationLink));

        // Toast = tên Undo group của lệnh sửa (8.5).
        internal static string RecurringWriteToastFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringWriteToastFormat));
        internal static string RecurringAddToastFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringAddToastFormat));
        internal static string RecurringPrefixToastFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringPrefixToastFormat));

        // Popover "Thêm luật": chọn loại chưa có luật + mẫu rồi mới ghi.
        internal static string RecurringAddTypeLabel => LiveOpsHubStringCatalog.Text(nameof(RecurringAddTypeLabel));
        internal static string RecurringAddPresetLabel => LiveOpsHubStringCatalog.Text(nameof(RecurringAddPresetLabel));
        internal static string RecurringAddConfirmButton => LiveOpsHubStringCatalog.Text(nameof(RecurringAddConfirmButton));
        internal static string RecurringAddCancelButton => LiveOpsHubStringCatalog.Text(nameof(RecurringAddCancelButton));

        // Foldout JSON: W4 chỉ đọc + Copy; G-RECURRING-JSON (W5) làm sửa được.
        internal static string RecurringJsonFoldoutLabel => LiveOpsHubStringCatalog.Text(nameof(RecurringJsonFoldoutLabel));
        internal static string RecurringJsonCopyButton => LiveOpsHubStringCatalog.Text(nameof(RecurringJsonCopyButton));
        internal static string RecurringJsonCopiedToastFormat => LiveOpsHubStringCatalog.Text(nameof(RecurringJsonCopiedToastFormat));

        // INTERIM(G-RECURRING-JSON): câu nói foldout chưa sửa được ở bản W4 — G-RECURRING-JSON gỡ cùng ô sửa (mục 12 I-6).
        internal static string RecurringJsonReadOnlyNote => LiveOpsHubStringCatalog.Text(nameof(RecurringJsonReadOnlyNote));

        // Tên mẫu dựng sẵn (LiveOpsRulePresets.BuiltIn) — người dùng đọc trong dropdown Mẫu.
        internal static string RecurringPresetWeeklyMonday => LiveOpsHubStringCatalog.Text(nameof(RecurringPresetWeeklyMonday));
        internal static string RecurringPresetDailyRunTwenty => LiveOpsHubStringCatalog.Text(nameof(RecurringPresetDailyRunTwenty));
        internal static string RecurringPresetBiweeklyMonday => LiveOpsHubStringCatalog.Text(nameof(RecurringPresetBiweeklyMonday));
    }
}
