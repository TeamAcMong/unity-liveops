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

        /// <summary>
        /// (J2-01) Khe giữa mép DƯỚI của toast và mép TRÊN của chân màn. 8px đúng bằng <c>bottom: 8px</c> mặc định của toast trong
        /// <c>liveops-hub-feedback.uss</c>: màn không có chân thì toast đậu đúng chỗ cũ, có chân thì nó trượt lên nguyên khối — một con số,
        /// hai tình huống. <c>internal</c> để test đọc đúng con số này thay vì chép lại 8.
        /// </summary>
        internal const float ToastFloorGapPixels = 8f;

        internal const float MinimumWidth = 620f;
        internal const float MinimumHeight = 420f;

        internal const string CompilingNoteElementName = "hub-compiling-note";

        /// <summary>Nhịp hỏi phút hiện tại cho giờ UTC ở status bar; chữ chỉ dựng lại khi phút đổi (8.3).</summary>
        internal const long StatusClockPollMilliseconds = 1000;

        /// <summary>⌘S của Unity sau khi hub đã lưu lịch (8.7) — chuỗi menu, không phải shortcut id.</summary>
        internal const string SaveMenuPath = "File/Save";

        // Tên element của băng đĩa do chính view giữ (LiveOpsHubDiskConflictBanner); cửa sổ chuyển tiếp để test đã có từ W4 và
        // probe không phải đổi chỗ tra.
        internal const string DiskBannerElementName = LiveOpsHubDiskConflictBanner.ElementName;
        internal const string DiskBannerReloadElementName = LiveOpsHubDiskConflictBanner.ReloadButtonName;
        internal const string DiskBannerDiffElementName = LiveOpsHubDiskConflictBanner.DiffButtonName;
        internal const string DiskBannerKeepElementName = LiveOpsHubDiskConflictBanner.KeepButtonName;

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

        // Cờ DUY NHẤT của cửa sổ mẫu sống qua domain reload: mọi thứ khác của nó ([NonSerialized] services tiêm vào, cờ cách
        // ly, asset mẫu) bị xoá khi biên dịch lại. Không nhớ điều này thì cửa sổ mẫu dựng lại thành cửa sổ hub THẬT trên lịch
        // thật, và người dùng tưởng đang nghịch dữ liệu mẫu (R-24).
        [SerializeField] private bool isPreviewSampleWindow;
        [NonSerialized] private Action<string> _openUrlForTest;

        // Người dùng đã chọn "Giữ bản trong Editor" và chưa lưu lần nào từ đó — lần ⌘S tới mới thật sự ghi đè bản trên đĩa.
        [NonSerialized] private bool _isOverwriteOfDiskPending;

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
            LiveEventCalendarAsset sampleAsset;
            LiveOpsHubServices services = BuildPreviewSampleServices(out sampleAsset);
            LiveOpsHubWindow window = CreateInstance<LiveOpsHubWindow>();
            // Không đi qua OpenWithServices vì cờ mẫu phải nằm trên cửa sổ TRƯỚC Show: Show có thể chạy CreateGUI ngay, và
            // sau này chính cờ đó là thứ duy nhất còn lại sau domain reload.
            window.isPreviewSampleWindow = true;
            window._injectedServices = services;
            window._pendingSectionId = LiveOpsHubSections.Ids.Overview;
            window._isIsolatedFromSessionState = true;
            window._ownedPreviewAsset = sampleAsset;
            window._ownsInjectedServices = true;
            window.Show();
            return window;
        }

        /// <summary>
        /// Trạng thái của cửa sổ mẫu NGAY SAU domain reload, dựng bằng đúng thứ Unity còn giữ: mỗi field
        /// <c>[SerializeField]</c> sống, mọi field <c>[NonSerialized]</c> về mặc định. Chỉ test dùng — mô phỏng reload bằng
        /// cách gọi tay <c>OnDisable</c>/<c>OnEnable</c> để lại cửa sổ nửa sống nửa chết và làm hỏng test chạy sau.
        /// </summary>
        internal static LiveOpsHubWindow OpenPreviewSampleAfterDomainReloadForTest()
        {
            LiveOpsHubWindow window = CreateInstance<LiveOpsHubWindow>();
            window.isPreviewSampleWindow = true;
            window.Show();
            return window;
        }

        /// <summary>Phiên mẫu 13/9/2026 08:47 với đồng hồ đứng yên trên asset chỉ sống trong bộ nhớ — một chỗ dựng duy nhất,
        /// dùng cả lúc mở cửa sổ lẫn lúc dựng lại sau domain reload.</summary>
        private static LiveOpsHubServices BuildPreviewSampleServices(out LiveEventCalendarAsset sampleAsset)
        {
            sampleAsset = LiveOpsHubPreviewSample.CreateAsset();
            return new LiveOpsHubServicesBuilder()
                .WithClock(LiveOpsHubPreviewSample.CreateClock())
                .WithTimeZone(LiveOpsHubPreviewSample.CreateTimeZone())
                .WithCalendarAsset(sampleAsset)
                .Build();
        }

        // ------------------------------------------------------------------------------------------------------------ vòng đời

        private void OnEnable()
        {
            ApplyWindowTitle();
            minSize = new Vector2(MinimumWidth, MinimumHeight);
            if (windowState == null) windowState = new LiveOpsHubWindowState();
            // Cờ cách ly là [NonSerialized] nên reload xoá mất: bật lại ngay ở đây để cửa sổ mẫu không ghi màn đang mở của
            // nó vào SessionState dùng chung với cửa sổ hub thật.
            if (isPreviewSampleWindow) _isIsolatedFromSessionState = true;
            // Đăng ký ở OnEnable chứ không ở CreateGUI: chính tay xử lý gọi lại CreateGUI, mà CreateGUI mở đầu bằng
            // TearDownChrome — gỡ đăng ký trong đó sẽ cắt luôn sự kiện đang chạy.
            LiveOpsHubLanguage.Changed -= OnLanguageChanged;
            LiveOpsHubLanguage.Changed += OnLanguageChanged;
            // Hộp xác nhận đặt chỗ theo CỬA SỔ HUB, không theo cửa sổ đang focus (UX-27 / soát W8-UX R4): hộp luôn mở từ một
            // thao tác trong hub, mà focus thì popover/Console/Project vừa click đều cướp được.
            LiveOpsConfirmWindow.RegisterOwnerWindow(this);
        }

        private void OnDisable()
        {
            LiveOpsHubLanguage.Changed -= OnLanguageChanged;
            LiveOpsConfirmWindow.UnregisterOwnerWindow(this);
            CaptureCurrentViewState();
            TearDownChrome();
            ReleaseServices();
        }

        private void OnDestroy()
        {
            // OnDisable chạy trước OnDestroy ở mọi đường đóng cửa sổ đã biết; gỡ lần nữa để một đường lạ không để lại handler
            // trỏ vào cửa sổ đã chết (event tĩnh sống lâu hơn cửa sổ).
            LiveOpsHubLanguage.Changed -= OnLanguageChanged;
            LiveOpsConfirmWindow.UnregisterOwnerWindow(this);
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
            DropDerivedTextCaches();
            CaptureCurrentViewState();
            CreateGUI();
        }

        /// <summary>
        /// (UX-20 / UJ-20) Dựng lại khung thôi CHƯA đủ để chữ đổi hết: phiên lịch giữ <c>ExportGateState</c> ĐÃ DỰNG THÀNH CHỮ
        /// trong một cache khoá theo <c>StateVersion</c> + format + readBack, mà đổi ngôn ngữ không đụng ba thứ đó — health màn
        /// Xuất đọc lại đúng bản cũ và badge tầng XUẤT của rail vẫn ghi "chặn" giữa một cửa sổ đã sang English.
        /// <c>NotifyPublishStateChanged</c> là seam DUY NHẤT của phiên tăng <c>StateVersion</c> mà không có tác dụng phụ nào
        /// khác (không bắn sự kiện, không đánh dấu lịch đã sửa), nên ở đây nó có nghĩa "bỏ mọi bản dựng sẵn thành chữ".
        /// Tên seam nói về publish là do file phiên nằm ngoài quyền ghi của gói — đã ghi vào contract-changes-G-UX-SHELL.md.
        /// </summary>
        private void DropDerivedTextCaches()
        {
            if (_services == null) return;
            _services.Session.NotifyPublishStateChanged();
            LiveOpsHealthThrottle.InvalidateAll();
        }

        private void OnFocus()
        {
            // Hai hub mở cùng lúc: cái người dùng vừa click là chủ của hộp xác nhận mở sau đó.
            LiveOpsConfirmWindow.RegisterOwnerWindow(this);
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

        /// <summary>
        /// (J2-01) Đậu toast trên chân màn đã ĐO ĐƯỢC. Màn gửi mép TRÊN của chân ở toạ độ world; đây đổi sang <c>bottom</c> theo đúng
        /// hộp đang làm gốc toạ độ cho toast (<c>hub-content</c>), cộng một khe thủ để toast không dính sát chữ.
        /// <para>
        /// <c>float.NaN</c> trả toast về luật USS — dùng khi rời màn Lịch. Luật USS vẫn là SÀN TRƯỚC KHI ĐO: giữa lúc dựng màn và lần
        /// <c>GeometryChangedEvent</c> đầu tiên chưa có số đo nào, và 64px của class vẫn tốt hơn 8px mặc định.
        /// </para>
        /// </summary>
        private void OnToastFloorRequested(float worldTopY)
        {
            if (_toast == null || _hubRoot == null) return;
            if (float.IsNaN(worldTopY))
            {
                _toast.style.bottom = StyleKeyword.Null; // style-inline-allowed: 6
                return;
            }
            VisualElement content = _hubRoot.Q(LiveOpsHubPaths.ShellElementNames.Content);
            if (content == null) return;
            Rect contentBound = content.worldBound;
            if (float.IsNaN(contentBound.yMax)) return;
            float bottom = contentBound.yMax - worldTopY + ToastFloorGapPixels;
            // Không bao giờ thấp hơn khe thủ: chân màn đo ra 0 (chưa bố cục xong) thì toast về đúng chỗ mặc định của nó, không âm.
            if (bottom < ToastFloorGapPixels) bottom = ToastFloorGapPixels;
            _toast.style.bottom = bottom; // style-inline-allowed: 6
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
                // Q-W5-5: status bar chỉ có MỘT dòng và người đọc lại sau nhiều thao tác, nên nó đọc câu NGẮN của bước;
                // toast bên cạnh vẫn giữ câu dài. Cùng phép rơi với Undo History (LiveOpsToastModel.StepName).
                windowState.RecentActionText = toast.StepName;
                windowState.RecentActionUndoGroup = toast.UndoGroup;
            }
            RefreshStatusBar();
        }

        /// <summary>Outcome nằm trong trạng thái cửa sổ (sống qua domain reload); view chỉ đọc lại từ đó.</summary>
        private void RefreshOutcomeView()
        {
            if (_outcomeView == null) return;
            if (windowState.LastOutcome == null) _outcomeView.ClearRecord();
            else _outcomeView.SetRecord(windowState.LastOutcome, OutcomeActionLabelOf(windowState.LastOutcome.ActionId));
        }

        /// <summary>
        /// Nhãn nút của outcome. <c>LiveOpsOutcomeView</c> ẩn nút khi nhãn rỗng, nên id nào khung chưa biết cách làm thì KHÔNG
        /// hiện nút — không bao giờ có nút trỏ tới thứ khung không chạy được (mục 12 I-11).
        /// </summary>
        internal static string OutcomeActionLabelOf(string actionId)
        {
            if (string.Equals(actionId, ExportSection.RevealFileActionId, StringComparison.Ordinal))
            {
                return LiveOpsHubStrings.ShellOutcomeRevealFileButton;
            }

            // (cổng W5, nợ P-N1) Id màn thì OnOutcomeActionInvoked ĐÃ điều hướng đúng — chỉ thiếu nhãn, nên nút bị ẩn và cả
            // nhánh V-14 bước 5 ("Mở Loại event" sau khi nhập JSON còn loại chưa khai báo) thành code chết. Nhãn lấy đúng câu
            // đã có trong catalog, không đẻ câu mới.
            if (string.Equals(actionId, LiveOpsHubSections.Ids.EventTypes, StringComparison.Ordinal))
            {
                return LiveOpsHubStrings.OverviewOpenEventTypesButton;
            }

            return string.Empty;
        }

        /// <summary>
        /// Nút trên outcome là một hành động của MÀN đang mở (vd "Mở Xuất JSON") — khung chỉ chuyển tiếp. "Mở thư mục" là ngoại
        /// lệ có chủ đích: nó chạm hệ điều hành, mà port <c>ILiveOpsHubFileDialog</c> chỉ khung mới giữ.
        /// </summary>
        private void OnOutcomeActionInvoked(string actionId, string actionArgument)
        {
            if (string.IsNullOrEmpty(actionId)) return;
            if (string.Equals(actionId, ExportSection.RevealFileActionId, StringComparison.Ordinal))
            {
                if (!string.IsNullOrEmpty(actionArgument)) _fileDialog?.Reveal(actionArgument);
                return;
            }
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
            LiveOpsHubRecentActionStep recentActionStep = RecentActionStepOf(hasRecentAction, recentActionGroup);

            StatusBarSignature signature = new StatusBarSignature(session, nowUtc, recentActionText, recentActionStep);
            if (!force && _hasStatusSignature && signature.Equals(_statusSignature)) return;
            _statusSignature = signature;
            _hasStatusSignature = true;

            LiveOpsHubStatusBarModel model = LiveOpsHubStatusBarModel.Build(session.Check, session.Asset != null, recentActionText,
                recentActionStep, nowUtc, session.Publish.ActiveStamp, _services.Format, LiveOpsHubKeyLabels.Undo, LiveOpsHubKeyLabels.Redo);
            _statusBar.SetLeft(model.LeftMark, model.LeftText, string.Empty);
            _statusBar.SetRight(model.RightText, model.RightTooltip);
        }

        /// <summary>
        /// (UX-26) Hai chiều của stack Undo phải tách nhau: <c>IsGroupOnTop</c> gộp "sẽ gỡ" và "sẽ trả lại" thành một bool nên
        /// sau khi Hoàn tác câu vẫn mời ⌘Z. Hỏi thẳng hai vế để câu nói đúng chuyện vừa xảy ra.
        /// </summary>
        private LiveOpsHubRecentActionStep RecentActionStepOf(bool hasRecentAction, int recentActionGroup)
        {
            if (!hasRecentAction || _undoTracker == null) return LiveOpsHubRecentActionStep.NotOnTop;
            if (_undoTracker.IsNextUndo(recentActionGroup)) return LiveOpsHubRecentActionStep.NextUndo;
            if (_undoTracker.IsNextRedo(recentActionGroup)) return LiveOpsHubRecentActionStep.NextRedo;
            return LiveOpsHubRecentActionStep.NotOnTop;
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
            private readonly int _recentActionStep;

            public StatusBarSignature(LiveOpsHubCalendarSession session, DateTime nowUtc, string recentActionText,
                LiveOpsHubRecentActionStep recentActionStep)
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
                _recentActionStep = (int)recentActionStep;
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
                    && _recentActionStep == other._recentActionStep;
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

            _diskBanner = LiveOpsHubDiskConflictBanner.Create(conflict, _services.Session.AssetFileName, _services.Format,
                ReloadFromDisk,
                // Đi qua BUS chứ không gọi thẳng Navigate: "Xem khác biệt" là một yêu cầu điều hướng như mọi yêu cầu khác của
                // hub (V-13), nên nó phải quan sát được ở cùng một chỗ — cửa sổ vẫn là nơi nghe và thực hiện.
                // Đích là màn LỊCH, không phải Xuất JSON: bảng V-13 cho nguồn Disk đúng một chỗ vẽ — pane "So với" của Lịch
                // (4.3 "Xem khác biệt mở pane So với, nguồn là bản trên đĩa"; ảnh h28f-calendar-compare-disk).
                () => _services.Bus.Navigate(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Calendar).WithCompareSource(LiveOpsHubCompareSource.Disk)),
                KeepEditorVersion).Element;
            _notes.Add(_diskBanner);
        }

        private void ReloadFromDisk()
        {
            _services?.Session.ReloadFromDisk();
            // Bản đĩa đã thắng: không còn gì để ghi đè, hộp ⌘S không được hỏi nữa.
            _isOverwriteOfDiskPending = false;
            UpdateDiskBanner();
        }

        /// <summary>
        /// "Giữ bản trong Editor": phiên ghi nháp lại vào asset và hạ cờ xung đột, nhưng file trên đĩa VẪN là bản của đồng đội —
        /// lần ⌘S tới mới thật sự ghi đè. Cửa sổ nhớ điều đó để mở hộp cấp 1 đúng lúc (Hình 28 khung 4 dòng cuối).
        /// </summary>
        private void KeepEditorVersion()
        {
            if (_services == null) return;
            _services.Session.KeepEditorVersion();
            _isOverwriteOfDiskPending = true;
            UpdateDiskBanner();
        }

        /// <summary>
        /// Hộp cấp 1 "Ghi đè N mục vừa đổi trên đĩa?" — chỉ hỏi khi người dùng đã chọn "Giữ bản trong Editor" và chưa lưu lần nào
        /// từ đó. Trả false = người dùng giữ lại bản trên đĩa, <see cref="SaveChanges"/> dừng (asset vẫn bẩn, tab vẫn có "*").
        /// </summary>
        private bool ConfirmOverwriteOfDiskIfNeeded()
        {
            if (!_isOverwriteOfDiskPending || _services == null) return true;
            LiveEventCalendarDiffResult diff = _services.Session.UnsavedDiff;
            string itemList = LiveOpsHubDiskConflictBanner.ItemListOf(diff);
            string fileName = _services.Session.AssetFileName;
            string body = itemList.Length == 0
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellOverwriteDiskConfirmBodyWithoutItemsFormat, fileName)
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellOverwriteDiskConfirmBodyFormat, fileName, itemList);
            LiveOpsConfirmRequest request = new LiveOpsConfirmRequest.Builder()
                .WithLevel(LiveOpsConfirmLevel.Level1)
                .WithTitle(LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.ShellOverwriteDiskConfirmTitleFormat),
                    _services.Format.Integer(diff.ChangeCount)))
                .WithBody(body)
                .WithKeyHint(LiveOpsHubStrings.ShellOverwriteDiskConfirmKeyHint)
                .WithButtons(LiveOpsHubStrings.ShellOverwriteDiskConfirmDestructive, LiveOpsHubStrings.ShellOverwriteDiskConfirmSafe)
                .Build();
            return _services.Confirmation.Confirm(request) == LiveOpsConfirmResult.Destructive;
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
            // Mức xác nhận của thao tác này là LiveOpsEditOperation.OverwriteDiskChanges = cấp 1 (bảng 7.0) — hỏi TRƯỚC khi ghi,
            // vì ghi rồi thì bản của đồng đội trên đĩa đã mất và Undo của hub không lấy lại được.
            if (!ConfirmOverwriteOfDiskIfNeeded()) return;
            if (!_services.Session.Save())
            {
                _services.Bus.ShowOutcome(LiveOpsOutcomeRecord.Blocked(SaveFailureHeadline(_services.Session),
                    LiveOpsHubStrings.ServicesSaveChangesFailedDetail, _services.Clock.UtcNow));
                UpdateUnsavedState();
                return;
            }
            base.SaveChanges();
            // Đĩa đã mang bản trong Editor: lần ⌘S sau không còn gì để hỏi.
            _isOverwriteOfDiskPending = false;
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
        /// Mục của menu ⋮ theo đúng thứ tự [FD §3.3]: Kiểm lại tất cả (F5) · Tắt chuyển động · Hiện hướng dẫn phím tắt ·
        /// Mở tài liệu LiveOps · Hiện dữ liệu mẫu. Mục thứ ba do G-OPT-SHORTCUTHELP (W6) dựng — trước đó nó là [P1-lùi] và
        /// cố tình KHÔNG có mục xám trỏ tới thứ chưa dựng.
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
                // Luôn bật: bảng phím đọc được cả khi chưa có lịch nào — nó nói về CỬA SỔ, không về dữ liệu đang mở.
                new OverflowMenuEntry(LiveOpsHubStrings.ShortcutHelpMenuItem, true, false, ShowShortcutHelp),
                new OverflowMenuEntry(LiveOpsHubStrings.ShellOpenDocumentationMenu, true, false, OpenDocumentation),
                new OverflowMenuEntry(LiveOpsHubStrings.ShellShowDesignSampleMenu, true, false, ShowDesignSample),
            };
        }

        /// <summary>
        /// "Hiện hướng dẫn phím tắt" (G-OPT-SHORTCUTHELP, W6): popover 320px neo vào gốc hub, cùng cách mọi popover khác của
        /// hub neo vào <c>worldBound</c> của element mở nó — menu ⋮ không có element riêng để neo.
        /// <para>
        /// Tiêu đề màn truyền từ registry đang chạy: câu "Đi tới màn …" của ⌘1…⌘6 đi theo VỊ TRÍ trong registry ([FD §3.1]),
        /// nên chép cứng sáu cái tên ở popover là bảng nói sai ngay khi thứ tự màn đổi.
        /// </para>
        /// </summary>
        private void ShowShortcutHelp()
        {
            Rect activator = _hubRoot != null ? _hubRoot.worldBound : new Rect();
            LiveOpsPopoverContent.ShowSingle(activator, new ShortcutHelpPopover(SectionTitles(), _layoutLoader, null));
        }

        /// <summary>Tiêu đề màn theo thứ tự registry; rỗng khi cửa sổ chưa dựng xong (popover rơi về số vị trí).</summary>
        private IReadOnlyList<string> SectionTitles()
        {
            List<string> titles = new List<string>(_sections.Count);
            foreach (IHubSection section in _sections) titles.Add(section.Title);
            return titles;
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

        /// <summary>Đã chọn "Giữ bản trong Editor", chưa lưu — lần ⌘S tới mở hộp "Ghi đè N mục vừa đổi trên đĩa?".</summary>
        internal bool IsOverwriteOfDiskPending => _isOverwriteOfDiskPending;
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
            if (isPreviewSampleWindow)
            {
                // Domain reload đã xoá services tiêm vào: dựng lại ĐÚNG phiên mẫu. Rơi xuống nhánh mặc định dưới là đi tìm
                // asset lịch thật của project — cửa sổ "chỉ để xem giao diện" hoá ra đang sửa lịch thật (R-24).
                LiveEventCalendarAsset sampleAsset;
                _services = BuildPreviewSampleServices(out sampleAsset);
                _ownedPreviewAsset = sampleAsset;
                _ownsInjectedServices = true;
                _isIsolatedFromSessionState = true;
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
            bus.ToastFloorRequested += OnToastFloorRequested;
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
            bus.ToastFloorRequested -= OnToastFloorRequested;
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
            // Hub không tự đè nháp (SP-8b): băng hỏi người dùng giữ bản nào. Log giữ lại để lịch sử Console còn dấu vết lần đổi.
            Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellDiskConflictLogFormat, _services.Session.AssetFileName));
            // Xung đột MỚI: câu trả lời cũ không còn nghĩa, người dùng phải chọn lại trước khi ⌘S ghi đè thứ gì.
            _isOverwriteOfDiskPending = false;
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
