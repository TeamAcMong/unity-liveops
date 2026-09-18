namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Calendar (G-CALENDAR, mục 7.3): toolbar màn Lịch, inspector đợt, luồng xoá ba mức, hộp rút ngắn đợt đang chạy,
    /// toast của kéo/sửa và popover Thêm đợt ba bước. Chữ mà chính control timeline tự hiện (header làn, thanh, thước, readout,
    /// minimap, chú giải, dòng gợi ý) nằm ở vùng Timeline/TimelineModel — màn không khai lại. Câu phát hiện lấy từ
    /// <see cref="LiveOpsFindingText"/> và câu thay đổi từ <see cref="LiveOpsChangeText"/> (V-8, V-22 CC-FT-1); ở đây chỉ có
    /// khung ghép quanh câu đó.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Toolbar màn [SD1 §3.1]. Nhãn khoảng ghép hai đầu để bản dịch tự chọn dấu nối.
        internal static string CalendarRangePreviousTooltip => LiveOpsHubStringCatalog.Text(nameof(CalendarRangePreviousTooltip));
        internal static string CalendarRangeNextTooltip => LiveOpsHubStringCatalog.Text(nameof(CalendarRangeNextTooltip));
        internal static string CalendarRangeLabelFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarRangeLabelFormat));
        internal static string CalendarRangeMenuEarlier => LiveOpsHubStringCatalog.Text(nameof(CalendarRangeMenuEarlier));
        internal static string CalendarRangeMenuLater => LiveOpsHubStringCatalog.Text(nameof(CalendarRangeMenuLater));
        internal static string CalendarRangeMenuPickStart => LiveOpsHubStringCatalog.Text(nameof(CalendarRangeMenuPickStart));

        // "Hôm nay" tắt khi khung đã chứa hôm nay: nút không làm gì thì phải nói vì sao, không im lặng nhấp nháy (7.0).
        internal static string CalendarTodayButton => LiveOpsHubStringCatalog.Text(nameof(CalendarTodayButton));
        internal static string CalendarTodayDisabledReason => LiveOpsHubStringCatalog.Text(nameof(CalendarTodayDisabledReason));

        internal static string CalendarZoomChoices => LiveOpsHubStringCatalog.Text(nameof(CalendarZoomChoices));

        // Nhãn menu "Bắt lưới": lựa chọn đi thẳng xuống bước bắt lưới của cử chỉ kéo (nợ D-3(c) làm ở W5).
        internal static string CalendarSnapMenuFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarSnapMenuFormat));
        internal static string CalendarSnapAuto => LiveOpsHubStringCatalog.Text(nameof(CalendarSnapAuto));
        internal static string CalendarSnapFifteenMinutes => LiveOpsHubStringCatalog.Text(nameof(CalendarSnapFifteenMinutes));
        internal static string CalendarSnapHour => LiveOpsHubStringCatalog.Text(nameof(CalendarSnapHour));
        internal static string CalendarSnapDay => LiveOpsHubStringCatalog.Text(nameof(CalendarSnapDay));
        internal static string CalendarSnapOff => LiveOpsHubStringCatalog.Text(nameof(CalendarSnapOff));
        internal static string CalendarSearchPlaceholder => LiveOpsHubStringCatalog.Text(nameof(CalendarSearchPlaceholder));
        internal static string CalendarHiddenLanesChipFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarHiddenLanesChipFormat));

        // Nút chính của section header.
        internal static string CalendarAddEventButton => LiveOpsHubStringCatalog.Text(nameof(CalendarAddEventButton));

        // Nạp UXML hỏng: câu này đi vào card lỗi của shell nên phải nêu đúng đường dẫn để sửa được ngay.
        internal static string CalendarMissingLayoutMessageFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarMissingLayoutMessageFormat));

        // Trạng thái chung "chưa có asset" (7.0): mọi màn trừ Tổng quan nói cùng câu và chỉ về đúng một chỗ tạo lịch.
        internal static string CalendarNoAssetEmptyText => LiveOpsHubStringCatalog.Text(nameof(CalendarNoAssetEmptyText));
        internal static string CalendarOpenOverviewButton => LiveOpsHubStringCatalog.Text(nameof(CalendarOpenOverviewButton));

        // Inspector (a) chưa chọn [SD1 §3.10]: nói làm gì để có nội dung, kèm tóm tắt để pane không rỗng vô nghĩa.
        internal static string CalendarInspectorEmptyText => LiveOpsHubStringCatalog.Text(nameof(CalendarInspectorEmptyText));
        internal static string CalendarInspectorSummaryFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarInspectorSummaryFormat));

        // Nhãn field của inspector và của popover Thêm đợt — dùng chung để hai chỗ không gọi khác tên cùng một thứ.
        internal static string CalendarFieldIdLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarFieldIdLabel));
        internal static string CalendarFieldTypeLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarFieldTypeLabel));
        internal static string CalendarFieldStartLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarFieldStartLabel));
        internal static string CalendarFieldEndLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarFieldEndLabel));
        internal static string CalendarFieldDurationLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarFieldDurationLabel));
        internal static string CalendarDurationUnitLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarDurationUnitLabel));
        internal static string CalendarDurationNoteFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDurationNoteFormat));
        internal static string CalendarFieldConfigKeyLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarFieldConfigKeyLabel));
        internal static string CalendarFieldSourceLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarFieldSourceLabel));
        internal static string CalendarFieldRequiresOptInLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarFieldRequiresOptInLabel));

        // Config key: chữ dẫn nghiêng nói rõ giá trị sẽ ghi ra JSON, để ô trống không bị đọc nhầm là "chưa cấu hình".
        internal static string CalendarConfigKeyPlaceholderFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarConfigKeyPlaceholderFormat));
        internal static string CalendarConfigKeyResetButton => LiveOpsHubStringCatalog.Text(nameof(CalendarConfigKeyResetButton));
        internal static string CalendarConfigKeyResetTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarConfigKeyResetTooltipFormat));

        // "Phải bấm tham gia" chỉ đọc ở Lịch: giá trị thuộc loại event, sửa ở màn Loại event (một nguồn sự thật).
        internal static string CalendarRequiresOptInYesFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarRequiresOptInYesFormat));
        internal static string CalendarRequiresOptInNoFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarRequiresOptInNoFormat));
        internal static string CalendarEditTypeLink => LiveOpsHubStringCatalog.Text(nameof(CalendarEditTypeLink));

        internal static string CalendarSourceFixedFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarSourceFixedFormat));
        internal static string CalendarSourceRecurringFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarSourceRecurringFormat));
        internal static string CalendarIssuesFoldoutFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarIssuesFoldoutFormat));

        // Mép đầu đợt đang chạy khoá: người chơi đã vào theo giờ cũ nên tooltip nói thẳng "đã chạy từ".
        internal static string CalendarStartLockedTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarStartLockedTooltipFormat));

        // (b) đợt sinh từ luật: field disabled thì phải chỉ đúng chỗ sửa được, không để người dùng bấm vào ô chết.
        internal static string CalendarRecurringNoteFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarRecurringNoteFormat));
        internal static string CalendarOpenRuleButton => LiveOpsHubStringCatalog.Text(nameof(CalendarOpenRuleButton));

        // (d) không đặt được: chuỗi gốc vẫn ở trong asset; nút đề nghị đúng một giá trị đọc được.
        internal static string CalendarUnreadableFixButtonFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarUnreadableFixButtonFormat));

        // Tag giai đoạn trên pane-title.
        internal static string CalendarPhaseUpcoming => LiveOpsHubStringCatalog.Text(nameof(CalendarPhaseUpcoming));
        internal static string CalendarPhaseRunning => LiveOpsHubStringCatalog.Text(nameof(CalendarPhaseRunning));
        internal static string CalendarPhaseEnded => LiveOpsHubStringCatalog.Text(nameof(CalendarPhaseEnded));
        internal static string CalendarPhaseUnplaceable => LiveOpsHubStringCatalog.Text(nameof(CalendarPhaseUnplaceable));

        // Xoá ba mức [FD §3.10]: nhãn nút đã cho biết có hộp hay không ("…" = sẽ hỏi).
        internal static string CalendarDeleteButton => LiveOpsHubStringCatalog.Text(nameof(CalendarDeleteButton));
        internal static string CalendarDeleteButtonWithDialog => LiveOpsHubStringCatalog.Text(nameof(CalendarDeleteButtonWithDialog));
        internal static string CalendarDeleteButtonImmediateTooltip => LiveOpsHubStringCatalog.Text(nameof(CalendarDeleteButtonImmediateTooltip));
        internal static string CalendarDeleteButtonConfirmTooltip => LiveOpsHubStringCatalog.Text(nameof(CalendarDeleteButtonConfirmTooltip));

        // Toast xoá có hai dạng: đủ giờ, và dạng ngắn cho cửa sổ dưới 1100px (giờ đủ ở tooltip) — PD của 7.3.
        internal static string CalendarDeleteToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDeleteToastFormat));
        internal static string CalendarDeleteToastShortFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDeleteToastShortFormat));

        internal static string CalendarDeleteConfirmTitleFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDeleteConfirmTitleFormat));
        internal static string CalendarDeleteRunningConfirmTitleFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDeleteRunningConfirmTitleFormat));
        internal static string CalendarDeletePublishedBodyFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDeletePublishedBodyFormat));
        internal static string CalendarDeleteRunningBodyFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDeleteRunningBodyFormat));

        // Hộp xoá nêu luôn đợt hết chồng giờ nhờ xoá: tính bằng CheckLane trước/sau nên là hệ quả thật, không phải lời hứa.
        internal static string CalendarOverlapClearedFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarOverlapClearedFormat));
        internal static string CalendarUndoHintFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarUndoHintFormat));

        // Lệnh sửa từ inspector (đổi id, đổi loại, config key, áp đề xuất) trên đợt đang chạy: hộp phải hỏi bằng câu hỏi và nút
        // phá huỷ phải nói đúng việc sắp làm — nhãn "Rút ngắn đợt" ở đây là nói sai việc (7.3).
        internal static string CalendarEditRunningConfirmTitleFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarEditRunningConfirmTitleFormat));
        internal static string CalendarEditDestructiveLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarEditDestructiveLabel));

        // Hộp xoá cấp 1 khi phiên chưa có dấu đã đăng đọc được: biến thể KHÔNG chừa chỗ cho giờ đăng, kẻo câu cụt "trong bản đăng :".
        internal static string CalendarDeletePublishedNoStampBodyFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDeletePublishedNoStampBodyFormat));

        // Menu khoảng: "Chọn ngày bắt đầu…" mở popover một ô ngày; nút chính nói thẳng kết quả sẽ xảy ra.
        internal static string CalendarPickRangeStartApplyButton => LiveOpsHubStringCatalog.Text(nameof(CalendarPickRangeStartApplyButton));

        // Rút ngắn đợt đang chạy [SD1 §3.15]: nhãn nút an toàn nêu giờ sẽ giữ, để đọc nút là biết kết quả.
        internal static string CalendarShortenConfirmTitleFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarShortenConfirmTitleFormat));
        internal static string CalendarShortenConfirmBodyFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarShortenConfirmBodyFormat));
        internal static string CalendarShortenDestructiveLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarShortenDestructiveLabel));
        internal static string CalendarKeepEndLabelFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarKeepEndLabelFormat));

        // Câu hậu quả P1 (7.0): chưa có LiveOpsStateReader nên luôn nói "không biết số người chơi toàn cục" trước mọi số đo.
        internal static string CalendarUnknownPlayerCountSentence => LiveOpsHubStringCatalog.Text(nameof(CalendarUnknownPlayerCountSentence));
        internal static string CalendarNoEditorRecordFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarNoEditorRecordFormat));

        // Toast của kéo/sửa — tên bước Undo trùng câu toast nên câu phải đủ để tìm lại trong Undo History.
        internal static string CalendarMoveToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarMoveToastFormat));
        internal static string CalendarMoveEndToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarMoveEndToastFormat));

        /// <summary>Tên bước Undo của lệnh dời — câu ngắn hiện sau "Đã hoàn tác: " [SD1 §3.8 khung 14].</summary>
        internal static string CalendarMoveUndoStepFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarMoveUndoStepFormat));

        /// <summary>
        /// Tên bước NGẮN cho hai kiểu kéo MÉP (đổi bắt đầu / đổi kết thúc) — [SD1 §3.4] (Q-W5-5). Kéo cả thanh dùng
        /// <see cref="CalendarMoveUndoStepFormat"/>: "dời" và "đổi" là hai việc khác nhau với người đọc lại lịch sử.
        /// </summary>
        internal static string CalendarResizeUndoStepFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarResizeUndoStepFormat));
        internal static string CalendarMoveStartToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarMoveStartToastFormat));
        internal static string CalendarRenameToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarRenameToastFormat));
        internal static string CalendarRetypeToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarRetypeToastFormat));
        internal static string CalendarConfigKeyToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarConfigKeyToastFormat));

        // Tên bước Undo NGẮN của ba lệnh sửa trong inspector (W8-UX, UJ-10). Không có chúng thì bước Undo mang chính câu toast
        // "Đã đổi …", và toast sau ⌘Z in "Đã hoàn tác: Đã đổi …" — lặp chữ, đọc như hai việc.
        internal static string CalendarRenameUndoStepFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarRenameUndoStepFormat));
        internal static string CalendarRetypeUndoStepFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarRetypeUndoStepFormat));
        internal static string CalendarConfigKeyUndoStepFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarConfigKeyUndoStepFormat));
        internal static string CalendarAddToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarAddToastFormat));
        internal static string CalendarRepairToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarRepairToastFormat));

        // Popover Thêm đợt [SD1 §3.11]: tiêu đề nêu bước đang ở, nút cuối nêu id sẽ tạo.
        internal static string CalendarAddStepTypeTitleFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarAddStepTypeTitleFormat));
        internal static string CalendarAddReviewTitle => LiveOpsHubStringCatalog.Text(nameof(CalendarAddReviewTitle));
        internal static string CalendarAddTypeFilterPlaceholder => LiveOpsHubStringCatalog.Text(nameof(CalendarAddTypeFilterPlaceholder));
        internal static string CalendarAddTypeFixedTag => LiveOpsHubStringCatalog.Text(nameof(CalendarAddTypeFixedTag));
        internal static string CalendarAddTypeOptInTag => LiveOpsHubStringCatalog.Text(nameof(CalendarAddTypeOptInTag));
        internal static string CalendarAddTypeRecurringTag => LiveOpsHubStringCatalog.Text(nameof(CalendarAddTypeRecurringTag));
        internal static string CalendarAddKeyHint => LiveOpsHubStringCatalog.Text(nameof(CalendarAddKeyHint));
        internal static string CalendarAddCancelButton => LiveOpsHubStringCatalog.Text(nameof(CalendarAddCancelButton));
        internal static string CalendarAddNextButton => LiveOpsHubStringCatalog.Text(nameof(CalendarAddNextButton));
        internal static string CalendarAddBackButton => LiveOpsHubStringCatalog.Text(nameof(CalendarAddBackButton));
        internal static string CalendarAddEndAutoNote => LiveOpsHubStringCatalog.Text(nameof(CalendarAddEndAutoNote));
        internal static string CalendarAddTimesLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarAddTimesLabel));
        internal static string CalendarTimeRangeFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarTimeRangeFormat));
        internal static string CalendarAddConfigKeyNoteFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarAddConfigKeyNoteFormat));
        internal static string CalendarAddSubmitFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarAddSubmitFormat));
        internal static string CalendarAddBackToTimesButton => LiveOpsHubStringCatalog.Text(nameof(CalendarAddBackToTimesButton));
        internal static string CalendarAddAnywayButton => LiveOpsHubStringCatalog.Text(nameof(CalendarAddAnywayButton));

        // Kiểm nhanh chỉ xét làn của loại đó, nên tag nói rõ phạm vi để không bị đọc thành "cả lịch đã sạch".
        internal static string CalendarQuickCheckOkTag => LiveOpsHubStringCatalog.Text(nameof(CalendarQuickCheckOkTag));
    }
}
