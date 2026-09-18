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

        /// <summary>Breakpoint "--medium" của hub (inspector thành drawer) nằm ở 820 — cỡ này là nơi UX-07 hỏng.</summary>
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
        /// <summary>Bề rộng dưới mốc này thì hub ở breakpoint "--medium" (inspector Lịch là drawer).</summary>
        internal const int MediumBreakpointWidth = 1000;

        /// <summary>Biến môi trường đặt nhãn lượt ghi JSON (tên gói hoặc tên đợt); không có thì "local".</summary>
        internal const string DiagnosticsLabelVariable = "LIVEOPS_UX_GATE_LABEL";

        private const string DefaultDiagnosticsLabel = "local";
        private const string DiagnosticsRelativeDirectory = ".cache/unity-liveops/ux-gate";

        /// <summary>Sáu cỡ user chốt 17/9/2026 cho MỌI màn.</summary>
        internal static readonly UxWindowSize[] AllSizes =
        {
            new UxWindowSize(700, 560),
            new UxWindowSize(820, 560),
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
        /// khởi đầu giống người dùng mở lại hub — không dùng để gây hành vi đang được kiểm.
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

        /// <summary>Chờ root có layout rồi chờ thêm để GeometryChanged (breakpoint, bề rộng track) và lượt dựng lại sau nó chạy xong.</summary>
        public IEnumerator WaitForLayout()
        {
            yield return UxEventSender.WaitUntil(() => LiveOpsHubWindowTestScope.HasLayout(Root),
                "cửa sổ hub " + Size + " không có layout — test UxGate phải chạy KHÔNG -nographics");
            yield return UxEventSender.Settle(UxEventSender.SettleFrames * 3, UxEventSender.SettleMilliseconds * 4);
            Rect bound = Root.worldBound;
            Assert.IsTrue(bound.width >= Size.Width - WindowClampTolerance && bound.height >= Size.Height - WindowClampTolerance - HostChromeHeight,
                "cửa sổ " + Size + " bị kẹp còn " + bound.width + "×" + bound.height + " — số đo bố cục không còn là của cỡ này " +
                "(màn hình máy chạy nhỏ hơn cỡ yêu cầu?)");
        }

        /// <summary>Dải tab của cửa sổ nổi ăn vào chiều cao root; không tính là cửa sổ bị kẹp.</summary>
        private const float HostChromeHeight = 40f;

        /// <summary>Đổi cỡ cửa sổ như kéo mép cửa sổ (UX-03): đặt position rồi chờ layout ổn định.</summary>
        public IEnumerator Resize(UxWindowSize size)
        {
            Size = size;
            Window.position = new Rect(0f, 0f, size.Width, size.Height);
            yield return WaitForLayout();
        }

        /// <summary>
        /// Đi tới màn như người dùng: bấm hàng rail của màn đó; rail hẹp (không có hàng hiện) thì bấm ô rail thu gọn cùng thứ tự.
        /// Không gọi <c>ApplyNavigation</c> — điều hướng bằng code bỏ qua đúng đường chuột mà hub phải hỗ trợ ở cỡ hẹp.
        /// </summary>
        public IEnumerator NavigateByRail(string sectionId)
        {
            if (string.Equals(Window.ActiveSectionId, sectionId, StringComparison.Ordinal)) yield break;
            VisualElement row = Window.Rail.GetRow(sectionId);
            if (row != null && UxLayoutAuditor.IsShownOnScreen(row))
            {
                yield return UxEventSender.Click(Window, row);
            }
            else
            {
                int sectionIndex = IndexOfSection(sectionId);
                IReadOnlyList<VisualElement> cells = Window.Rail.NarrowCells;
                Assert.IsTrue(sectionIndex >= 0 && cells != null && sectionIndex < cells.Count && UxLayoutAuditor.IsShownOnScreen(cells[sectionIndex]),
                    "không có hàng rail nào bấm được để tới màn '" + sectionId + "' ở cỡ " + Size);
                yield return UxEventSender.Click(Window, cells[sectionIndex]);
            }
            yield return UxEventSender.WaitUntil(() => string.Equals(Window.ActiveSectionId, sectionId, StringComparison.Ordinal),
                "bấm rail không đưa hub tới màn '" + sectionId + "'");
            yield return UxEventSender.Settle(UxEventSender.SettleFrames * 2, UxEventSender.SettleMilliseconds * 2);
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
