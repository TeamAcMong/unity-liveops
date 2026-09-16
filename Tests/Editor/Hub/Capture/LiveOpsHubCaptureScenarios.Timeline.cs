using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using UnityEditor;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng Timeline (G-TIMELINE-VIEW, W3): <c>ht-timeline-figure1</c> (Hình 1 — 3 tuần 8/9 → 29/9),
    /// <c>ht-timeline-month</c> (zoom Tháng, dải gom), <c>ht-timeline-drag-overlap</c> (đang kéo, xem trước "sẽ chồng" + readout) và
    /// (vá V-11) <c>ht-timeline-day-ruler</c> (zoom Ngày: thước ba tầng có tầng giờ máy + bubble giờ tại con trỏ). Cả bốn là "timeline
    /// trần" 803×420 = header làn 168 + track 635 — đúng bề rộng track của Hình 1, nên toạ độ thanh trên ảnh trùng số của
    /// <c>Geometry_Figure1_BarPositions</c>.
    ///
    /// Dựng như <c>hc-controls-gallery</c>: canvas riêng trong cửa sổ hub thật bằng màn giả, để có token hai skin + stylesheet theo đúng
    /// thứ tự nạp (theme → components → shell → feedback → controls → timeline → motion). KHÔNG dùng phiên
    /// (<c>LiveOpsHubTestServices</c>, <c>OpenWithServices</c>) — model dựng thẳng từ <see cref="LiveOpsDesignSample"/> + validator/diff
    /// core, đúng luật giữ độc lập của W3 (PLAN/w3/W3-DEPS.md).
    ///
    /// Tư thế "đang kéo" và "con trỏ đang ở 12:00" đặt thẳng qua <c>DragController</c> + <c>SetCursor</c> rồi
    /// <c>RefreshDragVisuals</c>: bắt con trỏ thật không chạy được trong lượt chụp batchmode. Đường chuột thật đã có test UI
    /// <c>Drag_WillOverlap_PreviewBeforeRelease</c> và <c>CursorBubble_FollowsPointer_HiddenOnLeave</c> phủ.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        internal const string TimelineCanvasElementName = "capture-timeline";

        /// <summary>803 = header làn 168 + track 635 (bề rộng track của Hình 1) — đổi số này là ảnh không còn đối chiếu được với [SD1 §3.3].</summary>
        private const int TimelineCanvasWidth = 803;
        private const int TimelineCanvasHeight = 420;

        /// <summary>Đủ cho thước + bốn làn + minimap + chú giải + dòng gợi ý dựng xong và Painter2D vẽ một khung ổn định.</summary>
        private const int TimelineSettleFrames = 8;

        /// <summary>Giờ dưới con trỏ ở <c>ht-timeline-day-ruler</c>: 13/9 12:00 UTC = 19:00 giờ máy (+7) — bubble và tầng 3 lệch nhau thấy rõ.</summary>
        private static readonly DateTime DayRulerCursorUtc = new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>Làn có vùng chồng sẵn của dữ liệu mẫu — nơi xem trước "sẽ chồng" đọc được nhất [SD1 §3.3].</summary>
        private const string TimelineCaptureTreasureHuntTypeId = "treasure-hunt";

        /// <summary>Kéo hunt-0916-bonus sang trái 57px ở 3 tuần (≈ 45 giờ): chui hẳn vào hunt-0914 nên xem trước là "sẽ chồng" rõ ràng.</summary>
        private const float DragOverlapShiftPixels = 57f;

        /// <summary>Làn nào cũng phủ đúng track 635px = 803 − header làn 168 [SD1 §3.2] — số này đúng ở cả bốn kịch bản, mọi zoom.</summary>
        private const int TimelineLaneWidth = 635;

        /// <summary>
        /// Thanh mốc của Hình 1 [SD1 §3.3]: lava-quest-2026-09a dài 72 giờ, ở 3 tuần (1,26 px/giờ) ra 90px. Chỉ khai cho
        /// <c>ht-timeline-figure1</c> — bề rộng thanh đổi theo zoom nên kịch bản Tháng/Ngày không dùng được số này.
        /// </summary>
        private const int TimelineFigure1LavaQuestEarlyWidth = 90;

        static partial void RegisterTimeline(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(TimelineScenario(LiveOpsHubCaptureScenarioIds.HtTimelineFigure1, LiveOpsTimelineZoom.ThreeWeeks, null,
                new LiveOpsHubCaptureExpectedFrame(LiveOpsDesignSample.LavaQuestEarlyEntryKey, TimelineFigure1LavaQuestEarlyWidth, 0f)));
            scenarios.Add(TimelineScenario(LiveOpsHubCaptureScenarioIds.HtTimelineMonth, LiveOpsTimelineZoom.Month, null));
            scenarios.Add(TimelineScenario(LiveOpsHubCaptureScenarioIds.HtTimelineDragOverlap, LiveOpsTimelineZoom.ThreeWeeks, PoseDragOverlap));
            scenarios.Add(TimelineScenario(LiveOpsHubCaptureScenarioIds.HtTimelineDayRuler, LiveOpsTimelineZoom.Day, PoseDayRulerCursor));
        }

        /// <summary>
        /// Khung mong đợi khai ba mức để measure-capture.py tự bắt hồi quy hình học thay vì phải đối chiếu tay trên
        /// <c>ht-timeline-figure1-&lt;skin&gt;.json</c>: canvas 803×420, mọi làn rộng 635 (khớp theo class), và — chỉ ở Hình 1 —
        /// thanh mốc lava-quest-2026-09a rộng 90 (khớp theo tên element = BarKey).
        /// </summary>
        private static LiveOpsHubCaptureScenario TimelineScenario(string id, LiveOpsTimelineZoom zoom, Action<LiveOpsTimelineElement> pose,
            params LiveOpsHubCaptureExpectedFrame[] extraFrames)
        {
            List<LiveOpsHubCaptureExpectedFrame> frames = new List<LiveOpsHubCaptureExpectedFrame>
            {
                new LiveOpsHubCaptureExpectedFrame(TimelineCanvasElementName, TimelineCanvasWidth, TimelineCanvasHeight),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.TimelineLane, TimelineLaneWidth, 0f),
            };
            if (extraFrames != null) frames.AddRange(extraFrames);
            return new LiveOpsHubCaptureScenario(id, StandardWidth, StandardHeight,
                    () => OpenTimelineCanvas(zoom, pose),
                    window => window.rootVisualElement.Q(TimelineCanvasElementName))
                .WithMinimumSettleFrames(TimelineSettleFrames)
                .WithExpectedFrames(frames.ToArray());
        }

        private static EditorWindow OpenTimelineCanvas(LiveOpsTimelineZoom zoom, Action<LiveOpsTimelineElement> pose)
        {
            FakeHubSection canvas = new FakeHubSection("timeline-capture", LiveOpsHubStrings.ShellWindowTitle, "G-TIMELINE-VIEW", PipelineStage.Configure)
            {
                ViewFactory = () => BuildTimelineCanvas(zoom, pose),
                RequiredElementNames = new[] { TimelineCanvasElementName },
            };
            return LiveOpsHubWindow.OpenForTest(new List<IHubSection> { canvas }, new ManualLiveOpsHubCompilationState(false), null, canvas.Id);
        }

        private static VisualElement BuildTimelineCanvas(LiveOpsTimelineZoom zoom, Action<LiveOpsTimelineElement> pose)
        {
            // Khung ảnh cố định, KHÔNG padding: track phải còn đúng 635px sau khi trừ header làn 168px.
            VisualElement canvas = new VisualElement { name = TimelineCanvasElementName };
            canvas.style.width = TimelineCanvasWidth;
            canvas.style.height = TimelineCanvasHeight;
            canvas.style.flexShrink = 0;
            canvas.style.overflow = Overflow.Hidden;

            LiveOpsTimelineElement timeline = new LiveOpsTimelineElement { name = "capture-timeline-element" };
            timeline.style.flexGrow = 1;
            timeline.SetDeviceOffset(LiveOpsDesignSample.DeviceOffset);
            canvas.Add(timeline);

            Func<LiveOpsTimelineInput> inputFactory = TimelineViewInputs.Checked(LiveOpsDesignSample.Document);
            timeline.RangeChanged += (startUtc, endUtc) => Rebuild(timeline, inputFactory);
            timeline.SetRange(LiveOpsTimelineGeometry.RangeStartFor(LiveOpsDesignSample.NowUtc, zoom), zoom);

            // Track chỉ có bề rộng thật sau layout; dựng lại model rồi đặt tư thế ở mỗi lần hình học đổi (cửa sổ chụp đổi cỡ sau Show).
            timeline.RegisterCallback<GeometryChangedEvent>(geometryEvent =>
            {
                Rebuild(timeline, inputFactory);
                pose?.Invoke(timeline);
            });
            return canvas;
        }

        private static void Rebuild(LiveOpsTimelineElement timeline, Func<LiveOpsTimelineInput> inputFactory)
        {
            timeline.SetModel(inputFactory()
                .WithRange(timeline.RangeStartUtc, timeline.RangeEndUtc)
                .WithTrackWidth(timeline.TrackWidth)
                .Build());
        }

        // ------------------------------------------------------------------------------------------------ ht-timeline-drag-overlap

        private static void PoseDragOverlap(LiveOpsTimelineElement timeline)
        {
            LiveOpsTimelineLane lane = timeline.FindLaneElement(TimelineCaptureTreasureHuntTypeId);
            // BarKey của một đợt cố định chính là EntryKey của nó (LiveOpsTimelineModel) — dải mới có BarKey tổng hợp.
            LiveOpsTimelineBar bar = timeline.FindBar(LiveOpsDesignSample.HuntBonusEntryKey);
            if (lane == null || bar == null || lane.Geometry == null) return;

            float center = lane.Geometry.XOf(bar.Model.StartUtc) + (lane.Geometry.XOf(bar.Model.EndUtc) - lane.Geometry.XOf(bar.Model.StartUtc)) / 2f;
            if (!timeline.DragController.IsActive)
            {
                timeline.Select(bar.Model.BarKey, false);
                if (!timeline.DragController.BeginBar(lane.Model, lane.Geometry, bar.Model, LiveOpsTimelineBarRegion.Body, center)) return;
            }
            timeline.DragController.Move(center - DragOverlapShiftPixels, false);
            timeline.RefreshDragVisuals();
        }

        // ------------------------------------------------------------------------------------------------ ht-timeline-day-ruler

        private static void PoseDayRulerCursor(LiveOpsTimelineElement timeline)
        {
            timeline.SetCursor(DayRulerCursorUtc);
        }
    }
}
