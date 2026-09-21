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

        /// <summary>
        /// Khoảng trống tối thiểu giữa hai nhãn tầng 1 (phiếu D-2 của cổng W4). Tầng 1 chứa CẢ nhãn tháng lẫn nhãn "Tuần n"
        /// [SD1 §3.2]; khi khung bắt đầu đúng thứ Hai thì hai nhãn cùng nằm ở x = 0 và chữ chồng lên nhau (đọc ra "THÁNG9 2026").
        /// Thước bỏ nhãn đứng sau — cùng cách [SD1 §3.2] đã bỏ nhãn tuần khi hết chỗ trước mép phải.
        /// </summary>
        private const float MonthTierLabelGap = 6f;

        /// <summary>padding ngang của cờ "bây giờ" trong USS (<c>padding: 0 3px</c>) — dùng để ước lượng quãng cờ chiếm.</summary>
        private const float NowFlagPadding = 3f;

        /// <summary>padding trái của nhãn thước trong USS (<c>padding: 0 0 0 2px</c>) — trừ ra khi đo ô của nhãn ngày.</summary>
        private const float DayLabelPaddingLeft = 2f;

        /// <summary>
        /// (UX-17) Bề rộng một ký tự của nhãn tầng 1 dùng để GIỮ CHỖ — nhãn sau phải bắt đầu sau chỗ này, nhãn không đủ chỗ
        /// thì bị bỏ. Lớn hơn <c>LiveOpsTimelineGeometry.LabelCharacterWidth</c> (5,6px) vì tầng này có
        /// <c>letter-spacing: 0.5px</c> và mốc tháng còn <c>-unity-font-style: bold</c>. Đo thật trên 6000.6:
        /// "THÁNG 3 2027" (12 ký tự) vẽ ra 76px, tức 6,33px/ký tự — ước bằng 5,6 thì thiếu gần 9px và nhãn sát mép track bị
        /// cắt dù phép tính bảo là vừa.
        /// <para>
        /// (W10, phiếu soát F7) Hằng này GIỮ NGUYÊN 6,4 — thử nâng lên 7,2 rồi ĐO RA HỎNG: chỗ giữ rộng thêm 12,5% làm nhãn
        /// tháng nuốt một vạch tuần, và ở mức thu nhỏ Ba tuần cửa sổ 1280 bản tiếng Anh số nhãn "Week n" tụt từ 3 xuống 2
        /// (plan/w10/logs/weekcount-old.txt so với weekcount-new.txt). Mất một số tuần để lấy khoảng dư cho một nhãn tháng
        /// là đổi hỏng. Khoảng dư nay lấy bằng <see cref="MonthTierLabelBoxCharacterWidth"/> — một số RIÊNG, chỉ đụng tới Ô
        /// của nhãn chứ không đụng tới luật nhường chỗ.
        /// </para>
        /// </summary>
        private const float MonthTierLabelCharacterWidth = 6.4f;

        /// <summary>
        /// (W9-25 chỗ #7) Bề rộng một ký tự dùng để ghi Ô của nhãn tầng 1 — RỘNG HƠN chỗ giữ chỗ ở trên, và đó là chủ ý.
        /// <para>
        /// Vì sao cần: trước W10 nhãn tầng 1 để <c>width: auto</c> nên ô ÔM KHÍT chữ — đo trên 2022.3 "THÁNG 11 2026" được ô
        /// 78px cho chữ 77px, dư đúng 1px. Một lần đổi metric phông là cắt im lặng một mốc tháng thành "THÁNG 3 20", một mốc
        /// KHÔNG có thật; và luật "chữ chiếm > 95% bề rộng ô" của cổng bố cục không có chỗ nào để đo. 7,2 so với 6,33px/ký tự
        /// đo thật để lại ≥ 12% dư ở cả hai bản Unity và cả hai ngôn ngữ.
        /// </para>
        /// <para>
        /// Vì sao ô được phép RỘNG HƠN chỗ giữ: nhãn tầng 1 là <c>position: absolute</c> và KHÔNG có nền — phần ô thừa đè
        /// sang chỗ của nhãn kế chỉ là khoảng trong suốt, chữ vẫn không chạm nhau vì luật nhường chỗ vẫn tính bằng
        /// <see cref="MonthTierLabelCharacterWidth"/>. Điều kiện duy nhất: ô KHÔNG được thò khỏi mép track — cha cắt con nên
        /// phần thò ra là lỗi <c>childOverflow</c> thật; <see cref="BindMonthTierLabels"/> kéo nhãn vào trong, và khi không
        /// còn chỗ cho cả ô thì co ô về đúng chỗ giữ (hành vi cũ) chứ không bỏ hẳn một mốc tháng.
        /// </para>
        /// </summary>
        private const float MonthTierLabelBoxCharacterWidth = 7.2f;

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
                        // Tầng 1 bind sau cả vòng: thứ tự tick trong danh sách là "tháng trước, tuần sau", không theo x,
                        // nên phải sắp theo x mới biết nhãn nào chồng nhãn nào (phiếu D-2).
                        break;
                    case LiveOpsTimelineRulerTier.DayOrHour:
                        BindDayLabel(LabelAt(_dayLabels, DayTier, dayCount++), tick, NextTickPosition(ticks, index), format.ShortDateTimeUtc(tick.TimeUtc));
                        break;
                    default:
                        BindDeviceLabel(LabelAt(_deviceLabels, DeviceTier, deviceCount++), tick, NextTickPosition(ticks, index), deviceTooltip);
                        break;
                }
            }
            // (UX-17, V9) Cờ "bây giờ" bind TRƯỚC nhãn tầng 1: nhãn tháng phải biết cờ đang chiếm quãng nào mới né được.
            BindNowFlag(model, format);
            monthCount = BindMonthTierLabels(ticks, NowFlagRange());
            HideFrom(_monthLabels, monthCount);
            HideFrom(_dayLabels, dayCount);
            HideFrom(_deviceLabels, deviceCount);

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

        /// <summary>
        /// Quãng x mà cờ "bây giờ" chiếm trên tầng 1 (cờ căn giữa vạch bằng <c>translate: -50%</c>). Đo bằng ước lượng
        /// <c>LabelCharacterWidth</c> + padding của cờ: bind chạy TRƯỚC layout nên <c>worldBound</c> của cờ chưa có số thật.
        /// Trả về quãng rỗng khi cờ đang ẩn.
        /// </summary>
        private (float left, float right) NowFlagRange()
        {
            if (NowFlag.ClassListContains(LiveOpsHubClassNames.TimelineHidden) || NowFlag.text.Length == 0)
            {
                return (float.NaN, float.NaN);
            }
            float width = NowFlag.text.Length * LiveOpsTimelineGeometry.LabelCharacterWidth + NowFlagPadding * 2f;
            float center = NowFlag.style.left.value.value;
            return (center - width / 2f, center + width / 2f);
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

        /// <summary>
        /// Bind nhãn tầng 1 theo thứ tự x và bỏ nhãn nào chồng lên nhãn đứng trước (phiếu D-2). Trả về số nhãn đã hiện.
        /// Tick tháng được thêm trước tick tuần nên khi trùng x, nhãn THÁNG giữ chỗ còn nhãn "Tuần n" bị bỏ — nhãn tháng là
        /// thứ duy nhất nói năm, bỏ nó thì khung không còn mốc tháng nào.
        /// </summary>
        private int BindMonthTierLabels(IReadOnlyList<LiveOpsTimelineRulerTick> ticks, (float left, float right) nowFlag)
        {
            List<LiveOpsTimelineRulerTick> tierTicks = new List<LiveOpsTimelineRulerTick>();
            for (int index = 0; index < ticks.Count; index++)
            {
                if (ticks[index].Tier != LiveOpsTimelineRulerTier.MonthAndWeek) continue;
                if (ticks[index].Text.Length == 0) continue;
                InsertByPosition(tierTicks, ticks[index]);
            }

            int visibleCount = 0;
            float nextFreeX = float.NegativeInfinity;
            bool hasFlag = !float.IsNaN(nowFlag.left);
            for (int index = 0; index < tierTicks.Count; index++)
            {
                LiveOpsTimelineRulerTick tick = tierTicks[index];
                if (tick.X < nextFreeX) continue;
                float width = tick.Text.Length * MonthTierLabelCharacterWidth;
                float left = tick.X;
                // (UX-17, V9) Cờ "08:47" nằm đè nhãn tháng là mất luôn NĂM — nhãn tháng là chỗ duy nhất nói năm. Nhãn dời sang
                // phải cờ; hết chỗ trước mốc tháng kế thì bỏ nhãn như luật chồng nhãn sẵn có.
                if (hasFlag && left < nowFlag.right && left + width > nowFlag.left)
                {
                    // (R-09) Né sang phải mà KHÔNG kẹp theo bề rộng track thì khi "bây giờ" rơi sát mốc tháng gần mép phải, nhãn
                    // bị đẩy hẳn ra ngoài khung và mất sạch — tệ hơn cả lúc chưa né (trước đó còn ló một phần). Hết chỗ bên phải
                    // thì thử né sang TRÁI cờ; bên trái cũng hết chỗ thì bỏ nhãn theo đúng luật chồng nhãn sẵn có.
                    float dodgeRight = nowFlag.right + MonthTierLabelGap;
                    if (dodgeRight + width <= _geometry.TrackWidth)
                    {
                        left = dodgeRight;
                    }
                    else
                    {
                        float dodgeLeft = nowFlag.left - MonthTierLabelGap - width;
                        if (dodgeLeft < 0f || dodgeLeft < nextFreeX) continue;
                        left = dodgeLeft;
                    }
                }
                // (UX-17) Ô của nhãn tầng 1 luôn rộng hơn chữ (xem khối ghi ô bên dưới), nên chỗ duy nhất cắt nó là mép
                // phải của track. Mốc tháng nằm ngoài khoảng đang xem (thu nhỏ hết cỡ sinh cả tick tháng sau mép track) thì
                // bỏ hẳn; nhãn ló một phần thì kéo vào trong mép, và chỉ kéo khi không đè nhãn đứng trước — "THÁNG 3 2027"
                // bị mép cắt còn "THÁNG 3 20" là đọc ra một mốc KHÔNG có thật. Bước kéo này tính bằng CHỖ GIỮ (đủ cho chữ);
                // bước ghi ô bên dưới kéo thêm nếu còn chỗ cho cả ô.
                if (left >= _geometry.TrackWidth) continue;
                if (left + width > _geometry.TrackWidth)
                {
                    float pulledLeft = _geometry.TrackWidth - width;
                    if (pulledLeft < nextFreeX) continue;
                    left = pulledLeft;
                }
                // (W9-25 chỗ #7) Ô của nhãn ghi thẳng, không để `width: auto` (ô tự co ôm khít chữ thì không đo được khoảng
                // dư). Ô rộng hơn chỗ giữ 12,5% nên luật "chữ chiếm > 95% ô" có số để kiểm; nhưng ô KHÔNG được thò khỏi mép
                // track vì cha cắt con: còn chỗ thì kéo nhãn vào cho vừa cả ô, hết chỗ thì co ô về đúng chỗ giữ — thà một
                // nhãn sát mép chật ô còn hơn mất hẳn một mốc tháng.
                float boxWidth = tick.Text.Length * MonthTierLabelBoxCharacterWidth;
                if (left + boxWidth > _geometry.TrackWidth)
                {
                    float boxLeft = _geometry.TrackWidth - boxWidth;
                    if (boxLeft >= nextFreeX && boxLeft >= 0f) left = boxLeft;
                    else boxWidth = _geometry.TrackWidth - left;
                }
                BindLabel(LabelAt(_monthLabels, MonthTier, visibleCount++), tick, tick.X + boxWidth, string.Empty, left);
                nextFreeX = left + width + MonthTierLabelGap;
            }
            return visibleCount;
        }

        /// <summary>Chèn giữ thứ tự x, tick thêm trước thắng khi trùng x (List.Sort không ổn định nên không dùng được ở đây).</summary>
        private static void InsertByPosition(List<LiveOpsTimelineRulerTick> sorted, LiveOpsTimelineRulerTick tick)
        {
            int position = sorted.Count;
            while (position > 0 && sorted[position - 1].X > tick.X) position--;
            sorted.Insert(position, tick);
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

        private static void BindLabel(Label label, LiveOpsTimelineRulerTick tick, float nextPosition, string tooltipText,
            float left = float.NaN)
        {
            BindLabelText(label, tick, nextPosition, tooltipText, tick.Text, left);
        }

        /// <summary>
        /// (UX-17, UJ-19) Nhãn tầng 2 rút gọn dần cho tới khi lọt ô của nó: "Mon 14/9" → "14/9" → "14". Bản cũ luôn ghi chữ đầy
        /// đủ rồi để <c>overflow: hidden</c> cắt, nên ô 25px ở cửa sổ 700 đọc ra "Mon 1" — một NGÀY KHÁC, không chỉ xấu chữ.
        /// Chữ đầy đủ vẫn còn trong tooltip.
        /// </summary>
        private static void BindDayLabel(Label label, LiveOpsTimelineRulerTick tick, float nextPosition, string tooltipText)
        {
            float room = float.IsNaN(nextPosition)
                ? float.PositiveInfinity
                : Math.Max(0f, nextPosition - tick.X) - DayLabelPaddingLeft;
            string text = FitDayText(tick, room);
            if (text == null)
            {
                // (R-08) Ô hẹp hơn cả bậc ngắn nhất: ẩn hẳn nhãn theo đúng luật chồng nhãn của tầng 1. Vẽ rồi để
                // overflow:hidden cắt "14" thành "1" là đọc ra một NGÀY KHÁC — chính lỗi UX-17 muốn diệt. Ngày đầy đủ
                // vẫn còn trong tooltip của vạch.
                label.text = string.Empty;
                label.tooltip = tooltipText;
                label.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, true);
                return;
            }
            BindLabelText(label, tick, nextPosition, tooltipText, text, float.NaN);
        }

        /// <summary>
        /// (UX-17) Nhãn tầng giờ-máy: ô hẹp hơn chữ thì ẨN hẳn, không vẽ rồi để <c>overflow: hidden</c> cắt.
        /// <para>
        /// Vì sao cần luật riêng: tầng này đi đường <c>BindLabel</c> trần, không có bậc rút gọn nào như
        /// <see cref="BindDayLabel"/>. Vạch cuối của thước lấy mép track làm mốc kế, nên ô của nó chỉ còn vài pixel —
        /// "22:00" vẽ trong ô 2px đọc ra "2", tức MỘT GIỜ KHÁC. Đây đúng loại lỗi mà luật R-08 của tầng ngày đã diệt;
        /// giờ đầy đủ vẫn còn trong tooltip của vạch.
        /// </para>
        /// </summary>
        private static void BindDeviceLabel(Label label, LiveOpsTimelineRulerTick tick, float nextPosition, string tooltipText)
        {
            float room = float.IsNaN(nextPosition)
                ? float.PositiveInfinity
                : Math.Max(0f, nextPosition - tick.X);
            if (!Fits(tick.Text, room))
            {
                label.text = string.Empty;
                label.tooltip = tooltipText;
                label.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, true);
                return;
            }
            BindLabel(label, tick, nextPosition, tooltipText);
        }

        /// <summary>Bậc rút gọn đầu tiên lọt ô; <c>null</c> khi không bậc nào lọt (nơi gọi ẩn nhãn — R-08).</summary>
        private static string FitDayText(LiveOpsTimelineRulerTick tick, float room)
        {
            string full = tick.Text;
            if (Fits(full, room)) return full;
            string withoutPrefix = WithoutMondayPrefix(full);
            if (Fits(withoutPrefix, room)) return withoutPrefix;
            int separator = withoutPrefix.IndexOf('/');
            string dayOnly = separator > 0 ? withoutPrefix.Substring(0, separator) : withoutPrefix;
            return Fits(dayOnly, room) ? dayOnly : null;
        }

        private static bool Fits(string text, float room)
        {
            return text.Length * LiveOpsTimelineGeometry.LabelCharacterWidth <= room;
        }

        private static string WithoutMondayPrefix(string text)
        {
            string prefix = LiveOpsHubStrings.TimelineRulerMondayPrefix + " ";
            return text.StartsWith(prefix, StringComparison.Ordinal) ? text.Substring(prefix.Length) : text;
        }

        private static void BindLabelText(Label label, LiveOpsTimelineRulerTick tick, float nextPosition, string tooltipText,
            string text, float left)
        {
            label.text = text;
            label.tooltip = tooltipText;
            label.EnableInClassList(LiveOpsHubClassNames.TimelineRulerLabelEmphasized, tick.IsEmphasized);
            label.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, false);
            label.style.left = float.IsNaN(left) ? tick.X : left; // style-inline-allowed: 3
            // Nhãn ngày/giờ cắt theo bề rộng ô của nó [SD1 §3.2]. (W9-25 chỗ #7) Nhãn tầng 1 (tháng, tuần) TRƯỚC ĐÂY để
            // `width: auto` — ô ôm khít chữ, dư 1px, không đo được; nay nơi gọi (BindMonthTierLabels) truyền thẳng ô rộng hơn
            // chỗ giữ, nên nhánh Auto dưới đây chỉ còn cho nhãn nào thật sự không có mốc kế (nextPosition = NaN).
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
