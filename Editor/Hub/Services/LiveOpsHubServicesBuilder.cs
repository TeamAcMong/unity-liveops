using DreamTech.LiveOps.Unity;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Builder của <see cref="LiveOpsHubServices"/> theo khuôn <c>With…</c> của package. Mặc định là đường thật của Editor (đồng hồ hệ
    /// thống, clipboard hệ thống, hộp modal, git user.name, múi giờ máy, parser của game, AssetDatabase, asset nhớ theo project); test
    /// và kịch bản chụp thay từng port. Mọi <c>With…</c> nhận null = giữ mặc định.
    /// </summary>
    public sealed class LiveOpsHubServicesBuilder
    {
        private ILiveOpsClock _clock;
        private ILiveOpsHubClipboard _clipboard;
        private ILiveOpsHubFileDialog _fileDialog;
        private ILiveOpsHubPublisherIdentity _publisherIdentity;
        private ILiveOpsHubTimeZone _timeZone;
        private LiveEventCalendarValidator _validator;
        private LiveEventCalendarAsset _calendarAsset;
        private bool _hasCalendarAsset;
        private ILiveOpsHubCompilationState _compilationState;
        private bool _autoCheckOnOpen = true;
        private ILiveOpsHubConfirmationPresenter _confirmation;
        private ILiveOpsHubActions _actions;
        private ILiveOpsHubJsonReadBack _jsonReadBack;
        private ILiveOpsHubLayoutLoader _layoutLoader;
        private LiveOpsHubAssetLocator _assetLocator;

        public LiveOpsHubServicesBuilder WithClock(ILiveOpsClock clock)
        {
            _clock = clock;
            return this;
        }

        public LiveOpsHubServicesBuilder WithClipboard(ILiveOpsHubClipboard clipboard)
        {
            _clipboard = clipboard;
            return this;
        }

        public LiveOpsHubServicesBuilder WithFileDialog(ILiveOpsHubFileDialog fileDialog)
        {
            _fileDialog = fileDialog;
            return this;
        }

        public LiveOpsHubServicesBuilder WithPublisherIdentity(ILiveOpsHubPublisherIdentity identity)
        {
            _publisherIdentity = identity;
            return this;
        }

        public LiveOpsHubServicesBuilder WithTimeZone(ILiveOpsHubTimeZone timeZone)
        {
            _timeZone = timeZone;
            return this;
        }

        public LiveOpsHubServicesBuilder WithValidator(LiveEventCalendarValidator validator)
        {
            _validator = validator;
            return this;
        }

        /// <summary>
        /// Mở đúng asset này và bỏ qua bộ tìm asset (test, chụp ảnh). null = phiên không có asset, không tìm. Kèm
        /// <see cref="WithAssetLocator"/> thì vẫn có bộ tìm (đường inspector: mở asset này VÀ nhớ GUID theo project).
        /// </summary>
        public LiveOpsHubServicesBuilder WithCalendarAsset(LiveEventCalendarAsset asset)
        {
            _calendarAsset = asset;
            _hasCalendarAsset = true;
            return this;
        }

        public LiveOpsHubServicesBuilder WithCompilationState(ILiveOpsHubCompilationState compilationState)
        {
            _compilationState = compilationState;
            return this;
        }

        /// <summary>Mặc định true (Q-11): kiểm một lần khi phiên nạp xong asset. Test/chụp ảnh tắt để dựng đúng trạng thái.</summary>
        public LiveOpsHubServicesBuilder WithAutoCheckOnOpen(bool autoCheckOnOpen)
        {
            _autoCheckOnOpen = autoCheckOnOpen;
            return this;
        }

        internal LiveOpsHubServicesBuilder WithConfirmation(ILiveOpsHubConfirmationPresenter presenter)
        {
            _confirmation = presenter;
            return this;
        }

        internal LiveOpsHubServicesBuilder WithActions(ILiveOpsHubActions actions)
        {
            _actions = actions;
            return this;
        }

        /// <summary>(V-16)</summary>
        internal LiveOpsHubServicesBuilder WithJsonReadBack(ILiveOpsHubJsonReadBack jsonReadBack)
        {
            _jsonReadBack = jsonReadBack;
            return this;
        }

        /// <summary>(V-16)</summary>
        internal LiveOpsHubServicesBuilder WithLayoutLoader(ILiveOpsHubLayoutLoader layoutLoader)
        {
            _layoutLoader = layoutLoader;
            return this;
        }

        /// <summary>Bộ tìm asset với khoá EditorUserSettings riêng (test không đè asset người dùng đang mở).</summary>
        internal LiveOpsHubServicesBuilder WithAssetLocator(LiveOpsHubAssetLocator assetLocator)
        {
            _assetLocator = assetLocator;
            return this;
        }

        public LiveOpsHubServices Build()
        {
            ILiveOpsClock clock = _clock ?? new SystemLiveOpsClock();
            ILiveOpsHubTimeZone timeZone = _timeZone ?? new DeviceLiveOpsHubTimeZone();
            ILiveOpsHubPublisherIdentity publisherIdentity = _publisherIdentity ?? new GitLiveOpsHubPublisherIdentity();
            LiveEventCalendarValidator validator = _validator ?? LiveEventCalendarValidator.Default;
            LiveOpsHubSectionBus bus = new LiveOpsHubSectionBus();
            // Asset tường minh (kể cả null) = không tìm: test/chụp ảnh không bao giờ đọc hay ghi GUID nhớ của người dùng. Bộ tìm truyền
            // TƯỜNG MINH thì luôn thắng — đường inspector "Mở trong LiveOps Hub" mở đúng asset mà vẫn nhớ GUID và vẫn đếm được số lịch
            // trong project (HelpBox "Có 2 LiveEventCalendarAsset", 7.1).
            LiveOpsHubAssetLocator locator = _assetLocator ?? (_hasCalendarAsset ? null : new LiveOpsHubAssetLocator());
            LiveOpsHubCalendarSession session = new LiveOpsHubCalendarSession(clock, validator, publisherIdentity, locator, bus);

            LiveOpsHubServices services = new LiveOpsHubServices(
                clock,
                _clipboard ?? new EditorLiveOpsHubClipboard(),
                _fileDialog ?? new EditorLiveOpsHubFileDialog(),
                publisherIdentity,
                timeZone,
                _compilationState ?? new EditorLiveOpsHubCompilationState(),
                validator,
                _confirmation ?? new ModalLiveOpsHubConfirmationPresenter(),
                // INTERIM(G-PASTE): chưa có action Dán/Nhập JSON thật (mục 12 I-3) — G-PASTE thay bằng LiveOpsHubPasteRunningJsonAction.
                _actions ?? new InterimUnavailableHubActions(),
                _jsonReadBack ?? new GameParserLiveOpsHubJsonReadBack(),
                _layoutLoader ?? new AssetDatabaseLiveOpsHubLayoutLoader(),
                bus,
                session,
                new LiveOpsHubFormat(timeZone.DeviceOffsetAt(clock.UtcNow)));
            session.Initialize(_calendarAsset, _hasCalendarAsset, _autoCheckOnOpen);
            return services;
        }
    }
}
