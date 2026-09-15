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
    /// <para>Lắp đúng những khối một game sẽ lắp (mục 4.4 kế hoạch 0.2.0): đồng hồ server (header Date) bọc trong đồng hồ tua được,
    /// lịch đọc bằng <see cref="JsonLiveEventCalendarParser.ParseOrDefault(string, LiveEventCalendarAsset)"/> — JSON định dạng 2
    /// như lấy từ remote config, trống thì dùng asset lịch trong project —, loại event đăng ký từ asset bằng
    /// <see cref="LiveOpsSystemBuilderCalendarAssetExtensions.WithEventTypesFrom"/>, PlayerPrefs, granter giả lập kho đồ. Phần bày
    /// nút giao cho <see cref="LiveOpsDebugPanel"/>, phần nhịp tim giao cho <see cref="LiveOpsUnityRunner"/> — game thật dùng lại
    /// đúng hai component đó. Mục đích là thử LUẬT và LUỒNG khi chưa có art.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LiveOpsDemo : MonoBehaviour
    {
        public const string RaceEventType = "sky-race";
        public const string HuntEventType = "treasure-hunt";
        public const string HuntGoalClaimKey = "goal";

        /// <summary>Tên field serialize giữ asset lịch — builder scene gán qua SerializedObject nên cần tên chuẩn một chỗ.</summary>
        public const string CalendarAssetFieldName = nameof(calendarAsset);

        // Hai hằng dưới là khoá PlayerPrefs của dữ liệu thử đã có trên máy dev (0.1.0) — đổi là mất tiến độ đang thử.
        private const string SystemId = "demo-liveops";
        private const string StoreKeyPrefix = "dreamtech.liveops.demo.";
        private const string IsoUtcFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

        // Mốc + tiền tố của luật đua trong JSON remote giả lập giữ đúng giá trị RecurringLiveEventCalendar 0.1.0 tự đặt, để id đợt
        // đua ("sky-race-<số>") không đổi so với bản demo cũ và tiến độ thử trong PlayerPrefs vẫn khớp đợt đang chạy.
        private const string RaceAnchorUtcText = "2026-01-05T00:00:00Z";
        private const string RaceIdPrefix = RaceEventType + "-";

        private static readonly LiveOpsRewardBundle RaceGoldChest =
            new LiveOpsRewardBundle("chest.gold", new[] { new LiveOpsRewardItem("coin", 100), new LiveOpsRewardItem("booster.magnet", 1) });

        private static readonly LiveOpsRewardBundle RaceRegularChest =
            new LiveOpsRewardBundle("chest.regular", new[] { new LiveOpsRewardItem("coin", 10) });

        private static readonly LiveOpsRewardBundle HuntGoalChest =
            new LiveOpsRewardBundle("chest.hunt", new[] { new LiveOpsRewardItem("coin", 30) });

        [Header("Lịch")]
        [Tooltip("Lịch mặc định trong project (sinh bằng Tools/DreamTech/LiveOps/Demo/Build Sample Calendar Asset). Dùng khi JSON " +
                 "remote trống, và là nơi khai loại event cho WithEventTypesFrom.")]
        [SerializeField] private LiveEventCalendarAsset calendarAsset;
        [Tooltip("Bật = giả lập remote config trả JSON định dạng 2 (đua lặp lại + đợt săn hôm nay + một mục hỏng). Tắt = remote trống, " +
                 "game rơi về lịch trong asset.")]
        [SerializeField] private bool simulateRemoteConfig = true;

        [Header("Đồng hồ")]
        [Tooltip("Lấy giờ từ header Date của URL dưới. Tắt thì dùng giờ máy (không chống vặn giờ).")]
        [SerializeField] private bool useServerTime = true;
        [SerializeField] private string serverTimeUrl = "https://www.google.com/generate_204";

        [Header("Sky Race — luật lặp trong JSON remote, tự vào khi có điểm")]
        // Số nguyên vì JSON định dạng 2 ghi periodHours/activeHours là số giờ nguyên.
        [Tooltip("Mỗi bao nhiêu giờ có một đợt.")]
        [SerializeField, Min(1)] private int racePeriodHours = 24;
        [Tooltip("Mỗi đợt chạy bao nhiêu giờ; phần còn lại của chu kỳ là nghỉ.")]
        [SerializeField, Min(1)] private int raceActiveHours = 20;
        [SerializeField, Min(1)] private int raceGoalPoints = 100;

        [Header("Treasure Hunt — đợt cố định trong JSON remote, phải bấm tham gia (requiresJoin của loại trong asset)")]
        [Tooltip("Đợt săn trong JSON mẫu bắt đầu 00:00 UTC hôm nay và kéo dài bấy nhiêu giờ.")]
        [SerializeField, Min(24f)] private float huntDurationHours = 72f;
        [SerializeField, Min(1)] private int huntUnlockLevel = 5;
        [Tooltip("Level giả lập của người chơi — thấp hơn mức mở khoá thì không vào được đợt săn.")]
        [SerializeField, Min(0)] private int playerLevel = 5;
        [SerializeField, Min(1)] private int huntGoalPoints = 50;

        private readonly Dictionary<string, int> _wallet = new Dictionary<string, int>(StringComparer.Ordinal);
        private PlayerPrefsLiveOpsTextStore _store;
        private LiveOpsSystem _system;
        private OffsetLiveOpsClock _clock;
        private SyncedLiveOpsClock _syncedClock;
        private LiveOpsDebugPanel _panel;
        private LiveOpsUnityRunner _runner;

        public LiveOpsSystem System => _system;
        public OffsetLiveOpsClock Clock => _clock;
        public LiveOpsDebugPanel Panel => _panel;
        public LiveOpsUnityRunner Runner => _runner;
        public IReadOnlyDictionary<string, int> Wallet => _wallet;
        public LiveEventCalendarAsset CalendarAsset => calendarAsset;

        /// <summary>JSON lịch mà demo đưa vào parser — đúng dạng một key remote config; chuỗi rỗng = remote trống.</summary>
        public string CalendarJson { get; private set; }

        /// <summary>Kết quả <c>ParseOrDefault</c> mà system đang chạy (<c>CameFromDefaultCalendar</c> = lịch lấy từ asset).</summary>
        public LiveEventCalendarParseResult CalendarResult { get; private set; }

        private void Awake()
        {
            BuildClocks();

            _panel = gameObject.AddComponent<LiveOpsDebugPanel>();
            _panel.ExtraStatusLine = FormatWallet;
            _panel.DrawExtraControls = DrawDemoControls;
            _runner = gameObject.AddComponent<LiveOpsUnityRunner>();

            UseRemoteCalendarJson(simulateRemoteConfig ? BuildRemoteCalendarJson(DateTime.UtcNow) : string.Empty);
        }

        private void OnDestroy()
        {
            if (_system != null) LiveOpsSystemRegistry.Unregister(_system);
        }

        // ---------------------------------------------------------------- Lắp ráp

        /// <summary>
        /// Dựng lại system với một JSON remote khác — như game nhận remote config rồi mới <c>Build()</c> (V-18). Chuỗi rỗng = remote
        /// trống → lịch trong asset. Đồng hồ, kho PlayerPrefs, panel và runner giữ nguyên để test PlayMode đổi nguồn lịch ngay trong
        /// scene đã nạp mà không phải dựng scene thứ hai.
        /// </summary>
        public void UseRemoteCalendarJson(string remoteJson)
        {
            if (_system != null) LiveOpsSystemRegistry.Unregister(_system);

            CalendarJson = remoteJson ?? string.Empty;
            // Một nhánh cho cả remote lẫn asset: game không tự rẽ "có remote thì Parse, không thì asset" — hàm này đã giữ luật
            // "JSON đọc được nhưng có mục hỏng thì vẫn dùng remote, không trộn với asset".
            CalendarResult = JsonLiveEventCalendarParser.ParseOrDefault(CalendarJson, calendarAsset);

            var builder = new LiveOpsSystemBuilder(SystemId)
                          .WithClock(_clock)
                          // CombinedCalendar chứ không phải Calendar: Calendar chỉ là phần đợt cố định, mất luật lặp của đợt đua.
                          .WithCalendar(CalendarResult.CombinedCalendar)
                          .WithTextStore(_store)
                          .WithRewardGranter(new DelegateLiveOpsRewardGranter(GrantToWallet));
            RegisterEventTypes(builder);
            _system = builder.Build();
            LiveOpsSystemRegistry.Register(_system);

            _panel.CalendarProblems = CalendarResult.Problems;
            _panel.Bind(_system, _clock, _syncedClock);
            _runner.Bind(_system, _syncedClock);
        }

        private void BuildClocks()
        {
            _store = new PlayerPrefsLiveOpsTextStore(StoreKeyPrefix);
            ILiveOpsClock baseClock = new SystemLiveOpsClock();
            if (useServerTime)
            {
                _syncedClock = new SyncedLiveOpsClock(new HttpDateHeaderServerTimeSource(serverTimeUrl), _store);
                baseClock = _syncedClock;
            }
            _clock = new OffsetLiveOpsClock(baseClock);
        }

        /// <summary>
        /// Loại event lấy từ asset (requiresJoin của treasure-hunt → ExplicitJoin); demo chỉ cấp luật quà và điều kiện theo loại
        /// vì asset không mang được code. Scene dựng tay thiếu asset thì vẫn chạy được với hai loại của demo — nhưng báo lỗi, vì
        /// như vậy nhánh "remote trống → asset" không thử được.
        /// </summary>
        private void RegisterEventTypes(LiveOpsSystemBuilder builder)
        {
            if (calendarAsset != null)
            {
                builder.WithEventTypesFrom(calendarAsset, CompletionRuleFor, EligibilityFor);
                return;
            }

            Debug.LogError("[LiveOps Demo] Thiếu asset lịch — dựng lại scene bằng Tools/DreamTech/LiveOps/Demo/Build Demo Scene.", this);
            builder.WithEventType(RaceEventType, CompletionRuleFor(RaceEventType))
                   .WithEventType(HuntEventType, CompletionRuleFor(HuntEventType), EligibilityFor(HuntEventType), LiveEventJoinPolicy.ExplicitJoin);
        }

        private ILiveEventCompletionRule CompletionRuleFor(string eventType)
        {
            if (string.Equals(eventType, RaceEventType, StringComparison.Ordinal)) return new DelegateLiveEventCompletionRule(RaceReward);
            if (string.Equals(eventType, HuntEventType, StringComparison.Ordinal)) return new DelegateLiveEventCompletionRule(HuntReward);
            return null;
        }

        private ILiveEventEligibility EligibilityFor(string eventType)
        {
            if (!string.Equals(eventType, HuntEventType, StringComparison.Ordinal)) return null;
            return new DelegateLiveEventEligibility((instance, nowUtc) => playerLevel >= huntUnlockLevel);
        }

        /// <summary>
        /// JSON định dạng 2 như remote config sẽ trả: luật lặp của đợt đua, một đợt săn bắt đầu 00:00 UTC hôm nay (id theo ngày nên ổn
        /// định trong ngày) và một mục cố ý hỏng để thấy lịch bỏ mục sai và panel liệt kê lý do.
        ///
        /// <para>Ghép chuỗi tay thay vì gọi LiveEventCalendarJsonWriter: bộ ghi tính SHA-256, mà đường game của demo phải giống game
        /// thật — không đụng hash để build IL2CPP (R-28) chứng minh được parser không cần nó.</para>
        /// </summary>
        private string BuildRemoteCalendarJson(DateTime utcNow)
        {
            DateTime startUtc = utcNow.Date;
            DateTime endUtc = startUtc.AddHours(huntDurationHours);
            int activeHours = Mathf.Min(raceActiveHours, racePeriodHours);
            return "{\"version\":2," +
                   "\"recurring\":[" +
                   "{\"type\":\"" + RaceEventType + "\"," +
                   "\"anchorUtc\":\"" + RaceAnchorUtcText + "\"," +
                   "\"idPrefix\":\"" + RaceIdPrefix + "\"," +
                   "\"periodHours\":" + racePeriodHours.ToString(CultureInfo.InvariantCulture) + "," +
                   "\"activeHours\":" + activeHours.ToString(CultureInfo.InvariantCulture) + "}" +
                   "]," +
                   "\"events\":[" +
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
