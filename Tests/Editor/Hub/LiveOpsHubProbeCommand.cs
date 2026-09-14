using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Probe CLI của hub (9.3) — cổng nhanh sau mỗi gói, vài giây sau khi Unity mở, không thay test:
    /// <code>
    /// Unity -batchmode -projectPath P -executeMethod DreamTech.LiveOps.Editor.Tests.LiveOpsHubProbeCommand.RunFromCommandLine -logFile L
    /// </code>
    /// (1) script biên dịch lỗi → thoát 2; (2) mọi UXML/USS khai ở <see cref="LiveOpsHubPaths"/> đã có trên đĩa phải nạp được, bố cục của
    /// khung bắt buộc có; (3) mọi tên <see cref="LiveOpsHubPaths.RequiredShellElementNames"/> có trong cây clone và mọi
    /// <see cref="IHubSection.RequiredElementNames"/> có trong view của từng màn; (4) hợp đồng màn; (5) dựng cả 6 màn vào host tách rời,
    /// <c>OnShown</c>, <c>GetHealth</c> hai lần cùng trạng thái, NotMeasured có lý do, bắt exception từng màn; (5b) khứ hồi trạng thái
    /// view; (6) mọi icon trong <see cref="LiveOpsHubIcons.AllDesignNames"/> nạp được. Lỗi &gt; 0 → thoát 1; sạch → log
    /// <c>LIVEOPS HUB PROBE OK</c> + thoát 0.
    /// <para>
    /// Ngữ cảnh (9.3: tối thiểu không asset + mẫu). Ở W2 chưa có phiên lịch: ngữ cảnh "không asset" = registry thật (6 màn giữ chỗ,
    /// không đọc dữ liệu) dựng ở (5); ngữ cảnh "mẫu" = health dạng Hình 4 gắn vào đúng id của registry thật rồi dựng
    /// <see cref="LiveOpsHubRailModel"/> — kiểm khung với dấu/badge/ô chặn của mẫu, KHÔNG chứng minh màn thật dựng với dữ liệu mẫu.
    /// Dữ liệu thật của từng ngữ cảnh do G-SESSION nối (đề xuất cùng quyền ghi: plan/w2/contract-changes-G-SHELL.md CC-SHELL-1).
    /// </para>
    /// </summary>
    [Category(LiveOpsHubTestCategories.UI)]
    public static class LiveOpsHubProbeCommand
    {
        public const int SuccessExitCode = 0;
        public const int ProblemsExitCode = 1;
        public const int CompilationFailedExitCode = 2;
        internal const string OkMarker = "LIVEOPS HUB PROBE OK";
        internal const string LogPrefix = "LIVEOPS HUB PROBE: ";
        private const int MaximumSubtitleLength = 60;

        /// <summary>P1 vẽ 4 tầng: CHẠY chưa có màn nên không vẽ (PD-1).</summary>
        private const int DrawnStageCount = 4;

        public static void RunFromCommandLine()
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                Debug.LogError(LogPrefix + "script biên dịch lỗi — probe chạy trên assembly cũ sẽ báo xanh giả");
                EditorApplication.Exit(CompilationFailedExitCode);
                return;
            }

            List<string> notes = new List<string>();
            List<string> problems;
            try
            {
                problems = RunChecks(notes);
            }
            catch (Exception exception)
            {
                problems = new List<string> { "probe ném " + exception.GetType().Name + ": " + exception.Message + "\n" + exception.StackTrace };
            }

            foreach (string note in notes) Debug.Log(LogPrefix + note);
            if (problems.Count > 0)
            {
                foreach (string problem in problems) Debug.LogError(LogPrefix + "LỖI " + problem);
                Debug.LogError(LogPrefix + "FAILED (" + problems.Count + " lỗi)");
                EditorApplication.Exit(ProblemsExitCode);
                return;
            }
            Debug.Log(OkMarker);
            EditorApplication.Exit(SuccessExitCode);
        }

        /// <summary>Mọi bước kiểm; trả danh sách lỗi (rỗng = đạt), ghi ghi chú (sheet chưa có của gói sau…) vào <paramref name="notes"/>.</summary>
        internal static List<string> RunChecks(List<string> notes)
        {
            List<string> problems = new List<string>();
            CheckLayoutFiles(problems, notes);
            CheckShellElements(problems);
            List<IHubSection> sections = LiveOpsHubSections.Create();
            CheckSectionContract(sections, problems);
            CheckSectionsBuild(sections, problems, notes);
            CheckDesignSampleRail(problems, notes);
            CheckIcons(problems);
            return problems;
        }

        // (2)
        private static void CheckLayoutFiles(List<string> problems, List<string> notes)
        {
            HashSet<string> required = new HashSet<string>(StringComparer.Ordinal) { LiveOpsHubPaths.ShellUxml };
            foreach (LiveOpsHubPaths.ShellStyleSheet sheet in LiveOpsHubPaths.ShellStyleSheetLoadOrder)
            {
                if (sheet.IsRequired) required.Add(sheet.Path);
            }

            foreach (FieldInfo field in typeof(LiveOpsHubPaths).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!field.IsLiteral || field.FieldType != typeof(string)) continue;
                string path = (string)field.GetRawConstantValue();
                bool isUxml = path.EndsWith(".uxml", StringComparison.Ordinal);
                bool isUss = path.EndsWith(".uss", StringComparison.Ordinal);
                if (!isUxml && !isUss) continue;

                // Đường dẫn ảo "Packages/…" chỉ trỏ đúng thư mục trong dev project; project tạm 2022.3 cài package bằng file: nên
                // phải đổi sang đường dẫn vật lý mới biết file có trên đĩa.
                bool existsOnDisk = File.Exists(FileUtil.GetPhysicalPath(path));
                if (!existsOnDisk)
                {
                    if (required.Contains(path)) problems.Add("thiếu tài nguyên của khung: " + path);
                    else notes.Add("chưa có (gói sau tạo): " + path);
                    continue;
                }
                UnityEngine.Object loaded = isUxml
                    ? (UnityEngine.Object)AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path)
                    : AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (loaded == null) problems.Add("có trên đĩa nhưng Unity không nạp được (import lỗi?): " + path);
            }
        }

        // (3) khung
        private static void CheckShellElements(List<string> problems)
        {
            VisualTreeAsset layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LiveOpsHubPaths.ShellUxml);
            if (layout == null) return;
            VisualElement clone = new VisualElement();
            layout.CloneTree(clone);
            foreach (string elementName in LiveOpsHubPaths.RequiredShellElementNames)
            {
                if (clone.Q(elementName) == null) problems.Add(LiveOpsHubPaths.ShellUxml + " thiếu element '" + elementName + "'");
            }
        }

        // (4)
        internal static void CheckSectionContract(IReadOnlyList<IHubSection> sections, List<string> problems)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<PipelineStage> knownStages = new HashSet<PipelineStage>(PipelineStages.All);
            foreach (IHubSection section in sections)
            {
                if (string.IsNullOrEmpty(section.Id) || !ids.Add(section.Id)) problems.Add("id màn rỗng hoặc trùng: '" + section.Id + "'");
                if (string.IsNullOrWhiteSpace(section.Title)) problems.Add(section.Id + ": thiếu tiêu đề");
                if (string.IsNullOrWhiteSpace(section.Subtitle) || section.Subtitle.Length >= MaximumSubtitleLength)
                {
                    problems.Add(section.Id + ": subtitle phải có và dưới 60 ký tự ('" + section.Subtitle + "')");
                }
                if (!knownStages.Contains(section.Stage)) problems.Add(section.Id + ": tầng " + section.Stage + " không có trong PipelineStages.All");
                if (section.RequiredElementNames == null) problems.Add(section.Id + ": RequiredElementNames null");
            }
        }

        // (5) + (5b) + (3) màn
        private static void CheckSectionsBuild(IReadOnlyList<IHubSection> sections, List<string> problems, List<string> notes)
        {
            DetachedHost host = new DetachedHost(sections);
            foreach (IHubSection section in sections)
            {
                if (section is IHubHostAware hostAware) hostAware.Bind(host);
            }
            foreach (IHubSection section in sections)
            {
                try
                {
                    if (section is IHubSectionActions actions) actions.PopulateHeaderActions(new VisualElement());
                    VisualElement view = section.CreateView();
                    if (view == null)
                    {
                        problems.Add(section.Id + ": CreateView trả null");
                        continue;
                    }
                    foreach (string elementName in section.RequiredElementNames)
                    {
                        if (view.Q(elementName) == null) problems.Add(section.Id + ": view thiếu element '" + elementName + "'");
                    }
                    section.OnShown();

                    SectionHealth first = section.GetHealth();
                    SectionHealth second = section.GetHealth();
                    if (first.State != second.State) problems.Add(section.Id + ": GetHealth hai lần khác trạng thái (" + first.State + " / " + second.State + ")");
                    if (first.State != HealthState.Ok && string.IsNullOrEmpty(first.Reason)) problems.Add(section.Id + ": " + first.State + " không có lý do");

                    if (section is IHubSectionViewState viewState)
                    {
                        viewState.RestoreViewState(viewState.CaptureViewState());
                        viewState.RestoreViewState(string.Empty);
                    }
                    notes.Add(string.Format(CultureInfo.InvariantCulture, "màn {0}: dựng OK, health {1}", section.Id, first.State));
                }
                catch (Exception exception)
                {
                    problems.Add(section.Id + ": ném " + exception.GetType().Name + ": " + exception.Message);
                }
            }
        }

        // (5) ngữ cảnh mẫu: health dạng Hình 4 theo id trên registry thật → rail model đủ 4 tầng, cổng chặn đầu tiên là KIỂM, có ô chặn.
        private static void CheckDesignSampleRail(List<string> problems, List<string> notes)
        {
            List<IHubSection> sections = LiveOpsHubSections.Create();
            Dictionary<string, SectionHealth> sampleHealthById = new Dictionary<string, SectionHealth>(StringComparer.Ordinal);
            foreach (FakeHubSection sample in FakeHubSection.CreateDesignSampleShaped()) sampleHealthById[sample.Id] = sample.Health;

            List<SectionHealth> healths = new List<SectionHealth>(sections.Count);
            foreach (IHubSection section in sections)
            {
                if (!sampleHealthById.TryGetValue(section.Id, out SectionHealth health))
                {
                    problems.Add("ngữ cảnh mẫu: registry có màn '" + section.Id + "' không có trong mẫu Hình 4");
                    health = SectionHealth.Ok();
                }
                healths.Add(health);
            }

            try
            {
                LiveOpsHubRailModel model = LiveOpsHubRailModel.Build(sections, healths, null, false);
                if (model.Stages.Count != DrawnStageCount) problems.Add("ngữ cảnh mẫu: rail có " + model.Stages.Count + " tầng, P1 cần " + DrawnStageCount);
                if (model.FirstBlockedGateIndex < 0 || model.Stages[model.FirstBlockedGateIndex].Stage != PipelineStage.Check)
                {
                    problems.Add("ngữ cảnh mẫu: cổng chặn đầu tiên phải là KIỂM");
                }
                if (!model.HasBlocker) problems.Add("ngữ cảnh mẫu: KIỂM Blocked mà không có ô chặn");
                notes.Add("ngữ cảnh mẫu (health Hình 4 trên registry thật): rail model OK, " + model.BlockerTitle);
            }
            catch (Exception exception)
            {
                problems.Add("ngữ cảnh mẫu: rail model ném " + exception.GetType().Name + ": " + exception.Message);
            }
        }

        // (6)
        private static void CheckIcons(List<string> problems)
        {
            foreach (string iconName in LiveOpsHubIcons.AllDesignNames)
            {
                if (LiveOpsHubIcons.Get(iconName) == null) problems.Add("icon không nạp được: " + iconName);
            }
        }

        /// <summary>Host không có cửa sổ: probe dựng màn tách rời để lỗi của khung không che lỗi của màn (và ngược lại).</summary>
        private sealed class DetachedHost : IHubHost
        {
            private readonly Dictionary<string, string> _viewStates = new Dictionary<string, string>(StringComparer.Ordinal);

            public DetachedHost(IReadOnlyList<IHubSection> sections)
            {
                Sections = sections;
            }

            public IReadOnlyList<IHubSection> Sections { get; }

            public void Navigate(string sectionId)
            {
            }

            public string GetSectionViewState(string sectionId)
            {
                return sectionId != null && _viewStates.TryGetValue(sectionId, out string viewState) ? viewState : string.Empty;
            }

            public void SetSectionViewState(string sectionId, string viewStateJson)
            {
                if (sectionId != null) _viewStates[sectionId] = viewStateJson ?? string.Empty;
            }
        }
    }
}
