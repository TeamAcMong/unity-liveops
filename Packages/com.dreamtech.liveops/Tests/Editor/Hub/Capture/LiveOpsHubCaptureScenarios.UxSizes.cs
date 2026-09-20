using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp của ma trận cỡ cửa sổ W8-UX (§3.4): ba trạng thái × sáu cỡ × hai ngôn ngữ (36 ảnh mỗi skin). Ảnh TRƯỚC chụp trên gốc đợt, ảnh SAU trên nhánh
    /// đã gộp — người duyệt đặt hai contact sheet cạnh nhau thay vì đọc 63 dòng bảng lỗi.
    /// <para>
    /// Kịch bản KHAI <c>expectedFrames</c> riêng (G-FIX-UX-2, cổng đợt W8-UX). Bảng mặc định của <c>measure-capture.py</c> là
    /// bảng của MỘT cỡ cửa sổ thiết kế (1280×760): rail rộng 196 và cột nội dung rộng 1084. Bộ ảnh này đổi đúng cái cỡ đó,
    /// và ở 700/820 rail thu về bậc hẹp theo thiết kế, nên hai con số bề RỘNG kia sai vai ở đây — để nguyên thì mọi ảnh của ma
    /// trận đều "lệch" dù giao diện đúng (lượt chụp TRƯỚC: 60/72 ảnh báo lệch vì đúng hai dòng đó).
    /// Giữ lại ba con số thiết kế KHÔNG phụ thuộc cỡ cửa sổ (chiều cao header, section header, status bar) — đó vẫn là hứa
    /// thiết kế ở mọi cỡ. Phần bề rộng/giãn theo cửa sổ do <see cref="UxLayoutAuditTests"/> đo bằng máy, không đo trên ảnh.
    /// </para>
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        /// <summary>Đợt đang chọn của ảnh "đã chọn" — cùng đợt mà hành trình Lịch bấm, để ảnh và test nói về một thứ.</summary>
        private const string UxSizesSelectedBarKey = LiveOpsDesignSample.LavaQuestEarlyEntryKey;

        /// <summary>Chiều cao thiết kế của header hub [SD1 §2.1] — không đổi theo cỡ cửa sổ, nên đo được ở mọi ảnh ma trận.</summary>
        private const float UxHeaderHeight = 26f;

        /// <summary>Chiều cao thiết kế của header màn [SD1 §2.1].</summary>
        private const float UxSectionHeaderHeight = 36f;

        /// <summary>Chiều cao thiết kế của status bar [SD1 §2.1].</summary>
        private const float UxStatusBarHeight = 20f;

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

            // W9-22(b): trạng thái CHỌN NHIỀU đợt. Chụp ở 1024x700 vì đó là cỡ hẹp nhất mà cả hai thanh của mẫu thiết kế
            // đều được vẽ; ở 700 trục không vẽ thanh thứ hai nên ảnh sẽ không nói về việc chọn nhiều.
            AddUxMultiSelection(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarMulti1024, 1024, 700);
            AddUxMultiSelection(scenarios, LiveOpsHubCaptureScenarioIds.UxCalendarMultiEnglish1024, 1024, 700,
                LiveOpsHubLanguageId.English);
        }

        /// <summary>Hai đợt của mẫu thiết kế mà cả 1024 lẫn các cỡ rộng đều vẽ — cùng cặp mà ca kiểm bố cục nhiều-chọn bấm.</summary>
        private static readonly string[] UxMultiSelectionBarKeys =
        {
            LiveOpsDesignSample.LavaQuestEarlyEntryKey, LiveOpsDesignSample.HuntEarlyEntryKey,
        };

        private static void AddUxMultiSelection(List<LiveOpsHubCaptureScenario> scenarios, string scenarioId, int width, int height,
            LiveOpsHubLanguageId language = LiveOpsHubLanguageId.Vietnamese)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(scenarioId, width, height, OpenUxCalendarMultiSelection)
                .WithLanguage(language)
                .WithExpectedFrames(UxSizeInvariantFrames()));
        }

        private static EditorWindow OpenUxCalendarMultiSelection()
        {
            LiveOpsHubServices services = UxSizesServices();
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            foreach (IHubSection section in sections)
            {
                // Đi qua SetSelectedBarKeys của presenter — cùng đường mà ⌘bấm thanh thứ hai đi, nên ảnh cho thấy đúng trạng
                // thái mà người dùng dựng được, kể cả phần thanh hành động hàng loạt ở chân màn.
                if (section is CalendarSection calendar)
                {
                    calendar.Presenter.SetSelectedBarKeys(UxMultiSelectionBarKeys, UxMultiSelectionBarKeys[0]);
                }
            }

            return LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Calendar);
        }

        private static void AddUxSize(List<LiveOpsHubCaptureScenario> scenarios, string scenarioId, int width, int height,
            bool withSelection, LiveOpsHubLanguageId language = LiveOpsHubLanguageId.Vietnamese)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(scenarioId, width, height, () => OpenUxCalendar(withSelection))
                .WithLanguage(language)
                .WithExpectedFrames(UxSizeInvariantFrames()));
        }

        private static void AddUxRecurringSize(List<LiveOpsHubCaptureScenario> scenarios, string scenarioId, int width, int height,
            LiveOpsHubLanguageId language = LiveOpsHubLanguageId.Vietnamese)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(scenarioId, width, height, OpenUxRecurring)
                .WithLanguage(language)
                .WithExpectedFrames(UxSizeInvariantFrames()));
        }

        /// <summary>
        /// Ba khung thiết kế KHÔNG đổi theo cỡ cửa sổ — đo được trên mọi ảnh của ma trận. Chiều rộng để 0 (= không đo chiều đó)
        /// vì bề rộng là cái đang thay đổi có chủ đích ở bộ ảnh này.
        /// </summary>
        /// <summary>
        /// Chiều cao header màn ở cửa sổ hẹp hơn <see cref="LiveOpsHubBreakpoints.MediumBelowWidth"/> (W9-01): phụ đề
        /// xuống dòng và nhóm nút xuống dòng riêng. Đo được 61 trên CẢ 42 ảnh hẹp của hai bản Unity.
        /// </summary>
        private const float UxSectionHeaderHeightBelowMedium = 61f;

        private static LiveOpsHubCaptureExpectedFrame[] UxSizeInvariantFrames()
        {
            return new[]
            {
                new LiveOpsHubCaptureExpectedFrame("liveops-hub-header", 0f, UxHeaderHeight),
                new LiveOpsHubCaptureExpectedFrame("liveops-hub-section-header", 0f, UxSectionHeaderHeight)
                    .WithHeightBelowWindowWidth(LiveOpsHubBreakpoints.MediumBelowWidth, UxSectionHeaderHeightBelowMedium),
                new LiveOpsHubCaptureExpectedFrame("liveops-hub-status", 0f, UxStatusBarHeight),
            };
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
