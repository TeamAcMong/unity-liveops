using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class LiveEventCalendarEditsTests
    {
        [Test]
        public void AddFixedEvent_AppendsNewEntry()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            var newEntry = new FixedLiveEventEntry(FixedLiveEventEntry.CreateEntryKey(), "new-event", "lava-quest",
                "2026-11-01T00:00:00Z", "2026-11-03T00:00:00Z", "");

            bool applied = LiveEventCalendarEdits.TryApply(document, new AddFixedEventEdit(newEntry), out LiveEventCalendarDocument result);

            Assert.IsTrue(applied);
            Assert.AreEqual(document.FixedEvents.Count + 1, result.FixedEvents.Count);
            Assert.IsTrue(result.TryGetFixedEvent(newEntry.EntryKey, out _));
        }

        [Test]
        public void ReplaceFixedEvent_UnknownEntryKey_ReturnsFalse_KeepsOriginal()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            var ghost = new FixedLiveEventEntry("no-such-key", "x", "lava-quest", "2026-11-01T00:00:00Z", "2026-11-02T00:00:00Z", "");

            bool applied = LiveEventCalendarEdits.TryApply(document, new ReplaceFixedEventEdit(ghost), out LiveEventCalendarDocument result);

            Assert.IsFalse(applied);
            Assert.AreSame(document, result);
        }

        [Test]
        public void ReplaceFixedEvent_KnownEntryKey_ReplacesInPlace()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            int originalIndex = document.IndexOfFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey);
            document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey, out FixedLiveEventEntry original);
            FixedLiveEventEntry shifted = original.WithTimes(original.StartUtcText, "2026-09-19T00:00:00Z");

            bool applied = LiveEventCalendarEdits.TryApply(document, new ReplaceFixedEventEdit(shifted), out LiveEventCalendarDocument result);

            Assert.IsTrue(applied);
            Assert.AreEqual(originalIndex, result.IndexOfFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey));
            result.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey, out FixedLiveEventEntry updated);
            Assert.AreEqual("2026-09-19T00:00:00Z", updated.EndUtcText);
        }

        [Test]
        public void RemoveFixedEvent_UnknownEntryKey_ReturnsFalse()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            bool applied = LiveEventCalendarEdits.TryApply(document, new RemoveFixedEventEdit("missing"), out LiveEventCalendarDocument result);

            Assert.IsFalse(applied);
            Assert.AreSame(document, result);
        }

        [Test]
        public void RemoveFixedEvent_KnownEntryKey_RemovesOnlyThatEntry()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            bool applied = LiveEventCalendarEdits.TryApply(document, new RemoveFixedEventEdit(LiveOpsDesignSample.HuntBonusEntryKey),
                out LiveEventCalendarDocument result);

            Assert.IsTrue(applied);
            Assert.AreEqual(document.FixedEvents.Count - 1, result.FixedEvents.Count);
            Assert.IsFalse(result.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey, out _));
        }

        [Test]
        public void SetRecurringRule_NewType_Appends_ExistingType_Replaces()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            var replacementWeeklyPass = new RecurringLiveEventRule("weekly-pass", "2026-01-05T00:00:00Z", "pass-", 168, 168, "changed");

            LiveEventCalendarEdits.TryApply(document, new SetRecurringRuleEdit(replacementWeeklyPass), out LiveEventCalendarDocument replaced);
            Assert.AreEqual(2, replaced.RecurringRules.Count);
            replaced.TryGetRecurringRule("weekly-pass", out RecurringLiveEventRule updated);
            Assert.AreEqual("changed", updated.ConfigKey);

            var newRule = new RecurringLiveEventRule("new-type", "2026-01-05T00:00:00Z", "", 24, 20, "");
            LiveEventCalendarEdits.TryApply(document, new SetRecurringRuleEdit(newRule), out LiveEventCalendarDocument appended);
            Assert.AreEqual(3, appended.RecurringRules.Count);
        }

        [Test]
        public void RemoveRecurringRule_UnknownType_ReturnsFalse()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            bool applied = LiveEventCalendarEdits.TryApply(document, new RemoveRecurringRuleEdit("no-type"), out LiveEventCalendarDocument result);
            Assert.IsFalse(applied);
        }

        [Test]
        public void MoveEventType_ClampsIndex_KeepsEverythingElse()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            string movedTypeId = document.EventTypes[0].TypeId;

            bool applied = LiveEventCalendarEdits.TryApply(document, new MoveEventTypeEdit(movedTypeId, 999),
                out LiveEventCalendarDocument result);

            Assert.IsTrue(applied);
            Assert.AreEqual(document.EventTypes.Count, result.EventTypes.Count);
            Assert.AreEqual(movedTypeId, result.EventTypes[result.EventTypes.Count - 1].TypeId);
            // Không đổi gì khác: mọi loại còn lại vẫn có mặt, thứ tự tương đối giữ nguyên.
            var expectedRemainingOrder = new List<string>();
            for (int index = 0; index < document.EventTypes.Count; index++)
            {
                if (document.EventTypes[index].TypeId != movedTypeId) expectedRemainingOrder.Add(document.EventTypes[index].TypeId);
            }
            var actualRemainingOrder = new List<string>();
            for (int index = 0; index < result.EventTypes.Count - 1; index++) actualRemainingOrder.Add(result.EventTypes[index].TypeId);
            CollectionAssert.AreEqual(expectedRemainingOrder, actualRemainingOrder);

            // Negative index kẹp về 0.
            LiveEventCalendarEdits.TryApply(document, new MoveEventTypeEdit(movedTypeId, -5), out LiveEventCalendarDocument clampedLow);
            Assert.AreEqual(movedTypeId, clampedLow.EventTypes[0].TypeId);
        }

        [Test]
        public void MoveEventType_UnknownTypeId_ReturnsFalse()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            bool applied = LiveEventCalendarEdits.TryApply(document, new MoveEventTypeEdit("missing", 0), out LiveEventCalendarDocument result);
            Assert.IsFalse(applied);
        }

        [Test]
        public void CompositeEdit_AllOrNothing_FailurePartwayRevertsEverything()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            var edits = new List<LiveEventCalendarEdit>
            {
                new RemoveFixedEventEdit(LiveOpsDesignSample.HuntBonusEntryKey),
                new RemoveFixedEventEdit("missing-entry-key"),
            };

            bool applied = LiveEventCalendarEdits.TryApply(document, new CompositeCalendarEdit(edits), out LiveEventCalendarDocument result);

            Assert.IsFalse(applied);
            Assert.AreSame(document, result);
            Assert.IsTrue(result.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey, out _), "Thất bại giữa chừng phải hoàn về nguyên tài liệu.");
        }

        [Test]
        public void CompositeEdit_AllSucceed_AppliesAllAsOneStep()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            var edits = new List<LiveEventCalendarEdit>
            {
                new RemoveFixedEventEdit(LiveOpsDesignSample.HuntBonusEntryKey),
                new SetRemoteConfigKeyEdit("other_key"),
            };

            bool applied = LiveEventCalendarEdits.TryApply(document, new CompositeCalendarEdit(edits), out LiveEventCalendarDocument result);

            Assert.IsTrue(applied);
            Assert.AreEqual("other_key", result.RemoteConfigKey);
            Assert.IsFalse(result.TryGetFixedEvent(LiveOpsDesignSample.HuntBonusEntryKey, out _));
        }

        [Test]
        public void ReplaceDocumentEdit_NullDocument_ReturnsFalse()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            bool applied = LiveEventCalendarEdits.TryApply(document, new ReplaceDocumentEdit(null), out LiveEventCalendarDocument result);
            Assert.IsFalse(applied);
            Assert.AreSame(document, result);
        }

        [Test]
        public void RemoveLatestPublishedStamp_EmptyStamps_ReturnsFalse()
        {
            LiveEventCalendarDocument document = LiveEventCalendarDocument.Empty;
            bool applied = LiveEventCalendarEdits.TryApply(document, new RemoveLatestPublishedStampEdit(), out LiveEventCalendarDocument result);
            Assert.IsFalse(applied);
        }
    }
}
