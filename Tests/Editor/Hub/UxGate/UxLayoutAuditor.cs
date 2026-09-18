using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Khoá của mỗi loại phát hiện. Đây CŨNG là tên khoá trong JSON chẩn đoán: người dùng giả (worktree G-UX-JOURNEY) ghi đúng
    /// các khoá này, nên báo cáo trước/sau của đợt so được hai nguồn bằng cùng một script mà không phải ánh xạ tên.
    /// </summary>
    internal static class UxLayoutFindingKinds
    {
        internal const string ZeroSizeNamed = "zeroSizeNamed";
        internal const string TextCut = "textCut";
        internal const string ChildOverflow = "childOverflow";
        internal const string ScrollViews = "scrollViews";
        internal const string AbsoluteOverText = "absoluteOverText";
        internal const string SiblingOverlap = "siblingOverlap";
        internal const string NotStretched = "notStretched";
        internal const string MissingElement = "missingElement";
    }

    /// <summary>
    /// Một luật "vùng vẽ phải giãn" (§3.3 mục e): phần tử <see cref="ChildSelector"/> phải chiếm ít nhất
    /// <see cref="MinimumRatio"/> bề rộng (hoặc chiều cao) của <see cref="ParentSelector"/>. Đây là loại lỗi mà đo khung tuyệt đối
    /// của ma trận ảnh KHÔNG bắt được: ở 1920 thân màn cao 900 vẫn "đẹp" trong khi nó chỉ lấp 40% chiều cao có thể dùng.
    /// </summary>
    internal readonly struct UxLayoutStretchRule
    {
        public UxLayoutStretchRule(string childSelector, string parentSelector, bool vertical, float minimumRatio)
        {
            ChildSelector = childSelector;
            ParentSelector = parentSelector;
            Vertical = vertical;
            MinimumRatio = minimumRatio;
        }

        /// <summary>Tên element ("#tên") hoặc class USS — <see cref="UxLayoutAuditor.FindBySelector"/> hiểu cả hai.</summary>
        public string ChildSelector { get; }

        public string ParentSelector { get; }

        /// <summary>true = so chiều cao (thân màn), false = so bề rộng (track thước).</summary>
        public bool Vertical { get; }

        public float MinimumRatio { get; }
    }

    /// <summary>Kết quả một lượt kiểm bố cục: mỗi loại một danh sách câu chẩn đoán, cộng phần đầu đủ để đọc JSON không cần tên file.</summary>
    internal sealed class UxLayoutAuditResult
    {
        public UxLayoutAuditResult(string screenId, UxWindowSize size, LiveOpsHubLanguageId language)
        {
            ScreenId = screenId;
            Size = size;
            Language = language;
        }

        public string ScreenId { get; }
        public UxWindowSize Size { get; }
        public LiveOpsHubLanguageId Language { get; }
        public Rect RootBound { get; set; }

        public List<string> ZeroSizeNamed { get; } = new List<string>();
        public List<string> TextCut { get; } = new List<string>();
        public List<string> ChildOverflow { get; } = new List<string>();

        /// <summary>MỌI ScrollView của màn (chẩn đoán), kể cả cái không có vấn đề — chỉ dòng mang dấu hiệu mới tính là lỗi.</summary>
        public List<string> ScrollViews { get; } = new List<string>();

        /// <summary>
        /// MỌI phần tử có tên mà cao hoặc rộng 0 — CHẨN ĐOÁN, không phải lỗi. Khung hub có sẵn vài chỗ cao 0 đúng thiết kế
        /// (spacer ngang của header, vùng ghi chú rỗng, chỗ cắm outcome khi chưa có outcome). Yêu cầu (a) của §3.3 nói về
        /// "phần tử QUAN TRỌNG theo danh sách của màn" — việc đó là <see cref="MissingElement"/>; danh sách này chỉ để người
        /// soát đọc khi đang truy một màn trống.
        /// </summary>

        public List<string> AbsoluteOverText { get; } = new List<string>();
        public List<string> SiblingOverlap { get; } = new List<string>();
        public List<string> NotStretched { get; } = new List<string>();
        public List<string> MissingElement { get; } = new List<string>();

        /// <summary>Tên file JSON: <c>&lt;màn&gt;-&lt;cỡ&gt;-&lt;ngôn ngữ&gt;</c> đúng như §3.3.</summary>
        public string FileStem =>
            ScreenId + "-" + Size + "-" + (Language == LiveOpsHubLanguageId.Vietnamese ? "vi" : "en");

        /// <summary>Những dòng tính là LỖI (ScrollView chỉ tính khi mang dấu hiệu tràn/cuộn ngang).</summary>
        public List<string> Problems()
        {
            List<string> problems = new List<string>();
            AppendProblems(problems, UxLayoutFindingKinds.MissingElement, MissingElement);
            AppendProblems(problems, UxLayoutFindingKinds.TextCut, TextCut);
            AppendProblems(problems, UxLayoutFindingKinds.ChildOverflow, ChildOverflow);
            AppendProblems(problems, UxLayoutFindingKinds.AbsoluteOverText, AbsoluteOverText);
            AppendProblems(problems, UxLayoutFindingKinds.SiblingOverlap, SiblingOverlap);
            AppendProblems(problems, UxLayoutFindingKinds.NotStretched, NotStretched);
            foreach (string entry in ScrollViews)
            {
                if (UxLayoutAuditor.IsScrollViewProblem(entry)) problems.Add(UxLayoutFindingKinds.ScrollViews + ": " + entry);
            }
            return problems;
        }

        private static void AppendProblems(List<string> problems, string kind, List<string> entries)
        {
            foreach (string entry in entries) problems.Add(kind + ": " + entry);
        }
    }

    /// <summary>
    /// Kiểm bố cục tự động của cổng W8-UX (§3.3): đi hết cây element của một cửa sổ và ghi ra mọi chỗ NGƯỜI DÙNG không dùng được —
    /// phần tử quan trọng cao/rộng 0, chữ bị cắt, con tràn khỏi cha, nội dung vượt khung mà không cuộn được, vùng vẽ không giãn
    /// theo cửa sổ.
    /// <para>
    /// Vì sao cần: 37 lỗi của SCHEDULE-UI-AUDIT và 26 lỗi hành trình đều nhìn thấy được trên ẢNH, nhưng 900 test xanh không bắt
    /// được cái nào — test cũ đo từng khung tuyệt đối theo hình thiết kế ở MỘT cỡ cửa sổ. Ở đây không đo con số thiết kế: chỉ hỏi
    /// "chữ này có đọc được không", "ô này có bấm được không", ở CẢ SÁU cỡ, nên lỗi kiểu "820 màn trắng" không lọt nữa.
    /// </para>
    /// <para>
    /// Chữ bị cắt đo bằng <c>MeasureTextSize</c> + <c>isElided</c> (reflection: cả hai đổi chỗ giữa 2022.3 và 6000.6). Cắt CÓ CHỦ
    /// ĐÍCH khai ở <see cref="UxLayoutAllowList"/> kèm lý do — allow-list là nơi DUY NHẤT được phép im lặng.
    /// </para>
    /// </summary>
    // Category ở đây là dấu cho code-lint (luật test-ui-category) và cho người đọc: lớp trợ giúp này chỉ chạy được khi
    // Unity CÓ đồ hoạ — panel/SendEvent/layout đều vô nghĩa dưới -nographics.
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal static class UxLayoutAuditor
    {
        /// <summary>Trần số dòng mỗi loại: một màn hỏng nặng sinh hàng nghìn dòng và JSON thành vô dụng.</summary>
        internal const int MaximumEntriesPerKind = 200;

        /// <summary>Sai số layout cho phép (px) — UI Toolkit làm tròn theo pixelsPerPoint nên 1 px lệch là bình thường.</summary>
        private const float LayoutTolerance = 1.5f;

        /// <summary>Chồng lấn dưới mức này là viền/đường kẻ chạm nhau, không phải hai khối đè lên nhau.</summary>
        private const float OverlapTolerance = 2f;

        internal const string ScrollViewOverflowVerticalMark = "TRÀN DỌC KHÔNG CÓ THANH CUỘN";
        internal const string ScrollViewOverflowHorizontalMark = "TRÀN NGANG KHÔNG CÓ THANH CUỘN";
        internal const string ScrollViewHorizontalBarMark = "CÓ THANH CUỘN NGANG";

        private static MethodInfo _measureTextSize;
        private static PropertyInfo _isElided;
        private static PropertyInfo _computedStyle;

        /// <summary>
        /// Duyệt cây của <paramref name="window"/> và trả kết quả. <paramref name="stretchRules"/> là các luật "vùng vẽ phải giãn"
        /// riêng của màn; <paramref name="requiredElements"/> là những phần tử quan trọng BẮT BUỘC có mặt và có kích thước.
        /// </summary>
        public static UxLayoutAuditResult Audit(EditorWindow window, string screenId, UxWindowSize size, LiveOpsHubLanguageId language,
            IReadOnlyList<string> requiredElements, IReadOnlyList<UxLayoutStretchRule> stretchRules)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            UxLayoutAuditResult result = new UxLayoutAuditResult(screenId, size, language);
            VisualElement root = window.rootVisualElement;
            result.RootBound = root.worldBound;
            Visit(root, result, true);
            CheckRequiredElements(root, result, requiredElements);
            CheckStretchRules(root, result, stretchRules);
            return result;
        }

        /// <summary>Phần tử đang hiện trên màn hình: cha con đều không display:none / visibility:hidden / opacity 0, và có kích thước.</summary>
        public static bool IsShownOnScreen(VisualElement element)
        {
            if (element == null) return false;
            for (VisualElement current = element; current != null; current = current.hierarchy.parent)
            {
                if (!IsSelfShown(current)) return false;
            }
            Rect bound = element.worldBound;
            return !float.IsNaN(bound.width) && bound.width > 0.5f && bound.height > 0.5f;
        }

        /// <summary>Nhãn ngắn của một element cho câu chẩn đoán: kiểu + #tên + vài class + chữ nó đang hiển thị.</summary>
        public static string Describe(VisualElement element)
        {
            if (element == null) return "(null)";
            StringBuilder builder = new StringBuilder(element.GetType().Name);
            if (!string.IsNullOrEmpty(element.name)) builder.Append('#').Append(element.name);
            int classCount = 0;
            foreach (string className in element.GetClasses())
            {
                if (className.StartsWith("unity-", StringComparison.Ordinal) && classCount > 0) continue;
                builder.Append('.').Append(className);
                if (++classCount >= 3) break;
            }
            if (element is TextElement text && !string.IsNullOrEmpty(text.text)) builder.Append(" '").Append(Shorten(text.text, 40)).Append('\'');
            return builder.ToString();
        }

        /// <summary>Tìm theo tên element (<c>#tên</c> hoặc tên trần) hoặc theo class USS — một hàm để bảng luật của màn viết gọn.</summary>
        public static VisualElement FindBySelector(VisualElement root, string selector)
        {
            if (root == null || string.IsNullOrEmpty(selector)) return null;
            if (selector[0] == '.') return root.Q(className: selector.Substring(1));
            string name = selector[0] == '#' ? selector.Substring(1) : selector;
            VisualElement byName = root.Q(name);
            return byName ?? root.Q(className: name);
        }

        /// <summary>Dòng ScrollView có phải lỗi không — chỉ tràn mà không cuộn được, hoặc có thanh cuộn ngang.</summary>
        public static bool IsScrollViewProblem(string entry)
        {
            if (string.IsNullOrEmpty(entry)) return false;
            return entry.IndexOf(ScrollViewOverflowVerticalMark, StringComparison.Ordinal) >= 0
                || entry.IndexOf(ScrollViewOverflowHorizontalMark, StringComparison.Ordinal) >= 0
                || entry.IndexOf(ScrollViewHorizontalBarMark, StringComparison.Ordinal) >= 0;
        }

        /// <summary>Ghi JSON chẩn đoán và trả đường dẫn — assert in đường dẫn này ra để người soát mở thẳng file.</summary>
        public static string WriteJson(UxLayoutAuditResult result)
        {
            string path = UxHubWindowFixture.DiagnosticsPath(result.FileStem);
            StringBuilder json = new StringBuilder();
            json.Append("{\n");
            AppendProperty(json, "screen", result.ScreenId);
            AppendProperty(json, "size", result.Size.ToString());
            AppendProperty(json, "language", result.Language == LiveOpsHubLanguageId.Vietnamese ? "vi" : "en");
            AppendProperty(json, "unityVersion", Application.unityVersion);
            json.Append("  \"rootBound\": ").Append(RectJson(result.RootBound)).Append(",\n");
            json.Append("  \"problemCount\": ").Append(result.Problems().Count.ToString(CultureInfo.InvariantCulture));
            AppendArray(json, UxLayoutFindingKinds.MissingElement, result.MissingElement);
            AppendArray(json, UxLayoutFindingKinds.ZeroSizeNamed, result.ZeroSizeNamed);
            AppendArray(json, UxLayoutFindingKinds.TextCut, result.TextCut);
            AppendArray(json, UxLayoutFindingKinds.ChildOverflow, result.ChildOverflow);
            AppendArray(json, UxLayoutFindingKinds.ScrollViews, result.ScrollViews);
            AppendArray(json, UxLayoutFindingKinds.AbsoluteOverText, result.AbsoluteOverText);
            AppendArray(json, UxLayoutFindingKinds.SiblingOverlap, result.SiblingOverlap);
            AppendArray(json, UxLayoutFindingKinds.NotStretched, result.NotStretched);
            json.Append("\n}\n");
            File.WriteAllText(path, json.ToString());
            return path;
        }

        // ================================================================================================ duyệt cây

        private static void Visit(VisualElement element, UxLayoutAuditResult result, bool ancestorsShown)
        {
            if (element.resolvedStyle.display == DisplayStyle.None) return;
            bool shown = ancestorsShown && IsSelfShown(element);
            Rect bound = element.worldBound;
            if (float.IsNaN(bound.width) || float.IsNaN(bound.height)) return;
            if (shown) CheckElement(element, bound, result);

            List<VisualElement> relativeChildren = new List<VisualElement>();
            List<VisualElement> absoluteChildren = new List<VisualElement>();
            List<VisualElement> textChildren = new List<VisualElement>();
            int childCount = element.hierarchy.childCount;
            for (int index = 0; index < childCount; index++)
            {
                VisualElement child = element.hierarchy[index];
                if (child.resolvedStyle.display == DisplayStyle.None) continue;
                if (shown && IsSelfShown(child))
                {
                    Rect childBound = child.worldBound;
                    if (childBound.width > 0.5f && childBound.height > 0.5f)
                    {
                        if (child.resolvedStyle.position == Position.Absolute) absoluteChildren.Add(child);
                        else relativeChildren.Add(child);
                        if (child is TextElement || HasDirectTextChild(child)) textChildren.Add(child);
                        CheckChildOverflow(element, bound, child, childBound, result);
                    }
                }
                Visit(child, result, shown);
            }
            if (!shown) return;
            CheckSiblingOverlap(relativeChildren, result);
            CheckAbsoluteOverText(absoluteChildren, textChildren, result);
        }

        private static bool IsSelfShown(VisualElement element)
        {
            IResolvedStyle style = element.resolvedStyle;
            if (style.display == DisplayStyle.None) return false;
            if (style.visibility == Visibility.Hidden) return false;
            return style.opacity > 0.01f;
        }

        private static bool HasDirectTextChild(VisualElement element)
        {
            int childCount = element.hierarchy.childCount;
            for (int index = 0; index < childCount && index < 12; index++)
            {
                if (element.hierarchy[index] is TextElement) return true;
            }
            return false;
        }

        private static void CheckElement(VisualElement element, Rect bound, UxLayoutAuditResult result)
        {
            if (!string.IsNullOrEmpty(element.name) && !element.name.StartsWith("unity-", StringComparison.Ordinal)
                && (bound.width < 0.5f || bound.height < 0.5f))
            {
                Add(result, result.ZeroSizeNamed, UxLayoutFindingKinds.ZeroSizeNamed,
                    Describe(element) + " " + RectText(bound, result.RootBound));
            }
            if (element is TextElement text && !string.IsNullOrEmpty(text.text) && bound.width > 0f) CheckTextCut(text, bound, result);
            if (element is ScrollView scrollView) CheckScrollView(scrollView, result);
        }

        private static void CheckTextCut(TextElement text, Rect bound, UxLayoutAuditResult result)
        {
            Rect content = text.contentRect;
            string value = text.text;
            WhiteSpace whiteSpace = text.resolvedStyle.whiteSpace;
            bool wraps = whiteSpace != WhiteSpace.NoWrap;
            string reason = null;
            if (!wraps)
            {
                Vector2 natural = MeasureText(text, value, float.NaN);
                if (natural.x > content.width + LayoutTolerance)
                {
                    reason = "ngang cần " + Number(natural.x) + " có " + Number(content.width);
                }
            }
            else
            {
                Vector2 wrapped = MeasureText(text, value, content.width);
                if (wrapped.y > content.height + LayoutTolerance)
                {
                    reason = "dọc cần " + Number(wrapped.y) + " có " + Number(content.height);
                }
            }
            if (reason == null && IsElided(text)) reason = "isElided";
            bool clipped = IsClippedByAncestor(text, bound, out string clipper);
            if (reason == null && !clipped) return;
            Add(result, result.TextCut, UxLayoutFindingKinds.TextCut,
                Describe(text) + " " + (reason ?? string.Empty) + (clipped ? " | bị cha cắt: " + clipper : string.Empty)
                + " @" + RectText(bound, result.RootBound));
        }

        private static void CheckScrollView(ScrollView scrollView, UxLayoutAuditResult result)
        {
            Rect viewport = scrollView.contentViewport.layout;
            Rect content = scrollView.contentContainer.layout;
            bool verticalBar = IsSelfShown(scrollView.verticalScroller);
            bool horizontalBar = IsSelfShown(scrollView.horizontalScroller);
            StringBuilder entry = new StringBuilder(Describe(scrollView))
                .Append(" viewport ").Append(Number(viewport.width)).Append('x').Append(Number(viewport.height))
                .Append(" content ").Append(Number(content.width)).Append('x').Append(Number(content.height))
                .Append(" vbar=").Append(verticalBar ? "true" : "false")
                .Append(" hbar=").Append(horizontalBar ? "true" : "false");
            if (content.height > viewport.height + LayoutTolerance && !verticalBar) entry.Append(" | ").Append(ScrollViewOverflowVerticalMark);
            if (content.width > viewport.width + LayoutTolerance && !horizontalBar) entry.Append(" | ").Append(ScrollViewOverflowHorizontalMark);
            if (horizontalBar) entry.Append(" | ").Append(ScrollViewHorizontalBarMark);
            entry.Append(" @").Append(RectText(scrollView.worldBound, result.RootBound));
            Add(result, result.ScrollViews, UxLayoutFindingKinds.ScrollViews, entry.ToString());
        }

        private static void CheckChildOverflow(VisualElement parent, Rect parentBound, VisualElement child, Rect childBound,
            UxLayoutAuditResult result)
        {
            if (parentBound.width < 1f || parentBound.height < 1f) return;
            // Nội dung cuộn RA NGOÀI viewport là đúng bản chất ScrollView — không phải tràn.
            if (parent is ScrollView) return;
            if (string.Equals(parent.name, "unity-content-container", StringComparison.Ordinal)) return;
            if (string.Equals(parent.name, "unity-content-viewport", StringComparison.Ordinal)) return;
            float right = childBound.xMax - parentBound.xMax;
            float left = parentBound.xMin - childBound.xMin;
            float bottom = childBound.yMax - parentBound.yMax;
            float top = parentBound.yMin - childBound.yMin;
            if (right <= LayoutTolerance && left <= LayoutTolerance && bottom <= LayoutTolerance && top <= LayoutTolerance) return;
            Add(result, result.ChildOverflow, UxLayoutFindingKinds.ChildOverflow,
                Describe(child) + " tràn " + Describe(parent) + " [phải " + Number(right) + ", trái " + Number(left)
                + ", dưới " + Number(bottom) + ", trên " + Number(top) + "] cha overflow=" + OverflowOf(parent)
                + " @" + RectText(childBound, result.RootBound));
        }

        private static void CheckSiblingOverlap(List<VisualElement> relativeChildren, UxLayoutAuditResult result)
        {
            for (int first = 0; first < relativeChildren.Count; first++)
            {
                for (int second = first + 1; second < relativeChildren.Count; second++)
                {
                    Rect firstBound = relativeChildren[first].worldBound;
                    Rect secondBound = relativeChildren[second].worldBound;
                    float overlapWidth = Mathf.Min(firstBound.xMax, secondBound.xMax) - Mathf.Max(firstBound.xMin, secondBound.xMin);
                    float overlapHeight = Mathf.Min(firstBound.yMax, secondBound.yMax) - Mathf.Max(firstBound.yMin, secondBound.yMin);
                    if (overlapWidth <= LayoutTolerance || overlapHeight <= LayoutTolerance) continue;
                    Add(result, result.SiblingOverlap, UxLayoutFindingKinds.SiblingOverlap,
                        Describe(relativeChildren[first]) + " chồng " + Describe(relativeChildren[second]) + " = "
                        + Number(overlapWidth) + "x" + Number(overlapHeight) + " @" + RectText(firstBound, result.RootBound));
                }
            }
        }

        private static void CheckAbsoluteOverText(List<VisualElement> absoluteChildren, List<VisualElement> textChildren,
            UxLayoutAuditResult result)
        {
            foreach (VisualElement overlay in absoluteChildren)
            {
                Rect overlayBound = overlay.worldBound;
                foreach (VisualElement other in textChildren)
                {
                    if (other == overlay) continue;
                    if (other.resolvedStyle.position == Position.Absolute) continue;
                    Rect otherBound = other.worldBound;
                    float overlapWidth = Mathf.Min(overlayBound.xMax, otherBound.xMax) - Mathf.Max(overlayBound.xMin, otherBound.xMin);
                    float overlapHeight = Mathf.Min(overlayBound.yMax, otherBound.yMax) - Mathf.Max(overlayBound.yMin, otherBound.yMin);
                    if (overlapWidth <= OverlapTolerance || overlapHeight <= OverlapTolerance) continue;
                    Add(result, result.AbsoluteOverText, UxLayoutFindingKinds.AbsoluteOverText,
                        Describe(overlay) + " phủ " + Describe(other) + " " + Number(overlapWidth) + "x" + Number(overlapHeight)
                        + " @" + RectText(overlayBound, result.RootBound));
                }
            }
        }

        private static void CheckRequiredElements(VisualElement root, UxLayoutAuditResult result, IReadOnlyList<string> requiredElements)
        {
            if (requiredElements == null) return;
            foreach (string selector in requiredElements)
            {
                VisualElement element = FindBySelector(root, selector);
                if (element == null)
                {
                    Add(result, result.MissingElement, UxLayoutFindingKinds.MissingElement,
                        selector + ": không có trong cây — người dùng không có lối vào");
                    continue;
                }
                if (IsShownOnScreen(element)) continue;
                Add(result, result.MissingElement, UxLayoutFindingKinds.MissingElement,
                    selector + ": có trong cây nhưng không dùng được " + RectText(element.worldBound, result.RootBound)
                    + " display=" + element.resolvedStyle.display + " visibility=" + element.resolvedStyle.visibility);
            }
        }

        private static void CheckStretchRules(VisualElement root, UxLayoutAuditResult result, IReadOnlyList<UxLayoutStretchRule> rules)
        {
            if (rules == null) return;
            foreach (UxLayoutStretchRule rule in rules)
            {
                VisualElement child = FindBySelector(root, rule.ChildSelector);
                VisualElement parent = FindBySelector(root, rule.ParentSelector);
                if (child == null || parent == null) continue;
                Rect childBound = child.worldBound;
                Rect parentBound = parent.worldBound;
                float childSize = rule.Vertical ? childBound.height : childBound.width;
                float parentSize = rule.Vertical ? parentBound.height : parentBound.width;
                if (parentSize < 1f) continue;
                float ratio = childSize / parentSize;
                if (ratio >= rule.MinimumRatio) continue;
                Add(result, result.NotStretched, UxLayoutFindingKinds.NotStretched,
                    rule.ChildSelector + " chỉ lấp " + Number(ratio * 100f) + "% " + (rule.Vertical ? "chiều cao" : "bề rộng")
                    + " của " + rule.ParentSelector + " (cần ≥ " + Number(rule.MinimumRatio * 100f) + "%): "
                    + Number(childSize) + " / " + Number(parentSize));
            }
        }

        private static void Add(UxLayoutAuditResult result, List<string> entries, string kind, string entry)
        {
            if (entries.Count >= MaximumEntriesPerKind) return;
            if (UxLayoutAllowList.Allows(result.ScreenId, kind, entry)) return;
            entries.Add(entry);
        }

        // ================================================================================================ đo chữ (reflection)

        /// <summary>
        /// <c>MeasureTextSize</c> và <c>isElided</c> đổi chỗ giữa 2022.3 và 6000.6 (lúc public, lúc internal) — gọi thẳng là hỏng
        /// một trong hai bản, nên tra bằng reflection một lần rồi nhớ. Không tìm được thì trả 0 / false: mất một loại phát hiện
        /// còn hơn làm đỏ cả cổng vì API đổi tên.
        /// </summary>
        private static Vector2 MeasureText(TextElement text, string value, float width)
        {
            if (_measureTextSize == null)
            {
                foreach (MethodInfo candidate in typeof(TextElement).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!string.Equals(candidate.Name, "MeasureTextSize", StringComparison.Ordinal)) continue;
                    ParameterInfo[] parameters = candidate.GetParameters();
                    if (parameters.Length != 5 || parameters[0].ParameterType != typeof(string)) continue;
                    _measureTextSize = candidate;
                    break;
                }
                if (_measureTextSize == null) return Vector2.zero;
            }
            try
            {
                Type modeType = _measureTextSize.GetParameters()[2].ParameterType;
                object undefined = Enum.Parse(modeType, "Undefined");
                object exactly = Enum.Parse(modeType, "Exactly");
                bool hasWidth = !float.IsNaN(width);
                object measured = _measureTextSize.Invoke(text, new object[]
                {
                    value, hasWidth ? width : 0f, hasWidth ? exactly : undefined, 0f, undefined,
                });
                return (Vector2)measured;
            }
            catch (Exception failure)
            {
                Debug.LogWarning("UxLayoutAuditor: MeasureTextSize hỏng — " + failure.Message);
                return Vector2.zero;
            }
        }

        private static bool IsElided(TextElement text)
        {
            if (_isElided == null)
            {
                _isElided = FindProperty(typeof(TextElement), "isElided");
                if (_isElided == null) return false;
            }
            try
            {
                return (bool)_isElided.GetValue(text, null);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary><c>IResolvedStyle</c> không có <c>overflow</c> — đọc <c>computedStyle</c> (internal) bằng reflection.</summary>
        private static string OverflowOf(VisualElement element)
        {
            try
            {
                if (_computedStyle == null) _computedStyle = FindProperty(typeof(VisualElement), "computedStyle");
                if (_computedStyle == null) return "?";
                object computed = _computedStyle.GetValue(element, null);
                if (computed == null) return "?";
                PropertyInfo overflow = FindProperty(computed.GetType(), "overflow");
                if (overflow != null) return overflow.GetValue(computed, null).ToString();
                FieldInfo field = computed.GetType().GetField("overflow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return field != null ? field.GetValue(computed).ToString() : "?";
            }
            catch (Exception)
            {
                return "?";
            }
        }

        private static bool IsClippedByAncestor(VisualElement element, Rect bound, out string clipper)
        {
            clipper = null;
            for (VisualElement ancestor = element.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
            {
                bool clips = ancestor is ScrollView || string.Equals(OverflowOf(ancestor), "Hidden", StringComparison.Ordinal);
                if (!clips) continue;
                Rect clip = ancestor.worldBound;
                bool outside = bound.xMax > clip.xMax + LayoutTolerance || bound.xMin < clip.xMin - LayoutTolerance
                    || bound.yMax > clip.yMax + LayoutTolerance || bound.yMin < clip.yMin - LayoutTolerance;
                if (!outside) return false;
                // Chữ cuộn ra khỏi viewport theo chiều DỌC là bình thường khi ScrollView cuộn được; chỉ ngang mới là cắt.
                if (ancestor is ScrollView && bound.xMax <= clip.xMax + LayoutTolerance && bound.xMin >= clip.xMin - LayoutTolerance) return false;
                clipper = Describe(ancestor) + " " + RectText(clip, clip);
                return true;
            }
            return false;
        }

        private static PropertyInfo FindProperty(Type type, string name)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                foreach (PropertyInfo property in current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (string.Equals(property.Name, name, StringComparison.Ordinal) && property.GetIndexParameters().Length == 0) return property;
                }
            }
            return null;
        }

        // ================================================================================================ chữ + JSON

        private static string Shorten(string value, int maximum)
        {
            if (value == null) return string.Empty;
            string flattened = value.Replace("\n", "\\n");
            return flattened.Length <= maximum ? flattened : flattened.Substring(0, maximum) + "…";
        }

        private static string RectText(Rect bound, Rect origin)
        {
            return "(" + Number(bound.x - origin.x) + "," + Number(bound.y - origin.y) + " "
                + Number(bound.width) + "x" + Number(bound.height) + ")";
        }

        private static string RectJson(Rect rect)
        {
            return "{\"x\": " + Number(rect.x) + ", \"y\": " + Number(rect.y) + ", \"width\": " + Number(rect.width)
                + ", \"height\": " + Number(rect.height) + "}";
        }

        internal static string Number(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? "0" : value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static void AppendProperty(StringBuilder json, string name, string value)
        {
            json.Append("  ").Append(Quote(name)).Append(": ").Append(Quote(value)).Append(",\n");
        }

        private static void AppendArray(StringBuilder json, string name, List<string> values)
        {
            json.Append(",\n  ").Append(Quote(name)).Append(": [");
            for (int index = 0; index < values.Count; index++)
            {
                json.Append(index == 0 ? "\n    " : ",\n    ").Append(Quote(values[index]));
            }
            json.Append(values.Count > 0 ? "\n  ]" : "]");
        }

        private static string Quote(string value)
        {
            StringBuilder builder = new StringBuilder("\"");
            foreach (char character in value ?? string.Empty)
            {
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < ' ') builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        else builder.Append(character);
                        break;
                }
            }
            return builder.Append('"').ToString();
        }
    }
}
