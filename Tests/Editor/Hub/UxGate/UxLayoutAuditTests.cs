using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kiểm bố cục tự động của cổng W8-UX (§3.3): MỖI màn × 6 cỡ cửa sổ × vi/en. Một test cho một màn; bên trong mở hub một lần
    /// cho mỗi ngôn ngữ rồi đổi cỡ như người kéo mép cửa sổ, kiểm sau mỗi lần.
    /// <para>
    /// Vì sao gộp 12 lượt vào một test thay vì 12 test tham số: <c>[UnityTest]</c> có tham số chạy khác nhau giữa UTF 1.1.33
    /// (2022.3) và 1.8 (6000.6) — cổng phải cho CÙNG kết quả hai bản. Đổi lại, mỗi test gom mọi lỗi của 12 lượt vào MỘT câu
    /// assert kèm đường dẫn JSON, nên vẫn thấy hết một lần chứ không phải sửa từng cái rồi chạy lại.
    /// </para>
    /// <para>
    /// Test này ĐỎ trên code trước đợt W8 là có chủ đích — đó là 63 lỗi mà 900 test cũ không thấy. Chỗ cắt có chủ đích khai ở
    /// <see cref="UxLayoutAllowList"/> kèm lý do, không sửa ngưỡng ở đây.
    /// </para>
    /// <para>
    /// Test nào tự nhận "tách MỘT nguyên nhân" thì khai <see cref="UxLayoutScreen.WithScreenRulesOnly"/>: câu assert chỉ nói về
    /// luật của màn (và về nhánh cây được khai), phần còn lại vẫn ghi JSON. Không có nó thì ba test "tách nguyên nhân" đầu tiên
    /// đều đỏ với đúng 225 chỗ của cả cửa sổ, tức là không tách gì cả (R-04).
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    [Category(LiveOpsHubTestCategories.UxGate)]
    public sealed class UxLayoutAuditTests
    {
        /// <summary>Thân màn phải lấp gần hết chiều cao khung nội dung; 95% chừa chỗ cho margin/viền của màn.</summary>
        private const float StretchRatio = 0.95f;

        /// <summary>
        /// Thân trục so với KHUNG TRỤC: 60% vì [SD1] §3.2 dành phần dưới cho minimap + chú giải + readout. Đây là ngưỡng "có
        /// chỗ mà bấm", không phải ngưỡng 95% của kế hoạch — ngưỡng 95% chỉ áp cho track thước (UX-03).
        /// </summary>
        private const float TimelineMainRatio = 0.6f;

        /// <summary>
        /// Thân LÀN so với thân trục: 50%. Lỗi UX-01 ở 2022.3 là vùng làn cao 0 (không có thanh nào để bấm) — nửa chiều cao đã
        /// đủ tách "cao 0" khỏi "chia đôi với minimap"; nâng cao hơn là đo lại tỉ lệ thiết kế của minimap, việc của gói C.
        /// </summary>
        private const float TimelineBodyRatio = 0.5f;

        /// <summary>Dấu màu của chú giải phải đạt 3:1 so với nền (WCAG 2.1 cho thành phần đồ hoạ) — đúng ngưỡng kế hoạch UX-19.</summary>
        private const float LegendContrastRatio = 3f;

        /// <summary>
        /// Chú giải trục có BỐN dấu: cố định, lặp, đã khép, chồng nhau (<c>LiveOpsTimelineLegend</c> dựng đúng bốn mục). Khai
        /// TỪNG LOẠI vào luật — không khai một con số tổng — để phép đo không lặng lẽ thu còn ba: lượt đo thật thấy bảy element
        /// khớp selector chung vì dấu "cố định" lặp lại, nên ngưỡng tổng 4 vẫn XANH ngay cả khi dấu "chồng nhau" rơi khỏi phép
        /// đo. Mà "chồng nhau" chính là dấu khai màu CÓ ALPHA, dấu duy nhất từng lọt lưới trước bản vá hợp thành — một dấu vô
        /// hình vẫn cho cổng màu xanh (RC-06/2.4 mục 1; phát hiện A-02).
        /// </summary>
        private static readonly string[] LegendSampleVariantSelectors =
        {
            "." + LiveOpsHubClassNames.TimelineLegendSampleFixed,
            "." + LiveOpsHubClassNames.TimelineLegendSampleRecurring,
            "." + LiveOpsHubClassNames.TimelineLegendSampleEnded,
            "." + LiveOpsHubClassNames.TimelineLegendSampleOverlap,
        };

        /// <summary>Số nấc ⌘+lăn dựng hai mức thu phóng khác mặc định cho lượt kiểm nhãn thước (UX-17).</summary>
        private const int RulerZoomInNotches = -6;

        private const int RulerZoomOutNotches = 6;

        /// <summary>
        /// Tập mã phiếu hoãn ĐƯỢC DUYỆT của cổng W8-UX. USER chốt ngày 18/9/2026: chỉ "chỗ không dùng được" của các màn
        /// NGOÀI đợt (Xuất JSON, Tổng quan, Loại event, Kiểm lịch, khung) mới được đánh dấu hoãn — đúng năm phiếu này.
        /// Dãy số không liền vì giữ nguyên mã của bảng kế hoạch: W9-02 và W9-07…W9-15 là màn TRONG đợt, đã bị rút khỏi
        /// danh sách hoãn ngày 19/9/2026 và trả về cho một gói sửa bố cục riêng.
        /// </summary>
        private static readonly string[] ApprovedDeferralIds = { "W9-01", "W9-03", "W9-04", "W9-05", "W9-06" };

        /// <summary>
        /// Năm màn NGOÀI đợt đi kèm năm phiếu trên. Khoá cả MÀN chứ không chỉ mã phiếu: chặn đúng đường lách "thêm một
        /// mã phiếu mới cho một màn TRONG đợt", thứ mà kiểm từng mục không nhìn ra.
        /// </summary>
        private static readonly string[] ApprovedDeferralScreens =
        {
            "export", "overview", "shell-rail-status", "event-types", "validation",
        };

        private static readonly UxWindowSize Narrow700 = new UxWindowSize(700, 560);
        private static readonly UxWindowSize Medium820 = new UxWindowSize(820, 560);
        private static readonly UxWindowSize Wide1440 = new UxWindowSize(1440, 900);

        private UxHubWindowFixture _fixture;

        [TearDown]
        public void TearDown()
        {
            if (_fixture != null) _fixture.Dispose();
            _fixture = null;
            UxHubWindowFixture.CloseStrayWindows();
        }

        // ================================================================================================ màn đầy đủ

        [UnityTest]
        public IEnumerator Overview_LayoutIsUsable_AtEverySize()
        {
            // Hoãn sang W9: màn này đỏ vì BỐ CỤC, ngoài phạm vi hành trình của đợt W8. Mọi câu assert bên dưới
            // giữ nguyên — gỡ dòng của màn khỏi UxLayoutDeferralList là test chạy lại đầy đủ ngay lượt sau.
            UxLayoutDeferralList.IgnoreWhenDeferred("overview");

            yield return RunScreen(new UxLayoutScreen("overview", LiveOpsHubSections.Ids.Overview)
                .WithRequiredElements(WithShell(OverviewSection.BodyElementName, OverviewSection.MetricsElementName))
                .WithStretchRules(SectionStretchRules(OverviewSection.BodyElementName))
                .WithNoOverlapRules(StatusBarNoOverlapRules()));
        }

        [UnityTest]
        public IEnumerator EventTypes_LayoutIsUsable_AtEverySize()
        {
            // Hoãn sang W9: màn này đỏ vì BỐ CỤC, ngoài phạm vi hành trình của đợt W8. Mọi câu assert bên dưới
            // giữ nguyên — gỡ dòng của màn khỏi UxLayoutDeferralList là test chạy lại đầy đủ ngay lượt sau.
            UxLayoutDeferralList.IgnoreWhenDeferred("event-types");

            yield return RunScreen(new UxLayoutScreen("event-types", LiveOpsHubSections.Ids.EventTypes)
                .WithRequiredElements(WithShell(LiveOpsHubPaths.EventTypesElementNames.Body,
                    LiveOpsHubPaths.EventTypesElementNames.Content))
                .WithStretchRules(SectionStretchRules(LiveOpsHubPaths.EventTypesElementNames.Body))
                .WithNoOverlapRules(StatusBarNoOverlapRules()));
        }

        [UnityTest]
        public IEnumerator Calendar_LayoutIsUsable_AtEverySize_NoSelection()
        {
            yield return RunScreen(CalendarScreen("calendar-no-selection"));
        }

        [UnityTest]
        public IEnumerator Calendar_LayoutIsUsable_AtEverySize_WithSelection()
        {
            yield return RunScreen(CalendarScreen("calendar-selection").WithAfterOpen(SelectFirstBar));
        }

        [UnityTest]
        public IEnumerator Calendar_LayoutIsUsable_AtEverySize_MultiSelection()
        {
            yield return RunScreen(CalendarScreen("calendar-multi-selection").WithAfterOpen(SelectTwoBars));
        }

        [UnityTest]
        public IEnumerator Recurring_LayoutIsUsable_AtEverySize()
        {
            yield return RunScreen(new UxLayoutScreen("recurring-default", LiveOpsHubSections.Ids.RecurringRules)
                .WithRequiredElements(WithShell(RecurringRulesSection.BodyElementName, RecurringRulesSection.ListElementName,
                    RecurringRuleForm.SentenceElementName))
                .WithStretchRules(SectionStretchRules(RecurringRulesSection.BodyElementName))
                .WithNoOverlapRules(StatusBarNoOverlapRules()));
        }

        [UnityTest]
        public IEnumerator Validation_LayoutIsUsable_AtEverySize()
        {
            // Hoãn sang W9: màn này đỏ vì BỐ CỤC, ngoài phạm vi hành trình của đợt W8. Mọi câu assert bên dưới
            // giữ nguyên — gỡ dòng của màn khỏi UxLayoutDeferralList là test chạy lại đầy đủ ngay lượt sau.
            UxLayoutDeferralList.IgnoreWhenDeferred("validation");

            yield return RunScreen(new UxLayoutScreen("validation", LiveOpsHubSections.Ids.Validation)
                .WithRequiredElements(WithShell(LiveOpsHubPaths.ValidationElementNames.Body,
                    LiveOpsHubPaths.ValidationElementNames.Toolbar, LiveOpsHubPaths.ValidationElementNames.Content))
                .WithStretchRules(SectionStretchRules(LiveOpsHubPaths.ValidationElementNames.Body))
                .WithNoOverlapRules(StatusBarNoOverlapRules()));
        }

        [UnityTest]
        public IEnumerator Export_LayoutIsUsable_AtEverySize()
        {
            // Hoãn sang W9: màn này đỏ vì BỐ CỤC, ngoài phạm vi hành trình của đợt W8. Mọi câu assert bên dưới
            // giữ nguyên — gỡ dòng của màn khỏi UxLayoutDeferralList là test chạy lại đầy đủ ngay lượt sau.
            UxLayoutDeferralList.IgnoreWhenDeferred("export");

            yield return RunScreen(new UxLayoutScreen("export", LiveOpsHubSections.Ids.Export)
                .WithRequiredElements(WithShell(ExportSection.BodyElementName))
                .WithStretchRules(SectionStretchRules(ExportSection.BodyElementName))
                .WithNoOverlapRules(StatusBarNoOverlapRules()));
        }

        /// <summary>UX-20/UX-27: rail và status bar là lối đi chung — ở cỡ hẹp chúng vẫn phải bấm và đọc được, và không đè nhau.</summary>
        [UnityTest]
        public IEnumerator Shell_RailAndStatusBar_AreUsable_AtEverySize()
        {
            // Hoãn sang W9: màn này đỏ vì BỐ CỤC, ngoài phạm vi hành trình của đợt W8. Mọi câu assert bên dưới
            // giữ nguyên — gỡ dòng của màn khỏi UxLayoutDeferralList là test chạy lại đầy đủ ngay lượt sau.
            UxLayoutDeferralList.IgnoreWhenDeferred("shell-rail-status");

            yield return RunScreen(new UxLayoutScreen("shell-rail-status", LiveOpsHubSections.Ids.Overview)
                .WithRequiredElements(LiveOpsHubPaths.ShellElementNames.Rail, LiveOpsHubPaths.ShellElementNames.Content,
                    LiveOpsHubPaths.ShellElementNames.SectionBody, LiveOpsHubPaths.ShellElementNames.StatusBar,
                    LiveOpsHubPaths.ShellElementNames.StatusLeftText, LiveOpsHubPaths.ShellElementNames.StatusRight)
                .WithNoOverlapRules(StatusBarNoOverlapRules()));
        }

        // ================================================================================================ tách một nguyên nhân

        /// <summary>
        /// UX-01: chuỗi <c>calendar-root → calendar-main</c> phải lấp chiều cao <c>hub-section-body</c> ở MỌI cỡ. Test riêng vì
        /// đây là lỗi gốc kéo theo "820 màn trắng" và "vùng làn cao 0" — tách ra để cổng chỉ đúng một nguyên nhân.
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_BodyFillsSection()
        {
            yield return RunScreen(new UxLayoutScreen("calendar-body-fills", LiveOpsHubSections.Ids.Calendar)
                .WithRequiredElements(LiveOpsHubPaths.CalendarElementNames.Root, LiveOpsHubPaths.CalendarElementNames.Main)
                .WithStretchRules(CalendarStretchRules())
                .WithScreenRulesOnly());
        }

        /// <summary>
        /// UX-01: thân làn của trục phải có chiều cao khi CHƯA chọn đợt nào — ở 2022.3 vùng này cao 0 ở mọi cỡ, nên không có
        /// thanh nào để bấm và người dùng không vào được màn.
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_LaneViewportHasHeight_NoSelection()
        {
            yield return RunScreen(new UxLayoutScreen("calendar-lane-viewport", LiveOpsHubSections.Ids.Calendar)
                .WithRequiredElements("." + LiveOpsHubClassNames.TimelineBody, "." + LiveOpsHubClassNames.TimelineLaneRow)
                .WithStretchRules(new UxLayoutStretchRule("." + LiveOpsHubClassNames.TimelineBody,
                    "." + LiveOpsHubClassNames.TimelineMain, true, TimelineBodyRatio))
                .WithScreenRulesOnly());
        }

        /// <summary>
        /// UX-03: track của thước phải bám bề rộng cột timeline ở mọi cỡ — không kẹt ở bề rộng dự phòng 635px. Ngưỡng 95% đúng
        /// như kế hoạch: ở 1280/1440 track kẹt 635px trong cột ~1000px là 63%, ngưỡng 50% của bản đầu tiên vẫn cho nó XANH (R-05).
        /// </summary>
        [UnityTest]
        public IEnumerator Timeline_RulerTrackFillsColumn_AtEverySize()
        {
            yield return RunScreen(new UxLayoutScreen("timeline-ruler-track", LiveOpsHubSections.Ids.Calendar)
                .WithRequiredElements("." + LiveOpsHubClassNames.TimelineRulerTrack)
                .WithStretchRules(RulerTrackStretchRule())
                .WithScreenRulesOnly());
        }

        // ================================================================================================ lỗi theo nhánh cây

        /// <summary>
        /// UX-07: ở 700/820 (breakpoint --medium) chưa chọn đợt nào, cột trục không được có lớp nào phủ lên và không được cắt chữ
        /// của chính nó — đây là chỗ "820 màn trắng" và "thanh không bấm được" cùng hiện ra.
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_Medium_NoSelection_TrackNotCovered()
        {
            yield return RunScreen(CalendarScreen("calendar-medium-no-selection")
                .WithSizes(Narrow700, Medium820)
                .WithScreenRulesOnly(LiveOpsHubPaths.CalendarElementNames.TimelineColumn));
        }

        /// <summary>UX-07: mở drawer inspector ở 820 thì cả drawer lẫn nút đóng của nó phải dùng được, và drawer không cắt chữ.</summary>
        [UnityTest]
        public IEnumerator Calendar_Medium_DrawerOpen_AnchorsVisible()
        {
            yield return RunScreen(new UxLayoutScreen("calendar-medium-drawer", LiveOpsHubSections.Ids.Calendar)
                .WithSizes(Medium820)
                .WithAfterOpen(SelectFirstBar, true)
                .WithRequiredElements(LiveOpsHubPaths.CalendarElementNames.Inspector,
                    LiveOpsHubPaths.CalendarElementNames.InspectorBody,
                    LiveOpsHubPaths.CalendarDepthElementNames.InspectorDrawerClose)
                .WithScreenRulesOnly(LiveOpsHubPaths.CalendarElementNames.Inspector));
        }

        /// <summary>
        /// UX-08/UX-09: trong pane inspector không được cắt chữ, không được có nút nằm ngoài pane, ghi chú phải xuống dòng thay
        /// vì tràn. Giới hạn vào nhánh inspector để câu assert nói đúng một chỗ.
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_Inspector_NoCutText_ButtonsInsidePane_NotesWrap()
        {
            yield return RunScreen(CalendarScreen("calendar-inspector")
                .WithAfterOpen(SelectFirstBar, true)
                .WithScreenRulesOnly(LiveOpsHubPaths.CalendarElementNames.Inspector));
        }

        /// <summary>UX-11: toast không được đè lên chân trang (status bar) ở bất kỳ cỡ nào.</summary>
        [UnityTest]
        public IEnumerator Calendar_Toast_DoesNotOverlapFooter()
        {
            yield return RunScreen(new UxLayoutScreen("calendar-toast", LiveOpsHubSections.Ids.Calendar)
                .WithAfterOpen(DragBarToRaiseToast, true)
                .WithRequiredElements("." + LiveOpsHubClassNames.Toast, LiveOpsHubPaths.ShellElementNames.StatusBar)
                .WithNoOverlapRules(new UxLayoutNoOverlapRule("." + LiveOpsHubClassNames.Toast,
                    LiveOpsHubPaths.ShellElementNames.StatusBar))
                .WithScreenRulesOnly("." + LiveOpsHubClassNames.Toast));
        }

        /// <summary>UX-16: dòng thông tin của làn (tên loại, số đợt) không được cắt ở cỡ nào, tiếng nào.</summary>
        [UnityTest]
        public IEnumerator Calendar_LaneMeta_NotCut()
        {
            yield return RunScreen(CalendarScreen("calendar-lane-meta")
                .WithScreenRulesOnly("." + LiveOpsHubClassNames.TimelineLaneMetaRow));
        }

        /// <summary>UX-29: toolbar màn Lịch ở cỡ hẹp không được để control nào tràn ra ngoài hay đè lên nhau.</summary>
        [UnityTest]
        public IEnumerator Calendar_Toolbar_Narrow_NoOverflow()
        {
            yield return RunScreen(CalendarScreen("calendar-toolbar-narrow")
                .WithSizes(Narrow700, Medium820)
                .WithScreenRulesOnly(LiveOpsHubPaths.CalendarElementNames.Toolbar));
        }

        /// <summary>
        /// UX-17: nhãn thước không được cắt và không được chồng nhau ở MỌI mức thu phóng — lượt này kiểm ba mức: mặc định, một
        /// lượt ⌘+lăn phóng to và một lượt thu nhỏ (cùng đường mà người dùng đổi zoom).
        /// </summary>
        [UnityTest]
        public IEnumerator Timeline_RulerLabels_NotCutNotOverlapped_AtEveryZoom()
        {
            yield return RunScreen(RulerLabelScreen("timeline-ruler-labels-default", 0));
            yield return RunScreen(RulerLabelScreen("timeline-ruler-labels-zoom-in", RulerZoomInNotches));
            yield return RunScreen(RulerLabelScreen("timeline-ruler-labels-zoom-out", RulerZoomOutNotches));
        }

        /// <summary>UX-19: dấu màu của chú giải phải tương phản ≥ 3:1 với nền — đo trên màu ĐÃ resolve của skin đang chạy.</summary>
        [UnityTest]
        public IEnumerator Timeline_LegendSamplesContrast()
        {
            yield return RunScreen(new UxLayoutScreen("timeline-legend-contrast", LiveOpsHubSections.Ids.Calendar)
                .WithSizes(Wide1440)
                .WithRequiredElements("." + LiveOpsHubClassNames.TimelineLegend)
                .WithContrastRules(new UxLayoutContrastRule("." + LiveOpsHubClassNames.TimelineLegendSample, LegendContrastRatio,
                    LegendSampleVariantSelectors))
                .WithScreenRulesOnly());
        }

        // ================================================================================================ cửa sổ PHỤ

        /// <summary>
        /// UX-10: popover Thêm đợt là cửa sổ RIÊNG (<c>PopupWindow</c>) — bản đầu tiên của cổng chỉ duyệt cây của hub nên chưa
        /// một lần nào kiểm nó. Popover rộng cố định 320px nên chỉ cần hai cỡ cửa sổ chủ để bắt trường hợp nó bị kẹp ở cỡ hẹp.
        /// </summary>
        [UnityTest]
        public IEnumerator AddEventPopover_ButtonsAndTagNotCut()
        {
            yield return RunScreen(new UxLayoutScreen("add-event-popover", LiveOpsHubSections.Ids.Calendar)
                .WithSizes(Narrow700, Wide1440)
                .WithAfterOpen(OpenAddEventPopover, true)
                .WithWindowPicker(PopoverWindow)
                .WithRequiredElements(LiveOpsHubPaths.AddEventPopoverElementNames.Root,
                    LiveOpsHubPaths.AddEventPopoverElementNames.Buttons,
                    LiveOpsHubPaths.AddEventPopoverElementNames.TypeFilter));
        }

        /// <summary>
        /// UX-27: hộp xác nhận cũng là cửa sổ RIÊNG. Mở bằng <c>OpenForTest</c> (không <c>ShowModalUtility</c> — vòng modal chặn
        /// batchmode); đây là kiểm BỐ CỤC của hộp, còn đường mở hộp từ thao tác thật là việc của hành trình UX-06.
        /// </summary>
        [UnityTest]
        public IEnumerator ConfirmWindow_TextNotCut()
        {
            yield return RunScreen(new UxLayoutScreen("confirm-window", LiveOpsHubSections.Ids.Calendar)
                .WithSizes(Wide1440)
                .WithAfterOpen(OpenConfirmWindow, true)
                .WithWindowPicker(fixture => _confirmWindow));
        }

        // ================================================================================================ tự kiểm

        /// <summary>Danh sách miễn trừ phải luôn có lý do đọc được — chỗ duy nhất cổng im lặng không được thành bãi rác.</summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void AllowList_EveryEntry_NamesAKindAndAReason()
        {
            foreach (UxLayoutAllowEntry entry in UxLayoutAllowList.All)
            {
                Assert.IsNotEmpty(entry.Selector, "mục miễn trừ không có selector");
                Assert.IsNotEmpty(entry.Kind, "mục miễn trừ '" + entry.Selector + "' không nói nó tha loại phát hiện nào");
                Assert.Greater(entry.Reason.Length, 40,
                    "mục miễn trừ '" + entry.Selector + "' có lý do quá ngắn — phải nói RÕ vì sao cắt chỗ đó là thiết kế");
            }
        }

        /// <summary>
        /// Danh sách HOÃN phải luôn đọc ra được việc: mỗi mục đúng một mã phiếu W9 riêng, đúng một màn riêng, có số chỗ và
        /// có lý do. Không có gác này thì "thêm một dòng hoãn" là cách rẻ nhất để làm một màn đỏ biến mất mà không ai thấy.
        /// <para>
        /// Kiểm từng mục là chưa đủ: mục thứ sáu với một mã phiếu mới và một lý do dài quá 40 ký tự vẫn qua được, tức danh
        /// sách phình âm thầm. Vì vậy test còn khoá CẢ TẬP mã phiếu và CẢ TẬP màn vào đúng phần USER đã duyệt — muốn thêm
        /// một màn thì phải sửa <see cref="ApprovedDeferralIds"/>/<see cref="ApprovedDeferralScreens"/>, và chỗ sửa đó
        /// buộc người sửa đọc câu chốt của USER ngay trên đầu hai mảng.
        /// </para>
        /// </summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void DeferralList_EveryEntry_HasUniqueIdAndReason()
        {
            HashSet<string> seenDeferralIds = new HashSet<string>();
            HashSet<string> seenScreenIds = new HashSet<string>();
            List<string> actualDeferralIds = new List<string>();
            List<string> actualScreenIds = new List<string>();
            foreach (UxLayoutDeferralEntry entry in UxLayoutDeferralList.All)
            {
                StringAssert.IsMatch("^W9-[0-9][0-9]$", entry.DeferralId,
                    "mục hoãn của màn '" + entry.ScreenId + "' không mang mã phiếu dạng W9-NN nên không tra lại được");
                Assert.IsTrue(seenDeferralIds.Add(entry.DeferralId),
                    "mã phiếu hoãn '" + entry.DeferralId + "' bị khai hai lần — hai màn dùng chung một phiếu thì gỡ phiếu "
                    + "xong vẫn còn màn im lặng");
                Assert.IsTrue(seenScreenIds.Add(entry.ScreenId),
                    "màn '" + entry.ScreenId + "' bị hoãn hai lần — mục thứ hai sẽ không bao giờ được tra tới");
                Assert.Greater(entry.FindingCount, 0,
                    "mục hoãn '" + entry.DeferralId + "' khai 0 chỗ không dùng được — màn đã xanh thì bỏ hoãn, đừng giữ lại");
                Assert.Greater(entry.Reason.Length, 40,
                    "mục hoãn '" + entry.DeferralId + "' có lý do quá ngắn — phải nói RÕ vì sao đợt này không sửa");
                actualDeferralIds.Add(entry.DeferralId);
                actualScreenIds.Add(entry.ScreenId);
            }

            CollectionAssert.AreEquivalent(ApprovedDeferralIds, actualDeferralIds,
                "tập mã phiếu hoãn lệch phần USER đã duyệt 18/9/2026 (đúng 5 màn NGOÀI đợt) — thêm hay bớt một phiếu là "
                + "đổi phạm vi của cổng, phải có câu trả lời của USER rồi mới sửa ApprovedDeferralIds");
            CollectionAssert.AreEquivalent(ApprovedDeferralScreens, actualScreenIds,
                "tập MÀN được hoãn lệch phần USER đã duyệt 18/9/2026 — màn TRONG đợt (Lịch, trục, Luật lặp) đỏ thì phải "
                + "sửa, hoãn nó là tự cấp phép cho chính mình");
        }

        // ================================================================================================ chạy một màn

        private LiveOpsConfirmWindow _confirmWindow;

        private IEnumerator RunScreen(UxLayoutScreen screen)
        {
            List<string> problems = new List<string>();
            List<string> jsonPaths = new List<string>();
            List<string> clampedSizes = new List<string>();
            IReadOnlyList<UxWindowSize> sizes = screen.Sizes ?? UxHubWindowFixture.AllSizes;
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                _fixture = UxHubWindowFixture.Open(screen.SectionId, sizes[0], language);
                yield return _fixture.WaitForLayout();
                if (screen.AfterOpen != null && !screen.ReapplyAfterResize) yield return screen.AfterOpen(_fixture);
                foreach (UxWindowSize size in sizes)
                {
                    yield return _fixture.Resize(size);
                    if (screen.AfterOpen != null && screen.ReapplyAfterResize) yield return screen.AfterOpen(_fixture);
                    if (_fixture.ClampNote.Length > 0 && !clampedSizes.Contains(_fixture.ClampNote)) clampedSizes.Add(_fixture.ClampNote);
                    EditorWindow audited = screen.WindowPicker == null ? _fixture.Window : screen.WindowPicker(_fixture);
                    Assert.IsNotNull(audited, "màn '" + screen.Id + "' không có cửa sổ nào để kiểm ở cỡ " + size);
                    UxLayoutRules rules = screen.Rules();
                    UxLayoutAuditResult result = UxLayoutAuditor.Audit(audited, screen.Id, size, language, rules);
                    result.Clamped = _fixture.IsClamped;
                    result.ClampNote = _fixture.ClampNote;
                    jsonPaths.Add(UxLayoutAuditor.WriteJson(result));
                    List<VisualElement> subtreeRoots = UxLayoutAuditor.ResolveSubtreeRoots(audited, rules);
                    foreach (string problem in result.AssertProblems(rules, subtreeRoots))
                    {
                        problems.Add(screen.Id + " " + size + " " + LanguageTag(language) + " — " + problem);
                    }
                    foreach (KeyValuePair<string, int> truncated in result.TruncatedCounts)
                    {
                        problems.Add(screen.Id + " " + size + " " + LanguageTag(language) + " — loại '" + truncated.Key
                            + "' có " + truncated.Value + " dòng, JSON chỉ giữ " + UxLayoutAuditor.MaximumEntriesPerKind
                            + " (đọc JSON là đọc bản ĐÃ CẮT)");
                    }
                    CloseSecondaryWindows(screen);
                }
                _fixture.Dispose();
                _fixture = null;
                UxHubWindowFixture.CloseStrayWindows();
            }
            if (clampedSizes.Count > 0)
            {
                Debug.LogWarning("UxLayoutAuditTests '" + screen.Id + "': máy chạy không đủ chỗ cho "
                    + string.Join("; ", clampedSizes.ToArray()) + " — lượt kiểm vẫn chạy nhưng cỡ đó chưa được đo đúng.");
            }
            Assert.IsEmpty(problems, "Kiểm bố cục màn '" + screen.Id + "' thấy " + problems.Count + " chỗ người dùng không dùng được."
                + "\nJSON chẩn đoán: " + jsonPaths[0] + " (và " + (jsonPaths.Count - 1) + " file cùng thư mục)"
                + (clampedSizes.Count > 0 ? "\nCỡ chưa đo đúng trên máy này: " + string.Join("; ", clampedSizes.ToArray()) : string.Empty)
                + "\n - " + string.Join("\n - ", problems.ToArray()));
        }

        private void CloseSecondaryWindows(UxLayoutScreen screen)
        {
            if (screen.WindowPicker == null) return;
            if (_confirmWindow != null)
            {
                _confirmWindow.Close();
                _confirmWindow = null;
            }
            UxHubWindowFixture.CloseStrayWindows();
        }

        private static string LanguageTag(LiveOpsHubLanguageId language)
        {
            return language == LiveOpsHubLanguageId.Vietnamese ? "vi" : "en";
        }

        // ================================================================================================ bảng màn

        private static UxLayoutScreen CalendarScreen(string screenId)
        {
            return new UxLayoutScreen(screenId, LiveOpsHubSections.Ids.Calendar)
                .WithRequiredElements(WithShell(LiveOpsHubPaths.CalendarElementNames.Root,
                    LiveOpsHubPaths.CalendarElementNames.Toolbar, LiveOpsHubPaths.CalendarElementNames.Main,
                    LiveOpsHubPaths.CalendarElementNames.TimelineColumn, LiveOpsHubPaths.CalendarElementNames.Timeline))
                .WithStretchRules(CalendarStretchRules())
                .WithNoOverlapRules(StatusBarNoOverlapRules());
        }

        private static UxLayoutScreen RulerLabelScreen(string screenId, int zoomNotches)
        {
            UxLayoutScreen screen = new UxLayoutScreen(screenId, LiveOpsHubSections.Ids.Calendar)
                .WithRequiredElements("." + LiveOpsHubClassNames.TimelineRulerTrack, "." + LiveOpsHubClassNames.TimelineRulerLabel)
                .WithScreenRulesOnly("." + LiveOpsHubClassNames.TimelineRulerTrack);
            if (zoomNotches == 0) return screen;
            return screen.WithAfterOpen(fixture => ZoomTimeline(fixture, zoomNotches), true);
        }

        /// <summary>Phần tử của khung có ở MỌI màn — mất một cái là mất lối đi, không phải lỗi riêng của màn nào.</summary>
        private static string[] WithShell(params string[] screenElements)
        {
            List<string> required = new List<string>
            {
                LiveOpsHubPaths.ShellElementNames.Root,
                LiveOpsHubPaths.ShellElementNames.Main,
                LiveOpsHubPaths.ShellElementNames.Content,
                LiveOpsHubPaths.ShellElementNames.SectionHeader,
                LiveOpsHubPaths.ShellElementNames.SectionTitle,
                LiveOpsHubPaths.ShellElementNames.SectionBody,
                LiveOpsHubPaths.ShellElementNames.StatusBar,
            };
            required.AddRange(screenElements);
            return required.ToArray();
        }

        private static UxLayoutStretchRule[] SectionStretchRules(string sectionRootSelector)
        {
            return new[]
            {
                new UxLayoutStretchRule(sectionRootSelector, LiveOpsHubPaths.ShellElementNames.SectionBody, true, StretchRatio),
            };
        }

        /// <summary>Chân trang là dòng chữ cuối cùng người dùng đọc — không lớp nổi nào của màn được đè lên nó.</summary>
        private static UxLayoutNoOverlapRule[] StatusBarNoOverlapRules()
        {
            return new[]
            {
                new UxLayoutNoOverlapRule(LiveOpsHubPaths.ShellElementNames.SectionBody, LiveOpsHubPaths.ShellElementNames.StatusBar),
            };
        }

        /// <summary>
        /// Bề rộng header làn của timeline [SD1 §3.2]: cột timeline = header làn 168px + track. Track không bao giờ với tới
        /// 168px đó, nên luật giãn phải tính trên phần cột CÒN LẠI (G-FIX-UX-4 ở cổng đợt W8-UX).
        /// </summary>
        private const float TimelineLaneHeaderWidth = 168f;

        private static UxLayoutStretchRule RulerTrackStretchRule()
        {
            return new UxLayoutStretchRule("." + LiveOpsHubClassNames.TimelineRulerTrack,
                LiveOpsHubPaths.CalendarElementNames.TimelineColumn, false, StretchRatio, TimelineLaneHeaderWidth);
        }

        private static UxLayoutStretchRule[] CalendarStretchRules()
        {
            return new[]
            {
                new UxLayoutStretchRule(LiveOpsHubPaths.CalendarElementNames.Root, LiveOpsHubPaths.ShellElementNames.SectionBody, true, StretchRatio),
                new UxLayoutStretchRule(LiveOpsHubPaths.CalendarElementNames.Main, LiveOpsHubPaths.CalendarElementNames.Root, true, TimelineMainRatio),
                RulerTrackStretchRule(),
            };
        }

        // ================================================================================================ thao tác dựng trạng thái

        /// <summary>Chọn đợt bằng cú BẤM THẬT lên thanh — không gọi API chọn, vì chính đường bấm mới là chỗ UX-07 hỏng.</summary>
        private static IEnumerator SelectFirstBar(UxHubWindowFixture fixture)
        {
            LiveOpsTimelineBar bar = fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
            yield return UxEventSender.Click(fixture.Window, bar);
        }

        private static IEnumerator SelectTwoBars(UxHubWindowFixture fixture)
        {
            yield return SelectFirstBar(fixture);
            LiveOpsTimelineBar second = fixture.BarOf(LiveOpsDesignSample.HuntEarlyEntryKey);
            yield return UxEventSender.Click(fixture.Window, second, UxEventSender.ActionModifier);
        }

        /// <summary>Kéo một thanh để hub bật toast "đã dời" — trạng thái mà UX-11 nói tới (toast đè chân trang).</summary>
        /// <summary>
        /// Dựng cảnh cho màn calendar-toast: KÉO một thanh để màn bật toast "đã dời …".
        /// <para>
        /// Phải kéo đợt hunt-0914 (14/9 → 17/9, chưa bắt đầu ở mốc thiết kế 13/9 01:47), KHÔNG phải lava-quest-2026-09a:
        /// đợt 09a ĐÃ KHÉP lúc 13/9 00:00 và <c>LiveOpsTimelineDragController.BeginBar</c> TỪ CHỐI mọi thanh
        /// <c>IsEnded</c>, nên cú kéo cũ không hề xảy ra — không có lần ghi nào, không có toast nào, và màn bị báo là "có
        /// toast trong cây nhưng display=None" ở cả 12 cặp cỡ × ngôn ngữ. Luật chia thanh ở
        /// plan/w8-ux/UX2-BAR-ASSIGNMENT.md mục 3.1: ca nào phải GHI thì cấm đứng trên 09a.
        /// </para>
        /// <para>
        /// Vì sao không dùng lava-quest-2026-09b như các ca kéo của <c>UxCalendarJourneyTests</c>: ma trận của màn này chạy
        /// từ cỡ hẹp nhất 700x560, và ở cỡ đó trục KHÔNG vẽ thanh 09b (đo được: fixture báo "trục không vẽ thanh
        /// 'entry-lava-quest-2026-09b'"). hunt-0914 nằm sát mốc "bây giờ" nên được vẽ ở mọi cỡ của ma trận.
        /// </para>
        /// </summary>
        private static IEnumerator DragBarToRaiseToast(UxHubWindowFixture fixture)
        {
            LiveOpsTimelineBar bar = fixture.BarOf(LiveOpsDesignSample.HuntEarlyEntryKey);
            Vector2 from = bar.worldBound.center;
            yield return UxEventSender.Drag(fixture.Window, from, from + new Vector2(60f, 0f),
                UxEventSender.MinimumDragSteps, EventModifiers.None);
        }

        private static IEnumerator ZoomTimeline(UxHubWindowFixture fixture, int notches)
        {
            LiveOpsTimelineElement timeline = fixture.Calendar.Timeline;
            yield return UxEventSender.Wheel(fixture.Window, timeline.worldBound.center, notches, UxEventSender.ActionModifier);
        }

        private static IEnumerator OpenAddEventPopover(UxHubWindowFixture fixture)
        {
            Button add = fixture.AddEventButton();
            Assert.IsNotNull(add, "màn Lịch không có nút Thêm đợt trong phần hành động của header (UX-10)");
            yield return UxEventSender.Click(fixture.Window, add);
            yield return UxEventSender.WaitUntil(() => LiveOpsPopoverContent.Current != null && LiveOpsPopoverContent.Current.IsOpen,
                "bấm Thêm đợt không mở popover nào (UX-10)");
        }

        private static EditorWindow PopoverWindow(UxHubWindowFixture fixture)
        {
            return LiveOpsPopoverContent.Current == null ? null : LiveOpsPopoverContent.Current.editorWindow;
        }

        private IEnumerator OpenConfirmWindow(UxHubWindowFixture fixture)
        {
            if (_confirmWindow != null) _confirmWindow.Close();
            // Dựng đúng hộp mà đường kéo rút ngắn đợt đang chạy sinh ra (CalendarTimelinePresenter): cùng tiêu đề, cùng
            // thân cảnh báo, cùng cặp nhãn nút — để lượt kiểm nói về hộp THẬT chứ không về một hộp rỗng.
            _confirmWindow = LiveOpsConfirmWindow.OpenForTest(new LiveOpsConfirmRequest.Builder()
                .WithTitle(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarShortenConfirmTitleFormat,
                    LiveOpsDesignSample.LavaQuestEarlyEntryKey))
                .WithBody(LiveOpsHubStrings.CalendarUnknownPlayerCountSentence)
                .WithHelpBoxWarning()
                .WithButtons(LiveOpsHubStrings.CalendarShortenDestructiveLabel, LiveOpsHubStrings.KitConfirmKeepLabel)
                .Build());
            yield return UxEventSender.Settle(UxEventSender.SettleFrames * 3, UxEventSender.SettleMilliseconds * 4);
        }
    }
}
