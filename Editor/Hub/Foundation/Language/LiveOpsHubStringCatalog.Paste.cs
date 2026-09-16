namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Paste (luồng Dán / Nhập JSON đang chạy) — bản tiếng Việt chép nguyên văn từ thiết kế [SD2 §2.9] và
    /// vá V-14, bản tiếng Anh dịch cùng lúc (G-I18N §4). Comment "vì sao" của từng câu ở
    /// <c>LiveOpsHubStrings.Paste.cs</c>.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterPaste(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.PasteMissingLayoutFormat),
                vietnamese: "Thiếu bố cục popover dán JSON: {0}",
                english: "The paste JSON popover layout is missing: {0}");

            table.Add(nameof(LiveOpsHubStrings.PastePopoverTitle),
                vietnamese: "Dán JSON đang chạy",
                english: "Paste the running JSON");
            table.Add(nameof(LiveOpsHubStrings.PasteImportPopoverTitle),
                vietnamese: "Nhập JSON đang chạy",
                english: "Import the running JSON");
            table.Add(nameof(LiveOpsHubStrings.PasteInputLabel),
                vietnamese: "JSON đang chạy trên remote config",
                english: "The JSON currently live on remote config");
            table.Add(nameof(LiveOpsHubStrings.PasteRemoteKeyLineFormat),
                vietnamese: "Key remote: {0} (đổi được sau ở Tổng quan)",
                english: "Remote key: {0} (you can change it later in Overview)");
            table.Add(nameof(LiveOpsHubStrings.PasteReadableWithUnknownTypesFormat),
                vietnamese: "Đọc được: {0} luật lặp · {1} đợt · {2} loại chưa khai báo",
                english: "Readable: {0} recurring rules · {1} events · {2} undeclared types");
            table.Add(nameof(LiveOpsHubStrings.PasteReadableFormat),
                vietnamese: "Đọc được: {0} luật lặp · {1} đợt",
                english: "Readable: {0} recurring rules · {1} events");
            table.Add(nameof(LiveOpsHubStrings.PasteNothingPastedReason),
                vietnamese: "Chưa dán gì vào ô JSON",
                english: "Nothing pasted into the JSON box yet");
            table.Add(nameof(LiveOpsHubStrings.PasteNotReadableReason),
                vietnamese: "Parser của game không đọc được chuỗi này",
                english: "The game parser cannot read this text");
            table.Add(nameof(LiveOpsHubStrings.PasteModeCompareOnly),
                vietnamese: "Chỉ so sánh với nháp",
                english: "Only compare with the draft");
            table.Add(nameof(LiveOpsHubStrings.PasteModeReplaceDraft),
                vietnamese: "Thay nháp bằng JSON này…",
                english: "Replace the draft with this JSON…");
            table.Add(nameof(LiveOpsHubStrings.PasteConfirmButton),
                vietnamese: "Dán",
                english: "Paste");
            table.Add(nameof(LiveOpsHubStrings.PasteChooseSaveLocationButton),
                vietnamese: "Chọn nơi lưu…",
                english: "Choose where to save…");
            table.Add(nameof(LiveOpsHubStrings.PasteCancelButton),
                vietnamese: "Huỷ",
                english: "Cancel");
            table.Add(nameof(LiveOpsHubStrings.PasteStampToggleLabel),
                vietnamese: "Ghi dấu đây là bản đang chạy — để hub bảo vệ đợt đang chạy",
                english: "Stamp this as the running version — so the hub protects running events");
            table.Add(nameof(LiveOpsHubStrings.PasteComparedToast),
                vietnamese: "Đã dán JSON đang chạy · Kiểm lịch so được với bản này",
                english: "Pasted the running JSON · the calendar check can compare against it now");

            table.Add(nameof(LiveOpsHubStrings.PasteReplaceConfirmTitle),
                vietnamese: "Thay lịch nháp bằng JSON đã dán?",
                english: "Replace the draft calendar with the pasted JSON?");
            table.Add(nameof(LiveOpsHubStrings.PasteReplaceConfirmBodyFormat),
                vietnamese: "{0} Hoàn tác được tới khi đóng Unity.",
                english: "{0} You can undo this until Unity closes.");
            table.Add(nameof(LiveOpsHubStrings.PasteReplaceConfirmReplacedFormat),
                vietnamese: "Thay {0} đợt: {1}",
                english: "Replaces {0} events: {1}");
            table.Add(nameof(LiveOpsHubStrings.PasteReplaceConfirmDroppedFormat),
                vietnamese: "xoá {0}",
                english: "deletes {0}");
            table.Add(nameof(LiveOpsHubStrings.PasteReplaceConfirmRulesFormat),
                vietnamese: "đổi luật {0}",
                english: "changes rule {0}");
            table.Add(nameof(LiveOpsHubStrings.PasteReplaceConfirmRulesRemovedFormat),
                vietnamese: "xoá luật {0}",
                english: "deletes rule {0}");
            table.Add(nameof(LiveOpsHubStrings.PasteReplaceConfirmRulesAddedFormat),
                vietnamese: "thêm luật {0}",
                english: "adds rule {0}");
            table.Add(nameof(LiveOpsHubStrings.PasteReplaceConfirmUnsavedFormat),
                vietnamese: "{0} còn {1} thay đổi chưa lưu — thay nháp là mất luôn",
                english: "{0} still has {1} unsaved changes — replacing the draft loses them");
            table.Add(nameof(LiveOpsHubStrings.PasteReplaceConfirmKeyHint),
                vietnamese: "Enter / Esc: Chỉ so sánh",
                english: "Enter / Esc: Only compare");
            table.Add(nameof(LiveOpsHubStrings.PasteReplaceConfirmDestructiveFormat),
                vietnamese: "Thay {0} mục",
                english: "Replace {0} items");
            table.Add(nameof(LiveOpsHubStrings.PasteReplaceConfirmSafe),
                vietnamese: "Chỉ so sánh",
                english: "Only compare");
            table.Add(nameof(LiveOpsHubStrings.PasteReplaceUndoName),
                vietnamese: "Thay nháp bằng JSON đã dán",
                english: "Replace the draft with the pasted JSON");

            table.Add(nameof(LiveOpsHubStrings.PasteImportSaveDialogTitle),
                vietnamese: "Chọn nơi lưu asset lịch",
                english: "Choose where to save the calendar asset");
            table.Add(nameof(LiveOpsHubStrings.PasteImportUndoNameFormat),
                vietnamese: "Nhập JSON đang chạy vào {0}",
                english: "Import the running JSON into {0}");
            table.Add(nameof(LiveOpsHubStrings.PasteImportStampNote),
                vietnamese: "Nhập từ JSON đang chạy",
                english: "Imported from the running JSON");
            table.Add(nameof(LiveOpsHubStrings.PasteImportOutcomeHeadlineFormat),
                vietnamese: "Đã tạo {0} từ JSON đang chạy: {1} luật, {2} đợt.",
                english: "Created {0} from the running JSON: {1} rules, {2} events.");
            table.Add(nameof(LiveOpsHubStrings.PasteImportOutcomeUnknownTypesFormat),
                vietnamese: "{0} loại chưa khai báo — khai báo ở Loại event trước khi đăng.",
                english: "{0} undeclared types — declare them in Event types before publishing.");
            table.Add(nameof(LiveOpsHubStrings.PasteImportOutcomeReadyDetail),
                vietnamese: "Mọi loại đã khai báo — kiểm lịch rồi Copy JSON khi cần đăng.",
                english: "Every type is declared — check the calendar, then Copy JSON when you publish.");
            table.Add(nameof(LiveOpsHubStrings.PasteImportUndoneStepFormat),
                vietnamese: "nội dung (file {0} vẫn giữ)",
                english: "the content (the {0} file is kept)");
            table.Add(nameof(LiveOpsHubStrings.PasteImportCreateFailedTitle),
                vietnamese: "Không tạo được asset lịch",
                english: "Could not create the calendar asset");
            table.Add(nameof(LiveOpsHubStrings.PasteImportCreateFailedFormat),
                vietnamese: "Không tạo được asset: {0}",
                english: "Could not create the asset: {0}");
            table.Add(nameof(LiveOpsHubStrings.PasteImportOutsideProjectFormat),
                vietnamese: "Chỉ lưu được asset lịch trong thư mục Assets của project. Nơi bạn chọn nằm ngoài: {0}",
                english: "The calendar asset can only be saved inside the project's Assets folder. The place you chose is outside it: {0}");
            table.Add(nameof(LiveOpsHubStrings.PasteImportCreateFailedCloseButton),
                vietnamese: "Đóng",
                english: "Close");
        }
    }
}
