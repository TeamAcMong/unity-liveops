namespace DreamTech.LiveOps.Unity
{
    /// <summary>
    /// Cách <see cref="JsonLiveEventCalendarParser.ParseOrDefault(string, LiveEventCalendarAsset, LiveEventCalendarRemoteFailurePolicy)"/>
    /// xử lý JSON remote CÓ CHỮ nhưng KHÔNG DÙNG ĐƯỢC — <c>JsonUtility</c> không đọc nổi, hoặc đọc được mà không mang
    /// mảng lịch nào (<c>"{}"</c>, gõ sai tên mảng) (Q-9). Là enum thay vì bool để game đọc code hiểu ngay hệ quả của
    /// từng lựa chọn, và để thêm cách thứ ba sau này mà không đổi chữ ký. Chỉ thêm giá trị vào cuối.
    /// </summary>
    public enum LiveEventCalendarRemoteFailurePolicy
    {
        /// <summary>
        /// Giống hệt <see cref="JsonLiveEventCalendarParser.Parse"/> của 0.1.0, nhưng phải CHỌN TAY: lịch rỗng + Problem
        /// "JSON lịch event hỏng: …". Người chơi không thấy đợt mới mở, nhưng không bao giờ thấy lại lịch trong build có
        /// thể cũ hơn bản đã đăng (đợt đã gỡ mở lại, id đổi làm mất tiến độ).
        /// </summary>
        KeepRemoteResult = 0,

        /// <summary>
        /// MẶC ĐỊNH của API mới <c>ParseOrDefault</c> (Q-9; 0.1.0 chưa có API này): dùng lịch trong asset + Problem báo
        /// vì sao. Người chơi vẫn có event, đổi lại lịch có thể cũ hơn bản đã đăng. Chọn làm mặc định vì "cả game không
        /// đợt nào chạy" là hỏng nặng hơn "lịch hơi cũ".
        /// </summary>
        UseDefaultCalendar = 1,
    }
}
