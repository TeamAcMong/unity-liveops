using System;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Tình trạng một loại event tại một thời điểm: đợt đang chạy hoặc sắp tới (trong tầm nhìn
    /// <see cref="LiveOpsSettings.UpcomingLookahead"/>), người chơi có được vào không, đã vào chưa. Icon Home, đồng hồ đếm ngược và
    /// nút tham gia đọc từ đây.
    /// </summary>
    public sealed class LiveEventStatus
    {
        internal LiveEventStatus(string eventType, LiveEventInstance instance, DateTime nowUtc, bool isEligible,
                                 LiveEventRecord record, bool isFinished, bool isClockTrusted)
        {
            EventType = eventType;
            Instance = instance;
            NowUtc = nowUtc;
            IsEligible = isEligible;
            Record = record;
            IsFinished = isFinished;
            IsClockTrusted = isClockTrusted;
        }

        public string EventType { get; }

        /// <summary>Đợt đang chạy, hoặc đợt sớm nhất sắp tới; null khi trong tầm nhìn không có đợt nào.</summary>
        public LiveEventInstance Instance { get; }

        public DateTime NowUtc { get; }
        public bool HasInstance => Instance != null;
        public LiveEventPhase Phase => Instance == null ? LiveEventPhase.None : Instance.PhaseAt(NowUtc);
        public bool IsActive => Phase == LiveEventPhase.Active;
        public bool IsUpcoming => Phase == LiveEventPhase.Upcoming;
        public TimeSpan TimeUntilStart => Instance == null ? TimeSpan.Zero : Instance.TimeUntilStart(NowUtc);
        public TimeSpan TimeLeft => Instance == null ? TimeSpan.Zero : Instance.TimeLeft(NowUtc);
        public bool IsEligible { get; }

        /// <summary>Phần của người chơi trong <see cref="Instance"/>; null khi chưa tham gia.</summary>
        public LiveEventRecord Record { get; }

        public bool IsJoined => Record != null;

        /// <summary>
        /// Đợt này đã khép với người chơi — thường là hết giờ nhưng chưa có đợt mới, hoặc giờ bị vặn lùi về một đợt đã chơi. Không
        /// cộng điểm, không tham gia lại.
        /// </summary>
        public bool IsFinished { get; }

        public bool IsClockTrusted { get; }

        public override string ToString()
        {
            if (Instance == null) return EventType + ": không có đợt nào";
            return EventType + ": " + Instance.EventId + " " + Phase + (IsJoined ? ", đã vào" : string.Empty) +
                   (IsFinished ? ", đã khép" : string.Empty);
        }
    }

    public enum LiveEventJoinStatus
    {
        Joined = 0,
        AlreadyJoined = 1,
        AlreadyFinished = 2,
        NoActiveEvent = 3,
        NotEligible = 4,
    }

    public sealed class LiveEventJoinOutcome
    {
        internal LiveEventJoinOutcome(LiveEventJoinStatus status, LiveEventRecord record)
        {
            Status = status;
            Record = record;
        }

        public LiveEventJoinStatus Status { get; }

        /// <summary>Bản ghi sau khi gọi; null khi không vào được.</summary>
        public LiveEventRecord Record { get; }

        /// <summary>Người chơi đang ở trong đợt (vừa vào hoặc đã vào từ trước).</summary>
        public bool IsInEvent => Status == LiveEventJoinStatus.Joined || Status == LiveEventJoinStatus.AlreadyJoined;

        public override string ToString()
        {
            return Status + (Record != null ? " " + Record.EventId : string.Empty);
        }
    }

    public enum LiveEventProgressStatus
    {
        Added = 0,

        /// <summary>Grant id này đã được cộng trước đó — không cộng lại.</summary>
        DuplicateGrant = 1,

        /// <summary>Số điểm không dương.</summary>
        InvalidAmount = 2,

        NoActiveEvent = 3,
        AlreadyFinished = 4,
        NotEligible = 5,

        /// <summary>Loại event yêu cầu <see cref="LiveEventJoinPolicy.ExplicitJoin"/> mà người chơi chưa bấm tham gia.</summary>
        NotJoined = 6,
    }

    public sealed class LiveEventProgressOutcome
    {
        internal LiveEventProgressOutcome(LiveEventProgressStatus status, LiveEventRecord record, long pointsAdded)
        {
            Status = status;
            Record = record;
            PointsAdded = pointsAdded;
        }

        public LiveEventProgressStatus Status { get; }

        /// <summary>Bản ghi sau khi gọi; null khi không có bản ghi nào liên quan.</summary>
        public LiveEventRecord Record { get; }

        /// <summary>Số điểm thực sự được cộng (có thể nhỏ hơn yêu cầu nếu chạm trần <see cref="long.MaxValue"/>).</summary>
        public long PointsAdded { get; }

        public bool IsAdded => Status == LiveEventProgressStatus.Added;

        public override string ToString()
        {
            return Status + (IsAdded ? " +" + PointsAdded : string.Empty) + (Record != null ? " " + Record.EventId : string.Empty);
        }
    }

    public enum LiveOpsClaimStatus
    {
        /// <summary>Quà đã vào kho đồ của game.</summary>
        Granted = 0,

        /// <summary>Đã chốt, nằm trong hàng chờ vì kho đồ chưa nhận — không mất, không phát trùng.</summary>
        Deferred = 1,

        AlreadyClaimed = 2,

        /// <summary>Chưa tham gia đợt, hoặc đợt đã khép (quà chưa nhận do luật khép đợt xử lý).</summary>
        NotAvailable = 3,
    }

    public sealed class LiveOpsClaimOutcome
    {
        internal LiveOpsClaimOutcome(LiveOpsClaimStatus status, LiveOpsRewardBundle bundle)
        {
            Status = status;
            Bundle = bundle ?? LiveOpsRewardBundle.None;
        }

        public LiveOpsClaimStatus Status { get; }
        public LiveOpsRewardBundle Bundle { get; }

        /// <summary>Quà đã được chốt cho người chơi (đã phát hoặc đang chờ phát).</summary>
        public bool IsSecured => Status == LiveOpsClaimStatus.Granted || Status == LiveOpsClaimStatus.Deferred;

        public override string ToString()
        {
            return Status + " " + Bundle;
        }
    }
}
