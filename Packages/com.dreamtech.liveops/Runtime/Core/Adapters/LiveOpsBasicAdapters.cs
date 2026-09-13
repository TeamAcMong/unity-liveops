using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>Lưu trong RAM — mặc định khi chưa cắm nơi lưu thật, và cho test.</summary>
    public sealed class InMemoryLiveOpsTextStore : ILiveOpsTextStore
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.Ordinal);

        public bool TryRead(string key, out string value)
        {
            return _values.TryGetValue(key, out value);
        }

        public void Write(string key, string value)
        {
            _values[key] = value ?? string.Empty;
        }

        public void Delete(string key)
        {
            _values.Remove(key);
        }
    }

    /// <summary>Chưa cắm kho đồ: mọi gói quà nằm chờ, không mất. Cắm granter thật rồi gọi <see cref="LiveOpsSystem.GrantPendingRewards"/>.</summary>
    public sealed class DeferredLiveOpsRewardGranter : ILiveOpsRewardGranter
    {
        public bool TryGrant(string grantId, LiveOpsRewardBundle bundle)
        {
            return false;
        }
    }

    /// <summary>Granter bọc một hàm của game — cách lắp nhanh nhất.</summary>
    public sealed class DelegateLiveOpsRewardGranter : ILiveOpsRewardGranter
    {
        private readonly Func<string, LiveOpsRewardBundle, bool> _grant;

        public DelegateLiveOpsRewardGranter(Func<string, LiveOpsRewardBundle, bool> grant)
        {
            _grant = grant ?? throw new ArgumentNullException(nameof(grant));
        }

        public bool TryGrant(string grantId, LiveOpsRewardBundle bundle)
        {
            return _grant(grantId, bundle);
        }
    }

    /// <summary>Ai cũng được vào.</summary>
    public sealed class AlwaysLiveEventEligibility : ILiveEventEligibility
    {
        public static readonly AlwaysLiveEventEligibility Instance = new AlwaysLiveEventEligibility();

        private AlwaysLiveEventEligibility()
        {
        }

        public bool IsEligible(LiveEventInstance instance, DateTime nowUtc)
        {
            return true;
        }
    }

    /// <summary>Điều kiện bọc một hàm của game (vd "level hiện tại >= 11").</summary>
    public sealed class DelegateLiveEventEligibility : ILiveEventEligibility
    {
        private readonly Func<LiveEventInstance, DateTime, bool> _isEligible;

        public DelegateLiveEventEligibility(Func<LiveEventInstance, DateTime, bool> isEligible)
        {
            _isEligible = isEligible ?? throw new ArgumentNullException(nameof(isEligible));
        }

        public bool IsEligible(LiveEventInstance instance, DateTime nowUtc)
        {
            return _isEligible(instance, nowUtc);
        }
    }

    /// <summary>Khép đợt không có quà (quà đã nhận hết giữa đợt, hoặc event chỉ có bảng xếp hạng).</summary>
    public sealed class NoLiveEventCompletionReward : ILiveEventCompletionRule
    {
        public static readonly NoLiveEventCompletionReward Instance = new NoLiveEventCompletionReward();

        private NoLiveEventCompletionReward()
        {
        }

        public LiveOpsRewardBundle RewardFor(LiveEventRecord record)
        {
            return LiveOpsRewardBundle.None;
        }
    }

    /// <summary>Luật khép đợt bọc một hàm của game.</summary>
    public sealed class DelegateLiveEventCompletionRule : ILiveEventCompletionRule
    {
        private readonly Func<LiveEventRecord, LiveOpsRewardBundle> _rewardFor;

        public DelegateLiveEventCompletionRule(Func<LiveEventRecord, LiveOpsRewardBundle> rewardFor)
        {
            _rewardFor = rewardFor ?? throw new ArgumentNullException(nameof(rewardFor));
        }

        public LiveOpsRewardBundle RewardFor(LiveEventRecord record)
        {
            return _rewardFor(record);
        }
    }
}
