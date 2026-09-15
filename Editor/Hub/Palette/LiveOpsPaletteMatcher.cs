using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Khớp của palette ⌘K ([FD §3.8], 8.7) — thuần, test không cần panel. Luật:
    /// <list type="bullet">
    /// <item>Bỏ dấu cả hai phía: FormD, bỏ dấu kết hợp, đ → d, chữ thường — người gõ "kiem" không bật bộ gõ vẫn tới Kiểm lịch.</item>
    /// <item>Khớp subsequence (các ký tự của câu gõ xuất hiện theo thứ tự, không cần liền nhau); khoảng trắng trong câu gõ bỏ qua.</item>
    /// <item>Xếp hạng theo trường khớp: tiêu đề &gt; tầng &gt; subtitle &gt; id luật; cùng trường thì khớp liền/đầu từ đứng trước; bằng
    /// điểm giữ thứ tự rail. Câu gõ rỗng → mọi mục theo thứ tự rail.</item>
    /// <item>Phần khớp in đậm bằng <c>&lt;b&gt;</c>; mọi đoạn chữ thô bọc <c>&lt;noparse&gt;</c> trước khi ghép thẻ ([FD §2.14] chỗ 7) —
    /// tiêu đề chứa "&lt;color&gt;" không được đổi màu cả hàng.</item>
    /// </list>
    /// Khớp theo từng ký tự gốc (mỗi ký tự gốc bỏ dấu riêng) để vị trí đậm rơi đúng chữ có dấu, không lệch khi FormD tách một chữ thành nhiều mã.
    /// </summary>
    internal static class LiveOpsPaletteMatcher
    {
        /// <summary>Điểm nền theo trường khớp — cách nhau đủ xa để thưởng khớp liền không đảo thứ tự trường.</summary>
        internal const int TitleFieldScore = 4000;
        internal const int StageFieldScore = 3000;
        internal const int SubtitleFieldScore = 2000;
        internal const int RuleIdFieldScore = 1000;

        /// <summary>Thưởng khi câu gõ khớp liền một khối và khi khớp từ đầu một từ — "kiem" lên trước "k…i…e…m" rải rác.</summary>
        internal const int ContiguousBonus = 300;
        internal const int WordStartBonus = 200;
        internal const int MaximumGapPenalty = 299;

        // đ (U+0111) và Đ (U+0110) viết bằng mã: chữ tiếng Việt chỉ nằm ở file chuỗi.
        private const char LowercaseD = '\u0111';
        private const char UppercaseD = '\u0110';

        private const string NoParseOpen = "<noparse>";
        private const string NoParseClose = "</noparse>";
        private const string BoldOpen = "<b>";
        private const string BoldClose = "</b>";

        /// <summary>Một mục palette: một section, hoặc một id luật dẫn tới Kiểm lịch đã lọc (không bao giờ là lệnh).</summary>
        internal sealed class Entry
        {
            public Entry(string title, string stageCaption, string subtitle, string ruleId, string sectionId, SectionHealth health, string reasonText)
            {
                Title = title ?? string.Empty;
                StageCaption = stageCaption ?? string.Empty;
                Subtitle = subtitle ?? string.Empty;
                RuleId = ruleId ?? string.Empty;
                SectionId = sectionId ?? string.Empty;
                Health = health;
                ReasonText = reasonText ?? string.Empty;
            }

            public string Title { get; }

            /// <summary>"KIỂM" (viết hoa sẵn — USS không có text-transform).</summary>
            public string StageCaption { get; }

            public string Subtitle { get; }

            /// <summary>Id luật (vd "overlap-same-type") — "" với mục section.</summary>
            public string RuleId { get; }

            public string SectionId { get; }
            public SectionHealth Health { get; }

            /// <summary>Lý do in sau tên với hàng không Ok (palette không có tooltip): "Không ở Play Mode".</summary>
            public string ReasonText { get; }

            public bool IsRule => RuleId.Length > 0;
        }

        internal sealed class Match
        {
            public Match(Entry entry, string richTitle, int score)
            {
                Entry = entry;
                RichTitle = richTitle;
                Score = score;
            }

            public Entry Entry { get; }

            /// <summary>Tiêu đề rich text: phần khớp trong <c>&lt;b&gt;</c>, chữ thô trong <c>&lt;noparse&gt;</c>.</summary>
            public string RichTitle { get; }

            public int Score { get; }
        }

        /// <summary>Bỏ dấu để so: FormD, bỏ dấu kết hợp, đ/Đ → d, chữ thường bất biến văn hoá.</summary>
        public static string Fold(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            StringBuilder folded = new StringBuilder(text.Length);
            foreach (char character in text) AppendFolded(folded, character);
            return folded.ToString();
        }

        /// <summary>Mục khớp, điểm cao trước; câu gõ rỗng (hoặc chỉ khoảng trắng) → mọi mục theo thứ tự đưa vào (= thứ tự rail).</summary>
        public static IReadOnlyList<Match> Find(IReadOnlyList<Entry> entries, string query)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            string foldedQuery = FoldQuery(query);
            List<KeyValuePair<int, Match>> ranked = new List<KeyValuePair<int, Match>>();
            for (int index = 0; index < entries.Count; index++)
            {
                Entry entry = entries[index];
                if (entry == null) continue;
                if (foldedQuery.Length == 0)
                {
                    ranked.Add(new KeyValuePair<int, Match>(index, new Match(entry, Escape(entry.Title), 0)));
                    continue;
                }
                Match match = MatchEntry(entry, foldedQuery);
                if (match != null) ranked.Add(new KeyValuePair<int, Match>(index, match));
            }
            // List.Sort không ổn định: so thêm vị trí gốc để mục bằng điểm giữ thứ tự rail.
            ranked.Sort((left, right) =>
            {
                int byScore = right.Value.Score.CompareTo(left.Value.Score);
                return byScore != 0 ? byScore : left.Key.CompareTo(right.Key);
            });
            List<Match> result = new List<Match>(ranked.Count);
            foreach (KeyValuePair<int, Match> item in ranked) result.Add(item.Value);
            return result;
        }

        /// <summary>Bọc chữ thô trong <c>&lt;noparse&gt;</c>; chuỗi "&lt;/noparse&gt;" trong chữ thô được tách để không đóng khối sớm.</summary>
        internal static string Escape(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            // "</noparse>" nằm trong chữ thô sẽ kết thúc khối noparse giữa chừng: tách thành "</" + khối mới + "noparse>".
            string safe = raw.Replace(NoParseClose, "</" + NoParseClose + NoParseOpen + "noparse>");
            return NoParseOpen + safe + NoParseClose;
        }

        private static Match MatchEntry(Entry entry, string foldedQuery)
        {
            int[] positions;
            int quality;
            if (TryMatchField(entry.Title, foldedQuery, out positions, out quality))
            {
                return new Match(entry, Highlight(entry.Title, positions), TitleFieldScore + quality);
            }
            string plainTitle = Escape(entry.Title);
            if (TryMatchField(entry.StageCaption, foldedQuery, out positions, out quality)) return new Match(entry, plainTitle, StageFieldScore + quality);
            if (TryMatchField(entry.Subtitle, foldedQuery, out positions, out quality)) return new Match(entry, plainTitle, SubtitleFieldScore + quality);
            if (TryMatchField(entry.RuleId, foldedQuery, out positions, out quality)) return new Match(entry, plainTitle, RuleIdFieldScore + quality);
            return null;
        }

        /// <summary>
        /// Subsequence tham lam trên chữ đã bỏ dấu của từng ký tự gốc. <paramref name="positions"/> = chỉ số ký tự GỐC khớp (để in đậm
        /// đúng chữ có dấu); <paramref name="quality"/> thưởng khớp liền và khớp đầu từ, phạt khoảng hở.
        /// </summary>
        private static bool TryMatchField(string field, string foldedQuery, out int[] positions, out int quality)
        {
            positions = null;
            quality = 0;
            if (string.IsNullOrEmpty(field)) return false;
            List<int> matched = new List<int>(foldedQuery.Length);
            StringBuilder single = new StringBuilder(4);
            int queryIndex = 0;
            for (int sourceIndex = 0; sourceIndex < field.Length && queryIndex < foldedQuery.Length; sourceIndex++)
            {
                single.Length = 0;
                AppendFolded(single, field[sourceIndex]);
                // Ký tự gốc bỏ dấu ra rỗng (dấu kết hợp đứng riêng) thì không khớp gì; ra nhiều ký tự thì phải khớp liền cả cụm.
                if (single.Length == 0) continue;
                int consumed = 0;
                while (consumed < single.Length && queryIndex + consumed < foldedQuery.Length && single[consumed] == foldedQuery[queryIndex + consumed]) consumed++;
                if (consumed == single.Length || (consumed > 0 && queryIndex + consumed == foldedQuery.Length))
                {
                    matched.Add(sourceIndex);
                    queryIndex += consumed;
                }
            }
            if (queryIndex < foldedQuery.Length) return false;
            positions = matched.ToArray();
            quality = QualityOf(field, positions);
            return true;
        }

        private static int QualityOf(string field, int[] positions)
        {
            int gaps = 0;
            for (int index = 1; index < positions.Length; index++) gaps += positions[index] - positions[index - 1] - 1;
            int score = -Math.Min(gaps, MaximumGapPenalty);
            if (gaps == 0) score += ContiguousBonus;
            if (positions.Length > 0 && IsWordStart(field, positions[0])) score += WordStartBonus;
            return score;
        }

        private static bool IsWordStart(string field, int index)
        {
            return index == 0 || !char.IsLetterOrDigit(field[index - 1]);
        }

        private static string Highlight(string title, int[] positions)
        {
            HashSet<int> bold = new HashSet<int>(positions);
            StringBuilder rich = new StringBuilder(title.Length + 32);
            int segmentStart = 0;
            while (segmentStart < title.Length)
            {
                bool isBold = bold.Contains(segmentStart);
                int segmentEnd = segmentStart;
                while (segmentEnd < title.Length && bold.Contains(segmentEnd) == isBold) segmentEnd++;
                // Dấu kết hợp đứng sau chữ khớp (chuỗi dạng FormD) đi cùng chữ đó để thẻ đậm không cắt đôi một chữ.
                while (segmentEnd < title.Length && CharUnicodeInfo.GetUnicodeCategory(title[segmentEnd]) == UnicodeCategory.NonSpacingMark) segmentEnd++;
                string segment = Escape(title.Substring(segmentStart, segmentEnd - segmentStart));
                if (isBold) rich.Append(BoldOpen).Append(segment).Append(BoldClose);
                else rich.Append(segment);
                segmentStart = segmentEnd;
            }
            return rich.ToString();
        }

        private static string FoldQuery(string query)
        {
            if (string.IsNullOrEmpty(query)) return string.Empty;
            StringBuilder folded = new StringBuilder(query.Length);
            foreach (char character in query)
            {
                if (char.IsWhiteSpace(character)) continue;
                AppendFolded(folded, character);
            }
            return folded.ToString();
        }

        private static void AppendFolded(StringBuilder builder, char character)
        {
            // đ/Đ không có dạng tách dấu trong Unicode (là chữ riêng) — FormD không đổi nó, phải thay tay.
            if (character == LowercaseD || character == UppercaseD)
            {
                builder.Append('d');
                return;
            }
            string decomposed = character.ToString().Normalize(NormalizationForm.FormD);
            foreach (char part in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(part) == UnicodeCategory.NonSpacingMark) continue;
                builder.Append(char.ToLowerInvariant(part));
            }
        }
    }
}
