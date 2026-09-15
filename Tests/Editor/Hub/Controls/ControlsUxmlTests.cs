using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Đăng ký dual-path của control dùng chung (SP-1): MỘT file UXML mỗi control đọc ra cùng giá trị ở 2022.3 (UxmlFactory/UxmlTraits)
    /// và 6000.6 ([UxmlAttribute], tên kebab-case sinh từ property) — cổng đợt chạy fixture này ở cả hai bản. Kèm hai nhánh
    /// <c>#if</c> theo bản của thư mục Controls: sắp bảng (<see cref="LiveOpsTableSorting"/>) và cắt giữa bằng USS (SP-6).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class ControlsUxmlTests
    {
        private const string UxmlDirectory = "Packages/com.dreamtech.liveops/Tests/Editor/Hub/Controls/Uxml/";
        private const string UtcFieldUxmlPath = UxmlDirectory + "UtcDateTimeField.uxml";
        private const string CycleBarUxmlPath = UxmlDirectory + "RuleCycleBar.uxml";
        private const string TabStripUxmlPath = UxmlDirectory + "TabStrip.uxml";
        private const string LongIdentifier = "star-tournament-2026-10-bonus-round";

        private ControlsTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            _panel?.Dispose();
            _panel = null;
        }

        [Test]
        public void Controls_UxmlReadOnBothVersions()
        {
            VisualElement utcContainer = CloneUxml(UtcFieldUxmlPath);
            LiveOpsUtcDateTimeField configured = utcContainer.Q<LiveOpsUtcDateTimeField>("start-without-device-line");
            Assert.IsNotNull(configured, "thẻ liveops:LiveOpsUtcDateTimeField không dựng được — đăng ký dual-path hỏng ở bản Unity này");
            Assert.IsFalse(configured.ShowDeviceTimeLine, "show-device-time-line=\"false\" không đọc được");
            Assert.AreEqual("Start", configured.label, "label của BaseField phải đọc được ở cả hai bản");
            Assert.AreEqual(string.Empty, configured.RawDateText, "ô dựng từ UXML chưa có giá trị — giá trị luôn đặt từ C#");
            LiveOpsUtcDateTimeField defaults = utcContainer.Q<LiveOpsUtcDateTimeField>("defaults");
            Assert.IsNotNull(defaults);
            Assert.IsTrue(defaults.ShowDeviceTimeLine, "thuộc tính vắng mặt phải giữ mặc định bật, không phải default(bool)");
            Assert.IsFalse(defaults.HasParseError, "ô mới dựng chưa ai gõ thì không được hiện lỗi");

            VisualElement cycleContainer = CloneUxml(CycleBarUxmlPath);
            LiveOpsRuleCycleBar overflowing = cycleContainer.Q<LiveOpsRuleCycleBar>("sky-race-overflow");
            Assert.IsNotNull(overflowing, "thẻ liveops:LiveOpsRuleCycleBar không dựng được");
            Assert.AreEqual(24, overflowing.PeriodHours, "period-hours=\"24\" không đọc được");
            Assert.AreEqual(30, overflowing.ActiveHours, "active-hours=\"30\" không đọc được");
            Assert.IsTrue(overflowing.IsOverflowing);
            LiveOpsRuleCycleBar weekly = cycleContainer.Q<LiveOpsRuleCycleBar>("weekly-pass");
            Assert.IsNotNull(weekly);
            Assert.AreEqual(168, weekly.PeriodHours);
            Assert.AreEqual(168, weekly.ActiveHours);
            Assert.IsFalse(weekly.IsOverflowing);

            VisualElement tabContainer = CloneUxml(TabStripUxmlPath);
            LiveOpsTabStrip zoom = tabContainer.Q<LiveOpsTabStrip>("zoom");
            Assert.IsNotNull(zoom, "thẻ liveops:LiveOpsTabStrip không dựng được");
            Assert.AreEqual("Day|3 weeks|Month", zoom.Choices, "choices không đọc được");
            Assert.AreEqual(3, zoom.ChoiceCount);
            Assert.AreEqual("3 weeks", zoom.TabAt(1).text);
            Assert.AreEqual(0, zoom.SelectedIndex, "dải tab dựng từ UXML phải chọn sẵn tab đầu — không bao giờ rỗng");
            LiveOpsTabStrip empty = tabContainer.Q<LiveOpsTabStrip>("empty");
            Assert.IsNotNull(empty);
            Assert.AreEqual(0, empty.ChoiceCount);
            Assert.AreEqual(-1, empty.SelectedIndex);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TableSorting_EnableCustomSorting_UsesVersionProperty()
        {
            MultiColumnListView table = new MultiColumnListView();
            Assert.IsFalse(LiveOpsTableSorting.IsCustomSortingEnabled(table), "bảng mới không tự bật sắp");
            LiveOpsTableSorting.EnableCustomSorting(table);
            Assert.IsTrue(LiveOpsTableSorting.IsCustomSortingEnabled(table));
            Assert.Throws<ArgumentNullException>(() => LiveOpsTableSorting.EnableCustomSorting(null));
        }

        [UnityTest]
        public IEnumerator ControlsUss_ElideMiddle_NarrowLabelIsElidedAtMiddle()
        {
            _panel = ControlsTestPanel.Open();
            VisualElement root = _panel.CreateRoot(false);
            Label standalone = CreateElidedLabel(LongIdentifier);
            root.Add(standalone);
            ListView list = new ListView(new[] { LongIdentifier }, 16f, () => CreateElidedLabel(string.Empty),
                (element, index) => ((Label)element).text = LongIdentifier);
            list.style.height = 40f;
            list.style.width = 80f;
            root.Add(list);
            yield return ControlsTestPanel.WaitForLayout(standalone, list);
            Label rowLabel = null;
            for (int frame = 0; frame < ControlsTestPanel.MaximumLayoutFrames && rowLabel == null; frame++)
            {
                rowLabel = list.Q<Label>(className: LiveOpsHubClassNames.TextElideMiddle);
                if (rowLabel == null || !ControlsTestPanel.HasLayout(rowLabel))
                {
                    rowLabel = null;
                    yield return null;
                }
            }
            Assert.IsNotNull(rowLabel, "hàng ListView không dựng Label sau 60 khung");
            yield return null;

            AssertElidedAtMiddle(standalone, "Label đứng riêng");
            AssertElidedAtMiddle(rowLabel, "Label trong hàng ListView");
            LogAssert.NoUnexpectedReceived();
        }

        private static Label CreateElidedLabel(string text)
        {
            Label label = new Label(text);
            label.AddToClassList(LiveOpsHubClassNames.TextElideMiddle);
            label.AddToClassList(LiveOpsHubClassNames.Mono);
            label.style.width = 80f;
            return label;
        }

        private static void AssertElidedAtMiddle(Label label, string context)
        {
            Assert.AreEqual(TextOverflowPosition.Middle, label.resolvedStyle.unityTextOverflowPosition,
                context + ": -unity-text-overflow-position: middle không áp — id dài sẽ bị cắt đuôi, mất hậu tố phân biệt đợt");
            Assert.AreEqual(TextOverflow.Ellipsis, label.resolvedStyle.textOverflow, context);
            Assert.IsTrue(label.isElided, context + ": chữ dài hơn 80px phải bị cắt (isElided)");
        }

        private static VisualElement CloneUxml(string path)
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Assert.IsNotNull(tree, "thiếu " + path);
            VisualElement container = new VisualElement();
            tree.CloneTree(container);
            return container;
        }
    }

    /// <summary>
    /// Cửa sổ trống có hai root kiểu hub (thường và <c>liveops-hub--skin-light</c>) nạp theme → components → controls, cho test UI của
    /// thư mục Controls. Vì sao không mở cửa sổ hub: <c>LiveOpsHubSkin</c> của hub tự bật/tắt class skin theo nền thật, nên không dựng
    /// được root sáng trên Editor tối để kiểm token hai skin; control dùng chung cũng không cần khung. Đặt trong file này vì bảng quyền
    /// ghi không cấp file hỗ trợ riêng cho thư mục Controls.
    /// </summary>
    [Category(LiveOpsHubTestCategories.UI)]
    internal sealed class ControlsTestPanel : IDisposable
    {
        internal const int MaximumLayoutFrames = 60;
        private const float DefaultWidth = 900f;
        private const float DefaultHeight = 640f;

        private ControlsProbeWindow _window;

        private ControlsTestPanel(ControlsProbeWindow window)
        {
            _window = window;
        }

        public EditorWindow Window => _window;

        public static ControlsTestPanel Open()
        {
            ControlsProbeWindow window = ScriptableObject.CreateInstance<ControlsProbeWindow>();
            window.Show();
            // Đặt kích thước SAU Show (SP-16) — đặt trước bị kẹp ≈ 401×202.
            window.position = new Rect(0f, 0f, DefaultWidth, DefaultHeight);
            window.rootVisualElement.style.flexDirection = FlexDirection.Row;
            return new ControlsTestPanel(window);
        }

        /// <summary>Root có token hai skin: class <c>liveops-hub-root</c> (+ <c>liveops-hub--skin-light</c>) và ba stylesheet theo thứ tự nạp của khung.</summary>
        public VisualElement CreateRoot(bool lightSkin)
        {
            VisualElement root = new VisualElement();
            root.AddToClassList(LiveOpsHubClassNames.Root);
            root.EnableInClassList(LiveOpsHubClassNames.SkinLight, lightSkin);
            root.style.flexGrow = 1f;
            root.style.flexBasis = 0f;
            foreach (string path in new[] { LiveOpsHubPaths.ThemeUss, LiveOpsHubPaths.ComponentsUss, LiveOpsHubPaths.ControlsUss })
            {
                StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                Assert.IsNotNull(sheet, "thiếu " + path);
                root.styleSheets.Add(sheet);
            }
            _window.rootVisualElement.Add(root);
            return root;
        }

        /// <summary>Chờ mọi element có layout, rồi thêm hai khung cho CustomStyleResolvedEvent và bind của ListView.</summary>
        public static IEnumerator WaitForLayout(params VisualElement[] elements)
        {
            int frames = 0;
            while (!AllHaveLayout(elements))
            {
                if (++frames > MaximumLayoutFrames) Assert.Fail("control không có layout sau 60 khung — test UI phải chạy không -nographics");
                yield return null;
            }
            yield return null;
            yield return null;
        }

        public static bool HasLayout(VisualElement element)
        {
            if (element == null || element.panel == null) return false;
            Rect layout = element.layout;
            return !float.IsNaN(layout.width) && layout.width > 0f && !float.IsNaN(layout.height) && layout.height > 0f;
        }

        public void Dispose()
        {
            if (_window != null) _window.Close();
            _window = null;
        }

        private static bool AllHaveLayout(VisualElement[] elements)
        {
            foreach (VisualElement element in elements)
            {
                if (!HasLayout(element)) return false;
            }
            return true;
        }

        private sealed class ControlsProbeWindow : EditorWindow
        {
        }
    }
}
