using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

using DreamTech.LiveOps.Tests;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Đợt W8-UX gói G-UX-INSPECTOR — test KHOÁ LỖI cho UX-04, UX-08, UX-09, UX-10 (plan/w8-ux/UX-FIX-PLAN.md mục 1.1).
    /// Mỗi test mô tả đúng một triệu chứng người dùng giả đã gặp trong Unity GUI, nên khi nó đỏ lại thì biết ngay lỗi nào
    /// quay về.
    /// <para>
    /// Lỗi thuộc TƯƠNG TÁC đi qua đường sự kiện thật của panel: gõ bằng <c>ITextEdition.UpdateText</c> (đúng hàm mà
    /// <c>KeyboardTextEditorEventHandler</c> gọi mỗi phím) rồi chuyển focus bằng <c>FocusController</c> — ô
    /// <c>isDelayed</c> chốt chữ trên đúng đường đó, giống Enter/Tab/bấm ra ngoài của người dùng. Test KHÔNG gọi hàm model
    /// để gây hành vi; model chỉ dùng để ĐỌC kết quả.
    /// </para>
    /// <para>
    /// Lỗi thuộc BỐ CỤC đo trên cửa sổ hub thật ở 1280×760 (cỡ mà ảnh hành trình đã chụp): chữ bị cắt đo bằng
    /// <c>MeasureTextSize</c> so với <c>contentRect</c>, con tràn khỏi cha đo bằng <c>worldBound</c> — không kết luận
    /// bằng tên class.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class CalendarInspectorUxFixTests
    {
        private const int MaximumLayoutFrames = 60;
        private const int MaximumLayoutMilliseconds = 5000;
        /// <summary>
        /// Chuỗi thô UJ-04 ghi vào asset: ngày thiếu số 0 (ở phần tháng) ghép với giờ bằng DẤU CÁCH. Ngày đoán lại được là
        /// 19/9, SAU giờ bắt đầu 16/9 của hunt-0916-bonus — nút "Sửa thành …" mới còn giữ được phần giờ thay vì bị kẹp về
        /// "bắt đầu + 1 giờ".
        /// </summary>
        private const string BrokenEndRawText = "2026-9-19 12:00";

        /// <summary>Tên bước Undo của lần sửa do chính test gây ra — không phải chữ người dùng đọc.</summary>
        private const string TestUndoName = "ux-fix-test";

        private const int WindowWidth = 1280;
        private const int WindowHeight = 760;

        /// <summary>Cỡ hẹp nhất của cổng — nằm sâu dưới bậc <c>--medium</c> (1100px).</summary>
        private static readonly UxWindowSize MediumWidthSize = new UxWindowSize(700, 560);

        /// <summary>
        /// Cỡ hẹp nhất của cổng — nằm sâu dưới bậc <c>--medium</c> (1100px). Khai báo TRƯỚC hai mảng dùng nó: field
        /// <c>static readonly</c> khởi tạo theo thứ tự khai báo, nên đứng sau thì mảng nhận một cỡ 0×0 mà không ai báo.
        /// </summary>
        private static readonly UxWindowSize MediumWidthSize = new UxWindowSize(700, 560);

        /// <summary>
        /// (soát W10 F1) Bộ cỡ cho ca nhãn đã wrap: cỡ HẸP NHẤT của cổng, cỡ ảnh hành trình và cỡ rộng rãi.
        /// Nhãn wrap ra khỏi luật dư bề rộng nên chỗ nó có thể hỏng là chiều cao, mà chiều cao chỉ đổi khi bề rộng khả dụng
        /// đổi — đo một cỡ thì không biết gì về cỡ kia.
        /// <para>
        /// (G-W10-CAL2) 700×560 ĐÃ TRỞ LẠI bộ này. Trước đó nó bị bỏ ra vì ở bậc <c>--medium</c> (dưới 1100px) pane chọn
        /// nhiều mang <c>display: none</c> — không có layout nào để đo, đo ở đó là đo một cây chết. Lỗi ấy nay đã chữa
        /// (xem <see cref="MultiSelectDrawer_AtEveryMediumWidth_OpensAndClosesWithEscape"/>), nên cỡ HẸP NHẤT của cổng
        /// đo được thật và phải có mặt: chiều cao wrap chỉ đổi khi bề rộng khả dụng đổi.
        /// </para>
        /// </summary>
        private static readonly UxWindowSize[] MultiSelectLabelSizes =
        {
            MediumWidthSize, new UxWindowSize(WindowWidth, WindowHeight), new UxWindowSize(1440, 900),
        };

        /// <summary>
        /// BỐN cỡ của cổng nằm dưới mốc <c>LiveOpsHubBreakpoints.MediumBelowWidth</c> = 1100px, tức bốn cỡ mà inspector Lịch
        /// là DRAWER. Ca drawer chọn nhiều đi hết bộ này: lỗi nó khoá là lỗi chỉ xuất hiện ở bậc <c>--medium</c>, nên đo một
        /// cỡ rồi suy ra ba cỡ kia là đúng cái lỗ hổng ma trận mà đợt W10 được mở ra để vá.
        /// </summary>
        private static readonly UxWindowSize[] MediumWidthSizes =
        {
            MediumWidthSize, new UxWindowSize(820, 560), new UxWindowSize(SnugProbeWidth, SnugProbeHeight),
            new UxWindowSize(1024, 700),
        };

        /// <summary>
        /// Số tài liệu mà ca drawer chọn nhiều chạy qua: mẫu thiết kế và tài liệu XẤU NHẤT. Hai bản, vì pane này vừa được
        /// làm cho HIỆN RA ở bốn cỡ hẹp nên từ nay nó là pane người dùng đọc thật, mà chữ chỉ chạm mép khi dữ liệu xấu.
        /// </summary>
        private const int MultiSelectDocumentCount = 2;

        /// <summary>
        /// Quãng cách hai bên mốc --medium của ca dò mốc. Lớn hơn mức kẹp cửa sổ mà cổng còn chấp nhận (4px) nên một pixel
        /// lệch của hệ điều hành không lật kết luận; nhỏ hơn nửa quãng 100px giữa --snug (1000) và --medium (1100) nên mốc
        /// trôi về 1000 vẫn làm ca đỏ.
        /// </summary>
        private const int BreakpointProbeMargin = 24;

        /// <summary>Chiều cao cửa sổ của ca dò mốc — mốc này chỉ theo BỀ RỘNG, chiều cao chỉ cần đủ để màn dựng đủ khối.</summary>
        private const int BreakpointProbeHeight = 760;

        /// <summary>Ký tự của phím Esc trong sự kiện phím: UIElements mang cả ký tự lẫn mã phím, nhánh Esc đọc cả hai.</summary>
        private const char EscapeCharacter = '\u001b';

        /// <summary>Cỡ dò của bậc --snug (W9-20): 950 nằm giữa 900 và 1000, tức trong khoảng mù cũ của ma trận cổng.</summary>
        private const int SnugProbeWidth = 950;
        private const int SnugProbeHeight = 700;

        /// <summary>Đợt SINH TỪ LUẬT của lịch mẫu — chọn nó thì inspector dựng khối field chỉ đọc (W9-21).</summary>
        private const string RecurringEntryKey = "weekly-pass#35";

        /// <summary>Chữ test gõ vào ô chỉ đọc. Khác mọi giá trị thật của lịch mẫu để "không đổi" là kết luận đọc được.</summary>
        private const string TypedProbeText = "gõ thử vào ô chỉ đọc";

        /// <summary>Giá trị ban đầu của ô số ĐỐI CHỨNG — cú kéo nhãn phải đổi được nó, nếu không thì cử chỉ mô phỏng là giả.</summary>
        private const int DragProbeStartValue = 12;

        /// <summary>Số bước của cú kéo mô phỏng và quãng kéo ngang — đủ dài để dragger của IntegerField vượt ngưỡng nhạy.</summary>
        private const int DragSteps = 6;
        private const float DragDistancePixels = 120f;

        /// <summary>Sai số đo chữ: <c>MeasureTextSize</c> làm tròn khác trình bày một chút ở cả hai bản Unity.</summary>
        private const float TextMeasureTolerance = 1.5f;

        /// <summary>Sai số hình học khi so mép con với mép cha (bo/viền nửa pixel).</summary>
        private const float BoundsTolerance = 0.75f;

        /// <summary>Tiền tố class của hub — phần tử mang class này là thứ bản dựng cố ý tạo ra để người dùng nhìn thấy.</summary>
        private const string HubClassPrefix = "liveops-hub-";

        private LiveOpsHubWindow _window;
        private ControlsTestPanel _panel;

        /// <summary>Cửa sổ của ca đo theo CỠ (W9-20) — mở qua fixture của cổng vì chỉ chỗ đó đặt được ngôn ngữ và cỡ cùng lúc.</summary>
        private UxHubWindowFixture _fixture;

        [TearDown]
        public void TearDown()
        {
            if (_window != null)
            {
                _window.Close();
                _window = null;
            }
            _panel?.Dispose();
            _panel = null;
            if (_fixture != null)
            {
                _fixture.Dispose();
                _fixture = null;
            }
            UxHubWindowFixture.CloseStrayWindows();
            LiveOpsHubTestServices.ReleaseAll();
        }

        // ============================================================================================ UX-04 · ô giờ ghi được

        /// <summary>
        /// UJ-03: chọn hunt-0916-bonus, gõ "18:00" vào ô giờ Bắt đầu rồi rời ô. Người dùng thấy ô đổi và dòng giờ máy đổi
        /// theo nên tưởng đã ăn, nhưng tài liệu không hề đổi — inspector chỉ nghe chuỗi THÔ, còn giá trị đọc được đi bằng
        /// <c>ChangeEvent&lt;DateTime&gt;</c> mà không ai nghe.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux04_StartTimeTypedThenFocusOut_WritesToDocument()
        {
            yield return OpenCalendarWithSelection(LiveOpsDesignSample.HuntBonusEntryKey);
            LiveOpsUtcDateTimeField startField = TimeFieldOf(LiveOpsHubStrings.CalendarFieldStartLabel);
            Assert.IsNotNull(startField, "inspector của đợt cố định phải có ô giờ Bắt đầu");
            TextField outside = AddFocusTarget();
            yield return WaitForLayout(startField, outside);

            // Hồi quy ed44a31: vùng nhập của ô giờ UTC rộng 0 — ô vẫn "có" nhưng người dùng không thấy chỗ nào để gõ.
            AssertNoZeroSize(startField, "ô giờ Bắt đầu của inspector");

            yield return MoveFocus(startField.TimeInput);
            TypeText(startField.TimeInput, "18:00");
            yield return MoveFocus(outside);
            yield return null;

            Assert.IsTrue(Services().Session.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey,
                out FixedLiveEventEntry entry), "đợt phải còn trong tài liệu");
            Assert.AreEqual("2026-09-16T18:00:00Z", entry.StartUtcText,
                "gõ giờ hợp lệ rồi rời ô PHẢI ghi vào tài liệu — đây là đường sửa giờ cơ bản nhất của inspector (UJ-03)");
        }

        /// <summary>
        /// UJ-03 nửa sau: sửa xong giờ thì bước Undo phải mang TÊN NGẮN ("Dời hunt-0916-bonus"), không phải câu toast dài
        /// — toast sau ⌘Z ghép tiền tố "Đã hoàn tác:" vào tên bước, nên tên bước là câu "Đã dời …" sẽ thành "Đã hoàn tác:
        /// Đã dời …" (UJ-10).
        /// </summary>
        [UnityTest]
        public IEnumerator Ux04_InspectorTimeEdit_NamesUndoStepShort()
        {
            yield return OpenCalendarWithSelection(LiveOpsDesignSample.HuntBonusEntryKey);
            List<LiveOpsToastModel> toasts = new List<LiveOpsToastModel>();
            Section().Presenter.ToastRequested += toast => toasts.Add(toast);
            LiveOpsUtcDateTimeField startField = TimeFieldOf(LiveOpsHubStrings.CalendarFieldStartLabel);
            TextField outside = AddFocusTarget();
            yield return WaitForLayout(startField, outside);

            yield return MoveFocus(startField.TimeInput);
            TypeText(startField.TimeInput, "18:00");
            yield return MoveFocus(outside);
            yield return null;

            Assert.AreEqual(1, toasts.Count, "một lần sửa ô giờ = đúng một toast");
            // (Cổng đợt W8-UX) Gói A tách một động từ "Đổi {0}" thành ba động từ thật (Dời / Đổi mép đầu / Đổi mép cuối, UX-26),
            // và inspector đi CHUNG đường đặt tên đó (CalendarTimelinePresenter.DragUndoStepName). Ô đang sửa ở đây là ô Bắt đầu
            // nên tên bước đúng là "Đổi mép đầu {0}" — cụ thể hơn câu cũ, nên test siết theo khoá mới chứ không nới ra.
            string expected = string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarDepthMoveStartEdgeUndoStepFormat, "hunt-0916-bonus");
            Assert.AreEqual(expected, toasts[0].UndoGroupName,
                "bước Undo của sửa trong inspector phải là câu ngắn như đường kéo — \"\" làm toast Hoàn tác lặp chữ (UJ-10)");
            Assert.AreNotEqual(toasts[0].Message, toasts[0].UndoGroupName, "toast giữ câu dài, Undo History không");
        }

        /// <summary>
        /// UX-04 ở popover Thêm đợt bước 2: gõ ngày/giờ HỢP LỆ vào ô giờ Bắt đầu rồi rời ô. Popover chỉ nghe đường chuỗi
        /// HỎNG nên luồng không đổi — bước 3 hiện giờ cũ và đợt được thêm với giờ cũ, đúng triệu chứng UJ-03 nhưng ở màn
        /// khác. Test đi qua ô thật (gõ + chuyển focus), không gọi <c>WithTimes</c>.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux04_PopoverStartTimeTyped_ReachesFlow()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventFlowModel flow = AddEventFlowModel.Create(services.Session, services.Clock.UtcNow)
                .WithType("treasure-hunt")
                .Next();
            AddEventPopover popover = new AddEventPopover(services, flow, _ => { });
            VisualElement root = OpenPopoverInPanel(popover);
            TextField outside = new TextField();
            root.parent.Add(outside);
            yield return WaitForLayout(root, outside);

            LiveOpsUtcDateTimeField startField = root.Q<LiveOpsUtcDateTimeField>();
            Assert.IsNotNull(startField, "bước 2 của popover phải có ô giờ Bắt đầu");
            AssertNoZeroSize(startField, "ô giờ Bắt đầu của popover");
            Assert.AreNotEqual("2026-09-21", popover.Flow.StartDateText, "mốc đầu của test phải khác ngày sắp gõ");

            yield return MoveFocus(startField.DateInput);
            TypeText(startField.DateInput, "2026-09-21");
            yield return MoveFocus(outside);
            yield return null;

            Assert.AreEqual("2026-09-21", popover.Flow.StartDateText,
                "gõ ngày hợp lệ rồi rời ô PHẢI tới được luồng — nếu không, bước 3 hiện giờ cũ và đợt được thêm với giờ cũ (UX-04)");

            LiveOpsUtcDateTimeField rebuiltField = root.Q<LiveOpsUtcDateTimeField>();
            Assert.IsNotNull(rebuiltField, "dựng lại bước 2 vẫn phải còn ô giờ");
            yield return MoveFocus(rebuiltField.TimeInput);
            TypeText(rebuiltField.TimeInput, "08:30");
            yield return MoveFocus(outside);
            yield return null;

            Assert.AreEqual("08:30", popover.Flow.StartTimeText, "nửa giờ cũng phải tới được luồng");
            Assert.IsTrue(popover.Flow.StartUtc.HasValue, "gõ đủ cặp ngày/giờ hợp lệ thì luồng phải đọc ra một giờ UTC");
            Assert.AreEqual(new DateTime(2026, 9, 21, 8, 30, 0, DateTimeKind.Utc), popover.Flow.StartUtc.Value);
        }

        /// <summary>
        /// UJ-04: ô ngày Kết thúc gõ thiếu một số ("2026-09-1") thì chuỗi thô ghi vào asset là "2026-09-1 12:00" — ghép
        /// bằng DẤU CÁCH. Lần dựng lại chỉ biết tách theo "T" nên phần giờ biến mất: Dài về 0, câu lỗi vừa trích "12:00"
        /// vừa bảo "Ô giờ còn trống", và đợt rơi khỏi trục.
        /// </summary>
        [Test]
        public void Ux04_RawTimeJoinedWithSpace_KeepsTimePart()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey,
                out FixedLiveEventEntry entry));
            services.Session.Apply(new ReplaceFixedEventEdit(entry.WithTimes(entry.StartUtcText, BrokenEndRawText)), TestUndoName);

            CalendarInspectorModel model = CalendarInspectorModel.Build(services.Session,
                LiveOpsDesignSample.HuntBonusEntryKey, services.Clock.UtcNow, services.Format);

            Assert.AreEqual(CalendarInspectorModel.StateUnreadableTimes, model.State);
            Assert.IsTrue(model.IsUnreadableEnd, "ô hỏng là Kết thúc");
            StringAssert.Contains("2026-9-19", model.UnreadableFieldErrorText, "câu lỗi phải nêu đúng chữ người dùng gõ");
            StringAssert.DoesNotContain(LiveOpsHubStrings.UtcFieldTimeEmpty, model.UnreadableFieldErrorText,
                "câu lỗi không được vừa trích giờ vừa bảo ô giờ trống — phần giờ vẫn còn trong chuỗi thô (UJ-04)");
            Assert.IsNotEmpty(model.UnreadableFixButtonText, "ngày đoán được thì phải có nút \"Sửa thành …\"");
            StringAssert.Contains("12:00", model.UnreadableFixValueText,
                "giá trị sửa nhanh phải giữ phần giờ người dùng KHÔNG đụng tới");
        }

        /// <summary>
        /// UJ-04 trên panel thật: ô giờ Kết thúc không được tự trống sau khi dựng lại từ chuỗi thô ghép bằng dấu cách.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux04_UnreadableEndWithSpace_KeepsTimeInputFilled()
        {
            yield return OpenCalendar();
            LiveOpsHubServices services = Services();
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey,
                out FixedLiveEventEntry entry));
            services.Session.Apply(new ReplaceFixedEventEdit(entry.WithTimes(entry.StartUtcText, BrokenEndRawText)), TestUndoName);
            Section().Presenter.SetSelectedBarKey(LiveOpsDesignSample.HuntBonusEntryKey);
            yield return null;

            LiveOpsUtcDateTimeField endField = TimeFieldOf(LiveOpsHubStrings.CalendarFieldEndLabel);
            Assert.IsNotNull(endField);
            Assert.AreEqual("2026-9-19", endField.RawDateText, "ô ngày giữ nguyên chữ người dùng gõ");
            Assert.AreEqual("12:00", endField.RawTimeText,
                "ô giờ người dùng KHÔNG đụng tới không được bị xoá khi dựng lại inspector (UJ-04)");
        }

        // ============================================================================================ UX-08 · inspector 280px

        /// <summary>
        /// C3 + C5 + UJ-19: pane inspector rộng 280px cắt "UTC", "= 1 ngày", "Về mặc định", "Sửa ở Loại event" và mọc
        /// thanh cuộn ngang; trên 2022.3 ô nhập còn giãn hết bề rộng làm hậu tố và nút biến mất hẳn.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux08_InspectorPane_NoCutTextNoHorizontalScroll()
        {
            yield return OpenCalendarWithSelection(LiveOpsDesignSample.HuntBonusEntryKey);
            ScrollView body = InspectorBody();
            yield return WaitForLayout(body);

            Assert.AreEqual(DisplayStyle.None, body.horizontalScroller.resolvedStyle.display,
                "inspector không bao giờ được cuộn ngang — thiết kế không có thanh đó (C3)");
            AssertNoCutText(body, "inspector");
            AssertInsideParent(body, "inspector");
        }

        /// <summary>
        /// C5: hậu tố "giờ", ghi chú "= 1 ngày" và nút "Về mặc định" phải NHÌN THẤY được — trên 2022.3 ô nhập mặc định
        /// <c>flex-grow: 1</c> nên chúng bị đẩy ra ngoài pane.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux08_DurationSuffixAndConfigKeyReset_StayInsidePane()
        {
            yield return OpenCalendarWithSelection(LiveOpsDesignSample.HuntBonusEntryKey);
            ScrollView body = InspectorBody();
            yield return WaitForLayout(body);

            Label unit = FindLabelWithText(body, LiveOpsHubStrings.CalendarDurationUnitLabel);
            Assert.IsNotNull(unit, "hàng Dài phải có hậu tố đơn vị");
            AssertVisibleInside(unit, body, "hậu tố \"giờ\" của hàng Dài");

            Button reset = FindButtonWithText(body, LiveOpsHubStrings.CalendarConfigKeyResetButton);
            Assert.IsNotNull(reset, "hàng Config key phải có nút Về mặc định");
            AssertVisibleInside(reset, body, "nút \"Về mặc định\"");

            Button editType = FindButtonWithText(body, LiveOpsHubStrings.CalendarEditTypeLink);
            Assert.IsNotNull(editType, "hàng Phải bấm tham gia phải có link sang Loại event");
            AssertVisibleInside(editType, body, "link \"Sửa ở Loại event\"");
        }

        /// <summary>
        /// C3: hai ô của <see cref="LiveOpsUtcDateTimeField"/> và nhãn "UTC" phải NHÌN THẤY được trọn vẹn trong pane 280px —
        /// cột nhãn 120px mặc định của BaseField đẩy "UTC" qua mép, và mọi cách bóp cột nhãn mà làm hỏng vùng nhập thì ô giờ
        /// biến mất hẳn (người dùng không còn chỗ gõ).
        /// </summary>
        [UnityTest]
        public IEnumerator Ux08_UtcFieldParts_StayVisibleInsidePane()
        {
            yield return OpenCalendarWithSelection(LiveOpsDesignSample.HuntBonusEntryKey);
            ScrollView body = InspectorBody();
            yield return WaitForLayout(body);

            string[] labels = { LiveOpsHubStrings.CalendarFieldStartLabel, LiveOpsHubStrings.CalendarFieldEndLabel };
            for (int index = 0; index < labels.Length; index++)
            {
                LiveOpsUtcDateTimeField field = TimeFieldOf(labels[index]);
                Assert.IsNotNull(field, "inspector thiếu ô giờ \"" + labels[index] + "\"");
                AssertVisibleInside(field.DateInput, body, "ô ngày của \"" + labels[index] + "\"");
                AssertVisibleInside(field.TimeInput, body, "ô giờ của \"" + labels[index] + "\"");
                AssertVisibleInside(field.ZoneLabel, body, "nhãn UTC của \"" + labels[index] + "\"");
                Assert.AreEqual(88f, field.DateInput.layout.width, 1f,
                    "ô ngày 88px (W9-19: chữ 75px ở 2022.3 + 4+4px đệm [SD1 §3.1] + 2px viền TextInput + 1,5px dư mỗi bên)");
                Assert.AreEqual(44f, field.TimeInput.layout.width, 1f, "ô giờ 44px [SD1 §3.1]");
            }
        }

        // ============================================================================================ UX-09 · card & ghi chú

        /// <summary>
        /// C4 + UJ-16: card "Vấn đề" dùng lại hàng NGANG của màn Kiểm lịch nên headline bị cắt và hai nút đề xuất nằm
        /// ngoài pane — mất hẳn đường sửa lỗi ngay tại inspector.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux09_FindingCard_StacksAndKeepsRepairButtonsInsidePane()
        {
            yield return OpenCalendarWithSelection(LiveOpsDesignSample.HuntBonusEntryKey);
            ScrollView body = InspectorBody();
            yield return WaitForLayout(body);

            VisualElement card = body.Q(className: LiveOpsHubClassNames.FindingRow);
            Assert.IsNotNull(card, "hunt-0916-bonus chồng giờ với hunt-0914 nên inspector phải có card Vấn đề");
            Assert.AreEqual(FlexDirection.Column, card.resolvedStyle.flexDirection,
                "card Vấn đề trong pane 280px phải xếp CỘT — hàng ngang đẩy nút đề xuất ra ngoài pane (C4)");

            List<Button> repairs = new List<Button>();
            card.Query<Button>().ToList(repairs);
            Assert.Greater(repairs.Count, 0, "phát hiện chồng giờ luôn có ít nhất một đề xuất sửa");
            for (int index = 0; index < repairs.Count; index++)
            {
                AssertVisibleInside(repairs[index], body, "nút đề xuất \"" + repairs[index].text + "\"");
            }
            AssertNoCutText(card, "card Vấn đề");
        }

        /// <summary>C6 + UJ-16: hai câu ghi chú dài của inspector phải xuống dòng, không bị mép pane cắt mất nửa câu.</summary>
        [UnityTest]
        public IEnumerator Ux09_RecurringNoteAndMultiSelectHelp_Wrap()
        {
            yield return OpenCalendarWithSelection("weekly-pass#35");
            ScrollView body = InspectorBody();
            yield return WaitForLayout(body);

            Label note = FindLabelWithText(body, Section().Inspector.Model.RecurringNoteText);
            Assert.IsNotNull(note, "trạng thái (b) phải có câu \"Đợt sinh từ luật …\"");
            Assert.AreEqual(WhiteSpace.Normal, note.resolvedStyle.whiteSpace,
                "câu ghi chú dài phải xuống dòng — một dòng thì mất nửa câu quan trọng (C6)");
            AssertNoCutText(body, "inspector đợt sinh từ luật");

            Section().Presenter.SetSelectedBarKeys(
                new[] { LiveOpsDesignSample.HuntBonusEntryKey, LiveOpsDesignSample.HuntEarlyEntryKey },
                LiveOpsDesignSample.HuntBonusEntryKey);
            yield return WaitForLayout(body);

            Label help = FindLabelWithText(body, LiveOpsHubStrings.TimelineMultiSelectHelpText);
            Assert.IsNotNull(help, "trạng thái (c) phải có câu hướng dẫn dời cả tập");
            Assert.AreEqual(WhiteSpace.Normal, help.resolvedStyle.whiteSpace,
                "câu \"Số dương dời về sau …\" phải xuống dòng (UJ-16)");
            AssertNoCutText(body, "inspector chọn nhiều");
        }

        // ============================================================================================ UX-10 · popover Thêm đợt

        /// <summary>
        /// UJ-17: gõ "hunt" lọc còn đúng một loại rồi Enter — popover đứng im vì không hàng nào đang trỏ và
        /// <c>CanAdvance</c> false. Mở popover cũng không trỏ hàng nào.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux10_FilterToSingleType_PointsAtItSoEnterAdvances()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventPopover popover = new AddEventPopover(services,
                AddEventFlowModel.Create(services.Session, services.Clock.UtcNow), _ => { });
            VisualElement root = OpenPopoverInPanel(popover);
            yield return WaitForLayout(root);

            Assert.IsNotEmpty(popover.Flow.EventType, "mở popover phải trỏ sẵn hàng đầu, không để bước 1 chết (UJ-17)");

            TextField filter = root.Q<TextField>(LiveOpsHubPaths.AddEventPopoverElementNames.TypeFilter);
            Assert.IsNotNull(filter);
            filter.value = "hunt";
            yield return null;

            Assert.AreEqual("treasure-hunt", popover.Flow.EventType,
                "lọc còn đúng một loại thì loại đó phải thành loại đang trỏ (UJ-17)");
            // Gửi phím vào ô lọc (nơi con trỏ người dùng đang ở) chứ không vào gốc themed root: callback Enter của popover đăng
            // ký TrickleDown trên CÂY NỘI DUNG, là con của gốc — phím nhắm thẳng vào gốc không bao giờ đi qua nó. Và phải FOCUS
            // ô lọc trước: 2022.3 định tuyến phím theo phần tử ĐANG focus chứ không theo target của sự kiện, nên không focus thì
            // phím rơi về gốc panel và lại không đi qua cây nội dung (xanh ở 6000.6, đỏ ở 2022.3).
            yield return MoveFocus(filter);
            SendKeyDown(filter, '\n', KeyCode.Return);
            Assert.AreEqual(AddEventFlowModel.StepChooseTimes, popover.Flow.Step,
                "Enter sau khi lọc còn một loại phải sang bước 2");
        }

        /// <summary>
        /// C7: nút chính "Thêm hunt-0921 vào lịch" và nút danger "Vẫn thêm (game sẽ bỏ đợt này)" bị cắt trong popover
        /// 320px — nút phá huỷ giấu đúng câu hậu quả.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux10_ReviewStepButtons_NotCut()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventPopover popover = new AddEventPopover(services, ReviewFlow(services, "2026-09-21"), _ => { });
            VisualElement root = OpenPopoverInPanel(popover);
            yield return WaitForLayout(root);

            VisualElement buttons = root.Q(LiveOpsHubPaths.AddEventPopoverElementNames.Buttons);
            Assert.IsNotNull(buttons);
            AssertNoCutText(buttons, "hàng nút của popover Thêm đợt");
            AssertInsideParent(root, "popover Thêm đợt");
        }

        /// <summary>C7 biến thể chồng giờ: nút danger phải đọc được TRỌN câu hậu quả.</summary>
        [UnityTest]
        public IEnumerator Ux10_OverlapStepButtons_NotCut()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventPopover popover = new AddEventPopover(services, ReviewFlow(services, "2026-09-15"), _ => { });
            VisualElement root = OpenPopoverInPanel(popover);
            yield return WaitForLayout(root);
            Assert.IsTrue(popover.Flow.WillBeDropped, "fixture phải là biến thể chồng giờ");

            VisualElement buttons = root.Q(LiveOpsHubPaths.AddEventPopoverElementNames.Buttons);
            AssertNoCutText(buttons, "hàng nút biến thể chồng giờ");
            AssertInsideParent(root, "popover Thêm đợt (chồng giờ)");
        }

        /// <summary>
        /// C9: tag kiểm nhanh gắn nhầm họ FILL (nền quiet) nên thành khối xám kéo hết bề rộng, tương phản 1,87:1 và
        /// không có dấu Ok. V17: chữ dẫn config key dùng mono cỡ thường và dòng phụ in nguyên dấu backtick.
        /// </summary>
        [UnityTest]
        public IEnumerator Ux10_QuickCheckTagAndConfigKeyHint_FollowDesign()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            AddEventPopover popover = new AddEventPopover(services, ReviewFlow(services, "2026-09-21"), _ => { });
            VisualElement root = OpenPopoverInPanel(popover);
            yield return WaitForLayout(root);

            VisualElement tag = root.Q(className: LiveOpsHubClassNames.Tag);
            Assert.IsNotNull(tag, "bước 3 không chồng giờ phải có tag kiểm nhanh");
            Assert.IsFalse(tag.ClassListContains(LiveOpsHubClassNames.FillOk),
                "tag không được mang họ FILL — nền quiet dưới chữ thường chỉ còn 1,87:1 (C9)");
            Assert.IsNotNull(tag.Q<LiveOpsStateMark>(), "tag kiểm nhanh phải có dấu Ok đứng trước (C9)");
            Assert.Less(tag.layout.width, AddEventPopover.PopoverWidth - 1f,
                "tag phải co theo nội dung, không kéo hết bề rộng popover (C9)");

            StringAssert.DoesNotContain("`", LiveOpsHubStrings.CalendarAddConfigKeyNoteFormat,
                "dòng phụ không in dấu backtick của tài liệu ra màn hình (V17)");
            // Hỏi ĐÚNG chữ dẫn của ô Config key: Q<LiveOpsPlaceholder>() không tên trả về chữ dẫn ĐẦU TIÊN trong cây — là chữ
            // dẫn của ô lọc loại ở bước 1 (vẫn còn trong cây, chỉ display:none) — nên mọi khẳng định trước đây đo nhầm phần tử.
            LiveOpsPlaceholder hint = root.Q<LiveOpsPlaceholder>(className: LiveOpsHubClassNames.CalendarFlowHint);
            Assert.IsNotNull(hint, "ô Config key rỗng phải có chữ dẫn");
            // Đo KIỂU CHỮ THẬT, không đo tên class: chữ dẫn chưa bao giờ mang class Mono nên khẳng định "không có class Mono"
            // xanh cả trên code cũ lẫn code mới — một khẳng định rỗng. (Chữ dẫn VẪN kế thừa font mono của ô cha: xem mục 14,
            // phần CHƯA sửa.)
            Assert.AreEqual(10f, hint.resolvedStyle.fontSize, 0.01f, "chữ dẫn phải là cỡ nhỏ 10px [SD1 §3.11] (V17)");
            Assert.AreEqual(FontStyle.Italic, hint.resolvedStyle.unityFontStyleAndWeight,
                "chữ dẫn phải NGHIÊNG để không đọc nhầm thành giá trị đã gõ (V17)");
            AssertNoCutText(root.Q(LiveOpsHubPaths.AddEventPopoverElementNames.StepReview), "bước 3 popover");
        }

        // ============================================================================================ W9-20 · bậc --snug 1000px

        /// <summary>
        /// (W9-20) Cỡ 950×700 nằm trong khoảng mù 900–1099 mà ma trận cổng trước W9 chỉ thử ở đúng một điểm 1024. Ở đó thanh
        /// công cụ màn Lịch đã nhường menu "Bắt lưới" (từ --medium) nhưng vẫn còn công tắc "So với đã đăng (n)" — bậc --snug
        /// nhường nốt nó, và lệnh vẫn còn đường vào qua menu ⋮.
        /// <para>
        /// Đo ở CẢ HAI ngôn ngữ vì chuỗi tiếng Việt dài hơn: luật nhường đặt theo CỠ nên phải đúng cho ngôn ngữ dài nhất.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator W9_20_CalendarToolbar_At950_YieldsCompareToggle_AndKeepsOverflowMenu()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, new UxWindowSize(SnugProbeWidth, SnugProbeHeight),
                    language);
                yield return _fixture.WaitForLayout();

                VisualElement toolbar = _fixture.Root.Q(className: LiveOpsHubClassNames.CalendarToolbar);
                Assert.IsNotNull(toolbar, "màn Lịch phải có thanh công cụ (" + language + ")");

                VisualElement compareToggle = _fixture.Root.Q(className: LiveOpsHubClassNames.CalendarDepthCompareToggle);
                Assert.IsNotNull(compareToggle, "công tắc So với đã đăng phải còn trong cây — nhường chỗ là ĐỔI CLASS, không phải dựng lại thanh");
                Assert.AreEqual(DisplayStyle.None, compareToggle.resolvedStyle.display,
                    "ở " + SnugProbeWidth + "px công tắc \"So với đã đăng\" phải rời thanh (bậc --snug, W9-20, " + language + ")");

                VisualElement overflowMenu = _fixture.Root.Q(className: LiveOpsHubClassNames.CalendarDepthOverflowMenu);
                Assert.IsNotNull(overflowMenu, "thanh phải có menu ⋮");
                Assert.AreEqual(DisplayStyle.Flex, overflowMenu.resolvedStyle.display,
                    "nhường control mà không còn menu ⋮ là CẮT ĐƯỜNG VÀO, không phải thu gọn (" + language + ")");

                AssertNoCutText(toolbar,
                    "thanh công cụ màn Lịch ở " + SnugProbeWidth + "x" + SnugProbeHeight + " (" + language + ")");

                _fixture.Dispose();
                _fixture = null;
            }
        }

        // ============================================================================================ W9-UX09-FOLDOUT

        /// <summary>
        /// (W9-UX09-FOLDOUT) Card "Vấn đề" phải MỞ ở đúng những đợt có vấn đề, dù trước đó vừa xem một đợt sạch. Bản cũ đặt
        /// <c>viewDataKey</c> dùng CHUNG cho mọi đợt, nên viewData khôi phục trạng thái gập của đợt trước SAU khi element gắn
        /// vào panel và ghi đè giá trị đặt theo dữ liệu: xem một đợt sạch rồi chọn đợt hỏng thì card mở ra GẬP và người dùng
        /// không thấy vấn đề nào.
        /// <para>
        /// Ca đi qua CẢ SÁU đợt mẫu theo một thứ tự cố định — trong đó có đợt sạch đứng TRƯỚC đợt hỏng — và mỗi lần đổi lựa
        /// chọn lại đòi đúng một điều: card mở khi và chỉ khi đợt đó có phát hiện. Kèm một khẳng định về CƠ CHẾ: không được
        /// đặt lại khoá viewData dùng chung, vì hành vi trên chỉ đúng nhờ trạng thái card suy từ dữ liệu.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Ux09_IssuesFoldout_OpensForEveryEntryThatHasFindings()
        {
            string[] entryKeys =
            {
                LiveOpsDesignSample.LavaQuestEarlyEntryKey,
                LiveOpsDesignSample.StarTournamentEntryKey,
                LiveOpsDesignSample.HuntBonusEntryKey,
                LiveOpsDesignSample.HuntEarlyEntryKey,
                LiveOpsDesignSample.LavaQuestMidEntryKey,
                LiveOpsDesignSample.LavaQuestLateEntryKey,
            };

            yield return OpenCalendar();
            int openedWithFindings = 0;
            for (int index = 0; index < entryKeys.Length; index++)
            {
                Section().Presenter.SetSelectedBarKey(entryKeys[index]);
                yield return null;
                yield return WaitForLayout(InspectorBody());

                Foldout foldout = InspectorBody().Q<Foldout>(className: LiveOpsHubClassNames.CalendarInspectorIssues);
                if (foldout == null) continue;

                List<VisualElement> cards = new List<VisualElement>();
                foldout.Query(className: LiveOpsHubClassNames.FindingRow).ToList(cards);
                bool hasFindings = cards.Count > 0;
                if (hasFindings) openedWithFindings++;

                Assert.AreEqual(hasFindings, foldout.value,
                    "đợt \"" + entryKeys[index] + "\" có " + cards.Count + " phát hiện mà card Vấn đề "
                    + (foldout.value ? "MỞ" : "GẬP")
                    + " — trạng thái card phải suy từ dữ liệu của chính đợt đang chọn, không thừa kế của đợt xem trước (W9-UX09)");
                Assert.IsTrue(string.IsNullOrEmpty(foldout.viewDataKey),
                    "card Vấn đề lại mang viewDataKey \"" + foldout.viewDataKey + "\" — viewData khôi phục SAU khi gắn panel nên "
                    + "nó ghi đè giá trị đặt theo dữ liệu, và một khoá dùng chung thì đợt hỏng thừa kế trạng thái gập của đợt sạch (W9-UX09)");
            }

            Assert.Greater(openedWithFindings, 0,
                "không đợt nào trong lịch mẫu có phát hiện — ca này sẽ xanh mà không kiểm được gì");
        }

        // ============================================================================ W9-21 · pane chỉ đọc không sửa được

        /// <summary>
        /// (W9-21 · R-10 lượt soát 2) Khối field chỉ đọc của đợt sinh từ luật phải GIỮ NGUYÊN giá trị hiển thị dù người dùng
        /// gõ vào ô hay KÉO NHÃN của ô số.
        /// <para>
        /// Vì sao ca này phải có: W9-21 bỏ <c>SetEnabled(false)</c> để chữ đạt tương phản, và từ đó rào duy nhất còn lại là
        /// <c>isReadOnly</c> — thứ chặn GÕ. Dragger của <c>IntegerField</c> nằm trên NHÃN và là một đường khác hẳn; không ca
        /// nào khẳng định đường đó cũng đóng, nên điều cả pane đang ngầm dựa vào ("xem được, sửa ở Luật lặp") không được canh
        /// bằng test nào. Rủi ro tối đa là HIỂN THỊ sai (không field nào của pane đăng ký
        /// <c>RegisterValueChangedCallback</c>), nhưng con số sai trên chỗ DUY NHẤT đọc được giờ của lần lặp thì vẫn là lỗi.
        /// </para>
        /// <para>
        /// Chống xanh giả: cú kéo được thử TRƯỚC trên một ô số dùng được (<see cref="AddDragProbe"/>). Cử chỉ mô phỏng không
        /// đổi nổi giá trị ô ấy thì câu "pane chỉ đọc không đổi được" đúng một cách rỗng tuếch.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator W9_21_ReadOnlyPane_KeepsItsValues_WhenTypedIntoOrDragged()
        {
            yield return OpenCalendarWithSelection(RecurringEntryKey);
            ScrollView body = InspectorBody();
            yield return WaitForLayout(body);

            VisualElement pane = body.Q(className: LiveOpsHubClassNames.CalendarInspectorReadOnlyPane);
            Assert.IsNotNull(pane, "đợt sinh từ luật phải có khối field chỉ đọc (W9-21)");
            Assert.IsTrue(pane.enabledInHierarchy,
                "khối chỉ đọc lại bị khoá bằng SetEnabled(false) — đó là cách khoá mà W9-21 vừa dẹp vì nó kéo tương phản xuống, "
                + "và khi khối bị khoá thì ca này không còn kiểm được đường nhập nào");

            TextField idField = pane.Q<TextField>();
            IntegerField duration = pane.Q<IntegerField>();
            Assert.IsNotNull(idField, "khối chỉ đọc phải có ô chữ (id đợt)");
            Assert.IsNotNull(duration, "khối chỉ đọc phải có ô số (độ dài)");
            string idTextBefore = idField.value;
            int durationBefore = duration.value;
            int revisionBefore = Services().Session.DocumentRevision;

            IntegerField dragProbe = AddDragProbe();
            yield return WaitForLayout(dragProbe);

            Assert.IsTrue(UxEventSender.PickReaches(_window, idField),
                "không bấm tới được ô id — cú gõ dưới đây sẽ không đi tới đâu và ca này thành lời khai suông");
            yield return UxEventSender.ReplaceText(_window, idField, TypedProbeText);
            yield return UxEventSender.PressEnter(_window);
            Assert.AreEqual(idTextBefore, idField.value,
                "gõ vào ô chỉ đọc mà giá trị ĐỔI — isReadOnly là rào duy nhất còn lại sau khi W9-21 bỏ SetEnabled(false)");

            yield return DragFieldLabel(dragProbe);
            Assert.AreNotEqual(DragProbeStartValue, dragProbe.value,
                "cú kéo nhãn mô phỏng không đổi nổi giá trị của một ô số DÙNG ĐƯỢC — chưa có cử chỉ thật thì hai câu dưới đây "
                + "đúng một cách rỗng tuếch");

            yield return DragFieldLabel(duration);
            Assert.AreEqual(durationBefore, duration.value,
                "kéo nhãn ô số của khối chỉ đọc làm ĐỔI con số người dùng đang đọc — isReadOnly chặn gõ chứ không chặn dragger "
                + "của IntegerField (W9-21)");
            Assert.AreEqual(revisionBefore, Services().Session.DocumentRevision,
                "khối chỉ đọc đã ghi vào tài liệu — pane này chỉ được đọc, mọi lần sửa đi qua màn Luật lặp");
        }

        // ============================================================================ W9-20 · lý do khoá ở bậc --snug

        /// <summary>
        /// (W9-20 · R-04 lượt soát 2) Ở 950px công tắc "So với đã đăng" rời thanh, nhưng NHÃN LÝ DO thì ở lại.
        /// <para>
        /// Vì sao: bản đầu của W9-20 ẩn cả hai, tức kéo vùng "lệnh bị khoá mà không một chữ nói vì sao" từ dưới 900px lên dưới
        /// 1000px — ngược SPIKE-B SP-3. Ở bậc này lệnh chỉ còn đường vào qua menu ⋮, mà mục menu bị khoá của
        /// <c>DropdownMenuAction</c> không mang được tooltip (nhãn mục cũng chốt cứng lúc append), nên nhãn trên thanh là chỗ
        /// DUY NHẤT còn nói được lý do.
        /// </para>
        /// <para>
        /// Trạng thái "chưa có dấu đã đăng" đặt thẳng qua <c>SetCompareState</c> — người sinh ra trạng thái ấy — để cổng đo
        /// được CẢ cảnh bị khoá, không chỉ cảnh mà lịch mẫu tình cờ đang ở.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator W9_20_CalendarToolbar_At950_KeepsCompareDisabledReason_OnScreen()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar,
                    new UxWindowSize(SnugProbeWidth, SnugProbeHeight), language);
                yield return _fixture.WaitForLayout();

                _fixture.Calendar.Toolbar.SetCompareState(false, false, 0);
                yield return _fixture.WaitForLayout();

                Label reason = _fixture.Calendar.Toolbar.CompareDisabledReason;
                Assert.IsNotNull(reason, "thanh công cụ phải có nhãn lý do của nút \"So với đã đăng\"");
                Assert.AreEqual(LiveOpsHubStrings.CalendarDepthCompareUnavailableReason, reason.text,
                    "nhãn lý do phải in đúng câu lý do (" + language + ")");
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(reason),
                    "ở " + SnugProbeWidth + "px nhãn lý do biến mất cùng cái nút — người dùng chỉ còn thấy một mục xám trong "
                    + "menu ⋮ và không một chữ nào nói vì sao (" + language + ")");

                VisualElement compareToggle = _fixture.Root.Q(className: LiveOpsHubClassNames.CalendarDepthCompareToggle);
                Assert.IsNotNull(compareToggle, "công tắc phải còn trong cây — nhường chỗ là ĐỔI CLASS, không phải dựng lại thanh");
                Assert.AreEqual(DisplayStyle.None, compareToggle.resolvedStyle.display,
                    "ở " + SnugProbeWidth + "px công tắc phải rời thanh (bậc --snug, W9-20, " + language + ")");

                AssertNoCutText(_fixture.Root.Q(className: LiveOpsHubClassNames.CalendarToolbar),
                    "thanh công cụ màn Lịch ở " + SnugProbeWidth + "x" + SnugProbeHeight + " khi lệnh So với bị khoá ("
                    + language + ")");

                _fixture.Dispose();
                _fixture = null;
            }
        }

        // ============================================================================================ W9-30 · biểu tượng lỗi

        /// <summary>
        /// (W9-30) Biến thể dữ liệu XẤU NHẤT của inspector: đợt có giờ KẾT THÚC không đọc được ("2026-10-3"). Đây là trạng
        /// thái DUY NHẤT hiện biểu tượng lỗi 12px, và ma trận bố cục chưa bao giờ dựng nó — nên ở ảnh hành trình 1280×760
        /// biểu tượng bị mép cửa sổ cắt đôi mà không cổng nào đỏ.
        /// <para>
        /// Đo được trước khi sửa: hàng giá trị (ô ngày 88 + 4 + ô giờ 44 + 4 + "UTC" 21) đã dùng 161px trong 168px mà cột
        /// field của pane 280px cho; cộng 4 + 12px biểu tượng là cần 177px, và mọi phần tử trong hàng đều
        /// <c>flex-shrink: 0</c> nên không ai nhường — hàng tràn 9px ra ngoài. Cách chữa là ĐƯA BIỂU TƯỢNG XUỐNG DÒNG LỖI,
        /// nên ca này khoá cả ba điều: biểu tượng nằm trong dòng lỗi, mọi phần nhìn thấy được nằm trong pane, và bề rộng
        /// hàng giá trị KHÔNG đổi giữa trạng thái sạch và trạng thái lỗi.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator W9_30_UnreadableEndTime_KeepsErrorIconInsidePane()
        {
            yield return OpenCalendarWithSelection(LiveOpsDesignSample.LavaQuestLateEntryKey);
            ScrollView body = InspectorBody();
            yield return WaitForLayout(body);

            LiveOpsUtcDateTimeField endField = TimeFieldOf(LiveOpsHubStrings.CalendarFieldEndLabel);
            LiveOpsUtcDateTimeField startField = TimeFieldOf(LiveOpsHubStrings.CalendarFieldStartLabel);
            Assert.IsNotNull(endField, "inspector phải có ô giờ Kết thúc");
            Assert.IsNotNull(startField, "inspector phải có ô giờ Bắt đầu để so hàng sạch với hàng lỗi");
            Assert.IsTrue(endField.HasParseError, "lịch mẫu giữ giờ kết thúc hỏng cố ý \"2026-10-3\" — không còn thì ca này vô nghĩa");
            Assert.AreEqual(DisplayStyle.Flex, endField.ErrorIcon.resolvedStyle.display, "trạng thái hỏng phải hiện biểu tượng lỗi");

            Assert.IsTrue(endField.ErrorRow.Contains(endField.ErrorIcon),
                "biểu tượng lỗi thuộc DÒNG LỖI; để nó cuối hàng giá trị là dựng lại đúng chỗ tràn của W9-30");
            Assert.IsFalse(endField.ValueRow.Contains(endField.ErrorIcon), "hàng giá trị không bao giờ chứa biểu tượng lỗi");

            AssertVisibleInside(endField.ErrorIcon, body, "biểu tượng lỗi của ô giờ Kết thúc");
            AssertVisibleInside(endField.ErrorLabel, body, "dòng lỗi của ô giờ Kết thúc");
            AssertVisibleInside(endField.ZoneLabel, body, "nhãn UTC của ô giờ Kết thúc");
            AssertVisibleInside(endField.DateInput, body, "ô ngày của ô giờ Kết thúc");
            AssertVisibleInside(endField.TimeInput, body, "ô giờ của ô giờ Kết thúc");

            float valueRowRightEdge = endField.ZoneLabel.worldBound.xMax;
            Assert.LessOrEqual(valueRowRightEdge, endField.ValueRow.worldBound.xMax + BoundsTolerance,
                "phần tử cuối hàng giá trị không được thò ra ngoài chính hàng của nó");
            Assert.AreEqual(startField.ValueRow.worldBound.xMax, valueRowRightEdge, BoundsTolerance,
                "hàng giá trị phải rộng y hệt nhau ở trạng thái sạch và trạng thái lỗi — bề rộng đổi theo trạng thái là "
                + "cách chắc chắn nhất để một trong hai trạng thái tràn pane");

            AssertNoCutText(body, "inspector của đợt có giờ kết thúc không đọc được");
        }

        // ============================================================================================ W9-25 · chữ sát mép ô

        /// <summary>
        /// (W9-25 chỗ 1) Nhãn ô "Dời cả hai (giờ)" của pane chọn nhiều. Bản tiếng Anh "Shift both (hours)" cần 92px chữ
        /// trong vùng nội dung 93px của cột nhãn 96px — dư đúng 1px ở CẢ BẢY cỡ, tức "chưa cắt nhưng một đổi metric font
        /// là cắt", đúng thứ luật dư 5% của W9-25 dựng lên để bắt.
        /// <para>
        /// Cột nhãn 96px KHÔNG nới được: đo ở 1280×760, hàng ô giờ UTC đã dùng 161px trong 168px mà cột field còn lại, nới
        /// nhãn thêm 8px là đẩy nhãn "UTC" ra ngoài pane — tức chữa chỗ này bằng cách làm hỏng chỗ W9-30 vừa chữa. Cách
        /// chữa là cho NHÃN CỦA RIÊNG Ô NÀY xuống dòng: chữ không còn bị cắt theo bề rộng ở bất kỳ metric nào, mà hôm nay
        /// nó vẫn nằm một dòng (92 &lt; 93) nên hàng không đổi một pixel.
        /// </para>
        /// <para>
        /// (soát W10 F1) Cho nhãn wrap ĐƯA NÓ RA KHỎI luật dư 5%: <c>UxLayoutAuditor.CheckTextCut</c> chỉ tính
        /// <c>naturalWidth</c> khi chữ KHÔNG xuống dòng, còn <c>CheckTextFillRatio</c> thoát ngay khi số ấy là NaN. Đổi lại,
        /// chữ xuống dòng có một luật khác giữ chỗ cho nó: CHIỀU CAO. Ca này vì thế đo chiều cao ở CẢ HAI bản chữ và ở CẢ
        /// hai đầu bộ cỡ — 1280×760 (cỡ ảnh hành trình) và 700×560 (cỡ hẹp nhất của cổng) — rồi chốt thêm rằng nhãn nằm
        /// TRỌN trong hàng field của nó. Chữ hôm nay vẫn một dòng ở mọi cỡ vì cột nhãn 96px không đổi theo bề rộng cửa sổ;
        /// ca sẽ đỏ đúng lúc một metric font mới đẩy nó xuống hai dòng mà hàng không nở theo.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator W9_25_MultiSelectShiftLabel_WrapsInsteadOfBeingCut()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                for (int sizeIndex = 0; sizeIndex < MultiSelectLabelSizes.Length; sizeIndex++)
                {
                    UxWindowSize size = MultiSelectLabelSizes[sizeIndex];
                    string place = "pane chọn nhiều ở " + size.Width + "×" + size.Height + " (" + language + ")";
                    _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, size, language);
                    yield return _fixture.WaitForLayout();

                    _fixture.Calendar.Presenter.SetSelectedBarKeys(
                        new[] { LiveOpsDesignSample.HuntBonusEntryKey, LiveOpsDesignSample.HuntEarlyEntryKey },
                        LiveOpsDesignSample.HuntBonusEntryKey);
                    yield return _fixture.WaitForLayout();

                    IntegerField shiftField = _fixture.Root.Q<IntegerField>(className: LiveOpsHubClassNames.CalendarInspectorFieldNumber);
                    Assert.IsNotNull(shiftField, "pane chọn nhiều phải có ô \"Dời cả hai (giờ)\" — " + place);
                    Assert.AreEqual(LiveOpsHubStrings.TimelineMultiSelectShiftFieldLabel, shiftField.label,
                        "ô của pane chọn nhiều là ô dời giờ, không phải ô số nào khác — " + place);
                    Label labelElement = shiftField.labelElement;
                    Assert.AreEqual(WhiteSpace.Normal, labelElement.resolvedStyle.whiteSpace,
                        "nhãn ô này phải được xuống dòng: cột nhãn 96px chỉ dư 1px cho bản tiếng Anh, và cột ấy không nới "
                        + "được vì hàng ô giờ UTC đã dùng gần hết chỗ — " + place);

                    // Luật thay thế cho "dư 5% bề rộng" khi chữ đã wrap: chỗ cho MỌI dòng nó cần, và nằm trọn trong hàng.
                    float labelWidth = labelElement.contentRect.width;
                    Assert.Greater(labelWidth, 0f, "nhãn phải có bề rộng thật để đo chiều cao wrap — " + place);
                    float neededHeight = labelElement.MeasureTextSize(labelElement.text, labelWidth,
                        VisualElement.MeasureMode.Exactly, 0f, VisualElement.MeasureMode.Undefined).y;
                    Assert.GreaterOrEqual(labelElement.contentRect.height + TextMeasureTolerance, neededHeight,
                        "nhãn \"" + labelElement.text + "\" xuống dòng cần cao "
                        + neededHeight.ToString("0.#", CultureInfo.InvariantCulture) + "px, chỗ có "
                        + labelElement.contentRect.height.ToString("0.#", CultureInfo.InvariantCulture)
                        + "px — chữ wrap không còn bị cắt theo BỀ RỘNG, nên chiều cao là chỗ duy nhất nó cắt được — " + place);
                    Assert.GreaterOrEqual(shiftField.worldBound.height + TextMeasureTolerance, labelElement.worldBound.height,
                        "hàng field phải cao ít nhất bằng nhãn đã wrap của nó — " + place);

                    AssertNoCutText(_fixture.Root.Q(className: LiveOpsHubClassNames.Inspector), place);

                    _fixture.Dispose();
                    _fixture = null;
                }
            }
        }

        /// <summary>
        /// (soát W10 → G-W10-CAL2) Chọn NHIỀU đợt ở cửa sổ hẹp hơn <c>LiveOpsHubBreakpoints.MediumBelowWidth</c> = 1100px
        /// PHẢI mở được drawer inspector — y như chọn một đợt.
        /// <para>
        /// Lỗi đã chữa: <c>CalendarTimelinePresenter.SetSelectedBarKeys</c> cố ý chỉ phát <c>SelectionSetChanged</c>, mà
        /// người nghe duy nhất của sự kiện ấy là <c>CalendarEventInspector</c> — nó dựng lại NỘI DUNG pane, còn
        /// <c>CalendarSection.ApplyInspectorDrawerLayout</c> không chạy lại nên pane giữ nguyên
        /// <c>liveops-hub-calendar--hidden</c> (<c>display: none</c>). Người dùng ctrl-click hai thanh và không thấy gì
        /// hiện ra, ở BỐN trong bảy cỡ của cổng. Nay <c>CalendarSection</c> nghe thẳng <c>SelectionSetChanged</c>.
        /// </para>
        /// <para>
        /// Vì sao ca này đo cả BỐN cỡ chứ không một cỡ: đây đúng là hạng lỗi "lỗ hổng ma trận" của đợt W10 — bộ kiểm cũ chỉ
        /// dựng pane chọn nhiều ở cỡ RỘNG, nên một pane không hiện ra ở bốn cỡ hẹp vẫn xanh suốt chín đợt. Ca cũng chốt
        /// trạng thái ĐÓNG trước khi chọn: không có vế ấy thì một pane hiện SẴN ở mọi lúc cũng làm ca xanh, và lời khai
        /// "drawer mở khi chọn" không còn được chứng minh.
        /// </para>
        /// <para>
        /// (soát vòng 2 · CAL2-R2-01) Vế "pane mở ra là pane CHỌN NHIỀU" chốt bằng NHÃN của ô số và bằng CHỮ trên nút xoá,
        /// chứ không bằng riêng lớp <c>liveops-hub-calendar-inspector-field--number</c>: lớp ấy còn nằm trên ô "Dài" của
        /// pane MỘT ĐỢT (cả bản chỉ-đọc lẫn bản sửa được) và trên popover Thêm đợt, nên đúng cái hồi quy mà vế này hứa bắt
        /// — màn vẽ pane một đợt thay vì pane chọn nhiều — sẽ đi lọt nếu chỉ hỏi "có IntegerField mang lớp ấy không".
        /// </para>
        /// <para>
        /// (soát vòng 2 · CAL2-R2-03) Ca chạy trên HAI tài liệu: mẫu thiết kế và <see cref="LiveOpsWorstCaseSample"/>. Bản
        /// vá làm một pane 280px HIỆN RA ở bốn cỡ mà trước đây nó <c>display: none</c> — từ nay nó là pane người dùng thật
        /// sự đọc, mà pane chưa ai đo trên dữ liệu xấu nhất là pane chưa ai biết có cắt chữ hay không. Pane chọn nhiều dùng
        /// CÙNG lớp tiêu đề và CÙNG nút đóng trong CÙNG bề rộng 280px với pane một đợt — chỗ mà lượt audit đã đo được chữ
        /// bị cắt trên chính tài liệu ấy — nên <c>AssertNoCutText</c> chạy thẳng trên cây inspector đang mở.
        /// </para>
        /// <para>
        /// (soát vòng 2 · CAL2-R2-08) Vế cuối bơm Esc vào nhánh phím của gốc màn QUA <c>HandleRootKeyDownForTest</c>: nhánh
        /// XỬ LÝ là nhánh thật, nhưng ĐƯỜNG tới nó (dispatch theo focus của panel) thì bị bỏ qua, nên gỡ mất
        /// <c>RegisterCallback&lt;KeyDownEvent&gt;</c> ở gốc màn sẽ KHÔNG làm ca này đỏ. Lối tắt ấy có từ trước và có lý do
        /// ghi rõ (bẫy dispatch theo focus ở 2022.3); ghi ra đây để lời khai không rộng hơn phép đo. Điều ca này chốt được
        /// là: <c>IsInspectorDrawerOpen</c> đọc <c>SelectedBarKey</c> (khoá MỐC của tập), nên nếu tập chọn nhiều để khoá
        /// mốc rỗng thì drawer mở ra mà không phím nào đóng lại được.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator MultiSelectDrawer_AtEveryMediumWidth_OpensAndClosesWithEscape()
        {
            for (int documentIndex = 0; documentIndex < MultiSelectDocumentCount; documentIndex++)
            {
                for (int sizeIndex = 0; sizeIndex < MediumWidthSizes.Length; sizeIndex++)
                {
                    UxWindowSize size = MediumWidthSizes[sizeIndex];
                    string place = "cửa sổ " + size + " (bậc --medium, inspector là drawer) trên "
                        + MultiSelectDocumentLabel(documentIndex);
                    _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, size,
                        LiveOpsHubLanguageId.Vietnamese, MultiSelectServices(documentIndex));
                    yield return _fixture.WaitForLayout();

                    CalendarSection section = _fixture.Calendar;
                    VisualElement inspector = _fixture.Root.Q(className: LiveOpsHubClassNames.Inspector);
                    Assert.IsNotNull(inspector, "màn Lịch phải có cây inspector — " + place);
                    Assert.AreEqual(DisplayStyle.None, inspector.resolvedStyle.display,
                        "chưa chọn đợt nào thì drawer phải ĐÓNG — không có vế này thì một pane hiện sẵn cũng làm ca xanh — "
                        + place);

                    string[] barKeys = MultiSelectBarKeys(documentIndex);
                    section.Presenter.SetSelectedBarKeys(barKeys, barKeys[0]);
                    yield return _fixture.WaitForLayout();

                    Assert.AreEqual(2, section.Presenter.SelectedBarKeys.Count,
                        "ca này chỉ nói được điều nó khai khi tập chọn thật sự có hai đợt — " + place);
                    for (int keyIndex = 0; keyIndex < barKeys.Length; keyIndex++)
                    {
                        Assert.IsNotNull(section.Presenter.FindBar(barKeys[keyIndex]),
                            "đợt \"" + barKeys[keyIndex] + "\" phải là thanh CÓ THẬT trên trục — "
                            + "SetSelectedBarKeys nhận cả khoá không tồn tại, nên thiếu vế này thì ca vẫn xanh với hai "
                            + "khoá ma và tiêu đề pane chỉ ghép được câu \"0 loại\", tức đo một pane không phải pane "
                            + "người dùng gặp — " + place);
                    }
                    Assert.AreEqual(DisplayStyle.Flex, inspector.resolvedStyle.display,
                        "ctrl-click hai thanh ở cửa sổ hẹp phải MỞ drawer inspector; pane còn display:none nghĩa là "
                        + "ApplyInspectorDrawerLayout không chạy lại sau SelectionSetChanged — " + place);
                    Assert.Greater(inspector.contentRect.width, 0f,
                        "drawer mở phải có bề rộng thật để vẽ được nội dung — " + place);

                    IntegerField shiftField = inspector.Q<IntegerField>(className: LiveOpsHubClassNames.CalendarInspectorFieldNumber);
                    Assert.IsNotNull(shiftField,
                        "drawer mở ra phải là pane CHỌN NHIỀU (có ô \"Dời cả hai (giờ)\"), không phải pane một đợt — " + place);
                    Assert.AreEqual(LiveOpsHubStrings.TimelineMultiSelectShiftFieldLabel, shiftField.label,
                        "ô số của pane chọn nhiều là ô DỜI GIỜ; lớp \"--number\" còn nằm trên ô \"Dài\" của pane một đợt nên "
                        + "riêng nó không phân biệt được hai pane — " + place);
                    string deleteButtonText = LiveOpsHubStringCatalog.Format(
                        nameof(LiveOpsHubStrings.TimelineMultiSelectDeleteButtonFormat), barKeys.Length);
                    Assert.IsNotNull(FindButtonWithText(inspector, deleteButtonText),
                        "pane chọn nhiều phải có nút \"" + deleteButtonText + "\" — chỉ pane chọn nhiều dựng nút này, nên nó "
                        + "là dấu riêng thật sự của pane — " + place);
                    Assert.IsTrue(section.IsInspectorDrawerOpen, "drawer đang mở thì màn phải tự nhận là đang mở — " + place);

                    AssertNoCutText(inspector, "drawer chọn nhiều ở " + place);

                    using (KeyDownEvent escape = KeyDownEvent.GetPooled(EscapeCharacter, KeyCode.Escape, EventModifiers.None))
                    {
                        section.HandleRootKeyDownForTest(escape);
                    }
                    yield return _fixture.WaitForLayout();

                    Assert.IsFalse(section.IsInspectorDrawerOpen, "Esc phải đóng được drawer chọn nhiều — " + place);
                    Assert.AreEqual(DisplayStyle.None, inspector.resolvedStyle.display,
                        "đóng drawer = pane giấu hẳn, không để một pane rỗng 280px cạnh trục — " + place);

                    _fixture.Dispose();
                    _fixture = null;
                }
            }
        }

        /// <summary>
        /// (soát vòng 2 · CAL2-R2-07) Mốc <see cref="UxHubWindowFixture.MediumBreakpointWidth"/> phải là mốc THẬT mà hub
        /// đổi inspector Lịch từ pane cố định thành drawer.
        /// <para>
        /// Vì sao cần ca này: hằng của cổng từng chép tay 1000 trong khi sản phẩm dùng 1100 — 1000 lại trùng đúng
        /// <c>LiveOpsHubBreakpoints.SnugBelowWidth</c> nên rất dễ lệch lần nữa. Cho hằng đọc thẳng hằng sản phẩm là đúng
        /// nhưng CHƯA đủ: không ca nào đứng trên hằng ấy thì nó lệch lại cũng không ai đỏ, tức một hằng đúng mà chết. Ca
        /// này đo HÀNH VI ở hai bên mốc — hẹp hơn mốc thì gốc hub mang lớp <c>--medium</c> và inspector giấu hẳn khi chưa
        /// chọn đợt nào; rộng hơn mốc thì không lớp ấy và inspector vẫn hiện — nên mốc trôi 100px là ca đỏ ngay.
        /// </para>
        /// <para>
        /// <see cref="BreakpointProbeMargin"/> px mỗi bên lớn hơn mức kẹp cửa sổ mà cổng còn chấp nhận (4px) để một pixel
        /// lệch của hệ điều hành không lật kết luận, và nhỏ hơn nửa quãng 100px giữa hai mốc --snug và --medium để mốc trôi
        /// về 1000 vẫn bị bắt. Ca cũng dùng chính <c>UxWindowSize.IsMedium</c> — phép phân bậc của cổng — nên phép ấy
        /// không còn là mã chết.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator MediumBreakpoint_MatchesTheWidthWhereTheInspectorBecomesADrawer()
        {
            int breakpointWidth = (int)UxHubWindowFixture.MediumBreakpointWidth;
            UxWindowSize narrow = new UxWindowSize(breakpointWidth - BreakpointProbeMargin, BreakpointProbeHeight);
            UxWindowSize wide = new UxWindowSize(breakpointWidth + BreakpointProbeMargin, BreakpointProbeHeight);

            Assert.IsTrue(narrow.IsMedium, "cỡ dò hẹp phải nằm DƯỚI mốc theo chính phép phân bậc của cổng — " + narrow);
            Assert.IsFalse(wide.IsMedium, "cỡ dò rộng phải nằm TRÊN mốc theo chính phép phân bậc của cổng — " + wide);

            yield return AssertInspectorDrawerMode(narrow, true);
            yield return AssertInspectorDrawerMode(wide, false);
        }

        /// <summary>
        /// Mở màn Lịch ở <paramref name="size"/> mà KHÔNG chọn đợt nào, rồi chốt hai điều: gốc hub có/không lớp
        /// <c>--medium</c>, và inspector giấu hẳn hay vẫn hiện. Hai điều ấy là hai đầu của cùng một mốc, nên đo cả hai thì
        /// lệch giữa lớp và hành vi cũng không đi lọt.
        /// </summary>
        private IEnumerator AssertInspectorDrawerMode(UxWindowSize size, bool expectDrawer)
        {
            string place = "cửa sổ " + size + " (mốc --medium = "
                + UxHubWindowFixture.MediumBreakpointWidth.ToString("0", CultureInfo.InvariantCulture) + "px)";
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, size, LiveOpsHubLanguageId.Vietnamese);
            yield return _fixture.WaitForLayout();
            Assert.IsFalse(_fixture.IsClamped,
                "cỡ dò bị hệ điều hành kẹp thì phép đo không còn của cỡ ấy — " + _fixture.ClampNote);

            VisualElement hubRoot = _fixture.Root.Q(className: LiveOpsHubClassNames.Root);
            Assert.IsNotNull(hubRoot, "cửa sổ hub phải có gốc mang lớp khung — " + place);
            Assert.AreEqual(expectDrawer, hubRoot.ClassListContains(LiveOpsHubClassNames.Medium),
                "lớp --medium của gốc hub phải khớp mốc mà cổng đang khai; lệch nghĩa là hằng của cổng không còn là mốc "
                + "thật của sản phẩm — " + place);

            VisualElement inspector = _fixture.Root.Q(className: LiveOpsHubClassNames.Inspector);
            Assert.IsNotNull(inspector, "màn Lịch phải có cây inspector — " + place);
            Assert.AreEqual(expectDrawer ? DisplayStyle.None : DisplayStyle.Flex, inspector.resolvedStyle.display,
                expectDrawer
                    ? "dưới mốc inspector là DRAWER: chưa chọn đợt nào thì phải giấu hẳn — " + place
                    : "trên mốc inspector là PANE cố định: chưa chọn đợt nào vẫn phải hiện — " + place);

            _fixture.Dispose();
            _fixture = null;
        }

        /// <summary>Nhãn tài liệu của vòng lặp ca drawer chọn nhiều — vào thẳng câu assert nên đọc được ngay trong log.</summary>
        private static string MultiSelectDocumentLabel(int documentIndex)
        {
            return documentIndex == 0 ? "mẫu thiết kế" : "tài liệu XẤU NHẤT";
        }

        /// <summary>
        /// Services của vòng lặp: chỉ số 0 = mẫu thiết kế (trả null để fixture tự dựng), chỉ số 1 = tài liệu xấu nhất.
        /// Lượt kiểm chạy tới cùng để màn có phát hiện thật, giống hệt cách lượt audit dựng services xấu nhất.
        /// </summary>
        private static LiveOpsHubServices MultiSelectServices(int documentIndex)
        {
            if (documentIndex == 0) return null;
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsWorstCaseSample.Document)));
            services.Session.RunCheckToCompletion();
            return services;
        }

        /// <summary>
        /// Hai đợt được chọn của mỗi tài liệu. Bản xấu nhất lấy đợt có id DÀI NHẤT ghép với đợt trỏ tới loại CHƯA KHAI: hai
        /// đợt khác loại nên tiêu đề pane phải ghép câu "2 loại" thay vì một tên loại, tức nhánh ghép chữ của pane này.
        /// </summary>
        private static string[] MultiSelectBarKeys(int documentIndex)
        {
            if (documentIndex == 0)
            {
                return new[] { LiveOpsDesignSample.HuntBonusEntryKey, LiveOpsDesignSample.HuntEarlyEntryKey };
            }
            return new[] { LiveOpsWorstCaseSample.LongEntryKey, LiveOpsWorstCaseSample.UndeclaredEntryKey };
        }

        // ============================================================================================ hạ tầng test

        private LiveOpsHubServices Services()
        {
            return Section().Services;
        }

        private CalendarSection Section()
        {
            IReadOnlyList<IHubSection> sections = ((IHubHost)_window).Sections;
            for (int index = 0; index < sections.Count; index++)
            {
                CalendarSection calendar = sections[index] as CalendarSection;
                if (calendar != null) return calendar;
            }
            Assert.Fail("cửa sổ hub không có màn Lịch");
            return null;
        }

        private VisualElement SectionView()
        {
            return _window.SectionBody == null || _window.SectionBody.childCount == 0 ? null : _window.SectionBody[0];
        }

        private ScrollView InspectorBody()
        {
            ScrollView body = (ScrollView)SectionView().Q(LiveOpsHubPaths.CalendarElementNames.InspectorBody);
            Assert.IsNotNull(body, "màn Lịch phải có thân inspector");
            return body;
        }

        private LiveOpsUtcDateTimeField TimeFieldOf(string label)
        {
            List<LiveOpsUtcDateTimeField> fields = new List<LiveOpsUtcDateTimeField>();
            InspectorBody().Query<LiveOpsUtcDateTimeField>().ToList(fields);
            for (int index = 0; index < fields.Count; index++)
            {
                if (string.Equals(fields[index].label, label, StringComparison.Ordinal)) return fields[index];
            }
            return null;
        }

        /// <summary>
        /// Ô số DÙNG ĐƯỢC gắn tạm vào cửa sổ hub làm ĐỐI CHỨNG DƯƠNG cho cú kéo nhãn. Đặt tuyệt đối ở mép dưới-trái để không
        /// bóp bố cục của hub và không nằm chồng lên pane inspector bên phải — cùng cách <see cref="AddFocusTarget"/> gắn ô chữ
        /// tạm vào root.
        /// </summary>
        private IntegerField AddDragProbe()
        {
            IntegerField probe = new IntegerField("probe") { value = DragProbeStartValue };
            probe.style.position = Position.Absolute;
            probe.style.left = 8f;
            probe.style.bottom = 8f;
            probe.style.width = 200f;
            _window.rootVisualElement.Add(probe);
            return probe;
        }

        /// <summary>
        /// Kéo NGANG trên nhãn của một ô số — đúng cử chỉ của dragger <c>IntegerField</c> (vùng kéo là nhãn, không phải ô nhập).
        /// </summary>
        private IEnumerator DragFieldLabel(IntegerField field)
        {
            Vector2 from = field.labelElement.worldBound.center;
            yield return UxEventSender.Drag(_window, from, from + new Vector2(DragDistancePixels, 0f), DragSteps,
                EventModifiers.None);
        }

        private TextField AddFocusTarget()
        {
            TextField outside = new TextField();
            _window.rootVisualElement.Add(outside);
            return outside;
        }

        private IEnumerator OpenCalendar()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            _window = LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Calendar);
            _window.position = new Rect(0, 0, WindowWidth, WindowHeight);
            yield return WaitForLayout(_window.rootVisualElement);
        }

        private IEnumerator OpenCalendarWithSelection(string barKey)
        {
            yield return OpenCalendar();
            Section().Presenter.SetSelectedBarKey(barKey);
            yield return null;
            yield return WaitForLayout(InspectorBody());
        }

        private VisualElement OpenPopoverInPanel(AddEventPopover popover)
        {
            _panel = ControlsTestPanel.Open();
            VisualElement host = _panel.CreateRoot(false);
            VisualElement root = popover.BuildForTest();
            // Popover thật là cửa sổ 320×236; trong panel test phải ép đúng bề rộng đó, kẻo chữ "không bị cắt" chỉ vì
            // panel rộng 900. (style inline: chỉ ở test, không phải code của hub.)
            root.style.width = AddEventPopover.PopoverWidth;
            root.style.height = AddEventPopover.PopoverHeight;
            root.style.flexGrow = 0f;
            root.style.flexShrink = 0f;
            host.Add(root);
            return root;
        }

        private static AddEventFlowModel ReviewFlow(LiveOpsHubServices services, string startDate)
        {
            return AddEventFlowModel.Create(services.Session, services.Clock.UtcNow)
                .WithType("treasure-hunt")
                .Next()
                .WithTimes(startDate, "00:00", 72)
                .Next();
        }

        /// <summary>
        /// Không TextElement nào ĐANG HIỆN trong cây cần nhiều chỗ hơn phần chữ thật sự được vẽ.
        /// <para>
        /// Chữ MỘT DÒNG đo theo bề rộng; chữ ĐÃ CHO xuống dòng (<c>white-space: normal</c>) đo theo CHIỀU CAO ở đúng bề rộng
        /// đang có. Bản trước bỏ qua hẳn nhánh xuống dòng, mà chính cách sửa UX-09 là gắn <c>white-space: normal</c>, nên test
        /// được thoả nhờ CƠ CHẾ của bản sửa chứ không phải nhờ chữ hiện đủ, và nút cao cứng 18px nuốt dòng thứ hai vẫn xanh.
        /// </para>
        /// <para>
        /// Phép lọc đi theo CHUỖI CHA chứ không chỉ đọc <c>display</c> của chính element (<see cref="IsInsideShownSubtree"/>):
        /// thanh công cụ luôn giữ cả bản đầy đủ lẫn bản rút gọn trong cây và chỉ đổi class, nên chữ của khối đang ẩn vẫn bị đo,
        /// với bề rộng 0, và mọi lần đo thanh đều đỏ vì một nhãn KHÔNG AI THẤY. Phần tử bị bóp còn 0px mà cha vẫn hiện thì
        /// KHÔNG bỏ qua — đó đúng là thứ phép đo này đi tìm.
        /// </para>
        /// <para>
        /// (R-09 lượt soát 2) MỘT luật đo chữ duy nhất cho mọi chỗ gọi. Trước đó thanh công cụ dùng một bản sao riêng đánh rơi
        /// nhánh xuống dòng: hai luật lệch nhau, và bản sao ấy vừa đỏ giả vừa không bắt được dòng bị cắt ở cây có nhãn wrap.
        /// </para>
        /// </summary>
        private static void AssertNoCutText(VisualElement root, string place)
        {
            Assert.IsNotNull(root, "không tìm thấy cây để đo: " + place);
            List<TextElement> texts = new List<TextElement>();
            root.Query<TextElement>().ToList(texts);
            int measuredCount = 0;
            for (int index = 0; index < texts.Count; index++)
            {
                TextElement text = texts[index];
                if (string.IsNullOrEmpty(text.text)) continue;
                if (!IsInsideShownSubtree(text)) continue;
                if (float.IsNaN(text.layout.width) || float.IsNaN(text.layout.height)) continue;
                measuredCount++;
                if (text.resolvedStyle.whiteSpace == WhiteSpace.Normal)
                {
                    float width = text.contentRect.width;
                    if (width <= 0f) continue;
                    float neededHeight = text.MeasureTextSize(text.text, width, VisualElement.MeasureMode.Exactly,
                        0f, VisualElement.MeasureMode.Undefined).y;
                    Assert.GreaterOrEqual(text.contentRect.height + TextMeasureTolerance, neededHeight,
                        place + ": chữ \"" + text.text + "\" xuống dòng cần cao "
                        + neededHeight.ToString("0.#", CultureInfo.InvariantCulture) + "px, chỗ có "
                        + text.contentRect.height.ToString("0.#", CultureInfo.InvariantCulture)
                        + "px — dòng sau bị cắt ngang");
                    continue;
                }
                float needed = text.MeasureTextSize(text.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;
                float available = text.contentRect.width;
                Assert.GreaterOrEqual(available + TextMeasureTolerance, needed,
                    place + ": chữ \"" + text.text + "\" cần " + needed.ToString("0.#", CultureInfo.InvariantCulture)
                    + "px, chỗ có " + available.ToString("0.#", CultureInfo.InvariantCulture) + "px — người dùng đọc được nửa câu");
            }

            Assert.Greater(measuredCount, 0, place + ": không đo được nhãn nào — phép lọc hỏng thì ca này thành lời khai suông");
        }

        /// <summary>
        /// Phần tử ĐANG được bày ra (không <c>display: none</c>, không <c>visible: false</c>) mà rộng hoặc cao 0 là phần tử
        /// người dùng KHÔNG nhìn thấy — đúng dạng hồi quy "vùng nhập ô giờ UTC rộng 0" đã lọt qua cả bộ test vì mọi phép đo
        /// đều bỏ qua phần tử rộng 0. Chỉ soi phần tử MANG CLASS CỦA HUB hoặc có tên: khung rỗng do bố cục không phải lỗi.
        /// </summary>
        private static void AssertNoZeroSize(VisualElement root, string place)
        {
            Assert.IsNotNull(root, "không tìm thấy cây để đo: " + place);
            List<VisualElement> elements = new List<VisualElement>();
            root.Query<VisualElement>().ToList(elements);
            for (int index = 0; index < elements.Count; index++)
            {
                VisualElement element = elements[index];
                if (!IsDisplayed(element) || !IsNamedOrClassed(element)) continue;
                if (element.childCount == 0)
                {
                    TextElement text = element as TextElement;
                    if (text == null || string.IsNullOrEmpty(text.text)) continue;
                }
                Assert.Greater(element.layout.width, 0f,
                    place + ": '" + DescribeElement(element) + "' rộng 0 — đang bày ra mà không vẽ gì");
                Assert.Greater(element.layout.height, 0f,
                    place + ": '" + DescribeElement(element) + "' cao 0 — đang bày ra mà không vẽ gì");
            }
        }

        /// <summary>Phần tử của hub (class "liveops-hub-…") hoặc phần tử có tên — thứ mà bản dựng cố ý tạo ra để người dùng thấy.</summary>
        private static bool IsNamedOrClassed(VisualElement element)
        {
            if (!string.IsNullOrEmpty(element.name)) return true;
            foreach (string className in element.GetClasses())
            {
                if (className.StartsWith(HubClassPrefix, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        /// <summary>
        /// Không con nào của <paramref name="root"/> thò ra ngoài khung của chính nó. Đo CẢ BỐN mép: bản trước chỉ so mép
        /// phải, nên con tràn sang trái (lề âm) hay tụt xuống dưới đáy khung không cuộn được đều lọt.
        /// </summary>
        private static void AssertInsideParent(VisualElement root, string place)
        {
            Rect frame = root.worldBound;
            List<VisualElement> children = new List<VisualElement>();
            root.Query<VisualElement>().ToList(children);
            for (int index = 0; index < children.Count; index++)
            {
                VisualElement child = children[index];
                if (!IsVisible(child)) continue;
                string who = place + ": '" + DescribeElement(child) + "' tràn qua mép ";
                Assert.LessOrEqual(child.worldBound.xMax, frame.xMax + BoundsTolerance,
                    who + "phải — nội dung rộng hơn khung mà không cuộn được");
                Assert.GreaterOrEqual(child.worldBound.xMin, frame.xMin - BoundsTolerance,
                    who + "trái — nội dung rộng hơn khung mà không cuộn được");
                if (IsScrollable(root)) continue;
                Assert.LessOrEqual(child.worldBound.yMax, frame.yMax + BoundsTolerance,
                    who + "dưới — nội dung cao hơn khung mà không cuộn được");
            }
        }

        /// <summary>Khung CÓ cuộn được thì nội dung cao hơn khung là bình thường — chỉ mép ngang mới là lỗi.</summary>
        private static bool IsScrollable(VisualElement element)
        {
            VisualElement walk = element;
            while (walk != null)
            {
                if (walk is ScrollView) return true;
                walk = walk.parent;
            }
            return false;
        }

        private static void AssertVisibleInside(VisualElement element, VisualElement container, string place)
        {
            string where = " [" + place + " " + element.worldBound.ToString() + " trong " + container.worldBound.ToString() + "]";
            Assert.IsTrue(IsVisible(element), place + " phải nhìn thấy được" + where);
            Assert.Greater(element.layout.width, 0f, place + " rộng 0 — biến mất khỏi màn hình" + where);
            Assert.Greater(element.layout.height, 0f, place + " cao 0 — biến mất khỏi màn hình" + where);
            Assert.LessOrEqual(element.worldBound.xMax, container.worldBound.xMax + BoundsTolerance,
                place + " nằm ngoài pane — trên máy người dùng nó biến mất" + where);
        }

        /// <summary>Chính nó và MỌI tổ tiên đều đang chiếm chỗ (không display:none, không visibility:hidden). Không xét kích thước.</summary>
        private static bool IsInsideShownSubtree(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.hierarchy.parent)
            {
                if (current.panel == null) return false;
                if (current.resolvedStyle.display == DisplayStyle.None) return false;
                if (current.resolvedStyle.visibility == Visibility.Hidden) return false;
            }
            return true;
        }

        /// <summary>
        /// Đang CHIẾM CHỖ thật: có panel, không display:none, không visible:false, layout đã đo. KHÔNG đòi rộng &gt; 0 —
        /// phần tử rộng 0 chính là lỗi mà <see cref="AssertNoZeroSize"/> phải bắt, lọc nó ở đây là tự bịt mắt.
        /// </summary>
        private static bool IsDisplayed(VisualElement element)
        {
            return element.panel != null && element.resolvedStyle.display == DisplayStyle.Flex && element.visible
                   && !float.IsNaN(element.layout.width) && !float.IsNaN(element.layout.height);
        }

        /// <summary>Đang chiếm chỗ VÀ có bề rộng — dùng cho phép so mép, nơi phần tử rộng 0 không nói lên điều gì.</summary>
        private static bool IsVisible(VisualElement element)
        {
            return IsDisplayed(element) && element.layout.width > 0f;
        }

        private static string DescribeElement(VisualElement element)
        {
            TextElement text = element as TextElement;
            string label = text == null || string.IsNullOrEmpty(text.text) ? string.Empty : " \"" + text.text + "\"";
            return element.GetType().Name + (string.IsNullOrEmpty(element.name) ? string.Empty : "#" + element.name) + label;
        }

        private static Label FindLabelWithText(VisualElement root, string text)
        {
            List<Label> labels = new List<Label>();
            root.Query<Label>().ToList(labels);
            for (int index = 0; index < labels.Count; index++)
            {
                if (string.Equals(labels[index].text, text, StringComparison.Ordinal)) return labels[index];
            }
            return null;
        }

        private static Button FindButtonWithText(VisualElement root, string text)
        {
            List<Button> buttons = new List<Button>();
            root.Query<Button>().ToList(buttons);
            for (int index = 0; index < buttons.Count; index++)
            {
                if (string.Equals(buttons[index].text, text, StringComparison.Ordinal)) return buttons[index];
            }
            return null;
        }

        /// <summary>
        /// Gửi một phím qua ĐÚNG đường dispatch của panel (trickle-down → bubble-up trên cây thật), không gọi thẳng hàm xử lý
        /// phím của popover: lỗi "phím không tới được gốc popover" chỉ lộ ra trên đường này. Sự kiện pool bọc trong using để
        /// trả lại đúng chỗ.
        /// </summary>
        private static void SendKeyDown(VisualElement target, char character, KeyCode keyCode)
        {
            using (KeyDownEvent keyDownEvent = KeyDownEvent.GetPooled(character, keyCode, EventModifiers.None))
            {
                keyDownEvent.target = target;
                target.SendEvent(keyDownEvent);
            }
        }

        /// <summary>Gõ chữ như bàn phím: <c>ITextEdition.UpdateText</c> đổi chữ và bắn InputEvent, chưa đổi value.</summary>
        private static void TypeText(TextField textField, string text)
        {
            MethodInfo updateText = typeof(ITextEdition).GetMethod("UpdateText",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(updateText, "ITextEdition.UpdateText không còn — đường gõ phím của TextField đã đổi");
            updateText.Invoke(textField.textEdition, new object[] { text });
            Assert.AreEqual(text, textField.text);
        }

        /// <summary>Chuyển focus thật qua FocusController — ô isDelayed chốt chữ trên đúng đường này (Tab / bấm ra ngoài).</summary>
        private static IEnumerator MoveFocus(TextField target)
        {
            int focusInCount = 0;
            EventCallback<FocusInEvent> onFocusIn = focusInEvent => focusInCount++;
            target.RegisterCallback(onFocusIn);
            target.Focus();
            yield return null;
            target.UnregisterCallback(onFocusIn);
            Assert.Greater(focusInCount, 0, "focus phải vào ô đích — không có FocusInEvent thì test không đi đường chốt chữ thật");
        }

        /// <summary>(V-23) Chỉ fail khi quá CẢ 60 khung LẪN 5 giây — máy chậm không được biến test UI thành đỏ giả.</summary>
        private static IEnumerator WaitForLayout(params VisualElement[] elements)
        {
            int frames = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!AllHaveLayout(elements))
            {
                frames++;
                if (frames > MaximumLayoutFrames && stopwatch.ElapsedMilliseconds > MaximumLayoutMilliseconds)
                {
                    Assert.Fail("cây không có layout sau 60 khung và 5 giây — test UI phải chạy không -nographics");
                }
                yield return null;
            }
            yield return null;
            yield return null;
        }

        private static bool AllHaveLayout(VisualElement[] elements)
        {
            for (int index = 0; index < elements.Length; index++)
            {
                if (!LiveOpsHubWindowTestScope.HasLayout(elements[index])) return false;
            }
            return true;
        }
    }
}
