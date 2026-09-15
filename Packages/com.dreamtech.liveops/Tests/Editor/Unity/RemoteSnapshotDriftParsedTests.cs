using System.Text;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;

namespace DreamTech.LiveOps.Unity.Tests
{
    /// <summary>
    /// Luật 12 (<c>remote-snapshot-drift</c>) chạy trên chuỗi JSON THẬT qua parser game (V-21 CC-VALB-5). Test cùng tên ở assembly core
    /// không tham chiếu parser nên dựng lại tài liệu bằng tay — bỏ sót đúng bước dễ vỡ nhất: parser có đọc chuỗi Firebase console đã
    /// đổi khoảng trắng/thứ tự key ra ĐÚNG tài liệu hay không. Ở đây chuỗi dán đi thẳng
    /// <see cref="JsonLiveEventCalendarParser.ParseDocument"/> → <see cref="LiveEventCalendarValidator.Default"/>, như phiên hub làm; tài
    /// liệu của dấu mới nhất cũng đọc từ <c>SnapshotJson</c> của dấu như G-SESSION sẽ truyền vào <c>LatestStampBaseline</c>.
    /// </summary>
    [TestFixture]
    public sealed class RemoteSnapshotDriftParsedTests
    {
        private const string LatestStampNotLoadedReasonCode = "latest-stamp-not-loaded";
        private static readonly UTF8Encoding Utf8WithoutByteOrderMark = new UTF8Encoding(false, false);

        [Test]
        public void ParsedWhitespaceOnly_Passed()
        {
            // Firebase console đổi xuống dòng sang CRLF và thụt 4 cách: sha nguyên văn khác, lịch y nguyên.
            string reformatted = LiveOpsDesignSample.PublishedSnapshotJson.Replace("\n", "\r\n").Replace("  ", "    ");
            Assert.AreNotEqual(LiveOpsDesignSample.PublishedSha256Hex, ShaOf(reformatted), "Tiền đề: đường nhanh theo sha nguyên văn không khớp.");

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, RemoteDriftResult(reformatted, ParseReadable(LiveOpsDesignSample.PublishedSnapshotJson), null).Outcome);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, RemoteDriftResult(reformatted, null, null).Outcome,
                "Không có bản so: dấu định dạng 2 là byte bộ ghi xuất, viết lại chuẩn bản dán là đủ.");

            // Đối chứng: cùng cách định dạng nhưng đổi một field thì phải lệch — Passed trên không đến từ việc bỏ qua sha lạ.
            string changed = reformatted.Replace("hunt_v1", "hunt_v2");
            LiveEventCalendarRuleResult changedResult = RemoteDriftResult(changed, ParseReadable(LiveOpsDesignSample.PublishedSnapshotJson), null);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, changedResult.Outcome);
            Assert.AreEqual("hunt-0914", changedResult.Findings[0].FoundText);
        }

        [Test]
        public void ParsedCanonicalRewriteEqual_Passed()
        {
            // Cùng lịch nhưng gõ khác hẳn: một dòng, key đảo thứ tự, đợt không theo thứ tự thời gian, giờ thiếu giây.
            const string rewritten =
                "{\"events\":[" +
                "{\"configKey\":\"star_tournament_v1\",\"endUtc\":\"2026-10-06T00:00Z\",\"startUtc\":\"2026-10-03T00:00Z\",\"type\":\"star-tournament\",\"id\":\"star-tournament-2026-10\"}," +
                "{\"type\":\"treasure-hunt\",\"id\":\"hunt-0914\",\"startUtc\":\"2026-09-14T00:00Z\",\"endUtc\":\"2026-09-17T00:00:00Z\",\"configKey\":\"hunt_v1\"}," +
                "{\"id\":\"lava-quest-2026-10\",\"type\":\"lava-quest\",\"startUtc\":\"2026-10-01T00:00:00Z\",\"endUtc\":\"2026-10-03T00:00:00Z\",\"configKey\":\"lava_quest_v2\"}," +
                "{\"id\":\"lava-quest-2026-09a\",\"type\":\"lava-quest\",\"startUtc\":\"2026-09-10T00:00:00Z\",\"endUtc\":\"2026-09-13T00:00Z\",\"configKey\":\"lava_quest_v2\"}," +
                "{\"id\":\"lava-quest-2026-09b\",\"type\":\"lava-quest\",\"startUtc\":\"2026-09-17T00:00:00Z\",\"endUtc\":\"2026-09-19T00:00:00Z\",\"configKey\":\"lava_quest_v2\"}]," +
                "\"recurring\":[" +
                "{\"configKey\":\"sky_race_v4\",\"activeHours\":20,\"periodHours\":24,\"idPrefix\":\"sky-race-\",\"anchorUtc\":\"2026-01-05T00:00Z\",\"type\":\"sky-race\"}," +
                "{\"type\":\"weekly-pass\",\"anchorUtc\":\"2026-01-05T00:00:00Z\",\"idPrefix\":\"weekly-pass-\",\"periodHours\":168,\"activeHours\":168,\"configKey\":\"weekly_pass_s3\"}]," +
                "\"version\":2}";
            Assert.AreNotEqual(LiveOpsDesignSample.PublishedSha256Hex, ShaOf(rewritten), "Tiền đề: đường nhanh theo sha nguyên văn không khớp.");

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, RemoteDriftResult(rewritten, ParseReadable(LiveOpsDesignSample.PublishedSnapshotJson), null).Outcome);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed,
                RemoteDriftResult(rewritten, null, ParseReadable(LiveOpsDesignSample.Document.LatestStamp.SnapshotJson)).Outcome,
                "Không bản so, có tài liệu dấu mới nhất đọc từ SnapshotJson (đường G-SESSION): so từng mục ra cùng lịch.");

            // Luật lặp đảo thứ tự nên viết lại chuẩn không ra đúng byte của dấu (bộ ghi giữ thứ tự luật lặp của asset): thiếu cả bản so
            // lẫn tài liệu dấu thì luật không có gì để so từng mục — chưa kiểm, không đoán Passed.
            LiveEventCalendarRuleResult withoutStampDocument = RemoteDriftResult(rewritten, null, null);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.NotMeasured, withoutStampDocument.Outcome);
            Assert.AreEqual(LatestStampNotLoadedReasonCode, withoutStampDocument.ReasonCode);

            string changed = rewritten.Replace("\"activeHours\":20", "\"activeHours\":21");
            LiveEventCalendarRuleResult changedResult = RemoteDriftResult(changed, ParseReadable(LiveOpsDesignSample.PublishedSnapshotJson), null);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, changedResult.Outcome, "Đối chứng: đổi một giá trị luật lặp thì phải lệch.");
            Assert.AreEqual("sky-race", changedResult.Findings[0].FoundText);
        }

        [Test]
        public void ParsedOlderStampRunning_LatestStampBaselineFromSnapshotJson_ComparesWithLatestStamp()
        {
            // Firebase đang chạy bản cũ (lava-quest-2026-09b còn khép 18/9) và người dùng chọn đúng bản cũ đó làm bản so trong phiên.
            string olderJson = LiveOpsDesignSample.PublishedSnapshotJson.Replace("\"endUtc\": \"2026-09-19T00:00:00Z\"", "\"endUtc\": \"2026-09-18T00:00:00Z\"");
            Assert.AreNotEqual(LiveOpsDesignSample.PublishedSnapshotJson, olderJson, "Tiền đề: bản cũ khác dấu mới nhất.");
            LiveEventCalendarDocument olderBaseline = ParseReadable(olderJson);
            // Đúng đường G-SESSION truyền: đọc SnapshotJson của dấu mới nhất bằng parser game.
            LiveEventCalendarDocument latestStampBaseline = ParseReadable(LiveOpsDesignSample.Document.LatestStamp.SnapshotJson);

            LiveEventCalendarRuleResult loaded = RemoteDriftResult(olderJson, olderBaseline, latestStampBaseline);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, loaded.Outcome, "Bản dán = bản so cũ nhưng khác dấu mới nhất: không được báo khớp.");
            Assert.AreEqual("lava-quest-2026-09b", loaded.Findings[0].FoundText);
            Assert.IsTrue(loaded.Findings[0].IsAboutRemoteSnapshot);

            LiveEventCalendarRuleResult notLoaded = RemoteDriftResult(olderJson, olderBaseline, null);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.NotMeasured, notLoaded.Outcome, "Thiếu tài liệu dấu mới nhất: chưa kiểm, không đoán theo bản so.");
            Assert.AreEqual(LatestStampNotLoadedReasonCode, notLoaded.ReasonCode);
        }

        // ----- Hỗ trợ -----

        private static LiveEventCalendarDocument ParseReadable(string json)
        {
            LiveEventCalendarDocumentParseResult parsed = JsonLiveEventCalendarParser.ParseDocument(json);
            Assert.IsTrue(parsed.IsReadable, parsed.ReadErrorText);
            return parsed.Document;
        }

        private static string ShaOf(string json)
        {
            return LiveEventCalendarSha256.ComputeHex(Utf8WithoutByteOrderMark.GetBytes(json));
        }

        /// <summary>Kiểm cả lịch mẫu bằng validator mặc định (không gọi thẳng luật) rồi lấy kết quả luật 12 theo id.</summary>
        private static LiveEventCalendarRuleResult RemoteDriftResult(string remoteJson, LiveEventCalendarDocument publishedBaseline,
            LiveEventCalendarDocument latestStampBaseline)
        {
            var builder = new LiveEventCalendarCheckContextBuilder(LiveOpsDesignSample.Document, LiveOpsDesignSample.NowUtc)
                .WithRemoteSnapshot(ParseReadable(remoteJson), ShaOf(remoteJson));
            if (publishedBaseline != null) builder.WithPublishedBaseline(publishedBaseline);
            if (latestStampBaseline != null) builder.WithLatestStampBaseline(latestStampBaseline);

            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(builder.Build());
            for (int index = 0; index < report.RuleResults.Count; index++)
            {
                if (report.RuleResults[index].RuleId == LiveEventCalendarRuleIds.RemoteSnapshotDrift) return report.RuleResults[index];
            }
            Assert.Fail("Báo cáo không có kết quả luật " + LiveEventCalendarRuleIds.RemoteSnapshotDrift);
            return null;
        }
    }
}
