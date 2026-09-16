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

        // Số đo RIÊNG của màn 7.2 ([SD1 §2.1]): inspector 300, dãy ô màu 18×18, và tám cột bảng 44/140/giãn/120/140/90/84/36.
        // Không khai thì ba ảnh chỉ đo năm khung vỏ hub dùng chung mọi màn — "ĐẠT số đo" khi đó không chứng minh được số nào của 7.2.
        private const float EventTypesRailWidth = 196f;
        private const float EventTypesContentWidth = 1084f;
        private const float EventTypesInspectorWidth = 300f;
        private const float EventTypesColumnColorWidth = 44f;
        private const float EventTypesColumnTypeIdWidth = 140f;
        private const float EventTypesColumnEntryWidth = 120f;
        private const float EventTypesColumnConfigKeyWidth = 140f;
        private const float EventTypesColumnSourceWidth = 90f;
        private const float EventTypesColumnEventCountWidth = 84f;
        private const float EventTypesColumnStateWidth = 36f;

        static partial void RegisterEventTypes(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H10EventTypesDefault, EventTypesWidth, EventTypesHeight,
                    OpenEventTypesDefault)
                .WithExpectedFrames(EventTypesFrames()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H10bEventTypesUnknownType, EventTypesWidth,
                    EventTypesHeight, OpenEventTypesUnknownType)
                .WithExpectedFrames(EventTypesFrames()));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H10cEventTypesColorCollision, EventTypesWidth,
                    EventTypesHeight, OpenEventTypesColorCollision)
                .WithExpectedFrames(EventTypesFrames()));
        }

        /// <summary>
        /// Khung mong đợi của cả ba ảnh: hai khung vỏ hub (rail, cột nội dung) + số đo riêng của 7.2. Ba ảnh cùng một bố cục nên
        /// cùng một bộ số — khác nhau chỉ ở nội dung bảng và inspector.
        /// <para>
        /// Cột "Tên hiển thị" là cột GIÃN nên không có số thiết kế để khai. KHÔNG khai ô màu 18×18: ToolbarToggle không tự vẽ
        /// viền, thứ nhìn thấy là swatch 12×12 bên trong, nên dò cạnh luôn ra 12 dù worldBound đúng 18 ở cả hai skin (đo thật:
        /// 12 ở 15/16 ô, 16 và 19 ở hai ô cạnh ô đang bật). Ô 18×18 và swatch 12×12 khoá bằng <c>resolvedStyle</c> ở
        /// <c>EventTypesSectionTests.ColorSlots_MatchDesignSize</c> — chính xác hơn dò cạnh. Swatch 9×9 của bảng nằm trong hàng
        /// list không có tên element nên lệnh chụp không ghi số đo; nó khoá bằng USS.
        /// </para>
        /// </summary>
        private static LiveOpsHubCaptureExpectedFrame[] EventTypesFrames()
        {
            List<LiveOpsHubCaptureExpectedFrame> frames = new List<LiveOpsHubCaptureExpectedFrame>
            {
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Rail, EventTypesRailWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Content, EventTypesContentWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(LiveOpsHubPaths.EventTypesElementNames.Inspector, EventTypesInspectorWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(EventTypeTable.ColorColumnName, EventTypesColumnColorWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(EventTypeTable.TypeIdColumnName, EventTypesColumnTypeIdWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(EventTypeTable.EntryColumnName, EventTypesColumnEntryWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(EventTypeTable.ConfigKeyColumnName, EventTypesColumnConfigKeyWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(EventTypeTable.SourceColumnName, EventTypesColumnSourceWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(EventTypeTable.EventCountColumnName, EventTypesColumnEventCountWidth, 0f),
                new LiveOpsHubCaptureExpectedFrame(EventTypeTable.StateColumnName, EventTypesColumnStateWidth, 0f),
            };
            return frames.ToArray();
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
