using System;

namespace DreamTech.LiveOps
{
    /// <summary>Người chơi vào một đợt bằng cách nào.</summary>
    public enum LiveEventJoinPolicy
    {
        /// <summary>Tự vào ở lần cộng điểm đầu tiên khi đợt đang chạy (collect event, pass tính theo level thắng).</summary>
        JoinOnFirstProgress = 0,

        /// <summary>Phải bấm tham gia (<see cref="LiveOpsSystem.TryJoin"/>) rồi mới cộng điểm được (đua, thử thách có nút Start).</summary>
        ExplicitJoin = 1,
    }

    /// <summary>Luật của một loại event. Mỗi loại đăng ký một lần với <see cref="LiveOpsSystemBuilder.WithEventType(LiveEventTypeRules)"/>.</summary>
    public sealed class LiveEventTypeRules
    {
        public LiveEventTypeRules(string eventType, ILiveEventCompletionRule completionRule = null,
                                  ILiveEventEligibility eligibility = null,
                                  LiveEventJoinPolicy joinPolicy = LiveEventJoinPolicy.JoinOnFirstProgress)
        {
            LiveEventInstance.ValidateIdentifier(eventType, nameof(eventType), "Loại event");
            if (!Enum.IsDefined(typeof(LiveEventJoinPolicy), joinPolicy)) throw new ArgumentOutOfRangeException(nameof(joinPolicy));
            EventType = eventType;
            CompletionRule = completionRule ?? NoLiveEventCompletionReward.Instance;
            Eligibility = eligibility ?? AlwaysLiveEventEligibility.Instance;
            JoinPolicy = joinPolicy;
        }

        public string EventType { get; }
        public ILiveEventCompletionRule CompletionRule { get; }
        public ILiveEventEligibility Eligibility { get; }
        public LiveEventJoinPolicy JoinPolicy { get; }
    }
}
