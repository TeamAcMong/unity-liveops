using System;
using System.Collections;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Phím tắt toàn hub (8.7). Hai nhóm khẳng định:
    /// <list type="number">
    /// <item><b>Hợp đồng đăng ký</b> (Logic được, nhưng để cùng fixture cho dễ đọc): mọi id của bảng 8.7 có mặt trong
    /// <see cref="ShortcutManager"/>, và <b>không id nào của hub là phím đơn không phím bổ trợ</b> — đó là luật SP-7b, thứ giữ cho
    /// "T" không nhảy về hôm nay lúc người dùng đang gõ "test" vào ô tìm.</item>
    /// <item><b>Hành vi thật trên cửa sổ</b>: ⌘K mở palette và focus ô nhập; gõ chữ vào ô nhập không kích hoạt lệnh nào; ⌘S lưu
    /// lịch và hạ cờ "*".</item>
    /// </list>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class HubShortcutTests
    {
        private const string ShortcutIdPrefix = "LiveOps Hub/";
        private const string UnsavedAssetFileName = "HubShortcutUnsaved.asset";
        private const string MovedEndUtc = "2026-09-22T12:00:00Z";
        private const double LayoutTimeoutSeconds = 5.0;

        /// <summary>Phím đơn mà timeline và bảng tự xử lý bằng <c>KeyDownEvent</c> — không được có trong ShortcutManager (SP-7b).</summary>
        private static readonly KeyCode[] SingleKeyElementCommands =
        {
            KeyCode.T, KeyCode.A, KeyCode.N, KeyCode.F, KeyCode.Minus, KeyCode.Equals,
            KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.DownArrow,
        };

        private LiveOpsHubWindow _window;

        [TearDown]
        public void TearDown()
        {
            if (_window != null) _window.Close();
            _window = null;
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        // ------------------------------------------------------------------------------------------------ hợp đồng đăng ký

        [Test]
        public void HubShortcuts_RegisteredInWindowContext()
        {
            HashSet<string> available = new HashSet<string>(ShortcutManager.instance.GetAvailableShortcutIds(), StringComparer.Ordinal);

            foreach (string shortcutId in LiveOpsHubShortcuts.AllShortcutIds)
            {
                Assert.IsTrue(available.Contains(shortcutId), "thiếu shortcut '" + shortcutId + "' — bảng 8.7 là hợp đồng với nhãn phím của hub");
                Assert.IsNotEmpty(LiveOpsHubKeyLabels.For(shortcutId),
                    "shortcut '" + shortcutId + "' không có phím mặc định — nhãn phím trên chip/toast sẽ rỗng");
            }
            Assert.AreEqual(5 + LiveOpsHubShortcuts.GoToSectionCount, LiveOpsHubShortcuts.AllShortcutIds.Count,
                "bảng 8.7 có 5 lệnh + 6 mục đi tới màn");
        }

        [Test]
        public void SingleKeyTimelineKeys_NoGlobalConflict()
        {
            foreach (string shortcutId in HubShortcutIds())
            {
                foreach (KeyCombination combination in ShortcutManager.instance.GetShortcutBinding(shortcutId).keyCombinationSequence)
                {
                    bool hasModifier = combination.action || combination.shift || combination.alt || combination.control;
                    bool isFunctionKey = combination.keyCode >= KeyCode.F1 && combination.keyCode <= KeyCode.F12;
                    Assert.IsTrue(hasModifier || isFunctionKey,
                        "'" + shortcutId + "' gán phím đơn " + combination.keyCode + " — SP-7b: [Shortcut] của hub chỉ nhận tổ hợp "
                        + "có phím bổ trợ hoặc phím chức năng, phím đơn do element tự xử lý");
                    Assert.IsFalse(Array.IndexOf(SingleKeyElementCommands, combination.keyCode) >= 0 && !hasModifier,
                        "'" + shortcutId + "' cướp phím đơn " + combination.keyCode + " của timeline/bảng");
                }
            }
        }

        [Test]
        public void SingleKeyT_NotRegisteredInShortcutManager()
        {
            foreach (string shortcutId in HubShortcutIds())
            {
                foreach (KeyCombination combination in ShortcutManager.instance.GetShortcutBinding(shortcutId).keyCombinationSequence)
                {
                    Assert.AreNotEqual(KeyCode.T, combination.keyCode,
                        "'T' (về hôm nay) phải do LiveOpsTimelineElement xử lý bằng KeyDownEvent, không qua ShortcutManager (SP-7b)");
                }
            }
        }

        // ------------------------------------------------------------------------------------------------ hành vi trên cửa sổ

        [UnityTest]
        public IEnumerator CtrlK_OpensPalette_FocusesQuery()
        {
            yield return OpenWindow(LiveOpsHubTestServices.DesignSampleScenario);
            Assert.IsFalse(_window.Palette.IsOpen);

            _window.TogglePaletteOrUnitySearch();

            Assert.IsTrue(_window.Palette.IsOpen, "⌘K khi palette đóng thì mở palette");
            yield return WaitUntilFocused(_window.Palette.Field);
            Assert.AreSame(_window.Palette.Field, FocusedField(_window),
                "ô nhập phải nhận focus ở khung sau khi mở — không thì người dùng gõ vào hư không");
            Assert.IsNotEmpty(_window.Palette.Matches, "palette mở trống vẫn liệt kê 6 màn");
        }

        [UnityTest]
        public IEnumerator FocusedTextField_SendT_And_Backspace_NothingRuns()
        {
            yield return OpenWindow(LiveOpsHubTestServices.DesignSampleScenario);
            _window.OpenPalette();
            yield return WaitUntilFocused(_window.Palette.Field);
            string sectionBefore = _window.ActiveSectionId;

            // Gõ "t" rồi Backspace ngay trong ô tìm: hai phím này là phím đơn, và đúng ca mà SP-7b sinh ra để chặn.
            SendKey(_window.Palette.Field, KeyCode.T, 't');
            SendKey(_window.Palette.Field, KeyCode.Backspace, '\b');
            yield return null;

            Assert.IsTrue(_window.Palette.IsOpen, "gõ chữ trong ô tìm không được đóng palette");
            Assert.AreEqual(sectionBefore, _window.ActiveSectionId, "gõ chữ trong ô tìm không được điều hướng");
            Assert.IsFalse(LiveOpsHubShortcuts.IsSingleKeyCommandAllowed(_window.Palette.Field),
                "focus ở ô nhập chữ: mọi handler phím đơn của hub phải bỏ qua");
        }

        [UnityTest]
        public IEnumerator SingleKeyCommandAllowed_OutsideTextField()
        {
            yield return OpenWindow(LiveOpsHubTestServices.DesignSampleScenario);

            Assert.IsTrue(LiveOpsHubShortcuts.IsSingleKeyCommandAllowed(_window.Rail.Element),
                "ngoài ô nhập chữ thì phím đơn là lệnh — nếu không, timeline mất hết phím một ký tự");
        }

        [UnityTest]
        public IEnumerator Save_ClearsUnsavedAndForwardsFileSave()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.Build(
                LiveOpsHubTestServices.CreateBuilder(LiveOpsHubTestServices.CreateClock())
                    .WithCalendarAsset(LiveOpsHubTestServices.CreateAssetFile(UnsavedAssetFileName, LiveOpsDesignSample.Document)));
            yield return OpenWindow(services);
            int saveMenuCalls = 0;
            // ExecuteMenuItem("File/Save") mở hộp lưu scene trong batchmode (Unity log assert) — thay bằng bộ đếm để vẫn khẳng
            // định ⌘S có chuyển tiếp cho Unity sau khi lưu lịch.
            _window.SetSaveMenuCommandForTest(() => saveMenuCalls++);

            FixedLiveEventEntry entry;
            Assert.IsTrue(services.Session.Document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey, out entry));
            services.Session.Apply(new ReplaceFixedEventEdit(new FixedLiveEventEntry(entry.EntryKey, entry.EventId, entry.EventType,
                entry.StartUtcText, MovedEndUtc, entry.ConfigKey)), "Dời kết thúc " + entry.EventId);
            Assert.IsTrue(_window.hasUnsavedChanges, "sửa một đợt là tab có * (8.3)");
            Assert.AreEqual(LiveOpsHubHeaderChipModel.FormUnsaved, ChipForm(services), "chip nháp phải ở dạng (a) cùng lúc với '*'");

            _window.SaveFromShortcut();
            yield return null;

            Assert.IsFalse(_window.hasUnsavedChanges, "⌘S lưu được thì hạ cờ * trên tab");
            Assert.IsFalse(services.Session.HasUnsavedChanges);
            Assert.AreNotEqual(LiveOpsHubHeaderChipModel.FormUnsaved, ChipForm(services), "lưu xong thì chip rời dạng (a)");
            StringAssert.DoesNotContain(LiveOpsHubStrings.ShellChipUnsaved, _window.Header.DraftLeftLabel.text);
            Assert.AreEqual(1, saveMenuCalls,
                "⌘S trong hub phải chuyển tiếp File/Save của Unity — người dùng bấm ⌘S mong lưu cả scene, không chỉ asset lịch");
        }

        // ------------------------------------------------------------------------------------------------ hỗ trợ

        private static IEnumerable<string> HubShortcutIds()
        {
            foreach (string shortcutId in ShortcutManager.instance.GetAvailableShortcutIds())
            {
                if (shortcutId.StartsWith(ShortcutIdPrefix, StringComparison.Ordinal)) yield return shortcutId;
            }
        }

        private static int ChipForm(LiveOpsHubServices services)
        {
            return LiveOpsHubHeaderChipModel.Build(services.Session, services.Format, string.Empty).DraftChipForm;
        }

        private IEnumerator OpenWindow(string scenarioId)
        {
            return OpenWindow(LiveOpsHubTestServices.ForScenario(scenarioId));
        }

        private IEnumerator OpenWindow(LiveOpsHubServices services)
        {
            _window = LiveOpsHubWindow.OpenWithServices(services, null);
            // SP-16: đặt kích thước SAU Show.
            _window.position = new Rect(0, 0, LiveOpsHubWindowTestScope.StandardWidth, LiveOpsHubWindowTestScope.StandardHeight);
            int frames = 0;
            double deadline = EditorApplication.timeSinceStartup + LayoutTimeoutSeconds;
            // V-23: chỉ fail khi quá CẢ 60 khung LẪN 5 giây — máy bận (hai Unity song song) sẽ đỏ giả nếu chỉ đếm khung.
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

        /// <summary>Palette focus ô nhập ở khung SAU khi mở (ô chưa có layout thì Focus không giữ) — chờ đúng điều kiện đó.</summary>
        private static IEnumerator WaitUntilFocused(VisualElement element)
        {
            int frames = 0;
            double deadline = EditorApplication.timeSinceStartup + LayoutTimeoutSeconds;
            while (element.panel == null || element.panel.focusController.focusedElement == null)
            {
                if (++frames > LiveOpsHubWindowTestScope.MaximumLayoutFrames && EditorApplication.timeSinceStartup > deadline)
                {
                    Assert.Fail("ô nhập palette không nhận focus sau 60 khung và 5 giây");
                }
                yield return null;
            }
        }

        private static VisualElement FocusedField(LiveOpsHubWindow window)
        {
            VisualElement focused = window.rootVisualElement.panel.focusController.focusedElement as VisualElement;
            // Focus thật nằm ở ô chữ con của TextField; test hỏi "có trong TextField của palette không", không hỏi element chính xác.
            if (focused == null) return null;
            return focused is TextField ? focused : focused.GetFirstAncestorOfType<TextField>();
        }

        private static void SendKey(VisualElement target, KeyCode keyCode, char character)
        {
            using (KeyDownEvent keyEvent = KeyDownEvent.GetPooled(character, keyCode, EventModifiers.None))
            {
                keyEvent.target = target;
                target.SendEvent(keyEvent);
            }
        }
    }
}
