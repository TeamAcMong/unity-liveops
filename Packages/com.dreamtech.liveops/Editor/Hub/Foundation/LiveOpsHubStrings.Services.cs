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
        internal static string ServicesUndoCreateCalendar => LiveOpsHubStringCatalog.Text(nameof(ServicesUndoCreateCalendar));
        internal static string ServicesUndoMarkPublishedFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesUndoMarkPublishedFormat));
        internal static string ServicesUndoRemoveLatestStampFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesUndoRemoveLatestStampFormat));
        internal static string ServicesUndoKeepEditorVersion => LiveOpsHubStringCatalog.Text(nameof(ServicesUndoKeepEditorVersion));
        internal static string ServicesUndoDiscardUnsaved => LiveOpsHubStringCatalog.Text(nameof(ServicesUndoDiscardUnsaved));

        // Lệnh sửa không áp được — câu nói vì sao, để màn hiện cạnh thao tác thay vì im lặng không làm gì.
        internal static string ServicesEditFailedNoAsset => LiveOpsHubStringCatalog.Text(nameof(ServicesEditFailedNoAsset));
        internal static string ServicesEditFailedNotApplicable => LiveOpsHubStringCatalog.Text(nameof(ServicesEditFailedNotApplicable));
        internal static string ServicesEditFailedContinuousEditOpen => LiveOpsHubStringCatalog.Text(nameof(ServicesEditFailedContinuousEditOpen));
        internal static string ServicesEditFailedContinuousGroupMismatch => LiveOpsHubStringCatalog.Text(nameof(ServicesEditFailedContinuousGroupMismatch));
        internal static string ServicesEditFailedNoChange => LiveOpsHubStringCatalog.Text(nameof(ServicesEditFailedNoChange));
        internal static string ServicesSaveFailedFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesSaveFailedFormat));
        internal static string ServicesSaveFailedNoPath => LiveOpsHubStringCatalog.Text(nameof(ServicesSaveFailedNoPath));
        internal static string ServicesSaveFailedDiskConflictFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesSaveFailedDiskConflictFormat));
        internal static string ServicesStampNoPublisher => LiveOpsHubStringCatalog.Text(nameof(ServicesStampNoPublisher));

        // Asset bị xoá khỏi project khi hub đang mở (4.3).
        internal static string ServicesAssetDeletedHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesAssetDeletedHeadlineFormat));
        internal static string ServicesAssetDeletedDetail => LiveOpsHubStringCatalog.Text(nameof(ServicesAssetDeletedDetail));

        // Health theo đích (6.4). "Chưa kiểm" luôn kèm cách làm cho hết chưa kiểm (F5), vì vòng rỗng không có câu là vô dụng.
        internal static string ServicesHealthNoAsset => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthNoAsset));
        internal static string ServicesHealthNeverChecked => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthNeverChecked));
        internal static string ServicesHealthRunningFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthRunningFormat));
        internal static string ServicesHealthInterruptedByReload => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthInterruptedByReload));
        internal static string ServicesHealthStaleEditedPrefixFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthStaleEditedPrefixFormat));
        internal static string ServicesHealthStaleMilestonePrefixFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthStaleMilestonePrefixFormat));
        internal static string ServicesHealthStaleNoDetail => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthStaleNoDetail));
        internal static string ServicesHealthDroppedGroupFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthDroppedGroupFormat));
        internal static string ServicesHealthProgressLostGroupFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthProgressLostGroupFormat));
        internal static string ServicesHealthShouldReviewGroupFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthShouldReviewGroupFormat));
        internal static string ServicesHealthNotMeasuredGroupFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthNotMeasuredGroupFormat));
        internal static string ServicesHealthRemoteFindingsFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthRemoteFindingsFormat));
        internal static string ServicesHealthItemFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthItemFormat));
        internal static string ServicesHealthItemSeparator => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthItemSeparator));
        internal static string ServicesHealthGroupSeparator => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthGroupSeparator));
        internal static string ServicesHealthColorCollisionBadge => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthColorCollisionBadge));
        internal static string ServicesHealthColorCollisionFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesHealthColorCollisionFormat));

        // Đóng cửa sổ khi còn thay đổi chưa lưu (8.3): tên asset + số thay đổi + danh sách id ngắn.
        internal static string ServicesSaveChangesMessageFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesSaveChangesMessageFormat));
        // Diff hậu quả rỗng mà file vẫn khác bản đã lưu (PD-22: dấu đã đăng, cảnh báo đã bỏ qua, thứ tự mục) — không bịa ra con số.
        internal static string ServicesSaveChangesMessageNoCountFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesSaveChangesMessageNoCountFormat));
        internal static string ServicesSaveChangesMoreFormat => LiveOpsHubStringCatalog.Text(nameof(ServicesSaveChangesMoreFormat));
        internal static string ServicesSaveChangesDocumentFields => LiveOpsHubStringCatalog.Text(nameof(ServicesSaveChangesDocumentFields));
        internal static string ServicesSaveChangesFailedDetail => LiveOpsHubStringCatalog.Text(nameof(ServicesSaveChangesFailedDetail));
    }
}
