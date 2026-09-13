using System;
using NUnit.Framework;
using UnityEngine;

namespace DreamTech.LiveOps.Unity.Tests
{
    [TestFixture]
    public class HttpDateHeaderServerTimeSourceTests
    {
        private const string MondayMorning = "Mon, 14 Sep 2026 08:00:00 GMT";

        [Test]
        public void ComputesUtc_FromDatePlusAgePlusHalfRoundTrip()
        {
            Assert.IsTrue(HttpDateHeaderServerTimeSource.TryComputeServerUtc(MondayMorning, "30", TimeSpan.FromMilliseconds(400),
                                                                             out DateTime serverUtc));

            Assert.AreEqual(new DateTime(2026, 9, 14, 8, 0, 30, 200, DateTimeKind.Utc), serverUtc);
            Assert.AreEqual(DateTimeKind.Utc, serverUtc.Kind);
        }

        [Test]
        public void RejectsMissingOrNonRfc1123Date_IgnoresUnreadableAge()
        {
            Assert.IsFalse(HttpDateHeaderServerTimeSource.TryComputeServerUtc(null, null, TimeSpan.Zero, out _));
            Assert.IsFalse(HttpDateHeaderServerTimeSource.TryComputeServerUtc("2026-09-14 08:00:00", null, TimeSpan.Zero, out _));

            Assert.IsTrue(HttpDateHeaderServerTimeSource.TryComputeServerUtc(MondayMorning, "abc", TimeSpan.Zero, out DateTime serverUtc));
            Assert.AreEqual(new DateTime(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc), serverUtc);
        }
    }

    [TestFixture]
    public class JsonLiveEventCalendarParserTests
    {
        [Test]
        public void ParsesEvents_ZuluOffsetAndMissingZoneAllMeanUtc()
        {
            const string json = "{\"events\":[" +
                                "{\"id\":\"lava-001\",\"type\":\"lava-quest\",\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"2026-09-17T00:00:00Z\",\"configKey\":\"lava_v2\"}," +
                                "{\"id\":\"hunt-001\",\"type\":\"treasure-hunt\",\"startUtc\":\"2026-09-15T07:00:00+07:00\",\"endUtc\":\"2026-09-16T00:00:00\"}" +
                                "]}";

            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse(json);

            Assert.IsFalse(result.HasProblems, string.Join("\n", result.Problems));
            Assert.AreEqual(2, result.Calendar.Instances.Count);
            Assert.AreEqual("lava_v2", result.Calendar.Instances[0].ConfigKey);

            LiveEventInstance hunt = result.Calendar.GetInstances("treasure-hunt", DateTime.MinValue, DateTime.MaxValue)[0];
            Assert.AreEqual(new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc), hunt.StartUtc, "+07:00 đổi về UTC");
            Assert.AreEqual(new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc), hunt.EndUtc, "Không ghi múi giờ = UTC");
            Assert.AreEqual(string.Empty, hunt.ConfigKey);
        }

        [Test]
        public void SkipsBrokenEntries_KeepsGoodOnes_ReportsEachProblem()
        {
            const string json = "{\"events\":[" +
                                "{\"id\":\"ok\",\"type\":\"hunt\",\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"2026-09-15T00:00:00Z\"}," +
                                "{\"type\":\"hunt\",\"startUtc\":\"2026-09-20T00:00:00Z\",\"endUtc\":\"2026-09-21T00:00:00Z\"}," +
                                "{\"id\":\"bad-date\",\"type\":\"hunt\",\"startUtc\":\"14/09/2026\",\"endUtc\":\"2026-09-15T00:00:00Z\"}," +
                                "{\"id\":\"backwards\",\"type\":\"hunt\",\"startUtc\":\"2026-09-25T00:00:00Z\",\"endUtc\":\"2026-09-24T00:00:00Z\"}," +
                                "{\"id\":\"overlap\",\"type\":\"hunt\",\"startUtc\":\"2026-09-14T12:00:00Z\",\"endUtc\":\"2026-09-16T00:00:00Z\"}" +
                                "]}";

            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse(json);

            Assert.AreEqual(1, result.Calendar.Instances.Count);
            Assert.AreEqual("ok", result.Calendar.Instances[0].EventId);
            Assert.AreEqual(4, result.Problems.Count, string.Join("\n", result.Problems));
        }

        [Test]
        public void BrokenJson_EmptyCalendarWithProblem_BlankTextIsNotAProblem()
        {
            LiveEventCalendarParseResult broken = JsonLiveEventCalendarParser.Parse("{ not json");
            Assert.AreEqual(0, broken.Calendar.Instances.Count);
            Assert.IsTrue(broken.HasProblems);

            Assert.IsTrue(JsonLiveEventCalendarParser.Parse("{}").HasProblems, "Thiếu mảng events");
            Assert.IsFalse(JsonLiveEventCalendarParser.Parse("  ").HasProblems, "Remote config chưa có key = chưa có event, không phải lỗi");
        }
    }

    [TestFixture]
    public class PlayerPrefsLiveOpsTextStoreTests
    {
        private const string KeyPrefix = "dreamtech.liveops.tests.";
        private const string Key = "state";

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(KeyPrefix + Key);
        }

        [Test]
        public void RoundTrip_ThenDelete()
        {
            var store = new PlayerPrefsLiveOpsTextStore(KeyPrefix);
            Assert.IsFalse(store.TryRead(Key, out _));

            store.Write(Key, "format=1\nvalue=a");
            Assert.IsTrue(store.TryRead(Key, out string value));
            Assert.AreEqual("format=1\nvalue=a", value);

            store.Delete(Key);
            Assert.IsFalse(store.TryRead(Key, out _));
        }
    }
}
