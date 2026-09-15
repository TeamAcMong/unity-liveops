using System;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bốn trạng thái sức khoẻ dùng chung cho rail, section, dấu trạng thái. Số giá trị là hợp đồng với
    /// <see cref="SectionHealth.RankOf"/> — thứ tự khai báo KHÔNG phải thứ tự xấu dần (NotMeasured đứng cuối nhưng xếp
    /// trên Ok), nên mọi phép gộp phải đi qua <see cref="SectionHealth.Worse"/> thay vì so số enum.
    /// </summary>
    internal enum HealthState
    {
        Ok = 0,
        Warning = 1,
        Blocked = 2,
        NotMeasured = 3,
    }

    /// <summary>
    /// Sức khoẻ của một section: trạng thái + badge + lý do. Lý do bắt buộc với mọi trạng thái khác Ok, vì
    /// "chưa kiểm" hay "bị chặn" mà không nói vì sao thì người dùng không biết phải làm gì ([FD §2.4]: vòng rỗng luôn
    /// đi kèm một câu lý do). Kiểu giá trị để rail so sánh/giữ bản cũ mà không cấp phát.
    /// </summary>
    internal readonly struct SectionHealth
    {
        private readonly string _badge;
        private readonly string _reason;

        private SectionHealth(HealthState state, string badge, string reason, bool isStale, DateTime? measuredAtUtc,
            LiveOpsHubFindingCounts counts)
        {
            State = state;
            _badge = badge;
            _reason = reason;
            IsStale = isStale;
            MeasuredAtUtc = measuredAtUtc;
            Counts = counts;
        }

        public HealthState State { get; }

        /// <summary>"" = không badge. <c>default(SectionHealth)</c> cũng trả "" chứ không null để view gán thẳng vào Label.</summary>
        public string Badge => _badge ?? string.Empty;

        public string Reason => _reason ?? string.Empty;

        /// <summary>true khi kết quả đã cũ (badge "cũ · …"): vẫn giữ badge của lần đo trước để người dùng thấy con số cũ.</summary>
        public bool IsStale { get; }

        public DateTime? MeasuredAtUtc { get; }

        /// <summary>
        /// Số phát hiện theo đích của màn (V-21 CC-SHELL-5 (b)). Rail cộng số này cho badge/tooltip tầng thay vì đọc lại chữ
        /// badge: chữ là để người đọc, gộp số từ chữ vỡ ngay khi một màn có hai loại đếm trong một badge ("2 bị bỏ · 2 nên xem").
        /// <c>default</c> = rỗng — màn không đếm phát hiện (Tổng quan, Xuất) không phải khai gì.
        /// </summary>
        public LiveOpsHubFindingCounts Counts { get; }

        public static SectionHealth Ok(string badge = "")
        {
            return new SectionHealth(HealthState.Ok, badge ?? string.Empty, string.Empty, false, null, default(LiveOpsHubFindingCounts));
        }

        public static SectionHealth Warning(string badge, string reason)
        {
            return new SectionHealth(HealthState.Warning, badge ?? string.Empty, RequireReason(reason, HealthState.Warning), false, null, default(LiveOpsHubFindingCounts));
        }

        public static SectionHealth Blocked(string badge, string reason)
        {
            return new SectionHealth(HealthState.Blocked, badge ?? string.Empty, RequireReason(reason, HealthState.Blocked), false, null, default(LiveOpsHubFindingCounts));
        }

        public static SectionHealth NotMeasured(string reason)
        {
            return new SectionHealth(HealthState.NotMeasured, string.Empty, RequireReason(reason, HealthState.NotMeasured), false, null, default(LiveOpsHubFindingCounts));
        }

        /// <summary>
        /// Bản "cũ" của kết quả này: giữ badge cũ nhưng State = NotMeasured — kết quả cũ không được trông như vẫn đúng
        /// (PD-23). Lý do cũ giữ nguyên nếu có; kết quả Ok không có lý do thì dùng câu chung, để NotMeasured luôn có lý do.
        /// </summary>
        public SectionHealth AsStale(DateTime measuredAtUtc)
        {
            string reason = string.IsNullOrEmpty(_reason) ? LiveOpsHubStrings.StaleHealthReason : _reason;
            return new SectionHealth(HealthState.NotMeasured, Badge, reason, true, DateTime.SpecifyKind(measuredAtUtc, DateTimeKind.Utc), Counts);
        }

        /// <summary>
        /// Bản mang số đếm cho trước; giữ nguyên trạng thái, badge, lý do, cờ cũ. Tách khỏi các hàm dựng để mọi chỗ đang gọi
        /// <c>Blocked(badge, reason)</c> không phải đổi, và bản cũ (<see cref="AsStale"/>) vẫn giữ số của lần đo trước như giữ badge.
        /// </summary>
        public SectionHealth WithCounts(LiveOpsHubFindingCounts counts)
        {
            return new SectionHealth(State, _badge, _reason, IsStale, MeasuredAtUtc, counts);
        }

        /// <summary>Trạng thái xấu hơn của hai bên (Blocked 3 > Warning 2 > NotMeasured 1 > Ok 0); bằng nhau thì giữ bên trái.</summary>
        public static SectionHealth Worse(SectionHealth left, SectionHealth right)
        {
            return RankOf(right.State) > RankOf(left.State) ? right : left;
        }

        public static int RankOf(HealthState state)
        {
            switch (state)
            {
                case HealthState.Blocked: return 3;
                case HealthState.Warning: return 2;
                case HealthState.NotMeasured: return 1;
                default: return 0;
            }
        }

        private static string RequireReason(string reason, HealthState state)
        {
            if (string.IsNullOrEmpty(reason))
            {
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, LiveOpsHubStrings.HealthReasonRequiredFormat, state), nameof(reason));
            }
            return reason;
        }
    }

    /// <summary>
    /// Số phát hiện của một màn theo bốn loại mà rail đếm (V-21 CC-SHELL-5 (b)): Bị bỏ, Mất tiến độ, Nên xem (chưa bỏ qua) và luật
    /// chưa kiểm được. G-SESSION điền theo đích (IMPLEMENTATION_PLAN 6.4); <c>LiveOpsHubRailModel</c> cộng các màn trong một tầng.
    /// Kiểu giá trị để <see cref="SectionHealth"/> vẫn so sánh/giữ bản cũ mà không cấp phát.
    /// </summary>
    internal readonly struct LiveOpsHubFindingCounts
    {
        public LiveOpsHubFindingCounts(int dropped, int progressLost, int shouldReview, int notMeasured)
        {
            Dropped = RequireNotNegative(dropped, nameof(dropped));
            ProgressLost = RequireNotNegative(progressLost, nameof(progressLost));
            ShouldReview = RequireNotNegative(shouldReview, nameof(shouldReview));
            NotMeasured = RequireNotNegative(notMeasured, nameof(notMeasured));
        }

        public int Dropped { get; }
        public int ProgressLost { get; }
        public int ShouldReview { get; }

        /// <summary>Số luật NotMeasured/Failed của đích — "1 chưa kiểm".</summary>
        public int NotMeasured { get; }

        public int Total => Dropped + ProgressLost + ShouldReview + NotMeasured;

        public bool IsEmpty => Total == 0;

        /// <summary>Cộng từng loại — tầng rail gộp nhiều màn bằng hàm này, không bằng chuỗi badge.</summary>
        public LiveOpsHubFindingCounts Add(LiveOpsHubFindingCounts other)
        {
            return new LiveOpsHubFindingCounts(Dropped + other.Dropped, ProgressLost + other.ProgressLost,
                ShouldReview + other.ShouldReview, NotMeasured + other.NotMeasured);
        }

        /// <summary>Số âm là lỗi lập trình của nơi đếm: ném ngay, vì badge "-1 bị bỏ" hay tổng trừ lẫn nhau sẽ giấu một phát hiện.</summary>
        private static int RequireNotNegative(int count, string parameterName)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, count, LiveOpsHubStrings.FindingCountNegative);
            }
            return count;
        }
    }
}
