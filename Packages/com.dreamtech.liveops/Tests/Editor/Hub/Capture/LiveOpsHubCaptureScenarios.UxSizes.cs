using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp của ma trận cỡ cửa sổ W8-UX (§3.4): ba trạng thái × sáu cỡ × hai ngôn ngữ (36 ảnh mỗi skin). Ảnh TRƯỚC chụp trên gốc đợt, ảnh SAU trên nhánh
    /// đã gộp — người duyệt đặt hai contact sheet cạnh nhau thay vì đọc 63 dòng bảng lỗi.
    /// <para>
    /// Kịch bản KHÔNG khai <c>expectedFrames</c>: ở đây không có con số thiết kế nào để đo (hình thiết kế chỉ có một cỡ), nên
    /// bảng mặc định của <c>measure-capture.py</c> là đủ và đúng vai — bộ này để NHÌN, phần đo bằng máy là
    /// <see cref="UxLayoutAuditTests"/>.
    /// </para>
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        /// <summary>Đợt đang chọn của ảnh "đã chọn" — cùng đợt mà hành trình Lịch bấm, để ảnh và test nói về một thứ.</summary>
        private const string UxSizesSelectedBarKey = LiveOpsDesignSample.LavaQuestEarlyEntryKey;

        static partial void RegisterUxSizes(List<LiveOpsHubCaptureScenario> scenarios)
        {
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendar700, 700, 560, false);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendar820, 820, 560, false);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendar1024, 1024, 700, false);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendar1280, 1280, 760, false);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendar1440, 1440, 900, false);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendar1920, 1920, 1040, false);

            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarSelected700, 700, 560, true);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarSelected820, 820, 560, true);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarSelected1024, 1024, 700, true);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarSelected1280, 1280, 760, true);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarSelected1440, 1440, 900, true);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarSelected1920, 1920, 1040, true);

            AddUxRecurringSize(scenarios, LiveOpsHubCaptureScenarioIds.UxRecurring700, 700, 560);
            AddUxRecurringSize(scenarios, LiveOpsHubCaptureScenarioIds.UxRecurring820, 820, 560);
            AddUxRecurringSize(scenarios, LiveOpsHubCaptureScenarioIds.UxRecurring1024, 1024, 700);
            AddUxRecurringSize(scenarios, LiveOpsHubCaptureScenarioIds.UxRecurring1280, 1280, 760);
            AddUxRecurringSize(scenarios, LiveOpsHubCaptureScenarioIds.UxRecurring1440, 1440, 900);
            AddUxRecurringSize(scenarios, LiveOpsHubCaptureScenarioIds.UxRecurring1920, 1920, 1040);

            // Nhánh tiếng Anh của đúng ba trạng thái trên (R-10): chữ dài hơn nên cắt chữ và tràn hàng lộ ra ở bản này trước.
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarEnglish700, 700, 560, false, LiveOpsHubLanguageId.English);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarEnglish820, 820, 560, false, LiveOpsHubLanguageId.English);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarEnglish1024, 1024, 700, false, LiveOpsHubLanguageId.English);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarEnglish1280, 1280, 760, false, LiveOpsHubLanguageId.English);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarEnglish1440, 1440, 900, false, LiveOpsHubLanguageId.English);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarEnglish1920, 1920, 1040, false, LiveOpsHubLanguageId.English);

            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarSelectedEnglish700, 700, 560, true, LiveOpsHubLanguageId.English);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarSelectedEnglish820, 820, 560, true, LiveOpsHubLanguageId.English);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarSelectedEnglish1024, 1024, 700, true, LiveOpsHubLanguageId.English);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarSelectedEnglish1280, 1280, 760, true, LiveOpsHubLanguageId.English);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarSelectedEnglish1440, 1440, 900, true, LiveOpsHubLanguageId.English);
            AddUxSize(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarSelectedEnglish1920, 1920, 1040, true, LiveOpsHubLanguageId.English);

            AddUxRecurringSize(scenarios, LiveOpsHubCaptureScenarioIds.UxRecurringEnglish700, 700, 560, LiveOpsHubLanguageId.English);
            AddUxRecurringSize(scenarios, LiveOpsHubCaptureScenarioIds.UxRecurringEnglish820, 820, 560, LiveOpsHubLanguageId.English);
            AddUxRecurringSize(scenarios, LiveOpsHubCaptureScenarioIds.UxRecurringEnglish1024, 1024, 700, LiveOpsHubLanguageId.English);
            AddUxRecurringSize(scenarios, LiveOpsHubCaptureScenarioIds.UxRecurringEnglish1280, 1280, 760, LiveOpsHubLanguageId.English);
            AddUxRecurringSize(scenarios, LiveOpsHubCaptureScenarioIds.UxRecurringEnglish1440, 1440, 900, LiveOpsHubLanguageId.English);
            AddUxRecurringSize(scenarios, LiveOpsHubCaptureScenarioIds.UxRecurringEnglish1920, 1920, 1040, LiveOpsHubLanguageId.English);
        }

        private static void AddUxSize(List<LiveOpsHubCaptureScenario> scenarios, string scenarioId, int width, int height,
            bool withSelection, LiveOpsHubLanguageId language = LiveOpsHubLanguageId.Vietnamese)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(scenarioId, width, height, () => OpenUxCalendar(withSelection))
                .WithLanguage(language));
        }

        private static void AddUxRecurringSize(List<LiveOpsHubCaptureScenario> scenarios, string scenarioId, int width, int height,
            LiveOpsHubLanguageId language = LiveOpsHubLanguageId.Vietnamese)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(scenarioId, width, height, OpenUxRecurring).WithLanguage(language));
        }

        private static EditorWindow OpenUxCalendar(bool withSelection)
        {
            LiveOpsHubServices services = UxSizesServices();
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            // Chọn TRƯỚC Show: view của màn dựng trong CreateGUI, đặt sau Show là đua với lượt layout đầu tiên (bẫy đã ghi ở
            // LiveOpsHubCaptureScenarios.Recurring).
            if (withSelection)
            {
                foreach (IHubSection section in sections)
                {
                    if (section is CalendarSection calendar) calendar.Presenter.SetSelectedBarKey(UxSizesSelectedBarKey);
                }
            }
            return LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Calendar);
        }

        private static EditorWindow OpenUxRecurring()
        {
            LiveOpsHubServices services = UxSizesServices();
            return LiveOpsHubWindow.OpenWithServices(services, LiveOpsHubSections.Create(services), LiveOpsHubSections.Ids.RecurringRules);
        }

        private static LiveOpsHubServices UxSizesServices()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            services.Session.RunCheckToCompletion();
            return services;
        }
    }
}
