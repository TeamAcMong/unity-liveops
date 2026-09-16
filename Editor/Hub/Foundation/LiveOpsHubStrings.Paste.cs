namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Paste (G-PASTE, mục 7.6 + [SD2 §2.9] + vá V-14): popover "Dán JSON đang chạy" / "Nhập JSON đang chạy",
    /// hộp xác nhận cấp 1 "Thay lịch nháp bằng JSON đã dán?" (Hình 8 hộp 3) và kết quả của luồng nhập.
    /// <para>
    /// Vì sao một vùng riêng chứ không nhét vào Export: nút mở luồng này nằm ở BỐN màn (Tổng quan, việc cần làm, cổng Xuất,
    /// hàng Chưa kiểm của Kiểm lịch) nên không màn nào là chủ của câu; chủ là chính luồng dán. Câu hai ngôn ngữ ở
    /// <c>Language/LiveOpsHubStringCatalog.Paste.cs</c>.
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // ----- Popover dán ([SD2 §2.9]) -----

        /// <summary>Popover không dựng được vì thiếu UXML — câu của vùng, nêu đúng đường dẫn để sửa được (kịch bản h28b).</summary>
        internal static string PasteMissingLayoutFormat => LiveOpsHubStringCatalog.Text(nameof(PasteMissingLayoutFormat));

        /// <summary>Tiêu đề popover ở chế độ DÁN — động từ + đối tượng, cùng chữ với nút mở nó (bỏ dấu ba chấm).</summary>
        internal static string PastePopoverTitle => LiveOpsHubStringCatalog.Text(nameof(PastePopoverTitle));

        /// <summary>Tiêu đề popover ở chế độ NHẬP (V-14 bước 1) — chưa có asset nên việc là "nhập vào asset mới".</summary>
        internal static string PasteImportPopoverTitle => LiveOpsHubStringCatalog.Text(nameof(PasteImportPopoverTitle));

        /// <summary>
        /// Nhãn ngay trên ô dán. Ô mono không nhãn thì người dùng không biết phải dán CÁI GÌ vào (JSON của hub hay của
        /// Firebase) — câu nói thẳng nguồn: bản đang chạy trên remote config.
        /// </summary>
        internal static string PasteInputLabel => LiveOpsHubStringCatalog.Text(nameof(PasteInputLabel));

        /// <summary>Dòng "Key remote: liveops_calendar (đổi được sau ở Tổng quan)" của chế độ nhập (V-14 bước 1).</summary>
        internal static string PasteRemoteKeyLineFormat => LiveOpsHubStringCatalog.Text(nameof(PasteRemoteKeyLineFormat));

        /// <summary>
        /// Dòng trạng thái khi parser đọc được: "Đọc được: 2 luật lặp · 6 đợt · 5 loại chưa khai báo" (V-14 bước 2). Đếm
        /// LOẠI chưa khai báo ngay tại đây vì đó là thứ duy nhất người dùng còn phải làm sau khi nhập.
        /// </summary>
        internal static string PasteReadableWithUnknownTypesFormat => LiveOpsHubStringCatalog.Text(nameof(PasteReadableWithUnknownTypesFormat));

        /// <summary>Cùng dòng đó khi mọi loại đã khai báo — không in "0 loại chưa khai báo" (số 0 đọc như một cảnh báo).</summary>
        internal static string PasteReadableFormat => LiveOpsHubStringCatalog.Text(nameof(PasteReadableFormat));

        /// <summary>
        /// Lý do nút chính bị khoá khi ô còn trống (SPIKE-B SP-3: lý do LUÔN in thành chữ cạnh nút, tooltip chỉ phụ).
        /// Ô trống khác ô sai cú pháp: nói "JSON hỏng" lúc chưa dán gì là đổ lỗi cho người dùng.
        /// </summary>
        internal static string PasteNothingPastedReason => LiveOpsHubStringCatalog.Text(nameof(PasteNothingPastedReason));

        /// <summary>Lý do khoá khi cú pháp đúng nhưng parser của game không đọc ra tài liệu nào (V-14 bước 2).</summary>
        internal static string PasteNotReadableReason => LiveOpsHubStringCatalog.Text(nameof(PasteNotReadableReason));

        /// <summary>Lựa chọn 1 của <c>RadioButtonGroup</c> — mặc định, không đụng nháp ([SD2 §2.9]).</summary>
        internal static string PasteModeCompareOnly => LiveOpsHubStringCatalog.Text(nameof(PasteModeCompareOnly));

        /// <summary>Lựa chọn 2 — mở hộp xác nhận cấp 1 ở Hình 8 ([SD2 §2.9]).</summary>
        internal static string PasteModeReplaceDraft => LiveOpsHubStringCatalog.Text(nameof(PasteModeReplaceDraft));

        /// <summary>Nút chính của chế độ dán: chạy đúng lựa chọn đang chọn.</summary>
        internal static string PasteConfirmButton => LiveOpsHubStringCatalog.Text(nameof(PasteConfirmButton));

        /// <summary>Nút chính của chế độ nhập — mở hộp chọn nơi lưu, chưa tạo gì (V-14 bước 3).</summary>
        internal static string PasteChooseSaveLocationButton => LiveOpsHubStringCatalog.Text(nameof(PasteChooseSaveLocationButton));

        internal static string PasteCancelButton => LiveOpsHubStringCatalog.Text(nameof(PasteCancelButton));

        /// <summary>Toggle bật sẵn của chế độ nhập (V-14 bước 1): ghi luôn dấu đã đăng cho bản vừa nhập.</summary>
        internal static string PasteStampToggleLabel => LiveOpsHubStringCatalog.Text(nameof(PasteStampToggleLabel));

        /// <summary>Toast Info sau khi dán để so — nói rõ cái vừa xảy ra là "đã có bản để so", không phải "đã sửa lịch".</summary>
        internal static string PasteComparedToast => LiveOpsHubStringCatalog.Text(nameof(PasteComparedToast));

        // ----- Hộp xác nhận cấp 1 "Thay nháp" (Hình 8 hộp 3, [SD2 §2.9]) -----

        internal static string PasteReplaceConfirmTitle => LiveOpsHubStringCatalog.Text(nameof(PasteReplaceConfirmTitle));

        /// <summary>Thân hộp: các vế "sẽ mất gì" ghép bằng " · ", rồi câu hứa hoàn tác ([SD2 §2.9]).</summary>
        internal static string PasteReplaceConfirmBodyFormat => LiveOpsHubStringCatalog.Text(nameof(PasteReplaceConfirmBodyFormat));

        /// <summary>Vế 1: "Thay 5 đợt: lava-quest-2026-09a, …" — nêu TỪNG id, không chỉ số đếm (bảng 7.0).</summary>
        internal static string PasteReplaceConfirmReplacedFormat => LiveOpsHubStringCatalog.Text(nameof(PasteReplaceConfirmReplacedFormat));

        /// <summary>Vế 2: "xoá hunt-0916-bonus" — đợt chỉ có trong nháp sẽ biến mất hẳn.</summary>
        internal static string PasteReplaceConfirmDroppedFormat => LiveOpsHubStringCatalog.Text(nameof(PasteReplaceConfirmDroppedFormat));

        /// <summary>Vế 3: "đổi luật weekly-pass" — luật lặp đổi là id lần lặp đổi, người chơi mất tiến độ.</summary>
        internal static string PasteReplaceConfirmRulesFormat => LiveOpsHubStringCatalog.Text(nameof(PasteReplaceConfirmRulesFormat));

        /// <summary>
        /// Vế 4: "xoá luật sky-race" — luật chỉ có trong nháp. Tách khỏi vế "đổi luật" vì hộp cấp 1 tồn tại để nói thứ
        /// Sẽ MẤT (bảng 7.0): gọi một luật sắp biến mất là "đổi" là nói nhẹ đi đúng cái nguy hiểm nhất (soát W5 P-10).
        /// </summary>
        internal static string PasteReplaceConfirmRulesRemovedFormat => LiveOpsHubStringCatalog.Text(nameof(PasteReplaceConfirmRulesRemovedFormat));

        /// <summary>Vế 5: "thêm luật lucky-spin" — luật chỉ có trong bản dán; thêm không làm mất gì nhưng phải nói ra.</summary>
        internal static string PasteReplaceConfirmRulesAddedFormat => LiveOpsHubStringCatalog.Text(nameof(PasteReplaceConfirmRulesAddedFormat));

        /// <summary>Vế thêm khi asset còn thay đổi chưa lưu ([SD2 §2.9] "nêu thêm số chưa lưu").</summary>
        internal static string PasteReplaceConfirmUnsavedFormat => LiveOpsHubStringCatalog.Text(nameof(PasteReplaceConfirmUnsavedFormat));

        /// <summary>Gợi ý phím góc trái: Enter/Esc = việc AN TOÀN (quyết định SPIKE-B 16/9).</summary>
        internal static string PasteReplaceConfirmKeyHint => LiveOpsHubStringCatalog.Text(nameof(PasteReplaceConfirmKeyHint));

        /// <summary>Nút phá huỷ: "Thay 7 mục" — đếm cả đợt lẫn luật sẽ bị ghi đè ([SD2 §2.9]).</summary>
        internal static string PasteReplaceConfirmDestructiveFormat => LiveOpsHubStringCatalog.Text(nameof(PasteReplaceConfirmDestructiveFormat));

        internal static string PasteReplaceConfirmSafe => LiveOpsHubStringCatalog.Text(nameof(PasteReplaceConfirmSafe));

        /// <summary>Tên Undo group (= câu toast) của lần thay nháp.</summary>
        internal static string PasteReplaceUndoName => LiveOpsHubStringCatalog.Text(nameof(PasteReplaceUndoName));

        // ----- Luồng "Nhập JSON đang chạy…" khi chưa có asset (V-14) -----

        /// <summary>Tiêu đề hộp chọn nơi lưu (V-14 bước 3).</summary>
        internal static string PasteImportSaveDialogTitle => LiveOpsHubStringCatalog.Text(nameof(PasteImportSaveDialogTitle));

        /// <summary>Tên Undo group của lần nhập: "Nhập JSON đang chạy vào Main.asset" (V-14 bước 4).</summary>
        internal static string PasteImportUndoNameFormat => LiveOpsHubStringCatalog.Text(nameof(PasteImportUndoNameFormat));

        /// <summary>Ghi chú của dấu đã đăng do luồng nhập tự ghi (V-14 bước 4).</summary>
        internal static string PasteImportStampNote => LiveOpsHubStringCatalog.Text(nameof(PasteImportStampNote));

        /// <summary>Headline outcome Ok sau khi nhập xong (V-14 bước 5).</summary>
        internal static string PasteImportOutcomeHeadlineFormat => LiveOpsHubStringCatalog.Text(nameof(PasteImportOutcomeHeadlineFormat));

        /// <summary>Dòng dưới outcome khi còn loại chưa khai báo — việc tiếp theo của người dùng (V-14 bước 5).</summary>
        internal static string PasteImportOutcomeUnknownTypesFormat => LiveOpsHubStringCatalog.Text(nameof(PasteImportOutcomeUnknownTypesFormat));

        /// <summary>Dòng dưới outcome khi mọi loại đã khai báo — không để trống, người dùng cần biết bước kế.</summary>
        internal static string PasteImportOutcomeReadyDetail => LiveOpsHubStringCatalog.Text(nameof(PasteImportOutcomeReadyDetail));

        /// <summary>
        /// Tên bước của toast khi Hoàn tác lần nhập: Undo trả nội dung asset về rỗng nhưng FILE vẫn nằm trên đĩa (tạo asset
        /// không nằm trong Undo của Unity) — toast phải nói ra, không thì người dùng tưởng file đã bị xoá (V-14 bước 6).
        /// </summary>
        internal static string PasteImportUndoneStepFormat => LiveOpsHubStringCatalog.Text(nameof(PasteImportUndoneStepFormat));

        /// <summary>Tiêu đề hộp báo lỗi không tạo được asset (7.0: hộp không phá huỷ vẫn dùng DisplayDialog).</summary>
        internal static string PasteImportCreateFailedTitle => LiveOpsHubStringCatalog.Text(nameof(PasteImportCreateFailedTitle));

        /// <summary>Thân hộp báo lỗi: "Không tạo được asset: &lt;đường dẫn&gt;" (V-14 bước 4).</summary>
        internal static string PasteImportCreateFailedFormat => LiveOpsHubStringCatalog.Text(nameof(PasteImportCreateFailedFormat));

        /// <summary>
        /// Thân hộp khi người dùng chọn nơi lưu NẰM NGOÀI project. Khác hẳn "bấm Huỷ": họ đã quyết định tạo, nên lý do
        /// không tạo được phải in thành chữ (SPIKE-B SP-3) thay vì luồng tắt im lặng (soát W5 P-5).
        /// </summary>
        internal static string PasteImportOutsideProjectFormat => LiveOpsHubStringCatalog.Text(nameof(PasteImportOutsideProjectFormat));

        internal static string PasteImportCreateFailedCloseButton => LiveOpsHubStringCatalog.Text(nameof(PasteImportCreateFailedCloseButton));
    }
}
