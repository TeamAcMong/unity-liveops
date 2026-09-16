using System;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Card "Các lần đã đăng" [SD2 §3.9]: mới nhất trước, đúng một hàng mang cờ "đang là bản so", và chỉ lần mới nhất gỡ
    /// được dấu. Giờ dấu không đọc được vẫn phải hiện nguyên văn — asset sửa tay không được làm mất một dòng lịch sử.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class ExportHistoryModelTests
    {
        private const string FirstSha = "3f9a1caa11223344556677889900aabbccddeeff00112233445566778899aabb";
        private const string SecondSha = "5b0d93aa11223344556677889900aabbccddeeff00112233445566778899aabb";
        private const string FirstNote = "mở lava-quest tháng 9";
        private const string SecondNote = "mở hunt-0916-bonus";
        private const string BrokenStampTimeText = "11/9 16:20 sáng";

        private readonly LiveOpsHubFormat _format = new LiveOpsHubFormat(LiveOpsDesignSample.DeviceOffset);

        [Test]
        public void Rows_NewestFirst_WithActiveAndRemovableFlags()
        {
            LiveEventCalendarDocument document = DocumentWithStamps(
                Stamp("2026-09-11T16:20:00Z", FirstSha, FirstNote),
                Stamp("2026-09-13T09:04:00Z", SecondSha, SecondNote));

            ExportHistoryModel model = ExportHistoryModel.Build(document, document.LatestStamp, _format);

            Assert.AreEqual(2, model.Rows.Count);
            Assert.AreEqual(SecondNote, model.Rows[0].NoteText, "hàng đầu là lần đăng gần nhất");
            Assert.IsTrue(model.Rows[0].IsActiveBaseline, "dấu đang so nằm ở hàng mới nhất");
            Assert.IsTrue(model.Rows[0].CanRemoveStamp, "chỉ lần mới nhất gỡ được dấu");
            Assert.IsFalse(model.Rows[1].IsActiveBaseline);
            Assert.IsFalse(model.Rows[1].CanRemoveStamp, "gỡ dấu giữa danh sách làm lịch sử nói dối thứ tự đăng");
            Assert.AreEqual(LiveOpsHubTestServices.PublisherName, model.Rows[0].PublisherText);
            Assert.AreEqual("5b0d93", model.Rows[0].ShaText, "cột sha in 6 ký tự đầu");
        }

        [Test]
        public void ActiveStampNotLatest_MarksOnlyThatRow()
        {
            LiveEventCalendarDocument document = DocumentWithStamps(
                Stamp("2026-09-11T16:20:00Z", FirstSha, FirstNote),
                Stamp("2026-09-13T09:04:00Z", SecondSha, SecondNote));

            ExportHistoryModel model = ExportHistoryModel.Build(document, document.PublishedStamps[0], _format);

            Assert.IsFalse(model.Rows[0].IsActiveBaseline, "dấu mới nhất không còn là bản so");
            Assert.IsTrue(model.Rows[1].IsActiveBaseline);
            Assert.IsTrue(model.Rows[0].CanRemoveStamp, "gỡ dấu vẫn theo lần mới nhất, không theo bản so đang chọn");
        }

        [Test]
        public void UnreadableStampTime_KeepsRawText()
        {
            LiveEventCalendarDocument document = DocumentWithStamps(Stamp(BrokenStampTimeText, FirstSha, FirstNote));

            ExportHistoryModel model = ExportHistoryModel.Build(document, document.LatestStamp, _format);

            Assert.AreEqual(BrokenStampTimeText, model.Rows[0].TimeText, "giờ hỏng in nguyên văn thay vì bỏ hàng đi");
        }

        [Test]
        public void EmptyNote_ShowsPlaceholderSentence()
        {
            LiveEventCalendarDocument document = DocumentWithStamps(Stamp("2026-09-11T16:20:00Z", FirstSha, string.Empty));

            ExportHistoryModel model = ExportHistoryModel.Build(document, document.LatestStamp, _format);

            Assert.AreEqual(LiveOpsHubStrings.ExportHistoryNoNote, model.Rows[0].NoteText);
        }

        [Test]
        public void NoStamp_EmptySentence()
        {
            ExportHistoryModel model = ExportHistoryModel.Build(LiveOpsDesignSample.DocumentWithoutPublishedStamp(), null, _format);

            Assert.IsTrue(model.IsEmpty);
            Assert.AreEqual(LiveOpsHubStrings.ExportHistoryEmpty, model.EmptyText);
        }

        [Test]
        public void Build_NullFormat_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ExportHistoryModel.Build(LiveEventCalendarDocument.Empty, null, null));
        }

        private static PublishedCalendarStamp Stamp(string publishedUtcText, string sha256Hex, string note)
        {
            return new PublishedCalendarStamp(publishedUtcText, LiveOpsHubTestServices.PublisherName, sha256Hex, 1612, 2, note, "{}");
        }

        private static LiveEventCalendarDocument DocumentWithStamps(params PublishedCalendarStamp[] stamps)
        {
            LiveEventCalendarDocumentBuilder builder = new LiveEventCalendarDocumentBuilder()
                .WithRemoteConfigKey(LiveEventCalendarDocument.DefaultRemoteConfigKey);
            foreach (PublishedCalendarStamp stamp in stamps) builder.WithPublishedStamp(stamp);
            return builder.Build();
        }
    }
}
