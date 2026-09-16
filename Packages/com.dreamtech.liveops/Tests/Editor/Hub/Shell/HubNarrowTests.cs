using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Rail 36 px của cửa sổ hẹp ([FD §3.7], mục 12 I-8): số đo rail, menu tầng, nút ‹› ghim mở và khoá EditorPrefs.
    /// <see cref="NarrowRail_Is36px"/> chuyển từ <c>HubLayoutTests</c> (V-21 CC-SHELL-2) — ở đó nó mâu thuẫn với I-8 vì W2 chưa
    /// có rail 36 px.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class HubNarrowTests
    {
        private const float Tolerance = 0.5f;
        private const int NarrowWidth = 820;
        private const int NarrowHeight = 560;

        private LiveOpsHubWindowTestScope _scope;
        private bool _hadPinnedPreference;
        private bool _pinnedPreferenceBefore;

        [SetUp]
        public void SetUp()
        {
            // Khoá EditorPrefs là của MÁY người dùng: nhớ giá trị trước rồi trả lại ở TearDown, như capture.sh làm với UserSkin.
            _hadPinnedPreference = EditorPrefs.HasKey(LiveOpsHubRail.PinnedOpenPreferenceKey);
            _pinnedPreferenceBefore = EditorPrefs.GetBool(LiveOpsHubRail.PinnedOpenPreferenceKey, false);
            EditorPrefs.SetBool(LiveOpsHubRail.PinnedOpenPreferenceKey, false);
        }

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
            _scope = null;
            if (_hadPinnedPreference) EditorPrefs.SetBool(LiveOpsHubRail.PinnedOpenPreferenceKey, _pinnedPreferenceBefore);
            else EditorPrefs.DeleteKey(LiveOpsHubRail.PinnedOpenPreferenceKey);
        }

        /// <summary>
        /// (V-21 CC-SHELL-2) Dưới 900 px rail phải THẬT SỰ còn 36 px, không chỉ bật class: W4 bật class mà giữ 196 nên cột nội
        /// dung ở 820 px chỉ còn 624 và Hình 13 không dựng lại được.
        /// </summary>
        [UnityTest]
        public IEnumerator NarrowRail_Is36px()
        {
            _scope = LiveOpsHubWindowTestScope.Open(width: NarrowWidth, height: NarrowHeight);
            yield return _scope.WaitForLayout();
            VisualElement root = _scope.Window.rootVisualElement;
            VisualElement hubRoot = _scope.Window.HubRoot;

            Assert.IsTrue(hubRoot.ClassListContains(LiveOpsHubClassNames.Narrow), "820 px < 900 nên --narrow phải bật");
            VisualElement rail = root.Q(LiveOpsHubPaths.ShellElementNames.Rail);
            Assert.AreEqual(36f, rail.worldBound.width, Tolerance, "rail thu về 36 px ([FD §3.7])");

            VisualElement content = root.Q(LiveOpsHubPaths.ShellElementNames.Content);
            Assert.AreEqual(NarrowWidth - 36f, content.worldBound.width, Tolerance, "cột nội dung lấy lại đúng phần rail nhả ra");

            // Hình dạng 196 px vẫn còn trong cây (không dựng lại khi đổi bề rộng) nhưng không được VẼ.
            VisualElement caption = root.Q(LiveOpsHubPaths.ShellElementNames.RailCaption);
            Assert.AreEqual(DisplayStyle.None, caption.resolvedStyle.display, "rail 36 px không còn chỗ cho caption 'ĐƯỜNG ĐI CỦA LỊCH'");
            Assert.Greater(_scope.Window.Rail.NarrowCells.Count, 0, "mỗi tầng một ô icon");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Hai ảnh hẹp của 9.5 (<c>hs-shell-narrow-820</c>, <c>hs-shell-compact-700</c>) phải tái lập được trên MỌI máy. Rail
        /// đọc EditorPrefs <c>LiveOpsHub.RailPinnedOpen</c> của máy một lần lúc dựng, nên máy của người đang ghim rail mở sẽ
        /// chụp ra rail 196 px — ảnh khác, số đo trượt, và không ai biết vì sao. Kịch bản chụp phải hạ khoá xuống rồi TRẢ
        /// LẠI đúng như <c>capture.sh</c> làm với <c>UserSkin</c>.
        /// </summary>
        [UnityTest]
        public IEnumerator NarrowCaptureScenario_IgnoresMachinePinnedRailPreference()
        {
            EditorPrefs.SetBool(LiveOpsHubRail.PinnedOpenPreferenceKey, true);
            EditorWindow window = LiveOpsHubCaptureScenarios.OpenShellWithUnpinnedRail();
            try
            {
                window.position = new Rect(window.position.x, window.position.y, NarrowWidth, NarrowHeight);
                yield return null;
                yield return null;
                yield return null;

                LiveOpsHubWindow hub = (LiveOpsHubWindow)window;
                Assert.IsFalse(hub.HubRoot.ClassListContains(LiveOpsHubClassNames.RailPinnedOpen),
                    "kịch bản chụp phải mở cửa sổ với rail KHÔNG ghim, dù máy đang ghim");
                Assert.IsFalse(hub.Rail.IsPinnedOpen);
                Assert.IsTrue(EditorPrefs.GetBool(LiveOpsHubRail.PinnedOpenPreferenceKey, false),
                    "và phải trả lại đúng lựa chọn của máy sau khi mở xong");
            }
            finally
            {
                window.Close();
            }
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Cửa sổ rộng: hình dạng 36 px dựng sẵn nhưng không vẽ — đổi bề rộng không được dựng lại rail.</summary>
        [UnityTest]
        public IEnumerator WideWindow_NarrowShapeExistsButHidden()
        {
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();
            VisualElement root = _scope.Window.rootVisualElement;

            Assert.IsFalse(_scope.Window.HubRoot.ClassListContains(LiveOpsHubClassNames.Narrow));
            Assert.AreEqual(196f, root.Q(LiveOpsHubPaths.ShellElementNames.Rail).worldBound.width, Tolerance);
            VisualElement narrowHost = root.Q(LiveOpsHubRail.NarrowElementName);
            Assert.IsNotNull(narrowHost, "ô icon tầng dựng sẵn ở mọi bề rộng");
            Assert.AreEqual(DisplayStyle.None, narrowHost.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, root.Q(LiveOpsHubRail.PinElementName).resolvedStyle.display, "nút ‹› chỉ có nghĩa khi cửa sổ hẹp");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Nút ‹›: ghim mở đưa rail về 196 px dù cửa sổ vẫn hẹp, và ghi EditorPrefs để lần mở sau còn nhớ.</summary>
        [UnityTest]
        public IEnumerator PinButton_PinsRailOpenAndRemembersInEditorPrefs()
        {
            _scope = LiveOpsHubWindowTestScope.Open(width: NarrowWidth, height: NarrowHeight);
            yield return _scope.WaitForLayout();
            LiveOpsHubRail rail = _scope.Window.Rail;
            VisualElement railElement = _scope.Window.rootVisualElement.Q(LiveOpsHubPaths.ShellElementNames.Rail);

            Assert.IsFalse(rail.IsPinnedOpen);
            Assert.AreEqual(LiveOpsHubStrings.ShellRailPinTooltip, rail.PinButton.tooltip);

            rail.TogglePinnedOpen();
            yield return null;
            yield return null;
            Assert.IsTrue(rail.IsPinnedOpen);
            Assert.IsTrue(_scope.Window.HubRoot.ClassListContains(LiveOpsHubClassNames.RailPinnedOpen), "class ghim nằm trên root để thắng --narrow");
            Assert.IsTrue(EditorPrefs.GetBool(LiveOpsHubRail.PinnedOpenPreferenceKey, false), "ghim phải sống qua domain reload");
            Assert.AreEqual(196f, railElement.worldBound.width, Tolerance, "ghim mở = rail đủ 196 px dù cửa sổ 820");

            rail.TogglePinnedOpen();
            yield return null;
            yield return null;
            Assert.IsFalse(rail.IsPinnedOpen);
            Assert.IsFalse(EditorPrefs.GetBool(LiveOpsHubRail.PinnedOpenPreferenceKey, true));
            Assert.AreEqual(36f, railElement.worldBound.width, Tolerance);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Menu tầng: mỗi màn một mục "tên — lý do" ([FD §3.7]), màn đang mở mang dấu tích.</summary>
        [Test]
        public void NarrowRailMenu_ListsSectionsOfStageWithReason()
        {
            LiveOpsHubRailSectionRow blocked = new LiveOpsHubRailSectionRow(LiveOpsHubSections.Ids.Validation, "Kiểm lịch",
                HealthState.Blocked, "2 đợt sẽ bị game bỏ khi đọc lịch", "2 bị bỏ", false);
            LiveOpsHubRailSectionRow quiet = new LiveOpsHubRailSectionRow(LiveOpsHubSections.Ids.Export, "Xuất JSON",
                HealthState.Ok, "JSON cho key liveops_calendar", string.Empty, true);
            LiveOpsHubRailStageRow stage = new LiveOpsHubRailStageRow(PipelineStage.Check, "KIỂM", HealthState.Blocked, "2 bị bỏ",
                HealthState.Blocked, false, string.Empty, false, false, new[] { blocked, quiet });

            IReadOnlyList<LiveOpsHubNarrowRailMenu.Item> items =
                LiveOpsHubNarrowRailMenu.BuildItems(stage, LiveOpsHubSections.Ids.Validation);

            Assert.AreEqual(2, items.Count);
            Assert.AreEqual("Kiểm lịch — 2 bị bỏ", items[0].Text, "rail thu gọn không còn chỗ in lý do nên lý do vào nhãn menu");
            Assert.IsTrue(items[0].IsOn, "màn đang mở mang dấu tích — rail thu gọn không có hàng active để nói 'bạn đang ở đây'");
            Assert.AreEqual("Xuất JSON", items[1].Text, "màn không có lý do thì chỉ còn tên màn, không có dấu — cụt");
            Assert.IsFalse(items[1].IsOn);
        }

        /// <summary>Dấu '/' trong lý do biến mục thành menu con của Unity — phải đổi ký tự trước khi dựng menu.</summary>
        [Test]
        public void NarrowRailMenu_SlashInReason_DoesNotBecomeSubmenu()
        {
            LiveOpsHubRailSectionRow row = new LiveOpsHubRailSectionRow(LiveOpsHubSections.Ids.Validation, "Kiểm lịch",
                HealthState.Warning, string.Empty, "20 giờ/ngày", false);
            string text = LiveOpsHubNarrowRailMenu.TextOf(row);
            Assert.IsFalse(text.Contains("/"), "còn '/' là Unity tách thành hai tầng menu: " + text);
            StringAssert.Contains("20 giờ", text);
        }
    }
}
