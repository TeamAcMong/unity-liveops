namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng EventTypes (G-EVENTTYPES): màn Loại event 7.2 — bảng loại, footer, inspector loại, dòng trùng màu / ghi đè theo
    /// băm, hàng loại chưa khai báo (V-17), popover Thêm loại, menu xoá loại và câu toast của từng lệnh sửa. Microcopy nguyên văn
    /// theo [SD1 §2.1] và [SD1 §2.2]. Mọi hằng mang tiền tố <c>EventTypes</c> vì lớp <c>partial</c> dùng chung với mọi vùng khác;
    /// câu có số hay có id là chuỗi định dạng <c>{0}</c> để nơi gọi chèn giá trị đã qua <see cref="LiveOpsHubFormat"/>.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Dấu nối dùng lại trong vùng: " · " ghép các phần một câu, ", " ghép danh sách ô trống. Mỗi vùng tự khai dấu nối
        // của mình (như Shell, Findings, Feedback đã làm) để không gói nào phải sửa file vùng của gói khác.
        internal static string EventTypesPartSeparator => LiveOpsHubStringCatalog.Text(nameof(EventTypesPartSeparator));
        internal static string EventTypesListSeparator => LiveOpsHubStringCatalog.Text(nameof(EventTypesListSeparator));

        // Nút hành động của màn (section header).
        internal static string EventTypesAddTypeButton => LiveOpsHubStringCatalog.Text(nameof(EventTypesAddTypeButton));

        // Bảng loại — tiêu đề cột [SD1 §2.1]. Cột 8 (dấu trạng thái) không có tiêu đề nên không có hằng.
        internal static string EventTypesColumnColor => LiveOpsHubStringCatalog.Text(nameof(EventTypesColumnColor));
        internal static string EventTypesColumnTypeId => LiveOpsHubStringCatalog.Text(nameof(EventTypesColumnTypeId));
        internal static string EventTypesColumnDisplayName => LiveOpsHubStringCatalog.Text(nameof(EventTypesColumnDisplayName));
        internal static string EventTypesColumnEntry => LiveOpsHubStringCatalog.Text(nameof(EventTypesColumnEntry));
        internal static string EventTypesColumnConfigKey => LiveOpsHubStringCatalog.Text(nameof(EventTypesColumnConfigKey));
        internal static string EventTypesColumnSource => LiveOpsHubStringCatalog.Text(nameof(EventTypesColumnSource));
        internal static string EventTypesColumnEventCount => LiveOpsHubStringCatalog.Text(nameof(EventTypesColumnEventCount));

        // Giá trị ô của bảng.
        internal static string EventTypesEntrySelfJoin => LiveOpsHubStringCatalog.Text(nameof(EventTypesEntrySelfJoin));
        internal static string EventTypesEntryRequiresJoin => LiveOpsHubStringCatalog.Text(nameof(EventTypesEntryRequiresJoin));
        internal static string EventTypesSourceFixed => LiveOpsHubStringCatalog.Text(nameof(EventTypesSourceFixed));
        internal static string EventTypesSourceRecurring => LiveOpsHubStringCatalog.Text(nameof(EventTypesSourceRecurring));
        /// <summary>Ô "Đợt" của loại sinh từ luật: chữ "luật" opacity 0,7 thay cho con số.</summary>
        internal static string EventTypesEventCountRule => LiveOpsHubStringCatalog.Text(nameof(EventTypesEventCountRule));
        /// <summary>Tên hiển thị của hàng loại chưa khai báo (Hình 10b) — không phải tên người dùng đặt.</summary>
        internal static string EventTypesUndeclaredName => LiveOpsHubStringCatalog.Text(nameof(EventTypesUndeclaredName));

        // Footer bảng.
        internal static string EventTypesFooterFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesFooterFormat));

        // Ô màu: tên tám ô [FD §2.5] và hai cách gọi tên ô ("ô 7" khi chỉ nói số, "ô 6 steel" khi nói cả tên).
        internal static string EventTypesColorSlotNumberFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesColorSlotNumberFormat));
        internal static string EventTypesColorSlotNamedFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesColorSlotNamedFormat));
        internal static string EventTypesColorSlotName0 => LiveOpsHubStringCatalog.Text(nameof(EventTypesColorSlotName0));
        internal static string EventTypesColorSlotName1 => LiveOpsHubStringCatalog.Text(nameof(EventTypesColorSlotName1));
        internal static string EventTypesColorSlotName2 => LiveOpsHubStringCatalog.Text(nameof(EventTypesColorSlotName2));
        internal static string EventTypesColorSlotName3 => LiveOpsHubStringCatalog.Text(nameof(EventTypesColorSlotName3));
        internal static string EventTypesColorSlotName4 => LiveOpsHubStringCatalog.Text(nameof(EventTypesColorSlotName4));
        internal static string EventTypesColorSlotName5 => LiveOpsHubStringCatalog.Text(nameof(EventTypesColorSlotName5));
        internal static string EventTypesColorSlotName6 => LiveOpsHubStringCatalog.Text(nameof(EventTypesColorSlotName6));
        internal static string EventTypesColorSlotName7 => LiveOpsHubStringCatalog.Text(nameof(EventTypesColorSlotName7));
        internal static string EventTypesColorSlotTooltipFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesColorSlotTooltipFormat));

        // Dòng phụ dưới dãy ô màu: đã ghi đè (quiet, không dấu) hay còn trùng (Warning + gợi ý ô trống, PD Q1).
        internal static string EventTypesHashOverrideFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesHashOverrideFormat));
        internal static string EventTypesColorCollisionFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesColorCollisionFormat));
        internal static string EventTypesColorCollisionNoFreeSlotFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesColorCollisionNoFreeSlotFormat));
        internal static string EventTypesResetColorButton => LiveOpsHubStringCatalog.Text(nameof(EventTypesResetColorButton));

        // Inspector loại.
        internal static string EventTypesInspectorTypeIdLabel => LiveOpsHubStringCatalog.Text(nameof(EventTypesInspectorTypeIdLabel));
        internal static string EventTypesInspectorTypeIdLockedTooltip => LiveOpsHubStringCatalog.Text(nameof(EventTypesInspectorTypeIdLockedTooltip));
        internal static string EventTypesInspectorDisplayNameLabel => LiveOpsHubStringCatalog.Text(nameof(EventTypesInspectorDisplayNameLabel));
        internal static string EventTypesInspectorColorLabel => LiveOpsHubStringCatalog.Text(nameof(EventTypesInspectorColorLabel));
        internal static string EventTypesInspectorRequiresJoinLabel => LiveOpsHubStringCatalog.Text(nameof(EventTypesInspectorRequiresJoinLabel));
        internal static string EventTypesInspectorConfigKeyLabel => LiveOpsHubStringCatalog.Text(nameof(EventTypesInspectorConfigKeyLabel));
        internal static string EventTypesInspectorConfigKeyTooltip => LiveOpsHubStringCatalog.Text(nameof(EventTypesInspectorConfigKeyTooltip));
        internal static string EventTypesInspectorConfigKeySubText => LiveOpsHubStringCatalog.Text(nameof(EventTypesInspectorConfigKeySubText));
        internal static string EventTypesInspectorNoSelection => LiveOpsHubStringCatalog.Text(nameof(EventTypesInspectorNoSelection));

        // Card "Dùng ở đâu".
        internal static string EventTypesUsageCardTitle => LiveOpsHubStringCatalog.Text(nameof(EventTypesUsageCardTitle));
        internal static string EventTypesUsageFixedCountFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesUsageFixedCountFormat));
        internal static string EventTypesUsageRecurringRule => LiveOpsHubStringCatalog.Text(nameof(EventTypesUsageRecurringRule));
        internal static string EventTypesUsageNextFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesUsageNextFormat));
        internal static string EventTypesUsageNone => LiveOpsHubStringCatalog.Text(nameof(EventTypesUsageNone));
        internal static string EventTypesOpenInCalendarButton => LiveOpsHubStringCatalog.Text(nameof(EventTypesOpenInCalendarButton));

        // Loại chưa khai báo (V-17, Hình 10b).
        internal static string EventTypesUnknownTypeInDraftFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesUnknownTypeInDraftFormat));
        internal static string EventTypesUnknownTypeInRemoteFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesUnknownTypeInRemoteFormat));
        internal static string EventTypesDeclareButton => LiveOpsHubStringCatalog.Text(nameof(EventTypesDeclareButton));
        /// <summary>
        /// (V-22 CC-FT-2 (a)) Tiền tố card tham chiếu của màn này. Nguồn câu (<see cref="LiveOpsFindingText.Headline"/>) KHÔNG
        /// mang tiền tố vì ở màn Kiểm lịch nói "Kiểm lịch ·" là thừa; chỉ màn Loại event ghép thêm để nói câu này đến từ đâu.
        /// </summary>
        internal static string EventTypesReferencePrefix => LiveOpsHubStringCatalog.Text(nameof(EventTypesReferencePrefix));

        // Menu ⋮ của loại.
        internal static string EventTypesDeleteMenuItem => LiveOpsHubStringCatalog.Text(nameof(EventTypesDeleteMenuItem));
        internal static string EventTypesDeleteMenuItemInUseFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesDeleteMenuItemInUseFormat));
        internal static string EventTypesDeleteInUseReasonFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesDeleteInUseReasonFormat));
        internal static string EventTypesTypeIdLockedReasonFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesTypeIdLockedReasonFormat));

        // Popover Thêm loại.
        internal static string EventTypesAddPopoverTitle => LiveOpsHubStringCatalog.Text(nameof(EventTypesAddPopoverTitle));
        internal static string EventTypesAddPopoverIdLabel => LiveOpsHubStringCatalog.Text(nameof(EventTypesAddPopoverIdLabel));
        internal static string EventTypesAddPopoverNameLabel => LiveOpsHubStringCatalog.Text(nameof(EventTypesAddPopoverNameLabel));
        internal static string EventTypesAddPopoverConfirmButton => LiveOpsHubStringCatalog.Text(nameof(EventTypesAddPopoverConfirmButton));
        internal static string EventTypesAddPopoverCancelButton => LiveOpsHubStringCatalog.Text(nameof(EventTypesAddPopoverCancelButton));
        internal static string EventTypesAddPopoverIdEmptyReason => LiveOpsHubStringCatalog.Text(nameof(EventTypesAddPopoverIdEmptyReason));
        internal static string EventTypesAddPopoverIdCharsetReason => LiveOpsHubStringCatalog.Text(nameof(EventTypesAddPopoverIdCharsetReason));
        internal static string EventTypesAddPopoverIdDuplicateReason => LiveOpsHubStringCatalog.Text(nameof(EventTypesAddPopoverIdDuplicateReason));

        // Trạng thái trống.
        internal static string EventTypesEmptyNoTypeTitle => LiveOpsHubStringCatalog.Text(nameof(EventTypesEmptyNoTypeTitle));
        internal static string EventTypesEmptyNoTypeBody => LiveOpsHubStringCatalog.Text(nameof(EventTypesEmptyNoTypeBody));
        internal static string EventTypesEmptyNoAssetTitle => LiveOpsHubStringCatalog.Text(nameof(EventTypesEmptyNoAssetTitle));
        internal static string EventTypesEmptyNoAssetButton => LiveOpsHubStringCatalog.Text(nameof(EventTypesEmptyNoAssetButton));

        // Toast = tên Undo group của chính lệnh sửa (4.3): một câu cho cả hai chỗ, không bao giờ lệch nhau.
        internal static string EventTypesToastAddedFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesToastAddedFormat));
        internal static string EventTypesToastDeclaredFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesToastDeclaredFormat));
        internal static string EventTypesToastRemovedFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesToastRemovedFormat));
        internal static string EventTypesToastColorChangedFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesToastColorChangedFormat));
        internal static string EventTypesToastDisplayNameChangedFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesToastDisplayNameChangedFormat));
        internal static string EventTypesToastRequiresJoinOnFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesToastRequiresJoinOnFormat));
        internal static string EventTypesToastRequiresJoinOffFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesToastRequiresJoinOffFormat));
        internal static string EventTypesToastConfigKeyChangedFormat => LiveOpsHubStringCatalog.Text(nameof(EventTypesToastConfigKeyChangedFormat));
    }
}
