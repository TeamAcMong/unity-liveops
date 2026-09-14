namespace DreamTech.LiveOps
{
    /// <summary>
    /// Định dạng JSON lịch mà bộ ghi xuất ra. Số của giá trị = số <c>version</c> game đọc, để dấu đã đăng
    /// (<see cref="PublishedCalendarStamp.FormatVersion"/>) ghi thẳng <c>(int)format</c> mà không cần bảng đổi.
    /// </summary>
    public enum LiveEventCalendarJsonFormat
    {
        /// <summary>Chỉ mảng <c>events</c> — đúng README 0.1.0; luật lặp không được xuất.</summary>
        Version1 = 1,

        /// <summary><c>version</c> + <c>recurring</c> + <c>events</c> — parser 0.2.0.</summary>
        Version2 = 2,
    }
}
