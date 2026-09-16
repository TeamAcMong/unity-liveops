// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Sections/LiveOpsSampleSection.cs
// Mẫu vi phạm G-I18N §4 (bản tiếng Anh): English là ngôn ngữ mặc định của hub nên câu tiếng Anh viết thẳng vào chỗ hiển thị
// cũng là chữ ngoài catalog — luật cũ chỉ dò ký tự tiếng Việt nên không thấy dạng này.
// lint-expect: display-text-only-in-catalog
// lint-expect: display-text-only-in-catalog
// lint-expect: display-text-only-in-catalog
namespace DreamTech.LiveOps.Editor
{
    internal sealed class LiveOpsSampleSection
    {
        private const string TitleElementName = "hub-sample-title";

        internal VisualElement CreateView()
        {
            VisualElement root = new VisualElement();
            Label title = new Label("Publish calendar now");
            title.name = TitleElementName;
            title.tooltip = "Runs the calendar check first";
            Label ready = root.Q<Label>(TitleElementName);
            ready.text = "Every condition met";
            Label separator = new Label(" · ");
            root.Add(title);
            root.Add(separator);
            return root;
        }
    }
}
