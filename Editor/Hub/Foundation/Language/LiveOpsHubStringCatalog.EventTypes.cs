namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng EventTypes — bản tiếng Việt là bản gốc (chép nguyên văn microcopy [SD1 §2.1], [SD1 §2.2]), bản tiếng Anh dịch
    /// cùng chỗ để chỗ giữ chỗ <c>{0}</c>/<c>{1}</c> soát được bằng mắt và không bao giờ thêm khoá ở bản này mà quên bản kia.
    /// Comment "vì sao" của từng câu ở lại <c>LiveOpsHubStrings.EventTypes.cs</c> cạnh property — đó là chỗ người đọc code tìm tới.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterEventTypes(LiveOpsHubStringTable table)
        {
            table.AddShared(nameof(LiveOpsHubStrings.EventTypesPartSeparator), " · ");
            table.AddShared(nameof(LiveOpsHubStrings.EventTypesListSeparator), ", ");

            table.Add(nameof(LiveOpsHubStrings.EventTypesAddTypeButton),
                vietnamese: "Thêm loại",
                english: "Add type");

            table.Add(nameof(LiveOpsHubStrings.EventTypesColumnColor),
                vietnamese: "Màu",
                english: "Color");
            table.Add(nameof(LiveOpsHubStrings.EventTypesColumnTypeId),
                vietnamese: "Id loại",
                english: "Type id");
            table.Add(nameof(LiveOpsHubStrings.EventTypesColumnDisplayName),
                vietnamese: "Tên hiển thị",
                english: "Display name");
            table.Add(nameof(LiveOpsHubStrings.EventTypesColumnEntry),
                vietnamese: "Cách vào",
                english: "How players join");
            table.Add(nameof(LiveOpsHubStrings.EventTypesColumnConfigKey),
                vietnamese: "Config key",
                english: "Config key");
            table.Add(nameof(LiveOpsHubStrings.EventTypesColumnSource),
                vietnamese: "Nguồn",
                english: "Source");
            table.Add(nameof(LiveOpsHubStrings.EventTypesColumnEventCount),
                vietnamese: "Đợt",
                english: "Events");

            table.Add(nameof(LiveOpsHubStrings.EventTypesEntrySelfJoin),
                vietnamese: "Tự vào",
                english: "Joins automatically");
            table.Add(nameof(LiveOpsHubStrings.EventTypesEntryRequiresJoin),
                vietnamese: "Phải bấm tham gia",
                english: "Must tap to join");
            table.Add(nameof(LiveOpsHubStrings.EventTypesSourceFixed),
                vietnamese: "Cố định",
                english: "Fixed");
            table.Add(nameof(LiveOpsHubStrings.EventTypesSourceRecurring),
                vietnamese: "Luật lặp",
                english: "Recurring rule");
            table.Add(nameof(LiveOpsHubStrings.EventTypesEventCountRule),
                vietnamese: "luật",
                english: "rule");
            table.Add(nameof(LiveOpsHubStrings.EventTypesUndeclaredName),
                vietnamese: "chưa khai báo",
                english: "not declared");

            table.Add(nameof(LiveOpsHubStrings.EventTypesFooterFormat),
                vietnamese: "{0} loại · config key của loại được ghi vào mọi đợt và mục recurring không tự khai khi Xuất JSON",
                english: "{0} {0|type|types} · the type's config key is written into every event and recurring entry that declares none when exporting JSON");

            table.Add(nameof(LiveOpsHubStrings.EventTypesColorSlotNumberFormat),
                vietnamese: "ô {0}",
                english: "slot {0}");
            table.Add(nameof(LiveOpsHubStrings.EventTypesColorSlotNamedFormat),
                vietnamese: "ô {0} {1}",
                english: "slot {0} {1}");
            // Tên tám ô màu [FD §2.5] là tên riêng của bảng màu — giữ nguyên ở cả hai ngôn ngữ để ảnh chụp và tài liệu thiết kế
            // gọi cùng một tên.
            table.AddShared(nameof(LiveOpsHubStrings.EventTypesColorSlotName0), "azure");
            table.AddShared(nameof(LiveOpsHubStrings.EventTypesColorSlotName1), "cyan");
            table.AddShared(nameof(LiveOpsHubStrings.EventTypesColorSlotName2), "teal");
            table.AddShared(nameof(LiveOpsHubStrings.EventTypesColorSlotName3), "indigo");
            table.AddShared(nameof(LiveOpsHubStrings.EventTypesColorSlotName4), "violet");
            table.AddShared(nameof(LiveOpsHubStrings.EventTypesColorSlotName5), "magenta");
            table.AddShared(nameof(LiveOpsHubStrings.EventTypesColorSlotName6), "steel");
            table.AddShared(nameof(LiveOpsHubStrings.EventTypesColorSlotName7), "mauve");
            table.Add(nameof(LiveOpsHubStrings.EventTypesColorSlotTooltipFormat),
                vietnamese: "Chọn {0} cho loại này",
                english: "Use {0} for this type");

            table.Add(nameof(LiveOpsHubStrings.EventTypesHashOverrideFormat),
                vietnamese: "Theo băm trùng {0} ({1}) → đang ghi đè {2}",
                english: "The hash collides with {0} ({1}) → overridden to {2}");
            table.Add(nameof(LiveOpsHubStrings.EventTypesColorCollisionFormat),
                vietnamese: "Trùng {0} với {1} — chọn ô trống: {2}",
                english: "Shares {0} with {1} — pick a free slot: {2}");
            table.Add(nameof(LiveOpsHubStrings.EventTypesColorCollisionNoFreeSlotFormat),
                vietnamese: "Trùng {0} với {1} — cả 8 ô đều đã có loại dùng",
                english: "Shares {0} with {1} — every one of the 8 slots is already taken");
            table.Add(nameof(LiveOpsHubStrings.EventTypesResetColorButton),
                vietnamese: "Về màu theo băm",
                english: "Back to the hashed color");

            table.Add(nameof(LiveOpsHubStrings.EventTypesInspectorTypeIdLabel),
                vietnamese: "Id loại",
                english: "Type id");
            table.Add(nameof(LiveOpsHubStrings.EventTypesInspectorTypeIdLockedTooltip),
                vietnamese: "Đổi id loại làm đổi id mọi đợt của nó — tạo loại mới rồi chuyển đợt",
                english: "Changing a type id changes the id of every event of that type — create a new type and move the events instead");
            table.Add(nameof(LiveOpsHubStrings.EventTypesInspectorDisplayNameLabel),
                vietnamese: "Tên hiển thị",
                english: "Display name");
            table.Add(nameof(LiveOpsHubStrings.EventTypesInspectorColorLabel),
                vietnamese: "Màu",
                english: "Color");
            table.Add(nameof(LiveOpsHubStrings.EventTypesInspectorRequiresJoinLabel),
                vietnamese: "Phải bấm tham gia",
                english: "Must tap to join");
            table.Add(nameof(LiveOpsHubStrings.EventTypesInspectorConfigKeyLabel),
                vietnamese: "Config key",
                english: "Config key");
            table.Add(nameof(LiveOpsHubStrings.EventTypesInspectorConfigKeyTooltip),
                vietnamese: "Config key mặc định: Xuất JSON ghi giá trị này vào mọi đợt của loại không tự khai configKey",
                english: "Default config key: exporting JSON writes this value into every event of the type that declares no configKey");
            table.Add(nameof(LiveOpsHubStrings.EventTypesInspectorConfigKeySubText),
                vietnamese: "mặc định của loại, ghi vào đợt không tự khai",
                english: "the type's default, written into events that declare none");
            table.Add(nameof(LiveOpsHubStrings.EventTypesInspectorNoSelection),
                vietnamese: "Chọn một loại trong bảng để sửa màu, cách vào và config key",
                english: "Pick a type in the table to edit its color, join mode and config key");

            table.Add(nameof(LiveOpsHubStrings.EventTypesUsageCardTitle),
                vietnamese: "Dùng ở đâu",
                english: "Where it is used");
            table.Add(nameof(LiveOpsHubStrings.EventTypesUsageFixedCountFormat),
                vietnamese: "{0} đợt cố định",
                english: "{0} fixed {0|event|events}");
            table.Add(nameof(LiveOpsHubStrings.EventTypesUsageRecurringRule),
                vietnamese: "1 luật lặp",
                english: "1 recurring rule");
            table.Add(nameof(LiveOpsHubStrings.EventTypesUsageNextFormat),
                vietnamese: "đợt tới {0}",
                english: "next event {0}");
            table.Add(nameof(LiveOpsHubStrings.EventTypesUsageNone),
                vietnamese: "Chưa có đợt hay luật nào dùng loại này",
                english: "No event or rule uses this type yet");
            table.Add(nameof(LiveOpsHubStrings.EventTypesOpenInCalendarButton),
                vietnamese: "Xem trên lịch",
                english: "Show on the calendar");

            table.Add(nameof(LiveOpsHubStrings.EventTypesUnknownTypeInDraftFormat),
                vietnamese: "{0}: có {1} mục trong lịch nháp nhưng chưa có loại — game sẽ bỏ",
                english: "{0}: {1} {1|entry|entries} in the draft calendar but no such type — the game will drop them");
            // (V-8) Một câu người dùng đọc = một chữ. Câu này trùng TỪNG CHỮ với FindingUnknownTypeRemoteConsequenceFormat
            // (vùng Findings, có trước gói này) nhưng phải khai riêng: dòng dưới bảng dựng từ tài liệu + bản dán và phải đúng
            // NGAY CẢ KHI chưa kiểm lần nào, lúc đó không có LiveEventCalendarFinding nào để gọi LiveOpsFindingText. Bản tiếng
            // Anh lấy đúng nguyên văn của vùng Findings; test EventTypesModelTests.UnknownTypeInRemote_TextMatchesFindingText
            // khoá hai khoá bằng nhau ở CẢ HAI ngôn ngữ để sửa một bên là bên kia đỏ.
            table.Add(nameof(LiveOpsHubStrings.EventTypesUnknownTypeInRemoteFormat),
                vietnamese: "{0}: có {1} đợt trong JSON đã dán nhưng chưa có loại — game sẽ bỏ",
                english: "{0}: {1} {1|event|events} in the pasted JSON have no declared type — the game will drop them");
            table.Add(nameof(LiveOpsHubStrings.EventTypesDeclareButton),
                vietnamese: "Khai báo",
                english: "Declare");
            table.Add(nameof(LiveOpsHubStrings.EventTypesReferencePrefix),
                vietnamese: "Kiểm lịch · ",
                english: "Calendar check · ");

            table.Add(nameof(LiveOpsHubStrings.EventTypesDeleteMenuItem),
                vietnamese: "Xoá loại",
                english: "Delete type");
            table.Add(nameof(LiveOpsHubStrings.EventTypesDeleteMenuItemInUseFormat),
                vietnamese: "Xoá loại (còn {0} đợt)",
                english: "Delete type ({0} {0|event|events} left)");
            table.Add(nameof(LiveOpsHubStrings.EventTypesDeleteInUseReasonFormat),
                vietnamese: "Còn {0} đợt hoặc luật dùng loại này — chuyển hoặc xoá chúng trước",
                english: "{0} {0|event or rule still uses|events or rules still use} this type — move or delete them first");
            table.Add(nameof(LiveOpsHubStrings.EventTypesTypeIdLockedReasonFormat),
                vietnamese: "Loại đang có {0} đợt hoặc luật — id khoá để không đổi id của chúng",
                english: "The type has {0} {0|event or rule|events or rules} — the id is locked so theirs do not change");

            table.Add(nameof(LiveOpsHubStrings.EventTypesAddPopoverTitle),
                vietnamese: "Thêm loại",
                english: "Add type");
            table.Add(nameof(LiveOpsHubStrings.EventTypesAddPopoverIdLabel),
                vietnamese: "Id loại",
                english: "Type id");
            table.Add(nameof(LiveOpsHubStrings.EventTypesAddPopoverNameLabel),
                vietnamese: "Tên hiển thị",
                english: "Display name");
            table.Add(nameof(LiveOpsHubStrings.EventTypesAddPopoverConfirmButton),
                vietnamese: "Thêm loại",
                english: "Add type");
            table.Add(nameof(LiveOpsHubStrings.EventTypesAddPopoverCancelButton),
                vietnamese: "Huỷ",
                english: "Cancel");
            table.Add(nameof(LiveOpsHubStrings.EventTypesAddPopoverIdEmptyReason),
                vietnamese: "Nhập id loại trước",
                english: "Enter a type id first");
            table.Add(nameof(LiveOpsHubStrings.EventTypesAddPopoverIdCharsetReason),
                vietnamese: "Id loại không được chứa '#' hay xuống dòng",
                english: "A type id cannot contain '#' or a line break");
            table.Add(nameof(LiveOpsHubStrings.EventTypesAddPopoverIdDuplicateReason),
                vietnamese: "Lịch đã có loại mang id này",
                english: "The calendar already has a type with this id");

            table.Add(nameof(LiveOpsHubStrings.EventTypesEmptyNoTypeTitle),
                vietnamese: "Chưa có loại event nào",
                english: "No event type yet");
            table.Add(nameof(LiveOpsHubStrings.EventTypesEmptyNoTypeBody),
                vietnamese: "Lịch cần ít nhất một loại để có làn; game đăng ký loại qua WithEventTypesFrom.",
                english: "A calendar needs at least one type to have a lane; the game registers types through WithEventTypesFrom.");
            table.Add(nameof(LiveOpsHubStrings.EventTypesEmptyNoAssetTitle),
                vietnamese: "Chưa có lịch LiveOps trong project",
                english: "No LiveOps calendar in the project");
            table.Add(nameof(LiveOpsHubStrings.EventTypesEmptyNoAssetButton),
                vietnamese: "Mở Tổng quan để tạo lịch",
                english: "Open Overview to create a calendar");

            table.Add(nameof(LiveOpsHubStrings.EventTypesToastAddedFormat),
                vietnamese: "Đã thêm loại {0}",
                english: "Added type {0}");
            table.Add(nameof(LiveOpsHubStrings.EventTypesToastDeclaredFormat),
                vietnamese: "Đã khai báo loại {0}",
                english: "Declared type {0}");
            table.Add(nameof(LiveOpsHubStrings.EventTypesToastRemovedFormat),
                vietnamese: "Đã xoá loại {0}",
                english: "Deleted type {0}");
            table.Add(nameof(LiveOpsHubStrings.EventTypesToastColorChangedFormat),
                vietnamese: "Đã đổi màu {0} sang {1}",
                english: "Changed the color of {0} to {1}");
            table.Add(nameof(LiveOpsHubStrings.EventTypesToastTypeIdChangedFormat),
                vietnamese: "Đã đổi id loại {0} thành {1}",
                english: "Changed type id {0} to {1}");
            table.Add(nameof(LiveOpsHubStrings.EventTypesToastDisplayNameChangedFormat),
                vietnamese: "Đã đổi tên hiển thị của {0}",
                english: "Changed the display name of {0}");
            table.Add(nameof(LiveOpsHubStrings.EventTypesToastRequiresJoinOnFormat),
                vietnamese: "Đã bật phải bấm tham gia cho {0}",
                english: "Turned on must-tap-to-join for {0}");
            table.Add(nameof(LiveOpsHubStrings.EventTypesToastRequiresJoinOffFormat),
                vietnamese: "Đã tắt phải bấm tham gia cho {0}",
                english: "Turned off must-tap-to-join for {0}");
            table.Add(nameof(LiveOpsHubStrings.EventTypesToastConfigKeyChangedFormat),
                vietnamese: "Đã đổi config key mặc định của {0}",
                english: "Changed the default config key of {0}");
        }
    }
}
