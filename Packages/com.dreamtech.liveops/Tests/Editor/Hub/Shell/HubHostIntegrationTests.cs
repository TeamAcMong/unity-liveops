using System;
using System.Collections;
using System.IO;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Cửa sổ hub thật nối vào phiên (G-HOSTUI, W4): toast/outcome/palette của bus có chỗ đứng trong cửa sổ, chip + status bar đọc
    /// đúng phiên, lý do nút bị khoá in thành chữ (SP-3), và băng "tệp đã đổi trên đĩa" hiện qua CẢ hai đường của SP-8b
    /// (postprocessor và cửa sổ lấy lại focus) mà không bao giờ tự đè nháp.
    /// <para>
    /// Mọi ca dựng bằng <c>OpenWithServices</c> trên CÙNG một services với registry thật — hai phiên song song (bẫy ghi ở
    /// <see cref="LiveOpsHubSections.Create()"/>) sẽ làm chip và rail nói về hai lịch khác nhau.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class HubHostIntegrationTests
    {
        private const string DiskAssetFileName = "HubHostDisk.asset";
        private const string FocusDiskAssetFileName = "HubHostDiskFocus.asset";
        private const string MovedEndUtc = "2026-09-23T12:00:00Z";
        private const string DiskEditMarkerComment = "# sửa tay ngoài Unity";
        private const double LayoutTimeoutSeconds = 5.0;
        private const int SectionCountP1 = 6;
        private const string RecentActionPrefix = " · Vừa làm: ";

        private LiveOpsHubWindow _window;

        [TearDown]
        public void TearDown()
        {
            if (_window != null) _window.Close();
            _window = null;
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        // ------------------------------------------------------------------------------------------------ toast · outcome · palette

        [UnityTest]
        public IEnumerator BusToast_ShowsInContentColumn()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenWindow(services);
            Assert.IsFalse(_window.Toast.IsVisible, "chưa có thao tác nào thì không có toast");

            services.Bus.ShowToast(LiveOpsToastModel.Info("Đã dời kết thúc lava-quest-2026-09b"));
            yield return null;

            Assert.IsTrue(_window.Toast.IsVisible, "cửa sổ phải nghe ToastRequested — không thì mọi toast của màn rơi vào hư không");
            Assert.AreEqual("Đã dời kết thúc lava-quest-2026-09b", _window.Toast.MessageLabel.text);
            VisualElement content = _window.HubRoot.Q(LiveOpsHubPaths.ShellElementNames.Content);
            Assert.AreSame(content, _window.Toast.hierarchy.parent, "toast phải là con của cột nội dung (8.1 bước 7)");
            Assert.AreSame(_window.Toast, content[content.childCount - 1], "toast là con CUỐI — USS không có z-index, thứ tự con quyết định lớp trên");
        }

        /// <summary>
        /// H-1: câu " · Vừa làm: … (⌘Z)" của status bar (8.3, [FD §3.4]). Toast là đường duy nhất câu đó tới được status bar —
        /// phiên mở group Undo thẳng qua <c>Undo.IncrementCurrentGroup</c>, không qua <c>LiveOpsHubUndoTracker.BeginGroup</c>,
        /// nên đọc <c>LastActionText</c> của tracker thì câu không bao giờ hiện.
        /// </summary>
        [UnityTest]
        public IEnumerator BusToastWithUndo_ShowsRecentActionInStatusBar()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenWindow(services);
            Assert.IsFalse(_window.StatusBar.LeftLabel.text.Contains(RecentActionPrefix),
                "chưa làm gì thì status bar không nói 'Vừa làm'");

            const string message = "Đã dời kết thúc lava-quest-2026-09b 19/9 → 20/9 00:00 UTC";
            LiveOpsHubEditOutcome outcome = services.Session.Apply(new SetRemoteConfigKeyEdit("liveops_calendar_v2"), message);
            services.Bus.ShowToast(LiveOpsToastModel.ForEdit(message, outcome.UndoGroup));
            yield return null;

            StringAssert.Contains(RecentActionPrefix + message, _window.StatusBar.LeftLabel.text,
                "toast có Hoàn tác phải để lại câu 'Vừa làm' trên status bar — toast tắt sau 6 giây, câu này là thứ ở lại");
            Assert.AreEqual(message, _window.WindowState.RecentActionText,
                "câu nằm trong trạng thái cửa sổ đã serialize nên sống qua domain reload");
            Assert.AreEqual(outcome.UndoGroup, _window.WindowState.RecentActionUndoGroup);
        }

        /// <summary>Toast không có nút Hoàn tác (thông báo thuần) không phải là "thao tác vừa làm" — không được chiếm câu đó.</summary>
        [UnityTest]
        public IEnumerator BusInfoToast_DoesNotClaimRecentAction()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenWindow(services);

            services.Bus.ShowToast(LiveOpsToastModel.Info("Đã copy 1.612 byte vào clipboard"));
            yield return null;

            Assert.IsFalse(_window.StatusBar.LeftLabel.text.Contains(RecentActionPrefix));
            Assert.AreEqual(string.Empty, _window.WindowState.RecentActionText);
        }

        [UnityTest]
        public IEnumerator BusOutcome_ShownAndSurvivesInWindowState()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenWindow(services);

            services.Bus.ShowOutcome(LiveOpsOutcomeRecord.Ok("Đã copy 1.612 byte vào clipboard", "Dán vào key liveops_calendar",
                services.Clock.UtcNow));
            yield return null;

            Assert.IsTrue(_window.OutcomeView.IsShown, "outcome ở lại tới khi làm việc khác (8.5) — phải hiện ngay");
            Assert.AreEqual("Đã copy 1.612 byte vào clipboard", _window.OutcomeView.HeadlineLabel.text);
            Assert.IsNotNull(_window.WindowState.LastOutcome, "outcome sống trong trạng thái cửa sổ để qua được domain reload");

            services.Bus.ClearOutcome();
            yield return null;

            Assert.IsFalse(_window.OutcomeView.IsShown);
            Assert.IsNull(_window.WindowState.LastOutcome);
        }

        [UnityTest]
        public IEnumerator Palette_ListsSectionsAndRuleIds_OpeningNavigates()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenWindow(services);
            _window.OpenPalette();
            yield return null;

            Assert.GreaterOrEqual(_window.Palette.Matches.Count, SectionCountP1, "palette mở trống liệt kê đủ 6 màn P1");

            // Gõ id luật: palette dẫn tới Kiểm lịch ĐÃ LỌC luật đó — id luật là thứ người dùng nhớ, không phải tên màn ([FD §3.8]).
            _window.Palette.ApplyQuery(LiveEventCalendarRuleIds.OverlapSameType);
            LiveOpsPaletteMatcher.Entry ruleEntry = null;
            int ruleIndex = -1;
            for (int index = 0; index < _window.Palette.Matches.Count; index++)
            {
                if (!_window.Palette.Matches[index].Entry.IsRule) continue;
                ruleEntry = _window.Palette.Matches[index].Entry;
                ruleIndex = index;
                break;
            }
            Assert.IsNotNull(ruleEntry, "gõ id luật phải ra hàng id luật — palette là đường đi tới, không phải chỉ danh sách màn");
            Assert.AreEqual(LiveOpsHubSections.Ids.Validation, ruleEntry.SectionId);

            // Phần "Opening…Navigates" của tên test: chọn đúng hàng đó rồi Enter như người dùng, và khẳng định cửa sổ đã sang
            // Kiểm lịch ĐÃ LỌC luật — trước đây test dừng ở "có hàng id luật" nên đường mở mục chưa có ai canh.
            _window.Palette.MoveSelection(ruleIndex - _window.Palette.SelectedIndex);
            Assert.AreEqual(ruleIndex, _window.Palette.SelectedIndex);
            _window.Palette.HandleKey(KeyCode.Return);
            yield return null;

            Assert.IsFalse(_window.Palette.IsOpen, "mở một mục thì palette đóng lại");
            Assert.AreEqual(LiveOpsHubSections.Ids.Validation, _window.ActiveSectionId);
            Assert.AreEqual(LiveEventCalendarRuleIds.OverlapSameType, _window.LastNavigation.RuleId,
                "id luật phải đi theo điều hướng — không thì người dùng tới màn Kiểm lịch chưa lọc gì");
        }

        // ------------------------------------------------------------------------------------------------ chip · status bar

        [UnityTest]
        public IEnumerator HeaderChips_ReadSession()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenWindow(services);

            Assert.AreEqual(services.Session.AssetFileName, _window.Header.AssetChipTextLabel.text, "chip trái nêu tên asset đang mở");
            Assert.IsNotEmpty(_window.Header.GoToKeyLabel.text, "ô 'Đi tới màn…' phải nêu phím thật của người dùng (⌘K)");
            Assert.AreEqual(LiveOpsHubKeyLabels.For(LiveOpsHubShortcuts.OpenPaletteId), _window.Header.GoToKeyLabel.text);
        }

        [UnityTest]
        public IEnumerator StatusBar_ReadsCheckStateOfSession()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            yield return OpenWindow(services);

            // Chưa thao tác gì nên phần "Vừa làm" rỗng ở CẢ hai vế — ca có thao tác nằm ở
            // BusToastWithUndo_ShowsRecentActionInStatusBar, không gộp vào đây để test này không "đúng" nhờ hai bên cùng rỗng.
            Assert.AreEqual(string.Empty, _window.WindowState.RecentActionText);
            LiveOpsHubStatusBarModel expected = LiveOpsHubStatusBarModel.Build(services.Session.Check, services.Session.Asset != null,
                string.Empty, false, services.Clock.UtcNow, services.Session.Publish.ActiveStamp, services.Format,
                LiveOpsHubKeyLabels.Undo);
            Assert.AreEqual(expected.LeftText, _window.StatusBar.LeftLabel.text, "status bar đọc CHÍNH phiên của cửa sổ");
            Assert.AreEqual(expected.RightText, _window.StatusBar.RightLabel.text);
            Assert.AreEqual(expected.RightTooltip, _window.StatusBar.RightLabel.tooltip, "tooltip giờ máy để không ai đọc nhầm giờ UTC");
        }

        /// <summary>
        /// Chưa có asset: status bar nói "chưa có lịch" chứ KHÔNG mời bấm F5 — <c>StartCheckFromShortcut</c> return ngay khi
        /// <c>Session.Asset == null</c> và mục "Kiểm lại tất cả (F5)" của menu ⋮ đang disabled, nên câu mời F5 là hai bề mặt
        /// nói hai điều khác nhau về cùng một phím (L-5 của soát 16/9).
        /// </summary>
        [UnityTest]
        public IEnumerator StatusBar_NoAsset_StaysEmptyWithoutMark()
        {
            yield return OpenWindow(LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.NoAssetScenario));

            Assert.AreEqual(LiveOpsHubStrings.ShellStatusNoCalendarAsset, _window.StatusBar.LeftLabel.text);
            Assert.AreNotEqual(LiveOpsHubStrings.ShellStatusNeverChecked, _window.StatusBar.LeftLabel.text,
                "không có asset thì F5 không chạy được lần kiểm nào — status bar không được mời bấm");
            Assert.IsTrue(_window.StatusBar.LeftMark.ClassListContains(LiveOpsHubClassNames.StatusMarkHidden),
                "chưa có kết quả kiểm thì dấu ẩn — một dấu không kèm lý do sẽ bị đọc thành trạng thái thật");
        }

        // ------------------------------------------------------------------------------------------------ SP-3 lý do in thành chữ

        [UnityTest]
        public IEnumerator DisabledButton_ReasonRenderedAsText()
        {
            yield return OpenWindow(LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario));
            const string reason = "Chặn: 2 đợt bị bỏ";

            Button button = new Button { text = "Xuất JSON" };
            LiveOpsButtonSlot slot = new LiveOpsButtonSlot(button);
            _window.SectionBody.Add(slot);
            slot.SetEnabledWithReason(false, reason);
            yield return null;

            // SP-3 (quyết định an toàn 16/9): lý do là CHỮ cạnh nút, không phải tooltip — test không assert tooltip hiện hay không.
            Label reasonLabel = slot.Q<Label>(className: LiveOpsHubClassNames.ButtonReason);
            Assert.IsNotNull(reasonLabel, "nút bị khoá phải có nhãn lý do thật trong cây, không chỉ tooltip");
            Assert.AreEqual(reason, reasonLabel.text);
            Assert.IsFalse(button.enabledSelf, "nút vẫn phải thật sự khoá — chữ lý do không thay cho việc chặn bấm");
        }

        // ------------------------------------------------------------------------------------------------ SP-8b băng đĩa

        [UnityTest]
        public IEnumerator DiskChange_WithUnsavedDraft_ShowsBannerAndKeepsDraft()
        {
            LiveOpsHubServices services = CreateFileBackedServices(DiskAssetFileName);
            yield return OpenWindow(services);
            ApplyEdit(services, MovedEndUtc);
            LiveEventCalendarDocument draft = services.Session.Document;

            yield return ChangeAssetOnDiskThenNotify(services);

            Assert.IsNotNull(services.Session.DiskConflict, "file đổi trong lúc còn nháp = xung đột, không phải chuyện im lặng");
            Assert.IsNotNull(_window.DiskBanner, "phải có băng 'tệp đã đổi trên đĩa' (4.3) — không thì người dùng không biết chọn gì");
            Assert.IsNotNull(_window.DiskBanner.Q<Button>(LiveOpsHubWindow.DiskBannerReloadElementName));
            Assert.IsNotNull(_window.DiskBanner.Q<Button>(LiveOpsHubWindow.DiskBannerDiffElementName));
            Assert.IsNotNull(_window.DiskBanner.Q<Button>(LiveOpsHubWindow.DiskBannerKeepElementName));
            Assert.IsTrue(LiveOpsHubCalendarSession.DocumentsEqual(draft, services.Session.Document),
                "hub KHÔNG BAO GIỜ tự đè nháp (SP-8b) — tài liệu của phiên vẫn là bản đang sửa");
        }

        [UnityTest]
        public IEnumerator WindowRegainsFocus_HashDiffers_ShowsDiskBanner()
        {
            LiveOpsHubServices services = CreateFileBackedServices(FocusDiskAssetFileName);
            yield return OpenWindow(services);
            ApplyEdit(services, MovedEndUtc);

            // Đường THỨ HAI của SP-8b: không gọi postprocessor, chỉ đổi file rồi cho cửa sổ lấy lại focus. Đây là ca auto refresh
            // tắt — Unity không tự import, nên nếu hub không tự so hash thì nháp sẽ âm thầm lệch file.
            ChangeAssetFileOnDisk(services);
            Assert.IsNull(services.Session.DiskConflict, "chưa ai báo: trước khi focus, phiên chưa biết file đã đổi");

            yield return RegainFocus();

            Assert.IsNotNull(services.Session.DiskConflict, "cửa sổ lấy lại focus phải tự so hash (SP-8b), không chờ auto refresh");
            Assert.IsNotNull(_window.DiskBanner, "so hash ra khác thì hiện băng, không tự đè");
        }

        [UnityTest]
        public IEnumerator FocusWithoutDiskChange_NoBanner()
        {
            LiveOpsHubServices services = CreateFileBackedServices(DiskAssetFileName);
            yield return OpenWindow(services);
            ApplyEdit(services, MovedEndUtc);

            yield return RegainFocus();

            Assert.IsNull(services.Session.DiskConflict, "hash trùng thì lần so là no-op — focus lên focus xuống không được sinh xung đột giả");
            Assert.IsNull(_window.DiskBanner);
        }

        // ------------------------------------------------------------------------------------------------ hỗ trợ

        private static LiveOpsHubServices CreateFileBackedServices(string fileName)
        {
            return LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                .WithCalendarAsset(LiveOpsHubTestServices.CreateAssetFile(fileName, LiveOpsDesignSample.Document)));
        }

        private static void ApplyEdit(LiveOpsHubServices services, string endUtcText)
        {
            FixedLiveEventEntry entry;
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey, out entry));
            LiveOpsHubEditOutcome outcome = services.Session.Apply(new ReplaceFixedEventEdit(new FixedLiveEventEntry(entry.EntryKey,
                entry.EventId, entry.EventType, entry.StartUtcText, endUtcText, entry.ConfigKey)), "Dời kết thúc " + entry.EventId);
            Assert.IsTrue(outcome.Applied, outcome.FailureText);
        }

        /// <summary>
        /// Đổi NỘI DUNG file asset ngoài Unity (như git pull / sửa tay YAML). Hub so <b>hash file</b> chứ không so cây YAML, nên
        /// thêm một dòng comment là đủ để hash khác — và YAML vẫn đọc được, nên bản trên đĩa vẫn là một lịch hợp lệ để so với nháp.
        /// Ghi bằng <see cref="File"/> chứ không qua AssetDatabase: ca "cửa sổ lấy lại focus" phải KHÔNG có ai báo trước.
        /// </summary>
        private static void ChangeAssetFileOnDisk(LiveOpsHubServices services)
        {
            string assetPath = services.Session.AssetPath;
            Assert.IsNotEmpty(assetPath, "ca này cần asset thật trên đĩa");
            string fullPath = Path.GetFullPath(assetPath);
            File.AppendAllText(fullPath, Environment.NewLine + DiskEditMarkerComment + Environment.NewLine);
        }

        private IEnumerator ChangeAssetOnDiskThenNotify(LiveOpsHubServices services)
        {
            ChangeAssetFileOnDisk(services);
            services.Session.HandleAssetsChanged(new[] { services.Session.AssetPath }, null, null, null);
            yield return null;
        }

        /// <summary>
        /// "Lấy lại focus" thật: <c>OnFocus</c> chỉ bắn khi focus ĐỔI, nên cửa sổ đang focus mà gọi <c>Focus()</c> là no-op. Đưa
        /// focus sang một cửa sổ khác rồi quay lại — đúng động tác của người dùng rời Unity đi sửa file rồi quay về.
        /// </summary>
        private IEnumerator RegainFocus()
        {
            EditorWindow other = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                other.ShowUtility();
                other.Focus();
                yield return null;
                _window.Focus();
                yield return null;
                yield return null;
            }
            finally
            {
                other.Close();
            }
        }

        private IEnumerator OpenWindow(LiveOpsHubServices services)
        {
            _window = LiveOpsHubWindow.OpenWithServices(services, null);
            // SP-16: đặt kích thước SAU Show.
            _window.position = new Rect(0, 0, LiveOpsHubWindowTestScope.StandardWidth, LiveOpsHubWindowTestScope.StandardHeight);
            int frames = 0;
            double deadline = EditorApplication.timeSinceStartup + LayoutTimeoutSeconds;
            // V-23: chỉ fail khi quá CẢ 60 khung LẪN 5 giây.
            while (!LiveOpsHubWindowTestScope.HasLayout(_window.rootVisualElement))
            {
                if (++frames > LiveOpsHubWindowTestScope.MaximumLayoutFrames && EditorApplication.timeSinceStartup > deadline)
                {
                    Assert.Fail("cửa sổ hub không có layout sau 60 khung và 5 giây — test UI phải chạy không -nographics");
                }
                yield return null;
            }
            yield return null;
            yield return null;
        }
    }
}
