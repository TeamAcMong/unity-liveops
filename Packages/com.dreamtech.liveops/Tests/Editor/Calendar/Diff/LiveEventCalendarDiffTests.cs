using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    /// <summary>
    /// Mỗi hàng bảng 6.3 (phân loại hậu quả) một test, trên lịch nhỏ dựng tay quanh BÂY GIỜ của mẫu (13/9/2026 08:47 UTC):
    /// một đợt đang chạy (12/9 → 15/9), một đợt chưa bắt đầu (20/9 → 22/9), một đợt đã khép (1/9 → 5/9), một luật lặp tuần
    /// có lần lặp weekly-pass-35 đang chạy (7/9 → 14/9). Dữ liệu nhỏ để mỗi test chỉ đổi đúng một thứ và thấy rõ vì sao.
    /// </summary>
    [TestFixture]
    public sealed class LiveEventCalendarDiffTests
    {
        private static readonly DateTime NowUtc = LiveOpsDesignSample.NowUtc;

        private const string QuestType = "quest";
        private const string OtherType = "other-quest";
        private const string WeeklyType = "weekly-pass";

        private const string RunningKey = "entry-running";
        private const string UpcomingKey = "entry-upcoming";
        private const string EndedKey = "entry-ended";
        private const string AddedKey = "entry-added";

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

        // ----- Hàng 1: mục nháp bị bộ biên dịch bỏ -----

        [Test]
        public void DroppedDraftEntry_AddedOrChangedIntoBroken_IsDropped()
        {
            LiveEventCalendarDocument baseline = Document(Running(), Ended());
            LiveEventCalendarDocument draft = Document(Running(), Ended().WithTimes(EndedStart, "2026-09-5"),
                // Chồng giờ với đợt đang chạy cùng loại, bắt đầu muộn hơn → bộ biên dịch bỏ đợt này.
                new FixedLiveEventEntry(AddedKey, "quest-0913", QuestType, "2026-09-13T00:00:00Z", "2026-09-14T00:00:00Z", "quest_v1"));

            LiveEventCalendarDiffResult result = LiveEventCalendarDiff.Compare(baseline, draft, NowUtc);

            Assert.AreEqual(2, result.ChangeCount);
            LiveEventCalendarChange added = FindChange(result, "quest-0913");
            Assert.AreEqual(LiveEventCalendarChangeKind.Added, added.Kind);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, added.Consequence);
            Assert.IsTrue(added.WillBeDropped);
            Assert.IsFalse(added.IsReviewRequired, "Hàng Bị bỏ không có Toggle — phải sửa, không phải xem.");

            LiveEventCalendarChange broken = FindChange(result, EndedId);
            Assert.AreEqual(LiveEventCalendarChangeKind.Changed, broken.Kind);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, broken.Consequence);
            Assert.IsTrue(broken.WillBeDropped);
            AssertSingleField(broken, "endUtc", EndedEnd, "2026-09-5");
            Assert.AreEqual(0, result.ReviewRequiredCount);
        }

        // ----- Hàng 2: đợt cố định đang chạy bị xoá / đổi id / đổi loại / dời ra ngoài khung tìm -----

        [TestCase("removed")]
        [TestCase("renamed")]
        [TestCase("retyped")]
        [TestCase("moved-out")]
        public void RunningFixedEvent_RemovedRenamedRetypedOrMovedOut_ProgressLost(string scenario)
        {
            LiveEventCalendarDocument saved = Document(Running(), Upcoming());
            FixedLiveEventEntry changed;
            switch (scenario)
            {
                case "removed": changed = null; break;
                case "renamed": changed = Running().WithEventId("quest-0912-renamed"); break;
                case "retyped": changed = Running().WithEventType(OtherType); break;
                // start mới 16/9 > max(end cũ 15/9, now) → SyncWindowWithCalendar không tìm thấy trong khung.
                default: changed = Running().WithTimes("2026-09-16T00:00:00Z", "2026-09-18T00:00:00Z"); break;
            }
            LiveEventCalendarDocument current = changed != null ? Document(changed, Upcoming()) : Document(Upcoming());

            LiveEventCalendarChange change = SingleChange(LiveEventCalendarDiff.CompareByEntryKey(saved, current, NowUtc));

            Assert.AreEqual(LiveEventCalendarConsequence.ProgressLost, change.Consequence, scenario);
            Assert.IsTrue(change.IsReviewRequired);
            Assert.AreEqual(RunningId, change.RunningEventIdBefore);
            Assert.AreEqual(Utc(2026, 9, 15), change.RunningEventEndUtc);
            if (scenario == "removed" || scenario == "moved-out") Assert.AreEqual(string.Empty, change.RunningEventIdAfter);
            if (scenario == "renamed") Assert.AreEqual("quest-0912-renamed", change.RunningEventIdAfter);
            if (scenario == "removed")
            {
                Assert.AreEqual(LiveEventCalendarChangeKind.Removed, change.Kind);
                Assert.AreEqual(LiveEventCalendarConsequence.ProgressLost,
                    SingleChange(LiveEventCalendarDiff.Compare(saved, current, NowUtc)).Consequence, "Bản đã đăng: cùng hậu quả.");
            }
        }

        // ----- Hàng 3: luật lặp có lần lặp đang chạy đổi id hoặc bị xoá -----

        [TestCase("prefix")]
        [TestCase("anchor")]
        [TestCase("period")]
        [TestCase("removed")]
        public void RunningRecurringInstance_IdChangedOrRuleRemoved_ProgressLost(string scenario)
        {
            RecurringLiveEventRule rule = WeeklyRule();
            RecurringLiveEventRule changed;
            switch (scenario)
            {
                case "prefix": changed = rule.WithIdPrefix("pass-"); break;
                // Dời neo đúng MỘT chu kỳ: khung đợt giữ nguyên nhưng id thành weekly-pass-36.
                case "anchor": changed = rule.WithAnchor("2025-12-29T00:00:00Z"); break;
                case "period": changed = rule.WithPeriodHours(336); break;
                default: changed = null; break;
            }
            LiveEventCalendarDocument baseline = Document(new[] { rule }, Upcoming());
            LiveEventCalendarDocument draft = changed != null ? Document(new[] { changed }, Upcoming()) : Document(Upcoming());

            LiveEventCalendarChange change = SingleChange(LiveEventCalendarDiff.Compare(baseline, draft, NowUtc));

            Assert.AreEqual(LiveEventCalendarItemKind.RecurringRule, change.ItemKind);
            Assert.AreEqual(WeeklyType, change.ItemId);
            Assert.AreEqual(LiveEventCalendarConsequence.ProgressLost, change.Consequence, scenario);
            Assert.IsTrue(change.IsReviewRequired);
            Assert.AreEqual("weekly-pass-35", change.RunningEventIdBefore);
            Assert.AreEqual(Utc(2026, 9, 14), change.RunningEventEndUtc);
            if (scenario == "prefix") Assert.AreEqual("pass-35", change.RunningEventIdAfter);
            if (scenario == "anchor") Assert.AreEqual("weekly-pass-36", change.RunningEventIdAfter);
            if (scenario == "removed") Assert.AreEqual(string.Empty, change.RunningEventIdAfter);
        }

        // ----- Hàng 4: đợt đang chạy bị khép ngay -----

        [TestCase("2026-09-13T08:47:00Z")]
        [TestCase("2026-09-13T00:00:00Z")]
        public void RunningFixedEvent_EndMovedToNowOrEarlier_ProgressLost(string newEnd)
        {
            LiveEventCalendarDocument baseline = Document(Running());
            LiveEventCalendarDocument draft = Document(Running().WithTimes(RunningStart, newEnd));

            LiveEventCalendarChange change = SingleChange(LiveEventCalendarDiff.Compare(baseline, draft, NowUtc));

            Assert.AreEqual(LiveEventCalendarConsequence.ProgressLost, change.Consequence, "Refresh sau khép ngay, chạy luật quà.");
            Assert.IsTrue(change.IsReviewRequired);
            AssertSingleField(change, "endUtc", RunningEnd, newEnd);
        }

        // ----- Hàng 5: đợt đang chạy rút ngắn (vẫn quá now) / dời start ra sau now / đổi configKey -----

        [TestCase("shortened")]
        [TestCase("start-after-now")]
        [TestCase("config-key")]
        public void RunningFixedEvent_ShortenedStartMovedAfterNowOrConfigKeyChanged_ShouldReview(string scenario)
        {
            FixedLiveEventEntry changed;
            switch (scenario)
            {
                case "shortened": changed = Running().WithTimes(RunningStart, "2026-09-14T00:00:00Z"); break;
                case "start-after-now": changed = Running().WithTimes("2026-09-13T12:00:00Z", RunningEnd); break;
                default: changed = Running().WithConfigKey("quest_v2"); break;
            }

            LiveEventCalendarChange change = SingleChange(LiveEventCalendarDiff.Compare(Document(Running()), Document(changed), NowUtc));

            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, change.Consequence, scenario);
            Assert.IsFalse(change.IsReviewRequired);
            Assert.AreEqual(RunningId, change.RunningEventIdBefore);
            Assert.AreEqual(RunningId, change.RunningEventIdAfter, "Bản ghi vẫn tìm thấy đợt cùng id.");
        }

        // ----- Hàng 6: luật lặp đang chạy đổi activeHours (giữ id) / configKey -----

        [TestCase("active-hours")]
        [TestCase("config-key")]
        public void RunningRecurringInstance_ActiveHoursOrConfigKeyChanged_ShouldReview(string scenario)
        {
            RecurringLiveEventRule rule = WeeklyRule();
            // 160 giờ: lần lặp 35 kết thúc 13/9 16:00 — vẫn sau now, nên không phải "khép ngay".
            RecurringLiveEventRule changed = scenario == "active-hours" ? rule.WithActiveHours(160) : rule.WithConfigKey("weekly_pass_s4");

            LiveEventCalendarChange change = SingleChange(
                LiveEventCalendarDiff.Compare(Document(new[] { rule }), Document(new[] { changed }), NowUtc));

            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, change.Consequence, scenario);
            Assert.AreEqual("weekly-pass-35", change.RunningEventIdBefore);
            Assert.AreEqual("weekly-pass-35", change.RunningEventIdAfter, "Id giữ, chỉ đổi khung/khoá.");
        }

        // ----- Hàng 7: đợt chưa bắt đầu đã có trong bản so: xoá / đổi id / đổi configKey / đổi loại -----

        [TestCase("removed")]
        [TestCase("renamed")]
        [TestCase("config-key")]
        [TestCase("retyped")]
        public void UpcomingPublishedEvent_RemovedRenamedConfigKeyOrRetyped_ShouldReview(string scenario)
        {
            FixedLiveEventEntry changed;
            switch (scenario)
            {
                case "removed": changed = null; break;
                case "renamed": changed = Upcoming().WithEventId("quest-0920-renamed"); break;
                case "config-key": changed = Upcoming().WithConfigKey("quest_v2"); break;
                default: changed = Upcoming().WithEventType(OtherType); break;
            }
            LiveEventCalendarDocument saved = Document(Upcoming());
            LiveEventCalendarDocument current = changed != null ? Document(changed) : Document();

            LiveEventCalendarChange change = SingleChange(LiveEventCalendarDiff.CompareByEntryKey(saved, current, NowUtc));

            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, change.Consequence, scenario);
            Assert.AreEqual(string.Empty, change.RunningEventIdBefore, "Người chơi chưa có bản ghi.");
            Assert.IsNull(change.RunningEventEndUtc);
        }

        // ----- Hàng 8: đợt đã khép đổi id / id của đợt đã khép bị dùng lại -----

        [TestCase("renamed")]
        [TestCase("reopened-same-id")]
        [TestCase("reused-by-new-entry")]
        public void EndedEvent_RenamedOrIdReused_ShouldReview(string scenario)
        {
            LiveEventCalendarDocument saved = Document(Ended());
            LiveEventCalendarDiffResult result;
            string expectedItemId = EndedId;
            switch (scenario)
            {
                case "renamed":
                    expectedItemId = "quest-0901-renamed";
                    result = LiveEventCalendarDiff.CompareByEntryKey(saved, Document(Ended().WithEventId(expectedItemId)), NowUtc);
                    break;
                case "reopened-same-id":
                    // Bản đã đăng: danh tính theo id nên đợt mới dùng lại id là "đổi giờ" của đợt cũ — nhưng mở lại cho tương lai.
                    result = LiveEventCalendarDiff.Compare(saved, Document(Ended().WithTimes(UpcomingStart, UpcomingEnd)), NowUtc);
                    break;
                default:
                    result = LiveEventCalendarDiff.CompareByEntryKey(saved,
                        Document(new FixedLiveEventEntry(AddedKey, EndedId, QuestType, UpcomingStart, UpcomingEnd, "quest_v1")), NowUtc);
                    Assert.AreEqual(LiveEventCalendarConsequence.Safe, FindChange(result, LiveEventCalendarChangeKind.Removed).Consequence,
                        "Xoá đợt đã khép không đổi gì với người chơi.");
                    break;
            }

            LiveEventCalendarChange change = scenario == "reused-by-new-entry"
                ? FindChange(result, LiveEventCalendarChangeKind.Added)
                : SingleChange(result);
            Assert.AreEqual(expectedItemId, change.ItemId);
            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, change.Consequence, scenario + ": RetiredEventIds chặn người đã chơi vào lại.");
        }

        // ----- Hàng 9: an toàn -----

        [TestCase("upcoming-retimed")]
        [TestCase("running-extended")]
        [TestCase("added-valid")]
        [TestCase("ended-retimed")]
        public void SafeChanges_RetimedUpcomingExtendedRunningAddedValidRetimedEnded_Safe(string scenario)
        {
            LiveEventCalendarDocument baseline = Document(Running(), Upcoming(), Ended());
            LiveEventCalendarDocument draft;
            switch (scenario)
            {
                case "upcoming-retimed": draft = Document(Running(), Upcoming().WithTimes("2026-09-21T00:00:00Z", "2026-09-24T00:00:00Z"), Ended()); break;
                case "running-extended": draft = Document(Running().WithTimes(RunningStart, "2026-09-16T00:00:00Z"), Upcoming(), Ended()); break;
                case "added-valid":
                    draft = Document(Running(), Upcoming(), Ended(),
                        new FixedLiveEventEntry(AddedKey, "quest-0925", QuestType, "2026-09-25T00:00:00Z", "2026-09-27T00:00:00Z", "quest_v1"));
                    break;
                default: draft = Document(Running(), Upcoming(), Ended().WithTimes("2026-09-02T00:00:00Z", "2026-09-06T00:00:00Z")); break;
            }

            LiveEventCalendarDiffResult result = LiveEventCalendarDiff.Compare(baseline, draft, NowUtc);
            LiveEventCalendarChange change = SingleChange(result);

            Assert.AreEqual(LiveEventCalendarConsequence.Safe, change.Consequence, scenario);
            Assert.IsFalse(change.IsReviewRequired);
            Assert.IsFalse(change.WillBeDropped);
            Assert.AreEqual(scenario == "added-valid" ? 3 : 2, result.KeptCount, "Chỉ mục vừa đổi/thêm nằm ngoài Giữ.");
        }

        // ----- Hàng 10: định nghĩa loại — chỉ ở "chưa lưu" -----

        [Test]
        public void EventTypeDefinitionChanges_OnlyInUnsavedDiff_RequiresJoinShouldReviewOthersSafe()
        {
            LiveEventCalendarDocument saved = DocumentWithTypes(Upcoming());
            LiveEventCalendarDocument current = Apply(saved,
                new SetEventTypeEdit(QuestTypeDefinition().WithDisplayName("Nhiệm vụ mới").WithColorSlot(3)),
                new SetEventTypeEdit(OtherTypeDefinition().WithRequiresJoin(true)));

            LiveEventCalendarDiffResult unsaved = LiveEventCalendarDiff.CompareByEntryKey(saved, current, NowUtc);
            Assert.AreEqual(2, unsaved.ChangedCount);
            LiveEventCalendarChange renamedType = FindChange(unsaved, QuestType);
            Assert.AreEqual(LiveEventCalendarItemKind.EventType, renamedType.ItemKind);
            Assert.AreEqual(LiveEventCalendarConsequence.Safe, renamedType.Consequence);
            Assert.AreEqual(2, renamedType.Fields.Count);
            Assert.AreEqual("displayName", renamedType.Fields[0].FieldName);
            Assert.AreEqual("colorSlot", renamedType.Fields[1].FieldName);
            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, FindChange(unsaved, OtherType).Consequence);

            Assert.IsTrue(LiveEventCalendarDiff.Compare(saved, current, NowUtc).IsEmpty,
                "Loại đi theo build, không theo JSON — không có trong diff với bản đã đăng (V-3).");
        }

        [Test]
        public void CompareByEntryKey_TypeRequiresJoinChanged_ShouldReview()
        {
            LiveEventCalendarDocument saved = DocumentWithTypes(Running());
            LiveEventCalendarDocument current = Apply(saved, new SetEventTypeEdit(QuestTypeDefinition().WithRequiresJoin(true)));

            LiveEventCalendarChange change = SingleChange(LiveEventCalendarDiff.CompareByEntryKey(saved, current, NowUtc));

            Assert.AreEqual(LiveEventCalendarItemKind.EventType, change.ItemKind);
            Assert.AreEqual(LiveEventCalendarChangeKind.Changed, change.Kind);
            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, change.Consequence);
            AssertSingleField(change, "requiresJoin", "false", "true");
            Assert.AreEqual(string.Empty, change.EntryKey);
        }

        [Test]
        public void CompareByEntryKey_LaneOrderChanged_SafeChange()
        {
            LiveEventCalendarDocument saved = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("alpha", "Alpha", 0, false, "alpha_v1"))
                .WithEventType(new LiveEventTypeDefinition("beta", "Beta", 1, false, "beta_v1"))
                .WithEventType(new LiveEventTypeDefinition("gamma", "Gamma", 2, false, "gamma_v1"))
                .WithEventType(new LiveEventTypeDefinition("delta", "Delta", 3, false, "delta_v1"))
                .Build();
            // Kéo làn cuối lên đầu: một thao tác = một thay đổi, không phải "mọi làn đổi chỉ số".
            LiveEventCalendarDocument current = Apply(saved, new MoveEventTypeEdit("delta", 0));

            LiveEventCalendarDiffResult unsaved = LiveEventCalendarDiff.CompareByEntryKey(saved, current, NowUtc);
            LiveEventCalendarChange change = SingleChange(unsaved);

            Assert.AreEqual("delta", change.ItemId);
            Assert.AreEqual(LiveEventCalendarConsequence.Safe, change.Consequence);
            AssertSingleField(change, "order", "4", "1");
            Assert.AreEqual(3, unsaved.KeptCount);
            Assert.IsTrue(LiveEventCalendarDiff.Compare(saved, current, NowUtc).IsEmpty, "Thứ tự làn không đổi JSON (V-12).");
        }

        // ----- Danh tính, đếm, sắp -----

        [Test]
        public void CompareByEntryKey_RenameIsChangeNotAddRemove()
        {
            LiveEventCalendarDocument saved = Document(Upcoming(), Ended());
            LiveEventCalendarDocument current = Document(Upcoming().WithEventId("quest-0920-renamed"), Ended());

            LiveEventCalendarDiffResult unsaved = LiveEventCalendarDiff.CompareByEntryKey(saved, current, NowUtc);
            Assert.AreEqual(0, unsaved.AddedCount);
            Assert.AreEqual(0, unsaved.RemovedCount);
            Assert.AreEqual(1, unsaved.ChangedCount);
            LiveEventCalendarChange change = unsaved.Changes[0];
            Assert.AreEqual(UpcomingKey, change.EntryKey);
            Assert.AreEqual("quest-0920-renamed", change.ItemId);
            AssertSingleField(change, "id", UpcomingId, "quest-0920-renamed");

            LiveEventCalendarDiffResult published = LiveEventCalendarDiff.Compare(saved, current, NowUtc);
            Assert.AreEqual(1, published.AddedCount, "Với bản đã đăng danh tính là id: đổi id = xoá + thêm.");
            Assert.AreEqual(1, published.RemovedCount);
        }

        [Test]
        public void Kept_NotListed_ButCounted()
        {
            RecurringLiveEventRule rule = WeeklyRule();
            LiveEventCalendarDocument baseline = Document(new[] { rule }, Running(), Upcoming(), Ended());
            LiveEventCalendarDocument draft = Document(new[] { rule }, Running(), Upcoming().WithConfigKey("quest_v2"), Ended());

            LiveEventCalendarDiffResult result = LiveEventCalendarDiff.Compare(baseline, draft, NowUtc);

            Assert.AreEqual(3, result.KeptCount, "Luật + 2 đợt không đổi.");
            Assert.AreEqual(1, result.Changes.Count);
            Assert.AreEqual(1, result.ChangeCount);
            foreach (LiveEventCalendarChange change in result.Changes) Assert.AreNotEqual(LiveEventCalendarChangeKind.Kept, change.Kind);

            LiveEventCalendarDiffResult same = LiveEventCalendarDiff.Compare(baseline, baseline, NowUtc);
            Assert.IsTrue(same.IsEmpty);
            Assert.AreEqual(4, same.KeptCount);
        }

        [Test]
        public void Changes_SortedByConsequenceThenItemKindThenTime()
        {
            RecurringLiveEventRule rule = WeeklyRule();
            LiveEventCalendarDocument baseline = Document(new[] { rule }, Running(), Upcoming(), Ended());
            LiveEventCalendarDocument draft = Document(new[] { rule.WithIdPrefix("pass-") },
                Running().WithTimes(RunningStart, "2026-09-16T00:00:00Z"),
                Upcoming().WithConfigKey("quest_v2"),
                Ended().WithTimes(EndedStart, "broken"),
                new FixedLiveEventEntry(AddedKey, "quest-0930", QuestType, "2026-09-30T00:00:00Z", "2026-10-01T00:00:00Z", "quest_v1"));

            LiveEventCalendarDiffResult result = LiveEventCalendarDiff.Compare(baseline, draft, NowUtc);

            var actualOrder = new List<string>();
            foreach (LiveEventCalendarChange change in result.Changes) actualOrder.Add(change.Consequence + ":" + change.ItemId);
            CollectionAssert.AreEqual(new[]
            {
                "Dropped:" + EndedId,
                "ProgressLost:" + WeeklyType,
                "ShouldReview:" + UpcomingId,
                "Safe:" + RunningId,
                "Safe:quest-0930",
            }, actualOrder);
        }

        [Test]
        public void NullOrDuplicateInput_NeverThrows()
        {
            Assert.IsTrue(LiveEventCalendarDiff.Compare(null, null, NowUtc).IsEmpty);
            Assert.AreEqual(1, LiveEventCalendarDiff.CompareByEntryKey(null, Document(Upcoming()), NowUtc).AddedCount);

            // Hai đợt cùng id (tài liệu dán vào): ghép theo thứ tự xuất hiện, không gộp, không ném.
            LiveEventCalendarDocument duplicated = Document(Upcoming(), new FixedLiveEventEntry(AddedKey, UpcomingId, QuestType,
                "2026-09-25T00:00:00Z", "2026-09-26T00:00:00Z", "quest_v1"));
            LiveEventCalendarDiffResult result = LiveEventCalendarDiff.Compare(Document(Upcoming()), duplicated, NowUtc);
            Assert.AreEqual(1, result.KeptCount);
            Assert.AreEqual(1, result.AddedCount);
            Assert.IsTrue(result.Changes[0].WillBeDropped, "Đợt trùng id bị bộ biên dịch bỏ.");

            Assert.DoesNotThrow(() => LiveEventCalendarDiff.Compare(Document(Running()), Document(Upcoming()), DateTime.MaxValue));
            Assert.DoesNotThrow(() => LiveEventCalendarDiff.Compare(Document(new[] { WeeklyRule() }), Document(), DateTime.MinValue));
        }

        // ----- Dựng dữ liệu -----

        private static FixedLiveEventEntry Running() =>
            new FixedLiveEventEntry(RunningKey, RunningId, QuestType, RunningStart, RunningEnd, "quest_v1");

        private static FixedLiveEventEntry Upcoming() =>
            new FixedLiveEventEntry(UpcomingKey, UpcomingId, QuestType, UpcomingStart, UpcomingEnd, "quest_v1");

        private static FixedLiveEventEntry Ended() =>
            new FixedLiveEventEntry(EndedKey, EndedId, QuestType, EndedStart, EndedEnd, "quest_v1");

        private static RecurringLiveEventRule WeeklyRule() =>
            new RecurringLiveEventRule(WeeklyType, WeeklyAnchor, "weekly-pass-", 168, 168, "weekly_pass_s3");

        private static LiveEventTypeDefinition QuestTypeDefinition() => new LiveEventTypeDefinition(QuestType, "Nhiệm vụ", 0, false, "quest_v1");

        private static LiveEventTypeDefinition OtherTypeDefinition() => new LiveEventTypeDefinition(OtherType, "Nhiệm vụ khác", 1, false, "other_v1");

        /// <summary>Không có định nghĩa loại — giống bản so dựng từ JSON (V-3).</summary>
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
            var builder = new LiveEventCalendarDocumentBuilder()
                .WithEventType(QuestTypeDefinition())
                .WithEventType(OtherTypeDefinition());
            foreach (FixedLiveEventEntry entry in fixedEvents) builder.WithFixedEvent(entry);
            return builder.Build();
        }

        private static LiveEventCalendarDocument Apply(LiveEventCalendarDocument document, params LiveEventCalendarEdit[] edits)
        {
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new CompositeCalendarEdit(edits), out LiveEventCalendarDocument result),
                "Lệnh sửa của test phải áp được.");
            return result;
        }

        private static DateTime Utc(int year, int month, int day) => new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

        private static LiveEventCalendarChange SingleChange(LiveEventCalendarDiffResult result)
        {
            Assert.AreEqual(1, result.Changes.Count, "Mong đúng một thay đổi: " + string.Join(" | ", result.Changes));
            return result.Changes[0];
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

        private static LiveEventCalendarChange FindChange(LiveEventCalendarDiffResult result, LiveEventCalendarChangeKind kind)
        {
            foreach (LiveEventCalendarChange change in result.Changes)
            {
                if (change.Kind == kind) return change;
            }
            Assert.Fail("Không có thay đổi loại " + kind + ": " + string.Join(" | ", result.Changes));
            return null;
        }

        private static void AssertSingleField(LiveEventCalendarChange change, string fieldName, string beforeText, string afterText)
        {
            Assert.AreEqual(1, change.Fields.Count, "Field: " + string.Join(" | ", change.Fields));
            Assert.AreEqual(fieldName, change.Fields[0].FieldName);
            Assert.AreEqual(beforeText, change.Fields[0].BeforeText);
            Assert.AreEqual(afterText, change.Fields[0].AfterText);
        }
    }
}
