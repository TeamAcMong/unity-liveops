using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;

namespace DreamTech.LiveOps.Unity
{
    /// <summary>
    /// Bảng thử live-ops vẽ bằng IMGUI: đồng hồ, từng loại event (đợt, giai đoạn, điểm), kết quả chờ báo, quà chờ phát, lỗi của lịch.
    /// Dùng khi chưa có UI theo design, và để thử luật ngay trong game thật (bật bằng cheat).
    ///
    /// <para>Không phải UI sản phẩm: không theo design, không tối ưu. Nút tua giờ chỉ hiện khi <see cref="Bind"/> có
    /// <see cref="OffsetLiveOpsClock"/>; nút đồng bộ chỉ hiện khi có <see cref="SyncedLiveOpsClock"/>.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LiveOpsDebugPanel : MonoBehaviour
    {
        private const float ReferenceScreenHeight = 1920f;
        private const int MaximumLogLines = 8;
        private const long SmallPointStep = 10;
        private const long LargePointStep = 100;
        private static readonly TimeSpan PastBoundaryMargin = TimeSpan.FromSeconds(1);

        private readonly List<string> _log = new List<string>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private LiveOpsSystem _system;
        private OffsetLiveOpsClock _offsetClock;
        private SyncedLiveOpsClock _syncedClock;
        private Vector2 _scroll;

        /// <summary>Dòng trạng thái riêng của game (vd ví sau khi nhận quà). Để null thì không hiện.</summary>
        public Func<string> ExtraStatusLine { get; set; }

        /// <summary>Vẽ thêm nút riêng của game ở cuối bảng; tham số là chiều cao nút đã co theo màn hình.</summary>
        public Action<float> DrawExtraControls { get; set; }

        /// <summary>Lý do các mục lịch bị bỏ (vd <see cref="LiveEventCalendarParseResult.Problems"/>). Để null thì không hiện.</summary>
        public IReadOnlyList<string> CalendarProblems { get; set; }

        public bool IsVisible { get; set; } = true;
        public LiveOpsSystem System => _system;

        public event Action<string> Logged;

        /// <summary>Tạo panel trên một GameObject mới và gắn vào hệ thống cho sẵn.</summary>
        public static LiveOpsDebugPanel Create(LiveOpsSystem system, OffsetLiveOpsClock offsetClock = null,
                                               SyncedLiveOpsClock syncedClock = null, bool keepAcrossScenes = true)
        {
            var host = new GameObject("LiveOpsDebugPanel");
            if (keepAcrossScenes) DontDestroyOnLoad(host);
            var panel = host.AddComponent<LiveOpsDebugPanel>();
            panel.Bind(system, offsetClock, syncedClock);
            return panel;
        }

        /// <param name="offsetClock">Có thì hiện nút tua giờ. Phải là đồng hồ system đang dùng (hoặc bọc nó).</param>
        /// <param name="syncedClock">Có thì hiện trạng thái đồng bộ và nút đồng bộ.</param>
        public void Bind(LiveOpsSystem system, OffsetLiveOpsClock offsetClock = null, SyncedLiveOpsClock syncedClock = null)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            Unbind();
            _system = system;
            _offsetClock = offsetClock;
            _syncedClock = syncedClock;
            _system.EventFinalized += OnEventFinalized;
            _system.CompletionRuleFailed += OnCompletionRuleFailed;
            _system.RewardGrantFailed += OnRewardGrantFailed;
        }

        // ---------------------------------------------------------------- Thao tác (nút + test dùng chung)

        public LiveEventJoinOutcome Join(string eventType)
        {
            if (_system == null) return null;
            LiveEventJoinOutcome outcome = _system.TryJoin(eventType);
            Log("Tham gia " + eventType + ": " + outcome);
            return outcome;
        }

        /// <summary>Mỗi lần bấm là một lần cộng riêng nên không gửi grant id.</summary>
        public LiveEventProgressOutcome AddPoints(string eventType, long points)
        {
            if (_system == null) return null;
            LiveEventProgressOutcome outcome = _system.AddPoints(eventType, points);
            Log("+" + points + " điểm " + eventType + ": " + outcome);
            return outcome;
        }

        public void AdvanceTime(TimeSpan duration)
        {
            if (_system == null || _offsetClock == null) return;
            _offsetClock.Advance(duration);
            Log("Tua giờ +" + FormatDuration(duration));
            _system.Refresh();
        }

        /// <summary>Tua tới ngay sau lúc đợt sắp tới của loại này bắt đầu.</summary>
        public void AdvanceToStart(string eventType)
        {
            if (_system == null || _offsetClock == null) return;
            LiveEventStatus status = _system.GetStatus(eventType);
            if (status.IsUpcoming) AdvanceTime(status.TimeUntilStart + PastBoundaryMargin);
        }

        /// <summary>Tua tới ngay sau lúc đợt đang chạy của loại này kết thúc.</summary>
        public void AdvanceToEnd(string eventType)
        {
            if (_system == null || _offsetClock == null) return;
            LiveEventStatus status = _system.GetStatus(eventType);
            if (status.IsActive) AdvanceTime(status.TimeLeft + PastBoundaryMargin);
        }

        public async void SyncClock()
        {
            if (_syncedClock == null) return;
            bool isSynced;
            try
            {
                isSynced = await _syncedClock.SyncAsync(_lifetime.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            if (this == null) return;
            Log(isSynced ? "Đồng bộ giờ server: " + FormatUtc(_syncedClock.LastSyncedServerUtc) : "Đồng bộ lỗi: " + _syncedClock.LastSyncError);
        }

        public IReadOnlyList<LiveEventRecord> Refresh()
        {
            if (_system == null) return Array.Empty<LiveEventRecord>();
            IReadOnlyList<LiveEventRecord> finalized = _system.Refresh();
            if (finalized.Count == 0) Log("Refresh: không có đợt nào vừa khép");
            return finalized;
        }

        public bool AcknowledgeResult(string eventId)
        {
            if (_system == null) return false;
            bool isAcknowledged = _system.AcknowledgeResult(eventId);
            if (isAcknowledged) Log("Đã báo kết quả " + eventId);
            return isAcknowledged;
        }

        public int GrantPendingRewards()
        {
            if (_system == null) return 0;
            int granted = _system.GrantPendingRewards();
            Log("Phát lại quà chờ: " + granted + " gói, còn " + _system.PendingRewardCount);
            return granted;
        }

        /// <summary>Xoá trạng thái live-ops trên máy và đưa đồng hồ tua về giờ thật.</summary>
        public void ResetEverything()
        {
            if (_system == null) return;
            _system.DebugClearLocalState();
            if (_offsetClock != null) _offsetClock.Offset = TimeSpan.Zero;
            Log("Đã xoá sạch dữ liệu live-ops");
        }

        public void Log(string message)
        {
            _log.Add(message);
            if (_log.Count > MaximumLogLines) _log.RemoveAt(0);
            Debug.Log("[LiveOps] " + message);
            Logged?.Invoke(message);
        }

        // ---------------------------------------------------------------- Vòng đời

        private void OnDestroy()
        {
            Unbind();
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        private void Unbind()
        {
            if (_system == null) return;
            _system.EventFinalized -= OnEventFinalized;
            _system.CompletionRuleFailed -= OnCompletionRuleFailed;
            _system.RewardGrantFailed -= OnRewardGrantFailed;
            _system = null;
        }

        private void OnEventFinalized(LiveEventRecord record)
        {
            Log("Khép đợt " + record.EventId + ": " + record.Points + " điểm, quà cuối đợt " + record.CompletionReward);
        }

        private void OnCompletionRuleFailed(LiveEventRecord record, Exception exception)
        {
            Log("Luật khép đợt " + record.EventId + " lỗi: " + exception.GetBaseException().Message);
        }

        private void OnRewardGrantFailed(string grantId, Exception exception)
        {
            Log("Phát " + grantId + " lỗi: " + exception.GetBaseException().Message);
        }

        // ---------------------------------------------------------------- Vẽ

        private void OnGUI()
        {
            if (!IsVisible || _system == null) return;

            float scale = Mathf.Max(0.5f, Screen.height / ReferenceScreenHeight);
            GUI.skin.button.fontSize = Mathf.RoundToInt(26 * scale);
            GUI.skin.label.fontSize = Mathf.RoundToInt(24 * scale);
            GUI.skin.box.fontSize = Mathf.RoundToInt(24 * scale);
            float buttonHeight = 60f * scale;

            GUILayout.BeginArea(new Rect(10f, 10f, Screen.width - 20f, Screen.height - 20f), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawClock(buttonHeight);
            foreach (string eventType in _system.EventTypes) DrawEventType(eventType, buttonHeight);
            DrawPendingResults(buttonHeight);
            DrawCalendarProblems();
            DrawExtraControls?.Invoke(buttonHeight);
            if (GUILayout.Button("Xoá dữ liệu live-ops", GUILayout.Height(buttonHeight))) ResetEverything();
            DrawLog();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawClock(float buttonHeight)
        {
            ILiveOpsClock clock = _system.Clock;
            GUILayout.Label("Giờ: " + clock.UtcNow.ToString("u", CultureInfo.InvariantCulture) + " · " +
                            (clock.IsTrusted ? "tin được" : "CHƯA tin được"));
            if (_syncedClock != null)
            {
                GUILayout.Label("Server: " + FormatUtc(_syncedClock.LastSyncedServerUtc) + " · máy lệch " +
                                FormatDuration(_syncedClock.LastKnownDeviceOffset) +
                                (_syncedClock.IsSyncing ? " · đang đồng bộ..." : string.Empty) +
                                (_syncedClock.LastSyncError != null ? " · lỗi: " + _syncedClock.LastSyncError : string.Empty));
            }
            if (_offsetClock != null) GUILayout.Label("Đang tua: +" + FormatDuration(_offsetClock.Offset));
            if (ExtraStatusLine != null) GUILayout.Label(ExtraStatusLine());
            GUILayout.Label("Quà chờ phát: " + _system.PendingRewardCount);

            GUILayout.BeginHorizontal();
            if (_offsetClock != null)
            {
                if (GUILayout.Button("Tua +1h", GUILayout.Height(buttonHeight))) AdvanceTime(TimeSpan.FromHours(1));
                if (GUILayout.Button("Tua +1 ngày", GUILayout.Height(buttonHeight))) AdvanceTime(TimeSpan.FromDays(1));
            }
            if (_syncedClock != null && GUILayout.Button("Đồng bộ giờ", GUILayout.Height(buttonHeight))) SyncClock();
            if (GUILayout.Button("Refresh", GUILayout.Height(buttonHeight))) Refresh();
            if (_system.PendingRewardCount > 0 && GUILayout.Button("Phát quà chờ", GUILayout.Height(buttonHeight))) GrantPendingRewards();
            GUILayout.EndHorizontal();
        }

        private void DrawEventType(string eventType, float buttonHeight)
        {
            LiveEventStatus status = _system.GetStatus(eventType);
            GUILayout.Space(8f);
            GUILayout.Box(DescribeStatus(status, _system.GetRules(eventType)));

            GUILayout.BeginHorizontal();
            if (status.IsActive && !status.IsJoined && !status.IsFinished &&
                _system.GetRules(eventType).JoinPolicy == LiveEventJoinPolicy.ExplicitJoin &&
                GUILayout.Button("Tham gia", GUILayout.Height(buttonHeight)))
            {
                Join(eventType);
            }
            if (status.IsActive && !status.IsFinished)
            {
                if (GUILayout.Button("+" + SmallPointStep, GUILayout.Height(buttonHeight))) AddPoints(eventType, SmallPointStep);
                if (GUILayout.Button("+" + LargePointStep, GUILayout.Height(buttonHeight))) AddPoints(eventType, LargePointStep);
            }
            if (_offsetClock != null && status.IsUpcoming && GUILayout.Button("→ bắt đầu", GUILayout.Height(buttonHeight))) AdvanceToStart(eventType);
            if (_offsetClock != null && status.IsActive && GUILayout.Button("→ hết đợt", GUILayout.Height(buttonHeight))) AdvanceToEnd(eventType);
            GUILayout.EndHorizontal();
        }

        private void DrawPendingResults(float buttonHeight)
        {
            IReadOnlyList<LiveEventRecord> results = _system.GetPendingResults();
            if (results.Count == 0) return;
            GUILayout.Space(8f);
            GUILayout.Label("KẾT QUẢ CHỜ BÁO");
            foreach (LiveEventRecord result in results)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(result.EventId + ": " + result.Points + " điểm · " + result.CompletionReward);
                if (GUILayout.Button("Xem xong", GUILayout.Height(buttonHeight), GUILayout.ExpandWidth(false))) AcknowledgeResult(result.EventId);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawCalendarProblems()
        {
            if (CalendarProblems == null || CalendarProblems.Count == 0) return;
            GUILayout.Space(8f);
            GUILayout.Label("LỊCH BỎ " + CalendarProblems.Count + " MỤC");
            foreach (string problem in CalendarProblems) GUILayout.Label("• " + problem);
        }

        private void DrawLog()
        {
            GUILayout.Space(8f);
            for (int index = _log.Count - 1; index >= 0; index--) GUILayout.Label(_log[index]);
        }

        private static string DescribeStatus(LiveEventStatus status, LiveEventTypeRules rules)
        {
            string header = status.EventType.ToUpperInvariant() + (rules.JoinPolicy == LiveEventJoinPolicy.ExplicitJoin ? " (bấm tham gia)" : string.Empty);
            if (!status.HasInstance) return header + "\nKhông có đợt nào trong tầm nhìn";

            string timing = status.IsActive
                ? "đang chạy, còn " + FormatDuration(status.TimeLeft)
                : status.IsUpcoming
                    ? "bắt đầu sau " + FormatDuration(status.TimeUntilStart)
                    : "đã hết giờ";
            string player = status.IsFinished
                ? "đã khép"
                : status.IsJoined
                    ? status.Record.Points + " điểm" + (status.Record.ClaimedKeys.Count > 0 ? ", đã nhận: " + string.Join(", ", status.Record.ClaimedKeys) : string.Empty)
                    : status.IsEligible ? "chưa vào" : "CHƯA ĐỦ ĐIỀU KIỆN";
            return header + "\n" + status.Instance.EventId + " · " + timing + "\n" + player;
        }

        private static string FormatUtc(DateTime? utc)
        {
            return utc.HasValue ? utc.Value.ToString("u", CultureInfo.InvariantCulture) : "chưa có";
        }

        private static string FormatDuration(TimeSpan duration)
        {
            string sign = duration < TimeSpan.Zero ? "-" : string.Empty;
            if (duration < TimeSpan.Zero) duration = duration.Negate();
            if (duration.TotalDays >= 1) return sign + (int)duration.TotalDays + "d " + duration.Hours + "h";
            if (duration.TotalHours >= 1) return sign + (int)duration.TotalHours + "h " + duration.Minutes + "m";
            return sign + duration.Minutes + "m " + duration.Seconds + "s";
        }
    }
}
