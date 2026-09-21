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
        /// <summary>(UX-11) Bậc nâng toast mặc định của màn Lịch, đúng con số trong <c>liveops-hub-feedback.uss</c>.</summary>
        /// <summary>
        /// Bậc nâng toast thường (USS <c>liveops-hub-content--raised-toast</c>). Là <c>internal</c> để test đọc được đúng con số
        /// mà USS dùng — chép lại 64 trong test là hai nguồn sự thật cho một khoảng cách.
        /// </summary>
        internal const float DefaultToastRaisePixels = 64f;

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

        /// <summary>
        /// (UX-02, UJ-01/UJ-23) Khung đang ở thang đo LIÊN TỤC (⌘+lăn, Shift+lăn, căn khung, minimap) chứ không ở một preset.
        /// Trong trạng thái đó <see cref="Refresh"/> KHÔNG được gọi <c>SetRange</c>: <c>SetRange</c> xoá <c>IsContinuousScale</c>
        /// và tự phát <c>RangeChanged</c>, nên mọi lần vẽ lại sẽ kéo trục về preset ngay sau khi người dùng vừa zoom.
        /// </summary>
        private bool _isContinuousRange;

        /// <summary>
        /// (UX-02) Chặn tái nhập: <see cref="Refresh"/> ghi model xuống element, element đổi hình học rồi phát lại
        /// <c>RangeChanged</c> — không có cờ này thì hai bên gọi vòng nhau trong cùng một khung.
        /// </summary>
        private bool _isApplyingRange;

        /// <summary>(UX-11) Gốc màn đang GẮN panel — chỉ gốc này được quyền tắt class nâng toast khi rời panel.</summary>
        private VisualElement _activeRoot;

        /// <summary>
        /// (R-11) Handler <c>DragActiveChanged</c> của LẦN DỰNG view gần nhất. Presenter sống lâu hơn view, nên chỉ <c>+=</c> ở
        /// mỗi <see cref="CreateView"/> là mỗi lần quay lại màn Lịch thêm một handler trỏ vào một hover card đã chết.
        /// </summary>
        private Action<bool> _dragActiveChangedHandler;

        public CalendarSection(LiveOpsHubServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _presenter = new CalendarTimelinePresenter(services);
            _presenter.SelectionChanged += OnSelectionChanged;
            // (W10 — lỗi thật) Chọn NHIỀU thanh đi đường SelectionSetChanged, không đi SelectionChanged. Trước bản này người
            // nghe duy nhất của nó là CalendarEventInspector, nên ở bậc --medium (cửa sổ hẹp hơn
            // LiveOpsHubBreakpoints.MediumBelowWidth = 1100) ApplyInspectorDrawerLayout không chạy lại và drawer giữ nguyên
            // display:none: người dùng ctrl-click hai thanh ở BỐN trong bảy cỡ của cổng (700, 820, 950, 1024) thì pane chọn
            // nhiều không bao giờ hiện ra.
            _presenter.SelectionSetChanged += OnSelectionSetChanged;
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
            // ⌘C đi qua ý định của timeline, nên timeline phải được BÁO là đã có đợt trong clipboard — không thì ⌘V tự tắt
            // ngay sau khi người dùng vừa ⌘C.
            _commandHandler.CopiedEventChanged += () =>
            {
                if (_timeline != null) _timeline.HasCopiedEvent = _commandHandler.HasCopiedEvent;
            };
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
            // (UX-01) Gốc do section dựng cũng phải giãn: CloneTree đặt `calendar-root` làm CON của nó, nên nếu nó co về chiều
            // cao nội tại thì mọi flex-grow bên dưới đều giãn trong một cái hộp cao 0.
            _root.AddToClassList(LiveOpsHubClassNames.CalendarDepthFillHeight);
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
            // (UX-11) Ghi lại ĐÂY là gốc đang sống: view cũ của lần dựng trước detach SAU khi view này đã bật class, nên nếu
            // detach nào cũng tắt class thì yêu cầu của view đang sống bị xoá và toast rơi xuống dưới minimap.
            _activeRoot = _root;
            ApplyToastRaise();

            _services.Session.DocumentChanged += OnSessionChanged;
            _services.Session.CheckChanged += OnSessionChanged;
            // (UX-11, R-01) Gắn lại panel phải KHẲNG ĐỊNH LẠI lớp nâng toast. Class chỉ được bật MỘT lần trong CreateView, mà
            // cửa sổ dựng lại cây (đổi cha khi docking, domain reload, đổi cỡ làm shell thay khung) khiến chính gốc màn rời panel
            // rồi vào lại: lần rời tắt class, không ai bật lại, toast rơi xuống dưới minimap. Ảnh chụp lượt trước là bằng chứng —
            // `hub-content` ở h01/h11/h28f chỉ còn bậc cao (bật muộn theo hình học) và ở h13 không còn class nâng nào.
            _root.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            _root.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel, TrickleDown.TrickleDown);
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            // Esc đóng drawer [SD1 §3.9] — nghe ở pha NỔI BỌT (không TrickleDown) để timeline huỷ cử chỉ kéo trước: đang kéo mà
            // Esc lại đóng drawer là mất luôn đường huỷ kéo, thứ SPIKE-B coi là lối thoát an toàn.
            _root.RegisterCallback<KeyDownEvent>(OnRootKeyDown);
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
            _toolbar.ZoomChanged += ApplyZoomPreset;
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
            // (UX-12, UJ-07) Nút "Hiện" của chip làn ẩn đi đúng đường lệnh của mục menu "Hiện tất cả làn".
            _toolbar.ShowHiddenLanesRequested += () => _commandHandler.ShowAllLanes();
            _toolbar.SearchChanged += OnSearchChanged;
            _toolbar.SearchSubmitted += OnSearchSubmitted;
            // Dải chú giải bị USS ẩn ở cửa sổ hẹp; mục "Chú giải" của menu ⋮ gắn class thắng luật ẩn đó lên chính dải.
            _toolbar.LegendVisibilityChanged += isVisible =>
                _timeline?.Legend.EnableInClassList(LiveOpsHubClassNames.CalendarDepthLegendShown, isVisible);
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
            // (UX-05, UJ-05) Kéo thanh thì tắt hover card: lúc kéo element giữ pointer capture và mỗi bước xem trước thay thanh
            // bằng element mới, nên PointerLeave của thanh cũ không bao giờ tới và thẻ treo lại che đúng thanh vừa thả.
            // (R-11) Gỡ handler của lần dựng trước rồi mới gắn handler mới — không thì mỗi lần quay lại màn Lịch để lại một
            // handler trỏ vào hover card của view đã chết.
            if (_dragActiveChangedHandler != null) _presenter.DragActiveChanged -= _dragActiveChangedHandler;
            _dragActiveChangedHandler = isDragging => _hoverCardHost?.Suppress(isDragging);
            _presenter.DragActiveChanged += _dragActiveChangedHandler;
            _timelineColumn.Add(_timeline);
            // (J2-01) Chân màn đổi hình học MÀ GỐC MÀN KHÔNG đổi: ngăn kéo inspector mở làm dải chú giải hẹp lại rồi gập thêm hàng,
            // minimap và chú giải trèo lên, còn gốc màn vẫn đúng khung cũ nên KHÔNG có GeometryChangedEvent nào trên gốc. Đo ở đó một mình
            // thì số đo trễ một lượt bố cục và toast đậu theo chỗ chân màn đứng Ở LƯỢT TRƯỚC — đo được 20px lệch ở 1024, đủ để đè minimap.
            // Nghe trên CHÍNH ba phần tử chân màn: chúng là thứ duy nhất biết mình vừa dịch.
            _timeline.Minimap.RegisterCallback<GeometryChangedEvent>(OnFooterGeometryChanged);
            _timeline.Legend.RegisterCallback<GeometryChangedEvent>(OnFooterGeometryChanged);
            _timeline.HintLine.RegisterCallback<GeometryChangedEvent>(OnFooterGeometryChanged);
            ApplySnapStep();
        }

        /// <summary>(J2-01) Một phần tử chân màn vừa dịch hoặc đổi chiều cao — đo lại chỗ đậu của toast.</summary>
        private void OnFooterGeometryChanged(GeometryChangedEvent geometryEvent)
        {
            if (_activeRoot == null) return;
            ApplyToastRaiseStep();
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
            // CreateView() chạy lại mỗi lần người dùng quay về màn này, còn presenter sống suốt đời màn: inspector cũ phải nhả
            // đăng ký SelectionSetChanged, không thì mỗi lần vào màn lại thêm một người nghe vẽ lên cây element đã chết.
            _inspector?.Detach();
            _inspector = new CalendarEventInspector(_services, _presenter, title, body);
            _inspector.AddEventRequested += OpenAddEventPopover;
            _inspector.NavigationRequested += RaiseNavigation;
            _inspector.ProposalRequested = OpenProposalPopover;
            _inspector.CloseDrawerRequested += CloseInspectorDrawer;
        }

        // ============================================================================================================ vẽ lại

        internal void Refresh()
        {
            if (_root == null) return;
            bool hasAsset = _services.Session.Asset != null;
            _empty?.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, hasAsset);
            _main?.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, !hasAsset);
            if (!hasAsset) return;

            // (UX-02, UX-03) Nguồn của khoảng: khi trục đang ở thang đo liên tục thì CHÍNH TRỤC là nguồn — nó vừa đổi vì ⌘+lăn,
            // Shift+lăn, căn khung hoặc vì cửa sổ rộng ra. Ở preset thì màn là nguồn và trục nhận lại qua SetRange.
            bool isContinuous = _isContinuousRange && _timeline != null && _timeline.IsContinuousScale;
            DateTime rangeStartUtc = isContinuous ? _timeline.RangeStartUtc : RangeStartUtc();
            DateTime rangeEndUtc = isContinuous
                ? _timeline.RangeEndUtc
                : LiveOpsTimelineGeometry.AddTicksClamped(rangeStartUtc, LiveOpsTimelineGeometry.RangeLengthOf(_zoom).Ticks);
            _presenter.ContentWidth = _root.layout.width;
            LiveOpsTimelineModel model = _presenter.BuildModel(rangeStartUtc, rangeEndUtc, TrackWidth());
            bool isMultiSelection = _presenter.SelectedBarKeys.Count > 1;
            bool wasApplyingRange = _isApplyingRange;
            _isApplyingRange = true;
            try
            {
            if (_timeline != null)
            {
                if (!isContinuous) _timeline.SetRange(rangeStartUtc, _zoom);
                _timeline.SetModel(model);
                // (G-OPT-TIMELINE) Vẽ lại phải dựng lại ĐÚNG tập đang chọn. Gọi thẳng Select(SelectedBarKey) là thu tập về một
                // thanh — mà Refresh() chạy ngay sau MỌI lệnh sửa (DocumentEdited), kể cả hai lệnh của chính bảng chọn nhiều,
                // nên bảng (c) và các thanh sáng biến mất đúng ở lệnh vừa bấm.
                if (isMultiSelection) _timeline.SelectMany(_presenter.SelectedBarKeys, _presenter.SelectedBarKey, false);
                else _timeline.Select(_presenter.SelectedBarKey, false);
            }
            }
            finally
            {
                _isApplyingRange = wasApplyingRange;
            }
            _toolbar?.SetRange(rangeStartUtc, rangeEndUtc, _services.Clock.UtcNow);
            _toolbar?.SetHiddenLaneCount(_presenter.HiddenLanes.Count);
            if (isMultiSelection) _inspector?.RefreshMultiple(_presenter.SelectedBarKeys);
            else _inspector?.Refresh(_presenter.SelectedBarKey);
            _listPane?.SetDocument(_services.Session.Document, _services.Clock.UtcNow, _presenter.SelectedBarKey);
            RefreshComparePane();
            ApplyInspectorDrawerLayout();
            AttachHoverCards();
        }

        /// <summary>
        /// (UX-07, UJ-06 · lệch thiết kế V-39) Drawer inspector ở <c>--medium</c>: chỉ hiện khi có đợt đang chọn, và khi hiện thì
        /// cột timeline lùi vào đúng bề rộng drawer thay vì bị phủ. Trạng thái chưa chọn từng để một pane rỗng 280px đứng cạnh
        /// trục; drawer mở từng phủ lên mọi thứ neo mép phải (chip "Đợt tới", nhãn "chồng n giờ", đuôi gợi ý).
        /// </summary>
        private void ApplyInspectorDrawerLayout()
        {
            if (_inspectorRoot == null) return;
            bool isDrawerMode = IsMediumWidth();
            bool hasSelection = _presenter.SelectedBarKey.Length > 0;
            bool isDrawerOpen = isDrawerMode && hasSelection && !_isComparePaneOpen;
            bool isHidden = _isComparePaneOpen || (isDrawerMode && !hasSelection);
            _inspectorRoot.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, isHidden);
            _split?.EnableInClassList(LiveOpsHubClassNames.CalendarDepthDrawerOpen, isDrawerOpen);
        }

        /// <summary>Cửa sổ ở bậc <c>--medium</c> theo class của root hub; chưa gắn panel thì đo bề rộng thân màn.</summary>
        private bool IsMediumWidth()
        {
            VisualElement hubRoot = FindHubRoot();
            if (hubRoot != null) return hubRoot.ClassListContains(LiveOpsHubClassNames.Medium);
            return _root != null && _root.resolvedStyle.width > 0f
                && _root.resolvedStyle.width < LiveOpsHubBreakpoints.MediumBelowWidth;
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
            _comparePane?.Root.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, !_isComparePaneOpen);
            ApplyInspectorDrawerLayout();
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
            // Bước khoảng là lệnh của PRESET: rời thang đo liên tục để trục nhận lại đúng độ dài khoảng của zoom đang chọn.
            _isContinuousRange = false;
            Refresh();
        }

        private void GoToToday()
        {
            _hasRangeStart = false;
            _isContinuousRange = false;
            Refresh();
        }

        /// <summary>
        /// (UX-02, UX-03 · UJ-01, UJ-02, UJ-23) Trục vừa đổi khoảng — vì ⌘+lăn, Shift+lăn, căn khung, minimap, hoặc vì bề rộng
        /// track đổi khi cửa sổ co giãn. Trước đây chỗ này chỉ lưu giờ bắt đầu và đổi nhãn toolbar, nên model (thước, thanh, làn)
        /// vẫn là model của lần dựng trước: người dùng thấy nhãn khoảng nhảy còn trục đứng yên.
        /// </summary>
        private void OnTimelineRangeChanged(DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            if (_isApplyingRange) return;
            _rangeStartUtc = rangeStartUtc;
            _hasRangeStart = true;
            _isContinuousRange = _timeline != null && _timeline.IsContinuousScale;
            Refresh();
        }

        /// <summary>
        /// (UX-02, UJ-23) Bấm một tab zoom sau khi đã ⌘+lăn: quay về preset và NEO theo tâm khung đang xem — hoặc theo bây giờ
        /// nếu bây giờ nằm trong khung. Giữ nguyên mốc trái cũ làm khung nhảy đi chỗ khác và vạch "bây giờ" biến mất.
        /// </summary>
        private void ApplyZoomPreset(LiveOpsTimelineZoom zoom)
        {
            DateTime anchorUtc = CurrentAnchorUtc();
            _zoom = zoom;
            _isContinuousRange = false;
            TimeSpan length = LiveOpsTimelineGeometry.RangeLengthOf(zoom);
            _rangeStartUtc = LiveOpsTimelineGeometry.AddTicksClamped(anchorUtc, -(length.Ticks / 2L));
            _hasRangeStart = true;
            Refresh();
        }

        /// <summary>Tâm khung đang xem, hoặc bây giờ khi bây giờ còn nằm trong khung (thứ người dùng đang nhìn).</summary>
        private DateTime CurrentAnchorUtc()
        {
            DateTime nowUtc = _services.Clock.UtcNow;
            if (_timeline == null) return _hasRangeStart ? _rangeStartUtc : nowUtc;
            DateTime startUtc = _timeline.RangeStartUtc;
            DateTime endUtc = _timeline.RangeEndUtc;
            if (endUtc <= startUtc) return nowUtc;
            if (nowUtc >= startUtc && nowUtc < endUtc) return nowUtc;
            return LiveOpsTimelineGeometry.AddTicksClamped(startUtc, (endUtc - startUtc).Ticks / 2L);
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
            // (UX-07) Drawer "mở khi chọn": bỏ chọn ở --medium phải giấu hẳn pane, không để một pane rỗng 280px cạnh trục.
            ApplyInspectorDrawerLayout();
        }

        /// <summary>
        /// (soát W10) Tập chọn NHIỀU đợt cũng là "có đợt đang chọn", nên drawer phải mở lại y như lần chọn một đợt.
        /// <para>
        /// Vì sao cần một người nghe RIÊNG ở màn: <see cref="CalendarTimelinePresenter.SetSelectedBarKeys"/> cố ý KHÔNG phát
        /// <c>SelectionChanged</c> (phát là tự tay thu tập về một thanh), và người nghe duy nhất của
        /// <c>SelectionSetChanged</c> trước đây là <see cref="CalendarEventInspector"/> — nó dựng lại NỘI DUNG pane nhưng
        /// không ai chạy lại <see cref="ApplyInspectorDrawerLayout"/>, nên ở cửa sổ hẹp hơn
        /// <see cref="LiveOpsHubBreakpoints.MediumBelowWidth"/> pane giữ nguyên <c>liveops-hub-calendar--hidden</c>: người
        /// dùng ctrl-click hai thanh và KHÔNG thấy gì hiện ra, ở bốn trong bảy cỡ của cổng (700, 820, 950, 1024).
        /// </para>
        /// <para>
        /// Chỉ chạy lại phần BỐ CỤC: nội dung pane do inspector tự vẽ (nó nghe cùng sự kiện), và các thanh sáng trên trục do
        /// chính cử chỉ vừa rồi đặt — gọi lại <c>SelectMany</c> ở đây là vẽ đè lên thứ vừa đúng.
        /// </para>
        /// </summary>
        private void OnSelectionSetChanged(IReadOnlyList<string> barKeys)
        {
            ApplyInspectorDrawerLayout();
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
            _isContinuousRange = false;
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
                _isContinuousRange = false;
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
            // (UX-05) Lần vẽ trước đã thay mọi thanh bằng element mới: builder của element chết phải đi, và nếu thẻ đang hiện
            // thuộc về một thanh đã rời cây thì nó phải tắt — không thì thẻ treo lại che đúng thanh vừa dựng.
            _hoverCardHost.PruneDetachedTargets();
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

        /// <summary>(UX-15) Test đọc thẳng ngữ cảnh menu mà màn tính ra — nhãn menu gốc của Unity không chụp ảnh được (S-24).</summary>
        internal CalendarMenuContext BuildMenuContextForTest(LiveOpsTimelineHit hit)
        {
            return BuildMenuContext(hit);
        }

        private CalendarMenuContext BuildMenuContext(LiveOpsTimelineHit hit)
        {
            LiveEventCalendarDocument compareDocument = _services.Session.Publish == null
                ? null
                : _services.Session.Publish.CompareDocument;
            CalendarMenuContext context = new CalendarMenuContext
            {
                BarKey = hit.BarKey,
                LaneTypeId = hit.LaneTypeId,
                CursorTimeText = hit.TimeUtc.HasValue ? _services.Format.ShortDateTime(hit.TimeUtc.Value) : string.Empty,
                HasCopiedEvent = _commandHandler.HasCopiedEvent,
                IsLaneCollapsed = _presenter.IsLaneCollapsed(hit.LaneTypeId),
                CanMoveLaneUp = _commandHandler.CanMoveLane(hit.LaneTypeId, MoveLaneIntent.Up),
                CanMoveLaneDown = _commandHandler.CanMoveLane(hit.LaneTypeId, MoveLaneIntent.Down),
                CanRevertToCompare = _commandHandler.CanRevertToCompare(hit.BarKey),
                CompareSource = _services.Session.Publish == null
                    ? LiveOpsHubCompareSource.Published
                    : _services.Session.Publish.ActiveCompareSource,
                // (UX-15) Ba cờ để menu nói đúng: làn lặp không nhận đợt cố định, không làn ẩn thì "Hiện tất cả làn" vô nghĩa,
                // và lý do khoá mục Hoàn về phân biệt "chưa đăng lần nào" với "đợt này chưa có trong bản đã đăng".
                IsRecurringLane = IsRecurringLane(hit.LaneTypeId),
                HiddenLaneCount = _presenter.HiddenLanes.Count,
                HasCompareDocument = compareDocument != null,
                IsInCompareDocument = compareDocument != null && hit.BarKey.Length > 0
                    && compareDocument.TryGetFixedEvent(hit.BarKey, out FixedLiveEventEntry _),
            };
            if (_commandHandler.TryGetDuplicateTarget(hit.BarKey, out FixedLiveEventEntry _, out DateTime targetStartUtc,
                out TimeSpan offset))
            {
                context.CanDuplicate = true;
                context.DuplicateTargetText = _services.Format.ShortDateTime(targetStartUtc);
                // (UX-15, UJ-18) Dạng ĐỦ CHỮ ("2 ngày 12 giờ"): "(+2n 12g)" là dạng của readout khi kéo — ở đó chỗ hẹp và người
                // đọc đang nhìn con trỏ; trong menu thì không thiếu chỗ, chỉ thiếu nghĩa.
                context.DuplicateOffsetText = _services.Format.Duration(offset, false);
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
            // Dựng mục qua CalendarContextMenus: '/' trong nhãn ngày giờ ("24/9 00:00") phải được né trước, không thì GenericMenu
            // bẻ mục thành menu con và hai mục quan trọng nhất của [FD §3.9] biến mất khỏi tầng một.
            CalendarContextMenus.PopulateGenericMenu(menu, items, activate);
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
                case CalendarMenuItemId.ToggleLaneCollapsed:
                    // Đi qua ý định của timeline chứ không gọi thẳng presenter: cùng một đường với mọi cử chỉ khác của trục,
                    // nên test đọc được một chỗ duy nhất và ảnh chụp dựng lại được bằng chính intent đó (G-OPT-TIMELINE).
                    _timeline?.RequestToggleLaneCollapsed(context.LaneTypeId, !context.IsLaneCollapsed);
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
            LiveEventCalendarDocument compareDocument = _services.Session.Publish == null
                ? null
                : _services.Session.Publish.CompareDocument;
            CalendarMenuContext context = new CalendarMenuContext
            {
                BarKey = entryKey,
                CanRevertToCompare = _commandHandler.CanRevertToCompare(entryKey),
                CompareSource = _services.Session.Publish == null
                    ? LiveOpsHubCompareSource.Published
                    : _services.Session.Publish.ActiveCompareSource,
                HasCompareDocument = compareDocument != null,
                IsInCompareDocument = compareDocument != null
                    && compareDocument.TryGetFixedEvent(entryKey, out FixedLiveEventEntry _),
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

        /// <summary>
        /// Đóng drawer [SD1 §3.9]: bỏ chọn đợt. Drawer "mở khi chọn", nên cách đóng thật thà nhất là bỏ lựa chọn — giấu riêng
        /// element sẽ để lại một đợt đang chọn trên trục mà không pane nào nói nó là đợt nào.
        /// </summary>
        private void CloseInspectorDrawer()
        {
            _presenter.SetSelectedBarKey(string.Empty);
        }

        /// <summary>
        /// Drawer inspector đang mở: cửa sổ ở <c>--medium</c>, pane So với đang đóng (hai pane loại trừ nhau) và có đợt đang chọn —
        /// đúng ba điều kiện làm inspector phủ lên trục. Ở cửa sổ rộng inspector là cột cố định, Esc không có gì để đóng.
        /// </summary>
        internal bool IsInspectorDrawerOpen
        {
            get
            {
                if (_root == null || _isComparePaneOpen || _presenter.SelectedBarKey.Length == 0) return false;
                VisualElement hubRoot = FindHubRoot();
                return hubRoot != null
                    ? hubRoot.ClassListContains(LiveOpsHubClassNames.Medium)
                    : _root.resolvedStyle.width > 0f && _root.resolvedStyle.width < LiveOpsHubBreakpoints.MediumBelowWidth;
            }
        }

        /// <summary>
        /// Test gọi thẳng nhánh phím của gốc màn. Không gửi <c>KeyDownEvent</c> qua <c>SendEvent</c>: ở 2022.3 phím được dispatch
        /// theo ELEMENT ĐANG FOCUS chứ không theo target đã gán, nên cùng một test xanh ở 6000.6 và đỏ ở 2022.3 — đúng bẫy mà
        /// <see cref="CalendarToolbar.HandleSearchKeyDownForTest"/> đã ghi.
        /// </summary>
        internal void HandleRootKeyDownForTest(KeyDownEvent keyEvent)
        {
            OnRootKeyDown(keyEvent);
        }

        /// <summary>Esc khi drawer mở = đóng drawer, đúng lời hứa của tooltip "Đóng (Esc)".</summary>
        private void OnRootKeyDown(KeyDownEvent keyEvent)
        {
            if (keyEvent.keyCode != KeyCode.Escape || !IsInspectorDrawerOpen) return;
            keyEvent.StopPropagation();
            CloseInspectorDrawer();
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
            _toolbar?.SetNarrow(IsNarrowWidth());
            ApplyInspectorDrawerLayout();
            // (UX-11, R-01) Khẳng định lại CẢ HAI lớp nâng toast, không chỉ bậc cao. Gốc của lỗi: cửa sổ hub gọi ShowSection
            // (bước 6 của CreateGUI) TRƯỚC SubscribeServices (bước 8), nên lần bật lớp nền trong CreateView rơi vào một bus chưa
            // ai nghe — còn bậc cao thì bật muộn theo hình học nên vẫn ăn. Đó đúng là ảnh chụp lượt trước: `hub-content` chỉ có
            // `--raised-toast-tall`, và ở 820px (chú giải ẩn ⇒ chân thấp ⇒ tắt bậc cao) thì không còn lớp nào.
            ApplyToastRaise();
        }

        /// <summary>
        /// (UX-11, C8/V8) Bậc nâng toast theo chân màn ĐO ĐƯỢC (minimap + chú giải + gợi ý), không theo hằng 64px: ở cửa sổ thật
        /// chú giải xuống hai dòng và chân cao 75px, nên toast đè lên đúng hai dòng đó.
        /// </summary>
        private void ApplyToastRaiseStep()
        {
            if (_timeline == null || _timelineColumn == null) return;
            float footerHeight = FooterHeight();
            bool isTall = footerHeight > DefaultToastRaisePixels;
            _services.Bus.SetContentClass(LiveOpsHubClassNames.CalendarDepthContentRaisedToastTall, isTall);
            // (J2-01) Hai bậc class ở trên là SÀN TRƯỚC KHI ĐO (màn vừa dựng, chưa có hình học); con số dưới đây là chỗ ĐÚNG.
            // Vì sao hai bậc không đủ: chiều cao chân màn đổi theo SỐ HÀNG mà dải chú giải gập, mà số hàng ấy đổi theo bề ngang
            // CÒN LẠI sau khi ngăn kéo inspector mở. Ở 820 ngăn kéo mở, chú giải rộng 504px và cao 68px thay vì 36px, mép trên của nó
            // trèo từ y=512 lên y=470 — giữa hai bậc 64 và 96, nên CẢ HAI đều sai và toast đè 452×24 lên hàng đầu chú giải (phiếu J2-01).
            _services.Bus.SetToastFloor(FooterTopWorldY());
        }

        /// <summary>
        /// (UX-11, R-01) Khẳng định cả HAI lớp nâng toast cho màn đang sống: lớp nền luôn bật (toast không bao giờ được rơi về
        /// 8px khi đang ở màn Lịch), bậc cao bật thêm khi chân màn đo được cao hơn 64px. Gọi ở CreateView VÀ ở mỗi lần gắn panel.
        /// </summary>
        private void ApplyToastRaise()
        {
            _services.Bus.SetContentClass(LiveOpsHubClassNames.ContentRaisedToast, true);
            ApplyToastRaiseStep();
        }

        /// <summary>Chiều cao chân màn = từ mép trên của phần nổi cao nhất (minimap/chú giải/gợi ý) tới đáy cột timeline.</summary>
        private float FooterHeight()
        {
            Rect column = _timelineColumn.worldBound;
            if (float.IsNaN(column.yMax)) return 0f;
            return column.yMax - FooterTopWorldYOf(column);
        }

        /// <summary>
        /// (J2-01) Mép TRÊN của chân màn ở toạ độ world — thứ cửa sổ cần để đậu toast. Trả <c>float.NaN</c> khi chưa đo được
        /// (bố cục chưa chạy), để cửa sổ giữ nguyên luật USS thay vì nhảy về một con số bịa.
        /// <para>
        /// Đo bằng toạ độ world chứ không bằng chiều cao: màn đo theo CỘT TIMELINE của nó, còn toast đậu theo hộp NỘI DUNG của khung —
        /// hai hộp khác nhau. Gửi chiều cao là bắt cửa sổ phải đoán hai hộp ấy đáy trùng nhau; gửi toạ độ world thì phép trừ ở cửa sổ
        /// đúng kể cả khi khung có padding hay chân trang riêng.
        /// </para>
        /// </summary>
        private float FooterTopWorldY()
        {
            if (_timelineColumn == null || _timeline == null) return float.NaN;
            Rect column = _timelineColumn.worldBound;
            if (float.IsNaN(column.yMax)) return float.NaN;
            return FooterTopWorldYOf(column);
        }

        private float FooterTopWorldYOf(Rect column)
        {
            float top = column.yMax;
            top = Math.Min(top, TopOf(_timeline.Minimap, column.yMax));
            top = Math.Min(top, TopOf(_timeline.Legend, column.yMax));
            top = Math.Min(top, TopOf(_timeline.HintLine, column.yMax));
            return top;
        }

        private static float TopOf(VisualElement element, float fallbackTop)
        {
            if (element == null || element.resolvedStyle.display == DisplayStyle.None) return fallbackTop;
            Rect bound = element.worldBound;
            return float.IsNaN(bound.y) || bound.height <= 0f ? fallbackTop : bound.y;
        }

        /// <summary>
        /// Cửa sổ đang hẹp (< 900) theo class <c>--narrow</c> mà <see cref="LiveOpsHubBreakpoints"/> gắn trên root hub — đo bề rộng
        /// THÂN MÀN sẽ lệch đúng bằng rail, và toolbar phải rút gọn cùng nhịp với USS chứ không theo một con số khác.
        /// </summary>
        private bool IsNarrowWidth()
        {
            VisualElement hubRoot = FindHubRoot();
            if (hubRoot != null) return hubRoot.ClassListContains(LiveOpsHubClassNames.Narrow);
            return _root != null && _root.resolvedStyle.width > 0f && _root.resolvedStyle.width < LiveOpsHubBreakpoints.NarrowBelowWidth;
        }

        private VisualElement FindHubRoot()
        {
            for (VisualElement element = _root; element != null; element = element.parent)
            {
                if (element.ClassListContains(LiveOpsHubClassNames.Root)) return element;
            }
            return null;
        }

        /// <summary>(UX-11, R-01) Gốc màn vào lại panel: bật lại lớp nâng toast mà lần rời panel đã tắt.</summary>
        private void OnAttachToPanel(AttachToPanelEvent attachEvent)
        {
            if (!ReferenceEquals(attachEvent.target, _root)) return;
            _activeRoot = _root;
            ApplyToastRaise();
        }

        /// <summary>Panel biến mất (đổi màn, đóng cửa sổ, domain reload): gỡ nghe phiên và huỷ thao tác kéo đang mở (SP-2 (d)).</summary>
        private void OnDetachFromPanel(DetachFromPanelEvent detachEvent)
        {
            // (R-01) CHỈ nhận lần rời panel của CHÍNH gốc màn. Callback đăng ký TrickleDown nên khi gốc rời panel thì mỗi element
            // con cũng chạy qua đây một lần (vô hại vì HandleDetach chốt theo gốc), nhưng nếu một bản Unity cho detach của element
            // CON trickle lên thì gỡ nghe phiên + AbortDrag theo một thanh bị thay lúc vẽ lại làn sẽ giết cử chỉ kéo đang chạy.
            if (!ReferenceEquals(detachEvent.target, _root)) return;
            HandleDetach(detachEvent.currentTarget as VisualElement);
        }

        /// <summary>(UX-11) Test gọi thẳng nhánh rời panel của MỘT gốc cụ thể — panel giả không phát Detach theo thứ tự thật.</summary>
        internal void HandleDetachForTest(VisualElement root)
        {
            HandleDetach(root);
        }

        private void HandleDetach(VisualElement detachingRoot)
        {
            _services.Session.DocumentChanged -= OnSessionChanged;
            _services.Session.CheckChanged -= OnSessionChanged;
            // PD-21 chỉ đúng KHI đang ở màn Lịch: rời màn mà để class nâng toast thì minimap/chú giải của màn khác cũng bị đẩy
            // xuống dưới toast. Bật ở CreateView thì phải tắt ở đây.
            // (UX-11) Nhưng CHỈ khi gốc rời panel đúng là gốc đang sống: cửa sổ dựng lại màn thì view CŨ detach SAU khi view mới
            // đã bật class, nên tắt theo mọi detach là xoá yêu cầu của view đang hiện.
            if (detachingRoot == null || ReferenceEquals(detachingRoot, _activeRoot))
            {
                _activeRoot = null;
                _services.Bus.SetContentClass(LiveOpsHubClassNames.ContentRaisedToast, false);
                _services.Bus.SetContentClass(LiveOpsHubClassNames.CalendarDepthContentRaisedToastTall, false);
                // (J2-01) Sàn đo được cũng phải trả lại: nó là style inline nên nó thắng mọi luật USS, để lại thì toast của màn
                // KHÁC cũng đậu theo chân của màn Lịch — tức lơ lửng giữa màn, không phải 8px mặc định.
                _services.Bus.SetToastFloor(float.NaN);
            }
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
