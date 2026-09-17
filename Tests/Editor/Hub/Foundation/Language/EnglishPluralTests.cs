using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Luật số ít/số nhiều của bản tiếng Anh (Q-W5-2, user chốt 17/9/2026) — MỘT luật chung, quét CẢ catalog một lượt.
    /// Bốn điều phải đúng: (1) <see cref="LiveOpsHubEnglishPlural.Resolve"/> chọn đúng vế cho n = 0/1/2 và cho nhiều dấu
    /// trong một câu; (2) mọi khoá có đối số đếm hoặc mang dấu, hoặc nằm trong
    /// <see cref="LiveOpsHubEnglishPluralExceptions"/> kèm lý do; (3) câu tiếng Anh của khoá đếm khi n = 1 không còn danh
    /// từ số nhiều trần ("1 rules"); (4) không câu tiếng Việt nào mang dấu — tiếng Việt không chia số.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class EnglishPluralTests
    {
        /// <summary>Ứng viên khoá đếm: một <c>{i}</c> (có thể là <c>{i}/{j}</c>) rồi tới một từ thường kết thúc bằng "s".</summary>
        private static readonly Regex CountedNounCandidate = new Regex(@"\{(\d)\}(?:/\{(\d)\})?\s+([a-z][a-z\-]*s)\b", RegexOptions.CultureInvariant);

        /// <summary>Dấu số ít/số nhiều trong câu — dùng để biết khoá nào đã theo luật.</summary>
        private static readonly Regex PluralMark = new Regex(@"\{(\d)\|([^|{}]*)\|([^|{}]*)\}", RegexOptions.CultureInvariant);

        [Test]
        public void Resolve_PicksSingularOnlyForIntegerOne()
        {
            const string text = "{0} {0|event|events} dropped";

            Assert.AreEqual("0 events dropped", Format(text, 0));
            Assert.AreEqual("1 event dropped", Format(text, 1));
            Assert.AreEqual("2 events dropped", Format(text, 2));
            Assert.AreEqual("-1 events dropped", Format(text, -1), "âm không phải số ít");
        }

        [Test]
        public void Resolve_HandlesSeveralMarksInOneSentence()
        {
            const string text = "Checked · {0} {0|rule|rules} · {1} {1|finding|findings}";

            Assert.AreEqual("Checked · 1 rule · 1 finding", Format(text, 1, 1));
            Assert.AreEqual("Checked · 1 rule · 9 findings", Format(text, 1, 9));
            Assert.AreEqual("Checked · 9 rules · 1 finding", Format(text, 9, 1));
        }

        /// <summary>
        /// Số đếm đã định dạng sẵn (<c>LiveOpsHubFormat.Integer</c>) vẫn phải chọn đúng vế: rất nhiều câu của hub đưa CHUỖI
        /// vào chỗ số, không nhận chuỗi thì luật im lặng không chạy ở đúng chỗ cần nó.
        /// </summary>
        [Test]
        public void Resolve_ReadsPreFormattedCountStrings()
        {
            const string text = "{0} {0|entry|entries} kept";

            Assert.AreEqual("1 entry kept", Format(text, "1"));
            Assert.AreEqual("1,000 entries kept", Format(text, "1,000"));
            Assert.AreEqual("lava-quest entries kept", Format(text, "lava-quest"), "chuỗi không đọc được thành số thì lấy vế số nhiều");
        }

        [Test]
        public void StripToPlural_LeavesAValidFormatString()
        {
            Assert.AreEqual("{0} events dropped", LiveOpsHubEnglishPlural.StripToPlural("{0} {0|event|events} dropped"));
            Assert.IsFalse(LiveOpsHubEnglishPlural.ContainsMark(LiveOpsHubEnglishPlural.StripToPlural("{0} {0|event|events}")));
            // Khuôn sau khi bỏ dấu phải đưa thẳng cho string.Format được — đó chính là lưới an toàn của LiveOpsHubStringTable.
            Assert.AreEqual("9 events dropped", string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubEnglishPlural.StripToPlural("{0} {0|event|events} dropped"), 9));
        }

        /// <summary>
        /// (2) Quét CẢ catalog: mọi ứng viên khoá đếm phải mang dấu hoặc có tên trong sổ ngoại lệ. Sổ ngoại lệ cũng không
        /// được mục nát — khoá đã xoá khỏi catalog mà còn nằm trong sổ thì test đỏ.
        /// </summary>
        [Test]
        public void EveryCountedKey_HasAMarkOrAWrittenDownReason()
        {
            List<string> missing = new List<string>();
            foreach (string key in LiveOpsHubStringCatalog.Keys)
            {
                string english;
                if (!LiveOpsHubStringCatalog.TryGetExactRaw(LiveOpsHubLanguageId.English, key, out english)) continue;
                if (LiveOpsHubEnglishPlural.ContainsMark(english)) continue;
                if (LiveOpsHubEnglishPluralExceptions.KeysWithoutMark.ContainsKey(key)) continue;
                foreach (Match match in CountedNounCandidate.Matches(english))
                {
                    string noun = match.Groups[3].Value;
                    if (Contains(LiveOpsHubEnglishPluralExceptions.WordsThatAreNotCountedNouns, noun)) continue;
                    missing.Add(key + " → \"" + match.Value + "\"");
                    break;
                }
            }
            CollectionAssert.IsEmpty(missing,
                "khoá đếm phải mang dấu {i|số ít|số nhiều} hoặc có lý do trong LiveOpsHubEnglishPluralExceptions");

            foreach (KeyValuePair<string, string> exception in LiveOpsHubEnglishPluralExceptions.KeysWithoutMark)
            {
                Assert.IsTrue(LiveOpsHubStringCatalog.ContainsKey(exception.Key),
                    "sổ ngoại lệ nhắc tới khoá không còn trong catalog: " + exception.Key);
                Assert.IsNotEmpty(exception.Value, "mỗi ngoại lệ phải có lý do viết ra: " + exception.Key);
            }
        }

        /// <summary>
        /// (3) Với n = 1, câu tiếng Anh của MỌI khoá mang dấu phải đọc vế số ít — và câu đã format không được còn "1 &lt;số
        /// nhiều&gt;" ở đâu cả ("1 rules", "1 events", "1 findings"). So bằng chuỗi DỰNG LẠI chứ không bằng "có chứa": tên
        /// màn "Event types" nằm ngay trong một câu có dấu <c>{i|type|types}</c>, phép "có chứa" sẽ báo đỏ giả ở đó.
        /// </summary>
        [Test]
        public void MarkedKeys_ReadSingularWhenCountIsOne()
        {
            List<string> wrong = new List<string>();
            foreach (string key in LiveOpsHubStringCatalog.Keys)
            {
                string english;
                if (!LiveOpsHubStringCatalog.TryGetExactRaw(LiveOpsHubLanguageId.English, key, out english)) continue;
                MatchCollection marks = PluralMark.Matches(english);
                if (marks.Count == 0) continue;

                object[] ones = OnesFor(english);
                string expectedSingular = PluralMark.Replace(english, match => match.Groups[2].Value);
                string expectedPlural = PluralMark.Replace(english, match => match.Groups[3].Value);
                if (!string.Equals(expectedSingular, LiveOpsHubEnglishPlural.Resolve(english, ones), StringComparison.Ordinal))
                {
                    wrong.Add(key + ": n = 1 không ra vế số ít");
                }
                if (!string.Equals(expectedPlural, LiveOpsHubEnglishPlural.StripToPlural(english), StringComparison.Ordinal))
                {
                    wrong.Add(key + ": bỏ dấu không ra vế số nhiều");
                }
                if (string.Equals(expectedSingular, expectedPlural, StringComparison.Ordinal))
                {
                    wrong.Add(key + ": dấu không đổi được gì — hai vế y hệt nhau");
                }

                string formatted = FormatKey(key, ones);
                foreach (Match mark in marks)
                {
                    string pluralSide = mark.Groups[3].Value;
                    if (string.Equals(mark.Groups[2].Value, pluralSide, StringComparison.Ordinal)) continue;
                    if (formatted.Contains("1 " + pluralSide)) wrong.Add(key + ": còn \"1 " + pluralSide + "\" khi n = 1");
                }
            }
            CollectionAssert.IsEmpty(wrong, "khoá đếm phải đọc vế số ít khi n = 1 và vế số nhiều cho mọi n khác");
        }

        /// <summary>Ba câu thật của catalog, ghim nguyên văn — luật chung đúng mà câu cụ thể sai thì test kia không thấy.</summary>
        [Test]
        public void RealSentences_ReadNaturallyForOneAndForMany()
        {
            Assert.AreEqual("Readable: 1 recurring rule · 1 event",
                FormatKey(nameof(LiveOpsHubStrings.PasteReadableFormat), 1, 1));
            Assert.AreEqual("Readable: 2 recurring rules · 7 events",
                FormatKey(nameof(LiveOpsHubStrings.PasteReadableFormat), 2, 7));
            Assert.AreEqual("1 event dropped",
                FormatKey(nameof(LiveOpsHubStrings.ExportGateReasonDroppedFormat), 1));
            Assert.AreEqual("0 events dropped",
                FormatKey(nameof(LiveOpsHubStrings.ExportGateReasonDroppedFormat), 0));
        }

        /// <summary>(4) Tiếng Việt không chia số — một dấu lọt vào bản tiếng Việt là câu in ra kèm ngoặc nhọn.</summary>
        [Test]
        public void VietnameseSentences_NeverCarryAMark()
        {
            List<string> marked = new List<string>();
            foreach (string key in LiveOpsHubStringCatalog.Keys)
            {
                string vietnamese;
                if (!LiveOpsHubStringCatalog.TryGetExactRaw(LiveOpsHubLanguageId.Vietnamese, key, out vietnamese)) continue;
                if (LiveOpsHubStringCatalog.IsShared(key)) continue;
                if (LiveOpsHubEnglishPlural.ContainsMark(vietnamese)) marked.Add(key);
            }
            CollectionAssert.IsEmpty(marked, "tiếng Việt không chia số — dấu {i|…|…} chỉ có ở bản tiếng Anh");
        }

        /// <summary>Mảng đối số toàn số 1, đủ dài cho chỉ số lớn nhất mà câu nhắc tới.</summary>
        private static object[] OnesFor(string text)
        {
            int highestIndex = -1;
            foreach (Match match in Regex.Matches(text, @"\{(\d)", RegexOptions.CultureInvariant))
            {
                int index = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                if (index > highestIndex) highestIndex = index;
            }
            object[] arguments = new object[highestIndex + 1];
            for (int index = 0; index < arguments.Length; index++) arguments[index] = 1;
            return arguments;
        }

        private static bool Contains(IReadOnlyList<string> words, string word)
        {
            for (int index = 0; index < words.Count; index++)
            {
                if (string.Equals(words[index], word, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static string Format(string text, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubEnglishPlural.Resolve(text, arguments), arguments);
        }

        private static string FormatKey(string key, params object[] arguments)
        {
            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.English))
            {
                return LiveOpsHubStringCatalog.Format(key, arguments);
            }
        }
    }
}
