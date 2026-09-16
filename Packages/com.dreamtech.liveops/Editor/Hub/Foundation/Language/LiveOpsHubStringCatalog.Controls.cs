namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Controls — bản tiếng Việt là bản gốc (chép nguyên văn từ <c>LiveOpsHubStrings.Controls.cs</c>, không đổi một ký tự),
    /// bản tiếng Anh do gói dịch G-I18N bước 2 điền vào chỗ <c>english: null</c>. Hai ngôn ngữ nằm trong cùng một lệnh đăng ký để
    /// chỗ giữ chỗ <c>{0}</c>/<c>{1}</c> soát được bằng mắt và không bao giờ thêm khoá ở bản này mà quên bản kia.
    /// Comment "vì sao" của từng câu ở lại <c>LiveOpsHubStrings.Controls.cs</c> cạnh property — đó là chỗ người đọc code tìm tới.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterControls(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.UtcFieldDatePlaceholder),
                vietnamese: "yyyy-MM-dd",
                english: "yyyy-MM-dd");
            table.Add(nameof(LiveOpsHubStrings.UtcFieldTimePlaceholder),
                vietnamese: "HH:mm",
                english: "HH:mm");

            table.Add(nameof(LiveOpsHubStrings.UtcFieldDateUnreadableWithSuggestionFormat),
                vietnamese: "Không đọc được \"{0}\". Ô ngày cần dạng {1} (yyyy-MM-dd).",
                english: "Cannot read \"{0}\". The date field needs {1} (yyyy-MM-dd).");
            table.Add(nameof(LiveOpsHubStrings.UtcFieldDateUnreadableFormat),
                vietnamese: "Không đọc được \"{0}\". Ô ngày cần dạng yyyy-MM-dd, ví dụ 2026-09-16.",
                english: "Cannot read \"{0}\". The date field needs the yyyy-MM-dd format, for example 2026-09-16.");
            table.Add(nameof(LiveOpsHubStrings.UtcFieldDateEmpty),
                vietnamese: "Ô ngày còn trống. Ô ngày cần dạng yyyy-MM-dd, ví dụ 2026-09-16.",
                english: "The date field is empty. It needs the yyyy-MM-dd format, for example 2026-09-16.");
            table.Add(nameof(LiveOpsHubStrings.UtcFieldTimeEmpty),
                vietnamese: "Ô giờ còn trống. Ô giờ cần dạng HH:mm, ví dụ 07:00.",
                english: "The time field is empty. It needs the HH:mm format, for example 07:00.");
            table.Add(nameof(LiveOpsHubStrings.UtcFieldTimeUnreadableFormat),
                vietnamese: "Không đọc được \"{0}\". Ô giờ cần dạng HH:mm, ví dụ 07:00.",
                english: "Cannot read \"{0}\". The time field needs the HH:mm format, for example 07:00.");

            table.Add(nameof(LiveOpsHubStrings.TabStripChoiceOutOfRangeFormat),
                vietnamese: "Tab thứ {0} không tồn tại — dải tab chỉ có {1} lựa chọn.",
                english: "Tab {0} does not exist — the tab strip only has {1} choices.");

            table.Add(nameof(LiveOpsHubStrings.JsonViewFormattedTab),
                vietnamese: "Đã định dạng",
                english: "Formatted");
            table.Add(nameof(LiveOpsHubStrings.JsonViewSingleLineTab),
                vietnamese: "Một dòng",
                english: "Single line");

            table.AddShared(nameof(LiveOpsHubStrings.JsonGutterAdded), "+");
            table.AddShared(nameof(LiveOpsHubStrings.JsonGutterModified), "~");
            table.AddShared(nameof(LiveOpsHubStrings.JsonGutterRemoved), "–");

            table.Add(nameof(LiveOpsHubStrings.JsonOverviewDroppedTooltipFormat),
                vietnamese: "dòng {0} · bị bỏ",
                english: "line {0} · dropped");
            table.Add(nameof(LiveOpsHubStrings.JsonOverviewWarningTooltipFormat),
                vietnamese: "dòng {0} · cảnh báo",
                english: "line {0} · warning");
            table.Add(nameof(LiveOpsHubStrings.JsonOverviewChangeTooltipFormat),
                vietnamese: "dòng {0} · thay đổi",
                english: "line {0} · change");
            table.Add(nameof(LiveOpsHubStrings.JsonAnnotationLineOutOfRangeFormat),
                vietnamese: "Chú thích trỏ dòng {0} — số dòng bắt đầu từ 1.",
                english: "The annotation points at line {0} — line numbers start at 1.");
        }
    }
}
