using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Khung catalog (§1.2, §1.3 đặc tả G-I18N): mọi thành viên <see cref="LiveOpsHubStrings"/> tra được chữ, mọi khoá catalog
    /// có đúng một thành viên dùng nó, chỗ giữ chỗ khớp giữa hai bản, khoá trùng bị chặn, thiếu khoá không ném.
    /// <para>
    /// Bước 1 của G-I18N mới dựng hạ tầng: bản tiếng Anh cố tình để trống, gói dịch (bước 2) mới điền. Nên khoá chưa dịch chỉ
    /// là CẢNH BÁO liệt kê ra (<see cref="UntranslatedKeys_AreListedAsWarning"/>), không làm đỏ; khi bước 2 xong, đổi cảnh báo
    /// đó thành assert là chốt được luật "đủ hai ngôn ngữ".
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class LiveOpsHubStringCatalogTests
    {
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
            foreach (PropertyInfo member in StringMembers())
            {
                string text = (string)member.GetValue(null, null);
                if (string.IsNullOrEmpty(text) || text.StartsWith(LiveOpsHubStringCatalog.MissingTextOpenMark, StringComparison.Ordinal))
                {
                    offenders.Add(member.Name);
                }
            }

            Assert.IsEmpty(offenders, "khoá không tra được chữ (rơi về ⟨khoá⟩): " + string.Join(", ", offenders.ToArray()));
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
                    // Chưa dịch thì không có gì để so — ca đó do UntranslatedKeys_AreListedAsWarning lo.
                    if (!LiveOpsHubStringCatalog.TryGetExact(language, key, out translated)) continue;
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
        /// Không assert: bước 1 của G-I18N cố tình để trống bản tiếng Anh. Cảnh báo in ra danh sách khoá còn thiếu để gói dịch
        /// biết chính xác phải điền gì, và để cổng gói bước 2 thấy con số về 0.
        /// </summary>
        [Test]
        public void UntranslatedKeys_AreListedAsWarning()
        {
            foreach (LiveOpsHubLanguageId language in LiveOpsHubLanguage.Available)
            {
                if (language == LiveOpsHubStringCatalog.SourceLanguage) continue;
                List<string> missing = new List<string>();
                foreach (string key in LiveOpsHubStringCatalog.Keys)
                {
                    string text;
                    if (!LiveOpsHubStringCatalog.TryGetExact(language, key, out text)) missing.Add(key);
                }

                if (missing.Count == 0) continue;
                StringBuilder report = new StringBuilder();
                report.Append("LiveOps Hub i18n: ").Append(missing.Count.ToString(CultureInfo.InvariantCulture))
                    .Append('/').Append(LiveOpsHubStringCatalog.Keys.Count.ToString(CultureInfo.InvariantCulture))
                    .Append(" khoá chưa có bản ").Append(language).Append(" — ").Append(string.Join(", ", missing.ToArray()));
                Debug.LogWarning(report.ToString());
            }
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
