using System;
using System.Collections;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Màn Loại event trong cửa sổ hub thật (7.2): đủ element ở cả hai ngữ cảnh, bảng theo thứ tự làn (V-12), hàng loại chưa khai
    /// báo mờ kèm nút "Khai báo" (V-17, Hình 10b), chọn ô màu sinh đúng một Undo group mang tên bằng câu toast, và trạng thái
    /// view (loại đang chọn) sống qua một vòng lưu/khôi phục.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class EventTypesSectionTests
    {
        private const string RemoteOnlyType = "lucky-spin";
        private const string SecondRemoteOnlyType = "beach-party";
        private const string StarTournament = "star-tournament";

        /// <summary>Padding của dải loại chưa khai báo trong <c>EventTypesSection.uss</c> — nút "Khai báo" dừng ở mốc này.</summary>
        private const float UnknownBarPadding = 6f;

        /// <summary>Số đo [SD1 §2.1] mục 3: ô màu 18×18 chứa swatch 12×12.</summary>
        private const float ColorSlotSize = 18f;
        private const float ColorSwatchSize = 12f;

        /// <summary>
        /// Nghịch đảo của ngưỡng "chữ chiếm quá 95% bề rộng ô" (W9-25): vùng nội dung phải rộng hơn chữ ít nhất 1/0,95 lần.
        /// <para>
        /// (soát W10 F7) Lấy THẲNG <see cref="UxLayoutAuditor.TextFillLimitRatio"/> chứ không chép lại con số 0,95:
        /// auditor cùng namespace và cùng asmdef với ca này, hằng ấy <c>internal</c> nên gọi được. Chép lại thì hai con số
        /// trôi khỏi nhau mà không gì báo — đúng thứ ca này sinh ra để chặn.
        /// </para>
        /// </summary>
        private const float TextFillCeilingRatio = 1f / UxLayoutAuditor.TextFillLimitRatio;

        /// <summary>
        /// Ô hẹp hơn mức này không áp luật dư 5% — 5% của một ô 20px mỏng hơn cả sai số của <c>MeasureTextSize</c>.
        /// <para>
        /// (cổng đợt W10, G-FIX-W10-1) Nợ F7 của G-W10-FIELD đã trả: hằng bên auditor nay <c>internal</c>, bản chép tay
        /// đã xoá, ca này đọc THẲNG một nguồn duy nhất.
        /// </para>
        /// </summary>
        private const float TextFillMinimumWidth = UxLayoutAuditor.TextFillMinimumWidth;

        /// <summary>
        /// Khoảng dư TỐI THIỂU (px) để một ô được coi là "rộng hơn chữ" — đọc THẲNG
        /// <c>UxLayoutAuditor.TextFillMinimumSlack</c> (cổng đợt W10, G-FIX-W10-1 đã mở hằng ấy thành <c>internal</c>).
        /// <para>
        /// Cổng dùng con số này để BỎ QUA nhãn tự co (dư 0). Ca này dùng nó ngược lại: ô bảng không bao giờ được phép tự
        /// co — nó là ô của một cột có bề rộng khai sẵn — nên "dư 0" ở đây là LỖI, không phải chỗ để bỏ qua. Ca vì thế
        /// NGHIÊM hơn cổng, và cố ý: chỗ 3 của W9-25 (ô giờ Tổng quan) chính là một nhãn tự co trong một ô còn rộng, và
        /// một ca bỏ qua nhãn tự co sẽ xanh trên đúng cái mã chưa sửa.
        /// </para>
        /// </summary>
        private const float TextFillMinimumSlack = UxLayoutAuditor.TextFillMinimumSlack;

        /// <summary>Ký tự "…" của nhãn ĐÃ rút gọn — bản chép của <c>UxLayoutAuditor.EllipsisCharacter</c>.</summary>
        private const char EllipsisCharacter = '\u2026';

        /// <summary>Sai số khi so bề rộng cột: layout UI Toolkit làm tròn theo dpi, so bằng == sẽ đỏ giả.</summary>
        private const float ColumnWidthTolerance = 1f;

        /// <summary>
        /// Sai số DƯỚI một pixel, cho phép so tâm swatch với tâm ô màu (F5): lệch thật là ~6px nên nửa pixel vẫn thoải mái,
        /// mà một sai số 1px thì không còn phân biệt được "đúng tâm" với "lệch một pixel".
        /// </summary>
        private const float SubPixelTolerance = 0.5f;

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        [UnityTest]
        public IEnumerator Section_RequiredElementsPresent_WithSampleData()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section))
            {
                yield return scope.WaitForLayout();
                scope.AssertRequiredElementsPresent();
                Assert.IsFalse(IsHidden(scope.View.Q(LiveOpsHubPaths.EventTypesElementNames.Content)), "có asset và có loại thì hiện bảng");
                Assert.IsTrue(IsHidden(scope.View.Q(LiveOpsHubPaths.EventTypesElementNames.Empty)));
                Assert.AreEqual(section.Model.FooterText, scope.View.Q<Label>(LiveOpsHubPaths.EventTypesElementNames.Footer).text);
            }
        }

        [UnityTest]
        public IEnumerator Section_RequiredElementsPresent_WithoutAsset()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario);
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section))
            {
                yield return scope.WaitForLayout();
                scope.AssertRequiredElementsPresent();
                Assert.IsTrue(IsHidden(scope.View.Q(LiveOpsHubPaths.EventTypesElementNames.Content)), "chưa có asset thì không vẽ bảng");
                Assert.IsFalse(IsHidden(scope.View.Q(LiveOpsHubPaths.EventTypesElementNames.Empty)));
                Assert.AreEqual(LiveOpsHubStrings.EventTypesEmptyNoAssetTitle, scope.View.Q<Label>("event-types-empty-title").text,
                    "(7.0) mọi màn trừ Tổng quan nói cùng một câu khi chưa có lịch");
                Button openOverview = scope.View.Q<Button>(className: LiveOpsHubClassNames.ButtonPrimary);
                Assert.IsNotNull(openOverview, "trạng thái trống luôn có một bước tiếp");
                Assert.AreEqual(LiveOpsHubStrings.EventTypesEmptyNoAssetButton, openOverview.text);
            }
        }

        [UnityTest]
        public IEnumerator UnknownTypeFromRemote_DimRowWithDeclareButton()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            services.Session.Remote.Set(RemoteJsonWithLuckySpin(), LiveOpsDesignSample.NowUtc);
            services.Session.RunCheckToCompletion();

            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section))
            {
                yield return scope.WaitForLayout();

                VisualElement unknownBar = scope.View.Q(LiveOpsHubPaths.EventTypesElementNames.UnknownBar);
                Assert.IsFalse(IsHidden(unknownBar), "(V-17) có loại chưa khai báo thì dòng Blocked dưới bảng phải hiện");
                Button declare = unknownBar.Q<Button>(EventTypesSection.DeclareButtonName);
                Assert.IsNotNull(declare, "dòng Blocked luôn kèm nút 'Khai báo'");
                Assert.AreEqual(LiveOpsHubStrings.EventTypesDeclareButton, declare.text);

                List<VisualElement> dimCells = scope.View.Query(className: LiveOpsHubClassNames.EventTypesRowUndeclared).ToList();
                Assert.Greater(dimCells.Count, 0, "hàng loại chưa khai báo phải mang class mờ (Hình 10b)");

                VisualElement referenceCard = scope.View.Q(LiveOpsHubPaths.EventTypesElementNames.ReferenceCard);
                Assert.IsFalse(IsHidden(referenceCard), "card tham chiếu phát hiện của Kiểm lịch phải hiện");
                Label headline = referenceCard.Q<Label>(className: LiveOpsHubClassNames.TextBlocked);
                Assert.IsNotNull(headline);
                Assert.IsTrue(headline.text.StartsWith(LiveOpsHubStrings.EventTypesReferencePrefix, StringComparison.Ordinal),
                    "(V-22 CC-FT-2 (a)) chỉ màn này ghép tiền tố 'Kiểm lịch · '");

                Click(declare);
                yield return null;
                Assert.IsTrue(services.Session.Document.TryGetEventType(RemoteOnlyType, out LiveEventTypeDefinition declared),
                    "'Khai báo' phải thêm định nghĩa loại vào nháp");
                Assert.AreEqual(LiveEventTypeColorSlots.DefaultSlotFor(RemoteOnlyType), declared.ColorSlot,
                    "loại mới lấy ô màu theo băm, đổi được sau ở inspector");
            }
        }

        /// <summary>
        /// Nút chính "Thêm loại" ([SD1 §2.1], 7.2 "[Toolbar Plus] Thêm loại"): icon đứng TRƯỚC chữ. <c>Button.text</c> được
        /// chính Button vẽ trên toàn bộ hộp nội dung nên Image con chỉ chồng lên chữ — đó là lỗi của ảnh h10 lần trước, và nó
        /// KHÔNG lộ ra ở bất kỳ số đo khung nào, phải đo bằng vị trí hai con.
        /// </summary>
        [UnityTest]
        public IEnumerator AddTypeButton_IconBeforeLabel_NotOverlapping()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section))
            {
                yield return scope.WaitForLayout();

                Button addButton = scope.Window.rootVisualElement.Q<Button>(EventTypesSection.AddButtonName);
                Assert.IsNotNull(addButton, "section header phải có nút chính của màn");
                Assert.IsTrue(string.IsNullOrEmpty(addButton.text),
                    "nút có icon thì KHÔNG dùng Button.text — chữ của nút tự vẽ đè lên icon con");

                VisualElement icon = addButton.Q(EventTypesSection.AddButtonIconName);
                Label label = addButton.Q<Label>(EventTypesSection.AddButtonLabelName);
                Assert.IsNotNull(icon, "icon plus của nút");
                Assert.IsNotNull(label, "chữ của nút nằm ở Label con");
                Assert.AreEqual(LiveOpsHubStrings.EventTypesAddTypeButton, label.text);

                Assert.Greater(label.worldBound.width, 0f, "chữ phải đo được (sheet của màn đã tới nút trong section header)");
                Assert.LessOrEqual(icon.worldBound.xMax, label.worldBound.xMin + 0.5f, "icon đứng trước chữ, không chồng lên chữ");
                Assert.LessOrEqual(addButton.worldBound.xMin, icon.worldBound.xMin, "icon nằm trong nút");
                Assert.GreaterOrEqual(addButton.worldBound.xMax, label.worldBound.xMax, "chữ không tràn khỏi nút");
            }
        }

        /// <summary>
        /// Hai loại lạ cùng lúc (bản dán có hai loại nháp không khai) — dải phải XẾP CHỒNG chứ không bóp cạnh nhau, và mỗi nút
        /// "Khai báo" ra mép phải ([SD1 §2.2] "chữ + giãn + nút Khai báo"). Một loại lạ không lộ được cả hai lỗi này.
        /// </summary>
        [UnityTest]
        public IEnumerator TwoUnknownTypes_RowsStackAndDeclareSitsAtRightEdge()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            services.Session.Remote.Set(RemoteJsonWithTwoUnknownTypes(), LiveOpsDesignSample.NowUtc);
            services.Session.RunCheckToCompletion();

            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section))
            {
                yield return scope.WaitForLayout();

                VisualElement unknownBar = scope.View.Q(LiveOpsHubPaths.EventTypesElementNames.UnknownBar);
                Assert.IsFalse(IsHidden(unknownBar));
                List<VisualElement> rows = unknownBar.Query(className: LiveOpsHubClassNames.EventTypesUnknownRow).ToList();
                Assert.AreEqual(2, rows.Count, "mỗi loại chưa khai báo một hàng");

                Assert.GreaterOrEqual(rows[1].worldBound.yMin, rows[0].worldBound.yMax - 0.5f,
                    "hai hàng xếp chồng — dải là cột, không phải hàng ngang");
                for (int index = 0; index < rows.Count; index++)
                {
                    VisualElement row = rows[index];
                    Assert.GreaterOrEqual(row.worldBound.width, unknownBar.worldBound.width - (2f * UnknownBarPadding) - 1f,
                        "hàng trải hết bề ngang của dải — dải là cột, hàng ngang thì hàng chỉ rộng bằng nội dung");
                    Button declare = row.Q<Button>(EventTypesSection.DeclareButtonName);
                    Assert.IsNotNull(declare, "hàng nào cũng có nút 'Khai báo'");
                    // Cộng margin-right của chính nút (Editor đặt sẵn 3px cho Button; .liveops-hub-button chỉ đè margin-left)
                    // để so mép HỘP LỀ với mép hàng — nút nằm sát mép phải khi hai mốc này trùng nhau.
                    Assert.GreaterOrEqual(declare.worldBound.xMax + declare.resolvedStyle.marginRight, row.worldBound.xMax - 0.5f,
                        "chữ giãn đẩy nút 'Khai báo' ra mép phải ([SD1 §2.2] 'chữ + giãn + nút Khai báo')");
                }
            }
        }

        /// <summary>[SD1 §2.2] "card chứa hàng phát hiện (sọc Blocked, icon err)" — sọc không thay được icon.</summary>
        [UnityTest]
        public IEnumerator ReferenceCard_FindingRowHasErrorIcon()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            services.Session.Remote.Set(RemoteJsonWithLuckySpin(), LiveOpsDesignSample.NowUtc);
            services.Session.RunCheckToCompletion();

            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section))
            {
                yield return scope.WaitForLayout();

                VisualElement referenceCard = scope.View.Q(LiveOpsHubPaths.EventTypesElementNames.ReferenceCard);
                Assert.IsFalse(IsHidden(referenceCard));
                VisualElement icon = referenceCard.Q(EventTypesSection.ReferenceIconElementName);
                Assert.IsNotNull(icon, "hàng phát hiện của card tham chiếu phải có icon err");
                Label headline = referenceCard.Q<Label>(className: LiveOpsHubClassNames.TextBlocked);
                Assert.IsNotNull(headline);
                Assert.LessOrEqual(icon.worldBound.xMax, headline.worldBound.xMin + 0.5f, "icon mở đầu hàng, trước chữ");
            }
        }

        /// <summary>
        /// Bấm lại đúng ô màu đang bật: tám ô là MỘT nhóm, luôn có đúng một ô bật ([SD1 §2.1] mục 3). ToolbarToggle tự lật về
        /// false, mà tài liệu không đổi nên không có lần vẽ lại nào sửa giúp — màn phải trả ô về bật ngay tại chỗ.
        /// </summary>
        [UnityTest]
        public IEnumerator PickSameColorAgain_KeepsSlotOnWithoutEditing()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            List<LiveOpsToastModel> toasts = new List<LiveOpsToastModel>();
            services.Bus.ToastRequested += toasts.Add;

            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section))
            {
                yield return scope.WaitForLayout();
                section.ApplyNavigation(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.EventTypes).WithEventType(StarTournament, false));
                yield return null;

                Assert.IsTrue(services.Session.Document.TryGetEventType(StarTournament, out LiveEventTypeDefinition before));
                VisualElement slots = scope.View.Q(EventTypeInspector.ColorSlotsElementName);
                ToolbarToggle current = (ToolbarToggle)slots[before.ColorSlot];
                Assert.IsTrue(current.value, "ô màu của loại đang chọn phải đang bật");

                current.value = false;
                yield return null;

                Assert.IsTrue(current.value, "bấm lại ô đang bật = chọn lại chính nó, không phải bỏ chọn");
                Assert.IsTrue(services.Session.Document.TryGetEventType(StarTournament, out LiveEventTypeDefinition after));
                Assert.AreEqual(before.ColorSlot, after.ColorSlot, "không có lệnh sửa nào");
                Assert.AreEqual(0, toasts.Count, "không sửa gì thì không toast");
            }
        }

        /// <summary>
        /// Dãy tám ô màu: ô 18×18 chứa swatch 12×12 ([SD1 §2.1] mục 3). Hai cỡ này KHÔNG đo được trên ảnh chụp — ToolbarToggle
        /// không tự vẽ viền nên dò cạnh chỉ thấy swatch bên trong — nên chốt bằng <c>resolvedStyle</c> ở đây.
        /// </summary>
        [UnityTest]
        public IEnumerator ColorSlots_MatchDesignSize()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section))
            {
                yield return scope.WaitForLayout();
                section.ApplyNavigation(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.EventTypes).WithEventType(StarTournament, false));
                yield return null;

                VisualElement slots = scope.View.Q(EventTypeInspector.ColorSlotsElementName);
                Assert.AreEqual(LiveEventTypeDefinition.ColorSlotCount, slots.childCount);
                for (int slot = 0; slot < slots.childCount; slot++)
                {
                    VisualElement toggle = slots[slot];
                    Assert.AreEqual(EventTypeInspector.ColorSlotElementName(slot), toggle.name,
                        "ô màu phải có tên element — lệnh chụp chỉ ghi số đo element có tên");
                    Assert.AreEqual(ColorSlotSize, toggle.resolvedStyle.width, 0.01f, "ô màu rộng 18");
                    Assert.AreEqual(ColorSlotSize, toggle.resolvedStyle.height, 0.01f, "ô màu cao 18");
                    VisualElement swatch = toggle.Q(className: LiveOpsHubClassNames.EventTypesColorSwatch);
                    Assert.IsNotNull(swatch, "mỗi ô màu chứa một swatch");
                    Assert.AreEqual(ColorSwatchSize, swatch.resolvedStyle.width, 0.01f, "swatch trong ô màu 12×12");
                    Assert.AreEqual(ColorSwatchSize, swatch.resolvedStyle.height, 0.01f);
                }
            }
        }

        /// <summary>
        /// Popover "Thêm loại" là ĐƯỜNG DUY NHẤT của luồng thêm loại ở 7.2: ba lý do khoá (id rỗng / sai ký tự / trùng loại đã
        /// có) in thành chữ cạnh nút (SPIKE-B SP-3), và nút mở lại ngay khi id hợp lệ.
        /// </summary>
        [UnityTest]
        public IEnumerator AddPopover_BlocksBadId_PrintsReasonNextToButton()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section))
            {
                yield return scope.WaitForLayout();
                yield return AssertAddPopoverReasons(scope);
            }
        }

        /// <summary>
        /// Gõ vào ô id rồi đọc lý do cạnh nút. Cây của popover phải nằm trong panel thật: <c>TextField.value</c> gửi
        /// <c>ChangeEvent</c> qua dispatcher của panel — cây rời panel thì callback không bao giờ chạy và test xanh giả.
        /// </summary>
        private static IEnumerator AssertAddPopoverReasons(SectionTestScope scope)
        {
            AddEventTypePopover popover = new AddEventTypePopover(new[] { StarTournament }, (typeId, displayName) => { });
            VisualElement built = popover.BuildForTest();
            scope.Window.rootVisualElement.Add(built);
            try
            {
                yield return null;

                Assert.IsTrue(popover.ConfirmSlot.IsBlocked, "chưa gõ gì thì nút chính khoá");
                Assert.AreEqual(LiveOpsHubStrings.EventTypesAddPopoverIdEmptyReason, popover.ConfirmSlot.Reason);

                popover.TypeIdField.value = "lucky" + LiveEventInstance.ReservedSeparator + "spin";
                Assert.IsTrue(popover.ConfirmSlot.IsBlocked, "ký tự core cấm trong id thì khoá");
                Assert.AreEqual(LiveOpsHubStrings.EventTypesAddPopoverIdCharsetReason, popover.ConfirmSlot.Reason);

                popover.TypeIdField.value = StarTournament;
                Assert.IsTrue(popover.ConfirmSlot.IsBlocked, "trùng loại đã có thì khoá — báo ngay khi gõ, không đợi bấm Thêm");
                Assert.AreEqual(LiveOpsHubStrings.EventTypesAddPopoverIdDuplicateReason, popover.ConfirmSlot.Reason);

                popover.TypeIdField.value = RemoteOnlyType;
                Assert.IsFalse(popover.ConfirmSlot.IsBlocked, "id hợp lệ thì nút mở lại");
                Assert.AreEqual(string.Empty, popover.ConfirmSlot.Reason);
            }
            finally
            {
                built.RemoveFromHierarchy();
            }
        }

        /// <summary>Bấm "Thêm" gửi đúng (id, tên hiển thị) cho màn — popover không tự đụng phiên.</summary>
        [UnityTest]
        public IEnumerator AddPopover_Confirm_SendsTypeIdAndDisplayName()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section))
            {
                yield return scope.WaitForLayout();

                string capturedTypeId = null;
                string capturedDisplayName = null;
                AddEventTypePopover popover = new AddEventTypePopover(new[] { StarTournament },
                    (typeId, displayName) =>
                    {
                        capturedTypeId = typeId;
                        capturedDisplayName = displayName;
                    });
                // Popover mở trong cửa sổ popup riêng; test gắn cây đã dựng vào panel thật để gửi được sự kiện bấm.
                VisualElement built = popover.BuildForTest();
                scope.Window.rootVisualElement.Add(built);
                try
                {
                    yield return null;
                    popover.TypeIdField.value = RemoteOnlyType;
                    popover.DisplayNameField.value = "Vòng quay may mắn";
                    yield return null;

                    Click(built.Q<Button>(AddEventTypePopover.ConfirmButtonName));
                    yield return null;

                    Assert.AreEqual(RemoteOnlyType, capturedTypeId);
                    Assert.AreEqual("Vòng quay may mắn", capturedDisplayName);
                }
                finally
                {
                    built.RemoveFromHierarchy();
                }
            }
        }

        [UnityTest]
        public IEnumerator Table_FollowsLaneOrder_UntilColumnSorted()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section))
            {
                yield return scope.WaitForLayout();

                IReadOnlyList<EventTypeRow> visible = section.Table.VisibleRows;
                Assert.AreEqual(services.Session.Document.EventTypes.Count, visible.Count);
                for (int index = 0; index < visible.Count; index++)
                {
                    Assert.AreEqual(services.Session.Document.EventTypes[index].TypeId, visible[index].TypeId,
                        "(V-12) chưa bật sort cột thì bảng theo đúng thứ tự làn");
                }
                Assert.IsTrue(LiveOpsTableSorting.IsCustomSortingEnabled(section.Table.View),
                    "bấm tiêu đề cột mới là đường sắp lại — kéo hàng không có ở P1 (V-12)");
            }
        }

        // ======================================================== W9-05: cột bảng co theo bề rộng, và co thì phải NÓI

        /// <summary>
        /// Bậc cột theo bề rộng là một hàm thuần, nên khoá bằng test đơn vị chứ không chỉ bằng một lượt dựng cửa sổ.
        /// Mốc: ba cột luôn hiện tốn 44 + 128 + 128 = 300px, cộng 14px trừ sẵn cho thanh cuộn dọc = 314px. Mỗi cột phụ
        /// tiếp theo cộng thêm đúng bề rộng tối thiểu ĐỌC ĐƯỢC của nó: 116 (Cách vào) → 140 (Khoá config) → 88 (Nguồn)
        /// → 56 (Đợt).
        /// <para>
        /// (W9-31) Ba chứ không còn bốn: cột 36px không tên đã bị gỡ, dấu trạng thái dọn về ô màu. Mốc lùi 36px của cột
        /// đã gỡ rồi tiến lại 8px vì bề rộng tối thiểu của cột "Tên hiển thị" nới từ 120 lên 128 — xem chú thích của
        /// <c>DisplayNameColumnMinimumWidth</c>: gỡ cột trạng thái là lần đầu cột giãn ấy chạm đáy của chính nó.
        /// </para>
        /// </summary>
        [Test]
        public void OptionalColumnCountThatFits_KeepsAsManyColumnsAsReallyFit()
        {
            Assert.AreEqual(-1, EventTypeTable.OptionalColumnCountThatFits(float.NaN), "chưa có số đo thì KHÔNG dựng lại cột");
            Assert.AreEqual(-1, EventTypeTable.OptionalColumnCountThatFits(0f), "bề rộng 0 là cây chưa layout, không phải bảng hẹp");

            Assert.AreEqual(0, EventTypeTable.OptionalColumnCountThatFits(314f), "vừa đúng ba cột luôn hiện");
            Assert.AreEqual(0, EventTypeTable.OptionalColumnCountThatFits(429f), "thiếu 1px cho cột Cách vào thì KHÔNG lấy nó");
            Assert.AreEqual(1, EventTypeTable.OptionalColumnCountThatFits(430f));
            Assert.AreEqual(2, EventTypeTable.OptionalColumnCountThatFits(570f));
            Assert.AreEqual(3, EventTypeTable.OptionalColumnCountThatFits(658f));
            Assert.AreEqual(4, EventTypeTable.OptionalColumnCountThatFits(714f));
            Assert.AreEqual(4, EventTypeTable.OptionalColumnCountThatFits(1600f), "rộng bao nhiêu cũng chỉ có bốn cột phụ");
        }

        /// <summary>
        /// Bỏ cột theo TIỀN TỐ: thiếu chỗ cho cột thứ hai thì mọi cột sau nó cũng bị bỏ, kể cả cột "Đợt" chỉ cần 56px.
        /// Nhặt cột nào vừa thì lấy sẽ làm THỨ TỰ cột đổi theo bề rộng cửa sổ và người dùng mất mốc đọc.
        /// </summary>
        [Test]
        public void OptionalColumnCountThatFits_DropsBySuffix_NotByWhicheverFits()
        {
            // 314 + 116 = 430 đủ cho cột Cách vào; thêm 56 nữa (486) vẫn KHÔNG đủ cho cột thứ hai (140) — và cột "Đợt"
            // (56px, đứng thứ tư) vẫn không được nhảy cóc lên thay chỗ.
            Assert.AreEqual(1, EventTypeTable.OptionalColumnCountThatFits(486f));
        }

        /// <summary>
        /// Cửa sổ hẹp làm bảng bỏ bớt cột — và việc đó phải được KHAI ra chữ (soát W9 R-01). Giấu im lặng thì người đọc
        /// tưởng bảng chỉ có bấy nhiêu cột và không bao giờ biết phải nới cửa sổ hay nhìn sang pane chi tiết.
        /// </summary>
        [UnityTest]
        public IEnumerator Table_AtNarrowWindow_SaysWhichColumnsItHides()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section, 700, 560))
            {
                yield return scope.WaitForLayout();

                IReadOnlyList<string> hidden = section.Table.HiddenColumnTitles;
                Assert.Greater(hidden.Count, 0, "ở 700px bảng không thể chứa đủ bảy cột — nếu không cột nào bị bỏ thì "
                    + "hoặc số đo đã đổi, hoặc bảng đang bóp cột thay vì bỏ cột");

                VisualElement note = scope.View.Q(EventTypesSection.ColumnsHiddenNoteName);
                Label noteText = scope.View.Q<Label>(EventTypesSection.ColumnsHiddenNoteTextName);
                Assert.IsNotNull(note, "phải có dòng khai báo cột bị ẩn dưới bảng");
                Assert.IsFalse(note.ClassListContains(LiveOpsHubClassNames.EventTypesHidden),
                    "có cột bị bỏ thì dòng khai báo phải HIỆN");
                foreach (string title in hidden)
                {
                    StringAssert.Contains(title, noteText.text, "dòng khai báo phải gọi TÊN cột bị bỏ, không nói chung chung");
                }

                StringAssert.Contains(noteText.text, note.tooltip,
                    "tooltip nhắc lại đúng câu đó cho người muốn đọc lại khi câu đã xuống nhiều dòng");
            }
        }

        /// <summary>Cửa sổ đủ rộng thì không cột nào bị bỏ và dòng khai báo phải BIẾN MẤT, không để lại câu cũ.</summary>
        [UnityTest]
        public IEnumerator Table_AtWideWindow_ShowsEveryColumnAndHidesTheNote()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section, 1280, 760))
            {
                yield return scope.WaitForLayout();

                Assert.AreEqual(0, section.Table.HiddenColumnTitles.Count, "1280px chứa đủ bảy cột");
                VisualElement note = scope.View.Q(EventTypesSection.ColumnsHiddenNoteName);
                Assert.IsTrue(note.ClassListContains(LiveOpsHubClassNames.EventTypesHidden),
                    "không bỏ cột nào thì dòng khai báo phải ẩn");
            }
        }

        /// <summary>
        /// Ca của soát W9 R-07: đang sắp theo một cột PHỤ rồi thu hẹp cửa sổ tới mức cột đó biến mất. Danh sách cột được
        /// dựng lại, nên mô tả sắp xếp trỏ vào một <c>Column</c> đã rời bảng phải bị gỡ và bảng về THỨ TỰ LÀN — giữ một
        /// thứ tự mà người dùng không còn nhìn thấy lý do là tệ hơn hẳn.
        /// </summary>
        [UnityTest]
        public IEnumerator Table_SortedByOptionalColumn_ThenNarrowed_FallsBackToLaneOrder()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section, 1280, 760))
            {
                yield return scope.WaitForLayout();
                Assert.AreEqual(0, section.Table.HiddenColumnTitles.Count, "nền của ca này là bảng ĐỦ bảy cột");

                section.Table.View.sortColumnDescriptions.Add(
                    new SortColumnDescription(EventTypeTable.SourceColumnName, SortDirection.Descending));
                yield return null;

                scope.Window.position = new UnityEngine.Rect(0, 0, 700, 560);
                yield return scope.WaitForLayout();
                // W9-16: chờ theo ĐIỀU KIỆN + hạn giờ thay vì đếm ba khung hình. Bảng dựng lại bộ cột trong một lượt
                // schedule.Execute nối sau lượt layout, nên "ba khung" là đủ hay không tuỳ máy đang bận tới đâu — đo
                // được trên chính cây này: 2/6 lượt đỏ ngay sau khi Unity vừa import lại USS, 4/6 lượt xanh. Điều kiện
                // dưới đây hỏi đúng cái đang chờ: bảng đã ÁP XONG bộ cột hợp với bề rộng nó đang có hay chưa.
                yield return UxEventSender.WaitUntil(
                    () => section.Table.AppliedOptionalColumnCount
                        == EventTypeTable.OptionalColumnCountThatFits(section.Table.View.resolvedStyle.width),
                    "bảng Loại event chưa áp xong bộ cột cho bề rộng mới sau khi thu hẹp cửa sổ còn 700px");

                Assert.Greater(section.Table.HiddenColumnTitles.Count, 0,
                    "700px phải bỏ bớt cột — ca này mới có nghĩa (bề rộng bảng đo được: "
                    + section.Table.View.resolvedStyle.width.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "px)");
                foreach (SortColumnDescription description in section.Table.View.sortColumnDescriptions)
                {
                    Assert.AreNotEqual(EventTypeTable.SourceColumnName, description.columnName,
                        "cột đã rời bảng thì mô tả sắp xếp của nó phải bị gỡ, không được trỏ vào Column đã bỏ");
                }

                IReadOnlyList<EventTypeRow> visible = section.Table.VisibleRows;
                for (int index = 0; index < visible.Count; index++)
                {
                    Assert.AreEqual(services.Session.Document.EventTypes[index].TypeId, visible[index].TypeId,
                        "không còn cột nào đang sắp thì bảng về đúng thứ tự làn (V-12)");
                }
            }
        }

        [UnityTest]
        public IEnumerator PickColor_AppliesOneUndoGroupAndShowsUndoToast()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            List<LiveOpsToastModel> toasts = new List<LiveOpsToastModel>();
            services.Bus.ToastRequested += toasts.Add;

            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section))
            {
                yield return scope.WaitForLayout();
                section.ApplyNavigation(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.EventTypes).WithEventType(StarTournament, false));
                yield return null;

                VisualElement slots = scope.View.Q(EventTypeInspector.ColorSlotsElementName);
                Assert.IsNotNull(slots, "inspector phải có dãy tám ô màu");
                Assert.AreEqual(LiveEventTypeDefinition.ColorSlotCount, slots.childCount);
                ToolbarToggle target = (ToolbarToggle)slots[2];
                target.value = true;
                yield return null;

                Assert.IsTrue(services.Session.Document.TryGetEventType(StarTournament, out LiveEventTypeDefinition type));
                Assert.AreEqual(2, type.ColorSlot, "bấm ô màu ghi thẳng vào nháp");
                Assert.AreEqual(1, toasts.Count, "một lệnh sửa = một toast");
                Assert.IsTrue(toasts[0].HasUndo, "toast của lệnh sửa luôn có Hoàn tác");
                Assert.AreEqual(toasts[0].Message, Undo.GetCurrentGroupName(),
                    "(4.3) tên Undo group bằng đúng câu toast — Hoàn tác và câu người dùng đọc không được lệch");
            }
        }

        [UnityTest]
        public IEnumerator SelectedType_SurvivesViewStateRoundTrip()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            EventTypesSection first = new EventTypesSection(services);
            string captured;
            using (SectionTestScope scope = SectionTestScope.Open(first))
            {
                yield return scope.WaitForLayout();
                first.ApplyNavigation(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.EventTypes).WithEventType(StarTournament, false));
                captured = first.CaptureViewState();
            }
            Assert.IsNotEmpty(captured);

            EventTypesSection second = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(second))
            {
                yield return scope.WaitForLayout();
                second.RestoreViewState(captured);
                yield return null;
                Assert.AreEqual(StarTournament, second.SelectedTypeId, "loại đang chọn sống qua domain reload (7.0)");
            }
        }

        [UnityTest]
        public IEnumerator RestoreViewState_BrokenJson_DoesNotThrow()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section))
            {
                yield return scope.WaitForLayout();
                Assert.DoesNotThrow(() => section.RestoreViewState("{ khong-phai-json"),
                    "(7.0) JSON của bản trước không được làm sập màn");
            }
        }

        // ======================================================== W9-31: bảng không còn cột trống không tên

        /// <summary>
        /// Không cột nào của bảng được mang tiêu đề RỖNG. Cột 36px "state" cũ có tiêu đề rỗng và chỉ vẽ dấu cho loại CHƯA
        /// KHAI — mà mẫu thiết kế không có loại nào chưa khai, nên ở mọi ảnh, mọi cỡ, nó là một cột trống có đường kẻ chia
        /// cột riêng: người soát đọc thành "bảng vỡ cột" và đọc đúng cái mình thấy (W9-31).
        /// <para>
        /// Kiểm ĐẦU CỘT chứ không kiểm "bảng có bao nhiêu cột": con số cột còn đổi theo bề rộng cửa sổ, còn luật "cột nào
        /// cũng phải tự giới thiệu được" thì đúng ở mọi cỡ.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Table_EveryColumnHasATitle()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section, 1280, 760))
            {
                yield return scope.WaitForLayout();

                Assert.Greater(section.Table.View.columns.Count, 0, "bảng phải có cột — không có thì ca này thành lời khai suông");
                foreach (Column column in section.Table.View.columns)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(column.title),
                        "cột '" + column.name + "' không có tiêu đề: một cột không tự giới thiệu được thì người đọc chỉ thấy "
                        + "một dải trống có đường kẻ riêng (W9-31)");
                }
            }
        }

        /// <summary>
        /// (W9-25) Ô chữ của bảng phải còn KHOẢNG DƯ ở cỡ cửa sổ HẸP NHẤT của bộ ảnh — đúng chỗ mọi cột rơi xuống bề rộng
        /// tối thiểu của chính nó, tức chỗ duy nhất các con số <c>*ColumnMinimumWidth</c> thật sự được thử.
        /// <para>
        /// Vì sao phải có ca này: bề rộng tối thiểu của cột "Tên hiển thị" khai 120px = 111px chữ dài nhất + 8px đệm ô,
        /// quên mất phần dư — nhưng cột ấy GIÃN nên trước W9-31 nó không bao giờ chạm đáy, và con số sai nằm im suốt chín
        /// đợt. Gỡ cột trạng thái 36px trả chỗ cho một cột phụ nữa ở 700px, cột tên hiển thị rơi xuống 120 và chữ lập tức
        /// chiếm 99,1% ô.
        /// </para>
        /// <para>
        /// (soát W10 F2) Lời khai cũ của ca này — "chỗ duy nhất mọi <c>*ColumnMinimumWidth</c> thật sự được thử" — ĐO RA LÀ
        /// SAI, và số đo nói thẳng: ở 700×560 bảng rộng 424px, giữ 0 cột phụ, nên cột "Tên hiển thị" (cột GIÃN) nhận hết
        /// chỗ thừa và rộng <b>240px</b>, cách đáy 128 của nó rất xa. Con số 99,1% từng thấy ở lượt 2022.3 là của trạng
        /// thái TRUNG GIAN: khi đáy còn 120 thì 44+128+120+116 = 408 ≤ 410 nên bảng còn giữ ĐƯỢC một cột phụ và cột tên
        /// hiển thị rơi xuống 120; nâng đáy lên 128 làm 300+116 = 416 &gt; 410, bảng bỏ luôn cột phụ ấy và cột tên hiển thị
        /// giãn trở lại. Nói cách khác cỡ 700×560 KHÔNG còn ép được cột này xuống đáy.
        /// </para>
        /// <para>
        /// Nên ca này có ba vế và vế chứng minh con số 128 là vế ĐƠN VỊ (vế 2), không phải vế bố cục:
        /// vế 1 chỉ chốt "cột không bao giờ hẹp hơn đáy đã khai" (bố cục tôn trọng <c>minWidth</c>);
        /// vế 2 kiểm chính hằng ấy — chữ dài nhất × 1/0,95 + đệm ô thật — nên nó bác con số 120 cũ mà không cần dựng lại
        /// cửa sổ với 120, và nó đúng ở mọi bản Unity vì không phụ thuộc bố cục;
        /// vế 3 đo mọi ô chữ của bảng ở cỡ hẹp nhất như cũ.
        /// Đáy 128 còn được khoá LẦN HAI ở <c>OptionalColumnCountThatFits_KeepsAsManyColumnsAsReallyFit</c>: mọi mốc bậc
        /// cột (314/430/570/658/714) đều tính từ <c>AlwaysVisibleColumnsWidth</c>, tức đổi 128 là năm con số ấy đỏ.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Table_AtNarrowWindow_KeepsSlackInEveryCell()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section, 700, 560))
            {
                yield return scope.WaitForLayout();
                yield return scope.WaitForLayout();

                List<Label> cells = new List<Label>();
                scope.View.Query<Label>(className: LiveOpsHubClassNames.EventTypesCell).ToList(cells);

                // --- Vế 1: bố cục phải TÔN TRỌNG đáy đã khai của cột "Tên hiển thị" (ở 700x560 cột này đang giãn tới
                // 240px — xem chú thích của ca; vế chứng minh con số 128 là vế 2, không phải vế này).
                // Nhận ô theo CHỮ của nó (tên hiển thị là dữ liệu, các cột khác in id kebab-case hoặc câu cố định của hub),
                // không theo chỉ số ô trong hàng: thứ tự ô của MultiColumnListView là chi tiết nội bộ của Unity.
                HashSet<string> displayNames = new HashSet<string>(StringComparer.Ordinal);
                IReadOnlyList<EventTypeRow> rows = section.Table.VisibleRows;
                for (int index = 0; index < rows.Count; index++)
                {
                    if (rows[index].DisplayName.Length > 0) displayNames.Add(rows[index].DisplayName);
                }

                Label widest = null;
                float widestNeeded = 0f;
                float displayNameColumnWidth = 0f;
                float displayNameCellPadding = 0f;
                for (int index = 0; index < cells.Count; index++)
                {
                    Label cell = cells[index];
                    if (cell.parent == null || string.IsNullOrEmpty(cell.text)) continue;
                    if (cell.ClassListContains(LiveOpsHubClassNames.Mono)) continue;
                    if (!displayNames.Contains(cell.text)) continue;
                    displayNameColumnWidth = cell.parent.worldBound.width;
                    displayNameCellPadding = displayNameColumnWidth - cell.contentRect.width;
                    float cellNeeded = cell.MeasureTextSize(cell.text, 0f, VisualElement.MeasureMode.Undefined, 0f,
                        VisualElement.MeasureMode.Undefined).x;
                    if (cellNeeded <= widestNeeded) continue;
                    widestNeeded = cellNeeded;
                    widest = cell;
                }

                Assert.IsNotNull(widest, "ở 700x560 bảng phải còn cột 'Tên hiển thị' có chữ — không có thì hai vế đầu rỗng");
                Assert.GreaterOrEqual(displayNameColumnWidth + ColumnWidthTolerance,
                    EventTypeTable.DisplayNameColumnMinimumWidth,
                    "cột 'Tên hiển thị' rộng "
                    + displayNameColumnWidth.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                    + "px, HẸP HƠN đáy đã khai "
                    + EventTypeTable.DisplayNameColumnMinimumWidth.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                    + "px — MultiColumnListView đang bóp qua minWidth, tức mọi con số đáy của bảng thôi có tác dụng (W9-05)");

                // --- Vế 2: phép ĐƠN VỊ trên chính hằng ấy. Đệm lấy từ ô thật (cột − vùng nội dung), không chép từ USS.
                float minimumThatKeepsSlack = widestNeeded * TextFillCeilingRatio + displayNameCellPadding;
                Assert.GreaterOrEqual(EventTypeTable.DisplayNameColumnMinimumWidth, minimumThatKeepsSlack,
                    "tên hiển thị dài nhất (\"" + widest.text + "\") cần "
                    + widestNeeded.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "px chữ + "
                    + displayNameCellPadding.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                    + "px đệm ô, nên bề rộng tối thiểu của cột phải từ "
                    + minimumThatKeepsSlack.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                    + "px trở lên mới còn 5% dư; hằng đang khai "
                    + EventTypeTable.DisplayNameColumnMinimumWidth.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                    + "px. (Đây là vế bác con số 120f cũ: 120 chỉ cho 112px vùng nội dung cho 111px chữ.)");

                // --- Vế 3: mọi ô chữ của bảng ở cỡ hẹp nhất.
                int measuredCount = 0;
                for (int index = 0; index < cells.Count; index++)
                {
                    Label cell = cells[index];
                    if (string.IsNullOrEmpty(cell.text)) continue;
                    if (cell.resolvedStyle.whiteSpace != WhiteSpace.NoWrap) continue;
                    if (cell.contentRect.width < TextFillMinimumWidth) continue;
                    // Nhãn ĐÃ rút gọn không áp luật dư 5% (cổng bỏ qua vì bề rộng đo được là bề rộng bản đã cắt).
                    if (cell.text.IndexOf(EllipsisCharacter.ToString(), StringComparison.Ordinal) >= 0) continue;
                    float needed = cell.MeasureTextSize(cell.text, 0f, VisualElement.MeasureMode.Undefined, 0f,
                        VisualElement.MeasureMode.Undefined).x;
                    measuredCount++;
                    // Ô bảng KHÔNG được tự co: nó là ô của một cột có bề rộng khai sẵn. "Dư 0" ở đây là ô đã ôm khít chữ,
                    // tức đúng trạng thái mà cổng bỏ qua — ca này assert thay vì bỏ qua, xem chú thích TextFillMinimumSlack.
                    Assert.Greater(cell.contentRect.width - needed, TextFillMinimumSlack,
                        "ô \"" + cell.text + "\" ôm KHÍT chữ (dư "
                        + (cell.contentRect.width - needed).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                        + "px) — ô bảng có bề rộng cột khai sẵn thì không được tự co theo chữ (W9-25)");
                    Assert.GreaterOrEqual(cell.contentRect.width, needed * TextFillCeilingRatio,
                        "ô \"" + cell.text + "\" cần "
                        + needed.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "px, chỗ có "
                        + cell.contentRect.width.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                        + "px — chưa cắt nhưng dưới 5% dư, một đổi metric font là cắt im lặng (W9-25)");
                }

                Assert.Greater(measuredCount, 0, "không đo được ô nào — phép lọc hỏng thì ca này thành lời khai suông");
            }
        }

        /// <summary>
        /// Biến thể dữ liệu XẤU NHẤT của màn: bản dán có loại CHƯA KHAI. Đây là trạng thái duy nhất vẽ ra dấu trạng thái,
        /// và nó phải nằm trong ô MÀU — cạnh swatch rỗng viền quiet vốn đã nói "hub chưa biết màu vì loại chưa khai".
        /// Mẫu thiết kế không bao giờ ở trạng thái này, nên không dựng riêng thì dấu ấy không bao giờ được đo (W9-31).
        /// </summary>
        [UnityTest]
        public IEnumerator Table_UndeclaredType_DrawsStateMarkInsideTheColorCell()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            services.Session.Remote.Set(RemoteJsonWithLuckySpin(), LiveOpsDesignSample.NowUtc);
            services.Session.RunCheckToCompletion();
            EventTypesSection section = new EventTypesSection(services);
            using (SectionTestScope scope = SectionTestScope.Open(section, 1280, 760))
            {
                yield return scope.WaitForLayout();
                yield return scope.WaitForLayout();

                List<VisualElement> colorCells = new List<VisualElement>();
                scope.View.Query<VisualElement>(className: LiveOpsHubClassNames.EventTypesCellCenter).ToList(colorCells);
                Assert.Greater(colorCells.Count, 0, "bảng phải dựng ô màu cho từng hàng");

                int undeclaredMarkCount = 0;
                int declaredMarkCount = 0;
                for (int index = 0; index < colorCells.Count; index++)
                {
                    VisualElement cell = colorCells[index];
                    LiveOpsStateMark mark = cell.Q<LiveOpsStateMark>();
                    Assert.IsNotNull(mark, "ô màu nào cũng giữ sẵn dấu trạng thái để hàng không nhảy khi đổi trạng thái");
                    bool undeclared = cell.ClassListContains(LiveOpsHubClassNames.EventTypesRowUndeclared);
                    if (undeclared) undeclaredMarkCount++;
                    else if (mark.visible) declaredMarkCount++;
                    Assert.AreEqual(undeclared, mark.visible,
                        "dấu Blocked chỉ vẽ cho loại CHƯA KHAI — loại đã khai mà cũng có dấu thì dấu thôi nói được gì");

                    // (soát W10 F5) Swatch phải nằm ĐÚNG tâm ô màu ở MỌI hàng. Dấu luôn có trong cây và chỉ ẩn bằng
                    // `visible` — ẩn kiểu đó vẫn chiếm chỗ — nên nếu dấu nằm trong dòng chảy thì phép căn giữa đang căn
                    // giữa cụm "swatch + dấu" và swatch lệch trái ~6px ở cả hàng đã khai, nơi dấu không hề được vẽ.
                    VisualElement swatch = cell.Q(className: LiveOpsHubClassNames.Swatch);
                    Assert.IsNotNull(swatch, "ô màu nào cũng phải có swatch");
                    float swatchOffset = swatch.worldBound.center.x - cell.worldBound.center.x;
                    Assert.AreEqual(cell.worldBound.center.x, swatch.worldBound.center.x, SubPixelTolerance,
                        "swatch của hàng \"" + (undeclared ? "chưa khai" : "đã khai") + "\" lệch tâm ô màu "
                        + swatchOffset.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                        + "px — dấu trạng thái không được chen vào phép căn giữa của swatch (F5)");

                    if (!undeclared) continue;
                    Assert.Greater(mark.worldBound.width, 0f, "dấu của loại chưa khai phải có chỗ vẽ thật trong ô màu 44px");
                    Assert.IsTrue(cell.worldBound.Contains(mark.worldBound.center),
                        "dấu phải nằm TRONG ô màu, không tràn sang cột id loại");
                    Assert.IsFalse(mark.worldBound.Overlaps(swatch.worldBound),
                        "dấu và swatch phải đứng rời nhau trong ô màu 44px, không chồng lên nhau");
                }

                Assert.AreEqual(1, undeclaredMarkCount, "bản dán này khai đúng một loại chưa có trong nháp (lucky-spin)");
                Assert.AreEqual(0, declaredMarkCount, "loại đã khai không được mang dấu Blocked");
            }
        }

        /// <summary>
        /// Bấm một <see cref="Button"/> trong panel thật. <c>ClickEvent</c> KHÔNG chạy <c>Clickable</c> của Button (manipulator
        /// nghe pointer), còn <c>NavigationSubmitEvent</c> thì Button xử lý ở cả 2022.3 lẫn 6000.6 — đây là đường bấm duy nhất
        /// gửi được từ test batchmode mà không phải dựng chuỗi pointer giả.
        /// </summary>
        private static void Click(Button button)
        {
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }

        private static bool IsHidden(VisualElement element)
        {
            Assert.IsNotNull(element, "element phải có mặt ở mọi trạng thái của màn");
            return element.ClassListContains(LiveOpsHubClassNames.EventTypesHidden);
        }

        /// <summary>Bản dán có HAI loại nháp không khai — tình huống đạt được và là tình huống duy nhất lộ lỗi bố cục của dải.</summary>
        private static string RemoteJsonWithTwoUnknownTypes()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.DocumentWithoutPublishedStamp())
                .WithFixedEvent(new FixedLiveEventEntry("entry-lucky-1", "lucky-spin-0919", RemoteOnlyType,
                    "2026-09-19T00:00:00Z", "2026-09-20T00:00:00Z", "lucky_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-beach-1", "beach-party-0921", SecondRemoteOnlyType,
                    "2026-09-21T00:00:00Z", "2026-09-22T00:00:00Z", "beach_v1"))
                .Build();
            return LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2).Text;
        }

        /// <summary>Bản dán có hai đợt loại <c>lucky-spin</c> mà nháp không khai (Hình 10b).</summary>
        private static string RemoteJsonWithLuckySpin()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.DocumentWithoutPublishedStamp())
                .WithFixedEvent(new FixedLiveEventEntry("entry-lucky-1", "lucky-spin-0919", RemoteOnlyType,
                    "2026-09-19T00:00:00Z", "2026-09-20T00:00:00Z", "lucky_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-lucky-2", "lucky-spin-0926", RemoteOnlyType,
                    "2026-09-26T00:00:00Z", "2026-09-27T00:00:00Z", "lucky_v1"))
                .Build();
            return LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2).Text;
        }
    }
}
