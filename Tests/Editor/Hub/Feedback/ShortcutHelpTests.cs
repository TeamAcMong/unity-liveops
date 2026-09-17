using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Popover "Hiện hướng dẫn phím tắt" (G-OPT-SHORTCUTHELP, W6 — P1-lùi; ma trận 9.5 ảnh <c>ho-shortcut-help</c>). Bốn điều
    /// phải giữ:
    /// <list type="number">
    /// <item><b>Không sót lệnh nào</b> — nhóm thứ nhất duyệt <see cref="ShortcutManager"/>, nên một lệnh mới đăng ký mà quên
    /// bảng hằng vẫn hiện ra.</item>
    /// <item><b>Nói ra họ phím không ai khác nói</b> — phím một ký tự của timeline không nằm trong
    /// <see cref="ShortcutManager"/> (SP-7b), nên cửa sổ Shortcuts của Unity không bao giờ kể chúng.</item>
    /// <item><b>Nút mở cửa sổ Shortcuts chạy thật</b>, và hỏng thì in lý do thành CHỮ cạnh nút (SP-3) thay vì thành nút chết.</item>
    /// <item><b>Bố cục không nuốt hàng nút</b> — bảng phím dài hơn cửa sổ popover nên phần cuộn phải là phần co lại.</item>
    /// </list>
    /// </summary>
    [TestFixture]
    public sealed class ShortcutHelpTests
    {
        private const string HostElementName = "shortcut-help-popover-host";
        private static readonly string[] SampleSectionTitles = { "Tổng quan", "Loại event", "Lịch", "Luật lặp", "Kiểm lịch", "Xuất JSON" };

        /// <summary>Id hub không có trong bảng hằng — dựng trạng thái "bản sau thêm lệnh mà quên câu".</summary>
        private const string UnknownShortcutName = "Some Future Command";

        /// <summary>Hai ký hiệu phím của macOS, dùng để chứng minh chúng KHÔNG lọt sang nền tảng khác.</summary>
        private const string MacShiftGlyph = "⇧";
        private const string MacOptionGlyph = "⌥";

        private LiveOpsHubWindowTestScope _scope;

        [TearDown]
        public void TearDown()
        {
            // Đóng cửa sổ Shortcuts Ở ĐÂY chứ không ở cuối thân test: assert đỏ thì phần sau assert không chạy, và một cửa
            // sổ Shortcuts thật còn mở sẽ cướp focus của các test UI chạy sau (đúng loại đỏ ngẫu nhiên của bài học W5).
            CloseShortcutManagerWindows();
            LiveOpsPopoverContent.CloseCurrent();
            _scope?.Dispose();
            _scope = null;
            LiveOpsHubTestServices.ReleaseAll();
        }

        // ------------------------------------------------------------------------------------------------------ model

        /// <summary>
        /// Nhóm thứ nhất = ĐÚNG tập id "LiveOps Hub/*" mà <see cref="ShortcutManager"/> đang biết, không thiếu không thừa, và
        /// mỗi hàng có câu mô tả thật (không phải khoá catalog trong ngoặc nhọn).
        /// </summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void ShortcutHelp_ListsEveryHubShortcutIdFromShortcutManager()
        {
            List<string> expected = new List<string>();
            foreach (string shortcutId in ShortcutManager.instance.GetAvailableShortcutIds())
            {
                if (shortcutId != null && shortcutId.StartsWith(ShortcutHelpModel.HubShortcutIdPrefix, StringComparison.Ordinal))
                {
                    expected.Add(shortcutId);
                }
            }
            Assert.AreEqual(LiveOpsHubShortcuts.AllShortcutIds.Count, expected.Count,
                "bảng 8.7 và ShortcutManager phải nói cùng một tập id — lệch là hướng dẫn kể thiếu hoặc kể thừa một lệnh");

            IReadOnlyList<ShortcutHelpGroup> groups = ShortcutHelpModel.Build(SampleSectionTitles);
            Assert.AreEqual(2, groups.Count, "đúng hai nhóm: phím của cửa sổ và phím của timeline");
            Assert.AreEqual(LiveOpsHubStrings.ShortcutHelpWindowGroupTitle, groups[0].Title);
            Assert.AreEqual(expected.Count, groups[0].Rows.Count, "mỗi id của hub đúng một hàng");

            for (int index = 0; index < groups[0].Rows.Count; index++)
            {
                ShortcutHelpRow row = groups[0].Rows[index];
                Assert.IsNotEmpty(row.Description, "hàng phím tắt không có câu mô tả thì người đọc chỉ thấy một phím trống nghĩa");
                StringAssert.DoesNotContain(LiveOpsHubStringCatalog.MissingTextOpenMark, row.Description,
                    "câu mô tả rơi về khoá catalog = thiếu chữ cho lệnh này");
                Assert.IsNotEmpty(row.KeyLabel, "phím bị gỡ thì in câu 'chưa gán phím', không bao giờ để ô trống");
                Assert.IsFalse(row.IsUnbound, "hồ sơ phím mặc định gán đủ phím cho mọi lệnh của hub (bảng 8.7)");
            }

            CollectionAssert.AreEquivalent(expected, ShortcutHelpModel.HubShortcutIds());
        }

        /// <summary>
        /// ⌘1…⌘6 đi theo VỊ TRÍ trong registry ([FD §3.1]) nên câu của chúng phải đọc tên màn từ registry đang chạy — chép cứng
        /// sáu cái tên là bảng nói sai ngay khi thứ tự màn đổi.
        /// </summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void ShortcutHelp_GoToSectionRows_UseSectionTitlesFromRegistry()
        {
            IReadOnlyList<ShortcutHelpGroup> groups = ShortcutHelpModel.Build(SampleSectionTitles);
            int found = 0;
            for (int index = 0; index < groups[0].Rows.Count; index++)
            {
                string description = groups[0].Rows[index].Description;
                for (int position = 0; position < SampleSectionTitles.Length; position++)
                {
                    if (description.IndexOf(SampleSectionTitles[position], StringComparison.Ordinal) >= 0) found++;
                }
            }
            Assert.AreEqual(SampleSectionTitles.Length, found, "đủ sáu hàng 'Đi tới màn …' mang đúng tên màn của registry");

            // Danh sách rỗng (cửa sổ chưa dựng xong) không được làm popover bịa tên màn — hàng rơi về số vị trí.
            IReadOnlyList<ShortcutHelpGroup> withoutTitles = ShortcutHelpModel.Build(Array.Empty<string>());
            Assert.AreEqual(groups[0].Rows.Count, withoutTitles[0].Rows.Count, "thiếu tên màn không được làm mất hàng nào");
        }

        /// <summary>
        /// Nhóm thứ hai kể chín phím một ký tự của timeline, và chính chúng là thứ <see cref="ShortcutManager"/> KHÔNG biết
        /// (SP-7b) — không có bảng này thì không chỗ nào nói ra chúng.
        /// </summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void ShortcutHelp_TimelineGroup_ListsSingleKeysThatShortcutManagerDoesNotKnow()
        {
            IReadOnlyList<ShortcutHelpGroup> groups = ShortcutHelpModel.Build(SampleSectionTitles);
            ShortcutHelpGroup timeline = groups[1];
            Assert.AreEqual(LiveOpsHubStrings.ShortcutHelpTimelineGroupTitle, timeline.Title);
            // Chín nhánh phím của LiveOpsTimelineElement.OnKeyDown, trừ nhánh KeyCode.Menu trên macOS: bàn phím Apple không có
            // phím Menu nên hàng đó là một lời hứa suông đúng trên chính máy chụp ảnh.
            bool isMacEditor = Application.platform == RuntimePlatform.OSXEditor;
            Assert.AreEqual(isMacEditor ? 8 : 9, timeline.Rows.Count, "số hàng timeline phải theo nền tảng đang chạy");

            string contextMenuDescription = LiveOpsHubStrings.ShortcutHelpTimelineContextMenu;
            bool hasContextMenuRow = false;
            for (int index = 0; index < timeline.Rows.Count; index++)
            {
                if (string.Equals(timeline.Rows[index].Description, contextMenuDescription, StringComparison.Ordinal)) hasContextMenuRow = true;
            }
            Assert.AreEqual(!isMacEditor, hasContextMenuRow,
                "hàng 'phím Menu' chỉ có ngoài macOS — in nó trên macOS là hứa một phím người đọc không bấm được");

            for (int index = 0; index < timeline.Rows.Count; index++)
            {
                Assert.IsNotEmpty(timeline.Rows[index].Description);
                Assert.IsNotEmpty(timeline.Rows[index].KeyLabel);
                Assert.IsFalse(timeline.Rows[index].IsUnbound, "phím của timeline là hằng trong code, không bao giờ 'chưa gán'");
            }

            // Vế "ShortcutManager KHÔNG biết chúng" do HubShortcutTests giữ (SingleKeyTimelineKeys_NoGlobalConflict +
            // SingleKeyT_NotRegisteredInShortcutManager duyệt keyCode của MỌI binding hub). Lặp lại ở đây bằng cách dò id
            // "LiveOps Hub/A" / "LiveOps Hub/Esc" là một assert xanh vĩnh viễn: hai id đó chưa bao giờ tồn tại.
        }

        /// <summary>
        /// Ba hàng ⇧ / ⌥ / ⌥⇧ phải đi CÙNG nhánh nền tảng với phím lệnh: trên macOS in ký hiệu, ngoài macOS in chữ
        /// ("Shift" / "Alt") — ghi cứng ký hiệu Mac thì người dùng Windows/Linux đọc "⌥ ← →" và không biết bấm phím nào.
        /// </summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void ShortcutHelp_TimelineModifierKeys_FollowThePlatform()
        {
            ShortcutHelpGroup timelineGroup = ShortcutHelpModel.Build(SampleSectionTitles)[1];
            string nudgeByDayKey = KeyLabelOf(timelineGroup, LiveOpsHubStrings.ShortcutHelpTimelineNudgeByDay);
            string moveEndKey = KeyLabelOf(timelineGroup, LiveOpsHubStrings.ShortcutHelpTimelineMoveEndEdge);
            string moveStartKey = KeyLabelOf(timelineGroup, LiveOpsHubStrings.ShortcutHelpTimelineMoveStartEdge);

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                StringAssert.Contains(LiveOpsHubStrings.ShortcutHelpShiftKeyMac, nudgeByDayKey);
                StringAssert.Contains(LiveOpsHubStrings.ShortcutHelpOptionKeyMac, moveEndKey);
                StringAssert.Contains(LiveOpsHubStrings.ShortcutHelpOptionKeyMac, moveStartKey);
                StringAssert.Contains(LiveOpsHubStrings.ShortcutHelpShiftKeyMac, moveStartKey);
                return;
            }

            StringAssert.Contains(LiveOpsHubStrings.KeyLabelShift, nudgeByDayKey);
            StringAssert.Contains(LiveOpsHubStrings.KeyLabelOption, moveEndKey);
            StringAssert.Contains(LiveOpsHubStrings.KeyLabelOption, moveStartKey);
            StringAssert.Contains(LiveOpsHubStrings.KeyLabelShift, moveStartKey);
            StringAssert.DoesNotContain(MacOptionGlyph, moveEndKey, "ngoài macOS không được in ký hiệu phím của Mac");
            StringAssert.DoesNotContain(MacShiftGlyph, nudgeByDayKey, "ngoài macOS không được in ký hiệu phím của Mac");
        }

        /// <summary>
        /// Nhánh dự phòng của <see cref="ShortcutHelpModel.DescriptionOf"/>: một lệnh mới đăng ký mà quên câu vẫn phải HIỆN RA
        /// (in phần sau tiền tố id) chứ không biến mất khỏi hướng dẫn. Không gọi thẳng được thì nhánh này là code chết —
        /// hồ sơ phím thật không bao giờ có id lạ, chính test <see cref="ShortcutHelp_ListsEveryHubShortcutIdFromShortcutManager"/>
        /// cấm trạng thái đó.
        /// </summary>
        [Test]
        [Category(LiveOpsHubTestCategories.Logic)]
        public void ShortcutHelp_UnknownShortcutId_FallsBackToTheIdSuffix()
        {
            string description = ShortcutHelpModel.DescriptionOf(ShortcutHelpModel.HubShortcutIdPrefix + UnknownShortcutName,
                SampleSectionTitles);
            Assert.AreEqual(UnknownShortcutName, description,
                "id lạ phải in phần sau tiền tố — hàng vẫn còn, và chữ ASCII lộ ra là lời nhắc thêm câu");
        }

        /// <summary>Nhãn phím của hàng mang đúng câu mô tả <paramref name="description"/>; hàng không có thì test đỏ ngay.</summary>
        private static string KeyLabelOf(ShortcutHelpGroup group, string description)
        {
            for (int index = 0; index < group.Rows.Count; index++)
            {
                if (string.Equals(group.Rows[index].Description, description, StringComparison.Ordinal)) return group.Rows[index].KeyLabel;
            }
            Assert.Fail("không có hàng nào mang câu mô tả này: " + description);
            return string.Empty;
        }

        // ------------------------------------------------------------------------------------------------------ view

        /// <summary>Cây popover vẽ đủ mọi hàng của model, hai nút có chữ, và dòng lý do ẩn cho tới lúc thật sự hỏng.</summary>
        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator ShortcutHelp_Popover_ShowsEveryRowAndBothButtons()
        {
            ShortcutHelpPopover popover = new ShortcutHelpPopover(SampleSectionTitles, null, () => true);
            VisualElement built = popover.BuildForTest();
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();
            _scope.Window.SectionBody.Add(built);
            yield return null;

            int expectedRowCount = 0;
            for (int index = 0; index < popover.Groups.Count; index++) expectedRowCount += popover.Groups[index].Rows.Count;
            List<VisualElement> rows = built.Query<VisualElement>(className: LiveOpsHubClassNames.ShortcutHelpRow).ToList();
            Assert.AreEqual(expectedRowCount, rows.Count, "mỗi hàng của model đúng một hàng trên cây");

            Label header = built.Q<Label>(LiveOpsHubPaths.ShortcutHelpElementNames.Header);
            Assert.AreEqual(LiveOpsHubStrings.ShortcutHelpPopoverTitle, header.text);
            Label note = built.Q<Label>(LiveOpsHubPaths.ShortcutHelpElementNames.Note);
            Assert.AreEqual(LiveOpsHubStrings.ShortcutHelpNote, note.text);
            Assert.AreEqual(LiveOpsHubStrings.ShortcutHelpOpenShortcutManagerButton, popover.OpenShortcutManagerButton.text);
            Assert.AreEqual(LiveOpsHubStrings.ShortcutHelpCloseButton, popover.CloseButton.text);
            Assert.IsTrue(popover.OpenFailedReasonLabel.ClassListContains(LiveOpsHubClassNames.ShortcutHelpHidden),
                "chưa bấm thì chưa có gì hỏng — dòng lý do phải ẩn");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Đường mở cửa sổ Shortcuts đi qua kiểu internal của UnityEditor: hỏng thì lý do phải IN THÀNH CHỮ cạnh nút (SP-3),
        /// và popover Ở LẠI để người dùng còn đọc được bảng phím.
        /// </summary>
        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator ShortcutHelp_OpenShortcutManagerFails_ReasonIsWrittenBesideButton()
        {
            ShortcutHelpPopover popover = new ShortcutHelpPopover(SampleSectionTitles, null, () => false);
            VisualElement built = popover.BuildForTest();
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();
            _scope.Window.SectionBody.Add(built);
            yield return null;

            popover.ClickOpenShortcutManagerForTest();
            yield return null;

            Assert.AreEqual(1, popover.OpenShortcutManagerCount);
            Assert.IsFalse(popover.OpenFailedReasonLabel.ClassListContains(LiveOpsHubClassNames.ShortcutHelpHidden),
                "mở hỏng thì dòng lý do phải hiện");
            Assert.AreEqual(LiveOpsHubStrings.ShortcutHelpOpenShortcutManagerFailedReason, popover.OpenFailedReasonLabel.text);
            Assert.AreEqual(0, popover.CloseCount, "popover ở lại: bảng phím vẫn phải đọc được khi không mở được cửa sổ Shortcuts");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Đường THẬT (không seam): nút mở đúng cửa sổ Shortcuts của Unity. SP-7 đã chứng minh
        /// <c>ExecuteMenuItem("Edit/Shortcuts...")</c> trả False trên macOS, nên test này là chỗ duy nhất chứng minh đường
        /// reflection còn chạy ở bản Unity đang kiểm — kiểu internal đổi tên là test đỏ, không phải nút chết im lặng.
        /// </summary>
        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator ShortcutHelp_OpenShortcutManager_OpensWindow()
        {
            Assert.IsNotNull(LiveOpsShortcutManagerWindow.ResolveWindowType(),
                "không tra được kiểu " + LiveOpsShortcutManagerWindow.WindowTypeName + " — bản Unity này đã đổi tên kiểu nội bộ");
            CloseShortcutManagerWindows();
            yield return null;

            ShortcutHelpPopover popover = new ShortcutHelpPopover(SampleSectionTitles, null, null);
            popover.BuildForTest();
            popover.ClickOpenShortcutManagerForTest();
            yield return null;

            Assert.IsTrue(LiveOpsShortcutManagerWindow.IsOpen(), "nút phải mở được cửa sổ Shortcuts thật");
            Assert.IsTrue(popover.OpenFailedReasonLabel.ClassListContains(LiveOpsHubClassNames.ShortcutHelpHidden),
                "mở được thì không in lý do hỏng");
            // Không đóng ở đây: [TearDown] đóng, và nó chạy cả khi hai assert trên đỏ.
        }

        /// <summary>Mục menu ⋮ "Hiện hướng dẫn phím tắt" mở đúng popover này — menu là lối vào DUY NHẤT của nó.</summary>
        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator ShortcutHelp_MenuItem_OpensPopover()
        {
            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();

            int invoked = 0;
            foreach (LiveOpsHubWindow.OverflowMenuEntry entry in _scope.Window.BuildOverflowMenuEntries())
            {
                if (!string.Equals(entry.Text, LiveOpsHubStrings.ShortcutHelpMenuItem, StringComparison.Ordinal)) continue;
                Assert.IsTrue(entry.IsEnabled, "mục bảng phím luôn bấm được — nó không phụ thuộc lịch đang mở");
                entry.Invoke();
                invoked++;
            }
            Assert.AreEqual(1, invoked, "đúng một mục menu mở bảng phím");
            yield return null;

            Assert.IsInstanceOf<ShortcutHelpPopover>(LiveOpsPopoverContent.Current, "menu ⋮ phải mở đúng popover phím tắt");
            LiveOpsPopoverContent.CloseCurrent();
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// Bảng phím dài hơn cửa sổ popover: phần cuộn là phần co lại, còn ghi chú và hàng nút phải nằm TRỌN trong khuôn
        /// 320×420 — popover dán đã trả giá đúng chỗ này ở W5 (ô nở theo nội dung đẩy hai nút ra ngoài).
        /// </summary>
        [UnityTest]
        [Category(LiveOpsHubTestCategories.UI)]
        public IEnumerator ShortcutHelp_Layout_FooterStaysInsidePopoverWindow()
        {
            ShortcutHelpPopover popover = new ShortcutHelpPopover(SampleSectionTitles, null, () => true);
            Vector2 popoverSize = popover.GetWindowSize();
            VisualElement host = new VisualElement { name = HostElementName };
            host.style.width = popoverSize.x;
            host.style.height = popoverSize.y;
            host.style.overflow = Overflow.Hidden;
            host.Add(popover.BuildForTest());

            _scope = LiveOpsHubWindowTestScope.Open();
            yield return _scope.WaitForLayout();
            _scope.Window.SectionBody.Add(host);
            yield return LiveOpsHubWindowTestScope.WaitForLayout(host);
            yield return null;

            VisualElement body = host.Q(LiveOpsHubPaths.ShortcutHelpElementNames.Body);
            VisualElement footer = host.Q(LiveOpsHubPaths.ShortcutHelpElementNames.Footer);
            VisualElement scroll = host.Q(LiveOpsHubPaths.ShortcutHelpElementNames.Scroll);
            Assert.AreEqual(popoverSize.x, host.worldBound.width, 0.5f, "khuôn popover rộng đúng 320px ([FD §7])");
            Assert.LessOrEqual(footer.worldBound.yMax, body.worldBound.yMax + 0.5f,
                "hàng nút phải nằm trong thân popover — rơi ra ngoài là không bấm được nút nào");
            Assert.Greater(scroll.worldBound.height, 0f, "phần cuộn phải còn chỗ để vẽ hàng phím");
            Assert.Less(scroll.worldBound.height, popoverSize.y,
                "phần cuộn là phần CO LẠI: cao bằng cả cửa sổ nghĩa là nó đang đẩy ghi chú và hàng nút ra ngoài");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Đóng mọi cửa sổ Shortcuts đang mở — test sau không được thừa hưởng cửa sổ của test trước.</summary>
        private static void CloseShortcutManagerWindows()
        {
            Type windowType = LiveOpsShortcutManagerWindow.ResolveWindowType();
            if (windowType == null) return;
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(windowType);
            for (int index = 0; index < windows.Length; index++)
            {
                EditorWindow window = windows[index] as EditorWindow;
                if (window != null) window.Close();
            }
        }
    }
}
