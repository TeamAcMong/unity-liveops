using System;

namespace DreamTech.LiveOps
{
    /// <summary>Giai đoạn của một đợt event so với một thời điểm. <see cref="None"/> chỉ dùng khi không có đợt nào.</summary>
    public enum LiveEventPhase
    {
        None = 0,
        Upcoming = 1,
        Active = 2,
        Ended = 3,
    }

    /// <summary>
    /// Một đợt event: [StartUtc, EndUtc). Bất biến.
    ///
    /// <para>Id là duy nhất cho MỖI ĐỢT, không phải cho loại event: tiến độ, quà và kết quả của người chơi đều khoá theo id này,
    /// nên hai đợt không bao giờ dùng chung dữ liệu. <see cref="ConfigKey"/> là khoá để game tự tra cấu hình riêng của đợt
    /// (vd key remote config); package không đọc nó.</para>
    /// </summary>
    public sealed class LiveEventInstance : IEquatable<LiveEventInstance>
    {
        /// <summary>Ký tự dùng làm dấu ngăn trong grant id — không được xuất hiện trong id đợt hay khoá nhận quà.</summary>
        public const char ReservedSeparator = '#';

        public LiveEventInstance(string eventId, string eventType, DateTime startUtc, DateTime endUtc, string configKey = null)
        {
            ValidateIdentifier(eventId, nameof(eventId), "Event id");
            ValidateIdentifier(eventType, nameof(eventType), "Loại event");
            if (endUtc <= startUtc) throw new ArgumentException("Đợt event phải kết thúc sau khi bắt đầu: " + eventId, nameof(endUtc));

            EventId = eventId;
            EventType = eventType;
            StartUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
            EndUtc = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc);
            ConfigKey = configKey ?? string.Empty;
        }

        public string EventId { get; }
        public string EventType { get; }
        public DateTime StartUtc { get; }
        public DateTime EndUtc { get; }
        public string ConfigKey { get; }
        public TimeSpan Duration => EndUtc - StartUtc;

        public LiveEventPhase PhaseAt(DateTime nowUtc)
        {
            if (nowUtc < StartUtc) return LiveEventPhase.Upcoming;
            return nowUtc < EndUtc ? LiveEventPhase.Active : LiveEventPhase.Ended;
        }

        public TimeSpan TimeUntilStart(DateTime nowUtc)
        {
            TimeSpan left = StartUtc - nowUtc;
            return left < TimeSpan.Zero ? TimeSpan.Zero : left;
        }

        public TimeSpan TimeLeft(DateTime nowUtc)
        {
            TimeSpan left = EndUtc - nowUtc;
            return left < TimeSpan.Zero ? TimeSpan.Zero : left;
        }

        /// <summary>0 lúc bắt đầu, 1 lúc kết thúc, kẹp trong [0, 1].</summary>
        public double Progress(DateTime nowUtc)
        {
            double progress = (nowUtc - StartUtc).TotalSeconds / Duration.TotalSeconds;
            if (progress < 0) return 0;
            return progress > 1 ? 1 : progress;
        }

        public bool Overlaps(LiveEventInstance other)
        {
            return other != null && StartUtc < other.EndUtc && other.StartUtc < EndUtc;
        }

        public bool Equals(LiveEventInstance other)
        {
            if (ReferenceEquals(other, null)) return false;
            return string.Equals(EventId, other.EventId, StringComparison.Ordinal) &&
                   string.Equals(EventType, other.EventType, StringComparison.Ordinal) &&
                   StartUtc == other.StartUtc && EndUtc == other.EndUtc &&
                   string.Equals(ConfigKey, other.ConfigKey, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LiveEventInstance);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EventId.GetHashCode() * 397) ^ StartUtc.GetHashCode();
            }
        }

        public override string ToString()
        {
            return EventId + " (" + EventType + ") [" + StartUtc.ToString("u") + " → " + EndUtc.ToString("u") + ")";
        }

        internal static void ValidateIdentifier(string value, string parameterName, string label)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException(label + " không được rỗng.", parameterName);
            if (value.IndexOf(ReservedSeparator) >= 0 || value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0)
            {
                throw new ArgumentException(label + " không được chứa '" + ReservedSeparator + "' hay xuống dòng: " + value, parameterName);
            }
        }
    }
}
