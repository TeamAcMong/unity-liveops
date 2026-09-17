namespace DreamTech.LiveOps.Unity
{
    /// <summary>
    /// Cách <see cref="JsonLiveEventCalendarParser.ParseOrDefault(string, LiveEventCalendarAsset, LiveEventCalendarRemoteFailurePolicy)"/>
    /// xử lý JSON remote CÓ CHỮ nhưng <c>JsonUtility</c> không đọc được (Q-9). Là enum thay vì bool để game đọc code hiểu
    /// ngay hệ quả của từng lựa chọn, và để thêm cách thứ ba sau này mà không đổi chữ ký. Chỉ thêm giá trị vào cuối.
    /// </summary>
    public enum LiveEventCalendarRemoteFailurePolicy
    {
        /// <summary>
        /// Hành vi 0.1.0, từ 0.2.0 phải CHỌN TAY: lịch rỗng + Problem "JSON lịch event hỏng: …". Người chơi không thấy đợt
        /// mới mở, nhưng không bao giờ thấy lại lịch trong build có thể cũ hơn bản đã đăng (đợt đã gỡ mở lại, id đổi làm
        /// mất tiến độ).
        /// </summary>
        KeepRemoteResult = 0,

        /// <summary>
        /// MẶC ĐỊNH từ 0.2.0 (Q-9): dùng lịch trong asset + Problem báo remote hỏng. Người chơi vẫn có event, đổi lại lịch
        /// có thể cũ hơn bản đã đăng. Chọn làm mặc định vì "cả game không đợt nào chạy" là hỏng nặng hơn "lịch hơi cũ".
        /// </summary>
        UseDefaultCalendar = 1,
    }
}
