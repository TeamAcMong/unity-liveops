using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Nơi lắp các khối Lego của live-ops. Mỗi <c>With...</c> là một ổ cắm: đổi module = đổi đúng một dòng ở composition root,
    /// không sửa <see cref="LiveOpsSystem"/>, UI hay luật khác.
    ///
    /// <para>Bắt buộc: lịch và ít nhất một loại event. Còn lại có mặc định: giờ máy, lưu trong RAM, chưa phát quà (quà nằm chờ),
    /// <see cref="LiveOpsSettings.Default"/>.</para>
    /// </summary>
    public sealed class LiveOpsSystemBuilder
    {
        private readonly string _systemId;
        private readonly List<LiveEventTypeRules> _eventTypes = new List<LiveEventTypeRules>();

        private ILiveOpsClock _clock;
        private ILiveEventCalendar _calendar;
        private ILiveOpsTextStore _textStore;
        private ILiveOpsRewardGranter _rewardGranter;
        private LiveOpsSettings _settings;

        /// <param name="systemId">Tách dữ liệu khi một game có nhiều hệ thống live-ops. Đổi id = người chơi bắt đầu lại từ đầu.</param>
        public LiveOpsSystemBuilder(string systemId)
        {
            LiveEventInstance.ValidateIdentifier(systemId, nameof(systemId), "System id");
            _systemId = systemId;
        }

        public LiveOpsSystemBuilder WithClock(ILiveOpsClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            return this;
        }

        public LiveOpsSystemBuilder WithCalendar(ILiveEventCalendar calendar)
        {
            _calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
            return this;
        }

        public LiveOpsSystemBuilder WithTextStore(ILiveOpsTextStore textStore)
        {
            _textStore = textStore ?? throw new ArgumentNullException(nameof(textStore));
            return this;
        }

        public LiveOpsSystemBuilder WithRewardGranter(ILiveOpsRewardGranter rewardGranter)
        {
            _rewardGranter = rewardGranter ?? throw new ArgumentNullException(nameof(rewardGranter));
            return this;
        }

        public LiveOpsSystemBuilder WithSettings(LiveOpsSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            return this;
        }

        public LiveOpsSystemBuilder WithEventType(LiveEventTypeRules rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            foreach (LiveEventTypeRules existing in _eventTypes)
            {
                if (string.Equals(existing.EventType, rules.EventType, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Loại event đã đăng ký: " + rules.EventType, nameof(rules));
                }
            }
            _eventTypes.Add(rules);
            return this;
        }

        public LiveOpsSystemBuilder WithEventType(string eventType, ILiveEventCompletionRule completionRule = null,
                                                  ILiveEventEligibility eligibility = null,
                                                  LiveEventJoinPolicy joinPolicy = LiveEventJoinPolicy.JoinOnFirstProgress)
        {
            return WithEventType(new LiveEventTypeRules(eventType, completionRule, eligibility, joinPolicy));
        }

        public LiveOpsSystem Build()
        {
            if (_calendar == null) throw new InvalidOperationException("Thiếu lịch event: gọi WithCalendar.");
            if (_eventTypes.Count == 0) throw new InvalidOperationException("Chưa có loại event nào: gọi WithEventType.");

            return new LiveOpsSystem(_systemId,
                                     _clock ?? new SystemLiveOpsClock(),
                                     _calendar,
                                     _textStore ?? new InMemoryLiveOpsTextStore(),
                                     _rewardGranter ?? new DeferredLiveOpsRewardGranter(),
                                     _settings ?? LiveOpsSettings.Default,
                                     new List<LiveEventTypeRules>(_eventTypes));
        }
    }
}
