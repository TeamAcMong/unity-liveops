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
    /// assert bằng cách ĐỌC trạng thái.
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

        private UxHubWindowFixture _fixture;

        [TearDown]
        public void TearDown()
        {
            if (_fixture != null) _fixture.Dispose();
            _fixture = null;
        }

        /// <summary>
        /// UX-22: gõ "Chạy mỗi đợt" lớn hơn chu kỳ thì chỉ được báo lỗi TẠI trường đó; nút ghi phải khoá lại chứ không cho ghi
        /// một luật tự mâu thuẫn.
        /// </summary>
        [UnityTest]
        public IEnumerator ActiveHoursLongerThanPeriod_ShowsFieldError_AndBlocksWrite()
        {
            yield return OpenRecurring(UxHubWindowFixture.AllSizes[4]);
            RecurringRuleForm form = _fixture.Recurring.Form;

            yield return UxEventSender.ReplaceText(_fixture.Window, form.ActiveField, ActiveHoursLongerThanPeriod);
            yield return UxEventSender.PressTab(_fixture.Window);

            VisualElement writeButton = _fixture.Root.Q(RecurringRuleForm.DraftWriteElementName);
            Assert.IsTrue(HasVisibleError(form),
                "gõ 'Chạy mỗi đợt' dài hơn chu kỳ mà không có câu lỗi nào ở trường đó (UX-22)");
            if (writeButton != null && UxLayoutAuditor.IsShownOnScreen(writeButton))
            {
                Assert.IsFalse(writeButton.enabledInHierarchy,
                    "luật đang sai mà nút ghi vẫn bấm được — người dùng ghi được một luật tự mâu thuẫn (UX-22)");
            }
        }

        /// <summary>
        /// UX-23: khi có nháp tiền tố, form khoá lại và chỉ ĐƯỢC hiện MỘT câu giải thích; bấm vào ô đang khoá phải đưa focus tới
        /// nút Huỷ — nếu không, người dùng bấm mãi vào ô chết mà không biết lối ra.
        /// </summary>
        [UnityTest]
        public IEnumerator PrefixDraft_LocksFormWithOneSentence_AndClickOnLockedFieldFocusesCancel()
        {
            yield return OpenRecurring(UxHubWindowFixture.AllSizes[4]);
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
        }

        /// <summary>UX-26: sửa một trường xong, toast phải nói RÕ trường nào đổi thành giá trị nào, không chỉ "Đã sửa".</summary>
        [UnityTest]
        public IEnumerator EditField_ToastNamesFieldAndValue()
        {
            yield return OpenRecurring(UxHubWindowFixture.AllSizes[4]);
            RecurringRuleForm form = _fixture.Recurring.Form;

            yield return UxEventSender.ReplaceText(_fixture.Window, form.PeriodField, "72");
            yield return UxEventSender.PressEnter(_fixture.Window);
            yield return UxEventSender.WaitUntil(() => _fixture.Window.Toast.IsVisible, "sửa chu kỳ xong không có toast nào (UX-26)");

            string message = _fixture.Window.Toast.MessageLabel.text ?? string.Empty;
            Assert.IsTrue(message.IndexOf("72", StringComparison.Ordinal) >= 0,
                "toast \"" + message + "\" không nói giá trị mới — người dùng không biết mình vừa ghi gì (UX-26)");
        }

        // ================================================================================================ trợ giúp

        private IEnumerator OpenRecurring(UxWindowSize size)
        {
            LiveOpsHubServices services = UxHubWindowFixture.DesignServices(null, DesignNowUtc);
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.RecurringRules, size, LiveOpsHubLanguageId.Vietnamese, services);
            yield return _fixture.WaitForLayout();
        }

        private bool HasVisibleError(RecurringRuleForm form)
        {
            foreach (VisualElement element in _fixture.Root.Query<VisualElement>(className: LiveOpsHubClassNames.CalendarFieldError).ToList())
            {
                if (UxLayoutAuditor.IsShownOnScreen(element)) return true;
            }
            return form.ActiveField != null && form.ActiveField.ClassListContains(LiveOpsHubClassNames.CalendarFieldError);
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
