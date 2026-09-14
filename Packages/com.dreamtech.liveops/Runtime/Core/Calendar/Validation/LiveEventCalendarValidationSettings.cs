using System;

namespace DreamTech.LiveOps
{
    /// <summary>Ngưỡng cấu hình của các luật Kiểm lịch — số có tên thay vì số trần trong luật.</summary>
    public sealed class LiveEventCalendarValidationSettings
    {
        /// <summary>Khoảng trống giữa hai đợt cùng loại dài hơn mức này thì "Nên xem" (người chơi mất thói quen quay lại).</summary>
        public static readonly TimeSpan DefaultLongGapThreshold = TimeSpan.FromDays(7);

        /// <summary>Chỉ nhắc khoảng trống kết thúc trong 30 ngày qua trở đi — khoảng trống lâu trong quá khứ không còn sửa được.</summary>
        public static readonly TimeSpan DefaultLongGapLookback = TimeSpan.FromDays(30);

        private static readonly LiveEventCalendarValidationSettings DefaultSettings = new LiveEventCalendarValidationSettings();

        public LiveEventCalendarValidationSettings(TimeSpan? longGapThreshold = null, TimeSpan? longGapLookback = null)
        {
            TimeSpan threshold = longGapThreshold ?? DefaultLongGapThreshold;
            TimeSpan lookback = longGapLookback ?? DefaultLongGapLookback;
            // Ngưỡng ≤ 0 biến mọi cặp đợt thành "khoảng trống dài" — lỗi lập trình của composition root, không phải dữ liệu lịch.
            if (threshold <= TimeSpan.Zero) throw new ArgumentException("longGapThreshold phải > 0.", nameof(longGapThreshold));
            if (lookback < TimeSpan.Zero) throw new ArgumentException("longGapLookback không được âm.", nameof(longGapLookback));

            LongGapThreshold = threshold;
            LongGapLookback = lookback;
        }

        public static LiveEventCalendarValidationSettings Default => DefaultSettings;

        public TimeSpan LongGapThreshold { get; }
        public TimeSpan LongGapLookback { get; }
    }
}
