using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DreamTech.LiveOps.Tests
{
    /// <summary>Lịch và quà dùng chung: một event đua lặp lại hằng tuần, một đợt săn kho báu cố định.</summary>
    internal static class LiveOpsTestFactory
    {
        /// <summary>Thứ Hai 2026-01-05 00:00 UTC.</summary>
        public static readonly DateTime Anchor = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        public const string RaceType = "sky-race";
        public const string HuntType = "treasure-hunt";
        public const int RaceGoal = 100;

        /// <summary>Đua chạy 3 ngày đầu mỗi tuần.</summary>
        public static readonly TimeSpan RacePeriod = TimeSpan.FromDays(7);

        public static readonly TimeSpan RaceActiveDuration = TimeSpan.FromDays(3);

        /// <summary>Đợt săn duy nhất: thứ Ba 00:00 → thứ Tư 00:00 của tuần đầu (nằm trong đợt đua 0).</summary>
        public static readonly LiveEventInstance FirstHunt =
            new LiveEventInstance("hunt-001", HuntType, Anchor.AddDays(1), Anchor.AddDays(2), "hunt_config_v1");

        public static RecurringLiveEventCalendar CreateRaceCalendar()
        {
            return new RecurringLiveEventCalendar(RaceType, Anchor, RacePeriod, RaceActiveDuration);
        }

        public static ILiveEventCalendar CreateDefaultCalendar()
        {
            return new CompositeLiveEventCalendar(CreateRaceCalendar(), new FixedLiveEventCalendar(new[] { FirstHunt }));
        }

        public static LiveOpsRewardBundle Coins(int amount, string presentationId = "chest.test")
        {
            return new LiveOpsRewardBundle(presentationId, new[] { new LiveOpsRewardItem("coin", amount) });
        }
    }

    /// <summary>Lịch thay được giữa chừng — giả lập remote config đổi lịch khi game đang chạy.</summary>
    internal sealed class SwappableCalendar : ILiveEventCalendar
    {
        public SwappableCalendar(ILiveEventCalendar inner)
        {
            Inner = inner;
        }

        public ILiveEventCalendar Inner { get; set; }
        public IReadOnlyCollection<string> EventTypes => Inner.EventTypes;

        public IReadOnlyList<LiveEventInstance> GetInstances(string eventType, DateTime fromUtc, DateTime toUtc)
        {
            return Inner.GetInstances(eventType, fromUtc, toUtc);
        }
    }

    /// <summary>
    /// Một lần dựng đầy đủ: đồng hồ tay, lịch thay được, lưu trong RAM, granter ghi lại. Đua tự vào khi có điểm, săn kho báu phải
    /// bấm tham gia và có điều kiện. Mặc định đồng hồ ở giữa ngày đầu của đợt đua 0.
    /// </summary>
    internal sealed class LiveOpsScenario
    {
        public const string SystemId = "test-liveops";

        public ManualLiveOpsClock Clock;
        public SwappableCalendar Calendar;
        public InMemoryLiveOpsTextStore Store;
        public RecordingLiveOpsRewardGranter Granter;
        public LiveOpsSettings Settings;
        public bool IsHuntEligible = true;
        public Func<LiveEventRecord, LiveOpsRewardBundle> RaceCompletion = DefaultRaceCompletion;
        public LiveOpsSystem System;
        public readonly List<LiveEventRecord> Finalized = new List<LiveEventRecord>();

        public static LiveOpsScenario Create(LiveOpsSettings settings = null, InMemoryLiveOpsTextStore store = null)
        {
            var scenario = new LiveOpsScenario
            {
                Clock = new ManualLiveOpsClock(LiveOpsTestFactory.Anchor.AddHours(12)),
                Calendar = new SwappableCalendar(LiveOpsTestFactory.CreateDefaultCalendar()),
                Store = store ?? new InMemoryLiveOpsTextStore(),
                Granter = new RecordingLiveOpsRewardGranter(),
                Settings = settings ?? LiveOpsSettings.Default,
            };
            scenario.Rebuild();
            return scenario;
        }

        /// <summary>Đủ mục tiêu → rương vàng; có điểm → xu an ủi; không điểm → không quà.</summary>
        public static LiveOpsRewardBundle DefaultRaceCompletion(LiveEventRecord record)
        {
            if (record.Points >= LiveOpsTestFactory.RaceGoal) return LiveOpsTestFactory.Coins(50, "chest.gold");
            return record.Points > 0 ? LiveOpsTestFactory.Coins(5, "chest.regular") : LiveOpsRewardBundle.None;
        }

        /// <summary>Dựng lại system trên cùng nơi lưu (giả lập mở lại app).</summary>
        public LiveOpsSystem Rebuild()
        {
            System = new LiveOpsSystemBuilder(SystemId)
                     .WithClock(Clock)
                     .WithCalendar(Calendar)
                     .WithTextStore(Store)
                     .WithRewardGranter(Granter)
                     .WithSettings(Settings)
                     .WithEventType(LiveOpsTestFactory.RaceType, new DelegateLiveEventCompletionRule(record => RaceCompletion(record)))
                     .WithEventType(LiveOpsTestFactory.HuntType,
                                    eligibility: new DelegateLiveEventEligibility((instance, nowUtc) => IsHuntEligible),
                                    joinPolicy: LiveEventJoinPolicy.ExplicitJoin)
                     .Build();
            System.EventFinalized += Finalized.Add;
            return System;
        }

        public void AdvancePastRaceEnd()
        {
            LiveEventStatus status = System.GetStatus(LiveOpsTestFactory.RaceType);
            Clock.Set(status.Instance.EndUtc + TimeSpan.FromMinutes(1));
        }
    }

    internal sealed class RecordingLiveOpsRewardGranter : ILiveOpsRewardGranter
    {
        public readonly List<string> GrantedIds = new List<string>();
        public readonly List<LiveOpsRewardBundle> GrantedBundles = new List<LiveOpsRewardBundle>();
        public bool IsReady = true;
        public bool ThrowsOnGrant;

        public bool TryGrant(string grantId, LiveOpsRewardBundle bundle)
        {
            if (ThrowsOnGrant) throw new InvalidOperationException("Kho đồ hỏng (giả lập)");
            if (!IsReady) return false;
            GrantedIds.Add(grantId);
            GrantedBundles.Add(bundle);
            return true;
        }
    }

    internal sealed class ManualElapsedTimeSource : IElapsedTimeSource
    {
        public TimeSpan Elapsed { get; private set; }

        public void Advance(TimeSpan duration)
        {
            Elapsed += duration;
        }
    }

    /// <summary>Nguồn giờ server giả: trả giờ đặt sẵn, lỗi một lần theo yêu cầu, hoặc treo tới khi test cho phản hồi.</summary>
    internal sealed class ScriptedServerTimeSource : IServerTimeSource
    {
        private TaskCompletionSource<DateTime> _heldResponse;

        public DateTime ServerUtc;
        public bool FailsNextCall;
        public bool HoldsResponses;
        public int CallCount { get; private set; }

        public Task<DateTime> FetchUtcNowAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            if (FailsNextCall)
            {
                FailsNextCall = false;
                return Task.FromException<DateTime>(new InvalidOperationException("Mất mạng (giả lập)"));
            }
            if (!HoldsResponses) return Task.FromResult(ServerUtc);
            _heldResponse = new TaskCompletionSource<DateTime>();
            return _heldResponse.Task;
        }

        public void ReleaseHeldResponse()
        {
            _heldResponse.SetResult(ServerUtc);
        }
    }

    /// <summary>
    /// Bỏ SynchronizationContext của Unity trong phạm vi using: continuation của task treo chạy ngay khi test cho phản hồi, thay vì
    /// bị đẩy sang vòng update sau của Editor (lúc đó test đã chạy xong).
    /// </summary>
    internal sealed class NoSynchronizationContextScope : IDisposable
    {
        private readonly SynchronizationContext _previous = SynchronizationContext.Current;

        public NoSynchronizationContextScope()
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }

        public void Dispose()
        {
            SynchronizationContext.SetSynchronizationContext(_previous);
        }
    }
}
