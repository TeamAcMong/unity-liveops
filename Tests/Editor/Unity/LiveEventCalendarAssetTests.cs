using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine;

namespace DreamTech.LiveOps.Unity.Tests
{
    /// <summary><see cref="LiveEventCalendarAsset"/>: khứ hồi tài liệu, asset hỏng không ném, tên field YAML, loại → builder.</summary>
    [TestFixture]
    public sealed class LiveEventCalendarAssetTests
    {
        private static readonly Regex JsonKey = new Regex("\"(\\w+)\":", RegexOptions.Compiled);

        private readonly List<LiveEventCalendarAsset> _createdAssets = new List<LiveEventCalendarAsset>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < _createdAssets.Count; index++)
            {
                if (_createdAssets[index] != null) UnityEngine.Object.DestroyImmediate(_createdAssets[index]);
            }
            _createdAssets.Clear();
        }

        private LiveEventCalendarAsset CreateAsset()
        {
            LiveEventCalendarAsset asset = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
            _createdAssets.Add(asset);
            return asset;
        }

        private sealed class FixedRewardRule : ILiveEventCompletionRule
        {
            public LiveOpsRewardBundle RewardFor(LiveEventRecord record)
            {
                return LiveOpsRewardBundle.None;
            }
        }

        [Test]
        public void EmptyAsset_ToDocument_NoThrow()
        {
            LiveEventCalendarAsset asset = CreateAsset();

            LiveEventCalendarDocument document = null;
            Assert.DoesNotThrow(() => document = asset.ToDocument());

            Assert.AreEqual(LiveEventCalendarAsset.CurrentSchemaVersion, asset.SchemaVersion);
            Assert.AreEqual(LiveEventCalendarDocument.DefaultRemoteConfigKey, document.RemoteConfigKey);
            Assert.AreEqual(0, document.EventTypes.Count);
            Assert.AreEqual(0, document.RecurringRules.Count);
            Assert.AreEqual(0, document.FixedEvents.Count);
            Assert.AreEqual(0, document.PublishedStamps.Count);
            Assert.AreEqual(0, document.IgnoredWarnings.Count);
            Assert.AreEqual(0, asset.Compile().EntryCount);
            Assert.IsFalse(asset.ToParseResult().HasProblems);
        }

        [Test]
        public void BrokenEntries_ToDocument_NoThrow_SkipsBadTypesAndRepairsEntryKeys()
        {
            // YAML sửa tay / xung đột merge: id loại rỗng hoặc có '#', loại trùng, ô màu ngoài [0, 7], đợt thiếu và trùng entryKey.
            LiveEventCalendarAsset asset = CreateAsset();
            JsonUtility.FromJsonOverwrite(
                "{\"remoteConfigKey\":null," +
                "\"eventTypes\":[" +
                "{\"typeId\":\"\",\"colorSlot\":1}," +
                "{\"typeId\":\"bad#type\",\"colorSlot\":1}," +
                "{\"typeId\":\"hunt\",\"displayName\":\"Săn\",\"colorSlot\":99,\"requiresJoin\":true}," +
                "{\"typeId\":\"hunt\",\"displayName\":\"Săn trùng\",\"colorSlot\":2}]," +
                "\"fixedEvents\":[" +
                "{\"entryKey\":\"\",\"eventId\":\"a\",\"eventType\":\"hunt\"}," +
                "{\"entryKey\":\"same\",\"eventId\":\"b\",\"eventType\":\"hunt\"}," +
                "{\"entryKey\":\"same\",\"eventId\":\"c\",\"eventType\":\"hunt\"}]}",
                asset);

            LiveEventCalendarDocument document = null;
            Assert.DoesNotThrow(() => document = asset.ToDocument());

            Assert.AreEqual(1, document.EventTypes.Count);
            Assert.AreEqual("hunt", document.EventTypes[0].TypeId);
            Assert.AreEqual("Săn", document.EventTypes[0].DisplayName, "Loại trùng id: giữ loại đứng trước.");
            Assert.AreEqual(LiveEventTypeColorSlots.DefaultSlotFor("hunt"), document.EventTypes[0].ColorSlot);

            Assert.AreEqual(3, document.FixedEvents.Count, "Không đợt nào bị mất vì khoá hỏng.");
            Assert.AreEqual("asset-fixed-0", document.FixedEvents[0].EntryKey);
            Assert.AreEqual("same", document.FixedEvents[1].EntryKey);
            Assert.AreEqual("asset-fixed-2", document.FixedEvents[2].EntryKey);

            LiveEventCalendarDocument again = asset.ToDocument();
            LiveOpsDocumentAssert.AssertDocumentsEqual(document, again);
        }

        [Test]
        public void ApplyDocument_ToDocument_RoundTrip()
        {
            LiveEventCalendarDocument source = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.Document)
                .WithRemoteConfigKey("liveops_calendar_v2")
                .WithIgnoredWarning(new IgnoredCalendarWarning("long-gap-between-events", "lava-quest", "2026-09-20T00:00:00Z",
                    "2026-10-01T00:00:00Z", "nghỉ giữa mùa", "2026-10-01T00:00:00Z"))
                .Build();
            LiveEventCalendarAsset asset = CreateAsset();

            asset.ApplyDocument(source);
            LiveEventCalendarDocument roundTripped = asset.ToDocument();

            LiveOpsDocumentAssert.AssertDocumentsEqual(source, roundTripped);

            // Ghi lại tài liệu nhỏ hơn phải THAY danh sách, không trộn phần cũ.
            asset.ApplyDocument(LiveEventCalendarDocument.Empty);
            LiveOpsDocumentAssert.AssertDocumentsEqual(LiveEventCalendarDocument.Empty, asset.ToDocument());
        }

        [Test]
        public void BrokenTimeStrings_Preserved()
        {
            LiveEventCalendarDocument source = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.Document)
                .WithRecurringRule(new RecurringLiveEventRule("star-tournament", "5/1/2026", string.Empty, 24, 30, string.Empty))
                .Build();
            LiveEventCalendarAsset asset = CreateAsset();

            asset.ApplyDocument(source);
            LiveEventCalendarDocument document = asset.ToDocument();

            Assert.IsTrue(document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestLateEntryKey, out FixedLiveEventEntry lateLava));
            Assert.AreEqual("2026-10-3", lateLava.EndUtcText, "PD-2: chuỗi hỏng giữ nguyên văn để hub hiện và báo lỗi.");
            Assert.AreEqual("5/1/2026", document.RecurringRules[2].AnchorUtcText);
            Assert.AreEqual(30, document.RecurringRules[2].ActiveHours);

            string serialized = JsonUtility.ToJson(asset);
            StringAssert.Contains("\"endUtc\":\"2026-10-3\"", serialized);
            StringAssert.Contains("\"anchorUtc\":\"5/1/2026\"", serialized);

            Assert.AreEqual(LiveEventCalendarDropReason.UnreadableEndUtc,
                asset.Compile().TryGetFixedOutcome(LiveOpsDesignSample.LavaQuestLateEntryKey, out LiveEventCalendarEntryOutcome outcome)
                    ? outcome.DropReason
                    : LiveEventCalendarDropReason.None);
        }

        [Test]
        public void NewerSchema_ReadsKnownFields()
        {
            LiveEventCalendarAsset asset = CreateAsset();
            JsonUtility.FromJsonOverwrite(
                "{\"schemaVersion\":2,\"remoteConfigKey\":\"calendar_next\",\"lanePresets\":[{\"name\":\"mùa thu\"}]," +
                "\"eventTypes\":[{\"typeId\":\"hunt\",\"displayName\":\"Săn\",\"colorSlot\":3,\"requiresJoin\":true," +
                "\"defaultConfigKey\":\"hunt_default\",\"icon\":\"shovel\"}]," +
                "\"fixedEvents\":[{\"entryKey\":\"key-1\",\"eventId\":\"hunt-1\",\"eventType\":\"hunt\"," +
                "\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"2026-09-15T00:00:00Z\",\"configKey\":\"\",\"audience\":\"vip\"}]}",
                asset);

            Assert.AreEqual(2, asset.SchemaVersion);
            LiveEventCalendarDocument document = asset.ToDocument();
            Assert.AreEqual("calendar_next", document.RemoteConfigKey);
            Assert.AreEqual(1, document.EventTypes.Count);
            Assert.AreEqual(3, document.EventTypes[0].ColorSlot);
            Assert.IsTrue(document.EventTypes[0].RequiresJoin);
            Assert.AreEqual("hunt_default", document.EventTypes[0].DefaultConfigKey);
            Assert.AreEqual(1, document.FixedEvents.Count);
            Assert.AreEqual("hunt-1", document.FixedEvents[0].EventId);
            Assert.AreEqual(1, asset.Compile().KeptCount);

            // Bản cũ hơn ghi đè field quen không được hạ số schema — inspector vẫn phải báo "lưu bởi bản mới hơn".
            asset.ApplyDocument(document);
            Assert.AreEqual(2, asset.SchemaVersion);

            LiveEventCalendarAsset legacy = CreateAsset();
            JsonUtility.FromJsonOverwrite("{\"schemaVersion\":0}", legacy);
            legacy.ApplyDocument(LiveEventCalendarDocument.Empty);
            Assert.AreEqual(LiveEventCalendarAsset.CurrentSchemaVersion, legacy.SchemaVersion);
        }

        [Test]
        public void WithEventTypesFrom_RequiresJoinBecomesExplicitJoin()
        {
            LiveEventCalendarAsset asset = CreateAsset();
            asset.ApplyDocument(LiveOpsDesignSample.Document);
            var lavaRule = new FixedRewardRule();
            var requestedTypes = new List<string>();

            LiveOpsSystem system = new LiveOpsSystemBuilder("main")
                .WithCalendar(asset.ToParseResult().CombinedCalendar)
                .WithEventTypesFrom(asset, typeId =>
                {
                    requestedTypes.Add(typeId);
                    return typeId == "lava-quest" ? lavaRule : null;
                })
                .Build();

            CollectionAssert.AreEqual(new[] { "lava-quest", "sky-race", "star-tournament", "treasure-hunt", "weekly-pass" }, system.EventTypes,
                "Đăng ký theo thứ tự làn của asset.");
            CollectionAssert.AreEqual(system.EventTypes, requestedTypes);
            Assert.AreEqual(LiveEventJoinPolicy.ExplicitJoin, system.GetRules("treasure-hunt").JoinPolicy);
            Assert.AreEqual(LiveEventJoinPolicy.JoinOnFirstProgress, system.GetRules("lava-quest").JoinPolicy);
            Assert.AreSame(lavaRule, system.GetRules("lava-quest").CompletionRule);
            Assert.AreSame(NoLiveEventCompletionReward.Instance, system.GetRules("sky-race").CompletionRule, "null = mặc định của builder.");
            Assert.AreSame(AlwaysLiveEventEligibility.Instance, system.GetRules("sky-race").Eligibility);

            Assert.Throws<ArgumentNullException>(() => new LiveOpsSystemBuilder("main").WithEventTypesFrom(null));
        }

        [Test]
        public void ToParseResult_UsesExportOrder()
        {
            // Hai đợt trùng id, đợt bắt đầu MUỘN đứng trước trong asset: thứ tự asset giữ "late-first", thứ tự xuất (game đọc
            // JSON đã sắp) giữ "early-second". Asset phải nói đúng như game đọc JSON xuất (V-6).
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("hunt", "Săn", 7, false, "hunt_default"))
                .WithFixedEvent(new FixedLiveEventEntry("late-first", "hunt-dup", "hunt", "2026-10-05T00:00:00Z", "2026-10-06T00:00:00Z", string.Empty))
                .WithFixedEvent(new FixedLiveEventEntry("early-second", "hunt-dup", "hunt", "2026-10-01T00:00:00Z", "2026-10-02T00:00:00Z", string.Empty))
                .Build();
            LiveEventCalendarAsset asset = CreateAsset();
            asset.ApplyDocument(document);

            LiveEventCalendarParseResult result = asset.ToParseResult();
            LiveEventCalendarCompilation expected = LiveEventCalendarCompiler.CompileInExportOrder(document);

            Assert.AreEqual(2, result.FormatVersion);
            Assert.IsTrue(result.CameFromDefaultCalendar);
            LiveOpsDocumentAssert.AssertCompiledOutcomesEqual("Entries", expected.Entries, result.Compilation.Entries);
            CollectionAssert.AreEqual(expected.Problems, result.Problems);
            Assert.IsTrue(result.Compilation.TryGetFixedOutcome("early-second", out LiveEventCalendarEntryOutcome earlyOutcome));
            Assert.IsTrue(earlyOutcome.IsKept);
            Assert.AreEqual(1, result.Calendar.Instances.Count);
            Assert.AreEqual(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), result.Calendar.Instances[0].StartUtc);
            Assert.AreEqual("hunt_default", result.Calendar.Instances[0].ConfigKey, "Asset biết loại nên configKey kế thừa được.");

            Assert.IsTrue(LiveEventCalendarCompiler.Compile(document).TryGetFixedOutcome("late-first", out LiveEventCalendarEntryOutcome sourceOrderOutcome));
            Assert.IsTrue(sourceOrderOutcome.IsKept, "Đối chứng: biên dịch theo thứ tự asset giữ đợt khác — nên thứ tự xuất mới là thứ đang được khoá.");
        }

        [Test]
        public void SerializedFieldNames_Stable()
        {
            // Tên + thứ tự field là định dạng file asset của người dùng (mục 4.1) — đổi tên là mất dữ liệu đã commit.
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("hunt", "Săn", 7, true, "hunt_default"))
                .WithRecurringRule(new RecurringLiveEventRule("hunt", "2026-01-05T00:00:00Z", "hunt-", 24, 20, "hunt_daily"))
                .WithFixedEvent(new FixedLiveEventEntry("key-1", "hunt-1", "hunt", "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z", "hunt_v1"))
                .WithPublishedStamp(new PublishedCalendarStamp("2026-09-11T16:20:00Z", "DatHoUnityDev", "abc", 2, 2, "ghi chú", "{}"))
                .WithIgnoredWarning(new IgnoredCalendarWarning("config-key-missing", "hunt-1", string.Empty, string.Empty, "đã hỏi", string.Empty))
                .Build();
            LiveEventCalendarAsset asset = CreateAsset();
            asset.ApplyDocument(document);

            var keys = new List<string>();
            foreach (Match match in JsonKey.Matches(JsonUtility.ToJson(asset)))
            {
                string key = match.Groups[1].Value;
                // Field nội bộ của Unity (nếu bản nào ghi ra) không phải định dạng của package.
                if (!key.StartsWith("m_", StringComparison.Ordinal)) keys.Add(key);
            }

            CollectionAssert.AreEqual(new[]
            {
                "schemaVersion", "remoteConfigKey",
                "eventTypes", "typeId", "displayName", "colorSlot", "requiresJoin", "defaultConfigKey",
                "recurringRules", "eventType", "anchorUtc", "idPrefix", "periodHours", "activeHours", "configKey",
                "fixedEvents", "entryKey", "eventId", "eventType", "startUtc", "endUtc", "configKey",
                "publishedStamps", "publishedUtc", "publisher", "sha256", "byteCount", "formatVersion", "note", "snapshotJson",
                "ignoredWarnings", "ruleId", "targetId", "rangeStartUtc", "rangeEndUtc", "note", "expiresUtc",
            }, keys, JsonUtility.ToJson(asset));
        }
    }
}
