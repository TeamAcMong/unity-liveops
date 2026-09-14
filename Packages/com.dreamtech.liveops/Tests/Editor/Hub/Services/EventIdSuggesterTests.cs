using System;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>PD-20 — id đợt trong fixture theo mẫu thiết kế (lava-quest-2026-09a/09b/10, hunt-0914, hunt-0916-bonus).</summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class EventIdSuggesterTests
    {
        private const string LavaQuest = "lava-quest";
        private const string TreasureHunt = "treasure-hunt";

        private static int _entryCounter;

        private static FixedLiveEventEntry Entry(string eventId, string eventType, string startUtc, string endUtc)
        {
            _entryCounter++;
            return new FixedLiveEventEntry("entry-" + _entryCounter, eventId, eventType, startUtc, endUtc, "");
        }

        private static LiveEventCalendarDocument DesignLikeDocument(params FixedLiveEventEntry[] extraEntries)
        {
            var builder = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(Entry("lava-quest-2026-09a", LavaQuest, "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z"))
                .WithFixedEvent(Entry("lava-quest-2026-09b", LavaQuest, "2026-09-17T00:00:00Z", "2026-09-20T00:00:00Z"))
                .WithFixedEvent(Entry("lava-quest-2026-10", LavaQuest, "2026-10-01T00:00:00Z", "2026-10-3"))
                .WithFixedEvent(Entry("hunt-0914", TreasureHunt, "2026-09-14T00:00:00Z", "2026-09-17T00:00:00Z"))
                .WithFixedEvent(Entry("hunt-0916-bonus", TreasureHunt, "2026-09-16T12:00:00Z", "2026-09-18T00:00:00Z"));
            for (int index = 0; index < extraEntries.Length; index++) builder.WithFixedEvent(extraEntries[index]);
            return builder.Build();
        }

        private static DateTime Utc(int month, int day) => new DateTime(2026, month, day, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void EventIdSuggester_LetterSuffix()
        {
            // Nhân bản 09b (thiết kế: điền sẵn lava-quest-2026-09c).
            string suggestion = LiveOpsEventIdSuggester.Suggest(DesignLikeDocument(), LavaQuest, Utc(9, 24), "lava-quest-2026-09b");
            Assert.AreEqual("lava-quest-2026-09c", suggestion);
        }

        [Test]
        public void EventIdSuggester_NumericSuffix()
        {
            string suggestion = LiveOpsEventIdSuggester.Suggest(DesignLikeDocument(), LavaQuest, Utc(11, 1), "lava-quest-2026-10");
            Assert.AreEqual("lava-quest-2026-11", suggestion);
        }

        [Test]
        public void EventIdSuggester_MonthDayPattern()
        {
            // Nhân bản hunt-0914 sang 21/9 (thiết kế: id đề xuất hunt-0921) — hậu tố -MMdd theo ngày bắt đầu mới, không +1.
            string suggestion = LiveOpsEventIdSuggester.Suggest(DesignLikeDocument(), TreasureHunt, new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc), "hunt-0914");
            Assert.AreEqual("hunt-0921", suggestion);
        }

        [Test]
        public void EventIdSuggester_Fallback()
        {
            string suggestion = LiveOpsEventIdSuggester.Suggest(DesignLikeDocument(), TreasureHunt, Utc(9, 23), "hunt-0916-bonus");
            Assert.AreEqual("hunt-0916-bonus-2", suggestion);
        }

        [Test]
        public void EventIdSuggester_SkipsExisting()
        {
            LiveEventCalendarDocument document = DesignLikeDocument(
                Entry("lava-quest-2026-09c", LavaQuest, "2026-09-24T00:00:00Z", "2026-09-27T00:00:00Z"),
                Entry("lava-quest-2026-11", LavaQuest, "2026-11-01T00:00:00Z", "2026-11-03T00:00:00Z"),
                Entry("hunt-0916-bonus-2", TreasureHunt, "2026-09-23T00:00:00Z", "2026-09-24T00:00:00Z"),
                Entry("hunt-0921", TreasureHunt, "2026-09-21T00:00:00Z", "2026-09-22T00:00:00Z"));

            Assert.AreEqual("lava-quest-2026-09d", LiveOpsEventIdSuggester.Suggest(document, LavaQuest, Utc(10, 1), "lava-quest-2026-09b"));
            Assert.AreEqual("lava-quest-2026-12", LiveOpsEventIdSuggester.Suggest(document, LavaQuest, Utc(12, 1), "lava-quest-2026-10"));
            Assert.AreEqual("hunt-0916-bonus-3", LiveOpsEventIdSuggester.Suggest(document, TreasureHunt, Utc(9, 30), "hunt-0916-bonus"));
            Assert.AreEqual("hunt-0921-2", LiveOpsEventIdSuggester.Suggest(document, TreasureHunt, Utc(9, 21), "hunt-0914"));
        }

        [Test]
        public void NoBasedOn_UsesNearestSameTypeByStart_NotDocumentOrder()
        {
            // Bắt đầu 18/9: 09b (17/9) gần hơn 09a (10/9) dù 09a đứng trước trong tài liệu.
            Assert.AreEqual("lava-quest-2026-09c", LiveOpsEventIdSuggester.Suggest(DesignLikeDocument(), LavaQuest, Utc(9, 18), null));
            // Bắt đầu 13/9: hunt-0914 (14/9) gần hơn hunt-0916-bonus → mẫu -MMdd theo ngày mới.
            Assert.AreEqual("hunt-0913", LiveOpsEventIdSuggester.Suggest(DesignLikeDocument(), TreasureHunt, Utc(9, 13), ""));
        }

        [Test]
        public void NoEventOfType_UsesTypeAndMonthDay()
        {
            Assert.AreEqual("star-tournament-1003", LiveOpsEventIdSuggester.Suggest(DesignLikeDocument(), "star-tournament", Utc(10, 3), null));
        }

        [Test]
        public void NumericSuffix_KeepsWidthAndCarries()
        {
            LiveEventCalendarDocument empty = LiveEventCalendarDocument.Empty;
            Assert.AreEqual("season-10", LiveOpsEventIdSuggester.Suggest(empty, "season", Utc(1, 1), "season-09"));
            Assert.AreEqual("season-100", LiveOpsEventIdSuggester.Suggest(empty, "season", Utc(1, 1), "season-99"));
            // 9999 không phải tháng/ngày hợp lệ → số thứ tự.
            Assert.AreEqual("event-10000", LiveOpsEventIdSuggester.Suggest(empty, "event", Utc(1, 1), "event-9999"));
        }

        [Test]
        public void LetterZ_FallsBackToCounter()
        {
            Assert.AreEqual("pass-09z-2", LiveOpsEventIdSuggester.Suggest(LiveEventCalendarDocument.Empty, "pass", Utc(1, 1), "pass-09z"));
        }
    }
}
