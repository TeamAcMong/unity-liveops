using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Track của một làn [SD1 §3.2, §3.14]: nền (cột cuối tuần, vạch ngày/thứ Hai, cột hôm nay) và vùng chồng giờ (nền + dải đỉnh
    /// + hai viền + gạch chéo 135° tối đa 80 nét) vẽ bằng Painter2D trên CHÍNH element này — mỗi làn một element nên một làn dày
    /// không kéo cả timeline vượt 65.535 vertex ở 2022.3 (R-10). Thanh, tag "bị bỏ", nhãn "chồng 12 giờ", chip "Đợt tới" là element
    /// con trong pool (luật style inline [FD §2.14] chỗ 3 và 8): vẽ sau nền nên luôn nằm trên nền.
    ///
    /// Làn không tự quyết điều gì: hàng phụ, vùng chồng, gom dải, chip đều đọc từ <see cref="LiveOpsTimelineLaneModel"/> đã dựng
    /// trong giới hạn ngân sách. Màu đọc từ token khai lại trên <c>.liveops-hub-timeline-lane</c> (custom property không kế thừa
    /// [API §12.2]) để đổi theo skin cùng nhịp phần còn lại.
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
#endif
    public sealed partial class LiveOpsTimelineLane : VisualElement
    {
        internal static readonly CustomStyleProperty<Color> WeekendColorProperty = new CustomStyleProperty<Color>("--liveops-hub-lane-weekend");
        internal static readonly CustomStyleProperty<Color> TodayColorProperty = new CustomStyleProperty<Color>("--liveops-hub-lane-today");
        internal static readonly CustomStyleProperty<Color> GridColorProperty = new CustomStyleProperty<Color>("--liveops-hub-lane-grid");
        internal static readonly CustomStyleProperty<Color> GridMondayColorProperty = new CustomStyleProperty<Color>("--liveops-hub-lane-grid-monday");
        internal static readonly CustomStyleProperty<Color> OverlapColorProperty = new CustomStyleProperty<Color>("--liveops-hub-lane-overlap");

        /// <summary>[SD1 §3.2]: lớp nền vùng chồng = blocked-fill × 0,16; gạch chéo opacity 0,35; dải đỉnh 3px.</summary>
        internal const float OverlapBackgroundAlpha = 0.16f;
        internal const float OverlapHatchAlpha = 0.35f;
        internal const float OverlapTopStripHeight = 3f;
        internal const float OverlapExtraHeight = 2f;
        internal const float LaneBottomPadding = 4f;
        internal const float DroppedTagGap = 4f;
        internal const float DroppedTagTopOffset = 2f;
        internal const float NextChipTopOffset = 5f;

        private const float LineWidth = 1f;
        private const float HalfPixel = 0.5f;
        private const int DaysPerWeek = 7;

        private readonly List<LiveOpsTimelineBar> _bars = new List<LiveOpsTimelineBar>();
        private readonly List<Label> _overlapLabels = new List<Label>();
        private readonly List<VisualElement> _droppedTags = new List<VisualElement>();
        private readonly Dictionary<string, VisualElement> _droppedTagByBarKey = new Dictionary<string, VisualElement>(StringComparer.Ordinal);
        private readonly VisualElement _nextChip;
        private readonly Label _nextChipLabel;

        private LiveOpsTimelineLaneModel _lane;
        private LiveOpsTimelineGeometry _geometry;
        private DateTime? _nowUtc;
        private IReadOnlyList<(DateTime startUtc, DateTime endUtc)> _previewOverlaps;
        private int _activeBarCount;
        private Color _weekendColor = Color.clear;
        private Color _todayColor = Color.clear;
        private Color _gridColor = Color.clear;
        private Color _gridMondayColor = Color.clear;
        private Color _overlapColor = Color.clear;

        public LiveOpsTimelineLane()
        {
            AddToClassList(LiveOpsHubClassNames.TimelineLane);
            pickingMode = PickingMode.Position;
            generateVisualContent += DrawBackground;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            _nextChip = new VisualElement { pickingMode = PickingMode.Position };
            _nextChip.AddToClassList(LiveOpsHubClassNames.Tag);
            _nextChip.AddToClassList(LiveOpsHubClassNames.TimelineNextChip);
            _nextChipLabel = new Label { pickingMode = PickingMode.Ignore };
            _nextChip.Add(_nextChipLabel);
            _nextChip.Add(new LiveOpsChevron(LiveOpsChevron.ChevronDirection.Right) { pickingMode = PickingMode.Ignore });
            _nextChip.RegisterCallback<ClickEvent>(OnNextChipClicked);
            _nextChip.AddToClassList(LiveOpsHubClassNames.TimelineHidden);
            Add(_nextChip);
        }

        /// <summary>
        /// Ước lượng vertex của phần làn đã dựng (<see cref="LiveOpsTimelineVertexBudget.EstimateLane"/> — tính cả thanh dù thanh là
        /// element riêng, nên là trần an toàn); 0 trước lần <c>SetLane</c> đầu.
        /// </summary>
        public int EstimatedVertexCount { get; private set; }

        internal LiveOpsTimelineLaneModel Model => _lane;
        internal LiveOpsTimelineGeometry Geometry => _geometry;

        /// <summary>padTop [SD1 §3.2]: 16 khi làn có vùng chồng (chừa chỗ nhãn "chồng 12 giờ"), không thì 4.</summary>
        internal float PaddingTop => _lane != null && _lane.OverlapRanges.Count > 0
            ? LiveOpsTimelineGeometry.LanePaddingTopWithOverlap
            : LiveOpsTimelineGeometry.LanePaddingTop;

        internal float LaneHeight => _lane == null ? 0f : PaddingTop + _lane.RowCount * LiveOpsTimelineGeometry.RowPitch + LaneBottomPadding;

        /// <summary>Số thanh đang dùng trong pool (thanh thừa ẩn, không xoá để lần vẽ sau không cấp phát lại).</summary>
        internal int BarCount => _activeBarCount;

        internal LiveOpsTimelineBar BarAt(int index) => _bars[index];

        internal VisualElement NextChip => _nextChip;
        internal Label NextChipLabel => _nextChipLabel;

        /// <summary>Bấm chip "Đợt tới" — element căn khung tới đợt đó (không phải intent sửa lịch).</summary>
        internal event Action<LiveOpsTimelineBarModel> NextChipClicked;

        internal void SetLane(LiveOpsTimelineLaneModel lane, LiveOpsTimelineGeometry geometry)
        {
            SetLane(lane, geometry, null, new LiveOpsHubFormat(TimeSpan.Zero), null);
        }

        /// <param name="nowUtc">Giờ hiện hành cho cột hôm nay; null = không vẽ cột hôm nay.</param>
        /// <param name="format">Định dạng chữ tooltip/chip (giờ máy từ element).</param>
        /// <param name="tooltipOf">Tooltip từng thanh (element dựng từ phát hiện của minimap qua LiveOpsFindingText); null = không tooltip.</param>
        internal void SetLane(LiveOpsTimelineLaneModel lane, LiveOpsTimelineGeometry geometry, DateTime? nowUtc, LiveOpsHubFormat format,
            Func<LiveOpsTimelineBarModel, string> tooltipOf)
        {
            _lane = lane ?? throw new ArgumentNullException(nameof(lane));
            _geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
            if (format == null) throw new ArgumentNullException(nameof(format));
            _nowUtc = nowUtc;
            _previewOverlaps = null;
            EstimatedVertexCount = LiveOpsTimelineVertexBudget.EstimateLane(lane, geometry);
            EnableInClassList(LiveOpsHubClassNames.TimelineLaneRecurring, lane.IsRecurring);
            tooltip = lane.IsRecurring ? LiveOpsHubStrings.TimelineRecurringLaneTooltip : string.Empty;
            style.height = LaneHeight; // style-inline-allowed: 3

            BindBars(lane, geometry, format, tooltipOf);
            BindOverlapLabels(lane.OverlapRanges, geometry);
            BindNextChip(lane, format, nowUtc);
            MarkDirtyRepaint();
        }

        internal LiveOpsTimelineBar FindBar(string barKey)
        {
            if (string.IsNullOrEmpty(barKey)) return null;
            for (int index = 0; index < _activeBarCount; index++)
            {
                if (string.Equals(_bars[index].Model.BarKey, barKey, StringComparison.Ordinal)) return _bars[index];
            }
            return null;
        }

        /// <summary>Thanh trúng toạ độ trong làn (x theo track, y theo đỉnh làn); thanh vẽ sau thắng khi chồng hình.</summary>
        /// <summary>
        /// Thanh dưới điểm (toạ độ trong làn). Duyệt từ cuối để thanh vẽ sau thắng khi đè nhau, nhưng thanh mà điểm nằm HẲN bên
        /// trong luôn thắng thanh chỉ trúng nhờ 4px lề bắt mép — hai đợt sát nhau thì mép cuối đợt trước không bị mép đầu đợt sau
        /// tóm mất (Hình 12 khung 7).
        /// </summary>
        internal LiveOpsTimelineBar BarAt(float trackPosition, float lanePosition)
        {
            LiveOpsTimelineBar grabbedByEdgeSlack = null;
            for (int index = _activeBarCount - 1; index >= 0; index--)
            {
                LiveOpsTimelineBar bar = _bars[index];
                if (!bar.Contains(trackPosition, lanePosition)) continue;
                if (bar.ContainsWithoutEdgeSlack(trackPosition, lanePosition)) return bar;
                if (grabbedByEdgeSlack == null) grabbedByEdgeSlack = bar;
            }
            return grabbedByEdgeSlack;
        }

        /// <summary>Tag "bị bỏ" ẩn khi thanh hover (tay nắm chiếm chỗ đó — Hình 12 khung 2).</summary>
        internal void SetDroppedTagHidden(string barKey, bool hidden)
        {
            if (barKey != null && _droppedTagByBarKey.TryGetValue(barKey, out VisualElement tag))
            {
                tag.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, hidden);
            }
        }

        /// <summary>
        /// Xem trước khi kéo (Hình 12 khung 7): vùng chồng mới và thanh "sẽ bị bỏ" trước khi thả. <paramref name="overlaps"/> null = về
        /// vùng chồng của model; tag "sẽ bị bỏ" đặt sau mép phải đã xem trước của thanh đó.
        /// </summary>
        internal void SetDragPreview(IReadOnlyList<(DateTime startUtc, DateTime endUtc)> overlaps, ICollection<string> willDropBarKeys,
            IDictionary<string, (float left, float width)> previewGeometry)
        {
            _previewOverlaps = overlaps;
            for (int index = 0; index < _activeBarCount; index++)
            {
                LiveOpsTimelineBar bar = _bars[index];
                bool willDrop = willDropBarKeys != null && willDropBarKeys.Contains(bar.Model.BarKey);
                bar.EnableInClassList(LiveOpsHubClassNames.TimelineBarWillDrop, willDrop);
            }
            RebuildDroppedTags(willDropBarKeys, previewGeometry);
            MarkDirtyRepaint();
        }

        internal void ClearDragPreview()
        {
            SetDragPreview(null, null, null);
        }

        /// <summary>Vùng chồng đang vẽ (xem trước khi kéo, không thì của model) — test đọc lại đúng thứ đã vẽ.</summary>
        internal IReadOnlyList<(DateTime startUtc, DateTime endUtc)> DrawnOverlaps =>
            _previewOverlaps ?? (_lane != null ? _lane.OverlapRanges : Array.Empty<(DateTime startUtc, DateTime endUtc)>());

        internal Color OverlapColor => _overlapColor;
        internal Color GridColor => _gridColor;

        private void BindBars(LiveOpsTimelineLaneModel lane, LiveOpsTimelineGeometry geometry, LiveOpsHubFormat format,
            Func<LiveOpsTimelineBarModel, string> tooltipOf)
        {
            IReadOnlyList<LiveOpsTimelineBarModel> bars = lane.Bars;
            float paddingTop = PaddingTop;
            for (int index = 0; index < bars.Count; index++)
            {
                if (index >= _bars.Count)
                {
                    LiveOpsTimelineBar created = new LiveOpsTimelineBar();
                    _bars.Add(created);
                }
                LiveOpsTimelineBar bar = _bars[index];
                // Thanh phải nằm trước chip "Đợt tới" và tag trong cây để tag vẽ trên thanh; Insert giữ thứ tự khi pool lớn dần.
                if (bar.parent != this) Insert(index, bar);
                LiveOpsTimelineBarModel model = bars[index];
                float top = paddingTop + model.RowIndex * LiveOpsTimelineGeometry.RowPitch + LaneBottomPadding;
                bar.Bind(model, geometry, top, lane.ColorSlot, tooltipOf != null ? tooltipOf(model) : string.Empty, format);
                bar.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, false);
            }
            for (int index = bars.Count; index < _bars.Count; index++) _bars[index].EnableInClassList(LiveOpsHubClassNames.TimelineHidden, true);
            _activeBarCount = bars.Count;
            RebuildDroppedTags(null, null);
        }

        private void RebuildDroppedTags(ICollection<string> willDropBarKeys, IDictionary<string, (float left, float width)> previewGeometry)
        {
            _droppedTagByBarKey.Clear();
            int used = 0;
            for (int index = 0; index < _activeBarCount; index++)
            {
                LiveOpsTimelineBar bar = _bars[index];
                bool willDrop = willDropBarKeys != null && willDropBarKeys.Contains(bar.Model.BarKey);
                if (!bar.Model.IsDropped && !willDrop) continue;
                VisualElement tag = DroppedTagAt(used++);
                tag.EnableInClassList(LiveOpsHubClassNames.TimelineDroppedTagWillDrop, willDrop);
                tag.Q<Label>().text = willDrop ? LiveOpsHubStrings.TimelineWillDropTag : LiveOpsHubStrings.TimelineDroppedTag;
                float right = bar.Left + bar.Width;
                if (previewGeometry != null && previewGeometry.TryGetValue(bar.Model.BarKey, out (float left, float width) preview))
                {
                    right = preview.left + preview.width;
                }
                tag.style.left = right + DroppedTagGap; // style-inline-allowed: 6
                tag.style.top = bar.Top + DroppedTagTopOffset; // style-inline-allowed: 6
                tag.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, false);
                tag.BringToFront();
                _droppedTagByBarKey[bar.Model.BarKey] = tag;
            }
            for (int index = used; index < _droppedTags.Count; index++) _droppedTags[index].EnableInClassList(LiveOpsHubClassNames.TimelineHidden, true);
        }

        private VisualElement DroppedTagAt(int index)
        {
            while (_droppedTags.Count <= index)
            {
                VisualElement tag = new VisualElement { pickingMode = PickingMode.Ignore };
                tag.AddToClassList(LiveOpsHubClassNames.TimelineDroppedTag);
                LiveOpsStateMark mark = new LiveOpsStateMark { Kind = LiveOpsStateMark.MarkKind.Health, Size = LiveOpsStateMark.MarkSize.Small };
                mark.SetHealth(HealthState.Blocked);
                tag.Add(mark);
                Label text = new Label();
                text.AddToClassList(LiveOpsHubClassNames.TextBlocked);
                tag.Add(text);
                Add(tag);
                _droppedTags.Add(tag);
            }
            return _droppedTags[index];
        }

        /// <summary>
        /// Nhãn "chồng …" giữa vùng chồng. Đo bằng <see cref="LiveOpsTimelineDragController.LengthText"/> chứ không bằng
        /// <see cref="LiveOpsHubFormat.Duration"/>: readout lúc kéo in "chồng 12 giờ với hunt-0916-bonus" ngay phía trên nhãn này
        /// [SD1 §3.7, Hình 12 khung 7], nên cùng một đại lượng trên cùng một màn phải cùng một công thức — bản chung sẽ in
        /// "chồng 1 ngày 12 giờ" cho đúng vùng mà readout gọi là "36 giờ".
        /// </summary>
        private void BindOverlapLabels(IReadOnlyList<(DateTime startUtc, DateTime endUtc)> overlaps, LiveOpsTimelineGeometry geometry)
        {
            for (int index = 0; index < overlaps.Count; index++)
            {
                if (index >= _overlapLabels.Count)
                {
                    Label created = new Label { pickingMode = PickingMode.Ignore };
                    created.AddToClassList(LiveOpsHubClassNames.TimelineOverlapLabel);
                    created.AddToClassList(LiveOpsHubClassNames.TextBlocked);
                    Add(created);
                    _overlapLabels.Add(created);
                }
                Label label = _overlapLabels[index];
                (DateTime startUtc, DateTime endUtc) overlap = overlaps[index];
                float left = Math.Max(0f, geometry.XOf(overlap.startUtc));
                float right = Math.Min(geometry.TrackWidth, geometry.XOf(overlap.endUtc));
                label.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineOverlapLabelFormat,
                    LiveOpsTimelineDragController.LengthText(overlap.endUtc - overlap.startUtc));
                label.style.left = (left + right) / 2f; // style-inline-allowed: 3
                label.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, right <= left);
            }
            for (int index = overlaps.Count; index < _overlapLabels.Count; index++) _overlapLabels[index].EnableInClassList(LiveOpsHubClassNames.TimelineHidden, true);
        }

        private void BindNextChip(LiveOpsTimelineLaneModel lane, LiveOpsHubFormat format, DateTime? nowUtc)
        {
            LiveOpsTimelineBarModel next = lane.NextOutsideRange;
            bool visible = next != null && lane.Bars.Count == 0;
            _nextChip.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !visible);
            if (!visible) return;
            string remaining = nowUtc.HasValue ? format.Remaining(nowUtc.Value, next.StartUtc) : string.Empty;
            _nextChipLabel.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineNextOutsideChipFormat, next.EventId,
                format.ShortDateTime(next.StartUtc), remaining);
            _nextChip.tooltip = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineNextOutsideChipTooltipFormat, next.EventId,
                format.ShortDateTime(next.StartUtc));
            _nextChip.style.top = PaddingTop + NextChipTopOffset; // style-inline-allowed: 3
            _nextChip.BringToFront();
        }

        private void OnNextChipClicked(ClickEvent clickEvent)
        {
            if (_lane?.NextOutsideRange == null) return;
            clickEvent.StopPropagation();
            NextChipClicked?.Invoke(_lane.NextOutsideRange);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent resolvedEvent)
        {
            if (customStyle.TryGetValue(WeekendColorProperty, out Color weekend)) _weekendColor = weekend;
            if (customStyle.TryGetValue(TodayColorProperty, out Color today)) _todayColor = today;
            if (customStyle.TryGetValue(GridColorProperty, out Color grid)) _gridColor = grid;
            if (customStyle.TryGetValue(GridMondayColorProperty, out Color gridMonday)) _gridMondayColor = gridMonday;
            if (customStyle.TryGetValue(OverlapColorProperty, out Color overlap)) _overlapColor = overlap;
            MarkDirtyRepaint();
        }

        // ------------------------------------------------------------------------------------------------ vẽ nền

        private void DrawBackground(MeshGenerationContext context)
        {
            if (_lane == null || _geometry == null) return;
            Rect bounds = contentRect;
            if (float.IsNaN(bounds.height) || bounds.height <= 0f) return;
            Painter2D painter = context.painter2D;
            LiveOpsTimelineGeometry geometry = _geometry;

            DrawWeekends(painter, geometry, bounds.height);
            DrawToday(painter, geometry, bounds.height);
            DrawDayLines(painter, geometry, bounds.height);
            IReadOnlyList<(DateTime startUtc, DateTime endUtc)> overlaps = DrawnOverlaps;
            for (int index = 0; index < overlaps.Count; index++) DrawOverlap(painter, geometry, overlaps[index], overlaps.Count);
        }

        private void DrawWeekends(Painter2D painter, LiveOpsTimelineGeometry geometry, float height)
        {
            if (_weekendColor.a <= 0f) return;
            // Thứ Bảy + Chủ nhật gộp một hình: đúng một Fill mỗi tuần như ước lượng của VertexBudget.
            DateTime day = DateTime.SpecifyKind(geometry.RangeStartUtc.Date, DateTimeKind.Utc);
            int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)day.DayOfWeek + DaysPerWeek) % DaysPerWeek;
            DateTime saturday = LiveOpsTimelineGeometry.AddTicksClamped(day, TimeSpan.FromDays(daysUntilSaturday - DaysPerWeek).Ticks);
            painter.fillColor = _weekendColor;
            for (DateTime weekendStart = saturday; weekendStart < geometry.RangeEndUtc;
                 weekendStart = LiveOpsTimelineGeometry.AddTicksClamped(weekendStart, TimeSpan.FromDays(DaysPerWeek).Ticks))
            {
                DateTime weekendEnd = LiveOpsTimelineGeometry.AddTicksClamped(weekendStart, TimeSpan.FromDays(2).Ticks);
                float left = Math.Max(0f, geometry.XOf(weekendStart));
                float right = Math.Min(geometry.TrackWidth, geometry.XOf(weekendEnd));
                if (right > left) FillRectangle(painter, left, 0f, right - left, height);
                if (weekendStart.Year >= DateTime.MaxValue.Year) break;
            }
        }

        private void DrawToday(Painter2D painter, LiveOpsTimelineGeometry geometry, float height)
        {
            if (!_nowUtc.HasValue || _todayColor.a <= 0f) return;
            DateTime today = DateTime.SpecifyKind(_nowUtc.Value.Date, DateTimeKind.Utc);
            float left = Math.Max(0f, geometry.XOf(today));
            float right = Math.Min(geometry.TrackWidth, geometry.XOf(LiveOpsTimelineGeometry.AddTicksClamped(today, TimeSpan.TicksPerDay)));
            if (right <= left) return;
            painter.fillColor = _todayColor;
            FillRectangle(painter, left, 0f, right - left, height);
        }

        private void DrawDayLines(Painter2D painter, LiveOpsTimelineGeometry geometry, float height)
        {
            if (_gridColor.a <= 0f && _gridMondayColor.a <= 0f) return;
            DateTime day = DateTime.SpecifyKind(geometry.RangeStartUtc.Date, DateTimeKind.Utc);
            if (day < geometry.RangeStartUtc) day = LiveOpsTimelineGeometry.AddTicksClamped(day, TimeSpan.TicksPerDay);
            painter.lineWidth = LineWidth;
            while (day < geometry.RangeEndUtc)
            {
                float lineX = geometry.XOf(day) + HalfPixel;
                painter.strokeColor = day.DayOfWeek == DayOfWeek.Monday ? _gridMondayColor : _gridColor;
                painter.BeginPath();
                painter.MoveTo(new Vector2(lineX, 0f));
                painter.LineTo(new Vector2(lineX, height));
                painter.Stroke();
                if (DateTime.MaxValue.Ticks - day.Ticks < TimeSpan.TicksPerDay) break;
                day = day.AddDays(1);
            }
        }

        private void DrawOverlap(Painter2D painter, LiveOpsTimelineGeometry geometry, (DateTime startUtc, DateTime endUtc) overlap, int overlapCount)
        {
            if (_overlapColor.a <= 0f) return;
            float left = Math.Max(0f, geometry.XOf(overlap.startUtc));
            float right = Math.Min(geometry.TrackWidth, geometry.XOf(overlap.endUtc));
            if (right <= left) return;
            float top = PaddingTop;
            float height = _lane.RowCount * LiveOpsTimelineGeometry.RowPitch + OverlapExtraHeight;
            float width = right - left;

            painter.fillColor = WithAlpha(_overlapColor, OverlapBackgroundAlpha);
            FillRectangle(painter, left, top, width, height);
            painter.fillColor = _overlapColor;
            FillRectangle(painter, left, top, width, OverlapTopStripHeight);

            painter.strokeColor = _overlapColor;
            painter.lineWidth = LineWidth;
            StrokeSegment(painter, new Vector2(left + HalfPixel, top), new Vector2(left + HalfPixel, top + height));
            StrokeSegment(painter, new Vector2(right - HalfPixel, top), new Vector2(right - HalfPixel, top + height));

            // Gạch chéo 135°, chu kỳ 8px, tối đa 80 nét mỗi làn chia đều cho các vùng chồng [SD1 §3.14].
            painter.strokeColor = WithAlpha(_overlapColor, OverlapHatchAlpha);
            int maximumSegments = Math.Max(1, LiveOpsTimelineVertexBudget.MaximumHatchSegmentsPerLane / Math.Max(1, overlapCount));
            int drawn = 0;
            float bottom = top + height;
            for (float offset = left; offset < right + height && drawn < maximumSegments; offset += LiveOpsTimelineVertexBudget.HatchPeriod)
            {
                // Nét đi từ (offset, top) xuống trái tới (offset − height, bottom), cắt theo hình chữ nhật vùng chồng.
                Vector2 start = new Vector2(offset, top);
                Vector2 end = new Vector2(offset - height, bottom);
                if (start.x > right)
                {
                    start = new Vector2(right, top + (offset - right));
                }
                if (end.x < left)
                {
                    end = new Vector2(left, top + (offset - left));
                }
                if (start.y >= end.y) continue;
                StrokeSegment(painter, start, end);
                drawn++;
            }
        }

        private static void FillRectangle(Painter2D painter, float left, float top, float width, float height)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(left, top));
            painter.LineTo(new Vector2(left + width, top));
            painter.LineTo(new Vector2(left + width, top + height));
            painter.LineTo(new Vector2(left, top + height));
            painter.ClosePath();
            painter.Fill();
        }

        private static void StrokeSegment(Painter2D painter, Vector2 start, Vector2 end)
        {
            painter.BeginPath();
            painter.MoveTo(start);
            painter.LineTo(end);
            painter.Stroke();
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, color.a * alpha);
        }

#if !UNITY_2023_2_OR_NEWER
        public new sealed class UxmlFactory : UxmlFactory<LiveOpsTimelineLane, UxmlTraits>
        {
        }

        public new sealed class UxmlTraits : VisualElement.UxmlTraits
        {
        }
#endif
    }

    /// <summary>
    /// Header 168px của một làn [SD1 §3.2]: dòng tên (swatch 9×9 · id loại · icon lặp · chip trạng thái) → dòng meta (+ chip "Không đặt
    /// được (n)") → meta2 tuỳ chọn. Chữ meta dựng sẵn ở model; header chỉ gắn vào Label. (V-22 CC-TLMODEL-2) làn <c>TypeId ""</c> hiện
    /// tên giữ chỗ "(chưa ghi loại)" thay ô tên trống.
    /// </summary>
    internal sealed class LiveOpsTimelineLaneHeader : VisualElement
    {
        private const string LoopIconName = "preAudioLoopOff";
        private const int LaneIconSize = 12;

        public LiveOpsTimelineLaneHeader()
        {
            AddToClassList(LiveOpsHubClassNames.TimelineLaneHeader);
            pickingMode = PickingMode.Position;

            VisualElement nameRow = new VisualElement { pickingMode = PickingMode.Ignore };
            nameRow.AddToClassList(LiveOpsHubClassNames.TimelineLaneNameRow);
            Add(nameRow);
            Swatch = new VisualElement { pickingMode = PickingMode.Ignore };
            Swatch.AddToClassList(LiveOpsHubClassNames.Swatch);
            Swatch.AddToClassList(LiveOpsHubClassNames.TimelineLaneSwatch);
            nameRow.Add(Swatch);
            NameLabel = new Label { pickingMode = PickingMode.Ignore };
            NameLabel.AddToClassList(LiveOpsHubClassNames.TimelineLaneName);
            NameLabel.AddToClassList(LiveOpsHubClassNames.Mono);
            nameRow.Add(NameLabel);
            LoopIcon = LiveOpsHubIcons.CreateImage(LoopIconName, LaneIconSize);
            LoopIcon.AddToClassList(LiveOpsHubClassNames.TimelineLaneLoopIcon);
            nameRow.Add(LoopIcon);
            Chip = new VisualElement { pickingMode = PickingMode.Position };
            Chip.AddToClassList(LiveOpsHubClassNames.TimelineLaneChip);
            ChipMark = new LiveOpsStateMark { Kind = LiveOpsStateMark.MarkKind.Health, Size = LiveOpsStateMark.MarkSize.Small };
            Chip.Add(ChipMark);
            ChipCount = new Label { pickingMode = PickingMode.Ignore };
            Chip.Add(ChipCount);
            nameRow.Add(Chip);

            VisualElement metaRow = new VisualElement { pickingMode = PickingMode.Ignore };
            metaRow.AddToClassList(LiveOpsHubClassNames.TimelineLaneMetaRow);
            Add(metaRow);
            MetaLabel = new Label { pickingMode = PickingMode.Ignore };
            MetaLabel.AddToClassList(LiveOpsHubClassNames.TimelineLaneMeta);
            metaRow.Add(MetaLabel);
            UnplaceableChip = new VisualElement { pickingMode = PickingMode.Position, tooltip = LiveOpsHubStrings.TimelineUnplaceableChipTooltip };
            UnplaceableChip.AddToClassList(LiveOpsHubClassNames.TimelineUnplaceableChip);
            LiveOpsStateMark unplaceableMark = new LiveOpsStateMark { Kind = LiveOpsStateMark.MarkKind.Health, Size = LiveOpsStateMark.MarkSize.Small };
            unplaceableMark.SetHealth(HealthState.Blocked);
            UnplaceableChip.Add(unplaceableMark);
            UnplaceableLabel = new Label { pickingMode = PickingMode.Ignore };
            UnplaceableChip.Add(UnplaceableLabel);
            UnplaceableChip.RegisterCallback<ClickEvent>(OnUnplaceableClicked);
            metaRow.Add(UnplaceableChip);

            SecondaryMetaLabel = new Label { pickingMode = PickingMode.Ignore };
            SecondaryMetaLabel.AddToClassList(LiveOpsHubClassNames.TimelineLaneMeta);
            SecondaryMetaLabel.AddToClassList(LiveOpsHubClassNames.TimelineLaneMetaSecondary);
            Add(SecondaryMetaLabel);
        }

        public LiveOpsTimelineLaneModel Model { get; private set; }
        internal VisualElement Swatch { get; }
        internal Label NameLabel { get; }
        internal Image LoopIcon { get; }
        internal VisualElement Chip { get; }
        internal LiveOpsStateMark ChipMark { get; }
        internal Label ChipCount { get; }
        internal Label MetaLabel { get; }
        internal VisualElement UnplaceableChip { get; }
        internal Label UnplaceableLabel { get; }
        internal Label SecondaryMetaLabel { get; }

        /// <summary>Bấm chip "Không đặt được (n)" — TypeId của làn; presenter chọn đợt không đặt được (INTERIM I-5 ở W4).</summary>
        internal event Action<string> UnplaceableClicked;

        internal void Bind(LiveOpsTimelineLaneModel lane)
        {
            Model = lane ?? throw new ArgumentNullException(nameof(lane));
            bool isUntyped = lane.TypeId.Length == 0;
            NameLabel.text = isUntyped ? LiveOpsHubStrings.TimelineUntypedLaneName : lane.TypeId;
            NameLabel.EnableInClassList(LiveOpsHubClassNames.TimelineLaneNamePlaceholder, isUntyped);
            NameLabel.tooltip = NameLabel.text;
            LiveOpsHubStyle.SetEventColor(Swatch, lane.ColorSlot);
            Swatch.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, isUntyped);
            LoopIcon.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !lane.IsRecurring);
            LoopIcon.tooltip = lane.IsRecurring ? LiveOpsHubStrings.TimelineRecurringLaneTooltip : string.Empty;

            bool hasChip = lane.ChipCount > 0 && lane.ChipState != HealthState.Ok;
            Chip.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !hasChip);
            if (hasChip)
            {
                ChipMark.SetHealth(lane.ChipState);
                ChipCount.text = lane.ChipCount.ToString(CultureInfo.InvariantCulture);
                Chip.tooltip = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineLaneChipTooltipFormat, lane.ChipCount);
            }

            MetaLabel.text = lane.MetaText;
            MetaLabel.tooltip = lane.MetaText;
            bool hasUnplaceable = lane.UnplaceableCount > 0;
            UnplaceableChip.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !hasUnplaceable);
            UnplaceableLabel.text = hasUnplaceable
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineUnplaceableChipFormat, lane.UnplaceableCount)
                : string.Empty;
            SecondaryMetaLabel.text = lane.SecondaryMetaText;
            SecondaryMetaLabel.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, lane.SecondaryMetaText.Length == 0);
        }

        private void OnUnplaceableClicked(ClickEvent clickEvent)
        {
            if (Model == null) return;
            clickEvent.StopPropagation();
            UnplaceableClicked?.Invoke(Model.TypeId);
        }
    }
}
