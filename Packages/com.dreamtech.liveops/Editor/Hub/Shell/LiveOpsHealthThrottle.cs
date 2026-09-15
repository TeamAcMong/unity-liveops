using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Giữ kết quả <see cref="IHubSection.GetHealth"/> trong một cửa sổ thời gian (mặc định 3 giây theo
    /// <c>EditorApplication.timeSinceStartup</c>, 8.4). Vì sao: rail làm mới mỗi giây cho mọi màn; không giữ thì một màn có
    /// health tốn kém làm cả Editor giật. Sau mỗi sửa/kiểm/Undo, <see cref="InvalidateAll"/> tăng thế hệ toàn cục để mọi
    /// throttle tính lại ngay — không phải chờ hết cửa sổ mới thấy dấu đổi.
    /// </summary>
    internal sealed class LiveOpsHealthThrottle
    {
        public const double DefaultSeconds = 3.0;

        // Thế hệ chung của mọi throttle: InvalidateAll chỉ tăng một số, không phải giữ danh sách throttle (không rò tham chiếu
        // tới màn đã đóng).
        private static int _globalGeneration;

        private readonly Func<SectionHealth> _compute;
        private readonly double _seconds;
        private readonly Func<double> _clock;
        private SectionHealth _cachedHealth;
        private bool _hasCachedHealth;
        private double _computedAtSeconds;
        private int _computedGeneration;

        public LiveOpsHealthThrottle(Func<SectionHealth> compute, double seconds = DefaultSeconds)
            : this(compute, seconds, ReadEditorClock)
        {
        }

        /// <param name="clock">Giây tăng dần; test tiêm đồng hồ tay để kiểm cửa sổ thời gian mà không phải chờ thật.</param>
        internal LiveOpsHealthThrottle(Func<SectionHealth> compute, double seconds, Func<double> clock)
        {
            _compute = compute ?? throw new ArgumentNullException(nameof(compute));
            if (seconds < 0 || double.IsNaN(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
            _seconds = seconds;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Số lần đã gọi hàm tính thật — test đọc để chứng minh throttle giữ kết quả.</summary>
        internal int ComputeCount { get; private set; }

        /// <summary>
        /// Kết quả còn trong cửa sổ và cùng thế hệ thì trả bản đã giữ; không thì tính lại. Hàm tính ném → cảnh báo Console và
        /// NotMeasured có lý do, giữ hết cửa sổ: màn ném liên tục không được spam Console mỗi giây, và dấu rỗng có lý do
        /// không bao giờ bị hiểu là "ổn".
        /// </summary>
        public SectionHealth Get()
        {
            double now = _clock();
            if (_hasCachedHealth && _computedGeneration == _globalGeneration && now - _computedAtSeconds < _seconds)
            {
                return _cachedHealth;
            }

            ComputeCount++;
            try
            {
                _cachedHealth = _compute();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellHealthThrewLogFormat, exception.GetType().Name,
                    exception.Message, exception.StackTrace));
                _cachedHealth = SectionHealth.NotMeasured(LiveOpsHubStrings.ShellHealthThrewReason);
            }
            _hasCachedHealth = true;
            _computedAtSeconds = now;
            _computedGeneration = _globalGeneration;
            return _cachedHealth;
        }

        public void Invalidate()
        {
            _hasCachedHealth = false;
        }

        /// <summary>Tăng thế hệ toàn cục — mọi throttle đang sống tính lại ở lần <see cref="Get"/> kế.</summary>
        public static void InvalidateAll()
        {
            unchecked
            {
                _globalGeneration++;
            }
        }

        private static double ReadEditorClock()
        {
            return EditorApplication.timeSinceStartup;
        }
    }
}
