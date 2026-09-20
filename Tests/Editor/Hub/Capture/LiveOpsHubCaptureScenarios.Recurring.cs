using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using UnityEditor;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng Recurring (G-RECURRING, W4) — bốn trạng thái của Hình 14 ([SD1 §4.1–4.3]) và hộp cấp 2 đổi tiền tố
    /// của Hình 8 ô 5:
    /// <list type="bullet">
    /// <item><c>h14a-recurring-default</c>: mẫu thiết kế đúng lúc 08:47 — tiền tố nháp đã ghi là <c>pass-</c> trong khi bản đã
    /// đăng còn <c>weekly-pass-</c>, nên HelpBox hậu quả còn ở dưới ô và bảng đợt kế tiếp có tag "đổi id".</item>
    /// <item><c>h14b-recurring-prefix-draft</c>: đang gõ tiền tố mới — nháp tại ô, viền warning, hai nút "Huỷ (Esc)" ·
    /// "Ghi tiền tố mới…". Dựng bằng ĐÚNG lối vào của form (<c>RequestFieldChange</c>), không nặn tay trạng thái.</item>
    /// <item><c>h14c-recurring-after-write</c>: vừa ghi xong qua chính lệnh sửa của phiên — HelpBox ở lại tới khi lần lặp cũ khép.</item>
    /// <item><c>h14d-recurring-active-longer</c>: chạy lâu hơn chu kỳ — field lỗi + thanh chu kỳ có con thứ ba blocked-fill.</item>
    /// <item><c>h08e-confirm-prefix-type-to-confirm</c>: hộp cấp 2 400×290, đang gõ thiếu một ký tự.</item>
    /// </list>
    /// Mọi kịch bản mở cửa sổ hub thật bằng <c>OpenWithServices</c> trên CÙNG một services với registry (bẫy hai phiên đã ghi ở
    /// <see cref="LiveOpsHubSections.Create()"/>), và đặt trạng thái TRƯỚC khi mở: view của màn dựng trong CreateGUI nên sửa
    /// sau khi Show là một cuộc đua với lượt layout đầu tiên.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        private const string RecurringWeeklyPassType = "weekly-pass";
        private const string RecurringPublishedPrefix = "weekly-pass-";
        private const string RecurringDraftPrefix = "pass-";

        /// <summary>Chạy 200 giờ trong chu kỳ 168 giờ: phần tràn 32 giờ là con thứ ba của thanh chu kỳ.</summary>
        private const int RecurringOverflowActiveHours = 200;

        private const string RecurringWriteUndoName = "Đổi tiền tố id weekly-pass- → pass-";

        static partial void RegisterRecurring(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H14aRecurringDefault, StandardWidth, StandardHeight,
                    OpenRecurringDefault)
                .WithExpectedFrames(ShellFramesWith(new LiveOpsHubCaptureExpectedFrame(RecurringRuleForm.SentenceElementName, 0f,
                    RecurringSentenceHeight))));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H14bRecurringPrefixDraft, StandardWidth, StandardHeight,
                OpenRecurringPrefixDraft));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H14cRecurringAfterWrite, StandardWidth, StandardHeight,
                OpenRecurringAfterWrite));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H14dRecurringActiveLonger, StandardWidth, StandardHeight,
                OpenRecurringActiveLonger));

            // W9-22(c): màn Luật lặp ở cỡ HẸP NHẤT của ma trận, đã cuộn tới card "đợt kế tiếp". Ở 700x560 card nằm dưới
            // lằn cuộn nên không ảnh nào của màn này từng cho thấy nó — mà đây đúng là chỗ lượt 3 vừa sửa bố cục (cột "Lúc
            // này" xuống dòng riêng dưới 1100px), tức là phần đang thiếu ảnh lại là phần vừa đổi nhiều nhất.
            // Khai expectedFrames RIÊNG: bảng mặc định của measure-capture.py là bảng của MỘT cỡ cửa sổ thiết kế (1280x760)
            // với rail rộng 196 và cột nội dung rộng 1084. Ảnh này cố tình chụp ở 700x560, nơi rail thu về bậc hẹp 36px theo
            // thiết kế, nên hai con số bề RỘNG kia sai vai ở đây — để nguyên thì ảnh báo lệch dù giao diện đúng (đo được ở
            // lượt chụp đầu: 2 dòng lệch, đúng hai dòng đó). Giữ ba con số KHÔNG phụ thuộc cỡ cửa sổ, cùng cách mà ma trận
            // ux-sizes đã chốt ở G-FIX-UX-2.
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H14fRecurringOccurrencesScrolled,
                    RecurringOccurrencesNarrowWidth, RecurringOccurrencesNarrowHeight, OpenRecurringDefault)
                .WithAfterLayout(ScrollToRecurringOccurrences)
                .WithExpectedFrames(UxSizeInvariantFrames()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H08eConfirmPrefixTypeToConfirm,
                    (int)LiveOpsConfirmWindow.Width, (int)LiveOpsConfirmWindow.TypeToConfirmHeight,
                    () => OpenConfirm(ConfirmLevel2SampleRequest(), RecurringTypedPrefixText),
                    window => ((LiveOpsConfirmWindow)window).Content)
                .WithExpectedFrames(ConfirmRootExpectedFrame()));
        }

        /// <summary>
        /// Câu token cao 20 ([SD1 §4.1] "11px, line-height 20") — một DÒNG chữ. Đo khung này là cách duy nhất bắt được hồi
        /// quy "bốn token thành bốn nút xếp dọc": ma trận mặc định chỉ đo rail/header/status nên câu cao 155 vẫn MEASURE OK.
        /// </summary>
        private const float RecurringSentenceHeight = 20f;

        /// <summary>
        /// Khung mặc định của measure-capture.py + khung riêng của kịch bản. Khai <c>expectedFrames</c> là GHI ĐÈ bảng mặc
        /// định, nên phải chép lại rail/header/section header/status/cột nội dung, nếu không kịch bản này mất phần đo khung.
        /// </summary>
        private static LiveOpsHubCaptureExpectedFrame[] ShellFramesWith(LiveOpsHubCaptureExpectedFrame extraFrame)
        {
            return new[]
            {
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Rail, ShellRailWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Header, 0f, ShellHeaderHeight),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.SectionHeader, 0f, ShellSectionHeaderHeight),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Status, 0f, ShellStatusHeight),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Content, ShellContentWidth, 0f),
                extraFrame,
            };
        }

        private const float ShellRailWidth = 196f;
        private const float ShellHeaderHeight = 26f;
        private const float ShellSectionHeaderHeight = 36f;
        private const float ShellStatusHeight = 20f;
        private const float ShellContentWidth = 1084f;

        /// <summary>Mockup Hình 8 ô 5 đang gõ thiếu ký tự cuối của "weekly-pass-35".</summary>
        private const string RecurringTypedPrefixText = "weekly-pass-3";

        /// <summary>Cỡ HẸP NHẤT của ma trận cỡ W8-UX — chỗ card "đợt kế tiếp" rơi xuống dưới lằn cuộn.</summary>
        private const int RecurringOccurrencesNarrowWidth = 700;

        private const int RecurringOccurrencesNarrowHeight = 560;

        /// <summary>
        /// Cuộn tới card "đợt kế tiếp" SAU khi cửa sổ có layout. Gọi trong lúc mở cửa sổ thì không làm gì: viewport của
        /// ScrollView lúc đó còn cao 0 nên <c>ScrollTo</c> không có gì để cuộn. Không tìm thấy card hay không có ScrollView
        /// nào bọc nó thì để nguyên — ảnh sẽ cho thấy đúng chỗ đó thiếu, còn ném ở đây chỉ làm hỏng cả lượt chụp.
        /// </summary>
        private static void ScrollToRecurringOccurrences(EditorWindow window)
        {
            VisualElement table = window.rootVisualElement.Q(RecurringNextOccurrencesTable.ElementName);
            if (table == null) return;
            for (VisualElement current = table.hierarchy.parent; current != null; current = current.hierarchy.parent)
            {
                if (current is ScrollView scrollView)
                {
                    scrollView.ScrollTo(table);
                    return;
                }
            }
        }

        private static EditorWindow OpenRecurringDefault()
        {
            return OpenRecurring(LiveOpsDesignSample.Document, null);
        }

        private static EditorWindow OpenRecurringPrefixDraft()
        {
            return OpenRecurring(DocumentWithWeeklyPassPrefix(RecurringPublishedPrefix), section =>
            {
                RecurringLiveEventRule rule = WeeklyPassRuleOf(section);
                if (rule != null) section.RequestFieldChange(RecurringRuleFields.IdPrefix, rule.WithIdPrefix(RecurringDraftPrefix));
            });
        }

        private static EditorWindow OpenRecurringAfterWrite()
        {
            return OpenRecurring(DocumentWithWeeklyPassPrefix(RecurringPublishedPrefix), section =>
            {
                RecurringLiveEventRule rule = WeeklyPassRuleOf(section);
                if (rule == null) return;
                // Đi đúng lệnh sửa của phiên: câu "HelpBox ở lại" phải là hệ quả của một lần ghi thật, không phải chữ dán vào.
                section.Services.Session.Apply(new SetRecurringRuleEdit(rule.WithIdPrefix(RecurringDraftPrefix)), RecurringWriteUndoName);
            });
        }

        private static EditorWindow OpenRecurringActiveLonger()
        {
            return OpenRecurring(WeeklyPassDocument(rule => rule.WithActiveHours(RecurringOverflowActiveHours)), null);
        }

        private static LiveEventCalendarDocument DocumentWithWeeklyPassPrefix(string idPrefix)
        {
            return WeeklyPassDocument(rule => rule.WithIdPrefix(idPrefix));
        }

        /// <summary>
        /// Mẫu thiết kế với luật weekly-pass đã sửa. Đi qua <see cref="SetRecurringRuleEdit"/> chứ không qua builder tài liệu:
        /// builder NỐI THÊM luật trùng loại (validator mới là chỗ báo), lệnh sửa mới THAY đúng luật của loại đó.
        /// </summary>
        private static LiveEventCalendarDocument WeeklyPassDocument(System.Func<RecurringLiveEventRule, RecurringLiveEventRule> apply)
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            RecurringLiveEventRule weeklyPass;
            if (!document.TryGetRecurringRule(RecurringWeeklyPassType, out weeklyPass)) return document;
            LiveEventCalendarDocument result;
            return LiveEventCalendarEdits.TryApply(document, new SetRecurringRuleEdit(apply(weeklyPass)), out result) ? result : document;
        }

        private static RecurringLiveEventRule WeeklyPassRuleOf(RecurringRulesSection section)
        {
            RecurringLiveEventRule rule;
            return section.Services.Session.Document.TryGetRecurringRule(RecurringWeeklyPassType, out rule) ? rule : null;
        }

        private static EditorWindow OpenRecurring(LiveEventCalendarDocument document,
            System.Action<RecurringRulesSection> prepare)
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document)));
            services.Session.RunCheckToCompletion();
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            if (prepare != null)
            {
                for (int index = 0; index < sections.Count; index++)
                {
                    RecurringRulesSection recurring = sections[index] as RecurringRulesSection;
                    if (recurring != null) prepare(recurring);
                }
            }
            return LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.RecurringRules);
        }
    }
}
