// MÃ DEV TẠM (G-UX-JOURNEY) — gửi sự kiện chuột/phím như người dùng thật qua EditorWindow.SendEvent, tìm element, mở hub.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DreamTech.LiveOps;
using DreamTech.LiveOps.Editor;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UxJourney
{
    public static class UxInput
    {
        /// <summary>
        /// Chuột THẬT của người đang dùng máy mà nằm trên cửa sổ của Unity này thì macOS gửi mouseMoved vào đó; UI Toolkit coi là
        /// PointerMove và cử chỉ kéo đang giữ capture sẽ nhảy theo. Trước mỗi cử chỉ: chờ tới khi con trỏ thật ra khỏi các cửa sổ
        /// của hub (tối đa 20s), ghi lại nếu vẫn nằm trong.
        /// </summary>
        public static IEnumerator WaitRealCursorAway(EditorWindow window)
        {
            UxRunner runner = UxJourneyDriver.Active;
            if (runner == null || window == null) yield break;
            double start = EditorApplication.timeSinceStartup;
            bool warned = false;
            while (true)
            {
                string[] parts = runner.Helper("cursor").Trim().Split(' ');
                float x, y;
                if (parts.Length < 2 || !float.TryParse(parts[0], out x) || !float.TryParse(parts[1], out y)) yield break;
                Rect guard = window.position;
                guard = new Rect(guard.x - 4, guard.y - 60, guard.width + 8, guard.height + 64);
                bool inside = guard.Contains(new Vector2(x, y));
                foreach (EditorWindow floating in UxCapture.AllFloatingWindows())
                {
                    if (floating != null && floating.position.Contains(new Vector2(x, y))) inside = true;
                }
                if (!inside)
                {
                    if (warned) runner.Log("con trỏ thật đã ra khỏi cửa sổ hub");
                    yield break;
                }
                if (!warned)
                {
                    runner.Log("con trỏ thật (" + x + "," + y + ") đang nằm trên cửa sổ hub — chờ trước khi thao tác");
                    warned = true;
                }
                if (EditorApplication.timeSinceStartup - start > 20)
                {
                    runner.Log("CẢNH BÁO: con trỏ thật vẫn nằm trên hub sau 20s — thao tác có thể bị chuột thật chen vào (" + runner.CurrentStep + ")");
                    File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, "cursor-warnings.txt"), runner.CurrentStep + "\n");
                    yield break;
                }
                yield return new WaitSeconds(0.5);
            }
        }

        public static void Send(EditorWindow window, Event e)
        {
            window.SendEvent(e);
        }

        public static void MouseMove(EditorWindow window, Vector2 position, EventModifiers modifiers = EventModifiers.None)
        {
            Send(window, new Event { type = EventType.MouseMove, mousePosition = position, modifiers = modifiers });
        }

        public static void MouseDown(EditorWindow window, Vector2 position, int button = 0, EventModifiers modifiers = EventModifiers.None, int clickCount = 1)
        {
            Send(window, new Event { type = EventType.MouseDown, mousePosition = position, button = button, modifiers = modifiers, clickCount = clickCount });
        }

        public static void MouseDrag(EditorWindow window, Vector2 position, Vector2 delta, int button = 0, EventModifiers modifiers = EventModifiers.None)
        {
            Send(window, new Event { type = EventType.MouseDrag, mousePosition = position, delta = delta, button = button, modifiers = modifiers });
        }

        public static void MouseUp(EditorWindow window, Vector2 position, int button = 0, EventModifiers modifiers = EventModifiers.None, int clickCount = 1)
        {
            Send(window, new Event { type = EventType.MouseUp, mousePosition = position, button = button, modifiers = modifiers, clickCount = clickCount });
        }

        public static void Wheel(EditorWindow window, Vector2 position, Vector2 delta, EventModifiers modifiers = EventModifiers.None)
        {
            Send(window, new Event { type = EventType.ScrollWheel, mousePosition = position, delta = delta, modifiers = modifiers });
        }

        public static void KeyDown(EditorWindow window, KeyCode keyCode, char character = '\0', EventModifiers modifiers = EventModifiers.None)
        {
            Send(window, new Event { type = EventType.KeyDown, keyCode = keyCode, character = character, modifiers = modifiers });
        }

        public static void KeyUp(EditorWindow window, KeyCode keyCode, EventModifiers modifiers = EventModifiers.None)
        {
            Send(window, new Event { type = EventType.KeyUp, keyCode = keyCode, modifiers = modifiers });
        }

        /// <summary>Một phím như bàn phím thật: KeyDown mang keyCode, KeyDown mang ký tự (IMGUI gửi hai lần), KeyUp.</summary>
        public static IEnumerator PressKey(EditorWindow window, KeyCode keyCode, EventModifiers modifiers = EventModifiers.None, char character = '\0')
        {
            KeyDown(window, keyCode, '\0', modifiers);
            if (character != '\0') KeyDown(window, KeyCode.None, character, modifiers);
            KeyUp(window, keyCode, modifiers);
            yield return new WaitFrames(2);
        }

        public static IEnumerator TypeText(EditorWindow window, string text)
        {
            foreach (char character in text)
            {
                KeyCode code = KeyCodeFor(character);
                KeyDown(window, code, '\0');
                KeyDown(window, KeyCode.None, character);
                KeyUp(window, code);
                yield return null;
            }
            yield return new WaitFrames(2);
        }

        private static KeyCode KeyCodeFor(char character)
        {
            if (character >= 'a' && character <= 'z') return KeyCode.A + (character - 'a');
            if (character >= 'A' && character <= 'Z') return KeyCode.A + (character - 'A');
            if (character >= '0' && character <= '9') return KeyCode.Alpha0 + (character - '0');
            switch (character)
            {
                case '-': return KeyCode.Minus;
                case ':': return KeyCode.Colon;
                case ' ': return KeyCode.Space;
                case '_': return KeyCode.Underscore;
                case '.': return KeyCode.Period;
                case '{': return KeyCode.LeftCurlyBracket;
                case '}': return KeyCode.RightCurlyBracket;
                case '"': return KeyCode.DoubleQuote;
                case ',': return KeyCode.Comma;
                default: return KeyCode.None;
            }
        }

        public static IEnumerator Click(EditorWindow window, VisualElement element, EventModifiers modifiers = EventModifiers.None, int button = 0)
        {
            if (element == null) throw new InvalidOperationException("Click: element null");
            Vector2 center = element.worldBound.center;
            yield return ClickAt(window, center, modifiers, button);
        }

        public static IEnumerator ClickAt(EditorWindow window, Vector2 position, EventModifiers modifiers = EventModifiers.None, int button = 0)
        {
            yield return WaitRealCursorAway(window);
            MouseMove(window, position, modifiers);
            yield return null;
            MouseDown(window, position, button, modifiers, 1);
            yield return null;
            MouseUp(window, position, button, modifiers, 1);
            yield return new WaitFrames(3);
        }

        public static IEnumerator DoubleClickAt(EditorWindow window, Vector2 position)
        {
            yield return WaitRealCursorAway(window);
            MouseMove(window, position);
            yield return null;
            MouseDown(window, position, 0, EventModifiers.None, 1);
            MouseUp(window, position, 0, EventModifiers.None, 1);
            yield return null;
            MouseDown(window, position, 0, EventModifiers.None, 2);
            MouseUp(window, position, 0, EventModifiers.None, 2);
            yield return new WaitFrames(3);
        }

        /// <summary>
        /// Kéo chuột: nhấn ở <paramref name="from"/>, nhiều bước trung gian (mỗi bước một frame), nhả ở <paramref name="to"/>.
        /// <paramref name="onBeforeRelease"/> chạy (và có thể chụp) khi chuột còn giữ ở điểm cuối.
        /// </summary>
        public static IEnumerator Drag(EditorWindow window, Vector2 from, Vector2 to, int steps, EventModifiers pressModifiers,
            EventModifiers moveModifiers, Func<IEnumerator> onBeforeRelease = null, bool release = true)
        {
            yield return WaitRealCursorAway(window);
            MouseMove(window, from, pressModifiers);
            yield return null;
            MouseDown(window, from, 0, pressModifiers, 1);
            yield return new WaitFrames(2);
            Vector2 previous = from;
            for (int step = 1; step <= steps; step++)
            {
                Vector2 point = Vector2.Lerp(from, to, step / (float)steps);
                MouseDrag(window, point, point - previous, 0, moveModifiers);
                previous = point;
                yield return null;
            }
            yield return new WaitFrames(3);
            if (onBeforeRelease != null) yield return onBeforeRelease();
            if (release)
            {
                MouseUp(window, to, 0, moveModifiers, 1);
                yield return new WaitFrames(3);
            }
        }
    }

    public static class UxFind
    {
        public static List<VisualElement> All(VisualElement root, Func<VisualElement, bool> predicate)
        {
            List<VisualElement> result = new List<VisualElement>();
            Walk(root, predicate, result);
            return result;
        }

        private static void Walk(VisualElement element, Func<VisualElement, bool> predicate, List<VisualElement> result)
        {
            if (element.resolvedStyle.display == DisplayStyle.None) return;
            if (predicate(element)) result.Add(element);
            for (int index = 0; index < element.hierarchy.childCount; index++) Walk(element.hierarchy[index], predicate, result);
        }

        public static VisualElement First(VisualElement root, Func<VisualElement, bool> predicate)
        {
            List<VisualElement> all = All(root, predicate);
            return all.Count > 0 ? all[0] : null;
        }

        public static bool Shown(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.hierarchy.parent)
            {
                if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.visibility == Visibility.Hidden) return false;
            }
            return element.worldBound.width > 0.5f && element.worldBound.height > 0.5f;
        }

        public static VisualElement ByClass(VisualElement root, string className)
        {
            return First(root, element => element.ClassListContains(className) && Shown(element));
        }

        public static List<VisualElement> AllByClass(VisualElement root, string className)
        {
            return All(root, element => element.ClassListContains(className) && Shown(element));
        }

        /// <summary>TextElement hiện (Label/Button/Toggle text…) có chữ đúng hoặc chứa <paramref name="text"/>.</summary>
        public static TextElement Text(VisualElement root, string text, bool exact = false)
        {
            return First(root, element => element is TextElement textElement && Shown(element) && textElement.text != null &&
                                          (exact ? textElement.text == text : textElement.text.Contains(text))) as TextElement;
        }

        /// <summary>Phần tử bấm được gần nhất chứa chữ: Button, ToolbarToggle, hoặc chính TextElement.</summary>
        public static VisualElement Clickable(VisualElement root, string text, bool exact = false)
        {
            TextElement label = Text(root, text, exact);
            if (label == null) return null;
            for (VisualElement current = label; current != null; current = current.hierarchy.parent)
            {
                if (current is Button || current is Toggle || current is UnityEditor.UIElements.ToolbarMenu) return current;
            }
            return label;
        }

        public static T Q<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name);
        }

        /// <summary>Thanh timeline có nhãn = id đợt (bar.Label.text).</summary>
        public static VisualElement Bar(VisualElement root, string eventId)
        {
            return First(root, element => element.ClassListContains("liveops-hub-timeline-bar") && Shown(element) &&
                                          HasChildText(element, eventId));
        }

        private static bool HasChildText(VisualElement element, string text)
        {
            for (int index = 0; index < element.hierarchy.childCount; index++)
            {
                if (element.hierarchy[index] is Label label && label.text != null && label.text.StartsWith(text, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public static VisualElement LaneHeader(VisualElement root, string typeId)
        {
            return First(root, element => element.ClassListContains("liveops-hub-timeline-lane-header") && Shown(element) &&
                                          All(element, child => child is Label label && label.text == typeId).Count > 0);
        }
    }

    public static class UxHub
    {
        public static readonly DateTime DesignNowUtc = new DateTime(2026, 9, 13, 8, 47, 0, DateTimeKind.Utc);
        private static readonly Assembly EditorAssembly = typeof(LiveOpsHubWindow).Assembly;
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static Type EditorType(string name)
        {
            return EditorAssembly.GetType("DreamTech.LiveOps.Editor." + name, true);
        }

        public static IDisposable OverrideLanguage(string language)
        {
            Type languageType = EditorType("LiveOpsHubLanguage");
            Type idType = EditorType("LiveOpsHubLanguageId");
            MethodInfo method = languageType.GetMethod("Override", AnyStatic);
            return (IDisposable)method.Invoke(null, new[] { Enum.Parse(idType, language) });
        }

        public static string CurrentLanguage()
        {
            Type languageType = EditorType("LiveOpsHubLanguage");
            return languageType.GetProperty("Current", AnyStatic).GetValue(null, null).ToString();
        }

        /// <summary>
        /// Services như kịch bản chụp h01 (tài liệu mẫu 13/9/2026 08:47, +7, kiểm xong) NHƯNG hộp xác nhận là modal THẬT (mặc định
        /// của builder) — người dùng thật thấy hộp, không phải presenter kịch bản của test.
        /// </summary>
        public static LiveOpsHubServices BuildDesignServices(DateTime nowUtc)
        {
            LiveEventCalendarAsset asset = (LiveEventCalendarAsset)EditorType("LiveOpsHubPreviewSample").GetMethod("CreateAsset", AnyStatic).Invoke(null, null);
            asset.name = "Main";
            LiveOpsHubServices services = new LiveOpsHubServicesBuilder()
                .WithClock(new ManualLiveOpsClock(nowUtc, true))
                .WithTimeZone(new ManualLiveOpsHubTimeZone(TimeSpan.FromHours(7)))
                .WithClipboard(new InMemoryLiveOpsHubClipboard())
                .WithCalendarAsset(asset)
                .WithAutoCheckOnOpen(false)
                .Build();
            object session = UxDiagnostics.FindProperty(typeof(LiveOpsHubServices), "Session").GetValue(services, null);
            session.GetType().GetMethod("RunCheckToCompletion", AnyInstance).Invoke(session, null);
            return services;
        }

        public static object Session(LiveOpsHubServices services)
        {
            return UxDiagnostics.FindProperty(typeof(LiveOpsHubServices), "Session").GetValue(services, null);
        }

        public static LiveOpsHubWindow Open(LiveOpsHubServices services, string sectionId)
        {
            MethodInfo method = typeof(LiveOpsHubWindow).GetMethod("OpenWithServices", AnyStatic, null,
                new[] { typeof(LiveOpsHubServices), typeof(string) }, null);
            return (LiveOpsHubWindow)method.Invoke(null, new object[] { services, sectionId });
        }

        public static void SetClock(LiveOpsHubServices services, DateTime nowUtc)
        {
            ((ManualLiveOpsClock)services.Clock).Set(nowUtc);
        }

        public static void Place(EditorWindow window, float x, float y, float width, float height)
        {
            window.position = new Rect(x, y, width, height);
            window.Focus();
        }

        public static EditorWindow ConfirmWindow()
        {
            Type type = EditorType("LiveOpsConfirmWindow");
            foreach (UnityEngine.Object found in Resources.FindObjectsOfTypeAll(type))
            {
                if (found is EditorWindow window && window != null) return window;
            }
            return null;
        }

        public static EditorWindow PopupWindow()
        {
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window != null && window.GetType().FullName == "UnityEditor.PopupWindow") return window;
            }
            return null;
        }

        public static void CloseAllHubWindows()
        {
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null) continue;
                string typeName = window.GetType().FullName ?? string.Empty;
                if (typeName.StartsWith("DreamTech.LiveOps", StringComparison.Ordinal) || typeName == "UnityEditor.PopupWindow")
                {
                    try
                    {
                        window.Close();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }
}
