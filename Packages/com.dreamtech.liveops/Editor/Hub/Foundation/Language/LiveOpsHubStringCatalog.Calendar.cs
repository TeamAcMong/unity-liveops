namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Calendar — bản tiếng Việt là bản gốc (chép nguyên văn từ hình thiết kế [SD1 §3]), bản tiếng Anh dịch sát nghĩa.
    /// Hai ngôn ngữ nằm trong cùng một lệnh đăng ký để chỗ giữ chỗ <c>{0}</c>/<c>{1}</c> soát được bằng mắt và không bao giờ thêm
    /// khoá ở bản này mà quên bản kia. Comment "vì sao" của từng câu ở lại <c>LiveOpsHubStrings.Calendar.cs</c> cạnh property.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterCalendar(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.CalendarRangePreviousTooltip),
                vietnamese: "Khoảng trước",
                english: "Previous range");
            table.Add(nameof(LiveOpsHubStrings.CalendarRangeNextTooltip),
                vietnamese: "Khoảng sau",
                english: "Next range");
            table.AddShared(nameof(LiveOpsHubStrings.CalendarRangeLabelFormat), "{0} – {1}");
            table.Add(nameof(LiveOpsHubStrings.CalendarRangeMenuEarlier),
                vietnamese: "3 tuần trước",
                english: "3 weeks earlier");
            table.Add(nameof(LiveOpsHubStrings.CalendarRangeMenuLater),
                vietnamese: "3 tuần sau",
                english: "3 weeks later");
            table.Add(nameof(LiveOpsHubStrings.CalendarRangeMenuPickStart),
                vietnamese: "Chọn ngày bắt đầu…",
                english: "Pick a start date…");

            table.Add(nameof(LiveOpsHubStrings.CalendarTodayButton),
                vietnamese: "Hôm nay (T)",
                english: "Today (T)");
            table.Add(nameof(LiveOpsHubStrings.CalendarTodayDisabledReason),
                vietnamese: "Khung đang chứa hôm nay",
                english: "The view already contains today");

            table.Add(nameof(LiveOpsHubStrings.CalendarZoomChoices),
                vietnamese: "Ngày|3 tuần|Tháng",
                english: "Day|3 weeks|Month");
            table.Add(nameof(LiveOpsHubStrings.CalendarSnapMenuFormat),
                vietnamese: "Bắt lưới: {0}",
                english: "Snap: {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarSnapAuto),
                vietnamese: "Tự động",
                english: "Automatic");
            table.Add(nameof(LiveOpsHubStrings.CalendarSnapFifteenMinutes),
                vietnamese: "15 phút",
                english: "15 minutes");
            table.Add(nameof(LiveOpsHubStrings.CalendarSnapHour),
                vietnamese: "1 giờ",
                english: "1 hour");
            table.Add(nameof(LiveOpsHubStrings.CalendarSnapDay),
                vietnamese: "1 ngày",
                english: "1 day");
            table.Add(nameof(LiveOpsHubStrings.CalendarSnapOff),
                vietnamese: "Tắt",
                english: "Off");
            table.Add(nameof(LiveOpsHubStrings.CalendarSearchPlaceholder),
                vietnamese: "Tìm id đợt",
                english: "Find event id");
            table.Add(nameof(LiveOpsHubStrings.CalendarHiddenLanesChipFormat),
                vietnamese: "Đang ẩn {0} làn · Hiện",
                english: "{0} {0|lane|lanes} hidden · Show");

            table.Add(nameof(LiveOpsHubStrings.CalendarAddEventButton),
                vietnamese: "Thêm đợt",
                english: "Add event");

            table.Add(nameof(LiveOpsHubStrings.CalendarMissingLayoutMessageFormat),
                vietnamese: "Không nạp được bố cục màn Lịch: {0}",
                english: "Cannot load the Calendar layout: {0}");

            table.Add(nameof(LiveOpsHubStrings.CalendarNoAssetEmptyText),
                vietnamese: "Chưa có lịch LiveOps trong project",
                english: "No LiveOps calendar in this project yet");
            table.Add(nameof(LiveOpsHubStrings.CalendarOpenOverviewButton),
                vietnamese: "Mở Tổng quan để tạo lịch",
                english: "Open Overview to create a calendar");

            table.Add(nameof(LiveOpsHubStrings.CalendarInspectorEmptyText),
                vietnamese: "Chọn một đợt trên trục để xem và sửa.",
                english: "Select an event on the axis to view and edit it.");
            table.Add(nameof(LiveOpsHubStrings.CalendarInspectorSummaryFormat),
                vietnamese: "{0} đợt cố định · {1} luật lặp · {2} không đặt được",
                english: "{0} fixed {0|event|events} · {1} recurring {1|rule|rules} · {2} cannot be placed");

            table.AddShared(nameof(LiveOpsHubStrings.CalendarFieldIdLabel), "Id");
            table.Add(nameof(LiveOpsHubStrings.CalendarFieldTypeLabel),
                vietnamese: "Loại",
                english: "Type");
            table.Add(nameof(LiveOpsHubStrings.CalendarFieldStartLabel),
                vietnamese: "Bắt đầu",
                english: "Start");
            table.Add(nameof(LiveOpsHubStrings.CalendarFieldEndLabel),
                vietnamese: "Kết thúc",
                english: "End");
            table.Add(nameof(LiveOpsHubStrings.CalendarFieldDurationLabel),
                vietnamese: "Dài",
                english: "Length");
            table.Add(nameof(LiveOpsHubStrings.CalendarDurationUnitLabel),
                vietnamese: "giờ",
                english: "hours");
            table.AddShared(nameof(LiveOpsHubStrings.CalendarDurationNoteFormat), "= {0}");
            table.AddShared(nameof(LiveOpsHubStrings.CalendarFieldConfigKeyLabel), "Config key");
            table.Add(nameof(LiveOpsHubStrings.CalendarFieldSourceLabel),
                vietnamese: "Nguồn",
                english: "Source");
            table.Add(nameof(LiveOpsHubStrings.CalendarFieldRequiresOptInLabel),
                vietnamese: "Phải bấm tham gia",
                english: "Requires opt-in");

            table.Add(nameof(LiveOpsHubStrings.CalendarConfigKeyPlaceholderFormat),
                vietnamese: "mặc định của loại: {0}",
                english: "type default: {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarConfigKeyResetButton),
                vietnamese: "Về mặc định",
                english: "Back to default");
            table.Add(nameof(LiveOpsHubStrings.CalendarConfigKeyResetTooltipFormat),
                vietnamese: "Xoá giá trị riêng, dùng mặc định của loại ({0})",
                english: "Clear the private value and use the type default ({0})");

            table.Add(nameof(LiveOpsHubStrings.CalendarRequiresOptInYesFormat),
                vietnamese: "Có · theo loại {0}",
                english: "Yes · from type {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarRequiresOptInNoFormat),
                vietnamese: "Không · theo loại {0}",
                english: "No · from type {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarEditTypeLink),
                vietnamese: "Sửa ở Loại event",
                english: "Edit in Event types");

            table.Add(nameof(LiveOpsHubStrings.CalendarSourceFixedFormat),
                vietnamese: "Cố định · {0}",
                english: "Fixed · {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarSourceRecurringFormat),
                vietnamese: "Sinh từ luật {0}",
                english: "Generated by rule {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarIssuesFoldoutFormat),
                vietnamese: "Vấn đề ({0})",
                english: "Issues ({0})");

            table.Add(nameof(LiveOpsHubStrings.CalendarStartLockedTooltipFormat),
                vietnamese: "Bắt đầu: khoá — đã chạy từ {0} UTC",
                english: "Start: locked — running since {0} UTC");

            table.Add(nameof(LiveOpsHubStrings.CalendarRecurringNoteFormat),
                vietnamese: "Đợt sinh từ luật {0}. Sửa chu kỳ, thời gian chạy hoặc tiền tố ở Luật lặp.",
                english: "This event is generated by rule {0}. Change the period, active time or id prefix in Recurring rules.");
            table.Add(nameof(LiveOpsHubStrings.CalendarOpenRuleButton),
                vietnamese: "Mở luật",
                english: "Open rule");

            table.Add(nameof(LiveOpsHubStrings.CalendarUnreadableFixButtonFormat),
                vietnamese: "Sửa thành {0}",
                english: "Fix to {0}");

            table.Add(nameof(LiveOpsHubStrings.CalendarPhaseUpcoming),
                vietnamese: "Sắp tới",
                english: "Upcoming");
            table.Add(nameof(LiveOpsHubStrings.CalendarPhaseRunning),
                vietnamese: "Đang chạy",
                english: "Running");
            table.Add(nameof(LiveOpsHubStrings.CalendarPhaseEnded),
                vietnamese: "Đã khép",
                english: "Ended");
            table.Add(nameof(LiveOpsHubStrings.CalendarPhaseUnplaceable),
                vietnamese: "Không đặt được",
                english: "Cannot place");

            table.Add(nameof(LiveOpsHubStrings.CalendarDeleteButton),
                vietnamese: "Xoá đợt",
                english: "Delete event");
            table.Add(nameof(LiveOpsHubStrings.CalendarDeleteButtonWithDialog),
                vietnamese: "Xoá đợt…",
                english: "Delete event…");
            table.Add(nameof(LiveOpsHubStrings.CalendarDeleteButtonImmediateTooltip),
                vietnamese: "Đợt chưa bắt đầu và chưa có trong bản đã đăng: xoá ngay, toast có Hoàn tác",
                english: "The event has not started and is not in the published version: it is deleted right away, with Undo in the toast");
            table.Add(nameof(LiveOpsHubStrings.CalendarDeleteButtonConfirmTooltip),
                vietnamese: "Mở hộp xác nhận trước khi xoá",
                english: "Opens a confirmation dialog before deleting");

            table.Add(nameof(LiveOpsHubStrings.CalendarDeleteToastFormat),
                vietnamese: "Đã xoá {0} ({1} → {2} UTC)",
                english: "Deleted {0} ({1} → {2} UTC)");
            table.Add(nameof(LiveOpsHubStrings.CalendarDeleteToastShortFormat),
                vietnamese: "Đã xoá {0} ({1} → {2})",
                english: "Deleted {0} ({1} → {2})");

            table.Add(nameof(LiveOpsHubStrings.CalendarDeleteConfirmTitleFormat),
                vietnamese: "Xoá {0}?",
                english: "Delete {0}?");
            table.Add(nameof(LiveOpsHubStrings.CalendarDeleteRunningConfirmTitleFormat),
                vietnamese: "Xoá đợt đang chạy {0}?",
                english: "Delete the running event {0}?");
            table.Add(nameof(LiveOpsHubStrings.CalendarDeletePublishedBodyFormat),
                vietnamese: "Đợt chưa bắt đầu ({0} → {1} UTC) nhưng đã có trong bản đăng {2}: lần đăng tới người chơi sẽ không thấy đợt này.",
                english: "The event has not started ({0} → {1} UTC) but it is in the {2} publish: after the next publish players will not see it.");
            table.Add(nameof(LiveOpsHubStrings.CalendarDeleteRunningBodyFormat),
                vietnamese: "Đợt đang chạy ({0} → {1} UTC): người chơi đang ở trong đợt này sẽ mất nó ngay lần đăng tới.",
                english: "The event is running ({0} → {1} UTC): players currently inside it lose it at the next publish.");

            table.Add(nameof(LiveOpsHubStrings.CalendarOverlapClearedFormat),
                vietnamese: "{0} hết chồng giờ và sẽ mở từ {1}.",
                english: "{0} no longer overlaps and will open from {1}.");
            table.Add(nameof(LiveOpsHubStrings.CalendarUndoHintFormat),
                vietnamese: "Hoàn tác được bằng {0} tới khi đóng Unity.",
                english: "You can undo with {0} until Unity is closed.");

            table.Add(nameof(LiveOpsHubStrings.CalendarEditRunningConfirmTitleFormat),
                vietnamese: "Sửa đợt đang chạy {0}?",
                english: "Edit the running event {0}?");
            table.Add(nameof(LiveOpsHubStrings.CalendarEditDestructiveLabel),
                vietnamese: "Sửa đợt",
                english: "Edit event");

            table.Add(nameof(LiveOpsHubStrings.CalendarDeletePublishedNoStampBodyFormat),
                vietnamese: "Đợt chưa bắt đầu ({0} → {1} UTC) nhưng đã có trong bản đăng: lần đăng tới người chơi sẽ không thấy đợt này.",
                english: "The event has not started ({0} → {1} UTC) but it is in the published version: after the next publish players will not see it.");

            table.Add(nameof(LiveOpsHubStrings.CalendarPickRangeStartApplyButton),
                vietnamese: "Đi tới ngày này",
                english: "Go to this date");

            table.Add(nameof(LiveOpsHubStrings.CalendarShortenConfirmTitleFormat),
                vietnamese: "Rút ngắn đợt đang chạy {0}?",
                english: "Shorten the running event {0}?");
            table.Add(nameof(LiveOpsHubStrings.CalendarShortenConfirmBodyFormat),
                vietnamese: "Kết thúc {0} → {1} UTC. Đợt chạy từ {2}; người chơi còn {3} thay vì {4}.",
                english: "End {0} → {1} UTC. The event has been running since {2}; players now have {3} left instead of {4}.");
            table.Add(nameof(LiveOpsHubStrings.CalendarShortenDestructiveLabel),
                vietnamese: "Rút ngắn đợt",
                english: "Shorten event");
            table.Add(nameof(LiveOpsHubStrings.CalendarKeepEndLabelFormat),
                vietnamese: "Giữ {0}",
                english: "Keep {0}");

            table.Add(nameof(LiveOpsHubStrings.CalendarUnknownPlayerCountSentence),
                vietnamese: "Hub không biết số người chơi toàn cục đang ở đợt này.",
                english: "The hub does not know how many players worldwide are in this event.");
            table.Add(nameof(LiveOpsHubStrings.CalendarNoEditorRecordFormat),
                vietnamese: "Editor này chưa có bản ghi của {0}, nên không có số để nêu.",
                english: "This Editor has no record of {0}, so there is no number to quote.");

            table.Add(nameof(LiveOpsHubStrings.CalendarMoveToastFormat),
                vietnamese: "Đã dời {0} {1} → {2} UTC",
                english: "Moved {0} {1} → {2} UTC");
            table.Add(nameof(LiveOpsHubStrings.CalendarMoveUndoStepFormat),
                vietnamese: "Dời {0}",
                english: "Move {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarResizeUndoStepFormat),
                vietnamese: "Đổi {0}",
                english: "Change {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarMoveEndToastFormat),
                vietnamese: "Đã dời kết thúc {0} {1} → {2} UTC",
                english: "Moved the end of {0} {1} → {2} UTC");
            table.Add(nameof(LiveOpsHubStrings.CalendarMoveStartToastFormat),
                vietnamese: "Đã dời bắt đầu {0} {1} → {2} UTC",
                english: "Moved the start of {0} {1} → {2} UTC");
            table.Add(nameof(LiveOpsHubStrings.CalendarRenameToastFormat),
                vietnamese: "Đã đổi id {0} → {1}",
                english: "Renamed id {0} → {1}");
            table.Add(nameof(LiveOpsHubStrings.CalendarRetypeToastFormat),
                vietnamese: "Đã đổi loại {0} sang {1}",
                english: "Changed the type of {0} to {1}");
            table.Add(nameof(LiveOpsHubStrings.CalendarConfigKeyToastFormat),
                vietnamese: "Đã đổi config key của {0}",
                english: "Changed the config key of {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarRenameUndoStepFormat),
                vietnamese: "Đổi id {0}",
                english: "Rename {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarRetypeUndoStepFormat),
                vietnamese: "Đổi loại {0}",
                english: "Change the type of {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarConfigKeyUndoStepFormat),
                vietnamese: "Đổi config key {0}",
                english: "Change the config key of {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddToastFormat),
                vietnamese: "Đã thêm {0} vào lịch",
                english: "Added {0} to the calendar");
            table.Add(nameof(LiveOpsHubStrings.CalendarRepairToastFormat),
                vietnamese: "Đã áp đề xuất cho {0}",
                english: "Applied the proposal for {0}");

            table.Add(nameof(LiveOpsHubStrings.CalendarAddStepTypeTitleFormat),
                vietnamese: "Thêm đợt · {0}",
                english: "Add event · {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddReviewTitle),
                vietnamese: "Thêm đợt · xem lại",
                english: "Add event · review");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddTypeFilterPlaceholder),
                vietnamese: "Gõ để lọc loại…",
                english: "Type to filter types…");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddTypeFixedTag),
                vietnamese: "cố định",
                english: "fixed");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddTypeOptInTag),
                vietnamese: "phải bấm tham gia",
                english: "requires opt-in");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddTypeRecurringTag),
                vietnamese: "(sinh từ luật — sửa ở Luật lặp)",
                english: "(generated by a rule — edit it in Recurring rules)");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddKeyHint),
                vietnamese: "Enter: tiếp · Esc: đóng",
                english: "Enter: next · Esc: close");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddCancelButton),
                vietnamese: "Huỷ",
                english: "Cancel");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddNextButton),
                vietnamese: "Tiếp",
                english: "Next");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddBackButton),
                vietnamese: "Quay lại",
                english: "Back");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddEndAutoNote),
                vietnamese: "UTC · tự tính",
                english: "UTC · computed");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddTimesLabel),
                vietnamese: "Giờ",
                english: "Times");
            table.AddShared(nameof(LiveOpsHubStrings.CalendarTimeRangeFormat), "{0} → {1} UTC");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddConfigKeyNoteFormat),
                vietnamese: "để trống = mặc định của loại, Xuất ghi `{0}`",
                english: "leave empty = the type default, Export writes `{0}`");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddSubmitFormat),
                vietnamese: "Thêm {0} vào lịch",
                english: "Add {0} to the calendar");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddBackToTimesButton),
                vietnamese: "Quay lại sửa giờ",
                english: "Back to the times");
            table.Add(nameof(LiveOpsHubStrings.CalendarAddAnywayButton),
                vietnamese: "Vẫn thêm (game sẽ bỏ đợt này)",
                english: "Add anyway (the game will drop this event)");

            table.Add(nameof(LiveOpsHubStrings.CalendarQuickCheckOkTag),
                vietnamese: "kiểm nhanh làn này: không chồng",
                english: "quick check on this lane: no overlap");
        }
    }
}
