using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Màn Xuất JSON dựng trong cửa sổ hub thật (mục 7.6, [SD2 §3]). Trọng tâm là đường đi tiền ra: Copy đưa ĐÚNG từng byte
    /// của chuỗi sẽ dán lên Firebase và ghi lại sha, Lưu file ghi đúng byte đó xuống đĩa, và mọi nhánh parser đọc lại (h /
    /// lệch) khoá ba nút bằng lý do in thành chữ.
    /// <para>
    /// Parser đọc lại đi qua port <c>ILiveOpsHubJsonReadBack</c> (V-16) nên hai kịch bản khó dựng — parser hỏng và parser
    /// lệch — dựng được bằng <see cref="RewritingLiveOpsHubJsonReadBack"/> mà không phải làm hỏng dữ liệu lịch.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class ExportSectionTests
    {
        private const string SavedFileName = "liveops_calendar-test.json";

        /// <summary>Vòng chờ của test UI (V-23): chỉ fail khi quá CẢ 60 khung LẪN 5 giây.</summary>
        private const int MaximumWaitFrames = 60;

        private const double MaximumWaitSeconds = 5d;

        private SectionTestScope _scope;
        private string _savedFilePath = string.Empty;

        private ExportSection Section => (ExportSection)_scope.Section;

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            if (_scope != null) _scope.Dispose();
            _scope = null;
            if (_savedFilePath.Length > 0 && File.Exists(_savedFilePath)) File.Delete(_savedFilePath);
            _savedFilePath = string.Empty;
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        [UnityTest]
        public IEnumerator Section_RequiredElementsPresent()
        {
            yield return OpenDesignSample(null, null);

            _scope.AssertRequiredElementsPresent();
            Assert.IsNotNull(_scope.View.Q(ExportGateCard.ElementName), "card Cổng xuất nằm trong thân màn");
            Assert.IsNotNull(_scope.View.Q(ExportDiffCard.ElementName));
            Assert.IsNotNull(_scope.View.Q(ExportHistoryCard.ElementName));
            Assert.AreEqual(1, Section.HistoryCard.RowCount, "mẫu thiết kế có đúng một dấu đã đăng");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Card cổng vẽ được MỌI mã trạng thái (a)–(k) + parser lệch — card chỉ đọc state, không tự suy luận.</summary>
        [Test]
        public void GateCard_RendersEveryGateState()
        {
            ExportGateCard card = new ExportGateCard();
            HashSet<ExportGateStatusCode> rendered = new HashSet<ExportGateStatusCode>();

            foreach (ExportGateState state in EveryGateState())
            {
                card.Bind(state);
                foreach (ExportGateStatusCode code in state.StatusCodes) rendered.Add(code);
                if (state.IsCompact)
                {
                    Assert.IsFalse(card.CompactRow.ClassListContains(LiveOpsHubClassNames.ExportHidden), "dạng gọn hiện một dòng");
                    Assert.AreEqual(state.CompactText, card.CompactText.text);
                }
                else
                {
                    Assert.Greater(card.VisibleRowCount, 0, "mã " + ExportGateModel.LetterOf(state.StatusCode) + " phải vẽ ra dòng cổng");
                }
            }

            foreach (ExportGateStatusCode code in (ExportGateStatusCode[])Enum.GetValues(typeof(ExportGateStatusCode)))
            {
                Assert.IsTrue(rendered.Contains(code), "chưa vẽ mã trạng thái " + ExportGateModel.LetterOf(code));
            }
        }

        [UnityTest]
        public IEnumerator Copy_ClipboardExactAndRecordsSha()
        {
            InMemoryLiveOpsHubClipboard clipboard = new InMemoryLiveOpsHubClipboard();
            yield return OpenFixedDraft(clipboard, null, null);

            LiveEventCalendarJsonText expected = Section.Services.Session.Publish.CurrentJson;
            yield return ClickHeaderButton(ExportSection.CopyButtonElementName);

            Assert.AreEqual(expected.Text, clipboard.Text, "clipboard nhận ĐÚNG chuỗi sẽ dán lên Firebase, không thêm bớt ký tự nào");
            Assert.AreEqual(expected.Sha256Hex, Section.Services.Session.Publish.LastExportedSha256Hex,
                "copy ghi lastExportedSha để nút Đánh dấu đã đăng mở khoá");
            Assert.AreEqual(ExportGateStatusCode.Exported, Section.Gate.StatusCode, "sau khi copy là trạng thái (c)");
            Assert.IsTrue(Section.MarkPublishedSlot.Button.enabledSelf, "ghi sổ chỉ cần đã copy đúng bản này");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SaveFile_WritesExactBytes()
        {
            _savedFilePath = Path.Combine(Path.GetTempPath(), SavedFileName);
            if (File.Exists(_savedFilePath)) File.Delete(_savedFilePath);
            ManualLiveOpsHubFileDialog fileDialog = new ManualLiveOpsHubFileDialog(_savedFilePath);
            yield return OpenFixedDraft(null, fileDialog, null);

            LiveEventCalendarJsonText expected = Section.Services.Session.Publish.CurrentJson;
            yield return ClickHeaderButton(ExportSection.SaveFileButtonElementName);

            Assert.IsTrue(File.Exists(_savedFilePath), "hộp lưu trả đường dẫn thì màn phải ghi file");
            Assert.AreEqual(expected.GetUtf8Bytes(), File.ReadAllBytes(_savedFilePath),
                "file chứa đúng byte UTF-8 của chuỗi xuất — không BOM, không đổi ký tự xuống dòng");
            Assert.AreEqual(expected.Sha256Hex, Section.Services.Session.Publish.LastExportedSha256Hex, "lưu file cũng ghi lastExportedSha");
            Assert.AreEqual(1, fileDialog.SaveFileCalls.Count);
            Assert.IsTrue(fileDialog.SaveFileCalls[0].DefaultName.Contains(expected.ShortSha), "tên file mặc định mang sha để đối chiếu dấu");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Format1_HelpBoxAndKeeps4Of6()
        {
            yield return OpenDesignSample(null, null);

            Section.Services.Session.Publish.SelectedFormat = LiveEventCalendarJsonFormat.Version1;
            Section.Refresh();
            yield return null;

            Assert.IsTrue(Section.Gate.HasStatus(ExportGateStatusCode.Format1), "chọn định dạng 1 là trạng thái kèm (j)");
            Assert.IsNotNull(_scope.View.Q(ExportSection.Format1NoticeElementName), "định dạng 1 phải có HelpBox cảnh báo");
            Assert.IsTrue(Section.Gate.Format1NoticeText.Contains("recurring"),
                "câu cảnh báo nói rõ luật lặp sẽ không được xuất — đó là thứ người dùng mất");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RemoveStamp_RequestTitleAndButtons()
        {
            ScriptedLiveOpsHubConfirmationPresenter presenter = new ScriptedLiveOpsHubConfirmationPresenter(LiveOpsConfirmResult.Safe);
            yield return OpenDesignSample(null, presenter);

            PublishedCalendarStamp latest = Section.Services.Session.Document.LatestStamp;
            // Menu chuột phải không bật được trong batchmode: gọi đúng lối vào mà mục menu "Gỡ dấu này…" gọi.
            Section.HistoryCard.RequestRemoveStamp(latest);

            Assert.AreEqual(1, presenter.Requests.Count, "gỡ dấu luôn hỏi cấp 1");
            LiveOpsConfirmRequest request = presenter.Requests[0];
            Assert.AreEqual(LiveOpsConfirmLevel.Level1, request.Level);
            Assert.IsTrue(request.Title.Contains("11/9"), "tiêu đề nêu giờ dấu sắp gỡ");
            Assert.AreEqual(LiveOpsHubStrings.ExportRemoveStampConfirmDestructive, request.DestructiveLabel);
            Assert.AreEqual(LiveOpsHubStrings.ExportRemoveStampConfirmSafe, request.SafeLabel);
            Assert.AreEqual(1, Section.Services.Session.Document.PublishedStamps.Count, "chọn 'Giữ dấu' thì dấu còn nguyên");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>(V-13) Nguồn bản so Remote: header đổi chữ và KHÔNG hàng nào có Toggle "Đã xem".</summary>
        [UnityTest]
        public IEnumerator CompareSourceRemote_HeaderAndNoReviewToggles()
        {
            yield return OpenDesignSample(null, null);

            LiveOpsHubCalendarSession session = Section.Services.Session;
            session.Remote.Set(LiveOpsDesignSample.PublishedSnapshotJson, LiveOpsDesignSample.NowUtc);
            Section.ApplyNavigation(LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Export)
                .WithCompareSource(LiveOpsHubCompareSource.Remote));
            yield return null;

            Assert.AreEqual(LiveOpsHubCompareSource.Remote, session.Publish.ActiveCompareSource);
            Assert.IsFalse(Section.DiffModel.HasReviewToggles, "'Đã xem' luôn so với dấu đã đăng");
            Assert.AreEqual(0, _scope.View.Query<Toggle>().ToList().Count, "card diff không vẽ Toggle nào khi so với bản remote");
            Assert.IsTrue(Section.DiffCard.TitleLabel.text.StartsWith(RemoteHeaderPrefix(), StringComparison.Ordinal),
                "header nói rõ đang so với bản remote đã dán");
            Assert.IsFalse(Section.DiffCard.BackToPublishedChip.ClassListContains(LiveOpsHubClassNames.ExportHidden),
                "chip 'Về bản đã đăng' là đường quay lại");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>(V-16) Parser của game không đọc lại được JSON do hub tạo → card lỗi (h) và ba nút khoá.</summary>
        [UnityTest]
        public IEnumerator ParserFailed_ViaRewritingReadBack_StateH()
        {
            yield return OpenDesignSample(null, null, new RewritingLiveOpsHubJsonReadBack(text => text.Substring(0, text.Length - 1)));

            Assert.AreEqual(ExportGateStatusCode.ReadBackFailed, Section.Gate.StatusCode);
            Assert.IsNotNull(_scope.View.Q(ExportSection.FailureCardElementName), "trạng thái (h) có card lỗi đầu thân");
            Assert.IsFalse(Section.CopySlot.Button.enabledSelf, "không đọc lại được thì không ai được dán JSON này lên Firebase");
            Assert.IsFalse(Section.SaveFileSlot.Button.enabledSelf);
            Assert.IsFalse(Section.MarkPublishedSlot.Button.enabledSelf);
            Assert.Greater(Section.CopySlot.Reason.Length, 0, "lý do khoá in thành chữ cạnh nút (SP-3)");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>(V-16, V-6) Parser đọc được nhưng giữ khác dãy mục của Kiểm lịch → lỗi của hub, dòng cổng 2 Blocked.</summary>
        [UnityTest]
        public IEnumerator ParserMismatch_ViaRewritingReadBack()
        {
            yield return OpenDesignSample(null, null,
                new RewritingLiveOpsHubJsonReadBack(text => text.Replace("\"2026-10-3\"", "\"2026-10-03T00:00:00Z\"")));

            Assert.AreEqual(ExportGateStatusCode.ReadBackMismatch, Section.Gate.StatusCode);
            ExportGateRow parser = Section.Gate.Row(ExportGateRowKind.ParserReadBack);
            Assert.AreEqual(ExportGateRowState.Blocked, parser.State);
            Assert.IsFalse(Section.CopySlot.Button.enabledSelf, "hub không mời dán JSON mà chính hub đọc ra kết quả khác");
            Assert.IsNull(_scope.View.Q(ExportSection.FailureCardElementName), "lệch không phải (h): parser vẫn đọc được");
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------------------------------------------- dựng

        private static string RemoteHeaderPrefix()
        {
            int placeholder = LiveOpsHubStrings.ExportDiffCardTitleRemoteFormat.IndexOf('{');
            return placeholder <= 0
                ? LiveOpsHubStrings.ExportDiffCardTitleRemoteFormat
                : LiveOpsHubStrings.ExportDiffCardTitleRemoteFormat.Substring(0, placeholder);
        }

        private IEnumerator ClickHeaderButton(string elementName)
        {
            Button button = _scope.Window.rootVisualElement.Q<Button>(elementName);
            Assert.IsNotNull(button, "section header thiếu nút '" + elementName + "'");
            yield return LiveOpsHubWindowTestScope.WaitForLayout(button);
            Assert.IsTrue(button.enabledSelf, "nút '" + elementName + "' phải mở trong kịch bản này");
            Vector2 center = button.worldBound.center;
            _scope.Window.SendEvent(new Event { type = EventType.MouseDown, mousePosition = center, button = 0, clickCount = 1 });
            _scope.Window.SendEvent(new Event { type = EventType.MouseUp, mousePosition = center, button = 0, clickCount = 1 });
            yield return null;
        }

        private IEnumerator OpenDesignSample(ILiveOpsHubClipboard clipboard, ILiveOpsHubConfirmationPresenter presenter,
            ILiveOpsHubJsonReadBack readBack = null)
        {
            yield return Open(LiveOpsDesignSample.Document, clipboard, null, presenter, readBack);
        }

        /// <summary>Nháp đã sửa hết đợt bị bỏ và đã xem mục bắt buộc — trạng thái (b), ba nút mở để thử Copy/Lưu file thật.</summary>
        private IEnumerator OpenFixedDraft(ILiveOpsHubClipboard clipboard, ILiveOpsHubFileDialog fileDialog,
            ILiveOpsHubConfirmationPresenter presenter)
        {
            yield return Open(FixedDraft(), clipboard, fileDialog, presenter, null);
            foreach (LiveEventCalendarChange change in Section.Services.Session.Publish.PublishedDiff.Changes)
            {
                if (change.IsReviewRequired) Section.Services.Session.Publish.SetReviewed(change, true);
            }
            Section.Refresh();
            yield return null;
            Assert.AreEqual(ExportGateStatusCode.Ready, Section.Gate.StatusCode, "nháp đã sửa hết và đã xem mục bắt buộc là trạng thái (b)");
        }

        private IEnumerator Open(LiveEventCalendarDocument document, ILiveOpsHubClipboard clipboard, ILiveOpsHubFileDialog fileDialog,
            ILiveOpsHubConfirmationPresenter presenter, ILiveOpsHubJsonReadBack readBack)
        {
            LiveOpsHubServicesBuilder builder = LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document));
            if (clipboard != null) builder.WithClipboard(clipboard);
            if (fileDialog != null) builder.WithFileDialog(fileDialog);
            if (presenter != null) builder.WithConfirmation(presenter);
            if (readBack != null) builder.WithJsonReadBack(readBack);
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(builder);
            services.Session.RunCheckToCompletion();
            _scope = SectionTestScope.Open(new ExportSection(services));
            yield return WaitForLayout();
        }

        private IEnumerator WaitForLayout()
        {
            yield return _scope.WaitForLayout();
        }

        /// <summary>Mẫu thiết kế sau khi sửa cả hai đợt bị bỏ — cùng nháp mà <c>ExportGateTests</c> dùng cho "giữ 8/8 mục".</summary>
        private static LiveEventCalendarDocument FixedDraft()
        {
            LiveEventCalendarDocument sample = LiveOpsDesignSample.Document;
            LiveEventCalendarDocumentBuilder builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(sample.RemoteConfigKey);
            foreach (LiveEventTypeDefinition type in sample.EventTypes) builder.WithEventType(type);
            foreach (RecurringLiveEventRule rule in sample.RecurringRules) builder.WithRecurringRule(rule);
            foreach (FixedLiveEventEntry entry in sample.FixedEvents)
            {
                FixedLiveEventEntry fixedEntry = entry;
                if (entry.EntryKey == LiveOpsDesignSample.HuntBonusEntryKey)
                {
                    fixedEntry = new FixedLiveEventEntry(entry.EntryKey, entry.EventId, entry.EventType, "2026-09-17T00:00:00Z",
                        "2026-09-18T00:00:00Z", entry.ConfigKey);
                }
                else if (entry.EntryKey == LiveOpsDesignSample.LavaQuestLateEntryKey)
                {
                    fixedEntry = new FixedLiveEventEntry(entry.EntryKey, entry.EventId, entry.EventType, entry.StartUtcText,
                        "2026-10-03T00:00:00Z", entry.ConfigKey);
                }
                builder.WithFixedEvent(fixedEntry);
            }
            foreach (PublishedCalendarStamp stamp in sample.PublishedStamps) builder.WithPublishedStamp(stamp);
            return builder.Build();
        }

        // ---------------------------------------------------------------------------- mọi mã trạng thái cho card cổng

        private static IEnumerable<ExportGateState> EveryGateState()
        {
            LiveEventCalendarDocument design = LiveOpsDesignSample.Document;
            LiveEventCalendarDocument fixedDraft = FixedDraft();
            LiveEventCalendarDocument published = PublishedDraft();

            yield return ExportGateModel.Evaluate(ReadyInput(design, 0));                                   // (a)
            yield return ExportGateModel.Evaluate(ReadyInput(fixedDraft, 0));                               // (k)
            yield return ExportGateModel.Evaluate(ReadyInput(fixedDraft, 1));                               // (b)
            yield return ExportGateModel.Evaluate(ExportedInput(fixedDraft));                               // (c)
            yield return ExportGateModel.Evaluate(StaleInput(design));                                      // (d)
            yield return ExportGateModel.Evaluate(ExportedInput(published));                                // (e)
            yield return ExportGateModel.Evaluate(ReadyInput(published, 0));                                // (f)
            yield return ExportGateModel.Evaluate(ReadyInput(LiveOpsDesignSample.DocumentWithoutPublishedStamp(), 0)); // (g)
            yield return ExportGateModel.Evaluate(FailedReadBackInput(fixedDraft));                         // (h)
            yield return ExportGateModel.Evaluate(ReadyInput(fixedDraft, 1)
                .WithRestoring(fixedDraft.LatestStamp, true));                                             // (i)
            yield return ExportGateModel.Evaluate(Format1Input(fixedDraft));                                // (j)
            yield return ExportGateModel.Evaluate(MismatchInput(design));                                   // parser lệch
        }

        /// <summary>Nháp trùng ĐÚNG bản chụp của dấu mới nhất — nền của (e) và (f).</summary>
        private static LiveEventCalendarDocument PublishedDraft()
        {
            LiveEventCalendarDocument fixedDraft = FixedDraft();
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(fixedDraft, LiveEventCalendarJsonFormat.Version2);
            LiveEventCalendarDocumentBuilder builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(fixedDraft.RemoteConfigKey);
            foreach (LiveEventTypeDefinition type in fixedDraft.EventTypes) builder.WithEventType(type);
            foreach (RecurringLiveEventRule rule in fixedDraft.RecurringRules) builder.WithRecurringRule(rule);
            foreach (FixedLiveEventEntry entry in fixedDraft.FixedEvents) builder.WithFixedEvent(entry);
            builder.WithPublishedStamp(new PublishedCalendarStamp("2026-09-13T09:04:00Z", LiveOpsHubTestServices.PublisherName,
                json.Sha256Hex, json.ByteCount, 2, LiveOpsDesignSample.PublishedNote, json.Text));
            return builder.Build();
        }

        private static ExportGateInput InputFor(LiveEventCalendarDocument draft, LiveEventCalendarJsonFormat jsonFormat,
            Func<string, string> rewriteBeforeRead)
        {
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(draft, jsonFormat);
            RewritingLiveOpsHubJsonReadBack readBackPort = new RewritingLiveOpsHubJsonReadBack(rewriteBeforeRead ?? (text => text));
            LiveEventCalendarDocumentParseResult readDocument = readBackPort.ReadBackDocument(json.Text);
            LiveEventCalendarParseResult readBack = readBackPort.ReadBack(json.Text);
            return new ExportGateInput(json, LiveEventCalendarCompiler.CompileInExportOrder(draft), readBack, readDocument.IsReadable,
                readDocument.ReadErrorText, new LiveOpsHubFormat(LiveOpsDesignSample.DeviceOffset));
        }

        private static ExportGateInput ReadyInput(LiveEventCalendarDocument draft, int reviewedRequiredCount)
        {
            return WithCheckAndStamp(InputFor(draft, LiveEventCalendarJsonFormat.Version2, null), draft, reviewedRequiredCount, false);
        }

        private static ExportGateInput ExportedInput(LiveEventCalendarDocument draft)
        {
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(draft, LiveEventCalendarJsonFormat.Version2);
            return ReadyInput(draft, 1).WithLastExportedSha256Hex(json.Sha256Hex);
        }

        private static ExportGateInput StaleInput(LiveEventCalendarDocument draft)
        {
            return WithCheckAndStamp(InputFor(draft, LiveEventCalendarJsonFormat.Version2, null), draft, 0, true);
        }

        private static ExportGateInput Format1Input(LiveEventCalendarDocument draft)
        {
            return WithCheckAndStamp(InputFor(draft, LiveEventCalendarJsonFormat.Version1, null), draft, 1, false);
        }

        private static ExportGateInput FailedReadBackInput(LiveEventCalendarDocument draft)
        {
            return WithCheckAndStamp(InputFor(draft, LiveEventCalendarJsonFormat.Version2, text => text.Substring(0, text.Length - 1)),
                draft, 1, false);
        }

        private static ExportGateInput MismatchInput(LiveEventCalendarDocument draft)
        {
            return WithCheckAndStamp(InputFor(draft, LiveEventCalendarJsonFormat.Version2,
                text => text.Replace("\"2026-10-3\"", "\"2026-10-03T00:00:00Z\"")), draft, 0, false);
        }

        private static ExportGateInput WithCheckAndStamp(ExportGateInput input, LiveEventCalendarDocument draft, int reviewedRequiredCount,
            bool isStale)
        {
            PublishedCalendarStamp stamp = draft.LatestStamp;
            LiveEventCalendarCheckContextBuilder contextBuilder = new LiveEventCalendarCheckContextBuilder(draft, LiveOpsDesignSample.NowUtc);
            if (stamp != null) contextBuilder.WithPublishedBaseline(BaselineOf(stamp));
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(contextBuilder.Build());
            ExportGateInput checkedInput = input.WithCheck(report, isStale, false, report.RuleResults.Count, report.RuleResults.Count,
                LiveOpsHubTestServices.CheckedAtUtc);
            if (stamp == null) return checkedInput;
            LiveEventCalendarDiffResult diff = LiveEventCalendarDiff.Compare(BaselineOf(stamp), draft, LiveOpsDesignSample.NowUtc);
            return checkedInput.WithPublished(stamp, diff, reviewedRequiredCount);
        }

        private static LiveEventCalendarDocument BaselineOf(PublishedCalendarStamp stamp)
        {
            return JsonLiveEventCalendarParser.ParseDocument(stamp.SnapshotJson).Document;
        }
    }
}
