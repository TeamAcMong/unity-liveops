using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine;

namespace DreamTech.LiveOps.Unity.Tests
{
    /// <summary>
    /// Hai nhánh chính sách Q-9 của <see cref="JsonLiveEventCalendarParser.ParseOrDefault(string, LiveEventCalendarAsset, LiveEventCalendarRemoteFailurePolicy)"/>
    /// khi JSON remote có chữ nhưng hỏng, và lời hứa không bao giờ trộn remote với asset khi JSON còn đọc được.
    /// </summary>
    [TestFixture]
    public sealed class LiveEventCalendarRemoteFailurePolicyTests
    {
        private const string BrokenJson = "{ \"version\": 2, \"events\": [ { \"id\": \"remote-1\" ";

        private static readonly DateTime SampleWindowStartUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime SampleWindowEndUtc = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);

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

        private static List<string> EventIdsInSampleWindow(ILiveEventCalendar calendar)
        {
            var eventIds = new List<string>();
            foreach (string eventType in new[] { "lava-quest", "sky-race", "star-tournament", "treasure-hunt", "weekly-pass", "remote-type" })
            {
                IReadOnlyList<LiveEventInstance> instances = calendar.GetInstances(eventType, SampleWindowStartUtc, SampleWindowEndUtc);
                for (int index = 0; index < instances.Count; index++) eventIds.Add(instances[index].EventId);
            }
            return eventIds;
        }

        [Test]
        public void KeepRemoteResult_BrokenJson_EmptyCalendarWithProblem()
        {
            LiveEventCalendarAsset asset = CreateDesignSampleAsset();

            LiveEventCalendarParseResult result =
                JsonLiveEventCalendarParser.ParseOrDefault(BrokenJson, asset, LiveEventCalendarRemoteFailurePolicy.KeepRemoteResult);
            LiveEventCalendarParseResult plainParse = JsonLiveEventCalendarParser.Parse(BrokenJson);

            Assert.IsFalse(result.CameFromDefaultCalendar);
            Assert.AreSame(FixedLiveEventCalendar.Empty, result.Calendar, "Đúng hành vi 0.1.0: lịch rỗng.");
            Assert.IsEmpty(EventIdsInSampleWindow(result.CombinedCalendar), "Không được lọt đợt nào của asset.");
            Assert.AreEqual(0, result.FormatVersion);
            Assert.AreEqual(1, result.Problems.Count, string.Join("\n", result.Problems));
            StringAssert.StartsWith("JSON lịch event hỏng: ", result.Problems[0]);
            CollectionAssert.AreEqual(plainParse.Problems, result.Problems, "KeepRemoteResult = như Parse.");
        }

        [Test]
        public void UseDefaultCalendar_BrokenJson_AssetCalendarPlusProblem()
        {
            LiveEventCalendarAsset asset = CreateDesignSampleAsset();
            LiveEventCalendarParseResult assetResult = asset.ToParseResult();
            string readErrorText = JsonLiveEventCalendarParser.ParseDocument(BrokenJson).ReadErrorText;

            LiveEventCalendarParseResult result =
                JsonLiveEventCalendarParser.ParseOrDefault(BrokenJson, asset, LiveEventCalendarRemoteFailurePolicy.UseDefaultCalendar);

            Assert.IsTrue(result.CameFromDefaultCalendar);
            Assert.AreEqual(2, result.FormatVersion);
            Assert.AreEqual(assetResult.Compilation.KeptCount, result.Compilation.KeptCount);
            CollectionAssert.AreEquivalent(EventIdsInSampleWindow(assetResult.CombinedCalendar), EventIdsInSampleWindow(result.CombinedCalendar));

            Assert.AreEqual(assetResult.Problems.Count + 1, result.Problems.Count, string.Join("\n", result.Problems));
            Assert.IsNotEmpty(readErrorText);
            Assert.AreEqual("JSON remote hỏng, dùng lịch mặc định trong asset: " + readErrorText, result.Problems[0],
                "Game phải log được vì sao người chơi đang thấy lịch trong build.");
            for (int index = 0; index < assetResult.Problems.Count; index++)
            {
                Assert.AreEqual(assetResult.Problems[index], result.Problems[index + 1]);
            }
        }

        [Test]
        public void BothPolicies_ReadableJsonWithBrokenEntries_NeverMixWithAsset()
        {
            LiveEventCalendarAsset asset = CreateDesignSampleAsset();
            const string readableJson = "{\"version\":2,\"events\":[" +
                                         "{\"id\":\"remote-1\",\"type\":\"remote-type\",\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"2026-09-16T00:00:00Z\"}," +
                                         "{\"id\":\"remote-broken\",\"type\":\"remote-type\",\"startUtc\":\"ngày mai\",\"endUtc\":\"2026-09-20T00:00:00Z\"}]}";

            foreach (LiveEventCalendarRemoteFailurePolicy policy in new[]
                     {
                         LiveEventCalendarRemoteFailurePolicy.KeepRemoteResult, LiveEventCalendarRemoteFailurePolicy.UseDefaultCalendar,
                     })
            {
                LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.ParseOrDefault(readableJson, asset, policy);

                Assert.IsFalse(result.CameFromDefaultCalendar, policy.ToString());
                CollectionAssert.AreEqual(new[] { "remote-1" }, EventIdsInSampleWindow(result.CombinedCalendar), policy.ToString());
                Assert.AreEqual(1, result.Problems.Count, policy + "\n" + string.Join("\n", result.Problems));
                Assert.AreEqual("Mục thứ 2 ('remote-broken'): startUtc không phải giờ ISO 8601: 'ngày mai' — bỏ qua.", result.Problems[0]);

                // JSON đọc được nhưng thiếu mảng vẫn là kết quả remote (lịch rỗng + Problem), không phải asset.
                LiveEventCalendarParseResult missingArrays = JsonLiveEventCalendarParser.ParseOrDefault("{}", asset, policy);
                Assert.IsFalse(missingArrays.CameFromDefaultCalendar, policy.ToString());
                Assert.IsEmpty(EventIdsInSampleWindow(missingArrays.CombinedCalendar), policy.ToString());
                Assert.AreEqual("JSON lịch event thiếu mảng \"events\".", missingArrays.Problems[0], policy.ToString());
            }
        }

        [Test]
        public void TwoArgumentOverload_UsesDefaultPolicyConstant()
        {
            // Q-9 đã chốt (0.2.0): mặc định là UseDefaultCalendar — ĐỔI HÀNH VI so với 0.1.0. Đổi mặc định lần nữa = đổi
            // hằng + test này + README + CHANGELOG (hai bản).
            Assert.AreEqual(LiveEventCalendarRemoteFailurePolicy.UseDefaultCalendar, JsonLiveEventCalendarParser.DefaultRemoteFailurePolicy);

            LiveEventCalendarAsset asset = CreateDesignSampleAsset();
            LiveEventCalendarParseResult twoArgument = JsonLiveEventCalendarParser.ParseOrDefault(BrokenJson, asset);
            LiveEventCalendarParseResult useDefaultCalendar =
                JsonLiveEventCalendarParser.ParseOrDefault(BrokenJson, asset, LiveEventCalendarRemoteFailurePolicy.UseDefaultCalendar);

            Assert.AreEqual(useDefaultCalendar.CameFromDefaultCalendar, twoArgument.CameFromDefaultCalendar);
            Assert.AreEqual(useDefaultCalendar.FormatVersion, twoArgument.FormatVersion);
            Assert.AreEqual(useDefaultCalendar.Compilation.KeptCount, twoArgument.Compilation.KeptCount);
            CollectionAssert.AreEqual(useDefaultCalendar.Problems, twoArgument.Problems);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => JsonLiveEventCalendarParser.ParseOrDefault(BrokenJson, asset, (LiveEventCalendarRemoteFailurePolicy)99));
        }

        [Test]
        public void TwoArgumentOverload_BrokenJson_UsesAssetCalendarAndStillReportsProblem()
        {
            // Khoá đúng hành vi user chốt ở Q-9, không chỉ khoá "đi theo hằng": người chơi vẫn thấy event của asset, và
            // dev vẫn đọc được vì sao (không nuốt lỗi).
            LiveEventCalendarAsset asset = CreateDesignSampleAsset();
            LiveEventCalendarParseResult assetResult = asset.ToParseResult();
            string readErrorText = JsonLiveEventCalendarParser.ParseDocument(BrokenJson).ReadErrorText;

            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.ParseOrDefault(BrokenJson, asset);

            Assert.IsTrue(result.CameFromDefaultCalendar);
            CollectionAssert.AreEquivalent(EventIdsInSampleWindow(assetResult.CombinedCalendar), EventIdsInSampleWindow(result.CombinedCalendar),
                "Mặc định 0.2.0: remote hỏng thì người chơi vẫn thấy đúng lịch trong build.");
            Assert.IsNotEmpty(EventIdsInSampleWindow(result.CombinedCalendar), "Mẫu thiết kế phải có đợt trong cửa sổ — nếu rỗng thì test không chứng minh gì.");
            Assert.AreEqual("JSON remote hỏng, dùng lịch mặc định trong asset: " + readErrorText, result.Problems[0]);

            // Nhánh cũ vẫn chọn tay được và vẫn khác mặc định — giữ được cho game không muốn đổi hành vi khi bump package.
            LiveEventCalendarParseResult keepRemote =
                JsonLiveEventCalendarParser.ParseOrDefault(BrokenJson, asset, LiveEventCalendarRemoteFailurePolicy.KeepRemoteResult);
            Assert.IsFalse(keepRemote.CameFromDefaultCalendar);
            Assert.AreSame(FixedLiveEventCalendar.Empty, keepRemote.Calendar);
            Assert.AreNotEqual(keepRemote.Problems[0], result.Problems[0], "Hai nhánh phải nói hai câu khác nhau.");
        }

        [Test]
        public void TwoArgumentOverload_BlankJson_UsesAssetWithoutExtraProblem()
        {
            // Chuỗi trắng KHÔNG phải lỗi (remote chưa đăng / chưa fetch kịp): dùng asset như 0.1.0 và KHÔNG thêm Problem —
            // để câu "JSON remote hỏng…" vẫn là dấu hiệu chắc chắn của một bản remote hỏng thật.
            LiveEventCalendarAsset asset = CreateDesignSampleAsset();
            LiveEventCalendarParseResult assetResult = asset.ToParseResult();

            foreach (string blankJson in new[] { null, string.Empty, "   \n\t" })
            {
                LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.ParseOrDefault(blankJson, asset);

                Assert.IsTrue(result.CameFromDefaultCalendar, "blank=" + (blankJson ?? "null"));
                CollectionAssert.AreEqual(assetResult.Problems, result.Problems, "blank=" + (blankJson ?? "null"));
                CollectionAssert.AreEquivalent(EventIdsInSampleWindow(assetResult.CombinedCalendar), EventIdsInSampleWindow(result.CombinedCalendar),
                    "blank=" + (blankJson ?? "null"));
            }
        }
    }
}
