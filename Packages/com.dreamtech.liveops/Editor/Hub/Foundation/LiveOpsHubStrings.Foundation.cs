namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Foundation (G-HUBBASE): caption tầng, đơn vị của bộ định dạng, nhãn phím dự phòng. Đơn vị tiếng Việt nằm
    /// ở đây (không trong <see cref="LiveOpsHubFormat"/>) để mọi chữ hiện trên UI chung một nguồn ([FD §6.1]).
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Caption tầng rail — viết HOA sẵn vì USS không có text-transform.
        internal const string StageCaptionConfigure = "CẤU HÌNH";
        internal const string StageCaptionSchedule = "LÊN LỊCH";
        internal const string StageCaptionCheck = "KIỂM";
        internal const string StageCaptionExport = "XUẤT";
        internal const string StageCaptionRun = "CHẠY";

        // Sức khoẻ
        internal const string StaleHealthReason = "Kết quả cũ — kiểm lại để cập nhật.";
        internal const string HealthReasonRequiredFormat = "Trạng thái {0} bắt buộc có lý do — người dùng phải biết vì sao và làm gì tiếp.";
        internal const string DisabledReasonRequired = "Nút bị khoá bắt buộc có lý do — nút disabled không nói vì sao là người dùng kẹt.";

        // Thời lượng: đầy đủ "15 giờ 13 phút" · có ngày thì bỏ phút "3 ngày 18 giờ" · gọn (--compact) "15g 13p".
        internal const string DurationDayUnit = "ngày";
        internal const string DurationHourUnit = "giờ";
        internal const string DurationMinuteUnit = "phút";
        internal const string CompactDayUnit = "n";
        internal const string CompactHourUnit = "g";
        internal const string CompactMinuteUnit = "p";

        // Tương đối: "sau 11 giờ 13 phút" · "còn 19 ngày 15 giờ" · "13 giờ trước".
        internal const string RelativeFuturePrefix = "sau";
        internal const string RelativePastSuffix = "trước";
        internal const string RemainingPrefix = "còn";

        // Ngày giờ
        internal const string UtcLabel = "UTC";
        internal const string DeviceTimeSuffix = "giờ máy";
        internal const string ByteUnit = "byte";
        internal const string IsoWeekPrefix = "Tuần";
        internal const string Monday = "thứ Hai";
        internal const string Tuesday = "thứ Ba";
        internal const string Wednesday = "thứ Tư";
        internal const string Thursday = "thứ Năm";
        internal const string Friday = "thứ Sáu";
        internal const string Saturday = "thứ Bảy";
        internal const string Sunday = "Chủ nhật";

        /// <summary>
        /// Danh sách ĐÓNG ký hiệu được viết vào Label ([FD §2.9]): có đủ trong Inter-Regular SDF (font Label mặc định) ở
        /// 2022.3 và 6000.6 ([API §12.3]). Ký hiệu ngoài danh sách (‹ › ▸ ▾ ✕) vẽ bằng <see cref="LiveOpsChevron"/> hoặc icon.
        /// </summary>
        internal const string HubGlyphs = "…·–—×←→↑↓≥⌘⇧⌥⌫";

        // Nhãn phím khi font thiếu ký hiệu phím của macOS — in chữ thay cho ô vuông trống.
        internal const string KeyLabelCommand = "Cmd";
        internal const string KeyLabelShift = "Shift";
        internal const string KeyLabelOption = "Alt";
        internal const string KeyLabelBackspace = "Backspace";
    }
}
