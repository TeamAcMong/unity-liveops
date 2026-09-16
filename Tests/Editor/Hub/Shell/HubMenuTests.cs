using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Bề mặt "chrome" của khung mà G-SHELLPOLISH (W5) dựng nốt: menu ⋮ của cửa sổ (8.3, [FD §3.3]), cửa sổ dữ liệu mẫu, HelpBox
    /// "Có 2 LiveEventCalendarAsset…" của Tổng quan (mục 12 I-9) và cảnh báo thiếu stylesheet sau khi ba sheet feedback/controls/
    /// timeline thành BẮT BUỘC (mục 12 I-10).
    /// <para>
    /// Vì sao I-10 nằm ở đây chứ không ở một file riêng: 10.3 giao cho gói đúng ba file test
    /// (<c>HubNarrowTests</c>, <c>HubDiskConflictTests</c>, <c>HubMenuTests</c>), và file này là file "chrome còn lại" của khung —
    /// cùng chỗ với <c>Overview_MultipleAssetsHelpBox</c> mà mục 12 I-9 đã đặt tên sẵn.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class HubMenuTests
    {
        private LiveOpsHubWindowTestScope _scope;

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
            _scope = null;
        }

        /// <summary>
        /// (mục 12 I-10) Ba stylesheet feedback/controls/timeline không còn là "chưa tới lượt": thiếu một cái là bản cài hỏng, nên
        /// cửa sổ phải <c>LogWarning</c> NÊU ĐÚNG ĐƯỜNG DẪN — im lặng thì người dùng chỉ thấy hub mất style mà không biết tìm ở đâu.
        /// </summary>
        [UnityTest]
        public IEnumerator MissingOptionalSheet_WarnsWithPath()
        {
            string missingPath = LiveOpsHubPaths.ControlsUss;
            LogAssert.Expect(LogType.Warning, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ShellMissingStyleSheetWarningFormat, missingPath));

            _scope = LiveOpsHubWindowTestScope.Open(layoutLoader: new MissingPathsLiveOpsHubLayoutLoader(new[] { missingPath }));
            yield return _scope.WaitForLayout();

            // Khung vẫn dựng được (cảnh báo, không phải card lỗi): thiếu style không được biến thành cửa sổ trắng.
            Assert.IsNotNull(_scope.Window.HubRoot, "thiếu một stylesheet không làm sập khung — chỉ cảnh báo");
            Assert.IsNotNull(_scope.Window.HubRoot.Q(LiveOpsHubPaths.ShellElementNames.Rail));
        }

        /// <summary>
        /// Menu ⋮ đủ mục theo [FD §3.3] và ĐÚNG THỨ TỰ. "Hiện hướng dẫn phím tắt" là [P1-lùi] (W6) nên không được có mục xám
        /// trỏ tới thứ chưa dựng — đó là luật của mục 12 cho mọi nhánh tạm.
        /// </summary>
        [UnityTest]
        public IEnumerator OverflowMenu_HasAllItems()
        {
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();

            IReadOnlyList<LiveOpsHubWindow.OverflowMenuEntry> entries = _scope.Window.BuildOverflowMenuEntries();
            List<string> texts = new List<string>();
            foreach (LiveOpsHubWindow.OverflowMenuEntry entry in entries) texts.Add(entry.Text);

            Assert.AreEqual(4, entries.Count, "đủ bốn mục P1, không thừa mục nào: " + string.Join(" · ", texts));
            Assert.AreEqual(LiveOpsHubStrings.ShellReduceMotionMenu, texts[1]);
            Assert.AreEqual(LiveOpsHubStrings.ShellOpenDocumentationMenu, texts[2]);
            Assert.AreEqual(LiveOpsHubStrings.ShellShowDesignSampleMenu, texts[3]);
            StringAssert.Contains("Kiểm lại tất cả", texts[0]);

            // OpenForTest dựng phiên KHÔNG asset: "Kiểm lại tất cả" phải xám vì F5 lúc này không làm gì (câu L-1 của soát 16/9).
            Assert.IsFalse(entries[0].IsEnabled, "chưa có asset lịch thì mục kiểm phải xám, không mời bấm một phím không làm gì");
            Assert.IsTrue(entries[2].IsEnabled);
            Assert.IsTrue(entries[3].IsEnabled);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>"Mở tài liệu LiveOps" mở đúng README của package — cùng địa chỉ với link tài liệu luật của Kiểm lịch.</summary>
        [UnityTest]
        public IEnumerator OverflowMenu_OpenDocumentation_UsesPackageReadmeUrl()
        {
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();
            List<string> opened = new List<string>();
            _scope.Window.SetOpenUrlForTest(url => opened.Add(url));

            foreach (LiveOpsHubWindow.OverflowMenuEntry entry in _scope.Window.BuildOverflowMenuEntries())
            {
                if (!string.Equals(entry.Text, LiveOpsHubStrings.ShellOpenDocumentationMenu, StringComparison.Ordinal)) continue;
                entry.Invoke();
            }

            CollectionAssert.AreEqual(new[] { LiveOpsHubPaths.RuleDocumentationBaseUrl }, opened);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// "Hiện dữ liệu mẫu": cửa sổ thứ hai chạy trên asset CHỈ TRONG BỘ NHỚ — không tạo file nào dưới Assets/ và không ghi
        /// đĩa, vì nó chỉ để xem giao diện.
        /// </summary>
        [UnityTest]
        public IEnumerator DesignSampleWindow_DoesNotWriteToDisk()
        {
            LiveOpsHubWindow preview = null;
            try
            {
                preview = LiveOpsHubWindow.OpenPreviewSample();
                yield return LiveOpsHubWindowTestScope.WaitFrames(3);

                LiveOpsHubCalendarSession session = preview.Services.Session;
                Assert.IsNotNull(session.Asset, "cửa sổ mẫu luôn có asset — nó là chỗ xem giao diện ĐẦY ĐỦ, không phải màn trống");
                Assert.AreEqual(string.Empty, session.AssetPath, "asset mẫu không có đường dẫn nên AssetDatabase không ghi được nó ra đĩa");
                Assert.AreEqual(LiveOpsHubPreviewSample.AssetName, session.Asset.name, "tên khác Main.asset để log không lẫn với lịch thật");
                Assert.AreEqual(HideFlags.DontSave, session.Asset.hideFlags & HideFlags.DontSave, "DontSave chặn cả đường lọt qua scene/prefab");
                Assert.AreEqual(LiveOpsHubPreviewSample.NowUtc, session.Clock.UtcNow, "đồng hồ đứng yên: mở lúc nào cũng cùng một khung 'đang chạy'");
            }
            finally
            {
                if (preview != null) preview.Close();
            }
        }

        /// <summary>
        /// (R-24) Hai cửa sổ cùng nghe <c>Undo.undoRedoPerformed</c>: Undo của một asset KHÁC không được kéo cửa sổ mẫu đi theo.
        /// Asset mẫu không bao giờ vào lịch sử Undo, nên phiên của nó phải im lặng.
        /// </summary>
        [UnityTest]
        public IEnumerator PreviewSample_DoesNotReactToRealAssetUndo()
        {
            LiveOpsHubWindow preview = null;
            LiveEventCalendarAsset otherAsset = null;
            try
            {
                preview = LiveOpsHubWindow.OpenPreviewSample();
                yield return LiveOpsHubWindowTestScope.WaitFrames(3);
                LiveOpsHubCalendarSession previewSession = preview.Services.Session;
                int documentChangedCount = 0;
                previewSession.DocumentChanged += () => documentChangedCount++;
                LiveEventCalendarDocument documentBefore = previewSession.Document;

                // Một asset KHÁC đi qua Undo thật: đây chính là "lịch thật đang mở ở cửa sổ kia" của R-24.
                otherAsset = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
                otherAsset.hideFlags = HideFlags.DontSave;
                otherAsset.name = "HubMenuTestsOtherCalendar";
                otherAsset.ApplyDocument(LiveEventCalendarDocument.Empty);
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                Undo.RecordObject(otherAsset, "HubMenuTests đổi lịch khác");
                otherAsset.ApplyDocument(LiveOpsHubPreviewSample.Document);
                Undo.CollapseUndoOperations(group);
                yield return null;

                Undo.PerformUndo();
                yield return null;

                Assert.AreEqual(0, documentChangedCount, "Undo của asset khác không được làm phiên mẫu dựng lại");
                Assert.IsTrue(LiveOpsHubCalendarSession.DocumentsEqual(documentBefore, previewSession.Document),
                    "tài liệu mẫu phải y nguyên sau Undo của asset khác");
            }
            finally
            {
                if (preview != null) preview.Close();
                if (otherAsset != null) UnityEngine.Object.DestroyImmediate(otherAsset);
            }
        }

        /// <summary>Ba sheet của gói khác đã vào nhóm bắt buộc — cờ là hợp đồng với probe CLI, không chỉ với cửa sổ.</summary>
        [Test]
        public void ShellStyleSheets_AllRequired()
        {
            foreach (LiveOpsHubPaths.ShellStyleSheet sheet in LiveOpsHubPaths.ShellStyleSheetLoadOrder)
            {
                Assert.IsTrue(sheet.IsRequired, "stylesheet " + sheet.Path + " phải bắt buộc từ W5 (mục 12 I-10)");
            }
        }
    }
}
