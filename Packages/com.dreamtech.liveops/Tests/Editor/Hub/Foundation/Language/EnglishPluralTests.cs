using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        /// <summary>
        /// Ứng viên khoá đếm: một <c>{i}</c> (có thể là <c>{i}/{j}</c>), rồi TỚI BA từ thường chen giữa, rồi một từ thường
        /// kết thúc bằng "s". Ba từ chen giữa là chỗ của tính từ ("{0} unsaved changes", "{0} safe changes"): bản đầu của
        /// luật chỉ nhìn từ đứng NGAY SAU <c>{i}</c> nên bốn câu đếm ấy không bao giờ thành ứng viên, không đỏ, không vào sổ
        /// ngoại lệ — đúng lỗ hổng phải bịt khi quét lại toàn catalog (Q-W5-2, 17/9/2026).
        /// <para>
        /// Từ nào trong khoảng chen giữa nằm ở <see cref="LiveOpsHubEnglishPluralExceptions.WordsThatAreNotCountedNouns"/>
        /// thì ứng viên bị bỏ: <c>"{0} is running"</c> hay <c>"{0} field still holds an unsaved draft"</c> không phải câu đếm.
        /// </para>
        /// </summary>
        private static readonly Regex CountedNounCandidate = new Regex(
            @"\{(\d)\}(?:/\{(\d)\})?\s+((?:[a-z][a-z\-]*\s+){0,3}?)([a-z][a-z\-]*s)\b", RegexOptions.CultureInvariant);

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
        /// vào chỗ số, không nhận chuỗi thì luật im lặng không chạy ở đúng chỗ cần nó. Chỉ có n = 1 là quan trọng, và
        /// <c>Integer(1)</c> ra đúng <c>"1"</c>; mọi số lớn hơn đọc vế số nhiều dù dấu phân nhóm của hub (dấu CHẤM, "1.000")
        /// không phải dấu phân nhóm của <c>CultureInfo.InvariantCulture</c> — nên test lấy CHÍNH chuỗi mà hub sinh ra, chứ
        /// không ghim một khuôn "1,000" mà hub chẳng bao giờ in.
        /// </summary>
        [Test]
        public void Resolve_ReadsPreFormattedCountStrings()
        {
            const string text = "{0} {0|entry|entries} kept";
            LiveOpsHubFormat format = new LiveOpsHubFormat(TimeSpan.Zero);

            Assert.AreEqual("1", format.Integer(1), "luật số ít chỉ gặp n = 1, và hub in n = 1 thành đúng một chữ số");
            Assert.AreEqual("1 entry kept", Format(text, format.Integer(1)));
            Assert.AreEqual(format.Integer(1000) + " entries kept", Format(text, format.Integer(1000)));
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
                    if (!IsCountedNounPhrase(match)) continue;
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

        /// <summary>
        /// (5) Khoá đã đánh dấu thì CHỖ GỌI phải đưa KHOÁ cho <c>LiveOpsHubStringCatalog.Format</c>, không được đọc câu trần
        /// qua <c>LiveOpsHubStrings.&lt;Khoá&gt;</c> rồi tự <c>string.Format</c>: đường trần đi qua <c>StripToPlural</c> nên nó
        /// im lặng trả vế SỐ NHIỀU cho mọi n. Không test nào khác thấy chuyện đó — cả bộ test UI ghim tiếng Việt, mà câu
        /// tiếng Việt không mang dấu nên hai đường cho kết quả y hệt nhau (Q-W5-2, lưới gác thêm 17/9/2026).
        /// <para>
        /// Quét MÃ NGUỒN của <c>Editor/</c> chứ không quét runtime: lỗi này là lỗi lúc VIẾT, và quét runtime thì phải gọi
        /// được hết mọi màn mới thấy. Bỏ qua dòng chú thích và mọi <c>nameof(...)</c> — đó là hai cách nhắc tên khoá hợp lệ.
        /// </para>
        /// </summary>
        [Test]
        public void MarkedKeys_AreNeverReadAsABareStringsProperty()
        {
            List<string> markedKeys = new List<string>();
            foreach (string key in LiveOpsHubStringCatalog.Keys)
            {
                string english;
                if (!LiveOpsHubStringCatalog.TryGetExactRaw(LiveOpsHubLanguageId.English, key, out english)) continue;
                if (LiveOpsHubEnglishPlural.ContainsMark(english)) markedKeys.Add(key);
            }
            CollectionAssert.IsNotEmpty(markedKeys, "catalog phải có khoá mang dấu — không thì chính test này vô nghĩa");

            string editorDirectory = Path.Combine(PackageDirectory(), EditorFolderName);
            Assert.IsTrue(Directory.Exists(editorDirectory), "không thấy thư mục mã nguồn Editor ở " + editorDirectory);
            string[] sourceFiles = Directory.GetFiles(editorDirectory, CSharpSearchPattern, SearchOption.AllDirectories);
            Assert.Greater(sourceFiles.Length, 0, "không đọc được file .cs nào dưới Editor/");

            List<string> bareUses = new List<string>();
            for (int fileIndex = 0; fileIndex < sourceFiles.Length; fileIndex++)
            {
                string[] lines = File.ReadAllText(sourceFiles[fileIndex]).Replace("\r\n", "\n").Split('\n');
                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    string line = lines[lineIndex];
                    if (line.TrimStart().StartsWith(CommentPrefix, StringComparison.Ordinal)) continue;
                    for (int keyIndex = 0; keyIndex < markedKeys.Count; keyIndex++)
                    {
                        if (!ReadsBareProperty(line, markedKeys[keyIndex])) continue;
                        bareUses.Add(Path.GetFileName(sourceFiles[fileIndex]) + ":" + (lineIndex + 1).ToString(CultureInfo.InvariantCulture)
                            + " → " + markedKeys[keyIndex]);
                    }
                }
            }
            CollectionAssert.IsEmpty(bareUses,
                "khoá mang dấu phải gọi qua LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.<Khoá>), …)");
        }

        /// <summary>Dòng này có đọc <c>LiveOpsHubStrings.&lt;Khoá&gt;</c> trần (không nằm trong <c>nameof</c>) hay không.</summary>
        private static bool ReadsBareProperty(string line, string key)
        {
            string reference = StringsTypeName + "." + key;
            int searchFrom = 0;
            while (searchFrom <= line.Length - reference.Length)
            {
                int found = line.IndexOf(reference, searchFrom, StringComparison.Ordinal);
                if (found < 0) return false;
                int afterReference = found + reference.Length;
                bool isWholeName = afterReference >= line.Length || !IsNameCharacter(line[afterReference]);
                bool insideNameOf = found >= NameOfPrefix.Length
                    && string.Equals(line.Substring(found - NameOfPrefix.Length, NameOfPrefix.Length), NameOfPrefix, StringComparison.Ordinal);
                if (isWholeName && !insideNameOf) return true;
                searchFrom = found + 1;
            }
            return false;
        }

        private static bool IsNameCharacter(char character)
        {
            return character == '_' || (character >= '0' && character <= '9')
                || (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z');
        }

        /// <summary>
        /// Gốc package ĐANG ĐƯỢC THỬ, hỏi Package Manager qua assembly của chính file này — không suy từ thư mục hiện hành:
        /// lượt 2022.3 chạy trong project tạm trỏ <c>file:</c> vào worktree, cwd ở đó không nói gì về bộ file vừa biên dịch.
        /// </summary>
        private static string PackageDirectory()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(EnglishPluralTests).Assembly);
            Assert.IsNotNull(package,
                "không hỏi được gốc package từ assembly test — bộ test này phải chạy như một phần của com.dreamtech.liveops");
            return Path.GetFullPath(package.resolvedPath);
        }

        private const string EditorFolderName = "Editor";
        private const string CSharpSearchPattern = "*.cs";
        private const string CommentPrefix = "//";
        private const string StringsTypeName = "LiveOpsHubStrings";
        private const string NameOfPrefix = "nameof(";

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

        /// <summary>
        /// Cụm khớp có thật sự là "số đếm + danh từ" không: cả danh từ LẪN mọi từ chen giữa đều phải nằm ngoài sổ từ không
        /// đếm được. Chỉ lọc mỗi danh từ thì "{0} field still holds …" thành ứng viên giả vì "holds" kết thúc bằng "s".
        /// </summary>
        private static bool IsCountedNounPhrase(Match match)
        {
            if (Contains(LiveOpsHubEnglishPluralExceptions.WordsThatAreNotCountedNouns, match.Groups[4].Value)) return false;
            string[] wordsBetween = match.Groups[3].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < wordsBetween.Length; index++)
            {
                if (Contains(LiveOpsHubEnglishPluralExceptions.WordsThatAreNotCountedNouns, wordsBetween[index])) return false;
            }
            return true;
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
