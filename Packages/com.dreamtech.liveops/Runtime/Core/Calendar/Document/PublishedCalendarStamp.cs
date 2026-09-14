using System;

namespace DreamTech.LiveOps
{
    /// <summary>Dấu "đã đăng" — chụp lại nội dung + sha của lần Xuất JSON gần nhất, dùng làm bản so cho diff và kiểm lịch.</summary>
    public sealed class PublishedCalendarStamp
    {
        public PublishedCalendarStamp(string publishedUtcText, string publisher, string sha256Hex, int byteCount,
            int formatVersion, string note, string snapshotJson)
        {
            PublishedUtcText = publishedUtcText ?? string.Empty;
            Publisher = publisher ?? string.Empty;
            Sha256Hex = sha256Hex ?? string.Empty;
            ByteCount = byteCount;
            FormatVersion = formatVersion;
            Note = note ?? string.Empty;
            SnapshotJson = snapshotJson ?? string.Empty;
        }

        public string PublishedUtcText { get; }
        public string Publisher { get; }

        /// <summary>64 ký tự hex thường.</summary>
        public string Sha256Hex { get; }

        public string ShortSha => Sha256Hex.Length >= 6 ? Sha256Hex.Substring(0, 6) : Sha256Hex;

        public int ByteCount { get; }
        public int FormatVersion { get; }
        public string Note { get; }

        /// <summary>Đúng chuỗi JSON đã copy/lưu lúc đăng — bản so cho lần kiểm remote-snapshot-drift sau.</summary>
        public string SnapshotJson { get; }

        public bool TryGetPublishedUtc(out DateTime publishedUtc) => LiveEventUtcText.TryParse(PublishedUtcText, out publishedUtc);
    }
}
