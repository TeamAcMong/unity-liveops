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

        /// <summary>Sai số đo chữ: <c>MeasureTextSize</c> làm tròn khác trình bày một chút ở cả hai bản Unity.</summary>
        private const float TextMeasureTolerance = 1.5f;

        /// <summary>Sai số hình học khi so mép con với mép cha (bo/viền nửa pixel).</summary>
        private const float BoundsTolerance = 0.75f;

        private LiveOpsHubWindow _window;
        private ControlsTestPanel _panel;

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
            string expected = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarResizeUndoStepFormat,
                "hunt-0916-bonus");
            Assert.AreEqual(expected, toasts[0].UndoGroupName,
                "bước Undo của sửa trong inspector phải là câu ngắn như đường kéo — \"\" làm toast Hoàn tác lặp chữ (UJ-10)");
            Assert.AreNotEqual(toasts[0].Message, toasts[0].UndoGroupName, "toast giữ câu dài, Undo History không");
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
            popover.OnKeyDown(KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None));
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
            LiveOpsPlaceholder hint = root.Q<LiveOpsPlaceholder>();
            Assert.IsNotNull(hint, "ô Config key rỗng phải có chữ dẫn");
            Assert.IsFalse(hint.ClassListContains(LiveOpsHubClassNames.Mono),
                "chữ dẫn là chữ nghiêng cỡ nhỏ, không phải mono cỡ thường (V17)");
            AssertNoCutText(root.Q(LiveOpsHubPaths.AddEventPopoverElementNames.StepReview), "bước 3 popover");
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

        /// <summary>Không TextElement nào trong cây cần rộng hơn phần chữ thật sự được vẽ.</summary>
        private static void AssertNoCutText(VisualElement root, string place)
        {
            Assert.IsNotNull(root, "không tìm thấy cây để đo: " + place);
            List<TextElement> texts = new List<TextElement>();
            root.Query<TextElement>().ToList(texts);
            for (int index = 0; index < texts.Count; index++)
            {
                TextElement text = texts[index];
                if (!IsVisible(text) || string.IsNullOrEmpty(text.text)) continue;
                if (text.resolvedStyle.whiteSpace == WhiteSpace.Normal) continue;
                float needed = text.MeasureTextSize(text.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;
                float available = text.contentRect.width;
                Assert.GreaterOrEqual(available + TextMeasureTolerance, needed,
                    place + ": chữ \"" + text.text + "\" cần " + needed.ToString("0.#", CultureInfo.InvariantCulture)
                    + "px, chỗ có " + available.ToString("0.#", CultureInfo.InvariantCulture) + "px — người dùng đọc được nửa câu");
            }
        }

        /// <summary>Không con nào của <paramref name="root"/> thò ra ngoài mép phải của chính nó.</summary>
        private static void AssertInsideParent(VisualElement root, string place)
        {
            float limit = root.worldBound.xMax + BoundsTolerance;
            List<VisualElement> children = new List<VisualElement>();
            root.Query<VisualElement>().ToList(children);
            for (int index = 0; index < children.Count; index++)
            {
                VisualElement child = children[index];
                if (!IsVisible(child)) continue;
                Assert.LessOrEqual(child.worldBound.xMax, limit,
                    place + ": '" + DescribeElement(child) + "' tràn qua mép phải — nội dung rộng hơn khung mà không cuộn được");
            }
        }

        private static void AssertVisibleInside(VisualElement element, VisualElement container, string place)
        {
            Assert.IsTrue(IsVisible(element), place + " phải nhìn thấy được");
            Assert.Greater(element.layout.width, 0f, place + " rộng 0 — biến mất khỏi màn hình");
            Assert.LessOrEqual(element.worldBound.xMax, container.worldBound.xMax + BoundsTolerance,
                place + " nằm ngoài pane — trên máy người dùng nó biến mất");
        }

        private static bool IsVisible(VisualElement element)
        {
            return element.panel != null && element.resolvedStyle.display == DisplayStyle.Flex && element.visible
                   && !float.IsNaN(element.layout.width) && element.layout.width > 0f;
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
