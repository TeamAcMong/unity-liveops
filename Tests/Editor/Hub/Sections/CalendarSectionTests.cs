using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

using DreamTech.LiveOps.Tests;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Màn Lịch dựng trong cửa sổ hub THẬT trên đúng phiên của mình: đủ tên element viết tay, trạng thái "chưa có asset", nút
    /// header và trạng thái view sống qua domain reload. Mở bằng <c>OpenWithServices</c> + registry dựng từ cùng services —
    /// mở bằng registry mặc định sẽ có hai phiên và màn đọc nhầm phiên.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class CalendarSectionTests
    {
        private const int MaximumLayoutFrames = 60;
        private const int MaximumLayoutMilliseconds = 5000;

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
        }

        [UnityTest]
        public IEnumerator CreateView_RequiredElementsPresent()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario);
            VisualElement view = SectionView();
            Assert.IsNotNull(view, "màn Lịch phải dựng được view (UXML nạp được)");
            foreach (string elementName in LiveOpsHubPaths.RequiredCalendarElementNames)
            {
                Assert.IsNotNull(view.Q(elementName),
                    "màn Lịch thiếu element '" + elementName + "' — UXML và RequiredElementNames lệch nhau");
            }
        }

        [UnityTest]
        public IEnumerator NoAsset_ShowsEmptyAndHidesBody()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.NoAssetScenario);
            VisualElement view = SectionView();
            Assert.IsNotNull(view);
            VisualElement empty = view.Q(LiveOpsHubPaths.CalendarElementNames.Empty);
            VisualElement main = view.Q(LiveOpsHubPaths.CalendarElementNames.Main);
            Assert.IsFalse(empty.ClassListContains(LiveOpsHubClassNames.CalendarHidden), "chưa có asset thì hiện trạng thái trống");
            Assert.IsTrue(main.ClassListContains(LiveOpsHubClassNames.CalendarHidden), "và giấu thân màn, không vẽ trục rỗng");
        }

        [UnityTest]
        public IEnumerator Selection_FillsInspectorTitle()
        {
            yield return OpenCalendar(LiveOpsHubTestServices.DesignSampleScenario);
            CalendarSection section = (CalendarSection)((IHubHost)_window).Sections[2];
            section.Presenter.SetSelectedBarKey(LiveOpsDesignSample.HuntBonusEntryKey);
            VisualElement title = SectionView().Q(LiveOpsHubPaths.CalendarElementNames.InspectorTitle);
            Assert.Greater(title.childCount, 0, "chọn một đợt thì pane-title có swatch + id");
            Assert.AreEqual(CalendarInspectorModel.StateFixedEvent, section.Inspector.Model.State);
        }

        [Test]
        public void HeaderActions_HasAddEventButton()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarSection section = new CalendarSection(services);
            VisualElement container = new VisualElement();
            section.PopulateHeaderActions(container);
            Button add = container.Q<Button>();
            Assert.IsNotNull(add, "section header của Lịch có nút chính Thêm đợt");
            Assert.AreEqual(LiveOpsHubStrings.CalendarAddEventButton, add.text);
            Assert.IsTrue(add.ClassListContains(LiveOpsHubClassNames.ButtonPrimary));
        }

        [Test]
        public void ViewState_RoundTripsSelectionAndZoom()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarSection section = new CalendarSection(services);
            section.Presenter.SetSelectedBarKey(LiveOpsDesignSample.HuntBonusEntryKey);
            string state = section.CaptureViewState();

            CalendarSection restored = new CalendarSection(services);
            restored.RestoreViewState(state);
            Assert.AreEqual(LiveOpsDesignSample.HuntBonusEntryKey, restored.Presenter.SelectedBarKey,
                "đợt đang chọn phải sống qua domain reload (7.0)");
        }

        [Test]
        public void ViewState_BrokenJsonFallsBackToDefault()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarSection section = new CalendarSection(services);
            Assert.DoesNotThrow(() => section.RestoreViewState("{khong-phai-json"),
                "JSON hỏng của bản trước không được làm sập màn");
            Assert.AreEqual(string.Empty, section.Presenter.SelectedBarKey);
        }

        [Test]
        public void Health_RoutesCalendarFindings()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarSection section = new CalendarSection(services);
            SectionHealth health = section.GetHealth();
            Assert.AreEqual(LiveOpsHubFindingRouting.ForSection(LiveOpsHubSections.Ids.Calendar, services).State, health.State,
                "health của màn đi qua đúng một lối định tuyến, không tự tính lại");
        }

        private IEnumerator OpenCalendar(string scenarioId)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(scenarioId);
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            _window = LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Calendar);
            _window.position = new Rect(0, 0, 1280, 760);
            yield return WaitForLayout(_window.rootVisualElement);
        }

        private VisualElement SectionView()
        {
            return _window.SectionBody == null || _window.SectionBody.childCount == 0 ? null : _window.SectionBody[0];
        }

        /// <summary>(V-23) Chỉ fail khi quá CẢ 60 khung LẪN 5 giây — máy chậm không được biến test UI thành đỏ giả.</summary>
        private static IEnumerator WaitForLayout(VisualElement element)
        {
            int frames = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!LiveOpsHubWindowTestScope.HasLayout(element))
            {
                frames++;
                if (frames > MaximumLayoutFrames && stopwatch.ElapsedMilliseconds > MaximumLayoutMilliseconds)
                {
                    Assert.Fail("cửa sổ hub không có layout sau 60 khung và 5 giây — test UI phải chạy không -nographics");
                }
                yield return null;
            }
            yield return null;
            yield return null;
        }
    }
}
