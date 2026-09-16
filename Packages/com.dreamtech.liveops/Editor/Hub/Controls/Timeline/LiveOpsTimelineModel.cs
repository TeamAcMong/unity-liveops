using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Preset zoom của timeline. Khai ở model (không ở control W3) để hình học/model thuần không phụ thuộc control; UXML nhận số.</summary>
    internal enum LiveOpsTimelineZoom
    {
        Day = 0,
        ThreeWeeks = 1,
        Month = 2,
    }

    /// <summary>
    /// Nguồn của một thanh. <see cref="FixedStrip"/> (thêm ở G-TIMELINE-MODEL, ngoài ba giá trị mục 3 — đề xuất ghi ở
    /// w2/contract-changes-G-TIMELINE-MODEL.md CC-TLMODEL-1) chỉ xuất hiện khi một làn cố định dày tới mức vượt ngân sách vertex (vd đợt
    /// hằng ngày cả năm ở 0,25 px/giờ): gom thành dải chỉ đọc thay vì để làn biến mất ở 2022.3 (R-10). BarKey của dải không phải
    /// EntryKey nên presenter không được tra nó như đợt cố định; zoom vào là tách lại từng thanh kéo được.
    /// </summary>
    internal enum LiveOpsTimelineBarSource
    {
        Fixed = 0,
        Recurring = 1,
        RecurringStrip = 2,
        FixedStrip = 3,
    }

    internal sealed class LiveOpsTimelineBarModel
    {
        public LiveOpsTimelineBarModel(string barKey, string eventId, string eventType, LiveOpsTimelineBarSource source, DateTime startUtc,
            DateTime endUtc, int rowIndex, int stripCount, LiveEventPhase phase, bool isRunning, bool isEnded, bool isDropped,
            bool isChangedSincePublished, string renamedFromId, HealthState worstFinding, double stripActiveHours = 0)
        {
            StripActiveHours = stripActiveHours;
            BarKey = barKey ?? string.Empty;
            EventId = eventId ?? string.Empty;
            EventType = eventType ?? string.Empty;
            Source = source;
            StartUtc = startUtc;
            EndUtc = endUtc;
            RowIndex = rowIndex;
            StripCount = stripCount;
            Phase = phase;
            IsRunning = isRunning;
            IsEnded = isEnded;
            IsDropped = isDropped;
            IsChangedSincePublished = isChangedSincePublished;
            RenamedFromId = renamedFromId ?? string.Empty;
            WorstFinding = worstFinding;
        }

        /// <summary>fixed: EntryKey; recurring: type + "#" + chỉ số lần lặp; dải lặp: type + "#strip#" + chỉ số đầu; dải cố định: type + "#fixed-strip#" + EntryKey đầu.</summary>
        public string BarKey { get; }

        /// <summary>Dải: "sky-race-252…258".</summary>
        public string EventId { get; }

        public string EventType { get; }
        public LiveOpsTimelineBarSource Source { get; }
        public DateTime StartUtc { get; }
        public DateTime EndUtc { get; }
        public int RowIndex { get; }

        /// <summary>Số đợt trong thanh — 1 với thanh đơn.</summary>
        public int StripCount { get; }

        /// <summary>Tổng số giờ các đợt trong dải thật sự chạy (không tính khe nghỉ) — 0 với thanh đơn.</summary>
        public double StripActiveHours { get; }

        /// <summary>
        /// Nhịp chạy của dải, làm tròn tới giờ: "sky-race · 42 đợt · <b>20 giờ/ngày</b>" [SD1 §3.8 khung 11]. Nhãn dải nói nhịp vì
        /// dải là một element thay 42 thanh — không có nó người dùng không biết dải đặc hay thưa. 0 khi dải dài 0 (không chia được).
        /// </summary>
        public int StripHoursPerDay
        {
            get
            {
                double spanDays = (EndUtc - StartUtc).TotalDays;
                if (spanDays <= 0d) return 0;
                return (int)Math.Round(StripActiveHours / spanDays, MidpointRounding.AwayFromZero);
            }
        }

        public LiveEventPhase Phase { get; }
        public bool IsRunning { get; }
        public bool IsEnded { get; }
        public bool IsDropped { get; }
        public bool IsChangedSincePublished { get; }

        /// <summary>
        /// (V-21 D-5) Model luôn để "" — presenter G-CALENDAR (W4) điền "Khác bản đã đăng: kết thúc 19/9 → 20/9" bằng
        /// <c>LiveOpsChangeText.ChangedTooltip</c>; model không phụ thuộc nguồn câu chữ của G-FINDINGTEXT.
        /// </summary>
        public string ChangedTooltip => string.Empty;

        /// <summary>
        /// "weekly-pass-35" khi nháp đổi tiền tố làm id lần lặp đang chạy thành "pass-35"; "" với mọi thanh khác. Đợt cố định luôn "":
        /// diff với bản đã đăng (<c>LiveEventCalendarDiff.Compare</c>) ghép đợt theo id nên đổi id hiện thành Bỏ + Thêm (hoặc cặp đổi tên
        /// đợt đã khép CC-DIFF-3), không bao giờ ra field "id" — field đó chỉ có ở <c>CompareByEntryKey</c> ("chưa lưu"), không phải đầu vào ở đây.
        /// </summary>
        public string RenamedFromId { get; }

        public HealthState WorstFinding { get; }
        public bool IsStrip => Source == LiveOpsTimelineBarSource.RecurringStrip || Source == LiveOpsTimelineBarSource.FixedStrip;

        public override string ToString() => EventId + " [" + Source + "] " + StartUtc.ToString("u", CultureInfo.InvariantCulture) + " → " +
                                             EndUtc.ToString("u", CultureInfo.InvariantCulture) + " row " + RowIndex;
    }

    internal sealed class LiveOpsTimelineLaneModel
    {
        public LiveOpsTimelineLaneModel(string typeId, int colorSlot, bool isRecurring, int rowCount, IReadOnlyList<LiveOpsTimelineBarModel> bars,
            IReadOnlyList<(DateTime startUtc, DateTime endUtc)> overlapRanges, int unplaceableCount, LiveOpsTimelineBarModel nextOutsideRange,
            string metaText, string secondaryMetaText, HealthState chipState, int chipCount)
        {
            TypeId = typeId ?? string.Empty;
            ColorSlot = colorSlot;
            IsRecurring = isRecurring;
            RowCount = rowCount;
            Bars = bars ?? Array.Empty<LiveOpsTimelineBarModel>();
            OverlapRanges = overlapRanges ?? Array.Empty<(DateTime startUtc, DateTime endUtc)>();
            UnplaceableCount = unplaceableCount;
            NextOutsideRange = nextOutsideRange;
            MetaText = metaText ?? string.Empty;
            SecondaryMetaText = secondaryMetaText ?? string.Empty;
            ChipState = chipState;
            ChipCount = chipCount;
        }

        public string TypeId { get; }
        public int ColorSlot { get; }
        public bool IsRecurring { get; }

        /// <summary>Số hàng phụ ≥ 1; chiều cao làn = padTop + RowCount × 28 + 4 [SD1 §3.2].</summary>
        public int RowCount { get; }

        public IReadOnlyList<LiveOpsTimelineBarModel> Bars { get; }

        /// <summary>Vùng chồng giờ giữa các đợt cố định cùng loại trong khoảng (đã gộp) — có thì padTop = 16 và meta2 nêu luật giữ đợt sớm hơn.</summary>
        public IReadOnlyList<(DateTime startUtc, DateTime endUtc)> OverlapRanges { get; }

        /// <summary>Chip "Không đặt được (n)": đợt có giờ bắt đầu hoặc kết thúc không đọc được — không bao giờ biến mất lặng lẽ.</summary>
        public int UnplaceableCount { get; }

        /// <summary>Chip "Đợt tới: …" — chỉ khi làn không có thanh nào trong khoảng; <c>null</c> khi không còn đợt cố định nào phía sau.</summary>
        public LiveOpsTimelineBarModel NextOutsideRange { get; }

        public string MetaText { get; }
        public string SecondaryMetaText { get; }

        /// <summary>
        /// Chip dòng tên: nặng nhất trong phát hiện Bị bỏ/Mất tiến độ của làn (Nên xem không lên chip; đợt không đặt được đã có chip
        /// riêng nên không đếm đôi). Mẫu Hình 1: weekly-pass ◆ 1, treasure-hunt ● 1, lava-quest không chip.
        /// </summary>
        public HealthState ChipState { get; }

        /// <summary>Số phát hiện đúng ở mức <see cref="ChipState"/>; 0 khi không chip.</summary>
        public int ChipCount { get; }
    }

    /// <summary>Một dải 3px của minimap — mỗi làn đang hiện một dải, theo thứ tự làn.</summary>
    internal sealed class LiveOpsTimelineMinimapBand
    {
        public LiveOpsTimelineMinimapBand(string typeId, int colorSlot, int rowIndex, IReadOnlyList<(DateTime startUtc, DateTime endUtc)> segments)
        {
            TypeId = typeId ?? string.Empty;
            ColorSlot = colorSlot;
            RowIndex = rowIndex;
            Segments = segments ?? Array.Empty<(DateTime startUtc, DateTime endUtc)>();
        }

        public string TypeId { get; }
        public int ColorSlot { get; }

        /// <summary>top = 2 + RowIndex × 3 [SD1 §3.6].</summary>
        public int RowIndex { get; }

        /// <summary>Đoạn đã gom khe dưới 1px ở tỉ lệ minimap.</summary>
        public IReadOnlyList<(DateTime startUtc, DateTime endUtc)> Segments { get; }
    }

    /// <summary>
    /// Vạch trên minimap: Blocked = đỏ cao hết dải (đợt bị bỏ), Warning = vàng nửa trên (mất tiến độ, nên xem). Tooltip
    /// ("Bị bỏ · hunt-0916-bonus") do view dựng từ <see cref="Finding"/> qua <c>LiveOpsFindingText</c> — model chỉ giữ dữ liệu.
    /// </summary>
    internal sealed class LiveOpsTimelineMinimapMark
    {
        public LiveOpsTimelineMinimapMark(DateTime atUtc, HealthState state, string targetId, string targetEntryKey, LiveEventCalendarFinding finding)
        {
            AtUtc = atUtc;
            State = state;
            TargetId = targetId ?? string.Empty;
            TargetEntryKey = targetEntryKey ?? string.Empty;
            Finding = finding;
        }

        public DateTime AtUtc { get; }
        public HealthState State { get; }
        public string TargetId { get; }
        public string TargetEntryKey { get; }

        /// <summary><c>null</c> khi vạch lấy từ bản biên dịch (chưa có báo cáo kiểm) — vẫn có vạch đỏ vì game vẫn bỏ đợt đó.</summary>
        public LiveEventCalendarFinding Finding { get; }
    }

    internal sealed class LiveOpsTimelineMinimapModel
    {
        public LiveOpsTimelineMinimapModel(DateTime rangeStartUtc, DateTime rangeEndUtc, float width, DateTime nowUtc, DateTime? publishedAtUtc,
            DateTime viewportStartUtc, DateTime viewportEndUtc, IReadOnlyList<LiveOpsTimelineMinimapBand> bands,
            IReadOnlyList<LiveOpsTimelineMinimapMark> marks)
        {
            RangeStartUtc = rangeStartUtc;
            RangeEndUtc = rangeEndUtc;
            Width = width;
            NowUtc = nowUtc;
            PublishedAtUtc = publishedAtUtc;
            ViewportStartUtc = viewportStartUtc;
            ViewportEndUtc = viewportEndUtc;
            Bands = bands ?? Array.Empty<LiveOpsTimelineMinimapBand>();
            Marks = marks ?? Array.Empty<LiveOpsTimelineMinimapMark>();
        }

        /// <summary>30 ngày trước bây giờ → 7 ngày sau đợt cố định cuối (mẫu 14/8 → 13/10), nới ra để luôn chứa khung đang xem.</summary>
        public DateTime RangeStartUtc { get; }

        public DateTime RangeEndUtc { get; }
        public float Width { get; }
        public DateTime NowUtc { get; }
        public DateTime? PublishedAtUtc { get; }
        public DateTime ViewportStartUtc { get; }
        public DateTime ViewportEndUtc { get; }
        public IReadOnlyList<LiveOpsTimelineMinimapBand> Bands { get; }

        /// <summary>Tối đa <see cref="LiveOpsTimelineModel.MaximumMinimapMarks"/> vạch, sắp theo giờ.</summary>
        public IReadOnlyList<LiveOpsTimelineMinimapMark> Marks { get; }

        public double PixelsPerHour => Width / (RangeEndUtc - RangeStartUtc).TotalHours;

        public float XOf(DateTime utc)
        {
            return (float)((utc - RangeStartUtc).TotalHours * PixelsPerHour);
        }
    }

    /// <summary>
    /// Model thuần của timeline Lịch: làn theo thứ tự loại, thanh (cố định, lần lặp, dải gom), hàng phụ, vùng chồng giờ, chip "Không đặt
    /// được"/"Đợt tới", meta header, chip trạng thái, minimap. Dựng từ dữ liệu phiên trong một lần gọi — control W3 chỉ vẽ lại, nên
    /// mọi luật hiển thị có test Logic ở đây. Luôn tôn trọng ngân sách vertex (R-10): làn nào ước lượng vượt
    /// <see cref="LiveOpsTimelineVertexBudget.LaneSafetyBudget"/> thì gom dải ở khe lớn dần tới khi vừa.
    /// </summary>
    internal sealed class LiveOpsTimelineModel
    {
        /// <summary>[SD1 §3.14]: tối đa 50 vạch trên minimap — ưu tiên Bị bỏ trước, rồi theo giờ.</summary>
        public const int MaximumMinimapMarks = 50;

        public const int MinimapDaysBeforeNow = 30;
        public const int MinimapDaysAfterLastFixedEvent = 7;
        public const float MinimapSegmentGapThreshold = 1f;

        private const string RecurringKeySeparator = "#";
        private const string RecurringStripKeyInfix = "#strip#";
        private const string FixedStripKeyInfix = "#fixed-strip#";
        private const int HoursPerDay = 24;

        private LiveOpsTimelineModel(DateTime nowUtc, DateTime rangeStartUtc, DateTime rangeEndUtc, DateTime? publishedAtUtc, string publishedShortSha,
            IReadOnlyList<LiveOpsTimelineLaneModel> lanes, LiveOpsTimelineMinimapModel minimap, LiveOpsTimelineGeometry geometry)
        {
            NowUtc = nowUtc;
            RangeStartUtc = rangeStartUtc;
            RangeEndUtc = rangeEndUtc;
            PublishedAtUtc = publishedAtUtc;
            PublishedShortSha = publishedShortSha ?? string.Empty;
            Lanes = lanes;
            Minimap = minimap;
            Geometry = geometry;
        }

        public DateTime NowUtc { get; }
        public DateTime RangeStartUtc { get; }
        public DateTime RangeEndUtc { get; }
        public DateTime? PublishedAtUtc { get; }
        public string PublishedShortSha { get; }
        public IReadOnlyList<LiveOpsTimelineLaneModel> Lanes { get; }
        public LiveOpsTimelineMinimapModel Minimap { get; }

        /// <summary>Hình học đã dùng để gom dải — view vẽ bằng đúng đối tượng này để toạ độ và quyết định gom không lệch nhau.</summary>
        public LiveOpsTimelineGeometry Geometry { get; }

        public LiveOpsTimelineLaneModel FindLane(string typeId)
        {
            for (int index = 0; index < Lanes.Count; index++)
            {
                if (string.Equals(Lanes[index].TypeId, typeId, StringComparison.Ordinal)) return Lanes[index];
            }
            return null;
        }

        public static LiveOpsTimelineModel Build(LiveOpsTimelineInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Document == null) throw new ArgumentException(LiveOpsHubStrings.TimelineDocumentRequired, nameof(input));

            DateTime nowUtc = input.NowUtc;
            DateTime rangeStartUtc = input.HasRange ? input.RangeStartUtc : LiveOpsTimelineGeometry.RangeStartFor(nowUtc, LiveOpsTimelineZoom.ThreeWeeks);
            DateTime rangeEndUtc = input.HasRange
                ? input.RangeEndUtc
                : LiveOpsTimelineGeometry.AddTicksClamped(rangeStartUtc, LiveOpsTimelineGeometry.RangeLengthOf(LiveOpsTimelineZoom.ThreeWeeks).Ticks);
            var geometry = new LiveOpsTimelineGeometry(rangeStartUtc, rangeEndUtc, input.TrackWidth);

            var context = new BuildContext(input, geometry);

            var lanes = new List<LiveOpsTimelineLaneModel>();
            List<string> laneTypes = context.LaneTypeOrder();
            for (int index = 0; index < laneTypes.Count; index++)
            {
                if (input.IsLaneHidden(laneTypes[index])) continue;
                lanes.Add(BuildLane(context, laneTypes[index]));
            }

            DateTime? publishedAtUtc = null;
            string publishedShortSha = string.Empty;
            PublishedCalendarStamp stamp = input.Document.LatestStamp;
            if (stamp != null)
            {
                if (stamp.TryGetPublishedUtc(out DateTime publishedUtc)) publishedAtUtc = publishedUtc;
                publishedShortSha = stamp.ShortSha;
            }

            LiveOpsTimelineMinimapModel minimap = BuildMinimap(context, lanes, publishedAtUtc);
            return new LiveOpsTimelineModel(nowUtc, rangeStartUtc, rangeEndUtc, publishedAtUtc, publishedShortSha, lanes, minimap, geometry);
        }

        // ----- Làn -----

        private static LiveOpsTimelineLaneModel BuildLane(BuildContext context, string typeId)
        {
            LiveEventCalendarDocument document = context.Input.Document;
            LiveOpsTimelineGeometry geometry = context.Geometry;
            bool isRecurring = document.TryGetRecurringRule(typeId, out RecurringLiveEventRule rule);
            int colorSlot = document.TryGetEventType(typeId, out LiveEventTypeDefinition definition)
                ? definition.ColorSlot
                : LiveEventTypeColorSlots.DefaultSlotFor(typeId);

            List<RecurringLiveEventCalendar> calendars = context.RecurringCalendarsOf(typeId);
            var occurrences = new List<LiveEventInstance>();
            if (calendars.Count > 0) AppendOccurrences(calendars[0], geometry.RangeStartUtc, geometry.RangeEndUtc, occurrences);

            var fixedCandidates = new List<FixedCandidate>();
            int fixedCount = 0;
            int unplaceableCount = 0;
            FixedCandidate nextOutside = null;
            List<FixedLiveEventEntry> entries = context.FixedEntriesOf(typeId);
            for (int index = 0; index < entries.Count; index++)
            {
                FixedLiveEventEntry entry = entries[index];
                fixedCount++;
                if (!entry.TryGetStartUtc(out DateTime startUtc) || !entry.TryGetEndUtc(out DateTime endUtc))
                {
                    unplaceableCount++;
                    continue;
                }
                // Kết thúc không sau bắt đầu: vẫn vẽ thanh tối thiểu tại giờ bắt đầu (game bỏ đợt đó — thanh mang tag "bị bỏ").
                DateTime drawnEndUtc = endUtc > startUtc ? endUtc : startUtc;
                var candidate = new FixedCandidate(entry, startUtc, drawnEndUtc, index);
                bool intersects = startUtc < geometry.RangeEndUtc && (drawnEndUtc > geometry.RangeStartUtc ||
                                                                      (drawnEndUtc == startUtc && startUtc >= geometry.RangeStartUtc));
                if (intersects) fixedCandidates.Add(candidate);
                else if (startUtc >= geometry.RangeEndUtc && (nextOutside == null || startUtc < nextOutside.StartUtc)) nextOutside = candidate;
            }
            fixedCandidates.Sort(FixedCandidate.CompareByStart);

            LaneComposition composition = ComposeWithinBudget(context, typeId, occurrences, calendars.Count > 0 ? calendars[0] : null, fixedCandidates);

            bool hasBars = composition.Bars.Count > 0;
            LiveOpsTimelineBarModel nextOutsideBar = !hasBars && nextOutside != null ? FixedBar(context, typeId, nextOutside, 0) : null;

            string metaText;
            if (typeId.Length == 0)
            {
                // Làn giữ chỗ cho đợt/luật chưa ghi loại (luật 3 báo Bị bỏ): tên làn trống nên meta phải tự nói vì sao làn này tồn tại.
                metaText = unplaceableCount > 0
                    ? LiveOpsHubStrings.TimelineUntypedLaneMeta
                    : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineUntypedLaneMetaCountFormat, fixedCount);
            }
            else if (isRecurring)
            {
                metaText = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineRecurringLaneMetaFormat,
                    HoursText(rule.PeriodHours), HoursText(rule.ActiveHours));
            }
            else if (fixedCount == 0)
            {
                metaText = LiveOpsHubStrings.TimelineFixedLaneMetaEmpty;
            }
            else if (unplaceableCount > 0)
            {
                // Chip "Không đặt được (n)" nằm trong dòng meta [SD1 §3.2 bảng làn] — bỏ số đợt để chip không bị cắt.
                metaText = LiveOpsHubStrings.TimelineFixedLaneMeta;
            }
            else
            {
                string format = fixedCandidates.Count == 0
                    ? LiveOpsHubStrings.TimelineFixedLaneMetaOutsideRangeFormat
                    : LiveOpsHubStrings.TimelineFixedLaneMetaCountFormat;
                metaText = string.Format(CultureInfo.InvariantCulture, format, fixedCount);
            }
            string secondaryMetaText = composition.OverlapRanges.Count > 0 ? LiveOpsHubStrings.TimelineLaneOverlapPolicyMeta : string.Empty;

            ComputeChip(context, typeId, out HealthState chipState, out int chipCount);

            return new LiveOpsTimelineLaneModel(typeId, colorSlot, isRecurring, composition.RowCount, composition.Bars, composition.OverlapRanges,
                unplaceableCount, nextOutsideBar, metaText, secondaryMetaText, chipState, chipCount);
        }

        /// <summary>
        /// Gom dải theo từng bậc tới khi làn vừa ngân sách: (1) khe &lt; 3px giữa hai lần lặp đều hẹp hơn 24px (sky-race ở zoom Tháng — thanh
        /// không có chỗ cho nhãn; weekly-pass liền nhau nhưng rộng thì giữ từng thanh "pass-35…41"); (2) mọi lần lặp khe &lt; ngưỡng, ngưỡng
        /// nhân đôi; (3) cả đợt cố định. Bậc (2)–(3) chỉ chạy khi ước lượng vượt <see cref="LiveOpsTimelineVertexBudget.LaneSafetyBudget"/>
        /// — ngưỡng vượt bề rộng track thì mọi thanh liền kề đã gom, nên vòng luôn dừng.
        /// </summary>
        private static LaneComposition ComposeWithinBudget(BuildContext context, string typeId, List<LiveEventInstance> occurrences,
            RecurringLiveEventCalendar calendar, List<FixedCandidate> fixedCandidates)
        {
            LiveOpsTimelineGeometry geometry = context.Geometry;
            float stopThreshold = geometry.TrackWidth * 2f + LiveOpsTimelineGeometry.StripGapThreshold;

            LaneComposition composition = Compose(context, typeId, occurrences, calendar, fixedCandidates,
                LiveOpsTimelineGeometry.StripGapThreshold, true, 0f);
            if (Fits(composition, geometry)) return composition;

            // Làn không có lần lặp thì bậc (2) ra y hệt bậc (1) — nhảy thẳng sang bậc (3).
            for (float threshold = LiveOpsTimelineGeometry.StripGapThreshold; occurrences.Count > 1 && threshold <= stopThreshold; threshold *= 2f)
            {
                composition = Compose(context, typeId, occurrences, calendar, fixedCandidates, threshold, false, 0f);
                if (Fits(composition, geometry)) return composition;
            }
            for (float threshold = LiveOpsTimelineGeometry.StripGapThreshold; ; threshold *= 2f)
            {
                composition = Compose(context, typeId, occurrences, calendar, fixedCandidates, stopThreshold, false, threshold);
                if (Fits(composition, geometry) || threshold > stopThreshold) return composition;
            }
        }

        private static bool Fits(LaneComposition composition, LiveOpsTimelineGeometry geometry)
        {
            var probe = new LiveOpsTimelineLaneModel(string.Empty, 0, false, composition.RowCount, composition.Bars, composition.OverlapRanges, 0,
                null, string.Empty, string.Empty, HealthState.Ok, 0);
            return LiveOpsTimelineVertexBudget.EstimateLane(probe, geometry) <= LiveOpsTimelineVertexBudget.LaneSafetyBudget;
        }

        /// <param name="fixedStripThreshold">0 = không gom đợt cố định (mặc định — đợt cố định kéo được).</param>
        private static LaneComposition Compose(BuildContext context, string typeId, List<LiveEventInstance> occurrences,
            RecurringLiveEventCalendar calendar, List<FixedCandidate> fixedCandidates, float recurringThreshold, bool onlyNarrowRecurring,
            float fixedStripThreshold)
        {
            LiveOpsTimelineGeometry geometry = context.Geometry;
            var pending = new List<PendingBar>();

            int groupStart = 0;
            for (int index = 1; index <= occurrences.Count; index++)
            {
                bool closeGroup = index == occurrences.Count;
                if (!closeGroup)
                {
                    LiveEventInstance previous = occurrences[index - 1];
                    LiveEventInstance current = occurrences[index];
                    float gap = geometry.XOf(current.StartUtc) - geometry.XOf(previous.EndUtc);
                    bool narrow = geometry.SpanWidth(previous.StartUtc, previous.EndUtc) < LiveOpsTimelineGeometry.LabelMinimumBarWidth &&
                                  geometry.SpanWidth(current.StartUtc, current.EndUtc) < LiveOpsTimelineGeometry.LabelMinimumBarWidth;
                    closeGroup = !(gap < recurringThreshold && (!onlyNarrowRecurring || narrow));
                }
                if (!closeGroup) continue;
                if (occurrences.Count > 0) pending.Add(RecurringPending(context, typeId, calendar, occurrences, groupStart, index));
                groupStart = index;
            }

            if (fixedStripThreshold <= 0f)
            {
                for (int index = 0; index < fixedCandidates.Count; index++) pending.Add(new PendingBar(fixedCandidates[index]));
            }
            else
            {
                int fixedGroupStart = 0;
                DateTime groupEndUtc = DateTime.MinValue;
                for (int index = 0; index <= fixedCandidates.Count; index++)
                {
                    bool closeGroup = index == fixedCandidates.Count;
                    if (!closeGroup && index > fixedGroupStart)
                    {
                        float gap = geometry.XOf(fixedCandidates[index].StartUtc) - geometry.XOf(groupEndUtc);
                        closeGroup = !(gap < fixedStripThreshold);
                    }
                    if (closeGroup && index > fixedGroupStart)
                    {
                        pending.Add(FixedPending(fixedCandidates, fixedGroupStart, index));
                        fixedGroupStart = index;
                    }
                    if (index < fixedCandidates.Count)
                    {
                        if (index == fixedGroupStart || fixedCandidates[index].EndUtc > groupEndUtc) groupEndUtc = fixedCandidates[index].EndUtc;
                    }
                }
            }

            return AssignRows(context, typeId, pending);
        }

        private static LaneComposition AssignRows(BuildContext context, string typeId, List<PendingBar> pending)
        {
            // Hàng phụ: xếp theo giờ bắt đầu (lần lặp trước đợt cố định khi trùng giờ, rồi theo thứ tự trong tài liệu), mỗi thanh vào
            // hàng thấp nhất đã rảnh — mép chạm nhau không chồng (cùng luật với FixedLiveEventCalendar). Mẫu: hunt-0914 hàng 0, bonus hàng 1.
            pending.Sort(PendingBar.CompareForRows);
            var rowEnds = new List<DateTime>();
            var bars = new List<LiveOpsTimelineBarModel>(pending.Count);
            var fixedIntervals = new List<(DateTime startUtc, DateTime endUtc)>();
            for (int index = 0; index < pending.Count; index++)
            {
                PendingBar bar = pending[index];
                int row = -1;
                for (int rowIndex = 0; rowIndex < rowEnds.Count; rowIndex++)
                {
                    if (rowEnds[rowIndex] <= bar.StartUtc)
                    {
                        row = rowIndex;
                        break;
                    }
                }
                if (row < 0)
                {
                    row = rowEnds.Count;
                    rowEnds.Add(bar.EndUtc);
                }
                else
                {
                    rowEnds[row] = bar.EndUtc;
                }

                if (bar.Candidate != null) bars.Add(FixedBar(context, typeId, bar.Candidate, row));
                else if (bar.Members != null) bars.Add(bar.ToStripModel(context, row));
                else bars.Add(bar.ToModel(row));
                if (bar.Candidate != null && bar.EndUtc > bar.StartUtc) fixedIntervals.Add((bar.StartUtc, bar.EndUtc));
            }
            return new LaneComposition(bars, Math.Max(1, rowEnds.Count), OverlapsOf(fixedIntervals));
        }

        private static IReadOnlyList<(DateTime startUtc, DateTime endUtc)> OverlapsOf(List<(DateTime startUtc, DateTime endUtc)> intervals)
        {
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
            return MergeIntervals(overlaps);
        }

        private static List<(DateTime startUtc, DateTime endUtc)> MergeIntervals(List<(DateTime startUtc, DateTime endUtc)> intervals)
        {
            intervals.Sort((left, right) => left.startUtc.CompareTo(right.startUtc));
            var merged = new List<(DateTime startUtc, DateTime endUtc)>();
            for (int index = 0; index < intervals.Count; index++)
            {
                (DateTime startUtc, DateTime endUtc) interval = intervals[index];
                if (merged.Count > 0 && interval.startUtc <= merged[merged.Count - 1].endUtc)
                {
                    (DateTime startUtc, DateTime endUtc) last = merged[merged.Count - 1];
                    merged[merged.Count - 1] = (last.startUtc, interval.endUtc > last.endUtc ? interval.endUtc : last.endUtc);
                }
                else
                {
                    merged.Add(interval);
                }
            }
            return merged;
        }

        private static PendingBar RecurringPending(BuildContext context, string typeId, RecurringLiveEventCalendar calendar,
            List<LiveEventInstance> occurrences, int firstIndex, int endIndex)
        {
            LiveEventInstance first = occurrences[firstIndex];
            LiveEventInstance last = occurrences[endIndex - 1];
            DateTime nowUtc = context.Input.NowUtc;
            long firstOccurrenceIndex = calendar.OccurrenceIndexAt(first.StartUtc);
            int count = endIndex - firstIndex;

            bool isRunning = false;
            bool isChanged = false;
            string renamedFromId = string.Empty;
            HealthState worst = HealthState.Ok;
            double activeHours = 0d;
            for (int index = firstIndex; index < endIndex; index++)
            {
                LiveEventInstance occurrence = occurrences[index];
                activeHours += (occurrence.EndUtc - occurrence.StartUtc).TotalHours;
                if (occurrence.PhaseAt(nowUtc) == LiveEventPhase.Active) isRunning = true;
                string previousId = context.RenamedRunningIdOf(typeId, occurrence.EventId);
                if (previousId.Length > 0)
                {
                    isChanged = true;
                    if (count == 1) renamedFromId = previousId;
                }
                worst = Worse(worst, context.WorstRecurringFinding(typeId, occurrence.EventId, previousId));
            }

            if (count == 1)
            {
                LiveEventPhase phase = first.PhaseAt(nowUtc);
                return new PendingBar(typeId + RecurringKeySeparator + firstOccurrenceIndex.ToString(CultureInfo.InvariantCulture), first.EventId,
                    typeId, LiveOpsTimelineBarSource.Recurring, first.StartUtc, first.EndUtc, 1, phase, isRunning, phase == LiveEventPhase.Ended,
                    false, isChanged, renamedFromId, worst, 0);
            }

            long lastOccurrenceIndex = calendar.OccurrenceIndexAt(last.StartUtc);
            string stripId = first.EventId + LiveOpsHubStrings.TimelineStripIdSeparator + lastOccurrenceIndex.ToString(CultureInfo.InvariantCulture);
            LiveEventPhase stripPhase = PhaseOf(first.StartUtc, last.EndUtc, nowUtc);
            return new PendingBar(typeId + RecurringStripKeyInfix + firstOccurrenceIndex.ToString(CultureInfo.InvariantCulture), stripId, typeId,
                LiveOpsTimelineBarSource.RecurringStrip, first.StartUtc, last.EndUtc, count, stripPhase, isRunning, last.EndUtc <= nowUtc, false,
                isChanged, string.Empty, worst, 0, activeHours);
        }

        private static PendingBar FixedPending(List<FixedCandidate> candidates, int firstIndex, int endIndex)
        {
            if (endIndex - firstIndex == 1) return new PendingBar(candidates[firstIndex]);
            var members = new List<FixedCandidate>(endIndex - firstIndex);
            for (int index = firstIndex; index < endIndex; index++) members.Add(candidates[index]);
            return new PendingBar(members);
        }

        private static LiveOpsTimelineBarModel FixedBar(BuildContext context, string typeId, FixedCandidate candidate, int row)
        {
            DateTime nowUtc = context.Input.NowUtc;
            FixedLiveEventEntry entry = candidate.Entry;
            LiveEventPhase phase = PhaseOf(candidate.StartUtc, candidate.EndUtc, nowUtc);
            bool isDropped = context.IsFixedDropped(entry);
            return new LiveOpsTimelineBarModel(entry.EntryKey, entry.EventId, typeId, LiveOpsTimelineBarSource.Fixed, candidate.StartUtc,
                candidate.EndUtc, row, 1, phase, phase == LiveEventPhase.Active, phase == LiveEventPhase.Ended, isDropped,
                context.IsFixedChanged(entry.EntryKey), string.Empty, context.WorstFixedFinding(entry.EntryKey));
        }

        private static LiveEventPhase PhaseOf(DateTime startUtc, DateTime endUtc, DateTime nowUtc)
        {
            if (nowUtc < startUtc) return LiveEventPhase.Upcoming;
            return nowUtc < endUtc ? LiveEventPhase.Active : LiveEventPhase.Ended;
        }

        /// <summary>
        /// Đi thẳng theo chỉ số lần lặp thay vì <see cref="RecurringLiveEventCalendar.GetInstances"/>: truy vấn đó dừng ở
        /// <see cref="RecurringLiveEventCalendar.MaximumInstancesPerQuery"/> (512) nên luật hằng giờ ở zoom Tháng (1.008 lần) sẽ bị cắt nửa
        /// khung, và lịch ghép của bản biên dịch còn trộn đợt cố định cùng loại vào kết quả.
        /// </summary>
        internal static void AppendOccurrences(RecurringLiveEventCalendar calendar, DateTime fromUtc, DateTime toUtc, List<LiveEventInstance> result)
        {
            if (toUtc <= fromUtc) return;
            long periodTicks = calendar.Period.Ticks;
            long activeTicks = calendar.ActiveDuration.Ticks;
            long anchorTicks = calendar.AnchorUtc.Ticks;
            for (long index = calendar.OccurrenceIndexAt(fromUtc); ; index++)
            {
                if (!TryOccurrenceTicks(anchorTicks, periodTicks, activeTicks, index, out long startTicks, out long endTicks)) break;
                if (startTicks >= toUtc.Ticks) break;
                if (endTicks > fromUtc.Ticks) result.Add(calendar.GetOccurrence(index));
            }
        }

        /// <summary>
        /// Đoạn minimap của một luật lặp, tính bằng số học chỉ số — không tạo <see cref="LiveEventInstance"/> nào. Khoảng minimap kéo tới
        /// 7 ngày sau đợt cố định xa nhất, nên một năm gõ nhầm (2126) cộng luật hằng giờ là ~876 nghìn lần lặp: tạo từng instance thì một lần
        /// dựng tốn ~0,7s và ~138MB, trái luật vẽ lại trong 1 frame [SD1 §3.7]. Khe nghỉ hẹp hơn
        /// <see cref="MinimapSegmentGapThreshold"/> thì cả chuỗi vốn sẽ bị gom thành một đoạn (ngưỡng gom sau đó chỉ tăng) nên trả thẳng một
        /// đoạn đầu → cuối; còn lại mỗi chu kỳ rộng ≥ 1px nên số đoạn bị chặn bởi bề rộng minimap.
        /// </summary>
        internal static void AppendMinimapRecurringSegments(RecurringLiveEventCalendar calendar, LiveOpsTimelineGeometry minimapGeometry,
            List<(DateTime startUtc, DateTime endUtc)> result)
        {
            long periodTicks = calendar.Period.Ticks;
            long activeTicks = calendar.ActiveDuration.Ticks;
            long anchorTicks = calendar.AnchorUtc.Ticks;
            long fromTicks = minimapGeometry.RangeStartUtc.Ticks;
            long toTicks = minimapGeometry.RangeEndUtc.Ticks;

            long firstIndex = calendar.OccurrenceIndexAt(minimapGeometry.RangeStartUtc);
            if (!TryOccurrenceTicks(anchorTicks, periodTicks, activeTicks, firstIndex, out long firstStartTicks, out long firstEndTicks)) return;
            if (firstEndTicks <= fromTicks)
            {
                firstIndex++;
                if (!TryOccurrenceTicks(anchorTicks, periodTicks, activeTicks, firstIndex, out firstStartTicks, out firstEndTicks)) return;
            }
            if (firstStartTicks >= toTicks) return;

            // Lần cuối = lần bắt đầu trước cuối khoảng; lùi thêm khi kết thúc của nó vượt DateTime.MaxValue (chỉ vài bước ở mép lịch).
            long lastIndex = calendar.OccurrenceIndexAt(minimapGeometry.RangeEndUtc);
            long lastStartTicks;
            long lastEndTicks;
            while (!TryOccurrenceTicks(anchorTicks, periodTicks, activeTicks, lastIndex, out lastStartTicks, out lastEndTicks) || lastStartTicks >= toTicks)
            {
                lastIndex--;
                if (lastIndex < firstIndex) return;
            }

            double restPixels = (double)(periodTicks - activeTicks) / TimeSpan.TicksPerHour * minimapGeometry.PixelsPerHour;
            if (restPixels < MinimapSegmentGapThreshold)
            {
                result.Add((new DateTime(firstStartTicks, DateTimeKind.Utc), new DateTime(lastEndTicks, DateTimeKind.Utc)));
                return;
            }
            for (long index = firstIndex; index <= lastIndex; index++)
            {
                if (!TryOccurrenceTicks(anchorTicks, periodTicks, activeTicks, index, out long startTicks, out long endTicks)) break;
                result.Add((new DateTime(startTicks, DateTimeKind.Utc), new DateTime(endTicks, DateTimeKind.Utc)));
            }
        }

        private static bool TryOccurrenceTicks(long anchorTicks, long periodTicks, long activeTicks, long index, out long startTicks, out long endTicks)
        {
            startTicks = 0;
            endTicks = 0;
            try
            {
                checked
                {
                    startTicks = anchorTicks + index * periodTicks;
                    endTicks = startTicks + activeTicks;
                }
            }
            catch (OverflowException)
            {
                return false;
            }
            return startTicks >= DateTime.MinValue.Ticks && endTicks <= DateTime.MaxValue.Ticks;
        }

        private static void ComputeChip(BuildContext context, string typeId, out HealthState chipState, out int chipCount)
        {
            chipState = HealthState.Ok;
            chipCount = 0;
            LiveEventCalendarCheckReport report = context.Input.CheckReport;
            if (report == null) return;

            int blockedCount = 0;
            int warningCount = 0;
            for (int index = 0; index < report.Findings.Count; index++)
            {
                LiveEventCalendarFinding finding = report.Findings[index];
                if (finding.Consequence != LiveEventCalendarConsequence.Dropped && finding.Consequence != LiveEventCalendarConsequence.ProgressLost) continue;
                if (!context.TryGetLaneTypeOf(finding, out string findingTypeId) || !string.Equals(findingTypeId, typeId, StringComparison.Ordinal)) continue;
                if (finding.TargetKind == LiveEventCalendarTargetKind.FixedEvent && context.IsUnplaceable(finding.TargetEntryKey)) continue;
                if (finding.Consequence == LiveEventCalendarConsequence.Dropped) blockedCount++;
                else warningCount++;
            }
            if (blockedCount > 0)
            {
                chipState = HealthState.Blocked;
                chipCount = blockedCount;
            }
            else if (warningCount > 0)
            {
                chipState = HealthState.Warning;
                chipCount = warningCount;
            }
        }

        private static string HoursText(int hours)
        {
            // "lặp mỗi 7 ngày" nhưng "lặp mỗi 24 giờ" [SD1 §3.2]: chỉ đổi sang ngày khi chẵn ngày và dài hơn một ngày.
            if (hours > HoursPerDay && hours % HoursPerDay == 0)
            {
                return (hours / HoursPerDay).ToString(CultureInfo.InvariantCulture) + " " + LiveOpsHubStrings.DurationDayUnit;
            }
            return hours.ToString(CultureInfo.InvariantCulture) + " " + LiveOpsHubStrings.DurationHourUnit;
        }

        internal static HealthState StateOf(LiveEventCalendarConsequence consequence)
        {
            switch (consequence)
            {
                case LiveEventCalendarConsequence.Dropped: return HealthState.Blocked;
                case LiveEventCalendarConsequence.ProgressLost:
                case LiveEventCalendarConsequence.ShouldReview: return HealthState.Warning;
                default: return HealthState.Ok;
            }
        }

        internal static HealthState Worse(HealthState left, HealthState right)
        {
            return SectionHealth.RankOf(right) > SectionHealth.RankOf(left) ? right : left;
        }

        // ----- Minimap -----

        private static LiveOpsTimelineMinimapModel BuildMinimap(BuildContext context, List<LiveOpsTimelineLaneModel> lanes, DateTime? publishedAtUtc)
        {
            LiveOpsTimelineInput input = context.Input;
            LiveOpsTimelineGeometry geometry = context.Geometry;
            DateTime nowUtc = input.NowUtc;

            DateTime rangeStartUtc = FloorToDay(LiveOpsTimelineGeometry.AddTicksClamped(nowUtc, -TimeSpan.FromDays(MinimapDaysBeforeNow).Ticks));
            DateTime lastFixedEndUtc = nowUtc;
            IReadOnlyList<FixedLiveEventEntry> entries = input.Document.FixedEvents;
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].TryGetEndUtc(out DateTime endUtc) && endUtc > lastFixedEndUtc) lastFixedEndUtc = endUtc;
            }
            DateTime rangeEndUtc = CeilingToDay(LiveOpsTimelineGeometry.AddTicksClamped(lastFixedEndUtc, TimeSpan.FromDays(MinimapDaysAfterLastFixedEvent).Ticks));
            if (geometry.RangeStartUtc < rangeStartUtc) rangeStartUtc = geometry.RangeStartUtc;
            if (geometry.RangeEndUtc > rangeEndUtc) rangeEndUtc = geometry.RangeEndUtc;

            var minimapGeometry = new LiveOpsTimelineGeometry(rangeStartUtc, rangeEndUtc, geometry.TrackWidth);
            var intervalsPerLane = new List<List<(DateTime startUtc, DateTime endUtc)>>(lanes.Count);
            for (int laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
            {
                intervalsPerLane.Add(MinimapIntervalsOf(context, lanes[laneIndex].TypeId, minimapGeometry));
            }

            List<LiveOpsTimelineMinimapMark> marks = MinimapMarks(context, rangeStartUtc, rangeEndUtc);

            // Gom khe < 1px; nếu minimap vẫn vượt ngân sách (luật hằng giờ + năm dài) thì nhân đôi ngưỡng như làn.
            LiveOpsTimelineMinimapModel minimap = null;
            float stopThreshold = geometry.TrackWidth * 2f + MinimapSegmentGapThreshold;
            for (float threshold = MinimapSegmentGapThreshold; ; threshold *= 2f)
            {
                var bands = new List<LiveOpsTimelineMinimapBand>(lanes.Count);
                for (int laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
                {
                    bands.Add(new LiveOpsTimelineMinimapBand(lanes[laneIndex].TypeId, lanes[laneIndex].ColorSlot, laneIndex,
                        MergeByPixelGap(intervalsPerLane[laneIndex], minimapGeometry, threshold)));
                }
                minimap = new LiveOpsTimelineMinimapModel(rangeStartUtc, rangeEndUtc, geometry.TrackWidth, nowUtc, publishedAtUtc,
                    geometry.RangeStartUtc, geometry.RangeEndUtc, bands, marks);
                if (LiveOpsTimelineVertexBudget.EstimateMinimap(minimap) <= LiveOpsTimelineVertexBudget.LaneSafetyBudget || threshold > stopThreshold)
                {
                    return minimap;
                }
            }
        }

        private static List<(DateTime startUtc, DateTime endUtc)> MinimapIntervalsOf(BuildContext context, string typeId, LiveOpsTimelineGeometry minimapGeometry)
        {
            DateTime fromUtc = minimapGeometry.RangeStartUtc;
            DateTime toUtc = minimapGeometry.RangeEndUtc;
            var intervals = new List<(DateTime startUtc, DateTime endUtc)>();
            List<RecurringLiveEventCalendar> calendars = context.RecurringCalendarsOf(typeId);
            if (calendars.Count > 0) AppendMinimapRecurringSegments(calendars[0], minimapGeometry, intervals);
            List<FixedLiveEventEntry> entries = context.FixedEntriesOf(typeId);
            for (int index = 0; index < entries.Count; index++)
            {
                if (!entries[index].TryGetStartUtc(out DateTime startUtc) || !entries[index].TryGetEndUtc(out DateTime endUtc)) continue;
                if (endUtc <= startUtc || endUtc <= fromUtc || startUtc >= toUtc) continue;
                intervals.Add((startUtc, endUtc));
            }
            intervals.Sort((left, right) => left.startUtc.CompareTo(right.startUtc));
            return intervals;
        }

        private static IReadOnlyList<(DateTime startUtc, DateTime endUtc)> MergeByPixelGap(List<(DateTime startUtc, DateTime endUtc)> sortedIntervals,
            LiveOpsTimelineGeometry geometry, float threshold)
        {
            var merged = new List<(DateTime startUtc, DateTime endUtc)>();
            for (int index = 0; index < sortedIntervals.Count; index++)
            {
                (DateTime startUtc, DateTime endUtc) interval = sortedIntervals[index];
                if (merged.Count > 0)
                {
                    (DateTime startUtc, DateTime endUtc) last = merged[merged.Count - 1];
                    if (geometry.XOf(interval.startUtc) - geometry.XOf(last.endUtc) < threshold)
                    {
                        merged[merged.Count - 1] = (last.startUtc, interval.endUtc > last.endUtc ? interval.endUtc : last.endUtc);
                        continue;
                    }
                }
                merged.Add(interval);
            }
            return merged;
        }

        private static List<LiveOpsTimelineMinimapMark> MinimapMarks(BuildContext context, DateTime fromUtc, DateTime toUtc)
        {
            var marks = new List<LiveOpsTimelineMinimapMark>();
            var markedEntryKeys = new HashSet<string>(StringComparer.Ordinal);
            LiveEventCalendarCheckReport report = context.Input.CheckReport;
            if (report != null)
            {
                for (int index = 0; index < report.Findings.Count; index++)
                {
                    LiveEventCalendarFinding finding = report.Findings[index];
                    if (finding.TargetKind == LiveEventCalendarTargetKind.RemoteSnapshot) continue;
                    HealthState state = StateOf(finding.Consequence);
                    if (state == HealthState.Ok) continue;
                    DateTime? atUtc = finding.AnchorUtc ?? finding.RangeStartUtc ?? finding.RangeEndUtc;
                    if (!atUtc.HasValue || atUtc.Value < fromUtc || atUtc.Value >= toUtc) continue;
                    marks.Add(new LiveOpsTimelineMinimapMark(atUtc.Value, state, finding.TargetId, finding.TargetEntryKey, finding));
                    if (state == HealthState.Blocked && finding.TargetKind == LiveEventCalendarTargetKind.FixedEvent) markedEntryKeys.Add(finding.TargetEntryKey);
                }
            }

            // Báo cáo có thể chưa có hoặc cũ: đợt game đang bỏ theo bản biên dịch hiện tại vẫn phải có vạch đỏ.
            IReadOnlyList<FixedLiveEventEntry> entries = context.Input.Document.FixedEvents;
            for (int index = 0; index < entries.Count; index++)
            {
                FixedLiveEventEntry entry = entries[index];
                if (markedEntryKeys.Contains(entry.EntryKey) || !context.IsFixedDropped(entry)) continue;
                DateTime atUtc;
                if (!entry.TryGetStartUtc(out atUtc) && !entry.TryGetEndUtc(out atUtc)) continue;
                if (atUtc < fromUtc || atUtc >= toUtc) continue;
                marks.Add(new LiveOpsTimelineMinimapMark(atUtc, HealthState.Blocked, entry.EventId, entry.EntryKey, null));
            }

            if (marks.Count > MaximumMinimapMarks)
            {
                marks.Sort((left, right) =>
                {
                    int bySeverity = SectionHealth.RankOf(right.State).CompareTo(SectionHealth.RankOf(left.State));
                    return bySeverity != 0 ? bySeverity : left.AtUtc.CompareTo(right.AtUtc);
                });
                marks.RemoveRange(MaximumMinimapMarks, marks.Count - MaximumMinimapMarks);
            }
            StableSortByTime(marks);
            return marks;
        }

        private static void StableSortByTime(List<LiveOpsTimelineMinimapMark> marks)
        {
            var ordered = new List<(LiveOpsTimelineMinimapMark mark, int position)>(marks.Count);
            for (int index = 0; index < marks.Count; index++) ordered.Add((marks[index], index));
            ordered.Sort((left, right) =>
            {
                int byTime = left.mark.AtUtc.CompareTo(right.mark.AtUtc);
                return byTime != 0 ? byTime : left.position.CompareTo(right.position);
            });
            for (int index = 0; index < ordered.Count; index++) marks[index] = ordered[index].mark;
        }

        private static DateTime FloorToDay(DateTime utc)
        {
            return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
        }

        private static DateTime CeilingToDay(DateTime utc)
        {
            DateTime floor = FloorToDay(utc);
            return floor == utc ? floor : LiveOpsTimelineGeometry.AddTicksClamped(floor, TimeSpan.TicksPerDay);
        }

        // ----- Kiểu phụ -----

        private sealed class FixedCandidate
        {
            public FixedCandidate(FixedLiveEventEntry entry, DateTime startUtc, DateTime endUtc, int documentPosition)
            {
                Entry = entry;
                StartUtc = startUtc;
                EndUtc = endUtc;
                DocumentPosition = documentPosition;
            }

            public FixedLiveEventEntry Entry { get; }
            public DateTime StartUtc { get; }
            public DateTime EndUtc { get; }
            public int DocumentPosition { get; }

            public static int CompareByStart(FixedCandidate left, FixedCandidate right)
            {
                int byStart = left.StartUtc.CompareTo(right.StartUtc);
                return byStart != 0 ? byStart : left.DocumentPosition.CompareTo(right.DocumentPosition);
            }
        }

        private sealed class PendingBar
        {
            private readonly string _barKey;
            private readonly string _eventId;
            private readonly string _eventType;
            private readonly LiveOpsTimelineBarSource _source;
            private readonly int _stripCount;
            private readonly double _stripActiveHours;
            private readonly LiveEventPhase _phase;
            private readonly bool _isRunning;
            private readonly bool _isEnded;
            private readonly bool _isDropped;
            private readonly bool _isChanged;
            private readonly string _renamedFromId;
            private readonly HealthState _worstFinding;

            public PendingBar(string barKey, string eventId, string eventType, LiveOpsTimelineBarSource source, DateTime startUtc, DateTime endUtc,
                int stripCount, LiveEventPhase phase, bool isRunning, bool isEnded, bool isDropped, bool isChanged, string renamedFromId,
                HealthState worstFinding, int sortGroup, double stripActiveHours = 0)
            {
                _stripActiveHours = stripActiveHours;
                _barKey = barKey;
                _eventId = eventId;
                _eventType = eventType;
                _source = source;
                StartUtc = startUtc;
                EndUtc = endUtc;
                _stripCount = stripCount;
                _phase = phase;
                _isRunning = isRunning;
                _isEnded = isEnded;
                _isDropped = isDropped;
                _isChanged = isChanged;
                _renamedFromId = renamedFromId;
                _worstFinding = worstFinding;
                SortGroup = sortGroup;
            }

            public PendingBar(FixedCandidate candidate)
            {
                Candidate = candidate;
                StartUtc = candidate.StartUtc;
                EndUtc = candidate.EndUtc;
                SortGroup = 1;
                DocumentPosition = candidate.DocumentPosition;
            }

            public PendingBar(List<FixedCandidate> members)
            {
                FixedCandidate first = members[0];
                FixedCandidate last = members[members.Count - 1];
                DateTime endUtc = first.EndUtc;
                for (int index = 1; index < members.Count; index++)
                {
                    if (members[index].EndUtc > endUtc) endUtc = members[index].EndUtc;
                }
                double activeHours = 0d;
                for (int index = 0; index < members.Count; index++)
                {
                    activeHours += (members[index].EndUtc - members[index].StartUtc).TotalHours;
                }
                _stripActiveHours = activeHours;
                Members = members;
                StartUtc = first.StartUtc;
                EndUtc = endUtc;
                _barKey = first.Entry.EventType + FixedStripKeyInfix + first.Entry.EntryKey;
                _eventId = first.Entry.EventId + LiveOpsHubStrings.TimelineStripIdSeparator + last.Entry.EventId;
                _eventType = first.Entry.EventType;
                _source = LiveOpsTimelineBarSource.FixedStrip;
                _stripCount = members.Count;
                SortGroup = 1;
                DocumentPosition = first.DocumentPosition;
            }

            public FixedCandidate Candidate { get; }
            public List<FixedCandidate> Members { get; }
            public DateTime StartUtc { get; }
            public DateTime EndUtc { get; }

            /// <summary>0 = lần lặp, 1 = đợt cố định — lần lặp giữ hàng 0 khi trùng giờ vì game ưu tiên luật lặp (PD-3).</summary>
            public int SortGroup { get; }

            public int DocumentPosition { get; }

            public static int CompareForRows(PendingBar left, PendingBar right)
            {
                int byStart = left.StartUtc.CompareTo(right.StartUtc);
                if (byStart != 0) return byStart;
                int byGroup = left.SortGroup.CompareTo(right.SortGroup);
                return byGroup != 0 ? byGroup : left.DocumentPosition.CompareTo(right.DocumentPosition);
            }

            public LiveOpsTimelineBarModel ToModel(int row)
            {
                return new LiveOpsTimelineBarModel(_barKey, _eventId, _eventType, _source, StartUtc, EndUtc, row, _stripCount, _phase, _isRunning,
                    _isEnded, _isDropped, _isChanged, _renamedFromId, _worstFinding, _stripActiveHours);
            }

            public LiveOpsTimelineBarModel ToStripModel(BuildContext context, int row)
            {
                DateTime nowUtc = context.Input.NowUtc;
                bool isRunning = false;
                bool isDropped = false;
                bool isChanged = false;
                HealthState worst = HealthState.Ok;
                for (int index = 0; index < Members.Count; index++)
                {
                    FixedLiveEventEntry entry = Members[index].Entry;
                    if (PhaseOf(Members[index].StartUtc, Members[index].EndUtc, nowUtc) == LiveEventPhase.Active) isRunning = true;
                    isDropped |= context.IsFixedDropped(entry);
                    isChanged |= context.IsFixedChanged(entry.EntryKey);
                    worst = Worse(worst, context.WorstFixedFinding(entry.EntryKey));
                }
                LiveEventPhase phase = PhaseOf(StartUtc, EndUtc, nowUtc);
                return new LiveOpsTimelineBarModel(_barKey, _eventId, _eventType, _source, StartUtc, EndUtc, row, _stripCount, phase, isRunning,
                    EndUtc <= nowUtc, isDropped, isChanged, string.Empty, worst, _stripActiveHours);
            }
        }

        private sealed class LaneComposition
        {
            public LaneComposition(List<LiveOpsTimelineBarModel> bars, int rowCount, IReadOnlyList<(DateTime startUtc, DateTime endUtc)> overlapRanges)
            {
                Bars = bars;
                RowCount = rowCount;
                OverlapRanges = overlapRanges;
            }

            public List<LiveOpsTimelineBarModel> Bars { get; }
            public int RowCount { get; }
            public IReadOnlyList<(DateTime startUtc, DateTime endUtc)> OverlapRanges { get; }
        }

        /// <summary>Bảng tra dựng một lần mỗi lần Build — tránh quét lại báo cáo/diff/tài liệu cho từng thanh.</summary>
        private sealed class BuildContext
        {
            private readonly Dictionary<string, List<FixedLiveEventEntry>> _fixedEntriesByType = new Dictionary<string, List<FixedLiveEventEntry>>(StringComparer.Ordinal);
            private readonly Dictionary<string, List<RecurringLiveEventCalendar>> _recurringCalendarsByType =
                new Dictionary<string, List<RecurringLiveEventCalendar>>(StringComparer.Ordinal);
            private readonly HashSet<string> _unplaceableEntryKeys = new HashSet<string>(StringComparer.Ordinal);
            private readonly HashSet<string> _changedFixedEntryKeys = new HashSet<string>(StringComparer.Ordinal);
            private readonly Dictionary<string, (string before, string after)> _renamedRunningIdByType =
                new Dictionary<string, (string before, string after)>(StringComparer.Ordinal);
            private readonly Dictionary<string, HealthState> _worstFixedFindingByEntryKey = new Dictionary<string, HealthState>(StringComparer.Ordinal);
            private readonly List<LiveEventCalendarFinding> _recurringFindings = new List<LiveEventCalendarFinding>();
            private static readonly List<RecurringLiveEventCalendar> NoCalendars = new List<RecurringLiveEventCalendar>();
            private static readonly List<FixedLiveEventEntry> NoEntries = new List<FixedLiveEventEntry>();

            public BuildContext(LiveOpsTimelineInput input, LiveOpsTimelineGeometry geometry)
            {
                Input = input;
                Geometry = geometry;
                Compilation = input.Compilation ?? LiveEventCalendarCompiler.CompileInExportOrder(input.Document);

                IReadOnlyList<FixedLiveEventEntry> entries = input.Document.FixedEvents;
                for (int index = 0; index < entries.Count; index++)
                {
                    FixedLiveEventEntry entry = entries[index];
                    if (!_fixedEntriesByType.TryGetValue(entry.EventType, out List<FixedLiveEventEntry> list))
                    {
                        list = new List<FixedLiveEventEntry>();
                        _fixedEntriesByType.Add(entry.EventType, list);
                    }
                    list.Add(entry);
                    if (!entry.TryGetStartUtc(out _) || !entry.TryGetEndUtc(out _)) _unplaceableEntryKeys.Add(entry.EntryKey);
                }

                IReadOnlyList<RecurringLiveEventCalendar> calendars = Compilation.RecurringCalendars;
                for (int index = 0; index < calendars.Count; index++)
                {
                    if (!_recurringCalendarsByType.TryGetValue(calendars[index].EventType, out List<RecurringLiveEventCalendar> list))
                    {
                        list = new List<RecurringLiveEventCalendar>();
                        _recurringCalendarsByType.Add(calendars[index].EventType, list);
                    }
                    list.Add(calendars[index]);
                }

                IndexDiff(input.PublishedDiff);
                IndexReport(input.CheckReport);
            }

            public LiveOpsTimelineInput Input { get; }
            public LiveOpsTimelineGeometry Geometry { get; }
            public LiveEventCalendarCompilation Compilation { get; }

            /// <summary>
            /// Thứ tự loại của tài liệu (= thứ tự làn), rồi loại chưa khai báo mà luật/đợt đang dùng — không đợt nào biến mất khỏi Lịch
            /// [SD1 §3.1]. Kể cả loại rỗng: đợt/luật chưa ghi loại có làn TypeId "" ở chỗ xuất hiện đầu tiên, để luật 3 (Bị bỏ) có thanh
            /// cho F8/vạch minimap nhảy tới và người dùng bấm vào sửa loại.
            /// </summary>
            public List<string> LaneTypeOrder()
            {
                LiveEventCalendarDocument document = Input.Document;
                var order = new List<string>();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < document.EventTypes.Count; index++)
                {
                    if (seen.Add(document.EventTypes[index].TypeId)) order.Add(document.EventTypes[index].TypeId);
                }
                for (int index = 0; index < document.RecurringRules.Count; index++)
                {
                    string typeId = document.RecurringRules[index].EventType;
                    if (seen.Add(typeId)) order.Add(typeId);
                }
                for (int index = 0; index < document.FixedEvents.Count; index++)
                {
                    string typeId = document.FixedEvents[index].EventType;
                    if (seen.Add(typeId)) order.Add(typeId);
                }
                return order;
            }

            public List<FixedLiveEventEntry> FixedEntriesOf(string typeId)
            {
                return _fixedEntriesByType.TryGetValue(typeId, out List<FixedLiveEventEntry> list) ? list : NoEntries;
            }

            public List<RecurringLiveEventCalendar> RecurringCalendarsOf(string typeId)
            {
                return _recurringCalendarsByType.TryGetValue(typeId, out List<RecurringLiveEventCalendar> list) ? list : NoCalendars;
            }

            public bool IsUnplaceable(string entryKey) => entryKey != null && _unplaceableEntryKeys.Contains(entryKey);

            public bool IsFixedDropped(FixedLiveEventEntry entry)
            {
                return Compilation.TryGetFixedOutcome(entry.EntryKey, out LiveEventCalendarEntryOutcome outcome) && !outcome.IsKept;
            }

            public bool IsFixedChanged(string entryKey) => _changedFixedEntryKeys.Contains(entryKey);

            /// <summary>Id cũ của lần lặp khi nó chính là lần đang chạy bị đổi id (weekly-pass-35 → pass-35); "" nếu không.</summary>
            public string RenamedRunningIdOf(string typeId, string eventId)
            {
                if (_renamedRunningIdByType.TryGetValue(typeId, out (string before, string after) renamed) &&
                    string.Equals(renamed.after, eventId, StringComparison.Ordinal))
                {
                    return renamed.before;
                }
                return string.Empty;
            }

            public HealthState WorstFixedFinding(string entryKey)
            {
                return _worstFixedFindingByEntryKey.TryGetValue(entryKey, out HealthState state) ? state : HealthState.Ok;
            }

            public HealthState WorstRecurringFinding(string typeId, string eventId, string previousId)
            {
                HealthState worst = HealthState.Ok;
                for (int index = 0; index < _recurringFindings.Count; index++)
                {
                    LiveEventCalendarFinding finding = _recurringFindings[index];
                    if (!string.Equals(finding.TargetId, typeId, StringComparison.Ordinal)) continue;
                    bool aboutThisOccurrence = string.Equals(finding.RelatedId, eventId, StringComparison.Ordinal) ||
                                               (previousId.Length > 0 && string.Equals(finding.RelatedId, previousId, StringComparison.Ordinal));
                    if (aboutThisOccurrence) worst = Worse(worst, StateOf(finding.Consequence));
                }
                return worst;
            }

            /// <summary>
            /// Loại (= làn) mà phát hiện thuộc về; <c>false</c> với phát hiện về bản remote hoặc đích không còn trong tài liệu. Tách "không
            /// thuộc làn nào" khỏi loại "" vì làn loại rỗng có thật — trả "" cho cả hai sẽ dồn phát hiện remote vào chip của làn đó.
            /// </summary>
            public bool TryGetLaneTypeOf(LiveEventCalendarFinding finding, out string typeId)
            {
                switch (finding.TargetKind)
                {
                    case LiveEventCalendarTargetKind.FixedEvent:
                        if (Input.Document.TryGetFixedEvent(finding.TargetEntryKey, out FixedLiveEventEntry entry))
                        {
                            typeId = entry.EventType;
                            return true;
                        }
                        break;
                    case LiveEventCalendarTargetKind.RecurringRule:
                    case LiveEventCalendarTargetKind.EventType:
                        typeId = finding.TargetId;
                        return true;
                }
                typeId = null;
                return false;
            }

            private void IndexDiff(LiveEventCalendarDiffResult diff)
            {
                if (diff == null) return;
                for (int index = 0; index < diff.Changes.Count; index++)
                {
                    LiveEventCalendarChange change = diff.Changes[index];
                    if (change.Kind == LiveEventCalendarChangeKind.Removed || change.Kind == LiveEventCalendarChangeKind.Kept) continue;
                    if (change.ItemKind == LiveEventCalendarItemKind.FixedEvent && change.EntryKey.Length > 0)
                    {
                        _changedFixedEntryKeys.Add(change.EntryKey);
                    }
                    else if (change.ItemKind == LiveEventCalendarItemKind.RecurringRule && change.RunningEventIdAfter.Length > 0 &&
                             !string.Equals(change.RunningEventIdBefore, change.RunningEventIdAfter, StringComparison.Ordinal))
                    {
                        _renamedRunningIdByType[change.ItemId] = (change.RunningEventIdBefore, change.RunningEventIdAfter);
                    }
                }
            }

            private void IndexReport(LiveEventCalendarCheckReport report)
            {
                if (report == null) return;
                for (int index = 0; index < report.Findings.Count; index++)
                {
                    LiveEventCalendarFinding finding = report.Findings[index];
                    HealthState state = StateOf(finding.Consequence);
                    if (finding.TargetKind == LiveEventCalendarTargetKind.FixedEvent && finding.TargetEntryKey.Length > 0)
                    {
                        HealthState current = WorstFixedFinding(finding.TargetEntryKey);
                        _worstFixedFindingByEntryKey[finding.TargetEntryKey] = Worse(current, state);
                    }
                    else if (finding.TargetKind == LiveEventCalendarTargetKind.RecurringRule)
                    {
                        _recurringFindings.Add(finding);
                    }
                }
            }
        }
    }
}
