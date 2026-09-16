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
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateCardMetaReady),
                vietnamese: "đủ điều kiện để copy",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateDroppedOk),
                vietnamese: "Kiểm lịch: không có đợt bị bỏ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateDroppedBlockedFormat),
                vietnamese: "Kiểm lịch: {0} đợt bị bỏ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateDroppedNeverChecked),
                vietnamese: "Kiểm lịch chưa chạy — chưa biết đợt nào bị bỏ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateDroppedRunning),
                vietnamese: "Đang kiểm lại — chờ kết quả đợt bị bỏ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateDroppedStale),
                vietnamese: "Kiểm lịch cũ — kiểm lại để biết còn đợt bị bỏ không",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateDroppedStaleMetaFormat),
                vietnamese: "lần kiểm trước: {0} đợt bị bỏ",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackOkFormat),
                vietnamese: "Parser của game xác nhận: giữ {0}/{1} mục, khớp Kiểm lịch",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackOkCompilerFormat),
                vietnamese: "Parser của game xác nhận: giữ {0}/{1} mục, khớp bộ biên dịch của hub",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchKeptCompilerFormat),
                vietnamese: "Parser giữ {0}/{1} nhưng bộ biên dịch của hub bỏ {2} — lỗi của hub",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchSameCountCompilerFormat),
                vietnamese: "Parser giữ {0}/{1}, bộ biên dịch của hub cũng bỏ {2} nhưng khác mục — lỗi của hub",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchEntryCountCompilerFormat),
                vietnamese: "Parser đọc {0} mục nhưng bộ biên dịch của hub có {1} mục — lỗi của hub",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackOkNarrowFormat),
                vietnamese: "Parser của game xác nhận: giữ {0}/{1} mục",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchKeptFormat),
                vietnamese: "Parser giữ {0}/{1} nhưng Kiểm lịch báo {2} bị bỏ — lỗi của hub",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchSameCountFormat),
                vietnamese: "Parser giữ {0}/{1}, Kiểm lịch cũng báo {2} bị bỏ nhưng khác mục — lỗi của hub",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchEntryCountFormat),
                vietnamese: "Parser đọc {0} mục nhưng Kiểm lịch có {1} mục — lỗi của hub",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchNarrow),
                vietnamese: "Parser lệch Kiểm lịch — lỗi của hub",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackMismatchMetaFormat),
                vietnamese: "mục thứ {0}: parser {1}, Kiểm lịch {2}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackFailed),
                vietnamese: "Parser của game không đọc lại được JSON này — lỗi của hub",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReadBackFailedNarrow),
                vietnamese: "Parser không đọc lại được JSON",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateEntryKeptFormat),
                vietnamese: "giữ {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateEntryDroppedFormat),
                vietnamese: "bỏ {0} ({1})",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateEntryAbsent),
                vietnamese: "không có mục",
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.ExportGateEntryNameFormat), "{0} · {1}");
            table.AddShared(nameof(LiveOpsHubStrings.ExportGateReasonCountFormat), "{0} {1}");
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyErrorAction),
                vietnamese: "Copy lỗi",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonUnreadable),
                vietnamese: "không đọc được",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonInvalidIdentifier),
                vietnamese: "id hoặc loại sai định dạng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonEndNotAfterStart),
                vietnamese: "kết thúc không sau bắt đầu",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonDuplicateEventId),
                vietnamese: "bị bỏ vì trùng id",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonOverlapsSameType),
                vietnamese: "bị bỏ vì chồng giờ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonShadowedByRecurring),
                vietnamese: "bị luật lặp che",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonInvalidRecurringRule),
                vietnamese: "luật lặp hỏng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateDropReasonDuplicateRecurringType),
                vietnamese: "bị bỏ vì trùng loại luật lặp",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateMismatchReportHeaderFormat),
                vietnamese: "LiveOps Hub: parser của game đọc lại JSON (sha {0}) lệch Kiểm lịch",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateMismatchReportCountsFormat),
                vietnamese: "Parser giữ {0}/{1} mục · Kiểm lịch giữ {2}/{3} mục",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessOkFormat),
                vietnamese: "Kiểm lịch chạy sau lần sửa cuối ({0} UTC)",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessStaleChangedFormat),
                vietnamese: "Kiểm lịch cũ: lịch đổi lúc {0}, sau lần kiểm {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessStaleMilestoneFormat),
                vietnamese: "Kiểm lịch cũ: đã qua mốc {0} sau lần kiểm {1}",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessStaleFormat),
                vietnamese: "Kiểm lịch cũ: kết quả lúc {0} có thể không còn đúng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessStaleNarrow),
                vietnamese: "Kiểm lịch cũ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessNeverChecked),
                vietnamese: "Kiểm lịch chưa chạy lần nào",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessRunningFormat),
                vietnamese: "Đang kiểm lại {0}/{1} luật…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFreshnessRunningMeta),
                vietnamese: "tự chạy khi mở màn",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateStartCheckAction),
                vietnamese: "Kiểm lại (F5)",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteNotPasted),
                vietnamese: "Bản remote: chưa dán — không biết Firebase đang giữ gì",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteNotPastedNarrow),
                vietnamese: "Bản remote: chưa dán",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteNotVerifiedAfterMark),
                vietnamese: "Chưa đối chiếu với Firebase",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteMatchesFormat),
                vietnamese: "Bản remote khớp dấu {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteDiffersFormat),
                vietnamese: "Bản remote khác dấu {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteVerifiedSuffixFormat),
                vietnamese: " · đã đối chiếu {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteWithoutStamp),
                vietnamese: "Bản remote đã dán nhưng chưa có dấu đã đăng để đối chiếu",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemotePasteAction),
                vietnamese: "Dán JSON đang chạy…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteCompareAction),
                vietnamese: "Xem khác biệt",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateRemoteStillUnchecked),
                vietnamese: "Bản remote vẫn chưa kiểm",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateReviewedOkFormat),
                vietnamese: "Đã xem {0}/{1} thay đổi bắt buộc",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReviewedNothingRequired),
                vietnamese: "Không có thay đổi bắt buộc cần xem",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReviewedBlockedFormat),
                vietnamese: "Chưa xem {0} thay đổi làm người chơi mất tiến độ",
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.ExportGateReviewedItemsSuffixFormat), " ({0})");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReviewedDiffMissing),
                vietnamese: "Chưa so được với bản đã đăng",
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.ExportGateItemFieldFormat), "{0} {1}");
            table.AddShared(nameof(LiveOpsHubStrings.ExportGateListSeparator), ", ");

            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldIdPrefix),
                vietnamese: "tiền tố",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldAnchor),
                vietnamese: "neo",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldPeriod),
                vietnamese: "chu kỳ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldActiveDuration),
                vietnamese: "thời gian chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldStart),
                vietnamese: "giờ bắt đầu",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldEnd),
                vietnamese: "giờ kết thúc",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFieldType),
                vietnamese: "loại",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateCompactFormat),
                vietnamese: "{0} điều kiện đạt · sha {1} · {2}",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonBlockedPrefix),
                vietnamese: "Chặn: ",
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.ExportGateReasonPartSeparator), " · ");
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonReadBackFailed),
                vietnamese: "parser không đọc lại được JSON",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonReadBackMismatch),
                vietnamese: "parser lệch Kiểm lịch",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonNeverChecked),
                vietnamese: "chưa kiểm lịch",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonStale),
                vietnamese: "Kiểm lịch cũ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonRunning),
                vietnamese: "đang kiểm lại lịch",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonDroppedFormat),
                vietnamese: "{0} đợt bị bỏ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonUnreviewedFormat),
                vietnamese: "{0} thay đổi bắt buộc chưa xem",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonDiffMissing),
                vietnamese: "chưa so được với bản đã đăng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonMissingForMark),
                vietnamese: "Còn thiếu cho Đánh dấu: copy hoặc lưu file bản này",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateReasonNoChanges),
                vietnamese: "Không có thay đổi để ghi",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipBlockedPrefix),
                vietnamese: "Copy JSON: ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipLastSeparator),
                vietnamese: ", và ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipDroppedFormat),
                vietnamese: "còn {0} đợt game sẽ bỏ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipUnreviewedFormat),
                vietnamese: "{0} thay đổi đụng đợt đang chạy chưa xem",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipReadBackFailed),
                vietnamese: "parser của game không đọc lại được JSON do hub tạo",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipReadBackMismatch),
                vietnamese: "parser của game lệch Kiểm lịch",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipNeverChecked),
                vietnamese: "Kiểm lịch chưa chạy — bấm F5",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipStale),
                vietnamese: "Kiểm lịch cũ — kiểm lại (F5) trước",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipRunning),
                vietnamese: "đang kiểm lại lịch",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipDiffMissing),
                vietnamese: "chưa so được với bản đã đăng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyTooltipReadyFormat),
                vietnamese: "Copy {0} vào clipboard · sha {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateSaveTooltipBlocked),
                vietnamese: "Lưu file: cùng điều kiện với Copy JSON",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateSaveTooltipReadyFormat),
                vietnamese: "Lưu file: {0} · sha {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateMarkTooltipNotExported),
                vietnamese: "Chưa copy hay lưu file JSON này",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateMarkTooltipDraftChangedFormat),
                vietnamese: "Nháp đã đổi sau lần copy {0} — copy bản mới rồi dán lại",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateMarkTooltipReadBackUnconfirmed),
                vietnamese: "Parser của game chưa xác nhận JSON này",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateMarkTooltipReadyFormat),
                vietnamese: "Ghi dấu đã đăng cho sha {0}",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateFailureSymptom),
                vietnamese: "JSON do hub tạo không đọc lại được — lỗi của hub, không phải của lịch",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFailurePositionFormat),
                vietnamese: "ở dòng {0}, cột {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFailureOpenLineFormat),
                vietnamese: "Mở JSON ở dòng {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFailureNote),
                vietnamese: "Báo dev kèm lỗi; không dán JSON này lên Firebase. Mọi nút xuất disabled cho tới khi parser đọc được.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFailureReportHeaderFormat),
                vietnamese: "LiveOps Hub: parser của game không đọc lại được JSON do hub tạo (sha {0})",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateNoChangesFormat),
                vietnamese: "Không có gì mới so với {0} · sha {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateSameCalendarDifferentJsonFormat),
                vietnamese: "Lịch không đổi so với {0}, nhưng JSON sắp xuất khác bản đã đăng: sha {1} → {2}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFirstPublish),
                vietnamese: "Chưa có dấu đã đăng nào — lần này sẽ là bản so đầu tiên",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateRestoringFormat),
                vietnamese: "Nháp đang là bản khôi phục từ {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateUndoRestoreAction),
                vietnamese: "Hoàn tác khôi phục",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateFormat1NoticeFormat),
                vietnamese: "Định dạng 1 không có recurring: {0} luật lặp sẽ không được xuất, game chỉ thấy {1} đợt cố định",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateHealthBlockedBadge),
                vietnamese: "chặn",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateHealthNeedsReviewBadgeFormat),
                vietnamese: "{0} cần xem",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateCopyBlockedColumn),
                vietnamese: "chặn Copy JSON",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ExportGateErrorJsonMissing),
                vietnamese: "Cổng xuất cần JSON đã ghi.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateErrorCompilationMissing),
                vietnamese: "Cổng xuất cần kết quả biên dịch nháp theo thứ tự xuất.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateErrorFormatMissing),
                vietnamese: "Cổng xuất cần bộ định dạng chữ.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateErrorReadBackMissing),
                vietnamese: "Cổng xuất cần kết quả parser đọc lại khi JSON đọc được.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateErrorInputMissing),
                vietnamese: "Cổng xuất cần đầu vào để đánh giá.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ExportGateErrorNegativeCount),
                vietnamese: "Số đếm của cổng xuất không được âm.",
                english: null);
        }
    }
}
