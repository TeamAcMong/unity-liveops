using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Thước timeline [SD1 §3.2, §3.13] (V-11): góc trục 168px ghi "UTC" (giờ máy trong tooltip) + track gồm tầng 1 (tháng, "Tuần n",
    /// cờ bây giờ "08:47"), tầng 2 (ngày, hoặc giờ UTC ở zoom Ngày, + bubble giờ tại con trỏ), hàng dấu 10px (cờ đã đăng) và — chỉ ở
    /// zoom Ngày — tầng 3 cao 14px ghi giờ máy. Vạch và nhãn đọc từ <see cref="LiveOpsTimelineGeometry.RulerTicks"/>; thước chỉ đặt
    /// Label trong pool (nhãn cần tooltip, cắt chữ, đậm thứ Hai [SD1 §3.14]) và vẽ vạch 1px bằng Painter2D trên track của chính nó —
    /// element riêng nên nhãn/vạch thước không cộng vào ngân sách vertex của làn.
    /// </summary>
    internal sealed class LiveOpsTimelineRuler : VisualElement
    {
        internal static readonly CustomStyleProperty<Color> DayLineColorProperty = new CustomStyleProperty<Color>("--liveops-hub-ruler-day-line");
        internal static readonly CustomStyleProperty<Color> WeekLineColorProperty = new CustomStyleProperty<Color>("--liveops-hub-ruler-week-line");

        /// <summary>Chiều cao tầng [SD1 §3.2]: tầng 1 và 2 cao 18, hàng dấu 10, tầng giờ máy 14 — khối trên 46px, zoom Ngày 60px.</summary>
        internal const float MonthTierHeight = 18f;
        internal const float DayTierHeight = 18f;
        internal const float MarksTierHeight = 10f;
        internal const float DeviceTierHeight = 14f;

        /// <summary>Vạch thứ Hai ở tầng 1 = màu chữ × 0,35 [SD1 §3.2] — USS không nhân alpha vào var() nên nhân ở đây.</summary>
        internal const float WeekLineAlpha = 0.35f;

        /// <summary>Cờ đã đăng đặt left = X − 1 để tâm cán 2px nằm đúng giờ đăng [SD1 §3.2].</summary>
        internal const float PublishedFlagOffset = 1f;

        private const string ClockFormat = "HH:mm";
        private const float LineWidth = 1f;
        private const float HalfPixel = 0.5f;

        private readonly List<Label> _monthLabels = new List<Label>();
        private readonly List<Label> _dayLabels = new List<Label>();
        private readonly List<Label> _deviceLabels = new List<Label>();
        private readonly List<LiveOpsTimelineRulerTick> _lineTicks = new List<LiveOpsTimelineRulerTick>();

        private LiveOpsTimelineGeometry _geometry;
        private LiveOpsHubFormat _format = new LiveOpsHubFormat(TimeSpan.Zero);
        private Color _dayLineColor = Color.clear;
        private Color _weekLineColor = Color.clear;

        public LiveOpsTimelineRuler()
        {
            AddToClassList(LiveOpsHubClassNames.TimelineRuler);
            pickingMode = PickingMode.Position;

            Corner = new VisualElement { pickingMode = PickingMode.Position };
            Corner.AddToClassList(LiveOpsHubClassNames.TimelineRulerCorner);
            Label title = new Label(LiveOpsHubStrings.TimelineRulerCornerTitle) { pickingMode = PickingMode.Ignore };
            title.AddToClassList(LiveOpsHubClassNames.TimelineRulerCornerTitle);
            Corner.Add(title);
            CornerSubtitle = new Label { pickingMode = PickingMode.Ignore };
            CornerSubtitle.AddToClassList(LiveOpsHubClassNames.TimelineRulerCornerSubtitle);
            Corner.Add(CornerSubtitle);
            Add(Corner);

            Track = new VisualElement { pickingMode = PickingMode.Position };
            Track.AddToClassList(LiveOpsHubClassNames.TimelineRulerTrack);
            Track.generateVisualContent += DrawLines;
            Track.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            Add(Track);

            MonthTier = CreateTier(LiveOpsHubClassNames.TimelineRulerTierMonth);
            DayTier = CreateTier(LiveOpsHubClassNames.TimelineRulerTierDay);
            MarksTier = CreateTier(LiveOpsHubClassNames.TimelineRulerTierMarks);
            DeviceTier = CreateTier(LiveOpsHubClassNames.TimelineRulerTierDevice);

            NowFlag = new Label { pickingMode = PickingMode.Position };
            NowFlag.AddToClassList(LiveOpsHubClassNames.TimelineRulerNowFlag);
            NowFlag.AddToClassList(LiveOpsHubClassNames.Mono);
            Bubble = new Label { pickingMode = PickingMode.Position };
            Bubble.AddToClassList(LiveOpsHubClassNames.TimelineRulerBubble);
            Bubble.AddToClassList(LiveOpsHubClassNames.TimelineHidden);
            PublishedFlag = new VisualElement { pickingMode = PickingMode.Position };
            PublishedFlag.AddToClassList(LiveOpsHubClassNames.TimelineRulerPublishedFlag);
            MarksTier.Add(PublishedFlag);
        }

        internal VisualElement Corner { get; }
        internal Label CornerSubtitle { get; }
        internal VisualElement Track { get; }
        internal VisualElement MonthTier { get; }
        internal VisualElement DayTier { get; }
        internal VisualElement MarksTier { get; }
        internal VisualElement DeviceTier { get; }
        internal Label NowFlag { get; }
        internal Label Bubble { get; }
        internal VisualElement PublishedFlag { get; }

        /// <summary>Zoom đang vẽ thước (preset, hoặc suy từ px/giờ khi zoom liên tục).</summary>
        internal LiveOpsTimelineZoom Zoom { get; private set; } = LiveOpsTimelineZoom.ThreeWeeks;

        internal bool HasDeviceTier => Zoom == LiveOpsTimelineZoom.Day;

        internal float Height => MonthTierHeight + DayTierHeight + MarksTierHeight + (HasDeviceTier ? DeviceTierHeight : 0f);

        /// <summary>Nhãn đang hiện của tầng 3 (giờ máy) — rỗng ở mọi zoom trừ Ngày.</summary>
        internal IReadOnlyList<Label> VisibleDeviceLabels => VisibleOf(_deviceLabels);

        internal IReadOnlyList<Label> VisibleDayLabels => VisibleOf(_dayLabels);
        internal IReadOnlyList<Label> VisibleMonthLabels => VisibleOf(_monthLabels);

        internal void SetModel(LiveOpsTimelineModel model, LiveOpsTimelineZoom zoom, TimeSpan deviceOffset, LiveOpsHubFormat format)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            _format = format ?? throw new ArgumentNullException(nameof(format));
            _geometry = model.Geometry;
            Zoom = zoom;
            EnableInClassList(LiveOpsHubClassNames.TimelineRulerDayZoom, HasDeviceTier);
            DeviceTier.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !HasDeviceTier);
            CornerSubtitle.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineRulerCornerSubtitleFormat, format.DeviceOffsetLabel);
            Corner.tooltip = CornerSubtitle.text;

            IReadOnlyList<LiveOpsTimelineRulerTick> ticks = LiveOpsTimelineGeometry.RulerTicks(_geometry, zoom, deviceOffset);
            _lineTicks.Clear();
            int monthCount = 0;
            int dayCount = 0;
            int deviceCount = 0;
            string deviceTooltip = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineDeviceTierTooltipFormat, format.DeviceOffsetLabel);
            for (int index = 0; index < ticks.Count; index++)
            {
                LiveOpsTimelineRulerTick tick = ticks[index];
                if (tick.HasLine) _lineTicks.Add(tick);
                if (tick.Text.Length == 0) continue;
                switch (tick.Tier)
                {
                    case LiveOpsTimelineRulerTier.MonthAndWeek:
                        BindLabel(LabelAt(_monthLabels, MonthTier, monthCount++), tick, float.NaN, string.Empty);
                        break;
                    case LiveOpsTimelineRulerTier.DayOrHour:
                        BindLabel(LabelAt(_dayLabels, DayTier, dayCount++), tick, NextTickPosition(ticks, index), format.ShortDateTimeUtc(tick.TimeUtc));
                        break;
                    default:
                        BindLabel(LabelAt(_deviceLabels, DeviceTier, deviceCount++), tick, NextTickPosition(ticks, index), deviceTooltip);
                        break;
                }
            }
            HideFrom(_monthLabels, monthCount);
            HideFrom(_dayLabels, dayCount);
            HideFrom(_deviceLabels, deviceCount);

            BindNowFlag(model, format);
            BindPublishedFlag(model, format);
            // Cờ và bubble luôn cuối tầng để vẽ trên nhãn ngày (Hình 1: bubble đè số ngày quanh con trỏ).
            MonthTier.Add(NowFlag);
            DayTier.Add(Bubble);
            Track.MarkDirtyRepaint();
        }

        /// <summary>Bubble giờ tại con trỏ ở tầng 2 (V-11): chữ UTC "16/9 12:00" đã bắt lưới, tooltip giờ máy; null = ẩn (rời track).</summary>
        internal void SetCursor(DateTime? cursorUtc)
        {
            bool visible = cursorUtc.HasValue && _geometry != null && cursorUtc.Value >= _geometry.RangeStartUtc && cursorUtc.Value <= _geometry.RangeEndUtc;
            Bubble.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !visible);
            if (!visible) return;
            Bubble.text = _format.ShortDateTime(cursorUtc.Value);
            Bubble.tooltip = _format.DeviceTimeLine(cursorUtc.Value);
            Bubble.style.left = _geometry.XOf(cursorUtc.Value); // style-inline-allowed: 6
        }

        private void BindNowFlag(LiveOpsTimelineModel model, LiveOpsHubFormat format)
        {
            bool visible = model.NowUtc >= model.RangeStartUtc && model.NowUtc <= model.RangeEndUtc;
            NowFlag.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !visible);
            if (!visible) return;
            string utcClock = model.NowUtc.ToString(ClockFormat, CultureInfo.InvariantCulture);
            NowFlag.text = utcClock;
            NowFlag.tooltip = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineNowFlagTooltipFormat, utcClock,
                format.DeviceClock(model.NowUtc));
            NowFlag.style.left = model.Geometry.XOf(model.NowUtc); // style-inline-allowed: 3
        }

        private void BindPublishedFlag(LiveOpsTimelineModel model, LiveOpsHubFormat format)
        {
            bool visible = model.PublishedAtUtc.HasValue && model.PublishedAtUtc.Value >= model.RangeStartUtc &&
                           model.PublishedAtUtc.Value <= model.RangeEndUtc;
            PublishedFlag.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !visible);
            if (!visible) return;
            DateTime publishedUtc = model.PublishedAtUtc.Value;
            PublishedFlag.tooltip = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelinePublishedFlagTooltipFormat,
                format.ShortDateTime(publishedUtc), model.PublishedShortSha);
            PublishedFlag.style.left = model.Geometry.XOf(publishedUtc) - PublishedFlagOffset; // style-inline-allowed: 3
        }

        private float NextTickPosition(IReadOnlyList<LiveOpsTimelineRulerTick> ticks, int index)
        {
            LiveOpsTimelineRulerTier tier = ticks[index].Tier;
            for (int next = index + 1; next < ticks.Count; next++)
            {
                if (ticks[next].Tier == tier && ticks[next].X > ticks[index].X) return ticks[next].X;
            }
            return _geometry.TrackWidth;
        }

        private static void BindLabel(Label label, LiveOpsTimelineRulerTick tick, float nextPosition, string tooltipText)
        {
            label.text = tick.Text;
            label.tooltip = tooltipText;
            label.EnableInClassList(LiveOpsHubClassNames.TimelineRulerLabelEmphasized, tick.IsEmphasized);
            label.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, false);
            label.style.left = tick.X; // style-inline-allowed: 3
            // Nhãn ngày/giờ cắt theo bề rộng ô của nó [SD1 §3.2]; nhãn tầng 1 (tháng, tuần) không giới hạn — thước cắt ở mép track.
            label.style.width = float.IsNaN(nextPosition) ? StyleKeyword.Auto : new StyleLength(Math.Max(0f, nextPosition - tick.X)); // style-inline-allowed: 3
        }

        private static Label LabelAt(List<Label> pool, VisualElement tier, int index)
        {
            while (pool.Count <= index)
            {
                Label created = new Label { pickingMode = PickingMode.Position };
                created.AddToClassList(LiveOpsHubClassNames.TimelineRulerLabel);
                tier.Add(created);
                pool.Add(created);
            }
            return pool[index];
        }

        private static void HideFrom(List<Label> pool, int firstHidden)
        {
            for (int index = firstHidden; index < pool.Count; index++) pool[index].EnableInClassList(LiveOpsHubClassNames.TimelineHidden, true);
        }

        private static IReadOnlyList<Label> VisibleOf(List<Label> pool)
        {
            List<Label> visible = new List<Label>();
            foreach (Label label in pool)
            {
                if (!label.ClassListContains(LiveOpsHubClassNames.TimelineHidden) && label.parent != null &&
                    !label.parent.ClassListContains(LiveOpsHubClassNames.TimelineHidden))
                {
                    visible.Add(label);
                }
            }
            return visible;
        }

        private VisualElement CreateTier(string variantClassName)
        {
            VisualElement tier = new VisualElement { pickingMode = PickingMode.Ignore };
            tier.AddToClassList(LiveOpsHubClassNames.TimelineRulerTier);
            tier.AddToClassList(variantClassName);
            Track.Add(tier);
            return tier;
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent resolvedEvent)
        {
            if (Track.customStyle.TryGetValue(DayLineColorProperty, out Color dayLine)) _dayLineColor = dayLine;
            if (Track.customStyle.TryGetValue(WeekLineColorProperty, out Color weekLine))
            {
                _weekLineColor = new Color(weekLine.r, weekLine.g, weekLine.b, weekLine.a * WeekLineAlpha);
            }
            Track.MarkDirtyRepaint();
        }

        private void DrawLines(MeshGenerationContext context)
        {
            if (_geometry == null) return;
            Painter2D painter = context.painter2D;
            painter.lineWidth = LineWidth;
            for (int index = 0; index < _lineTicks.Count; index++)
            {
                LiveOpsTimelineRulerTick tick = _lineTicks[index];
                bool isMonthTier = tick.Tier == LiveOpsTimelineRulerTier.MonthAndWeek;
                Color color = isMonthTier ? _weekLineColor : _dayLineColor;
                if (color.a <= 0f) continue;
                float top = isMonthTier ? 0f : MonthTierHeight;
                float bottom = isMonthTier ? MonthTierHeight : MonthTierHeight + DayTierHeight;
                painter.strokeColor = color;
                painter.BeginPath();
                painter.MoveTo(new Vector2(tick.X + HalfPixel, top));
                painter.LineTo(new Vector2(tick.X + HalfPixel, bottom));
                painter.Stroke();
            }
        }
    }
}
