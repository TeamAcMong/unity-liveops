using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DreamTech.LiveOps.Unity.Tests
{
    /// <summary>
    /// Bộ gác tài liệu. Mọi khối `csharp của README được chép NGUYÊN VĂN vào chính file này, nên README sai tên API là đỏ
    /// ở cả hai bản Unity thay vì lặng lẽ lạc hậu; đoạn "chờ remote có hạn giờ" (README mục 4.4) còn được CHẠY thật vì nó
    /// là lời khuyên về hành vi, không chỉ về cú pháp. Ngoài ra: mọi id luật Kiểm lịch phải có anchor trong README (id nằm
    /// trong ghi chú "Bỏ qua" của người dùng nên phải tra được), và hai bản CHANGELOG phải giống hệt nhau.
    /// </summary>
    [TestFixture]
    public sealed class ReadmeSnippetCompileTests
    {
        // Đường dẫn TỪ GỐC PACKAGE, không từ thư mục hiện hành: lượt 2022.3 chạy trên project tạm nằm ngoài repo và
        // unity-slot.sh không đổi thư mục, nên "Packages/com.dreamtech.liveops/..." tính theo cwd sẽ trỏ đi đâu là tuỳ chỗ
        // phát lệnh. Gốc thật hỏi Package Manager ở PackageDirectory().
        private const string ReadmeRelativePath = "README.md";
        private const string SelfSourceRelativePath = "Tests/Editor/Unity/ReadmeSnippetCompileTests.cs";
        private const string ChangelogFileName = "CHANGELOG.md";

        // Dấu mở/đóng vùng chép nguyên văn. So sánh theo dòng đã Trim nên dòng khai báo hằng này không tự khớp chính nó.
        private const string SnippetBeginMarker = "// readme-snippet-begin";
        private const string SnippetEndMarker = "// readme-snippet-end";
        private const string CSharpFenceOpen = "```csharp";
        private const string FenceClose = "```";

        // Hạn giờ chờ đoạn async của README xong trong test. Dài hơn nhiều so với hạn giờ của chính đoạn code để một máy
        // chậm không làm test chớp đỏ; hết hạn này là đoạn code thật sự treo.
        private const float TaskWaitSeconds = 10f;
        private const int SnippetWaitBudgetMilliseconds = 50;

        private const string RemoteJsonFormat2 =
            "{\"version\":2,\"recurring\":[],\"events\":[{\"id\":\"hunt-0914\",\"type\":\"treasure-hunt\"," +
            "\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"2026-09-17T00:00:00Z\",\"configKey\":\"hunt_default\"}]}";

        private readonly List<LiveEventCalendarAsset> _createdAssets = new List<LiveEventCalendarAsset>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < _createdAssets.Count; index++)
            {
                if (_createdAssets[index] != null) UnityEngine.Object.DestroyImmediate(_createdAssets[index]);
            }
            _createdAssets.Clear();
        }

        // ------------------------------------------------------------------------------- đoạn code chép từ README

        /// <summary>
        /// Khối `csharp thứ NHẤT của README (mục 2, "Bước 4 — composition root"). Không chạy: nó ghi PlayerPrefs của máy
        /// và dựng runner sống qua scene. Giá trị của nó là COMPILE — tên API sai trong README thành lỗi biên dịch.
        /// </summary>
        private static LiveOpsSystem ReadmeCompositionRoot(string cdnUrl, string remoteConfigJson,
            LiveEventCalendarAsset mainCalendarAsset)
        {
            // readme-snippet-begin
            var store = new PlayerPrefsLiveOpsTextStore();                                   // ← hoặc save system của game
            var serverClock = new SyncedLiveOpsClock(new HttpDateHeaderServerTimeSource(cdnUrl), store);
            var clock = new OffsetLiveOpsClock(serverClock);                                 // tua giờ bằng cheat

            // Remote config trống → lịch trong asset; đọc được → lịch remote. Một nhánh cho cả hai nguồn.
            LiveEventCalendarParseResult calendarResult =
                JsonLiveEventCalendarParser.ParseOrDefault(remoteConfigJson, mainCalendarAsset);

            LiveOpsSystem liveOps = new LiveOpsSystemBuilder("main")
                .WithClock(clock)
                .WithCalendar(calendarResult.CombinedCalendar)                               // KHÔNG dùng .Calendar: nó chỉ có đợt cố định
                .WithTextStore(store)
                .WithRewardGranter(new DelegateLiveOpsRewardGranter(GrantToInventory))
                .WithSettings(new LiveOpsSettings(finalizeRequiresTrustedClock: true))
                .WithEventTypesFrom(mainCalendarAsset, CreateCompletionRuleFor, CreateEligibilityFor)
                .Build();
            LiveOpsSystemRegistry.Register(liveOps);
            LiveOpsUnityRunner.Create(liveOps, serverClock);                                 // Refresh định kỳ + vòng đời app

            foreach (string problem in calendarResult.Problems) Debug.LogWarning(problem);   // mục lịch bị bỏ và vì sao
            // readme-snippet-end
            return liveOps;
        }

        /// <summary>Khối `csharp thứ HAI của README (mục 4.4, "Remote về muộn"). Được CHẠY ở hai test dưới.</summary>
        // readme-snippet-begin
        static async Task<LiveEventCalendarParseResult> LoadCalendarAsync(
            Func<Task<string>> fetchRemoteJsonAsync, LiveEventCalendarAsset mainCalendarAsset, TimeSpan waitBudget)
        {
            Task<string> fetch = fetchRemoteJsonAsync();
            Task finished = await Task.WhenAny(fetch, Task.Delay(waitBudget));
            bool arrivedInTime = ReferenceEquals(finished, fetch) && fetch.Status == TaskStatus.RanToCompletion;
            string remoteJson = arrivedInTime ? fetch.Result : string.Empty;   // hết hạn / lỗi mạng → "" = dùng lịch trong asset
            return JsonLiveEventCalendarParser.ParseOrDefault(remoteJson, mainCalendarAsset);
        }
        // readme-snippet-end

        // Chỗ cắm của game trong khối thứ nhất. Thân rỗng vì khối đó chỉ cần compile.
        private static bool GrantToInventory(string grantId, LiveOpsRewardBundle bundle) => true;

        private static ILiveEventCompletionRule CreateCompletionRuleFor(string eventType) => null;

        private static ILiveEventEligibility CreateEligibilityFor(string eventType) => null;

        // ------------------------------------------------------------------------------- chép nguyên văn

        [Test]
        public void Readme_CSharpSnippets_CopiedVerbatimIntoThisTest()
        {
            IReadOnlyList<string> readmeSnippets = ReadCSharpBlocks(ReadAllTextAtPackagePath(ReadmeRelativePath));
            IReadOnlyList<string> testSnippets = ReadMarkedRegions(ReadAllTextAtPackagePath(SelfSourceRelativePath));

            Assert.AreEqual(readmeSnippets.Count, testSnippets.Count,
                "README có " + readmeSnippets.Count.ToString(CultureInfo.InvariantCulture) + " khối csharp nhưng file test có "
                + testSnippets.Count.ToString(CultureInfo.InvariantCulture) + " vùng chép — thêm khối vào README thì phải thêm"
                + " vùng " + SnippetBeginMarker + " tương ứng (cùng thứ tự) vào " + SelfSourceRelativePath);
            Assert.Greater(readmeSnippets.Count, 0, "README phải có ít nhất một khối csharp — cách lắp vào game là thứ người đọc cần nhất");

            for (int index = 0; index < readmeSnippets.Count; index++)
            {
                Assert.AreEqual(readmeSnippets[index], testSnippets[index],
                    "khối csharp thứ " + (index + 1).ToString(CultureInfo.InvariantCulture)
                    + " của README khác bản chép trong test — sửa một bên thì sửa cả hai, đúng từng ký tự");
            }
        }

        [Test]
        public void ReadmeCompositionRoot_SnippetIsCompiledIntoThisAssembly()
        {
            // Khối thứ nhất không chạy được trong test, nên chứng cứ duy nhất là nó đã biên dịch: hỏi lại bằng reflection
            // để trình biên dịch không coi đây là code chết và để test nói rõ vì sao vùng chép đó tồn tại.
            MethodInfo snippet = typeof(ReadmeSnippetCompileTests).GetMethod(nameof(ReadmeCompositionRoot),
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(snippet, "vùng chép của khối composition root đã biến mất khỏi assembly test");
            Assert.AreEqual(typeof(LiveOpsSystem), snippet.ReturnType,
                "composition root trong README phải dựng ra LiveOpsSystem — đổi kiểu trả về là đổi cách lắp của game");
        }

        // ------------------------------------------------------------------------------- chạy đoạn "remote về muộn"

        [UnityTest]
        public IEnumerator ReadmeSnippet_RemoteArrivesInTime_UsesRemoteCalendar()
        {
            LiveEventCalendarAsset asset = CreateDesignSampleAsset();
            Task<LiveEventCalendarParseResult> running = LoadCalendarAsync(
                () => Task.FromResult(RemoteJsonFormat2), asset, TimeSpan.FromMilliseconds(SnippetWaitBudgetMilliseconds));

            yield return WaitForTask(running);

            LiveEventCalendarParseResult result = running.Result;
            Assert.IsFalse(result.CameFromDefaultCalendar, "remote về kịp thì phải dùng lịch remote, không rơi về asset");
            Assert.AreEqual(2, result.FormatVersion, "bản remote của test là định dạng 2");
        }

        [UnityTest]
        public IEnumerator ReadmeSnippet_RemoteTooLate_FallsBackToAssetCalendar()
        {
            LiveEventCalendarAsset asset = CreateDesignSampleAsset();
            var neverCompletes = new TaskCompletionSource<string>();
            Task<LiveEventCalendarParseResult> running = LoadCalendarAsync(
                () => neverCompletes.Task, asset, TimeSpan.FromMilliseconds(SnippetWaitBudgetMilliseconds));

            yield return WaitForTask(running);

            LiveEventCalendarParseResult result = running.Result;
            Assert.IsTrue(result.CameFromDefaultCalendar,
                "hết hạn chờ thì phải dùng lịch trong asset — đây chính là lời khuyên README mục 4.4 đưa ra");
            Assert.AreEqual(LiveEventCalendarParseResult.AssetFormatVersion, result.FormatVersion,
                "kết quả từ asset luôn mang định dạng 2 vì asset mang được luật lặp");

            neverCompletes.TrySetResult(string.Empty);
        }

        // ------------------------------------------------------------------------------- anchor luật và CHANGELOG

        [Test]
        public void Readme_HasAnchorForEveryCalendarRuleId()
        {
            string readme = ReadAllTextAtPackagePath(ReadmeRelativePath);
            IReadOnlyList<string> ruleIds = LiveEventCalendarRuleIds.All;

            for (int index = 0; index < ruleIds.Count; index++)
            {
                string anchor = "id=\"" + ruleIds[index] + "\"";
                Assert.IsTrue(readme.IndexOf(anchor, StringComparison.Ordinal) >= 0,
                    "README thiếu anchor " + anchor + " — id luật nằm trong ghi chú \"Bỏ qua\" lưu ở asset của người dùng,"
                    + " nên mỗi id phải tra được trong tài liệu");
            }
        }

        [Test]
        public void Readme_NamesTheTwoCheckedUnityVersionsAndTheUncheckedRange()
        {
            string readme = ReadAllTextAtPackagePath(ReadmeRelativePath);

            // PD-34: chỉ hứa đúng hai bản đã chạy thật, và nói thẳng khoảng chưa kiểm.
            StringAssert.Contains("2022.3.62f2", readme);
            StringAssert.Contains("6000.6.0f1", readme);
            StringAssert.Contains("chưa kiểm", readme);
        }

        [Test]
        public void Changelog_PackageCopyAndRepositoryCopy_AreIdentical()
        {
            string packageChangelogPath = Path.Combine(PackageDirectory(), ChangelogFileName);
            string repositoryChangelogPath = Path.GetFullPath(Path.Combine(PackageDirectory(), "..", "..", ChangelogFileName));

            Assert.IsTrue(File.Exists(packageChangelogPath), "package phải có " + ChangelogFileName);
            if (!File.Exists(repositoryChangelogPath))
            {
                Assert.Pass("package không nằm trong dev repo (không có " + ChangelogFileName + " ở gốc) — chỉ kiểm bản của package");
            }

            Assert.AreEqual(NormalizeNewLines(File.ReadAllText(repositoryChangelogPath)),
                NormalizeNewLines(File.ReadAllText(packageChangelogPath)),
                "hai bản CHANGELOG đã lệch — bản ở gốc repo là bản sao của bản trong package, sửa phải sửa cùng lúc");
        }

        // ------------------------------------------------------------------------------- hỗ trợ

        private LiveEventCalendarAsset CreateDesignSampleAsset()
        {
            LiveEventCalendarAsset asset = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
            _createdAssets.Add(asset);
            asset.ApplyDocument(LiveOpsDesignSample.Document);
            return asset;
        }

        /// <summary>Chờ Task xong bằng nhịp frame của Editor: chặn main thread sẽ khoá luôn chỗ chạy tiếp của await.</summary>
        private static IEnumerator WaitForTask(Task task)
        {
            float deadline = Time.realtimeSinceStartup + TaskWaitSeconds;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;

            Assert.IsTrue(task.IsCompleted, "đoạn async của README không xong trong "
                + TaskWaitSeconds.ToString(CultureInfo.InvariantCulture) + " giây");
            if (task.IsFaulted) throw task.Exception;
        }

        /// <summary>
        /// Gốc package ĐANG ĐƯỢC THỬ, hỏi Package Manager qua assembly của chính file này. Không suy từ thư mục hiện hành:
        /// Unity chạy batchmode với cwd của chỗ phát lệnh, nên ở lượt 2022.3 (project tạm trỏ <c>file:</c> vào worktree)
        /// một đường dẫn tính theo cwd có thể đọc nhầm bản khác — câu "xanh ở cả hai bản" chỉ thật khi hai lượt đọc đúng
        /// bộ file mà chúng vừa biên dịch.
        /// </summary>
        private static string PackageDirectory()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ReadmeSnippetCompileTests).Assembly);
            Assert.IsNotNull(package,
                "không hỏi được gốc package từ assembly test — bộ test này phải chạy như một phần của com.dreamtech.liveops");
            return Path.GetFullPath(package.resolvedPath);
        }

        private static string ReadAllTextAtPackagePath(string relativePath)
        {
            string fullPath = Path.Combine(PackageDirectory(), relativePath);
            Assert.IsTrue(File.Exists(fullPath), "không đọc được " + relativePath + " (đường dẫn thật: " + fullPath + ")");
            return NormalizeNewLines(File.ReadAllText(fullPath));
        }

        private static string NormalizeNewLines(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        /// <summary>Mọi khối ```csharp của Markdown, theo thứ tự xuất hiện, đã bỏ thụt đầu dòng chung.</summary>
        private static IReadOnlyList<string> ReadCSharpBlocks(string markdown)
        {
            var blocks = new List<string>();
            string[] lines = markdown.Split('\n');
            var current = new List<string>();
            bool inside = false;

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                if (!inside)
                {
                    if (line.Trim().Equals(CSharpFenceOpen, StringComparison.Ordinal)) inside = true;
                    continue;
                }
                if (line.Trim().Equals(FenceClose, StringComparison.Ordinal))
                {
                    blocks.Add(Dedent(current));
                    current.Clear();
                    inside = false;
                    continue;
                }
                current.Add(line);
            }

            Assert.IsFalse(inside, "README có khối ```csharp không đóng");
            return blocks;
        }

        /// <summary>Mọi vùng giữa hai dấu chép, theo thứ tự xuất hiện, đã bỏ thụt đầu dòng chung.</summary>
        private static IReadOnlyList<string> ReadMarkedRegions(string source)
        {
            var regions = new List<string>();
            string[] lines = source.Split('\n');
            var current = new List<string>();
            bool inside = false;

            for (int index = 0; index < lines.Length; index++)
            {
                string trimmed = lines[index].Trim();
                if (!inside)
                {
                    if (trimmed.StartsWith(SnippetBeginMarker, StringComparison.Ordinal)) inside = true;
                    continue;
                }
                if (trimmed.StartsWith(SnippetEndMarker, StringComparison.Ordinal))
                {
                    regions.Add(Dedent(current));
                    current.Clear();
                    inside = false;
                    continue;
                }
                current.Add(lines[index]);
            }

            Assert.IsFalse(inside, "file test có vùng " + SnippetBeginMarker + " không đóng");
            return regions;
        }

        /// <summary>
        /// Bỏ phần thụt chung và khoảng trắng cuối dòng. Cần vì trong README đoạn code nằm ở cột 0, còn trong test nó nằm
        /// trong thân lớp hoặc thân hàm — khác thụt đầu dòng không phải là khác nội dung.
        /// </summary>
        private static string Dedent(IReadOnlyList<string> lines)
        {
            int commonIndent = int.MaxValue;
            for (int index = 0; index < lines.Count; index++)
            {
                string line = lines[index];
                if (line.Trim().Length == 0) continue;
                int indent = 0;
                while (indent < line.Length && line[indent] == ' ') indent++;
                if (indent < commonIndent) commonIndent = indent;
            }
            if (commonIndent == int.MaxValue) commonIndent = 0;

            var builder = new StringBuilder();
            for (int index = 0; index < lines.Count; index++)
            {
                string line = lines[index].TrimEnd();
                if (line.Length > commonIndent) builder.Append(line.Substring(commonIndent));
                else if (line.Trim().Length > 0) builder.Append(line.TrimStart());
                if (index < lines.Count - 1) builder.Append('\n');
            }
            return builder.ToString().Trim('\n');
        }
    }
}
