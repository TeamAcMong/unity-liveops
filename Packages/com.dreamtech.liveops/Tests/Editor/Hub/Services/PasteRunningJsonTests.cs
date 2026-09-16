using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
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
    /// Luồng "Dán / Thay nháp / Nhập JSON đang chạy" ([SD2 §2.9], vá V-14). Test đi qua CHÍNH popover của sản phẩm
    /// (<c>LiveOpsHubPasteRunningJsonAction.CreatePopover</c> → <c>BuildForTest</c> → dán → bấm nút chính): gate "parser của
    /// game đọc được chưa" chỉ có một chỗ, nên test dựng lại gate thứ hai là test một thứ không ai chạy.
    /// <para>
    /// Hộp xác nhận đi qua <see cref="ScriptedLiveOpsHubConfirmationPresenter"/> và hộp lưu file qua
    /// <see cref="ManualLiveOpsHubFileDialog"/> — không cửa sổ hệ thống nào mở trong batchmode.
    /// </para>
    /// </summary>
    [TestFixture]
    internal sealed class PasteRunningJsonTests
    {
        private const string RunningJsonWithTwoTypes =
            "{\n" +
            "  \"version\": 2,\n" +
            "  \"recurring\": [\n" +
            "    {\n" +
            "      \"type\": \"weekly-pass\",\n" +
            "      \"anchorUtc\": \"2026-01-05T00:00:00Z\",\n" +
            "      \"idPrefix\": \"weekly-pass-\",\n" +
            "      \"periodHours\": 168,\n" +
            "      \"activeHours\": 168,\n" +
            "      \"configKey\": \"weekly_pass_s3\"\n" +
            "    }\n" +
            "  ],\n" +
            "  \"events\": [\n" +
            "    {\n" +
            "      \"id\": \"lucky-spin-0918\",\n" +
            "      \"type\": \"lucky-spin\",\n" +
            "      \"startUtc\": \"2026-09-18T00:00:00Z\",\n" +
            "      \"endUtc\": \"2026-09-20T00:00:00Z\",\n" +
            "      \"configKey\": \"lucky_spin_v1\"\n" +
            "    }\n" +
            "  ]\n" +
            "}";

        /// <summary>Thiếu dấu phẩy sau <c>"version": 2</c> — lỗi cú pháp có vị trí dòng/ký tự đọc được.</summary>
        private const string BrokenJson =
            "{\n" +
            "  \"version\": 2\n" +
            "  \"events\": []\n" +
            "}";

        /// <summary>Vị trí dấu phẩy còn thiếu của <see cref="BrokenJson"/>: cuối giá trị <c>2</c> ở dòng 2.</summary>
        private const int BrokenJsonErrorLine = 2;
        private const int BrokenJsonErrorColumn = 15;

        /// <summary>Fixture không luật lặp, không đợt — ca "không còn loại chưa khai báo" của outcome sau khi nhập.</summary>
        private const string EmptyRunningJson = "{\n  \"version\": 2,\n  \"recurring\": [],\n  \"events\": []\n}";

        /// <summary>Ô dán cao 96px — cùng số với <c>.liveops-hub-paste-input-scroll</c> trong USS của popover.</summary>
        private const float PasteInputScrollHeight = 96f;

        /// <summary>Đường dẫn nằm ngoài project — ca mà <c>TryCreateAsset</c> không bao giờ chạm tới.</summary>
        private const string OutsideProjectPath = "/Users/ai-do/Desktop/Main.asset";

        private readonly List<string> _createdAssetPaths = new List<string>();

        /// <summary>Cửa sổ chứa cây popover của test bố cục — đóng ở TearDown dù test đỏ giữa chừng.</summary>
        private EditorWindow _layoutWindow;

        [TearDown]
        public void TearDown()
        {
            if (_layoutWindow != null)
            {
                _layoutWindow.Close();
                _layoutWindow = null;
            }
            foreach (string assetPath in _createdAssetPaths)
            {
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (!string.IsNullOrEmpty(guid)) LiveOpsHubTestServices.EraseSessionState(guid);
                AssetDatabase.DeleteAsset(assetPath);
            }
            _createdAssetPaths.Clear();
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        // ------------------------------------------------------------------------------------------------ port mặc định

        /// <summary>
        /// (mục 12 I-3 đóng) Builder mặc định lắp luồng THẬT, nên nút "Dán JSON đang chạy…" của bốn màn luôn bật và không
        /// còn lý do cạnh nút — đây là test khoá việc adapter tạm của bản dev đã biến mất.
        /// </summary>
        [Test]
        public void Actions_DefaultCanPasteRunningJson_IsTrue()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();

            Assert.IsTrue(services.Actions.CanPasteRunningJson, "luồng dán đã dựng thì nút không bao giờ khoá");
            Assert.AreEqual(string.Empty, services.Actions.PasteRunningJsonUnavailableReason,
                "nút mở thì lý do phải rỗng (V-20 C-2) — lý do còn chữ là slot vẫn vẽ nhãn khoá");
        }

        /// <summary>(V-14) Cùng luật đó cho nút "Nhập JSON đang chạy…" của Tổng quan trạng thái (a).</summary>
        [Test]
        public void Actions_DefaultCanImportRunningJson_IsTrue()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario);

            Assert.IsTrue(services.Actions.CanImportRunningJson);
            Assert.AreEqual(string.Empty, services.Actions.ImportRunningJsonUnavailableReason);
        }

        // ------------------------------------------------------------------------------------------------ gate đọc được

        /// <summary>
        /// JSON hỏng: dòng trạng thái nêu DÒNG và KÝ TỰ, nút chính khoá kèm đúng câu đó in thành chữ (SPIKE-B SP-3) và
        /// không luồng nào khởi động dù có bấm.
        /// </summary>
        [Test]
        public void BrokenJson_StatusHasLineAndColumn_ConfirmBlockedWithReason()
        {
            PasteRunningJsonPopover popover = OpenPastePopover(DesignSampleServices());

            popover.PasteForTest(BrokenJson);

            // Vị trí là chỗ dấu phẩy PHẢI có (cuối giá trị dòng 2), không phải chỗ token lạ ở dòng 3 — người sửa gõ dấu phẩy
            // vào đúng đó. Ghép lại câu từ catalog thay vì so chuỗi tiếng Việt: test không được khoá theo ngôn ngữ đang chọn.
            string expectedStatus = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.KitJsonSyntaxPositionFormat,
                BrokenJsonErrorLine, BrokenJsonErrorColumn, LiveOpsHubStrings.KitJsonSyntaxMissingComma);
            Assert.AreEqual(expectedStatus, popover.StatusLabel.text,
                "câu lỗi phải nói DÒNG và KÝ TỰ, không chỉ 'JSON hỏng'");
            Assert.IsFalse(popover.ConfirmButton.enabledSelf);
            Assert.IsTrue(popover.ConfirmSlot.IsBlocked);
            Assert.AreEqual(popover.StatusLabel.text, popover.ConfirmSlot.Reason,
                "lý do cạnh nút phải là chính câu lỗi đang hiện, không phải một câu chung chung thứ hai");

            popover.ClickConfirmForTest();
            Assert.AreEqual(0, popover.SubmitCount, "chưa đọc được thì bấm cũng không được chạy luồng nào");
        }

        /// <summary>Ô còn trống: nút khoá với câu RIÊNG của "chưa dán gì" — nói "JSON hỏng" lúc này là đổ lỗi cho người dùng.</summary>
        [Test]
        public void EmptyInput_ConfirmBlockedWithNothingPastedReason()
        {
            PasteRunningJsonPopover popover = OpenPastePopover(DesignSampleServices());

            Assert.IsFalse(popover.ConfirmButton.enabledSelf);
            Assert.AreEqual(LiveOpsHubStrings.PasteNothingPastedReason, popover.ConfirmSlot.Reason);
        }

        // ------------------------------------------------------------------------------------------------ chỉ so sánh

        /// <summary>
        /// "Chỉ so sánh với nháp": bản dán vào <c>Remote</c> của phiên, luật 12 hết NotMeasured — và nháp KHÔNG đổi
        /// (dán để so không phải là sửa lịch).
        /// </summary>
        [Test]
        public void CompareOnly_SetsRemoteSnapshot_DriftRuleLeavesNotMeasured()
        {
            LiveOpsHubServices services = DesignSampleServices();
            Assume.That(RuleOutcomeOf(services, LiveEventCalendarRuleIds.RemoteSnapshotDrift),
                Is.EqualTo(LiveEventCalendarRuleOutcome.NotMeasured), "chưa dán thì luật 12 là Chưa kiểm");
            LiveEventCalendarDocument draftBefore = services.Session.Document;

            PasteAndConfirm(services, LiveOpsDesignSample.PublishedSnapshotJson, PasteRunningJsonChoice.CompareOnly);
            services.Session.RunCheckToCompletion();

            Assert.IsTrue(services.Session.Remote.HasSnapshot, "bản dán phải sống trong phiên để luật 12 có thước đo");
            Assert.AreNotEqual(LiveEventCalendarRuleOutcome.NotMeasured,
                RuleOutcomeOf(services, LiveEventCalendarRuleIds.RemoteSnapshotDrift),
                "dán xong thì luật 12 phải đo được, không còn là vòng rỗng");
            Assert.AreSame(draftBefore, services.Session.Document, "chỉ so sánh thì nháp không được đổi một mục nào");
            Assert.IsFalse(services.Session.HasUnsavedChanges, "dán để so không bao giờ làm asset bẩn");
        }

        /// <summary>
        /// (V-17) Loại chỉ có trong bản dán mà asset chưa khai báo: luật 8 ra phát hiện <c>unknown-event-type</c> về bản
        /// remote, và nút của hàng là "Khai báo" — người dùng có đúng một việc phải làm tiếp.
        /// </summary>
        [Test]
        public void CompareOnly_UnknownTypeInRemote_FindingWithDeclareButton()
        {
            LiveOpsHubServices services = DesignSampleServices();

            PasteAndConfirm(services, RunningJsonWithTwoTypes, PasteRunningJsonChoice.CompareOnly);
            services.Session.RunCheckToCompletion();

            LiveEventCalendarFinding finding = FindFinding(services, LiveEventCalendarRuleIds.UnknownEventType, "lucky-spin");
            Assert.IsNotNull(finding, "lucky-spin chỉ có trong bản dán và asset chưa khai báo — luật 8 phải nói ra");
            Assert.IsTrue(finding.IsAboutRemoteSnapshot, "phát hiện nói về bản remote, không về nháp (V-17)");
            Assert.AreEqual(LiveOpsHubStrings.FindingButtonDeclareType, LiveOpsFindingText.PrimaryButtonText(finding));
        }

        // ------------------------------------------------------------------------------------------------ thay nháp

        /// <summary>
        /// "Thay nháp bằng JSON này…" qua hộp cấp 1: một Undo group duy nhất, và <c>entryKey</c> ghép THEO ID ĐỢT — mục
        /// trùng id giữ key cũ nên lựa chọn trên timeline và Undo không nhảy mục (mục 6.2).
        /// </summary>
        [Test]
        public void ReplaceDraft_OneUndoGroup_KeepsEntryKeyOfSameEventId()
        {
            ScriptedLiveOpsHubConfirmationPresenter presenter =
                new ScriptedLiveOpsHubConfirmationPresenter(LiveOpsConfirmResult.Destructive);
            LiveOpsHubServices services = DesignSampleServices(presenter);
            string entryKeyBefore = EntryKeyOf(services.Session.Document, "lava-quest-2026-09a");
            int undoGroupBefore = Undo.GetCurrentGroup();

            PasteAndConfirm(services, LiveOpsDesignSample.PublishedSnapshotJson, PasteRunningJsonChoice.ReplaceDraft);

            Assert.AreEqual(1, presenter.Requests.Count, "thay nháp phải hỏi đúng một lần, ở cấp 1");
            Assert.AreEqual(LiveOpsConfirmLevel.Level1, presenter.Requests[0].Level);
            StringAssert.Contains("hunt-0916-bonus", presenter.Requests[0].Body,
                "thân hộp phải nêu TỪNG id sẽ mất, không chỉ một con số");
            Assert.AreEqual(entryKeyBefore, EntryKeyOf(services.Session.Document, "lava-quest-2026-09a"),
                "đợt trùng id phải giữ entryKey cũ — đổi key là timeline, toast và Undo nhảy mục");
            Assert.IsNull(EntryKeyOf(services.Session.Document, "hunt-0916-bonus"),
                "đợt chỉ có trong nháp phải biến mất sau khi thay");

            Undo.RevertAllDownToGroup(undoGroupBefore + 1);
            Assert.IsNotNull(EntryKeyOf(services.Session.Document, "hunt-0916-bonus"),
                "một Undo group: hoàn tác một lần là cả lần thay trở lại");
        }

        /// <summary>Nút an toàn của hộp CHÍNH LÀ "Chỉ so sánh": huỷ thay nháp vẫn giữ bản dán, không bắt dán lại từ đầu.</summary>
        [Test]
        public void ReplaceDraft_SafeChoice_KeepsDraftAndStillSetsRemote()
        {
            ScriptedLiveOpsHubConfirmationPresenter presenter = new ScriptedLiveOpsHubConfirmationPresenter(LiveOpsConfirmResult.Safe);
            LiveOpsHubServices services = DesignSampleServices(presenter);
            LiveEventCalendarDocument draftBefore = services.Session.Document;

            PasteAndConfirm(services, LiveOpsDesignSample.PublishedSnapshotJson, PasteRunningJsonChoice.ReplaceDraft);

            Assert.AreSame(draftBefore, services.Session.Document, "chọn an toàn thì nháp còn nguyên");
            Assert.IsTrue(services.Session.Remote.HasSnapshot, "bản vừa dán vẫn là bản remote để so");
        }

        // ------------------------------------------------------------------------------------------------ nhập (V-14)

        /// <summary>(V-14 bước 3–4) Tạo asset đúng nơi người dùng chọn, nội dung là tài liệu parser đọc từ bản dán.</summary>
        [Test]
        public void ImportWithoutAsset_CreatesAssetAtChosenPath_DocumentFromJson()
        {
            ManualLiveOpsHubFileDialog fileDialog = new ManualLiveOpsHubFileDialog(ImportAssetPath("Created"));
            LiveOpsHubServices services = NoAssetServices(fileDialog);

            PasteAndConfirm(services, RunningJsonWithTwoTypes, PasteRunningJsonChoice.CompareOnly, PasteRunningJsonMode.ImportIntoNewAsset);

            Assert.AreEqual(1, fileDialog.SaveFileCalls.Count, "đọc được thì mới hỏi nơi lưu");
            Assert.IsNotNull(services.Session.Asset, "phiên phải mở đúng asset vừa tạo");
            Assert.AreEqual(fileDialog.PathToReturn, services.Session.AssetPath);
            Assert.AreEqual(1, services.Session.Document.RecurringRules.Count);
            Assert.AreEqual(1, services.Session.Document.FixedEvents.Count);
            Assert.AreEqual(0, services.Session.Document.EventTypes.Count,
                "PD-43: hub không đoán hộ định nghĩa loại — asset mới không có loại nào");
            Assert.AreEqual(LiveEventCalendarDocument.DefaultRemoteConfigKey, services.Session.Document.RemoteConfigKey);
        }

        /// <summary>(V-14 bước 3) Huỷ hộp lưu = không tạo gì: không asset, không phiên nào đổi.</summary>
        [Test]
        public void ImportWithoutAsset_CancelFileDialog_NothingCreated()
        {
            ManualLiveOpsHubFileDialog fileDialog = new ManualLiveOpsHubFileDialog(string.Empty);
            LiveOpsHubServices services = NoAssetServices(fileDialog);

            PasteAndConfirm(services, RunningJsonWithTwoTypes, PasteRunningJsonChoice.CompareOnly, PasteRunningJsonMode.ImportIntoNewAsset);

            Assert.AreEqual(1, fileDialog.SaveFileCalls.Count);
            Assert.IsNull(services.Session.Asset, "huỷ hộp lưu thì phiên vẫn là phiên chưa có lịch");
            // Huỷ là người dùng tự quyết định: KHÔNG được hiện hộp báo nào — đây là vế còn lại của cặp với ca ngoài project.
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>(V-14 bước 2) JSON hỏng: KHÔNG mở hộp lưu, KHÔNG tạo asset — gate đứng trước mọi tác dụng phụ.</summary>
        [Test]
        public void ImportWithoutAsset_BrokenJson_NoFileDialogNoAsset()
        {
            ManualLiveOpsHubFileDialog fileDialog = new ManualLiveOpsHubFileDialog(ImportAssetPath("NeverCreated"));
            LiveOpsHubServices services = NoAssetServices(fileDialog);
            PasteRunningJsonPopover popover = OpenPastePopover(services, PasteRunningJsonMode.ImportIntoNewAsset);

            popover.PasteForTest(BrokenJson);
            popover.ClickConfirmForTest();

            Assert.AreEqual(0, fileDialog.SaveFileCalls.Count, "chưa đọc được thì không bao giờ hỏi nơi lưu");
            Assert.IsNull(services.Session.Asset);
        }

        /// <summary>(V-14 bước 5) Loại chưa khai báo sau khi nhập: phát hiện luật 8 với nút "Khai báo".</summary>
        [Test]
        public void ImportWithoutAsset_TypesLeftUndeclared_UnknownTypeFindingsWithDeclare()
        {
            ManualLiveOpsHubFileDialog fileDialog = new ManualLiveOpsHubFileDialog(ImportAssetPath("Undeclared"));
            LiveOpsHubServices services = NoAssetServices(fileDialog);

            PasteAndConfirm(services, RunningJsonWithTwoTypes, PasteRunningJsonChoice.CompareOnly, PasteRunningJsonMode.ImportIntoNewAsset);
            services.Session.RunCheckToCompletion();

            LiveEventCalendarFinding finding = FindFinding(services, LiveEventCalendarRuleIds.UnknownEventType, "lucky-spin");
            Assert.IsNotNull(finding, "asset mới chưa có loại nào nên mọi loại của bản dán đều chưa khai báo");
            Assert.AreEqual(LiveOpsHubStrings.FindingButtonDeclareType, LiveOpsFindingText.PrimaryButtonText(finding));
        }

        /// <summary>
        /// (V-14 bước 4) Toggle bật: dấu đã đăng ghi trên ĐÚNG byte của bản dán (sha + bản chụp là chuỗi nguyên văn), và
        /// bản dán cũng thành bản remote của phiên nên luật 12 khớp ngay.
        /// </summary>
        [Test]
        public void ImportWithoutAsset_StampToggleOn_BaselineIsPastedTextAndRemoteSnapshotSet()
        {
            ManualLiveOpsHubFileDialog fileDialog = new ManualLiveOpsHubFileDialog(ImportAssetPath("Stamped"));
            LiveOpsHubServices services = NoAssetServices(fileDialog);

            PasteAndConfirm(services, RunningJsonWithTwoTypes, PasteRunningJsonChoice.CompareOnly, PasteRunningJsonMode.ImportIntoNewAsset);

            PublishedCalendarStamp stamp = services.Session.Document.LatestStamp;
            Assert.IsNotNull(stamp, "Toggle bật sẵn nên lần nhập mặc định có dấu đã đăng");
            Assert.AreEqual(RunningJsonWithTwoTypes, stamp.SnapshotJson, "bản chụp của dấu là chuỗi dán NGUYÊN VĂN");
            Assert.AreEqual(LiveOpsHubStrings.PasteImportStampNote, stamp.Note);
            Assert.IsTrue(services.Session.Remote.MatchesStampExactly(stamp),
                "dán rồi ghi dấu cùng một chuỗi thì luật 12 phải khớp ngay ở đường nhanh sha");
        }

        /// <summary>(V-14 bước 4) Toggle tắt: không ghi dấu nào — hub không tự nhận là đã đăng hộ người dùng.</summary>
        [Test]
        public void ImportWithoutAsset_StampToggleOff_NoStamp()
        {
            ManualLiveOpsHubFileDialog fileDialog = new ManualLiveOpsHubFileDialog(ImportAssetPath("NoStamp"));
            LiveOpsHubServices services = NoAssetServices(fileDialog);
            PasteRunningJsonPopover popover = OpenPastePopover(services, PasteRunningJsonMode.ImportIntoNewAsset);

            popover.PasteForTest(RunningJsonWithTwoTypes);
            popover.StampToggle.value = false;
            popover.ClickConfirmForTest();

            Assert.IsNotNull(services.Session.Asset);
            Assert.AreEqual(0, services.Session.Document.PublishedStamps.Count);
        }

        /// <summary>
        /// (V-14 bước 6) Hoàn tác lần nhập: nội dung asset về rỗng, nhưng FILE vẫn nằm trên đĩa — tạo asset không nằm
        /// trong Undo của Unity, nên test khoá đúng cái nửa vời đó thay vì hứa hẹn "Undo xoá luôn file".
        /// </summary>
        [Test]
        public void ImportWithoutAsset_UndoRestoresEmptyAsset_FileKept()
        {
            string assetPath = ImportAssetPath("Undone");
            ManualLiveOpsHubFileDialog fileDialog = new ManualLiveOpsHubFileDialog(assetPath);
            LiveOpsHubServices services = NoAssetServices(fileDialog);
            int undoGroupBefore = Undo.GetCurrentGroup();

            PasteAndConfirm(services, RunningJsonWithTwoTypes, PasteRunningJsonChoice.CompareOnly, PasteRunningJsonMode.ImportIntoNewAsset);
            Assume.That(services.Session.Document.FixedEvents.Count, Is.EqualTo(1));

            Undo.RevertAllDownToGroup(undoGroupBefore + 1);

            Assert.AreEqual(0, services.Session.Document.FixedEvents.Count, "Undo trả nội dung asset về rỗng");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<LiveEventCalendarAsset>(assetPath), "file asset vẫn còn trên đĩa");
        }

        // --------------------------------------------------------------------------- lệch của luật lặp (soát W5 P-10)

        /// <summary>
        /// Luật chỉ có trong nháp thì thay nháp là XOÁ nó, không phải "đổi" nó: hộp cấp 1 tồn tại để nói thứ sẽ mất (bảng 7.0),
        /// mà "đổi luật sky-race" đọc như một việc lành. Kiểm cả hai vế cùng lúc: weekly-pass có ở hai bên nhưng khác nội
        /// dung nên vẫn là "đổi" — một test gộp hai vế mới chứng minh được là chúng KHÁC nhau.
        /// </summary>
        [Test]
        public void ReplaceConfirm_RuleOnlyInDraft_SaysDeletedNotChanged()
        {
            LiveOpsHubServices services = DesignSampleServices();
            LiveOpsHubPasteRunningJsonAction action = (LiveOpsHubPasteRunningJsonAction)services.Actions;
            LiveEventCalendarDocumentParseResult parsed = services.JsonReadBack.ReadBackDocument(RunningJsonWithTwoTypes);
            Assume.That(parsed.IsReadable, Is.True, "fixture của test phải đọc được");

            string body = action.BuildReplaceConfirmRequest(parsed.Document).Body;

            StringAssert.Contains(RuleClause(LiveOpsHubStrings.PasteReplaceConfirmRulesRemovedFormat, "sky-race"), body,
                "sky-race chỉ có trong nháp — thay nháp là nó biến mất, hộp phải nói đúng chữ xoá");
            StringAssert.DoesNotContain(RuleClause(LiveOpsHubStrings.PasteReplaceConfirmRulesFormat, "sky-race"), body,
                "gọi một luật sắp mất là 'đổi luật' là nói nhẹ đi đúng cái nguy hiểm nhất");
            StringAssert.Contains(RuleClause(LiveOpsHubStrings.PasteReplaceConfirmRulesFormat, "weekly-pass"), body,
                "weekly-pass còn ở cả hai bên nhưng khác nội dung — vẫn là vế 'đổi'");
        }

        // --------------------------------------------------------------------------- ngoài project (soát W5 P-5)

        /// <summary>
        /// Bấm Huỷ và chọn nơi lưu NGOÀI project là hai nhánh khác nhau: huỷ trả chuỗi rỗng (im lặng đúng), ngoài project
        /// trả <c>null</c> để luồng còn biết mà báo. Trả cùng một giá trị cho hai ca là cách làm một nhánh hỏng thành vô hình.
        /// </summary>
        [Test]
        public void ProjectRelativeAssetPath_OutsideProject_IsNullAndCancelIsEmpty()
        {
            Assert.AreEqual(string.Empty, LiveOpsHubPasteRunningJsonAction.ProjectRelativeAssetPathOf(string.Empty),
                "huỷ hộp lưu = không chọn gì, không phải một đường dẫn hỏng");
            Assert.IsNull(LiveOpsHubPasteRunningJsonAction.ProjectRelativeAssetPathOf(OutsideProjectPath),
                "ngoài project phải phân biệt được với huỷ thì luồng mới in được lý do thành chữ (SPIKE-B SP-3)");
            Assert.AreEqual("Assets/LiveOps/Calendars/Main.asset",
                LiveOpsHubPasteRunningJsonAction.ProjectRelativeAssetPathOf("Assets/LiveOps/Calendars/Main.asset"),
                "đường dẫn dự án (adapter Manual của test) vẫn đi thẳng");
        }

        /// <summary>
        /// (V-14 bước 3) Chọn nơi lưu ngoài project: KHÔNG tạo asset, KHÔNG đụng phiên — và hộp lưu đã được hỏi đúng một lần
        /// (gate đọc-được đã qua). Chỗ khác huỷ — câu báo — khoá bằng test ngay trên.
        /// </summary>
        [Test]
        public void ImportWithoutAsset_PathOutsideProject_NothingCreated()
        {
            ManualLiveOpsHubFileDialog fileDialog = new ManualLiveOpsHubFileDialog(OutsideProjectPath);
            LiveOpsHubServices services = NoAssetServices(fileDialog);

            // Batchmode không vẽ được hộp nên Unity ghi chính nội dung hộp ra log: đây là cách DUY NHẤT chứng minh người dùng
            // ĐƯỢC BÁO chứ không phải luồng tắt im lặng — và báo đúng lý do "nằm ngoài project" (SPIKE-B SP-3).
            LogAssert.Expect(LogType.Assert, new Regex(Regex.Escape(string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.PasteImportOutsideProjectFormat, OutsideProjectPath))));

            PasteAndConfirm(services, RunningJsonWithTwoTypes, PasteRunningJsonChoice.CompareOnly, PasteRunningJsonMode.ImportIntoNewAsset);

            Assert.AreEqual(1, fileDialog.SaveFileCalls.Count, "đọc được thì vẫn hỏi nơi lưu");
            Assert.IsNull(services.Session.Asset, "ngoài project thì Unity không nạp được asset — không tạo file chết");
        }

        // --------------------------------------------------------------------------- outcome sau khi nhập (soát W5 P-4)

        /// <summary>
        /// (V-14 bước 5) Outcome của lần nhập còn loại chưa khai báo mang <c>ActionId</c> = id màn Loại event — đó là thứ duy nhất
        /// luồng nói được về nút "Mở Loại event" (khung điều hướng theo id). Khoá lại vì nếu không thì đây là code chết:
        /// <c>LiveOpsHubWindow.OutcomeActionLabelOf</c> hiện chưa biết nhãn của id màn nên nút bị ẩn (nợ đã ghi ở w5/deviations.md).
        /// </summary>
        [Test]
        public void ImportWithoutAsset_TypesLeftUndeclared_OutcomeActionOpensEventTypes()
        {
            ManualLiveOpsHubFileDialog fileDialog = new ManualLiveOpsHubFileDialog(ImportAssetPath("OutcomeAction"));
            LiveOpsHubServices services = NoAssetServices(fileDialog);
            List<LiveOpsOutcomeRecord> outcomes = new List<LiveOpsOutcomeRecord>();
            services.Bus.OutcomeRequested += outcome => outcomes.Add(outcome);

            PasteAndConfirm(services, RunningJsonWithTwoTypes, PasteRunningJsonChoice.CompareOnly, PasteRunningJsonMode.ImportIntoNewAsset);

            Assert.AreEqual(1, outcomes.Count, "một lần nhập là đúng một outcome");
            Assert.AreEqual(LiveOpsHubSections.Ids.EventTypes, outcomes[0].ActionId,
                "còn loại chưa khai báo thì việc tiếp theo là mở màn Loại event");
        }

        /// <summary>Mọi loại đã khai báo thì không có việc tiếp theo — <c>ActionId</c> rỗng, khung không vẽ nút nào.</summary>
        [Test]
        public void ReplaceDraft_OutcomeOfImportOnly_NoActionIdWhenTypesDeclared()
        {
            ManualLiveOpsHubFileDialog fileDialog = new ManualLiveOpsHubFileDialog(ImportAssetPath("OutcomeNoAction"));
            LiveOpsHubServices services = NoAssetServices(fileDialog);
            List<LiveOpsOutcomeRecord> outcomes = new List<LiveOpsOutcomeRecord>();
            services.Bus.OutcomeRequested += outcome => outcomes.Add(outcome);

            PasteAndConfirm(services, EmptyRunningJson, PasteRunningJsonChoice.CompareOnly, PasteRunningJsonMode.ImportIntoNewAsset);

            Assert.AreEqual(1, outcomes.Count);
            Assert.AreEqual(string.Empty, outcomes[0].ActionId,
                "không còn loại lạ thì không được mời người dùng sang màn khác — nút phải biến mất");
        }

        // --------------------------------------------------------------------------- bố cục popover (soát W5 P-1/P-3)

        /// <summary>
        /// Dán bản đang chạy THẬT (dài hơn ô 96px rất nhiều): hai nút và dòng trạng thái phải còn NẰM TRONG cửa sổ popover.
        /// <para>
        /// Số 264 của lượt trước chưa từng được kiểm với nội dung thật, mà ô dán lại cao "theo nội dung" — ảnh h09f của lượt đó
        /// cho thấy cây nở quá 550px và hàng nút rơi khỏi khung (soát W5 P-3). Test dựng ĐÚNG khuôn <c>GetWindowSize()</c> mà
        /// <c>PopupWindow</c> dùng, nên nó khoá cả hai số: chiều cao cửa sổ và chiều cao ô dán.
        /// </para>
        /// </summary>
        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator Layout_LongJsonPasted_ButtonsStayInsidePopoverWindow()
        {
            LiveOpsHubServices services = DesignSampleServices();
            LiveOpsHubPasteRunningJsonAction action = (LiveOpsHubPasteRunningJsonAction)services.Actions;
            PasteRunningJsonPopover popover = action.CreatePopover(PasteRunningJsonMode.PasteIntoOpenCalendar);
            Vector2 popoverSize = popover.GetWindowSize();
            _layoutWindow = ScriptableObject.CreateInstance<EditorWindow>();
            _layoutWindow.position = new Rect(120f, 120f, popoverSize.x, popoverSize.y);
            _layoutWindow.ShowUtility();
            VisualElement host = _layoutWindow.rootVisualElement;
            host.style.width = popoverSize.x;
            host.style.height = popoverSize.y;
            host.Add(popover.BuildForTest());
            popover.PasteForTest(LiveOpsDesignSample.PublishedSnapshotJson);

            yield return WaitForLayout(popover.ConfirmButton);

            Assert.AreEqual(PasteInputScrollHeight, InputScrollOf(host).worldBound.height, 1f,
                "ô dán phải đứng ở 96px của thiết kế dù bản dán dài bao nhiêu");
            Assert.LessOrEqual(popover.ConfirmButton.worldBound.yMax, popoverSize.y,
                "nút chính bị đẩy khỏi cửa sổ = người dùng không còn cách nào dán");
            Assert.LessOrEqual(popover.CancelButton.worldBound.yMax, popoverSize.y, "nút Huỷ cũng phải bấm được");
            Assert.LessOrEqual(popover.StatusLabel.worldBound.yMax, popoverSize.y,
                "dòng 'Đọc được: …' là câu phải đọc TRƯỚC khi bấm, không được nằm ngoài khung");
            Assert.LessOrEqual(popover.InputField.worldBound.width, InputScrollOf(host).worldBound.width + 1f,
                "dòng JSON dài phải XUỐNG DÒNG trong ô (white-space: normal), không được nở ngang rồi bị cắt cụt ở mép");
        }

        // ------------------------------------------------------------------------------------------------ hỗ trợ

        private LiveOpsHubServices DesignSampleServices(ILiveOpsHubConfirmationPresenter presenter = null)
        {
            LiveOpsHubServicesBuilder builder = LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(LiveOpsDesignSample.Document));
            if (presenter != null) builder.WithConfirmation(presenter);
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(builder);
            services.Session.RunCheckToCompletion();
            return services;
        }

        private LiveOpsHubServices NoAssetServices(ILiveOpsHubFileDialog fileDialog)
        {
            return LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(null)
                .WithFileDialog(fileDialog));
        }

        /// <summary>Đường dẫn asset dưới thư mục test — <c>LiveOpsHubTestServices.ReleaseAll</c> xoá cả thư mục ở TearDown.</summary>
        private string ImportAssetPath(string name)
        {
            string assetPath = LiveOpsHubTestServices.TestFolder + "/PasteImport" + name + ".asset";
            _createdAssetPaths.Add(assetPath);
            return assetPath;
        }

        /// <summary>Popover của chính luồng — dựng cây mà không mở cửa sổ hệ thống (<c>BuildForTest</c> của lớp gốc).</summary>
        private static PasteRunningJsonPopover OpenPastePopover(LiveOpsHubServices services,
            PasteRunningJsonMode mode = PasteRunningJsonMode.PasteIntoOpenCalendar)
        {
            LiveOpsHubPasteRunningJsonAction action = (LiveOpsHubPasteRunningJsonAction)services.Actions;
            PasteRunningJsonPopover popover = action.CreatePopover(mode);
            popover.BuildForTest();
            return popover;
        }

        private static void PasteAndConfirm(LiveOpsHubServices services, string pastedText, PasteRunningJsonChoice choice,
            PasteRunningJsonMode mode = PasteRunningJsonMode.PasteIntoOpenCalendar)
        {
            PasteRunningJsonPopover popover = OpenPastePopover(services, mode);
            popover.PasteForTest(pastedText);
            popover.SelectChoiceForTest(choice);
            popover.ClickConfirmForTest();
        }

        private static LiveEventCalendarRuleOutcome RuleOutcomeOf(LiveOpsHubServices services, string ruleId)
        {
            LiveEventCalendarCheckReport report = services.Session.Check.LastReport;
            if (report == null) throw new InvalidOperationException("chưa có kết quả kiểm — gọi RunCheckToCompletion trước");
            foreach (LiveEventCalendarRuleResult result in report.RuleResults)
            {
                if (string.Equals(result.RuleId, ruleId, StringComparison.Ordinal)) return result.Outcome;
            }
            throw new InvalidOperationException("không có luật '" + ruleId + "' trong báo cáo");
        }

        private static LiveEventCalendarFinding FindFinding(LiveOpsHubServices services, string ruleId, string targetId)
        {
            LiveEventCalendarCheckReport report = services.Session.Check.LastReport;
            if (report == null) return null;
            foreach (LiveEventCalendarFinding finding in report.Findings)
            {
                if (string.Equals(finding.RuleId, ruleId, StringComparison.Ordinal)
                    && string.Equals(finding.TargetId, targetId, StringComparison.Ordinal))
                {
                    return finding;
                }
            }
            return null;
        }

        /// <summary>Ghép lại một vế luật từ catalog — test không được khoá theo ngôn ngữ đang chọn.</summary>
        private static string RuleClause(string clauseFormat, string ruleType)
        {
            return string.Format(CultureInfo.InvariantCulture, clauseFormat, ruleType);
        }

        private static VisualElement InputScrollOf(VisualElement host)
        {
            VisualElement scroll = host.Q(LiveOpsHubPaths.PasteElementNames.InputScroll);
            Assert.IsNotNull(scroll, "cây popover phải có khung cuộn của ô dán");
            return scroll;
        }

        /// <summary>
        /// Chờ layout theo CẢ số khung lẫn giờ thật (V-23): batch chạy 60 khung trong ~60 ms nên chỉ đếm khung là đỏ giả
        /// trên máy bận.
        /// </summary>
        private static IEnumerator WaitForLayout(VisualElement element)
        {
            double deadline = EditorApplication.timeSinceStartup + 5d;
            int frames = 0;
            while (float.IsNaN(element.worldBound.width) || element.worldBound.width <= 0f)
            {
                if (++frames > 60 && EditorApplication.timeSinceStartup > deadline)
                {
                    Assert.Fail("cây popover không ra layout sau 60 khung và 5 giây");
                }
                yield return null;
            }
        }

        /// <summary>null khi nháp không còn đợt mang id đó — dùng để chứng minh cả "giữ key" lẫn "bị xoá".</summary>
        private static string EntryKeyOf(LiveEventCalendarDocument document, string eventId)
        {
            foreach (FixedLiveEventEntry entry in document.FixedEvents)
            {
                if (string.Equals(entry.EventId, eventId, StringComparison.Ordinal)) return entry.EntryKey;
            }
            return null;
        }
    }
}
