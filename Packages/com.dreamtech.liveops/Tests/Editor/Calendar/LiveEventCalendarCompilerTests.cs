using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class LiveEventCalendarCompilerTests
    {
        private static readonly DateTime Anchor = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        // ----- Biên dịch: dữ liệu mẫu -----

        [Test]
        public void DesignSample_Keeps6Of8_WithDropReasons()
        {
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.CompileInExportOrder(LiveOpsDesignSample.Document);

            Assert.AreEqual(8, compilation.EntryCount, "2 luật + 6 đợt cố định.");
            Assert.AreEqual(6, compilation.KeptCount);
            Assert.AreEqual(2, compilation.DroppedCount);

            Assert.IsTrue(compilation.TryGetFixedOutcome(LiveOpsDesignSample.LavaQuestLateEntryKey, out LiveEventCalendarEntryOutcome lateLava));
            Assert.IsFalse(lateLava.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.UnreadableEndUtc, lateLava.DropReason);

            Assert.IsTrue(compilation.TryGetFixedOutcome(LiveOpsDesignSample.HuntBonusEntryKey, out LiveEventCalendarEntryOutcome huntBonus));
            Assert.IsFalse(huntBonus.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.OverlapsSameType, huntBonus.DropReason);
            Assert.AreEqual("hunt-0914", huntBonus.RelatedEventId);

            Assert.IsTrue(compilation.TryGetFixedOutcome(LiveOpsDesignSample.LavaQuestEarlyEntryKey, out LiveEventCalendarEntryOutcome earlyLava));
            Assert.IsTrue(earlyLava.IsKept);
            Assert.IsTrue(compilation.TryGetRecurringOutcome("weekly-pass", out LiveEventCalendarEntryOutcome weeklyPass));
            Assert.IsTrue(weeklyPass.IsKept);
        }

        [Test]
        public void SourceIndex_IsPositionInOriginalList_NotFiltered()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.Compile(document);

            for (int index = 0; index < document.RecurringRules.Count; index++)
            {
                Assert.AreEqual(index, compilation.Entries[index].SourceIndex);
            }
            for (int index = 0; index < document.FixedEvents.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = compilation.Entries[document.RecurringRules.Count + index];
                Assert.AreEqual(index, outcome.SourceIndex, "Kể cả mục bị bỏ vẫn giữ đúng vị trí gốc, không bị lọc khỏi danh sách.");
                Assert.AreEqual(document.FixedEvents[index].EntryKey, outcome.EntryKey);
            }
        }

        // ----- Câu Problems tương thích 0.1.0 -----

        // Đuôi tên tham số runtime nối vào ArgumentException.Message (Mono 2022.3 "\nParameter name: x", .NET 6000.6
        // " (Parameter 'x')") — không phải câu của game; bỏ đi khi đối chiếu với message thật của LiveEventInstance.
        private static readonly Regex RuntimeParameterSuffix = new Regex(@"(\r?\nParameter name: \w+)|( \(Parameter '\w+'\))");

        [Test]
        public void Problems_Format1Strings_EqualParser010()
        {
            // Cùng dữ liệu với JsonLiveEventCalendarParserGoldenTests (G-GOLDEN) — thiếu id/loại trong JSON = chuỗi rỗng.
            LiveEventCalendarDocument brokenEntries = FixedOnly(
                Entry("ok", "hunt", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"),
                Entry("", "hunt", "2026-09-20T00:00:00Z", "2026-09-21T00:00:00Z"),
                Entry("bad-date", "hunt", "14/09/2026", "2026-09-15T00:00:00Z"),
                Entry("backwards", "hunt", "2026-09-25T00:00:00Z", "2026-09-24T00:00:00Z"),
                Entry("overlap", "hunt", "2026-09-14T12:00:00Z", "2026-09-16T00:00:00Z"));
            CollectionAssert.AreEqual(new[]
            {
                "Mục thứ 2: Event id không được rỗng. — bỏ qua.",
                "Mục thứ 3 ('bad-date'): startUtc không phải giờ ISO 8601: '14/09/2026' — bỏ qua.",
                "Mục thứ 4: Đợt event phải kết thúc sau khi bắt đầu: backwards — bỏ qua.",
                "Đợt 'overlap' chồng giờ với 'ok' cùng loại 'hunt' — bỏ 'overlap'.",
            }, LiveEventCalendarCompiler.Compile(brokenEntries).Problems);

            LiveEventCalendarDocument multiFault = FixedOnly(
                Entry("", "hunt", "14/09/2026", "2026-09-15T00:00:00Z"),
                Entry("both-dates", "hunt", "hôm nay", "mai"),
                Entry("bad-end", "hunt", "2026-09-14T00:00:00Z", "mai"),
                Entry("a#b", "hunt", "2026-09-14T00:00:00Z", "mai"),
                Entry("", "hunt", "2026-09-25T00:00:00Z", "2026-09-24T00:00:00Z"),
                Entry("no-type", "", "2026-09-25T00:00:00Z", "2026-09-24T00:00:00Z"),
                Entry("a#b", "", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"),
                Entry("typed", "hunt#x", "2026-09-25T00:00:00Z", "2026-09-24T00:00:00Z"),
                Entry("line\nbreak", "hunt", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"));
            CollectionAssert.AreEqual(new[]
            {
                "Mục thứ 1 (''): startUtc không phải giờ ISO 8601: '14/09/2026' — bỏ qua.",
                "Mục thứ 2 ('both-dates'): startUtc không phải giờ ISO 8601: 'hôm nay' — bỏ qua.",
                "Mục thứ 3 ('bad-end'): endUtc không phải giờ ISO 8601: 'mai' — bỏ qua.",
                "Mục thứ 4 ('a#b'): endUtc không phải giờ ISO 8601: 'mai' — bỏ qua.",
                "Mục thứ 5: Event id không được rỗng. — bỏ qua.",
                "Mục thứ 6: Loại event không được rỗng. — bỏ qua.",
                "Mục thứ 7: Event id không được chứa '#' hay xuống dòng: a#b — bỏ qua.",
                "Mục thứ 8: Loại event không được chứa '#' hay xuống dòng: hunt#x — bỏ qua.",
                "Mục thứ 9: Event id không được chứa '#' hay xuống dòng: line\nbreak — bỏ qua.",
            }, LiveEventCalendarCompiler.Compile(multiFault).Problems);

            LiveEventCalendarDocument duplicateAndOverlap = FixedOnly(
                Entry("a", "hunt", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"),
                Entry("a", "hunt", "2026-09-16T00:00:00Z", "2026-09-17T00:00:00Z"),
                Entry("b", "hunt", "2026-09-14T12:00:00Z", "2026-09-16T00:00:00Z"));
            CollectionAssert.AreEqual(new[]
            {
                "Trùng id 'a' ở mục thứ 2 — giữ mục xuất hiện trước.",
                "Đợt 'b' chồng giờ với 'a' cùng loại 'hunt' — bỏ 'b'.",
            }, LiveEventCalendarCompiler.Compile(duplicateAndOverlap).Problems);

            // Bẫy vị trí 0.1.0: câu trùng id đếm trong danh sách đã lọc mục hỏng ("mục thứ 2"), SourceIndex vẫn là vị trí gốc (2).
            LiveEventCalendarDocument brokenBeforeDuplicate = FixedOnly(
                Entry("bad-date", "hunt", "14/09/2026", "2026-09-15T00:00:00Z"),
                Entry("a", "hunt", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"),
                Entry("a", "hunt", "2026-09-16T00:00:00Z", "2026-09-17T00:00:00Z"));
            LiveEventCalendarCompilation brokenBeforeDuplicateCompilation = LiveEventCalendarCompiler.Compile(brokenBeforeDuplicate);
            CollectionAssert.AreEqual(new[]
            {
                "Mục thứ 1 ('bad-date'): startUtc không phải giờ ISO 8601: '14/09/2026' — bỏ qua.",
                "Trùng id 'a' ở mục thứ 2 — giữ mục xuất hiện trước.",
            }, brokenBeforeDuplicateCompilation.Problems);
            Assert.IsTrue(brokenBeforeDuplicateCompilation.TryGetFixedOutcome("entry-2", out LiveEventCalendarEntryOutcome duplicate));
            Assert.AreEqual(2, duplicate.SourceIndex);
            Assert.AreEqual(brokenBeforeDuplicateCompilation.Problems[1], duplicate.ProblemText);

            // Câu id/loại/giờ ngược phải trùng message thật của LiveEventInstance (nguồn của 0.1.0), không chỉ trùng chuỗi chép tay.
            AssertSameAsInstanceMessage(multiFault, "entry-4", () => new LiveEventInstance("", "hunt", Utc(25), Utc(24)));
            AssertSameAsInstanceMessage(multiFault, "entry-5", () => new LiveEventInstance("no-type", "", Utc(25), Utc(24)));
            AssertSameAsInstanceMessage(multiFault, "entry-6", () => new LiveEventInstance("a#b", "", Utc(14), Utc(15)));
            AssertSameAsInstanceMessage(multiFault, "entry-7", () => new LiveEventInstance("typed", "hunt#x", Utc(25), Utc(24)));
            AssertSameAsInstanceMessage(brokenEntries, "entry-3", () => new LiveEventInstance("backwards", "hunt", Utc(25), Utc(24)));
        }

        // ----- Trùng id, chồng giờ, bị luật lặp che -----

        [Test]
        public void DuplicateId_KeepsFirstInSourceOrder()
        {
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.Compile(FixedOnly(
                Entry("same", "hunt", "2026-09-20T00:00:00Z", "2026-09-21T00:00:00Z"),
                Entry("same", "hunt", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z")));

            compilation.TryGetFixedOutcome("entry-0", out LiveEventCalendarEntryOutcome first);
            compilation.TryGetFixedOutcome("entry-1", out LiveEventCalendarEntryOutcome second);
            Assert.IsTrue(first.IsKept, "Compile giữ đúng thứ tự đầu vào: mục đứng trước thắng dù bắt đầu muộn hơn.");
            Assert.IsFalse(second.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.DuplicateEventId, second.DropReason);
            Assert.AreEqual("same", second.RelatedEventId);
            Assert.AreEqual(1, compilation.FixedCalendar.Instances.Count);
            Assert.AreEqual(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc), compilation.FixedCalendar.Instances[0].StartUtc);
        }

        [Test]
        public void Overlap_KeepsEarlierStart_TouchingEdgesAllowed()
        {
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.Compile(FixedOnly(
                Entry("late", "hunt", "2026-09-14T12:00:00Z", "2026-09-16T00:00:00Z"),
                Entry("early", "hunt", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z"),
                Entry("touching", "hunt", "2026-09-15T00:00:00Z", "2026-09-15T06:00:00Z"),
                Entry("other-type", "sky-race", "2026-09-14T06:00:00Z", "2026-09-14T08:00:00Z")));

            compilation.TryGetFixedOutcome("entry-0", out LiveEventCalendarEntryOutcome late);
            Assert.IsFalse(late.IsKept, "Đợt bắt đầu sớm hơn được giữ dù đứng sau trong danh sách.");
            Assert.AreEqual(LiveEventCalendarDropReason.OverlapsSameType, late.DropReason);
            Assert.AreEqual("early", late.RelatedEventId);
            Assert.AreEqual(new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc), late.OverlapStartUtc);
            Assert.AreEqual(new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc), late.OverlapEndUtc);

            compilation.TryGetFixedOutcome("entry-1", out LiveEventCalendarEntryOutcome early);
            compilation.TryGetFixedOutcome("entry-2", out LiveEventCalendarEntryOutcome touching);
            compilation.TryGetFixedOutcome("entry-3", out LiveEventCalendarEntryOutcome otherType);
            Assert.IsTrue(early.IsKept);
            Assert.IsTrue(touching.IsKept, "Chạm mép (bắt đầu đúng lúc đợt trước kết thúc) không phải chồng giờ.");
            Assert.IsTrue(otherType.IsKept, "Chồng giờ chỉ xét cùng loại.");
        }

        [Test]
        public void Composite_RecurringFirst_FixedOverlapIsShadowedByRecurring()
        {
            // sky-race chạy 00:00–20:00 mỗi ngày: đợt 19:00–21:00 chồng lần lặp; đợt 21:00–23:00 nằm gọn trong khe nghỉ (và
            // chạm mép, không chồng, đợt 19:00–21:00 — nếu chồng thì nó bị bỏ vì OverlapsSameType trước khi xét luật lặp).
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "sky-race-", 24, 20, ""))
                .WithFixedEvent(new FixedLiveEventEntry("entry-0", "sky-overlap", "sky-race", "2026-01-05T19:00:00Z", "2026-01-05T21:00:00Z", ""))
                .WithFixedEvent(new FixedLiveEventEntry("entry-1", "sky-night", "sky-race", "2026-01-05T21:00:00Z", "2026-01-05T23:00:00Z", ""))
                .Build();

            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.Compile(document);

            compilation.TryGetFixedOutcome("entry-0", out LiveEventCalendarEntryOutcome overlap);
            Assert.IsFalse(overlap.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.ShadowedByRecurring, overlap.DropReason);
            Assert.AreEqual("sky-race-0", overlap.RelatedEventId);
            compilation.TryGetFixedOutcome("entry-1", out LiveEventCalendarEntryOutcome night);
            Assert.IsTrue(night.IsKept);

            Assert.AreEqual(2, compilation.FixedCalendar.Instances.Count, "FixedCalendar không biết luật lặp — vẫn chứa đợt bị che.");
            IReadOnlyList<LiveEventInstance> seenByGame = compilation.Calendar.GetInstances("sky-race",
                new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc));
            CollectionAssert.AreEqual(new[] { "sky-race-0", "sky-night" }, EventIds(seenByGame));
            Assert.AreEqual(1, compilation.DroppedCount);
        }

        [Test]
        public void Composite_FixedIdEqualsOccurrenceId_IsShadowedByRecurring()
        {
            // Không chồng giờ với lần lặp nào (sky-race-0 chạy 00:00–01:00, đợt cố định 12:00–13:00) nhưng trùng id của lần
            // lặp 0: Composite bỏ đợt này ở mọi khung hỏi có chứa sky-race-0 — IsKept phải là false (6.1 luật 7).
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "sky-race-", 24, 1, ""))
                .WithFixedEvent(new FixedLiveEventEntry("entry-0", "sky-race-0", "sky-race", "2026-01-05T12:00:00Z", "2026-01-05T13:00:00Z", ""))
                .WithFixedEvent(new FixedLiveEventEntry("entry-1", "sky-race-007", "sky-race", "2026-01-06T12:00:00Z", "2026-01-06T13:00:00Z", ""))
                .WithFixedEvent(new FixedLiveEventEntry("entry-2", "sky-race--3", "sky-race", "2026-01-07T12:00:00Z", "2026-01-07T13:00:00Z", ""))
                .Build();

            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.Compile(document);

            compilation.TryGetFixedOutcome("entry-0", out LiveEventCalendarEntryOutcome sameId);
            Assert.IsFalse(sameId.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.ShadowedByRecurring, sameId.DropReason);
            Assert.AreEqual("sky-race-0", sameId.RelatedEventId);
            IReadOnlyList<LiveEventInstance> dayOne = compilation.Calendar.GetInstances("sky-race",
                new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual(1, dayOne.Count, "Game chỉ thấy lần lặp sky-race-0, không thấy đợt cố định trùng id.");
            Assert.AreEqual(new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc), dayOne[0].StartUtc);

            compilation.TryGetFixedOutcome("entry-1", out LiveEventCalendarEntryOutcome leadingZero);
            Assert.IsTrue(leadingZero.IsKept, "\"007\" không phải cách lịch lặp viết số thứ tự nên không trùng id lần lặp nào.");
            compilation.TryGetFixedOutcome("entry-2", out LiveEventCalendarEntryOutcome negativeIndex);
            Assert.IsFalse(negativeIndex.IsKept, "Lần lặp trước mốc có id âm (sky-race--3) cũng là id lần lặp.");
            Assert.AreEqual(LiveEventCalendarDropReason.ShadowedByRecurring, negativeIndex.DropReason);
        }

        // ----- Đa lỗi trên một mục (V-7) -----

        [Test]
        public void Compiler_MultiFaultEntry_OnePrimaryReasonPlusAdditional()
        {
            var document = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("k1", "bad#id", "type-a", "2026-09-20T00:00:00Z", "2026-09-19T00:00:00Z", ""))
                .Build();

            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.Compile(document);

            Assert.IsTrue(compilation.TryGetFixedOutcome("k1", out LiveEventCalendarEntryOutcome outcome));
            Assert.IsFalse(outcome.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.InvalidIdentifier, outcome.DropReason, "Id/loại hỏng đứng trước end<=start theo thứ tự kiểm.");
            Assert.AreEqual(1, outcome.AdditionalItemReasons.Count);
            Assert.AreEqual(LiveEventCalendarDropReason.EndNotAfterStart, outcome.AdditionalItemReasons[0]);
        }

        [Test]
        public void Compiler_BothTimesUnreadable_PrimaryIsStart_AdditionalIsEnd()
        {
            var document = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("k1", "e1", "type-a", "not-a-time", "also-not-a-time", ""))
                .Build();

            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.Compile(document);

            compilation.TryGetFixedOutcome("k1", out LiveEventCalendarEntryOutcome outcome);
            Assert.AreEqual(LiveEventCalendarDropReason.UnreadableStartUtc, outcome.DropReason);
            Assert.AreEqual(1, outcome.AdditionalItemReasons.Count);
            Assert.AreEqual(LiveEventCalendarDropReason.UnreadableEndUtc, outcome.AdditionalItemReasons[0]);
        }

        // ----- Luật lặp hỏng -----

        [Test]
        public void InvalidRecurring_ActiveLongerThanPeriod_DroppedNeverThrows()
        {
            var document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "", 20, 24, ""))
                .Build();

            LiveEventCalendarCompilation compilation = null;
            Assert.DoesNotThrow(() => compilation = LiveEventCalendarCompiler.Compile(document));

            compilation.TryGetRecurringOutcome("sky-race", out LiveEventCalendarEntryOutcome outcome);
            Assert.IsFalse(outcome.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.InvalidRecurringRule, outcome.DropReason);
            Assert.AreEqual(0, compilation.RecurringCalendars.Count);
        }

        [Test]
        public void InvalidRecurring_PrefixHash_DroppedNeverThrows()
        {
            // R-4: RecurringLiveEventCalendar ném từ GetInstances khi idPrefix có '#' — bộ biên dịch phải kiểm trước.
            var document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "sky#race-", 24, 20, ""))
                .Build();

            LiveEventCalendarCompilation compilation = null;
            Assert.DoesNotThrow(() => compilation = LiveEventCalendarCompiler.Compile(document));

            compilation.TryGetRecurringOutcome("sky-race", out LiveEventCalendarEntryOutcome outcome);
            Assert.IsFalse(outcome.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.InvalidRecurringRule, outcome.DropReason);
        }

        [Test]
        public void InvalidRecurring_SecondRuleSameType_DroppedNeverThrows()
        {
            var document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "first-", 24, 20, ""))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-06T00:00:00Z", "second-", 24, 20, ""))
                .Build();

            LiveEventCalendarCompilation compilation = null;
            Assert.DoesNotThrow(() => compilation = LiveEventCalendarCompiler.Compile(document));

            Assert.AreEqual(1, compilation.RecurringCalendars.Count);
            Assert.AreEqual("first-", compilation.RecurringCalendars[0].IdPrefix);
            Assert.IsTrue(compilation.Entries[0].IsKept);
            Assert.IsFalse(compilation.Entries[1].IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.DuplicateRecurringType, compilation.Entries[1].DropReason);
        }

        [Test]
        public void InvalidRecurring_PeriodZero_DroppedNeverThrows()
        {
            var document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "", 0, 0, ""))
                .Build();

            LiveEventCalendarCompilation compilation = null;
            Assert.DoesNotThrow(() => compilation = LiveEventCalendarCompiler.Compile(document));

            compilation.TryGetRecurringOutcome("sky-race", out LiveEventCalendarEntryOutcome outcome);
            Assert.IsFalse(outcome.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.InvalidRecurringRule, outcome.DropReason);
            Assert.AreEqual("Luật lặp thứ 1 ('sky-race'): chu kỳ phải > 0 giờ — bỏ qua.", outcome.ProblemText);
            Assert.AreEqual(0, compilation.RecurringCalendars.Count);
        }

        [Test]
        public void InvalidRecurring_AnchorUnreadable_DroppedNeverThrows()
        {
            var document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "5/1/2026", "", 24, 20, ""))
                .Build();

            LiveEventCalendarCompilation compilation = null;
            Assert.DoesNotThrow(() => compilation = LiveEventCalendarCompiler.Compile(document));

            compilation.TryGetRecurringOutcome("sky-race", out LiveEventCalendarEntryOutcome outcome);
            Assert.IsFalse(outcome.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.InvalidRecurringRule, outcome.DropReason);
            Assert.AreEqual(1, compilation.Problems.Count, string.Join("\n", compilation.Problems));
            Assert.AreEqual(0, compilation.RecurringCalendars.Count);
        }

        [TestCase(int.MaxValue, 1)]
        [TestCase(300000000, 300000000)]
        [TestCase(300000000, 1)]
        public void InvalidRecurring_HoursBeyondDateTimeRange_DroppedNeverThrows(int periodHours, int activeHours)
        {
            // JSON remote xấu: TimeSpan.FromHours(300000000) ném OverflowException — bộ biên dịch hứa không ném.
            var document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "", periodHours, activeHours, ""))
                .Build();

            LiveEventCalendarCompilation compilation = null;
            Assert.DoesNotThrow(() => compilation = LiveEventCalendarCompiler.Compile(document));

            compilation.TryGetRecurringOutcome("sky-race", out LiveEventCalendarEntryOutcome outcome);
            Assert.IsFalse(outcome.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.InvalidRecurringRule, outcome.DropReason);
            Assert.AreEqual(0, compilation.RecurringCalendars.Count);
        }

        [Test]
        public void LongestAllowedPeriod_CompilesAndQueriesNeverThrow()
        {
            // Chu kỳ dài nhất còn nhận (= cả khoảng DateTime tính bằng giờ): phép chia khung của GetInstancesInRange không
            // được tràn long (Period.Ticks × 256).
            int longestPeriodHours = (int)(DateTime.MaxValue.Ticks / TimeSpan.TicksPerHour);
            var document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "", longestPeriodHours, 24, ""))
                .Build();

            LiveEventCalendarCompilation compilation = null;
            Assert.DoesNotThrow(() => compilation = LiveEventCalendarCompiler.Compile(document));
            Assert.AreEqual(1, compilation.RecurringCalendars.Count);
            Assert.DoesNotThrow(() => compilation.GetInstancesInRange("sky-race", DateTime.MinValue, DateTime.MaxValue));
            Assert.DoesNotThrow(() => compilation.TryFindInstanceById("sky-race", "sky-race-0", LiveOpsDesignSample.NowUtc, out _));
        }

        // ----- Thứ tự xuất (V-6) -----

        [Test]
        public void ExportOrder_StableByStart_UnreadableLast_Idempotent()
        {
            var document = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("k-c", "c", "t", "2026-03-01T00:00:00Z", "2026-03-02T00:00:00Z", ""))
                .WithFixedEvent(new FixedLiveEventEntry("k-unreadable-1", "u1", "t", "not-a-time", "2026-01-02T00:00:00Z", ""))
                .WithFixedEvent(new FixedLiveEventEntry("k-a", "a", "t", "2026-01-01T00:00:00Z", "2026-01-02T00:00:00Z", ""))
                .WithFixedEvent(new FixedLiveEventEntry("k-unreadable-2", "u2", "t", "also-not-a-time", "2026-01-02T00:00:00Z", ""))
                .WithFixedEvent(new FixedLiveEventEntry("k-b", "b", "t", "2026-02-01T00:00:00Z", "2026-02-02T00:00:00Z", ""))
                .Build();

            LiveEventCalendarDocument exported = LiveEventCalendarExportOrder.Apply(document);

            CollectionAssert.AreEqual(new[] { "k-a", "k-b", "k-c", "k-unreadable-1", "k-unreadable-2" },
                Keys(exported.FixedEvents), "Đọc được sắp theo start tăng dần; không đọc được xếp cuối theo thứ tự asset.");

            Assert.IsTrue(LiveEventCalendarExportOrder.IsInExportOrder(exported));
            Assert.IsFalse(LiveEventCalendarExportOrder.IsInExportOrder(document));

            LiveEventCalendarDocument appliedTwice = LiveEventCalendarExportOrder.Apply(exported);
            CollectionAssert.AreEqual(Keys(exported.FixedEvents), Keys(appliedTwice.FixedEvents), "Apply(Apply(d)) == Apply(d).");
        }

        [Test]
        public void ExportOrder_RecurringKeepsAssetOrder()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            LiveEventCalendarDocument exported = LiveEventCalendarExportOrder.Apply(document);

            Assert.AreEqual(document.RecurringRules.Count, exported.RecurringRules.Count);
            for (int index = 0; index < document.RecurringRules.Count; index++)
            {
                Assert.AreEqual(document.RecurringRules[index].EventType, exported.RecurringRules[index].EventType,
                    "Đưa làn lên/xuống không được đổi thứ tự luật trong JSON (mục 5.2 luật 7).");
            }
        }

        [Test]
        public void CompileInExportOrder_DuplicateIdKeepsEarlierStartNotAssetOrder()
        {
            var lateStartFirstInAsset = new FixedLiveEventEntry("entry-late", "dup", "t", "2026-01-10T00:00:00Z", "2026-01-11T00:00:00Z", "");
            var earlyStartSecondInAsset = new FixedLiveEventEntry("entry-early", "dup", "t", "2026-01-01T00:00:00Z", "2026-01-02T00:00:00Z", "");
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(lateStartFirstInAsset)
                .WithFixedEvent(earlyStartSecondInAsset)
                .Build();

            LiveEventCalendarCompilation byAssetOrder = LiveEventCalendarCompiler.Compile(document);
            byAssetOrder.TryGetFixedOutcome("entry-late", out LiveEventCalendarEntryOutcome lateByAsset);
            byAssetOrder.TryGetFixedOutcome("entry-early", out LiveEventCalendarEntryOutcome earlyByAsset);
            Assert.IsTrue(lateByAsset.IsKept, "Thứ tự asset: đợt đứng trước (start muộn hơn) được giữ.");
            Assert.IsFalse(earlyByAsset.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.DuplicateEventId, earlyByAsset.DropReason);

            LiveEventCalendarCompilation byExportOrder = LiveEventCalendarCompiler.CompileInExportOrder(document);
            byExportOrder.TryGetFixedOutcome("entry-late", out LiveEventCalendarEntryOutcome lateByExport);
            byExportOrder.TryGetFixedOutcome("entry-early", out LiveEventCalendarEntryOutcome earlyByExport);
            Assert.IsTrue(earlyByExport.IsKept, "Thứ tự xuất: đợt bắt đầu sớm hơn đứng trước trong JSON nên được giữ (V-6).");
            Assert.IsFalse(lateByExport.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.DuplicateEventId, lateByExport.DropReason);
        }

        // ----- Composite: lịch chạy được -----

        [Test]
        public void Compile_CalendarMatchesFixedPlusRecurringQueries_OverOneYear()
        {
            // Mẫu thiết kế + đợt cố định cho hai loại CÓ luật lặp, để mỗi loại rơi vào đúng một trong ba ca: chỉ luật
            // (không đợt), chỉ đợt (không luật), cả luật và đợt (một đợt bị che vì chồng giờ, một đợt nằm trong khe nghỉ
            // của luật nên được giữ). sky-race chạy 00:00–20:00 mỗi ngày, nghỉ 20:00–24:00.
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.Document)
                .WithFixedEvent(new FixedLiveEventEntry("entry-weekly-pass-special", "weekly-pass-special", "weekly-pass",
                    "2026-05-01T00:00:00Z", "2026-05-02T00:00:00Z", ""))
                .WithFixedEvent(new FixedLiveEventEntry("entry-sky-night", "sky-night", "sky-race",
                    "2026-03-10T20:00:00Z", "2026-03-10T23:00:00Z", ""))
                .WithFixedEvent(new FixedLiveEventEntry("entry-sky-overlap", "sky-overlap", "sky-race",
                    "2026-04-01T19:00:00Z", "2026-04-01T21:00:00Z", ""))
                .Build();
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.CompileInExportOrder(document);

            Assert.IsTrue(compilation.TryGetFixedOutcome("entry-sky-night", out LiveEventCalendarEntryOutcome skyNight));
            Assert.IsTrue(skyNight.IsKept, "Đợt nằm trong khe nghỉ của luật không bị che.");
            Assert.IsTrue(compilation.TryGetFixedOutcome("entry-sky-overlap", out LiveEventCalendarEntryOutcome skyOverlap));
            Assert.AreEqual(LiveEventCalendarDropReason.ShadowedByRecurring, skyOverlap.DropReason);
            Assert.IsTrue(compilation.TryGetFixedOutcome("entry-weekly-pass-special", out LiveEventCalendarEntryOutcome weeklySpecial));
            Assert.AreEqual(LiveEventCalendarDropReason.ShadowedByRecurring, weeklySpecial.DropReason);

            var keptFixedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < compilation.Entries.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = compilation.Entries[index];
                if (outcome.Kind == LiveEventCalendarEntryKind.FixedEvent && outcome.IsKept) keptFixedIds.Add(outcome.EventId);
            }

            var recurringCalendarByType = new Dictionary<string, RecurringLiveEventCalendar>(StringComparer.Ordinal);
            for (int index = 0; index < compilation.RecurringCalendars.Count; index++)
            {
                recurringCalendarByType[compilation.RecurringCalendars[index].EventType] = compilation.RecurringCalendars[index];
            }

            DateTime yearStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime yearEnd = yearStart.AddYears(1);
            int comparedWindowCount = 0;
            for (int typeIndex = 0; typeIndex < document.EventTypes.Count; typeIndex++)
            {
                string eventType = document.EventTypes[typeIndex].TypeId;
                for (DateTime hourStart = yearStart; hourStart < yearEnd; hourStart = hourStart.AddHours(1))
                {
                    DateTime hourEnd = hourStart.AddHours(1);
                    // Kỳ vọng dựng từ các phần rời + outcome: mọi lần lặp của luật cùng loại, cộng đợt cố định mà bộ biên dịch
                    // báo IsKept — nếu IsKept nói sai (vd không đánh dấu đợt bị che), Calendar ghép sẽ lệch ở giờ đó.
                    var expected = new List<LiveEventInstance>();
                    if (recurringCalendarByType.TryGetValue(eventType, out RecurringLiveEventCalendar recurringCalendar))
                    {
                        expected.AddRange(recurringCalendar.GetInstances(eventType, hourStart, hourEnd));
                    }
                    IReadOnlyList<LiveEventInstance> fixedInstances = compilation.FixedCalendar.GetInstances(eventType, hourStart, hourEnd);
                    for (int index = 0; index < fixedInstances.Count; index++)
                    {
                        if (keptFixedIds.Contains(fixedInstances[index].EventId)) expected.Add(fixedInstances[index]);
                    }

                    IReadOnlyList<LiveEventInstance> actual = compilation.Calendar.GetInstances(eventType, hourStart, hourEnd);
                    CollectionAssert.AreEqual(SortedIds(expected), SortedIds(actual), eventType + " lúc " + LiveEventUtcText.Format(hourStart));
                    comparedWindowCount++;
                }
            }
            Assert.AreEqual(5 * 365 * 24, comparedWindowCount, "Mỗi giờ của năm 2026 cho cả 5 loại.");
        }

        [Test]
        public void GetInstancesInRange_HourlyRuleOver31Days_Returns744()
        {
            var document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("hourly", "2026-01-01T00:00:00Z", "hourly-", 1, 1, ""))
                .Build();
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.Compile(document);

            DateTime from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime to = from.AddDays(31);

            IReadOnlyList<LiveEventInstance> instances = compilation.GetInstancesInRange("hourly", from, to);

            Assert.AreEqual(744, instances.Count, "31 ngày × 24 giờ, không bị cắt ở MaximumInstancesPerQuery (512).");
            for (int index = 1; index < instances.Count; index++)
            {
                Assert.Less(instances[index - 1].StartUtc, instances[index].StartUtc, "Kết quả phải sắp theo giờ bắt đầu.");
            }
        }

        [Test]
        public void GetInstancesInRange_FixedOnlyType_NotCapped_DelegatesDirectly()
        {
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.CompileInExportOrder(LiveOpsDesignSample.Document);
            IReadOnlyList<LiveEventInstance> instances = compilation.GetInstancesInRange("lava-quest",
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.AreEqual(2, instances.Count, "lava-quest-2026-09a + 09b kept; lava-quest-2026-10 hỏng endUtc nên không vào tới FixedCalendar.");
        }

        [Test]
        public void TryFindInstanceById_FindsRecurringOccurrence()
        {
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.CompileInExportOrder(LiveOpsDesignSample.Document);

            bool found = compilation.TryFindInstanceById("weekly-pass", "pass-35", LiveOpsDesignSample.NowUtc, out LiveEventInstance instance);

            Assert.IsTrue(found);
            Assert.AreEqual("pass-35", instance.EventId);
        }

        /// <summary>Đợt chưa có EntryKey thật — <see cref="FixedOnly"/> gán "entry-" + vị trí khi dựng tài liệu.</summary>
        private static FixedLiveEventEntry Entry(string eventId, string eventType, string startUtcText, string endUtcText)
        {
            return new FixedLiveEventEntry("pending", eventId, eventType, startUtcText, endUtcText, "");
        }

        /// <summary>Tài liệu chỉ có đợt cố định; EntryKey = "entry-" + vị trí để test tra outcome theo vị trí gốc.</summary>
        private static LiveEventCalendarDocument FixedOnly(params FixedLiveEventEntry[] entries)
        {
            var builder = new LiveEventCalendarDocumentBuilder();
            for (int index = 0; index < entries.Length; index++)
            {
                FixedLiveEventEntry entry = entries[index];
                builder.WithFixedEvent(new FixedLiveEventEntry("entry-" + index.ToString(CultureInfo.InvariantCulture), entry.EventId,
                    entry.EventType, entry.StartUtcText, entry.EndUtcText, entry.ConfigKey));
            }
            return builder.Build();
        }

        private static DateTime Utc(int septemberDay)
        {
            return new DateTime(2026, 9, septemberDay, 0, 0, 0, DateTimeKind.Utc);
        }

        private static void AssertSameAsInstanceMessage(LiveEventCalendarDocument document, string entryKey, TestDelegate construct)
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(construct);
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.Compile(document);
            Assert.IsTrue(compilation.TryGetFixedOutcome(entryKey, out LiveEventCalendarEntryOutcome outcome));
            string expected = "Mục thứ " + (outcome.SourceIndex + 1).ToString(CultureInfo.InvariantCulture) + ": " + RuntimeParameterSuffix.Replace(exception.Message, string.Empty) +
                              " — bỏ qua.";
            Assert.AreEqual(expected, outcome.ProblemText);
        }

        private static List<string> Keys(IReadOnlyList<FixedLiveEventEntry> entries)
        {
            var keys = new List<string>(entries.Count);
            for (int index = 0; index < entries.Count; index++) keys.Add(entries[index].EntryKey);
            return keys;
        }

        private static List<string> SortedIds(IReadOnlyList<LiveEventInstance> instances)
        {
            var ordered = new List<LiveEventInstance>(instances);
            ordered.Sort((left, right) =>
            {
                int byStart = left.StartUtc.CompareTo(right.StartUtc);
                return byStart != 0 ? byStart : string.CompareOrdinal(left.EventId, right.EventId);
            });
            return EventIds(ordered);
        }

        private static List<string> EventIds(IReadOnlyList<LiveEventInstance> instances)
        {
            var ids = new List<string>(instances.Count);
            for (int index = 0; index < instances.Count; index++) ids.Add(instances[index].EventId);
            return ids;
        }
    }
}
