using System;
using System.Threading;
using System.Threading.Tasks;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Đồng hồ chống vặn giờ máy: lấy giờ server một lần rồi cộng thời gian trôi đơn điệu, không đọc giờ máy nữa.
    ///
    /// <para><b>Tin được</b> (<see cref="IsTrusted"/>) từ lúc <see cref="SyncAsync"/> thành công tới lúc <see cref="Invalidate"/>.
    /// Game gọi Invalidate khi app vào nền vì bộ đếm đơn điệu của iOS/Android không chạy lúc máy ngủ, rồi đồng bộ lại khi app quay
    /// lại. <c>LiveOpsUnityRunner</c> của package làm sẵn hai việc này.</para>
    ///
    /// <para><b>Chưa tin được</b> (chưa đồng bộ, mất mạng, vừa quay lại từ nền): ước lượng bằng giờ máy + độ lệch đo ở lần đồng
    /// bộ trước, và KHÔNG BAO GIỜ trả giờ sớm hơn mốc tin được lớn nhất từng thấy. Mốc đó lưu qua các lần mở app, nên vặn giờ máy
    /// lùi lại để chơi lại đợt cũ bị chặn. Giờ chưa tin được không bao giờ nâng mốc — vặn giờ tới rồi mới có mạng không làm
    /// đồng hồ bị kẹt ở tương lai.</para>
    ///
    /// <para>Chỉ dùng trên main thread.</para>
    /// </summary>
    public sealed class SyncedLiveOpsClock : ILiveOpsClock
    {
        public const string DefaultStoreKey = "liveops.clock";
        private const int CurrentFormat = 1;

        private readonly IServerTimeSource _serverTimeSource;
        private readonly IElapsedTimeSource _elapsedTimeSource;
        private readonly Func<DateTime> _deviceUtcNow;
        private readonly ILiveOpsTextStore _textStore;
        private readonly string _storeKey;

        private bool _hasAnchor;
        private bool _isInvalidated;
        private int _invalidationCount;
        private DateTime _anchorServerUtc;
        private TimeSpan _anchorElapsed;
        private Task<bool> _activeSync;

        /// <param name="textStore">Nơi lưu độ lệch và mốc tin được. Để null thì mỗi lần mở app bắt đầu lại từ giờ máy.</param>
        /// <param name="elapsedTimeSource">Mặc định <see cref="StopwatchElapsedTimeSource"/>.</param>
        /// <param name="deviceUtcNow">Mặc định <see cref="DateTime.UtcNow"/>; test truyền giờ giả.</param>
        public SyncedLiveOpsClock(IServerTimeSource serverTimeSource, ILiveOpsTextStore textStore = null,
                                  string storeKey = DefaultStoreKey, IElapsedTimeSource elapsedTimeSource = null,
                                  Func<DateTime> deviceUtcNow = null)
        {
            if (string.IsNullOrEmpty(storeKey)) throw new ArgumentException("Khoá lưu không được rỗng.", nameof(storeKey));
            _serverTimeSource = serverTimeSource ?? throw new ArgumentNullException(nameof(serverTimeSource));
            _textStore = textStore;
            _storeKey = storeKey;
            _elapsedTimeSource = elapsedTimeSource ?? new StopwatchElapsedTimeSource();
            _deviceUtcNow = deviceUtcNow ?? (() => DateTime.UtcNow);
            TrustedHighWaterUtc = DateTime.MinValue;
            Load();
        }

        public bool IsTrusted => _hasAnchor && !_isInvalidated;

        public DateTime UtcNow
        {
            get
            {
                if (IsTrusted) return _anchorServerUtc + (_elapsedTimeSource.Elapsed - _anchorElapsed);
                DateTime estimate = DateTime.SpecifyKind(_deviceUtcNow(), DateTimeKind.Utc) + LastKnownDeviceOffset;
                return estimate < TrustedHighWaterUtc ? TrustedHighWaterUtc : estimate;
            }
        }

        /// <summary>Giờ server trừ giờ máy, đo ở lần đồng bộ thành công gần nhất (kể cả của lần mở app trước).</summary>
        public TimeSpan LastKnownDeviceOffset { get; private set; }

        /// <summary>Giờ tin được lớn nhất từng thấy; giờ chưa tin được không bao giờ trả sớm hơn mốc này.</summary>
        public DateTime TrustedHighWaterUtc { get; private set; }

        /// <summary>Giờ server lấy được ở lần đồng bộ thành công gần nhất trong phiên này; null nếu chưa đồng bộ được.</summary>
        public DateTime? LastSyncedServerUtc { get; private set; }

        /// <summary>Lý do lần đồng bộ gần nhất thất bại; null nếu lần gần nhất thành công hoặc chưa thử.</summary>
        public string LastSyncError { get; private set; }

        public bool IsSyncing => _activeSync != null && !_activeSync.IsCompleted;

        /// <summary>Đồng hồ vừa chuyển giữa tin được và chưa tin được — hệ thống nên Refresh.</summary>
        public event Action TrustChanged;

        /// <summary>
        /// Lấy giờ server và neo đồng hồ vào đó. Trả false khi nguồn giờ lỗi (lỗi ghi ở <see cref="LastSyncError"/>). Gọi chồng nhau
        /// thì dùng chung một lần lấy. Chỉ ném khi bị huỷ.
        /// </summary>
        public Task<bool> SyncAsync(CancellationToken cancellationToken)
        {
            if (IsSyncing) return _activeSync;
            _activeSync = SyncCoreAsync(cancellationToken);
            return _activeSync;
        }

        /// <summary>
        /// Thôi tin giờ hiện tại (app vào nền). Mốc tin được được chốt và lưu trước khi thôi tin. Lần đồng bộ đang chờ phản hồi cũng
        /// bị bỏ khi về tới: nó được neo vào bộ đếm đã dừng trong lúc máy ngủ nên sẽ trả giờ cũ.
        /// </summary>
        public void Invalidate()
        {
            _invalidationCount++;
            if (!IsTrusted) return;
            RaiseHighWater(UtcNow);
            _isInvalidated = true;
            Save();
            TrustChanged?.Invoke();
        }

        /// <summary>Xoá độ lệch và mốc đã lưu (cheat, test). Không đổi trạng thái tin được của phiên hiện tại.</summary>
        public void DebugForgetPersistedState()
        {
            LastKnownDeviceOffset = TimeSpan.Zero;
            TrustedHighWaterUtc = DateTime.MinValue;
            _textStore?.Delete(_storeKey);
        }

        private async Task<bool> SyncCoreAsync(CancellationToken cancellationToken)
        {
            int invalidationCountAtStart = _invalidationCount;
            DateTime serverUtc;
            try
            {
                serverUtc = await _serverTimeSource.FetchUtcNowAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LastSyncError = exception.GetBaseException().Message;
                return false;
            }

            if (invalidationCountAtStart != _invalidationCount)
            {
                LastSyncError = "Bỏ kết quả đồng bộ: app đã vào nền trong lúc chờ phản hồi.";
                return false;
            }

            serverUtc = DateTime.SpecifyKind(serverUtc, DateTimeKind.Utc);
            bool wasTrusted = IsTrusted;
            _anchorServerUtc = serverUtc;
            _anchorElapsed = _elapsedTimeSource.Elapsed;
            _hasAnchor = true;
            _isInvalidated = false;
            LastKnownDeviceOffset = serverUtc - DateTime.SpecifyKind(_deviceUtcNow(), DateTimeKind.Utc);
            LastSyncedServerUtc = serverUtc;
            LastSyncError = null;
            RaiseHighWater(serverUtc);
            Save();
            if (!wasTrusted) TrustChanged?.Invoke();
            return true;
        }

        private void RaiseHighWater(DateTime trustedUtc)
        {
            if (trustedUtc > TrustedHighWaterUtc) TrustedHighWaterUtc = trustedUtc;
        }

        private void Load()
        {
            if (_textStore == null || !_textStore.TryRead(_storeKey, out string text)) return;
            if (!LiveOpsTextRecord.TryDecode(text, out LiveOpsTextRecord record) || record.Format != CurrentFormat) return;
            LastKnownDeviceOffset = TimeSpan.FromTicks(record.GetLong("offsetTicks", 0));
            TrustedHighWaterUtc = record.GetDateTime("highWater", DateTime.MinValue);
        }

        private void Save()
        {
            if (_textStore == null) return;
            var record = new LiveOpsTextRecord(CurrentFormat);
            record.SetLong("offsetTicks", LastKnownDeviceOffset.Ticks);
            record.SetDateTime("highWater", TrustedHighWaterUtc);
            _textStore.Write(_storeKey, record.Encode());
        }
    }
}
