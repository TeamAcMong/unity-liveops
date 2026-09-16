using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Menu chọn ngôn ngữ ở góc phải header 26px ([FD §3.3], D-L2). Nhãn là mã ngắn ("EN"/"VI") vì header chỉ cao 26px và đã có
    /// tiêu đề; mục menu là tên gốc của ngôn ngữ ("English", "Tiếng Việt") để người đang thấy giao diện lạ vẫn nhận ra tiếng của
    /// mình. Chọn mục = <see cref="LiveOpsHubLanguage.Set"/>; cửa sổ nghe <see cref="LiveOpsHubLanguage.Changed"/> và tự dựng lại.
    /// <para>
    /// Menu dựng bằng C# chứ không khai trong UXML: <c>ToolbarMenu</c> là type của <c>UnityEditor.UIElements</c>, khai trong UXML
    /// phải mở thêm namespace editor và thêm tên element vào danh sách element sống còn của khung — đắt hơn hẳn một dòng Add.
    /// </para>
    /// </summary>
    internal sealed class LiveOpsHubLanguageMenu
    {
        internal LiveOpsHubLanguageMenu(VisualElement header)
        {
            if (header == null) throw new ArgumentNullException(nameof(header));
            Element = new ToolbarMenu();
            Element.AddToClassList(LiveOpsHubClassNames.LanguageMenu);
            Element.tooltip = LiveOpsHubStrings.LanguageMenuTooltip;
            Rebuild();
            header.Add(Element);
        }

        internal ToolbarMenu Element { get; }

        /// <summary>Đổ lại nhãn và mục menu theo ngôn ngữ đang chọn (dấu tick đi theo mục đang dùng).</summary>
        internal void Rebuild()
        {
            LiveOpsHubLanguageId current = LiveOpsHubLanguage.Current;
            Element.text = LiveOpsHubLanguage.ShortCode(current);
            Element.menu.ClearItems();
            foreach (LiveOpsHubLanguageId language in LiveOpsHubLanguage.Available)
            {
                LiveOpsHubLanguageId chosen = language;
                Element.menu.AppendAction(
                    LiveOpsHubLanguage.NativeName(chosen),
                    action => LiveOpsHubLanguage.Set(chosen),
                    action => chosen == LiveOpsHubLanguage.Current
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal);
            }
        }
    }
}
