using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEngine.UIElements;

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

        /// <summary>Bề rộng card "JSON sẽ đăng" [SD2 §3.3] — <c>flex-basis 520px</c>, không co.</summary>
        private const float JsonCardWidth = 520f;

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
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19CprimeSavedFile, OpenExportAfterSaveFile));
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
            scenarios.Add(Section(LiveOpsHubCaptureScenarioIds.H19LCompareRemote, OpenExportCompareRemote));
            // Hình 21 là KHUNG "JSON sẽ đăng" từ dòng 1 (9.5: "JSON viewer Light, dòng 1–24, cao 385px"), không phải cả cửa sổ:
            // chụp cả cửa sổ với cùng dữ liệu cho ra ảnh TRÙNG BYTE với h18, tức Hình 21 không được chụp.
            scenarios.Add(new LiveOpsHubCaptureScenario(LiveOpsHubCaptureScenarioIds.H21ExportJsonTop, StandardWidth, StandardHeight,
                    () => OpenExport(LiveOpsDesignSample.Document, null, null),
                    window => window.rootVisualElement.Q(ExportSection.JsonCardElementName))
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(ExportSection.JsonCardElementName, JsonCardWidth, 0f)));

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
            // Biến thể PHẢI có nút "Copy JSON <sha nháp>" — nút chỉ hiện khi hộp có đường copy thật, nên kịch bản phải truyền
            // callback (hộp không tự copy: clipboard + lastExportedSha là việc của phiên).
            scenarios.Add(MarkPublished(LiveOpsHubCaptureScenarioIds.H20cMarkPublishedDraftChanged,
                () => MarkPublishedSampleInput(ExportCopiedSha), MarkPublishedCopyStub));
        }

        private static LiveOpsHubCaptureScenario Section(string id, Func<EditorWindow> openWindow)
        {
            return new LiveOpsHubCaptureScenario(id, StandardWidth, StandardHeight, openWindow);
        }

        private static LiveOpsHubCaptureScenario MarkPublished(string id, Func<MarkPublishedInput> buildInput,
            Func<MarkPublishedInput> copyCurrentJson = null)
        {
            return new LiveOpsHubCaptureScenario(id, (int)MarkPublishedWindow.Width, (int)MarkPublishedWindow.Height,
                    () => MarkPublishedWindow.OpenForTest(buildInput(), copyCurrentJson),
                    window => ((MarkPublishedWindow)window).Content)
                .WithExpectedFrames(new LiveOpsHubCaptureExpectedFrame(MarkPublishedContent.RootElementName, MarkPublishedWindow.Width,
                    MarkPublishedWindow.Height));
        }

        /// <summary>Đường copy giả của hộp: kịch bản chụp chỉ cần nút HIỆN, không bấm — trả lại đúng input đang vẽ.</summary>
        private static MarkPublishedInput MarkPublishedCopyStub()
        {
            return MarkPublishedSampleInput(ExportDraftSha).WithCopiedInsideDialog(ExportDraftSha, ExportMarkNowUtc);
        }

        private static MarkPublishedInput MarkPublishedSampleInput(string lastExportedSha256Hex)
        {
            return new MarkPublishedInput(ExportDraftSha, lastExportedSha256Hex, ExportCopiedUtc, ExportMarkNowUtc, ExportSampleByteCount,
                ExportSampleAssetFileName, LiveOpsHubTestServices.PublisherName, LiveOpsHubTestServices.PublisherSource,
                new LiveOpsHubFormat(LiveOpsDesignSample.DeviceOffset));
        }

        /// <summary>
        /// Hộp 4 của Hình 8 — "Khôi phục vào nháp…" [SD2 §3.9]. Request dựng qua LỐI VÀO THẬT của card lịch sử
        /// (<see cref="ExportHistoryCard.RequestRestore"/>): ảnh phải chứng minh câu mà SẢN PHẨM ghép từ catalog + danh sách mục
        /// đổi, không phải một chuỗi tiếng Việt viết cứng trong file kịch bản.
        /// </summary>
        internal static LiveOpsConfirmRequest ExportRestoreConfirmRequest()
        {
            return CaptureConfirmRequest(LiveOpsDesignSample.Document,
                section => section.HistoryCard.RequestRestore(section.Services.Session.Document.LatestStamp));
        }

        /// <summary>Hộp (S-19) — "Gỡ dấu này…" của lần đăng mới nhất, cũng đi qua lối vào thật của card lịch sử [SD2 §3.9].</summary>
        internal static LiveOpsConfirmRequest ExportRemoveStampConfirmRequest()
        {
            return CaptureConfirmRequest(ExportTwoStampDraft(),
                section => section.HistoryCard.RequestRemoveStamp(section.Services.Session.Document.LatestStamp));
        }

        /// <summary>
        /// Chạy một thao tác hỏi-xác-nhận của màn với presenter ghi lại request rồi trả "Giữ nháp"/"Giữ dấu" — không cần mở cửa
        /// sổ hub, và KHÔNG áp thay đổi phá huỷ nào.
        /// </summary>
        private static LiveOpsConfirmRequest CaptureConfirmRequest(LiveEventCalendarDocument document, Action<ExportSection> invoke)
        {
            ScriptedLiveOpsHubConfirmationPresenter presenter = new ScriptedLiveOpsHubConfirmationPresenter(LiveOpsConfirmResult.Safe);
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document))
                .WithConfirmation(presenter));
            services.Session.RunCheckToCompletion();
            ExportSection export = FindExportSection(LiveOpsHubSections.Create(services));
            if (export != null) invoke(export);
            if (presenter.Requests.Count == 0) throw new InvalidOperationException("thao tác không hỏi xác nhận — kịch bản chụp sai lối vào");
            return presenter.Requests[0];
        }

        private static EditorWindow OpenExportAfterCopy()
        {
            return OpenExportThen(LiveOpsHubTestServices.CreateMemoryAsset(ExportFixedDraft()), null, ReviewEverythingRequired,
                section => section.CopyJson());
        }

        /// <summary>
        /// (e) Ghi dấu đã đăng THẬT. Asset phải có FILE trên đĩa: <c>LiveOpsHubPublishState.MarkPublished</c> lưu asset sau khi
        /// áp dấu, và asset chỉ-trong-bộ-nhớ trả <c>FailureAfterApply</c> — outcome khi đó là hộp đỏ "không lưu được", không
        /// phải hình (e). Asset test bị xoá ngay khi cửa sổ đóng để lượt chụp không để lại file trong worktree.
        /// </summary>
        private static EditorWindow OpenExportAfterMarkPublished()
        {
            LiveOpsHubTestServices.ReleaseAll();
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(ExportSampleAssetFileName, ExportFixedDraft());
            EditorWindow window = OpenExportThen(asset, null, section =>
            {
                ReviewEverythingRequired(section);
                section.Services.Session.Publish.RecordExport(section.Services.Session.Publish.CurrentJson, false);
            }, section => section.MarkPublished(ExportMarkNote));
            window.rootVisualElement.RegisterCallback<DetachFromPanelEvent>(detach => LiveOpsHubTestServices.ReleaseAll());
            return window;
        }

        /// <summary>
        /// (c′) Vừa lưu file: đi ĐÚNG đường "Lưu file…" của màn (hộp lưu giả trả đường dẫn tạm) nên outcome, <c>lastExportedSha</c>
        /// và bản ghi mang đường dẫn file đều do sản phẩm sinh ra. File tạm xoá ngay sau khi ghi — outcome chỉ cần đường dẫn.
        /// </summary>
        private static EditorWindow OpenExportAfterSaveFile()
        {
            ManualLiveOpsHubFileDialog fileDialog = new ManualLiveOpsHubFileDialog();
            return OpenExportThen(LiveOpsHubTestServices.CreateMemoryAsset(ExportFixedDraft()), null, ReviewEverythingRequired, section =>
            {
                // Tên file là tên MẶC ĐỊNH của sản phẩm + đuôi mà hộp lưu của hệ thống tự thêm (liveops_calendar-<sha>.json);
                // chỉ thư mục là chỗ tạm của lượt chụp.
                string savedPath = Path.Combine(Path.GetTempPath(), string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubPaths.ExportFileNameFormat, section.Services.Session.Publish.CurrentJson.ShortSha)
                    + "." + LiveOpsHubPaths.ExportFileExtension);
                fileDialog.PathToReturn = savedPath;
                section.SaveJsonToFile();
                if (File.Exists(savedPath)) File.Delete(savedPath);
            }, null, fileDialog);
        }

        /// <summary>
        /// (V-13) Card diff so với bản remote đã dán. Dán bản remote làm lần kiểm thành cũ, và <c>OnShown</c> tự kiểm lại — chụp
        /// ngay lúc đó ra spinner "Đang kiểm lại 4/12 luật…" (số phụ thuộc số khung harness chờ), không phải trạng thái V-13.
        /// Chạy kiểm tới hết SAU khi cửa sổ mở rồi mới để lệnh chụp chờ layout.
        /// </summary>
        private static EditorWindow OpenExportCompareRemote()
        {
            return OpenExportThen(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document), null, section =>
            {
                section.Services.Session.Remote.Set(LiveOpsDesignSample.PublishedSnapshotJson, LiveOpsDesignSample.NowUtc);
                section.Services.Session.Publish.SelectCompareSource(LiveOpsHubCompareSource.Remote);
            }, section =>
            {
                section.Services.Session.RunCheckToCompletion();
                section.Refresh();
            });
        }

        private static EditorWindow OpenExportWithStaleCheck()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.StaleCheckScenario);
            List<IHubSection> sections = LiveOpsHubSections.Create(services);
            return LiveOpsHubWindow.OpenWithServices(services, sections, LiveOpsHubSections.Ids.Export);
        }

        /// <summary>
        /// Trạng thái (i): đi đúng luồng "Khôi phục vào nháp…" của card lịch sử, hộp xác nhận trả Destructive. Chạy SAU khi cửa
        /// sổ dựng — khung gọi <c>RestoreViewState</c> ngay sau <c>CreateView</c>, nên trạng thái đặt trước đó dễ bị một bản
        /// trạng thái view cũ/rỗng ghi đè.
        /// </summary>
        private static EditorWindow OpenExportRestoring()
        {
            return OpenExportThen(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document), null, null,
                section => section.HistoryCard.RequestRestore(section.Services.Session.Document.LatestStamp),
                new ScriptedLiveOpsHubConfirmationPresenter(LiveOpsConfirmResult.Destructive));
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
            return OpenExportThen(LiveOpsHubTestServices.CreateMemoryAsset(document), readBack, prepare, null, confirmation);
        }

        /// <param name="afterShow">
        /// Việc chạy SAU khi cửa sổ mở: outcome nằm trong khung cửa sổ, và trạng thái view của màn (trạng thái (i)) chỉ sống
        /// được sau khi khung đã gọi <c>RestoreViewState</c>.
        /// </param>
        /// <param name="fileDialog">Hộp lưu file giả — kịch bản (c′) đi đường "Lưu file…" thật.</param>
        private static EditorWindow OpenExportThen(LiveEventCalendarAsset asset, ILiveOpsHubJsonReadBack readBack,
            Action<ExportSection> prepare, Action<ExportSection> afterShow, ILiveOpsHubConfirmationPresenter confirmation = null,
            ILiveOpsHubFileDialog fileDialog = null)
        {
            LiveOpsHubServicesBuilder builder = LiveOpsHubTestServices.CreateBuilder(null).WithCalendarAsset(asset);
            if (readBack != null) builder.WithJsonReadBack(readBack);
            if (confirmation != null) builder.WithConfirmation(confirmation);
            if (fileDialog != null) builder.WithFileDialog(fileDialog);
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

        /// <summary>
        /// Hai dấu đã đăng (11/9 16:20 · sha 3f9a1c, rồi 13/9 09:04) trên nháp đã sửa — nền của hộp "Gỡ dấu này…": thân hộp nêu
        /// dấu TRƯỚC làm bản so mới, nên phải có một dấu trước để nêu.
        /// </summary>
        private static LiveEventCalendarDocument ExportTwoStampDraft()
        {
            LiveEventCalendarDocument fixedDraft = ExportFixedDraft();
            LiveEventCalendarJsonText designJson = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document,
                LiveEventCalendarJsonFormat.Version2);
            LiveEventCalendarDocumentBuilder builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(fixedDraft.RemoteConfigKey);
            foreach (LiveEventTypeDefinition type in fixedDraft.EventTypes) builder.WithEventType(type);
            foreach (RecurringLiveEventRule rule in fixedDraft.RecurringRules) builder.WithRecurringRule(rule);
            foreach (FixedLiveEventEntry entry in fixedDraft.FixedEvents) builder.WithFixedEvent(entry);
            foreach (PublishedCalendarStamp stamp in fixedDraft.PublishedStamps) builder.WithPublishedStamp(stamp);
            builder.WithPublishedStamp(new PublishedCalendarStamp("2026-09-13T09:04:00Z", LiveOpsHubTestServices.PublisherName,
                designJson.Sha256Hex, designJson.ByteCount, 2, LiveOpsDesignSample.PublishedNote, designJson.Text));
            return builder.Build();
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
