using System;

namespace DreamTech.LiveOps
{
    /// <summary>Tham số chung của <see cref="LiveOpsSystem"/>. Bất biến; mọi tham số có giá trị mặc định dùng được ngay.</summary>
    public sealed class LiveOpsSettings
    {
        public static readonly TimeSpan DefaultUpcomingLookahead = TimeSpan.FromDays(31);
        public static readonly TimeSpan DefaultRecordRetention = TimeSpan.FromDays(30);
        public const int DefaultMaximumRememberedGrantIds = 64;
        public const int DefaultMaximumRetiredEventIds = 256;

        // Khai báo sau các giá trị mặc định: field static khởi tạo theo thứ tự trong file.
        public static readonly LiveOpsSettings Default = new LiveOpsSettings();

        /// <param name="upcomingLookahead">Nhìn trước bao xa để báo "đợt sau bắt đầu sau X" khi không có đợt nào đang chạy.</param>
        /// <param name="recordRetention">
        /// Đợt đã khép quá lâu thế này (tính từ lúc khép) thì bản ghi bị dọn, kể cả khi UI chưa báo kết quả; chỉ còn nhớ id để không
        /// cho vào lại.
        /// </param>
        /// <param name="maximumRememberedGrantIds">Số grant id gần nhất mỗi bản ghi nhớ để chống cộng điểm trùng.</param>
        /// <param name="maximumRetiredEventIds">Số id đợt đã dọn còn được nhớ.</param>
        /// <param name="finalizeRequiresTrustedClock">
        /// true = chỉ khép đợt khi <see cref="ILiveOpsClock.IsTrusted"/>: người chơi vặn giờ tới lúc offline không khép sớm được,
        /// đổi lại người chơi offline phải chờ có mạng mới thấy kết quả. Chỉ bật khi dùng <see cref="SyncedLiveOpsClock"/> — với
        /// <see cref="SystemLiveOpsClock"/> đồng hồ không bao giờ tin được và không đợt nào khép.
        /// </param>
        public LiveOpsSettings(TimeSpan? upcomingLookahead = null, TimeSpan? recordRetention = null,
                               int maximumRememberedGrantIds = DefaultMaximumRememberedGrantIds,
                               int maximumRetiredEventIds = DefaultMaximumRetiredEventIds,
                               bool finalizeRequiresTrustedClock = false)
        {
            UpcomingLookahead = upcomingLookahead ?? DefaultUpcomingLookahead;
            RecordRetention = recordRetention ?? DefaultRecordRetention;
            if (UpcomingLookahead <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(upcomingLookahead), "Phải dài hơn 0.");
            if (RecordRetention < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(recordRetention), "Không được âm.");
            if (maximumRememberedGrantIds < 1) throw new ArgumentOutOfRangeException(nameof(maximumRememberedGrantIds), "Ít nhất 1.");
            if (maximumRetiredEventIds < 1) throw new ArgumentOutOfRangeException(nameof(maximumRetiredEventIds), "Ít nhất 1.");

            MaximumRememberedGrantIds = maximumRememberedGrantIds;
            MaximumRetiredEventIds = maximumRetiredEventIds;
            FinalizeRequiresTrustedClock = finalizeRequiresTrustedClock;
        }

        public TimeSpan UpcomingLookahead { get; }
        public TimeSpan RecordRetention { get; }
        public int MaximumRememberedGrantIds { get; }
        public int MaximumRetiredEventIds { get; }
        public bool FinalizeRequiresTrustedClock { get; }
    }
}
