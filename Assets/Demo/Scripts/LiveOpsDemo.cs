using System;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Unity;
using UnityEngine;

namespace DreamTech.LiveOps.Demo
{
    /// <summary>
    /// Bàn thử live-ops (chỉ nằm trong dev project, không đi theo package).
    ///
    /// <para>Lắp đúng những khối một game sẽ lắp: đồng hồ server (header Date) bọc trong đồng hồ tua được, lịch lặp lại ghép với
    /// lịch JSON như lấy từ remote config, PlayerPrefs, granter giả lập kho đồ, hai loại event với luật khác nhau. Phần bày nút giao
    /// cho <see cref="LiveOpsDebugPanel"/>, phần nhịp tim giao cho <see cref="LiveOpsUnityRunner"/> — game thật dùng lại đúng hai
    /// component đó. Mục đích là thử LUẬT và LUỒNG khi chưa có art.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LiveOpsDemo : MonoBehaviour
    {
        public const string RaceEventType = "sky-race";
        public const string HuntEventType = "treasure-hunt";
        public const string HuntGoalClaimKey = "goal";
        private const string SystemId = "demo-liveops";
        private const string StoreKeyPrefix = "dreamtech.liveops.demo.";
        private const string IsoUtcFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

        private static readonly DateTime RaceAnchorUtc = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        private static readonly LiveOpsRewardBundle RaceGoldChest =
            new LiveOpsRewardBundle("chest.gold", new[] { new LiveOpsRewardItem("coin", 100), new LiveOpsRewardItem("booster.magnet", 1) });

        private static readonly LiveOpsRewardBundle RaceRegularChest =
            new LiveOpsRewardBundle("chest.regular", new[] { new LiveOpsRewardItem("coin", 10) });

        private static readonly LiveOpsRewardBundle HuntGoalChest =
            new LiveOpsRewardBundle("chest.hunt", new[] { new LiveOpsRewardItem("coin", 30) });

        [Header("Đồng hồ")]
        [Tooltip("Lấy giờ từ header Date của URL dưới. Tắt thì dùng giờ máy (không chống vặn giờ).")]
        [SerializeField] private bool useServerTime = true;
        [SerializeField] private string serverTimeUrl = "https://www.google.com/generate_204";

        [Header("Sky Race — lịch lặp lại, tự vào khi có điểm")]
        [Tooltip("Mỗi bao nhiêu giờ có một đợt.")]
        [SerializeField, Min(0.1f)] private float racePeriodHours = 24f;
        [Tooltip("Mỗi đợt chạy bao nhiêu giờ; phần còn lại của chu kỳ là nghỉ.")]
        [SerializeField, Min(0.1f)] private float raceActiveHours = 20f;
        [SerializeField, Min(1)] private int raceGoalPoints = 100;

        [Header("Treasure Hunt — lịch JSON, phải bấm tham gia")]
        [Tooltip("Đợt săn trong JSON mẫu bắt đầu 00:00 UTC hôm nay và kéo dài bấy nhiêu giờ.")]
        [SerializeField, Min(24f)] private float huntDurationHours = 72f;
        [SerializeField, Min(1)] private int huntUnlockLevel = 5;
        [Tooltip("Level giả lập của người chơi — thấp hơn mức mở khoá thì không vào được đợt săn.")]
        [SerializeField, Min(0)] private int playerLevel = 5;
        [SerializeField, Min(1)] private int huntGoalPoints = 50;

        private readonly Dictionary<string, int> _wallet = new Dictionary<string, int>(StringComparer.Ordinal);
        private LiveOpsSystem _system;
        private OffsetLiveOpsClock _clock;
        private SyncedLiveOpsClock _syncedClock;
        private IReadOnlyList<string> _calendarProblems;
        private LiveOpsDebugPanel _panel;
        private LiveOpsUnityRunner _runner;

        public LiveOpsSystem System => _system;
        public OffsetLiveOpsClock Clock => _clock;
        public LiveOpsDebugPanel Panel => _panel;
        public LiveOpsUnityRunner Runner => _runner;
        public IReadOnlyDictionary<string, int> Wallet => _wallet;

        /// <summary>JSON lịch mẫu mà demo đưa vào parser — đúng dạng một key remote config.</summary>
        public string CalendarJson { get; private set; }

        private void Awake()
        {
            Build();

            _panel = gameObject.AddComponent<LiveOpsDebugPanel>();
            _panel.ExtraStatusLine = FormatWallet;
            _panel.DrawExtraControls = DrawDemoControls;
            _panel.CalendarProblems = _calendarProblems;
            _panel.Bind(_system, _clock, _syncedClock);

            _runner = gameObject.AddComponent<LiveOpsUnityRunner>();
            _runner.Bind(_system, _syncedClock);
        }

        private void OnDestroy()
        {
            if (_system != null) LiveOpsSystemRegistry.Unregister(_system);
        }

        // ---------------------------------------------------------------- Lắp ráp

        private void Build()
        {
            var store = new PlayerPrefsLiveOpsTextStore(StoreKeyPrefix);
            ILiveOpsClock baseClock = new SystemLiveOpsClock();
            if (useServerTime)
            {
                _syncedClock = new SyncedLiveOpsClock(new HttpDateHeaderServerTimeSource(serverTimeUrl), store);
                baseClock = _syncedClock;
            }
            _clock = new OffsetLiveOpsClock(baseClock);

            // Mốc cố định để id đợt đua không đổi giữa các lần chạy.
            var race = new RecurringLiveEventCalendar(RaceEventType, RaceAnchorUtc, TimeSpan.FromHours(racePeriodHours),
                                                      TimeSpan.FromHours(Mathf.Min(raceActiveHours, racePeriodHours)));

            CalendarJson = BuildHuntCalendarJson(DateTime.UtcNow);
            LiveEventCalendarParseResult hunt = JsonLiveEventCalendarParser.Parse(CalendarJson);
            _calendarProblems = hunt.Problems;

            _system = new LiveOpsSystemBuilder(SystemId)
                      .WithClock(_clock)
                      .WithCalendar(new CompositeLiveEventCalendar(race, hunt.Calendar))
                      .WithTextStore(store)
                      .WithRewardGranter(new DelegateLiveOpsRewardGranter(GrantToWallet))
                      .WithEventType(RaceEventType, new DelegateLiveEventCompletionRule(RaceReward))
                      .WithEventType(HuntEventType, new DelegateLiveEventCompletionRule(HuntReward),
                                     new DelegateLiveEventEligibility((instance, nowUtc) => playerLevel >= huntUnlockLevel),
                                     LiveEventJoinPolicy.ExplicitJoin)
                      .Build();
            LiveOpsSystemRegistry.Register(_system);
        }

        /// <summary>
        /// Lịch săn kho báu như remote config sẽ trả: một đợt bắt đầu 00:00 UTC hôm nay (id theo ngày nên ổn định trong ngày) và một mục
        /// cố ý hỏng để thấy lịch bỏ mục sai và panel liệt kê lý do.
        /// </summary>
        private string BuildHuntCalendarJson(DateTime utcNow)
        {
            DateTime startUtc = utcNow.Date;
            DateTime endUtc = startUtc.AddHours(huntDurationHours);
            return "{\"events\":[" +
                   "{\"id\":\"hunt-" + startUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "\"," +
                   "\"type\":\"" + HuntEventType + "\"," +
                   "\"startUtc\":\"" + startUtc.ToString(IsoUtcFormat, CultureInfo.InvariantCulture) + "\"," +
                   "\"endUtc\":\"" + endUtc.ToString(IsoUtcFormat, CultureInfo.InvariantCulture) + "\"," +
                   "\"configKey\":\"hunt_demo_v1\"}," +
                   "{\"id\":\"hunt-broken\",\"type\":\"" + HuntEventType + "\",\"startUtc\":\"ngày mai\",\"endUtc\":\"2026-01-01T00:00:00Z\"}" +
                   "]}";
        }

        private LiveOpsRewardBundle RaceReward(LiveEventRecord record)
        {
            if (record.Points >= raceGoalPoints) return RaceGoldChest;
            return record.Points > 0 ? RaceRegularChest : LiveOpsRewardBundle.None;
        }

        /// <summary>Đạt mốc mà quên bấm nhận thì cuối đợt tự nhận — mẫu "auto-collect" của battle pass.</summary>
        private LiveOpsRewardBundle HuntReward(LiveEventRecord record)
        {
            return record.Points >= huntGoalPoints && !record.HasClaimed(HuntGoalClaimKey) ? HuntGoalChest : LiveOpsRewardBundle.None;
        }

        private bool GrantToWallet(string grantId, LiveOpsRewardBundle bundle)
        {
            foreach (LiveOpsRewardItem item in bundle.Items)
            {
                _wallet.TryGetValue(item.ItemId, out int amount);
                _wallet[item.ItemId] = amount + item.Amount;
            }
            if (_panel != null) _panel.Log("Nhận quà " + grantId + ": " + bundle);
            return true;
        }

        private string FormatWallet()
        {
            if (_wallet.Count == 0) return "Ví: trống";
            var parts = new List<string>(_wallet.Count);
            foreach (KeyValuePair<string, int> entry in _wallet) parts.Add(entry.Key + " x" + entry.Value);
            return "Ví: " + string.Join(", ", parts);
        }

        private void DrawDemoControls(float buttonHeight)
        {
            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Nhận quà mốc " + huntGoalPoints + " điểm (săn)", GUILayout.Height(buttonHeight))) ClaimHuntGoal();
            if (GUILayout.Button("Level người chơi: " + playerLevel, GUILayout.Height(buttonHeight)))
            {
                SetPlayerLevel(playerLevel >= huntUnlockLevel ? 0 : huntUnlockLevel);
            }
            GUILayout.EndHorizontal();
        }

        // ---------------------------------------------------------------- Thao tác riêng của demo (nút + test PlayMode)

        /// <summary>Nhận quà mốc của đợt săn đang chạy — module event tự kiểm đủ điều kiện rồi mới gọi ClaimReward.</summary>
        public LiveOpsClaimOutcome ClaimHuntGoal()
        {
            LiveEventStatus status = _system.GetStatus(HuntEventType);
            if (!status.IsJoined || status.Record.Points < huntGoalPoints)
            {
                _panel.Log("Chưa đủ " + huntGoalPoints + " điểm săn kho báu để nhận quà mốc");
                return null;
            }
            LiveOpsClaimOutcome outcome = _system.ClaimReward(status.Record.EventId, HuntGoalClaimKey, HuntGoalChest);
            _panel.Log("Nhận quà mốc săn kho báu: " + outcome);
            return outcome;
        }

        public void SetPlayerLevel(int level)
        {
            playerLevel = Mathf.Max(0, level);
            _panel.Log("Level người chơi = " + playerLevel);
        }

        public void ResetEverything()
        {
            _wallet.Clear();
            _panel.ResetEverything();
        }
    }
}
