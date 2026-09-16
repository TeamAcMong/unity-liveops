using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Kịch bản chụp vùng Export (G-EXPORT, W4) — Hình 18, 19, 20, 21 và hai hộp cấp 1 của Hình 8:
    /// <list type="bullet">
    /// <item><c>h18-export-blocked</c>: mẫu thiết kế nguyên trạng — (a) còn 2 đợt bị bỏ và 1 thay đổi bắt buộc chưa xem.</item>
    /// <item>12 kịch bản <c>h19-*</c>: đủ mã (b)…(k) + parser lệch, dựng bằng cách đổi ĐÚNG một điều kiện mỗi lần.</item>
    /// <item><c>h19-l-compare-remote</c> (V-13): card diff so với bản remote đã dán — không Toggle "Đã xem".</item>
    /// <item><c>h20a/b/c</c>: ba biến thể hộp Đánh dấu đã đăng (410×330).</item>
    /// <item><c>h21-export-json-top</c>: JSON viewer từ dòng 1.</item>
    /// <item><c>h07d</c>/<c>h07e</c>: outcome trong cửa sổ sau khi Copy và sau khi Ghi dấu (cần host của G-HOSTUI).</item>
    /// <item><c>h08d</c>/<c>h08g</c>: hộp cấp 1 "Khôi phục vào nháp" và "Gỡ dấu đã đăng".</item>
    /// </list>
    /// (V-16) Hai kịch bản parser — (h) và lệch — dựng bằng <see cref="RewritingLiveOpsHubJsonReadBack"/>, không phải bằng
    /// dữ liệu lịch hỏng: JSON do hub viết ra vẫn đúng, chỉ bộ đọc lại bị làm sai đúng một chỗ.
    /// </summary>
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static partial class LiveOpsHubCaptureScenarios
    {
        private const string ExportRestoreUndoName = "Khôi phục bản đã đăng 11/9 16:20 vào nháp";
        private const string ExportMarkNote = "mở hunt-0916-bonus cho sự kiện giữa tháng";
        private const string ExportDraftSha = "5b0d93aa11223344556677889900aabbccddeeff00112233445566778899aabb";
        private const string ExportCopiedSha = "91aa02ff11223344556677889900aabbccddeeff00112233445566778899aabb";
        private const int ExportSampleByteCount = 1612;
        private const string ExportSampleAssetFileName = "Main.asset";

        private static readonly DateTime ExportCopiedUtc = new DateTime(2026, 9, 13, 9, 2, 0, DateTimeKind.Utc);
        private static readonly DateTime ExportMarkNowUtc = new DateTime(2026, 9, 13, 9, 4, 0, DateTimeKind.Utc);

        static partial void RegisterExport(List<LiveOpsHubCaptureScenario> scenarios)
        {
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H18ExportBlocked, () => OpenExport(LiveOpsDesignSample.Document, null, null)));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19BReady, () => OpenExport(ExportFixedDraft(), null, ReviewEverythingRequired)));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19CCopied, () => OpenExport(ExportFixedDraft(), null, section =>
            {
                ReviewEverythingRequired(section);
                section.Services.Session.Publish.RecordExport(section.Services.Session.Publish.CurrentJson, false);
            })));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19CprimeSavedFile, () => OpenExport(ExportFixedDraft(), null, section =>
            {
                ReviewEverythingRequired(section);
                section.Services.Session.Publish.RecordExport(section.Services.Session.Publish.CurrentJson, true);
            })));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19DStale, OpenExportWithStaleCheck));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19EMarked, () => OpenExport(ExportPublishedDraft(), null, section =>
                section.Services.Session.Publish.RecordExport(section.Services.Session.Publish.CurrentJson, false))));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19FNoChanges, () => OpenExport(ExportPublishedDraft(), null, null)));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19GFirstPublish,
                () => OpenExport(LiveOpsDesignSample.DocumentWithoutPublishedStamp(), null, null)));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19HParserFailed, () => OpenExport(ExportFixedDraft(),
                new RewritingLiveOpsHubJsonReadBack(text => text.Substring(0, text.Length - 1)), ReviewEverythingRequired)));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19IRestoring, OpenExportRestoring));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19JFormat1, () => OpenExport(ExportFixedDraft(), null, section =>
            {
                ReviewEverythingRequired(section);
                section.Services.Session.Publish.SelectedFormat = LiveEventCalendarJsonFormat.Version1;
            })));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19KReviewRequired, () => OpenExport(ExportFixedDraft(), null, null)));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19ParserMismatch, () => OpenExport(LiveOpsDesignSample.Document,
                new RewritingLiveOpsHubJsonReadBack(text => text.Replace("\"2026-10-3\"", "\"2026-10-03T00:00:00Z\"")), null)));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19LCompareRemote, () => OpenExport(LiveOpsDesignSample.Document, null, section =>
            {
                section.Services.Session.Remote.Set(LiveOpsDesignSample.PublishedSnapshotJson, LiveOpsDesignSample.NowUtc);
                section.Services.Session.Publish.SelectCompareSource(LiveOpsHubCompareSource.Remote);
            })));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H21ExportJsonTop, () => OpenExport(LiveOpsDesignSample.Document, null, null)));

            // Outcome nằm trong KHUNG cửa sổ (G-HOSTUI), nên hai kịch bản này chạy việc thật SAU khi cửa sổ đã mở.
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H07dOutcomeCopied, OpenExportAfterCopy));
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H07eOutcomeMarked, OpenExportAfterMarkPublished));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H08dConfirmRestorePublished,
                    (int)LiveOpsConfirmWindow.Width, (int)LiveOpsConfirmWindow.Level1Height,
                    () => OpenConfirm(ExportRestoreConfirmRequest(), string.Empty),
                    window => ((LiveOpsConfirmWindow)window).Content)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsConfirmContent.RootElementName, LiveOpsConfirmWindow.Width,
                    LiveOpsConfirmWindow.Level1Height)));

            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H08gConfirmRemoveStamp,
                    (int)LiveOpsConfirmWindow.Width, (int)LiveOpsConfirmWindow.Level1Height,
                    () => OpenConfirm(ExportRemoveStampConfirmRequest(), string.Empty),
                    window => ((LiveOpsConfirmWindow)window).Content)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(LiveOpsConfirmContent.RootElementName, LiveOpsConfirmWindow.Width,
                    LiveOpsConfirmWindow.Level1Height)));

            scenarios.Add(MarkPublished(LiveOpsHubCaptureScenarioIds.H20aMarkPublishedReady,
                () => MarkPublishedSampleInput(ExportDraftSha).WithUserInput(true, ExportMarkNote)));
            scenarios.Add(MarkPublished(LiveOpsHubCaptureScenarioIds.H20bMarkPublishedMissing,
                () => MarkPublishedSampleInput(ExportDraftSha)));
            scenarios.Add(MarkPublished(LiveOpsHubCaptureScenarioIds.H20cMarkPublishedDraftChanged,
                () => MarkPublishedSampleInput(ExportCopiedSha)));
        }

        private static LiveOpsHubCaptureScenario Section(string id, Func<EditorWindow> openWindow)
        {
            return new LiveOpsHubCaptureScenario(id, StandardWidth, StandardHeight, openWindow);
        }

        private static LiveOpsHubCaptureScenario MarkPublished(string id, Func<MarkPublishedInput> buildInput)
        {
            return new LiveOpsHubCaptureScenario(id, (int)MarkPublishedWindow.Width, (int)MarkPublishedWindow.Height,
                    () => MarkPublishedWindow.OpenForTest(buildInput(), null),
                    window => ((MarkPublishedWindow)window).Content)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(MarkPublishedContent.RootElementName, MarkPublishedWindow.Width,
                    MarkPublishedWindow.Height));
        }

        private static MarkPublishedInput MarkPublishedSampleInput(string lastExportedSha256Hex)
        {
            return new MarkPublishedInput(ExportDraftSha, lastExportedSha256Hex, ExportCopiedUtc, ExportMarkNowUtc, ExportSampleByteCount,
                ExportSampleAssetFileName, LiveOpsHubTestServices.PublisherName, LiveOpsHubTestServices.PublisherSource,
                new LiveOpsHubFormat(LiveOpsDesignSample.DeviceOffset));
        }

        /// <summary>Hộp 4 của Hình 8 — khôi phục bản đã đăng vào nháp, chữ nguyên văn [SD2 §3.9].</summary>
        internal static LiveOpsConfirmRequest ExportRestoreConfirmRequest()
        {
            return new LiveOpsConfirmRequest.Builder()
                .WithTitle("Khôi phục bản 11/9 16:20 vào nháp?")
                .WithBody("Nháp mất 5 thay đổi: hunt-0916-bonus, lava-quest-2026-09b, lava-quest-2026-10, hunt-0914, weekly-pass.")
                .WithKeyHint("Enter / Esc: Giữ nháp")
                .WithButtons("Khôi phục", "Giữ nháp")
                .Build();
        }

        /// <summary>Hộp (S-19) — gỡ dấu đã đăng mới nhất, chữ nguyên văn [SD2 §3.9].</summary>
        internal static LiveOpsConfirmRequest ExportRemoveStampConfirmRequest()
        {
            return new LiveOpsConfirmRequest.Builder()
                .WithTitle("Gỡ dấu đã đăng 13/9 09:04?")
                .WithBody("Bản so quay về 11/9 16:20 · sha 3f9a1c; Xuất JSON sẽ tính lại 4 thay đổi.")
                .WithButtons("Gỡ dấu", "Giữ dấu")
                .Build();
        }

        private static EditorWindow OpenExportAfterCopy()
        {
            return OpenExportThen(ExportFixedDraft(), null, ReviewEverythingRequired, section => section.CopyJson());
        }

        private static EditorWindow OpenExportAfterMarkPublished()
        {
            return OpenExportThen(ExportFixedDraft(), null, section =>
            {
                ReviewEverythingRequired(section);
                section.Services.Session.Publish.RecordExport(section.Services.Session.Publish.CurrentJson, false);
            }, section => section.MarkPublished(ExportMarkNote));
        }

        private static EditorWindow OpenExportWithStaleCheck()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.StaleCheckScenario);
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            return LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Export);
        }

        /// <summary>Trạng thái (i): đi đúng luồng "Khôi phục vào nháp…" của card lịch sử, hộp xác nhận trả Destructive.</summary>
        private static EditorWindow OpenExportRestoring()
        {
            return OpenExport(LiveOpsDesignSample.Document, null, section =>
            {
                section.HistoryCard.RequestRestore(section.Services.Session.Document.LatestStamp);
            }, new ScriptedLiveOpsHubConfirmationPresenter(LiveOpsConfirmResult.Destructive));
        }

        private static void ReviewEverythingRequired(ExportSection section)
        {
            foreach (LiveEventCalendarChange change in section.Services.Session.Publish.PublishedDiff.Changes)
            {
                if (change.IsReviewRequired) section.Services.Session.Publish.SetReviewed(change, true);
            }
        }

        private static EditorWindow OpenExport(LiveEventCalendarDocument document, ILiveOpsHubJsonReadBack readBack,
            Action<ExportSection> prepare, ILiveOpsHubConfirmationPresenter confirmation = null)
        {
            return OpenExportThen(document, readBack, prepare, null, confirmation);
        }

        /// <param name="afterShow">Việc chạy SAU khi cửa sổ mở — chỉ dành cho outcome, vì outcome nằm trong khung cửa sổ.</param>
        private static EditorWindow OpenExportThen(LiveEventCalendarDocument document, ILiveOpsHubJsonReadBack readBack,
            Action<ExportSection> prepare, Action<ExportSection> afterShow, ILiveOpsHubConfirmationPresenter confirmation = null)
        {
            LiveOpsHubServicesBuilder builder = LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document));
            if (readBack != null) builder.WithJsonReadBack(readBack);
            if (confirmation != null) builder.WithConfirmation(confirmation);
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(builder);
            services.Session.RunCheckToCompletion();

            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            ExportSection export = FindExportSection(sections);
            if (prepare != null && export != null) prepare(export);
            EditorWindow window = LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Export);
            if (afterShow != null && export != null) afterShow(export);
            return window;
        }

        private static ExportSection FindExportSection(List<IHubSection> sections)
        {
            for (int index = 0; index < sections.Count; index++)
            {
                ExportSection export = sections[index] as ExportSection;
                if (export != null) return export;
            }
            return null;
        }

        /// <summary>Mẫu thiết kế sau khi sửa cả hai đợt bị bỏ — nền của mọi kịch bản "đã qua cổng".</summary>
        private static LiveEventCalendarDocument ExportFixedDraft()
        {
            LiveEventCalendarDocument sample = LiveOpsDesignSample.Document;
            LiveEventCalendarDocumentBuilder builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(sample.RemoteConfigKey);
            foreach (LiveEventTypeDefinition type in sample.EventTypes) builder.WithEventType(type);
            foreach (RecurringLiveEventRule rule in sample.RecurringRules) builder.WithRecurringRule(rule);
            foreach (FixedLiveEventEntry entry in sample.FixedEvents) builder.WithFixedEvent(ExportFixedEntry(entry));
            foreach (PublishedCalendarStamp stamp in sample.PublishedStamps) builder.WithPublishedStamp(stamp);
            return builder.Build();
        }

        private static FixedLiveEventEntry ExportFixedEntry(FixedLiveEventEntry entry)
        {
            if (entry.EntryKey == LiveOpsDesignSample.HuntBonusEntryKey)
            {
                return new FixedLiveEventEntry(entry.EntryKey, entry.EventId, entry.EventType, "2026-09-17T00:00:00Z",
                    "2026-09-18T00:00:00Z", entry.ConfigKey);
            }
            if (entry.EntryKey == LiveOpsDesignSample.LavaQuestLateEntryKey)
            {
                return new FixedLiveEventEntry(entry.EntryKey, entry.EventId, entry.EventType, entry.StartUtcText,
                    "2026-10-03T00:00:00Z", entry.ConfigKey);
            }
            return entry;
        }

        /// <summary>Nháp trùng ĐÚNG bản chụp của dấu mới nhất — nền của (e) và (f).</summary>
        private static LiveEventCalendarDocument ExportPublishedDraft()
        {
            LiveEventCalendarDocument fixedDraft = ExportFixedDraft();
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(fixedDraft, LiveEventCalendarJsonFormat.Version2);
            LiveEventCalendarDocumentBuilder builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(fixedDraft.RemoteConfigKey);
            foreach (LiveEventTypeDefinition type in fixedDraft.EventTypes) builder.WithEventType(type);
            foreach (RecurringLiveEventRule rule in fixedDraft.RecurringRules) builder.WithRecurringRule(rule);
            foreach (FixedLiveEventEntry entry in fixedDraft.FixedEvents) builder.WithFixedEvent(entry);
            builder.WithPublishedStamp(new PublishedCalendarStamp("2026-09-13T09:04:00Z", LiveOpsHubTestServices.PublisherName,
                json.Sha256Hex, json.ByteCount, 2, LiveOpsDesignSample.PublishedNote, json.Text));
            return builder.Build();
        }
    }
}
