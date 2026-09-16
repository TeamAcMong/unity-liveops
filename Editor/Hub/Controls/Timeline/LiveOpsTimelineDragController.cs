using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Máy trạng thái một cử chỉ kéo trên timeline [SD1 §3.7]: kéo thân, kéo mép đầu/cuối, ⌘/Ctrl + kéo chỗ trống để tạo (V-10).
    /// Không đụng VisualElement: nhận toạ độ track + hình học + model làn, phát intent Preview → Commit | Cancel, nên test đọc được
    /// từng bước mà không cần panel, và element chỉ lo chuyển sự kiện chuột thành lời gọi.
    ///
    /// Xem trước "sẽ chồng" (Hình 12 khung 7) tính TRƯỚC khi thả từ chính thanh cố định của làn trong model — luật game giữ đợt bắt
    /// đầu sớm hơn — không gọi kiểm nhanh của phiên (<c>CheckLane</c> là việc của presenter G-CALENDAR, W4). Một cử chỉ phát đúng
    /// một Commit hoặc một Cancel: presenter gom mọi Preview vào một nhóm Undo, Esc không để lại bước Undo (M-14).
    /// </summary>
    internal sealed class LiveOpsTimelineDragController
    {
        /// <summary>Dưới 4px coi là bấm (chọn), không phải kéo — chuột run tay không được dời đợt.</summary>
        internal const float DragThreshold = 4f;

        internal enum DragGesture
        {
            None = 0,
            MoveBody = 1,
            ResizeStart = 2,
            ResizeEnd = 3,
            Create = 4,
        }

        private static readonly TimeSpan UnsnappedMinimumLength = TimeSpan.FromMinutes(1);

        private readonly List<string> _willDropBarKeys = new List<string>();
        private readonly List<(DateTime startUtc, DateTime endUtc)> _previewOverlaps = new List<(DateTime startUtc, DateTime endUtc)>();

        private LiveOpsTimelineLaneModel _lane;
        private LiveOpsTimelineGeometry _geometry;
        private float _pointerStartTrackPosition;
        private DateTime _createAnchorUtc;
        private bool _hasRaisedPreview;

        public DragGesture Gesture { get; private set; }
        public bool IsActive => Gesture != DragGesture.None;

        /// <summary>Đã vượt ngưỡng 4px — từ đây mới có Preview; thả trước ngưỡng là một cú bấm.</summary>
        public bool IsDragging { get; private set; }

        public string BarKey { get; private set; } = string.Empty;
        public string LaneTypeId { get; private set; } = string.Empty;
        public DateTime OriginalStartUtc { get; private set; }
        public DateTime OriginalEndUtc { get; private set; }
        public DateTime PreviewStartUtc { get; private set; }
        public DateTime PreviewEndUtc { get; private set; }

        /// <summary>Thanh "sẽ bị bỏ" nếu thả ngay lúc này (có thể gồm chính thanh đang kéo).</summary>
        public IReadOnlyList<string> WillDropBarKeys => _willDropBarKeys;

        /// <summary>Vùng chồng giờ của làn nếu thả ngay lúc này (đã gộp).</summary>
        public IReadOnlyList<(DateTime startUtc, DateTime endUtc)> PreviewOverlaps => _previewOverlaps;

        /// <summary>Id đợt chồng lâu nhất với khoảng đang xem trước; "" khi không chồng.</summary>
        public string OverlapWithEventId { get; private set; } = string.Empty;

        public TimeSpan OverlapDuration { get; private set; }

        internal event Action<LiveOpsTimelineIntent> IntentRaised;

        /// <summary>Bắt đầu kéo một thanh; false (không làm gì) khi thanh chỉ đọc: dải, đợt sinh từ luật, đã khép, mép đầu đợt đang chạy.</summary>
        public bool BeginBar(LiveOpsTimelineLaneModel lane, LiveOpsTimelineGeometry geometry, LiveOpsTimelineBarModel bar,
            LiveOpsTimelineBarRegion region, float trackPosition)
        {
            if (lane == null) throw new ArgumentNullException(nameof(lane));
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));
            if (bar == null) throw new ArgumentNullException(nameof(bar));
            if (bar.Source != LiveOpsTimelineBarSource.Fixed || bar.IsEnded) return false;
            DragGesture gesture;
            switch (region)
            {
                case LiveOpsTimelineBarRegion.StartEdge:
                    gesture = DragGesture.ResizeStart;
                    break;
                case LiveOpsTimelineBarRegion.EndEdge:
                    gesture = DragGesture.ResizeEnd;
                    break;
                default:
                    gesture = DragGesture.MoveBody;
                    break;
            }
            // Đợt đang chạy: mép đầu khoá, dời cả thân cũng đổi giờ bắt đầu nên cũng khoá — chỉ còn kéo mép cuối.
            if (bar.IsRunning && gesture != DragGesture.ResizeEnd) return false;

            Reset();
            _lane = lane;
            _geometry = geometry;
            Gesture = gesture;
            BarKey = bar.BarKey;
            LaneTypeId = lane.TypeId;
            OriginalStartUtc = bar.StartUtc;
            OriginalEndUtc = bar.EndUtc;
            PreviewStartUtc = bar.StartUtc;
            PreviewEndUtc = bar.EndUtc;
            _pointerStartTrackPosition = trackPosition;
            return true;
        }

        /// <summary>⌘/Ctrl + kéo chỗ trống (V-10): chỉ làn cố định — làn lặp tắt tạo đợt ("Làn lặp — sửa ở Luật lặp").</summary>
        public bool BeginCreate(LiveOpsTimelineLaneModel lane, LiveOpsTimelineGeometry geometry, float trackPosition)
        {
            if (lane == null) throw new ArgumentNullException(nameof(lane));
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));
            if (lane.IsRecurring) return false;
            Reset();
            _lane = lane;
            _geometry = geometry;
            Gesture = DragGesture.Create;
            LaneTypeId = lane.TypeId;
            _pointerStartTrackPosition = trackPosition;
            _createAnchorUtc = LiveOpsTimelineGeometry.Snap(geometry.TimeAt(trackPosition), LiveOpsTimelineGeometry.AutoSnapStep(geometry.PixelsPerHour));
            OriginalStartUtc = _createAnchorUtc;
            OriginalEndUtc = _createAnchorUtc;
            PreviewStartUtc = _createAnchorUtc;
            PreviewEndUtc = _createAnchorUtc;
            return true;
        }

        /// <summary>Chuột di chuyển; Alt giữ = bỏ bắt lưới. Trả true khi vừa phát một Preview (vượt ngưỡng và giờ xem trước đổi).</summary>
        public bool Move(float trackPosition, bool disableSnap)
        {
            if (!IsActive) return false;
            if (!IsDragging)
            {
                if (Math.Abs(trackPosition - _pointerStartTrackPosition) < DragThreshold) return false;
                IsDragging = true;
            }

            TimeSpan step = LiveOpsTimelineGeometry.AutoSnapStep(_geometry.PixelsPerHour);
            TimeSpan minimumLength = disableSnap ? UnsnappedMinimumLength : step;
            long deltaTicks = (long)Math.Round((trackPosition - _pointerStartTrackPosition) / _geometry.PixelsPerHour * TimeSpan.TicksPerHour);
            DateTime start = OriginalStartUtc;
            DateTime end = OriginalEndUtc;
            switch (Gesture)
            {
                case DragGesture.MoveBody:
                    start = SnapUnless(LiveOpsTimelineGeometry.AddTicksClamped(OriginalStartUtc, deltaTicks), step, disableSnap);
                    end = LiveOpsTimelineGeometry.AddTicksClamped(start, (OriginalEndUtc - OriginalStartUtc).Ticks);
                    break;
                case DragGesture.ResizeStart:
                    start = SnapUnless(LiveOpsTimelineGeometry.AddTicksClamped(OriginalStartUtc, deltaTicks), step, disableSnap);
                    if (start > end - minimumLength) start = LiveOpsTimelineGeometry.AddTicksClamped(end, -minimumLength.Ticks);
                    break;
                case DragGesture.ResizeEnd:
                    end = SnapUnless(LiveOpsTimelineGeometry.AddTicksClamped(OriginalEndUtc, deltaTicks), step, disableSnap);
                    if (end < start + minimumLength) end = LiveOpsTimelineGeometry.AddTicksClamped(start, minimumLength.Ticks);
                    break;
                case DragGesture.Create:
                    DateTime current = SnapUnless(_geometry.TimeAt(trackPosition), step, disableSnap);
                    start = current < _createAnchorUtc ? current : _createAnchorUtc;
                    end = current < _createAnchorUtc ? _createAnchorUtc : current;
                    if (end < start + minimumLength) end = LiveOpsTimelineGeometry.AddTicksClamped(start, minimumLength.Ticks);
                    break;
            }

            bool changed = start != PreviewStartUtc || end != PreviewEndUtc || !_hasRaisedPreview;
            PreviewStartUtc = start;
            PreviewEndUtc = end;
            ComputeOverlapPreview();
            if (!changed) return false;
            _hasRaisedPreview = true;
            Raise(LiveOpsTimelineGesturePhase.Preview);
            return true;
        }

        /// <summary>Thả chuột. Trả true khi đã phát Commit; false khi chưa vượt ngưỡng (nơi gọi xử lý như một cú bấm).</summary>
        public bool Commit()
        {
            if (!IsActive) return false;
            bool wasDragging = IsDragging && _hasRaisedPreview;
            if (wasDragging) Raise(LiveOpsTimelineGesturePhase.Commit);
            Reset();
            return wasDragging;
        }

        /// <summary>Esc hoặc mất capture: phát Cancel khi đã có Preview (presenter trả tài liệu về trước cử chỉ, không tạo bước Undo).</summary>
        public bool Cancel()
        {
            if (!IsActive) return false;
            bool wasDragging = IsDragging && _hasRaisedPreview;
            if (wasDragging) Raise(LiveOpsTimelineGesturePhase.Cancel);
            Reset();
            return wasDragging;
        }

        /// <summary>Mép readout bám theo [SD1 §3.7]: kéo thân và mép đầu bám mép đầu; mép cuối bám mép cuối; tạo bám phía con trỏ.</summary>
        public DateTime AnchorUtc => Gesture == DragGesture.ResizeEnd || (Gesture == DragGesture.Create && PreviewEndUtc > _createAnchorUtc)
            ? PreviewEndUtc
            : PreviewStartUtc;

        /// <summary>
        /// Vế chính của readout: kéo thân "17/9 00:00 → 18/9 12:00 UTC · dời +12 giờ · dài 36 giờ"; kéo mép "Kết thúc 20/9 00:00 UTC ·
        /// 07:00 giờ máy · dời +1 ngày · dài 3 ngày". Khi sẽ chồng thì chỉ giữ phần giờ — vế chồng giờ (<see cref="ReadoutOverlapText"/>)
        /// thay chỗ phần còn lại (Hình 12 khung 7).
        /// </summary>
        public string ReadoutMainText(LiveOpsHubFormat format)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (!IsActive) return string.Empty;
            bool overlapping = OverlapWithEventId.Length > 0;
            string separator = LiveOpsHubStrings.TimelineReadoutSeparator;
            string length = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineReadoutLengthFormat,
                LengthText(PreviewEndUtc - PreviewStartUtc));
            switch (Gesture)
            {
                case DragGesture.ResizeStart:
                case DragGesture.ResizeEnd:
                {
                    bool isEnd = Gesture == DragGesture.ResizeEnd;
                    DateTime edge = isEnd ? PreviewEndUtc : PreviewStartUtc;
                    DateTime originalEdge = isEnd ? OriginalEndUtc : OriginalStartUtc;
                    string edgeText = string.Format(CultureInfo.InvariantCulture,
                        isEnd ? LiveOpsHubStrings.TimelineReadoutEndEdgeFormat : LiveOpsHubStrings.TimelineReadoutStartEdgeFormat, format.ShortDateTime(edge));
                    if (overlapping) return edgeText;
                    string device = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineReadoutDeviceFormat, format.DeviceClock(edge));
                    return edgeText + separator + device + separator + ShiftText(edge - originalEdge, format) + separator + length;
                }
                case DragGesture.Create:
                {
                    string range = RangeText(format);
                    return overlapping ? range : range + separator + length;
                }
                default:
                {
                    string range = RangeText(format);
                    return overlapping ? range : range + separator + ShiftText(PreviewStartUtc - OriginalStartUtc, format) + separator + length;
                }
            }
        }

        /// <summary>"chồng 12 giờ với hunt-0916-bonus" — chữ blocked; "" khi không chồng.</summary>
        public string ReadoutOverlapText(LiveOpsHubFormat format)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (OverlapWithEventId.Length == 0) return string.Empty;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineReadoutOverlapFormat, LengthText(OverlapDuration),
                OverlapWithEventId);
        }

        private string RangeText(LiveOpsHubFormat format)
        {
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineReadoutRangeFormat, format.ShortDateTime(PreviewStartUtc),
                format.ShortDateTime(PreviewEndUtc));
        }

        private static string ShiftText(TimeSpan shift, LiveOpsHubFormat format)
        {
            string sign = shift < TimeSpan.Zero ? LiveOpsHubStrings.TimelineNegativeSign : LiveOpsHubStrings.TimelinePositiveSign;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineReadoutShiftFormat, sign, LengthText(shift));
        }

        /// <summary>
        /// Thời lượng của readout và của nhãn vùng chồng [SD1 §3.7, Hình 12 khung 5–7]. Tròn ngày thì đo bằng NGÀY ("dời +1 ngày",
        /// "dài 3 ngày" — khung 6); còn lại đo bằng GIỜ và KHÔNG cuộn lên ngày ("dài 36 giờ" — khung 5, "chồng 12 giờ" — khung 7).
        /// Khác <see cref="LiveOpsHubFormat.Duration"/> (bản chung của hub cho "3 ngày 18 giờ") vì người đang kéo so độ dài mới với
        /// độ dài cũ — "1 ngày 12 giờ" bắt họ tự cộng lại, còn "3 ngày" thì đọc ngay ra chu kỳ. Có phút lẻ (bắt lưới 15 phút ở zoom
        /// gần) thì ghi thêm phút; dưới một phút ghi "0 phút" như bản chung. Âm thì in trị tuyệt đối — dấu do nơi gọi đặt.
        /// </summary>
        internal static string LengthText(TimeSpan duration)
        {
            TimeSpan absolute = duration < TimeSpan.Zero ? duration.Negate() : duration;
            // Đúng N ngày chẵn (không dư giờ/phút/giây) mới cuộn lên "ngày" — mọi giá trị lẻ giữ nguyên đơn vị giờ để so được với nhau.
            if (absolute.Ticks >= TimeSpan.TicksPerDay && absolute.Ticks % TimeSpan.TicksPerDay == 0)
            {
                long days = absolute.Ticks / TimeSpan.TicksPerDay;
                return days.ToString(CultureInfo.InvariantCulture) + " " + LiveOpsHubStrings.DurationDayUnit;
            }
            int hours = (int)Math.Floor(absolute.TotalHours);
            int minutes = absolute.Minutes;
            if (hours == 0 && minutes == 0)
            {
                return "0" + " " + LiveOpsHubStrings.DurationMinuteUnit;
            }
            string text = hours > 0 ? hours.ToString(CultureInfo.InvariantCulture) + " " + LiveOpsHubStrings.DurationHourUnit : string.Empty;
            if (minutes == 0) return text;
            string minutesText = minutes.ToString(CultureInfo.InvariantCulture) + " " + LiveOpsHubStrings.DurationMinuteUnit;
            return text.Length == 0 ? minutesText : text + " " + minutesText;
        }

        /// <summary>
        /// Chồng giờ nếu thả ngay: khoảng xem trước so với từng thanh cố định khác của làn (dải không tính — chỉ đọc). Game giữ đợt bắt
        /// đầu sớm hơn nên bên bắt đầu muộn hơn "sẽ bị bỏ"; trùng giờ bắt đầu thì đợt đang kéo chịu thiệt (thứ tự trong tài liệu không
        /// đổi khi kéo, còn đợt đang có chỗ thì giữ chỗ).
        /// </summary>
        private void ComputeOverlapPreview()
        {
            _willDropBarKeys.Clear();
            _previewOverlaps.Clear();
            OverlapWithEventId = string.Empty;
            OverlapDuration = TimeSpan.Zero;

            var intervals = new List<(DateTime startUtc, DateTime endUtc)>();
            bool candidateDrops = false;
            IReadOnlyList<LiveOpsTimelineBarModel> bars = _lane.Bars;
            for (int index = 0; index < bars.Count; index++)
            {
                LiveOpsTimelineBarModel other = bars[index];
                if (other.Source != LiveOpsTimelineBarSource.Fixed) continue;
                if (string.Equals(other.BarKey, BarKey, StringComparison.Ordinal)) continue;
                if (other.EndUtc > other.StartUtc) intervals.Add((other.StartUtc, other.EndUtc));

                DateTime overlapStart = other.StartUtc > PreviewStartUtc ? other.StartUtc : PreviewStartUtc;
                DateTime overlapEnd = other.EndUtc < PreviewEndUtc ? other.EndUtc : PreviewEndUtc;
                if (overlapStart >= overlapEnd) continue;
                if (overlapEnd - overlapStart > OverlapDuration)
                {
                    OverlapDuration = overlapEnd - overlapStart;
                    OverlapWithEventId = other.EventId;
                }
                if (PreviewStartUtc < other.StartUtc) _willDropBarKeys.Add(other.BarKey);
                else candidateDrops = true;
            }
            if (candidateDrops && BarKey.Length > 0) _willDropBarKeys.Add(BarKey);

            intervals.Add((PreviewStartUtc, PreviewEndUtc));
            var overlaps = new List<(DateTime startUtc, DateTime endUtc)>();
            for (int first = 0; first < intervals.Count; first++)
            {
                for (int second = first + 1; second < intervals.Count; second++)
                {
                    DateTime overlapStart = intervals[first].startUtc > intervals[second].startUtc ? intervals[first].startUtc : intervals[second].startUtc;
                    DateTime overlapEnd = intervals[first].endUtc < intervals[second].endUtc ? intervals[first].endUtc : intervals[second].endUtc;
                    if (overlapStart < overlapEnd) overlaps.Add((overlapStart, overlapEnd));
                }
            }
            overlaps.Sort((left, right) => left.startUtc.CompareTo(right.startUtc));
            for (int index = 0; index < overlaps.Count; index++)
            {
                if (_previewOverlaps.Count > 0 && overlaps[index].startUtc <= _previewOverlaps[_previewOverlaps.Count - 1].endUtc)
                {
                    (DateTime startUtc, DateTime endUtc) last = _previewOverlaps[_previewOverlaps.Count - 1];
                    _previewOverlaps[_previewOverlaps.Count - 1] = (last.startUtc, overlaps[index].endUtc > last.endUtc ? overlaps[index].endUtc : last.endUtc);
                }
                else
                {
                    _previewOverlaps.Add(overlaps[index]);
                }
            }
        }

        private void Raise(LiveOpsTimelineGesturePhase phase)
        {
            LiveOpsTimelineIntent intent = Gesture == DragGesture.Create
                ? (LiveOpsTimelineIntent)new CreateByDragIntent(LaneTypeId, PreviewStartUtc, PreviewEndUtc, phase)
                : new MoveBarIntent(BarKey, PreviewStartUtc, PreviewEndUtc, phase);
            IntentRaised?.Invoke(intent);
        }

        private static DateTime SnapUnless(DateTime utc, TimeSpan step, bool disableSnap)
        {
            return disableSnap ? utc : LiveOpsTimelineGeometry.Snap(utc, step);
        }

        private void Reset()
        {
            Gesture = DragGesture.None;
            IsDragging = false;
            _hasRaisedPreview = false;
            BarKey = string.Empty;
            LaneTypeId = string.Empty;
            _willDropBarKeys.Clear();
            _previewOverlaps.Clear();
            OverlapWithEventId = string.Empty;
            OverlapDuration = TimeSpan.Zero;
            _lane = null;
            _geometry = null;
        }
    }
}
