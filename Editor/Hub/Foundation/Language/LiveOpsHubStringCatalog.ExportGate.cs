namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng ExportGate — bản tiếng Việt là bản gốc (chép nguyên văn từ <c>LiveOpsHubStrings.ExportGate.cs</c>, không đổi một ký tự),
    /// bản tiếng Anh do gói dịch G-I18N bước 2 điền vào chỗ <c>english: null</c>. Hai ngôn ngữ nằm trong cùng một lệnh đăng ký để
    /// chỗ giữ chỗ <c>{0}</c>/<c>{1}</c> soát được bằng mắt và không bao giờ thêm khoá ở bản này mà quên bản kia.
    /// Comment "vì sao" của từng câu ở lại <c>LiveOpsHubStrings.ExportGate.cs</c> cạnh property — đó là chỗ người đọc code tìm tới.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterExportGate(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.ExportGateCardMetaBlocked),
                vietnamese: "bấm một dòng để tới màn liên quan",
                english: "click a row to open the related screen");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCardMetaReady),
                vietnamese: "đủ điều kiện để copy",
                english: "every condition met for copy");

            table.Add(nameof(LiveOpsHubStrings.ExportGateDroppedOk),
                vietnamese: "Kiểm lịch: không có đợt bị bỏ",
                english: "Validation: no events dropped");
            table.Add(nameof(LiveOpsHubStrings.ExportGateDroppedBlockedFormat),
                vietnamese: "Kiểm lịch: {0} đợt bị bỏ",
                english: "Validation: {0} events dropped");
            table.Add(nameof(LiveOpsHubStrings.ExportGateDroppedNeverChecked),
                vietnamese: "Kiểm lịch chưa chạy — chưa biết đợt nào bị bỏ",
                english: "Validation has not run — which events get dropped is unknown");
            table.Add(nameof(LiveOpsHubStrings.ExportGateDroppedRunning),
                vietnamese: "Đang kiểm lại — chờ kết quả đợt bị bỏ",
                english: "Validating again — waiting for the dropped-event result");
            table.Add(nameof(LiveOpsHubStrings.ExportGateDroppedStale),
                vietnamese: "Kiểm lịch cũ — kiểm lại để biết còn đợt bị bỏ không",
                english: "Validation is stale — validate again to see whether events are still dropped");
            table.Add(nameof(LiveOpsHubStrings.ExportGateDroppedStaleMetaFormat),
                vietnamese: "lần kiểm trước: {0} đợt bị bỏ",
                english: "previous validation: {0} events dropped");

            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackOkFormat),
                vietnamese: "Parser của game xác nhận: giữ {0}/{1} mục, khớp Kiểm lịch",
                english: "The game parser confirms: {0}/{1} entries kept, matches Validation");

            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackOkCompilerFormat),
                vietnamese: "Parser của game xác nhận: giữ {0}/{1} mục, khớp bộ biên dịch của hub",
                english: "The game parser confirms: {0}/{1} entries kept, matches the hub compiler");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchKeptCompilerFormat),
                vietnamese: "Parser giữ {0}/{1} nhưng bộ biên dịch của hub bỏ {2} — lỗi của hub",
                english: "The parser keeps {0}/{1} but the hub compiler drops {2} — hub bug");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchSameCountCompilerFormat),
                vietnamese: "Parser giữ {0}/{1}, bộ biên dịch của hub cũng bỏ {2} nhưng khác mục — lỗi của hub",
                english: "The parser keeps {0}/{1}, the hub compiler also drops {2} but not the same entries — hub bug");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchEntryCountCompilerFormat),
                vietnamese: "Parser đọc {0} mục nhưng bộ biên dịch của hub có {1} mục — lỗi của hub",
                english: "The parser reads {0} entries but the hub compiler has {1} entries — hub bug");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackOkNarrowFormat),
                vietnamese: "Parser của game xác nhận: giữ {0}/{1} mục",
                english: "The game parser confirms: {0}/{1} entries kept");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchKeptFormat),
                vietnamese: "Parser giữ {0}/{1} nhưng Kiểm lịch báo {2} bị bỏ — lỗi của hub",
                english: "The parser keeps {0}/{1} but Validation reports {2} dropped — hub bug");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchSameCountFormat),
                vietnamese: "Parser giữ {0}/{1}, Kiểm lịch cũng báo {2} bị bỏ nhưng khác mục — lỗi của hub",
                english: "The parser keeps {0}/{1}, Validation also reports {2} dropped but not the same entries — hub bug");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchEntryCountFormat),
                vietnamese: "Parser đọc {0} mục nhưng Kiểm lịch có {1} mục — lỗi của hub",
                english: "The parser reads {0} entries but Validation has {1} entries — hub bug");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchNarrow),
                vietnamese: "Parser lệch Kiểm lịch — lỗi của hub",
                english: "The parser disagrees with Validation — hub bug");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchMetaFormat),
                vietnamese: "mục thứ {0}: parser {1}, Kiểm lịch {2}",
                english: "entry {0}: parser {1}, Validation {2}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackFailed),
                vietnamese: "Parser của game không đọc lại được JSON này — lỗi của hub",
                english: "The game parser cannot read this JSON back — hub bug");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackFailedNarrow),
                vietnamese: "Parser không đọc lại được JSON",
                english: "The parser cannot read the JSON back");
            table.Add(nameof(LiveOpsHubStrings.ExportGateEntryKeptFormat),
                vietnamese: "giữ {0}",
                english: "kept {0}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateEntryDroppedFormat),
                vietnamese: "bỏ {0} ({1})",
                english: "dropped {0} ({1})");
            table.Add(nameof(LiveOpsHubStrings.ExportGateEntryAbsent),
                vietnamese: "không có mục",
                english: "no entry");
            table.AddShared(nameof(LiveOpsHubStrings.ExportGateEntryNameFormat), "{0} · {1}");
            table.AddShared(nameof(LiveOpsHubStrings.ExportGateReasonCountFormat), "{0} {1}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyErrorAction),
                vietnamese: "Copy lỗi",
                english: "Copy error");

            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonUnreadable),
                vietnamese: "không đọc được",
                english: "unreadable");
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonInvalidIdentifier),
                vietnamese: "id hoặc loại sai định dạng",
                english: "malformed id or type");
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonEndNotAfterStart),
                vietnamese: "kết thúc không sau bắt đầu",
                english: "end not after start");
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonDuplicateEventId),
                vietnamese: "bị bỏ vì trùng id",
                english: "dropped for duplicate id");
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonOverlapsSameType),
                vietnamese: "bị bỏ vì chồng giờ",
                english: "dropped for overlap");
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonShadowedByRecurring),
                vietnamese: "bị luật lặp che",
                english: "shadowed by a recurring rule");
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonInvalidRecurringRule),
                vietnamese: "luật lặp hỏng",
                english: "broken recurring rule");
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonDuplicateRecurringType),
                vietnamese: "bị bỏ vì trùng loại luật lặp",
                english: "dropped for duplicate recurring type");

            table.Add(nameof(LiveOpsHubStrings.ExportGateMismatchReportHeaderFormat),
                vietnamese: "LiveOps Hub: parser của game đọc lại JSON (sha {0}) lệch Kiểm lịch",
                english: "LiveOps Hub: the game parser read the JSON back (sha {0}) and disagrees with Validation");
            table.Add(nameof(LiveOpsHubStrings.ExportGateMismatchReportCountsFormat),
                vietnamese: "Parser giữ {0}/{1} mục · Kiểm lịch giữ {2}/{3} mục",
                english: "The parser keeps {0}/{1} entries · Validation keeps {2}/{3} entries");

            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessOkFormat),
                vietnamese: "Kiểm lịch chạy sau lần sửa cuối ({0} UTC)",
                english: "Validation ran after the last edit ({0} UTC)");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessStaleChangedFormat),
                vietnamese: "Kiểm lịch cũ: lịch đổi lúc {0}, sau lần kiểm {1}",
                english: "Validation is stale: the calendar changed at {0}, after the validation at {1}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessStaleMilestoneFormat),
                vietnamese: "Kiểm lịch cũ: đã qua mốc {0} sau lần kiểm {1}",
                english: "Validation is stale: milestone {0} passed after the validation at {1}");

            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessStaleFormat),
                vietnamese: "Kiểm lịch cũ: kết quả lúc {0} có thể không còn đúng",
                english: "Validation is stale: the result from {0} may no longer hold");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessStaleNarrow),
                vietnamese: "Kiểm lịch cũ",
                english: "Validation is stale");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessNeverChecked),
                vietnamese: "Kiểm lịch chưa chạy lần nào",
                english: "Validation has never run");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessRunningFormat),
                vietnamese: "Đang kiểm lại {0}/{1} luật…",
                english: "Validating {0}/{1} rules…");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessRunningMeta),
                vietnamese: "tự chạy khi mở màn",
                english: "runs automatically when the screen opens");
            table.Add(nameof(LiveOpsHubStrings.ExportGateStartCheckAction),
                vietnamese: "Kiểm lại (F5)",
                english: "Validate again (F5)");

            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteNotPasted),
                vietnamese: "Bản remote: chưa dán — không biết Firebase đang giữ gì",
                english: "Remote copy: not pasted — what Firebase holds is unknown");
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteNotPastedNarrow),
                vietnamese: "Bản remote: chưa dán",
                english: "Remote copy: not pasted");
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteNotVerifiedAfterMark),
                vietnamese: "Chưa đối chiếu với Firebase",
                english: "Not compared with Firebase yet");
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteMatchesFormat),
                vietnamese: "Bản remote khớp dấu {0}",
                english: "The remote copy matches stamp {0}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteDiffersFormat),
                vietnamese: "Bản remote khác dấu {0}",
                english: "The remote copy differs from stamp {0}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteVerifiedSuffixFormat),
                vietnamese: " · đã đối chiếu {0}",
                english: " · compared {0}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteWithoutStamp),
                vietnamese: "Bản remote đã dán nhưng chưa có dấu đã đăng để đối chiếu",
                english: "The remote copy is pasted but there is no published stamp to compare with");
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemotePasteAction),
                vietnamese: "Dán JSON đang chạy…",
                english: "Paste the running JSON…");
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteCompareAction),
                vietnamese: "Xem khác biệt",
                english: "View differences");
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteStillUnchecked),
                vietnamese: "Bản remote vẫn chưa kiểm",
                english: "The remote copy is still unchecked");

            table.Add(nameof(LiveOpsHubStrings.ExportGateReviewedOkFormat),
                vietnamese: "Đã xem {0}/{1} thay đổi bắt buộc",
                english: "Reviewed {0}/{1} required changes");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReviewedNothingRequired),
                vietnamese: "Không có thay đổi bắt buộc cần xem",
                english: "No required changes to review");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReviewedBlockedFormat),
                vietnamese: "Chưa xem {0} thay đổi làm người chơi mất tiến độ",
                english: "{0} changes causing player progress loss not reviewed");
            table.AddShared(nameof(LiveOpsHubStrings.ExportGateReviewedItemsSuffixFormat), " ({0})");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReviewedDiffMissing),
                vietnamese: "Chưa so được với bản đã đăng",
                english: "Cannot compare with the published baseline yet");
            table.AddShared(nameof(LiveOpsHubStrings.ExportGateItemFieldFormat), "{0} {1}");
            table.AddShared(nameof(LiveOpsHubStrings.ExportGateListSeparator), ", ");

            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldIdPrefix),
                vietnamese: "tiền tố",
                english: "prefix");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldAnchor),
                vietnamese: "neo",
                english: "anchor");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldPeriod),
                vietnamese: "chu kỳ",
                english: "period");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldActiveDuration),
                vietnamese: "thời gian chạy",
                english: "active duration");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldStart),
                vietnamese: "giờ bắt đầu",
                english: "start time");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldEnd),
                vietnamese: "giờ kết thúc",
                english: "end time");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldType),
                vietnamese: "loại",
                english: "type");

            table.Add(nameof(LiveOpsHubStrings.ExportGateCompactFormat),
                vietnamese: "{0} điều kiện đạt · sha {1} · {2}",
                english: "{0} conditions met · sha {1} · {2}");

            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonBlockedPrefix),
                vietnamese: "Chặn: ",
                english: "Blocked: ");
            table.AddShared(nameof(LiveOpsHubStrings.ExportGateReasonPartSeparator), " · ");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonReadBackFailed),
                vietnamese: "parser không đọc lại được JSON",
                english: "the parser cannot read the JSON back");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonReadBackMismatch),
                vietnamese: "parser lệch Kiểm lịch",
                english: "the parser disagrees with Validation");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonNeverChecked),
                vietnamese: "chưa kiểm lịch",
                english: "validation has not run");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonStale),
                vietnamese: "Kiểm lịch cũ",
                english: "Validation is stale");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonRunning),
                vietnamese: "đang kiểm lại lịch",
                english: "validating the calendar again");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonDroppedFormat),
                vietnamese: "{0} đợt bị bỏ",
                english: "{0} events dropped");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonUnreviewedFormat),
                vietnamese: "{0} thay đổi bắt buộc chưa xem",
                english: "{0} required changes not reviewed");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonDiffMissing),
                vietnamese: "chưa so được với bản đã đăng",
                english: "cannot compare with the published baseline");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonMissingForMark),
                vietnamese: "Còn thiếu cho Đánh dấu: copy hoặc lưu file bản này",
                english: "Still missing for Mark published: copy or save this version to a file");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonNoChanges),
                vietnamese: "Không có thay đổi để ghi",
                english: "No changes to record");

            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipBlockedPrefix),
                vietnamese: "Copy JSON: ",
                english: "Copy JSON: ");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipLastSeparator),
                vietnamese: ", và ",
                english: ", and ");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipDroppedFormat),
                vietnamese: "còn {0} đợt game sẽ bỏ",
                english: "{0} events will still be dropped by the game");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipUnreviewedFormat),
                vietnamese: "{0} thay đổi đụng đợt đang chạy chưa xem",
                english: "{0} changes touching running events are not reviewed");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipReadBackFailed),
                vietnamese: "parser của game không đọc lại được JSON do hub tạo",
                english: "the game parser cannot read back the JSON the hub produced");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipReadBackMismatch),
                vietnamese: "parser của game lệch Kiểm lịch",
                english: "the game parser disagrees with Validation");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipNeverChecked),
                vietnamese: "Kiểm lịch chưa chạy — bấm F5",
                english: "Validation has not run — press F5");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipStale),
                vietnamese: "Kiểm lịch cũ — kiểm lại (F5) trước",
                english: "Validation is stale — validate again (F5) first");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipRunning),
                vietnamese: "đang kiểm lại lịch",
                english: "validating the calendar again");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipDiffMissing),
                vietnamese: "chưa so được với bản đã đăng",
                english: "cannot compare with the published baseline");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipReadyFormat),
                vietnamese: "Copy {0} vào clipboard · sha {1}",
                english: "Copy {0} to the clipboard · sha {1}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateSaveTooltipBlocked),
                vietnamese: "Lưu file: cùng điều kiện với Copy JSON",
                english: "Save to file: same conditions as Copy JSON");
            table.Add(nameof(LiveOpsHubStrings.ExportGateSaveTooltipReadyFormat),
                vietnamese: "Lưu file: {0} · sha {1}",
                english: "Save to file: {0} · sha {1}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateMarkTooltipNotExported),
                vietnamese: "Chưa copy hay lưu file JSON này",
                english: "This JSON has not been copied or saved to a file");
            table.Add(nameof(LiveOpsHubStrings.ExportGateMarkTooltipDraftChangedFormat),
                vietnamese: "Nháp đã đổi sau lần copy {0} — copy bản mới rồi dán lại",
                english: "The draft changed after the {0} copy — copy the new version and paste it again");
            table.Add(nameof(LiveOpsHubStrings.ExportGateMarkTooltipReadBackUnconfirmed),
                vietnamese: "Parser của game chưa xác nhận JSON này",
                english: "The game parser has not confirmed this JSON");
            table.Add(nameof(LiveOpsHubStrings.ExportGateMarkTooltipReadyFormat),
                vietnamese: "Ghi dấu đã đăng cho sha {0}",
                english: "Mark published for sha {0}");

            table.Add(nameof(LiveOpsHubStrings.ExportGateFailureSymptom),
                vietnamese: "JSON do hub tạo không đọc lại được — lỗi của hub, không phải của lịch",
                english: "The JSON the hub produced cannot be read back — a hub bug, not a calendar problem");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFailurePositionFormat),
                vietnamese: "ở dòng {0}, cột {1}",
                english: "at line {0}, column {1}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFailureOpenLineFormat),
                vietnamese: "Mở JSON ở dòng {0}",
                english: "Open the JSON at line {0}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFailureNote),
                vietnamese: "Báo dev kèm lỗi; không dán JSON này lên Firebase. Mọi nút xuất disabled cho tới khi parser đọc được.",
                english: "Send the error to a developer; do not paste this JSON into Firebase. Every export button stays disabled until the parser can read it.");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFailureReportHeaderFormat),
                vietnamese: "LiveOps Hub: parser của game không đọc lại được JSON do hub tạo (sha {0})",
                english: "LiveOps Hub: the game parser cannot read back the JSON the hub produced (sha {0})");

            table.Add(nameof(LiveOpsHubStrings.ExportGateNoChangesFormat),
                vietnamese: "Không có gì mới so với {0} · sha {1}",
                english: "Nothing new compared with {0} · sha {1}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateSameCalendarDifferentJsonFormat),
                vietnamese: "Lịch không đổi so với {0}, nhưng JSON sắp xuất khác bản đã đăng: sha {1} → {2}",
                english: "The calendar is unchanged since {0}, but the JSON about to be exported differs from the published baseline: sha {1} → {2}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFirstPublish),
                vietnamese: "Chưa có dấu đã đăng nào — lần này sẽ là bản so đầu tiên",
                english: "No published stamp yet — this one becomes the first baseline");
            table.Add(nameof(LiveOpsHubStrings.ExportGateRestoringFormat),
                vietnamese: "Nháp đang là bản khôi phục từ {0}",
                english: "The draft is currently a restore from {0}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateUndoRestoreAction),
                vietnamese: "Hoàn tác khôi phục",
                english: "Undo restore");
            table.Add(nameof(LiveOpsHubStrings.ExportGateFormat1NoticeFormat),
                vietnamese: "Định dạng 1 không có recurring: {0} luật lặp sẽ không được xuất, game chỉ thấy {1} đợt cố định",
                english: "Format 1 has no recurring: {0} recurring rules will not be exported, the game only sees {1} fixed events");

            table.Add(nameof(LiveOpsHubStrings.ExportGateHealthBlockedBadge),
                vietnamese: "chặn",
                english: "blocked");
            table.Add(nameof(LiveOpsHubStrings.ExportGateHealthNeedsReviewBadgeFormat),
                vietnamese: "{0} cần xem",
                english: "{0} to review");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyBlockedColumn),
                vietnamese: "chặn Copy JSON",
                english: "blocks Copy JSON");

            table.Add(nameof(LiveOpsHubStrings.ExportGateErrorJsonMissing),
                vietnamese: "Cổng xuất cần JSON đã ghi.",
                english: "The export gate needs the written JSON.");
            table.Add(nameof(LiveOpsHubStrings.ExportGateErrorCompilationMissing),
                vietnamese: "Cổng xuất cần kết quả biên dịch nháp theo thứ tự xuất.",
                english: "The export gate needs the draft compilation result in export order.");
            table.Add(nameof(LiveOpsHubStrings.ExportGateErrorFormatMissing),
                vietnamese: "Cổng xuất cần bộ định dạng chữ.",
                english: "The export gate needs the text formatter.");
            table.Add(nameof(LiveOpsHubStrings.ExportGateErrorReadBackMissing),
                vietnamese: "Cổng xuất cần kết quả parser đọc lại khi JSON đọc được.",
                english: "The export gate needs the parser read-back result when the JSON is readable.");
            table.Add(nameof(LiveOpsHubStrings.ExportGateErrorInputMissing),
                vietnamese: "Cổng xuất cần đầu vào để đánh giá.",
                english: "The export gate needs input to evaluate.");
            table.Add(nameof(LiveOpsHubStrings.ExportGateErrorNegativeCount),
                vietnamese: "Số đếm của cổng xuất không được âm.",
                english: "Export gate counts cannot be negative.");
        }
    }
}
