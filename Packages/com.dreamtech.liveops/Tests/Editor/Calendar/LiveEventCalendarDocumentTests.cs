using System;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class LiveEventCalendarDocumentTests
    {
        [Test]
        public void Empty_HasNoItems()
        {
            LiveEventCalendarDocument empty = LiveEventCalendarDocument.Empty;

            Assert.AreEqual(0, empty.EventTypes.Count);
            Assert.AreEqual(0, empty.RecurringRules.Count);
            Assert.AreEqual(0, empty.FixedEvents.Count);
            Assert.AreEqual(0, empty.PublishedStamps.Count);
            Assert.AreEqual(0, empty.IgnoredWarnings.Count);
            Assert.IsNull(empty.LatestStamp);
            Assert.AreEqual(LiveEventCalendarDocument.DefaultRemoteConfigKey, empty.RemoteConfigKey);
        }

        [Test]
        public void DesignSample_Has5Types_2Rules_6FixedEvents()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;

            Assert.AreEqual(5, document.EventTypes.Count);
            Assert.AreEqual(2, document.RecurringRules.Count);
            Assert.AreEqual(6, document.FixedEvents.Count);
            Assert.AreEqual(1, document.PublishedStamps.Count);
            Assert.IsNotNull(document.LatestStamp);
        }

        [Test]
        public void Builder_DuplicateTypeId_Throws()
        {
            var builder = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("lava-quest", "A", 0, false, ""));

            Assert.Throws<ArgumentException>(() => builder.WithEventType(new LiveEventTypeDefinition("lava-quest", "B", 1, false, "")));
        }

        [Test]
        public void Builder_DuplicateRecurringType_DoesNotThrow_ValidatorReportsLater()
        {
            var builder = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "", 24, 20, ""))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-06T00:00:00Z", "", 24, 20, ""));

            LiveEventCalendarDocument document = builder.Build();

            Assert.AreEqual(2, document.RecurringRules.Count);
        }

        [Test]
        public void TryGetFixedEvent_UnknownEntryKey_ReturnsFalse()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            Assert.IsFalse(document.TryGetFixedEvent("no-such-key", out FixedLiveEventEntry entry));
            Assert.IsNull(entry);
        }

        [Test]
        public void IndexOfFixedEvent_ReturnsAssetPosition()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            Assert.AreEqual(0, document.IndexOfFixedEvent(LiveOpsDesignSample.LavaQuestEarlyEntryKey));
            Assert.AreEqual(5, document.IndexOfFixedEvent(LiveOpsDesignSample.StarTournamentEntryKey));
            Assert.AreEqual(-1, document.IndexOfFixedEvent("missing"));
        }

        [Test]
        public void EffectiveConfigKeyOf_FixedEvent_InheritsTypeDefault_WhenOwnKeyEmpty()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            document.TryGetFixedEvent(LiveOpsDesignSample.HuntEarlyEntryKey, out FixedLiveEventEntry huntEarly);

            Assert.IsFalse(huntEarly.HasOwnConfigKey);
            Assert.AreEqual("hunt_default", document.EffectiveConfigKeyOf(huntEarly));
        }

        [Test]
        public void EffectiveConfigKeyOf_FixedEvent_PrefersOwnKey()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            document.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey, out FixedLiveEventEntry huntBonus);

            Assert.IsTrue(huntBonus.HasOwnConfigKey);
            Assert.AreEqual("hunt_bonus", document.EffectiveConfigKeyOf(huntBonus));
        }

        [Test]
        public void EffectiveConfigKeyOf_RecurringRule_InheritsTypeDefault()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("sky-race", "Đua trên trời", 4, false, "sky_race_v4"))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "", 24, 20, ""))
                .Build();
            document.TryGetRecurringRule("sky-race", out RecurringLiveEventRule rule);

            Assert.AreEqual("sky_race_v4", document.EffectiveConfigKeyOf(rule));
        }

        [Test]
        public void RecurringRule_EffectiveIdPrefix_DefaultsToTypeDash()
        {
            var rule = new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "", 24, 20, "");
            Assert.AreEqual("sky-race-", rule.EffectiveIdPrefix);
        }

        [Test]
        public void FixedLiveEventEntry_CreateEntryKey_Is32HexChars()
        {
            string key = FixedLiveEventEntry.CreateEntryKey();
            Assert.AreEqual(32, key.Length);
        }

        [Test]
        public void IgnoredCalendarWarning_ReminderExpiry()
        {
            var reminder = new IgnoredCalendarWarning("long-gap-between-events", "lava-quest", "", "", "note",
                "2026-09-14T00:00:00Z");
            var permanent = new IgnoredCalendarWarning("long-gap-between-events", "lava-quest", "", "", "note", "");

            Assert.IsTrue(reminder.IsReminder);
            Assert.IsFalse(reminder.IsExpiredAt(new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Utc)));
            Assert.IsTrue(reminder.IsExpiredAt(new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc)));
            Assert.IsFalse(permanent.IsReminder);
            Assert.IsFalse(permanent.IsExpiredAt(new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void DocumentBuilder_CopyConstructor_PreservesEverything()
        {
            LiveEventCalendarDocument original = LiveOpsDesignSample.Document;
            LiveEventCalendarDocument copy = new LiveEventCalendarDocumentBuilder(original).Build();

            LiveOpsDocumentAssert.AssertDocumentsEqual(original, copy);
        }
    }
}
