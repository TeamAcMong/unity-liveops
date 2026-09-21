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
    /// Ngữ cảnh (9.3; V-21 CC-SHELL-1, G-SESSION): bốn phiên THẬT dựng bằng <see cref="LiveOpsHubTestServices.ForScenario"/> — không asset,
    /// asset rỗng, mẫu thiết kế (đã kiểm), kiểm cũ. Mỗi ngữ cảnh: registry nhận services của phiên đó, dựng cả 6 màn vào host tách rời
    /// mang services, rồi dựng <see cref="LiveOpsHubRailModel"/> từ health định tuyến của phiên (<see cref="LiveOpsHubFindingRouting.ForSection"/>)
    /// + bộ tổng hợp của lần kiểm — kiểm dấu/badge/ô chặn bằng dữ liệu phiên, không bằng health dán tay.
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
            try
            {
                foreach (string scenario in LiveOpsHubTestServices.ContextScenarios)
                {
                    LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(scenario);
                    List<IHubSection> sections = LiveOpsHubSections.Create(services);
                    CheckSectionContract(sections, problems);
                    CheckSectionsBuild(scenario, sections, services, problems, notes);
                    CheckSessionRail(scenario, sections, services, problems, notes);
                }
            }
            finally
            {
                LiveOpsHubTestServices.ReleaseAll();
            }
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
        private static void CheckSectionsBuild(string scenario, IReadOnlyList<IHubSection> sections, LiveOpsHubServices services, List<string> problems,
            List<string> notes)
        {
            DetachedHost host = new DetachedHost(sections, services);
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
                        problems.Add(scenario + " · " + section.Id + ": CreateView trả null");
                        continue;
                    }
                    foreach (string elementName in section.RequiredElementNames)
                    {
                        if (view.Q(elementName) == null) problems.Add(scenario + " · " + section.Id + ": view thiếu element '" + elementName + "'");
                    }
                    section.OnShown();

                    SectionHealth first = section.GetHealth();
                    SectionHealth second = section.GetHealth();
                    if (first.State != second.State) problems.Add(scenario + " · " + section.Id + ": GetHealth hai lần khác trạng thái (" + first.State + " / " + second.State + ")");
                    if (first.State != HealthState.Ok && string.IsNullOrEmpty(first.Reason)) problems.Add(scenario + " · " + section.Id + ": " + first.State + " không có lý do");

                    if (section is IHubSectionViewState viewState)
                    {
                        viewState.RestoreViewState(viewState.CaptureViewState());
                        viewState.RestoreViewState(string.Empty);
                    }
                    notes.Add(string.Format(CultureInfo.InvariantCulture, "{0} · màn {1}: dựng OK, health {2}", scenario, section.Id, first.State));
                }
                catch (Exception exception)
                {
                    problems.Add(scenario + " · " + section.Id + ": ném " + exception.GetType().Name + ": " + exception.Message);
                }
            }
        }

        // (5) rail của phiên: health định tuyến từ dữ liệu phiên (thứ màn W4 sẽ trả) + bộ tổng hợp của lần kiểm → đủ 4 tầng, ô chặn đúng ngữ cảnh.
        private static void CheckSessionRail(string scenario, IReadOnlyList<IHubSection> sections, LiveOpsHubServices services, List<string> problems,
            List<string> notes)
        {
            try
            {
                List<SectionHealth> healths = new List<SectionHealth>(sections.Count);
                foreach (IHubSection section in sections)
                {
                    SectionHealth first = LiveOpsHubFindingRouting.ForSection(section.Id, services);
                    SectionHealth second = LiveOpsHubFindingRouting.ForSection(section.Id, services);
                    if (first.State != second.State) problems.Add(scenario + " · " + section.Id + ": health phiên hai lần khác trạng thái");
                    if (first.State != HealthState.Ok && string.IsNullOrEmpty(first.Reason)) problems.Add(scenario + " · " + section.Id + ": health phiên " + first.State + " không có lý do");
                    healths.Add(first);
                }
                LiveOpsHubCheckState check = services.Session.Check;
                LiveEventCalendarCheckSummary summary = check.LastReport != null ? check.LastReport.Summary : null;
                LiveOpsHubRailModel model = LiveOpsHubRailModel.Build(sections, healths, summary, check.IsStale);
                if (model.Stages.Count != DrawnStageCount) problems.Add(scenario + ": rail có " + model.Stages.Count + " tầng, P1 cần " + DrawnStageCount);

                bool expectsBlocker = scenario == LiveOpsHubTestServices.DesignSampleScenario || scenario == LiveOpsHubTestServices.StaleCheckScenario;
                if (model.HasBlocker != expectsBlocker)
                {
                    problems.Add(scenario + ": ô chặn " + (model.HasBlocker ? "có" : "không có") + " — mong đợi " + (expectsBlocker ? "có (mẫu còn 2 đợt bị bỏ)" : "không"));
                }
                if (scenario == LiveOpsHubTestServices.DesignSampleScenario
                    && (model.FirstBlockedGateIndex < 0 || model.Stages[model.FirstBlockedGateIndex].Stage != PipelineStage.Check))
                {
                    problems.Add(scenario + ": cổng chặn đầu tiên phải là KIỂM");
                }
                notes.Add(scenario + ": rail của phiên OK" + (model.HasBlocker ? ", " + model.BlockerTitle : string.Empty));
            }
            catch (Exception exception)
            {
                problems.Add(scenario + ": rail của phiên ném " + exception.GetType().Name + ": " + exception.Message);
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

            public DetachedHost(IReadOnlyList<IHubSection> sections, LiveOpsHubServices services)
            {
                Sections = sections;
                Services = services;
            }

            public IReadOnlyList<IHubSection> Sections { get; }

            public LiveOpsHubServices Services { get; }

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
