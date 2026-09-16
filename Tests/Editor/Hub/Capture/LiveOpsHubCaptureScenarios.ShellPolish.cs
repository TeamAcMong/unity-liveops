using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng ShellPolish (G-SHELLPOLISH, W5): Hình 9 (b) nhiều asset lịch, Hình 28 khung 4 (băng asset đổi trên
    /// đĩa) và hộp "Ghi đè … mục vừa đổi trên đĩa?", cùng hai khung hẹp 820 (rail 36 px) và 700 (compact).
    /// <para>
    /// Hai kịch bản đầu dựng trạng thái mà KHÔNG đụng đĩa lâu dài: (b) tạo một asset lịch tạm chỉ để phiên đếm được hai cái rồi
    /// xoá ngay trong cùng lượt (phiên chụp số lúc <c>Initialize</c>), còn khung 4 dựng thẳng
    /// <see cref="LiveOpsHubDiskConflict"/> từ ba tài liệu — chụp ảnh không được để lại file thừa trong project.
    /// </para>
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        private const int NarrowCaptureWidth = 820;
        private const int NarrowCaptureHeight = 560;
        private const int CompactCaptureWidth = 700;
        private const int CompactCaptureHeight = 520;
        private const float NarrowRailWidth = 36f;

        /// <summary>Khoá EditorUserSettings riêng của lượt chụp — không bao giờ đè GUID lịch mà người dùng đang mở (PD-16).</summary>
        private const string CaptureAssetLocatorKey = "DreamTech.LiveOps.Hub.Capture.CalendarAssetGuid";

        /// <summary>Asset lịch thứ hai, tạo rồi xoá ngay trong cùng lượt để trạng thái (b) có thật mà project vẫn sạch.</summary>
        private const string SecondCalendarAssetPath = "Assets/LiveOpsHubCaptureSecondCalendar.asset";

        // Ba mục khác nhau giữa đĩa và Editor + một thay đổi chưa lưu — đúng các con số của Hình 28 khung 4.
        private const string DiskHuntEndUtc = "2026-09-17T12:00:00Z";
        private const string DiskLavaQuestLateEndUtc = "2026-10-04T00:00:00Z";
        private const int DiskWeeklyPassActiveHours = 120;
        private const string SavedLavaQuestMidEndUtc = "2026-09-19T00:00:00Z";
        private const string DiskConflictAssetFileName = "Main.asset";

        static partial void RegisterShellPolish(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H09bOverviewMultipleAssets, StandardWidth, StandardHeight,
                OpenOverviewWithMultipleAssets));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H28dDiskChangedBanner, StandardWidth, StandardHeight,
                OpenDiskChangedBanner));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H28eConfirmOverwriteDisk, (int)LiveOpsConfirmWindow.Width,
                    (int)LiveOpsConfirmWindow.Level1Height, () => OpenConfirm(ConfirmOverwriteDiskSampleRequest(), string.Empty),
                    window => ((LiveOpsConfirmWindow)window).Content)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsConfirmContent.RootElementName, LiveOpsConfirmWindow.Width,
                    LiveOpsConfirmWindow.Level1Height)));

            // Hình 13 chụp ở 820: rail phải là 36 px (I-8), không phải 196 — đó là toàn bộ lý do G-CALENDAR-DEPTH đợi gói này.
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.HsShellNarrow820, NarrowCaptureWidth, NarrowCaptureHeight,
                    OpenShellWithSession)
                .WithExpectedFrames(
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Rail, NarrowRailWidth, 0f),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Header, 0f, 26f),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Status, 0f, 20f),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Content, NarrowCaptureWidth - NarrowRailWidth, 0f)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.HsShellCompact700, CompactCaptureWidth, CompactCaptureHeight,
                    OpenShellWithSession)
                .WithExpectedFrames(
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Rail, NarrowRailWidth, 0f),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Header, 0f, 26f),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Status, 0f, 20f),
                    new LiveOpsHubCaptureExpectedFrame(LiveOpsHubClassNames.Content, CompactCaptureWidth - NarrowRailWidth, 0f)));
        }

        /// <summary>
        /// (b) project có hai asset lịch [SD1 §1.3]. Asset thứ hai chỉ cần TỒN TẠI lúc phiên khởi tạo: <c>CalendarAssetCount</c>
        /// chụp số ngay tại đó, nên xoá file ngay sau khi dựng services vẫn giữ nguyên trạng thái cần chụp.
        /// </summary>
        private static EditorWindow OpenOverviewWithMultipleAssets()
        {
            LiveOpsHubServices services;
            LiveEventCalendarAsset secondAsset = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
            secondAsset.ApplyDocument(LiveEventCalendarDocument.Empty);
            AssetDatabase.CreateAsset(secondAsset, SecondCalendarAssetPath);
            try
            {
                services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document))
                    .WithAssetLocator(new LiveOpsHubAssetLocator(CaptureAssetLocatorKey)));
            }
            finally
            {
                AssetDatabase.DeleteAsset(SecondCalendarAssetPath);
            }
            services.Session.RunCheckToCompletion();
            return OpenOverview(services);
        }

        /// <summary>
        /// Hình 28 khung 4. Băng dựng thẳng từ ba tài liệu thay vì sửa file thật: ảnh cần ĐÚNG các con số của hình (3 mục khác
        /// nhau, 1 thay đổi chưa lưu), mà một lần ghi file thật lại phụ thuộc thứ tự import của lượt chụp.
        /// </summary>
        private static EditorWindow OpenDiskChangedBanner()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            LiveOpsHubWindow window = LiveOpsHubWindow.OpenWithServices(services, LiveOpsHubSections.Ids.Overview);
            window.HubRoot?.AddToClassList(LiveOpsHubClassNames.NoMotion);

            LiveEventCalendarDocument editorDocument = LiveOpsDesignSample.Document;
            LiveOpsHubDiskConflict conflict = new LiveOpsHubDiskConflict(LiveOpsDesignSample.NowUtc,
                BuildDiskDocument(editorDocument), editorDocument, BuildSavedDocument(editorDocument));
            LiveOpsHubDiskConflictBanner banner = LiveOpsHubDiskConflictBanner.Create(conflict, DiskConflictAssetFileName, services.Format,
                () => { }, () => { }, () => { });
            window.ShellNotes?.Add(banner.Element);
            return window;
        }

        /// <summary>Bản trên đĩa: hunt-0914 và lava-quest-2026-10 đổi giờ, luật weekly-pass đổi thời gian chạy — ba mục.</summary>
        private static LiveEventCalendarDocument BuildDiskDocument(LiveEventCalendarDocument editorDocument)
        {
            LiveEventCalendarDocument document = ReplaceEnd(editorDocument, LiveOpsDesignSample.HuntEarlyEntryKey, DiskHuntEndUtc);
            document = ReplaceEnd(document, LiveOpsDesignSample.LavaQuestLateEntryKey, DiskLavaQuestLateEndUtc);
            RecurringLiveEventRule rule;
            if (!document.TryGetRecurringRule("weekly-pass", out rule)) return document;
            RecurringLiveEventRule changed = new RecurringLiveEventRule(rule.EventType, rule.AnchorUtcText, rule.IdPrefix,
                rule.PeriodHours, DiskWeeklyPassActiveHours, rule.ConfigKey);
            return Apply(document, new SetRecurringRuleEdit(changed));
        }

        /// <summary>Bản đã lưu lần trước: chỉ khác nháp ở lava-quest-2026-09b — đúng "1 thay đổi chưa lưu" của hình.</summary>
        private static LiveEventCalendarDocument BuildSavedDocument(LiveEventCalendarDocument editorDocument)
        {
            return ReplaceEnd(editorDocument, LiveOpsDesignSample.LavaQuestMidEntryKey, SavedLavaQuestMidEndUtc);
        }

        private static LiveEventCalendarDocument ReplaceEnd(LiveEventCalendarDocument document, string entryKey, string endUtcText)
        {
            FixedLiveEventEntry entry;
            if (!document.TryGetFixedEvent(entryKey, out entry)) return document;
            return Apply(document, new ReplaceFixedEventEdit(new FixedLiveEventEntry(entry.EntryKey, entry.EventId, entry.EventType,
                entry.StartUtcText, endUtcText, entry.ConfigKey)));
        }

        private static LiveEventCalendarDocument Apply(LiveEventCalendarDocument document, LiveEventCalendarEdit edit)
        {
            LiveEventCalendarDocument result;
            if (!LiveEventCalendarEdits.TryApply(document, edit, out result))
            {
                throw new InvalidOperationException("kịch bản chụp khung 4: không dựng được bản trên đĩa — lệnh sửa không áp được");
            }
            return result;
        }

        /// <summary>Hộp cấp 1 của ⌘S sau khi chọn "Giữ bản trong Editor" — cùng câu mà <c>LiveOpsHubWindow</c> dựng.</summary>
        internal static LiveOpsConfirmRequest ConfirmOverwriteDiskSampleRequest()
        {
            LiveEventCalendarDocument editorDocument = LiveOpsDesignSample.Document;
            LiveOpsHubDiskConflict conflict = new LiveOpsHubDiskConflict(LiveOpsDesignSample.NowUtc,
                BuildDiskDocument(editorDocument), editorDocument, BuildSavedDocument(editorDocument));
            LiveOpsHubFormat format = new LiveOpsHubFormat(TimeSpan.Zero);
            string itemList = LiveOpsHubDiskConflictBanner.ItemListOf(conflict.DiskVersusEditor);
            return new LiveOpsConfirmRequest.Builder()
                .WithLevel(LiveOpsConfirmLevel.Level1)
                .WithTitle(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.ShellOverwriteDiskConfirmTitleFormat, format.Integer(conflict.DiskVersusEditor.ChangeCount)))
                .WithBody(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.ShellOverwriteDiskConfirmBodyFormat, DiskConflictAssetFileName, itemList))
                .WithKeyHint(LiveOpsHubStrings.ShellOverwriteDiskConfirmKeyHint)
                .WithButtons(LiveOpsHubStrings.ShellOverwriteDiskConfirmDestructive, LiveOpsHubStrings.ShellOverwriteDiskConfirmSafe)
                .Build();
        }
    }
}
