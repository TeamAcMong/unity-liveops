using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public class LiveEventCalendarTests
    {
        private const string RaceType = LiveOpsTestFactory.RaceType;
        private static readonly DateTime Anchor = LiveOpsTestFactory.Anchor;

        [Test]
        public void Instance_PhaseBoundaries_StartIsActive_EndIsEnded()
        {
            var instance = new LiveEventInstance("e1", "type", Anchor, Anchor.AddDays(1));

            Assert.AreEqual(LiveEventPhase.Upcoming, instance.PhaseAt(Anchor.AddTicks(-1)));
            Assert.AreEqual(LiveEventPhase.Active, instance.PhaseAt(Anchor));
            Assert.AreEqual(LiveEventPhase.Active, instance.PhaseAt(Anchor.AddDays(1).AddTicks(-1)));
            Assert.AreEqual(LiveEventPhase.Ended, instance.PhaseAt(Anchor.AddDays(1)));
            Assert.AreEqual(TimeSpan.FromHours(18), instance.TimeLeft(Anchor.AddHours(6)));
            Assert.AreEqual(TimeSpan.Zero, instance.TimeLeft(Anchor.AddDays(2)));
            Assert.AreEqual(TimeSpan.FromHours(1), instance.TimeUntilStart(Anchor.AddHours(-1)));
        }

        [Test]
        public void Instance_RejectsInvalidInput()
        {
            Assert.Throws<ArgumentException>(() => new LiveEventInstance("", "type", Anchor, Anchor.AddDays(1)));
            Assert.Throws<ArgumentException>(() => new LiveEventInstance("e1", "type", Anchor, Anchor));
            Assert.Throws<ArgumentException>(() => new LiveEventInstance("e#1", "type", Anchor, Anchor.AddDays(1)), "'#' dành cho grant id");
        }

        [Test]
        public void Recurring_IdsAreDeterministic_NegativeBeforeAnchor()
        {
            RecurringLiveEventCalendar calendar = LiveOpsTestFactory.CreateRaceCalendar();

            Assert.AreEqual("sky-race-0", calendar.GetInstances(RaceType, Anchor.AddHours(1), Anchor.AddHours(2))[0].EventId);
            Assert.AreEqual("sky-race-2", calendar.GetOccurrence(2).EventId);
            Assert.AreEqual(Anchor.AddDays(14), calendar.GetOccurrence(2).StartUtc);
            Assert.AreEqual(-1, calendar.OccurrenceIndexAt(Anchor.AddTicks(-1)));
            Assert.AreEqual("sky-race--1", calendar.GetOccurrence(-1).EventId);
            Assert.AreEqual(calendar.GetOccurrence(5), LiveOpsTestFactory.CreateRaceCalendar().GetOccurrence(5),
                            "Dựng lại lịch (cài lại app, máy khác) phải ra đúng đợt đó");
        }

        [Test]
        public void Recurring_QueryInsideCooldownGap_ReturnsOnlyNextOccurrence()
        {
            // Đợt 0 chạy [ngày 0, ngày 3), nghỉ tới ngày 7.
            IReadOnlyList<LiveEventInstance> instances =
                LiveOpsTestFactory.CreateRaceCalendar().GetInstances(RaceType, Anchor.AddDays(4), Anchor.AddDays(8));

            CollectionAssert.AreEqual(new[] { "sky-race-1" }, Ids(instances));
        }

        [Test]
        public void Recurring_WideQuery_ReturnsOrderedOccurrences_OtherTypeEmpty()
        {
            RecurringLiveEventCalendar calendar = LiveOpsTestFactory.CreateRaceCalendar();

            CollectionAssert.AreEqual(new[] { "sky-race-0", "sky-race-1", "sky-race-2", "sky-race-3" },
                                      Ids(calendar.GetInstances(RaceType, Anchor.AddDays(1), Anchor.AddDays(22))));
            Assert.AreEqual(0, calendar.GetInstances("other", Anchor, Anchor.AddDays(30)).Count);
        }

        [Test]
        public void Recurring_RejectsActiveDurationLongerThanPeriod()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new RecurringLiveEventCalendar(RaceType, Anchor, TimeSpan.FromDays(1), TimeSpan.FromDays(2)));
        }

        [Test]
        public void Fixed_DropsDuplicateAndOverlapping_ReportsEachProblem()
        {
            var calendar = new FixedLiveEventCalendar(new[]
            {
                new LiveEventInstance("b", "hunt", Anchor.AddDays(2), Anchor.AddDays(4)),
                new LiveEventInstance("a", "hunt", Anchor, Anchor.AddDays(3)),
                new LiveEventInstance("a", "hunt", Anchor.AddDays(10), Anchor.AddDays(11)),
                null,
                // Bắt đầu đúng lúc "a" kết thúc: chạm mép, không chồng.
                new LiveEventInstance("c", "hunt", Anchor.AddDays(3), Anchor.AddDays(5)),
            });

            CollectionAssert.AreEqual(new[] { "a", "c" }, Ids(calendar.Instances), "Giữ đợt bắt đầu sớm hơn, bỏ 'b' chồng giờ");
            Assert.AreEqual(3, calendar.Problems.Count, string.Join("\n", calendar.Problems));
        }

        [Test]
        public void Fixed_AllowsOverlapAcrossTypes()
        {
            var calendar = new FixedLiveEventCalendar(new[]
            {
                new LiveEventInstance("race-x", "race", Anchor, Anchor.AddDays(3)),
                new LiveEventInstance("hunt-x", "hunt", Anchor.AddDays(1), Anchor.AddDays(2)),
            });

            Assert.IsFalse(calendar.HasProblems);
            Assert.AreEqual(1, calendar.GetInstances("hunt", Anchor, Anchor.AddDays(5)).Count);
        }

        [Test]
        public void Composite_EarlierCalendarWins_SameAnswerForAnyWindow()
        {
            var special = new FixedLiveEventCalendar(new[]
            {
                // Chồng lên đợt lặp 1 (ngày 7–10) → bị bỏ.
                new LiveEventInstance("race-special", RaceType, Anchor.AddDays(5), Anchor.AddDays(8)),
                // Nằm gọn trong lúc nghỉ sau đợt 0 → được giữ.
                new LiveEventInstance("race-bonus", RaceType, Anchor.AddDays(3), Anchor.AddDays(5)),
            });
            var composite = new CompositeLiveEventCalendar(LiveOpsTestFactory.CreateRaceCalendar(), special);

            CollectionAssert.AreEqual(new[] { "sky-race-0", "race-bonus", "sky-race-1" },
                                      Ids(composite.GetInstances(RaceType, Anchor, Anchor.AddDays(9))));
            CollectionAssert.AreEqual(new[] { "race-bonus" }, Ids(composite.GetInstances(RaceType, Anchor.AddDays(4), Anchor.AddDays(6))),
                                      "Khung [4, 6) không chạm đợt lặp nào, nhưng race-special vẫn phải bị bỏ");
            CollectionAssert.AreEquivalent(new[] { RaceType }, composite.EventTypes);
        }

        private static string[] Ids(IEnumerable<LiveEventInstance> instances)
        {
            var ids = new List<string>();
            foreach (LiveEventInstance instance in instances) ids.Add(instance.EventId);
            return ids.ToArray();
        }
    }
}
