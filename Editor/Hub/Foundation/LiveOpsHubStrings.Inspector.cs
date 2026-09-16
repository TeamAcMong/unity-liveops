namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Inspector (G-INSPECTOR): inspector gọn của <c>LiveEventCalendarAsset</c> — tóm tắt asset, cảnh báo khi asset
    /// hỏng hoặc do bản package mới hơn lưu, gợi ý gỡ bớt lịch sử đăng, và nút "Mở trong LiveOps Hub" (mục 1.1, 4.1, 10.3).
    /// Mọi hằng mang tiền tố <c>Inspector</c> vì lớp <c>partial</c> dùng chung với mọi vùng khác; câu có số là chuỗi định dạng
    /// <c>{0}</c> để nơi gọi chèn số đã qua <see cref="LiveOpsHubFormat"/>.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // Ba dòng tóm tắt: người mở asset trong Project muốn biết ngay lịch này có gì mà không phải mở hub.
        internal static string InspectorSummaryFormat => LiveOpsHubStringCatalog.Text(nameof(InspectorSummaryFormat));
        internal static string InspectorRemoteConfigKeyFormat => LiveOpsHubStringCatalog.Text(nameof(InspectorRemoteConfigKeyFormat));
        internal static string InspectorIgnoredWarningsFormat => LiveOpsHubStringCatalog.Text(nameof(InspectorIgnoredWarningsFormat));

        // Lịch sử đăng: nói cả số lần lẫn mốc giờ của lần cuối vì đó là câu đầu tiên người review git muốn đối chiếu.
        internal static string InspectorPublishedFormat => LiveOpsHubStringCatalog.Text(nameof(InspectorPublishedFormat));
        internal static string InspectorPublishedNone => LiveOpsHubStringCatalog.Text(nameof(InspectorPublishedNone));

        // Schema lớn hơn bản code biết (mục 4.1): cảnh báo TRƯỚC khi người dùng bấm mở hub, vì chính lần hub lưu đè mới là
        // lúc field lạ biến mất — inspector là nơi duy nhất thấy asset mà chưa mở hub.
        internal static string InspectorNewerSchemaFormat => LiveOpsHubStringCatalog.Text(nameof(InspectorNewerSchemaFormat));

        // Asset hỏng: hai câu tách nhau vì hai nguyên nhân khác nhau — loại không dùng được (ToDocument bỏ) và mục bị bộ
        // biên dịch bỏ. Inspector chỉ NÊU SỐ rồi chỉ sang Kiểm lịch; không dựng lại lời giải thích của phiên (10.3).
        internal static string InspectorBrokenEventTypesFormat => LiveOpsHubStringCatalog.Text(nameof(InspectorBrokenEventTypesFormat));
        internal static string InspectorDroppedEntriesFormat => LiveOpsHubStringCatalog.Text(nameof(InspectorDroppedEntriesFormat));

        // Gợi ý gỡ bớt bản chụp khi lịch sử đăng quá dài (mục 4.1: > 50 thì gợi ý, KHÔNG tự xoá).
        internal static string InspectorManyStampsFormat => LiveOpsHubStringCatalog.Text(nameof(InspectorManyStampsFormat));

        // Đường duy nhất từ asset sang hub.
        internal static string InspectorOpenInHubButton => LiveOpsHubStringCatalog.Text(nameof(InspectorOpenInHubButton));
        internal static string InspectorOpenInHubTooltip => LiveOpsHubStringCatalog.Text(nameof(InspectorOpenInHubTooltip));

        // Vì sao inspector không cho sửa: mọi thao tác sửa lịch phải đi qua phiên của hub để có Undo có tên và có kiểm.
        internal static string InspectorEditHint => LiveOpsHubStringCatalog.Text(nameof(InspectorEditHint));

        // Lỗi lập trình (Unity dựng inspector với target không phải asset lịch) — không bao giờ do dữ liệu lịch hỏng.
        internal static string InspectorErrorAssetMissing => LiveOpsHubStringCatalog.Text(nameof(InspectorErrorAssetMissing));
    }
}
