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
    /// Trạng thái gõ dựng SAU khi cửa sổ hiện, qua đúng đường người dùng đi (<c>Editor.value</c>): ô đã thuộc panel nên UI
    /// Toolkit phát <c>ChangeEvent</c> và foldout tự kiểm, không cần một seam riêng trong control sản phẩm. Vẽ lại form sau
    /// đó không nuốt chữ (foldout giữ cờ "đang sửa dở"), nên không có cuộc đua nào với lượt layout đầu tiên.
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

        /// <summary>
        /// Chiều cao ô JSON (<c>min-height</c> 116 của <c>.liveops-hub-recurring-json-editor</c>) và chiều cao một DÒNG lỗi
        /// (chữ 10px, dòng 13). Đo hai khung RIÊNG của hình này chứ không chỉ năm khung chung của khung hub: dòng lỗi CHỈ có
        /// khung khi foldout thật sự đang báo lỗi — ảnh lỡ chụp JSON đúng thì nó ẩn (<c>display: none</c>, cao 0) và số đo đỏ
        /// ngay, thay vì "đạt" với nội dung sai như lần chụp đầu của gói.
        /// </summary>
        private const float RecurringJsonEditorHeight = 116f;

        /// <summary>
        /// Chiều cao một DÒNG lỗi, khai theo DẢI (W9-17): 15,5 ± 3 px, tức 12,5…18,5.
        /// <para>
        /// Vì sao không còn là một con số: cùng một dòng chữ 10px đo được 13px trên 2022.3 và 17px (skin tối) / 18px (skin
        /// sáng) trên 6000.6 — chênh lệch của atlas font giữa hai bản Unity, không phải của bố cục. Pin 13px làm 2 ô ảnh
        /// 6000.6 lệch ở cả lượt 2 lẫn lượt 3; pin 17px sẽ làm 2022.3 lệch thay. Dải này nói đúng thứ đang được hứa và vẫn
        /// đỏ ở cả hai đầu quan trọng: 0px (dòng lỗi bị <c>display: none</c> vì ảnh lỡ chụp JSON ĐÚNG) và ≥ 26px (câu lỗi
        /// xuống hai dòng, tức bố cục quanh nó đã đổi).
        /// </para>
        /// </summary>
        private const float RecurringJsonErrorHeight = 15.5f;

        private const float RecurringJsonErrorHeightTolerance = 3f;

        static partial void RegisterRecurringJson(List<LiveOpsHubCaptureScenario> scenarios)
        {
            // expectedFrames GHI ĐÈ bảng mặc định của measure-capture.py, nên phải kèm lại năm khung khung-hub
            // (ShellFramesWith) — khai mỗi hai khung riêng là lặng lẽ bỏ đo cả rail/header/status của hình này.
            List<LiveOpsHubCaptureExpectedFrame> frames = new List<LiveOpsHubCaptureExpectedFrame>(ShellFramesWith(
                new LiveOpsHubCaptureExpectedFrame(RecurringRuleJsonFoldout.EditorElementName, 0f, RecurringJsonEditorHeight)));
            frames.Add(new LiveOpsHubCaptureExpectedFrame(RecurringRuleJsonFoldout.ErrorElementName, 0f, RecurringJsonErrorHeight,
                RecurringJsonErrorHeightTolerance));
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H14eRecurringJsonError, StandardWidth, StandardHeight,
                    OpenRecurringJsonError)
                .WithExpectedFrames(frames.ToArray()));
        }

        private static EditorWindow OpenRecurringJsonError()
        {
            RecurringRulesSection target = null;
            EditorWindow window = OpenRecurring(LiveOpsDesignSample.Document, section => target = section);
            if (target == null) return window;
            RecurringRuleJsonFoldout foldout = target.Form.JsonFoldout;
            foldout.value = true;
            // Gán SAU khi cửa sổ Show: lúc này ô đã thuộc panel nên gán value phát ChangeEvent y như người dùng gõ, foldout
            // chạy đúng bộ kiểm của mình và khoá "Áp" kèm câu lỗi. Gán TRƯỚC Show thì không có panel, ChangeEvent không phát
            // và Bind() đầu tiên đè mất chữ — đó là lý do kịch bản này không dùng tham số prepare để gõ.
            foldout.Editor.value = RecurringJsonWithMissingComma();
            return window;
        }
    }
}
