using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Registry kịch bản chụp, chia file theo vùng (<c>LiveOpsHubCaptureScenarios.&lt;Vùng&gt;.cs</c>) để gói song song không sửa
    /// chung một file (mục 2.4). File gốc này (G-SHELL) khai sẵn <c>static partial void Register&lt;Vùng&gt;</c> cho MỌI vùng của
    /// ma trận 9.5: vùng chưa có file thì partial không cài và trình biên dịch bỏ lời gọi — gói tới sau chỉ thêm file vùng của mình.
    /// </summary>
    internal static partial class LiveOpsHubCaptureScenarios
    {
        /// <summary>Mọi kịch bản đã đăng ký, theo thứ tự vùng; id trùng là lỗi lập trình (hai ảnh ghi đè nhau).</summary>
        public static IReadOnlyList<LiveOpsHubCaptureScenario> All()
        {
            List<LiveOpsHubCaptureScenario> scenarios = new List<LiveOpsHubCaptureScenario>();
            RegisterShell(scenarios);
            RegisterFeedback(scenarios);
            RegisterControls(scenarios);
            RegisterTimeline(scenarios);
            RegisterOverview(scenarios);
            RegisterEventTypes(scenarios);
            RegisterCalendar(scenarios);
            RegisterCalendarDepth(scenarios);
            RegisterRecurring(scenarios);
            RegisterRecurringJson(scenarios);
            RegisterValidation(scenarios);
            RegisterValidationDepth(scenarios);
            RegisterExport(scenarios);
            RegisterPaste(scenarios);
            RegisterShellPolish(scenarios);
            RegisterOptTimeline(scenarios);
            RegisterShortcutHelp(scenarios);
            RegisterUxSizes(scenarios);
            RegisterWorstCase(scenarios);

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (LiveOpsHubCaptureScenario scenario in scenarios)
            {
                if (!ids.Add(scenario.Id))
                {
                    throw new InvalidOperationException("Kịch bản chụp '" + scenario.Id + "' đăng ký hai lần — hai ảnh cùng tên sẽ ghi đè nhau.");
                }
            }
            return scenarios;
        }

        /// <summary>Kịch bản theo id; null khi chưa đăng ký.</summary>
        public static LiveOpsHubCaptureScenario Find(string id)
        {
            foreach (LiveOpsHubCaptureScenario scenario in All())
            {
                if (string.Equals(scenario.Id, id, StringComparison.Ordinal)) return scenario;
            }
            return null;
        }

        static partial void RegisterShell(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterFeedback(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterControls(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterTimeline(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterOverview(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterEventTypes(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterCalendar(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterCalendarDepth(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterRecurring(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterRecurringJson(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterValidation(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterValidationDepth(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterExport(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterPaste(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterShellPolish(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterOptTimeline(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterShortcutHelp(List<LiveOpsHubCaptureScenario> scenarios);

        /// <summary>Ma trận cỡ cửa sổ của đợt W8-UX (§3.4) — bí danh <c>capture.sh --scenarios ux-sizes</c>.</summary>
        static partial void RegisterUxSizes(List<LiveOpsHubCaptureScenario> scenarios);
        static partial void RegisterWorstCase(List<LiveOpsHubCaptureScenario> scenarios);

        /// <summary>
        /// Khung số đo của hộp xác nhận: đo BỀ RỘNG 400px, KHÔNG đo chiều cao (W9-17).
        /// <para>
        /// Bề rộng 400 là hứa thiết kế [FD §3.10] — hộp là một cột cố định ở mọi cấp, mọi ngôn ngữ, mọi nội dung. Chiều cao
        /// thì KHÔNG: hộp tự co theo nội dung (số dòng câu hỏi, có hay không ô "gõ để xác nhận", có hay không số liệu).
        /// </para>
        /// <para>
        /// Bản cũ khai chiều cao bằng chiều cao CỬA SỔ (212px cấp 1, 290px cấp 2) cho phần tử gốc BÊN TRONG cửa sổ đó. Hai
        /// con số ấy không bao giờ đúng: mười kịch bản hộp đo được bốn giá trị rời rạc 100 / 113 / 127 / 201px, ổn định y hệt
        /// ở cả hai bản Unity lẫn cả hai skin — nên 40 trong 42 ô lệch của bảng số đo ảnh ở lượt 2 VÀ lượt 3 đều là chính nó,
        /// hai lượt liền, không phải hồi quy của đợt nào. Thay một con số sai bằng một con số sai khác (ví dụ pin 201) chỉ
        /// chuyển chỗ lệch sang chín kịch bản còn lại, và lần sau sửa một câu chữ là lệch lại.
        /// </para>
        /// <para>
        /// Bỏ đo chiều cao KHÔNG phải nới cổng: thứ đáng kiểm ở hộp là "chữ có đọc hết không, nút có nằm trong hộp không", và
        /// đó là việc của <c>UxLayoutAuditTests.ConfirmWindow_TextNotCut</c> — nó đo chữ bằng MeasureTextSize trên cây thật
        /// chứ không dò cạnh trên ảnh. Cái mất ở đây là một câu khẳng định SAI, cái giữ lại là bề rộng và toàn bộ cổng bố cục.
        /// </para>
        /// </summary>
        private static LiveOpsHubCaptureExpectedFrame ConfirmRootExpectedFrame()
        {
            return new LiveOpsHubCaptureExpectedFrame(LiveOpsConfirmContent.RootElementName, LiveOpsConfirmWindow.Width, 0f);
        }
    }
}
