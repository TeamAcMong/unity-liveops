// MÃ DEV TẠM (G-UX-JOURNEY) — hành trình J1…J11.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DreamTech.LiveOps.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UxJourney
{
    public static partial class UxJourneys
    {
        private static readonly DateTime RunningMomentUtc = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);
        private const string HuntBonus = "entry-hunt-0916-bonus";
        private const string HuntEarly = "entry-hunt-0914";
        private const string LavaMid = "entry-lava-quest-2026-09b";

        static partial void RegisterJourneys(Dictionary<string, Func<UxRunner, IEnumerator>> registry)
        {
            registry["J1"] = J1;
            registry["J2"] = J2;
            registry["J3"] = J3;
            registry["J4"] = J4;
            registry["J5"] = J5;
            registry["J6"] = J6;
            registry["J7"] = J7;
            registry["J8"] = J8;
            registry["J9"] = J9;
            registry["J9b"] = J9Sizes;
            registry["J10"] = J10;
            registry["J11"] = J11;
            RegisterExtra(registry);
        }

        static partial void RegisterExtra(Dictionary<string, Func<UxRunner, IEnumerator>> registry);

        private static IEnumerator PlaceSized(UxRunner runner, float width, float height)
        {
            float y = height > 960 ? 30 : 80;
            float x = width >= 1900 ? 0 : 40;
            UxHub.Place(Hub, x, y, width, height);
            yield return new WaitFrames(10);
            yield return new WaitSeconds(0.5);
            runner.Log("cỡ " + width + "×" + height + " → position " + Hub.position + " root " + Hub.rootVisualElement.layout);
        }

        // ------------------------------------------------------------------------------------------------ J1 co giãn

        private static IEnumerator J1(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 1280, 760);
            runner.CurrentStep = "J1-00";
            yield return SelectBar(runner, HuntBonus);
            float[,] sizes = { { 700, 560 }, { 820, 560 }, { 1024, 700 }, { 1280, 760 }, { 1440, 900 }, { 1920, 1040 } };
            for (int index = 0; index < sizes.GetLength(0); index++)
            {
                float width = sizes[index, 0], height = sizes[index, 1];
                string size = Size(width, height);
                runner.CurrentStep = "J1-" + size;
                yield return PlaceSized(runner, width, height);
                yield return NavigateTo(runner, "Lịch");
                yield return Snap(runner, "J1-" + (index * 2 + 1).ToString("00") + "-lich-" + size, "Lịch, đang chọn hunt-0916-bonus, cửa sổ " + size);
                yield return NavigateTo(runner, "Luật lặp");
                yield return Snap(runner, "J1-" + (index * 2 + 2).ToString("00") + "-luatlap-" + size, "Luật lặp, cửa sổ " + size);
                yield return NavigateTo(runner, "Lịch");
            }
        }

        // ------------------------------------------------------------------------------------------------ J2 inspector

        private static IEnumerator J2(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 1280, 760);
            runner.CurrentStep = "J2-01";
            yield return Snap(runner, "J2-01-truoc-khi-chon-1280x760", "mở Lịch, chưa chọn đợt (inspector trạng thái a)");
            VisualElement bar = Bar(HuntBonus);
            // rê chuột dừng trên thanh 800ms → hover card
            yield return UxInput.WaitRealCursorAway(Hub);
            UxInput.MouseMove(Hub, bar.worldBound.center);
            yield return new WaitSeconds(0.3);
            UxInput.MouseMove(Hub, bar.worldBound.center + new Vector2(1, 0));
            yield return new WaitSeconds(1.0);
            yield return Snap(runner, "J2-02-hover-card-1280x760", "rê chuột dừng trên hunt-0916-bonus → hover card");
            yield return SelectBar(runner, HuntBonus);
            yield return Snap(runner, "J2-03-chon-thanh-1280x760", "bấm chọn hunt-0916-bonus → inspector");
            DumpTree(runner, Root.Q("calendar-inspector"), "J2-tree-inspector.txt");

            VisualElement inspector = Root.Q("calendar-inspector");
            VisualElement startRow = FieldRow(inspector, "Bắt đầu");
            TextField startTime = startRow == null ? null : startRow.Q<TextField>(className: "liveops-hub-utc-field__time");
            if (startTime == null) Note(runner, "không thấy ô giờ Bắt đầu trong inspector");
            else
            {
                yield return UxInput.Click(Hub, InputOf(startTime));
                yield return new WaitFrames(3);
                yield return Snap(runner, "J2-04-focus-o-gio-bat-dau", "bấm vào ô giờ Bắt đầu (focus, chọn hết?)");
                yield return UxInput.PressKey(Hub, KeyCode.A, EventModifiers.Command, 'a');
                yield return UxInput.TypeText(Hub, "18:00");
                yield return Snap(runner, "J2-05-go-18-00-chua-enter", "gõ 18:00, chưa Enter");
                yield return UxInput.PressKey(Hub, KeyCode.Return, EventModifiers.None, '\n');
                yield return new WaitFrames(10);
                yield return Snap(runner, "J2-06-enter-gio-bat-dau", "Enter → ghi giờ bắt đầu 16/9 18:00");
            }

            inspector = Root.Q("calendar-inspector");
            VisualElement durationRow = FieldRow(inspector, "Dài");
            IntegerField duration = durationRow == null ? null : durationRow.Q<IntegerField>();
            if (duration == null) Note(runner, "không thấy ô Dài");
            else
            {
                runner.Log("Dài trước = " + duration.value);
                yield return ReplaceText(Hub, duration, "24", true);
                yield return new WaitFrames(8);
                yield return Snap(runner, "J2-07-doi-dai-24", "đổi Dài = 24 + Enter");
            }

            inspector = Root.Q("calendar-inspector");
            VisualElement endRow = FieldRow(inspector, "Kết thúc");
            TextField endDate = endRow == null ? null : endRow.Q<TextField>(className: "liveops-hub-utc-field__date");
            if (endDate != null)
            {
                yield return ReplaceText(Hub, endDate, "2026-09-1", true);
                yield return new WaitFrames(8);
                yield return Snap(runner, "J2-08-ngay-ket-thuc-go-hong", "gõ ngày kết thúc hỏng '2026-09-1' + Enter");
            }
            // cuộn thân inspector xuống xem Vấn đề + nút xoá
            VisualElement body = Root.Q("calendar-inspector-body");
            if (body != null)
            {
                UxInput.Wheel(Hub, body.worldBound.center, new Vector2(0, 30));
                yield return new WaitFrames(6);
                yield return Snap(runner, "J2-09-cuon-inspector", "lăn chuột trong inspector xuống cuối");
            }
            yield return Undo(runner);
            yield return Snap(runner, "J2-10-undo", "⌘Z sau khi sửa");
        }

        // ------------------------------------------------------------------------------------------------ J3 kéo thả

        private static IEnumerator J3(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 1280, 760);
            runner.CurrentStep = "J3-01";
            yield return SelectBar(runner, HuntBonus);
            VisualElement bar = Bar(HuntBonus);
            Rect bound = bar.worldBound;
            float dayPixels = 30.24f;
            yield return Snap(runner, "J3-01-truoc-keo", "chọn hunt-0916-bonus trước khi kéo");
            yield return UxInput.Drag(Hub, bound.center, bound.center + new Vector2(dayPixels, 0), 12, EventModifiers.None, EventModifiers.None,
                () => Snap(runner, "J3-02-keo-than-+1ngay-chua-nha", "kéo thân +1 ngày, CHƯA nhả (ghost, readout, kiểm nhanh)"));
            yield return Snap(runner, "J3-03-keo-than-da-nha", "nhả chuột → toast + status bar");

            bar = Bar(HuntBonus);
            bound = bar.worldBound;
            yield return UxInput.Drag(Hub, new Vector2(bound.xMax - 3, bound.center.y), new Vector2(bound.xMax - 3 + dayPixels, bound.center.y), 12,
                EventModifiers.None, EventModifiers.None, () => Snap(runner, "J3-04-keo-mep-phai-chua-nha", "kéo mép phải +1 ngày, chưa nhả"));
            yield return Snap(runner, "J3-05-keo-mep-phai-da-nha", "nhả mép phải");

            VisualElement early = Bar(HuntEarly);
            bound = early.worldBound;
            yield return UxInput.Drag(Hub, new Vector2(bound.xMax - 3, bound.center.y), new Vector2(bound.xMax - 3 + dayPixels * 2, bound.center.y), 14,
                EventModifiers.None, EventModifiers.None, () => Snap(runner, "J3-06-keo-tao-chong-chua-nha", "kéo mép cuối hunt-0914 +2 ngày tạo chồng, chưa nhả"));
            yield return new WaitSeconds(1.5);
            bool confirmOpened = UxHub.ConfirmWindow() != null;
            Note(runner, "sau khi nhả kéo tạo chồng: hộp xác nhận " + (confirmOpened ? "CÓ" : "KHÔNG") + " hiện");
            yield return Snap(runner, "J3-07-keo-tao-chong-da-nha", "nhả sau khi kéo tạo chồng");

            yield return Undo(runner);
            yield return Snap(runner, "J3-08-undo-1", "⌘Z lần 1");
            yield return Undo(runner);
            yield return Snap(runner, "J3-09-undo-2", "⌘Z lần 2");
            VisualElement toastUndo = UxFind.ByClass(Root, "liveops-hub-toast-action-button");
            if (toastUndo != null)
            {
                yield return UxInput.Click(Hub, toastUndo);
                yield return Snap(runner, "J3-10-bam-nut-toast", "bấm nút trên toast (" + ((toastUndo as TextElement)?.text ?? "") + ")");
            }

            // Xoá đợt đã đăng chưa bắt đầu (hunt-0914) bằng ⌘⌫ → hộp cấp 1: Esc (giữ), rồi lần 2 chọn Xoá
            yield return SelectBar(runner, HuntEarly);
            runner.CurrentStep = "J3-11";
            yield return UxOs.Activate(runner);
            runner.PlanModal("J3-11-hop-xoa-hunt-0914-esc", "key:53");
            ExecuteCommand(Hub, "SoftDelete");
            yield return WaitModalClosed(runner, 20);
            yield return Snap(runner, "J3-12-sau-esc-giu-lai", "sau khi Esc ở hộp xoá (giữ lại)");
            yield return SelectBar(runner, HuntEarly);
            runner.CurrentStep = "J3-13";
            runner.PlanModal("J3-13-hop-xoa-hunt-0914-xoa", "key:48:shift", "capture", "key:49");
            ExecuteCommand(Hub, "SoftDelete");
            yield return WaitModalClosed(runner, 20);
            yield return Snap(runner, "J3-14-sau-chon-xoa", "sau khi chọn Xoá đợt ở hộp");
            UxOs.Release(runner);
            yield return Undo(runner);
            yield return Snap(runner, "J3-15-undo-xoa", "⌘Z hoàn tác xoá");

            // Đợt đang chạy (18/9 10:00): rút ngắn mép cuối lava-quest-2026-09b → hộp sau khi nhả
            yield return OpenDesignHub(runner, "calendar", 1280, 760, "Vietnamese", RunningMomentUtc);
            runner.CurrentStep = "J3-16";
            yield return SelectBar(runner, LavaMid);
            VisualElement running = Bar(LavaMid);
            bound = running.worldBound;
            yield return UxOs.Activate(runner);
            runner.PlanModal("J3-17-hop-rut-ngan-esc", "key:53");
            yield return UxInput.Drag(Hub, new Vector2(bound.xMax - 3, bound.center.y), new Vector2(bound.xMax - 3 - dayPixels, bound.center.y), 12,
                EventModifiers.None, EventModifiers.None, () => Snap(runner, "J3-16-rut-ngan-dang-chay-chua-nha", "18/9 10:00: kéo mép cuối 09b sang trái 1 ngày, chưa nhả"));
            yield return new WaitSeconds(1);
            ForceDelayCalls(runner);
            yield return new WaitSeconds(1);
            yield return WaitModalClosed(runner, 20);
            yield return Snap(runner, "J3-18-sau-esc-giu", "sau Esc ở hộp rút ngắn (giữ)");
            running = Bar(LavaMid);
            bound = running.worldBound;
            runner.PlanModal("J3-19-hop-rut-ngan-chon-rut", "key:48:shift", "capture", "key:49");
            yield return UxInput.Drag(Hub, new Vector2(bound.xMax - 3, bound.center.y), new Vector2(bound.xMax - 3 - dayPixels, bound.center.y), 12,
                EventModifiers.None, EventModifiers.None);
            yield return new WaitSeconds(1);
            ForceDelayCalls(runner);
            yield return new WaitSeconds(1);
            yield return WaitModalClosed(runner, 20);
            UxOs.Release(runner);
            yield return Snap(runner, "J3-20-sau-chon-rut-ngan", "sau khi chọn Rút ngắn đợt");
            // kéo mép đầu đợt đang chạy (khoá)
            running = Bar(LavaMid);
            bound = running.worldBound;
            yield return UxInput.Drag(Hub, new Vector2(bound.xMin + 3, bound.center.y), new Vector2(bound.xMin + 3 - dayPixels, bound.center.y), 10,
                EventModifiers.None, EventModifiers.None, () => Snap(runner, "J3-21-keo-mep-dau-dang-chay", "kéo mép đầu đợt đang chạy (khoá), chưa nhả"));
            yield return Snap(runner, "J3-22-nha-mep-dau-dang-chay", "nhả mép đầu đợt đang chạy");
            yield return Undo(runner);
            yield return Snap(runner, "J3-23-undo", "⌘Z");
            DrainModalPlans(runner);
        }

        // ------------------------------------------------------------------------------------------------ J4 zoom, cuộn

        private static IEnumerator J4(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 1280, 760);
            runner.CurrentStep = "J4-01";
            VisualElement track = UxFind.ByClass(Root, "liveops-hub-timeline-ruler-track");
            Vector2 pointer = new Vector2(track.worldBound.xMin + track.worldBound.width * 0.55f, Bar(HuntEarly).worldBound.center.y);
            UxInput.MouseMove(Hub, pointer);
            yield return new WaitFrames(4);
            yield return Snap(runner, "J4-01-con-tro-tren-track", "con trỏ đứng trên track (bubble giờ)");
            for (int notch = 0; notch < 4; notch++)
            {
                UxInput.Wheel(Hub, pointer, new Vector2(0, -3), EventModifiers.Command);
                yield return new WaitFrames(3);
            }
            yield return Snap(runner, "J4-02-cmd-lan-zoom-vao", "⌘ + lăn 4 nấc (zoom vào tại con trỏ)");
            for (int notch = 0; notch < 8; notch++)
            {
                UxInput.Wheel(Hub, pointer, new Vector2(0, 3), EventModifiers.Command);
                yield return new WaitFrames(3);
            }
            yield return Snap(runner, "J4-03-cmd-lan-zoom-ra", "⌘ + lăn ngược 8 nấc (zoom ra)");
            for (int notch = 0; notch < 6; notch++)
            {
                UxInput.Wheel(Hub, pointer, new Vector2(0, 3), EventModifiers.Shift);
                yield return new WaitFrames(3);
            }
            yield return Snap(runner, "J4-04-shift-lan-cuon-ngang", "Shift + lăn 6 nấc (cuộn ngang)");
            UxInput.Wheel(Hub, pointer, new Vector2(0, 3));
            yield return new WaitFrames(4);
            yield return Snap(runner, "J4-05-lan-tron", "lăn trơn 1 nấc trên track");
            foreach (string tab in new[] { "Ngày", "Tháng", "3 tuần" })
            {
                yield return ClickButtonText(runner, Hub, tab);
                yield return Snap(runner, "J4-06-tab-" + (tab == "Ngày" ? "ngay" : tab == "Tháng" ? "thang" : "3tuan"), "bấm tab zoom '" + tab + "'");
            }
            VisualElement today = UxFind.ByClass(Root, "liveops-hub-calendar-today-button");
            if (today != null)
            {
                runner.Log("Hôm nay enabled=" + today.enabledInHierarchy + " tooltip=" + today.tooltip);
                yield return UxInput.Click(Hub, today);
                yield return Snap(runner, "J4-07-hom-nay", "bấm Hôm nay (T)");
            }
            ToolbarMenu range = UxFind.First(Root, element => element is ToolbarMenu menu && menu.text != null && menu.text.Contains("–")) as ToolbarMenu;
            if (range != null)
            {
                List<DropdownMenuAction> actions = ReadMenu(runner, "menu khoảng ngày '" + range.text + "'", range.menu);
                RunMenu(runner, actions, "3 tuần sau");
                yield return new WaitFrames(6);
                yield return Snap(runner, "J4-08-menu-khoang-3-tuan-sau", "menu khoảng ngày → 3 tuần sau");
                actions = ReadMenu(runner, "menu khoảng ngày '" + range.text + "'", range.menu);
                yield return UxOs.Activate(runner);
                foreach (DropdownMenuAction action in actions)
                {
                    if (action.name.Contains("…"))
                    {
                        runner.Log("chạy '" + action.name + "'");
                        action.Execute();
                        break;
                    }
                }
                yield return new WaitSeconds(1.5);
                EditorWindow popup = UxHub.PopupWindow();
                yield return Snap(runner, "J4-09-chon-ngay-bat-dau", "menu khoảng ngày → chọn ngày bắt đầu… (popover?)", popup);
                if (popup != null)
                {
                    DumpTree(runner, popup.rootVisualElement, "J4-tree-range-popover.txt");
                    TextField dateField = popup.rootVisualElement.Q<TextField>(className: "liveops-hub-utc-field__date") ?? popup.rootVisualElement.Q<TextField>();
                    if (dateField != null)
                    {
                        yield return ReplaceText(popup, dateField, "2026-10-01", true);
                        yield return new WaitSeconds(0.8);
                        yield return Snap(runner, "J4-10-go-ngay-bat-dau-enter", "gõ 2026-10-01 + Enter trong popover chọn ngày", UxHub.PopupWindow());
                    }
                    if (UxHub.PopupWindow() != null) yield return UxInput.PressKey(UxHub.PopupWindow(), KeyCode.Escape, EventModifiers.None, (char)27);
                }
                UxOs.Release(runner);
            }
            ToolbarMenu snap = UxFind.First(Root, element => element is ToolbarMenu menu && menu.text != null && menu.text.StartsWith("Bắt lưới")) as ToolbarMenu;
            if (snap != null) ReadMenu(runner, "menu bắt lưới", snap.menu);
            // Tìm id
            VisualElement search = UxFind.ByClass(Root, "liveops-hub-calendar-search");
            if (search != null)
            {
                TextField searchField = search.Q<TextField>();
                yield return ReplaceText(Hub, searchField, "star", false);
                yield return new WaitFrames(8);
                yield return Snap(runner, "J4-11-tim-star", "gõ 'star' vào ô Tìm id đợt");
                yield return UxInput.PressKey(Hub, KeyCode.Return, EventModifiers.None, '\n');
                yield return new WaitFrames(8);
                yield return Snap(runner, "J4-12-tim-enter", "Enter trong ô tìm");
            }
        }

        // ------------------------------------------------------------------------------------------------ J5 thêm đợt

        private static IEnumerator J5(UxRunner runner)
        {
            // PopupWindow chỉ hiện và chỉ sống khi Unity là app đang dùng (mất focus = tự đóng) → đưa Unity lên trước suốt J5.
            yield return OpenDesignHub(runner, "calendar", 1280, 760);
            yield return UxOs.Activate(runner);
            try
            {
                IEnumerator inner = J5Body(runner);
                while (inner.MoveNext()) yield return inner.Current;
            }
            finally
            {
                UxOs.Release(runner);
            }
        }

        private static IEnumerator J5Body(UxRunner runner)
        {
            runner.CurrentStep = "J5-01";
            VisualElement add = UxFind.Clickable(Root, "Thêm đợt", true);
            yield return OpenPopoverFrom(runner, add);
            EditorWindow popup = UxHub.PopupWindow();
            if (popup == null)
            {
                Note(runner, "bấm 'Thêm đợt' khi Unity ở nền: KHÔNG thấy popover (PopupWindow)");
                yield return Snap(runner, "J5-01-khong-co-popover", "bấm Thêm đợt: không có popover");
                yield break;
            }
            yield return Snap(runner, "J5-01-buoc1", "popover Thêm đợt bước 1", popup);
            DumpTree(runner, popup.rootVisualElement, "J5-tree-step1.txt");
            yield return UxInput.TypeText(popup, "hunt");
            yield return Snap(runner, "J5-02-loc-hunt", "gõ 'hunt' vào ô lọc", popup);
            yield return UxInput.PressKey(popup, KeyCode.Return, EventModifiers.None, '\n');
            yield return new WaitFrames(8);
            yield return Snap(runner, "J5-03-enter-buoc2", "Enter → bước 2", UxHub.PopupWindow());
            popup = UxHub.PopupWindow();
            if (popup == null) yield break;
            DumpTree(runner, popup.rootVisualElement, "J5-tree-step2.txt");
            TextField date = popup.rootVisualElement.Q<TextField>(className: "liveops-hub-utc-field__date");
            if (date != null) yield return ReplaceText(popup, date, "2026-09-15", false);
            yield return UxInput.PressKey(popup, KeyCode.Tab, EventModifiers.None, '\t');
            yield return Snap(runner, "J5-04-go-ngay-15-9", "gõ ngày 2026-09-15 (sẽ chồng hunt-0914), Tab", popup);
            IntegerField duration = popup.rootVisualElement.Q<IntegerField>();
            if (duration != null) yield return ReplaceText(popup, duration, "72", false);
            yield return UxInput.PressKey(popup, KeyCode.Tab, EventModifiers.None, '\t');
            yield return Snap(runner, "J5-05-dai-72", "Dài 72, Tab", popup);
            yield return ClickButtonText(runner, popup, "Tiếp");
            popup = UxHub.PopupWindow();
            yield return Snap(runner, "J5-06-buoc3-chong-gio", "bước 3 — kiểm nhanh báo chồng", popup);
            if (popup == null) yield break;
            DumpTree(runner, popup.rootVisualElement, "J5-tree-step3-overlap.txt");
            yield return UxInput.PressKey(popup, KeyCode.Return, EventModifiers.None, '\n');
            yield return new WaitFrames(8);
            popup = UxHub.PopupWindow();
            yield return Snap(runner, "J5-07-enter-quay-lai-sua-gio", "Enter ở biến thể chồng (= Quay lại sửa giờ?)", popup);
            if (popup == null) yield break;
            date = popup.rootVisualElement.Q<TextField>(className: "liveops-hub-utc-field__date");
            if (date != null) yield return ReplaceText(popup, date, "2026-09-21", true);
            yield return new WaitFrames(8);
            popup = UxHub.PopupWindow();
            yield return Snap(runner, "J5-08-sua-21-9-enter", "sửa ngày 2026-09-21 + Enter", popup);
            if (popup != null && popup.rootVisualElement.Q<TextField>(className: "liveops-hub-utc-field__date") != null)
            {
                yield return ClickButtonText(runner, popup, "Tiếp");
                popup = UxHub.PopupWindow();
                yield return Snap(runner, "J5-09-buoc3-khong-chong", "bước 3 — không chồng", popup);
            }
            if (popup == null) yield break;
            VisualElement idRow = FieldRow(popup.rootVisualElement, "Id");
            TextField idField = idRow == null ? null : idRow.Q<TextField>();
            if (idField != null)
            {
                yield return ReplaceText(popup, idField, "hunt-0921-test", false);
                yield return Snap(runner, "J5-10-sua-id", "sửa Id thành hunt-0921-test (chưa rời ô)", popup);
            }
            VisualElement submit = UxFind.First(popup.rootVisualElement, element => element is Button button && button.text != null && button.text.StartsWith("Thêm "));
            if (submit != null)
            {
                runner.Log("nút thêm: '" + ((Button)submit).text + "'");
                yield return UxInput.Click(popup, submit);
                yield return new WaitSeconds(1);
            }
            yield return Snap(runner, "J5-11-da-them", "bấm Thêm … vào lịch → hub", UxHub.PopupWindow());
            // Nhấp đúp chỗ trống làn star-tournament
            VisualElement lane = UxFind.All(Root, element => element.ClassListContains("liveops-hub-timeline-lane-row") && UxFind.Shown(element))
                .Find(element => UxFind.First(element, child => child is Label label && label.text == "star-tournament") != null);
            if (lane != null)
            {
                Rect laneBound = lane.worldBound;
                yield return UxInput.DoubleClickAt(Hub, new Vector2(laneBound.xMin + 168 + 120, laneBound.center.y + 6));
                yield return new WaitUntilOrTimeout(() => UxHub.PopupWindow() != null, 3);
                yield return Snap(runner, "J5-12-nhap-dup-lan-trong", "nhấp đúp chỗ trống làn star-tournament", UxHub.PopupWindow());
                if (UxHub.PopupWindow() != null) yield return UxInput.PressKey(UxHub.PopupWindow(), KeyCode.Escape, EventModifiers.None, (char)27);
                yield return new WaitFrames(6);
                yield return Snap(runner, "J5-13-esc-dong-popover", "Esc đóng popover");
            }
        }

        // ------------------------------------------------------------------------------------------------ J6 chọn nhiều

        private static IEnumerator J6(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 1280, 760);
            runner.CurrentStep = "J6-01";
            yield return SelectBar(runner, HuntEarly);
            yield return UxInput.ClickAt(Hub, Bar(HuntBonus).worldBound.center, EventModifiers.Command);
            yield return Snap(runner, "J6-01-cmd-click-2-thanh", "bấm hunt-0914, ⌘-click hunt-0916-bonus");
            yield return UxInput.ClickAt(Hub, Bar(HuntBonus).worldBound.center, EventModifiers.Command);
            yield return Snap(runner, "J6-02-cmd-click-bo-chon", "⌘-click lại hunt-0916-bonus (bỏ khỏi tập)");
            yield return SelectBar(runner, "entry-lava-quest-2026-09a");
            yield return UxInput.ClickAt(Hub, Bar(LavaMid).worldBound.center, EventModifiers.Shift);
            yield return Snap(runner, "J6-03-shift-click-dai", "bấm lava-quest-2026-09a, Shift-click 09b (chọn dải)");
            // khung chọn trên chỗ trống làn treasure-hunt
            VisualElement early = Bar(HuntEarly);
            Vector2 from = new Vector2(early.worldBound.xMin - 40, early.worldBound.yMin - 6);
            Vector2 to = new Vector2(Bar(HuntBonus).worldBound.xMax + 20, Bar(HuntBonus).worldBound.yMax + 4);
            yield return UxInput.Drag(Hub, from, to, 10, EventModifiers.None, EventModifiers.None,
                () => Snap(runner, "J6-04-khung-chon-chua-nha", "kéo khung chọn quanh 2 thanh treasure-hunt, chưa nhả"));
            yield return Snap(runner, "J6-05-khung-chon-da-nha", "nhả khung chọn → inspector nhiều đợt");
            DumpTree(runner, Root.Q("calendar-inspector"), "J6-tree-inspector-multi.txt");
            // Shift kéo theo đợt sau: chọn 09b, nhấn không Shift, giữa chừng giữ Shift
            yield return SelectBar(runner, "entry-lava-quest-2026-09a");
            yield return SelectBar(runner, LavaMid);
            VisualElement mid = Bar(LavaMid);
            Vector2 start = mid.worldBound.center;
            yield return UxInput.WaitRealCursorAway(Hub);
            UxInput.MouseMove(Hub, start);
            yield return null;
            UxInput.MouseDown(Hub, start);
            yield return new WaitFrames(2);
            Vector2 previous = start;
            for (int step = 1; step <= 12; step++)
            {
                Vector2 point = start + new Vector2(30.24f * 2 * step / 12f, 0);
                UxInput.MouseDrag(Hub, point, point - previous, 0, step > 4 ? EventModifiers.Shift : EventModifiers.None);
                previous = point;
                yield return null;
            }
            yield return new WaitFrames(4);
            yield return Snap(runner, "J6-06-shift-keo-theo-chua-nha", "kéo 09b +2 ngày, giữ Shift giữa chừng (kéo theo đợt sau), chưa nhả");
            UxInput.MouseUp(Hub, previous, 0, EventModifiers.Shift);
            yield return new WaitFrames(6);
            yield return Snap(runner, "J6-07-shift-keo-theo-da-nha", "nhả → toast");
            yield return Undo(runner);
            // Xoá nhiều: chọn 2 thanh treasure-hunt rồi nút Xoá n đợt…
            yield return SelectBar(runner, HuntEarly);
            yield return UxInput.ClickAt(Hub, Bar(HuntBonus).worldBound.center, EventModifiers.Command);
            VisualElement deleteButton = UxFind.First(Root.Q("calendar-inspector"), element => element is Button button && button.text != null && button.text.StartsWith("Xoá"));
            if (deleteButton != null)
            {
                runner.Log("nút xoá nhiều: '" + ((Button)deleteButton).text + "'");
                yield return UxOs.Activate(runner);
                runner.PlanModal("J6-08-hop-xoa-nhieu", "key:48:shift", "capture", "key:49");
                yield return UxInput.Click(Hub, deleteButton);
                yield return new WaitSeconds(1);
                yield return WaitModalClosed(runner, 20);
                UxOs.Release(runner);
                yield return Snap(runner, "J6-09-sau-xoa-nhieu", "sau khi xoá 2 đợt");
                yield return Undo(runner);
                yield return Snap(runner, "J6-10-undo-xoa-nhieu", "⌘Z hoàn tác xoá nhiều");
            }
            else
            {
                Note(runner, "inspector nhiều đợt không có nút Xoá…");
            }
            DrainModalPlans(runner);
        }

        // ------------------------------------------------------------------------------------------------ J7 làn

        private static IEnumerator J7(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 1280, 760);
            runner.CurrentStep = "J7-01";
            VisualElement weeklyHeader = UxFind.LaneHeader(Root, "weekly-pass");
            VisualElement chevron = weeklyHeader == null ? null : weeklyHeader.Q(className: "liveops-hub-timeline-lane-chevron");
            if (chevron != null)
            {
                yield return UxInput.Click(Hub, chevron);
                yield return Snap(runner, "J7-01-thu-gon-weekly-pass", "bấm chevron thu gọn làn weekly-pass");
                VisualElement skyHeader = UxFind.LaneHeader(Root, "sky-race");
                VisualElement skyChevron = skyHeader == null ? null : skyHeader.Q(className: "liveops-hub-timeline-lane-chevron");
                if (skyChevron != null) yield return UxInput.Click(Hub, skyChevron);
                yield return Snap(runner, "J7-02-thu-gon-sky-race", "thu gọn thêm sky-race");
                // bấm vào tên làn (không phải chevron) có mở/thu không?
                VisualElement name = UxFind.First(UxFind.LaneHeader(Root, "weekly-pass"), element => element is Label label && label.text == "weekly-pass");
                if (name != null) yield return UxInput.Click(Hub, name);
                yield return Snap(runner, "J7-03-bam-ten-lan", "bấm tên làn weekly-pass");
            }
            else
            {
                Note(runner, "không thấy chevron thu gọn làn");
            }
            VisualElement lavaHeader = UxFind.LaneHeader(Root, "lava-quest");
            CalendarContextMenu(runner, "header làn lava-quest", lavaHeader.worldBound.center, null);
            CalendarContextMenu(runner, "header làn star-tournament (Đưa lên)", UxFind.LaneHeader(Root, "star-tournament").worldBound.center, "Đưa lên");
            yield return new WaitFrames(8);
            yield return Snap(runner, "J7-04-dua-len-star-tournament", "menu chuột phải header star-tournament → Đưa lên");
            CalendarContextMenu(runner, "header làn lava-quest (Ẩn làn)", UxFind.LaneHeader(Root, "lava-quest").worldBound.center, "Ẩn");
            yield return new WaitFrames(8);
            yield return Snap(runner, "J7-05-an-lan-lava-quest", "menu chuột phải header lava-quest → Ẩn làn");
            VisualElement hiddenChip = UxFind.ByClass(Root, "liveops-hub-calendar-hidden-lanes-chip");
            if (hiddenChip != null)
            {
                yield return UxInput.Click(Hub, hiddenChip);
                yield return Snap(runner, "J7-06-bam-chip-hien-lan", "bấm chip 'Đang ẩn … · Hiện'");
            }
            else
            {
                Note(runner, "sau khi ẩn làn không thấy chip làn ẩn trên toolbar");
            }
            // menu của thanh cố định, thanh lặp, chỗ trống
            CalendarContextMenu(runner, "thanh cố định lava-quest-2026-09b", Bar(LavaMid).worldBound.center, null);
            VisualElement recurringBar = UxFind.First(Root, element => element.ClassListContains("liveops-hub-timeline-bar--recurring") && element.name.StartsWith("weekly-pass#") && UxFind.Shown(element));
            if (recurringBar != null) CalendarContextMenu(runner, "thanh lặp " + recurringBar.name, recurringBar.worldBound.center, null);
            VisualElement starLane = UxFind.All(Root, element => element.ClassListContains("liveops-hub-timeline-lane-row") && UxFind.Shown(element))
                .Find(element => UxFind.First(element, child => child is Label label && label.text == "star-tournament") != null);
            if (starLane != null) CalendarContextMenu(runner, "chỗ trống làn star-tournament", new Vector2(starLane.worldBound.xMin + 300, starLane.worldBound.center.y), null);
            // thử chuột phải thật một lần: ghi lại chuyện gì xảy ra (menu gốc hệ điều hành)
            runner.PlanModal("J7-07-menu-goc-chuot-phai", "key:53");
            long ticks = runner.TickCount;
            DateTime before = DateTime.Now;
            UxInput.MouseDown(Hub, Bar(LavaMid).worldBound.center, 1);
            UxInput.MouseUp(Hub, Bar(LavaMid).worldBound.center, 1);
            Note(runner, "chuột phải thật lên thanh 09b: SendEvent trở về sau " + (DateTime.Now - before).TotalMilliseconds.ToString("0") + " ms");
            yield return new WaitSeconds(2);
            yield return Snap(runner, "J7-08-sau-chuot-phai-that", "sau chuột phải thật");
            DrainModalPlans(runner);
        }

        // ------------------------------------------------------------------------------------------------ J8 cửa sổ hẹp

        private static IEnumerator J8(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 820, 560);
            runner.CurrentStep = "J8-01";
            yield return Snap(runner, "J8-01-hep-820x560", "cửa sổ 820×560, chưa chọn");
            yield return SelectBar(runner, HuntBonus);
            yield return Snap(runner, "J8-02-chon-mo-drawer", "chọn hunt-0916-bonus → drawer");
            DumpTree(runner, Root, "J8-tree-drawer.txt");
            VisualElement close = UxFind.ByClass(Root, "liveops-hub-calendar-drawer-close");
            if (close != null)
            {
                yield return UxInput.Click(Hub, close);
                yield return Snap(runner, "J8-03-dong-drawer-nut", "bấm nút đóng drawer");
            }
            else Note(runner, "drawer không có nút đóng hiện");
            yield return SelectBar(runner, HuntBonus);
            yield return UxInput.PressKey(Hub, KeyCode.Escape, EventModifiers.None, (char)27);
            yield return new WaitFrames(6);
            yield return Snap(runner, "J8-04-esc-dong-drawer", "chọn lại rồi Esc");
            VisualElement compare = UxFind.ByClass(Root, "liveops-hub-calendar-compare-toggle");
            if (compare != null)
            {
                yield return UxInput.Click(Hub, compare);
                yield return Snap(runner, "J8-05-so-voi-da-dang", "bấm 'So với đã đăng' ở cửa sổ hẹp");
                VisualElement row = UxFind.ByClass(Root, "liveops-hub-calendar-compare-row");
                if (row != null)
                {
                    yield return UxInput.Click(Hub, row);
                    yield return Snap(runner, "J8-06-bam-dong-so-sanh", "bấm dòng đầu của pane So với đã đăng");
                }
                yield return UxInput.Click(Hub, UxFind.ByClass(Root, "liveops-hub-calendar-compare-toggle") ?? compare);
                yield return Snap(runner, "J8-07-tat-so-sanh", "tắt So với đã đăng");
            }
            else Note(runner, "cửa sổ hẹp: không có nút So với đã đăng trên toolbar");
            ToolbarMenu overflow = UxFind.First(Root, element => element is ToolbarMenu menu && menu.ClassListContains("liveops-hub-calendar-overflow-menu") && UxFind.Shown(menu)) as ToolbarMenu;
            if (overflow != null)
            {
                List<DropdownMenuAction> actions = ReadMenu(runner, "menu ⋮ (hẹp)", overflow.menu);
                RunMenu(runner, actions, "Chú giải");
                yield return new WaitFrames(6);
                yield return Snap(runner, "J8-08-menu-chu-giai", "menu ⋮ → Chú giải");
            }
            ToolbarMenu zoomMenu = UxFind.First(Root, element => element is ToolbarMenu menu && menu.ClassListContains("liveops-hub-calendar-zoom-menu") && UxFind.Shown(menu)) as ToolbarMenu;
            if (zoomMenu != null)
            {
                List<DropdownMenuAction> actions = ReadMenu(runner, "menu zoom (hẹp)", zoomMenu.menu);
                RunMenu(runner, actions, "Tháng");
                yield return new WaitFrames(6);
                yield return Snap(runner, "J8-09-zoom-thang-hep", "menu zoom → Tháng ở cửa sổ hẹp");
            }
            yield return PlaceSized(runner, 700, 560);
            yield return SelectBar(runner, LavaMid);
            yield return Snap(runner, "J8-10-700x560-drawer", "700×560 chọn lava-quest-2026-09b");
        }

        // ------------------------------------------------------------------------------------------------ J9 Luật lặp

        private static IEnumerator J9(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "recurring", 1280, 760);
            runner.CurrentStep = "J9-01";
            yield return Snap(runner, "J9-01-luat-lap-1280x760", "Luật lặp mặc định (13/9 08:47)");
            VisualElement token = Root.Q("recurring-token-id-prefix");
            if (token != null)
            {
                yield return UxInput.Click(Hub, token);
                yield return Snap(runner, "J9-02-bam-token-tien-to", "bấm token 'pass-' trong câu");
            }
            TextField prefix = Root.Q<TextField>("recurring-prefix-field");
            yield return ReplaceText(Hub, prefix, "wp-", false);
            yield return Snap(runner, "J9-03-go-tien-to-chua-roi-o", "gõ tiền tố 'wp-', chưa rời ô");
            yield return UxInput.PressKey(Hub, KeyCode.Return, EventModifiers.None, '\n');
            yield return new WaitFrames(10);
            yield return Snap(runner, "J9-04-enter-nhap-tai-o", "Enter → nháp tại ô (bước 1)");
            DumpTree(runner, Root.Q("recurring-form-scroll") ?? Root, "J9-tree-draft.txt");
            Button write = Root.Q<Button>("recurring-draft-write");
            if (write != null)
            {
                yield return UxOs.Activate(runner);
                runner.PlanModal("J9-05-hop-go-ten-esc", "key:53");
                yield return UxInput.Click(Hub, write);
                yield return new WaitSeconds(1);
                yield return WaitModalClosed(runner, 20);
                yield return Snap(runner, "J9-06-sau-esc-giu-tien-to-cu", "Esc ở hộp gõ tên → về nháp");
                string running = RunningOccurrenceId();
                runner.Log("id đợt đang chạy theo bảng: " + running);
                write = Root.Q<Button>("recurring-draft-write");
                if (write != null)
                {
                    runner.PlanModal("J9-07-hop-go-ten-go-dung", "type:" + running, "capture", "key:48", "capture", "key:49");
                    yield return UxInput.Click(Hub, write);
                    yield return new WaitSeconds(1);
                    yield return WaitModalClosed(runner, 25);
                    yield return Snap(runner, "J9-08-sau-doi-tien-to", "sau hộp gõ tên (gõ '" + running + "', Tab, Space)");
                    UxOs.Release(runner);
                }
            }
            else
            {
                Note(runner, "không thấy nút 'Ghi tiền tố mới…' sau khi Enter");
            }
            // Neo
            VisualElement anchor = Root.Q("recurring-anchor-field");
            TextField anchorDate = anchor == null ? null : anchor.Q<TextField>(className: "liveops-hub-utc-field__date");
            if (anchorDate != null)
            {
                yield return ReplaceText(Hub, anchorDate, "2026-01-06", true);
                yield return new WaitFrames(10);
                yield return Snap(runner, "J9-09-doi-neo", "đổi Neo sang 2026-01-06 + Enter");
                Button cancel = Root.Q<Button>("recurring-draft-cancel");
                if (cancel != null)
                {
                    yield return UxInput.Click(Hub, cancel);
                    yield return Snap(runner, "J9-10-huy-nhap-neo", "bấm Huỷ (Esc) ở nháp neo");
                }
            }
            IntegerField period = Root.Q<IntegerField>("recurring-period-field");
            if (period != null)
            {
                yield return ReplaceText(Hub, period, "144", true);
                yield return new WaitFrames(10);
                yield return Snap(runner, "J9-11-doi-chu-ky-144", "đổi Chu kỳ = 144 + Enter");
                Button cancel = Root.Q<Button>("recurring-draft-cancel");
                if (cancel != null)
                {
                    yield return UxInput.PressKey(Hub, KeyCode.Escape, EventModifiers.None, (char)27);
                    yield return new WaitFrames(6);
                    yield return Snap(runner, "J9-12-esc-huy-nhap-chu-ky", "Esc để huỷ nháp chu kỳ");
                }
            }
            IntegerField active = Root.Q<IntegerField>("recurring-active-field");
            if (active != null)
            {
                yield return ReplaceText(Hub, active, "200", true);
                yield return new WaitFrames(10);
                yield return Snap(runner, "J9-13-chay-200-lon-hon-chu-ky", "Chạy mỗi đợt = 200 (> chu kỳ) + Enter");
                Button cancel = Root.Q<Button>("recurring-draft-cancel");
                if (cancel != null)
                {
                    yield return UxInput.Click(Hub, cancel);
                    yield return Snap(runner, "J9-14-huy-nhap-chay", "Huỷ nháp giờ chạy");
                }
            }
            // sky-race
            Button skyRow = Root.Q<Button>("recurring-row-sky-race");
            if (skyRow != null)
            {
                yield return UxInput.Click(Hub, skyRow);
                yield return Snap(runner, "J9-15-chon-sky-race", "chọn luật sky-race");
                IntegerField skyActive = Root.Q<IntegerField>("recurring-active-field");
                if (skyActive != null)
                {
                    yield return ReplaceText(Hub, skyActive, "22", true);
                    yield return new WaitFrames(10);
                    yield return Snap(runner, "J9-16-sky-race-chay-22", "sky-race Chạy mỗi đợt = 22 + Enter");
                    Button write2 = Root.Q<Button>("recurring-draft-write");
                    if (write2 != null)
                    {
                        yield return UxOs.Activate(runner);
                        runner.PlanModal("J9-17-hop-doi-gio-chay", "key:48:shift", "capture", "key:49");
                        yield return UxInput.Click(Hub, write2);
                        yield return new WaitSeconds(1);
                        yield return WaitModalClosed(runner, 20);
                        UxOs.Release(runner);
                        yield return Snap(runner, "J9-18-sau-ghi-gio-chay", "sau hộp đổi giờ chạy");
                    }
                }
            }
            // JSON hỏng
            Foldout json = Root.Q<Foldout>("recurring-json-foldout");
            if (json != null)
            {
                VisualElement toggle = json.Q<Toggle>() ?? (VisualElement)json;
                yield return UxInput.Click(Hub, toggle);
                yield return new WaitFrames(8);
                yield return Snap(runner, "J9-19-mo-json", "mở foldout JSON của luật này");
                TextField editor = Root.Q<TextField>("recurring-json-text");
                if (editor != null)
                {
                    yield return UxInput.Click(Hub, InputOf(editor));
                    yield return UxInput.PressKey(Hub, KeyCode.A, EventModifiers.Command, 'a');
                    yield return UxInput.TypeText(Hub, "{\"type\": \"sky-race\" \"periodHours\": 24}");
                    yield return new WaitFrames(12);
                    yield return new WaitSeconds(0.6);
                    yield return Snap(runner, "J9-20-json-hong", "gõ JSON thiếu dấu phẩy");
                    Button apply = Root.Q<Button>("recurring-json-apply");
                    if (apply != null)
                    {
                        runner.Log("nút Áp enabled=" + apply.enabledInHierarchy);
                        yield return UxInput.Click(Hub, apply);
                        yield return Snap(runner, "J9-21-bam-ap-khi-hong", "bấm Áp khi JSON hỏng");
                    }
                }
            }
            // Thêm 5 lần kế tiếp
            yield return NavigateTo(runner, "Luật lặp");
            Button weeklyRow = Root.Q<Button>("recurring-row-weekly-pass");
            if (weeklyRow != null) yield return UxInput.Click(Hub, weeklyRow);
            VisualElement more = UxFind.Clickable(Root, "Thêm 5", true);
            if (more != null)
            {
                yield return UxInput.Click(Hub, more);
                yield return new WaitFrames(8);
                yield return new WaitSeconds(0.6);
                yield return Snap(runner, "J9-22-them-5", "bấm Thêm 5");
                VisualElement form = Root.Q("recurring-form-scroll");
                if (form != null)
                {
                    UxInput.Wheel(Hub, form.worldBound.center, new Vector2(0, 40));
                    yield return new WaitFrames(8);
                    yield return Snap(runner, "J9-23-cuon-xuong", "lăn chuột xuống cuối form");
                }
            }
            yield return PlaceSized(runner, 820, 560);
            yield return Snap(runner, "J9-24-luat-lap-820x560", "Luật lặp ở 820×560");
            yield return PlaceSized(runner, 1920, 1040);
            yield return Snap(runner, "J9-25-luat-lap-1920x1040", "Luật lặp ở 1920×1040");
            DrainModalPlans(runner);
        }

        private static IEnumerator J9Sizes(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "recurring", 1280, 760);
            yield return PlaceSized(runner, 820, 560);
            yield return Snap(runner, "J9-24-luat-lap-820x560", "Luật lặp ở 820×560");
            TextField prefix = Root.Q<TextField>("recurring-prefix-field");
            if (prefix != null)
            {
                yield return ReplaceText(Hub, prefix, "wp-", true);
                yield return new WaitFrames(8);
                yield return Snap(runner, "J9-26-nhap-tien-to-820x560", "nháp tiền tố ở 820×560");
                Button cancel = Root.Q<Button>("recurring-draft-cancel");
                if (cancel != null) yield return UxInput.Click(Hub, cancel);
            }
            yield return PlaceSized(runner, 1920, 1040);
            yield return Snap(runner, "J9-25-luat-lap-1920x1040", "Luật lặp ở 1920×1040");
        }

        private static string RunningOccurrenceId()
        {
            VisualElement rows = Root.Q("recurring-occurrences-rows");
            if (rows == null) return "weekly-pass-35";
            foreach (VisualElement row in rows.Children())
            {
                if (UxFind.First(row, element => element is Label label && label.text == "Đang chạy") == null) continue;
                List<VisualElement> monos = UxFind.All(row, element => element is Label label && label.ClassListContains("liveops-hub-mono"));
                if (monos.Count > 0) return ((Label)monos[monos.Count - 1]).text;
            }
            return "weekly-pass-35";
        }

        // ------------------------------------------------------------------------------------------------ J10 English

        private static IEnumerator J10(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 1280, 760);
            runner.CurrentStep = "J10-00";
            ToolbarMenu languageMenu = UxFind.First(Root, element => element is ToolbarMenu menu && menu.ClassListContains("liveops-hub-language-menu")) as ToolbarMenu;
            if (languageMenu != null) ReadMenu(runner, "menu ngôn ngữ (header)", languageMenu.menu);
            // Người dùng chọn English: LiveOpsHubLanguage.Set → Changed → cửa sổ dựng lại. Không ghi EditorPrefs của máy: bỏ scope
            // ghim tiếng Việt (pref máy chưa đặt = English) rồi gọi đúng hàm dựng lại mà sự kiện Changed gọi.
            if (LanguageScope != null)
            {
                LanguageScope.Dispose();
                LanguageScope = null;
            }
            typeof(LiveOpsHubWindow).GetMethod("RebuildForLanguageChangeForTest", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Hub, null);
            yield return new WaitFrames(10);
            runner.Log("ngôn ngữ hiện tại: " + UxHub.CurrentLanguage());
            foreach (float[] size in new[] { new float[] { 1280, 760 }, new float[] { 820, 560 } })
            {
                string label = Size(size[0], size[1]);
                runner.CurrentStep = "J10-" + label;
                yield return PlaceSized(runner, size[0], size[1]);
                yield return NavigateTo(runner, "Calendar");
                yield return Snap(runner, "J10-01-calendar-" + label, "English, Calendar " + label);
                yield return SelectBar(runner, HuntBonus);
                yield return Snap(runner, "J10-02-calendar-selected-" + label, "English, chọn hunt-0916-bonus " + label);
                VisualElement bar = Bar(HuntBonus);
                if (bar != null)
                {
                    Rect bound = bar.worldBound;
                    yield return UxInput.Drag(Hub, bound.center, bound.center + new Vector2(30, 0), 10, EventModifiers.None, EventModifiers.None,
                        () => Snap(runner, "J10-03-drag-readout-" + label, "English, kéo thân chưa nhả " + label));
                    yield return Snap(runner, "J10-04-drag-toast-" + label, "English, nhả → toast " + label);
                    yield return Undo(runner);
                }
                yield return NavigateTo(runner, "Recurring rules");
                yield return Snap(runner, "J10-05-recurring-" + label, "English, Recurring rules " + label);
                TextField prefix = Root.Q<TextField>("recurring-prefix-field");
                if (prefix != null)
                {
                    yield return ReplaceText(Hub, prefix, "wp-", true);
                    yield return new WaitFrames(8);
                    yield return Snap(runner, "J10-06-recurring-draft-" + label, "English, prefix draft " + label);
                    Button cancel = Root.Q<Button>("recurring-draft-cancel");
                    if (cancel != null) yield return UxInput.Click(Hub, cancel);
                }
                yield return NavigateTo(runner, "Calendar");
            }
            VisualElement add = UxFind.Clickable(Root, "Add event", true);
            if (add != null)
            {
                yield return UxOs.Activate(runner);
                yield return OpenPopoverFrom(runner, add);
                yield return Snap(runner, "J10-07-add-event-popover", "English, popover Add event", UxHub.PopupWindow());
                if (UxHub.PopupWindow() != null) yield return UxInput.PressKey(UxHub.PopupWindow(), KeyCode.Escape, EventModifiers.None, (char)27);
                UxOs.Release(runner);
            }
        }

        // ------------------------------------------------------------------------------------------------ J11 màn khác

        private static IEnumerator J11(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "overview", 1440, 900);
            string[] titles = { "Tổng quan", "Loại event", "Kiểm lịch", "Xuất JSON" };
            string[] slugs = { "tong-quan", "loai-event", "kiem-lich", "xuat-json" };
            foreach (float[] size in new[] { new float[] { 1440, 900 }, new float[] { 820, 560 } })
            {
                string label = Size(size[0], size[1]);
                runner.CurrentStep = "J11-" + label;
                yield return PlaceSized(runner, size[0], size[1]);
                for (int index = 0; index < titles.Length; index++)
                {
                    yield return NavigateTo(runner, titles[index]);
                    yield return new WaitSeconds(0.4);
                    yield return Snap(runner, "J11-" + (index + 1).ToString("00") + "-" + slugs[index] + "-" + label, titles[index] + " " + label);
                }
            }
        }
    }
}
