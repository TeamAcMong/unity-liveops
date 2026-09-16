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
        [NonSerialized] private IHubSection _currentSection;
        [NonSerialized] private Action _detachBreakpoints;
        [NonSerialized] private double _nextHealthRefreshSeconds;
        [NonSerialized] private bool _isUpdateRegistered;

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

        // ------------------------------------------------------------------------------------------------------------ vòng đời

        private void OnEnable()
        {
            titleContent = new GUIContent(LiveOpsHubStrings.ShellWindowTitle, LiveOpsHubIcons.Get(LiveOpsHubPaths.CalendarIconName));
            minSize = new Vector2(MinimumWidth, MinimumHeight);
            if (windowState == null) windowState = new LiveOpsHubWindowState();
        }

        private void OnDisable()
        {
            CaptureCurrentViewState();
            TearDownChrome();
            ReleaseServices();
        }

        private void OnFocus()
        {
            // Dự phòng khi probe skin chưa bắn (8.8): OnFocus chạy khi người dùng quay lại cửa sổ sau khi đổi Theme.
            _skin?.ApplyFromEditorSkin();
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
            _notes = _hubRoot.Q(LiveOpsHubPaths.ShellElementNames.ShellNotes);
            _body = _hubRoot.Q(LiveOpsHubPaths.ShellElementNames.SectionBody);
            _rail = new LiveOpsHubRail(_hubRoot, Navigate);

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

            // (10)
            UpdateCompilingNote();
            _notes.schedule.Execute(UpdateCompilingNote).Every(CompilationPollMilliseconds);
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

        /// <summary>Unity hỏi khi đóng tab có *: lưu asset qua phiên; chỉ hạ cờ khi file đã ghi (lưu hỏng thì tab vẫn *).</summary>
        public override void SaveChanges()
        {
            if (_services == null || !_services.Session.Save()) return;
            base.SaveChanges();
            UpdateUnsavedState();
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
            // INTERIM(G-SHELLPOLISH): menu ⋮ mới có "Tắt chuyển động"; "Kiểm lại tất cả (F5)" cần phiên kiểm (G-HOSTUI), "Mở tài liệu"
            // và "Hiện dữ liệu mẫu" thêm ở G-SHELLPOLISH (mục 12 I-8) — không thêm mục disabled trỏ tới thứ chưa có.
            bool reduceMotion = EditorPrefs.GetBool(ReduceMotionPreferenceKey, false);
            menu.AddItem(new GUIContent(LiveOpsHubStrings.ShellReduceMotionMenu), reduceMotion, () =>
            {
                bool enabled = !EditorPrefs.GetBool(ReduceMotionPreferenceKey, false);
                EditorPrefs.SetBool(ReduceMotionPreferenceKey, enabled);
                _hubRoot?.EnableInClassList(LiveOpsHubClassNames.NoMotion, enabled);
            });
        }

        // ------------------------------------------------------------------------------------------------------------ cho test

        internal VisualElement HubRoot => _hubRoot;
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
                builder.WithCalendarAsset(_pendingAsset);
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
            bus.HealthInvalidated += OnHealthInvalidated;
            bus.OutcomeRequested += OnOutcomeRequested;
            bus.OutcomeCleared += OnOutcomeCleared;
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
            bus.HealthInvalidated -= OnHealthInvalidated;
            bus.OutcomeRequested -= OnOutcomeRequested;
            bus.OutcomeCleared -= OnOutcomeCleared;
            _isServicesSubscribed = false;
        }

        private void ReleaseServices()
        {
            if (_services == null) return;
            // R-25: domain reload/đóng cửa sổ giữa lúc kéo hoặc kiểm — phiên huỷ kéo dở (không để Undo group mở) và dừng nhịp kiểm.
            if (_ownsServices) _services.Session.Dispose();
            _services = null;
            _ownsServices = false;
        }

        private void OnSessionDocumentChanged()
        {
            UpdateUnsavedState();
            InvalidateAndRefreshHealth();
        }

        private void OnSessionCheckChanged()
        {
            InvalidateAndRefreshHealth();
        }

        private void OnSessionDiskChangeDetected()
        {
            // INTERIM(G-SHELLPOLISH): chưa có băng "asset đổi trên đĩa" (mục 12 I-8) — chỉ log; phiên vẫn giữ bản chụp nháp nên không mất việc.
            Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.InterimDiskConflictLog, _services.Session.AssetFileName));
            InvalidateAndRefreshHealth();
        }

        private void OnHealthInvalidated()
        {
            InvalidateAndRefreshHealth();
        }

        private void OnOutcomeRequested(LiveOpsOutcomeRecord outcome)
        {
            // Outcome sống trong trạng thái cửa sổ (qua domain reload); view outcome của G-HOSTUI đọc từ đây.
            windowState.LastOutcome = outcome;
        }

        private void OnOutcomeCleared()
        {
            windowState.LastOutcome = null;
        }

        private void InvalidateAndRefreshHealth()
        {
            LiveOpsHealthThrottle.InvalidateAll();
            RefreshHealth();
        }

        /// <summary>Tab có * khi và chỉ khi phiên còn thay đổi chưa lưu (8.3); câu hỏi nêu tên asset, số thay đổi và id ngắn.</summary>
        internal void UpdateUnsavedState()
        {
            LiveOpsHubCalendarSession session = _services != null ? _services.Session : null;
            bool hasUnsaved = session != null && session.HasUnsavedChanges;
            if (hasUnsaved) saveChangesMessage = BuildSaveChangesMessage(session);
            if (hasUnsavedChanges != hasUnsaved) hasUnsavedChanges = hasUnsaved;
        }

        private static string BuildSaveChangesMessage(LiveOpsHubCalendarSession session)
        {
            const int MaximumListedIds = 3;
            LiveEventCalendarDiffResult diff = session.UnsavedDiff;
            List<string> ids = new List<string>();
            foreach (LiveEventCalendarChange change in diff.Changes)
            {
                if (ids.Count >= MaximumListedIds) break;
                if (!string.IsNullOrEmpty(change.ItemId) && !ids.Contains(change.ItemId)) ids.Add(change.ItemId);
            }
            int count = Math.Max(diff.ChangeCount, 1);
            string list = ids.Count == 0 ? LiveOpsHubStrings.ServicesSaveChangesDocumentFields : string.Join(LiveOpsHubStrings.ServicesHealthItemSeparator, ids);
            if (diff.ChangeCount > ids.Count && ids.Count > 0)
            {
                list = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesSaveChangesMoreFormat, list, diff.ChangeCount - ids.Count);
            }
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesSaveChangesMessageFormat, session.AssetFileName, count, list);
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
            _compilingNote = null;
            _hubRoot = null;
        }
    }
}
