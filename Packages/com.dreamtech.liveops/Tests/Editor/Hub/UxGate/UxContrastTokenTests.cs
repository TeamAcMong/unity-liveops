using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Một token màu của <c>liveops-hub-theme.uss</c> cùng bậc tương phản WCAG mà nó phải đạt trên nền cửa sổ của skin.
    /// </summary>
    internal readonly struct UxColorTokenRule
    {
        public UxColorTokenRule(string propertyName, float minimumRatio, string purpose)
        {
            PropertyName = propertyName;
            MinimumRatio = minimumRatio;
            Purpose = purpose;
        }

        /// <summary>Tên custom property, đúng như khai trong <c>liveops-hub-theme.uss</c>.</summary>
        public string PropertyName { get; }

        /// <summary>Bậc phải đạt: 4,5:1 cho màu CHỮ, 3:1 cho hình khối (chốt của USER 19/9 theo WCAG 2.1).</summary>
        public float MinimumRatio { get; }

        /// <summary>Token này vẽ cái gì — để câu đỏ nói được vì sao con số ấy quan trọng, không chỉ đọc ra một tên biến.</summary>
        public string Purpose { get; }
    }

    /// <summary>
    /// Cổng tương phản WCAG cho TOÀN BỘ token màu của hub, ở CẢ HAI skin.
    /// <para>
    /// Vì sao cần một bộ test riêng bên cạnh luật <c>lowContrast</c> của <see cref="UxLayoutAuditor"/>: luật kia đo màu ĐÃ
    /// RESOLVE của skin ĐANG CHẠY, mà cổng chỉ chạy được một skin mỗi lượt (đổi skin Editor là việc riêng của
    /// <c>capture.sh</c>, SP-4). Khối <c>.liveops-hub--skin-light</c> vì thế CHƯA TỪNG được đo — ba token chữ dưới chuẩn nằm
    /// nguyên trong đó mà không lượt cổng nào bắt được. Ở đây thì khác: hai root dựng sẵn (một thường, một mang class skin
    /// sáng) cho cùng một stylesheet giải ra HAI bộ giá trị trong CÙNG một lượt, nên một lượt chạy phủ cả hai theme và chạy
    /// được ở bất kỳ skin Editor nào.
    /// </para>
    /// <para>
    /// Bậc theo chốt của USER 19/9: chữ thường ≥ 4,5:1; chữ to và hình khối (ô màu chú giải, đường kẻ, viền trạng thái) ≥ 3:1.
    /// Nền để đo là nền cửa sổ Editor của từng skin — hằng ở đây, và <see cref="BackdropConstant_MatchesMeasuredBackdrop_InRunningSkin"/>
    /// đối chiếu hằng ấy với phép đo THẬT của chính <see cref="UxLayoutAuditor"/>, để bảng nền không bao giờ trôi trong im lặng.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    [Category(LiveOpsHubTestCategories.UxGate)]
    public sealed class UxContrastTokenTests
    {
        /// <summary>Bậc WCAG 2.1 AA cho chữ thường.</summary>
        private const float TextContrastRatio = 4.5f;

        /// <summary>Bậc WCAG 2.1 AA cho chữ to và cho thành phần đồ hoạ / thành phần giao diện.</summary>
        private const float ShapeContrastRatio = 3f;

        /// <summary>Nền cửa sổ Editor skin tối — cùng giá trị mà <c>UxLayoutAuditor.BackdropOf</c> đo ra ở lượt cổng.</summary>
        private const string DarkWindowBackgroundHex = "#383838";

        /// <summary>Nền cửa sổ Editor skin sáng ([FD §2.3], chú thích <c>liveops-hub-theme.uss</c>).</summary>
        private const string LightWindowBackgroundHex = "#C8C8C8";

        /// <summary>Sai số so hằng nền với số đo thật: nền vẽ qua 8 bit/kênh nên 1 nấc là làm tròn, không phải lệch bảng.</summary>
        private const float BackgroundToleranceChannels = 1.5f / 255f;

        /// <summary>Số khung chờ cho <c>CustomStyleResolvedEvent</c> chạy xong trên root mới gắn vào panel.</summary>
        private const int StyleResolveFrames = 4;

        private const string ThemeRelativePath = "Editor/Hub/UI/liveops-hub-theme.uss";
        private const string SkinLightBlockHeader = ".liveops-hub-root.liveops-hub--skin-light";
        private const string DarkSkinName = "skin tối";
        private const string LightSkinName = "skin sáng";

        /// <summary>
        /// Token màu CHỮ: phải đạt 4,5:1. <c>--liveops-hub-color-quiet</c> nằm ở đây chứ không ở nhóm hình khối vì chú thích của
        /// theme gọi nó là "vòng rỗng" nhưng thực tế nó là màu chữ phụ dùng khắp hub — kể cả tag lý do của popover Thêm đợt.
        /// </summary>
        private static readonly UxColorTokenRule[] TextTokenRules =
        {
            new UxColorTokenRule("--liveops-hub-color-blocked-text", TextContrastRatio, "câu lỗi và tag 'bị bỏ'"),
            new UxColorTokenRule("--liveops-hub-color-warning-text", TextContrastRatio, "câu cảnh báo"),
            new UxColorTokenRule("--liveops-hub-color-quiet", TextContrastRatio, "chữ phụ khắp hub, tag lý do của popover"),
            new UxColorTokenRule("--liveops-hub-color-focus", TextContrastRatio, "viền tiêu điểm bàn phím"),
            new UxColorTokenRule("--liveops-hub-color-link", TextContrastRatio, "chữ liên kết"),
            new UxColorTokenRule("--liveops-hub-color-now", TextContrastRatio, "nhãn mốc 'lúc này'"),
            new UxColorTokenRule("--liveops-hub-json-key", TextContrastRatio, "khoá JSON"),
            new UxColorTokenRule("--liveops-hub-json-string", TextContrastRatio, "chuỗi JSON"),
            new UxColorTokenRule("--liveops-hub-json-number", TextContrastRatio, "số JSON"),
            new UxColorTokenRule("--liveops-hub-json-literal", TextContrastRatio, "true/false/null của JSON"),
        };

        /// <summary>Token HÌNH KHỐI: dải màu, chấm trạng thái, ô màu chú giải — bậc 3:1.</summary>
        private static readonly UxColorTokenRule[] ShapeTokenRules =
        {
            new UxColorTokenRule("--liveops-hub-color-blocked-fill", ShapeContrastRatio, "dải màu trạng thái chặn"),
            new UxColorTokenRule("--liveops-hub-color-warning-fill", ShapeContrastRatio, "dải màu trạng thái cảnh báo"),
            new UxColorTokenRule("--liveops-hub-event-color-0", ShapeContrastRatio, "màu loại event 0"),
            new UxColorTokenRule("--liveops-hub-event-color-1", ShapeContrastRatio, "màu loại event 1"),
            new UxColorTokenRule("--liveops-hub-event-color-2", ShapeContrastRatio, "màu loại event 2"),
            new UxColorTokenRule("--liveops-hub-event-color-3", ShapeContrastRatio, "màu loại event 3"),
            new UxColorTokenRule("--liveops-hub-event-color-4", ShapeContrastRatio, "màu loại event 4"),
            new UxColorTokenRule("--liveops-hub-event-color-5", ShapeContrastRatio, "màu loại event 5"),
            new UxColorTokenRule("--liveops-hub-event-color-6", ShapeContrastRatio, "màu loại event 6"),
            new UxColorTokenRule("--liveops-hub-event-color-7", ShapeContrastRatio, "màu loại event 7"),
        };

        /// <summary>
        /// Token khai màu trần nhưng KHÔNG đo trên nền cửa sổ, kèm lý do. Đây là danh sách miễn trừ DUY NHẤT của bộ test này:
        /// thêm một dòng vào đây là một quyết định phải giải thích được, không phải cách lấy màu xanh.
        /// </summary>
        private static readonly Dictionary<string, string> TokensNotMeasuredOnWindowBackground = new Dictionary<string, string>
        {
            {
                "--liveops-hub-color-on-selection",
                "chữ trên nền HÀNG ĐANG CHỌN (--unity-colors-highlight-background), không phải trên nền cửa sổ — đo ở đây là đo sai nền"
            },
        };

        private TimelineTestPanel _panel;
        private UxHubWindowFixture _fixture;

        [TearDown]
        public void TearDown()
        {
            if (_panel != null) _panel.Dispose();
            _panel = null;
            if (_fixture != null) _fixture.Dispose();
            _fixture = null;
            UxHubWindowFixture.CloseStrayWindows();
        }

        /// <summary>
        /// Mọi token trong bảng phải đạt bậc của nó trên nền cửa sổ, ở CẢ HAI skin. Gom mọi chỗ trượt vào MỘT câu đỏ thay vì
        /// dừng ở chỗ đầu tiên: người sửa màu cần thấy trọn danh sách trong một lượt chạy, không phải bóc từng lớp hành.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryColorToken_MeetsWcagContrast_OnWindowBackground_InBothSkins()
        {
            _panel = TimelineTestPanel.Open();
            VisualElement darkRoot = _panel.CreateRoot(false);
            VisualElement lightRoot = _panel.CreateRoot(true);
            yield return TimelineTestPanel.WaitUntil(
                () => TimelineTestPanel.HasLayout(darkRoot) && TimelineTestPanel.HasLayout(lightRoot),
                "hai root token chưa có layout");
            for (int frame = 0; frame < StyleResolveFrames; frame++) yield return null;

            List<string> failures = new List<string>();
            CollectFailures(darkRoot, DarkSkinName, Hex(DarkWindowBackgroundHex), failures);
            CollectFailures(lightRoot, LightSkinName, Hex(LightWindowBackgroundHex), failures);

            Assert.IsEmpty(failures,
                "token màu không đạt bậc tương phản WCAG (chữ ≥ " + Number(TextContrastRatio) + ":1, hình khối ≥ "
                + Number(ShapeContrastRatio) + ":1 — chốt của USER 19/9):" + Environment.NewLine
                + string.Join(Environment.NewLine, failures.ToArray()));
        }

        /// <summary>
        /// Bảng token ở trên phải phủ MỌI token khai màu trần trong <c>liveops-hub-theme.uss</c>. Vì sao cần: một lượt sau thêm
        /// token màu mới (ví dụ thang màu riêng cho dấu chú giải) mà quên khai vào bảng thì token mới không được đo, và cổng
        /// tương phản im lặng tụt lại đúng chỗ vừa sửa. Token có alpha nằm ngoài phép đo này vì nền thật của chúng là nền mà
        /// chúng phủ lên, không phải nền cửa sổ — chúng đã có luật <c>lowContrast</c> đo tại chỗ, có hợp thành alpha.
        /// </summary>
        [Test]
        public void TokenTable_CoversEveryOpaqueColorTokenOfTheme()
        {
            // Bỏ chú thích TRƯỚC khi dò mốc khối skin sáng: chú thích đầu file có nhắc nguyên văn tên selector ấy, nên
            // IndexOf trên văn bản thô dừng ở câu văn xuôi và khối "skin tối" thu lại còn mỗi lời tựa (đo thật: 0 token).
            string themeText = StripComments(File.ReadAllText(Path.Combine(PackageDirectory(), ThemeRelativePath)));
            int skinLightIndex = themeText.IndexOf(SkinLightBlockHeader, StringComparison.Ordinal);
            Assert.Greater(skinLightIndex, 0,
                "không tìm thấy khối " + SkinLightBlockHeader + " trong theme — bảng token không còn nói được về skin sáng");

            HashSet<string> declaredInDark = OpaqueColorTokensIn(themeText.Substring(0, skinLightIndex));
            HashSet<string> declaredInLight = OpaqueColorTokensIn(themeText.Substring(skinLightIndex));
            Assert.IsNotEmpty(declaredInDark,
                "không đọc được token màu trần nào ở khối skin tối — phép dò hỏng thì bảng phủ sóng thành lời khai suông");
            HashSet<string> measured = new HashSet<string>();
            foreach (UxColorTokenRule rule in AllTokenRules()) measured.Add(rule.PropertyName);

            List<string> missing = new List<string>();
            foreach (string token in declaredInDark)
            {
                if (measured.Contains(token)) continue;
                if (TokensNotMeasuredOnWindowBackground.ContainsKey(token)) continue;
                missing.Add(token + " — khai màu trần trong theme mà không có trong bảng, nên KHÔNG được đo tương phản");
            }
            foreach (string token in declaredInDark)
            {
                if (declaredInLight.Contains(token)) continue;
                missing.Add(token + " — chỉ khai ở khối skin tối, khối skin sáng thiếu nên skin sáng dùng màu của skin tối");
            }
            foreach (string token in measured)
            {
                if (declaredInDark.Contains(token)) continue;
                missing.Add(token + " — bảng đo một token mà theme không còn khai, câu đỏ của nó sẽ nói về màu không tồn tại");
            }

            Assert.IsEmpty(missing, "bảng token và theme không khớp:" + Environment.NewLine
                + string.Join(Environment.NewLine, missing.ToArray()));
        }

        /// <summary>
        /// Tự kiểm hằng nền: ở skin ĐANG CHẠY, hằng của bộ test phải bằng nền mà <see cref="UxLayoutAuditor.BackdropOf"/> đo
        /// được sau lưng một dấu chú giải thật. Hai chỗ đọc hai con số khác nhau thì mọi tỉ số ở trên nói về một cửa sổ không
        /// có thật, và không ai biết. Skin còn lại không tự kiểm được ở đây (đổi skin Editor là việc của <c>capture.sh</c>) —
        /// đó là giới hạn đã khai, không phải chỗ bỏ sót.
        /// </summary>
        [UnityTest]
        public IEnumerator BackdropConstant_MatchesMeasuredBackdrop_InRunningSkin()
        {
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, UxHubWindowFixture.AllSizes[4],
                LiveOpsHubLanguageId.Vietnamese);
            yield return _fixture.WaitForLayout();

            VisualElement sample = _fixture.Root.Q(className: LiveOpsHubClassNames.TimelineLegendSample);
            Assert.IsNotNull(sample,
                "không tìm thấy dấu chú giải nào — không có chỗ nào để đo nền thật, và hằng nền của bộ test thành lời khai suông");

            bool proSkin = UnityEditor.EditorGUIUtility.isProSkin;
            string skinName = proSkin ? DarkSkinName : LightSkinName;
            Color expected = Hex(proSkin ? DarkWindowBackgroundHex : LightWindowBackgroundHex);
            Color measured = UxLayoutAuditor.BackdropOf(sample);

            Assert.AreEqual(expected.r, measured.r, BackgroundToleranceChannels, RedChannelMessage(skinName, expected, measured));
            Assert.AreEqual(expected.g, measured.g, BackgroundToleranceChannels, GreenChannelMessage(skinName, expected, measured));
            Assert.AreEqual(expected.b, measured.b, BackgroundToleranceChannels, BlueChannelMessage(skinName, expected, measured));
        }

        // ------------------------------------------------------------------------------------------------- trợ giúp

        private static void CollectFailures(VisualElement root, string skinName, Color background, List<string> failures)
        {
            foreach (UxColorTokenRule rule in AllTokenRules())
            {
                CustomStyleProperty<Color> property = new CustomStyleProperty<Color>(rule.PropertyName);
                if (!root.customStyle.TryGetValue(property, out Color declared))
                {
                    failures.Add(skinName + " · " + rule.PropertyName + " (" + rule.Purpose
                        + "): TryGetValue = false — theme không khai token này cho skin ấy, element đọc nó sẽ về màu dự phòng của C#");
                    continue;
                }

                Color seen = UxLayoutAuditor.CompositeOver(declared, background);
                float ratio = UxLayoutAuditor.ContrastRatio(seen, background);
                if (ratio >= rule.MinimumRatio) continue;
                failures.Add(skinName + " · " + rule.PropertyName + " (" + rule.Purpose + "): " + Number(ratio)
                    + ":1 — cần ≥ " + Number(rule.MinimumRatio) + ":1; màu " + HexText(declared) + " trên nền " + HexText(background));
            }
        }

        private static IEnumerable<UxColorTokenRule> AllTokenRules()
        {
            for (int index = 0; index < TextTokenRules.Length; index++) yield return TextTokenRules[index];
            for (int index = 0; index < ShapeTokenRules.Length; index++) yield return ShapeTokenRules[index];
        }

        /// <summary>Tên mọi token khai màu ĐỤC (hex) trong một khối USS; token khai <c>rgba(…)</c> hoặc <c>var(…)</c> không tính.</summary>
        private static HashSet<string> OpaqueColorTokensIn(string block)
        {
            HashSet<string> tokens = new HashSet<string>();
            foreach (Match match in Regex.Matches(StripComments(block), @"(--liveops-hub-[a-z0-9-]+)\s*:\s*#([0-9A-Fa-f]{6})\s*;"))
            {
                tokens.Add(match.Groups[1].Value);
            }
            return tokens;
        }

        /// <summary>Bỏ chú thích USS trước khi dò: chú thích của theme có chép nguyên mã màu làm ví dụ.</summary>
        private static string StripComments(string text)
        {
            return Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        }

        private static string PackageDirectory()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UxContrastTokenTests).Assembly);
            Assert.IsNotNull(package,
                "không hỏi được gốc package từ assembly test — bộ test này phải chạy như một phần của com.dreamtech.liveops");
            return Path.GetFullPath(package.resolvedPath);
        }

        private static string RedChannelMessage(string skinName, Color expected, Color measured)
        {
            return BackdropMessage(skinName, expected, measured) + " (kênh đỏ)";
        }

        private static string GreenChannelMessage(string skinName, Color expected, Color measured)
        {
            return BackdropMessage(skinName, expected, measured) + " (kênh lục)";
        }

        private static string BlueChannelMessage(string skinName, Color expected, Color measured)
        {
            return BackdropMessage(skinName, expected, measured) + " (kênh lam)";
        }

        private static string BackdropMessage(string skinName, Color expected, Color measured)
        {
            return "hằng nền " + skinName + " của bộ test là " + HexText(expected) + " nhưng nền đo thật sau lưng dấu chú giải là "
                + HexText(measured) + " — mọi tỉ số ở bộ test này đang nói về một cửa sổ khác cửa sổ người dùng thấy";
        }

        private static Color Hex(string value)
        {
            string digits = value.TrimStart('#');
            return new Color(
                int.Parse(digits.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f,
                int.Parse(digits.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f,
                int.Parse(digits.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f,
                1f);
        }

        private static string HexText(Color color)
        {
            StringBuilder builder = new StringBuilder("#");
            builder.Append(Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f).ToString("X2", CultureInfo.InvariantCulture));
            builder.Append(Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f).ToString("X2", CultureInfo.InvariantCulture));
            builder.Append(Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f).ToString("X2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static string Number(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
