using System;
using System.Globalization;
using System.Text;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bộ định dạng chữ ngày/giờ/số của hub theo [FD §6.1]: "16/9 12:00", "3/10/2026", "15 giờ 13 phút", "1.601 byte",
    /// "48,4%". Mọi số viết bằng <see cref="CultureInfo.InvariantCulture"/> rồi mới đổi dấu, vì hub phải in cùng một chữ
    /// trên máy locale vi-VN, fr-FR hay en-US (<c>Format_IndependentOfThreadCulture</c>) — dùng culture của thread thì
    /// ảnh chụp và test lệch theo máy. Giờ máy chỉ là dòng phụ: độ lệch tiêm từ port múi giờ để test cố định +7.
    /// </summary>
    internal sealed class LiveOpsHubFormat
    {
        private const int DaysPerWeek = 7;
        private const int IsoWeekOffsetDays = 10;
        private const int LastIsoWeekCandidate = 53;

        private readonly TimeSpan _deviceOffset;

        public LiveOpsHubFormat(TimeSpan deviceOffset)
        {
            _deviceOffset = deviceOffset;
            DeviceOffsetLabel = BuildOffsetLabel(deviceOffset);
        }

        /// <summary>"UTC+7", "UTC-3:30", "UTC" khi lệch 0.</summary>
        public string DeviceOffsetLabel { get; }

        /// <summary>"16/9 12:00" — giờ UTC, không số 0 đầu ở ngày/tháng ("07/9" là cách viết cấm).</summary>
        public string ShortDateTime(DateTime utc)
        {
            return DayMonth(utc) + " " + Clock(utc);
        }

        /// <summary>"16/9 12:00 UTC".</summary>
        public string ShortDateTimeUtc(DateTime utc)
        {
            return ShortDateTime(utc) + " " + LiveOpsHubStrings.UtcLabel;
        }

        /// <summary>"3/10/2026".</summary>
        public string DateWithYear(DateTime utc)
        {
            return DayMonth(utc) + "/" + utc.Year.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>"19:00 16/9 giờ máy" — luôn có chữ "giờ máy" để không bị đọc thành giờ UTC.</summary>
        public string DeviceTimeLine(DateTime utc)
        {
            DateTime device = ToDevice(utc);
            return Clock(device) + " " + DayMonth(device) + " " + LiveOpsHubStrings.DeviceTimeSuffix;
        }

        /// <summary>"07:00" giờ máy (không hậu tố — nơi gọi tự đặt ngữ cảnh, vd tầng thước giờ máy).</summary>
        public string DeviceClock(DateTime utc)
        {
            return Clock(ToDevice(utc));
        }

        /// <summary>
        /// "15 giờ 13 phút" · có ngày thì bỏ phút "3 ngày 18 giờ" · <paramref name="compact"/> (cửa sổ --compact) "15g 13p".
        /// Phần bằng 0 bị bỏ ("15 giờ", "3 ngày"); dưới một phút in "0 phút". Thời lượng âm in theo trị tuyệt đối — dấu
        /// trước/sau là việc của <see cref="Relative"/>.
        /// </summary>
        public string Duration(TimeSpan duration, bool compact)
        {
            TimeSpan absolute = duration < TimeSpan.Zero ? duration.Negate() : duration;
            int days = (int)Math.Floor(absolute.TotalDays);
            int hours = absolute.Hours;
            int minutes = absolute.Minutes;
            string dayUnit = compact ? LiveOpsHubStrings.CompactDayUnit : LiveOpsHubStrings.DurationDayUnit;
            string hourUnit = compact ? LiveOpsHubStrings.CompactHourUnit : LiveOpsHubStrings.DurationHourUnit;
            string minuteUnit = compact ? LiveOpsHubStrings.CompactMinuteUnit : LiveOpsHubStrings.DurationMinuteUnit;

            StringBuilder builder = new StringBuilder();
            if (days > 0)
            {
                AppendPart(builder, days, dayUnit, compact);
                if (hours > 0) AppendPart(builder, hours, hourUnit, compact);
                return builder.ToString();
            }
            if (hours > 0) AppendPart(builder, hours, hourUnit, compact);
            if (minutes > 0 || hours == 0) AppendPart(builder, minutes, minuteUnit, compact);
            return builder.ToString();
        }

        /// <summary>"sau 11 giờ 13 phút" khi mốc ở tương lai · "13 giờ trước" khi mốc đã qua (hoặc đúng lúc này).</summary>
        public string Relative(DateTime nowUtc, DateTime targetUtc)
        {
            TimeSpan difference = targetUtc - nowUtc;
            if (difference > TimeSpan.Zero)
            {
                return LiveOpsHubStrings.RelativeFuturePrefix + " " + Duration(difference, false);
            }
            return Duration(difference, false) + " " + LiveOpsHubStrings.RelativePastSuffix;
        }

        /// <summary>
        /// "còn 19 ngày 15 giờ" — thời gian còn lại tới lúc đợt đang chạy khép. Tách khỏi <see cref="Relative"/> vì cùng một
        /// khoảng nhưng nghĩa khác ("sau" = chưa bắt đầu, "còn" = đang chạy); nơi gọi biết đợt đang ở giai đoạn nào.
        /// </summary>
        public string Remaining(DateTime nowUtc, DateTime endUtc)
        {
            TimeSpan difference = endUtc - nowUtc;
            return LiveOpsHubStrings.RemainingPrefix + " " + Duration(difference < TimeSpan.Zero ? TimeSpan.Zero : difference, false);
        }

        /// <summary>"1.601 byte".</summary>
        public string Bytes(int byteCount)
        {
            return Integer(byteCount) + " " + LiveOpsHubStrings.ByteUnit;
        }

        /// <summary>"48,4%" từ tỉ lệ 0,484 — một chữ số thập phân, dấu phẩy thập phân.</summary>
        public string Percent(double ratio)
        {
            string invariant = (ratio * 100d).ToString("0.0", CultureInfo.InvariantCulture);
            return invariant.Replace('.', ',') + "%";
        }

        /// <summary>"1.601" — dấu chấm ngăn hàng nghìn (không "1 601", không "1,601").</summary>
        public string Integer(int value)
        {
            string invariant = value.ToString("#,0", CultureInfo.InvariantCulture);
            return invariant.Replace(',', '.');
        }

        /// <summary>"thứ Hai" … "Chủ nhật" theo ngày UTC.</summary>
        public string DayOfWeek(DateTime utc)
        {
            switch (utc.DayOfWeek)
            {
                case System.DayOfWeek.Monday: return LiveOpsHubStrings.Monday;
                case System.DayOfWeek.Tuesday: return LiveOpsHubStrings.Tuesday;
                case System.DayOfWeek.Wednesday: return LiveOpsHubStrings.Wednesday;
                case System.DayOfWeek.Thursday: return LiveOpsHubStrings.Thursday;
                case System.DayOfWeek.Friday: return LiveOpsHubStrings.Friday;
                case System.DayOfWeek.Saturday: return LiveOpsHubStrings.Saturday;
                default: return LiveOpsHubStrings.Sunday;
            }
        }

        /// <summary>
        /// "Tuần 38" theo tuần ISO 8601 (tuần bắt đầu thứ Hai, tuần 1 chứa thứ Năm đầu năm). Tự tính thay vì
        /// <c>ISOWeek</c> để không phụ thuộc mức API .NET của từng bản Unity; 14/9/2026 = Tuần 38.
        /// </summary>
        public string IsoWeekLabel(DateTime utc)
        {
            return LiveOpsHubStrings.IsoWeekPrefix + " " + IsoWeekNumber(utc).ToString(CultureInfo.InvariantCulture);
        }

        internal static int IsoWeekNumber(DateTime date)
        {
            int isoDayOfWeek = date.DayOfWeek == System.DayOfWeek.Sunday ? DaysPerWeek : (int)date.DayOfWeek;
            int week = (date.DayOfYear - isoDayOfWeek + IsoWeekOffsetDays) / DaysPerWeek;
            if (week < 1)
            {
                return IsoWeekNumber(new DateTime(date.Year - 1, 12, 31));
            }
            if (week == LastIsoWeekCandidate)
            {
                // Tuần 53 chỉ có khi 31/12 rơi vào thứ Năm trở về trước tính từ đầu tuần; không thì thuộc tuần 1 năm sau.
                DateTime lastDay = new DateTime(date.Year, 12, 31);
                int lastIsoDayOfWeek = lastDay.DayOfWeek == System.DayOfWeek.Sunday ? DaysPerWeek : (int)lastDay.DayOfWeek;
                if (lastIsoDayOfWeek < (int)System.DayOfWeek.Thursday) return 1;
            }
            return week;
        }

        private DateTime ToDevice(DateTime utc)
        {
            return DateTime.SpecifyKind(utc, DateTimeKind.Utc) + _deviceOffset;
        }

        private static string DayMonth(DateTime value)
        {
            return value.Day.ToString(CultureInfo.InvariantCulture) + "/" + value.Month.ToString(CultureInfo.InvariantCulture);
        }

        private static string Clock(DateTime value)
        {
            return value.Hour.ToString("00", CultureInfo.InvariantCulture) + ":" + value.Minute.ToString("00", CultureInfo.InvariantCulture);
        }

        private static void AppendPart(StringBuilder builder, int amount, string unit, bool compact)
        {
            if (builder.Length > 0) builder.Append(' ');
            builder.Append(amount.ToString(CultureInfo.InvariantCulture));
            // Bản gọn dính đơn vị vào số ("15g"), bản đủ có khoảng trắng ("15 giờ").
            if (!compact) builder.Append(' ');
            builder.Append(unit);
        }

        private static string BuildOffsetLabel(TimeSpan offset)
        {
            if (offset == TimeSpan.Zero) return LiveOpsHubStrings.UtcLabel;
            TimeSpan absolute = offset < TimeSpan.Zero ? offset.Negate() : offset;
            string sign = offset < TimeSpan.Zero ? "-" : "+";
            string hours = ((int)Math.Floor(absolute.TotalHours)).ToString(CultureInfo.InvariantCulture);
            string minutes = absolute.Minutes == 0 ? string.Empty : ":" + absolute.Minutes.ToString("00", CultureInfo.InvariantCulture);
            return LiveOpsHubStrings.UtcLabel + sign + hours + minutes;
        }
    }
}
