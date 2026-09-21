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

        [Test]
        public void RemoveRecurringRule_KnownType_RemovesOnlyThatRule()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;

            bool applied = LiveEventCalendarEdits.TryApply(document, new RemoveRecurringRuleEdit("weekly-pass"), out LiveEventCalendarDocument result);

            Assert.IsTrue(applied);
            Assert.AreEqual(1, result.RecurringRules.Count);
            Assert.AreEqual("sky-race", result.RecurringRules[0].EventType);
            Assert.IsFalse(result.TryGetRecurringRule("weekly-pass", out _));
            Assert.AreEqual(document.FixedEvents.Count, result.FixedEvents.Count);
        }

        [Test]
        public void SetEventType_NewType_Appends_ExistingType_ReplacesInPlace()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            int skyRaceIndex = IndexOfType(document, "sky-race");
            LiveEventTypeDefinition recolored = document.EventTypes[skyRaceIndex].WithColorSlot(2);

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new SetEventTypeEdit(recolored), out LiveEventCalendarDocument replaced));
            Assert.AreEqual(document.EventTypes.Count, replaced.EventTypes.Count);
            Assert.AreEqual(skyRaceIndex, IndexOfType(replaced, "sky-race"), "Thay loại không đổi thứ tự làn.");
            Assert.AreEqual(2, replaced.EventTypes[skyRaceIndex].ColorSlot);

            var luckySpin = new LiveEventTypeDefinition("lucky-spin", "Vòng quay", 5, false, "");
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new SetEventTypeEdit(luckySpin), out LiveEventCalendarDocument appended));
            Assert.AreEqual(document.EventTypes.Count + 1, appended.EventTypes.Count);
            Assert.AreEqual("lucky-spin", appended.EventTypes[appended.EventTypes.Count - 1].TypeId);
        }

        [Test]
        public void RemoveEventType_KnownType_RemovesDefinitionOnly_UnknownReturnsFalse()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new RemoveEventTypeEdit("star-tournament"), out LiveEventCalendarDocument result));
            Assert.AreEqual(document.EventTypes.Count - 1, result.EventTypes.Count);
            Assert.IsFalse(result.TryGetEventType("star-tournament", out _));
            Assert.AreEqual(document.FixedEvents.Count, result.FixedEvents.Count, "Xoá định nghĩa loại không tự xoá đợt của loại đó.");

            Assert.IsFalse(LiveEventCalendarEdits.TryApply(document, new RemoveEventTypeEdit("missing"), out LiveEventCalendarDocument unchanged));
            Assert.AreSame(document, unchanged);
        }

        [Test]
        public void SetRemoteConfigKey_ChangesOnlyKey()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new SetRemoteConfigKeyEdit("liveops_calendar_v2"), out LiveEventCalendarDocument result));
            Assert.AreEqual("liveops_calendar_v2", result.RemoteConfigKey);
            Assert.AreEqual(LiveEventCalendarDocument.DefaultRemoteConfigKey, document.RemoteConfigKey, "Tài liệu gốc bất biến.");
            Assert.AreEqual(document.FixedEvents.Count, result.FixedEvents.Count);
            Assert.AreEqual(document.PublishedStamps.Count, result.PublishedStamps.Count);
        }

        [Test]
        public void AddPublishedStamp_BecomesLatest()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            var stamp = new PublishedCalendarStamp("2026-09-13T09:04:00Z", "DatHoUnityDev", "", 1612, 2, "mở hunt-0916-bonus", "{}");

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new AddPublishedStampEdit(stamp), out LiveEventCalendarDocument result));
            Assert.AreEqual(2, result.PublishedStamps.Count);
            Assert.AreSame(stamp, result.LatestStamp);
        }

        [Test]
        public void RemoveLatestPublishedStamp_RemovesOnlyNewest()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            PublishedCalendarStamp original = document.LatestStamp;
            var stamp = new PublishedCalendarStamp("2026-09-13T09:04:00Z", "DatHoUnityDev", "", 1612, 2, "", "{}");
            LiveEventCalendarEdits.TryApply(document, new AddPublishedStampEdit(stamp), out LiveEventCalendarDocument stamped);

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(stamped, new RemoveLatestPublishedStampEdit(), out LiveEventCalendarDocument result));
            Assert.AreEqual(1, result.PublishedStamps.Count);
            Assert.AreSame(original, result.LatestStamp, "Gỡ dấu mới nhất thì bản so quay về dấu trước.");
        }

        [Test]
        public void AddIgnoredWarning_Appends_RemoveIgnoredWarning_MatchesEquivalentCopy()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            var warning = new IgnoredCalendarWarning("long-gap-between-events", "lava-quest", "2026-09-20T00:00:00Z",
                "2026-10-01T00:00:00Z", "chờ lịch tháng 10", "");

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new AddIgnoredWarningEdit(warning), out LiveEventCalendarDocument ignored));
            Assert.AreEqual(1, ignored.IgnoredWarnings.Count);

            // Bản sao cùng luật/đích/khoảng/hạn (ghi chú khác) vẫn gỡ được — Undo/hub có thể dựng lại object mới từ asset.
            var equivalentCopy = new IgnoredCalendarWarning("long-gap-between-events", "lava-quest", "2026-09-20T00:00:00Z",
                "2026-10-01T00:00:00Z", "ghi chú khác", "");
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(ignored, new RemoveIgnoredWarningEdit(equivalentCopy), out LiveEventCalendarDocument restored));
            Assert.AreEqual(0, restored.IgnoredWarnings.Count);

            var otherRange = new IgnoredCalendarWarning("long-gap-between-events", "lava-quest", "2026-09-21T00:00:00Z",
                "2026-10-01T00:00:00Z", "", "");
            Assert.IsFalse(LiveEventCalendarEdits.TryApply(ignored, new RemoveIgnoredWarningEdit(otherRange), out LiveEventCalendarDocument unchanged));
            Assert.AreSame(ignored, unchanged);
        }

        [Test]
        public void Composite_AppliesInOrder()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            string newEntryKey = FixedLiveEventEntry.CreateEntryKey();
            var added = new FixedLiveEventEntry(newEntryKey, "draft", "lava-quest", "2026-11-01T00:00:00Z", "2026-11-02T00:00:00Z", "");
            var edits = new List<LiveEventCalendarEdit>
            {
                new AddFixedEventEdit(added),
                // Lệnh sau trỏ vào mục do lệnh trước tạo — chỉ thành công khi áp đúng thứ tự.
                new ReplaceFixedEventEdit(added.WithEventId("final")),
            };

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new CompositeCalendarEdit(edits), out LiveEventCalendarDocument result));
            Assert.IsTrue(result.TryGetFixedEvent(newEntryKey, out FixedLiveEventEntry entry));
            Assert.AreEqual("final", entry.EventId);

            var reversed = new List<LiveEventCalendarEdit> { edits[1], edits[0] };
            Assert.IsFalse(LiveEventCalendarEdits.TryApply(document, new CompositeCalendarEdit(reversed), out LiveEventCalendarDocument notApplied));
            Assert.AreSame(document, notApplied);
        }

        [Test]
        public void EditOnMissingEntry_ReturnsFalseAndSameDocument()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            var ghost = new FixedLiveEventEntry("removed-by-undo", "x", "lava-quest", "2026-11-01T00:00:00Z", "2026-11-02T00:00:00Z", "");
            LiveEventCalendarEdit[] editsOnMissing =
            {
                new ReplaceFixedEventEdit(ghost),
                new RemoveFixedEventEdit("removed-by-undo"),
                new RemoveRecurringRuleEdit("removed-type"),
                new RemoveEventTypeEdit("removed-type"),
                new MoveEventTypeEdit("removed-type", 0),
                new RemoveIgnoredWarningEdit(new IgnoredCalendarWarning("rule", "target", "", "", "", "")),
            };
            foreach (LiveEventCalendarEdit edit in editsOnMissing)
            {
                Assert.IsFalse(LiveEventCalendarEdits.TryApply(document, edit, out LiveEventCalendarDocument result), edit.GetType().Name);
                Assert.AreSame(document, result, edit.GetType().Name);
            }
        }

        [Test]
        public void ReplaceDocument_KeepsNothingOfOld()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            LiveEventCalendarDocument replacement = new LiveEventCalendarDocumentBuilder()
                .WithRemoteConfigKey("other_key")
                .WithFixedEvent(new FixedLiveEventEntry("only-entry", "only", "lava-quest", "2026-11-01T00:00:00Z", "2026-11-02T00:00:00Z", ""))
                .Build();

            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, new ReplaceDocumentEdit(replacement), out LiveEventCalendarDocument result));
            Assert.AreSame(replacement, result);
            Assert.AreEqual(0, result.EventTypes.Count);
            Assert.AreEqual(0, result.RecurringRules.Count);
            Assert.AreEqual(1, result.FixedEvents.Count);
            Assert.AreEqual(0, result.PublishedStamps.Count);
            Assert.AreEqual("other_key", result.RemoteConfigKey);
        }

        private static int IndexOfType(LiveEventCalendarDocument document, string typeId)
        {
            for (int index = 0; index < document.EventTypes.Count; index++)
            {
                if (string.Equals(document.EventTypes[index].TypeId, typeId, System.StringComparison.Ordinal)) return index;
            }
            return -1;
        }
    }
}
