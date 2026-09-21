namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Foundation (G-HUBBASE): caption tầng, đơn vị của bộ định dạng, nhãn phím dự phòng. Đơn vị tiếng Việt nằm
    /// ở đây (không trong <see cref="LiveOpsHubFormat"/>) để mọi chữ hiện trên UI chung một nguồn ([FD §6.1]).
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Caption tầng rail — viết HOA sẵn vì USS không có text-transform.
        internal static string StageCaptionConfigure => LiveOpsHubStringCatalog.Text(nameof(StageCaptionConfigure));
        internal static string StageCaptionSchedule => LiveOpsHubStringCatalog.Text(nameof(StageCaptionSchedule));
        internal static string StageCaptionCheck => LiveOpsHubStringCatalog.Text(nameof(StageCaptionCheck));
        internal static string StageCaptionExport => LiveOpsHubStringCatalog.Text(nameof(StageCaptionExport));
        internal static string StageCaptionRun => LiveOpsHubStringCatalog.Text(nameof(StageCaptionRun));

        // Sức khoẻ
        internal static string StaleHealthReason => LiveOpsHubStringCatalog.Text(nameof(StaleHealthReason));
        internal static string HealthReasonRequiredFormat => LiveOpsHubStringCatalog.Text(nameof(HealthReasonRequiredFormat));
        internal static string DisabledReasonRequired => LiveOpsHubStringCatalog.Text(nameof(DisabledReasonRequired));
        internal static string FindingCountNegative => LiveOpsHubStringCatalog.Text(nameof(FindingCountNegative));

        // Thời lượng: đầy đủ "15 giờ 13 phút" · có ngày thì bỏ phút "3 ngày 18 giờ" · gọn (--compact) "15g 13p".
        internal static string DurationDayUnit => LiveOpsHubStringCatalog.Text(nameof(DurationDayUnit));
        internal static string DurationHourUnit => LiveOpsHubStringCatalog.Text(nameof(DurationHourUnit));
        internal static string DurationMinuteUnit => LiveOpsHubStringCatalog.Text(nameof(DurationMinuteUnit));

        // (W9-24) Vế SỐ ÍT của ba đơn vị trên. Bản tiếng Anh đọc "1 day" nhưng "0 days"/"2 days" (luật số ít/số nhiều
        // Q-W5-2); tiếng Việt không chia số nên hai vế cùng một chữ, nhưng vẫn phải là KHOÁ RIÊNG để bảng chữ khai đủ hai
        // dạng — nơi gọi chọn vế theo số, không chỗ nào tự nối chuỗi "days" cho n = 1 nữa.
        internal static string DurationDayUnitSingular => LiveOpsHubStringCatalog.Text(nameof(DurationDayUnitSingular));
        internal static string DurationHourUnitSingular => LiveOpsHubStringCatalog.Text(nameof(DurationHourUnitSingular));
        internal static string DurationMinuteUnitSingular => LiveOpsHubStringCatalog.Text(nameof(DurationMinuteUnitSingular));

        internal static string CompactDayUnit => LiveOpsHubStringCatalog.Text(nameof(CompactDayUnit));
        internal static string CompactHourUnit => LiveOpsHubStringCatalog.Text(nameof(CompactHourUnit));
        internal static string CompactMinuteUnit => LiveOpsHubStringCatalog.Text(nameof(CompactMinuteUnit));

        // Tương đối: "sau 11 giờ 13 phút" · "còn 19 ngày 15 giờ" · "13 giờ trước".
        internal static string RelativeFuturePrefix => LiveOpsHubStringCatalog.Text(nameof(RelativeFuturePrefix));
        internal static string RelativePastSuffix => LiveOpsHubStringCatalog.Text(nameof(RelativePastSuffix));
        internal static string RemainingPrefix => LiveOpsHubStringCatalog.Text(nameof(RemainingPrefix));

        // Ngày giờ
        internal static string UtcLabel => LiveOpsHubStringCatalog.Text(nameof(UtcLabel));
        internal static string DeviceTimeSuffix => LiveOpsHubStringCatalog.Text(nameof(DeviceTimeSuffix));
        internal static string ByteUnit => LiveOpsHubStringCatalog.Text(nameof(ByteUnit));
        internal static string IsoWeekPrefix => LiveOpsHubStringCatalog.Text(nameof(IsoWeekPrefix));
        internal static string Monday => LiveOpsHubStringCatalog.Text(nameof(Monday));
        internal static string Tuesday => LiveOpsHubStringCatalog.Text(nameof(Tuesday));
        internal static string Wednesday => LiveOpsHubStringCatalog.Text(nameof(Wednesday));
        internal static string Thursday => LiveOpsHubStringCatalog.Text(nameof(Thursday));
        internal static string Friday => LiveOpsHubStringCatalog.Text(nameof(Friday));
        internal static string Saturday => LiveOpsHubStringCatalog.Text(nameof(Saturday));
        internal static string Sunday => LiveOpsHubStringCatalog.Text(nameof(Sunday));

        /// <summary>
        /// Danh sách ĐÓNG ký hiệu được viết vào Label ([FD §2.9]): có đủ trong Inter-Regular SDF (font Label mặc định) ở
        /// 2022.3 và 6000.6 ([API §12.3]). Ký hiệu ngoài danh sách (‹ › ▸ ▾ ✕) vẽ bằng <see cref="LiveOpsChevron"/> hoặc icon.
        /// </summary>
        internal static string HubGlyphs => LiveOpsHubStringCatalog.Text(nameof(HubGlyphs));

        /// <summary>
        /// Tên và mã ngắn của hai ngôn ngữ hub (D-L1). Đăng ký bằng <c>AddShared</c> — tên ngôn ngữ KHÔNG dịch: người đang thấy
        /// giao diện tiếng Anh vẫn phải nhận ra dòng tên tiếng mẹ đẻ của mình trong menu chọn ngôn ngữ.
        /// </summary>
        internal static string LanguageNameEnglish => LiveOpsHubStringCatalog.Text(nameof(LanguageNameEnglish));
        internal static string LanguageNameVietnamese => LiveOpsHubStringCatalog.Text(nameof(LanguageNameVietnamese));
        internal static string LanguageShortCodeEnglish => LiveOpsHubStringCatalog.Text(nameof(LanguageShortCodeEnglish));
        internal static string LanguageShortCodeVietnamese => LiveOpsHubStringCatalog.Text(nameof(LanguageShortCodeVietnamese));

        /// <summary>Tooltip của menu chọn ngôn ngữ ở header — nhãn chỉ là "EN"/"VI" nên tooltip phải nói menu này làm gì.</summary>
        internal static string LanguageMenuTooltip => LiveOpsHubStringCatalog.Text(nameof(LanguageMenuTooltip));

        // Nhãn phím khi font thiếu ký hiệu phím của macOS — in chữ thay cho ô vuông trống.
        internal static string KeyLabelCommand => LiveOpsHubStringCatalog.Text(nameof(KeyLabelCommand));
        internal static string KeyLabelShift => LiveOpsHubStringCatalog.Text(nameof(KeyLabelShift));
        internal static string KeyLabelOption => LiveOpsHubStringCatalog.Text(nameof(KeyLabelOption));
        internal static string KeyLabelBackspace => LiveOpsHubStringCatalog.Text(nameof(KeyLabelBackspace));
    }
}
