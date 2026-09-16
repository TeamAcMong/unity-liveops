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
        private const string StarTournament = "star-tournament";

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

                declare.SendEvent(ClickEvent.GetPooled());
                yield return null;
                Assert.IsTrue(services.Session.Document.TryGetEventType(RemoteOnlyType, out LiveEventTypeDefinition declared),
                    "'Khai báo' phải thêm định nghĩa loại vào nháp");
                Assert.AreEqual(LiveEventTypeColorSlots.DefaultSlotFor(RemoteOnlyType), declared.ColorSlot,
                    "loại mới lấy ô màu theo băm, đổi được sau ở inspector");
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

        private static bool IsHidden(VisualElement element)
        {
            Assert.IsNotNull(element, "element phải có mặt ở mọi trạng thái của màn");
            return element.ClassListContains(LiveOpsHubClassNames.EventTypesHidden);
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
