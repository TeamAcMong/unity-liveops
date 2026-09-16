using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using UnityEditor;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng Calendar (G-CALENDAR, W4) của ma trận 9.5: Hình 1 mặc định và ba trạng thái inspector (a)/(b)/(d),
    /// hai hộp xác nhận mà màn Lịch mở (Hình 8), và bốn bước của popover Thêm đợt (Hình 13b). Mọi kịch bản dựng trên PHIÊN THẬT
    /// từ lịch mẫu (<see cref="LiveOpsHubTestServices.DesignSampleScenario"/>) — không nặn tay trạng thái, để ảnh là thứ người
    /// dùng thật sự thấy.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        private const int CalendarWidth = 1280;
        private const int CalendarHeight = 760;
        /// <summary>
        /// Cửa sổ chụp popover: rail của hub chiếm 196px, nên cột nội dung = bề rộng cửa sổ − 196. Cửa sổ 420 chỉ chừa 224px và
        /// popover 320px bị CẮT — đó là lý do mọi ảnh Hình 13b của lượt trước rộng đúng 224. 560 − 196 = 364, đủ chỗ cho 320.
        /// </summary>
        private const int AddEventPopoverWindowWidth = 560;
        private const int AddEventPopoverWindowHeight = 460;

        /// <summary>Bề rộng inspector đợt của màn Lịch [SD1 §3.9].</summary>
        private const float CalendarInspectorWidth = 280f;
        private const string AddEventCaptureSectionId = "capture-add-event";

        /// <summary>18/9 10:00 UTC — mốc giả định của hình hộp "Rút ngắn đợt đang chạy" [SD1 §3.15]: lava-quest-2026-09b đang chạy.</summary>
        private static readonly DateTime CalendarRunningMomentUtc = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);

        static partial void RegisterCalendar(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H01CalendarDefault, CalendarWidth, CalendarHeight,
                () => OpenCalendar(LiveOpsDesignSample.HuntBonusEntryKey)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H01aInspectorEmpty, CalendarWidth, CalendarHeight,
                    () => OpenCalendar(string.Empty), CalendarInspectorOf)
                .WithExpectedFrames(CalendarInspectorExpectedFrame()));

            // Thanh sinh từ luật: khoá thanh của timeline là "<loại>#<chỉ số>" — inspector nhận đúng luật weekly-pass.
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H01bInspectorRecurring, CalendarWidth, CalendarHeight,
                    () => OpenCalendar("weekly-pass#35"), CalendarInspectorOf)
                .WithExpectedFrames(CalendarInspectorExpectedFrame()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H01dInspectorUnreadable, CalendarWidth, CalendarHeight,
                    () => OpenCalendar(LiveOpsDesignSample.LavaQuestLateEntryKey), CalendarInspectorOf)
                .WithExpectedFrames(CalendarInspectorExpectedFrame()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H08aConfirmShortenRunning,
                    (int)LiveOpsConfirmWindow.Width, (int)LiveOpsConfirmWindow.Level1Height, OpenShortenRunningConfirm,
                    window => ((LiveOpsConfirmWindow)window).Content)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsConfirmContent.RootElementName,
                    LiveOpsConfirmWindow.Width, LiveOpsConfirmWindow.Level1Height)));

            // Biến thể "không có số" của Hình 8: ở P1 hub CHƯA đọc bản ghi người chơi (7.0) nên hộp luôn ở dạng này —
            // hai id chụp cùng một thân hộp cho tới khi có LiveOpsStateReader (ghi ở contract-changes-G-CALENDAR.md).
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H08fConfirmShortenRunningNoNumber,
                    (int)LiveOpsConfirmWindow.Width, (int)LiveOpsConfirmWindow.Level1Height, OpenShortenRunningConfirm,
                    window => ((LiveOpsConfirmWindow)window).Content)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsConfirmContent.RootElementName,
                    LiveOpsConfirmWindow.Width, LiveOpsConfirmWindow.Level1Height)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H08bConfirmDeletePublished,
                    (int)LiveOpsConfirmWindow.Width, (int)LiveOpsConfirmWindow.Level1Height, OpenDeletePublishedConfirm,
                    window => ((LiveOpsConfirmWindow)window).Content)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsConfirmContent.RootElementName,
                    LiveOpsConfirmWindow.Width, LiveOpsConfirmWindow.Level1Height)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H13bAddEventStep1, AddEventPopoverWindowWidth,
                    AddEventPopoverWindowHeight, () => OpenAddEventPopover(AddEventFlowModel.StepChooseType), AddEventPopoverRootOf)
                .WithExpectedFrames(AddEventPopoverExpectedFrame()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H13bAddEventStep2, AddEventPopoverWindowWidth,
                    AddEventPopoverWindowHeight, () => OpenAddEventPopover(AddEventFlowModel.StepChooseTimes), AddEventPopoverRootOf)
                .WithExpectedFrames(AddEventPopoverExpectedFrame()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H13bAddEventStep3, AddEventPopoverWindowWidth,
                    AddEventPopoverWindowHeight, () => OpenAddEventPopover(AddEventFlowModel.StepReview), AddEventPopoverRootOf)
                .WithExpectedFrames(AddEventPopoverExpectedFrame()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H13bAddEventOverlap, AddEventPopoverWindowWidth,
                    AddEventPopoverWindowHeight, OpenAddEventPopoverWithOverlap, AddEventPopoverRootOf)
                .WithExpectedFrames(AddEventPopoverExpectedFrame()));

            RegisterCalendarFrames(scenarios);
        }

        private static EditorWindow OpenCalendar(string selectedBarKey)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            LiveOpsHubWindow window = LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Calendar);
            CalendarSection calendar = (CalendarSection)sections[2];
            calendar.Presenter.SetSelectedBarKey(selectedBarKey);
            return window;
        }

        private static VisualElement CalendarInspectorOf(EditorWindow window)
        {
            return window.rootVisualElement.Q(LiveOpsHubPaths.CalendarElementNames.Inspector);
        }

        /// <summary>Inspector đợt rộng đúng 280px [SD1 §3.9]. Không khai khung này thì ảnh (a)/(b)/(d) không đo gì và
        /// "MEASURE OK" là lời khen rỗng — bề rộng từng trôi 252/458/833px mà cổng vẫn xanh.</summary>
        private static LiveOpsHubCaptureExpectedFrame CalendarInspectorExpectedFrame()
        {
            return new LiveOpsHubCaptureExpectedFrame(LiveOpsHubPaths.CalendarElementNames.Inspector, CalendarInspectorWidth, 0f);
        }

        /// <summary>Popover Thêm đợt rộng 320px [SD1 §3.11] — cùng lý do với khung inspector.</summary>
        private static LiveOpsHubCaptureExpectedFrame AddEventPopoverExpectedFrame()
        {
            return new LiveOpsHubCaptureExpectedFrame(LiveOpsPopoverContent.RootElementName, AddEventPopover.PopoverWidth, 0f);
        }

        /// <summary>Hộp rút ngắn dựng từ CHÍNH presenter trên phiên thật, không phải chữ dán — câu trong ảnh là câu code sinh ra.</summary>
        private static EditorWindow OpenShortenRunningConfirm()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            ((ManualLiveOpsClock)services.Clock).Set(CalendarRunningMomentUtc);
            CalendarTimelinePresenter presenter = new CalendarTimelinePresenter(services);
            services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey, out FixedLiveEventEntry before);
            FixedLiveEventEntry after = before.WithTimes(before.StartUtcText, "2026-09-19T00:00:00Z");
            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.ChangeFixedEventTimes,
                services.Session.Document, DocumentWith(services, after), services.Session.Publish.ActiveBaseline,
                CalendarRunningMomentUtc, before.EntryKey);
            return LiveOpsConfirmWindow.OpenForTest(presenter.BuildShortenRequest(decision, before, after));
        }

        private static EditorWindow OpenDeletePublishedConfirm()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            CalendarDeleteFlow flow = CalendarDeleteFlow.For(services.Session, LiveOpsDesignSample.HuntEarlyEntryKey,
                services.Clock.UtcNow, services.Format);
            return LiveOpsConfirmWindow.OpenForTest(flow.BuildConfirmRequest(LiveOpsHubKeyLabels.Undo));
        }

        private static LiveEventCalendarDocument DocumentWith(LiveOpsHubServices services, FixedLiveEventEntry entry)
        {
            LiveEventCalendarEdits.TryApply(services.Session.Document, new ReplaceFixedEventEdit(entry),
                out LiveEventCalendarDocument result);
            return result ?? services.Session.Document;
        }

        /// <summary>
        /// Ba bước của Hình 13b trên CÙNG một mốc 21/9 00:00 + 72 giờ: id đề nghị <c>hunt-0921</c> ở bước 3 chỉ khớp khi bước 2
        /// đã ở 21/9. Trước đây bước 2 để nguyên mặc định "ngày mai" (14/9) nên hai ảnh kể hai câu chuyện khác nhau.
        /// </summary>
        private static EditorWindow OpenAddEventPopover(int step)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            AddEventFlowModel flow = AddEventFlowModel.Create(services.Session, services.Clock.UtcNow);
            if (step >= AddEventFlowModel.StepChooseTimes)
            {
                flow = flow.WithType("treasure-hunt").Next().WithTimes("2026-09-21", "00:00", 72);
            }
            if (step >= AddEventFlowModel.StepReview) flow = flow.Next();
            return OpenAddEventPopoverHost(services, flow);
        }

        /// <summary>Biến thể chồng giờ của Hình 13b: 15/9 00:00 + 72 giờ đè lên hunt-0914 nên kiểm nhanh báo game sẽ bỏ đợt.</summary>
        private static EditorWindow OpenAddEventPopoverWithOverlap()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            AddEventFlowModel flow = AddEventFlowModel.Create(services.Session, services.Clock.UtcNow)
                .WithType("treasure-hunt")
                .Next()
                .WithTimes("2026-09-15", "00:00", 72)
                .Next();
            return OpenAddEventPopoverHost(services, flow);
        }

        /// <summary>
        /// Popover là cửa sổ riêng lúc chạy thật, nhưng ảnh cần một panel có token của hub: dựng cây popover trong thân một màn
        /// giả của chính cửa sổ hub rồi cắt ảnh theo gốc popover — cùng cách <c>hf-toast-bare</c> đã dùng cho toast.
        /// </summary>
        private static EditorWindow OpenAddEventPopoverHost(LiveOpsHubServices services, AddEventFlowModel flow)
        {
            AddEventPopover popover = new AddEventPopover(services, flow, _ => { });
            FakeHubSection section = new FakeHubSection(AddEventCaptureSectionId, LiveOpsHubStrings.ShellCalendarTitle,
                LiveOpsHubStrings.ShellCalendarSubtitle, PipelineStage.Schedule)
            {
                ViewFactory = () => BuildAddEventPopoverView(popover),
                RequiredElementNames = LiveOpsHubPaths.RequiredAddEventPopoverElementNames,
            };
            return LiveOpsHubWindow.OpenWithServices(services, new List<IHubSection> { section }, section.Id);
        }

        /// <summary>
        /// Dựng qua <c>BuildForTest</c> của lớp gốc: nó bọc nội dung trong gốc có token + sheet của popover, đúng cây mà cửa sổ
        /// popover thật dựng. Thân màn giả rộng hơn 320 nên ghim bề rộng cửa sổ popover vào chính gốc đó.
        /// </summary>
        private static VisualElement BuildAddEventPopoverView(AddEventPopover popover)
        {
            VisualElement popoverRoot = popover.BuildForTest();
            popoverRoot.style.width = AddEventPopover.PopoverWidth;
            popoverRoot.style.flexShrink = 0;
            popoverRoot.style.alignSelf = Align.FlexStart;
            return popoverRoot;
        }

        private static VisualElement AddEventPopoverRootOf(EditorWindow window)
        {
            return window.rootVisualElement.Q(LiveOpsPopoverContent.RootElementName);
        }

        // ================================================================================================ Hình 12 [SD1 §3.8]

        /// <summary>
        /// 803 = header làn 168 + track 635, đúng khung "timeline trần" của ma trận 9.5 dòng 12 (và của <c>ht-timeline-*</c>), nên
        /// toạ độ thanh trên ảnh Hình 12 so thẳng được với ảnh Hình 1.
        /// </summary>
        private const int CalendarFrameCanvasWidth = 803;
        private const int CalendarFrameCanvasHeight = 420;
        private const int CalendarFrameLaneWidth = 635;
        internal const string CalendarFrameCanvasElementName = "capture-calendar-frame";
        private const string CalendarFrameTimelineElementName = "capture-calendar-frame-timeline";
        private const string CalendarFrameSectionId = "capture-calendar-frame";

        /// <summary>Đủ cho thước + bốn làn + minimap + chú giải dựng xong và Painter2D vẽ một khung ổn định (cùng số với vùng Timeline).</summary>
        private const int CalendarFrameSettleFrames = 8;

        /// <summary>Trục chung của Hình 12: 14/9 00:00 → 19/9 00:00 UTC (5 ngày) — mọi khung trừ khung 6 và khung 11.</summary>
        private static readonly DateTime CalendarFrameRangeStartUtc = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime CalendarFrameRangeEndUtc = new DateTime(2026, 9, 19, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Ngoại lệ khung 6 [SD1 §3.8]: trục 16/9 → 21/9 để lava-quest-2026-09b nằm giữa khung khi kéo mép cuối.</summary>
        private static readonly DateTime CalendarFrameEdgeRangeStartUtc = new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime CalendarFrameEdgeRangeEndUtc = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Khung 13: con trỏ đứng giữa khoảng trống của làn star-tournament (16/9 12:00 UTC).</summary>
        private static readonly DateTime CalendarFrameCursorUtc = new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);

        private const string CalendarFrameHuntLaneTypeId = "treasure-hunt";
        private const string CalendarFrameLavaQuestLaneTypeId = "lava-quest";
        private const string CalendarFrameStarTournamentLaneTypeId = "star-tournament";

        /// <summary>Khung 5: dời thân hunt-0916-bonus +12 giờ → 17/9 00:00 → 18/9 12:00, vừa đủ hết chồng giờ với hunt-0914.</summary>
        private const double CalendarFrameMoveBodyShiftHours = 12d;

        /// <summary>Khung 6: kéo mép cuối lava-quest-2026-09b từ 19/9 sang 20/9 — readout "dời +1 ngày · dài 3 ngày".</summary>
        private const double CalendarFrameResizeEndShiftHours = 24d;

        /// <summary>Khung 7: kéo mép cuối hunt-0914 tới 17/9 12:00 → chồng 12 giờ với bonus đã dời ở khung 5.</summary>
        private const double CalendarFrameOverlapShiftHours = 12d;

        /// <summary>Khung 8/14: toast vẽ TRONG khung ảnh (left 8, right 8, bottom 28) đúng [SD1 §3.8].</summary>
        private const float CalendarFrameToastSideMargin = 8f;
        private const float CalendarFrameToastBottomMargin = 28f;

        /// <summary>Đồng hồ toast đứng yên trong ảnh: 6 giây đếm ngược không được tắt toast khi lượt chụp chạy chậm.</summary>
        private static double CalendarFrameFrozenClockSeconds()
        {
            return 0d;
        }

        /// <summary>
        /// Mười một khung trạng thái của Hình 12. Chín khung đầu chỉ là TƯ THẾ của control timeline (không sửa tài liệu) nên dựng trên
        /// host không phiên — giống hệt vùng Timeline; hai khung còn lại (8 xoá, 14 sau Undo) là HỆ QUẢ của một lệnh sửa nên phải chạy
        /// trên phiên thật qua <see cref="CalendarTimelinePresenter"/>.
        /// </summary>
        private static void RegisterCalendarFrames(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame01, () => OpenCalendarFrame(
                LiveOpsDesignSample.Document, PoseCalendarFrameIdle)));

            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame03a, () => OpenCalendarFrame(
                LiveOpsDesignSample.Document, PoseCalendarFrameSelectedWithFocus)));

            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame03b, () => OpenCalendarFrame(
                LiveOpsDesignSample.Document, PoseCalendarFrameSelectedWithoutFocus)));

            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame04, () => OpenCalendarFrame(
                LiveOpsDesignSample.Document, PoseCalendarFrameKeyboardFocus)));

            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame05, () => OpenCalendarFrame(
                LiveOpsDesignSample.Document, PoseCalendarFrameMoveBody)));

            // Khung 6 chạy trên bản tài liệu có 09b kết thúc 19/9 — kéo mép cuối sang 20/9 mới ra đúng "dời +1 ngày · dài 3 ngày".
            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame06, () => OpenCalendarFrame(
                CalendarFrameResizeEndDocument(), PoseCalendarFrameResizeEnd)));

            // Khung 7 tiếp từ khung 5: bonus đã nằm ở 17/9 00:00 → 18/9 12:00 trước khi kéo mép cuối hunt-0914.
            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame07, () => OpenCalendarFrame(
                CalendarFrameOverlapDocument(), PoseCalendarFrameResizeIntoOverlap)));

            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame08, OpenCalendarDeleteFrame));

            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame11, () => OpenCalendarFrame(
                LiveOpsDesignSample.Document, PoseCalendarFrameMonthZoom)));

            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame13, () => OpenCalendarFrame(
                LiveOpsDesignSample.Document, PoseCalendarFrameNextChip)));

            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame14, OpenCalendarUndoFrame));
        }

        /// <summary>Khung mong đợi đo hai mức: canvas 803×420 và mọi làn rộng 635 — đổi là ảnh Hình 12 hết so được với [SD1 §3.8].</summary>
        private static LiveOpsHubCaptureScenario CalendarFrameScenario(string id, Func<EditorWindow> openWindow)
        {
            return new LiveOpsHubCaptureScenario(id, CalendarWidth, CalendarHeight, openWindow,
                    window => window.rootVisualElement.Q(CalendarFrameCanvasElementName))
                .WithMinimumSettleFrames(CalendarFrameSettleFrames)
                .WithExpectedFrames(
                    new LiveOpsHubCaptureExpectedFrame(CalendarFrameCanvasElementName, CalendarFrameCanvasWidth, CalendarFrameCanvasHeight),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.TimelineLane, CalendarFrameLaneWidth, 0f));
        }

        private static LiveEventCalendarDocument CalendarFrameResizeEndDocument()
        {
            return TimelineViewInputs.WithFixedTimes(LiveOpsDesignSample.Document, LiveOpsDesignSample.LavaQuestMidEntryKey,
                "2026-09-17T00:00:00Z", "2026-09-19T00:00:00Z");
        }

        private static LiveEventCalendarDocument CalendarFrameOverlapDocument()
        {
            return TimelineViewInputs.WithFixedTimes(LiveOpsDesignSample.Document, LiveOpsDesignSample.HuntBonusEntryKey,
                "2026-09-17T00:00:00Z", "2026-09-18T12:00:00Z");
        }

        // ------------------------------------------------------------------------------------ host không phiên (khung 1,3,4,5,6,7,11,13)

        private static EditorWindow OpenCalendarFrame(LiveEventCalendarDocument document, Action<LiveOpsTimelineElement> pose)
        {
            FakeHubSection section = new FakeHubSection(CalendarFrameSectionId, LiveOpsHubStrings.ShellCalendarTitle,
                LiveOpsHubStrings.ShellCalendarSubtitle, PipelineStage.Schedule)
            {
                ViewFactory = () => BuildCalendarFrameCanvas(document, pose),
                RequiredElementNames = new[] { CalendarFrameCanvasElementName },
            };
            return LiveOpsHubWindow.OpenForTest(new List<IHubSection> { section }, new ManualLiveOpsHubCompilationState(false), null,
                section.Id);
        }

        /// <summary>Khung ảnh cố định, KHÔNG padding: track phải còn đúng 635px sau khi trừ header làn 168px.</summary>
        private static VisualElement CreateCalendarFrameCanvas()
        {
            VisualElement canvas = new VisualElement { name = CalendarFrameCanvasElementName };
            canvas.style.width = CalendarFrameCanvasWidth;
            canvas.style.height = CalendarFrameCanvasHeight;
            canvas.style.flexShrink = 0;
            canvas.style.overflow = Overflow.Hidden;
            return canvas;
        }

        private static LiveOpsTimelineElement CreateCalendarFrameTimeline(VisualElement canvas)
        {
            LiveOpsTimelineElement timeline = new LiveOpsTimelineElement { name = CalendarFrameTimelineElementName };
            timeline.style.flexGrow = 1;
            timeline.SetDeviceOffset(LiveOpsDesignSample.DeviceOffset);
            canvas.Add(timeline);
            return timeline;
        }

        private static VisualElement BuildCalendarFrameCanvas(LiveEventCalendarDocument document, Action<LiveOpsTimelineElement> pose)
        {
            VisualElement canvas = CreateCalendarFrameCanvas();
            LiveOpsTimelineElement timeline = CreateCalendarFrameTimeline(canvas);

            Func<LiveOpsTimelineInput> inputFactory = TimelineViewInputs.Checked(document);
            // Đổi khung (pose) làm model phải dựng lại rồi đặt tư thế LẠI: SetModel dựng lại làn nên xem trước kéo và lựa chọn mất theo.
            timeline.RangeChanged += (startUtc, endUtc) =>
            {
                RebuildCalendarFrame(timeline, inputFactory);
                pose?.Invoke(timeline);
            };
            timeline.SetRange(LiveOpsTimelineGeometry.RangeStartFor(LiveOpsDesignSample.NowUtc, LiveOpsTimelineZoom.ThreeWeeks),
                LiveOpsTimelineZoom.ThreeWeeks);
            timeline.RegisterCallback<GeometryChangedEvent>(geometryEvent =>
            {
                RebuildCalendarFrame(timeline, inputFactory);
                pose?.Invoke(timeline);
            });
            return canvas;
        }

        private static void RebuildCalendarFrame(LiveOpsTimelineElement timeline, Func<LiveOpsTimelineInput> inputFactory)
        {
            timeline.SetModel(inputFactory()
                .WithRange(timeline.RangeStartUtc, timeline.RangeEndUtc)
                .WithTrackWidth(timeline.TrackWidth)
                .Build());
        }

        // ------------------------------------------------------------------------------------------------------------------ tư thế

        /// <summary>
        /// Đưa khung về đúng trục của Hình 12. <c>FrameInstance</c> là đường công khai duy nhất đặt được khoảng tuỳ ý, và nó thêm lề
        /// 10% mỗi bên, nên truyền vào khoảng đã thu 1/1,2 để mép khung rơi CHÍNH XÁC vào giờ thiết kế.
        /// Trả false khi vừa đổi khung — lần đổi đó tự phát <c>RangeChanged</c> và pose chạy lại, nên nơi gọi dừng lượt này.
        /// </summary>
        private static bool ApplyCalendarFrameRange(LiveOpsTimelineElement timeline, DateTime rangeStartUtc, DateTime rangeEndUtc,
            string laneTypeId)
        {
            if (timeline.RangeStartUtc == rangeStartUtc && timeline.RangeEndUtc == rangeEndUtc) return true;
            double marginHours = (rangeEndUtc - rangeStartUtc).TotalHours * (1d - 1d / (1d + LiveOpsTimelineElement.FrameMarginRatio * 2d)) / 2d;
            timeline.FrameInstance(laneTypeId, rangeStartUtc.AddHours(marginHours), rangeEndUtc.AddHours(-marginHours));
            return false;
        }

        private static void PoseCalendarFrameIdle(LiveOpsTimelineElement timeline)
        {
            ApplyCalendarFrameRange(timeline, CalendarFrameRangeStartUtc, CalendarFrameRangeEndUtc, CalendarFrameHuntLaneTypeId);
        }

        /// <summary>Khung 3 nửa trái: hunt-0914 đã chọn VÀ timeline đang giữ focus (nền highlight đậm).</summary>
        private static void PoseCalendarFrameSelectedWithFocus(LiveOpsTimelineElement timeline)
        {
            if (!ApplyCalendarFrameRange(timeline, CalendarFrameRangeStartUtc, CalendarFrameRangeEndUtc, CalendarFrameHuntLaneTypeId)) return;
            timeline.Select(LiveOpsDesignSample.HuntEarlyEntryKey, false);
            timeline.Focus();
        }

        /// <summary>
        /// Khung 3 nửa phải: hunt-0916-bonus đã chọn nhưng focus ĐÃ RỜI timeline (highlight-inactive + viền quiet). Không gọi
        /// <c>Focus()</c> là đủ — element chỉ mang class <c>--has-focus</c> sau <c>FocusInEvent</c>.
        /// </summary>
        private static void PoseCalendarFrameSelectedWithoutFocus(LiveOpsTimelineElement timeline)
        {
            if (!ApplyCalendarFrameRange(timeline, CalendarFrameRangeStartUtc, CalendarFrameRangeEndUtc, CalendarFrameHuntLaneTypeId)) return;
            timeline.Select(LiveOpsDesignSample.HuntBonusEntryKey, false);
        }

        /// <summary>Khung 4: focus bàn phím — <c>Select(…, focus: true)</c> vừa lấy focus vừa bật viền focus trên đúng thanh.</summary>
        private static void PoseCalendarFrameKeyboardFocus(LiveOpsTimelineElement timeline)
        {
            if (!ApplyCalendarFrameRange(timeline, CalendarFrameRangeStartUtc, CalendarFrameRangeEndUtc, CalendarFrameHuntLaneTypeId)) return;
            timeline.Select(LiveOpsDesignSample.HuntEarlyEntryKey, true);
        }

        private static void PoseCalendarFrameMoveBody(LiveOpsTimelineElement timeline)
        {
            if (!ApplyCalendarFrameRange(timeline, CalendarFrameRangeStartUtc, CalendarFrameRangeEndUtc, CalendarFrameHuntLaneTypeId)) return;
            PoseCalendarFrameDrag(timeline, CalendarFrameHuntLaneTypeId, LiveOpsDesignSample.HuntBonusEntryKey,
                LiveOpsTimelineBarRegion.Body, CalendarFrameMoveBodyShiftHours);
        }

        private static void PoseCalendarFrameResizeEnd(LiveOpsTimelineElement timeline)
        {
            if (!ApplyCalendarFrameRange(timeline, CalendarFrameEdgeRangeStartUtc, CalendarFrameEdgeRangeEndUtc,
                CalendarFrameLavaQuestLaneTypeId)) return;
            PoseCalendarFrameDrag(timeline, CalendarFrameLavaQuestLaneTypeId, LiveOpsDesignSample.LavaQuestMidEntryKey,
                LiveOpsTimelineBarRegion.EndEdge, CalendarFrameResizeEndShiftHours);
        }

        private static void PoseCalendarFrameResizeIntoOverlap(LiveOpsTimelineElement timeline)
        {
            if (!ApplyCalendarFrameRange(timeline, CalendarFrameRangeStartUtc, CalendarFrameRangeEndUtc, CalendarFrameHuntLaneTypeId)) return;
            PoseCalendarFrameDrag(timeline, CalendarFrameHuntLaneTypeId, LiveOpsDesignSample.HuntEarlyEntryKey,
                LiveOpsTimelineBarRegion.EndEdge, CalendarFrameOverlapShiftHours);
        }

        /// <summary>Khung 11: zoom Tháng = 42 ngày từ 8/9 (preset, không phải zoom liên tục) — sky-race gom thành một dải.</summary>
        private static void PoseCalendarFrameMonthZoom(LiveOpsTimelineElement timeline)
        {
            DateTime monthRangeStartUtc = LiveOpsTimelineGeometry.RangeStartFor(LiveOpsDesignSample.NowUtc, LiveOpsTimelineZoom.Month);
            if (!timeline.IsContinuousScale && timeline.Zoom == LiveOpsTimelineZoom.Month
                && timeline.RangeStartUtc == monthRangeStartUtc) return;
            timeline.SetRange(monthRangeStartUtc, LiveOpsTimelineZoom.Month);
        }

        /// <summary>
        /// Khung 13: làn star-tournament không có thanh nào trong 14/9 → 19/9 nên làn tự hiện chip "Đợt tới: …" ở mép phải. Chỉ cần
        /// cuộn làn đó vào khung — chip do chính model làn dựng, không đặt tay.
        /// </summary>
        private static void PoseCalendarFrameNextChip(LiveOpsTimelineElement timeline)
        {
            if (!ApplyCalendarFrameRange(timeline, CalendarFrameRangeStartUtc, CalendarFrameRangeEndUtc,
                CalendarFrameStarTournamentLaneTypeId)) return;
            // Con trỏ đang ở TRONG làn trống — đúng tình huống mà [SD1 §3.8 khung 13] mô tả ("nhấp đúp chỗ trống để thêm đợt").
            // Không có tư thế này thì khung 13 trùng từng byte với khung 1 và không chứng minh được trạng thái nào cả.
            // (nợ D-3(b)) Phải nói RÕ làn nào và "đang ở chỗ trống": tư thế một tham số chỉ đặt vạch giờ, dòng gợi ý vẫn nói về
            // đợt đã chọn nên microcopy "nhấp đúp chỗ trống để thêm đợt" không bao giờ lọt vào ảnh.
            timeline.SetCursor(CalendarFrameCursorUtc, CalendarFrameStarTournamentLaneTypeId, true);
        }

        /// <summary>
        /// Tư thế "đang kéo" dựng bằng chính <see cref="LiveOpsTimelineDragController"/> — bắt chuột thật không chạy được trong lượt
        /// chụp batchmode (cùng lý do với <c>ht-timeline-drag-overlap</c>). Bắt LẠI cử chỉ ở mỗi lượt pose: <c>SetModel</c> dựng lại làn
        /// nên giữ cử chỉ cũ là giữ một <see cref="LiveOpsTimelineGeometry"/> đã chết và xem trước sẽ lệch giờ.
        /// <c>Select</c> gọi SAU khi đã kéo để dòng gợi ý đọc trạng thái "đang kéo", đúng thứ tự người dùng gây ra.
        /// </summary>
        private static void PoseCalendarFrameDrag(LiveOpsTimelineElement timeline, string laneTypeId, string barKey,
            LiveOpsTimelineBarRegion region, double shiftHours)
        {
            LiveOpsTimelineLane lane = timeline.FindLaneElement(laneTypeId);
            LiveOpsTimelineBar bar = timeline.FindBar(barKey);
            if (lane == null || bar == null || lane.Geometry == null) return;
            timeline.DragController.Cancel();

            float startX = lane.Geometry.XOf(bar.Model.StartUtc);
            float endX = lane.Geometry.XOf(bar.Model.EndUtc);
            float grabPosition = region == LiveOpsTimelineBarRegion.EndEdge ? endX : startX + (endX - startX) / 2f;
            if (!timeline.DragController.BeginBar(lane.Model, lane.Geometry, bar.Model, region, grabPosition)) return;
            timeline.DragController.Move(grabPosition + (float)(shiftHours * lane.Geometry.PixelsPerHour), false);
            timeline.Select(barKey, false);
            timeline.RefreshDragVisuals();
        }

        // ------------------------------------------------------------------------------------------ host có phiên (khung 8 và 14)

        private static EditorWindow OpenCalendarDeleteFrame()
        {
            return new CalendarActionFrameHost(DeleteBonusEvent, CalendarFrameHuntLaneTypeId).Open();
        }

        private static EditorWindow OpenCalendarUndoFrame()
        {
            return new CalendarActionFrameHost(MoveBonusEventThenUndo, CalendarFrameHuntLaneTypeId).Open();
        }

        /// <summary>
        /// Khung 8: ⌘⌫ trên đợt CHƯA đăng và CHƯA bắt đầu — presenter xoá ngay, không hộp, rồi phát toast dạng ngắn (khung 803px nằm
        /// dưới ngưỡng 1100px nên toast rút còn ngày, giờ đủ ở tooltip).
        /// </summary>
        private static void DeleteBonusEvent(CalendarTimelinePresenter presenter, LiveOpsToast toast)
        {
            presenter.SetSelectedBarKey(LiveOpsDesignSample.HuntBonusEntryKey);
            presenter.RequestDelete(LiveOpsDesignSample.HuntBonusEntryKey);
        }

        /// <summary>
        /// Khung 14: dời hunt-0916-bonus rồi ⌘Z. Đi đúng đường người dùng — ý định kéo (Preview → Commit) sinh một bước Undo mang tên
        /// câu toast, rồi nút Hoàn tác của chính toast gỡ đúng bước đó và toast đổi thành "Đã hoàn tác: …" kèm Làm lại.
        /// </summary>
        private static void MoveBonusEventThenUndo(CalendarTimelinePresenter presenter, LiveOpsToast toast)
        {
            DateTime movedStartUtc = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
            DateTime movedEndUtc = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
            presenter.SetSelectedBarKey(LiveOpsDesignSample.HuntBonusEntryKey);
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, movedStartUtc, movedEndUtc,
                LiveOpsTimelineGesturePhase.Preview));
            presenter.HandleIntent(new MoveBarIntent(LiveOpsDesignSample.HuntBonusEntryKey, movedStartUtc, movedEndUtc,
                LiveOpsTimelineGesturePhase.Commit));
            toast.HandleActionClicked();
        }

        /// <summary>
        /// Host của khung 8 và 14 [SD1 §3.8]: cùng mini timeline 803×420 nhưng chạy trên PHIÊN thật + <see cref="CalendarTimelinePresenter"/>
        /// + toast thật. Hai khung này là HỆ QUẢ của một lệnh sửa, nên tài liệu, bước Undo và câu toast phải do presenter sinh ra — dán
        /// chữ vào một toast rỗng sẽ cho ảnh đúng mà đường đi sai.
        /// </summary>
        private sealed class CalendarActionFrameHost
        {
            private readonly LiveOpsHubServices _services;
            private readonly CalendarTimelinePresenter _presenter;
            private readonly LiveOpsHubUndoTracker _undoTracker;
            private readonly LiveOpsToast _toast;
            private readonly Action<CalendarTimelinePresenter, LiveOpsToast> _action;
            private readonly string _laneTypeId;
            private LiveOpsTimelineElement _timeline;
            private bool _hasRunAction;

            public CalendarActionFrameHost(Action<CalendarTimelinePresenter, LiveOpsToast> action, string laneTypeId)
            {
                _action = action ?? throw new ArgumentNullException(nameof(action));
                _laneTypeId = laneTypeId;
                _services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
                _presenter = new CalendarTimelinePresenter(_services);
                // Bề rộng thân màn quyết định toast dài hay ngắn (ngưỡng 1100px): khung ảnh 803px → dạng ngắn của [SD1 §3.8] khung 8.
                _presenter.ContentWidth = CalendarFrameCanvasWidth;
                // Tracker phải sống TRƯỚC lệnh sửa: nó dựng lại hai ngăn Undo từ willFlushUndoRecord, tạo sau là mất bước vừa ghi.
                _undoTracker = new LiveOpsHubUndoTracker();
                _toast = new LiveOpsToast(_undoTracker, CalendarFrameFrozenClockSeconds);
            }

            public EditorWindow Open()
            {
                FakeHubSection section = new FakeHubSection(CalendarFrameSectionId, LiveOpsHubStrings.ShellCalendarTitle,
                    LiveOpsHubStrings.ShellCalendarSubtitle, PipelineStage.Schedule)
                {
                    ViewFactory = BuildView,
                    RequiredElementNames = new[] { CalendarFrameCanvasElementName },
                };
                return LiveOpsHubWindow.OpenWithServices(_services, new List<IHubSection> { section }, section.Id);
            }

            private VisualElement BuildView()
            {
                VisualElement canvas = CreateCalendarFrameCanvas();
                _timeline = CreateCalendarFrameTimeline(canvas);
                _timeline.IntentRaised += _presenter.HandleIntent;
                _presenter.ToastRequested += _toast.Show;
                // Nghe PHIÊN chứ không chỉ presenter: Undo của khung 14 đổi tài liệu ngoài đường của presenter, và khung 14 phải
                // vẽ lại trong đúng một frame sau ⌘Z ([SD1 §3.8] khung 14 — không spinner).
                _services.Session.DocumentChanged += Rebuild;
                _timeline.RangeChanged += (startUtc, endUtc) => Pose();
                _timeline.RegisterCallback<GeometryChangedEvent>(geometryEvent => Pose());

                _toast.style.position = Position.Absolute;
                _toast.style.left = CalendarFrameToastSideMargin;
                _toast.style.right = CalendarFrameToastSideMargin;
                _toast.style.bottom = CalendarFrameToastBottomMargin;
                canvas.Add(_toast);

                // Tắt chuyển động cho ảnh: toast hiện bằng transition opacity 0 → 1, chụp giữa chừng ra một toast mờ đọc không nổi.
                canvas.RegisterCallback<AttachToPanelEvent>(attachEvent => DisableMotionOnHubRoot(canvas));

                // Lệnh chụp đóng cửa sổ sau mỗi kịch bản: gỡ delegate Undo của tracker, nếu không nó sống tới domain reload kế tiếp.
                canvas.RegisterCallback<DetachFromPanelEvent>(detachEvent =>
                {
                    _services.Session.DocumentChanged -= Rebuild;
                    _undoTracker.Dispose();
                });
                return canvas;
            }

            private void Pose()
            {
                if (_timeline == null) return;
                Rebuild();
                if (!ApplyCalendarFrameRange(_timeline, CalendarFrameRangeStartUtc, CalendarFrameRangeEndUtc, _laneTypeId)) return;
                if (_hasRunAction) return;
                // Một lần duy nhất: pose chạy lại ở mỗi lượt layout, nhưng xoá/hoàn tác hai lần là hai bước Undo và sai cả ảnh lẫn tài liệu.
                _hasRunAction = true;
                // Và chạy ở LƯỢT SAU: lệnh sửa dựng lại toàn bộ cây làn, mà lúc này panel đang duyệt danh sách element chờ layout để
                // phát GeometryChangedEvent — đổi cây giữa chừng ném "Collection was modified" (thấy ở khung 14 khi ⌘Z).
                _timeline.schedule.Execute(RunAction);
            }

            private static void DisableMotionOnHubRoot(VisualElement canvas)
            {
                for (VisualElement current = canvas; current != null; current = current.hierarchy.parent)
                {
                    if (!current.ClassListContains(LiveOpsHubClassNames.Root)) continue;
                    current.AddToClassList(LiveOpsHubClassNames.NoMotion);
                    return;
                }
            }

            private void RunAction()
            {
                _action(_presenter, _toast);
                Rebuild();
            }

            private void Rebuild()
            {
                if (_timeline == null) return;
                _timeline.SetModel(_presenter.BuildModel(_timeline.RangeStartUtc, _timeline.RangeEndUtc, _timeline.TrackWidth));
                _timeline.Select(_presenter.SelectedBarKey, false);
            }
        }
    }
}
