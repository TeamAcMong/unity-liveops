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
        private const int AddEventPopoverWindowWidth = 420;
        private const int AddEventPopoverWindowHeight = 420;
        private const string AddEventCaptureSectionId = "capture-add-event";

        /// <summary>18/9 10:00 UTC — mốc giả định của hình hộp "Rút ngắn đợt đang chạy" [SD1 §3.15]: lava-quest-2026-09b đang chạy.</summary>
        private static readonly DateTime CalendarRunningMomentUtc = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);

        static partial void RegisterCalendar(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H01CalendarDefault, CalendarWidth, CalendarHeight,
                () => OpenCalendar(LiveOpsDesignSample.HuntBonusEntryKey)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H01aInspectorEmpty, CalendarWidth, CalendarHeight,
                () => OpenCalendar(string.Empty), CalendarInspectorOf));

            // Thanh sinh từ luật: khoá thanh của timeline là "<loại>#<chỉ số>" — inspector nhận đúng luật weekly-pass.
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H01bInspectorRecurring, CalendarWidth, CalendarHeight,
                () => OpenCalendar("weekly-pass#35"), CalendarInspectorOf));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H01dInspectorUnreadable, CalendarWidth, CalendarHeight,
                () => OpenCalendar(LiveOpsDesignSample.LavaQuestLateEntryKey), CalendarInspectorOf));

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
                AddEventPopoverWindowHeight, () => OpenAddEventPopover(AddEventFlowModel.StepChooseType), AddEventPopoverRootOf));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H13bAddEventStep2, AddEventPopoverWindowWidth,
                AddEventPopoverWindowHeight, () => OpenAddEventPopover(AddEventFlowModel.StepChooseTimes), AddEventPopoverRootOf));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H13bAddEventStep3, AddEventPopoverWindowWidth,
                AddEventPopoverWindowHeight, () => OpenAddEventPopover(AddEventFlowModel.StepReview), AddEventPopoverRootOf));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H13bAddEventOverlap, AddEventPopoverWindowWidth,
                AddEventPopoverWindowHeight, OpenAddEventPopoverWithOverlap, AddEventPopoverRootOf));
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

        private static EditorWindow OpenAddEventPopover(int step)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            AddEventFlowModel flow = AddEventFlowModel.Create(services.Session, services.Clock.UtcNow);
            if (step >= AddEventFlowModel.StepChooseTimes) flow = flow.WithType("treasure-hunt");
            if (step >= AddEventFlowModel.StepReview) flow = flow.WithTimes("2026-09-21", "00:00", 72);
            return OpenAddEventPopoverHost(services, flow);
        }

        /// <summary>Biến thể chồng giờ của Hình 13b: 15/9 00:00 + 72 giờ đè lên hunt-0914 nên kiểm nhanh báo game sẽ bỏ đợt.</summary>
        private static EditorWindow OpenAddEventPopoverWithOverlap()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            AddEventFlowModel flow = AddEventFlowModel.Create(services.Session, services.Clock.UtcNow)
                .WithType("treasure-hunt")
                .WithTimes("2026-09-15", "00:00", 72);
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
                ViewFactory = popover.Build,
                RequiredElementNames = LiveOpsHubPaths.RequiredAddEventPopoverElementNames,
            };
            return LiveOpsHubWindow.OpenWithServices(services, new List<IHubSection> { section }, section.Id);
        }

        private static VisualElement AddEventPopoverRootOf(EditorWindow window)
        {
            return window.rootVisualElement.Q(LiveOpsHubPaths.AddEventPopoverElementNames.Root);
        }
    }
}
