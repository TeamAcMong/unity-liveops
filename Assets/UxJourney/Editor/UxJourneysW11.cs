// MÃ DEV TẠM (G-UX-JOURNEY) — năm cảnh kiểm TẬN MẮT của đợt W11.
// Vì sao lại một file nữa thay vì nối vào UxJourneysW10: W10 là danh sách nghiệm thu của đợt vét W10 và nó phải giữ nguyên để
// còn so sánh "trước/sau" với lượt đi dạo W10; đợt W11 nghiệm thu BA BẢN VÁ khác (J2-01 toast, J2-02 banner, J2-06 dấu phẩy)
// cộng hai chỗ W10 còn nợ (J2-04 ở 950 · vòng nhanh sáu màn). Trộn hai danh sách vào một file làm lượt sau không bỏ được cụm cũ đi.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UxJourney
{
    public static partial class UxJourneys
    {
        /// <summary>Tên file số đo của đợt W11 — tách khỏi W10-do-dac.txt để hai lượt đi dạo không lẫn nguồn.</summary>
        private const string W11MeasurementFileName = "W11-do-dac.txt";

        /// <summary>Lớp của dải chú giải dưới trục — cảnh (a) phải chứng minh toast KHÔNG đè lên nó khi ngăn kéo MỞ.</summary>
        private const string W11TimelineLegendClassName = "liveops-hub-timeline-legend";

        private const string W11TimelineHintClassName = "liveops-hub-timeline-hint";

        private const string W11ValidationNoticeClassName = "liveops-hub-validation-notice";

        private const string W11ValidationNoticeButtonClassName = "liveops-hub-validation-notice-button";

        private const string W11RuleSentenceClassName = "liveops-hub-rule-sentence";

        private const string W11RuleTokenClassName = "liveops-hub-rule-token";

        static partial void RegisterW11(Dictionary<string, Func<UxRunner, IEnumerator>> registry)
        {
            registry["W11a"] = W11aToastVersusLegendWithDrawerOpen;
            registry["W11b"] = W11bValidationNoticeBanner;
            registry["W11c"] = W11cRuleSentencePunctuation;
            registry["W11d"] = W11dHintLineAt950WithDroppedEntry;
            registry["W11e"] = W11eSixSectionsSweep;
        }

        /// <summary>Ghi một dòng số đo vào cả nhật ký lượt chạy lẫn file số đo của đợt — kết luận sau này luôn có số, không chỉ ảnh.</summary>
        private static void MeasureLine(UxRunner runner, string line)
        {
            runner.Log(line);
            File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, W11MeasurementFileName), line + "\n");
        }

        private static string Number(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        /// <summary>Độ sáng tương đối WCAG 2.1 — dùng để nói "viền có đọc được không" bằng SỐ chứ không bằng cảm giác.</summary>
        private static float RelativeLuminance(Color color)
        {
            return 0.2126f * LuminanceChannel(color.r) + 0.7152f * LuminanceChannel(color.g) + 0.0722f * LuminanceChannel(color.b);
        }

        private static float LuminanceChannel(float channel)
        {
            return channel <= 0.03928f ? channel / 12.92f : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }

        private static float ContrastRatio(Color first, Color second)
        {
            float firstLuminance = RelativeLuminance(first);
            float secondLuminance = RelativeLuminance(second);
            float lighter = Mathf.Max(firstLuminance, secondLuminance);
            float darker = Mathf.Min(firstLuminance, secondLuminance);
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        // ---------------------------------------------------------------- (a) toast không đè chú giải khi NGĂN KÉO MỞ

        /// <summary>
        /// Phiếu J2-01 đo được ở đúng một cảnh: cửa sổ 820, ngăn kéo inspector ĐANG MỞ (chú giải hẹp lại nên gập hai hàng và mép
        /// trên của nó trèo lên tới chỗ toast đậu). Nên cảnh này KHÔNG chỉ kéo một thanh rồi chụp: nó khẳng định ngăn kéo mở
        /// bằng số trước khi sinh toast, và đo lại cùng phép đo chồng lấn mà lượt W10 đã dùng, để hai lượt so được với nhau.
        /// Kèm cảnh ngăn kéo ĐÓNG làm đối chứng — nếu chỉ cảnh mở còn đè thì bản vá sai chỗ, nếu cả hai đè thì bản vá không chạy.
        /// </summary>
        private static IEnumerator W11aToastVersusLegendWithDrawerOpen(UxRunner runner)
        {
            int[] widths = { 820, 1280 };
            foreach (int width in widths)
            {
                float height = width >= 1280 ? 760 : 560;
                string size = Size(width, height);
                yield return OpenDesignHub(runner, "calendar", width, height);
                yield return EnsureW11LegendVisible(runner);
                yield return Snap(runner, "W11a-" + size + "-1-chua-chon", "chú giải bật, chưa chọn gì, chưa có toast");
                ReportToastLegendHint(runner, "W11a-" + size + "-1-chua-chon");

                // Chọn một đợt: dưới mốc 1100 thì pane inspector nằm trong NGĂN KÉO và cú chọn này là thứ mở nó ra.
                yield return SelectBar(runner, LavaMid);
                yield return new WaitFrames(8);
                yield return Snap(runner, "W11a-" + size + "-2-ngan-keo-mo", "chọn 1 đợt → ngăn kéo mở, chú giải hẹp lại và có thể gập hàng");
                ReportW11Drawer(runner, "W11a-" + size + "-2-ngan-keo-mo");
                ReportToastLegendHint(runner, "W11a-" + size + "-2-ngan-keo-mo");

                VisualElement bar = Bar(LavaMid);
                if (bar == null)
                {
                    Note(runner, size + ": không thấy thanh " + LavaMid + " để kéo sinh toast");
                    continue;
                }
                Vector2 start = bar.worldBound.center;
                yield return UxInput.WaitRealCursorAway(Hub);
                yield return UxInput.Drag(Hub, start, start + new Vector2(60, 0), 10, EventModifiers.None, EventModifiers.None, null);
                yield return new WaitFrames(12);
                yield return Snap(runner, "W11a-" + size + "-3-toast-ngan-keo-mo", "kéo đợt rồi nhả → toast Hoàn tác trong khi ngăn kéo VẪN MỞ");
                ReportW11Drawer(runner, "W11a-" + size + "-3-toast-ngan-keo-mo");
                ReportToastLegendHint(runner, "W11a-" + size + "-3-toast-ngan-keo-mo");
                DumpTree(runner, Root, "W11a-" + size + "-toast-tree.txt");

                // Đối chứng: đóng ngăn kéo bằng Esc rồi sinh lại toast. Chú giải rộng ra, thường chỉ còn một hàng — đây là cảnh
                // mà lượt W10 đã thấy "không đè" ở 1280, giữ lại để so hai cảnh trong CÙNG một lượt chạy.
                yield return Undo(runner);
                yield return new WaitFrames(8);
                yield return UxInput.PressKey(Hub, KeyCode.Escape, EventModifiers.None, (char)27);
                yield return new WaitFrames(8);
                VisualElement barAgain = Bar(LavaMid);
                if (barAgain != null)
                {
                    Vector2 secondStart = barAgain.worldBound.center;
                    yield return UxInput.WaitRealCursorAway(Hub);
                    yield return UxInput.Drag(Hub, secondStart, secondStart + new Vector2(60, 0), 10, EventModifiers.None, EventModifiers.None, null);
                    yield return new WaitFrames(12);
                    yield return Snap(runner, "W11a-" + size + "-4-toast-ngan-keo-dong", "đối chứng: cùng toast khi ngăn kéo ĐÓNG");
                    ReportW11Drawer(runner, "W11a-" + size + "-4-toast-ngan-keo-dong");
                    ReportToastLegendHint(runner, "W11a-" + size + "-4-toast-ngan-keo-dong");
                    yield return Undo(runner);
                    yield return new WaitFrames(8);
                }
            }
            DrainModalPlans(runner);
        }

        private static IEnumerator EnsureW11LegendVisible(UxRunner runner)
        {
            VisualElement legend = UxFind.ByClass(Root, W11TimelineLegendClassName);
            if (legend != null && UxFind.Shown(legend)) yield break;
            UnityEditor.UIElements.ToolbarMenu overflow = UxFind.First(Root, element => element is UnityEditor.UIElements.ToolbarMenu menu &&
                                                                                         menu.ClassListContains("liveops-hub-calendar-overflow-menu") && UxFind.Shown(menu))
                as UnityEditor.UIElements.ToolbarMenu;
            if (overflow == null)
            {
                Note(runner, "chú giải đang ẩn mà không có menu ⋮ để bật lại");
                yield break;
            }
            List<DropdownMenuAction> actions = ReadMenu(runner, "menu ⋮ (bật chú giải)", overflow.menu);
            RunMenu(runner, actions, "Chú giải");
            yield return new WaitFrames(8);
        }

        /// <summary>Ngăn kéo inspector: đang hiện không, rộng cao bao nhiêu — cảnh (a) chỉ có nghĩa khi ngăn kéo THẬT SỰ mở.</summary>
        private static void ReportW11Drawer(UxRunner runner, string stepId)
        {
            VisualElement inspector = Root.Q("calendar-inspector");
            bool isShown = inspector != null && UxFind.Shown(inspector);
            string line = stepId + " | ngăn kéo hiện=" + isShown +
                          " | hộp=" + (inspector == null ? "không có" : inspector.worldBound.ToString());
            MeasureLine(runner, line);
        }

        /// <summary>
        /// Đo chồng lấn hình học giữa toast và HAI thứ nằm ở chân trục: dải chú giải và dòng gợi ý. Lượt W10 chỉ đo chú giải;
        /// bản vá J2-01 nâng toast theo chân màn ĐO ĐƯỢC nên nó cũng có thể đè dòng gợi ý — phải đo cả hai mới kết luận được.
        /// </summary>
        private static void ReportToastLegendHint(UxRunner runner, string stepId)
        {
            VisualElement toast = UxFind.ByClass(Root, "liveops-hub-toast");
            VisualElement legend = UxFind.ByClass(Root, W11TimelineLegendClassName);
            VisualElement hint = UxFind.ByClass(Root, W11TimelineHintClassName);
            StringBuilder builder = new StringBuilder();
            builder.Append(stepId)
                .Append(" | toast=").Append(toast == null || !UxFind.Shown(toast) ? "không có" : toast.worldBound.ToString())
                .Append(" | chú giải=").Append(legend == null || !UxFind.Shown(legend) ? "không có" : legend.worldBound.ToString())
                .Append(" | gợi ý=").Append(hint == null || !UxFind.Shown(hint) ? "không có" : hint.worldBound.ToString());
            AppendOverlap(builder, "toast↔chú giải", toast, legend);
            AppendOverlap(builder, "toast↔gợi ý", toast, hint);
            if (toast != null && UxFind.Shown(toast))
            {
                TextElement toastText = UxFind.First(toast, element => element is TextElement text && !string.IsNullOrEmpty(text.text)) as TextElement;
                if (toastText != null) builder.Append(" | câu toast='").Append(toastText.text).Append('\'');
            }
            MeasureLine(runner, builder.ToString());
        }

        private static void AppendOverlap(StringBuilder builder, string title, VisualElement first, VisualElement second)
        {
            if (first == null || second == null || !UxFind.Shown(first) || !UxFind.Shown(second)) return;
            Rect firstBound = first.worldBound;
            Rect secondBound = second.worldBound;
            float overlapWidth = Mathf.Min(firstBound.xMax, secondBound.xMax) - Mathf.Max(firstBound.xMin, secondBound.xMin);
            float overlapHeight = Mathf.Min(firstBound.yMax, secondBound.yMax) - Mathf.Max(firstBound.yMin, secondBound.yMin);
            bool isOverlapping = overlapWidth > 0.5f && overlapHeight > 0.5f;
            builder.Append(" | ").Append(title).Append('=').Append(Number(overlapWidth)).Append('×').Append(Number(overlapHeight))
                .Append(isOverlapping ? " [ĐÈ]" : " [không đè]");
        }

        // ---------------------------------------------------------------- (b) hộp thông báo màn Kiểm lịch

        /// <summary>
        /// Phiếu J2-02: nút trong banner "kết quả cũ" nhận trọn bề ngang banner, và hai nút cùng đọc "Kiểm lại". Banner chỉ hiện
        /// ở trạng thái KIỂM RỒI MỚI SỬA, nên cảnh này dựng trạng thái ấy bằng đúng đường người dùng: services của bộ đi dạo đã
        /// chạy kiểm xong khi mở, ta sang màn Lịch KÉO một đợt (một lần sửa thật, có Undo), rồi quay lại màn Kiểm lịch.
        /// Ba cỡ vì lỗi là "nút dãn theo bề ngang": 1440 là chỗ dải nút dài nhất, 820 là chỗ banner gập nhiều hàng.
        /// </summary>
        private static IEnumerator W11bValidationNoticeBanner(UxRunner runner)
        {
            int[] widths = { 1440, 1280, 820 };
            foreach (int width in widths)
            {
                float height = width >= 1440 ? 900 : (width >= 1280 ? 760 : 560);
                string size = Size(width, height);
                yield return OpenDesignHub(runner, "calendar", width, height);
                VisualElement bar = Bar(LavaMid);
                if (bar == null)
                {
                    Note(runner, size + ": không thấy thanh " + LavaMid + " để sửa lịch sau lượt kiểm");
                    continue;
                }
                Vector2 start = bar.worldBound.center;
                yield return UxInput.WaitRealCursorAway(Hub);
                yield return UxInput.Drag(Hub, start, start + new Vector2(60, 0), 10, EventModifiers.None, EventModifiers.None, null);
                yield return new WaitFrames(12);
                yield return NavigateTo(runner, LiveOpsValidationTitle());
                yield return new WaitFrames(10);
                yield return Snap(runner, "W11b-" + size + "-1-banner", "màn Kiểm lịch sau khi sửa lịch → banner 'kết quả cũ'");
                ReportValidationNotice(runner, "W11b-" + size);
                ReportDuplicateButtonText(runner, "W11b-" + size);
                DumpTree(runner, Root, "W11b-" + size + "-tree.txt");
            }
            DrainModalPlans(runner);
        }

        /// <summary>
        /// Tên màn Kiểm lịch đọc từ hàng rail đang hiện thay vì dán chuỗi cứng — chuỗi hiển thị thuộc catalog vi/en và lượt đi dạo
        /// này chạy tiếng Việt, nhưng dán cứng thì đổi catalog là hành trình câm mà không ai biết.
        /// </summary>
        private static string LiveOpsValidationTitle()
        {
            VisualElement label = UxFind.First(Root, element => element is Label text && text.ClassListContains("liveops-hub-rail-row-label") &&
                                                                UxFind.Shown(element) && text.text != null &&
                                                                (text.text.Contains("Kiểm") || text.text.Contains("Validat")));
            return label == null ? "Kiểm lịch" : ((Label)label).text;
        }

        /// <summary>
        /// Ba con số quyết phiếu J2-02: (1) nút có dãn hết bề ngang banner không, (2) banner xếp ngang hay dọc, (3) viền nút đo
        /// được bao nhiêu so với nền ngay dưới nó. Bậc hình khối của WCAG là 3:1 — ghi cả số lẫn kết để lượt sau đọc số.
        /// </summary>
        private static void ReportValidationNotice(UxRunner runner, string stepId)
        {
            VisualElement notice = UxFind.ByClass(Root, W11ValidationNoticeClassName);
            if (notice == null || !UxFind.Shown(notice))
            {
                MeasureLine(runner, stepId + " | banner 'kết quả cũ' KHÔNG HIỆN — cảnh chưa dựng được");
                return;
            }
            VisualElement button = UxFind.ByClass(notice, W11ValidationNoticeButtonClassName);
            VisualElement text = UxFind.ByClass(notice, "liveops-hub-validation-notice-text");
            StringBuilder builder = new StringBuilder();
            builder.Append(stepId).Append(" | banner=").Append(notice.worldBound.ToString())
                .Append(" flex-direction=").Append(notice.resolvedStyle.flexDirection)
                .Append(" align-items=").Append(notice.resolvedStyle.alignItems)
                .Append(" | chữ=").Append(text == null ? "không có" : text.worldBound.ToString())
                .Append(" | nút=").Append(button == null ? "không có" : button.worldBound.ToString());
            if (button != null)
            {
                float buttonShare = notice.worldBound.width <= 0.5f ? 0f : button.worldBound.width / notice.worldBound.width * 100f;
                bool isStretched = buttonShare > 90f;
                builder.Append(" | nút chiếm ").Append(Number(buttonShare)).Append("% bề ngang banner")
                    .Append(isStretched ? " [DÃN HẾT — TRƯỢT]" : " [không dãn]");
                Button typed = button as Button;
                if (typed != null) builder.Append(" | chữ nút='").Append(typed.text).Append('\'');
                Color borderColor = button.resolvedStyle.borderBottomColor;
                Color buttonBackground = button.resolvedStyle.backgroundColor;
                Color noticeBackground = FirstOpaqueBackground(notice);
                builder.Append(" | viền=").Append(ColorUtility.ToHtmlStringRGB(borderColor))
                    .Append(" nền nút=").Append(ColorUtility.ToHtmlStringRGB(buttonBackground))
                    .Append(" nền sau lưng=").Append(ColorUtility.ToHtmlStringRGB(noticeBackground))
                    .Append(" | viền/nền nút=").Append(ContrastRatio(borderColor, buttonBackground).ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" viền/nền sau lưng=").Append(ContrastRatio(borderColor, noticeBackground).ToString("0.00", CultureInfo.InvariantCulture));
            }
            MeasureLine(runner, builder.ToString());
        }

        /// <summary>Nền mờ thì không nói được gì về tương phản — leo lên cha tới khi gặp nền ĐỤC đầu tiên, đó là màu mắt thật sự thấy.</summary>
        private static Color FirstOpaqueBackground(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.hierarchy.parent)
            {
                Color background = current.resolvedStyle.backgroundColor;
                if (background.a > 0.99f) return background;
            }
            return Color.gray;
        }

        /// <summary>Hai nút cùng chữ hiện CÙNG LÚC là lỗi đọc được — đếm theo chữ, ghi ra mọi chữ trùng.</summary>
        private static void ReportDuplicateButtonText(UxRunner runner, string stepId)
        {
            List<VisualElement> buttons = UxFind.All(Root, element => element is Button && UxFind.Shown(element));
            Dictionary<string, int> counts = new Dictionary<string, int>();
            StringBuilder list = new StringBuilder();
            foreach (VisualElement element in buttons)
            {
                Button button = (Button)element;
                if (string.IsNullOrEmpty(button.text)) continue;
                counts.TryGetValue(button.text, out int current);
                counts[button.text] = current + 1;
                list.Append(" | '").Append(button.text).Append("' ").Append(Number(button.worldBound.width)).Append("px");
            }
            StringBuilder duplicates = new StringBuilder();
            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (pair.Value > 1) duplicates.Append(" | TRÙNG CHỮ '").Append(pair.Key).Append("' ×").Append(pair.Value);
            }
            MeasureLine(runner, stepId + " | nút đang hiện=" + buttons.Count + (duplicates.Length == 0 ? " | không nút nào trùng chữ" : duplicates.ToString()));
            MeasureLine(runner, stepId + " | danh sách nút" + list);
        }

        // ---------------------------------------------------------------- (c) dấu phẩy sau chip token của câu Luật lặp

        /// <summary>
        /// Phiếu J2-06: chip token có đệm hai bên, mảnh chữ đứng sau chip bắt đầu bằng dấu phẩy, nên đệm PHẢI đẩy dấu phẩy rời ra
        /// và câu đọc thành "Mỗi [7 ngày] , neo từ". Bản vá hạ đệm phải xuống 1px CHỈ khi sau chip là dấu câu. Cảnh này đo
        /// khoảng hở giữa mép phải chip và mép trái mảnh chữ kế — số, rồi mới nhìn ảnh.
        /// </summary>
        private static IEnumerator W11cRuleSentencePunctuation(UxRunner runner)
        {
            int[] widths = { 1280, 820 };
            foreach (int width in widths)
            {
                float height = width >= 1280 ? 760 : 560;
                string size = Size(width, height);
                yield return OpenDesignHub(runner, "recurring", width, height);
                yield return Snap(runner, "W11c-" + size + "-1-cau-luat", "câu Luật lặp có chip token ở " + size);
                ReportRuleSentences(runner, "W11c-" + size);
                DumpTree(runner, Root, "W11c-" + size + "-tree.txt");
            }
        }

        /// <summary>
        /// Với mỗi câu luật: liệt kê từng mảnh theo thứ tự trái→phải kèm hộp, rồi cho MỖI chip một dòng kết luận — mảnh ngay sau
        /// nó bắt đầu bằng dấu câu hay không, chip có mang class "--followed-by-punctuation" không, và hở bao nhiêu pixel.
        /// </summary>
        private static void ReportRuleSentences(UxRunner runner, string stepId)
        {
            List<VisualElement> sentences = UxFind.AllByClass(Root, W11RuleSentenceClassName);
            MeasureLine(runner, stepId + " | câu luật đang hiện=" + sentences.Count);
            int sentenceIndex = 0;
            foreach (VisualElement sentence in sentences)
            {
                if (!UxFind.Shown(sentence)) continue;
                sentenceIndex++;
                // Câu luật KHÔNG phẳng: nó gồm các cụm (sentence-group) và chip/chữ nằm TRONG cụm. Lượt đầu của cảnh này
                // chỉ đọc con trực tiếp nên in ra năm cái tên cụm thay vì câu — phải gom LÁ theo thứ tự tài liệu mới đọc được câu.
                List<VisualElement> pieces = new List<VisualElement>();
                CollectSentenceLeaves(sentence, pieces);
                StringBuilder readable = new StringBuilder();
                foreach (VisualElement piece in pieces)
                {
                    TextElement text = piece as TextElement;
                    string content = text == null ? UxDiagnostics.Name(piece) : text.text;
                    bool isToken = piece.ClassListContains(W11RuleTokenClassName);
                    readable.Append(isToken ? "[" + content + "]" : content);
                }
                MeasureLine(runner, stepId + " | câu " + sentenceIndex + " đọc thành: " + readable);

                for (int index = 0; index < pieces.Count - 1; index++)
                {
                    VisualElement piece = pieces[index];
                    if (!piece.ClassListContains(W11RuleTokenClassName)) continue;
                    VisualElement next = pieces[index + 1];
                    TextElement nextText = next as TextElement;
                    string nextContent = nextText == null ? string.Empty : nextText.text;
                    bool nextStartsWithPunctuation = nextContent.Length > 0 && (nextContent[0] == ',' || nextContent[0] == '.' ||
                                                                                nextContent[0] == ';' || nextContent[0] == ':');
                    bool hasPunctuationClass = piece.ClassListContains("liveops-hub-rule-token--followed-by-punctuation");
                    float gap = next.worldBound.xMin - piece.worldBound.xMax;
                    Button token = piece as Button;
                    string tokenText = token == null ? UxDiagnostics.Name(piece) : token.text;
                    // Đệm phải của chip: chữ trong chip kết thúc ở đâu so với mép hộp chip — đó là khoảng hở mắt THẬT SỰ thấy
                    // giữa chữ cuối của chip và dấu phẩy, hở hình học giữa hai hộp chỉ là một nửa câu chuyện.
                    float paddingRight = piece.resolvedStyle.paddingRight;
                    MeasureLine(runner, stepId + " | câu " + sentenceIndex + " chip '" + tokenText + "' → mảnh kế '" +
                                        nextContent + "' | sau chip là dấu câu=" + nextStartsWithPunctuation +
                                        " | có class --followed-by-punctuation=" + hasPunctuationClass +
                                        " | hở hộp=" + Number(gap) + "px | padding-right chip=" + Number(paddingRight) + "px" +
                                        (nextStartsWithPunctuation && !hasPunctuationClass ? " [TRƯỢT — chip thiếu class]" : string.Empty));
                }
            }
        }


        /// <summary>
        /// Gom lá của câu luật theo thứ tự trái→phải: chip token tính là MỘT lá (không mở ra nhãn bên trong nó), mảnh chữ
        /// thường cũng là một lá. Cụm bao ngoài chỉ là chỗ ngắt dòng, không phải một mảnh của câu.
        /// </summary>
        private static void CollectSentenceLeaves(VisualElement element, List<VisualElement> pieces)
        {
            for (int index = 0; index < element.hierarchy.childCount; index++)
            {
                VisualElement child = element.hierarchy[index];
                if (!UxFind.Shown(child)) continue;
                if (child.ClassListContains(W11RuleTokenClassName) || child is TextElement)
                {
                    pieces.Add(child);
                    continue;
                }
                CollectSentenceLeaves(child, pieces);
            }
        }

        // ---------------------------------------------------------------- (d) dòng gợi ý ở 950 với đợt BỊ BỎ

        /// <summary>
        /// Nợ W11-15: lượt W10 tìm thanh "bị bỏ" bằng cách dò nhãn rồi lấy thanh GẦN NHẤT theo hình học, nên ở 950 nó trúng
        /// lava-quest-2026-09a (câu ngắn "đã khép") thay vì hunt-0916-bonus (câu dài nhất). Cảnh này chọn theo KHOÁ của model —
        /// đúng đường mà ca cổng calendar-selection-dropped đi — nên 950 lần này NHÌN đúng cảnh xấu nhất.
        /// Kèm 700 và 820 để so với lượt W10 trên cùng một thước.
        /// </summary>
        private static IEnumerator W11dHintLineAt950WithDroppedEntry(UxRunner runner)
        {
            int[] widths = { 950, 820, 700 };
            string[] languages = { "Vietnamese", "English" };
            foreach (string language in languages)
            {
                foreach (int width in widths)
                {
                    float height = width >= 950 ? 700 : 560;
                    yield return OpenDesignHub(runner, "calendar", width, height, language);
                    string tag = (language == "English" ? "en" : "vi") + "-" + Size(width, height);
                    // Chọn theo KHOÁ, không dò nhãn: HuntBonus LÀ đợt bị bỏ của mẫu mồi và câu gợi ý của nó là biến thể dài nhất.
                    VisualElement dropped = Bar(HuntBonus);
                    if (dropped == null)
                    {
                        Note(runner, tag + ": không thấy thanh " + HuntBonus + " (đợt bị bỏ) trên trục");
                        continue;
                    }
                    yield return SelectBar(runner, HuntBonus);
                    yield return new WaitFrames(8);
                    yield return Snap(runner, "W11d-" + tag + "-doi-bi-bo", "chọn đợt BỊ BỎ theo khoá → câu gợi ý dài nhất (" + language + ")");
                    ReportW11Hint(runner, "W11d-" + tag + "-doi-bi-bo");
                }
            }
        }

        /// <summary>
        /// Dòng gợi ý: hộp, và từng đoạn chữ kèm cờ "có dấu rút gọn". Thêm một phép đo mà lượt W10 chưa có — chữ đo được có vừa
        /// hộp không: một câu gập hàng đúng cách vẫn đọc đủ, còn một câu bị cắt cứng thì không có "…" mà vẫn mất chữ.
        /// </summary>
        private static void ReportW11Hint(UxRunner runner, string stepId)
        {
            VisualElement hint = UxFind.ByClass(Root, W11TimelineHintClassName);
            StringBuilder builder = new StringBuilder();
            builder.Append(stepId).Append(" | gợi ý ");
            if (hint == null || !UxFind.Shown(hint))
            {
                builder.Append("KHÔNG THẤY");
                MeasureLine(runner, builder.ToString());
                return;
            }
            builder.Append(Number(hint.worldBound.width)).Append('×').Append(Number(hint.worldBound.height));
            foreach (VisualElement child in UxFind.All(hint, element => element is TextElement && UxFind.Shown(element)))
            {
                TextElement text = (TextElement)child;
                if (string.IsNullOrEmpty(text.text)) continue;
                Vector2 measured = text.MeasureTextSize(text.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                builder.Append(" | ").Append(UxDiagnostics.Name(child)).Append("='").Append(text.text).Append('\'')
                    .Append(" rộng cần ").Append(Number(measured.x)).Append(" có ").Append(Number(text.contentRect.width))
                    .Append(" gập=").Append(text.resolvedStyle.whiteSpace == WhiteSpace.Normal);
                if (text.text.Contains("…")) builder.Append(" [CÓ DẤU RÚT GỌN]");
            }
            MeasureLine(runner, builder.ToString());
        }

        // ---------------------------------------------------------------- (e) vòng nhanh sáu màn ở 820 và 1440

        /// <summary>
        /// Vòng nhanh sau khi gộp hai gói W11: không tìm lỗi sâu mà bắt hồi quy THÔ — màn trắng, chữ đè, thanh cuộn ngang,
        /// rail sai trạng thái. Dữ liệu là mẫu mồi THẬT (cùng services mà mọi cảnh khác dùng), không phải bộ dữ liệu xấu nhất.
        /// </summary>
        private static IEnumerator W11eSixSectionsSweep(UxRunner runner)
        {
            string[] sections = { "overview", "calendar", "recurring", "event-types", "validation", "export" };
            int[] widths = { 820, 1440 };
            foreach (int width in widths)
            {
                float height = width >= 1440 ? 900 : 560;
                foreach (string section in sections)
                {
                    yield return OpenDesignHub(runner, section, width, height);
                    string size = Size(width, height);
                    yield return Snap(runner, "W11e-" + size + "-" + section, "vòng nhanh: màn " + section + " ở " + size);
                    ReportW11SectionSanity(runner, "W11e-" + size + "-" + section);
                }
            }
        }

        /// <summary>
        /// Bốn dấu hiệu hồi quy thô, đo được: tiêu đề màn, số phần tử hiện, chỗ thò khỏi mép phải, và CHIỀU CAO dùng thật của
        /// thân màn so với cửa sổ (nợ W11-13 nói hai màn bỏ trống gần nửa bề ngang ở 1440×900 — đo để phiếu có số).
        /// </summary>
        private static void ReportW11SectionSanity(UxRunner runner, string stepId)
        {
            Rect root = Root.worldBound;
            int shownCount = UxFind.All(Root, element => UxFind.Shown(element)).Count;
            List<VisualElement> overflowing = UxFind.All(Root, element => UxFind.Shown(element) && element.worldBound.xMax > root.xMax + 0.5f);
            VisualElement content = UxFind.ByClass(Root, "liveops-hub-content");
            float usedBottom = 0f;
            float usedRight = 0f;
            if (content != null)
            {
                // Đo theo CHỮ đang hiện, không theo hộp chứa: hộp cuộn luôn căng hết cột nội dung nên đo hộp thì màn nào cũng
                // "dùng 100%" kể cả khi nửa dưới trống trơn — đúng cái mà phiếu J2-05 / nợ W11-13 nói tới.
                foreach (VisualElement element in UxFind.All(content, child => child is TextElement && UxFind.Shown(child)))
                {
                    TextElement text = (TextElement)element;
                    if (string.IsNullOrEmpty(text.text)) continue;
                    Rect bound = element.worldBound;
                    if (bound.height <= 0.5f || bound.width <= 0.5f) continue;
                    if (bound.yMax > usedBottom) usedBottom = bound.yMax;
                    if (bound.xMax > usedRight) usedRight = bound.xMax;
                }
            }
            StringBuilder builder = new StringBuilder();
            builder.Append(stepId).Append(" | tiêu đề='").Append(SectionTitle()).Append("' | phần tử hiện=").Append(shownCount)
                .Append(" | thò khỏi mép phải=").Append(overflowing.Count);
            if (content != null)
            {
                Rect contentBound = content.worldBound;
                float verticalShare = contentBound.height <= 0.5f ? 0f : (usedBottom - contentBound.yMin) / contentBound.height * 100f;
                float horizontalShare = contentBound.width <= 0.5f ? 0f : (usedRight - contentBound.xMin) / contentBound.width * 100f;
                builder.Append(" | cột nội dung=").Append(contentBound.ToString())
                    .Append(" | dùng theo chiều dọc=").Append(Number(verticalShare)).Append('%')
                    .Append(" | dùng theo chiều ngang=").Append(Number(horizontalShare)).Append('%');
            }
            for (int index = 0; index < overflowing.Count && index < 6; index++)
            {
                builder.Append(" | ").Append(UxDiagnostics.Name(overflowing[index])).Append(' ').Append(overflowing[index].worldBound.ToString());
            }
            MeasureLine(runner, builder.ToString());
        }
    }
}
