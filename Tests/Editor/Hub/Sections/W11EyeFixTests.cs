using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Đợt W11, gói G-W11-EYE: khoá ba lỗi mà người dùng giả NHÌN THẤY trên ảnh chụp cửa sổ hub thật ở lượt đi dạo W10
    /// (phiếu J2-01, J2-02, J2-06 trong <c>plan/w10/W10-JOURNEY-2.md</c>) — ba lỗi mà mọi ca của cổng tĩnh đều để lọt.
    /// <para>
    /// J2-01 (toast đè dải chú giải) khoá ở <c>UxLayoutAuditTests.Calendar_Toast_DoesNotCoverLegend_AtEveryWidth</c>, vì nó
    /// là quan hệ hình học giữa hai phần tử ở BẢY cỡ cửa sổ — đúng chỗ ma trận bố cục làm việc. Hai phiếu còn lại đo được
    /// trên một màn đơn lẻ nên nằm ở đây.
    /// </para>
    /// <para>
    /// Mọi phép đo đi qua <c>resolvedStyle</c> / <c>worldBound</c> của cửa sổ THẬT ở đúng cỡ người dùng mở: gọi thẳng hàm
    /// model thì chính lớp bị lỗi — lớp bố cục — không bao giờ chạy.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class W11EyeFixTests
    {
        /// <summary>Cỡ cửa sổ rộng nhất của ma trận — chỗ phiếu J2-02 đo được dải nút ~1080px.</summary>
        private const int WideWidth = 1440;

        private const int WideHeight = 900;

        /// <summary>
        /// Trần bề ngang của nút trong banner, tính theo phần bề ngang banner. Nút thật chỉ rộng bằng chữ của nó (cỡ 90px ở
        /// 1440), nên nửa banner là một trần RỘNG RÃI: nó không khoá bản sửa vào một con số pixel, mà chặn đúng hạng lỗi
        /// "nút ăn trọn bề ngang" — thứ chỉ xảy ra khi banner còn là hộp xếp dọc.
        /// </summary>
        private const float NoticeButtonWidthRatio = 0.5f;

        /// <summary>Bậc tương phản hình khối WCAG 2.1 (chốt của USER 19/9) — viền nút là hình khối, không phải chữ.</summary>
        private const float ShapeContrastRatio = 3f;

        /// <summary>Khe ngang tối đa giữa chữ của chip token và dấu phẩy đứng sau nó; trên mức này mắt đọc ra "chữ , chữ".</summary>
        private const float MaximumPunctuationGapPixels = 1.5f;

        /// <summary>
        /// Khe ngang tối thiểu khi mảnh chữ sau chip mở đầu bằng KHOẢNG TRẮNG thật (" + số thứ tự."): khoảng ấy là khoảng cách
        /// của câu, kéo nó lại thì câu đọc thành "[pass-]+ số thứ tự" — lỗi ngược lại của J2-06, và bản vá lượt đầu đã mắc đúng nó.
        /// </summary>
        private const float MinimumWordGapPixels = 2.5f;

        /// <summary>Dấu câu có thể mở đầu một mảnh nối — giống bảng của <c>RecurringRuleSentence</c>, chép ở đây để test đọc được.</summary>
        private static readonly char[] SentencePunctuation = { ',', '.', ';', ':', ')', ']', '!', '?' };

        /// <summary>Hai phần tử coi là CÙNG MỘT HÀNG khi mép trên lệch dưới ngần này — câu gập dòng thì phép đo khe vô nghĩa.</summary>
        private const float SameLineTolerancePixels = 8f;

        /// <summary>Số khung chờ style resolve xong trên root dựng tay (theo UxContrastTokenTests).</summary>
        private const int StyleResolveFrames = 3;

        private SectionTestScope _scope;
        private TimelineTestPanel _panel;

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            if (_scope != null) _scope.Dispose();
            _scope = null;
            if (_panel != null) _panel.Dispose();
            _panel = null;
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        // ------------------------------------------------------------------ J2-02 banner "kết quả cũ" màn Kiểm lịch

        /// <summary>
        /// (J2-02) Nút trong banner "kết quả cũ" KHÔNG được dãn hết bề ngang banner.
        /// <para>
        /// Gốc lỗi đọc được trong mã chứ không phải suy đoán: <c>validation-notice</c> không có luật USS nào, nên nó là hộp
        /// xếp DỌC mặc định của UI Toolkit và <c>align-items</c> mặc định là <c>stretch</c> — nút nhận trọn bề ngang. Ở 1440
        /// mắt thấy một dải nút ~1080px, và ở skin sáng dải ấy gần như tàng hình (xem ca viền dưới đây).
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator ValidationStaleNotice_ButtonDoesNotStretchAcrossBanner()
        {
            yield return OpenStaleValidation();

            VisualElement notice = _scope.View.Q(LiveOpsHubPaths.ValidationElementNames.Notice);
            Assert.IsNotNull(notice, "màn Kiểm lịch thiếu banner 'kết quả cũ'");
            Button button = notice.Q<Button>(ValidationSection.NoticeButtonElementName);
            Assert.IsNotNull(button, "banner 'kết quả cũ' thiếu nút hành động");

            float noticeWidth = notice.resolvedStyle.width;
            float buttonWidth = button.resolvedStyle.width;
            Assert.Greater(noticeWidth, 0f, "banner chưa có bề ngang thì phép đo dưới đây vô nghĩa");
            Assert.Greater(buttonWidth, 0f, "nút chưa có bề ngang — không đo được gì");
            Assert.LessOrEqual(buttonWidth, noticeWidth * NoticeButtonWidthRatio,
                "nút của banner 'kết quả cũ' rộng " + buttonWidth.ToString("0.#", CultureInfo.InvariantCulture) + "px trên banner "
                + noticeWidth.ToString("0.#", CultureInfo.InvariantCulture) + "px — nó đang dãn theo bề ngang banner chứ không theo chữ của nó");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (J2-02) Hai nút "kiểm lại" hiện CÙNG LÚC trên màn Kiểm lịch (một ở section header, một trong banner) không được
        /// mang đúng một chữ: người đọc không có cách nào biết chúng khác gì nhau.
        /// <para>
        /// Đo trên NÚT THẬT chứ không chỉ so hai khoá catalog: khoá có thể khác nhau mà cả hai cùng trỏ về một câu, và
        /// ngược lại một bản dịch mới có thể vô tình làm hai câu trùng nhau ở một ngôn ngữ. Ca này chạy ở ngôn ngữ đang
        /// chạy của hub; ca so catalog hai ngôn ngữ nằm ngay dưới.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator ValidationStaleNotice_ButtonTextDiffersFromHeaderButton()
        {
            yield return OpenStaleValidation();

            Button noticeButton = _scope.View.Q<Button>(ValidationSection.NoticeButtonElementName);
            Assert.IsNotNull(noticeButton, "banner 'kết quả cũ' thiếu nút hành động");
            Button headerButton = _scope.Window.rootVisualElement.Q<Button>(ValidationSection.RecheckButtonElementName);
            Assert.IsNotNull(headerButton, "section header thiếu nút Kiểm lại");

            string noticeText = TextOf(noticeButton);
            string headerText = TextOf(headerButton);
            Assert.IsNotEmpty(noticeText, "nút banner không có chữ — không đo được gì");
            Assert.IsNotEmpty(headerText, "nút header không có chữ — không đo được gì");
            Assert.AreNotEqual(headerText, noticeText,
                "hai nút cùng hiện trên màn Kiểm lịch mang đúng một chữ '" + noticeText + "'");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (J2-02) Cùng một luật, đo ở TẦNG CATALOG cho CẢ HAI ngôn ngữ: bản dịch tiếng Anh cũng không được làm hai nút
        /// trùng chữ. Ca trên chỉ chạy được ngôn ngữ đang chạy của hub, nên một mình nó để lọt bản còn lại.
        /// </summary>
        [Test]
        public void ValidationStaleNotice_ButtonStringDiffersFromHeader_InBothLanguages()
        {
            foreach (LiveOpsHubLanguageId language in new[] { LiveOpsHubLanguageId.Vietnamese, LiveOpsHubLanguageId.English })
            {
                string header;
                string notice;
                Assert.IsTrue(LiveOpsHubStringCatalog.TryGetExact(language,
                        nameof(LiveOpsHubStrings.ValidationRecheckButton), out header),
                    "bản " + language + " thiếu câu cho nút header");
                Assert.IsTrue(LiveOpsHubStringCatalog.TryGetExact(language,
                        nameof(LiveOpsHubStrings.ValidationStaleRecheckButton), out notice),
                    "bản " + language + " thiếu câu cho nút banner");
                Assert.AreNotEqual(header, notice,
                    "bản " + language + ": chữ nút banner trùng chữ nút header");
            }
        }

        /// <summary>
        /// (J2-02) Viền hình khối của nút ở skin SÁNG phải đạt 3:1 trên CẢ HAI nền mà một nút đứng lên: nền trang #C8C8C8
        /// và nền của chính nút #E4E4E4.
        /// <para>
        /// Vì sao viền chứ không phải nền nút: nền nút #E4E4E4 trên nền trang #C8C8C8 chỉ 1,32:1, tức tự nó nút KHÔNG đọc ra
        /// là một nút — chỉ còn viền làm việc ấy. Viền mạnh nhất của Unity ở đó (mép dưới #939393) đo ra 2,42:1 so với nền
        /// nút và 1,84:1 so với nền trang, cả hai dưới bậc.
        /// </para>
        /// <para>
        /// (R09) Vì sao đo HAI nền chứ không một: bản đầu chỉ đo nền trang, nên trong ba con số mà chú thích của token khai
        /// (3,18 · 4,19 · 1,32) chỉ có 3,18 được máy gác — hai con số kia là lời. Nay 4,19 cũng có ca canh.
        /// GIỚI HẠN của ca này: nó KHÔNG phủ nền thứ ba của skin sáng, <c>--unity-colors-default-background</c> #A5A5A5
        /// (rail, chip, minimap…), nơi token chỉ đạt 2,16:1. Hôm nay không nút nào đứng trên nền ấy, và luật "đừng đặt nút
        /// vào đó" khai ở chú thích token trong <c>liveops-hub-theme.uss</c> — nhưng chưa có cổng máy gác, nợ W11-16.
        /// </para>
        /// <para>
        /// Đo trên root dựng tay mang class skin sáng, KHÔNG đổi skin của Editor: <c>capture.sh</c> là chỗ duy nhất được đặt
        /// <c>EditorPrefs UserSkin</c> (SP-4), nên một lượt EditMode phải đọc được khối skin sáng mà không đụng tới máy.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator ButtonBorderToken_MeetsShapeContrast_InLightSkin()
        {
            _panel = TimelineTestPanel.Open();
            VisualElement lightRoot = _panel.CreateRoot(true);
            yield return TimelineTestPanel.WaitUntil(() => TimelineTestPanel.HasLayout(lightRoot),
                "root skin sáng chưa có layout");
            for (int frame = 0; frame < StyleResolveFrames; frame++) yield return null;

            CustomStyleProperty<Color> property = new CustomStyleProperty<Color>(ButtonBorderTokenName);
            Assert.IsTrue(lightRoot.customStyle.TryGetValue(property, out Color border),
                "theme không khai " + ButtonBorderTokenName + " cho skin sáng — luật USS đọc nó sẽ về màu dự phòng của C#");

            AssertBorderMeetsShapeContrast(border, UxLayoutAuditor.WindowBackground(false), "nền trang #C8C8C8");
            AssertBorderMeetsShapeContrast(border, LightButtonBackground, "nền nút #E4E4E4");
        }

        /// <summary>Một phép đo của ca trên: viền hợp thành lên <paramref name="background"/> rồi so bậc hình khối.</summary>
        private static void AssertBorderMeetsShapeContrast(Color border, Color background, string backgroundName)
        {
            Color seen = UxLayoutAuditor.CompositeOver(border, background);
            float ratio = UxLayoutAuditor.ContrastRatio(seen, background);
            Assert.GreaterOrEqual(ratio, ShapeContrastRatio,
                "viền nút skin sáng trên " + backgroundName + " " + ratio.ToString("0.00", CultureInfo.InvariantCulture)
                + ":1 — cần ≥ " + ShapeContrastRatio.ToString("0.##", CultureInfo.InvariantCulture)
                + ":1 (bậc hình khối, chốt của USER 19/9)");
        }

        /// <summary>Tên token viền nút — khai ở <c>liveops-hub-theme.uss</c>, khối <c>.liveops-hub--skin-light</c>.</summary>
        private const string ButtonBorderTokenName = "--liveops-hub-color-button-border";

        /// <summary>
        /// Nền nút của skin SÁNG (<c>--unity-colors-button-background</c> = #E4E4E4). Viết thành hằng chứ không đọc
        /// <c>customStyle</c>: biến ấy do stylesheet mặc định của Editor khai theo skin ĐANG CHẠY, mà lượt cổng chạy skin tối,
        /// nên đọc ra sẽ là màu nút của skin TỐI — đúng cú pháp mà sai cảnh. Cùng lối với
        /// <c>UxLayoutAuditor.LightWindowBackgroundChannel</c>: màu của skin không chạy thì khai thành hằng có nguồn.
        /// </summary>
        private static readonly Color LightButtonBackground = new Color(228f / 255f, 228f / 255f, 228f / 255f, 1f);

        // ------------------------------------------------------------------ J2-06 dấu phẩy rời khỏi chip token

        /// <summary>
        /// (J2-06) Dấu phẩy của câu Luật lặp phải DÍNH vào chữ trong chip token đứng trước nó.
        /// <para>
        /// Gốc lỗi: <c>.liveops-hub-rule-token</c> có <c>padding: 0 3px</c> — khoảng thở của CHIP — và mảnh chữ kế bắt đầu
        /// bằng dấu phẩy (", neo từ "), nên 3px ấy biến thành khoảng cách của CÂU và người đọc thấy "Mỗi [7 ngày] , neo từ".
        /// </para>
        /// <para>
        /// Đo khe giữa mép phải của VÙNG NỘI DUNG chip (đã trừ padding và viền) và mép trái của nhãn kế, chứ không đo giữa
        /// hai HỘP: khe giữa hai hộp bằng 0 ở cả bản hỏng lẫn bản đúng, nên nó không phân biệt được hai bản.
        /// </para>
        /// <para>
        /// Khoá CẢ HAI CHIỀU, vì bản vá lượt đầu của chính gói này kéo quá tay: mảnh cuối " + số thứ tự." mở đầu bằng KHOẢNG
        /// TRẮNG thật, kéo nó lại thì câu đọc thành "[pass-]+ số thứ tự" — lỗi ngược lại, và chỉ ảnh chụp mới bắt được nó.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator RuleSentence_PunctuationStaysAttachedToToken()
        {
            yield return OpenDesignRecurring();

            VisualElement sentence = _scope.View.Q(className: LiveOpsHubClassNames.RuleSentence);
            Assert.IsNotNull(sentence, "màn Luật lặp thiếu câu token");

            int measuredPunctuationPairs = 0;
            int measuredSpacePairs = 0;
            List<string> wrong = new List<string>();
            for (int index = 0; index + 1 < sentence.childCount; index++)
            {
                Button token = LastTokenOf(sentence[index]);
                Label following = FirstTextOf(sentence[index + 1]);
                if (token == null || following == null || following.text.Length == 0) continue;
                Rect tokenBound = token.worldBound;
                Rect followingBound = following.worldBound;
                if (Mathf.Abs(followingBound.yMin - tokenBound.yMin) > SameLineTolerancePixels) continue;

                float tokenContentRight = tokenBound.xMax - token.resolvedStyle.paddingRight
                    - token.resolvedStyle.borderRightWidth;
                float gap = followingBound.xMin - tokenContentRight;
                string pair = "'" + token.text + "' → '" + following.text + "'";
                if (IsSentencePunctuation(following.text[0]))
                {
                    // Dấu câu THUỘC VỀ chữ trong chip: nó phải dính, không được cách một khoảng thở của chip.
                    measuredPunctuationPairs++;
                    if (gap > MaximumPunctuationGapPixels)
                    {
                        wrong.Add(pair + ": dấu câu cách chip " + gap.ToString("0.##", CultureInfo.InvariantCulture) + "px");
                    }
                    continue;
                }
                // Mảnh mở đầu bằng khoảng trắng thật (" + số thứ tự."): khoảng ấy là khoảng cách CỦA CÂU, kéo lại là
                // dính "[pass-]+ số thứ tự" — lỗi ngược lại, và bản vá J2-06 lượt đầu của gói này đã mắc đúng nó.
                measuredSpacePairs++;
                if (gap < MinimumWordGapPixels)
                {
                    wrong.Add(pair + ": khoảng trắng của câu bị nuốt, còn "
                        + gap.ToString("0.##", CultureInfo.InvariantCulture) + "px");
                }
            }

            Assert.Greater(measuredPunctuationPairs, 0,
                "không đo được cặp chip-token/dấu-câu nào trên một hàng — phép đo hỏng thì ca này thành lời khai suông");
            Assert.Greater(measuredSpacePairs, 0,
                "không đo được cặp chip-token/khoảng-trắng nào — vế 'không kéo quá tay' của ca này chưa đo gì");
            Assert.IsEmpty(wrong, "khoảng cách quanh chip token sai: " + string.Join("; ", wrong.ToArray()));
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------ trợ giúp

        /// <summary>Ký tự mở đầu mảnh chữ kế có phải DẤU CÂU không — dấu câu thì nó thuộc về chữ trong chip đứng trước.</summary>
        private static bool IsSentencePunctuation(char character)
        {
            for (int index = 0; index < SentencePunctuation.Length; index++)
            {
                if (SentencePunctuation[index] == character) return true;
            }
            return false;
        }

        /// <summary>Chip token đứng CUỐI một cụm của câu (cụm = "chữ nối + token"); null khi cụm không kết thúc bằng token.</summary>
        private static Button LastTokenOf(VisualElement group)
        {
            if (group.childCount == 0) return null;
            Button button = group[group.childCount - 1] as Button;
            return button != null && button.ClassListContains(LiveOpsHubClassNames.RuleToken) ? button : null;
        }

        private static Label FirstTextOf(VisualElement group)
        {
            if (group.childCount == 0) return null;
            Label label = group[0] as Label;
            return label != null && label.ClassListContains(LiveOpsHubClassNames.RecurringSentenceText) ? label : null;
        }

        /// <summary>Chữ đọc được của một nút — nút có Label con (nút header mang icon) thì <c>text</c> của nút rỗng.</summary>
        private static string TextOf(Button button)
        {
            if (!string.IsNullOrEmpty(button.text)) return button.text;
            Label label = button.Q<Label>();
            return label == null ? string.Empty : label.text;
        }

        private IEnumerator OpenStaleValidation()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.StaleCheckScenario);
            _scope = SectionTestScope.Open(new ValidationSection(services), WideWidth, WideHeight);
            yield return _scope.WaitForLayout();
        }

        private IEnumerator OpenDesignRecurring()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithConfirmation(new ScriptedLiveOpsHubConfirmationPresenter())
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            _scope = SectionTestScope.Open(new RecurringRulesSection(services), WideWidth, WideHeight);
            yield return _scope.WaitForLayout();
        }
    }
}
