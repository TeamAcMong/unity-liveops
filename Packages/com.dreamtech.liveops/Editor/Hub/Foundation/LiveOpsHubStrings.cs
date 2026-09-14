namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Nguồn chữ tiếng Việt duy nhất của LiveOps Hub — mọi label/tooltip/toast/thông báo lỗi hiển thị trên UI phải
    /// là một hằng ở đây, không viết chuỗi tiếng Việt trực tiếp trong code hay UXML (<c>code-lint.py</c> chặn ở
    /// mục 9.4). Lớp chia theo vùng màn hình để hai gói việc chạy song song không cùng sửa một file:
    /// <c>LiveOpsHubStrings.&lt;Vùng&gt;.cs</c> (vd <c>LiveOpsHubStrings.Shell.cs</c>, <c>LiveOpsHubStrings.Overview.cs</c>) —
    /// bảng vùng → gói ở mục 10.4 của kế hoạch. File gốc này (do G-SKELETON tạo ở W0) chỉ khai vỏ lớp <c>partial</c>,
    /// không chứa hằng nào.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
    }
}
