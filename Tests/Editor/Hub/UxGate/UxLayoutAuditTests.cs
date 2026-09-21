using System;
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
        /// (W10-10) Trần khung chờ thẻ hover tắt hẳn trước lượt quét. <see cref="UxHubWindowFixture.HideHoverCards"/> gỡ class
        /// ngay trong lượt gọi nên bình thường vòng chờ thoát ở khung đầu; trần này chỉ để một thẻ "cứng đầu" thành ca ĐỎ có
        /// tên màn, chứ không thành một vòng chờ vô hạn.
        /// </summary>
        private const int HoverCardCloseFrames = 30;

        /// <summary>
        /// Tập mã phiếu hoãn ĐƯỢC DUYỆT, theo đúng câu chốt gần nhất của USER. Lịch sử: cổng W8-UX mở năm phiếu cho các
        /// màn NGOÀI đợt (USER chốt 18/9/2026); đợt W9 rút dần và đóng nốt phiếu cuối (W9-01) ngày 20/9/2026, tập trở về
        /// RỖNG; đợt W11 mở lại với mười hai phiếu W11-NN (USER chốt 21/9/2026) — xem chú thích ngay trên mảng.
        /// </summary>
        /// <remarks>
        /// Luật của chỗ này không đổi qua ba lần: tập chỉ dài thêm khi có CÂU TRẢ LỜI của USER, và mỗi mã phải tra lại
        /// được ra một màn cụ thể với một con số cụ thể. Sửa hai mảng này mà không có câu chốt là tự cấp phép.
        /// </remarks>
        // (W11, 21/9/2026) USER mở lại cửa hoãn với ĐÚNG 12 mã dưới đây. Nguyên văn quyết định: "Sửa lỗi mắt thấy rồi phát
        // hành; nợ bố cục dữ liệu xấu nhất để đợt W11. Ghi nợ thành id + số liệu rõ trong CHANGELOG và sổ kế hoạch, KHÔNG
        // giấu." 12 mã này là đúng 12 ca đỏ mà cổng W10 đã khai công khai, không phải một tập rộng hơn. Thêm mã thứ 13 là
        // mở rộng phạm vi USER đã duyệt — phải có câu trả lời của USER trước.
        private static readonly string[] ApprovedDeferralIds =
        {
            "W11-01", "W11-02", "W11-03", "W11-04", "W11-05", "W11-06",
            "W11-07", "W11-08", "W11-09", "W11-10", "W11-11", "W11-12",
        };

        /// <summary>
        /// Mười hai màn đi kèm mười hai phiếu trên. Khoá cả MÀN chứ không chỉ mã phiếu: chặn đúng đường lách "thêm một
        /// mã phiếu mới cho một màn chưa ai duyệt", thứ mà kiểm từng mục không nhìn ra.
        /// </summary>
        private static readonly string[] ApprovedDeferralScreens =
        {
            "calendar-worst-data", "event-types-worst-data", "recurring-worst-data", "overview-worst-data",
            "export-worst-data", "calendar-multi-selection-worst-data", "calendar-medium-drawer-worst-data",
            "add-event-popover-worst-data", "calendar-inspector-fielderror", "calendar-empty", "export-no-baseline",
            "overview-empty",
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
            yield return RunScreen(new UxLayoutScreen("overview", LiveOpsHubSections.Ids.Overview)
                .WithRequiredElements(WithShell(OverviewSection.BodyElementName, OverviewSection.MetricsElementName))
                .WithStretchRules(SectionStretchRules(OverviewSection.BodyElementName))
                .WithNoOverlapRules(StatusBarNoOverlapRules()));
        }

        [UnityTest]
        public IEnumerator EventTypes_LayoutIsUsable_AtEverySize()
        {
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

        /// <summary>
        /// UX-20/UX-27: rail và status bar là lối đi chung — ở cỡ hẹp chúng vẫn phải bấm và đọc được, và không đè nhau.
        /// <para>
        /// W9-26 — test này KHÔNG soi khung cho tới lượt W9. Nó mở section Tổng quan rồi để auditor duyệt CẢ cây cửa sổ, còn
        /// <see cref="UxLayoutScreen.WithRequiredElements"/> chỉ đòi rail/status CÓ MẶT chứ không thu hẹp phạm vi đo. Hệ quả đo
        /// được trên <c>6d73130</c>: tập chỗ lỗi của test này trùng 100% với tập của <see cref="Overview_LayoutIsUsable_AtEverySize"/>
        /// (118 = 118, so từng dòng cỡ × ngôn ngữ × loại × mô tả, 0 dòng riêng mỗi bên) và KHÔNG một chỗ nào thuộc rail hay
        /// status bar. Tức là kể cả khi nó xanh, nó xanh vì màn Tổng quan sạch — UX-20 chưa bao giờ có tiêu chí nghiệm thu thật.
        /// </para>
        /// <para>
        /// Bản vá: <c>WithScreenRulesOnly</c> giới hạn phát hiện chung vào ĐÚNG hai nhánh rail và status bar, và luật không-đè
        /// nay nói về chính hai nhánh đó (rail ↔ status bar) thay vì về thân section. Mở ở section Tổng quan vẫn giữ nguyên:
        /// khung phải dùng được KHI đang có một màn thật bên trong, và màn nào cũng được — nội dung của nó không còn lọt vào
        /// câu assert nữa.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Shell_RailAndStatusBar_AreUsable_AtEverySize()
        {
            yield return RunScreen(ShellRailStatusScreen("shell-rail-status"));
        }

        // ================================================================================================ màn DỮ LIỆU XẤU NHẤT

        // Vì sao cả khối này tồn tại (vá LỖ HỔNG MA TRẬN, 20/9/2026): 24 màn của bản W9 đều đứng trên
        // LiveOpsDesignSample.Document ở trạng thái ĐẸP — không đợt nào bị bỏ, không giờ nào hỏng, không loại nào chưa khai,
        // không danh sách nào rỗng, không chuỗi nào dài. Ba lỗi W9-29 / W9-30 / W9-31 lọt lưới đúng vì thế: cả ba chỉ vẽ ra
        // khi dữ liệu XẤU, mà không màn nào của ma trận từng dựng dữ liệu xấu. Hai trong ba lỗi ấy còn có sẵn dữ liệu trong
        // mẫu (đợt BỊ BỎ, đợt có giờ KHÔNG ĐỌC ĐƯỢC) — chỉ là chưa màn nào CHỌN tới nó.
        // Luật từ đây: mỗi màn phải có ít nhất một biến thể "ngày tồi tệ nhất" (chuỗi dài nhất, giờ không đọc được, id dài,
        // số lớn, danh sách rỗng) trong ma trận BỐ CỤC, không chỉ trong test hành vi.
        // Và luật ấy KHÔNG nằm trong khối chú thích này nữa (soát W10 R-04): UxLayoutScreenCatalog kê từng màn cùng cách nó
        // chạm dữ liệu xấu, RunScreen đối chiếu mọi màn đang chạy với bảng kê, còn UxLayoutScreenCatalogTests khoá "mỗi
        // section phải có ít nhất một màn dữ liệu xấu nhất" và khoá TẬP màn còn nợ. Chú thích không chặn được màn thứ 39.

        /// <summary>
        /// (W9-29) Chọn đợt BỊ BỎ — biến thể DÀI NHẤT của dòng gợi ý đáy trục, vì nó cộng thêm câu phát hiện
        /// ("bị bỏ vì … — F8 xem lỗi") vào trước cụm phím. Đây là câu bị ellipsis nuốt mất "⌘⌫ xoá" ở 700 · 820 · 950.
        /// <para>
        /// Giới hạn phát hiện vào đúng nhánh dòng gợi ý: câu đỏ phải nói về MỘT chỗ — dòng gợi ý — chứ không gom cả cửa sổ.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_TimelineHint_NotCut_WithDroppedEntrySelected()
        {
            yield return RunScreen(new UxLayoutScreen("calendar-selection-dropped", LiveOpsHubSections.Ids.Calendar)
                .WithAfterOpen(SelectDroppedBar, true)
                .WithRequiredElements("." + LiveOpsHubClassNames.TimelineHint, "." + LiveOpsHubClassNames.TimelineHintText)
                .WithScreenRulesOnly("." + LiveOpsHubClassNames.TimelineHint));
        }

        /// <summary>
        /// (W9-30) Chọn đợt có giờ kết thúc KHÔNG ĐỌC ĐƯỢC: chỉ ở trạng thái này hàng ô ngày giờ UTC mới dựng biểu tượng lỗi,
        /// và đó là phần tử bị đẩy khỏi hàng rồi mép cửa sổ cắt đôi ở 1280x760.
        /// <para>
        /// Dựng trạng thái bằng <c>SetSelectedBarKey</c> chứ không bằng cú bấm thanh: đợt có giờ hỏng KHÔNG đặt được lên trục
        /// (trục treo nó vào chip "Không đặt được"), nên không có thanh nào để bấm. Đường bấm chip là việc của test HÀNH VI
        /// UX-09; ở đây thứ đang kiểm là BỐ CỤC của pane sau khi đã chọn.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_Inspector_FieldError_NotCutAtWindowEdge()
        {
            yield return RunScreen(new UxLayoutScreen("calendar-inspector-fielderror", LiveOpsHubSections.Ids.Calendar)
                .WithAfterOpen(SelectUnreadableEndEntry, true)
                .WithRequiredElements(LiveOpsHubPaths.CalendarElementNames.Inspector,
                    LiveOpsHubPaths.CalendarElementNames.InspectorBody)
                .WithScreenRulesOnly(LiveOpsHubPaths.CalendarElementNames.Inspector));
        }

        /// <summary>Màn Lịch trên tài liệu xấu nhất: id đợt dài nhất, tên loại dài nhất, một loại chưa khai, một giờ hỏng.</summary>
        [UnityTest]
        public IEnumerator Calendar_LayoutIsUsable_WithWorstCaseData()
        {
            yield return RunScreen(CalendarScreen("calendar-worst-data")
                .WithServices(WorstCaseServices)
                .WithAfterOpen(SelectLongestBar, true));
        }

        /// <summary>Màn Lịch khi lịch RỖNG: không đợt, không luật — câu "chưa có gì" cũng phải dùng được ở mọi cỡ.</summary>
        [UnityTest]
        public IEnumerator Calendar_LayoutIsUsable_WhenEmpty()
        {
            yield return RunScreen(EmptyScreen("calendar-empty", LiveOpsHubSections.Ids.Calendar));
        }

        /// <summary>Màn Luật lặp trên dữ liệu xấu nhất: tiền tố id dài nhất, chu kỳ 8760 giờ, một lần chạy 999 giờ.</summary>
        [UnityTest]
        public IEnumerator Recurring_LayoutIsUsable_WithWorstCaseData()
        {
            yield return RunScreen(new UxLayoutScreen("recurring-worst-data", LiveOpsHubSections.Ids.RecurringRules)
                .WithServices(WorstCaseServices)
                .WithRequiredElements(WithShell(RecurringRulesSection.BodyElementName, RecurringRulesSection.ListElementName,
                    RecurringRuleForm.SentenceElementName))
                .WithStretchRules(SectionStretchRules(RecurringRulesSection.BodyElementName))
                .WithNoOverlapRules(StatusBarNoOverlapRules()));
        }

        /// <summary>Màn Luật lặp khi danh sách luật RỖNG.</summary>
        [UnityTest]
        public IEnumerator Recurring_LayoutIsUsable_WhenEmpty()
        {
            yield return RunScreen(EmptyScreen("recurring-empty", LiveOpsHubSections.Ids.RecurringRules));
        }

        /// <summary>Màn Tổng quan trên dữ liệu xấu nhất: id dài, việc cần làm nhiều, số đếm lớn.</summary>
        [UnityTest]
        public IEnumerator Overview_LayoutIsUsable_WithWorstCaseData()
        {
            yield return RunScreen(new UxLayoutScreen("overview-worst-data", LiveOpsHubSections.Ids.Overview)
                .WithServices(WorstCaseServices)
                .WithRequiredElements(WithShell(OverviewSection.BodyElementName))
                .WithStretchRules(SectionStretchRules(OverviewSection.BodyElementName))
                .WithNoOverlapRules(StatusBarNoOverlapRules()));
        }

        /// <summary>Màn Tổng quan khi không có đợt nào sắp diễn ra và không có việc cần làm.</summary>
        [UnityTest]
        public IEnumerator Overview_LayoutIsUsable_WhenEmpty()
        {
            yield return RunScreen(EmptyScreen("overview-empty", LiveOpsHubSections.Ids.Overview));
        }

        /// <summary>
        /// (W9-31) Màn Loại event khi có loại CHƯA KHAI. Bảng chỉ vẽ dấu trạng thái ở đúng trạng thái này
        /// (<c>EventTypeTable.BindStateCell</c> đặt <c>visible = !row.IsDeclared</c>), nên mẫu đẹp để cột ấy trống ở mọi cỡ.
        /// </summary>
        [UnityTest]
        public IEnumerator EventTypes_LayoutIsUsable_WithUndeclaredType()
        {
            yield return RunScreen(new UxLayoutScreen("event-types-worst-data", LiveOpsHubSections.Ids.EventTypes)
                .WithServices(WorstCaseServices)
                .WithRequiredElements(WithShell(LiveOpsHubPaths.EventTypesElementNames.Body,
                    LiveOpsHubPaths.EventTypesElementNames.Content))
                .WithStretchRules(SectionStretchRules(LiveOpsHubPaths.EventTypesElementNames.Body))
                .WithNoOverlapRules(StatusBarNoOverlapRules()));
        }

        /// <summary>Màn Loại event khi bảng RỖNG.</summary>
        [UnityTest]
        public IEnumerator EventTypes_LayoutIsUsable_WhenEmpty()
        {
            yield return RunScreen(EmptyScreen("event-types-empty", LiveOpsHubSections.Ids.EventTypes));
        }

        /// <summary>Màn Kiểm lịch khi có phát hiện với câu dài nhất (loại chưa khai + giờ không đọc được + id dài).</summary>
        [UnityTest]
        public IEnumerator Validation_LayoutIsUsable_WithWorstCaseFindings()
        {
            yield return RunScreen(new UxLayoutScreen("validation-worst-data", LiveOpsHubSections.Ids.Validation)
                .WithServices(WorstCaseServices)
                .WithRequiredElements(WithShell(LiveOpsHubPaths.ValidationElementNames.Body,
                    LiveOpsHubPaths.ValidationElementNames.Toolbar, LiveOpsHubPaths.ValidationElementNames.Content))
                .WithStretchRules(SectionStretchRules(LiveOpsHubPaths.ValidationElementNames.Body))
                .WithNoOverlapRules(StatusBarNoOverlapRules()));
        }

        /// <summary>Màn Kiểm lịch khi lịch rỗng — 0 phát hiện, "mọi thứ ổn".</summary>
        [UnityTest]
        public IEnumerator Validation_LayoutIsUsable_WhenEmpty()
        {
            yield return RunScreen(EmptyScreen("validation-empty", LiveOpsHubSections.Ids.Validation));
        }

        /// <summary>Màn Xuất JSON trên dữ liệu xấu nhất: số byte bảy chữ số, tên người đăng dài, ghi chú dài, sha 64 ký tự.</summary>
        [UnityTest]
        public IEnumerator Export_LayoutIsUsable_WithWorstCaseData()
        {
            yield return RunScreen(new UxLayoutScreen("export-worst-data", LiveOpsHubSections.Ids.Export)
                .WithServices(WorstCaseServices)
                .WithRequiredElements(WithShell(ExportSection.BodyElementName))
                .WithStretchRules(SectionStretchRules(ExportSection.BodyElementName))
                .WithNoOverlapRules(StatusBarNoOverlapRules()));
        }

        /// <summary>Màn Xuất JSON khi CHƯA có bản đã đăng — không có bản so, khác biệt rỗng.</summary>
        [UnityTest]
        public IEnumerator Export_LayoutIsUsable_WithoutPublishedStamp()
        {
            yield return RunScreen(new UxLayoutScreen("export-no-baseline", LiveOpsHubSections.Ids.Export)
                .WithServices(WithoutPublishedStampServices)
                .WithRequiredElements(WithShell(ExportSection.BodyElementName))
                .WithStretchRules(SectionStretchRules(ExportSection.BodyElementName))
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
            yield return RunScreen(LaneViewportScreen("calendar-lane-viewport"));
        }

        /// <summary>
        /// UX-03: track của thước phải bám bề rộng cột timeline ở mọi cỡ — không kẹt ở bề rộng dự phòng 635px. Ngưỡng 95% đúng
        /// như kế hoạch: ở 1280/1440 track kẹt 635px trong cột ~1000px là 63%, ngưỡng 50% của bản đầu tiên vẫn cho nó XANH (R-05).
        /// </summary>
        [UnityTest]
        public IEnumerator Timeline_RulerTrackFillsColumn_AtEverySize()
        {
            yield return RunScreen(RulerTrackScreen("timeline-ruler-track"));
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
            yield return RunScreen(MediumDrawerScreen("calendar-medium-drawer").WithAfterOpen(SelectFirstBar, true));
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

        /// <summary>
        /// UX-11: toast không được đè lên chân trang (status bar) ở bất kỳ cỡ nào.
        /// <para>
        /// (W9-28, 20/9/2026) Bản cũ chạy SÁU cỡ thiết kế và bỏ 950x700 của W9-20, vì nó khai <c>ReapplyAfterResize</c>: cú
        /// kéo chạy lại sau mỗi lần đổi cỡ TRÊN CÙNG MỘT phiên, nên đợt bị dời thêm 60px mỗi lượt và trôi dần khỏi khoảng
        /// ngày trục đang vẽ — bảy lượt thì đợt rơi khỏi trục ("trục không vẽ thanh 'entry-hunt-0914'") và màn đỏ vì một lý
        /// do KHÔNG liên quan tới UX-11. Đó cũng đúng là mẫu đỏ chập chờn duy nhất từng thấy của màn này (cổng W9, lượt
        /// TOÀN BỘ).
        /// </para>
        /// <para>
        /// Nay là MỘT nguyên nhân đã chứng minh cộng MỘT hàng rào phòng xa — khai đúng như thế, không nói quá (soát W10
        /// R-08). Nguyên nhân đã chứng minh: cú kéo CỘNG DỒN trên cùng một phiên, chữa bằng
        /// <see cref="UxLayoutScreen.WithRebuildPerSize"/> (mỗi cỡ kéo đúng MỘT lần trên tài liệu nguyên vẹn) — đây là thứ
        /// khớp với mẫu đỏ duy nhất từng thấy. Hàng rào phòng xa: <see cref="UxLayoutScreen.WithReadyCondition"/> chờ TOAST
        /// THẬT theo điều kiện + hạn giờ, vì toast tự tắt sau <c>LiveOpsToast.VisibleSeconds</c> giây đồng hồ THẬT. Hàng rào
        /// ấy CHƯA một lần nào kích hoạt trong các lượt đã chạy (không log nào có câu "dựng trạng thái hai lần vẫn không đạt
        /// điều kiện đo"), nên nó là phòng xa cho máy bận, chưa phải một gốc đã đo được. Với nguyên nhân thứ nhất đã hết,
        /// màn trở lại ĐỦ BẢY cỡ của ma trận (gồm 950x700): cổng rộng hơn bản W9 chứ không hẹp đi.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_Toast_DoesNotOverlapFooter()
        {
            yield return RunScreen(ToastScreen("calendar-toast").WithAfterOpen(DragBarToRaiseToast, true));
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
            yield return RunScreen(LegendContrastScreen("timeline-legend-contrast"));
        }

        // ================================================================================================ cửa sổ PHỤ

        /// <summary>
        /// UX-10: popover Thêm đợt là cửa sổ RIÊNG (<c>PopupWindow</c>) — bản đầu tiên của cổng chỉ duyệt cây của hub nên chưa
        /// một lần nào kiểm nó. Popover rộng cố định 320px nên chỉ cần hai cỡ cửa sổ chủ để bắt trường hợp nó bị kẹp ở cỡ hẹp.
        /// </summary>
        [UnityTest]
        public IEnumerator AddEventPopover_ButtonsAndTagNotCut()
        {
            yield return RunScreen(AddEventPopoverScreen("add-event-popover"));
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

        // ============================================================== W10-07: biến thể DỮ LIỆU XẤU NHẤT của 12 màn còn nợ

        // Vì sao khối này tồn tại: đợt W10 đã vá lỗ hổng ma trận cho SÁU section nhưng để lại 12 màn "tách một nguyên nhân"
        // và hai cửa sổ phụ đứng trên mẫu ĐẸP — chúng bị khoá trong UxLayoutScreenCatalog.ScreensWithoutWorstCaseVariant như
        // một món nợ công khai (phiếu W10-07). Mười hai màn dưới đây trả món nợ ấy: mỗi màn giữ NGUYÊN luật đo của bản đẹp
        // (cùng RequiredElements, cùng StretchRules, cùng ScreenRulesOnly) và chỉ đổi DỮ LIỆU, nên chênh lệch đỏ giữa hai
        // bản đọc được thẳng thành "chỗ này chỉ vỡ khi dữ liệu xấu".

        /// <summary>
        /// (W10-07) Khung (rail + chân trang) khi tài liệu XẤU NHẤT: tên tài liệu dài, số phát hiện lớn, nên câu trạng thái
        /// bên trái chân trang dài nhất mà hub sinh ra được.
        /// </summary>
        [UnityTest]
        public IEnumerator Shell_RailAndStatusBar_AreUsable_WithWorstCaseData()
        {
            yield return RunScreen(ShellRailStatusScreen("shell-rail-status-worst-data")
                .WithServices(WorstCaseServices));
        }

        /// <summary>
        /// (W10-07) Chọn NHIỀU thanh trên tài liệu xấu nhất. Inspector ở trạng thái (c) in dòng GỘP của cả tập, nên tập có
        /// một id dài nhất là câu dài nhất mà pane ấy vẽ.
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_MultiSelection_LayoutIsUsable_WithWorstCaseData()
        {
            yield return RunScreen(CalendarScreen("calendar-multi-selection-worst-data")
                .WithServices(WorstCaseServices)
                .WithAfterOpen(SelectTwoWorstCaseBars));
        }

        /// <summary>
        /// (W10-07) Chuỗi <c>calendar-root → calendar-main</c> khi lịch RỖNG. Trạng thái rỗng dựng một template KHÁC, và
        /// đúng chỗ ấy chuỗi giãn dễ đứt nhất — tài liệu xấu nhất không đổi kết quả vì luật đang đo là luật GIÃN.
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_BodyFillsSection_WhenEmpty()
        {
            yield return RunScreen(new UxLayoutScreen("calendar-body-fills-empty", LiveOpsHubSections.Ids.Calendar)
                .WithServices(EmptyServices)
                .WithRequiredElements(LiveOpsHubPaths.CalendarElementNames.Root, LiveOpsHubPaths.CalendarElementNames.Main)
                .WithStretchRules(CalendarStretchRules())
                .WithScreenRulesOnly());
        }

        /// <summary>
        /// (W10-07) Vùng làn khi số làn LỚN NHẤT. Tài liệu xấu nhất có cả loại CHƯA KHAI, nên nó sinh thêm một làn mà mẫu
        /// đẹp không bao giờ có — và số làn là thứ quyết định chiều cao vùng làn.
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_LaneViewportHasHeight_WithWorstCaseData()
        {
            yield return RunScreen(LaneViewportScreen("calendar-lane-viewport-worst-data")
                .WithServices(WorstCaseServices));
        }

        /// <summary>(W10-07) Track thước trên tài liệu xấu nhất — bề rộng track bám cột timeline, và cột đổi theo số làn.</summary>
        [UnityTest]
        public IEnumerator Timeline_RulerTrackFillsColumn_WithWorstCaseData()
        {
            yield return RunScreen(RulerTrackScreen("timeline-ruler-track-worst-data")
                .WithServices(WorstCaseServices));
        }

        /// <summary>
        /// (W10-07) Ngăn kéo inspector ở 820 khi đợt đang chọn có id DÀI NHẤT. Ngăn kéo hẹp hơn pane thường, nên đúng chỗ ấy
        /// chữ dài mới chạm mép.
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_Medium_DrawerOpen_WithWorstCaseData()
        {
            yield return RunScreen(MediumDrawerScreen("calendar-medium-drawer-worst-data")
                .WithServices(WorstCaseServices)
                .WithAfterOpen(SelectLongestBar, true));
        }

        /// <summary>
        /// (W10-07) Toast trên tài liệu xấu nhất. Kéo đợt <c>ShortEntryKey</c> (14/9 → 15/9, CHƯA bắt đầu ở mốc 13/9) chứ
        /// không kéo đợt id dài: đợt id dài đang CHẠY, và luật chia thanh cấm ca phải GHI đứng trên một thanh đang chạy —
        /// hub sẽ hỏi xác nhận thay vì bật toast.
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_Toast_DoesNotOverlapFooter_WithWorstCaseData()
        {
            yield return RunScreen(ToastScreen("calendar-toast-worst-data")
                .WithServices(WorstCaseServices)
                .WithAfterOpen(DragWorstCaseBarToRaiseToast, true));
        }

        /// <summary>
        /// (W10-07) Nhãn thước ở hai mức thu phóng trên tài liệu xấu nhất — mức phóng to sinh nhãn DÀY nhất, mức thu nhỏ
        /// sinh nhãn THÁNG (chuỗi dài nhất của thước).
        /// </summary>
        [UnityTest]
        public IEnumerator Timeline_RulerLabels_NotCutNotOverlapped_WithWorstCaseData()
        {
            yield return RunScreen(RulerLabelScreen("timeline-ruler-labels-zoom-in-worst-data", RulerZoomInNotches)
                .WithServices(WorstCaseServices));
            yield return RunScreen(RulerLabelScreen("timeline-ruler-labels-zoom-out-worst-data", RulerZoomOutNotches)
                .WithServices(WorstCaseServices));
        }

        /// <summary>
        /// (W10-07) Chú giải khi có loại CHƯA KHAI: dấu màu của loại chưa khai là dấu DUY NHẤT khai màu có alpha, đúng loại
        /// dấu từng lọt lưới tương phản trước bản vá màu đã hợp thành.
        /// </summary>
        [UnityTest]
        public IEnumerator Timeline_LegendSamplesContrast_WithWorstCaseData()
        {
            yield return RunScreen(LegendContrastScreen("timeline-legend-contrast-worst-data")
                .WithServices(WorstCaseServices));
        }

        /// <summary>
        /// (W10-07) Popover Thêm đợt khi dropdown tên loại mang TÊN DÀI NHẤT. Popover rộng cố định 320px nên tên loại dài là
        /// chỗ duy nhất nó có thể vỡ.
        /// </summary>
        [UnityTest]
        public IEnumerator AddEventPopover_ButtonsAndTagNotCut_WithWorstCaseData()
        {
            yield return RunScreen(AddEventPopoverScreen("add-event-popover-worst-data")
                .WithServices(WorstCaseServices));
        }

        /// <summary>
        /// (W10-07) Hộp xác nhận khi câu hỏi IN ID ĐỢT dài nhất. Hộp rộng cố định, nên id dài là chỗ duy nhất chữ của nó
        /// chạm mép.
        /// </summary>
        [UnityTest]
        public IEnumerator ConfirmWindow_TextNotCut_WithWorstCaseData()
        {
            yield return RunScreen(new UxLayoutScreen("confirm-window-worst-data", LiveOpsHubSections.Ids.Calendar)
                .WithSizes(Wide1440)
                .WithServices(WorstCaseServices)
                .WithAfterOpen(OpenWorstCaseConfirmWindow, true)
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
                // Nhận cả W9-NN (đợt W8-UX/W9) lẫn W11-NN (đợt W11) — hai đợt duy nhất USER đã mở cửa hoãn. Không nhận
                // dạng chung "W<số>-NN": mã của một đợt chưa ai duyệt mà lọt vào đây thì hai mảng duyệt ở trên là chỗ
                // chặn cuối, và một regex rộng làm chỗ chặn ấy trông như đã có phép.
                StringAssert.IsMatch("^W(9|11)-[0-9][0-9]$", entry.DeferralId,
                    "mục hoãn của màn '" + entry.ScreenId + "' không mang mã phiếu dạng W9-NN hay W11-NN nên không tra "
                    + "lại được");
                Assert.IsTrue(seenDeferralIds.Add(entry.DeferralId),
                    "mã phiếu hoãn '" + entry.DeferralId + "' bị khai hai lần — hai màn dùng chung một phiếu thì gỡ phiếu "
                    + "xong vẫn còn màn im lặng");
                Assert.IsTrue(seenScreenIds.Add(entry.ScreenId),
                    "màn '" + entry.ScreenId + "' bị hoãn hai lần — mục thứ hai sẽ không bao giờ được tra tới");
                Assert.Greater(entry.FindingCount, 0,
                    "mục hoãn '" + entry.DeferralId + "' khai 0 chỗ không dùng được — màn đã xanh thì bỏ hoãn, đừng giữ lại");
                Assert.Greater(entry.Reason.Length, 40,
                    "mục hoãn '" + entry.DeferralId + "' có lý do quá ngắn — phải nói RÕ vì sao đợt này không sửa");
                AssertDeferredSizesAreReal(entry);
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

        /// <summary>
        /// Gác phần HOÃN THEO CỠ của W9-18: nhãn cỡ phải là cỡ có thật trong ma trận, không được khai trùng, và không được
        /// khai đủ CẢ ma trận.
        /// <para>
        /// Ba câu này chặn ba đường lách khác nhau. Nhãn gõ sai ("1024x760") không khớp cỡ nào nên mục hoãn im lặng KHÔNG
        /// hoãn gì — người viết tưởng đã hẹn sửa, cổng thì đỏ mãi. Khai trùng làm số cỡ trong câu Ignored sai. Và khai đủ
        /// cả ma trận là hoãn cả màn bằng đường vòng: nó qua được mọi câu assert ở trên mà vẫn giấu được một màn đỏ, đúng
        /// loại "xanh giả" mà danh sách hoãn sinh ra để diệt — hoãn cả màn thì phải khai RỖNG, để câu Ignored nói thẳng
        /// "ở MỌI cỡ".
        /// </para>
        /// </summary>
        private static void AssertDeferredSizesAreReal(UxLayoutDeferralEntry entry)
        {
            if (entry.DeferredSizes.Count == 0) return;
            HashSet<string> matrixSizes = new HashSet<string>();
            foreach (UxWindowSize size in UxHubWindowFixture.AllLayoutSizes) matrixSizes.Add(size.ToString());
            HashSet<string> seen = new HashSet<string>();
            foreach (string label in entry.DeferredSizes)
            {
                Assert.IsTrue(matrixSizes.Contains(label),
                    "mục hoãn '" + entry.DeferralId + "' khai cỡ '" + label + "' không có trong ma trận kiểm — nhãn gõ sai "
                    + "thì mục hoãn KHÔNG hoãn cỡ nào cả mà vẫn trông như đã hẹn sửa");
                Assert.IsTrue(seen.Add(label),
                    "mục hoãn '" + entry.DeferralId + "' khai cỡ '" + label + "' hai lần — số cỡ trong câu Ignored sẽ sai");
            }

            Assert.Less(entry.DeferredSizes.Count, matrixSizes.Count,
                "mục hoãn '" + entry.DeferralId + "' khai đủ CẢ ma trận cỡ — hoãn cả màn thì khai danh sách RỖNG để câu "
                + "Ignored nói thẳng 'ở MỌI cỡ', đừng liệt kê từng cỡ rồi để nó trông như hoãn một phần");
        }

        // ================================================================================================ chạy một màn

        private LiveOpsConfirmWindow _confirmWindow;

        private IEnumerator RunScreen(UxLayoutScreen screen)
        {
            AssertScreenIsRegistered(screen);
            List<string> problems = new List<string>();
            List<string> jsonPaths = new List<string>();
            List<string> clampedSizes = new List<string>();
            Dictionary<string, int> problemsPerSize = new Dictionary<string, int>();
            UxLayoutDeferralEntry deferral = UxLayoutDeferralList.Find(screen.Id);
            IReadOnlyList<UxWindowSize> declaredSizes = screen.Sizes ?? UxHubWindowFixture.AllLayoutSizes;
            List<UxWindowSize> sizes = new List<UxWindowSize>();
            List<string> deferredSizeLabels = new List<string>();
            foreach (UxWindowSize declared in declaredSizes)
            {
                if (deferral != null && deferral.IsDeferredAt(declared.ToString())) deferredSizeLabels.Add(declared.ToString());
                else sizes.Add(declared);
            }

            // Mọi cỡ đều hoãn ⇒ Ignored kèm mã phiếu, như bản cũ. Còn cỡ nào chưa hoãn thì test CHẠY THẬT ở đúng những cỡ
            // đó — đây là cái mà mục hoãn theo MÀN của đợt W8 làm mất (W9-18).
            if (sizes.Count == 0) Assert.Ignore(deferral.IgnoreMessage);
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                if (!screen.RebuildPerSize)
                {
                    _fixture = OpenFixture(screen, sizes[0], language);
                    yield return _fixture.WaitForLayout();
                    if (screen.AfterOpen != null && !screen.ReapplyAfterResize) yield return ApplyAfterOpen(screen, problems);
                }

                foreach (UxWindowSize size in sizes)
                {
                    if (screen.RebuildPerSize)
                    {
                        // Mỗi cỡ một phiên MỚI: thao tác ghi của cỡ trước không được cộng dồn sang cỡ sau (W9-28).
                        if (_fixture != null) _fixture.Dispose();
                        UxHubWindowFixture.CloseStrayWindows();
                        _fixture = OpenFixture(screen, size, language);
                        yield return _fixture.WaitForLayout();
                    }
                    else
                    {
                        yield return _fixture.Resize(size);
                    }

                    if (screen.AfterOpen != null && (screen.ReapplyAfterResize || screen.RebuildPerSize))
                    {
                        yield return ApplyAfterOpen(screen, problems);
                    }
                    if (_fixture.ClampNote.Length > 0 && !clampedSizes.Contains(_fixture.ClampNote)) clampedSizes.Add(_fixture.ClampNote);
                    EditorWindow audited = screen.WindowPicker == null ? _fixture.Window : screen.WindowPicker(_fixture);
                    Assert.IsNotNull(audited, "màn '" + screen.Id + "' không có cửa sổ nào để kiểm ở cỡ " + size);
                    yield return CloseHoverCardsBeforeAudit(screen, size, audited);
                    UxLayoutRules rules = screen.Rules();
                    UxLayoutAuditResult result = UxLayoutAuditor.Audit(audited, screen.Id, size, language, rules);
                    result.Clamped = _fixture.IsClamped;
                    result.ClampNote = _fixture.ClampNote;
                    jsonPaths.Add(UxLayoutAuditor.WriteJson(result));
                    List<VisualElement> subtreeRoots = UxLayoutAuditor.ResolveSubtreeRoots(audited, rules);
                    foreach (string problem in result.AssertProblems(rules, subtreeRoots))
                    {
                        problems.Add(screen.Id + " " + size + " " + LanguageTag(language) + " — " + problem);
                        string sizeKey = size + " " + LanguageTag(language);
                        problemsPerSize.TryGetValue(sizeKey, out int already);
                        problemsPerSize[sizeKey] = already + 1;
                    }
                    foreach (KeyValuePair<string, int> truncated in result.TruncatedCounts)
                    {
                        problems.Add(screen.Id + " " + size + " " + LanguageTag(language) + " — loại '" + truncated.Key
                            + "' có " + truncated.Value + " dòng, JSON chỉ giữ " + UxLayoutAuditor.MaximumEntriesPerKind
                            + " (đọc JSON là đọc bản ĐÃ CẮT)");
                    }
                    CloseSecondaryWindows(screen);
                }
                if (_fixture != null) _fixture.Dispose();
                _fixture = null;
                UxHubWindowFixture.CloseStrayWindows();
            }
            // GIỚI HẠN ĐÃ BIẾT, không phải lỗi của gói này (soát W10 R-10): trên máy đang chạy, hai cỡ rộng nhất bị hệ điều
            // hành kẹp chiều cao (1440x900 → 1440x895, 1920x1040 → 1920x895), nên biến thể dữ liệu xấu nhất ở cỡ RỘNG NHẤT
            // chưa thật sự được đo. Đây là CẢNH BÁO chứ không phải lỗi vì làm đỏ ở đây là làm đỏ theo máy chạy, không theo mã
            // — nhưng nó cũng có nghĩa là một phần ma trận đang khai 7 cỡ mà chỉ đo đúng 5. Muốn đóng thì cần một máy/chế độ
            // đo đủ chiều cao, hoặc một cách đo khác cho hai cỡ đó; phiếu ghi ở báo cáo đợt W10.
            if (clampedSizes.Count > 0)
            {
                Debug.LogWarning("UxLayoutAuditTests '" + screen.Id + "': máy chạy không đủ chỗ cho "
                    + string.Join("; ", clampedSizes.ToArray()) + " — lượt kiểm vẫn chạy nhưng cỡ đó chưa được đo đúng.");
            }
            if (deferredSizeLabels.Count > 0)
            {
                Debug.LogWarning("UxLayoutAuditTests '" + screen.Id + "': " + deferral.DeferralId + " còn hoãn "
                    + deferredSizeLabels.Count + " cỡ (" + string.Join(", ", deferredSizeLabels.ToArray())
                    + "); lượt này kiểm ĐẦY ĐỦ " + sizes.Count + " cỡ còn lại.");
            }
            Assert.IsEmpty(problems, "Kiểm bố cục màn '" + screen.Id + "' thấy " + problems.Count + " chỗ người dùng không dùng được."
                + "\nTheo cỡ: " + SizeTally(problemsPerSize)
                + "\nJSON chẩn đoán: " + jsonPaths[0] + " (và " + (jsonPaths.Count - 1) + " file cùng thư mục)"
                + (deferredSizeLabels.Count > 0 ? "\nCỡ đang HOÃN (" + deferral.DeferralId + "), không tính ở đây: "
                    + string.Join(", ", deferredSizeLabels.ToArray()) : string.Empty)
                + (clampedSizes.Count > 0 ? "\nCỡ chưa đo đúng trên máy này: " + string.Join("; ", clampedSizes.ToArray()) : string.Empty)
                + "\n - " + string.Join("\n - ", problems.ToArray()));
        }

        /// <summary>
        /// Mọi màn đang chạy phải có một dòng trong <see cref="UxLayoutScreenCatalog"/>, và dòng ấy phải nói ĐÚNG về màn.
        /// <para>
        /// Vì sao ở đây chứ không chỉ trong ca tự kiểm bảng kê (soát W10 R-04): bảng kê một mình chỉ chứng minh những dòng
        /// ĐÃ KHAI là hợp lệ — nó không biết có màn nào chạy mà không khai. Câu này đóng nốt chiều còn lại: thêm một màn vào
        /// ma trận mà quên trả lời "màn này nhìn thấy dữ liệu xấu ở đâu" là ĐỎ ngay lượt đầu, chứ không im lặng thành một lỗ
        /// hổng mới của ma trận.
        /// </para>
        /// <para>
        /// Đối chiếu HAI CHIỀU với <c>WithServices</c>: khai "đứng trên services xấu nhất" mà màn không dựng services (hoặc
        /// ngược lại) cũng đỏ, kẻo lời khai trôi khỏi mã và bảng kê thành một tờ giấy nói về một ma trận khác.
        /// </para>
        /// </summary>
        private static void AssertScreenIsRegistered(UxLayoutScreen screen)
        {
            UxLayoutScreenRegistration registration = UxLayoutScreenCatalog.Find(screen.Id);
            Assert.IsNotNull(registration,
                "màn '" + screen.Id + "' chưa có dòng nào trong UxLayoutScreenCatalog — mỗi màn của ma trận phải trả lời "
                + "được 'nó nhìn thấy dữ liệu XẤU NHẤT ở đâu' (tự dựng services, chọn tới phần xấu của mẫu, được màn khác "
                + "phủ hộ, hay còn nợ một phiếu). Đây là luật vá lỗ hổng ma trận của đợt W10, không phải thủ tục giấy tờ: "
                + "ba lỗi W9-29/30/31 lọt lưới đúng vì màn nào cũng đứng trên tài liệu đẹp");
            Assert.AreEqual(registration.SectionId, screen.SectionId,
                "màn '" + screen.Id + "' chạy ở section '" + screen.SectionId + "' nhưng bảng kê khai '"
                + registration.SectionId + "' — một trong hai chỗ nói sai về màn này");
            Assert.AreEqual(registration.Coverage == UxWorstCaseCoverage.Services, screen.Services != null,
                "màn '" + screen.Id + "' và bảng kê không đồng ý về services: bảng kê khai '" + registration.Coverage
                + "', mã " + (screen.Services != null ? "CÓ" : "KHÔNG") + " dựng services riêng. Lời khai phủ dữ liệu xấu "
                + "chỉ có giá trị khi nó nói về đúng thứ mã đang làm");
        }

        /// <summary>Mở hub với services của màn; màn không khai services thì dùng mẫu thiết kế như trước.</summary>
        private static UxHubWindowFixture OpenFixture(UxLayoutScreen screen, UxWindowSize size, LiveOpsHubLanguageId language)
        {
            LiveOpsHubServices services = screen.Services == null ? null : screen.Services();
            return UxHubWindowFixture.Open(screen.SectionId, size, language, services);
        }

        /// <summary>
        /// Dựng trạng thái của màn rồi CHỜ THEO ĐIỀU KIỆN nếu màn khai điều kiện, và dựng lại ĐÚNG MỘT LẦN khi hết hạn giờ
        /// (W9-28).
        /// <para>
        /// Vì sao không chờ suông thêm vài khung: trạng thái mà điều kiện nói tới có thứ TỰ TẮT theo đồng hồ thật (toast sống
        /// 6 giây), nên "chờ thêm" làm hỏng đúng thứ đang chờ. Chờ theo điều kiện thì máy nhanh đi tiếp ngay, máy bận vẫn
        /// đúng; hết hạn thì dựng lại một lần rồi mới bỏ cuộc — và lúc bỏ cuộc phải GHI THÀNH LỖI, vì một lượt đo trên trạng
        /// thái không có thật là một lượt xanh giả.
        /// </para>
        /// </summary>
        private IEnumerator ApplyAfterOpen(UxLayoutScreen screen, List<string> problems)
        {
            yield return screen.AfterOpen(_fixture);
            if (screen.ReadyCondition == null) yield break;
            UxHubWindowFixture fixtureForCondition = _fixture;
            Func<bool> condition = () => screen.ReadyCondition(fixtureForCondition);
            yield return UxEventSender.WaitUntilOrTimeout(condition, screen.ReadyTimeoutMilliseconds);
            if (condition()) yield break;
            yield return screen.AfterOpen(_fixture);
            yield return UxEventSender.WaitUntilOrTimeout(condition, screen.ReadyTimeoutMilliseconds);
            if (condition()) yield break;
            problems.Add(screen.Id + " " + _fixture.Size + " " + LanguageTag(_fixture.Language)
                + " — dựng trạng thái hai lần vẫn không đạt điều kiện đo trong " + screen.ReadyTimeoutMilliseconds
                + "ms; lượt đo sau đây sẽ nói về một trạng thái KHÔNG có thật");
        }

        /// <summary>
        /// Một dòng "cỡ × ngôn ngữ = số chỗ" đứng TRƯỚC danh sách chi tiết (W9-18). Với 617 chỗ chia trên 7 cỡ × 2 ngôn ngữ,
        /// người đọc cần biết ngay "đỏ ở đâu" trước khi đọc dòng thứ nhất của 617 dòng; bản cũ chỉ in tổng rồi đổ thẳng chi tiết.
        /// </summary>
        private static string SizeTally(Dictionary<string, int> problemsPerSize)
        {
            if (problemsPerSize.Count == 0) return "(không cỡ nào đỏ)";
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, int> pair in problemsPerSize)
            {
                parts.Add(pair.Key + "=" + pair.Value.ToString(CultureInfo.InvariantCulture));
            }

            parts.Sort(StringComparer.Ordinal);
            return string.Join("  ", parts.ToArray());
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

        // ------------------------------------------------------------------ khuôn màn dùng chung cho bản ĐẸP và bản XẤU NHẤT

        // Mỗi khuôn dưới đây sinh ra ĐÚNG một bộ luật đo. Bản dữ liệu đẹp và bản dữ liệu xấu nhất của cùng một màn phải
        // dùng chung khuôn, nếu không thì chênh lệch đỏ giữa hai bản có thể là do LUẬT khác nhau chứ không do dữ liệu —
        // và cả phép so ấy mất nghĩa (W10-07).

        private static UxLayoutScreen ShellRailStatusScreen(string screenId)
        {
            return new UxLayoutScreen(screenId, LiveOpsHubSections.Ids.Overview)
                .WithRequiredElements(LiveOpsHubPaths.ShellElementNames.Rail, LiveOpsHubPaths.ShellElementNames.Content,
                    LiveOpsHubPaths.ShellElementNames.SectionBody, LiveOpsHubPaths.ShellElementNames.StatusBar,
                    LiveOpsHubPaths.ShellElementNames.StatusLeftText, LiveOpsHubPaths.ShellElementNames.StatusRight)
                .WithNoOverlapRules(new UxLayoutNoOverlapRule(LiveOpsHubPaths.ShellElementNames.Rail,
                    LiveOpsHubPaths.ShellElementNames.StatusBar))
                .WithScreenRulesOnly(LiveOpsHubPaths.ShellElementNames.Rail, LiveOpsHubPaths.ShellElementNames.StatusBar);
        }

        private static UxLayoutScreen LaneViewportScreen(string screenId)
        {
            return new UxLayoutScreen(screenId, LiveOpsHubSections.Ids.Calendar)
                .WithRequiredElements("." + LiveOpsHubClassNames.TimelineBody, "." + LiveOpsHubClassNames.TimelineLaneRow)
                .WithStretchRules(new UxLayoutStretchRule("." + LiveOpsHubClassNames.TimelineBody,
                    "." + LiveOpsHubClassNames.TimelineMain, true, TimelineBodyRatio))
                .WithScreenRulesOnly();
        }

        private static UxLayoutScreen RulerTrackScreen(string screenId)
        {
            return new UxLayoutScreen(screenId, LiveOpsHubSections.Ids.Calendar)
                .WithRequiredElements("." + LiveOpsHubClassNames.TimelineRulerTrack)
                .WithStretchRules(RulerTrackStretchRule())
                .WithScreenRulesOnly();
        }

        private static UxLayoutScreen MediumDrawerScreen(string screenId)
        {
            return new UxLayoutScreen(screenId, LiveOpsHubSections.Ids.Calendar)
                .WithSizes(Medium820)
                .WithRequiredElements(LiveOpsHubPaths.CalendarElementNames.Inspector,
                    LiveOpsHubPaths.CalendarElementNames.InspectorBody,
                    LiveOpsHubPaths.CalendarDepthElementNames.InspectorDrawerClose)
                .WithScreenRulesOnly(LiveOpsHubPaths.CalendarElementNames.Inspector);
        }

        private static UxLayoutScreen ToastScreen(string screenId)
        {
            return new UxLayoutScreen(screenId, LiveOpsHubSections.Ids.Calendar)
                .WithRebuildPerSize()
                .WithReadyCondition(IsToastShown, ToastReadyTimeoutMilliseconds)
                .WithRequiredElements("." + LiveOpsHubClassNames.Toast, LiveOpsHubPaths.ShellElementNames.StatusBar)
                .WithNoOverlapRules(new UxLayoutNoOverlapRule("." + LiveOpsHubClassNames.Toast,
                    LiveOpsHubPaths.ShellElementNames.StatusBar))
                .WithScreenRulesOnly("." + LiveOpsHubClassNames.Toast);
        }

        private static UxLayoutScreen LegendContrastScreen(string screenId)
        {
            return new UxLayoutScreen(screenId, LiveOpsHubSections.Ids.Calendar)
                .WithSizes(Wide1440)
                .WithRequiredElements("." + LiveOpsHubClassNames.TimelineLegend)
                .WithContrastRules(new UxLayoutContrastRule("." + LiveOpsHubClassNames.TimelineLegendSample, LegendContrastRatio,
                    LegendSampleVariantSelectors))
                .WithScreenRulesOnly();
        }

        private static UxLayoutScreen AddEventPopoverScreen(string screenId)
        {
            return new UxLayoutScreen(screenId, LiveOpsHubSections.Ids.Calendar)
                .WithSizes(Narrow700, Wide1440)
                .WithAfterOpen(OpenAddEventPopover, true)
                .WithWindowPicker(PopoverWindow)
                .WithRequiredElements(LiveOpsHubPaths.AddEventPopoverElementNames.Root,
                    LiveOpsHubPaths.AddEventPopoverElementNames.Buttons,
                    LiveOpsHubPaths.AddEventPopoverElementNames.TypeFilter);
        }

        private static UxLayoutScreen CalendarScreen(string screenId)
        {
            return new UxLayoutScreen(screenId, LiveOpsHubSections.Ids.Calendar)
                .WithRequiredElements(WithShell(LiveOpsHubPaths.CalendarElementNames.Root,
                    LiveOpsHubPaths.CalendarElementNames.Toolbar, LiveOpsHubPaths.CalendarElementNames.Main,
                    LiveOpsHubPaths.CalendarElementNames.TimelineColumn, LiveOpsHubPaths.CalendarElementNames.Timeline))
                .WithStretchRules(CalendarStretchRules())
                .WithNoOverlapRules(StatusBarNoOverlapRules());
        }

        /// <summary>
        /// Màn "danh sách RỖNG" của một section: lịch không loại, không luật, không đợt.
        /// <para>
        /// Chỉ đòi phần tử của KHUNG chứ không đòi thân riêng của section: trạng thái rỗng dựng một template KHÁC (màn Tổng
        /// quan đổi sang <c>overview-empty-body</c>), nên khai tên element của trạng thái đầy vào đây là đỏ vì lời khai sai
        /// chứ không vì bố cục. Phát hiện CHUNG vẫn quét cả cửa sổ, tức vẫn bắt được khung trống cao 0, chữ cắt và con tràn —
        /// đúng thứ trạng thái rỗng hay làm hỏng.
        /// </para>
        /// </summary>
        private static UxLayoutScreen EmptyScreen(string screenId, string sectionId)
        {
            return new UxLayoutScreen(screenId, sectionId)
                .WithServices(EmptyServices)
                .WithRequiredElements(WithShell())
                .WithNoOverlapRules(StatusBarNoOverlapRules());
        }

        /// <summary>Services trên tài liệu XẤU NHẤT, đã chạy xong một lượt kiểm để màn Kiểm lịch có phát hiện thật để vẽ.</summary>
        private static LiveOpsHubServices WorstCaseServices()
        {
            return BuildServices(LiveOpsWorstCaseSample.Document);
        }

        /// <summary>Services trên lịch RỖNG.</summary>
        private static LiveOpsHubServices EmptyServices()
        {
            return BuildServices(LiveOpsWorstCaseSample.EmptyDocument);
        }

        /// <summary>Services trên mẫu thiết kế nhưng CHƯA có bản đã đăng — màn Xuất JSON không có bản nào để so.</summary>
        private static LiveOpsHubServices WithoutPublishedStampServices()
        {
            return BuildServices(LiveOpsDesignSample.DocumentWithoutPublishedStamp());
        }

        private static LiveOpsHubServices BuildServices(LiveEventCalendarDocument document)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document)));
            services.Session.RunCheckToCompletion();
            return services;
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

        /// <summary>
        /// (W10-10) Tắt thẻ hover NGAY TRƯỚC lượt quét, rồi chờ THEO ĐIỀU KIỆN (không theo số khung cố định) cho tới khi cây
        /// sạch thẻ, và khẳng định là sạch.
        /// <para>
        /// Vì sao phải đứng ở đây, trong <see cref="RunScreen"/>, chứ không chỉ trong vài hàm dựng trạng thái có bấm thanh:
        /// thẻ hover có thể còn dính lại từ ca CHẠY TRƯỚC (cùng cửa sổ Editor, cùng nhịp update), nên màn nào cũng có thể
        /// nhận nhầm. Đặt ở đường chung thì mọi màn đếm cùng một thứ, bất kể thứ tự chạy — đó là điều phiếu W10-10 đòi.
        /// </para>
        /// <para>
        /// Khẳng định cuối cùng là chốt chống xanh giả: nếu một ngày nào đó có thẻ hover không tắt được bằng
        /// <see cref="UxHubWindowFixture.HideHoverCards"/> (màn mới quên khai chủ thẻ), ca đỏ ngay với tên màn và cỡ, thay
        /// vì con số của màn lại trôi âm thầm như trước.
        /// </para>
        /// </summary>
        private IEnumerator CloseHoverCardsBeforeAudit(UxLayoutScreen screen, UxWindowSize size, EditorWindow audited)
        {
            _fixture.HideHoverCards();
            for (int frame = 0; frame < HoverCardCloseFrames; frame++)
            {
                if (!UxHubWindowFixture.HasVisibleHoverCard(_fixture.Root)
                    && !UxHubWindowFixture.HasVisibleHoverCard(audited.rootVisualElement))
                {
                    break;
                }
                yield return null;
            }

            bool stillVisible = UxHubWindowFixture.HasVisibleHoverCard(_fixture.Root)
                || UxHubWindowFixture.HasVisibleHoverCard(audited.rootVisualElement);
            Assert.IsFalse(stillVisible,
                "màn '" + screen.Id + "' ở cỡ " + size + " còn thẻ hover đang hiện sau " + HoverCardCloseFrames
                + " khung kể từ lúc gọi HideHoverCards — số chỗ của màn sẽ gồm cả dòng của thẻ hover và phụ thuộc thứ tự "
                + "chạy (phiếu W10-10). Màn nào mới có thẻ hover thì khai chủ thẻ vào UxHubWindowFixture.HideHoverCards");
        }

        // ================================================================================================ thao tác dựng trạng thái

        /// <summary>Chọn đợt bằng cú BẤM THẬT lên thanh — không gọi API chọn, vì chính đường bấm mới là chỗ UX-07 hỏng.</summary>
        private static IEnumerator SelectFirstBar(UxHubWindowFixture fixture)
        {
            LiveOpsTimelineBar bar = fixture.BarOf(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
            yield return UxEventSender.Click(fixture.Window, bar);
            yield return MovePointerOffTimeline(fixture);
        }

        /// <summary>
        /// Đưa con trỏ RA KHỎI trục sau một cú bấm chọn. Đo được ở lượt W10 khi máy chạy ba Unity song song: con trỏ ảo nằm
        /// lại trên thanh vừa bấm, hẹn giờ hover card (500 ms) chạy hết trước lúc lượt kiểm đọc cây, và HOVER CARD lọt vào
        /// số đo — màn <c>calendar-worst-data</c> nhảy từ 178 lên 190 chỗ, sáu dòng của thẻ hover chỉ có ở bản tiếng Anh,
        /// tức con số của màn phụ thuộc MÁY CHẠY NHANH HAY CHẬM.
        /// <para>
        /// Vì sao đây là chữa đúng chỗ chứ không phải giấu phát hiện: chủ ngữ của những màn này là TRẠNG THÁI CHỌN, không
        /// phải thẻ hover. Thẻ hover là một màn khác và cần luật riêng của nó (chỗ nợ đã ghi ở báo cáo gói) — để nó lọt vào
        /// đây thì cả hai màn đều đo sai: màn chọn đếm thêm thứ không phải của mình, còn thẻ hover thì không ai đo nó một
        /// cách có chủ đích.
        /// </para>
        /// </summary>
        private static IEnumerator MovePointerOffTimeline(UxHubWindowFixture fixture)
        {
            VisualElement header = fixture.Root.Q(LiveOpsHubPaths.ShellElementNames.SectionHeader);
            if (header == null) yield break;
            UxEventSender.MouseMove(fixture.Window, header.worldBound.center, EventModifiers.None);
            yield return UxEventSender.Settle(UxEventSender.SettleFrames, UxEventSender.SettleMilliseconds);
        }

        /// <summary>Chọn đợt BỊ BỎ (hunt-0916-bonus) — biến thể dài nhất của dòng gợi ý đáy trục (W9-29).</summary>
        private static IEnumerator SelectDroppedBar(UxHubWindowFixture fixture)
        {
            LiveOpsTimelineBar bar = fixture.BarOf(LiveOpsDesignSample.HuntBonusEntryKey);
            yield return UxEventSender.Click(fixture.Window, bar);
            yield return MovePointerOffTimeline(fixture);
        }

        /// <summary>Chọn đợt có id DÀI NHẤT của tài liệu xấu nhất.</summary>
        private static IEnumerator SelectLongestBar(UxHubWindowFixture fixture)
        {
            LiveOpsTimelineBar bar = fixture.BarOf(LiveOpsWorstCaseSample.LongEntryKey);
            yield return UxEventSender.Click(fixture.Window, bar);
            yield return MovePointerOffTimeline(fixture);
        }

        /// <summary>
        /// Chọn đợt có giờ kết thúc KHÔNG ĐỌC ĐƯỢC (W9-30). Không bấm thanh vì đợt ấy không đặt được lên trục — xem chú
        /// thích của <see cref="Calendar_Inspector_FieldError_NotCutAtWindowEdge"/>.
        /// </summary>
        private static IEnumerator SelectUnreadableEndEntry(UxHubWindowFixture fixture)
        {
            fixture.Calendar.Presenter.SetSelectedBarKey(LiveOpsDesignSample.LavaQuestLateEntryKey);
            yield return fixture.WaitForLayout();
        }

        /// <summary>
        /// Chọn HAI thanh của tài liệu xấu nhất: thanh id dài nhất làm mốc, cộng một thanh ngắn. Tập trộn dài + ngắn là tập
        /// bắt được cả câu gộp dài nhất lẫn chỗ hai dòng khác chiều dài xếp cạnh nhau.
        /// </summary>
        private static IEnumerator SelectTwoWorstCaseBars(UxHubWindowFixture fixture)
        {
            yield return SelectLongestBar(fixture);
            LiveOpsTimelineBar second = fixture.BarOf(LiveOpsWorstCaseSample.ShortEntryKey);
            yield return UxEventSender.Click(fixture.Window, second, UxEventSender.ActionModifier);
            yield return MovePointerOffTimeline(fixture);
        }

        /// <summary>
        /// Kéo một thanh của tài liệu xấu nhất để bật toast. Dùng <c>ShortEntryKey</c> (14/9 → 15/9, CHƯA bắt đầu ở mốc
        /// 13/9 08:47) chứ không dùng thanh id dài: thanh ấy ĐANG CHẠY, mà kéo thanh đang chạy đi qua đường hỏi xác nhận
        /// chứ không qua đường toast — cùng luật chia thanh mà <see cref="DragBarToRaiseToast"/> đã ghi.
        /// </summary>
        private static IEnumerator DragWorstCaseBarToRaiseToast(UxHubWindowFixture fixture)
        {
            LiveOpsTimelineBar bar = fixture.BarOf(LiveOpsWorstCaseSample.ShortEntryKey);
            Vector2 from = bar.worldBound.center;
            yield return UxEventSender.Drag(fixture.Window, from, from + new Vector2(ToastDragDistance, 0f),
                UxEventSender.MinimumDragSteps, EventModifiers.None);
        }

        private static IEnumerator SelectTwoBars(UxHubWindowFixture fixture)
        {
            yield return SelectFirstBar(fixture);
            LiveOpsTimelineBar second = fixture.BarOf(LiveOpsDesignSample.HuntEarlyEntryKey);
            yield return UxEventSender.Click(fixture.Window, second, UxEventSender.ActionModifier);
            yield return MovePointerOffTimeline(fixture);
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
        /// <summary>Bề ngang một cú kéo dựng toast (px) — giá trị gốc của cổng W8, giữ nguyên.</summary>
        private const float ToastDragDistance = 60f;

        /// <summary>
        /// Hạn giờ chờ toast hiện ra sau cú kéo. Chọn 1.500ms vì nó ngắn hơn hẳn <c>LiveOpsToast.VisibleSeconds</c> (6 giây):
        /// hết hạn nghĩa là cú kéo KHÔNG ghi được gì, không phải toast đã sống rồi tắt — hai chuyện ấy cần hai câu khác nhau.
        /// </summary>
        private const int ToastReadyTimeoutMilliseconds = 1500;

        private static IEnumerator DragBarToRaiseToast(UxHubWindowFixture fixture)
        {
            LiveOpsTimelineBar bar = fixture.BarOf(LiveOpsDesignSample.HuntEarlyEntryKey);
            Vector2 from = bar.worldBound.center;
            yield return UxEventSender.Drag(fixture.Window, from, from + new Vector2(ToastDragDistance, 0f),
                UxEventSender.MinimumDragSteps, EventModifiers.None);
        }

        /// <summary>
        /// Toast ĐANG hiện thật trên màn: vừa còn trong vòng đời của chính nó (<c>IsVisible</c>), vừa còn được vẽ ra
        /// (<c>IsShownOnScreen</c>). Hỏi cả hai vì toast ẩn bằng class rồi mới rời layout sau một nhịp mờ dần, nên một mình
        /// <c>IsVisible</c> có lúc nói "còn" trong khi <c>display</c> đã tắt.
        /// </summary>
        private static bool IsToastShown(UxHubWindowFixture fixture)
        {
            LiveOpsToast toast = fixture.Window == null ? null : fixture.Window.Toast;
            return toast != null && toast.IsVisible && UxLayoutAuditor.IsShownOnScreen(toast);
        }

        private static IEnumerator ZoomTimeline(UxHubWindowFixture fixture, int notches)
        {
            LiveOpsTimelineElement timeline = fixture.Calendar.Timeline;
            yield return UxEventSender.Wheel(fixture.Window, timeline.worldBound.center, notches, UxEventSender.ActionModifier);
        }

        /// <summary>Hạn giờ chờ popover mở sau một cú bấm, trước khi thử bấm lại.</summary>
        private const int PopoverOpenTimeoutMilliseconds = 800;

        private static IEnumerator OpenAddEventPopover(UxHubWindowFixture fixture)
        {
            // W9-16: bấm rồi chờ THEO ĐIỀU KIỆN, và nếu hết hạn thì truy lại nút MỘT lần nữa. Cú bấm đầu có thể rơi vào một
            // khung mà header đang dựng lại, nên toạ độ tâm nút đã cũ và con trỏ hạ xuống chỗ trống — đo được ở lượt 4: chờ
            // 9 giây, 60 khung, popover không bao giờ mở, còn lượt 3 và lượt 5 cùng cây thì xanh. Truy lại nút (chứ không
            // bấm lại toạ độ cũ) là cách duy nhất tự chữa được đúng nguyên nhân đó.
            yield return ClickAddEventButton(fixture);
            if (IsAddEventPopoverOpen()) yield break;
            yield return ClickAddEventButton(fixture);
            Assert.IsTrue(IsAddEventPopoverOpen(), "bấm Thêm đợt hai lần vẫn không mở popover nào (UX-10)");
        }

        private static IEnumerator ClickAddEventButton(UxHubWindowFixture fixture)
        {
            Button add = fixture.AddEventButton();
            Assert.IsNotNull(add, "màn Lịch không có nút Thêm đợt trong phần hành động của header (UX-10)");
            yield return UxEventSender.Click(fixture.Window, add);
            yield return UxEventSender.WaitUntilOrTimeout(IsAddEventPopoverOpen, PopoverOpenTimeoutMilliseconds);
        }

        private static bool IsAddEventPopoverOpen()
        {
            return LiveOpsPopoverContent.Current != null && LiveOpsPopoverContent.Current.IsOpen;
        }

        private static EditorWindow PopoverWindow(UxHubWindowFixture fixture)
        {
            return LiveOpsPopoverContent.Current == null ? null : LiveOpsPopoverContent.Current.editorWindow;
        }

        /// <summary>
        /// (W10-07) Hộp xác nhận in ID ĐỢT dài nhất của tài liệu xấu nhất. Hộp rộng cố định nên id dài là chỗ duy nhất chữ
        /// của nó chạm mép — bản mẫu đẹp dùng id ngắn nên chỗ ấy chưa lần nào bị thử.
        /// </summary>
        private IEnumerator OpenWorstCaseConfirmWindow(UxHubWindowFixture fixture)
        {
            yield return OpenConfirmWindowWithEntryKey(LiveOpsWorstCaseSample.LongEntryKey);
        }

        private IEnumerator OpenConfirmWindow(UxHubWindowFixture fixture)
        {
            yield return OpenConfirmWindowWithEntryKey(LiveOpsDesignSample.LavaQuestEarlyEntryKey);
        }

        private IEnumerator OpenConfirmWindowWithEntryKey(string entryKey)
        {
            if (_confirmWindow != null) _confirmWindow.Close();
            // Dựng đúng hộp mà đường kéo rút ngắn đợt đang chạy sinh ra (CalendarTimelinePresenter): cùng tiêu đề, cùng
            // thân cảnh báo, cùng cặp nhãn nút — để lượt kiểm nói về hộp THẬT chứ không về một hộp rỗng.
            _confirmWindow = LiveOpsConfirmWindow.OpenForTest(new LiveOpsConfirmRequest.Builder()
                .WithTitle(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarShortenConfirmTitleFormat,
                    entryKey))
                .WithBody(LiveOpsHubStrings.CalendarUnknownPlayerCountSentence)
                .WithHelpBoxWarning()
                .WithButtons(LiveOpsHubStrings.CalendarShortenDestructiveLabel, LiveOpsHubStrings.KitConfirmKeepLabel)
                .Build());
            yield return UxEventSender.Settle(UxEventSender.SettleFrames * 3, UxEventSender.SettleMilliseconds * 4);
        }
    }
}
