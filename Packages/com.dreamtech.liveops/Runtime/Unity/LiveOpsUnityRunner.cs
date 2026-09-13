using System;
using System.Threading;
using UnityEngine;

namespace DreamTech.LiveOps.Unity
{
    /// <summary>
    /// Nhịp tim của live-ops trong Unity: Refresh định kỳ theo giờ thật (không theo timeScale), thôi tin đồng hồ khi app vào nền,
    /// đồng bộ lại khi app quay lại, thử đồng bộ lại định kỳ khi chưa có mạng, ghi log lỗi của luật / granter.
    ///
    /// <para>Không bắt buộc: game tự gọi <see cref="LiveOpsSystem.Refresh"/>, <see cref="SyncedLiveOpsClock.Invalidate"/> và
    /// <see cref="SyncedLiveOpsClock.SyncAsync"/> ở vòng đời của mình cũng được.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LiveOpsUnityRunner : MonoBehaviour
    {
        public const float DefaultRefreshIntervalSeconds = 5f;
        public const float DefaultSyncRetryIntervalSeconds = 60f;
        private const float MinimumRefreshIntervalSeconds = 0.1f;
        private const float MinimumSyncRetryIntervalSeconds = 1f;

        [Tooltip("Bao lâu Refresh một lần (giây thật). Đợt hết giờ được khép trễ nhiều nhất bằng khoảng này; đồng hồ đếm ngược trên UI " +
                 "đọc thẳng GetStatus nên không bị ảnh hưởng.")]
        [SerializeField, Min(MinimumRefreshIntervalSeconds)] private float refreshIntervalSeconds = DefaultRefreshIntervalSeconds;

        [Tooltip("Chưa đồng bộ được giờ server thì bao lâu thử lại một lần (giây thật).")]
        [SerializeField, Min(MinimumSyncRetryIntervalSeconds)] private float syncRetryIntervalSeconds = DefaultSyncRetryIntervalSeconds;

        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private LiveOpsSystem _system;
        private SyncedLiveOpsClock _syncedClock;
        private float _nextRefreshTime;
        private float _nextSyncAttemptTime;
        private string _lastLoggedSyncError;

        public LiveOpsSystem System => _system;
        public SyncedLiveOpsClock SyncedClock => _syncedClock;

        /// <summary>Rút ngắn khoảng này có hiệu lực ngay, không phải chờ hết nhịp đang đếm.</summary>
        public float RefreshIntervalSeconds
        {
            get => refreshIntervalSeconds;
            set
            {
                refreshIntervalSeconds = Mathf.Max(MinimumRefreshIntervalSeconds, value);
                _nextRefreshTime = Mathf.Min(_nextRefreshTime, Time.unscaledTime + refreshIntervalSeconds);
            }
        }

        public float SyncRetryIntervalSeconds
        {
            get => syncRetryIntervalSeconds;
            set => syncRetryIntervalSeconds = Mathf.Max(MinimumSyncRetryIntervalSeconds, value);
        }

        /// <summary>Tạo runner trên một GameObject mới và gắn vào hệ thống cho sẵn.</summary>
        public static LiveOpsUnityRunner Create(LiveOpsSystem system, SyncedLiveOpsClock syncedClock = null, bool keepAcrossScenes = true)
        {
            var host = new GameObject("LiveOpsUnityRunner");
            if (keepAcrossScenes) DontDestroyOnLoad(host);
            var runner = host.AddComponent<LiveOpsUnityRunner>();
            runner.Bind(system, syncedClock);
            return runner;
        }

        /// <param name="syncedClock">
        /// Đồng hồ server mà system đang dùng (có thể nằm bên trong một <see cref="OffsetLiveOpsClock"/>). Null khi game dùng giờ máy.
        /// </param>
        public void Bind(LiveOpsSystem system, SyncedLiveOpsClock syncedClock = null)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            Unbind();
            _system = system;
            _syncedClock = syncedClock;
            _system.CompletionRuleFailed += LogCompletionRuleFailure;
            _system.RewardGrantFailed += LogRewardGrantFailure;
            if (_syncedClock != null) _syncedClock.TrustChanged += RefreshNow;
            RefreshNow();
            RequestSync();
        }

        public void RefreshNow()
        {
            if (_system == null) return;
            _nextRefreshTime = Time.unscaledTime + refreshIntervalSeconds;
            try
            {
                _system.Refresh();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        /// <summary>Đồng bộ giờ server ngay; bỏ qua nếu đang đồng bộ. Đồng hồ chuyển sang tin được thì TrustChanged tự Refresh.</summary>
        public async void RequestSync()
        {
            if (_syncedClock == null || _syncedClock.IsSyncing) return;
            _nextSyncAttemptTime = Time.unscaledTime + syncRetryIntervalSeconds;

            SyncedLiveOpsClock clock = _syncedClock;
            bool isSynced;
            try
            {
                isSynced = await clock.SyncAsync(_lifetime.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            if (this == null || !ReferenceEquals(clock, _syncedClock)) return;

            if (isSynced)
            {
                _lastLoggedSyncError = null;
            }
            else if (!string.Equals(_lastLoggedSyncError, clock.LastSyncError, StringComparison.Ordinal))
            {
                // Chỉ log khi lỗi đổi: offline thì cứ mỗi phút thử lại một lần, log lặp lại chỉ làm rác console.
                _lastLoggedSyncError = clock.LastSyncError;
                Debug.LogWarning("[LiveOps] Chưa đồng bộ được giờ server, dùng giờ ước lượng: " + clock.LastSyncError, this);
            }
        }

        private void Update()
        {
            if (_system == null) return;
            float now = Time.unscaledTime;
            if (now >= _nextRefreshTime) RefreshNow();
            if (_syncedClock != null && !_syncedClock.IsTrusted && !_syncedClock.IsSyncing && now >= _nextSyncAttemptTime) RequestSync();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (_system == null) return;
            if (isPaused)
            {
                if (_syncedClock != null) _syncedClock.Invalidate();
                return;
            }
            // Lần đồng bộ đang bay từ trước khi vào nền sẽ bị đồng hồ bỏ; cho phép thử lại ngay khi nó xong.
            _nextSyncAttemptTime = 0f;
            RefreshNow();
            RequestSync();
        }

        private void OnDestroy()
        {
            Unbind();
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        private void Unbind()
        {
            if (_system != null)
            {
                _system.CompletionRuleFailed -= LogCompletionRuleFailure;
                _system.RewardGrantFailed -= LogRewardGrantFailure;
            }
            if (_syncedClock != null) _syncedClock.TrustChanged -= RefreshNow;
            _system = null;
            _syncedClock = null;
        }

        private void LogCompletionRuleFailure(LiveEventRecord record, Exception exception)
        {
            Debug.LogError("[LiveOps] Luật khép đợt " + record.EventId + " ném lỗi — đợt sẽ được khép lại ở lần Refresh sau.", this);
            Debug.LogException(exception, this);
        }

        private void LogRewardGrantFailure(string grantId, Exception exception)
        {
            Debug.LogError("[LiveOps] Granter ném lỗi khi phát " + grantId + " — quà vẫn nằm trong hàng chờ.", this);
            Debug.LogException(exception, this);
        }
    }
}
