namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Recurring (màn Luật lặp) — bản tiếng Việt là bản gốc chép nguyên văn từ thiết kế [SD1 §4.1–4.3],
    /// bản tiếng Anh dịch cùng lúc (G-I18N §4: không bao giờ thêm khoá ở một bản mà quên bản kia). Comment "vì sao"
    /// của từng câu ở <c>LiveOpsHubStrings.Recurring.cs</c> cạnh property.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterRecurring(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.RecurringAddRuleButton),
                vietnamese: "Thêm luật",
                english: "Add rule");
            table.Add(nameof(LiveOpsHubStrings.RecurringAddRuleNoTypeReason),
                vietnamese: "Mọi loại event đã có luật lặp",
                english: "Every event type already has a recurring rule");
            table.Add(nameof(LiveOpsHubStrings.RecurringAddRuleNoAssetReason),
                vietnamese: "Chưa có lịch để thêm luật",
                english: "No calendar to add a rule to");
            table.Add(nameof(LiveOpsHubStrings.RecurringDeleteRuleButton),
                vietnamese: "Xoá luật…",
                english: "Delete rule…");

            table.Add(nameof(LiveOpsHubStrings.RecurringEmptyTitle),
                vietnamese: "Chưa có luật lặp",
                english: "No recurring rules yet");
            table.Add(nameof(LiveOpsHubStrings.RecurringEmptyBody),
                vietnamese: "Đợt lặp sinh ra từ neo, chu kỳ và tiền tố; loại có luật thì không kéo được trên Lịch.",
                english: "Recurring occurrences come from the anchor, the cycle and the prefix; a type with a rule cannot be dragged on the Calendar.");
            table.Add(nameof(LiveOpsHubStrings.RecurringNoAssetTitle),
                vietnamese: "Chưa có lịch LiveOps trong project",
                english: "No LiveOps calendar in this project");
            table.Add(nameof(LiveOpsHubStrings.RecurringNoAssetBody),
                vietnamese: "Luật lặp nằm trong một LiveEventCalendarAsset — mở hoặc tạo lịch ở Tổng quan trước.",
                english: "Recurring rules live inside a LiveEventCalendarAsset — open or create one in Overview first.");
            table.Add(nameof(LiveOpsHubStrings.RecurringNoAssetActionButton),
                vietnamese: "Mở Tổng quan để tạo lịch",
                english: "Open Overview to create a calendar");
            table.Add(nameof(LiveOpsHubStrings.RecurringMissingLayoutFormat),
                vietnamese: "Thiếu bố cục màn Luật lặp: {0}",
                english: "The Recurring rules layout is missing: {0}");

            table.Add(nameof(LiveOpsHubStrings.RecurringListMetaFormat),
                vietnamese: "mỗi {0} · chạy {1}",
                english: "every {0} · runs {1}");

            table.Add(nameof(LiveOpsHubStrings.RecurringSentenceEveryPrefix),
                vietnamese: "Mỗi ",
                english: "Every ");
            table.Add(nameof(LiveOpsHubStrings.RecurringSentenceAnchorPrefix),
                vietnamese: ", neo từ ",
                english: ", anchored at ");
            table.Add(nameof(LiveOpsHubStrings.RecurringSentenceActivePrefix),
                vietnamese: ", mỗi đợt chạy ",
                english: ", each occurrence runs ");
            table.Add(nameof(LiveOpsHubStrings.RecurringSentenceIdPrefix),
                vietnamese: ", id = ",
                english: ", id = ");
            table.Add(nameof(LiveOpsHubStrings.RecurringSentenceIdSuffix),
                vietnamese: " + số thứ tự.",
                english: " + occurrence number.");

            table.Add(nameof(LiveOpsHubStrings.RecurringPresetLabel),
                vietnamese: "Mẫu",
                english: "Template");
            table.Add(nameof(LiveOpsHubStrings.RecurringPresetCustom),
                vietnamese: "Tùy chỉnh…",
                english: "Custom…");
            table.Add(nameof(LiveOpsHubStrings.RecurringEventTypeLabel),
                vietnamese: "Loại event",
                english: "Event type");
            table.Add(nameof(LiveOpsHubStrings.RecurringEventTypeLockedTooltip),
                vietnamese: "Đổi loại là một luật khác hẳn — tạo luật mới cho loại kia",
                english: "Changing the type makes a different rule — add a new rule for that type instead");
            table.Add(nameof(LiveOpsHubStrings.RecurringIdPrefixLabel),
                vietnamese: "Tiền tố id",
                english: "Id prefix");
            table.Add(nameof(LiveOpsHubStrings.RecurringIdPrefixDefaultFormat),
                vietnamese: "để trống = mặc định {0}",
                english: "empty = default {0}");
            table.Add(nameof(LiveOpsHubStrings.RecurringAnchorLabel),
                vietnamese: "Neo",
                english: "Anchor");
            table.Add(nameof(LiveOpsHubStrings.RecurringPeriodHoursLabel),
                vietnamese: "Chu kỳ (giờ)",
                english: "Cycle (hours)");
            table.Add(nameof(LiveOpsHubStrings.RecurringActiveHoursLabel),
                vietnamese: "Chạy mỗi đợt (giờ)",
                english: "Run per occurrence (hours)");
            table.AddShared(nameof(LiveOpsHubStrings.RecurringEqualsFormat), "= {0}");
            table.AddShared(nameof(LiveOpsHubStrings.RecurringAnchorDeviceLineFormat), "{0} · {1}");
            table.AddShared(nameof(LiveOpsHubStrings.RecurringAnchorTokenFormat), "{0} {1} {2} {3}");

            table.Add(nameof(LiveOpsHubStrings.RecurringCycleSeamlessFormat),
                vietnamese: "chạy {0} · liền mạch, không nghỉ",
                english: "runs {0} · seamless, no break");
            table.Add(nameof(LiveOpsHubStrings.RecurringCycleWithRestFormat),
                vietnamese: "chạy {0} | nghỉ {1}",
                english: "runs {0} | break {1}");
            table.Add(nameof(LiveOpsHubStrings.RecurringCycleOverflowFormat),
                vietnamese: "chạy {0} · dài hơn chu kỳ {1}",
                english: "runs {0} · longer than the {1} cycle");

            table.Add(nameof(LiveOpsHubStrings.RecurringActiveLongerThanPeriodError),
                vietnamese: "Chạy lâu hơn chu kỳ: đợt sau mở trước khi đợt trước khép",
                english: "Runs longer than the cycle: the next occurrence opens before the previous one closes");
            table.Add(nameof(LiveOpsHubStrings.RecurringPeriodMustBePositiveError),
                vietnamese: "Chu kỳ phải lớn hơn 0 giờ",
                english: "The cycle must be longer than 0 hours");
            table.Add(nameof(LiveOpsHubStrings.RecurringActiveMustBePositiveError),
                vietnamese: "Thời gian chạy phải lớn hơn 0 giờ",
                english: "The run length must be longer than 0 hours");
            table.Add(nameof(LiveOpsHubStrings.RecurringAnchorUnreadableError),
                vietnamese: "Neo không đọc được — cần ngày yyyy-MM-dd và giờ HH:mm",
                english: "The anchor cannot be read — it needs a yyyy-MM-dd date and an HH:mm time");
            table.Add(nameof(LiveOpsHubStrings.RecurringAnchorNotice),
                vietnamese: "Neo không ở 00:00 UTC — đợt sẽ mở giữa ngày",
                english: "The anchor is not at 00:00 UTC — occurrences will open mid-day");

            table.Add(nameof(LiveOpsHubStrings.RecurringOccurrencesTitleFormat),
                vietnamese: "{0} đợt kế tiếp",
                english: "Next {0} occurrences");
            table.Add(nameof(LiveOpsHubStrings.RecurringOccurrencesSubtitleFormat),
                vietnamese: "theo nháp · {0}",
                english: "from the draft · {0}");
            table.AddShared(nameof(LiveOpsHubStrings.RecurringColumnId), "Id");
            table.Add(nameof(LiveOpsHubStrings.RecurringColumnStartUtc),
                vietnamese: "Bắt đầu UTC",
                english: "Start UTC");
            table.Add(nameof(LiveOpsHubStrings.RecurringColumnEndUtc),
                vietnamese: "Kết thúc UTC",
                english: "End UTC");
            table.Add(nameof(LiveOpsHubStrings.RecurringColumnDeviceTime),
                vietnamese: "Giờ máy",
                english: "Device time");
            table.Add(nameof(LiveOpsHubStrings.RecurringColumnNow),
                vietnamese: "Lúc này",
                english: "Right now");
            table.Add(nameof(LiveOpsHubStrings.RecurringPhaseRunning),
                vietnamese: "Đang chạy",
                english: "Running");
            table.Add(nameof(LiveOpsHubStrings.RecurringPhaseUpcoming),
                vietnamese: "Sắp tới",
                english: "Upcoming");
            table.Add(nameof(LiveOpsHubStrings.RecurringPhaseEnded),
                vietnamese: "Đã khép",
                english: "Ended");
            table.AddShared(nameof(LiveOpsHubStrings.RecurringPhaseWithRelativeFormat), "{0} · {1}");
            table.Add(nameof(LiveOpsHubStrings.RecurringIdChangeTag),
                vietnamese: "đổi id",
                english: "id changes");
            table.AddShared(nameof(LiveOpsHubStrings.RecurringIdArrow), "→");
            table.Add(nameof(LiveOpsHubStrings.RecurringAddMoreButtonFormat),
                vietnamese: "Thêm {0}",
                english: "Add {0}");
            table.Add(nameof(LiveOpsHubStrings.RecurringComputingNote),
                vietnamese: "đang tính…",
                english: "computing…");
            table.Add(nameof(LiveOpsHubStrings.RecurringOccurrenceLimitNoteFormat),
                vietnamese: "Đã hiện tối đa {0} đợt",
                english: "Showing the maximum of {0} occurrences");
            table.Add(nameof(LiveOpsHubStrings.RecurringOccurrencesEmptyReason),
                vietnamese: "Không đợt nào sinh ra: luật đang lỗi ở trên nên game bỏ hẳn luật này — sửa ô lỗi rồi bảng mới có đợt.",
                english: "No occurrence is generated: the rule above is invalid so the game drops it entirely — fix the field in error and the table fills in.");

            table.Add(nameof(LiveOpsHubStrings.RecurringDraftNoticeFormat),
                vietnamese: "Nháp tại ô này, chưa ghi vào {0}: Lịch, Kiểm lịch và Xuất JSON vẫn thấy {1}",
                english: "Draft in this field only, not written to {0}: Calendar, Check calendar and Export JSON still see {1}");
            table.Add(nameof(LiveOpsHubStrings.RecurringPrefixConsequenceFormat),
                vietnamese: "{0} đang chạy tới {1} sẽ thành {2}; người chơi có điểm bắt đầu lại từ 0.",
                english: "{0}, running until {1}, becomes {2}; players with progress start again from 0.");
            table.Add(nameof(LiveOpsHubStrings.RecurringIdentityConsequenceFormat),
                vietnamese: "{0} đang chạy tới {1} sẽ mang id {2}; người chơi có điểm bắt đầu lại từ 0.",
                english: "{0}, running until {1}, takes the id {2}; players with progress start again from 0.");
            table.Add(nameof(LiveOpsHubStrings.RecurringNoReplacementConsequenceFormat),
                vietnamese: "{0} đang chạy tới {1} sẽ biến mất khỏi lịch: luật mới không có đợt nào chạy lúc này; người chơi có điểm bắt đầu lại từ 0.",
                english: "{0}, running until {1}, disappears from the calendar: the new rule has no occurrence running right now; players with progress start again from 0.");
            table.Add(nameof(LiveOpsHubStrings.RecurringActiveHoursConsequenceFormat),
                vietnamese: "{0} đang chạy sẽ khép lúc {1} thay vì {2}; người chơi còn dở mất phần thời gian còn lại.",
                english: "{0} is running and will close at {1} instead of {2}; players in the middle lose the remaining time.");
            table.Add(nameof(LiveOpsHubStrings.RecurringDraftCancelButton),
                vietnamese: "Huỷ (Esc)",
                english: "Cancel (Esc)");
            table.Add(nameof(LiveOpsHubStrings.RecurringDraftWritePrefixButton),
                vietnamese: "Ghi tiền tố mới…",
                english: "Write the new prefix…");
            table.Add(nameof(LiveOpsHubStrings.RecurringDraftWriteValueButton),
                vietnamese: "Ghi giá trị mới…",
                english: "Write the new value…");
            table.Add(nameof(LiveOpsHubStrings.RecurringErrorNoConfirmationNeeded),
                vietnamese: "Nháp này không cần hỏi xác nhận — không dựng hộp cho nó",
                english: "This draft needs no confirmation — do not build a dialog for it");

            table.Add(nameof(LiveOpsHubStrings.RecurringConfirmPrefixTitle),
                vietnamese: "Đổi tiền tố id của đợt đang chạy",
                english: "Change the id prefix of the running occurrence");
            table.Add(nameof(LiveOpsHubStrings.RecurringConfirmPrefixBodyFormat),
                vietnamese: "{0} → {1}. Đợt đang chạy tới {2}. Người chơi đã có điểm ở {0} sẽ bắt đầu lại từ 0 ở {1}.",
                english: "{0} → {1}. The occurrence runs until {2}. Players who already have progress in {0} start again from 0 in {1}.");
            table.Add(nameof(LiveOpsHubStrings.RecurringConfirmNoReplacementBodyFormat),
                vietnamese: "{0} đang chạy tới {1} sẽ biến mất khỏi lịch: luật mới không có đợt nào chạy lúc này. Người chơi đã có điểm ở {0} bắt đầu lại từ 0.",
                english: "{0}, running until {1}, disappears from the calendar: the new rule has no occurrence running right now. Players who already have progress in {0} start again from 0.");
            table.Add(nameof(LiveOpsHubStrings.RecurringConfirmPrefixDestructive),
                vietnamese: "Đổi tiền tố",
                english: "Change the prefix");
            table.Add(nameof(LiveOpsHubStrings.RecurringConfirmPrefixSafe),
                vietnamese: "Giữ tiền tố cũ",
                english: "Keep the old prefix");
            table.Add(nameof(LiveOpsHubStrings.RecurringConfirmPrefixKeyHint),
                vietnamese: "Esc: Giữ tiền tố cũ · Enter không đổi gì",
                english: "Esc: keep the old prefix · Enter changes nothing");
            table.Add(nameof(LiveOpsHubStrings.RecurringConfirmIdentityTitle),
                vietnamese: "Đổi id của đợt đang chạy",
                english: "Change the id of the running occurrence");
            table.Add(nameof(LiveOpsHubStrings.RecurringConfirmIdentityDestructive),
                vietnamese: "Đổi luật",
                english: "Change the rule");
            table.Add(nameof(LiveOpsHubStrings.RecurringConfirmActiveTitle),
                vietnamese: "Rút ngắn đợt đang chạy",
                english: "Shorten the running occurrence");
            table.Add(nameof(LiveOpsHubStrings.RecurringConfirmActiveBodyFormat),
                vietnamese: "{0} đang chạy sẽ khép lúc {1} thay vì {2}. Người chơi còn dở mất phần thời gian còn lại.",
                english: "{0} is running and will close at {1} instead of {2}. Players in the middle lose the remaining time.");
            table.Add(nameof(LiveOpsHubStrings.RecurringConfirmActiveDestructive),
                vietnamese: "Đổi giờ chạy",
                english: "Change the run length");
            table.Add(nameof(LiveOpsHubStrings.RecurringConfirmActiveKeyHint),
                vietnamese: "Esc: Giữ giờ chạy cũ",
                english: "Esc: keep the old run length");

            table.Add(nameof(LiveOpsHubStrings.RecurringDeleteConfirmTitleFormat),
                vietnamese: "Xoá luật lặp {0}",
                english: "Delete recurring rule {0}");
            table.Add(nameof(LiveOpsHubStrings.RecurringDeleteConfirmBodyFormat),
                vietnamese: "{0} đang chạy tới {1}. Xoá luật là đợt biến mất khỏi lịch game; người chơi đang có điểm mất tiến độ.",
                english: "{0} runs until {1}. Deleting the rule removes the occurrence from the game calendar; players with progress lose it.");
            table.Add(nameof(LiveOpsHubStrings.RecurringDeleteConfirmDestructive),
                vietnamese: "Xoá luật",
                english: "Delete the rule");
            table.Add(nameof(LiveOpsHubStrings.RecurringDeleteToastFormat),
                vietnamese: "Xoá luật lặp {0}",
                english: "Delete recurring rule {0}");

            table.Add(nameof(LiveOpsHubStrings.RecurringAfterWriteNoticeFormat),
                vietnamese: "{0} đang chạy (tới {1}) sẽ thành {2}. Người chơi đã có điểm ở {0} bắt đầu lại từ 0.",
                english: "{0}, running until {1}, becomes {2}. Players who already have progress in {0} start again from 0.");
            table.Add(nameof(LiveOpsHubStrings.RecurringRevertPrefixButtonFormat),
                vietnamese: "Hoàn về tiền tố {0}",
                english: "Revert to prefix {0}");
            table.Add(nameof(LiveOpsHubStrings.RecurringDecideInValidationLink),
                vietnamese: "Quyết định ở Kiểm lịch",
                english: "Decide in Check calendar");

            table.Add(nameof(LiveOpsHubStrings.RecurringWriteToastFormat),
                vietnamese: "Đổi luật lặp {0}",
                english: "Change recurring rule {0}");
            table.Add(nameof(LiveOpsHubStrings.RecurringAddToastFormat),
                vietnamese: "Thêm luật lặp {0}",
                english: "Add recurring rule {0}");
            table.Add(nameof(LiveOpsHubStrings.RecurringPrefixToastFormat),
                vietnamese: "Đổi tiền tố id {0} → {1}",
                english: "Change id prefix {0} → {1}");

            table.Add(nameof(LiveOpsHubStrings.RecurringAddTypeLabel),
                vietnamese: "Loại chưa có luật",
                english: "Type without a rule");
            table.Add(nameof(LiveOpsHubStrings.RecurringAddPresetLabel),
                vietnamese: "Mẫu",
                english: "Template");
            table.Add(nameof(LiveOpsHubStrings.RecurringAddConfirmButton),
                vietnamese: "Thêm luật",
                english: "Add rule");
            table.Add(nameof(LiveOpsHubStrings.RecurringAddCancelButton),
                vietnamese: "Huỷ (Esc)",
                english: "Cancel (Esc)");

            table.Add(nameof(LiveOpsHubStrings.RecurringJsonFoldoutLabel),
                vietnamese: "JSON của luật này",
                english: "JSON of this rule");
            table.AddShared(nameof(LiveOpsHubStrings.RecurringJsonCopyButton), "Copy");
            table.Add(nameof(LiveOpsHubStrings.RecurringJsonCopiedToastFormat),
                vietnamese: "Đã copy JSON của luật {0}",
                english: "Copied the JSON of rule {0}");
            table.Add(nameof(LiveOpsHubStrings.RecurringJsonReadOnlyNote),
                vietnamese: "Bản này chỉ đọc — sửa luật ở các ô phía trên",
                english: "This view is read-only — edit the rule in the fields above");

            table.Add(nameof(LiveOpsHubStrings.RecurringPresetWeeklyMonday),
                vietnamese: "Hằng tuần thứ Hai 00:00 UTC",
                english: "Weekly, Monday 00:00 UTC");
            table.Add(nameof(LiveOpsHubStrings.RecurringPresetDailyRunTwenty),
                vietnamese: "Hằng ngày 00:00 UTC · chạy 20 giờ",
                english: "Daily 00:00 UTC · runs 20 hours");
            table.Add(nameof(LiveOpsHubStrings.RecurringPresetBiweeklyMonday),
                vietnamese: "Hai tuần một lần thứ Hai",
                english: "Every two weeks, Monday");
        }
    }
}
