using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Khung catalog (§1.2, §1.3 đặc tả G-I18N): mọi thành viên <see cref="LiveOpsHubStrings"/> tra được chữ, mọi khoá catalog
    /// có đúng một thành viên dùng nó, chỗ giữ chỗ khớp giữa hai bản, khoá trùng bị chặn, thiếu khoá không ném.
    /// <para>
    /// Bước dịch đã xong nên khoá thiếu một bản là ĐỎ (<see cref="EveryCatalogKey_HasBothLanguages_NonEmpty"/>), không còn là
    /// cảnh báo: một khoá chưa dịch sẽ im lặng hiện câu tiếng Việt cho người đang đọc tiếng Anh, đúng thứ mà gói này sinh ra để
    /// chặn. Mọi thành viên cũng phải tra được chữ ở TỪNG ngôn ngữ, không chỉ ngôn ngữ đang ghim.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class LiveOpsHubStringCatalogTests
    {
        /// <summary>Biến môi trường trỏ chỗ ghi bảng đối chiếu bản dịch — lệnh chạy cổng gói đặt, người dùng thường không.</summary>
        private const string TranslationReviewPathVariable = "LIVEOPS_HUB_TRANSLATION_REVIEW";

        private static readonly Regex PlaceholderPattern = new Regex(@"\{(\d+)(?::[^}]*)?\}", RegexOptions.Compiled);

        private static IReadOnlyList<PropertyInfo> StringMembers()
        {
            List<PropertyInfo> members = new List<PropertyInfo>();
            PropertyInfo[] properties = typeof(LiveOpsHubStrings)
                .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType == typeof(string) && property.GetIndexParameters().Length == 0) members.Add(property);
            }
            members.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            return members;
        }

        [Test]
        public void EveryStringsMember_IsACatalogPropertyWithMatchingKey()
        {
            IReadOnlyList<PropertyInfo> members = StringMembers();
            Assert.Greater(members.Count, 600, "LiveOpsHubStrings phải còn đủ chữ của hub — thiếu là vùng nào đó mất file");

            HashSet<string> catalogKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string key in LiveOpsHubStringCatalog.Keys) catalogKeys.Add(key);
            List<string> offenders = new List<string>();
            foreach (PropertyInfo member in members)
            {
                if (!catalogKeys.Contains(member.Name)) offenders.Add(member.Name);
            }

            Assert.IsEmpty(offenders, "thành viên không có khoá cùng tên trong catalog: " + string.Join(", ", offenders.ToArray()));
            Assert.IsEmpty(typeof(LiveOpsHubStrings).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
                "LiveOpsHubStrings không được còn hằng chữ nào — mọi thành viên là property đọc catalog (§1.1)");
        }

        [Test]
        public void EveryStringsMember_ResolvesToText_NeverBracketedKey()
        {
            List<string> offenders = new List<string>();
            // Fixture ghim Tiếng Việt cho cả assembly, nên phải tự mở scope từng ngôn ngữ: đọc đúng ngôn ngữ đang ghim thì
            // một khoá chỉ có bản Việt vẫn xanh, mà đó chính là ca người dùng English gặp câu tiếng Việt.
            foreach (LiveOpsHubLanguageId language in LiveOpsHubLanguage.Available)
            {
                using (LiveOpsHubLanguage.Override(language))
                {
                    foreach (PropertyInfo member in StringMembers())
                    {
                        string text = (string)member.GetValue(null, null);
                        if (string.IsNullOrEmpty(text) || text.StartsWith(LiveOpsHubStringCatalog.MissingTextOpenMark, StringComparison.Ordinal))
                        {
                            offenders.Add(member.Name + " [" + language + "]");
                        }
                    }
                }
            }

            Assert.IsEmpty(offenders, "khoá không tra được chữ (rơi về ⟨khoá⟩): " + string.Join(", ", offenders.ToArray()));
        }

        [Test]
        public void EveryCatalogKey_HasBothLanguages_NonEmpty()
        {
            List<string> offenders = new List<string>();
            foreach (string key in LiveOpsHubStringCatalog.Keys)
            {
                foreach (LiveOpsHubLanguageId language in LiveOpsHubLanguage.Available)
                {
                    string text;
                    // TryGetExact KHÔNG rơi về ngôn ngữ khác: null lẫn chuỗi rỗng đều trả false, đúng luật "một bản rỗng = ĐỎ".
                    if (!LiveOpsHubStringCatalog.TryGetExact(language, key, out text)) offenders.Add(key + " [" + language + "]");
                }
            }

            Assert.IsEmpty(offenders, "khoá thiếu chữ ở một ngôn ngữ (người dùng ngôn ngữ đó sẽ thấy câu của ngôn ngữ kia): "
                + string.Join(", ", offenders.ToArray()));
        }

        [Test]
        public void EveryCatalogKey_HasSourceLanguageText()
        {
            List<string> offenders = new List<string>();
            foreach (string key in LiveOpsHubStringCatalog.Keys)
            {
                string text;
                if (!LiveOpsHubStringCatalog.TryGetExact(LiveOpsHubStringCatalog.SourceLanguage, key, out text)) offenders.Add(key);
            }

            Assert.IsEmpty(offenders, "bản gốc (tiếng Việt) là bắt buộc, thiếu ở: " + string.Join(", ", offenders.ToArray()));
        }

        [Test]
        public void EveryCatalogKey_IsUsedByExactlyOneStringsMember()
        {
            HashSet<string> memberNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (PropertyInfo member in StringMembers()) memberNames.Add(member.Name);

            List<string> deadKeys = new List<string>();
            foreach (string key in LiveOpsHubStringCatalog.Keys)
            {
                if (!memberNames.Contains(key)) deadKeys.Add(key);
            }

            Assert.IsEmpty(deadKeys, "khoá chết (không thành viên nào dùng): " + string.Join(", ", deadKeys.ToArray()));
        }

        [Test]
        public void EveryCatalogKey_PlaceholdersMatchBetweenLanguages()
        {
            List<string> offenders = new List<string>();
            foreach (string key in LiveOpsHubStringCatalog.Keys)
            {
                string source;
                LiveOpsHubStringCatalog.TryGetExact(LiveOpsHubStringCatalog.SourceLanguage, key, out source);
                foreach (LiveOpsHubLanguageId language in LiveOpsHubLanguage.Available)
                {
                    if (language == LiveOpsHubStringCatalog.SourceLanguage) continue;
                    string translated;
                    if (!LiveOpsHubStringCatalog.TryGetExact(language, key, out translated))
                    {
                        offenders.Add(key + " [" + language + "] chưa có chữ để so chỗ giữ chỗ");
                        continue;
                    }
                    string sourceShape = DescribePlaceholders(source);
                    string translatedShape = DescribePlaceholders(translated);
                    if (!string.Equals(sourceShape, translatedShape, StringComparison.Ordinal))
                    {
                        offenders.Add(key + " [" + language + "] gốc " + sourceShape + " ≠ dịch " + translatedShape);
                    }
                }
            }

            Assert.IsEmpty(offenders, "chỗ giữ chỗ lệch giữa hai bản (đảo {0}/{1} là người đọc code hiểu sai tham số): "
                + string.Join(" | ", offenders.ToArray()));
        }

        [Test]
        public void SharedKey_SameTextInEveryLanguage()
        {
            List<string> offenders = new List<string>();
            foreach (string key in LiveOpsHubStringCatalog.Keys)
            {
                if (!LiveOpsHubStringCatalog.IsShared(key)) continue;
                string source;
                LiveOpsHubStringCatalog.TryGetExact(LiveOpsHubStringCatalog.SourceLanguage, key, out source);
                foreach (LiveOpsHubLanguageId language in LiveOpsHubLanguage.Available)
                {
                    string text;
                    LiveOpsHubStringCatalog.TryGetExact(language, key, out text);
                    if (!string.Equals(source, text, StringComparison.Ordinal)) offenders.Add(key + " [" + language + "]");
                }
            }

            Assert.IsEmpty(offenders, "AddShared phải cho cùng một chữ ở mọi ngôn ngữ: " + string.Join(", ", offenders.ToArray()));
        }

        [Test]
        public void DuplicateKey_Throws()
        {
            LiveOpsHubStringTable table = new LiveOpsHubStringTable();
            table.Add("SampleKey", "một", "one");

            Assert.Throws<ArgumentException>(() => table.Add("SampleKey", "hai", "two"),
                "khoá trùng là hai vùng cùng khai một tên — tra chữ sẽ im lặng lấy bản sau, phải chặn lúc dựng bảng");
            Assert.Throws<ArgumentException>(() => table.AddShared("SampleKey", "·"));
            Assert.Throws<ArgumentException>(() => table.Add(string.Empty, "một", "one"));
        }

        [Test]
        public void BuildTable_DoesNotThrow_AndHoldsEveryRegion()
        {
            LiveOpsHubStringTable table = null;
            Assert.DoesNotThrow(() => table = LiveOpsHubStringCatalog.BuildTable(),
                "dựng bảng ném = có khoá trùng giữa hai vùng");
            Assert.AreEqual(LiveOpsHubStringCatalog.Keys.Count, table.Keys.Count);
        }

        [Test]
        public void MissingKey_ReturnsBracketedKey_NeverThrows()
        {
            string text = null;
            Assert.DoesNotThrow(() => text = LiveOpsHubStringCatalog.Text("KhoaKhongCoThat"),
                "thiếu chữ là lỗi lập trình, nhưng cửa sổ vẫn phải dựng được để người dùng thấy chỗ hỏng");
            Assert.AreEqual(LiveOpsHubStringCatalog.MissingTextOpenMark + "KhoaKhongCoThat" + LiveOpsHubStringCatalog.MissingTextCloseMark, text);
            Assert.DoesNotThrow(() => LiveOpsHubStringCatalog.Text(null));
        }

        [Test]
        public void UntranslatedLanguage_FallsBackToSourceText()
        {
            LiveOpsHubStringTable table = new LiveOpsHubStringTable();
            table.Add("SampleKey", "chữ gốc", null);

            string english;
            Assert.IsFalse(table.TryGet(LiveOpsHubLanguageId.English, "SampleKey", out english),
                "chưa dịch thì TryGet của ngôn ngữ đó phải trả false — bảng đối chiếu bản dịch đếm bằng đường này");
            Assert.IsTrue(table.TryGet(LiveOpsHubLanguageId.Vietnamese, "SampleKey", out english));
        }

        /// <summary>
        /// Thứ tự tra dự phòng của §1.3: thiếu chữ ở ngôn ngữ đang chọn thì rơi về ENGLISH (mặc định của hub), không rơi về
        /// bản gốc tiếng Việt. Soi trên bảng riêng vì bảng thật không được phép có khoá thiếu chữ.
        /// </summary>
        [Test]
        public void MissingTextInCurrentLanguage_FallsBackToEnglish()
        {
            LiveOpsHubStringTable table = new LiveOpsHubStringTable();
            table.Add("OnlyEnglishKey", null, "English only");
            table.Add("OnlyVietnameseKey", "Chỉ có tiếng Việt", null);

            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.Vietnamese))
            {
                Assert.AreEqual("English only", LiveOpsHubStringCatalog.Resolve(table, "OnlyEnglishKey"),
                    "thiếu bản tiếng Việt thì rơi về English, không được trả rỗng");
                Assert.AreEqual("Chỉ có tiếng Việt", LiveOpsHubStringCatalog.Resolve(table, "OnlyVietnameseKey"));
            }

            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.English))
            {
                Assert.AreEqual("English only", LiveOpsHubStringCatalog.Resolve(table, "OnlyEnglishKey"));
                Assert.AreEqual("Chỉ có tiếng Việt", LiveOpsHubStringCatalog.Resolve(table, "OnlyVietnameseKey"),
                    "hết đường dự phòng thì vẫn phải có chữ để cửa sổ dựng được");
            }
        }

        /// <summary>
        /// Bảng đối chiếu bản dịch cho user soát ở cổng gói (§8 bước 9, §9): sinh bằng reflection từ chính catalog, KHÔNG gõ
        /// tay. Luôn dựng nội dung để đường sinh có test đi qua; chỉ ghi ra file khi lệnh chạy đặt biến môi trường
        /// <see cref="TranslationReviewPathVariable"/> — test không được tự ý ghi vào máy người chạy.
        /// </summary>
        [Test]
        public void TranslationReviewTable_IsGeneratedFromCatalog()
        {
            string markdown = BuildTranslationReview();

            Assert.IsTrue(markdown.Contains(nameof(LiveOpsHubStrings.ShellOverviewTitle)),
                "bảng đối chiếu phải liệt kê mọi khoá của catalog");
            Assert.AreEqual(LiveOpsHubStringCatalog.Keys.Count, CountTableRows(markdown), "mỗi khoá đúng một hàng");

            string outputPath = Environment.GetEnvironmentVariable(TranslationReviewPathVariable);
            if (string.IsNullOrEmpty(outputPath)) return;
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(outputPath, markdown, new UTF8Encoding(false));
        }

        private static string BuildTranslationReview()
        {
            int sharedCount = 0;
            foreach (string key in LiveOpsHubStringCatalog.Keys)
            {
                if (LiveOpsHubStringCatalog.IsShared(key)) sharedCount++;
            }

            StringBuilder markdown = new StringBuilder();
            markdown.Append("# LiveOps Hub — bảng đối chiếu bản dịch\n\n");
            markdown.Append("Sinh tự động bằng reflection từ `LiveOpsHubStringCatalog` (test `")
                .Append(nameof(TranslationReviewTable_IsGeneratedFromCatalog)).Append("`), không gõ tay.\n\n");
            markdown.Append("Tổng ").Append(LiveOpsHubStringCatalog.Keys.Count.ToString(CultureInfo.InvariantCulture))
                .Append(" khoá, trong đó ").Append(sharedCount.ToString(CultureInfo.InvariantCulture))
                .Append(" khoá ký hiệu dùng chung hai ngôn ngữ (`AddShared`). Thứ tự hàng = thứ tự đăng ký trong file catalog.\n\n");
            markdown.Append("| # | Khoá | Tiếng Việt | English |\n|---|---|---|---|\n");

            int row = 0;
            foreach (string key in LiveOpsHubStringCatalog.Keys)
            {
                row++;
                string vietnamese;
                string english;
                LiveOpsHubStringCatalog.TryGetExact(LiveOpsHubLanguageId.Vietnamese, key, out vietnamese);
                LiveOpsHubStringCatalog.TryGetExact(LiveOpsHubLanguageId.English, key, out english);
                markdown.Append("| ").Append(row.ToString(CultureInfo.InvariantCulture))
                    .Append(" | `").Append(key).Append("` | ").Append(ForMarkdownCell(vietnamese))
                    .Append(" | ").Append(ForMarkdownCell(english)).Append(" |\n");
            }

            return markdown.ToString();
        }

        private static int CountTableRows(string markdown)
        {
            int rows = 0;
            foreach (string line in markdown.Split('\n'))
            {
                if (line.StartsWith("| ", StringComparison.Ordinal) && line.Contains("` |")) rows++;
            }

            return rows;
        }

        /// <summary>Ô bảng markdown: gạch đứng và xuống dòng phải thoát, nếu không bảng vỡ và người soát đọc nhầm cột.</summary>
        private static string ForMarkdownCell(string text)
        {
            if (string.IsNullOrEmpty(text)) return "**(thiếu)**";
            return text.Replace("|", "\\|").Replace("\n", "<br>").Replace("\r", string.Empty);
        }

        /// <summary>Tập chỉ số <c>{n}</c> kèm số lần dùng, viết thành chuỗi so sánh được; <c>{</c> lạc cũng lộ ra.</summary>
        private static string DescribePlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            SortedDictionary<int, int> countByIndex = new SortedDictionary<int, int>();
            foreach (Match match in PlaceholderPattern.Matches(text))
            {
                int index = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                countByIndex[index] = countByIndex.ContainsKey(index) ? countByIndex[index] + 1 : 1;
            }

            StringBuilder shape = new StringBuilder();
            foreach (KeyValuePair<int, int> pair in countByIndex)
            {
                shape.Append('{').Append(pair.Key.ToString(CultureInfo.InvariantCulture)).Append("}×")
                    .Append(pair.Value.ToString(CultureInfo.InvariantCulture)).Append(' ');
            }

            int braces = 0;
            foreach (char character in text)
            {
                if (character == '{') braces++;
            }

            shape.Append("braces=").Append(braces.ToString(CultureInfo.InvariantCulture));
            return shape.ToString();
        }
    }
}
