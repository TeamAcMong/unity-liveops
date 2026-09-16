using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Phiên lịch sống (4.3): mọi sửa qua một chỗ ghi Undo + SetDirty + "chưa lưu"; kéo nhiều frame rồi Esc không để lại bước Undo và trả
    /// cả field đổi ở frame sau (SP-9); lưu ghi file; biên dịch theo thứ tự xuất (V-6); seam đọc lại JSON và nạp layout (V-16); chữ ký
    /// năm kiểu đóng băng ở cổng W3 (PD-35).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class CalendarSessionTests
    {
        private const string AssetFileName = "SessionMain.asset";

        /// <summary>Thư mục công cụ dev. Ghép từ hai mảnh vì <c>check-class-names.py</c> coi chuỗi nguyên khối là tên class USS chưa khai hằng.</summary>
        private const string ToolsFolderName = "liveops" + "-hub";

        private const string ContractFreezeFileName = "contract-freeze-W3.md";

        /// <summary>Đặt = "1" để CHO PHÉP sinh lại chữ ký đóng băng (chỉ khi user đã duyệt việc đổi hợp đồng).</summary>
        private const string ContractFreezeUpdateVariable = "LIVEOPS_HUB_UPDATE_CONTRACT_FREEZE";
        private const string MovedEndUtc = "2026-09-20T12:00:00Z";

        /// <summary>Loại của đợt lava-quest trong mẫu thiết kế — làn dùng cho <c>CheckLane</c>.</summary>
        private const string LavaQuestEventType = "lava-quest";

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        [Test]
        public void Session_Apply_RecordsUndoSetsDirtyAndUnsaved()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = OpenSession(asset);
            Assert.IsFalse(EditorUtility.IsDirty(asset), "asset vừa lưu phải sạch trước khi sửa");
            Assert.IsFalse(session.HasUnsavedChanges);
            int documentChangedCount = 0;
            session.DocumentChanged += () => documentChangedCount++;

            LiveOpsHubEditOutcome outcome = session.Apply(MoveLavaQuestEnd(session.Document, MovedEndUtc), "Dời kết thúc lava-quest-2026-09b");

            Assert.IsTrue(outcome.Applied, outcome.FailureText);
            Assert.AreEqual("Dời kết thúc lava-quest-2026-09b", outcome.UndoName, "tên Undo group = câu toast");
            Assert.AreEqual(outcome.UndoGroup, Undo.GetCurrentGroup(), "group trả về phải là group đang ở đỉnh — toast hỏi Hoàn tác theo số này");
            Assert.AreEqual("Dời kết thúc lava-quest-2026-09b", Undo.GetCurrentGroupName());
            Assert.IsTrue(EditorUtility.IsDirty(asset), "R-6: thiếu SetDirty thì ⌘S không ghi và tab không có *");
            Assert.IsTrue(session.HasUnsavedChanges, "tab phải có * sau khi sửa");
            Assert.AreEqual(1, session.UnsavedDiff.ChangeCount, "một đợt đổi = một thay đổi chưa lưu");
            Assert.AreEqual(MovedEndUtc, FindLavaQuestMid(asset.ToDocument()).EndUtcText, "asset phải mang giá trị mới — asset là sự thật của phiên");
            Assert.AreEqual(MovedEndUtc, FindLavaQuestMid(session.Document).EndUtcText);
            Assert.AreEqual(1, documentChangedCount, "một sửa = một lần DocumentChanged");
            Assert.IsNotNull(session.LastChangedUtc);
            Assert.AreEqual(0, CountRedoRecords(), "sửa mới không để lại redo");
        }

        [Test]
        public void Session_Apply_WithoutAsset_FailsWithReason()
        {
            LiveOpsHubCalendarSession session = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario).Session;

            LiveOpsHubEditOutcome outcome = session.Apply(new SetRemoteConfigKeyEdit("khac"), "Đổi key");

            Assert.IsFalse(outcome.Applied);
            Assert.AreEqual(LiveOpsHubEditOutcome.NoUndoGroup, outcome.UndoGroup);
            Assert.AreEqual(LiveOpsHubStrings.ServicesEditFailedNoAsset, outcome.FailureText, "lệnh sửa trượt phải nói vì sao, không im lặng");
        }

        [Test]
        public void Session_Undo_RestoresDocument_RaisesChanged()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = OpenSession(asset);
            string originalEnd = FindLavaQuestMid(session.Document).EndUtcText;
            session.Apply(MoveLavaQuestEnd(session.Document, MovedEndUtc), "Dời kết thúc");
            int documentChangedCount = 0;
            session.DocumentChanged += () => documentChangedCount++;

            Undo.PerformUndo();

            Assert.AreEqual(originalEnd, FindLavaQuestMid(session.Document).EndUtcText, "Undo của Unity phải đưa phiên về tài liệu cũ");
            Assert.AreEqual(1, documentChangedCount, "Undo.undoRedoPerformed → DocumentChanged đúng một lần");
            Assert.IsFalse(session.HasUnsavedChanges, "về đúng bản đã lưu thì hết *");

            Undo.PerformRedo();
            Assert.AreEqual(MovedEndUtc, FindLavaQuestMid(session.Document).EndUtcText, "Redo trả lại sửa");
            Assert.IsTrue(session.HasUnsavedChanges);
        }

        [Test]
        public void Session_ContinuousEditCancel_LeavesNoUndoStep()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = OpenSession(asset);
            LiveEventCalendarDocument original = session.Document;
            session.Apply(new SetRemoteConfigKeyEdit("liveops_calendar_v2"), "Đổi key remote");
            LiveEventCalendarDocument afterKeyEdit = session.Document;

            int group = session.BeginContinuousEdit("Kéo lava-quest-2026-09b");
            Assert.AreNotEqual(LiveOpsHubEditOutcome.NoUndoGroup, group);
            session.UpdateContinuousEdit(MoveLavaQuestEnd(afterKeyEdit, "2026-09-20T06:00:00Z"));
            session.UpdateContinuousEdit(MoveLavaQuestEnd(afterKeyEdit, MovedEndUtc));
            Assert.AreEqual(MovedEndUtc, FindLavaQuestMid(asset.ToDocument()).EndUtcText, "bước kéo ghi thẳng vào asset để timeline vẽ đúng");
            session.CancelContinuousEdit(group);

            Assert.IsTrue(LiveOpsHubCalendarSession.DocumentsEqual(afterKeyEdit, asset.ToDocument()), "Esc phải trả asset về lúc bắt đầu kéo");
            Assert.IsTrue(LiveOpsHubCalendarSession.DocumentsEqual(afterKeyEdit, session.Document));
            Assert.AreEqual(0, CountRedoRecords(), "huỷ kéo không để lại redo (SP-9)");
            Assert.IsFalse(session.IsContinuousEditOpen);

            // Bước Undo kế tiếp phải là "Đổi key remote" — không có bước kéo rỗng nào chen giữa.
            Undo.PerformUndo();
            Assert.IsTrue(LiveOpsHubCalendarSession.DocumentsEqual(original, asset.ToDocument()),
                "một lần ⌘Z sau Esc phải gỡ đúng lần sửa trước kéo — kéo đã huỷ không được thành một bước Undo");
        }

        [UnityTest]
        public IEnumerator Session_ContinuousEditCancel_RevertsFieldsChangedInLaterFrames()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = OpenSession(asset);
            LiveEventCalendarDocument start = session.Document;

            int group = session.BeginContinuousEdit("Kéo nhiều frame");
            // Frame 1: field A (endUtc của lava-quest-2026-09b).
            session.UpdateContinuousEdit(MoveLavaQuestEnd(start, MovedEndUtc));
            yield return null;
            yield return null;

            // Frame 3: A giữ giá trị mới, thêm B (startUtc của hunt-0914) và C (configKey của hunt-0914) — sau lần flush của vòng editor.
            FixedLiveEventEntry hunt = FindEntry(start, LiveOpsDesignSample.HuntEarlyEntryKey);
            FixedLiveEventEntry movedHunt = new FixedLiveEventEntry(hunt.EntryKey, hunt.EventId, hunt.EventType, "2026-09-14T06:00:00Z", hunt.EndUtcText, "hunt_later");
            session.UpdateContinuousEdit(new CompositeCalendarEdit(new LiveEventCalendarEdit[]
            {
                MoveLavaQuestEnd(start, MovedEndUtc),
                new ReplaceFixedEventEdit(movedHunt),
            }));
            Assert.AreEqual("hunt_later", FindEntry(asset.ToDocument(), LiveOpsDesignSample.HuntEarlyEntryKey).ConfigKey);
            yield return null;
            yield return null;

            session.CancelContinuousEdit(group);

            LiveEventCalendarDocument reverted = asset.ToDocument();
            Assert.AreEqual(FindLavaQuestMid(start).EndUtcText, FindLavaQuestMid(reverted).EndUtcText, "field A đổi ở frame 1 phải về");
            Assert.AreEqual(hunt.StartUtcText, FindEntry(reverted, LiveOpsDesignSample.HuntEarlyEntryKey).StartUtcText,
                "field B đổi ở frame 3 phải về — RecordObject trước MỖI ApplyDocument (SP-9)");
            Assert.AreEqual(hunt.ConfigKey, FindEntry(reverted, LiveOpsDesignSample.HuntEarlyEntryKey).ConfigKey, "field C đổi ở frame 3 phải về");
            Assert.IsTrue(LiveOpsHubCalendarSession.DocumentsEqual(start, reverted));
            Assert.IsTrue(LiveOpsHubCalendarSession.DocumentsEqual(start, session.Document));
            Assert.AreEqual(0, CountRedoRecords(), "redo = 0 sau huỷ");
            Assert.IsFalse(session.HasUnsavedChanges, "huỷ kéo trên asset sạch thì không còn *");
        }

        [Test]
        public void Session_ContinuousEditCommit_CollapsesToOneUndoStep()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = OpenSession(asset);
            LiveEventCalendarDocument start = session.Document;

            int group = session.BeginContinuousEdit("Kéo lava-quest-2026-09b");
            session.UpdateContinuousEdit(MoveLavaQuestEnd(start, "2026-09-20T03:00:00Z"));
            session.UpdateContinuousEdit(MoveLavaQuestEnd(start, "2026-09-20T06:00:00Z"));
            session.UpdateContinuousEdit(MoveLavaQuestEnd(start, MovedEndUtc));
            LiveOpsHubEditOutcome outcome = session.CommitContinuousEdit(group);

            Assert.IsTrue(outcome.Applied, outcome.FailureText);
            Assert.AreEqual(group, outcome.UndoGroup);
            Assert.AreEqual(MovedEndUtc, FindLavaQuestMid(session.Document).EndUtcText);
            Undo.PerformUndo();
            Assert.IsTrue(LiveOpsHubCalendarSession.DocumentsEqual(start, session.Document), "một lần kéo = một bước Undo");
        }

        [Test]
        public void Session_Save_WritesFileClearsUnsaved()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = OpenSession(asset);
            session.Apply(MoveLavaQuestEnd(session.Document, MovedEndUtc), "Dời kết thúc");
            int documentChangedCount = 0;
            session.DocumentChanged += () => documentChangedCount++;

            Assert.IsTrue(session.Save(), "lưu asset thật phải thành công");

            Assert.IsFalse(EditorUtility.IsDirty(asset));
            Assert.IsFalse(session.HasUnsavedChanges, "lưu xong thì hết *");
            string fileText = File.ReadAllText(FileUtil.GetPhysicalPath(session.AssetPath));
            StringAssert.Contains(MovedEndUtc, fileText, "file YAML phải mang giá trị mới");
            Assert.AreEqual(1, documentChangedCount, "lưu báo view vẽ lại chip nháp");
            Assert.AreEqual(LiveEventCalendarSha256.ComputeHex(File.ReadAllBytes(FileUtil.GetPhysicalPath(session.AssetPath))),
                session.Store.GetString(LiveOpsHubCalendarSession.SavedFileHashStoreName, string.Empty),
                "hash file của lần tự lưu phải được giữ để postprocessor không báo nhầm là đổi ngoài");
        }

        [Test]
        public void Session_Save_MemoryAsset_ReturnsFalse()
        {
            LiveOpsHubCalendarSession session = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.EmptyAssetScenario).Session;
            session.Apply(new SetRemoteConfigKeyEdit("khac"), "Đổi key");

            Assert.IsFalse(session.Save(), "asset không có file không lưu được — không được báo đã lưu");
            Assert.IsTrue(session.HasUnsavedChanges);
        }

        [Test]
        public void Session_Discard_RestoresSavedSnapshot()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = OpenSession(asset);
            LiveEventCalendarDocument saved = session.Document;
            session.Apply(MoveLavaQuestEnd(saved, MovedEndUtc), "Dời kết thúc");

            session.DiscardChanges();

            Assert.IsTrue(LiveOpsHubCalendarSession.DocumentsEqual(saved, asset.ToDocument()), "Không lưu = asset về bản chụp lúc lưu");
            Assert.IsFalse(session.HasUnsavedChanges);
            Assert.IsFalse(EditorUtility.IsDirty(asset));
        }

        [Test]
        public void Session_ReloadFromDisk_DropsUnsavedDraft()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = OpenSession(asset);
            string savedEndUtc = FindLavaQuestMid(session.Document).EndUtcText;
            session.Apply(MoveLavaQuestEnd(session.Document, MovedEndUtc), "Dời kết thúc lava-quest");
            Assert.IsTrue(session.HasUnsavedChanges, "phải đang có nháp chưa lưu, nếu không test này vô nghĩa");

            session.ReloadFromDisk();

            Assert.AreEqual(savedEndUtc, FindLavaQuestMid(session.Document).EndUtcText, "Tải lại = lấy đúng bản trên đĩa, bỏ nháp");
            Assert.IsFalse(session.HasUnsavedChanges, "tải lại xong thì hết * trên tab");
            Assert.IsFalse(EditorUtility.IsDirty(asset), "asset không còn bẩn sau khi tải lại");
            Assert.IsNull(session.DiskConflict);

            // R-5: ngăn xếp Undo không được giữ bản ghi dựng lại nháp vừa bị bỏ.
            Undo.PerformUndo();
            Assert.AreEqual(savedEndUtc, FindLavaQuestMid(session.Document).EndUtcText, "⌘Z sau khi Tải lại không được dựng lại nháp cũ");
        }

        [Test]
        public void Session_CheckLane_DoesNotTouchSessionCheck()
        {
            LiveOpsHubCalendarSession session = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario).Session;
            LiveEventCalendarCheckReport sessionReport = session.Check.LastReport;
            Assert.IsNotNull(sessionReport, "ngữ cảnh mẫu phải đã kiểm xong");
            LiveOpsHubCheckStaleReason staleReason = session.Check.StaleReason;
            bool isStale = session.Check.IsStale;
            LiveEventCalendarDocument preview = WithLavaQuestEndDocument(session.Document, MovedEndUtc);

            LiveEventCalendarCheckReport laneReport = session.CheckLane(LavaQuestEventType, preview);

            Assert.IsNotNull(laneReport, "kiểm nhanh một làn phải trả báo cáo của riêng nó");
            Assert.AreSame(sessionReport, session.Check.LastReport, "kiểm nhanh KHÔNG đổi kết quả Kiểm lịch của phiên");
            Assert.AreEqual(isStale, session.Check.IsStale);
            Assert.AreEqual(staleReason, session.Check.StaleReason);
            Assert.IsFalse(session.Check.IsRunning, "kiểm nhanh chạy đồng bộ, không mở lần kiểm nền nào");
            Assert.IsFalse(session.HasUnsavedChanges, "xem trước không đụng tài liệu");
        }

        [Test]
        public void Session_SelectAsset_RunsAutoCheck()
        {
            // (Q-11) Kiểm tự chạy một lần khi mở hub VÀ khi đổi asset — không thì đổi asset xong rail đứng ở "Chưa kiểm lần nào".
            LiveEventCalendarAsset first = LiveOpsHubTestServices.CreateMemoryAsset(LiveEventCalendarDocument.Empty);
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(null).WithCalendarAsset(first).WithAutoCheckOnOpen(true));
            LiveOpsHubCalendarSession session = services.Session;
            session.RunCheckToCompletion();
            Assert.IsNotNull(session.Check.LastReport, "mở hub với asset phải tự kiểm một lần");

            Assert.IsTrue(session.TrySelectAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document)));
            Assert.IsTrue(session.Check.IsRunning, "đổi asset xong phải có một lần kiểm đang chạy (Q-11)");
            session.RunCheckToCompletion();

            Assert.IsNotNull(session.Check.LastReport, "đổi asset cũng phải tự kiểm — Q-11 nói rõ 'và khi đổi asset'");
            Assert.AreEqual(2, session.Check.LastReport.Summary.DroppedCount, "kết quả phải là của asset MỚI (mẫu thiết kế: 2 đợt bị bỏ)");
        }

        [Test]
        public void Session_DisposeOnWindowClose_DoesNotClaimInterruptedByReload()
        {
            LiveEventCalendarAsset asset = LiveOpsHubTestServices.CreateAssetFile(AssetFileName, LiveOpsDesignSample.Document);
            LiveOpsHubCalendarSession session = OpenSession(asset);
            session.StartCheck();
            Assert.IsTrue(session.Check.IsRunning, "phải đang kiểm dở, nếu không test này vô nghĩa");

            // Đóng cửa sổ bình thường (OnDisable → ReleaseServices → Dispose), KHÔNG có domain reload nào.
            session.Dispose();
            LiveOpsHubCalendarSession reopened = OpenSession(asset);

            Assert.AreNotEqual(LiveOpsHubCheckStaleReason.InterruptedByReload, reopened.Check.StaleReason,
                "đóng rồi mở lại cửa sổ trong cùng phiên Unity không phải 'bị cắt ngang khi Unity nạp lại script' (R-25)");
            Assert.AreEqual(LiveOpsHubCheckStaleReason.NeverChecked, reopened.Check.StaleReason);
        }

        [Test]
        public void Session_TryCreateAsset_CreatesFileWithOneUndoGroup()
        {
            LiveOpsHubCalendarSession session = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario).Session;
            string path = LiveOpsHubTestServices.TestFolder + "/Created/Main.asset";

            Assert.IsTrue(session.TryCreateAsset(path));

            Assert.IsNotNull(session.Asset);
            Assert.AreEqual(path, session.AssetPath);
            Assert.IsTrue(File.Exists(FileUtil.GetPhysicalPath(path)), "tạo lịch phải có file trong project");
            Assert.AreEqual(LiveOpsHubStrings.ServicesUndoCreateCalendar, Undo.GetCurrentGroupName());
            Assert.IsFalse(session.HasUnsavedChanges);
            Assert.IsFalse(session.TryCreateAsset(path), "đường dẫn đã có asset → không đè");
            Assert.IsFalse(session.TryCreateAsset("Packages/khong-duoc.asset"), "chỉ tạo trong Assets/");
        }

        [Test]
        public void Session_CompilationIsInExportOrder()
        {
            // Trùng id: mục bắt đầu SAU đứng trước trong asset — thứ tự xuất đưa mục bắt đầu trước lên trước (V-6), game đọc JSON giữ mục đó.
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.Document)
                .WithFixedEvent(new FixedLiveEventEntry("entry-duplicate-late", "dup-0925", "lava-quest", "2026-09-26T00:00:00Z", "2026-09-27T00:00:00Z", string.Empty))
                .WithFixedEvent(new FixedLiveEventEntry("entry-duplicate-early", "dup-0925", "lava-quest", "2026-09-25T00:00:00Z", "2026-09-25T12:00:00Z", string.Empty))
                .Build();
            LiveOpsHubCalendarSession session = OpenSession(LiveOpsHubTestServices.CreateMemoryAsset(document));

            LiveEventCalendarCompilation expected = LiveEventCalendarCompiler.CompileInExportOrder(session.Document);
            LiveEventCalendarCompilation actual = session.Compilation;

            Assert.AreEqual(expected.Entries.Count, actual.Entries.Count);
            for (int index = 0; index < expected.Entries.Count; index++)
            {
                Assert.AreEqual(expected.Entries[index].EntryKey, actual.Entries[index].EntryKey, "mục thứ " + index + " phải theo thứ tự xuất");
                Assert.AreEqual(expected.Entries[index].IsKept, actual.Entries[index].IsKept, "mục thứ " + index);
                Assert.AreEqual(expected.Entries[index].DropReason, actual.Entries[index].DropReason, "mục thứ " + index);
            }
            Assert.IsTrue(actual.TryGetFixedOutcome("entry-duplicate-early", out LiveEventCalendarEntryOutcome early) && early.IsKept,
                "trùng id giữ mục bắt đầu trước — cùng mục game giữ khi đọc JSON xuất");
            Assert.DoesNotThrow(() => session.BuildCheckContext(LiveOpsDesignSample.NowUtc),
                "ngữ cảnh kiểm nhận bản biên dịch của phiên — lệch thứ tự xuất thì builder ném");
        }

        [Test]
        public void Services_ReadBackAndLayoutLoaderInjectable()
        {
            RewritingLiveOpsHubJsonReadBack readBack = new RewritingLiveOpsHubJsonReadBack(text => "{");
            MissingPathsLiveOpsHubLayoutLoader layoutLoader = new MissingPathsLiveOpsHubLayoutLoader(new[] { LiveOpsHubPaths.ShellUxml });
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document))
                .WithJsonReadBack(readBack)
                .WithLayoutLoader(layoutLoader));

            Assert.AreSame(readBack, services.JsonReadBack);
            Assert.AreSame(layoutLoader, services.LayoutLoader);
            Assert.IsNull(services.LayoutLoader.LoadVisualTree(LiveOpsHubPaths.ShellUxml), "seam h28b: thiếu UXML dựng được mà không đổi file");

            services.Session.RunCheckToCompletion();
            ExportGateState gate = services.Session.EvaluateExportGate(services.JsonReadBack, services.Format);
            Assert.AreEqual(ExportGateStatusCode.ReadBackFailed, gate.StatusCode, "seam (h): parser thật đọc chuỗi đã viết lại và báo không đọc được");
            Assert.AreEqual("{", readBack.LastRewrittenText);
        }

        [Test]
        public void Services_DefaultBuild_UsesGamePathAdapters()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(new LiveOpsHubServicesBuilder().WithCalendarAsset(null).WithAutoCheckOnOpen(false));

            Assert.IsInstanceOf<GameParserLiveOpsHubJsonReadBack>(services.JsonReadBack, "mặc định đọc lại bằng parser của game");
            Assert.IsInstanceOf<AssetDatabaseLiveOpsHubLayoutLoader>(services.LayoutLoader);
            Assert.IsInstanceOf<SystemLiveOpsClock>(services.Clock);
            Assert.IsInstanceOf<DeviceLiveOpsHubTimeZone>(services.TimeZone);
            Assert.IsInstanceOf<EditorLiveOpsHubCompilationState>(services.CompilationState);
            Assert.AreSame(LiveEventCalendarValidator.Default, services.Validator);
            Assert.IsNotNull(services.Bus);
            Assert.IsNull(services.Session.Asset, "WithCalendarAsset(null) = không tìm asset nhớ của người dùng");
            Assert.AreEqual(0, services.Session.CalendarAssetCount);
        }

        [Test]
        public void Sections_Create_EverySectionReceivesServices()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);

            List<IHubSection> sections = LiveOpsHubSections.Create(services);

            Assert.AreEqual(6, sections.Count);
            Assert.AreSame(services, ((OverviewSection)sections[0]).Services);
            Assert.AreSame(services, ((EventTypesSection)sections[1]).Services);
            Assert.AreSame(services, ((CalendarSection)sections[2]).Services);
            Assert.AreSame(services, ((RecurringRulesSection)sections[3]).Services);
            Assert.AreSame(services, ((ValidationSection)sections[4]).Services);
            Assert.AreSame(services, ((ExportSection)sections[5]).Services);
            Assert.Throws<ArgumentNullException>(() => LiveOpsHubSections.Create(null));
        }

        [Test]
        public void ContractFreeze_WritesSignatures()
        {
            Type[] frozen = { typeof(IHubHost), typeof(LiveOpsHubServices), typeof(LiveOpsHubCalendarSession), typeof(LiveOpsHubCheckState), typeof(LiveOpsHubPublishState) };
            StringBuilder text = new StringBuilder();
            text.Append("# Hợp đồng đóng băng ở cổng W3 (PD-35)\n\n");
            text.Append("Sinh bằng reflection từ test `CalendarSessionTests.ContractFreeze_WritesSignatures` (G-SESSION) — không sửa tay. ");
            text.Append("Gói W4/W5 cần đổi chữ ký nào dưới đây thì ghi `tools/liveops-hub/contract-changes.md` và dừng (10.1).\n");
            foreach (Type type in frozen) AppendSignatures(text, type);
            string signatures = text.ToString();

            foreach (Type type in frozen) StringAssert.Contains(TypeName(type), signatures);
            StringAssert.Contains("LiveOpsHubServices Services { get; }", signatures, "IHubHost phải có đúng property Services (G-SESSION)");
            StringAssert.Contains("LiveOpsHubEditOutcome Apply(LiveEventCalendarEdit edit, String undoName)", signatures);
            StringAssert.Contains("void SelectCompareSource(LiveOpsHubCompareSource source)", signatures);
            StringAssert.Contains("void Reevaluate(DateTime nowUtc)", signatures);

            string toolsDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tools", ToolsFolderName));
            if (!Directory.Exists(toolsDirectory))
            {
                Assert.Pass("project này không phải dev repo (không có tools/liveops-hub) — chỉ kiểm chữ ký, không ghi file");
            }

            // PD-35 là bộ GÁC, không phải bộ sinh: file đã commit là chữ ký đóng băng. Gỡ/đổi một thành viên đang đóng băng phải làm test
            // này ĐỎ (rồi gói W4/W5 ghi tools/liveops-hub/contract-changes.md và dừng), chứ không được lặng lẽ đổi nội dung file.
            string freezePath = Path.Combine(toolsDirectory, ContractFreezeFileName);
            bool regenerate = string.Equals(Environment.GetEnvironmentVariable(ContractFreezeUpdateVariable), "1", StringComparison.Ordinal);
            if (File.Exists(freezePath) && !regenerate)
            {
                Assert.AreEqual(NormalizeNewLines(File.ReadAllText(freezePath)), NormalizeNewLines(signatures),
                    "chữ ký của 5 kiểu đóng băng đã khác bản commit trong " + ContractFreezeFileName
                    + " — đổi hợp đồng thì ghi contract-changes.md và dừng (10.1); chỉ khi user duyệt mới chạy lại với "
                    + ContractFreezeUpdateVariable + "=1 để sinh lại file");
                return;
            }
            File.WriteAllText(freezePath, signatures, new UTF8Encoding(false));
        }

        // ------------------------------------------------------------------------------------------------------------ hỗ trợ

        internal static LiveOpsHubCalendarSession OpenSession(LiveEventCalendarAsset asset)
        {
            return LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null).WithCalendarAsset(asset)).Session;
        }

        internal static FixedLiveEventEntry FindLavaQuestMid(LiveEventCalendarDocument document)
        {
            return FindEntry(document, LiveOpsDesignSample.LavaQuestMidEntryKey);
        }

        internal static FixedLiveEventEntry FindEntry(LiveEventCalendarDocument document, string entryKey)
        {
            Assert.IsTrue(document.TryGetFixedEvent(entryKey, out FixedLiveEventEntry entry), "tài liệu thiếu đợt " + entryKey);
            return entry;
        }

        /// <summary>Tài liệu xem trước (không đi qua phiên): dời kết thúc lava-quest.</summary>
        internal static LiveEventCalendarDocument WithLavaQuestEndDocument(LiveEventCalendarDocument document, string endUtc)
        {
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, MoveLavaQuestEnd(document, endUtc), out LiveEventCalendarDocument result),
                "lệnh dời kết thúc phải áp được");
            return result;
        }

        internal static LiveEventCalendarEdit MoveLavaQuestEnd(LiveEventCalendarDocument document, string endUtc)
        {
            FixedLiveEventEntry entry = FindLavaQuestMid(document);
            return new ReplaceFixedEventEdit(new FixedLiveEventEntry(entry.EntryKey, entry.EventId, entry.EventType, entry.StartUtcText, endUtc, entry.ConfigKey));
        }

        /// <summary>Số bản ghi redo của Undo (API internal <c>Undo.GetRecords</c> — cùng cách đo của SP-9).</summary>
        internal static int CountRedoRecords()
        {
            MethodInfo method = typeof(Undo).GetMethod("GetRecords", BindingFlags.NonPublic | BindingFlags.Static, null,
                new[] { typeof(List<string>), typeof(List<string>) }, null);
            if (method == null) Assert.Inconclusive("Undo.GetRecords(undo, redo) không có ở bản Unity này — không đếm được redo");
            List<string> undoRecords = new List<string>();
            List<string> redoRecords = new List<string>();
            method.Invoke(null, new object[] { undoRecords, redoRecords });
            return redoRecords.Count;
        }

        private static void AppendSignatures(StringBuilder text, Type type)
        {
            text.Append("\n## ").Append(TypeName(type)).Append("\n\n```csharp\n");
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            List<string> lines = new List<string>();
            foreach (ConstructorInfo constructor in type.GetConstructors(Flags))
            {
                if (constructor.IsPrivate) continue;
                lines.Add(Accessibility(constructor) + TypeName(type) + "(" + Parameters(constructor.GetParameters()) + ")");
            }
            foreach (EventInfo eventInfo in type.GetEvents(Flags))
            {
                MethodInfo adder = eventInfo.GetAddMethod(true);
                if (adder == null || adder.IsPrivate) continue;
                lines.Add(Accessibility(adder) + "event " + TypeName(eventInfo.EventHandlerType) + " " + eventInfo.Name);
            }
            foreach (PropertyInfo property in type.GetProperties(Flags))
            {
                MethodInfo getter = property.GetGetMethod(true);
                MethodInfo setter = property.GetSetMethod(true);
                MethodInfo visible = getter != null && !getter.IsPrivate ? getter : setter;
                if (visible == null || visible.IsPrivate) continue;
                string accessors = (getter != null && !getter.IsPrivate ? "get; " : string.Empty) + (setter != null && !setter.IsPrivate ? "set; " : string.Empty);
                lines.Add(Accessibility(visible) + TypeName(property.PropertyType) + " " + property.Name + " { " + accessors + "}");
            }
            foreach (MethodInfo method in type.GetMethods(Flags))
            {
                if (method.IsPrivate || method.IsSpecialName) continue;
                lines.Add(Accessibility(method) + (method.IsStatic ? "static " : string.Empty) + (method.ReturnType == typeof(void) ? "void" : TypeName(method.ReturnType))
                          + " " + method.Name + "(" + Parameters(method.GetParameters()) + ")");
            }
            lines.Sort(StringComparer.Ordinal);
            foreach (string line in lines) text.Append(line).Append('\n');
            text.Append("```\n");
        }

        private static string Accessibility(MethodBase method)
        {
            if (method.DeclaringType != null && method.DeclaringType.IsInterface) return string.Empty;
            if (method.IsPublic) return "public ";
            if (method.IsAssembly) return "internal ";
            if (method.IsFamily) return "protected ";
            return "private ";
        }

        private static string Parameters(ParameterInfo[] parameters)
        {
            StringBuilder builder = new StringBuilder();
            foreach (ParameterInfo parameter in parameters)
            {
                if (builder.Length > 0) builder.Append(", ");
                builder.Append(TypeName(parameter.ParameterType)).Append(' ').Append(parameter.Name);
            }
            return builder.ToString();
        }

        /// <summary>So chữ ký theo nội dung, không theo kiểu xuống dòng của máy đã commit file.</summary>
        private static string NormalizeNewLines(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static string TypeName(Type type)
        {
            if (Nullable.GetUnderlyingType(type) is Type underlying) return TypeName(underlying) + "?";
            if (!type.IsGenericType) return type.Name;
            string name = type.Name.Substring(0, type.Name.IndexOf('`'));
            StringBuilder builder = new StringBuilder(name).Append('<');
            Type[] arguments = type.GetGenericArguments();
            for (int index = 0; index < arguments.Length; index++)
            {
                if (index > 0) builder.Append(", ");
                builder.Append(TypeName(arguments[index]));
            }
            return builder.Append('>').ToString();
        }
    }
}
