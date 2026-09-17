// MÃ DEV TẠM (G-UX-JOURNEY) — hành trình bổ sung để xác nhận các nghi vấn từ lượt đầu.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UxJourney
{
    public static partial class UxJourneys
    {
        static partial void RegisterExtra(Dictionary<string, Func<UxRunner, IEnumerator>> registry)
        {
            registry["J1b"] = J1b;
            registry["J2b"] = J2b;
            registry["J7b"] = J7b;
            registry["J5b"] = J5b;
        }

        /// <summary>
        /// Nhịp update của Editor bỏ qua delayCall sau chuỗi chuột giả (trạng thái nút chuột gốc không được xoá) — gọi tay đúng hàm
        /// mà vòng update gọi ở tick kế, để hộp xác nhận "lúc thả" hiện như người dùng thật sẽ thấy.
        /// </summary>
        internal static void ForceDelayCalls(UxRunner runner)
        {
            MethodInfo method = typeof(EditorApplication).GetMethod("Internal_CallDelayFunctions", BindingFlags.Static | BindingFlags.NonPublic);
            runner.Log("gọi tay EditorApplication.Internal_CallDelayFunctions: " + (method != null));
            if (method != null) method.Invoke(null, null);
        }

        // J1b: timeline có tự vừa bề rộng khi MỞ ở cỡ lớn? bấm lại tab zoom có vừa lại không?
        private static IEnumerator J1b(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 1920, 1040);
            yield return PlaceSized(runner, 1920, 1040);
            yield return Snap(runner, "J1b-01-mo-moi-1920x1040", "mở hub mới ngay ở 1920×1040 (không đổi cỡ sau khi mở)");
            yield return ClickButtonText(runner, Hub, "3 tuần");
            yield return Snap(runner, "J1b-02-bam-tab-3tuan-1920", "bấm tab '3 tuần' ở 1920");
            yield return PlaceSized(runner, 1280, 760);
            yield return Snap(runner, "J1b-03-thu-ve-1280", "thu về 1280×760 (sau khi đã vừa 1920)");
            yield return ClickButtonText(runner, Hub, "Ngày");
            yield return ClickButtonText(runner, Hub, "3 tuần");
            yield return Snap(runner, "J1b-04-bam-lai-tab-1280", "bấm Ngày rồi 3 tuần ở 1280");
            // Sau khi zoom bằng ⌘+lăn, bấm vào một thanh: thanh vẽ ở đâu và chọn trúng gì?
            VisualElement track = UxFind.ByClass(Root, "liveops-hub-timeline-ruler-track");
            Vector2 pointer = new Vector2(track.worldBound.xMin + track.worldBound.width * 0.4f, Bar(HuntEarly).worldBound.center.y);
            yield return UxInput.WaitRealCursorAway(Hub);
            for (int notch = 0; notch < 5; notch++)
            {
                UxInput.Wheel(Hub, pointer, new Vector2(0, -3), EventModifiers.Command);
                yield return new WaitFrames(3);
            }
            yield return Snap(runner, "J1b-05-sau-zoom-lan", "⌘ + lăn 5 nấc");
            yield return SelectBar(runner, HuntEarly);
            yield return Snap(runner, "J1b-06-bam-thanh-sau-zoom", "bấm thanh hunt-0914 (vị trí vẽ) sau khi zoom bằng lăn");
            VisualElement inspectorTitle = Root.Q("calendar-inspector-title");
            Note(runner, "sau zoom lăn + bấm hunt-0914: tiêu đề inspector = '" + (inspectorTitle == null ? "" : string.Join(" ", UxFind.All(inspectorTitle, e => e is TextElement).ConvertAll(e => ((TextElement)e).text))) + "'");
        }

        // J2b: sửa giờ bắt đầu trong inspector — Enter / Tab / bấm ra ngoài
        private static IEnumerator J2b(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 1280, 760);
            yield return SelectBar(runner, HuntBonus);
            string[] modes = { "enter", "tab", "click-out" };
            string[] values = { "18:00", "20:00", "06:00" };
            for (int index = 0; index < modes.Length; index++)
            {
                VisualElement inspector = Root.Q("calendar-inspector");
                VisualElement startRow = FieldRow(inspector, "Bắt đầu");
                TextField startTime = startRow == null ? null : startRow.Q<TextField>(className: "liveops-hub-utc-field__time");
                if (startTime == null)
                {
                    Note(runner, "J2b: không thấy ô giờ Bắt đầu");
                    yield break;
                }
                yield return ReplaceText(Hub, startTime, values[index], false);
                if (modes[index] == "enter") yield return UxInput.PressKey(Hub, KeyCode.Return, EventModifiers.None, '\n');
                if (modes[index] == "tab") yield return UxInput.PressKey(Hub, KeyCode.Tab, EventModifiers.None, '\t');
                if (modes[index] == "click-out")
                {
                    VisualElement legend = UxFind.ByClass(Root, "liveops-hub-timeline-hint");
                    if (legend != null) yield return UxInput.Click(Hub, legend);
                }
                yield return new WaitFrames(10);
                yield return new WaitSeconds(0.8);
                inspector = Root.Q("calendar-inspector");
                startRow = FieldRow(inspector, "Bắt đầu");
                TextField after = startRow == null ? null : startRow.Q<TextField>(className: "liveops-hub-utc-field__time");
                Note(runner, "J2b " + modes[index] + ": gõ " + values[index] + " → ô giờ sau đó = '" + (after == null ? "?" : after.value) + "', tiêu đề cửa sổ '" + Hub.titleContent.text + "'");
                yield return Snap(runner, "J2b-0" + (index + 1) + "-gio-bat-dau-" + modes[index], "gõ giờ bắt đầu " + values[index] + " rồi " + modes[index]);
            }
        }

        // J5b: popover Thêm đợt — hub là cửa sổ key, Unity đứng trước; chụp thêm GrabPixels của chính popover
        private static IEnumerator J5b(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 1280, 760);
            yield return UxOs.Activate(runner);
            Hub.Focus();
            yield return new WaitSeconds(1.2);
            VisualElement add = UxFind.Clickable(Root, "Thêm đợt", true);
            yield return UxInput.Click(Hub, add);
            yield return new WaitUntilOrTimeout(() => UxHub.PopupWindow() != null, 4);
            EditorWindow popup = UxHub.PopupWindow();
            Note(runner, "J5b: popover sau khi bấm Thêm đợt = " + (popup == null ? "không có" : popup.position.ToString() + " hasFocus=" + popup.hasFocus));
            if (popup != null)
            {
                popup.Focus();
                yield return new WaitSeconds(0.8);
                string stem = System.IO.Path.Combine(runner.Options.OutputDirectory, "J5b-01-popover");
                runner.Log("grab popover: " + UxCapture.GrabWindow(popup, stem + "-popover-grab.png"));
                yield return Snap(runner, "J5b-01-popover", "popover Thêm đợt (hub key + Unity đứng trước)", popup);
                if (UxHub.PopupWindow() != null)
                {
                    popup = UxHub.PopupWindow();
                    yield return UxInput.TypeText(popup, "hunt");
                    yield return UxInput.PressKey(popup, KeyCode.Return, EventModifiers.None, '\n');
                    yield return new WaitSeconds(0.6);
                    popup = UxHub.PopupWindow();
                    Note(runner, "J5b: sau gõ 'hunt' + Enter popover = " + (popup == null ? "đã đóng" : "còn"));
                    if (popup != null)
                    {
                        runner.Log("grab popover b2: " + UxCapture.GrabWindow(popup, System.IO.Path.Combine(runner.Options.OutputDirectory, "J5b-02-buoc2-popover-grab.png")));
                        yield return Snap(runner, "J5b-02-buoc2", "bước 2", popup);
                        TextField date = popup.rootVisualElement.Q<TextField>(className: "liveops-hub-utc-field__date");
                        if (date != null) yield return ReplaceText(popup, date, "2026-09-15", true);
                        yield return new WaitSeconds(0.6);
                        popup = UxHub.PopupWindow();
                        if (popup != null)
                        {
                            yield return UxInput.PressKey(popup, KeyCode.Return, EventModifiers.None, '\n');
                            yield return new WaitSeconds(0.6);
                            popup = UxHub.PopupWindow();
                            if (popup != null)
                            {
                                runner.Log("grab popover b3: " + UxCapture.GrabWindow(popup, System.IO.Path.Combine(runner.Options.OutputDirectory, "J5b-03-buoc3-popover-grab.png")));
                                DumpTree(runner, popup.rootVisualElement, "J5b-tree-step3.txt");
                                yield return Snap(runner, "J5b-03-buoc3", "bước 3 (15/9, chồng giờ)", popup);
                            }
                        }
                    }
                }
            }
            UxOs.Release(runner);
        }

        // J7b: đọc menu chuột phải TRƯỚC khi đổi làn; bấm chip "Hiện" sau khi ẩn
        private static IEnumerator J7b(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 1280, 760);
            runner.CurrentStep = "J7b-01";
            CalendarContextMenu(runner, "thanh cố định lava-quest-2026-09b", Bar(LavaMid).worldBound.center, null);
            CalendarContextMenu(runner, "thanh cố định hunt-0916-bonus", Bar(HuntBonus).worldBound.center, null);
            VisualElement recurringBar = UxFind.First(Root, element => element.ClassListContains("liveops-hub-timeline-bar--recurring") && element.name.StartsWith("weekly-pass#") && UxFind.Shown(element));
            if (recurringBar != null) CalendarContextMenu(runner, "thanh lặp " + recurringBar.name, recurringBar.worldBound.center, null);
            VisualElement header = UxFind.LaneHeader(Root, "weekly-pass");
            CalendarContextMenu(runner, "header làn weekly-pass (Thu gọn)", header.worldBound.center, "Thu gọn");
            yield return new WaitFrames(8);
            yield return Snap(runner, "J7b-01-thu-gon-bang-menu", "menu chuột phải header weekly-pass → Thu gọn làn");
            CalendarContextMenu(runner, "header làn lava-quest (Ẩn)", UxFind.LaneHeader(Root, "lava-quest").worldBound.center, "Ẩn");
            yield return new WaitFrames(8);
            yield return Snap(runner, "J7b-02-an-lan", "menu → Ẩn làn lava-quest");
            VisualElement chip = UxFind.ByClass(Root, "liveops-hub-calendar-hidden-lanes-chip");
            if (chip != null)
            {
                runner.Log("chip làn ẩn: " + UxDiagnostics.Name(chip) + " " + chip.worldBound + " picking=" + chip.pickingMode);
                DumpTree(runner, chip, "J7b-tree-chip.txt");
                VisualElement show = UxFind.First(chip, element => element is TextElement text && text.text != null && text.text.Contains("Hiện")) ?? chip;
                yield return UxInput.Click(Hub, show);
                yield return new WaitFrames(8);
                yield return Snap(runner, "J7b-03-bam-hien", "bấm chữ 'Hiện' trên chip làn ẩn");
            }
        }
    }
}
