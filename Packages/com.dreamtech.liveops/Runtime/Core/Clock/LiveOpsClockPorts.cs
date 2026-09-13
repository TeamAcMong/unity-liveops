using System;
using System.Threading;
using System.Threading.Tasks;

namespace DreamTech.LiveOps
{
    /// <summary>Giờ hiện tại (UTC) mà mọi quyết định của live-ops dựa vào.</summary>
    public interface ILiveOpsClock
    {
        DateTime UtcNow { get; }

        /// <summary>
        /// Giờ này có chống được việc người chơi vặn giờ máy không. Giờ máy luôn là false; <see cref="SyncedLiveOpsClock"/> là true
        /// từ lúc đồng bộ server thành công tới lúc app vào nền. <see cref="LiveOpsSettings.FinalizeRequiresTrustedClock"/> dựa vào đây.
        /// </summary>
        bool IsTrusted { get; }
    }

    /// <summary>Nguồn giờ server. Lỗi mạng hay phản hồi hỏng thì NÉM exception — không trả giờ máy thay thế.</summary>
    public interface IServerTimeSource
    {
        /// <summary>Giờ UTC của server tại lúc phản hồi về tới máy (nguồn tự bù độ trễ nếu bù được).</summary>
        Task<DateTime> FetchUtcNowAsync(CancellationToken cancellationToken);
    }

    /// <summary>Thời gian trôi đơn điệu kể từ một mốc bất kỳ — không đổi khi người chơi vặn giờ máy.</summary>
    public interface IElapsedTimeSource
    {
        TimeSpan Elapsed { get; }
    }
}
