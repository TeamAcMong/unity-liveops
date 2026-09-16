using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng Paste (G-PASTE, W5) — ba ảnh của luồng Dán / Nhập JSON đang chạy:
    /// <list type="bullet">
    /// <item><c>h08c-confirm-replace-draft</c>: hộp cấp 1 "Thay lịch nháp bằng JSON đã dán?" (Hình 8 ô 3).</item>
    /// <item><c>h17-9-remote-drift</c>: màn Kiểm lịch sau khi dán một bản remote LỆCH với dấu đã đăng ([SD2 §2.8]).</item>
    /// <item><c>h09f-overview-import-json</c>: Tổng quan trạng thái (a) với popover "Nhập JSON đang chạy" đã dán, Toggle
    /// ghi dấu bật (vá V-14).</item>
    /// </list>
    /// Cả ba đi qua ĐƯỜNG THẬT: hộp dựng bằng <c>LiveOpsHubPasteRunningJsonAction.BuildReplaceConfirmRequest</c>, bản remote
    /// đặt bằng chính phiên, popover dựng bằng <c>CreatePopover</c> — ảnh chứng minh sản phẩm, không chứng minh file kịch bản.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        /// <summary>
        /// Bản remote LỆCH: đúng bản đã đăng 11/9 16:20, sửa hai mục — hunt-0914 đổi configKey và lava-quest-2026-09b đổi giờ
        /// kết thúc. Hai mục là con số của câu thiết kế "Bản remote khác dấu 11/9 16:20: 2 đợt".
        /// </summary>
        private static string PasteDriftedRemoteJson()
        {
            return LiveOpsDesignSample.PublishedSnapshotJson
                .Replace("\"configKey\": \"hunt_v1\"", "\"configKey\": \"hunt_v3\"")
                .Replace("\"endUtc\": \"2026-09-19T00:00:00Z\"", "\"endUtc\": \"2026-09-21T00:00:00Z\"");
        }

        static partial void RegisterPaste(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H08cConfirmReplaceDraft,
                    (int)LiveOpsConfirmWindow.Width, (int)LiveOpsConfirmWindow.Level1Height,
                    () => OpenConfirm(PasteReplaceConfirmRequest(), string.Empty),
                    window => ((LiveOpsConfirmWindow)window).Content)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsConfirmContent.RootElementName,
                    LiveOpsConfirmWindow.Width, LiveOpsConfirmWindow.Level1Height)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H179RemoteDrift, StandardWidth, StandardHeight,
                OpenValidationRemoteDrift));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H09fOverviewImportJson, StandardWidth, StandardHeight,
                OpenOverviewImportPopover));
        }

        /// <summary>
        /// Hộp ô 3 của Hình 8 — dựng từ nháp mẫu và bản đã đăng làm "JSON đã dán": đúng năm đợt trùng id, một đợt bị xoá
        /// (hunt-0916-bonus) và một luật đổi (weekly-pass), tức câu mẫu của [SD2 §2.9].
        /// </summary>
        internal static LiveOpsConfirmRequest PasteReplaceConfirmRequest()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            LiveOpsHubPasteRunningJsonAction action = (LiveOpsHubPasteRunningJsonAction)services.Actions;
            LiveEventCalendarDocumentParseResult parsed = services.JsonReadBack.ReadBackDocument(PasteDriftedRemoteJson());
            return action.BuildReplaceConfirmRequest(parsed.Document);
        }

        /// <summary>
        /// Trạng thái "đã dán JSON và lệch bản" ([SD2 §2.8]): nhóm Chưa kiểm của luật 12 biến mất, thay bằng hàng Warning
        /// "Bản remote khác dấu …" — đặt bản remote bằng chính phiên rồi kiểm lại như người dùng bấm.
        /// </summary>
        private static EditorWindow OpenValidationRemoteDrift()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            services.Session.Remote.Set(PasteDriftedRemoteJson(), LiveOpsDesignSample.NowUtc);
            services.Session.RunCheckToCompletion();
            return OpenValidation(services, null);
        }

        /// <summary>
        /// Tổng quan (a) + popover "Nhập JSON đang chạy" đã dán. Popover là cửa sổ HĐH riêng nên không nằm trong ảnh của
        /// cửa sổ hub: gắn CHÍNH cây của nó vào root hub (cùng lớp, cùng dữ liệu, cùng gate đọc-lại) — cùng cách Hình 17 ô 5
        /// và ô 7 đã làm.
        /// </summary>
        private static EditorWindow OpenOverviewImportPopover()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario);
            EditorWindow window = OpenOverview(services);
            LiveOpsHubPasteRunningJsonAction action = (LiveOpsHubPasteRunningJsonAction)services.Actions;
            PasteRunningJsonPopover popover = action.CreatePopover(PasteRunningJsonMode.ImportIntoNewAsset);
            VisualElement built = popover.BuildForTest();
            window.rootVisualElement.Add(built);
            // Dán sẵn bản đang chạy: ảnh phải vẽ dòng "Đọc được: …" và nút chính ĐÃ MỞ, đúng khung của vá V-14.
            popover.PasteForTest(LiveOpsDesignSample.PublishedSnapshotJson);
            return window;
        }
    }
}
