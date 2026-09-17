using System;
using System.Globalization;
using NUnit.Framework;
using DreamTech.LiveOps.Tests;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Model thuần của status bar (Logic, chạy <c>-nographics</c>): bốn ca câu trái, chỗ nối "Vừa làm …", và câu phải với giờ UTC +
    /// tooltip giờ máy ([FD §3.4], 8.3). Dựng phiên thật của <see cref="LiveOpsHubTestServices"/> chứ không nặn trạng thái kiểm
    /// bằng tay — câu "cũ" phải là câu mà đường thật (Apply → MarkCalendarEdited) sinh ra.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class LiveOpsHubStatusBarModelTests
    {
        private const string UndoKeyLabel = "⌘Z";
        private static readonly DateTime NowUtc = new DateTime(2026, 9, 13, 8, 47, 0, DateTimeKind.Utc);
        private static readonly LiveOpsHubFormat Format = new LiveOpsHubFormat(TimeSpan.FromHours(7));

        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
        }

        [Test]
        public void NeverChecked_EmptyRingAndF5Hint()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.EmptyAssetScenario);

            LiveOpsHubStatusBarModel model = Build(services);

            Assert.IsNull(model.LeftMark, "chưa kiểm thì không có bằng chứng để đeo dấu — vòng rỗng, không phải Ok");
            Assert.AreEqual("Chưa kiểm lần nào — F5 để kiểm", model.LeftText);
        }

        [Test]
        public void Checked_ShowsWorstMarkRuleCountAndFindingCount()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            LiveEventCalendarCheckSummary summary = services.Session.Check.LastReport.Summary;

            LiveOpsHubStatusBarModel model = Build(services);

            Assert.IsNotNull(model.LeftMark, "kết quả còn mới thì đeo dấu = mức xấu nhất của chính lần kiểm");
            Assert.AreEqual(LiveOpsHubFindingRouting.StateOf(summary.WorstConsequence.Value), model.LeftMark.Value);
            string checkedClock = services.Session.Check.CheckedAtUtc.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            StringAssert.StartsWith("Kiểm lúc " + checkedClock + " UTC", model.LeftText);
            Assert.AreEqual(8, checkedClock.Length, "giây phải có: hai lần kiểm cách nhau vài giây phải phân biệt được");
            StringAssert.Contains(summary.RuleCount + " luật", model.LeftText);
            StringAssert.Contains(summary.NeedsActionCount + " phát hiện", model.LeftText);
            StringAssert.Contains("lịch chưa đổi từ lần kiểm", model.LeftText);
        }

        [Test]
        public void StaleByEdit_EmptyRingAndBothTimes()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.StaleCheckScenario);

            LiveOpsHubStatusBarModel model = Build(services);

            Assert.AreEqual(LiveOpsHubCheckStaleReason.CalendarEdited, services.Session.Check.StaleReason);
            Assert.IsNull(model.LeftMark, "kết quả cũ không còn là bằng chứng — dấu Ok/Blocked lúc này là nói dối");
            Assert.AreEqual("Lịch đã đổi lúc 08:46:50 UTC, sau lần kiểm 08:46:30 — F5 để kiểm lại", model.LeftText);
        }

        [Test]
        public void StaleByMilestone_NamesMilestoneNotEditTime()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            LiveOpsHubCheckState check = services.Session.Check;
            // Đẩy đồng hồ qua mốc gần nhất của chính lịch mẫu: đường thật của PD-23, không gán StaleReason bằng tay.
            DateTime milestone = NextMilestoneAfter(services, check.CheckedAtUtc.Value);
            check.Reevaluate(milestone.AddSeconds(1));

            LiveOpsHubStatusBarModel model = Build(services, milestone.AddSeconds(1));

            Assert.AreEqual(LiveOpsHubCheckStaleReason.MilestonePassed, check.StaleReason);
            Assert.IsNull(model.LeftMark);
            StringAssert.StartsWith("Đã qua mốc ", model.LeftText);
            StringAssert.Contains(" sau lần kiểm " + check.CheckedAtUtc.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                + " — F5 để kiểm lại", model.LeftText);
        }

        [Test]
        public void RecentAction_OnTop_KeepsUndoKeyLabel()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);

            LiveOpsHubStatusBarModel onTop = LiveOpsHubStatusBarModel.Build(services.Session.Check, true, "Xoá hunt-0916-bonus", true, NowUtc,
                services.Session.Publish.ActiveStamp, Format, UndoKeyLabel);
            LiveOpsHubStatusBarModel buried = LiveOpsHubStatusBarModel.Build(services.Session.Check, true, "Xoá hunt-0916-bonus", false, NowUtc,
                services.Session.Publish.ActiveStamp, Format, UndoKeyLabel);

            StringAssert.EndsWith(" · Vừa làm: Xoá hunt-0916-bonus (⌘Z)", onTop.LeftText);
            StringAssert.EndsWith(" · Vừa làm: Xoá hunt-0916-bonus", buried.LeftText);
            Assert.IsFalse(buried.LeftText.Contains(UndoKeyLabel),
                "bước Undo không còn trên đỉnh: mời bấm ⌘Z là mời gỡ thao tác của người khác");
        }

        [Test]
        public void RecentAction_WithoutUndoKeyBinding_NoEmptyParentheses()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);

            LiveOpsHubStatusBarModel model = LiveOpsHubStatusBarModel.Build(services.Session.Check, true, "Dời lava-quest-2026-09b", true, NowUtc,
                services.Session.Publish.ActiveStamp, Format, string.Empty);

            StringAssert.EndsWith(" · Vừa làm: Dời lava-quest-2026-09b", model.LeftText);
            Assert.IsFalse(model.LeftText.Contains("()"), "không có phím gán thì bỏ hẳn ngoặc, không in cặp ngoặc rỗng");
        }

        /// <summary>
        /// Q-W5-5 (user chốt 17/9/2026): chỗ "Vừa làm: …" chỉ có MỘT dòng và người đọc lại sau nhiều thao tác, nên nó đọc
        /// CÂU NGẮN của bước — nguồn duy nhất là <see cref="LiveOpsToastModel.StepName"/>, cùng phép rơi với Undo History.
        /// Lệnh chưa có câu ngắn riêng vẫn hiện câu toast như cũ, nên không màn nào phải sửa theo.
        /// </summary>
        [Test]
        public void RecentAction_ReadsShortStepName_FallsBackToToastSentence()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            const string longSentence = "Đã dời kết thúc lava-quest-2026-09b 20/9 → 22/9 UTC";
            const string shortStep = "Đổi lava-quest-2026-09b";

            LiveOpsToastModel withShortStep = LiveOpsToastModel.ForEdit(longSentence, 11, undoneStepName: shortStep);
            LiveOpsToastModel withoutShortStep = LiveOpsToastModel.ForEdit(longSentence, 12);

            Assert.AreEqual(shortStep, withShortStep.StepName, "có câu ngắn thì status bar đọc câu ngắn");
            Assert.AreEqual(longSentence, withoutShortStep.StepName, "chưa có câu ngắn thì rơi về câu toast");

            LiveOpsHubStatusBarModel shortModel = LiveOpsHubStatusBarModel.Build(services.Session.Check, true,
                withShortStep.StepName, true, NowUtc, services.Session.Publish.ActiveStamp, Format, UndoKeyLabel);
            LiveOpsHubStatusBarModel fallbackModel = LiveOpsHubStatusBarModel.Build(services.Session.Check, true,
                withoutShortStep.StepName, true, NowUtc, services.Session.Publish.ActiveStamp, Format, UndoKeyLabel);

            StringAssert.EndsWith(" · Vừa làm: " + shortStep + " (⌘Z)", shortModel.LeftText);
            Assert.IsFalse(shortModel.LeftText.Contains(longSentence), "câu dài của toast không được tràn sang status bar");
            StringAssert.EndsWith(" · Vừa làm: " + longSentence + " (⌘Z)", fallbackModel.LeftText);
        }

        [Test]
        public void RightText_NoStamp_OnlyUtcClock()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.EmptyAssetScenario);

            LiveOpsHubStatusBarModel model = Build(services);

            Assert.IsNull(services.Session.Publish.ActiveStamp);
            Assert.AreEqual("13/9 08:47 UTC", model.RightText, "chưa ghi dấu thì không bịa phần 'đã đăng …· sha …'");
        }

        [Test]
        public void RightTooltip_IsDeviceClockWithOffset()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.EmptyAssetScenario);

            LiveOpsHubStatusBarModel model = Build(services);

            Assert.AreEqual("15:47 giờ máy (UTC+7)", model.RightTooltip);
        }

        [Test]
        public void NoSession_LeftIsEmptyWithoutMark()
        {
            LiveOpsHubStatusBarModel model = LiveOpsHubStatusBarModel.Build(null, false, string.Empty, false, NowUtc, null, Format, UndoKeyLabel);

            Assert.IsNull(model.LeftMark);
            Assert.AreEqual(string.Empty, model.LeftText, "khung trần chưa có phiên: một vòng rỗng không kèm lý do sẽ bị đọc thành trạng thái thật");
            Assert.AreEqual("13/9 08:47 UTC", model.RightText);
        }

        [Test]
        public void NoAsset_SaysNoCalendarInsteadOfInvitingF5()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario);

            LiveOpsHubStatusBarModel model = Build(services);

            Assert.IsNull(model.LeftMark, "chưa có asset thì không có kết quả nào để đeo dấu");
            Assert.AreEqual(LiveOpsHubStrings.ShellStatusNoCalendarAsset, model.LeftText);
            Assert.AreNotEqual(LiveOpsHubStrings.ShellStatusNeverChecked, model.LeftText,
                "F5 không chạy được lần kiểm nào khi chưa có asset — mục \"Kiểm lại tất cả (F5)\" của menu ⋮ cũng đang disabled");
        }

        private static LiveOpsHubStatusBarModel Build(LiveOpsHubServices services)
        {
            return Build(services, NowUtc);
        }

        private static LiveOpsHubStatusBarModel Build(LiveOpsHubServices services, DateTime nowUtc)
        {
            return LiveOpsHubStatusBarModel.Build(services.Session.Check, services.Session.Asset != null, string.Empty, false, nowUtc,
                services.Session.Publish.ActiveStamp, Format, UndoKeyLabel);
        }

        /// <summary>Mốc gần nhất sau <paramref name="afterUtc"/> trong chính lịch mẫu — cùng bộ dò mà phiên dùng mỗi giây.</summary>
        private static DateTime NextMilestoneAfter(LiveOpsHubServices services, DateTime afterUtc)
        {
            DateTime? milestone = LiveOpsHubCheckState.FindEarliestMilestone(services.Session.Compilation, afterUtc, afterUtc.AddYears(1));
            Assert.IsTrue(milestone.HasValue, "lịch mẫu phải còn ít nhất một mốc trong một năm tới");
            return milestone.Value;
        }
    }
}
