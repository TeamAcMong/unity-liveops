using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Đổi skin chỉ bằng class root, KHÔNG đổi skin thật của Editor (SP-4: đổi skin bằng code ghi Preferences toàn máy và treo
    /// batchmode). Đường chính đã kiểm chứng SP-5: một class trên wrapper ghi đè <c>--unity-colors-window-background</c> →
    /// <c>CustomStyleResolvedEvent</c> của root bắn lại với màu mới → <see cref="LiveOpsHubSkin"/> bật/tắt
    /// <c>liveops-hub--skin-light</c>.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class HubSkinTests
    {
        private const string LightOverrideClass = "hub-skin-test-light-background";
        private const string DarkOverrideClass = "hub-skin-test-dark-background";
        private const string OverrideSheetText =
            "." + LightOverrideClass + " { --unity-colors-window-background: #C8C8C8; }\n" +
            "." + DarkOverrideClass + " { --unity-colors-window-background: #383838; }\n";

        private LiveOpsHubWindowTestScope _scope;
        private StyleSheet _overrideSheet;

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
            _scope = null;
            if (_overrideSheet != null) UnityEngine.Object.DestroyImmediate(_overrideSheet);
            _overrideSheet = null;
        }

        [UnityTest]
        public IEnumerator SkinProbe_ThemeVariableChange_TogglesLightClass()
        {
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();
            LiveOpsHubWindow window = _scope.Window;
            VisualElement hubRoot = window.HubRoot;
            VisualElement wrapper = window.rootVisualElement;
            bool startsLight = hubRoot.ClassListContains(LiveOpsHubClassNames.SkinLight);
            Assert.AreEqual(!EditorGUIUtility.isProSkin, startsLight, "class skin lúc dựng phải khớp skin thật của Editor");

            _overrideSheet = CreateStyleSheet(OverrideSheetText);
            wrapper.styleSheets.Add(_overrideSheet);
            int changesBefore = window.Skin.ChangeCount;

            // Ép nền về phía ngược với skin hiện tại → class phải đổi; gỡ → class về như cũ. Không đụng isProSkin.
            string towardsOpposite = startsLight ? DarkOverrideClass : LightOverrideClass;
            wrapper.AddToClassList(towardsOpposite);
            yield return WaitForClass(hubRoot, !startsLight);
            Assert.AreEqual(!startsLight, hubRoot.ClassListContains(LiveOpsHubClassNames.SkinLight),
                "probe --liveops-hub-skin-probe đổi màu nền mà class skin không đổi — hub dock không focus sẽ lẫn hai skin khi đổi Theme");
            Assert.AreEqual(!startsLight, window.Skin.IsLight, "LiveOpsHubSkin.IsLight phải theo class vừa đổi");

            wrapper.RemoveFromClassList(towardsOpposite);
            yield return WaitForClass(hubRoot, startsLight);
            Assert.AreEqual(startsLight, hubRoot.ClassListContains(LiveOpsHubClassNames.SkinLight), "gỡ ghi đè phải trả class skin về theo nền thật");
            Assert.AreEqual(changesBefore + 2, window.Skin.ChangeCount, "mỗi lần nền đổi phía đúng một lần đổi class — không bật lại vô ích");
            Assert.AreEqual(!startsLight, EditorGUIUtility.isProSkin, "test không được đổi skin thật của Editor");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SkinProbe_IsLightBackground_EditorWindowColors()
        {
            Assert.IsFalse(LiveOpsHubSkin.IsLightBackground(new Color32(0x38, 0x38, 0x38, 0xFF)), "Dark #383838");
            Assert.IsTrue(LiveOpsHubSkin.IsLightBackground(new Color32(0xC8, 0xC8, 0xC8, 0xFF)), "Light #C8C8C8");
            Assert.IsFalse(LiveOpsHubSkin.IsLightBackground(new Color32(0x28, 0x28, 0x28, 0xFF)), "nền tối tuỳ biến");
            Assert.IsTrue(LiveOpsHubSkin.IsLightBackground(new Color32(0xA5, 0xA5, 0xA5, 0xFF)), "nền sáng tuỳ biến (#A5A5A5 của rail Light)");
        }

        private static IEnumerator WaitForClass(VisualElement hubRoot, bool light)
        {
            int frames = 0;
            while (hubRoot.ClassListContains(LiveOpsHubClassNames.SkinLight) != light && frames < LiveOpsHubWindowTestScope.MaximumLayoutFrames)
            {
                frames++;
                yield return null;
            }
        }

        /// <summary>
        /// Dựng StyleSheet từ chữ lúc chạy bằng importer nội bộ của UI Toolkit (<c>StyleSheetImporterImpl.Import(StyleSheet, string)</c>,
        /// có ở cả 2022.3 và 6000.6 — đối chiếu dump API của kế hoạch). Vì sao không dùng file .uss: bảng quyền ghi của gói không có
        /// file tài nguyên test; sheet trong bộ nhớ cũng không để lại asset nào trên đĩa.
        /// </summary>
        private static StyleSheet CreateStyleSheet(string text)
        {
            Type importerType = typeof(EditorWindow).Assembly.GetType("UnityEditor.UIElements.StyleSheets.StyleSheetImporterImpl");
            Assert.IsNotNull(importerType, "không tìm thấy UnityEditor.UIElements.StyleSheets.StyleSheetImporterImpl ở bản Unity này");
            object importer = Activator.CreateInstance(importerType, true);
            MethodInfo import = importerType.GetMethod("Import", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                new[] { typeof(StyleSheet), typeof(string) }, null);
            Assert.IsNotNull(import, "StyleSheetImporterImpl.Import(StyleSheet, string) không có ở bản Unity này");
            StyleSheet sheet = ScriptableObject.CreateInstance<StyleSheet>();
            sheet.hideFlags = HideFlags.DontSave;
            import.Invoke(importer, new object[] { sheet, text });
            return sheet;
        }
    }
}
