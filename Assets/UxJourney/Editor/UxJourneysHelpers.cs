// MÃ DEV TẠM (G-UX-JOURNEY) — trợ giúp dùng chung cho các hành trình: điều hướng, ô nhập, menu (đọc + chạy mục), chờ.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using DreamTech.LiveOps.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UxJourney
{
    public static partial class UxJourneys
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly StringBuilder MenuLog = new StringBuilder();

        internal static VisualElement Root => Hub.rootVisualElement;

        internal static IEnumerator Snap(UxRunner runner, string id, string note, params EditorWindow[] extra)
        {
            return UxCapture.Step(runner, Hub, id, note, extra);
        }

        internal static string SectionTitle()
        {
            Label title = Root.Q<Label>("hub-section-title");
            return title == null ? string.Empty : title.text;
        }

        internal static void Note(UxRunner runner, string text)
        {
            runner.Log("GHI CHÚ: " + text);
            File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, "notes.txt"), runner.CurrentStep + " | " + text + "\n");
        }

        /// <summary>Đi tới màn như người dùng: bấm hàng rail nếu thấy; rail hẹp thì mở "Đi tới màn…" (⌘K), gõ tên, Enter.</summary>
        internal static IEnumerator NavigateTo(UxRunner runner, string title)
        {
            if (SectionTitle() == title) yield break;
            VisualElement railLabel = UxFind.First(Root, element => element is Label label && label.ClassListContains("liveops-hub-rail-row-label") &&
                                                                    label.text == title && UxFind.Shown(label));
            if (railLabel != null)
            {
                yield return UxInput.Click(Hub, railLabel);
                yield return new WaitFrames(6);
            }
            if (SectionTitle() != title)
            {
                VisualElement gotoChip = Root.Q("hub-header-goto");
                if (gotoChip != null && UxFind.Shown(gotoChip))
                {
                    yield return UxInput.Click(Hub, gotoChip);
                    yield return new WaitFrames(6);
                    yield return UxInput.TypeText(Hub, title);
                    yield return new WaitFrames(4);
                    yield return UxInput.PressKey(Hub, KeyCode.Return, EventModifiers.None, '\n');
                    yield return new WaitFrames(8);
                    Note(runner, "rail hẹp: đi tới '" + title + "' bằng bảng Đi tới màn (⌘K) → màn hiện: '" + SectionTitle() + "'");
                }
            }
            if (SectionTitle() != title)
            {
                Note(runner, "KHÔNG tới được màn '" + title + "' bằng giao diện (đang ở '" + SectionTitle() + "')");
            }
            yield return new WaitFrames(4);
        }

        /// <summary>Hàng field trong <paramref name="scope"/> có nhãn đúng <paramref name="label"/>: leo lên cha tới khi có ô nhập.</summary>
        internal static VisualElement FieldRow(VisualElement scope, string label)
        {
            VisualElement labelElement = UxFind.First(scope, element => element is Label text && text.text == label && UxFind.Shown(element));
            if (labelElement == null) return null;
            for (VisualElement current = labelElement.hierarchy.parent; current != null && current != scope; current = current.hierarchy.parent)
            {
                if (UxFind.First(current, element => element is TextInputBaseField<string> || element is TextInputBaseField<int> || element is DropdownField) != null)
                {
                    return current;
                }
            }
            return null;
        }

        internal static VisualElement InputOf(VisualElement field)
        {
            VisualElement input = field.Q(className: "unity-base-field__input");
            return input ?? field;
        }

        /// <summary>Bấm vào ô, chọn hết (⌘A), gõ chữ mới, tuỳ chọn Enter — đúng cách người dùng sửa một ô.</summary>
        internal static IEnumerator ReplaceText(EditorWindow window, VisualElement field, string text, bool pressEnter)
        {
            if (field == null) throw new InvalidOperationException("ReplaceText: ô null");
            VisualElement input = InputOf(field);
            yield return UxInput.Click(window, input);
            yield return new WaitFrames(2);
            yield return UxInput.PressKey(window, KeyCode.A, EventModifiers.Command, 'a');
            yield return UxInput.TypeText(window, text);
            if (pressEnter) yield return UxInput.PressKey(window, KeyCode.Return, EventModifiers.None, '\n');
            yield return new WaitFrames(6);
        }

        internal static IEnumerator ClickButtonText(UxRunner runner, EditorWindow window, string text, bool exact = true)
        {
            VisualElement target = UxFind.Clickable(window.rootVisualElement, text, exact);
            if (target == null)
            {
                Note(runner, "không thấy nút/chữ '" + text + "'");
                yield break;
            }
            if (!target.enabledInHierarchy) Note(runner, "nút '" + text + "' đang disabled");
            yield return UxInput.Click(window, target);
            yield return new WaitFrames(6);
        }

        // ------------------------------------------------------------------------------------------------ menu

        /// <summary>Đọc các mục của một ToolbarMenu (DropdownMenu) như menu sẽ hiện, ghi vào menus.txt.</summary>
        internal static List<DropdownMenuAction> ReadMenu(UxRunner runner, string title, DropdownMenu menu)
        {
            List<DropdownMenuAction> actions = new List<DropdownMenuAction>();
            StringBuilder builder = new StringBuilder();
            builder.Append("[").Append(runner.CurrentStep).Append("] ").Append(title).Append(":\n");
            foreach (DropdownMenuItem item in menu.MenuItems())
            {
                if (item is DropdownMenuAction action)
                {
                    try
                    {
                        action.UpdateActionStatus(null);
                    }
                    catch (Exception)
                    {
                    }
                    actions.Add(action);
                    builder.Append("   · ").Append(action.name).Append(" [").Append(action.status).Append("]\n");
                }
                else
                {
                    builder.Append("   ——\n");
                }
            }
            File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, "menus.txt"), builder.ToString());
            runner.Log("menu " + title + ": " + actions.Count + " mục");
            return actions;
        }

        internal static bool RunMenu(UxRunner runner, List<DropdownMenuAction> actions, string contains)
        {
            foreach (DropdownMenuAction action in actions)
            {
                if (action.name.Contains(contains))
                {
                    runner.Log("chọn mục menu '" + action.name + "'");
                    action.Execute();
                    return true;
                }
            }
            Note(runner, "menu không có mục chứa '" + contains + "'");
            return false;
        }

        internal static object CalendarSectionObject()
        {
            FieldInfo field = typeof(LiveOpsHubWindow).GetField("_sections", AnyInstance);
            System.Collections.IEnumerable sections = (System.Collections.IEnumerable)field.GetValue(Hub);
            foreach (object section in sections)
            {
                if (section.GetType().Name == "CalendarSection") return section;
            }
            return null;
        }

        /// <summary>
        /// Menu chuột phải của trục: ĐÚNG dữ liệu mà <c>CalendarSection.OnTimelineContextRequested</c> đưa cho GenericMenu (menu gốc
        /// NSMenu chặn luồng chính nên không bấm bằng chuột được trong phiên tự động). Trả danh sách mục; <paramref name="choose"/>
        /// không rỗng thì chạy mục đó qua chính hàm ActivateMenuItem của màn.
        /// </summary>
        internal static List<string> CalendarContextMenu(UxRunner runner, string title, Vector2 worldPoint, string choose)
        {
            object section = CalendarSectionObject();
            VisualElement timeline = (VisualElement)section.GetType().GetProperty("Timeline", AnyInstance).GetValue(section, null);
            Vector2 local = timeline.WorldToLocal(worldPoint);
            object hit = timeline.GetType().GetMethod("HitTest", AnyInstance).Invoke(timeline, new object[] { local });
            object context = section.GetType().GetMethod("BuildMenuContext", AnyInstance).Invoke(section, new[] { hit });
            MethodInfo itemsFor = section.GetType().GetMethod("ItemsFor", AnyInstance);
            System.Collections.IEnumerable items = (System.Collections.IEnumerable)itemsFor.Invoke(section, new[] { hit, context });
            List<string> texts = new List<string>();
            StringBuilder builder = new StringBuilder();
            builder.Append("[").Append(runner.CurrentStep).Append("] menu chuột phải ").Append(title).Append(" (hit ").Append(hit).Append("):\n");
            object chosenId = null;
            if (items != null)
            {
                foreach (object item in items)
                {
                    string text = item.ToString();
                    texts.Add(text);
                    builder.Append("   · ").Append(text).Append('\n');
                    string itemText = (string)item.GetType().GetProperty("Text").GetValue(item, null);
                    bool enabled = (bool)item.GetType().GetProperty("IsEnabled").GetValue(item, null);
                    if (!string.IsNullOrEmpty(choose) && chosenId == null && enabled && itemText.Contains(choose))
                    {
                        chosenId = item.GetType().GetProperty("Id").GetValue(item, null);
                    }
                }
            }
            else
            {
                builder.Append("   (không có menu)\n");
            }
            File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, "menus.txt"), builder.ToString());
            if (!string.IsNullOrEmpty(choose))
            {
                if (chosenId == null) Note(runner, "menu " + title + " không có mục bật chứa '" + choose + "'");
                else
                {
                    runner.Log("chạy mục menu '" + choose + "' (" + chosenId + ")");
                    section.GetType().GetMethod("ActivateMenuItem", AnyInstance).Invoke(section, new[] { chosenId, context });
                }
            }
            return texts;
        }

        internal static void ExecuteCommand(EditorWindow window, string command)
        {
            window.SendEvent(new Event { type = EventType.ValidateCommand, commandName = command });
            window.SendEvent(new Event { type = EventType.ExecuteCommand, commandName = command });
        }

        internal static IEnumerator Undo(UxRunner runner)
        {
            runner.Log("⌘Z (Edit/Undo): " + EditorApplication.ExecuteMenuItem("Edit/Undo"));
            yield return new WaitFrames(8);
        }

        internal static void DrainModalPlans(UxRunner runner)
        {
            while (runner.ModalPlans.TryDequeue(out string[] _))
            {
            }
            while (runner.ModalStems.TryDequeue(out string _))
            {
            }
        }

        internal static IEnumerator WaitModalClosed(UxRunner runner, double seconds)
        {
            yield return new WaitUntilOrTimeout(() => UxHub.ConfirmWindow() == null, seconds);
            yield return new WaitFrames(6);
            DrainModalPlans(runner);
        }

        internal static VisualElement Bar(string key)
        {
            return Root.Q(key);
        }

        internal static IEnumerator SelectBar(UxRunner runner, string key)
        {
            VisualElement bar = Bar(key);
            if (bar == null)
            {
                Note(runner, "không thấy thanh " + key);
                yield break;
            }
            Rect bound = bar.worldBound;
            // bấm giữa thân (tránh tay nắm 8px ở hai mép)
            yield return UxInput.ClickAt(Hub, new Vector2(bound.center.x, bound.center.y));
            yield return new WaitFrames(6);
        }

        internal static IEnumerator OpenPopoverFrom(UxRunner runner, VisualElement activator)
        {
            yield return UxInput.Click(Hub, activator);
            yield return new WaitUntilOrTimeout(() => UxHub.PopupWindow() != null, 4);
            yield return new WaitFrames(6);
        }
    }
}
