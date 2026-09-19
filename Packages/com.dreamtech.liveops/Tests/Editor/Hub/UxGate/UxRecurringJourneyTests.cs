using System;
using System.Collections;
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
        private const string NewPeriodHours = "180";

        private UxHubWindowFixture _fixture;

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
        /// thiết kế không làm test xanh nhầm; (2) lỗi phải nằm ở HÀNG của chính trường vừa gõ, không phải "ở đâu đó trong cửa
        /// sổ"; (3) nút ghi phải CÓ và phải khoá — bản đầu tiên bọc câu assert trong <c>if (writeButton != null && …)</c> nên nút
        /// biến mất cũng là xanh.
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

                yield return UxEventSender.ReplaceText(_fixture.Window, form.ActiveField, ActiveHoursLongerThanPeriod);
                yield return WaitUntilFieldHoldsText(form.ActiveField, ActiveHoursLongerThanPeriod, "chạy mỗi đợt");
                yield return UxEventSender.PressTab(_fixture.Window);

                Assert.IsTrue(HasVisibleErrorOnActiveField(form),
                    "gõ 'Chạy mỗi đợt' dài hơn chu kỳ mà hàng của trường đó không có câu lỗi nào (UX-22)");
                Assert.IsTrue(form.ActiveField.ClassListContains(LiveOpsHubClassNames.RecurringFieldInvalid),
                    "ô đang giữ giá trị hỏng mà không mang viền chặn — câu lỗi ở dưới không chỉ được vào ô nào (UX-22)");
                VisualElement writeButton = _fixture.Root.Q(RecurringRuleForm.DraftWriteElementName);
                Assert.IsNotNull(writeButton, "form không có nút ghi nháp nào để nói về (UX-22)");
                // Hợp đồng UJ-15 của màn là GỠ HẲN lời mời, không phải làm nó xám: giá trị này khiến game bỏ CẢ luật nên
                // "Ghi giá trị mới…" là mời làm một việc không làm được (RecurringRuleForm.cs:294-300, gác sẵn ở
                // RecurringUxFixTests.RunLongerThanPeriod_ShowsFieldErrorOnly_NoWriteButton). Bản đầu tiên của ca này đòi
                // nút PHẢI hiện và phải xám — tức đòi ngược hợp đồng, nên nó đỏ ở nửa sau kể cả khi màn làm đúng.
                Assert.IsFalse(UxLayoutAuditor.IsShownOnScreen(writeButton),
                    "luật đang sai mà nút ghi vẫn mời bấm — người dùng ghi được một luật tự mâu thuẫn (UX-22)");
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

                yield return UxEventSender.ReplaceText(_fixture.Window, form.PrefixField, NewPrefixDraft);
                yield return WaitUntilFieldHoldsText(form.PrefixField, NewPrefixDraft, "tiền tố id");
                yield return UxEventSender.PressTab(_fixture.Window);
                yield return UxEventSender.WaitUntil(() => UxLayoutAuditor.IsShownOnScreen(form.DraftBlock),
                    "gõ tiền tố mới không bật khối nháp nào (UX-23)");

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

                yield return UxEventSender.ReplaceText(_fixture.Window, form.PeriodField, NewPeriodHours);
                yield return WaitUntilFieldHoldsText(form.PeriodField, NewPeriodHours, "chu kỳ");
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

                string message = _fixture.Window.Toast.MessageLabel.text ?? string.Empty;
                Assert.IsTrue(message.IndexOf(NewPeriodHours, StringComparison.Ordinal) >= 0,
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
            LiveOpsHubServices services = UxHubWindowFixture.DesignServices(
                new ScriptedLiveOpsHubConfirmationPresenter(confirmResults ?? Array.Empty<LiveOpsConfirmResult>()), DesignNowUtc);
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

        /// <summary>
        /// Câu KHOÁ = lý do "vì sao ô này không gõ được", in MỘT lần ở ô đầu tiên của nhóm bị khoá
        /// (<c>RecurringRuleForm.MarkDrafting</c>, class <c>liveops-hub-recurring-field-lock-reason</c>). Đếm để bắt trường
        /// hợp cùng một câu in dưới từng ô — lúc đó màn đọc như ba lỗi khác nhau đang xảy ra cùng lúc.
        /// <para>
        /// Bản đầu tiên đếm <c>DraftNotice</c> + <c>DraftHelpBox</c>: đó là HAI thứ khác nhau và [SD1 §4.2] đòi CẢ HAI cùng
        /// hiện (dòng "nháp chỉ ở ô này, Main.asset vẫn thấy giá trị cũ" và HelpBox hậu quả), nên ca này đỏ ở một màn đúng.
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
        /// Chờ ô nhập THẬT SỰ đang giữ chuỗi vừa gõ, trước khi bấm Enter/Tab để chốt. Ô <c>isDelayed</c> chưa chốt nên phải đọc
        /// chữ trong ô con, không đọc <c>value</c>.
        /// <para>
        /// Vì sao chờ chứ không gõ xong là chốt luôn: cú bấm lấy focus của <see cref="UxEventSender.ReplaceText"/> và các phím
        /// theo sau đi qua <c>EditorWindow.SendEvent</c>, và ngay sau một lượt biên dịch lại, một khung của Editor kéo dài hàng
        /// trăm ms — bấm Enter khi ô còn chưa nhận đủ ký tự thì ô chốt lại ĐÚNG GIÁ TRỊ CŨ, không có thay đổi nào và màn đúng
        /// khi không dựng nháp. Lượt đó đỏ vì nhịp máy, không vì màn sai. Chờ ở đây biến cái đó thành một câu lỗi đọc được.
        /// </para>
        /// </summary>
        private static IEnumerator WaitUntilFieldHoldsText(VisualElement field, string text, string fieldNameForMessage)
        {
            // Dual-path 2022.3 / 6000.6: ở bản này phần nhận phím CHÍNH LÀ một TextElement, ở bản kia nó là VisualElement
            // bọc một TextElement con. Hỏi cả hai thay vì đi theo tên class nội bộ của một bản.
            VisualElement inputHost = field.Q(className: "unity-base-field__input") ?? field;
            TextElement input = inputHost as TextElement ?? inputHost.Q<TextElement>();
            yield return UxEventSender.WaitUntil(
                () => input != null && string.Equals(input.text, text, StringComparison.Ordinal),
                "gõ vào ô " + fieldNameForMessage + " mà ô không nhận được chuỗi \"" + text + "\" (ô đang giữ \""
                + (input == null ? "<không có ô nhập>" : input.text) + "\")");
        }

        private VisualElement FocusedElement()
        {
            IPanel panel = _fixture.Root.panel;
            if (panel == null || panel.focusController == null) return null;
            return panel.focusController.focusedElement as VisualElement;
        }
    }
}
