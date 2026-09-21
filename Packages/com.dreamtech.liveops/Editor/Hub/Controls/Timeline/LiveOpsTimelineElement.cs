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
        private readonly VisualElement _readoutQuickCheck;
        private readonly LiveOpsStateMark _readoutQuickCheckMark;
        private readonly Label _readoutQuickCheckText;

        /// <summary>(R-03) Quãng ngang bị thanh chiếm trong dải dọc của readout — dùng lại giữa các lần đặt, không cấp phát mỗi khung kéo.</summary>
        private readonly List<(float left, float right)> _readoutBlockedSpans = new List<(float left, float right)>();

        private readonly List<string> _selectedBarKeys = new List<string>();
        private readonly VisualElement _marquee;

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
        private bool _cursorOnEmptyLane;
        private string _quickCheckTagText = string.Empty;
        private HealthState _quickCheckTagHealth = HealthState.Ok;
        private bool _marqueeActive;
        private bool _marqueePassedThreshold;
        private Vector2 _marqueeOriginInOverlay;
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
            // (nợ D-3(a), Hình 12 khung 5) Vế thứ ba: tag kiểm nhanh của làn đang kéo. Câu đã được presenter tính xong ở mỗi
            // bước xem trước; thiếu chỗ VẼ thì người kéo không bao giờ biết mình vừa kéo hết chồng giờ hay chưa.
            _readoutQuickCheck = new VisualElement { pickingMode = PickingMode.Ignore };
            _readoutQuickCheck.AddToClassList(LiveOpsHubClassNames.TimelineReadoutQuickCheck);
            _readoutQuickCheck.AddToClassList(LiveOpsHubClassNames.TimelineHidden);
            Label quickCheckSeparator = new Label(LiveOpsHubStrings.TimelineReadoutSeparator) { pickingMode = PickingMode.Ignore };
            quickCheckSeparator.AddToClassList(LiveOpsHubClassNames.TimelineReadoutQuickCheckText);
            _readoutQuickCheck.Add(quickCheckSeparator);
            _readoutQuickCheckMark = new LiveOpsStateMark { pickingMode = PickingMode.Ignore, Size = LiveOpsStateMark.MarkSize.Small };
            _readoutQuickCheck.Add(_readoutQuickCheckMark);
            _readoutQuickCheckText = new Label { pickingMode = PickingMode.Ignore };
            _readoutQuickCheckText.AddToClassList(LiveOpsHubClassNames.TimelineReadoutQuickCheckText);
            _readoutQuickCheck.Add(_readoutQuickCheckText);
            Readout.Add(_readoutQuickCheck);
            _overlay.Add(Readout);

            // (Hình 12 khung 9) Khung chọn nằm trong lớp phủ, không trong làn: nó cắt ngang nhiều làn nên không thuộc làn nào cả.
            _marquee = new VisualElement { pickingMode = PickingMode.Ignore };
            _marquee.AddToClassList(LiveOpsHubClassNames.TimelineMarquee);
            _marquee.AddToClassList(LiveOpsHubClassNames.TimelineHidden);
            // Nền mờ 0,2 là một con riêng: `opacity` trong USS nhuộm cả element, nên nền và viền phải nằm ở hai element khác
            // nhau thì viền 1px mới còn đục đúng như [SD1 §3.8 khung 9].
            VisualElement marqueeFill = new VisualElement { pickingMode = PickingMode.Ignore };
            marqueeFill.AddToClassList(LiveOpsHubClassNames.TimelineMarqueeFill);
            _marquee.Add(marqueeFill);
            _overlay.Add(_marquee);

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

        /// <summary>
        /// (G-OPT-TIMELINE, Hình 12 khung 9) Cả TẬP thanh đang chọn, theo thứ tự đã chọn; <see cref="SelectedBarKey"/> là thanh
        /// chạm sau cùng (neo của Shift-click và mốc của inspector). Một thanh = tập một phần tử, nên nơi gọi cũ không đổi gì.
        /// </summary>
        public IReadOnlyList<string> SelectedBarKeys => _selectedBarKeys;

        /// <summary>Số thanh đang chọn — dùng cho dòng gợi ý và inspector; 0 khi không chọn gì.</summary>
        public int SelectionCount => _selectedBarKeys.Count;

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
            _selectedBarKeys.Clear();
            if (_selectedBarKey.Length > 0) _selectedBarKeys.Add(_selectedBarKey);
            _keyboardFocusBarKey = focus ? _selectedBarKey : string.Empty;
            if (focus) Focus();
            ApplyBarStates();
            UpdateHintLine();
        }

        /// <summary>
        /// (Hình 12 khung 9) Đặt cả tập chọn. <paramref name="primaryBarKey"/> là thanh chạm sau cùng; không nằm trong tập thì
        /// lấy phần tử cuối, tập rỗng thì bỏ chọn. Không phát intent — nơi gọi (chuột, presenter) tự phát.
        /// </summary>
        internal void SelectMany(IReadOnlyList<string> barKeys, string primaryBarKey, bool focus)
        {
            _selectedBarKeys.Clear();
            if (barKeys != null)
            {
                for (int index = 0; index < barKeys.Count; index++)
                {
                    string barKey = barKeys[index];
                    if (!string.IsNullOrEmpty(barKey) && !_selectedBarKeys.Contains(barKey)) _selectedBarKeys.Add(barKey);
                }
            }
            string primary = primaryBarKey ?? string.Empty;
            if (primary.Length == 0 || !_selectedBarKeys.Contains(primary))
            {
                primary = _selectedBarKeys.Count > 0 ? _selectedBarKeys[_selectedBarKeys.Count - 1] : string.Empty;
            }
            _selectedBarKey = primary;
            _keyboardFocusBarKey = focus ? primary : string.Empty;
            if (focus) Focus();
            ApplyBarStates();
            UpdateHintLine();
        }

        internal bool IsSelected(string barKey)
        {
            return !string.IsNullOrEmpty(barKey) && _selectedBarKeys.Contains(barKey);
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

        /// <summary>(Hình 12 khung 12) Mục menu "Thu gọn / Mở làn" của header làn — presenter giữ danh sách làn thu gọn.</summary>
        internal void RequestToggleLaneCollapsed(string typeId, bool collapsed)
        {
            if (string.IsNullOrEmpty(typeId)) return;
            IntentRaised?.Invoke(new ToggleLaneCollapsedIntent(typeId, collapsed));
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

        /// <summary>(Hình 12 khung 9) Khung chọn đang kéo — test và kịch bản chụp đọc lại đúng hình đã vẽ.</summary>
        internal VisualElement Marquee => _marquee;
        internal Label ReadoutText => _readoutText;
        internal Label ReadoutOverlap => _readoutOverlap;

        /// <summary>(nợ D-3(a)) Vế tag kiểm nhanh của readout — ẩn khi presenter chưa gửi câu nào.</summary>
        internal VisualElement ReadoutQuickCheck => _readoutQuickCheck;
        internal Label ReadoutQuickCheckText => _readoutQuickCheckText;
        internal LiveOpsStateMark ReadoutQuickCheckMark => _readoutQuickCheckMark;
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

        /// <summary>(R-01) Hàng của làn (header + track) — test đo được nền làn có phủ hết hàng không khi header cao hơn làn.</summary>
        internal VisualElement RowAt(int index) => _rows[index].Row;

        internal VisualElement FindRowElement(string typeId) => FindRow(typeId)?.Row;

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
                range = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.TimelineStripTooltipFormat), bar.EventId, bar.StripCount,
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
            // (UX-13, UJ-08) Chevron của header đi cùng đường với mục menu "Thu gọn / Mở làn" — một lệnh, hai lối vào.
            header.CollapseToggleClicked += (typeId, collapsed) => RequestToggleLaneCollapsed(typeId, collapsed);
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
                    bar.SetInteractionState(hover, IsSelected(key),
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
            // (nợ D-3(b)) Con trỏ đứng trên chỗ trống của một làn: gợi ý nói về CHỖ ĐANG TRỎ TỚI, không phải về đợt đã chọn từ
            // trước — người dùng đang hỏi "ở đây làm được gì", và câu trả lời là nhấp đúp.
            if (_cursorOnEmptyLane)
            {
                LiveOpsTimelineLaneModel cursorLane = FindLaneModel(_cursorLaneTypeId);
                if (cursorLane != null && !cursorLane.IsRecurring)
                {
                    HintLine.ShowEmptyLane(cursorLane.TypeId, RangeTextForHint());
                    return;
                }
            }
            // (Hình 12 khung 9) Chọn nhiều: không có "đợt đang chọn" nào để nêu tên, nên gợi ý nói SỐ đợt, làn và hai phím làm
            // ra tập đó — đúng câu trả lời cho "tôi đang giữ cái gì trong tay".
            if (_selectedBarKeys.Count > 1)
            {
                HintLine.ShowMultiSelected(_selectedBarKeys.Count, MultiSelectionLaneText());
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

        /// <summary>Tên làn cho dòng gợi ý chọn nhiều: một loại thì nêu tên loại, nhiều loại thì nêu số loại.</summary>
        private string MultiSelectionLaneText()
        {
            var laneTypeIds = new List<string>();
            for (int index = 0; index < _selectedBarKeys.Count; index++)
            {
                LiveOpsTimelineBar bar = FindBar(_selectedBarKeys[index]);
                if (bar == null) continue;
                string typeId = bar.Model.EventType;
                if (typeId.Length > 0 && !laneTypeIds.Contains(typeId)) laneTypeIds.Add(typeId);
            }
            if (laneTypeIds.Count == 1) return laneTypeIds[0];
            return LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.TimelineMultiSelectMixedTypesFormat),
                laneTypeIds.Count);
        }

        private LiveOpsTimelineLaneModel FindLaneModel(string typeId)
        {
            LaneRow row = FindRow(typeId);
            return row?.Lane.Model;
        }

        /// <summary>
        /// Khoảng đang xem cho dòng gợi ý làn trống — dạng ngắn "14/9 → 19/9" đúng [SD1 §3.8 khung 13]. Ngày/tháng đi qua
        /// <see cref="LiveOpsHubFormat.DayMonthText"/>: thứ tự ngày/tháng và dấu phân cách là quyết định định dạng của CẢ hub
        /// [FD §6.1], ghép tay ở đây là khoá cứng nó ngoài tầm bản dịch.
        /// </summary>
        private string RangeTextForHint()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineHintRangeFormat,
                LiveOpsHubFormat.DayMonthText(_rangeStartUtc), LiveOpsHubFormat.DayMonthText(RangeEndUtc));
        }

        private void RaiseRangeChanged()
        {
            RangeChanged?.Invoke(_rangeStartUtc, RangeEndUtc);
        }

        /// <summary>
        /// (nợ D-3(c)) Giờ tại con trỏ, bắt theo BƯỚC LƯỚI CỦA KHUNG NHÌN (menu "Bắt lưới" của toolbar), không theo bước tự động
        /// của zoom. Giờ này đi thẳng vào nhãn menu "Thêm đợt bắt đầu … UTC…" và vào <c>PasteAtTimeIntent</c>, nên bắt bằng một
        /// bước khác với lúc kéo là hai đường cùng một thao tác cho ra hai giờ.
        /// "Tắt" (bước ≤ 0) = không bắt lưới gì cả.
        /// </summary>
        private DateTime SnappedTimeAt(float trackPosition)
        {
            LiveOpsTimelineGeometry geometry = CurrentGeometry();
            DateTime timeUtc = geometry.TimeAt(trackPosition);
            TimeSpan step = DragController.EffectiveStepFor(geometry.PixelsPerHour);
            return step <= TimeSpan.Zero ? timeUtc : LiveOpsTimelineGeometry.Snap(timeUtc, step);
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
            // (Hình 12 khung 9) ⌘/Ctrl bật tắt từng thanh, Shift chọn dải trong làn. Hai cử chỉ này KHÔNG mở cử chỉ kéo: người
            // dùng đang gom tập chọn, một cú run tay 4px không được dời đợt vừa thêm vào tập.
            if (pointerEvent.actionKey || pointerEvent.shiftKey)
            {
                List<string> nextSelection = pointerEvent.actionKey
                    ? ToggledSelection(model.BarKey)
                    : RangeSelection(row.Lane.Model, model.BarKey);
                SelectMany(nextSelection, model.BarKey, false);
                IntentRaised?.Invoke(new SelectManyIntent(nextSelection, model.BarKey));
                return;
            }
            Select(model.BarKey, false);
            IntentRaised?.Invoke(new SelectBarIntent(model.BarKey));
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
            // (Hình 12 khung 9) Kéo trên chỗ trống = khung chọn. Mở cử chỉ ngay từ lúc nhấn nhưng chỉ VẼ sau ngưỡng 4px, để một
            // cú bấm thường vẫn là bỏ chọn như trước (xử lý ở nhánh nhả chuột).
            BeginMarquee(pointerEvent.position);
            this.CapturePointer(pointerEvent.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent pointerEvent)
        {
            if (DragController.IsActive)
            {
                LaneRow row = FindRow(DragController.LaneTypeId);
                if (row == null) return;
                float trackPosition = row.Lane.WorldToLocal(pointerEvent.position).x;
                bool wasDragging = DragController.IsDragging;
                // Shift ở ĐÂY (bước di chuột) chứ không ở lúc nhấn: Shift lúc nhấn là chọn dải, Shift giữa chừng là kéo theo đợt sau.
                DragController.Move(trackPosition, pointerEvent.altKey, pointerEvent.shiftKey);
                if (DragController.IsDragging)
                {
                    if (!wasDragging) UpdateHintLine();
                    RefreshDragVisuals();
                }
                pointerEvent.StopPropagation();
                return;
            }

            if (_marqueeActive)
            {
                UpdateMarquee(pointerEvent.position);
                pointerEvent.StopPropagation();
                return;
            }

            Vector2 local = this.WorldToLocal(pointerEvent.position);
            LiveOpsTimelineHit hit = HitTest(local);
            UpdateHover(hit.Kind == LiveOpsTimelineHitKind.Bar ? hit.BarKey : string.Empty);
            bool overTrack = hit.Kind == LiveOpsTimelineHitKind.Bar || hit.Kind == LiveOpsTimelineHitKind.EmptyLane ||
                             (hit.Kind == LiveOpsTimelineHitKind.Ruler && hit.TimeUtc.HasValue);
            SetCursor(overTrack ? SnappedTimeAt(Ruler.Track.WorldToLocal(pointerEvent.position).x) : (DateTime?)null,
                hit.Kind == LiveOpsTimelineHitKind.Ruler ? string.Empty : hit.LaneTypeId,
                hit.Kind == LiveOpsTimelineHitKind.EmptyLane);
        }

        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (_marqueeActive)
            {
                EndMarquee(true, pointerEvent.position);
                if (this.HasPointerCapture(pointerEvent.pointerId)) this.ReleasePointer(pointerEvent.pointerId);
                pointerEvent.StopPropagation();
                return;
            }
            if (!DragController.IsActive) return;
            // Kết thúc cử chỉ TRƯỚC khi trả capture: PointerCaptureOutEvent sau đó thấy không còn cử chỉ nên không phát Cancel.
            EndDrag(true);
            if (this.HasPointerCapture(pointerEvent.pointerId)) this.ReleasePointer(pointerEvent.pointerId);
            pointerEvent.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent captureEvent)
        {
            if (_marqueeActive) EndMarquee(false, Vector2.zero);
            if (DragController.IsActive) EndDrag(false);
        }

        // ------------------------------------------------------------------------------------ khung chọn (Hình 12 khung 9)

        private void BeginMarquee(Vector2 worldPosition)
        {
            _marqueeActive = true;
            _marqueePassedThreshold = false;
            _marqueeOriginInOverlay = _overlay.WorldToLocal(worldPosition);
        }

        private void UpdateMarquee(Vector2 worldPosition)
        {
            Vector2 current = _overlay.WorldToLocal(worldPosition);
            float width = Math.Abs(current.x - _marqueeOriginInOverlay.x);
            float height = Math.Abs(current.y - _marqueeOriginInOverlay.y);
            if (!_marqueePassedThreshold && width < LiveOpsTimelineDragController.DragThreshold
                && height < LiveOpsTimelineDragController.DragThreshold)
            {
                return;
            }
            _marqueePassedThreshold = true;
            _marquee.style.left = Math.Min(_marqueeOriginInOverlay.x, current.x); // style-inline-allowed: 4
            _marquee.style.top = Math.Min(_marqueeOriginInOverlay.y, current.y); // style-inline-allowed: 4
            _marquee.style.width = width; // style-inline-allowed: 4
            _marquee.style.height = height; // style-inline-allowed: 4
            _marquee.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, false);
        }

        /// <summary>
        /// Thả khung chọn: chưa qua ngưỡng 4px thì đây là một cú bấm chỗ trống = bỏ chọn (hành vi W4 giữ nguyên); qua ngưỡng thì
        /// chọn mọi thanh CHẠM khung. Dải gom không vào tập — nó không ánh xạ ra một đợt nào (V-22 CC-TLMODEL-1).
        /// </summary>
        /// <param name="commit">false = mất capture giữa chừng, khung tắt mà không đổi tập chọn.</param>
        /// <param name="worldPosition">Vị trí con trỏ của CHÍNH sự kiện nhả chuột; bỏ qua khi <paramref name="commit"/> false.</param>
        private void EndMarquee(bool commit, Vector2 worldPosition)
        {
            bool passedThreshold = _marqueePassedThreshold;
            _marqueeActive = false;
            _marqueePassedThreshold = false;
            _marquee.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, true);
            if (!commit) return;
            if (!passedThreshold)
            {
                if (_selectedBarKeys.Count == 0) return;
                Select(string.Empty, false);
                IntentRaised?.Invoke(new SelectBarIntent(string.Empty));
                return;
            }
            List<string> inside = BarKeysInMarquee(MarqueeWorldRect(worldPosition));
            SelectMany(inside, inside.Count > 0 ? inside[inside.Count - 1] : string.Empty, false);
            IntentRaised?.Invoke(new SelectManyIntent(inside, SelectedBarKey));
        }

        /// <summary>
        /// Hình world của khung chọn, dựng từ điểm neo và VỊ TRÍ CON TRỎ của sự kiện nhả chuột — không đọc
        /// <c>_marquee.worldBound</c>. <c>worldBound</c> là kết quả của lần layout gần nhất: nếu PointerUp được bơm cùng lượt
        /// với PointerMove cuối (chuyện thường gặp khi chuột đi nhanh, và là cách test bơm sự kiện), layout chưa chạy lại nên
        /// tập chọn sẽ là tập của bước kéo TRƯỚC đó.
        /// </summary>
        private Rect MarqueeWorldRect(Vector2 worldPosition)
        {
            Vector2 current = _overlay.WorldToLocal(worldPosition);
            Vector2 minimum = new Vector2(Math.Min(_marqueeOriginInOverlay.x, current.x),
                Math.Min(_marqueeOriginInOverlay.y, current.y));
            Vector2 maximum = new Vector2(Math.Max(_marqueeOriginInOverlay.x, current.x),
                Math.Max(_marqueeOriginInOverlay.y, current.y));
            Vector2 worldMinimum = _overlay.LocalToWorld(minimum);
            Vector2 worldMaximum = _overlay.LocalToWorld(maximum);
            return new Rect(worldMinimum, worldMaximum - worldMinimum);
        }

        /// <summary>
        /// (Hình 12 khung 9) Vẽ khung chọn BAO quanh một tập thanh. <c>internal</c> chứ không <c>private</c> vì lượt chụp
        /// batchmode không bắt được con trỏ thật (cùng lý do với <see cref="RefreshDragVisuals"/> và <see cref="SetCursor"/>):
        /// đường của người dùng vẫn là kéo chuột trên chỗ trống, đường này chỉ dựng lại đúng hình mà cú kéo đó để lại.
        /// </summary>
        /// <param name="barKeys">Thanh phải nằm trong khung; rỗng hoặc không tìm thấy thanh nào thì khung tắt.</param>
        /// <param name="margin">Lề quanh tập thanh, tính bằng pixel.</param>
        internal void ShowMarqueeAroundBars(IReadOnlyList<string> barKeys, float margin)
        {
            bool hasBar = false;
            float left = 0f;
            float top = 0f;
            float right = 0f;
            float bottom = 0f;
            for (int index = 0; barKeys != null && index < barKeys.Count; index++)
            {
                LiveOpsTimelineBar bar = FindBar(barKeys[index]);
                if (bar == null) continue;
                Rect world = bar.worldBound;
                Vector2 minimum = _overlay.WorldToLocal(new Vector2(world.xMin, world.yMin));
                Vector2 maximum = _overlay.WorldToLocal(new Vector2(world.xMax, world.yMax));
                if (!hasBar)
                {
                    left = minimum.x;
                    top = minimum.y;
                    right = maximum.x;
                    bottom = maximum.y;
                    hasBar = true;
                    continue;
                }
                left = Math.Min(left, minimum.x);
                top = Math.Min(top, minimum.y);
                right = Math.Max(right, maximum.x);
                bottom = Math.Max(bottom, maximum.y);
            }
            _marquee.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !hasBar);
            if (!hasBar) return;
            _marquee.style.left = left - margin; // style-inline-allowed: 4
            _marquee.style.top = top - margin; // style-inline-allowed: 4
            _marquee.style.width = right - left + margin * 2f; // style-inline-allowed: 4
            _marquee.style.height = bottom - top + margin * 2f; // style-inline-allowed: 4
        }

        private List<string> BarKeysInMarquee(Rect marqueeWorld)
        {
            var keys = new List<string>();
            foreach (LaneRow row in _rows)
            {
                if (!row.IsActive) continue;
                for (int index = 0; index < row.Lane.BarCount; index++)
                {
                    LiveOpsTimelineBar bar = row.Lane.BarAt(index);
                    if (bar.Model == null || bar.Model.IsStrip) continue;
                    if (bar.ClassListContains(LiveOpsHubClassNames.TimelineHidden)) continue;
                    if (!marqueeWorld.Overlaps(bar.worldBound)) continue;
                    if (!keys.Contains(bar.Model.BarKey)) keys.Add(bar.Model.BarKey);
                }
            }
            return keys;
        }

        /// <summary>⌘/Ctrl-click: thanh đã có trong tập thì bỏ ra, chưa có thì thêm vào cuối.</summary>
        private List<string> ToggledSelection(string barKey)
        {
            var next = new List<string>(_selectedBarKeys);
            if (!next.Remove(barKey)) next.Add(barKey);
            return next;
        }

        /// <summary>
        /// Shift-click: chọn DẢI trong cùng một làn, từ thanh neo (thanh chạm gần nhất) tới thanh vừa bấm, theo thứ tự thời gian.
        /// Neo không nằm trong làn này (hoặc chưa chọn gì) thì dải rút về đúng một thanh — không nối ngang qua hai làn, vì hai
        /// làn là hai loại event và "dải" giữa chúng không có nghĩa nào cả.
        /// </summary>
        private List<string> RangeSelection(LiveOpsTimelineLaneModel lane, string barKey)
        {
            var ordered = new List<LiveOpsTimelineBarModel>();
            IReadOnlyList<LiveOpsTimelineBarModel> bars = lane.Bars;
            for (int index = 0; index < bars.Count; index++)
            {
                if (!bars[index].IsStrip) ordered.Add(bars[index]);
            }
            ordered.Sort((left, right) => left.StartUtc.CompareTo(right.StartUtc));
            int anchorIndex = -1;
            int clickedIndex = -1;
            for (int index = 0; index < ordered.Count; index++)
            {
                if (string.Equals(ordered[index].BarKey, _selectedBarKey, StringComparison.Ordinal)) anchorIndex = index;
                if (string.Equals(ordered[index].BarKey, barKey, StringComparison.Ordinal)) clickedIndex = index;
            }
            if (clickedIndex < 0) return new List<string> { barKey };
            if (anchorIndex < 0) anchorIndex = clickedIndex;
            int first = Math.Min(anchorIndex, clickedIndex);
            int last = Math.Max(anchorIndex, clickedIndex);
            var keys = new List<string>();
            for (int index = first; index <= last; index++) keys.Add(ordered[index].BarKey);
            return keys;
        }

        private void OnPointerLeave(PointerLeaveEvent leaveEvent)
        {
            if (DragController.IsActive) return;
            UpdateHover(string.Empty);
            SetCursor(null, string.Empty, false);
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
            SetCursor(cursorUtc, _cursorLaneTypeId, _cursorOnEmptyLane);
        }

        /// <summary>
        /// (nợ D-3(b)) Như trên nhưng nói rõ con trỏ đang ở làn nào và có phải CHỖ TRỐNG của làn đó không — dòng gợi ý cần cả
        /// hai để in "star-tournament · 14/9 → 19/9 · nhấp đúp chỗ trống để thêm đợt" (Hình 12 khung 13).
        /// </summary>
        internal void SetCursor(DateTime? cursorUtc, string laneTypeId, bool isEmptyLane)
        {
            _cursorLaneTypeId = laneTypeId ?? string.Empty;
            _cursorOnEmptyLane = isEmptyLane && cursorUtc.HasValue && _cursorLaneTypeId.Length > 0;
            _cursorUtc = cursorUtc;
            Ruler.SetCursor(cursorUtc);
            bool visible = cursorUtc.HasValue && _model != null && cursorUtc.Value >= _model.RangeStartUtc && cursorUtc.Value <= _model.RangeEndUtc;
            CursorLine.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !visible);
            if (visible) CursorLine.style.left = _model.Geometry.XOf(cursorUtc.Value); // style-inline-allowed: 6
            UpdateHintLine();
        }

        /// <summary>
        /// (nợ D-3(a)) Câu tag kiểm nhanh của lần xem trước gần nhất; "" = không có tag. Presenter gọi trong nhánh Preview, ngay
        /// trước khi element vẽ lại readout, nên tag và giờ trên readout luôn là của CÙNG một bước kéo.
        /// </summary>
        internal void SetDragQuickCheckTag(string text, HealthState health)
        {
            _quickCheckTagText = text ?? string.Empty;
            _quickCheckTagHealth = health;
            ApplyQuickCheckTag();
        }

        private void ApplyQuickCheckTag()
        {
            bool hasTag = _quickCheckTagText.Length > 0;
            _readoutQuickCheckText.text = _quickCheckTagText;
            _readoutQuickCheckMark.SetHealth(_quickCheckTagHealth);
            _readoutQuickCheck.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, !hasTag);
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
            // (G-OPT-TIMELINE) Shift giữa chừng: đợt phía sau đi theo. Vẽ chúng bằng CÙNG đường translate với thanh đang kéo và
            // gắn class --dragging, nên ClearDragVisuals trả tất cả về chỗ cũ mà không cần biết có bao nhiêu đợt đã đi theo.
            IReadOnlyList<LiveOpsTimelineBarMove> followers = DragController.FollowerMoves;
            for (int index = 0; index < followers.Count; index++)
            {
                LiveOpsTimelineBarMove follower = followers[index];
                LiveOpsTimelineBar followerBar = row.Lane.FindBar(follower.BarKey);
                if (followerBar == null) continue;
                (float left, float width, bool clippedStart, bool clippedEnd) followerRect =
                    geometry.BarRect(follower.NewStartUtc, follower.NewEndUtc);
                previewGeometry[follower.BarKey] = (followerRect.left, followerRect.width);
                followerBar.SetPreviewGeometry(followerRect.left, followerRect.width);
                followerBar.EnableInClassList(LiveOpsHubClassNames.TimelineBarDragging, true);
            }
            var willDrop = new HashSet<string>(DragController.WillDropBarKeys, StringComparer.Ordinal);
            row.Lane.SetDragPreview(new List<(DateTime startUtc, DateTime endUtc)>(DragController.PreviewOverlaps), willDrop, previewGeometry);
            PlaceReadout(row, geometry, barTop, previewGeometry);
        }

        /// <summary>
        /// Chỗ đặt readout khi kéo [SD1 §3.7]: bám mép đang kéo, ưu tiên PHÍA TRÊN thanh.
        /// (UX-18, T3) Với làn trên cùng, "phía trên" chính là hàng dấu của thước nên phải lật xuống dưới.
        /// (R-03) Nửa còn lại của T3: ở làn giữa khung, "phía trên" là làn BÊN TRÊN — readout che thanh của làn đó và nuốt luôn
        /// nhãn "chồng 12 giờ" (khung 5, 7). Nên mỗi vị trí ứng viên phải được soát xem có đụng thanh nào đang vẽ không; trên lẫn
        /// dưới đều đụng thì readout ở lại TRONG hàng của làn đang kéo và ĐẨY NGANG tới chỗ trống gần nhất.
        /// </summary>
        private void PlaceReadout(LaneRow row, LiveOpsTimelineGeometry geometry, float barTop,
            IDictionary<string, (float left, float width)> previewGeometry)
        {
            _readoutText.text = DragController.ReadoutMainText(_format);
            string overlap = DragController.ReadoutOverlapText(_format);
            // (UX-18, T2) Dấu ngăn dính vào chữ; khe trái do USS giữ. Ghép " · " + chữ thì UI Toolkit bỏ khoảng trắng ĐẦU của
            // Label và người dùng đọc ra "UTC· chồng 12 giờ".
            _readoutOverlap.text = overlap.Length > 0
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineReadoutOverlapPrefixFormat, overlap)
                : string.Empty;
            _readoutOverlap.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, overlap.Length == 0);
            Readout.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, false);

            float readoutWidth = Readout.layout.width;
            if (float.IsNaN(readoutWidth) || readoutWidth <= 0f)
            {
                readoutWidth = (_readoutText.text.Length + _readoutOverlap.text.Length + _readoutQuickCheckText.text.Length)
                               * LiveOpsTimelineGeometry.LabelCharacterWidth + LiveOpsTimelineGeometry.LabelHorizontalPadding;
            }
            // Bám mép đang kéo, kẹp trong track lề 6px; chạm mép phải thì lật sang trái tay nắm [SD1 §3.7].
            float anchorX = geometry.XOf(DragController.AnchorUtc);
            float left = anchorX;
            if (left + readoutWidth > _trackWidth - ReadoutMargin) left = anchorX - readoutWidth;
            left = Math.Max(ReadoutMargin, Math.Min(left, _trackWidth - ReadoutMargin - readoutWidth));
            float overlayTop = _overlay.worldBound.y;
            float laneTopInOverlay = row.Lane.worldBound.y - overlayTop;
            if (float.IsNaN(laneTopInOverlay)) laneTopInOverlay = 0f;
            float rulerBottomInOverlay = Math.Max(0f, Ruler.worldBound.yMax - overlayTop);
            if (float.IsNaN(rulerBottomInOverlay)) rulerBottomInOverlay = 0f;

            float above = laneTopInOverlay + barTop - ReadoutHeight - ReadoutGapAboveBar;
            float below = laneTopInOverlay + barTop + LiveOpsTimelineGeometry.BarHeight + ReadoutGapAboveBar;
            float top;
            if (above >= rulerBottomInOverlay && IsReadoutSpotFree(above, left, readoutWidth, overlayTop, previewGeometry))
            {
                top = above;
            }
            else if (IsReadoutSpotFree(below, left, readoutWidth, overlayTop, previewGeometry))
            {
                top = below;
            }
            else
            {
                float rowTopInOverlay = row.Row.worldBound.y - overlayTop;
                if (float.IsNaN(rowTopInOverlay)) rowTopInOverlay = laneTopInOverlay;
                float inRow = Math.Max(rulerBottomInOverlay, rowTopInOverlay);
                CollectReadoutBlockedSpans(inRow, overlayTop, previewGeometry);
                float pushed = NearestFreeReadoutLeft(left, readoutWidth);
                if (IsReadoutSpanFree(pushed, readoutWidth))
                {
                    top = inRow;
                    left = pushed;
                }
                else
                {
                    // Cửa sổ chật tới mức không còn chỗ nào trống (hàng 48px, thanh trải hết bề rộng): giữ ĐÚNG chỗ của thiết
                    // kế thay vì dời sang một chỗ cũng bị che — dời mà vẫn che là đổi chỗ lỗi, không phải sửa lỗi.
                    top = Math.Max(rulerBottomInOverlay, above);
                }
            }
            Readout.style.left = left; // style-inline-allowed: 6
            Readout.style.top = Math.Max(rulerBottomInOverlay, top); // style-inline-allowed: 6
        }

        /// <summary>Chỗ đặt ứng viên có đụng thanh nào đang vẽ không (dải dọc <see cref="ReadoutHeight"/> px tính từ <paramref name="topInOverlay"/>).</summary>
        private bool IsReadoutSpotFree(float topInOverlay, float left, float readoutWidth, float overlayTop,
            IDictionary<string, (float left, float width)> previewGeometry)
        {
            CollectReadoutBlockedSpans(topInOverlay, overlayTop, previewGeometry);
            return IsReadoutSpanFree(left, readoutWidth);
        }

        /// <summary>
        /// Quãng ngang bị thanh chiếm trong dải dọc của readout, tính trên MỌI làn đang hiện: readout là lớp phủ nên nó che được
        /// cả làn khác, không riêng làn đang kéo. Thanh đang xem trước lấy hình học xem trước, không lấy chỗ cũ.
        /// </summary>
        private void CollectReadoutBlockedSpans(float topInOverlay, float overlayTop,
            IDictionary<string, (float left, float width)> previewGeometry)
        {
            _readoutBlockedSpans.Clear();
            float bottomInOverlay = topInOverlay + ReadoutHeight;
            foreach (LaneRow row in _rows)
            {
                if (!row.IsActive) continue;
                float laneTopInOverlay = row.Lane.worldBound.y - overlayTop;
                if (float.IsNaN(laneTopInOverlay)) continue;
                for (int index = 0; index < row.Lane.BarCount; index++)
                {
                    LiveOpsTimelineBar bar = row.Lane.BarAt(index);
                    float barHeight = bar.IsCollapsed ? LiveOpsTimelineLane.CollapsedBarHeight : LiveOpsTimelineGeometry.BarHeight;
                    float barTopInOverlay = laneTopInOverlay + bar.Top;
                    if (bottomInOverlay <= barTopInOverlay || topInOverlay >= barTopInOverlay + barHeight) continue;
                    float barLeft = bar.Left;
                    float barWidth = bar.Width;
                    if (previewGeometry != null && previewGeometry.TryGetValue(bar.Model.BarKey, out (float left, float width) preview))
                    {
                        barLeft = preview.left;
                        barWidth = preview.width;
                    }
                    _readoutBlockedSpans.Add((barLeft - ReadoutGapAboveBar, barLeft + barWidth + ReadoutGapAboveBar));
                }
                // (R-03) Nhãn "chồng 12 giờ", tag "bị bỏ" và chip "Đợt tới" cũng là vật cản: chúng là chỗ DUY NHẤT nói ra lý do
                // một đợt bị bỏ hay đợt tới là đợt nào, che chúng cũng tệ như che thanh. Chúng là con của làn nhưng không phải
                // thanh, và đã có layout thật nên đọc thẳng layout.
                foreach (VisualElement child in row.Lane.Children())
                {
                    if (child is LiveOpsTimelineBar) continue;
                    if (child.ClassListContains(LiveOpsHubClassNames.TimelineHidden)) continue;
                    Rect box = child.layout;
                    if (float.IsNaN(box.x) || float.IsNaN(box.y) || box.width <= 0f || box.height <= 0f) continue;
                    float childTopInOverlay = laneTopInOverlay + box.y;
                    if (bottomInOverlay <= childTopInOverlay || topInOverlay >= childTopInOverlay + box.height) continue;
                    _readoutBlockedSpans.Add((box.x - ReadoutGapAboveBar, box.xMax + ReadoutGapAboveBar));
                }
            }
        }

        private bool IsReadoutSpanFree(float left, float readoutWidth)
        {
            float right = left + readoutWidth;
            for (int index = 0; index < _readoutBlockedSpans.Count; index++)
            {
                (float left, float right) span = _readoutBlockedSpans[index];
                if (left < span.right && right > span.left) return false;
            }
            return true;
        }

        /// <summary>Chỗ trống gần chỗ muốn đặt nhất: mép phải/mép trái của từng quãng bị chiếm; không chỗ nào trống thì giữ nguyên.</summary>
        private float NearestFreeReadoutLeft(float preferredLeft, float readoutWidth)
        {
            if (IsReadoutSpanFree(preferredLeft, readoutWidth)) return preferredLeft;
            float best = preferredLeft;
            float bestDistance = float.PositiveInfinity;
            for (int index = 0; index < _readoutBlockedSpans.Count; index++)
            {
                (float left, float right) span = _readoutBlockedSpans[index];
                TryReadoutCandidate(span.right, preferredLeft, readoutWidth, ref best, ref bestDistance);
                TryReadoutCandidate(span.left - readoutWidth, preferredLeft, readoutWidth, ref best, ref bestDistance);
            }
            return best;
        }

        private void TryReadoutCandidate(float candidate, float preferredLeft, float readoutWidth, ref float best,
            ref float bestDistance)
        {
            float clamped = Math.Max(ReadoutMargin, Math.Min(candidate, _trackWidth - ReadoutMargin - readoutWidth));
            if (Math.Abs(clamped - candidate) > 0.5f) return;
            if (!IsReadoutSpanFree(clamped, readoutWidth)) return;
            float distance = Math.Abs(clamped - preferredLeft);
            if (distance >= bestDistance) return;
            bestDistance = distance;
            best = clamped;
        }

        private void ClearDragVisuals()
        {
            SetDragQuickCheckTag(string.Empty, HealthState.Ok);
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
                    IntentRaised?.Invoke(new SelectBarIntent(nextKey));
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
            // (nợ D-3(c)) Nhích bàn phím dùng CÙNG bước lưới với cử chỉ kéo. "Tắt" không có nghĩa cho phím mũi tên (bước 0 =
            // không nhích được), nên chỉ ở nhánh đó mới rơi về bước tự động theo zoom.
            TimeSpan snapStep = DragController.EffectiveStepFor(_pixelsPerHour);
            if (snapStep <= TimeSpan.Zero) snapStep = LiveOpsTimelineGeometry.AutoSnapStep(_pixelsPerHour);
            TimeSpan step = shift && !alt ? TimeSpan.FromDays(1) : snapStep;
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
                IntentRaised?.Invoke(new SelectBarIntent(nearest.BarKey));
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
