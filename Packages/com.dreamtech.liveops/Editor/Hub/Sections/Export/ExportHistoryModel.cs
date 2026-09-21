using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Một hàng của card "Các lần đã đăng" [SD2 §3.9] — bốn cột + hai cờ quyết định viền trái và menu chuột phải.</summary>
    internal sealed class ExportHistoryRow
    {
        internal ExportHistoryRow(PublishedCalendarStamp stamp, string timeText, string publisherText, string noteText, string shaText,
            bool isActiveBaseline, bool canRemoveStamp, bool isVerifiedAgainstRemote, string verifiedText)
        {
            Stamp = stamp;
            TimeText = timeText ?? string.Empty;
            PublisherText = publisherText ?? string.Empty;
            NoteText = noteText ?? string.Empty;
            ShaText = shaText ?? string.Empty;
            IsActiveBaseline = isActiveBaseline;
            CanRemoveStamp = canRemoveStamp;
            IsVerifiedAgainstRemote = isVerifiedAgainstRemote;
            VerifiedText = verifiedText ?? string.Empty;
        }

        public PublishedCalendarStamp Stamp { get; }
        public string TimeText { get; }
        public string PublisherText { get; }
        public string NoteText { get; }
        public string ShaText { get; }

        /// <summary>Viền trái 3px: "đang là bản so", KHÔNG phải dòng đang chọn [SD2 §3.9].</summary>
        public bool IsActiveBaseline { get; }

        /// <summary>Chỉ lần MỚI NHẤT gỡ được dấu: gỡ dấu giữa danh sách sẽ làm lịch sử nói dối thứ tự đăng.</summary>
        public bool CanRemoveStamp { get; }

        /// <summary>
        /// JSON đang chạy đã dán và khớp ĐÚNG dấu này [SD2 §3.9]: hàng có chấm Ok + "· đã đối chiếu 09:10". Khác
        /// <see cref="IsActiveBaseline"/> — "đang là bản so" là lựa chọn của người dùng, "đã đối chiếu" là bằng chứng.
        /// </summary>
        public bool IsVerifiedAgainstRemote { get; }

        /// <summary>"· đã đối chiếu 09:10"; "" khi chưa đối chiếu.</summary>
        public string VerifiedText { get; }
    }

    /// <summary>
    /// Danh sách các lần đã đăng, mới nhất trước. Thuần: nhận tài liệu + dấu đang là bản so + bộ định dạng, trả hàng đã có
    /// sẵn chữ. Thứ tự nghịch với <c>PublishedStamps</c> (asset lưu theo thứ tự ghi) vì người dùng tìm lần đăng gần nhất.
    /// </summary>
    internal sealed class ExportHistoryModel
    {
        private ExportHistoryModel(IReadOnlyList<ExportHistoryRow> rows, string emptyText)
        {
            Rows = rows ?? Array.Empty<ExportHistoryRow>();
            EmptyText = emptyText ?? string.Empty;
        }

        public IReadOnlyList<ExportHistoryRow> Rows { get; }

        /// <summary>"Chưa có dấu đã đăng nào"; "" khi có hàng.</summary>
        public string EmptyText { get; }

        public bool IsEmpty => Rows.Count == 0;

        /// <summary>Dựng danh sách khi chưa có bản remote nào để đối chiếu.</summary>
        public static ExportHistoryModel Build(LiveEventCalendarDocument document, PublishedCalendarStamp activeStamp, LiveOpsHubFormat format)
        {
            return Build(document, activeStamp, null, format);
        }

        /// <param name="remote">
        /// Bản JSON đang chạy đã dán; dấu nào khớp ĐÚNG bản đó thì hàng mang chấm Ok + "· đã đối chiếu &lt;giờ&gt;"
        /// [SD2 §3.9]. null = chưa dán bản nào.
        /// </param>
        public static ExportHistoryModel Build(LiveEventCalendarDocument document, PublishedCalendarStamp activeStamp,
            LiveOpsHubRemoteSnapshot remote, LiveOpsHubFormat format)
        {
            if (format == null) throw new ArgumentNullException(nameof(format), LiveOpsHubStrings.ExportGateErrorFormatMissing);
            DateTime? verifiedUtc = remote == null ? null : remote.VerifiedUtc;

            List<ExportHistoryRow> rows = new List<ExportHistoryRow>();
            if (document != null)
            {
                IReadOnlyList<PublishedCalendarStamp> stamps = document.PublishedStamps;
                PublishedCalendarStamp latest = document.LatestStamp;
                for (int index = stamps.Count - 1; index >= 0; index--)
                {
                    PublishedCalendarStamp stamp = stamps[index];
                    if (stamp == null) continue;
                    DateTime publishedUtc;
                    // Giờ dấu không đọc được (file sửa tay) vẫn phải hiện: in nguyên văn chuỗi trong asset thay vì bỏ hàng đi.
                    string timeText = stamp.TryGetPublishedUtc(out publishedUtc) ? format.ShortDateTime(publishedUtc) : stamp.PublishedUtcText;
                    string noteText = stamp.Note.Length > 0 ? stamp.Note : LiveOpsHubStrings.ExportHistoryNoNote;
                    bool verified = remote != null && verifiedUtc.HasValue && remote.MatchesStampExactly(stamp);
                    string verifiedText = verified
                        ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportGateRemoteVerifiedSuffixFormat,
                            ClockText(verifiedUtc.Value))
                        : string.Empty;
                    rows.Add(new ExportHistoryRow(stamp, timeText, stamp.Publisher, noteText, stamp.ShortSha,
                        ReferenceEquals(stamp, activeStamp), ReferenceEquals(stamp, latest), verified, verifiedText));
                }
            }

            return new ExportHistoryModel(rows, rows.Count == 0 ? LiveOpsHubStrings.ExportHistoryEmpty : string.Empty);
        }

        /// <summary>"09:10" giờ UTC — cùng cách in mà cổng xuất dùng cho câu "· đã đối chiếu {0}".</summary>
        private static string ClockText(DateTime utc)
        {
            return utc.ToString(ClockPattern, CultureInfo.InvariantCulture);
        }

        private const string ClockPattern = "HH:mm";
    }
}
