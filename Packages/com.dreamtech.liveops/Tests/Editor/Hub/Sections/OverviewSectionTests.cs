using System;
using System.Collections;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Màn Tổng quan dựng trong cửa sổ hub THẬT với phiên thật (9.1): token hai skin và stylesheet khung chỉ có ở
    /// <c>.liveops-hub-root</c> nên view dựng ngoài cửa sổ cho <c>resolvedStyle</c> sai. Mỗi test mở cửa sổ bằng
    /// <see cref="LiveOpsHubWindow.OpenWithServices(LiveOpsHubServices, IReadOnlyList{IHubSection}, string)"/> với registry thật dựng
    /// từ CÙNG một services — màn và cửa sổ phải nhìn chung một phiên.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class OverviewSectionTests
    {
        private const double LayoutTimeoutSeconds = 5.0;

        /// <summary>Cỡ cửa sổ hẹp nhất của bộ ảnh (`hs-shell-compact-700`) — chỗ thân màn chắc chắn dài hơn khung.</summary>
        private const float NarrowWindowWidth = 700f;

        /// <summary>Rộng hơn breakpoint hẹp (900 px) nên bố cục 2×2 của <c>--narrow</c> KHÔNG bật — xem WideButShortWindow.</summary>
        private const float WideWindowWidth = 1100f;
        private const float NarrowWindowHeight = 520f;

        /// <summary>Sai số một pixel khi so mép: layout UI Toolkit làm tròn theo dpi, so bằng == sẽ đỏ giả.</summary>
        private const float LayoutTolerance = 1f;

        /// <summary>
        /// Sai số DƯỚI một pixel, cho những phép so mà chính một pixel là thứ đang đo (F8: hai dòng cùng cột lệch nhau
        /// đúng 1px). Dùng <see cref="LayoutTolerance"/> ở đó thì ca xanh trên đúng cái lỗi nó sinh ra để bắt.
        /// </summary>
        private const float SubPixelTolerance = 0.5f;

        /// <summary>
        /// Nghịch đảo của ngưỡng "chữ chiếm quá 95% bề rộng ô" (W9-25): vùng nội dung phải rộng hơn chữ ít nhất 1/0,95 lần.
        /// <para>
        /// (soát W10 F7) Lấy THẲNG <see cref="UxLayoutAuditor.TextFillLimitRatio"/>. Bản trước chép lại số 0,95 kèm lời
        /// khai "cổng nằm trong nhánh test khác" — lời khai ấy SAI: auditor cùng namespace
        /// <c>DreamTech.LiveOps.Editor.Tests</c> và cùng asmdef <c>DreamTech.LiveOps.Editor.Tests.asmdef</c> với ca này,
        /// hằng lại <c>internal</c>, nên gọi thẳng được. Chép lại thì hai con số trôi khỏi nhau mà không gì báo.
        /// </para>
        /// </summary>
        private const float TextFillCeilingRatio = 1f / UxLayoutAuditor.TextFillLimitRatio;

        /// <summary>
        /// Khoảng dư TỐI THIỂU (px) để một ô được coi là "rộng hơn chữ" — đọc THẲNG
        /// <c>UxLayoutAuditor.TextFillMinimumSlack</c> (cổng đợt W10, G-FIX-W10-1 đã mở hằng ấy thành <c>internal</c> và
        /// xoá bản chép tay, nợ F7 của G-W10-FIELD).
        /// <para>
        /// Cổng dùng con số này để BỎ QUA nhãn tự co. Ca dưới đây dùng nó NGƯỢC LẠI — nhãn tự co trong một ô còn rộng
        /// chính là lỗi của W9-25 chỗ 3 — nên nó assert thay vì bỏ qua. Ca vì thế nghiêm hơn cổng, và cố ý: một ca bỏ qua
        /// nhãn tự co sẽ XANH trên đúng cái mã chưa sửa.
        /// </para>
        /// </summary>
        private const float TextFillMinimumSlack = UxLayoutAuditor.TextFillMinimumSlack;

        private LiveOpsHubWindow _window;

        [TearDown]
        public void TearDown()
        {
            if (_window != null)
            {
                _window.Close();
                _window = null;
            }
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        /// <summary>Câu lý do của ngữ cảnh khoá — nội dung không quan trọng, điều quan trọng là màn in ĐÚNG câu của port.</summary>
        private const string PasteUnavailableReason = "Luồng dán bị khoá trong ngữ cảnh test";

        /// <summary>
        /// Nút "Dán JSON đang chạy…" khi luồng dán bị khoá: nút khoá và lý do IN THÀNH CHỮ cạnh nút, lấy đúng câu của port —
        /// đường chính theo SPIKE-B SP-3, không test tooltip.
        /// <para>
        /// Từ W5 luồng thật (<c>LiveOpsHubPasteRunningJsonAction</c>) LUÔN bật, nên ngữ cảnh "bị khoá" phải dựng tường minh
        /// bằng <see cref="ManualLiveOpsHubActions"/> — trước đó nó đến miễn phí từ adapter tạm của bản dev (mục 12 I-3, đã xoá).
        /// Màn vẫn phải vẽ đúng phía khoá, nên test này không mất đi cùng adapter tạm.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator PasteUnavailable_ButtonDisabledWithActionsReason()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document))
                .WithActions(new ManualLiveOpsHubActions(PasteUnavailableReason)));
            services.Session.RunCheckToCompletion();
            yield return OpenOverview(services);
            Assume.That(services.Actions.CanPasteRunningJson, Is.False, "ngữ cảnh test phải là bản có luồng dán bị khoá");

            LiveOpsButtonSlot slot = FindSlotWithButtonText(View(), LiveOpsHubStrings.OverviewPasteRunningJsonButton);
            Assert.IsNotNull(slot, "hàng bản remote phải có nút Dán JSON đang chạy…");
            Assert.IsFalse(slot.Button.enabledSelf, "action chưa có thì nút phải khoá, không được bấm vào chỗ không có gì");
            Assert.IsTrue(slot.IsBlocked);
            Assert.AreEqual(services.Actions.PasteRunningJsonUnavailableReason, slot.Reason,
                "lý do in cạnh nút phải là câu của chính port, không phải câu màn tự viết");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Số đo Hình 4 chốt bằng resolvedStyle: cột "chặn Copy JSON" 96px, NÚT việc cần làm 150px (đo chính nút, không đo slot
        /// bọc — slot còn chứa chữ lý do khoá), thân "Đường đi của lịch" 48px, cột bảng 7 ngày 170/250/140/130. Ô không viền nên
        /// dò cạnh trên ảnh chỉ thấy mép chữ — đây mới là chỗ chốt số.
        /// </summary>
        [UnityTest]
        public IEnumerator NeedsActionRow_ColumnWidthsMatchDesign()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenOverview(services);
            VisualElement view = View();

            VisualElement blocks = view.Q(className: LiveOpsHubClassNames.OverviewNeedBlocks);
            Assert.IsNotNull(blocks);
            Assert.AreEqual(96f, blocks.resolvedStyle.width, 0.5f, "cột trạng thái cổng rộng 96px [SD1 §1.1]");

            List<Button> rowButtons = RowButtons(view);
            Assert.Greater(rowButtons.Count, 0, "lịch mẫu phải có hàng việc cần làm");
            foreach (Button rowButton in rowButtons)
            {
                Assert.AreEqual(150f, rowButton.resolvedStyle.width, 0.5f,
                    "nút việc cần làm rộng 150px, chữ dài ngắn khác nhau không làm cột răng cưa: " + rowButton.name);
            }

            Assert.AreEqual(48f, view.Q(OverviewSection.FlowElementName).resolvedStyle.height, 0.5f, "thân Đường đi của lịch cao 48px");

            Assert.AreEqual(170f, view.Q(className: LiveOpsHubClassNames.OverviewUpcomingCellTime).resolvedStyle.width, 0.5f);
            Assert.AreEqual(250f, view.Q(className: LiveOpsHubClassNames.OverviewUpcomingCellEvent).resolvedStyle.width, 0.5f);
            Assert.AreEqual(140f, view.Q(className: LiveOpsHubClassNames.OverviewUpcomingCellType).resolvedStyle.width, 0.5f);
            Assert.AreEqual(130f, view.Q(className: LiveOpsHubClassNames.OverviewUpcomingCellKind).resolvedStyle.width, 0.5f);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (W9-25 chỗ 3) Ô giờ của bảng "7 ngày tới" phải còn KHOẢNG DƯ, không chỉ "chưa cắt". Chuỗi dài nhất của bảng
        /// ("14/9 00:00 → 20/9 00:00") cần 157px; trước khi sửa, nhãn mono tự co đúng bằng chữ mình trong một ô 170px đã
        /// trừ 6px đệm ô và 3px đệm mặc định của chính nhãn — còn 161px, dư 4px, tức 2,5%: chưa cắt, nhưng một đổi metric
        /// font (bản Unity khác, DPI khác, cỡ chữ Editor khác) là cắt IM LẶNG, đúng loại hỏng mà W9-25 dựng lên để bắt.
        /// <para>
        /// Ca này đo trên NHÃN, đúng chỗ luật của cổng đo: ô rộng mà nhãn tự co thì chỗ dư của ô không thành chỗ dư của
        /// chữ. Bề rộng cột giữ đúng 170px của Hình 4 (khoá ở ca ngay trên) — chỗ dư lấy từ đệm, không lấy từ cột.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator UpcomingTimeCell_KeepsSlackForTheLongestTimeText()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenOverview(services);
            VisualElement view = View();

            List<VisualElement> timeCells = new List<VisualElement>();
            view.Query<VisualElement>(className: LiveOpsHubClassNames.OverviewUpcomingCellTime).ToList(timeCells);
            Assert.Greater(timeCells.Count, 1, "bảng 7 ngày của lịch mẫu phải có hàng dữ liệu, không chỉ hàng tiêu đề");

            int measuredCount = 0;
            for (int index = 0; index < timeCells.Count; index++)
            {
                Label time = timeCells[index].Q<Label>(className: LiveOpsHubClassNames.Mono);
                if (time == null || string.IsNullOrEmpty(time.text)) continue;
                if (time.resolvedStyle.whiteSpace != WhiteSpace.NoWrap) continue;
                float needed = time.MeasureTextSize(time.text, 0f, VisualElement.MeasureMode.Undefined, 0f,
                    VisualElement.MeasureMode.Undefined).x;
                float available = time.contentRect.width;
                measuredCount++;
                // Vế TỰ CO, đúng cái đã hỏng: trước khi sửa, nhãn mono co đúng bằng chữ mình (dư 0) nên ô 170px còn rộng
                // mà chữ vẫn không có một pixel dư nào. Không có vế này thì ca xanh trên mã chưa sửa.
                Assert.Greater(available - needed, TextFillMinimumSlack,
                    "nhãn giờ \"" + time.text + "\" đang ôm KHÍT chữ (dư "
                    + (available - needed).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                    + "px): nhãn tự co thì chỗ dư của ô 170px không bao giờ thành chỗ dư của CHỮ (W9-25 chỗ 3)");
                Assert.GreaterOrEqual(available, needed * TextFillCeilingRatio,
                    "ô giờ \"" + time.text + "\" cần " + needed.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                    + "px, chỗ có " + available.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                    + "px — chưa cắt nhưng dưới 5% dư, một đổi metric font là cắt im lặng (W9-25)");
            }

            Assert.Greater(measuredCount, 0, "không đo được ô giờ nào — phép lọc hỏng thì ca này thành lời khai suông");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (soát W10 F8) Mọi dòng chữ của cột "Giờ (UTC)" phải bắt đầu ở CÙNG một mép trái — nhãn giờ (mono) và nhãn tương
        /// đối của hàng dữ liệu, cùng nhãn tiêu đề của hàng đầu (hàng tiêu đề dùng LẠI đúng class ô <c>--time</c>).
        /// <para>
        /// Vì sao phải khoá: chỗ 3 của W9-25 nới vùng nội dung bằng cách bỏ đệm ngang mặc định của TextElement, nhưng luật
        /// USS chỉ trùng nhãn mang class <c>liveops-hub-mono</c>. Hai nhãn kia giữ nguyên 1px đệm trái, nên ba dòng cùng
        /// một cột bắt đầu lệch nhau 1px — một cạnh không thẳng, và không ca nào trong bộ kiểm nhìn thấy.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator UpcomingTimeColumn_EveryLineStartsAtTheSameLeftEdge()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenOverview(services);
            VisualElement view = View();

            List<VisualElement> timeCells = new List<VisualElement>();
            view.Query<VisualElement>(className: LiveOpsHubClassNames.OverviewUpcomingCellTime).ToList(timeCells);
            Assert.Greater(timeCells.Count, 1, "bảng 7 ngày của lịch mẫu phải có cả hàng tiêu đề lẫn hàng dữ liệu");

            int comparedCount = 0;
            for (int index = 0; index < timeCells.Count; index++)
            {
                VisualElement cell = timeCells[index];
                float cellLeft = cell.worldBound.xMin;
                List<Label> lines = new List<Label>();
                cell.Query<Label>().ToList(lines);
                for (int line = 0; line < lines.Count; line++)
                {
                    Label label = lines[line];
                    if (string.IsNullOrEmpty(label.text)) continue;
                    comparedCount++;
                    float textLeft = label.worldBound.xMin + label.resolvedStyle.paddingLeft;
                    Assert.AreEqual(cellLeft, textLeft, SubPixelTolerance,
                        "dòng \"" + label.text + "\" của cột Giờ bắt đầu ở "
                        + textLeft.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                        + " trong khi mép trái ô là "
                        + cellLeft.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                        + " — ba dòng cùng một cột phải thẳng một cạnh (F8)");
                    Assert.AreEqual(0f, label.resolvedStyle.paddingLeft, SubPixelTolerance,
                        "nhãn \"" + label.text + "\" còn giữ đệm trái mặc định của TextElement: luật bỏ đệm ngang của ô "
                        + "giờ phải trùng MỌI con trực tiếp, không riêng nhãn mono (F8)");
                }
            }

            Assert.Greater(comparedCount, 2, "phải so được cả nhãn tiêu đề lẫn hai mức chữ của hàng dữ liệu");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Không hàng nào vẽ ra ngoài card: slot của nút = [lý do khoá][nút 150px], hàng có lý do dài từng đẩy nút vượt mép phải
        /// cửa sổ và chữ nút bị cắt cụt trên mọi ảnh ghim. Mép phải đo bằng worldBound vì đó chính là thứ máy chụp ghi lại.
        /// </summary>
        [UnityTest]
        public IEnumerator NeedsActionRow_ButtonSlotStaysInsideCard()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenOverview(services);
            VisualElement view = View();

            VisualElement card = view.Q(OverviewSection.NeedsActionBodyElementName);
            float cardRightEdge = card.worldBound.xMax;
            List<VisualElement> slots = view.Query(className: LiveOpsHubClassNames.OverviewNeedButton).ToList();
            Assert.Greater(slots.Count, 0);
            foreach (VisualElement slot in slots)
            {
                Assert.LessOrEqual(slot.worldBound.xMax, cardRightEdge + 0.5f,
                    "slot nút vẽ ra ngoài card: " + slot.name);
            }
            foreach (Button rowButton in RowButtons(view))
            {
                Assert.LessOrEqual(rowButton.worldBound.xMax, cardRightEdge + 0.5f,
                    "nút vẽ ra ngoài card (chữ nút sẽ bị cắt): " + rowButton.name);
            }
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Mọi tên element sống còn phải Q được ở CẢ hai ngữ cảnh — probe 9.3 và HubWindowTests duyệt cả hai.</summary>
        [UnityTest]
        public IEnumerator RequiredElements_PresentWithAndWithoutAsset()
        {
            LiveOpsHubServices withAsset = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenOverview(withAsset);
            AssertRequiredElements();

            LiveOpsHubServices withoutAsset = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario);
            yield return OpenOverview(withoutAsset);
            AssertRequiredElements();
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>(a) chưa có asset: empty ba bước thay CẢ thân, nút chính là "Tạo asset lịch…" [SD1 §1.2].</summary>
        [UnityTest]
        public IEnumerator NoAsset_ShowsThreeStepEmptyInsteadOfCards()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario);
            yield return OpenOverview(services);

            VisualElement view = View();
            Assert.IsFalse(view.Q(OverviewSection.EmptyElementName).ClassListContains(LiveOpsHubClassNames.OverviewHidden));
            Assert.IsTrue(view.Q(OverviewSection.DefaultBodyElementName).ClassListContains(LiveOpsHubClassNames.OverviewHidden));

            VisualElement steps = view.Q(OverviewSection.EmptyStepsElementName);
            Assert.AreEqual(3, steps.childCount, "empty (a) luôn nói ba bước: tạo asset · khai loại · kiểm lần đầu");

            Button create = view.Q<Button>(OverviewSection.CreateAssetButtonElementName);
            Assert.IsNotNull(create);
            Assert.IsTrue(create.ClassListContains(LiveOpsHubClassNames.ButtonPrimary), "bước tiếp theo phải là nút chính");
            Assert.AreEqual(LiveOpsHubStrings.OverviewCreateAssetButton, create.text);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Mẫu thiết kế: bốn metric, hàng việc cần làm có nút, bảng 7 ngày có dòng — không ô nào trống lặng lẽ.</summary>
        [UnityTest]
        public IEnumerator DesignSample_RendersFourMetricsAndRows()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenOverview(services);

            VisualElement view = View();
            Assert.AreEqual(4, view.Q(OverviewSection.MetricsElementName).childCount, "ĐANG CHẠY · CẦN XỬ LÝ · CHƯA KIỂM · ĐÃ ĐĂNG");
            Assert.Greater(view.Q(OverviewSection.NeedsActionBodyElementName).childCount, 0);
            Assert.Greater(view.Q(OverviewSection.FlowElementName).childCount, 0);
            Assert.Greater(view.Q(OverviewSection.UpcomingElementName).childCount, 1, "bảng có hàng tiêu đề + ít nhất một dòng");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Chưa có dấu đã đăng: tab "Bản đã đăng" khoá, và lý do phải đọc được THÀNH CHỮ trên màn (7.0 / SPIKE-B SP-3: tooltip
        /// không bao giờ là đường duy nhất, và không test nào assert tooltip). <see cref="LiveOpsTabStrip"/> chưa có nhãn lý do
        /// cạnh tab (bề mặt của G-CONTROLS), nên chỗ in lý do là chân metric ĐÃ ĐĂNG + hàng việc cần làm — cả hai cùng nói
        /// "Chưa có dấu đã đăng".
        /// </summary>
        [UnityTest]
        public IEnumerator WithoutPublishedStamp_PublishedTabIsDisabledAndReasonIsOnScreen()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.DocumentWithoutPublishedStamp())));
            services.Session.RunCheckToCompletion();
            yield return OpenOverview(services);

            VisualElement view = View();
            LiveOpsTabStrip tabs = view.Q<LiveOpsTabStrip>(OverviewSection.UpcomingTabsElementName);
            Assert.IsNotNull(tabs);
            Assert.AreEqual(OverviewSection.DraftTabIndex, tabs.SelectedIndex);
            Assert.IsFalse(tabs.TabAt(OverviewSection.PublishedTabIndex).enabledSelf);

            // Hai chỗ in lý do thành chữ, cả hai đều lấy câu từ catalog nên khẳng định không phụ thuộc ngôn ngữ đang bật.
            Assert.IsTrue(AnyVisibleLabelWithText(view, LiveOpsHubStrings.OverviewRowNoStampTitle),
                "vì sao tab bị khoá phải in thành chữ ở hàng việc cần làm, không chỉ nằm trong tooltip");
            Assert.IsTrue(AnyVisibleLabelWithText(view, LiveOpsHubStrings.OverviewMetricPublishedNoStampFoot),
                "chân metric ĐÃ ĐĂNG cũng phải nói vì sao chưa có bản để so");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Nút chính của header kiểm lại toàn bộ; health của màn vẫn Ok (6.4) để không đếm đôi với màn gốc.</summary>
        [UnityTest]
        public IEnumerator RecheckAllButton_StartsCheck_AndHealthStaysOk()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenOverview(services);

            IHubSection section = FindOverviewSection();
            Assert.AreEqual(HealthState.Ok, section.GetHealth().State, "Tổng quan không tự kết luận (6.4)");

            VisualElement actions = _window.rootVisualElement.Q(LiveOpsHubPaths.ShellElementNames.SectionActions);
            Button recheck = actions.Q<Button>(OverviewSection.RecheckAllButtonElementName);
            Assert.IsNotNull(recheck, "header màn phải có nút Kiểm lại tất cả");
            // Chữ của nút nằm ở Label con (nút có icon [Refresh] theo 7.1) — Button.text rỗng là ĐÚNG, không phải mất chữ.
            Assert.AreEqual(LiveOpsHubStrings.OverviewRecheckAllButton,
                recheck.Q<Label>(OverviewSection.RecheckAllLabelElementName).text);
            Assert.IsNotNull(recheck.Q<Image>(), "7.1 ghi '[Refresh] Kiểm lại tất cả' — nút phải có icon");

            services.Session.Check.Reset();
            Assert.IsNull(services.Session.Check.LastReport);
            Click(recheck);
            services.Session.RunCheckToCompletion();
            Assert.IsNotNull(services.Session.Check.LastReport, "bấm Kiểm lại tất cả phải chạy lần kiểm mới");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Hàng việc cần làm bấm nút = phát yêu cầu điều hướng lên bus; màn không gọi thẳng cửa sổ (8.x).</summary>
        [UnityTest]
        public IEnumerator NeedsActionRow_Button_PublishesNavigationOnBus()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenOverview(services);

            List<LiveOpsHubNavigation> requests = new List<LiveOpsHubNavigation>();
            Action<LiveOpsHubNavigation> handler = navigation => requests.Add(navigation);
            services.Bus.NavigationRequested += handler;
            try
            {
                Button button = FindButtonWithText(View(), LiveOpsHubStrings.OverviewOpenValidationButton);
                Assert.IsNotNull(button, "hàng 2 đợt bị bỏ phải có nút Mở Kiểm lịch");
                Click(button);
            }
            finally
            {
                services.Bus.NavigationRequested -= handler;
            }

            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual(LiveOpsHubSections.Ids.Validation, requests[0].SectionId);
            Assert.AreEqual(LiveOpsHubNavigation.FilterDropped, requests[0].FilterConsequence);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Đường đi của lịch vẽ ĐÚNG health của registry: ép tầng Kiểm thành Blocked bằng registry giả rồi soi chính view —
        /// test model không chứng minh được điều này vì nó tự dựng health, còn ở cửa sổ thật 5 màn W4 còn lại có thể là màn giữ
        /// chỗ (health NotMeasured) nên không hàng nào "dừng ở đây".
        /// </summary>
        [UnityTest]
        public IEnumerator FlowCard_BlockedCheckStage_MarksStopsHereAndDeadConnector()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenOverview(services, BuildRegistryWithBlockedCheckStage(services));

            VisualElement view = View();
            VisualElement checkNode = view.Q(OverviewPipelineFlow.NodeElementNamePrefix
                + PipelineStage.Check.ToString().ToLowerInvariant());
            Assert.IsNotNull(checkNode, "thân Đường đi của lịch phải có nút tầng Kiểm");
            Assert.IsTrue(AnyVisibleLabelWithText(checkNode, LiveOpsHubStrings.OverviewFlowNoteStopsHere),
                "tầng cổng chặn đầu tiên phải in 'dừng ở đây'");

            VisualElement checkConnector = view.Q(OverviewPipelineFlow.ConnectorElementNamePrefix
                + PipelineStage.Check.ToString().ToLowerInvariant());
            Assert.IsNotNull(checkConnector);
            Assert.IsTrue(checkConnector.ClassListContains(LiveOpsHubClassNames.OverviewFlowConnectorDead),
                "đường nối sau tầng chặn đầu tiên phải chết — mắt thấy lịch dừng ở đâu mà không phải đọc chữ");

            VisualElement scheduleConnector = view.Q(OverviewPipelineFlow.ConnectorElementNamePrefix
                + PipelineStage.Schedule.ToString().ToLowerInvariant());
            Assert.IsNotNull(scheduleConnector);
            Assert.IsFalse(scheduleConnector.ClassListContains(LiveOpsHubClassNames.OverviewFlowConnectorDead),
                "đường nối TRƯỚC tầng chặn vẫn sống");
            LogAssert.NoUnexpectedReceived();
        }

        // ============================================================================================================ hạ tầng

        /// <summary>
        /// Registry giả ĐÚNG thứ tự + tầng của <see cref="LiveOpsHubSections.Create(LiveOpsHubServices)"/>, chỉ khác là tầng Kiểm
        /// bị ép Blocked. Màn Tổng quan là màn thật (nó là thứ đang đo), 5 màn còn lại chỉ cần trả health.
        /// </summary>
        private static List<IHubSection> BuildRegistryWithBlockedCheckStage(LiveOpsHubServices services)
        {
            return new List<IHubSection>
            {
                new OverviewSection(services),
                new StubHubSection(LiveOpsHubSections.Ids.EventTypes, PipelineStage.Configure, SectionHealth.Ok()),
                new StubHubSection(LiveOpsHubSections.Ids.Calendar, PipelineStage.Schedule, SectionHealth.Ok()),
                new StubHubSection(LiveOpsHubSections.Ids.RecurringRules, PipelineStage.Schedule, SectionHealth.Ok()),
                new StubHubSection(LiveOpsHubSections.Ids.Validation, PipelineStage.Check,
                    SectionHealth.Blocked(BlockedStageBadge, BlockedStageReason)),
                new StubHubSection(LiveOpsHubSections.Ids.Export, PipelineStage.Export, SectionHealth.Ok()),
            };
        }

        private const string BlockedStageBadge = "2 bị bỏ";
        private const string BlockedStageReason = "2 đợt sẽ bị game bỏ";

        /// <summary>Màn giả chỉ để cấp health cho test khung — không dựng gì, không đọc phiên.</summary>
        private sealed class StubHubSection : IHubSection
        {
            private readonly SectionHealth _health;

            internal StubHubSection(string id, PipelineStage stage, SectionHealth health)
            {
                Id = id;
                Stage = stage;
                _health = health;
            }

            public string Id { get; }
            public string Title => Id;
            public string Subtitle => Id;
            public PipelineStage Stage { get; }
            public IReadOnlyList<string> RequiredElementNames => Array.Empty<string>();

            public SectionHealth GetHealth()
            {
                return _health;
            }

            public VisualElement CreateView()
            {
                return new VisualElement();
            }

            public void OnShown()
            {
            }
        }

        /// <summary>Có Label nào đang hiện mang ĐÚNG câu này không — "in thành chữ" nghĩa là người dùng đọc được, không phải tooltip.</summary>
        private static bool AnyVisibleLabelWithText(VisualElement root, string text)
        {
            foreach (Label label in root.Query<Label>().ToList())
            {
                if (label.resolvedStyle.display == DisplayStyle.None) continue;
                if (string.Equals(label.text, text, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        /// <summary>
        /// Q-W5-1 (user chốt 17/9/2026): dưới 900px thân màn dài hơn cửa sổ. Trước khi có ScrollView phần dư bị CẮT ở đáy —
        /// mất hẳn note cuối và một phần bảng 7 ngày. Test dựng đúng cỡ hẹp nhất của bộ ảnh (700×520, `hs-shell-compact-700`)
        /// rồi chứng minh hai vế: nội dung THẬT SỰ cao hơn khung (không thì test này xanh vô nghĩa), và cuộn tới đáy thì phần
        /// tử CUỐI của thân nằm trọn trong khung nhìn.
        /// </summary>
        [UnityTest]
        public IEnumerator NarrowWindow_BodyScrolls_NothingClipped()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenOverview(services, LiveOpsHubSections.Create(services), NarrowWindowWidth, NarrowWindowHeight);
            VisualElement view = View();

            ScrollView scroll = view.Q<ScrollView>(OverviewSection.ScrollElementName);
            AssertBodyScrollIsVerticalWithAutoScroller(scroll);
            Assert.Greater(scroll.contentContainer.worldBound.height, scroll.contentViewport.worldBound.height,
                "cửa sổ 700×520 phải làm thân dài hơn khung nhìn — nếu không, test không chứng minh được gì");

            Label footerNote = view.Q<Label>(OverviewSection.FooterNoteElementName);
            Assert.IsNotNull(footerNote, "note cuối thân là phần tử cuối cùng phải cuộn tới được");
            scroll.verticalScroller.value = scroll.verticalScroller.highValue;
            yield return null;
            yield return null;

            Rect viewport = scroll.contentViewport.worldBound;
            Rect note = footerNote.worldBound;
            Assert.LessOrEqual(note.yMax, viewport.yMax + LayoutTolerance,
                "cuộn tới đáy rồi mà note cuối vẫn nằm dưới mép khung = vẫn còn bị cắt");
            Assert.GreaterOrEqual(note.yMin, viewport.yMin - LayoutTolerance,
                "note cuối bị đẩy lên trên mép khung = cuộn quá tay, người đọc không thấy chữ");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Ba dòng của <c>OverviewSection.ConfigureBodyScroll</c> = ba nửa của điều user chốt ("ScrollView DỌC, thanh cuộn
        /// AUTO, ngang Hidden"). Test cũ chỉ đọc một dòng, nên xoá nhầm hai dòng kia vẫn xanh.
        /// </summary>
        private static void AssertBodyScrollIsVerticalWithAutoScroller(ScrollView scroll)
        {
            Assert.IsNotNull(scroll, "thân màn phải nằm trong ScrollView dọc (Q-W5-1)");
            Assert.AreEqual(ScrollViewMode.Vertical, scroll.mode, "thân màn cuộn DỌC (Q-W5-1)");
            Assert.AreEqual(ScrollerVisibility.Auto, scroll.verticalScrollerVisibility,
                "thanh cuộn dọc để Auto — hiện khi thân dài hơn khung, tự ẩn khi vừa");
            Assert.AreEqual(ScrollerVisibility.Hidden, scroll.horizontalScrollerVisibility,
                "cuộn ngang tắt — bố cục đã co theo breakpoint, chữ không bao giờ nằm ngoài bề ngang");
        }

        /// <summary>
        /// Cửa sổ RỘNG mà THẤP: không còn ở breakpoint hẹp (bố cục 2×2 của <c>--narrow</c> không bật) nhưng thân vẫn cao hơn
        /// khung. Ca này là nửa còn lại của Q-W5-1 — bản sửa nào chỉ bọc ScrollView cho nhánh hẹp sẽ xanh ở 700×520 và vẫn
        /// cắt mất chữ ở đây.
        /// </summary>
        [UnityTest]
        public IEnumerator WideButShortWindow_BodyStillScrolls()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenOverview(services, LiveOpsHubSections.Create(services), WideWindowWidth, NarrowWindowHeight);
            VisualElement view = View();

            ScrollView scroll = view.Q<ScrollView>(OverviewSection.ScrollElementName);
            AssertBodyScrollIsVerticalWithAutoScroller(scroll);
            Assert.Greater(scroll.contentContainer.worldBound.height, scroll.contentViewport.worldBound.height,
                "cửa sổ rộng mà thấp vẫn phải làm thân dài hơn khung nhìn — nếu không, test không chứng minh được gì");

            Label footerNote = view.Q<Label>(OverviewSection.FooterNoteElementName);
            Assert.IsNotNull(footerNote, "note cuối thân là phần tử cuối cùng phải cuộn tới được");
            scroll.verticalScroller.value = scroll.verticalScroller.highValue;
            yield return null;
            yield return null;

            Rect viewport = scroll.contentViewport.worldBound;
            Rect note = footerNote.worldBound;
            Assert.LessOrEqual(note.yMax, viewport.yMax + LayoutTolerance,
                "cuộn tới đáy rồi mà note cuối vẫn nằm dưới mép khung = vẫn còn bị cắt");
            LogAssert.NoUnexpectedReceived();
        }

        private static List<Button> RowButtons(VisualElement view)
        {
            List<Button> buttons = new List<Button>();
            foreach (VisualElement slot in view.Query(className: LiveOpsHubClassNames.OverviewNeedButton).ToList())
            {
                LiveOpsButtonSlot buttonSlot = slot as LiveOpsButtonSlot;
                if (buttonSlot != null) buttons.Add(buttonSlot.Button);
            }
            return buttons;
        }

        private VisualElement View()
        {
            Assert.IsNotNull(_window.SectionBody, "cửa sổ chưa dựng thân màn");
            Assert.Greater(_window.SectionBody.childCount, 0, "thân màn rỗng — màn Tổng quan ném khi dựng?");
            return _window.SectionBody[0];
        }

        private IHubSection FindOverviewSection()
        {
            foreach (IHubSection section in ((IHubHost)_window).Sections)
            {
                if (string.Equals(section.Id, LiveOpsHubSections.Ids.Overview, StringComparison.Ordinal)) return section;
            }
            Assert.Fail("registry thiếu màn Tổng quan");
            return null;
        }

        private void AssertRequiredElements()
        {
            VisualElement view = View();
            Assert.AreEqual(OverviewSection.BodyElementName, view.name, "root của view phải là thân màn Tổng quan");
            foreach (IHubSection section in ((IHubHost)_window).Sections)
            {
                if (!string.Equals(section.Id, LiveOpsHubSections.Ids.Overview, StringComparison.Ordinal)) continue;
                foreach (string elementName in section.RequiredElementNames)
                {
                    Assert.IsNotNull(view.Q(elementName), "thiếu element '" + elementName + "' — UXML và RequiredElementNames lệch nhau");
                }
            }
        }

        private IEnumerator OpenOverview(LiveOpsHubServices services)
        {
            return OpenOverview(services, LiveOpsHubSections.Create(services));
        }

        private IEnumerator OpenOverview(LiveOpsHubServices services, IReadOnlyList<IHubSection> sections)
        {
            return OpenOverview(services, sections, LiveOpsHubWindowTestScope.StandardWidth, LiveOpsHubWindowTestScope.StandardHeight);
        }

        private IEnumerator OpenOverview(LiveOpsHubServices services, IReadOnlyList<IHubSection> sections, float width, float height)
        {
            if (_window != null)
            {
                _window.Close();
                _window = null;
            }
            _window = LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Overview);
            // SP-16: đặt kích thước SAU Show.
            _window.position = new Rect(0, 0, width, height);
            int frames = 0;
            double deadline = EditorApplication.timeSinceStartup + LayoutTimeoutSeconds;
            // V-23: chỉ fail khi quá CẢ 60 khung LẪN 5 giây — máy bận (hai Unity song song) không được làm test đỏ giả.
            while (!LiveOpsHubWindowTestScope.HasLayout(_window.rootVisualElement))
            {
                if (++frames > LiveOpsHubWindowTestScope.MaximumLayoutFrames && EditorApplication.timeSinceStartup > deadline)
                {
                    Assert.Fail("cửa sổ hub không có layout sau 60 khung và 5 giây — test UI phải chạy không -nographics");
                }
                yield return null;
            }
            yield return null;
            yield return null;
        }

        private static LiveOpsButtonSlot FindSlotWithButtonText(VisualElement root, string text)
        {
            foreach (LiveOpsButtonSlot slot in root.Query<LiveOpsButtonSlot>().ToList())
            {
                if (string.Equals(slot.Button.text, text, StringComparison.Ordinal)) return slot;
            }
            return null;
        }

        private static Button FindButtonWithText(VisualElement root, string text)
        {
            foreach (Button button in root.Query<Button>().ToList())
            {
                if (string.Equals(button.text, text, StringComparison.Ordinal)) return button;
            }
            return null;
        }

        /// <summary>
        /// Bấm nút bằng đúng đường của UI Toolkit thay vì gọi callback tay: <c>NavigationSubmitEvent</c> đi qua chính
        /// <c>Clickable</c> của Button (đường Enter), nên nút đang khoá thật sự không chạy. ClickEvent gửi tay KHÔNG đủ —
        /// Clickable phát ClickEvent chứ không nghe nó.
        /// </summary>
        private static void Click(Button button)
        {
            using (NavigationSubmitEvent submitEvent = NavigationSubmitEvent.GetPooled(EventModifiers.None))
            {
                submitEvent.target = button;
                button.SendEvent(submitEvent);
            }
        }
    }
}
