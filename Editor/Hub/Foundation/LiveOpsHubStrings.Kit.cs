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
        internal const string KitToastUndonePrefix = "Đã hoàn tác: ";

        // Hộp xác nhận (8.6): nút an toàn mặc định và gợi ý phím — không "Bạn có chắc?", không OK/Cancel.
        internal const string KitConfirmKeepLabel = "Giữ lại";
        internal const string KitConfirmLevel1KeyHintFormat = "Enter / Esc: {0}";
        internal const string KitConfirmTypeToConfirmKeyHintFormat = "Esc: {0} · Enter không đổi gì";

        // Câu hậu quả P1 khi chưa có LiveOpsStateReader (7.0, PD-17): không in 0, không "không ai bị ảnh hưởng".
        internal const string KitUnknownPlayerCountSentence = "Hub không biết số người chơi toàn cục đang ở đợt này.";
        internal const string KitNoTestDataSentence = "Bản hub này chưa đọc dữ liệu thử trong Editor, nên không có số để nêu.";

        // Nhãn mức xác nhận (bảng 7.0) — tooltip/palette nói cùng một chữ với bảng quyết định.
        internal const string KitRequirementNone = "Không hỏi · có Hoàn tác";
        internal const string KitRequirementLevel1 = "Hỏi một lần";
        internal const string KitRequirementTypeToConfirm = "Gõ id đang chạy để xác nhận";
        internal const string KitRequirementDedicatedDialog = "Hộp riêng";
        internal const string KitRequirementNotAllowed = "Không cho";

        // Nguồn tên người đăng (hộp Đánh dấu đã đăng hiện nguồn để người dùng biết tên lấy từ đâu).
        internal const string KitPublisherSourceGit = "git user.name";
        internal const string KitPublisherSourceAccount = "tên tài khoản máy";

        // Lý do nút Dán/Nhập JSON đang chạy khi luồng đó chưa dựng ở bản dev (sổ nhánh tạm mục 12, dòng I-3). Câu nằm ở đây
        // vì chữ tiếng Việt chỉ được ở file chuỗi; hằng tạm của nhánh (có dấu) nằm ở file action tạm và chỉ trỏ tới câu này.
        internal const string KitActionNotBuiltInDevBuild = "Chưa có trong bản dev này";

        // Lỗi cú pháp JSON (PD-13): "Dòng 3, ký tự 18: thiếu dấu phẩy".
        internal const string KitJsonSyntaxPositionFormat = "Dòng {0}, ký tự {1}: {2}";
        internal const string KitJsonSyntaxMissingComma = "thiếu dấu phẩy";
        internal const string KitJsonSyntaxUnexpectedEnd = "JSON kết thúc giữa chừng";
        internal const string KitJsonSyntaxUnterminatedString = "chuỗi chưa đóng dấu nháy";
        internal const string KitJsonSyntaxUnexpectedCharacter = "ký tự không đúng chỗ";
        internal const string KitJsonSyntaxByteOrderMark = "đầu JSON có ký tự BOM (U+FEFF) mà parser của game không đọc được — lưu lại dạng UTF-8 không BOM";

        // Lỗi lập trình của model thuần (ArgumentException) — không bao giờ do dữ liệu lịch hỏng.
        internal const string KitErrorSectionIdEmpty = "Id màn không được rỗng.";
        internal const string KitErrorUnknownFilterConsequence = "Bộ lọc hậu quả không có trong danh sách: ";
        internal const string KitErrorToastMessageEmpty = "Câu toast không được rỗng.";
        internal const string KitErrorToastUndoGroupNegative = "Toast có Hoàn tác phải có Undo group không âm.";
        internal const string KitErrorToastWithoutUndo = "Toast không có Undo group thì không có trạng thái đã hoàn tác.";
        internal const string KitErrorConfirmTitleEmpty = "Hộp xác nhận phải có tiêu đề.";
        internal const string KitErrorConfirmButtonLabelsEmpty = "Hộp xác nhận phải có nhãn nút phá huỷ và nút an toàn.";
        internal const string KitErrorConfirmTypeToConfirmTextEmpty = "Hộp gõ để xác nhận phải có chuỗi cần gõ.";
        internal const string KitErrorConfirmLevel1WithTypeText = "Hộp cấp 1 không có ô gõ — dùng WithTypeToConfirm cho cấp 2.";
        internal const string KitErrorPolicyDraftAfterMissing = "Thao tác đổi mục cần tài liệu sau khi sửa.";
    }
}
