namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Export (G-EXPORT, W4) — bản tiếng Việt chép nguyên văn microcopy [SD2 §3.2], [SD2 §3.5], [SD2 §3.8],
    /// [SD2 §3.9], [SD2 §3.11]; bản tiếng Anh dịch cùng lệnh đăng ký để chỗ giữ chỗ <c>{0}</c> soát được bằng mắt.
    /// Comment "vì sao" của từng câu nằm cạnh property ở <c>LiveOpsHubStrings.Export.cs</c>.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterExport(LiveOpsHubStringTable table)
        {
            // ------------------------------------------------------------------------------------------ khung màn + header
            table.Add(nameof(LiveOpsHubStrings.ExportMissingLayoutFormat),
                vietnamese: "Thiếu bố cục màn Xuất JSON: {0}",
                english: "The Export JSON layout is missing: {0}");
            table.Add(nameof(LiveOpsHubStrings.ExportNoAssetTitle),
                vietnamese: "Chưa có lịch LiveOps trong project",
                english: "No LiveOps calendar in this project");
            table.Add(nameof(LiveOpsHubStrings.ExportNoAssetBody),
                vietnamese: "JSON xuất ra từ một LiveEventCalendarAsset — mở hoặc tạo lịch ở Tổng quan trước.",
                english: "The JSON is written from a LiveEventCalendarAsset — open or create one in Overview first.");
            table.Add(nameof(LiveOpsHubStrings.ExportNoAssetActionButton),
                vietnamese: "Mở Tổng quan để tạo lịch",
                english: "Open Overview to create a calendar");

            table.Add(nameof(LiveOpsHubStrings.ExportCopyButton),
                vietnamese: "Copy JSON",
                english: "Copy JSON");
            table.Add(nameof(LiveOpsHubStrings.ExportSaveFileButton),
                vietnamese: "Lưu file…",
                english: "Save file…");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkPublishedButton),
                vietnamese: "Đánh dấu đã đăng…",
                english: "Mark as published…");
            table.Add(nameof(LiveOpsHubStrings.ExportFooterNote),
                vietnamese: "Hub chỉ tạo JSON. Bạn tự dán vào Firebase rồi ghi dấu đã đăng (D5).",
                english: "The hub only writes the JSON. You paste it into Firebase yourself, then stamp it as published (D5).");

            // -------------------------------------------------------------------------------------------------------- card
            table.Add(nameof(LiveOpsHubStrings.ExportGateCardTitle),
                vietnamese: "Cổng xuất",
                english: "Export gate");
            table.Add(nameof(LiveOpsHubStrings.ExportJsonCardTitle),
                vietnamese: "JSON sẽ đăng",
                english: "JSON to publish");
            table.Add(nameof(LiveOpsHubStrings.ExportJsonCardMetaFormat),
                vietnamese: "parser của game giữ {0}/{1} mục · khớp Kiểm lịch",
                english: "the game parser keeps {0}/{1} entries · matches the calendar check");

            // ---------------------------------------------------------------------------------------------------- card diff
            table.Add(nameof(LiveOpsHubStrings.ExportDiffCardTitleFormat),
                vietnamese: "So với bản đã đăng · {0} UTC · sha {1}",
                english: "Compared to the published version · {0} UTC · sha {1}");
            table.Add(nameof(LiveOpsHubStrings.ExportDiffCardTitleRemoteFormat),
                vietnamese: "So với bản remote đã dán · dán lúc {0} · sha {1}",
                english: "Compared to the pasted remote version · pasted at {0} · sha {1}");
            table.Add(nameof(LiveOpsHubStrings.ExportDiffCardTitleNoStamp),
                vietnamese: "So với bản đã đăng",
                english: "Compared to the published version");
            table.Add(nameof(LiveOpsHubStrings.ExportDiffMetaFormat),
                vietnamese: "Đã xem {0}/{1} · bắt buộc {2}/{3}",
                english: "Reviewed {0}/{1} · required {2}/{3}");
            table.Add(nameof(LiveOpsHubStrings.ExportDiffChipAddedFormat),
                vietnamese: "Thêm {0}",
                english: "Added {0}");
            table.Add(nameof(LiveOpsHubStrings.ExportDiffChipChangedFormat),
                vietnamese: "Đổi {0}",
                english: "Changed {0}");
            table.Add(nameof(LiveOpsHubStrings.ExportDiffChipRemovedFormat),
                vietnamese: "Xoá {0}",
                english: "Removed {0}");
            table.Add(nameof(LiveOpsHubStrings.ExportDiffChipKeptFormat),
                vietnamese: "Giữ {0}",
                english: "Kept {0}");

            table.Add(nameof(LiveOpsHubStrings.ExportDiffGroupDroppedFormat),
                vietnamese: "BỊ BỎ KHI GAME ĐỌC LỊCH ({0})",
                english: "DROPPED WHEN THE GAME READS THE CALENDAR ({0})");
            table.Add(nameof(LiveOpsHubStrings.ExportDiffGroupProgressLostFormat),
                vietnamese: "NGƯỜI CHƠI MẤT TIẾN ĐỘ ({0})",
                english: "PLAYERS LOSE PROGRESS ({0})");
            table.Add(nameof(LiveOpsHubStrings.ExportDiffGroupShouldReviewFormat),
                vietnamese: "NÊN XEM ({0})",
                english: "WORTH A LOOK ({0})");
            table.Add(nameof(LiveOpsHubStrings.ExportDiffGroupSafeFormat),
                vietnamese: "AN TOÀN ({0})",
                english: "SAFE ({0})");

            table.Add(nameof(LiveOpsHubStrings.ExportDiffReviewedToggle),
                vietnamese: "Đã xem",
                english: "Reviewed");
            table.Add(nameof(LiveOpsHubStrings.ExportDiffRequiredTag),
                vietnamese: "bắt buộc",
                english: "required");
            table.Add(nameof(LiveOpsHubStrings.ExportDiffFixInValidationLink),
                vietnamese: "Sửa ở Kiểm lịch",
                english: "Fix in Calendar check");

            table.AddShared(nameof(LiveOpsHubStrings.ExportDiffSymbolAdded), "+");
            table.AddShared(nameof(LiveOpsHubStrings.ExportDiffSymbolChanged), "~");
            table.AddShared(nameof(LiveOpsHubStrings.ExportDiffSymbolProgressLost), "!");

            table.Add(nameof(LiveOpsHubStrings.ExportCompareBackToPublishedChip),
                vietnamese: "Về bản đã đăng",
                english: "Back to the published version");

            // ------------------------------------------------------------------------------------ card Các lần đã đăng
            table.Add(nameof(LiveOpsHubStrings.ExportHistoryCardTitle),
                vietnamese: "Các lần đã đăng",
                english: "Publish history");
            table.Add(nameof(LiveOpsHubStrings.ExportHistoryCardMeta),
                vietnamese: "chuột phải: So với nháp · Khôi phục vào nháp… · Gỡ dấu này… (lần mới nhất)",
                english: "right-click: Compare with the draft · Restore into the draft… · Remove this stamp… (latest only)");
            table.Add(nameof(LiveOpsHubStrings.ExportHistoryColumnTime),
                vietnamese: "Lúc (UTC)",
                english: "At (UTC)");
            table.Add(nameof(LiveOpsHubStrings.ExportHistoryColumnPublisher),
                vietnamese: "Người",
                english: "Who");
            table.Add(nameof(LiveOpsHubStrings.ExportHistoryColumnNote),
                vietnamese: "Ghi chú",
                english: "Note");
            table.Add(nameof(LiveOpsHubStrings.ExportHistoryColumnSha),
                vietnamese: "sha",
                english: "sha");
            table.Add(nameof(LiveOpsHubStrings.ExportHistoryActiveSuffix),
                vietnamese: "· đang là bản so",
                english: "· current comparison base");
            table.Add(nameof(LiveOpsHubStrings.ExportHistoryEmpty),
                vietnamese: "Chưa có dấu đã đăng nào",
                english: "No published stamp yet");
            table.Add(nameof(LiveOpsHubStrings.ExportHistoryNoNote),
                vietnamese: "(không ghi chú)",
                english: "(no note)");
            table.Add(nameof(LiveOpsHubStrings.ExportHistoryMenuCompare),
                vietnamese: "So với nháp",
                english: "Compare with the draft");
            table.Add(nameof(LiveOpsHubStrings.ExportHistoryMenuRestore),
                vietnamese: "Khôi phục vào nháp…",
                english: "Restore into the draft…");
            table.Add(nameof(LiveOpsHubStrings.ExportHistoryMenuRemoveStamp),
                vietnamese: "Gỡ dấu này…",
                english: "Remove this stamp…");
            table.Add(nameof(LiveOpsHubStrings.ExportHistoryCompareToastFormat),
                vietnamese: "So nháp với bản đã đăng {0}",
                english: "Comparing the draft with the version published {0}");

            // ---------------------------------------------------- hộp cấp 1: khôi phục vào nháp · gỡ dấu đã đăng
            table.Add(nameof(LiveOpsHubStrings.ExportRestoreConfirmTitleFormat),
                vietnamese: "Khôi phục bản {0} vào nháp?",
                english: "Restore the {0} version into the draft?");
            table.Add(nameof(LiveOpsHubStrings.ExportRestoreConfirmBodyFormat),
                vietnamese: "Nháp mất {0} thay đổi: {1}.",
                english: "The draft loses {0} changes: {1}.");
            table.Add(nameof(LiveOpsHubStrings.ExportRestoreConfirmBodyNoChanges),
                vietnamese: "Nháp đang giống bản này — khôi phục chỉ bỏ những sửa chưa lưu của loại và luật.",
                english: "The draft already matches this version — restoring only drops unsaved edits to types and rules.");
            table.Add(nameof(LiveOpsHubStrings.ExportRestoreConfirmKeyHint),
                vietnamese: "Enter / Esc: Giữ nháp",
                english: "Enter / Esc: Keep the draft");
            table.Add(nameof(LiveOpsHubStrings.ExportRestoreConfirmDestructive),
                vietnamese: "Khôi phục",
                english: "Restore");
            table.Add(nameof(LiveOpsHubStrings.ExportRestoreConfirmSafe),
                vietnamese: "Giữ nháp",
                english: "Keep the draft");
            table.Add(nameof(LiveOpsHubStrings.ExportRestoreUndoNameFormat),
                vietnamese: "Khôi phục bản đã đăng {0} vào nháp",
                english: "Restore the version published {0} into the draft");

            table.Add(nameof(LiveOpsHubStrings.ExportRemoveStampConfirmTitleFormat),
                vietnamese: "Gỡ dấu đã đăng {0}?",
                english: "Remove the stamp published {0}?");
            table.Add(nameof(LiveOpsHubStrings.ExportRemoveStampConfirmBodyFormat),
                vietnamese: "Bản so quay về {0} · sha {1}; Xuất JSON sẽ tính lại {2} thay đổi.",
                english: "The comparison base goes back to {0} · sha {1}; Export JSON will recount {2} changes.");
            table.Add(nameof(LiveOpsHubStrings.ExportRemoveStampConfirmBodyNoBaselineFormat),
                vietnamese: "Không còn dấu nào khác: Xuất JSON quay về lần đăng đầu và sẽ tính lại {0} thay đổi.",
                english: "No other stamp is left: Export JSON goes back to a first publish and will recount {0} changes.");
            table.Add(nameof(LiveOpsHubStrings.ExportRemoveStampConfirmDestructive),
                vietnamese: "Gỡ dấu",
                english: "Remove the stamp");
            table.Add(nameof(LiveOpsHubStrings.ExportRemoveStampConfirmSafe),
                vietnamese: "Giữ dấu",
                english: "Keep the stamp");
            table.Add(nameof(LiveOpsHubStrings.ExportRemoveStampToastFormat),
                vietnamese: "Gỡ dấu đã đăng {0}",
                english: "Removed the stamp published {0}");

            // ------------------------------------------------------------------------------------------------------ metric
            table.Add(nameof(LiveOpsHubStrings.ExportMetricSizeCaption),
                vietnamese: "KÍCH THƯỚC",
                english: "SIZE");
            table.Add(nameof(LiveOpsHubStrings.ExportMetricSizeUnit),
                vietnamese: "byte",
                english: "bytes");
            table.Add(nameof(LiveOpsHubStrings.ExportMetricSizeFootFormat),
                vietnamese: "{0} dòng",
                english: "{0} lines");
            table.Add(nameof(LiveOpsHubStrings.ExportMetricFormatCaption),
                vietnamese: "ĐỊNH DẠNG",
                english: "FORMAT");
            table.Add(nameof(LiveOpsHubStrings.ExportMetricFormatUnitVersion2),
                vietnamese: "có recurring",
                english: "with recurring");
            table.Add(nameof(LiveOpsHubStrings.ExportMetricFormatUnitVersion1),
                vietnamese: "không có recurring",
                english: "without recurring");
            table.AddShared(nameof(LiveOpsHubStrings.ExportMetricFormatChoice1), "1");
            table.AddShared(nameof(LiveOpsHubStrings.ExportMetricFormatChoice2), "2");
            table.AddShared(nameof(LiveOpsHubStrings.ExportMetricShaCaption), "SHA");
            table.Add(nameof(LiveOpsHubStrings.ExportMetricCompareCaption),
                vietnamese: "SO VỚI",
                english: "COMPARED TO");
            table.Add(nameof(LiveOpsHubStrings.ExportMetricCompareNone),
                vietnamese: "chưa đăng",
                english: "not published");
            table.Add(nameof(LiveOpsHubStrings.ExportMetricCompareFootEmpty),
                vietnamese: "không có thay đổi",
                english: "no changes");

            // ----------------------------------------------------------------------------------------------------- outcome
            table.Add(nameof(LiveOpsHubStrings.ExportOutcomeCopiedHeadlineFormat),
                vietnamese: "Đã copy {0} byte vào clipboard lúc {1} UTC · sha {2}",
                english: "Copied {0} bytes to the clipboard at {1} UTC · sha {2}");
            table.Add(nameof(LiveOpsHubStrings.ExportOutcomeCopiedDetail),
                vietnamese: "Dán vào key liveops_calendar rồi bấm Đánh dấu đã đăng",
                english: "Paste it into key liveops_calendar, then press Mark as published");
            table.Add(nameof(LiveOpsHubStrings.ExportOutcomeSavedHeadlineFormat),
                vietnamese: "Đã lưu {0} ({1} byte) vào {2}",
                english: "Saved {0} ({1} bytes) to {2}");
            table.Add(nameof(LiveOpsHubStrings.ExportOutcomeSavedDetail),
                vietnamese: "Dán nội dung file vào key liveops_calendar rồi bấm Đánh dấu đã đăng",
                english: "Paste the file contents into key liveops_calendar, then press Mark as published");
            table.Add(nameof(LiveOpsHubStrings.ExportOutcomeMarkedHeadlineFormat),
                vietnamese: "Đã ghi dấu đã đăng {0} UTC · sha {1} · {2}",
                english: "Stamped as published {0} UTC · sha {1} · {2}");
            table.Add(nameof(LiveOpsHubStrings.ExportOutcomeMarkedDetailFormat),
                vietnamese: "Đã lưu {0} — nhớ commit để team thấy dấu này",
                english: "Saved {0} — remember to commit so the team sees this stamp");
            table.Add(nameof(LiveOpsHubStrings.ExportSaveFilePanelTitle),
                vietnamese: "Lưu JSON lịch LiveOps",
                english: "Save the LiveOps calendar JSON");

            // ------------------------------------------------------------------------- hộp Đánh dấu đã đăng [SD2 §3.11]
            table.Add(nameof(LiveOpsHubStrings.ExportMarkWindowTitle),
                vietnamese: "Đánh dấu đã đăng",
                english: "Mark as published");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkHeadingReadyFormat),
                vietnamese: "Ghi dấu đã đăng cho sha {0}",
                english: "Stamp sha {0} as published");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkHeadingDraftChangedFormat),
                vietnamese: "Nháp đã đổi sau lần copy {0}",
                english: "The draft changed after the {0} copy");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkBodyFormat),
                vietnamese: "Hub sẽ ghi vào {0}: {1} UTC, {2} ({3}), sha {4}, {5} byte và bản chụp JSON để so lần sau, rồi lưu {0}. Hub không gửi gì lên Firebase.",
                english: "The hub writes into {0}: {1} UTC, {2} ({3}), sha {4}, {5} bytes and a JSON snapshot for the next comparison, then saves {0}. The hub sends nothing to Firebase.");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkBodyDraftChangedFormat),
                vietnamese: "Nháp hiện tại có sha {0}, khác JSON bạn đã copy ({1}). Ghi dấu lúc này sẽ lưu một bản chụp khác thứ đã dán lên Firebase, và mọi lần so sau đều lệch.",
                english: "The current draft has sha {0}, which differs from the JSON you copied ({1}). Stamping now would store a snapshot different from what is on Firebase, and every later comparison would be wrong.");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkShaOkFormat),
                vietnamese: "Bạn đã copy sha {0} lúc {1} UTC — khớp nháp",
                english: "You copied sha {0} at {1} UTC — it matches the draft");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkShaBlockedFormat),
                vietnamese: "{0} → {1} — copy bản mới rồi dán lại vào liveops_calendar",
                english: "{0} → {1} — copy the new version and paste it into liveops_calendar again");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkShaCopiedFormat),
                vietnamese: "Đã copy {0} lúc {1} — dán lại vào liveops_calendar rồi tick",
                english: "Copied {0} at {1} — paste it into liveops_calendar again, then tick");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkCopyNewButtonFormat),
                vietnamese: "Copy JSON {0}",
                english: "Copy JSON {0}");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkConfirmToggle),
                vietnamese: "Tôi đã dán đúng JSON này vào remote config key `liveops_calendar`",
                english: "I pasted exactly this JSON into remote config key `liveops_calendar`");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkNoteLabel),
                vietnamese: "Ghi chú (bắt buộc)",
                english: "Note (required)");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkNotePlaceholder),
                vietnamese: "vd: mở hunt-0916-bonus cho sự kiện giữa tháng",
                english: "e.g. opened hunt-0916-bonus for the mid-month event");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkCancelButton),
                vietnamese: "Huỷ",
                english: "Cancel");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkConfirmButton),
                vietnamese: "Ghi dấu đã đăng",
                english: "Stamp as published");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkMissingPrefix),
                vietnamese: "Còn thiếu: ",
                english: "Still missing: ");
            table.AddShared(nameof(LiveOpsHubStrings.ExportMarkMissingSeparator), ", ");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkMissingConfirm),
                vietnamese: "xác nhận đã dán",
                english: "the paste confirmation");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkMissingNote),
                vietnamese: "ghi chú",
                english: "the note");
            table.Add(nameof(LiveOpsHubStrings.ExportMarkMissingCopy),
                vietnamese: "copy bản mới",
                english: "a copy of the new version");

            table.Add(nameof(LiveOpsHubStrings.ExportErrorServicesMissing),
                vietnamese: "Màn Xuất JSON cần services của cửa sổ",
                english: "The Export JSON screen needs the window services");
        }
    }
}
