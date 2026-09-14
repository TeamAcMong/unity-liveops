using System;
using System.Collections.Generic;
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
        public void Compiler_RecurringRule_ActiveLongerThanPeriod_Dropped()
        {
            var document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "", 20, 24, ""))
                .Build();

            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.Compile(document);

            compilation.TryGetRecurringOutcome("sky-race", out LiveEventCalendarEntryOutcome outcome);
            Assert.IsFalse(outcome.IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.InvalidRecurringRule, outcome.DropReason);
            Assert.AreEqual(0, compilation.RecurringCalendars.Count);
        }

        [Test]
        public void Compiler_RecurringRule_PrefixWithHash_DroppedNeverThrows()
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
        public void Compiler_DuplicateRecurringType_KeepsFirst()
        {
            var document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "first-", 24, 20, ""))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-06T00:00:00Z", "second-", 24, 20, ""))
                .Build();

            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.Compile(document);

            Assert.AreEqual(1, compilation.RecurringCalendars.Count);
            Assert.AreEqual("first-", compilation.RecurringCalendars[0].IdPrefix);
            Assert.IsTrue(compilation.Entries[0].IsKept);
            Assert.IsFalse(compilation.Entries[1].IsKept);
            Assert.AreEqual(LiveEventCalendarDropReason.DuplicateRecurringType, compilation.Entries[1].DropReason);
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
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.CompileInExportOrder(LiveOpsDesignSample.Document);
            DateTime yearStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime yearEnd = yearStart.AddYears(1);

            // sky-race: chỉ có luật lặp, không có đợt cố định cùng loại — Composite phải khớp thẳng lịch lặp.
            IReadOnlyList<LiveEventInstance> skyRaceViaCalendar = compilation.Calendar.GetInstances("sky-race", yearStart, yearEnd);
            RecurringLiveEventCalendar skyRaceCalendar = compilation.RecurringCalendars[1];
            IReadOnlyList<LiveEventInstance> skyRaceDirect = skyRaceCalendar.GetInstances("sky-race", yearStart, yearEnd);
            CollectionAssert.AreEqual(EventIds(skyRaceDirect), EventIds(skyRaceViaCalendar));

            // lava-quest: chỉ có đợt cố định, không có luật lặp — Composite phải khớp thẳng FixedCalendar.
            IReadOnlyList<LiveEventInstance> lavaQuestViaCalendar = compilation.Calendar.GetInstances("lava-quest", yearStart, yearEnd);
            IReadOnlyList<LiveEventInstance> lavaQuestDirect = compilation.FixedCalendar.GetInstances("lava-quest", yearStart, yearEnd);
            CollectionAssert.AreEqual(EventIds(lavaQuestDirect), EventIds(lavaQuestViaCalendar));
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

        private static List<string> Keys(IReadOnlyList<FixedLiveEventEntry> entries)
        {
            var keys = new List<string>(entries.Count);
            for (int index = 0; index < entries.Count; index++) keys.Add(entries[index].EntryKey);
            return keys;
        }

        private static List<string> EventIds(IReadOnlyList<LiveEventInstance> instances)
        {
            var ids = new List<string>(instances.Count);
            for (int index = 0; index < instances.Count; index++) ids.Add(instances[index].EventId);
            return ids;
        }
    }
}
