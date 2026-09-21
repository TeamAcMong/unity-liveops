using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// (W11) Hai ảnh BẰNG CHỨNG của nợ bố cục dữ liệu dài: màn Lịch và màn Loại event đứng trên bộ dữ liệu xấu nhất, tức
    /// trên chuỗi dài BẰNG ĐÚNG giới hạn của <see cref="LiveOpsIdentifierLimits"/>.
    /// <para>
    /// Vì sao cần ảnh khi gói này không đổi một pixel nào của sản phẩm: 12 mục hoãn của
    /// <see cref="UxLayoutDeferralList"/> nói "còn 176 chỗ không dùng được" bằng SỐ. Số không cho người duyệt thấy 176 chỗ
    /// ấy trông ra sao, và đợt W11 sẽ phải quyết định hình dạng mới của sáu màn dựa trên chính cảnh ấy. Không ma trận ảnh
    /// nào trước đây chụp dữ liệu xấu nhất — cả 24 hình của ma trận 9.5 đều đứng trên mẫu ĐẸP.
    /// </para>
    /// <para>
    /// Chụp ở 1280x760 — cỡ THIẾT KẾ, và là cỡ rộng nhất mà màn hình máy chạy cổng không kẹp (1440x900 bị kẹp còn
    /// 1440x881, xem cờ <c>clamped</c> của mọi lượt kiểm bố cục). Một ảnh ở cỡ bị kẹp nói về một cửa sổ không ai có.
    /// </para>
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        /// <summary>Cỡ cửa sổ của hai ảnh bằng chứng — xem chú thích lớp cho lý do không lấy cỡ rộng hơn.</summary>
        private const int WorstCaseCaptureWidth = 1280;

        private const int WorstCaseCaptureHeight = 760;

        static partial void RegisterWorstCase(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.W11CalendarWorstData,
                WorstCaseCaptureWidth, WorstCaseCaptureHeight, OpenWorstCaseCalendar));
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.W11EventTypesWorstData,
                WorstCaseCaptureWidth, WorstCaseCaptureHeight, OpenWorstCaseEventTypes));
        }

        private static EditorWindow OpenWorstCaseCalendar()
        {
            LiveOpsHubServices services = WorstCaseCaptureServices();
            return LiveOpsHubWindow.OpenWithServices(services, LiveOpsHubSections.Create(services),
                LiveOpsHubSections.Ids.Calendar);
        }

        private static EditorWindow OpenWorstCaseEventTypes()
        {
            LiveOpsHubServices services = WorstCaseCaptureServices();
            return LiveOpsHubWindow.OpenWithServices(services, LiveOpsHubSections.Create(services),
                LiveOpsHubSections.Ids.EventTypes);
        }

        /// <summary>Đúng bộ services mà <c>UxLayoutAuditTests.WorstCaseServices</c> dựng — ảnh và số phải nói về một cây.</summary>
        private static LiveOpsHubServices WorstCaseCaptureServices()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsWorstCaseSample.Document)));
            services.Session.RunCheckToCompletion();
            return services;
        }
    }
}
