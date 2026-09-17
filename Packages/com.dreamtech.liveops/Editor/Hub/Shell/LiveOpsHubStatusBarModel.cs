using System;
using System.Globalization;
using System.Text;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ của status bar 20 px (8.3, [FD §3.4]) — model thuần, không đụng Editor, nên mọi câu (kể cả câu "cũ" vì mốc thời gian)
    /// test được bằng đồng hồ tay ở category Logic. View <see cref="LiveOpsHubStatusBar"/> chỉ đổ chữ.
    /// <para>
    /// Trái = một câu về lần Kiểm lịch gần nhất. Bốn ca, và ba trong bốn ca KHÔNG có dấu trạng thái (<see cref="LeftMark"/> null =
    /// vòng rỗng): chưa kiểm và hai kiểu "cũ" không có bằng chứng để đeo dấu — in dấu Ok/Blocked lúc đó là nói dối về một kết quả
    /// không còn đúng. Chỉ ca "kết quả còn mới" mới mang dấu, và dấu đó là mức xấu nhất của chính lần kiểm ([FD §3.4] chú thích 8).
    /// </para>
    /// <para>
    /// Phần " · Vừa làm: … (⌘Z)" nối vào câu trái để thao tác cuối không mất khi toast tắt sau 6 giây. Nhãn "(⌘Z)" chỉ còn khi bước
    /// Undo đó vẫn trên đỉnh stack chung của Editor (<paramref name="isLastActionOnTop"/>) — bấm ⌘Z lúc không còn trên đỉnh sẽ gỡ
    /// thao tác của người khác, nên câu không được mời làm việc đó.
    /// </para>
    /// </summary>
    internal sealed class LiveOpsHubStatusBarModel
    {
        private LiveOpsHubStatusBarModel(HealthState? leftMark, string leftText, string rightText, string rightTooltip)
        {
            LeftMark = leftMark;
            LeftText = leftText ?? string.Empty;
            RightText = rightText ?? string.Empty;
            RightTooltip = rightTooltip ?? string.Empty;
        }

        /// <param name="check">null = chưa có phiên lịch (khung trần của W2) — câu trái để trống, không vẽ vòng rỗng vô nghĩa.</param>
        /// <param name="hasCalendarAsset">
        /// false = phiên có nhưng KHÔNG có asset lịch. Câu trái nói đúng chuyện đó thay vì mời bấm F5: không có asset thì F5 không
        /// chạy được lần kiểm nào và mục "Kiểm lại tất cả (F5)" của menu ⋮ đã disabled (7.0).
        /// </param>
        /// <param name="lastActionText">Câu của thao tác Undo được gần nhất ("" = chưa làm gì trong phiên).</param>
        /// <param name="isLastActionOnTop">Bước Undo của thao tác đó còn là bước kế tiếp của ⌘Z.</param>
        /// <param name="activeStamp">Dấu đã đăng đang chọn; null = chưa từng ghi dấu (phần "đã đăng …· sha …" biến mất).</param>
        /// <param name="undoKeyLabel">Nhãn phím Undo thật của người dùng ("⌘Z", "Ctrl Z"); "" = không gán phím → bỏ luôn ngoặc.</param>
        public static LiveOpsHubStatusBarModel Build(LiveOpsHubCheckState check, bool hasCalendarAsset, string lastActionText,
            bool isLastActionOnTop, DateTime nowUtc, PublishedCalendarStamp activeStamp, LiveOpsHubFormat format, string undoKeyLabel)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            DateTime utcNow = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);

            HealthState? leftMark;
            string leftText = BuildCheckSentence(check, hasCalendarAsset, out leftMark);
            leftText = AppendRecentAction(leftText, lastActionText, isLastActionOnTop, undoKeyLabel);

            return new LiveOpsHubStatusBarModel(leftMark, leftText, BuildRightText(utcNow, activeStamp, format),
                BuildRightTooltip(utcNow, format));
        }

        /// <summary>null = vòng rỗng (chưa kiểm hoặc kết quả đã cũ) — xem tóm tắt lớp.</summary>
        public HealthState? LeftMark { get; }

        public string LeftText { get; }

        public string RightText { get; }

        public string RightTooltip { get; }

        // ------------------------------------------------------------------------------------------------------------ trái

        private static string BuildCheckSentence(LiveOpsHubCheckState check, bool hasCalendarAsset, out HealthState? mark)
        {
            mark = null;
            if (check == null) return string.Empty;
            // Không có asset: nói "chưa có lịch", không mời F5. Xét TRƯỚC mọi ca kiểm vì phiên không asset vẫn mang
            // StaleReason = NeverChecked, và câu "Chưa kiểm lần nào — F5 để kiểm" ở đó là một lời mời vào phím không làm gì.
            if (!hasCalendarAsset) return LiveOpsHubStrings.ShellStatusNoCalendarAsset;

            if (check.IsRunning)
            {
                // Lần kiểm đang chạy: số luật đã xong là con số duy nhất đúng lúc này — không nêu lại con số của báo cáo cũ.
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellStatusCheckingFormat,
                    check.CompletedRuleCount, check.RuleCount);
            }

            switch (check.StaleReason)
            {
                case LiveOpsHubCheckStaleReason.NeverChecked:
                    return LiveOpsHubStrings.ShellStatusNeverChecked;

                case LiveOpsHubCheckStaleReason.MilestonePassed:
                    return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellStatusStaleMilestoneFormat,
                        MilestoneText(check), ClockWithSeconds(check.CheckedAtUtc));

                case LiveOpsHubCheckStaleReason.CalendarEdited:
                    return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellStatusStaleEditedFormat,
                        ClockWithSeconds(check.CalendarEditedUtc), ClockWithSeconds(check.CheckedAtUtc));

                case LiveOpsHubCheckStaleReason.InterruptedByReload:
                    return LiveOpsHubStrings.ShellStatusStaleInterrupted;
            }

            if (check.LastReport == null) return LiveOpsHubStrings.ShellStatusNeverChecked;

            LiveEventCalendarCheckSummary summary = check.LastReport.Summary;
            // Dấu của ca "còn mới" = mức xấu nhất của chính lần kiểm; không có hậu quả nào thì Ok.
            mark = summary.WorstConsequence.HasValue
                ? LiveOpsHubFindingRouting.StateOf(summary.WorstConsequence.Value)
                : HealthState.Ok;
            return LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ShellStatusCheckedFormat),
                ClockWithSeconds(check.CheckedAtUtc), summary.RuleCount, summary.NeedsActionCount);
        }

        /// <summary>
        /// Mốc đã qua in đủ ngày + giờ (câu nói về một thời điểm trong tương lai của lịch, không phải trong phiên), còn giờ của lần
        /// kiểm in dạng đồng hồ có giây như mọi chỗ khác của status bar.
        /// </summary>
        private static string MilestoneText(LiveOpsHubCheckState check)
        {
            if (!check.PassedMilestoneUtc.HasValue) return string.Empty;
            DateTime milestone = check.PassedMilestoneUtc.Value;
            return DayMonth(milestone) + " " + milestone.ToString("HH:mm", CultureInfo.InvariantCulture) + " " + LiveOpsHubStrings.UtcLabel;
        }

        private static string AppendRecentAction(string sentence, string lastActionText, bool isLastActionOnTop, string undoKeyLabel)
        {
            if (string.IsNullOrEmpty(lastActionText)) return sentence;

            string action = isLastActionOnTop && !string.IsNullOrEmpty(undoKeyLabel)
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellStatusRecentActionWithKeyFormat, lastActionText, undoKeyLabel)
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellStatusRecentActionFormat, lastActionText);

            if (sentence.Length == 0) return action;
            StringBuilder builder = new StringBuilder(sentence.Length + action.Length + 3);
            builder.Append(sentence).Append(LiveOpsHubStrings.ShellRailPartSeparator).Append(action);
            return builder.ToString();
        }

        // ------------------------------------------------------------------------------------------------------------ phải

        private static string BuildRightText(DateTime nowUtc, PublishedCalendarStamp activeStamp, LiveOpsHubFormat format)
        {
            string now = format.ShortDateTimeUtc(nowUtc);
            DateTime publishedUtc;
            if (activeStamp == null || !activeStamp.TryGetPublishedUtc(out publishedUtc))
            {
                // Chưa ghi dấu (hoặc dấu có giờ không đọc được): chỉ in giờ hiện tại — bịa "đã đăng —" sẽ thành một dấu không có thật.
                return now;
            }
            return now + LiveOpsHubStrings.ShellRailPartSeparator
                + string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellStatusPublishedFormat, format.ShortDateTime(publishedUtc))
                + LiveOpsHubStrings.ShellRailPartSeparator
                + string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellStatusShaFormat, activeStamp.ShortSha);
        }

        private static string BuildRightTooltip(DateTime nowUtc, LiveOpsHubFormat format)
        {
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellStatusDeviceTimeFormat,
                format.DeviceClock(nowUtc), LiveOpsHubStrings.DeviceTimeSuffix, format.DeviceOffsetLabel);
        }

        // ------------------------------------------------------------------------------------------------------------ giờ

        /// <summary>
        /// "08:46:58" — status bar là chỗ DUY NHẤT của hub in giây: hai lần kiểm cách nhau vài giây phải phân biệt được, nên
        /// <see cref="LiveOpsHubFormat"/> (chỉ có phút) không đủ. Giây không rò sang chỗ khác vì hằng định dạng nằm ngay đây.
        /// </summary>
        private static string ClockWithSeconds(DateTime? utc)
        {
            return utc.HasValue ? utc.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture) : string.Empty;
        }

        /// <summary>"14/9" — không số 0 đầu, cùng luật với <see cref="LiveOpsHubFormat"/> ([FD §6.1]).</summary>
        private static string DayMonth(DateTime utc)
        {
            return utc.Day.ToString(CultureInfo.InvariantCulture) + "/" + utc.Month.ToString(CultureInfo.InvariantCulture);
        }
    }
}
