using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Tầng của thước timeline [SD1 §3.2]: 1 tháng/tuần/cờ, 2 ngày (hoặc giờ UTC ở zoom Ngày), 3 giờ máy (chỉ zoom Ngày).</summary>
    internal enum LiveOpsTimelineRulerTier
    {
        MonthAndWeek = 1,
        DayOrHour = 2,
        DeviceTime = 3,
    }

    /// <summary>
    /// Một vạch/nhãn của thước. Dữ liệu thuần để view (G-TIMELINE-VIEW) dựng Label trong pool — nhãn thước là Label vì cần
    /// tooltip, cắt chữ và đậm thứ Hai [SD1 §3.14], nên hình học chỉ nói "ở đâu, chữ gì, có đậm/vạch không".
    /// </summary>
    internal sealed class LiveOpsTimelineRulerTick
    {
        public LiveOpsTimelineRulerTick(LiveOpsTimelineRulerTier tier, DateTime timeUtc, float trackPosition, string text, bool isEmphasized, bool hasLine)
        {
            Tier = tier;
            TimeUtc = timeUtc;
            X = trackPosition;
            Text = text ?? string.Empty;
            IsEmphasized = isEmphasized;
            HasLine = hasLine;
        }

        public LiveOpsTimelineRulerTier Tier { get; }
        public DateTime TimeUtc { get; }

        /// <summary>Toạ độ trong track (0 = mép trái track, không tính header làn 168px).</summary>
        public float X { get; }

        /// <summary>"" khi nhãn bị bỏ vì hết chỗ trước mép phải — view vẫn vẽ vạch nếu <see cref="HasLine"/>, không dựng Label rỗng.</summary>
        public string Text { get; }

        /// <summary>Thứ Hai và nhãn tháng in đậm — weekly-pass neo vào thứ Hai nên mốc tuần phải nổi.</summary>
        public bool IsEmphasized { get; }

        /// <summary>Có vạch dọc 1px dưới nhãn. Tuần đầu cụt ở zoom Tháng chỉ ghi ngày, không vạch (Hình 12 khung 11).</summary>
        public bool HasLine { get; }

        public override string ToString() => Tier + " " + Text + " @" + X.ToString("0.0", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Hình học thuần của timeline: thời gian ↔ toạ độ track, hình chữ nhật thanh, bước bắt lưới, nhãn thanh, khoảng của preset
    /// zoom, zoom liên tục tại con trỏ, vạch thước. Không đụng VisualElement nên test Logic chạy <c>-nographics</c>; control
    /// (W3) chỉ đọc lại số ở đây để ảnh chụp và test khớp cùng một công thức [SD1 §3.2–3.5].
    /// </summary>
    internal sealed class LiveOpsTimelineGeometry
    {
        public const float LaneHeaderWidth = 168f, CompactLaneHeaderWidth = 120f, BarHeight = 20f, RowPitch = 28f;
        public const float LanePaddingTop = 4f, LanePaddingTopWithOverlap = 16f, MinimumBarWidth = 2f, StripGapThreshold = 3f;
        public const float EdgeGrabWidth = 8f, ReadoutMargin = 6f;

        /// <summary>(V-11) Kẹp zoom liên tục: dưới 0,25 px/giờ một năm đã vừa ~2.200px; trên 48 px/giờ một phút chưa tới 1px.</summary>
        public const double MinimumPixelsPerHour = 0.25, MaximumPixelsPerHour = 48;

        /// <summary>(V-11) Mỗi nấc bánh xe nhân/chia 1,15 — nhỏ đủ để trackpad (nhiều delta nhỏ) mượt, lớn đủ để chuột có nấc không lê thê.</summary>
        public const double ZoomFactorPerWheelNotch = 1.15;

        /// <summary>Shift + bánh xe: mỗi nấc cuộn một khoảng cố định theo pixel (không theo giờ) để cảm giác cuộn giống nhau ở mọi zoom.</summary>
        public const float ScrollPixelsPerWheelNotch = 40f;

        /// <summary>Preset 3 tuần/Tháng bắt đầu 5 ngày trước hôm nay: vạch bây giờ rơi vào khoảng 1/4 track (162px/635 ở Hình 1), vừa thấy đợt vừa khép vừa thấy đường phía trước.</summary>
        public const int LeadingDaysBeforeToday = 5;

        // Thuật toán barLabel của mockup [SD1 §3.5]: font mono ≈ 5,6px/ký tự; padding 12; vuông "khác bản đã đăng" chiếm 8; icon lặp 13.
        public const float LabelCharacterWidth = 5.6f;
        public const float LabelHorizontalPadding = 12f;
        public const float ChangedMarkerReserve = 8f;
        public const float RecurringIconReserve = 13f;
        public const float LabelMinimumBarWidth = 24f;
        public const float LabelFullIdMinimumBarWidth = 64f;
        public const float LabelMinimumRoom = 8f;
        private const int LabelMinimumKeptCharacters = 3;

        // Bắt lưới tự động [SD1 §3.2 bảng zoom]: 15 phút ở zoom Ngày, 1 giờ ở 3 tuần, 1 ngày ở Tháng. Ngưỡng nằm giữa các preset
        // (Ngày ≈ 26 px/giờ, 3 tuần ≈ 1,26, Tháng ≈ 0,63) để cửa sổ hẹp 820px (3 tuần còn 1,22 px/giờ) vẫn giữ bước của preset.
        public const double QuarterHourSnapMinimumPixelsPerHour = 6;
        public const double HourSnapMinimumPixelsPerHour = 0.8;

        // Thước [SD1 §3.2, Hình 12 khung 11]: bỏ nhãn tuần khi không đủ chỗ trước mép phải track — tick vẫn có, chữ rỗng, còn vạch.
        public const float WeekLabelMinimumRoom = 48f;
        public const float MonthZoomWeekLabelMinimumRoom = 44f;
        public const int HourTickStepHours = 3;

        private static readonly Regex OrdinalSuffixPattern = new Regex("^[0-9]{1,4}[a-z]?$", RegexOptions.CultureInvariant);
        private static readonly TimeSpan QuarterHour = TimeSpan.FromMinutes(15);

        private readonly float _trackWidth;

        public LiveOpsTimelineGeometry(DateTime rangeStartUtc, DateTime rangeEndUtc, float trackWidth)
        {
            if (rangeEndUtc <= rangeStartUtc) throw new ArgumentException(LiveOpsHubStrings.TimelineRangeInvalid, nameof(rangeEndUtc));
            if (!(trackWidth > 0f) || float.IsInfinity(trackWidth))
            {
                throw new ArgumentOutOfRangeException(nameof(trackWidth), LiveOpsHubStrings.TimelineTrackWidthInvalid);
            }
            RangeStartUtc = DateTime.SpecifyKind(rangeStartUtc, DateTimeKind.Utc);
            RangeEndUtc = DateTime.SpecifyKind(rangeEndUtc, DateTimeKind.Utc);
            _trackWidth = trackWidth;
            PixelsPerHour = trackWidth / (RangeEndUtc - RangeStartUtc).TotalHours;
        }

        public DateTime RangeStartUtc { get; }
        public DateTime RangeEndUtc { get; }
        public float TrackWidth => _trackWidth;

        /// <summary>= bề rộng track ÷ số giờ của khoảng [SD1 §3.2] — zoom 3 tuần vẫn vừa đúng 21 ngày khi track hẹp lại.</summary>
        public double PixelsPerHour { get; }

        public float XOf(DateTime utc)
        {
            return (float)((utc - RangeStartUtc).TotalHours * PixelsPerHour);
        }

        public DateTime TimeAt(float trackPosition)
        {
            return AddTicksClamped(RangeStartUtc, (long)Math.Round(trackPosition / PixelsPerHour * TimeSpan.TicksPerHour));
        }

        /// <summary>
        /// [SD1 §3.2]: <c>x1 = max(0, X(start))</c>, <c>x2 = min(track, X(end))</c>, <c>rộng = max(2, x2 − x1 − 1)</c> — trừ 1px để hai
        /// thanh liền nhau (weekly-pass chạy hết chu kỳ) vẫn có khe nhìn thấy. Cờ cắt mép cho dấu ◂ ▸.
        /// </summary>
        public (float left, float width, bool clippedStart, bool clippedEnd) BarRect(DateTime startUtc, DateTime endUtc)
        {
            float left = Math.Max(0f, XOf(startUtc));
            float right = Math.Min(_trackWidth, XOf(endUtc));
            float width = Math.Max(MinimumBarWidth, right - left - 1f);
            bool clippedStart = startUtc < RangeStartUtc;
            bool clippedEnd = endUtc > RangeEndUtc;
            return (left, width, clippedStart, clippedEnd);
        }

        /// <summary>Bề rộng thật của khoảng trên track (chưa trừ 1px khe, chưa kẹp tối thiểu) — dùng để quyết định gom dải và nhãn.</summary>
        public float SpanWidth(DateTime startUtc, DateTime endUtc)
        {
            return (float)((endUtc - startUtc).TotalHours * PixelsPerHour);
        }

        public static TimeSpan AutoSnapStep(double pixelsPerHour)
        {
            if (pixelsPerHour >= QuarterHourSnapMinimumPixelsPerHour) return QuarterHour;
            if (pixelsPerHour >= HourSnapMinimumPixelsPerHour) return TimeSpan.FromHours(1);
            return TimeSpan.FromDays(1);
        }

        /// <summary>Làm tròn tới bội gần nhất của bước tính từ gốc DateTime (00:00 UTC) — mọi bước chia hết một ngày nên lưới trùng nửa đêm UTC.</summary>
        public static DateTime Snap(DateTime utc, TimeSpan step)
        {
            if (step <= TimeSpan.Zero) return utc;
            long stepTicks = step.Ticks;
            long remainder = utc.Ticks % stepTicks;
            long floorTicks = utc.Ticks - remainder;
            long snappedTicks = remainder * 2 >= stepTicks ? floorTicks + stepTicks : floorTicks;
            if (snappedTicks > DateTime.MaxValue.Ticks) snappedTicks = floorTicks;
            return new DateTime(snappedTicks, DateTimeKind.Utc);
        }

        /// <summary>
        /// Thuật toán barLabel [SD1 §3.5]: <c>room = w − 12 − (changed ? 8 : 0)</c>; <c>w &lt; 24</c> hoặc <c>room &lt; 8</c> → không nhãn;
        /// <c>w &lt; 64</c> → số thứ tự (hậu tố sau dấu "-" cuối khớp <c>^\d{1,4}[a-z]?$</c>) nếu vừa, không thì trống — id như
        /// hunt-0916-bonus dựa vào tooltip; còn lại id đầy đủ, dài quá thì cắt giữa giữ <c>ceil(k/2)</c> đầu + "…" + <c>floor(k/2)</c> cuối.
        /// </summary>
        public static string BarLabel(string eventId, float width, bool isRecurring, bool isChanged)
        {
            string identifier = eventId ?? string.Empty;
            float room = width - LabelHorizontalPadding - (isChanged ? ChangedMarkerReserve : 0f);
            if (width < LabelMinimumBarWidth || room < LabelMinimumRoom || identifier.Length == 0) return string.Empty;

            if (width < LabelFullIdMinimumBarWidth)
            {
                int separatorIndex = identifier.LastIndexOf('-');
                string suffix = separatorIndex >= 0 ? identifier.Substring(separatorIndex + 1) : identifier;
                if (!OrdinalSuffixPattern.IsMatch(suffix)) return string.Empty;
                return suffix.Length * LabelCharacterWidth <= room ? suffix : string.Empty;
            }

            float textRoom = room - (isRecurring ? RecurringIconReserve : 0f);
            int maximumCharacters = (int)Math.Floor(textRoom / LabelCharacterWidth);
            if (identifier.Length <= maximumCharacters) return identifier;

            int keptCharacters = Math.Max(LabelMinimumKeptCharacters, maximumCharacters - 1);
            if (keptCharacters >= identifier.Length) return identifier;
            int headLength = (keptCharacters + 1) / 2;
            int tailLength = keptCharacters / 2;
            return identifier.Substring(0, headLength) + LiveOpsHubStrings.TimelineBarLabelEllipsis +
                   identifier.Substring(identifier.Length - tailLength);
        }

        /// <summary>Ngày: 00:00 hôm nay (khoảng 24 giờ, PD-18). 3 tuần/Tháng: 00:00 của 5 ngày trước hôm nay — mẫu 13/9 08:47 → 8/9.</summary>
        public static DateTime RangeStartFor(DateTime nowUtc, LiveOpsTimelineZoom zoom)
        {
            DateTime today = DateTime.SpecifyKind(nowUtc.Date, DateTimeKind.Utc);
            if (zoom == LiveOpsTimelineZoom.Day) return today;
            return AddTicksClamped(today, -TimeSpan.FromDays(LeadingDaysBeforeToday).Ticks);
        }

        public static TimeSpan RangeLengthOf(LiveOpsTimelineZoom zoom)
        {
            switch (zoom)
            {
                case LiveOpsTimelineZoom.Day: return TimeSpan.FromHours(24);
                case LiveOpsTimelineZoom.ThreeWeeks: return TimeSpan.FromDays(21);
                default: return TimeSpan.FromDays(42);
            }
        }

        /// <summary>
        /// (V-11) Zoom liên tục tại con trỏ: thời điểm dưới <paramref name="anchorX"/> đứng yên, px/giờ nhân <c>1,15^(−nấc)</c> rồi kẹp
        /// [0,25; 48]. <paramref name="wheelDelta"/> tính theo NẤC (view chia delta của WheelEvent cho cỡ một nấc; trackpad ra số lẻ và
        /// cộng dồn được); dương = cuộn xuống = thu nhỏ, như zoom bản đồ.
        /// </summary>
        public static (DateTime rangeStartUtc, double pixelsPerHour) ZoomAround(DateTime rangeStartUtc, double pixelsPerHour, double wheelDelta,
            float anchorX)
        {
            double currentScale = ClampScale(pixelsPerHour);
            double nextScale = ClampScale(currentScale * Math.Pow(ZoomFactorPerWheelNotch, -wheelDelta));
            double anchorOffsetTicks = anchorX / currentScale * TimeSpan.TicksPerHour;
            DateTime anchorUtc = AddTicksClamped(rangeStartUtc, (long)Math.Round(anchorOffsetTicks));
            double nextOffsetTicks = anchorX / nextScale * TimeSpan.TicksPerHour;
            return (AddTicksClamped(anchorUtc, -(long)Math.Round(nextOffsetTicks)), nextScale);
        }

        /// <summary>(V-11) Shift + bánh xe: dời khung một số pixel cố định mỗi nấc, quy ra giờ theo zoom hiện hành; dương = sang phải (tương lai).</summary>
        public static DateTime ScrollBy(DateTime rangeStartUtc, double pixelsPerHour, double wheelDelta)
        {
            double scale = ClampScale(pixelsPerHour);
            double offsetTicks = wheelDelta * ScrollPixelsPerWheelNotch / scale * TimeSpan.TicksPerHour;
            return AddTicksClamped(rangeStartUtc, (long)Math.Round(offsetTicks));
        }

        /// <summary>
        /// (V-11) Vạch thước theo zoom [SD1 §3.2, §3.13]. Tầng 1: nhãn tháng ở đầu khoảng và mỗi ngày 1, "Tuần n" ở thứ Hai (3 tuần).
        /// Tầng 2: Ngày — giờ UTC mỗi 3 giờ "00" "03"…; 3 tuần — mỗi ngày "13", thứ Hai "T2 14" đậm; Tháng — thứ Hai "T2 14/9" đậm có
        /// vạch, tuần đầu cụt chỉ "8/9" không vạch. Tầng 3 (chỉ Ngày): giờ máy "07:00" tại cùng x với tầng 2 — ô nhập vẫn là UTC,
        /// giờ máy chỉ để đọc [SD1 §3.13].
        /// </summary>
        public static IReadOnlyList<LiveOpsTimelineRulerTick> RulerTicks(LiveOpsTimelineGeometry geometry, LiveOpsTimelineZoom zoom, TimeSpan deviceOffset)
        {
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));
            var ticks = new List<LiveOpsTimelineRulerTick>();
            var format = new LiveOpsHubFormat(deviceOffset);

            AddMonthTicks(geometry, ticks);
            if (zoom == LiveOpsTimelineZoom.ThreeWeeks) AddWeekTicks(geometry, format, ticks);

            switch (zoom)
            {
                case LiveOpsTimelineZoom.Day:
                    AddHourTicks(geometry, format, ticks);
                    break;
                case LiveOpsTimelineZoom.ThreeWeeks:
                    AddDayTicks(geometry, ticks);
                    break;
                default:
                    AddMondayTicks(geometry, ticks);
                    break;
            }
            return ticks;
        }

        internal static double ClampScale(double pixelsPerHour)
        {
            if (double.IsNaN(pixelsPerHour)) return MinimumPixelsPerHour;
            return Math.Max(MinimumPixelsPerHour, Math.Min(MaximumPixelsPerHour, pixelsPerHour));
        }

        internal static DateTime AddTicksClamped(DateTime value, long ticks)
        {
            if (ticks > 0 && value.Ticks > DateTime.MaxValue.Ticks - ticks) return new DateTime(DateTime.MaxValue.Ticks, DateTimeKind.Utc);
            if (ticks < 0 && value.Ticks < DateTime.MinValue.Ticks - ticks) return new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Utc);
            return new DateTime(value.Ticks + ticks, DateTimeKind.Utc);
        }

        private static void AddMonthTicks(LiveOpsTimelineGeometry geometry, List<LiveOpsTimelineRulerTick> ticks)
        {
            DateTime start = geometry.RangeStartUtc;
            ticks.Add(new LiveOpsTimelineRulerTick(LiveOpsTimelineRulerTier.MonthAndWeek, start, 0f, MonthLabel(start), true, false));
            DateTime monthStart = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            for (DateTime boundary = NextMonth(monthStart); boundary < geometry.RangeEndUtc; boundary = NextMonth(boundary))
            {
                if (boundary <= start) continue;
                ticks.Add(new LiveOpsTimelineRulerTick(LiveOpsTimelineRulerTier.MonthAndWeek, boundary, geometry.XOf(boundary),
                    MonthLabel(boundary), true, true));
                if (boundary.Year >= DateTime.MaxValue.Year) break;
            }
        }

        private static void AddWeekTicks(LiveOpsTimelineGeometry geometry, LiveOpsHubFormat format, List<LiveOpsTimelineRulerTick> ticks)
        {
            foreach (DateTime day in DayBoundaries(geometry))
            {
                if (day.DayOfWeek != DayOfWeek.Monday) continue;
                float trackPosition = geometry.XOf(day);
                // Hết chỗ trước mép phải chỉ bỏ NHÃN "Tuần n"; vạch thứ Hai vẫn giữ để mốc neo weekly-pass không mất ở cuối khung [SD1 §3.2].
                string text = geometry.TrackWidth - trackPosition < WeekLabelMinimumRoom ? string.Empty : format.IsoWeekLabel(day);
                ticks.Add(new LiveOpsTimelineRulerTick(LiveOpsTimelineRulerTier.MonthAndWeek, day, trackPosition, text, false, true));
            }
        }

        private static void AddDayTicks(LiveOpsTimelineGeometry geometry, List<LiveOpsTimelineRulerTick> ticks)
        {
            foreach (DateTime day in DayBoundaries(geometry))
            {
                bool isMonday = day.DayOfWeek == DayOfWeek.Monday;
                string dayNumber = day.Day.ToString(CultureInfo.InvariantCulture);
                string text = isMonday ? LiveOpsHubStrings.TimelineRulerMondayPrefix + " " + dayNumber : dayNumber;
                ticks.Add(new LiveOpsTimelineRulerTick(LiveOpsTimelineRulerTier.DayOrHour, day, geometry.XOf(day), text, isMonday, true));
            }
        }

        private static void AddMondayTicks(LiveOpsTimelineGeometry geometry, List<LiveOpsTimelineRulerTick> ticks)
        {
            DateTime start = geometry.RangeStartUtc;
            bool startsOnMonday = start.TimeOfDay == TimeSpan.Zero && start.DayOfWeek == DayOfWeek.Monday;
            if (!startsOnMonday)
            {
                // Tuần đầu cụt: chỉ ngày "8/9" nhạt, không vạch (Hình 12 khung 11).
                ticks.Add(new LiveOpsTimelineRulerTick(LiveOpsTimelineRulerTier.DayOrHour, start, 0f, DayMonthText(start), false, false));
            }
            foreach (DateTime day in DayBoundaries(geometry))
            {
                if (day.DayOfWeek != DayOfWeek.Monday) continue;
                float trackPosition = geometry.XOf(day);
                // Hình 12 khung 11: còn < 44px tới mép phải thì chỉ bỏ nhãn, vạch thứ Hai vẫn vẽ.
                string text = geometry.TrackWidth - trackPosition < MonthZoomWeekLabelMinimumRoom
                    ? string.Empty
                    : LiveOpsHubStrings.TimelineRulerMondayPrefix + " " + DayMonthText(day);
                ticks.Add(new LiveOpsTimelineRulerTick(LiveOpsTimelineRulerTier.DayOrHour, day, trackPosition, text, true, true));
            }
        }

        private static void AddHourTicks(LiveOpsTimelineGeometry geometry, LiveOpsHubFormat format, List<LiveOpsTimelineRulerTick> ticks)
        {
            TimeSpan step = TimeSpan.FromHours(HourTickStepHours);
            long firstTicks = geometry.RangeStartUtc.Ticks + (step.Ticks - geometry.RangeStartUtc.Ticks % step.Ticks) % step.Ticks;
            for (long tickTime = firstTicks; tickTime < geometry.RangeEndUtc.Ticks; tickTime += step.Ticks)
            {
                var utc = new DateTime(tickTime, DateTimeKind.Utc);
                float trackPosition = geometry.XOf(utc);
                string hourText = utc.Hour.ToString("00", CultureInfo.InvariantCulture);
                ticks.Add(new LiveOpsTimelineRulerTick(LiveOpsTimelineRulerTier.DayOrHour, utc, trackPosition, hourText, utc.Hour == 0, true));
                ticks.Add(new LiveOpsTimelineRulerTick(LiveOpsTimelineRulerTier.DeviceTime, utc, trackPosition, format.DeviceClock(utc), false, false));
                if (DateTime.MaxValue.Ticks - tickTime < step.Ticks) break;
            }
        }

        /// <summary>Mọi mốc 00:00 UTC nằm trong [đầu khoảng, cuối khoảng).</summary>
        private static IEnumerable<DateTime> DayBoundaries(LiveOpsTimelineGeometry geometry)
        {
            DateTime day = DateTime.SpecifyKind(geometry.RangeStartUtc.Date, DateTimeKind.Utc);
            if (day < geometry.RangeStartUtc) day = AddTicksClamped(day, TimeSpan.TicksPerDay);
            while (day < geometry.RangeEndUtc)
            {
                yield return day;
                if (DateTime.MaxValue.Ticks - day.Ticks < TimeSpan.TicksPerDay) yield break;
                day = day.AddDays(1);
            }
        }

        private static DateTime NextMonth(DateTime monthStart)
        {
            return monthStart.Year >= DateTime.MaxValue.Year && monthStart.Month == 12 ? DateTime.MaxValue : monthStart.AddMonths(1);
        }

        private static string MonthLabel(DateTime utc)
        {
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineRulerMonthFormat, utc.Month, utc.Year);
        }

        private static string DayMonthText(DateTime utc)
        {
            return utc.Day.ToString(CultureInfo.InvariantCulture) + "/" + utc.Month.ToString(CultureInfo.InvariantCulture);
        }
    }
}
