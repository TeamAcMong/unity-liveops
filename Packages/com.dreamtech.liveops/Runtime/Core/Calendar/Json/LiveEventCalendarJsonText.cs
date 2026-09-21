using System;
using System.Collections.Generic;
using System.Text;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Kết quả của <see cref="LiveEventCalendarJsonWriter.Write"/>: chuỗi đã định dạng + byte UTF-8 + sha + bản đồ dòng.
    /// Bất biến và chỉ bộ ghi dựng được — vì sao: sha, số byte và bản đồ dòng phải luôn tính trên đúng chuỗi này; nếu
    /// ai cũng dựng được thì Copy, Lưu file và dấu đã đăng có thể mang ba giá trị không khớp nhau.
    /// </summary>
    public sealed class LiveEventCalendarJsonText
    {
        private const int ShortShaLength = 6;

        // Không BOM (mục 5.2 luật 1): BOM đổi byte → đổi sha của cùng một lịch.
        private static readonly UTF8Encoding Utf8WithoutByteOrderMark = new UTF8Encoding(false, false);

        private readonly byte[] _utf8Bytes;
        private readonly List<LiveEventCalendarJsonLine> _lines;
        private readonly Dictionary<(LiveEventCalendarItemKind, string), (int FirstLine, int LastLine)> _itemLineRanges;
        private readonly Dictionary<(LiveEventCalendarItemKind, string, string), int> _fieldLines;

        internal LiveEventCalendarJsonText(LiveEventCalendarJsonFormat format, string text, List<LiveEventCalendarJsonLine> lines)
        {
            Format = format;
            Text = text ?? string.Empty;
            _lines = lines ?? throw new ArgumentNullException(nameof(lines));

            _utf8Bytes = Utf8WithoutByteOrderMark.GetBytes(Text);
            Sha256Hex = LiveEventCalendarSha256.ComputeHex(_utf8Bytes);

            _itemLineRanges = new Dictionary<(LiveEventCalendarItemKind, string), (int FirstLine, int LastLine)>();
            _fieldLines = new Dictionary<(LiveEventCalendarItemKind, string, string), int>();
            IndexLines();
        }

        public LiveEventCalendarJsonFormat Format { get; }

        public string Text { get; }

        /// <summary>Bản sao mỗi lần gọi — Lưu file ghi đúng mảng này; bên gọi sửa mảng cũng không làm lệch sha đã tính.</summary>
        public byte[] GetUtf8Bytes()
        {
            var copy = new byte[_utf8Bytes.Length];
            Buffer.BlockCopy(_utf8Bytes, 0, copy, 0, _utf8Bytes.Length);
            return copy;
        }

        public int ByteCount => _utf8Bytes.Length;

        /// <summary>Số LF + 1 (không có LF cuối file nên dòng cuối là <c>}</c>).</summary>
        public int LineCount => _lines.Count;

        /// <summary>Hex thường 64 ký tự trên đúng <see cref="GetUtf8Bytes"/>.</summary>
        public string Sha256Hex { get; }

        /// <summary>6 ký tự đầu — cái hiện trên chip, tên file và câu "sha 5b0d93".</summary>
        public string ShortSha => Sha256Hex.Substring(0, ShortShaLength);

        public IReadOnlyList<LiveEventCalendarJsonLine> Lines => _lines;

        /// <summary>Dòng <c>{</c> tới dòng <c>}</c> (gồm dấu phẩy) của mục. Hai luật trùng loại: trả luật đứng trước.</summary>
        public bool TryGetItemLineRange(LiveEventCalendarItemKind kind, string itemKey, out int firstLine, out int lastLine)
        {
            firstLine = 0;
            lastLine = 0;
            if (itemKey == null || !_itemLineRanges.TryGetValue((kind, itemKey), out (int FirstLine, int LastLine) range)) return false;
            firstLine = range.FirstLine;
            lastLine = range.LastLine;
            return true;
        }

        /// <summary>Dòng mang key <paramref name="fieldName"/> của mục. Hai luật trùng loại: trả dòng của luật đứng trước.</summary>
        public bool TryGetFieldLine(LiveEventCalendarItemKind kind, string itemKey, string fieldName, out int lineNumber)
        {
            lineNumber = 0;
            if (itemKey == null || string.IsNullOrEmpty(fieldName)) return false;
            return _fieldLines.TryGetValue((kind, itemKey, fieldName), out lineNumber);
        }

        private void IndexLines()
        {
            // Mục trùng khoá (hai luật cùng loại trong tài liệu dán vào) nằm liền nhau với cùng (kind, key), nên không thể
            // tách lần xuất hiện bằng khoá: dòng "{" của mục mới là mốc bắt đầu một lần xuất hiện. Lần đầu thắng — cùng luật
            // "giữ luật đứng trước" của tài liệu và bộ biên dịch, để lề viewer chỉ đúng luật game chạy.
            bool currentOccurrenceIsIndexed = false;
            LiveEventCalendarItemKind currentKind = default;
            string currentItemKey = null;

            for (int index = 0; index < _lines.Count; index++)
            {
                LiveEventCalendarJsonLine line = _lines[index];
                if (!line.ItemKind.HasValue) continue;

                (LiveEventCalendarItemKind, string) key = (line.ItemKind.Value, line.ItemKey);
                bool opensItem = line.FieldName.Length == 0 && line.Text.TrimStart().StartsWith("{", StringComparison.Ordinal);
                if (opensItem)
                {
                    currentKind = line.ItemKind.Value;
                    currentItemKey = line.ItemKey;
                    currentOccurrenceIsIndexed = !_itemLineRanges.ContainsKey(key);
                    if (currentOccurrenceIsIndexed) _itemLineRanges.Add(key, (line.Number, line.Number));
                    continue;
                }

                if (!currentOccurrenceIsIndexed || currentKind != line.ItemKind.Value ||
                    !string.Equals(currentItemKey, line.ItemKey, StringComparison.Ordinal))
                {
                    continue;
                }

                (int FirstLine, int LastLine) range = _itemLineRanges[key];
                _itemLineRanges[key] = (range.FirstLine, line.Number);

                if (line.FieldName.Length > 0)
                {
                    (LiveEventCalendarItemKind, string, string) fieldKey = (line.ItemKind.Value, line.ItemKey, line.FieldName);
                    if (!_fieldLines.ContainsKey(fieldKey)) _fieldLines.Add(fieldKey, line.Number);
                }
            }
        }
    }
}
