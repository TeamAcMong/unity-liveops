namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng CalendarDepth (G-CALENDAR-DEPTH, mục 7.3): năm mảnh chiều sâu của màn Lịch — hai nút toolbar W5, pane Danh sách
    /// ([SD1 §3.12]), pane "So với đã đăng" ([SD1 §3.4]), hover card + F8 ([SD1 §3.8]) và năm menu chuột phải ([FD §3.9]).
    /// <para>
    /// Câu của TỪNG hàng diff và TỪNG phát hiện KHÔNG nằm ở đây (V-8, V-22 CC-FT-1): hàng pane So với lấy câu từ
    /// <see cref="LiveOpsChangeText"/> (overload có <see cref="LiveOpsChangeTextContext"/>), dòng lỗi của hover card lấy từ
    /// <see cref="LiveOpsFindingText"/>. Vùng này chỉ có chữ của KHUNG năm mảnh đó. Câu hai ngôn ngữ ở
    /// <c>Language/LiveOpsHubStringCatalog.CalendarDepth.cs</c>.
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // ----- Hai nút toolbar W5 [SD1 §3.1] -----

        internal static string CalendarDepthListToggle => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthListToggle));

        /// <summary>"So với đã đăng (5)" — số là số thay đổi so với bản so đang chọn, đếm cùng một diff mà pane vẽ.</summary>
        internal static string CalendarDepthCompareToggleFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthCompareToggleFormat));

        /// <summary>Lý do nút So với bị khoá — SPIKE-B SP-3: in thành CHỮ cạnh nút, tooltip chỉ là bản phụ.</summary>
        internal static string CalendarDepthCompareUnavailableReason => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthCompareUnavailableReason));

        // ----- Pane Danh sách [SD1 §3.12] -----

        internal static string CalendarDepthListColumnId => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthListColumnId));
        internal static string CalendarDepthListColumnType => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthListColumnType));
        internal static string CalendarDepthListColumnStart => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthListColumnStart));
        internal static string CalendarDepthListColumnEnd => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthListColumnEnd));
        internal static string CalendarDepthListColumnState => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthListColumnState));

        /// <summary>Lịch chưa có đợt cố định nào: pane vẫn mở được, nói rõ vì sao trống thay vì vẽ một bảng rỗng.</summary>
        internal static string CalendarDepthListEmpty => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthListEmpty));

        // ----- Pane "So với đã đăng" [SD1 §3.4], nguồn Disk theo vá V-13 -----

        internal static string CalendarDepthComparePublishedTitleFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthComparePublishedTitleFormat));

        /// <summary>(V-13) Nguồn Disk: tiêu đề nêu GIỜ file đổi trên đĩa — người đọc phải biết mình đang so với bản nào.</summary>
        internal static string CalendarDepthCompareDiskTitleFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthCompareDiskTitleFormat));

        internal static string CalendarDepthCompareEmpty => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthCompareEmpty));

        /// <summary>Note dưới pane: nói CẢ hai cử chỉ (bấm dòng, chuột phải) vì không cử chỉ nào có nút để nhìn thấy.</summary>
        internal static string CalendarDepthCompareNote => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthCompareNote));

        /// <summary>(V-13) Nguồn Disk không có "Hoàn về bản đã đăng" nên note cũng phải đổi theo, không nhắc một mục không có.</summary>
        internal static string CalendarDepthCompareDiskNote => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthCompareDiskNote));

        internal static string CalendarDepthRevertToPublished => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthRevertToPublished));

        /// <summary>(V-13) Nguồn Disk: lấy đúng MỘT mục từ bản trên đĩa, không nuốt cả file.</summary>
        internal static string CalendarDepthTakeFromDisk => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthTakeFromDisk));

        internal static string CalendarDepthRevertToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthRevertToastFormat));
        internal static string CalendarDepthTakeFromDiskToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthTakeFromDiskToastFormat));

        // ----- Hover card + F8 [SD1 §3.8] -----

        internal static string CalendarDepthHoverStartLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthHoverStartLabel));
        internal static string CalendarDepthHoverEndLabel => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthHoverEndLabel));

        /// <summary>"16/9 12:00 UTC (19:00 giờ máy)" — giờ máy trong ngoặc, luôn có chữ "giờ máy" [SD1 §3.13].</summary>
        internal static string CalendarDepthHoverUtcWithDeviceFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthHoverUtcWithDeviceFormat));

        /// <summary>"18/9 00:00 UTC · dài 36 giờ" — dòng Kết thúc gộp luôn thời lượng để khỏi phải tự trừ.</summary>
        internal static string CalendarDepthHoverEndValueFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthHoverEndValueFormat));

        internal static string CalendarDepthHoverFooter => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthHoverFooter));

        /// <summary>Chỉ có ở thẻ ĐÃ GHIM (đến bằng F8): mở popover Đề xuất, hoặc focus inspector khi phát hiện không có cách sửa.</summary>
        internal static string CalendarDepthHoverQuickFixButton => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthHoverQuickFixButton));

        /// <summary>"1/5" — thứ tự phát hiện đang xem trên TỔNG phát hiện của cả lịch, khớp số của màn Kiểm lịch.</summary>
        internal static string CalendarDepthHoverCounterFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthHoverCounterFormat));

        /// <summary>Tooltip nút đóng drawer — nêu luôn phím, vì Esc là đường nhanh hơn với tay đang ở bàn phím [SD1 §3.9].</summary>
        internal static string CalendarDepthDrawerCloseTooltip => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthDrawerCloseTooltip));

        // Menu ⋮ của toolbar ở cửa sổ hẹp (8.8): tooltip của nút và mục bật lại dải chú giải.
        internal static string CalendarDepthOverflowMenuTooltip => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthOverflowMenuTooltip));
        internal static string CalendarDepthLegendToggle => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthLegendToggle));

        // ----- Menu chuột phải [FD §3.9] -----

        internal static string CalendarDepthMenuEditInInspector => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuEditInInspector));

        /// <summary>"Nhân bản sang 24/9 00:00 (+7 ngày)…" — menu nêu ĐÍCH cụ thể, không để người dùng đoán offset.</summary>
        internal static string CalendarDepthMenuDuplicateFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuDuplicateFormat));

        // Nhãn "Nhân bản" của nhánh KHOÁ: khuôn có {0}/{1} không dùng lại được ở đây vì không có đích để điền — in khuôn ra màn
        // hình là in đúng dấu ngoặc nhọn cho người dùng đọc.
        internal static string CalendarDepthMenuDuplicateDisabled => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuDuplicateDisabled));

        // Vì sao không nhân bản được: giờ bắt đầu của đợt không đọc được (vd "2026-10-3"), nên không tính được đích.
        internal static string CalendarDepthMenuDuplicateUnreadableStartReason => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuDuplicateUnreadableStartReason));

        internal static string CalendarDepthMenuFrame => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuFrame));
        internal static string CalendarDepthMenuMoveStart => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuMoveStart));
        internal static string CalendarDepthMenuSetDuration => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuSetDuration));
        internal static string CalendarDepthMenuCopyId => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuCopyId));
        internal static string CalendarDepthMenuCopyEventJson => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuCopyEventJson));
        internal static string CalendarDepthMenuDelete => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuDelete));

        internal static string CalendarDepthMenuOpenRuleFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuOpenRuleFormat));

        /// <summary>Mục disabled của thanh lặp: nhãn TỰ NÓI lý do, không phải một mục xám không giải thích gì.</summary>
        internal static string CalendarDepthMenuRecurringReadOnly => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuRecurringReadOnly));

        internal static string CalendarDepthMenuHideLane => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuHideLane));
        internal static string CalendarDepthMenuMoveLaneUp => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuMoveLaneUp));
        internal static string CalendarDepthMenuMoveLaneDown => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuMoveLaneDown));
        internal static string CalendarDepthMenuShowAllLanes => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuShowAllLanes));
        internal static string CalendarDepthMenuAddForLaneFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuAddForLaneFormat));
        internal static string CalendarDepthMenuOpenEventTypes => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuOpenEventTypes));

        /// <summary>Chỗ trống làn cố định: giờ trong nhãn là giờ TẠI CON TRỎ, đã bắt lưới — bấm vào là ra đúng giờ đó.</summary>
        internal static string CalendarDepthMenuAddAtFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuAddAtFormat));

        internal static string CalendarDepthMenuPasteAtCursor => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuPasteAtCursor));

        /// <summary>Clipboard không có JSON đợt: mục vẫn hiện nhưng disabled và nhãn nói vì sao (SPIKE-B SP-3).</summary>
        internal static string CalendarDepthMenuPasteDisabledReason => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuPasteDisabledReason));

        internal static string CalendarDepthMenuLaneAtTopReason => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuLaneAtTopReason));
        internal static string CalendarDepthMenuLaneAtBottomReason => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuLaneAtBottomReason));

        /// <summary>Ghép nhãn mục với phím tắt đọc từ binding thật ("Xoá đợt… ⌘⌫"); không có binding thì bỏ hẳn vế phím.</summary>
        internal static string CalendarDepthMenuShortcutFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuShortcutFormat));

        /// <summary>Mục disabled ghép thêm lý do: "Đưa lên — Đã ở đầu".</summary>
        internal static string CalendarDepthMenuDisabledReasonFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuDisabledReasonFormat));

        // ----- Toast của lệnh chiều sâu -----

        internal static string CalendarDepthCopyIdToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthCopyIdToastFormat));
        internal static string CalendarDepthCopyJsonToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthCopyJsonToastFormat));
        internal static string CalendarDepthPasteToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthPasteToastFormat));
        internal static string CalendarDepthHideLaneToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthHideLaneToastFormat));
        internal static string CalendarDepthShowAllLanesToast => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthShowAllLanesToast));

        /// <summary>(V-12) Đưa làn KHÔNG đổi JSON/sha — toast nói đúng "đưa làn", không nói "sửa lịch".</summary>
        internal static string CalendarDepthMoveLaneUpToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMoveLaneUpToastFormat));
        internal static string CalendarDepthMoveLaneDownToastFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMoveLaneDownToastFormat));
    
        // ----- Tên bước Undo của ba lệnh chiều sâu (phiếu D-5) -----
        // Toast sau ⌘Z in "Đã hoàn tác: " + TÊN BƯỚC. Câu toast của lệnh bắt đầu bằng "Đã …", nên nếu dùng lại câu toast làm
        // tên bước thì người dùng đọc "Đã hoàn tác: Đã dán …" — hai lần "Đã". Bốn khuôn dưới là dạng động từ nguyên thể.
        internal static string CalendarDepthPasteUndoStepFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthPasteUndoStepFormat));
        internal static string CalendarDepthMoveLaneUndoStepFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMoveLaneUndoStepFormat));
        internal static string CalendarDepthRevertUndoStepFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthRevertUndoStepFormat));
        internal static string CalendarDepthTakeFromDiskUndoStepFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthTakeFromDiskUndoStepFormat));

        // ----- Đợt W8-UX: chữ mới của gói A -----

        /// <summary>
        /// (UX-04, UJ-04) Giờ trong chuỗi thô không đọc được. Câu toast cũ in mốc mặc định "1/1 00:00" — một giờ mà người dùng
        /// chưa bao giờ gõ, nói ngược lại đúng cái lỗi hub vừa báo ở ô. Nói thẳng "chưa đọc được" là câu duy nhất luôn đúng.
        /// </summary>
        internal static string CalendarDepthUnreadableTimeText => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthUnreadableTimeText));

        /// <summary>
        /// (UX-26, UJ-10) Tên bước kéo MỘT MÉP. Câu chung "Đổi {0}" làm status bar ("Vừa làm: Đổi hunt-0914") và toast ("Đã dời
        /// kết thúc…") đọc như hai thao tác khác nhau; hai khuôn dưới nói đúng mép nào đã đổi.
        /// </summary>
        internal static string CalendarDepthMoveStartEdgeUndoStepFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMoveStartEdgeUndoStepFormat));
        internal static string CalendarDepthMoveEndEdgeUndoStepFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMoveEndEdgeUndoStepFormat));

        /// <summary>
        /// (UX-15, UJ-18) Có dấu đã đăng rồi nhưng đợt này chưa nằm trong bản đó. Lý do cũ "Chưa có dấu đã đăng" nói sai sự
        /// thật — người dùng vừa đăng xong vẫn đọc câu đó và tưởng lần đăng không ăn.
        /// </summary>
        internal static string CalendarDepthMenuNotInPublishedReason => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuNotInPublishedReason));

        /// <summary>(UX-15, UJ-18) Làn lặp sinh đợt từ luật nên không nhận đợt cố định; mục vẫn ở lại và tự nói lý do.</summary>
        internal static string CalendarDepthMenuRecurringLaneReason => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuRecurringLaneReason));

        /// <summary>(UX-15, UJ-18) "Hiện tất cả làn" khi không làn nào ẩn là một lời hứa suông — khoá kèm lý do.</summary>
        internal static string CalendarDepthMenuNoHiddenLaneReason => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthMenuNoHiddenLaneReason));

        /// <summary>
        /// (UX-12, UJ-07) Nút "Hiện" trong chip "Đang ẩn n làn". Trước đây cả chip chỉ là một Label nên bấm vào chữ "Hiện"
        /// không có gì xảy ra — người dùng bấm ba lần rồi bỏ cuộc.
        /// </summary>
        internal static string CalendarDepthShowHiddenLanesButton => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthShowHiddenLanesButton));

        /// <summary>
        /// (UX-12) Nhãn của chip, KHÔNG còn đuôi "· Hiện": đuôi đó giờ là một nút thật đứng cạnh. Khuôn cũ
        /// <c>CalendarHiddenLanesChipFormat</c> giữ nguyên cho vùng Calendar, không đổi ở đợt này.
        /// </summary>
        internal static string CalendarDepthHiddenLanesLabelFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthHiddenLanesLabelFormat));

        internal static string CalendarDepthShowHiddenLanesTooltip => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthShowHiddenLanesTooltip));

        /// <summary>(UX-29) Mục menu ⋮ thay cho nút "So với đã đăng (n)" khi cửa sổ hẹp — nhãn không mang số vì DropdownMenu
        /// chốt nhãn lúc dựng menu.</summary>
        internal static string CalendarDepthCompareMenuItem => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthCompareMenuItem));

        /// <summary>(UX-29) Mục menu ⋮ của một lựa chọn bắt lưới: "Bắt lưới: Tự động".</summary>
        internal static string CalendarDepthSnapMenuItemFormat => LiveOpsHubStringCatalog.Text(nameof(CalendarDepthSnapMenuItemFormat));
    }
}
