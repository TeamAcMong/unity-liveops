using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Cổng phủ của ma trận ảnh 9.5 (gói G-ACCEPT, W7): bảng hằng id đóng từ W0 và bộ kịch bản đăng ký phải khớp nhau ĐÚNG
    /// một–một, trừ những chỗ có tên và lý do trong <see cref="LiveOpsHubCaptureDeferrals"/>.
    /// <para>
    /// Vì sao cần một test riêng thay vì tin vào lượt chụp: lượt chụp chỉ chụp những kịch bản ĐÃ đăng ký, nên một hằng bị bỏ
    /// quên không làm lượt chụp đỏ — nó chỉ lặng lẽ không có ảnh, và contact sheet của cổng đợt cũng không có ô nào để người
    /// duyệt thấy là thiếu. Đây là chỗ duy nhất đọc bảng hằng làm nguồn chuẩn và bắt chênh lệch cả hai chiều.
    /// </para>
    /// <para>
    /// Test chạy được <c>-nographics</c> (category Logic): nó chỉ DỰNG danh sách kịch bản (mỗi kịch bản là id + kích thước +
    /// một lambda chưa gọi), không mở cửa sổ nào.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class CaptureCoverageTests
    {
        /// <summary>Mục của CHANGELOG mà mọi id hoãn phải có một dòng — người đọc bản phát hành không mở mã nguồn.</summary>
        private const string ChangelogDeferredSectionHeading = "### Chưa có ở bản này";

        private const string ChangelogFileName = "CHANGELOG.md";

        [Test]
        public void EveryDeclaredScenarioId_HasExactlyOneRegisteredScenario_OrIsDeferredWithAReason()
        {
            IReadOnlyDictionary<string, string> declared = DeclaredScenarioIdsByConstantName();
            IReadOnlyDictionary<string, int> registeredCounts = RegisteredScenarioIdCounts();
            IReadOnlyDictionary<string, string> deferred = LiveOpsHubCaptureDeferrals.DeferredScenarioIds;

            List<string> problems = new List<string>();
            foreach (KeyValuePair<string, string> entry in declared)
            {
                string constantName = entry.Key;
                string scenarioId = entry.Value;
                registeredCounts.TryGetValue(scenarioId, out int count);
                bool isDeferred = deferred.ContainsKey(constantName);

                if (count == 1 && !isDeferred) continue;
                if (count == 0 && isDeferred) continue;

                if (count > 1)
                {
                    problems.Add(scenarioId + " (" + constantName + "): đăng ký " + count
                        + " lần — hai ảnh cùng tên sẽ ghi đè nhau");
                }
                else if (count == 0)
                {
                    problems.Add(scenarioId + " (" + constantName + "): không có kịch bản nào đăng ký, và cũng không nằm trong"
                        + " LiveOpsHubCaptureDeferrals.DeferredScenarioIds kèm lý do — hình của ma trận 9.5 này sẽ thiếu ảnh mà"
                        + " không ai thấy");
                }
                else
                {
                    problems.Add(scenarioId + " (" + constantName + "): vừa có kịch bản đăng ký vừa bị khai là hoãn — bỏ nó khỏi"
                        + " DeferredScenarioIds, ảnh đã có rồi");
                }
            }

            Assert.IsEmpty(problems, "Phủ ma trận ảnh 9.5 sai:\n - " + string.Join("\n - ", problems.ToArray()));
        }

        [Test]
        public void EveryRegisteredScenarioId_IsDeclaredAsAConstant_OrIsListedAsAKnownExtra()
        {
            IReadOnlyDictionary<string, string> declared = DeclaredScenarioIdsByConstantName();
            HashSet<string> declaredIds = new HashSet<string>(declared.Values, StringComparer.Ordinal);
            IReadOnlyDictionary<string, string> knownExtras = LiveOpsHubCaptureDeferrals.ScenarioIdsOutsideTheConstantTable;

            List<string> problems = new List<string>();
            foreach (string scenarioId in RegisteredScenarioIdCounts().Keys)
            {
                if (declaredIds.Contains(scenarioId)) continue;
                if (knownExtras.ContainsKey(scenarioId)) continue;
                problems.Add(scenarioId);
            }

            Assert.IsEmpty(problems, "Kịch bản chụp mang id không có trong bảng hằng và cũng không khai ở"
                + " LiveOpsHubCaptureDeferrals.ScenarioIdsOutsideTheConstantTable — ma trận truy vết 9.5 sẽ không biết ảnh này"
                + " thuộc hình nào:\n - " + string.Join("\n - ", problems.ToArray()));
        }

        /// <summary>
        /// Sổ hoãn không được mục nát: một hằng bị đổi tên hay xoá mà sổ vẫn giữ tên cũ thì mục ấy thành lời hứa chết — nó
        /// không còn che cho id nào nữa nhưng vẫn trông như một khoản nợ đang được theo dõi.
        /// </summary>
        [Test]
        public void EveryDeferredEntry_NamesARealConstant_AndCarriesANonEmptyReason()
        {
            IReadOnlyDictionary<string, string> declared = DeclaredScenarioIdsByConstantName();

            List<string> problems = new List<string>();
            foreach (KeyValuePair<string, string> entry in LiveOpsHubCaptureDeferrals.DeferredScenarioIds)
            {
                if (!declared.ContainsKey(entry.Key))
                {
                    problems.Add(entry.Key + ": không còn là hằng của LiveOpsHubCaptureScenarioIds");
                }
                if (string.IsNullOrWhiteSpace(entry.Value))
                {
                    problems.Add(entry.Key + ": thiếu mảnh câu CHANGELOG");
                }
            }

            foreach (KeyValuePair<string, string> entry in LiveOpsHubCaptureDeferrals.ScenarioIdsOutsideTheConstantTable)
            {
                if (string.IsNullOrWhiteSpace(entry.Value))
                {
                    problems.Add(entry.Key + ": id ngoài bảng hằng mà không nói lý do");
                }
            }

            Assert.IsEmpty(problems, "Sổ LiveOpsHubCaptureDeferrals đã mục nát:\n - " + string.Join("\n - ", problems.ToArray()));
        }

        /// <summary>
        /// Mỗi id hoãn phải có một dòng THẬT trong mục "Chưa có ở bản này" của CHANGELOG — đọc file, không tin lời khai trong
        /// mã. Đây là điều kiện nghiệm thu "mục P1-lùi chưa làm có trong CHANGELOG" của 10.3, và cũng là cái chặn kiểu cắt
        /// tính năng âm thầm mà PD-28 đặt ra cho nhánh tạm.
        /// </summary>
        [Test]
        public void EveryDeferredEntry_HasARealLineInTheChangelogDeferredSection()
        {
            if (LiveOpsHubCaptureDeferrals.DeferredScenarioIds.Count == 0)
            {
                Assert.Pass("không id nào bị hoãn ở bản này — không có dòng CHANGELOG nào phải kiểm");
            }

            string deferredSection = ReadChangelogDeferredSection();

            List<string> problems = new List<string>();
            foreach (KeyValuePair<string, string> entry in LiveOpsHubCaptureDeferrals.DeferredScenarioIds)
            {
                if (deferredSection.IndexOf(entry.Value, StringComparison.Ordinal) < 0)
                {
                    problems.Add(entry.Key + ": mục \"" + ChangelogDeferredSectionHeading + "\" của CHANGELOG không có câu \""
                        + entry.Value + "\"");
                }
            }

            Assert.IsEmpty(problems, "Id hoãn mà CHANGELOG không nói:\n - " + string.Join("\n - ", problems.ToArray()));
        }

        // ------------------------------------------------------------------------------- hỗ trợ

        /// <summary>
        /// Mọi hằng chuỗi của lớp <c>partial</c> <see cref="LiveOpsHubCaptureScenarioIds"/>, kể cả hằng thêm ở file
        /// <c>…Ids.Fix&lt;Wn&gt;k.cs</c> của gói sửa: đọc bằng reflection nên không ai phải nhớ cập nhật một danh sách thứ hai.
        /// </summary>
        private static IReadOnlyDictionary<string, string> DeclaredScenarioIdsByConstantName()
        {
            Dictionary<string, string> byName = new Dictionary<string, string>(StringComparer.Ordinal);
            FieldInfo[] fields = typeof(LiveOpsHubCaptureScenarioIds)
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (FieldInfo field in fields)
            {
                if (!field.IsLiteral || field.FieldType != typeof(string)) continue;
                byName.Add(field.Name, (string)field.GetRawConstantValue());
            }

            Assert.Greater(byName.Count, 0, "không đọc được hằng id nào — bảng hằng của W0 phải là nguồn chuẩn của ma trận 9.5");
            return byName;
        }

        private static IReadOnlyDictionary<string, int> RegisteredScenarioIdCounts()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (LiveOpsHubCaptureScenario scenario in LiveOpsHubCaptureScenarios.All())
            {
                counts.TryGetValue(scenario.Id, out int previous);
                counts[scenario.Id] = previous + 1;
            }
            return counts;
        }

        /// <summary>
        /// Phần văn bản nằm giữa tiêu đề "Chưa có ở bản này" và tiêu đề kế tiếp. Gốc package hỏi Package Manager qua assembly
        /// của chính file này (không suy từ thư mục hiện hành) vì lượt 2022.3 chạy trên project tạm trỏ <c>file:</c> vào
        /// worktree — đọc nhầm bản khác thì câu "xanh ở cả hai bản" là giả.
        /// </summary>
        private static string ReadChangelogDeferredSection()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(CaptureCoverageTests).Assembly);
            Assert.IsNotNull(package, "không hỏi được gốc package từ assembly test");

            string changelogPath = Path.Combine(Path.GetFullPath(package.resolvedPath), ChangelogFileName);
            Assert.IsTrue(File.Exists(changelogPath), "không đọc được " + ChangelogFileName + " (" + changelogPath + ")");

            string text = File.ReadAllText(changelogPath).Replace("\r\n", "\n").Replace("\r", "\n");
            int start = text.IndexOf(ChangelogDeferredSectionHeading, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, "CHANGELOG thiếu mục \"" + ChangelogDeferredSectionHeading + "\"");

            start += ChangelogDeferredSectionHeading.Length;
            int end = text.IndexOf("\n#", start, StringComparison.Ordinal);
            return end < 0 ? text.Substring(start) : text.Substring(start, end - start);
        }
    }
}
