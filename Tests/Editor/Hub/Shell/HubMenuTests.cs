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
        private const string FirstCalendarFileName = "HubMenuFirst.asset";
        private const string SecondCalendarFileName = "HubMenuSecond.asset";
        private const string TestAssetLocatorKey = "DreamTech.LiveOps.Hub.Tests.HubMenuCalendarGuid";

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
        /// Menu ⋮ đủ mục theo [FD §3.3] và ĐÚNG THỨ TỰ. Từ W6, "Hiện hướng dẫn phím tắt" là mục THẬT (G-OPT-SHORTCUTHELP dựng
        /// popover của nó) nên nó đứng thứ ba và luôn bật — trước đó gói P1-lùi chưa làm thì luật của mục 12 cấm để lại một mục
        /// xám trỏ vào thứ chưa dựng.
        /// </summary>
        [UnityTest]
        public IEnumerator OverflowMenu_HasAllItems()
        {
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();

            IReadOnlyList<LiveOpsHubWindow.OverflowMenuEntry> entries = _scope.Window.BuildOverflowMenuEntries();
            List<string> texts = new List<string>();
            foreach (LiveOpsHubWindow.OverflowMenuEntry entry in entries) texts.Add(entry.Text);

            Assert.AreEqual(5, entries.Count, "đủ năm mục P1, không thừa mục nào: " + string.Join(" · ", texts));
            Assert.AreEqual(LiveOpsHubStrings.ShellReduceMotionMenu, texts[1]);
            Assert.AreEqual(LiveOpsHubStrings.ShortcutHelpMenuItem, texts[2]);
            Assert.AreEqual(LiveOpsHubStrings.ShellOpenDocumentationMenu, texts[3]);
            Assert.AreEqual(LiveOpsHubStrings.ShellShowDesignSampleMenu, texts[4]);
            StringAssert.Contains("Kiểm lại tất cả", texts[0]);

            // OpenForTest dựng phiên KHÔNG asset: "Kiểm lại tất cả" phải xám vì F5 lúc này không làm gì (câu L-1 của soát 16/9).
            Assert.IsFalse(entries[0].IsEnabled, "chưa có asset lịch thì mục kiểm phải xám, không mời bấm một phím không làm gì");
            Assert.IsTrue(entries[2].IsEnabled, "bảng phím đọc được cả khi chưa có lịch — nó nói về cửa sổ, không về dữ liệu");
            Assert.IsTrue(entries[3].IsEnabled);
            Assert.IsTrue(entries[4].IsEnabled);
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
        /// (R-24) Cửa sổ mẫu phải CÒN LÀ cửa sổ mẫu sau domain reload (biên dịch lại, gắn lại dock). Mọi thứ nhận diện nó —
        /// services tiêm vào, cờ cách ly, asset mẫu — đều là <c>[NonSerialized]</c>, nên trước khi có cờ
        /// <c>isPreviewSampleWindow</c> thì <c>EnsureServices</c> rơi vào nhánh mặc định: phiên THẬT, đi tìm asset lịch mặc
        /// định của project. Người dùng mở "Hiện dữ liệu mẫu (chỉ để xem giao diện)" rồi sửa thử — hoá ra sửa lịch thật.
        /// <para>
        /// Hai test mẫu còn lại chạy trọn trong MỘT domain nên không bao giờ chạm nhánh này. Ở đây cửa sổ được dựng ở đúng
        /// trạng thái sau reload (chỉ field <c>[SerializeField]</c> còn lại) thay vì gọi tay <c>OnDisable</c>/<c>OnEnable</c>:
        /// gọi tay để lại một cửa sổ nửa sống nửa chết mà <c>Close()</c> của Unity không đóng được, và test chạy sau vớ phải nó.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator PreviewSample_AfterDomainReload_StaysOnSampleData()
        {
            LiveOpsHubWindow preview = null;
            try
            {
                preview = LiveOpsHubWindow.OpenPreviewSampleAfterDomainReloadForTest();
                yield return LiveOpsHubWindowTestScope.WaitFrames(3);

                LiveOpsHubCalendarSession session = preview.Services.Session;
                Assert.IsNotNull(session.Asset, "cửa sổ mẫu sau reload vẫn phải có asset mẫu");
                Assert.AreEqual(LiveOpsHubPreviewSample.AssetName, session.Asset.name,
                    "sau reload cửa sổ mẫu không được cầm lịch THẬT của project (R-24)");
                Assert.AreEqual(string.Empty, session.AssetPath, "asset mẫu vẫn không có đường dẫn nên không thể ra đĩa");
                Assert.AreEqual(LiveOpsHubPreviewSample.NowUtc, session.Clock.UtcNow, "đồng hồ mẫu vẫn đứng yên sau reload");
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

        /// <summary>
        /// (mục 12 I-9) Project có nhiều hơn một asset lịch: HelpBox info ở ĐẦU thân Tổng quan nói rõ có mấy cái và hub đang mở
        /// cái nào, kèm nút "Đổi…" [SD1 §1.3]. Model đã tính câu này từ W4 nhưng không ai vẽ.
        /// </summary>
        [UnityTest]
        public IEnumerator Overview_MultipleAssetsHelpBox()
        {
            LiveOpsHubTestServices.CreateAssetFile(FirstCalendarFileName, LiveEventCalendarDocument.Empty);
            LiveEventCalendarAsset second = LiveOpsHubTestServices.CreateAssetFile(SecondCalendarFileName, LiveEventCalendarDocument.Empty);
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                .WithCalendarAsset(second)
                .WithAssetLocator(new LiveOpsHubAssetLocator(TestAssetLocatorKey)));
            LiveOpsHubWindow window = null;
            try
            {
                Assert.Greater(services.Session.CalendarAssetCount, 1, "ca này cần project có nhiều hơn một asset lịch");
                window = LiveOpsHubWindow.OpenWithServices(services, LiveOpsHubSections.Ids.Overview);
                window.position = new Rect(0, 0, LiveOpsHubWindowTestScope.StandardWidth, LiveOpsHubWindowTestScope.StandardHeight);
                yield return LiveOpsHubWindowTestScope.WaitFrames(3);

                VisualElement notice = window.SectionBody.Q(OverviewSection.MultipleAssetsElementName);
                Assert.IsNotNull(notice, "HelpBox nhiều asset phải nằm trong cây của màn");
                Assert.IsFalse(notice.ClassListContains(LiveOpsHubClassNames.OverviewHidden), "có 2 asset thì HelpBox phải hiện");

                Label text = notice.Q<Label>(OverviewSection.MultipleAssetsTextElementName);
                StringAssert.Contains("LiveEventCalendarAsset", text.text);
                StringAssert.Contains(services.Session.AssetFileName, text.text, "phải nói hub đang mở CÁI NÀO, không chỉ 'có nhiều cái'");
                Assert.IsNotNull(notice.Q<Button>(OverviewSection.SwitchAssetButtonElementName), "phải có lối đổi sang asset khác");
                Assert.IsNotNull(window.SectionBody.panel, "thân màn phải còn trong panel — HelpBox không được thay cả thân");
            }
            finally
            {
                if (window != null) window.Close();
                LiveOpsHubTestServices.ReleaseAll();
            }
        }

        /// <summary>Một asset: không có gì để nói, HelpBox biến mất hẳn thay vì thành một dòng rỗng chiếm chỗ.</summary>
        [UnityTest]
        public IEnumerator Overview_SingleAsset_NoHelpBox()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.ForScenario(LiveOpsHubTestServices.DesignSampleScenario);
            LiveOpsHubWindow window = null;
            try
            {
                window = LiveOpsHubWindow.OpenWithServices(services, LiveOpsHubSections.Ids.Overview);
                window.position = new Rect(0, 0, LiveOpsHubWindowTestScope.StandardWidth, LiveOpsHubWindowTestScope.StandardHeight);
                yield return LiveOpsHubWindowTestScope.WaitFrames(3);

                VisualElement notice = window.SectionBody.Q(OverviewSection.MultipleAssetsElementName);
                Assert.IsNotNull(notice);
                Assert.IsTrue(notice.ClassListContains(LiveOpsHubClassNames.OverviewHidden));
                Assert.AreEqual(DisplayStyle.None, notice.resolvedStyle.display, "ẩn thật, không chỉ chữ rỗng");
            }
            finally
            {
                if (window != null) window.Close();
                LiveOpsHubTestServices.ReleaseAll();
            }
        }

        /// <summary>
        /// (Q-1, PD-8) Popover "Đổi key remote…": nút chính khoá kèm lý do IN THÀNH CHỮ khi key rỗng / có khoảng trắng / không
        /// đổi gì (SP-3), và key hợp lệ đi vào một lệnh sửa có Undo.
        /// </summary>
        [UnityTest]
        public IEnumerator RemoteKeyPopover_BlocksBadKeyWithWrittenReason()
        {
            List<string> applied = new List<string>();
            OverviewRemoteKeyPopover popover = new OverviewRemoteKeyPopover(LiveEventCalendarDocument.DefaultRemoteConfigKey, applied.Add);
            VisualElement built = popover.BuildForTest();

            // Nội dung popover phải nằm trong một panel THẬT: BaseField.value gửi ChangeEvent bằng SendEvent, mà SendEvent trên
            // cây ngoài panel là no-op — lý do khoá sẽ không bao giờ đổi theo ký tự gõ vào (đúng điểm của SP-3).
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();
            _scope.Window.SectionBody.Add(built);
            yield return null;

            Assert.AreEqual(LiveEventCalendarDocument.DefaultRemoteConfigKey, popover.KeyField.value, "ô mở ra đã có key đang dùng để sửa");
            Assert.IsFalse(popover.ConfirmSlot.Button.enabledSelf, "chưa đổi gì thì không có gì để áp");
            Assert.AreEqual(LiveOpsHubStrings.OverviewRemoteKeyUnchangedReason, popover.ConfirmSlot.ReasonLabel.text);

            popover.KeyField.value = string.Empty;
            yield return null;
            Assert.IsFalse(popover.ConfirmSlot.Button.enabledSelf);
            Assert.AreEqual(LiveOpsHubStrings.OverviewRemoteKeyEmptyReason, popover.ConfirmSlot.ReasonLabel.text,
                "key rỗng = game không biết đọc lịch ở đâu, phải nói ra chứ không im lặng cho qua");

            popover.KeyField.value = "liveops calendar v2";
            yield return null;
            Assert.IsFalse(popover.ConfirmSlot.Button.enabledSelf);
            Assert.AreEqual(LiveOpsHubStrings.OverviewRemoteKeyWhitespaceReason, popover.ConfirmSlot.ReasonLabel.text);

            popover.KeyField.value = "liveops_calendar_v2";
            yield return null;
            Assert.IsTrue(popover.ConfirmSlot.Button.enabledSelf);
            Assert.AreEqual(string.Empty, popover.ConfirmSlot.ReasonLabel.text, "mở nút thì xoá lý do");
            CollectionAssert.IsEmpty(applied, "chưa bấm thì chưa áp gì");
            LogAssert.NoUnexpectedReceived();
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
