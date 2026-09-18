using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Bộ gửi SỰ KIỆN THẬT của cổng máy W8-UX (UX-FIX-PLAN §3.1): mọi thao tác đi qua <see cref="EditorWindow.SendEvent"/> của cửa
    /// sổ đích — đường IMGUI → UI Toolkit giống hệt chuột/bàn phím của người dùng.
    /// <para>
    /// Vì sao không gọi hàm model/presenter hay <c>panel.visualTree.SendEvent(KeyDownEvent)</c>: 26 lỗi của USER-JOURNEY-FINDINGS
    /// lọt qua 900 test xanh chính vì test gọi thẳng hàm đích — handler thật (ô giờ chỉ nghe <c>RawTextCommitted</c>, chevron
    /// <c>PickingMode.Ignore</c>, chip là Label) không bao giờ được chạm. Ở 2022.3 phím gửi thẳng vào panel còn đi đường dispatch
    /// khác với phím thật (ghi chú của <c>CalendarToolbar</c>), nên chỉ <c>EditorWindow.SendEvent</c> cho cùng kết quả hai bản.
    /// </para>
    /// <para>
    /// Kéo luôn có ≥ <see cref="MinimumDragSteps"/> bước <c>MouseDrag</c> trung gian: một cú nhảy thẳng từ Down tới Up không đi qua
    /// ngưỡng bắt đầu kéo, không bật readout/hover — tức bỏ qua đúng chỗ UX-05/UX-14 hỏng. Sau mỗi bước chờ theo CẢ khung lẫn giờ
    /// thật (V-23): batch chạy 60 khung trong vài chục ms, chờ theo khung thôi thì hẹn giờ của hover card (500 ms) chưa bao giờ tới.
    /// </para>
    /// </summary>
    // Category ở đây là dấu cho code-lint (luật test-ui-category) và cho người đọc: lớp trợ giúp này chỉ chạy được khi
    // Unity CÓ đồ hoạ — panel/SendEvent/layout đều vô nghĩa dưới -nographics.
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static class UxEventSender
    {
        /// <summary>Số bước MouseDrag trung gian tối thiểu của một cú kéo (§3.1).</summary>
        internal const int MinimumDragSteps = 4;

        /// <summary>Khung chờ sau một thao tác chuột/phím để handler, GeometryChanged và lượt vẽ lại chạy xong.</summary>
        internal const int SettleFrames = 3;

        /// <summary>Giờ thật chờ thêm sau một thao tác — đủ cho <c>schedule.Execute</c> 0 ms và một nhịp update của Editor.</summary>
        internal const int SettleMilliseconds = 40;

        /// <summary>Trần chờ một điều kiện: vượt CẢ số khung lẫn số giây mới fail (V-23).</summary>
        internal const int MaximumWaitFrames = 60;

        internal const int MaximumWaitMilliseconds = 5000;

        /// <summary>Hai lần bấm của nhấp đúp cách nhau (ms) — dưới ngưỡng nhấp đúp của macOS/Windows.</summary>
        private const int DoubleClickGapMilliseconds = 30;

        private const string DelayCallPumpMethodName = "Internal_CallDelayFunctions";

        private static MethodInfo _delayCallPump;

        // ================================================================================================ chuột

        internal static void MouseMove(EditorWindow window, Vector2 position, EventModifiers modifiers)
        {
            Send(window, new Event { type = EventType.MouseMove, mousePosition = position, modifiers = modifiers });
        }

        internal static void MouseDown(EditorWindow window, Vector2 position, int button, EventModifiers modifiers, int clickCount)
        {
            Send(window, new Event
            {
                type = EventType.MouseDown, mousePosition = position, button = button, modifiers = modifiers, clickCount = clickCount,
            });
        }

        internal static void MouseDrag(EditorWindow window, Vector2 position, Vector2 delta, int button, EventModifiers modifiers)
        {
            Send(window, new Event
            {
                type = EventType.MouseDrag, mousePosition = position, delta = delta, button = button, modifiers = modifiers,
            });
        }

        internal static void MouseUp(EditorWindow window, Vector2 position, int button, EventModifiers modifiers, int clickCount)
        {
            Send(window, new Event
            {
                type = EventType.MouseUp, mousePosition = position, button = button, modifiers = modifiers, clickCount = clickCount,
            });
        }

        /// <summary>Rê chuột tới <paramref name="position"/> rồi đứng yên <paramref name="milliseconds"/> (hover card, bubble giờ).</summary>
        internal static IEnumerator HoverAt(EditorWindow window, Vector2 position, int milliseconds)
        {
            MouseMove(window, position, EventModifiers.None);
            yield return Settle(SettleFrames, SettleMilliseconds);
            // Nhích 1 px: UI Toolkit chỉ phát PointerEnter khi phần tử dưới con trỏ ĐỔI giữa hai lần di chuột.
            MouseMove(window, position + Vector2.right, EventModifiers.None);
            yield return Settle(SettleFrames, milliseconds);
        }

        /// <summary>Bấm tâm <c>worldBound</c> của element: Move → Down → Up như cú bấm thật.</summary>
        internal static IEnumerator Click(EditorWindow window, VisualElement element, EventModifiers modifiers = EventModifiers.None)
        {
            if (element == null) throw new ArgumentNullException(nameof(element), "UxEventSender.Click: element cần bấm không có trong cây");
            Assert.IsTrue(IsPointInsideWindow(window, element.worldBound.center),
                "phần tử '" + UxLayoutAuditor.Describe(element) + "' nằm ngoài cửa sổ (" + element.worldBound + ") — người dùng không bấm được");
            yield return ClickAt(window, element.worldBound.center, modifiers);
        }

        internal static IEnumerator ClickAt(EditorWindow window, Vector2 position, EventModifiers modifiers = EventModifiers.None)
        {
            MouseMove(window, position, modifiers);
            yield return null;
            MouseDown(window, position, 0, modifiers, 1);
            yield return null;
            MouseUp(window, position, 0, modifiers, 1);
            yield return Settle(SettleFrames, SettleMilliseconds);
        }

        internal static IEnumerator DoubleClickAt(EditorWindow window, Vector2 position)
        {
            MouseMove(window, position, EventModifiers.None);
            yield return null;
            MouseDown(window, position, 0, EventModifiers.None, 1);
            MouseUp(window, position, 0, EventModifiers.None, 1);
            yield return Settle(1, DoubleClickGapMilliseconds);
            MouseDown(window, position, 0, EventModifiers.None, 2);
            MouseUp(window, position, 0, EventModifiers.None, 2);
            yield return Settle(SettleFrames, SettleMilliseconds);
        }

        /// <summary>
        /// Kéo: Move → Down → <paramref name="steps"/> bước MouseDrag (mỗi bước một khung) → (tuỳ chọn) chạy
        /// <paramref name="whileHolding"/> khi còn giữ chuột ở điểm cuối → Up. <paramref name="release"/> = false để test đọc trạng
        /// thái giữa chừng rồi tự nhả bằng <see cref="MouseUp"/>.
        /// </summary>
        internal static IEnumerator Drag(EditorWindow window, Vector2 from, Vector2 to, int steps, EventModifiers modifiers,
            Func<IEnumerator> whileHolding = null, bool release = true)
        {
            int stepCount = Math.Max(MinimumDragSteps, steps);
            MouseMove(window, from, modifiers);
            yield return null;
            MouseDown(window, from, 0, modifiers, 1);
            yield return Settle(SettleFrames, SettleMilliseconds);
            Vector2 previous = from;
            for (int step = 1; step <= stepCount; step++)
            {
                Vector2 point = Vector2.Lerp(from, to, step / (float)stepCount);
                MouseDrag(window, point, point - previous, 0, modifiers);
                previous = point;
                yield return null;
            }
            yield return Settle(SettleFrames, SettleMilliseconds);
            if (whileHolding != null) yield return whileHolding();
            if (!release) yield break;
            MouseUp(window, to, 0, modifiers, 1);
            yield return Settle(SettleFrames, SettleMilliseconds);
        }

        /// <summary>
        /// Lăn chuột <paramref name="notches"/> nấc (âm = lăn lên) tại <paramref name="position"/>. Mỗi nấc là một sự kiện riêng như
        /// bánh xe thật, delta theo đúng hằng nấc của timeline để số nấc test viết ra khớp số nấc control hiểu.
        /// </summary>
        internal static IEnumerator Wheel(EditorWindow window, Vector2 position, int notches, EventModifiers modifiers)
        {
            MouseMove(window, position, modifiers);
            yield return null;
            int direction = notches < 0 ? -1 : 1;
            for (int notch = 0; notch < Math.Abs(notches); notch++)
            {
                Send(window, new Event
                {
                    type = EventType.ScrollWheel, mousePosition = position,
                    delta = new Vector2(0f, direction * LiveOpsTimelineElement.WheelDeltaPerNotch), modifiers = modifiers,
                });
                yield return Settle(1, SettleMilliseconds);
            }
            yield return Settle(SettleFrames, SettleMilliseconds);
        }

        // ================================================================================================ phím

        /// <summary>Một phím như bàn phím thật: KeyDown mang keyCode, KeyDown mang ký tự (IMGUI tách hai lần), KeyUp.</summary>
        internal static IEnumerator PressKey(EditorWindow window, KeyCode keyCode, char character = '\0',
            EventModifiers modifiers = EventModifiers.None)
        {
            Send(window, new Event { type = EventType.KeyDown, keyCode = keyCode, character = '\0', modifiers = modifiers });
            if (character != '\0')
            {
                Send(window, new Event { type = EventType.KeyDown, keyCode = KeyCode.None, character = character, modifiers = modifiers });
            }
            Send(window, new Event { type = EventType.KeyUp, keyCode = keyCode, modifiers = modifiers });
            yield return Settle(SettleFrames, SettleMilliseconds);
        }

        internal static IEnumerator PressEnter(EditorWindow window)
        {
            yield return PressKey(window, KeyCode.Return, '\n');
        }

        internal static IEnumerator PressTab(EditorWindow window)
        {
            yield return PressKey(window, KeyCode.Tab, '\t');
        }

        internal static IEnumerator PressEscape(EditorWindow window)
        {
            yield return PressKey(window, KeyCode.Escape, (char)27);
        }

        /// <summary>Gõ từng ký tự như người thật — không gán <c>value</c> (gán thẳng bỏ qua đúng handler đang được kiểm).</summary>
        internal static IEnumerator TypeText(EditorWindow window, string text)
        {
            foreach (char character in text ?? string.Empty)
            {
                KeyCode code = KeyCodeOf(character);
                Send(window, new Event { type = EventType.KeyDown, keyCode = code, character = '\0' });
                Send(window, new Event { type = EventType.KeyDown, keyCode = KeyCode.None, character = character });
                Send(window, new Event { type = EventType.KeyUp, keyCode = code });
                yield return null;
            }
            yield return Settle(SettleFrames, SettleMilliseconds);
        }

        /// <summary>
        /// Sửa một ô như người dùng: bấm vào ô nhập (focus), chọn hết bằng phím tắt của hệ điều hành, gõ chữ mới. Không nhấn
        /// Enter — test chọn cách rời ô (Enter / Tab / bấm ra ngoài) vì UX-04 hỏng khác nhau ở từng cách.
        /// </summary>
        internal static IEnumerator ReplaceText(EditorWindow window, VisualElement field, string text)
        {
            if (field == null) throw new ArgumentNullException(nameof(field), "UxEventSender.ReplaceText: không thấy ô cần gõ");
            VisualElement input = field.Q(className: "unity-base-field__input") ?? field;
            yield return Click(window, input);
            yield return PressKey(window, KeyCode.A, 'a', SelectAllModifier);
            yield return TypeText(window, text);
        }

        /// <summary>Phím tắt "chọn hết" của máy đang chạy: ⌘ trên macOS, Ctrl ở nơi khác.</summary>
        internal static EventModifiers SelectAllModifier =>
            Application.platform == RuntimePlatform.OSXEditor ? EventModifiers.Command : EventModifiers.Control;

        /// <summary>Phím "hành động" của timeline (⌘ trên macOS, Ctrl ở nơi khác) — cùng nghĩa với <c>WheelEvent.actionKey</c>.</summary>
        internal static EventModifiers ActionModifier => SelectAllModifier;

        /// <summary>
        /// Chạy một mục menu của Editor bằng đúng đường menu (⌘Z = <c>Edit/Undo</c>). Phím tắt toàn cục của Unity không đi qua
        /// <c>EditorWindow.SendEvent</c>, nên đây là đường gần người dùng nhất mà phiên tự động có.
        /// </summary>
        internal static IEnumerator ExecuteMenuItem(string menuPath)
        {
            Assert.IsTrue(EditorApplication.ExecuteMenuItem(menuPath), "không chạy được mục menu '" + menuPath + "'");
            yield return Settle(SettleFrames * 2, SettleMilliseconds * 2);
        }

        // ================================================================================================ chờ

        /// <summary>Chờ đủ CẢ <paramref name="frames"/> khung lẫn <paramref name="milliseconds"/> giờ thật (V-23), bơm delayCall mỗi khung.</summary>
        internal static IEnumerator Settle(int frames, int milliseconds)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            int frame = 0;
            while (frame < frames || stopwatch.ElapsedMilliseconds < milliseconds)
            {
                PumpDelayCalls();
                frame++;
                yield return null;
            }
        }

        /// <summary>Chờ tới khi <paramref name="condition"/> đúng; chỉ fail khi quá cả 60 khung lẫn 5 giây (V-23).</summary>
        internal static IEnumerator WaitUntil(Func<bool> condition, string failureMessage)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            int frames = 0;
            while (!condition())
            {
                if (++frames > MaximumWaitFrames && stopwatch.ElapsedMilliseconds > MaximumWaitMilliseconds)
                {
                    Assert.Fail(failureMessage + " (đã chờ " + frames + " khung, " + stopwatch.ElapsedMilliseconds + " ms)");
                }
                PumpDelayCalls();
                yield return null;
            }
        }

        /// <summary>
        /// Bơm hàng <c>EditorApplication.delayCall</c> ngay. Đây là MÔ PHỎNG một nhịp vòng update của Editor (UJ-11: hộp xác nhận
        /// hoãn bằng delayCall không chạy khi phiên tự động đứng trong một cử chỉ), không phải cửa sau vào hub: hàm gọi đúng
        /// hàng đợi mà Editor tự gọi mỗi khung. Không tìm thấy hàm (bản Unity đổi tên) thì bỏ qua — khung kế tiếp vẫn chạy nó.
        /// </summary>
        internal static void PumpDelayCalls()
        {
            if (_delayCallPump == null)
            {
                _delayCallPump = typeof(EditorApplication).GetMethod(DelayCallPumpMethodName, BindingFlags.Static | BindingFlags.NonPublic);
                if (_delayCallPump == null) return;
            }
            _delayCallPump.Invoke(null, null);
        }

        // ================================================================================================ nội bộ

        /// <summary>
        /// Con trỏ đặt ở TÂM <paramref name="target"/> có thật sự chạm tới nó không (<c>panel.Pick</c> trả chính nó hoặc con nó).
        /// Đây là cách duy nhất phân biệt "có trong cây, có kích thước" với "bấm được": chevron <c>PickingMode.Ignore</c>, chip
        /// dựng bằng Label và nút nằm dưới một lớp phủ đều qua được mọi assert cũ mà chuột không bao giờ tới (UX-07/09/12/13).
        /// </summary>
        internal static bool PickReaches(EditorWindow window, VisualElement target)
        {
            if (window == null || target == null) return false;
            IPanel panel = window.rootVisualElement.panel;
            if (panel == null) return false;
            VisualElement picked = panel.Pick(target.worldBound.center);
            for (VisualElement current = picked; current != null; current = current.hierarchy.parent)
            {
                if (current == target) return true;
            }
            return false;
        }

        internal static bool IsPointInsideWindow(EditorWindow window, Vector2 point)
        {
            Rect bound = window.rootVisualElement.worldBound;
            return bound.Contains(point);
        }

        private static void Send(EditorWindow window, Event sentEvent)
        {
            if (window == null) throw new ArgumentNullException(nameof(window), "UxEventSender: cửa sổ đích đã đóng");
            window.SendEvent(sentEvent);
        }

        private static KeyCode KeyCodeOf(char character)
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
                case ',': return KeyCode.Comma;
                case '#': return KeyCode.Hash;
                default: return KeyCode.None;
            }
        }
    }
}
