using System.Collections;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Chiều sâu màn Kiểm lịch ([SD2 §2.4–2.7], Hình 17 ô 1, 5, 6). Bốn hợp đồng được khoá ở đây:
    /// <list type="bullet">
    /// <item>Sửa hàng loạt luôn cho XEM TRƯỚC và bật tắt được từng mục; nhiều thay đổi vẫn là MỘT Undo group mang đúng số.</item>
    /// <item>"Bỏ qua cảnh báo…" đòi ghi chú, lưu vào asset, và gắn với KHOẢNG — khoảng đổi thì cảnh báo hiện lại.</item>
    /// <item>"Quyết định… ▾" có đủ hai cách đúng; mục 2 ghi một ghi chú hẹn giờ, tới hạn thì phát hiện quay lại có tag.</item>
    /// <item>Menu chuột phải không chụp được (S-24) nên nội dung menu được đọc thẳng từ hàm dựng menu.</item>
    /// </list>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class ValidationDepthTests
    {
        private const string HuntType = "treasure-hunt";
        private const string LavaType = "lava-quest";
        private const string HuntConfigKey = "hunt_default";
        private const string LavaConfigKey = "lava_quest_v2";
        private const string IgnoreNote = "lava-quest nghỉ đến mùa tháng 10";

        /// <summary>
        /// Ghi chú DÀI hơn sức chứa một hàng: dùng để chứng minh hàng chỉ giữ bản rút gọn còn thẻ ghim "Xem ghi chú" giữ bản
        /// đủ (mục 7.5). Ghi chú ngắn của thiết kế không cắt nên không đọc được luật này.
        /// </summary>
        private const string LongIgnoreNote = "lava-quest nghỉ đến mùa tháng 10 vì đợt săn kho báu đã chiếm trọn khung giờ "
            + "cuối tuần, đội vận hành chốt trong buổi họp lịch ngày 12/9 và sẽ xem lại sau khi đợt hunt-0916-bonus khép";

        /// <summary>Hạn của ghi chú hẹn trong thiết kế ([SD2 §2.4] mục 2: "14/9 00:00 UTC").</summary>
        private const string ReminderDeadlineUtcText = "2026-09-14T00:00:00Z";

        /// <summary>Một hạn đã qua so với đồng hồ của lịch mẫu (13/9 08:47) — ghi chú hẹn hết hiệu lực.</summary>
        private const string PassedDeadlineUtcText = "2026-09-12T00:00:00Z";

        private SectionTestScope _scope;

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            LiveOpsPopoverContent.CloseCurrent();
            if (_scope != null) _scope.Dispose();
            _scope = null;
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        // ------------------------------------------------------------------------------- card xem trước sửa hàng loạt (ô 1)

        [UnityTest]
        public IEnumerator BulkRepairPreview_Shown()
        {
            LiveOpsHubServices services = ServicesFor(TwoBrokenTimesDocument());
            yield return Open(services);

            Assert.IsNull(Section.BulkPreview, "chưa bấm thì chưa có card");
            yield return ClickHeaderButton(ValidationSection.SafeRepairButtonElementName);

            SafeRepairPreviewCard card = Section.BulkPreview;
            Assert.IsNotNull(card, "[SD2 §2.5] bấm \"Sửa các lỗi an toàn (n)…\" CHÈN một card, không mở popover và không mở hộp modal");
            Assert.AreEqual(2, card.ItemCount, "hai đợt giờ hỏng chuẩn hoá được = hai hàng thay đổi");
            Assert.AreEqual(2, card.SelectedCount, "mọi mục bật sẵn");
            Assert.AreEqual(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ValidationDepthBulkPreviewTitleFormat, 2), card.TitleLabel.text);
            Assert.AreEqual(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ValidationDepthBulkPreviewApplyFormat, 2), card.ApplyButton.text);
            StringAssert.Contains(card.UndoGroupName, card.UndoLine.text, "dòng 10px nói ĐÚNG tên Undo group sắp tạo");
            Assert.IsNotNull(_scope.View.Q(LiveOpsHubPaths.ValidationDepthElementNames.BulkPreviewCard),
                "card nằm trong cây của màn (chỗ cắm ngay dưới dải summary)");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BulkRepairPreview_GeometryMatchesDesign()
        {
            // Ba số đo của [SD2 §2.5] (hàng cao ≥ 22, khe 6, dòng Undo thụt 20px) và bề ngang card bằng cả chỗ cắm. Khoá ở
            // đây chứ không trên ảnh: ô tick nằm 4px trong viền card nên dò cạnh ở skin tối bắt nhầm cạnh ô tick (soát W5 F-8).
            LiveOpsHubServices services = ServicesFor(TwoBrokenTimesDocument());
            yield return Open(services);
            yield return ClickHeaderButton(ValidationSection.SafeRepairButtonElementName);
            SafeRepairPreviewCard card = Section.BulkPreview;
            Assume.That(card, Is.Not.Null);
            yield return LiveOpsHubWindowTestScope.WaitForLayout(card);

            VisualElement host = _scope.View.Q(LiveOpsHubPaths.ValidationDepthElementNames.BulkPreviewHost);
            Assert.IsNotNull(host, "chỗ cắm luôn có trong cây");
            Assert.AreEqual(host.worldBound.width, card.worldBound.width, 1f,
                "card chen ngang cả chỗ cắm dưới dải summary — không phải popover hẹp");

            VisualElement firstRow = _scope.View.Q(LiveOpsHubPaths.ValidationDepthElementNames.BulkPreviewRowPrefix + "0");
            VisualElement secondRow = _scope.View.Q(LiveOpsHubPaths.ValidationDepthElementNames.BulkPreviewRowPrefix + "1");
            Assert.IsNotNull(firstRow);
            Assert.IsNotNull(secondRow);
            Assert.GreaterOrEqual(firstRow.worldBound.height, BulkPreviewRowMinimumHeight, "[SD2 §2.5] hàng cao tối thiểu 22");
            Assert.AreEqual(BulkPreviewRowGap, secondRow.worldBound.yMin - firstRow.worldBound.yMax, 1f,
                "[SD2 §2.5] khe giữa hai hàng là 6");
            Assert.AreEqual(BulkPreviewUndoIndent, card.UndoLine.worldBound.xMin - firstRow.worldBound.xMin, 1f,
                "[SD2 §2.5] dòng Undo thụt 20px cho thẳng với chữ của hàng (Toggle 14 + khe 6)");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BulkRepairPreview_ToggleOff_EveryCountFollowsSelection()
        {
            LiveOpsHubServices services = ServicesFor(TwoBrokenTimesDocument());
            yield return Open(services);
            yield return ClickHeaderButton(ValidationSection.SafeRepairButtonElementName);
            SafeRepairPreviewCard card = Section.BulkPreview;
            Assume.That(card, Is.Not.Null);

            card.Toggles[0].value = false;
            yield return null;

            Assert.AreEqual(1, card.SelectedCount);
            Assert.AreEqual(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ValidationDepthBulkPreviewTitleFormat, 1), card.TitleLabel.text);
            Assert.AreEqual(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ValidationSafeRepairButtonFormat, 1), Section.SafeRepairSlot.Button.text,
                "[SD2 §2.5] nhãn nút section header cũng đếm theo số mục đang bật");

            card.Toggles[1].value = false;
            yield return null;

            Assert.IsFalse(card.ApplyButton.enabledSelf, "bỏ chọn hết thì không còn gì để áp");
            Assert.AreEqual(LiveOpsHubStrings.ValidationDepthBulkPreviewNothingReason,
                CardApplyReason(card), "SPIKE-B SP-3: lý do khoá in THÀNH CHỮ, không chỉ tooltip");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BulkRepairPreview_Apply_OneUndoGroupNamedForCount()
        {
            LiveOpsHubServices services = ServicesFor(TwoBrokenTimesDocument());
            yield return Open(services);
            LiveOpsToastModel toast = null;
            services.Bus.ToastRequested += model => toast = model;
            string documentBefore = LiveOpsHubDocumentSnapshot.Write(services.Session.Document);

            yield return ClickHeaderButton(ValidationSection.SafeRepairButtonElementName);
            Assume.That(Section.BulkPreview, Is.Not.Null);
            yield return Click(Section.BulkPreview.ApplyButton);

            Assert.AreNotEqual(documentBefore, LiveOpsHubDocumentSnapshot.Write(services.Session.Document), "Áp phải đổi lịch thật");
            Assert.IsNotNull(toast, "mỗi lần áp một toast Hoàn tác");
            Assert.AreEqual(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ValidationSafeRepairUndoFormat, 2), toast.Message,
                "[SD2 §2.5] tên group \"LiveOps: Sửa nhanh 2 lỗi\" — đếm theo số mục đã áp");
            Assert.AreEqual(toast.Message, toast.UndoGroupName, "tên Undo group trùng câu toast (luật toast chung)");
            Assert.AreNotEqual(LiveOpsToastModel.NoUndoGroup, toast.UndoGroup, "hai thay đổi vẫn nằm trong ĐÚNG MỘT Undo group");
            Assert.IsTrue(services.Session.Check.IsRunning, "sửa xong phải tự kiểm lại (PD-10)");
            Assert.IsNull(Section.BulkPreview, "áp xong card đóng lại");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BulkRepairPreview_ApplyAfterToggleOff_AppliesOnlyTickedItem()
        {
            // Luật "chỉ áp mục đang bật" và "tên group đếm theo số ĐÃ ÁP" chỉ đọc được khi tập áp KHÁC tập đầy (soát W5 F-7):
            // với hai mục bật hết, một hồi quy bỏ qua Toggle vẫn cho đúng kết quả.
            LiveOpsHubServices services = ServicesFor(TwoBrokenTimesDocument());
            yield return Open(services);
            LiveOpsToastModel toast = null;
            services.Bus.ToastRequested += model => toast = model;

            yield return ClickHeaderButton(ValidationSection.SafeRepairButtonElementName);
            SafeRepairPreviewCard card = Section.BulkPreview;
            Assume.That(card, Is.Not.Null);
            Assume.That(card.ItemCount, Is.EqualTo(2));
            string huntEndBefore = EndTimeTextOf(services, HuntBrokenEntryKey);
            string lavaEndBefore = EndTimeTextOf(services, LavaBrokenEntryKey);

            card.Toggles[0].value = false;
            yield return null;
            Assume.That(card.SelectedCount, Is.EqualTo(1));

            yield return Click(card.ApplyButton);

            int changed = 0;
            if (EndTimeTextOf(services, HuntBrokenEntryKey) != huntEndBefore) changed++;
            if (EndTimeTextOf(services, LavaBrokenEntryKey) != lavaEndBefore) changed++;
            Assert.AreEqual(1, changed,
                "đúng MỘT đợt được sửa — bỏ tick mà vẫn áp cả hai thì Toggle chỉ là trang trí");
            Assert.IsNotNull(toast);
            Assert.AreEqual(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ValidationSafeRepairUndoFormat, 1), toast.Message,
                "[SD2 §2.5] tên group đếm theo số mục ĐÃ ÁP, không phải số mục có trong card");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BulkRepairPreview_ToggleOffEverything_HeaderButtonStaysTheSwitchThatClosesTheCard()
        {
            // Soát W5 F-5: khoá nút header lúc này vừa nói dối ("không có lỗi nào sửa nhanh an toàn được" — vẫn còn hai lệnh
            // sửa, người dùng chỉ vừa bỏ tick) vừa lấy mất đúng cái nút đã mở card.
            LiveOpsHubServices services = ServicesFor(TwoBrokenTimesDocument());
            yield return Open(services);
            yield return ClickHeaderButton(ValidationSection.SafeRepairButtonElementName);
            SafeRepairPreviewCard card = Section.BulkPreview;
            Assume.That(card, Is.Not.Null);

            card.Toggles[0].value = false;
            card.Toggles[1].value = false;
            yield return null;

            Assert.IsTrue(Section.SafeRepairSlot.Button.enabledSelf, "nút header còn vai trò công tắc nên không được khoá");
            Assert.AreEqual(string.Empty, Section.SafeRepairSlot.Reason,
                "nút mở thì không được in lý do khoá — nhất là lý do sai sự thật");

            yield return ClickHeaderButton(ValidationSection.SafeRepairButtonElementName);

            Assert.IsNull(Section.BulkPreview, "bấm lần nữa phải ĐÓNG card");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BulkRepairPreview_ClosedWhenSectionDetaches()
        {
            // Soát W5 F-3: cửa sổ dựng lại thân bằng _body.Clear() + CreateView() trên CÙNG instance section, nên biến còn trỏ
            // vào card cũ là nút header đếm theo một card vô hình và lần bấm đầu tiên chỉ "đóng" nó.
            LiveOpsHubServices services = ServicesFor(TwoBrokenTimesDocument());
            yield return Open(services);
            yield return ClickHeaderButton(ValidationSection.SafeRepairButtonElementName);
            Assume.That(Section.BulkPreview, Is.Not.Null);

            ValidationSection section = Section;
            _scope.Dispose();
            _scope = null;
            yield return null;

            Assert.IsNull(section.BulkPreview, "rời panel là card không còn — state không được sống lâu hơn cây");
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------------------------------- Quyết định… ▾ (§2.4)

        [UnityTest]
        public IEnumerator DecisionMenu_TwoChoices()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            yield return Open(services);

            ValidationFindingRow row = FindRow(LiveEventCalendarRuleIds.RunningEventIdChanged);
            Assert.IsNotNull(row, "lịch mẫu đổi tiền tố luật lặp đang chạy nên phải có hàng running-event-id-changed");
            Assert.AreEqual(ValidationRowAction.Decision, row.Row.Action, "[SD2 §2.4] hai cách đều đúng = Quyết định…, không phải nút");
            Assert.IsNull(row.ActionButton, "menu THAY CHỖ nút — không được có cả hai");
            ToolbarMenu menu = row.DecisionMenuElement as ToolbarMenu;
            Assert.IsNotNull(menu, "hàng phải được cắm một ToolbarMenu có chevron");
            Assert.AreEqual(LiveOpsFindingText.PrimaryButtonText(row.Row.Finding), menu.text, "(V-8) chữ nút lấy từ LiveOpsFindingText");
            Assert.AreEqual(2, row.Row.Finding.Repairs.Count, "[SD2 §2.4] đúng hai mục: giữ tiền tố cũ, hoặc để sau khi đợt khép");
            Assert.AreEqual(row.Row.ActionTooltip, menu.tooltip,
                "[SD2 §2.3] tooltip \"Hai cách đúng…\" đi theo nút; nút đổi thành menu thì menu phải mang nó (soát W5 F-10)");
            Assert.AreNotEqual(string.Empty, menu.tooltip, "câu tooltip không được rỗng");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DecisionMenu_ItemLabelsComeFromFindingText()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarFinding finding = FindFinding(services, LiveEventCalendarRuleIds.RunningEventIdChanged);
            Assume.That(finding, Is.Not.Null);

            DecisionMenu menu = new DecisionMenu(finding, services.Format, repair => { });

            Assert.AreEqual(finding.Repairs.Count, menu.ItemLabels.Count);
            for (int index = 0; index < finding.Repairs.Count; index++)
            {
                Assert.AreEqual(LiveOpsFindingText.PlainText(LiveOpsFindingText.RepairOptionText(finding, finding.Repairs[index], services.Format)),
                    menu.ItemLabels[index], "(V-8) mỗi mục là câu của LiveOpsFindingText ở dạng phẳng — menu gốc không vẽ rich text");
            }
        }

        [UnityTest]
        public IEnumerator DecisionMenu_DeferChoice_WritesReminderIntoAsset()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            yield return Open(services);
            LiveEventCalendarFinding finding = FindFinding(services, LiveEventCalendarRuleIds.RunningEventIdChanged);
            Assume.That(finding, Is.Not.Null);
            Assert.IsNotNull(RepairWithId(finding, LiveOpsFindingText.DeferUntilEndRepairId),
                "[SD2 §2.4] mục 2 của menu là \"để sau khi đợt đang chạy khép\"");

            // Đi qua chính callback của menu: mục menu phải dẫn tới lệnh sửa của nó, không phải tới một lệnh sửa gần giống.
            var chosen = new List<LiveEventCalendarRepair>();
            DecisionMenu menu = new DecisionMenu(finding, services.Format, chosen.Add);
            InvokeMenuItem(menu.Element.menu, menu.ItemLabels[1]);
            Assert.AreEqual(1, chosen.Count, "chọn mục 2 phải gọi đúng một lần");
            services.Session.Apply(chosen[0].Edit, LiveOpsHubStrings.ValidationDepthIgnoreHeader);
            yield return null;

            Assert.AreEqual(1, services.Session.Document.IgnoredWarnings.Count,
                "mục 2 ghi MỘT ghi chú hẹn giờ vào asset (cùng cơ chế với Bỏ qua cảnh báo)");
            Assert.IsTrue(services.Session.Document.IgnoredWarnings[0].IsReminder, "ghi chú hẹn giờ phải có hạn");
            Assert.AreEqual(ReminderDeadlineUtcText, services.Session.Document.IgnoredWarnings[0].ExpiresUtcText,
                "[SD2 §2.4] hạn là lúc đợt đang chạy khép (14/9 00:00 UTC)");
            LogAssert.NoUnexpectedReceived();
        }

        // --------------------------------------------------------------------------- popover Bỏ qua cảnh báo… (ô 5, §2.7)

        [UnityTest]
        public IEnumerator IgnorePopover_RequiresNote()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            yield return Open(services);
            LiveEventCalendarFinding finding = FindFinding(services, LiveEventCalendarRuleIds.LongGapBetweenEvents);
            Assume.That(finding, Is.Not.Null, "lịch mẫu có một khoảng trống dài (Nên xem)");

            var applied = new List<IgnoredCalendarWarning>();
            IgnoreWarningPopover popover = new IgnoreWarningPopover(finding, services.Format, services.LayoutLoader,
                CalendarAssetName(services), applied.Add);
            VisualElement built = popover.BuildForTest();
            _scope.Window.rootVisualElement.Add(built);
            try
            {
                yield return null;

                Assert.IsFalse(popover.ConfirmButton.enabledSelf, "ghi chú rỗng thì không bỏ qua được");
                Assert.AreEqual(LiveOpsHubStrings.ValidationDepthIgnoreNoteRequiredReason, popover.ConfirmSlot.Reason,
                    "SPIKE-B SP-3: lý do khoá in thành chữ cạnh nút");
                Submit(popover.ConfirmButton);
                Assert.AreEqual(0, popover.IgnoreCount, "nút khoá mà vẫn chạy thì lý do chỉ là trang trí");
                Assert.AreEqual(0, applied.Count);

                popover.NoteField.value = IgnoreNote;
                yield return null;

                Assert.IsTrue(popover.ConfirmButton.enabledSelf, "có ghi chú thì bỏ qua được");
                Assert.AreEqual(string.Empty, popover.ConfirmSlot.Reason, "nút mở thì lý do phải biến mất");
                Assert.IsTrue(popover.ScopeToggle.value, "[SD2 §2.7] Toggle phạm vi bật sẵn: bỏ qua gắn với đúng khoảng này");
                // Soát W5 F-2: câu phạm vi PHẢI là Label riêng. Để nó làm label của Toggle thì BaseField vẽ nhãn trước ô tick,
                // câu hai dòng đẩy ô tick ra sát mép phải và cắt cụt còn ~6px — nhìn không ra là phạm vi đang bật.
                Assert.IsTrue(string.IsNullOrEmpty(popover.ScopeToggle.label), "Toggle không được mang câu phạm vi làm label");
                Assert.IsNotNull(popover.ScopeLabel, "câu phạm vi nằm ở Label riêng");
                Assert.AreNotEqual(string.Empty, popover.ScopeLabel.text, "câu phạm vi vẫn phải đọc được");
                Assert.AreSame(popover.ScopeToggle.parent, popover.ScopeLabel.parent, "hai element nằm cùng một hàng");
                Assert.AreSame(popover.ScopeToggle, popover.ScopeLabel.parent[0], "ô tick đứng TRƯỚC câu chữ");
                Submit(popover.ConfirmButton);

                Assert.AreEqual(1, applied.Count, "bấm \"Bỏ qua\" mới ghi cảnh báo");
                Assert.AreEqual(IgnoreNote, applied[0].Note);
                Assert.AreNotEqual(string.Empty, applied[0].RangeStartUtcText, "Toggle bật = giới hạn đúng khoảng của phát hiện");
                Assert.AreEqual(string.Empty, applied[0].ExpiresUtcText, "bỏ qua bằng tay KHÔNG có hạn — hạn chỉ sinh từ Quyết định…");
            }
            finally
            {
                built.RemoveFromHierarchy();
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator IgnorePopover_ScopeOff_MeansEveryRange()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            yield return Open(services);
            LiveEventCalendarFinding finding = FindFinding(services, LiveEventCalendarRuleIds.LongGapBetweenEvents);
            Assume.That(finding, Is.Not.Null);

            var applied = new List<IgnoredCalendarWarning>();
            IgnoreWarningPopover popover = new IgnoreWarningPopover(finding, services.Format, services.LayoutLoader,
                CalendarAssetName(services), applied.Add);
            VisualElement built = popover.BuildForTest();
            _scope.Window.rootVisualElement.Add(built);
            try
            {
                yield return null;
                popover.ScopeToggle.value = false;
                popover.NoteField.value = IgnoreNote;
                yield return null;
                Submit(popover.ConfirmButton);

                Assert.AreEqual(1, applied.Count);
                Assert.AreEqual(string.Empty, applied[0].RangeStartUtcText, "[SD2 §2.7] Toggle phạm vi tắt = mọi khoảng (hai mốc rỗng)");
                Assert.AreEqual(string.Empty, applied[0].RangeEndUtcText);
                Assert.AreEqual(LiveOpsHubStrings.FindingIgnoredEveryRange,
                    LiveOpsFindingText.IgnoredWarningRangeText(applied[0], services.Format), "meta của nó đọc là \"mọi khoảng\"");
            }
            finally
            {
                built.RemoveFromHierarchy();
            }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void IgnoreWarning_SavedInDocument_AndRangeChangeBringsItBack()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarFinding finding = FindFinding(services, LiveEventCalendarRuleIds.LongGapBetweenEvents);
            Assume.That(finding, Is.Not.Null);

            ApplyIgnore(services, finding, IgnoreNote, true, string.Empty);

            Assert.AreEqual(1, services.Session.Document.IgnoredWarnings.Count, "[SD2 §2.7] cảnh báo lưu trong asset để review bằng git");
            Assert.IsNull(FindFinding(services, LiveEventCalendarRuleIds.LongGapBetweenEvents), "bỏ qua rồi thì phát hiện rời danh sách");
            Assert.AreEqual(1, IgnoredFindingCount(services), "nó nằm trong nhóm \"Đã bỏ qua\"");

            // Khoảng đổi (dời đợt sau trễ thêm một ngày) => cảnh báo cũ không còn khớp và phải HIỆN LẠI.
            MoveLateLavaQuestByOneDay(services);

            Assert.IsNotNull(FindFinding(services, LiveEventCalendarRuleIds.LongGapBetweenEvents),
                "[SD2 §2.7] phạm vi gắn với KHOẢNG: khoảng đổi thì cảnh báo hiện lại");
            Assert.AreEqual(1, services.Session.Document.IgnoredWarnings.Count, "cảnh báo cũ vẫn nằm trong asset, chỉ là hết khớp");
        }

        // ------------------------------------------------------------------------------- nhóm "Đã bỏ qua (n)" (ô 6, V-15)

        [UnityTest]
        public IEnumerator IgnoredGroup_UnignoreButton_RemovesWarningOneUndoGroupAndRechecks()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarFinding finding = FindFinding(services, LiveEventCalendarRuleIds.LongGapBetweenEvents);
            Assume.That(finding, Is.Not.Null);
            ApplyIgnore(services, finding, IgnoreNote, true, string.Empty);
            yield return Open(services);

            LiveOpsToastModel toast = null;
            services.Bus.ToastRequested += model => toast = model;
            ValidationIgnoredRow row = FirstIgnoredRow();
            Assert.IsNotNull(row, "nhóm \"Đã bỏ qua\" mở ra phải liệt kê từng mục, không chỉ một dòng mono");
            yield return null;
            Assert.AreEqual(LiveOpsHubStrings.ValidationDepthUnignoreButton, row.UnignoreButton.text,
                "mỗi hàng có nút nhỏ \"Bỏ bỏ qua\" (menu chuột phải không chụp được, S-24)");
            StringAssert.Contains(LiveEventCalendarRuleIds.LongGapBetweenEvents, LiveOpsFindingText.PlainText(row.MetaLabel.text),
                "meta nêu luật · đích · khoảng");
            StringAssert.Contains(IgnoreNote, LiveOpsFindingText.PlainText(row.NoteLabel.text), "ghi chú hiện ngay trên hàng");

            yield return Click(row.UnignoreButton);

            Assert.AreEqual(0, services.Session.Document.IgnoredWarnings.Count, "\"Bỏ bỏ qua\" gỡ cảnh báo khỏi asset");
            Assert.IsNotNull(toast, "một toast Hoàn tác, không hỏi lại (bảng 7.0)");
            Assert.AreNotEqual(LiveOpsToastModel.NoUndoGroup, toast.UndoGroup, "đúng MỘT Undo group");
            StringAssert.Contains(LiveEventCalendarRuleIds.LongGapBetweenEvents, toast.Message, "tên group nêu luật và đích");
            Assert.IsTrue(services.Session.Check.IsRunning, "gỡ xong tự kiểm lại (PD-10) để phát hiện quay lại nhóm Nên xem");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator IgnoredGroup_ContextMenu_UnignoreViewNoteViewInCalendar()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarFinding finding = FindFinding(services, LiveEventCalendarRuleIds.LongGapBetweenEvents);
            Assume.That(finding, Is.Not.Null);
            ApplyIgnore(services, finding, IgnoreNote, true, string.Empty);
            yield return Open(services);

            ValidationIgnoredRow row = FirstIgnoredRow();
            Assume.That(row, Is.Not.Null);
            DropdownMenu menu = new DropdownMenu();
            row.PopulateContextMenu(menu);

            IReadOnlyList<string> labels = MenuLabels(menu);
            Assert.AreEqual(3, labels.Count, "[SD2 §2.7] đúng ba mục");
            Assert.AreEqual(LiveOpsHubStrings.ValidationDepthUnignoreButton, labels[0]);
            Assert.AreEqual(LiveOpsHubStrings.ValidationDepthViewNoteMenuItem, labels[1]);
            Assert.AreEqual(LiveOpsHubStrings.FindingLinkViewInCalendar, labels[2]);

            // "Xem ghi chú" ghim hover card — ghi chú ĐỦ, không rút gọn như trên hàng.
            row.PopulateContextMenu(menu);
            InvokeMenuItem(menu, LiveOpsHubStrings.ValidationDepthViewNoteMenuItem);
            yield return null;

            Assert.IsNotNull(Section.HoverCardHost, "\"Xem ghi chú\" phải ghim một hover card");
            Assert.IsTrue(Section.HoverCardHost.IsPinned, "thẻ GHIM: rời chuột không tắt");
            Assert.IsNotNull(Section.HoverCardHost.Card.Q(LiveOpsHubPaths.ValidationDepthElementNames.IgnoredNoteCard));
            StringAssert.Contains(IgnoreNote, LiveOpsFindingText.PlainText(PinnedNoteText()),
                "thẻ ghim mang ghi chú ĐỦ — đó là lý do tồn tại của \"Xem ghi chú\"");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator IgnoredGroup_ReminderShowsDueTag()
        {
            // Ghi chú hẹn CÒN HẠN: hàng trong nhóm "Đã bỏ qua" mang tag hạn, và meta khoảng đọc "hẹn tới 14/9 00:00"
            // (V-21 CC-VALB-4) — KHÔNG phải "14/9 → mọi": với người đọc đó là một hạn, không phải khoảng bị ẩn.
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarFinding finding = FindFinding(services, LiveEventCalendarRuleIds.LongGapBetweenEvents);
            Assume.That(finding, Is.Not.Null);
            ApplyIgnore(services, finding, IgnoreNote, true, ReminderDeadlineUtcText);
            yield return Open(services);

            ValidationIgnoredRow row = FirstIgnoredRow();
            Assert.IsNotNull(row);
            string expectedRange = LiveOpsFindingText.IgnoredWarningRangeText(services.Session.Document.IgnoredWarnings[0], services.Format);
            Assert.IsNotNull(row.TagLabel, "hàng hẹn giờ phải nhận ra được từ xa giữa những mục bỏ qua vĩnh viễn");
            Assert.AreEqual(expectedRange, row.TagLabel.text);
            StringAssert.Contains(LiveOpsFindingText.PlainText(expectedRange), LiveOpsFindingText.PlainText(row.MetaLabel.text),
                "(CC-VALB-4) meta khoảng của ghi chú hẹn đọc \"hẹn tới …\"");
            _scope.Dispose();
            _scope = null;

            // Hạn ĐÃ QUA: phát hiện quay lại danh sách kèm tag "đã tới hẹn" (V-22 CC-FT-1).
            LiveOpsHubServices dueServices = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarFinding dueFinding = FindFinding(dueServices, LiveEventCalendarRuleIds.LongGapBetweenEvents);
            ApplyIgnore(dueServices, dueFinding, IgnoreNote, true, PassedDeadlineUtcText);
            yield return Open(dueServices);

            ValidationFindingRow backRow = FindRow(LiveEventCalendarRuleIds.LongGapBetweenEvents);
            Assert.IsNotNull(backRow, "hạn qua thì cảnh báo hết ẩn");
            Assert.IsNotNull(backRow.TagLabel, "hàng phải nói được vì sao nó quay lại");
            Assert.AreEqual(LiveOpsFindingText.DueReminderTag, backRow.TagLabel.text);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator IgnoredGroup_LongNote_RowShortensItButPinnedCardKeepsItWhole()
        {
            // Mục 7.5 nói hàng chỉ mang "ghi chú rút gọn". In nguyên văn thì hàng cao dần theo độ dài ghi chú và "Xem ghi chú"
            // mất lý do tồn tại (soát W5 F-6).
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarFinding finding = FindFinding(services, LiveEventCalendarRuleIds.LongGapBetweenEvents);
            Assume.That(finding, Is.Not.Null);
            ApplyIgnore(services, finding, LongIgnoreNote, true, string.Empty);
            yield return Open(services);

            ValidationIgnoredRow row = FirstIgnoredRow();
            Assert.IsNotNull(row);
            string rowNote = LiveOpsFindingText.PlainText(row.NoteLabel.text);
            Assert.AreNotEqual(LongIgnoreNote, rowNote, "ghi chú dài phải bị cắt trên hàng");
            Assert.Less(rowNote.Length, LongIgnoreNote.Length, "bản rút gọn phải ngắn hơn bản đủ");
            string ellipsis = LiveOpsHubStrings.ValidationDepthIgnoredNoteEllipsis;
            Assert.AreEqual(ellipsis, rowNote.Substring(rowNote.Length - ellipsis.Length), "chỗ bị cắt phải nói là đã cắt");
            Assert.AreEqual(LongIgnoreNote.Substring(0, NoteHeadCompareLength), rowNote.Substring(0, NoteHeadCompareLength),
                "phần giữ lại là phần ĐẦU của ghi chú");

            DropdownMenu menu = new DropdownMenu();
            row.PopulateContextMenu(menu);
            InvokeMenuItem(menu, LiveOpsHubStrings.ValidationDepthViewNoteMenuItem);
            yield return null;

            string pinnedNote = LiveOpsFindingText.PlainText(PinnedNoteText());
            Assert.AreEqual(LongIgnoreNote, pinnedNote, "thẻ ghim giữ ghi chú ĐỦ");
            Assert.AreNotEqual(rowNote, pinnedNote, "chữ của hàng phải KHÁC chữ của thẻ ghim, nếu không thẻ ghim là thừa");
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------------------ menu chuột phải của hàng phát hiện

        [UnityTest]
        public IEnumerator ContextMenu_Items()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            yield return Open(services);

            ValidationFindingRow row = FindRow(LiveEventCalendarRuleIds.OverlapSameType);
            Assert.IsNotNull(row, "lịch mẫu có một đợt chồng giờ cùng loại");
            DropdownMenu menu = new DropdownMenu();
            row.PopulateContextMenu(menu);

            IReadOnlyList<string> labels = MenuLabels(menu);
            Assert.AreEqual(4, labels.Count, "mục 7.5: \"Xem trong lịch · Sửa nhanh… · Copy mô tả lỗi · Mở tài liệu luật <id>\"");
            Assert.AreEqual(LiveOpsHubStrings.FindingLinkViewInCalendar, labels[0]);
            Assert.AreEqual(LiveOpsHubStrings.ValidationDepthContextQuickFixMenuItem, labels[1]);
            Assert.AreEqual(LiveOpsHubStrings.ValidationDepthContextCopyDescriptionMenuItem, labels[2]);
            StringAssert.Contains(LiveEventCalendarRuleIds.OverlapSameType, labels[3], "mục cuối nêu id luật");

            InvokeMenuItem(menu, LiveOpsHubStrings.ValidationDepthContextCopyDescriptionMenuItem);
            StringAssert.Contains(LiveEventCalendarRuleIds.OverlapSameType, services.Clipboard.Text,
                "\"Copy mô tả lỗi\" đưa cả id luật vào clipboard để dán được vào ticket");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ContextMenu_QuickFixWithoutRepair_PrintsReasonInLabel()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            yield return Open(services);

            ValidationFindingRow row = FindRowWithoutRepair();
            Assume.That(row, Is.Not.Null, "lịch mẫu có ít nhất một phát hiện không kèm lệnh sửa nào");
            DropdownMenu menu = new DropdownMenu();
            row.PopulateContextMenu(menu);

            IReadOnlyList<string> labels = MenuLabels(menu);
            StringAssert.Contains(LiveOpsHubStrings.ValidationDepthContextQuickFixNoRepairReason, labels[1],
                "SPIKE-B SP-3: mục bị khoá ghi lý do NGAY TRONG NHÃN — menu gốc không có chỗ nào khác để in chữ");
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------------------------------------------- hạ tầng

        private ValidationSection Section => (ValidationSection)_scope.Section;

        private IEnumerator Open(LiveOpsHubServices services)
        {
            _scope = SectionTestScope.Open(new ValidationSection(services));
            yield return _scope.WaitForLayout();
        }

        private IEnumerator ClickHeaderButton(string elementName)
        {
            Button button = _scope.Window.rootVisualElement.Q<Button>(elementName);
            Assert.IsNotNull(button, "section header thiếu nút '" + elementName + "'");
            yield return Click(button);
        }

        private IEnumerator Click(VisualElement element)
        {
            yield return LiveOpsHubWindowTestScope.WaitForLayout(element);
            Vector2 center = element.worldBound.center;
            _scope.Window.SendEvent(new Event { type = EventType.MouseDown, mousePosition = center, button = 0, clickCount = 1 });
            _scope.Window.SendEvent(new Event { type = EventType.MouseUp, mousePosition = center, button = 0, clickCount = 1 });
            yield return null;
        }

        /// <summary>Bấm nút nằm ngoài cửa sổ hub (cây popover gắn rời): gửi thẳng NavigationSubmit như ProposalPopoverTests.</summary>
        private static void Submit(Button button)
        {
            Assert.IsNotNull(button, "thiếu nút");
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }

        /// <summary>Chữ ghi chú trong hover card ghim — Label thứ hai (Label đầu là tiêu đề "Ghi chú bỏ qua · …").</summary>
        private string PinnedNoteText()
        {
            VisualElement card = Section.HoverCardHost.Card.Q(LiveOpsHubPaths.ValidationDepthElementNames.IgnoredNoteCard);
            Assert.IsNotNull(card, "thiếu thẻ ghi chú trong hover card");
            var notes = new List<Label>();
            card.Query<Label>(className: LiveOpsHubClassNames.ValidationIgnoredNoteFull).ToList(notes);
            Assert.AreEqual(1, notes.Count, "thẻ ghim có đúng một Label ghi chú đủ");
            return notes[0].text;
        }

        /// <summary>Số ký tự đầu đem so — đủ để chắc hàng giữ phần ĐẦU, ngắn hơn hẳn mức cắt nên không phụ thuộc con số cắt.</summary>
        private const int NoteHeadCompareLength = 24;

        // Ba số đo của card xem trước ([SD2 §2.5]).
        private const float BulkPreviewRowMinimumHeight = 22f;
        private const float BulkPreviewRowGap = 6f;
        private const float BulkPreviewUndoIndent = 20f;

        private const string HuntBrokenEntryKey = "entry-hunt-broken";
        private const string LavaBrokenEntryKey = "entry-lava-broken";

        /// <summary>Giờ kết thúc của một đợt — đổi nghĩa là lệnh sửa an toàn "chuẩn hoá giờ" đã chạy cho đợt đó.</summary>
        private static string EndTimeTextOf(LiveOpsHubServices services, string entryKey)
        {
            IReadOnlyList<FixedLiveEventEntry> entries = services.Session.Document.FixedEvents;
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].EntryKey == entryKey) return entries[index].EndUtcText;
            }
            return null;
        }

        private static string CardApplyReason(SafeRepairPreviewCard card)
        {
            LiveOpsButtonSlot slot = card.ApplyButton.parent as LiveOpsButtonSlot;
            return slot == null ? string.Empty : slot.Reason;
        }

        private ValidationFindingRow FindRow(string ruleId)
        {
            for (int index = 0; index < Section.VisibleRows.Count; index++)
            {
                LiveEventCalendarFinding finding = Section.VisibleRows[index].Row.Finding;
                if (finding != null && finding.RuleId == ruleId) return Section.VisibleRows[index];
            }
            return null;
        }

        private ValidationFindingRow FindRowWithoutRepair()
        {
            for (int index = 0; index < Section.VisibleRows.Count; index++)
            {
                LiveEventCalendarFinding finding = Section.VisibleRows[index].Row.Finding;
                if (finding != null && finding.Repairs.Count == 0) return Section.VisibleRows[index];
            }
            return null;
        }

        /// <summary>
        /// Nhóm "Đã bỏ qua (n) ▸" mặc định THU GỌN ([SD2 §2.1]): thân bị ẩn bằng class nên hàng bên trong không có layout và
        /// không bấm được. Mọi test của nhóm phải mở nó ra trước, đúng như người dùng phải bấm header trước.
        /// </summary>
        private ValidationIgnoredRow FirstIgnoredRow()
        {
            ValidationGroupCard card = Section.IgnoredCard;
            if (card == null || card.IgnoredRows.Count == 0) return null;
            card.SetCollapsed(false);
            return card.IgnoredRows[0];
        }

        private static IReadOnlyList<string> MenuLabels(DropdownMenu menu)
        {
            var labels = new List<string>();
            List<DropdownMenuItem> items = menu.MenuItems();
            for (int index = 0; index < items.Count; index++)
            {
                DropdownMenuAction action = items[index] as DropdownMenuAction;
                if (action != null) labels.Add(action.name);
            }
            return labels;
        }

        private static void InvokeMenuItem(DropdownMenu menu, string label)
        {
            List<DropdownMenuItem> items = menu.MenuItems();
            for (int index = 0; index < items.Count; index++)
            {
                DropdownMenuAction action = items[index] as DropdownMenuAction;
                if (action != null && action.name == label)
                {
                    action.Execute();
                    return;
                }
            }
            Assert.Fail("menu không có mục '" + label + "'");
        }

        private static LiveEventCalendarRepair RepairWithId(LiveEventCalendarFinding finding, string repairId)
        {
            for (int index = 0; index < finding.Repairs.Count; index++)
            {
                if (finding.Repairs[index].RepairId == repairId) return finding.Repairs[index];
            }
            return null;
        }

        private static LiveEventCalendarFinding FindFinding(LiveOpsHubServices services, string ruleId)
        {
            LiveEventCalendarCheckReport report = services.Session.Check.LastReport;
            if (report == null) return null;
            for (int index = 0; index < report.Findings.Count; index++)
            {
                if (report.Findings[index].RuleId == ruleId) return report.Findings[index];
            }
            return null;
        }

        private static int IgnoredFindingCount(LiveOpsHubServices services)
        {
            LiveEventCalendarCheckReport report = services.Session.Check.LastReport;
            return report == null ? 0 : report.IgnoredFindings.Count;
        }

        /// <summary>Ghi một cảnh báo đã bỏ qua vào asset rồi kiểm lại — cùng dữ liệu mà popover sinh ra.</summary>
        private static void ApplyIgnore(LiveOpsHubServices services, LiveEventCalendarFinding finding, string note,
            bool limitToRange, string expiresUtcText)
        {
            string startText = limitToRange && finding.RangeStartUtc.HasValue ? LiveEventUtcText.Format(finding.RangeStartUtc.Value) : string.Empty;
            string endText = limitToRange && finding.RangeEndUtc.HasValue ? LiveEventUtcText.Format(finding.RangeEndUtc.Value) : string.Empty;
            var warning = new IgnoredCalendarWarning(finding.RuleId, finding.TargetId, startText, endText, note, expiresUtcText);
            services.Session.Apply(new AddIgnoredWarningEdit(warning), LiveOpsHubStrings.ValidationDepthIgnoreHeader);
            services.Session.RunCheckToCompletion();
        }

        /// <summary>Dời đợt lava-quest tháng 10 trễ thêm một ngày qua chính phiên: khoảng trống đổi nên cảnh báo cũ hết khớp.</summary>
        private static void MoveLateLavaQuestByOneDay(LiveOpsHubServices services)
        {
            FixedLiveEventEntry late = null;
            IReadOnlyList<FixedLiveEventEntry> entries = services.Session.Document.FixedEvents;
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].EntryKey == LiveOpsDesignSample.LavaQuestLateEntryKey) late = entries[index];
            }
            Assume.That(late, Is.Not.Null);
            var moved = new FixedLiveEventEntry(late.EntryKey, late.EventId, late.EventType,
                "2026-10-02T00:00:00Z", "2026-10-04T00:00:00Z", late.ConfigKey);
            services.Session.Apply(new ReplaceFixedEventEdit(moved), LiveOpsHubStrings.ValidationDepthIgnoreHeader);
            services.Session.RunCheckToCompletion();
        }

        private static string CalendarAssetName(LiveOpsHubServices services)
        {
            return services.Session.AssetFileName;
        }

        private static LiveOpsHubServices ServicesFor(LiveEventCalendarDocument document)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document)));
            services.Session.RunCheckToCompletion();
            return services;
        }

        /// <summary>Hai đợt khác loại, mỗi đợt một giờ kết thúc viết sai nhưng chuẩn hoá được = ĐÚNG HAI lệnh sửa an toàn.</summary>
        private static LiveEventCalendarDocument TwoBrokenTimesDocument()
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(HuntType, "Săn kho báu", 2, false, HuntConfigKey))
                .WithEventType(new LiveEventTypeDefinition(LavaType, "Nhiệm vụ dung nham", 0, false, LavaConfigKey))
                .WithFixedEvent(new FixedLiveEventEntry("entry-hunt-broken", "hunt-1001", HuntType,
                    "2026-10-01T00:00:00Z", "2026-10-3", HuntConfigKey))
                .WithFixedEvent(new FixedLiveEventEntry("entry-lava-broken", "lava-1101", LavaType,
                    "2026-11-01T00:00:00Z", "2026-11-3", LavaConfigKey))
                .Build();
        }
    }
}
