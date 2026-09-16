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

        /// <summary>
        /// Nút "Dán JSON đang chạy…" khi action chưa có (INTERIM(G-PASTE)): nút khoá và lý do IN THÀNH CHỮ cạnh nút, lấy đúng
        /// câu của port — đường chính theo SPIKE-B SP-3, không test tooltip.
        /// </summary>
        [UnityTest]
        public IEnumerator PasteUnavailable_ButtonDisabledWithActionsReason()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenOverview(services);
            Assume.That(services.Actions.CanPasteRunningJson, Is.False, "ngữ cảnh test phải là bản chưa có action dán (mục 12 I-3)");

            LiveOpsButtonSlot slot = FindSlotWithButtonText(View(), LiveOpsHubStrings.OverviewPasteRunningJsonButton);
            Assert.IsNotNull(slot, "hàng bản remote phải có nút Dán JSON đang chạy…");
            Assert.IsFalse(slot.Button.enabledSelf, "action chưa có thì nút phải khoá, không được bấm vào chỗ không có gì");
            Assert.IsTrue(slot.IsBlocked);
            Assert.AreEqual(services.Actions.PasteRunningJsonUnavailableReason, slot.Reason,
                "lý do in cạnh nút phải là câu của chính port, không phải câu màn tự viết");
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

        /// <summary>Chưa có dấu đã đăng: tab "Bản đã đăng" khoá kèm lý do — không có bản nào để so thì không giả vờ có.</summary>
        [UnityTest]
        public IEnumerator WithoutPublishedStamp_PublishedTabIsDisabledWithReason()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.DocumentWithoutPublishedStamp())));
            services.Session.RunCheckToCompletion();
            yield return OpenOverview(services);

            LiveOpsTabStrip tabs = View().Q<LiveOpsTabStrip>(OverviewSection.UpcomingTabsElementName);
            Assert.IsNotNull(tabs);
            Assert.AreEqual(OverviewSection.DraftTabIndex, tabs.SelectedIndex);
            Assert.IsFalse(tabs.TabAt(OverviewSection.PublishedTabIndex).enabledSelf);
            Assert.AreEqual(LiveOpsHubStrings.OverviewUpcomingTabPublishedDisabledReason,
                tabs.SlotAt(OverviewSection.PublishedTabIndex).tooltip);
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
            Assert.AreEqual(LiveOpsHubStrings.OverviewRecheckAllButton, recheck.text);

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

        // ============================================================================================================ hạ tầng

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
            if (_window != null)
            {
                _window.Close();
                _window = null;
            }
            _window = LiveOpsHubWindow.OpenWithServices(services, LiveOpsHubSections.Create(services), LiveOpsHubSections.Ids.Overview);
            // SP-16: đặt kích thước SAU Show.
            _window.position = new Rect(0, 0, LiveOpsHubWindowTestScope.StandardWidth, LiveOpsHubWindowTestScope.StandardHeight);
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

        /// <summary>Bấm nút bằng đúng đường của UI Toolkit (ClickEvent) thay vì gọi callback tay — nút khoá phải thật sự không chạy.</summary>
        private static void Click(Button button)
        {
            using (ClickEvent clickEvent = ClickEvent.GetPooled())
            {
                clickEvent.target = button;
                button.SendEvent(clickEvent);
            }
        }
    }
}
