using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Mặt tiền live-ops cho game: đợt nào đang chạy, người chơi đã vào chưa, cộng điểm, nhận quà giữa đợt, khép đợt khi hết giờ,
    /// kết quả chờ UI. Mọi phần thay được (đồng hồ, lịch, nơi lưu, phát quà, luật từng loại event) cắm qua
    /// <see cref="LiveOpsSystemBuilder"/>; class này chỉ điều phối và giữ trạng thái của người chơi.
    ///
    /// <para>Bất biến:
    /// <list type="bullet">
    /// <item>Dữ liệu người chơi khoá theo id của TỪNG ĐỢT. Một đợt đã khép (hoặc đã dọn) không bao giờ vào lại, kể cả khi giờ bị vặn lùi.</item>
    /// <item>Mỗi bản ghi giữ bản sao khung giờ của đợt. Khi đợt chưa khép, lịch là nguồn đúng (remote config dời giờ kết thúc thì bản
    /// ghi theo); lịch xoá mất đợt thì bản ghi vẫn khép đúng giờ đã lưu và quà vẫn phát.</item>
    /// <item>Luật khép đợt chạy đúng một lần mỗi đợt. Quà được ghi vào hàng chờ TRƯỚC khi đưa cho game, nên app tắt giữa chừng
    /// không mất quà, gọi lại không phát trùng.</item>
    /// <item>Lỗi trong code của game (luật khép đợt, granter) không làm hỏng vòng Refresh — báo qua <see cref="CompletionRuleFailed"/>
    /// và <see cref="RewardGrantFailed"/>, lần sau thử lại.</item>
    /// </list></para>
    ///
    /// <para>Chỉ dùng trên main thread.</para>
    /// </summary>
    public sealed class LiveOpsSystem
    {
        internal const string CompletionGrantPrefix = "liveops.complete#";
        internal const string ClaimGrantPrefix = "liveops.claim#";

        private static readonly IReadOnlyList<LiveEventRecord> NoRecords = Array.Empty<LiveEventRecord>();

        private readonly ILiveOpsTextStore _textStore;
        private readonly ILiveOpsRewardGranter _rewardGranter;
        private readonly Dictionary<string, LiveEventTypeRules> _rulesByType = new Dictionary<string, LiveEventTypeRules>(StringComparer.Ordinal);
        private readonly List<string> _eventTypes = new List<string>();
        private readonly string _stateKey;
        private readonly LiveOpsLocalState _state;
        private bool _isRefreshing;

        internal LiveOpsSystem(string systemId, ILiveOpsClock clock, ILiveEventCalendar calendar, ILiveOpsTextStore textStore,
                               ILiveOpsRewardGranter rewardGranter, LiveOpsSettings settings, IEnumerable<LiveEventTypeRules> eventTypeRules)
        {
            SystemId = systemId;
            Clock = clock;
            Calendar = calendar;
            Settings = settings;
            _textStore = textStore;
            _rewardGranter = rewardGranter;
            foreach (LiveEventTypeRules rules in eventTypeRules)
            {
                _rulesByType.Add(rules.EventType, rules);
                _eventTypes.Add(rules.EventType);
            }
            _stateKey = "liveops." + systemId + ".state";
            _state = _textStore.TryRead(_stateKey, out string stored) ? LiveOpsLocalState.Decode(stored) : new LiveOpsLocalState();
        }

        public string SystemId { get; }
        public ILiveOpsClock Clock { get; }
        public ILiveEventCalendar Calendar { get; }
        public LiveOpsSettings Settings { get; }

        /// <summary>Các loại event đã đăng ký, theo thứ tự đăng ký.</summary>
        public IReadOnlyList<string> EventTypes => _eventTypes;

        public int PendingRewardCount => _state.PendingRewards.Count;

        /// <summary>Điểm, tham gia, quà chờ phát, kết quả hay bản ghi vừa đổi.</summary>
        public event Action StateChanged;

        /// <summary>Một đợt vừa khép (đã lưu, quà đã vào hàng chờ). Bắn một lần mỗi đợt, kể cả đợt hết giờ lúc app đang tắt.</summary>
        public event Action<LiveEventRecord> EventFinalized;

        /// <summary>Luật khép đợt của game ném lỗi; đợt đó chưa khép và được thử lại ở lần Refresh sau.</summary>
        public event Action<LiveEventRecord, Exception> CompletionRuleFailed;

        /// <summary>Granter của game ném lỗi; gói quà vẫn nằm trong hàng chờ.</summary>
        public event Action<string, Exception> RewardGrantFailed;

        public bool IsRegistered(string eventType)
        {
            return eventType != null && _rulesByType.ContainsKey(eventType);
        }

        public LiveEventTypeRules GetRules(string eventType)
        {
            return RequireRules(eventType);
        }

        // ---------------------------------------------------------------- Đọc

        public LiveEventStatus GetStatus(string eventType)
        {
            LiveEventTypeRules rules = RequireRules(eventType);
            DateTime nowUtc = Clock.UtcNow;
            bool isClockTrusted = Clock.IsTrusted;
            LiveEventInstance instance = FindCurrentOrNext(eventType, nowUtc);
            if (instance == null) return new LiveEventStatus(eventType, null, nowUtc, false, null, false, isClockTrusted);

            LiveEventRecordData record = _state.FindRecord(instance.EventId);
            return new LiveEventStatus(eventType, instance, nowUtc, rules.Eligibility.IsEligible(instance, nowUtc),
                                       record?.ToSnapshot(), IsFinished(instance.EventId, record), isClockTrusted);
        }

        /// <summary>Bản ghi của một đợt; null nếu người chơi chưa vào hoặc bản ghi đã được dọn.</summary>
        public LiveEventRecord GetRecord(string eventId)
        {
            LiveEventRecordData record = eventId != null ? _state.FindRecord(eventId) : null;
            return record?.ToSnapshot();
        }

        public IReadOnlyList<LiveEventRecord> GetRecords()
        {
            var records = new List<LiveEventRecord>(_state.Records.Count);
            foreach (LiveEventRecordData record in _state.Records) records.Add(record.ToSnapshot());
            return records;
        }

        /// <summary>Các đợt đã khép mà UI chưa báo kết quả, đợt kết thúc sớm nhất trước. Màn Home hỏi danh sách này mỗi lần hiện.</summary>
        public IReadOnlyList<LiveEventRecord> GetPendingResults()
        {
            var results = new List<LiveEventRecord>();
            foreach (LiveEventRecordData record in _state.Records)
            {
                if (record.IsFinalized && !record.IsAcknowledged) results.Add(record.ToSnapshot());
            }
            results.Sort((left, right) => left.Instance.EndUtc.CompareTo(right.Instance.EndUtc));
            return results;
        }

        // ---------------------------------------------------------------- Ghi

        /// <summary>Vào đợt đang chạy của loại này (dùng cho <see cref="LiveEventJoinPolicy.ExplicitJoin"/>; loại tự vào gọi cũng được).</summary>
        public LiveEventJoinOutcome TryJoin(string eventType)
        {
            LiveEventTypeRules rules = RequireRules(eventType);
            RefreshCore();

            DateTime nowUtc = Clock.UtcNow;
            LiveEventInstance instance = FindActive(eventType, nowUtc);
            if (instance == null) return new LiveEventJoinOutcome(LiveEventJoinStatus.NoActiveEvent, null);

            LiveEventRecordData record = _state.FindRecord(instance.EventId);
            if (IsFinished(instance.EventId, record)) return new LiveEventJoinOutcome(LiveEventJoinStatus.AlreadyFinished, record?.ToSnapshot());
            if (record != null) return new LiveEventJoinOutcome(LiveEventJoinStatus.AlreadyJoined, record.ToSnapshot());
            if (!rules.Eligibility.IsEligible(instance, nowUtc)) return new LiveEventJoinOutcome(LiveEventJoinStatus.NotEligible, null);

            record = Join(instance, nowUtc);
            SaveAndNotify();
            return new LiveEventJoinOutcome(LiveEventJoinStatus.Joined, record.ToSnapshot());
        }

        /// <summary>
        /// Cộng điểm vào đợt đang chạy. Không chờ mạng. <paramref name="grantId"/> (tuỳ chọn) chống cộng trùng khi cùng một hành động
        /// có thể báo lại — vd "win-level-57"; để null nếu mỗi lần gọi là một lần cộng riêng.
        /// </summary>
        public LiveEventProgressOutcome AddPoints(string eventType, long points, string grantId = null)
        {
            LiveEventTypeRules rules = RequireRules(eventType);
            if (points <= 0) return new LiveEventProgressOutcome(LiveEventProgressStatus.InvalidAmount, null, 0);
            RefreshCore();

            DateTime nowUtc = Clock.UtcNow;
            LiveEventInstance instance = FindActive(eventType, nowUtc);
            if (instance == null) return new LiveEventProgressOutcome(LiveEventProgressStatus.NoActiveEvent, null, 0);

            LiveEventRecordData record = _state.FindRecord(instance.EventId);
            if (IsFinished(instance.EventId, record))
            {
                return new LiveEventProgressOutcome(LiveEventProgressStatus.AlreadyFinished, record?.ToSnapshot(), 0);
            }
            if (record == null)
            {
                if (!rules.Eligibility.IsEligible(instance, nowUtc)) return new LiveEventProgressOutcome(LiveEventProgressStatus.NotEligible, null, 0);
                if (rules.JoinPolicy == LiveEventJoinPolicy.ExplicitJoin) return new LiveEventProgressOutcome(LiveEventProgressStatus.NotJoined, null, 0);
                record = Join(instance, nowUtc);
            }
            else if (!string.IsNullOrEmpty(grantId) && record.RecentGrantIds.Contains(grantId))
            {
                return new LiveEventProgressOutcome(LiveEventProgressStatus.DuplicateGrant, record.ToSnapshot(), 0);
            }

            long before = record.Points;
            record.Points = points > long.MaxValue - before ? long.MaxValue : before + points;
            if (!string.IsNullOrEmpty(grantId)) RememberGrantId(record, grantId);
            SaveAndNotify();
            return new LiveEventProgressOutcome(LiveEventProgressStatus.Added, record.ToSnapshot(), record.Points - before);
        }

        /// <summary>Lưu trạng thái riêng của module event vào bản ghi. False khi chưa vào đợt hoặc đợt đã khép.</summary>
        public bool TrySetCustomState(string eventId, string customState)
        {
            LiveEventRecordData record = eventId != null ? _state.FindRecord(eventId) : null;
            if (record == null || record.IsFinalized) return false;

            customState = customState ?? string.Empty;
            if (string.Equals(record.CustomState, customState, StringComparison.Ordinal)) return true;
            record.CustomState = customState;
            SaveAndNotify();
            return true;
        }

        /// <summary>
        /// Nhận một phần quà giữa đợt (mốc battle pass, rương đạt điểm...). Mỗi <paramref name="claimKey"/> chỉ nhận được một lần mỗi
        /// đợt; module tự quyết đủ điều kiện hay chưa trước khi gọi. Gọi lại khi kết quả là <see cref="LiveOpsClaimStatus.Deferred"/>
        /// sẽ thử phát lại đúng gói đã chốt.
        /// </summary>
        public LiveOpsClaimOutcome ClaimReward(string eventId, string claimKey, LiveOpsRewardBundle bundle)
        {
            LiveEventInstance.ValidateIdentifier(eventId, nameof(eventId), "Event id");
            LiveEventInstance.ValidateIdentifier(claimKey, nameof(claimKey), "Khoá nhận quà");
            if (bundle == null) throw new ArgumentNullException(nameof(bundle));

            string grantId = ClaimGrantPrefix + eventId + LiveEventInstance.ReservedSeparator + claimKey;
            PendingLiveOpsReward pending = _state.FindPendingReward(grantId);
            if (pending != null) return GrantOne(pending);

            RefreshCore();
            LiveEventRecordData record = _state.FindRecord(eventId);
            if (record == null) return new LiveOpsClaimOutcome(LiveOpsClaimStatus.NotAvailable, bundle);
            if (record.ClaimedKeys.Contains(claimKey)) return new LiveOpsClaimOutcome(LiveOpsClaimStatus.AlreadyClaimed, bundle);
            if (record.IsFinalized) return new LiveOpsClaimOutcome(LiveOpsClaimStatus.NotAvailable, bundle);

            record.ClaimedKeys.Add(claimKey);
            if (bundle.IsEmpty)
            {
                SaveAndNotify();
                return new LiveOpsClaimOutcome(LiveOpsClaimStatus.Granted, bundle);
            }

            pending = new PendingLiveOpsReward(grantId, eventId, bundle);
            _state.PendingRewards.Add(pending);
            SaveAndNotify();
            return GrantOne(pending);
        }

        /// <summary>UI đã báo kết quả đợt này cho người chơi. False khi đợt chưa khép hoặc đã báo rồi.</summary>
        public bool AcknowledgeResult(string eventId)
        {
            LiveEventRecordData record = eventId != null ? _state.FindRecord(eventId) : null;
            if (record == null || !record.IsFinalized || record.IsAcknowledged) return false;
            record.IsAcknowledged = true;
            SaveAndNotify();
            return true;
        }

        /// <summary>
        /// Khép các đợt đã hết giờ, cập nhật khung giờ theo lịch, dọn bản ghi cũ, phát quà còn nợ. Trả các đợt vừa khép trong lần
        /// gọi này. Gọi định kỳ (<c>LiveOpsUnityRunner</c> làm sẵn) và ở các mốc quan trọng (mở Home). Gọi thừa không sao.
        /// </summary>
        public IReadOnlyList<LiveEventRecord> Refresh()
        {
            return RefreshCore();
        }

        /// <summary>Phát lại các gói quà game chưa nhận được. Trả số gói đã phát.</summary>
        public int GrantPendingRewards()
        {
            if (_state.PendingRewards.Count == 0) return 0;
            int granted = 0;
            foreach (PendingLiveOpsReward pending in new List<PendingLiveOpsReward>(_state.PendingRewards))
            {
                if (!_state.PendingRewards.Contains(pending)) continue;
                if (GrantOne(pending).Status == LiveOpsClaimStatus.Granted) granted++;
            }
            return granted;
        }

        // ---------------------------------------------------------------- Công cụ debug / cheat

        /// <summary>Đặt điểm trực tiếp cho đợt chưa khép.</summary>
        public bool DebugSetPoints(string eventId, long points)
        {
            LiveEventRecordData record = eventId != null ? _state.FindRecord(eventId) : null;
            if (record == null || record.IsFinalized) return false;
            record.Points = points < 0 ? 0 : points;
            SaveAndNotify();
            return true;
        }

        /// <summary>Xoá toàn bộ trạng thái trên máy: bản ghi, quà chờ phát, id đã dọn.</summary>
        public void DebugClearLocalState()
        {
            _state.Records.Clear();
            _state.PendingRewards.Clear();
            _state.RetiredEventIds.Clear();
            _textStore.Delete(_stateKey);
            StateChanged?.Invoke();
        }

        // ---------------------------------------------------------------- Nội bộ

        private IReadOnlyList<LiveEventRecord> RefreshCore()
        {
            if (_isRefreshing) return NoRecords;
            _isRefreshing = true;

            var finalized = new List<LiveEventRecord>();
            var failures = new List<KeyValuePair<LiveEventRecord, Exception>>();
            bool changed = false;
            try
            {
                DateTime nowUtc = Clock.UtcNow;
                bool canFinalize = Clock.IsTrusted || !Settings.FinalizeRequiresTrustedClock;
                foreach (LiveEventRecordData record in new List<LiveEventRecordData>(_state.Records))
                {
                    if (record.IsFinalized) continue;
                    if (SyncWindowWithCalendar(record, nowUtc)) changed = true;
                    if (!canFinalize || record.Instance.PhaseAt(nowUtc) != LiveEventPhase.Ended) continue;
                    try
                    {
                        finalized.Add(FinalizeRecord(record, nowUtc));
                        changed = true;
                    }
                    catch (Exception exception)
                    {
                        failures.Add(new KeyValuePair<LiveEventRecord, Exception>(record.ToSnapshot(), exception));
                    }
                }
                if (RetireOldRecords(nowUtc)) changed = true;
                if (changed) Save();
            }
            finally
            {
                _isRefreshing = false;
            }

            if (changed) StateChanged?.Invoke();
            foreach (KeyValuePair<LiveEventRecord, Exception> failure in failures) CompletionRuleFailed?.Invoke(failure.Key, failure.Value);
            // Phát quà trước khi báo đợt đã khép: UI nghe EventFinalized thì ví đã có quà.
            GrantPendingRewards();
            foreach (LiveEventRecord record in finalized) EventFinalized?.Invoke(record);
            return finalized;
        }

        /// <summary>Gọi luật TRƯỚC khi đổi trạng thái: luật ném lỗi thì bản ghi còn nguyên để lần sau thử lại.</summary>
        private LiveEventRecord FinalizeRecord(LiveEventRecordData record, DateTime nowUtc)
        {
            LiveOpsRewardBundle reward = LiveOpsRewardBundle.None;
            bool isTypeRegistered = _rulesByType.TryGetValue(record.Instance.EventType, out LiveEventTypeRules rules);
            if (isTypeRegistered) reward = rules.CompletionRule.RewardFor(record.ToSnapshot()) ?? LiveOpsRewardBundle.None;

            record.IsFinalized = true;
            record.FinalizedUtc = nowUtc;
            record.CompletionReward = reward;
            // Loại event đã bị gỡ khỏi game thì không còn UI nào báo kết quả — đánh dấu đã báo để không treo trong danh sách chờ.
            if (!isTypeRegistered) record.IsAcknowledged = true;
            if (!reward.IsEmpty)
            {
                string grantId = CompletionGrantPrefix + record.EventId;
                if (_state.FindPendingReward(grantId) == null) _state.PendingRewards.Add(new PendingLiveOpsReward(grantId, record.EventId, reward));
            }
            return record.ToSnapshot();
        }

        /// <summary>
        /// Lịch là nguồn đúng khi đợt chưa khép. Tìm lại đợt theo id trong khung từ giờ bắt đầu đã lưu tới max(giờ kết thúc đã lưu,
        /// bây giờ) — đủ rộng để thấy đợt được kéo dài hay rút ngắn. Không thấy (lịch đã xoá đợt) thì giữ khung đã lưu.
        /// </summary>
        private bool SyncWindowWithCalendar(LiveEventRecordData record, DateTime nowUtc)
        {
            LiveEventInstance stored = record.Instance;
            DateTime searchEnd = stored.EndUtc > nowUtc ? stored.EndUtc : nowUtc;
            if (searchEnd < DateTime.MaxValue) searchEnd = searchEnd.AddTicks(1);

            foreach (LiveEventInstance candidate in Calendar.GetInstances(stored.EventType, stored.StartUtc, searchEnd))
            {
                if (candidate == null || !string.Equals(candidate.EventId, stored.EventId, StringComparison.Ordinal)) continue;
                if (candidate.Equals(stored)) return false;
                record.Instance = candidate;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Hạn giữ tính từ lúc KHÉP, không phải lúc đợt hết giờ: người chơi bỏ app lâu hơn hạn giữ rồi quay lại thì đợt vừa khép vẫn
        /// còn đủ thời gian để UI báo kết quả, thay vì bị dọn ngay trong cùng lần Refresh.
        /// </summary>
        private bool RetireOldRecords(DateTime nowUtc)
        {
            bool changed = false;
            for (int index = _state.Records.Count - 1; index >= 0; index--)
            {
                LiveEventRecordData record = _state.Records[index];
                if (!record.IsFinalized || nowUtc - record.FinalizedUtc <= Settings.RecordRetention) continue;
                if (_state.HasPendingRewardFor(record.EventId)) continue;
                _state.Records.RemoveAt(index);
                _state.RememberRetired(record.EventId, Settings.MaximumRetiredEventIds);
                changed = true;
            }
            return changed;
        }

        private LiveEventRecordData Join(LiveEventInstance instance, DateTime nowUtc)
        {
            var record = new LiveEventRecordData { Instance = instance, JoinedUtc = nowUtc };
            _state.Records.Add(record);
            return record;
        }

        private bool IsFinished(string eventId, LiveEventRecordData record)
        {
            return (record != null && record.IsFinalized) || _state.IsRetired(eventId);
        }

        private void RememberGrantId(LiveEventRecordData record, string grantId)
        {
            record.RecentGrantIds.Add(grantId);
            while (record.RecentGrantIds.Count > Settings.MaximumRememberedGrantIds) record.RecentGrantIds.RemoveAt(0);
        }

        private LiveEventInstance FindCurrentOrNext(string eventType, DateTime nowUtc)
        {
            DateTime horizon = nowUtc > DateTime.MaxValue - Settings.UpcomingLookahead ? DateTime.MaxValue : nowUtc + Settings.UpcomingLookahead;
            LiveEventInstance best = null;
            foreach (LiveEventInstance candidate in Calendar.GetInstances(eventType, nowUtc, horizon))
            {
                if (candidate == null || candidate.EndUtc <= nowUtc) continue;
                if (!string.Equals(candidate.EventType, eventType, StringComparison.Ordinal)) continue;
                if (best == null || candidate.StartUtc < best.StartUtc) best = candidate;
            }
            return best;
        }

        private LiveEventInstance FindActive(string eventType, DateTime nowUtc)
        {
            LiveEventInstance instance = FindCurrentOrNext(eventType, nowUtc);
            return instance != null && instance.PhaseAt(nowUtc) == LiveEventPhase.Active ? instance : null;
        }

        private LiveOpsClaimOutcome GrantOne(PendingLiveOpsReward pending)
        {
            bool granted;
            try
            {
                granted = _rewardGranter.TryGrant(pending.GrantId, pending.Bundle);
            }
            catch (Exception exception)
            {
                granted = false;
                RewardGrantFailed?.Invoke(pending.GrantId, exception);
            }
            if (!granted) return new LiveOpsClaimOutcome(LiveOpsClaimStatus.Deferred, pending.Bundle);

            _state.PendingRewards.Remove(pending);
            SaveAndNotify();
            return new LiveOpsClaimOutcome(LiveOpsClaimStatus.Granted, pending.Bundle);
        }

        private LiveEventTypeRules RequireRules(string eventType)
        {
            if (eventType != null && _rulesByType.TryGetValue(eventType, out LiveEventTypeRules rules)) return rules;
            throw new ArgumentException("Loại event chưa đăng ký bằng LiveOpsSystemBuilder.WithEventType: " + (eventType ?? "null"),
                                        nameof(eventType));
        }

        private void Save()
        {
            _textStore.Write(_stateKey, _state.Encode());
        }

        private void SaveAndNotify()
        {
            Save();
            StateChanged?.Invoke();
        }
    }
}
