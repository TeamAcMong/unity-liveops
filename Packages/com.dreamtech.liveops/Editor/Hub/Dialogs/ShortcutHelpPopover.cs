using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Một hàng của bảng phím: câu mô tả lệnh + nhãn phím. Bất biến — model dựng xong là chỉ đọc.</summary>
    internal sealed class ShortcutHelpRow
    {
        internal ShortcutHelpRow(string description, string keyLabel, bool isUnbound)
        {
            Description = description ?? string.Empty;
            KeyLabel = keyLabel ?? string.Empty;
            IsUnbound = isUnbound;
        }

        public string Description { get; }

        public string KeyLabel { get; }

        /// <summary>Lệnh có trong <see cref="ShortcutManager"/> nhưng người dùng đã gỡ phím — nhãn là câu "chưa gán phím".</summary>
        public bool IsUnbound { get; }
    }

    /// <summary>Một nhóm của bảng phím (tiêu đề + hàng) — hai nhóm vì hub có hai cơ chế phím khác nhau (SP-7b).</summary>
    internal sealed class ShortcutHelpGroup
    {
        internal ShortcutHelpGroup(string title, IReadOnlyList<ShortcutHelpRow> rows)
        {
            Title = title ?? string.Empty;
            Rows = rows ?? Array.Empty<ShortcutHelpRow>();
        }

        public string Title { get; }

        public IReadOnlyList<ShortcutHelpRow> Rows { get; }
    }

    /// <summary>
    /// Nội dung bảng phím, tách khỏi view để test đọc được không cần panel. Hai nhóm:
    /// <list type="number">
    /// <item><b>Phím tắt của cửa sổ hub</b> — duyệt từ <see cref="ShortcutManager"/> (chứ không từ hằng
    /// <c>LiveOpsHubShortcuts.AllShortcutIds</c>): nhãn phím phải là phím THẬT trong profile của người dùng, và một lệnh mới
    /// đăng ký mà quên cập nhật bảng hằng thì vẫn phải hiện ra chứ không biến mất khỏi hướng dẫn.</item>
    /// <item><b>Phím một ký tự của timeline</b> — hằng chữ, vì SP-7b cấm đăng ký chúng qua <see cref="ShortcutManager"/>: chúng
    /// do <c>LiveOpsTimelineElement.OnKeyDown</c> xử lý nên không có id nào để tra.</item>
    /// </list>
    /// </summary>
    internal static class ShortcutHelpModel
    {
        /// <summary>Tiền tố id của mọi phím tắt hub (8.7) — ASCII và không đổi sau phát hành (PD-15).</summary>
        internal const string HubShortcutIdPrefix = "LiveOps Hub/";

        /// <summary>
        /// Id hub đang có trong <see cref="ShortcutManager"/>, xếp theo thứ tự bảng 8.7 trước, id lạ (bản sau thêm mà quên
        /// bảng hằng) xếp sau theo đúng thứ tự <see cref="ShortcutManager"/> trả về.
        /// </summary>
        internal static IReadOnlyList<string> HubShortcutIds()
        {
            HashSet<string> available = new HashSet<string>(StringComparer.Ordinal);
            foreach (string shortcutId in ShortcutManager.instance.GetAvailableShortcutIds())
            {
                if (shortcutId != null && shortcutId.StartsWith(HubShortcutIdPrefix, StringComparison.Ordinal)) available.Add(shortcutId);
            }

            List<string> ordered = new List<string>(available.Count);
            foreach (string shortcutId in LiveOpsHubShortcuts.AllShortcutIds)
            {
                if (available.Remove(shortcutId)) ordered.Add(shortcutId);
            }
            foreach (string shortcutId in ShortcutManager.instance.GetAvailableShortcutIds())
            {
                if (available.Remove(shortcutId)) ordered.Add(shortcutId);
            }
            return ordered;
        }

        /// <param name="sectionTitles">
        /// Tiêu đề màn theo đúng thứ tự registry — ⌘1…⌘6 đi theo VỊ TRÍ, nên câu "Đi tới màn Tổng quan" chỉ đúng khi đọc từ
        /// registry đang chạy. Danh sách rỗng hoặc ngắn hơn thì hàng đó rơi về số vị trí, không bịa tên màn.
        /// </param>
        internal static IReadOnlyList<ShortcutHelpGroup> Build(IReadOnlyList<string> sectionTitles)
        {
            List<ShortcutHelpGroup> groups = new List<ShortcutHelpGroup>(2);
            groups.Add(new ShortcutHelpGroup(LiveOpsHubStrings.ShortcutHelpWindowGroupTitle, WindowRows(sectionTitles)));
            groups.Add(new ShortcutHelpGroup(LiveOpsHubStrings.ShortcutHelpTimelineGroupTitle, TimelineRows()));
            return groups;
        }

        private static IReadOnlyList<ShortcutHelpRow> WindowRows(IReadOnlyList<string> sectionTitles)
        {
            IReadOnlyList<string> shortcutIds = HubShortcutIds();
            List<ShortcutHelpRow> rows = new List<ShortcutHelpRow>(shortcutIds.Count);
            for (int index = 0; index < shortcutIds.Count; index++)
            {
                string shortcutId = shortcutIds[index];
                string keyLabel = LiveOpsHubKeyLabels.For(shortcutId);
                bool isUnbound = keyLabel.Length == 0;
                rows.Add(new ShortcutHelpRow(DescriptionOf(shortcutId, sectionTitles),
                    isUnbound ? LiveOpsHubStrings.ShortcutHelpUnboundKey : keyLabel, isUnbound));
            }
            return rows;
        }

        /// <summary>
        /// Câu của lệnh theo ngôn ngữ đang chọn (PD-15: <c>displayName</c> trong thuộc tính C# là hằng ASCII nên không theo
        /// ngôn ngữ được). Id lạ — bản sau thêm lệnh mà chưa thêm câu — in phần sau tiền tố thay vì bỏ hàng: người đọc vẫn
        /// thấy phím đó tồn tại, và chữ ASCII lộ ra là lời nhắc thêm câu.
        /// <para>
        /// <c>internal</c> chứ không <c>private</c>: trạng thái "id lạ" là trạng thái mà test
        /// <c>ShortcutHelp_ListsEveryHubShortcutIdFromShortcutManager</c> CẤM tồn tại trong hồ sơ phím thật, nên nhánh dự
        /// phòng chỉ đi qua được khi test gọi thẳng hàm này với một id giả — không mở ra thì nó là code không ai chạy.
        /// </para>
        /// </summary>
        internal static string DescriptionOf(string shortcutId, IReadOnlyList<string> sectionTitles)
        {
            if (string.Equals(shortcutId, LiveOpsHubShortcuts.OpenPaletteId, StringComparison.Ordinal)) return LiveOpsHubStrings.ShellShortcutOpenPalette;
            if (string.Equals(shortcutId, LiveOpsHubShortcuts.SaveCalendarId, StringComparison.Ordinal)) return LiveOpsHubStrings.ShellShortcutSaveCalendar;
            if (string.Equals(shortcutId, LiveOpsHubShortcuts.CheckAllId, StringComparison.Ordinal)) return LiveOpsHubStrings.ShellShortcutCheckAll;
            if (string.Equals(shortcutId, LiveOpsHubShortcuts.NextFindingId, StringComparison.Ordinal)) return LiveOpsHubStrings.ShellShortcutNextFinding;
            if (string.Equals(shortcutId, LiveOpsHubShortcuts.PreviousFindingId, StringComparison.Ordinal)) return LiveOpsHubStrings.ShellShortcutPreviousFinding;

            for (int position = 1; position <= LiveOpsHubShortcuts.GoToSectionCount; position++)
            {
                if (!string.Equals(shortcutId, LiveOpsHubShortcuts.GoToSectionId(position), StringComparison.Ordinal)) continue;
                string sectionName = sectionTitles != null && position <= sectionTitles.Count
                    ? sectionTitles[position - 1]
                    : position.ToString(CultureInfo.InvariantCulture);
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellShortcutGoToSectionFormat, sectionName);
            }

            return shortcutId.Substring(HubShortcutIdPrefix.Length);
        }

        /// <summary>
        /// Các hàng phím của timeline, chép đúng nhánh <c>switch</c> trong <c>LiveOpsTimelineElement.OnKeyDown</c>. Bảng này
        /// là thứ DUY NHẤT nói ra chúng: chúng không nằm trong cửa sổ Shortcuts của Unity, nên không có bảng nào khác để tra.
        /// <para>
        /// Phím bổ trợ đi theo NỀN TẢNG y như phím lệnh: macOS in ký hiệu (⌘ ⇧ ⌥), chỗ khác in chữ ("Ctrl", "Shift", "Alt").
        /// Ghi cứng ký hiệu của Mac thì người dùng Windows/Linux đọc "⌥ ← →" và không biết phải bấm gì.
        /// </para>
        /// <para>
        /// Hàng cuối (<c>KeyCode.Menu</c>) CHỈ có ngoài macOS: bàn phím Apple không có phím Menu, nên in hàng đó trên macOS là
        /// hứa một phím người đọc không bấm được — và bảng này thì không có nguồn nào khác để đối chiếu lại.
        /// </para>
        /// </summary>
        private static IReadOnlyList<ShortcutHelpRow> TimelineRows()
        {
            bool isMacEditor = Application.platform == RuntimePlatform.OSXEditor;
            string actionKey = isMacEditor ? LiveOpsHubStrings.TimelineActionKeyMac : LiveOpsHubStrings.TimelineActionKeyOther;
            string shiftKey = isMacEditor ? LiveOpsHubStrings.ShortcutHelpShiftKeyMac : LiveOpsHubStrings.KeyLabelShift;
            string optionKey = isMacEditor ? LiveOpsHubStrings.ShortcutHelpOptionKeyMac : LiveOpsHubStrings.KeyLabelOption;
            // Ký hiệu Mac dính liền nhau ("⌥⇧") đúng như Unity in; tên chữ thì phải tách bằng khoảng trắng, không thì ra "AltShift".
            string optionShiftKey = isMacEditor ? optionKey + shiftKey : optionKey + " " + shiftKey;

            List<ShortcutHelpRow> rows = new List<ShortcutHelpRow>(9);
            rows.Add(TimelineRow(LiveOpsHubStrings.ShortcutHelpTimelineNudgeByStep, LiveOpsHubStrings.ShortcutHelpKeyArrowsLeftRight));
            rows.Add(TimelineRow(LiveOpsHubStrings.ShortcutHelpTimelineNudgeByDay, ModifierArrowsLeftRight(shiftKey)));
            rows.Add(TimelineRow(LiveOpsHubStrings.ShortcutHelpTimelineMoveEndEdge, ModifierArrowsLeftRight(optionKey)));
            rows.Add(TimelineRow(LiveOpsHubStrings.ShortcutHelpTimelineMoveStartEdge, ModifierArrowsLeftRight(optionShiftKey)));
            rows.Add(TimelineRow(LiveOpsHubStrings.ShortcutHelpTimelineSelectAdjacentLane,
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShortcutHelpKeyActionArrowsUpDownFormat, actionKey)));
            rows.Add(TimelineRow(LiveOpsHubStrings.ShortcutHelpTimelineFrameAll, LiveOpsHubStrings.ShortcutHelpKeyFrameAll));
            rows.Add(TimelineRow(LiveOpsHubStrings.ShortcutHelpTimelineZoom, LiveOpsHubStrings.ShortcutHelpKeyZoom));
            rows.Add(TimelineRow(LiveOpsHubStrings.ShortcutHelpTimelineCancelDrag, LiveOpsHubStrings.ShortcutHelpKeyEscape));
            // Phím Menu chỉ có trên bàn phím Windows/Linux — test đọc lại điều kiện này từ Application.platform, không từ đây.
            if (!isMacEditor)
            {
                rows.Add(TimelineRow(LiveOpsHubStrings.ShortcutHelpTimelineContextMenu, LiveOpsHubStrings.ShortcutHelpKeyContextMenu));
            }
            return rows;
        }

        /// <summary>Nhãn "&lt;phím bổ trợ&gt; ← →" — phím bổ trợ đã dựng sẵn theo nền tảng trước khi vào đây.</summary>
        private static string ModifierArrowsLeftRight(string modifierKey)
        {
            return string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ShortcutHelpKeyModifierArrowsLeftRightFormat, modifierKey);
        }

        /// <summary>
        /// Một hàng timeline. Nhãn phím đi qua <see cref="LiveOpsHubKeyLabels.ReplaceMissingGlyphs"/> — bộ đó CHỈ đổi bốn ký
        /// hiệu ⇧ ⌥ ⌘ ⌫ sang chữ khi font Label của Editor thiếu chúng; mũi tên ← → ↑ ↓ và dấu − thì không đổi. Đủ dùng vì
        /// [API §12.3] đã đo: font Label mặc định (Inter-Regular SDF) có đủ cả họ mũi tên lẫn bốn ký hiệu đó ở hai bản Unity —
        /// đó cũng là lý do cột phím KHÔNG đeo class Mono (RobotoMono thiếu đúng những ký tự này).
        /// </summary>
        private static ShortcutHelpRow TimelineRow(string description, string keyLabel)
        {
            return new ShortcutHelpRow(description, LiveOpsHubKeyLabels.ReplaceMissingGlyphs(keyLabel), false);
        }
    }

    /// <summary>
    /// Mở cửa sổ Shortcuts của Unity. SP-7 đã kiểm chứng: <c>ExecuteMenuItem("Edit/Shortcuts...")</c> trả <b>False</b> trên
    /// macOS ở cả hai bản ("no menu named") vì trên macOS mục này nằm ở menu ứng dụng, nên đường menu chỉ là đường DỰ PHÒNG.
    /// Đường chính là kiểu nội bộ <c>UnityEditor.ShortcutManagement.ShortcutManagerWindow</c> qua reflection.
    /// <para>
    /// Vì sao không ném khi hỏng: hai đường đều đi qua thứ Unity không hứa (kiểu internal, đường menu theo nền tảng). Hỏng thì
    /// popover in lý do thành chữ cạnh nút (SP-3) — bảng phím vẫn đọc được, chỉ là không đổi phím từ đây được.
    /// </para>
    /// </summary>
    internal static class LiveOpsShortcutManagerWindow
    {
        /// <summary>Kiểu cửa sổ Shortcuts; tên đủ cả assembly vì nó là kiểu internal của UnityEditor.</summary>
        internal const string WindowTypeName = "UnityEditor.ShortcutManagement.ShortcutManagerWindow, UnityEditor";

        /// <summary>Đường menu dự phòng — đúng trên Windows/Linux; trên macOS Unity trả False (SP-7).</summary>
        internal const string MenuItemPath = "Edit/Shortcuts...";

        private const string OpenMethodName = "Open";

        internal static Type ResolveWindowType()
        {
            return Type.GetType(WindowTypeName, false);
        }

        /// <summary>true khi có ít nhất một cửa sổ Shortcuts đang mở (kể cả cửa sổ người dùng mở từ trước).</summary>
        internal static bool IsOpen()
        {
            Type windowType = ResolveWindowType();
            if (windowType == null) return false;
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(windowType);
            return windows != null && windows.Length > 0;
        }

        /// <summary>Mở cửa sổ; trả false khi cả hai đường đều không mở được (popover in lý do).</summary>
        internal static bool TryOpen()
        {
            Type windowType = ResolveWindowType();
            if (windowType != null && TryOpenWindowType(windowType) && IsOpen()) return true;
            // Dự phòng: đường menu. Trên macOS trả False ngay, nên nó không bao giờ che mất lỗi của đường chính.
            return EditorApplication.ExecuteMenuItem(MenuItemPath) && IsOpen();
        }

        private static bool TryOpenWindowType(Type windowType)
        {
            try
            {
                MethodInfo openMethod = windowType.GetMethod(OpenMethodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (openMethod != null)
                {
                    openMethod.Invoke(null, null);
                    return true;
                }
                EditorWindow window = EditorWindow.GetWindow(windowType);
                return window != null;
            }
            catch (Exception openFailure)
            {
                // Kiểu internal đổi giữa hai bản là chuyện có thật; nuốt im lặng thì nút thành nút chết không ai dò ra.
                Debug.LogWarning(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.ShortcutHelpOpenShortcutManagerFailedLogFormat, openFailure.Message));
                return false;
            }
        }
    }

    /// <summary>
    /// Popover "Hiện hướng dẫn phím tắt" của menu ⋮ ([FD §3.3], ma trận 9.5 ảnh <c>ho-shortcut-help</c>) — gói P1-lùi
    /// G-OPT-SHORTCUTHELP (W6). Ba luật:
    /// <list type="bullet">
    /// <item><b>Đọc phím thật, không chép tay</b> — nhóm cửa sổ duyệt <see cref="ShortcutManager"/> và tra nhãn qua
    /// <see cref="LiveOpsHubKeyLabels"/>, nên người dùng đổi phím trong profile thì bảng này đổi theo.</item>
    /// <item><b>Nói ra cả họ phím không có trong cửa sổ Shortcuts</b> — phím một ký tự của timeline (SP-7b) không đăng ký
    /// qua <see cref="ShortcutManager"/>, nên nếu bảng này không kể thì không chỗ nào kể.</item>
    /// <item><b>Popover không sửa gì</b> — nó chỉ đọc; đổi phím là việc của cửa sổ Shortcuts mà nút chính mở ra.</item>
    /// </list>
    /// </summary>
    internal sealed class ShortcutHelpPopover : LiveOpsPopoverContent
    {
        /// <summary>
        /// 320px theo khuôn popover của hub ([FD §7]). Chiều cao 420: hai tiêu đề nhóm + 11 hàng phím tắt + 9 hàng timeline
        /// cao hơn bất kỳ cửa sổ popover nào nên danh sách PHẢI cuộn — 420 là chỗ đủ cho khoảng 14 hàng thấy ngay mà popover
        /// vẫn không cao quá nửa màn hình 900px thấp nhất mà [FD §7] tính tới. Chỗ thừa dồn xuống hàng nút (margin-top: auto).
        /// </summary>
        private static readonly Vector2 WindowSize = new Vector2(320f, 420f);

        /// <summary>Sheet riêng của popover phím tắt — bốn sheet chung do <c>LiveOpsPopoverContent</c> nạp ở gốc.</summary>
        private static readonly string[] PopoverOwnSheets = { LiveOpsHubPaths.ShortcutHelpPopoverUss };

        private readonly IReadOnlyList<string> _sectionTitles;
        private readonly ILiveOpsHubLayoutLoader _layoutLoader;
        private readonly Func<bool> _openShortcutManager;

        private Label _openFailedReasonLabel;

        /// <param name="sectionTitles">Tiêu đề màn theo thứ tự registry — cho câu "Đi tới màn …" của ⌘1…⌘6.</param>
        /// <param name="layoutLoader">Nguồn UXML/USS; null = AssetDatabase (cùng nhánh dự phòng với các popover khác).</param>
        /// <param name="openShortcutManager">
        /// Đường mở cửa sổ Shortcuts; null = đường thật. Kịch bản chụp truyền hàm riêng: ảnh không được kéo theo một cửa sổ
        /// Shortcuts thật vào khung hình.
        /// </param>
        internal ShortcutHelpPopover(IReadOnlyList<string> sectionTitles, ILiveOpsHubLayoutLoader layoutLoader,
            Func<bool> openShortcutManager)
        {
            _sectionTitles = sectionTitles;
            _layoutLoader = layoutLoader ?? new AssetDatabaseLiveOpsHubLayoutLoader();
            _openShortcutManager = openShortcutManager;
        }

        /// <summary>Nhóm đang hiện — test đọc thẳng, không cần panel.</summary>
        internal IReadOnlyList<ShortcutHelpGroup> Groups { get; private set; }

        internal Button OpenShortcutManagerButton { get; private set; }

        internal Button CloseButton { get; private set; }

        internal Label OpenFailedReasonLabel => _openFailedReasonLabel;

        /// <summary>Số lần nút mở cửa sổ Shortcuts đã chạy — test chứng minh nút gọi đúng một lần mỗi cú bấm.</summary>
        internal int OpenShortcutManagerCount { get; private set; }

        protected override Vector2 PopoverSize => WindowSize;

        /// <summary>Nút chính nhận focus đầu tiên: Esc chỉ tới root khi đã có element focus (luật của lớp gốc).</summary>
        protected override Focusable InitialFocus => OpenShortcutManagerButton;

        internal void ClickOpenShortcutManagerForTest()
        {
            OnOpenShortcutManagerClicked();
        }

        protected override VisualElement BuildContent()
        {
            VisualTreeAsset layout = _layoutLoader.LoadVisualTree(LiveOpsHubPaths.ShortcutHelpPopoverUxml);
            if (layout == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.ShortcutHelpMissingLayoutFormat, LiveOpsHubPaths.ShortcutHelpPopoverUxml));
            }

            // Bỏ vỏ TemplateContainer (cùng lý do với popover dán): vỏ không mang class nên nó không nhận bố cục 320px.
            VisualElement root = layout.Instantiate().Q(LiveOpsHubPaths.ShortcutHelpElementNames.Body) ?? layout.Instantiate();
            root.name = LiveOpsHubPaths.ShortcutHelpElementNames.Body;
            LiveOpsFeedbackStyleSheets.AddStyleSheets(root, PopoverOwnSheets, _layoutLoader);

            Label header = root.Q<Label>(LiveOpsHubPaths.ShortcutHelpElementNames.Header);
            if (header != null) header.text = LiveOpsHubStrings.ShortcutHelpPopoverTitle;

            Groups = ShortcutHelpModel.Build(_sectionTitles);
            VisualElement groupsHost = root.Q(LiveOpsHubPaths.ShortcutHelpElementNames.Groups);
            if (groupsHost != null) BuildGroups(groupsHost, Groups);

            Label note = root.Q<Label>(LiveOpsHubPaths.ShortcutHelpElementNames.Note);
            if (note != null) note.text = LiveOpsHubStrings.ShortcutHelpNote;

            _openFailedReasonLabel = root.Q<Label>(LiveOpsHubPaths.ShortcutHelpElementNames.OpenFailedReason);
            if (_openFailedReasonLabel != null)
            {
                _openFailedReasonLabel.text = LiveOpsHubStrings.ShortcutHelpOpenShortcutManagerFailedReason;
                _openFailedReasonLabel.EnableInClassList(LiveOpsHubClassNames.ShortcutHelpHidden, true);
            }

            CloseButton = root.Q<Button>(LiveOpsHubPaths.ShortcutHelpElementNames.Close);
            if (CloseButton != null)
            {
                CloseButton.text = LiveOpsHubStrings.ShortcutHelpCloseButton;
                CloseButton.clicked += ClosePopover;
            }

            OpenShortcutManagerButton = root.Q<Button>(LiveOpsHubPaths.ShortcutHelpElementNames.OpenShortcutManager);
            if (OpenShortcutManagerButton != null)
            {
                OpenShortcutManagerButton.text = LiveOpsHubStrings.ShortcutHelpOpenShortcutManagerButton;
                OpenShortcutManagerButton.clicked += OnOpenShortcutManagerClicked;
            }

            return root;
        }

        private static void BuildGroups(VisualElement host, IReadOnlyList<ShortcutHelpGroup> groups)
        {
            host.Clear();
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                ShortcutHelpGroup group = groups[groupIndex];
                Label title = new Label(group.Title);
                title.AddToClassList(LiveOpsHubClassNames.ShortcutHelpGroupTitle);
                host.Add(title);

                for (int rowIndex = 0; rowIndex < group.Rows.Count; rowIndex++)
                {
                    host.Add(BuildRow(group.Rows[rowIndex]));
                }
            }
        }

        private static VisualElement BuildRow(ShortcutHelpRow row)
        {
            VisualElement rowElement = new VisualElement();
            rowElement.AddToClassList(LiveOpsHubClassNames.ShortcutHelpRow);

            Label description = new Label(row.Description);
            description.AddToClassList(LiveOpsHubClassNames.ShortcutHelpRowDescription);
            rowElement.Add(description);

            Label key = new Label(row.KeyLabel);
            key.AddToClassList(LiveOpsHubClassNames.ShortcutHelpRowKey);
            // KHÔNG đeo class Mono: RobotoMono thiếu ← → ↑ ↓ ⌘ ⇧ ⌥ ⌫ ([API §12.3]) — đúng những ký tự mà cột này toàn là.
            // Cột đã thẳng nhờ canh phải + flex-shrink 0, nên nó không cần font đều chữ để đọc theo một cột.
            // Chữ "chưa gán phím" là một câu, không phải ký hiệu phím: nhạt đi để cột phím vẫn đọc được như một cột phím.
            key.EnableInClassList(LiveOpsHubClassNames.ShortcutHelpRowKeyUnbound, row.IsUnbound);
            rowElement.Add(key);
            return rowElement;
        }

        /// <summary>
        /// Không đóng popover sau khi mở cửa sổ Shortcuts: người dùng đổi phím xong thường muốn đối chiếu lại ngay với bảng
        /// này. Hỏng thì hiện dòng lý do và popover ở lại — không toast, vì popover là cửa sổ riêng, toast của hub không tới.
        /// </summary>
        private void OnOpenShortcutManagerClicked()
        {
            OpenShortcutManagerCount++;
            bool opened = _openShortcutManager != null ? _openShortcutManager() : LiveOpsShortcutManagerWindow.TryOpen();
            if (_openFailedReasonLabel == null) return;
            _openFailedReasonLabel.EnableInClassList(LiveOpsHubClassNames.ShortcutHelpHidden, opened);
        }
    }
}
