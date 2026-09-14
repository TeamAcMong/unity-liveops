// lint-sample-path: Packages/com.dreamtech.liveops/Tests/Editor/Hub/Shell/HubSampleTests.cs
// Mẫu đạt: test chạm EditorWindow có category UI; chỉ dùng tính năng UTF 1.1.33.
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace DreamTech.LiveOps.Editor.Tests
{
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class HubSampleTests
    {
        [UnityTest]
        public IEnumerator Window_Opens_HasLayout()
        {
            EditorWindow window = UnityEngine.ScriptableObject.CreateInstance<EditorWindow>();
            window.Show();
            int frames = 0;
            while (float.IsNaN(window.rootVisualElement.layout.width))
            {
                if (++frames > 60) Assert.Fail("Cửa sổ không có layout sau 60 vòng");
                yield return null;
            }
            window.Close();
        }
    }
}
