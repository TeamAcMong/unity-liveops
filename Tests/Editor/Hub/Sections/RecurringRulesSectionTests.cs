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
        private const string AnchorUtcText = "2026-01-05T00:00:00Z";

        /// <summary>Luật chạy liền mạch 24/24 — mẫu "Hằng ngày 00:00 UTC · chạy 20 giờ" rút nó đi 4 giờ.</summary>
        private const int SeamlessDayHours = 24;

        private const int DailyPresetActiveHours = 20;

        /// <summary>Vòng chờ của test UI (V-23): chỉ fail khi quá CẢ 60 khung LẪN 5 giây.</summary>
        private const int MaximumWaitFrames = 60;

        private const double MaximumWaitSeconds = 5d;

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

        /// <summary>
        /// (Q-W4-4, user duyệt 16/9) Nháp sống ở ĐÚNG MỘT ô: gõ tiếp vào ô thứ hai sẽ thay nháp cũ bằng nháp mới và cái vừa
        /// gõ ở ô thứ nhất biến mất không dấu vết. Nên khi một ô giữ nháp thì ba ô còn lại KHOÁ, kèm lý do in THÀNH CHỮ cạnh
        /// ô (SPIKE-B SP-3 — test không assert tooltip, tooltip chỉ là đường dự phòng).
        /// </summary>
        [UnityTest]
        public IEnumerator DraftInOneField_LocksOtherThree_WithReasonAsText()
        {
            yield return OpenPublishedPrefixSample(new ScriptedLiveOpsHubConfirmationPresenter());

            Section.Form.PrefixField.value = NewPrefix;
            yield return null;

            Assert.IsTrue(Section.Draft.NeedsConfirmation, "gõ tiền tố mới khi đợt đang chạy chỉ tạo nháp tại ô");
            Assert.IsTrue(Section.Form.PrefixField.enabledSelf, "chính ô đang giữ nháp phải mở để còn sửa tiếp hoặc Esc");
            string expectedReason = string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.RecurringFieldLockedByDraftFormat, LiveOpsHubStrings.RecurringIdPrefixLabel);
            AssertFieldLocked(Section.Form.AnchorField, expectedReason);
            AssertFieldLocked(Section.Form.PeriodField, expectedReason);
            AssertFieldLocked(Section.Form.ActiveField, expectedReason);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (Q-W4-4 mở rộng) Ô JSON là lối sửa thứ NĂM của cùng một luật: bấm "Áp" trong lúc còn nháp sẽ dựng nháp mới đè nháp
        /// cũ, đúng lỗ hổng mà Q-W4-4 bịt ở bốn ô kia. Nút ghi khoá kèm ĐÚNG câu lý do đó in thành chữ; ô nhập vẫn mở để còn
        /// đọc và sao chép JSON.
        /// </summary>
        [UnityTest]
        public IEnumerator DraftInOneField_LocksJsonApply_WithReasonAsText()
        {
            yield return OpenPublishedPrefixSample(new ScriptedLiveOpsHubConfirmationPresenter());

            Section.Form.PrefixField.value = NewPrefix;
            yield return null;

            string expectedReason = string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.RecurringFieldLockedByDraftFormat, LiveOpsHubStrings.RecurringIdPrefixLabel);
            Assert.IsTrue(Section.Form.JsonFoldout.ApplySlot.IsBlocked, "còn nháp thì nút Áp của foldout JSON cũng phải khoá");
            Assert.AreEqual(expectedReason, Section.Form.JsonFoldout.ApplySlot.Reason, "lý do in THÀNH CHỮ cạnh nút, không chỉ tooltip");
            Assert.IsTrue(Section.Form.JsonFoldout.Editor.enabledSelf, "ô nhập vẫn mở: khoá là để không nuốt nháp, không phải để cấm đọc");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Ô JSON hiện luật ĐANG GHI, kể cả khi form đang giữ nháp. Cùng một nghĩa "đang ghi" với chỗ nút "Áp" so ứng viên
        /// (<c>TryGetWrittenRule</c>); đổ JSON của nháp vào thì câu "JSON chưa đổi so với luật đang ghi" nói sai về chính
        /// thứ nó vừa so, và người dùng đọc JSON của nháp như thể Main.asset đã đổi.
        /// </summary>
        [UnityTest]
        public IEnumerator DraftHeld_JsonBox_ShowsWrittenRuleNotDraft()
        {
            yield return OpenPublishedPrefixSample(new ScriptedLiveOpsHubConfirmationPresenter());

            Section.Form.PrefixField.value = NewPrefix;
            yield return null;

            Assert.IsTrue(Section.Draft.NeedsConfirmation, "gõ tiền tố mới khi đợt đang chạy chỉ tạo nháp tại ô");
            string json = Section.Form.JsonFoldout.Editor.value;
            StringAssert.Contains(PublishedPrefix, json, "ô JSON phải hiện tiền tố của luật đang ghi trong Main.asset");
            Assert.IsFalse(json.Contains(NewPrefix), "…chứ không phải tiền tố của nháp chưa ghi");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Huỷ nháp tại ô mở lại cả bốn ô và xoá hết nhãn lý do — khoá là trạng thái tạm, không phải cửa một chiều.</summary>
        [UnityTest]
        public IEnumerator DraftResolved_UnlocksAllFields()
        {
            yield return OpenPublishedPrefixSample(new ScriptedLiveOpsHubConfirmationPresenter());
            Section.Form.PrefixField.value = NewPrefix;
            yield return null;

            yield return ClickButton(RecurringRuleForm.DraftCancelElementName);

            Assert.IsFalse(Section.Draft.NeedsConfirmation);
            AssertFieldUnlocked(Section.Form.PrefixField);
            AssertFieldUnlocked(Section.Form.AnchorField);
            AssertFieldUnlocked(Section.Form.PeriodField);
            AssertFieldUnlocked(Section.Form.ActiveField);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Ô bị khoá: không gõ được VÀ có một nhãn lý do đọc được ngay cạnh, không phải chỉ một ô xám câm.</summary>
        private void AssertFieldLocked(VisualElement input, string expectedReason)
        {
            Assert.IsFalse(input.enabledSelf, "ô '" + input.name + "' phải khoá trong lúc ô khác giữ nháp");
            Label reason = LockReasonOf(input);
            Assert.IsNotNull(reason, "ô '" + input.name + "' bị khoá mà không có nhãn lý do nào cạnh ô");
            Assert.AreEqual(expectedReason, reason.text);
            Assert.IsFalse(reason.ClassListContains(LiveOpsHubClassNames.RecurringHidden), "nhãn lý do phải HIỆN, không chỉ có chữ");
        }

        private void AssertFieldUnlocked(VisualElement input)
        {
            Assert.IsTrue(input.enabledSelf, "ô '" + input.name + "' phải mở lại khi hết nháp");
            Label reason = LockReasonOf(input);
            Assert.IsNotNull(reason);
            Assert.AreEqual(string.Empty, reason.text);
            Assert.IsTrue(reason.ClassListContains(LiveOpsHubClassNames.RecurringHidden));
        }

        /// <summary>
        /// Nhãn lý do là Label đầu tiên mang class lý-do-khoá trong cùng NHÓM của ô (hàng field + chỗ treo chú thích) —
        /// tìm theo cây chứ không theo tên, vì nhãn không có tên riêng trong hợp đồng element của màn.
        /// </summary>
        private static Label LockReasonOf(VisualElement input)
        {
            VisualElement group = input.parent != null ? input.parent.parent : null;
            return group == null ? null : group.Q<Label>(className: LiveOpsHubClassNames.RecurringFieldLockReason);
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
            // Trống phải có chỗ đi tiếp (mục 7): lịch đã mở nhưng chưa có luật nào thì nút là "Thêm luật".
            Button emptyAction = _scope.View.Q<Button>(RecurringRulesSection.EmptyActionElementName);
            Assert.IsNotNull(emptyAction, "khối trống thiếu nút bước tiếp");
            Assert.AreEqual(LiveOpsHubStrings.RecurringAddRuleButton, emptyAction.text);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Chưa có asset là trạng thái chung của mọi màn (mục 7): câu ngắn "Chưa có lịch LiveOps trong project" + nút đưa
        /// người dùng sang Tổng quan — màn này không tự viết lại câu riêng và không để người dùng đứng trước một khối chữ suông.
        /// </summary>
        [UnityTest]
        public IEnumerator NoAsset_EmptyStateOffersOverview()
        {
            // WithCalendarAsset(null) là cách khai "chưa có asset": bỏ hẳn lời gọi thì phiên đi dò asset thật trong dự án.
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(null));
            _scope = SectionTestScope.Open(new RecurringRulesSection(services));
            yield return _scope.WaitForLayout();

            string navigatedSectionId = string.Empty;
            services.Bus.NavigationRequested += navigation => navigatedSectionId = navigation.SectionId;
            Assert.AreEqual(LiveOpsHubStrings.RecurringNoAssetTitle, _scope.View.Q<Label>(RecurringRulesSection.EmptyTitleElementName).text);
            Button emptyAction = _scope.View.Q<Button>(RecurringRulesSection.EmptyActionElementName);
            Assert.AreEqual(LiveOpsHubStrings.RecurringNoAssetActionButton, emptyAction.text);

            // Cửa sổ của SectionTestScope mở bằng OpenForTest (services riêng của nó), nên yêu cầu điều hướng dừng ở bus —
            // đúng thứ cần đo: section có phát đúng đích hay không, không phải khung có đổi màn hay không.
            yield return ClickButton(RecurringRulesSection.EmptyActionElementName);

            Assert.AreEqual(LiveOpsHubSections.Ids.Overview, navigatedSectionId, "nút bước tiếp phải đưa sang Tổng quan để tạo lịch");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Áp "Mẫu" đổi CẢ nhịp: mẫu hằng ngày 24/20 giữ nguyên id của đợt đang chạy nhưng khép nó sớm 4 giờ. Suy thao tác
        /// theo tên ô (Anchor → ChangeRecurringIdentity) thì policy trả None và đợt đang chạy bị rút ngắn KHÔNG hộp nào —
        /// đúng thứ bảng 7.0 cấm.
        /// </summary>
        [UnityTest]
        public IEnumerator Preset_ShortensRunningActiveHours_AsksLevel1()
        {
            ScriptedLiveOpsHubConfirmationPresenter presenter = new ScriptedLiveOpsHubConfirmationPresenter();
            yield return OpenSeamlessDailySample(presenter);

            int presetIndex = DailyPresetIndex();
            Section.Form.PresetField.value = Section.Form.PresetField.choices[presetIndex];
            yield return null;

            Assert.IsTrue(Section.Draft.NeedsConfirmation, "mẫu rút ngắn đợt đang chạy phải giữ nháp tại ô, không ghi thẳng");
            Assert.AreEqual(LiveOpsConfirmRequirement.Level1, Section.Draft.Requirement, "rút ngắn đợt đang chạy = hộp cấp 1 (bảng 7.0)");
            Assert.AreEqual(SeamlessDayHours, WrittenRule().ActiveHours, "Main.asset chưa đổi ở bước một");

            presenter.Enqueue(LiveOpsConfirmResult.Destructive);
            yield return ClickButton(RecurringRuleForm.DraftWriteElementName);

            Assert.AreEqual(1, presenter.Requests.Count, "đúng một hộp, và là hộp của bước hai");
            Assert.AreEqual(LiveOpsConfirmLevel.Level1, presenter.Requests[0].Level);
            Assert.AreEqual(DailyPresetActiveHours, WrittenRule().ActiveHours, "xác nhận xong mới ghi nhịp của mẫu");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Vào màn, sang màn khác, quay lại: khung gọi <c>CreateView</c> mỗi lượt. Nối sự kiện lại mỗi lượt thì một lần bấm
        /// "Thêm 5" cộng 5 lần số lượt — bảng nhảy thẳng lên 20 hàng.
        /// </summary>
        [UnityTest]
        public IEnumerator ReopenedTwice_AddMoreStepsOnce()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter());
            _scope.Window.ShowSection(Section.Id);
            yield return _scope.WaitForLayout();
            _scope.Window.ShowSection(Section.Id);
            yield return _scope.WaitForLayout();

            Assert.AreEqual(RecurringRuleModel.DefaultOccurrenceCount, Section.Form.Occurrences.RowCount);
            yield return ClickButton(RecurringNextOccurrencesTable.AddMoreElementName);
            yield return WaitForOccurrenceRowsToChange();

            Assert.AreEqual(RecurringRuleModel.DefaultOccurrenceCount + RecurringRuleModel.OccurrenceCountStep,
                Section.Form.Occurrences.RowCount, "một lần bấm = một bước 5 đợt, dù đã vào ra màn ba lần");
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

        /// <summary>
        /// Tag "đổi id" của cột Lúc này phải có thoi Warning ([SD1 §4.1]): chữ suông thì nó lẫn với mọi chú thích khác, còn
        /// dấu giai đoạn bên trái nói chuyện khác hẳn ("đợt này đang ở đâu").
        /// </summary>
        [UnityTest]
        public IEnumerator IdChangeTag_HasWarningMark()
        {
            yield return OpenDesignSample(new ScriptedLiveOpsHubConfirmationPresenter());

            VisualElement tag = _scope.View.Q(className: LiveOpsHubClassNames.RecurringIdChangeTag);
            Assert.IsNotNull(tag, "mẫu thiết kế có đợt weekly-pass-35 đổi id nên bảng phải có tag 'đổi id'");
            LiveOpsStateMark mark = tag.Q<LiveOpsStateMark>();
            Assert.IsNotNull(mark, "tag thiếu thoi Warning");
            Assert.IsTrue(mark.ClassListContains(LiveOpsHubClassNames.StateMarkWarning), "thoi phải là họ Warning");
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

        /// <summary>Chờ bảng đợt kế tiếp tính lại sau debounce 250ms (V-23: quá CẢ 60 khung LẪN 5 giây mới fail).</summary>
        private IEnumerator WaitForOccurrenceRowsToChange()
        {
            int frames = 0;
            double startedAt = EditorApplication.timeSinceStartup;
            while (Section.Form.Occurrences.RowCount == RecurringRuleModel.DefaultOccurrenceCount)
            {
                bool framesExhausted = ++frames > MaximumWaitFrames;
                bool secondsExhausted = EditorApplication.timeSinceStartup - startedAt > MaximumWaitSeconds;
                if (framesExhausted && secondsExhausted) Assert.Fail("bảng đợt kế tiếp không tính lại sau 60 khung và 5 giây");
                yield return null;
            }
        }

        private static int DailyPresetIndex()
        {
            IReadOnlyList<LiveOpsRulePresetLibrary.Preset> presets = LiveOpsRulePresets.Resolve();
            for (int index = 0; index < presets.Count; index++)
            {
                if (presets[index].PeriodHours == SeamlessDayHours && presets[index].ActiveHours == DailyPresetActiveHours) return index;
            }
            Assert.Fail("không tìm thấy mẫu hằng ngày 24/20 trong LiveOpsRulePresets.Resolve()");
            return -1;
        }

        private RecurringLiveEventRule WrittenRule()
        {
            RecurringLiveEventRule rule;
            Assert.IsTrue(Section.Services.Session.Document.TryGetRecurringRule(WeeklyPassType, out rule));
            return rule;
        }

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

        /// <summary>
        /// Một luật DUY NHẤT chạy liền mạch 24/24 neo 00:00 UTC: 08:47 nằm giữa đợt đang chạy, và mẫu hằng ngày 24/20 đổi
        /// đúng MỘT thứ — thời gian chạy — nên hậu quả đo được là "khép sớm 4 giờ", không lẫn với đổi id.
        /// </summary>
        private IEnumerator OpenSeamlessDailySample(ScriptedLiveOpsHubConfirmationPresenter presenter)
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(WeeklyPassType, "Pass tuần", 1, false, string.Empty))
                .WithRecurringRule(new RecurringLiveEventRule(WeeklyPassType, AnchorUtcText, PublishedPrefix, SeamlessDayHours,
                    SeamlessDayHours, string.Empty))
                .Build();
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithConfirmation(presenter)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document)));
            _scope = SectionTestScope.Open(new RecurringRulesSection(services));
            yield return _scope.WaitForLayout();
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
