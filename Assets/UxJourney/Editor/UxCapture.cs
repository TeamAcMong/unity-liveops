// MÃ DEV TẠM (G-UX-JOURNEY) — chụp cửa sổ (GrabPixels), chụp màn hình OS và chẩn đoán bố cục.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UxJourney
{
    public static class UxCapture
    {
        /// <summary>Menubar macOS + thanh tiêu đề cửa sổ nổi + dải tab của DockArea, đo ở lượt khám phá.</summary>
        public static float TitleBarAllowance = 58f;
        public const float ScreenWidth = 1920f;
        public const float ScreenHeight = 1080f;

        /// <summary>
        /// Một bước chụp: chờ vẽ lại, GrabPixels cửa sổ hub (-grab.png), screencapture vùng mọi cửa sổ nổi (.png), JSON chẩn đoán.
        /// </summary>
        public static IEnumerator Step(UxRunner runner, EditorWindow hub, string stepId, string note, params EditorWindow[] extraWindows)
        {
            runner.CurrentStep = stepId;
            if (hub != null) hub.Repaint();
            foreach (EditorWindow extra in extraWindows)
            {
                if (extra != null) extra.Repaint();
            }
            yield return new WaitFrames(4);
            yield return new WaitSeconds(0.45);
            string stem = Path.Combine(runner.Options.OutputDirectory, stepId);
            string grabError = null;
            if (hub != null)
            {
                grabError = GrabWindow(hub, stem + "-grab.png");
            }
            List<Rect> tracked = new List<Rect>();
            if (hub != null) tracked.Add(hub.position);
            foreach (EditorWindow extra in extraWindows) if (extra != null) tracked.Add(extra.position);
            foreach (EditorWindow floating in AllFloatingWindows()) if (floating != null && floating != hub) tracked.Add(floating.position);
            string osResult = CaptureProcessWindows(runner, stem, tracked);
            Rect region = OsRegion(hub, extraWindows);
            List<EditorWindow> diagnosed = new List<EditorWindow>();
            if (hub != null) diagnosed.Add(hub);
            foreach (EditorWindow extra in AllFloatingWindows())
            {
                if (extra != null && extra != hub && !diagnosed.Contains(extra)) diagnosed.Add(extra);
            }
            string json = UxDiagnostics.Describe(runner, stepId, note, hub, diagnosed, region, grabError, osResult);
            File.WriteAllText(stem + ".json", json, new UTF8Encoding(false));
            runner.Log("chụp " + stepId + " — " + note + (grabError != null ? " (grab lỗi: " + grabError + ")" : string.Empty));
        }

        public static List<EditorWindow> AllFloatingWindows()
        {
            List<EditorWindow> result = new List<EditorWindow>();
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null) continue;
                string typeName = window.GetType().FullName ?? string.Empty;
                if (typeName.StartsWith("DreamTech.LiveOps", StringComparison.Ordinal) || typeName == "UnityEditor.PopupWindow" ||
                    typeName.Contains("Popup") || typeName.Contains("Tooltip"))
                {
                    if (IsDocked(window) && !typeName.StartsWith("DreamTech.LiveOps", StringComparison.Ordinal)) continue;
                    result.Add(window);
                }
            }
            return result;
        }

        public static bool IsDocked(EditorWindow window)
        {
            PropertyInfo property = UxDiagnostics.FindProperty(typeof(EditorWindow), "docked");
            if (property == null) return false;
            try
            {
                return (bool)property.GetValue(window, null);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static Rect OsRegion(EditorWindow hub, EditorWindow[] extraWindows)
        {
            bool any = false;
            Rect union = new Rect();
            void Include(EditorWindow window, float titleAllowance)
            {
                if (window == null) return;
                Rect position = window.position;
                Rect expanded = new Rect(position.x - 6f, position.y - titleAllowance, position.width + 12f, position.height + titleAllowance + 6f);
                if (!any)
                {
                    union = expanded;
                    any = true;
                }
                else
                {
                    union = Rect.MinMaxRect(Math.Min(union.xMin, expanded.xMin), Math.Min(union.yMin, expanded.yMin),
                        Math.Max(union.xMax, expanded.xMax), Math.Max(union.yMax, expanded.yMax));
                }
            }
            Include(hub, TitleBarAllowance);
            if (extraWindows != null)
            {
                foreach (EditorWindow extra in extraWindows) Include(extra, 30f);
            }
            foreach (EditorWindow floating in AllFloatingWindows())
            {
                if (floating != hub) Include(floating, 30f);
            }
            if (!any) union = new Rect(0, 0, ScreenWidth, ScreenHeight);
            float xMin = Mathf.Clamp(union.xMin, 0, ScreenWidth);
            float yMin = Mathf.Clamp(union.yMin, 0, ScreenHeight);
            float xMax = Mathf.Clamp(union.xMax, 0, ScreenWidth);
            float yMax = Mathf.Clamp(union.yMax, 0, ScreenHeight);
            return Rect.MinMaxRect(Mathf.Floor(xMin), Mathf.Floor(yMin), Mathf.Ceil(xMax), Mathf.Ceil(yMax));
        }

        /// <summary>
        /// Chụp từng cửa sổ của CHÍNH tiến trình Unity này bằng CGWindowID (screencapture -l — không phụ thuộc cửa sổ khác che
        /// lên, không chụp app khác của người dùng) rồi ghép theo toạ độ màn hình + thứ tự z. <paramref name="tracked"/> null
        /// (luồng watchdog) = mọi cửa sổ trên màn hình trừ cửa sổ chính của Editor. Không gọi API Unity — chạy được ở luồng nền.
        /// </summary>
        public static string CaptureProcessWindows(UxRunner runner, string stem, List<Rect> tracked)
        {
            string listing = runner.Helper("list " + runner.ProcessId);
            List<string[]> chosen = new List<string[]>();
            foreach (string line in listing.Split('\n'))
            {
                string[] cells = line.Split('\t');
                if (cells.Length < 9) continue;
                int layer = int.Parse(cells[1], CultureInfo.InvariantCulture);
                float x = float.Parse(cells[2], CultureInfo.InvariantCulture), y = float.Parse(cells[3], CultureInfo.InvariantCulture);
                float w = float.Parse(cells[4], CultureInfo.InvariantCulture), h = float.Parse(cells[5], CultureInfo.InvariantCulture);
                bool onscreen = cells[6] == "1";
                string name = cells[8];
                if (!onscreen || w < 8 || h < 8) continue;
                if (name.Contains("Windows, Mac, Linux")) continue;
                if (w >= 1900 && h <= 40) continue;
                bool include = layer != 0;
                if (!include)
                {
                    if (tracked == null) include = true;
                    else
                    {
                        Rect bounds = new Rect(x, y, w, h);
                        foreach (Rect rect in tracked)
                        {
                            if (bounds.Contains(rect.center)) include = true;
                        }
                    }
                }
                if (include) chosen.Add(cells);
            }
            if (chosen.Count == 0) return "không có cửa sổ nào để chụp";
            string partsDirectory = Path.Combine(Path.GetDirectoryName(stem), "_parts");
            Directory.CreateDirectory(partsDirectory);
            StringBuilder composeArguments = new StringBuilder();
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            List<string> specs = new List<string>();
            // CGWindowList trả trước → sau; ghép sau → trước.
            for (int index = chosen.Count - 1; index >= 0; index--)
            {
                string[] cells = chosen[index];
                string part = Path.Combine(partsDirectory, Path.GetFileName(stem) + "-w" + cells[0] + ".png");
                RunProcessStatic("/usr/sbin/screencapture", "-x -o -l " + cells[0] + " '" + part + "'");
                float x = float.Parse(cells[2], CultureInfo.InvariantCulture), y = float.Parse(cells[3], CultureInfo.InvariantCulture);
                float w = float.Parse(cells[4], CultureInfo.InvariantCulture), h = float.Parse(cells[5], CultureInfo.InvariantCulture);
                minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x + w); maxY = Math.Max(maxY, y + h);
                specs.Add("'" + part + "':" + (int)x + ":" + (int)y);
            }
            composeArguments.Append("'").Append(stem).Append(".png' ").Append((int)minX).Append(' ').Append((int)minY).Append(' ')
                .Append((int)(maxX - minX)).Append(' ').Append((int)(maxY - minY));
            foreach (string spec in specs) composeArguments.Append(' ').Append(spec);
            string result = UxRunner.RunProcess("/usr/bin/python3", "'" + Path.Combine(runner.Options.ToolsDirectory, "compose.py") + "' " + composeArguments, 30000);
            StringBuilder summary = new StringBuilder();
            foreach (string[] cells in chosen) summary.Append("[id " + cells[0] + " layer " + cells[1] + " " + cells[2] + "," + cells[3] + " " + cells[4] + "×" + cells[5] + " '" + cells[8] + "'] ");
            return summary + "→ " + result.Trim();
        }

        private static void RunProcessStatic(string file, string arguments)
        {
            UxRunner.RunProcess(file, arguments, 15000);
        }

        public static string OsScreenshot(Rect region, string path)
        {
            string arguments = string.Format(CultureInfo.InvariantCulture, "-x -R {0},{1},{2},{3} '{4}'", (int)region.x, (int)region.y,
                (int)region.width, (int)region.height, path);
            return UxRunner.RunProcess("/usr/sbin/screencapture", arguments, 15000).Trim();
        }

        public static string GrabWindow(EditorWindow window, string path)
        {
            Texture2D full = null;
            RenderTexture renderTexture = null;
            try
            {
                object hostView = typeof(EditorWindow).GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(window);
                if (hostView == null) return "m_Parent null";
                MethodInfo repaint = FindMethod(hostView.GetType(), "RepaintImmediately", Type.EmptyTypes);
                MethodInfo grab = FindMethod(hostView.GetType(), "GrabPixels", new[] { typeof(RenderTexture), typeof(Rect) });
                Rect? hostPosition = ReadRectProperty(hostView, "position");
                if (repaint == null || grab == null || !hostPosition.HasValue) return "thiếu RepaintImmediately/GrabPixels/position";
                repaint.Invoke(hostView, null);
                float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
                int width = Mathf.RoundToInt(hostPosition.Value.width * pixelsPerPoint);
                int height = Mathf.RoundToInt(hostPosition.Value.height * pixelsPerPoint);
                if (width <= 0 || height <= 0) return "host " + width + "x" + height;
                renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                grab.Invoke(hostView, new object[] { renderTexture, new Rect(0, 0, width, height) });
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTexture;
                full = new Texture2D(width, height, TextureFormat.RGBA32, false);
                full.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                full.Apply();
                RenderTexture.active = previous;
                Rect bound = window.rootVisualElement.worldBound;
                int left = Mathf.Clamp(Mathf.RoundToInt(bound.x * pixelsPerPoint), 0, width);
                int top = Mathf.Clamp(Mathf.RoundToInt(bound.y * pixelsPerPoint), 0, height);
                int right = Mathf.Clamp(Mathf.RoundToInt(bound.xMax * pixelsPerPoint), 0, width);
                int bottom = Mathf.Clamp(Mathf.RoundToInt(bound.yMax * pixelsPerPoint), 0, height);
                RectInt crop = new RectInt(left, top, right - left, bottom - top);
                if (crop.width <= 0 || crop.height <= 0) return "crop rỗng";
                Color32[] source = full.GetPixels32();
                Color32[] destination = new Color32[crop.width * crop.height];
                for (int row = 0; row < crop.height; row++)
                {
                    Array.Copy(source, (crop.y + row) * full.width + crop.x, destination, (crop.height - 1 - row) * crop.width, crop.width);
                }
                Texture2D cropped = new Texture2D(crop.width, crop.height, TextureFormat.RGBA32, false);
                cropped.SetPixels32(destination);
                cropped.Apply();
                File.WriteAllBytes(path, cropped.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(cropped);
                return null;
            }
            catch (Exception exception)
            {
                Exception inner = exception is TargetInvocationException && exception.InnerException != null ? exception.InnerException : exception;
                return inner.GetType().Name + ": " + inner.Message;
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
    }

    /// <summary>Chẩn đoán bố cục: phần tử tên cao/rộng 0, chữ bị cắt/tràn, con tràn cha, anh em chồng nhau, ScrollView thiếu thanh cuộn.</summary>
    public static class UxDiagnostics
    {
        private const int MaximumEntriesPerKind = 250;
        private static MethodInfo _measureTextSize;
        private static PropertyInfo _isElided;

        public static string Describe(UxRunner runner, string stepId, string note, EditorWindow hub, List<EditorWindow> windows, Rect osRegion,
            string grabError, string osResult)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\n");
            Prop(json, "step", stepId);
            Prop(json, "note", note);
            Prop(json, "unityVersion", Application.unityVersion);
            Prop(json, "label", runner.Options.Label);
            json.Append("  \"pixelsPerPoint\": ").Append(Num(EditorGUIUtility.pixelsPerPoint)).Append(",\n");
            json.Append("  \"osRegion\": ").Append(RectJson(osRegion)).Append(",\n");
            Prop(json, "grabError", grabError ?? string.Empty);
            Prop(json, "osScreenshotResult", osResult ?? string.Empty);
            EditorWindow focused = EditorWindow.focusedWindow;
            Prop(json, "focusedWindow", focused == null ? string.Empty : focused.GetType().Name + " " + focused.titleContent.text);
            json.Append("  \"windows\": [");
            bool firstWindow = true;
            foreach (EditorWindow window in windows)
            {
                if (window == null) continue;
                json.Append(firstWindow ? "\n    " : ",\n    ");
                firstWindow = false;
                DescribeWindow(json, window, window == hub);
            }
            json.Append("\n  ],\n");
            List<string> console = runner.DrainConsole();
            json.Append("  \"console\": [");
            for (int index = 0; index < console.Count; index++)
            {
                json.Append(index == 0 ? "\n    " : ",\n    ").Append(Quote(console[index]));
            }
            json.Append("\n  ]\n}\n");
            return json.ToString();
        }

        private sealed class Findings
        {
            public readonly List<string> ZeroSize = new List<string>();
            public readonly List<string> TextCut = new List<string>();
            public readonly List<string> TextOutsideWindow = new List<string>();
            public readonly List<string> ChildOverflow = new List<string>();
            public readonly List<string> SiblingOverlap = new List<string>();
            public readonly List<string> AbsoluteOverText = new List<string>();
            public readonly List<string> ScrollViews = new List<string>();
            public readonly List<string> Texts = new List<string>();
            public readonly List<string> Focus = new List<string>();
        }

        private static void DescribeWindow(StringBuilder json, EditorWindow window, bool isHub)
        {
            VisualElement root = window.rootVisualElement;
            Rect rootBound = root.worldBound;
            Findings findings = new Findings();
            Visit(root, rootBound, findings, true);
            json.Append("{\"type\": ").Append(Quote(window.GetType().FullName)).Append(", \"title\": ").Append(Quote(window.titleContent.text))
                .Append(", \"isHub\": ").Append(isHub ? "true" : "false")
                .Append(", \"position\": ").Append(RectJson(window.position))
                .Append(", \"rootBound\": ").Append(RectJson(rootBound))
                .Append(", \"hasFocus\": ").Append(window.hasFocus ? "true" : "false");
            VisualElement focusedElement = root.panel?.focusController?.focusedElement as VisualElement;
            json.Append(", \"focusedElement\": ").Append(Quote(focusedElement == null ? string.Empty : Name(focusedElement)));
            Array(json, "zeroSizeNamed", findings.ZeroSize);
            Array(json, "textCut", findings.TextCut);
            Array(json, "textOutsideWindow", findings.TextOutsideWindow);
            Array(json, "childOverflow", findings.ChildOverflow);
            Array(json, "siblingOverlap", findings.SiblingOverlap);
            Array(json, "absoluteOverText", findings.AbsoluteOverText);
            Array(json, "scrollViews", findings.ScrollViews);
            Array(json, "texts", findings.Texts);
            json.Append("}");
        }

        private static bool IsShown(VisualElement element)
        {
            IResolvedStyle style = element.resolvedStyle;
            if (style.display == DisplayStyle.None) return false;
            if (style.visibility == Visibility.Hidden) return false;
            if (style.opacity <= 0.01f) return false;
            return true;
        }

        private static void Visit(VisualElement element, Rect rootBound, Findings findings, bool ancestorsShown)
        {
            if (element.resolvedStyle.display == DisplayStyle.None) return;
            bool shown = ancestorsShown && IsShown(element);
            Rect bound = element.worldBound;
            if (float.IsNaN(bound.width) || float.IsNaN(bound.height)) return;
            if (shown)
            {
                CheckElement(element, bound, rootBound, findings);
            }
            int childCount = element.hierarchy.childCount;
            List<VisualElement> relativeChildren = new List<VisualElement>();
            List<VisualElement> absoluteChildren = new List<VisualElement>();
            List<VisualElement> textChildren = new List<VisualElement>();
            for (int index = 0; index < childCount; index++)
            {
                VisualElement child = element.hierarchy[index];
                if (child.resolvedStyle.display == DisplayStyle.None) continue;
                if (shown && IsShown(child))
                {
                    Rect childBound = child.worldBound;
                    if (childBound.width > 0.5f && childBound.height > 0.5f)
                    {
                        if (child.resolvedStyle.position == Position.Absolute) absoluteChildren.Add(child);
                        else relativeChildren.Add(child);
                        if (child is TextElement || ContainsText(child)) textChildren.Add(child);
                        CheckChildOverflow(element, bound, child, childBound, rootBound, findings);
                    }
                }
                Visit(child, rootBound, findings, shown);
            }
            if (!shown) return;
            for (int a = 0; a < relativeChildren.Count; a++)
            {
                for (int b = a + 1; b < relativeChildren.Count; b++)
                {
                    Rect first = relativeChildren[a].worldBound;
                    Rect second = relativeChildren[b].worldBound;
                    float overlapWidth = Math.Min(first.xMax, second.xMax) - Math.Max(first.xMin, second.xMin);
                    float overlapHeight = Math.Min(first.yMax, second.yMax) - Math.Max(first.yMin, second.yMin);
                    if (overlapWidth > 1f && overlapHeight > 1f && findings.SiblingOverlap.Count < MaximumEntriesPerKind)
                    {
                        findings.SiblingOverlap.Add(Name(relativeChildren[a]) + " ∩ " + Name(relativeChildren[b]) + " = " +
                                                    Num(overlapWidth) + "×" + Num(overlapHeight) + " @" + RectText(first, rootBound) + "/" +
                                                    RectText(second, rootBound));
                    }
                }
            }
            foreach (VisualElement overlay in absoluteChildren)
            {
                Rect overlayBound = overlay.worldBound;
                foreach (VisualElement other in textChildren)
                {
                    if (other == overlay) continue;
                    if (other.resolvedStyle.position == Position.Absolute) continue;
                    Rect otherBound = other.worldBound;
                    float overlapWidth = Math.Min(overlayBound.xMax, otherBound.xMax) - Math.Max(overlayBound.xMin, otherBound.xMin);
                    float overlapHeight = Math.Min(overlayBound.yMax, otherBound.yMax) - Math.Max(overlayBound.yMin, otherBound.yMin);
                    if (overlapWidth > 2f && overlapHeight > 2f && findings.AbsoluteOverText.Count < MaximumEntriesPerKind)
                    {
                        findings.AbsoluteOverText.Add(Name(overlay) + " phủ " + Name(other) + " " + Num(overlapWidth) + "×" + Num(overlapHeight) + " @" +
                                                      RectText(overlayBound, rootBound));
                    }
                }
            }
        }

        private static bool ContainsText(VisualElement element)
        {
            int count = element.hierarchy.childCount;
            for (int index = 0; index < count && index < 12; index++)
            {
                if (element.hierarchy[index] is TextElement) return true;
            }
            return false;
        }

        private static void CheckElement(VisualElement element, Rect bound, Rect rootBound, Findings findings)
        {
            if (!string.IsNullOrEmpty(element.name) && !element.name.StartsWith("unity-", StringComparison.Ordinal) &&
                (bound.width < 0.5f || bound.height < 0.5f) && findings.ZeroSize.Count < MaximumEntriesPerKind)
            {
                findings.ZeroSize.Add(Name(element) + " " + RectText(bound, rootBound));
            }

            if (element is TextElement text && !string.IsNullOrEmpty(text.text) && bound.width > 0f)
            {
                string value = text.text;
                Rect content = text.contentRect;
                Vector2 natural = Measure(text, value, float.NaN, float.NaN);
                string whiteSpace = text.resolvedStyle.whiteSpace.ToString();
                bool wraps = whiteSpace == "Normal" || whiteSpace == "PreWrap";
                string elided = IsElided(text);
                string cut = null;
                if (!wraps && natural.x > content.width + 1.5f)
                {
                    cut = "ngang cần " + Num(natural.x) + " có " + Num(content.width);
                }
                else if (wraps)
                {
                    Vector2 wrapped = Measure(text, value, content.width, float.NaN);
                    if (wrapped.y > content.height + 1.5f) cut = "dọc cần " + Num(wrapped.y) + " có " + Num(content.height);
                }
                if (elided == "true" && cut == null) cut = "isElided";
                bool clippedByAncestor = ClippedByAncestor(text, bound, out string clipper);
                if ((cut != null || clippedByAncestor) && findings.TextCut.Count < MaximumEntriesPerKind)
                {
                    findings.TextCut.Add(Name(text) + " \"" + Short(value, 90) + "\" " + (cut ?? string.Empty) +
                                         (clippedByAncestor ? " | bị cha cắt: " + clipper : string.Empty) + " overflow=" + text.resolvedStyle.textOverflow +
                                         " ws=" + whiteSpace + " @" + RectText(bound, rootBound));
                }
                if ((bound.xMax > rootBound.xMax + 1f || bound.yMax > rootBound.yMax + 1f || bound.xMin < rootBound.xMin - 1f) &&
                    findings.TextOutsideWindow.Count < MaximumEntriesPerKind)
                {
                    findings.TextOutsideWindow.Add(Name(text) + " \"" + Short(value, 60) + "\" @" + RectText(bound, rootBound));
                }
                if (findings.Texts.Count < 900)
                {
                    findings.Texts.Add(RectText(bound, rootBound) + " " + element.GetType().Name + " \"" + Short(value, 140) + "\"");
                }
            }
            else if (element is TextField field && findings.Texts.Count < 900)
            {
                findings.Texts.Add(RectText(bound, rootBound) + " TextField[" + field.label + "] = \"" + Short(field.value, 80) + "\"");
            }

            if (element is ScrollView scrollView && findings.ScrollViews.Count < MaximumEntriesPerKind)
            {
                VisualElement viewport = scrollView.contentViewport;
                VisualElement contentContainer = scrollView.contentContainer;
                Rect viewportRect = viewport.layout;
                Rect contentRect = contentContainer.layout;
                bool vertical = scrollView.verticalScroller.resolvedStyle.display != DisplayStyle.None &&
                                scrollView.verticalScroller.resolvedStyle.visibility != Visibility.Hidden;
                bool horizontal = scrollView.horizontalScroller.resolvedStyle.display != DisplayStyle.None &&
                                  scrollView.horizontalScroller.resolvedStyle.visibility != Visibility.Hidden;
                string entry = Name(scrollView) + " viewport " + Num(viewportRect.width) + "×" + Num(viewportRect.height) + " content " +
                               Num(contentRect.width) + "×" + Num(contentRect.height) + " vbar=" + vertical + " hbar=" + horizontal;
                bool hiddenOverflowY = contentRect.height > viewportRect.height + 1f && !vertical;
                bool hiddenOverflowX = contentRect.width > viewportRect.width + 1f && !horizontal;
                if (hiddenOverflowY) entry += " | TRÀN DỌC KHÔNG CÓ THANH CUỘN";
                if (hiddenOverflowX) entry += " | TRÀN NGANG KHÔNG CÓ THANH CUỘN";
                if (horizontal) entry += " | CÓ THANH CUỘN NGANG";
                findings.ScrollViews.Add(entry + " @" + RectText(scrollView.worldBound, rootBound));
            }
        }

        private static bool ClippedByAncestor(VisualElement element, Rect bound, out string clipper)
        {
            clipper = null;
            for (VisualElement ancestor = element.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
            {
                if (OverflowOf(ancestor) != "Hidden" && !(ancestor is ScrollView)) continue;
                Rect clip = ancestor.worldBound;
                if (bound.xMax > clip.xMax + 1.5f || bound.xMin < clip.xMin - 1.5f || bound.yMax > clip.yMax + 1.5f || bound.yMin < clip.yMin - 1.5f)
                {
                    // Nội dung ScrollView cuộn ra ngoài viewport là bình thường khi có thanh cuộn; chỉ ghi phần cắt theo chiều ngang.
                    if (ancestor is ScrollView && bound.xMax <= clip.xMax + 1.5f && bound.xMin >= clip.xMin - 1.5f) return false;
                    clipper = Name(ancestor) + " " + RectText(clip, clip);
                    return true;
                }
                return false;
            }
            return false;
        }

        private static void CheckChildOverflow(VisualElement parent, Rect parentBound, VisualElement child, Rect childBound, Rect rootBound, Findings findings)
        {
            if (parentBound.width < 1f || parentBound.height < 1f) return;
            if (findings.ChildOverflow.Count >= MaximumEntriesPerKind) return;
            if (parent is ScrollView || parent.name == "unity-content-container" || parent.name == "unity-content-viewport") return;
            float right = childBound.xMax - parentBound.xMax;
            float left = parentBound.xMin - childBound.xMin;
            float bottom = childBound.yMax - parentBound.yMax;
            float top = parentBound.yMin - childBound.yMin;
            if (right > 1.5f || left > 1.5f || bottom > 1.5f || top > 1.5f)
            {
                findings.ChildOverflow.Add(Name(child) + " tràn " + Name(parent) + " [phải " + Num(right) + ", trái " + Num(left) + ", dưới " + Num(bottom) +
                                           ", trên " + Num(top) + "] cha overflow=" + OverflowOf(parent) + " @" + RectText(childBound, rootBound));
            }
        }

        private static PropertyInfo _computedStyle;

        public static PropertyInfo FindProperty(Type type, string name)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                foreach (PropertyInfo property in current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (property.Name == name && property.GetIndexParameters().Length == 0) return property;
                }
            }
            return null;
        }

        /// <summary>IResolvedStyle không có overflow — đọc computedStyle (internal) bằng reflection.</summary>
        public static string OverflowOf(VisualElement element)
        {
            try
            {
                if (_computedStyle == null) _computedStyle = FindProperty(typeof(VisualElement), "computedStyle");
                if (_computedStyle == null) return "?";
                object computed = _computedStyle.GetValue(element, null);
                if (computed == null) return "?";
                Type type = computed.GetType();
                PropertyInfo property = FindProperty(type, "overflow");
                if (property != null) return property.GetValue(computed, null).ToString();
                FieldInfo field = type.GetField("overflow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return field != null ? field.GetValue(computed).ToString() : "?";
            }
            catch (Exception)
            {
                return "?";
            }
        }

        private static Vector2 Measure(TextElement text, string value, float width, float height)
        {
            if (_measureTextSize == null)
            {
                foreach (MethodInfo candidate in typeof(TextElement).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (candidate.Name == "MeasureTextSize" && candidate.GetParameters().Length == 5 && candidate.GetParameters()[0].ParameterType == typeof(string))
                    {
                        _measureTextSize = candidate;
                        break;
                    }
                }
            }
            if (_measureTextSize == null) return Vector2.zero;
            try
            {
                Type modeType = _measureTextSize.GetParameters()[2].ParameterType;
                object undefined = Enum.Parse(modeType, "Undefined");
                object exactly = Enum.Parse(modeType, "Exactly");
                object result = _measureTextSize.Invoke(text, new object[]
                {
                    value, float.IsNaN(width) ? 0f : width, float.IsNaN(width) ? undefined : exactly, float.IsNaN(height) ? 0f : height,
                    float.IsNaN(height) ? undefined : exactly,
                });
                return (Vector2)result;
            }
            catch (Exception)
            {
                return Vector2.zero;
            }
        }

        private static string IsElided(TextElement text)
        {
            if (_isElided == null) _isElided = FindProperty(typeof(TextElement), "isElided");
            if (_isElided == null) return "?";
            try
            {
                return ((bool)_isElided.GetValue(text, null)) ? "true" : "false";
            }
            catch (Exception)
            {
                return "?";
            }
        }

        public static string Name(VisualElement element)
        {
            StringBuilder builder = new StringBuilder(element.GetType().Name);
            if (!string.IsNullOrEmpty(element.name)) builder.Append('#').Append(element.name);
            int classes = 0;
            foreach (string className in element.GetClasses())
            {
                if (className.StartsWith("unity-", StringComparison.Ordinal) && classes > 0) continue;
                builder.Append('.').Append(className);
                if (++classes >= 3) break;
            }
            if (element is TextElement text && !string.IsNullOrEmpty(text.text)) builder.Append(" '").Append(Short(text.text, 40)).Append('\'');
            return builder.ToString();
        }

        private static string Short(string value, int maximum)
        {
            if (value == null) return string.Empty;
            value = value.Replace("\n", "⏎");
            return value.Length <= maximum ? value : value.Substring(0, maximum) + "…";
        }

        private static string RectText(Rect bound, Rect origin)
        {
            return "(" + Num(bound.x - origin.x) + "," + Num(bound.y - origin.y) + " " + Num(bound.width) + "×" + Num(bound.height) + ")";
        }

        private static string RectJson(Rect rect)
        {
            return "{\"x\": " + Num(rect.x) + ", \"y\": " + Num(rect.y) + ", \"width\": " + Num(rect.width) + ", \"height\": " + Num(rect.height) + "}";
        }

        private static void Array(StringBuilder json, string name, List<string> values)
        {
            json.Append(",\n      \"").Append(name).Append("\": [");
            for (int index = 0; index < values.Count; index++)
            {
                json.Append(index == 0 ? "\n        " : ",\n        ").Append(Quote(values[index]));
            }
            json.Append(values.Count > 0 ? "\n      ]" : "]");
        }

        private static void Prop(StringBuilder json, string name, string value)
        {
            json.Append("  ").Append(Quote(name)).Append(": ").Append(Quote(value)).Append(",\n");
        }

        public static string Num(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? "0" : value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        public static string Quote(string value)
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
