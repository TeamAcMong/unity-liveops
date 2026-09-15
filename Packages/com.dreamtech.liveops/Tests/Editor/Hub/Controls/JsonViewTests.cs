using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// JSON viewer: chữ hiện đúng byte sẽ đăng kể cả khi giá trị chứa thẻ rich text; lề và dải tổng quan phân biệt bằng hình dạng
    /// ([SD2 §3.6]); chú thích nhận câu dựng sẵn (V-21 D-5).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class JsonViewTests
    {
        private const string TaggedLine = "  \"note\": \"<b>x</b>\",";
        private const string ClosingTagLine = "  \"trap\": \"</noparse><color=red>y</color>\"";
        private const float WidthTolerance = 1f;

        private ControlsTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            _panel?.Dispose();
            _panel = null;
        }

        [UnityTest]
        public IEnumerator JsonView_NoParse_ShowsTagsLiterally()
        {
            _panel = ControlsTestPanel.Open();
            VisualElement root = _panel.CreateRoot(false);
            LiveOpsJsonView view = CreateView(root, 200f);
            view.SetText("{\n" + TaggedLine + "\n" + ClosingTagLine + "\n}", "{" + TaggedLine.Trim() + ClosingTagLine.Trim() + "}");
            yield return ControlsTestPanel.WaitForLayout(view);
            yield return WaitForBoundRows(view, 4);

            Assert.IsTrue(view.Colors.IsResolved, "màu cú pháp phải đọc được từ token khai lại trên .liveops-hub-json-view");
            Label taggedRow = FindCodeLabel(view, "note");
            Assert.IsNotNull(taggedRow, "hàng dòng 2 chưa được bind");
            Assert.IsTrue(taggedRow.enableRichText, "code dùng rich text cho màu cú pháp");
            StringAssert.Contains("<noparse>\"<b>x</b>\"</noparse>", taggedRow.text, "giá trị thô phải nằm trong noparse trước khi ghép thẻ màu");
            StringAssert.Contains("<color=#" + view.Colors.StringHex + ">", taggedRow.text, "chuỗi mang màu string của skin");
            StringAssert.Contains("<color=#" + view.Colors.KeyHex + ">", taggedRow.text, "key mang màu key của skin");

            // Đo chữ đã vẽ: bản rich text của view phải rộng bằng chữ thường không parse; bản không noparse (thẻ bị hiểu) phải hẹp hơn
            // — chứng minh phép đo thật sự thấy thẻ bị nuốt, không phải luôn bằng nhau.
            Label protectedLabel = CreateMeasureLabel(root, taggedRow.text, true);
            Label literalLabel = CreateMeasureLabel(root, TaggedLine, false);
            Label parsedLabel = CreateMeasureLabel(root, TaggedLine, true);
            Label trapLabel = CreateMeasureLabel(root, FindCodeLabel(view, "trap").text, true);
            Label trapLiteral = CreateMeasureLabel(root, ClosingTagLine, false);
            yield return ControlsTestPanel.WaitForLayout(protectedLabel, literalLabel, parsedLabel, trapLabel, trapLiteral);

            Assert.AreEqual(literalLabel.layout.width, protectedLabel.layout.width, WidthTolerance,
                "dòng \"note\": \"<b>x</b>\" phải hiện nguyên văn — thẻ bị hiểu thì người duyệt đọc sai byte sẽ đăng");
            Assert.Less(parsedLabel.layout.width, literalLabel.layout.width - 8f, "đối chứng: không noparse thì <b></b> bị nuốt, dòng hẹp lại");
            Assert.AreEqual(trapLiteral.layout.width, trapLabel.layout.width, WidthTolerance,
                "giá trị chứa sẵn </noparse> không được đóng khối noparse sớm");

            Assert.AreEqual("<noparse>a</</noparse><noparse>noparse>b</noparse>", LiveOpsJsonView.ProtectRawText("a</noparse>b"));
            Assert.AreEqual(string.Empty, LiveOpsJsonView.ProtectRawText(string.Empty));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator JsonView_OverviewStripMarkerShapes()
        {
            _panel = ControlsTestPanel.Open();
            VisualElement root = _panel.CreateRoot(false);
            LiveOpsJsonView view = CreateView(root, 300f);
            const int lineCount = 60;
            view.SetText(BuildLines(lineCount), "{}");
            view.SetAnnotations(new[]
            {
                new LiveOpsJsonLineAnnotation(54, HealthState.Blocked, "không đọc được — đợt bị bỏ"),
                new LiveOpsJsonLineAnnotation(7, HealthState.Warning, "đổi id đợt đang chạy"),
            });
            view.SetLineChanges(new[]
            {
                new LiveOpsJsonLineChange(34, LiveOpsJsonLineChangeKind.Modified),
                new LiveOpsJsonLineChange(36, LiveOpsJsonLineChangeKind.Added),
                new LiveOpsJsonLineChange(37, LiveOpsJsonLineChangeKind.Added),
                new LiveOpsJsonLineChange(38, LiveOpsJsonLineChangeKind.Added),
            });
            yield return ControlsTestPanel.WaitForLayout(view, view.Overview);
            yield return ControlsTestPanel.WaitForLayout(view.OverviewMarkers.ToArrayForTest());

            IReadOnlyList<VisualElement> markers = view.OverviewMarkers;
            VisualElement dropped = SingleMarker(markers, LiveOpsHubClassNames.JsonOverviewMarkerDropped);
            VisualElement warning = SingleMarker(markers, LiveOpsHubClassNames.JsonOverviewMarkerWarning);
            List<VisualElement> changes = MarkersOf(markers, LiveOpsHubClassNames.JsonOverviewMarkerChange);
            Assert.AreEqual(2, changes.Count, "khối + liên tiếp 36–38 chỉ vẽ một vạch ở dòng đầu; dòng ~ 34 một vạch");

            Rect content = view.Overview.contentRect;
            Assert.AreEqual(10f, view.Overview.layout.width, 0.5f, "dải tổng quan rộng 10px");
            // layout của con tính từ mép viền của dải (viền trái 1px) — so với contentRect cùng hệ toạ độ.
            Assert.AreEqual(content.width, dropped.layout.width, 0.5f, "bị bỏ = vạch full bề rộng");
            Assert.AreEqual(content.xMin, dropped.layout.xMin, 0.5f);
            Assert.AreEqual(3f, dropped.layout.height, 0.5f, "bị bỏ cao 3px");
            Assert.AreEqual(content.width / 2f, warning.layout.width, 0.5f, "cảnh báo = nửa bề rộng");
            Assert.AreEqual(content.xMax, warning.layout.xMax, 0.5f, "cảnh báo căn phải");
            Assert.AreEqual(2f, warning.layout.height, 0.5f, "cảnh báo cao 2px");
            foreach (VisualElement change in changes)
            {
                Assert.AreEqual(content.width / 2f, change.layout.width, 0.5f, "thay đổi = nửa bề rộng");
                Assert.AreEqual(content.xMin, change.layout.xMin, 0.5f, "thay đổi căn trái");
                Assert.AreEqual(1f, change.layout.height, 0.5f, "thay đổi cao 1px");
            }
            Assert.AreNotEqual(dropped.resolvedStyle.backgroundColor, warning.resolvedStyle.backgroundColor);

            Assert.AreEqual("dòng 54 · bị bỏ", dropped.tooltip);
            Assert.AreEqual("dòng 7 · cảnh báo", warning.tooltip);
            CollectionAssert.AreEquivalent(new[] { "dòng 34 · thay đổi", "dòng 36 · thay đổi" }, new[] { changes[0].tooltip, changes[1].tooltip });
            float overviewHeight = view.Overview.contentRect.height;
            Assert.AreEqual(overviewHeight * 53f / lineCount, dropped.layout.y, 1f, "vạch đặt theo % dòng trong toàn file");
            Assert.AreEqual(overviewHeight * 6f / lineCount, warning.layout.y, 1f);
            Assert.IsNotNull(view.OverviewViewport.parent, "khung viewport nằm trên dải");

            // Tab "Một dòng": không dải tổng quan, không số dòng; tab và cách xem luôn đồng bộ hai chiều.
            view.IsSingleLine = true;
            yield return null;
            yield return null;
            Assert.AreEqual(DisplayStyle.None, view.Overview.resolvedStyle.display, "một dòng không có dải tổng quan");
            Assert.AreEqual(0, view.OverviewMarkers.Count);
            Assert.AreEqual(1, view.LineCount);
            Assert.AreEqual(LiveOpsJsonView.SingleLineTabIndex, view.ModeTabs.SelectedIndex, "tab phải theo cách xem");
            view.ModeTabs.SelectedIndex = LiveOpsJsonView.FormattedTabIndex;
            Assert.IsFalse(view.IsSingleLine, "bấm tab Đã định dạng đổi lại cách xem");
            Assert.AreEqual(lineCount, view.LineCount);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator JsonView_GutterAndAnnotation_ErrorWinsOverAdded()
        {
            _panel = ControlsTestPanel.Open();
            VisualElement root = _panel.CreateRoot(false);
            LiveOpsJsonView view = CreateView(root, 200f);
            view.SetText("{\n  \"a\": 1,\n  \"b\": true,\n  \"c\": null\n}", "{}");
            view.SetLineChanges(new[]
            {
                new LiveOpsJsonLineChange(2, LiveOpsJsonLineChangeKind.Added),
                new LiveOpsJsonLineChange(3, LiveOpsJsonLineChangeKind.Added),
            });
            view.SetAnnotations(new[]
            {
                new LiveOpsJsonLineAnnotation(3, HealthState.Warning, "đổi id"),
                new LiveOpsJsonLineAnnotation(3, HealthState.Blocked, "chồng 12 giờ"),
                new LiveOpsJsonLineAnnotation(4, HealthState.Ok, "hunt_v1 → mặc định của loại"),
            });
            yield return ControlsTestPanel.WaitForLayout(view);
            yield return WaitForBoundRows(view, 5);

            VisualElement addedRow = RowOf(view, "\"a\"");
            VisualElement brokenRow = RowOf(view, "\"b\"");
            VisualElement neutralRow = RowOf(view, "\"c\"");
            Label addedMark = addedRow.Q<Label>(className: LiveOpsHubClassNames.JsonGutterMark);
            Assert.AreEqual(LiveOpsHubStrings.JsonGutterAdded, addedMark.text);
            Assert.AreEqual(DisplayStyle.Flex, addedMark.resolvedStyle.display);

            Assert.IsTrue(brokenRow.ClassListContains(LiveOpsHubClassNames.JsonLineBlocked), "mức nặng nhất của dòng quyết định tint");
            Assert.IsFalse(brokenRow.ClassListContains(LiveOpsHubClassNames.JsonLineWarning));
            Image brokenIcon = brokenRow.Q<Image>(className: LiveOpsHubClassNames.JsonGutterIcon);
            Assert.AreEqual(DisplayStyle.Flex, brokenIcon.resolvedStyle.display, "dòng lỗi mang icon lỗi 12px");
            Assert.IsNotNull(brokenIcon.image);
            Assert.AreEqual(DisplayStyle.None, brokenRow.Q<Label>(className: LiveOpsHubClassNames.JsonGutterMark).resolvedStyle.display,
                "✕ thắng + (dòng lỗi nằm trong khối thêm mới)");
            Label brokenAnnotation = brokenRow.Q<Label>(className: LiveOpsHubClassNames.JsonAnnotation);
            Assert.AreEqual("đổi id · chồng 12 giờ", brokenAnnotation.text, "chú thích cùng dòng nối bằng \" · \"");
            Assert.IsTrue(brokenAnnotation.ClassListContains(LiveOpsHubClassNames.TextBlocked));

            Label neutralAnnotation = neutralRow.Q<Label>(className: LiveOpsHubClassNames.JsonAnnotation);
            Assert.IsTrue(neutralAnnotation.ClassListContains(LiveOpsHubClassNames.TextQuiet), "chú thích trung tính là chữ quiet");
            Assert.IsFalse(neutralRow.ClassListContains(LiveOpsHubClassNames.JsonLineBlocked));
            Assert.AreEqual(32f, neutralRow.Q<Label>(className: LiveOpsHubClassNames.JsonLineNumber).layout.width, 0.5f, "số dòng 32px");
            Assert.AreEqual(14f, neutralRow.Q(className: LiveOpsHubClassNames.JsonGutter).layout.width, 0.5f, "lề 14px");
            Assert.AreEqual(LiveOpsJsonView.LineHeight, neutralRow.layout.height, 0.5f, "dòng 16px");

            string richNumber = view.RichTextOfLine(2);
            StringAssert.Contains("<color=#" + view.Colors.NumberHex + "><noparse>1</noparse></color>", richNumber);
            StringAssert.Contains("<color=#" + view.Colors.LiteralHex + "><noparse>true</noparse></color>", view.RichTextOfLine(3));
            LogAssert.NoUnexpectedReceived();
        }

        private static LiveOpsJsonView CreateView(VisualElement root, float height)
        {
            LiveOpsJsonView view = new LiveOpsJsonView();
            view.style.height = height;
            view.style.width = 520f;
            view.style.flexGrow = 0f;
            root.Add(view);
            return view;
        }

        private static string BuildLines(int count)
        {
            StringBuilder builder = new StringBuilder();
            for (int index = 1; index <= count; index++)
            {
                if (index > 1) builder.Append('\n');
                builder.Append("  \"line").Append(index).Append("\": ").Append(index).Append(',');
            }
            return builder.ToString();
        }

        private static IEnumerator WaitForBoundRows(LiveOpsJsonView view, int expectedRows)
        {
            for (int frame = 0; frame < ControlsTestPanel.MaximumLayoutFrames; frame++)
            {
                List<Label> labels = view.LineList.Query<Label>(className: LiveOpsHubClassNames.JsonCode).ToList();
                int bound = 0;
                foreach (Label label in labels)
                {
                    if (!string.IsNullOrEmpty(label.text) && ControlsTestPanel.HasLayout(label)) bound++;
                }
                if (bound >= expectedRows) yield break;
                yield return null;
            }
            Assert.Fail("ListView chưa bind đủ " + expectedRows + " dòng sau 60 khung");
        }

        private static Label FindCodeLabel(LiveOpsJsonView view, string fragment)
        {
            foreach (Label label in view.LineList.Query<Label>(className: LiveOpsHubClassNames.JsonCode).ToList())
            {
                if (label.text.IndexOf(fragment, StringComparison.Ordinal) >= 0) return label;
            }
            return null;
        }

        private static VisualElement RowOf(LiveOpsJsonView view, string fragment)
        {
            Label code = FindCodeLabel(view, fragment);
            Assert.IsNotNull(code, "không thấy hàng chứa " + fragment);
            return code.parent;
        }

        private static Label CreateMeasureLabel(VisualElement root, string text, bool richText)
        {
            Label label = new Label(text) { enableRichText = richText };
            label.AddToClassList(LiveOpsHubClassNames.JsonCode);
            label.AddToClassList(LiveOpsHubClassNames.Mono);
            // Không co giãn: bề rộng layout = bề rộng chữ đã sắp (không bị flex kéo hay cắt).
            label.style.flexGrow = 0f;
            label.style.flexShrink = 0f;
            label.style.alignSelf = Align.FlexStart;
            label.style.overflow = Overflow.Visible;
            root.Add(label);
            return label;
        }

        private static VisualElement SingleMarker(IReadOnlyList<VisualElement> markers, string className)
        {
            List<VisualElement> found = MarkersOf(markers, className);
            Assert.AreEqual(1, found.Count, "phải có đúng một vạch " + className);
            return found[0];
        }

        private static List<VisualElement> MarkersOf(IReadOnlyList<VisualElement> markers, string className)
        {
            List<VisualElement> found = new List<VisualElement>();
            foreach (VisualElement marker in markers)
            {
                if (marker.ClassListContains(className)) found.Add(marker);
            }
            return found;
        }
    }

    internal static class JsonViewTestExtensions
    {
        public static VisualElement[] ToArrayForTest(this IReadOnlyList<VisualElement> elements)
        {
            VisualElement[] array = new VisualElement[elements.Count];
            for (int index = 0; index < elements.Count; index++) array[index] = elements[index];
            return array;
        }
    }
}
