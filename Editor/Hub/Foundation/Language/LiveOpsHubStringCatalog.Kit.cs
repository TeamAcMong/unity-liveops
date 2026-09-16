namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Kit — bản tiếng Việt là bản gốc (chép nguyên văn từ <c>LiveOpsHubStrings.Kit.cs</c>, không đổi một ký tự),
    /// bản tiếng Anh do gói dịch G-I18N bước 2 điền vào chỗ <c>english: null</c>. Hai ngôn ngữ nằm trong cùng một lệnh đăng ký để
    /// chỗ giữ chỗ <c>{0}</c>/<c>{1}</c> soát được bằng mắt và không bao giờ thêm khoá ở bản này mà quên bản kia.
    /// Comment "vì sao" của từng câu ở lại <c>LiveOpsHubStrings.Kit.cs</c> cạnh property — đó là chỗ người đọc code tìm tới.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterKit(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.KitToastUndonePrefix),
                vietnamese: "Đã hoàn tác: ",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.KitConfirmKeepLabel),
                vietnamese: "Giữ lại",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitConfirmLevel1KeyHintFormat),
                vietnamese: "Enter / Esc: {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitConfirmTypeToConfirmKeyHintFormat),
                vietnamese: "Esc: {0} · Enter không đổi gì",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.KitUnknownPlayerCountSentence),
                vietnamese: "Hub không biết số người chơi toàn cục đang ở đợt này.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitNoTestDataSentence),
                vietnamese: "Bản hub này chưa đọc dữ liệu thử trong Editor, nên không có số để nêu.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.KitRequirementNone),
                vietnamese: "Không hỏi · có Hoàn tác",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitRequirementLevel1),
                vietnamese: "Hỏi một lần",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitRequirementTypeToConfirm),
                vietnamese: "Gõ id đang chạy để xác nhận",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitRequirementDedicatedDialog),
                vietnamese: "Hộp riêng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitRequirementNotAllowed),
                vietnamese: "Không cho",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.KitPublisherSourceGit),
                vietnamese: "git user.name",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitPublisherSourceAccount),
                vietnamese: "tên tài khoản máy",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.KitActionNotBuiltInDevBuild),
                vietnamese: "Chưa có trong bản dev này",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.KitJsonSyntaxPositionFormat),
                vietnamese: "Dòng {0}, ký tự {1}: {2}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitJsonSyntaxMissingComma),
                vietnamese: "thiếu dấu phẩy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitJsonSyntaxUnexpectedEnd),
                vietnamese: "JSON kết thúc giữa chừng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitJsonSyntaxUnterminatedString),
                vietnamese: "chuỗi chưa đóng dấu nháy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitJsonSyntaxUnexpectedCharacter),
                vietnamese: "ký tự không đúng chỗ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitJsonSyntaxByteOrderMark),
                vietnamese: "đầu JSON có ký tự BOM (U+FEFF) mà parser của game không đọc được — lưu lại dạng UTF-8 không BOM",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.KitErrorSectionIdEmpty),
                vietnamese: "Id màn không được rỗng.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitErrorUnknownFilterConsequence),
                vietnamese: "Bộ lọc hậu quả không có trong danh sách: ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitErrorToastMessageEmpty),
                vietnamese: "Câu toast không được rỗng.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitErrorToastUndoGroupNegative),
                vietnamese: "Toast có Hoàn tác phải có Undo group không âm.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitErrorToastWithoutUndo),
                vietnamese: "Toast không có Undo group thì không có trạng thái đã hoàn tác.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitErrorConfirmTitleEmpty),
                vietnamese: "Hộp xác nhận phải có tiêu đề.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitErrorConfirmButtonLabelsEmpty),
                vietnamese: "Hộp xác nhận phải có nhãn nút phá huỷ và nút an toàn.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitErrorConfirmTypeToConfirmTextEmpty),
                vietnamese: "Hộp gõ để xác nhận phải có chuỗi cần gõ.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitErrorConfirmLevel1WithTypeText),
                vietnamese: "Hộp cấp 1 không có ô gõ — dùng WithTypeToConfirm cho cấp 2.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.KitErrorPolicyDraftAfterMissing),
                vietnamese: "Thao tác đổi mục cần tài liệu sau khi sửa.",
                english: null);
        }
    }
}
