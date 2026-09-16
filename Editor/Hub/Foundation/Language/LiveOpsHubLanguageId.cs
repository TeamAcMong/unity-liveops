namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Hai ngôn ngữ của LiveOps Hub (D-L1). <see cref="English"/> = 0 vì là mặc định: giá trị pref lạ hoặc thiếu rơi về 0 mà
    /// vẫn ra đúng ngôn ngữ mặc định, không cần nhánh riêng. Thứ tự khai cũng là thứ tự mục trong menu chọn ngôn ngữ.
    /// </summary>
    internal enum LiveOpsHubLanguageId
    {
        English = 0,
        Vietnamese = 1,
    }
}
