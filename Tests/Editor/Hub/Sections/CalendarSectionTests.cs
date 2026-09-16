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

        // ================================================================================================ Hình 12 [SD1 §3.8]

        /// <summary>803×420 của ma trận 9.5 dòng 12 và bề rộng track 635 — số đo ảnh Hình 12 so thẳng với Hình 1.</summary>
        private const float FrameCanvasWidth = 803f;
        private const float FrameCanvasHeight = 420f;
        private const float FrameLaneWidth = 635f;

        /// <summary>Đủ cho thước + làn + minimap dựng xong và pose chạy hết (bằng MinimumSettleFrames của kịch bản).</summary>
        private const int FrameSettleFrames = 8;

        private static readonly DateTime FrameRangeStartUtc = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime FrameRangeEndUtc = new DateTime(2026, 9, 19, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime FrameEdgeRangeStartUtc = new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime FrameEdgeRangeEndUtc = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc);

        private const string HuntLaneTypeId = "treasure-hunt";
        private const string SkyRaceLaneTypeId = "sky-race";
        private const string StarTournamentLaneTypeId = "star-tournament";

        private static readonly string[] Hinh12ScenarioIds =
        {
            LiveOpsHubCaptureScenarioIds.H12Frame01,
            LiveOpsHubCaptureScenarioIds.H12Frame03a,
            LiveOpsHubCaptureScenarioIds.H12Frame03b,
            LiveOpsHubCaptureScenarioIds.H12Frame04,
            LiveOpsHubCaptureScenarioIds.H12Frame05,
            LiveOpsHubCaptureScenarioIds.H12Frame06,
            LiveOpsHubCaptureScenarioIds.H12Frame07,
            LiveOpsHubCaptureScenarioIds.H12Frame08,
            LiveOpsHubCaptureScenarioIds.H12Frame11,
            LiveOpsHubCaptureScenarioIds.H12Frame13,
            LiveOpsHubCaptureScenarioIds.H12Frame14,
        };

        [Test]
        public void Hinh12_ElevenFramesRegisteredAtMiniTimelineSize()
        {
            foreach (string scenarioId in Hinh12ScenarioIds)
            {
                LiveOpsHubCaptureScenario scenario = LiveOpsHubCaptureScenarios.Find(scenarioId);
                Assert.IsNotNull(scenario, "kịch bản '" + scenarioId + "' của ma trận 9.5 dòng 12 chưa đăng ký");
                Assert.AreEqual(LiveOpsHubLanguageId.Vietnamese, scenario.Language,
                    "ảnh ma trận 9.5 ghim tiếng Việt để so được với hình thiết kế");
                Assert.GreaterOrEqual(scenario.MinimumSettleFrames, FrameSettleFrames,
                    "khung Hình 12 vẽ bằng Painter2D — chụp sớm sẽ ra thước và làn chưa xong");
                LiveOpsHubCaptureExpectedFrame canvasFrame = null;
                foreach (LiveOpsHubCaptureExpectedFrame frame in scenario.ExpectedFrames)
                {
                    if (Math.Abs(frame.Width - FrameCanvasWidth) < 0.01f) canvasFrame = frame;
                }
                Assert.IsNotNull(canvasFrame, "khung ảnh của '" + scenarioId + "' phải khai 803×420 như ma trận 9.5");
                Assert.AreEqual(FrameCanvasHeight, canvasFrame.Height, 0.01f);
            }
        }

        [UnityTest]
        public IEnumerator Frame01_AxisIsFiveDaysFromFourteenSeptember()
        {
            yield return OpenFrame(LiveOpsHubCaptureScenarioIds.H12Frame01);
            LiveOpsTimelineElement timeline = FrameTimeline();
            Assert.AreEqual(FrameRangeStartUtc, timeline.RangeStartUtc, "trục chung của Hình 12 bắt đầu 14/9 00:00 UTC");
            Assert.AreEqual(FrameRangeEndUtc, timeline.RangeEndUtc, "và kết thúc 19/9 00:00 UTC (5 ngày)");
            Assert.AreEqual(string.Empty, timeline.SelectedBarKey, "khung 1 là trạng thái nghỉ — không thanh nào được chọn");
            VisualElement canvas = FrameCanvas();
            Assert.AreEqual(FrameCanvasWidth, canvas.layout.width, 0.5f);
            Assert.AreEqual(FrameCanvasHeight, canvas.layout.height, 0.5f);
            Assert.AreEqual(FrameLaneWidth, timeline.FindLaneElement(HuntLaneTypeId).layout.width, 0.5f,
                "track phải còn đúng 635px sau khi trừ header làn 168px");
        }

        [UnityTest]
        public IEnumerator Frame03a_SelectedBarKeepsTimelineFocus()
        {
            yield return OpenFrame(LiveOpsHubCaptureScenarioIds.H12Frame03a);
            LiveOpsTimelineElement timeline = FrameTimeline();
            Assert.AreEqual(LiveOpsDesignSample.HuntEarlyEntryKey, timeline.SelectedBarKey);
            Assert.IsTrue(timeline.ClassListContains(LiveOpsHubClassNames.TimelineHasFocus),
                "khung 3 nửa trái: timeline đang giữ focus nên nền highlight là bản đậm");
        }

        [UnityTest]
        public IEnumerator Frame03b_SelectedBarWithoutTimelineFocus()
        {
            yield return OpenFrame(LiveOpsHubCaptureScenarioIds.H12Frame03b);
            LiveOpsTimelineElement timeline = FrameTimeline();
            Assert.AreEqual(LiveOpsDesignSample.HuntBonusEntryKey, timeline.SelectedBarKey);
            Assert.IsFalse(timeline.ClassListContains(LiveOpsHubClassNames.TimelineHasFocus),
                "khung 3 nửa phải: focus đã rời timeline — highlight-inactive + viền quiet");
            Assert.IsFalse(timeline.FindBar(LiveOpsDesignSample.HuntBonusEntryKey)
                    .ClassListContains(LiveOpsHubClassNames.TimelineBarFocused),
                "mất focus vùng thì không còn viền focus bàn phím");
        }

        [UnityTest]
        public IEnumerator Frame04_KeyboardFocusRingOnSelectedBar()
        {
            yield return OpenFrame(LiveOpsHubCaptureScenarioIds.H12Frame04);
            LiveOpsTimelineElement timeline = FrameTimeline();
            Assert.IsTrue(timeline.FindBar(LiveOpsDesignSample.HuntEarlyEntryKey)
                    .ClassListContains(LiveOpsHubClassNames.TimelineBarFocused),
                "khung 4 là focus bàn phím: viền focus nằm trên chính thanh đang chọn");
        }

        [UnityTest]
        public IEnumerator Frame05_MoveBodyPreviewsTwelveHoursAndClearsOverlap()
        {
            yield return OpenFrame(LiveOpsHubCaptureScenarioIds.H12Frame05);
            LiveOpsTimelineElement timeline = FrameTimeline();
            LiveOpsTimelineDragController drag = timeline.DragController;
            Assert.IsTrue(drag.IsDragging, "khung 5 là tư thế đang kéo thân, không phải đã thả");
            Assert.AreEqual(new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc), drag.PreviewStartUtc);
            Assert.AreEqual(new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc), drag.PreviewEndUtc);
            Assert.AreEqual(0, drag.PreviewOverlaps.Count, "dời +12 giờ là vừa đủ hết chồng giờ với hunt-0914");
            Assert.IsTrue(timeline.FindBar(LiveOpsDesignSample.HuntBonusEntryKey)
                .ClassListContains(LiveOpsHubClassNames.TimelineBarDragging));
        }

        [UnityTest]
        public IEnumerator Frame06_ResizeEndReadoutSaysEndMovedOneDay()
        {
            yield return OpenFrame(LiveOpsHubCaptureScenarioIds.H12Frame06);
            LiveOpsTimelineElement timeline = FrameTimeline();
            Assert.AreEqual(FrameEdgeRangeStartUtc, timeline.RangeStartUtc, "khung 6 dùng trục riêng 16/9 → 21/9");
            Assert.AreEqual(FrameEdgeRangeEndUtc, timeline.RangeEndUtc);
            LiveOpsTimelineDragController drag = timeline.DragController;
            Assert.AreEqual(LiveOpsTimelineDragController.DragGesture.ResizeEnd, drag.Gesture);
            Assert.AreEqual(new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc), drag.PreviewStartUtc,
                "kéo mép cuối không được dời mép đầu");
            Assert.AreEqual(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc), drag.PreviewEndUtc);
            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.Vietnamese))
            {
                string readout = drag.ReadoutMainText(new LiveOpsHubFormat(LiveOpsDesignSample.DeviceOffset));
                StringAssert.Contains("20/9 00:00", readout, "readout nêu mép đang kéo [SD1 §3.7]");
                StringAssert.Contains("07:00", readout, "và giờ máy của mép đó (+7)");
                StringAssert.Contains("3 ngày", readout, "17/9 → 20/9 là 3 ngày");
            }
        }

        [UnityTest]
        public IEnumerator Frame07_ResizeIntoOverlapMarksBonusWillDrop()
        {
            yield return OpenFrame(LiveOpsHubCaptureScenarioIds.H12Frame07);
            LiveOpsTimelineElement timeline = FrameTimeline();
            LiveOpsTimelineDragController drag = timeline.DragController;
            Assert.AreEqual(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc), drag.PreviewEndUtc,
                "khung 7 kéo mép cuối hunt-0914 tới 17/9 12:00");
            Assert.AreEqual("hunt-0916-bonus", drag.OverlapWithEventId, "chồng với đúng đợt bonus đã dời ở khung 5");
            Assert.AreEqual(TimeSpan.FromHours(12), drag.OverlapDuration, "chồng 12 giờ");
            CollectionAssert.Contains(drag.WillDropBarKeys, LiveOpsDesignSample.HuntBonusEntryKey);
            Assert.IsTrue(timeline.FindBar(LiveOpsDesignSample.HuntBonusEntryKey)
                    .ClassListContains(LiveOpsHubClassNames.TimelineBarWillDrop),
                "xem trước hiện TRƯỚC khi thả, không đợi sau [SD1 §3.8 khung 7]");
        }

        [UnityTest]
        public IEnumerator Frame08_DeleteRemovesEventAndShowsShortToast()
        {
            yield return OpenFrame(LiveOpsHubCaptureScenarioIds.H12Frame08);
            LiveOpsTimelineElement timeline = FrameTimeline();
            Assert.IsNull(timeline.FindBar(LiveOpsDesignSample.HuntBonusEntryKey),
                "khung 8: đợt chưa đăng và chưa bắt đầu bị xoá NGAY, không hộp xác nhận");
            LiveOpsToast toast = FrameCanvas().Q<LiveOpsToast>();
            Assert.IsNotNull(toast, "toast vẽ trong chính khung ảnh 803×420");
            Assert.IsTrue(toast.IsVisible);
            StringAssert.Contains("hunt-0916-bonus", toast.Model.Message);
            Assert.IsFalse(toast.Model.Message.Contains("12:00"),
                "khung 803px dưới ngưỡng 1100px: toast rút còn ngày, giờ đủ nằm ở tooltip");
            StringAssert.Contains("12:00", toast.Model.Tooltip);
            Assert.IsTrue(toast.Model.HasUndo, "xoá xong luôn còn một bước Hoàn tác");
        }

        [UnityTest]
        public IEnumerator Frame11_MonthZoomShowsFortyTwoDayRange()
        {
            yield return OpenFrame(LiveOpsHubCaptureScenarioIds.H12Frame11);
            LiveOpsTimelineElement timeline = FrameTimeline();
            Assert.IsFalse(timeline.IsContinuousScale, "khung 11 là zoom preset Tháng, không phải zoom liên tục");
            Assert.AreEqual(new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc), timeline.RangeStartUtc,
                "42 ngày từ 8/9 [SD1 §3.8 khung 11]");
            Assert.AreEqual(TimeSpan.FromDays(42), timeline.RangeEndUtc - timeline.RangeStartUtc);
            Assert.IsTrue(HasStripBar(timeline.FindLaneElement(SkyRaceLaneTypeId)),
                "sky-race gom thành MỘT dải — một element thay 42 thanh, giữ ngân sách vertex");
        }

        [UnityTest]
        public IEnumerator Frame13_StarTournamentLaneShowsNextEventChip()
        {
            yield return OpenFrame(LiveOpsHubCaptureScenarioIds.H12Frame13);
            LiveOpsTimelineElement timeline = FrameTimeline();
            LiveOpsTimelineLane lane = timeline.FindLaneElement(StarTournamentLaneTypeId);
            Assert.IsNotNull(lane);
            Assert.AreEqual(0, lane.BarCount, "khung 13: làn không có thanh nào trong 14/9 → 19/9");
            Assert.IsFalse(lane.NextChip.ClassListContains(LiveOpsHubClassNames.TimelineHidden),
                "làn trống trong khoảng thì hiện chip Đợt tới ở mép phải");
            StringAssert.Contains("star-tournament-2026-10", lane.NextChipLabel.text);
        }

        [UnityTest]
        public IEnumerator Frame14_UndoRestoresTimesAndToastOffersRedo()
        {
            yield return OpenFrame(LiveOpsHubCaptureScenarioIds.H12Frame14);
            LiveOpsToast toast = FrameCanvas().Q<LiveOpsToast>();
            Assert.IsNotNull(toast);
            Assert.IsTrue(toast.IsVisible);
            Assert.IsTrue(toast.Model.IsUndone, "khung 14 là trạng thái SAU ⌘Z — toast nói 'Đã hoàn tác: …' và mời Làm lại");
            StringAssert.Contains("hunt-0916-bonus", toast.Model.Message);
            LiveOpsTimelineElement timeline = FrameTimeline();
            LiveOpsTimelineBar bar = timeline.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            Assert.IsNotNull(bar, "hoàn tác xong đợt phải trở lại trục");
            Assert.AreEqual(new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc), bar.Model.StartUtc,
                "và trở về đúng giờ trước khi kéo — vẽ lại trong một frame, không spinner");
        }

        private static bool HasStripBar(LiveOpsTimelineLane lane)
        {
            if (lane == null) return false;
            for (int index = 0; index < lane.BarCount; index++)
            {
                if (lane.BarAt(index).Model.IsStrip) return true;
            }
            return false;
        }

        /// <summary>Mở đúng cửa sổ mà lệnh chụp mở cho kịch bản đó, rồi chờ thêm số khung ổn định của chính kịch bản.</summary>
        private IEnumerator OpenFrame(string scenarioId)
        {
            LiveOpsHubCaptureScenario scenario = LiveOpsHubCaptureScenarios.Find(scenarioId);
            Assert.IsNotNull(scenario, "kịch bản chụp '" + scenarioId + "' chưa đăng ký");
            _window = (LiveOpsHubWindow)scenario.OpenWindow();
            _window.position = new Rect(0, 0, scenario.WindowWidth, scenario.WindowHeight);
            yield return WaitForLayout(_window.rootVisualElement);
            for (int frame = 0; frame < scenario.MinimumSettleFrames; frame++) yield return null;
        }

        private VisualElement FrameCanvas()
        {
            VisualElement canvas = _window.rootVisualElement.Q(LiveOpsHubCaptureScenarios.CalendarFrameCanvasElementName);
            Assert.IsNotNull(canvas, "khung ảnh 803×420 của Hình 12 phải nằm trong cửa sổ");
            return canvas;
        }

        private LiveOpsTimelineElement FrameTimeline()
        {
            LiveOpsTimelineElement timeline = FrameCanvas().Q<LiveOpsTimelineElement>();
            Assert.IsNotNull(timeline, "khung Hình 12 dựng trên chính control timeline của W3");
            return timeline;
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
