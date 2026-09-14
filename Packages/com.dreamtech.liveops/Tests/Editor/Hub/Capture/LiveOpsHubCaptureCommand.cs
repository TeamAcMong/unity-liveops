using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Lệnh chụp batchmode của ma trận 9.5 (<c>tools/liveops-hub/capture.sh</c> gọi qua <c>unity-slot.sh</c>):
    /// <code>
    /// Unity -batchmode -projectPath P -executeMethod DreamTech.LiveOps.Editor.Tests.LiveOpsHubCaptureCommand.CaptureFromCommandLine
    ///   -liveopsCaptureOut DIR -liveopsCaptureScenarios "id,id|registered" -liveopsCaptureSkins dark|light -logFile L
    /// </code>
    /// KHÔNG <c>-nographics</c> (layout NaN, ảnh một màu [API §12.5]) và KHÔNG <c>-quit</c> — lệnh tự <c>EditorApplication.Exit</c>.
    /// <para>
    /// Skin (SP-4, luật bắt buộc): lệnh KHÔNG BAO GIỜ đổi skin trong phiên (<c>SwitchSkinAndRepaintAllViews</c> treo Editor và ghi
    /// Preferences toàn máy). Mỗi lượt Unity nhận đúng MỘT skin; capture.sh đặt EditorPrefs <c>UserSkin</c> trước lượt. Đầu lệnh đọc
    /// <c>EditorGUIUtility.isProSkin</c>; lệch skin yêu cầu → thoát 3 kèm câu nêu skin thật, không chụp gì.
    /// </para>
    /// Mã thoát: 0 đạt · 1 kịch bản lỗi (id lạ, không có layout, ghi file hỏng) · 2 script biên dịch lỗi hoặc tham số sai ·
    /// 3 skin lệch hoặc một bước chụp nội bộ (reflection <c>m_Parent</c>/<c>GrabPixels</c>) hỏng.
    /// </summary>
    // Cần đồ hoạ thật (không -nographics) như test UI — đánh dấu cùng category để công cụ lọc không xếp nhầm vào nhóm Logic.
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    public static class LiveOpsHubCaptureCommand
    {
        public const int SuccessExitCode = 0;
        public const int ScenarioFailedExitCode = 1;
        public const int UsageExitCode = 2;
        public const int SkinOrCaptureStepExitCode = 3;

        internal const string OutputArgument = "-liveopsCaptureOut";
        internal const string ScenariosArgument = "-liveopsCaptureScenarios";
        internal const string SkinsArgument = "-liveopsCaptureSkins";
        internal const string RegisteredScenarios = "registered";
        internal const string DarkSkin = "dark";
        internal const string LightSkin = "light";
        internal const string LogPrefix = "LIVEOPS CAPTURE: ";

        /// <summary>Tối đa số vòng update chờ layout + generateVisualContent của một kịch bản (9.5).</summary>
        internal const int MaximumLayoutFrames = 60;

        /// <summary>Tên class được ghi vào JSON số đo dù element không có tên — khung measure-capture.py đo theo class.</summary>
        private static readonly HashSet<string> MeasuredClasses = new HashSet<string>(StringComparer.Ordinal)
        {
            LiveOpsHubClassNames.Root, LiveOpsHubClassNames.Header, LiveOpsHubClassNames.Rail, LiveOpsHubClassNames.SectionHeader,
            LiveOpsHubClassNames.Status, LiveOpsHubClassNames.Content, LiveOpsHubClassNames.SectionBody, LiveOpsHubClassNames.Toast,
            LiveOpsHubClassNames.Inspector, LiveOpsHubClassNames.TimelineLane, LiveOpsHubClassNames.TimelineBar, LiveOpsHubClassNames.StateMark,
            LiveOpsHubClassNames.ButtonPrimary, LiveOpsHubClassNames.RailRow, LiveOpsHubClassNames.RailStageRow, LiveOpsHubClassNames.RailBlocker,
            LiveOpsHubClassNames.Failure, LiveOpsHubClassNames.Note,
        };

        private static CaptureRun _activeRun;

        public static void CaptureFromCommandLine()
        {
            // Chạy trên assembly cũ khi script mới lỗi biên dịch sẽ ra ảnh của code cũ mà vẫn thoát 0 — chặn trước mọi thứ.
            if (EditorUtility.scriptCompilationFailed)
            {
                Finish(UsageExitCode, "script biên dịch lỗi — ảnh sẽ là của code cũ, không chụp");
                return;
            }

            CaptureOptions options;
            string usageError;
            if (!CaptureOptions.TryParse(Environment.GetCommandLineArgs(), out options, out usageError))
            {
                Finish(UsageExitCode, usageError);
                return;
            }

            string skinProblem = DescribeSkinMismatch(options.Skin, EditorGUIUtility.isProSkin);
            if (skinProblem != null)
            {
                Finish(SkinOrCaptureStepExitCode, skinProblem);
                return;
            }

            List<LiveOpsHubCaptureScenario> scenarios = new List<LiveOpsHubCaptureScenario>();
            string scenarioError = ResolveScenarios(options.Scenarios, scenarios);
            if (scenarioError != null)
            {
                Finish(ScenarioFailedExitCode, scenarioError);
                return;
            }

            Directory.CreateDirectory(options.OutputDirectory);
            Log("bắt đầu " + scenarios.Count + " kịch bản, skin " + options.Skin + ", Unity " + Application.unityVersion + " → " + options.OutputDirectory);
            _activeRun = new CaptureRun(options, scenarios);
            _activeRun.Start();
        }

        /// <summary>null khi skin Editor khớp skin yêu cầu; không thì câu nêu skin thật (thoát 3).</summary>
        internal static string DescribeSkinMismatch(string requestedSkin, bool isProSkin)
        {
            bool wantsDark = string.Equals(requestedSkin, DarkSkin, StringComparison.Ordinal);
            if (wantsDark == isProSkin) return null;
            return "yêu cầu skin " + requestedSkin + " nhưng Editor đang " + (isProSkin ? DarkSkin : LightSkin) + " (EditorGUIUtility.isProSkin = " + isProSkin +
                ") — capture.sh phải đặt EditorPrefs UserSkin trước lượt Unity; lệnh chụp không đổi skin trong phiên (SP-4)";
        }

        /// <summary>Đọc danh sách id ("registered" = mọi kịch bản đã đăng ký); null khi đủ, không thì câu lỗi nêu id lạ.</summary>
        internal static string ResolveScenarios(IReadOnlyList<string> requestedIds, List<LiveOpsHubCaptureScenario> resolved)
        {
            IReadOnlyList<LiveOpsHubCaptureScenario> registered = LiveOpsHubCaptureScenarios.All();
            if (requestedIds.Count == 1 && string.Equals(requestedIds[0], RegisteredScenarios, StringComparison.Ordinal))
            {
                resolved.AddRange(registered);
                return registered.Count == 0 ? "chưa có kịch bản nào được đăng ký" : null;
            }
            List<string> unknown = new List<string>();
            foreach (string id in requestedIds)
            {
                LiveOpsHubCaptureScenario match = null;
                foreach (LiveOpsHubCaptureScenario scenario in registered)
                {
                    if (string.Equals(scenario.Id, id, StringComparison.Ordinal)) match = scenario;
                }
                if (match == null) unknown.Add(id);
                else resolved.Add(match);
            }
            if (unknown.Count == 0) return null;
            List<string> knownIds = new List<string>();
            foreach (LiveOpsHubCaptureScenario scenario in registered) knownIds.Add(scenario.Id);
            return "kịch bản chưa đăng ký: " + string.Join(", ", unknown) + " — đã đăng ký: " + string.Join(", ", knownIds);
        }

        internal static void Log(string message)
        {
            Debug.Log(LogPrefix + message);
        }

        private static void Finish(int exitCode, string message)
        {
            if (exitCode == SuccessExitCode) Log(message);
            else Debug.LogError(LogPrefix + "thoát " + exitCode + ": " + message);
            EditorApplication.Exit(exitCode);
        }

        // ---------------------------------------------------------------------------------------------------------------- tham số

        internal sealed class CaptureOptions
        {
            private CaptureOptions(string outputDirectory, IReadOnlyList<string> scenarios, string skin)
            {
                OutputDirectory = outputDirectory;
                Scenarios = scenarios;
                Skin = skin;
            }

            public string OutputDirectory { get; }
            public IReadOnlyList<string> Scenarios { get; }
            public string Skin { get; }

            internal static bool TryParse(IReadOnlyList<string> arguments, out CaptureOptions options, out string error)
            {
                options = null;
                string output = ValueAfter(arguments, OutputArgument);
                string scenarios = ValueAfter(arguments, ScenariosArgument);
                string skins = ValueAfter(arguments, SkinsArgument);
                if (string.IsNullOrEmpty(output))
                {
                    error = "thiếu " + OutputArgument + " <thư mục>";
                    return false;
                }
                if (string.IsNullOrEmpty(scenarios))
                {
                    error = "thiếu " + ScenariosArgument + " \"id,id\" hoặc registered";
                    return false;
                }
                // Đúng MỘT skin mỗi lượt: hai skin trong một phiên buộc phải đổi skin giữa chừng — thứ SP-4 cấm.
                if (!string.Equals(skins, DarkSkin, StringComparison.Ordinal) && !string.Equals(skins, LightSkin, StringComparison.Ordinal))
                {
                    error = SkinsArgument + " phải là đúng một skin (dark hoặc light), nhận '" + skins + "' — mỗi skin một lượt Unity (SP-4)";
                    return false;
                }
                List<string> ids = new List<string>();
                foreach (string part in scenarios.Split(','))
                {
                    string id = part.Trim();
                    if (id.Length > 0) ids.Add(id);
                }
                if (ids.Count == 0)
                {
                    error = ScenariosArgument + " rỗng";
                    return false;
                }
                options = new CaptureOptions(output, ids, skins);
                error = null;
                return true;
            }

            private static string ValueAfter(IReadOnlyList<string> arguments, string name)
            {
                for (int index = 0; index < arguments.Count - 1; index++)
                {
                    if (string.Equals(arguments[index], name, StringComparison.Ordinal)) return arguments[index + 1];
                }
                return null;
            }
        }

        // ---------------------------------------------------------------------------------------------------------------- vòng chụp

        /// <summary>
        /// Máy trạng thái chạy trên <c>EditorApplication.update</c>: mở kịch bản → đặt kích thước sau Show → chờ layout → chụp → ghi →
        /// đóng → kịch bản kế. Không chặn luồng chính bằng vòng lặp: layout và generateVisualContent chỉ chạy giữa các vòng update.
        /// </summary>
        private sealed class CaptureRun
        {
            private readonly CaptureOptions _options;
            private readonly List<LiveOpsHubCaptureScenario> _scenarios;
            private readonly List<string> _failures = new List<string>();
            private int _scenarioIndex = -1;
            private EditorWindow _window;
            private int _frames;

            public CaptureRun(CaptureOptions options, List<LiveOpsHubCaptureScenario> scenarios)
            {
                _options = options;
                _scenarios = scenarios;
            }

            public void Start()
            {
                EditorApplication.update += Tick;
                OpenNext();
            }

            private LiveOpsHubCaptureScenario Current => _scenarios[_scenarioIndex];

            private void OpenNext()
            {
                CloseWindow();
                _scenarioIndex++;
                if (_scenarioIndex >= _scenarios.Count)
                {
                    Complete();
                    return;
                }
                _frames = 0;
                try
                {
                    _window = Current.OpenWindow();
                    if (_window == null) throw new InvalidOperationException("OpenWindow trả null");
                    // SP-16: đặt kích thước SAU Show — đặt trước bị kẹp ≈ 401×202.
                    _window.position = new Rect(0, 0, Current.WindowWidth, Current.WindowHeight);
                }
                catch (Exception exception)
                {
                    FailScenario("mở cửa sổ ném " + exception.GetType().Name + ": " + exception.Message);
                    OpenNext();
                }
            }

            private void Tick()
            {
                if (_scenarioIndex >= _scenarios.Count || _window == null) return;
                _frames++;
                VisualElement target;
                try
                {
                    target = Current.CaptureTarget == null ? _window.rootVisualElement : Current.CaptureTarget(_window);
                }
                catch (Exception exception)
                {
                    FailScenario("tìm element cắt ảnh ném " + exception.Message);
                    OpenNext();
                    return;
                }

                bool ready = target != null && LiveOpsHubWindowTestScope.HasLayout(target) && _frames >= Current.MinimumSettleFrames;
                if (!ready)
                {
                    if (_frames > MaximumLayoutFrames)
                    {
                        FailScenario(target == null ? "không tìm thấy element cắt ảnh" : "element cắt ảnh không có layout sau " + MaximumLayoutFrames + " vòng");
                        OpenNext();
                    }
                    return;
                }

                string stepError;
                if (!CaptureCurrent(target, out stepError))
                {
                    Log("kịch bản " + Current.Id + ": bước chụp hỏng — " + stepError);
                    CloseWindow();
                    EditorApplication.update -= Tick;
                    Finish(SkinOrCaptureStepExitCode, "bước chụp nội bộ hỏng ở " + Current.Id + ": " + stepError);
                    return;
                }
                OpenNext();
            }

            private bool CaptureCurrent(VisualElement target, out string stepError)
            {
                Texture2D full = null;
                RenderTexture renderTexture = null;
                try
                {
                    object hostView = typeof(EditorWindow).GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_window);
                    if (hostView == null)
                    {
                        stepError = "EditorWindow.m_Parent null (reflection) — cửa sổ chưa có host view";
                        return false;
                    }
                    MethodInfo repaint = FindMethod(hostView.GetType(), "RepaintImmediately", Type.EmptyTypes);
                    if (repaint == null)
                    {
                        stepError = "không tìm thấy GUIView.RepaintImmediately()";
                        return false;
                    }
                    MethodInfo grab = FindMethod(hostView.GetType(), "GrabPixels", new[] { typeof(RenderTexture), typeof(Rect) });
                    if (grab == null)
                    {
                        stepError = "không tìm thấy GUIView.GrabPixels(RenderTexture, Rect)";
                        return false;
                    }
                    Rect? hostPosition = ReadRectProperty(hostView, "position");
                    if (!hostPosition.HasValue)
                    {
                        stepError = "không đọc được View.position của host view";
                        return false;
                    }

                    repaint.Invoke(hostView, null);
                    float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
                    int width = Mathf.RoundToInt(hostPosition.Value.width * pixelsPerPoint);
                    int height = Mathf.RoundToInt(hostPosition.Value.height * pixelsPerPoint);
                    if (width <= 0 || height <= 0)
                    {
                        stepError = "host view có kích thước " + width + "×" + height;
                        return false;
                    }
                    renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    grab.Invoke(hostView, new object[] { renderTexture, new Rect(0, 0, width, height) });

                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = renderTexture;
                    full = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    full.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    full.Apply();
                    RenderTexture.active = previous;

                    // worldBound tính theo panel của host view (gồm dải tab) — cắt đúng vùng element, không cắt cả dải tab.
                    Rect bound = target.worldBound;
                    RectInt crop = ClampCrop(bound, pixelsPerPoint, width, height);
                    if (crop.width <= 0 || crop.height <= 0)
                    {
                        stepError = "vùng cắt rỗng (worldBound " + bound + " trong ảnh " + width + "×" + height + ")";
                        return false;
                    }
                    Texture2D cropped = CropFlipped(full, crop);
                    string stem = Path.Combine(_options.OutputDirectory, Current.Id + "-" + _options.Skin);
                    File.WriteAllBytes(stem + ".png", cropped.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(cropped);
                    File.WriteAllText(stem + ".json", DescribeMeasurements(target, bound, crop, pixelsPerPoint, hostPosition.Value), new UTF8Encoding(false));
                    Log("kịch bản " + Current.Id + " → " + stem + ".png (" + crop.width + "×" + crop.height + " px, host " + width + "×" + height +
                        ", root y " + _window.rootVisualElement.worldBound.y.ToString(CultureInfo.InvariantCulture) + ")");
                    stepError = null;
                    return true;
                }
                catch (TargetInvocationException exception)
                {
                    stepError = "reflection ném " + (exception.InnerException ?? exception).GetType().Name + ": " + (exception.InnerException ?? exception).Message;
                    return false;
                }
                catch (IOException exception)
                {
                    // Ghi file hỏng là lỗi kịch bản (đĩa/quyền), không phải bước chụp nội bộ.
                    FailScenario("ghi file hỏng: " + exception.Message);
                    stepError = null;
                    return true;
                }
                finally
                {
                    if (full != null) UnityEngine.Object.DestroyImmediate(full);
                    if (renderTexture != null)
                    {
                        renderTexture.Release();
                        UnityEngine.Object.DestroyImmediate(renderTexture);
                    }
                }
            }

            private string DescribeMeasurements(VisualElement target, Rect bound, RectInt crop, float pixelsPerPoint, Rect hostPosition)
            {
                StringBuilder json = new StringBuilder();
                json.Append("{\n");
                AppendProperty(json, "scenario", Current.Id, true);
                AppendProperty(json, "skin", _options.Skin, true);
                AppendProperty(json, "unityVersion", Application.unityVersion, true);
                json.Append("  \"pixelsPerPoint\": ").Append(Number(pixelsPerPoint)).Append(",\n");
                json.Append("  \"window\": {\"width\": ").Append(Number(crop.width / pixelsPerPoint)).Append(", \"height\": ")
                    .Append(Number(crop.height / pixelsPerPoint)).Append("},\n");
                json.Append("  \"requestedWindow\": {\"width\": ").Append(Current.WindowWidth).Append(", \"height\": ").Append(Current.WindowHeight).Append("},\n");
                json.Append("  \"hostView\": {\"width\": ").Append(Number(hostPosition.width)).Append(", \"height\": ").Append(Number(hostPosition.height)).Append("},\n");
                json.Append("  \"rootLayout\": {\"width\": ").Append(Number(_window.rootVisualElement.layout.width)).Append(", \"height\": ")
                    .Append(Number(_window.rootVisualElement.layout.height)).Append("},\n");

                json.Append("  \"elements\": [");
                bool first = true;
                AppendElements(json, target, target, bound, ref first);
                json.Append("\n  ]");

                if (Current.ExpectedFrames.Count > 0)
                {
                    json.Append(",\n  \"expectedFrames\": [");
                    for (int index = 0; index < Current.ExpectedFrames.Count; index++)
                    {
                        LiveOpsHubCaptureExpectedFrame frame = Current.ExpectedFrames[index];
                        json.Append(index == 0 ? "\n    " : ",\n    ").Append("{\"element\": ").Append(Quote(frame.Element));
                        if (frame.Width > 0) json.Append(", \"width\": ").Append(Number(frame.Width));
                        if (frame.Height > 0) json.Append(", \"height\": ").Append(Number(frame.Height));
                        json.Append('}');
                    }
                    json.Append("\n  ]");
                }
                json.Append("\n}\n");
                return json.ToString();
            }

            private static void AppendElements(StringBuilder json, VisualElement element, VisualElement target, Rect targetBound, ref bool first)
            {
                if (ShouldDescribe(element, target))
                {
                    Rect bound = element.worldBound;
                    json.Append(first ? "\n    " : ",\n    ");
                    first = false;
                    json.Append("{\"name\": ").Append(Quote(element.name ?? string.Empty)).Append(", \"classes\": [");
                    bool firstClass = true;
                    foreach (string className in element.GetClasses())
                    {
                        if (!firstClass) json.Append(", ");
                        json.Append(Quote(className));
                        firstClass = false;
                    }
                    json.Append("], \"worldBound\": {\"x\": ").Append(Number(bound.x - targetBound.x)).Append(", \"y\": ").Append(Number(bound.y - targetBound.y))
                        .Append(", \"width\": ").Append(Number(bound.width)).Append(", \"height\": ").Append(Number(bound.height)).Append("}}");
                }
                for (int index = 0; index < element.hierarchy.childCount; index++)
                {
                    AppendElements(json, element.hierarchy[index], target, targetBound, ref first);
                }
            }

            private static bool ShouldDescribe(VisualElement element, VisualElement target)
            {
                if (element == target) return true;
                if (float.IsNaN(element.worldBound.width)) return false;
                if (!string.IsNullOrEmpty(element.name) && !element.name.StartsWith("unity-", StringComparison.Ordinal)) return true;
                foreach (string className in element.GetClasses())
                {
                    if (MeasuredClasses.Contains(className)) return true;
                }
                return false;
            }

            private static RectInt ClampCrop(Rect bound, float pixelsPerPoint, int imageWidth, int imageHeight)
            {
                int left = Mathf.Clamp(Mathf.RoundToInt(bound.x * pixelsPerPoint), 0, imageWidth);
                int top = Mathf.Clamp(Mathf.RoundToInt(bound.y * pixelsPerPoint), 0, imageHeight);
                int right = Mathf.Clamp(Mathf.RoundToInt(bound.xMax * pixelsPerPoint), 0, imageWidth);
                int bottom = Mathf.Clamp(Mathf.RoundToInt(bound.yMax * pixelsPerPoint), 0, imageHeight);
                return new RectInt(left, top, right - left, bottom - top);
            }

            /// <summary>
            /// GrabPixels trả ảnh lộn dọc so với cách EncodeToPNG ghi ([API §9]): hàng 0 của texture đọc về là hàng TRÊN CÙNG của cửa sổ.
            /// Cắt theo toạ độ trên-trái rồi đảo hàng để PNG ra đúng chiều.
            /// </summary>
            private static Texture2D CropFlipped(Texture2D full, RectInt crop)
            {
                Color32[] source = full.GetPixels32();
                Color32[] destination = new Color32[crop.width * crop.height];
                for (int row = 0; row < crop.height; row++)
                {
                    int sourceRow = crop.y + row;
                    int destinationRow = crop.height - 1 - row;
                    Array.Copy(source, sourceRow * full.width + crop.x, destination, destinationRow * crop.width, crop.width);
                }
                Texture2D cropped = new Texture2D(crop.width, crop.height, TextureFormat.RGBA32, false);
                cropped.SetPixels32(destination);
                cropped.Apply();
                return cropped;
            }

            private static MethodInfo FindMethod(Type type, string name, Type[] parameterTypes)
            {
                for (Type current = type; current != null; current = current.BaseType)
                {
                    MethodInfo method = current.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                        null, parameterTypes, null);
                    if (method != null) return method;
                }
                return null;
            }

            private static Rect? ReadRectProperty(object instance, string name)
            {
                for (Type current = instance.GetType(); current != null; current = current.BaseType)
                {
                    PropertyInfo property = current.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (property != null && property.PropertyType == typeof(Rect)) return (Rect)property.GetValue(instance, null);
                }
                return null;
            }

            private void FailScenario(string reason)
            {
                string id = _scenarioIndex >= 0 && _scenarioIndex < _scenarios.Count ? Current.Id : "?";
                _failures.Add(id + ": " + reason);
                Debug.LogError(LogPrefix + "kịch bản " + id + " lỗi — " + reason);
            }

            private void CloseWindow()
            {
                if (_window != null)
                {
                    try
                    {
                        _window.Close();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(LogPrefix + "đóng cửa sổ ném " + exception.Message);
                    }
                }
                _window = null;
            }

            private void Complete()
            {
                EditorApplication.update -= Tick;
                _activeRun = null;
                if (_failures.Count == 0)
                {
                    Finish(SuccessExitCode, "xong " + _scenarios.Count + " kịch bản, skin " + _options.Skin);
                    return;
                }
                Finish(ScenarioFailedExitCode, _failures.Count + " kịch bản lỗi: " + string.Join(" | ", _failures));
            }

            private static void AppendProperty(StringBuilder json, string name, string value, bool trailingComma)
            {
                json.Append("  ").Append(Quote(name)).Append(": ").Append(Quote(value)).Append(trailingComma ? ",\n" : "\n");
            }

            private static string Number(float value)
            {
                return float.IsNaN(value) || float.IsInfinity(value) ? "0" : value.ToString("0.###", CultureInfo.InvariantCulture);
            }

            private static string Quote(string value)
            {
                StringBuilder builder = new StringBuilder("\"");
                foreach (char character in value ?? string.Empty)
                {
                    switch (character)
                    {
                        case '"': builder.Append("\\\""); break;
                        case '\\': builder.Append("\\\\"); break;
                        case '\n': builder.Append("\\n"); break;
                        case '\r': builder.Append("\\r"); break;
                        case '\t': builder.Append("\\t"); break;
                        default:
                            if (character < ' ') builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                            else builder.Append(character);
                            break;
                    }
                }
                return builder.Append('"').ToString();
            }
        }
    }
}
