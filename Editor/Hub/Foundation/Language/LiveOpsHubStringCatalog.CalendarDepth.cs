namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng CalendarDepth (chiều sâu màn Lịch) — bản tiếng Việt chép nguyên văn từ thiết kế [SD1 §3.1, §3.4, §3.8,
    /// §3.12] và [FD §3.9], bản tiếng Anh dịch cùng lúc (G-I18N §4). Comment "vì sao" của từng câu ở
    /// <c>LiveOpsHubStrings.CalendarDepth.cs</c>.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterCalendarDepth(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthListToggle),
                vietnamese: "Danh sách",
                english: "List");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthCompareToggleFormat),
                vietnamese: "So với đã đăng ({0})",
                english: "Vs published ({0})");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthCompareUnavailableReason),
                vietnamese: "Chưa có dấu đã đăng",
                english: "No published stamp yet");

            table.Add(nameof(LiveOpsHubStrings.CalendarDepthOverflowMenuTooltip),
                vietnamese: "Lệnh khác của thanh công cụ",
                english: "More toolbar commands");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthLegendToggle),
                vietnamese: "Chú giải",
                english: "Legend");

            table.Add(nameof(LiveOpsHubStrings.CalendarDepthListColumnId),
                vietnamese: "id",
                english: "id");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthListColumnType),
                vietnamese: "loại",
                english: "type");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthListColumnStart),
                vietnamese: "bắt đầu",
                english: "start");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthListColumnEnd),
                vietnamese: "kết thúc",
                english: "end");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthListColumnState),
                vietnamese: "trạng thái",
                english: "state");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthListEmpty),
                vietnamese: "Lịch này chưa có đợt cố định nào",
                english: "This calendar has no fixed events yet");

            table.Add(nameof(LiveOpsHubStrings.CalendarDepthComparePublishedTitleFormat),
                vietnamese: "Thay đổi từ lần đăng {0}",
                english: "Changes since the {0} publish");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthCompareDiskTitleFormat),
                vietnamese: "So với bản trên đĩa · đổi lúc {0}",
                english: "Vs the version on disk · changed at {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthCompareEmpty),
                vietnamese: "Không có thay đổi nào so với bản này",
                english: "Nothing differs from this version");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthCompareNote),
                vietnamese: "Bấm một dòng để chọn và căn khung. Chuột phải: Hoàn về bản đã đăng.",
                english: "Click a row to select it and frame it. Right-click: Revert to the published version.");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthCompareDiskNote),
                vietnamese: "Bấm một dòng để chọn và căn khung. Chuột phải: Lấy bản trên đĩa cho mục này.",
                english: "Click a row to select it and frame it. Right-click: Take the version on disk for this entry.");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthRevertToPublished),
                vietnamese: "Hoàn về bản đã đăng",
                english: "Revert to the published version");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthTakeFromDisk),
                vietnamese: "Lấy bản trên đĩa cho mục này",
                english: "Take the version on disk for this entry");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthRevertToastFormat),
                vietnamese: "Đã hoàn {0} về bản đã đăng",
                english: "Reverted {0} to the published version");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthTakeFromDiskToastFormat),
                vietnamese: "Đã lấy {0} từ bản trên đĩa",
                english: "Took {0} from the version on disk");

            table.Add(nameof(LiveOpsHubStrings.CalendarDepthHoverStartLabel),
                vietnamese: "Bắt đầu",
                english: "Start");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthHoverEndLabel),
                vietnamese: "Kết thúc",
                english: "End");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthHoverUtcWithDeviceFormat),
                vietnamese: "{0} UTC ({1})",
                english: "{0} UTC ({1})");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthHoverEndValueFormat),
                vietnamese: "{0} UTC · dài {1}",
                english: "{0} UTC · {1} long");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthHoverFooter),
                vietnamese: "Kéo để dời · kéo mép để đổi giờ · F8 lỗi kế tiếp",
                english: "Drag to move · drag an edge to change times · F8 for the next problem");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthHoverQuickFixButton),
                vietnamese: "Sửa nhanh…",
                english: "Quick fix…");
            table.AddShared(nameof(LiveOpsHubStrings.CalendarDepthHoverCounterFormat), "{0}/{1}");

            table.Add(nameof(LiveOpsHubStrings.CalendarDepthDrawerCloseTooltip),
                vietnamese: "Đóng (Esc)",
                english: "Close (Esc)");

            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuEditInInspector),
                vietnamese: "Sửa trong inspector",
                english: "Edit in the inspector");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuDuplicateFormat),
                vietnamese: "Nhân bản sang {0} (+{1})…",
                english: "Duplicate to {0} (+{1})…");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuDuplicateDisabled),
                vietnamese: "Nhân bản…",
                english: "Duplicate…");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuDuplicateUnreadableStartReason),
                vietnamese: "giờ bắt đầu không đọc được",
                english: "the start time cannot be read");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuFrame),
                vietnamese: "Căn khung",
                english: "Frame");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuMoveStart),
                vietnamese: "Dời bắt đầu tới…",
                english: "Move the start to…");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuSetDuration),
                vietnamese: "Đặt thời lượng…",
                english: "Set the duration…");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuCopyId),
                vietnamese: "Copy id",
                english: "Copy the id");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuCopyEventJson),
                vietnamese: "Copy đợt (JSON)",
                english: "Copy the event (JSON)");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuDelete),
                vietnamese: "Xoá đợt…",
                english: "Delete the event…");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuOpenRuleFormat),
                vietnamese: "Mở luật {0}",
                english: "Open the {0} rule");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuRecurringReadOnly),
                vietnamese: "Không sửa được: đợt sinh từ luật",
                english: "Not editable: this event comes from a rule");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuHideLane),
                vietnamese: "Ẩn làn",
                english: "Hide the lane");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuMoveLaneUp),
                vietnamese: "Đưa lên",
                english: "Move up");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuMoveLaneDown),
                vietnamese: "Đưa xuống",
                english: "Move down");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuShowAllLanes),
                vietnamese: "Hiện tất cả làn",
                english: "Show every lane");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuAddForLaneFormat),
                vietnamese: "Thêm đợt cố định cho {0}…",
                english: "Add a fixed event for {0}…");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuOpenEventTypes),
                vietnamese: "Mở Loại event",
                english: "Open Event types");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuAddAtFormat),
                vietnamese: "Thêm đợt bắt đầu {0} UTC…",
                english: "Add an event starting {0} UTC…");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuPasteAtCursor),
                vietnamese: "Dán đợt đã copy tại con trỏ",
                english: "Paste the copied event at the cursor");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuPasteDisabledReason),
                vietnamese: "Chưa copy đợt nào",
                english: "No event has been copied yet");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuLaneAtTopReason),
                vietnamese: "Đã ở đầu",
                english: "Already at the top");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuLaneAtBottomReason),
                vietnamese: "Đã ở cuối",
                english: "Already at the bottom");
            table.AddShared(nameof(LiveOpsHubStrings.CalendarDepthMenuShortcutFormat), "{0} {1}");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuDisabledReasonFormat),
                vietnamese: "{0} — {1}",
                english: "{0} — {1}");

            table.Add(nameof(LiveOpsHubStrings.CalendarDepthCopyIdToastFormat),
                vietnamese: "Đã copy id {0}",
                english: "Copied the id {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthCopyJsonToastFormat),
                vietnamese: "Đã copy đợt {0} (JSON)",
                english: "Copied the event {0} (JSON)");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthPasteToastFormat),
                vietnamese: "Đã dán {0} tại {1} UTC",
                english: "Pasted {0} at {1} UTC");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthHideLaneToastFormat),
                vietnamese: "Đã ẩn làn {0}",
                english: "Hid the {0} lane");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthShowAllLanesToast),
                vietnamese: "Đã hiện tất cả làn",
                english: "Showed every lane");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMoveLaneUpToastFormat),
                vietnamese: "Đã đưa làn {0} lên",
                english: "Moved the {0} lane up");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMoveLaneDownToastFormat),
                vietnamese: "Đã đưa làn {0} xuống",
                english: "Moved the {0} lane down");

            table.Add(nameof(LiveOpsHubStrings.CalendarDepthPasteUndoStepFormat),
                vietnamese: "Dán {0}",
                english: "Paste {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMoveLaneUndoStepFormat),
                vietnamese: "Đưa làn {0}",
                english: "Move the {0} lane");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthRevertUndoStepFormat),
                vietnamese: "Hoàn {0} về bản đã đăng",
                english: "Revert {0} to the published version");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthTakeFromDiskUndoStepFormat),
                vietnamese: "Lấy {0} từ bản trên đĩa",
                english: "Take {0} from the version on disk");

            // ----- Đợt W8-UX (gói A) -----
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthUnreadableTimeText),
                vietnamese: "giờ chưa đọc được",
                english: "time not readable");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMoveStartEdgeUndoStepFormat),
                vietnamese: "Đổi mép đầu {0}",
                english: "Change the start edge of {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMoveEndEdgeUndoStepFormat),
                vietnamese: "Đổi mép cuối {0}",
                english: "Change the end edge of {0}");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuNotInPublishedReason),
                vietnamese: "Đợt này chưa có trong bản đã đăng",
                english: "This event is not in the published version");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuRecurringLaneReason),
                vietnamese: "Làn lặp — sửa ở Luật lặp",
                english: "Recurring lane — edit it in Recurring rules");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthMenuNoHiddenLaneReason),
                vietnamese: "Không có làn nào đang ẩn",
                english: "No lane is hidden");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthShowHiddenLanesButton),
                vietnamese: "Hiện",
                english: "Show");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthHiddenLanesLabelFormat),
                vietnamese: "Đang ẩn {0} làn",
                english: "{0} {0|lane|lanes} hidden");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthShowHiddenLanesTooltip),
                vietnamese: "Hiện lại mọi làn đang ẩn",
                english: "Show every hidden lane again");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthCompareMenuItem),
                vietnamese: "So với đã đăng",
                english: "Vs published");
            table.Add(nameof(LiveOpsHubStrings.CalendarDepthSnapMenuItemFormat),
                vietnamese: "Bắt lưới: {0}",
                english: "Snap: {0}");
        }
    }
}
