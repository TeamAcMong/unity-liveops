using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// (V-21 D-3) Nửa JSON view + cycle bar của <c>SelfDrawnElements_ResolveTokens_BothSkinClasses</c> (nửa lane + minimap ở
    /// <c>TimelineSkinTokenTests</c> của G-TIMELINE-VIEW). Custom property không kế thừa từ root ([API §12.2]): element tự vẽ chỉ đọc
    /// được token khi USS khai lại trên chính selector của nó bằng <c>var()</c>. Quên khai lại thì <c>TryGetValue</c> trả false, màu
    /// cú pháp/vạch mốc biến mất âm thầm — không log, chỉ thấy trên ảnh. Kiểm ở root thường và root <c>liveops-hub--skin-light</c>
    /// (đổi class, không đổi skin thật của Editor — SP-4), và giá trị phải theo đúng skin của root.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class ControlsSkinTokenTests
    {
        // Giá trị của liveops-hub-theme.uss ([FD §2.3, §2.6]) — theme đổi thì test này đổi cùng commit.
        private const string DarkJsonKey = "#8FB8E8";
        private const string LightJsonKey = "#1F4E8C";
        private const string DarkJsonString = "#B5CEA8";
        // (G-UX3-TIMELINE) Hai hằng Light đổi theo token: #3F6B28 chỉ 3,75:1 và #1E6B6B chỉ 3,72:1 trên nền cửa sổ #C8C8C8,
        // dưới bậc chữ thường 4,5:1 của WCAG. Bản mới đo 5,13:1 và 5,23:1. Đây là bản SAO của liveops-hub-theme.uss —
        // sửa token mà quên hai dòng này là test đỏ, đúng ý của nó.
        private const string LightJsonString = "#31551E";
        private const string DarkJsonNumber = "#C8A2E0";
        private const string LightJsonNumber = "#6A3D9A";
        private const string DarkJsonLiteral = "#7FC4C4";
        private const string LightJsonLiteral = "#175353";
        private const string DarkNow = "#E6E6E6";
        private const string LightNow = "#1A1A1A";
        private const float ColorTolerance = 1.5f / 255f;

        private ControlsTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            _panel?.Dispose();
            _panel = null;
        }

        [UnityTest]
        public IEnumerator SelfDrawnElements_ResolveTokens_BothSkinClasses()
        {
            _panel = ControlsTestPanel.Open();
            VisualElement darkRoot = _panel.CreateRoot(false);
            VisualElement lightRoot = _panel.CreateRoot(true);
            LiveOpsJsonView darkJson = AddJsonView(darkRoot);
            LiveOpsJsonView lightJson = AddJsonView(lightRoot);
            LiveOpsRuleCycleBar darkBar = AddCycleBar(darkRoot);
            LiveOpsRuleCycleBar lightBar = AddCycleBar(lightRoot);
            yield return ControlsTestPanel.WaitForLayout(darkJson, lightJson, darkBar, lightBar);

            AssertJsonTokens(darkJson, "root thường", DarkJsonKey, DarkJsonString, DarkJsonNumber, DarkJsonLiteral);
            AssertJsonTokens(lightJson, "root liveops-hub--skin-light", LightJsonKey, LightJsonString, LightJsonNumber, LightJsonLiteral);
            AssertCycleBarToken(darkBar, "root thường", DarkNow);
            AssertCycleBarToken(lightBar, "root liveops-hub--skin-light", LightNow);

            // Đổi class skin lúc đang mở (probe skin của hub bật/tắt class): token và màu đã đọc phải theo ngay.
            lightRoot.RemoveFromClassList(LiveOpsHubClassNames.SkinLight);
            yield return null;
            yield return null;
            AssertJsonTokens(lightJson, "root vừa tắt liveops-hub--skin-light", DarkJsonKey, DarkJsonString, DarkJsonNumber, DarkJsonLiteral);
            AssertCycleBarToken(lightBar, "root vừa tắt liveops-hub--skin-light", DarkNow);
            LogAssert.NoUnexpectedReceived();
        }

        private static LiveOpsJsonView AddJsonView(VisualElement root)
        {
            LiveOpsJsonView view = new LiveOpsJsonView();
            view.style.height = 80f;
            view.style.flexGrow = 0f;
            view.SetText("{\n  \"version\": 2\n}", "{\"version\":2}");
            root.Add(view);
            return view;
        }

        private static LiveOpsRuleCycleBar AddCycleBar(VisualElement root)
        {
            LiveOpsRuleCycleBar bar = new LiveOpsRuleCycleBar { PeriodHours = 24, ActiveHours = 30 };
            bar.style.width = 200f;
            root.Add(bar);
            return bar;
        }

        private static void AssertJsonTokens(LiveOpsJsonView view, string context, string key, string stringColor, string number, string literal)
        {
            AssertToken(view.customStyle, LiveOpsJsonView.KeyColorProperty, key, context + " · JSON key");
            AssertToken(view.customStyle, LiveOpsJsonView.StringColorProperty, stringColor, context + " · JSON string");
            AssertToken(view.customStyle, LiveOpsJsonView.NumberColorProperty, number, context + " · JSON number");
            AssertToken(view.customStyle, LiveOpsJsonView.LiteralColorProperty, literal, context + " · JSON literal");
            Assert.IsTrue(view.Colors.IsResolved, context + ": JSON view chưa nhận CustomStyleResolvedEvent");
            Assert.AreEqual(key.Substring(1) + "FF", view.Colors.KeyHex, context + ": màu thẻ rich text phải lấy từ token vừa đọc");
            StringAssert.Contains("<color=#" + view.Colors.KeyHex + ">", view.RichTextOfLine(2), context + ": dòng đã dựng phải dùng màu mới");
        }

        private static void AssertCycleBarToken(LiveOpsRuleCycleBar bar, string context, string expected)
        {
            AssertToken(bar.customStyle, LiveOpsRuleCycleBar.PeriodEndColorProperty, expected, context + " · vạch mốc hết chu kỳ");
            AssertColor(expected, bar.PeriodEndColor, context + ": màu vạch đã lưu trong thanh");
        }

        private static void AssertToken(ICustomStyle style, CustomStyleProperty<Color> property, string expected, string context)
        {
            Assert.IsTrue(style.TryGetValue(property, out Color actual),
                context + ": TryGetValue(" + property.name + ") = false — USS phải khai lại token trên selector của chính element");
            AssertColor(expected, actual, context);
        }

        private static void AssertColor(string expectedHex, Color actual, string context)
        {
            Assert.IsTrue(ColorUtility.TryParseHtmlString(expectedHex, out Color expected), "hằng màu hỏng: " + expectedHex);
            Assert.AreEqual(expected.r, actual.r, ColorTolerance, context + " (r) — token sai skin?");
            Assert.AreEqual(expected.g, actual.g, ColorTolerance, context + " (g)");
            Assert.AreEqual(expected.b, actual.b, ColorTolerance, context + " (b)");
        }
    }
}
