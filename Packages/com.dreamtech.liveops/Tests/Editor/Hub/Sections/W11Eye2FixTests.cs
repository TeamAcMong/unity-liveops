using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Đợt W11.6, gói G-W11-EYE2: khoá hai lỗi mà người dùng giả NHÌN THẤY ở lượt đi dạo W11 trên ảnh chụp cửa sổ hub thật
    /// (phiếu J3-01 và J3-03 trong <c>plan/w11/W11-JOURNEY.md</c> §6).
    /// <para>
    /// J3-02 (viền nút skin tối) nằm ở <see cref="UxShapeContrastTests"/> vì nó là một phép đo MÀU chạy trên hai skin, không
    /// phải một quan hệ hình học của một màn. J3-04 là việc của bộ đi dạo (worktree G-UX-JOURNEY), không của package.
    /// </para>
    /// <para>
    /// Lưới CHUNG cho cùng hạng lỗi — "nút trong hộp xếp dọc lấy align stretch" — nằm ở
    /// <c>UxLayoutAuditor.CheckButtonStretch</c> nên nó chạy trên MỌI màn × 7 cỡ của ma trận. Hai ca dưới đây đo THÊM bề
    /// ngang thật của đúng hai nút mà phiếu J3-01 đã đếm pixel, ở đủ 7 cỡ: lưới kia nói "nguyên nhân đã hết", hai ca này
    /// nói "hệ quả cũng đã hết", và một phiếu mắt-thấy đáng có cả hai.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class W11Eye2FixTests
    {
        /// <summary>
        /// Trần bề ngang của một nút PHÁ HUỶ, tính theo phần bề ngang hộp chứa nó. Cùng con số với
        /// <c>W11EyeFixTests.NoticeButtonWidthRatio</c> và cùng lý do: nút thật chỉ rộng bằng chữ của nó (đo được 85px cho
        /// nút banner), nên một nửa hộp là trần RỘNG RÃI — nó không khoá bản sửa vào một con số pixel mà chặn đúng hạng lỗi
        /// "nút ăn trọn bề ngang".
        /// </summary>
        private const float DestructiveButtonWidthRatio = 0.5f;

        private const int MaximumLayoutFrames = 60;

        private const double MaximumLayoutSeconds = 5d;

        private LiveOpsHubWindow _window;

        [TearDown]
        public void TearDown()
        {
            if (_window != null) _window.Close();
            _window = null;
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        // ------------------------------------------------------------------ J3-01 nút phá huỷ thôi dãn

        /// <summary>
        /// (J3-01) Nút "Xoá luật…" của màn Luật lặp không được rộng theo hộp chứa ở BẤT KỲ cỡ nào của ma trận.
        /// <para>
        /// Lượt đi dạo W11 đo được 637px ở 1280×760 và 504px ở 820×560 — tức trọn bề ngang pane. Gốc là
        /// <c>align-items: stretch</c> mặc định của UI Toolkit trong form luật (hộp xếp dọc), nên ca này khẳng định CẢ hai
        /// mặt: bề ngang thật (hệ quả) và <c>align-self</c> đã resolve (nguyên nhân). Chỉ đo bề ngang thì một lần đổi bố
        /// cục làm pane hẹp lại cũng "qua"; chỉ đo align thì không ai thấy con số mà phiếu nói về.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator RecurringDeleteButton_DoesNotStretch_AtEveryLayoutSize()
        {
            List<string> wrong = new List<string>();
            int measured = 0;
            foreach (UxWindowSize size in UxHubWindowFixture.AllLayoutSizes)
            {
                yield return OpenHub(LiveOpsHubSections.Ids.RecurringRules, size);
                Button delete = _window.rootVisualElement.Q<Button>(RecurringRuleForm.DeleteElementName);
                if (delete == null || !UxLayoutAuditor.IsShownOnScreen(delete)) continue;
                measured++;
                AppendIfStretched(delete, "Luật lặp · 'Xoá luật…'", size, wrong);
                CloseHub();
            }

            Assert.Greater(measured, 0, "không cỡ nào dựng được nút 'Xoá luật…' — kết luận từ vòng lặp rỗng là xanh giả");
            Assert.IsEmpty(wrong, "nút phá huỷ vẫn dãn theo hộp chứa:" + Environment.NewLine
                + string.Join(Environment.NewLine, wrong.ToArray()));
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (J3-01) Nút "Xoá đợt…" của inspector Lịch — cùng luật, cùng bảy cỡ. Đo được 265px (pane 1280) và 264px (pane 820)
        /// trước bản vá, tức trọn bề ngang pane trừ đệm.
        /// </summary>
        [UnityTest]
        public IEnumerator CalendarDeleteButton_DoesNotStretch_AtEveryLayoutSize()
        {
            List<string> wrong = new List<string>();
            int measured = 0;
            foreach (UxWindowSize size in UxHubWindowFixture.AllLayoutSizes)
            {
                yield return OpenHub(LiveOpsHubSections.Ids.Calendar, size);
                CalendarSection section = SectionOf<CalendarSection>();
                section.Presenter.SetSelectedBarKey(LiveOpsDesignSample.HuntBonusEntryKey);
                yield return null;
                yield return WaitForLayout(_window.rootVisualElement);

                Button delete = FindDangerButton(_window.rootVisualElement);
                if (delete == null || !UxLayoutAuditor.IsShownOnScreen(delete)) continue;
                measured++;
                AppendIfStretched(delete, "Lịch · '" + delete.text + "'", size, wrong);
                CloseHub();
            }

            Assert.Greater(measured, 0, "không cỡ nào dựng được nút 'Xoá đợt…' — kết luận từ vòng lặp rỗng là xanh giả");
            Assert.IsEmpty(wrong, "nút phá huỷ vẫn dãn theo hộp chứa:" + Environment.NewLine
                + string.Join(Environment.NewLine, wrong.ToArray()));
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------ J3-03 menu định dạng màn Xuất JSON

        /// <summary>
        /// (J3-03) Menu đổi định dạng của thẻ ĐỊNH DẠNG phải đọc ra là một MENU, không phải một ô nhập rỗng.
        /// <para>
        /// Ba điều kiện, mỗi cái trả lời đúng một nửa của triệu chứng mà lượt đi dạo mô tả ("một hộp rỗng cao 18px, rộng hết
        /// thẻ, chỉ có mũi tên ▾ ở mép phải"): (a) nó KHÔNG rộng hết thẻ nữa, (b) nó có tooltip nói ra việc nó làm, (c) nó
        /// vẫn KHÔNG mang chữ — [SD2 §3.5] cấm in con số định dạng hai lần trên cùng một tile, và bản vá này không được
        /// lén đổi quyết định thiết kế ấy.
        /// </para>
        /// <para>
        /// (R-F6 lượt soát G-W11-EYE2) Chạy đủ BẢY cỡ của ma trận, không chỉ 1440×900: lượt đi dạo chụp thẻ ở 1280 và bản
        /// soát đòi xem 820 — một ca khoá đúng một cỡ thì hai cỡ kia không có gì canh.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator ExportFormatMenu_LooksLikeMenu_NotEmptyInputField()
        {
            List<string> wrong = new List<string>();
            int measured = 0;
            foreach (UxWindowSize size in UxHubWindowFixture.AllLayoutSizes)
            {
                yield return OpenHub(LiveOpsHubSections.Ids.Export, size);

                ToolbarMenu menu = _window.rootVisualElement.Q<ToolbarMenu>(ExportMetrics.FormatMenuElementName);
                Assert.IsNotNull(menu, "thẻ ĐỊNH DẠNG thiếu menu đổi định dạng @" + size);
                if (!UxLayoutAuditor.IsShownOnScreen(menu))
                {
                    CloseHub();
                    continue;
                }

                VisualElement tile = menu.hierarchy.parent;
                Assert.IsNotNull(tile, "menu đổi định dạng không có hộp chứa @" + size);
                float tileWidth = tile.contentRect.width;
                float menuWidth = menu.resolvedStyle.width;
                if (tileWidth <= 0f)
                {
                    wrong.Add("@" + size + ": thẻ ĐỊNH DẠNG chưa có bề ngang thì phép đo vô nghĩa");
                    CloseHub();
                    continue;
                }

                measured++;
                if (menuWidth >= tileWidth * DestructiveButtonWidthRatio)
                {
                    wrong.Add("@" + size + ": menu rộng " + Number(menuWidth) + "px trên thẻ " + Number(tileWidth)
                        + "px — vẫn dãn theo thẻ nên mắt đọc ra một trường dữ liệu, không phải một nút");
                }

                if (EffectiveAlign(menu) == Align.Stretch) wrong.Add("@" + size + ": menu vẫn lấy align stretch trong tile xếp dọc");
                if (string.IsNullOrEmpty(menu.tooltip)) wrong.Add("@" + size + ": menu KHÔNG có chữ theo thiết kế, nên tooltip là chỗ duy nhất nói nó dùng để làm gì");
                if (!string.IsNullOrEmpty(menu.text)) wrong.Add("@" + size + ": menu không được mang chữ — giá trị đã in ở dòng value của tile [SD2 §3.5]");
                CloseHub();
            }

            Assert.Greater(measured, 0, "không cỡ nào dựng được menu đổi định dạng — kết luận từ vòng lặp rỗng là xanh giả");
            Assert.IsEmpty(wrong, "menu đổi định dạng vẫn đọc ra như một ô nhập rỗng:" + Environment.NewLine
                + string.Join(Environment.NewLine, wrong.ToArray()));
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (J3-03) Tooltip phải có ở CẢ HAI ngôn ngữ. Ca trên chỉ chạy ngôn ngữ đang chạy của hub, nên một mình nó để lọt
        /// bản còn lại — đúng cặp ca mà J2-02 đã đặt ra cho chữ nút banner.
        /// </summary>
        [Test]
        public void ExportFormatMenuTooltip_ExistsInBothLanguages()
        {
            foreach (LiveOpsHubLanguageId language in new[] { LiveOpsHubLanguageId.Vietnamese, LiveOpsHubLanguageId.English })
            {
                string tooltip;
                Assert.IsTrue(LiveOpsHubStringCatalog.TryGetExact(language,
                        nameof(LiveOpsHubStrings.ExportMetricFormatMenuTooltip), out tooltip),
                    "bản " + language + " thiếu câu tooltip cho menu đổi định dạng");
                Assert.IsNotEmpty(tooltip, "bản " + language + ": tooltip rỗng");
            }
        }

        // ------------------------------------------------------------------ trợ giúp

        private static void AppendIfStretched(VisualElement button, string place, UxWindowSize size, List<string> wrong)
        {
            VisualElement host = button.hierarchy.parent;
            float hostWidth = host == null ? 0f : host.contentRect.width;
            float width = button.resolvedStyle.width;
            if (EffectiveAlign(button) == Align.Stretch)
            {
                wrong.Add(place + " @" + size + ": vẫn lấy align stretch trong hộp xếp dọc (rộng " + Number(width) + "px)");
                return;
            }

            if (hostWidth <= 0f)
            {
                wrong.Add(place + " @" + size + ": hộp chứa chưa có bề ngang — phép đo không nói được gì");
                return;
            }

            if (width <= hostWidth * DestructiveButtonWidthRatio) return;
            wrong.Add(place + " @" + size + ": rộng " + Number(width) + "px trên hộp " + Number(hostWidth) + "px");
        }

        /// <summary>Align THẬT SỰ áp lên phần tử: <c>align-self</c> của nó, hoặc <c>align-items</c> của cha khi nó để auto.</summary>
        private static Align EffectiveAlign(VisualElement element)
        {
            Align self = element.resolvedStyle.alignSelf;
            if (self != Align.Auto) return self;
            VisualElement parent = element.hierarchy.parent;
            return parent == null ? Align.Stretch : parent.resolvedStyle.alignItems;
        }

        /// <summary>Nút phá huỷ đang hiện đầu tiên trong cây — inspector Lịch chỉ dựng một cái khi chọn MỘT đợt.</summary>
        private static Button FindDangerButton(VisualElement root)
        {
            List<Button> buttons = new List<Button>();
            root.Query<Button>().ToList(buttons);
            for (int index = 0; index < buttons.Count; index++)
            {
                if (!buttons[index].ClassListContains(LiveOpsHubClassNames.ButtonDanger)) continue;
                if (!UxLayoutAuditor.IsShownOnScreen(buttons[index])) continue;
                return buttons[index];
            }

            return null;
        }

        private static string Number(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private T SectionOf<T>() where T : class, IHubSection
        {
            IReadOnlyList<IHubSection> sections = ((IHubHost)_window).Sections;
            for (int index = 0; index < sections.Count; index++)
            {
                T found = sections[index] as T;
                if (found != null) return found;
            }

            Assert.Fail("cửa sổ hub không có màn " + typeof(T).Name);
            return null;
        }

        private IEnumerator OpenHub(string sectionId, UxWindowSize size)
        {
            CloseHub();
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            _window = LiveOpsHubWindow.OpenWithServices(services, LiveOpsHubSections.Create(services), sectionId);
            _window.position = new Rect(0f, 0f, size.Width, size.Height);
            yield return WaitForLayout(_window.rootVisualElement);
        }

        private void CloseHub()
        {
            if (_window == null) return;
            _window.Close();
            _window = null;
        }

        private static IEnumerator WaitForLayout(VisualElement element)
        {
            double deadline = EditorApplication.timeSinceStartup + MaximumLayoutSeconds;
            for (int frame = 0; frame < MaximumLayoutFrames; frame++)
            {
                if (element.panel != null && element.worldBound.width > 1f && element.worldBound.height > 1f) yield break;
                if (EditorApplication.timeSinceStartup > deadline) break;
                yield return null;
            }

            Assert.IsTrue(element.worldBound.width > 1f, "cửa sổ hub chưa có layout sau khi chờ hết hạn");
        }
    }
}
