// lint-sample-path: Packages/com.dreamtech.liveops/Tests/Editor/Hub/Sections/OverviewSampleTests.cs
// Mẫu vi phạm test: async Task (UTF 1.1.33 không có), WaitForEndOfFrame (treo batchmode), chạm EditorWindow không category UI.
// lint-expect: test-forbidden
// lint-expect: test-forbidden
// lint-expect: test-ui-category
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DreamTech.LiveOps.Editor.Tests
{
    public sealed class OverviewSampleTests
    {
        [Test]
        public async Task Overview_Loads_Async()
        {
            EditorWindow window = ScriptableObject.CreateInstance<EditorWindow>();
            await Task.Yield();
            window.Close();
        }

        [UnityTest]
        public IEnumerator Overview_Waits_EndOfFrame()
        {
            yield return new WaitForEndOfFrame();
        }
    }
}
