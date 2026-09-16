using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Một hàng của card "Các lần đã đăng" [SD2 §3.9] — bốn cột + hai cờ quyết định viền trái và menu chuột phải.</summary>
    internal sealed class ExportHistoryRow
    {
        internal ExportHistoryRow(PublishedCalendarStamp stamp, string timeText, string publisherText, string noteText, string shaText,
            bool isActiveBaseline, bool canRemoveStamp)
        {
            Stamp = stamp;
            TimeText = timeText ?? string.Empty;
            PublisherText = publisherText ?? string.Empty;
            NoteText = noteText ?? string.Empty;
            ShaText = shaText ?? string.Empty;
            IsActiveBaseline = isActiveBaseline;
            CanRemoveStamp = canRemoveStamp;
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

        public static ExportHistoryModel Build(LiveEventCalendarDocument document, PublishedCalendarStamp activeStamp, LiveOpsHubFormat format)
        {
            if (format == null) throw new ArgumentNullException(nameof(format), LiveOpsHubStrings.ExportGateErrorFormatMissing);

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
                    rows.Add(new ExportHistoryRow(stamp, timeText, stamp.Publisher, noteText, stamp.ShortSha,
                        ReferenceEquals(stamp, activeStamp), ReferenceEquals(stamp, latest)));
                }
            }

            return new ExportHistoryModel(rows, rows.Count == 0 ? LiveOpsHubStrings.ExportHistoryEmpty : string.Empty);
        }
    }
}
