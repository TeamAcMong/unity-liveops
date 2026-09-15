namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng ExportGate (G-EXPORTGATE): năm dòng cổng xuất, dạng gọn (PD-11), lý do cạnh nút và tooltip ba nút, card lỗi (h),
    /// dòng parser lệch (V-6), note (i)/(j), badge health màn Xuất và cột "chặn Copy JSON" của Tổng quan. Microcopy nguyên văn
    /// theo [SD2 §3.2], [SD2 §3.4], [SD2 §3.10]. Mọi hằng mang tiền tố <c>ExportGate</c> vì lớp <c>partial</c> dùng chung với mọi
    /// vùng khác; câu có số là chuỗi định dạng <c>{0}</c> để nơi gọi chèn số đã qua <see cref="LiveOpsHubFormat"/>.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Card cổng — meta header [SD2 §3.4].
        internal const string ExportGateCardMetaBlocked = "bấm một dòng để tới màn liên quan";
        internal const string ExportGateCardMetaReady = "đủ điều kiện để copy";

        // Dòng 1 — Không còn đợt bị bỏ.
        internal const string ExportGateDroppedOk = "Kiểm lịch: không có đợt bị bỏ";
        internal const string ExportGateDroppedBlockedFormat = "Kiểm lịch: {0} đợt bị bỏ";
        internal const string ExportGateDroppedNeverChecked = "Kiểm lịch chưa chạy — chưa biết đợt nào bị bỏ";
        internal const string ExportGateDroppedRunning = "Đang kiểm lại — chờ kết quả đợt bị bỏ";
        internal const string ExportGateDroppedStale = "Kiểm lịch cũ — kiểm lại để biết còn đợt bị bỏ không";
        internal const string ExportGateDroppedStaleMetaFormat = "lần kiểm trước: {0} đợt bị bỏ";

        // Dòng 2 — Parser của game đọc lại. Lý do bỏ gom theo nhóm như meta Hình 18 "1 không đọc được · 1 bị bỏ vì chồng giờ".
        internal const string ExportGateReadBackOkFormat = "Parser của game xác nhận: giữ {0}/{1} mục, khớp Kiểm lịch";
        // Bản "bộ biên dịch" dùng khi Kiểm lịch cũ/chưa chạy hoặc số Bị bỏ của Kiểm lịch khác số bộ biên dịch bỏ (luật 8, phát hiện
        // đã bỏ qua) — nói "Kiểm lịch" lúc đó là mâu thuẫn với dòng 1.
        internal const string ExportGateReadBackOkCompilerFormat = "Parser của game xác nhận: giữ {0}/{1} mục, khớp bộ biên dịch của hub";
        internal const string ExportGateReadBackMismatchKeptCompilerFormat = "Parser giữ {0}/{1} nhưng bộ biên dịch của hub bỏ {2} — lỗi của hub";
        internal const string ExportGateReadBackMismatchSameCountCompilerFormat = "Parser giữ {0}/{1}, bộ biên dịch của hub cũng bỏ {2} nhưng khác mục — lỗi của hub";
        internal const string ExportGateReadBackMismatchEntryCountCompilerFormat = "Parser đọc {0} mục nhưng bộ biên dịch của hub có {1} mục — lỗi của hub";
        internal const string ExportGateReadBackOkNarrowFormat = "Parser của game xác nhận: giữ {0}/{1} mục";
        internal const string ExportGateReadBackMismatchKeptFormat = "Parser giữ {0}/{1} nhưng Kiểm lịch báo {2} bị bỏ — lỗi của hub";
        internal const string ExportGateReadBackMismatchSameCountFormat = "Parser giữ {0}/{1}, Kiểm lịch cũng báo {2} bị bỏ nhưng khác mục — lỗi của hub";
        internal const string ExportGateReadBackMismatchEntryCountFormat = "Parser đọc {0} mục nhưng Kiểm lịch có {1} mục — lỗi của hub";
        internal const string ExportGateReadBackMismatchNarrow = "Parser lệch Kiểm lịch — lỗi của hub";
        internal const string ExportGateReadBackMismatchMetaFormat = "mục thứ {0}: parser {1}, Kiểm lịch {2}";
        internal const string ExportGateReadBackFailed = "Parser của game không đọc lại được JSON này — lỗi của hub";
        internal const string ExportGateReadBackFailedNarrow = "Parser không đọc lại được JSON";
        internal const string ExportGateEntryKeptFormat = "giữ {0}";
        internal const string ExportGateEntryDroppedFormat = "bỏ {0} ({1})";
        internal const string ExportGateEntryAbsent = "không có mục";
        internal const string ExportGateEntryNameFormat = "{0} · {1}";
        internal const string ExportGateReasonCountFormat = "{0} {1}";
        internal const string ExportGateCopyErrorAction = "Copy lỗi";

        internal const string ExportGateDropReasonUnreadable = "không đọc được";
        internal const string ExportGateDropReasonInvalidIdentifier = "id hoặc loại sai định dạng";
        internal const string ExportGateDropReasonEndNotAfterStart = "kết thúc không sau bắt đầu";
        internal const string ExportGateDropReasonDuplicateEventId = "bị bỏ vì trùng id";
        internal const string ExportGateDropReasonOverlapsSameType = "bị bỏ vì chồng giờ";
        internal const string ExportGateDropReasonShadowedByRecurring = "bị luật lặp che";
        internal const string ExportGateDropReasonInvalidRecurringRule = "luật lặp hỏng";
        internal const string ExportGateDropReasonDuplicateRecurringType = "bị bỏ vì trùng loại luật lặp";

        // Bản chép khi bấm "Copy lỗi" ở dòng parser lệch — người dùng dán cho dev, nên nêu sha và từng vị trí lệch.
        internal const string ExportGateMismatchReportHeaderFormat = "LiveOps Hub: parser của game đọc lại JSON (sha {0}) lệch Kiểm lịch";
        internal const string ExportGateMismatchReportCountsFormat = "Parser giữ {0}/{1} mục · Kiểm lịch giữ {2}/{3} mục";

        // Dòng 3 — Kiểm lịch chạy sau lần sửa cuối.
        internal const string ExportGateFreshnessOkFormat = "Kiểm lịch chạy sau lần sửa cuối ({0} UTC)";
        internal const string ExportGateFreshnessStaleChangedFormat = "Kiểm lịch cũ: lịch đổi lúc {0}, sau lần kiểm {1}";
        internal const string ExportGateFreshnessStaleMilestoneFormat = "Kiểm lịch cũ: đã qua mốc {0} sau lần kiểm {1}";
        // Không rõ vì sao cũ (PD-23: có thể do domain reload, không phải sửa) thì không khẳng định "lịch đã đổi".
        internal const string ExportGateFreshnessStaleFormat = "Kiểm lịch cũ: kết quả lúc {0} có thể không còn đúng";
        internal const string ExportGateFreshnessStaleNarrow = "Kiểm lịch cũ";
        internal const string ExportGateFreshnessNeverChecked = "Kiểm lịch chưa chạy lần nào";
        internal const string ExportGateFreshnessRunningFormat = "Đang kiểm lại {0}/{1} luật…";
        internal const string ExportGateFreshnessRunningMeta = "tự chạy khi mở màn";
        internal const string ExportGateStartCheckAction = "Kiểm lại (F5)";

        // Dòng 4 — Bản remote: không bao giờ chặn [SD2 §3.4].
        internal const string ExportGateRemoteNotPasted = "Bản remote: chưa dán — không biết Firebase đang giữ gì";
        internal const string ExportGateRemoteNotPastedNarrow = "Bản remote: chưa dán";
        internal const string ExportGateRemoteNotVerifiedAfterMark = "Chưa đối chiếu với Firebase";
        internal const string ExportGateRemoteMatchesFormat = "Bản remote khớp dấu {0}";
        internal const string ExportGateRemoteDiffersFormat = "Bản remote khác dấu {0}";
        internal const string ExportGateRemoteVerifiedSuffixFormat = " · đã đối chiếu {0}";
        internal const string ExportGateRemoteWithoutStamp = "Bản remote đã dán nhưng chưa có dấu đã đăng để đối chiếu";
        internal const string ExportGateRemotePasteAction = "Dán JSON đang chạy…";
        internal const string ExportGateRemoteCompareAction = "Xem khác biệt";
        internal const string ExportGateRemoteStillUnchecked = "Bản remote vẫn chưa kiểm";

        // Dòng 5 — Đã xem mục bắt buộc.
        internal const string ExportGateReviewedOkFormat = "Đã xem {0}/{1} thay đổi bắt buộc";
        internal const string ExportGateReviewedNothingRequired = "Không có thay đổi bắt buộc cần xem";
        internal const string ExportGateReviewedBlockedFormat = "Chưa xem {0} thay đổi làm người chơi mất tiến độ";
        internal const string ExportGateReviewedItemsSuffixFormat = " ({0})";
        internal const string ExportGateReviewedDiffMissing = "Chưa so được với bản đã đăng";
        internal const string ExportGateItemFieldFormat = "{0} {1}";
        internal const string ExportGateListSeparator = ", ";

        // Nhãn field trong ngoặc của dòng 5 và tooltip Copy ("weekly-pass tiền tố") — chỉ những field người dùng sửa được.
        internal const string ExportGateFieldIdPrefix = "tiền tố";
        internal const string ExportGateFieldAnchor = "neo";
        internal const string ExportGateFieldPeriod = "chu kỳ";
        internal const string ExportGateFieldActiveDuration = "thời gian chạy";
        internal const string ExportGateFieldStart = "giờ bắt đầu";
        internal const string ExportGateFieldEnd = "giờ kết thúc";
        internal const string ExportGateFieldType = "loại";

        // Dạng gọn (PD-11): chỉ đếm dòng có thể chặn.
        internal const string ExportGateCompactFormat = "{0} điều kiện đạt · sha {1} · {2}";

        // Lý do cạnh nút [SD2 §3.2].
        internal const string ExportGateReasonBlockedPrefix = "Chặn: ";
        internal const string ExportGateReasonPartSeparator = " · ";
        internal const string ExportGateReasonReadBackFailed = "parser không đọc lại được JSON";
        internal const string ExportGateReasonReadBackMismatch = "parser lệch Kiểm lịch";
        internal const string ExportGateReasonNeverChecked = "chưa kiểm lịch";
        internal const string ExportGateReasonStale = "Kiểm lịch cũ";
        internal const string ExportGateReasonRunning = "đang kiểm lại lịch";
        internal const string ExportGateReasonDroppedFormat = "{0} đợt bị bỏ";
        internal const string ExportGateReasonUnreviewedFormat = "{0} thay đổi bắt buộc chưa xem";
        internal const string ExportGateReasonDiffMissing = "chưa so được với bản đã đăng";
        internal const string ExportGateReasonMissingForMark = "Còn thiếu cho Đánh dấu: copy hoặc lưu file bản này";
        internal const string ExportGateReasonNoChanges = "Không có thay đổi để ghi";

        // Tooltip ba nút — đặt trên slot vì nút disabled không nhận hover [SD2 §3.2].
        internal const string ExportGateCopyTooltipBlockedPrefix = "Copy JSON: ";
        internal const string ExportGateCopyTooltipLastSeparator = ", và ";
        internal const string ExportGateCopyTooltipDroppedFormat = "còn {0} đợt game sẽ bỏ";
        internal const string ExportGateCopyTooltipUnreviewedFormat = "{0} thay đổi đụng đợt đang chạy chưa xem";
        internal const string ExportGateCopyTooltipReadBackFailed = "parser của game không đọc lại được JSON do hub tạo";
        internal const string ExportGateCopyTooltipReadBackMismatch = "parser của game lệch Kiểm lịch";
        internal const string ExportGateCopyTooltipNeverChecked = "Kiểm lịch chưa chạy — bấm F5";
        internal const string ExportGateCopyTooltipStale = "Kiểm lịch cũ — kiểm lại (F5) trước";
        internal const string ExportGateCopyTooltipRunning = "đang kiểm lại lịch";
        internal const string ExportGateCopyTooltipDiffMissing = "chưa so được với bản đã đăng";
        internal const string ExportGateCopyTooltipReadyFormat = "Copy {0} vào clipboard · sha {1}";
        internal const string ExportGateSaveTooltipBlocked = "Lưu file: cùng điều kiện với Copy JSON";
        internal const string ExportGateSaveTooltipReadyFormat = "Lưu file: {0} · sha {1}";
        internal const string ExportGateMarkTooltipNotExported = "Chưa copy hay lưu file JSON này";
        internal const string ExportGateMarkTooltipDraftChangedFormat = "Nháp đã đổi sau lần copy {0} — copy bản mới rồi dán lại";
        internal const string ExportGateMarkTooltipReadBackUnconfirmed = "Parser của game chưa xác nhận JSON này";
        internal const string ExportGateMarkTooltipReadyFormat = "Ghi dấu đã đăng cho sha {0}";

        // (h) card lỗi đầu thân [SD2 §3.10].
        internal const string ExportGateFailureSymptom = "JSON do hub tạo không đọc lại được — lỗi của hub, không phải của lịch";
        internal const string ExportGateFailurePositionFormat = "ở dòng {0}, cột {1}";
        internal const string ExportGateFailureOpenLineFormat = "Mở JSON ở dòng {0}";
        internal const string ExportGateFailureNote = "Báo dev kèm lỗi; không dán JSON này lên Firebase. Mọi nút xuất disabled cho tới khi parser đọc được.";
        internal const string ExportGateFailureReportHeaderFormat = "LiveOps Hub: parser của game không đọc lại được JSON do hub tạo (sha {0})";

        // Thân Xuất: diff trống (f)/(g), note (i), HelpBox (j).
        internal const string ExportGateNoChangesFormat = "Không có gì mới so với {0} · sha {1}";
        internal const string ExportGateSameCalendarDifferentJsonFormat = "Lịch không đổi so với {0}, nhưng JSON sắp xuất khác bản đã đăng: sha {1} → {2}";
        internal const string ExportGateFirstPublish = "Chưa có dấu đã đăng nào — lần này sẽ là bản so đầu tiên";
        internal const string ExportGateRestoringFormat = "Nháp đang là bản khôi phục từ {0}";
        internal const string ExportGateUndoRestoreAction = "Hoàn tác khôi phục";
        internal const string ExportGateFormat1NoticeFormat = "Định dạng 1 không có recurring: {0} luật lặp sẽ không được xuất, game chỉ thấy {1} đợt cố định";

        // Health màn Xuất (6.4) và cột Tổng quan [SD1 §1.1].
        internal const string ExportGateHealthBlockedBadge = "chặn";
        internal const string ExportGateHealthNeedsReviewBadgeFormat = "{0} cần xem";
        internal const string ExportGateCopyBlockedColumn = "chặn Copy JSON";

        // Lỗi lập trình của model (ArgumentException) — không bao giờ do dữ liệu lịch hỏng.
        internal const string ExportGateErrorJsonMissing = "Cổng xuất cần JSON đã ghi.";
        internal const string ExportGateErrorCompilationMissing = "Cổng xuất cần kết quả biên dịch nháp theo thứ tự xuất.";
        internal const string ExportGateErrorFormatMissing = "Cổng xuất cần bộ định dạng chữ.";
        internal const string ExportGateErrorReadBackMissing = "Cổng xuất cần kết quả parser đọc lại khi JSON đọc được.";
        internal const string ExportGateErrorInputMissing = "Cổng xuất cần đầu vào để đánh giá.";
        internal const string ExportGateErrorNegativeCount = "Số đếm của cổng xuất không được âm.";
    }
}
