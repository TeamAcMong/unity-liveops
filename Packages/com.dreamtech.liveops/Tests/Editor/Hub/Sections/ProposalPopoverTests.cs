using System.Collections;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Popover "Đề xuất…" ([SD2 §2.6], SPIKE-B SP-2). Luật an toàn được khoá ở đây: phím mặc định làm việc AN TOÀN
    /// (Enter = Quay lại) và việc đổi điều người chơi thấy chỉ chạy khi có cú click thật vào "Áp".
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class ProposalPopoverTests
    {
        [TearDown]
        public void TearDown()
        {
            LiveOpsPopoverContent.CloseCurrent();
            LiveOpsHubTestServices.ReleaseAll();
        }

        [UnityTest]
        public IEnumerator Enter_IsBack_ApplyOnlyOnClick()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarFinding finding = FirstProposalFinding(services);
            Assume.That(finding, Is.Not.Null, "lịch mẫu phải có một phát hiện dạng Đề xuất…");

            var applied = new List<LiveEventCalendarRepair>();
            ProposalPopover popover = new ProposalPopover(finding, services.Format, services.LayoutLoader, null, applied.Add, 0);
            using (SectionTestScope scope = SectionTestScope.Open(new ValidationSection(services)))
            {
                yield return scope.WaitForLayout();
                // Popover mở trong cửa sổ popup riêng; test gắn cây đã dựng vào panel thật để gửi được sự kiện bấm.
                VisualElement built = popover.BuildForTest();
                scope.Window.rootVisualElement.Add(built);
                try
                {
                    yield return null;

                    using (KeyDownEvent enterEvent = KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None))
                    {
                        popover.HandleEnterForTest(enterEvent);
                    }

                    Assert.IsTrue(popover.EnterHandled, "Enter phải đi qua nhánh Quay lại");
                    Assert.AreEqual(0, popover.ApplyCount, "Enter KHÔNG được áp đề xuất");
                    Assert.AreEqual(0, applied.Count, "không cú click nào thì lịch không đổi");

                    Click(built.Q<Button>(ProposalPopover.ApplyElementName));
                    yield return null;

                    Assert.AreEqual(1, popover.ApplyCount, "\"Áp\" chạy khi click");
                    Assert.AreEqual(1, applied.Count);
                    Assert.AreSame(finding.Repairs[0], applied[0], "áp đúng lựa chọn đang chọn");
                }
                finally
                {
                    built.RemoveFromHierarchy();
                }
            }
        }

        private static void Click(Button button)
        {
            Assert.IsNotNull(button, "popover thiếu nút");
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }

        [Test]
        public void Options_ReadBeforeAndAfterFromFindingText()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarFinding finding = FirstProposalFinding(services);
            Assume.That(finding, Is.Not.Null);

            ProposalPopover popover = new ProposalPopover(finding, services.Format, services.LayoutLoader, null, null, 0);
            VisualElement root = popover.BuildForTest();
            RadioButtonGroup options = root.Q<RadioButtonGroup>(ProposalPopover.OptionsElementName);

            Assert.IsNotNull(options, "popover dùng RadioButtonGroup, không phải danh sách nút");
            Assert.AreEqual(finding.Repairs.Count, options.choices == null ? 0 : CountOf(options.choices),
                "mỗi cách sửa đúng một lựa chọn");
            Assert.AreEqual(LiveOpsFindingText.RepairOptionText(finding, finding.Repairs[0], services.Format),
                FirstOf(options.choices), "(V-8) chữ lựa chọn lấy nguyên từ LiveOpsFindingText");
            Assert.AreEqual(LiveOpsHubStrings.ValidationProposalBackButton, root.Q<Button>(ProposalPopover.BackElementName).text);
            Assert.AreEqual(LiveOpsHubStrings.ValidationProposalEnterHint, root.Q<Label>(ProposalPopover.HintElementName).text);
        }

        [Test]
        public void Preselected_KeepsChoiceOpenedFromDetailPane()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarFinding finding = FirstProposalFinding(services);
            Assume.That(finding, Is.Not.Null);
            Assume.That(finding.Repairs.Count, Is.GreaterThan(1), "ca này cần hai cách sửa để chọn sẵn cái thứ hai");

            ProposalPopover popover = new ProposalPopover(finding, services.Format, services.LayoutLoader, null, null, 1);
            popover.BuildForTest();

            // Nút trong pane Chi tiết mở ĐÚNG popover này với lựa chọn vừa bấm được chọn sẵn ([SD2 §2.4]).
            Assert.AreEqual(1, popover.SelectedIndex);
        }

        /// <summary>
        /// (V-34, nợ Findings) Nút áp nêu ĐỘNG TỪ của cách sửa đang chọn — mockup [SD2 §2.6] viết "Áp: đổi hunt-0916-bonus".
        /// Duyệt cả chín <c>RepairId</c> mà hub biết: mỗi id phải cho một khuôn chữ RIÊNG, không rơi về khuôn trung tính
        /// "Áp: {0}" — khuôn đó chỉ còn dành cho lệnh sửa của luật game tự viết.
        /// </summary>
        [Test]
        public void ApplyButton_VerbFollowsRepairId()
        {
            string[] knownRepairIds =
            {
                LiveOpsFindingText.NormalizeRepairId, LiveOpsFindingText.KeepStartSetDurationRepairId,
                LiveOpsFindingText.SwapStartEndRepairId, LiveOpsFindingText.RenameRepairId,
                LiveOpsFindingText.ShiftStartKeepEndRepairId, LiveOpsFindingText.ShiftWholeKeepDurationRepairId,
                LiveOpsFindingText.SetActiveToPeriodRepairId, LiveOpsFindingText.RevertRepairId,
                LiveOpsFindingText.DeferUntilEndRepairId,
            };

            var offenders = new List<string>();
            for (int index = 0; index < knownRepairIds.Length; index++)
            {
                string format = ProposalPopover.ApplyFormatOf(knownRepairIds[index]);
                if (format == LiveOpsHubStrings.ValidationProposalApplyFormat) offenders.Add(knownRepairIds[index]);
            }

            Assert.IsEmpty(offenders, "RepairId còn rơi về khuôn trung tính \"Áp: {0}\": " + string.Join(", ", offenders.ToArray()));
            Assert.AreEqual(LiveOpsHubStrings.ValidationProposalApplyRenameFormat,
                ProposalPopover.ApplyFormatOf(LiveOpsFindingText.RenameRepairId), "mockup [SD2 §2.6] đặt \"đổi\" cho đổi id");
            Assert.AreEqual(LiveOpsHubStrings.ValidationProposalApplyShiftFormat,
                ProposalPopover.ApplyFormatOf(LiveOpsFindingText.ShiftWholeKeepDurationRepairId), "hai cách DỜI giữ \"dời\" của W4");
            Assert.AreEqual(LiveOpsHubStrings.ValidationProposalApplyFormat, ProposalPopover.ApplyFormatOf("luat-game-tu-viet"),
                "RepairId lạ vẫn phải có chữ nút, không được rỗng");
        }

        /// <summary>Nút áp trên cây thật đổi chữ theo lựa chọn đang chọn, không chỉ hàm khuôn.</summary>
        [Test]
        public void ApplyButton_TextUsesSelectedRepairVerb()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveEventCalendarFinding finding = FirstProposalFinding(services);
            Assume.That(finding, Is.Not.Null);

            ProposalPopover popover = new ProposalPopover(finding, services.Format, services.LayoutLoader, null, null, 0);
            VisualElement root = popover.BuildForTest();

            string expected = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                ProposalPopover.ApplyFormatOf(finding.Repairs[0].RepairId), finding.TargetId);
            Assert.AreEqual(expected, root.Q<Button>(ProposalPopover.ApplyElementName).text);
        }

        private static int CountOf(IEnumerable<string> choices)
        {
            int count = 0;
            foreach (string choice in choices) count++;
            return count;
        }

        private static string FirstOf(IEnumerable<string> choices)
        {
            foreach (string choice in choices) return choice;
            return string.Empty;
        }

        private static LiveEventCalendarFinding FirstProposalFinding(LiveOpsHubServices services)
        {
            LiveEventCalendarCheckReport report = services.Session.Check.LastReport;
            for (int index = 0; index < report.Findings.Count; index++)
            {
                if (report.Findings[index].RepairKind == LiveEventCalendarRepairKind.Proposal) return report.Findings[index];
            }
            return null;
        }
    }
}
