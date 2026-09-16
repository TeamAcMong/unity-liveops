using System;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Composition root của một cửa sổ hub: mọi port (đồng hồ, clipboard, hộp file, người đăng, múi giờ, biên dịch, xác nhận, action,
    /// đọc lại JSON, nạp layout), bộ kiểm, bus section → cửa sổ, phiên lịch và bộ định dạng chữ. Dựng bằng
    /// <see cref="LiveOpsHubServicesBuilder"/>; màn chỉ nhận services qua ctor/host, không tự tạo adapter nào — nhờ vậy test và kịch bản
    /// chụp thay đồng hồ, clipboard, hộp modal mà không đụng màn. Chữ ký đóng băng khi cổng W3 xanh (PD-35).
    /// </summary>
    public sealed class LiveOpsHubServices
    {
        internal LiveOpsHubServices(ILiveOpsClock clock, ILiveOpsHubClipboard clipboard, ILiveOpsHubFileDialog fileDialog,
            ILiveOpsHubPublisherIdentity publisherIdentity, ILiveOpsHubTimeZone timeZone, ILiveOpsHubCompilationState compilationState,
            LiveEventCalendarValidator validator, ILiveOpsHubConfirmationPresenter confirmation, ILiveOpsHubActions actions,
            ILiveOpsHubJsonReadBack jsonReadBack, ILiveOpsHubLayoutLoader layoutLoader, LiveOpsHubSectionBus bus,
            LiveOpsHubCalendarSession session, LiveOpsHubFormat format)
        {
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
            FileDialog = fileDialog ?? throw new ArgumentNullException(nameof(fileDialog));
            PublisherIdentity = publisherIdentity ?? throw new ArgumentNullException(nameof(publisherIdentity));
            TimeZone = timeZone ?? throw new ArgumentNullException(nameof(timeZone));
            CompilationState = compilationState ?? throw new ArgumentNullException(nameof(compilationState));
            Validator = validator ?? throw new ArgumentNullException(nameof(validator));
            Confirmation = confirmation ?? throw new ArgumentNullException(nameof(confirmation));
            Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            JsonReadBack = jsonReadBack ?? throw new ArgumentNullException(nameof(jsonReadBack));
            LayoutLoader = layoutLoader ?? throw new ArgumentNullException(nameof(layoutLoader));
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Format = format ?? throw new ArgumentNullException(nameof(format));
        }

        /// <summary>Port đồng hồ của core: <see cref="SystemLiveOpsClock"/> ở Editor, <see cref="ManualLiveOpsClock"/> ở test/chụp ảnh.</summary>
        public ILiveOpsClock Clock { get; }

        public ILiveOpsHubClipboard Clipboard { get; }
        public ILiveOpsHubFileDialog FileDialog { get; }
        public ILiveOpsHubPublisherIdentity PublisherIdentity { get; }
        public ILiveOpsHubTimeZone TimeZone { get; }
        public ILiveOpsHubCompilationState CompilationState { get; }
        public LiveEventCalendarValidator Validator { get; }

        internal ILiveOpsHubConfirmationPresenter Confirmation { get; }
        internal ILiveOpsHubActions Actions { get; }

        /// <summary>(V-16) Parser của game đọc lại JSON — seam cho kịch bản (h)/lệch.</summary>
        internal ILiveOpsHubJsonReadBack JsonReadBack { get; }

        /// <summary>(V-16) Nạp UXML/USS — seam cho kịch bản thiếu UXML.</summary>
        internal ILiveOpsHubLayoutLoader LayoutLoader { get; }

        internal LiveOpsHubSectionBus Bus { get; }
        internal LiveOpsHubCalendarSession Session { get; }

        /// <summary>Định dạng chữ với lệch giờ máy lúc dựng services.</summary>
        internal LiveOpsHubFormat Format { get; }
    }
}
