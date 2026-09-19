using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Hành trình màn Luật lặp của cổng W8-UX (§3.2) — cùng luật với hành trình Lịch: chỉ chuột và phím thật qua cửa sổ hub,
    /// assert bằng cách ĐỌC trạng thái, và chạy ở CẢ vi lẫn en.
    /// <para>
    /// Ba lỗi khoá ở đây đều là lỗi chỉ thấy khi gõ bằng tay: "Chạy mỗi đợt" lớn hơn chu kỳ mà nút ghi vẫn bật (UX-22), nháp tiền
    /// tố khoá form nhưng bấm ô khoá không đưa được người dùng tới nút thoát (UX-23), và sửa trường xong toast không nói trường
    /// nào đổi thành gì (UX-26).
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    [Category(LiveOpsHubTestCategories.UxGate)]
    public sealed class UxRecurringJourneyTests
    {
        private static readonly DateTime DesignNowUtc = new DateTime(2026, 9, 13, 1, 47, 0, DateTimeKind.Utc);

        /// <summary>Chu kỳ mẫu thiết kế là 168 giờ; 200 giờ "chạy mỗi đợt" là trạng thái sai của [SD1 §4.3].</summary>
        private const string ActiveHoursLongerThanPeriod = "200";

        /// <summary>
        /// Tiền tố MỚI của lượt UX-23. Bản đầu tiên gõ "pass-" — đúng bằng tiền tố ĐANG GHI của luật mẫu
        /// (<c>LiveOpsDesignSample.cs:184</c>: <c>new RecurringLiveEventRule("weekly-pass", …, "pass-", 168, 168, …)</c>), nên
        /// <c>RecurringRulesSection.IsSameAsWritten</c> bỏ qua và không nháp nào được dựng. Gõ lại đúng chữ cũ là "không sửa
        /// gì", không phải "nháp không bật" — muốn kiểm nháp thì phải gõ một giá trị thật sự khác.
        /// </summary>
        private const string NewPrefixDraft = "pass-v2-";

        /// <summary>
        /// Chu kỳ mới của lượt UX-26 — toast phải nói CẢ tên trường lẫn giá trị này. CHỌN 180 chứ không phải 72 vì
        /// <c>RecurringRuleModel.PeriodText</c> đổi bội số của 24 giờ sang ngày ("72" in ra "3 ngày"): số giờ tròn ngày làm
        /// câu assert đi tìm chuỗi mà toast không bao giờ in. 180 giờ không tròn ngày nên toast in đúng con số người dùng gõ,
        /// và test đọc CHỮ CỦA SẢN PHẨM chứ không tự dựng chuỗi mong đợi bằng chính hàm format của sản phẩm.
        /// <para>180 &gt; 168 giờ "chạy mỗi đợt" của luật mẫu nên luật mới vẫn hợp lệ — nháp ghi được, khác hẳn ca UX-22.</para>
        /// </summary>
        private const int NewPeriodHours = 180;

        /// <summary>
        /// Chu kỳ mới của lượt "chu kỳ tròn ngày" (R-06). Từ khi UX-26 bỏ 72 để lấy 180, KHÔNG còn hành trình nào đi qua nhánh
        /// quy đổi bội số 24 giờ của <c>RecurringRuleModel.PeriodText</c> — mà đó chính là nhánh làm hỏng UJ-22 (toast in một
        /// đơn vị, ô in một đơn vị khác). 192 giờ = 8 ngày: tròn ngày và vẫn lớn hơn 168 giờ "chạy mỗi đợt" của luật mẫu nên
        /// luật mới hợp lệ, nháp ghi được.
        /// </summary>
        private const int WholeDayPeriodHours = 192;

        /// <summary>Số lượt gõ lại tối đa của <see cref="TypeIntoFieldUntilItHolds"/> trước khi kết luận ô thật sự không nhận phím.</summary>
        private const int MaximumTypeAttempts = 3;

        /// <summary>Hạn chờ một lượt gõ: chỉ bỏ cuộc khi quá CẢ số khung lẫn số mili giây (cùng luật V-23 với <c>UxEventSender.WaitUntil</c>).</summary>
        private const int WaitFramesPerTypeAttempt = 30;

        private const int WaitMillisecondsPerTypeAttempt = 1500;

        private static readonly string NewPeriodHoursText = NewPeriodHours.ToString(CultureInfo.InvariantCulture);

        private static readonly string WholeDayPeriodHoursText = WholeDayPeriodHours.ToString(CultureInfo.InvariantCulture);

        private UxHubWindowFixture _fixture;

        /// <summary>Hộp xác nhận giả của phiên đang mở — test đọc lại để biết màn CÓ hỏi thật hay ghi thẳng (R-04).</summary>
        private ScriptedLiveOpsHubConfirmationPresenter _confirmations;

        [TearDown]
        public void TearDown()
        {
            DisposeFixture();
            UxHubWindowFixture.CloseStrayWindows();
        }

        /// <summary>
        /// UX-22: gõ "Chạy mỗi đợt" lớn hơn chu kỳ thì chỉ được báo lỗi TẠI trường đó; nút ghi phải khoá lại chứ không cho ghi
        /// một luật tự mâu thuẫn.
        /// <para>
        /// Assert chặt hơn bản đầu tiên ở ba chỗ (R-11): (1) đọc trạng thái lỗi TRƯỚC khi gõ để lỗi có sẵn ở trường khác của mẫu
        /// thiết kế không làm test xanh nhầm; (2) lỗi phải nằm ở CỤM của chính trường vừa gõ, không phải "ở đâu đó trong cửa
        /// sổ"; (3) lời mời ghi không được hiện ra.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator ActiveHoursLongerThanPeriod_ShowsFieldError_AndBlocksWrite()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenRecurring(UxHubWindowFixture.AllSizes[4], language);
                RecurringRuleForm form = _fixture.Recurring.Form;
                Assert.IsFalse(HasVisibleErrorOnActiveField(form),
                    "trường 'Chạy mỗi đợt' đã báo lỗi TRƯỚC khi test gõ gì — lượt này không nói được gì về UX-22");

                yield return TypeIntoFieldUntilItHolds(form.ActiveField, ActiveHoursLongerThanPeriod, "chạy mỗi đợt");
                yield return UxEventSender.PressTab(_fixture.Window);

                Assert.IsTrue(HasVisibleErrorOnActiveField(form),
                    "gõ 'Chạy mỗi đợt' dài hơn chu kỳ mà hàng của trường đó không có câu lỗi nào (UX-22)");
                Assert.IsTrue(form.ActiveField.ClassListContains(LiveOpsHubClassNames.RecurringFieldInvalid),
                    "ô đang giữ giá trị hỏng mà không mang viền chặn — câu lỗi ở dưới không chỉ được vào ô nào (UX-22)");
                // Hợp đồng UJ-15 của màn là GỠ HẲN lời mời, không phải làm nó xám (RecurringRuleForm.cs:294-300, gác sẵn ở
                // RecurringUxFixTests.RunLongerThanPeriod_ShowsFieldErrorOnly_NoWriteButton). Câu này hỏi ĐÚNG cái hợp đồng
                // nói: người dùng KHÔNG được thấy lời mời ghi. Không đòi nút phải nằm trong cây: bản dựng hiện giấu nút bằng
                // class, một bản dựng đúng hơn có thể gỡ hẳn khối nháp khi giá trị hỏng (UX-FIX-PLAN.md:58 "Field lỗi ⇒ không
                // dựng nháp/nút ghi") — lúc đó Q trả null và đòi IsNotNull sẽ bắt đền một bản sửa ĐÚNG (R-03).
                VisualElement writeButton = _fixture.Root.Q(RecurringRuleForm.DraftWriteElementName);
                Assert.IsFalse(writeButton != null && UxLayoutAuditor.IsShownOnScreen(writeButton),
                    "luật đang sai mà nút ghi vẫn mời bấm — người dùng ghi được một luật tự mâu thuẫn (UX-22)");
                // Lối ra thì NGƯỢC LẠI: nó phải có thật. Gỡ lời mời ghi mà không để lại đường thoát là bỏ người dùng lại với
                // một ô hỏng và ba ô khoá.
                VisualElement cancelButton = _fixture.Root.Q(RecurringRuleForm.DraftCancelElementName);
                Assert.IsNotNull(cancelButton, "gỡ nút ghi mà không để lại lối ra nào (UX-22)");
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(cancelButton), "lối ra 'Huỷ (Esc)' không hiện ra (UX-22)");
                Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, cancelButton),
                    "con trỏ không chạm được lối ra 'Huỷ (Esc)' — người dùng kẹt lại với giá trị hỏng (UX-22)");
                DisposeFixture();
            }
        }

        /// <summary>
        /// UX-23: khi có nháp tiền tố, form khoá lại và chỉ ĐƯỢC hiện MỘT câu giải thích; bấm vào ô đang khoá phải đưa focus tới
        /// nút Huỷ — nếu không, người dùng bấm mãi vào ô chết mà không biết lối ra.
        /// </summary>
        [UnityTest]
        public IEnumerator PrefixDraft_LocksFormWithOneSentence_AndClickOnLockedFieldFocusesCancel()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenRecurring(UxHubWindowFixture.AllSizes[4], language);
                RecurringRuleForm form = _fixture.Recurring.Form;

                yield return TypeIntoFieldUntilItHolds(form.PrefixField, NewPrefixDraft, "tiền tố id");
                yield return UxEventSender.PressTab(_fixture.Window);
                yield return UxEventSender.WaitUntil(() => UxLayoutAuditor.IsShownOnScreen(form.DraftBlock),
                    "gõ tiền tố mới không bật khối nháp nào (UX-23)");

                // [SD1 §4.2] đòi khối nháp hợp lệ hiện ĐỦ HAI phần: dòng "nháp chỉ ở ô này, Main.asset vẫn thấy giá trị cũ"
                // và HelpBox hậu quả. Từ khi CountVisibleLockSentences đổi sang đếm câu LÝ DO KHOÁ, không còn câu nào đòi hai
                // phần này hiện ra ở một cảnh nháp hợp lệ — phần hiển thị của BindDraftBlock mất người gác (R-05).
                AssertShown(RecurringRuleForm.DraftNoticeElementName,
                    "khối nháp không nói 'nháp chỉ nằm ở ô này, Main.asset vẫn giữ giá trị cũ' (UX-23)");
                AssertShown(RecurringRuleForm.DraftHelpBoxElementName,
                    "khối nháp không nói hậu quả của việc ghi tiền tố mới (UX-23)");

                int lockSentences = CountVisibleLockSentences();
                Assert.AreEqual(1, lockSentences,
                    "khối nháp hiện " + lockSentences + " câu khoá — [SD1 §4.2] chỉ có MỘT câu, nhiều câu là người dùng đọc hai lần (UX-23)");

                VisualElement cancel = _fixture.Root.Q(RecurringRuleForm.DraftCancelElementName);
                Assert.IsNotNull(cancel, "khối nháp không có nút Huỷ để thoát (UX-23)");
                yield return UxEventSender.Click(_fixture.Window, form.AnchorField);
                Assert.AreSame(cancel, FocusedElement(),
                    "bấm vào ô đang khoá không đưa focus tới nút Huỷ — người dùng không thấy lối ra (UX-23)");
                DisposeFixture();
            }
        }

        /// <summary>
        /// UX-26: sửa một trường xong, toast phải nói RÕ TRƯỜNG NÀO đổi thành GIÁ TRỊ NÀO, không chỉ "Đã sửa". Bản đầu tiên chỉ
        /// kiểm con số nên một toast "Đã sửa thành 72" (không nêu trường) vẫn xanh, tức khoá thiếu nửa lỗi (R-12).
        /// <para>
        /// Hành trình đi ĐỦ HAI BƯỚC của [SD1 §4.2] / bảng 7.0: luật mẫu chạy liền mạch (chu kỳ 168 = thời gian chạy 168) nên
        /// lúc nào cũng có một lần lặp đang chạy, và đổi chu kỳ là đổi mốc sinh id của chính lần lặp đó — màn BẮT BUỘC giữ
        /// nháp tại ô rồi mới hỏi, không ghi thẳng. Bản đầu tiên chờ toast ngay sau Enter, tức đòi màn bỏ qua bước hai; nó đỏ
        /// trong khi màn đang giữ đúng hợp đồng. Giữ nháp lại thành một câu assert riêng ở đây để bước đó không âm thầm mất.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator EditField_ToastNamesFieldAndValue()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenRecurring(UxHubWindowFixture.AllSizes[4], language, LiveOpsConfirmResult.Destructive);
                RecurringRuleForm form = _fixture.Recurring.Form;

                yield return TypeIntoFieldUntilItHolds(form.PeriodField, NewPeriodHoursText, "chu kỳ");
                yield return UxEventSender.PressEnter(_fixture.Window);
                yield return UxEventSender.WaitUntil(() => UxLayoutAuditor.IsShownOnScreen(form.DraftBlock),
                    "gõ chu kỳ mới rồi Enter mà không khối nháp nào bật lên (UX-26)");
                Assert.IsFalse(_fixture.Window.Toast.IsVisible,
                    "chu kỳ mới chưa qua bước xác nhận mà đã báo đã ghi — Main.asset và toast nói hai chuyện khác nhau (UX-26)");

                VisualElement writeButton = _fixture.Root.Q(RecurringRuleForm.DraftWriteElementName);
                Assert.IsNotNull(writeButton, "khối nháp không có nút ghi để đi tiếp (UX-26)");
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(writeButton), "nút ghi của khối nháp không hiện ra (UX-26)");
                yield return UxEventSender.Click(_fixture.Window, writeButton);
                yield return UxEventSender.WaitUntil(() => _fixture.Window.Toast.IsVisible, "ghi chu kỳ xong không có toast nào (UX-26)");

                // Hai câu dưới đây gác hồi quy "ghi thẳng không hỏi" (R-04): nếu ai đó bỏ đường hỏi trong
                // RecurringRulesSection.OnDraftWriteRequested thì toast vẫn hiện y hệt và ca này vẫn xanh một cách rỗng.
                Assert.AreEqual(1, _confirmations.Requests.Count,
                    "bấm ghi mà màn không mở hộp xác nhận nào — đổi chu kỳ của lần lặp đang chạy bị ghi thẳng (UX-26)");
                RecurringLiveEventRule written = WrittenRuleOfSelectedType();
                Assert.IsNotNull(written, "ghi xong mà tài liệu không còn luật lặp nào của loại đang mở (UX-26)");
                Assert.AreEqual(NewPeriodHours, written.PeriodHours,
                    "toast báo đã ghi nhưng Main.asset vẫn giữ chu kỳ cũ (UX-26)");

                string message = _fixture.Window.Toast.MessageLabel.text ?? string.Empty;
                Assert.IsTrue(message.IndexOf(NewPeriodHoursText, StringComparison.Ordinal) >= 0,
                    "toast \"" + message + "\" không nói giá trị mới — người dùng không biết mình vừa ghi gì (UX-26)");
                string fieldLabel = FieldLabelTextOf(form.PeriodField);
                Assert.IsNotEmpty(fieldLabel, "trường chu kỳ không có nhãn nào để toast nhắc tên (UX-26)");
                Assert.IsTrue(message.IndexOf(fieldLabel, StringComparison.OrdinalIgnoreCase) >= 0,
                    "toast \"" + message + "\" không nêu tên trường \"" + fieldLabel
                    + "\" — sửa nhiều trường liên tiếp thì không biết toast nói về cái nào (UX-26)");
                DisposeFixture();
            }
        }

        /// <summary>
        /// UJ-22 (R-06): chu kỳ TRÒN NGÀY thì màn đọc theo ngày ("192 giờ" ⇒ "8 ngày"), và toast phải đọc y hệt cái ô đang
        /// hiện — hai chỗ lệch đơn vị là người dùng tưởng mình ghi nhầm.
        /// <para>
        /// Test KHÔNG gọi lại <c>RecurringRuleModel.PeriodText</c> để dựng chuỗi mong đợi (làm vậy là chép lại lỗi của sản
        /// phẩm vào câu assert): nó ĐỌC chữ quy đổi ngay trên màn — chữ phụ "= …" cạnh ô chu kỳ — rồi đòi toast chứa đúng chữ đó.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator EditPeriodInWholeDays_ToastReadsSameUnitAsScreen()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenRecurring(UxHubWindowFixture.AllSizes[4], language, LiveOpsConfirmResult.Destructive);
                RecurringRuleForm form = _fixture.Recurring.Form;

                yield return TypeIntoFieldUntilItHolds(form.PeriodField, WholeDayPeriodHoursText, "chu kỳ");
                yield return UxEventSender.PressEnter(_fixture.Window);
                yield return UxEventSender.WaitUntil(() => UxLayoutAuditor.IsShownOnScreen(form.DraftBlock),
                    "gõ chu kỳ tròn ngày rồi Enter mà không khối nháp nào bật lên (UJ-22)");

                Label suffix = SuffixLabelOf(form.PeriodField);
                Assert.IsNotNull(suffix, "ô chu kỳ không có chữ phụ nào để đọc đơn vị quy đổi (UJ-22)");
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(suffix), "chữ quy đổi cạnh ô chu kỳ không hiện ra (UJ-22)");
                string screenText = ValueTextOf(suffix);
                Assert.IsNotEmpty(screenText, "chữ quy đổi cạnh ô chu kỳ rỗng (UJ-22)");
                Assert.IsTrue(screenText.IndexOf(WholeDayPeriodHoursText, StringComparison.Ordinal) < 0,
                    "chữ quy đổi \"" + screenText + "\" vẫn in số giờ thô — lượt này không đi qua nhánh tròn ngày, "
                    + "tức không kiểm được đúng chỗ UJ-22 hỏng");

                VisualElement writeButton = _fixture.Root.Q(RecurringRuleForm.DraftWriteElementName);
                Assert.IsNotNull(writeButton, "khối nháp không có nút ghi để đi tiếp (UJ-22)");
                yield return UxEventSender.Click(_fixture.Window, writeButton);
                yield return UxEventSender.WaitUntil(() => _fixture.Window.Toast.IsVisible, "ghi chu kỳ tròn ngày xong không có toast nào (UJ-22)");

                string message = _fixture.Window.Toast.MessageLabel.text ?? string.Empty;
                Assert.IsTrue(message.IndexOf(screenText, StringComparison.Ordinal) >= 0,
                    "toast \"" + message + "\" không đọc cùng đơn vị với chữ \"" + screenText
                    + "\" đang hiện cạnh ô chu kỳ — hai chỗ nói hai con số khác nhau về cùng một luật (UJ-22)");
                DisposeFixture();
            }
        }

        /// <summary>
        /// UX-20/UX-21: ở 700 và 820, lối vào rail của màn Luật lặp phải hiện và chạm được bằng con trỏ; đi tới màn đó thì thân
        /// màn phải hiện ra. Ở cỡ hẹp chỉ kiểm CHẠM ĐƯỢC chứ không bấm — ô rail thu gọn mở <c>GenericMenu</c> mà phiên tự động
        /// không lái được (xem <see cref="UxHubWindowFixture.NavigateByRail"/>).
        /// </summary>
        [UnityTest]
        public IEnumerator NarrowRail_RecurringEntryIsReachable_AndBodyShows()
        {
            foreach (UxWindowSize size in new[] { new UxWindowSize(700, 560), new UxWindowSize(820, 560) })
            {
                yield return OpenSection(LiveOpsHubSections.Ids.Overview, size, LiveOpsHubLanguageId.Vietnamese);
                VisualElement entry = _fixture.RailEntryFor(LiveOpsHubSections.Ids.RecurringRules);
                Assert.IsNotNull(entry, "ở cỡ " + size + " rail không có lối vào nào cho màn Luật lặp (UX-20)");
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(entry),
                    "ở cỡ " + size + " lối vào rail của màn Luật lặp không hiện ra (UX-20)");
                Assert.IsTrue(UxEventSender.PickReaches(_fixture.Window, entry),
                    "ở cỡ " + size + " con trỏ không chạm được lối vào rail của màn Luật lặp (UX-20)");
                DisposeFixture();

                yield return OpenSection(LiveOpsHubSections.Ids.RecurringRules, size, LiveOpsHubLanguageId.Vietnamese);
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(_fixture.Root.Q(RecurringRulesSection.BodyElementName)),
                    "ở cỡ " + size + " mở màn Luật lặp mà thân màn không hiện ra (UX-21)");
                DisposeFixture();
            }
        }

        // ================================================================================================ trợ giúp

        private void DisposeFixture()
        {
            if (_fixture != null) _fixture.Dispose();
            _fixture = null;
            _confirmations = null;
        }

        private IEnumerator OpenRecurring(UxWindowSize size, LiveOpsHubLanguageId language,
            params LiveOpsConfirmResult[] confirmResults)
        {
            yield return OpenSection(LiveOpsHubSections.Ids.RecurringRules, size, language, confirmResults);
        }

        /// <summary>
        /// Mở hub với hộp xác nhận GIẢ, luôn luôn. Truyền null là dùng hộp THẬT (<c>ShowModalUtility</c>) — nó CHẶN batchmode
        /// vĩnh viễn: lượt 2022.3 của đợt này treo tới hết hạn giờ 3000s và không ra XML nào, còn log thì đứng im nên nhìn không
        /// khác gì "đang chạy chậm". Một hành trình gõ vào form có thể chạm đường hỏi xác nhận bất cứ lúc nào, nên chỗ này không
        /// được phép "tuỳ ca".
        /// </summary>
        private IEnumerator OpenSection(string sectionId, UxWindowSize size, LiveOpsHubLanguageId language,
            params LiveOpsConfirmResult[] confirmResults)
        {
            _confirmations = new ScriptedLiveOpsHubConfirmationPresenter(confirmResults ?? Array.Empty<LiveOpsConfirmResult>());
            LiveOpsHubServices services = UxHubWindowFixture.DesignServices(_confirmations, DesignNowUtc);
            _fixture = UxHubWindowFixture.Open(sectionId, size, language, services);
            yield return _fixture.WaitForLayout();
        }

        /// <summary>
        /// Lỗi đang hiện Ở CỤM của trường "Chạy mỗi đợt". Quét cả cửa sổ là sai: mẫu thiết kế có sẵn lỗi ở chỗ khác thì câu
        /// assert xanh mà không nói gì về trường vừa gõ (R-11).
        /// <para>
        /// Hai chỗ mà bản đầu tiên dò trượt, đều chứng minh được bằng code màn: (1) nó tìm class
        /// <c>LiveOpsHubClassNames.CalendarFieldError</c> = "liveops-hub-calendar-field-error"
        /// (<c>LiveOpsHubClassNames.Calendar.cs:32</c>) trong khi dòng lỗi của form Luật lặp mang
        /// <c>RecurringFieldError</c> = "liveops-hub-recurring-field-error" (<c>LiveOpsHubClassNames.Recurring.cs:41</c>, gắn
        /// ở <c>RecurringRuleForm.AddFieldRow</c>); (2) nó dừng ở HÀNG ô (nhãn + ô + chữ phụ) trong khi dòng lỗi nằm ở
        /// <c>noticeHost</c> — EM của hàng đó dưới cùng một cụm field. Cả hai làm câu assert không bao giờ thấy được lỗi, và
        /// câu "trước khi gõ chưa có lỗi" cũng đúng một cách rỗng tuếch.
        /// </para>
        /// </summary>
        private bool HasVisibleErrorOnActiveField(RecurringRuleForm form)
        {
            VisualElement field = form.ActiveField;
            if (field == null) return false;
            VisualElement group = FieldGroupOf(field);
            if (group == null) return false;
            foreach (VisualElement element in group.Query<VisualElement>(className: LiveOpsHubClassNames.RecurringFieldError).ToList())
            {
                if (UxLayoutAuditor.IsShownOnScreen(element) && !string.IsNullOrEmpty(TextOf(element))) return true;
            }
            return false;
        }

        /// <summary>
        /// Cụm của một ô nhập = hàng ô (<c>liveops-hub-recurring-field-row</c>) CỘNG chỗ treo chú thích dưới nó. Bắt lấy hàng
        /// theo class rồi lên một bậc: dò theo "cha đầu tiên có nhiều hơn một con" thì dừng ngay ở hàng ô và bỏ sót mọi thứ
        /// treo dưới ô (dòng phụ, lý do khoá, dòng lỗi).
        /// </summary>
        private static VisualElement FieldGroupOf(VisualElement field)
        {
            for (VisualElement current = field; current != null; current = current.hierarchy.parent)
            {
                if (current.ClassListContains(LiveOpsHubClassNames.RecurringFieldRow)) return current.hierarchy.parent;
            }
            return null;
        }

        private static string TextOf(VisualElement element)
        {
            Label label = element as Label;
            return label == null ? string.Empty : label.text ?? string.Empty;
        }

        private void AssertShown(string elementName, string failureMessage)
        {
            VisualElement element = _fixture.Root.Q(elementName);
            Assert.IsNotNull(element, failureMessage);
            Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(element), failureMessage);
        }

        /// <summary>
        /// Chữ của nhãn đi kèm một ô nhập — dùng để hỏi toast có nêu đúng tên trường không. Nhãn của form Luật lặp KHÔNG phải
        /// nhãn dựng sẵn của <c>BaseField</c> (ô được thêm vào hàng trần, không truyền label): nó là một <c>Label</c> anh em
        /// mang class <c>liveops-hub-recurring-field-label</c> (<c>RecurringRuleForm.AddFieldRow</c>). Bản đầu tiên chỉ hỏi
        /// nhãn dựng sẵn nên luôn nhận chuỗi rỗng và chết ở câu <c>Assert.IsNotEmpty</c> trước khi kịp đọc toast.
        /// </summary>
        private static string FieldLabelTextOf(VisualElement field)
        {
            VisualElement group = FieldGroupOf(field);
            if (group == null) return string.Empty;
            Label label = group.Q<Label>(className: LiveOpsHubClassNames.RecurringFieldLabel);
            return label == null ? string.Empty : label.text ?? string.Empty;
        }

        /// <summary>Chữ phụ "= 8 ngày" đứng cạnh ô — chỗ màn tự đọc giá trị vừa gõ ra thành đơn vị người đọc hiểu.</summary>
        private static Label SuffixLabelOf(VisualElement field)
        {
            VisualElement group = FieldGroupOf(field);
            return group == null ? null : group.Q<Label>(className: LiveOpsHubClassNames.RecurringFieldSuffix);
        }

        /// <summary>
        /// Bỏ dấu "=" dẫn đầu của chữ phụ, giữ lại đúng phần GIÁ TRỊ. Khoá <c>RecurringEqualsFormat</c> là "= {0}" dùng chung
        /// cho cả hai ngôn ngữ nên cắt theo dấu "=" là đủ, và test không phải chép lại khoá catalog vào đây.
        /// </summary>
        private static string ValueTextOf(Label suffix)
        {
            string text = suffix.text ?? string.Empty;
            int equalsIndex = text.IndexOf('=');
            return equalsIndex < 0 ? text.Trim() : text.Substring(equalsIndex + 1).Trim();
        }

        /// <summary>Luật lặp ĐANG GHI trong tài liệu của phiên — để so toast với thứ thật sự nằm trong Main.asset.</summary>
        private RecurringLiveEventRule WrittenRuleOfSelectedType()
        {
            LiveEventCalendarDocument document = _fixture.Services.Session.Document;
            if (document == null) return null;
            RecurringLiveEventRule rule;
            return document.TryGetRecurringRule(_fixture.Recurring.SelectedEventType, out rule) ? rule : null;
        }

        /// <summary>
        /// Câu KHOÁ = lý do "vì sao ô này không gõ được", in MỘT lần ở ô đầu tiên của nhóm bị khoá
        /// (<c>RecurringRuleForm.MarkDrafting</c>, class <c>liveops-hub-recurring-field-lock-reason</c>). Đếm để bắt trường
        /// hợp cùng một câu in dưới từng ô — lúc đó màn đọc như ba lỗi khác nhau đang xảy ra cùng lúc.
        /// <para>
        /// Bản đầu tiên đếm <c>DraftNotice</c> + <c>DraftHelpBox</c>: đó là HAI thứ khác nhau và [SD1 §4.2] đòi CẢ HAI cùng
        /// hiện (dòng "nháp chỉ ở ô này, Main.asset vẫn thấy giá trị cũ" và HelpBox hậu quả), nên ca này đỏ ở một màn đúng.
        /// Hai thứ đó giờ có câu gác riêng ngay trong ca UX-23.
        /// </para>
        /// </summary>
        private int CountVisibleLockSentences()
        {
            int count = 0;
            foreach (VisualElement element in _fixture.Root.Query<VisualElement>(
                className: LiveOpsHubClassNames.RecurringFieldLockReason).ToList())
            {
                if (UxLayoutAuditor.IsShownOnScreen(element) && !string.IsNullOrEmpty(TextOf(element))) count++;
            }
            return count;
        }

        /// <summary>
        /// Gõ vào một ô và GÕ LẠI tối đa <see cref="MaximumTypeAttempts"/> lượt cho tới khi ô thật sự đang giữ chuỗi đó, rồi
        /// mới trả về để người gọi bấm Enter/Tab chốt.
        /// <para>
        /// Vì sao phải gõ lại chứ không chỉ chờ: cú bấm lấy focus và từng phím của <see cref="UxEventSender.ReplaceText"/> đi
        /// qua <c>EditorWindow.SendEvent</c> và mỗi ký tự chỉ được gửi ĐÚNG MỘT LẦN. Ngay sau một lượt biên dịch lại — hoặc khi
        /// máy đang chạy nhiều tiến trình Unity — một khung của Editor kéo dài hàng trăm ms và phím rơi giữa chừng: ô giữ lại
        /// chuỗi cũ, Enter chốt đúng giá trị cũ, màn ĐÚNG khi không dựng nháp, và lượt đó đỏ vì nhịp máy. Chỉ CHỜ thì không
        /// chữa được — phím đã rơi thì chờ bao lâu ô cũng không tự nhận thêm (R-02). Gõ lại là thao tác người dùng thật vẫn làm
        /// khi thấy ô chưa đổi, và <c>ReplaceText</c> chọn hết trước khi gõ nên lượt sau đè sạch lượt trước.
        /// </para>
        /// </summary>
        private IEnumerator TypeIntoFieldUntilItHolds(VisualElement field, string text, string fieldNameForMessage)
        {
            for (int attempt = 1; attempt <= MaximumTypeAttempts; attempt++)
            {
                yield return UxEventSender.ReplaceText(_fixture.Window, field, text);
                Stopwatch stopwatch = Stopwatch.StartNew();
                int frames = 0;
                while (!FieldHoldsText(field, text)
                    && (frames < WaitFramesPerTypeAttempt || stopwatch.ElapsedMilliseconds < WaitMillisecondsPerTypeAttempt))
                {
                    UxEventSender.PumpDelayCalls();
                    frames++;
                    yield return null;
                }
                if (FieldHoldsText(field, text)) yield break;
            }
            Assert.Fail("gõ vào ô " + fieldNameForMessage + " " + MaximumTypeAttempts + " lượt mà ô vẫn không nhận được chuỗi \""
                + text + "\" (ô đang giữ \"" + TextInsideField(field) + "\")");
        }

        private static bool FieldHoldsText(VisualElement field, string text)
        {
            return string.Equals(TextInsideField(field), text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Chữ ĐANG NẰM TRONG ô nhập, không phải <c>value</c>: ô của form là <c>isDelayed</c> nên <c>value</c> chỉ đổi lúc chốt.
        /// <para>
        /// Đi xuống qua <c>unity-base-field__input</c> rồi lấy <c>TextElement</c> đầu tiên — cùng một đường mà
        /// <see cref="UxEventSender.ReplaceText"/> dùng để bấm vào ô, nên hai bên luôn nói về cùng một chỗ. Có <c>?? field</c>
        /// đỡ phía sau vì ở bản Unity nào đó phần nhận phím chính là gốc của ô chứ không nằm dưới lớp bọc.
        /// </para>
        /// </summary>
        private static string TextInsideField(VisualElement field)
        {
            if (field == null) return string.Empty;
            VisualElement inputHost = field.Q(className: "unity-base-field__input") ?? field;
            TextElement input = inputHost as TextElement ?? inputHost.Q<TextElement>();
            return input == null ? string.Empty : input.text ?? string.Empty;
        }

        private VisualElement FocusedElement()
        {
            IPanel panel = _fixture.Root.panel;
            if (panel == null || panel.focusController == null) return null;
            return panel.focusController.focusedElement as VisualElement;
        }
    }
}
