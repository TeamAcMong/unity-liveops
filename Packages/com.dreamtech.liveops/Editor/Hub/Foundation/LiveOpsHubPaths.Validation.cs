using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Phần vùng Validation của <see cref="LiveOpsHubPaths"/> (V-5, G-VALIDATION): tên element viết tay trong
    /// <c>ValidationSection.uxml</c>. Đường dẫn UXML/USS của màn và của popover Đề xuất… đã có ở file gốc (G-SKELETON) nên file
    /// này chỉ khai tên element.
    /// <para>
    /// Cả mười hai tên tồn tại ở MỌI trạng thái của màn (chưa kiểm · đang kiểm · cũ · không còn lỗi · không có asset): probe CLI
    /// và <c>HubWindowTests.EverySection_RequiredElementsPresent</c> duyệt nhiều ngữ cảnh, nên phần nào chưa dùng thì ẩn bằng
    /// class chứ không bị gỡ khỏi cây.
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubPaths
    {
        internal static class ValidationElementNames
        {
            internal const string Body = "validation-body";

            /// <summary>HelpBox "kết quả cũ" đầu thân — hai lý do cũ (lịch đổi / đã qua mốc) dùng chung một element.</summary>
            internal const string Notice = "validation-notice";

            internal const string Toolbar = "validation-toolbar";
            internal const string Tabs = "validation-tabs";
            internal const string TypeMenu = "validation-type-menu";
            internal const string Search = "validation-search";
            internal const string Summary = "validation-summary";

            /// <summary>Hộp spinner + thanh tiến trình khi đang chạy 12 luật; ẩn ở mọi trạng thái khác.</summary>
            internal const string Progress = "validation-progress";

            internal const string Content = "validation-content";
            internal const string Groups = "validation-groups";
            internal const string Empty = "validation-empty";

            /// <summary>Pane Chi tiết 300px — sibling NGOÀI split để thành drawer ở <c>--medium</c> (7.5).</summary>
            internal const string Detail = "validation-detail";
        }

        /// <summary>
        /// Tài liệu luật mà link "Vì sao? (tài liệu luật)" của pane Chi tiết mở (7.5). Trùng <c>documentationUrl</c> trong
        /// <c>package.json</c> — neo là chính id luật nên README chỉ cần một tiêu đề mỗi luật.
        /// </summary>
        internal const string RuleDocumentationBaseUrl =
            "https://github.com/TeamAcMong/unity-liveops/blob/main/Packages/com.dreamtech.liveops/README.md";

        /// <summary>Địa chỉ tài liệu của một luật; id rỗng trả về trang README không neo thay vì một neo hỏng.</summary>
        internal static string RuleDocumentationUrl(string ruleId)
        {
            return string.IsNullOrEmpty(ruleId) ? RuleDocumentationBaseUrl : RuleDocumentationBaseUrl + "#" + ruleId;
        }

        /// <summary>Hợp đồng <see cref="IHubSection.RequiredElementNames"/> của màn Kiểm lịch.</summary>
        internal static readonly IReadOnlyList<string> RequiredValidationElementNames = Array.AsReadOnly(new[]
        {
            ValidationElementNames.Body, ValidationElementNames.Notice, ValidationElementNames.Toolbar,
            ValidationElementNames.Tabs, ValidationElementNames.TypeMenu, ValidationElementNames.Search,
            ValidationElementNames.Summary, ValidationElementNames.Progress, ValidationElementNames.Content,
            ValidationElementNames.Groups, ValidationElementNames.Empty, ValidationElementNames.Detail,
        });
    }
}
