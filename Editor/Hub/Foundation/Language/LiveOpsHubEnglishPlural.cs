using System.Globalization;
using System.Text;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Luật số ít/số nhiều của bản TIẾNG ANH (Q-W5-2, user chốt 17/9/2026) — MỘT luật chung cho cả catalog, không vá lẻ
    /// từng câu.
    /// <para>
    /// Cách đánh dấu: danh từ đếm được viết ngay trong câu tiếng Anh thành nhóm <c>{i|số ít|số nhiều}</c>, trong đó
    /// <c>i</c> là chỉ số của ĐỐI SỐ ĐẾM đứng trước nó —
    /// <c>"Readable: {0} {0|recurring rule|recurring rules} · {1} {1|event|events}"</c>. Vì sao đánh dấu theo TỪNG danh từ
    /// chứ không làm "khuôn số ít riêng cho cả câu": catalog có những câu mang hai ba số đếm độc lập
    /// (<c>ShellStatusCheckedFormat</c>, <c>ValidationSummaryRightFormat</c>, <c>PasteImportOutcomeHeadlineFormat</c>,
    /// <c>ExportGateMismatchReportCountsFormat</c>) — một khuôn cho cả câu phải đẻ 4–8 biến thể mỗi câu.
    /// </para>
    /// <para>
    /// <see cref="Resolve"/> chạy TRƯỚC <c>string.Format</c>: nó chọn vế rồi trả về một khuôn <c>string.Format</c> hợp lệ.
    /// Vế số ít chỉ dùng khi đối số <c>i</c> là SỐ NGUYÊN bằng 1; mọi giá trị khác (gồm 0, số thực, chuỗi) lấy vế số nhiều —
    /// tiếng Anh đọc "0 events", không phải "0 event".
    /// </para>
    /// <para>
    /// Câu TIẾNG VIỆT không bao giờ mang dấu (tiếng Việt không chia số) — <c>EnglishPluralTests</c> gác điều đó.
    /// </para>
    /// </summary>
    internal static class LiveOpsHubEnglishPlural
    {
        private const char GroupOpen = '{';
        private const char GroupClose = '}';
        private const char GroupSeparator = '|';

        /// <summary>Câu có ít nhất một nhóm <c>{i|…|…}</c> hay không — test gác và bảng đối chiếu bản dịch đọc qua đây.</summary>
        internal static bool ContainsMark(string text)
        {
            return IndexOfMark(text, 0) >= 0;
        }

        /// <summary>
        /// Bỏ dấu, luôn giữ vế SỐ NHIỀU. Đây là lưới an toàn của <c>LiveOpsHubStringTable</c>: chỗ nào còn đọc chữ bằng
        /// <c>LiveOpsHubStringCatalog.Text</c> rồi tự <c>string.Format</c> thì nhận đúng câu như trước khi có luật này,
        /// chứ không nhận một khuôn hỏng làm <c>string.Format</c> ném.
        /// </summary>
        internal static string StripToPlural(string text)
        {
            return Rewrite(text, null, true);
        }

        /// <summary>
        /// Chọn vế theo đối số rồi trả khuôn <c>string.Format</c> hợp lệ. <paramref name="arguments"/> phải là ĐÚNG mảng
        /// sắp truyền cho <c>string.Format</c> — chỉ số trong dấu là chỉ số của mảng đó.
        /// </summary>
        internal static string Resolve(string text, object[] arguments)
        {
            return Rewrite(text, arguments, false);
        }

        private static string Rewrite(string text, object[] arguments, bool alwaysPlural)
        {
            if (string.IsNullOrEmpty(text)) return text;
            int markStart = IndexOfMark(text, 0);
            if (markStart < 0) return text;

            StringBuilder builder = new StringBuilder(text.Length);
            int copiedUpTo = 0;
            while (markStart >= 0)
            {
                int markEnd = text.IndexOf(GroupClose, markStart);
                if (markEnd < 0) break;
                builder.Append(text, copiedUpTo, markStart - copiedUpTo);
                builder.Append(ChosenSide(text.Substring(markStart + 1, markEnd - markStart - 1), arguments, alwaysPlural));
                copiedUpTo = markEnd + 1;
                markStart = IndexOfMark(text, copiedUpTo);
            }
            builder.Append(text, copiedUpTo, text.Length - copiedUpTo);
            return builder.ToString();
        }

        /// <summary>
        /// Một vế của nhóm đã bỏ ngoặc: <c>"0|event|events"</c>. Dấu hỏng (thiếu vế, thừa vế, chỉ số ngoài mảng) đọc thành
        /// vế CUỐI: câu vẫn ra chữ đọc được, còn cái sai thì test dấu bắt — không để một dấu gõ nhầm làm cửa sổ ném.
        /// </summary>
        private static string ChosenSide(string body, object[] arguments, bool alwaysPlural)
        {
            string[] parts = body.Split(GroupSeparator);
            if (parts.Length < 3) return parts[parts.Length - 1];
            string singular = parts[1];
            string plural = parts[parts.Length - 1];
            if (alwaysPlural || arguments == null) return plural;
            int index;
            if (!TryParseIndex(parts[0], out index) || index < 0 || index >= arguments.Length) return plural;
            return IsExactlyOne(arguments[index]) ? singular : plural;
        }

        /// <summary>Chỉ số của nhóm — chỉ nhận chữ số ASCII, không nhận dấu và khoảng trắng (khuôn phải đọc được bằng mắt).</summary>
        private static bool TryParseIndex(string text, out int index)
        {
            index = 0;
            if (text.Length == 0) return false;
            for (int position = 0; position < text.Length; position++)
            {
                char character = text[position];
                if (character < '0' || character > '9') return false;
                index = (index * 10) + (character - '0');
            }
            return true;
        }

        /// <summary>
        /// Đối số có phải SỐ NGUYÊN bằng 1. Số thực 1.0 cố ý KHÔNG tính: "1.0 hours" trong tiếng Anh vẫn đọc số nhiều, và
        /// một câu đo được (byte, giờ) không được đổi cách đọc chỉ vì người gọi đổi kiểu số.
        /// <para>
        /// Chuỗi cũng được nhận và ĐỌC THÀNH SỐ vì rất nhiều chỗ trong hub đưa số đếm đã định dạng sẵn vào câu
        /// (<c>LiveOpsHubFormat.Integer</c>). Chỉ n = 1 mới đổi được cách đọc, và <c>Integer(1)</c> ra đúng <c>"1"</c> nên
        /// đường chuỗi này luôn đọc được đúng ca duy nhất cần nó. Số lớn hơn thì hub in dấu phân nhóm bằng dấu CHẤM
        /// ("1.000") — <c>InvariantCulture</c> ngăn hàng nghìn bằng dấu PHẨY nên chuỗi ấy không parse được và rơi về vế số
        /// nhiều; đó CHÍNH LÀ vế đúng, không phải một ca hỏng cần vá.
        /// </para>
        /// </summary>
        private static bool IsExactlyOne(object argument)
        {
            if (argument is string text)
            {
                long parsed;
                return long.TryParse(text, NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out parsed) && parsed == 1L;
            }
            if (argument is int intValue) return intValue == 1;
            if (argument is long longValue) return longValue == 1L;
            if (argument is short shortValue) return shortValue == 1;
            if (argument is byte byteValue) return byteValue == 1;
            if (argument is uint unsignedIntValue) return unsignedIntValue == 1u;
            if (argument is ulong unsignedLongValue) return unsignedLongValue == 1ul;
            if (argument is ushort unsignedShortValue) return unsignedShortValue == 1;
            if (argument is sbyte signedByteValue) return signedByteValue == 1;
            return false;
        }

        /// <summary>Vị trí nhóm kế tiếp: một <c>{</c>, vài chữ số, rồi <c>|</c> — khuôn <c>{0}</c> bình thường không khớp.</summary>
        private static int IndexOfMark(string text, int startIndex)
        {
            if (string.IsNullOrEmpty(text)) return -1;
            for (int position = startIndex; position < text.Length; position++)
            {
                if (text[position] != GroupOpen) continue;
                int scan = position + 1;
                while (scan < text.Length && text[scan] >= '0' && text[scan] <= '9') scan++;
                if (scan > position + 1 && scan < text.Length && text[scan] == GroupSeparator) return position;
            }
            return -1;
        }
    }
}
