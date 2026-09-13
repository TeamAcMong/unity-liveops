using System;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Nơi lưu chuỗi theo khoá (PlayerPrefs, save system của game, cloud...). Package tự mã hoá trạng thái thành chuỗi nên game
    /// chỉ cần cài 3 hàm này.
    /// </summary>
    public interface ILiveOpsTextStore
    {
        bool TryRead(string key, out string value);
        void Write(string key, string value);
        void Delete(string key);
    }

    /// <summary>
    /// Phát quà vào kho đồ của game. Trả false khi chưa phát được lúc này (kho đồ chưa sẵn sàng) — quà nằm lại hàng chờ và được
    /// phát ở lần <see cref="LiveOpsSystem.GrantPendingRewards"/> sau. <paramref name="grantId"/> ổn định giữa các lần thử, game
    /// dùng để chống phát trùng nếu kho đồ có hỗ trợ. Ném exception cũng được coi là chưa phát (và báo qua
    /// <see cref="LiveOpsSystem.RewardGrantFailed"/>).
    /// </summary>
    public interface ILiveOpsRewardGranter
    {
        bool TryGrant(string grantId, LiveOpsRewardBundle bundle);
    }

    /// <summary>Người chơi có được vào đợt này không (level tối thiểu, nền tảng, nhóm A/B, không cho vào 12 giờ cuối...).</summary>
    public interface ILiveEventEligibility
    {
        bool IsEligible(LiveEventInstance instance, DateTime nowUtc);
    }

    /// <summary>
    /// Quà chốt lúc một đợt khép (hết giờ), tính từ bản ghi của người chơi. Chạy ĐÚNG MỘT LẦN cho mỗi đợt người chơi đã tham gia —
    /// kể cả khi đợt hết giờ lúc app đang tắt. Đây là chỗ "tự nhận quà chưa nhận" của battle pass: đọc
    /// <see cref="LiveEventRecord.ClaimedKeys"/> để biết mốc nào chưa nhận. Ném exception thì đợt chưa khép, lần Refresh sau thử lại.
    /// </summary>
    public interface ILiveEventCompletionRule
    {
        LiveOpsRewardBundle RewardFor(LiveEventRecord record);
    }
}
