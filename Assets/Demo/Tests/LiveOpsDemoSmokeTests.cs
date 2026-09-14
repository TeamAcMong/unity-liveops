using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DreamTech.LiveOps.Demo.Tests
{
    /// <summary>
    /// Chạy bàn thử live-ops thật trong PlayMode: PlayerPrefs, đồng hồ server bọc đồng hồ tua được, runner tự Refresh. Test
    /// EditMode của package gọi thẳng API; ở đây kiểm cùng luồng đó qua đúng những component game sẽ dùng.
    ///
    /// <para>Không phụ thuộc mạng: đồng bộ giờ thất bại thì đồng hồ dùng giờ máy, mọi bước vẫn chạy như nhau.</para>
    /// </summary>
    public sealed class LiveOpsDemoSmokeTests
    {
        private const string SceneName = "LiveOpsDemo";
        private const float TimeoutSeconds = 10f;
        private const string Race = LiveOpsDemo.RaceEventType;
        private const string Hunt = LiveOpsDemo.HuntEventType;

        // Loại chỉ có trong asset lịch mẫu (Assets/Demo/LiveOps/Calendars/Main.asset) — JSON remote giả lập của demo không có.
        private const string LavaQuest = "lava-quest";
        private const string WeeklyPass = "weekly-pass";
        private const string SampleWeeklyPassIdPrefix = "pass-";

        // Khoảng giờ chứa mọi đợt cố định của asset mẫu — hỏi lịch theo khoảng để test không phụ thuộc hôm nay là ngày nào.
        private static readonly DateTime SampleRangeStartUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime SampleRangeEndUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

        // Luật đua chỉ có trong JSON của test: tiền tố, chu kỳ và thời lượng đều khác asset (24/20 giờ, "sky-race-") và khác
        // inspector của demo, nên đợt đua mang các giá trị này chỉ có thể đến từ luật lặp trong JSON định dạng 2.
        private const string JsonRaceIdPrefix = "race-json-";
        private const string JsonRaceConfigKey = "sky_race_json";
        private const int JsonRacePeriodHours = 6;
        private const int JsonRaceActiveHours = 2;
        private static readonly string Format2RaceOnlyJson =
            "{\"version\":2,\"recurring\":[{\"type\":\"" + Race + "\",\"anchorUtc\":\"2026-01-05T00:00:00Z\",\"idPrefix\":\"" +
            JsonRaceIdPrefix + "\",\"periodHours\":" + JsonRacePeriodHours.ToString(CultureInfo.InvariantCulture) +
            ",\"activeHours\":" + JsonRaceActiveHours.ToString(CultureInfo.InvariantCulture) +
            ",\"configKey\":\"" + JsonRaceConfigKey + "\"}],\"events\":[]}";

        private LiveOpsDemo _demo;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return LoadDemo();
            // PlayerPrefs sống qua các lần chạy: mỗi test bắt đầu từ trạng thái sạch.
            _demo.ResetEverything();
            // Giờ thật có thể rơi vào lúc nghỉ giữa hai đợt đua — tua tới đợt kế tiếp cho chắc.
            _demo.Panel.AdvanceToStart(Race);
            Assert.IsTrue(_demo.System.GetStatus(Race).IsActive, "Phải có đợt đua đang chạy trước khi test");
        }

        [UnityTest]
        public IEnumerator AddingPoints_JoinsRace_ProgressSurvivesSceneReload()
        {
            Assert.IsTrue(_demo.Panel.AddPoints(Race, 30).IsAdded);
            TimeSpan offset = _demo.Clock.Offset;

            yield return LoadDemo();
            _demo.Clock.Offset = offset; // tua giờ không được lưu — đặt lại để còn trong cùng đợt

            LiveEventStatus status = _demo.System.GetStatus(Race);
            Assert.IsTrue(status.IsJoined);
            Assert.AreEqual(30, status.Record.Points);
        }

        [UnityTest]
        public IEnumerator TreasureHunt_NeedsJoin_GoalClaimPaysOnce()
        {
            Assert.AreEqual(LiveEventProgressStatus.NotJoined, _demo.Panel.AddPoints(Hunt, 10).Status);
            Assert.AreEqual(LiveEventJoinStatus.Joined, _demo.Panel.Join(Hunt).Status);
            Assert.IsTrue(_demo.Panel.AddPoints(Hunt, 100).IsAdded);

            Assert.AreEqual(LiveOpsClaimStatus.Granted, _demo.ClaimHuntGoal().Status);
            Assert.AreEqual(LiveOpsClaimStatus.AlreadyClaimed, _demo.ClaimHuntGoal().Status);
            Assert.AreEqual(30, _demo.Wallet["coin"]);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LowLevelPlayer_CannotJoinHunt()
        {
            _demo.SetPlayerLevel(0);
            Assert.AreEqual(LiveEventJoinStatus.NotEligible, _demo.Panel.Join(Hunt).Status);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RaceEnd_RunnerFinalizesAndPaysChest_ResultWaitsForAcknowledge()
        {
            Assert.IsTrue(_demo.Panel.AddPoints(Race, 100).IsAdded);
            string raceId = _demo.System.GetStatus(Race).Record.EventId;

            // Tua qua giờ kết thúc mà KHÔNG gọi Refresh — runner phải tự khép đợt ở nhịp kế tiếp.
            _demo.Clock.Advance(_demo.System.GetStatus(Race).TimeLeft + TimeSpan.FromSeconds(1));
            _demo.Runner.RefreshIntervalSeconds = 0.1f;
            yield return WaitUntil(() => _demo.Wallet.ContainsKey("coin"), "runner khép đợt và phát rương");

            Assert.AreEqual(100, _demo.Wallet["coin"]);
            Assert.AreEqual(1, _demo.Wallet["booster.magnet"]);

            IReadOnlyList<LiveEventRecord> pending = _demo.System.GetPendingResults();
            Assert.AreEqual(1, pending.Count);
            Assert.AreEqual(raceId, pending[0].EventId);
            Assert.IsTrue(_demo.Panel.AcknowledgeResult(raceId));
            Assert.AreEqual(0, _demo.System.GetPendingResults().Count);
        }

        [UnityTest]
        public IEnumerator BrokenCalendarEntry_IsDropped_AndReported()
        {
            Assert.AreEqual(1, _demo.Panel.CalendarProblems.Count, string.Join("\n", _demo.Panel.CalendarProblems));
            Assert.IsTrue(_demo.System.GetStatus(Hunt).IsActive, "Mục hỏng không kéo theo mục tốt");
            yield return null;
        }

        [UnityTest]
        public IEnumerator CalendarAsset_IsDefaultWhenRemoteEmpty()
        {
            LiveEventCalendarAsset asset = _demo.CalendarAsset;
            Assert.IsNotNull(asset, "Scene chưa gán asset lịch — dựng lại bằng Tools/DreamTech/LiveOps/Demo/Build Demo Scene.");
            Assert.IsFalse(_demo.CalendarResult.CameFromDefaultCalendar, "JSON remote giả lập có chữ thì không được rơi về asset");

            _demo.UseRemoteCalendarJson(string.Empty);

            Assert.IsTrue(_demo.CalendarResult.CameFromDefaultCalendar);
            // Cùng Problems với chính asset (mục giờ sai + đợt chồng giờ bị bỏ) — panel báo đúng thứ game đọc.
            CollectionAssert.AreEqual(asset.ToParseResult().Problems, _demo.Panel.CalendarProblems);

            // Đợt cố định của asset: lava-quest-2026-10 giờ kết thúc hỏng bị bỏ, hunt-0916-bonus chồng giờ hunt-0914 bị bỏ, đợt săn
            // "hôm nay" của JSON remote giả lập không còn.
            CollectionAssert.AreEqual(new[] { "lava-quest-2026-09a", "lava-quest-2026-09b" }, EventIdsBetweenSampleRange(LavaQuest));
            CollectionAssert.AreEqual(new[] { "hunt-0914" }, EventIdsBetweenSampleRange(Hunt));

            // Luật lặp của asset: weekly-pass chạy 168/168 giờ nên lúc nào cũng có đợt, id theo tiền tố của nháp.
            LiveEventStatus weeklyPass = _demo.System.GetStatus(WeeklyPass);
            Assert.IsTrue(weeklyPass.IsActive);
            AssertStartsWith(SampleWeeklyPassIdPrefix, weeklyPass.Instance.EventId);

            // Loại event đăng ký từ asset (WithEventTypesFrom), đúng thứ tự làn của asset.
            IReadOnlyList<LiveEventTypeDefinition> assetTypes = asset.ToDocument().EventTypes;
            var assetTypeIds = new List<string>(assetTypes.Count);
            foreach (LiveEventTypeDefinition type in assetTypes) assetTypeIds.Add(type.TypeId);
            CollectionAssert.AreEqual(assetTypeIds, _demo.System.EventTypes);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Format2Json_RecurringRuleDrivesRace()
        {
            _demo.UseRemoteCalendarJson(Format2RaceOnlyJson);

            LiveEventCalendarParseResult result = _demo.CalendarResult;
            Assert.IsFalse(result.CameFromDefaultCalendar, "JSON đọc được thì không được dùng asset");
            Assert.AreEqual(2, result.FormatVersion);
            Assert.AreEqual(0, _demo.Panel.CalendarProblems.Count, string.Join("\n", _demo.Panel.CalendarProblems));
            Assert.AreEqual(1, result.RecurringCalendars.Count);

            _demo.Panel.AdvanceToStart(Race);
            LiveEventStatus first = _demo.System.GetStatus(Race);
            Assert.IsTrue(first.IsActive, "Luật lặp trong JSON phải cho đợt đua");
            AssertStartsWith(JsonRaceIdPrefix, first.Instance.EventId);
            Assert.AreEqual(TimeSpan.FromHours(JsonRaceActiveHours), first.Instance.Duration);
            Assert.AreEqual(JsonRaceConfigKey, first.Instance.ConfigKey);

            // Tiến độ ghi vào đúng đợt của luật JSON.
            Assert.IsTrue(_demo.Panel.AddPoints(Race, 10).IsAdded);
            Assert.AreEqual(first.Instance.EventId, _demo.System.GetStatus(Race).Record.EventId);

            // Đợt kế theo chu kỳ của JSON, không phải 24 giờ của asset hay inspector.
            _demo.Panel.AdvanceToEnd(Race);
            _demo.Panel.AdvanceToStart(Race);
            LiveEventStatus next = _demo.System.GetStatus(Race);
            Assert.IsTrue(next.IsActive);
            AssertStartsWith(JsonRaceIdPrefix, next.Instance.EventId);
            Assert.AreEqual(TimeSpan.FromHours(JsonRacePeriodHours), next.Instance.StartUtc - first.Instance.StartUtc);
            yield return null;
        }

        // So thứ tự byte (Ordinal): StringAssert.StartsWith so theo culture, id đợt là định danh chứ không phải chữ hiển thị.
        private static void AssertStartsWith(string expectedPrefix, string actual)
        {
            Assert.IsTrue(actual != null && actual.StartsWith(expectedPrefix, StringComparison.Ordinal),
                          "Cần bắt đầu bằng '" + expectedPrefix + "', thấy '" + actual + "'");
        }

        private List<string> EventIdsBetweenSampleRange(string eventType)
        {
            IReadOnlyList<LiveEventInstance> instances = _demo.System.Calendar.GetInstances(eventType, SampleRangeStartUtc, SampleRangeEndUtc);
            var eventIds = new List<string>(instances.Count);
            foreach (LiveEventInstance instance in instances) eventIds.Add(instance.EventId);
            return eventIds;
        }

        private IEnumerator LoadDemo()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            _demo = UnityEngine.Object.FindAnyObjectByType<LiveOpsDemo>();
            Assert.IsNotNull(_demo, "Scene thiếu LiveOpsDemo — dựng lại bằng Tools/DreamTech/LiveOps/Demo/Build Demo Scene.");
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string what)
        {
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) Assert.Fail("Quá " + TimeoutSeconds + "s mà chưa thấy: " + what);
                yield return null;
            }
        }
    }
}
