using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Lệnh ĐO tương phản màu đã hợp thành trong batchmode — đường duy nhất đo được skin SÁNG (phiếu W9-27):
    /// <code>
    /// Unity -batchmode -projectPath P -executeMethod DreamTech.LiveOps.Editor.Tests.LiveOpsHubContrastCommand.MeasureFromCommandLine
    ///   -liveopsContrastSkin dark|light -logFile L
    /// </code>
    /// KHÔNG <c>-nographics</c> (layout NaN nên không đọc được <c>resolvedStyle</c>) và KHÔNG <c>-quit</c> — lệnh tự
    /// <c>EditorApplication.Exit</c>. <c>capture.sh --contrast</c> gọi nó, và chỉ <c>capture.sh</c> mới được đặt
    /// <c>EditorPrefs UserSkin</c> (SP-4) — lệnh này KHÔNG đổi skin, chỉ kiểm skin thật có đúng skin yêu cầu không.
    /// <para>
    /// Kết quả ghi thành file bằng chứng (<see cref="UxContrastEvidence.PathFor"/>) để một test EditMode của cổng đọc lại.
    /// Vì sao không đo thẳng trong lượt cổng: xem <see cref="UxContrastEvidence"/>.
    /// </para>
    /// Mã thoát: 0 đạt (kể cả khi có dòng trượt — trượt hay không là việc của cổng, không phải của lệnh đo) ·
    /// 1 dựng cảnh lỗi · 2 script biên dịch lỗi hoặc tham số sai · 3 skin lệch.
    /// </summary>
    // Cần đồ hoạ thật (không -nographics) như test UI — đánh dấu cùng category để công cụ lọc không xếp nhầm vào nhóm Logic.
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    public static class LiveOpsHubContrastCommand
    {
        internal const string SkinArgument = "-liveopsContrastSkin";
        internal const string LogPrefix = "LIVEOPS CONTRAST: ";

        /// <summary>Cỡ cửa sổ đo: cỡ thiết kế chuẩn của ma trận ảnh — tương phản không đổi theo cỡ, nên một cỡ là đủ.</summary>
        private const int WindowWidth = 1280;

        private const int WindowHeight = 760;

        /// <summary>Tối đa số vòng update chờ layout của một cảnh; hết vòng mà chưa có layout là cảnh hỏng.</summary>
        private const int MaximumLayoutFrames = 60;

        /// <summary>Số vòng chờ thêm sau khi có layout, để lượt dựng lại do chọn đợt kịp xong.</summary>
        private const int SettleFrames = 6;

        /// <summary>Thanh của đợt sinh từ LUẬT — chọn nó thì inspector dựng pane CHỈ ĐỌC (nơi W9-21 sinh ra).</summary>
        private const string RecurringBarKey = "weekly-pass#35";

        private static MeasureRun _activeRun;

        public static void MeasureFromCommandLine()
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                Finish(LiveOpsHubCaptureCommand.UsageExitCode, "script biên dịch lỗi — số đo sẽ là của code cũ, không đo");
                return;
            }

            string skin = ValueAfter(Environment.GetCommandLineArgs(), SkinArgument);
            if (!string.Equals(skin, UxContrastEvidence.DarkSkin, StringComparison.Ordinal)
                && !string.Equals(skin, UxContrastEvidence.LightSkin, StringComparison.Ordinal))
            {
                Finish(LiveOpsHubCaptureCommand.UsageExitCode,
                    SkinArgument + " phải là đúng một skin (dark hoặc light), nhận '" + skin + "' — mỗi skin một lượt Unity (SP-4)");
                return;
            }

            string skinProblem = LiveOpsHubCaptureCommand.DescribeSkinMismatch(skin, EditorGUIUtility.isProSkin);
            if (skinProblem != null)
            {
                Finish(LiveOpsHubCaptureCommand.SkinOrCaptureStepExitCode, skinProblem);
                return;
            }

            Debug.Log(LogPrefix + "bắt đầu đo skin " + skin + ", Unity " + Application.unityVersion);
            _activeRun = new MeasureRun(skin);
            _activeRun.Start();
        }

        private static string ValueAfter(IReadOnlyList<string> arguments, string name)
        {
            for (int index = 0; index < arguments.Count - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.Ordinal)) return arguments[index + 1];
            }
            return null;
        }

        private static void Finish(int exitCode, string message)
        {
            if (exitCode == LiveOpsHubCaptureCommand.SuccessExitCode) Debug.Log(LogPrefix + message);
            else Debug.LogError(LogPrefix + "thoát " + exitCode + ": " + message);
            EditorApplication.Exit(exitCode);
        }

        /// <summary>Một cảnh đo: mở hub ở màn nào, và (tuỳ chọn) chọn sẵn thanh nào để inspector dựng pane tương ứng.</summary>
        private sealed class MeasureScene
        {
            public MeasureScene(string sectionId, string barKey, string place)
            {
                SectionId = sectionId;
                BarKey = barKey;
                Place = place;
            }

            public string SectionId { get; }
            public string BarKey { get; }

            /// <summary>Tên đọc được của cảnh — đi vào câu trượt để người soát biết ngay chỗ nào.</summary>
            public string Place { get; }
        }

        /// <summary>
        /// Máy trạng thái chạy trên <c>EditorApplication.update</c>: mở cảnh → chờ layout → đo → đóng → cảnh kế. Không chặn
        /// luồng chính bằng vòng lặp vì layout chỉ chạy giữa các vòng update.
        /// </summary>
        private sealed class MeasureRun
        {
            private readonly string _skin;
            private readonly List<MeasureScene> _scenes;
            private readonly List<string> _failures = new List<string>();
            private int _sceneIndex = -1;
            private int _measuredCount;
            private int _frames;
            private EditorWindow _window;
            private LiveOpsHubLanguageScope _languageScope;

            public MeasureRun(string skin)
            {
                _skin = skin;
                _scenes = new List<MeasureScene>
                {
                    new MeasureScene(LiveOpsHubSections.Ids.Overview, null, "màn Tổng quan"),
                    new MeasureScene(LiveOpsHubSections.Ids.EventTypes, null, "màn Loại event"),
                    new MeasureScene(LiveOpsHubSections.Ids.Calendar, null, "màn Lịch, chưa chọn đợt"),
                    // Hai nhánh của inspector Lịch — đúng cặp mà ca EditMode của skin đang chạy đi qua (R-03 lượt soát 2):
                    // đợt sinh từ luật dựng pane CHỈ ĐỌC, đợt cố định dựng ô ngày giờ UTC (opacity 0,7 và 0,82).
                    new MeasureScene(LiveOpsHubSections.Ids.Calendar, RecurringBarKey, "màn Lịch, đợt sinh từ luật"),
                    new MeasureScene(LiveOpsHubSections.Ids.Calendar, LiveOpsDesignSample.HuntBonusEntryKey,
                        "màn Lịch, đợt cố định"),
                    new MeasureScene(LiveOpsHubSections.Ids.RecurringRules, null, "màn Luật lặp"),
                    new MeasureScene(LiveOpsHubSections.Ids.Validation, null, "màn Kiểm lịch"),
                    new MeasureScene(LiveOpsHubSections.Ids.Export, null, "màn Xuất JSON"),
                };
            }

            public void Start()
            {
                EditorApplication.update += Tick;
                OpenNext();
            }

            private MeasureScene Current => _scenes[_sceneIndex];

            private void OpenNext()
            {
                CloseWindow();
                _sceneIndex++;
                if (_sceneIndex >= _scenes.Count)
                {
                    Complete();
                    return;
                }
                _frames = 0;
                _languageScope = LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.Vietnamese);
                try
                {
                    LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
                    List<IHubSection> sections = LiveOpsHubSections.Create(services);
                    if (Current.BarKey != null)
                    {
                        foreach (IHubSection section in sections)
                        {
                            if (section is CalendarSection calendar) calendar.Presenter.SetSelectedBarKey(Current.BarKey);
                        }
                    }

                    _window = LiveOpsHubWindow.OpenWithServices(services, sections, Current.SectionId);
                    // SP-16: đặt kích thước SAU Show — đặt trước bị kẹp ≈ 401×202.
                    _window.position = new Rect(0f, 0f, WindowWidth, WindowHeight);
                }
                catch (Exception exception)
                {
                    EditorApplication.update -= Tick;
                    Finish(LiveOpsHubCaptureCommand.ScenarioFailedExitCode,
                        "mở cảnh '" + Current.Place + "' ném " + exception.GetType().Name + ": " + exception.Message);
                }
            }

            private void Tick()
            {
                if (_sceneIndex >= _scenes.Count || _window == null) return;
                _frames++;
                VisualElement root = _window.rootVisualElement;
                bool ready = LiveOpsHubWindowTestScope.HasLayout(root) && _frames >= SettleFrames;
                if (!ready)
                {
                    if (_frames <= MaximumLayoutFrames) return;
                    EditorApplication.update -= Tick;
                    Finish(LiveOpsHubCaptureCommand.ScenarioFailedExitCode,
                        "cảnh '" + Current.Place + "' không có layout sau " + MaximumLayoutFrames + " vòng");
                    return;
                }

                _measuredCount += UxComposedContrast.CollectFailures(root, Current.Place, _failures);
                Debug.Log(LogPrefix + "đo xong " + Current.Place + " — tổng " + _measuredCount + " đoạn chữ, "
                    + _failures.Count + " dòng trượt");
                OpenNext();
            }

            private void Complete()
            {
                EditorApplication.update -= Tick;
                UxContrastEvidenceData data = new UxContrastEvidenceData
                {
                    schema = UxContrastEvidence.SchemaVersion,
                    unityVersion = Application.unityVersion,
                    skin = _skin,
                    proSkin = EditorGUIUtility.isProSkin,
                    sourceDigest = UxContrastEvidence.ComputeSourceDigest(),
                    measuredCount = _measuredCount,
                    sceneCount = _scenes.Count,
                    scenes = SceneNames(),
                    minimumRatio = UxComposedContrast.TextContrastRatio,
                    failures = _failures.ToArray(),
                };
                string path = UxContrastEvidence.PathFor(_skin);
                try
                {
                    UxContrastEvidence.Write(path, data);
                }
                catch (System.IO.IOException exception)
                {
                    Finish(LiveOpsHubCaptureCommand.ScenarioFailedExitCode, "ghi bằng chứng hỏng: " + exception.Message);
                    return;
                }

                Finish(LiveOpsHubCaptureCommand.SuccessExitCode, "đã ghi " + path + " — " + _measuredCount + " đoạn chữ trên "
                    + _scenes.Count + " cảnh, " + _failures.Count + " dòng trượt, dấu nguồn " + data.sourceDigest);
            }

            /// <summary>Tên đọc được của từng cảnh, đúng thứ tự đo — cổng đối chiếu danh sách này (soát W10 R-02).</summary>
            private string[] SceneNames()
            {
                string[] names = new string[_scenes.Count];
                for (int index = 0; index < _scenes.Count; index++) names[index] = _scenes[index].Place;
                return names;
            }

            private void CloseWindow()
            {
                if (_window != null)
                {
                    _window.Close();
                    _window = null;
                }
                if (_languageScope != null)
                {
                    _languageScope.Dispose();
                    _languageScope = null;
                }
                LiveOpsHubTestServices.ReleaseAll();
            }
        }
    }
}
