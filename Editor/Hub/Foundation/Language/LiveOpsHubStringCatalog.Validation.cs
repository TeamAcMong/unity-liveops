namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Validation (màn Kiểm lịch) — bản tiếng Việt chép nguyên văn từ thiết kế [SD2 §1.1, §2.1–2.8], bản tiếng
    /// Anh dịch cùng lúc (G-I18N §4: không bao giờ thêm khoá ở một bản mà quên bản kia). Comment "vì sao" của từng câu ở
    /// <c>LiveOpsHubStrings.Validation.cs</c> cạnh property.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterValidation(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.ValidationMissingLayoutFormat),
                vietnamese: "Thiếu bố cục màn Kiểm lịch: {0}",
                english: "The Check calendar layout is missing: {0}");

            table.Add(nameof(LiveOpsHubStrings.ValidationNoAssetTitle),
                vietnamese: "Chưa có lịch LiveOps trong project",
                english: "No LiveOps calendar in this project");
            table.Add(nameof(LiveOpsHubStrings.ValidationNoAssetBody),
                vietnamese: "Kiểm lịch chạy 12 luật trên một LiveEventCalendarAsset — mở hoặc tạo lịch ở Tổng quan trước.",
                english: "Check calendar runs 12 rules over a LiveEventCalendarAsset — open or create one in Overview first.");
            table.Add(nameof(LiveOpsHubStrings.ValidationNoAssetActionButton),
                vietnamese: "Mở Tổng quan để tạo lịch",
                english: "Open Overview to create a calendar");

            table.Add(nameof(LiveOpsHubStrings.ValidationSafeRepairButtonFormat),
                vietnamese: "Sửa các lỗi an toàn ({0})…",
                english: "Fix the safe errors ({0})…");
            table.Add(nameof(LiveOpsHubStrings.ValidationSafeRepairNothingReason),
                vietnamese: "Không có lỗi nào sửa nhanh an toàn được",
                english: "No error can be fixed safely");

            table.Add(nameof(LiveOpsHubStrings.ValidationRecheckButton),
                vietnamese: "Kiểm lại",
                english: "Check again");
            table.Add(nameof(LiveOpsHubStrings.ValidationRecheckRunningButton),
                vietnamese: "Đang kiểm",
                english: "Checking");

            table.Add(nameof(LiveOpsHubStrings.ValidationTabAllFormat),
                vietnamese: "Tất cả {0}",
                english: "All {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationTabDroppedFormat),
                vietnamese: "Bị bỏ {0}",
                english: "Dropped {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationTabProgressLostFormat),
                vietnamese: "Mất tiến độ {0}",
                english: "Progress lost {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationTabShouldReviewFormat),
                vietnamese: "Nên xem {0}",
                english: "Should review {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationTabNotMeasuredFormat),
                vietnamese: "Chưa kiểm {0}",
                english: "Not measured {0}");

            table.Add(nameof(LiveOpsHubStrings.ValidationTypeMenuAll),
                vietnamese: "Loại event: tất cả",
                english: "Event type: all");
            table.Add(nameof(LiveOpsHubStrings.ValidationTypeMenuFormat),
                vietnamese: "Loại event: {0}",
                english: "Event type: {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationTypeMenuAllItem),
                vietnamese: "Tất cả",
                english: "All");
            table.Add(nameof(LiveOpsHubStrings.ValidationSearchPlaceholder),
                vietnamese: "Tìm id đợt hoặc id luật",
                english: "Search an event id or a rule id");

            table.Add(nameof(LiveOpsHubStrings.ValidationSummaryDroppedFormat),
                vietnamese: "{0} bị bỏ",
                english: "{0} dropped");
            table.Add(nameof(LiveOpsHubStrings.ValidationSummaryProgressLostFormat),
                vietnamese: "{0} mất tiến độ",
                english: "{0} progress lost");
            table.Add(nameof(LiveOpsHubStrings.ValidationSummaryShouldReviewFormat),
                vietnamese: "{0} nên xem",
                english: "{0} should review");
            table.Add(nameof(LiveOpsHubStrings.ValidationSummaryNotMeasuredFormat),
                vietnamese: "{0} chưa kiểm",
                english: "{0} not measured");
            table.Add(nameof(LiveOpsHubStrings.ValidationSummaryPassedFormat),
                vietnamese: "{0} luật đã qua",
                english: "{0} rules passed");
            table.Add(nameof(LiveOpsHubStrings.ValidationSummaryRightFormat),
                vietnamese: "Kiểm lúc {0} · {1} luật · {2} lỗi sửa nhanh an toàn được",
                english: "Checked at {0} · {1} rules · {2} errors can be fixed safely");

            table.Add(nameof(LiveOpsHubStrings.ValidationGroupDroppedTitle),
                vietnamese: "Bị bỏ khi game đọc lịch",
                english: "Dropped when the game reads the calendar");
            table.Add(nameof(LiveOpsHubStrings.ValidationGroupDroppedBlurb),
                vietnamese: "Game đọc lịch và bỏ qua các đợt này; người chơi không bao giờ thấy chúng.",
                english: "The game reads the calendar and skips these events; players never see them.");
            table.Add(nameof(LiveOpsHubStrings.ValidationGroupProgressLostTitle),
                vietnamese: "Người chơi mất tiến độ",
                english: "Players lose progress");
            table.Add(nameof(LiveOpsHubStrings.ValidationGroupProgressLostBlurb),
                vietnamese: "Đợt vẫn chạy, nhưng tiến độ đã có bị mất.",
                english: "The event still runs, but the progress already earned is lost.");
            table.Add(nameof(LiveOpsHubStrings.ValidationGroupShouldReviewTitle),
                vietnamese: "Nên xem",
                english: "Should review");
            table.Add(nameof(LiveOpsHubStrings.ValidationGroupNotMeasuredTitle),
                vietnamese: "Chưa kiểm",
                english: "Not measured");
            table.Add(nameof(LiveOpsHubStrings.ValidationGroupPassedTitleFormat),
                vietnamese: "{0} luật đã qua",
                english: "{0} rules passed");
            table.Add(nameof(LiveOpsHubStrings.ValidationGroupIgnoredTitleFormat),
                vietnamese: "Đã bỏ qua ({0})",
                english: "Ignored ({0})");

            table.Add(nameof(LiveOpsHubStrings.ValidationGroupMetaFindingsFormat),
                vietnamese: "{0} phát hiện",
                english: "{0} findings");
            table.Add(nameof(LiveOpsHubStrings.ValidationGroupMetaSafeRepairFormat),
                vietnamese: "{0} phát hiện · sửa nhanh được {1}/{2}",
                english: "{0} findings · {1}/{2} can be fixed safely");
            table.Add(nameof(LiveOpsHubStrings.ValidationGroupMetaNotMeasuredFormat),
                vietnamese: "{0} luật · không tính là đã qua",
                english: "{0} rules · this does not count as passed");

            table.Add(nameof(LiveOpsHubStrings.ValidationStaleTag),
                vietnamese: "cũ",
                english: "stale");
            table.Add(nameof(LiveOpsHubStrings.ValidationStaleEditedFormat),
                vietnamese: "Lịch đã đổi lúc {0}, sau lần kiểm {1}. Kết quả dưới đây là của bản cũ.",
                english: "The calendar changed at {0}, after the {1} check. The results below are from the older version.");
            table.Add(nameof(LiveOpsHubStrings.ValidationStaleMilestoneFormat),
                vietnamese: "Đã qua mốc {0} sau lần kiểm {1} — đợt đã mở hoặc khép nên kết quả có thể khác.",
                english: "The {0} milestone passed after the {1} check — an event opened or closed, so the results may differ.");
            table.Add(nameof(LiveOpsHubStrings.ValidationStaleInterruptedNotice),
                vietnamese: "Unity nạp lại script khi đang kiểm — lần kiểm dở dang đã mất, kiểm lại để có kết quả.",
                english: "Unity reloaded scripts while checking — the unfinished run is gone, check again for results.");

            table.Add(nameof(LiveOpsHubStrings.ValidationNeverCheckedTitle),
                vietnamese: "Chưa kiểm lịch này",
                english: "This calendar has not been checked");
            table.Add(nameof(LiveOpsHubStrings.ValidationNeverCheckedBodyFormat),
                vietnamese: "Kết quả chỉ có sau khi chạy {0} luật; chưa kiểm không có nghĩa là không có lỗi.",
                english: "Results only exist after the {0} rules run; not checked does not mean no errors.");
            table.Add(nameof(LiveOpsHubStrings.ValidationNeverCheckedActionFormat),
                vietnamese: "Kiểm ngay ({0})",
                english: "Check now ({0})");

            table.Add(nameof(LiveOpsHubStrings.ValidationProgressFormat),
                vietnamese: "Đang kiểm {0}/{1} luật…",
                english: "Checking {0}/{1} rules…");
            table.Add(nameof(LiveOpsHubStrings.ValidationProgressRatioFormat),
                vietnamese: "{0} / {1}",
                english: "{0} / {1}");

            table.Add(nameof(LiveOpsHubStrings.ValidationNoErrorsTitle),
                vietnamese: "Không có đợt nào bị bỏ hay mất tiến độ",
                english: "No event is dropped and no progress is lost");
            table.Add(nameof(LiveOpsHubStrings.ValidationNoErrorsBodyFormat),
                vietnamese: "{0} luật đã qua · {1} chưa kiểm. Chưa kiểm không tính là đã qua.",
                english: "{0} rules passed · {1} not measured. Not measured does not count as passed.");
            table.Add(nameof(LiveOpsHubStrings.ValidationShouldReviewOnlyNote),
                vietnamese: "Không có đợt nào bị bỏ hay mất tiến độ",
                english: "No event is dropped and no progress is lost");

            table.Add(nameof(LiveOpsHubStrings.ValidationSafeRepairUndoFormat),
                vietnamese: "LiveOps: Sửa nhanh {0} lỗi",
                english: "LiveOps: Quick fix {0} errors");

            table.Add(nameof(LiveOpsHubStrings.ValidationDetailTitle),
                vietnamese: "Chi tiết",
                english: "Details");
            table.Add(nameof(LiveOpsHubStrings.ValidationDetailCloseTooltip),
                vietnamese: "Đóng (Esc)",
                english: "Close (Esc)");
            table.Add(nameof(LiveOpsHubStrings.ValidationDetailEmpty),
                vietnamese: "Chọn một phát hiện để xem điều xảy ra với người chơi",
                english: "Select a finding to see what happens to players");
            table.Add(nameof(LiveOpsHubStrings.ValidationDetailEventLabel),
                vietnamese: "Đợt",
                english: "Event");
            table.Add(nameof(LiveOpsHubStrings.ValidationDetailTypeLabel),
                vietnamese: "Loại",
                english: "Type");
            table.Add(nameof(LiveOpsHubStrings.ValidationDetailRuleLabel),
                vietnamese: "Luật",
                english: "Rule");
            table.Add(nameof(LiveOpsHubStrings.ValidationDetailWhatHappensHeading),
                vietnamese: "ĐIỀU XẢY RA VỚI NGƯỜI CHƠI",
                english: "WHAT HAPPENS TO PLAYERS");
            table.Add(nameof(LiveOpsHubStrings.ValidationDetailWhyHeading),
                vietnamese: "VÌ SAO",
                english: "WHY");
            table.Add(nameof(LiveOpsHubStrings.ValidationDetailHowToFixHeading),
                vietnamese: "CÁCH SỬA",
                english: "HOW TO FIX");
            table.Add(nameof(LiveOpsHubStrings.ValidationDetailDocumentationLink),
                vietnamese: "Vì sao? (tài liệu luật)",
                english: "Why? (rule documentation)");

            table.Add(nameof(LiveOpsHubStrings.ValidationProposalHeaderFormat),
                vietnamese: "Đề xuất cho {0}",
                english: "Proposals for {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalQuickCheckOk),
                vietnamese: "kiểm nhanh làn này: không chồng",
                english: "quick check on this lane: no overlap");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalQuickCheckOverlapFormat),
                vietnamese: "kiểm nhanh làn này: vẫn còn {0} vấn đề",
                english: "quick check on this lane: {0} issues remain");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalEnterHint),
                vietnamese: "Enter: Quay lại",
                english: "Enter: Back");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalApplyFormat),
                vietnamese: "Áp: {0}",
                english: "Apply: {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalApplyShiftFormat),
                vietnamese: "Áp: dời {0}",
                english: "Apply: move {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalApplyRenameFormat),
                vietnamese: "Áp: đổi {0}",
                english: "Apply: rename {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalApplyNormalizeFormat),
                vietnamese: "Áp: chuẩn hoá giờ {0}",
                english: "Apply: normalise the time of {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalApplySetDurationFormat),
                vietnamese: "Áp: đặt {0} dài 24 giờ",
                english: "Apply: set {0} to 24 hours");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalApplySwapFormat),
                vietnamese: "Áp: đảo giờ của {0}",
                english: "Apply: swap the times of {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalApplySetActiveFormat),
                vietnamese: "Áp: đặt số giờ chạy của {0}",
                english: "Apply: set the active hours of {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalApplyRevertFormat),
                vietnamese: "Áp: hoàn về cho {0}",
                english: "Apply: revert {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalApplyDeferFormat),
                vietnamese: "Áp: hẹn lại {0}",
                english: "Apply: defer {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalOptionDetailFormat),
                vietnamese: "bắt đầu {0} → {1} · dài {2}",
                english: "start {0} → {1} · lasts {2}");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalConsequenceFormat),
                vietnamese: "Đợt sẽ xuất hiện với người chơi từ {0}.",
                english: "The event will show up for players from {0}.");
            table.Add(nameof(LiveOpsHubStrings.ValidationProposalBackButton),
                vietnamese: "Quay lại",
                english: "Back");

            table.Add(nameof(LiveOpsHubStrings.ValidationProposalAppliedFormat),
                vietnamese: "Áp đề xuất cho {0}: {1}",
                english: "Applied a proposal to {0}: {1}");
        }
    }
}
