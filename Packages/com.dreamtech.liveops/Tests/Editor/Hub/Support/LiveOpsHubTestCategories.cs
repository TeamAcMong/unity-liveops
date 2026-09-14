namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Hai nhóm test của LiveOps Hub (mục 9.1). <see cref="Logic"/> là core/parser/asset/model thuần — chạy được
    /// với cờ Unity <c>-nographics</c>. <see cref="UI"/> là test chạm layout/vẽ/phím/cửa sổ thật — layout ra NaN,
    /// không vẽ, không nhận phím khi Unity chạy <c>-nographics</c> [API §12.5] nên nhóm này bắt buộc chạy không có
    /// cờ đó. <c>run-editmode.sh --category Logic|UI|all</c> chọn đúng cờ Unity theo hai hằng này.
    /// </summary>
    internal static class LiveOpsHubTestCategories
    {
        internal const string Logic = "LiveOpsHub.Logic";
        internal const string UI = "LiveOpsHub.UI";
    }
}
