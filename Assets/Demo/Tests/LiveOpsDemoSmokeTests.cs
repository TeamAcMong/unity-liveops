using System;
using System.Collections;
using System.Collections.Generic;
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
    public class LiveOpsDemoSmokeTests
    {
        private const string SceneName = "LiveOpsDemo";
        private const float TimeoutSeconds = 10f;
        private const string Race = LiveOpsDemo.RaceEventType;
        private const string Hunt = LiveOpsDemo.HuntEventType;

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
