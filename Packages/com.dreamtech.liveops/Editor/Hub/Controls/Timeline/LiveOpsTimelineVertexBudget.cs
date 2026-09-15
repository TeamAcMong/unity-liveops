using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Ước lượng vertex của một làn / minimap TRƯỚC khi vẽ (R-10): ở 2022.3 một VisualElement vượt 65.535 vertex thì mất hình + log
    /// Error, còn 6000.6 không lỗi ở mọi cỡ — nên chỉ test trên 6000.6 là không thấy. Hệ số là số ĐO của SP-13 trên 2022.3 (Painter2D
    /// <c>Fill</c> hình chữ nhật ≈ 27–43 vertex, <c>Stroke</c> đoạn thẳng ≈ 5,5–8,2), không đoán.
    ///
    /// <para>Hợp đồng với control W3 (G-TIMELINE-VIEW): mỗi thanh vẽ tối đa <see cref="BarFillRectangles"/> hình Fill (thân + dải màu đáy)
    /// và <see cref="BarStrokeSegments"/> đoạn Stroke (viền), cộng một đoạn gạch khi bị bỏ và một Fill vuông "khác bản đã đăng"; nền làn
    /// vẽ vạch ngày, cột cuối tuần, cột hôm nay, vạch bây giờ, vùng chồng (nền + dải đỉnh + hai viền) và tối đa 80 nét gạch chéo. Nhãn,
    /// dấu cắt mép, icon là element/Label riêng nên không tính vào làn. View vẽ nhiều hơn thế thì phải sửa hệ số ở đây trước — test
    /// render <c>TimelineVertexBudgetRenderTests</c> (W3, 2022.3) là nơi đo lại.</para>
    ///
    /// <para>Với cách vẽ này một thanh ≈ 120 vertex (không phải 44 như phép tính "≈ 450 thanh Fill mỗi làn" của R-10), nên model gom dải
    /// ở khe lớn dần cho tới khi <see cref="EstimateLane"/> ≤ <see cref="LaneSafetyBudget"/> — thay vì đổi sang
    /// <c>MeshGenerationContext.Allocate</c> (4 vertex/quad) mà chưa ai đo trên 2022.3.</para>
    /// </summary>
    internal static class LiveOpsTimelineVertexBudget
    {
        public const int MaximumVerticesPerElement = 65535;
        public const int LaneSafetyBudget = 20000;       // chừa khoảng cho hai bản
        public const int FillVerticesPerRectangle = 44;  // SP-13 đo 2022.3: Fill hình chữ nhật ≈ 27–43 vertex, lấy trần an toàn
        public const int StrokeVerticesPerSegment = 8;   // SP-13 đo 2022.3: Stroke đoạn thẳng ≈ 5,5–8,2 vertex

        public const int BarFillRectangles = 2;
        public const int BarStrokeSegments = 4;
        public const int DroppedStrikeSegments = 1;
        public const int ChangedMarkerRectangles = 1;

        /// <summary>Tay nắm 2×12 ở hai mép chỉ vẽ trên MỘT thanh đang hover — tính một lần cho cả làn.</summary>
        public const int HoverHandleRectangles = 2;

        /// <summary>Cột hôm nay + vạch bây giờ 2px.</summary>
        public const int TodayAndNowRectangles = 2;

        public const int OverlapRectanglesPerRange = 2;
        public const int OverlapBorderSegmentsPerRange = 2;

        /// <summary>[SD1 §3.14]: gạch chéo tối đa 80 nét mỗi làn, chu kỳ 8px.</summary>
        public const int MaximumHatchSegmentsPerLane = 80;
        public const float HatchPeriod = 8f;

        public const int MinimapNowRectangles = 1;
        public const int MinimapPublishedFlagRectangles = 1;
        public const int MinimapPublishedFlagSegments = 2;
        public const int MinimapViewportSegments = 4;

        private const int DaysPerWeek = 7;

        public static int EstimateLane(LiveOpsTimelineLaneModel lane, LiveOpsTimelineGeometry geometry)
        {
            if (lane == null) throw new ArgumentNullException(nameof(lane));
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));

            long vertices = 0;
            double rangeDays = (geometry.RangeEndUtc - geometry.RangeStartUtc).TotalDays;
            long dayLines = (long)Math.Ceiling(rangeDays) + 1;
            long weekendColumns = (long)Math.Ceiling(rangeDays / DaysPerWeek) + 1;
            vertices += dayLines * StrokeVerticesPerSegment;
            vertices += weekendColumns * FillVerticesPerRectangle;
            vertices += TodayAndNowRectangles * FillVerticesPerRectangle;

            IReadOnlyList<(DateTime startUtc, DateTime endUtc)> overlaps = lane.OverlapRanges;
            long hatchSegments = 0;
            float laneRowsHeight = lane.RowCount * LiveOpsTimelineGeometry.RowPitch;
            for (int index = 0; index < overlaps.Count; index++)
            {
                vertices += OverlapRectanglesPerRange * FillVerticesPerRectangle + OverlapBorderSegmentsPerRange * StrokeVerticesPerSegment;
                float width = Math.Min(geometry.TrackWidth, geometry.SpanWidth(overlaps[index].startUtc, overlaps[index].endUtc));
                hatchSegments += (long)Math.Ceiling((width + laneRowsHeight) / HatchPeriod);
            }
            vertices += Math.Min(MaximumHatchSegmentsPerLane, hatchSegments) * StrokeVerticesPerSegment;

            IReadOnlyList<LiveOpsTimelineBarModel> bars = lane.Bars;
            for (int index = 0; index < bars.Count; index++)
            {
                LiveOpsTimelineBarModel bar = bars[index];
                vertices += BarFillRectangles * FillVerticesPerRectangle + BarStrokeSegments * StrokeVerticesPerSegment;
                if (bar.IsDropped) vertices += DroppedStrikeSegments * StrokeVerticesPerSegment;
                if (bar.IsChangedSincePublished) vertices += ChangedMarkerRectangles * FillVerticesPerRectangle;
            }
            if (bars.Count > 0) vertices += HoverHandleRectangles * FillVerticesPerRectangle;

            return vertices > int.MaxValue ? int.MaxValue : (int)vertices;
        }

        public static int EstimateMinimap(LiveOpsTimelineMinimapModel minimap)
        {
            if (minimap == null) throw new ArgumentNullException(nameof(minimap));

            long vertices = 0;
            for (int index = 0; index < minimap.Bands.Count; index++) vertices += (long)minimap.Bands[index].Segments.Count * FillVerticesPerRectangle;
            vertices += (long)minimap.Marks.Count * FillVerticesPerRectangle;
            vertices += MinimapNowRectangles * FillVerticesPerRectangle;
            if (minimap.PublishedAtUtc.HasValue)
            {
                vertices += MinimapPublishedFlagRectangles * FillVerticesPerRectangle + MinimapPublishedFlagSegments * StrokeVerticesPerSegment;
            }
            vertices += MinimapViewportSegments * StrokeVerticesPerSegment;
            return vertices > int.MaxValue ? int.MaxValue : (int)vertices;
        }
    }
}
