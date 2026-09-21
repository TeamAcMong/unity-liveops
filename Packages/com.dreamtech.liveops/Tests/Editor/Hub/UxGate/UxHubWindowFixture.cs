using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Một cỡ cửa sổ của ma trận bố cục W8-UX. <see cref="ToString"/> là nhãn "700x560" để tên test NUnit và tên file JSON đọc
    /// được ngay mà không phải tra bảng.
    /// </summary>
    internal readonly struct UxWindowSize
    {
        public UxWindowSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; }
        public int Height { get; }

        /// <summary>
        /// Cỡ này nằm ở bậc <c>--medium</c> của hub (inspector Lịch thành drawer "mở khi chọn"). Mốc lấy thẳng từ
        /// <see cref="UxHubWindowFixture.MediumBreakpointWidth"/>, tức từ chính hằng của sản phẩm.
        /// </summary>
        public bool IsMedium => Width < UxHubWindowFixture.MediumBreakpointWidth;

        public override string ToString()
        {
            return Width.ToString(CultureInfo.InvariantCulture) + "x" + Height.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Mở cửa sổ LiveOps Hub THẬT cho cổng W8-UX: services mẫu thiết kế (13/9/2026 08:47, +7, đã kiểm), registry màn thật, ngôn
    /// ngữ chọn được, cỡ cửa sổ đặt SAU Show (SP-16). Test chỉ dựng trạng thái ban đầu ở đây; mọi thao tác sau đó phải đi qua
    /// <see cref="UxEventSender"/>. <see cref="Dispose"/> đóng cửa sổ, popover, trả ngôn ngữ và dọn services — gọi trong
    /// <c>[TearDown]</c> để assert fail không để lại cửa sổ cho test sau.
    /// </summary>
    // Category ở đây là dấu cho code-lint (luật test-ui-category) và cho người đọc: lớp trợ giúp này chỉ chạy được khi
    // Unity CÓ đồ hoạ — panel/SendEvent/layout đều vô nghĩa dưới -nographics.
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal sealed class UxHubWindowFixture : IDisposable
    {
        /// <summary>
        /// Bề rộng dưới mốc này thì hub ở breakpoint <c>--medium</c> (inspector Lịch là drawer).
        /// <para>
        /// (soát W10) Đọc THẲNG hằng của sản phẩm, không chép tay: bản chép cũ ghi 1000 trong khi
        /// <see cref="LiveOpsHubBreakpoints.MediumBelowWidth"/> là 1100, nên cỡ 1024×700 bị cổng coi là KHÔNG-medium
        /// đúng lúc hub thật đang ở bậc <c>--medium</c> — một khoảng 100px mà mọi nhánh rẽ theo mốc này rẽ sai trong im
        /// lặng. Hằng khởi tạo từ hằng thì nó không lệch lại được nữa.
        /// </para>
        /// </summary>
        internal const float MediumBreakpointWidth = LiveOpsHubBreakpoints.MediumBelowWidth;

        /// <summary>Biến môi trường đặt nhãn lượt ghi JSON (tên gói hoặc tên đợt); không có thì "local".</summary>
        internal const string DiagnosticsLabelVariable = "LIVEOPS_UX_GATE_LABEL";

        private const string DefaultDiagnosticsLabel = "local";
        private const string DiagnosticsRelativeDirectory = ".cache/unity-liveops/ux-gate";

        /// <summary>
        /// Sáu cỡ user chốt 17/9/2026. Test hành trình trỏ vào bộ này THEO CHỈ MỤC (<c>AllSizes[4]</c> = 1440x900 là cỡ
        /// "rộng rãi" mà mọi ca inspector đứng trên), nên thứ tự và chỉ mục của sáu dòng dưới đây là HỢP ĐỒNG: chen một cỡ
        /// vào giữa sẽ dời im lặng khoảng hai chục lời gọi sang một cỡ khác mà không test nào đỏ. Cỡ mới của ma trận bố
        /// cục vì vậy vào <see cref="AllLayoutSizes"/>, không vào đây (W9-20).
        /// </summary>
        internal static readonly UxWindowSize[] AllSizes =
        {
            new UxWindowSize(700, 560),
            new UxWindowSize(820, 560),
            new UxWindowSize(1024, 700),
            new UxWindowSize(1280, 760),
            new UxWindowSize(1440, 900),
            new UxWindowSize(1920, 1040),
        };

        /// <summary>
        /// Bộ cỡ của MA TRẬN KIỂM BỐ CỤC: sáu cỡ trên cộng 950x700 (W9-20).
        /// <para>
        /// Vì sao thêm 950: breakpoint "--medium" trải 900…1099px mà ma trận cũ chỉ thử ĐÚNG MỘT điểm trong khoảng đó
        /// (1024). Toolbar màn Lịch nhường chỗ theo CỠ nên luật nhường áp cho cả bản tiếng Anh, và thanh tiếng Anh ở 1024
        /// còn 113px trống — nghĩa là menu "Bắt lưới" bản en (đo được 112px) vừa đủ ở 1024 và hết chỗ ngay dưới ~1000px.
        /// Khoảng 900–1099 vì thế là một khoảng MÙ: cổng xanh ở 1024 không nói gì về 950. Đây là cỡ kiểm, không phải cỡ
        /// thiết kế mới — nó nằm giữa 820 và 1024 đúng chỗ bậc breakpoint mới.
        /// </para>
        /// </summary>
        internal static readonly UxWindowSize[] AllLayoutSizes =
        {
            new UxWindowSize(700, 560),
            new UxWindowSize(820, 560),
            new UxWindowSize(950, 700),
            new UxWindowSize(1024, 700),
            new UxWindowSize(1280, 760),
            new UxWindowSize(1440, 900),
            new UxWindowSize(1920, 1040),
        };

        internal static readonly LiveOpsHubLanguageId[] AllLanguages = { LiveOpsHubLanguageId.Vietnamese, LiveOpsHubLanguageId.English };

        /// <summary>Cửa sổ nhỏ hơn cỡ yêu cầu quá mức này nghĩa là hệ điều hành đã kẹp cửa sổ — số đo bố cục không còn của cỡ đó.</summary>
        private const float WindowClampTolerance = 4f;

        private LiveOpsHubLanguageScope _languageScope;

        private UxHubWindowFixture(LiveOpsHubWindow window, LiveOpsHubServices services, IReadOnlyList<IHubSection> sections,
            UxWindowSize size, LiveOpsHubLanguageId language, LiveOpsHubLanguageScope languageScope)
        {
            Window = window;
            Services = services;
            Sections = sections;
            Size = size;
            Language = language;
            _languageScope = languageScope;
        }

        public LiveOpsHubWindow Window { get; private set; }
        public LiveOpsHubServices Services { get; }
        public IReadOnlyList<IHubSection> Sections { get; }
        public UxWindowSize Size { get; private set; }
        public LiveOpsHubLanguageId Language { get; }
        public VisualElement Root => Window.rootVisualElement;

        public CalendarSection Calendar => SectionOf<CalendarSection>();
        public RecurringRulesSection Recurring => SectionOf<RecurringRulesSection>();

        /// <summary>
        /// Mở hub ở <paramref name="sectionId"/>. <paramref name="services"/> null = mẫu thiết kế; <paramref name="prepare"/> chạy
        /// TRƯỚC Show (view của màn dựng trong CreateGUI, sửa sau Show là đua với lượt layout đầu) và chỉ được dựng trạng thái
        /// KHỞI ĐẦU giống người dùng mở lại hub — không dùng để gây hành vi đang được kiểm.
        /// <para>
        /// Chỗ duy nhất hiện dùng nó là trạng thái "đang ẩn n làn" (UX-12): làn chỉ ẩn được qua menu chuột phải (GenericMenu của
        /// IMGUI, <c>SendEvent</c> không với tới), nên nếu không dựng sẵn thì test khoá UX-12 buộc phải <c>Assert.Ignore</c> —
        /// và một test Ignore ở CẢ hai bản Unity không khoá được lỗi nào.
        /// </para>
        /// </summary>
        public static UxHubWindowFixture Open(string sectionId, UxWindowSize size, LiveOpsHubLanguageId language,
            LiveOpsHubServices services = null, Action<IReadOnlyList<IHubSection>> prepare = null)
        {
            LiveOpsHubLanguageScope languageScope = LiveOpsHubLanguage.Override(language);
            LiveOpsHubServices chosen = services ?? LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            List<IHubSection> sections = LiveOpsHubSections.Create(chosen);
            if (prepare != null) prepare(sections);
            LiveOpsHubWindow window = LiveOpsHubWindow.OpenWithServices(chosen, sections, sectionId);
            window.position = new Rect(0f, 0f, size.Width, size.Height);
            return new UxHubWindowFixture(window, chosen, sections, size, language, languageScope);
        }

        /// <summary>Services mẫu thiết kế với hộp xác nhận và đồng hồ do test chọn (UX-06 cần "18/9 10:00" và presenter ghi thứ tự).</summary>
        public static LiveOpsHubServices DesignServices(ILiveOpsHubConfirmationPresenter confirmation, DateTime nowUtc)
        {
            ManualLiveOpsClock clock = new ManualLiveOpsClock(nowUtc, true);
            LiveOpsHubServicesBuilder builder = LiveOpsHubTestServices.CreateBuilder(clock)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document));
            if (confirmation != null) builder = builder.WithConfirmation(confirmation);
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(builder);
            services.Session.RunCheckToCompletion();
            return services;
        }

        /// <summary>
        /// Chờ root có layout rồi chờ thêm để GeometryChanged (breakpoint, bề rộng track) và lượt dựng lại sau nó chạy xong.
        /// Cửa sổ bị hệ điều hành KẸP nhỏ hơn cỡ yêu cầu (màn hình máy chạy thấp hơn 1040) KHÔNG làm test đỏ: lượt kiểm vẫn chạy
        /// trên cửa sổ thật đó — lỗi tìm được vẫn là lỗi thật — nhưng <see cref="IsClamped"/>/<see cref="ClampNote"/> ghi lại để
        /// câu assert VÀ JSON chẩn đoán nói rõ cỡ nào chưa được đo đúng trên máy này. Làm đỏ thì cổng hỏng vĩnh viễn trên mọi máy
        /// màn hình nhỏ; im lặng thì người đọc tưởng đã đo đủ sáu cỡ.
        /// <para>
        /// Kẹp đo trên <c>window.position</c> chứ không trên chiều cao root trừ đi một hằng "chrome" đoán trước: bản đầu tiên trừ
        /// 40 px nên 1440x900 kẹp còn 886 (hụt 14) KHÔNG bị đánh dấu, và lượt cổng tưởng đã đo đủ sáu cỡ (R-08). Khung cửa sổ là
        /// thứ hệ điều hành kẹp, so đúng nó thì không cần đoán chrome.
        /// </para>
        /// </summary>
        public IEnumerator WaitForLayout()
        {
            yield return UxEventSender.WaitUntil(() => LiveOpsHubWindowTestScope.HasLayout(Root),
                "cửa sổ hub " + Size + " không có layout — test UxGate phải chạy KHÔNG -nographics");
            yield return UxEventSender.Settle(UxEventSender.SettleFrames * 3, UxEventSender.SettleMilliseconds * 4);
            Rect position = Window.position;
            IsClamped = position.width < Size.Width - WindowClampTolerance || position.height < Size.Height - WindowClampTolerance;
            ClampNote = IsClamped
                ? Size + " bị kẹp còn cửa sổ " + UxLayoutAuditor.Number(position.width) + "x" + UxLayoutAuditor.Number(position.height)
                  + " (vùng vẽ " + UxLayoutAuditor.Number(Root.worldBound.width) + "x" + UxLayoutAuditor.Number(Root.worldBound.height)
                  + ") — màn hình máy chạy nhỏ hơn cỡ yêu cầu"
                : string.Empty;
        }

        /// <summary>Rỗng khi cửa sổ đúng cỡ; khác rỗng = cỡ này chưa được đo đúng trên máy đang chạy (xem <see cref="WaitForLayout"/>).</summary>
        public string ClampNote { get; private set; } = string.Empty;

        /// <summary>true = hệ điều hành kẹp cửa sổ nhỏ hơn cỡ yêu cầu; JSON chẩn đoán ghi cờ này để báo cáo đọc được.</summary>
        public bool IsClamped { get; private set; }

        /// <summary>Đổi cỡ cửa sổ như kéo mép cửa sổ (UX-03): đặt position rồi chờ layout ổn định.</summary>
        public IEnumerator Resize(UxWindowSize size)
        {
            Size = size;
            Window.position = new Rect(0f, 0f, size.Width, size.Height);
            yield return WaitForLayout();
        }

        /// <summary>
        /// Đi tới màn như người dùng: BẤM hàng rail của màn đó. Không gọi <c>ApplyNavigation</c> — điều hướng bằng code bỏ qua
        /// đúng đường chuột mà hub phải hỗ trợ.
        /// <para>
        /// Chỉ dùng được ở cỡ CÓ hàng rail. Ở rail thu gọn, bấm ô icon mở <c>GenericMenu.DropDown</c> (IMGUI) — phiên tự động
        /// không lái được menu đó, và ở 2022.3 batchmode menu còn ở lại chặn mọi test sau (lượt đầu của đợt này treo tới hết hạn
        /// giờ vì đúng chuyện đó). Đường hẹp vì vậy kiểm bằng <see cref="RailEntryFor"/> + <c>PickReaches</c>, không bấm.
        /// </para>
        /// </summary>
        public IEnumerator NavigateByRail(string sectionId)
        {
            if (string.Equals(Window.ActiveSectionId, sectionId, StringComparison.Ordinal)) yield break;
            VisualElement row = Window.Rail.GetRow(sectionId);
            Assert.IsTrue(row != null && UxLayoutAuditor.IsShownOnScreen(row),
                "không có hàng rail nào hiện ra để bấm tới màn '" + sectionId + "' ở cỡ " + Size
                + " — ở rail thu gọn hãy dùng RailEntryFor + PickReaches, đừng bấm (ô icon mở GenericMenu)");
            yield return UxEventSender.Click(Window, row);
            yield return UxEventSender.WaitUntil(() => string.Equals(Window.ActiveSectionId, sectionId, StringComparison.Ordinal),
                "bấm rail không đưa hub tới màn '" + sectionId + "'");
            yield return UxEventSender.Settle(UxEventSender.SettleFrames * 2, UxEventSender.SettleMilliseconds * 2);
        }

        /// <summary>
        /// Lối vào rail của một màn ở cỡ hiện tại: hàng rail khi rail còn hàng, ô icon của tầng chứa màn đó khi rail đã thu gọn.
        /// Null = ở cỡ này người dùng không có lối nào tới màn đó.
        /// </summary>
        public VisualElement RailEntryFor(string sectionId)
        {
            VisualElement row = Window.Rail.GetRow(sectionId);
            if (row != null && UxLayoutAuditor.IsShownOnScreen(row)) return row;
            int sectionIndex = IndexOfSection(sectionId);
            IReadOnlyList<VisualElement> cells = Window.Rail.NarrowCells;
            if (sectionIndex < 0 || cells == null || sectionIndex >= cells.Count) return null;
            return cells[sectionIndex];
        }

        /// <summary>
        /// Nút "Thêm đợt" của header màn Lịch, tìm theo CHỮ của nó trong catalog. Lấy nút ĐẦU TIÊN của vùng hành động là sai:
        /// thêm một nút nữa vào header là test UX-10 bấm nhầm nút và đỏ vì một lý do không có thật (R-19).
        /// </summary>
        public Button AddEventButton()
        {
            VisualElement actions = Root.Q(LiveOpsHubPaths.ShellElementNames.SectionActions);
            if (actions == null) return null;
            string label = LiveOpsHubStrings.CalendarAddEventButton;
            foreach (Button button in actions.Query<Button>().ToList())
            {
                if (string.Equals(button.text, label, StringComparison.Ordinal)) return button;
            }
            return null;
        }

        /// <summary>
        /// Tắt HẲN mọi thẻ hover của cửa sổ — thẻ đang hiện, thẻ đang chờ 500 ms, và thẻ đang ghim — ở cả shell lẫn từng màn.
        /// <para>
        /// (W10-10) Vì sao phải có hàm này thay vì tin vào "rê chuột ra chỗ khác rồi chờ vài khung": hẹn giờ của thẻ hover
        /// đếm bằng GIỜ THẬT (500 ms hiện, 100 ms ẩn) còn lượt kiểm đếm bằng KHUNG HÌNH. Máy chạy ba lượt Unity song song
        /// thì cùng một số khung ứng với nhiều thời gian hơn, nên thẻ kịp hiện; máy rảnh thì không. Hệ quả đo được: màn
        /// <c>calendar-worst-data</c> ra 178, 180 hoặc 184 chỗ trên CÙNG một cây mã, tuỳ thứ tự chạy. Một con số mà cổng
        /// đang dùng để theo dõi phiếu khác thì không được phép phụ thuộc vào máy chạy nhanh hay chậm.
        /// </para>
        /// <para>
        /// Đây KHÔNG phải giấu phát hiện: thẻ hover là một màn riêng, cần luật riêng của nó. Để nó lọt vào màn khác thì cả
        /// hai màn cùng đo sai — màn kia đếm thêm thứ không phải của mình, còn thẻ hover thì không ai đo nó có chủ đích.
        /// </para>
        /// </summary>
        public void HideHoverCards()
        {
            LiveOpsHubWindow hub = Window as LiveOpsHubWindow;
            if (hub != null && hub.HoverCardHost != null) hub.HoverCardHost.Hide();
            for (int index = 0; index < Sections.Count; index++)
            {
                if (Sections[index] is CalendarSection calendar && calendar.HoverCardHost != null)
                {
                    calendar.HoverCardHost.Hide();
                }
                if (Sections[index] is ValidationSection validation && validation.HoverCardHost != null)
                {
                    validation.HoverCardHost.Hide();
                }
            }
        }

        /// <summary>
        /// Cây <paramref name="root"/> còn thẻ hover nào ĐANG HIỆN không. Dò bằng CLASS chứ không bằng danh sách chủ thẻ:
        /// thêm một màn có thẻ hover mà quên khai ở <see cref="HideHoverCards"/> thì câu này vẫn bắt được, và bắt bằng một
        /// ca đỏ chứ không bằng một con số lệch âm thầm.
        /// </summary>
        public static bool HasVisibleHoverCard(VisualElement root)
        {
            return root != null && root.Q(className: LiveOpsHubClassNames.HoverCardVisible) != null;
        }

        /// <summary>Thanh timeline theo khoá (entry key) — tên element của thanh là khoá của nó.</summary>
        public LiveOpsTimelineBar BarOf(string barKey)
        {
            CalendarSection calendar = Calendar;
            Assert.IsNotNull(calendar, "hub không có màn Lịch");
            LiveOpsTimelineBar bar = calendar.Timeline.FindBar(barKey);
            Assert.IsNotNull(bar, "trục không vẽ thanh '" + barKey + "'");
            return bar;
        }

        /// <summary>Đường dẫn JSON chẩn đoán: <c>~/.cache/unity-liveops/ux-gate/&lt;nhãn&gt;/&lt;bản Unity&gt;/&lt;tên&gt;.json</c>.</summary>
        public static string DiagnosticsPath(string fileStem)
        {
            string label = Environment.GetEnvironmentVariable(DiagnosticsLabelVariable);
            if (string.IsNullOrEmpty(label)) label = DefaultDiagnosticsLabel;
            string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string directory = Path.Combine(Path.Combine(Path.Combine(home, DiagnosticsRelativeDirectory), label), Application.unityVersion);
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, fileStem + ".json");
        }

        public void Dispose()
        {
            if (Window != null) Window.Close();
            Window = null;
            CloseStrayWindows();
            if (_languageScope != null) _languageScope.Dispose();
            _languageScope = null;
            LiveOpsHubTestServices.ReleaseAll();
        }

        /// <summary>Đóng popover/hộp xác nhận mà hành trình mở ra và chưa kịp đóng — cửa sổ sót làm test sau nhận sự kiện nhầm chỗ.</summary>
        internal static void CloseStrayWindows()
        {
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null) continue;
                Type type = window.GetType();
                if (type == typeof(LiveOpsConfirmWindow) || type == typeof(UnityEditor.PopupWindow)) window.Close();
            }
        }

        private int IndexOfSection(string sectionId)
        {
            for (int index = 0; index < Sections.Count; index++)
            {
                if (string.Equals(Sections[index].Id, sectionId, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        private T SectionOf<T>() where T : class, IHubSection
        {
            for (int index = 0; index < Sections.Count; index++)
            {
                if (Sections[index] is T section) return section;
            }
            return null;
        }
    }
}
