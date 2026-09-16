using System;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kiểm lịch nối vào phiên: một luật mỗi nhịp (cửa sổ không đóng băng), sửa lịch làm kết quả thành cũ, và ngữ cảnh kiểm mang đủ
    /// ba tài liệu nền — bản so đang chọn (luật 9/10), bản remote đã dán (luật 8/12) và (CC-VALB-1) tài liệu của dấu MỚI NHẤT cho luật 12.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class CheckStateTests
    {
        private const string OlderStampUtcText = "2026-09-11T16:20:00Z";
        private const string LatestStampUtcText = "2026-09-12T09:05:00Z";

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

        [Test]
        public void Session_CheckProgress_OneRulePerUpdate()
        {
            LiveOpsHubCalendarSession session = OpenDesignSampleSession();
            session.StartCheck();
            Assert.IsTrue(session.Check.IsRunning, "F5 bắt đầu lần kiểm, không chạy hết ngay trong một frame");
            int ruleCount = session.Check.RuleCount;
            Assert.Greater(ruleCount, 1, "mẫu thiết kế phải có nhiều hơn một luật để đo được nhịp");

            for (int completed = 1; completed <= ruleCount; completed++)
            {
                int before = session.Check.CompletedRuleCount;
                session.AdvanceCheck();
                if (!session.Check.IsRunning) break;
                Assert.AreEqual(before + 1, session.Check.CompletedRuleCount, "mỗi nhịp EditorApplication.update chạy đúng MỘT luật");
            }

            Assert.IsFalse(session.Check.IsRunning, "chạy đủ số luật thì xong");
            Assert.IsNotNull(session.Check.LastReport);
            Assert.AreEqual(ruleCount, session.Check.LastReport.RuleResults.Count, "mọi luật đều có kết quả");
            Assert.AreEqual(LiveOpsHubCheckStaleReason.None, session.Check.StaleReason);
        }

        [Test]
        public void Session_EditAfterCheck_MarksStale()
        {
            ManualLiveOpsClock clock = LiveOpsHubTestServices.CreateClock();
            LiveOpsHubCalendarSession session = OpenDesignSampleSession(clock);
            session.RunCheckToCompletion();
            Assert.IsFalse(session.Check.IsStale);
            DateTime editedUtc = LiveOpsDesignSample.NowUtc.AddSeconds(20);
            clock.Set(editedUtc);

            session.Apply(CalendarSessionTests.MoveLavaQuestEnd(session.Document, "2026-09-21T00:00:00Z"), "Dời kết thúc lava-quest");

            Assert.IsTrue(session.Check.IsStale, "sửa lịch xong thì con số của lần kiểm trước không còn là bằng chứng");
            Assert.AreEqual(LiveOpsHubCheckStaleReason.CalendarEdited, session.Check.StaleReason);
            Assert.AreEqual(editedUtc, session.Check.CalendarEditedUtc, "giờ sửa lấy từ đồng hồ tiêm vào, không phải giờ máy");
            Assert.AreEqual(editedUtc, session.LastChangedUtc);
            Assert.IsNotNull(session.Check.LastReport, "kết quả cũ vẫn còn để hiện kèm badge 'cũ'");
        }

        [Test]
        public void Session_UndoAfterCheck_AlsoMarksStale()
        {
            // Undo của Unity chỉ chạy trên asset CÓ FILE — asset chỉ trong bộ nhớ không vào hệ Undo.
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile("SessionCheck.asset", LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = CalendarSessionTests.OpenSession(asset);
            session.Apply(CalendarSessionTests.MoveLavaQuestEnd(session.Document, "2026-09-21T00:00:00Z"), "Dời kết thúc lava-quest");
            session.RunCheckToCompletion();
            Assert.IsFalse(session.Check.IsStale);

            Undo.PerformUndo();

            Assert.IsTrue(session.Check.IsStale, "Hoàn tác cũng là đổi lịch — kết quả kiểm thành cũ như mọi lần sửa khác");
            Assert.AreEqual(LiveOpsHubCheckStaleReason.CalendarEdited, session.Check.StaleReason);
        }

        [Test]
        public void CheckState_ContextCarriesLatestStampBaseline_WhenCompareSourceIsOlderStamp()
        {
            LiveEventCalendarDocument olderSnapshot = LiveOpsDesignSample.DocumentWithoutPublishedStamp();
            LiveEventCalendarDocument latestSnapshot = WithoutEntry(olderSnapshot, LiveOpsDesignSample.StarTournamentEntryKey);
            PublishedCalendarStamp olderStamp = StampOf(OlderStampUtcText, olderSnapshot);
            PublishedCalendarStamp latestStamp = StampOf(LatestStampUtcText, latestSnapshot);
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.DocumentWithoutPublishedStamp())
                .WithPublishedStamp(olderStamp)
                .WithPublishedStamp(latestStamp)
                .Build();
            LiveOpsHubCalendarSession session = OpenSession(document);
            Assert.AreEqual(LatestStampUtcText, session.Document.LatestStamp.PublishedUtcText, "dấu mới nhất là dấu thêm sau");

            // Người dùng chọn DẤU CŨ làm bản so (card lịch sử ở màn Xuất JSON).
            session.Publish.SelectActiveStamp(olderStamp);
            LiveEventCalendarCheckContext context = session.BuildCheckContext(LiveOpsDesignSample.NowUtc);

            Assert.AreEqual(OlderStampUtcText, session.Publish.ActiveStamp.PublishedUtcText);
            Assert.IsNotNull(context.PublishedBaseline, "luật 9/10 so với bản so đang chọn");
            Assert.AreEqual(olderSnapshot.FixedEvents.Count, context.PublishedBaseline.FixedEvents.Count, "bản so = dấu CŨ đang chọn");
            Assert.IsNotNull(context.LatestStampBaseline, "(CC-VALB-1) luật 12 cần dấu MỚI NHẤT, độc lập với bản so đang chọn");
            Assert.AreEqual(latestSnapshot.FixedEvents.Count, context.LatestStampBaseline.FixedEvents.Count,
                "dấu mới nhất bỏ star-tournament nên ít hơn bản so một đợt — hai tài liệu phải khác nhau");
            Assert.AreEqual(0, context.PublishedBaseline.EventTypes.Count, "(V-3) bản đăng không mang định nghĩa loại — ghép vào sẽ làm diff nói dối");
            Assert.AreEqual(0, context.LatestStampBaseline.EventTypes.Count);
        }

        [Test]
        public void CheckState_NoStamp_ContextBaselinesAreNull()
        {
            LiveOpsHubCalendarSession session = OpenSession(LiveOpsDesignSample.DocumentWithoutPublishedStamp());

            LiveEventCalendarCheckContext context = session.BuildCheckContext(LiveOpsDesignSample.NowUtc);

            Assert.IsNull(context.PublishedBaseline, "chưa đăng lần nào = không có bản so");
            Assert.IsNull(context.LatestStampBaseline);
            Assert.IsNull(context.RemoteSnapshot, "chưa dán JSON đang chạy");
        }

        [Test]
        public void CheckState_RemoteSnapshotPasted_ContextCarriesDocumentAndSha()
        {
            LiveOpsHubCalendarSession session = OpenDesignSampleSession();
            session.RunCheckToCompletion();
            Assert.AreEqual(LiveOpsHubCheckStaleReason.None, session.Check.StaleReason, "kiểm xong thì kết quả còn mới");
            string remoteJson = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.DocumentWithoutPublishedStamp(),
                LiveEventCalendarJsonFormat.Version2).Text;

            session.Remote.Set(remoteJson, LiveOpsDesignSample.NowUtc);
            LiveEventCalendarCheckContext context = session.BuildCheckContext(LiveOpsDesignSample.NowUtc);

            Assert.IsNotNull(context.RemoteSnapshot, "luật 8 và 12 đọc bản dán qua ngữ cảnh");
            Assert.AreEqual(session.Remote.Sha256Hex, context.RemoteSnapshotSha256Hex);
            Assert.AreEqual(LiveOpsHubCheckStaleReason.CalendarEdited, session.Check.StaleReason,
                "dán bản remote mới làm kết quả kiểm trước đó thành cũ — nó chưa hề thấy bản này");
        }

        // ------------------------------------------------------------------------------------------------------------ hỗ trợ

        private static LiveOpsHubCalendarSession OpenDesignSampleSession()
        {
            return OpenDesignSampleSession(null);
        }

        private static LiveOpsHubCalendarSession OpenDesignSampleSession(ManualLiveOpsClock clock)
        {
            return LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(clock)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document))).Session;
        }

        private static LiveOpsHubCalendarSession OpenSession(LiveEventCalendarDocument document)
        {
            return CalendarSessionTests.OpenSession(LiveOpsHubTestServices.CreateMemoryAsset(document));
        }

        private static PublishedCalendarStamp StampOf(string publishedUtcText, LiveEventCalendarDocument snapshot)
        {
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(snapshot, LiveEventCalendarJsonFormat.Version2);
            return new PublishedCalendarStamp(publishedUtcText, LiveOpsHubTestServices.PublisherName, json.Sha256Hex, json.ByteCount,
                (int)json.Format, string.Empty, json.Text);
        }

        private static LiveEventCalendarDocument WithoutEntry(LiveEventCalendarDocument document, string entryKey)
        {
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new RemoveFixedEventEdit(entryKey), out LiveEventCalendarDocument result),
                "bỏ đợt " + entryKey + " phải áp được");
            return result;
        }
    }
}
