using System;
using System.Collections.Generic;
using UnityEditor;
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
        private readonly CalendarCommandHandler _commandHandler;

        private IHubHost _host;
        private VisualElement _root;
        private VisualElement _empty;
        private VisualElement _main;
        private VisualElement _timelineColumn;
        private LiveOpsTimelineElement _timeline;
        private CalendarToolbar _toolbar;
        private CalendarEventInspector _inspector;
        private CalendarListPane _listPane;
        private CalendarComparePane _comparePane;
        private LiveOpsHoverCardHost _hoverCardHost;
        private TwoPaneSplitView _split;
        private VisualElement _inspectorRoot;
        private bool _isListPaneOpen;
        private bool _isComparePaneOpen;
        private int _pinnedFindingIndex = -1;
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

            _commandHandler = new CalendarCommandHandler(services, _presenter);
            _commandHandler.AddEventRequested += OnAddEventRequestedAt;
            _commandHandler.DuplicateRequested += OnDuplicateRequested;
            _commandHandler.LanesChanged += OnDocumentEdited;
            _commandHandler.FrameRequested += FrameBar;
            _commandHandler.FindingStepRequested += StepFinding;
            _presenter.CommandHandler = _commandHandler;
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
        internal CalendarListPane ListPane => _listPane;
        internal CalendarComparePane ComparePane => _comparePane;
        internal CalendarCommandHandler CommandHandler => _commandHandler;
        internal LiveOpsHoverCardHost HoverCardHost => _hoverCardHost;

        /// <summary>Pane "So với đã đăng" đang mở — loại trừ với inspector đợt [SD1 §3.4].</summary>
        internal bool IsComparePaneOpen => _isComparePaneOpen;

        internal bool IsListPaneOpen => _isListPaneOpen;

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
            BuildListPane();
            BuildComparePane();

            // Hover card của thanh là con CUỐI của gốc màn: nó phải nổi trên trục, trên minimap và trên chú giải, mà USS không
            // có z-index — thứ tự con là thứ tự vẽ ([FD §2.14]).
            _hoverCardHost = new LiveOpsHoverCardHost(_root);
            ApplyPaneVisibility();

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
            // (V-13) "Xem khác biệt" của băng asset đổi trên đĩa: mở pane So với ở nguồn Disk, không phải bản đã đăng.
            if (navigation.CompareSource != LiveOpsHubCompareSource.None)
            {
                _services.Session.Publish?.SelectCompareSource(navigation.CompareSource);
                _isComparePaneOpen = true;
                ApplyPaneVisibility();
                Refresh();
            }
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
                listPaneOpen = _isListPaneOpen,
                comparePaneOpen = _isComparePaneOpen,
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
            ApplySnapStep();
            _isListPaneOpen = state.listPaneOpen;
            _isComparePaneOpen = state.comparePaneOpen;
            _toolbar?.SetListPaneOpenWithoutNotify(_isListPaneOpen);
            ApplyPaneVisibility();
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
            // (nợ D-3(c)) Lựa chọn bắt lưới đi THẲNG xuống cử chỉ kéo, không còn chỉ nằm trong trạng thái view.
            _toolbar.SnapModeChanged += _ =>
            {
                ApplySnapStep();
                Refresh();
            };
            _toolbar.ListPaneToggled += isOpen =>
            {
                _isListPaneOpen = isOpen;
                ApplyPaneVisibility();
            };
            _toolbar.ComparePaneToggled += isOpen =>
            {
                _isComparePaneOpen = isOpen;
                ApplyPaneVisibility();
                Refresh();
            };
            _toolbar.SearchChanged += OnSearchChanged;
            _toolbar.SearchSubmitted += OnSearchSubmitted;
            _toolbar.SetZoomWithoutNotify(_zoom);
            _toolbar.SetListPaneOpenWithoutNotify(_isListPaneOpen);
        }

        private void BuildTimeline()
        {
            if (_timelineColumn == null) return;
            _timeline = new LiveOpsTimelineElement { name = LiveOpsHubPaths.CalendarElementNames.Timeline };
            _timeline.IntentRaised += _presenter.HandleIntent;
            _timeline.RangeChanged += OnTimelineRangeChanged;
            _timeline.HoverChanged += OnTimelineHoverChanged;
            _timeline.ContextRequested += OnTimelineContextRequested;
            _timeline.UnplaceableRequested += OnUnplaceableRequested;
            _timeline.SetDeviceOffset(_services.TimeZone.DeviceOffsetAt(_services.Clock.UtcNow));
            // (nợ D-3(a)) Tag kiểm nhanh của presenter đi thẳng vào vế thứ ba của readout.
            _presenter.QuickCheckTagChanged += (tagText, health) => _timeline.SetDragQuickCheckTag(tagText, health);
            _timelineColumn.Add(_timeline);
            ApplySnapStep();
        }

        private void BuildListPane()
        {
            VisualElement host = _root.Q(LiveOpsHubPaths.CalendarElementNames.ListPane);
            if (host == null) return;
            _listPane = new CalendarListPane(host, _services.Format);
            _listPane.SelectionChanged += entryKey =>
            {
                _presenter.SetSelectedBarKey(entryKey);
                FrameSelected();
            };
        }

        /// <summary>
        /// Pane So với là SIBLING của inspector, ngoài split: split tự ghi style inline lên hai pane con nên bề rộng 280 cố định
        /// sẽ mất nếu pane nằm trong đó [SD1 §3.9].
        /// </summary>
        private void BuildComparePane()
        {
            _inspectorRoot = _root.Q(LiveOpsHubPaths.CalendarElementNames.Inspector);
            if (_main == null) return;
            _comparePane = new CalendarComparePane(_services);
            _comparePane.RowActivated += OnCompareRowActivated;
            _comparePane.RowContextRequested += OnCompareRowContextRequested;
            _main.Add(_comparePane.Root);
        }

        private void BuildInspector()
        {
            VisualElement title = _root.Q(LiveOpsHubPaths.CalendarElementNames.InspectorTitle);
            VisualElement body = _root.Q(LiveOpsHubPaths.CalendarElementNames.InspectorBody);
            if (title == null || body == null) return;
            _inspector = new CalendarEventInspector(_services, _presenter, title, body);
            _inspector.AddEventRequested += OpenAddEventPopover;
            _inspector.NavigationRequested += RaiseNavigation;
            _inspector.ProposalRequested = OpenProposalPopover;
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
            _listPane?.SetDocument(_services.Session.Document, _services.Clock.UtcNow, _presenter.SelectedBarKey);
            RefreshComparePane();
            AttachHoverCards();
        }

        /// <summary>
        /// Pane So với vẽ lại kể cả khi đang đóng: nút toolbar in SỐ thay đổi, và số đó phải là số của chính pane — đếm bằng một
        /// đường khác là cách chắc chắn nhất để nút nói "5" còn pane liệt kê 4 dòng.
        /// </summary>
        private void RefreshComparePane()
        {
            if (_comparePane == null) return;
            _comparePane.Refresh();
            _comparePane.SetSelectedEntryKey(_presenter.SelectedBarKey);
            LiveOpsHubPublishState publish = _services.Session.Publish;
            bool hasBaseline = publish != null && publish.CompareDocument != null;
            _toolbar?.SetCompareState(_isComparePaneOpen, hasBaseline, _comparePane.ChangeCount);
            // Bản so biến mất (gỡ dấu đã đăng, giải xung đột đĩa) thì pane phải tự đóng, không đứng lại với dữ liệu cũ.
            if (_isComparePaneOpen && !hasBaseline)
            {
                _isComparePaneOpen = false;
                ApplyPaneVisibility();
            }
        }

        /// <summary>Inspector và pane So với LOẠI TRỪ nhau (cùng chỗ, cùng 280px); pane Danh sách bật/tắt bằng split.</summary>
        private void ApplyPaneVisibility()
        {
            if (_split != null)
            {
                if (_isListPaneOpen) _split.UnCollapse();
                else _split.CollapseChild(0);
            }
            _inspectorRoot?.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, _isComparePaneOpen);
            _comparePane?.Root.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, !_isComparePaneOpen);
        }

        /// <summary>(nợ D-3(c)) Bước bắt lưới của khung nhìn: Tự động = null (theo zoom), Tắt = 0, còn lại là bước cố định.</summary>
        private void ApplySnapStep()
        {
            if (_timeline == null) return;
            _timeline.DragController.SnapStep = SnapStepOf(_toolbar == null ? CalendarSnapMode.Automatic : _toolbar.SnapMode);
        }

        private static TimeSpan? SnapStepOf(CalendarSnapMode mode)
        {
            switch (mode)
            {
                case CalendarSnapMode.FifteenMinutes: return TimeSpan.FromMinutes(15);
                case CalendarSnapMode.Hour: return TimeSpan.FromHours(1);
                case CalendarSnapMode.Day: return TimeSpan.FromDays(1);
                case CalendarSnapMode.Off: return TimeSpan.Zero;
                default: return null;
            }
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

        // ============================================================================================================ chiều sâu W5

        /// <summary>Gắn hover card cho từng thanh sau mỗi lần dựng lại làn — thanh là element mới nên phải gắn lại.</summary>
        private void AttachHoverCards()
        {
            if (_hoverCardHost == null || _timeline == null || _presenter.Model == null) return;
            for (int laneIndex = 0; laneIndex < _timeline.LaneCount; laneIndex++)
            {
                LiveOpsTimelineLane lane = _timeline.LaneAt(laneIndex);
                for (int barIndex = 0; barIndex < lane.BarCount; barIndex++)
                {
                    LiveOpsTimelineBar bar = lane.BarAt(barIndex);
                    if (bar.Model.IsStrip) continue;
                    LiveOpsTimelineBar captured = bar;
                    _hoverCardHost.Attach(bar, () => BuildHoverCard(captured.Model, false, -1));
                }
            }
        }

        /// <summary>Rời thanh thì bỏ ghim: thẻ ghim bằng F8 không được sống mãi khi người dùng đã đi chỗ khác.</summary>
        private void OnTimelineHoverChanged(LiveOpsTimelineHover hover)
        {
            if (hover == null || !hover.IsLeave || _hoverCardHost == null || !_hoverCardHost.IsPinned) return;
            _hoverCardHost.Hide();
            _pinnedFindingIndex = -1;
        }

        private VisualElement BuildHoverCard(LiveOpsTimelineBarModel bar, bool isPinned, int findingIndex)
        {
            IReadOnlyList<LiveEventCalendarFinding> findings = CalendarHoverCardContent.FindingsInOrder(_services.Session);
            LiveEventCalendarFinding finding = findingIndex >= 0 && findingIndex < findings.Count
                ? findings[findingIndex]
                : FindingForBar(findings, bar);
            LiveEventCalendarDocument document = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            if (!isPinned) return CalendarHoverCardContent.Build(bar, finding, _services, document.LatestStamp);
            return CalendarHoverCardContent.Build(bar, finding, _services, document.LatestStamp, true, findingIndex + 1,
                findings.Count, () => QuickFix(finding));
        }

        private static LiveEventCalendarFinding FindingForBar(IReadOnlyList<LiveEventCalendarFinding> findings,
            LiveOpsTimelineBarModel bar)
        {
            for (int index = 0; index < findings.Count; index++)
            {
                if (string.Equals(findings[index].TargetEntryKey, bar.BarKey, StringComparison.Ordinal)) return findings[index];
            }
            return null;
        }

        /// <summary>
        /// F8 / Shift+F8: đi tới phát hiện kế tiếp của CẢ LỊCH, chọn đợt của nó, căn khung và GHIM hover card kèm bộ đếm "1/5"
        /// [SD1 §3.8]. Không có phát hiện nào thì không ghim một thẻ rỗng.
        /// </summary>
        private void StepFinding(int direction)
        {
            IReadOnlyList<LiveEventCalendarFinding> findings = CalendarHoverCardContent.FindingsInOrder(_services.Session);
            if (findings.Count == 0 || _timeline == null || _hoverCardHost == null) return;
            int next = _pinnedFindingIndex < 0
                ? (direction < 0 ? findings.Count - 1 : 0)
                : ((_pinnedFindingIndex + direction) % findings.Count + findings.Count) % findings.Count;
            _pinnedFindingIndex = next;
            string entryKey = findings[next].TargetEntryKey ?? string.Empty;
            if (entryKey.Length > 0)
            {
                _presenter.SetSelectedBarKey(entryKey);
                FrameSelected();
            }
            LiveOpsTimelineBarModel bar = _presenter.FindBar(_presenter.SelectedBarKey);
            LiveOpsTimelineBar barElement = _timeline.FindBar(_presenter.SelectedBarKey);
            if (bar == null || barElement == null) return;
            _hoverCardHost.ShowPinned(barElement, BuildHoverCard(bar, true, next));
        }

        /// <summary>"Sửa nhanh…" của thẻ ghim: có cách sửa thì mở popover Đề xuất, không thì đưa người dùng về inspector.</summary>
        private void QuickFix(LiveEventCalendarFinding finding)
        {
            _hoverCardHost?.Hide();
            _pinnedFindingIndex = -1;
            if (finding == null) return;
            if (finding.Repairs.Count == 0)
            {
                _inspector?.FocusFirstField();
                return;
            }
            OpenProposalPopover(finding, 0);
        }

        /// <summary>
        /// (mục 12 I-4) Nút đề xuất mở ĐÚNG popover Đề xuất của màn Kiểm lịch, chọn sẵn lựa chọn vừa bấm — không còn sửa thẳng
        /// field. Một popover cho cả hai màn nên câu, kiểm nhanh và luật "Enter = Quay lại" chỉ có một bản.
        /// </summary>
        internal void OpenProposalPopover(LiveEventCalendarFinding finding, int repairIndex)
        {
            if (finding == null || finding.Repairs.Count == 0) return;
            ProposalPopover popover = new ProposalPopover(finding, _services.Format, _services.LayoutLoader,
                repair => RemainingAfterRepair(finding, repair), repair => ApplyRepair(finding, repair), repairIndex);
            LiveOpsPopoverContent.ShowSingle(ActivatorRect(), popover);
        }

        /// <summary>Số vấn đề CÒN LẠI trên làn sau khi áp một cách sửa; âm = không kiểm được (popover đổi dấu theo số này).</summary>
        private int RemainingAfterRepair(LiveEventCalendarFinding finding, LiveEventCalendarRepair repair)
        {
            if (repair == null || repair.Edit == null) return -1;
            LiveEventCalendarDocument document = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            if (!LiveEventCalendarEdits.TryApply(document, repair.Edit, out LiveEventCalendarDocument preview)) return -1;
            string eventType = EventTypeOf(finding, document);
            if (eventType.Length == 0) return -1;
            LiveEventCalendarCheckReport report = _services.Session.CheckLane(eventType, preview);
            return report == null ? -1 : report.Findings.Count;
        }

        private static string EventTypeOf(LiveEventCalendarFinding finding, LiveEventCalendarDocument document)
        {
            string entryKey = finding.TargetEntryKey ?? string.Empty;
            return entryKey.Length > 0 && document.TryGetFixedEvent(entryKey, out FixedLiveEventEntry entry)
                ? entry.EventType
                : string.Empty;
        }

        private void ApplyRepair(LiveEventCalendarFinding finding, LiveEventCalendarRepair repair)
        {
            if (repair == null || repair.Edit == null) return;
            string message = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarRepairToastFormat, LiveOpsFindingText.RepairOptionText(finding, repair, _services.Format));
            _presenter.ApplyEdit(repair.Edit, LiveOpsEditOperation.ApplyProposal, finding.TargetEntryKey, message, string.Empty);
        }

        // ---------------------------------------------------------------------------------------------------- menu chuột phải

        /// <summary>Chuột phải trên trục: chọn đúng menu theo thứ trúng, dựng từ dữ liệu của <see cref="CalendarContextMenus"/>.</summary>
        private void OnTimelineContextRequested(LiveOpsTimelineContextRequest request)
        {
            if (request == null || _timeline == null) return;
            LiveOpsTimelineHit hit = request.Hit;
            CalendarMenuContext context = BuildMenuContext(hit);
            IReadOnlyList<CalendarMenuItem> items = ItemsFor(hit, context);
            if (items == null || items.Count == 0) return;
            ShowContextMenu(items, request.WorldPosition, id => ActivateMenuItem(id, context));
        }

        private IReadOnlyList<CalendarMenuItem> ItemsFor(LiveOpsTimelineHit hit, CalendarMenuContext context)
        {
            switch (hit.Kind)
            {
                case LiveOpsTimelineHitKind.Bar:
                {
                    LiveOpsTimelineBarModel bar = _presenter.FindBar(hit.BarKey);
                    if (bar == null || bar.IsStrip) return null;
                    return bar.Source == LiveOpsTimelineBarSource.Fixed
                        ? CalendarContextMenus.ForFixedBar(context)
                        : CalendarContextMenus.ForRecurringBar(context);
                }
                case LiveOpsTimelineHitKind.LaneHeader:
                    return CalendarContextMenus.ForLaneHeader(context);
                case LiveOpsTimelineHitKind.EmptyLane:
                    return IsRecurringLane(context.LaneTypeId) ? null : CalendarContextMenus.ForEmptyLane(context);
                default:
                    return null;
            }
        }

        private CalendarMenuContext BuildMenuContext(LiveOpsTimelineHit hit)
        {
            CalendarMenuContext context = new CalendarMenuContext
            {
                BarKey = hit.BarKey,
                LaneTypeId = hit.LaneTypeId,
                CursorTimeText = hit.TimeUtc.HasValue ? _services.Format.ShortDateTime(hit.TimeUtc.Value) : string.Empty,
                HasCopiedEvent = _commandHandler.HasCopiedEvent,
                CanMoveLaneUp = _commandHandler.CanMoveLane(hit.LaneTypeId, MoveLaneIntent.Up),
                CanMoveLaneDown = _commandHandler.CanMoveLane(hit.LaneTypeId, MoveLaneIntent.Down),
                CanRevertToCompare = _commandHandler.CanRevertToCompare(hit.BarKey),
                CompareSource = _services.Session.Publish == null
                    ? LiveOpsHubCompareSource.Published
                    : _services.Session.Publish.ActiveCompareSource,
            };
            if (_commandHandler.TryGetDuplicateTarget(hit.BarKey, out FixedLiveEventEntry _, out DateTime targetStartUtc,
                out TimeSpan offset))
            {
                context.CanDuplicate = true;
                context.DuplicateTargetText = _services.Format.ShortDateTime(targetStartUtc);
                context.DuplicateOffsetText = _services.Format.Duration(offset, true);
            }
            return context;
        }

        /// <summary>
        /// Menu chuột phải trên trục dùng <see cref="GenericMenu"/> (menu gốc của Editor) vì chỗ bấm không phải một element có
        /// <c>ContextualMenuManipulator</c> — timeline tự bắt chuột phải để biết trúng thanh, header làn hay chỗ trống. Nội dung
        /// vẫn là danh sách của <see cref="CalendarContextMenus"/>, nên test Logic đọc đúng thứ menu này hiện.
        /// </summary>
        private static void ShowContextMenu(IReadOnlyList<CalendarMenuItem> items, Vector2 worldPosition,
            Action<CalendarMenuItemId> activate)
        {
            GenericMenu menu = new GenericMenu();
            for (int index = 0; index < items.Count; index++)
            {
                CalendarMenuItem item = items[index];
                if (item.IsSeparator)
                {
                    menu.AddSeparator(string.Empty);
                    continue;
                }
                GUIContent content = new GUIContent(item.Text);
                if (!item.IsEnabled)
                {
                    menu.AddDisabledItem(content);
                    continue;
                }
                CalendarMenuItemId id = item.Id;
                menu.AddItem(content, false, () => activate?.Invoke(id));
            }
            menu.DropDown(new Rect(worldPosition, Vector2.zero));
        }

        private bool IsRecurringLane(string typeId)
        {
            LiveEventCalendarDocument document = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            return typeId.Length > 0 && document.TryGetRecurringRule(typeId, out RecurringLiveEventRule _);
        }

        /// <summary>Một mục menu đã bấm; mọi việc thật nằm ở <see cref="CalendarCommandHandler"/> hoặc presenter.</summary>
        private void ActivateMenuItem(CalendarMenuItemId id, CalendarMenuContext context)
        {
            switch (id)
            {
                case CalendarMenuItemId.EditInInspector:
                    _presenter.SetSelectedBarKey(context.BarKey);
                    _isComparePaneOpen = false;
                    ApplyPaneVisibility();
                    _inspector?.FocusFirstField();
                    break;
                case CalendarMenuItemId.Duplicate:
                    _commandHandler.RequestDuplicate(context.BarKey);
                    break;
                case CalendarMenuItemId.Frame:
                    _presenter.SetSelectedBarKey(context.BarKey);
                    FrameSelected();
                    break;
                case CalendarMenuItemId.MoveStart:
                case CalendarMenuItemId.SetDuration:
                    _presenter.SetSelectedBarKey(context.BarKey);
                    _inspector?.FocusFirstField();
                    break;
                case CalendarMenuItemId.RevertToCompare:
                    _commandHandler.RevertToCompare(context.BarKey);
                    break;
                case CalendarMenuItemId.CopyId:
                    _commandHandler.CopyEventId(context.BarKey);
                    break;
                case CalendarMenuItemId.CopyEventJson:
                    if (_commandHandler.CopyEventJson(context.BarKey) && _timeline != null) _timeline.HasCopiedEvent = true;
                    break;
                case CalendarMenuItemId.Delete:
                    _presenter.RequestDelete(context.BarKey);
                    break;
                case CalendarMenuItemId.OpenRule:
                    RaiseNavigation(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.RecurringRules)
                        .WithEventType(context.LaneTypeId, true));
                    break;
                case CalendarMenuItemId.HideLane:
                    _commandHandler.HideLane(context.LaneTypeId);
                    break;
                case CalendarMenuItemId.MoveLaneUp:
                    _commandHandler.MoveLane(context.LaneTypeId, MoveLaneIntent.Up);
                    break;
                case CalendarMenuItemId.MoveLaneDown:
                    _commandHandler.MoveLane(context.LaneTypeId, MoveLaneIntent.Down);
                    break;
                case CalendarMenuItemId.ShowAllLanes:
                    _commandHandler.ShowAllLanes();
                    break;
                case CalendarMenuItemId.AddForLane:
                    OnAddEventRequestedAt(context.LaneTypeId, RangeStartUtc(), null);
                    break;
                case CalendarMenuItemId.OpenEventTypes:
                    RaiseNavigation(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.EventTypes)
                        .WithEventType(context.LaneTypeId, true));
                    break;
                case CalendarMenuItemId.AddAtCursor:
                    if (_timeline != null && _timeline.CursorUtc.HasValue)
                    {
                        OnAddEventRequestedAt(context.LaneTypeId, _timeline.CursorUtc.Value, null);
                    }
                    break;
                case CalendarMenuItemId.PasteAtCursor:
                    if (_timeline != null && _timeline.CursorUtc.HasValue)
                    {
                        _commandHandler.PasteAt(context.LaneTypeId, _timeline.CursorUtc.Value);
                    }
                    break;
            }
        }

        // ---------------------------------------------------------------------------------------------------- pane So với

        private void OnCompareRowActivated(string entryKey)
        {
            if (entryKey.Length == 0) return;
            _presenter.SetSelectedBarKey(entryKey);
            FrameSelected();
            _comparePane?.SetSelectedEntryKey(entryKey);
        }

        private void OnCompareRowContextRequested(string entryKey, ContextualMenuPopulateEvent menuEvent)
        {
            if (entryKey.Length == 0) return;
            CalendarMenuContext context = new CalendarMenuContext
            {
                BarKey = entryKey,
                CanRevertToCompare = _commandHandler.CanRevertToCompare(entryKey),
                CompareSource = _services.Session.Publish == null
                    ? LiveOpsHubCompareSource.Published
                    : _services.Session.Publish.ActiveCompareSource,
            };
            CalendarContextMenus.Populate(menuEvent.menu, CalendarContextMenus.ForCompareRow(context),
                id => ActivateMenuItem(id, context));
        }

        /// <summary>Chip "Không đặt được (n)" ở header làn: mở pane Danh sách và chọn đúng dòng [SD1 §3.1].</summary>
        private void OnUnplaceableRequested(string laneTypeId)
        {
            _isListPaneOpen = true;
            _toolbar?.SetListPaneOpenWithoutNotify(true);
            ApplyPaneVisibility();
            LiveEventCalendarDocument document = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            IReadOnlyList<FixedLiveEventEntry> entries = document.FixedEvents;
            for (int index = 0; index < entries.Count; index++)
            {
                FixedLiveEventEntry entry = entries[index];
                if (!string.Equals(entry.EventType, laneTypeId, StringComparison.Ordinal)) continue;
                if (entry.TryGetStartUtc(out DateTime _) && entry.TryGetEndUtc(out DateTime _)) continue;
                _presenter.SetSelectedBarKey(entry.EntryKey);
                _listPane?.Select(entry.EntryKey);
                return;
            }
        }

        /// <summary>Nhân bản: popover Thêm đợt mở thẳng ở BƯỚC XEM LẠI với id đề xuất — người dùng sửa trước khi thêm [FD §3.9].</summary>
        private void OnDuplicateRequested(FixedLiveEventEntry entry, DateTime targetStartUtc)
        {
            if (entry == null) return;
            entry.TryGetStartUtc(out DateTime startUtc);
            entry.TryGetEndUtc(out DateTime endUtc);
            int durationHours = endUtc > startUtc ? (int)(endUtc - startUtc).TotalHours : AddEventFlowModel.DefaultDurationHours;
            AddEventFlowModel flow = AddEventFlowModel.CreateAt(_services.Session, _services.Clock.UtcNow, entry.EventType,
                targetStartUtc, durationHours).Next();
            ShowAddEventPopover(flow);
        }

        private void FrameBar(LiveOpsTimelineBarModel bar)
        {
            if (bar == null || _timeline == null) return;
            _timeline.FrameInstance(bar.EventType, bar.StartUtc, bar.EndUtc);
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
            public bool listPaneOpen;
            public bool comparePaneOpen;
        }
    }
}
