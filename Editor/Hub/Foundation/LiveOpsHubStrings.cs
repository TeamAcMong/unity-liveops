namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Cửa duy nhất để code hub lấy chữ hiển thị — mọi label/tooltip/toast/thông báo lỗi trên UI phải là một thành viên ở
    /// đây, không viết chuỗi hiển thị trực tiếp trong code hay UXML (<c>code-lint.py</c> chặn ở mục 9.4). Lớp chia theo vùng
    /// màn hình để hai gói việc chạy song song không cùng sửa một file: <c>LiveOpsHubStrings.&lt;Vùng&gt;.cs</c>
    /// (vd <c>LiveOpsHubStrings.Shell.cs</c>) — bảng vùng → gói ở mục 10.4 của kế hoạch. File gốc này (do G-SKELETON tạo ở W0)
    /// chỉ khai vỏ lớp <c>partial</c>, không chứa thành viên nào.
    /// <para>
    /// Từ G-I18N (W3.5) hub có hai bộ chữ (Tiếng Việt + English) nên chữ KHÔNG còn nằm ở đây: mỗi thành viên là một property
    /// đọc catalog theo khoá, đúng một khuôn duy nhất (lint <c>strings-must-be-catalog-property</c> chặn mọi khuôn khác):
    /// </para>
    /// <code>
    /// internal static string ShellWindowTitle => LiveOpsHubStringCatalog.Text(nameof(ShellWindowTitle));
    /// </code>
    /// <para>
    /// <c>nameof</c> làm khoá nên khoá và tên thành viên không bao giờ lệch nhau. Câu của hai ngôn ngữ nằm ở
    /// <c>Language/LiveOpsHubStringCatalog.&lt;Vùng&gt;.cs</c> của cùng vùng đó; comment "vì sao" của từng câu ở lại ĐÂY, cạnh
    /// property, vì đó là chỗ người đọc code tìm tới. Thêm chuỗi mới = thêm property ở file vùng + một dòng đăng ký ở file
    /// catalog của vùng, đủ cả hai ngôn ngữ.
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
    }
}
