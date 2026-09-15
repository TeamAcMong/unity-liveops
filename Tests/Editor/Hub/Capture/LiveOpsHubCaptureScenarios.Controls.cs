using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng Controls (G-CONTROLS, W2): <c>hc-controls-gallery</c> (gallery control dùng chung, 1080×600) và
    /// <c>h03a-components-controls</c> (Hình 3: nút, ToolbarToggle, field, card theo trạng thái, 1080×420). Cả hai dựng trong cửa sổ hub
    /// thật bằng màn giả để có token hai skin + stylesheet theo đúng thứ tự nạp, rồi cắt ảnh theo element của bảng.
    ///
    /// Trạng thái hover/nhấn/focus của Hình 3 ép bằng cách gán <c>VisualElement.pseudoStates</c> (internal) qua reflection — chỉ trong
    /// code chụp, không trong sản phẩm (SP-18 kiểm chứng: gán là đủ, màu <c>:hover</c> đổi sau 4 khung ở hai bản). Gán lại ở mỗi
    /// GeometryChangedEvent vì focus controller/con trỏ thật có thể xoá cờ khi panel dựng lại.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        internal const string ControlsGalleryElementName = "capture-controls-gallery";
        internal const string ComponentsTableElementName = "capture-components-controls";
        private const int ControlsGalleryWidth = 1080;
        private const int ControlsGalleryHeight = 600;
        private const int ComponentsTableWidth = 1080;
        private const int ComponentsTableHeight = 420;
        private const int PseudoStateSettleFrames = 8;
        private const float GalleryColumnGap = 16f;
        private const float GalleryLeftColumnWidth = 500f;
        private const float ComponentCaptionWidth = 110f;
        private const float ComponentCellWidth = 136f;
        private const float FieldLabelWidth = 62f;
        private const string LongEventIdentifier = "star-tournament-2026-10-bonus";

        private static readonly TimeSpan GalleryDeviceOffset = TimeSpan.FromHours(7);

        static partial void RegisterControls(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.HcControlsGallery, StandardWidth, StandardHeight,
                    () => OpenControlsCanvas(ControlsGalleryElementName, BuildControlsGallery),
                    window => window.rootVisualElement.Q(ControlsGalleryElementName))
                .WithMinimumSettleFrames(PseudoStateSettleFrames)
                // Chỉ khai khung dò cạnh đo được ở CẢ HAI skin. Element viền 1px (ô ngày 76, ô giờ 44, thanh chu kỳ 14) có hai cạnh cách
                // nhau 1px (nền→viền, viền→trong); measure-capture.py lấy cạnh mạnh hơn nên ra 74/42/12 ở skin có phía trong tương phản
                // hơn — dump điểm ảnh gốc đã xác nhận hộp ngoài đúng 76/44/14 (báo cáo G-CONTROLS-build). Kích thước viền ngoài được khoá
                // bằng test UI (UtcField_ValidInput_SetsValueAndDeviceLine, CycleBar_ActiveLongerThanPeriod_ThirdChildBlocked); ở đây đo
                // phần tô bên trong thanh (14 − 2 viền = 12), có cạnh tô→viền rõ ở hai skin.
                .WithExpectedFrames(
                    new LiveOpsHubCaptureExpectedFrame(ControlsGalleryElementName, ControlsGalleryWidth, ControlsGalleryHeight),
                    new LiveOpsHubCaptureExpectedFrame("gallery-cycle-weekly-run", 0f, 12f),
                    new LiveOpsHubCaptureExpectedFrame("gallery-json-overview", 10f, 0f)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H03aComponentsControls, StandardWidth, StandardHeight,
                    () => OpenControlsCanvas(ComponentsTableElementName, BuildComponentsTable),
                    window => window.rootVisualElement.Q(ComponentsTableElementName))
                .WithMinimumSettleFrames(PseudoStateSettleFrames)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(ComponentsTableElementName, ComponentsTableWidth, ComponentsTableHeight)));
        }

        private static EditorWindow OpenControlsCanvas(string elementName, Func<VisualElement> build)
        {
            FakeHubSection canvas = new FakeHubSection("controls-capture", LiveOpsHubStrings.ShellWindowTitle, "G-CONTROLS", PipelineStage.Configure)
            {
                ViewFactory = build,
                RequiredElementNames = new[] { elementName },
            };
            return LiveOpsHubWindow.OpenForTest(new List<IHubSection> { canvas }, new ManualLiveOpsHubCompilationState(false), null, canvas.Id);
        }

        // ------------------------------------------------------------------------------------------------ hc-controls-gallery

        private static VisualElement BuildControlsGallery()
        {
            VisualElement gallery = CreateCanvas(ControlsGalleryElementName, ControlsGalleryWidth, ControlsGalleryHeight);
            gallery.style.flexDirection = FlexDirection.Row;

            VisualElement left = new VisualElement();
            left.style.width = GalleryLeftColumnWidth;
            left.style.flexShrink = 0;
            gallery.Add(left);

            left.Add(CreateCaption("LiveOpsUtcDateTimeField — đọc được · không đọc được · trống (gợi ý)"));
            LiveOpsUtcDateTimeField valid = CreateUtcField("Bắt đầu");
            valid.SetRawTextWithoutNotify("2026-09-16", "12:00");
            valid.DateInput.name = "gallery-utc-valid-date";
            valid.TimeInput.name = "gallery-utc-valid-time";
            left.Add(valid);
            LiveOpsUtcDateTimeField broken = CreateUtcField("Kết thúc");
            broken.SetRawTextWithoutNotify("2026-10-3", "00:00");
            left.Add(broken);
            left.Add(CreateUtcField("Neo"));

            left.Add(CreateCaption("LiveOpsRuleCycleBar — liền mạch · có nghỉ · chạy lâu hơn chu kỳ"));
            left.Add(CreateCycleRow("gallery-cycle-weekly", 168, 168, 0, "weekly-pass · chạy 7 ngày · liền mạch, không nghỉ"));
            left.Add(CreateCycleRow("gallery-cycle-sky-race", 24, 20, 1, "sky-race · chạy 20 giờ | nghỉ 4 giờ"));
            left.Add(CreateCycleRow("gallery-cycle-overflow", 24, 30, 5, "chạy 30 giờ trong chu kỳ 24 giờ — đợt sau mở trước khi đợt trước khép"));

            left.Add(CreateCaption("LiveOpsTabStrip — loại trừ nhau · tab khoá có lý do"));
            LiveOpsTabStrip draftTabs = new LiveOpsTabStrip { name = "gallery-tabs-draft", Choices = "Nháp|Bản đã đăng" };
            left.Add(draftTabs);
            LiveOpsTabStrip zoomTabs = new LiveOpsTabStrip { name = "gallery-tabs-zoom", Choices = "Ngày|3 tuần|Tháng" };
            zoomTabs.SetSelectedIndexWithoutNotify(1);
            zoomTabs.SetChoiceEnabled(2, false, "Chưa có dữ liệu tháng");
            zoomTabs.style.marginTop = 4;
            left.Add(zoomTabs);

            left.Add(CreateCaption("Cắt giữa bằng USS (SP-6) — 80 · 120 · 160 px"));
            VisualElement elideRow = new VisualElement();
            elideRow.style.flexDirection = FlexDirection.Row;
            foreach (int width in new[] { 80, 120, 160 })
            {
                Label elided = new Label(LongEventIdentifier) { name = "gallery-elide-" + width };
                elided.AddToClassList(LiveOpsHubClassNames.TextElideMiddle);
                elided.AddToClassList(LiveOpsHubClassNames.Mono);
                elided.AddToClassList(LiveOpsHubClassNames.Tag);
                elided.style.width = width;
                elided.style.marginRight = 12;
                elideRow.Add(elided);
            }
            left.Add(elideRow);

            VisualElement right = new VisualElement();
            right.style.flexGrow = 1;
            right.style.marginLeft = GalleryColumnGap;
            gallery.Add(right);
            right.Add(CreateCaption("LiveOpsJsonView — lề ✕ ! + ~ · chú thích · dải tổng quan"));
            right.Add(CreateDesignSampleJsonView());
            return gallery;
        }

        private static LiveOpsUtcDateTimeField CreateUtcField(string label)
        {
            LiveOpsUtcDateTimeField field = new LiveOpsUtcDateTimeField(label);
            field.SetDeviceOffset(GalleryDeviceOffset);
            field.labelElement.style.minWidth = FieldLabelWidth;
            field.labelElement.style.width = FieldLabelWidth;
            field.style.marginBottom = 6;
            return field;
        }

        private static VisualElement CreateCycleRow(string name, int periodHours, int activeHours, int colorSlot, string caption)
        {
            VisualElement row = new VisualElement();
            row.style.marginBottom = 6;
            LiveOpsRuleCycleBar bar = new LiveOpsRuleCycleBar { name = name, PeriodHours = periodHours, ActiveHours = activeHours };
            bar.SetColorSlot(colorSlot);
            bar.Run.name = name + "-run";
            bar.style.width = 420;
            row.Add(bar);
            Label note = new Label(caption);
            note.AddToClassList(LiveOpsHubClassNames.MetricFoot);
            row.Add(note);
            return row;
        }

        /// <summary>JSON mẫu 13/9 (65 dòng, [SD2 §3.6]): ! dòng 7, ~ dòng 34, + khối 36–42, ✕ dòng 39 và 53 kèm câu của thiết kế.</summary>
        private static LiveOpsJsonView CreateDesignSampleJsonView()
        {
            LiveEventCalendarJsonText formatted = LiveEventCalendarJsonWriter.Write(DreamTech.LiveOps.Tests.LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version2);
            LiveOpsJsonView view = new LiveOpsJsonView { name = "gallery-json-view" };
            view.Overview.name = "gallery-json-overview";
            view.style.height = 520;
            view.style.flexGrow = 0;
            view.SetText(formatted.Text, LiveEventCalendarJsonWriter.WriteSingleLine(formatted));
            view.SetAnnotations(new[]
            {
                new LiveOpsJsonLineAnnotation(7, HealthState.Warning, "đổi id đợt đang chạy"),
                new LiveOpsJsonLineAnnotation(34, HealthState.Ok, "hunt_v1 → mặc định của loại"),
                new LiveOpsJsonLineAnnotation(39, HealthState.Blocked, "chồng 12 giờ"),
                new LiveOpsJsonLineAnnotation(53, HealthState.Blocked, "không đọc được — đợt bị bỏ"),
            });
            List<LiveOpsJsonLineChange> changes = new List<LiveOpsJsonLineChange> { new LiveOpsJsonLineChange(34, LiveOpsJsonLineChangeKind.Modified) };
            for (int lineNumber = 36; lineNumber <= 42; lineNumber++) changes.Add(new LiveOpsJsonLineChange(lineNumber, LiveOpsJsonLineChangeKind.Added));
            view.SetLineChanges(changes);
            return view;
        }

        // ------------------------------------------------------------------------------------------------ h03a-components-controls

        private static VisualElement BuildComponentsTable()
        {
            VisualElement table = CreateCanvas(ComponentsTableElementName, ComponentsTableWidth, ComponentsTableHeight);
            List<(VisualElement Element, string States)> forced = new List<(VisualElement Element, string States)>();

            VisualElement header = CreateComponentRow(20f, false);
            header.Add(CreateComponentCell(CreateCaption("Thành phần"), ComponentCaptionWidth));
            table.Add(header);

            VisualElement buttons = CreateComponentRow(40f, true);
            buttons.Add(CreateComponentCell(CreateCaption("Button"), ComponentCaptionWidth));
            buttons.Add(CreateComponentCell(CreateHubButton("Kiểm lại"), ComponentCellWidth));
            Button hover = CreateHubButton("Kiểm lại");
            forced.Add((hover, "Hover"));
            buttons.Add(CreateComponentCell(hover, ComponentCellWidth));
            Button pressed = CreateHubButton("Kiểm lại");
            forced.Add((pressed, "Active, Hover"));
            buttons.Add(CreateComponentCell(pressed, ComponentCellWidth));
            Button focused = CreateHubButton("Kiểm lại");
            forced.Add((focused, "Focus"));
            buttons.Add(CreateComponentCell(focused, ComponentCellWidth));
            LiveOpsButtonSlot blocked = new LiveOpsButtonSlot(CreateHubButton("Copy JSON"));
            blocked.SetEnabledWithReason(false, "Chặn: 2 đợt bị bỏ");
            buttons.Add(CreateComponentCell(blocked, 180f));
            Button primary = CreateHubButton("Kiểm lại");
            primary.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            buttons.Add(CreateComponentCell(primary, 110f));
            Button danger = CreateHubButton("Xoá đợt…");
            danger.AddToClassList(LiveOpsHubClassNames.ButtonDanger);
            buttons.Add(CreateComponentCell(danger, 110f));
            table.Add(buttons);

            VisualElement toggles = CreateComponentRow(40f, true);
            toggles.Add(CreateComponentCell(CreateCaption("ToolbarToggle"), ComponentCaptionWidth));
            toggles.Add(CreateComponentCell(CreateToolbarWith(new ToolbarToggle { text = "3 tuần" }), ComponentCellWidth));
            ToolbarToggle hoverToggle = new ToolbarToggle { text = "3 tuần" };
            forced.Add((hoverToggle, "Hover"));
            toggles.Add(CreateComponentCell(CreateToolbarWith(hoverToggle), ComponentCellWidth));
            ToolbarToggle checkedToggle = new ToolbarToggle { text = "3 tuần" };
            checkedToggle.SetValueWithoutNotify(true);
            toggles.Add(CreateComponentCell(CreateToolbarWith(checkedToggle), ComponentCellWidth));
            ToolbarToggle disabledToggle = new ToolbarToggle { text = "3 tuần" };
            disabledToggle.SetEnabled(false);
            toggles.Add(CreateComponentCell(CreateToolbarWith(disabledToggle), ComponentCellWidth));
            LiveOpsTabStrip strip = new LiveOpsTabStrip { Choices = "Ngày|3 tuần|Tháng" };
            strip.SetSelectedIndexWithoutNotify(1);
            toggles.Add(CreateComponentCell(strip, 200f));
            table.Add(toggles);

            VisualElement fields = CreateComponentRow(74f, true);
            fields.Add(CreateComponentCell(CreateCaption("Field"), ComponentCaptionWidth));
            fields.Add(CreateComponentCell(CreateConfigKeyField(), 230f));
            TextField focusedField = CreateConfigKeyField();
            forced.Add((focusedField, "Focus"));
            forced.Add((focusedField.Q(className: TextField.inputUssClassName), "Focus"));
            fields.Add(CreateComponentCell(focusedField, 230f));
            LiveOpsUtcDateTimeField errorField = CreateUtcField("Kết thúc");
            errorField.SetRawTextWithoutNotify("2026-10-3", "00:00");
            errorField.SetErrorText("Ô ngày cần dạng 2026-10-03 (yyyy-MM-dd).");
            fields.Add(CreateComponentCell(errorField, 380f));
            table.Add(fields);

            VisualElement cards = CreateComponentRow(96f, true);
            cards.Add(CreateComponentCell(CreateCaption("Card"), ComponentCaptionWidth));
            cards.Add(CreateComponentCell(CreatePassedCard(false, false, forced), 290f));
            cards.Add(CreateComponentCell(CreatePassedCard(true, false, forced), 290f));
            cards.Add(CreateComponentCell(CreatePassedCard(true, true, forced), 290f));
            table.Add(cards);

            table.RegisterCallback<GeometryChangedEvent>(geometryEvent => ApplyPseudoStates(forced));
            return table;
        }

        private static Button CreateHubButton(string text)
        {
            Button button = new Button { text = text };
            button.AddToClassList(LiveOpsHubClassNames.Button);
            button.AddToClassList(LiveOpsHubClassNames.ButtonFirst);
            return button;
        }

        private static Toolbar CreateToolbarWith(ToolbarToggle toggle)
        {
            Toolbar toolbar = new Toolbar();
            toolbar.Add(toggle);
            return toolbar;
        }

        private static TextField CreateConfigKeyField()
        {
            TextField field = new TextField("Config key") { value = "hunt_bonus" };
            // Mono chỉ cho giá trị (ASCII id) — nhãn giữ font Inter như Hình 3.
            field.Q(className: TextField.inputUssClassName).AddToClassList(LiveOpsHubClassNames.Mono);
            field.labelElement.style.minWidth = FieldLabelWidth;
            field.labelElement.style.width = FieldLabelWidth;
            return field;
        }

        /// <summary>Card "Đã qua · 6 luật" ([FD §2.16]): mở (chevron xuống + thân 10px 0,82) · thu gọn (chevron phải) · header bấm được + hover.</summary>
        private static VisualElement CreatePassedCard(bool collapsed, bool hovered, List<(VisualElement Element, string States)> forced)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList(LiveOpsHubClassNames.Card);
            card.EnableInClassList(LiveOpsHubClassNames.CardCollapsed, collapsed);
            card.style.width = 270;

            VisualElement cardHeader = new VisualElement();
            cardHeader.AddToClassList(LiveOpsHubClassNames.CardHeader);
            if (collapsed) cardHeader.AddToClassList(LiveOpsHubClassNames.CardHeaderClickable);
            cardHeader.Add(new LiveOpsChevron(collapsed ? LiveOpsChevron.ChevronDirection.Right : LiveOpsChevron.ChevronDirection.Down));
            Label title = new Label("Đã qua");
            title.style.marginLeft = 6;
            title.style.flexGrow = 1;
            cardHeader.Add(title);
            Label count = new Label("6 luật");
            count.style.opacity = hovered ? 1f : 0.7f;
            cardHeader.Add(count);
            card.Add(cardHeader);
            if (hovered) forced.Add((cardHeader, "Hover"));

            if (!collapsed)
            {
                Label body = new Label("duplicate-event-id · end-before-start · invalid-identifier…");
                body.AddToClassList(LiveOpsHubClassNames.MetricFoot);
                body.style.paddingLeft = 8;
                body.style.paddingTop = 4;
                body.style.paddingBottom = 4;
                card.Add(body);
            }
            return card;
        }

        private static void ApplyPseudoStates(List<(VisualElement Element, string States)> forced)
        {
            PropertyInfo pseudoStates = typeof(VisualElement).GetProperty("pseudoStates", BindingFlags.NonPublic | BindingFlags.Instance);
            if (pseudoStates == null)
            {
                Debug.LogWarning("LIVEOPS CAPTURE: VisualElement.pseudoStates không còn ở bản Unity này — ảnh h03a thiếu hover/nhấn/focus (SP-18, R-35)");
                return;
            }
            foreach ((VisualElement element, string states) in forced)
            {
                if (element == null) continue;
                object value = Enum.Parse(pseudoStates.PropertyType, states);
                if (Equals(pseudoStates.GetValue(element), value)) continue;
                pseudoStates.SetValue(element, value);
                element.MarkDirtyRepaint();
            }
        }

        // ------------------------------------------------------------------------------------------------ dựng hình dùng chung

        /// <summary>Khung ảnh cố định bề rộng/cao của bảng 9.5 — style inline là hình học của code chụp, không phải UI sản phẩm.</summary>
        private static VisualElement CreateCanvas(string name, int width, int height)
        {
            VisualElement canvas = new VisualElement { name = name };
            canvas.style.width = width;
            canvas.style.height = height;
            canvas.style.flexShrink = 0;
            canvas.style.paddingLeft = 8;
            canvas.style.paddingRight = 8;
            canvas.style.paddingTop = 6;
            canvas.style.overflow = Overflow.Hidden;
            return canvas;
        }

        private static Label CreateCaption(string text)
        {
            Label caption = new Label(text);
            caption.AddToClassList(LiveOpsHubClassNames.Caption);
            caption.style.marginTop = 6;
            caption.style.marginBottom = 4;
            return caption;
        }

        private static VisualElement CreateComponentRow(float height, bool hasTopBorder)
        {
            VisualElement row = CreateTableRow(height, hasTopBorder);
            row.style.alignItems = Align.Center;
            return row;
        }

        private static VisualElement CreateComponentCell(VisualElement content, float width)
        {
            VisualElement cell = new VisualElement();
            cell.style.width = width;
            cell.style.flexShrink = 0;
            cell.style.flexDirection = FlexDirection.Row;
            cell.style.alignItems = Align.Center;
            cell.style.paddingRight = 8;
            cell.Add(content);
            return cell;
        }
    }
}
