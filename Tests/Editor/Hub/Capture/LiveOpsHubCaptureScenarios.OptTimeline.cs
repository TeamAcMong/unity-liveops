using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng OptTimeline (G-OPT-TIMELINE, W6 — P1-lùi) của ma trận 9.5, hai khung còn thiếu của Hình 12:
    /// <list type="bullet">
    /// <item><c>h12-frame-09</c>: chọn nhiều + khung chọn — hai thanh treasure-hunt đang chọn, khung chọn nền highlight 0,2
    /// viền 1px, dòng gợi ý "2 đợt · treasure-hunt · ⌘-click bật tắt · Shift-click chọn dải".</item>
    /// <item><c>h12-frame-12</c>: làn thu gọn — weekly-pass và sky-race gập (chevron phải, dải 6px không nhãn), treasure-hunt
    /// mở (chevron xuống).</item>
    /// </list>
    /// Dùng lại đúng khung ảnh 803×420 và đường dựng của <see cref="RegisterCalendarFrames"/> (cùng lớp partial) để hai khung
    /// này so được từng pixel với mười một khung W4 và khung W5 — khung riêng là khung không đối chiếu được với gì cả.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        /// <summary>Lề quanh tập thanh của khung chọn: đủ để viền khung không dính vào viền thanh ở ảnh 803px.</summary>
        private const float OptTimelineMarqueeMargin = 6f;

        /// <summary>Khung 12 gập hai làn LẶP và để treasure-hunt mở — đúng [SD1 §3.8 khung 12], và cũng là ca thật nhất:
        /// làn lặp đặc thanh nhất nên là thứ người ta muốn gập trước.</summary>
        private static readonly string[] OptTimelineCollapsedLanes = { "weekly-pass", "sky-race" };

        private static readonly string[] OptTimelineSelectedBarKeys =
        {
            LiveOpsDesignSample.HuntEarlyEntryKey,
            LiveOpsDesignSample.HuntBonusEntryKey,
        };

        static partial void RegisterOptTimeline(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame09, () => OpenCalendarFrame(
                LiveOpsDesignSample.Document, PoseOptTimelineMultiSelect)));

            scenarios.Add(CalendarFrameScenario(LiveOpsHubCaptureScenarioIds.H12Frame12, OpenOptTimelineCollapsedFrame));
        }

        /// <summary>
        /// Khung 9: chọn hai thanh rồi vẽ khung chọn bao quanh chúng. Bắt con trỏ thật không chạy được trong lượt chụp
        /// batchmode (cùng lý do với tư thế kéo của khung 5–7), nên tư thế đi qua đúng hai đường mà cú kéo để lại:
        /// <c>SelectMany</c> và <c>ShowMarqueeAroundBars</c>.
        /// </summary>
        private static void PoseOptTimelineMultiSelect(LiveOpsTimelineElement timeline)
        {
            if (!ApplyCalendarFrameRange(timeline, CalendarFrameRangeStartUtc, CalendarFrameRangeEndUtc, CalendarFrameHuntLaneTypeId)) return;
            timeline.SelectMany(OptTimelineSelectedBarKeys, LiveOpsDesignSample.HuntBonusEntryKey, false);
            timeline.ShowMarqueeAroundBars(OptTimelineSelectedBarKeys, OptTimelineMarqueeMargin);
        }

        /// <summary>
        /// Khung 12 KHÔNG dựng bằng tư thế: làn thu gọn là dữ liệu ĐẦU VÀO của model (<c>WithCollapsedLanes</c>), không phải
        /// trạng thái của view. Dựng bằng tư thế thì mỗi lần model dựng lại là làn bung ra, đúng lỗi mà cờ ở model tránh.
        /// </summary>
        private static EditorWindow OpenOptTimelineCollapsedFrame()
        {
            FakeHubSection section = new FakeHubSection(CalendarFrameSectionId, LiveOpsHubStrings.ShellCalendarTitle,
                LiveOpsHubStrings.ShellCalendarSubtitle, PipelineStage.Schedule)
            {
                ViewFactory = BuildOptTimelineCollapsedCanvas,
                RequiredElementNames = new[] { CalendarFrameCanvasElementName },
            };
            return LiveOpsHubWindow.OpenForTest(new List<IHubSection> { section }, new ManualLiveOpsHubCompilationState(false), null,
                section.Id);
        }

        private static VisualElement BuildOptTimelineCollapsedCanvas()
        {
            VisualElement canvas = CreateCalendarFrameCanvas();
            LiveOpsTimelineElement timeline = CreateCalendarFrameTimeline(canvas);

            Func<LiveOpsTimelineInput> inputFactory = TimelineViewInputs.Checked(LiveOpsDesignSample.Document);
            Func<LiveOpsTimelineInput> collapsedFactory = () => inputFactory().WithCollapsedLanes(OptTimelineCollapsedLanes);
            timeline.RangeChanged += (startUtc, endUtc) =>
            {
                RebuildCalendarFrame(timeline, collapsedFactory);
                PoseOptTimelineCollapsed(timeline);
            };
            timeline.SetRange(LiveOpsTimelineGeometry.RangeStartFor(LiveOpsDesignSample.NowUtc, LiveOpsTimelineZoom.ThreeWeeks),
                LiveOpsTimelineZoom.ThreeWeeks);
            timeline.RegisterCallback<GeometryChangedEvent>(geometryEvent =>
            {
                RebuildCalendarFrame(timeline, collapsedFactory);
                PoseOptTimelineCollapsed(timeline);
            });
            return canvas;
        }

        private static void PoseOptTimelineCollapsed(LiveOpsTimelineElement timeline)
        {
            ApplyCalendarFrameRange(timeline, CalendarFrameRangeStartUtc, CalendarFrameRangeEndUtc, CalendarFrameHuntLaneTypeId);
        }
    }
}
