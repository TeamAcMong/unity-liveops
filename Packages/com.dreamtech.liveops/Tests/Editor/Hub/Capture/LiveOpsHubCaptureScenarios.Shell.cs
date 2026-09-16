using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DreamTech.LiveOps.Tests;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng Shell (G-SHELL, W2 → G-HOSTUI W4): Hình 2 (4 HealthState + họ giai đoạn), khung với màn giữ chỗ, Hình 28
    /// khung 1–3. Mọi kịch bản mở cửa sổ hub thật bằng <c>OpenForTest</c>; kịch bản thiếu UXML dùng loader giả đường dẫn, không đổi
    /// file trên đĩa (V-16).
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        internal const string StateMarksGalleryElementName = "capture-state-marks";
        // Chữ health của Hình 28 khung 1 — ảnh tiếng Việt của ma trận 9.5 đã đạt số đo với đúng bốn câu này, đổi một ký tự là
        // phải chụp lại cả hình; bản mẫu tiếng Anh dùng đường riêng (OpenFailureSectionForTranslationSample).
        private const string FixtureWarningBadge = "1 mất tiến độ";
        private const string FixtureWarningReason = "weekly-pass đổi tiền tố khi weekly-pass-35 đang chạy";
        private const string FixtureBlockedBadge = "2 bị bỏ";
        private const string FixtureBlockedReason = "2 đợt sẽ bị game bỏ khi đọc lịch";
        // Câu lý do của bản mẫu tiếng Anh: id + mũi tên, không thuộc ngôn ngữ nào — catalog không có câu tương đương cho ca
        // fixture này, mà một câu tiếng Việt trong ảnh tiếng Anh thì người soát bản dịch đọc nhầm thành chỗ chưa dịch.
        private const string NeutralWarningReason = "weekly-pass → weekly-pass-35";
        private const int StandardWidth = 1280;
        private const int StandardHeight = 760;
        private const int StateMarksWidth = 640;
        private const int StateMarksHeight = 300;

        // G-HOSTUI (W4) — Hình 3 nửa khung, palette Hình 6, toast Hình 7, khung với phiên thật.
        internal const string ShellComponentsTableElementName = "capture-components-shell";
        private const int ShellComponentsWidth = 1080;
        private const int ShellComponentsHeight = 300;
        private const string PaletteTypingQuery = "kiem";
        private const string PaletteNoMatchQuery = "xyz";
        private const string ToastUndoMessage = "Đã dời kết thúc lava-quest-2026-09b 19/9 → 20/9 00:00 UTC";
        private const string ToastBuriedMessage = "Đã áp mẫu \"Hằng tuần thứ Hai\" cho weekly-pass";
        private const string ToastOtherActionName = "Đổi màu loại hunt";
        private const int PaletteFocusSettleFrames = 8;
        private const int ToastEditDelaySeconds = 12;

        static partial void RegisterShell(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H02StateMarks, StandardWidth, StandardHeight, OpenStateMarksGallery,
                window => window.rootVisualElement.Q(StateMarksGalleryElementName)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.HsShellSkeleton, StandardWidth, StandardHeight,
                () => LiveOpsHubWindow.OpenForTest(LiveOpsHubSections.Create(), new ManualLiveOpsHubCompilationState(false), null, LiveOpsHubSections.Ids.Overview)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H28aFailureSection, StandardWidth, StandardHeight, OpenFailureSection));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H28bMissingUxml, StandardWidth, StandardHeight,
                () => LiveOpsHubWindow.OpenForTest(LiveOpsHubSections.Create(), new ManualLiveOpsHubCompilationState(false),
                    new MissingPathsLiveOpsHubLayoutLoader(new[] { LiveOpsHubPaths.ShellUxml }), LiveOpsHubSections.Ids.Overview)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H28cCompiling, StandardWidth, StandardHeight,
                () => LiveOpsHubWindow.OpenForTest(LiveOpsHubSections.Create(), new ManualLiveOpsHubCompilationState(true), null, LiveOpsHubSections.Ids.Calendar)));

            // G-I18N (W3.5): mẫu tiếng Anh, ngoài ma trận 9.5 — dựng lại đúng hai kịch bản trên (khung sườn + card lỗi có
            // chữ finding) nhưng ghim English, để cổng gói có ảnh thật đối chiếu catalog không chỉ đọc code.
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.HsShellSkeletonEnglish, StandardWidth, StandardHeight,
                () => LiveOpsHubWindow.OpenForTest(LiveOpsHubSections.Create(), new ManualLiveOpsHubCompilationState(false), null, LiveOpsHubSections.Ids.Overview))
                .WithLanguage(LiveOpsHubLanguageId.English));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H28aFailureSectionEnglish, StandardWidth, StandardHeight,
                OpenFailureSectionForTranslationSample).WithLanguage(LiveOpsHubLanguageId.English));

            // ---------------------------------------------------------------- G-HOSTUI (W4)

            // Khung với PHIÊN THẬT (mẫu thiết kế 13/9 08:47): chip, status bar và rail đều nói về cùng một lịch, khác với
            // hs-shell-skeleton của W2 (màn giữ chỗ, không phiên).
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.HsShellWithSession, StandardWidth, StandardHeight,
                    OpenShellWithSession)
                .WithExpectedFrames(
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Rail, 196f, 0f),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Header, 0f, 26f),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Status, 0f, 20f),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Content, 1084f, 0f)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H03bComponentsShell, StandardWidth, StandardHeight,
                    () => OpenShellComponentsTable(), window => window.rootVisualElement.Q(ShellComponentsTableElementName))
                .WithMinimumSettleFrames(PseudoStateSettleFrames)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(ShellComponentsTableElementName, ShellComponentsWidth, ShellComponentsHeight)));

            // Ba trạng thái palette ([FD §3.8]). Chụp cả cửa sổ chứ không riêng panel: scrim phủ CẢ rail là điểm của hình.
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H06aPaletteEmpty, StandardWidth, StandardHeight,
                    () => OpenPaletteScenario(string.Empty))
                .WithMinimumSettleFrames(PaletteFocusSettleFrames));
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H06bPaletteTyping, StandardWidth, StandardHeight,
                    () => OpenPaletteScenario(PaletteTypingQuery))
                .WithMinimumSettleFrames(PaletteFocusSettleFrames));
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H06cPaletteNoMatch, StandardWidth, StandardHeight,
                    () => OpenPaletteScenario(PaletteNoMatchQuery))
                .WithMinimumSettleFrames(PaletteFocusSettleFrames));

            // Ba trạng thái toast (Hình 7) trong cửa sổ THẬT có phiên — khác hf-toast-bare của W2 (cửa sổ trần, toast gắn tay).
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H07aToastUndo, StandardWidth, StandardHeight,
                    () => OpenToastScenario(ToastState.Undoable))
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Toast, 0f, ToastHeight)));
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H07bToastUndoDisabled, StandardWidth, StandardHeight,
                    () => OpenToastScenario(ToastState.Buried))
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Toast, 0f, ToastHeight)));
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H07cToastRedo, StandardWidth, StandardHeight,
                    () => OpenToastScenario(ToastState.Undone))
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Toast, 0f, ToastHeight)));
        }

        // ================================================================================================ G-HOSTUI (W4)

        /// <summary>Cửa sổ hub trên phiên mẫu thiết kế đã kiểm xong — chip, status bar, rail cùng đọc một lịch.</summary>
        private static EditorWindow OpenShellWithSession()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            return LiveOpsHubWindow.OpenWithServices(services, LiveOpsHubSections.Ids.Overview);
        }

        /// <summary>
        /// Palette mở sẵn với câu tìm cho trước. Palette focus ô nhập ở khung SAU khi mở, nên kịch bản chờ thêm khung
        /// (<see cref="PaletteFocusSettleFrames"/>) để ảnh có đúng viền focus của ô.
        /// </summary>
        private static EditorWindow OpenPaletteScenario(string query)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveOpsHubWindow window = LiveOpsHubWindow.OpenWithServices(services, LiveOpsHubSections.Ids.Calendar);
            window.HubRoot?.AddToClassList(LiveOpsHubClassNames.NoMotion);
            window.OpenPalette();
            if (query.Length > 0) window.Palette.ApplyQuery(query);
            return window;
        }

        private enum ToastState
        {
            /// <summary>Hoàn tác bật: bước Undo vừa tạo còn trên đỉnh.</summary>
            Undoable,

            /// <summary>Hoàn tác khoá: đã có thao tác khác sau đó (Undo là stack chung của Editor).</summary>
            Buried,

            /// <summary>Đã hoàn tác: chữ đổi thành "Đã hoàn tác: …" với nút Làm lại.</summary>
            Undone,
        }

        /// <summary>
        /// Toast trong cửa sổ thật. Nhóm Undo là thao tác THẬT trên lịch mẫu (không ép trạng thái nút): ca "Buried" làm thêm một
        /// thao tác nữa nên bước cũ tụt khỏi đỉnh và nút tự khoá kèm "Đã có thao tác khác sau đó".
        /// </summary>
        private static EditorWindow OpenToastScenario(ToastState state)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveOpsHubWindow window = LiveOpsHubWindow.OpenWithServices(services, LiveOpsHubSections.Ids.Calendar);
            window.HubRoot?.AddToClassList(LiveOpsHubClassNames.NoMotion);

            // Nhích đồng hồ vài giây trước khi sửa: không thì câu status bar đọc thành "Lịch đã đổi lúc 08:47:00, sau lần kiểm
            // 08:47:00" — cùng một giây ở hai vế, người soát ảnh sẽ tưởng câu bị hỏng.
            ManualLiveOpsClock clock = services.Clock as ManualLiveOpsClock;
            if (clock != null) clock.Set(LiveOpsDesignSample.NowUtc.AddSeconds(ToastEditDelaySeconds));

            string message = state == ToastState.Buried ? ToastBuriedMessage : ToastUndoMessage;
            LiveOpsHubEditOutcome outcome = services.Session.Apply(MoveLavaQuestEndForToast(services), message);
            LiveOpsToastModel model = LiveOpsToastModel.ForEdit(message, outcome.UndoGroup);

            if (state == ToastState.Buried)
            {
                // Thao tác thứ hai đẩy bước cũ xuống dưới đỉnh — đúng cách người dùng làm mất quyền Hoàn tác của toast.
                services.Session.Apply(new SetRemoteConfigKeyEdit("liveops_calendar_v2"), ToastOtherActionName);
            }
            if (state == ToastState.Undone) model = model.AsUndone();

            services.Bus.ShowToast(model);
            return window;
        }

        private static LiveEventCalendarEdit MoveLavaQuestEndForToast(LiveOpsHubServices services)
        {
            FixedLiveEventEntry entry;
            services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey, out entry);
            return new ReplaceFixedEventEdit(new FixedLiveEventEntry(entry.EntryKey, entry.EventId, entry.EventType, entry.StartUtcText,
                ToastMovedEndUtc, entry.ConfigKey));
        }

        private const string ToastMovedEndUtc = "2026-09-20T00:00:00Z";

        // ---------------------------------------------------------------- h03b: nửa Shell của Hình 3

        /// <summary>
        /// Hình 3 nửa khung ([FD §2.4]): Chip (trung tính · hover · nháp a · nháp c) · Hàng rail (nghỉ · hover · active · active mất
        /// focus · focus bàn phím) · Hàng bảng (nghỉ · hover · selected · selected mất focus · active bản so). Dựng bằng ĐÚNG class
        /// của khung trong cửa sổ hub thật nên màu là màu token hai skin, không phải màu dán tay.
        /// <para>
        /// Hover/active/focus ép bằng <c>pseudoStates</c> qua reflection (SP-18) — chỉ trong code chụp. "Active mất focus" là trạng
        /// thái riêng của rail/bảng (class <c>--has-focus</c> trên container, không phải pseudo-state), nên hai cột đó dựng bằng
        /// container không mang class đó.
        /// </para>
        /// </summary>
        private static EditorWindow OpenShellComponentsTable()
        {
            FakeHubSection canvas = new FakeHubSection("shell-components", LiveOpsHubStrings.ShellWindowTitle, "Hình 3", PipelineStage.Configure)
            {
                ViewFactory = BuildShellComponentsTable,
                RequiredElementNames = new[] { ShellComponentsTableElementName },
            };
            return LiveOpsHubWindow.OpenForTest(new List<IHubSection> { canvas }, new ManualLiveOpsHubCompilationState(false), null, canvas.Id);
        }

        private static VisualElement BuildShellComponentsTable()
        {
            List<(VisualElement Element, string States)> forced = new List<(VisualElement, string)>();
            VisualElement table = CreateCanvas(ShellComponentsTableElementName, ShellComponentsWidth, ShellComponentsHeight);

            // Không có hàng tiêu đề cột dùng chung: ba hàng có ba bộ trạng thái KHÁC nhau (chip 4, rail 5, bảng 5 — ma trận 9.5),
            // nên một tiêu đề cột chung sẽ dán nhãn sai cho hàng chip. Mỗi ô tự mang tên trạng thái của nó.
            table.Add(BuildChipRow(forced));
            table.Add(BuildRailRow(forced));
            table.Add(BuildListRow(forced));

            // Gán lại mỗi lần layout đổi: focus controller/con trỏ thật có thể xoá cờ khi panel dựng lại (như h03a).
            table.RegisterCallback<GeometryChangedEvent>(geometryEvent => ApplyPseudoStates(forced));
            return table;
        }

        private static VisualElement BuildChipRow(List<(VisualElement Element, string States)> forced)
        {
            VisualElement row = CreateComponentRow(ShellRowHeight, true);
            row.Add(CreateComponentCell(CreateCaption("Chip"), ShellCaptionWidth));
            row.Add(CreateCaptionedCell("trung tính", CreateChip(ChipAssetKey, ChipAssetName, string.Empty, false, forced), ChipCellWidth));
            row.Add(CreateCaptionedCell("hover", CreateChip(ChipAssetKey, ChipAssetName, string.Empty, true, forced), ChipCellWidth));
            row.Add(CreateCaptionedCell("nháp (a)", CreateChip(string.Empty, ChipUnsavedLeft, ChipUnsavedRight, false, forced), ChipCellWidth));
            row.Add(CreateCaptionedCell("nháp (c)", CreateChip(string.Empty, LiveOpsHubStrings.ShellChipNeverPublished, string.Empty, false, forced), ChipCellWidth));
            return row;
        }

        private static VisualElement CreateChip(string key, string leftText, string rightText, bool hovered,
            List<(VisualElement Element, string States)> forced)
        {
            VisualElement chip = new VisualElement();
            chip.AddToClassList(LiveOpsHubClassNames.Chip);
            if (key.Length > 0)
            {
                Label keyLabel = new Label(key);
                keyLabel.AddToClassList(LiveOpsHubClassNames.ChipKey);
                chip.Add(keyLabel);
            }
            Label left = new Label(leftText);
            left.AddToClassList(LiveOpsHubClassNames.ChipText);
            chip.Add(left);
            if (rightText.Length > 0)
            {
                VisualElement divider = new VisualElement();
                divider.AddToClassList(LiveOpsHubClassNames.ChipDivider);
                chip.Add(divider);
                Label right = new Label(rightText);
                right.AddToClassList(LiveOpsHubClassNames.ChipText);
                chip.Add(right);
            }
            if (hovered) forced.Add((chip, PseudoStateHover));
            return chip;
        }

        private static VisualElement BuildRailRow(List<(VisualElement Element, string States)> forced)
        {
            VisualElement row = CreateComponentRow(ShellRowHeight, true);
            row.Add(CreateComponentCell(CreateCaption("Hàng rail"), ShellCaptionWidth));
            row.Add(CreateCaptionedCell("nghỉ", CreateRailRow(false, false, PseudoStateNone, forced)));
            row.Add(CreateCaptionedCell("hover", CreateRailRow(false, false, PseudoStateHover, forced)));
            row.Add(CreateCaptionedCell("active", CreateRailRow(true, true, PseudoStateNone, forced)));
            row.Add(CreateCaptionedCell("active mất focus", CreateRailRow(true, false, PseudoStateNone, forced)));
            row.Add(CreateCaptionedCell("focus bàn phím", CreateRailRow(false, false, PseudoStateFocus, forced)));
            return row;
        }

        /// <param name="containerHasFocus">Class <c>--has-focus</c> trên container rail — đây là thứ phân biệt "active" với
        /// "active mất focus", không phải pseudo-state của chính hàng ([FD §3.5]).</param>
        private static VisualElement CreateRailRow(bool isActive, bool containerHasFocus, string pseudoStates,
            List<(VisualElement Element, string States)> forced)
        {
            VisualElement container = new VisualElement();
            container.AddToClassList(LiveOpsHubClassNames.Rail);
            container.EnableInClassList(LiveOpsHubClassNames.RailHasFocus, containerHasFocus);
            // Hàng rail thật nằm trong rail 196px; ô bảng hẹp hơn nên bỏ bề rộng cố định của rail đi (hình học của code chụp).
            container.style.width = ShellCellWidth - ShellCellPadding;
            container.style.flexShrink = 0;

            VisualElement railRow = new VisualElement();
            railRow.AddToClassList(LiveOpsHubClassNames.RailRow);
            railRow.EnableInClassList(LiveOpsHubClassNames.RailRowActive, isActive);
            Label label = new Label(LiveOpsHubStrings.ShellValidationTitle);
            label.AddToClassList(LiveOpsHubClassNames.RailRowLabel);
            railRow.Add(label);
            LiveOpsStateMark mark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
            mark.SetHealth(HealthState.Blocked);
            mark.AddToClassList(LiveOpsHubClassNames.RailRowMark);
            railRow.Add(mark);
            container.Add(railRow);

            if (pseudoStates != PseudoStateNone) forced.Add((railRow, pseudoStates));
            return container;
        }

        private static VisualElement BuildListRow(List<(VisualElement Element, string States)> forced)
        {
            VisualElement row = CreateComponentRow(ShellRowHeight, true);
            row.Add(CreateComponentCell(CreateCaption("Hàng bảng"), ShellCaptionWidth));
            row.Add(CreateCaptionedCell("nghỉ", CreateListRow(false, false, false, PseudoStateNone, forced)));
            row.Add(CreateCaptionedCell("hover", CreateListRow(false, false, false, PseudoStateHover, forced)));
            row.Add(CreateCaptionedCell("selected", CreateListRow(true, true, false, PseudoStateNone, forced)));
            row.Add(CreateCaptionedCell("selected mất focus", CreateListRow(true, false, false, PseudoStateNone, forced)));
            row.Add(CreateCaptionedCell("active bản so", CreateListRow(false, false, true, PseudoStateNone, forced)));
            return row;
        }

        /// <param name="isActiveCompareSource">Hàng "đang là bản so" của bảng lịch sử dấu — dùng class hàng active, không phải selected.</param>
        private static VisualElement CreateListRow(bool isSelected, bool containerHasFocus, bool isActiveCompareSource, string pseudoStates,
            List<(VisualElement Element, string States)> forced)
        {
            VisualElement container = new VisualElement();
            container.EnableInClassList(LiveOpsHubClassNames.ListHasFocus, containerHasFocus);
            container.style.width = ShellCellWidth - ShellCellPadding;
            container.style.flexShrink = 0;

            VisualElement listRow = new VisualElement();
            listRow.AddToClassList(LiveOpsHubClassNames.FindingRow);
            // "Selected" là class của ListView gốc Unity (bảng 8.10 không có hằng riêng): lấy từ hằng của engine để ảnh dùng
            // ĐÚNG class mà bảng thật mang, không phải một chuỗi gõ tay trông giống.
            listRow.EnableInClassList(BaseVerticalCollectionView.itemSelectedVariantUssClassName, isSelected);
            listRow.EnableInClassList(LiveOpsHubClassNames.RowActive, isActiveCompareSource);
            LiveOpsHubStyle.SetSeverityStripe(listRow, HealthState.Blocked);
            Label label = new Label(ListRowText);
            label.style.marginLeft = 6;
            listRow.Add(label);
            container.Add(listRow);

            if (pseudoStates != PseudoStateNone) forced.Add((listRow, pseudoStates));
            return container;
        }

        /// <summary>Một ô của Hình 3: tên trạng thái 10px rồi tới chính thành phần — mỗi hàng có bộ trạng thái riêng nên nhãn đi theo ô.</summary>
        private static VisualElement CreateCaptionedCell(string stateCaption, VisualElement content)
        {
            return CreateCaptionedCell(stateCaption, content, ShellCellWidth);
        }

        /// <param name="width">Hàng chip chỉ có 4 ô nên ô rộng hơn — chip nháp (a) hai phần dài hơn một hàng rail, cắt là mất chữ.</param>
        private static VisualElement CreateCaptionedCell(string stateCaption, VisualElement content, float width)
        {
            VisualElement cell = new VisualElement();
            cell.style.width = width;
            cell.style.flexShrink = 0;
            cell.style.flexDirection = FlexDirection.Column;
            cell.style.justifyContent = Justify.Center;
            Label caption = new Label(stateCaption);
            caption.AddToClassList(LiveOpsHubClassNames.Caption);
            caption.style.marginBottom = 3;
            cell.Add(caption);
            cell.Add(content);
            return cell;
        }

        private const string PseudoStateNone = "";
        private const string PseudoStateHover = "Hover";
        private const string PseudoStateFocus = "Focus";
        private const float ShellCaptionWidth = 110f;
        private const float ShellCellWidth = 180f;
        private const float ChipCellWidth = 236f;
        private const float ShellCellPadding = 8f;
        private const float ShellRowHeight = 44f;
        private const string ChipAssetKey = "Lịch";
        private const string ChipAssetName = "Main.asset";
        private const string ChipUnsavedLeft = "Chưa lưu · ⌘S";
        private const string ChipUnsavedRight = "5 khác bản đã đăng";
        private const string ListRowText = "hunt-0916-bonus";


        /// <summary>
        /// Hình 28 khung 1 ([FD §4.1]): Tổng quan · Lịch (active, ném khi dựng) · Luật lặp Warning · Kiểm lịch Blocked. Exception ném thật
        /// từ màn giả để thân card là stack thật, không phải chữ dán.
        /// </summary>
        private static EditorWindow OpenFailureSection()
        {
            return OpenFailureSection(FixtureWarningBadge, FixtureWarningReason, FixtureBlockedBadge, FixtureBlockedReason);
        }

        /// <summary>
        /// Bản mẫu soát bản dịch của Hình 28 khung 1: chữ health lấy từ CHÍNH catalog (và câu lý do là id trung tính) nên ảnh
        /// tiếng Anh không lẫn chữ Việt gán cứng của fixture. Ảnh tiếng Việt vẫn đi đường <see cref="OpenFailureSection"/> với
        /// đúng chữ cũ, nên không kịch bản nào của ma trận 9.5 phải chụp lại.
        /// </summary>
        private static EditorWindow OpenFailureSectionForTranslationSample()
        {
            return OpenFailureSection(
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellRailProgressLostCountFormat, 1),
                NeutralWarningReason,
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellRailDroppedCountFormat, 2),
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellRailBlockerDroppedDetailFormat, 2));
        }

        private static EditorWindow OpenFailureSection(string warningBadge, string warningReason, string blockedBadge, string blockedReason)
        {
            List<FakeHubSection> sections = FakeHubSection.CreateRegistryShaped();
            sections[2].CreateViewException = CreateThrownException();
            // Số đếm đi cùng badge như phiên thật (6.4, V-21 CC-SHELL-5 (b)): rail cộng Counts, chữ badge chỉ để hiện.
            sections[3].Health = SectionHealth.Warning(warningBadge, warningReason).WithCounts(new LiveOpsHubFindingCounts(0, 1, 0, 0));
            sections[4].Health = SectionHealth.Blocked(blockedBadge, blockedReason).WithCounts(new LiveOpsHubFindingCounts(2, 1, 2, 1));
            return LiveOpsHubWindow.OpenForTest(FakeHubSection.AsSections(sections), new ManualLiveOpsHubCompilationState(false), null,
                LiveOpsHubSections.Ids.Calendar);
        }

        private static Exception CreateThrownException()
        {
            try
            {
                object lane = null;
                return new NullReferenceException(lane.ToString());
            }
            catch (NullReferenceException exception)
            {
                return exception;
            }
        }

        /// <summary>
        /// Hình 2 ([FD §2.4]): bảng 640 px — cột HealthState 84 · Dấu 36 · Hàng tầng 168 · Dòng section 136 · Việc cần làm 200, hàng
        /// 36 px; dưới là hàng Summary (dải tóm tắt) và hàng giai đoạn đợt (4 tag pha + tag "bị bỏ"). Cửa sổ 1280×760 rồi cắt theo
        /// bảng thay vì cửa sổ 640×300 của bảng 9.5: hub có minSize 620×420 và rail 196 px chiếm chỗ bảng; cửa sổ riêng cần file
        /// ngoài bảng quyền ghi (plan/w2/contract-changes-G-SHELL.md CC-SHELL-4). Dựng bằng đúng class của rail/hàng phát hiện trong cửa sổ hub (có token hai skin), ảnh cắt
        /// theo bảng. Kích thước hình học là style inline của code chụp — không phải UI sản phẩm.
        /// </summary>
        private static EditorWindow OpenStateMarksGallery()
        {
            FakeHubSection gallery = new FakeHubSection("state-marks", LiveOpsHubStrings.ShellWindowTitle, "Hình 2", PipelineStage.Configure)
            {
                ViewFactory = BuildStateMarksGallery,
                RequiredElementNames = new[] { StateMarksGalleryElementName },
            };
            return LiveOpsHubWindow.OpenForTest(new List<IHubSection> { gallery }, new ManualLiveOpsHubCompilationState(false), null, gallery.Id);
        }

        private static VisualElement BuildStateMarksGallery()
        {
            VisualElement table = new VisualElement { name = StateMarksGalleryElementName };
            table.style.width = StateMarksWidth;
            table.style.height = StateMarksHeight;
            table.style.flexShrink = 0;
            table.style.paddingLeft = 8;
            table.style.paddingRight = 8;
            table.style.paddingTop = 6;

            VisualElement headerRow = CreateTableRow(20f, false);
            string[] headers = { "HealthState", "Dấu", "Hàng tầng trên rail", "Dòng section", "Việc cần làm" };
            for (int column = 0; column < headers.Length; column++)
            {
                Label caption = new Label(headers[column]);
                caption.AddToClassList(LiveOpsHubClassNames.Caption);
                headerRow.Add(CreateCell(column, caption));
            }
            table.Add(headerRow);

            table.Add(CreateStateRow(HealthState.Blocked, "Blocked", LiveOpsHubStrings.StageCaptionCheck, "2 bị bỏ", LiveOpsHubStrings.ShellValidationTitle,
                "2 đợt sẽ bị game bỏ"));
            table.Add(CreateStateRow(HealthState.Warning, "Warning", LiveOpsHubStrings.StageCaptionSchedule, "1 mất tiến độ", LiveOpsHubStrings.ShellRecurringRulesTitle,
                "weekly-pass đổi tiền tố"));
            table.Add(CreateStateRow(HealthState.NotMeasured, "NotMeasured", LiveOpsHubStrings.StageCaptionRun, LiveOpsHubStrings.ShellRailNotMeasuredBadge, "Trực tiếp",
                "Không ở Play Mode"));
            table.Add(CreateStateRow(HealthState.Ok, "Ok", LiveOpsHubStrings.StageCaptionConfigure, string.Empty, LiveOpsHubStrings.ShellOverviewTitle, string.Empty));

            table.Add(CreateSummaryRow());

            VisualElement phaseRow = CreateTableRow(36f, true);
            phaseRow.Add(CreateRowCaption("Giai đoạn đợt"));
            phaseRow.Add(CreatePhaseTag(LiveEventPhase.Active, false, "Đang chạy"));
            phaseRow.Add(CreatePhaseTag(LiveEventPhase.Upcoming, false, "Sắp tới"));
            phaseRow.Add(CreatePhaseTag(LiveEventPhase.Ended, false, "Đã khép"));
            phaseRow.Add(CreatePhaseTag(LiveEventPhase.Ended, true, "Chờ hiện kết quả"));
            phaseRow.Add(CreateDroppedTag());
            table.Add(phaseRow);
            return table;
        }

        private const float SummaryClusterSpacing = 11f;

        /// <summary>
        /// Hàng "Summary" của Hình 2 ([FD §2.4]): dải cao 24, padding 0 8, viền 1, bo 3, nền toolbar; năm cụm dấu 7 + số, cách nhau
        /// 10–12 px. Màu nền/viền lấy qua class ô chặn rail (token hai skin — style inline không đọc được var()); kích thước ghi đè inline
        /// vì đây là dựng hình cho ảnh chụp, không phải UI sản phẩm.
        /// </summary>
        private static VisualElement CreateSummaryRow()
        {
            VisualElement row = CreateTableRow(36f, true);
            row.Add(CreateRowCaption("Summary"));

            VisualElement strip = new VisualElement();
            strip.AddToClassList(LiveOpsHubClassNames.RailBlocker);
            strip.style.flexDirection = FlexDirection.Row;
            strip.style.alignItems = Align.Center;
            strip.style.height = 24;
            strip.style.marginLeft = 0;
            strip.style.marginRight = 0;
            strip.style.marginTop = 0;
            strip.style.marginBottom = 0;
            strip.style.paddingTop = 0;
            strip.style.paddingBottom = 0;
            strip.style.paddingLeft = 8;
            strip.style.paddingRight = 8;
            strip.Add(CreateSummaryCluster(HealthState.Blocked, "2 bị bỏ", false));
            strip.Add(CreateSummaryCluster(HealthState.Warning, "1 mất tiến độ", true));
            strip.Add(CreateSummaryCluster(HealthState.Warning, "2 nên xem", true));
            strip.Add(CreateSummaryCluster(HealthState.NotMeasured, "1 chưa kiểm", true));
            strip.Add(CreateSummaryCluster(HealthState.Ok, "6 luật đã qua", true));
            row.Add(strip);
            return row;
        }

        private static VisualElement CreateSummaryCluster(HealthState state, string text, bool hasSpacingBefore)
        {
            VisualElement cluster = new VisualElement();
            cluster.style.flexDirection = FlexDirection.Row;
            cluster.style.alignItems = Align.Center;
            if (hasSpacingBefore) cluster.style.marginLeft = SummaryClusterSpacing;
            LiveOpsStateMark mark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
            mark.SetHealth(state);
            cluster.Add(mark);
            Label label = new Label(text);
            label.style.marginLeft = 4;
            if (state != HealthState.Ok) LiveOpsHubStyle.SetStateText(label, state);
            cluster.Add(label);
            return cluster;
        }

        /// <summary>Tag pha "bị bỏ" của Hình 2: dấu Blocked 7 + chữ blocked-text — đợt bị bỏ là hậu quả, không phải một pha của vòng đời.</summary>
        private static VisualElement CreateDroppedTag()
        {
            VisualElement tag = new VisualElement();
            tag.style.flexDirection = FlexDirection.Row;
            tag.style.alignItems = Align.Center;
            LiveOpsStateMark mark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
            mark.SetHealth(HealthState.Blocked);
            tag.Add(mark);
            Label label = new Label("bị bỏ");
            label.style.marginLeft = 6;
            LiveOpsHubStyle.SetStateText(label, HealthState.Blocked);
            tag.Add(label);
            return tag;
        }

        private static VisualElement CreateRowCaption(string text)
        {
            Label caption = new Label(text);
            caption.AddToClassList(LiveOpsHubClassNames.Caption);
            return CreateCell(0, caption);
        }

        private static readonly float[] ColumnWidths = { 84f, 36f, 168f, 136f, 200f };

        private static VisualElement CreateStateRow(HealthState state, string stateName, string stageCaption, string badgeText, string sectionTitle, string needsAction)
        {
            VisualElement row = CreateTableRow(36f, true);

            Label name = new Label(stateName);
            LiveOpsHubStyle.SetStateText(name, state);
            row.Add(CreateCell(0, name));

            LiveOpsStateMark mark = new LiveOpsStateMark();
            mark.SetHealth(state);
            row.Add(CreateCell(1, mark));

            VisualElement stageRow = new VisualElement();
            stageRow.AddToClassList(LiveOpsHubClassNames.RailStageRow);
            stageRow.style.flexGrow = 1;
            VisualElement gutter = new VisualElement();
            gutter.AddToClassList(LiveOpsHubClassNames.Gutter);
            LiveOpsStateMark stageMark = new LiveOpsStateMark();
            stageMark.SetHealth(state);
            gutter.Add(stageMark);
            stageRow.Add(gutter);
            Label caption = new Label(stageCaption);
            caption.AddToClassList(LiveOpsHubClassNames.RailStageLabel);
            stageRow.Add(caption);
            if (!string.IsNullOrEmpty(badgeText))
            {
                Label badge = new Label(badgeText);
                badge.AddToClassList(LiveOpsHubClassNames.RailBadge);
                LiveOpsHubStyle.SetStateText(badge, state);
                stageRow.Add(badge);
            }
            row.Add(CreateCell(2, stageRow));

            VisualElement sectionRow = new VisualElement();
            sectionRow.AddToClassList(LiveOpsHubClassNames.RailRow);
            sectionRow.style.flexGrow = 1;
            Label sectionLabel = new Label(sectionTitle);
            sectionLabel.AddToClassList(LiveOpsHubClassNames.RailRowLabel);
            sectionLabel.style.marginLeft = 0;
            sectionRow.Add(sectionLabel);
            LiveOpsStateMark sectionMark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
            sectionMark.SetHealth(state);
            sectionMark.AddToClassList(LiveOpsHubClassNames.RailRowMark);
            sectionMark.EnableInClassList(LiveOpsHubClassNames.RailRowMarkHidden, state == HealthState.Ok);
            sectionRow.Add(sectionMark);
            row.Add(CreateCell(3, sectionRow));

            VisualElement findingRow = new VisualElement();
            findingRow.AddToClassList(LiveOpsHubClassNames.FindingRow);
            findingRow.style.flexGrow = 1;
            if (state != HealthState.Ok)
            {
                LiveOpsHubStyle.SetSeverityStripe(findingRow, state);
                LiveOpsStateMark findingMark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Small };
                findingMark.SetHealth(state);
                findingRow.Add(findingMark);
                Label finding = new Label(needsAction);
                finding.style.marginLeft = 6;
                findingRow.Add(finding);
            }
            else
            {
                Label none = new Label("không có hàng");
                none.AddToClassList(LiveOpsHubClassNames.TextQuiet);
                findingRow.Add(none);
            }
            row.Add(CreateCell(4, findingRow));
            return row;
        }

        private static VisualElement CreatePhaseTag(LiveEventPhase phase, bool isPendingResult, string text)
        {
            VisualElement tag = new VisualElement();
            tag.style.flexDirection = FlexDirection.Row;
            tag.style.alignItems = Align.Center;
            tag.style.marginRight = 12;
            LiveOpsStateMark mark = new LiveOpsStateMark();
            mark.SetPhase(phase, isPendingResult);
            tag.Add(mark);
            Label label = new Label(text);
            label.style.marginLeft = 6;
            tag.Add(label);
            return tag;
        }

        private static VisualElement CreateTableRow(float height, bool hasTopBorder)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = height;
            row.style.flexShrink = 0;
            if (hasTopBorder)
            {
                row.style.borderTopWidth = 1;
                row.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f, 0.35f);
            }
            return row;
        }

        private static VisualElement CreateCell(int column, VisualElement content)
        {
            VisualElement cell = new VisualElement();
            cell.style.width = ColumnWidths[column];
            cell.style.flexShrink = 0;
            cell.style.flexDirection = FlexDirection.Row;
            cell.style.alignItems = Align.Center;
            cell.Add(content);
            return cell;
        }
    }
}
