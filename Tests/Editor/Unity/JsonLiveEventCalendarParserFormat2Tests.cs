using System;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine;

namespace DreamTech.LiveOps.Unity.Tests
{
    /// <summary>
    /// Parser 0.2.0: định dạng 2 (luật lặp), đọc qua bộ biên dịch core, <c>ParseDocument</c>, <c>ParseOrDefault</c>. Định dạng
    /// 1 do <see cref="JsonLiveEventCalendarParserGoldenTests"/> khoá — fixture này không lặp lại các câu Problems đó.
    /// </summary>
    [TestFixture]
    public sealed class JsonLiveEventCalendarParserFormat2Tests
    {
        private const string SkyRaceRuleJson =
            "{\"type\":\"sky-race\",\"anchorUtc\":\"2026-01-05T00:00:00Z\",\"idPrefix\":\"sky-race-\",\"periodHours\":24,\"activeHours\":20,\"configKey\":\"sky_race_v4\"}";

        private const string HuntEventJson =
            "{\"id\":\"hunt-0914\",\"type\":\"treasure-hunt\",\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"2026-09-17T00:00:00Z\",\"configKey\":\"hunt_default\"}";

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

        private LiveEventCalendarAsset CreateDesignSampleAsset()
        {
            LiveEventCalendarAsset asset = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
            _createdAssets.Add(asset);
            asset.ApplyDocument(LiveOpsDesignSample.Document);
            return asset;
        }

        private static string RecurringRuleJson(string type, string anchorUtc, string idPrefix, int periodHours, int activeHours)
        {
            return "{\"type\":\"" + type + "\",\"anchorUtc\":\"" + anchorUtc + "\",\"idPrefix\":\"" + idPrefix + "\",\"periodHours\":" +
                   periodHours.ToString(CultureInfo.InvariantCulture) + ",\"activeHours\":" + activeHours.ToString(CultureInfo.InvariantCulture) + ",\"configKey\":\"\"}";
        }

        /// <summary>Luật hỏng phải bị bỏ kèm đúng một Problem, phần cố định vẫn đọc, và hỏi lịch ghép không ném.</summary>
        private static void AssertRuleDroppedWithProblem(string rulesJson, int expectedKeptRuleCount, string expectedProblem)
        {
            string json = "{\"version\":2,\"recurring\":[" + rulesJson + "],\"events\":[" + HuntEventJson + "]}";

            LiveEventCalendarParseResult result = null;
            Assert.DoesNotThrow(() => result = JsonLiveEventCalendarParser.Parse(json));

            Assert.AreEqual(expectedKeptRuleCount, result.RecurringCalendars.Count);
            Assert.AreEqual(1, result.Calendar.Instances.Count, "Luật hỏng không được kéo phần đợt cố định xuống theo.");
            Assert.AreEqual(1, result.Problems.Count, string.Join("\n", result.Problems));
            Assert.AreEqual(expectedProblem, result.Problems[0]);
            Assert.DoesNotThrow(() => result.CombinedCalendar.GetInstances("sky-race", new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void DesignSample_Reads8Entries_Keeps6()
        {
            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse(LiveOpsDesignSample.ExpectedFormat2Json);

            Assert.AreEqual(2, result.FormatVersion);
            Assert.IsFalse(result.CameFromDefaultCalendar);
            Assert.AreEqual(8, result.Compilation.EntryCount, "2 luật lặp + 6 đợt cố định.");
            Assert.AreEqual(6, result.Compilation.KeptCount);
            Assert.AreEqual(2, result.RecurringCalendars.Count);
            Assert.AreEqual(4, result.Calendar.Instances.Count, "Bỏ lava-quest-2026-10 (endUtc hỏng) và hunt-0916-bonus (chồng giờ).");

            Assert.AreEqual(2, result.Problems.Count, string.Join("\n", result.Problems));
            Assert.AreEqual("Mục thứ 5 ('lava-quest-2026-10'): endUtc không phải giờ ISO 8601: '2026-10-3' — bỏ qua.", result.Problems[0]);
            Assert.AreEqual("Đợt 'hunt-0916-bonus' chồng giờ với 'hunt-0914' cùng loại 'treasure-hunt' — bỏ 'hunt-0916-bonus'.", result.Problems[1]);

            Assert.IsTrue(result.Compilation.TryGetFixedOutcome("json-2", out LiveEventCalendarEntryOutcome bonusOutcome));
            Assert.AreEqual("hunt-0916-bonus", bonusOutcome.EventId);
            Assert.AreEqual(LiveEventCalendarDropReason.OverlapsSameType, bonusOutcome.DropReason);
        }

        [Test]
        public void RecurringOnly_NoProblem()
        {
            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse("{\"version\":2,\"recurring\":[" + SkyRaceRuleJson + "]}");

            Assert.IsFalse(result.HasProblems, string.Join("\n", result.Problems));
            Assert.AreEqual(2, result.FormatVersion);
            Assert.AreEqual(1, result.RecurringCalendars.Count);
            Assert.AreEqual(0, result.Calendar.Instances.Count, "Thiếu events khi đã có recurring là hợp lệ.");
        }

        [Test]
        public void EmptyObject_StillMissingEventsProblem()
        {
            foreach (string json in new[] { "{}", "{\"version\":2}", "{\"version\":2,\"unknown\":[]}" })
            {
                LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse(json);

                Assert.AreEqual(1, result.Problems.Count, json + "\n" + string.Join("\n", result.Problems));
                Assert.AreEqual("JSON lịch event thiếu mảng \"events\".", result.Problems[0], json);
                Assert.AreEqual(0, result.Calendar.Instances.Count, json);
                Assert.AreEqual(0, result.RecurringCalendars.Count, json);
            }
        }

        [Test]
        public void VersionMissingWithRecurring_IsFormat2()
        {
            Assert.AreEqual(2, JsonLiveEventCalendarParser.Parse("{\"recurring\":[],\"events\":[" + HuntEventJson + "]}").FormatVersion);
            Assert.AreEqual(2, JsonLiveEventCalendarParser.Parse("{\"recurring\":null,\"events\":[]}").FormatVersion,
                "\"recurring\": null vẫn là CÓ ghi key (JsonUtility cho mảng rỗng).");
            Assert.AreEqual(1, JsonLiveEventCalendarParser.Parse("{\"events\":[" + HuntEventJson + "]}").FormatVersion);
            Assert.AreEqual(2, JsonLiveEventCalendarParser.ParseDocument("{\"recurring\":[" + SkyRaceRuleJson + "]}").FormatVersion);
        }

        [Test]
        public void NewerVersion_ProblemButStillReads()
        {
            string json = "{\"version\":3,\"lanes\":[{\"order\":1}],\"recurring\":[" + SkyRaceRuleJson + "],\"events\":[" + HuntEventJson + "]}";

            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse(json);

            Assert.AreEqual(3, result.FormatVersion);
            Assert.AreEqual(1, result.Problems.Count, string.Join("\n", result.Problems));
            Assert.AreEqual("Lịch định dạng 3 mới hơn parser này (đọc được 1 và 2) — chỉ đọc recurring và events.", result.Problems[0]);
            Assert.AreEqual(1, result.RecurringCalendars.Count);
            Assert.AreEqual(1, result.Calendar.Instances.Count);
        }

        [Test]
        public void InvalidRecurring_PeriodZero_DroppedWithProblem_NeverThrows()
        {
            AssertRuleDroppedWithProblem(RecurringRuleJson("sky-race", "2026-01-05T00:00:00Z", "sky-race-", 0, 0), 0,
                "Luật lặp thứ 1 ('sky-race'): chu kỳ phải > 0 giờ — bỏ qua.");
        }

        [Test]
        public void InvalidRecurring_ActiveLongerThanPeriod_DroppedWithProblem_NeverThrows()
        {
            AssertRuleDroppedWithProblem(RecurringRuleJson("sky-race", "2026-01-05T00:00:00Z", "sky-race-", 24, 25), 0,
                "Luật lặp thứ 1 ('sky-race'): thời gian chạy phải trong (0, chu kỳ] — bỏ qua.");
        }

        [Test]
        public void InvalidRecurring_PrefixHash_DroppedWithProblem_NeverThrows()
        {
            // R-4: ctor RecurringLiveEventCalendar ném với tiền tố có '#' — bộ biên dịch phải chặn trước khi dựng.
            AssertRuleDroppedWithProblem(RecurringRuleJson("sky-race", "2026-01-05T00:00:00Z", "sky#race-", 24, 20), 0,
                "Luật lặp thứ 1 ('sky-race'): tiền tố id chứa '#' hoặc xuống dòng — bỏ qua.");
        }

        [Test]
        public void InvalidRecurring_AnchorUnreadable_DroppedWithProblem_NeverThrows()
        {
            AssertRuleDroppedWithProblem(RecurringRuleJson("sky-race", "2026-1-5", "sky-race-", 24, 20), 0,
                "Luật lặp thứ 1 ('sky-race'): neo không đọc được: '2026-1-5' — bỏ qua.");
        }

        [Test]
        public void InvalidRecurring_SecondRuleSameType_DroppedWithProblem_NeverThrows()
        {
            AssertRuleDroppedWithProblem(SkyRaceRuleJson + "," + RecurringRuleJson("sky-race", "2026-01-06T00:00:00Z", "other-", 48, 10), 1,
                "Luật lặp thứ 2 ('sky-race'): trùng luật cho loại 'sky-race' — giữ luật đứng trước.");
        }

        [Test]
        public void CombinedCalendar_HasRecurringInstances()
        {
            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse(LiveOpsDesignSample.ExpectedFormat2Json);
            var dayStart = new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Utc);

            // Khung nằm gọn trong 20 giờ chạy của lần lặp ngày 13/9 — chỉ đúng một lần lặp giao khung.
            IReadOnlyList<LiveEventInstance> skyRaces = result.CombinedCalendar.GetInstances("sky-race", dayStart.AddHours(1), dayStart.AddHours(19));

            Assert.AreEqual(1, skyRaces.Count);
            long expectedIndex = (dayStart - new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)).Ticks / TimeSpan.TicksPerDay;
            Assert.AreEqual("sky-race-" + expectedIndex.ToString(CultureInfo.InvariantCulture), skyRaces[0].EventId);
            Assert.AreEqual(dayStart, skyRaces[0].StartUtc);
            Assert.AreEqual(dayStart.AddHours(20), skyRaces[0].EndUtc);
            Assert.AreEqual("sky_race_v4", skyRaces[0].ConfigKey);

            IReadOnlyList<LiveEventInstance> hunts = result.CombinedCalendar.GetInstances("treasure-hunt", dayStart, dayStart.AddDays(7));
            Assert.AreEqual(1, hunts.Count, "Lịch ghép vẫn có đợt cố định.");
            Assert.AreEqual("hunt-0914", hunts[0].EventId);
        }

        [Test]
        public void CalendarProperty010_IsFixedPartOnly()
        {
            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse(LiveOpsDesignSample.ExpectedFormat2Json);
            var from = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

            Assert.AreSame(result.Compilation.FixedCalendar, result.Calendar);
            Assert.AreEqual(0, result.Calendar.GetInstances("sky-race", from, to).Count, "Calendar 0.1.0 không có luật lặp.");
            Assert.AreEqual(0, result.Calendar.GetInstances("weekly-pass", from, to).Count);
            Assert.Greater(result.CombinedCalendar.GetInstances("sky-race", from, to).Count, 0);
            CollectionAssert.DoesNotContain(result.Calendar.EventTypes, "sky-race");
            CollectionAssert.Contains(result.CombinedCalendar.EventTypes, "sky-race");
        }

        [Test]
        public void WrongJsonTypes_NumberWhereStringExpected_DoesNotThrow()
        {
            // JsonUtility ép số↔chuỗi im lặng (SP-11): id số thành chuỗi, chu kỳ viết dạng chuỗi thành int — không tới nhánh exception.
            const string json = "{\"version\":2," +
                                 "\"recurring\":[{\"type\":\"sky-race\",\"anchorUtc\":\"2026-01-05T00:00:00Z\",\"periodHours\":\"24\",\"activeHours\":\"20\"}]," +
                                 "\"events\":[{\"id\":5,\"type\":\"treasure-hunt\",\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"2026-09-15T00:00:00Z\"}]}";

            LiveEventCalendarParseResult result = null;
            Assert.DoesNotThrow(() => result = JsonLiveEventCalendarParser.Parse(json));

            Assert.IsFalse(result.HasProblems, string.Join("\n", result.Problems));
            Assert.AreEqual(1, result.Calendar.Instances.Count);
            Assert.AreEqual("5", result.Calendar.Instances[0].EventId);
            Assert.AreEqual(1, result.RecurringCalendars.Count);
            Assert.AreEqual(TimeSpan.FromHours(24), result.RecurringCalendars[0].Period);
            Assert.AreEqual("sky-race-", result.RecurringCalendars[0].IdPrefix, "Thiếu idPrefix → mặc định type + \"-\".");
        }

        [Test]
        public void ParseOrDefault_BlankUsesAsset()
        {
            LiveEventCalendarAsset asset = CreateDesignSampleAsset();
            LiveEventCalendarParseResult assetResult = asset.ToParseResult();

            foreach (string json in new[] { null, string.Empty, "   ", "\n\t" })
            {
                LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.ParseOrDefault(json, asset);

                Assert.IsTrue(result.CameFromDefaultCalendar, "json='" + json + "'");
                Assert.AreEqual(2, result.FormatVersion);
                Assert.AreEqual(6, result.Compilation.KeptCount);
                Assert.AreEqual(2, result.RecurringCalendars.Count);
                CollectionAssert.AreEqual(assetResult.Problems, result.Problems, "Remote trống không phải lỗi — chỉ Problems của asset.");
            }

            LiveEventCalendarParseResult withoutAsset = JsonLiveEventCalendarParser.ParseOrDefault("  ", null);
            Assert.IsFalse(withoutAsset.CameFromDefaultCalendar, "Không có asset thì như Parse.");
            Assert.IsFalse(withoutAsset.HasProblems);
            Assert.AreSame(FixedLiveEventCalendar.Empty, withoutAsset.Calendar);
        }

        [Test]
        public void ParseOrDefault_BrokenJson_FollowsDefaultRemoteFailurePolicy()
        {
            LiveEventCalendarAsset asset = CreateDesignSampleAsset();
            const string brokenJson = "{ \"version\": 2, \"events\": [ ";

            LiveEventCalendarParseResult twoArgument = JsonLiveEventCalendarParser.ParseOrDefault(brokenJson, asset);
            LiveEventCalendarParseResult explicitDefault =
                JsonLiveEventCalendarParser.ParseOrDefault(brokenJson, asset, JsonLiveEventCalendarParser.DefaultRemoteFailurePolicy);

            // Không khoá chính sách nào ở đây (Q-9 còn mở) — chỉ khoá rằng overload hai tham số đi đúng theo hằng.
            Assert.AreEqual(explicitDefault.CameFromDefaultCalendar, twoArgument.CameFromDefaultCalendar);
            Assert.AreEqual(explicitDefault.FormatVersion, twoArgument.FormatVersion);
            Assert.AreEqual(explicitDefault.Compilation.KeptCount, twoArgument.Compilation.KeptCount);
            CollectionAssert.AreEqual(explicitDefault.Problems, twoArgument.Problems);
            Assert.IsTrue(twoArgument.HasProblems, "JSON hỏng luôn phải để lại dấu vết trong Problems, dù chọn chính sách nào.");
        }

        [Test]
        public void ParseDocument_KeepsBrokenStringsAndSourceOrder()
        {
            const string json = "{\"version\":2," +
                                 "\"recurring\":[{\"type\":\"sky-race\",\"anchorUtc\":\"2026-1-5\",\"idPrefix\":\"\",\"periodHours\":0,\"activeHours\":20,\"configKey\":\"\"}]," +
                                 "\"events\":[" +
                                 "{\"id\":\"late\",\"type\":\"lava-quest\",\"startUtc\":\"2026-10-01T00:00:00Z\",\"endUtc\":\"2026-10-3\",\"configKey\":\"\"}," +
                                 "{\"id\":\"early#1\",\"type\":\"lava-quest\",\"startUtc\":\"2026-9-1\",\"endUtc\":\"2026-09-03T00:00:00+07:00\"}" +
                                 "]}";

            LiveEventCalendarDocumentParseResult result = JsonLiveEventCalendarParser.ParseDocument(json);

            Assert.IsTrue(result.IsReadable);
            Assert.AreEqual(string.Empty, result.ReadErrorText);
            Assert.AreEqual(2, result.FormatVersion);
            Assert.AreEqual(0, result.Problems.Count, "Lý do bỏ mục là việc của bộ biên dịch, không phải của bước đọc.");

            LiveEventCalendarDocument document = result.Document;
            Assert.AreEqual(0, document.EventTypes.Count, "JSON không mang định nghĩa loại.");

            Assert.AreEqual(1, document.RecurringRules.Count);
            Assert.AreEqual("2026-1-5", document.RecurringRules[0].AnchorUtcText);
            Assert.AreEqual(string.Empty, document.RecurringRules[0].IdPrefix, "Giữ tiền tố thô, không thay bằng tiền tố hiệu lực.");
            Assert.AreEqual(0, document.RecurringRules[0].PeriodHours);

            Assert.AreEqual(2, document.FixedEvents.Count);
            Assert.AreEqual("json-0", document.FixedEvents[0].EntryKey);
            Assert.AreEqual("late", document.FixedEvents[0].EventId, "Không sắp theo giờ — giữ thứ tự JSON.");
            Assert.AreEqual("2026-10-3", document.FixedEvents[0].EndUtcText);
            Assert.AreEqual("json-1", document.FixedEvents[1].EntryKey);
            Assert.AreEqual("early#1", document.FixedEvents[1].EventId);
            Assert.AreEqual("2026-9-1", document.FixedEvents[1].StartUtcText);
            Assert.AreEqual("2026-09-03T00:00:00+07:00", document.FixedEvents[1].EndUtcText, "Không chuẩn hoá múi giờ ở bước đọc.");
            Assert.IsFalse(document.FixedEvents[1].HasOwnConfigKey);

            LiveEventCalendarDocumentParseResult broken = JsonLiveEventCalendarParser.ParseDocument("{ not json");
            Assert.IsFalse(broken.IsReadable);
            Assert.IsNotEmpty(broken.ReadErrorText);
            Assert.AreEqual(0, broken.FormatVersion);
            Assert.AreSame(LiveEventCalendarDocument.Empty, broken.Document);
            Assert.AreEqual(1, broken.Problems.Count);
            Assert.AreEqual("JSON lịch event hỏng: " + broken.ReadErrorText, broken.Problems[0]);
        }
    }
}
