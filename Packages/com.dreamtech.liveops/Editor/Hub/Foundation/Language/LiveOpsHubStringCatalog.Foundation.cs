namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Foundation — bản tiếng Việt là bản gốc (chép nguyên văn từ <c>LiveOpsHubStrings.Foundation.cs</c>, không đổi một ký tự),
    /// bản tiếng Anh do gói dịch G-I18N bước 2 điền vào chỗ <c>english: null</c>. Hai ngôn ngữ nằm trong cùng một lệnh đăng ký để
    /// chỗ giữ chỗ <c>{0}</c>/<c>{1}</c> soát được bằng mắt và không bao giờ thêm khoá ở bản này mà quên bản kia.
    /// Comment "vì sao" của từng câu ở lại <c>LiveOpsHubStrings.Foundation.cs</c> cạnh property — đó là chỗ người đọc code tìm tới.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterFoundation(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.StageCaptionConfigure),
                vietnamese: "CẤU HÌNH",
                english: "CONFIGURE");
            table.Add(nameof(LiveOpsHubStrings.StageCaptionSchedule),
                vietnamese: "LÊN LỊCH",
                english: "SCHEDULE");
            table.Add(nameof(LiveOpsHubStrings.StageCaptionCheck),
                vietnamese: "KIỂM",
                english: "CHECK");
            table.Add(nameof(LiveOpsHubStrings.StageCaptionExport),
                vietnamese: "XUẤT",
                english: "EXPORT");
            table.Add(nameof(LiveOpsHubStrings.StageCaptionRun),
                vietnamese: "CHẠY",
                english: "RUN");

            table.Add(nameof(LiveOpsHubStrings.StaleHealthReason),
                vietnamese: "Kết quả cũ — kiểm lại để cập nhật.",
                english: "Result is stale — check again to refresh.");
            table.Add(nameof(LiveOpsHubStrings.HealthReasonRequiredFormat),
                vietnamese: "Trạng thái {0} bắt buộc có lý do — người dùng phải biết vì sao và làm gì tiếp.",
                english: "State {0} must carry a reason — the user has to know why and what to do next.");
            table.Add(nameof(LiveOpsHubStrings.DisabledReasonRequired),
                vietnamese: "Nút bị khoá bắt buộc có lý do — nút disabled không nói vì sao là người dùng kẹt.",
                english: "A locked button must carry a reason — a disabled button that says nothing leaves the user stuck.");
            table.Add(nameof(LiveOpsHubStrings.FindingCountNegative),
                vietnamese: "Số phát hiện không được âm — nơi đếm theo đích đã tính sai.",
                english: "Finding count cannot be negative — the per-target counter miscounted.");

            table.Add(nameof(LiveOpsHubStrings.DurationDayUnit),
                vietnamese: "ngày",
                english: "days");
            table.Add(nameof(LiveOpsHubStrings.DurationHourUnit),
                vietnamese: "giờ",
                english: "hours");
            table.Add(nameof(LiveOpsHubStrings.DurationMinuteUnit),
                vietnamese: "phút",
                english: "minutes");
            table.Add(nameof(LiveOpsHubStrings.DurationDayUnitSingular),
                vietnamese: "ngày",
                english: "day");
            table.Add(nameof(LiveOpsHubStrings.DurationHourUnitSingular),
                vietnamese: "giờ",
                english: "hour");
            table.Add(nameof(LiveOpsHubStrings.DurationMinuteUnitSingular),
                vietnamese: "phút",
                english: "minute");
            table.Add(nameof(LiveOpsHubStrings.CompactDayUnit),
                vietnamese: "n",
                english: "d");
            table.Add(nameof(LiveOpsHubStrings.CompactHourUnit),
                vietnamese: "g",
                english: "h");
            table.Add(nameof(LiveOpsHubStrings.CompactMinuteUnit),
                vietnamese: "p",
                english: "m");

            table.Add(nameof(LiveOpsHubStrings.RelativeFuturePrefix),
                vietnamese: "sau",
                english: "in");
            table.Add(nameof(LiveOpsHubStrings.RelativePastSuffix),
                vietnamese: "trước",
                english: "ago");
            table.Add(nameof(LiveOpsHubStrings.RemainingPrefix),
                vietnamese: "còn",
                english: "time left:");

            table.Add(nameof(LiveOpsHubStrings.UtcLabel),
                vietnamese: "UTC",
                english: "UTC");
            table.Add(nameof(LiveOpsHubStrings.DeviceTimeSuffix),
                vietnamese: "giờ máy",
                english: "device time");
            table.Add(nameof(LiveOpsHubStrings.ByteUnit),
                vietnamese: "byte",
                english: "bytes");
            table.Add(nameof(LiveOpsHubStrings.IsoWeekPrefix),
                vietnamese: "Tuần",
                english: "Week");
            table.Add(nameof(LiveOpsHubStrings.Monday),
                vietnamese: "thứ Hai",
                english: "Monday");
            table.Add(nameof(LiveOpsHubStrings.Tuesday),
                vietnamese: "thứ Ba",
                english: "Tuesday");
            table.Add(nameof(LiveOpsHubStrings.Wednesday),
                vietnamese: "thứ Tư",
                english: "Wednesday");
            table.Add(nameof(LiveOpsHubStrings.Thursday),
                vietnamese: "thứ Năm",
                english: "Thursday");
            table.Add(nameof(LiveOpsHubStrings.Friday),
                vietnamese: "thứ Sáu",
                english: "Friday");
            table.Add(nameof(LiveOpsHubStrings.Saturday),
                vietnamese: "thứ Bảy",
                english: "Saturday");
            table.Add(nameof(LiveOpsHubStrings.Sunday),
                vietnamese: "Chủ nhật",
                english: "Sunday");

            table.AddShared(nameof(LiveOpsHubStrings.HubGlyphs), "…·–—×←→↑↓≥⌘⇧⌥⌫");

            table.AddShared(nameof(LiveOpsHubStrings.LanguageNameEnglish), "English");
            table.AddShared(nameof(LiveOpsHubStrings.LanguageNameVietnamese), "Tiếng Việt");
            table.AddShared(nameof(LiveOpsHubStrings.LanguageShortCodeEnglish), "EN");
            table.AddShared(nameof(LiveOpsHubStrings.LanguageShortCodeVietnamese), "VI");
            table.Add(nameof(LiveOpsHubStrings.LanguageMenuTooltip),
                vietnamese: "Ngôn ngữ của LiveOps Hub",
                english: "LiveOps Hub language");

            table.Add(nameof(LiveOpsHubStrings.KeyLabelCommand),
                vietnamese: "Cmd",
                english: "Cmd");
            table.Add(nameof(LiveOpsHubStrings.KeyLabelShift),
                vietnamese: "Shift",
                english: "Shift");
            table.Add(nameof(LiveOpsHubStrings.KeyLabelOption),
                vietnamese: "Alt",
                english: "Alt");
            table.Add(nameof(LiveOpsHubStrings.KeyLabelBackspace),
                vietnamese: "Backspace",
                english: "Backspace");
        }
    }
}
