// MÃ DEV TẠM (G-UX-JOURNEY) — các hành trình người dùng.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DreamTech.LiveOps.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UxJourney
{
    public static partial class UxJourneys
    {
        private static readonly Dictionary<string, Func<UxRunner, IEnumerator>> Registry = new Dictionary<string, Func<UxRunner, IEnumerator>>();
        internal static IDisposable LanguageScope;
        internal static LiveOpsHubWindow Hub;

        static UxJourneys()
        {
            Registry["J0"] = Explore;
            RegisterAll(Registry);
        }

        static partial void RegisterAll(Dictionary<string, Func<UxRunner, IEnumerator>> registry);

        public static Func<UxRunner, IEnumerator> Find(string id)
        {
            return Registry.TryGetValue(id, out Func<UxRunner, IEnumerator> factory) ? factory : null;
        }

        public static void CleanupAfterJourney(UxRunner runner)
        {
            runner.NestedTick = null;
            UxHub.CloseAllHubWindows();
            Hub = null;
            if (LanguageScope != null)
            {
                LanguageScope.Dispose();
                LanguageScope = null;
            }
        }

        // ------------------------------------------------------------------------------------------------ dùng chung

        internal static IEnumerator OpenDesignHub(UxRunner runner, string sectionId, float width, float height, string language = "Vietnamese",
            DateTime? nowUtc = null)
        {
            UxHub.CloseAllHubWindows();
            yield return new WaitFrames(3);
            if (LanguageScope != null) LanguageScope.Dispose();
            LanguageScope = language == null ? null : UxHub.OverrideLanguage(language);
            LiveOpsHubServices services = UxHub.BuildDesignServices(nowUtc ?? UxHub.DesignNowUtc);
            Hub = UxHub.Open(services, sectionId);
            yield return new WaitFrames(2);
            UxHub.Place(Hub, 40, 80, width, height);
            yield return new WaitUntilOrTimeout(() => Hub != null && Hub.rootVisualElement.worldBound.width > 10, 10);
            yield return new WaitFrames(10);
            yield return new WaitSeconds(0.5);
            runner.Log("mở hub " + sectionId + " " + width + "×" + height + " ngôn ngữ " + UxHub.CurrentLanguage() + " position " + Hub.position);
        }

        internal static IEnumerator Resize(UxRunner runner, float width, float height)
        {
            UxHub.Place(Hub, 40, 80, width, height);
            yield return new WaitFrames(8);
            yield return new WaitSeconds(0.4);
            runner.Log("đổi cỡ " + width + "×" + height + " → position " + Hub.position + " root " + Hub.rootVisualElement.worldBound);
        }

        internal static string Size(float width, float height)
        {
            return ((int)width) + "x" + ((int)height);
        }

        internal static void DumpTree(UxRunner runner, VisualElement root, string fileName)
        {
            StringBuilder builder = new StringBuilder();
            DumpElement(builder, root, root.worldBound, 0);
            File.WriteAllText(Path.Combine(runner.Options.OutputDirectory, fileName), builder.ToString(), new UTF8Encoding(false));
        }

        private static void DumpElement(StringBuilder builder, VisualElement element, Rect origin, int depth)
        {
            if (element.resolvedStyle.display == DisplayStyle.None) return;
            Rect bound = element.worldBound;
            builder.Append(new string(' ', depth * 2)).Append(UxDiagnostics.Name(element)).Append(" (")
                .Append(UxDiagnostics.Num(bound.x - origin.x)).Append(',').Append(UxDiagnostics.Num(bound.y - origin.y)).Append(' ')
                .Append(UxDiagnostics.Num(bound.width)).Append('×').Append(UxDiagnostics.Num(bound.height)).Append(')');
            if (!string.IsNullOrEmpty(element.tooltip)) builder.Append(" tip='").Append(element.tooltip.Replace("\n", "⏎")).Append('\'');
            if (element.resolvedStyle.position == Position.Absolute) builder.Append(" [abs]");
            if (element.resolvedStyle.visibility == Visibility.Hidden) builder.Append(" [hidden]");
            if (!element.enabledInHierarchy) builder.Append(" [disabled]");
            if (element is TextField field) builder.Append(" value='").Append(field.value).Append('\'');
            if (element is BaseField<int> intField) builder.Append(" value=").Append(intField.value);
            builder.Append('\n');
            for (int index = 0; index < element.hierarchy.childCount; index++) DumpElement(builder, element.hierarchy[index], origin, depth + 1);
        }

        // ------------------------------------------------------------------------------------------------ J0 khám phá

        private static IEnumerator Explore(UxRunner runner)
        {
            runner.Log("chờ 12s ở nền để đo nhịp tick");
            yield return new WaitSeconds(12);
            yield return OpenDesignHub(runner, "calendar", 1280, 760);
            yield return UxCapture.Step(runner, Hub, "J0-01-calendar-1280x760", "khám phá: mở Lịch");
            DumpTree(runner, Hub.rootVisualElement, "J0-tree-calendar.txt");

            VisualElement bar = UxFind.Bar(Hub.rootVisualElement, "hunt-0916-bonus");
            runner.Log("bar hunt-0916-bonus = " + (bar == null ? "null" : bar.worldBound.ToString()));
            if (bar != null)
            {
                yield return UxInput.Click(Hub, bar);
                yield return new WaitFrames(10);
                yield return UxCapture.Step(runner, Hub, "J0-02-select-bar", "khám phá: bấm thanh hunt-0916-bonus");
                DumpTree(runner, Hub.rootVisualElement, "J0-tree-selected.txt");
            }

            // Menu chuột phải header làn: GenericMenu → menu hệ điều hành chặn luồng chính; watchdog chụp + Esc.
            VisualElement header = UxFind.LaneHeader(Hub.rootVisualElement, "lava-quest");
            runner.Log("lane header lava-quest = " + (header == null ? "null" : header.worldBound.ToString()));
            if (header != null)
            {
                Vector2 point = header.worldBound.center;
                runner.CurrentStep = "J0-03-context-menu";
                runner.Log("chuột phải header làn — bắt đầu " + DateTime.Now.ToString("HH:mm:ss.fff"));
                long ticksBefore = runner.TickCount;
                UxInput.MouseDown(Hub, point, 1);
                UxInput.MouseUp(Hub, point, 1);
                runner.Log("chuột phải header làn — trở về " + DateTime.Now.ToString("HH:mm:ss.fff") + " ticks trôi " + (runner.TickCount - ticksBefore));
                yield return new WaitFrames(10);
                yield return UxCapture.Step(runner, Hub, "J0-03-after-context-menu", "khám phá: sau menu chuột phải");
            }

            // Hộp modal: đợt đang chạy (đồng hồ 18/9 10:00), kéo mép cuối lava-quest-2026-09b sang trái 1 ngày.
            yield return OpenDesignHub(runner, "calendar", 1280, 760, "Vietnamese", new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc));
            yield return UxCapture.Step(runner, Hub, "J0-04-running-clock", "khám phá: đồng hồ 18/9 10:00");
            VisualElement running = UxFind.Bar(Hub.rootVisualElement, "lava-quest-2026-09b");
            runner.Log("bar lava-quest-2026-09b = " + (running == null ? "null" : running.worldBound.ToString()));
            if (running != null)
            {
                int nestedTicks = 0;
                bool captured = false;
                runner.NestedTick = () =>
                {
                    EditorWindow confirm = UxHub.ConfirmWindow();
                    if (confirm == null) return;
                    nestedTicks++;
                    if (!captured && nestedTicks > 30)
                    {
                        captured = true;
                        runner.Log("hộp xác nhận thấy trong tick lồng: " + confirm.position);
                        string grab = UxCapture.GrabWindow(confirm, Path.Combine(runner.Options.OutputDirectory, "J0-05-confirm-grab.png"));
                        string os = UxCapture.CaptureProcessWindows(runner, Path.Combine(runner.Options.OutputDirectory, "J0-05-confirm"), new List<Rect> { Hub.position, confirm.position });
                        DumpTree(runner, confirm.rootVisualElement, "J0-tree-confirm.txt");
                        runner.Log("grab " + grab + " os " + os);
                        Button safe = confirm.rootVisualElement.Q<Button>("confirm-safe");
                        if (safe != null)
                        {
                            Vector2 center = safe.worldBound.center;
                            confirm.SendEvent(new Event { type = EventType.MouseDown, mousePosition = center, button = 0, clickCount = 1 });
                            confirm.SendEvent(new Event { type = EventType.MouseUp, mousePosition = center, button = 0, clickCount = 1 });
                            runner.Log("đã bấm nút an toàn trong tick lồng");
                        }
                    }
                };
                Rect bound = running.worldBound;
                Vector2 from = new Vector2(bound.xMax - 2, bound.center.y);
                Vector2 to = new Vector2(bound.xMax - 2 - 30, bound.center.y);
                yield return UxInput.Drag(Hub, from, to, 10, EventModifiers.None, EventModifiers.None,
                    () => UxCapture.Step(runner, Hub, "J0-05a-drag-before-release", "kéo mép cuối, chưa nhả"));
                runner.Log("đã nhả chuột");
                yield return new WaitSeconds(2);
                yield return UxCapture.Step(runner, Hub, "J0-06-after-confirm", "sau hộp xác nhận");
                runner.NestedTick = null;
            }

            // Luật lặp
            VisualElement railRow = UxFind.Clickable(Hub.rootVisualElement, "Luật lặp", true);
            runner.Log("rail Luật lặp = " + (railRow == null ? "null" : UxDiagnostics.Name(railRow)));
            if (railRow != null)
            {
                yield return UxInput.Click(Hub, railRow);
                yield return new WaitFrames(10);
                yield return UxCapture.Step(runner, Hub, "J0-07-recurring", "Luật lặp");
                DumpTree(runner, Hub.rootVisualElement, "J0-tree-recurring.txt");
            }
        }
    }
}
