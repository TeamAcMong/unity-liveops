using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public class SyncedLiveOpsClockTests
    {
        private static readonly DateTime ServerNow = new DateTime(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc);

        private DateTime _deviceUtc;
        private ManualElapsedTimeSource _elapsed;
        private ScriptedServerTimeSource _server;
        private InMemoryLiveOpsTextStore _store;

        [SetUp]
        public void SetUp()
        {
            _deviceUtc = ServerNow.AddMinutes(-3); // máy chạy chậm 3 phút
            _elapsed = new ManualElapsedTimeSource();
            _server = new ScriptedServerTimeSource { ServerUtc = ServerNow };
            _store = new InMemoryLiveOpsTextStore();
        }

        [Test]
        public void BeforeFirstSync_UsesDeviceTime_Untrusted()
        {
            SyncedLiveOpsClock clock = CreateClock();

            Assert.IsFalse(clock.IsTrusted);
            Assert.AreEqual(_deviceUtc, clock.UtcNow);
        }

        [Test]
        public void AfterSync_FollowsServerPlusElapsed_IgnoresDeviceClockChanges()
        {
            SyncedLiveOpsClock clock = CreateClock();
            Assert.IsTrue(clock.SyncAsync(CancellationToken.None).Result);

            _elapsed.Advance(TimeSpan.FromMinutes(10));
            _deviceUtc = _deviceUtc.AddDays(-30); // người chơi vặn giờ máy lùi 30 ngày

            Assert.IsTrue(clock.IsTrusted);
            Assert.AreEqual(ServerNow.AddMinutes(10), clock.UtcNow);
            Assert.AreEqual(TimeSpan.FromMinutes(3), clock.LastKnownDeviceOffset);
        }

        [Test]
        public void SyncFailure_StaysUntrusted_ReportsError()
        {
            _server.FailsNextCall = true;
            SyncedLiveOpsClock clock = CreateClock();

            Assert.IsFalse(clock.SyncAsync(CancellationToken.None).Result);
            Assert.IsFalse(clock.IsTrusted);
            StringAssert.Contains("Mất mạng", clock.LastSyncError);
        }

        [Test]
        public void Invalidate_FallsBackToDevicePlusMeasuredOffset()
        {
            SyncedLiveOpsClock clock = CreateClock();
            clock.SyncAsync(CancellationToken.None).Wait();
            int trustChanges = 0;
            clock.TrustChanged += () => trustChanges++;

            clock.Invalidate();
            _deviceUtc = _deviceUtc.AddHours(5); // máy ngủ 5 tiếng, bộ đếm đơn điệu đứng yên

            Assert.IsFalse(clock.IsTrusted);
            Assert.AreEqual(1, trustChanges);
            Assert.AreEqual(ServerNow.AddHours(5), clock.UtcNow, "Giờ máy + 3 phút lệch đo được lúc đồng bộ");
        }

        [Test]
        public void RewindingDeviceClock_CannotGoBelowTrustedHighWater_EvenAfterRestart()
        {
            SyncedLiveOpsClock clock = CreateClock();
            clock.SyncAsync(CancellationToken.None).Wait();
            _elapsed.Advance(TimeSpan.FromHours(2));
            clock.Invalidate(); // app vào nền: chốt mốc 10:00

            _deviceUtc = ServerNow.AddDays(-7); // offline, vặn giờ lùi một tuần rồi mở lại app
            SyncedLiveOpsClock afterRestart = CreateClock();

            Assert.IsFalse(afterRestart.IsTrusted);
            Assert.AreEqual(ServerNow.AddHours(2), afterRestart.TrustedHighWaterUtc);
            Assert.AreEqual(ServerNow.AddHours(2), afterRestart.UtcNow);
        }

        [Test]
        public void UntrustedForwardTime_NeverRaisesHighWater()
        {
            SyncedLiveOpsClock clock = CreateClock();
            _deviceUtc = ServerNow.AddDays(10); // vặn giờ tới lúc chưa có mạng
            Assert.AreEqual(ServerNow.AddDays(10), clock.UtcNow);

            clock.SyncAsync(CancellationToken.None).Wait();

            Assert.AreEqual(ServerNow, clock.UtcNow, "Có mạng thì về đúng giờ server, không kẹt ở tương lai");
            Assert.AreEqual(ServerNow, clock.TrustedHighWaterUtc);
        }

        [Test]
        public void ConcurrentSyncCalls_ShareOneFetch()
        {
            using (new NoSynchronizationContextScope())
            {
                _server.HoldsResponses = true;
                SyncedLiveOpsClock clock = CreateClock();

                Task<bool> first = clock.SyncAsync(CancellationToken.None);
                Task<bool> second = clock.SyncAsync(CancellationToken.None);

                Assert.AreSame(first, second);
                Assert.AreEqual(1, _server.CallCount);
                _server.ReleaseHeldResponse();
                Assert.IsTrue(first.Result);
            }
        }

        [Test]
        public void GoingToBackgroundDuringSync_DiscardsStaleResponse()
        {
            using (new NoSynchronizationContextScope())
            {
                _server.HoldsResponses = true;
                SyncedLiveOpsClock clock = CreateClock();

                Task<bool> sync = clock.SyncAsync(CancellationToken.None);
                clock.Invalidate();
                _server.ReleaseHeldResponse();

                Assert.IsFalse(sync.Result, "Phản hồi neo vào bộ đếm đã dừng lúc máy ngủ — phải bỏ");
                Assert.IsFalse(clock.IsTrusted);
            }
        }

        [Test]
        public void FallbackSource_UsesFirstSourceThatAnswers_AllFailingThrows()
        {
            var broken = new ScriptedServerTimeSource { FailsNextCall = true };
            var fallback = new FallbackServerTimeSource(broken, _server);

            Assert.AreEqual(ServerNow, fallback.FetchUtcNowAsync(CancellationToken.None).Result);
            Assert.AreEqual(1, broken.CallCount);

            var allBroken = new FallbackServerTimeSource(new ScriptedServerTimeSource { FailsNextCall = true });
            Assert.Throws<AggregateException>(() => allBroken.FetchUtcNowAsync(CancellationToken.None).Wait());
        }

        private SyncedLiveOpsClock CreateClock()
        {
            return new SyncedLiveOpsClock(_server, _store, elapsedTimeSource: _elapsed, deviceUtcNow: () => _deviceUtc);
        }
    }
}
