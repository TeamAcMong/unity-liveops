using System;
using System.Text;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// JSON đang chạy trên remote config mà người dùng dán để so (Q-6): đọc bằng ĐÚNG parser của game, sha tính trên byte UTF-8 nguyên
    /// văn bản dán, sống trong SessionState theo GUID asset và KHÔNG làm asset bẩn — dán để so không phải là sửa lịch.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class RemoteSnapshotTests
    {
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
        public void Remote_Set_ParsesWithGameParserAndHashesPastedBytes()
        {
            LiveOpsHubCalendarSession session = OpenSession();
            string remoteJson = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.DocumentWithoutPublishedStamp(),
                LiveEventCalendarJsonFormat.Version2).Text;
            int changedCount = 0;
            session.Remote.Changed += () => changedCount++;

            session.Remote.Set(remoteJson, LiveOpsDesignSample.NowUtc);

            Assert.IsTrue(session.Remote.HasSnapshot);
            Assert.AreEqual(remoteJson, session.Remote.PastedText, "giữ nguyên văn bản dán — sha phải tính trên đúng byte người dùng dán");
            Assert.IsTrue(session.Remote.IsReadable, "parser của game đọc được");
            Assert.IsNotNull(session.Remote.Document);
            Assert.AreEqual(LiveEventCalendarSha256.ComputeHex(Encoding.UTF8.GetBytes(remoteJson)), session.Remote.Sha256Hex);
            Assert.AreEqual(LiveOpsDesignSample.NowUtc, session.Remote.VerifiedUtc);
            Assert.AreEqual(1, changedCount, "dán xong báo đúng một lần");
            Assert.IsFalse(session.HasUnsavedChanges, "dán để so không phải sửa lịch — asset không được bẩn");
        }

        [Test]
        public void Remote_UnreadableText_KeepsTextButDocumentIsNull()
        {
            LiveOpsHubCalendarSession session = OpenSession();

            session.Remote.Set("{ không phải JSON", LiveOpsDesignSample.NowUtc);

            Assert.IsTrue(session.Remote.HasSnapshot, "vẫn là 'đã dán' — có thứ để hiện lỗi trên");
            Assert.IsFalse(session.Remote.IsReadable);
            Assert.IsNull(session.Remote.Document, "luật 12 coi bản không đọc được như chưa dán");
            Assert.IsNotNull(session.Remote.ParseResult, "nhưng vẫn giữ kết quả parse để nêu lỗi cú pháp");
            Assert.IsNotEmpty(session.Remote.Sha256Hex, "sha tính trên byte, không phụ thuộc đọc được hay không");
        }

        [Test]
        public void Remote_BlankText_ClearsSnapshot()
        {
            LiveOpsHubCalendarSession session = OpenSession();
            session.Remote.Set(LiveOpsDesignSample.PublishedSnapshotJson, LiveOpsDesignSample.NowUtc);
            int changedCount = 0;
            session.Remote.Changed += () => changedCount++;

            session.Remote.Set("   \n\t ", LiveOpsDesignSample.NowUtc);

            Assert.IsFalse(session.Remote.HasSnapshot, "'đã dán rỗng' không phải bằng chứng gì về remote");
            Assert.IsEmpty(session.Remote.PastedText);
            Assert.IsEmpty(session.Remote.Sha256Hex);
            Assert.IsNull(session.Remote.VerifiedUtc);
            Assert.AreEqual(1, changedCount);
        }

        [Test]
        public void Remote_MatchesStampExactly_BySha()
        {
            LiveOpsHubCalendarSession session = OpenSession();
            PublishedCalendarStamp stamp = session.Document.LatestStamp;
            Assert.IsNotNull(stamp, "mẫu thiết kế có một dấu đã đăng");

            session.Remote.Set(stamp.SnapshotJson, LiveOpsDesignSample.NowUtc);

            Assert.IsTrue(session.Remote.MatchesStampExactly(stamp), "dán đúng từng byte JSON của dấu = đường nhanh của luật 12");
            Assert.IsFalse(session.Remote.MatchesStampExactly(null));

            session.Remote.Set(stamp.SnapshotJson + "\n", LiveOpsDesignSample.NowUtc);
            Assert.IsFalse(session.Remote.MatchesStampExactly(stamp), "khác một byte thì không còn là đường nhanh — phải để luật 12 so tử tế");
        }

        [Test]
        public void Remote_MarkVerified_KeepsTextUpdatesTime()
        {
            LiveOpsHubCalendarSession session = OpenSession();
            session.Remote.Set(LiveOpsDesignSample.PublishedSnapshotJson, LiveOpsDesignSample.NowUtc);
            string sha = session.Remote.Sha256Hex;
            DateTime laterUtc = LiveOpsDesignSample.NowUtc.AddMinutes(23);

            session.Remote.MarkVerified(laterUtc);

            Assert.AreEqual(laterUtc, session.Remote.VerifiedUtc, "bấm Đối chiếu lại chỉ đổi giờ");
            Assert.AreEqual(sha, session.Remote.Sha256Hex, "không đổi bản dán");
            Assert.AreEqual(LiveOpsDesignSample.PublishedSnapshotJson, session.Remote.PastedText);
        }

        [Test]
        public void Remote_BelongsToAsset_NotCarriedToAnotherCalendar()
        {
            LiveEventCalendarAsset first = LiveOpsHubTestServices.CreateAssetFile("RemoteFirst.asset", LiveOpsDesignSample.Document);
            LiveEventCalendarAsset second = LiveOpsHubTestServices.CreateAssetFile("RemoteSecond.asset", LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = CalendarSessionTests.OpenSession(first);
            session.Remote.Set(LiveOpsDesignSample.PublishedSnapshotJson, LiveOpsDesignSample.NowUtc);
            Assert.IsTrue(session.Remote.HasSnapshot);

            Assert.IsTrue(session.TrySelectAsset(second), "đổi sang lịch khác");

            Assert.IsFalse(session.Remote.HasSnapshot, "bản dán khoá theo GUID asset — không mang sang lịch khác");

            Assert.IsTrue(session.TrySelectAsset(first), "quay lại lịch cũ");
            Assert.IsTrue(session.Remote.HasSnapshot, "bản dán của lịch cũ vẫn còn trong SessionState của nó");
            Assert.AreEqual(LiveOpsDesignSample.PublishedSnapshotJson, session.Remote.PastedText);
        }

        // ------------------------------------------------------------------------------------------------------------ hỗ trợ

        private static LiveOpsHubCalendarSession OpenSession()
        {
            return CalendarSessionTests.OpenSession(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document));
        }
    }
}
