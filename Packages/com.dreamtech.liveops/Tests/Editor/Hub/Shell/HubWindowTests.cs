using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Cửa sổ hub thật (UI, không -nographics): trình tự CreateGUI 8.1, mọi màn dựng không ném ở các ngữ cảnh, màn ném không kéo
    /// sập cửa sổ, thiếu UXML không trắng cửa sổ, id sai về Tổng quan, trạng thái cửa sổ sống qua serialize, mở từ menu.
    /// <para>
    /// Ngữ cảnh (không asset / asset rỗng / mẫu / kiểm cũ / đang kiểm) ở W2 chưa có phiên lịch: 6 màn thật là màn giữ chỗ không đọc
    /// dữ liệu, nên phần "registry thật" giống nhau ở mọi ngữ cảnh. Phần khác nhau giữa các ngữ cảnh là registry giả cùng id/tầng
    /// mang health của ngữ cảnh đó — khung (rail, ô chặn, badge tầng, element sống còn) được kiểm với đúng tổ hợp dấu mà màn thật
    /// sẽ phát ra. Dữ liệu thật của từng ngữ cảnh do G-SESSION nối (W3): đề xuất cho G-SESSION cùng quyền ghi file này và probe ở
    /// plan/w2/contract-changes-G-SHELL.md (CC-SHELL-1) — chưa được duyệt thì các test này KHÔNG chứng minh màn thật chạy với dữ liệu.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class HubWindowTests
    {
        private static readonly DateTime CheckedAtUtc = new DateTime(2026, 9, 13, 8, 46, 30, DateTimeKind.Utc);

        private const double CompilationNoteTimeoutSeconds = 5.0;

        private LiveOpsHubWindowTestScope _scope;

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
            _scope = null;
        }

        [UnityTest]
        public IEnumerator Open_EverySection_CreateViewWithoutException_NoAsset()
        {
            yield return AssertEverySectionShows(HubTestContext.NoAsset);
        }

        [UnityTest]
        public IEnumerator Open_EverySection_CreateViewWithoutException_EmptyAsset()
        {
            yield return AssertEverySectionShows(HubTestContext.EmptyAsset);
        }

        [UnityTest]
        public IEnumerator Open_EverySection_CreateViewWithoutException_DesignSample()
        {
            yield return AssertEverySectionShows(HubTestContext.DesignSample);
        }

        [UnityTest]
        public IEnumerator Open_EverySection_CreateViewWithoutException_StaleCheck()
        {
            yield return AssertEverySectionShows(HubTestContext.StaleCheck);
        }

        [UnityTest]
        public IEnumerator Open_EverySection_CreateViewWithoutException_RunningCheck()
        {
            yield return AssertEverySectionShows(HubTestContext.RunningCheck);
        }

        [UnityTest]
        public IEnumerator CreateGui_BindsEverySectionBeforeFirstView_RestoresBeforeShown()
        {
            List<FakeHubSection> fakes = FakeHubSection.CreateRegistryShaped();
            _scope = LiveOpsHubWindowTestScope.Open(FakeHubSection.AsSections(fakes), sectionId: LiveOpsHubSections.Ids.Calendar);
            yield return _scope.WaitForLayout();

            foreach (FakeHubSection fake in fakes)
            {
                Assert.AreSame(_scope.Window, fake.BoundHost, fake.Id + ": mọi màn phải được Bind TRƯỚC khi hiện màn nào (8.1 bước 5)");
                Assert.AreEqual("Bind", fake.Calls[0], fake.Id + ": Bind phải là lời gọi đầu tiên");
            }
            FakeHubSection calendar = fakes[2];
            CollectionAssert.AreEqual(new[] { "Bind", "CreateView", "Restore", "Shown" }, calendar.Calls,
                "trạng thái view phải vào màn trước OnShown (8.1 bước 9)");
            Assert.AreEqual(LiveOpsHubSections.Ids.Calendar, _scope.Window.ActiveSectionId);
            Assert.IsTrue(_scope.Window.HubRoot.ClassListContains(LiveOpsHubClassNames.Root));
            Assert.AreEqual(!EditorGUIUtility.isProSkin, _scope.Window.HubRoot.ClassListContains(LiveOpsHubClassNames.SkinLight),
                "class skin bật đúng một dòng theo isProSkin lúc dựng (8.1 bước 4)");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Section_Throws_ShowsFailureView_RailStillNavigates()
        {
            List<FakeHubSection> fakes = FakeHubSection.CreateRegistryShaped();
            fakes[2].CreateViewException = new NullReferenceException("Object reference not set to an instance of an object");
            LogAssert.Expect(LogType.Warning, new Regex("màn 'calendar' ném lỗi khi dựng"));
            _scope = LiveOpsHubWindowTestScope.Open(FakeHubSection.AsSections(fakes), sectionId: LiveOpsHubSections.Ids.Calendar);
            yield return _scope.WaitForLayout();
            LiveOpsHubWindow window = _scope.Window;

            VisualElement card = window.SectionBody.Q(LiveOpsHubFailureView.CardElementName);
            Assert.IsNotNull(card, "màn ném phải hiện card lỗi trong thân, không để thân trắng");
            Assert.IsTrue(card.ClassListContains(LiveOpsHubClassNames.Failure));
            Assert.AreEqual("Không hiện được màn Lịch", card.Q<Label>(LiveOpsHubFailureView.TitleElementName).text);
            StringAssert.Contains("NullReferenceException", card.Q<Label>(LiveOpsHubFailureView.BodyElementName).text, "thân card nêu loại lỗi (mono)");
            Assert.IsNotNull(card.Q<Button>(LiveOpsHubFailureView.CopyButtonName), "card lỗi phải có Copy lỗi");
            Assert.IsNotNull(card.Q<Button>(LiveOpsHubFailureView.RetryButtonName), "card lỗi phải có Thử dựng lại");
            Assert.AreEqual(LiveOpsHubStrings.ShellCalendarTitle, window.SectionHeader.TitleLabel.text, "header giữ tiêu đề thật của màn lỗi");

            SectionHealth health = window.HealthOf(fakes[2]);
            Assert.AreEqual(HealthState.NotMeasured, health.State, "màn ném khi dựng mang NotMeasured tới lần dựng thành công");
            Assert.AreEqual(LiveOpsHubStrings.ShellSectionFailedHealthReason, health.Reason);

            window.Navigate(LiveOpsHubSections.Ids.Validation);
            Assert.AreEqual(LiveOpsHubSections.Ids.Validation, window.ActiveSectionId, "rail vẫn đi được sang màn khác sau khi một màn ném");
            Assert.IsNull(window.SectionBody.Q(LiveOpsHubFailureView.CardElementName), "màn khác không mang card lỗi của Lịch");
            Assert.IsNotNull(window.SectionBody.Q(FakeHubSection.BodyElementName));

            fakes[2].CreateViewException = null;
            window.Navigate(LiveOpsHubSections.Ids.Calendar);
            Assert.IsFalse(window.IsSectionFailed(LiveOpsHubSections.Ids.Calendar), "dựng lại thành công thì bỏ trạng thái lỗi");
            Assert.AreEqual(HealthState.Ok, window.HealthOf(fakes[2]).State);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MissingUxml_ShowsLabelNotBlankWindow()
        {
            MissingPathsLiveOpsHubLayoutLoader loader = new MissingPathsLiveOpsHubLayoutLoader(new[] { LiveOpsHubPaths.ShellUxml });
            _scope = LiveOpsHubWindowTestScope.Open(layoutLoader: loader);
            yield return _scope.WaitForLayout();
            LiveOpsHubWindow window = _scope.Window;

            Assert.IsTrue(window.IsLayoutMissing);
            VisualElement card = window.rootVisualElement.Q(LiveOpsHubFailureView.CardElementName);
            Assert.IsNotNull(card, "thiếu UXML phải dựng card lỗi bằng C#, không để cửa sổ trắng");
            Label title = card.Q<Label>(LiveOpsHubFailureView.TitleElementName);
            Assert.AreEqual("Không tải được bố cục LiveOps Hub: thiếu LiveOpsHub.uxml", title.text);
            StringAssert.Contains(LiveOpsHubPaths.ShellUxml, card.Q<Label>(LiveOpsHubFailureView.BodyElementName).text, "thân nêu đúng đường dẫn cần tìm");
            Assert.IsNotNull(card.Q<Button>(LiveOpsHubFailureView.RevealButtonName), "có nút Mở thư mục package");
            Assert.IsNotNull(card.Q<Button>(LiveOpsHubFailureView.CopyButtonName), "có nút Copy lỗi");
            Assert.AreEqual(LiveOpsHubStrings.ShellWindowTitle, window.rootVisualElement.Q<Label>(LiveOpsHubPaths.ShellElementNames.HeaderTitle).text,
                "header chỉ còn \"LiveOps Hub\"");

            yield return LiveOpsHubWindowTestScope.WaitForLayout(title);
            Assert.Greater(title.layout.width, 0f, "Label lỗi phải thật sự có kích thước trên cửa sổ");
            Assert.Greater(title.layout.height, 0f);
            Assert.IsNull(window.Rail, "không có bố cục thì không dựng rail nửa vời");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MissingUxml_RevealTarget_IsExistingPhysicalLocation()
        {
            // Card thiếu UXML hiện đúng lúc file không có: nút "Mở thư mục package" phải mở một chỗ CÓ trên đĩa (đường vật lý), không
            // đưa đường ảo Packages/… của chính file đang thiếu cho Finder.
            string uiFolder = Path.GetDirectoryName(LiveOpsHubPaths.ShellUxml).Replace('\\', '/');
            string missingTarget = LiveOpsHubFailureView.ResolveRevealPath(uiFolder + "/khong-co-file-nay.uxml");
            Assert.IsTrue(Path.IsPathRooted(missingTarget), "đường vật lý tuyệt đối: " + missingTarget);
            Assert.IsTrue(Directory.Exists(missingTarget), "file thiếu → thư mục gần nhất còn tồn tại: " + missingTarget);
            Assert.AreEqual("UI", Path.GetFileName(missingTarget), "thư mục gần nhất là thư mục file phải nằm");

            string existingTarget = LiveOpsHubFailureView.ResolveRevealPath(LiveOpsHubPaths.ShellUxml);
            Assert.IsTrue(File.Exists(existingTarget), "file còn (bố cục thiếu element) → mở đúng file: " + existingTarget);
            Assert.AreEqual(string.Empty, LiveOpsHubFailureView.ResolveRevealPath(string.Empty));
        }

        [UnityTest]
        public IEnumerator StatusBar_WithoutSession_HidesMarkByClass()
        {
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();
            LiveOpsHubStatusBar statusBar = _scope.Window.StatusBar;

            // W2 chưa có phiên: status bar trống và dấu ẩn bằng class (không style inline) — vòng rỗng không kèm câu bị đọc thành trạng thái thật.
            Assert.IsTrue(statusBar.LeftMark.ClassListContains(LiveOpsHubClassNames.StatusMarkHidden));
            Assert.AreEqual(Visibility.Hidden, statusBar.LeftMark.resolvedStyle.visibility);

            statusBar.SetLeft(HealthState.Ok, "Kiểm lúc 08:46:58 UTC", string.Empty);
            yield return LiveOpsHubWindowTestScope.WaitFrames(2);
            Assert.IsFalse(statusBar.LeftMark.ClassListContains(LiveOpsHubClassNames.StatusMarkHidden));
            Assert.AreEqual(Visibility.Visible, statusBar.LeftMark.resolvedStyle.visibility, "có câu trạng thái thì dấu hiện");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator UnknownSectionId_WarnsAndShowsOverview()
        {
            LogAssert.Expect(LogType.Warning, new Regex("không có màn id 'simulation'.*LiveOpsHubSections\\.Ids"));
            _scope = LiveOpsHubWindowTestScope.Open(sectionId: "simulation");
            yield return _scope.WaitForLayout();

            Assert.AreEqual(LiveOpsHubSections.Ids.Overview, _scope.Window.ActiveSectionId, "id sai phải về Tổng quan, không ném");
            Assert.AreEqual(LiveOpsHubStrings.ShellOverviewTitle, _scope.Window.SectionHeader.TitleLabel.text);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Compiling_ShowsNoteAboveBody_KeepsSection()
        {
            _scope = LiveOpsHubWindowTestScope.Open(isCompiling: true, sectionId: LiveOpsHubSections.Ids.Calendar);
            yield return _scope.WaitForLayout();
            LiveOpsHubWindow window = _scope.Window;

            VisualElement note = window.ShellNotes.Q(LiveOpsHubWindow.CompilingNoteElementName);
            Assert.IsNotNull(note, "đang biên dịch phải hiện note đầu thân (Hình 28 khung 3)");
            Assert.IsTrue(note.ClassListContains(LiveOpsHubClassNames.Note));
            Assert.AreEqual(LiveOpsHubStrings.ShellCompilingNote, note.Q<Label>().text);
            Assert.IsNotNull(note.Q<LiveOpsSpinner>(), "note có spinner — đang làm, không phải lỗi");
            Assert.AreEqual(LiveOpsHubSections.Ids.Calendar, window.ActiveSectionId, "note không thay màn: thân và lựa chọn giữ nguyên");
            Assert.Greater(window.SectionBody.childCount, 0, "không phủ đen/thay thân màn");

            _scope.CompilationState.IsCompiling = false;
            // Poll là lịch 500 ms trên view — chờ theo thời gian thật, không theo số khung (khung batch có thể nhanh hơn nhiều).
            double deadline = EditorApplication.timeSinceStartup + CompilationNoteTimeoutSeconds;
            while (window.ShellNotes.Q(LiveOpsHubWindow.CompilingNoteElementName) != null)
            {
                if (EditorApplication.timeSinceStartup > deadline) Assert.Fail("note không tự gỡ sau khi hết biên dịch (poll 500 ms trên view)");
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void WindowState_SerializesOutcomeSelectionZoom()
        {
            LiveOpsHubWindowState state = new LiveOpsHubWindowState
            {
                ActiveSectionId = LiveOpsHubSections.Ids.Calendar,
                LastOutcome = LiveOpsOutcomeRecord.Ok("Đã copy JSON 5b0d93", "1.601 byte · 65 dòng", CheckedAtUtc, "reveal-file", "calendar.json"),
                RecentActionText = "Dời lava-quest-2026-09b",
                RecentActionUndoGroup = 42,
            };
            const string calendarViewState = "{\"selectedBarKey\":\"hunt-0916-bonus\",\"zoom\":1,\"rangeStartUtc\":\"2026-09-13T00:00:00Z\"}";
            state.SetSectionViewState(LiveOpsHubSections.Ids.Calendar, calendarViewState);
            state.SetSectionViewState(LiveOpsHubSections.Ids.Validation, "{\"filter\":\"dropped\"}");

            LiveOpsHubWindowState restored = JsonUtility.FromJson<LiveOpsHubWindowState>(JsonUtility.ToJson(state));

            Assert.AreEqual(LiveOpsHubSections.Ids.Calendar, restored.ActiveSectionId);
            Assert.IsNotNull(restored.LastOutcome, "outcome phải sống qua domain reload");
            Assert.AreEqual("Đã copy JSON 5b0d93", restored.LastOutcome.Headline);
            Assert.AreEqual("calendar.json", restored.LastOutcome.ActionArgument);
            Assert.AreEqual(calendarViewState, restored.GetSectionViewState(LiveOpsHubSections.Ids.Calendar), "lựa chọn + zoom + khoảng của Lịch phải còn nguyên");
            Assert.AreEqual("{\"filter\":\"dropped\"}", restored.GetSectionViewState(LiveOpsHubSections.Ids.Validation));
            Assert.AreEqual("Dời lava-quest-2026-09b", restored.RecentActionText);
            Assert.AreEqual(42, restored.RecentActionUndoGroup);

            LiveOpsHubWindowState empty = JsonUtility.FromJson<LiveOpsHubWindowState>(JsonUtility.ToJson(new LiveOpsHubWindowState()));
            Assert.IsNull(empty.LastOutcome, "serializer dựng outcome rỗng — phải đọc thành 'không có', không phải outcome trống");
            Assert.AreEqual(string.Empty, empty.GetSectionViewState(LiveOpsHubSections.Ids.Calendar));

            restored.SetSectionViewState(LiveOpsHubSections.Ids.Validation, string.Empty);
            Assert.AreEqual(1, restored.SectionViewStateCount, "JSON rỗng xoá mục, không để rác");
        }

        [UnityTest]
        public IEnumerator EverySection_RequiredElementsPresent_NoAsset()
        {
            yield return AssertRequiredElements(HubTestContext.NoAsset);
        }

        [UnityTest]
        public IEnumerator EverySection_RequiredElementsPresent_DesignSample()
        {
            yield return AssertRequiredElements(HubTestContext.DesignSample);
        }

        [UnityTest]
        public IEnumerator SectionViewState_SurvivesSerializeRoundTrip()
        {
            List<FakeHubSection> fakes = FakeHubSection.CreateRegistryShaped();
            const string zoomState = "{\"zoom\":2,\"selected\":\"hunt-0914\"}";
            fakes[2].ViewStateToCapture = zoomState;
            _scope = LiveOpsHubWindowTestScope.Open(FakeHubSection.AsSections(fakes), sectionId: LiveOpsHubSections.Ids.Calendar);
            yield return _scope.WaitForLayout();

            _scope.Window.Navigate(LiveOpsHubSections.Ids.Export);
            Assert.AreEqual(zoomState, _scope.Window.WindowState.GetSectionViewState(LiveOpsHubSections.Ids.Calendar), "rời màn phải chụp trạng thái view");
            string serialized = JsonUtility.ToJson(_scope.Window.WindowState);
            _scope.Dispose();

            // Mô phỏng domain reload: cửa sổ mới, màn mới (instance khác), trạng thái đọc lại từ JSON đã serialize.
            List<FakeHubSection> reloaded = FakeHubSection.CreateRegistryShaped();
            _scope = LiveOpsHubWindowTestScope.Open(FakeHubSection.AsSections(reloaded), sectionId: LiveOpsHubSections.Ids.Export);
            yield return _scope.WaitForLayout();
            _scope.Window.ReplaceWindowStateForTest(JsonUtility.FromJson<LiveOpsHubWindowState>(serialized));
            _scope.Window.Navigate(LiveOpsHubSections.Ids.Calendar);

            Assert.AreEqual(zoomState, reloaded[2].LastRestoredViewState, "trạng thái view phải đi lại vào màn sau reload");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Open_FromMenuPath()
        {
            CloseAllHubWindows();
            Assert.IsTrue(EditorApplication.ExecuteMenuItem(LiveOpsHubWindow.MenuPath), "menu " + LiveOpsHubWindow.MenuPath + " phải tồn tại");
            yield return null;

            LiveOpsHubWindow[] windows = Resources.FindObjectsOfTypeAll<LiveOpsHubWindow>();
            try
            {
                Assert.AreEqual(1, windows.Length, "mở từ menu phải có đúng một cửa sổ hub");
                int frames = 0;
                while (!LiveOpsHubWindowTestScope.HasLayout(windows[0].rootVisualElement))
                {
                    if (++frames > LiveOpsHubWindowTestScope.MaximumLayoutFrames) Assert.Fail("cửa sổ mở từ menu không có layout");
                    yield return null;
                }
                Assert.IsFalse(windows[0].IsLayoutMissing, "mở từ menu dùng AssetDatabase thật — UXML của package phải nạp được");
                Assert.IsNotNull(windows[0].Rail);
                Assert.AreEqual(6, windows[0].Rail.SectionRows.Count, "mở từ menu dùng registry thật: 6 màn");
                Assert.AreEqual(LiveOpsHubStrings.ShellWindowTitle, windows[0].titleContent.text);
            }
            finally
            {
                CloseAllHubWindows();
            }
            LogAssert.NoUnexpectedReceived();
        }

        // ------------------------------------------------------------------------------------------------------------ ngữ cảnh

        private enum HubTestContext
        {
            NoAsset = 0,
            EmptyAsset = 1,
            DesignSample = 2,
            StaleCheck = 3,
            RunningCheck = 4,
        }

        private IEnumerator AssertEverySectionShows(HubTestContext context)
        {
            // Registry thật.
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();
            ShowEverySection(_scope.Window, context + " (registry)");
            _scope.Dispose();

            // Registry giả mang health của ngữ cảnh — rail dựng đủ dấu/badge/ô chặn mà không ném.
            List<FakeHubSection> fakes = ContextSections(context);
            _scope = LiveOpsHubWindowTestScope.Open(FakeHubSection.AsSections(fakes));
            yield return _scope.WaitForLayout();
            ShowEverySection(_scope.Window, context + " (health ngữ cảnh)");
            Assert.AreEqual(6, _scope.Window.Rail.SectionRows.Count);
            Assert.AreEqual(context == HubTestContext.DesignSample || context == HubTestContext.StaleCheck, _scope.Window.Rail.BlockerElement != null,
                context + ": ô chặn chỉ khi một tầng cổng Blocked");
            LogAssert.NoUnexpectedReceived();
        }

        private IEnumerator AssertRequiredElements(HubTestContext context)
        {
            // Registry thật (6 màn giữ chỗ — chưa đọc dữ liệu nên như nhau ở mọi ngữ cảnh W2).
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();
            AssertShellAndSectionElements(_scope, context + " (registry)");
            _scope.Dispose();

            // Health của ngữ cảnh: rail dựng dấu/badge/ô chặn của ngữ cảnh mà element sống còn của khung và của màn vẫn đủ.
            List<FakeHubSection> fakes = ContextSections(context);
            _scope = LiveOpsHubWindowTestScope.Open(FakeHubSection.AsSections(fakes));
            yield return _scope.WaitForLayout();
            LiveOpsHubWindow window = _scope.Window;
            AssertShellAndSectionElements(_scope, context + " (health ngữ cảnh)");

            List<VisualElement> stageRows = window.Rail.Element.Query(className: LiveOpsHubClassNames.RailStageRow).ToList();
            Assert.AreEqual(4, stageRows.Count, context + ": 4 tầng P1");
            Label checkBadge = stageRows[2].Q<Label>(className: LiveOpsHubClassNames.RailBadge);
            VisualElement blockerHost = window.rootVisualElement.Q(LiveOpsHubPaths.ShellElementNames.RailBlockerHost);
            switch (context)
            {
                case HubTestContext.NoAsset:
                    Assert.IsNull(window.Rail.BlockerElement, "không asset: không có gì bị chặn — không ô chặn");
                    Assert.AreEqual(LiveOpsHubStrings.ShellRailNotMeasuredBadge, checkBadge.text, "không asset: KIỂM là vòng rỗng \"chưa kiểm\"");
                    Assert.AreEqual(0, blockerHost.childCount);
                    break;
                case HubTestContext.DesignSample:
                    Assert.IsNotNull(window.Rail.BlockerElement, "mẫu thiết kế (Hình 4): KIỂM Blocked → có ô chặn");
                    Assert.IsTrue(blockerHost.Contains(window.Rail.BlockerElement), "ô chặn nằm trong element sống còn " + LiveOpsHubPaths.ShellElementNames.RailBlockerHost);
                    Assert.AreEqual("2 bị bỏ", checkBadge.text, "mẫu thiết kế: badge KIỂM \"2 bị bỏ\"");
                    Assert.AreEqual("2 bị bỏ", stageRows[1].Q<Label>(className: LiveOpsHubClassNames.RailBadge).text, "mẫu thiết kế: badge LÊN LỊCH");
                    Assert.AreEqual("chặn", stageRows[3].Q<Label>(className: LiveOpsHubClassNames.RailBadge).text, "mẫu thiết kế: badge XUẤT");
                    break;
            }
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertShellAndSectionElements(LiveOpsHubWindowTestScope scope, string label)
        {
            LiveOpsHubWindow window = scope.Window;
            foreach (string elementName in LiveOpsHubPaths.RequiredShellElementNames)
            {
                Assert.IsNotNull(window.rootVisualElement.Q(elementName), label + ": khung thiếu element '" + elementName + "'");
            }
            foreach (IHubSection section in scope.Sections)
            {
                window.Navigate(section.Id);
                Assert.IsFalse(window.IsSectionFailed(section.Id), label + ": màn " + section.Id + " ném khi dựng");
                foreach (string elementName in section.RequiredElementNames)
                {
                    Assert.IsNotNull(window.SectionBody.Q(elementName), label + ": màn '" + section.Id + "' thiếu element '" + elementName + "'");
                }
            }
        }

        private static void ShowEverySection(LiveOpsHubWindow window, string label)
        {
            foreach (IHubSection section in ((IHubHost)window).Sections)
            {
                window.Navigate(section.Id);
                Assert.AreEqual(section.Id, window.ActiveSectionId, label + ": không mở được màn " + section.Id);
                Assert.IsFalse(window.IsSectionFailed(section.Id), label + ": màn " + section.Id + " ném khi dựng");
                Assert.AreEqual(section.Title, window.SectionHeader.TitleLabel.text, label + ": header không mang tiêu đề của " + section.Id);
                Assert.AreEqual(section.Subtitle, window.SectionHeader.SubtitleLabel.text);
                Assert.Greater(window.SectionBody.childCount, 0, label + ": thân màn " + section.Id + " trống");
                Assert.IsTrue(window.Rail.GetRow(section.Id).ClassListContains(LiveOpsHubClassNames.RailRowActive), label + ": hàng rail không active");
            }
        }

        private static List<FakeHubSection> ContextSections(HubTestContext context)
        {
            const string noAssetReason = "Chưa có asset lịch";
            const string neverCheckedReason = "Chưa kiểm lần nào — F5 để kiểm";
            List<FakeHubSection> sections;
            switch (context)
            {
                case HubTestContext.NoAsset:
                    sections = FakeHubSection.CreateRegistryShaped();
                    for (int index = 1; index < sections.Count; index++) sections[index].Health = SectionHealth.NotMeasured(noAssetReason);
                    return sections;
                case HubTestContext.EmptyAsset:
                    sections = FakeHubSection.CreateRegistryShaped();
                    sections[4].Health = SectionHealth.NotMeasured(neverCheckedReason);
                    sections[5].Health = SectionHealth.NotMeasured(neverCheckedReason);
                    return sections;
                case HubTestContext.DesignSample:
                    return FakeHubSection.CreateDesignSampleShaped();
                case HubTestContext.StaleCheck:
                    sections = FakeHubSection.CreateDesignSampleShaped();
                    sections[4].Health = sections[4].Health.AsStale(CheckedAtUtc);
                    return sections;
                case HubTestContext.RunningCheck:
                    sections = FakeHubSection.CreateRegistryShaped();
                    sections[4].Health = SectionHealth.NotMeasured("Đang kiểm 7/12 luật…");
                    return sections;
                default:
                    throw new ArgumentOutOfRangeException(nameof(context), context, null);
            }
        }

        private static void CloseAllHubWindows()
        {
            foreach (LiveOpsHubWindow window in Resources.FindObjectsOfTypeAll<LiveOpsHubWindow>())
            {
                window.Close();
            }
        }
    }
}
