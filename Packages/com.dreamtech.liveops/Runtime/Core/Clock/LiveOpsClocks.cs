using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DreamTech.LiveOps
{
    /// <summary>Giờ của máy. Không chống vặn giờ (<see cref="IsTrusted"/> luôn false) — dùng cho game offline hoặc lúc prototype.</summary>
    public sealed class SystemLiveOpsClock : ILiveOpsClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public bool IsTrusted => false;
    }

    /// <summary>Giờ do code đặt (test).</summary>
    public sealed class ManualLiveOpsClock : ILiveOpsClock
    {
        public ManualLiveOpsClock(DateTime utcNow, bool isTrusted = true)
        {
            UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
            IsTrusted = isTrusted;
        }

        public DateTime UtcNow { get; private set; }
        public bool IsTrusted { get; set; }

        public void Set(DateTime utcNow)
        {
            UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        }

        public void Advance(TimeSpan duration)
        {
            UtcNow += duration;
        }
    }

    /// <summary>Bọc một đồng hồ khác và cộng thêm độ lệch — cheat "tua tới cuối đợt" trong build thật.</summary>
    public sealed class OffsetLiveOpsClock : ILiveOpsClock
    {
        public OffsetLiveOpsClock(ILiveOpsClock inner)
        {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public ILiveOpsClock Inner { get; }
        public TimeSpan Offset { get; set; }
        public DateTime UtcNow => Inner.UtcNow + Offset;
        public bool IsTrusted => Inner.IsTrusted;

        public void Advance(TimeSpan duration)
        {
            Offset += duration;
        }
    }

    /// <summary>
    /// Bộ đếm đơn điệu dựa trên <see cref="Stopwatch"/>. Trên iOS/Android bộ đếm này KHÔNG chạy lúc máy ngủ, nên sau khi app quay
    /// lại từ nền phải đồng bộ lại — <see cref="SyncedLiveOpsClock.Invalidate"/> tồn tại vì lý do đó.
    /// </summary>
    public sealed class StopwatchElapsedTimeSource : IElapsedTimeSource
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public TimeSpan Elapsed => _stopwatch.Elapsed;
    }

    /// <summary>Nguồn giờ server bọc một hàm của game (vd endpoint riêng trả JSON).</summary>
    public sealed class DelegateServerTimeSource : IServerTimeSource
    {
        private readonly Func<CancellationToken, Task<DateTime>> _fetch;

        public DelegateServerTimeSource(Func<CancellationToken, Task<DateTime>> fetch)
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        public Task<DateTime> FetchUtcNowAsync(CancellationToken cancellationToken)
        {
            return _fetch(cancellationToken);
        }
    }

    /// <summary>Thử lần lượt từng nguồn, nguồn đầu tiên trả được giờ thắng. Mọi nguồn đều lỗi thì ném <see cref="AggregateException"/>.</summary>
    public sealed class FallbackServerTimeSource : IServerTimeSource
    {
        private readonly List<IServerTimeSource> _sources = new List<IServerTimeSource>();

        public FallbackServerTimeSource(IEnumerable<IServerTimeSource> sources)
        {
            if (sources != null)
            {
                foreach (IServerTimeSource source in sources)
                {
                    if (source != null) _sources.Add(source);
                }
            }
            if (_sources.Count == 0) throw new ArgumentException("Cần ít nhất một nguồn giờ.", nameof(sources));
        }

        public FallbackServerTimeSource(params IServerTimeSource[] sources) : this((IEnumerable<IServerTimeSource>)sources)
        {
        }

        public async Task<DateTime> FetchUtcNowAsync(CancellationToken cancellationToken)
        {
            var failures = new List<Exception>(_sources.Count);
            foreach (IServerTimeSource source in _sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return await source.FetchUtcNowAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
            throw new AggregateException("Không nguồn giờ server nào trả lời được.", failures);
        }
    }
}
