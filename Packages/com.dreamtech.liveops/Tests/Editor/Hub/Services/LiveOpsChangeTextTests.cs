using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// (V-8) Câu cho hàng diff. Hàng diff có ctor internal (V-20) nên mọi ca dựng HAI tài liệu rồi gọi <see cref="LiveEventCalendarDiff"/>
    /// thật — câu luôn được kiểm trên đúng tổ hợp field/hậu quả thuật toán sinh ra. Lịch nhỏ quanh BÂY GIỜ của mẫu (13/9/2026 08:47 UTC):
    /// đợt đang chạy 12/9 → 15/9, đợt chưa bắt đầu 20/9 → 22/9, đợt đã khép 1/9 → 5/9, luật tuần có weekly-pass-35 (7/9 → 14/9).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class LiveOpsChangeTextTests
    {
        private static readonly DateTime NowUtc = LiveOpsDesignSample.NowUtc;
        private static readonly Regex NoParseSegment = new Regex("<noparse>.*?</noparse>", RegexOptions.Singleline);

        private const string QuestType = "quest";
        private const string OtherType = "other-quest";
        private const string RunningId = "quest-0912";
        private const string UpcomingId = "quest-0920";
        private const string EndedId = "quest-0901";
        private const string RunningStart = "2026-09-12T00:00:00Z";
        private const string RunningEnd = "2026-09-15T00:00:00Z";
        private const string UpcomingStart = "2026-09-20T00:00:00Z";
        private const string UpcomingEnd = "2026-09-22T00:00:00Z";
        private const string EndedStart = "2026-09-01T00:00:00Z";
        private const string EndedEnd = "2026-09-05T00:00:00Z";
        private const string WeeklyAnchor = "2026-01-05T00:00:00Z";

        private static LiveOpsHubFormat Format => new LiveOpsHubFormat(LiveOpsDesignSample.DeviceOffset);

        [Test]
        public void EveryChangeCase_HasSentence()
        {
            var consequencesSeen = new HashSet<LiveEventCalendarConsequence>();
            int changeCount = 0;
            foreach (DiffCase diffCase in AllCases())
            {
                LiveEventCalendarDiffResult result = diffCase.ByEntryKey
                    ? LiveEventCalendarDiff.CompareByEntryKey(diffCase.Baseline, diffCase.Draft, NowUtc)
                    : LiveEventCalendarDiff.Compare(diffCase.Baseline, diffCase.Draft, NowUtc);
                var context = new LiveOpsChangeTextContext(diffCase.Baseline, diffCase.Draft, NowUtc, result, diffCase.ByEntryKey);
                Assert.IsNotEmpty(result.Changes, diffCase.Name + ": ca phải sinh ít nhất một hàng");

                bool expectedSeen = false;
                foreach (LiveEventCalendarChange change in result.Changes)
                {
                    changeCount++;
                    string label = diffCase.Name + " · " + change;
                    consequencesSeen.Add(change.Consequence);
                    if (change.Consequence == diffCase.ExpectedConsequence) expectedSeen = true;

                    AssertSentence(LiveOpsChangeText.RowText(change, Format), label + " RowText");
                    AssertSentence(LiveOpsChangeText.RowText(change, context, Format), label + " RowText(ngữ cảnh)");
                    AssertSentence(LiveOpsChangeText.ConsequenceSentence(change, Format), label + " ConsequenceSentence");
                    AssertSentence(LiveOpsChangeText.ConsequenceSentence(change, context, Format), label + " ConsequenceSentence(ngữ cảnh)");
                    AssertSentence(LiveOpsChangeText.ChangedTooltip(change, Format), label + " ChangedTooltip");
                    AssertSentence(LiveOpsChangeText.ChangedTooltip(change, context, Format), label + " ChangedTooltip(ngữ cảnh)");
                    StringAssert.StartsWith("Khác bản đã đăng: ", LiveOpsFindingText.PlainText(LiveOpsChangeText.ChangedTooltip(change, Format)), label);

                    if (change.Consequence == LiveEventCalendarConsequence.ProgressLost)
                    {
                        // Hàng bắt buộc xem phải nêu đúng id đợt người chơi đang chơi dở và giờ khép của nó.
                        StringAssert.Contains(change.RunningEventIdBefore, Plain(LiveOpsChangeText.ConsequenceSentence(change, Format)), label);
                        StringAssert.Contains("đang chạy", Plain(LiveOpsChangeText.ConsequenceSentence(change, Format)), label);
                    }
                }
                Assert.IsTrue(expectedSeen, diffCase.Name + ": không có hàng " + diffCase.ExpectedConsequence + " — ca không phủ đúng hàng bảng 6.3");
            }
            CollectionAssert.AreEquivalent(new[]
            {
                LiveEventCalendarConsequence.Dropped, LiveEventCalendarConsequence.ProgressLost, LiveEventCalendarConsequence.ShouldReview,
                LiveEventCalendarConsequence.Safe,
            }, consequencesSeen);
            Assert.Greater(changeCount, 30);
        }

        [Test]
        public void EveryChangeCase_EmptyFieldsAndEndedRename_NameTheReason()
        {
            // (V-20 CC-DIFF-2) Đợt đang chạy không đổi field nào nhưng đợt mới chen lên làm nó bị bỏ vì chồng giờ.
            LiveEventCalendarDocument baseline = Document(Running());
            LiveEventCalendarDocument draft = Document(Running(), Entry("entry-earlier", "quest-0911", QuestType, "2026-09-11T00:00:00Z", "2026-09-14T00:00:00Z"));
            LiveEventCalendarDiffResult result = LiveEventCalendarDiff.Compare(baseline, draft, NowUtc);
            LiveEventCalendarChange droppedByOther = FindChange(result, RunningId);
            Assert.AreEqual(0, droppedByOther.Fields.Count, "Tiền đề CC-DIFF-2");
            Assert.IsTrue(droppedByOther.WillBeDropped);
            var context = new LiveOpsChangeTextContext(baseline, draft, NowUtc, result, false);

            Assert.AreEqual("quest-0912 · bị bỏ khi game đọc lịch (do mục khác)", Plain(LiveOpsChangeText.RowText(droppedByOther, Format)));
            Assert.AreEqual("game bỏ mục này khi đọc lịch vì mục khác — người chơi không thấy nó",
                Plain(LiveOpsChangeText.ConsequenceSentence(droppedByOther, Format)));
            Assert.AreEqual("chồng 2 ngày với quest-0911 (do mục khác)", Plain(LiveOpsChangeText.ConsequenceSentence(droppedByOther, context, Format)));
            Assert.AreEqual("Khác bản đã đăng: bị bỏ khi game đọc lịch (do mục khác)", Plain(LiveOpsChangeText.ChangedTooltip(droppedByOther, Format)));

            // Ngược lại: gỡ mục chắn thì đợt quay lại lịch — hàng Changed rỗng field, không bị bỏ.
            LiveEventCalendarDiffResult returned = LiveEventCalendarDiff.CompareByEntryKey(draft, baseline, NowUtc);
            LiveEventCalendarChange back = FindChange(returned, RunningId);
            Assert.AreEqual(0, back.Fields.Count);
            Assert.IsFalse(back.WillBeDropped);
            Assert.AreEqual("quest-0912 · quay lại lịch khi game đọc (do mục khác)", Plain(LiveOpsChangeText.RowText(back, Format)));

            // (V-20 CC-DIFF-3) Đổi id đợt đã khép ở diff với bản đã đăng = xoá + thêm; cả hai nửa nêu id cũ lẫn id mới.
            LiveEventCalendarDocument endedBaseline = Document(Ended());
            LiveEventCalendarDocument renamedDraft = Document(Ended().WithEventId("quest-0901-renamed"));
            LiveEventCalendarDiffResult renamed = LiveEventCalendarDiff.Compare(endedBaseline, renamedDraft, NowUtc);
            var renameContext = new LiveOpsChangeTextContext(endedBaseline, renamedDraft, NowUtc, renamed, false);
            Assert.AreEqual(2, renamed.Changes.Count);
            foreach (LiveEventCalendarChange half in renamed.Changes)
            {
                Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, half.Consequence, "Tiền đề CC-DIFF-3: " + half);
                Assert.AreEqual("quest-0901 → quest-0901-renamed (đổi id đợt đã khép)", Plain(LiveOpsChangeText.RowText(half, renameContext, Format)), half.ToString());
                string consequence = Plain(LiveOpsChangeText.ConsequenceSentence(half, renameContext, Format));
                StringAssert.Contains("quest-0901 → quest-0901-renamed", consequence, half.ToString());
                StringAssert.Contains("id quest-0901", consequence);
                // Thanh --changed của cùng mục nói cùng một việc với hàng diff, không "thêm mới" ở nửa này và "đã xoá" ở nửa kia.
                Assert.AreEqual("Khác bản đã đăng: đổi id đợt đã khép quest-0901 → quest-0901-renamed",
                    Plain(LiveOpsChangeText.ChangedTooltip(half, renameContext, Format)), half.ToString());
            }

            // Cùng thao tác ở diff "chưa lưu" (một hàng Changed): cùng câu hậu quả — đợt đã khép không ai "thấy" id mới.
            LiveEventCalendarDiffResult renamedUnsaved = LiveEventCalendarDiff.CompareByEntryKey(endedBaseline, renamedDraft, NowUtc);
            var renamedUnsavedContext = new LiveOpsChangeTextContext(endedBaseline, renamedDraft, NowUtc, renamedUnsaved, true);
            LiveEventCalendarChange renamedRow = FindChange(renamedUnsaved, "quest-0901-renamed");
            Assert.AreEqual("đợt đã khép đổi id quest-0901 → quest-0901-renamed; bản ghi người đã chơi vẫn mang id quest-0901",
                Plain(LiveOpsChangeText.ConsequenceSentence(renamedRow, renamedUnsavedContext, Format)));

            // Xoá đợt đã khép + thêm đợt KHÁC khung: không phải đổi id — không được ghép thành "đổi id".
            LiveEventCalendarDocument otherWindow = Document(Entry("entry-other", "quest-0902", QuestType, "2026-09-02T00:00:00Z", "2026-09-06T00:00:00Z"));
            LiveEventCalendarDiffResult notRename = LiveEventCalendarDiff.Compare(endedBaseline, otherWindow, NowUtc);
            var notRenameContext = new LiveOpsChangeTextContext(endedBaseline, otherWindow, NowUtc, notRename, false);
            foreach (LiveEventCalendarChange change in notRename.Changes)
            {
                StringAssert.DoesNotContain("đổi id", Plain(LiveOpsChangeText.RowText(change, notRenameContext, Format)), change.ToString());
                Assert.AreEqual(change.Kind == LiveEventCalendarChangeKind.Removed ? "Khác bản đã đăng: đã xoá" : "Khác bản đã đăng: thêm mới",
                    Plain(LiveOpsChangeText.ChangedTooltip(change, notRenameContext, Format)), change.ToString());
                Assert.AreEqual(change.Kind == LiveEventCalendarChangeKind.Removed ? "đợt đã khép, không ai mất gì" : "mục mới, không ai mất gì",
                    Plain(LiveOpsChangeText.ConsequenceSentence(change, notRenameContext, Format)), change.ToString());
            }
        }

        [Test]
        public void ShouldReviewConsequence_CombinedFields_FollowDirectionOfChange()
        {
            // Diff xếp Nên xem vì configKey dù giờ đổi theo hướng an toàn: câu không được nói điều giờ không làm.
            AssertShouldReview("đang chạy kéo dài + configKey", Document(Running()),
                Document(Running().WithTimes(RunningStart, "2026-09-16T00:00:00Z").WithConfigKey("quest_v2")), false,
                "quest-0912 đang chạy tới 15/9 00:00 UTC; người chơi đang chơi chuyển sang cấu hình quest_v2",
                "quest-0912 đang chạy tới 15/9 00:00 UTC; người chơi đang chơi chuyển sang cấu hình quest_v2");
            AssertShouldReview("đang chạy dời bắt đầu về trước + configKey", Document(Running()),
                Document(Running().WithTimes("2026-09-11T00:00:00Z", RunningEnd).WithConfigKey("quest_v2")), false,
                "quest-0912 đang chạy tới 15/9 00:00 UTC; người chơi đang chơi chuyển sang cấu hình quest_v2",
                "quest-0912 đang chạy tới 15/9 00:00 UTC; người chơi đang chơi chuyển sang cấu hình quest_v2");
            // Bắt đầu dời về sau nhưng vẫn trước BÂY GIỜ: không tạm dừng. Không đồng hồ thì im lặng vì configKey đã giải thích hàng.
            AssertShouldReview("đang chạy dời bắt đầu về sau (trước now) + configKey", Document(Running()),
                Document(Running().WithTimes("2026-09-13T00:00:00Z", RunningEnd).WithConfigKey("quest_v2")), true,
                "quest-0912 đang chạy tới 15/9 00:00 UTC; người chơi đang chơi chuyển sang cấu hình quest_v2",
                "quest-0912 đang chạy tới 15/9 00:00 UTC; người chơi đang chơi chuyển sang cấu hình quest_v2");
            // Đúng chiều thì vẫn nói.
            AssertShouldReview("đang chạy rút ngắn + configKey", Document(Running()),
                Document(Running().WithTimes(RunningStart, "2026-09-14T00:00:00Z").WithConfigKey("quest_v2")), false,
                "quest-0912 đang chạy tới 15/9 00:00 UTC; người chơi còn ít thời gian hơn; người chơi đang chơi chuyển sang cấu hình quest_v2",
                "quest-0912 đang chạy tới 15/9 00:00 UTC; người chơi còn ít thời gian hơn; người chơi đang chơi chuyển sang cấu hình quest_v2");
            AssertShouldReview("đang chạy dời bắt đầu ra sau now", Document(Running()),
                Document(Running().WithTimes("2026-09-13T12:00:00Z", RunningEnd)), true,
                "quest-0912 đang chạy tới 15/9 00:00 UTC; người chơi tạm dừng cộng điểm tới lúc đợt mở lại",
                "quest-0912 đang chạy tới 15/9 00:00 UTC; người chơi tạm dừng cộng điểm tới lúc đợt mở lại");

            // Đợt chưa bắt đầu đổi giờ + configKey: không phải "mở lại id đợt đã khép".
            foreach (bool byEntryKey in new[] { false, true })
            {
                AssertShouldReview("chưa bắt đầu đổi giờ + configKey", Document(Upcoming()),
                    Document(Upcoming().WithTimes("2026-09-21T00:00:00Z", "2026-09-23T00:00:00Z").WithConfigKey("quest_v2")), byEntryKey,
                    "đợt chưa bắt đầu; xuất ghi configKey quest_v2",
                    "chưa ai đang chơi đợt này; xuất ghi configKey quest_v2");
            }
            // Đợt đã khép mở lại cùng id cho tương lai: đúng ca của mệnh đề "mở lại id".
            AssertShouldReview("đã khép mở lại cùng id", Document(Ended()), Document(Ended().WithTimes(UpcomingStart, UpcomingEnd)), true,
                "đợt đã khép; mở lại id này cho tương lai — người đã chơi đợt cũ không vào lại được",
                "chưa ai đang chơi đợt này; mở lại id này cho tương lai — người đã chơi đợt cũ không vào lại được");
        }

        [Test]
        public void DroppedChangedRow_WithContext_NamesDropReason()
        {
            // Đổi configKey của đợt chưa bắt đầu, cùng lúc thêm một đợt sớm hơn chồng giờ: mục bị bỏ vì chồng giờ, không vì configKey.
            LiveEventCalendarDocument baseline = Document(Upcoming());
            LiveEventCalendarDocument draft = Document(Entry("entry-earlier", "quest-0919", QuestType, "2026-09-19T00:00:00Z", "2026-09-21T00:00:00Z"),
                Upcoming().WithConfigKey("quest_v2"));
            LiveEventCalendarDiffResult result = LiveEventCalendarDiff.Compare(baseline, draft, NowUtc);
            var context = new LiveOpsChangeTextContext(baseline, draft, NowUtc, result, false);
            LiveEventCalendarChange dropped = FindChange(result, UpcomingId);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, dropped.Consequence, "Tiền đề");
            Assert.AreEqual("quest_v1 → quest_v2; chồng 1 ngày với quest-0919", Plain(LiveOpsChangeText.ConsequenceSentence(dropped, context, Format)));
            Assert.AreEqual("quest_v1 → quest_v2", Plain(LiveOpsChangeText.ConsequenceSentence(dropped, Format)), "Không ngữ cảnh: bớt chữ, không đoán lý do.");

            // Chính giờ đổi làm mục chồng giờ với đợt khác: giá trị đọc được, lý do vẫn phải nêu.
            LiveEventCalendarDocument shiftedBaseline = Document(Upcoming(), Entry("entry-next", "quest-0923", QuestType, "2026-09-23T00:00:00Z", "2026-09-25T00:00:00Z"));
            LiveEventCalendarDocument shiftedDraft = Document(Upcoming(), Entry("entry-next", "quest-0923", QuestType, "2026-09-21T12:00:00Z", "2026-09-25T00:00:00Z"));
            LiveEventCalendarDiffResult shifted = LiveEventCalendarDiff.CompareByEntryKey(shiftedBaseline, shiftedDraft, NowUtc);
            var shiftedContext = new LiveOpsChangeTextContext(shiftedBaseline, shiftedDraft, NowUtc, shifted, true);
            Assert.AreEqual("2026-09-23T00:00:00Z → 2026-09-21T12:00:00Z; chồng 12 giờ với quest-0920",
                Plain(LiveOpsChangeText.ConsequenceSentence(FindChange(shifted, "quest-0923"), shiftedContext, Format)));

            // Giá trị hỏng tự nói lý do: giữ đúng mẫu [SD2 §3.8], không nối thêm "giờ kết thúc sai định dạng".
            LiveEventCalendarDocument brokenDraft = Document(Upcoming().WithTimes(UpcomingStart, "2026-09-2"));
            LiveEventCalendarDiffResult broken = LiveEventCalendarDiff.Compare(baseline, brokenDraft, NowUtc);
            var brokenContext = new LiveOpsChangeTextContext(baseline, brokenDraft, NowUtc, broken, false);
            Assert.AreEqual("2026-09-22T00:00:00Z → \"2026-09-2\"", Plain(LiveOpsChangeText.ConsequenceSentence(FindChange(broken, UpcomingId), brokenContext, Format)));
        }

        [Test]
        public void ChangedTooltip_DesignSample09b()
        {
            LiveEventCalendarDiffResult result = LiveEventCalendarDiff.Compare(PublishedBaseline(), LiveOpsDesignSample.Document, NowUtc);
            LiveEventCalendarChange lateLava = FindChange(result, "lava-quest-2026-09b");

            // [SD1 §3.4] vuông 5×5 của thanh --changed.
            Assert.AreEqual("Khác bản đã đăng: kết thúc 19/9 → 20/9", Plain(LiveOpsChangeText.ChangedTooltip(lateLava, Format)));
        }

        [Test]
        public void DesignSampleRows_MatchDiffCard()
        {
            // [SD2 §3.8] năm hàng card diff của mẫu, nguyên văn (dòng 1 · dòng 2).
            LiveEventCalendarDocument baseline = PublishedBaseline();
            LiveEventCalendarDocument draft = LiveOpsDesignSample.Document;
            LiveEventCalendarDiffResult result = LiveEventCalendarDiff.Compare(baseline, draft, NowUtc);
            var context = new LiveOpsChangeTextContext(baseline, draft, NowUtc, result, false);

            AssertRow(context, FindChange(result, "hunt-0916-bonus"), "hunt-0916-bonus (thêm mới)", "chồng 12 giờ với hunt-0914");
            AssertRow(context, FindChange(result, "lava-quest-2026-10"), "lava-quest-2026-10 endUtc", "2026-10-03T00:00:00Z → \"2026-10-3\"");
            AssertRow(context, FindChange(result, "weekly-pass"), "weekly-pass: tiền tố weekly-pass- → pass-",
                "weekly-pass-35 đang chạy tới 14/9 00:00 UTC; người chơi có điểm bắt đầu lại từ 0.");
            AssertRow(context, FindChange(result, "hunt-0914"), "hunt-0914 configKey hunt_v1 → hunt_default",
                "đợt chưa bắt đầu; đợt không tự khai nên xuất ghi mặc định của loại");
            // (V-22 CC-FT-2 (a)) Chữ chuẩn "19/9 → 20/9" không giờ (nửa đêm), cùng quy tắc tooltip [SD1 §3.4] — không "20/9 00:00" của hàng mẫu.
            AssertRow(context, FindChange(result, "lava-quest-2026-09b"), "lava-quest-2026-09b kết thúc 19/9 → 20/9", "đợt chưa bắt đầu, không ai mất gì");

            // Không ngữ cảnh vẫn đúng (bớt chữ, không nói sai).
            Assert.AreEqual("weekly-pass-35 đang chạy tới 14/9 00:00 UTC; người chơi có điểm bắt đầu lại từ 0.",
                Plain(LiveOpsChangeText.ConsequenceSentence(FindChange(result, "weekly-pass"), Format)));
            Assert.AreEqual("không ai mất gì", Plain(LiveOpsChangeText.ConsequenceSentence(FindChange(result, "lava-quest-2026-09b"), Format)));
        }

        [Test]
        public void ChangeTexts_UseNoParseForRawValues()
        {
            LiveEventCalendarDocument baseline = Document(Running(), Upcoming());
            LiveEventCalendarDocument draft = Document(Running().WithConfigKey("<b>v2</b>"), Upcoming().WithEventId("<i>quest</i>"),
                Entry("entry-hostile", "<color=red>x</color>", QuestType, "2026-09-25T00:00:00Z", "2026-9-27"));
            LiveEventCalendarDiffResult result = LiveEventCalendarDiff.CompareByEntryKey(baseline, draft, NowUtc);
            var context = new LiveOpsChangeTextContext(baseline, draft, NowUtc, result, true);
            Assert.GreaterOrEqual(result.Changes.Count, 3);
            foreach (LiveEventCalendarChange change in result.Changes)
            {
                foreach (string text in new[]
                         {
                             LiveOpsChangeText.RowText(change, Format), LiveOpsChangeText.RowText(change, context, Format),
                             LiveOpsChangeText.ConsequenceSentence(change, Format), LiveOpsChangeText.ConsequenceSentence(change, context, Format),
                             LiveOpsChangeText.ChangedTooltip(change, Format), LiveOpsChangeText.ChangedTooltip(change, context, Format),
                         })
                {
                    StringAssert.DoesNotContain("<", NoParseSegment.Replace(text, string.Empty), change + ": giá trị thô lọt ngoài noparse — " + text);
                }
            }
            StringAssert.Contains("<i>quest</i>", Plain(LiveOpsChangeText.RowText(FindChange(result, "<i>quest</i>"), Format)));
        }

        // =============================================================================================================== ca bảng 6.3

        private sealed class DiffCase
        {
            public DiffCase(string name, LiveEventCalendarDocument baseline, LiveEventCalendarDocument draft, bool byEntryKey, LiveEventCalendarConsequence expected)
            {
                Name = name;
                Baseline = baseline;
                Draft = draft;
                ByEntryKey = byEntryKey;
                ExpectedConsequence = expected;
            }

            public string Name { get; }
            public LiveEventCalendarDocument Baseline { get; }
            public LiveEventCalendarDocument Draft { get; }
            public bool ByEntryKey { get; }
            public LiveEventCalendarConsequence ExpectedConsequence { get; }
        }

        private static IEnumerable<DiffCase> AllCases()
        {
            RecurringLiveEventRule weekly = WeeklyRule();
            const LiveEventCalendarConsequence dropped = LiveEventCalendarConsequence.Dropped;
            const LiveEventCalendarConsequence progressLost = LiveEventCalendarConsequence.ProgressLost;
            const LiveEventCalendarConsequence shouldReview = LiveEventCalendarConsequence.ShouldReview;
            const LiveEventCalendarConsequence safe = LiveEventCalendarConsequence.Safe;

            foreach (bool byEntryKey in new[] { false, true })
            {
                string mode = byEntryKey ? "chưa lưu · " : "đã đăng · ";

                // Hàng 1: bị bỏ — thêm mới hỏng, đổi thành hỏng, giữ nguyên nhưng bị mục khác làm bỏ, luật lặp thêm mới hỏng.
                yield return new DiffCase(mode + "bị bỏ: thêm chồng giờ + đổi thành giờ hỏng", Document(Running(), Ended()),
                    Document(Running(), Ended().WithTimes(EndedStart, "2026-09-5"), Entry("entry-added", "quest-0913", QuestType, "2026-09-13T00:00:00Z", "2026-09-14T00:00:00Z")),
                    byEntryKey, dropped);
                yield return new DiffCase(mode + "bị bỏ do mục khác (CC-DIFF-2)", Document(Running()),
                    Document(Running(), Entry("entry-earlier", "quest-0911", QuestType, "2026-09-11T00:00:00Z", "2026-09-14T00:00:00Z")), byEntryKey, dropped);
                yield return new DiffCase(mode + "bị bỏ do luật lặp mới che", Document(Running()),
                    Document(new[] { new RecurringLiveEventRule(QuestType, WeeklyAnchor, "quest-r-", 24, 24, "quest_v1") }, Running()), byEntryKey, dropped);
                yield return new DiffCase(mode + "bị bỏ: luật lặp thêm mới chạy lâu hơn chu kỳ", Document(Upcoming()),
                    Document(new[] { new RecurringLiveEventRule("sky-race", WeeklyAnchor, "sky-race-", 24, 30, "sky_v1") }, Upcoming()), byEntryKey, dropped);
                yield return new DiffCase(mode + "quay lại lịch do mục khác được gỡ", Document(Running(), Entry("entry-earlier", "quest-0911", QuestType, "2026-09-11T00:00:00Z", "2026-09-14T00:00:00Z")),
                    Document(Running()), byEntryKey, progressLost);

                // Hàng 2: đợt cố định đang chạy bị xoá / đổi id / đổi loại / dời ra ngoài khung.
                yield return new DiffCase(mode + "đang chạy bị xoá", Document(Running(), Upcoming()), Document(Upcoming()), byEntryKey, progressLost);
                yield return new DiffCase(mode + "đang chạy đổi loại", Document(Running()), Document(Running().WithEventType(OtherType)), byEntryKey, progressLost);
                yield return new DiffCase(mode + "đang chạy dời ra ngoài khung", Document(Running()),
                    Document(Running().WithTimes("2026-09-16T00:00:00Z", "2026-09-18T00:00:00Z")), byEntryKey, progressLost);
                if (byEntryKey)
                {
                    yield return new DiffCase(mode + "đang chạy đổi id", Document(Running()), Document(Running().WithEventId("quest-0912-renamed")), true, progressLost);
                }

                // Hàng 3: luật lặp có lần lặp đang chạy đổi tiền tố / neo / chu kỳ / bị xoá.
                yield return new DiffCase(mode + "luật đổi tiền tố", Document(new[] { weekly }), Document(new[] { weekly.WithIdPrefix("pass-") }), byEntryKey, progressLost);
                yield return new DiffCase(mode + "luật dời neo", Document(new[] { weekly }), Document(new[] { weekly.WithAnchor("2025-12-29T00:00:00Z") }), byEntryKey, progressLost);
                yield return new DiffCase(mode + "luật đổi chu kỳ", Document(new[] { weekly }), Document(new[] { weekly.WithPeriodHours(336) }), byEntryKey, progressLost);
                yield return new DiffCase(mode + "luật bị xoá", Document(new[] { weekly }, Upcoming()), Document(Upcoming()), byEntryKey, progressLost);

                // Hàng 4: đợt đang chạy khép ngay.
                yield return new DiffCase(mode + "đang chạy khép ngay", Document(Running()), Document(Running().WithTimes(RunningStart, "2026-09-13T00:00:00Z")), byEntryKey, progressLost);

                // Hàng 5: đợt đang chạy rút ngắn / dời bắt đầu ra sau now / đổi configKey.
                yield return new DiffCase(mode + "đang chạy rút ngắn", Document(Running()), Document(Running().WithTimes(RunningStart, "2026-09-14T00:00:00Z")), byEntryKey, shouldReview);
                yield return new DiffCase(mode + "đang chạy dời bắt đầu ra sau now", Document(Running()), Document(Running().WithTimes("2026-09-13T12:00:00Z", RunningEnd)), byEntryKey, shouldReview);
                yield return new DiffCase(mode + "đang chạy đổi configKey", Document(Running()), Document(Running().WithConfigKey("quest_v2")), byEntryKey, shouldReview);

                // Hàng 6: luật lặp đang chạy đổi thời gian chạy / configKey.
                yield return new DiffCase(mode + "luật đổi thời gian chạy", Document(new[] { weekly }), Document(new[] { weekly.WithActiveHours(160) }), byEntryKey, shouldReview);
                yield return new DiffCase(mode + "luật đổi configKey", Document(new[] { weekly }), Document(new[] { weekly.WithConfigKey("weekly_pass_s4") }), byEntryKey, shouldReview);

                // Hàng 7: đợt chưa bắt đầu đã có trong bản so: xoá / đổi configKey (kể cả kế thừa mặc định) / đổi loại / đổi id.
                yield return new DiffCase(mode + "chưa bắt đầu bị xoá", Document(Upcoming(), Running()), Document(Running()), byEntryKey, shouldReview);
                yield return new DiffCase(mode + "chưa bắt đầu đổi configKey", Document(Upcoming()), Document(Upcoming().WithConfigKey("quest_v2")), byEntryKey, shouldReview);
                yield return new DiffCase(mode + "chưa bắt đầu kế thừa configKey của loại", Document(Upcoming()),
                    DocumentWithQuestDefault("quest_default", Upcoming().WithConfigKey(string.Empty)), byEntryKey, shouldReview);
                yield return new DiffCase(mode + "chưa bắt đầu đổi loại", Document(Upcoming()), Document(Upcoming().WithEventType(OtherType)), byEntryKey, shouldReview);
                yield return new DiffCase(mode + "chưa bắt đầu đổi id", Document(Upcoming()), Document(Upcoming().WithEventId("quest-0920-renamed")), byEntryKey, shouldReview);

                // Hàng 8: đợt đã khép đổi id (CC-DIFF-3 ở bản đã đăng) / mở lại id / mục mới dùng lại id.
                yield return new DiffCase(mode + "đã khép đổi id", Document(Ended()), Document(Ended().WithEventId("quest-0901-renamed")), byEntryKey, shouldReview);
                yield return new DiffCase(mode + "đã khép mở lại cùng id", Document(Ended()), Document(Ended().WithTimes(UpcomingStart, UpcomingEnd)), byEntryKey, shouldReview);
                yield return new DiffCase(mode + "mục mới dùng lại id đã khép", Document(Ended()),
                    Document(Entry("entry-added", EndedId, QuestType, UpcomingStart, UpcomingEnd)), byEntryKey, shouldReview);
                yield return new DiffCase(mode + "đã khép bị xoá", Document(Ended(), Running()), Document(Running()), byEntryKey, safe);

                // Hàng 9: an toàn.
                yield return new DiffCase(mode + "chưa bắt đầu đổi giờ", Document(Upcoming()), Document(Upcoming().WithTimes("2026-09-21T00:00:00Z", "2026-09-24T00:00:00Z")), byEntryKey, safe);
                yield return new DiffCase(mode + "đang chạy kéo dài", Document(Running()), Document(Running().WithTimes(RunningStart, "2026-09-16T00:00:00Z")), byEntryKey, safe);
                yield return new DiffCase(mode + "thêm mục mới hợp lệ", Document(Running()),
                    Document(Running(), Entry("entry-added", "quest-0925", QuestType, "2026-09-25T00:00:00Z", "2026-09-27T00:00:00Z")), byEntryKey, safe);
                yield return new DiffCase(mode + "đã khép đổi giờ", Document(Ended()), Document(Ended().WithTimes("2026-09-02T00:00:00Z", "2026-09-06T00:00:00Z")), byEntryKey, safe);
            }

            // Hàng 10: định nghĩa loại — chỉ ở "chưa lưu".
            LiveEventCalendarDocument typed = DocumentWithTypes(Upcoming());
            yield return new DiffCase("chưa lưu · loại đổi requiresJoin", typed,
                Replace(typed, new LiveEventTypeDefinition(QuestType, "Nhiệm vụ", 0, true, "quest_v1")), true, shouldReview);
            yield return new DiffCase("chưa lưu · loại đổi tên hiển thị", typed,
                Replace(typed, new LiveEventTypeDefinition(QuestType, "Nhiệm vụ mới", 0, false, "quest_v1")), true, safe);
            yield return new DiffCase("chưa lưu · đổi thứ tự làn", typed, Apply(typed, new MoveEventTypeEdit(OtherType, 0)), true, safe);
        }

        // =============================================================================================================== dữ liệu

        private static FixedLiveEventEntry Entry(string entryKey, string eventId, string eventType, string startUtcText, string endUtcText) =>
            new FixedLiveEventEntry(entryKey, eventId, eventType, startUtcText, endUtcText, "quest_v1");

        private static FixedLiveEventEntry Running() => Entry("entry-running", RunningId, QuestType, RunningStart, RunningEnd);
        private static FixedLiveEventEntry Upcoming() => Entry("entry-upcoming", UpcomingId, QuestType, UpcomingStart, UpcomingEnd);
        private static FixedLiveEventEntry Ended() => Entry("entry-ended", EndedId, QuestType, EndedStart, EndedEnd);

        private static RecurringLiveEventRule WeeklyRule() =>
            new RecurringLiveEventRule("weekly-pass", WeeklyAnchor, "weekly-pass-", 168, 168, "weekly_pass_s3");

        private static LiveEventCalendarDocument Document(params FixedLiveEventEntry[] fixedEvents)
        {
            return Document(Array.Empty<RecurringLiveEventRule>(), fixedEvents);
        }

        private static LiveEventCalendarDocument Document(RecurringLiveEventRule[] rules, params FixedLiveEventEntry[] fixedEvents)
        {
            var builder = new LiveEventCalendarDocumentBuilder();
            foreach (RecurringLiveEventRule rule in rules) builder.WithRecurringRule(rule);
            foreach (FixedLiveEventEntry entry in fixedEvents) builder.WithFixedEvent(entry);
            return builder.Build();
        }

        private static LiveEventCalendarDocument DocumentWithTypes(params FixedLiveEventEntry[] fixedEvents)
        {
            return DocumentWithQuestDefault("quest_v1", fixedEvents);
        }

        private static LiveEventCalendarDocument DocumentWithQuestDefault(string questDefaultConfigKey, params FixedLiveEventEntry[] fixedEvents)
        {
            var builder = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(QuestType, "Nhiệm vụ", 0, false, questDefaultConfigKey))
                .WithEventType(new LiveEventTypeDefinition(OtherType, "Nhiệm vụ khác", 1, false, "other_v1"));
            foreach (FixedLiveEventEntry entry in fixedEvents) builder.WithFixedEvent(entry);
            return builder.Build();
        }

        private static LiveEventCalendarDocument Replace(LiveEventCalendarDocument document, LiveEventTypeDefinition type)
        {
            return Apply(document, new SetEventTypeEdit(type));
        }

        private static LiveEventCalendarDocument Apply(LiveEventCalendarDocument document, LiveEventCalendarEdit edit)
        {
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, edit, out LiveEventCalendarDocument result), "Lệnh sửa của test phải áp được.");
            return result;
        }

        /// <summary>Bản so thật: tài liệu đọc từ JSON đã đăng của mẫu bằng parser game — không có định nghĩa loại (V-3).</summary>
        private static LiveEventCalendarDocument PublishedBaseline()
        {
            LiveEventCalendarDocumentParseResult parsed = JsonLiveEventCalendarParser.ParseDocument(LiveOpsDesignSample.PublishedSnapshotJson);
            Assert.IsTrue(parsed.IsReadable, parsed.ReadErrorText);
            return parsed.Document;
        }

        private static LiveEventCalendarChange FindChange(LiveEventCalendarDiffResult result, string itemId)
        {
            foreach (LiveEventCalendarChange change in result.Changes)
            {
                if (change.ItemId == itemId) return change;
            }
            Assert.Fail("Không có thay đổi cho '" + itemId + "': " + string.Join(" | ", result.Changes));
            return null;
        }

        private static void AssertRow(LiveOpsChangeTextContext context, LiveEventCalendarChange change, string rowText, string consequence)
        {
            Assert.AreEqual(rowText, Plain(LiveOpsChangeText.RowText(change, context, Format)), change.ToString());
            Assert.AreEqual(consequence, Plain(LiveOpsChangeText.ConsequenceSentence(change, context, Format)), change.ToString());
        }

        private static string Plain(string text) => LiveOpsFindingText.PlainText(text);

        /// <summary>Ca có đúng một hàng Nên xem: so câu có ngữ cảnh và câu không ngữ cảnh (không ngữ cảnh chỉ được bớt chữ, không nói sai).</summary>
        private static void AssertShouldReview(string name, LiveEventCalendarDocument baseline, LiveEventCalendarDocument draft, bool byEntryKey,
            string withContext, string withoutContext)
        {
            LiveEventCalendarDiffResult result = byEntryKey
                ? LiveEventCalendarDiff.CompareByEntryKey(baseline, draft, NowUtc)
                : LiveEventCalendarDiff.Compare(baseline, draft, NowUtc);
            Assert.AreEqual(1, result.Changes.Count, name + ": " + string.Join(" | ", result.Changes));
            LiveEventCalendarChange change = result.Changes[0];
            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, change.Consequence, name + ": tiền đề");
            var context = new LiveOpsChangeTextContext(baseline, draft, NowUtc, result, byEntryKey);
            Assert.AreEqual(withContext, Plain(LiveOpsChangeText.ConsequenceSentence(change, context, Format)), name + " (ngữ cảnh)");
            Assert.AreEqual(withoutContext, Plain(LiveOpsChangeText.ConsequenceSentence(change, Format)), name + " (không ngữ cảnh)");
        }

        private static void AssertSentence(string text, string label)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(text), label + " rỗng");
            string plain = Plain(text);
            StringAssert.DoesNotContain("{", plain, label + ": chỗ trống format chưa điền — " + plain);
            StringAssert.DoesNotContain("  ", plain, label + ": mảnh rỗng — " + plain);
            StringAssert.DoesNotContain(": ,", plain, label + ": danh sách field rỗng — " + plain);
            StringAssert.DoesNotContain("()", plain, label + ": ngoặc rỗng — " + plain);
            Assert.IsFalse(plain.EndsWith(" ", StringComparison.Ordinal) || plain.EndsWith(":", StringComparison.Ordinal) || plain.EndsWith(";", StringComparison.Ordinal),
                label + ": câu cụt — " + plain);
        }
    }
}
