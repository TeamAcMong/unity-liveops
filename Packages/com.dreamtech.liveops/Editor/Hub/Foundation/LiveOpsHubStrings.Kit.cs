namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ của bộ helper hub (vùng Kit, G-HUBKIT): câu toast chung, nhãn và gợi ý phím của hộp xác nhận, câu hậu quả
    /// dùng chung khi đụng đợt đang chạy (PD-17), nhãn nguồn tên người đăng, câu lỗi cú pháp JSON, và thông điệp lỗi lập
    /// trình của các model thuần. Mọi hằng mang tiền tố <c>Kit</c> vì lớp <c>partial</c> dùng chung với mọi vùng khác —
    /// tiền tố giữ cho hai gói song song không vô tình khai trùng tên.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Toast (8.5): toast sau Hoàn tác giữ câu cũ và thêm tiền tố, để người dùng biết chính thao tác nào vừa bị gỡ.
        internal static string KitToastUndonePrefix => LiveOpsHubStringCatalog.Text(nameof(KitToastUndonePrefix));

        // Hộp xác nhận (8.6): nút an toàn mặc định và gợi ý phím — không "Bạn có chắc?", không OK/Cancel.
        internal static string KitConfirmKeepLabel => LiveOpsHubStringCatalog.Text(nameof(KitConfirmKeepLabel));
        internal static string KitConfirmLevel1KeyHintFormat => LiveOpsHubStringCatalog.Text(nameof(KitConfirmLevel1KeyHintFormat));
        internal static string KitConfirmTypeToConfirmKeyHintFormat => LiveOpsHubStringCatalog.Text(nameof(KitConfirmTypeToConfirmKeyHintFormat));

        // Câu hậu quả P1 khi chưa có LiveOpsStateReader (7.0, PD-17): không in 0, không "không ai bị ảnh hưởng".
        internal static string KitUnknownPlayerCountSentence => LiveOpsHubStringCatalog.Text(nameof(KitUnknownPlayerCountSentence));
        internal static string KitNoTestDataSentence => LiveOpsHubStringCatalog.Text(nameof(KitNoTestDataSentence));

        // Nhãn mức xác nhận (bảng 7.0) — tooltip/palette nói cùng một chữ với bảng quyết định.
        internal static string KitRequirementNone => LiveOpsHubStringCatalog.Text(nameof(KitRequirementNone));
        internal static string KitRequirementLevel1 => LiveOpsHubStringCatalog.Text(nameof(KitRequirementLevel1));
        internal static string KitRequirementTypeToConfirm => LiveOpsHubStringCatalog.Text(nameof(KitRequirementTypeToConfirm));
        internal static string KitRequirementDedicatedDialog => LiveOpsHubStringCatalog.Text(nameof(KitRequirementDedicatedDialog));
        internal static string KitRequirementNotAllowed => LiveOpsHubStringCatalog.Text(nameof(KitRequirementNotAllowed));

        // Nguồn tên người đăng (hộp Đánh dấu đã đăng hiện nguồn để người dùng biết tên lấy từ đâu).
        internal static string KitPublisherSourceGit => LiveOpsHubStringCatalog.Text(nameof(KitPublisherSourceGit));
        internal static string KitPublisherSourceAccount => LiveOpsHubStringCatalog.Text(nameof(KitPublisherSourceAccount));

        // Lý do nút Dán/Nhập JSON đang chạy khi luồng đó chưa dựng ở bản dev (sổ nhánh tạm mục 12, dòng I-3). Câu nằm ở đây
        // vì chữ tiếng Việt chỉ được ở file chuỗi; hằng tạm của nhánh (có dấu) nằm ở file action tạm và chỉ trỏ tới câu này.
        internal static string KitActionNotBuiltInDevBuild => LiveOpsHubStringCatalog.Text(nameof(KitActionNotBuiltInDevBuild));

        // Lỗi cú pháp JSON (PD-13): "Dòng 3, ký tự 18: thiếu dấu phẩy".
        internal static string KitJsonSyntaxPositionFormat => LiveOpsHubStringCatalog.Text(nameof(KitJsonSyntaxPositionFormat));
        internal static string KitJsonSyntaxMissingComma => LiveOpsHubStringCatalog.Text(nameof(KitJsonSyntaxMissingComma));
        internal static string KitJsonSyntaxUnexpectedEnd => LiveOpsHubStringCatalog.Text(nameof(KitJsonSyntaxUnexpectedEnd));
        internal static string KitJsonSyntaxUnterminatedString => LiveOpsHubStringCatalog.Text(nameof(KitJsonSyntaxUnterminatedString));
        internal static string KitJsonSyntaxUnexpectedCharacter => LiveOpsHubStringCatalog.Text(nameof(KitJsonSyntaxUnexpectedCharacter));
        internal static string KitJsonSyntaxByteOrderMark => LiveOpsHubStringCatalog.Text(nameof(KitJsonSyntaxByteOrderMark));

        // Lỗi lập trình của model thuần (ArgumentException) — không bao giờ do dữ liệu lịch hỏng.
        internal static string KitErrorSectionIdEmpty => LiveOpsHubStringCatalog.Text(nameof(KitErrorSectionIdEmpty));
        internal static string KitErrorUnknownFilterConsequence => LiveOpsHubStringCatalog.Text(nameof(KitErrorUnknownFilterConsequence));
        internal static string KitErrorToastMessageEmpty => LiveOpsHubStringCatalog.Text(nameof(KitErrorToastMessageEmpty));
        internal static string KitErrorToastUndoGroupNegative => LiveOpsHubStringCatalog.Text(nameof(KitErrorToastUndoGroupNegative));
        internal static string KitErrorToastWithoutUndo => LiveOpsHubStringCatalog.Text(nameof(KitErrorToastWithoutUndo));
        internal static string KitErrorConfirmTitleEmpty => LiveOpsHubStringCatalog.Text(nameof(KitErrorConfirmTitleEmpty));
        internal static string KitErrorConfirmButtonLabelsEmpty => LiveOpsHubStringCatalog.Text(nameof(KitErrorConfirmButtonLabelsEmpty));
        internal static string KitErrorConfirmTypeToConfirmTextEmpty => LiveOpsHubStringCatalog.Text(nameof(KitErrorConfirmTypeToConfirmTextEmpty));
        internal static string KitErrorConfirmLevel1WithTypeText => LiveOpsHubStringCatalog.Text(nameof(KitErrorConfirmLevel1WithTypeText));
        internal static string KitErrorPolicyDraftAfterMissing => LiveOpsHubStringCatalog.Text(nameof(KitErrorPolicyDraftAfterMissing));
    }
}
