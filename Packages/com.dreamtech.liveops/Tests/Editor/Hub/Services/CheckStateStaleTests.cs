using System;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Vì sao kết quả Kiểm lịch không còn là bằng chứng (PD-23). Thuần: dựng <see cref="LiveOpsHubCheckState"/> trực tiếp với đồng hồ tay,
    /// không cần asset, cửa sổ hay nhịp <c>EditorApplication.update</c> — mọi trạng thái cũ dựng được bằng cách đưa thời gian tới đúng mốc.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class CheckStateStaleTests
    {
        /// <summary>Bắt đầu đợt sớm hơn trong lịch chỉ-đợt-cố-định của test — mốc SỚM NHẤT sau 13/9 08:47.</summary>
        private static readonly DateTime EarlierStartUtc = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Bắt đầu đợt muộn hơn — có mặt để chứng minh hàm trả mốc sớm nhất chứ không phải mốc gần "bây giờ" nhất.</summary>
        private static readonly DateTime LaterStartUtc = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void NeverChecked_ReasonNeverChecked()
        {
            LiveOpsHubCheckState check = NewCheckState();

            Assert.IsNull(check.LastReport);
            Assert.AreEqual(LiveOpsHubCheckStaleReason.NeverChecked, check.StaleReason);
            Assert.IsFalse(check.IsStale, "chưa đo không phải là 'kết quả cũ' — không có con số nào để giữ");
            Assert.IsFalse(check.IsRunning);
            Assert.AreEqual(0, check.CompletedRuleCount);
            Assert.Greater(check.RuleCount, 0, "số luật của validator biết trước khi kiểm — vòng tiến độ vẽ được ngay");
        }

        [Test]
        public void EditAfterCheck_CalendarEdited()
        {
            LiveOpsHubCheckState check = NewCheckState();
            RunCheck(check, LiveOpsDesignSample.NowUtc);
            Assert.AreEqual(LiveOpsHubCheckStaleReason.None, check.StaleReason);
            DateTime editedUtc = LiveOpsDesignSample.NowUtc.AddSeconds(20);

            check.MarkCalendarEdited(editedUtc);

            Assert.AreEqual(LiveOpsHubCheckStaleReason.CalendarEdited, check.StaleReason);
            Assert.IsTrue(check.IsStale);
            Assert.AreEqual(editedUtc, check.CalendarEditedUtc, "giữ lần đổi ĐẦU TIÊN sau lần kiểm để câu chữ nêu đúng giờ");
            Assert.IsNull(check.PassedMilestoneUtc, "cũ vì sửa thì không nêu mốc");
            Assert.IsNotNull(check.LastReport, "kết quả cũ vẫn giữ con số — chỉ nói rõ là cũ");

            check.MarkCalendarEdited(editedUtc.AddSeconds(30));
            Assert.AreEqual(editedUtc, check.CalendarEditedUtc, "lần đổi sau không đẩy giờ lên");
        }

        [Test]
        public void TimePassesFixedStart_MilestonePassed_ReportsEarliestMilestone()
        {
            // Chỉ đợt cố định (không luật lặp) để mốc sớm nhất chắc chắn là một lần BẮT ĐẦU của đợt, không phải ranh giới của lần lặp.
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("lava-quest", "Nhiệm vụ dung nham", 0, false, "lava_quest_v2"))
                .WithEventType(new LiveEventTypeDefinition("star-tournament", "Giải ngôi sao", 6, false, "star_tournament_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-earlier", "earlier-0914", "lava-quest",
                    "2026-09-14T00:00:00Z", "2026-09-19T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-later", "later-0915", "star-tournament",
                    "2026-09-15T00:00:00Z", "2026-09-16T00:00:00Z", "star_tournament_v1"))
                .Build();
            LiveOpsHubCheckState check = NewCheckState();
            RunCheck(check, LiveOpsDesignSample.NowUtc, document);

            check.Reevaluate(LaterStartUtc.AddHours(12));

            Assert.AreEqual(LiveOpsHubCheckStaleReason.MilestonePassed, check.StaleReason);
            Assert.IsTrue(check.IsStale);
            Assert.AreEqual(EarlierStartUtc, check.PassedMilestoneUtc,
                "phải là mốc SỚM NHẤT đã qua, không phải mốc gần 'bây giờ' nhất");
            Assert.IsNull(check.CalendarEditedUtc);
        }

        [Test]
        public void TimePassesRecurringBoundary_MilestonePassed()
        {
            // Chỉ luật lặp sky-race (chu kỳ 24h, mở 20h từ 05/01/2026 00:00 UTC): mốc kế tiếp sau 08:47 là 20:00 cùng ngày (kết thúc lần đang mở).
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("sky-race", "Đua trên trời", 4, false, "sky_race_v4"))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "sky-race-", 24, 20, "sky_race_v4"))
                .Build();
            LiveOpsHubCheckState check = NewCheckState();
            RunCheck(check, LiveOpsDesignSample.NowUtc, document);
            DateTime boundaryUtc = new DateTime(2026, 9, 13, 20, 0, 0, DateTimeKind.Utc);

            check.Reevaluate(boundaryUtc.AddMinutes(1));

            Assert.AreEqual(LiveOpsHubCheckStaleReason.MilestonePassed, check.StaleReason);
            Assert.AreEqual(boundaryUtc, check.PassedMilestoneUtc, "mốc của luật lặp là kết thúc lần đang mở");
        }

        [Test]
        public void TimePassesNoMilestone_StaysFresh()
        {
            LiveOpsHubCheckState check = NewCheckState();
            RunCheck(check, LiveOpsDesignSample.NowUtc);

            check.Reevaluate(LiveOpsDesignSample.NowUtc.AddMinutes(5));

            Assert.AreEqual(LiveOpsHubCheckStaleReason.None, check.StaleReason, "không đợt nào bắt đầu/kết thúc trong 5 phút đó");
            Assert.IsFalse(check.IsStale);
            Assert.IsNull(check.PassedMilestoneUtc);
        }

        [Test]
        public void ReloadWhileRunning_InterruptedByReload()
        {
            LiveOpsHubCheckState check = NewCheckState();
            check.Begin(BuildContext(LiveOpsDesignSample.Document, LiveOpsDesignSample.NowUtc));
            check.Step();
            Assert.IsTrue(check.IsRunning, "còn luật chưa chạy");

            check.MarkInterruptedByReload();

            Assert.IsFalse(check.IsRunning, "domain reload giết lần chạy dở");
            Assert.IsNull(check.LastReport, "kết quả dở dang không được kể là kết quả");
            Assert.AreEqual(LiveOpsHubCheckStaleReason.InterruptedByReload, check.StaleReason);
            Assert.IsTrue(check.IsStale, "phải nói rõ vì sao trống, không im lặng như 'chưa kiểm'");
        }

        [Test]
        public void Recheck_ClearsStale()
        {
            LiveOpsHubCheckState check = NewCheckState();
            RunCheck(check, LiveOpsDesignSample.NowUtc);
            check.MarkCalendarEdited(LiveOpsDesignSample.NowUtc.AddSeconds(20));
            Assert.IsTrue(check.IsStale);

            RunCheck(check, LiveOpsDesignSample.NowUtc.AddSeconds(30));

            Assert.AreEqual(LiveOpsHubCheckStaleReason.None, check.StaleReason);
            Assert.IsFalse(check.IsStale);
            Assert.IsNull(check.CalendarEditedUtc, "kiểm lại xoá luôn lý do cũ, không để câu chữ kể chuyện lần trước");
            Assert.IsNull(check.PassedMilestoneUtc);
            Assert.AreEqual(LiveOpsDesignSample.NowUtc.AddSeconds(30), check.CheckedAtUtc);
        }

        [Test]
        public void EditDuringRun_ResultIsStaleAsSoonAsItCompletes()
        {
            LiveOpsHubCheckState check = NewCheckState();
            check.Begin(BuildContext(LiveOpsDesignSample.Document, LiveOpsDesignSample.NowUtc));
            check.Step();
            DateTime editedUtc = LiveOpsDesignSample.NowUtc.AddSeconds(2);

            check.MarkCalendarEdited(editedUtc);
            check.RunToCompletion();

            Assert.IsNotNull(check.LastReport, "vẫn ra kết quả — nhưng nó mô tả bản TRƯỚC khi sửa");
            Assert.AreEqual(LiveOpsHubCheckStaleReason.CalendarEdited, check.StaleReason);
            Assert.AreEqual(editedUtc, check.CalendarEditedUtc);
        }

        // ------------------------------------------------------------------------------------------------------------ hỗ trợ

        private static LiveOpsHubCheckState NewCheckState()
        {
            return new LiveOpsHubCheckState(LiveEventCalendarValidator.Default);
        }

        private static void RunCheck(LiveOpsHubCheckState check, DateTime nowUtc)
        {
            RunCheck(check, nowUtc, LiveOpsDesignSample.Document);
        }

        private static void RunCheck(LiveOpsHubCheckState check, DateTime nowUtc, LiveEventCalendarDocument document)
        {
            check.Begin(BuildContext(document, nowUtc));
            check.RunToCompletion();
            Assert.IsNotNull(check.LastReport, "lần kiểm phải chạy xong trong một lượt");
        }

        private static LiveEventCalendarCheckContext BuildContext(LiveEventCalendarDocument document, DateTime nowUtc)
        {
            return new LiveEventCalendarCheckContextBuilder(document, nowUtc)
                .WithCompilation(LiveEventCalendarCompiler.CompileInExportOrder(document))
                .Build();
        }
    }
}
