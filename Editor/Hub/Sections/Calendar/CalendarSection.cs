using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Màn <c>calendar</c> ở tầng Schedule (7.3): timeline là trung tâm, toolbar khoảng/zoom/bắt lưới ở trên, inspector đợt 280px
    /// bên phải. View mỏng — mọi quyết định nằm ở <see cref="CalendarTimelinePresenter"/>, <see cref="CalendarInspectorModel"/>,
    /// <see cref="CalendarDeleteFlow"/> và <see cref="AddEventFlowModel"/>; màn chỉ nối dây và gắn class.
    /// </summary>
    internal sealed class CalendarSection : IHubSection, IHubHostAware, IHubSectionActions, IHubSectionViewState, IHubSectionNavigation
    {
        private readonly LiveOpsHubServices _services;
        private readonly CalendarTimelinePresenter _presenter;

        private IHubHost _host;
        private VisualElement _root;
        private VisualElement _empty;
        private VisualElement _main;
        private VisualElement _timelineColumn;
        private LiveOpsTimelineElement _timeline;
        private CalendarToolbar _toolbar;
        private CalendarEventInspector _inspector;
        private TwoPaneSplitView _split;
        private LiveOpsTimelineZoom _zoom = LiveOpsTimelineZoom.ThreeWeeks;
        private DateTime _rangeStartUtc;
        private bool _hasRangeStart;
        private string _searchText = string.Empty;

        public CalendarSection(LiveOpsHubServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _presenter = new CalendarTimelinePresenter(services);
            _presenter.SelectionChanged += OnSelectionChanged;
            _presenter.NavigationRequested += RaiseNavigation;
            _presenter.ToastRequested += toast => _services.Bus.ShowToast(toast);
            _presenter.AddEventRequested += OnAddEventRequestedAt;
            _presenter.ZoomRequested += OnZoomRequested;
            _presenter.DocumentEdited += OnDocumentEdited;
        }

        public string Id => LiveOpsHubSections.Ids.Calendar;
        public string Title => LiveOpsHubStrings.ShellCalendarTitle;
        public string Subtitle => LiveOpsHubStrings.ShellCalendarSubtitle;
        public PipelineStage Stage => PipelineStage.Schedule;
        public IReadOnlyList<string> RequiredElementNames => LiveOpsHubPaths.RequiredCalendarElementNames;

        /// <summary>Services của cửa sổ (G-SESSION) — giữ nguyên tên property mà registry và test của G-SESSION đang đọc.</summary>
        internal LiveOpsHubServices Services => _services;

        internal CalendarTimelinePresenter Presenter => _presenter;
        internal CalendarToolbar Toolbar => _toolbar;
        internal CalendarEventInspector Inspector => _inspector;
        internal LiveOpsTimelineElement Timeline => _timeline;

        public SectionHealth GetHealth()
        {
            return LiveOpsHubFindingRouting.ForSection(Id, _services);
        }

        public void Bind(IHubHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public VisualElement CreateView()
        {
            _root = new VisualElement();
            VisualTreeAsset layout = _services.LayoutLoader.LoadVisualTree(LiveOpsHubPaths.CalendarSectionUxml);
            // Nạp hỏng thì NÉM: shell bắt và hiện LiveOpsHubFailureView nêu đường dẫn (7.0). Trả về cây rỗng sẽ thành một màn
            // trắng im lặng — đúng thứ RequiredElementNames sinh ra để chặn (UXML sai cú pháp XML từng lọt qua kiểu đó).
            if (layout == null)
            {
                throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.CalendarMissingLayoutMessageFormat, LiveOpsHubPaths.CalendarSectionUxml));
            }
            layout.CloneTree(_root);
            StyleSheet sheet = _services.LayoutLoader.LoadStyleSheet(LiveOpsHubPaths.CalendarSectionUss);
            if (sheet != null) _root.styleSheets.Add(sheet);

            _empty = _root.Q(LiveOpsHubPaths.CalendarElementNames.Empty);
            _main = _root.Q(LiveOpsHubPaths.CalendarElementNames.Main);
            _split = _root.Q<TwoPaneSplitView>(LiveOpsHubPaths.CalendarElementNames.Split);
            _timelineColumn = _root.Q(LiveOpsHubPaths.CalendarElementNames.TimelineColumn);
            BuildEmptyState();
            BuildToolbar();
            BuildTimeline();
            BuildInspector();

            // INTERIM(G-CALENDAR-DEPTH): pane Danh sách và pane So với đã đăng chưa dựng ở W4 (mục 12 I-5) nên split luôn thu
            // pane trái — split vẫn còn trong cây (W5 chỉ cần mở lại) và người dùng không thấy một pane trống không giải thích được.
            _split?.CollapseChild(0);

            // PD-21: toast của màn Lịch phải nằm TRÊN minimap/chú giải/gợi ý, nên cột nội dung bật class nâng toast.
            _services.Bus.SetContentClass(LiveOpsHubClassNames.ContentRaisedToast, true);

            _services.Session.DocumentChanged += OnSessionChanged;
            _services.Session.CheckChanged += OnSessionChanged;
            _root.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel, TrickleDown.TrickleDown);
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            Refresh();
            return _root;
        }

        public void OnShown()
        {
            Refresh();
        }

        public void PopulateHeaderActions(VisualElement container)
        {
            if (container == null) return;
            Button add = new Button(OpenAddEventPopover) { text = LiveOpsHubStrings.CalendarAddEventButton };
            add.AddToClassList(LiveOpsHubClassNames.Button);
            add.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            container.Add(add);
        }

        public void ApplyNavigation(LiveOpsHubNavigation navigation)
        {
            if (navigation == null) return;
            if (navigation.EntryKey.Length > 0)
            {
                _presenter.SetSelectedBarKey(navigation.EntryKey);
                FrameSelected();
                return;
            }
            if (navigation.EventType.Length == 0 || _timeline == null) return;
            _timeline.FrameAll();
        }

        // ============================================================================================================ trạng thái view

        public string CaptureViewState()
        {
            CalendarViewState state = new CalendarViewState
            {
                zoom = (int)_zoom,
                rangeStartUtcText = _hasRangeStart ? LiveEventUtcText.Format(_rangeStartUtc) : string.Empty,
                selectedBarKey = _presenter.SelectedBarKey,
                hiddenLanes = HiddenLaneArray(),
                snapMode = _toolbar == null ? 0 : (int)_toolbar.SnapMode,
            };
            return JsonUtility.ToJson(state);
        }

        /// <summary>JSON rỗng hoặc hỏng → mặc định, không ném: trạng thái lưu từ bản trước không được làm sập màn (7.0).</summary>
        public void RestoreViewState(string viewStateJson)
        {
            if (string.IsNullOrEmpty(viewStateJson)) return;
            CalendarViewState state;
            try
            {
                state = JsonUtility.FromJson<CalendarViewState>(viewStateJson);
            }
            catch (ArgumentException)
            {
                return;
            }
            if (state == null) return;
            _zoom = ZoomOf(state.zoom);
            _hasRangeStart = LiveEventUtcText.TryParse(state.rangeStartUtcText ?? string.Empty, out _rangeStartUtc);
            // Làn ẩn chỉ có MỘT bản, ở presenter: bản thứ hai trong màn từng làm chip "Đang ẩn n làn" đếm một danh sách mà
            // timeline không bao giờ đọc.
            _presenter.SetHiddenLanes(state.hiddenLanes);
            _presenter.SetSelectedBarKey(state.selectedBarKey ?? string.Empty);
            _toolbar?.SetSnapMode(SnapOf(state.snapMode));
            // RestoreViewState chạy SAU CreateView, nên tab zoom đã dựng với giá trị mặc định — không đồng bộ lại thì tab sáng
            // "3 tuần" trong khi trục vẽ theo zoom vừa khôi phục.
            _toolbar?.SetZoomWithoutNotify(_zoom);
            Refresh();
        }

        private string[] HiddenLaneArray()
        {
            IReadOnlyList<string> hiddenLanes = _presenter.HiddenLanes;
            string[] result = new string[hiddenLanes.Count];
            for (int index = 0; index < hiddenLanes.Count; index++) result[index] = hiddenLanes[index];
            return result;
        }

        // ============================================================================================================ dựng cây

        private void BuildEmptyState()
        {
            if (_empty == null) return;
            _empty.Clear();
            _empty.Add(new Label(LiveOpsHubStrings.CalendarNoAssetEmptyText));
            Button openOverview = new Button(() => RaiseNavigation(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Overview)))
            {
                text = LiveOpsHubStrings.CalendarOpenOverviewButton,
            };
            openOverview.AddToClassList(LiveOpsHubClassNames.Button);
            _empty.Add(openOverview);
        }

        private void BuildToolbar()
        {
            Toolbar host = _root.Q<Toolbar>(LiveOpsHubPaths.CalendarElementNames.Toolbar);
            if (host == null) return;
            _toolbar = new CalendarToolbar(host, _services.Format);
            _toolbar.RangeStepRequested += StepRange;
            _toolbar.TodayRequested += GoToToday;
            _toolbar.PickRangeStartRequested += OpenRangeStartPopover;
            _toolbar.ZoomChanged += zoom =>
            {
                _zoom = zoom;
                Refresh();
            };
            _toolbar.SnapModeChanged += _ => Refresh();
            _toolbar.SearchChanged += OnSearchChanged;
            _toolbar.SearchSubmitted += OnSearchSubmitted;
            _toolbar.SetZoomWithoutNotify(_zoom);
        }

        private void BuildTimeline()
        {
            if (_timelineColumn == null) return;
            _timeline = new LiveOpsTimelineElement { name = LiveOpsHubPaths.CalendarElementNames.Timeline };
            _timeline.IntentRaised += _presenter.HandleIntent;
            _timeline.RangeChanged += OnTimelineRangeChanged;
            _timeline.SetDeviceOffset(_services.TimeZone.DeviceOffsetAt(_services.Clock.UtcNow));
            _timelineColumn.Add(_timeline);
        }

        private void BuildInspector()
        {
            VisualElement title = _root.Q(LiveOpsHubPaths.CalendarElementNames.InspectorTitle);
            VisualElement body = _root.Q(LiveOpsHubPaths.CalendarElementNames.InspectorBody);
            if (title == null || body == null) return;
            _inspector = new CalendarEventInspector(_services, _presenter, title, body);
            _inspector.AddEventRequested += OpenAddEventPopover;
            _inspector.NavigationRequested += RaiseNavigation;
        }

        // ============================================================================================================ vẽ lại

        internal void Refresh()
        {
            if (_root == null) return;
            bool hasAsset = _services.Session.Asset != null;
            _empty?.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, hasAsset);
            _main?.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, !hasAsset);
            if (!hasAsset) return;

            DateTime rangeStartUtc = RangeStartUtc();
            DateTime rangeEndUtc = LiveOpsTimelineGeometry.AddTicksClamped(rangeStartUtc,
                LiveOpsTimelineGeometry.RangeLengthOf(_zoom).Ticks);
            _presenter.ContentWidth = _root.layout.width;
            LiveOpsTimelineModel model = _presenter.BuildModel(rangeStartUtc, rangeEndUtc, TrackWidth());
            if (_timeline != null)
            {
                _timeline.SetRange(rangeStartUtc, _zoom);
                _timeline.SetModel(model);
                _timeline.Select(_presenter.SelectedBarKey, false);
            }
            _toolbar?.SetRange(rangeStartUtc, rangeEndUtc, _services.Clock.UtcNow);
            _toolbar?.SetHiddenLaneCount(_presenter.HiddenLanes.Count);
            _inspector?.Refresh(_presenter.SelectedBarKey);
        }

        private float TrackWidth()
        {
            float trackWidth = _timeline == null ? 0f : _timeline.TrackWidth;
            return trackWidth > 0f ? trackWidth : LiveOpsTimelineElement.FallbackTrackWidth;
        }

        private DateTime RangeStartUtc()
        {
            if (_hasRangeStart) return _rangeStartUtc;
            // Mặc định đặt hôm nay ở khoảng một phần ba đầu khung: người dùng mở Lịch để xem sắp tới, không phải để xem quá khứ.
            DateTime nowUtc = _services.Clock.UtcNow;
            TimeSpan length = LiveOpsTimelineGeometry.RangeLengthOf(_zoom);
            _rangeStartUtc = nowUtc.Date.AddTicks(-(length.Ticks / 3));
            _hasRangeStart = true;
            return _rangeStartUtc;
        }

        private void StepRange(int direction)
        {
            TimeSpan length = LiveOpsTimelineGeometry.RangeLengthOf(_zoom);
            _rangeStartUtc = RangeStartUtc().AddTicks(length.Ticks * Math.Sign(direction));
            _hasRangeStart = true;
            Refresh();
        }

        private void GoToToday()
        {
            _hasRangeStart = false;
            Refresh();
        }

        private void OnTimelineRangeChanged(DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            _rangeStartUtc = rangeStartUtc;
            _hasRangeStart = true;
            _toolbar?.SetRange(rangeStartUtc, rangeEndUtc, _services.Clock.UtcNow);
        }

        /// <summary>Gõ id đợt: chọn và căn khung đợt khớp đầu tiên (7.3) — không lọc bớt thanh, không đợt nào biến mất.</summary>
        private void OnSearchChanged(string searchText)
        {
            _searchText = searchText ?? string.Empty;
            SelectMatch(false);
        }

        /// <summary>Enter trong ô tìm: đợt khớp KẾ TIẾP sau đợt đang chọn, hết danh sách thì vòng lại đầu (7.3).</summary>
        private void OnSearchSubmitted(string searchText)
        {
            _searchText = searchText ?? string.Empty;
            SelectMatch(true);
        }

        /// <param name="afterSelection">
        /// true = bắt đầu dò từ NGAY SAU đợt đang chọn. Danh sách thanh phẳng hoá theo thứ tự làn rồi thứ tự thanh, nên "kế tiếp"
        /// là thứ tự người dùng nhìn thấy trên trục.
        /// </param>
        private void SelectMatch(bool afterSelection)
        {
            if (_searchText.Length == 0 || _presenter.Model == null) return;
            List<LiveOpsTimelineBarModel> bars = MatchableBars();
            if (bars.Count == 0) return;
            int startIndex = 0;
            if (afterSelection)
            {
                for (int index = 0; index < bars.Count; index++)
                {
                    if (!string.Equals(bars[index].BarKey, _presenter.SelectedBarKey, StringComparison.Ordinal)) continue;
                    startIndex = index + 1;
                    break;
                }
            }
            for (int step = 0; step < bars.Count; step++)
            {
                LiveOpsTimelineBarModel bar = bars[(startIndex + step) % bars.Count];
                if (bar.EventId.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0) continue;
                _presenter.SetSelectedBarKey(bar.BarKey);
                FrameSelected();
                return;
            }
        }

        private List<LiveOpsTimelineBarModel> MatchableBars()
        {
            List<LiveOpsTimelineBarModel> result = new List<LiveOpsTimelineBarModel>();
            IReadOnlyList<LiveOpsTimelineLaneModel> lanes = _presenter.Model.Lanes;
            for (int laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
            {
                IReadOnlyList<LiveOpsTimelineBarModel> bars = lanes[laneIndex].Bars;
                for (int barIndex = 0; barIndex < bars.Count; barIndex++)
                {
                    if (!bars[barIndex].IsStrip) result.Add(bars[barIndex]);
                }
            }
            return result;
        }

        private void FrameSelected()
        {
            LiveOpsTimelineBarModel bar = _presenter.FindBar(_presenter.SelectedBarKey);
            if (bar == null || _timeline == null) return;
            _timeline.FrameInstance(bar.EventType, bar.StartUtc, bar.EndUtc);
        }

        // ============================================================================================================ sự kiện

        private void OnSelectionChanged(string barKey)
        {
            _inspector?.Refresh(barKey);
            _timeline?.Select(barKey, false);
        }

        private void OnDocumentEdited()
        {
            _services.Bus.InvalidateHealth();
            Refresh();
        }

        private void OnSessionChanged()
        {
            Refresh();
        }

        private void OnZoomRequested(DateTime startUtc, DateTime endUtc)
        {
            _rangeStartUtc = startUtc;
            _hasRangeStart = true;
            _zoom = LiveOpsTimelineZoom.Day;
            _toolbar?.SetZoomWithoutNotify(_zoom);
            Refresh();
        }

        private void OnAddEventRequestedAt(string laneTypeId, DateTime startUtc, DateTime? endUtc)
        {
            int durationHours = endUtc == null ? AddEventFlowModel.DefaultDurationHours : (int)(endUtc.Value - startUtc).TotalHours;
            ShowAddEventPopover(AddEventFlowModel.CreateAt(_services.Session, _services.Clock.UtcNow, laneTypeId, startUtc,
                durationHours));
        }

        private void OpenAddEventPopover()
        {
            ShowAddEventPopover(AddEventFlowModel.Create(_services.Session, _services.Clock.UtcNow));
        }

        private void ShowAddEventPopover(AddEventFlowModel flow)
        {
            LiveOpsPopoverContent.ShowSingle(ActivatorRect(), new AddEventPopover(_services, flow, SubmitAddEvent));
        }

        /// <summary>Mở popover "Chọn ngày bắt đầu…" của menu khoảng — mục menu này TỪNG gọi thẳng "Hôm nay", tức làm khác hẳn nhãn.</summary>
        private void OpenRangeStartPopover()
        {
            DateTime currentStartUtc = RangeStartUtc();
            LiveOpsPopoverContent.ShowSingle(ActivatorRect(), new RangeStartPopover(_services, currentStartUtc, startUtc =>
            {
                _rangeStartUtc = startUtc;
                _hasRangeStart = true;
                Refresh();
            }));
        }

        private Rect ActivatorRect()
        {
            Rect anchor = _root == null ? new Rect(0f, 0f, 1f, 1f) : _root.worldBound;
            return new Rect(anchor.x, anchor.y, 1f, 1f);
        }

        private void SubmitAddEvent(AddEventFlowModel flow)
        {
            AddFixedEventEdit edit = flow.ToEdit();
            if (edit == null) return;
            string message = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarAddToastFormat, flow.SuggestedEventId);
            if (!_presenter.ApplyEdit(edit, LiveOpsEditOperation.AddFixedEvent, edit.Entry.EntryKey, message, string.Empty)) return;
            _presenter.SetSelectedBarKey(edit.Entry.EntryKey);
        }

        private void RaiseNavigation(LiveOpsHubNavigation navigation)
        {
            if (navigation == null) return;
            _services.Bus.Navigate(navigation);
        }

        private void OnGeometryChanged(GeometryChangedEvent geometryEvent)
        {
            _presenter.ContentWidth = geometryEvent.newRect.width;
        }

        /// <summary>Panel biến mất (đổi màn, đóng cửa sổ, domain reload): gỡ nghe phiên và huỷ thao tác kéo đang mở (SP-2 (d)).</summary>
        private void OnDetachFromPanel(DetachFromPanelEvent detachEvent)
        {
            _services.Session.DocumentChanged -= OnSessionChanged;
            _services.Session.CheckChanged -= OnSessionChanged;
            // PD-21 chỉ đúng KHI đang ở màn Lịch: rời màn mà để class nâng toast thì minimap/chú giải của màn khác cũng bị đẩy
            // xuống dưới toast. Bật ở CreateView thì phải tắt ở đây.
            _services.Bus.SetContentClass(LiveOpsHubClassNames.ContentRaisedToast, false);
            _presenter.AbortDrag();
            _host?.SetSectionViewState(Id, CaptureViewState());
        }

        private static LiveOpsTimelineZoom ZoomOf(int value)
        {
            switch (value)
            {
                case 0: return LiveOpsTimelineZoom.Day;
                case 2: return LiveOpsTimelineZoom.Month;
                default: return LiveOpsTimelineZoom.ThreeWeeks;
            }
        }

        private static CalendarSnapMode SnapOf(int value)
        {
            switch (value)
            {
                case 1: return CalendarSnapMode.FifteenMinutes;
                case 2: return CalendarSnapMode.Hour;
                case 3: return CalendarSnapMode.Day;
                case 4: return CalendarSnapMode.Off;
                default: return CalendarSnapMode.Automatic;
            }
        }

        /// <summary>
        /// Popover một ô của mục menu "Chọn ngày bắt đầu…" [SD1 §3.1]: đặt mốc trái của khung. Ô giờ chỉ nhận UTC như mọi ô giờ
        /// khác của hub; Esc đóng và không đổi gì (lớp gốc lo).
        /// </summary>
        private sealed class RangeStartPopover : LiveOpsPopoverContent
        {
            private const float PopoverWidthPixels = 320f;
            private const float PopoverHeightPixels = 116f;

            private readonly LiveOpsHubServices _services;
            private readonly DateTime _initialStartUtc;
            private readonly Action<DateTime> _apply;

            private LiveOpsUtcDateTimeField _field;

            internal RangeStartPopover(LiveOpsHubServices services, DateTime initialStartUtc, Action<DateTime> apply)
            {
                _services = services ?? throw new ArgumentNullException(nameof(services));
                _initialStartUtc = initialStartUtc;
                _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            }

            protected override Vector2 PopoverSize
            {
                get { return new Vector2(PopoverWidthPixels, PopoverHeightPixels); }
            }

            protected override Focusable InitialFocus
            {
                get { return _field; }
            }

            internal LiveOpsUtcDateTimeField Field => _field;

            protected override VisualElement BuildContent()
            {
                VisualElement content = new VisualElement();
                StyleSheet sheet = _services.LayoutLoader.LoadStyleSheet(LiveOpsHubPaths.CalendarSectionUss);
                if (sheet != null) content.styleSheets.Add(sheet);

                Label title = new Label(LiveOpsHubStrings.CalendarRangeMenuPickStart);
                title.AddToClassList(LiveOpsHubClassNames.Caption);
                content.Add(title);

                _field = new LiveOpsUtcDateTimeField(LiveOpsHubStrings.CalendarFieldStartLabel);
                _field.SetDeviceOffset(_services.TimeZone.DeviceOffsetAt(_services.Clock.UtcNow));
                _field.SetValueWithoutNotify(_initialStartUtc);
                content.Add(_field);

                VisualElement buttons = new VisualElement();
                buttons.AddToClassList(LiveOpsHubClassNames.CalendarFlowButtons);
                Button cancel = new Button(ClosePopover) { text = LiveOpsHubStrings.CalendarAddCancelButton };
                cancel.AddToClassList(LiveOpsHubClassNames.Button);
                buttons.Add(cancel);
                Button apply = new Button(ApplyAndClose) { text = LiveOpsHubStrings.CalendarPickRangeStartApplyButton };
                apply.AddToClassList(LiveOpsHubClassNames.Button);
                apply.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
                buttons.Add(apply);
                content.Add(buttons);
                return content;
            }

            /// <summary>Chuỗi không đọc được thì KHÔNG đổi khung và KHÔNG đóng: ô giữ nguyên câu lỗi của chính nó (7.0).</summary>
            internal void ApplyAndClose()
            {
                if (_field == null) return;
                if (!LiveOpsUtcDateTimeField.TryParseParts(_field.RawDateText, _field.RawTimeText, out DateTime startUtc)) return;
                _apply(startUtc);
                ClosePopover();
            }
        }

        /// <summary>Trạng thái view sống qua domain reload (7.0): zoom, khoảng, đợt đang chọn, làn ẩn, chế độ bắt lưới.</summary>
        [Serializable]
        private sealed class CalendarViewState
        {
            public int zoom;
            public string rangeStartUtcText;
            public string selectedBarKey;
            public string[] hiddenLanes;
            public int snapMode;
        }
    }
}
