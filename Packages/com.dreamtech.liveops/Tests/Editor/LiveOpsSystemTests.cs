using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public class LiveOpsSystemTests
    {
        private const string Race = LiveOpsTestFactory.RaceType;
        private const string Hunt = LiveOpsTestFactory.HuntType;
        private static readonly LiveEventInstance FirstHunt = LiveOpsTestFactory.FirstHunt;

        [Test]
        public void Build_RequiresCalendarAndEventType_RejectsDuplicateType()
        {
            Assert.Throws<InvalidOperationException>(() => new LiveOpsSystemBuilder("x").WithEventType(Race).Build());
            Assert.Throws<InvalidOperationException>(() => new LiveOpsSystemBuilder("x").WithCalendar(FixedLiveEventCalendar.Empty).Build());
            Assert.Throws<ArgumentException>(() => new LiveOpsSystemBuilder("x").WithEventType(Race).WithEventType(Race));
        }

        [Test]
        public void GetStatus_ReportsActiveAndUpcoming_UnknownTypeThrows()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();

            LiveEventStatus race = scenario.System.GetStatus(Race);
            Assert.IsTrue(race.IsActive);
            Assert.AreEqual("sky-race-0", race.Instance.EventId);
            Assert.AreEqual(TimeSpan.FromHours(60), race.TimeLeft);
            Assert.IsFalse(race.IsJoined);

            LiveEventStatus hunt = scenario.System.GetStatus(Hunt);
            Assert.IsTrue(hunt.IsUpcoming);
            Assert.AreEqual(TimeSpan.FromHours(12), hunt.TimeUntilStart);

            Assert.Throws<ArgumentException>(() => scenario.System.GetStatus("not-registered"));
        }

        [Test]
        public void AddPoints_AutoJoinsActiveEvent_PersistsImmediately()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();

            LiveEventProgressOutcome outcome = scenario.System.AddPoints(Race, 30);

            Assert.IsTrue(outcome.IsAdded);
            Assert.AreEqual(30, outcome.Record.Points);
            Assert.AreEqual(scenario.Clock.UtcNow, outcome.Record.JoinedUtc);
            Assert.AreEqual(30, scenario.Rebuild().GetStatus(Race).Record.Points, "Mở lại app vẫn còn điểm");
        }

        [Test]
        public void AddPoints_InCooldownGap_NoActiveEvent()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            scenario.Clock.Set(LiveOpsTestFactory.Anchor.AddDays(5));

            Assert.AreEqual(LiveEventProgressStatus.NoActiveEvent, scenario.System.AddPoints(Race, 10).Status);
            Assert.IsTrue(scenario.System.GetStatus(Race).IsUpcoming, "Lúc nghỉ, trạng thái báo đợt kế tiếp");
        }

        [Test]
        public void ExplicitJoinType_RequiresJoinAndEligibility_JoinedPlayerKeepsProgressing()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            scenario.Clock.Set(FirstHunt.StartUtc.AddHours(1));
            LiveOpsSystem system = scenario.System;

            Assert.AreEqual(LiveEventProgressStatus.NotJoined, system.AddPoints(Hunt, 5).Status);

            scenario.IsHuntEligible = false;
            Assert.AreEqual(LiveEventJoinStatus.NotEligible, system.TryJoin(Hunt).Status);
            Assert.AreEqual(LiveEventProgressStatus.NotEligible, system.AddPoints(Hunt, 5).Status);

            scenario.IsHuntEligible = true;
            Assert.AreEqual(LiveEventJoinStatus.Joined, system.TryJoin(Hunt).Status);
            Assert.AreEqual(LiveEventJoinStatus.AlreadyJoined, system.TryJoin(Hunt).Status);

            scenario.IsHuntEligible = false;
            Assert.IsTrue(system.AddPoints(Hunt, 5).IsAdded, "Đã vào rồi thì đổi điều kiện không đá người chơi ra");
        }

        [Test]
        public void AddPoints_SameGrantId_CountsOnce_NonPositiveRejected()
        {
            LiveOpsSystem system = LiveOpsScenario.Create().System;

            Assert.IsTrue(system.AddPoints(Race, 10, "win-level-57").IsAdded);
            LiveEventProgressOutcome duplicate = system.AddPoints(Race, 10, "win-level-57");

            Assert.AreEqual(LiveEventProgressStatus.DuplicateGrant, duplicate.Status);
            Assert.AreEqual(10, duplicate.Record.Points);
            Assert.AreEqual(LiveEventProgressStatus.InvalidAmount, system.AddPoints(Race, 0).Status);
        }

        [Test]
        public void EventEnd_FinalizesOnce_PaysCompletionReward_ResultWaitsForAcknowledge()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            string raceId = scenario.System.AddPoints(Race, 120).Record.EventId;

            scenario.AdvancePastRaceEnd();
            IReadOnlyList<LiveEventRecord> finalized = scenario.System.Refresh();
            scenario.System.Refresh();

            Assert.AreEqual(1, finalized.Count);
            Assert.AreEqual(1, scenario.Finalized.Count, "EventFinalized bắn đúng một lần");
            CollectionAssert.AreEqual(new[] { LiveOpsSystem.CompletionGrantPrefix + raceId }, scenario.Granter.GrantedIds);
            Assert.AreEqual("chest.gold", scenario.Granter.GrantedBundles[0].PresentationId);

            IReadOnlyList<LiveEventRecord> pending = scenario.System.GetPendingResults();
            Assert.AreEqual(1, pending.Count);
            Assert.AreEqual(120, pending[0].Points);
            Assert.IsTrue(scenario.System.AcknowledgeResult(raceId));
            Assert.IsFalse(scenario.System.AcknowledgeResult(raceId));
            Assert.AreEqual(0, scenario.System.GetPendingResults().Count);
        }

        [Test]
        public void EventThatEndedWhileAppWasClosed_FinalizesOnNextLaunch()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            scenario.System.AddPoints(Race, 40);
            scenario.Clock.Advance(TimeSpan.FromDays(20)); // app tắt 20 ngày

            scenario.Rebuild().Refresh();

            Assert.AreEqual(1, scenario.Finalized.Count);
            Assert.AreEqual("chest.regular", scenario.Granter.GrantedBundles[0].PresentationId);
        }

        [Test]
        public void LongAbsence_ResultStaysPending_RetentionCountsFromFinalize()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            string raceId = scenario.System.AddPoints(Race, 40).Record.EventId;
            scenario.Clock.Advance(TimeSpan.FromDays(45)); // lâu hơn hạn giữ mặc định (30 ngày) tính từ lúc đợt hết giờ

            LiveOpsSystem relaunched = scenario.Rebuild();
            relaunched.Refresh();
            relaunched.Refresh();

            Assert.AreEqual(1, relaunched.GetPendingResults().Count, "Quay lại sau thời gian dài vẫn thấy kết quả đợt vừa khép");
            Assert.AreEqual(scenario.Clock.UtcNow, relaunched.GetRecord(raceId).FinalizedUtc);

            scenario.Clock.Advance(LiveOpsSettings.DefaultRecordRetention + TimeSpan.FromMinutes(1));
            relaunched.Refresh();
            Assert.IsNull(relaunched.GetRecord(raceId), "Hết hạn giữ tính từ lúc khép thì mới dọn");
        }

        [Test]
        public void GranterNotReady_KeepsRewardQueuedAcrossRestart_DeliversOnce()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            scenario.Granter.IsReady = false;
            scenario.System.AddPoints(Race, 150);
            scenario.AdvancePastRaceEnd();
            scenario.System.Refresh();

            Assert.AreEqual(1, scenario.System.PendingRewardCount);
            Assert.AreEqual(1, scenario.Rebuild().PendingRewardCount, "Quà chờ sống qua lần mở app");

            scenario.Granter.IsReady = true;
            Assert.AreEqual(1, scenario.System.GrantPendingRewards());
            Assert.AreEqual(0, scenario.System.GrantPendingRewards());
            Assert.AreEqual(1, scenario.Granter.GrantedIds.Count);
        }

        [Test]
        public void GranterThrows_RaisesEvent_KeepsRewardQueued()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            var failedGrantIds = new List<string>();
            scenario.System.RewardGrantFailed += (grantId, exception) => failedGrantIds.Add(grantId);
            scenario.Granter.ThrowsOnGrant = true;
            scenario.System.AddPoints(Race, 150);
            scenario.AdvancePastRaceEnd();

            Assert.DoesNotThrow(() => scenario.System.Refresh());
            Assert.AreEqual(1, failedGrantIds.Count);
            Assert.AreEqual(1, scenario.System.PendingRewardCount);
        }

        [Test]
        public void ClaimReward_OncePerKey_DeferredRetryPaysSameBundle_ClosedAfterFinalize()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            LiveOpsSystem system = scenario.System;
            string raceId = system.AddPoints(Race, 50).Record.EventId;
            LiveOpsRewardBundle milestone = LiveOpsTestFactory.Coins(20);

            scenario.Granter.IsReady = false;
            Assert.AreEqual(LiveOpsClaimStatus.Deferred, system.ClaimReward(raceId, "milestone-1", milestone).Status);
            Assert.IsTrue(system.GetRecord(raceId).HasClaimed("milestone-1"), "Chốt ngay cả khi kho đồ chưa nhận");

            scenario.Granter.IsReady = true;
            Assert.AreEqual(LiveOpsClaimStatus.Granted, system.ClaimReward(raceId, "milestone-1", milestone).Status);
            Assert.AreEqual(LiveOpsClaimStatus.AlreadyClaimed, system.ClaimReward(raceId, "milestone-1", milestone).Status);
            Assert.AreEqual(1, scenario.Granter.GrantedIds.Count);

            scenario.AdvancePastRaceEnd();
            system.Refresh();
            Assert.AreEqual(LiveOpsClaimStatus.NotAvailable, system.ClaimReward(raceId, "milestone-2", milestone).Status);
            Assert.AreEqual(LiveOpsClaimStatus.NotAvailable, system.ClaimReward("never-joined", "milestone-1", milestone).Status);
        }

        [Test]
        public void CompletionRule_SeesClaimedKeys_AutoCollectsOnlyUnclaimed()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            scenario.RaceCompletion = record => record.HasClaimed("goal") ? LiveOpsRewardBundle.None : LiveOpsTestFactory.Coins(99, "goal.auto");
            string raceId = scenario.System.AddPoints(Race, 100).Record.EventId;
            scenario.System.ClaimReward(raceId, "goal", LiveOpsTestFactory.Coins(99, "goal.manual"));

            scenario.AdvancePastRaceEnd();
            scenario.System.Refresh();

            Assert.AreEqual(1, scenario.Granter.GrantedBundles.Count, "Đã bấm nhận thì cuối đợt không phát lại");
            Assert.AreEqual("goal.manual", scenario.Granter.GrantedBundles[0].PresentationId);
        }

        [Test]
        public void NextOccurrence_StartsWithFreshRecord_OldOneFinalizedFirst()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            scenario.System.AddPoints(Race, 70);
            scenario.Clock.Set(LiveOpsTestFactory.Anchor.AddDays(7).AddHours(1));

            LiveEventProgressOutcome outcome = scenario.System.AddPoints(Race, 5);

            Assert.AreEqual("sky-race-1", outcome.Record.EventId);
            Assert.AreEqual(5, outcome.Record.Points);
            Assert.AreEqual(70, scenario.System.GetRecord("sky-race-0").Points);
            Assert.IsTrue(scenario.System.GetRecord("sky-race-0").IsFinalized);
        }

        [Test]
        public void CalendarExtendsRunningEvent_RecordFollowsCalendar()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            scenario.Clock.Set(FirstHunt.StartUtc.AddHours(1));
            scenario.System.TryJoin(Hunt);

            var extended = new LiveEventInstance(FirstHunt.EventId, Hunt, FirstHunt.StartUtc, FirstHunt.EndUtc.AddDays(1));
            scenario.Calendar.Inner = new CompositeLiveEventCalendar(LiveOpsTestFactory.CreateRaceCalendar(),
                                                                     new FixedLiveEventCalendar(new[] { extended }));
            scenario.Clock.Set(FirstHunt.EndUtc.AddHours(1)); // quá giờ cũ, chưa tới giờ mới

            scenario.System.Refresh();
            LiveEventRecord record = scenario.System.GetRecord(FirstHunt.EventId);

            Assert.IsFalse(record.IsFinalized);
            Assert.AreEqual(extended.EndUtc, record.Instance.EndUtc);
            Assert.IsTrue(scenario.System.AddPoints(Hunt, 3).IsAdded);
        }

        [Test]
        public void CalendarDropsJoinedEvent_StopsProgress_StillFinalizesAtStoredEnd()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            scenario.Clock.Set(FirstHunt.StartUtc.AddHours(1));
            scenario.System.TryJoin(Hunt);
            scenario.Calendar.Inner = LiveOpsTestFactory.CreateRaceCalendar(); // remote config gỡ đợt săn

            Assert.AreEqual(LiveEventProgressStatus.NoActiveEvent, scenario.System.AddPoints(Hunt, 1).Status);

            scenario.Clock.Set(FirstHunt.EndUtc.AddMinutes(1));
            scenario.System.Refresh();
            Assert.IsTrue(scenario.System.GetRecord(FirstHunt.EventId).IsFinalized);
        }

        [Test]
        public void RewoundClock_CannotReplayFinishedEvent_EvenAfterRecordRetired()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create(new LiveOpsSettings(recordRetention: TimeSpan.FromDays(1)));
            DateTime duringRace = scenario.Clock.UtcNow;
            scenario.System.AddPoints(Race, 10);
            scenario.AdvancePastRaceEnd();
            scenario.System.Refresh();

            scenario.Clock.Set(duringRace);
            Assert.AreEqual(LiveEventProgressStatus.AlreadyFinished, scenario.System.AddPoints(Race, 10).Status);
            Assert.IsTrue(scenario.System.GetStatus(Race).IsFinished);

            scenario.Clock.Set(LiveOpsTestFactory.Anchor.AddDays(5)); // đợt 0 hết ở ngày 3, giữ bản ghi 1 ngày
            scenario.System.Refresh();
            Assert.IsNull(scenario.System.GetRecord("sky-race-0"), "Bản ghi cũ đã được dọn");

            scenario.Clock.Set(duringRace);
            Assert.AreEqual(LiveEventProgressStatus.AlreadyFinished, scenario.Rebuild().AddPoints(Race, 10).Status,
                            "Id đã dọn vẫn được nhớ qua lần mở app");
            Assert.AreEqual(LiveEventJoinStatus.AlreadyFinished, scenario.System.TryJoin(Race).Status);
        }

        [Test]
        public void FinalizeRequiresTrustedClock_WaitsUntilClockIsTrusted()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create(new LiveOpsSettings(finalizeRequiresTrustedClock: true));
            scenario.System.AddPoints(Race, 10);
            scenario.AdvancePastRaceEnd();
            scenario.Clock.IsTrusted = false;

            Assert.AreEqual(0, scenario.System.Refresh().Count);
            Assert.AreEqual(LiveEventProgressStatus.NoActiveEvent, scenario.System.AddPoints(Race, 10).Status,
                            "Chưa khép nhưng đã hết giờ theo đồng hồ thì vẫn không cộng điểm");

            scenario.Clock.IsTrusted = true;
            Assert.AreEqual(1, scenario.System.Refresh().Count);
        }

        [Test]
        public void CompletionRuleThrows_OtherEventsStillFinalize_FailedOneRetriesLater()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            scenario.Clock.Set(FirstHunt.StartUtc.AddHours(1));
            scenario.System.AddPoints(Race, 10);
            scenario.System.TryJoin(Hunt);
            var failedEventIds = new List<string>();
            scenario.System.CompletionRuleFailed += (record, exception) => failedEventIds.Add(record.EventId);
            bool isRuleBroken = true;
            scenario.RaceCompletion = record =>
            {
                if (isRuleBroken) throw new InvalidOperationException("Lỗi trong luật của game (giả lập)");
                return LiveOpsRewardBundle.None;
            };

            scenario.Clock.Set(LiveOpsTestFactory.Anchor.AddDays(4)); // cả hai đợt đều đã hết
            IReadOnlyList<LiveEventRecord> finalized = scenario.System.Refresh();

            Assert.AreEqual(1, finalized.Count);
            Assert.AreEqual(FirstHunt.EventId, finalized[0].EventId);
            CollectionAssert.AreEqual(new[] { "sky-race-0" }, failedEventIds);
            Assert.IsFalse(scenario.System.GetRecord("sky-race-0").IsFinalized);

            isRuleBroken = false;
            Assert.AreEqual(1, scenario.System.Refresh().Count);
        }

        [Test]
        public void CustomState_SurvivesRestart_LockedAfterFinalize()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            string raceId = scenario.System.AddPoints(Race, 1).Record.EventId;
            const string customState = "tiles=3\nboard=a=b\\c"; // xuống dòng, '=' và '\' phải đi qua được codec

            Assert.IsTrue(scenario.System.TrySetCustomState(raceId, customState));
            Assert.AreEqual(customState, scenario.Rebuild().GetRecord(raceId).CustomState);

            scenario.AdvancePastRaceEnd();
            scenario.System.Refresh();
            Assert.IsFalse(scenario.System.TrySetCustomState(raceId, "late"));
        }

        [Test]
        public void EventTypeRemovedFromGame_FinalizesWithoutReward_NotLeftPending()
        {
            LiveOpsScenario scenario = LiveOpsScenario.Create();
            scenario.Clock.Set(FirstHunt.StartUtc.AddHours(1));
            scenario.System.TryJoin(Hunt);

            // Bản game mới gỡ loại săn kho báu nhưng máy người chơi còn bản ghi cũ.
            LiveOpsSystem withoutHunt = new LiveOpsSystemBuilder(LiveOpsScenario.SystemId)
                                        .WithClock(scenario.Clock)
                                        .WithCalendar(scenario.Calendar)
                                        .WithTextStore(scenario.Store)
                                        .WithRewardGranter(scenario.Granter)
                                        .WithEventType(Race)
                                        .Build();
            scenario.Clock.Set(FirstHunt.EndUtc.AddMinutes(1));
            withoutHunt.Refresh();

            LiveEventRecord record = withoutHunt.GetRecord(FirstHunt.EventId);
            Assert.IsTrue(record.IsFinalized);
            Assert.IsFalse(record.IsPendingResult);
        }

        [Test]
        public void CorruptStoredState_StartsEmpty()
        {
            var store = new InMemoryLiveOpsTextStore();
            store.Write("liveops." + LiveOpsScenario.SystemId + ".state", "rác không phải bản ghi");

            LiveOpsScenario scenario = LiveOpsScenario.Create(store: store);

            Assert.AreEqual(0, scenario.System.GetRecords().Count);
            Assert.IsTrue(scenario.System.AddPoints(Race, 1).IsAdded);
        }

        [Test]
        public void TextRecord_RoundTripsEscapedValuesAndBundles_RejectsBrokenInput()
        {
            var bundle = new LiveOpsRewardBundle("chest", new[] { new LiveOpsRewardItem("coin", 5), new LiveOpsRewardItem("gem", 1) });
            var record = new LiveOpsTextRecord(3);
            record.SetString("custom", "a=b\nc\\d\r");
            record.SetBundle("reward", bundle);

            Assert.IsTrue(LiveOpsTextRecord.TryDecode(record.Encode(), out LiveOpsTextRecord decoded));
            Assert.AreEqual(3, decoded.Format);
            Assert.AreEqual("a=b\nc\\d\r", decoded.GetString("custom", null));
            Assert.AreEqual(bundle, decoded.GetBundle("reward"));
            Assert.IsFalse(LiveOpsTextRecord.TryDecode("format=1\nbroken\\x", out _));
            Assert.IsFalse(LiveOpsTextRecord.TryDecode("value=1", out _), "Thiếu format");
        }
    }
}
