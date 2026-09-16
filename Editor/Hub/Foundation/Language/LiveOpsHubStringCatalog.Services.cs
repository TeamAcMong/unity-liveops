namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Services — bản tiếng Việt là bản gốc (chép nguyên văn từ <c>LiveOpsHubStrings.Services.cs</c>, không đổi một ký tự),
    /// bản tiếng Anh do gói dịch G-I18N bước 2 điền vào chỗ <c>english: null</c>. Hai ngôn ngữ nằm trong cùng một lệnh đăng ký để
    /// chỗ giữ chỗ <c>{0}</c>/<c>{1}</c> soát được bằng mắt và không bao giờ thêm khoá ở bản này mà quên bản kia.
    /// Comment "vì sao" của từng câu ở lại <c>LiveOpsHubStrings.Services.cs</c> cạnh property — đó là chỗ người đọc code tìm tới.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterServices(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.ServicesUndoCreateCalendar),
                vietnamese: "Tạo lịch LiveOps",
                english: "Create LiveOps calendar");
            table.Add(nameof(LiveOpsHubStrings.ServicesUndoMarkPublishedFormat),
                vietnamese: "Ghi dấu đã đăng sha {0}",
                english: "Mark published sha {0}");
            table.Add(nameof(LiveOpsHubStrings.ServicesUndoRemoveLatestStampFormat),
                vietnamese: "Gỡ dấu đã đăng sha {0}",
                english: "Remove published stamp sha {0}");
            table.Add(nameof(LiveOpsHubStrings.ServicesUndoKeepEditorVersion),
                vietnamese: "Giữ bản trong Editor thay bản trên đĩa",
                english: "Keep the Editor version instead of the disk version");
            table.Add(nameof(LiveOpsHubStrings.ServicesUndoDiscardUnsaved),
                vietnamese: "Bỏ thay đổi chưa lưu",
                english: "Discard unsaved changes");

            table.Add(nameof(LiveOpsHubStrings.ServicesEditFailedNoAsset),
                vietnamese: "Chưa có asset lịch — tạo hoặc chọn asset ở Tổng quan trước.",
                english: "No calendar asset yet — create or pick one in Overview first.");
            table.Add(nameof(LiveOpsHubStrings.ServicesEditFailedNotApplicable),
                vietnamese: "Lệnh sửa không áp được lên lịch hiện tại (mục cần sửa không còn).",
                english: "The edit does not apply to the current calendar (the item it edits is gone).");
            table.Add(nameof(LiveOpsHubStrings.ServicesEditFailedContinuousEditOpen),
                vietnamese: "Đang kéo một mục — thả chuột hoặc Esc trước khi sửa việc khác.",
                english: "An item is being dragged — release the mouse or press Esc before editing something else.");
            table.Add(nameof(LiveOpsHubStrings.ServicesEditFailedContinuousGroupMismatch),
                vietnamese: "Thao tác kéo này đã kết thúc hoặc không phải thao tác đang mở.",
                english: "This drag has ended or is not the open one.");
            table.Add(nameof(LiveOpsHubStrings.ServicesEditFailedNoChange),
                vietnamese: "Không có thay đổi để ghi.",
                english: "No change to record.");
            table.Add(nameof(LiveOpsHubStrings.ServicesSaveFailedFormat),
                vietnamese: "Không lưu được {0} — asset vẫn còn thay đổi chưa lưu.",
                english: "Cannot save {0} — the asset still has unsaved changes.");
            table.Add(nameof(LiveOpsHubStrings.ServicesSaveFailedNoPath),
                vietnamese: "Asset lịch này chỉ nằm trong bộ nhớ (không có file) nên không lưu được.",
                english: "This calendar asset lives only in memory (no file), so it cannot be saved.");
            table.Add(nameof(LiveOpsHubStrings.ServicesSaveFailedDiskConflictFormat),
                vietnamese: "Chưa lưu được {0} — file đã đổi trên đĩa trong lúc bạn đang sửa. Chọn Tải lại hoặc Giữ bản trong Editor trước đã.",
                english: "Cannot save {0} yet — the file changed on disk while you were editing. Choose Reload or Keep editor version first.");
            table.Add(nameof(LiveOpsHubStrings.ServicesStampNoPublisher),
                vietnamese: "không rõ",
                english: "unknown");

            table.Add(nameof(LiveOpsHubStrings.ServicesAssetDeletedHeadlineFormat),
                vietnamese: "{0} đã bị xoá khỏi project",
                english: "{0} was deleted from the project");
            table.Add(nameof(LiveOpsHubStrings.ServicesAssetDeletedDetail),
                vietnamese: "Hub về trạng thái chưa có lịch — tạo hoặc chọn asset khác ở Tổng quan.",
                english: "The hub is back to having no calendar — create or pick another asset in Overview.");

            table.Add(nameof(LiveOpsHubStrings.InterimDiskConflictLog),
                vietnamese: "LiveOps Hub: {0} vừa đổi trên đĩa trong lúc hub còn thay đổi chưa lưu — nháp vẫn giữ trong phiên (Tải lại / Giữ bản trong Editor chưa có ở bản dev này).",
                english: "LiveOps Hub: {0} changed on disk while the hub still had unsaved changes — the draft is kept in the session (Reload / Keep editor version are not built in this dev build).");

            table.Add(nameof(LiveOpsHubStrings.ServicesHealthNoAsset),
                vietnamese: "Chưa có asset lịch",
                english: "No calendar asset");
            table.Add(nameof(LiveOpsHubStrings.ServicesHealthNeverChecked),
                vietnamese: "Chưa kiểm lần nào — F5 để kiểm",
                english: "Never checked — press F5 to check");
            table.Add(nameof(LiveOpsHubStrings.ServicesHealthRunningFormat),
                vietnamese: "Đang kiểm {0}/{1} luật…",
                english: "Checking {0}/{1} rules…");
            table.Add(nameof(LiveOpsHubStrings.ServicesHealthInterruptedByReload),
                vietnamese: "Lần kiểm bị cắt ngang khi Unity nạp lại script — F5 để kiểm lại",
                english: "The check was cut short when Unity reloaded scripts — press F5 to check again");
            table.Add(nameof(LiveOpsHubStrings.ServicesHealthStaleEditedPrefixFormat),
                vietnamese: "Kết quả kiểm lúc {0}, lịch đã đổi sau đó: ",
                english: "Check result from {0}, the calendar changed after that: ");
            table.Add(nameof(LiveOpsHubStrings.ServicesHealthStaleMilestonePrefixFormat),
                vietnamese: "Kết quả kiểm lúc {0}, đã qua mốc {1} UTC sau đó: ",
                english: "Check result from {0}, the {1} UTC milestone passed after that: ");
            table.Add(nameof(LiveOpsHubStrings.ServicesHealthStaleNoDetail),
                vietnamese: "không còn phát hiện nào ở lần kiểm đó",
                english: "no findings left from that check");
            table.Add(nameof(LiveOpsHubStrings.ServicesHealthDroppedGroupFormat),
                vietnamese: "{0} đợt bị bỏ: {1}",
                english: "{0} dropped: {1}");
            table.Add(nameof(LiveOpsHubStrings.ServicesHealthProgressLostGroupFormat),
                vietnamese: "{0} mất tiến độ: {1}",
                english: "{0} progress loss: {1}");
            table.Add(nameof(LiveOpsHubStrings.ServicesHealthShouldReviewGroupFormat),
                vietnamese: "{0} nên xem: {1}",
                english: "{0} should review: {1}");
            table.Add(nameof(LiveOpsHubStrings.ServicesHealthNotMeasuredGroupFormat),
                vietnamese: "{0} luật chưa kiểm",
                english: "{0} rules not measured");
            table.Add(nameof(LiveOpsHubStrings.ServicesHealthRemoteFindingsFormat),
                vietnamese: "{0} phát hiện về bản remote",
                english: "{0} findings on the remote version");
            table.AddShared(nameof(LiveOpsHubStrings.ServicesHealthItemFormat), "{0} ({1})");
            table.AddShared(nameof(LiveOpsHubStrings.ServicesHealthItemSeparator), ", ");
            table.AddShared(nameof(LiveOpsHubStrings.ServicesHealthGroupSeparator), " · ");
            table.Add(nameof(LiveOpsHubStrings.ServicesHealthColorCollisionBadge),
                vietnamese: "trùng màu",
                english: "color clash");
            table.Add(nameof(LiveOpsHubStrings.ServicesHealthColorCollisionFormat),
                vietnamese: "{0} dùng chung ô màu {1}",
                english: "{0} share color slot {1}");

            table.Add(nameof(LiveOpsHubStrings.ServicesSaveChangesMessageFormat),
                vietnamese: "{0} có {1} thay đổi chưa lưu: {2}. Lưu trước khi đóng LiveOps Hub?",
                english: "{0} has unsaved changes ({1}): {2}. Save before closing LiveOps Hub?");

            table.Add(nameof(LiveOpsHubStrings.ServicesSaveChangesMessageNoCountFormat),
                vietnamese: "{0} còn thay đổi chưa lưu (dấu đã đăng, cảnh báo đã bỏ qua hoặc thứ tự mục). Lưu trước khi đóng LiveOps Hub?",
                english: "{0} still has unsaved changes (published stamp, ignored warnings or item order). Save before closing LiveOps Hub?");
            table.Add(nameof(LiveOpsHubStrings.ServicesSaveChangesMoreFormat),
                vietnamese: "{0} và {1} mục khác",
                english: "{0} and {1} more");
            table.Add(nameof(LiveOpsHubStrings.ServicesSaveChangesDocumentFields),
                vietnamese: "lịch",
                english: "calendar fields");
            table.Add(nameof(LiveOpsHubStrings.ServicesSaveChangesFailedDetail),
                vietnamese: "Thay đổi vẫn còn trong Editor — mở lại LiveOps Hub để lưu hoặc xử lý.",
                english: "The changes are still in the Editor — reopen LiveOps Hub to save or handle them.");
        }
    }
}
