using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Đợt W8-UX, gói G-UX-RECURRING: khoá bảy lỗi UI/UX của màn Luật lặp mà người dùng giả bắt được khi mở hub thật
    /// (UX-21…UX-26, UX-32 trong <c>plan/w8-ux/UX-FIX-PLAN.md</c>). Mỗi test ĐỎ trên code trước đợt — đó là điều kiện để
    /// tin rằng bản sửa thật sự chạm vào lỗi chứ không chỉ chạm vào code.
    /// <para>
    /// Đo bằng <c>resolvedStyle</c> của cửa sổ hub THẬT ở đúng cỡ người dùng mở, và mọi thao tác đi qua sự kiện chuột thật
    /// (<c>EditorWindow.SendEvent</c>): gọi thẳng hàm model thì chính lớp bị lỗi — lớp bố cục — không bao giờ chạy.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class RecurringUxFixTests
    {
        private const string WeeklyPassType = "weekly-pass";
        private const string SkyRaceType = "sky-race";
        private const string PublishedPrefix = "weekly-pass-";
        private const string NewPrefix = "bonus-";

        /// <summary>sky-race chạy 20 giờ mỗi 24 giờ; 30 > 24 nên luật hỏng và game bỏ hẳn luật (UJ-15).</summary>
        private const int LongerThanPeriodHours = 30;

        /// <summary>Chu kỳ của weekly-pass trong mẫu thiết kế — 168 giờ, tức 7 ngày chẵn.</summary>
        private const int WeekPeriodHours = 168;

        /// <summary>Cỡ nhỏ nhất của ma trận kiểm bố cục (mục 2 chốt của user) — chỗ mọi thứ vỡ trước.</summary>
        private const int NarrowWidth = 700;

        private const int NarrowHeight = 560;
        private const int WideWidth = 1440;
        private const int WideHeight = 900;

        /// <summary>Cỡ cửa sổ của ma trận ảnh (9.5) — chỗ ảnh h14e chụp, nên cũng là chỗ phải đo được lối cuộn.</summary>
        private const int CaptureWidth = 1280;

        private const int CaptureHeight = 760;

        /// <summary>Thân màn phải lấp ít nhất 95% chiều cao khung — ngưỡng của kiểm bố cục tự động (mục 3.3 (e)).</summary>
        private const float FillRatio = 0.95f;

        /// <summary>Ô nhập rộng nhất mà form 640 còn đọc được; 2022.3 để mặc định thì ô giãn tới 1183 (UX-21).</summary>
        private const float MaximumFieldInputWidth = 280f;

        /// <summary>
        /// Hạn giờ THẬT của mọi vòng chờ trong fixture này (W10 — bỏ vế đếm khung).
        /// <para>
        /// Vì sao không còn vế "quá 60 khung": số khung không phải thời gian. Máy chạy chậm thì cùng một quãng chờ có ít
        /// khung hơn, nên vế ấy làm hạn giờ DÀI RA đúng lúc cần một câu trả lời dứt khoát — và nó cũng không rút ngắn được
        /// gì khi máy chạy nhanh. Giờ thật một mình là thứ đo được và đọc được (cùng kết luận với W9-16 ở
        /// <c>UxEventSender.WaitUntil</c>).
        /// </para>
        /// </summary>
        private const int MaximumWaitMilliseconds = 5000;

        private SectionTestScope _scope;

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
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        // ------------------------------------------------------------------ UX-21 thân giãn + cỡ hẹp

        /// <summary>
        /// (UX-21) Gốc màn là <c>TemplateContainer</c> do <c>Instantiate()</c> trả về: không ai gắn class giãn cho nó nên
        /// <c>flex-grow</c> của pane con không có gì để giãn theo, và viền pane trái dừng giữa cửa sổ.
        /// </summary>
        [UnityTest]
        public IEnumerator Body_FillsSectionHeight()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);

            float sectionHeight = _scope.Window.SectionBody.resolvedStyle.height;
            Assert.Greater(sectionHeight, 0f, "khung chưa có chiều cao thì phép đo dưới đây vô nghĩa");
            Assert.GreaterOrEqual(_scope.View.resolvedStyle.height, sectionHeight * FillRatio,
                "gốc màn Luật lặp phải lấp chiều cao khung, không dừng giữa cửa sổ");
            VisualElement split = _scope.View.Q(className: LiveOpsHubClassNames.RecurringSplit);
            Assert.IsNotNull(split);
            Assert.GreaterOrEqual(split.resolvedStyle.height, sectionHeight * FillRatio,
                "hai pane phải cao suốt khung — viền phải của pane trái là đường phân cách, không phải một đoạn kẻ ngắn");
        }

        /// <summary>
        /// (UX-21) Ở 700 px: form và bảng đợt phải nằm gọn trong pane, không đẩy ScrollView mọc thanh cuộn ngang. Cột
        /// "Giờ máy" là cột được phép biến mất (cột "Lúc này" thì không — nó nói đợt nào đang chạy).
        /// </summary>
        [UnityTest]
        public IEnumerator Narrow_FormFitsWithoutHorizontalScroll()
        {
            yield return OpenDesignSample(NarrowWidth, NarrowHeight);

            ScrollView scroll = _scope.View.Q<ScrollView>(className: LiveOpsHubClassNames.RecurringFormScroll);
            Assert.IsNotNull(scroll, "màn thiếu ScrollView của pane form");
            float viewportWidth = scroll.contentViewport.resolvedStyle.width;
            Assert.Greater(viewportWidth, 0f);
            Assert.LessOrEqual(scroll.contentContainer.resolvedStyle.width, viewportWidth + 0.5f,
                "nội dung form rộng hơn pane ⇒ người dùng phải quét ngang mới đọc hết một hàng");
            // (RC-06b, soát lượt 3) Phép đo ngay trên chỉ nói "hôm nay nội dung vừa pane"; nó KHÔNG khoá được cách chữa.
            // Bản sửa tắt hẳn thanh cuộn ngang (RecurringRulesSection đặt horizontalScrollerVisibility = Hidden), mà Hidden
            // cũng gỡ luôn LỐI THOÁT cuộn: hôm nào nội dung thật sự rộng hơn viewport thì người dùng không còn cách nào
            // đọc tới — bị cắt câm. Hai điều đó phải đi CÙNG NHAU, nên khoá cả thuộc tính ở đây: ai gỡ Hidden mà quên là
            // thanh thừa quay lại, ai giữ Hidden mà làm nội dung tràn là dòng trên đỏ trước.
            Assert.AreEqual(ScrollerVisibility.Hidden, scroll.horizontalScrollerVisibility,
                "thanh cuộn ngang của pane form phải tắt hẳn — nội dung đã vừa pane thì thanh đó chỉ ăn chiều cao và nói dối");

            VisualElement deviceCell = OccurrenceCell(LiveOpsHubClassNames.RecurringCellDevice);
            Assert.IsNotNull(deviceCell, "bảng đợt kế tiếp thiếu cột Giờ máy");
            Assert.AreEqual(DisplayStyle.None, deviceCell.resolvedStyle.display,
                "dưới ngưỡng hẹp, cột Giờ máy nhường chỗ cho cột Lúc này");
            VisualElement nowCell = OccurrenceCell(LiveOpsHubClassNames.RecurringCellNow);
            Assert.AreNotEqual(DisplayStyle.None, nowCell.resolvedStyle.display, "cột Lúc này không bao giờ bị ẩn");
        }

        /// <summary>
        /// (UX-21, R-07) Mở foldout JSON ở 1280×760 thì thân form CAO HƠN khung — đó là chuyện bình thường, nhưng luật của
        /// đợt là "vượt khung thì phải CUỘN ĐƯỢC tới": dòng lỗi JSON và hàng nút "Áp"/"Copy" nằm dưới mép vùng cuộn, cuộn
        /// tới phải thấy TRỌN, không bị cắt ngang thân chữ. Ảnh h14e chụp đúng cỡ này nên kịch bản chụp cũng cuộn như đây.
        /// </summary>
        [UnityTest]
        public IEnumerator JsonFoldoutOpen_ScrollReachesErrorAndButtons()
        {
            yield return OpenDesignSample(CaptureWidth, CaptureHeight);

            Section.Form.JsonFoldout.value = true;
            Section.Form.JsonFoldout.Editor.value = BrokenJson;
            yield return _scope.WaitForLayout();

            ScrollView scroll = _scope.View.Q<ScrollView>(className: LiveOpsHubClassNames.RecurringFormScroll);
            Assert.IsNotNull(scroll, "màn thiếu ScrollView của pane form");
            float viewportHeight = scroll.contentViewport.resolvedStyle.height;
            Assert.Greater(scroll.contentContainer.resolvedStyle.height, viewportHeight,
                "foldout JSON mở ở cỡ này phải làm thân cao hơn khung — nếu không, test này không đo đúng ca");

            VisualElement apply = _scope.View.Q(RecurringRuleJsonFoldout.ApplyElementName);
            Label error = _scope.View.Q<Label>(RecurringRuleJsonFoldout.ErrorElementName);
            Assert.IsNotNull(apply, "foldout thiếu nút Áp");
            Assert.IsNotNull(error, "foldout thiếu dòng lỗi");
            Assert.Greater(error.text.Length, 0, "JSON hỏng thì dòng lỗi phải có chữ");

            scroll.ScrollTo(apply);
            yield return _scope.WaitForLayout();

            AssertInsideViewport(scroll, error, "dòng lỗi JSON");
            AssertInsideViewport(scroll, apply, "nút Áp");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>JSON thiếu dấu phẩy cuối dòng 3 — cùng chữ với kịch bản ảnh h14e.</summary>
        private const string BrokenJson = "{\n  \"type\": \"weekly-pass\",\n  \"idPrefix\": \"s\"\n  \"periodHours\": 168,\n"
            + "  \"activeHours\": 168,\n  \"configKey\": \"weekly_pass_s3\"\n}";

        private static void AssertInsideViewport(ScrollView scroll, VisualElement element, string what)
        {
            Rect viewport = scroll.contentViewport.worldBound;
            Rect bounds = element.worldBound;
            Assert.GreaterOrEqual(bounds.yMin, viewport.yMin - 0.5f, what + " bị cắt ở mép TRÊN vùng cuộn");
            Assert.LessOrEqual(bounds.yMax, viewport.yMax + 0.5f, what + " bị cắt ở mép DƯỚI vùng cuộn — cuộn tới rồi vẫn "
                + "chỉ thấy nửa thân chữ");
        }

        /// <summary>
        /// (UX-21) Ở 2022.3, <c>TextField</c>/<c>DropdownField</c> mặc định <c>flex-grow: 1</c> nên ô kéo dài hết pane
        /// (đo được 1183 px trong ảnh hành trình) — form "max-width 640" không còn nghĩa gì.
        /// </summary>
        [UnityTest]
        public IEnumerator Wide_FieldInputsKeepReadableWidth()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);

            AssertInputNotStretched(Section.Form.PrefixField);
            AssertInputNotStretched(Section.Form.PresetField);
            AssertInputNotStretched(Section.Form.AnchorField);
        }

        /// <summary>
        /// (UX-21) Tên luật là DANH TÍNH của hàng: pane hẹp thì meta ("mỗi 7 ngày · chạy 7 ngày") phải co trước, còn tên
        /// không được co thành "weekly-pa…". <c>flex-shrink</c> tính theo flex-basis nên chỉ đổi tỉ lệ là chưa đủ.
        /// </summary>
        [UnityTest]
        public IEnumerator ListRow_NameDoesNotShrink_MetaDoes()
        {
            yield return OpenDesignSample(NarrowWidth, NarrowHeight);

            Label name = _scope.View.Q<Label>(className: LiveOpsHubClassNames.RecurringListName);
            Label meta = _scope.View.Q<Label>(className: LiveOpsHubClassNames.RecurringListMeta);
            Assert.IsNotNull(name);
            Assert.IsNotNull(meta);
            Assert.AreEqual(0f, name.resolvedStyle.flexShrink, "tên luật không được co — người dùng tìm hàng theo nó");
            Assert.Greater(meta.resolvedStyle.flexShrink, 0f, "meta là thứ được phép cắt");
            Assert.GreaterOrEqual(name.resolvedStyle.width, name.MeasureTextSize(name.text, 0f, VisualElement.MeasureMode.Undefined,
                0f, VisualElement.MeasureMode.Undefined).x - 0.5f, "tên luật của mẫu thiết kế phải hiện đủ, không ellipsis");
        }

        // ------------------------------------------------------------------ UX-22 field lỗi

        /// <summary>
        /// (UX-22, UJ-15) "Chạy mỗi đợt" dài hơn chu kỳ = luật game sẽ BỎ HẲN. Không có gì để "ghi", nên hàng nút nháp và
        /// câu hậu quả "sẽ biến mất" là lời mời làm một việc không làm được — chỉ còn lỗi dưới ô và lối ra "Huỷ (Esc)".
        /// </summary>
        [UnityTest]
        public IEnumerator RunLongerThanPeriod_ShowsFieldErrorOnly_NoWriteButton()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);
            yield return SelectRule(SkyRaceType);

            Section.Form.ActiveField.value = LongerThanPeriodHours;
            yield return null;

            AssertNotVisible(RecurringRuleForm.DraftWriteElementName, "nút ghi giá trị hỏng");
            AssertNotVisible(RecurringRuleForm.DraftHelpBoxElementName, "câu hậu quả của một giá trị không ghi được");
            AssertVisible(RecurringRuleForm.DraftCancelElementName, "lối ra Huỷ (Esc)");
            Label error = ErrorLabelOf(Section.Form.ActiveField);
            Assert.IsNotNull(error, "ô 'Chạy mỗi đợt' thiếu dòng lỗi");
            Assert.AreEqual(LiveOpsHubStrings.RecurringActiveLongerThanPeriodError, error.text);
            Assert.AreNotEqual(DisplayStyle.None, error.resolvedStyle.display, "dòng lỗi phải hiện");
            Assert.GreaterOrEqual(error.worldBound.xMin, Section.Form.ActiveField.worldBound.xMin - 0.5f,
                "câu lỗi thụt theo cột ô, không nằm dưới cột nhãn");

            // Ô này mang CẢ HAI class: --invalid (giá trị hỏng) và --drafting (chưa ghi). Viền phải là viền CHẶN, không
            // phải viền "đang nhập" — hai luật một class cùng độ đặc hiệu thì luật đứng sau thắng, và đó là lỗi cũ.
            Assert.IsTrue(Section.Form.ActiveField.ClassListContains(LiveOpsHubClassNames.RecurringFieldInvalid),
                "ô giữ giá trị hỏng phải mang class --invalid");
            Assert.IsTrue(Section.Form.ActiveField.ClassListContains(LiveOpsHubClassNames.RecurringFieldDrafting),
                "…và vẫn đang giữ nháp, nếu không test này không đo đúng ca UX-22");
            yield return AssertInvalidBorderWinsOverDrafting(Section.Form.ActiveField);
            LogAssert.NoUnexpectedReceived();
        }


        /// <summary>(UX-22) "0 đợt kế tiếp" mà vẫn mời "Thêm 5": thêm 5 lần nữa của một luật game sẽ bỏ vẫn là 0 đợt.</summary>
        [UnityTest]
        public IEnumerator ZeroOccurrences_HidesAddMoreButton()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);
            yield return SelectRule(SkyRaceType);

            Section.Form.ActiveField.value = LongerThanPeriodHours;
            yield return WaitForOccurrenceRows(0);
            yield return _scope.WaitForLayout();

            AssertNotVisible(RecurringNextOccurrencesTable.AddMoreElementName, "nút Thêm 5 khi bảng trống vì luật hỏng");
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------ UX-23 câu khoá

        /// <summary>
        /// (UX-23, V16) Cùng một câu "Khoá trong lúc ô Tiền tố id còn nháp chưa ghi" in ba lần đọc như ba lỗi khác nhau.
        /// Một câu chung cho cả nhóm ô bị khoá là đủ — và nó phải nói ra lối đi tiếp, không chỉ nói "khoá".
        /// </summary>
        [UnityTest]
        public IEnumerator PrefixDraft_ShowsLockSentenceOnce()
        {
            yield return OpenPublishedPrefixSample(WideWidth, WideHeight);

            yield return TypeNewPrefixAndWaitForDraft();

            int visibleLockSentences = 0;
            _scope.View.Query<Label>(className: LiveOpsHubClassNames.RecurringFieldLockReason).ForEach(label =>
            {
                if (label.text.Length > 0 && label.resolvedStyle.display != DisplayStyle.None) visibleLockSentences++;
            });
            Assert.AreEqual(1, visibleLockSentences, "đúng MỘT câu khoá cho cả nhóm ô, không một câu mỗi ô");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (UX-23, UJ-21) Bấm vào một ô bị khoá hiện không phản hồi gì — người dùng bấm lại vài lần rồi bỏ. Bấm phải đưa
        /// họ tới đúng LỐI RA: focus nút "Huỷ (Esc)" và nháy chính nút đó. Cổng hành trình của gói G khoá cùng hành vi
        /// (<c>UxRecurringJourneyTests</c>), nên hai gói phải nói một câu.
        /// </summary>
        [UnityTest]
        public IEnumerator PrefixDraft_ClickLockedField_FocusesCancelAndFlashesIt()
        {
            yield return OpenPublishedPrefixSample(WideWidth, WideHeight);

            yield return TypeNewPrefixAndWaitForDraft();
            yield return ClickAt(Section.Form.PeriodField);

            Button cancel = _scope.View.Q<Button>(RecurringRuleForm.DraftCancelElementName);
            Assert.IsNotNull(cancel, "khối nháp thiếu nút Huỷ (Esc)");
            // Class chớp được thêm ở KHUNG SAU khi gỡ (không thì transition không chạy lại ở lần bấm thứ hai), nên chờ
            // nó xuất hiện thay vì đọc ngay; nó chỉ sống 300ms nên phải đo trước khi chờ focus.
            yield return WaitForClass(cancel, LiveOpsHubClassNames.RowFlash);
            Assert.IsTrue(cancel.ClassListContains(LiveOpsHubClassNames.RowFlash),
                "bấm ô bị khoá phải nháy nút Huỷ để chỉ đường ra");
            yield return WaitForFocusInside(cancel);
            Assert.IsTrue(IsFocusInside(cancel), "…và đưa con trỏ tới chính nút Huỷ, chỗ người dùng đang thiếu");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (UJ-21) Bấm ô khoá lần thứ hai cũng phải nháy: gỡ rồi thêm lại class trong cùng một khung thì computed style
        /// không đổi và transition không chạy lại — người dùng bấm tiếp và màn im.
        /// </summary>
        [UnityTest]
        public IEnumerator PrefixDraft_ClickLockedFieldTwice_FlashesBothTimes()
        {
            yield return OpenPublishedPrefixSample(WideWidth, WideHeight);

            yield return TypeNewPrefixAndWaitForDraft();
            Button cancel = _scope.View.Q<Button>(RecurringRuleForm.DraftCancelElementName);
            Assert.IsNotNull(cancel, "khối nháp thiếu nút Huỷ (Esc)");

            yield return ClickAt(Section.Form.PeriodField);
            yield return WaitForClass(cancel, LiveOpsHubClassNames.RowFlash);
            Assert.IsTrue(cancel.ClassListContains(LiveOpsHubClassNames.RowFlash), "lần bấm thứ nhất phải nháy");

            // Lần hai bấm NGAY trong lúc nháy thứ nhất còn sống — đó mới là ca UJ-21 (bấm liên tiếp vào ô chết).
            SendClick(Section.Form.ActiveField);
            Assert.IsFalse(cancel.ClassListContains(LiveOpsHubClassNames.RowFlash),
                "class chớp phải RỜI cây ngay trong khung bấm: gỡ rồi thêm lại trong cùng một khung thì computed style "
                + "không đổi và transition không chạy lại");
            yield return WaitForClass(cancel, LiveOpsHubClassNames.RowFlash);
            Assert.IsTrue(cancel.ClassListContains(LiveOpsHubClassNames.RowFlash),
                "…và quay lại ở khung sau: lần bấm thứ hai cũng nháy");
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------ UX-24 nút Thêm luật

        /// <summary>
        /// (UX-24, UJ-09) <c>new Button { text = … }</c> rồi <c>Insert(0, Image)</c>: chữ do chính TextElement của Button vẽ
        /// nên icon đè lên giữa chữ ("Th+m luật"). Màn Loại event đã làm đúng — Image + Label là hai con riêng.
        /// </summary>
        [UnityTest]
        public IEnumerator AddRuleButton_DrawsIconAndTextSideBySide()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);

            Button addRule = _scope.Window.rootVisualElement.Q<Button>(RecurringRulesSection.AddRuleButtonElementName);
            Assert.IsNotNull(addRule, "header của màn thiếu nút Thêm luật");
            Assert.IsTrue(string.IsNullOrEmpty(addRule.text), "Button không được giữ text: chữ của Button vẽ dưới mọi con");
            Label label = addRule.Q<Label>();
            Assert.IsNotNull(label, "chữ của nút phải là một Label con");
            Assert.AreEqual(LiveOpsHubStrings.RecurringAddRuleButton, label.text);
            Image icon = addRule.Q<Image>();
            Assert.IsNotNull(icon, "nút thiếu icon");
            yield return LiveOpsHubWindowTestScope.WaitForLayout(label);
            Assert.GreaterOrEqual(label.worldBound.xMin, icon.worldBound.xMax - 0.5f, "icon và chữ không được chồng nhau");
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------ UX-25 dấu trên hàng chọn + nhãn tiếng Anh

        /// <summary>
        /// (UX-25, V15) Hàng đang chọn có nền highlight; thoi Warning giữ nguyên nền warning-fill nên tương phản tụt còn
        /// 1,53:1 ở skin sáng — dấu "luật này có vấn đề" biến mất đúng lúc người dùng đang nhìn nó.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectedRowMark_UsesOnSelectionColor()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);

            Button row = Section.List.FindRow(WeeklyPassType);
            Assert.IsNotNull(row);
            Assert.IsTrue(row.ClassListContains(LiveOpsHubClassNames.RecurringListRowSelected), "weekly-pass là hàng đang chọn");
            LiveOpsStateMark mark = row.Q<LiveOpsStateMark>();
            Assert.IsNotNull(mark, "hàng có câu 'sau khi ghi' còn treo thì phải có dấu mức (UX-32)");
            Color background = mark.resolvedStyle.backgroundColor;
            Assert.AreEqual(1f, background.r, 0.02f, "dấu trên hàng đang chọn dùng màu on-selection");
            Assert.AreEqual(1f, background.g, 0.02f);
            Assert.AreEqual(1f, background.b, 0.02f);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (UX-25) Đổi MÀU dấu trên hàng đang chọn không được xoá HÌNH của nó: dấu Blocked là tròn đặc + vạch cắt, mà
        /// vạch cắt vẽ bằng đúng màu on-selection vừa gán cho nền — nếu không chữa, trên hàng chọn nó tàng hình và dấu
        /// Blocked chỉ còn khác dấu Ok ở cỡ.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectedRowBlockedMark_KeepsItsBar()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);

            Button row = Section.List.FindRow(WeeklyPassType);
            Assert.IsNotNull(row);
            Assert.IsTrue(row.ClassListContains(LiveOpsHubClassNames.RecurringListRowSelected), "weekly-pass là hàng đang chọn");

            // Dấu Blocked dựng NGAY TRONG hàng đang chọn: mẫu thiết kế không có luật nào bị chặn, mà thứ phải đo ở đây là
            // luật USS của hàng chọn chứ không phải dữ liệu mẫu.
            LiveOpsStateMark blockedMark = new LiveOpsStateMark();
            blockedMark.SetHealth(HealthState.Blocked);
            row.Add(blockedMark);
            yield return LiveOpsHubWindowTestScope.WaitForLayout(blockedMark);

            Color face = blockedMark.resolvedStyle.backgroundColor;
            Color bar = blockedMark.Bar.resolvedStyle.backgroundColor;
            Assert.AreNotEqual(DisplayStyle.None, blockedMark.Bar.resolvedStyle.display, "dấu Blocked phải còn con vạch cắt");
            Assert.Greater(blockedMark.Bar.resolvedStyle.width, 0f, "vạch cắt phải có bề ngang thật");
            blockedMark.RemoveFromHierarchy();

            bool sameColor = Mathf.Abs(face.r - bar.r) < 0.01f && Mathf.Abs(face.g - bar.g) < 0.01f
                && Mathf.Abs(face.b - bar.b) < 0.01f && Mathf.Abs(face.a - bar.a) < 0.01f;
            Assert.IsFalse(sameColor, "vạch cắt trùng màu nền ⇒ dấu Blocked thành chấm tròn đặc, mất hình 'biển cấm'");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (R-06) Dấu Warning của câu "sau khi ghi" là chuyện của HÀNG, không phải của tập phát hiện: F8 / Shift F8 đi qua
        /// các luật mà Kiểm lịch CÓ phát hiện. Gộp hai thứ lại thì F8 dừng ở một hàng mà màn Kiểm lịch không hề nhắc tới.
        /// </summary>
        [UnityTest]
        public IEnumerator FindingNavigation_SkipsRuleThatOnlyHasAfterWriteNotice()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);
            yield return SelectRule(SkyRaceType);

            Assert.IsTrue(RecurringRuleModel.HasAfterWriteNotice(Section.Services.Session, WeeklyPassType),
                "mẫu thiết kế phải còn câu 'sau khi ghi' ở weekly-pass — nếu không, test này đo nhầm thứ");
            Assert.IsFalse(HasRecurringFinding(WeeklyPassType),
                "…và Kiểm lịch KHÔNG có phát hiện nào cho weekly-pass, đó mới là ca R-06");

            Assert.IsFalse(Section.TryMoveToFinding(1), "F8 không có chỗ để tới: màn không có luật nào mang phát hiện thật");
            Assert.AreEqual(SkyRaceType, Section.SelectedEventType, "…nên luật đang chọn phải giữ nguyên");
            LogAssert.NoUnexpectedReceived();
        }

        private bool HasRecurringFinding(string eventType)
        {
            LiveEventCalendarCheckReport report = Section.Services.Session.Check.LastReport;
            if (report == null) return false;
            IReadOnlyList<LiveEventCalendarFinding> findings = report.Findings;
            for (int index = 0; index < findings.Count; index++)
            {
                LiveEventCalendarFinding finding = findings[index];
                if (finding.IsIgnored || !LiveOpsHubFindingRouting.BelongsTo(finding, LiveOpsHubHealthTarget.RecurringRules)) continue;
                if (string.Equals(finding.TargetId, eventType, System.StringComparison.Ordinal)) return true;
            }
            return false;
        }

        /// <summary>
        /// (UX-25, UJ-19) Nhãn tiếng Anh "Run per occurrence (hours)" đo 143 px trong cột nhãn 96 px: hoặc bị cắt, hoặc đè
        /// lên ô số. Nhãn phải xuống dòng và không bao giờ chạm ô (UX-32 cũng nói chính chỗ này).
        /// </summary>
        [UnityTest]
        public IEnumerator EnglishFieldLabels_WrapAndNeverTouchInput()
        {
            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.English))
            {
                yield return OpenDesignSample(WideWidth, WideHeight);

                Label label = LabelOf(Section.Form.ActiveField);
                Assert.IsNotNull(label, "hàng 'Run per occurrence (hours)' thiếu nhãn");
                Assert.AreEqual(WhiteSpace.Normal, label.resolvedStyle.whiteSpace, "nhãn phải được xuống dòng, không bị cắt");
                Vector2 measured = label.MeasureTextSize(label.text, label.resolvedStyle.width,
                    VisualElement.MeasureMode.Exactly, 0f, VisualElement.MeasureMode.Undefined);
                Assert.GreaterOrEqual(label.resolvedStyle.height, measured.y - 0.5f, "nhãn phải cao đủ cho số dòng nó cần");
                Assert.LessOrEqual(label.worldBound.xMax, Section.Form.ActiveField.worldBound.xMin + 0.5f,
                    "nhãn không được chạm hay đè lên ô số");
                LogAssert.NoUnexpectedReceived();
            }
        }

        // ------------------------------------------------------------------ UX-26 toast nêu trường + giá trị

        /// <summary>
        /// (UX-26, UJ-22) Toast "Đổi luật lặp sky-race" không nói đổi gì, mà chính nó là tên bước Hoàn tác: người dùng đọc
        /// status bar vài phút sau không biết ⌘Z sẽ trả lại cái gì.
        /// </summary>
        [UnityTest]
        public IEnumerator EditToast_NamesFieldAndValues()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);
            yield return SelectRule(SkyRaceType);
            LiveOpsToastModel toast = null;
            Section.Services.Bus.ToastRequested += model => toast = model;

            Section.Form.ActiveField.value = SkyRaceLongerActiveHours;
            yield return null;

            Assert.IsNotNull(toast, "mọi lệnh ghi thẳng đều có toast");
            StringAssert.Contains(LiveOpsHubStrings.RecurringActiveHoursLabel, toast.Message, "toast phải nêu TRƯỜNG vừa đổi");
            StringAssert.Contains(SkyRaceType, toast.Message, "…và luật nào");
            StringAssert.Contains(SkyRaceLongerActiveHours.ToString(CultureInfo.InvariantCulture), toast.Message,
                "…và giá trị mới");
            LogAssert.NoUnexpectedReceived();
        }

        private const int SkyRaceLongerActiveHours = 22;

        // ------------------------------------------------------------------ UX-32 chi tiết chữ

        /// <summary>(UX-32, T1) Label của UI Toolkit có padding mặc định nên câu token đọc thành "7 ngày , neo từ …".</summary>
        [UnityTest]
        public IEnumerator SentenceText_HasNoPaddingAroundWords()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);

            VisualElement sentence = _scope.View.Q(RecurringRuleForm.SentenceElementName);
            Assert.IsNotNull(sentence);
            int checkedLabels = 0;
            sentence.Query<Label>().ForEach(label =>
            {
                checkedLabels++;
                Assert.AreEqual(0f, label.resolvedStyle.paddingLeft, 0.01f, "chữ nối của câu không được có padding trái");
                Assert.AreEqual(0f, label.resolvedStyle.paddingRight, 0.01f, "…hay padding phải: nó thành cách trước dấu phẩy");
                Assert.AreEqual(0f, label.resolvedStyle.marginLeft, 0.01f);
                Assert.AreEqual(0f, label.resolvedStyle.marginRight, 0.01f);
            });
            Assert.Greater(checkedLabels, 0, "câu đọc phải có ít nhất một Label chữ nối");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (UX-32, T4) Giờ máy của ô Neo đứng cùng hàng với ô nhập nên hàng dài gấp đôi các hàng khác và ở 820 nó bị cắt.
        /// Nó là dòng PHỤ của ô, đúng chỗ là một dòng riêng dưới ô — thẳng cột với khối nháp.
        /// </summary>
        [UnityTest]
        public IEnumerator AnchorDeviceTime_SitsOnItsOwnLine()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);

            Label suffix = SuffixOf(Section.Form.AnchorField);
            Assert.IsNotNull(suffix);
            Assert.AreEqual(DisplayStyle.None, suffix.resolvedStyle.display, "hàng ô Neo không còn giữ chữ phụ giờ máy");
            string deviceText = AnchorDeviceLineText();
            Assert.Greater(deviceText.Length, 0, "mẫu thiết kế phải có dòng giờ máy — nếu không, test này đo nhầm thứ");
            Label deviceLine = DeviceLineOf(Section.Form.AnchorField, deviceText);
            Assert.IsNotNull(deviceLine, "ô Neo thiếu dòng phụ giờ máy nằm ngoài hàng ô");
            Assert.GreaterOrEqual(deviceLine.worldBound.yMin, Section.Form.AnchorField.worldBound.yMax - 0.5f,
                "dòng giờ máy nằm DƯỚI ô, không cùng hàng với ô");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>(UX-32, T5) "■Đang chạy": dấu giai đoạn dính chữ vì dấu chỉ có margin 1 px.</summary>
        [UnityTest]
        public IEnumerator NowCellMark_HasGapBeforeText()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);

            VisualElement nowCell = OccurrenceCell(LiveOpsHubClassNames.RecurringCellNow);
            Assert.IsNotNull(nowCell);
            LiveOpsStateMark mark = nowCell.Q<LiveOpsStateMark>();
            Assert.IsNotNull(mark, "cột Lúc này thiếu dấu giai đoạn");
            Assert.GreaterOrEqual(mark.resolvedStyle.marginRight, 4f, "dấu phải cách chữ ít nhất 4 px");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (UX-32, T6) Cùng một luật mà hai màn đọc nhịp bằng hai đơn vị thì người dùng phải tự quy đổi mới tin hai chỗ
        /// nói cùng một thứ. Chuỗi nhịp trên màn này phải TRÙNG chuỗi header làn của màn Lịch — weekly-pass 168 giờ đọc
        /// "7 ngày" ở cả hai chỗ (<c>TimelineModelTests</c> khoá "lặp mỗi 7 ngày · chạy 7 ngày").
        /// </summary>
        [UnityTest]
        public IEnumerator PeriodRhythm_ReadsSameUnitAsLaneHeader()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);

            Button periodToken = _scope.View.Q<Button>(RecurringRuleSentence.TokenElementName(RecurringRuleFields.PeriodHours));
            Assert.IsNotNull(periodToken, "câu đọc thiếu token chu kỳ");
            string laneUnit = RecurringRuleModel.PeriodText(WeekPeriodHours, Section.Services.Format);
            Assert.AreEqual("7 ngày", laneUnit, "168 giờ chẵn ngày và dài hơn một ngày ⇒ đọc theo ngày, y như header làn");
            Assert.AreEqual(laneUnit, periodToken.text, "token nhịp phải đọc đúng chuỗi mà header làn của màn Lịch in ra");
            Label suffix = SuffixOf(Section.Form.PeriodField);
            Assert.IsNotNull(suffix);
            StringAssert.Contains("7", suffix.text, "ô Chu kỳ vẫn in số giờ thô nên chữ phụ '= 7 ngày' phải còn");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (UX-32, T9) Ghi xong, HelpBox "vẫn còn weekly-pass-35 đang chạy" ở lại dưới ô nhưng hàng luật trong pane trái mất
        /// dấu Warning — pane trái nói "không sao", form nói "có chuyện", cùng một luật.
        /// </summary>
        [UnityTest]
        public IEnumerator AfterWriteNotice_KeepsWarningMarkOnListRow()
        {
            yield return OpenDesignSample(WideWidth, WideHeight);

            VisualElement afterWrite = _scope.View.Q(RecurringRuleForm.AfterWriteElementName);
            Assert.IsNotNull(afterWrite);
            Assert.AreNotEqual(DisplayStyle.None, afterWrite.resolvedStyle.display,
                "mẫu thiết kế có câu 'sau khi ghi' của weekly-pass — nếu không, test này đo nhầm thứ");
            Button row = Section.List.FindRow(WeeklyPassType);
            LiveOpsStateMark mark = row.Q<LiveOpsStateMark>();
            Assert.IsNotNull(mark, "hàng luật phải mang dấu mức trong lúc câu cảnh báo còn treo");
            Assert.IsTrue(mark.ClassListContains(LiveOpsHubClassNames.StateMarkWarning), "và dấu đó là họ Warning");
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------ hạ tầng

        private RecurringRulesSection Section => (RecurringRulesSection)_scope.Section;

        /// <summary>
        /// Ô của một HÀNG trong bảng đợt, không phải ô cùng class ở hàng tiêu đề: tiêu đề đứng trước trong cây nên
        /// <c>Q</c> từ gốc màn luôn trả về nó, và mọi phép đo sau đó nói về sai thứ.
        /// </summary>
        private VisualElement OccurrenceCell(string className)
        {
            VisualElement rows = _scope.View.Q(RecurringNextOccurrencesTable.RowsElementName);
            Assert.IsNotNull(rows, "bảng đợt kế tiếp thiếu khối hàng");
            Assert.Greater(rows.childCount, 0, "mẫu thiết kế phải có ít nhất một đợt kế tiếp");
            return rows[0].Q(className: className);
        }

        private void AssertInputNotStretched(VisualElement input)
        {
            Assert.LessOrEqual(input.resolvedStyle.width, MaximumFieldInputWidth,
                "ô '" + input.name + "' giãn hết pane — form 'max-width 640' không còn nghĩa gì");
        }

        private void AssertVisible(string elementName, string what)
        {
            VisualElement element = _scope.View.Q(elementName);
            Assert.IsNotNull(element, what + " phải có mặt");
            Assert.AreNotEqual(DisplayStyle.None, element.resolvedStyle.display, what + " phải hiện");
        }

        private void AssertNotVisible(string elementName, string what)
        {
            VisualElement element = _scope.View.Q(elementName);
            if (element == null) return;
            Assert.AreEqual(DisplayStyle.None, element.resolvedStyle.display, what + " không được hiện");
        }

        private static Label LabelOf(VisualElement input)
        {
            VisualElement row = input.parent;
            return row == null ? null : row.Q<Label>(className: LiveOpsHubClassNames.RecurringFieldLabel);
        }

        private static Label SuffixOf(VisualElement input)
        {
            VisualElement row = input.parent;
            return row == null ? null : row.Q<Label>(className: LiveOpsHubClassNames.RecurringFieldSuffix);
        }

        /// <summary>
        /// Dòng phụ giờ máy tìm theo CHỮ chứ không theo class: test phải biên dịch được trên code trước đợt, mà ở đó dòng
        /// này chưa tồn tại như một phần tử riêng — nó còn là chữ phụ nằm trong chính hàng ô.
        /// </summary>
        private Label DeviceLineOf(VisualElement input, string expectedText)
        {
            VisualElement group = input.parent != null ? input.parent.parent : null;
            if (group == null || expectedText.Length == 0) return null;
            Label found = null;
            group.Query<Label>().ForEach(label =>
            {
                if (found == null && string.Equals(label.text, expectedText, System.StringComparison.Ordinal)
                    && !IsInsideFieldRow(label)) found = label;
            });
            return found;
        }

        private static bool IsInsideFieldRow(VisualElement element)
        {
            for (VisualElement walk = element; walk != null; walk = walk.parent)
            {
                if (walk.ClassListContains(LiveOpsHubClassNames.RecurringFieldRow)) return true;
            }
            return false;
        }

        /// <summary>Dòng lỗi của ô nhận ra bằng class chữ-chặn; câu lý do khoá dùng chữ-yên nên không lẫn.</summary>
        private static Label ErrorLabelOf(VisualElement input)
        {
            VisualElement group = input.parent != null ? input.parent.parent : null;
            return group == null ? null : group.Q<Label>(className: LiveOpsHubClassNames.TextBlocked);
        }

        private string AnchorDeviceLineText()
        {
            RecurringRuleModel model = RecurringRuleModel.Build(Section.Services.Session, Section.SelectedEventType,
                RecurringPrefixDraft.None, RecurringRuleModel.DefaultOccurrenceCount, Section.Services.Format);
            return model.AnchorDeviceLine;
        }

        /// <summary>
        /// Focus không phải lúc nào cũng xong trong khung gửi sự kiện: panel xử lý focus của chính cú bấm trước, phần đặt
        /// lại focus chạy ở lượt sau, và cửa sổ vừa mở có thể nhận focus muộn một khung. Chờ theo V-23 rồi mới kết luận.
        /// </summary>
        private IEnumerator WaitForFocusInside(VisualElement field)
        {
            yield return WaitForCondition(field, () => IsFocusInside(field));
        }

        /// <summary>
        /// Vòng chờ CHUNG của fixture: điều kiện + hạn giờ thật, và mỗi vòng chạy một nhịp panel
        /// (<see cref="UxEventSender.PumpPanelScheduler"/>) cùng một nhịp <c>delayCall</c>.
        /// <para>
        /// Vì sao phải tự cầm nhịp panel: hai ca <c>PrefixDraft_ClickLockedField*</c> đỏ trên 2022.3 khi có Unity khác chạy
        /// song song, xanh 21/21 khi chạy riêng. Gốc là NHỊP VẼ, không phải hành vi — nút Huỷ nháy bằng hai lượt schedule
        /// (thêm class, rồi +300 ms gỡ class) mà panel chỉ chạy hàng schedule lúc cửa sổ được vẽ; máy bận thì hai lượt ấy
        /// lọt gọn vào một nhịp, giữa hai lần test nhìn vào cây. Vòng chờ này chạy đúng một nhịp panel trước mỗi lần đọc,
        /// nên lượt THÊM class luôn rơi vào một vòng quan sát.
        /// </para>
        /// <para>
        /// Hết hạn thì TRẢ VỀ chứ không fail ở đây: câu assert của chính ca mới biết nó đang chờ cái gì và nói được câu
        /// người đọc hiểu.
        /// </para>
        /// </summary>
        private static IEnumerator WaitForCondition(VisualElement element, Func<bool> condition)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                if (stopwatch.ElapsedMilliseconds > MaximumWaitMilliseconds) yield break;
                UxEventSender.PumpPanelScheduler(element);
                UxEventSender.PumpDelayCalls();
                yield return null;
            }
        }

        private bool IsFocusInside(VisualElement field)
        {
            VisualElement focused = _scope.View.panel.focusController.focusedElement as VisualElement;
            while (focused != null)
            {
                if (focused == field) return true;
                focused = focused.parent;
            }
            return false;
        }

        /// <summary>
        /// So màu viền thật của ô với hai ô MẪU dựng ngay trong màn (cùng bộ stylesheet, cùng skin): một ô chỉ --invalid,
        /// một ô chỉ --drafting. Đo theo mẫu chứ không theo mã màu chép tay để test còn đúng ở cả skin tối lẫn sáng.
        /// </summary>
        private IEnumerator AssertInvalidBorderWinsOverDrafting(VisualElement field)
        {
            VisualElement invalidProbe = Probe(LiveOpsHubClassNames.RecurringFieldInvalid);
            VisualElement draftingProbe = Probe(LiveOpsHubClassNames.RecurringFieldDrafting);
            _scope.View.Add(invalidProbe);
            _scope.View.Add(draftingProbe);
            yield return LiveOpsHubWindowTestScope.WaitForLayout(invalidProbe);

            Color blocked = invalidProbe.resolvedStyle.borderTopColor;
            Color warning = draftingProbe.resolvedStyle.borderTopColor;
            Color actual = field.resolvedStyle.borderTopColor;
            invalidProbe.RemoveFromHierarchy();
            draftingProbe.RemoveFromHierarchy();

            Assert.AreNotEqual(blocked, warning, "hai ô mẫu phải khác màu, nếu không phép so này vô nghĩa");
            AssertSameColor(blocked, actual, "viền của ô giữ giá trị hỏng phải là viền CHẶN, không phải viền 'đang nhập'");
        }

        private static VisualElement Probe(string stateClassName)
        {
            VisualElement probe = new VisualElement();
            probe.AddToClassList(LiveOpsHubClassNames.RecurringFieldInput);
            probe.AddToClassList(stateClassName);
            return probe;
        }

        private static void AssertSameColor(Color expected, Color actual, string message)
        {
            Assert.AreEqual(expected.r, actual.r, 0.01f, message);
            Assert.AreEqual(expected.g, actual.g, 0.01f, message);
            Assert.AreEqual(expected.b, actual.b, 0.01f, message);
            Assert.AreEqual(expected.a, actual.a, 0.01f, message);
        }

        /// <summary>Chờ class xuất hiện trên phần tử (V-23: quá CẢ 60 khung LẪN 5 giây mới thôi).</summary>
        private static IEnumerator WaitForClass(VisualElement element, string className)
        {
            yield return WaitForClassState(element, className, true);
        }

        private static IEnumerator WaitForClassState(VisualElement element, string className, bool expected)
        {
            yield return WaitForCondition(element, () => element.ClassListContains(className) == expected);
        }

        /// <summary>
        /// Gửi cú bấm rồi TRẢ NGAY, không nhả khung nào: nơi gọi cần đọc trạng thái ĐÚNG TRONG khung bấm (class chớp phải
        /// rời cây ở đây). Phần tử phải đã có bố cục — dùng sau một <see cref="ClickAt"/> hoặc một lần chờ bố cục.
        /// </summary>
        private void SendClick(VisualElement element)
        {
            Vector2 center = element.worldBound.center;
            _scope.Window.SendEvent(new Event { type = EventType.MouseDown, mousePosition = center, button = 0, clickCount = 1 });
            _scope.Window.SendEvent(new Event { type = EventType.MouseUp, mousePosition = center, button = 0, clickCount = 1 });
        }

        /// <summary>
        /// Gõ tiền tố mới vào ô rồi chờ tới khi NHÁP thật sự tồn tại — theo ĐIỀU KIỆN + hạn giờ thật, không đếm khung.
        /// <para>
        /// (W10-11) Bản cũ ghi <c>yield return null</c> đúng MỘT khung sau khi đặt <c>value</c>. Một khung là đủ khi máy
        /// rảnh, nhưng ô tiền tố đi qua <c>ChangeEvent</c> → presenter dựng nháp → panel vẽ lại; khi máy đang chạy vài
        /// lượt Unity song song thì chuỗi ấy có thể tràn sang khung sau, và mọi câu phía dưới (nút Huỷ, class chớp) đọc
        /// một cây CHƯA có khối nháp. Đó là gốc chập chờn của hai ca <c>PrefixDraft_ClickLockedField*</c>: không ca nào
        /// sai về HÀNH VI, chúng chỉ nhìn sớm một khung.
        /// </para>
        /// <para>
        /// Hạn giờ hết thì câu assert dưới đây đỏ ngay tại chỗ với lý do đọc được, chứ không để ca đỏ ở một câu xa hơn
        /// nói về nút Huỷ trong khi thứ thiếu là cái nháp.
        /// </para>
        /// </summary>
        private IEnumerator TypeNewPrefixAndWaitForDraft()
        {
            Section.Form.PrefixField.value = NewPrefix;
            // Nhường ÍT NHẤT một khung, KHÔNG phải để "chờ cho chắc": đặt `value` chạy ChangeEvent ngay trong lượt gọi,
            // nên `NeedsConfirmation` đã true trước khi vòng chờ dưới đây hỏi câu đầu tiên — vòng chờ sẽ thoát mà không
            // nhả khung nào, và cây nháp lẫn resolvedStyle của câu khoá thì chỉ có sau một lượt vẽ của panel. Đo được:
            // bỏ hẳn khung này làm PrefixDraft_ShowsLockSentenceOnce đếm 0 câu khoá và hai ca ClickLockedField* không
            // thấy nút Huỷ.
            yield return null;
            // Rồi mới chờ theo ĐIỀU KIỆN + hạn giờ (W10-11): điều kiện hỏi CÂY, không hỏi mô hình, vì thứ mọi câu phía
            // dưới đọc là cây — khối nháp có mặt và nút Huỷ đã dựng.
            yield return WaitForCondition(_scope.View,
                () => Section.Draft.NeedsConfirmation && DraftCancelButton() != null);
            Assert.IsTrue(Section.Draft.NeedsConfirmation,
                "gõ tiền tố mới khi đợt đang chạy phải tạo nháp tại ô — chờ theo điều kiện tới "
                + MaximumWaitMilliseconds + " ms vẫn chưa thấy nháp nào");
            Assert.IsNotNull(DraftCancelButton(),
                "có nháp rồi mà cây vẫn chưa dựng nút Huỷ (Esc) sau " + MaximumWaitMilliseconds + " ms");
        }

        /// <summary>Nút "Huỷ (Esc)" của khối nháp — null khi khối nháp chưa dựng.</summary>
        private Button DraftCancelButton()
        {
            return _scope.View.Q<Button>(RecurringRuleForm.DraftCancelElementName);
        }

        /// <summary>Chuột thật lên tâm một phần tử của màn — kể cả phần tử đang bị khoá (đó là chỗ UX-23 đo).</summary>
        private IEnumerator ClickAt(VisualElement element)
        {
            yield return LiveOpsHubWindowTestScope.WaitForLayout(element);
            SendClick(element);
            // Không chờ thêm khung nào ở đây (W10): hai khung cố định của bản cũ vừa là phép đếm khung, vừa THỪA — mọi nơi
            // gọi ClickAt đều chờ tiếp theo ĐIỀU KIỆN (class chớp xuất hiện, focus vào đúng nút), và vòng chờ ấy tự cầm
            // nhịp panel nên nó phủ luôn quãng mà hai khung kia định chờ hộ.
        }

        private IEnumerator SelectRule(string eventType)
        {
            Section.ApplyNavigation(LiveOpsHubNavigation.To(Section.Id).WithEventType(eventType, false));
            yield return null;
        }

        /// <summary>Chờ bảng đợt kế tiếp tính lại sau debounce 250 ms — theo ĐIỀU KIỆN + hạn giờ thật, không đếm khung.</summary>
        private IEnumerator WaitForOccurrenceRows(int expectedRowCount)
        {
            // Nhường ÍT NHẤT một khung trước khi đo: resolvedStyle chỉ đổi sau một lượt resolve style của panel, nên đọc
            // ngay sau khi gán giá trị là đọc lại con số của khung trước — cái bẫy làm test này đỏ nhầm.
            yield return null;
            yield return WaitForCondition(_scope.View, () => Section.Form.Occurrences.RowCount == expectedRowCount);
            Assert.AreEqual(expectedRowCount, Section.Form.Occurrences.RowCount,
                "bảng đợt kế tiếp không về " + expectedRowCount + " hàng trong " + MaximumWaitMilliseconds + " ms");
        }

        private IEnumerator OpenDesignSample(int width, int height)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithConfirmation(new ScriptedLiveOpsHubConfirmationPresenter())
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            _scope = SectionTestScope.Open(new RecurringRulesSection(services), width, height);
            yield return _scope.WaitForLayout();
        }

        /// <summary>Tiền tố weekly-pass bằng ĐÚNG bản đã đăng: gõ tiền tố mới từ đây là ca "đổi id đợt đang chạy" sạch.</summary>
        private IEnumerator OpenPublishedPrefixSample(int width, int height)
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            RecurringLiveEventRule weeklyPass;
            Assert.IsTrue(document.TryGetRecurringRule(WeeklyPassType, out weeklyPass));
            LiveEventCalendarDocument withPublishedPrefix;
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document,
                new SetRecurringRuleEdit(weeklyPass.WithIdPrefix(PublishedPrefix)), out withPublishedPrefix));
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithConfirmation(new ScriptedLiveOpsHubConfirmationPresenter())
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(withPublishedPrefix)));
            _scope = SectionTestScope.Open(new RecurringRulesSection(services), width, height);
            yield return _scope.WaitForLayout();
        }
    }
}
