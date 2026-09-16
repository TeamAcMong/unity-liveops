using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEngine;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Services dựng sẵn cho test và kịch bản chụp (9.1, 9.5): đồng hồ tay ở giờ của hình (13/9/2026 08:47 UTC), lệch +7, clipboard bộ nhớ,
    /// hộp file/xác nhận giả, người đăng cố định, không biên dịch, KHÔNG tự kiểm và KHÔNG tìm asset nhớ của người dùng. Asset trong bộ nhớ
    /// (<c>HideFlags.DontSave</c>) và asset file dưới <see cref="TestFolder"/> đều được ghi lại để <see cref="ReleaseAll"/> dọn trong
    /// <c>[TearDown]</c> — test đỏ giữa chừng không để lại phiên đang nghe Undo hay file rác.
    /// </summary>
    internal static class LiveOpsHubTestServices
    {
        public const string NoAssetScenario = "no-asset";
        public const string EmptyAssetScenario = "empty-asset";
        public const string DesignSampleScenario = "design-sample";
        public const string StaleCheckScenario = "stale-check";
        public const string RunningCheckScenario = "running-check";

        /// <summary>Thư mục asset thật của test (9.1) — tạo khi cần, xoá ở <see cref="ReleaseAll"/>.</summary>
        public const string TestFolder = "Assets/__LiveOpsHubTests__";

        public const string PublisherName = "DatHoUnityDev";
        public const string PublisherSource = "git user.name";

        /// <summary>Giờ của lần kiểm trong ngữ cảnh kiểm cũ — trùng câu thiết kế "Kết quả kiểm lúc 08:46:30".</summary>
        public static readonly DateTime CheckedAtUtc = new DateTime(2026, 9, 13, 8, 46, 30, DateTimeKind.Utc);

        /// <summary>Lịch đổi sau lần kiểm — "Lịch đã đổi lúc 08:46:50 UTC".</summary>
        public static readonly DateTime EditedAfterCheckUtc = new DateTime(2026, 9, 13, 8, 46, 50, DateTimeKind.Utc);

        /// <summary>Số luật đã xong trong ngữ cảnh đang kiểm — "Đang kiểm 7/12 luật…".</summary>
        public const int RunningCompletedRuleCount = 7;

        private static readonly List<LiveOpsHubServices> CreatedServices = new List<LiveOpsHubServices>();
        private static readonly List<UnityEngine.Object> CreatedMemoryAssets = new List<UnityEngine.Object>();
        private static readonly List<string> CreatedAssetGuids = new List<string>();
        private static bool _createdTestFolder;

        /// <summary>
        /// Mọi khoá SessionState của một phiên (khoá theo GUID asset, 8.10). <see cref="ReleaseAll"/> xoá hết cho từng asset của test:
        /// Unity cấp lại CÙNG một GUID khi test sau tạo lại asset ở ĐÚNG đường dẫn cũ, nên bản chụp "đã lưu"/"nháp" của test trước sẽ
        /// được phiên mới nạp lại và làm test sau đỏ theo cách rất khó lần (tab có * ngay khi vừa mở lịch sạch).
        /// </summary>
        private static readonly string[] SessionStoreNames =
        {
            LiveOpsHubCalendarSession.SavedSnapshotStoreName, LiveOpsHubCalendarSession.DraftSnapshotStoreName,
            LiveOpsHubCalendarSession.SavedFileHashStoreName, LiveOpsHubCalendarSession.CheckRunningStoreName,
            LiveOpsHubPublishState.ExportFormatStoreName, LiveOpsHubPublishState.LastExportedShaStoreName,
            LiveOpsHubPublishState.LastExportedUtcStoreName, LiveOpsHubPublishState.LastExportedViaFileStoreName,
            LiveOpsHubPublishState.ReviewedStoreName, LiveOpsHubPublishState.ActiveBaselineStoreName,
            LiveOpsHubRemoteSnapshot.PastedTextStoreName, LiveOpsHubRemoteSnapshot.VerifiedUtcStoreName,
        };

        public static IReadOnlyList<string> ContextScenarios => new[] { NoAssetScenario, EmptyAssetScenario, DesignSampleScenario, StaleCheckScenario };

        public static LiveOpsHubServices FromDesignSample()
        {
            return ForScenario(DesignSampleScenario);
        }

        /// <summary>Builder với mọi port giả của test; chưa gắn asset (gọi <c>WithCalendarAsset</c> để có).</summary>
        public static LiveOpsHubServicesBuilder CreateBuilder(ManualLiveOpsClock clock)
        {
            return new LiveOpsHubServicesBuilder()
                .WithClock(clock ?? CreateClock())
                .WithTimeZone(new ManualLiveOpsHubTimeZone(LiveOpsDesignSample.DeviceOffset))
                .WithClipboard(new InMemoryLiveOpsHubClipboard())
                .WithFileDialog(new ManualLiveOpsHubFileDialog())
                .WithPublisherIdentity(new ManualLiveOpsHubPublisherIdentity(PublisherName, PublisherSource))
                .WithCompilationState(new ManualLiveOpsHubCompilationState(false))
                .WithConfirmation(new ScriptedLiveOpsHubConfirmationPresenter())
                .WithAutoCheckOnOpen(false);
        }

        public static ManualLiveOpsClock CreateClock()
        {
            return new ManualLiveOpsClock(LiveOpsDesignSample.NowUtc, true);
        }

        /// <summary>Dựng services và ghi lại để dọn; mọi đường tạo services của test đi qua đây.</summary>
        public static LiveOpsHubServices Build(LiveOpsHubServicesBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            LiveOpsHubServices services = builder.Build();
            CreatedServices.Add(services);
            return services;
        }

        /// <summary>Ngữ cảnh cửa sổ (9.2 HubWindowTests, 9.3 probe): không asset · asset rỗng · mẫu thiết kế · kiểm cũ · đang kiểm.</summary>
        public static LiveOpsHubServices ForScenario(string scenarioId)
        {
            ManualLiveOpsClock clock = CreateClock();
            switch (scenarioId)
            {
                case NoAssetScenario:
                    return Build(CreateBuilder(clock).WithCalendarAsset(null));
                case EmptyAssetScenario:
                    return Build(CreateBuilder(clock).WithCalendarAsset(CreateMemoryAsset(LiveEventCalendarDocument.Empty)));
                case DesignSampleScenario:
                {
                    LiveOpsHubServices services = Build(CreateBuilder(clock).WithCalendarAsset(CreateMemoryAsset(LiveOpsDesignSample.Document)));
                    services.Session.RunCheckToCompletion();
                    return services;
                }
                case StaleCheckScenario:
                {
                    clock.Set(CheckedAtUtc);
                    LiveOpsHubServices services = Build(CreateBuilder(clock).WithCalendarAsset(CreateMemoryAsset(LiveOpsDesignSample.Document)));
                    services.Session.RunCheckToCompletion();
                    clock.Set(LiveOpsDesignSample.NowUtc);
                    services.Session.Check.MarkCalendarEdited(EditedAfterCheckUtc);
                    return services;
                }
                case RunningCheckScenario:
                {
                    LiveOpsHubServices services = Build(CreateBuilder(clock).WithCalendarAsset(CreateMemoryAsset(LiveOpsDesignSample.Document)));
                    // Không qua StartCheck: nhịp EditorApplication.update sẽ chạy tiếp các luật trong lúc test chờ layout.
                    services.Session.Check.Begin(services.Session.BuildCheckContext(clock.UtcNow));
                    for (int step = 0; step < RunningCompletedRuleCount; step++) services.Session.Check.Step();
                    return services;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(scenarioId), scenarioId, "kịch bản services lạ");
            }
        }

        /// <summary>Asset lịch chỉ trong bộ nhớ (không file, không GUID) — Lưu trả false, SessionState giữ trong kho bộ nhớ của phiên.</summary>
        public static LiveEventCalendarAsset CreateMemoryAsset(LiveEventCalendarDocument document)
        {
            LiveEventCalendarAsset asset = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
            asset.hideFlags = HideFlags.DontSave;
            asset.name = "LiveOpsHubTestCalendar";
            asset.ApplyDocument(document ?? LiveEventCalendarDocument.Empty);
            CreatedMemoryAssets.Add(asset);
            return asset;
        }

        /// <summary>Asset lịch thật trên đĩa dưới <see cref="TestFolder"/>, đã lưu (không bẩn).</summary>
        public static LiveEventCalendarAsset CreateAssetFile(string fileName, LiveEventCalendarDocument document)
        {
            if (!AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.CreateFolder("Assets", TestFolder.Substring("Assets/".Length));
                _createdTestFolder = true;
            }
            string path = TestFolder + "/" + fileName;
            AssetDatabase.DeleteAsset(path);
            LiveEventCalendarAsset asset = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
            asset.ApplyDocument(document ?? LiveEventCalendarDocument.Empty);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid))
            {
                // Xoá ngay lúc tạo nữa (không chỉ lúc dọn): test đỏ giữa chừng ở lượt trước có thể đã để lại khoá của cùng GUID.
                EraseSessionState(guid);
                if (!CreatedAssetGuids.Contains(guid)) CreatedAssetGuids.Add(guid);
            }
            return asset;
        }

        /// <summary>Xoá mọi khoá SessionState của một asset lịch — xem <see cref="SessionStoreNames"/>.</summary>
        public static void EraseSessionState(string assetGuid)
        {
            if (string.IsNullOrEmpty(assetGuid)) return;
            foreach (string name in SessionStoreNames) SessionState.EraseString(LiveOpsHubSessionStore.KeyOf(name, assetGuid));
        }

        /// <summary>Dọn mọi phiên (gỡ nghe Undo/postprocessor, dừng nhịp kiểm), asset bộ nhớ và thư mục asset thật của test.</summary>
        public static void ReleaseAll()
        {
            foreach (LiveOpsHubServices services in CreatedServices) services.Session.Dispose();
            CreatedServices.Clear();
            foreach (UnityEngine.Object asset in CreatedMemoryAssets)
            {
                if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
            }
            CreatedMemoryAssets.Clear();
            foreach (string guid in CreatedAssetGuids) EraseSessionState(guid);
            CreatedAssetGuids.Clear();
            if (AssetDatabase.IsValidFolder(TestFolder) || _createdTestFolder)
            {
                AssetDatabase.DeleteAsset(TestFolder);
                _createdTestFolder = false;
            }
        }
    }
}
