using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Unity
{
    /// <summary>
    /// Kết quả đọc JSON lịch thành TÀI LIỆU (chưa biên dịch) — dùng khi cần thấy đúng cái JSON viết, kể cả mục hỏng: hub
    /// đọc bản đang chạy đã dán, so khứ hồi, diff với bản đã đăng. Game dùng <see cref="JsonLiveEventCalendarParser.Parse"/>.
    /// </summary>
    public sealed class LiveEventCalendarDocumentParseResult
    {
        internal LiveEventCalendarDocumentParseResult(LiveEventCalendarDocument document, int formatVersion, bool isReadable,
            bool isBlank, string readErrorText, IReadOnlyList<string> problems)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            FormatVersion = formatVersion;
            IsReadable = isReadable;
            IsBlank = isBlank;
            ReadErrorText = readErrorText ?? string.Empty;
            Problems = problems ?? throw new ArgumentNullException(nameof(problems));
        }

        /// <summary>
        /// <c>EventTypes</c> rỗng (JSON không mang định nghĩa loại); <c>EntryKey</c> của đợt = <c>"json-&lt;chỉ số&gt;"</c> theo
        /// thứ tự trong JSON; chuỗi giờ và id giữ nguyên văn. JSON hỏng hoặc trống → <see cref="LiveEventCalendarDocument.Empty"/>.
        /// </summary>
        public LiveEventCalendarDocument Document { get; }

        /// <summary>
        /// Số <c>version</c> trong JSON khi &gt; 0; thiếu thì 2 nếu có mảng <c>recurring</c>, không thì 1. 0 khi JSON trống
        /// hoặc không đọc được (không có gì để nói định dạng).
        /// </summary>
        public int FormatVersion { get; }

        /// <summary><c>false</c> khi <c>JsonUtility</c> ném (sai cú pháp, BOM, gốc không phải object). JSON trống vẫn là đọc được.</summary>
        public bool IsReadable { get; }

        /// <summary>Message gốc của <c>JsonUtility</c> khi không đọc được; rỗng khi đọc được.</summary>
        public string ReadErrorText { get; }

        /// <summary>
        /// Chỉ vấn đề cấp đọc (JSON hỏng, thiếu mảng, định dạng mới hơn) — lý do bỏ từng mục do bộ biên dịch báo, nằm ở
        /// <see cref="LiveEventCalendarParseResult.Problems"/> sau khi biên dịch.
        /// </summary>
        public IReadOnlyList<string> Problems { get; }

        /// <summary>JSON null/chỉ khoảng trắng — remote config chưa có key, không phải lỗi (hành vi 0.1.0).</summary>
        internal bool IsBlank { get; }
    }
}
