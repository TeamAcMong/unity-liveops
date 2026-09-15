using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// (V-8) Nguồn câu duy nhất của phát hiện Kiểm lịch. Phát hiện dựng bằng <see cref="LiveEventCalendarFindingBuilder"/> với đúng giá
    /// trị thô luật core ghi (xem comment của từng luật) — không cần luật chạy thật để có câu, và (V-21 D-6) hàng mẫu thiết kế khoá chữ
    /// mà không phụ thuộc validator. Hai test cần mã lý do/hình dạng dữ liệu THẬT (CC-VALB-2/3/4) chạy validator để bắt lệch khi core đổi.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class LiveOpsFindingTextTests
    {
        private static readonly DateTime NowUtc = LiveOpsDesignSample.NowUtc;
        private static readonly Regex NoParseSegment = new Regex("<noparse>.*?</noparse>", RegexOptions.Singleline);

        /// <summary>Tên asset lịch của mẫu thiết kế [SD2 §2.3 hàng 5] "lưu trong Main.asset".</summary>
        private const string DesignCalendarAssetName = "Main.asset";

        /// <summary>Vùng noparse như bộ parse rich text thấy: mở bằng thẻ viết thường của lớp câu, đóng ở thẻ đóng ĐẦU TIÊN mọi kiểu hoa thường.</summary>
        private static readonly Regex ParserNoParseSegment = new Regex("<noparse>.*?</noparse>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static LiveOpsHubFormat Format => new LiveOpsHubFormat(LiveOpsDesignSample.DeviceOffset);

        // =============================================================================================================== đủ câu

        [Test]
        public void EveryRuleIdAndDetailCode_HasSentence()
        {
            int pairCount = 0;
            foreach (string ruleId in LiveEventCalendarRuleIds.All)
            {
                Assert.IsNotEmpty(LiveOpsFindingText.WhySentence(ruleId), "VÌ SAO của " + ruleId);
                IReadOnlyList<string> detailCodes = LiveEventCalendarDetailCodes.ForRule(ruleId);
                Assert.IsNotEmpty(detailCodes, ruleId);

                // Nhóm = (loại đích, field của luật 3): chỉ trong cùng nhóm hai biến thể mới cạnh tranh một hàng. Gộp cả hai loại đích vào một
                // tập làm luật 9 có tới 10 headline cho 5 mã — hai mã trùng câu vẫn qua ngưỡng.
                var headlinesByGroup = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
                foreach (string detailCode in detailCodes)
                {
                    pairCount++;
                    Assert.IsTrue(LiveOpsFindingText.HasSentence(ruleId, detailCode), ruleId + " / " + detailCode + " chưa có câu");

                    foreach (LiveEventCalendarFinding finding in SampleFindings(ruleId, detailCode, "hunt-0914"))
                    {
                        string label = ruleId + " / " + detailCode + " / " + finding.TargetKind + " / " + finding.RepairKind;
                        string headline = LiveOpsFindingText.Headline(finding, Format);
                        AssertSentence(headline, label + " Headline");
                        Assert.AreNotEqual(LiveOpsFindingText.RuleIdLine(finding), headline, label + ": headline rơi về câu dự phòng");
                        AssertSentence(LiveOpsFindingText.Meta(finding, Format), label + " Meta");
                        AssertSentence(LiveOpsFindingText.Meta(finding, Format, NowUtc), label + " Meta(now)");
                        AssertSentence(LiveOpsFindingText.ShortLabel(finding), label + " ShortLabel");
                        AssertSentence(LiveOpsFindingText.ConsequenceSentence(finding, Format), label + " ConsequenceSentence");
                        StringAssert.DoesNotContain("<noparse>", LiveOpsFindingText.ShortLabel(finding), label + ": mảnh tooltip không mang giá trị thô");
                        string group = finding.TargetKind + "|" + (ruleId == LiveEventCalendarRuleIds.InvalidIdentifier ? finding.ExpectedText : string.Empty);
                        if (!headlinesByGroup.TryGetValue(group, out Dictionary<string, string> headlineByCode))
                        {
                            headlineByCode = new Dictionary<string, string>(StringComparer.Ordinal);
                            headlinesByGroup.Add(group, headlineByCode);
                        }
                        if (!headlineByCode.ContainsKey(detailCode)) headlineByCode.Add(detailCode, LiveOpsFindingText.PlainText(headline));
                        foreach (LiveEventCalendarRepair repair in finding.Repairs)
                        {
                            AssertSentence(LiveOpsFindingText.RepairOptionText(finding, repair, Format), label + " RepairOptionText " + repair.RepairId);
                        }
                    }
                }
                // Các biến thể trong một luật phải nói KHÁC nhau — cùng câu cho "xoá" và "đổi loại" là người đọc không biết mình phải làm gì.
                if (ruleId != LiveEventCalendarRuleIds.UtcTimeFormat && ruleId != LiveEventCalendarRuleIds.ConfigKeyMissing)
                {
                    foreach (KeyValuePair<string, Dictionary<string, string>> group in headlinesByGroup)
                    {
                        var distinct = new HashSet<string>(group.Value.Values, StringComparer.Ordinal);
                        Assert.AreEqual(group.Value.Count, distinct.Count, ruleId + " / " + group.Key + ": biến thể trùng headline — " +
                            string.Join(" | ", group.Value.Values));
                    }
                }
            }
            Assert.Greater(pairCount, 30, "Danh sách mã đóng của 12 luật");
            Assert.IsFalse(LiveOpsFindingText.HasSentence(LiveEventCalendarRuleIds.OverlapSameType, "not-a-code"));
            Assert.IsFalse(LiveOpsFindingText.HasSentence("not-a-rule", LiveEventCalendarDetailCodes.OverlapKeptEarlier));
        }

        [Test]
        public void ShortLabel_DesignTooltipFragments()
        {
            LiveEventCalendarFinding overlap = OverlapFinding();
            LiveEventCalendarFinding endUnreadable = UtcTimeFinding();
            LiveEventCalendarFinding configKey = ConfigKeyFinding();
            LiveEventCalendarFinding longGap = LongGapFinding();

            Assert.AreEqual("chồng giờ", LiveOpsFindingText.ShortLabel(overlap));
            Assert.AreEqual("giờ kết thúc sai định dạng", LiveOpsFindingText.ShortLabel(endUnreadable));
            Assert.AreEqual("không tự khai configKey", LiveOpsFindingText.ShortLabel(configKey));
            Assert.AreEqual("trống 11 ngày", LiveOpsFindingText.ShortLabel(longGap));

            // Ghép như tooltip section Lịch [FD §3.1]: "2 đợt bị bỏ: hunt-0916-bonus (chồng giờ), lava-quest-2026-10 (giờ kết thúc sai định
            // dạng) · 2 nên xem: hunt-0914 không tự khai configKey, lava-quest trống 11 ngày" — mảnh câu không lệch thiết kế một chữ.
            string dropped = overlap.TargetId + " (" + LiveOpsFindingText.ShortLabel(overlap) + "), " +
                             endUnreadable.TargetId + " (" + LiveOpsFindingText.ShortLabel(endUnreadable) + ")";
            string review = configKey.TargetId + " " + LiveOpsFindingText.ShortLabel(configKey) + ", " +
                            longGap.TargetId + " " + LiveOpsFindingText.ShortLabel(longGap);
            Assert.AreEqual("hunt-0916-bonus (chồng giờ), lava-quest-2026-10 (giờ kết thúc sai định dạng)", dropped);
            Assert.AreEqual("hunt-0914 không tự khai configKey, lava-quest trống 11 ngày", review);

            Assert.AreEqual("đổi tiền tố khi đang chạy", LiveOpsFindingText.ShortLabel(RunningPrefixFinding(true)));
            Assert.AreEqual("loại chưa khai báo", LiveOpsFindingText.ShortLabel(Builder(LiveEventCalendarRuleIds.UnknownEventType,
                LiveEventCalendarDetailCodes.UnknownTypeInDraft, LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.EventType, "lucky-spin")
                .WithTexts("2", string.Empty).Build()));
        }

        [Test]
        public void AlsoWrongText_ListsAdditionalItemReasons()
        {
            // Đợt vừa hỏng giờ kết thúc (lý do chính) vừa có id chứa '#': bộ biên dịch thật gom lỗi id vào AdditionalItemReasons (V-7).
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("entry-broken", "lava#10", "lava-quest", "2026-10-01T00:00:00Z", "2026-10-3", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-clean", "lava-quest-2026-11", "lava-quest", "2026-11-01T00:00:00Z", "2026-11-03T00:00:00Z", "lava_quest_v2"))
                .Build();
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.CompileInExportOrder(document);

            Assert.IsTrue(compilation.TryGetFixedOutcome("entry-broken", out LiveEventCalendarEntryOutcome broken));
            Assert.AreEqual(LiveEventCalendarDropReason.UnreadableEndUtc, broken.DropReason);
            CollectionAssert.Contains(broken.AdditionalItemReasons, LiveEventCalendarDropReason.InvalidIdentifier);
            Assert.AreEqual("cũng sai: id chứa '#'", LiveOpsFindingText.AlsoWrongText(broken));

            Assert.IsTrue(compilation.TryGetFixedOutcome("entry-clean", out LiveEventCalendarEntryOutcome clean));
            Assert.AreEqual(string.Empty, LiveOpsFindingText.AlsoWrongText(clean), "Mục không có lỗi phụ không in \"cũng sai: \" rỗng.");
            Assert.AreEqual(string.Empty, LiveOpsFindingText.AlsoWrongText(null));

            // Nhiều lỗi phụ, loại (không phải id) sai: nêu đủ, theo thứ tự, đúng ô.
            var multiple = new LiveEventCalendarEntryOutcome(LiveEventCalendarEntryKind.FixedEvent, 0, "entry-multiple", "lava-quest-2026-12", string.Empty,
                false, LiveEventCalendarDropReason.UnreadableStartUtc,
                new[] { LiveEventCalendarDropReason.UnreadableEndUtc, LiveEventCalendarDropReason.InvalidIdentifier }, string.Empty, null, null, string.Empty);
            Assert.AreEqual("cũng sai: giờ kết thúc sai định dạng, loại rỗng", LiveOpsFindingText.AlsoWrongText(multiple));
        }

        // =============================================================================================================== mẫu thiết kế

        [Test]
        public void DesignSample_HeadlinesMatchDesign()
        {
            // (V-21 D-6) 5 phát hiện mẫu 6.2 dựng bằng builder, giá trị thô đúng như luật ghi — chữ so nguyên văn [SD2 §2.3].
            AssertRow(UtcTimeFinding(),
                "lava-quest-2026-10 có giờ kết thúc sai định dạng",
                "tìm thấy \"2026-10-3\" · cần 2026-10-03T00:00:00Z",
                "utc-time-format · lava-quest-2026-10", "Sửa", "Xem trong lịch",
                "An toàn: chỉ chuẩn hoá chữ, không đổi điều người chơi thấy");
            AssertRow(OverlapFinding(),
                "hunt-0916-bonus chồng 12 giờ với hunt-0914",
                "16/9 12:00 → 17/9 00:00 UTC · game giữ đợt bắt đầu sớm hơn",
                "overlap-same-type · hunt-0916-bonus", "Đề xuất…", "Xem trong lịch",
                "Đổi điều người chơi thấy — áp từng cái");
            AssertRow(RunningPrefixFinding(true),
                "weekly-pass đổi tiền tố khi weekly-pass-35 đang chạy",
                "weekly-pass-35 → pass-35 · đang chạy tới 14/9 00:00 UTC",
                "running-event-id-changed · weekly-pass", "Quyết định…", "Mở luật",
                "Hai cách đúng: giữ tiền tố weekly-pass-, hoặc để sau khi weekly-pass-35 khép (14/9 00:00 UTC)");
            AssertRow(ConfigKeyFinding(),
                "hunt-0914 không tự khai configKey — xuất sẽ ghi hunt_default",
                "đợt chưa bắt đầu (14/9 00:00 UTC) · bản đã đăng dùng hunt_v1",
                "config-key-missing · hunt-0914", string.Empty, "Xem trong lịch", string.Empty);
            AssertRow(LongGapFinding(),
                "lava-quest trống 11 ngày (20/9 → 1/10)",
                "không có đợt lava-quest nào trong 20/9 00:00 → 1/10 00:00 UTC",
                "long-gap-between-events · lava-quest", "Bỏ qua cảnh báo…", string.Empty,
                "Chỉ khoảng này, ghi chú bắt buộc, lưu trong Main.asset");
            Assert.AreEqual("Chỉ khoảng này, ghi chú bắt buộc, lưu trong asset lịch",
                LiveOpsFindingText.PlainText(LiveOpsFindingText.PrimaryButtonTooltip(LongGapFinding(), Format)), "Không biết tên asset: câu chung, không bỏ trống.");

            // Pane Chi tiết của hàng overlap: hậu quả trước, vì sao sau, hai nút "CÁCH SỬA" [SD2 §2.3 overlap].
            LiveEventCalendarFinding overlap = OverlapFinding();
            Assert.AreEqual("Không thấy đợt hunt-0916-bonus (16/9 12:00); hunt-0914 chạy bình thường.",
                LiveOpsFindingText.PlainText(LiveOpsFindingText.ConsequenceSentence(overlap, Format)));
            Assert.AreEqual("FixedLiveEventCalendar bỏ đợt chồng giờ cùng loại và giữ đợt bắt đầu sớm hơn.", LiveOpsFindingText.WhySentence(overlap.RuleId));
            Assert.AreEqual("Dời tới 17/9 00:00 (24 giờ)…", LiveOpsFindingText.PlainText(LiveOpsFindingText.RepairOptionText(overlap, overlap.Repairs[0], Format)));
            Assert.AreEqual("Giữ 36 giờ: tới 18/9 12:00…", LiveOpsFindingText.PlainText(LiveOpsFindingText.RepairOptionText(overlap, overlap.Repairs[1], Format)));

            // Menu "Quyết định… ▾" [SD2 §2.3 running-event-id-changed].
            LiveEventCalendarFinding weeklyPass = RunningPrefixFinding(true);
            Assert.AreEqual("Giữ tiền tố weekly-pass- (hoàn về)", LiveOpsFindingText.PlainText(LiveOpsFindingText.RepairOptionText(weeklyPass, weeklyPass.Repairs[0], Format)));
            Assert.AreEqual("Để sau khi weekly-pass-35 khép (14/9 00:00 UTC)…",
                LiveOpsFindingText.PlainText(LiveOpsFindingText.RepairOptionText(weeklyPass, weeklyPass.Repairs[1], Format)));
            Assert.AreEqual("weekly-pass-35 đang chạy tới 14/9 00:00 UTC; người chơi có điểm bắt đầu lại từ 0.",
                LiveOpsFindingText.PlainText(LiveOpsFindingText.ConsequenceSentence(weeklyPass, Format)));
            Assert.AreEqual(string.Empty, LiveOpsFindingText.ManualFixSentence(weeklyPass, Format), "Ca thường có Decision ×2 — không hiện câu sửa tay.");

            // Hàng Chưa kiểm của mẫu (chưa dán JSON đang chạy).
            LiveEventCalendarRuleResult notPasted = LiveEventCalendarRuleResult.NotMeasured(LiveEventCalendarRuleIds.RemoteSnapshotDrift, LiveOpsFindingText.RemoteNotPastedReasonCode);
            Assert.AreEqual("So với bản đang chạy trên remote config", LiveOpsFindingText.RuleResultHeadline(notPasted));
            Assert.AreEqual("Chưa dán JSON đang chạy · luật này không tính là đã qua", LiveOpsFindingText.RuleResultMeta(notPasted, Format, null));
            Assert.AreEqual("Dán JSON đang chạy…", LiveOpsFindingText.RuleResultButtonText(notPasted));
        }

        [Test]
        public void Texts_UseNoParseForRawValues()
        {
            const string hostileId = "<b>hunt</b>";
            foreach (string ruleId in LiveEventCalendarRuleIds.All)
            {
                foreach (string detailCode in LiveEventCalendarDetailCodes.ForRule(ruleId))
                {
                    foreach (LiveEventCalendarFinding finding in SampleFindings(ruleId, detailCode, hostileId))
                    {
                        string label = ruleId + " / " + detailCode + " / " + finding.TargetKind;
                        AssertNoTagOutsideNoParse(LiveOpsFindingText.Headline(finding, Format), label + " Headline");
                        AssertNoTagOutsideNoParse(LiveOpsFindingText.Meta(finding, Format, NowUtc), label + " Meta");
                        AssertNoTagOutsideNoParse(LiveOpsFindingText.ConsequenceSentence(finding, Format), label + " ConsequenceSentence");
                        AssertNoTagOutsideNoParse(LiveOpsFindingText.RuleIdLine(finding), label + " RuleIdLine");
                        AssertNoTagOutsideNoParse(LiveOpsFindingText.ManualFixSentence(finding, Format), label + " ManualFixSentence");
                        foreach (LiveEventCalendarRepair repair in finding.Repairs)
                        {
                            AssertNoTagOutsideNoParse(LiveOpsFindingText.RepairOptionText(finding, repair, Format), label + " " + repair.RepairId);
                        }
                    }
                }
            }

            // Giá trị thô mang đúng thẻ đóng noparse không được thoát khỏi vùng noparse; PlainText trả lại nguyên văn.
            // Bộ parse so tên thẻ không phân biệt hoa thường (TMP_Text: ToUpperFast) nên mọi biến thể phải được thoát, kể cả thẻ mở lồng.
            foreach (string closingTag in new[] { "x</noparse><b>y", "x</NoParse><b>y", "x</NOPARSE ><b>y", "<NoParse>x</noparse><b>y", "a<noparse" })
            {
                string wrapped = LiveOpsFindingText.NoParse(closingTag);
                AssertNoTagOutsideNoParse(wrapped, "thẻ noparse trong giá trị " + closingTag);
                string outsideAsParser = ParserNoParseSegment.Replace(wrapped, string.Empty);
                StringAssert.DoesNotContain("<", outsideAsParser, "Giá trị " + closingTag + " đóng vùng noparse như bộ parse thấy — " + wrapped);
                Assert.AreEqual(closingTag, LiveOpsFindingText.PlainText(wrapped), "PlainText trả lại nguyên văn " + closingTag);
            }

            LiveEventCalendarFinding overlap = Builder(LiveEventCalendarRuleIds.OverlapSameType, LiveEventCalendarDetailCodes.OverlapKeptEarlier,
                    LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, hostileId)
                .WithRelatedId("hunt-0914").WithRange(Utc(9, 16, 12), Utc(9, 17, 0)).Build();
            Assert.AreEqual("<b>hunt</b> chồng 12 giờ với hunt-0914", LiveOpsFindingText.PlainText(LiveOpsFindingText.Headline(overlap, Format)),
                "Tooltip (không rich text) hiện đúng id có dấu <>.");
            Assert.AreEqual("id\\ncó xuống dòng", LiveOpsFindingText.PlainText(LiveOpsFindingText.NoParse("id\ncó xuống dòng")),
                "Xuống dòng trong id hiện thành \\n — không làm vỡ hàng.");

            LiveEventCalendarRuleResult failed = LiveEventCalendarRuleResult.Failed(LiveEventCalendarRuleIds.LongGapBetweenEvents, "NullReferenceException", "<color=red>boom</color>");
            AssertNoTagOutsideNoParse(LiveOpsFindingText.RuleResultMeta(failed, Format, null), "message exception");
            Assert.AreEqual("Luật long-gap-between-events không chạy được: NullReferenceException",
                LiveOpsFindingText.PlainText(LiveOpsFindingText.RuleResultHeadline(failed)), "[SD2 §2.8] luật ném");
            Assert.AreEqual("Copy lỗi", LiveOpsFindingText.RuleResultButtonText(failed));
        }

        // =============================================================================================================== V-21

        [Test]
        public void RunningIdChanged_NoRepair_SentenceNamesManualFix()
        {
            // Dữ liệu THẬT của ca CC-VALB-3: mục loại khác mang cùng id đứng trước ở thứ tự xuất → game bỏ đợt đang chạy, không lệnh hoàn
            // về nào cứu được → RepairKind None.
            LiveEventCalendarDocument baseline = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("entry-running", "quest-0912", "quest", "2026-09-12T00:00:00Z", "2026-09-15T00:00:00Z", "quest_v1"))
                .Build();
            LiveEventCalendarDocument draft = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("entry-running", "quest-0912", "quest", "2026-09-12T00:00:00Z", "2026-09-15T00:00:00Z", "quest_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-blocker", "quest-0912", "hunt", "2026-09-11T00:00:00Z", "2026-09-12T00:00:00Z", "hunt_v1"))
                .Build();
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(
                new LiveEventCalendarCheckContextBuilder(draft, NowUtc).WithPublishedBaseline(baseline).Build());
            LiveEventCalendarFinding fixedFinding = FindingOf(report, LiveEventCalendarRuleIds.RunningEventIdChanged);
            Assert.AreEqual(LiveEventCalendarRepairKind.None, fixedFinding.RepairKind, "Tiền đề: ca không lệnh sửa của G-VALIDATOR-B.");

            string manualFix = LiveOpsFindingText.PlainText(LiveOpsFindingText.ManualFixSentence(fixedFinding, Format));
            StringAssert.Contains("Mở Lịch, khôi phục đợt id quest-0912", manualFix, "Câu phải chỉ đúng việc tay: khôi phục id đợt đang chạy trong Lịch.");
            StringAssert.Contains("12/9 00:00 → 15/9 00:00 UTC", manualFix, "…và khung giờ đợt đang chạy.");
            StringAssert.Contains("đổi id hoặc dời mục đang chắn", manualFix);
            Assert.AreEqual("Xem trong lịch", LiveOpsFindingText.PrimaryButtonText(fixedFinding));
            Assert.AreEqual(string.Empty, LiveOpsFindingText.LinkText(fixedFinding), "Nút chính đã là link — không lặp link phụ.");
            StringAssert.EndsWith("không hoàn về tự động được — sửa tay trong Lịch", LiveOpsFindingText.PlainText(LiveOpsFindingText.Meta(fixedFinding, Format)));
            Assert.AreEqual("quest-0912 bị xoá hoặc bị game bỏ khi đang chạy", LiveOpsFindingText.PlainText(LiveOpsFindingText.Headline(fixedFinding, Format)));

            // Luật lặp: nút là "Mở luật", câu chỉ vào luật + tiền tố/neo/chu kỳ của bản đã đăng.
            LiveEventCalendarFinding recurringFinding = RunningPrefixFinding(false);
            Assert.AreEqual("Mở luật", LiveOpsFindingText.PrimaryButtonText(recurringFinding));
            string recurringFix = LiveOpsFindingText.PlainText(LiveOpsFindingText.ManualFixSentence(recurringFinding, Format));
            StringAssert.Contains("Mở luật weekly-pass, trả tiền tố, neo và chu kỳ về như bản đã đăng", recurringFix);
            StringAssert.Contains("vẫn là weekly-pass-35 (7/9 00:00 → 14/9 00:00 UTC)", recurringFix);
            StringAssert.EndsWith("không hoàn về tự động được — sửa tay ở Luật lặp", LiveOpsFindingText.PlainText(LiveOpsFindingText.Meta(recurringFinding, Format)),
                "Meta chỉ cùng màn với nút \"Mở luật\" và câu sửa tay — không bảo sửa trong Lịch.");

            foreach (LiveEventCalendarFinding finding in new[] { fixedFinding, recurringFinding })
            {
                string primary = LiveOpsFindingText.PrimaryButtonText(finding);
                Assert.AreNotEqual("Quyết định…", primary, "Không có lựa chọn thì không có menu Quyết định rỗng.");
                Assert.AreNotEqual("Xem diff", primary, "Xem diff là của luật 12.");
            }
        }

        [Test]
        public void RemoteDrift_NotMeasuredReasonCodes_HaveSentence()
        {
            LiveOpsHubFormat format = Format;

            // Chưa dán JSON đang chạy — chạy luật thật để lấy mã lý do core đang phát.
            LiveEventCalendarCheckReport notPasted = LiveEventCalendarValidator.Default.Check(
                new LiveEventCalendarCheckContextBuilder(LiveOpsDesignSample.DocumentWithoutPublishedStamp(), NowUtc).Build());
            LiveEventCalendarRuleResult remoteNotPasted = ResultOf(notPasted, LiveEventCalendarRuleIds.RemoteSnapshotDrift);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.NotMeasured, remoteNotPasted.Outcome);
            Assert.AreEqual(LiveOpsFindingText.RemoteNotPastedReasonCode, remoteNotPasted.ReasonCode, "Mã chép ở Editor phải khớp core.");
            Assert.IsTrue(LiveOpsFindingText.HasRuleResultSentence(remoteNotPasted.RuleId, remoteNotPasted.Outcome, remoteNotPasted.ReasonCode));
            Assert.AreEqual("Chưa dán JSON đang chạy · luật này không tính là đã qua", LiveOpsFindingText.RuleResultMeta(remoteNotPasted, format, null));

            LiveEventCalendarRuleResult runningNotApplicable = ResultOf(notPasted, LiveEventCalendarRuleIds.RunningEventIdChanged);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.NotApplicable, runningNotApplicable.Outcome);
            Assert.AreEqual(LiveOpsFindingText.NoPublishedStampReasonCode, runningNotApplicable.ReasonCode);
            Assert.IsTrue(LiveOpsFindingText.HasRuleResultSentence(runningNotApplicable.RuleId, runningNotApplicable.Outcome, runningNotApplicable.ReasonCode));
            AssertSentence(LiveOpsFindingText.RuleResultMeta(runningNotApplicable, format, null), "no-published-stamp");
            Assert.AreEqual("không áp dụng: chưa có dấu đã đăng", LiveOpsFindingText.RuleResultNotApplicableLabel(runningNotApplicable), "(PD-9) nhãn card Đã qua");
            Assert.AreEqual("không áp dụng: lý do future-reason", LiveOpsFindingText.PlainText(LiveOpsFindingText.RuleResultNotApplicableLabel(
                LiveEventCalendarRuleResult.NotApplicable(LiveEventCalendarRuleIds.LongGapBetweenEvents, "future-reason"))));
            Assert.AreEqual(string.Empty, LiveOpsFindingText.RuleResultNotApplicableLabel(remoteNotPasted), "Chưa kiểm không phải Không áp dụng.");

            // (V-21 CC-VALB-2) Đã dán, tài liệu có dấu mới nhất nhưng phiên không truyền tài liệu của dấu và bản so không phải dấu đó.
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            LiveEventCalendarJsonText remoteJson = LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2);
            LiveEventCalendarCheckReport notLoaded = LiveEventCalendarValidator.Default.Check(
                new LiveEventCalendarCheckContextBuilder(document, NowUtc).WithRemoteSnapshot(document, remoteJson.Sha256Hex).Build());
            LiveEventCalendarRuleResult latestStampNotLoaded = ResultOf(notLoaded, LiveEventCalendarRuleIds.RemoteSnapshotDrift);
            Assert.AreEqual(LiveEventCalendarRuleOutcome.NotMeasured, latestStampNotLoaded.Outcome);
            Assert.AreEqual(LiveOpsFindingText.LatestStampNotLoadedReasonCode, latestStampNotLoaded.ReasonCode, "Mã chép ở Editor phải khớp core.");
            Assert.IsTrue(LiveOpsFindingText.HasRuleResultSentence(latestStampNotLoaded.RuleId, latestStampNotLoaded.Outcome, latestStampNotLoaded.ReasonCode));
            Assert.AreEqual("So với bản đang chạy trên remote config", LiveOpsFindingText.RuleResultHeadline(latestStampNotLoaded));
            Assert.AreEqual("Chưa so được với dấu đã đăng mới nhất 11/9 16:20 — bản so đang chọn là dấu khác.",
                LiveOpsFindingText.RuleResultMeta(latestStampNotLoaded, format, document.LatestStamp));
            Assert.AreEqual("Chưa so được với dấu đã đăng mới nhất — bản so đang chọn là dấu khác.",
                LiveOpsFindingText.RuleResultMeta(latestStampNotLoaded, format, null));
            Assert.AreEqual(string.Empty, LiveOpsFindingText.RuleResultButtonText(latestStampNotLoaded), "Đã dán rồi — không mời dán lại.");

            // Mã lạ vẫn có câu chung, không hàng trống.
            LiveEventCalendarRuleResult unknownReason = LiveEventCalendarRuleResult.NotMeasured(LiveEventCalendarRuleIds.RemoteSnapshotDrift, "future-reason");
            Assert.IsFalse(LiveOpsFindingText.HasRuleResultSentence(unknownReason.RuleId, unknownReason.Outcome, unknownReason.ReasonCode));
            AssertSentence(LiveOpsFindingText.RuleResultMeta(unknownReason, format, null), "mã lạ");
        }

        [Test]
        public void DeferUntilEndReminder_RangeShowsDueTimeNotOpenRange()
        {
            // (V-21 CC-VALB-4) Ghi chú hẹn do lựa chọn "Để sau khi … khép" THẬT sinh ra: khoảng [lúc khép, mở), hạn = lúc khép.
            LiveEventCalendarDocument baseline = JsonLiveEventCalendarParser.ParseDocument(LiveOpsDesignSample.PublishedSnapshotJson).Document;
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(
                new LiveEventCalendarCheckContextBuilder(LiveOpsDesignSample.Document, NowUtc).WithPublishedBaseline(baseline).Build());
            LiveEventCalendarFinding weeklyPass = FindingOf(report, LiveEventCalendarRuleIds.RunningEventIdChanged);
            Assert.AreEqual(LiveEventCalendarRepairKind.Decision, weeklyPass.RepairKind);

            IgnoredCalendarWarning reminder = null;
            foreach (LiveEventCalendarEdit edit in ((CompositeCalendarEdit)weeklyPass.Repairs[1].Edit).Edits)
            {
                if (edit is AddIgnoredWarningEdit addWarning) reminder = addWarning.Warning;
            }
            Assert.IsNotNull(reminder);
            Assert.AreEqual(string.Empty, reminder.RangeEndUtcText, "Tiền đề CC-VALB-4: khoảng mở phía sau.");

            Assert.AreEqual("hẹn tới 14/9 00:00", LiveOpsFindingText.PlainText(LiveOpsFindingText.IgnoredWarningRangeText(reminder, Format)));
            Assert.AreEqual("Để sau khi weekly-pass-35 khép (14/9 00:00 UTC)…",
                LiveOpsFindingText.PlainText(LiveOpsFindingText.RepairOptionText(weeklyPass, weeklyPass.Repairs[1], Format)));
            Assert.AreEqual("Giữ tiền tố weekly-pass- (hoàn về)",
                LiveOpsFindingText.PlainText(LiveOpsFindingText.RepairOptionText(weeklyPass, weeklyPass.Repairs[0], Format)), "Chữ menu đọc lệnh THẬT của luật.");

            var ignoredGap = new IgnoredCalendarWarning(LiveEventCalendarRuleIds.LongGapBetweenEvents, "lava-quest", "2026-09-20T00:00:00Z", "2026-10-01T00:00:00Z",
                "lava-quest nghỉ đến mùa tháng 10", string.Empty);
            Assert.AreEqual("20/9 00:00 → 1/10 00:00", LiveOpsFindingText.IgnoredWarningRangeText(ignoredGap, Format));
            Assert.AreEqual("mọi khoảng", LiveOpsFindingText.IgnoredWarningRangeText(
                new IgnoredCalendarWarning(LiveEventCalendarRuleIds.LongGapBetweenEvents, "lava-quest", string.Empty, string.Empty, "ghi chú", string.Empty), Format));
            Assert.AreEqual("đã tới hẹn", LiveOpsFindingText.DueReminderTag);
        }

        [Test]
        public void RunningMovedOut_HeadlineFollowsDirectionOfMove()
        {
            // Luật thật ra moved-out cho mọi khung mới không giao khung đang chạy (12/9 → 15/9) — cả dời về sau lẫn dời về trước.
            FixedLiveEventEntry running = new FixedLiveEventEntry("entry-running", "quest-0912", "quest", "2026-09-12T00:00:00Z", "2026-09-15T00:00:00Z", "quest_v1");
            LiveEventCalendarDocument baseline = new LiveEventCalendarDocumentBuilder().WithFixedEvent(running).Build();

            LiveEventCalendarFinding later = MovedOutFinding(baseline, running.WithTimes("2026-09-16T00:00:00Z", "2026-09-18T00:00:00Z"));
            Assert.AreEqual("quest-0912 dời ra sau 16/9 00:00 khi đang chạy", LiveOpsFindingText.PlainText(LiveOpsFindingText.Headline(later, Format)));

            LiveEventCalendarFinding earlier = MovedOutFinding(baseline, running.WithTimes("2026-09-01T00:00:00Z", "2026-09-05T00:00:00Z"));
            Assert.AreEqual("quest-0912 dời sớm về 1/9 00:00 khi đang chạy", LiveOpsFindingText.PlainText(LiveOpsFindingText.Headline(earlier, Format)),
                "Đợt đang chạy từ 12/9 bị dời về 1/9 không được nói \"dời ra sau 1/9\".");
            StringAssert.Contains("khung mới 1/9 00:00 → 5/9 00:00 UTC", LiveOpsFindingText.PlainText(LiveOpsFindingText.Meta(earlier, Format)));

            // Luật lặp cùng quy tắc: so lúc bắt đầu mới với khung đang chạy.
            LiveEventCalendarFinding recurringEarlier = Builder(LiveEventCalendarRuleIds.RunningEventIdChanged, LiveEventCalendarDetailCodes.RunningMovedOutOfWindow,
                    LiveEventCalendarConsequence.ProgressLost, LiveEventCalendarTargetKind.RecurringRule, "weekly-pass")
                .WithRelatedId("weekly-pass-35").WithTexts("2026-08-31T00:00:00Z · 2026-09-07T00:00:00Z", "2026-09-07T00:00:00Z · 2026-09-14T00:00:00Z")
                .WithRange(Utc(9, 7, 0), Utc(9, 14, 0)).Build();
            Assert.AreEqual("weekly-pass dời weekly-pass-35 sớm về 31/8 00:00 khi đang chạy",
                LiveOpsFindingText.PlainText(LiveOpsFindingText.Headline(recurringEarlier, Format)));
        }

        [Test]
        public void RepairOptionText_CoreRepairIds_NeverFallBackToRawValues()
        {
            // Id cách sửa chép tay ở Editor (lớp luật core là internal): chạy luật 1/2/4/5/6 THẬT và khẳng định mọi lệnh sửa có câu riêng —
            // core đổi id thì RepairOptionText rơi âm thầm về giá trị thô, test này đỏ.
            LiveEventCalendarDocument draft = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "sky-race-", 24, 30, "sky_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-utc", "lava-quest-2026-10", "lava-quest", "2026-10-01T00:00:00Z", "2026-10-3", "lava_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-swap", "lava-quest-2026-11", "lava-quest", "2026-11-10T00:00:00Z", "2026-11-08T00:00:00Z", "lava_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-dup-1", "dup-1", "dup", "2026-10-12T00:00:00Z", "2026-10-13T00:00:00Z", "dup_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-dup-2", "dup-1", "dup", "2026-10-14T00:00:00Z", "2026-10-15T00:00:00Z", "dup_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-hunt", "hunt-0914", "hunt", "2026-09-14T00:00:00Z", "2026-09-17T00:00:00Z", "hunt_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-bonus", "hunt-0916-bonus", "hunt", "2026-09-16T12:00:00Z", "2026-09-18T00:00:00Z", "hunt_v1"))
                .Build();
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(new LiveEventCalendarCheckContextBuilder(draft, NowUtc).Build());

            var editorRepairIds = new HashSet<string>(StringComparer.Ordinal)
            {
                LiveOpsFindingText.NormalizeRepairId, LiveOpsFindingText.KeepStartSetDurationRepairId, LiveOpsFindingText.SwapStartEndRepairId,
                LiveOpsFindingText.RenameRepairId, LiveOpsFindingText.ShiftStartKeepEndRepairId, LiveOpsFindingText.ShiftWholeKeepDurationRepairId,
                LiveOpsFindingText.SetActiveToPeriodRepairId,
            };
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (LiveEventCalendarFinding finding in report.Findings)
            {
                foreach (LiveEventCalendarRepair repair in finding.Repairs)
                {
                    string label = finding.RuleId + " / " + repair.RepairId;
                    Assert.IsTrue(editorRepairIds.Contains(repair.RepairId), label + ": id cách sửa của core không khớp hằng chép ở LiveOpsFindingText");
                    seen.Add(repair.RepairId);
                    string text = LiveOpsFindingText.RepairOptionText(finding, repair, Format);
                    AssertSentence(text, label);
                    Assert.AreNotEqual(RawFallback(repair), text, label + ": câu rơi về giá trị thô");
                }
            }
            CollectionAssert.AreEquivalent(editorRepairIds, seen, "Mẫu phải sinh đủ 7 id cách sửa của luật 1/2/4/5/6.");
        }

        [Test]
        public void RemoteSubjectVariant_SaysRunningJsonAndNeverBlocksCopy()
        {
            // (V-17) Cùng luật 8, nguồn khác: nháp = Bị bỏ chặn Copy; chỉ trong JSON đang chạy = Nên xem, câu nói rõ không chặn nháp.
            LiveEventCalendarFinding draft = Builder(LiveEventCalendarRuleIds.UnknownEventType, LiveEventCalendarDetailCodes.UnknownTypeInDraft,
                LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.EventType, "lucky-spin").WithTexts("2", string.Empty).Build();
            LiveEventCalendarFinding remote = Builder(LiveEventCalendarRuleIds.UnknownEventType, LiveEventCalendarDetailCodes.UnknownTypeInRemote,
                    LiveEventCalendarConsequence.ShouldReview, LiveEventCalendarTargetKind.EventType, "lucky-spin")
                .WithTexts("2", string.Empty).WithRemoteSnapshotSubject(true).Build();

            Assert.AreEqual("2 đợt lucky-spin thuộc loại chưa khai báo", LiveOpsFindingText.PlainText(LiveOpsFindingText.Headline(draft, Format)));
            Assert.AreEqual("2 đợt lucky-spin trong JSON đang chạy thuộc loại chưa khai báo", LiveOpsFindingText.PlainText(LiveOpsFindingText.Headline(remote, Format)));
            Assert.AreEqual("trong JSON đang chạy · không chặn Copy JSON của nháp", LiveOpsFindingText.Meta(remote, Format));
            Assert.AreEqual("lucky-spin: có 2 đợt trong JSON đã dán nhưng chưa có loại — game sẽ bỏ",
                LiveOpsFindingText.PlainText(LiveOpsFindingText.ConsequenceSentence(remote, Format)), "[SD1 §2.2] nguyên văn");
            StringAssert.DoesNotContain("JSON đang chạy", LiveOpsFindingText.Meta(draft, Format));
            Assert.AreEqual("Khai báo", LiveOpsFindingText.PrimaryButtonText(remote));
            Assert.AreEqual(string.Empty, LiveOpsFindingText.LinkText(remote));

            // Luật 12 lệch: câu nêu giờ dấu mới nhất, id khác nhau, và không chặn Copy; nút "Xem diff".
            LiveEventCalendarFinding drift = Builder(LiveEventCalendarRuleIds.RemoteSnapshotDrift, LiveEventCalendarDetailCodes.RemoteDiffers,
                    LiveEventCalendarConsequence.ShouldReview, LiveEventCalendarTargetKind.RemoteSnapshot, string.Empty)
                .WithTexts("hunt-0914" + LiveEventCalendarFindingBuilder.ValueSeparator + "weekly-pass", "2").WithRemoteSnapshotSubject(true).Build();
            Assert.AreEqual("Bản remote khác dấu 11/9 16:20: 2 mục",
                LiveOpsFindingText.PlainText(LiveOpsFindingText.Headline(drift, Format, LiveOpsDesignSample.Document.LatestStamp)));
            Assert.AreEqual("khác ở: hunt-0914, weekly-pass · hub không biết ai đã sửa trên console nên không đoán · không chặn Copy JSON của nháp",
                LiveOpsFindingText.PlainText(LiveOpsFindingText.Meta(drift, Format)));
            Assert.AreEqual("Xem diff", LiveOpsFindingText.PrimaryButtonText(drift));
            Assert.AreEqual("remote-snapshot-drift", LiveOpsFindingText.PlainText(LiveOpsFindingText.RuleIdLine(drift)), "Bản remote không có đích — chỉ id luật.");

            // Luật không tự nói về bản remote mà phát hiện mang cờ: meta vẫn nêu nguồn.
            LiveEventCalendarFinding flaggedGap = Builder(LiveEventCalendarRuleIds.LongGapBetweenEvents, LiveEventCalendarDetailCodes.LongGap,
                    LiveEventCalendarConsequence.ShouldReview, LiveEventCalendarTargetKind.EventType, "lava-quest")
                .WithRange(Utc(9, 20, 0), Utc(10, 1, 0)).WithRemoteSnapshotSubject(true).Build();
            StringAssert.EndsWith("trong JSON đang chạy · không chặn Copy JSON của nháp", LiveOpsFindingText.Meta(flaggedGap, Format));
        }

        // =============================================================================================================== dữ liệu

        private static LiveEventCalendarFindingBuilder Builder(string ruleId, string detailCode, LiveEventCalendarConsequence consequence,
            LiveEventCalendarTargetKind targetKind, string targetId)
        {
            return new LiveEventCalendarFindingBuilder(ruleId, detailCode, consequence, targetKind, targetId);
        }

        private static DateTime Utc(int month, int day, int hour) => new DateTime(2026, month, day, hour, 0, 0, DateTimeKind.Utc);

        private static LiveEventCalendarFinding UtcTimeFinding()
        {
            Assert.IsTrue(LiveOpsDesignSample.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestLateEntryKey, out FixedLiveEventEntry entry));
            var repair = new LiveEventCalendarRepair(LiveOpsFindingText.NormalizeRepairId, LiveEventCalendarRepairKind.SafeRepair,
                new ReplaceFixedEventEdit(entry.WithTimes(entry.StartUtcText, "2026-10-03T00:00:00Z")), "2026-10-3", "2026-10-03T00:00:00Z",
                Utc(10, 1, 0), Utc(10, 3, 0));
            return Builder(LiveEventCalendarRuleIds.UtcTimeFormat, LiveEventCalendarDetailCodes.EndUnreadable, LiveEventCalendarConsequence.Dropped,
                    LiveEventCalendarTargetKind.FixedEvent, "lava-quest-2026-10")
                .WithTargetEntryKey(LiveOpsDesignSample.LavaQuestLateEntryKey)
                .WithTexts("2026-10-3", "2026-10-03T00:00:00Z")
                .WithRange(Utc(10, 1, 0), Utc(10, 3, 0))
                .WithAnchor(Utc(10, 1, 0))
                .WithRepairs(LiveEventCalendarRepairKind.SafeRepair, new[] { repair })
                .Build();
        }

        private static LiveEventCalendarFinding OverlapFinding()
        {
            Assert.IsTrue(LiveOpsDesignSample.Document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey, out FixedLiveEventEntry entry));
            const string before = "2026-09-16T12:00:00Z · 2026-09-18T00:00:00Z";
            var shiftStart = new LiveEventCalendarRepair(LiveOpsFindingText.ShiftStartKeepEndRepairId, LiveEventCalendarRepairKind.Proposal,
                new ReplaceFixedEventEdit(entry.WithTimes("2026-09-17T00:00:00Z", "2026-09-18T00:00:00Z")), before,
                "2026-09-17T00:00:00Z · 2026-09-18T00:00:00Z", Utc(9, 17, 0), Utc(9, 18, 0));
            var shiftWhole = new LiveEventCalendarRepair(LiveOpsFindingText.ShiftWholeKeepDurationRepairId, LiveEventCalendarRepairKind.Proposal,
                new ReplaceFixedEventEdit(entry.WithTimes("2026-09-17T00:00:00Z", "2026-09-18T12:00:00Z")), before,
                "2026-09-17T00:00:00Z · 2026-09-18T12:00:00Z", Utc(9, 17, 0), Utc(9, 18, 12));
            return Builder(LiveEventCalendarRuleIds.OverlapSameType, LiveEventCalendarDetailCodes.OverlapKeptEarlier, LiveEventCalendarConsequence.Dropped,
                    LiveEventCalendarTargetKind.FixedEvent, "hunt-0916-bonus")
                .WithTargetEntryKey(LiveOpsDesignSample.HuntBonusEntryKey)
                .WithRelatedId("hunt-0914")
                .WithTexts(before, "2026-09-17T00:00:00Z")
                .WithRange(Utc(9, 16, 12), Utc(9, 17, 0))
                .WithAnchor(Utc(9, 16, 12))
                .WithRepairs(LiveEventCalendarRepairKind.Proposal, new[] { shiftStart, shiftWhole })
                .Build();
        }

        /// <summary>weekly-pass đổi tiền tố khi weekly-pass-35 (7/9 → 14/9) đang chạy; <paramref name="withDecision"/> false = ca CC-VALB-3.</summary>
        private static LiveEventCalendarFinding RunningPrefixFinding(bool withDecision)
        {
            LiveEventCalendarFindingBuilder builder = Builder(LiveEventCalendarRuleIds.RunningEventIdChanged, LiveEventCalendarDetailCodes.RunningIdChanged,
                    LiveEventCalendarConsequence.ProgressLost, LiveEventCalendarTargetKind.RecurringRule, "weekly-pass")
                .WithTargetEntryKey("weekly-pass")
                .WithRelatedId("weekly-pass-35")
                .WithTexts("pass-35", "weekly-pass-35")
                .WithRange(Utc(9, 7, 0), Utc(9, 14, 0))
                .WithAnchor(Utc(9, 7, 0));
            if (!withDecision) return builder.Build();

            Assert.IsTrue(LiveOpsDesignSample.Document.TryGetRecurringRule("weekly-pass", out RecurringLiveEventRule draftRule));
            LiveEventCalendarEdit revert = new SetRecurringRuleEdit(draftRule.WithIdPrefix("weekly-pass-"));
            var reminder = new IgnoredCalendarWarning(LiveEventCalendarRuleIds.RunningEventIdChanged, "weekly-pass", "2026-09-14T00:00:00Z", string.Empty,
                string.Empty, "2026-09-14T00:00:00Z");
            return builder.WithRepairs(LiveEventCalendarRepairKind.Decision, new[]
            {
                new LiveEventCalendarRepair(LiveOpsFindingText.RevertRepairId, LiveEventCalendarRepairKind.Decision, revert, "pass-35", "weekly-pass-35",
                    Utc(9, 7, 0), Utc(9, 14, 0)),
                new LiveEventCalendarRepair(LiveOpsFindingText.DeferUntilEndRepairId, LiveEventCalendarRepairKind.Decision,
                    new CompositeCalendarEdit(new[] { revert, new AddIgnoredWarningEdit(reminder) }), "pass-35", "weekly-pass-35", Utc(9, 7, 0), Utc(9, 14, 0)),
            }).Build();
        }

        private static LiveEventCalendarFinding ConfigKeyFinding()
        {
            return Builder(LiveEventCalendarRuleIds.ConfigKeyMissing, LiveEventCalendarDetailCodes.ConfigKeyInheritedDiffers, LiveEventCalendarConsequence.ShouldReview,
                    LiveEventCalendarTargetKind.FixedEvent, "hunt-0914")
                .WithTargetEntryKey(LiveOpsDesignSample.HuntEarlyEntryKey)
                .WithRelatedId("treasure-hunt")
                .WithTexts("hunt_default", "hunt_v1")
                .WithRange(Utc(9, 14, 0), Utc(9, 17, 0))
                .WithAnchor(Utc(9, 14, 0))
                .Build();
        }

        private static LiveEventCalendarFinding LongGapFinding()
        {
            return Builder(LiveEventCalendarRuleIds.LongGapBetweenEvents, LiveEventCalendarDetailCodes.LongGap, LiveEventCalendarConsequence.ShouldReview,
                    LiveEventCalendarTargetKind.EventType, "lava-quest")
                .WithTargetEntryKey("lava-quest")
                .WithRelatedId("lava-quest-2026-10")
                .WithTexts("2026-09-20T00:00:00Z · 2026-10-01T00:00:00Z", string.Empty)
                .WithRange(Utc(9, 20, 0), Utc(10, 1, 0))
                .WithAnchor(Utc(9, 20, 0))
                .WithRepairs(LiveEventCalendarRepairKind.Ignorable, null)
                .Build();
        }

        /// <summary>
        /// Một (hoặc hai, khi luật có hai loại đích / hai kiểu sửa) phát hiện cho mỗi cặp, giá trị thô đúng hình dạng luật core ghi.
        /// <paramref name="identifier"/> là id/giá trị thô chèn vào mọi chỗ — test noparse truyền chuỗi có thẻ.
        /// </summary>
        private static IEnumerable<LiveEventCalendarFinding> SampleFindings(string ruleId, string detailCode, string identifier)
        {
            const string separator = LiveEventCalendarFindingBuilder.ValueSeparator;
            DateTime start = Utc(9, 12, 0);
            DateTime end = Utc(9, 15, 0);
            LiveEventCalendarEdit anyEdit = new RemoveFixedEventEdit("entry-" + detailCode);

            switch (ruleId)
            {
                case LiveEventCalendarRuleIds.UtcTimeFormat:
                    yield return Builder(ruleId, detailCode, LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, identifier)
                        .WithTexts(identifier, "2026-10-03T00:00:00Z").WithRange(start, null).WithAnchor(start)
                        .WithRepairs(LiveEventCalendarRepairKind.SafeRepair, new[]
                        {
                            new LiveEventCalendarRepair(LiveOpsFindingText.NormalizeRepairId, LiveEventCalendarRepairKind.SafeRepair, anyEdit, identifier,
                                "2026-10-03T00:00:00Z", start, end),
                        }).Build();
                    if (detailCode == LiveEventCalendarDetailCodes.StartUnreadable)
                    {
                        yield return Builder(ruleId, detailCode, LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, identifier)
                            .WithTexts(identifier + separator + "2026-10-3", "yyyy-MM-ddTHH:mm:ssZ" + separator + "2026-10-03T00:00:00Z").Build();
                    }
                    break;
                case LiveEventCalendarRuleIds.EndBeforeStart:
                    yield return Builder(ruleId, detailCode, LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, identifier)
                        .WithTexts("2026-09-15T00:00:00Z" + separator + "2026-09-12T00:00:00Z", "2026-09-15T00:00:00Z").WithRange(end, start).WithAnchor(end)
                        .WithRepairs(LiveEventCalendarRepairKind.Proposal, new[]
                        {
                            new LiveEventCalendarRepair(LiveOpsFindingText.KeepStartSetDurationRepairId, LiveEventCalendarRepairKind.Proposal, anyEdit, string.Empty,
                                string.Empty, end, end.AddHours(24)),
                            new LiveEventCalendarRepair(LiveOpsFindingText.SwapStartEndRepairId, LiveEventCalendarRepairKind.Proposal, anyEdit, string.Empty,
                                string.Empty, start, end),
                        }).Build();
                    break;
                case LiveEventCalendarRuleIds.InvalidIdentifier:
                    yield return Builder(ruleId, detailCode, LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, identifier)
                        .WithTexts(identifier, "id").WithRange(start, end).WithAnchor(start).Build();
                    yield return Builder(ruleId, detailCode, LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, identifier)
                        .WithTexts(identifier, "type").WithRange(start, end).WithAnchor(start).Build();
                    break;
                case LiveEventCalendarRuleIds.DuplicateEventId:
                    yield return Builder(ruleId, detailCode, LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, identifier)
                        .WithRelatedId(identifier).WithTexts(identifier, identifier + "-2").WithRange(start, end).WithAnchor(start)
                        .WithRepairs(LiveEventCalendarRepairKind.Proposal, new[]
                        {
                            new LiveEventCalendarRepair(LiveOpsFindingText.RenameRepairId, LiveEventCalendarRepairKind.Proposal, anyEdit, identifier,
                                identifier + "-2", start, end),
                        }).Build();
                    break;
                case LiveEventCalendarRuleIds.OverlapSameType:
                    yield return Builder(ruleId, detailCode, LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, identifier)
                        .WithRelatedId(identifier).WithTexts(identifier, identifier).WithRange(start, end).WithAnchor(start)
                        .WithRepairs(LiveEventCalendarRepairKind.Proposal, new[]
                        {
                            new LiveEventCalendarRepair(LiveOpsFindingText.ShiftStartKeepEndRepairId, LiveEventCalendarRepairKind.Proposal, anyEdit, identifier,
                                identifier, end, end.AddHours(24)),
                            new LiveEventCalendarRepair(LiveOpsFindingText.ShiftWholeKeepDurationRepairId, LiveEventCalendarRepairKind.Proposal, anyEdit, identifier,
                                identifier, end, end.AddHours(72)),
                        }).Build();
                    break;
                case LiveEventCalendarRuleIds.RecurringRuleInvalid:
                {
                    LiveEventCalendarFindingBuilder builder = Builder(ruleId, detailCode, LiveEventCalendarConsequence.Dropped,
                        LiveEventCalendarTargetKind.RecurringRule, identifier).WithTexts(identifier, identifier).WithAnchor(start);
                    if (detailCode == LiveEventCalendarDetailCodes.ActiveLongerThanPeriod)
                    {
                        builder.WithTexts("200", "168").WithRepairs(LiveEventCalendarRepairKind.Proposal, new[]
                        {
                            new LiveEventCalendarRepair(LiveOpsFindingText.SetActiveToPeriodRepairId, LiveEventCalendarRepairKind.Proposal, anyEdit, "200", "168", null, null),
                        });
                    }
                    yield return builder.Build();
                    break;
                }
                case LiveEventCalendarRuleIds.ShadowedByRecurring:
                    yield return Builder(ruleId, detailCode, LiveEventCalendarConsequence.Dropped, LiveEventCalendarTargetKind.FixedEvent, identifier)
                        .WithRelatedId(identifier).WithTexts(identifier, identifier).WithRange(start, end).WithAnchor(start).Build();
                    break;
                case LiveEventCalendarRuleIds.UnknownEventType:
                {
                    bool remote = detailCode == LiveEventCalendarDetailCodes.UnknownTypeInRemote;
                    yield return Builder(ruleId, detailCode, remote ? LiveEventCalendarConsequence.ShouldReview : LiveEventCalendarConsequence.Dropped,
                            LiveEventCalendarTargetKind.EventType, identifier)
                        .WithRelatedId(identifier).WithTexts("2", string.Empty).WithRange(start, end).WithRemoteSnapshotSubject(remote).Build();
                    break;
                }
                case LiveEventCalendarRuleIds.RunningEventIdChanged:
                {
                    string found;
                    string expected = identifier;
                    switch (detailCode)
                    {
                        case LiveEventCalendarDetailCodes.RunningIdChanged: found = identifier + "-new"; break;
                        case LiveEventCalendarDetailCodes.RunningRemoved: found = string.Empty; break;
                        case LiveEventCalendarDetailCodes.RunningRetyped: found = identifier + "-type"; break;
                        case LiveEventCalendarDetailCodes.RunningMovedOutOfWindow: found = "2026-09-16T00:00:00Z" + separator + "2026-09-18T00:00:00Z"; break;
                        default: found = "2026-09-12T00:00:00Z" + separator + "2026-09-13T08:00:00Z"; break;
                    }
                    foreach (LiveEventCalendarTargetKind targetKind in new[] { LiveEventCalendarTargetKind.FixedEvent, LiveEventCalendarTargetKind.RecurringRule })
                    {
                        LiveEventCalendarFindingBuilder builder = Builder(ruleId, detailCode, LiveEventCalendarConsequence.ProgressLost, targetKind, identifier)
                            .WithRelatedId(identifier).WithTexts(found, expected).WithRange(start, end).WithAnchor(start);
                        yield return builder.Build();
                        yield return builder.WithRepairs(LiveEventCalendarRepairKind.Decision, new[]
                        {
                            new LiveEventCalendarRepair(LiveOpsFindingText.RevertRepairId, LiveEventCalendarRepairKind.Decision, anyEdit, found, expected, start, end),
                            new LiveEventCalendarRepair(LiveOpsFindingText.DeferUntilEndRepairId, LiveEventCalendarRepairKind.Decision, anyEdit, found, expected, start, end),
                        }).Build();
                    }
                    break;
                }
                case LiveEventCalendarRuleIds.ConfigKeyMissing:
                {
                    string found = detailCode == LiveEventCalendarDetailCodes.ConfigKeyEmpty ? string.Empty : identifier;
                    string expected = detailCode == LiveEventCalendarDetailCodes.ConfigKeyInheritedDiffers ? identifier + "_v1" : string.Empty;
                    yield return Builder(ruleId, detailCode, LiveEventCalendarConsequence.ShouldReview, LiveEventCalendarTargetKind.FixedEvent, identifier)
                        .WithRelatedId(identifier).WithTexts(found, expected).WithRange(Utc(9, 14, 0), Utc(9, 17, 0)).WithAnchor(Utc(9, 14, 0)).Build();
                    yield return Builder(ruleId, detailCode, LiveEventCalendarConsequence.ShouldReview, LiveEventCalendarTargetKind.RecurringRule, identifier)
                        .WithRelatedId(identifier).WithTexts(found, expected).WithAnchor(start).Build();
                    break;
                }
                case LiveEventCalendarRuleIds.LongGapBetweenEvents:
                    yield return Builder(ruleId, detailCode, LiveEventCalendarConsequence.ShouldReview, LiveEventCalendarTargetKind.EventType, identifier)
                        .WithRelatedId(identifier).WithTexts("2026-09-20T00:00:00Z" + separator + "2026-10-01T00:00:00Z", string.Empty)
                        .WithRange(Utc(9, 20, 0), Utc(10, 1, 0)).WithRepairs(LiveEventCalendarRepairKind.Ignorable, null).Build();
                    break;
                case LiveEventCalendarRuleIds.RemoteSnapshotDrift:
                {
                    bool differs = detailCode == LiveEventCalendarDetailCodes.RemoteDiffers;
                    yield return Builder(ruleId, detailCode, LiveEventCalendarConsequence.ShouldReview, LiveEventCalendarTargetKind.RemoteSnapshot, string.Empty)
                        .WithTexts(differs ? identifier + separator + "weekly-pass" : "8", differs ? "2" : string.Empty).WithRemoteSnapshotSubject(true).Build();
                    break;
                }
                default:
                    Assert.Fail("Luật mới chưa có dữ liệu mẫu cho test câu: " + ruleId);
                    break;
            }
        }

        private static void AssertRow(LiveEventCalendarFinding finding, string headline, string meta, string ruleIdLine, string primaryButton, string link,
            string primaryTooltip)
        {
            Assert.AreEqual(headline, LiveOpsFindingText.PlainText(LiveOpsFindingText.Headline(finding, Format)), finding.RuleId + " headline");
            Assert.AreEqual(meta, LiveOpsFindingText.PlainText(LiveOpsFindingText.Meta(finding, Format, NowUtc)), finding.RuleId + " meta");
            Assert.AreEqual(ruleIdLine, LiveOpsFindingText.PlainText(LiveOpsFindingText.RuleIdLine(finding)), finding.RuleId + " id luật");
            Assert.AreEqual(primaryButton, LiveOpsFindingText.PrimaryButtonText(finding), finding.RuleId + " nút chính");
            Assert.AreEqual(link, LiveOpsFindingText.LinkText(finding), finding.RuleId + " link");
            Assert.AreEqual(primaryTooltip, LiveOpsFindingText.PlainText(LiveOpsFindingText.PrimaryButtonTooltip(finding, Format, DesignCalendarAssetName)),
                finding.RuleId + " tooltip nút");
            AssertSentence(LiveOpsFindingText.ConsequenceSentence(finding, Format), finding.RuleId + " hậu quả");
        }

        private static void AssertSentence(string text, string label)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(text), label + " rỗng");
            string plain = LiveOpsFindingText.PlainText(text);
            StringAssert.DoesNotContain("{", plain, label + ": chỗ trống format chưa điền — " + plain);
            StringAssert.DoesNotContain("  ", plain, label + ": hai dấu cách (mảnh rỗng) — " + plain);
            Assert.IsFalse(plain.StartsWith(" ", StringComparison.Ordinal) || plain.EndsWith(" ", StringComparison.Ordinal), label + ": khoảng trắng đầu/cuối — " + plain);
            Assert.IsFalse(plain.StartsWith("·", StringComparison.Ordinal) || plain.EndsWith("·", StringComparison.Ordinal), label + ": dấu · thừa — " + plain);
            StringAssert.DoesNotContain("· ·", plain, label + ": mảnh rỗng giữa hai dấu · — " + plain);
        }

        private static void AssertNoTagOutsideNoParse(string text, string label)
        {
            string outside = NoParseSegment.Replace(text ?? string.Empty, string.Empty);
            StringAssert.DoesNotContain("<", outside, label + ": giá trị thô lọt ra ngoài noparse — " + text);
        }

        private static LiveEventCalendarFinding FindingOf(LiveEventCalendarCheckReport report, string ruleId)
        {
            foreach (LiveEventCalendarFinding finding in report.Findings)
            {
                if (finding.RuleId == ruleId) return finding;
            }
            Assert.Fail("Không có phát hiện " + ruleId);
            return null;
        }

        private static LiveEventCalendarFinding MovedOutFinding(LiveEventCalendarDocument baseline, FixedLiveEventEntry movedEntry)
        {
            LiveEventCalendarDocument draft = new LiveEventCalendarDocumentBuilder().WithFixedEvent(movedEntry).Build();
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(
                new LiveEventCalendarCheckContextBuilder(draft, NowUtc).WithPublishedBaseline(baseline).Build());
            LiveEventCalendarFinding finding = FindingOf(report, LiveEventCalendarRuleIds.RunningEventIdChanged);
            Assert.AreEqual(LiveEventCalendarDetailCodes.RunningMovedOutOfWindow, finding.DetailCode, "Tiền đề: luật thật ra moved-out.");
            return finding;
        }

        /// <summary>Câu dự phòng của <c>RepairOptionText</c> khi không nhận ra id: giá trị sau sửa (hoặc id) bọc noparse, nối bằng " và ".</summary>
        private static string RawFallback(LiveEventCalendarRepair repair)
        {
            string joined = repair.AfterText.Length > 0 ? repair.AfterText : repair.RepairId;
            string[] values = joined.Split(new[] { LiveEventCalendarFindingBuilder.ValueSeparator }, StringSplitOptions.None);
            for (int index = 0; index < values.Length; index++) values[index] = LiveOpsFindingText.NoParse(values[index]);
            return string.Join(" và ", values);
        }

        private static LiveEventCalendarRuleResult ResultOf(LiveEventCalendarCheckReport report, string ruleId)
        {
            foreach (LiveEventCalendarRuleResult result in report.RuleResults)
            {
                if (result.RuleId == ruleId) return result;
            }
            Assert.Fail("Không có kết quả luật " + ruleId);
            return null;
        }
    }
}
