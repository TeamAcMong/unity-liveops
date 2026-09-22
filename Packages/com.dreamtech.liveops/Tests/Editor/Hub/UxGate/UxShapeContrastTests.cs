using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Nền đục mà một component đứng lên — ba mặt phẳng có thật của hub, cùng bảng với
    /// <see cref="UxComposedTokenContrastTests"/> (hằng đọc thẳng từ đó, không chép tay).
    /// </summary>
    internal enum UxShapeBackdrop
    {
        /// <summary>Nền cửa sổ hub — <c>--unity-colors-window-background</c>.</summary>
        Window,

        /// <summary>Nền header thẻ và mọi dải công cụ — <c>--unity-colors-toolbar-background</c>.</summary>
        Card,

        /// <summary>Nền chip header / rail / minimap — <c>--unity-colors-default-background</c>.</summary>
        Chip,
    }

    /// <summary>
    /// Một component mà người dùng phải NHẬN RA LÀ MỘT COMPONENT: tên, cách dựng, nhánh mang cạnh/nền cần đo, và nền nó
    /// đứng lên. Khác bảng chỗ-dùng của <see cref="UxComposedTokenContrastTests"/> ở chỗ nó KHÔNG khai màu: màu đọc từ
    /// <c>resolvedStyle</c> của phần tử thật, nên không có con số nào ở đây là lời khai tay.
    /// </summary>
    internal readonly struct UxShapeComponent
    {
        public UxShapeComponent(string name, Func<VisualElement> build, string measuredChildSelector,
            UxShapeBackdrop backdrop, string place, string borderTokenName = null, string backgroundTokenName = null)
        {
            Name = name;
            Build = build;
            MeasuredChildSelector = measuredChildSelector;
            Backdrop = backdrop;
            Place = place;
            BorderTokenName = borderTokenName;
            BackgroundTokenName = backgroundTokenName;
        }

        public string Name { get; }

        public Func<VisualElement> Build { get; }

        /// <summary>Class của con MANG cạnh/nền thật (ô nhập của TextField); rỗng = đo chính phần tử dựng ra.</summary>
        public string MeasuredChildSelector { get; }

        public UxShapeBackdrop Backdrop { get; }

        public string Place { get; }

        /// <summary>
        /// Tên custom property của hub mà luật USS của component này đọc cho MÀU CẠNH — rỗng nghĩa là cạnh lấy màu từ một
        /// biến của chính Unity. Khai NGUỒN chứ không khai GIÁ TRỊ: giá trị vẫn đọc từ <c>resolvedStyle</c> của phần tử
        /// thật, dòng này chỉ trả lời "màu ấy của ai", và <see cref="UxShapeContrastTests"/> đối chiếu lại với giá trị
        /// token đọc trên chính root ấy nên một lời khai sai là cổng đỏ chứ không phải một dòng ghi chú mốc.
        /// </summary>
        public string BorderTokenName { get; }

        /// <summary>Như <see cref="BorderTokenName"/> nhưng cho MẶT NỀN.</summary>
        public string BackgroundTokenName { get; }
    }

    /// <summary>
    /// Một chỗ ĐÃ ĐO ĐƯỢC LÀ TRƯỢT và được ghi nợ thay vì sửa trong đợt này. Ca cổng đối chiếu tỉ số đo lại với con số khai
    /// ở đây: nợ vì vậy KHÔNG phải một lời miễn trừ suông mà là một con số bị khoá — trượt thêm một nấc là cổng đỏ, và sửa
    /// xong thì cũng đỏ (buộc phải gỡ dòng nợ), đúng kiểu mà đợt này đòi.
    /// </summary>
    internal readonly struct UxShapeDebt
    {
        public UxShapeDebt(string componentName, string skinName, string debtId, float measuredRatio)
        {
            ComponentName = componentName;
            SkinName = skinName;
            DebtId = debtId;
            MeasuredRatio = measuredRatio;
        }

        public string ComponentName { get; }

        public string SkinName { get; }

        public string DebtId { get; }

        public float MeasuredRatio { get; }
    }

    /// <summary>
    /// (J3-02) Cổng tương phản cho CẠNH và NỀN của component — nửa mà bộ cổng chưa bao giờ có.
    /// <para>
    /// Khe mà bộ test này bịt, nói thẳng: mọi bằng chứng tương phản của hub tới lượt W11 đều đo màu CHỮ
    /// (<see cref="UxComposedContrast.CollectFailures"/> duyệt <c>TextElement</c>, bảng chỗ-dùng của
    /// <see cref="UxComposedTokenContrastTests"/> khai toàn token chữ, <c>capture.sh --contrast</c> đo chữ trên pixel).
    /// Bậc hình khối 3:1 của WCAG 2.1 §1.4.11 chỉ có đúng MỘT dòng (viền swatch "loại chưa khai báo", phiếu W10-09).
    /// Hậu quả đo được: nút của hub ở skin TỐI vẽ nền #585858 trên nền trang #383838 (1,65:1) với viền #303030
    /// (1,13:1) suốt mười một đợt mà không ca nào đỏ — lượt đi dạo W11 phải đếm tay trên pixel ảnh chụp mới thấy.
    /// </para>
    /// <para>
    /// Luật đo, đúng theo §1.4.11: một component ĐỌC RA LÀ COMPONENT khi ÍT NHẤT MỘT trong các nét nhận dạng của nó —
    /// bốn cạnh hoặc mặt nền — đạt 3:1 so với nền XUNG QUANH. Vì sao "ít nhất một" chứ không "mọi": nút skin sáng có nền
    /// #E4E4E4 chỉ 1,32:1 so với nền trang #C8C8C8, nhưng viền #6B6B6B đạt 3,19:1 và mắt đọc ra nút ngay — đòi cả hai là
    /// đòi một thứ mà cả Unity lẫn WCAG đều không đòi, và sẽ biến cổng thành máy sinh miễn trừ.
    /// </para>
    /// <para>
    /// PHỦ ĐẾN ĐÂU, khai trước khi ai đọc rộng hơn: màu của một component đến từ hai nguồn. Nguồn THỨ NHẤT là token của
    /// hub — nó theo class <c>liveops-hub--skin-light</c> nên một lượt đọc được CẢ HAI skin. Nguồn THỨ HAI là biến của
    /// chính Unity (<c>--unity-colors-button-background</c>, <c>--unity-colors-window-border</c>…) — nó theo skin ĐANG
    /// CHẠY của Editor, mà đổi <c>EditorPrefs UserSkin</c> là độc quyền của <c>capture.sh</c> (SP-4). Bộ test này KHÔNG
    /// đoán nguồn và cũng KHÔNG đoán theo giá trị: bảng <see cref="UxShapeContrastTests"/> khai TÊN token là nguồn của
    /// từng nét, và ở skin không-đang-chạy chỉ những nét có nguồn ấy — đối chiếu lại đúng giá trị token đọc trên chính
    /// root ấy — mới được tính. Nét còn lại ghi ra bằng chứng dưới dạng "chưa đo được ở skin kia", không tính là đạt.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    [Category(LiveOpsHubTestCategories.UxGate)]
    public sealed class UxShapeContrastTests
    {
        /// <summary>Bậc WCAG 2.1 AA cho thành phần giao diện — chốt của USER 19/9/2026, đọc hằng dùng chung.</summary>
        private const float ShapeContrastRatio = UxComposedContrast.ShapeContrastRatio;

        /// <summary>Số khung chờ style resolve xong trên root dựng tay (theo UxComposedTokenContrastTests).</summary>
        private const int StyleResolveFrames = 4;

        /// <summary>Hai màu coi là MỘT khi mọi kênh lệch dưới ngần này — dưới một nấc 8 bit là làm tròn, không phải khác màu.</summary>
        private const float SameColorTolerance = 1.5f / 255f;

        /// <summary>Sai số khi đối chiếu tỉ số đo lại với con số đã khai ở bảng nợ.</summary>
        private const float DebtRatioTolerance = 0.05f;

        private const string DarkSkinName = "skin tối";
        private const string LightSkinName = "skin sáng";

        private const string ThemeRelativePath = "Editor/Hub/UI/liveops-hub-theme.uss";

        /// <summary>Token viền nút của hub — nguồn màu cạnh của MỌI dòng nút trong bảng dưới đây.</summary>
        private const string ButtonBorderTokenName = "--liveops-hub-color-button-border";

        /// <summary>
        /// Khai báo custom property MÀU của hub trong theme — dò trên văn bản file, không chép tay một danh sách sẽ mốc.
        /// Chỉ nhận giá trị mở đầu bằng <c>#</c> hoặc <c>rgb</c>: theme còn khai token khoảng cách (<c>--liveops-hub-space-4:
        /// 4px</c>), và đọc một Dimension ra Color làm UI Toolkit ghi cảnh báo — lượt chạy đầu của ca này đỏ vì đúng chỗ đó.
        /// </summary>
        private static readonly Regex ColorTokenDeclarationPattern =
            new Regex(@"(--liveops-hub-[a-z0-9-]+)\s*:\s*(?:#|rgba?\()", RegexOptions.Compiled);

        /// <summary>
        /// Bốn họ component mà phiếu J3-02 đòi mở rộng phép đo sang: nút (thường + phá huỷ), ô nhập, chip, viền trạng thái.
        /// Mỗi dòng dựng đúng thứ hub dựng — cùng class, cùng thứ tự — nên màu đo được là màu người dùng thấy.
        /// </summary>
        private static readonly UxShapeComponent[] Components =
        {
            new UxShapeComponent("nút thường", BuildButton, null, UxShapeBackdrop.Window,
                "nút của mọi section header và mọi pane — diện mạo nút chung của hub", ButtonBorderTokenName),
            new UxShapeComponent("nút phá huỷ", BuildDangerButton, null, UxShapeBackdrop.Window,
                "'Xoá luật…' màn Luật lặp và 'Xoá đợt…' inspector Lịch", ButtonBorderTokenName),
            new UxShapeComponent("nút trên nền thẻ", BuildButton, null, UxShapeBackdrop.Card,
                "nút nằm trong header thẻ / dải công cụ (thẻ Xuất JSON, toolbar màn Kiểm lịch)", ButtonBorderTokenName),
            // (R-F3 lượt soát G-W11-EYE2) Mặt phẳng THỨ TƯ mà chú thích của token đã khai từ J2-02 và bảng này bỏ trắng ở
            // lượt đầu: nền chip / rail / minimap. Đó là mặt phẳng DUY NHẤT đã biết là trượt (nợ W11-16), nên để nó ngoài
            // bảng là để nợ ấy không có số bị khoá trong khi ba nợ mới W11-18/19/20 thì có — hai chuẩn cho cùng một thứ.
            new UxShapeComponent("nút trên nền chip", BuildButton, null, UxShapeBackdrop.Chip,
                "nút đặt trên .liveops-hub-rail / .liveops-hub-chip / minimap — mặt phẳng của nợ W11-16",
                ButtonBorderTokenName),
            new UxShapeComponent("ô nhập", BuildTextField, "unity-base-text-field__input", UxShapeBackdrop.Window,
                "mọi ô nhập của inspector Lịch và form Luật lặp"),
            new UxShapeComponent("chip header", BuildChip, null, UxShapeBackdrop.Window,
                "chip tên calendar đang mở ở header hub"),
            new UxShapeComponent("viền trạng thái (dải summary)", BuildSummaryStrip, null, UxShapeBackdrop.Window,
                "dải '2 bị bỏ · 1 mất tiến độ' màn Kiểm lịch — viền --unity-colors-window-border"),
            new UxShapeComponent("viền thẻ", BuildCard, null, UxShapeBackdrop.Window,
                "khung của mọi thẻ (Tổng quan, Xuất JSON) — cùng token viền với dải summary"),
        };

        /// <summary>
        /// Chỗ đã đo được là TRƯỢT và user duyệt ghi nợ thay vì sửa trong đợt này. Mỗi dòng phải có mã nợ và con số đo
        /// được; ca <see cref="EveryComponentEdge_MeetsShapeContrast_OnItsBackdrop"/> khoá đúng con số ấy.
        /// </summary>
        private static readonly UxShapeDebt[] Debts =
        {
            // Ba chỗ dưới đây do lượt đo ĐẦU TIÊN của chính bộ test này tìm ra — không phiếu nào của lượt đi dạo nói về
            // chúng, vì mắt người chỉ bắt được cái nút. Cả ba KHÔNG phải màu của hub: chúng là mặc định của chính Unity
            // (ô nhập, chip, khung thẻ) dùng nguyên khắp hub, nên sửa là một quyết định diện mạo toàn cục chứ không phải
            // một bản vá — và phiếu J3-02 chỉ mang quyết định của user cho NÚT. Ghi nợ kèm số đo, khoá số lại ở đây.
            // (R-F3) Nợ W11-16 mở từ đợt J2-02: viền nút skin SÁNG #6B6B6B trên nền chip #A5A5A5 chỉ 2,16:1. Nay có dòng
            // trong bảng nên nó là một con số BỊ KHOÁ như ba nợ mới — trượt thêm một nấc hay sửa xong đều làm cổng đỏ.
            new UxShapeDebt("nút trên nền chip", LightSkinName, "W11-16", 2.16f),
            new UxShapeDebt("ô nhập", DarkSkinName, "W11-18", 1.7f),
            new UxShapeDebt("chip header", DarkSkinName, "W11-19", 1.3f),
            new UxShapeDebt("viền trạng thái (dải summary)", DarkSkinName, "W11-20", 1.3f),
            new UxShapeDebt("viền thẻ", DarkSkinName, "W11-20", 1.3f),
        };

        private TimelineTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            if (_panel != null) _panel.Dispose();
            _panel = null;
        }

        /// <summary>
        /// Mọi component của bảng phải đọc ra LÀ component trên nền của nó, ở mọi skin mà lượt này đo được. Gom mọi chỗ
        /// trượt vào một câu đỏ kèm bốn cạnh + nền của từng chỗ: người sửa màu cần thấy trọn bảng trong một lượt.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryComponentEdge_MeetsShapeContrast_OnItsBackdrop()
        {
            bool proSkin = EditorGUIUtility.isProSkin;
            string runningSkinName = proSkin ? DarkSkinName : LightSkinName;

            _panel = TimelineTestPanel.Open();
            VisualElement darkRoot = CreateShapeRoot(false);
            VisualElement lightRoot = CreateShapeRoot(true);
            Dictionary<string, VisualElement> darkProbes = AddProbes(darkRoot);
            Dictionary<string, VisualElement> lightProbes = AddProbes(lightRoot);

            yield return TimelineTestPanel.WaitUntil(
                () => TimelineTestPanel.HasLayout(darkRoot) && TimelineTestPanel.HasLayout(lightRoot),
                "hai root đo hình khối chưa có layout");
            for (int frame = 0; frame < StyleResolveFrames; frame++) yield return null;

            AssertDeclaredTokenNamesExist();

            List<string> failures = new List<string>();
            List<string> evidence = new List<string>();
            int measuredCount = 0;

            for (int index = 0; index < Components.Length; index++)
            {
                UxShapeComponent component = Components[index];
                VisualElement darkProbe = Measured(darkProbes[component.Name], component);
                VisualElement lightProbe = Measured(lightProbes[component.Name], component);
                measuredCount += AppendSkin(component, DarkSkinName, darkProbe, darkRoot, true,
                    runningSkinName, failures, evidence);
                measuredCount += AppendSkin(component, LightSkinName, lightProbe, lightRoot, false,
                    runningSkinName, failures, evidence);
            }

            WriteEvidence(evidence);
            Assert.Greater(measuredCount, 0, "không chỗ nào được đo — kết luận từ vòng lặp rỗng là xanh giả");
            Assert.IsEmpty(failures,
                "cạnh/nền của một component không đạt bậc hình khối " + UxLayoutAuditor.Number(ShapeContrastRatio)
                + ":1 (chốt của USER 19/9):" + Environment.NewLine + string.Join(Environment.NewLine, failures.ToArray()));
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Nền dùng làm mốc phải khớp số đo THẬT ở skin đang chạy. Không có ca này thì ba hằng nền là lời khai tay và mọi
        /// tỉ số ở trên nói về một cửa sổ khác cửa sổ người dùng thấy.
        /// <para>
        /// Ca song sinh với <c>UxComposedTokenContrastTests.ComposedBackdropConstants_MatchMeasuredBackdrops_InRunningSkin</c>
        /// và cùng giới hạn của nó: khoá ĐÚNG cột skin đang chạy.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator ShapeBackdropConstants_MatchMeasuredBackdrops_InRunningSkin()
        {
            bool proSkin = EditorGUIUtility.isProSkin;
            _panel = TimelineTestPanel.Open();
            VisualElement root = CreateShapeRoot(!proSkin);

            VisualElement cardHeaderProbe = new VisualElement();
            cardHeaderProbe.AddToClassList(LiveOpsHubClassNames.CardHeader);
            root.Add(cardHeaderProbe);
            VisualElement chipProbe = new VisualElement();
            chipProbe.AddToClassList(LiveOpsHubClassNames.Chip);
            root.Add(chipProbe);

            yield return TimelineTestPanel.WaitUntil(() => TimelineTestPanel.HasLayout(root), "root đo nền chưa có layout");
            for (int frame = 0; frame < StyleResolveFrames; frame++) yield return null;

            AssertSameColor(BackdropColor(UxShapeBackdrop.Window, proSkin), root.resolvedStyle.backgroundColor, "nền cửa sổ");
            AssertSameColor(BackdropColor(UxShapeBackdrop.Card, proSkin), cardHeaderProbe.resolvedStyle.backgroundColor, "nền thẻ");
            AssertSameColor(BackdropColor(UxShapeBackdrop.Chip, proSkin), chipProbe.resolvedStyle.backgroundColor, "nền chip");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Viền nút phải đọc token <c>--liveops-hub-color-button-border</c> ở CẢ HAI skin, không phải viền mặc định của
        /// Unity. Ca này tách riêng khỏi ca tỉ số vì nó trả lời một câu khác: "luật USS có ăn không". Một lượt xoá nhầm
        /// luật <c>.liveops-hub-button { border-color: … }</c> vẫn để ca tỉ số XANH ở skin đang chạy nếu Unity tình cờ
        /// vẽ đủ tương phản — nhưng skin kia thì không, và mắt người dùng thì thấy ngay.
        /// </summary>
        [UnityTest]
        public IEnumerator ButtonBorder_ReadsHubToken_InBothSkins()
        {
            _panel = TimelineTestPanel.Open();
            VisualElement darkRoot = CreateShapeRoot(false);
            VisualElement lightRoot = CreateShapeRoot(true);
            Button darkButton = (Button)BuildButton();
            Button lightButton = (Button)BuildButton();
            darkRoot.Add(darkButton);
            lightRoot.Add(lightButton);

            yield return TimelineTestPanel.WaitUntil(
                () => TimelineTestPanel.HasLayout(darkRoot) && TimelineTestPanel.HasLayout(lightRoot),
                "hai root đo viền nút chưa có layout");
            for (int frame = 0; frame < StyleResolveFrames; frame++) yield return null;

            AssertBorderReadsToken(darkRoot, darkButton, DarkSkinName);
            AssertBorderReadsToken(lightRoot, lightButton, LightSkinName);
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------------------------------------ dựng probe

        private static VisualElement BuildButton()
        {
            Button button = new Button { text = "Kiểm lại" };
            button.AddToClassList(LiveOpsHubClassNames.Button);
            return button;
        }

        private static VisualElement BuildDangerButton()
        {
            Button button = (Button)BuildButton();
            button.text = "Xoá luật…";
            button.AddToClassList(LiveOpsHubClassNames.ButtonDanger);
            button.AddToClassList(LiveOpsHubClassNames.ButtonSelfStart);
            return button;
        }

        private static VisualElement BuildTextField()
        {
            return new TextField { value = "weekly-pass-35" };
        }

        private static VisualElement BuildChip()
        {
            VisualElement chip = new VisualElement();
            chip.AddToClassList(LiveOpsHubClassNames.Chip);
            return chip;
        }

        private static VisualElement BuildSummaryStrip()
        {
            VisualElement strip = new VisualElement();
            strip.AddToClassList(LiveOpsHubClassNames.ValidationSummary);
            return strip;
        }

        private static VisualElement BuildCard()
        {
            VisualElement card = new VisualElement();
            card.AddToClassList(LiveOpsHubClassNames.Card);
            return card;
        }

        /// <summary>Root mang token hai skin + bốn sheet mà các class trên sống trong đó (shell cho chip, validation cho dải summary).</summary>
        private VisualElement CreateShapeRoot(bool lightSkin)
        {
            VisualElement root = _panel.CreateRoot(lightSkin);
            foreach (string path in new[] { LiveOpsHubPaths.ShellUss, LiveOpsHubPaths.ValidationSectionUss })
            {
                StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                Assert.IsNotNull(sheet, "thiếu " + path);
                root.styleSheets.Add(sheet);
            }
            return root;
        }

        /// <summary>
        /// Mỗi component dựng một probe RIÊNG cho mỗi root: một VisualElement chỉ nằm được trong một cây, và dùng chung
        /// probe giữa hai root là đo cùng một skin hai lần mà không ai biết.
        /// </summary>
        private static Dictionary<string, VisualElement> AddProbes(VisualElement root)
        {
            Dictionary<string, VisualElement> probes = new Dictionary<string, VisualElement>(StringComparer.Ordinal);
            for (int index = 0; index < Components.Length; index++)
            {
                UxShapeComponent component = Components[index];
                VisualElement probe = component.Build();
                probes[component.Name] = probe;
                root.Add(probe);
            }

            return probes;
        }

        private static VisualElement Measured(VisualElement probe, UxShapeComponent component)
        {
            if (string.IsNullOrEmpty(component.MeasuredChildSelector)) return probe;
            VisualElement child = probe.Q(className: component.MeasuredChildSelector);
            Assert.IsNotNull(child, component.Name + ": không tìm thấy con mang class " + component.MeasuredChildSelector);
            return child;
        }

        // ------------------------------------------------------------------------------------------------ phép đo

        /// <summary>Năm nét nhận dạng của một component: bốn cạnh rồi tới mặt nền — thứ tự giữ nguyên để câu đỏ đọc được.</summary>
        private static Color[] ShapeColorsOf(VisualElement element)
        {
            IResolvedStyle style = element.resolvedStyle;
            return new[]
            {
                style.borderTopColor, style.borderRightColor, style.borderBottomColor, style.borderLeftColor,
                style.backgroundColor,
            };
        }

        /// <summary>
        /// (R-F4 lượt soát G-W11-EYE2) Bề dày của đúng bốn cạnh ấy, cùng thứ tự; mặt nền luôn "có" nên nhận
        /// <see cref="float.PositiveInfinity"/>. Vì sao cần: một cạnh bề dày 0 VẪN có màu trong <c>resolvedStyle</c>, nên
        /// không đọc bề dày thì một component có thể ĐẠT bậc nhờ một nét KHÔNG BAO GIỜ ĐƯỢC VẼ — đúng hạng "đạt giả" mà
        /// bộ đo này được dựng ra để chặn.
        /// </summary>
        private static float[] ShapeWidthsOf(VisualElement element)
        {
            IResolvedStyle style = element.resolvedStyle;
            return new[]
            {
                style.borderTopWidth, style.borderRightWidth, style.borderBottomWidth, style.borderLeftWidth,
                float.PositiveInfinity,
            };
        }

        private static readonly string[] ShapeColorNames = { "cạnh trên", "cạnh phải", "cạnh dưới", "cạnh trái", "mặt nền" };

        /// <summary>Bốn nét đầu của <see cref="ShapeColorNames"/> là CẠNH; nét thứ năm là mặt nền.</summary>
        private const int BorderColorCount = 4;

        /// <summary>
        /// Đo một component ở MỘT skin và ghi kết quả. Trả về số nét THẬT SỰ được đo ở skin ấy — 0 nghĩa là skin này không
        /// nói được gì về component này (mọi nét của nó lấy màu từ biến của Unity, không truy được về token nào của hub),
        /// và điều đó được ghi ra bằng chứng chứ không im lặng tính là đạt.
        /// <para>
        /// (R-F1 lượt soát G-W11-EYE2) Ở skin KHÔNG đang chạy, một nét chỉ được tin khi NGUỒN của nó là một custom property
        /// của hub — tức bảng <see cref="Components"/> khai tên token cho nét ấy VÀ màu đọc được trùng đúng giá trị token
        /// ấy trên chính root này. Lượt đầu tin theo GIÁ TRỊ ("màu này trùng giá trị một token nào đó của hub") và điều đó
        /// cho một kết quả ĐẠT GIẢ đo được: mặt nền của dải summary đọc <c>--unity-colors-toolbar-background</c> = #3C3C3C
        /// ở skin tối đang chạy, mà theme lại khai <c>--liveops-hub-legend-fixed: #3C3C3C</c> trong khối skin sáng — hai
        /// giá trị trùng nhau, phép lọc tưởng là màu của hub và ghi 6,6:1 cho một mặt nền mà skin sáng KHÔNG BAO GIỜ vẽ
        /// (pixel ảnh chụp skin sáng: nền #CBCBCB 1,02:1, cạnh #939393 1,84:1). Trùng giá trị không phải là cùng nguồn.
        /// </para>
        /// </summary>
        private static int AppendSkin(UxShapeComponent component, string skinName, VisualElement probe,
            VisualElement skinRoot, bool darkSkin, string runningSkinName, List<string> failures, List<string> evidence)
        {
            bool isRunningSkin = string.Equals(skinName, runningSkinName, StringComparison.Ordinal);
            Color[] colors = ShapeColorsOf(probe);
            float[] widths = ShapeWidthsOf(probe);
            Color backdrop = BackdropColor(component.Backdrop, darkSkin);
            float best = 0f;
            string bestName = string.Empty;
            int measured = 0;
            StringBuilder line = new StringBuilder();
            line.Append(component.Name).Append(" · ").Append(skinName).Append(" · nền ").Append(HexOf(backdrop)).Append(": ");

            for (int index = 0; index < colors.Length; index++)
            {
                Color color = colors[index];
                // Nét trong suốt không phải nét: không có gì để mắt bám vào.
                if (color.a < 0.05f) continue;
                // Cạnh bề dày 0 cũng không phải nét: nó có màu nhưng không được vẽ ra pixel nào.
                if (widths[index] <= 0f) continue;
                string tokenName = index < BorderColorCount ? component.BorderTokenName : component.BackgroundTokenName;
                if (!string.IsNullOrEmpty(tokenName))
                {
                    Color token;
                    if (!skinRoot.customStyle.TryGetValue(new CustomStyleProperty<Color>(tokenName), out token))
                    {
                        failures.Add(component.Name + " · " + skinName + ": theme không khai " + tokenName
                            + " cho skin này — bảng Components khai một nguồn không còn tồn tại");
                        continue;
                    }

                    if (!SameColor(color, token))
                    {
                        // Luật USS thôi đọc token (bị xoá, bị một luật khác đè). Con số tính ra sau đó sẽ nói về màu mặc
                        // định của Unity chứ không về thứ hub khai, nên đây là ĐỎ, không phải một dòng bằng chứng.
                        failures.Add(component.Name + " · " + skinName + " (" + component.Place + "): "
                            + ShapeColorNames[index] + " đọc ra " + HexOf(color) + " nhưng token " + tokenName
                            + " của skin này là " + HexOf(token)
                            + " — luật USS thôi đọc token, mọi con số của dòng này sẽ nói về một cửa sổ khác");
                        continue;
                    }
                }
                else if (!isRunningSkin)
                {
                    // Không truy được nguồn về hub: ở skin không đang chạy, màu đọc được là màu của skin ĐANG CHẠY.
                    continue;
                }

                float ratio = UxLayoutAuditor.ContrastRatio(UxLayoutAuditor.CompositeOver(color, backdrop), backdrop);
                measured++;
                line.Append(ShapeColorNames[index]).Append(' ').Append(HexOf(color)).Append(' ')
                    .Append(UxLayoutAuditor.Number(ratio)).Append(":1  ");
                if (ratio <= best) continue;
                best = ratio;
                bestName = ShapeColorNames[index];
            }

            if (measured == 0)
            {
                evidence.Add(line.Append("CHƯA ĐO ĐƯỢC ở skin này — không nét nào của nó truy được về một token của hub, "
                    + "mà biến của Unity thì theo skin ĐANG CHẠY của Editor (" + runningSkinName + ") chứ không theo class").ToString());
                return 0;
            }

            line.Append("→ mạnh nhất ").Append(bestName).Append(' ').Append(UxLayoutAuditor.Number(best)).Append(":1");
            UxShapeDebt debt;
            if (TryFindDebt(component.Name, skinName, out debt))
            {
                line.Append("  [nợ ").Append(debt.DebtId).Append("]");
                evidence.Add(line.ToString());
                Assert.AreEqual(debt.MeasuredRatio, best, DebtRatioTolerance,
                    "nợ " + debt.DebtId + " (" + component.Name + " · " + skinName + ") khai " + UxLayoutAuditor.Number(debt.MeasuredRatio)
                    + ":1 nhưng đo lại ra " + UxLayoutAuditor.Number(best) + ":1 — sửa xong thì gỡ dòng nợ, trượt thêm thì đổi số");
                return measured;
            }

            evidence.Add(line.ToString());
            if (best >= ShapeContrastRatio) return measured;
            failures.Add(component.Name + " · " + skinName + " (" + component.Place + "): nét mạnh nhất là " + bestName
                + " " + UxLayoutAuditor.Number(best) + ":1 trên nền " + HexOf(backdrop) + " — cần ≥ "
                + UxLayoutAuditor.Number(ShapeContrastRatio) + ":1");
            return measured;
        }

        /// <summary>
        /// Mọi TÊN custom property màu mà theme của hub khai, dò trên văn bản file. Dùng để chặn một lời khai nguồn đã mốc:
        /// bảng <see cref="Components"/> khai tên token cho từng nét, và một tên gõ sai hay một token bị đổi tên sẽ làm mọi
        /// nét của component ấy lặng lẽ rơi về "chưa đo được" thay vì báo động.
        /// </summary>
        private static void AssertDeclaredTokenNamesExist()
        {
            string packageDirectory = UxContrastEvidence.PackageDirectory();
            Assert.IsNotNull(packageDirectory, "không hỏi được gốc package — không đọc được theme để kiểm tên token");
            string themeText = File.ReadAllText(Path.Combine(packageDirectory, ThemeRelativePath));
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in ColorTokenDeclarationPattern.Matches(themeText))
            {
                names.Add(match.Groups[1].Value);
            }

            Assert.IsNotEmpty(names, "không đọc được token màu nào của hub trong theme — phép kiểm nguồn sẽ vô nghĩa");
            for (int index = 0; index < Components.Length; index++)
            {
                UxShapeComponent component = Components[index];
                AssertTokenNameExists(names, component.Name, component.BorderTokenName);
                AssertTokenNameExists(names, component.Name, component.BackgroundTokenName);
            }
        }

        private static void AssertTokenNameExists(HashSet<string> declaredNames, string componentName, string tokenName)
        {
            if (string.IsNullOrEmpty(tokenName)) return;
            Assert.IsTrue(declaredNames.Contains(tokenName),
                componentName + " khai nguồn màu là " + tokenName + " nhưng theme không khai token tên ấy");
        }

        private static bool TryFindDebt(string componentName, string skinName, out UxShapeDebt found)
        {
            for (int index = 0; index < Debts.Length; index++)
            {
                if (!string.Equals(Debts[index].ComponentName, componentName, StringComparison.Ordinal)) continue;
                if (!string.Equals(Debts[index].SkinName, skinName, StringComparison.Ordinal)) continue;
                found = Debts[index];
                return true;
            }

            found = default(UxShapeDebt);
            return false;
        }

        private static void AssertBorderReadsToken(VisualElement root, Button button, string skinName)
        {
            CustomStyleProperty<Color> property =
                new CustomStyleProperty<Color>("--liveops-hub-color-button-border");
            Color token;
            Assert.IsTrue(root.customStyle.TryGetValue(property, out token),
                "theme không khai --liveops-hub-color-button-border cho " + skinName);
            AssertSameColor(token, button.resolvedStyle.borderTopColor, skinName + ": cạnh trên của nút");
            AssertSameColor(token, button.resolvedStyle.borderBottomColor, skinName + ": cạnh dưới của nút");
        }

        private static Color BackdropColor(UxShapeBackdrop backdrop, bool darkSkin)
        {
            switch (backdrop)
            {
                case UxShapeBackdrop.Window:
                    return UxLayoutAuditor.WindowBackground(darkSkin);
                case UxShapeBackdrop.Card:
                    return ParseHex(darkSkin
                        ? UxComposedTokenContrastTests.DarkCardBackgroundHex
                        : UxComposedTokenContrastTests.LightCardBackgroundHex);
                default:
                    return ParseHex(darkSkin
                        ? UxComposedTokenContrastTests.DarkChipBackgroundHex
                        : UxComposedTokenContrastTests.LightChipBackgroundHex);
            }
        }

        private static Color ParseHex(string hex)
        {
            Color color;
            Assert.IsTrue(ColorUtility.TryParseHtmlString(hex, out color), "không đọc được màu " + hex);
            return color;
        }

        private static bool SameColor(Color first, Color second)
        {
            return Mathf.Abs(first.r - second.r) <= SameColorTolerance
                && Mathf.Abs(first.g - second.g) <= SameColorTolerance
                && Mathf.Abs(first.b - second.b) <= SameColorTolerance
                && Mathf.Abs(first.a - second.a) <= SameColorTolerance;
        }

        private static void AssertSameColor(Color expected, Color measured, string place)
        {
            Assert.IsTrue(SameColor(expected, measured),
                place + ": khai " + HexOf(expected) + " nhưng đo ra " + HexOf(measured));
        }

        private static string HexOf(Color color)
        {
            Color32 bytes = color;
            return "#" + bytes.r.ToString("X2", CultureInfo.InvariantCulture)
                + bytes.g.ToString("X2", CultureInfo.InvariantCulture)
                + bytes.b.ToString("X2", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Ghi TOÀN BỘ số đo (cả dòng đạt) ra đĩa. Vì sao cả dòng đạt: báo cáo đợt phải trích được con số của một chỗ ĐANG
        /// XANH — "nút skin tối 5,41:1" — mà không phải làm nó đỏ trước rồi đọc câu assert.
        /// </summary>
        private static void WriteEvidence(List<string> evidence)
        {
            try
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string directory = Path.Combine(Path.Combine(home, ".cache/unity-liveops/ux-shape-contrast"),
                    Application.unityVersion);
                Directory.CreateDirectory(directory);
                string skin = EditorGUIUtility.isProSkin ? "dark" : "light";
                File.WriteAllText(Path.Combine(directory, "shape-" + skin + ".txt"),
                    string.Join(Environment.NewLine, evidence.ToArray()) + Environment.NewLine);
            }
            catch (IOException error)
            {
                // Ghi bằng chứng hỏng KHÔNG được làm đỏ phép đo: câu assert ở trên mới là cổng.
                Debug.Log("UxShapeContrastTests: không ghi được bằng chứng — " + error.Message);
            }
        }
    }
}
