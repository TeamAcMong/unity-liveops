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

        private const string NewPrefixDraft = "pass-";

        /// <summary>Chu kỳ mới của lượt UX-26 — toast phải nói CẢ tên trường lẫn giá trị này.</summary>
        private const string NewPeriodHours = "72";

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
                yield return UxEventSender.PressTab(_fixture.Window);

                Assert.IsTrue(HasVisibleErrorOnActiveField(form),
                    "gõ 'Chạy mỗi đợt' dài hơn chu kỳ mà hàng của trường đó không có câu lỗi nào (UX-22)");
                VisualElement writeButton = _fixture.Root.Q(RecurringRuleForm.DraftWriteElementName);
                Assert.IsNotNull(writeButton, "form không có nút ghi nháp để khoá lại (UX-22)");
                Assert.IsTrue(UxLayoutAuditor.IsShownOnScreen(writeButton), "nút ghi nháp không hiện ra (UX-22)");
                Assert.IsFalse(writeButton.enabledInHierarchy,
                    "luật đang sai mà nút ghi vẫn bấm được — người dùng ghi được một luật tự mâu thuẫn (UX-22)");
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
        /// </summary>
        [UnityTest]
        public IEnumerator EditField_ToastNamesFieldAndValue()
        {
            foreach (LiveOpsHubLanguageId language in UxHubWindowFixture.AllLanguages)
            {
                yield return OpenRecurring(UxHubWindowFixture.AllSizes[4], language);
                RecurringRuleForm form = _fixture.Recurring.Form;

                yield return UxEventSender.ReplaceText(_fixture.Window, form.PeriodField, NewPeriodHours);
                yield return UxEventSender.PressEnter(_fixture.Window);
                yield return UxEventSender.WaitUntil(() => _fixture.Window.Toast.IsVisible, "sửa chu kỳ xong không có toast nào (UX-26)");

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

        private IEnumerator OpenRecurring(UxWindowSize size, LiveOpsHubLanguageId language)
        {
            yield return OpenSection(LiveOpsHubSections.Ids.RecurringRules, size, language);
        }

        /// <summary>
        /// Mở hub với hộp xác nhận GIẢ, luôn luôn. Truyền null là dùng hộp THẬT (<c>ShowModalUtility</c>) — nó CHẶN batchmode
        /// vĩnh viễn: lượt 2022.3 của đợt này treo tới hết hạn giờ 3000s và không ra XML nào, còn log thì đứng im nên nhìn không
        /// khác gì "đang chạy chậm". Một hành trình gõ vào form có thể chạm đường hỏi xác nhận bất cứ lúc nào, nên chỗ này không
        /// được phép "tuỳ ca".
        /// </summary>
        private IEnumerator OpenSection(string sectionId, UxWindowSize size, LiveOpsHubLanguageId language)
        {
            LiveOpsHubServices services = UxHubWindowFixture.DesignServices(
                new ScriptedLiveOpsHubConfirmationPresenter(), DesignNowUtc);
            _fixture = UxHubWindowFixture.Open(sectionId, size, language, services);
            yield return _fixture.WaitForLayout();
        }

        /// <summary>
        /// Lỗi đang hiện Ở HÀNG của trường "Chạy mỗi đợt". Quét cả cửa sổ là sai: mẫu thiết kế có sẵn lỗi ở chỗ khác thì câu
        /// assert xanh mà không nói gì về trường vừa gõ (R-11).
        /// </summary>
        private bool HasVisibleErrorOnActiveField(RecurringRuleForm form)
        {
            VisualElement field = form.ActiveField;
            if (field == null) return false;
            if (field.ClassListContains(LiveOpsHubClassNames.CalendarFieldError)) return true;
            VisualElement row = RowOf(field);
            if (row == null) return false;
            foreach (VisualElement element in row.Query<VisualElement>(className: LiveOpsHubClassNames.CalendarFieldError).ToList())
            {
                if (UxLayoutAuditor.IsShownOnScreen(element)) return true;
            }
            return false;
        }

        /// <summary>Hàng chứa một ô nhập: cha gần nhất có nhiều hơn một con (ô + nhãn/câu lỗi).</summary>
        private static VisualElement RowOf(VisualElement field)
        {
            for (VisualElement current = field.hierarchy.parent; current != null; current = current.hierarchy.parent)
            {
                if (current.hierarchy.childCount > 1) return current;
            }
            return null;
        }

        /// <summary>Chữ của nhãn đi kèm một ô nhập — dùng để hỏi toast có nêu đúng tên trường không.</summary>
        private static string FieldLabelTextOf(VisualElement field)
        {
            if (field is BaseField<int> integerField && !string.IsNullOrEmpty(integerField.label)) return integerField.label;
            if (field is BaseField<string> textField && !string.IsNullOrEmpty(textField.label)) return textField.label;
            Label label = field.Q<Label>(className: "unity-base-field__label");
            return label == null ? string.Empty : label.text ?? string.Empty;
        }

        /// <summary>Câu khoá = HelpBox/ghi chú đang hiện trong khối nháp; đếm để bắt trường hợp hai câu nói cùng một việc.</summary>
        private int CountVisibleLockSentences()
        {
            int count = 0;
            foreach (string elementName in new[] { RecurringRuleForm.DraftNoticeElementName, RecurringRuleForm.DraftHelpBoxElementName })
            {
                VisualElement element = _fixture.Root.Q(elementName);
                if (element != null && UxLayoutAuditor.IsShownOnScreen(element)) count++;
            }
            return count;
        }

        private VisualElement FocusedElement()
        {
            IPanel panel = _fixture.Root.panel;
            if (panel == null || panel.focusController == null) return null;
            return panel.focusController.focusedElement as VisualElement;
        }
    }
}
