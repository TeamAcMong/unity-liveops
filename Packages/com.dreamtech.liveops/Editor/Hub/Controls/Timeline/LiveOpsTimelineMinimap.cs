using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Minimap 20px dưới timeline [SD1 §3.6]: mỗi làn một dải 3px màu loại, vạch đỏ cao hết dải ở đợt bị bỏ, vạch vàng nửa trên ở cảnh
    /// báo, vạch bây giờ 1px, cờ đã đăng, khung khoảng đang xem. Element riêng vẽ Painter2D (dưới 1.000 vertex ở dữ liệu mẫu, tối đa
    /// 50 vạch — model đã gom đoạn khe dưới 1px [SD1 §3.14]). Bấm = <see cref="JumpRequested"/> tới giờ tại chỗ bấm.
    ///
    /// Tooltip theo vị trí: vạch → câu ngắn của phát hiện qua <c>LiveOpsFindingText.ShortLabel</c> + <c>PlainText</c> (V-22 CC-FT-1,
    /// tooltip không rich text), chỗ trống → câu chú giải. Màu đọc từ token khai lại trên <c>.liveops-hub-timeline-minimap</c>.
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
#endif
    public sealed partial class LiveOpsTimelineMinimap : VisualElement
    {
        internal static readonly CustomStyleProperty<Color> BlockedColorProperty = new CustomStyleProperty<Color>("--liveops-hub-minimap-blocked");
        internal static readonly CustomStyleProperty<Color> WarningColorProperty = new CustomStyleProperty<Color>("--liveops-hub-minimap-warning");
        internal static readonly CustomStyleProperty<Color> NowColorProperty = new CustomStyleProperty<Color>("--liveops-hub-minimap-now");
        internal static readonly CustomStyleProperty<Color> TextColorProperty = new CustomStyleProperty<Color>("--liveops-hub-minimap-text");

        internal static readonly CustomStyleProperty<Color>[] EventColorProperties =
        {
            new CustomStyleProperty<Color>("--liveops-hub-minimap-event-color-0"),
            new CustomStyleProperty<Color>("--liveops-hub-minimap-event-color-1"),
            new CustomStyleProperty<Color>("--liveops-hub-minimap-event-color-2"),
            new CustomStyleProperty<Color>("--liveops-hub-minimap-event-color-3"),
            new CustomStyleProperty<Color>("--liveops-hub-minimap-event-color-4"),
            new CustomStyleProperty<Color>("--liveops-hub-minimap-event-color-5"),
            new CustomStyleProperty<Color>("--liveops-hub-minimap-event-color-6"),
            new CustomStyleProperty<Color>("--liveops-hub-minimap-event-color-7"),
        };

        // Số đo [SD1 §3.6]: dải 3px top = 2 + hàng × 3; vạch 2px; vạch vàng cao 9; cờ rộng 4 (viền trái 1 + viền trên 3); khung top/bottom 1, opacity 0,7.
        internal const float BandTop = 2f;
        internal const float BandHeight = 3f;
        internal const float MarkWidth = 2f;
        internal const float WarningMarkHeight = 9f;
        internal const float EdgeInset = 1f;
        internal const float PublishedFlagWidth = 4f;
        internal const float PublishedFlagTopHeight = 3f;
        internal const float ViewportAlpha = 0.7f;

        /// <summary>Rê chuột trong 3px quanh vạch thì tooltip nói vạch đó — vạch 2px quá hẹp để trỏ đúng.</summary>
        internal const float MarkHoverRadius = 3f;

        private const float LineWidth = 1f;
        private const float HalfPixel = 0.5f;

        private readonly Color[] _eventColors = new Color[EventColorProperties.Length];
        private LiveOpsTimelineMinimapModel _model;
        private Color _blockedColor = Color.clear;
        private Color _warningColor = Color.clear;
        private Color _nowColor = Color.clear;
        private Color _textColor = Color.clear;
        private Vector2 _lastPointerPosition;

        public LiveOpsTimelineMinimap()
        {
            AddToClassList(LiveOpsHubClassNames.TimelineMinimap);
            pickingMode = PickingMode.Position;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<TooltipEvent>(OnTooltip);
        }

        /// <summary>Bấm minimap: giờ tại chỗ bấm — presenter căn khung quanh giờ đó.</summary>
        public event Action<DateTime> JumpRequested;

        /// <summary>Ước lượng vertex (<see cref="LiveOpsTimelineVertexBudget.EstimateMinimap"/>); 0 trước lần <c>SetMinimap</c> đầu.</summary>
        public int EstimatedVertexCount { get; private set; }

        internal LiveOpsTimelineMinimapModel Model => _model;
        internal Color BlockedColor => _blockedColor;
        internal Color EventColor(int colorSlot) => _eventColors[((colorSlot % _eventColors.Length) + _eventColors.Length) % _eventColors.Length];

        internal void SetMinimap(LiveOpsTimelineMinimapModel model)
        {
            SetMinimap(model, new LiveOpsHubFormat(TimeSpan.Zero));
        }

        internal void SetMinimap(LiveOpsTimelineMinimapModel model, LiveOpsHubFormat format)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            if (format == null) throw new ArgumentNullException(nameof(format));
            EstimatedVertexCount = LiveOpsTimelineVertexBudget.EstimateMinimap(model);
            tooltip = LegendTooltip();
            MarkDirtyRepaint();
        }

        /// <summary>Tooltip tại toạ độ trong minimap: vạch gần nhất trong 3px, không thì câu chú giải của minimap.</summary>
        internal string TooltipAt(Vector2 localPosition)
        {
            if (_model == null) return string.Empty;
            float scale = HorizontalScale();
            LiveOpsTimelineMinimapMark nearest = null;
            float nearestDistance = float.MaxValue;
            for (int index = 0; index < _model.Marks.Count; index++)
            {
                LiveOpsTimelineMinimapMark mark = _model.Marks[index];
                float distance = Math.Abs(_model.XOf(mark.AtUtc) * scale - localPosition.x);
                if (distance <= MarkHoverRadius && distance < nearestDistance)
                {
                    nearest = mark;
                    nearestDistance = distance;
                }
            }
            return nearest != null ? MarkTooltip(nearest) : LegendTooltip();
        }

        /// <summary>
        /// "Cảnh báo · weekly-pass-35 đổi id" — nhãn hậu quả · id · câu ngắn của LiveOpsFindingText (không rich text), nối bằng
        /// khoảng trắng đúng [SD1 §3.6]. Vạch KHÔNG có phát hiện (đợt không đặt được lên trục) không mượn được câu nào nên lý do
        /// là chữ riêng của timeline và bọc ngoặc để không đọc dính vào id: "Bị bỏ · lava-quest-2026-10 (không đặt được)".
        /// </summary>
        internal static string MarkTooltip(LiveOpsTimelineMinimapMark mark)
        {
            if (mark == null) throw new ArgumentNullException(nameof(mark));
            string consequenceLabel = mark.State == HealthState.Blocked ? LiveOpsHubStrings.TimelineMinimapDroppedLabel : LiveOpsHubStrings.TimelineMinimapProgressLostLabel;
            if (mark.Finding == null)
            {
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineMinimapMarkNoFindingFormat, consequenceLabel,
                    mark.TargetId, LiveOpsHubStrings.TimelineMinimapUnplaceableReason);
            }
            consequenceLabel = ConsequenceLabelOf(mark.Finding.Consequence, consequenceLabel);
            string reason = LiveOpsFindingText.PlainText(LiveOpsFindingText.ShortLabel(mark.Finding));
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineMinimapMarkFormat, consequenceLabel, mark.TargetId, reason);
        }

        internal static string ConsequenceLabelOf(LiveEventCalendarConsequence consequence, string fallback)
        {
            switch (consequence)
            {
                case LiveEventCalendarConsequence.Dropped: return LiveOpsHubStrings.TimelineMinimapDroppedLabel;
                case LiveEventCalendarConsequence.ProgressLost: return LiveOpsHubStrings.TimelineMinimapProgressLostLabel;
                case LiveEventCalendarConsequence.ShouldReview: return LiveOpsHubStrings.TimelineMinimapShouldReviewLabel;
                default: return fallback;
            }
        }

        /// <summary>Giờ tại toạ độ x trong minimap (theo bề rộng thật của element, không theo bề rộng lúc dựng model).</summary>
        internal DateTime TimeAt(float localX)
        {
            if (_model == null) return default;
            float scale = HorizontalScale();
            double hours = localX / scale / _model.PixelsPerHour;
            return LiveOpsTimelineGeometry.AddTicksClamped(_model.RangeStartUtc, (long)Math.Round(hours * TimeSpan.TicksPerHour));
        }

        private string LegendTooltip()
        {
            if (_model == null) return string.Empty;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineMinimapLegendTooltipFormat, DayMonth(_model.RangeStartUtc),
                DayMonth(_model.RangeEndUtc));
        }

        /// <summary>"14/8" — ngày/tháng không số 0 đầu như mọi chữ ngày của hub [FD §6.1]; minimap dài hơn một tháng nên không cần giờ.</summary>
        private static string DayMonth(DateTime utc)
        {
            return utc.Day.ToString(CultureInfo.InvariantCulture) + "/" + utc.Month.ToString(CultureInfo.InvariantCulture);
        }

        private float HorizontalScale()
        {
            float width = contentRect.width;
            if (_model == null || float.IsNaN(width) || width <= 0f || _model.Width <= 0f) return 1f;
            return width / _model.Width;
        }

        private void OnPointerMove(PointerMoveEvent pointerEvent)
        {
            _lastPointerPosition = pointerEvent.localPosition;
        }

        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            if (_model == null || pointerEvent.button != 0) return;
            pointerEvent.StopPropagation();
            JumpRequested?.Invoke(TimeAt(pointerEvent.localPosition.x));
        }

        private void OnTooltip(TooltipEvent tooltipEvent)
        {
            if (_model == null) return;
            tooltipEvent.tooltip = TooltipAt(_lastPointerPosition);
            tooltipEvent.rect = worldBound;
            tooltipEvent.StopPropagation();
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent resolvedEvent)
        {
            for (int index = 0; index < EventColorProperties.Length; index++)
            {
                if (customStyle.TryGetValue(EventColorProperties[index], out Color eventColor)) _eventColors[index] = eventColor;
            }
            if (customStyle.TryGetValue(BlockedColorProperty, out Color blocked)) _blockedColor = blocked;
            if (customStyle.TryGetValue(WarningColorProperty, out Color warning)) _warningColor = warning;
            if (customStyle.TryGetValue(NowColorProperty, out Color now)) _nowColor = now;
            if (customStyle.TryGetValue(TextColorProperty, out Color text)) _textColor = text;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            if (_model == null) return;
            Rect bounds = contentRect;
            if (float.IsNaN(bounds.width) || bounds.width <= 0f || bounds.height <= 0f) return;
            float scale = HorizontalScale();
            Painter2D painter = context.painter2D;

            for (int bandIndex = 0; bandIndex < _model.Bands.Count; bandIndex++)
            {
                LiveOpsTimelineMinimapBand band = _model.Bands[bandIndex];
                painter.fillColor = EventColor(band.ColorSlot);
                float top = BandTop + band.RowIndex * BandHeight;
                if (top + BandHeight > bounds.height) break;
                for (int segmentIndex = 0; segmentIndex < band.Segments.Count; segmentIndex++)
                {
                    (DateTime startUtc, DateTime endUtc) segment = band.Segments[segmentIndex];
                    float left = _model.XOf(segment.startUtc) * scale;
                    float right = _model.XOf(segment.endUtc) * scale;
                    FillRectangle(painter, left, top, Math.Max(LineWidth, right - left), BandHeight);
                }
            }

            for (int markIndex = 0; markIndex < _model.Marks.Count; markIndex++)
            {
                LiveOpsTimelineMinimapMark mark = _model.Marks[markIndex];
                bool blocked = mark.State == HealthState.Blocked;
                painter.fillColor = blocked ? _blockedColor : _warningColor;
                float left = _model.XOf(mark.AtUtc) * scale - MarkWidth / 2f;
                float height = blocked ? bounds.height - EdgeInset * 2f : WarningMarkHeight;
                FillRectangle(painter, left, EdgeInset, MarkWidth, height);
            }

            painter.fillColor = _nowColor;
            FillRectangle(painter, _model.XOf(_model.NowUtc) * scale, 0f, LineWidth, bounds.height);

            if (_model.PublishedAtUtc.HasValue)
            {
                float flagLeft = _model.XOf(_model.PublishedAtUtc.Value) * scale;
                painter.fillColor = _textColor;
                FillRectangle(painter, flagLeft, EdgeInset, LineWidth, bounds.height - EdgeInset * 2f);
                FillRectangle(painter, flagLeft, EdgeInset, PublishedFlagWidth, PublishedFlagTopHeight);
            }

            float viewportLeft = Math.Max(0f, _model.XOf(_model.ViewportStartUtc) * scale) + HalfPixel;
            float viewportRight = Math.Min(bounds.width, _model.XOf(_model.ViewportEndUtc) * scale) - HalfPixel;
            if (viewportRight > viewportLeft)
            {
                painter.strokeColor = new Color(_textColor.r, _textColor.g, _textColor.b, _textColor.a * ViewportAlpha);
                painter.lineWidth = LineWidth;
                float top = EdgeInset + HalfPixel;
                float bottom = bounds.height - EdgeInset - HalfPixel;
                painter.BeginPath();
                painter.MoveTo(new Vector2(viewportLeft, top));
                painter.LineTo(new Vector2(viewportRight, top));
                painter.LineTo(new Vector2(viewportRight, bottom));
                painter.LineTo(new Vector2(viewportLeft, bottom));
                painter.ClosePath();
                painter.Stroke();
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

#if !UNITY_2023_2_OR_NEWER
        public new sealed class UxmlFactory : UxmlFactory<LiveOpsTimelineMinimap, UxmlTraits>
        {
        }

        public new sealed class UxmlTraits : VisualElement.UxmlTraits
        {
        }
#endif
    }
}
