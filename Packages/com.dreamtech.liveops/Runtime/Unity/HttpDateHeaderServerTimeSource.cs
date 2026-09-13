using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace DreamTech.LiveOps.Unity
{
    /// <summary>
    /// Lấy giờ server từ header <c>Date</c> của một request HEAD. Mọi web server đều trả header này nên không cần endpoint riêng —
    /// nên trỏ vào server hoặc CDN của chính game (vd host catalog Addressables) để không phụ thuộc dịch vụ thời gian bên thứ ba.
    ///
    /// <para>Độ chính xác khoảng ±1 giây: <c>Date</c> tính theo giây; nguồn cộng thêm <c>Age</c> (phản hồi lấy từ cache CDN mang
    /// Date của lúc tạo bản gốc) và nửa thời gian khứ hồi. Phản hồi lỗi HTTP (404, 405...) vẫn dùng được nếu có Date; lỗi kết nối
    /// thì ném exception.</para>
    /// </summary>
    public sealed class HttpDateHeaderServerTimeSource : IServerTimeSource
    {
        public const int DefaultTimeoutSeconds = 10;

        private readonly int _timeoutSeconds;

        public HttpDateHeaderServerTimeSource(string url, int timeoutSeconds = DefaultTimeoutSeconds)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentException("URL không được rỗng.", nameof(url));
            if (timeoutSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "Phải lớn hơn 0.");
            Url = url;
            _timeoutSeconds = timeoutSeconds;
        }

        public string Url { get; }

        /// <summary>Chỉ gọi trên main thread (UnityWebRequest).</summary>
        public async Task<DateTime> FetchUtcNowAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (UnityWebRequest request = UnityWebRequest.Head(Url))
            {
                request.timeout = _timeoutSeconds;
                request.SetRequestHeader("Cache-Control", "no-cache");

                Stopwatch roundTrip = Stopwatch.StartNew();
                await SendAsync(request, cancellationToken);
                roundTrip.Stop();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    throw new InvalidOperationException("Không lấy được giờ từ " + Url + ": " + request.error);
                }

                string dateHeader = request.GetResponseHeader("Date");
                if (!TryComputeServerUtc(dateHeader, request.GetResponseHeader("Age"), roundTrip.Elapsed, out DateTime serverUtc))
                {
                    throw new InvalidOperationException("Phản hồi từ " + Url + " không có header Date hợp lệ: '" + (dateHeader ?? "null") + "'.");
                }
                return serverUtc;
            }
        }

        /// <summary>Tính giờ server từ các header, tách riêng để test không cần mạng.</summary>
        internal static bool TryComputeServerUtc(string dateHeader, string ageHeader, TimeSpan roundTrip, out DateTime serverUtc)
        {
            serverUtc = default;
            if (string.IsNullOrEmpty(dateHeader)) return false;
            if (!DateTimeOffset.TryParseExact(dateHeader.Trim(), "r", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                                              out DateTimeOffset parsed))
            {
                return false;
            }

            DateTime utc = parsed.UtcDateTime;
            if (!string.IsNullOrEmpty(ageHeader) &&
                int.TryParse(ageHeader.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int ageSeconds) && ageSeconds > 0)
            {
                utc = utc.AddSeconds(ageSeconds);
            }
            if (roundTrip > TimeSpan.Zero) utc += TimeSpan.FromTicks(roundTrip.Ticks / 2);
            serverUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return true;
        }

        private static Task SendAsync(UnityWebRequest request, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            SynchronizationContext unityContext = SynchronizationContext.Current;
            CancellationTokenRegistration registration = default;
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    // Token có thể bị huỷ từ thread khác mà UnityWebRequest chỉ được đụng trên main thread. Post lên context của Unity
                    // cũng giữ đúng thứ tự: lệnh huỷ chạy trước continuation (cũng được post) — tức trước khi request bị Dispose.
                    if (unityContext != null) unityContext.Post(_ => AbortQuietly(request), null);
                    else AbortQuietly(request);
                    completion.TrySetCanceled(cancellationToken);
                });
            }

            operation.completed += _ =>
            {
                registration.Dispose();
                completion.TrySetResult(true);
            };
            return completion.Task;
        }

        private static void AbortQuietly(UnityWebRequest request)
        {
            try
            {
                request.Abort();
            }
            catch (Exception)
            {
                // Request đã xong hoặc đã Dispose: không còn gì để huỷ.
            }
        }
    }
}
