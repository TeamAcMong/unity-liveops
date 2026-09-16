using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Timeline của màn Lịch [SD1 §3.1–3.7]: thước ba tầng → thân cuộn dọc (mỗi loại một hàng = header 168px + <see cref="LiveOpsTimelineLane"/>)
    /// → minimap → chú giải → dòng gợi ý, cộng lớp phủ vạch bây giờ 2px, vạch con trỏ và readout khi kéo.
    ///
    /// View mỏng: vẽ lại từ <see cref="LiveOpsTimelineModel"/> mà presenter đưa vào (<see cref="SetModel"/>), không đọc tài liệu, không
    /// quyết định luật lịch. Mọi thao tác của người dùng thành SỰ KIỆN dữ liệu (V-10): intent sửa lịch (<see cref="IntentRaised"/>), đổi
    /// khoảng (<see cref="RangeChanged"/> — presenter dựng lại model), hover, yêu cầu menu ngữ cảnh. Element không dựng menu, hover card
    /// hay popover: đó là việc của presenter W4/W5, để W5 gắn thêm mà không sửa file Timeline/*.
    ///
    /// Phím theo [FD §5.1]: phím chỉ có nghĩa trên timeline đăng ký trên chính element (focusable), bỏ qua khi đang gõ trong TextField
    /// (target là TextElement bên trong ở cả hai bản [API §12.1]); Tab/Shift Tab qua NavigationMoveEvent, còn đợt kế thì giữ focus,
    /// hết đợt thì để focus đi tiếp; lệnh Edit (Copy, Duplicate, SoftDelete/Delete, FrameSelected, Paste) qua Validate/ExecuteCommandEvent.
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
#endif
    public sealed partial class LiveOpsTimelineElement : VisualElement
    {
        /// <summary>Delta một nấc bánh xe chuột của IMGUI/UI Toolkit Editor (3 dòng) — trackpad ra số lẻ, chia ra nấc lẻ và cộng dồn được.</summary>
        internal const float WheelDeltaPerNotch = 3f;

        /// <summary>Căn khung một đợt/dải chừa 10% mỗi bên để mép đợt không dính mép track.</summary>
        internal const double FrameMarginRatio = 0.1;

        /// <summary>Track mặc định trước khi có layout (số của Hình 1) — chỉ để px/giờ không vô hạn khi UXML đặt zoom trước layout.</summary>
        internal const float FallbackTrackWidth = 635f;

        internal const float ReadoutMargin = LiveOpsTimelineGeometry.ReadoutMargin;
        internal const float ReadoutHeight = 18f;
        internal const float ReadoutGapAboveBar = 2f;
        internal const float NowLineWidth = 2f;

        private readonly List<LaneRow> _rows = new List<LaneRow>();
        private readonly Dictionary<string, LiveOpsTimelineMinimapMark> _markByTarget = new Dictionary<string, LiveOpsTimelineMinimapMark>(StringComparer.Ordinal);
        private readonly VisualElement _main;
        private readonly ScrollView _body;
        private readonly VisualElement _overlay;
        private readonly VisualElement _ghost;
        private readonly Label _readoutText;
        private readonly Label _readoutOverlap;

        private LiveOpsTimelineModel _model;
        private LiveOpsTimelineZoom _zoom = LiveOpsTimelineZoom.ThreeWeeks;
        private DateTime _rangeStartUtc;
        private double _pixelsPerHour;
        private float _trackWidth;
        private TimeSpan _deviceOffset;
        private LiveOpsHubFormat _format = new LiveOpsHubFormat(TimeSpan.Zero);
        private string _selectedBarKey = string.Empty;
        private string _keyboardFocusBarKey = string.Empty;
        private string _hoverBarKey = string.Empty;
        private DateTime? _cursorUtc;
        private string _cursorLaneTypeId = string.Empty;
        private bool _consumeNextHorizontalNavigation;

        public LiveOpsTimelineElement()
        {
            AddToClassList(LiveOpsHubClassNames.Timeline);
            focusable = true;
            tabIndex = 0;
            pickingMode = PickingMode.Position;

            _main = new VisualElement { pickingMode = PickingMode.Ignore };
            _main.AddToClassList(LiveOpsHubClassNames.TimelineMain);
            Add(_main);

            Ruler = new LiveOpsTimelineRuler();
            _main.Add(Ruler);
            _body = new ScrollView(ScrollViewMode.Vertical);
            _body.AddToClassList(LiveOpsHubClassNames.TimelineBody);
            _body.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _main.Add(_body);

            _overlay = new VisualElement { pickingMode = PickingMode.Ignore };
            _overlay.AddToClassList(LiveOpsHubClassNames.TimelineOverlay);
            _main.Add(_overlay);
            NowLine = new VisualElement { pickingMode = PickingMode.Ignore };
            NowLine.AddToClassList(LiveOpsHubClassNames.TimelineNowLine);
            _overlay.Add(NowLine);
            CursorLine = new VisualElement { pickingMode = PickingMode.Ignore };
            CursorLine.AddToClassList(LiveOpsHubClassNames.TimelineCursorLine);
            CursorLine.AddToClassList(LiveOpsHubClassNames.TimelineHidden);
            _overlay.Add(CursorLine);
            Readout = new VisualElement { pickingMode = PickingMode.Ignore };
            Readout.AddToClassList(LiveOpsHubClassNames.TimelineReadout);
            Readout.AddToClassList(LiveOpsHubClassNames.TimelineHidden);
            _readoutText = new Label { pickingMode = PickingMode.Ignore };
            _readoutText.AddToClassList(LiveOpsHubClassNames.TimelineReadoutText);
            Readout.Add(_readoutText);
            _readoutOverlap = new Label { pickingMode = PickingMode.Ignore };
            _readoutOverlap.AddToClassList(LiveOpsHubClassNames.TimelineReadoutOverlap);
            _readoutOverlap.AddToClassList(LiveOpsHubClassNames.TextBlocked);
            Readout.Add(_readoutOverlap);
            _overlay.Add(Readout);

            _ghost = new VisualElement { pickingMode = PickingMode.Ignore };
            _ghost.AddToClassList(LiveOpsHubClassNames.TimelineBarGhost);

            Minimap = new LiveOpsTimelineMinimap();
            Minimap.JumpRequested += OnMinimapJumpRequested;
            Add(Minimap);
            Legend = new LiveOpsTimelineLegend();
            Add(Legend);
            HintLine = new LiveOpsTimelineHintLine();
            Add(HintLine);

            DragController = new LiveOpsTimelineDragController();
            DragController.IntentRaised += intent => IntentRaised?.Invoke(intent);

            _trackWidth = FallbackTrackWidth;
            _pixelsPerHour = _trackWidth / LiveOpsTimelineGeometry.RangeLengthOf(_zoom).TotalHours;

            Ruler.Track.RegisterCallback<GeometryChangedEvent>(OnTrackGeometryChanged);
            RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            RegisterCallback<FocusInEvent>(focusEvent => EnableInClassList(LiveOpsHubClassNames.TimelineHasFocus, true));
            RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        // ================================================================================================ API công khai (mục 3)

        /// <summary>Thuộc tính UXML <c>zoom-level</c>: 0 Ngày · 1 3 tuần · 2 Tháng (SP-1). Ngoài khoảng thì kẹp; đặt = về đúng preset.</summary>
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute]
#endif
        public int ZoomLevel
        {
            get => (int)_zoom;
            set => SetRange(_rangeStartUtc, (LiveOpsTimelineZoom)Math.Max((int)LiveOpsTimelineZoom.Day, Math.Min((int)LiveOpsTimelineZoom.Month, value)));
        }

        public DateTime RangeStartUtc => _rangeStartUtc;

        /// <summary>Preset: bề rộng track ÷ số giờ của preset; zoom liên tục: giá trị đã kẹp 0,25–48.</summary>
        public double PixelsPerHour => _pixelsPerHour;

        /// <summary>(V-11) true sau ⌘/Ctrl + bánh xe hoặc căn khung; về preset khi đặt <see cref="ZoomLevel"/> / <see cref="SetRange"/>.</summary>
        public bool IsContinuousScale { get; private set; }

        public string SelectedBarKey => _selectedBarKey;

        /// <summary>Khoảng hoặc bề rộng track đổi — presenter dựng lại model với <c>WithRange(start, end)</c> + <c>WithTrackWidth(TrackWidth)</c>.</summary>
        public event Action<DateTime, DateTime> RangeChanged;

        internal event Action<LiveOpsTimelineIntent> IntentRaised;

        /// <summary>(V-10) Vào/rời thanh: BarKey + worldBound; không trễ — trễ 500/100 ms của hover card do host lo.</summary>
        internal event Action<LiveOpsTimelineHover> HoverChanged;

        /// <summary>(V-10) Chuột phải / phím Menu: vị trí trúng + toạ độ; element KHÔNG dựng menu.</summary>
        internal event Action<LiveOpsTimelineContextRequest> ContextRequested;

        /// <summary>Bấm chip "Không đặt được (n)" ở header làn — TypeId làn; presenter chọn đợt không đặt được.</summary>
        internal event Action<string> UnplaceableRequested;

        internal LiveOpsTimelineDragController DragController { get; }

        internal void SetModel(LiveOpsTimelineModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _rangeStartUtc = model.RangeStartUtc;
            _pixelsPerHour = model.Geometry.PixelsPerHour;

            _markByTarget.Clear();
            for (int index = 0; index < model.Minimap.Marks.Count; index++)
            {
                LiveOpsTimelineMinimapMark mark = model.Minimap.Marks[index];
                if (mark.TargetEntryKey.Length > 0 && !_markByTarget.ContainsKey(EntryKeyPrefix + mark.TargetEntryKey)) _markByTarget[EntryKeyPrefix + mark.TargetEntryKey] = mark;
                if (mark.TargetId.Length > 0 && !_markByTarget.ContainsKey(IdPrefix + mark.TargetId)) _markByTarget[IdPrefix + mark.TargetId] = mark;
            }

            BindRows(model);
            Ruler.SetModel(model, RulerZoom, _deviceOffset, _format);
            Ruler.SetCursor(_cursorUtc);
            Minimap.SetMinimap(model.Minimap, _format);
            BindNowLine(model);
            ApplyBarStates();
            RefreshDragVisuals();
            UpdateHintLine();
        }

        internal void SetRange(DateTime rangeStartUtc, LiveOpsTimelineZoom zoom)
        {
            _zoom = zoom;
            IsContinuousScale = false;
            _rangeStartUtc = DateTime.SpecifyKind(rangeStartUtc, DateTimeKind.Utc);
            _pixelsPerHour = _trackWidth / LiveOpsTimelineGeometry.RangeLengthOf(zoom).TotalHours;
            RaiseRangeChanged();
        }

        /// <summary>Zoom liên tục 0,25–48 px/giờ; <paramref name="anchorUtc"/> đứng yên tại chỗ nó đang hiện (ngoài khung thì neo giữa track).</summary>
        public void SetContinuousScale(double pixelsPerHour, DateTime anchorUtc)
        {
            double nextScale = LiveOpsTimelineGeometry.ClampScale(pixelsPerHour);
            DateTime anchor = DateTime.SpecifyKind(anchorUtc, DateTimeKind.Utc);
            double anchorX = (anchor - _rangeStartUtc).TotalHours * _pixelsPerHour;
            if (anchorX < 0 || anchorX > _trackWidth || double.IsNaN(anchorX)) anchorX = _trackWidth / 2f;
            _rangeStartUtc = LiveOpsTimelineGeometry.AddTicksClamped(anchor, -(long)Math.Round(anchorX / nextScale * TimeSpan.TicksPerHour));
            _pixelsPerHour = nextScale;
            IsContinuousScale = true;
            RaiseRangeChanged();
        }

        /// <summary>Căn khung tất cả (phím A): khoảng của minimap — 30 ngày trước bây giờ tới 7 ngày sau đợt cố định cuối.</summary>
        public void FrameAll()
        {
            if (_model == null) return;
            FrameRange(_model.Minimap.RangeStartUtc, _model.Minimap.RangeEndUtc, 0d);
        }

        public void FrameInstance(string laneTypeId, DateTime startUtc, DateTime endUtc)
        {
            FrameRange(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc), DateTime.SpecifyKind(endUtc, DateTimeKind.Utc), FrameMarginRatio);
            LaneRow row = FindRow(laneTypeId);
            if (row != null && row.Row.panel != null) _body.ScrollTo(row.Row);
        }

        public void Select(string barKey, bool focus)
        {
            _selectedBarKey = barKey ?? string.Empty;
            _keyboardFocusBarKey = focus ? _selectedBarKey : string.Empty;
            if (focus) Focus();
            ApplyBarStates();
            UpdateHintLine();
        }

        internal LiveOpsTimelineHit HitTest(Vector2 localPosition)
        {
            Vector2 world = this.LocalToWorld(localPosition);
            if (Ruler.worldBound.Contains(world))
            {
                DateTime? rulerTime = Ruler.Track.worldBound.Contains(world) ? SnappedTimeAt(Ruler.Track.WorldToLocal(world).x) : (DateTime?)null;
                return new LiveOpsTimelineHit(LiveOpsTimelineHitKind.Ruler, string.Empty, LiveOpsTimelineBarRegion.Body, string.Empty, rulerTime, localPosition);
            }
            if (!_body.contentViewport.worldBound.Contains(world)) return new LiveOpsTimelineHit(LiveOpsTimelineHitKind.None, string.Empty,
                LiveOpsTimelineBarRegion.Body, string.Empty, null, localPosition);
            for (int index = 0; index < _rows.Count; index++)
            {
                LaneRow row = _rows[index];
                if (!row.IsActive || !row.Row.worldBound.Contains(world)) continue;
                string typeId = row.Lane.Model.TypeId;
                if (row.Header.worldBound.Contains(world))
                {
                    return new LiveOpsTimelineHit(LiveOpsTimelineHitKind.LaneHeader, string.Empty, LiveOpsTimelineBarRegion.Body, typeId, null, localPosition);
                }
                Vector2 laneLocal = row.Lane.WorldToLocal(world);
                LiveOpsTimelineBar bar = row.Lane.BarAt(laneLocal.x, laneLocal.y);
                if (bar != null)
                {
                    return new LiveOpsTimelineHit(LiveOpsTimelineHitKind.Bar, bar.Model.BarKey, bar.RegionAt(laneLocal.x), typeId, null, localPosition);
                }
                return new LiveOpsTimelineHit(LiveOpsTimelineHitKind.EmptyLane, string.Empty, LiveOpsTimelineBarRegion.Body, typeId,
                    SnappedTimeAt(laneLocal.x), localPosition);
            }
            return new LiveOpsTimelineHit(LiveOpsTimelineHitKind.None, string.Empty, LiveOpsTimelineBarRegion.Body, string.Empty, null, localPosition);
        }

        /// <summary>(V-11) Tầng thước giờ máy (zoom Ngày), bubble con trỏ, readout kéo mép, tooltip giờ máy.</summary>
        public void SetDeviceOffset(TimeSpan deviceOffset)
        {
            _deviceOffset = deviceOffset;
            _format = new LiveOpsHubFormat(deviceOffset);
            if (_model != null) SetModel(_model);
        }

        // ================================================================================================ móc cho presenter (V-10)

        /// <summary>Menu header làn "Ẩn làn" (presenter W5 gọi) — presenter giữ danh sách ẩn trong view state rồi dựng lại model.</summary>
        internal void RequestHideLane(string typeId)
        {
            IntentRaised?.Invoke(new HideLaneIntent(typeId));
        }

        internal void RequestShowAllLanes()
        {
            IntentRaised?.Invoke(new ShowAllLanesIntent());
        }

        /// <summary>"Đưa lên" (−1) / "Đưa xuống" (+1) (V-12) — presenter biến thành <c>MoveEventTypeEdit</c>.</summary>
        internal void RequestMoveLane(string typeId, int direction)
        {
            IntentRaised?.Invoke(new MoveLaneIntent(typeId, direction));
        }

        /// <summary>Presenter bật khi clipboard có JSON đợt — lệnh Paste chỉ hợp lệ lúc đó.</summary>
        internal bool HasCopiedEvent { get; set; }

        // ================================================================================================ trạng thái cho test/presenter

        internal LiveOpsTimelineModel Model => _model;
        internal LiveOpsTimelineZoom Zoom => _zoom;

        /// <summary>Bề rộng track thật (sau layout) — presenter truyền vào <c>WithTrackWidth</c>.</summary>
        internal float TrackWidth => _trackWidth;

        internal DateTime RangeEndUtc => IsContinuousScale
            ? LiveOpsTimelineGeometry.AddTicksClamped(_rangeStartUtc, (long)Math.Round(_trackWidth / _pixelsPerHour * TimeSpan.TicksPerHour))
            : LiveOpsTimelineGeometry.AddTicksClamped(_rangeStartUtc, LiveOpsTimelineGeometry.RangeLengthOf(_zoom).Ticks);

        internal LiveOpsTimelineRuler Ruler { get; }
        internal LiveOpsTimelineMinimap Minimap { get; }
        internal LiveOpsTimelineLegend Legend { get; }
        internal LiveOpsTimelineHintLine HintLine { get; }
        internal VisualElement NowLine { get; }
        internal VisualElement CursorLine { get; }
        internal VisualElement Readout { get; }
        internal Label ReadoutText => _readoutText;
        internal Label ReadoutOverlap => _readoutOverlap;
        internal ScrollView Body => _body;
        internal string HoverBarKey => _hoverBarKey;
        internal DateTime? CursorUtc => _cursorUtc;

        internal int LaneCount
        {
            get
            {
                int count = 0;
                foreach (LaneRow row in _rows)
                {
                    if (row.IsActive) count++;
                }
                return count;
            }
        }

        internal LiveOpsTimelineLane LaneAt(int index) => _rows[index].Lane;
        internal LiveOpsTimelineLaneHeader HeaderAt(int index) => _rows[index].Header;

        internal LiveOpsTimelineLane FindLaneElement(string typeId) => FindRow(typeId)?.Lane;
        internal LiveOpsTimelineLaneHeader FindHeader(string typeId) => FindRow(typeId)?.Header;

        internal LiveOpsTimelineBar FindBar(string barKey)
        {
            foreach (LaneRow row in _rows)
            {
                if (!row.IsActive) continue;
                LiveOpsTimelineBar bar = row.Lane.FindBar(barKey);
                if (bar != null) return bar;
            }
            return null;
        }

        /// <summary>Zoom của thước: preset khi không zoom liên tục; zoom liên tục suy từ px/giờ theo cùng ngưỡng bắt lưới.</summary>
        internal LiveOpsTimelineZoom RulerZoom
        {
            get
            {
                if (!IsContinuousScale) return _zoom;
                if (_pixelsPerHour >= LiveOpsTimelineGeometry.QuarterHourSnapMinimumPixelsPerHour) return LiveOpsTimelineZoom.Day;
                return _pixelsPerHour >= LiveOpsTimelineGeometry.HourSnapMinimumPixelsPerHour ? LiveOpsTimelineZoom.ThreeWeeks : LiveOpsTimelineZoom.Month;
            }
        }

        /// <summary>Tooltip của thanh: id + khoảng UTC; có phát hiện thì thêm câu vạch minimap (LiveOpsFindingText.ShortLabel, CC-FT-1).</summary>
        internal string BarTooltip(LiveOpsTimelineBarModel bar)
        {
            string range;
            if (bar.IsStrip)
            {
                range = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineStripTooltipFormat, bar.EventId, bar.StripCount,
                    _format.ShortDateTime(bar.StartUtc), _format.ShortDateTime(bar.EndUtc));
            }
            else
            {
                range = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineBarTooltipFormat, bar.EventId,
                    _format.ShortDateTime(bar.StartUtc), _format.ShortDateTime(bar.EndUtc));
            }
            LiveOpsTimelineMinimapMark mark = MarkOf(bar);
            return mark == null ? range : range + "\n" + LiveOpsTimelineMinimap.MarkTooltip(mark);
        }

        // ================================================================================================ dựng lại

        private const string EntryKeyPrefix = "entry:";
        private const string IdPrefix = "id:";

        private LiveOpsTimelineMinimapMark MarkOf(LiveOpsTimelineBarModel bar)
        {
            if (bar.IsStrip || bar.WorstFinding == HealthState.Ok) return null;
            if (bar.Source == LiveOpsTimelineBarSource.Fixed && _markByTarget.TryGetValue(EntryKeyPrefix + bar.BarKey, out LiveOpsTimelineMinimapMark byEntry)) return byEntry;
            if (_markByTarget.TryGetValue(IdPrefix + bar.EventId, out LiveOpsTimelineMinimapMark byId)) return byId;
            return bar.RenamedFromId.Length > 0 && _markByTarget.TryGetValue(IdPrefix + bar.RenamedFromId, out LiveOpsTimelineMinimapMark byOldId) ? byOldId : null;
        }

        private void BindRows(LiveOpsTimelineModel model)
        {
            for (int index = 0; index < model.Lanes.Count; index++)
            {
                if (index >= _rows.Count) _rows.Add(CreateRow());
                LaneRow row = _rows[index];
                LiveOpsTimelineLaneModel lane = model.Lanes[index];
                row.Header.Bind(lane);
                row.Lane.SetLane(lane, model.Geometry, model.NowUtc, _format, BarTooltip);
                row.Row.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, false);
                row.IsActive = true;
            }
            for (int index = model.Lanes.Count; index < _rows.Count; index++)
            {
                _rows[index].IsActive = false;
                _rows[index].Row.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, true);
            }
        }

        private LaneRow CreateRow()
        {
            VisualElement rowElement = new VisualElement { pickingMode = PickingMode.Ignore };
            rowElement.AddToClassList(LiveOpsHubClassNames.TimelineLaneRow);
            LiveOpsTimelineLaneHeader header = new LiveOpsTimelineLaneHeader();
            header.UnplaceableClicked += typeId => UnplaceableRequested?.Invoke(typeId);
            rowElement.Add(header);
            LiveOpsTimelineLane lane = new LiveOpsTimelineLane();
            lane.NextChipClicked += next => FrameInstance(next.EventType, next.StartUtc, next.EndUtc);
            rowElement.Add(lane);
            _body.Add(rowElement);
            return new LaneRow(rowElement, header, lane);
        }

        private LaneRow FindRow(string typeId)
        {
            foreach (LaneRow row in _rows)
            {
                if (row.IsActive && string.Equals(row.Lane.Model.TypeId, typeId ?? string.Empty, StringComparison.Ordinal)) return row;
            }
            return null;
        }

        private void BindNowLine(LiveOpsTimelineModel model)
        {
            bool visible = model.NowUtc >= model.RangeStartUtc && model.NowUtc <= model.RangeEndUtc;
            NowLine.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !visible);
            if (visible) NowLine.style.left = model.Geometry.XOf(model.NowUtc) - NowLineWidth / 2f; // style-inline-allowed: 3
        }

        private void ApplyBarStates()
        {
            foreach (LaneRow row in _rows)
            {
                if (!row.IsActive) continue;
                for (int index = 0; index < row.Lane.BarCount; index++)
                {
                    LiveOpsTimelineBar bar = row.Lane.BarAt(index);
                    string key = bar.Model.BarKey;
                    bool hover = key.Length > 0 && string.Equals(key, _hoverBarKey, StringComparison.Ordinal);
                    bar.SetInteractionState(hover, key.Length > 0 && string.Equals(key, _selectedBarKey, StringComparison.Ordinal),
                        key.Length > 0 && string.Equals(key, _keyboardFocusBarKey, StringComparison.Ordinal));
                    row.Lane.SetDroppedTagHidden(key, hover);
                }
            }
        }

        private void UpdateHintLine()
        {
            if (DragController.IsDragging)
            {
                HintLine.ShowDragging();
                return;
            }
            LiveOpsTimelineBar selected = FindBar(_selectedBarKey);
            if (selected == null)
            {
                HintLine.ShowIdle();
                return;
            }
            LiveOpsTimelineMinimapMark mark = MarkOf(selected.Model);
            string shortLabel = mark?.Finding != null ? LiveOpsFindingText.PlainText(LiveOpsFindingText.ShortLabel(mark.Finding)) : string.Empty;
            HintLine.ShowSelected(selected.Model, shortLabel);
        }

        private void RaiseRangeChanged()
        {
            RangeChanged?.Invoke(_rangeStartUtc, RangeEndUtc);
        }

        private DateTime SnappedTimeAt(float trackPosition)
        {
            LiveOpsTimelineGeometry geometry = CurrentGeometry();
            return LiveOpsTimelineGeometry.Snap(geometry.TimeAt(trackPosition), LiveOpsTimelineGeometry.AutoSnapStep(geometry.PixelsPerHour));
        }

        private LiveOpsTimelineGeometry CurrentGeometry()
        {
            if (_model != null) return _model.Geometry;
            DateTime end = RangeEndUtc;
            return new LiveOpsTimelineGeometry(_rangeStartUtc, end > _rangeStartUtc ? end : _rangeStartUtc.AddHours(1), Math.Max(1f, _trackWidth));
        }

        private void FrameRange(DateTime startUtc, DateTime endUtc, double marginRatio)
        {
            if (endUtc <= startUtc) endUtc = startUtc.AddHours(1);
            double spanHours = (endUtc - startUtc).TotalHours * (1d + marginRatio * 2d);
            double nextScale = LiveOpsTimelineGeometry.ClampScale(_trackWidth / spanHours);
            double visibleHours = _trackWidth / nextScale;
            double leadingHours = (visibleHours - (endUtc - startUtc).TotalHours) / 2d;
            _rangeStartUtc = LiveOpsTimelineGeometry.AddTicksClamped(startUtc, -(long)Math.Round(leadingHours * TimeSpan.TicksPerHour));
            _pixelsPerHour = nextScale;
            IsContinuousScale = true;
            RaiseRangeChanged();
        }

        private void OnTrackGeometryChanged(GeometryChangedEvent geometryEvent)
        {
            float width = Ruler.Track.layout.width;
            if (float.IsNaN(width) || width <= 0f || Math.Abs(width - _trackWidth) < 0.01f) return;
            _trackWidth = width;
            // Preset giữ đúng số ngày (px/giờ theo bề rộng); zoom liên tục giữ px/giờ (khoảng dài/ngắn theo bề rộng) [SD1 §3.2].
            if (!IsContinuousScale) _pixelsPerHour = _trackWidth / LiveOpsTimelineGeometry.RangeLengthOf(_zoom).TotalHours;
            RaiseRangeChanged();
        }

        private void OnMinimapJumpRequested(DateTime atUtc)
        {
            // Nhảy khung: giữ độ dài khoảng, đặt giờ bấm vào giữa track.
            double visibleHours = _trackWidth / _pixelsPerHour;
            _rangeStartUtc = LiveOpsTimelineGeometry.AddTicksClamped(atUtc, -(long)Math.Round(visibleHours / 2d * TimeSpan.TicksPerHour));
            RaiseRangeChanged();
        }

        // ================================================================================================ bánh xe (V-11)

        private void OnWheel(WheelEvent wheelEvent)
        {
            float delta = Math.Abs(wheelEvent.delta.y) > float.Epsilon ? wheelEvent.delta.y : wheelEvent.delta.x;
            if (Math.Abs(delta) <= float.Epsilon) return;
            double notches = delta / WheelDeltaPerNotch;
            if (wheelEvent.actionKey)
            {
                float anchorX = Ruler.Track.WorldToLocal(wheelEvent.mousePosition).x;
                (DateTime rangeStartUtc, double pixelsPerHour) zoomed =
                    LiveOpsTimelineGeometry.ZoomAround(_rangeStartUtc, _pixelsPerHour, notches, anchorX);
                _rangeStartUtc = zoomed.rangeStartUtc;
                _pixelsPerHour = zoomed.pixelsPerHour;
                IsContinuousScale = true;
                wheelEvent.StopPropagation();
                RaiseRangeChanged();
                return;
            }
            if (wheelEvent.shiftKey)
            {
                _rangeStartUtc = LiveOpsTimelineGeometry.ScrollBy(_rangeStartUtc, _pixelsPerHour, notches);
                wheelEvent.StopPropagation();
                RaiseRangeChanged();
            }
            // Bánh xe trơn: không nuốt — ScrollView của thân cuộn dọc danh sách làn như mọi danh sách của Editor.
        }

        // ================================================================================================ chuột

        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            Vector2 local = this.WorldToLocal(pointerEvent.position);
            LiveOpsTimelineHit hit = HitTest(local);
            if (pointerEvent.button == 1)
            {
                ContextRequested?.Invoke(new LiveOpsTimelineContextRequest(hit, pointerEvent.position));
                pointerEvent.StopPropagation();
                return;
            }
            if (pointerEvent.button != 0) return;
            Focus();
            LaneRow row = FindRow(hit.LaneTypeId);
            switch (hit.Kind)
            {
                case LiveOpsTimelineHitKind.Bar:
                    OnBarPointerDown(pointerEvent, hit, row);
                    break;
                case LiveOpsTimelineHitKind.EmptyLane:
                    OnEmptyLanePointerDown(pointerEvent, hit, row);
                    break;
            }
        }

        private void OnBarPointerDown(PointerDownEvent pointerEvent, LiveOpsTimelineHit hit, LaneRow row)
        {
            LiveOpsTimelineBar bar = row?.Lane.FindBar(hit.BarKey);
            if (bar == null) return;
            LiveOpsTimelineBarModel model = bar.Model;
            pointerEvent.StopPropagation();
            if (model.IsStrip)
            {
                // (CC-TLMODEL-1) bấm dải = zoom vào [StartUtc, EndUtc]; model tách lại thanh kéo được khi đủ gần. Không chọn, không kéo.
                FrameRange(model.StartUtc, model.EndUtc, FrameMarginRatio);
                return;
            }
            if (pointerEvent.clickCount == 2 && model.Source != LiveOpsTimelineBarSource.Fixed)
            {
                IntentRaised?.Invoke(new OpenRuleIntent(model.EventType));
                return;
            }
            Select(model.BarKey, false);
            IntentRaised?.Invoke(new SelectBarIntent(model.BarKey, pointerEvent.actionKey, pointerEvent.shiftKey));
            float trackPosition = row.Lane.WorldToLocal(pointerEvent.position).x;
            if (DragController.BeginBar(row.Lane.Model, row.Lane.Geometry, model, hit.BarRegion, trackPosition))
            {
                this.CapturePointer(pointerEvent.pointerId);
            }
        }

        private void OnEmptyLanePointerDown(PointerDownEvent pointerEvent, LiveOpsTimelineHit hit, LaneRow row)
        {
            if (row == null) return;
            pointerEvent.StopPropagation();
            float trackPosition = row.Lane.WorldToLocal(pointerEvent.position).x;
            if (pointerEvent.clickCount == 2 && !row.Lane.Model.IsRecurring)
            {
                // Nhấp đúp chỗ trống làn cố định → popover Thêm đợt, giờ bắt lưới về 00:00 của ngày [SD1 §3.7].
                DateTime day = DateTime.SpecifyKind(row.Lane.Geometry.TimeAt(trackPosition).Date, DateTimeKind.Utc);
                IntentRaised?.Invoke(new AddAtTimeIntent(row.Lane.Model.TypeId, day, null));
                return;
            }
            if (pointerEvent.actionKey && DragController.BeginCreate(row.Lane.Model, row.Lane.Geometry, trackPosition))
            {
                this.CapturePointer(pointerEvent.pointerId);
                return;
            }
            if (_selectedBarKey.Length > 0)
            {
                Select(string.Empty, false);
                IntentRaised?.Invoke(new SelectBarIntent(string.Empty, false, false));
            }
        }

        private void OnPointerMove(PointerMoveEvent pointerEvent)
        {
            if (DragController.IsActive)
            {
                LaneRow row = FindRow(DragController.LaneTypeId);
                if (row == null) return;
                float trackPosition = row.Lane.WorldToLocal(pointerEvent.position).x;
                bool wasDragging = DragController.IsDragging;
                DragController.Move(trackPosition, pointerEvent.altKey);
                if (DragController.IsDragging)
                {
                    if (!wasDragging) UpdateHintLine();
                    RefreshDragVisuals();
                }
                pointerEvent.StopPropagation();
                return;
            }

            Vector2 local = this.WorldToLocal(pointerEvent.position);
            LiveOpsTimelineHit hit = HitTest(local);
            UpdateHover(hit.Kind == LiveOpsTimelineHitKind.Bar ? hit.BarKey : string.Empty);
            bool overTrack = hit.Kind == LiveOpsTimelineHitKind.Bar || hit.Kind == LiveOpsTimelineHitKind.EmptyLane ||
                             (hit.Kind == LiveOpsTimelineHitKind.Ruler && hit.TimeUtc.HasValue);
            _cursorLaneTypeId = hit.Kind == LiveOpsTimelineHitKind.Ruler ? string.Empty : hit.LaneTypeId;
            SetCursor(overTrack ? SnappedTimeAt(Ruler.Track.WorldToLocal(pointerEvent.position).x) : (DateTime?)null);
        }

        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (!DragController.IsActive) return;
            // Kết thúc cử chỉ TRƯỚC khi trả capture: PointerCaptureOutEvent sau đó thấy không còn cử chỉ nên không phát Cancel.
            EndDrag(true);
            if (this.HasPointerCapture(pointerEvent.pointerId)) this.ReleasePointer(pointerEvent.pointerId);
            pointerEvent.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent captureEvent)
        {
            if (DragController.IsActive) EndDrag(false);
        }

        private void OnPointerLeave(PointerLeaveEvent leaveEvent)
        {
            if (DragController.IsActive) return;
            UpdateHover(string.Empty);
            SetCursor(null);
        }

        private void UpdateHover(string barKey)
        {
            if (string.Equals(barKey, _hoverBarKey, StringComparison.Ordinal)) return;
            _hoverBarKey = barKey;
            ApplyBarStates();
            LiveOpsTimelineBar bar = FindBar(barKey);
            HoverChanged?.Invoke(new LiveOpsTimelineHover(barKey, bar != null ? bar.worldBound : Rect.zero));
        }

        /// <summary>
        /// Đặt vạch + bubble giờ tại con trỏ (V-11). <c>internal</c> chứ không <c>private</c> để kịch bản chụp
        /// <c>ht-timeline-day-ruler</c> dựng được tư thế "con trỏ đang ở 12:00" mà không phải giả lập bắt con trỏ trong
        /// batchmode; đường của người dùng vẫn là <c>PointerMoveEvent</c> trên track.
        /// </summary>
        internal void SetCursor(DateTime? cursorUtc)
        {
            _cursorUtc = cursorUtc;
            Ruler.SetCursor(cursorUtc);
            bool visible = cursorUtc.HasValue && _model != null && cursorUtc.Value >= _model.RangeStartUtc && cursorUtc.Value <= _model.RangeEndUtc;
            CursorLine.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !visible);
            if (visible) CursorLine.style.left = _model.Geometry.XOf(cursorUtc.Value); // style-inline-allowed: 6
        }

        private void EndDrag(bool commit)
        {
            if (commit) DragController.Commit();
            else DragController.Cancel();
            ClearDragVisuals();
            UpdateHintLine();
        }

        /// <summary>
        /// Vẽ lại bóng, thanh ma, xem trước chồng giờ và readout theo trạng thái của <see cref="DragController"/>.
        /// <c>internal</c> chứ không <c>private</c> để kịch bản chụp <c>ht-timeline-drag-overlap</c> dựng được khung "đang kéo,
        /// sẽ chồng" từ controller: bắt con trỏ thật không chạy được trong lượt chụp batchmode (đường chuột thật đã có test UI
        /// <c>Drag_WillOverlap_PreviewBeforeRelease</c> phủ).
        /// </summary>
        internal void RefreshDragVisuals()
        {
            if (!DragController.IsDragging || _model == null)
            {
                return;
            }
            LaneRow row = FindRow(DragController.LaneTypeId);
            if (row == null) return;
            LiveOpsTimelineGeometry geometry = row.Lane.Geometry;
            (float left, float width, bool clippedStart, bool clippedEnd) preview = geometry.BarRect(DragController.PreviewStartUtc, DragController.PreviewEndUtc);
            var previewGeometry = new Dictionary<string, (float left, float width)>(StringComparer.Ordinal);
            LiveOpsTimelineBar bar = row.Lane.FindBar(DragController.BarKey);
            float barTop;
            if (bar != null)
            {
                previewGeometry[bar.Model.BarKey] = (preview.left, preview.width);
                // Bóng viền 1px ở chỗ cũ [SD1 §3.5 --dragging]; thanh thật đi theo chuột.
                (float left, float width, bool clippedStart, bool clippedEnd) original = geometry.BarRect(DragController.OriginalStartUtc, DragController.OriginalEndUtc);
                if (_ghost.parent != row.Lane) row.Lane.Add(_ghost);
                _ghost.style.left = original.left; // style-inline-allowed: 4
                _ghost.style.width = original.width; // style-inline-allowed: 4
                _ghost.style.top = bar.Top; // style-inline-allowed: 4
                _ghost.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, false);
                _ghost.SendToBack();
                bar.SetPreviewGeometry(preview.left, preview.width);
                bar.EnableInClassList(LiveOpsHubClassNames.TimelineBarDragging, true);
                barTop = bar.Top;
            }
            else
            {
                // Tạo bằng kéo: thanh ma là chính bóng, vẽ ở khoảng đang kéo trên hàng đầu.
                if (_ghost.parent != row.Lane) row.Lane.Add(_ghost);
                barTop = row.Lane.PaddingTop + LiveOpsTimelineLane.LaneBottomPadding;
                _ghost.style.left = preview.left; // style-inline-allowed: 4
                _ghost.style.width = preview.width; // style-inline-allowed: 4
                _ghost.style.top = barTop; // style-inline-allowed: 4
                _ghost.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, false);
            }
            var willDrop = new HashSet<string>(DragController.WillDropBarKeys, StringComparer.Ordinal);
            row.Lane.SetDragPreview(new List<(DateTime startUtc, DateTime endUtc)>(DragController.PreviewOverlaps), willDrop, previewGeometry);
            PlaceReadout(row, geometry, barTop);
        }

        private void PlaceReadout(LaneRow row, LiveOpsTimelineGeometry geometry, float barTop)
        {
            _readoutText.text = DragController.ReadoutMainText(_format);
            string overlap = DragController.ReadoutOverlapText(_format);
            _readoutOverlap.text = overlap.Length > 0 ? LiveOpsHubStrings.TimelineReadoutSeparator + overlap : string.Empty;
            _readoutOverlap.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, overlap.Length == 0);
            Readout.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, false);

            float readoutWidth = Readout.layout.width;
            if (float.IsNaN(readoutWidth) || readoutWidth <= 0f)
            {
                readoutWidth = (_readoutText.text.Length + _readoutOverlap.text.Length) * LiveOpsTimelineGeometry.LabelCharacterWidth +
                               LiveOpsTimelineGeometry.LabelHorizontalPadding;
            }
            // Bám mép đang kéo, kẹp trong track lề 6px; chạm mép phải thì lật sang trái tay nắm [SD1 §3.7].
            float anchorX = geometry.XOf(DragController.AnchorUtc);
            float left = anchorX;
            if (left + readoutWidth > _trackWidth - ReadoutMargin) left = anchorX - readoutWidth;
            left = Math.Max(ReadoutMargin, Math.Min(left, _trackWidth - ReadoutMargin - readoutWidth));
            float laneTopInOverlay = row.Lane.worldBound.y - _overlay.worldBound.y;
            if (float.IsNaN(laneTopInOverlay)) laneTopInOverlay = 0f;
            float top = Math.Max(0f, laneTopInOverlay + barTop - ReadoutHeight - ReadoutGapAboveBar);
            Readout.style.left = left; // style-inline-allowed: 6
            Readout.style.top = top; // style-inline-allowed: 6
        }

        private void ClearDragVisuals()
        {
            Readout.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, true);
            _ghost.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, true);
            foreach (LaneRow row in _rows)
            {
                if (!row.IsActive) continue;
                row.Lane.ClearDragPreview();
                for (int index = 0; index < row.Lane.BarCount; index++)
                {
                    LiveOpsTimelineBar bar = row.Lane.BarAt(index);
                    if (!bar.ClassListContains(LiveOpsHubClassNames.TimelineBarDragging)) continue;
                    bar.EnableInClassList(LiveOpsHubClassNames.TimelineBarDragging, false);
                    bar.ClearPreviewGeometry();
                }
            }
        }

        // ================================================================================================ phím

        /// <summary>
        /// SP-7b chỉ có MỘT câu trả lời: uỷ quyền sang <see cref="LiveOpsHubShortcuts.IsSingleKeyCommandAllowed"/> (CC-HOSTUI-4).
        /// Bản cũ ở đây không xét <c>ITextEdition.isReadOnly</c> nên ô chữ con của TextField ở 6000.6 lọt lưới.
        /// </summary>
        private static bool IsInsideTextField(IEventHandler target)
        {
            return !LiveOpsHubShortcuts.IsSingleKeyCommandAllowed(target as VisualElement);
        }

        private void OnKeyDown(KeyDownEvent keyEvent)
        {
            if (IsInsideTextField(keyEvent.target)) return;
            _consumeNextHorizontalNavigation = false;
            bool handled = true;
            switch (keyEvent.keyCode)
            {
                case KeyCode.Escape:
                    handled = DragController.IsActive;
                    if (handled)
                    {
                        EndDrag(false);
                        if (panel != null && this.HasPointerCapture(PointerId.mousePointerId)) this.ReleasePointer(PointerId.mousePointerId);
                    }
                    break;
                case KeyCode.A:
                    handled = !keyEvent.actionKey && !keyEvent.shiftKey && !keyEvent.altKey;
                    if (handled) FrameAll();
                    break;
                case KeyCode.Equals:
                case KeyCode.KeypadPlus:
                    handled = StepZoom(-1);
                    break;
                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                    handled = StepZoom(+1);
                    break;
                case KeyCode.LeftArrow:
                case KeyCode.RightArrow:
                    handled = !keyEvent.actionKey && Nudge(keyEvent.keyCode == KeyCode.RightArrow ? 1 : -1, keyEvent.shiftKey, keyEvent.altKey);
                    _consumeNextHorizontalNavigation = handled;
                    break;
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                    handled = keyEvent.actionKey && SelectInAdjacentLane(keyEvent.keyCode == KeyCode.DownArrow ? 1 : -1);
                    _consumeNextHorizontalNavigation = handled;
                    break;
                case KeyCode.Menu:
                    handled = RaiseContextForSelection();
                    break;
                default:
                    handled = false;
                    break;
            }
            if (handled) keyEvent.StopPropagation();
        }

        private void OnNavigationMove(NavigationMoveEvent navigationEvent)
        {
            if (IsInsideTextField(navigationEvent.target)) return;
            switch (navigationEvent.direction)
            {
                case NavigationMoveEvent.Direction.Left:
                case NavigationMoveEvent.Direction.Right:
                case NavigationMoveEvent.Direction.Up:
                case NavigationMoveEvent.Direction.Down:
                    // Mũi tên đã xử lý ở KeyDownEvent sinh thêm NavigationMoveEvent ở cả hai bản [API §12.1] — chặn để không xử lý hai lần.
                    if (_consumeNextHorizontalNavigation)
                    {
                        _consumeNextHorizontalNavigation = false;
                        navigationEvent.StopPropagation();
                    }
                    return;
                case NavigationMoveEvent.Direction.Next:
                case NavigationMoveEvent.Direction.Previous:
                    string nextKey = AdjacentBarKey(navigationEvent.direction == NavigationMoveEvent.Direction.Next ? 1 : -1);
                    // Hết đợt thì không làm gì để focus rời timeline như bình thường — không bẫy focus [FD §5.2].
                    if (nextKey.Length == 0) return;
                    Select(nextKey, true);
                    IntentRaised?.Invoke(new SelectBarIntent(nextKey, false, false));
                    navigationEvent.StopPropagation();
#if UNITY_2023_2_OR_NEWER
                    focusController?.IgnoreEvent(navigationEvent);
#else
                    navigationEvent.PreventDefault();
#endif
                    return;
            }
        }

        private bool StepZoom(int direction)
        {
            int next = (int)_zoom + direction;
            if (next < (int)LiveOpsTimelineZoom.Day || next > (int)LiveOpsTimelineZoom.Month)
            {
                if (!IsContinuousScale) return false;
                next = (int)_zoom;
            }
            SetRange(_rangeStartUtc, (LiveOpsTimelineZoom)next);
            return true;
        }

        /// <summary>← → nhích theo lưới; Shift 1 ngày; Alt chỉ mép cuối; Alt Shift chỉ mép đầu [FD §5.2]. Một lần nhấn = một Commit.</summary>
        private bool Nudge(int direction, bool shift, bool alt)
        {
            LiveOpsTimelineBar bar = FindBar(_selectedBarKey);
            if (bar == null || !bar.IsDraggable) return false;
            LiveOpsTimelineBarModel model = bar.Model;
            bool startEdgeOnly = alt && shift;
            bool endEdgeOnly = alt && !shift;
            if (model.IsRunning && !endEdgeOnly) return true;
            TimeSpan step = shift && !alt ? TimeSpan.FromDays(1) : LiveOpsTimelineGeometry.AutoSnapStep(_pixelsPerHour);
            long offset = step.Ticks * direction;
            DateTime start = model.StartUtc;
            DateTime end = model.EndUtc;
            if (startEdgeOnly)
            {
                start = LiveOpsTimelineGeometry.AddTicksClamped(start, offset);
                if (start >= end) return true;
            }
            else if (endEdgeOnly)
            {
                end = LiveOpsTimelineGeometry.AddTicksClamped(end, offset);
                if (end <= start) return true;
            }
            else
            {
                start = LiveOpsTimelineGeometry.AddTicksClamped(start, offset);
                end = LiveOpsTimelineGeometry.AddTicksClamped(end, offset);
            }
            IntentRaised?.Invoke(new MoveBarIntent(model.BarKey, start, end, LiveOpsTimelineGesturePhase.Commit));
            return true;
        }

        private List<LiveOpsTimelineBarModel> OrderedBars()
        {
            var bars = new List<LiveOpsTimelineBarModel>();
            if (_model == null) return bars;
            foreach (LiveOpsTimelineLaneModel lane in _model.Lanes)
            {
                var laneBars = new List<LiveOpsTimelineBarModel>();
                foreach (LiveOpsTimelineBarModel bar in lane.Bars)
                {
                    if (!bar.IsStrip) laneBars.Add(bar);
                }
                laneBars.Sort((left, right) => left.StartUtc != right.StartUtc ? left.StartUtc.CompareTo(right.StartUtc) : left.RowIndex.CompareTo(right.RowIndex));
                bars.AddRange(laneBars);
            }
            return bars;
        }

        private string AdjacentBarKey(int direction)
        {
            List<LiveOpsTimelineBarModel> bars = OrderedBars();
            if (bars.Count == 0) return string.Empty;
            int current = -1;
            for (int index = 0; index < bars.Count; index++)
            {
                if (string.Equals(bars[index].BarKey, _selectedBarKey, StringComparison.Ordinal)) current = index;
            }
            int next = current < 0 ? (direction > 0 ? 0 : bars.Count - 1) : current + direction;
            return next >= 0 && next < bars.Count ? bars[next].BarKey : string.Empty;
        }

        /// <summary>⌘/Ctrl ↑ ↓: sang làn trên/dưới, chọn đợt gần giờ bắt đầu của đợt đang chọn (hoặc con trỏ) nhất.</summary>
        private bool SelectInAdjacentLane(int direction)
        {
            if (_model == null || _model.Lanes.Count == 0) return false;
            LiveOpsTimelineBar selected = FindBar(_selectedBarKey);
            int laneIndex = -1;
            for (int index = 0; index < _model.Lanes.Count && selected != null; index++)
            {
                if (string.Equals(_model.Lanes[index].TypeId, selected.Model.EventType, StringComparison.Ordinal)) laneIndex = index;
            }
            DateTime reference = selected != null ? selected.Model.StartUtc : _cursorUtc ?? _model.NowUtc;
            for (int laneStep = laneIndex + direction; laneStep >= 0 && laneStep < _model.Lanes.Count; laneStep += direction)
            {
                LiveOpsTimelineBarModel nearest = null;
                double nearestDistance = double.MaxValue;
                foreach (LiveOpsTimelineBarModel bar in _model.Lanes[laneStep].Bars)
                {
                    if (bar.IsStrip) continue;
                    double distance = Math.Abs((bar.StartUtc - reference).TotalHours);
                    if (distance < nearestDistance)
                    {
                        nearest = bar;
                        nearestDistance = distance;
                    }
                }
                if (nearest == null) continue;
                Select(nearest.BarKey, true);
                IntentRaised?.Invoke(new SelectBarIntent(nearest.BarKey, false, false));
                return true;
            }
            return true;
        }

        private bool RaiseContextForSelection()
        {
            LiveOpsTimelineBar bar = FindBar(_selectedBarKey);
            if (bar == null) return false;
            Vector2 center = bar.worldBound.center;
            LiveOpsTimelineHit hit = new LiveOpsTimelineHit(LiveOpsTimelineHitKind.Bar, bar.Model.BarKey, LiveOpsTimelineBarRegion.Body,
                bar.Model.EventType, null, this.WorldToLocal(center));
            ContextRequested?.Invoke(new LiveOpsTimelineContextRequest(hit, center));
            return true;
        }

        private void OnFocusOut(FocusOutEvent focusEvent)
        {
            // Focus chuyển sang element con của timeline (vd ô trong chip) vẫn tính là timeline có focus.
            VisualElement next = focusEvent.relatedTarget as VisualElement;
            bool stillInside = next != null && (next == this || Contains(next));
            EnableInClassList(LiveOpsHubClassNames.TimelineHasFocus, stillInside);
        }

        // ================================================================================================ lệnh Edit (R-15)

        internal const string CopyCommand = "Copy";
        internal const string PasteCommand = "Paste";
        internal const string DuplicateCommand = "Duplicate";
        internal const string SoftDeleteCommand = "SoftDelete";
        internal const string DeleteCommand = "Delete";
        internal const string FrameSelectedCommand = "FrameSelected";

        /// <summary>Lệnh có hợp lệ lúc này không — Validate trả lời bằng đúng hàm này để Edit menu bật/tắt khớp Execute.</summary>
        internal bool CanExecuteCommand(string commandName)
        {
            LiveOpsTimelineBar selected = FindBar(_selectedBarKey);
            bool hasFixedSelection = selected != null && selected.Model.Source == LiveOpsTimelineBarSource.Fixed;
            switch (commandName)
            {
                case CopyCommand:
                case DuplicateCommand:
                case SoftDeleteCommand:
                case DeleteCommand:
                    return hasFixedSelection;
                case FrameSelectedCommand:
                    return selected != null;
                case PasteCommand:
                    LaneRow row = FindRow(_cursorLaneTypeId);
                    return HasCopiedEvent && _cursorUtc.HasValue && row != null && !row.Lane.Model.IsRecurring;
                default:
                    return false;
            }
        }

        private void OnValidateCommand(ValidateCommandEvent commandEvent)
        {
            if (!CanExecuteCommand(commandEvent.commandName)) return;
            commandEvent.StopPropagation();
            commandEvent.imguiEvent?.Use();
        }

        private void OnExecuteCommand(ExecuteCommandEvent commandEvent)
        {
            if (!CanExecuteCommand(commandEvent.commandName)) return;
            LiveOpsTimelineBar selected = FindBar(_selectedBarKey);
            switch (commandEvent.commandName)
            {
                case CopyCommand:
                    IntentRaised?.Invoke(new CopyBarIntent(selected.Model.BarKey));
                    break;
                case DuplicateCommand:
                    IntentRaised?.Invoke(new DuplicateBarIntent(selected.Model.BarKey));
                    break;
                case SoftDeleteCommand:
                case DeleteCommand:
                    IntentRaised?.Invoke(new DeleteBarIntent(selected.Model.BarKey));
                    break;
                case FrameSelectedCommand:
                    FrameInstance(selected.Model.EventType, selected.Model.StartUtc, selected.Model.EndUtc);
                    break;
                case PasteCommand:
                    IntentRaised?.Invoke(new PasteAtTimeIntent(_cursorLaneTypeId, _cursorUtc.Value));
                    break;
            }
            commandEvent.StopPropagation();
            commandEvent.imguiEvent?.Use();
        }

        private sealed class LaneRow
        {
            public LaneRow(VisualElement row, LiveOpsTimelineLaneHeader header, LiveOpsTimelineLane lane)
            {
                Row = row;
                Header = header;
                Lane = lane;
            }

            public VisualElement Row { get; }
            public LiveOpsTimelineLaneHeader Header { get; }
            public LiveOpsTimelineLane Lane { get; }
            public bool IsActive { get; set; }
        }

#if !UNITY_2023_2_OR_NEWER
        public new sealed class UxmlFactory : UxmlFactory<LiveOpsTimelineElement, UxmlTraits>
        {
        }

        public new sealed class UxmlTraits : VisualElement.UxmlTraits
        {
            // Tên thuộc tính trùng tên kebab-case mà [UxmlAttribute] của Unity 6 sinh từ property (ZoomLevel → "zoom-level").
            private readonly UxmlIntAttributeDescription _zoomLevel = new UxmlIntAttributeDescription
            {
                name = "zoom-level",
                defaultValue = (int)LiveOpsTimelineZoom.ThreeWeeks,
            };

            public override void Init(VisualElement visualElement, IUxmlAttributes bag, CreationContext context)
            {
                base.Init(visualElement, bag, context);
                ((LiveOpsTimelineElement)visualElement).ZoomLevel = _zoomLevel.GetValueFromBag(bag, context);
            }
        }
#endif
    }
}
