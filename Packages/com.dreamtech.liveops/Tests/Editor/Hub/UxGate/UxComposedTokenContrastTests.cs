using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Nền đục mà một chỗ chữ nằm TRÊN. Ba nền này là ba biến nền của Unity mà hub thật sự đặt chữ lên; mỗi cái có giá trị
    /// riêng cho từng skin, và <see cref="UxComposedTokenContrastTests.ComposedBackdropConstants_MatchMeasuredBackdrops_InRunningSkin"/>
    /// đối chiếu hằng với số đo thật của skin đang chạy nên chúng không phải lời khai tay.
    /// </summary>
    internal enum UxComposedBackdrop
    {
        /// <summary>Nền cửa sổ hub — <c>--unity-colors-window-background</c>.</summary>
        WindowBackground,

        /// <summary>
        /// Nền header của thẻ và của mọi dải công cụ — <c>--unity-colors-toolbar-background</c>. Ở skin tối nền này SÁNG HƠN
        /// nền cửa sổ (#3C3C3C so với #383838), nên chữ phụ đạt chuẩn trên nền cửa sổ vẫn có thể trượt ở đây.
        /// </summary>
        CardBackground,

        /// <summary>Nền của chip header — <c>--unity-colors-default-background</c>.</summary>
        ChipBackground,
    }

    /// <summary>
    /// Một CHỖ DÙNG có thật của một token màu (chữ, hoặc nét vẽ từ W10-09): token nào, nằm trong khối có opacity bao nhiêu,
    /// trên nền nào, phải đạt bậc nào. Khác bảng token của <see cref="UxContrastTokenTests"/> ở đúng một điểm và đó là điểm sinh ra phiếu W10-03/W10-06:
    /// bảng kia đo màu TRƯỚC khi nhân opacity và luôn đo trên nền CỬA SỔ, nên một token xanh ở bảng kia vẫn có thể là chữ
    /// không đọc nổi ở màn thật.
    /// </summary>
    internal readonly struct UxComposedSite
    {
        public UxComposedSite(string tokenName, float ancestorOpacity, UxComposedBackdrop backdrop, float minimumRatio, string place)
        {
            TokenName = tokenName;
            AncestorOpacity = ancestorOpacity;
            Backdrop = backdrop;
            MinimumRatio = minimumRatio;
            Place = place;
        }

        /// <summary>Tên custom property, đúng như khai trong <c>liveops-hub-theme.uss</c>.</summary>
        public string TokenName { get; }

        /// <summary>Tích opacity của mọi khối cha bọc chỗ chữ ấy — 1 khi không khối nào làm mờ.</summary>
        public float AncestorOpacity { get; }

        /// <summary>Nền đục gần nhất phía sau chỗ chữ.</summary>
        public UxComposedBackdrop Backdrop { get; }

        /// <summary>Bậc phải đạt theo chốt của USER 19/9 (WCAG 2.1): 4,5:1 cho chữ thường, 3:1 cho hình khối.</summary>
        public float MinimumRatio { get; }

        /// <summary>Chỗ ấy ở đâu trong hub — để câu đỏ chỉ được tay vào màn phải mở ra xem.</summary>
        public string Place { get; }
    }

    /// <summary>
    /// Cổng tương phản cho màu ĐÃ HỢP THÀNH (token × opacity của tổ tiên × nền đục), chạy cho CẢ HAI theme trong MỘT lượt.
    /// <para>
    /// Vì sao cần bộ test này bên cạnh hai bộ đã có:
    /// <list type="bullet">
    /// <item><see cref="UxContrastTokenTests.EveryColorToken_MeetsWcagContrast_OnItsOwnBackdrop_InBothSkins"/> phủ cả hai skin
    /// nhưng đo màu TRƯỚC opacity và chỉ trên nền CỬA SỔ.</item>
    /// <item><see cref="UxContrastTokenTests.ComposedTextColor_MeetsWcagContrast_InCalendarInspectorPanes"/> đo đúng màu đã hợp
    /// thành trên cây thật, nhưng cây thật chỉ giải ra được skin ĐANG CHẠY của Editor (đổi <c>EditorPrefs UserSkin</c> là việc
    /// riêng của <c>capture.sh</c>, SP-4), nên nó chưa bao giờ nói được về theme còn lại.</item>
    /// </list>
    /// Ở giữa hai cái đó có một khe, và đúng 45 dòng trượt WCAG đã rơi vào khe ấy (37 dòng skin tối của phiếu W10-06 + 8 dòng
    /// skin sáng của phiếu W10-03): token xanh, cây thật ở skin tối xanh, mà chữ người dùng nhìn thấy thì 3,34-4,48:1.
    /// </para>
    /// <para>
    /// Cách bộ test này bịt khe: GIÁ TRỊ TOKEN lấy từ hai root dựng sẵn (một thường, một mang class skin sáng) nên hai theme
    /// cùng đọc được trong một lượt; NỀN lấy từ bảng hằng theo skin, và bảng hằng ấy bị
    /// <see cref="ComposedBackdropConstants_MatchMeasuredBackdrops_InRunningSkin"/> đối chiếu với số đo thật mỗi lượt chạy.
    /// Ca ấy khoá ĐÚNG cột của skin đang chạy, và mọi lượt cổng tới nay đều ở skin tối, nên cột skin sáng vẫn CHƯA có lượt
    /// tự kiểm nào (F6 của soát 21/9) — cột sáng hiện chỉ có số đo pixel đếm tay trên ảnh chụp đỡ. Không lượt nào ghi
    /// <c>EditorPrefs</c>.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    [Category(LiveOpsHubTestCategories.UxGate)]
    public sealed class UxComposedTokenContrastTests
    {
        /// <summary>
        /// Bậc WCAG 2.1 AA cho chữ thường — đọc hằng dùng chung, KHÔNG khai lại literal 4,5 (nợ F5 của soát 21/9, trả ở
        /// cổng đợt vét W10 khi <c>G-W10-MATRIX</c> đã vào cây). Ba chỗ của bộ cổng (bảng token, bảng chỗ dùng hợp thành,
        /// phép đo trên cây thật) nay cùng đọc một nguồn, nên không thể có hai con số về cùng một bậc.
        /// </summary>
        private const float TextContrastRatio = UxComposedContrast.TextContrastRatio;

        /// <summary>
        /// Bậc WCAG 2.1 AA cho hình khối — nửa còn lại của chốt USER 19/9, từ phiếu W10-09 đã có chỗ dùng ở
        /// <see cref="Sites"/> (viền swatch "loại chưa khai báo" nằm trong ô mờ 0,70).
        /// </summary>
        private const float ShapeContrastRatio = UxComposedContrast.ShapeContrastRatio;


        /// <summary>Số khung chờ cho lượt style của root mới gắn vào panel.</summary>
        private const int StyleResolveFrames = 4;

        private const string DarkSkinName = "skin tối";
        private const string LightSkinName = "skin sáng";
        private const string ThemeRelativePath = "Editor/Hub/UI/liveops-hub-theme.uss";

        /// <summary>Thư mục gốc của mọi sheet hub — ca quét màu chữ duyệt đệ quy từ đây.</summary>
        private const string HubRelativeDirectory = "Editor/Hub";

        /// <summary>Khai màu chữ bằng biến Unity, đã bỏ hết khoảng trắng — xem <see cref="IsTextColorFromUnityVariable"/>.</summary>
        private const string UnityDefaultTextColorDeclaration = "color:var(--unity-colors-default-text)";

        /// <summary>
        /// Nền header thẻ (<c>--unity-colors-toolbar-background</c>). Hai giá trị dưới đây đọc từ ẢNH CHỤP THẬT của cửa sổ hub
        /// ở hai skin (nhãn chụp <c>w10theme-truoc</c>, màn <c>h04-overview-default</c>), rồi được
        /// <see cref="ComposedBackdropConstants_MatchMeasuredBackdrops_InRunningSkin"/> đối chiếu lại với
        /// <c>resolvedStyle.backgroundColor</c> của một header thẻ thật mỗi lượt chạy.
        /// </summary>
        private const string DarkCardBackgroundHex = "#3C3C3C";

        private const string LightCardBackgroundHex = "#CBCBCB";

        /// <summary>Nền chip header (<c>--unity-colors-default-background</c>) — cùng đường xác minh với nền header thẻ.</summary>
        private const string DarkChipBackgroundHex = "#282828";

        private const string LightChipBackgroundHex = "#A5A5A5";

        /// <summary>Sai số so hằng nền với số đo thật: nền vẽ qua 8 bit/kênh nên 1 nấc là làm tròn, không phải lệch bảng.</summary>
        private const float BackgroundToleranceChannels = 1.5f / 255f;

        /// <summary>
        /// Mọi chỗ dùng đã biết của màu chữ hub, gom từ phép đo 910 đoạn chữ × 8 màn × 2 skin của đợt W10 (bằng chứng
        /// <c>~/.cache/unity-liveops/ux-contrast/&lt;bản Unity&gt;/composed-{dark,light}.json</c>). Bốn cặp đầu là bốn gốc của
        /// 37 dòng trượt skin tối; cặp cuối là gốc của 8 dòng trượt skin sáng.
        /// <para>
        /// Vì sao khai thành bảng thay vì duyệt cây thật: cây thật chỉ giải ra được MỘT skin mỗi lượt. Bảng này là chỗ DUY
        /// NHẤT trong bộ test được phép nói "chỗ này có opacity bao nhiêu" mà không đo — nên mỗi dòng phải đọc được ra một
        /// màn có thật, và dòng nào hết chỗ dùng thì xoá đi chứ không để lại cho đẹp bảng.
        /// </para>
        /// <para>
        /// BẢNG NÀY PHỦ ĐẾN ĐÂU, nói thẳng để không ai đọc nó rộng hơn thứ nó chứng minh (F2 + F3 của soát 21/9, cập
        /// nhật ở cổng đợt vét W10):
        /// <list type="bullet">
        /// <item>phủ những opacity xuất hiện trong CHÍN cảnh đã đo — 1 · 0,70 · 0,82 · 0,85;</item>
        /// <item>KHÔNG còn dòng nào là TÍCH của nhiều khối cha, vì tích ấy đã bị gỡ ở nguồn (phiếu <b>W10-07</b>): hàng
        /// "kết quả cũ" của màn Kiểm lịch không còn mang opacity của riêng nó, chỉ headline mờ 0,82 và headline CHƯA ĐO
        /// (màu quiet) giữ nguyên cường độ. Ba dòng W10-07 dưới đây đo đúng ba chỗ ấy. Nếu ai đặt lại opacity lên HÀNG
        /// thì cảnh "màn Kiểm lịch, kết quả cũ" của <c>capture.sh --contrast</c> đỏ — bảng này là lời khai, cảnh kia là
        /// phép đo, và cổng giữ cả hai;</item>
        /// <item>bậc "chữ to và hình khối 3:1" nay CÓ một dòng: viền swatch
        /// <c>.liveops-hub-event-types-swatch--undeclared</c> nằm trong cell mờ 0,70 của màn Loại event. Trước bản vá
        /// W10-09 viền ấy lấy màu quiet và skin SÁNG chỉ đạt 2,82:1; token riêng đo ra 3,16:1. Vẫn chưa phải phép quét
        /// toàn bộ nét vẽ của hub — mới là một chỗ trượt đã biết, đo được, và đã vá.</item>
        /// </list>
        /// </para>
        /// </summary>
        private static readonly UxComposedSite[] Sites =
        {
            new UxComposedSite("--liveops-hub-color-text", 0.70f, UxComposedBackdrop.CardBackground, TextContrastRatio,
                "caption HOA và dòng meta trong thẻ (ĐANG CHẠY · CẦN XỬ LÝ · ĐÃ ĐĂNG · KÍCH THƯỚC · SHA…) — 28 dòng của W10-06"),
            new UxComposedSite("--liveops-hub-color-text", 0.82f, UxComposedBackdrop.CardBackground, TextContrastRatio,
                "dòng chân thẻ số đo (.liveops-hub-metric-foot) khi thẻ nằm trên dải header"),
            new UxComposedSite("--liveops-hub-color-text", 0.70f, UxComposedBackdrop.WindowBackground, TextContrastRatio,
                "chữ phụ đặt thẳng trên nền cửa sổ (chú thích, placeholder ô nhập)"),
            new UxComposedSite("--liveops-hub-color-text", 0.70f, UxComposedBackdrop.ChipBackground, TextContrastRatio,
                "khoá phím trong chip header (.liveops-hub-chip-key)"),
            new UxComposedSite("--liveops-hub-color-text", 1f, UxComposedBackdrop.CardBackground, TextContrastRatio,
                "nhãn trạng thái Ok (." + LiveOpsHubClassNames.TextOk + ") trong thẻ — inspector Lịch, thẻ hiệu Xuất JSON, "
                + "cổng Xuất, thẻ Tổng quan; trước lượt sửa 2 luật này còn đọc thẳng --unity-colors-default-text"),
            new UxComposedSite("--liveops-hub-color-quiet", 1f, UxComposedBackdrop.CardBackground, TextContrastRatio,
                "số đếm của thẻ Tổng quan và nhãn 'Chưa kiểm' của màn Kiểm lịch — 4 dòng của W10-06"),
            new UxComposedSite("--liveops-hub-color-blocked-text", 0.85f, UxComposedBackdrop.WindowBackground, TextContrastRatio,
                "câu lỗi trong khối mờ 0,85 ('2 bị bỏ', 'dừng ở đây', 'bị chặn') — 3 dòng của W10-06"),
            new UxComposedSite("--liveops-hub-color-blocked-text", 0.82f, UxComposedBackdrop.WindowBackground, TextContrastRatio,
                "dòng lỗi màn Xuất JSON và màn Kiểm lịch trong khối mờ 0,82 — 2 dòng của W10-06"),
            new UxComposedSite("--liveops-hub-color-blocked-text", 0.82f, UxComposedBackdrop.CardBackground, TextContrastRatio,
                "cùng dòng lỗi ấy khi nó nằm trong một thẻ thay vì thẳng trên nền cửa sổ"),
            new UxComposedSite("--liveops-hub-color-link", 1f, UxComposedBackdrop.ChipBackground, TextContrastRatio,
                "chữ bấm được của chip asset ở header (tên calendar đang mở) — 8 dòng của W10-03"),
            // (W10-07) Hàng "kết quả cũ" của màn Kiểm lịch sau bản vá: hàng KHÔNG còn opacity riêng, nên dòng meta và dòng
            // id luật chỉ còn 0,70 của chính chúng (trước là tích 0,574 = 3,83:1 tối / 4,10:1 sáng), còn headline mờ 0,82.
            new UxComposedSite("--liveops-hub-color-text", 0.70f, UxComposedBackdrop.WindowBackground, TextContrastRatio,
                "dòng meta và dòng id luật của hàng 'kết quả cũ' màn Kiểm lịch — phiếu W10-07"),
            new UxComposedSite("--liveops-hub-color-text", 0.82f, UxComposedBackdrop.WindowBackground, TextContrastRatio,
                "headline của hàng 'kết quả cũ' màn Kiểm lịch — phiếu W10-07"),
            // (W10-07) Headline CHƯA ĐO vẽ bằng quiet. Bản vá giữ nó ở cường độ đầy vì quiet chỉ vừa đủ bậc chữ ở opacity 1;
            // dòng này khoá đúng điều đó lại — hạ opacity của nó xuống là ca đỏ ngay, ở cả hai skin.
            new UxComposedSite("--liveops-hub-color-quiet", 1f, UxComposedBackdrop.WindowBackground, TextContrastRatio,
                "headline CHƯA ĐO của hàng 'kết quả cũ' màn Kiểm lịch — phiếu W10-07"),
            // (W10-09) Dòng HÌNH KHỐI đầu tiên của bảng: viền swatch "loại chưa khai báo" nằm trong ô mang opacity 0,70.
            // Trước bản vá viền ấy lấy màu quiet và skin sáng chỉ đạt 2,82:1; token riêng #444444 đo ra 3,16:1.
            new UxComposedSite("--liveops-hub-color-undeclared-border", 0.70f, UxComposedBackdrop.WindowBackground,
                ShapeContrastRatio, "viền swatch 'loại chưa khai báo' màn Loại event — phiếu W10-09"),
        };

        private TimelineTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            if (_panel != null) _panel.Dispose();
            _panel = null;
        }

        /// <summary>
        /// Mọi chỗ dùng trong bảng phải đạt bậc của nó SAU khi nhân opacity và hợp thành lên nền, ở CẢ HAI skin. Gom mọi chỗ
        /// trượt vào một câu đỏ: người sửa màu cần thấy trọn danh sách trong một lượt, không phải bóc từng lớp hành.
        /// </summary>
        [UnityTest]
        public IEnumerator ComposedTokenColor_MeetsWcagContrast_AtEverySite_InBothSkins()
        {
            _panel = TimelineTestPanel.Open();
            VisualElement darkRoot = _panel.CreateRoot(false);
            VisualElement lightRoot = _panel.CreateRoot(true);
            yield return TimelineTestPanel.WaitUntil(
                () => TimelineTestPanel.HasLayout(darkRoot) && TimelineTestPanel.HasLayout(lightRoot),
                "hai root token chưa có layout");
            for (int frame = 0; frame < StyleResolveFrames; frame++) yield return null;

            List<string> failures = new List<string>();
            CollectComposedFailures(darkRoot, DarkSkinName, true, failures);
            CollectComposedFailures(lightRoot, LightSkinName, false, failures);

            Assert.IsEmpty(failures,
                "màu ĐÃ HỢP THÀNH của một chỗ dùng có thật không đạt bậc chữ thường "
                + UxLayoutAuditor.Number(TextContrastRatio) + ":1 (chốt của USER 19/9):" + Environment.NewLine
                + string.Join(Environment.NewLine, failures.ToArray()));
        }

        /// <summary>
        /// Hằng nền của bảng phải khớp số đo thật ở skin ĐANG CHẠY. Không có ca này thì hai cột hằng nền là lời khai tay, và
        /// một lượt đổi biến nền của Unity (hoặc một lượt đổi class nền của header thẻ) sẽ làm mọi tỉ số ở trên nói về một
        /// cửa sổ khác cửa sổ người dùng thấy mà không ai biết.
        /// <para>
        /// GIỚI HẠN, nói đúng mức (F6 của soát 21/9): ca này khoá ĐÚNG MỘT cột — cột của skin đang chạy. Mọi lượt cổng
        /// tới nay đều chạy ở skin tối, nên <b>cột skin sáng CHƯA có lượt tự kiểm nào</b>. Đừng đọc câu này thành
        /// "capture.sh khoá nốt cột sáng": <c>capture.sh</c> chỉ CHỤP ẢNH, nó không chạy EditMode, nên lượt ấy không tồn
        /// tại. Thứ duy nhất đang đỡ cột sáng là số đo pixel đếm tay trên ảnh <c>h04-overview-default-light.png</c>
        /// (#C8C8C8 · #CBCBCB · #A5A5A5, ghi trong báo cáo soát) — bằng chứng thật nhưng KHÔNG phải cổng tự chạy. Muốn
        /// khoá nốt cột sáng thì phải có một lượt EditMode chạy dưới skin sáng, và việc đổi <c>EditorPrefs UserSkin</c>
        /// là độc quyền của <c>capture.sh</c> (SP-4) nên đó là việc của một gói có quyền ghi harness.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator ComposedBackdropConstants_MatchMeasuredBackdrops_InRunningSkin()
        {
            bool proSkin = EditorGUIUtility.isProSkin;
            string skinName = proSkin ? DarkSkinName : LightSkinName;

            _panel = TimelineTestPanel.Open();
            VisualElement root = _panel.CreateRoot(!proSkin);
            StyleSheet shellSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(LiveOpsHubPaths.ShellUss);
            Assert.IsNotNull(shellSheet, "thiếu " + LiveOpsHubPaths.ShellUss + " — không dựng được chip header để đo nền");
            root.styleSheets.Add(shellSheet);

            VisualElement cardHeaderProbe = new VisualElement();
            cardHeaderProbe.AddToClassList(LiveOpsHubClassNames.CardHeader);
            root.Add(cardHeaderProbe);

            VisualElement chipProbe = new VisualElement();
            chipProbe.AddToClassList(LiveOpsHubClassNames.Chip);
            root.Add(chipProbe);

            yield return TimelineTestPanel.WaitUntil(() => TimelineTestPanel.HasLayout(root), "root đo nền chưa có layout");
            for (int frame = 0; frame < StyleResolveFrames; frame++) yield return null;

            AssertSameColor(CardBackground(proSkin), cardHeaderProbe.resolvedStyle.backgroundColor, skinName,
                "nền đo thật của một header thẻ (." + LiveOpsHubClassNames.CardHeader + ")");
            AssertSameColor(ChipBackground(proSkin), chipProbe.resolvedStyle.backgroundColor, skinName,
                "nền đo thật của một chip header (." + LiveOpsHubClassNames.Chip + ")");
            AssertSameColor(UxLayoutAuditor.WindowBackground(proSkin), root.resolvedStyle.backgroundColor, skinName,
                "nền đo thật của root hub");
        }

        /// <summary>
        /// Mọi token nhắc trong bảng chỗ dùng phải còn tồn tại trong CẢ HAI khối skin của theme. Vì sao: một lượt đổi tên
        /// token mà quên bảng này thì <c>TryGetValue</c> trả false, và nếu ca ở trên chỉ "bỏ qua khi không đọc được" thì cổng
        /// im lặng thành vòng lặp rỗng — đúng kiểu xanh giả mà đợt này cấm.
        /// </summary>
        [Test]
        public void EverySiteToken_IsDeclaredInBothSkinBlocksOfTheme()
        {
            // Bỏ chú thích TRƯỚC khi dò mốc khối skin sáng: lời tựa đầu file có nhắc NGUYÊN VĂN tên selector ấy, nên dò trên
            // văn bản thô sẽ dừng ở câu văn xuôi và "khối skin tối" thu lại còn mỗi lời tựa — ca xanh giả đúng kiểu đợt này cấm
            // (chính bẫy ấy đã bắt ca này đỏ ở lượt chạy đầu).
            string themeText = StripBlockComments(File.ReadAllText(Path.Combine(PackageDirectory(), ThemeRelativePath)));
            int skinLightIndex = themeText.IndexOf(".liveops-hub-root.liveops-hub--skin-light", StringComparison.Ordinal);
            Assert.Greater(skinLightIndex, 0, "không tìm thấy khối skin sáng trong theme");
            string darkBlock = themeText.Substring(0, skinLightIndex);
            string lightBlock = themeText.Substring(skinLightIndex);

            List<string> missing = new List<string>();
            HashSet<string> checkedTokens = new HashSet<string>();
            for (int index = 0; index < Sites.Length; index++)
            {
                string token = Sites[index].TokenName;
                if (!checkedTokens.Add(token)) continue;
                if (darkBlock.IndexOf(token + ":", StringComparison.Ordinal) < 0) missing.Add(token + " (khối skin tối)");
                if (lightBlock.IndexOf(token + ":", StringComparison.Ordinal) < 0) missing.Add(token + " (khối skin sáng)");
            }

            Assert.IsEmpty(missing,
                "bảng chỗ dùng nhắc token mà theme không khai: " + string.Join(", ", missing.ToArray()));
        }

        /// <summary>
        /// Không luật USS nào của hub được đặt màu CHỮ bằng <c>var(--unity-colors-default-text)</c>; màu chữ phải đi qua
        /// token <c>--liveops-hub-color-text</c>.
        /// <para>
        /// Vì sao cần ca này chứ không chỉ sửa một dòng: đúng một luật như thế đã lọt qua cả bảng token lẫn bảng
        /// <see cref="Sites"/> của chính gói W10-06 (<c>.liveops-hub-text--ok</c> trong
        /// <c>liveops-hub-components.uss</c>, F1 của soát 21/9). Biến của Unity có HAI cái sai cùng lúc: nó giải theo
        /// skin ĐANG CHẠY của Editor chứ không theo class <c>.liveops-hub--skin-light</c> (nên mọi phép đo skin sáng đọc
        /// nhầm màu chữ skin tối), và giá trị skin tối của nó là <c>#D2D2D2</c> — đúng màu mà phiếu W10-06 kết tội.
        /// Một dòng sửa thì hết một dòng; ca này thì bịt cả lối.
        /// </para>
        /// <para>
        /// Phạm vi, nói rõ để không ai đọc rộng hơn: ca soi ĐÚNG thuộc tính <c>color</c> (thuộc tính duy nhất sinh ra màu
        /// chữ). Các chỗ dùng biến ấy làm <c>background-color</c> / <c>border-color</c> là nét vẽ, không phải chữ, nên
        /// không thuộc phạm vi ca này.
        /// </para>
        /// </summary>
        [Test]
        public void NoHubStyleSheet_PaintsTextWithUnityDefaultTextVariable()
        {
            string hubDirectory = Path.Combine(PackageDirectory(), HubRelativeDirectory);
            Assert.IsTrue(Directory.Exists(hubDirectory), "không thấy thư mục " + HubRelativeDirectory + " trong package");
            string[] styleSheetPaths = Directory.GetFiles(hubDirectory, "*.uss", SearchOption.AllDirectories);
            Assert.IsNotEmpty(styleSheetPaths, "không tìm thấy file .uss nào dưới " + HubRelativeDirectory
                + " — ca này sẽ xanh giả nếu để vòng lặp rỗng đi qua");

            List<string> offenders = new List<string>();
            for (int fileIndex = 0; fileIndex < styleSheetPaths.Length; fileIndex++)
            {
                string[] lines = File.ReadAllLines(styleSheetPaths[fileIndex]);
                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    if (!IsTextColorFromUnityVariable(lines[lineIndex])) continue;
                    offenders.Add(FileNameOf(styleSheetPaths[fileIndex]) + ":" + (lineIndex + 1) + " · "
                        + lines[lineIndex].Trim());
                }
            }

            Assert.IsEmpty(offenders,
                "luật màu CHỮ đọc thẳng biến của Unity thay vì token --liveops-hub-color-text — biến ấy theo skin ĐANG "
                + "CHẠY của Editor chứ không theo class skin, và ở skin tối nó là #D2D2D2 (× opacity 0,7 trên nền thẻ "
                + "còn 4,48:1, đúng phiếu W10-06):" + Environment.NewLine
                + string.Join(Environment.NewLine, offenders.ToArray()));
        }

        /// <summary>
        /// Một dòng USS có phải là khai <c>color: var(--unity-colors-default-text)</c> hay không. Bỏ hết khoảng trắng rồi
        /// mới so đầu dòng: như thế <c>background-color:</c> và <c>border-left-color:</c> không lọt vào (chúng không bắt
        /// đầu bằng <c>color:</c>), mà mọi kiểu thụt lề hay khoảng trắng quanh dấu hai chấm đều bắt được.
        /// </summary>
        private static bool IsTextColorFromUnityVariable(string line)
        {
            string condensed = line.Replace(" ", string.Empty).Replace("\t", string.Empty);
            return condensed.StartsWith(UnityDefaultTextColorDeclaration, StringComparison.Ordinal);
        }

        /// <summary>Tên file để câu đỏ chỉ được tay vào đúng sheet, không in cả đường dẫn tuyệt đối của máy chạy.</summary>
        private static string FileNameOf(string fullPath)
        {
            return Path.GetFileName(fullPath);
        }

        private static void CollectComposedFailures(VisualElement root, string skinName, bool proSkin, List<string> failures)
        {
            for (int index = 0; index < Sites.Length; index++)
            {
                UxComposedSite site = Sites[index];
                CustomStyleProperty<Color> property = new CustomStyleProperty<Color>(site.TokenName);
                if (!root.customStyle.TryGetValue(property, out Color declared))
                {
                    failures.Add(skinName + " · " + site.TokenName + " (" + site.Place
                        + "): TryGetValue = false — theme không khai token này cho skin ấy");
                    continue;
                }

                Color backdrop = BackdropFor(site.Backdrop, proSkin);
                Color faded = new Color(declared.r, declared.g, declared.b, declared.a * site.AncestorOpacity);
                Color seen = UxLayoutAuditor.CompositeOver(faded, backdrop);
                float ratio = UxLayoutAuditor.ContrastRatio(seen, backdrop);
                if (ratio >= site.MinimumRatio) continue;
                failures.Add(skinName + " · " + site.Place + ": " + UxLayoutAuditor.Number(ratio) + ":1 — token "
                    + site.TokenName + " = " + HexText(declared) + " nhân opacity " + UxLayoutAuditor.Number(site.AncestorOpacity)
                    + " hợp thành ra " + HexText(seen) + " trên nền " + HexText(backdrop) + " ("
                    + BackdropName(site.Backdrop) + ") — cần ≥ " + UxLayoutAuditor.Number(site.MinimumRatio) + ":1");
            }
        }

        private static Color BackdropFor(UxComposedBackdrop backdrop, bool proSkin)
        {
            switch (backdrop)
            {
                case UxComposedBackdrop.CardBackground: return CardBackground(proSkin);
                case UxComposedBackdrop.ChipBackground: return ChipBackground(proSkin);
                default: return UxLayoutAuditor.WindowBackground(proSkin);
            }
        }

        private static string BackdropName(UxComposedBackdrop backdrop)
        {
            switch (backdrop)
            {
                case UxComposedBackdrop.CardBackground: return "nền header thẻ";
                case UxComposedBackdrop.ChipBackground: return "nền chip header";
                default: return "nền cửa sổ";
            }
        }

        private static Color CardBackground(bool proSkin)
        {
            return Hex(proSkin ? DarkCardBackgroundHex : LightCardBackgroundHex);
        }

        private static Color ChipBackground(bool proSkin)
        {
            return Hex(proSkin ? DarkChipBackgroundHex : LightChipBackgroundHex);
        }

        private static void AssertSameColor(Color expected, Color measured, string skinName, string sourceName)
        {
            string message = "hằng nền " + skinName + " của bộ test là " + HexText(expected) + " nhưng " + sourceName
                + " là " + HexText(measured)
                + " — mọi tỉ số ở bộ test này đang nói về một cửa sổ khác cửa sổ người dùng thấy";
            Assert.AreEqual(expected.r, measured.r, BackgroundToleranceChannels, message + " (kênh đỏ)");
            Assert.AreEqual(expected.g, measured.g, BackgroundToleranceChannels, message + " (kênh lục)");
            Assert.AreEqual(expected.b, measured.b, BackgroundToleranceChannels, message + " (kênh lam)");
        }

        /// <summary>
        /// Bỏ mọi chú thích khối <c>/* … *&#47;</c> của USS, giữ nguyên độ dài dòng không quan trọng — ca duy nhất dùng hàm này
        /// chỉ cần biết token nào nằm trong khối skin nào.
        /// </summary>
        private static string StripBlockComments(string text)
        {
            System.Text.StringBuilder stripped = new System.Text.StringBuilder(text.Length);
            int index = 0;
            while (index < text.Length)
            {
                if (index + 1 < text.Length && text[index] == '/' && text[index + 1] == '*')
                {
                    int close = text.IndexOf("*/", index + 2, StringComparison.Ordinal);
                    if (close < 0) break;
                    index = close + 2;
                    continue;
                }

                stripped.Append(text[index]);
                index++;
            }

            return stripped.ToString();
        }

        private static Color Hex(string value)
        {
            Assert.IsTrue(ColorUtility.TryParseHtmlString(value, out Color parsed), "không đọc được màu " + value);
            return parsed;
        }

        private static string HexText(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

        private static string PackageDirectory()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UxComposedTokenContrastTests).Assembly);
            Assert.IsNotNull(package,
                "không hỏi được gốc package từ assembly test — bộ test này phải chạy như một phần của com.dreamtech.liveops");
            return Path.GetFullPath(package.resolvedPath);
        }
    }
}
