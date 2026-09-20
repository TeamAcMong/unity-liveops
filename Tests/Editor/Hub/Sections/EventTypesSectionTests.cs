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
        /// Mốc: bốn cột luôn hiện tốn 44 + 128 + 120 + 36 = 328px, cộng 14px trừ sẵn cho thanh cuộn dọc = 342px. Mỗi cột
        /// phụ tiếp theo cộng thêm đúng bề rộng tối thiểu ĐỌC ĐƯỢC của nó: 116 (Cách vào) → 140 (Khoá config) → 88
        /// (Nguồn) → 56 (Đợt).
        /// </summary>
        [Test]
        public void OptionalColumnCountThatFits_KeepsAsManyColumnsAsReallyFit()
        {
            Assert.AreEqual(-1, EventTypeTable.OptionalColumnCountThatFits(float.NaN), "chưa có số đo thì KHÔNG dựng lại cột");
            Assert.AreEqual(-1, EventTypeTable.OptionalColumnCountThatFits(0f), "bề rộng 0 là cây chưa layout, không phải bảng hẹp");

            Assert.AreEqual(0, EventTypeTable.OptionalColumnCountThatFits(342f), "vừa đúng bốn cột luôn hiện");
            Assert.AreEqual(0, EventTypeTable.OptionalColumnCountThatFits(457f), "thiếu 1px cho cột Cách vào thì KHÔNG lấy nó");
            Assert.AreEqual(1, EventTypeTable.OptionalColumnCountThatFits(458f));
            Assert.AreEqual(2, EventTypeTable.OptionalColumnCountThatFits(598f));
            Assert.AreEqual(3, EventTypeTable.OptionalColumnCountThatFits(686f));
            Assert.AreEqual(4, EventTypeTable.OptionalColumnCountThatFits(742f));
            Assert.AreEqual(4, EventTypeTable.OptionalColumnCountThatFits(1600f), "rộng bao nhiêu cũng chỉ có bốn cột phụ");
        }

        /// <summary>
        /// Bỏ cột theo TIỀN TỐ: thiếu chỗ cho cột thứ hai thì mọi cột sau nó cũng bị bỏ, kể cả cột "Đợt" chỉ cần 56px.
        /// Nhặt cột nào vừa thì lấy sẽ làm THỨ TỰ cột đổi theo bề rộng cửa sổ và người dùng mất mốc đọc.
        /// </summary>
        [Test]
        public void OptionalColumnCountThatFits_DropsBySuffix_NotByWhicheverFits()
        {
            // 342 + 116 = 458 đủ cho cột Cách vào; thêm 56 nữa (514) vẫn KHÔNG đủ cho cột thứ hai (140) — và cột "Đợt"
            // (56px, đứng thứ tư) vẫn không được nhảy cóc lên thay chỗ.
            Assert.AreEqual(1, EventTypeTable.OptionalColumnCountThatFits(514f));
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
                Assert.Greater(hidden.Count, 0, "ở 700px bảng không thể chứa đủ tám cột — nếu không cột nào bị bỏ thì "
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

                Assert.AreEqual(0, section.Table.HiddenColumnTitles.Count, "1280px chứa đủ tám cột");
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
                Assert.AreEqual(0, section.Table.HiddenColumnTitles.Count, "nền của ca này là bảng ĐỦ tám cột");

                section.Table.View.sortColumnDescriptions.Add(
                    new SortColumnDescription(EventTypeTable.SourceColumnName, SortDirection.Descending));
                yield return null;

                scope.Window.position = new UnityEngine.Rect(0, 0, 700, 560);
                yield return scope.WaitForLayout();
                yield return LiveOpsHubWindowTestScope.WaitFrames(3);

                Assert.Greater(section.Table.HiddenColumnTitles.Count, 0, "700px phải bỏ bớt cột — ca này mới có nghĩa");
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
