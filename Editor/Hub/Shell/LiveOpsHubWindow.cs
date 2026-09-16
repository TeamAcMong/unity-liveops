using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using DreamTech.LiveOps.Unity;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Cửa sổ LiveOps Hub — khung cho 6 màn P1 (8.1, [FD §3]). Khung chỉ biết màn qua <see cref="IHubSection"/>; lỗi của một màn
    /// không kéo sập cửa sổ (card lỗi thay thân màn, rail vẫn dùng được), thiếu bố cục không bao giờ thành cửa sổ trắng (Label
    /// thay cả cửa sổ). G-SESSION thêm services (<see cref="OpenWithServices"/>, <see cref="OpenWithAsset"/>, lưu/huỷ, bus → cửa sổ),
    /// G-HOSTUI thêm chip, palette, toast, phím — trình tự <see cref="CreateGUI"/> đã chừa đúng chỗ cho các bước đó.
    /// </summary>
    public sealed class LiveOpsHubWindow : EditorWindow, IHubHost, IHasCustomMenu
    {
        public const string MenuPath = "Tools/DreamTech/LiveOps/LiveOps Hub";

        /// <summary>SessionState: màn đang mở khi mở lại cửa sổ trong cùng phiên Editor (8.10).</summary>
        internal const string ActiveSectionSessionKey = "DreamTech.LiveOps.Hub.ActiveSection";

        /// <summary>EditorPrefs (theo máy): tắt chuyển động → class <c>liveops-hub--no-motion</c> trên root.</summary>
        internal const string ReduceMotionPreferenceKey = "LiveOpsHub.ReduceMotion";

        /// <summary>Nhịp làm mới health từ <c>EditorApplication.update</c> (8.1 bước 8); throttle 3 giây của từng màn giữ chi phí thấp.</summary>
        internal const double HealthRefreshIntervalSeconds = 1.0;

        /// <summary>Nhịp hỏi trạng thái biên dịch khi note đang hiện — lịch trên view, tự dừng khi view rời panel.</summary>
        internal const long CompilationPollMilliseconds = 500;

        internal const float MinimumWidth = 620f;
        internal const float MinimumHeight = 420f;

        internal const string CompilingNoteElementName = "hub-compiling-note";

        /// <summary>Nhịp hỏi phút hiện tại cho giờ UTC ở status bar; chữ chỉ dựng lại khi phút đổi (8.3).</summary>
        internal const long StatusClockPollMilliseconds = 1000;

        /// <summary>⌘S của Unity sau khi hub đã lưu lịch (8.7) — chuỗi menu, không phải shortcut id.</summary>
        internal const string SaveMenuPath = "File/Save";

        internal const string DiskBannerElementName = "hub-disk-banner";
        internal const string DiskBannerReloadElementName = "hub-disk-banner-reload";
        internal const string DiskBannerDiffElementName = "hub-disk-banner-diff";
        internal const string DiskBannerKeepElementName = "hub-disk-banner-keep";

        [SerializeField] private LiveOpsHubWindowState windowState = new LiveOpsHubWindowState();

        // Tiêm bởi OpenForTest trước Show (CreateGUI chạy trong Show). Không serialize: sau domain reload cửa sổ dựng lại bằng
        // registry và adapter Editor thật — màn giả của test không sống qua reload.
        [NonSerialized] private IReadOnlyList<IHubSection> _injectedSections;
        [NonSerialized] private ILiveOpsHubCompilationState _injectedCompilationState;
        [NonSerialized] private ILiveOpsHubLayoutLoader _injectedLayoutLoader;
        [NonSerialized] private string _pendingSectionId;
        [NonSerialized] private bool _isIsolatedFromSessionState;
        [NonSerialized] private LiveOpsHubServices _injectedServices;
        [NonSerialized] private LiveEventCalendarAsset _pendingAsset;

        // Services sống cùng cửa sổ (không dựng lại mỗi CreateGUI): phiên giữ nháp, bản chụp và lần kiểm đang chạy. Cửa sổ chỉ Dispose
        // services do chính nó dựng — services tiêm từ test/chụp ảnh thuộc về nơi tiêm.
        [NonSerialized] private LiveOpsHubServices _services;
        [NonSerialized] private bool _ownsServices;
        [NonSerialized] private bool _isServicesSubscribed;

        [NonSerialized] private readonly List<IHubSection> _sections = new List<IHubSection>();
        [NonSerialized] private readonly Dictionary<string, LiveOpsHealthThrottle> _throttles = new Dictionary<string, LiveOpsHealthThrottle>(StringComparer.Ordinal);
        [NonSerialized] private readonly HashSet<string> _failedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        [NonSerialized] private ILiveOpsHubCompilationState _compilationState;
        [NonSerialized] private ILiveOpsHubLayoutLoader _layoutLoader;
        [NonSerialized] private ILiveOpsHubClipboard _clipboard;
        [NonSerialized] private ILiveOpsHubFileDialog _fileDialog;
        [NonSerialized] private VisualElement _hubRoot;
        [NonSerialized] private LiveOpsHubSkin _skin;
        [NonSerialized] private LiveOpsHubHeader _header;
        [NonSerialized] private LiveOpsHubRail _rail;
        [NonSerialized] private LiveOpsHubSectionHeader _sectionHeader;
        [NonSerialized] private LiveOpsHubStatusBar _statusBar;
        [NonSerialized] private VisualElement _notes;
        [NonSerialized] private VisualElement _body;
        [NonSerialized] private VisualElement _compilingNote;
        [NonSerialized] private VisualElement _diskBanner;
        [NonSerialized] private VisualElement _outcomeHost;
        [NonSerialized] private LiveOpsHubPalette _palette;
        [NonSerialized] private LiveOpsHoverCardHost _hoverCardHost;
        [NonSerialized] private LiveOpsHubUndoTracker _undoTracker;
        [NonSerialized] private LiveOpsToast _toast;
        [NonSerialized] private LiveOpsOutcomeView _outcomeView;
        [NonSerialized] private IHubSection _currentSection;
        [NonSerialized] private Action _detachBreakpoints;
        [NonSerialized] private double _nextHealthRefreshSeconds;
        [NonSerialized] private bool _isUpdateRegistered;
        [NonSerialized] private StatusBarSignature _statusSignature;
        [NonSerialized] private bool _hasStatusSignature;
        [NonSerialized] private Action _saveMenuCommandForTest;

        // Cửa sổ dữ liệu mẫu (menu ⋮ "Hiện dữ liệu mẫu"): asset DontSave không đường dẫn — người gọi sở hữu và phải
        // DestroyImmediate, vì DontSave gồm cả DontUnloadUnusedAsset nên Unity không bao giờ tự dọn (mục 3, ràng buộc vòng đời).
        [NonSerialized] private LiveEventCalendarAsset _ownedPreviewAsset;
        [NonSerialized] private bool _ownsInjectedServices;
        [NonSerialized] private Action<string> _openUrlForTest;

        IReadOnlyList<IHubSection> IHubHost.Sections => _sections;

        LiveOpsHubServices IHubHost.Services => _services;

        // ------------------------------------------------------------------------------------------------------------ mở

        [MenuItem(MenuPath)]
        private static void OpenFromMenu()
        {
            Open();
        }

        public static LiveOpsHubWindow Open()
        {
            return Open(null);
        }

        /// <param name="sectionId">Màn cần hiện; null/rỗng = màn đã mở gần nhất trong phiên (SessionState) hoặc Tổng quan.</param>
        public static LiveOpsHubWindow Open(string sectionId)
        {
            LiveOpsHubWindow window = GetWindow<LiveOpsHubWindow>();
            window.Show();
            if (!string.IsNullOrEmpty(sectionId))
            {
                if (window._hubRoot != null) window.ShowSection(sectionId);
                else window._pendingSectionId = sectionId;
            }
            return window;
        }

        /// <summary>
        /// Mở hub với asset này (inspector "Mở trong LiveOps Hub"): cửa sổ đã mở thì đổi asset của phiên (và nhớ GUID theo project),
        /// chưa mở thì dựng phiên với asset này thay vì asset đã nhớ.
        /// </summary>
        public static LiveOpsHubWindow OpenWithAsset(LiveEventCalendarAsset asset)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            LiveOpsHubWindow window = GetWindow<LiveOpsHubWindow>();
            // CreateGUI có thể chạy ngay trong GetWindow hoặc ở lần vẽ sau: có phiên thì đổi asset, chưa thì để phiên dựng với asset này.
            if (window._services != null) window._services.Session.TrySelectAsset(asset);
            else window._pendingAsset = asset;
            window.Show();
            return window;
        }

        /// <summary>
        /// Test + chụp ảnh với services dựng sẵn (<c>LiveOpsHubTestServices</c>): <c>CreateInstance</c> + <c>Show</c>, không <c>GetWindow</c>,
        /// không đọc/ghi SessionState màn đang mở. Cửa sổ không Dispose services tiêm vào — nơi tạo services dọn (asset bộ nhớ, SessionState).
        /// </summary>
        internal static LiveOpsHubWindow OpenWithServices(LiveOpsHubServices services, string sectionId)
        {
            return OpenWithServices(services, null, sectionId);
        }

        /// <summary>Như trên, với registry giả (test khung cần màn giả mang health định tuyến từ đúng phiên này).</summary>
        /// <param name="sections">null = registry thật dựng từ <paramref name="services"/>.</param>
        internal static LiveOpsHubWindow OpenWithServices(LiveOpsHubServices services, IReadOnlyList<IHubSection> sections, string sectionId)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            LiveOpsHubWindow window = CreateInstance<LiveOpsHubWindow>();
            window._injectedServices = services;
            window._injectedSections = sections;
            window._pendingSectionId = sectionId;
            window._isIsolatedFromSessionState = true;
            window.Show();
            return window;
        }

        /// <summary>
        /// Test + chụp ảnh trước khi có services (G-SHELL). <c>CreateInstance</c> + <c>Show</c>, không <c>GetWindow</c> — không đụng
        /// layout đã lưu của người dùng và mỗi test có cửa sổ riêng. Cửa sổ test không đọc/ghi SessionState để thứ tự test không
        /// ảnh hưởng màn mở đầu. Từ W3 cửa sổ tự dựng services KHÔNG asset (không tìm asset, không tự kiểm) để khung vẫn có phiên thật.
        /// </summary>
        /// <param name="layoutLoader">null = AssetDatabase (V-16: h28b truyền <see cref="MissingPathsLiveOpsHubLayoutLoader"/>).</param>
        internal static LiveOpsHubWindow OpenForTest(IReadOnlyList<IHubSection> sections, ILiveOpsHubCompilationState compilationState,
            ILiveOpsHubLayoutLoader layoutLoader, string sectionId)
        {
            if (sections == null) throw new ArgumentNullException(nameof(sections));
            LiveOpsHubWindow window = CreateInstance<LiveOpsHubWindow>();
            window._injectedSections = sections;
            window._injectedCompilationState = compilationState;
            window._injectedLayoutLoader = layoutLoader;
            window._pendingSectionId = sectionId;
            window._isIsolatedFromSessionState = true;
            window.Show();
            return window;
        }

        /// <summary>
        /// Menu ⋮ "Hiện dữ liệu mẫu (chỉ để xem giao diện)" ([FD §3.3]): một cửa sổ hub THỨ HAI chạy trên tài liệu mẫu
        /// 13/9/2026 08:47 với đồng hồ đứng yên. Asset mẫu chỉ sống trong bộ nhớ (<c>DontSave</c>, không đường dẫn) nên
        /// không bao giờ ra đĩa, và không ghi Undo — sửa thử ở đây không chen vào lịch sử Undo của lịch thật (R-24).
        /// Cửa sổ này sở hữu cả services lẫn asset và dọn cả hai khi đóng.
        /// </summary>
        internal static LiveOpsHubWindow OpenPreviewSample()
        {
            LiveEventCalendarAsset sampleAsset = LiveOpsHubPreviewSample.CreateAsset();
            LiveOpsHubServices services = new LiveOpsHubServicesBuilder()
                .WithClock(LiveOpsHubPreviewSample.CreateClock())
                .WithTimeZone(LiveOpsHubPreviewSample.CreateTimeZone())
                .WithCalendarAsset(sampleAsset)
                .Build();
            LiveOpsHubWindow window = OpenWithServices(services, LiveOpsHubSections.Ids.Overview);
            window._ownedPreviewAsset = sampleAsset;
            window._ownsInjectedServices = true;
            return window;
        }

        // ------------------------------------------------------------------------------------------------------------ vòng đời

        private void OnEnable()
        {
            ApplyWindowTitle();
            minSize = new Vector2(MinimumWidth, MinimumHeight);
            if (windowState == null) windowState = new LiveOpsHubWindowState();
            // Đăng ký ở OnEnable chứ không ở CreateGUI: chính tay xử lý gọi lại CreateGUI, mà CreateGUI mở đầu bằng
            // TearDownChrome — gỡ đăng ký trong đó sẽ cắt luôn sự kiện đang chạy.
            LiveOpsHubLanguage.Changed -= OnLanguageChanged;
            LiveOpsHubLanguage.Changed += OnLanguageChanged;
        }

        private void OnDisable()
        {
            LiveOpsHubLanguage.Changed -= OnLanguageChanged;
            CaptureCurrentViewState();
            TearDownChrome();
            ReleaseServices();
        }

        private void OnDestroy()
        {
            // OnDisable chạy trước OnDestroy ở mọi đường đóng cửa sổ đã biết; gỡ lần nữa để một đường lạ không để lại handler
            // trỏ vào cửa sổ đã chết (event tĩnh sống lâu hơn cửa sổ).
            LiveOpsHubLanguage.Changed -= OnLanguageChanged;
        }

        private void ApplyWindowTitle()
        {
            titleContent = new GUIContent(LiveOpsHubStrings.ShellWindowTitle, LiveOpsHubIcons.Get(LiveOpsHubPaths.CalendarIconName));
        }

        /// <summary>
        /// Đổi ngôn ngữ đụng chữ ở khắp nơi (tiêu đề cửa sổ, rail, header màn, thân màn, status bar) nên dựng lại cả khung bằng
        /// chính <see cref="CreateGUI"/> — đường này Unity vẫn chạy lại sau mỗi lần nạp script, rẻ và không sót chỗ nào. Trạng
        /// thái view của màn được chụp trước để người dùng không mất chỗ đang cuộn/đang chọn khi chỉ đổi ngôn ngữ.
        /// </summary>
        private void OnLanguageChanged()
        {
            ApplyWindowTitle();
            if (_hubRoot == null && !IsLayoutMissing) return;
            CaptureCurrentViewState();
            CreateGUI();
        }

        private void OnFocus()
        {
            // Dự phòng khi probe skin chưa bắn (8.8): OnFocus chạy khi người dùng quay lại cửa sổ sau khi đổi Theme.
            _skin?.ApplyFromEditorSkin();
            // SP-8b: người dùng quay lại Unity sau khi sửa file ngoài — so hash ngay, không chờ auto refresh.
            CheckDiskOnFocus();
        }

        /// <summary>
        /// Trình tự 8.1: (1) nạp UXML qua loader — thiếu thì Label thay cửa sổ; (2) clone + stylesheet theo thứ tự; (3) Q element
        /// sống còn; (4) class skin + probe; (5) registry + Bind mọi màn trước khi hiện; (6) rail → màn → health; (8) update 1 giây,
        /// breakpoints; (9) trạng thái view đã lưu đi vào màn qua RestoreViewState trước OnShown; (10) note đang biên dịch. Bước 7
        /// (palette, hover card, toast) là của G-HOSTUI.
        /// </summary>
        private void CreateGUI()
        {
            TearDownChrome();
            VisualElement windowRoot = rootVisualElement;
            windowRoot.Clear();

            // (5, dựng trước) Services trước bố cục: loader của bố cục là port của services (V-16), và khung lỗi cũng cần clipboard/hộp file.
            EnsureServices();
            _layoutLoader = _injectedLayoutLoader ?? _services.LayoutLoader;
            _compilationState = _injectedCompilationState ?? _services.CompilationState;
            _clipboard = _services.Clipboard;
            _fileDialog = _services.FileDialog;

            // (1)
            VisualTreeAsset layout = _layoutLoader.LoadVisualTree(LiveOpsHubPaths.ShellUxml);
            if (layout == null)
            {
                IsLayoutMissing = true;
                LiveOpsHubFailureView.ShowMissingLayout(windowRoot, LiveOpsHubPaths.ShellUxml, _layoutLoader, _clipboard, _fileDialog);
                return;
            }

            // (2)
            layout.CloneTree(windowRoot);
            VisualElement hubRoot = windowRoot.Q(LiveOpsHubPaths.ShellElementNames.Root);
            if (hubRoot != null) AttachStyleSheets(hubRoot);

            // (3)
            List<string> missingNames = new List<string>();
            foreach (string elementName in LiveOpsHubPaths.RequiredShellElementNames)
            {
                if (windowRoot.Q(elementName) == null) missingNames.Add(elementName);
            }
            if (missingNames.Count > 0)
            {
                IsLayoutMissing = true;
                LiveOpsHubFailureView.ShowMissingElements(windowRoot, missingNames, _layoutLoader, _clipboard, _fileDialog);
                return;
            }
            IsLayoutMissing = false;
            _hubRoot = hubRoot;

            // (4)
            _skin = LiveOpsHubSkin.Attach(_hubRoot);
            _skin.SkinChanged += OnSkinChanged;
            _hubRoot.EnableInClassList(LiveOpsHubClassNames.NoMotion, EditorPrefs.GetBool(ReduceMotionPreferenceKey, false));

            _header = new LiveOpsHubHeader(_hubRoot);
            _sectionHeader = new LiveOpsHubSectionHeader(_hubRoot);
            _statusBar = new LiveOpsHubStatusBar(_hubRoot);
            _hasStatusSignature = false;
            _notes = _hubRoot.Q(LiveOpsHubPaths.ShellElementNames.ShellNotes);
            _body = _hubRoot.Q(LiveOpsHubPaths.ShellElementNames.SectionBody);
            _outcomeHost = _hubRoot.Q(LiveOpsHubPaths.ShellElementNames.OutcomeHost);
            _rail = new LiveOpsHubRail(_hubRoot, Navigate);
            BuildHeaderInteractions();

            // (5)
            _sections.Clear();
            _throttles.Clear();
            _failedSectionIds.Clear();
            IReadOnlyList<IHubSection> sections = _injectedSections ?? LiveOpsHubSections.Create(_services);
            foreach (IHubSection section in sections)
            {
                if (section == null) continue;
                _sections.Add(section);
                IHubSection captured = section;
                _throttles[section.Id] = new LiveOpsHealthThrottle(captured.GetHealth);
            }
            foreach (IHubSection section in _sections)
            {
                if (section is IHubHostAware hostAware) hostAware.Bind(this);
            }

            // (7) palette + hover card trên root clone; toast và outcome là con cuối cột nội dung. Dựng TRƯỚC khi hiện màn để
            // màn đầu tiên đã có chỗ bắn toast/outcome (bus nối ở bước 8).
            BuildFeedbackHosts();

            // (6)
            RefreshHealth();
            ShowSection(ResolveInitialSectionId());

            // (8)
            SubscribeServices();
            UpdateUnsavedState();
            _detachBreakpoints = LiveOpsHubBreakpoints.Attach(_hubRoot);
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            _isUpdateRegistered = true;
            _nextHealthRefreshSeconds = EditorApplication.timeSinceStartup + HealthRefreshIntervalSeconds;

            // (9) outcome đã lưu qua domain reload hiện lại ngay; băng đĩa dựng lại nếu phiên còn xung đột.
            RefreshOutcomeView();
            UpdateDiskBanner();
            RefreshStatusBar();

            // (10)
            UpdateCompilingNote();
            _notes.schedule.Execute(UpdateCompilingNote).Every(CompilationPollMilliseconds);
            // Giờ ở status bar chỉ đổi mỗi phút: nhịp 1 giây nhưng chỉ dựng lại chữ khi ĐẦU VÀO đổi (8.3, M-6).
            _statusBar.Element.schedule.Execute(RefreshStatusBarIfChanged).Every(StatusClockPollMilliseconds);
        }

        // ------------------------------------------------------------------------------------------------------------ header

        /// <summary>
        /// Nối bấm cho ô "Đi tới màn…" và hai chip. Bấm bằng <c>PointerDownEvent</c> (không <c>Clickable</c>): ba chỗ này là chữ,
        /// không phải nút — gắn manipulator sẽ kéo theo trạng thái nhấn/giữ của Button mà thiết kế không có.
        /// </summary>
        private void BuildHeaderInteractions()
        {
            _header.SetGoToKeyLabel(LiveOpsHubKeyLabels.For(LiveOpsHubShortcuts.OpenPaletteId));
            _header.GoTo.RegisterCallback<PointerDownEvent>(pointerEvent =>
            {
                pointerEvent.StopPropagation();
                OpenPalette();
            });
            _header.AssetChip.RegisterCallback<PointerDownEvent>(pointerEvent =>
            {
                pointerEvent.StopPropagation();
                PingCalendarAssetOrOpenOverview();
            });
            _header.DraftLeftLabel.RegisterCallback<PointerDownEvent>(pointerEvent =>
            {
                // Nửa trái chỉ là nút Lưu ở dạng (a); dạng khác nó là chữ trạng thái nên bấm không được làm gì.
                if (_services == null || !_services.Session.HasUnsavedChanges) return;
                pointerEvent.StopPropagation();
                SaveChanges();
            });
            _header.DraftRightLabel.RegisterCallback<PointerDownEvent>(pointerEvent =>
            {
                if (_header.DraftRightLabel.text.Length == 0) return;
                pointerEvent.StopPropagation();
                Navigate(LiveOpsHubSections.Ids.Export);
            });
        }

        /// <summary>Có asset thì chọn nó trong Project; chưa có thì chip là lối vào Tổng quan để tạo lịch (7.1).</summary>
        private void PingCalendarAssetOrOpenOverview()
        {
            LiveEventCalendarAsset asset = _services != null ? _services.Session.Asset : null;
            if (asset == null)
            {
                Navigate(LiveOpsHubSections.Ids.Overview);
                return;
            }
            EditorGUIUtility.PingObject(asset);
        }

        // ------------------------------------------------------------------------------------------------------------ phản hồi

        /// <summary>
        /// Palette, hover card, toast, outcome (8.1 bước 7). Toast và outcome là con cuối cột nội dung nên chúng nổi trên thân màn
        /// mà không cần z-index (USS không có); palette/scrim nằm trên root clone để phủ cả rail.
        /// </summary>
        private void BuildFeedbackHosts()
        {
            _palette = new LiveOpsHubPalette(_hubRoot, BuildPaletteEntries, OpenPaletteEntry);
            _hoverCardHost = new LiveOpsHoverCardHost(_hubRoot);

            _undoTracker = new LiveOpsHubUndoTracker();
            _toast = new LiveOpsToast(_undoTracker);
            // Thao tác cuối hiện lại ở status bar sau khi toast tắt: nghe Undo/Redo của Editor để chữ "(⌘Z)" mất đúng lúc.
            _undoTracker.UndoRedoPerformed += RefreshStatusBar;

            _outcomeView = new LiveOpsOutcomeView();
            _outcomeView.ActionInvoked += OnOutcomeActionInvoked;
            _outcomeHost.Add(_outcomeView);

            VisualElement content = _hubRoot.Q(LiveOpsHubPaths.ShellElementNames.Content);
            content.Add(_toast);
        }

        /// <summary>
        /// Màn xin bật/tắt một class trên element nội dung (PD-21: nâng toast lên trên minimap và chú giải của màn Lịch).
        /// Không nối dây thì <c>SetContentClass</c> là lệnh rỗng — toast vẫn nằm dưới minimap (phiếu D-6 của cổng W4).
        /// </summary>
        private void OnContentClassRequested(string className, bool enabled)
        {
            if (_hubRoot == null || string.IsNullOrEmpty(className)) return;
            VisualElement content = _hubRoot.Q(LiveOpsHubPaths.ShellElementNames.Content);
            if (content == null) return;
            content.EnableInClassList(className, enabled);
        }

        /// <summary>Mục của palette: 6 màn + id luật của lần kiểm gần nhất ([FD §3.8]). Không bao giờ có lệnh.</summary>
        private IReadOnlyList<LiveOpsPaletteMatcher.Entry> BuildPaletteEntries()
        {
            List<LiveOpsPaletteMatcher.Entry> entries = new List<LiveOpsPaletteMatcher.Entry>();
            foreach (IHubSection section in _sections)
            {
                SectionHealth health = HealthOf(section);
                entries.Add(new LiveOpsPaletteMatcher.Entry(section.Title, PipelineStages.CaptionOf(section.Stage), section.Subtitle,
                    string.Empty, section.Id, health, health.Reason));
            }

            LiveOpsHubCheckState check = _services != null ? _services.Session.Check : null;
            LiveEventCalendarCheckReport report = check != null ? check.LastReport : null;
            if (report == null) return entries;

            IHubSection validation = FindSection(LiveOpsHubSections.Ids.Validation);
            if (validation == null) return entries;
            SectionHealth validationHealth = HealthOf(validation);
            foreach (LiveEventCalendarRuleResult result in report.RuleResults)
            {
                // Gõ "overlap" dẫn tới Kiểm lịch đã lọc đúng luật đó — id luật là thứ người dùng nhớ, không phải tên màn.
                entries.Add(new LiveOpsPaletteMatcher.Entry(validation.Title, PipelineStages.CaptionOf(validation.Stage), string.Empty,
                    result.RuleId, validation.Id, validationHealth, string.Empty));
            }
            return entries;
        }

        private void OpenPaletteEntry(LiveOpsPaletteMatcher.Entry entry)
        {
            if (entry == null) return;
            LiveOpsHubNavigation navigation = LiveOpsHubNavigation.To(entry.SectionId);
            if (entry.IsRule) navigation = navigation.WithRule(entry.RuleId);
            Navigate(navigation);
        }

        /// <summary>
        /// Toast là ĐƯỜNG DUY NHẤT câu "Vừa làm: …" tới được status bar. Vì sao không đọc
        /// <see cref="LiveOpsHubUndoTracker"/>: phiên mở group Undo thẳng qua <c>Undo.IncrementCurrentGroup</c>
        /// (<c>LiveOpsHubCalendarSession.Apply</c>), không qua <c>BeginGroup</c> của tracker, nên
        /// <c>LastActionText</c> của tracker luôn rỗng ở hub thật và phần " · Vừa làm: …" không bao giờ hiện (H-1).
        /// Câu + số group đi cùng toast nên ghi thẳng vào trạng thái cửa sổ — field đã serialize, sống qua domain reload.
        /// </summary>
        private void OnToastRequested(LiveOpsToastModel toast)
        {
            _toast?.Show(toast);
            if (toast != null && toast.HasUndo)
            {
                windowState.RecentActionText = toast.Message;
                windowState.RecentActionUndoGroup = toast.UndoGroup;
            }
            RefreshStatusBar();
        }

        /// <summary>Outcome nằm trong trạng thái cửa sổ (sống qua domain reload); view chỉ đọc lại từ đó.</summary>
        private void RefreshOutcomeView()
        {
            if (_outcomeView == null) return;
            if (windowState.LastOutcome == null) _outcomeView.ClearRecord();
            else _outcomeView.SetRecord(windowState.LastOutcome);
        }

        /// <summary>Nút trên outcome là một hành động của MÀN đang mở (vd "Mở Xuất JSON") — khung chỉ chuyển tiếp.</summary>
        private void OnOutcomeActionInvoked(string actionId, string actionArgument)
        {
            if (string.IsNullOrEmpty(actionId)) return;
            if (FindSection(actionId) != null) Navigate(actionId);
        }

        // ------------------------------------------------------------------------------------------------------------ status bar

        /// <summary>
        /// Nhịp 1 giây (schedule của status bar và <see cref="RefreshHealth"/>) đi qua đây: chỉ dựng lại chữ khi ĐẦU VÀO đổi.
        /// Bộ lọc "chỉ khi đổi phút" cũ không bao giờ có tác dụng — <see cref="RefreshHealth"/> gọi thẳng
        /// <see cref="RefreshStatusBar"/> mỗi giây và chính hàm đó ghi lại mốc phút, nên mốc luôn "chưa đổi" khi schedule
        /// chạy tới (M-6).
        /// </summary>
        private void RefreshStatusBarIfChanged()
        {
            RefreshStatusBarCore(false);
        }

        /// <summary>Dựng lại chữ ngay, kể cả khi đầu vào không đổi: dựng lại khung, đổi ngôn ngữ, toast, Undo/Redo.</summary>
        internal void RefreshStatusBar()
        {
            RefreshStatusBarCore(true);
        }

        /// <summary>
        /// Một chỗ dựng chữ status bar. <paramref name="force"/> = false thì so chữ ký đầu vào trước: 8.3 nói vế phải đổi mỗi
        /// PHÚT, vế trái chỉ đổi khi lần kiểm / thao tác gần nhất / dấu đã đăng đổi — dựng lại mỗi giây là một
        /// <c>StringBuilder</c> + vài <c>string.Format</c> cho MỖI cửa sổ hub đang mở, để ra đúng chữ cũ. Chữ ký có
        /// <c>CompletedRuleCount</c> nên ca "đang kiểm" vẫn đếm từng luật như trước.
        /// </summary>
        private void RefreshStatusBarCore(bool force)
        {
            if (_statusBar == null) return;
            if (_services == null)
            {
                _statusBar.Clear();
                _hasStatusSignature = false;
                return;
            }
            LiveOpsHubCalendarSession session = _services.Session;
            DateTime nowUtc = _services.Clock.UtcNow;
            string recentActionText = windowState.RecentActionText;
            int recentActionGroup = windowState.RecentActionUndoGroup;
            bool hasRecentAction = recentActionGroup != LiveOpsToastModel.NoUndoGroup && recentActionText.Length > 0;
            if (!hasRecentAction) recentActionText = string.Empty;
            bool isRecentActionOnTop = hasRecentAction && _undoTracker != null && _undoTracker.IsGroupOnTop(recentActionGroup);

            StatusBarSignature signature = new StatusBarSignature(session, nowUtc, recentActionText, isRecentActionOnTop);
            if (!force && _hasStatusSignature && signature.Equals(_statusSignature)) return;
            _statusSignature = signature;
            _hasStatusSignature = true;

            LiveOpsHubStatusBarModel model = LiveOpsHubStatusBarModel.Build(session.Check, session.Asset != null, recentActionText,
                isRecentActionOnTop, nowUtc, session.Publish.ActiveStamp, _services.Format, LiveOpsHubKeyLabels.Undo);
            _statusBar.SetLeft(model.LeftMark, model.LeftText, string.Empty);
            _statusBar.SetRight(model.RightText, model.RightTooltip);
        }

        /// <summary>
        /// Chữ ký đầu vào của status bar: mọi thứ <see cref="LiveOpsHubStatusBarModel.Build"/> đọc, gọn lại thành giá trị so
        /// được. Giờ hiện tại chỉ giữ tới PHÚT vì cả hai vế đều in tới phút (câu "Kiểm lúc …" in giây nhưng lấy từ báo cáo,
        /// không phải từ đồng hồ đang chạy).
        /// </summary>
        private readonly struct StatusBarSignature : IEquatable<StatusBarSignature>
        {
            private readonly bool _hasCheck;
            private readonly bool _hasAsset;
            private readonly bool _isRunning;
            private readonly int _completedRuleCount;
            private readonly int _ruleCount;
            private readonly int _staleReason;
            private readonly long _calendarEditedTicks;
            private readonly long _passedMilestoneTicks;
            private readonly object _lastReport;
            private readonly object _activeStamp;
            private readonly long _minuteStamp;
            private readonly string _recentActionText;
            private readonly bool _isRecentActionOnTop;

            public StatusBarSignature(LiveOpsHubCalendarSession session, DateTime nowUtc, string recentActionText,
                bool isRecentActionOnTop)
            {
                LiveOpsHubCheckState check = session != null ? session.Check : null;
                _hasCheck = check != null;
                _hasAsset = session != null && session.Asset != null;
                _isRunning = check != null && check.IsRunning;
                _completedRuleCount = check != null ? check.CompletedRuleCount : 0;
                _ruleCount = check != null ? check.RuleCount : 0;
                _staleReason = check != null ? (int)check.StaleReason : -1;
                _calendarEditedTicks = check != null && check.CalendarEditedUtc.HasValue ? check.CalendarEditedUtc.Value.Ticks : -1L;
                _passedMilestoneTicks = check != null && check.PassedMilestoneUtc.HasValue ? check.PassedMilestoneUtc.Value.Ticks : -1L;
                _lastReport = check != null ? check.LastReport : null;
                _activeStamp = session != null ? session.Publish.ActiveStamp : null;
                _minuteStamp = nowUtc.Ticks / TimeSpan.TicksPerMinute;
                _recentActionText = recentActionText ?? string.Empty;
                _isRecentActionOnTop = isRecentActionOnTop;
            }

            public bool Equals(StatusBarSignature other)
            {
                return _hasCheck == other._hasCheck
                    && _hasAsset == other._hasAsset
                    && _isRunning == other._isRunning
                    && _completedRuleCount == other._completedRuleCount
                    && _ruleCount == other._ruleCount
                    && _staleReason == other._staleReason
                    && _calendarEditedTicks == other._calendarEditedTicks
                    && _passedMilestoneTicks == other._passedMilestoneTicks
                    && ReferenceEquals(_lastReport, other._lastReport)
                    && ReferenceEquals(_activeStamp, other._activeStamp)
                    && _minuteStamp == other._minuteStamp
                    && string.Equals(_recentActionText, other._recentActionText, StringComparison.Ordinal)
                    && _isRecentActionOnTop == other._isRecentActionOnTop;
            }

            public override bool Equals(object other)
            {
                return other is StatusBarSignature signature && Equals(signature);
            }

            public override int GetHashCode()
            {
                // Không dùng làm khoá dictionary; đủ để giữ hợp đồng Equals/GetHashCode.
                return _minuteStamp.GetHashCode() ^ _completedRuleCount ^ _staleReason ^ _recentActionText.GetHashCode();
            }
        }

        // ------------------------------------------------------------------------------------------------------------ băng đĩa

        /// <summary>
        /// Băng "tệp đã đổi trên đĩa" (4.3, SPIKE-B SP-8b): hiện khi phiên báo có xung đột, biến mất khi người dùng đã chọn. Hub
        /// KHÔNG BAO GIỜ tự đè nháp — ba nút là ba quyết định, không có nút nào chạy ngầm.
        /// </summary>
        private void UpdateDiskBanner()
        {
            if (_notes == null || _services == null) return;
            LiveOpsHubDiskConflict conflict = _services.Session.DiskConflict;
            if (conflict == null)
            {
                _diskBanner?.RemoveFromHierarchy();
                _diskBanner = null;
                return;
            }
            if (_diskBanner != null) _diskBanner.RemoveFromHierarchy();

            _diskBanner = new VisualElement { name = DiskBannerElementName };
            _diskBanner.AddToClassList(LiveOpsHubClassNames.Note);
            _diskBanner.AddToClassList(LiveOpsHubClassNames.DiskBanner);

            Label text = new Label(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellDiskBannerFormat,
                _services.Session.AssetFileName, _services.Format.ShortDateTimeUtc(conflict.DetectedUtc)));
            text.AddToClassList(LiveOpsHubClassNames.DiskBannerText);
            _diskBanner.Add(text);

            VisualElement actions = new VisualElement();
            actions.AddToClassList(LiveOpsHubClassNames.DiskBannerActions);
            Button reload = new Button(ReloadFromDisk) { name = DiskBannerReloadElementName, text = LiveOpsHubStrings.ShellDiskBannerReloadButton };
            reload.tooltip = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellDiskBannerReloadTooltipFormat,
                conflict.LostIfReload.ChangeCount);
            actions.Add(reload);
            actions.Add(new Button(() => Navigate(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Export).WithCompareSource(LiveOpsHubCompareSource.Disk)))
            {
                name = DiskBannerDiffElementName,
                text = LiveOpsHubStrings.ShellDiskBannerDiffButton,
            });
            actions.Add(new Button(KeepEditorVersion) { name = DiskBannerKeepElementName, text = LiveOpsHubStrings.ShellDiskBannerKeepButton });
            _diskBanner.Add(actions);
            _notes.Add(_diskBanner);
        }

        private void ReloadFromDisk()
        {
            _services?.Session.ReloadFromDisk();
            UpdateDiskBanner();
        }

        private void KeepEditorVersion()
        {
            _services?.Session.KeepEditorVersion();
            UpdateDiskBanner();
        }

        /// <summary>
        /// Đường so hash thứ hai của SP-8b: cửa sổ lấy lại focus. <c>AssetPostprocessor</c> là đường chính nhưng nó chỉ bắn khi Unity
        /// tự import; người dùng có thể đổi file rồi quay lại Unity với auto refresh tắt. Phiên tự so hash và bỏ qua khi hash trùng,
        /// nên gọi mỗi lần focus không tốn gì và không bao giờ đè nháp.
        /// </summary>
        private void CheckDiskOnFocus()
        {
            if (_services == null) return;
            string assetPath = _services.Session.AssetPath;
            if (assetPath.Length == 0) return;
            _services.Session.HandleAssetsChanged(new[] { assetPath }, null, null, null);
        }

        // ------------------------------------------------------------------------------------------------------------ IHubHost

        /// <summary>
        /// Id sai → cảnh báo nêu id + nơi tra id hợp lệ rồi hiện Tổng quan (không ném: id có thể là của bản cũ). Internal + cài
        /// tường minh <see cref="IHubHost.Navigate"/>: lớp cửa sổ là public, cài ngầm sẽ biến điều hướng thành API public của
        /// package ngoài mục 3 (game chỉ được mở hub, không điều khiển màn).
        /// </summary>
        internal void Navigate(string sectionId)
        {
            ShowSection(sectionId);
        }

        void IHubHost.Navigate(string sectionId)
        {
            Navigate(sectionId);
        }

        string IHubHost.GetSectionViewState(string sectionId)
        {
            return windowState.GetSectionViewState(sectionId);
        }

        void IHubHost.SetSectionViewState(string sectionId, string viewStateJson)
        {
            windowState.SetSectionViewState(sectionId, viewStateJson);
        }

        /// <summary>Điều hướng có tham số (ô chặn rail, sau này bus): hiện màn rồi đưa tham số cho màn nếu màn nhận.</summary>
        internal void Navigate(LiveOpsHubNavigation navigation)
        {
            if (navigation == null) throw new ArgumentNullException(nameof(navigation));
            LastNavigation = navigation;
            ShowSection(navigation.SectionId);
            if (_currentSection is IHubSectionNavigation navigable
                && string.Equals(_currentSection.Id, navigation.SectionId, StringComparison.Ordinal)
                && !_failedSectionIds.Contains(_currentSection.Id))
            {
                navigable.ApplyNavigation(navigation);
            }
        }

        // ------------------------------------------------------------------------------------------------------------ menu ⋮

        // ------------------------------------------------------------------------------------------------------------ lưu / huỷ

        /// <summary>
        /// Unity hỏi khi đóng tab có *: lưu asset qua phiên; chỉ hạ cờ khi file đã ghi (lưu hỏng thì tab vẫn *). Lưu hỏng KHÔNG được im
        /// lặng — Unity vẫn đóng cửa sổ sau lời gọi này, nên lý do phải đi qua bus để người dùng còn đọc được.
        /// </summary>
        public override void SaveChanges()
        {
            if (_services == null) return;
            if (!_services.Session.Save())
            {
                _services.Bus.ShowOutcome(LiveOpsOutcomeRecord.Blocked(SaveFailureHeadline(_services.Session),
                    LiveOpsHubStrings.ServicesSaveChangesFailedDetail, _services.Clock.UtcNow));
                UpdateUnsavedState();
                return;
            }
            base.SaveChanges();
            UpdateUnsavedState();
        }

        /// <summary>Câu "vì sao không lưu được" theo đúng nhánh mà <c>Session.Save</c> vừa trượt.</summary>
        private static string SaveFailureHeadline(LiveOpsHubCalendarSession session)
        {
            if (session.DiskConflict != null)
            {
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesSaveFailedDiskConflictFormat, session.AssetFileName);
            }
            if (session.AssetPath.Length == 0) return LiveOpsHubStrings.ServicesSaveFailedNoPath;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesSaveFailedFormat, session.AssetFileName);
        }

        /// <summary>"Không lưu": asset về bản chụp lúc lưu gần nhất (một Undo group), rồi hạ cờ.</summary>
        public override void DiscardChanges()
        {
            _services?.Session.DiscardChanges();
            base.DiscardChanges();
            UpdateUnsavedState();
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            if (menu == null) throw new ArgumentNullException(nameof(menu));
            IReadOnlyList<OverflowMenuEntry> entries = BuildOverflowMenuEntries();
            for (int index = 0; index < entries.Count; index++)
            {
                OverflowMenuEntry entry = entries[index];
                if (entry.IsEnabled) menu.AddItem(new GUIContent(entry.Text), entry.IsOn, entry.Invoke);
                else menu.AddDisabledItem(new GUIContent(entry.Text));
            }
        }

        /// <summary>
        /// Mục của menu ⋮ theo đúng thứ tự [FD §3.3]: Kiểm lại tất cả (F5) · Tắt chuyển động · Mở tài liệu LiveOps · Hiện dữ
        /// liệu mẫu. "Hiện hướng dẫn phím tắt" là [P1-lùi] (G-OPT-SHORTCUTHELP, W6) nên KHÔNG có mục disabled trỏ tới nó.
        /// <para>
        /// Tách khỏi <see cref="AddItemsToMenu"/> vì <c>GenericMenu</c> không cho đọc lại nhãn: phần quyết định (mục nào, chữ gì,
        /// mục nào bật/khoá) nằm ở đây để test đọc thẳng, cùng lối với <see cref="LiveOpsHubNarrowRailMenu.BuildItems"/>.
        /// </para>
        /// </summary>
        internal IReadOnlyList<OverflowMenuEntry> BuildOverflowMenuEntries()
        {
            string checkKeyLabel = LiveOpsHubKeyLabels.For(LiveOpsHubShortcuts.CheckAllId);
            string checkAllText = checkKeyLabel.Length == 0
                ? LiveOpsHubStrings.ShellCheckAllMenuWithoutKey
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellCheckAllMenuFormat, checkKeyLabel);
            bool hasAsset = _services != null && _services.Session.Asset != null;
            bool reduceMotion = EditorPrefs.GetBool(ReduceMotionPreferenceKey, false);

            return new[]
            {
                new OverflowMenuEntry(checkAllText, hasAsset, false, StartCheckFromShortcut),
                new OverflowMenuEntry(LiveOpsHubStrings.ShellReduceMotionMenu, true, reduceMotion, ToggleReduceMotion),
                new OverflowMenuEntry(LiveOpsHubStrings.ShellOpenDocumentationMenu, true, false, OpenDocumentation),
                new OverflowMenuEntry(LiveOpsHubStrings.ShellShowDesignSampleMenu, true, false, ShowDesignSample),
            };
        }

        private void ToggleReduceMotion()
        {
            bool enabled = !EditorPrefs.GetBool(ReduceMotionPreferenceKey, false);
            EditorPrefs.SetBool(ReduceMotionPreferenceKey, enabled);
            _hubRoot?.EnableInClassList(LiveOpsHubClassNames.NoMotion, enabled);
        }

        /// <summary>
        /// "Mở tài liệu LiveOps": README của package — cùng địa chỉ mà link "Vì sao? (tài liệu luật)" của Kiểm lịch mở, và cùng
        /// <c>documentationUrl</c> trong <c>package.json</c>, nên hub chỉ có MỘT nơi gọi là tài liệu.
        /// </summary>
        private void OpenDocumentation()
        {
            if (_openUrlForTest != null) _openUrlForTest(LiveOpsHubPaths.RuleDocumentationBaseUrl);
            else Application.OpenURL(LiveOpsHubPaths.RuleDocumentationBaseUrl);
        }

        private void ShowDesignSample()
        {
            OpenPreviewSample();
        }

        /// <summary>
        /// Thay <c>Application.OpenURL</c> cho test — mở trình duyệt thật trong batchmode là tác dụng phụ ra ngoài Unity. Cùng
        /// seam với <see cref="SetSaveMenuCommandForTest"/>. null = mở thật.
        /// </summary>
        internal void SetOpenUrlForTest(Action<string> openUrl)
        {
            _openUrlForTest = openUrl;
        }

        /// <summary>Một mục của menu ⋮ — bất biến, không giữ tham chiếu tới <c>GenericMenu</c>.</summary>
        internal readonly struct OverflowMenuEntry
        {
            private readonly Action _invoke;

            internal OverflowMenuEntry(string text, bool isEnabled, bool isOn, Action invoke)
            {
                Text = text;
                IsEnabled = isEnabled;
                IsOn = isOn;
                _invoke = invoke;
            }

            internal string Text { get; }

            /// <summary>false = mục xám (vd chưa có asset lịch thì "Kiểm lại tất cả" không làm được gì).</summary>
            internal bool IsEnabled { get; }

            /// <summary>Mục có dấu tích ("Tắt chuyển động" đang bật).</summary>
            internal bool IsOn { get; }

            internal void Invoke()
            {
                _invoke?.Invoke();
            }
        }

        // ------------------------------------------------------------------------------------------------------------ phím tắt

        /// <summary>⌘K: palette đóng thì mở; đang mở thì đóng và nhường ⌘K lại cho Unity Search (8.7).</summary>
        internal void TogglePaletteOrUnitySearch()
        {
            _palette?.ToggleOrSearch();
        }

        /// <summary>Mở palette (ô "Đi tới màn…", và đường mở duy nhất của test) — không bao giờ đóng, khác với ⌘K.</summary>
        internal void OpenPalette()
        {
            _palette?.Open();
        }

        /// <summary>
        /// ⌘S: lưu lịch rồi chuyển tiếp ⌘S của Unity (<c>File/Save</c>) — người dùng bấm ⌘S mong lưu CẢ scene đang mở, không chỉ
        /// asset lịch. Lưu lịch hỏng thì không chuyển tiếp: <c>SaveChanges</c> đã bắn outcome nói lý do, chạy tiếp sẽ che nó đi.
        /// </summary>
        internal void SaveFromShortcut()
        {
            if (_services == null) return;
            if (!_services.Session.HasUnsavedChanges && _services.Session.Asset == null) return;
            SaveChanges();
            if (hasUnsavedChanges) return;
            if (_saveMenuCommandForTest != null) _saveMenuCommandForTest();
            else EditorApplication.ExecuteMenuItem(SaveMenuPath);
        }

        /// <summary>
        /// Thay lệnh "File/Save" của Unity cho test. Cần seam vì <c>ExecuteMenuItem("File/Save")</c> mở hộp lưu scene trong
        /// batchmode (log assert của Unity làm đỏ mọi test chạy cùng lượt) — cùng cách <see cref="LiveOpsHubPalette"/> thay
        /// đường mở Unity Search. null = chạy lệnh thật.
        /// </summary>
        internal void SetSaveMenuCommandForTest(Action command)
        {
            _saveMenuCommandForTest = command;
        }

        /// <summary>F5: chạy lại Kiểm lịch và bỏ cache health để rail/status đọc số mới ngay nhịp sau.</summary>
        internal void StartCheckFromShortcut()
        {
            if (_services == null || _services.Session.Asset == null) return;
            _services.Session.StartCheck();
            InvalidateAndRefreshHealth();
        }

        /// <summary>F8 / ⇧F8: màn hiện tại có danh sách phát hiện thì đi trong đó; không thì mở Kiểm lịch (8.7).</summary>
        /// <param name="direction">+1 = kế tiếp, −1 = trước đó.</param>
        internal void MoveToFinding(int direction)
        {
            if (_currentSection is IHubSectionFindings findings && !_failedSectionIds.Contains(_currentSection.Id)
                && findings.TryMoveToFinding(direction))
            {
                return;
            }
            Navigate(LiveOpsHubSections.Ids.Validation);
        }

        /// <summary>⌘1…⌘6: theo VỊ TRÍ trong registry ([FD §3.1]); vị trí ngoài danh sách thì không làm gì.</summary>
        internal void GoToSectionAt(int index)
        {
            if (index < 0 || index >= _sections.Count) return;
            Navigate(_sections[index].Id);
        }

        // ------------------------------------------------------------------------------------------------------------ cho test

        internal VisualElement HubRoot => _hubRoot;
        internal LiveOpsHubHeader Header => _header;
        internal LiveOpsHubPalette Palette => _palette;
        internal LiveOpsToast Toast => _toast;
        internal LiveOpsOutcomeView OutcomeView => _outcomeView;
        internal LiveOpsHoverCardHost HoverCardHost => _hoverCardHost;
        internal LiveOpsHubUndoTracker UndoTracker => _undoTracker;
        internal VisualElement DiskBanner => _diskBanner;
        internal LiveOpsHubRail Rail => _rail;
        internal LiveOpsHubSectionHeader SectionHeader => _sectionHeader;
        internal LiveOpsHubStatusBar StatusBar => _statusBar;
        internal LiveOpsHubSkin Skin => _skin;
        internal VisualElement SectionBody => _body;
        internal VisualElement ShellNotes => _notes;
        internal IHubSection CurrentSection => _currentSection;
        internal string ActiveSectionId => _currentSection == null ? string.Empty : _currentSection.Id;
        internal LiveOpsHubWindowState WindowState => windowState;
        internal LiveOpsHubNavigation LastNavigation { get; private set; }
        internal LiveOpsHubServices Services => _services;

        /// <summary>true khi cửa sổ đang hiện khung 2 (thiếu UXML hoặc element sống còn).</summary>
        internal bool IsLayoutMissing { get; private set; }

        internal bool IsSectionFailed(string sectionId) => _failedSectionIds.Contains(sectionId ?? string.Empty);

        /// <summary>Thay trạng thái cửa sổ như sau domain reload (test chứng minh trạng thái view đi lại vào màn).</summary>
        /// <summary>
        /// Chạy đúng đường dựng lại khi đổi ngôn ngữ. Test UI cần seam này vì nó đổi ngôn ngữ bằng scope ghim (không bắn
        /// <see cref="LiveOpsHubLanguage.Changed"/>) để khỏi ghi vào pref thật của máy người chạy test.
        /// </summary>
        internal void RebuildForLanguageChangeForTest()
        {
            OnLanguageChanged();
        }

        internal void ReplaceWindowStateForTest(LiveOpsHubWindowState state)
        {
            windowState = state ?? new LiveOpsHubWindowState();
        }

        /// <summary>Làm mới health ngay (không chờ nhịp 1 giây) — test và cửa sổ sau khi màn báo đổi.</summary>
        internal void RefreshHealth()
        {
            if (_rail == null) return;
            List<SectionHealth> healths = new List<SectionHealth>(_sections.Count);
            foreach (IHubSection section in _sections)
            {
                healths.Add(HealthOf(section));
            }
            // Bộ tổng hợp + cờ cũ của lần kiểm gần nhất (badge tầng KIỂM "2 bị bỏ" / "cũ · 2 bị bỏ", ô chặn nhắc F5).
            LiveOpsHubCheckState check = _services != null ? _services.Session.Check : null;
            LiveEventCalendarCheckSummary summary = check != null && check.LastReport != null ? check.LastReport.Summary : null;
            _rail.Build(LiveOpsHubRailModel.Build(_sections, healths, summary, check != null && check.IsStale));
            _rail.SetActiveSection(ActiveSectionId);
            // Nhịp 1 giây: KHÔNG ép dựng lại chữ status bar — cổng chữ ký lo phần "chỉ khi đổi" (M-6).
            RefreshStatusBarIfChanged();
        }

        internal SectionHealth HealthOf(IHubSection section)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            // Màn ném khi dựng mang NotMeasured "Màn ném lỗi khi dựng" tới lần dựng thành công — GetHealth của nó có thể vẫn "ổn".
            if (_failedSectionIds.Contains(section.Id)) return SectionHealth.NotMeasured(LiveOpsHubStrings.ShellSectionFailedHealthReason);
            return _throttles.TryGetValue(section.Id, out LiveOpsHealthThrottle throttle) ? throttle.Get() : section.GetHealth();
        }

        // ------------------------------------------------------------------------------------------------------------ màn

        internal void ShowSection(string sectionId)
        {
            if (_hubRoot == null || _sections.Count == 0) return;
            IHubSection section = FindSection(sectionId);
            if (section == null)
            {
                if (!string.IsNullOrEmpty(sectionId))
                {
                    Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellUnknownSectionWarningFormat, sectionId));
                }
                section = FindSection(LiveOpsHubSections.Ids.Overview) ?? _sections[0];
            }

            if (_currentSection != null) CaptureViewState(_currentSection);
            _currentSection = section;
            windowState.ActiveSectionId = section.Id;
            if (!_isIsolatedFromSessionState) SessionState.SetString(ActiveSectionSessionKey, section.Id);
            _rail.SetActiveSection(section.Id);
            BuildSection(section);
            RefreshHealth();
        }

        private void BuildSection(IHubSection section)
        {
            _body.Clear();
            try
            {
                _sectionHeader.Show(section);
                VisualElement view = section.CreateView();
                if (view == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellNullViewMessageFormat, section.Id));
                }
                _body.Add(view);
                if (section is IHubSectionViewState viewState) viewState.RestoreViewState(windowState.GetSectionViewState(section.Id));
                section.OnShown();
                _failedSectionIds.Remove(section.Id);
            }
            catch (Exception exception)
            {
                // Cảnh báo (không LogException): card lỗi đã nói đủ trên cửa sổ; Error sẽ làm fail test của màn KHÁC chạy cùng lượt.
                Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellSectionThrewLogFormat, section.Id,
                    LiveOpsHubFailureView.DescribeException(exception)));
                _body.Clear();
                _failedSectionIds.Add(section.Id);
                string sectionId = section.Id;
                _body.Add(LiveOpsHubFailureView.ForSection(section, exception, () => ShowSection(sectionId), _clipboard));
            }
            if (_throttles.TryGetValue(section.Id, out LiveOpsHealthThrottle throttle)) throttle.Invalidate();
        }

        private string ResolveInitialSectionId()
        {
            if (!string.IsNullOrEmpty(_pendingSectionId))
            {
                string pending = _pendingSectionId;
                _pendingSectionId = null;
                return pending;
            }
            if (!_isIsolatedFromSessionState)
            {
                string remembered = SessionState.GetString(ActiveSectionSessionKey, string.Empty);
                if (FindSection(remembered) != null) return remembered;
            }
            // Sau domain reload SessionState vẫn còn, nhưng cửa sổ test không đọc nó — trạng thái đã serialize là nguồn kế.
            if (FindSection(windowState.ActiveSectionId) != null) return windowState.ActiveSectionId;
            return LiveOpsHubSections.Ids.Overview;
        }

        private IHubSection FindSection(string sectionId)
        {
            if (string.IsNullOrEmpty(sectionId)) return null;
            foreach (IHubSection section in _sections)
            {
                if (string.Equals(section.Id, sectionId, StringComparison.Ordinal)) return section;
            }
            return null;
        }

        private void CaptureCurrentViewState()
        {
            if (_currentSection != null) CaptureViewState(_currentSection);
        }

        private void CaptureViewState(IHubSection section)
        {
            if (!(section is IHubSectionViewState viewState) || _failedSectionIds.Contains(section.Id)) return;
            try
            {
                windowState.SetSectionViewState(section.Id, viewState.CaptureViewState());
            }
            catch (Exception exception)
            {
                // Màn ném khi chụp trạng thái thì mất trạng thái của riêng màn đó, không chặn đóng cửa sổ hay domain reload.
                Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellViewStateThrewLogFormat, section.Id, exception.Message));
            }
        }

        // ------------------------------------------------------------------------------------------------------------ khung

        private void AttachStyleSheets(VisualElement hubRoot)
        {
            foreach (LiveOpsHubPaths.ShellStyleSheet sheet in LiveOpsHubPaths.ShellStyleSheetLoadOrder)
            {
                StyleSheet loaded = _layoutLoader.LoadStyleSheet(sheet.Path);
                if (loaded != null)
                {
                    hubRoot.styleSheets.Add(loaded);
                    continue;
                }
                // Sheet bắt buộc thiếu = package hỏng → nói đúng đường dẫn (8.1 bước 2). Sheet chưa bắt buộc (nhánh tạm ở
                // LiveOpsHubPaths.ShellStyleSheetLoadOrder) thì im lặng — probe CLI liệt kê riêng.
                if (sheet.IsRequired)
                {
                    Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellMissingStyleSheetWarningFormat, sheet.Path));
                }
            }
        }

        private void UpdateCompilingNote()
        {
            if (_notes == null || _compilationState == null) return;
            bool isCompiling = _compilationState.IsCompiling;
            if (isCompiling && _compilingNote == null)
            {
                _compilingNote = new VisualElement { name = CompilingNoteElementName };
                _compilingNote.AddToClassList(LiveOpsHubClassNames.Note);
                _compilingNote.Add(new LiveOpsSpinner());
                Label text = new Label(LiveOpsHubStrings.ShellCompilingNote);
                text.AddToClassList(LiveOpsHubClassNames.NoteText);
                _compilingNote.Add(text);
                _notes.Insert(0, _compilingNote);
            }
            else if (!isCompiling && _compilingNote != null)
            {
                _compilingNote.RemoveFromHierarchy();
                _compilingNote = null;
            }
        }

        private void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextHealthRefreshSeconds) return;
            _nextHealthRefreshSeconds = EditorApplication.timeSinceStartup + HealthRefreshIntervalSeconds;
            // Nhịp 1 giây cũng là nhịp "kết quả kiểm thành cũ khi qua mốc" (PD-23) — trước RefreshHealth để rail đọc lý do mới.
            _services?.Session.Tick();
            RefreshHealth();
        }

        // ------------------------------------------------------------------------------------------------------------ services

        private void EnsureServices()
        {
            if (_services != null) return;
            if (_injectedServices != null)
            {
                _services = _injectedServices;
                _ownsServices = false;
                return;
            }
            LiveOpsHubServicesBuilder builder = new LiveOpsHubServicesBuilder();
            if (_isIsolatedFromSessionState)
            {
                // OpenForTest (G-SHELL): phiên thật nhưng không asset — không đọc GUID nhớ của người dùng, không tự chạy kiểm.
                builder.WithCalendarAsset(null).WithAutoCheckOnOpen(false).WithCompilationState(_injectedCompilationState).WithLayoutLoader(_injectedLayoutLoader);
            }
            else if (_pendingAsset != null)
            {
                // Mở từ inspector: asset tường minh NHƯNG vẫn có bộ tìm — phiên nhớ GUID theo project (PD-16) và đếm được số
                // LiveEventCalendarAsset cho HelpBox "Có 2 LiveEventCalendarAsset" (7.1).
                builder.WithCalendarAsset(_pendingAsset).WithAssetLocator(new LiveOpsHubAssetLocator());
            }
            _pendingAsset = null;
            _services = builder.Build();
            _ownsServices = true;
        }

        private void SubscribeServices()
        {
            if (_services == null || _isServicesSubscribed) return;
            LiveOpsHubCalendarSession session = _services.Session;
            session.DocumentChanged += OnSessionDocumentChanged;
            session.CheckChanged += OnSessionCheckChanged;
            session.DiskChangeDetected += OnSessionDiskChangeDetected;
            LiveOpsHubSectionBus bus = _services.Bus;
            bus.NavigationRequested += Navigate;
            bus.ToastRequested += OnToastRequested;
            bus.HealthInvalidated += OnHealthInvalidated;
            bus.OutcomeRequested += OnOutcomeRequested;
            bus.OutcomeCleared += OnOutcomeCleared;
            bus.ContentClassRequested += OnContentClassRequested;
            _isServicesSubscribed = true;
        }

        private void UnsubscribeServices()
        {
            if (_services == null || !_isServicesSubscribed) return;
            LiveOpsHubCalendarSession session = _services.Session;
            session.DocumentChanged -= OnSessionDocumentChanged;
            session.CheckChanged -= OnSessionCheckChanged;
            session.DiskChangeDetected -= OnSessionDiskChangeDetected;
            LiveOpsHubSectionBus bus = _services.Bus;
            bus.NavigationRequested -= Navigate;
            bus.ToastRequested -= OnToastRequested;
            bus.HealthInvalidated -= OnHealthInvalidated;
            bus.OutcomeRequested -= OnOutcomeRequested;
            bus.OutcomeCleared -= OnOutcomeCleared;
            bus.ContentClassRequested -= OnContentClassRequested;
            _isServicesSubscribed = false;
        }

        private void ReleaseServices()
        {
            if (_services == null) return;
            // R-25: domain reload/đóng cửa sổ giữa lúc kéo hoặc kiểm — phiên huỷ kéo dở (không để Undo group mở) và dừng nhịp kiểm.
            if (_ownsServices || _ownsInjectedServices) _services.Session.Dispose();
            _services = null;
            _ownsServices = false;
            _ownsInjectedServices = false;
            _injectedServices = null;
            if (_ownedPreviewAsset == null) return;
            // DontSave gồm DontUnloadUnusedAsset: không tay nào dọn thì asset mẫu sống tới khi tắt Unity.
            DestroyImmediate(_ownedPreviewAsset);
            _ownedPreviewAsset = null;
        }

        private void OnSessionDocumentChanged()
        {
            UpdateUnsavedState();
            UpdateDiskBanner();
            InvalidateAndRefreshHealth();
        }

        private void OnSessionCheckChanged()
        {
            InvalidateAndRefreshHealth();
        }

        private void OnSessionDiskChangeDetected()
        {
            // Hub không tự đè nháp (SP-8b): băng hỏi người dùng giữ bản nào. Log giữ nguyên để lịch sử Console còn dấu vết lần đổi.
            Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InterimDiskConflictLog, _services.Session.AssetFileName));
            UpdateDiskBanner();
            InvalidateAndRefreshHealth();
        }

        private void OnHealthInvalidated()
        {
            InvalidateAndRefreshHealth();
        }

        private void OnOutcomeRequested(LiveOpsOutcomeRecord outcome)
        {
            // Outcome sống trong trạng thái cửa sổ (qua domain reload); view chỉ đọc lại từ đó, không giữ bản sao riêng.
            windowState.LastOutcome = outcome;
            RefreshOutcomeView();
        }

        private void OnOutcomeCleared()
        {
            windowState.LastOutcome = null;
            RefreshOutcomeView();
        }

        private void InvalidateAndRefreshHealth()
        {
            LiveOpsHealthThrottle.InvalidateAll();
            RefreshHealth();
        }

        /// <summary>
        /// Đổ hai chip header và đồng bộ cờ "*" của tab (8.3). Một nguồn duy nhất — <see cref="LiveOpsHubHeaderChipModel"/> — quyết
        /// cả dạng chip lẫn <c>hasUnsavedChanges</c>, nên không bao giờ có cảnh tab sạch mà chip nói "Chưa lưu".
        /// </summary>
        internal void UpdateUnsavedState()
        {
            LiveOpsHubCalendarSession session = _services != null ? _services.Session : null;
            LiveOpsHubFormat format = _services != null ? _services.Format : new LiveOpsHubFormat(TimeSpan.Zero);
            LiveOpsHubHeaderChipModel chips = LiveOpsHubHeaderChipModel.Build(session, format,
                LiveOpsHubKeyLabels.For(LiveOpsHubShortcuts.SaveCalendarId));
            _header?.SetChips(chips);
            // Gán VÔ ĐIỀU KIỆN: model trả "" ở mọi dạng khác, nên sau khi lưu xong câu cũ ("Main.asset có 3 thay đổi chưa
            // lưu: …") không còn nằm lại trên EditorWindow (L-1).
            saveChangesMessage = chips.SaveChangesMessage;
            if (hasUnsavedChanges != chips.HasUnsavedChanges) hasUnsavedChanges = chips.HasUnsavedChanges;
        }

        private void OnSkinChanged()
        {
            // Cache icon đã xoá trong LiveOpsHubSkin; dựng lại rail (dấu, chevron) và màn đang hiện để icon theo skin mới.
            if (_rail == null) return;
            RefreshHealth();
            if (_currentSection != null) ShowSection(_currentSection.Id);
        }

        private void TearDownChrome()
        {
            UnsubscribeServices();
            if (_isUpdateRegistered)
            {
                EditorApplication.update -= OnEditorUpdate;
                _isUpdateRegistered = false;
            }
            if (_skin != null)
            {
                _skin.SkinChanged -= OnSkinChanged;
                _skin.Detach();
                _skin = null;
            }
            _detachBreakpoints?.Invoke();
            _detachBreakpoints = null;
            _rail?.Dispose();
            _rail = null;
            if (_undoTracker != null)
            {
                // Tracker nghe Undo.undoRedoPerformed (event tĩnh sống lâu hơn cửa sổ): không Dispose là rò handler qua mỗi CreateGUI.
                _undoTracker.UndoRedoPerformed -= RefreshStatusBar;
                _undoTracker.Dispose();
                _undoTracker = null;
            }
            if (_outcomeView != null)
            {
                _outcomeView.ActionInvoked -= OnOutcomeActionInvoked;
                _outcomeView = null;
            }
            _toast = null;
            _palette = null;
            _hoverCardHost = null;
            _outcomeHost = null;
            _diskBanner = null;
            _compilingNote = null;
            _hubRoot = null;
        }
    }
}
