using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// BA LOẠI ô mà user chốt là được phép rút chữ có điều kiện — hai loại đầu chốt 21/9/2026, loại thứ ba (J2-03) chốt cùng
    /// ngày sau lượt đi thử màn Lịch. Không có loại thứ tư: mọi ô khác rút chữ vẫn là LỖI, y như trước.
    /// </summary>
    internal enum UxTruncatableCellKind
    {
        /// <summary>
        /// Ô của một cột đã khai BỀ RỘNG CỐ ĐỊNH. Cột GIÃN không thuộc diện này — cột giãn nở được thì chữ bị cắt ở đó là lỗi
        /// bố cục thật, không phải cái giá đã biết trước của một bảng nhiều cột.
        /// </summary>
        FixedWidthTableCell,

        /// <summary>Ô NHẬP: chữ dài hơn ô thì con trỏ cuộn theo, phần ngoài khung không mất đi.</summary>
        InputField,

        /// <summary>
        /// (J2-03) NHÃN NẰM TRONG MỘT HÌNH KHỐI DO DỮ LIỆU ĐỊNH BỀ RỘNG — hôm nay đúng một chỗ: nhãn thanh đợt trên trục.
        /// <para>
        /// Vì sao đây là loại RIÊNG chứ không gộp vào <see cref="FixedWidthTableCell"/>: ô cột cố định rộng bao nhiêu là do
        /// BỐ CỤC chọn, nên về nguyên tắc vẫn nới được. Bề rộng một thanh đợt thì bằng khoảng thời gian của đợt nhân tỉ lệ
        /// zoom — một đợt hai giờ ở mức zoom "Tháng" chỉ có vài pixel dù cửa sổ to cỡ nào. Không có bề rộng nào nới được để
        /// chữ vừa, nên đòi "không được rút" ở đây là đòi bỏ luôn nhãn trên thanh, tức mất nhiều hơn được.
        /// </para>
        /// <para>
        /// Khác hai loại kia ở một điểm nữa: hub TỰ rút chuỗi (<c>LiveOpsTimelineGeometry.BarLabel</c> cắt tại dấu phân cách
        /// rồi thêm "…") chứ không để UI Toolkit tự elide, nên phần tử chỉ còn mẩu chữ. Điều kiện (a) vì thế phải so tooltip
        /// với MẨU còn lại — <see cref="UxTruncationExemption.CarriesFullTextTooltip"/> đã có sẵn nhánh ấy.
        /// </para>
        /// </summary>
        GeometryDrivenBarLabel,
    }

    /// <summary>
    /// Một chỗ được miễn theo luật rút gọn CÓ ĐIỀU KIỆN. Mỗi mục phải khai đủ bốn thứ, và cả bốn đều bị một câu assert kiểm:
    /// ô loại gì, chữ đầy đủ đọc lại được ở ĐÂU ngoài tooltip, và TÊN CA TEST chứng minh hai điều kiện ấy.
    /// </summary>
    internal sealed class UxTruncationExemptionEntry
    {
        public UxTruncationExemptionEntry(string screenId, string selector, UxTruncatableCellKind cellKind,
            string fullTextSurface, string testName, string reason)
        {
            if (string.IsNullOrEmpty(selector)) throw new ArgumentNullException(nameof(selector));
            if (string.IsNullOrEmpty(fullTextSurface)) throw new ArgumentNullException(nameof(fullTextSurface));
            if (string.IsNullOrEmpty(testName)) throw new ArgumentNullException(nameof(testName));
            if (string.IsNullOrEmpty(reason)) throw new ArgumentNullException(nameof(reason));
            ScreenId = screenId;
            Selector = selector;
            CellKind = cellKind;
            FullTextSurface = fullTextSurface;
            TestName = testName;
            Reason = reason;
        }

        /// <summary>Màn áp dụng; <see cref="UxTruncationExemption.AnyScreen"/> = mọi màn.</summary>
        public string ScreenId { get; }

        /// <summary>Tên element hoặc class USS của CHÍNH ô bị phát hiện (hoặc của ô bọc ngoài, với ô nhập).</summary>
        public string Selector { get; }

        public UxTruncatableCellKind CellKind { get; }

        /// <summary>Điều kiện (b): chỗ KHÁC trong cùng màn hiện đủ chữ — câu này phải trỏ tới một element có thật.</summary>
        public string FullTextSurface { get; }

        /// <summary>
        /// Tên ca test chứng minh hai điều kiện. <see cref="UxTruncationExemptionTests"/> phải có đúng một phương thức tên
        /// này — <c>Catalog_EveryEntry_HasProvingTest</c> đọc bằng reflection, nên khai một tên không tồn tại là ĐỎ.
        /// </summary>
        public string TestName { get; }

        public string Reason { get; }
    }

    /// <summary>
    /// LUẬT RÚT GỌN CÓ ĐIỀU KIỆN — user chốt 21/9/2026, thay cho câu hỏi Q-W10-1 của cổng đợt W10.
    /// <para>
    /// Nguyên văn quyết định: chữ dài hơn ô <b>trong ô bảng cột bề rộng cố định</b> và <b>trong ô nhập</b> được phép rút bằng
    /// "…" <b>có điều kiện</b> — (a) có tooltip hiện đủ chữ, và (b) có chỗ khác trong màn hiển thị đủ. Luật "chữ chiếm hơn 95%
    /// bề rộng ô" (W9-25) được MIỄN cho đúng hai loại ô ấy. Mọi loại ô khác giữ nguyên: rút chữ = LỖI.
    /// </para>
    /// <para>
    /// Vì sao đây KHÔNG phải một danh sách miễn trừ thứ hai bên cạnh <see cref="UxLayoutAllowList"/>: mục của
    /// <see cref="UxLayoutAllowList"/> tha một phát hiện bằng LỜI KHAI — đọc xong không ai biết lời khai ấy còn đúng không.
    /// Mục ở đây chỉ tha khi HAI phép đo cùng đồng ý:
    /// </para>
    /// <list type="number">
    /// <item>Điều kiện (a) đo TẠI LƯỢT KIỂM, trên mọi màn × cỡ × ngôn ngữ: ô phải đang mang tooltip chứa đủ chữ của nó. Gỡ
    /// tooltip đi thì miễn trừ biến mất và phát hiện quay lại đỏ ngay ở lượt sau — không cần ai nhớ sửa bảng này.</item>
    /// <item>Điều kiện (b) đo ở một CA TEST riêng cho từng mục (<see cref="UxTruncationExemptionEntry.TestName"/>): mở hub
    /// thật, dựng đúng trạng thái làm ô bị cắt, rồi chứng minh chữ đầy đủ còn đọc được ở chỗ đã khai. Mục không có ca test là
    /// ĐỎ, nên "khai miễn trừ suông" không qua được cổng.</item>
    /// </list>
    /// </summary>
    internal static class UxTruncationExemption
    {
        /// <summary>Mục áp dụng cho mọi màn.</summary>
        internal const string AnyScreen = "*";

        /// <summary>
        /// Tên element của ô cột "Id loại" — cột khai cứng 140px trong <c>EventTypeTable</c>. Đọc THẲNG hằng của sản phẩm,
        /// không chép chuỗi: bảng miễn trừ trỏ vào một tên không còn tồn tại thì nó im lặng không tha gì cả, và chỗ hỏng
        /// chỉ lộ ra ở một con số đỏ tăng lên mà không ai hiểu vì sao.
        /// </summary>
        internal const string EventTypesTypeIdCellName = EventTypeTable.TypeIdCellName;

        /// <summary>Tên element của ô cột "Config key" — cột khai cứng 140px trong <c>EventTypeTable</c>.</summary>
        internal const string EventTypesConfigKeyCellName = EventTypeTable.ConfigKeyCellName;

        private static readonly UxTruncationExemptionEntry[] Entries =
        {
            new UxTruncationExemptionEntry(AnyScreen, EventTypesTypeIdCellName,
                UxTruncatableCellKind.FixedWidthTableCell, EventTypeInspectorTypeIdSurface,
                nameof(UxTruncationExemptionTests.EventTypesTypeIdCell_TruncatesWithTooltipAndInspectorShowsFullText),
                "Cột 'Id loại' khai cứng 140px (EventTypeTable.AddColumns) nên id dài hơn thế KHÔNG thể vừa ô ở bất kỳ cỡ "
                + "cửa sổ nào. Chữ đủ đọc lại ở tooltip của chính ô và ở ô nhập 'Id loại' của inspector bên phải — đúng chỗ "
                + "[SD1] §2.1 đặt chi tiết."),
            new UxTruncationExemptionEntry(AnyScreen, EventTypesConfigKeyCellName,
                UxTruncatableCellKind.FixedWidthTableCell, EventTypeInspectorConfigKeySurface,
                nameof(UxTruncationExemptionTests.EventTypesConfigKeyCell_TruncatesWithTooltipAndInspectorShowsFullText),
                "Cột 'Config key' khai cứng 140px và là cột PHỤ (bị bỏ hẳn ở cửa sổ hẹp). Chữ đủ đọc lại ở tooltip của ô và "
                + "ở ô nhập 'Config key' của inspector."),
            new UxTruncationExemptionEntry(AnyScreen, TimelineBarSelector,
                UxTruncatableCellKind.GeometryDrivenBarLabel, CalendarInspectorTitleIdSurface,
                nameof(UxTruncationExemptionTests.TimelineBarLabel_ShortensWithTooltipAndInspectorShowsFullText),
                "Bề rộng thanh đợt là HÌNH HỌC suy từ dữ liệu (khoảng thời gian × tỉ lệ zoom), không phải bề rộng do bố "
                + "cục chọn — một đợt ngắn ở mức zoom rộng chỉ có vài pixel ở MỌI cỡ cửa sổ. Chữ đủ đọc lại ở tooltip của "
                + "chính thanh (id + khoảng UTC) và ở dòng tiêu đề inspector sau khi chọn thanh. LƯU Ý: mục này KHÔNG tha "
                + "cho dòng tiêu đề inspector — trên dữ liệu dài nhất, chính dòng ấy cũng bị pane cắt, và chỗ cắt đó vẫn "
                + "là một phát hiện ĐỎ nằm trong nợ W11-01. Miễn trừ ở đây chỉ nói về nhãn trên thanh."),
        };

        /// <summary>
        /// Selector của mục J2-03 là class của CẢ THANH chứ không phải class của nhãn, vì tooltip nằm trên thanh còn nhãn
        /// thì không mang tooltip nào (<c>LiveOpsTimelineBar</c> đặt <c>pickingMode = Ignore</c> cho nhãn để chuột vẫn bắt
        /// vào thân thanh). <c>TooltipWithin</c> dừng ở ô đã khớp, nên khai nhãn là tự cắt mất đường đọc tooltip.
        /// <para>
        /// Khai cả thanh KHÔNG tha rộng hơn ý định: trong một thanh chỉ có đúng một phần tử mang chữ — cái nhãn này. Mọi
        /// phần con khác (dải màu, icon, vuông "khác bản đã đăng", gạch bị bỏ, dấu cắt mép, vùng bắt mép) không có chữ nên
        /// không bao giờ sinh ra phát hiện chữ.
        /// </para>
        /// </summary>
        internal const string TimelineBarSelector = LiveOpsHubClassNames.TimelineBar;

        /// <summary>Class của dòng id trên tiêu đề inspector Lịch — chỗ khai cho điều kiện (b) của mục J2-03.</summary>
        internal const string CalendarInspectorTitleIdSurface = LiveOpsHubClassNames.CalendarInspectorTitleId;

        /// <summary>Tên element ô nhập "Id loại" của inspector Loại event — chỗ khai cho điều kiện (b).</summary>
        internal const string EventTypeInspectorTypeIdSurface = EventTypeInspector.TypeIdFieldName;

        /// <summary>Tên element ô nhập "Config key" của inspector Loại event — chỗ khai cho điều kiện (b).</summary>
        internal const string EventTypeInspectorConfigKeySurface = EventTypeInspector.ConfigKeyFieldName;

        /// <summary>Mọi mục — dùng cho ca tự kiểm bảng và cho báo cáo của cổng người.</summary>
        public static IReadOnlyList<UxTruncationExemptionEntry> All => Entries;

        /// <summary>
        /// Phát hiện <paramref name="kind"/> trên <paramref name="element"/> của màn <paramref name="screenId"/> có được miễn
        /// theo luật rút gọn có điều kiện không. <paramref name="note"/> nói VÌ SAO khi trả false trên một mục có khai — câu
        /// ấy đi thẳng vào dòng chẩn đoán để người đọc biết mục tồn tại mà điều kiện (a) không đạt.
        /// </summary>
        public static bool Allows(string screenId, string kind, VisualElement element, out string note)
        {
            note = string.Empty;
            if (element == null) return false;
            bool isTextCut = string.Equals(kind, UxLayoutFindingKinds.TextCut, StringComparison.Ordinal);
            bool isTextTight = string.Equals(kind, UxLayoutFindingKinds.TextTight, StringComparison.Ordinal);
            // Luật của user nói về CHỮ trong ô: tràn con, chồng anh em, cột không tên… không nằm trong quyết định ấy và
            // không được mượn nó để im lặng.
            if (!isTextCut && !isTextTight) return false;
            foreach (UxTruncationExemptionEntry entry in Entries)
            {
                if (!string.Equals(entry.ScreenId, AnyScreen, StringComparison.Ordinal)
                    && !string.Equals(entry.ScreenId, screenId, StringComparison.Ordinal)) continue;
                VisualElement cell = MatchedCell(entry.Selector, element);
                if (cell == null) continue;
                // W9-25 được MIỄN hẳn cho hai loại ô này: ở đó không có chữ nào bị giấu đi, chỉ có khoảng dư mỏng — mà khoảng
                // dư mỏng trong một ô có bề rộng CỐ ĐỊNH là điều đương nhiên, không phải rủi ro cắt im lặng của W9-19.
                if (isTextTight) return true;
                if (CarriesFullTextTooltip(element, cell, out string missing)) return true;
                note = "mục miễn trừ có điều kiện '" + entry.Selector + "' KHÔNG áp dụng: " + missing;
                return false;
            }
            return false;
        }

        /// <summary>
        /// Điều kiện (a) đo được: ô (hoặc phần tử chữ bên trong nó) đang mang tooltip chứa ĐỦ chữ.
        /// <para>
        /// Hai đường rút gọn cần hai phép so khác nhau. UI Toolkit tự rút (<c>isElided</c>) hoặc cha cắt cứng thì
        /// <c>TextElement.text</c> vẫn là chuỗi ĐẦY ĐỦ — tooltip chỉ cần chứa nó. Hub tự rút (nhãn thanh trục) thì chuỗi
        /// trong element đã mất đuôi và chỉ còn "…" — lúc ấy phải hỏi tooltip có dài HƠN phần còn lại không, vì chuỗi đầy đủ
        /// không còn nằm ở đâu trong cây để so.
        /// </para>
        /// </summary>
        internal static bool CarriesFullTextTooltip(VisualElement element, VisualElement cell, out string missing)
        {
            string tooltip = TooltipWithin(element, cell);
            if (tooltip.Length == 0)
            {
                missing = "ô không mang tooltip nào, nên chữ bị rút không còn đường đọc lại";
                return false;
            }
            string shown = element is TextElement text ? text.text ?? string.Empty : string.Empty;
            if (shown.Length == 0)
            {
                missing = "phần tử không có chữ để đối chiếu với tooltip";
                return false;
            }
            int ellipsis = shown.IndexOf('…');
            if (ellipsis < 0)
            {
                if (tooltip.IndexOf(shown, StringComparison.Ordinal) >= 0)
                {
                    missing = string.Empty;
                    return true;
                }
                missing = "tooltip '" + tooltip + "' không chứa đủ chữ của ô '" + shown + "'";
                return false;
            }
            string kept = shown.Substring(0, ellipsis);
            if (tooltip.Length > kept.Length && tooltip.StartsWith(kept, StringComparison.Ordinal))
            {
                missing = string.Empty;
                return true;
            }
            missing = "tooltip '" + tooltip + "' không phải bản đầy đủ của chuỗi đã rút '" + shown + "'";
            return false;
        }

        /// <summary>
        /// Tooltip gần nhất từ <paramref name="element"/> đi lên, DỪNG ở <paramref name="cell"/>. Không đi tiếp lên trên ô:
        /// tooltip của cả bảng hay cả hàng nói chuyện khác, mượn nó để tha một ô là tha rộng hơn ý định.
        /// </summary>
        private static string TooltipWithin(VisualElement element, VisualElement cell)
        {
            for (VisualElement node = element; node != null; node = node.hierarchy.parent)
            {
                string tooltip = node.tooltip ?? string.Empty;
                if (tooltip.Length > 0) return tooltip;
                if (node == cell) break;
            }
            return string.Empty;
        }

        /// <summary>Ô khớp selector: chính phần tử bị phát hiện, hoặc tổ tiên gần nhất khớp (chữ của ô nhập nằm sâu bên trong).</summary>
        private static VisualElement MatchedCell(string selector, VisualElement element)
        {
            for (VisualElement node = element; node != null; node = node.hierarchy.parent)
            {
                if (UxLayoutAllowList.Identifies(selector, node)) return node;
            }
            return null;
        }
    }
}
