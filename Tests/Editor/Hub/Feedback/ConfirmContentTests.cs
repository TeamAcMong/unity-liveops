using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Hộp xác nhận (8.6, [FD §3.10]) và adapter modal:
    /// <list type="bullet">
    /// <item>UI (cửa sổ <see cref="LiveOpsConfirmWindow"/> mở không modal, phím gửi bằng <c>SendEvent</c> thật để Navigation event đi kèm cũng
    /// chạy): Enter/Esc ra nút an toàn; nút phá huỷ chỉ chạy bằng click hoặc Space khi nó focus; cấp 2 Enter trong ô không làm gì và mở khoá
    /// chỉ khi gõ đúng id.</item>
    /// <item>Logic: bố cục đọc từ request, thiếu UXML chỉ còn nút an toàn, presenter nối <see cref="LiveOpsConfirmationPolicy"/> đúng bảng 7.0
    /// (kể cả C-6: chữ phải gõ là id người chơi đang giữ của bản đã đăng).</item>
    /// </list>
    /// </summary>
    [TestFixture]
    public sealed class ConfirmContentTests
    {
        private const int MaximumFrames = 60;
        private const string RunningId = "weekly-pass-35";

        private LiveOpsConfirmWindow _window;

        [TearDown]
        public void TearDown()
        {
            if (_window != null) _window.Close();
            _window = null;
        }

        private static LiveOpsConfirmRequest DeletePublishedRequest()
        {
            return new LiveOpsConfirmRequest.Builder()
                .WithTitle("Xoá hunt-0914?")
                .WithBody("Đợt chưa bắt đầu (14/9 00:00 → 17/9 00:00 UTC) nhưng đã có trong bản đăng 11/9 16:20: lần đăng tới người chơi sẽ không thấy đợt này.")
                .WithButtons("Xoá đợt", "Giữ lại")
                .Build();
        }

        private static LiveOpsConfirmRequest PrefixTypeToConfirmRequest(string typeToConfirmText = RunningId)
        {
            return new LiveOpsConfirmRequest.Builder()
                .WithTitle("Đổi tiền tố id của đợt đang chạy")
                .WithBody("weekly-pass-35 → pass-35. Đợt đang chạy tới 14/9 00:00 UTC. Hub không biết số người chơi toàn cục đang có điểm.")
                .WithTypeToConfirm(typeToConfirmText)
                .WithButtons("Đổi tiền tố", "Giữ tiền tố cũ")
                .WithKeyHint("Esc: Giữ tiền tố cũ · Enter không đổi gì")
                .Build();
        }

        // ── UI: phím thật trên cửa sổ hộp ─────────────────────────────────────────────────────────────────────────

        private IEnumerator Open(LiveOpsConfirmRequest request)
        {
            // Hộp trước chưa đóng (assert trước đó sai) thì đóng ở đây: hai cửa sổ hộp cùng mở làm focus rơi sai cửa sổ.
            if (_window != null) _window.Close();
            _window = LiveOpsConfirmWindow.OpenForTest(request);
            LiveOpsConfirmContent content = _window.Content;
            int frames = 0;
            while (!IsFocusedInside(content, request.Level == LiveOpsConfirmLevel.TypeToConfirm ? (VisualElement)content.TypeField : content.SafeButton))
            {
                if (++frames > MaximumFrames) Assert.Fail("hộp không focus phần tử đầu sau " + MaximumFrames + " khung");
                yield return null;
            }
        }

        private LiveOpsConfirmResult? CompletedResult(LiveOpsConfirmContent content)
        {
            return content.IsCompleted ? content.Result : (LiveOpsConfirmResult?)null;
        }

        private IEnumerator FocusAndWait(VisualElement element)
        {
            element.Focus();
            int frames = 0;
            while (!IsFocusedInside(_window.Content, element))
            {
                if (++frames > MaximumFrames) Assert.Fail("không focus được '" + element.name + "'");
                yield return null;
            }
        }

        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator Confirm_EnterAndEscReturnSafe()
        {
            foreach (string key in new[] { "return", "[enter]", "escape" })
            {
                yield return Open(DeletePublishedRequest());
                LiveOpsConfirmContent content = _window.Content;
                Assert.IsTrue(IsFocusedInside(content, content.SafeButton), "cấp 1: nút an toàn focus sẵn");
                _window.SendEvent(Event.KeyboardEvent(key));
                Assert.AreEqual(LiveOpsConfirmResult.Safe, CompletedResult(content), "phím '" + key + "' ra nút an toàn");
                Assert.AreEqual(LiveOpsConfirmResult.Safe, _window.Result);
                yield return null;
                Assert.IsTrue(_window == null, "chọn xong hộp đóng");
                _window = null;
            }

            yield return Open(PrefixTypeToConfirmRequest());
            LiveOpsConfirmContent typed = _window.Content;
            typed.TypeField.value = RunningId;
            _window.SendEvent(Event.KeyboardEvent("escape"));
            Assert.AreEqual(LiveOpsConfirmResult.Safe, CompletedResult(typed), "cấp 2: Esc trong ô gõ ra nút an toàn kể cả đã gõ khớp");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator Confirm_DestructiveOnlyOnClickOrSpace()
        {
            // Enter lúc nút phá huỷ đang focus: vẫn là an toàn (NavigationSubmitEvent không được "bấm" nút phá huỷ).
            yield return Open(DeletePublishedRequest());
            LiveOpsConfirmContent enterContent = _window.Content;
            yield return FocusAndWait(enterContent.DestructiveButton);
            _window.SendEvent(Event.KeyboardEvent("return"));
            Assert.AreEqual(LiveOpsConfirmResult.Safe, CompletedResult(enterContent), "Enter trên nút phá huỷ đang focus vẫn ra an toàn");
            yield return null;
            _window = null;

            // Tab tới nút phá huỷ rồi Space: phá huỷ.
            yield return Open(DeletePublishedRequest());
            LiveOpsConfirmContent spaceContent = _window.Content;
            yield return FocusAndWait(spaceContent.DestructiveButton);
            _window.SendEvent(Event.KeyboardEvent("space"));
            Assert.AreEqual(LiveOpsConfirmResult.Destructive, CompletedResult(spaceContent), "Space khi nút phá huỷ focus");
            Assert.AreEqual(LiveOpsConfirmResult.Destructive, _window.Result);
            yield return null;
            _window = null;

            // Space trên nút an toàn: an toàn.
            yield return Open(DeletePublishedRequest());
            LiveOpsConfirmContent safeSpace = _window.Content;
            _window.SendEvent(Event.KeyboardEvent("space"));
            Assert.AreEqual(LiveOpsConfirmResult.Safe, CompletedResult(safeSpace), "Space trên nút an toàn focus sẵn");
            yield return null;
            _window = null;

            // Click chuột lên nút phá huỷ: phá huỷ.
            yield return Open(DeletePublishedRequest());
            LiveOpsConfirmContent clickContent = _window.Content;
            Rect bound = clickContent.DestructiveButton.worldBound;
            Vector2 center = bound.center;
            _window.SendEvent(new Event { type = EventType.MouseDown, mousePosition = center, button = 0, clickCount = 1 });
            _window.SendEvent(new Event { type = EventType.MouseUp, mousePosition = center, button = 0, clickCount = 1 });
            Assert.AreEqual(LiveOpsConfirmResult.Destructive, CompletedResult(clickContent), "click nút phá huỷ");
            yield return null;
            _window = null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator Confirm_TypeToConfirm_EnterInFieldDoesNothing_UnlocksOnExactMatch()
        {
            yield return Open(PrefixTypeToConfirmRequest());
            LiveOpsConfirmContent content = _window.Content;
            Assert.IsTrue(IsFocusedInside(content, content.TypeField), "cấp 2: ô gõ focus sẵn");
            Assert.IsFalse(content.IsDestructiveEnabled, "chưa gõ thì nút phá huỷ khoá");
            Assert.AreEqual(LiveOpsHubStrings.FeedbackConfirmLockedTooltip, content.DestructiveSlot.tooltip, "tooltip nằm trên slot");

            content.TypeField.value = "weekly-pass-3";
            Assert.IsFalse(content.IsDestructiveEnabled, "thiếu một ký tự vẫn khoá");
            _window.SendEvent(Event.KeyboardEvent("return"));
            Assert.IsFalse(content.IsCompleted, "Enter trong ô không chạy nút nào");

            content.TypeField.value = RunningId;
            Assert.IsTrue(content.IsDestructiveEnabled, "gõ đúng id thì mở khoá");
            Assert.AreEqual(string.Empty, content.DestructiveSlot.tooltip);
            _window.SendEvent(Event.KeyboardEvent("return"));
            _window.SendEvent(Event.KeyboardEvent("[enter]"));
            Assert.IsFalse(content.IsCompleted, "Enter trong ô không chạy nút nào — kể cả đã gõ khớp");
            Assert.IsTrue(_window != null, "hộp vẫn mở");

            foreach (string nearMiss in new[] { "Weekly-pass-35", "weekly-pass-35 ", " weekly-pass-35", "weekly-pass-350" })
            {
                content.TypeField.value = nearMiss;
                Assert.IsFalse(content.IsDestructiveEnabled, "so Ordinal chính xác: '" + nearMiss + "' không mở khoá");
            }

            content.TypeField.value = RunningId;
            yield return FocusAndWait(content.DestructiveButton);
            _window.SendEvent(Event.KeyboardEvent("space"));
            Assert.AreEqual(LiveOpsConfirmResult.Destructive, CompletedResult(content), "gõ khớp, Tab tới nút rồi Space");
            LogAssert.NoUnexpectedReceived();
        }

        // ── Logic: bố cục và presenter ────────────────────────────────────────────────────────────────────────────

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Confirm_LayoutFromRequest_Level1AndLevel2()
        {
            LiveOpsConfirmContent level1 = new LiveOpsConfirmContent(DeletePublishedRequest());
            Assert.IsFalse(level1.IsLayoutMissing);
            Assert.AreEqual("Xoá hunt-0914?", level1.Q<Label>(LiveOpsConfirmContent.TitleElementName).text);
            Assert.IsTrue(level1.Q<Label>(LiveOpsConfirmContent.BodyElementName).text.StartsWith("Đợt chưa bắt đầu", StringComparison.Ordinal), "bắt đầu bằng chuỗi mong đợi");
            Assert.IsTrue(level1.Q(LiveOpsConfirmContent.WarningElementName).ClassListContains(LiveOpsHubClassNames.ConfirmWarningHidden),
                "cấp 1 thân là chữ thường, không HelpBox");
            Assert.IsTrue(level1.Q(LiveOpsConfirmContent.TypeAreaElementName).ClassListContains(LiveOpsHubClassNames.ConfirmTypeHidden));
            Assert.IsNull(level1.TypeField);
            Assert.AreEqual("Enter / Esc: Giữ lại", level1.Q<Label>(LiveOpsConfirmContent.KeyHintElementName).text);
            Assert.AreEqual("Giữ lại", level1.SafeButton.text);
            Assert.IsTrue(level1.SafeButton.ClassListContains(LiveOpsHubClassNames.ButtonPrimary), "nút an toàn là nút chính (chữ đậm)");
            Assert.AreEqual("Xoá đợt", level1.DestructiveButton.text);
            Assert.IsTrue(level1.DestructiveButton.ClassListContains(LiveOpsHubClassNames.ButtonDanger), "nút phá huỷ chỉ đổi màu chữ");
            Assert.IsTrue(level1.IsDestructiveEnabled);
            Assert.IsTrue(level1.ClassListContains(LiveOpsHubClassNames.Root), "hộp là panel riêng — gốc mang token hub");
            VisualElement buttons = level1.Q(className: LiveOpsHubClassNames.ConfirmButtons);
            Assert.AreEqual(3, buttons.childCount, "[gợi ý] … [phá huỷ] [an toàn]");
            Assert.AreSame(level1.SafeButton, buttons[2], "nút an toàn ở cuối phải");

            LiveOpsConfirmContent level2 = new LiveOpsConfirmContent(PrefixTypeToConfirmRequest());
            Assert.IsTrue(level2.Q(className: LiveOpsHubClassNames.Confirm).ClassListContains(LiveOpsHubClassNames.ConfirmLevel2));
            Assert.IsFalse(level2.Q(LiveOpsConfirmContent.WarningElementName).ClassListContains(LiveOpsHubClassNames.ConfirmWarningHidden),
                "cấp 2 thân là HelpBox cảnh báo");
            Assert.IsTrue(level2.Q(LiveOpsConfirmContent.BodyElementName).ClassListContains(LiveOpsHubClassNames.ConfirmBodyHidden));
            Assert.AreEqual(RunningId, level2.Q<Label>(LiveOpsConfirmContent.TypeIdElementName).text);
            Assert.AreEqual("Enter trong ô không chạy nút nào · bấm Đổi tiền tố, hoặc Tab tới nút rồi Space",
                level2.Q<Label>(LiveOpsConfirmContent.TypeHintElementName).text);
            Assert.IsNotNull(level2.TypeField);
            Assert.IsFalse(level2.IsDestructiveEnabled);
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Confirm_MissingUxml_OnlySafeButton()
        {
            LiveOpsConfirmContent content = new LiveOpsConfirmContent(DeletePublishedRequest(),
                new MissingPathsLiveOpsHubLayoutLoader(new[] { LiveOpsHubPaths.ConfirmWindowUxml }));
            Assert.IsTrue(content.IsLayoutMissing);
            Assert.IsNull(content.DestructiveButton, "không dựng được câu hậu quả thì không cho bấm phá huỷ");
            Assert.IsNotNull(content.SafeButton);
            Assert.IsNotNull(content.Q(LiveOpsConfirmContent.MissingLayoutElementName));
            int completed = 0;
            content.Completed += result => completed++;
            Assert.AreEqual(0, completed);
            Assert.Throws<ArgumentNullException>(() => new LiveOpsConfirmContent(null));
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Presenter_ModalAdapter_PassesRequestAndResult()
        {
            LiveOpsConfirmRequest shown = null;
            ModalLiveOpsHubConfirmationPresenter presenter = new ModalLiveOpsHubConfirmationPresenter(request =>
            {
                shown = request;
                return LiveOpsConfirmResult.Destructive;
            });
            LiveOpsConfirmRequest delete = DeletePublishedRequest();
            Assert.AreEqual(LiveOpsConfirmResult.Destructive, presenter.Confirm(delete));
            Assert.AreSame(delete, shown);
            Assert.Throws<ArgumentNullException>(() => presenter.Confirm(null));
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Presenter_PolicyTable_NoneProceeds_LevelsMustMatch_BlockedKindsThrow()
        {
            ScriptedLiveOpsHubConfirmationPresenter scripted = new ScriptedLiveOpsHubConfirmationPresenter(LiveOpsConfirmResult.Destructive);
            int builds = 0;
            Func<LiveOpsConfirmRequest> buildDelete = () =>
            {
                builds++;
                return DeletePublishedRequest();
            };

            LiveOpsConfirmDecision none = new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonNotStartedUnpublished,
                string.Empty, null, 0);
            Assert.AreEqual(LiveOpsConfirmResult.Destructive, ModalLiveOpsHubConfirmationPresenter.ConfirmAsPolicyDecides(scripted, none, buildDelete),
                "Không hỏi: thao tác chạy, toast Hoàn tác lo đường lui");
            Assert.AreEqual(0, builds, "không dựng hộp khi không hỏi");
            Assert.AreEqual(0, scripted.Requests.Count);

            LiveOpsConfirmDecision level1 = new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.Level1, LiveOpsConfirmationPolicy.ReasonPublishedNotStarted,
                string.Empty, null, 0);
            Assert.AreEqual(LiveOpsConfirmResult.Destructive, ModalLiveOpsHubConfirmationPresenter.ConfirmAsPolicyDecides(scripted, level1, buildDelete));
            Assert.AreEqual(1, scripted.Requests.Count, "cấp 1 mở đúng một hộp");
            Assert.Throws<InvalidOperationException>(() => ModalLiveOpsHubConfirmationPresenter.ConfirmAsPolicyDecides(scripted, level1,
                () => PrefixTypeToConfirmRequest()), "request nặng hơn chính sách cũng là lỗi dựng hộp");

            LiveOpsConfirmDecision notAllowed = new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.NotAllowed, LiveOpsConfirmationPolicy.ReasonEnded,
                string.Empty, null, 0);
            Assert.Throws<InvalidOperationException>(() => ModalLiveOpsHubConfirmationPresenter.ConfirmAsPolicyDecides(scripted, notAllowed, buildDelete),
                "Không cho: nút phải khoá kèm lý do, không được tới hộp");
            LiveOpsConfirmDecision dedicated = new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.DedicatedDialog, LiveOpsConfirmationPolicy.ReasonAlways,
                string.Empty, null, 0);
            Assert.Throws<InvalidOperationException>(() => ModalLiveOpsHubConfirmationPresenter.ConfirmAsPolicyDecides(scripted, dedicated, buildDelete),
                "Ghi dấu đã đăng dùng hộp riêng");
            Assert.AreEqual(1, scripted.Requests.Count, "không lần ném nào mở hộp");
        }

        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void Presenter_RecurringPrefixWithBaseline_TypesPublishedRunningId()
        {
            // V-20 C-6 (7.4 thắng): nháp đã đổi pass- → p-, bản đã đăng vẫn weekly-pass- → người chơi giữ weekly-pass-35.
            DateTime nowUtc = new DateTime(2026, 9, 13, 8, 47, 0, DateTimeKind.Utc);
            LiveEventCalendarDocument before = DocumentWithWeeklyPassPrefix("pass-");
            LiveEventCalendarDocument after = DocumentWithWeeklyPassPrefix("p-");
            LiveEventCalendarDocument published = DocumentWithWeeklyPassPrefix("weekly-pass-");
            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.ChangeRecurringIdentity, before, after, published,
                nowUtc, "weekly-pass");
            Assert.AreEqual(LiveOpsConfirmRequirement.TypeToConfirm, decision.Requirement);

            ScriptedLiveOpsHubConfirmationPresenter scripted = new ScriptedLiveOpsHubConfirmationPresenter(LiveOpsConfirmResult.Destructive);
            Assert.Throws<InvalidOperationException>(() => ModalLiveOpsHubConfirmationPresenter.ConfirmAsPolicyDecides(scripted, decision,
                () => PrefixTypeToConfirmRequest("pass-35")), "chữ phải gõ là id người chơi đang giữ, không phải id nháp");
            Assert.Throws<InvalidOperationException>(() => ModalLiveOpsHubConfirmationPresenter.ConfirmAsPolicyDecides(scripted, decision,
                DeletePublishedRequest), "chính sách đòi gõ id — hộp cấp 1 nhẹ hơn là lỗi");
            Assert.AreEqual(0, scripted.Requests.Count);

            Assert.AreEqual(LiveOpsConfirmResult.Destructive, ModalLiveOpsHubConfirmationPresenter.ConfirmAsPolicyDecides(scripted, decision,
                () => PrefixTypeToConfirmRequest(decision.TypeToConfirmText)));
            Assert.AreEqual(RunningId, scripted.Requests[0].TypeToConfirmText);
        }

        private static LiveEventCalendarDocument DocumentWithWeeklyPassPrefix(string idPrefix)
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("weekly-pass", "2026-01-05T00:00:00Z", idPrefix, 168, 168, string.Empty))
                .Build();
        }

        private static bool IsFocusedInside(LiveOpsConfirmContent content, VisualElement expected)
        {
            if (content == null || expected == null || content.panel == null) return false;
            for (VisualElement current = content.panel.focusController.focusedElement as VisualElement; current != null; current = current.hierarchy.parent)
            {
                if (current == expected) return true;
            }
            return false;
        }
    }
}
