namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng ShortcutHelp (G-OPT-SHORTCUTHELP, W6) — popover "Hiện hướng dẫn phím tắt". Bản tiếng Việt là bản gốc,
    /// bản tiếng Anh dịch cùng lúc (G-I18N §4). Comment "vì sao" của từng câu ở <c>LiveOpsHubStrings.ShortcutHelp.cs</c>.
    /// <para>
    /// Nhãn phím đi bằng <see cref="LiveOpsHubStringTable.AddShared"/>: ký hiệu phím ("← →", "Esc") là ký hiệu bàn phím,
    /// dịch ra vẫn y hệt — tách hai bản chỉ tạo chỗ cho chúng trôi khỏi nhau. Riêng tên chữ của phím bổ trợ ngoài macOS
    /// ("Alt", "Shift") KHÔNG khai lại ở đây: chúng đã có ở vùng Foundation (<c>KeyLabelOption</c> / <c>KeyLabelShift</c>) và
    /// popover đọc thẳng từ đó — khai lại là hai câu cho cùng một phím.
    /// </para>
    /// <para>
    /// Không câu nào của vùng này đếm số, nên không câu nào cần dấu số ít/số nhiều của bản tiếng Anh (luật Q-W5-2 do
    /// G-FIX-W6-1 mang vào): vùng mới sinh sau lượt quét catalog nên chỗ an toàn nhất là không đẻ câu đếm.
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterShortcutHelp(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpMissingLayoutFormat),
                vietnamese: "Thiếu bố cục popover hướng dẫn phím tắt: {0}",
                english: "The keyboard shortcut help popover layout is missing: {0}");

            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpMenuItem),
                vietnamese: "Hiện hướng dẫn phím tắt",
                english: "Show keyboard shortcut help");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpPopoverTitle),
                vietnamese: "Phím tắt của LiveOps Hub",
                english: "LiveOps Hub keyboard shortcuts");

            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpWindowGroupTitle),
                vietnamese: "Khi cửa sổ hub đang có focus",
                english: "While the hub window has focus");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpTimelineGroupTitle),
                vietnamese: "Khi timeline đang có focus",
                english: "While the timeline has focus");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpUnboundKey),
                vietnamese: "chưa gán phím",
                english: "no key assigned");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpNote),
                vietnamese: "Đổi phím trong cửa sổ Shortcuts của Unity. Phím của timeline không có trong cửa sổ đó: timeline tự "
                    + "xử lý chúng để không cướp phím lúc bạn đang gõ chữ.",
                english: "Change key bindings in Unity's Shortcuts window. The timeline keys are not listed there: the timeline "
                    + "handles them itself so they never steal a key while you are typing.");

            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpOpenShortcutManagerButton),
                vietnamese: "Mở cửa sổ Shortcuts…",
                english: "Open the Shortcuts window…");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpCloseButton),
                vietnamese: "Đóng",
                english: "Close");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpOpenShortcutManagerFailedReason),
                vietnamese: "Bản Unity này không mở được cửa sổ Shortcuts từ đây",
                english: "This Unity version cannot open the Shortcuts window from here");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpOpenShortcutManagerFailedLogFormat),
                vietnamese: "LiveOps Hub: không mở được cửa sổ Shortcuts của Unity — {0}",
                english: "LiveOps Hub: cannot open Unity's Shortcuts window — {0}");

            table.AddShared(nameof(LiveOpsHubStrings.ShortcutHelpKeyArrowsLeftRight), "← →");
            // {0} = phím bổ trợ theo NỀN TẢNG (⇧/⌥ trên macOS, "Shift"/"Alt" chỗ khác) — ghi cứng ký hiệu Mac thì người dùng
            // Windows/Linux đọc "⌥ ← →" không biết bấm phím nào.
            table.AddShared(nameof(LiveOpsHubStrings.ShortcutHelpKeyModifierArrowsLeftRightFormat), "{0} ← →");
            table.AddShared(nameof(LiveOpsHubStrings.ShortcutHelpShiftKeyMac), "⇧");
            table.AddShared(nameof(LiveOpsHubStrings.ShortcutHelpOptionKeyMac), "⌥");
            table.AddShared(nameof(LiveOpsHubStrings.ShortcutHelpKeyActionArrowsUpDownFormat), "{0}↑ {0}↓");
            table.AddShared(nameof(LiveOpsHubStrings.ShortcutHelpKeyFrameAll), "A");
            // "= −" đúng phím mà LiveOpsTimelineElement nghe (KeyCode.Equals / KeyCode.Minus) và đúng chữ bảng phím 5.2 của
            // thiết kế; "+" là phím đó khi giữ Shift nên nhãn "+ −" hứa một tổ hợp không có nhánh nào xử lý.
            table.AddShared(nameof(LiveOpsHubStrings.ShortcutHelpKeyZoom), "= −");
            table.AddShared(nameof(LiveOpsHubStrings.ShortcutHelpKeyEscape), "Esc");
            table.AddShared(nameof(LiveOpsHubStrings.ShortcutHelpKeyContextMenu), "Menu");

            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpTimelineNudgeByStep),
                vietnamese: "Nhích đợt đang chọn một bước lưới",
                english: "Nudge the selected event by one grid step");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpTimelineNudgeByDay),
                vietnamese: "Nhích đợt đang chọn một ngày",
                english: "Nudge the selected event by one day");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpTimelineMoveEndEdge),
                vietnamese: "Đổi giờ kết thúc của đợt đang chọn",
                english: "Move the end of the selected event");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpTimelineMoveStartEdge),
                vietnamese: "Đổi giờ bắt đầu của đợt đang chọn",
                english: "Move the start of the selected event");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpTimelineSelectAdjacentLane),
                vietnamese: "Chọn đợt ở làn trên hoặc làn dưới",
                english: "Select the event in the lane above or below");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpTimelineFrameAll),
                vietnamese: "Thu cả lịch vào khung nhìn",
                english: "Fit the whole calendar into view");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpTimelineZoom),
                vietnamese: "Phóng to hoặc thu nhỏ timeline",
                english: "Zoom the timeline in or out");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpTimelineCancelDrag),
                vietnamese: "Huỷ cử chỉ kéo đang làm",
                english: "Cancel the drag in progress");
            table.Add(nameof(LiveOpsHubStrings.ShortcutHelpTimelineContextMenu),
                vietnamese: "Mở menu chuột phải của đợt đang chọn",
                english: "Open the context menu of the selected event");
        }
    }
}
