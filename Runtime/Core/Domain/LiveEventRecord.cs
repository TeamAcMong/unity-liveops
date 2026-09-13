using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Bản chụp phần của người chơi trong một đợt: tham gia lúc nào, bao nhiêu điểm, đã nhận quà nào, đã khép chưa, UI đã báo
    /// kết quả chưa. Bất biến — đọc lại từ <see cref="LiveOpsSystem"/> sau mỗi thay đổi.
    ///
    /// <para><see cref="Instance"/> là khung giờ ĐÃ LƯU cùng bản ghi: nếu lịch bị xoá đợt này, bản ghi vẫn khép đúng giờ cũ.</para>
    /// </summary>
    public sealed class LiveEventRecord
    {
        internal LiveEventRecord(LiveEventInstance instance, DateTime joinedUtc, long points, string customState,
                                 IReadOnlyList<string> claimedKeys, bool isFinalized, DateTime finalizedUtc, bool isAcknowledged,
                                 LiveOpsRewardBundle completionReward)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            JoinedUtc = DateTime.SpecifyKind(joinedUtc, DateTimeKind.Utc);
            Points = points;
            CustomState = customState ?? string.Empty;
            ClaimedKeys = claimedKeys ?? Array.Empty<string>();
            IsFinalized = isFinalized;
            FinalizedUtc = DateTime.SpecifyKind(finalizedUtc, DateTimeKind.Utc);
            IsAcknowledged = isAcknowledged;
            CompletionReward = completionReward ?? LiveOpsRewardBundle.None;
        }

        public LiveEventInstance Instance { get; }
        public string EventId => Instance.EventId;
        public string EventType => Instance.EventType;
        public DateTime JoinedUtc { get; }
        public long Points { get; }

        /// <summary>Trạng thái riêng của module event (chuỗi do module tự mã hoá). Package chỉ lưu, không đọc.</summary>
        public string CustomState { get; }

        /// <summary>Các khoá quà đã nhận giữa đợt qua <see cref="LiveOpsSystem.ClaimReward"/>.</summary>
        public IReadOnlyList<string> ClaimedKeys { get; }

        /// <summary>Đợt đã hết giờ và đã chốt quà cuối đợt. Không cộng điểm, không nhận quà giữa đợt nữa.</summary>
        public bool IsFinalized { get; }

        /// <summary>
        /// Lúc đợt được khép, theo đồng hồ của hệ thống; <see cref="DateTime.MinValue"/> khi chưa khép. Bản ghi được giữ
        /// <see cref="LiveOpsSettings.RecordRetention"/> kể từ mốc này — người chơi quay lại sau thời gian dài vẫn kịp thấy kết quả.
        /// </summary>
        public DateTime FinalizedUtc { get; }

        /// <summary>UI đã báo kết quả cho người chơi (<see cref="LiveOpsSystem.AcknowledgeResult"/>).</summary>
        public bool IsAcknowledged { get; }

        /// <summary>Quà luật khép đợt trả về lúc chốt; <see cref="LiveOpsRewardBundle.None"/> khi chưa khép hoặc không có quà.</summary>
        public LiveOpsRewardBundle CompletionReward { get; }

        /// <summary>Đã khép nhưng UI chưa báo — màn kết quả hỏi danh sách này mỗi lần mở Home.</summary>
        public bool IsPendingResult => IsFinalized && !IsAcknowledged;

        public bool HasClaimed(string claimKey)
        {
            foreach (string key in ClaimedKeys)
            {
                if (string.Equals(key, claimKey, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public override string ToString()
        {
            return EventId + ": " + Points + " điểm" + (IsFinalized ? ", đã khép" : string.Empty) +
                   (IsPendingResult ? " (chờ báo kết quả)" : string.Empty);
        }
    }
}
