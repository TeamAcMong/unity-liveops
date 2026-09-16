using System;
using NUnit.Framework;
using DreamTech.LiveOps.Tests;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Model thuần của hai chip header (Logic): bốn dạng chip nháp và câu <c>saveChangesMessage</c> ([FD §3.3], 8.3). Điểm phải giữ
    /// là hai con số không được lẫn nhau — "chưa lưu" (so với đĩa) và "khác bản đã đăng" (so với dấu) — nên mọi ca dựng bằng phiên
    /// thật rồi sửa qua <c>Apply</c>/<c>MarkPublished</c>, không gán trạng thái tay.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class LiveOpsHubHeaderChipModelTests
    {
        private const string SaveKeyLabel = "⌘S";
        private static readonly LiveOpsHubFormat Format = new LiveOpsHubFormat(TimeSpan.FromHours(7));

        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
        }

        [Test]
        public void NoAsset_FormNoAsset_NoStar()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario);

            LiveOpsHubHeaderChipModel model = LiveOpsHubHeaderChipModel.Build(services.Session, Format, SaveKeyLabel);

            Assert.AreEqual(LiveOpsHubHeaderChipModel.FormNoAsset, model.DraftChipForm);
            Assert.AreEqual("Chưa có lịch", model.AssetChipText);
            Assert.AreEqual("Mở Tổng quan để tạo lịch", model.DraftLeftText);
            Assert.IsFalse(model.HasUnsavedChanges);
            Assert.AreEqual(string.Empty, model.SaveChangesMessage);
        }

        [Test]
        public void NeverPublished_FormC_EmptyRingSentence()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.EmptyAssetScenario);

            LiveOpsHubHeaderChipModel model = LiveOpsHubHeaderChipModel.Build(services.Session, Format, SaveKeyLabel);

            Assert.IsNull(services.Session.Publish.ActiveStamp);
            Assert.AreEqual(LiveOpsHubHeaderChipModel.FormNeverPublished, model.DraftChipForm);
            Assert.AreEqual("Chưa có dấu đã đăng", model.DraftLeftText);
            Assert.AreEqual(string.Empty, model.DraftRightText);
            Assert.IsFalse(model.HasUnsavedChanges, "chưa có dấu KHÔNG phải trạng thái chưa lưu — tab không được có *");
        }

        [Test]
        public void Unsaved_FormA_TwoPartsAndStar()
        {
            // Lịch mẫu đã mang sẵn một dấu đã đăng (11/9 16:20) — không cần ghi dấu mới để có phần phải của chip.
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            ApplyOneEdit(services);

            LiveOpsHubHeaderChipModel model = LiveOpsHubHeaderChipModel.Build(services.Session, Format, SaveKeyLabel);

            Assert.IsTrue(services.Session.HasUnsavedChanges);
            Assert.AreEqual(LiveOpsHubHeaderChipModel.FormUnsaved, model.DraftChipForm);
            Assert.AreEqual("Chưa lưu · ⌘S", model.DraftLeftText);
            StringAssert.EndsWith(" khác bản đã đăng", model.DraftRightText);
            Assert.IsTrue(model.HasUnsavedChanges, "tab có '*' khi VÀ CHỈ KHI chip ở dạng (a)");
        }

        [Test]
        public void Unsaved_WithoutSaveKeyBinding_ChipDropsKeyPart()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            ApplyOneEdit(services);

            LiveOpsHubHeaderChipModel model = LiveOpsHubHeaderChipModel.Build(services.Session, Format, string.Empty);

            Assert.AreEqual("Chưa lưu", model.DraftLeftText, "không có phím Lưu gán thì không in dấu · cụt");
        }

        [Test]
        public void Unsaved_WithoutStamp_NoZeroPublishedDiffPart()
        {
            // Lịch chưa từng đăng: asset rỗng rồi thêm một đợt — đúng bước đầu tiên của người dùng mới.
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.EmptyAssetScenario);
            AddOneEvent(services);

            LiveOpsHubHeaderChipModel model = LiveOpsHubHeaderChipModel.Build(services.Session, Format, SaveKeyLabel);

            Assert.IsTrue(services.Session.HasUnsavedChanges);
            Assert.IsNull(services.Session.Publish.ActiveStamp);
            Assert.AreEqual(string.Empty, model.DraftRightText,
                "'0 khác bản đã đăng' khi chưa có dấu sẽ đọc thành 'đã khớp bản đăng' — sai hẳn nghĩa");
        }

        [Test]
        public void MatchesPublished_FormFour_NamesStampTime()
        {
            // MarkPublished tự lưu: asset chỉ trong bộ nhớ sẽ trượt ở bước lưu, nên ca này cần file thật.
            LiveOpsHubServices services = CreateFileBackedServices("ChipFormMatches.asset");
            MarkPublished(services);

            LiveOpsHubHeaderChipModel model = LiveOpsHubHeaderChipModel.Build(services.Session, Format, SaveKeyLabel);

            Assert.AreEqual(LiveOpsHubHeaderChipModel.FormMatchesPublished, model.DraftChipForm,
                "ghi dấu ngay trên nháp hiện tại: nháp khớp dấu, không còn 'khác bản đã đăng'");
            StringAssert.StartsWith("Khớp dấu đã đăng ", model.DraftLeftText);
            Assert.IsFalse(model.DraftLeftText.Contains("Firebase"), "dấu là việc tự khai — không được viết 'khớp Firebase'");
        }

        [Test]
        public void DiffersFromPublished_FormB_AfterEditIsSaved()
        {
            // Asset THẬT trên đĩa: dạng (b) chỉ tồn tại sau một lần Lưu thành công, mà asset chỉ trong bộ nhớ thì Save() trả false.
            LiveOpsHubServices services = CreateFileBackedServices("ChipFormB.asset");
            MarkPublished(services);
            ApplyOneEdit(services);
            Assert.IsTrue(services.Session.Save(), "asset trên đĩa phải lưu được — dạng (b) là trạng thái sau khi lưu");

            LiveOpsHubHeaderChipModel model = LiveOpsHubHeaderChipModel.Build(services.Session, Format, SaveKeyLabel);

            Assert.IsFalse(model.HasUnsavedChanges);
            Assert.AreEqual(LiveOpsHubHeaderChipModel.FormDiffersFromPublished, model.DraftChipForm);
            StringAssert.StartsWith("Khác bản đã đăng · ", model.DraftLeftText);
            StringAssert.EndsWith(" thay đổi", model.DraftLeftText);
        }

        [Test]
        public void SaveChangesMessage_NamesAssetChangeCountAndItemIds()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            ApplyOneEdit(services);

            LiveOpsHubHeaderChipModel model = LiveOpsHubHeaderChipModel.Build(services.Session, Format, SaveKeyLabel);

            StringAssert.StartsWith(services.Session.AssetFileName + " có ", model.SaveChangesMessage);
            StringAssert.Contains("thay đổi chưa lưu", model.SaveChangesMessage);
            StringAssert.EndsWith("Lưu trước khi đóng LiveOps Hub?", model.SaveChangesMessage);
            Assert.IsFalse(model.SaveChangesMessage.Contains("khác bản đã đăng"),
                "câu đóng cửa sổ CHỈ dùng con số 'chưa lưu'; trộn hai con số là lỗi đã có tên ([FD §3.3])");
        }

        /// <summary>Một lệnh sửa thật (dời kết thúc một đợt) — sau lệnh này phiên có đúng một thay đổi chưa lưu.</summary>
        private static void ApplyOneEdit(LiveOpsHubServices services)
        {
            FixedLiveEventEntry entry;
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey, out entry));
            LiveOpsHubEditOutcome outcome = services.Session.Apply(
                new ReplaceFixedEventEdit(new FixedLiveEventEntry(entry.EntryKey, entry.EventId, entry.EventType, entry.StartUtcText,
                    "2026-09-21T00:00:00Z", entry.ConfigKey)),
                "Dời kết thúc " + entry.EventId);
            Assert.IsTrue(outcome.Applied, outcome.FailureText);
        }

        /// <summary>Thêm một đợt vào lịch rỗng — lệnh sửa thật của người dùng mới, không nặn tài liệu bằng tay.</summary>
        private static void AddOneEvent(LiveOpsHubServices services)
        {
            LiveOpsHubEditOutcome outcome = services.Session.Apply(
                new AddFixedEventEdit(new FixedLiveEventEntry("entry-first", "first-event", "hunt",
                    "2026-09-20T00:00:00Z", "2026-09-22T00:00:00Z", string.Empty)),
                "Thêm first-event");
            Assert.IsTrue(outcome.Applied, outcome.FailureText);
        }

        /// <summary>Phiên trên asset lịch mẫu THẬT dưới thư mục test — cần cho mọi ca đi qua Lưu.</summary>
        private static LiveOpsHubServices CreateFileBackedServices(string fileName)
        {
            return LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                .WithCalendarAsset(LiveOpsHubTestServices.CreateAssetFile(fileName, LiveOpsDesignSample.Document)));
        }

        private static void MarkPublished(LiveOpsHubServices services)
        {
            LiveOpsHubEditOutcome outcome = services.Session.Publish.MarkPublished(string.Empty);
            Assert.IsTrue(outcome.Applied, outcome.FailureText);
        }
    }
}
