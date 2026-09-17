// MÃ DEV TẠM (G-UX-JOURNEY) — probe: hộp modal, popover, menu gốc.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UxJourney
{
    public static partial class UxJourneys
    {
        internal static VisualElement BarByKey(string barName)
        {
            return Hub.rootVisualElement.Q(barName);
        }

        internal static IEnumerator ProbeModalPopoverMenu(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 1280, 760, "Vietnamese", new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc));
            VisualElement running = BarByKey("entry-lava-quest-2026-09b");
            runner.Log("bar 09b = " + (running == null ? "null" : running.worldBound.ToString()));
            Rect bound = running.worldBound;
            Vector2 from = new Vector2(bound.xMax - 2, bound.center.y);
            Vector2 to = new Vector2(bound.xMax - 32, bound.center.y);
            long ticks = runner.TickCount;
            yield return UxInput.Drag(Hub, from, to, 8, EventModifiers.None, EventModifiers.None);
            runner.Log("đã nhả; chờ hộp xác nhận");
            UxOs.ScheduleKeys(runner, Path.Combine(runner.Options.OutputDirectory, "J0b-confirm-watch"), 4000, new string[0], "theo dõi hộp");
            yield return new WaitUntilOrTimeout(() => UxHub.ConfirmWindow() != null, 15);
            EditorWindow confirm = UxHub.ConfirmWindow();
            runner.Log("confirm = " + (confirm == null ? "null" : confirm.position.ToString()) + " tick trôi " + (runner.TickCount - ticks));
            if (confirm != null)
            {
                long before = runner.TickCount;
                yield return new WaitSeconds(1.5);
                runner.Log("tick trong 1.5s khi hộp mở: " + (runner.TickCount - before));
                yield return UxCapture.Step(runner, Hub, "J0b-01-confirm", "hộp xác nhận rút ngắn", confirm);
                Button safe = confirm.rootVisualElement.Q<Button>("confirm-safe");
                yield return UxInput.Click(confirm, safe);
                yield return new WaitUntilOrTimeout(() => UxHub.ConfirmWindow() == null, 5);
                runner.Log("confirm sau bấm an toàn = " + (UxHub.ConfirmWindow() == null ? "đã đóng" : "còn mở"));
            }
            yield return UxCapture.Step(runner, Hub, "J0b-02-after-confirm", "sau hộp");

            VisualElement add = UxFind.Clickable(Hub.rootVisualElement, "Thêm đợt", true);
            yield return UxInput.Click(Hub, add);
            yield return new WaitSeconds(2);
            EditorWindow popup = UxHub.PopupWindow();
            runner.Log("popup (nền) = " + (popup == null ? "null" : popup.position.ToString()));
            if (popup == null)
            {
                yield return UxOs.Activate(runner);
                yield return UxInput.Click(Hub, add);
                yield return new WaitSeconds(1.5);
                popup = UxHub.PopupWindow();
                runner.Log("popup (có focus) = " + (popup == null ? "null" : popup.position.ToString()));
                UxOs.Release(runner);
            }
            if (popup != null)
            {
                yield return UxCapture.Step(runner, Hub, "J0b-03-popover", "popover", popup);
                DumpTree(runner, popup.rootVisualElement, "J0b-tree-popover.txt");
                yield return UxInput.PressKey(popup, KeyCode.Escape, EventModifiers.None, (char)27);
                yield return new WaitSeconds(1);
                runner.Log("popup sau Esc = " + (UxHub.PopupWindow() == null ? "đã đóng" : "còn"));
            }

            yield return UxOs.Activate(runner);
            running = BarByKey("entry-lava-quest-2026-09b");
            UxOs.ScheduleKeys(runner, Path.Combine(runner.Options.OutputDirectory, "J0b-04-context-menu"), 1500, new[] { "key code 53" }, "menu chuột phải thanh");
            runner.Log("chuột phải thanh 09b");
            UxInput.MouseDown(Hub, running.worldBound.center, 1);
            runner.Log("MouseDown phải trở về");
            UxInput.MouseUp(Hub, running.worldBound.center, 1);
            yield return new WaitSeconds(3);
            UxOs.Release(runner);
            yield return UxCapture.Step(runner, Hub, "J0b-05-after-menu", "sau menu");
        }

        static partial void RegisterAll(Dictionary<string, Func<UxRunner, IEnumerator>> registry)
        {
            registry["J0b"] = ProbeModalPopoverMenu;
            RegisterJourneys(registry);
        }

        static partial void RegisterJourneys(Dictionary<string, Func<UxRunner, IEnumerator>> registry);
    }
}
