using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng RecurringJson (G-RECURRING-JSON, W5) — một hình duy nhất của ma trận 9.5:
    /// <list type="bullet">
    /// <item><c>h14e-recurring-json-error</c>: foldout "JSON của luật này" đang MỞ và SỬA ĐƯỢC, người dùng vừa xoá dấu phẩy
    /// cuối dòng 3 nên dòng lỗi đọc đúng ví dụ của thiết kế ("Dòng 3, ký tự 18: thiếu dấu phẩy") và nút "Áp" khoá kèm chính
    /// câu đó in thành chữ cạnh nút (SPIKE-B SP-3).</item>
    /// </list>
    /// Trạng thái dựng TRƯỚC khi mở cửa sổ, qua chính control của form (mở foldout + gõ vào ô): cửa sổ dựng bằng
    /// <c>OpenWithServices</c> có <c>windowState</c> rỗng nên không có trạng thái view đã lưu nào đè lại, và đặt sau khi Show
    /// là một cuộc đua với lượt layout đầu tiên.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        /// <summary>
        /// JSON của luật weekly-pass trong mẫu thiết kế, thiếu dấu phẩy cuối dòng 3. Dòng 3 dài đúng 17 ký tự nên chỗ phải
        /// chèn dấu phẩy là ký tự thứ 18 — ảnh ra đúng câu lỗi mà thiết kế viết. Ghép bằng "\n" chứ không dùng chuỗi
        /// verbatim: file nguồn checkout ở Windows sẽ có "\r\n", số ký tự của dòng đổi theo và ảnh lệch câu.
        /// </summary>
        private static string RecurringJsonWithMissingComma()
        {
            return string.Join("\n", new[]
            {
                "{",
                "  \"type\": \"" + RecurringWeeklyPassType + "\",",
                "  \"idPrefix\": \"s\"",
                "  \"periodHours\": 168,",
                "  \"activeHours\": 168,",
                "  \"configKey\": \"weekly_pass_s3\"",
                "}",
            });
        }

        static partial void RegisterRecurringJson(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H14eRecurringJsonError, StandardWidth, StandardHeight,
                OpenRecurringJsonError));
        }

        private static EditorWindow OpenRecurringJsonError()
        {
            return OpenRecurring(LiveOpsDesignSample.Document, section =>
            {
                RecurringRuleJsonFoldout foldout = section.Form.JsonFoldout;
                foldout.value = true;
                // KHÔNG gán thẳng Editor.value: kịch bản dựng trạng thái TRƯỚC khi cửa sổ Show (SP-16, xem OpenRecurring),
                // lúc đó foldout chưa thuộc panel nào nên ChangeEvent của UI Toolkit không phát — chữ gán vào sẽ bị Bind()
                // đầu tiên (sau khi cửa sổ hiện) đè mất mà không qua bộ kiểm. SetEditorTextForCapture đi thẳng đường kiểm
                // (SetValueWithoutNotify + Validate) và tự khớp eventType với luật weekly-pass đang chọn nên Bind() đầu
                // tiên đó không coi đây là "đổi sang luật khác" rồi xoá cờ đang sửa — luồng người dùng thật gõ khi ô đã
                // trong panel nên không cần đường này.
                foldout.SetEditorTextForCapture(RecurringWeeklyPassType, RecurringJsonWithMissingComma());
            });
        }
    }
}
