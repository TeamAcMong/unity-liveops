// MÃ DEV TẠM (G-UX-JOURNEY) — bảy cảnh kiểm TẬN MẮT của đợt vét W10.
// Vì sao tách file riêng: bảy cảnh này không phải hành trình khám phá như J1…J11 mà là danh sách nghiệm thu của MỘT đợt;
// gom chung vào UxJourneysMain sẽ làm lẫn "đi dạo tìm lỗi" với "kiểm lại phiếu đã sửa", và lượt sau khó bỏ cả cụm đi.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DreamTech.LiveOps.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UxJourney
{
    public static partial class UxJourneys
    {
        /// <summary>Đợt có giờ kết thúc hỏng cố ý ("2026-10-3") trong mẫu mồi — cảnh (c) ô ngày giờ KHÔNG đọc được.</summary>
        private const string LavaQuestLateEntryKey = "entry-lava-quest-2026-10";

        /// <summary>Lớp của dải chú giải dưới trục — cảnh (f) phải chứng minh toast KHÔNG đè lên nó.</summary>
        private const string TimelineLegendClassName = "liveops-hub-timeline-legend";

        private const string TimelineHintClassName = "liveops-hub-timeline-hint";

        static partial void RegisterW10(Dictionary<string, Func<UxRunner, IEnumerator>> registry)
        {
            registry["W10a"] = W10aMultiSelectionDrawer;
            registry["W10b"] = W10bHintLine;
            registry["W10c"] = W10cUnreadableDateTime;
            registry["W10d"] = W10dEventTypesTable;
            registry["W10e"] = W10eQuietText;
            registry["W10f"] = W10fToastVersusLegend;
            registry["W10g"] = W10gSixSections;
        }

        // ---------------------------------------------------------------- (a) chọn nhiều thanh ở bốn cỡ dưới mốc 1100

        /// <summary>
        /// Bốn cỡ dưới mốc trung bình (1100px) — ở đó pane inspector nằm trong NGĂN KÉO, và lỗi W9-25 là ngăn kéo không
        /// mở khi tập chọn có nhiều hơn một đợt. Mỗi cỡ chụp hai nhịp: TRƯỚC khi chọn (ngăn kéo phải đóng) và SAU khi
        /// ⌘-click thanh thứ hai (ngăn kéo phải mở, thân pane phải là pane NHIỀU đợt).
        /// </summary>
        private static IEnumerator W10aMultiSelectionDrawer(UxRunner runner)
        {
            int[] widths = { 700, 820, 950, 1024 };
            int[] heights = { 560, 560, 700, 700 };
            for (int index = 0; index < widths.Length; index++)
            {
                float width = widths[index];
                float height = heights[index];
                yield return OpenDesignHub(runner, "calendar", width, height);
                string size = Size(width, height);
                yield return Snap(runner, "W10a-" + size + "-1-chua-chon", "cỡ " + size + ": chưa chọn, ngăn kéo phải ĐÓNG");
                ReportDrawer(runner, "W10a-" + size + "-truoc", 0);

                yield return SelectBar(runner, HuntEarly);
                yield return Snap(runner, "W10a-" + size + "-2-chon-mot", "chọn 1 đợt (hunt-0914) → ngăn kéo mở, pane MỘT đợt");
                ReportDrawer(runner, "W10a-" + size + "-mot", 1);

                VisualElement second = Bar(HuntBonus);
                if (second == null)
                {
                    Note(runner, "cỡ " + size + ": không thấy thanh " + HuntBonus + " để ⌘-click");
                }
                else
                {
                    yield return UxInput.ClickAt(Hub, second.worldBound.center, EventModifiers.Command);
                    yield return new WaitFrames(8);
                    yield return Snap(runner, "W10a-" + size + "-3-chon-nhieu", "⌘-click thanh thứ hai → ngăn kéo phải MỞ, pane NHIỀU đợt");
                    ReportDrawer(runner, "W10a-" + size + "-nhieu", 2);
                    DumpTree(runner, Root, "W10a-" + size + "-tree.txt");
                }

                yield return UxInput.PressKey(Hub, KeyCode.Escape, EventModifiers.None, (char)27);
                yield return new WaitFrames(8);
                yield return Snap(runner, "W10a-" + size + "-4-esc", "Esc → ngăn kéo phải đóng lại");
                ReportDrawer(runner, "W10a-" + size + "-esc", 0);
            }
        }

        /// <summary>
        /// Ghi ra số đo của ngăn kéo: có hiện không, cao bao nhiêu, pane đang là MỘT hay NHIỀU đợt. Nhận dạng pane nhiều
        /// đợt bằng NÚT XOÁ có số đếm chứ không bằng riêng tên lớp — lớp dùng chung với pane một đợt (bài học W9-25).
        /// </summary>
        private static void ReportDrawer(UxRunner runner, string stepId, int expectedSelectionCount)
        {
            VisualElement inspector = Root.Q("calendar-inspector");
            bool isShown = inspector != null && UxFind.Shown(inspector);
            float height = inspector == null ? 0f : inspector.worldBound.height;
            float width = inspector == null ? 0f : inspector.worldBound.width;
            string deleteButtonText = "";
            if (inspector != null)
            {
                VisualElement deleteButton = UxFind.First(inspector, element => element is Button button && button.text != null &&
                                                                                (button.text.StartsWith("Xoá", StringComparison.Ordinal) ||
                                                                                 button.text.StartsWith("Delete", StringComparison.Ordinal)));
                if (deleteButton != null) deleteButtonText = ((Button)deleteButton).text;
            }
            string line = stepId + " | mong đợi tập chọn = " + expectedSelectionCount +
                          " | inspector hiện = " + isShown +
                          " | " + width.ToString("0.#", CultureInfo.InvariantCulture) + "×" + height.ToString("0.#", CultureInfo.InvariantCulture) +
                          " | nút xoá = '" + deleteButtonText + "'";
            runner.Log(line);
            File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, "W10-do-dac.txt"), line + "\n");
        }

        // ---------------------------------------------------------------- (b) dòng gợi ý đáy trục, vi + en, có đợt bị bỏ

        /// <summary>
        /// Ba cỡ hẹp × hai ngôn ngữ. Mỗi cỡ đi ba trạng thái của dòng gợi ý: không chọn gì, chọn một đợt thường, và chọn
        /// đợt BỊ BỎ (câu dài nhất — có vế lý do + cụm phím ở CUỐI, chỗ mà bản cũ cắt mất bằng "…").
        /// </summary>
        private static IEnumerator W10bHintLine(UxRunner runner)
        {
            int[] widths = { 700, 820, 950 };
            string[] languages = { "Vietnamese", "English" };
            foreach (string language in languages)
            {
                foreach (int width in widths)
                {
                    float height = width >= 950 ? 700 : 560;
                    yield return OpenDesignHub(runner, "calendar", width, height, language);
                    string tag = (language == "English" ? "en" : "vi") + "-" + Size(width, height);
                    yield return Snap(runner, "W10b-" + tag + "-1-khong-chon", "gợi ý khi chưa chọn (" + language + ")");
                    ReportHint(runner, "W10b-" + tag + "-khong-chon");

                    yield return SelectBar(runner, HuntBonus);
                    yield return Snap(runner, "W10b-" + tag + "-2-chon-thuong", "gợi ý khi chọn đợt thường");
                    ReportHint(runner, "W10b-" + tag + "-chon-thuong");

                    VisualElement skipped = FindSkippedBar(runner);
                    if (skipped == null)
                    {
                        Note(runner, tag + ": không tìm ra thanh 'bị bỏ' trên trục");
                    }
                    else
                    {
                        yield return UxInput.ClickAt(Hub, skipped.worldBound.center);
                        yield return new WaitFrames(8);
                        yield return Snap(runner, "W10b-" + tag + "-3-doi-bi-bo", "gợi ý khi chọn đợt BỊ BỎ — câu dài nhất");
                        ReportHint(runner, "W10b-" + tag + "-doi-bi-bo");
                    }
                }
            }
        }

        /// <summary>
        /// Thanh của một lần lặp bị bỏ: tìm nhãn mang chữ "bị bỏ" / "dropped" rồi lấy thanh nằm cùng hàng với nó. Không
        /// dò bằng id cứng vì mẫu mồi có thể đổi đợt nào bị bỏ mà không đổi tên nào.
        /// </summary>
        private static VisualElement FindSkippedBar(UxRunner runner)
        {
            TextElement tag = UxFind.First(Root, element => element is TextElement text && UxFind.Shown(element) && text.text != null &&
                                                            (text.text.Contains("bị bỏ") || text.text.Contains("dropped"))) as TextElement;
            if (tag == null) return null;
            Vector2 anchor = tag.worldBound.center;
            List<VisualElement> bars = UxFind.AllByClass(Root, "liveops-hub-timeline-bar");
            VisualElement best = null;
            float bestDistance = float.MaxValue;
            foreach (VisualElement bar in bars)
            {
                Rect bound = bar.worldBound;
                float distance = Mathf.Abs(bound.center.y - anchor.y) * 4f + Mathf.Abs(bound.center.x - anchor.x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = bar;
                }
            }
            runner.Log("nhãn bị bỏ '" + tag.text + "' tại " + anchor + " → thanh gần nhất " + (best == null ? "null" : best.name + " " + best.worldBound));
            return best;
        }

        /// <summary>Ghi chiều cao dòng gợi ý + CHỮ đầy đủ của nó, để đối chiếu với ảnh: câu có bị cắt bằng "…" không.</summary>
        private static void ReportHint(UxRunner runner, string stepId)
        {
            VisualElement hint = UxFind.ByClass(Root, TimelineHintClassName);
            StringBuilder builder = new StringBuilder();
            builder.Append(stepId).Append(" | gợi ý ");
            if (hint == null)
            {
                builder.Append("KHÔNG THẤY");
            }
            else
            {
                builder.Append(hint.worldBound.width.ToString("0.#", CultureInfo.InvariantCulture)).Append('×')
                    .Append(hint.worldBound.height.ToString("0.#", CultureInfo.InvariantCulture));
                foreach (VisualElement child in UxFind.All(hint, element => element is TextElement))
                {
                    TextElement text = (TextElement)child;
                    builder.Append(" | ").Append(UxDiagnostics.Name(child)).Append("='").Append(text.text).Append('\'');
                    if (text.text != null && text.text.Contains("…")) builder.Append(" [CÓ DẤU RÚT GỌN]");
                }
            }
            runner.Log(builder.ToString());
            File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, "W10-do-dac.txt"), builder + "\n");
        }

        // ---------------------------------------------------------------- (c) ô ngày giờ không đọc được ở 1280

        /// <summary>
        /// Đợt "entry-lava-quest-2026-10" có endUtc hỏng cố ý. Ở 1280 pane inspector nằm cạnh trục (không phải ngăn kéo),
        /// nên đây là chỗ mà biểu tượng lỗi dễ bị mép pane cắt nhất.
        /// </summary>
        private static IEnumerator W10cUnreadableDateTime(UxRunner runner)
        {
            yield return OpenDesignHub(runner, "calendar", 1280, 760);
            VisualElement bar = Bar(LavaQuestLateEntryKey);
            if (bar == null)
            {
                Note(runner, "1280: thanh " + LavaQuestLateEntryKey + " không nằm trong tầm nhìn — đổi zoom sang Tháng rồi tìm lại");
                yield return ZoomToMonth(runner);
                bar = Bar(LavaQuestLateEntryKey);
            }
            if (bar != null)
            {
                yield return UxInput.ClickAt(Hub, bar.worldBound.center);
            }
            else
            {
                // Đợt giờ hỏng nằm ở tháng 10, ngoài tầm nhìn của trục ở mọi mức zoom mà toolbar cho chọn, nên không bấm
                // được bằng chuột. Đặt thẳng khoá thanh qua presenter — ĐÚNG đường mà lệnh chụp h01d dùng — rồi mới nhìn
                // pane. Điều đang kiểm là PANE, không phải cách chọn.
                if (!SelectBarKeyThroughPresenter(runner, LavaQuestLateEntryKey))
                {
                    Note(runner, "không đặt được khoá thanh qua presenter — cảnh (c) KHÔNG chụp được");
                    yield break;
                }
            }
            yield return new WaitFrames(10);
            yield return Snap(runner, "W10c-1280x760-o-gio-hong", "chọn đợt có endUtc '2026-10-3' → ô ngày giờ không đọc được");
            DumpTree(runner, Root.Q("calendar-inspector"), "W10c-tree-inspector.txt");
            ReportErrorIcons(runner, "W10c-1280x760");
        }

        /// <summary>
        /// Đặt khoá thanh đang chọn qua <c>CalendarSection.Presenter.SetSelectedBarKey</c>. Dùng reflection vì màn nằm
        /// trong danh sách riêng của cửa sổ; đây là cùng một đường mà lệnh chụp của cổng đi, nên pane dựng ra y hệt.
        /// </summary>
        private static bool SelectBarKeyThroughPresenter(UxRunner runner, string barKey)
        {
            System.Reflection.FieldInfo field = typeof(LiveOpsHubWindow).GetField("_sections",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            IEnumerable list = field == null ? null : field.GetValue(Hub) as IEnumerable;
            if (list == null)
            {
                runner.Log("không đọc được danh sách màn của cửa sổ hub");
                return false;
            }
            foreach (object section in list)
            {
                if (section == null) continue;
                if (section.GetType().Name != "CalendarSection") continue;
                object presenter = section.GetType()
                    .GetProperty("Presenter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(section, null);
                if (presenter == null) return false;
                presenter.GetType().GetMethod("SetSelectedBarKey").Invoke(presenter, new object[] { barKey });
                runner.Log("đặt khoá thanh đang chọn = " + barKey + " qua presenter");
                return true;
            }
            return false;
        }

        private static IEnumerator ZoomToMonth(UxRunner runner)
        {
            VisualElement monthTab = UxFind.Clickable(Root, "Tháng", true) ?? UxFind.Clickable(Root, "Month", true);
            if (monthTab == null)
            {
                Note(runner, "không thấy tab/mục zoom Tháng");
                yield break;
            }
            yield return UxInput.Click(Hub, monthTab);
            yield return new WaitFrames(10);
        }

        /// <summary>
        /// Biểu tượng lỗi bị cắt hay không đo bằng hình: hộp của biểu tượng phải nằm TRỌN trong hộp của pane chứa nó.
        /// Ghi ra số dư từng mép để lượt sau đọc số, không đọc lời khai.
        /// </summary>
        private static void ReportErrorIcons(UxRunner runner, string stepId)
        {
            VisualElement inspector = Root.Q("calendar-inspector");
            if (inspector == null)
            {
                Note(runner, stepId + ": không có calendar-inspector để đo biểu tượng lỗi");
                return;
            }
            Rect pane = inspector.worldBound;
            List<VisualElement> icons = UxFind.All(inspector, element => element is Image && UxFind.Shown(element));
            StringBuilder builder = new StringBuilder();
            builder.Append(stepId).Append(" | pane ").Append(pane.ToString()).Append(" | biểu tượng hiện = ").Append(icons.Count);
            foreach (VisualElement icon in icons)
            {
                Rect bound = icon.worldBound;
                float leftMargin = bound.xMin - pane.xMin;
                float rightMargin = pane.xMax - bound.xMax;
                float topMargin = bound.yMin - pane.yMin;
                float bottomMargin = pane.yMax - bound.yMax;
                bool isCut = leftMargin < -0.5f || rightMargin < -0.5f || topMargin < -0.5f || bottomMargin < -0.5f;
                builder.Append(" | ").Append(UxDiagnostics.Name(icon)).Append(' ').Append(bound.ToString())
                    .Append(" dư(trái,phải,trên,dưới)=")
                    .Append(leftMargin.ToString("0.#", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rightMargin.ToString("0.#", CultureInfo.InvariantCulture)).Append(',')
                    .Append(topMargin.ToString("0.#", CultureInfo.InvariantCulture)).Append(',')
                    .Append(bottomMargin.ToString("0.#", CultureInfo.InvariantCulture));
                if (isCut) builder.Append(" [BỊ CẮT]");
            }
            runner.Log(builder.ToString());
            File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, "W10-do-dac.txt"), builder + "\n");
        }

        // ---------------------------------------------------------------- (d) bảng Loại event ở 700 và 1280

        /// <summary>
        /// Hai cỡ đối nghịch: 700 là chỗ bảng phải BỎ bớt cột phụ, 1280 là chỗ bảng phải đủ cột. Cả hai cỡ đều soát luật
        /// rút gọn có điều kiện: ô nào có "…" thì PHẢI có tooltip đủ chữ.
        /// </summary>
        private static IEnumerator W10dEventTypesTable(UxRunner runner)
        {
            int[] widths = { 700, 1280 };
            foreach (int width in widths)
            {
                float height = width >= 1280 ? 760 : 560;
                yield return OpenDesignHub(runner, "event-types", width, height);
                string size = Size(width, height);
                yield return Snap(runner, "W10d-" + size + "-1-bang", "bảng Loại event ở " + size);
                ReportTableColumns(runner, "W10d-" + size);
                ReportTruncatedCells(runner, "W10d-" + size);
                DumpTree(runner, Root, "W10d-" + size + "-tree.txt");

                // Loại chưa khai báo: hàng có viền swatch riêng — phiếu W10-09 vừa nâng viền ấy lên 3,16:1 ở skin sáng.
                VisualElement undeclared = UxFind.ByClass(Root, "liveops-hub-event-types-row--undeclared");
                if (undeclared != null)
                {
                    yield return UxInput.ClickAt(Hub, undeclared.worldBound.center);
                    yield return new WaitFrames(8);
                    yield return Snap(runner, "W10d-" + size + "-2-loai-chua-khai", "chọn hàng loại CHƯA KHAI BÁO — viền swatch riêng");
                }
                else Note(runner, size + ": không thấy hàng loại chưa khai báo");
            }
        }

        /// <summary>Đếm cột đang hiện của bảng + đọc dòng chữ "đã bỏ cột…" nếu có, kèm tooltip liệt kê cột.</summary>
        private static void ReportTableColumns(UxRunner runner, string stepId)
        {
            List<VisualElement> headers = UxFind.All(Root, element => element.ClassListContains("unity-multi-column-header__column") && UxFind.Shown(element));
            StringBuilder builder = new StringBuilder();
            builder.Append(stepId).Append(" | cột hiện = ").Append(headers.Count);
            foreach (VisualElement header in headers)
            {
                TextElement text = UxFind.First(header, element => element is TextElement title && !string.IsNullOrEmpty(title.text)) as TextElement;
                builder.Append(" | '").Append(text == null ? "?" : text.text).Append('\'');
            }
            TextElement droppedNotice = UxFind.First(Root, element => element is TextElement note && UxFind.Shown(element) && note.text != null &&
                                                                      (note.text.Contains("cột") || note.text.Contains("column"))) as TextElement;
            if (droppedNotice != null)
            {
                builder.Append(" | dòng chữ dưới bảng = '").Append(droppedNotice.text).Append("' tooltip='")
                    .Append(droppedNotice.tooltip == null ? "" : droppedNotice.tooltip.Replace("\n", "⏎")).Append('\'');
            }
            runner.Log(builder.ToString());
            File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, "W10-do-dac.txt"), builder + "\n");
        }

        /// <summary>
        /// Mọi nhãn ĐANG HIỆN mà chữ không vừa hộp của nó: ghi chữ, tooltip và kết luận ĐẠT/TRƯỢT của luật rút gọn —
        /// có "…" mà tooltip rỗng là TRƯỢT, không cần nhìn ảnh mới biết.
        /// </summary>
        private static void ReportTruncatedCells(UxRunner runner, string stepId)
        {
            List<VisualElement> labels = UxFind.All(Root, element => element is TextElement && UxFind.Shown(element));
            StringBuilder builder = new StringBuilder();
            int truncatedCount = 0;
            int missingTooltipCount = 0;
            foreach (VisualElement element in labels)
            {
                TextElement text = (TextElement)element;
                if (string.IsNullOrEmpty(text.text)) continue;
                Vector2 measured = text.MeasureTextSize(text.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                bool doesNotFit = measured.x > text.contentRect.width + 0.5f;
                if (!doesNotFit && !text.text.Contains("…")) continue;
                // Chữ GẬP DÒNG (white-space: normal) không bị cắt — nó xuống hàng. Luật rút gọn chỉ nói về ô KHÔNG gập
                // dòng; gộp cả hai vào một con số là tự báo động giả rồi tự khai miễn trừ.
                bool doesWrap = text.resolvedStyle.whiteSpace == WhiteSpace.Normal;
                bool doesEllipsis = text.resolvedStyle.textOverflow == TextOverflow.Ellipsis;
                if (doesWrap && !text.text.Contains("…"))
                {
                    File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, "W10-do-dac.txt"),
                        stepId + " | GẬP DÒNG (không phải rút gọn) '" + text.text + "'\n");
                    continue;
                }
                truncatedCount++;
                bool hasFullTooltip = !string.IsNullOrEmpty(text.tooltip) && text.tooltip.Contains(text.text.Replace("…", string.Empty).Trim());
                if (!hasFullTooltip) missingTooltipCount++;
                builder.Append(stepId).Append(" | RÚT GỌN ").Append(doesEllipsis ? "(ellipsis) " : "(cắt cứng) ").Append('\'').Append(text.text).Append("' rộng cần ")
                    .Append(measured.x.ToString("0.#", CultureInfo.InvariantCulture)).Append(" có ")
                    .Append(text.contentRect.width.ToString("0.#", CultureInfo.InvariantCulture))
                    .Append(" | tooltip='").Append(text.tooltip == null ? "" : text.tooltip.Replace("\n", "⏎")).Append('\'')
                    .Append(hasFullTooltip ? " [ĐẠT]" : " [TRƯỢT — thiếu tooltip đủ chữ]").Append('\n');
            }
            string summary = stepId + " | ô không chứa hết chữ = " + truncatedCount + ", trong đó THIẾU tooltip = " + missingTooltipCount;
            runner.Log(summary);
            File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, "W10-do-dac.txt"), summary + "\n" + builder);
        }

        // ---------------------------------------------------------------- (e) màu chữ mờ

        /// <summary>
        /// Chữ mờ (class quiet) nằm rải khắp hub. Lượt GUI chỉ chạy được MỘT skin (skin là pref của máy, chỉ capture.sh
        /// mới được đổi — SP-4), nên hành trình này chụp các màn đậm đặc chữ mờ ở skin đang chạy; skin còn lại lấy từ
        /// lượt capture.sh cùng cây. Kèm số đo màu hợp thành của từng đoạn chữ mờ để đối chiếu với mắt.
        /// </summary>
        private static IEnumerator W10eQuietText(UxRunner runner)
        {
            string[] sections = { "overview", "validation", "event-types", "recurring", "export" };
            foreach (string section in sections)
            {
                yield return OpenDesignHub(runner, section, 1280, 760);
                yield return Snap(runner, "W10e-" + section + "-1280x760", "chữ mờ ở màn " + section + " (skin đang chạy)");
                ReportQuietText(runner, "W10e-" + section);
            }
        }

        private static void ReportQuietText(UxRunner runner, string stepId)
        {
            List<VisualElement> quiet = UxFind.All(Root, element => element.ClassListContains("liveops-hub-text--quiet") && UxFind.Shown(element));
            StringBuilder builder = new StringBuilder();
            builder.Append(stepId).Append(" | đoạn chữ mờ hiện = ").Append(quiet.Count);
            foreach (VisualElement element in quiet)
            {
                TextElement text = element as TextElement;
                Color color = element.resolvedStyle.color;
                float opacity = element.resolvedStyle.opacity;
                builder.Append(" | '").Append(text == null ? UxDiagnostics.Name(element) : text.text).Append("' màu=")
                    .Append(ColorUtility.ToHtmlStringRGB(color)).Append(" opacity=").Append(opacity.ToString("0.##", CultureInfo.InvariantCulture));
            }
            runner.Log(builder.ToString());
            File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, "W10-do-dac.txt"), builder + "\n");
        }

        // ---------------------------------------------------------------- (f) toast không đè chú giải

        /// <summary>
        /// Toast của màn Lịch nổi ở đáy content, dải chú giải cũng nằm ở đáy trục — hai thứ tranh nhau một chỗ. Sinh toast
        /// bằng đúng đường người dùng (kéo một thanh rồi nhả → toast Hoàn tác), rồi đo chồng lấn giữa hai hộp.
        /// </summary>
        private static IEnumerator W10fToastVersusLegend(UxRunner runner)
        {
            int[] widths = { 820, 1280 };
            foreach (int width in widths)
            {
                float height = width >= 1280 ? 760 : 560;
                yield return OpenDesignHub(runner, "calendar", width, height);
                string size = Size(width, height);
                yield return EnsureLegendVisible(runner);
                yield return Snap(runner, "W10f-" + size + "-1-chu-giai", "dải chú giải đang bật, chưa có toast");
                ReportToastVersusLegend(runner, "W10f-" + size + "-truoc-toast");

                VisualElement bar = Bar(LavaMid);
                if (bar == null)
                {
                    Note(runner, size + ": không thấy thanh " + LavaMid + " để kéo sinh toast");
                    continue;
                }
                yield return SelectBar(runner, LavaMid);
                Vector2 start = Bar(LavaMid).worldBound.center;
                yield return UxInput.WaitRealCursorAway(Hub);
                yield return UxInput.Drag(Hub, start, start + new Vector2(60, 0), 10, EventModifiers.None, EventModifiers.None, null);
                yield return new WaitFrames(10);
                yield return Snap(runner, "W10f-" + size + "-2-toast", "kéo đợt rồi nhả → toast Hoàn tác, chú giải vẫn phải đọc được");
                ReportToastVersusLegend(runner, "W10f-" + size + "-co-toast");
                yield return Undo(runner);
                yield return new WaitFrames(8);
            }
            DrainModalPlans(runner);
        }

        private static IEnumerator EnsureLegendVisible(UxRunner runner)
        {
            VisualElement legend = UxFind.ByClass(Root, TimelineLegendClassName);
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

        /// <summary>Đo chồng lấn hình học giữa toast và chú giải — số, không phải cảm giác.</summary>
        private static void ReportToastVersusLegend(UxRunner runner, string stepId)
        {
            VisualElement toast = UxFind.ByClass(Root, "liveops-hub-toast");
            VisualElement legend = UxFind.ByClass(Root, TimelineLegendClassName);
            StringBuilder builder = new StringBuilder();
            builder.Append(stepId).Append(" | toast=").Append(toast == null ? "không có" : toast.worldBound.ToString())
                .Append(" | chú giải=").Append(legend == null ? "không có" : legend.worldBound.ToString());
            if (toast != null && legend != null && UxFind.Shown(toast) && UxFind.Shown(legend))
            {
                Rect first = toast.worldBound;
                Rect second = legend.worldBound;
                float overlapWidth = Mathf.Min(first.xMax, second.xMax) - Mathf.Max(first.xMin, second.xMin);
                float overlapHeight = Mathf.Min(first.yMax, second.yMax) - Mathf.Max(first.yMin, second.yMin);
                bool isOverlapping = overlapWidth > 0.5f && overlapHeight > 0.5f;
                builder.Append(" | chồng lấn=").Append(overlapWidth.ToString("0.#", CultureInfo.InvariantCulture)).Append('×')
                    .Append(overlapHeight.ToString("0.#", CultureInfo.InvariantCulture)).Append(isOverlapping ? " [ĐÈ]" : " [không đè]");
            }
            runner.Log(builder.ToString());
            File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, "W10-do-dac.txt"), builder + "\n");
        }

        // ---------------------------------------------------------------- (g) vòng nhanh sáu màn ở 820 và 1440

        /// <summary>
        /// Đi hết sáu màn ở hai cỡ, mỗi màn một ảnh. Mục đích không phải tìm lỗi sâu mà là bắt hồi quy THÔ sau khi gộp
        /// bảy gói: màn trắng, chữ đè, thanh cuộn ngang, rail sai trạng thái.
        /// </summary>
        private static IEnumerator W10gSixSections(UxRunner runner)
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
                    yield return Snap(runner, "W10g-" + size + "-" + section, "vòng nhanh: màn " + section + " ở " + size);
                    ReportSectionSanity(runner, "W10g-" + size + "-" + section);
                }
            }
        }

        /// <summary>
        /// Ba dấu hiệu hồi quy thô, đo được: tiêu đề màn có chữ không, thân màn có phần tử hiện không, và có chỗ nào
        /// thò ra khỏi mép phải của cửa sổ không (thanh cuộn ngang trá hình).
        /// </summary>
        private static void ReportSectionSanity(UxRunner runner, string stepId)
        {
            Rect root = Root.worldBound;
            int shownCount = UxFind.All(Root, element => UxFind.Shown(element)).Count;
            List<VisualElement> overflowing = UxFind.All(Root, element => UxFind.Shown(element) && element.worldBound.xMax > root.xMax + 0.5f);
            StringBuilder builder = new StringBuilder();
            builder.Append(stepId).Append(" | tiêu đề='").Append(SectionTitle()).Append("' | phần tử hiện=").Append(shownCount)
                .Append(" | thò khỏi mép phải=").Append(overflowing.Count);
            for (int index = 0; index < overflowing.Count && index < 6; index++)
            {
                builder.Append(" | ").Append(UxDiagnostics.Name(overflowing[index])).Append(' ').Append(overflowing[index].worldBound.ToString());
            }
            runner.Log(builder.ToString());
            File.AppendAllText(Path.Combine(runner.Options.OutputDirectory, "W10-do-dac.txt"), builder + "\n");
        }
    }
}
