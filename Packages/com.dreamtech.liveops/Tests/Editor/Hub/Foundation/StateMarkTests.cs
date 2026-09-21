using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class StateMarkTests
    {
        private const int MaximumLayoutFrames = 60;
        private const string LayoutTimeoutMessage = "dấu không có layout sau 60 khung — test UI phải chạy không -nographics";
        private const string AttributesUxmlPath = "Packages/com.dreamtech.liveops/Tests/Editor/Hub/Foundation/Uxml/StateMarkAttributes.uxml";

        private StateMarkProbeWindow _window;

        [TearDown]
        public void TearDown()
        {
            if (_window != null) _window.Close();
            _window = null;
        }

        [UnityTest]
        public IEnumerator StateMark_NotMeasured_HasVisibleBorder()
        {
            VisualElement darkRoot = CreateHubRoot(false);
            VisualElement lightRoot = CreateHubRoot(true);
            LiveOpsStateMark darkMark = AddMark(darkRoot, HealthState.NotMeasured, LiveOpsStateMark.MarkSize.Regular);
            LiveOpsStateMark lightMark = AddMark(lightRoot, HealthState.NotMeasured, LiveOpsStateMark.MarkSize.Regular);
            LiveOpsStateMark smallMark = AddMark(darkRoot, HealthState.NotMeasured, LiveOpsStateMark.MarkSize.Small);
            int frames = 0;
            while (!AllHaveLayout(new VisualElement[] { darkMark, lightMark, smallMark }))
            {
                if (++frames > MaximumLayoutFrames) Assert.Fail(LayoutTimeoutMessage);
                yield return null;
            }

            AssertVisibleRing(darkMark, "root thường");
            AssertVisibleRing(lightMark, "root liveops-hub--skin-light");
            AssertVisibleRing(smallMark, "bản 7px");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator StateMark_Blocked_HasBarChild()
        {
            VisualElement root = CreateHubRoot(false);
            LiveOpsStateMark blocked = AddMark(root, HealthState.Blocked, LiveOpsStateMark.MarkSize.Small);
            LiveOpsStateMark ok = AddMark(root, HealthState.Ok, LiveOpsStateMark.MarkSize.Regular);
            int frames = 0;
            while (!AllHaveLayout(new VisualElement[] { blocked, ok }))
            {
                if (++frames > MaximumLayoutFrames) Assert.Fail(LayoutTimeoutMessage);
                yield return null;
            }

            VisualElement blockedBar = blocked.Q(className: LiveOpsHubClassNames.StateMarkBar);
            Assert.IsNotNull(blockedBar, "vạch cắt phải là con liveops-hub-state-mark__bar — Blocked không có vạch thì chỉ khác Ok bằng màu");
            Assert.AreEqual(DisplayStyle.Flex, blockedBar.resolvedStyle.display);
            Assert.Greater(blockedBar.resolvedStyle.width, 0f);
            Assert.GreaterOrEqual(blocked.resolvedStyle.width, 8f, "Blocked luôn ≥ 8px kể cả bản nhỏ để vạch cắt còn đọc được");

            VisualElement okBar = ok.Q(className: LiveOpsHubClassNames.StateMarkBar);
            Assert.IsNotNull(okBar, "con vạch cắt luôn tồn tại (ẩn bằng USS) để view tái dùng không thêm/bớt con");
            Assert.AreEqual(DisplayStyle.None, okBar.resolvedStyle.display);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator StateMark_InRow_NeverStretchesToEllipse()
        {
            VisualElement root = CreateHubRoot(false);
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.height = 22f;
            row.style.alignItems = Align.Stretch;
            root.Add(row);
            HealthState[] states = { HealthState.Ok, HealthState.Warning, HealthState.Blocked, HealthState.NotMeasured };
            LiveOpsStateMark[] marks = new LiveOpsStateMark[states.Length];
            for (int index = 0; index < states.Length; index++)
            {
                marks[index] = new LiveOpsStateMark();
                marks[index].SetHealth(states[index]);
                row.Add(marks[index]);
                row.Add(new Label("weekly-pass-35 kéo dài một hàng rất dài để flex co giãn"));
            }
            int frames = 0;
            while (!AllHaveLayout(marks))
            {
                if (++frames > MaximumLayoutFrames) Assert.Fail(LayoutTimeoutMessage);
                yield return null;
            }

            for (int index = 0; index < marks.Length; index++)
            {
                Assert.AreEqual(marks[index].resolvedStyle.height, marks[index].resolvedStyle.width, 0.01f,
                    states[index] + ": dấu trong hàng 22px bị kéo méo — tròn thành elip thì Blocked/NotMeasured khó phân biệt");
                Assert.Less(marks[index].layout.height, 20f, states[index] + ": dấu không được giãn theo chiều cao hàng");
            }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void StateMark_UxmlAttributesRead()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AttributesUxmlPath);
            Assert.IsNotNull(tree, "thiếu " + AttributesUxmlPath);
            VisualElement container = new VisualElement();
            tree.CloneTree(container);

            LiveOpsStateMark phaseMark = container.Q<LiveOpsStateMark>("phase-large-blocked");
            Assert.IsNotNull(phaseMark, "thẻ liveops:LiveOpsStateMark không dựng được — đăng ký dual-path hỏng ở bản Unity này");
            Assert.AreEqual(LiveOpsStateMark.MarkKind.Phase, phaseMark.Kind, "kind=\"phase\" không đọc được");
            Assert.AreEqual(LiveOpsStateMark.MarkSize.Large, phaseMark.Size, "size=\"large\" không đọc được");
            Assert.AreEqual("blocked", phaseMark.Health, "health=\"blocked\" không đọc được (hoặc bị kind đè)");
            Assert.IsTrue(phaseMark.ClassListContains(LiveOpsHubClassNames.StateMarkLarge));
            Assert.IsFalse(phaseMark.ClassListContains(LiveOpsHubClassNames.StateMarkBlocked), "Kind=Phase không mang class họ sức khoẻ");

            LiveOpsStateMark healthMark = container.Q<LiveOpsStateMark>("health-small-warning");
            Assert.IsNotNull(healthMark);
            Assert.AreEqual(LiveOpsStateMark.MarkKind.Health, healthMark.Kind);
            Assert.AreEqual(LiveOpsStateMark.MarkSize.Small, healthMark.Size);
            Assert.IsTrue(healthMark.ClassListContains(LiveOpsHubClassNames.StateMarkWarning));
            Assert.IsTrue(healthMark.ClassListContains(LiveOpsHubClassNames.StateMarkSmall));

            LiveOpsStateMark defaultMark = container.Q<LiveOpsStateMark>("defaults");
            Assert.IsNotNull(defaultMark);
            Assert.AreEqual(LiveOpsStateMark.MarkSize.Regular, defaultMark.Size, "size vắng mặt phải là Regular, không phải default(enum) = 0");
            Assert.AreEqual(HealthState.NotMeasured, defaultMark.HealthValue, "dấu chưa đặt trạng thái không bao giờ được trông như Ok");
            Assert.IsNotNull(defaultMark.Q(className: LiveOpsHubClassNames.StateMarkBar));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void StateMark_SetPhase_SwitchesFamily()
        {
            LiveOpsStateMark mark = new LiveOpsStateMark();
            mark.SetHealth(HealthState.Blocked);
            mark.SetPhase(LiveEventPhase.Active, false);

            Assert.AreEqual(LiveOpsStateMark.MarkKind.Phase, mark.Kind);
            Assert.IsTrue(mark.ClassListContains(LiveOpsHubClassNames.PhaseRunning));
            Assert.IsFalse(mark.ClassListContains(LiveOpsHubClassNames.StateMarkBlocked), "giai đoạn và sức khoẻ không bao giờ cùng hiện trên một dấu");

            mark.SetHealth(HealthState.Warning);
            Assert.IsFalse(mark.ClassListContains(LiveOpsHubClassNames.PhaseRunning));
            Assert.IsTrue(mark.ClassListContains(LiveOpsHubClassNames.StateMarkWarning));
        }

        private VisualElement CreateHubRoot(bool lightSkin)
        {
            if (_window == null)
            {
                _window = ScriptableObject.CreateInstance<StateMarkProbeWindow>();
                _window.Show();
                _window.position = new Rect(0f, 0f, 480f, 320f);
            }
            VisualElement root = new VisualElement();
            root.AddToClassList(LiveOpsHubClassNames.Root);
            root.EnableInClassList(LiveOpsHubClassNames.SkinLight, lightSkin);
            StyleSheet theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(LiveOpsHubPaths.ThemeUss);
            StyleSheet components = AssetDatabase.LoadAssetAtPath<StyleSheet>(LiveOpsHubPaths.ComponentsUss);
            Assert.IsNotNull(theme, "thiếu " + LiveOpsHubPaths.ThemeUss);
            Assert.IsNotNull(components, "thiếu " + LiveOpsHubPaths.ComponentsUss);
            root.styleSheets.Add(theme);
            root.styleSheets.Add(components);
            _window.rootVisualElement.Add(root);
            return root;
        }

        private static LiveOpsStateMark AddMark(VisualElement parent, HealthState state, LiveOpsStateMark.MarkSize size)
        {
            LiveOpsStateMark mark = new LiveOpsStateMark { Size = size };
            mark.SetHealth(state);
            parent.Add(mark);
            return mark;
        }

        private static bool AllHaveLayout(VisualElement[] elements)
        {
            foreach (VisualElement element in elements)
            {
                if (element.panel == null || float.IsNaN(element.layout.width) || element.layout.width <= 0f) return false;
            }
            return true;
        }

        private static void AssertVisibleRing(LiveOpsStateMark mark, string context)
        {
            IResolvedStyle resolved = mark.resolvedStyle;
            Assert.GreaterOrEqual(resolved.width, 7f, context + ": NotMeasured hẹp hơn 7px thì vòng rỗng trông như chấm Ok");
            Assert.AreEqual(2f, resolved.borderLeftWidth, 0.01f, context + ": NotMeasured phải có viền 2px — không viền là trông như Ok");
            Assert.AreEqual(2f, resolved.borderTopWidth, 0.01f, context);
            Assert.Greater(resolved.borderLeftColor.a, 0f, context + ": viền trong suốt = vòng rỗng biến mất (token quiet không tới được dấu)");
        }

        // Cửa sổ trống chỉ để có panel thật cho layout; lồng trong fixture vì thư mục test Foundation không có file hỗ trợ
        // riêng (LiveOpsHubWindowTestScope là của G-SHELL, W2).
        private sealed class StateMarkProbeWindow : EditorWindow
        {
        }
    }
}
