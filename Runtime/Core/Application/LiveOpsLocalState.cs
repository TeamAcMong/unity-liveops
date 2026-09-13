using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps
{
    /// <summary>Phần của người chơi trong một đợt — bản sửa được, chỉ <see cref="LiveOpsSystem"/> đụng vào.</summary>
    internal sealed class LiveEventRecordData
    {
        public LiveEventInstance Instance;
        public DateTime JoinedUtc;
        public long Points;
        public string CustomState = string.Empty;
        public readonly List<string> RecentGrantIds = new List<string>();
        public readonly List<string> ClaimedKeys = new List<string>();
        public bool IsFinalized;
        public DateTime FinalizedUtc = DateTime.MinValue;
        public bool IsAcknowledged;
        public LiveOpsRewardBundle CompletionReward = LiveOpsRewardBundle.None;

        public string EventId => Instance.EventId;

        public LiveEventRecord ToSnapshot()
        {
            return new LiveEventRecord(Instance, JoinedUtc, Points, CustomState, new List<string>(ClaimedKeys).AsReadOnly(),
                                       IsFinalized, FinalizedUtc, IsAcknowledged, CompletionReward);
        }
    }

    /// <summary>Quà đã chốt nhưng game chưa nhận được (granter trả false hoặc app tắt giữa chừng).</summary>
    internal sealed class PendingLiveOpsReward
    {
        public PendingLiveOpsReward(string grantId, string eventId, LiveOpsRewardBundle bundle)
        {
            GrantId = grantId;
            EventId = eventId;
            Bundle = bundle;
        }

        public string GrantId { get; }
        public string EventId { get; }
        public LiveOpsRewardBundle Bundle { get; }
    }

    /// <summary>Toàn bộ trạng thái live-ops trên máy người chơi: bản ghi từng đợt, quà chờ phát, id các đợt đã dọn.</summary>
    internal sealed class LiveOpsLocalState
    {
        private const int CurrentFormat = 1;

        public readonly List<LiveEventRecordData> Records = new List<LiveEventRecordData>();
        public readonly List<PendingLiveOpsReward> PendingRewards = new List<PendingLiveOpsReward>();

        /// <summary>Id các đợt đã khép và đã dọn bản ghi — vẫn nhớ để không cho vào lại khi giờ bị vặn lùi. Mới nhất ở cuối.</summary>
        public readonly List<string> RetiredEventIds = new List<string>();

        public LiveEventRecordData FindRecord(string eventId)
        {
            foreach (LiveEventRecordData record in Records)
            {
                if (string.Equals(record.EventId, eventId, StringComparison.Ordinal)) return record;
            }
            return null;
        }

        public bool IsRetired(string eventId)
        {
            return RetiredEventIds.Contains(eventId);
        }

        public void RememberRetired(string eventId, int maximumCount)
        {
            if (RetiredEventIds.Contains(eventId)) return;
            RetiredEventIds.Add(eventId);
            while (RetiredEventIds.Count > maximumCount) RetiredEventIds.RemoveAt(0);
        }

        public PendingLiveOpsReward FindPendingReward(string grantId)
        {
            foreach (PendingLiveOpsReward pending in PendingRewards)
            {
                if (string.Equals(pending.GrantId, grantId, StringComparison.Ordinal)) return pending;
            }
            return null;
        }

        public bool HasPendingRewardFor(string eventId)
        {
            foreach (PendingLiveOpsReward pending in PendingRewards)
            {
                if (string.Equals(pending.EventId, eventId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public string Encode()
        {
            var record = new LiveOpsTextRecord(CurrentFormat);

            record.SetInt("records", Records.Count);
            for (int index = 0; index < Records.Count; index++)
            {
                LiveEventRecordData data = Records[index];
                string prefix = "record" + index.ToString(CultureInfo.InvariantCulture);
                record.SetInstance(prefix + ".instance", data.Instance);
                record.SetDateTime(prefix + ".joined", data.JoinedUtc);
                record.SetLong(prefix + ".points", data.Points);
                record.SetString(prefix + ".custom", data.CustomState);
                record.SetStringList(prefix + ".grants", data.RecentGrantIds);
                record.SetStringList(prefix + ".claims", data.ClaimedKeys);
                record.SetBool(prefix + ".finalized", data.IsFinalized);
                record.SetDateTime(prefix + ".finalizedAt", data.FinalizedUtc);
                record.SetBool(prefix + ".acknowledged", data.IsAcknowledged);
                record.SetBundle(prefix + ".reward", data.CompletionReward);
            }

            record.SetInt("rewards", PendingRewards.Count);
            for (int index = 0; index < PendingRewards.Count; index++)
            {
                string prefix = "reward" + index.ToString(CultureInfo.InvariantCulture);
                record.SetString(prefix + ".grant", PendingRewards[index].GrantId);
                record.SetString(prefix + ".event", PendingRewards[index].EventId);
                record.SetBundle(prefix + ".bundle", PendingRewards[index].Bundle);
            }

            record.SetStringList("retired", RetiredEventIds);
            return record.Encode();
        }

        /// <summary>
        /// Chuỗi hỏng hoặc khác định dạng thì trả trạng thái rỗng — mất tiến độ event tốt hơn làm game crash lúc khởi động. Mục hỏng
        /// lẻ trong một chuỗi còn tốt thì bỏ riêng mục đó.
        /// </summary>
        public static LiveOpsLocalState Decode(string text)
        {
            var state = new LiveOpsLocalState();
            if (!LiveOpsTextRecord.TryDecode(text, out LiveOpsTextRecord record) || record.Format != CurrentFormat) return state;

            int recordCount = record.GetInt("records", 0);
            for (int index = 0; index < recordCount; index++)
            {
                string prefix = "record" + index.ToString(CultureInfo.InvariantCulture);
                LiveEventInstance instance = record.GetInstance(prefix + ".instance");
                if (instance == null || state.FindRecord(instance.EventId) != null) continue;

                var data = new LiveEventRecordData
                {
                    Instance = instance,
                    JoinedUtc = record.GetDateTime(prefix + ".joined", instance.StartUtc),
                    Points = record.GetLong(prefix + ".points", 0),
                    CustomState = record.GetString(prefix + ".custom", string.Empty),
                    IsFinalized = record.GetBool(prefix + ".finalized", false),
                    FinalizedUtc = record.GetDateTime(prefix + ".finalizedAt", instance.EndUtc),
                    IsAcknowledged = record.GetBool(prefix + ".acknowledged", false),
                    CompletionReward = record.GetBundle(prefix + ".reward"),
                };
                data.RecentGrantIds.AddRange(record.GetStringList(prefix + ".grants"));
                data.ClaimedKeys.AddRange(record.GetStringList(prefix + ".claims"));
                state.Records.Add(data);
            }

            int rewardCount = record.GetInt("rewards", 0);
            for (int index = 0; index < rewardCount; index++)
            {
                string prefix = "reward" + index.ToString(CultureInfo.InvariantCulture);
                string grantId = record.GetString(prefix + ".grant", string.Empty);
                string eventId = record.GetString(prefix + ".event", string.Empty);
                LiveOpsRewardBundle bundle = record.GetBundle(prefix + ".bundle");
                if (grantId.Length > 0 && eventId.Length > 0 && !bundle.IsEmpty && state.FindPendingReward(grantId) == null)
                {
                    state.PendingRewards.Add(new PendingLiveOpsReward(grantId, eventId, bundle));
                }
            }

            state.RetiredEventIds.AddRange(record.GetStringList("retired"));
            return state;
        }
    }
}
