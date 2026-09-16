using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Phím tắt toàn hub (8.7). Mọi mục ở đây đăng ký với <c>context = typeof(LiveOpsHubWindow)</c>, nên chúng chỉ chạy khi hub có
    /// focus và thắng shortcut Global cùng phím (⌘K của Unity Search) mà không cướp phím của Editor lúc người dùng làm việc khác.
    /// <para>
    /// <b>Luật một dòng (SPIKE-B SP-7b, quyết định an toàn 16/9/2026):</b> bảng này <b>chỉ</b> nhận tổ hợp có phím bổ trợ (⌘K, ⌘S,
    /// ⌘1…6) và phím chức năng (F5, F8) — những phím không bao giờ là một ký tự người dùng đang gõ. <b>Phím đơn một ký tự (T, N, F,
    /// A, mũi tên…) không bao giờ vào <see cref="ShortcutManager"/></b>: chúng do chính element (timeline, bảng) xử lý bằng
    /// <c>KeyDownEvent</c> và bỏ qua khi focus đang ở ô nhập chữ sửa được. Lý do: <see cref="ShortcutManager"/> bắt phím ở tầng
    /// cửa sổ, nên "T" đăng ký ở đây sẽ nhảy về hôm nay ngay giữa lúc người dùng gõ "test" vào ô tìm. Test
    /// <c>SingleKeyT_NotRegisteredInShortcutManager</c> và <c>SingleKeyTimelineKeys_NoGlobalConflict</c> khoá luật này lại.
    /// </para>
    /// <para>
    /// Id giữ ASCII và <b>không đổi sau phát hành</b>: id nằm trong profile phím của người dùng, và
    /// <see cref="LiveOpsHubKeyLabels"/> tra nhãn phím theo đúng chuỗi id này. <c>displayName</c> (chữ hiện trong Edit → Shortcuts)
    /// là hằng ASCII vì thuộc tính C# chỉ nhận hằng biên dịch — nó không theo được ngôn ngữ đang chọn của hub. Nhãn tiếng Việt/tiếng
    /// Anh theo ngôn ngữ nằm trong catalog (<c>LiveOpsHubStrings.ShellShortcut…</c>) và chỉ hiện bên trong hub (PD-15, nhánh dự phòng).
    /// </para>
    /// </summary>
    internal static class LiveOpsHubShortcuts
    {
        internal const string OpenPaletteId = LiveOpsHubPalette.OpenPaletteShortcutId;
        internal const string SaveCalendarId = "LiveOps Hub/Save Calendar";
        internal const string CheckAllId = "LiveOps Hub/Check All";
        internal const string NextFindingId = "LiveOps Hub/Next Finding";
        internal const string PreviousFindingId = "LiveOps Hub/Previous Finding";

        /// <summary>Tiền tố id của sáu mục "đi tới màn"; số đằng sau là VỊ TRÍ trong registry, không phải id màn.</summary>
        internal const string GoToSectionIdPrefix = "LiveOps Hub/Go To Section ";

        /// <summary>Số mục ⌘1…⌘N — đúng số màn P1 của <see cref="LiveOpsHubSections"/> ([FD §3.1]).</summary>
        internal const int GoToSectionCount = 6;

        private const string OpenPaletteDisplayName = "Open go-to-section palette";
        private const string SaveCalendarDisplayName = "Save calendar";
        private const string CheckAllDisplayName = "Check everything again";
        private const string NextFindingDisplayName = "Next finding";
        private const string PreviousFindingDisplayName = "Previous finding";

        /// <summary>Mọi id của hub theo đúng thứ tự bảng 8.7 — test hợp đồng và hướng dẫn phím tắt (W6) duyệt từ đây.</summary>
        internal static IReadOnlyList<string> AllShortcutIds
        {
            get
            {
                List<string> ids = new List<string> { OpenPaletteId, SaveCalendarId, CheckAllId, NextFindingId, PreviousFindingId };
                for (int position = 1; position <= GoToSectionCount; position++) ids.Add(GoToSectionId(position));
                return ids;
            }
        }

        internal static string GoToSectionId(int position)
        {
            return GoToSectionIdPrefix + position.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        [Shortcut(OpenPaletteId, typeof(LiveOpsHubWindow), KeyCode.K, ShortcutModifiers.Action, displayName = OpenPaletteDisplayName)]
        private static void OpenPalette(ShortcutArguments arguments)
        {
            Window(arguments)?.TogglePaletteOrUnitySearch();
        }

        [Shortcut(SaveCalendarId, typeof(LiveOpsHubWindow), KeyCode.S, ShortcutModifiers.Action, displayName = SaveCalendarDisplayName)]
        private static void SaveCalendar(ShortcutArguments arguments)
        {
            Window(arguments)?.SaveFromShortcut();
        }

        [Shortcut(CheckAllId, typeof(LiveOpsHubWindow), KeyCode.F5, displayName = CheckAllDisplayName)]
        private static void CheckAll(ShortcutArguments arguments)
        {
            Window(arguments)?.StartCheckFromShortcut();
        }

        [Shortcut(NextFindingId, typeof(LiveOpsHubWindow), KeyCode.F8, displayName = NextFindingDisplayName)]
        private static void NextFinding(ShortcutArguments arguments)
        {
            Window(arguments)?.MoveToFinding(1);
        }

        [Shortcut(PreviousFindingId, typeof(LiveOpsHubWindow), KeyCode.F8, ShortcutModifiers.Shift, displayName = PreviousFindingDisplayName)]
        private static void PreviousFinding(ShortcutArguments arguments)
        {
            Window(arguments)?.MoveToFinding(-1);
        }

        // Sáu mục ⌘1…⌘6 phải viết tay: thuộc tính C# không sinh được trong vòng lặp. Thân gọi chung một hàm để không có sáu bản sao
        // logic; chỉ con số vị trí khác nhau.
        [Shortcut(GoToSectionIdPrefix + "1", typeof(LiveOpsHubWindow), KeyCode.Alpha1, ShortcutModifiers.Action)]
        private static void GoToSection1(ShortcutArguments arguments) => GoToSection(arguments, 1);

        [Shortcut(GoToSectionIdPrefix + "2", typeof(LiveOpsHubWindow), KeyCode.Alpha2, ShortcutModifiers.Action)]
        private static void GoToSection2(ShortcutArguments arguments) => GoToSection(arguments, 2);

        [Shortcut(GoToSectionIdPrefix + "3", typeof(LiveOpsHubWindow), KeyCode.Alpha3, ShortcutModifiers.Action)]
        private static void GoToSection3(ShortcutArguments arguments) => GoToSection(arguments, 3);

        [Shortcut(GoToSectionIdPrefix + "4", typeof(LiveOpsHubWindow), KeyCode.Alpha4, ShortcutModifiers.Action)]
        private static void GoToSection4(ShortcutArguments arguments) => GoToSection(arguments, 4);

        [Shortcut(GoToSectionIdPrefix + "5", typeof(LiveOpsHubWindow), KeyCode.Alpha5, ShortcutModifiers.Action)]
        private static void GoToSection5(ShortcutArguments arguments) => GoToSection(arguments, 5);

        [Shortcut(GoToSectionIdPrefix + "6", typeof(LiveOpsHubWindow), KeyCode.Alpha6, ShortcutModifiers.Action)]
        private static void GoToSection6(ShortcutArguments arguments) => GoToSection(arguments, 6);

        private static void GoToSection(ShortcutArguments arguments, int position)
        {
            Window(arguments)?.GoToSectionAt(position - 1);
        }

        /// <summary>
        /// Cửa sổ nhận phím. Context cửa sổ nên <c>arguments.context</c> luôn là hub — nhưng vẫn kiểm kiểu: một profile phím cũ hoặc
        /// một bản Unity khác có thể gọi lại với context rỗng, và ném ở đây sẽ thành exception trong đường xử lý phím của Editor.
        /// </summary>
        private static LiveOpsHubWindow Window(ShortcutArguments arguments)
        {
            return arguments.context as LiveOpsHubWindow;
        }

        /// <summary>
        /// Phím đơn có được phép ở element không (SP-7b): đây là câu hỏi DÙNG CHUNG mà mọi handler <c>KeyDownEvent</c> phím đơn
        /// của hub nên hỏi trước. Focus đang ở ô nhập chữ SỬA ĐƯỢC thì phím đơn là ký tự người dùng gõ, không phải lệnh.
        /// <para>
        /// Trạng thái hôm nay (ghi đúng, không nói quá): handler phím đơn DUY NHẤT đang chạy là
        /// <c>LiveOpsTimelineElement.OnKeyDown</c> / <c>OnNavigationMove</c>, và nó vẫn dùng bản <c>IsInsideTextField</c> RIÊNG —
        /// bản đó chỉ xét <c>TextField</c>, không xét <c>ITextEdition.isReadOnly</c>, nên lỏng hơn hàm này. File
        /// <c>LiveOpsTimelineElement.cs</c> thuộc quyền ghi G-TIMELINE-VIEW (<c>ownership.tsv</c>), G-HOSTUI không sửa được;
        /// việc gộp hai bản đã ghi vào <c>contract-changes-G-HOSTUI.md</c> cho cổng W4. Khi gộp xong, câu "duy nhất một chỗ hỏi"
        /// mới đúng cả về chữ lẫn về code.
        /// </para>
        /// </summary>
        /// <param name="target">Element nhận sự kiện (<c>evt.target</c>).</param>
        internal static bool IsSingleKeyCommandAllowed(UnityEngine.UIElements.VisualElement target)
        {
            if (target == null) return true;
            // `is TextField` phải xét RIÊNG: ô nhập chính nó là đích của sự kiện, và GetFirstAncestorOfType không phải bản Unity
            // nào cũng tính chính nó là tổ tiên.
            if (target is UnityEngine.UIElements.TextField) return false;
            if (target.GetFirstAncestorOfType<UnityEngine.UIElements.TextField>() != null) return false;
            // TextElement sửa được (ô chữ con của TextField ở 6000.6) — vẫn là chỗ người dùng gõ, không phải chỗ nhận lệnh.
            UnityEngine.UIElements.ITextEdition edition = target as UnityEngine.UIElements.ITextEdition;
            return edition == null || edition.isReadOnly;
        }
    }
}
