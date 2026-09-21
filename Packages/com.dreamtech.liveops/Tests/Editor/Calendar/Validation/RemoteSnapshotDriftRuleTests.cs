using System.Text;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class RemoteSnapshotDriftRuleTests
    {
        private static readonly UTF8Encoding Utf8WithoutByteOrderMark = new UTF8Encoding(false, false);

        [Test]
        public void Passes_WhenConditionAbsent()
        {
            // Bản dán đúng từng byte bản đã đăng → đường nhanh theo sha nguyên văn.
            LiveEventCalendarRuleResult result = Evaluate(PublishedBaselineSample.Document(), LiveOpsDesignSample.PublishedSha256Hex);

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, result.Outcome);
        }

        [Test]
        public void RemoteDrift_NotPasted_NotMeasured()
        {
            LiveEventCalendarRuleResult result = new RemoteSnapshotDriftRule().Evaluate(PublishedBaselineSample.ContextWithBaseline(LiveOpsDesignSample.Document));

            Assert.AreEqual(LiveEventCalendarRuleOutcome.NotMeasured, result.Outcome, "Chưa dán JSON đang chạy · luật này không tính là đã qua.");
            Assert.AreEqual(RemoteSnapshotDriftRule.RemoteNotPastedReasonCode, result.ReasonCode);
        }

        /// <summary>
        /// Phạm vi của test này là LUẬT: nhận sha nguyên văn của chuỗi đã đổi khoảng trắng + tài liệu parser đọc ra từ chuỗi đó (đổi
        /// khoảng trắng không đổi tài liệu). Assembly core không tham chiếu parser Unity nên tài liệu được dựng lại bằng tay; bước
        /// "parser đọc chuỗi CRLF/thụt 4 ra đúng tài liệu" cần test ở assembly có parser (đề xuất trong contract-changes của gói).
        /// Đối chứng âm cùng sha chứng minh Passed đến từ việc so tài liệu, không phải từ việc bỏ qua sha lạ.
        /// </summary>
        [Test]
        public void RemoteDrift_WhitespaceOnly_Passed()
        {
            // Firebase console đổi xuống dòng sang CRLF và thụt 4 cách: sha nguyên văn khác, tài liệu đọc ra giống hệt bản đã đăng.
            string reformatted = LiveOpsDesignSample.PublishedSnapshotJson.Replace("\n", "\r\n").Replace("  ", "    ");
            string reformattedSha = LiveEventCalendarSha256.ComputeHex(Utf8WithoutByteOrderMark.GetBytes(reformatted));
            Assert.AreNotEqual(LiveOpsDesignSample.PublishedSha256Hex, reformattedSha, "Tiền đề: đường nhanh không khớp.");
            Assert.AreEqual(LiveOpsDesignSample.PublishedSnapshotJson, reformatted.Replace("\r\n", "\n").Replace("    ", "  "),
                "Tiền đề: chuỗi chỉ khác khoảng trắng/xuống dòng.");

            LiveEventCalendarRuleResult result = Evaluate(PublishedBaselineSample.Document(), reformattedSha);

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, result.Outcome, "PD-31: chỉ khác khoảng trắng không phải lệch.");

            LiveEventCalendarDocument changedRemote = PublishedBaselineSample.Document();
            Assert.IsTrue(changedRemote.TryGetFixedEvent("published-1", out FixedLiveEventEntry hunt));
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(changedRemote, new ReplaceFixedEventEdit(hunt.WithConfigKey("hunt_v2")), out changedRemote));
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, Evaluate(changedRemote, reformattedSha).Outcome,
                "Đối chứng: cùng sha lạ nhưng tài liệu khác một field thì phải lệch.");
        }

        [Test]
        public void RemoteDrift_CanonicalRewriteEqual_Passed()
        {
            // Cùng lịch nhưng gõ khác: giờ thiếu giây / có phần lẻ 0, tiền tố bỏ trống (= mặc định), đợt không theo thứ tự thời gian.
            LiveEventCalendarDocument remote = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("weekly-pass", "2026-01-05T00:00Z", string.Empty, 168, 168, "weekly_pass_s3"))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00.000Z", "sky-race-", 24, 20, "sky_race_v4"))
                .WithFixedEvent(new FixedLiveEventEntry("remote-4", "star-tournament-2026-10", "star-tournament", "2026-10-03T00:00Z", "2026-10-06T00:00:00Z", "star_tournament_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("remote-1", "hunt-0914", "treasure-hunt", "2026-09-14T00:00:00Z", "2026-09-17T00:00:00Z", "hunt_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("remote-0", "lava-quest-2026-09a", "lava-quest", "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("remote-3", "lava-quest-2026-10", "lava-quest", "2026-10-01T00:00:00Z", "2026-10-03T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("remote-2", "lava-quest-2026-09b", "lava-quest", "2026-09-17T00:00:00+00:00", "2026-09-19T00:00:00Z", "lava_quest_v2"))
                .Build();
            Assert.AreEqual(LiveOpsDesignSample.PublishedSha256Hex, LiveEventCalendarJsonWriter.Write(remote, LiveEventCalendarJsonFormat.Version2).Sha256Hex,
                "Tiền đề: viết lại chuẩn ra đúng byte đã đăng.");

            LiveEventCalendarRuleResult result = Evaluate(remote, "0000000000000000000000000000000000000000000000000000000000000000");

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, result.Outcome);
        }

        [Test]
        public void RemoteDrift_ShaUppercase_FastPathPassed()
        {
            LiveEventCalendarRuleResult result = Evaluate(PublishedBaselineSample.Document(), LiveOpsDesignSample.PublishedSha256Hex.ToUpperInvariant());

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, result.Outcome);
        }

        [Test]
        public void Finds_TwoItemsDiffer_ListsIds()
        {
            // Bản đang chạy đã có hai thay đổi của nháp: lava-quest-2026-09b kéo tới 20/9 và thêm hunt-0916-bonus.
            LiveEventCalendarDocument baseline = PublishedBaselineSample.Document();
            Assert.IsTrue(baseline.TryGetFixedEvent("published-2", out FixedLiveEventEntry lavaMid));
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(baseline, new ReplaceFixedEventEdit(lavaMid.WithTimes(lavaMid.StartUtcText, "2026-09-20T00:00:00Z")),
                out LiveEventCalendarDocument remote));
            remote = new LiveEventCalendarDocumentBuilder(remote)
                .WithFixedEvent(new FixedLiveEventEntry("remote-bonus", "hunt-0916-bonus", "treasure-hunt", "2026-09-16T12:00:00Z", "2026-09-18T00:00:00Z", "hunt_bonus"))
                .Build();

            LiveEventCalendarRuleResult result = Evaluate(remote, "pasted-sha");

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, result.Outcome);
            Assert.AreEqual(1, result.Findings.Count, "Một phát hiện cho cả bản remote, nêu số mục và id.");
            LiveEventCalendarFinding finding = result.Findings[0];
            Assert.AreEqual(LiveEventCalendarDetailCodes.RemoteDiffers, finding.DetailCode);
            Assert.AreEqual("lava-quest-2026-09b" + LiveEventCalendarFindingBuilder.ValueSeparator + "hunt-0916-bonus", finding.FoundText,
                "Thứ tự: mục của bản so theo thứ tự xuất, rồi mục chỉ có ở bản dán.");
            Assert.AreEqual("2", finding.ExpectedText, "\"Bản remote khác dấu 11/9 16:20: 2 đợt\".");
        }

        [Test]
        public void Finds_RecurringRuleDiffers_ListsType()
        {
            LiveEventCalendarDocument remote = new LiveEventCalendarDocumentBuilder(PublishedBaselineSample.Document()).Build();
            Assert.IsTrue(remote.TryGetRecurringRule("weekly-pass", out RecurringLiveEventRule rule));
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(remote, new SetRecurringRuleEdit(rule.WithIdPrefix("pass-")), out remote));

            LiveEventCalendarFinding finding = Evaluate(remote, "pasted-sha").Findings[0];

            Assert.AreEqual("weekly-pass", finding.FoundText);
            Assert.AreEqual("1", finding.ExpectedText);
        }

        [Test]
        public void Finding_HasTargetAndRepairKind()
        {
            LiveEventCalendarDocument remote = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("remote-0", "lava-quest-2026-09a", "lava-quest", "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z", "lava_quest_v2"))
                .Build();

            LiveEventCalendarFinding finding = Evaluate(remote, "pasted-sha").Findings[0];

            Assert.AreEqual(LiveEventCalendarTargetKind.RemoteSnapshot, finding.TargetKind);
            Assert.AreEqual(string.Empty, finding.TargetId);
            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, finding.Consequence);
            Assert.AreEqual(LiveEventCalendarRepairKind.None, finding.RepairKind, "6.1: None + \"Xem diff\".");
            Assert.AreEqual(0, finding.Repairs.Count);
            Assert.IsTrue(finding.IsAboutRemoteSnapshot, "V-17: nói về JSON đang chạy, không vào Cần xử lý của nháp.");
        }

        [Test]
        public void RemoteDrift_NoStamp_FoundWithItemCount()
        {
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(LiveOpsDesignSample.DocumentWithoutPublishedStamp(), LiveOpsDesignSample.NowUtc)
                .WithRemoteSnapshot(PublishedBaselineSample.Document(), LiveOpsDesignSample.PublishedSha256Hex)
                .Build();

            LiveEventCalendarRuleResult result = new RemoteSnapshotDriftRule().Evaluate(context);

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, result.Outcome);
            LiveEventCalendarFinding finding = result.Findings[0];
            Assert.AreEqual(LiveEventCalendarDetailCodes.RemoteWithoutStamp, finding.DetailCode);
            Assert.AreEqual("7", finding.FoundText, "\"Bản remote có 7 mục nhưng chưa có dấu đã đăng để so\" — 2 luật + 5 đợt.");
            Assert.IsTrue(finding.IsAboutRemoteSnapshot);
        }

        [Test]
        public void RemoteDrift_OnlyOrderOfSameStartEventsDiffers_Passed()
        {
            // Hai đợt cùng giờ bắt đầu, thứ tự khác: sha chuẩn khác (hoà giờ giữ thứ tự asset) nhưng không mục nào khác.
            FixedLiveEventEntry first = new FixedLiveEventEntry("key-a", "quest-a", "quest", "2026-09-20T00:00:00Z", "2026-09-21T00:00:00Z", "quest_v1");
            FixedLiveEventEntry second = new FixedLiveEventEntry("key-b", "hunt-a", "hunt", "2026-09-20T00:00:00Z", "2026-09-21T00:00:00Z", "hunt_v1");
            LiveEventCalendarDocument baseline = new LiveEventCalendarDocumentBuilder().WithFixedEvent(first).WithFixedEvent(second).Build();
            LiveEventCalendarDocument remote = new LiveEventCalendarDocumentBuilder().WithFixedEvent(second).WithFixedEvent(first).Build();
            Assert.AreNotEqual(LiveEventCalendarJsonWriter.Write(baseline, LiveEventCalendarJsonFormat.Version2).Sha256Hex,
                LiveEventCalendarJsonWriter.Write(remote, LiveEventCalendarJsonFormat.Version2).Sha256Hex, "Tiền đề: tầng 2 không kết luận được.");
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(LiveEventCalendarDocument.Empty, LiveOpsDesignSample.NowUtc)
                .WithPublishedBaseline(baseline)
                .WithRemoteSnapshot(remote, "pasted-sha")
                .Build();

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, new RemoteSnapshotDriftRule().Evaluate(context).Outcome);
        }

        [Test]
        public void RemoteDrift_SameStartOverlapOrderSwapped_KeptEventDiffers_Found()
        {
            // Hai đợt CÙNG loại cùng giờ bắt đầu, chồng nhau: game giữ đợt đứng trước, bỏ đợt sau. Đảo thứ tự thì object từng id y
            // nguyên nhưng người chơi thấy đợt khác.
            FixedLiveEventEntry longer = new FixedLiveEventEntry("key-a", "quest-a", "quest", "2026-09-20T00:00:00Z", "2026-09-22T00:00:00Z", "quest_v1");
            FixedLiveEventEntry shorter = new FixedLiveEventEntry("key-b", "quest-b", "quest", "2026-09-20T00:00:00Z", "2026-09-21T00:00:00Z", "quest_v1");
            LiveEventCalendarDocument baseline = new LiveEventCalendarDocumentBuilder().WithFixedEvent(longer).WithFixedEvent(shorter).Build();
            LiveEventCalendarDocument remote = new LiveEventCalendarDocumentBuilder().WithFixedEvent(shorter).WithFixedEvent(longer).Build();
            Assert.IsTrue(LiveEventCalendarCompiler.CompileInExportOrder(baseline).TryGetFixedOutcome("key-a", out LiveEventCalendarEntryOutcome keptInBaseline));
            Assert.IsTrue(keptInBaseline.IsKept, "Tiền đề: bản so giữ quest-a.");
            Assert.IsTrue(LiveEventCalendarCompiler.CompileInExportOrder(remote).TryGetFixedOutcome("key-a", out LiveEventCalendarEntryOutcome droppedInRemote));
            Assert.IsFalse(droppedInRemote.IsKept, "Tiền đề: bản dán bỏ quest-a.");
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(LiveEventCalendarDocument.Empty, LiveOpsDesignSample.NowUtc)
                .WithPublishedBaseline(baseline)
                .WithRemoteSnapshot(remote, "pasted-sha")
                .Build();

            LiveEventCalendarRuleResult result = new RemoteSnapshotDriftRule().Evaluate(context);

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, result.Outcome);
            Assert.AreEqual("quest-a" + LiveEventCalendarFindingBuilder.ValueSeparator + "quest-b", result.Findings[0].FoundText);
            Assert.AreEqual("2", result.Findings[0].ExpectedText);
        }

        [Test]
        public void RemoteDrift_OlderBaselineSelected_ComparesWithLatestStamp()
        {
            // Người dùng chọn dấu cũ làm bản so trong phiên: luật 12 vẫn so với dấu mới nhất 11/9 16:20 của tài liệu.
            LiveEventCalendarDocument olderBaseline = OlderPublishedDocument();

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed,
                EvaluateWithBaseline(olderBaseline, PublishedBaselineSample.Document(), LiveOpsDesignSample.PublishedSha256Hex).Outcome,
                "Bản dán đúng dấu mới nhất: chọn bản so cũ không đổi kết quả.");
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed,
                EvaluateWithBaseline(PublishedBaselineSample.Document(), PublishedBaselineSample.Document(), LiveOpsDesignSample.PublishedSha256Hex).Outcome);

            string reformatted = LiveOpsDesignSample.PublishedSnapshotJson.Replace("\n", "\r\n");
            string reformattedSha = LiveEventCalendarSha256.ComputeHex(Utf8WithoutByteOrderMark.GetBytes(reformatted));
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, EvaluateWithBaseline(olderBaseline, PublishedBaselineSample.Document(), reformattedSha).Outcome,
                "Chỉ khác khoảng trắng: viết lại chuẩn bản dán ra đúng sha của dấu, không cần tài liệu của dấu.");
        }

        [Test]
        public void RemoteDrift_RemoteIsOlderStamp_NeverClaimsMatch()
        {
            // Firebase đang chạy bản cũ, bản so đang chọn cũng là bản cũ đó: không được nói "khớp" chỉ vì bản dán = bản so.
            LiveEventCalendarDocument olderBaseline = OlderPublishedDocument();
            string olderSha = LiveEventCalendarJsonWriter.Write(olderBaseline, LiveEventCalendarJsonFormat.Version2).Sha256Hex;

            LiveEventCalendarRuleResult result = EvaluateWithBaseline(olderBaseline, OlderPublishedDocument(), olderSha);

            Assert.AreNotEqual(LiveEventCalendarRuleOutcome.Passed, result.Outcome);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.NotMeasured, result.Outcome, "Core không có tài liệu của dấu mới nhất để liệt kê mục khác.");
            Assert.AreEqual(RemoteSnapshotDriftRule.LatestStampNotLoadedReasonCode, result.ReasonCode);

            LiveEventCalendarRuleResult withLatestBaseline = EvaluateWithBaseline(PublishedBaselineSample.Document(), OlderPublishedDocument(), olderSha);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, withLatestBaseline.Outcome, "Bản so là dấu mới nhất thì so từng mục như thường.");
        }

        [Test]
        public void RemoteDrift_OlderBaselineSelected_LatestStampBaselineLoaded_ComparesItemsWithLatestStamp()
        {
            // V-21 CC-VALB-1: phiên đã đọc tài liệu của dấu mới nhất. Bản so đang chọn là dấu cũ, Firebase cũng đang chạy dấu cũ đó:
            // luật 12 phải liệt kê đúng mục lệch so với dấu MỚI NHẤT, không "khớp" theo bản so và không còn "chưa kiểm".
            LiveEventCalendarDocument olderBaseline = OlderPublishedDocument();
            string olderSha = LiveEventCalendarJsonWriter.Write(olderBaseline, LiveEventCalendarJsonFormat.Version2).Sha256Hex;

            LiveEventCalendarRuleResult result = EvaluateWithLatestStampBaseline(olderBaseline, PublishedBaselineSample.Document(), OlderPublishedDocument(), olderSha);

            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, result.Outcome);
            Assert.AreEqual(1, result.Findings.Count);
            Assert.AreEqual(LiveEventCalendarDetailCodes.RemoteDiffers, result.Findings[0].DetailCode);
            Assert.AreEqual("lava-quest-2026-09b", result.Findings[0].FoundText, "Chỉ đợt đổi giờ kết thúc giữa hai dấu.");
            Assert.AreEqual("1", result.Findings[0].ExpectedText);
        }

        [Test]
        public void RemoteDrift_FormatOneStamp_LatestStampBaselineLoaded_CanonicalEqualPassed()
        {
            // Dấu định dạng 1 bỏ qua tầng 2a (định dạng 1 không ghi luật lặp), nên kết luận "cùng lịch" phải đến từ tài liệu của dấu.
            // Bản so đang chọn là bản cũ hơn (khác giờ kết thúc): thiếu tài liệu dấu thì "chưa kiểm", có thì Passed ở tầng 2b.
            LiveEventCalendarDocument stampDocument = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", "2026-09-12T00:00:00Z", "2026-09-15T00:00:00Z"));
            LiveEventCalendarDocument olderBaseline = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", "2026-09-12T00:00:00Z", "2026-09-14T00:00:00Z"));
            LiveEventCalendarJsonText stampJson = LiveEventCalendarJsonWriter.Write(stampDocument, LiveEventCalendarJsonFormat.Version1);
            LiveEventCalendarDocument draft = new LiveEventCalendarDocumentBuilder(stampDocument)
                .WithPublishedStamp(new PublishedCalendarStamp("2026-09-11T16:20:00Z", "tester", stampJson.Sha256Hex, stampJson.ByteCount,
                    (int)LiveEventCalendarJsonFormat.Version1, string.Empty, stampJson.Text))
                .Build();
            // Cùng đợt, gõ giờ thiếu giây: sha nguyên văn lạ, viết lại chuẩn định dạng 2 ra đúng bản chuẩn của dấu.
            LiveEventCalendarDocument remote = ValidationTestFixtures.FixedDocument(
                ValidationTestFixtures.Entry("quest-0912", "quest", "2026-09-12T00:00Z", "2026-09-15T00:00Z"));

            LiveEventCalendarCheckContextBuilder builder = new LiveEventCalendarCheckContextBuilder(draft, LiveOpsDesignSample.NowUtc)
                .WithPublishedBaseline(olderBaseline)
                .WithRemoteSnapshot(remote, "pasted-sha");
            LiveEventCalendarRuleResult withoutLatest = new RemoteSnapshotDriftRule().Evaluate(builder.Build());
            LiveEventCalendarRuleResult withLatest = new RemoteSnapshotDriftRule().Evaluate(builder.WithLatestStampBaseline(stampDocument).Build());

            Assert.AreEqual(LiveEventCalendarRuleOutcome.NotMeasured, withoutLatest.Outcome, "Tiền đề: bản so cũ không viết ra sha của dấu.");
            Assert.AreEqual(RemoteSnapshotDriftRule.LatestStampNotLoadedReasonCode, withoutLatest.ReasonCode);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Passed, withLatest.Outcome);
        }

        [Test]
        public void RemoteDrift_OlderBaselineSelected_LatestStampBaselineMissing_NotMeasuredLatestStampNotLoaded()
        {
            // V-21 CC-VALB-2: phiên chưa đọc được dấu mới nhất, bản so là dấu cũ, bản dán khác dấu mới nhất về nội dung → "chưa kiểm"
            // với mã riêng, không Found/Passed đoán theo bản so.
            LiveEventCalendarDocument olderBaseline = OlderPublishedDocument();
            string olderSha = LiveEventCalendarJsonWriter.Write(olderBaseline, LiveEventCalendarJsonFormat.Version2).Sha256Hex;

            LiveEventCalendarRuleResult result = EvaluateWithLatestStampBaseline(olderBaseline, null, OlderPublishedDocument(), olderSha);

            Assert.AreEqual(LiveEventCalendarRuleOutcome.NotMeasured, result.Outcome);
            Assert.AreEqual("latest-stamp-not-loaded", result.ReasonCode, "Mã NotMeasured đã chốt ở mục 3 (CC-VALB-2).");
            Assert.AreEqual(RemoteSnapshotDriftRule.LatestStampNotLoadedReasonCode, result.ReasonCode);
            Assert.AreEqual(0, result.Findings.Count);
        }

        [Test]
        public void RemoteDrift_LatestStampBaselineWinsOverSelectedBaseline_EvenWhenRemoteEqualsSelected()
        {
            // Bản dán = bản so đang chọn (dấu cũ) từng byte viết lại: nếu luật lỡ lấy bản so làm thước đo sẽ ra Passed giả.
            LiveEventCalendarDocument olderBaseline = OlderPublishedDocument();
            string olderSha = LiveEventCalendarJsonWriter.Write(olderBaseline, LiveEventCalendarJsonFormat.Version2).Sha256Hex;
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(LiveOpsDesignSample.Document, LiveOpsDesignSample.NowUtc)
                .WithPublishedBaseline(olderBaseline)
                .WithLatestStampBaseline(PublishedBaselineSample.Document())
                .WithRemoteSnapshot(OlderPublishedDocument(), olderSha)
                .Build();

            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(context);

            LiveEventCalendarRuleResult result = ResultOf(report, LiveEventCalendarRuleIds.RemoteSnapshotDrift);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.Found, result.Outcome, "Qua validator mặc định: context mang dấu mới nhất tới đúng luật 12.");
            Assert.IsTrue(result.Findings[0].IsAboutRemoteSnapshot);
        }

        /// <summary>Bản đã đăng trước 11/9: lava-quest-2026-09b còn kết thúc 18/9.</summary>
        private static LiveEventCalendarDocument OlderPublishedDocument()
        {
            LiveEventCalendarDocument older = PublishedBaselineSample.Document();
            Assert.IsTrue(older.TryGetFixedEvent("published-2", out FixedLiveEventEntry lavaMid));
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(older, new ReplaceFixedEventEdit(lavaMid.WithTimes(lavaMid.StartUtcText, "2026-09-18T00:00:00Z")),
                out older));
            return older;
        }

        private static LiveEventCalendarRuleResult EvaluateWithBaseline(LiveEventCalendarDocument baseline, LiveEventCalendarDocument remote, string remoteSha256Hex)
        {
            LiveEventCalendarCheckContext context = new LiveEventCalendarCheckContextBuilder(LiveOpsDesignSample.Document, LiveOpsDesignSample.NowUtc)
                .WithPublishedBaseline(baseline)
                .WithRemoteSnapshot(remote, remoteSha256Hex)
                .Build();
            return new RemoteSnapshotDriftRule().Evaluate(context);
        }

        private static LiveEventCalendarRuleResult EvaluateWithLatestStampBaseline(LiveEventCalendarDocument baseline, LiveEventCalendarDocument latestStampBaseline,
            LiveEventCalendarDocument remote, string remoteSha256Hex)
        {
            LiveEventCalendarCheckContextBuilder builder = new LiveEventCalendarCheckContextBuilder(LiveOpsDesignSample.Document, LiveOpsDesignSample.NowUtc)
                .WithPublishedBaseline(baseline)
                .WithRemoteSnapshot(remote, remoteSha256Hex);
            if (latestStampBaseline != null) builder.WithLatestStampBaseline(latestStampBaseline);
            return new RemoteSnapshotDriftRule().Evaluate(builder.Build());
        }

        private static LiveEventCalendarRuleResult ResultOf(LiveEventCalendarCheckReport report, string ruleId)
        {
            for (int index = 0; index < report.RuleResults.Count; index++)
            {
                if (report.RuleResults[index].RuleId == ruleId) return report.RuleResults[index];
            }
            Assert.Fail("Báo cáo không có kết quả luật " + ruleId);
            return null;
        }

        private static LiveEventCalendarRuleResult Evaluate(LiveEventCalendarDocument remote, string remoteSha256Hex)
        {
            return new RemoteSnapshotDriftRule().Evaluate(PublishedBaselineSample.ContextWithBaseline(LiveOpsDesignSample.Document, remote, remoteSha256Hex));
        }
    }
}
