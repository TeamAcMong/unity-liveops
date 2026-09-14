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

        [Test]
        public void Builder_DuplicateEntryKey_Throws()
        {
            var builder = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("same-key", "a", "lava-quest", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z", ""));

            Assert.Throws<ArgumentException>(() => builder.WithFixedEvent(
                new FixedLiveEventEntry("same-key", "b", "lava-quest", "2026-09-12T00:00:00Z", "2026-09-13T00:00:00Z", "")));
        }

        [Test]
        public void Document_KeepsBrokenTimeStrings()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestLateEntryKey, out FixedLiveEventEntry lateLava);

            Assert.AreEqual("2026-10-3", lateLava.EndUtcText, "Tài liệu giữ nguyên văn chuỗi hỏng — hub và validator cần thấy đúng chữ đã gõ.");
            Assert.IsFalse(lateLava.TryGetEndUtc(out _));
            Assert.IsTrue(lateLava.TryGetStartUtc(out _));

            var brokenRule = new RecurringLiveEventRule("sky-race", "hôm qua", "", -1, 0, "");
            LiveEventCalendarDocument withBrokenRule = new LiveEventCalendarDocumentBuilder().WithRecurringRule(brokenRule).Build();
            Assert.AreEqual("hôm qua", withBrokenRule.RecurringRules[0].AnchorUtcText);
            Assert.AreEqual(-1, withBrokenRule.RecurringRules[0].PeriodHours);
        }

        [Test]
        public void LatestStamp_IsLastAdded()
        {
            var older = new PublishedCalendarStamp("2026-09-11T16:20:00Z", "a", "", 1, 2, "", "{}");
            var newer = new PublishedCalendarStamp("2026-09-01T00:00:00Z", "b", "", 1, 2, "", "{}");
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithPublishedStamp(older)
                .WithPublishedStamp(newer)
                .Build();

            Assert.AreSame(newer, document.LatestStamp, "Dấu mới nhất = dấu thêm sau cùng, không sắp lại theo giờ ghi trong dấu.");
            Assert.AreSame(older, document.PublishedStamps[0]);
        }

        [Test]
        public void CanonicalText_ChangesWhenAnyFieldChanges()
        {
            var entry = new FixedLiveEventEntry("key", "id", "lava-quest", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z", "config");
            string[] entryVariants =
            {
                entry.WithEventId("id-2").CanonicalText,
                entry.WithEventType("sky-race").CanonicalText,
                entry.WithTimes("2026-09-10T01:00:00Z", entry.EndUtcText).CanonicalText,
                entry.WithTimes(entry.StartUtcText, "2026-09-12T00:00:00Z").CanonicalText,
                entry.WithConfigKey("config-2").CanonicalText,
            };
            foreach (string variant in entryVariants) Assert.AreNotEqual(entry.CanonicalText, variant);
            Assert.AreEqual(entry.CanonicalText,
                new FixedLiveEventEntry("other-key", "id", "lava-quest", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z", "config").CanonicalText,
                "EntryKey không đi vào JSON nên không đổi dấu vân.");

            var rule = new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "sky-", 24, 20, "config");
            string[] ruleVariants =
            {
                new RecurringLiveEventRule("weekly-pass", "2026-01-05T00:00:00Z", "sky-", 24, 20, "config").CanonicalText,
                rule.WithAnchor("2026-01-06T00:00:00Z").CanonicalText,
                rule.WithIdPrefix("race-").CanonicalText,
                rule.WithPeriodHours(48).CanonicalText,
                rule.WithActiveHours(10).CanonicalText,
                rule.WithConfigKey("config-2").CanonicalText,
            };
            foreach (string variant in ruleVariants) Assert.AreNotEqual(rule.CanonicalText, variant);
        }

        [Test]
        public void DesignSample_PublishedStamp_MatchesSnapshotBytes()
        {
            PublishedCalendarStamp stamp = LiveOpsDesignSample.Document.LatestStamp;
            byte[] snapshotBytes = System.Text.Encoding.UTF8.GetBytes(stamp.SnapshotJson);

            Assert.AreEqual(LiveOpsDesignSample.PublishedByteCount, snapshotBytes.Length);
            Assert.AreEqual(stamp.ByteCount, snapshotBytes.Length);
            using (System.Security.Cryptography.SHA256 hashAlgorithm = System.Security.Cryptography.SHA256.Create())
            {
                string hashHexadecimalText = BitConverter.ToString(hashAlgorithm.ComputeHash(snapshotBytes)).Replace("-", string.Empty).ToLowerInvariant();
                Assert.AreEqual(stamp.Sha256Hex, hashHexadecimalText);
            }
            Assert.AreEqual("DatHoUnityDev", stamp.Publisher);
            Assert.AreEqual("mở lava-quest tháng 9", stamp.Note);
            // Hai chỗ bản đăng khác nháp mà diff/kiểm lịch mẫu cần (6.3, 6.1 luật 10): hunt-0914 hunt_v1, endUtc chuẩn.
            StringAssert.Contains("\"configKey\": \"hunt_v1\"", stamp.SnapshotJson);
            StringAssert.Contains("\"endUtc\": \"2026-10-03T00:00:00Z\"", stamp.SnapshotJson);
            StringAssert.DoesNotContain("hunt_default", stamp.SnapshotJson);
        }
    }
}
