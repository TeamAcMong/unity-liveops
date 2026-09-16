namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ của phiên lịch và composition root (vùng Services, G-SESSION): tên Undo group, câu thất bại của lệnh sửa, outcome khi
    /// asset bị xoá, lý do health theo đích (6.4) và câu hỏi lưu khi đóng cửa sổ. Mọi hằng mang tiền tố <c>Services</c> vì lớp
    /// <c>partial</c> dùng chung với mọi vùng khác.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Undo group (4.3): tên group = câu người dùng đọc trong Edit → Undo History, nên nói việc vừa làm chứ không nói tên hàm.
        internal const string ServicesUndoCreateCalendar = "Tạo lịch LiveOps";
        internal const string ServicesUndoMarkPublishedFormat = "Ghi dấu đã đăng sha {0}";
        internal const string ServicesUndoRemoveLatestStampFormat = "Gỡ dấu đã đăng sha {0}";
        internal const string ServicesUndoKeepEditorVersion = "Giữ bản trong Editor thay bản trên đĩa";
        internal const string ServicesUndoDiscardUnsaved = "Bỏ thay đổi chưa lưu";

        // Lệnh sửa không áp được — câu nói vì sao, để màn hiện cạnh thao tác thay vì im lặng không làm gì.
        internal const string ServicesEditFailedNoAsset = "Chưa có asset lịch — tạo hoặc chọn asset ở Tổng quan trước.";
        internal const string ServicesEditFailedNotApplicable = "Lệnh sửa không áp được lên lịch hiện tại (mục cần sửa không còn).";
        internal const string ServicesEditFailedContinuousEditOpen = "Đang kéo một mục — thả chuột hoặc Esc trước khi sửa việc khác.";
        internal const string ServicesEditFailedContinuousGroupMismatch = "Thao tác kéo này đã kết thúc hoặc không phải thao tác đang mở.";
        internal const string ServicesEditFailedNoChange = "Không có thay đổi để ghi.";
        internal const string ServicesSaveFailedFormat = "Không lưu được {0} — asset vẫn còn thay đổi chưa lưu.";
        internal const string ServicesSaveFailedNoPath = "Asset lịch này chỉ nằm trong bộ nhớ (không có file) nên không lưu được.";
        internal const string ServicesStampNoPublisher = "không rõ";

        // Asset bị xoá khỏi project khi hub đang mở (4.3).
        internal const string ServicesAssetDeletedHeadlineFormat = "{0} đã bị xoá khỏi project";
        internal const string ServicesAssetDeletedDetail = "Hub về trạng thái chưa có lịch — tạo hoặc chọn asset khác ở Tổng quan.";

        // INTERIM(G-SHELLPOLISH): câu log tạm thay băng "asset đổi trên đĩa" (mục 12 I-8) — G-SHELLPOLISH xoá cùng dòng nối log ở cửa sổ.
        internal const string InterimDiskConflictLog = "LiveOps Hub: {0} vừa đổi trên đĩa trong lúc hub còn thay đổi chưa lưu — nháp vẫn giữ trong phiên (Tải lại / Giữ bản trong Editor chưa có ở bản dev này).";

        // Health theo đích (6.4). "Chưa kiểm" luôn kèm cách làm cho hết chưa kiểm (F5), vì vòng rỗng không có câu là vô dụng.
        internal const string ServicesHealthNoAsset = "Chưa có asset lịch";
        internal const string ServicesHealthNeverChecked = "Chưa kiểm lần nào — F5 để kiểm";
        internal const string ServicesHealthRunningFormat = "Đang kiểm {0}/{1} luật…";
        internal const string ServicesHealthInterruptedByReload = "Lần kiểm bị cắt ngang khi Unity nạp lại script — F5 để kiểm lại";
        internal const string ServicesHealthStaleEditedPrefixFormat = "Kết quả kiểm lúc {0}, lịch đã đổi sau đó: ";
        internal const string ServicesHealthStaleMilestonePrefixFormat = "Kết quả kiểm lúc {0}, đã qua mốc {1} UTC sau đó: ";
        internal const string ServicesHealthStaleNoDetail = "không còn phát hiện nào ở lần kiểm đó";
        internal const string ServicesHealthDroppedGroupFormat = "{0} đợt bị bỏ: {1}";
        internal const string ServicesHealthProgressLostGroupFormat = "{0} mất tiến độ: {1}";
        internal const string ServicesHealthShouldReviewGroupFormat = "{0} nên xem: {1}";
        internal const string ServicesHealthNotMeasuredGroupFormat = "{0} luật chưa kiểm";
        internal const string ServicesHealthRemoteFindingsFormat = "{0} phát hiện về bản remote";
        internal const string ServicesHealthItemFormat = "{0} ({1})";
        internal const string ServicesHealthItemSeparator = ", ";
        internal const string ServicesHealthGroupSeparator = " · ";
        internal const string ServicesHealthColorCollisionBadge = "trùng màu";
        internal const string ServicesHealthColorCollisionFormat = "{0} dùng chung ô màu {1}";

        // Đóng cửa sổ khi còn thay đổi chưa lưu (8.3): tên asset + số thay đổi + danh sách id ngắn.
        internal const string ServicesSaveChangesMessageFormat = "{0} có {1} thay đổi chưa lưu: {2}. Lưu trước khi đóng LiveOps Hub?";
        internal const string ServicesSaveChangesMoreFormat = "{0} và {1} mục khác";
        internal const string ServicesSaveChangesDocumentFields = "lịch";
    }
}
