using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng EventTypes (G-EVENTTYPES, W4 — ma trận 9.5 dòng 10, 10b, 10c): màn Loại event mặc định (Hình 10), sau khi
    /// dán JSON có loại chưa khai báo (Hình 10b, V-17) và biến thể trùng màu chưa xử lý (Hình 10c).
    /// <para>
    /// Cả ba mở cửa sổ hub thật bằng <c>OpenWithServices</c> với CÙNG một services cho cửa sổ và registry — màn thật đọc phiên, nên
    /// đường <c>OpenForTest</c> (hai phiên) của W2 sẽ chụp ra bảng rỗng.
    /// </para>
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        private const int EventTypesWidth = 1280;
        private const int EventTypesHeight = 760;

        /// <summary>Loại chỉ có trong JSON đang chạy đã dán (Hình 10b) — băm ra ô 5, không đụng ô nào của năm loại mẫu.</summary>
        private const string EventTypesRemoteOnlyType = "lucky-spin";

        /// <summary>Loại của Hình 10c và ô theo băm của nó — ô 7 trùng treasure-hunt, đúng tình huống "chưa xử lý".</summary>
        private const string EventTypesCollisionType = "star-tournament";
        private const int EventTypesCollisionSlot = 7;

        static partial void RegisterEventTypes(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H10EventTypesDefault, EventTypesWidth, EventTypesHeight,
                OpenEventTypesDefault));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H10bEventTypesUnknownType, EventTypesWidth,
                EventTypesHeight, OpenEventTypesUnknownType));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H10cEventTypesColorCollision, EventTypesWidth,
                EventTypesHeight, OpenEventTypesColorCollision));
        }

        /// <summary>
        /// Hình 10: lịch mẫu đã kiểm, đang chọn star-tournament — loại này đang ghi đè ô 6 steel nên inspector hiện dòng phụ
        /// quiet "theo băm trùng treasure-hunt", đúng thứ hình thiết kế vẽ.
        /// </summary>
        private static EditorWindow OpenEventTypesDefault()
        {
            return OpenEventTypes(LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario), EventTypesCollisionType);
        }

        /// <summary>
        /// Hình 10b: đặt bản remote qua API phiên (không mở popover Dán JSON — popover là của G-PASTE, W5) rồi kiểm lại, nên luật 8
        /// sinh phát hiện "loại chưa khai báo" của bản dán (V-17).
        /// </summary>
        private static EditorWindow OpenEventTypesUnknownType()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            services.Session.Remote.Set(RemoteJsonWithUnknownEventType(), LiveOpsDesignSample.NowUtc);
            services.Session.RunCheckToCompletion();
            return OpenEventTypes(services);
        }

        /// <summary>
        /// Hình 10c: star-tournament vẫn ở ô 7 theo băm nên trùng treasure-hunt — inspector hiện dòng Warning kèm ô trống gợi ý,
        /// và hub KHÔNG tự dời màu (PD Q1).
        /// </summary>
        private static EditorWindow OpenEventTypesColorCollision()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            document.TryGetEventType(EventTypesCollisionType, out LiveEventTypeDefinition type);
            LiveEventCalendarEdits.TryApply(document, new SetEventTypeEdit(type.WithColorSlot(EventTypesCollisionSlot)),
                out LiveEventCalendarDocument collided);
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(collided)));
            services.Session.RunCheckToCompletion();
            return OpenEventTypes(services, EventTypesCollisionType);
        }

        private static EditorWindow OpenEventTypes(LiveOpsHubServices services)
        {
            return OpenEventTypes(services, string.Empty);
        }

        private static EditorWindow OpenEventTypes(LiveOpsHubServices services, string selectedTypeId)
        {
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            LiveOpsHubWindow window = LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.EventTypes);
            if (selectedTypeId.Length == 0) return window;

            // Chọn loại đúng như người dùng bấm vào hàng: đi qua ApplyNavigation (đường điều hướng thật), không thọc vào field.
            for (int index = 0; index < sections.Count; index++)
            {
                EventTypesSection section = sections[index] as EventTypesSection;
                if (section == null) continue;
                section.ApplyNavigation(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.EventTypes).WithEventType(selectedTypeId, false));
            }
            return window;
        }

        private static string RemoteJsonWithUnknownEventType()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.DocumentWithoutPublishedStamp())
                .WithFixedEvent(new FixedLiveEventEntry("entry-lucky-1", "lucky-spin-0919", EventTypesRemoteOnlyType,
                    "2026-09-19T00:00:00Z", "2026-09-20T00:00:00Z", "lucky_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-lucky-2", "lucky-spin-0926", EventTypesRemoteOnlyType,
                    "2026-09-26T00:00:00Z", "2026-09-27T00:00:00Z", "lucky_v1"))
                .Build();
            return LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2).Text;
        }
    }
}
