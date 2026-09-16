using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng Overview (G-OVERVIEW, W4): Hình 4/5 (thân mặc định, Dark + Light), Hình 9 (a) chưa có asset và (c)
    /// không còn việc chặn, cùng hai trạng thái chưa vẽ trong tài liệu (chưa kiểm lần nào · đang kiểm). Mọi kịch bản mở cửa sổ
    /// hub THẬT với registry dựng từ CÙNG một services — màn và cửa sổ phải nhìn chung một phiên.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        /// <summary>
        /// (PD-45) Mẫu tiếng Anh để user soát bản dịch — không thuộc ma trận 9.5 nên KHÔNG có hình thiết kế đối chiếu.
        /// Hằng nằm ở đây thay vì <see cref="LiveOpsHubCaptureScenarioIds"/> vì file đó thuộc quyền ghi của G-SKELETON/G-I18N
        /// (xem plan/w4/contract-changes-G-OVERVIEW.md — cổng đợt dời hằng về file id chung).
        /// </summary>
        internal const string OverviewDefaultEnglishScenarioId = "h04-overview-default-en";

        private const int OverviewWidth = 1280;
        private const int OverviewHeight = 760;

        /// <summary>Số đo Hình 4 phải giữ: cột "chặn Copy JSON" 96px, nút việc cần làm 150px, thân flow 48px, cột bảng 170/250/140/130.</summary>
        private const float NeedsActionBlocksWidth = 96f;
        private const float NeedsActionButtonWidth = 150f;
        private const float FlowBodyHeight = 48f;
        private const float UpcomingTimeColumnWidth = 170f;
        private const float UpcomingEventColumnWidth = 250f;
        private const float UpcomingTypeColumnWidth = 140f;
        private const float UpcomingKindColumnWidth = 130f;
        private const float RailWidth = 196f;
        private const float ContentWidth = 1084f;

        static partial void RegisterOverview(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H04OverviewDefault, OverviewWidth, OverviewHeight,
                    () => OpenOverview(LiveOpsHubTestServices.DesignSampleScenario))
                .WithExpectedFrames(DefaultFrames()));

            scenarios.Add(new LiveOpsHubCaptureScenario(OverviewDefaultEnglishScenarioId, OverviewWidth, OverviewHeight,
                    () => OpenOverview(LiveOpsHubTestServices.DesignSampleScenario))
                .WithExpectedFrames(DefaultFrames())
                .WithLanguage(LiveOpsHubLanguageId.English));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H09aOverviewNoAsset, OverviewWidth, OverviewHeight,
                () => OpenOverview(LiveOpsHubTestServices.NoAssetScenario)));

            // (c) "không còn việc chặn": lịch rỗng đã kiểm xong — còn đúng một luật chưa đo được (bản remote chưa dán).
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H09cOverviewNoBlockers, OverviewWidth, OverviewHeight,
                OpenNoBlockers));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H09dOverviewNeverChecked, OverviewWidth, OverviewHeight,
                OpenNeverChecked));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H09eOverviewChecking, OverviewWidth, OverviewHeight,
                () => OpenOverview(LiveOpsHubTestServices.RunningCheckScenario)));
        }

        private static LiveOpsHubCaptureExpectedFrame[] DefaultFrames()
        {
            return new[]
            {
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Rail, RailWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Content, ContentWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.OverviewNeedBlocks, NeedsActionBlocksWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.OverviewNeedButton, NeedsActionButtonWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.OverviewFlow, 0f, FlowBodyHeight),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.OverviewUpcomingCellTime, UpcomingTimeColumnWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.OverviewUpcomingCellEvent, UpcomingEventColumnWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.OverviewUpcomingCellType, UpcomingTypeColumnWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.OverviewUpcomingCellKind, UpcomingKindColumnWidth, 0f),
            };
        }

        private static EditorWindow OpenOverview(string scenarioId)
        {
            return OpenOverview(LiveOpsHubTestServices.ForScenario(scenarioId));
        }

        private static EditorWindow OpenOverview(LiveOpsHubServices services)
        {
            return LiveOpsHubWindow.OpenWithServices(services, LiveOpsHubSections.Create(services), LiveOpsHubSections.Ids.Overview);
        }

        /// <summary>Lịch rỗng đã kiểm xong: không phát hiện nào, nhưng luật bản remote vẫn chưa đo được — đúng trạng thái (c).</summary>
        private static EditorWindow OpenNoBlockers()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.EmptyAssetScenario);
            services.Session.RunCheckToCompletion();
            return OpenOverview(services);
        }

        /// <summary>Lịch mẫu nhưng CHƯA kiểm lần nào: metric CẦN XỬ LÝ phải là vòng rỗng, không phải số 0.</summary>
        private static EditorWindow OpenNeverChecked()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            return OpenOverview(services);
        }
    }
}
