using System;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Trạng thái đăng của phiên: bản so (V-3 không mang loại), nguồn bản so (V-13) rơi về "bản đã đăng" khi nguồn đã chọn không còn dữ
    /// liệu, tick "Đã xem" tự tắt khi mục đổi tiếp, ghi dấu đã đăng thì LƯU luôn (dấu phải nằm trong file để git thấy), và sha lần xuất
    /// gần nhất sống trong SessionState chứ không làm asset bẩn.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class PublishStateTests
    {
        private const string AssetFileName = "SessionPublish.asset";
        private const string FirstEndUtc = "2026-09-21T00:00:00Z";
        private const string SecondEndUtc = "2026-09-22T00:00:00Z";

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
        public void PublishState_ReviewedResetsWhenItemChanges()
        {
            LiveOpsHubCalendarSession session = OpenMemorySession();
            session.Apply(CalendarSessionTests.MoveLavaQuestEnd(session.Document, FirstEndUtc), "Dời kết thúc lava-quest");
            LiveEventCalendarChange change = FindChange(session, LiveOpsDesignSample.LavaQuestMidEntryKey);

            session.Publish.SetReviewed(change, true);

            Assert.IsTrue(session.Publish.IsReviewed(change),
                "tick Đã xem phải giữ được trong phiên; thay đổi = " + change + ", dấu đang so = "
                + (session.Publish.ActiveStamp != null ? session.Publish.ActiveStamp.ShortSha : "(không có)"));

            // Cùng một mục, nhưng người dùng sửa tiếp: Fingerprint đổi nên tick cũ không còn nói về thay đổi đang hiện.
            session.Apply(CalendarSessionTests.MoveLavaQuestEnd(session.Document, SecondEndUtc), "Dời kết thúc lava-quest lần hai");
            LiveEventCalendarChange changedAgain = FindChange(session, LiveOpsDesignSample.LavaQuestMidEntryKey);

            Assert.AreNotEqual(change.Fingerprint, changedAgain.Fingerprint, "sửa tiếp phải đổi Fingerprint, nếu không test này vô nghĩa");
            Assert.IsFalse(session.Publish.IsReviewed(changedAgain), "mục đổi tiếp thì tick tự tắt — không được coi là đã xem");
        }

        [Test]
        public void PublishState_MarkPublished_AddsStampAndSaves()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.DocumentWithoutPublishedStamp());
            LiveOpsHubCalendarSession session = CalendarSessionTests.OpenSession(asset);
            Assert.IsNull(session.Document.LatestStamp, "bắt đầu từ lịch chưa đăng lần nào");
            LiveEventCalendarJsonText json = session.Publish.CurrentJson;

            LiveOpsHubEditOutcome outcome = session.Publish.MarkPublished("mở lava-quest tháng 9");

            Assert.IsTrue(outcome.Applied, outcome.FailureText);
            PublishedCalendarStamp stamp = session.Document.LatestStamp;
            Assert.IsNotNull(stamp, "ghi dấu = thêm một dấu vào tài liệu");
            Assert.AreEqual(json.Sha256Hex, stamp.Sha256Hex, "dấu phải mang sha của ĐÚNG chuỗi JSON đang hiện");
            Assert.AreEqual(json.ByteCount, stamp.ByteCount);
            Assert.AreEqual(json.Text, stamp.SnapshotJson, "dấu giữ nguyên văn JSON để lần sau còn so được");
            Assert.AreEqual(LiveOpsHubTestServices.PublisherName, stamp.Publisher);
            Assert.AreEqual("mở lava-quest tháng 9", stamp.Note);
            Assert.IsFalse(session.HasUnsavedChanges, "ghi dấu xong phải LƯU — dấu chỉ có giá trị khi nằm trong file");
            Assert.AreSame(stamp, session.Publish.ActiveStamp, "dấu vừa ghi thành bản so hiện hành");
        }

        [Test]
        public void PublishState_MarkPublished_SaveFails_KeepsStampAndUndoGroup()
        {
            // Asset chỉ trong bộ nhớ: Apply chạy được, Save luôn trả false — đúng nhánh "lệnh đã áp nhưng lưu hỏng".
            LiveOpsHubCalendarSession session = CalendarSessionTests.OpenSession(
                LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.DocumentWithoutPublishedStamp()));

            LiveOpsHubEditOutcome outcome = session.Publish.MarkPublished("thử đăng khi không lưu được");

            Assert.IsFalse(outcome.Applied, "lưu hỏng thì không được báo là xong");
            Assert.IsNotEmpty(outcome.FailureText, "phải nói vì sao không lưu được");
            Assert.IsNotNull(session.Document.LatestStamp, "dấu ĐÃ nằm trong tài liệu — câu trả về không được giả vờ là chưa có gì xảy ra");
            Assert.IsTrue(session.HasUnsavedChanges, "tab phải còn * vì dấu chưa vào file");
            Assert.AreNotEqual(LiveOpsHubEditOutcome.NoUndoGroup, outcome.UndoGroup, "toast phải mời được Hoàn tác cho dấu vừa thêm");
            Assert.IsNotEmpty(outcome.UndoName, "tên Undo group = câu toast");
        }

        [Test]
        public void PublishState_RemoveLatestStamp_DoesNotSave()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = CalendarSessionTests.OpenSession(asset);

            LiveOpsHubEditOutcome outcome = session.Publish.RemoveLatestStamp();

            Assert.IsTrue(outcome.Applied, outcome.FailureText);
            Assert.IsNull(session.Document.LatestStamp);
            Assert.IsTrue(session.HasUnsavedChanges, "gỡ dấu KHÔNG tự lưu — gỡ nhầm thì Hoàn tác trước khi lưu");
            Assert.IsFalse(session.Publish.RemoveLatestStamp().Applied, "không còn dấu nào để gỡ");
        }

        [Test]
        public void PublishState_LastExportedShaInSessionState()
        {
            LiveOpsHubCalendarSession session = OpenMemorySession();
            LiveEventCalendarJsonText json = session.Publish.CurrentJson;
            Assert.IsEmpty(session.Publish.LastExportedSha256Hex, "chưa Copy/Lưu file lần nào");

            session.Publish.RecordExport(json, false);

            Assert.AreEqual(json.Sha256Hex, session.Publish.LastExportedSha256Hex);
            Assert.AreEqual(LiveOpsDesignSample.NowUtc, session.Publish.LastExportedUtc, "giờ xuất lấy từ đồng hồ tiêm vào");
            Assert.IsFalse(session.Publish.LastExportedViaFile, "Copy chứ không phải Lưu file");
            Assert.IsFalse(session.HasUnsavedChanges, "lần xuất là việc của phiên, không phải nội dung lịch — asset không được bẩn vì nó");

            session.Publish.RecordExport(json, true);
            Assert.IsTrue(session.Publish.LastExportedViaFile);
        }

        [Test]
        public void PublishState_CompareSourceDiskAndRemote_FallBackToPublishedWhenMissing()
        {
            LiveOpsHubCalendarSession session = OpenMemorySession();
            Assert.AreEqual(LiveOpsHubCompareSource.Published, session.Publish.ActiveCompareSource, "mặc định so với bản đã đăng");

            // Chưa có xung đột đĩa và chưa dán JSON đang chạy: lời chọn vẫn nhận, nhưng hiệu lực là Published — không ném, không màn hình trắng.
            session.Publish.SelectCompareSource(LiveOpsHubCompareSource.Disk);
            Assert.AreEqual(LiveOpsHubCompareSource.Published, session.Publish.ActiveCompareSource, "không có bản đĩa để so");
            Assert.AreSame(session.Publish.ActiveBaseline, session.Publish.CompareDocument);

            session.Publish.SelectCompareSource(LiveOpsHubCompareSource.Remote);
            Assert.AreEqual(LiveOpsHubCompareSource.Published, session.Publish.ActiveCompareSource, "chưa dán bản remote");

            string remoteJson = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.DocumentWithoutPublishedStamp(),
                LiveEventCalendarJsonFormat.Version2).Text;
            session.Remote.Set(remoteJson, LiveOpsDesignSample.NowUtc);

            Assert.AreEqual(LiveOpsHubCompareSource.Remote, session.Publish.ActiveCompareSource, "dán xong thì lời chọn cũ có hiệu lực ngay");
            Assert.AreSame(session.Remote.Document, session.Publish.CompareDocument);
            Assert.IsNotNull(session.Publish.CompareDiff);

            session.Remote.Clear();
            Assert.AreEqual(LiveOpsHubCompareSource.Published, session.Publish.ActiveCompareSource, "xoá bản dán thì rơi lại về bản đã đăng");

            session.Publish.SelectCompareSource(LiveOpsHubCompareSource.None);
            Assert.AreEqual(LiveOpsHubCompareSource.Published, session.Publish.ActiveCompareSource, "None = về mặc định, không phải 'không so gì'");
        }

        [Test]
        public void PublishState_ActiveBaselineHasNoEventTypes()
        {
            LiveOpsHubCalendarSession session = OpenMemorySession();

            LiveEventCalendarDocument baseline = session.Publish.ActiveBaseline;

            Assert.IsNotNull(baseline, "mẫu thiết kế có một dấu đã đăng");
            Assert.AreEqual(0, baseline.EventTypes.Count, "(V-3) bản đăng không mang định nghĩa loại — ghép loại của nháp vào sẽ làm diff nói dối");
            Assert.Greater(baseline.FixedEvents.Count, 0, "nhưng vẫn có đợt để so");
        }

        [Test]
        public void PublishState_SelectedFormatAndJsonFollowIt()
        {
            LiveOpsHubCalendarSession session = OpenMemorySession();
            Assert.AreEqual(LiveEventCalendarJsonFormat.Version2, session.Publish.SelectedFormat, "định dạng mặc định là 2");
            LiveEventCalendarJsonText version2 = session.Publish.CurrentJson;

            session.Publish.SelectedFormat = LiveEventCalendarJsonFormat.Version1;

            LiveEventCalendarJsonText version1 = session.Publish.CurrentJson;
            Assert.AreEqual(LiveEventCalendarJsonFormat.Version1, version1.Format);
            Assert.AreNotEqual(version2.Sha256Hex, version1.Sha256Hex, "đổi định dạng thì chuỗi sẽ copy cũng đổi");
            Assert.IsFalse(session.HasUnsavedChanges, "định dạng đang chọn sống trong SessionState, không làm asset bẩn");
        }

        // ------------------------------------------------------------------------------------------------------------ hỗ trợ

        private static LiveOpsHubCalendarSession OpenMemorySession()
        {
            return CalendarSessionTests.OpenSession(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document));
        }

        private static LiveEventCalendarChange FindChange(LiveOpsHubCalendarSession session, string entryKey)
        {
            foreach (LiveEventCalendarChange change in session.Publish.PublishedDiff.Changes)
            {
                if (string.Equals(change.EntryKey, entryKey, StringComparison.Ordinal)) return change;
            }
            Assert.Fail("diff không có thay đổi nào cho đợt " + entryKey);
            return null;
        }
    }
}
