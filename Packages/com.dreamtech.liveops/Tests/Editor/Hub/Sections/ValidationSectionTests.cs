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
    /// Màn Kiểm lịch dựng trong cửa sổ hub thật (mục 7.5, [SD2 §2]). Trọng tâm: "Sửa" an toàn đi trọn ba việc — một Undo
    /// group, một toast, rồi TỰ KIỂM LẠI (PD-10) — và các hàng dẫn người dùng tới đúng màn tiếp theo.
    /// <para>
    /// Nút bấm bằng chuột thật qua <c>EditorWindow.SendEvent</c> (như RecurringRulesSectionTests): gọi thẳng handler thì
    /// không chứng minh được nút đã nối vào luồng.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class ValidationSectionTests
    {
        private const string RemoteOnlyType = "lucky-spin";

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
        public IEnumerator DesignSample_RequiredElementsAndGroupsPresent()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            yield return Open(services);

            _scope.AssertRequiredElementsPresent();
            Assert.Greater(Section.VisibleRows.Count, 0, "lịch mẫu có phát hiện nên thân màn phải có hàng");
            Assert.Greater(Section.Cards.Count, 0);
            Assert.IsNotNull(_scope.View.Q(LiveOpsHubPaths.ValidationElementNames.Detail), "pane Chi tiết phải ở trong cây ở mọi trạng thái");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SafeFix_AppliesOneUndoGroupAndRechecks()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            yield return Open(services);

            LiveOpsToastModel toast = null;
            services.Bus.ToastRequested += model => toast = model;
            string documentBefore = LiveOpsHubDocumentSnapshot.Write(services.Session.Document);

            Assert.IsTrue(Section.SafeRepairSlot.Button.enabledSelf, "lịch mẫu có đúng một lỗi sửa nhanh được nên nút phải bật");
            yield return ClickHeaderButton(ValidationSection.SafeRepairButtonElementName);

            Assert.AreNotEqual(documentBefore, LiveOpsHubDocumentSnapshot.Write(services.Session.Document), "bấm Sửa phải đổi lịch thật");
            Assert.IsNotNull(toast, "mỗi lệnh sửa một toast Hoàn tác");
            Assert.AreNotEqual(LiveOpsToastModel.NoUndoGroup, toast.UndoGroup, "sửa an toàn phải nằm trong đúng một Undo group");
            Assert.AreEqual(toast.Message, toast.UndoGroupName, "tên Undo group trùng câu toast (luật toast chung)");
            StringAssert.Contains("1", toast.Message, "câu nêu số lỗi đã sửa ([SD2 §2.5] \"LiveOps: Sửa nhanh 1 lỗi\")");
            // PD-10: kết quả cũ không được phép trông như kết quả mới — sửa xong là kiểm lại ngay.
            Assert.IsTrue(services.Session.Check.IsRunning, "sửa xong phải tự kiểm lại");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RemoteDriftRow_XemDiff_NavigatesExportCompareRemote()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            services.Session.Remote.Set(RemoteJsonWithLuckySpin(), LiveOpsDesignSample.NowUtc);
            services.Session.RunCheckToCompletion();
            yield return Open(services);

            LiveOpsHubNavigation navigation = null;
            services.Bus.NavigationRequested += request => navigation = request;
            ValidationFindingRow row = FindRow(LiveEventCalendarRuleIds.RemoteSnapshotDrift);
            Assert.IsNotNull(row, "dán JSON lệch phải ra một hàng remote-snapshot-drift");
            Assert.IsNotNull(row.ActionButton, "hàng lệch bản remote có nút \"Xem diff\"");

            yield return Click(row.ActionButton);

            Assert.IsNotNull(navigation, "(V-13) \"Xem diff\" phải phát một yêu cầu điều hướng");
            Assert.AreEqual(LiveOpsHubSections.Ids.Export, navigation.SectionId);
            Assert.AreEqual(LiveOpsHubCompareSource.Remote, navigation.CompareSource, "(V-13) mở Xuất JSON với bản so là remote");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NeverChecked_EmptySaysNotCheckedIsNotPassed()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            yield return Open(services);

            Label title = _scope.View.Q<Label>(ValidationSection.EmptyTitleElementName);
            Assert.AreEqual(LiveOpsHubStrings.ValidationNeverCheckedTitle, title.text);
            Label body = _scope.View.Q<Label>(ValidationSection.EmptyBodyElementName);
            // Q-12: trống KHÔNG phải đạt — câu phải nói thẳng điều đó, nếu không người đọc tưởng lịch sạch.
            StringAssert.Contains(LiveOpsHubStrings.ValidationNeverCheckedBodyFormat.Substring(0, NeverCheckedSentencePrefixLength),
                body.text);
            Assert.IsNotEmpty(_scope.View.Q<Button>(ValidationSection.EmptyActionElementName).text, "trống phải có chỗ đi tiếp");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Phần đầu câu trước tham số {0} — đủ để nhận ra đúng câu mà không phụ thuộc số luật.</summary>
        private const int NeverCheckedSentencePrefixLength = 24;

        [UnityTest]
        public IEnumerator StaleCheck_ShowsNoticeAndStaleTag()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.StaleCheckScenario);
            yield return Open(services);

            Assert.IsFalse(_scope.View.Q(LiveOpsHubPaths.ValidationElementNames.Notice)
                .ClassListContains(LiveOpsHubClassNames.ValidationHidden), "kết quả cũ phải có HelpBox đầu thân");
            Assert.IsNotNull(_scope.View.Q<HelpBox>(), "câu \"kết quả cũ\" là HelpBox info, không phải một Label lẫn vào thân");
            Assert.IsNotNull(Section.Cards[0].StaleTag, "mọi header card mang tag \"cũ\"");
            Assert.AreEqual(LiveOpsHubStrings.ValidationStaleTag, Section.Cards[0].StaleTag.text);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SelectingRow_OpensDetailWithConsequenceFirst()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            yield return Open(services);

            ValidationFindingRow row = Section.VisibleRows[0];
            yield return Click(row);

            Assert.AreSame(row.Row.Finding, Section.Detail.Finding, "bấm hàng mở đúng phát hiện đó trong pane Chi tiết");
            Assert.IsNotNull(Section.Detail.DocumentationButton, "pane Chi tiết luôn có link tài liệu luật");
            LogAssert.NoUnexpectedReceived();
        }

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

        private ValidationFindingRow FindRow(string ruleId)
        {
            for (int index = 0; index < Section.VisibleRows.Count; index++)
            {
                ValidationRow row = Section.VisibleRows[index].Row;
                if (row.Finding != null && string.Equals(row.Finding.RuleId, ruleId, System.StringComparison.Ordinal))
                {
                    return Section.VisibleRows[index];
                }
            }
            return null;
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
