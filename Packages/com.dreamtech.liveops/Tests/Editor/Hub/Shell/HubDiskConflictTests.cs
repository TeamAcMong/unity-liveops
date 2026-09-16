using System;
using System.Collections;
using System.Globalization;
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
    /// Băng "asset đổi từ bên ngoài" của Hình 28 khung 4 (G-SHELLPOLISH W5, mục 12 I-8): ba câu nói đủ hậu quả của cả hai lựa
    /// chọn, "Xem khác biệt" mở pane So với nguồn Disk, và ⌘S sau khi chọn "Giữ bản trong Editor" phải hỏi trước khi ghi đè.
    /// <para>
    /// G-HOSTUI (W4) đã chứng minh băng HIỆN qua cả hai đường của SP-8b (<c>HubHostIntegrationTests</c>); file này lo phần
    /// W5: băng nói GÌ và ⌘S làm gì sau đó.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class HubDiskConflictTests
    {
        private const string DiskAssetFileName = "HubDiskConflict.asset";
        private const string MovedEndUtc = "2026-09-23T12:00:00Z";
        private const string DiskEditMarkerComment = "# sửa tay ngoài Unity";
        private const double LayoutTimeoutSeconds = 5.0;

        private LiveOpsHubWindow _window;

        [TearDown]
        public void TearDown()
        {
            if (_window != null) _window.Close();
            _window = null;
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        /// <summary>
        /// Ba câu của khung 4: đổi bao nhiêu mục · "Tải lại" mất gì · "Giữ bản trong Editor" ghi đè gì. Băng chỉ nói "file đã
        /// đổi" là bắt người dùng đoán hậu quả của cả hai nút.
        /// </summary>
        [UnityTest]
        public IEnumerator DiskBanner_SaysConsequenceOfBothChoices()
        {
            LiveOpsHubServices services = CreateFileBackedServices();
            yield return OpenWindow(services);
            ApplyEdit(services);
            yield return ChangeAssetOnDiskThenNotify(services);

            Assert.IsNotNull(_window.DiskBanner, "file đổi trong lúc còn nháp thì phải có băng");
            Label text = _window.DiskBanner.Q<Label>(LiveOpsHubDiskConflictBanner.TextElementName);
            Assert.IsNotNull(text, "băng phải có phần chữ thật trong cây, không chỉ tooltip trên nút");

            StringAssert.Contains(services.Session.AssetFileName, text.text, "câu luôn gọi tên file, không bao giờ 'asset của bạn'");
            StringAssert.Contains("Tải lại:", text.text, "phải nói hậu quả của Tải lại");
            StringAssert.Contains("Giữ bản trong Editor:", text.text, "phải nói hậu quả của Giữ bản trong Editor");
            StringAssert.Contains("ghi đè", text.text, "người dùng phải biết TRƯỚC rằng lần lưu tới sẽ đè bản trên đĩa");

            Button keep = _window.DiskBanner.Q<Button>(LiveOpsHubDiskConflictBanner.KeepButtonName);
            Assert.IsTrue(keep.ClassListContains(LiveOpsHubClassNames.ButtonPrimary),
                "nút chính = nút AN TOÀN (Hình 28 khung 4): hub không tự đè nên hành động mời sẵn là hành động không mất gì");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// (V-13) "Xem khác biệt" chỉ phát sự kiện điều hướng mang nguồn Disk — pane "So với" là việc của màn LỊCH (bảng V-13
        /// cho nguồn Disk đúng một chỗ vẽ), băng không tự vẽ diff nào.
        /// </summary>
        [UnityTest]
        public IEnumerator DiskBanner_ViewDifferences_NavigatesCalendarCompareDisk()
        {
            LiveOpsHubServices services = CreateFileBackedServices();
            yield return OpenWindow(services);
            ApplyEdit(services);
            yield return ChangeAssetOnDiskThenNotify(services);

            LiveOpsHubNavigation navigation = null;
            Action<LiveOpsHubNavigation> handler = value => navigation = value;
            services.Bus.NavigationRequested += handler;
            try
            {
                Click(_window.DiskBanner.Q<Button>(LiveOpsHubDiskConflictBanner.DiffButtonName));
                yield return null;
            }
            finally
            {
                services.Bus.NavigationRequested -= handler;
            }

            Assert.IsNotNull(navigation, "bấm 'Xem khác biệt' phải phát điều hướng qua bus");
            Assert.AreEqual(LiveOpsHubSections.Ids.Calendar, navigation.SectionId,
                "nguồn Disk chỉ có pane So với ở màn Lịch — màn Xuất JSON không xử lý nguồn này (V-13)");
            Assert.AreEqual(LiveOpsHubCompareSource.Disk, navigation.CompareSource, "nguồn so là bản TRÊN ĐĨA, không phải bản đã đăng");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Đã chọn "Giữ bản trong Editor" thì ⌘S mở hộp cấp 1 "Ghi đè N mục vừa đổi trên đĩa?" (Hình 28 khung 4 dòng cuối).
        /// Chọn nút an toàn = không lưu: asset vẫn bẩn, bản của đồng đội trên đĩa còn nguyên.
        /// </summary>
        [UnityTest]
        public IEnumerator KeepEditorVersion_ThenSave_AsksBeforeOverwritingDisk()
        {
            ScriptedLiveOpsHubConfirmationPresenter confirmation = new ScriptedLiveOpsHubConfirmationPresenter();
            LiveOpsHubServices services = CreateFileBackedServices(confirmation);
            yield return OpenWindow(services);
            ApplyEdit(services);
            yield return ChangeAssetOnDiskThenNotify(services);

            Click(_window.DiskBanner.Q<Button>(LiveOpsHubDiskConflictBanner.KeepButtonName));
            yield return null;
            Assert.IsNull(services.Session.DiskConflict, "trả lời xong thì băng tắt — nhưng file trên đĩa VẪN là bản của đồng đội");
            Assert.IsNull(_window.DiskBanner);
            Assert.IsTrue(_window.IsOverwriteOfDiskPending, "chọn 'Giữ bản trong Editor' = lần ⌘S tới sẽ ghi đè bản trên đĩa");

            // Hàng đợi rỗng → presenter trả Safe, đúng thứ hộp thật trả khi người dùng Esc.
            _window.SetSaveMenuCommandForTest(() => { });
            _window.SaveFromShortcut();
            yield return null;

            Assert.AreEqual(1, confirmation.Requests.Count, "⌘S sau khi Giữ bản trong Editor phải HỎI trước khi ghi");
            LiveOpsConfirmRequest request = confirmation.Requests[0];
            Assert.AreEqual(LiveOpsConfirmLevel.Level1, request.Level, "bảng 7.0: OverwriteDiskChanges là cấp 1");
            Assert.IsTrue(request.Title.StartsWith("Ghi đè ", StringComparison.Ordinal), request.Title);
            StringAssert.Contains("mục vừa đổi trên đĩa", request.Title);
            StringAssert.Contains(services.Session.AssetFileName, request.Body, "thân hộp phải gọi tên file sắp bị đè");
            Assert.IsTrue(services.Session.HasUnsavedChanges, "chọn nút an toàn = không lưu gì, asset vẫn bẩn");
            Assert.IsTrue(_window.IsOverwriteOfDiskPending, "chưa lưu thì câu hỏi vẫn còn treo cho lần ⌘S sau");

            // Lần thứ hai người dùng đồng ý: lưu thật, và lần ⌘S sau không hỏi lại vì đĩa đã mang bản trong Editor.
            confirmation.Enqueue(LiveOpsConfirmResult.Destructive);
            _window.SaveFromShortcut();
            yield return null;
            Assert.AreEqual(2, confirmation.Requests.Count);
            Assert.IsFalse(services.Session.HasUnsavedChanges, "đồng ý thì lưu thật");
            Assert.IsFalse(_window.IsOverwriteOfDiskPending, "đĩa đã mang bản trong Editor — không còn gì để hỏi");

            _window.SaveFromShortcut();
            yield return null;
            Assert.AreEqual(2, confirmation.Requests.Count, "⌘S lần sau không được hỏi lại một câu đã trả lời");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>"Tải lại" = bản đĩa thắng: không còn gì để ghi đè, ⌘S sau đó không được hỏi.</summary>
        [UnityTest]
        public IEnumerator ReloadFromDisk_ThenSave_DoesNotAsk()
        {
            ScriptedLiveOpsHubConfirmationPresenter confirmation = new ScriptedLiveOpsHubConfirmationPresenter();
            LiveOpsHubServices services = CreateFileBackedServices(confirmation);
            yield return OpenWindow(services);
            ApplyEdit(services);
            yield return ChangeAssetOnDiskThenNotify(services);

            Click(_window.DiskBanner.Q<Button>(LiveOpsHubDiskConflictBanner.ReloadButtonName));
            yield return null;

            Assert.IsFalse(_window.IsOverwriteOfDiskPending);
            _window.SetSaveMenuCommandForTest(() => { });
            _window.SaveFromShortcut();
            yield return null;
            Assert.AreEqual(0, confirmation.Requests.Count, "lấy bản đĩa rồi thì ⌘S không đè của ai — không có gì để hỏi");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Diff không nêu id nào (chỉ khác thứ diff hậu quả không liệt kê): câu vẫn đọc được, không có cặp ngoặc rỗng.</summary>
        [Test]
        public void DiskBannerSentences_WithoutItemIds_HasNoEmptyParentheses()
        {
            DateTime detectedUtc = new DateTime(2026, 9, 13, 8, 45, 0, DateTimeKind.Utc);
            LiveEventCalendarDocument document = LiveEventCalendarDocument.Empty;
            LiveOpsHubDiskConflict conflict = new LiveOpsHubDiskConflict(detectedUtc, document, document, document);

            string sentences = LiveOpsHubDiskConflictBanner.BuildSentences(conflict, "Main.asset", new LiveOpsHubFormat(TimeSpan.Zero));

            StringAssert.Contains("Main.asset", sentences);
            Assert.IsFalse(sentences.Contains("()"), "không được in cặp ngoặc rỗng: " + sentences);
        }

        /// <summary>
        /// Câu của băng phải đọc ĐÚNG NHƯ Hình 28 khung 4: "…3 mục (hunt-0914, lava-quest-2026-10, luật weekly-pass). Tải lại:
        /// mất 1 thay đổi chưa lưu (Dời lava-quest-2026-09b)."
        /// <para>
        /// Ba điều test này gác: (a) thứ tự mục theo TÊN chứ không theo thứ tự duyệt tài liệu — cùng một xung đột phải luôn
        /// đọc ra một câu; (b) luật lặp có tiền tố "luật" nên id luật không lẫn với id đợt; (c) thay đổi chưa lưu nói bằng
        /// ĐỘNG TỪ, vì "(lava-quest-2026-09b)" trần bắt người dùng tự nhớ mình đã làm gì với nó.
        /// </para>
        /// </summary>
        [Test]
        public void DiskBannerSentences_MatchDesignFigure()
        {
            LiveOpsHubDiskConflict conflict = LiveOpsHubCaptureScenarios.DesignFigureDiskConflict();

            string sentences = LiveOpsHubDiskConflictBanner.BuildSentences(conflict,
                LiveOpsHubCaptureScenarios.DiskConflictSampleAssetFileName, new LiveOpsHubFormat(TimeSpan.Zero));

            StringAssert.Contains("(hunt-0914, lava-quest-2026-10, luật weekly-pass)", sentences,
                "thứ tự và tên mục phải bám Hình 28 khung 4: " + sentences);
            StringAssert.Contains("(Dời lava-quest-2026-09b)", sentences,
                "thay đổi chưa lưu phải có động từ như hình: " + sentences);
        }

        private static LiveOpsHubServices CreateFileBackedServices(ScriptedLiveOpsHubConfirmationPresenter confirmation = null)
        {
            LiveOpsHubServicesBuilder builder = LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                .WithCalendarAsset(LiveOpsHubTestServices.CreateAssetFile(DiskAssetFileName, LiveOpsDesignSample.Document));
            if (confirmation != null) builder.WithConfirmation(confirmation);
            return LiveOpsHubTestServices.Build(builder);
        }

        private static void ApplyEdit(LiveOpsHubServices services)
        {
            FixedLiveEventEntry entry;
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey, out entry));
            LiveOpsHubEditOutcome outcome = services.Session.Apply(new ReplaceFixedEventEdit(new FixedLiveEventEntry(entry.EntryKey,
                entry.EventId, entry.EventType, entry.StartUtcText, MovedEndUtc, entry.ConfigKey)), "Dời kết thúc " + entry.EventId);
            Assert.IsTrue(outcome.Applied, outcome.FailureText);
        }

        /// <summary>Hub so HASH FILE chứ không so cây YAML — thêm một dòng comment là đủ khác hash mà YAML vẫn đọc được.</summary>
        private IEnumerator ChangeAssetOnDiskThenNotify(LiveOpsHubServices services)
        {
            string assetPath = services.Session.AssetPath;
            Assert.IsNotEmpty(assetPath, "ca này cần asset thật trên đĩa");
            File.AppendAllText(Path.GetFullPath(assetPath), Environment.NewLine + DiskEditMarkerComment + Environment.NewLine);
            // Cửa sổ ghi một cảnh báo CÓ CHỦ ĐÍCH để lịch sử Console còn dấu vết lần file đổi ngoài (SP-8b) — khai trước để
            // LogAssert.NoUnexpectedReceived() ở cuối test vẫn bắt được cảnh báo thật.
            LogAssert.Expect(LogType.Warning, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ShellDiskConflictLogFormat, services.Session.AssetFileName));
            services.Session.HandleAssetsChanged(new[] { assetPath }, null, null, null);
            yield return null;
        }

        /// <summary>
        /// Bấm nút bằng <c>NavigationSubmitEvent</c> — <c>Clickable</c> PHÁT ClickEvent chứ không nghe nó, nên gửi ClickEvent
        /// vào nút là không gọi được handler (cùng helper với OverviewSectionTests).
        /// </summary>
        private static void Click(Button button)
        {
            using (NavigationSubmitEvent submitEvent = NavigationSubmitEvent.GetPooled(EventModifiers.None))
            {
                submitEvent.target = button;
                button.SendEvent(submitEvent);
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
