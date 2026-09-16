using System.Collections;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Màn Luật lặp dựng trong cửa sổ hub thật (mục 7.4, [SD1 §4]). Trọng tâm là luồng hai bước: gõ tiền tố mới chỉ tạo NHÁP
    /// tại ô — Main.asset chưa đổi — và chỉ hộp cấp 2 gõ đúng id đang chạy mới ghi. Esc / "Giữ tiền tố cũ" trong hộp đưa người
    /// dùng về bước một với nháp còn nguyên; nháp chỉ mất khi bấm "Huỷ (Esc)" tại ô.
    /// <para>
    /// Nút bấm bằng chuột thật qua <c>EditorWindow.SendEvent</c> (như ConfirmContentTests): gọi thẳng handler thì không chứng
    /// minh được nút đã nối vào luồng.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class RecurringRulesSectionTests
    {
        private const string WeeklyPassType = "weekly-pass";
        private const string SkyRaceType = "sky-race";
        private const string PublishedPrefix = "weekly-pass-";
        private const string NewPrefix = "bonus-";
        private const string RunningEventId = "weekly-pass-35";
        private const int SkyRaceLongerActiveHours = 22;

        private SectionTestScope _scope;

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            if (_scope != null) _scope.Dispose();
            _scope = null;
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        [UnityTest]
        public IEnumerator Section_RequiredElementsPresent()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter());

            _scope.AssertRequiredElementsPresent();
            Assert.AreEqual(2, Section.List.RowCount, "mẫu thiết kế có hai luật lặp: weekly-pass và sky-race");
            Assert.AreEqual(WeeklyPassType, Section.SelectedEventType, "luật đầu danh sách được chọn sẵn");
            Assert.IsNotNull(_scope.View.Q(RecurringNextOccurrencesTable.ElementName));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PrefixChangeWithRunning_DraftUntilTypedConfirm()
        {
            ScriptedLiveOpsHubConfirmationPresenter presenter = new ScriptedLiveOpsHubConfirmationPresenter();
            yield return OpenPublishedPrefixSample(presenter);

            Section.Form.PrefixField.value = NewPrefix;
            yield return null;

            Assert.IsTrue(Section.Draft.NeedsConfirmation, "gõ tiền tố mới khi weekly-pass-35 đang chạy chỉ tạo nháp tại ô");
            Assert.AreEqual(PublishedPrefix, WrittenPrefix(), "Main.asset chưa đổi — Lịch, Kiểm lịch và Xuất JSON vẫn thấy tiền tố cũ");
            Assert.AreEqual(0, presenter.Requests.Count, "bước một không hỏi gì cả");
            Assert.IsNotNull(_scope.View.Q(RecurringRuleForm.DraftBlockElementName), "khối cảnh báo nằm ngay dưới ô đang sửa");

            presenter.Enqueue(LiveOpsConfirmResult.Destructive);
            yield return ClickButton(RecurringRuleForm.DraftWriteElementName);

            Assert.AreEqual(1, presenter.Requests.Count, "bước hai mở đúng một hộp");
            Assert.AreEqual(LiveOpsConfirmLevel.TypeToConfirm, presenter.Requests[0].Level);
            Assert.AreEqual(RunningEventId, presenter.Requests[0].TypeToConfirmText, "phải gõ id người chơi đang giữ (C-6)");
            Assert.AreEqual(NewPrefix, WrittenPrefix(), "gõ khớp + bấm Đổi tiền tố mới ghi vào asset");
            Assert.IsFalse(Section.Draft.NeedsConfirmation, "ghi xong thì không còn nháp");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EscInConfirm_KeepsDraft()
        {
            ScriptedLiveOpsHubConfirmationPresenter presenter = new ScriptedLiveOpsHubConfirmationPresenter();
            yield return OpenPublishedPrefixSample(presenter);

            Section.Form.PrefixField.value = NewPrefix;
            yield return null;
            // Esc trong hộp và nút "Giữ tiền tố cũ" cùng ra Safe — cả hai phải đưa người dùng về bước một, không mất nháp.
            presenter.Enqueue(LiveOpsConfirmResult.Safe);
            yield return ClickButton(RecurringRuleForm.DraftWriteElementName);

            Assert.AreEqual(1, presenter.Requests.Count);
            Assert.AreEqual(PublishedPrefix, WrittenPrefix(), "đóng hộp bằng Esc không ghi gì");
            Assert.IsTrue(Section.Draft.NeedsConfirmation, "nháp còn nguyên — chỉ 'Huỷ (Esc)' tại ô mới bỏ nháp");
            Assert.AreEqual(NewPrefix, Section.Draft.DraftRule.EffectiveIdPrefix);

            yield return ClickButton(RecurringRuleForm.DraftCancelElementName);

            Assert.IsFalse(Section.Draft.NeedsConfirmation, "'Huỷ (Esc)' tại ô là chỗ DUY NHẤT nháp mất đi");
            Assert.AreEqual(PublishedPrefix, WrittenPrefix());
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ExtendActiveHours_WritesImmediatelyWithToast()
        {
            ScriptedLiveOpsHubConfirmationPresenter presenter = new ScriptedLiveOpsHubConfirmationPresenter();
            yield return OpenDesignSample(presenter);
            LiveOpsToastModel toast = null;
            Section.Services.Bus.ToastRequested += model => toast = model;

            Section.ApplyNavigation(LiveOpsHubNavigation.To(Section.Id).WithEventType(SkyRaceType, false));
            yield return null;
            // sky-race chạy 20 giờ mỗi 24 giờ và 08:47 UTC nằm trong đợt: kéo dài đợt đang chạy không lấy đi gì của người chơi.
            Section.Form.ActiveField.value = SkyRaceLongerActiveHours;
            yield return null;

            Assert.AreEqual(0, presenter.Requests.Count, "kéo dài đợt đang chạy không phải hỏi (bảng 7.0)");
            Assert.IsNotNull(toast, "mọi lệnh sửa ghi thẳng đều có toast kèm Hoàn tác");
            RecurringLiveEventRule rule;
            Assert.IsTrue(Section.Services.Session.Document.TryGetRecurringRule(SkyRaceType, out rule));
            Assert.AreEqual(SkyRaceLongerActiveHours, rule.ActiveHours);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Navigation_SelectsAndFlashesRule()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter());

            Section.ApplyNavigation(LiveOpsHubNavigation.To(Section.Id).WithEventType(SkyRaceType, true));
            yield return null;

            Assert.AreEqual(SkyRaceType, Section.SelectedEventType);
            Button row = Section.List.FindRow(SkyRaceType);
            Assert.IsNotNull(row, "hàng của luật được điều hướng tới phải có trong danh sách");
            Assert.IsTrue(row.ClassListContains(LiveOpsHubClassNames.RowFlash),
                "hàng chớp nền một nhịp để mắt bắt được chỗ vừa nhảy tới");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EmptyState_WhenCalendarHasNoRule()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveEventCalendarDocument.Empty)));
            _scope = SectionTestScope.Open(new RecurringRulesSection(services));
            yield return _scope.WaitForLayout();

            VisualElement empty = _scope.View.Q(RecurringRulesSection.EmptyElementName);
            Assert.IsFalse(empty.ClassListContains(LiveOpsHubClassNames.RecurringHidden),
                "lịch chưa có luật nào thì hiện trạng thái trống");
            Assert.AreEqual(LiveOpsHubStrings.RecurringEmptyTitle, _scope.View.Q<Label>(RecurringRulesSection.EmptyTitleElementName).text);
            Assert.AreEqual(0, Section.List.RowCount);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DeleteRule_WithRunningOccurrence_AsksLevelTwoThenRemoves()
        {
            ScriptedLiveOpsHubConfirmationPresenter presenter = new ScriptedLiveOpsHubConfirmationPresenter();
            yield return OpenPublishedPrefixSample(presenter);
            presenter.Enqueue(LiveOpsConfirmResult.Destructive);

            yield return ClickButton(RecurringRuleForm.DeleteElementName);

            Assert.AreEqual(1, presenter.Requests.Count);
            Assert.AreEqual(LiveOpsConfirmLevel.TypeToConfirm, presenter.Requests[0].Level,
                "xoá luật có lần lặp đang chạy = cấp 2 (bảng 7.0)");
            Assert.AreEqual(RunningEventId, presenter.Requests[0].TypeToConfirmText);
            RecurringLiveEventRule removed;
            Assert.IsFalse(Section.Services.Session.Document.TryGetRecurringRule(WeeklyPassType, out removed));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TokenClick_FocusesItsField()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter());

            yield return ClickButton(RecurringRuleSentence.TokenElementName(RecurringRuleFields.ActiveHours));
            yield return null;

            Assert.IsTrue(IsFocusInside(Section.Form.ActiveField), "bấm token đưa focus tới đúng ô của token đó");
            LogAssert.NoUnexpectedReceived();
        }

        private RecurringRulesSection Section => (RecurringRulesSection)_scope.Section;

        private bool IsFocusInside(VisualElement field)
        {
            VisualElement focused = _scope.View.panel.focusController.focusedElement as VisualElement;
            while (focused != null)
            {
                if (focused == field) return true;
                focused = focused.parent;
            }
            return false;
        }

        private string WrittenPrefix()
        {
            RecurringLiveEventRule rule;
            Assert.IsTrue(Section.Services.Session.Document.TryGetRecurringRule(WeeklyPassType, out rule));
            return rule.EffectiveIdPrefix;
        }

        /// <summary>Chuột thật lên một nút của màn: chờ nút có layout rồi gửi MouseDown/MouseUp vào cửa sổ.</summary>
        private IEnumerator ClickButton(string elementName)
        {
            Button button = _scope.View.Q<Button>(elementName);
            Assert.IsNotNull(button, "màn thiếu nút '" + elementName + "'");
            yield return LiveOpsHubWindowTestScope.WaitForLayout(button);
            Vector2 center = button.worldBound.center;
            _scope.Window.SendEvent(new Event { type = EventType.MouseDown, mousePosition = center, button = 0, clickCount = 1 });
            _scope.Window.SendEvent(new Event { type = EventType.MouseUp, mousePosition = center, button = 0, clickCount = 1 });
            yield return null;
        }

        private IEnumerator OpenDesignSample(ScriptedLiveOpsHubConfirmationPresenter presenter)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithConfirmation(presenter)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            _scope = SectionTestScope.Open(new RecurringRulesSection(services));
            yield return _scope.WaitForLayout();
        }

        /// <summary>
        /// Như mẫu thiết kế nhưng tiền tố weekly-pass đang bằng ĐÚNG bản đã đăng: từ đây gõ tiền tố mới là tình huống
        /// "đổi id của đợt đang chạy" sạch, không lẫn với hậu quả đã ghi sẵn trong mẫu.
        /// </summary>
        private IEnumerator OpenPublishedPrefixSample(ScriptedLiveOpsHubConfirmationPresenter presenter)
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            RecurringLiveEventRule weeklyPass;
            Assert.IsTrue(document.TryGetRecurringRule(WeeklyPassType, out weeklyPass));
            // Qua lệnh sửa chứ không qua builder: builder NỐI THÊM luật trùng loại (validator mới là chỗ báo), còn
            // SetRecurringRuleEdit thay đúng luật của loại đó — đúng thứ người dùng làm trên form.
            LiveEventCalendarDocument withPublishedPrefix;
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new SetRecurringRuleEdit(weeklyPass.WithIdPrefix(PublishedPrefix)),
                out withPublishedPrefix));

            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithConfirmation(presenter)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(withPublishedPrefix)));
            _scope = SectionTestScope.Open(new RecurringRulesSection(services));
            yield return _scope.WaitForLayout();
        }
    }
}
