using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class EditorFontTests
    {
        private const int MaximumLayoutFrames = 60;

        private FontProbeWindow _window;

        [TearDown]
        public void TearDown()
        {
            if (_window != null) _window.Close();
            _window = null;
        }

        [UnityTest]
        public IEnumerator EditorFont_HasHubGlyphs()
        {
            // Font Label mặc định chỉ lấy được sau layout trong panel thật: Inter-Regular SDF.asset không Load được theo
            // đường dẫn ở cả hai bản ([API §12.3], S-6).
            _window = ScriptableObject.CreateInstance<FontProbeWindow>();
            _window.Show();
            Label label = new Label(LiveOpsHubStrings.HubGlyphs);
            _window.rootVisualElement.Add(label);

            int frames = 0;
            while (float.IsNaN(label.layout.width) || label.layout.width <= 0f)
            {
                if (++frames > MaximumLayoutFrames) Assert.Fail("Label không có layout sau " + MaximumLayoutFrames + " khung — test UI phải chạy không -nographics");
                yield return null;
            }

            FontAsset fontAsset = label.resolvedStyle.unityFontDefinition.fontAsset;
            Assert.IsNotNull(fontAsset, "Label mặc định của Editor phải dùng FontAsset (Inter-Regular SDF), không phải Font .ttf");
            // Overload (string, out List<char>) không tìm fallback và báo thiếu cả "…·–—×" — phải dùng overload có hai cờ.
            bool hasEverything = fontAsset.HasCharacters(LiveOpsHubStrings.HubGlyphs, out uint[] missing, true, true);
            Assert.IsTrue(hasEverything, "font " + fontAsset.name + " thiếu ký hiệu viết vào Label: " + DescribeMissing(missing)
                + " — ký hiệu thiếu sẽ thành ô trống; phải vẽ bằng LiveOpsChevron/icon hoặc đổi nhãn sang chữ");
            Assert.IsTrue(LiveOpsHubKeyLabels.EditorFontHas("⌘⇧⌥⌫"), "helper nhãn phím phải thấy cùng bộ glyph với font Label");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MonoFont_IsFontAsset()
        {
            Object monoFont = EditorGUIUtility.Load(LiveOpsHubPaths.MonoFontResource);
            Assert.IsTrue(monoFont is FontAsset,
                "-unity-font-definition của .liveops-hub-mono trỏ tới " + LiveOpsHubPaths.MonoFontResource + " phải là FontAsset — sai thì chữ mono về font thường mà không báo lỗi");
            LogAssert.NoUnexpectedReceived();
        }

        private static string DescribeMissing(uint[] missing)
        {
            if (missing == null || missing.Length == 0) return "(không rõ)";
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (uint codePoint in missing)
            {
                builder.Append(char.ConvertFromUtf32((int)codePoint)).Append(" U+").Append(codePoint.ToString("X4", System.Globalization.CultureInfo.InvariantCulture)).Append(' ');
            }
            return builder.ToString();
        }

        // Cửa sổ trống chỉ để có panel thật cho layout; lồng trong fixture vì thư mục test Foundation không có file hỗ trợ
        // riêng (LiveOpsHubWindowTestScope là của G-SHELL, W2).
        private sealed class FontProbeWindow : EditorWindow
        {
        }
    }
}
