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

        private SectionHealth(HealthState state, string badge, string reason, bool isStale, DateTime? measuredAtUtc)
        {
            State = state;
            _badge = badge;
            _reason = reason;
            IsStale = isStale;
            MeasuredAtUtc = measuredAtUtc;
        }

        public HealthState State { get; }

        /// <summary>"" = không badge. <c>default(SectionHealth)</c> cũng trả "" chứ không null để view gán thẳng vào Label.</summary>
        public string Badge => _badge ?? string.Empty;

        public string Reason => _reason ?? string.Empty;

        /// <summary>true khi kết quả đã cũ (badge "cũ · …"): vẫn giữ badge của lần đo trước để người dùng thấy con số cũ.</summary>
        public bool IsStale { get; }

        public DateTime? MeasuredAtUtc { get; }

        public static SectionHealth Ok(string badge = "")
        {
            return new SectionHealth(HealthState.Ok, badge ?? string.Empty, string.Empty, false, null);
        }

        public static SectionHealth Warning(string badge, string reason)
        {
            return new SectionHealth(HealthState.Warning, badge ?? string.Empty, RequireReason(reason, HealthState.Warning), false, null);
        }

        public static SectionHealth Blocked(string badge, string reason)
        {
            return new SectionHealth(HealthState.Blocked, badge ?? string.Empty, RequireReason(reason, HealthState.Blocked), false, null);
        }

        public static SectionHealth NotMeasured(string reason)
        {
            return new SectionHealth(HealthState.NotMeasured, string.Empty, RequireReason(reason, HealthState.NotMeasured), false, null);
        }

        /// <summary>
        /// Bản "cũ" của kết quả này: giữ badge cũ nhưng State = NotMeasured — kết quả cũ không được trông như vẫn đúng
        /// (PD-23). Lý do cũ giữ nguyên nếu có; kết quả Ok không có lý do thì dùng câu chung, để NotMeasured luôn có lý do.
        /// </summary>
        public SectionHealth AsStale(DateTime measuredAtUtc)
        {
            string reason = string.IsNullOrEmpty(_reason) ? LiveOpsHubStrings.StaleHealthReason : _reason;
            return new SectionHealth(HealthState.NotMeasured, Badge, reason, true, DateTime.SpecifyKind(measuredAtUtc, DateTimeKind.Utc));
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
}
