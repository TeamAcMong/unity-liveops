namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Hai nhóm test của LiveOps Hub (mục 9.1). <see cref="Logic"/> là core/parser/asset/model thuần — chạy được
    /// với cờ Unity <c>-nographics</c>. <see cref="UI"/> là test chạm layout/vẽ/phím/cửa sổ thật — layout ra NaN,
    /// không vẽ, không nhận phím khi Unity chạy <c>-nographics</c> [API §12.5] nên nhóm này bắt buộc chạy không có
    /// cờ đó. <c>run-editmode.sh --category Logic|UI|UxGate|all</c> chọn đúng cờ Unity theo các hằng này.
    /// <para>
    /// <see cref="UxGate"/> (đợt W8-UX) là tập con của <see cref="UI"/>: hành trình chạy SỰ KIỆN chuột/phím thật qua cửa sổ hub
    /// và kiểm bố cục tự động ở 6 cỡ cửa sổ. Test mang CẢ HAI category để lượt <c>--category UI</c> vẫn phủ nó, còn cổng đợt
    /// chạy riêng được bằng <c>--category UxGate</c> (luôn có đồ hoạ).
    /// </para>
    /// </summary>
    internal static class LiveOpsHubTestCategories
    {
        internal const string Logic = "LiveOpsHub.Logic";
        internal const string UI = "LiveOpsHub.UI";
        internal const string UxGate = "LiveOpsHub.UxGate";
    }
}
