namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng ValidationDepth (chiều sâu màn Kiểm lịch) — bản tiếng Việt chép nguyên văn từ thiết kế [SD2 §2.4–2.7],
    /// bản tiếng Anh dịch cùng lúc (G-I18N §4). Comment "vì sao" của từng câu ở <c>LiveOpsHubStrings.ValidationDepth.cs</c>.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterValidationDepth(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthBulkPreviewTitleFormat),
                vietnamese: "Sẽ áp {0} thay đổi an toàn",
                english: "Will apply {0} {0|safe change|safe changes}");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthBulkPreviewMeta),
                vietnamese: "không đổi điều người chơi thấy",
                english: "nothing players see will change");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthBulkPreviewChangeFormat),
                vietnamese: "{0} {1} {2} → {3}",
                english: "{0} {1} {2} → {3}");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthBulkPreviewChangeNoFieldFormat),
                vietnamese: "{0} {1} → {2}",
                english: "{0} {1} → {2}");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthBulkPreviewUndoFormat),
                vietnamese: "Undo group: {0}",
                english: "Undo group: {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthBulkPreviewApplyFormat),
                vietnamese: "Áp {0} thay đổi",
                english: "Apply {0} {0|change|changes}");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthBulkPreviewCancelButton),
                vietnamese: "Huỷ",
                english: "Cancel");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthBulkPreviewNothingReason),
                vietnamese: "Bỏ chọn hết rồi: không còn thay đổi nào để áp",
                english: "Everything is unticked: there is no change left to apply");

            table.Add(nameof(LiveOpsHubStrings.ValidationDepthIgnoreHeader),
                vietnamese: "Bỏ qua cảnh báo",
                english: "Ignore this warning");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthIgnoreScopeFormat),
                vietnamese: "Chỉ khoảng {0} của {1} — hiện lại nếu khoảng này đổi",
                english: "Only the {0} range of {1} — it comes back if that range changes");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthIgnoreScopeEveryRange),
                vietnamese: "Cảnh báo này không gắn khoảng nào — bỏ qua áp cho mọi khoảng",
                english: "This warning has no range — ignoring applies to every range");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthIgnoreNoteLabel),
                vietnamese: "Ghi chú (bắt buộc)",
                english: "Note (required)");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthIgnoreFooterFormat),
                vietnamese: "lưu trong {0}",
                english: "saved in {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthIgnoreCancelButton),
                vietnamese: "Huỷ",
                english: "Cancel");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthIgnoreConfirmButton),
                vietnamese: "Bỏ qua",
                english: "Ignore");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthIgnoreNoteRequiredReason),
                vietnamese: "Chưa có ghi chú: cảnh báo bị ẩn phải nói được vì sao",
                english: "No note yet: a hidden warning has to say why");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthIgnoreUndoFormat),
                vietnamese: "Bỏ qua {0} · {1}",
                english: "Ignore {0} · {1}");

            table.Add(nameof(LiveOpsHubStrings.ValidationDepthUnignoreButton),
                vietnamese: "Bỏ bỏ qua",
                english: "Stop ignoring");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthUnignoreUndoFormat),
                vietnamese: "Bỏ bỏ qua {0} · {1}",
                english: "Stop ignoring {0} · {1}");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthIgnoredMetaFormat),
                vietnamese: "{0} · {1} · {2}",
                english: "{0} · {1} · {2}");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthIgnoredMetaWithTagFormat),
                vietnamese: "{0} · {1}",
                english: "{0} · {1}");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthIgnoredNoteEmpty),
                vietnamese: "(chưa ghi chú)",
                english: "(no note)");
            // Dấu câu dùng chung: AddShared để bảng tiếng Anh không phải chép lại một ký tự y hệt.
            table.AddShared(nameof(LiveOpsHubStrings.ValidationDepthIgnoredNoteEllipsis), "…");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthViewNoteMenuItem),
                vietnamese: "Xem ghi chú",
                english: "View the note");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthIgnoredNoteCardTitleFormat),
                vietnamese: "Ghi chú bỏ qua · {0}",
                english: "Ignore note · {0}");

            table.Add(nameof(LiveOpsHubStrings.ValidationDepthContextQuickFixMenuItem),
                vietnamese: "Sửa nhanh…",
                english: "Quick fix…");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthContextCopyDescriptionMenuItem),
                vietnamese: "Copy mô tả lỗi",
                english: "Copy the error description");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthContextOpenRuleDocumentationFormat),
                vietnamese: "Mở tài liệu luật {0}",
                english: "Open the rule documentation {0}");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthMenuDisabledReasonFormat),
                vietnamese: "{0} — {1}",
                english: "{0} — {1}");
            table.Add(nameof(LiveOpsHubStrings.ValidationDepthContextQuickFixNoRepairReason),
                vietnamese: "phát hiện này không có lệnh sửa nào",
                english: "this finding has no repair");

            table.Add(nameof(LiveOpsHubStrings.ValidationDepthDecisionAppliedFormat),
                vietnamese: "Quyết định cho {0}: {1}",
                english: "Decision for {0}: {1}");
        }
    }
}
