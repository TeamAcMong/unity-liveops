using System;
using System.Globalization;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Đọc/ghi/chuẩn hoá chuỗi giờ UTC của tài liệu lịch. Một nguồn dùng chung cho game (parser 0.2.0 gọi lại
    /// <see cref="TryParse"/> thay vì tự giữ bản định dạng riêng như 0.1.0) và hub (ô sửa giờ, Sửa an toàn).
    /// </summary>
    public static class LiveEventUtcText
    {
        public const string CanonicalFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

        // Cùng bộ định dạng + AssumeUniversal như JsonLiveEventCalendarParser 0.1.0 — chuỗi không ghi múi hiểu là UTC.
        private static readonly string[] DateFormats =
        {
            "yyyy-MM-dd'T'HH:mm:ssK",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
            "yyyy-MM-dd'T'HH:mmK",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
            "yyyy-MM-dd'T'HH:mm",
        };

        private static readonly string[] DateOnlyFormats = { "yyyy-MM-dd", "yyyy-M-d", "yyyy-MM-d", "yyyy-M-dd" };

        // TimeSpan.TryParseExact dùng bộ định dạng riêng (không phải DateTime): "hh" 2 chữ số, dấu ':' phải escape.
        private static readonly string[] TimeSpanOnlyFormats = { "hh\\:mm", "hh\\:mm\\:ss" };

        public static bool TryParse(string text, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrEmpty(text)) return false;
            if (!DateTimeOffset.TryParseExact(text.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                                              out DateTimeOffset parsed))
            {
                return false;
            }
            utc = DateTime.SpecifyKind(parsed.UtcDateTime, DateTimeKind.Utc);
            return true;
        }

        public static string Format(DateTime utc)
        {
            return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString(CanonicalFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Chuẩn hoá "an toàn" (không đổi thời điểm): thêm số 0 thiếu ("2026-10-3" → "2026-10-03T00:00:00Z"), thiếu giờ
        /// thì hiểu là 00:00:00, có múi giờ khác Z thì đổi về Z (cùng thời điểm). Trả <c>false</c> khi chuỗi mơ hồ
        /// (vd "3/10/2026" có thể là 3 tháng 10 hoặc 10 tháng 3) — chỗ đó phải sửa tay, không phải Sửa an toàn.
        /// </summary>
        public static bool TryNormalize(string text, out string canonicalText)
        {
            canonicalText = null;
            if (TryParse(text, out DateTime direct))
            {
                canonicalText = Format(direct);
                return true;
            }
            if (string.IsNullOrEmpty(text)) return false;

            string trimmed = text.Trim();
            int separatorIndex = trimmed.IndexOf('T');
            if (separatorIndex < 0) separatorIndex = trimmed.IndexOf(' ');

            string datePart = separatorIndex >= 0 ? trimmed.Substring(0, separatorIndex) : trimmed;
            string timePart = separatorIndex >= 0 ? trimmed.Substring(separatorIndex + 1).TrimEnd('Z', 'z') : null;

            if (!DateTime.TryParseExact(datePart, DateOnlyFormats, CultureInfo.InvariantCulture, DateTimeStyles.None,
                                        out DateTime dateOnly))
            {
                return false;
            }

            TimeSpan timeOfDay = TimeSpan.Zero;
            if (!string.IsNullOrEmpty(timePart) &&
                !TimeSpan.TryParseExact(timePart, new[] { "hh\\:mm", "hh\\:mm\\:ss" }, CultureInfo.InvariantCulture, out timeOfDay))
            {
                return false;
            }

            canonicalText = Format(DateTime.SpecifyKind(dateOnly.Date + timeOfDay, DateTimeKind.Utc));
            return true;
        }

        /// <summary>Ô ngày (yyyy-MM-dd) + ô giờ (HH:mm) tách riêng của inspector ghép lại thành một giờ UTC.</summary>
        public static bool TryParseDateAndTime(string dateText, string timeText, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrEmpty(dateText)) return false;
            if (!DateTime.TryParseExact(dateText.Trim(), DateOnlyFormats, CultureInfo.InvariantCulture, DateTimeStyles.None,
                                        out DateTime dateOnly))
            {
                return false;
            }

            TimeSpan timeOfDay = TimeSpan.Zero;
            if (!string.IsNullOrEmpty(timeText) &&
                !TimeSpan.TryParseExact(timeText.Trim(), TimeSpanOnlyFormats, CultureInfo.InvariantCulture, out timeOfDay))
            {
                return false;
            }

            utc = DateTime.SpecifyKind(dateOnly.Date + timeOfDay, DateTimeKind.Utc);
            return true;
        }
    }
}
