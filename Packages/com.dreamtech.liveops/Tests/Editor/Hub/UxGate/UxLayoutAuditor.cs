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

        /// <summary>Chữ CHƯA cắt nhưng chiếm gần hết bề rộng ô — LỖI của W9-25, xem <see cref="UxLayoutAuditor.TextFillLimitRatio"/>.</summary>
        internal const string TextTight = "textTight";
        internal const string ChildOverflow = "childOverflow";

        /// <summary>
        /// Một cột của bảng (<c>MultiColumnListView</c>) đang hiện mà đầu cột KHÔNG có chữ — xem
        /// <see cref="UxLayoutAuditor.CheckColumnHeaderTitle"/>. Đây là lưới bắt hạng lỗi của phiếu W9-31.
        /// </summary>
        internal const string UntitledColumn = "untitledColumn";
        internal const string ScrollViews = "scrollViews";
        internal const string AbsoluteOverText = "absoluteOverText";
        internal const string SiblingOverlap = "siblingOverlap";
        internal const string NotStretched = "notStretched";
        internal const string MissingElement = "missingElement";
        internal const string LowContrast = "lowContrast";
        internal const string PairOverlap = "pairOverlap";
    }

    /// <summary>
    /// Một câu chẩn đoán CÙNG element sinh ra nó. Giữ element là điều kiện của hai việc mà bản đầu tiên của cổng không làm được:
    /// (1) lọc phát hiện theo MỘT nhánh cây (luật của màn chỉ nói về inspector, về toolbar…), (2) so khớp danh sách miễn trừ theo
    /// đúng element bị phát hiện chứ không theo cả câu (câu có kèm mô tả cha và chữ của element, dễ tha nhầm).
    /// </summary>
    internal readonly struct UxLayoutFinding
    {
        public UxLayoutFinding(VisualElement owner, string text)
        {
            Owner = owner;
            Text = text;
        }

        /// <summary>Element bị phát hiện (không phải cha, không phải element được so cùng).</summary>
        public VisualElement Owner { get; }

        public string Text { get; }
    }

    /// <summary>
    /// Một luật "vùng vẽ phải giãn" (§3.3 mục e): phần tử <see cref="ChildSelector"/> phải chiếm ít nhất
    /// <see cref="MinimumRatio"/> bề rộng (hoặc chiều cao) của <see cref="ParentSelector"/>. Đây là loại lỗi mà đo khung tuyệt đối
    /// của ma trận ảnh KHÔNG bắt được: ở 1920 thân màn cao 900 vẫn "đẹp" trong khi nó chỉ lấp 40% chiều cao có thể dùng.
    /// </summary>
    internal readonly struct UxLayoutStretchRule
    {
        public UxLayoutStretchRule(string childSelector, string parentSelector, bool vertical, float minimumRatio)
            : this(childSelector, parentSelector, vertical, minimumRatio, 0f)
        {
        }

        /// <param name="reservedParentSize">
        /// Số px mà THIẾT KẾ đã dành sẵn trong cha cho một thứ khác, nên con không bao giờ với tới được (vd header làn 168px
        /// của timeline [SD1 §3.2]: cột = header làn + track). Tỉ lệ tính trên phần cha CÒN LẠI; để 0 khi cả cha là chỗ của con.
        /// Không có tham số này thì luật "track lấp ≥ 95% cột" là luật không bao giờ đạt được, và một luật không bao giờ đạt
        /// không phân biệt nổi giao diện đúng với giao diện hỏng.
        /// </param>
        public UxLayoutStretchRule(string childSelector, string parentSelector, bool vertical, float minimumRatio,
            float reservedParentSize)
        {
            ChildSelector = childSelector;
            ParentSelector = parentSelector;
            Vertical = vertical;
            MinimumRatio = minimumRatio;
            ReservedParentSize = reservedParentSize;
        }

        /// <summary>Tên element ("#tên") hoặc class USS — <see cref="UxLayoutAuditor.FindBySelector"/> hiểu cả hai.</summary>
        public string ChildSelector { get; }

        public string ParentSelector { get; }

        /// <summary>true = so chiều cao (thân màn), false = so bề rộng (track thước).</summary>
        public bool Vertical { get; }

        public float MinimumRatio { get; }

        /// <summary>Phần bề rộng/chiều cao của cha mà thiết kế dành cho thứ khác — xem tham số của hàm dựng.</summary>
        public float ReservedParentSize { get; }
    }

    /// <summary>
    /// Một luật "dấu màu phải đọc được" (UX-19): mọi element khớp <see cref="Selector"/> phải tương phản ít nhất
    /// <see cref="MinimumRatio"/> so với nền ngay sau nó. Đo bằng công thức độ sáng tương đối WCAG trên màu ĐÃ RESOLVE của
    /// skin đang chạy, nên một lượt ở dark và một lượt ở light nói được hai chuyện khác nhau.
    /// </summary>
    internal readonly struct UxLayoutContrastRule
    {
        public UxLayoutContrastRule(string selector, float minimumRatio, IReadOnlyList<string> requiredVariantSelectors)
        {
            Selector = selector;
            MinimumRatio = minimumRatio;
            RequiredVariantSelectors = requiredVariantSelectors;
        }

        public string Selector { get; }
        public float MinimumRatio { get; }

        /// <summary>
        /// Selector của TỪNG LOẠI dấu mà luật này phải đo được ít nhất một element (chú giải trục: cố định / lặp / đã khép /
        /// chồng nhau). Vì sao khai danh sách selector chứ không khai một con số tổng rồi so "≥": đếm tổng không phân biệt nổi
        /// "đủ bốn loại" với "một loại xuất hiện bốn lần". Lượt đo thật thấy BẢY element khớp selector chung (dấu "cố định" lặp
        /// lại ở nhiều chỗ), nên ngưỡng tổng 4 vẫn cho màu XANH ngay cả khi dấu "chồng nhau" — dấu MỜ NHẤT, dấu duy nhất khai
        /// màu có alpha, và đúng dấu vừa lộ ra lỗi hợp thành — biến mất khỏi phép đo (RC-06/2.4; phát hiện A-02).
        /// </summary>
        public IReadOnlyList<string> RequiredVariantSelectors { get; }
    }

    /// <summary>
    /// Luật "hai khối này KHÔNG được chồng nhau" (UX-11 toast đè chân trang, UX-20 status bar, UX-24 header Luật lặp). Khác
    /// <c>siblingOverlap</c> ở chỗ hai element không cần cùng cha: toast là lớp nổi của vùng nội dung còn chân trang là con của
    /// khung, nên phép so anh-em không bao giờ gặp chúng trong một lượt.
    /// </summary>
    internal readonly struct UxLayoutNoOverlapRule
    {
        public UxLayoutNoOverlapRule(string firstSelector, string secondSelector)
        {
            FirstSelector = firstSelector;
            SecondSelector = secondSelector;
        }

        public string FirstSelector { get; }
        public string SecondSelector { get; }
    }

    /// <summary>
    /// Luật riêng của một màn cho một lượt kiểm. Gói lại thành một đối tượng thay vì năm tham số rời vì bảng màn của
    /// <see cref="UxLayoutAuditTests"/> khai thiếu một luật là im lặng bỏ qua cả một yêu cầu của §3.3 — có tên trường thì đọc
    /// bảng đó thấy ngay chỗ nào chưa khai.
    /// </summary>
    internal sealed class UxLayoutRules
    {
        public UxLayoutRules(IReadOnlyList<string> requiredElements, IReadOnlyList<UxLayoutStretchRule> stretchRules,
            IReadOnlyList<UxLayoutContrastRule> contrastRules, IReadOnlyList<UxLayoutNoOverlapRule> noOverlapRules,
            IReadOnlyList<string> subtreeSelectors, bool screenRulesOnly)
        {
            RequiredElements = requiredElements;
            StretchRules = stretchRules;
            ContrastRules = contrastRules;
            NoOverlapRules = noOverlapRules;
            SubtreeSelectors = subtreeSelectors;
            ScreenRulesOnly = screenRulesOnly;
        }

        /// <summary>Phần tử quan trọng của màn (§3.3 mục a) — thiếu hoặc không dùng được là mất lối vào.</summary>
        public IReadOnlyList<string> RequiredElements { get; }

        public IReadOnlyList<UxLayoutStretchRule> StretchRules { get; }
        public IReadOnlyList<UxLayoutContrastRule> ContrastRules { get; }
        public IReadOnlyList<UxLayoutNoOverlapRule> NoOverlapRules { get; }

        /// <summary>
        /// Nhánh cây mà phát hiện chung (chữ cắt, con tràn, anh em chồng…) PHẢI sạch. Rỗng = không giới hạn nhánh nào.
        /// </summary>
        public IReadOnlyList<string> SubtreeSelectors { get; }

        /// <summary>
        /// true = câu assert chỉ nói về luật của màn (thiếu phần tử / không giãn / tương phản) và về các nhánh khai ở
        /// <see cref="SubtreeSelectors"/>. Phát hiện chung của phần còn lại vẫn ghi JSON nhưng không làm test đỏ — nếu không,
        /// một test tự nhận "tách một nguyên nhân" lại đỏ vì 225 chỗ của cả cửa sổ và không còn là tín hiệu của nguyên nhân nào.
        /// </summary>
        public bool ScreenRulesOnly { get; }
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

        /// <summary>Khung THẬT của cửa sổ sau khi hệ điều hành đã kẹp — so với <see cref="Size"/> mới biết cỡ này có được đo đúng không.</summary>
        public Rect WindowPosition { get; set; }

        /// <summary>true = hệ điều hành kẹp cửa sổ nhỏ hơn cỡ yêu cầu; lượt vẫn chạy nhưng cỡ này CHƯA được đo đúng (R-08).</summary>
        public bool Clamped { get; set; }

        public string ClampNote { get; set; } = string.Empty;

        public List<UxLayoutFinding> ZeroSizeNamed { get; } = new List<UxLayoutFinding>();
        public List<UxLayoutFinding> TextCut { get; } = new List<UxLayoutFinding>();

        /// <summary>Chữ vừa đủ lọt ô nhưng khoảng dư dưới ngưỡng (W9-25).</summary>
        public List<UxLayoutFinding> TextTight { get; } = new List<UxLayoutFinding>();
        public List<UxLayoutFinding> ChildOverflow { get; } = new List<UxLayoutFinding>();

        /// <summary>Cột bảng đang hiện mà đầu cột không có chữ (W9-31).</summary>
        public List<UxLayoutFinding> UntitledColumn { get; } = new List<UxLayoutFinding>();

        /// <summary>MỌI ScrollView của màn (chẩn đoán), kể cả cái không có vấn đề — chỉ dòng mang dấu hiệu mới tính là lỗi.</summary>
        public List<UxLayoutFinding> ScrollViews { get; } = new List<UxLayoutFinding>();

        public List<UxLayoutFinding> AbsoluteOverText { get; } = new List<UxLayoutFinding>();
        public List<UxLayoutFinding> SiblingOverlap { get; } = new List<UxLayoutFinding>();
        public List<UxLayoutFinding> NotStretched { get; } = new List<UxLayoutFinding>();
        public List<UxLayoutFinding> MissingElement { get; } = new List<UxLayoutFinding>();
        public List<UxLayoutFinding> LowContrast { get; } = new List<UxLayoutFinding>();

        /// <summary>Hai khối mà luật của màn cấm chồng nhau (toast × chân trang…) — luật của MÀN, khác siblingOverlap tự dò.</summary>
        public List<UxLayoutFinding> PairOverlap { get; } = new List<UxLayoutFinding>();

        /// <summary>
        /// Số dòng THẬT của loại đã chạm trần <see cref="UxLayoutAuditor.MaximumEntriesPerKind"/>. Không ghi lại thì JSON in ra
        /// con số đã bị cắt mà không có dấu hiệu nào, và báo cáo trước/sau so hai con số đều sai (R-14).
        /// </summary>
        public Dictionary<string, int> TruncatedCounts { get; } = new Dictionary<string, int>();

        /// <summary>Tên file JSON: <c>&lt;màn&gt;-&lt;cỡ&gt;-&lt;ngôn ngữ&gt;</c> đúng như §3.3.</summary>
        public string FileStem =>
            ScreenId + "-" + Size + "-" + (Language == LiveOpsHubLanguageId.Vietnamese ? "vi" : "en");

        /// <summary>Những dòng tính là LỖI trên TOÀN cửa sổ (ScrollView chỉ tính khi mang dấu hiệu tràn/cuộn ngang).</summary>
        public List<string> Problems()
        {
            List<string> problems = new List<string>();
            AppendScreenRuleProblems(problems);
            AppendGeneralProblems(problems, null);
            return problems;
        }

        /// <summary>
        /// Những dòng mà câu assert của MÀN được phép đỏ vì, theo <paramref name="rules"/>: luật của màn luôn tính; phát hiện
        /// chung tính trên cả cửa sổ, hoặc chỉ trên các nhánh khai ở <see cref="UxLayoutRules.SubtreeSelectors"/>.
        /// </summary>
        public List<string> AssertProblems(UxLayoutRules rules, IReadOnlyList<VisualElement> subtreeRoots)
        {
            List<string> problems = new List<string>();
            AppendScreenRuleProblems(problems);
            if (rules != null && rules.ScreenRulesOnly)
            {
                if (subtreeRoots != null && subtreeRoots.Count > 0) AppendGeneralProblems(problems, subtreeRoots);
                return problems;
            }
            AppendGeneralProblems(problems, subtreeRoots);
            return problems;
        }

        private void AppendScreenRuleProblems(List<string> problems)
        {
            AppendProblems(problems, UxLayoutFindingKinds.MissingElement, MissingElement, null);
            AppendProblems(problems, UxLayoutFindingKinds.NotStretched, NotStretched, null);
            AppendProblems(problems, UxLayoutFindingKinds.LowContrast, LowContrast, null);
            AppendProblems(problems, UxLayoutFindingKinds.PairOverlap, PairOverlap, null);
        }

        private void AppendGeneralProblems(List<string> problems, IReadOnlyList<VisualElement> subtreeRoots)
        {
            AppendProblems(problems, UxLayoutFindingKinds.TextCut, TextCut, subtreeRoots);
            // W9-25 (USER chốt 20/9/2026): "chữ chiếm hơn 95% bề rộng ô" nay là LỖI, không còn là cảnh báo in ra rồi thôi.
            // Vì sao nâng: bảy chỗ của bản cũ đều thiếu 1–6px khoảng dư, tức chúng CHƯA cắt chỉ nhờ metric font của đúng
            // hai bản Unity đang chạy — một đổi DPI, đổi cỡ chữ Editor hay đổi bản Unity là cắt IM LẶNG, đúng cách W9-19
            // đã lọt. Một cảnh báo không ai buộc phải đọc thì không phải là một cái lưới.
            AppendProblems(problems, UxLayoutFindingKinds.TextTight, TextTight, subtreeRoots);
            AppendProblems(problems, UxLayoutFindingKinds.ChildOverflow, ChildOverflow, subtreeRoots);
            // W9-31: cột không tên là lỗi CẤU TRÚC của bảng, không phải lỗi của một chuỗi — nó nằm ở đây cùng các phát hiện
            // chung để MỌI màn có bảng đều đi qua lưới, chứ không chỉ màn nào nhớ khai luật.
            AppendProblems(problems, UxLayoutFindingKinds.UntitledColumn, UntitledColumn, subtreeRoots);
            AppendProblems(problems, UxLayoutFindingKinds.AbsoluteOverText, AbsoluteOverText, subtreeRoots);
            AppendProblems(problems, UxLayoutFindingKinds.SiblingOverlap, SiblingOverlap, subtreeRoots);
            foreach (UxLayoutFinding finding in ScrollViews)
            {
                if (!UxLayoutAuditor.IsScrollViewProblem(finding.Text)) continue;
                if (!UxLayoutAuditor.IsInsideAny(finding.Owner, subtreeRoots)) continue;
                problems.Add(UxLayoutFindingKinds.ScrollViews + ": " + finding.Text);
            }
        }

        private static void AppendProblems(List<string> problems, string kind, List<UxLayoutFinding> entries,
            IReadOnlyList<VisualElement> subtreeRoots)
        {
            foreach (UxLayoutFinding finding in entries)
            {
                if (!UxLayoutAuditor.IsInsideAny(finding.Owner, subtreeRoots)) continue;
                problems.Add(kind + ": " + finding.Text);
            }
        }
    }

    /// <summary>
    /// Kiểm bố cục tự động của cổng W8-UX (§3.3): đi hết cây element của một cửa sổ và ghi ra mọi chỗ NGƯỜI DÙNG không dùng được —
    /// phần tử quan trọng cao/rộng 0, chữ bị cắt, con tràn khỏi cha, nội dung vượt khung mà không cuộn được, vùng vẽ không giãn
    /// theo cửa sổ, dấu màu không đủ tương phản.
    /// <para>
    /// Vì sao cần: 37 lỗi của SCHEDULE-UI-AUDIT và 26 lỗi hành trình đều nhìn thấy được trên ẢNH, nhưng 900 test xanh không bắt
    /// được cái nào — test cũ đo từng khung tuyệt đối theo hình thiết kế ở MỘT cỡ cửa sổ. Ở đây không đo con số thiết kế: chỉ hỏi
    /// "chữ này có đọc được không", "ô này có bấm được không", ở CẢ SÁU cỡ, nên lỗi kiểu "820 màn trắng" không lọt nữa.
    /// </para>
    /// <para>
    /// Chữ bị cắt đo bằng <c>MeasureTextSize</c> + <c>isElided</c> (reflection: cả hai đổi chỗ giữa 2022.3 và 6000.6). Cắt CÓ CHỦ
    /// ĐÍCH khai ở <see cref="UxLayoutAllowList"/> kèm lý do — allow-list là nơi DUY NHẤT được phép im lặng. Mọi đường reflection
    /// đều có hàm <c>CanX</c> để <c>UxGateSelfCheckTests</c> khẳng định nó tra được trên bản Unity đang chạy: "hỏng thì im lặng"
    /// không được phép biến cổng hỏng thành cổng xanh.
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

        /// <summary>Dưới mức alpha này thì nền coi như trong suốt — không có gì để đo tương phản.</summary>
        private const float MinimumMeasurableAlpha = 0.05f;

        /// <summary>Kênh màu của nền cửa sổ Editor skin tối (#383838) — xem <see cref="WindowBackground"/>.</summary>
        private const float DarkWindowBackgroundChannel = 56f / 255f;

        /// <summary>Kênh màu của nền cửa sổ Editor skin sáng (#C8C8C8) — xem <see cref="WindowBackground"/>.</summary>
        private const float LightWindowBackgroundChannel = 200f / 255f;

        /// <summary>
        /// Sai số RIÊNG cho nhánh đo chữ. <c>MeasureTextSize</c> làm tròn theo atlas font và theo pixelsPerPoint, nên nó lệch
        /// nhiều hơn số đo layout: lượt 6000.6 có 112/938 dòng textCut thiếu ≤ 3 px (kiểu "cần 36 có 34") — đó là nhiễu của phép
        /// đo, không phải chữ người dùng đọc thiếu (R-18). Ngưỡng này KHÔNG dùng cho childOverflow: ở đó 2 px là tràn thật.
        /// </summary>
        private const float TextMeasureTolerance = 3f;

        /// <summary>Chồng lấn dưới mức này là viền/đường kẻ chạm nhau, không phải hai khối đè lên nhau.</summary>
        private const float OverlapTolerance = 2f;

        /// <summary>
        /// Chữ chiếm quá tỉ lệ này của bề rộng ô thì báo <see cref="UxLayoutFindingKinds.TextTight"/> (W9-25) — từ 20/9/2026
        /// đây là LỖI làm đỏ cổng, không còn là cảnh báo.
        /// <para>
        /// Vì sao cần một luật RIÊNG bên cạnh "đã cắt chưa": lượt 3 của đợt W8 chữa ô id bị cắt bằng cách nhường đệm ngang
        /// về 0, nên chuỗi mono <c>2026-09-10</c> (75px trên 2022.3) còn ĐÚNG 0,5px dư mỗi bên trong ô pin 76px. Cổng cũ
        /// xanh — vì chưa cắt — nhưng một đổi metric font bất kỳ (bản Unity khác, DPI khác, cỡ chữ Editor khác) là cắt lại
        /// IM LẶNG. Ngưỡng này biến "còn dư bao nhiêu" thành thứ đo được, tức là cái lưới đáng lẽ đã bắt W9-19 từ trong đợt.
        /// </para>
        /// <para>
        /// 95% chọn theo [SD1]: ô nào cũng còn ít nhất một nửa đệm thiết kế mỗi bên thì còn chịu được một lần đổi metric.
        /// Dưới ngưỡng này KHÔNG báo gì — đây là cảnh báo về khoảng dư, không phải một ngưỡng bố cục mới.
        /// </para>
        /// <para>
        /// LƯỚI NÀY HẸP HƠN TÊN CỦA NÓ, khai thẳng để lần sau không ai đọc thành "mọi chữ sát mép ô đều bị bắt" (soát W10
        /// R-06). Nó CHỈ nói về ô rộng từ <see cref="TextFillMinimumWidth"/> px trở lên, chữ KHÔNG xuống dòng, CHƯA rút gọn
        /// (không có "…", không <c>isElided</c>), và khoảng dư LỚN HƠN <see cref="TextFillMinimumSlack"/> px. Nghĩa là nhãn
        /// ôm KHÍT chữ (dư 0, lấp đúng 100%) KHÔNG làm đỏ — đó là nhãn TỰ CO, chữ dài thêm thì ô dài theo. Khoảng bắt thật
        /// của luật vì vậy là 95%–~99% của một ô rộng ≥ 60px. Muốn bắt cả nhãn tự co thì phải hỏi một câu KHÁC ("ô có chỗ
        /// để nở không"), không phải hạ ba ngưỡng này.
        /// </para>
        /// </summary>
        internal const float TextFillLimitRatio = 0.95f;

        /// <summary>
        /// Ô hẹp hơn mức này không áp luật dư 5%: 5% của một ô 20px là 1px, mỏng hơn cả sai số của <c>MeasureTextSize</c>
        /// (<see cref="TextMeasureTolerance"/> = 3px), nên mọi icon và mọi ô một ký tự sẽ báo "chật" mà không nói lên điều gì.
        /// </summary>
        private const float TextFillMinimumWidth = 60f;

        /// <summary>
        /// Khoảng dư phải LỚN HƠN ngần này pixel thì mới được coi là "ô rộng hơn chữ". Dưới mức đó nghĩa là ô đang ôm khít
        /// chữ — ô TỰ CO theo nội dung.
        /// <para>
        /// Vì sao phải có vế này (đo được ở hai lượt đầu của W9-25): phần lớn <c>Label</c> của hub tự co, nên chúng rộng
        /// ĐÚNG bằng chữ của mình ("cần 74 có 74, dư 0") và tỉ lệ lấp luôn là 100%. Luật "chữ chiếm > 95% bề rộng ô" một
        /// mình vì vậy báo 1 500+ chỗ ở lượt đầu, gần như toàn bộ là nhãn tự co — một cảnh báo nổ ở mọi nhãn thì không ai
        /// đọc nữa, tức cái lưới sinh ra để bắt W9-19 lại tự vô hiệu hoá mình. (Lượt hai thử phân biệt bằng "hàng chứa còn
        /// thừa bao nhiêu" và KHÔNG ăn thua: hàng nào có một khối giãn thì tổng bề rộng con luôn bằng bề rộng hàng, nên chỗ
        /// thừa đo được là 0 kể cả khi nhãn nở ra thoải mái.)
        /// </para>
        /// <para>
        /// Phân biệt đúng chỗ là nhìn vào KHOẢNG DƯ, đúng như tên phiếu: ô tự co có dư 0 — chữ dài thêm thì ô dài theo, không
        /// bao giờ cắt im lặng. Ô có rủi ro là ô rộng HƠN chữ nhưng chỉ hơn một chút: ô ngày 76px của W9-19 ôm chuỗi mono
        /// 75px, dư đúng 1px, và một đổi metric font là cắt. Luật đọc thành một câu: "ô rộng hơn chữ, nhưng dư dưới 5%".
        /// </para>
        /// <para>
        /// 0,5px vì <c>MeasureTextSize</c> và layout đều làm tròn theo pixelsPerPoint: dưới nửa pixel thì "dư" là nhiễu của
        /// phép đo chứ không phải chỗ trống có thật.
        /// </para>
        /// </summary>
        private const float TextFillMinimumSlack = 0.5f;

        /// <summary>
        /// Ký tự "…" mà hub tự đặt vào một nhãn ĐÃ RÚT GỌN (nhãn thanh trục rút id theo bề rộng thanh). Chuỗi đã rút thì
        /// luật dư 5% không có nghĩa gì: bề rộng đo được là bề rộng của bản ĐÃ CẮT, nên nó luôn vừa khít ô theo đúng thiết
        /// kế, và "còn dư mấy pixel" không nói gì về chuỗi thật. Câu hỏi đúng cho nhãn như vậy là câu của W9-23 — mẩu còn
        /// lại có đọc ra id không — chứ không phải câu của W9-25.
        /// </summary>
        private const char EllipsisCharacter = '\u2026';

        internal const string ScrollViewOverflowVerticalMark = "TRÀN DỌC KHÔNG CÓ THANH CUỘN";
        internal const string ScrollViewOverflowHorizontalMark = "TRÀN NGANG KHÔNG CÓ THANH CUỘN";
        internal const string ScrollViewHorizontalBarMark = "CÓ THANH CUỘN NGANG";

        /// <summary>Hàm nội bộ của UI Toolkit trả lời "element này có cắt con của nó không" — có ở CẢ 2022.3 lẫn 6000.6.</summary>
        private const string ShouldClipMethodName = "ShouldClip";

        /// <summary>Kiểu nội bộ của UI Toolkit cho ĐẦU MỘT CỘT của <c>MultiColumnListView</c> — có ở CẢ 2022.3 lẫn 6000.6.</summary>
        private const string MultiColumnHeaderColumnTypeName = "MultiColumnHeaderColumn";

        private static MethodInfo _measureTextSize;
        private static PropertyInfo _isElided;
        private static MethodInfo _shouldClip;

        /// <summary>
        /// Duyệt cây của <paramref name="window"/> và trả kết quả theo <paramref name="rules"/> (phần tử bắt buộc, luật giãn,
        /// luật tương phản).
        /// </summary>
        public static UxLayoutAuditResult Audit(EditorWindow window, string screenId, UxWindowSize size,
            LiveOpsHubLanguageId language, UxLayoutRules rules)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            UxLayoutAuditResult result = new UxLayoutAuditResult(screenId, size, language);
            VisualElement root = window.rootVisualElement;
            result.RootBound = root.worldBound;
            result.WindowPosition = window.position;
            Visit(root, result, true);
            if (rules == null) return result;
            CheckRequiredElements(root, result, rules.RequiredElements);
            CheckStretchRules(root, result, rules.StretchRules);
            CheckContrastRules(root, result, rules.ContrastRules);
            CheckNoOverlapRules(root, result, rules.NoOverlapRules);
            return result;
        }

        /// <summary>Các nhánh cây mà luật của màn giới hạn phát hiện chung vào; null/rỗng = cả cửa sổ.</summary>
        public static List<VisualElement> ResolveSubtreeRoots(EditorWindow window, UxLayoutRules rules)
        {
            if (window == null || rules == null || rules.SubtreeSelectors == null || rules.SubtreeSelectors.Count == 0) return null;
            List<VisualElement> roots = new List<VisualElement>();
            foreach (string selector in rules.SubtreeSelectors)
            {
                VisualElement found = FindBySelector(window.rootVisualElement, selector);
                if (found != null) roots.Add(found);
            }
            return roots;
        }

        /// <summary><paramref name="element"/> nằm trong một trong các nhánh; <paramref name="roots"/> null/rỗng = không giới hạn.</summary>
        public static bool IsInsideAny(VisualElement element, IReadOnlyList<VisualElement> roots)
        {
            if (roots == null || roots.Count == 0) return true;
            if (element == null) return false;
            for (VisualElement current = element; current != null; current = current.hierarchy.parent)
            {
                for (int index = 0; index < roots.Count; index++)
                {
                    if (current == roots[index]) return true;
                }
            }
            return false;
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

        /// <summary>Mọi element khớp selector (class USS hoặc tên) — luật tương phản áp cho TẤT CẢ dấu, không chỉ cái đầu.</summary>
        public static List<VisualElement> FindAllBySelector(VisualElement root, string selector)
        {
            List<VisualElement> found = new List<VisualElement>();
            if (root == null || string.IsNullOrEmpty(selector)) return found;
            string bare = selector[0] == '.' || selector[0] == '#' ? selector.Substring(1) : selector;
            found.AddRange(root.Query(className: bare).ToList());
            if (found.Count == 0) found.AddRange(root.Query(bare).ToList());
            return found;
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
            json.Append("  \"requestedSize\": ").Append(SizeJson(result.Size)).Append(",\n");
            json.Append("  \"windowPosition\": ").Append(RectJson(result.WindowPosition)).Append(",\n");
            json.Append("  \"rootBound\": ").Append(RectJson(result.RootBound)).Append(",\n");
            json.Append("  \"clamped\": ").Append(result.Clamped ? "true" : "false").Append(",\n");
            AppendProperty(json, "clampNote", result.ClampNote ?? string.Empty);
            json.Append("  \"truncated\": ").Append(TruncatedJson(result)).Append(",\n");
            json.Append("  \"problemCount\": ").Append(result.Problems().Count.ToString(CultureInfo.InvariantCulture));
            AppendArray(json, UxLayoutFindingKinds.MissingElement, result.MissingElement);
            AppendArray(json, UxLayoutFindingKinds.ZeroSizeNamed, result.ZeroSizeNamed);
            AppendArray(json, UxLayoutFindingKinds.TextCut, result.TextCut);
            AppendArray(json, UxLayoutFindingKinds.TextTight, result.TextTight);
            AppendArray(json, UxLayoutFindingKinds.ChildOverflow, result.ChildOverflow);
            AppendArray(json, UxLayoutFindingKinds.UntitledColumn, result.UntitledColumn);
            AppendArray(json, UxLayoutFindingKinds.ScrollViews, result.ScrollViews);
            AppendArray(json, UxLayoutFindingKinds.AbsoluteOverText, result.AbsoluteOverText);
            AppendArray(json, UxLayoutFindingKinds.SiblingOverlap, result.SiblingOverlap);
            AppendArray(json, UxLayoutFindingKinds.NotStretched, result.NotStretched);
            AppendArray(json, UxLayoutFindingKinds.LowContrast, result.LowContrast);
            AppendArray(json, UxLayoutFindingKinds.PairOverlap, result.PairOverlap);
            json.Append("\n}\n");
            File.WriteAllText(path, json.ToString());
            return path;
        }

        // ================================================================================================ tự kiểm reflection

        /// <summary>Tra được <c>MeasureTextSize</c> trên bản Unity đang chạy không (R-07: hỏng thì cả loại textCut biến mất).</summary>
        public static bool CanMeasureText()
        {
            return ResolveMeasureTextSize() != null;
        }

        /// <summary>Tra được <c>isElided</c> không — mất nó thì chữ rút gọn bằng "…" không còn bị phát hiện.</summary>
        public static bool CanReadIsElided()
        {
            return ResolveIsElided() != null;
        }

        /// <summary>
        /// Hỏi được "element có cắt con không" trên bản Unity đang chạy không — mất nó thì "bị cha cắt" luôn false và
        /// <see cref="IsClippedByAncestor"/> thành code chết.
        /// </summary>
        public static bool CanDetectClipping()
        {
            return ResolveShouldClip() != null;
        }

        /// <summary>Phép đo chữ THẬT (không chỉ tra được hàm): bề rộng của một chuỗi khác rỗng phải lớn hơn 0.</summary>
        public static float MeasuredWidthOf(TextElement text, string value)
        {
            return MeasureText(text, value, float.NaN).x;
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
                        if (HasVisibleText(child)) textChildren.Add(child);
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

        /// <summary>
        /// Element có chữ NGƯỜI ĐỌC ĐƯỢC không (chính nó hoặc con trực tiếp). Ô chữ RỖNG không tính: một lớp nổi phủ lên chỗ
        /// cắm chữ còn trống không che mất thông tin nào của người dùng (R-02).
        /// </summary>
        private static bool HasVisibleText(VisualElement element)
        {
            if (element is TextElement self) return !string.IsNullOrEmpty(self.text) && self.text.Trim().Length > 0;
            int childCount = element.hierarchy.childCount;
            for (int index = 0; index < childCount && index < 12; index++)
            {
                if (element.hierarchy[index] is TextElement child && !string.IsNullOrEmpty(child.text) && child.text.Trim().Length > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static void CheckElement(VisualElement element, Rect bound, UxLayoutAuditResult result)
        {
            if (!string.IsNullOrEmpty(element.name) && !element.name.StartsWith("unity-", StringComparison.Ordinal)
                && (bound.width < 0.5f || bound.height < 0.5f))
            {
                Add(result, result.ZeroSizeNamed, UxLayoutFindingKinds.ZeroSizeNamed, element,
                    Describe(element) + " " + RectText(bound, result.RootBound));
            }
            if (element is TextElement text && !string.IsNullOrEmpty(text.text) && bound.width > 0f) CheckTextCut(text, bound, result);
            if (element is ScrollView scrollView) CheckScrollView(scrollView, result);
            if (IsTableColumnHeader(element)) CheckColumnHeaderTitle(element, bound, result);
        }

        /// <summary>
        /// Element này có phải ĐẦU MỘT CỘT của <c>MultiColumnListView</c> không. Nhận theo TÊN KIỂU chứ không theo class USS:
        /// <c>MultiColumnHeaderColumn</c> có ở cả 2022.3 lẫn 6000.6, còn class <c>unity-multi-column-header__column</c> CHỈ có
        /// ở 6000.6 (2022.3 đặt tên khác), nên bắt theo class là mất nửa lưới ở đúng bản Unity mà 55 trong 62 chỗ của W9-25
        /// từng sống.
        /// </summary>
        private static bool IsTableColumnHeader(VisualElement element)
        {
            return string.Equals(element.GetType().Name, MultiColumnHeaderColumnTypeName, StringComparison.Ordinal);
        }

        /// <summary>
        /// (W9-31) Cột đang HIỆN thì đầu cột phải có chữ. Một cột không tên là một cột người đọc không giải thích được: nó có
        /// đường kẻ chia cột riêng, chiếm bề rộng của các cột khác, và ở mẫu dữ liệu nào không vẽ ô nào thì nó đọc thành
        /// "bảng vỡ cột" — đúng thứ báo cáo hành trình W9 thấy trên ảnh mà 24 màn của ma trận không hề đỏ.
        /// <para>
        /// Vì sao là LƯỚI chứ không phải luật khai theo màn: cột không tên dựng được ở bất kỳ bảng nào (hub có bốn
        /// <c>MultiColumnListView</c>), nên luật phải đi theo CẤU TRÚC bảng, không theo trí nhớ của người viết màn. Chỗ cắt
        /// có chủ đích vẫn khai ở <see cref="UxLayoutAllowList"/> như mọi loại khác.
        /// </para>
        /// <para>
        /// Đầu cột thường mang thêm icon sắp xếp, nên câu hỏi đúng là "có đoạn chữ NÀO đọc được trong đầu cột không", không
        /// phải "Label thứ nhất có chữ không".
        /// </para>
        /// </summary>
        private static void CheckColumnHeaderTitle(VisualElement columnHeader, Rect bound, UxLayoutAuditResult result)
        {
            if (bound.width < 0.5f || bound.height < 0.5f) return;
            if (HasAnyReadableText(columnHeader)) return;
            Add(result, result.UntitledColumn, UxLayoutFindingKinds.UntitledColumn, columnHeader,
                Describe(columnHeader) + " cột bảng đang hiện nhưng đầu cột KHÔNG có chữ @" + RectText(bound, result.RootBound));
        }

        /// <summary>Có đoạn chữ khác rỗng nào trong cây con này không (kể cả chính element).</summary>
        private static bool HasAnyReadableText(VisualElement element)
        {
            if (element is TextElement self && !string.IsNullOrEmpty(self.text) && self.text.Trim().Length > 0) return true;
            int childCount = element.hierarchy.childCount;
            for (int index = 0; index < childCount; index++)
            {
                if (HasAnyReadableText(element.hierarchy[index])) return true;
            }
            return false;
        }

        /// <summary>
        /// Số đầu cột bảng ĐẾM ĐƯỢC trong một cây — <see cref="UxGateSelfCheckTests"/> đọc để khẳng định lưới W9-31 còn nhìn
        /// thấy bảng thật. Tên kiểu nội bộ của UI Toolkit đổi thì phép đếm về 0 và lưới IM LẶNG biến mất, đúng hạng lỗi
        /// "cổng hỏng thành cổng xanh" mà tự kiểm sinh ra để chặn.
        /// </summary>
        internal static int CountTableColumnHeaders(VisualElement root)
        {
            if (root == null) return 0;
            int count = IsTableColumnHeader(root) ? 1 : 0;
            int childCount = root.hierarchy.childCount;
            for (int index = 0; index < childCount; index++)
            {
                count += CountTableColumnHeaders(root.hierarchy[index]);
            }
            return count;
        }

        private static void CheckTextCut(TextElement text, Rect bound, UxLayoutAuditResult result)
        {
            Rect content = text.contentRect;
            string value = text.text;
            WhiteSpace whiteSpace = text.resolvedStyle.whiteSpace;
            bool wraps = whiteSpace != WhiteSpace.NoWrap;
            string reason = null;
            float naturalWidth = float.NaN;
            if (!wraps)
            {
                Vector2 natural = MeasureText(text, value, float.NaN);
                naturalWidth = natural.x;
                if (natural.x > content.width + TextMeasureTolerance)
                {
                    reason = "ngang cần " + Number(natural.x) + " có " + Number(content.width);
                }
            }
            else
            {
                Vector2 wrapped = MeasureText(text, value, content.width);
                if (wrapped.y > content.height + TextMeasureTolerance)
                {
                    reason = "dọc cần " + Number(wrapped.y) + " có " + Number(content.height);
                }
            }
            if (reason == null && IsElided(text)) reason = "isElided";
            bool clipped = IsClippedByAncestor(text, bound, result.RootBound, out string clipper);
            if (reason == null && !clipped)
            {
                CheckTextFillRatio(text, content, naturalWidth, bound, result);
                return;
            }
            Add(result, result.TextCut, UxLayoutFindingKinds.TextCut, text,
                Describe(text) + " " + (reason ?? string.Empty) + (clipped ? " | bị cha cắt: " + clipper : string.Empty)
                + " @" + RectText(bound, result.RootBound));
        }

        /// <summary>
        /// W9-25: chữ CHƯA cắt nhưng khoảng dư của ô đã mỏng hơn <see cref="TextFillLimitRatio"/>. Chỉ áp cho chữ KHÔNG
        /// xuống dòng (chữ xuống dòng thì bề rộng không còn là cái quyết định đọc được hay không) và cho ô đủ rộng để 5%
        /// còn là một con số có nghĩa.
        /// </summary>
        private static void CheckTextFillRatio(TextElement text, Rect content, float naturalWidth, Rect bound,
            UxLayoutAuditResult result)
        {
            if (float.IsNaN(naturalWidth) || naturalWidth <= 0f) return;
            if (content.width < TextFillMinimumWidth) return;
            if (text.text.IndexOf(EllipsisCharacter.ToString(), StringComparison.Ordinal) >= 0 || IsElided(text)) return;
            float fill = naturalWidth / content.width;
            if (fill <= TextFillLimitRatio) return;
            float slack = content.width - naturalWidth;
            if (slack <= TextFillMinimumSlack) return;
            Add(result, result.TextTight, UxLayoutFindingKinds.TextTight, text,
                Describe(text) + " chữ chiếm " + Number(fill * 100f) + "% bề rộng ô (cần " + Number(naturalWidth)
                + " có " + Number(content.width) + ", dư " + Number(slack) + ") @" + RectText(bound, result.RootBound));
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
            Add(result, result.ScrollViews, UxLayoutFindingKinds.ScrollViews, scrollView, entry.ToString());
        }

        /// <summary>
        /// Con không có gì để MẤT khi bị cắt: không chữ, không con nào có chữ, và không nhận con trỏ. Dùng để tách mặt nạ trang
        /// trí khỏi chữ/nút bị cắt thật (G-FIX-UX-8).
        /// </summary>
        private static bool IsPurelyDecorative(VisualElement element)
        {
            if (element == null) return false;
            if (element.pickingMode != PickingMode.Ignore) return false;
            if (element is TextElement) return false;
            List<TextElement> texts = element.Query<TextElement>().ToList();
            for (int index = 0; index < texts.Count; index++)
            {
                if (!string.IsNullOrEmpty(texts[index].text)) return false;
            }
            return true;
        }

        private static void CheckChildOverflow(VisualElement parent, Rect parentBound, VisualElement child, Rect childBound,
            UxLayoutAuditResult result)
        {
            if (parentBound.width < 1f || parentBound.height < 1f) return;
            // Nội dung cuộn RA NGOÀI viewport là đúng bản chất ScrollView — không phải tràn.
            if (parent is ScrollView) return;
            if (string.Equals(parent.name, "unity-content-container", StringComparison.Ordinal)) return;
            if (string.Equals(parent.name, "unity-content-viewport", StringComparison.Ordinal)) return;
            // Con position:absolute thò ra khỏi cha là CÁCH DỰNG bình thường của UI Toolkit (tay cầm mép thanh timeline đặt
            // left:-5px/right:-5px theo đúng [SD2] §3.2, chấm trạng thái đặt ngoài góc thẻ…). Chỉ khi cha CẮT (overflow:hidden)
            // thì phần thò ra mới thật sự biến mất khỏi mắt người dùng — đó mới là lỗi (R-02).
            if (child.resolvedStyle.position == Position.Absolute && !ClipsChildren(parent)) return;
            // Hình TRANG TRÍ cố tình to hơn cha đang CẮT là cách dựng mặt nạ (bo góc, vạt mép) của UI Toolkit: phần thò ra bị
            // cắt đúng như ý người vẽ, người dùng không mất chữ nào. Chỉ bỏ qua khi con không mang chữ, không có con nào mang
            // chữ, và không nhận con trỏ — tức không có gì để đọc hay để bấm mà mất đi (G-FIX-UX-8).
            if (ClipsChildren(parent) && IsPurelyDecorative(child)) return;
            float right = childBound.xMax - parentBound.xMax;
            float left = parentBound.xMin - childBound.xMin;
            float bottom = childBound.yMax - parentBound.yMax;
            float top = parentBound.yMin - childBound.yMin;
            if (right <= LayoutTolerance && left <= LayoutTolerance && bottom <= LayoutTolerance && top <= LayoutTolerance) return;
            Add(result, result.ChildOverflow, UxLayoutFindingKinds.ChildOverflow, child,
                Describe(child) + " tràn " + Describe(parent) + " [phải " + Number(right) + ", trái " + Number(left)
                + ", dưới " + Number(bottom) + ", trên " + Number(top) + "] cha cắt con=" + (ClipsChildren(parent) ? "có" : "không")
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
                    Add(result, result.SiblingOverlap, UxLayoutFindingKinds.SiblingOverlap, relativeChildren[first],
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
                // LiveOpsPlaceholder là khung xám "chỗ này sẽ có nội dung" của màn giữ chỗ: nó ĐƯỢC phủ lên vùng bên dưới, đó
                // là việc của nó. Đếm nó vào đây làm 88% số dòng absoluteOverText của lượt 6000.6 là rác (R-02).
                if (overlay is LiveOpsPlaceholder) continue;
                Rect overlayBound = overlay.worldBound;
                foreach (VisualElement other in textChildren)
                {
                    if (other == overlay) continue;
                    if (other.resolvedStyle.position == Position.Absolute) continue;
                    Rect otherBound = other.worldBound;
                    float overlapWidth = Mathf.Min(overlayBound.xMax, otherBound.xMax) - Mathf.Max(overlayBound.xMin, otherBound.xMin);
                    float overlapHeight = Mathf.Min(overlayBound.yMax, otherBound.yMax) - Mathf.Max(overlayBound.yMin, otherBound.yMin);
                    if (overlapWidth <= OverlapTolerance || overlapHeight <= OverlapTolerance) continue;
                    Add(result, result.AbsoluteOverText, UxLayoutFindingKinds.AbsoluteOverText, overlay,
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
                    Add(result, result.MissingElement, UxLayoutFindingKinds.MissingElement, root,
                        selector + ": không có trong cây — người dùng không có lối vào");
                    continue;
                }
                if (IsShownOnScreen(element)) continue;
                Add(result, result.MissingElement, UxLayoutFindingKinds.MissingElement, element,
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
                // Trừ phần cha mà thiết kế đã dành cho thứ khác (header làn 168px) TRƯỚC khi tính tỉ lệ — xem ReservedParentSize.
                float usableParentSize = parentSize - rule.ReservedParentSize;
                if (usableParentSize < 1f) continue;
                float ratio = childSize / usableParentSize;
                if (ratio >= rule.MinimumRatio) continue;
                string reservedNote = rule.ReservedParentSize > 0f
                    ? " (đã trừ " + Number(rule.ReservedParentSize) + "px thiết kế dành sẵn trong cha)"
                    : string.Empty;
                Add(result, result.NotStretched, UxLayoutFindingKinds.NotStretched, child,
                    rule.ChildSelector + " chỉ lấp " + Number(ratio * 100f) + "% " + (rule.Vertical ? "chiều cao" : "bề rộng")
                    + " dùng được của " + rule.ParentSelector + reservedNote + " (cần ≥ " + Number(rule.MinimumRatio * 100f)
                    + "%): " + Number(childSize) + " / " + Number(usableParentSize));
            }
        }

        private static void CheckContrastRules(VisualElement root, UxLayoutAuditResult result, IReadOnlyList<UxLayoutContrastRule> rules)
        {
            if (rules == null) return;
            foreach (UxLayoutContrastRule rule in rules)
            {
                List<VisualElement> targets = FindAllBySelector(root, rule.Selector);
                foreach (VisualElement target in targets)
                {
                    if (!IsShownOnScreen(target)) continue;
                    Color declared = target.resolvedStyle.backgroundColor;
                    if (declared.a < MinimumMeasurableAlpha) continue;
                    Color background = BackdropOf(target);
                    // Màu CÓ ALPHA không phải màu mắt người nhìn thấy. Dấu --overlap khai rgba(240,84,84,0.16): đo thẳng như màu
                    // đục ra 3,41:1 (ĐẠT) nhưng hợp thành thật trên nền #383838 ra #553C3C = 1,17:1 (TRƯỢT). Không hợp thành thì
                    // cổng bỏ sót đúng những dấu MỜ NHẤT — tức những dấu khó đọc nhất (RC-06/2.4 mục 1).
                    Color seen = CompositeOver(declared, background);
                    float ratio = ContrastRatio(seen, background);
                    if (ratio >= rule.MinimumRatio) continue;
                    Add(result, result.LowContrast, UxLayoutFindingKinds.LowContrast, target,
                        Describe(target) + " tương phản " + Number(ratio) + ":1 so với nền (cần ≥ "
                        + Number(rule.MinimumRatio) + ":1) — dấu " + ColorText(declared) + " hợp thành ra " + ColorText(seen)
                        + " trên nền " + ColorText(background));
                }
                CheckContrastVariantsMeasured(root, result, rule);
            }
        }

        /// <summary>
        /// Mỗi LOẠI dấu mà luật khai phải đo được ít nhất một element. Kiểm theo loại chứ không theo tổng vì một loại lặp lại
        /// nhiều lần che được chỗ của một loại đã biến mất — xem <see cref="UxLayoutContrastRule.RequiredVariantSelectors"/>.
        /// </summary>
        private static void CheckContrastVariantsMeasured(VisualElement root, UxLayoutAuditResult result, UxLayoutContrastRule rule)
        {
            if (rule.RequiredVariantSelectors == null) return;
            foreach (string variantSelector in rule.RequiredVariantSelectors)
            {
                int measuredCount = 0;
                foreach (VisualElement target in FindAllBySelector(root, variantSelector))
                {
                    if (!IsShownOnScreen(target)) continue;
                    if (target.resolvedStyle.backgroundColor.a < MinimumMeasurableAlpha) continue;
                    measuredCount++;
                }
                if (measuredCount > 0) continue;
                Add(result, result.LowContrast, UxLayoutFindingKinds.LowContrast, root,
                    variantSelector + ": không đo được dấu nào (luật " + rule.Selector
                    + ") — loại dấu này rơi khỏi phép đo màu, luật của màn im lặng bỏ qua nó");
            }
        }

        private static void CheckNoOverlapRules(VisualElement root, UxLayoutAuditResult result, IReadOnlyList<UxLayoutNoOverlapRule> rules)
        {
            if (rules == null) return;
            foreach (UxLayoutNoOverlapRule rule in rules)
            {
                VisualElement first = FindBySelector(root, rule.FirstSelector);
                VisualElement second = FindBySelector(root, rule.SecondSelector);
                if (first == null || second == null) continue;
                if (!IsShownOnScreen(first) || !IsShownOnScreen(second)) continue;
                Rect firstBound = first.worldBound;
                Rect secondBound = second.worldBound;
                float overlapWidth = Mathf.Min(firstBound.xMax, secondBound.xMax) - Mathf.Max(firstBound.xMin, secondBound.xMin);
                float overlapHeight = Mathf.Min(firstBound.yMax, secondBound.yMax) - Mathf.Max(firstBound.yMin, secondBound.yMin);
                if (overlapWidth <= OverlapTolerance || overlapHeight <= OverlapTolerance) continue;
                Add(result, result.PairOverlap, UxLayoutFindingKinds.PairOverlap, first,
                    rule.FirstSelector + " đè " + rule.SecondSelector + " " + Number(overlapWidth) + "x" + Number(overlapHeight)
                    + " @" + RectText(firstBound, result.RootBound));
            }
        }

        /// <summary>
        /// Nền ngay sau <paramref name="element"/>: cha gần nhất có nền ĐỤC; không có thì nền cửa sổ theo skin.
        /// <c>internal</c> để <c>UxContrastTokenTests</c> tự kiểm hằng nền của nó bằng CHÍNH phép đo mà luật tương phản dùng —
        /// hai chỗ đọc hai con số khác nhau thì bảng nền trôi trong im lặng.
        /// </summary>
        internal static Color BackdropOf(VisualElement element)
        {
            return OpaqueBackgroundFrom(element.hierarchy.parent);
        }

        /// <summary>
        /// Nền mà CHỮ của <paramref name="element"/> nằm trên: tính CẢ nền của chính element, rồi mới tới tổ tiên.
        /// <para>
        /// Khác <see cref="BackdropOf"/> đúng một bước đầu, và bước ấy là bước quyết định. <see cref="BackdropOf"/> sinh ra
        /// cho DẤU MÀU (ô màu chú giải): ở đó nền của chính element CHÍNH LÀ thứ đang được đo, nên phải bỏ qua. Với CHỮ thì
        /// ngược lại — một <c>Label</c> tự khai <c>background-color</c> vẽ nền ấy ngay dưới nét chữ của mình.
        /// </para>
        /// <para>
        /// Đo được ở lượt skin SÁNG đầu tiên (W9-27): cờ "bây giờ" của thước (<c>.liveops-hub-timeline-ruler-now-flag</c>) là
        /// một Label nền #1A1A1A chữ #C8C8C8 — đọc rất rõ, tương phản thật ≈ 11:1. Dùng <see cref="BackdropOf"/> thì bước đầu
        /// nhảy qua chính nó, rơi lên nền sáng của thước và ra 1,03:1, tức cổng sẽ đỏ vì một chỗ KHÔNG hỏng. Một cổng báo
        /// nhầm cũng vô dụng như một cổng im lặng.
        /// </para>
        /// </summary>
        internal static Color TextBackdropOf(VisualElement element)
        {
            return OpaqueBackgroundFrom(element);
        }

        private static Color OpaqueBackgroundFrom(VisualElement start)
        {
            for (VisualElement current = start; current != null; current = current.hierarchy.parent)
            {
                Color color = current.resolvedStyle.backgroundColor;
                if (color.a > 0.95f) return color;
            }
            return WindowBackground(EditorGUIUtility.isProSkin);
        }

        /// <summary>
        /// Nền cửa sổ Editor theo skin: #383838 (tối) / #C8C8C8 (sáng) — đúng hai con số mà chú thích của
        /// <c>liveops-hub-theme.uss</c> lấy làm nền quy chiếu khi chọn màu token.
        /// <para>
        /// Vì sao gom thành MỘT hàm dùng chung: bản cũ trả 0,8 (#CCCCCC) cho skin sáng trong khi bảng của
        /// <c>UxContrastTokenTests</c> khai #C8C8C8. Lệch 4/255 ấy LẬT phán quyết của <c>--liveops-hub-color-quiet</c> ở skin
        /// sáng (#555555 cho 4,46:1 trên #C8C8C8 — TRƯỢT, nhưng 4,64:1 trên #CCCCCC — ĐẠT) mà không ca nào bắt được, vì phép tự
        /// kiểm hằng nền chỉ chạy được ở skin ĐANG CHẠY (phát hiện A-01). Một nguồn sự thật thì hai chỗ không lệch được nữa.
        /// </para>
        /// </summary>
        internal static Color WindowBackground(bool proSkin)
        {
            float channel = proSkin ? DarkWindowBackgroundChannel : LightWindowBackgroundChannel;
            return new Color(channel, channel, channel, 1f);
        }

        /// <summary>
        /// Hợp thành màu có alpha lên nền đục theo phép source-over — trả về đúng màu mà mắt người nhìn thấy. WCAG chỉ định
        /// nghĩa tỉ số giữa hai màu ĐỤC, nên mọi màu có alpha phải đi qua đây trước khi tính tỉ số.
        /// </summary>
        internal static Color CompositeOver(Color source, Color backdrop)
        {
            float alpha = Mathf.Clamp01(source.a);
            return new Color(
                source.r * alpha + backdrop.r * (1f - alpha),
                source.g * alpha + backdrop.g * (1f - alpha),
                source.b * alpha + backdrop.b * (1f - alpha),
                1f);
        }

        /// <summary>Tỉ số tương phản WCAG 2.1 giữa hai màu đục (dùng cho dấu màu của chú giải, UX-19).</summary>
        internal static float ContrastRatio(Color first, Color second)
        {
            float firstLuminance = RelativeLuminance(first);
            float secondLuminance = RelativeLuminance(second);
            float lighter = Mathf.Max(firstLuminance, secondLuminance);
            float darker = Mathf.Min(firstLuminance, secondLuminance);
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        private static float RelativeLuminance(Color color)
        {
            return 0.2126f * LinearChannel(color.r) + 0.7152f * LinearChannel(color.g) + 0.0722f * LinearChannel(color.b);
        }

        private static float LinearChannel(float channel)
        {
            return channel <= 0.03928f ? channel / 12.92f : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }

        private static string ColorText(Color color)
        {
            return "rgba(" + Number(color.r * 255f) + "," + Number(color.g * 255f) + "," + Number(color.b * 255f) + ","
                + AlphaText(color.a) + ")";
        }

        /// <summary>
        /// Alpha in ĐỦ hai chữ số thập phân. Vì sao không dùng <see cref="Number"/>: nó làm tròn một chữ số, nên dấu khai alpha
        /// 0,16 in ra "0.2" và câu chẩn đoán hợp thành tự nói sai đầu vào của chính nó — người đọc suy ngược từ 0,2 ra một màu
        /// hợp thành khác và tưởng phép hợp thành hỏng (phát hiện A-09).
        /// </summary>
        private static string AlphaText(float alpha)
        {
            return float.IsNaN(alpha) || float.IsInfinity(alpha) ? "0" : alpha.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static void Add(UxLayoutAuditResult result, List<UxLayoutFinding> entries, string kind, VisualElement owner, string entry)
        {
            if (UxLayoutAllowList.Allows(result.ScreenId, kind, owner)) return;
            if (entries.Count >= MaximumEntriesPerKind)
            {
                // Trần cắt IM LẶNG là cách cổng nói dối: JSON in "problemCount" của phần đã cắt và báo cáo trước/sau so hai con
                // số đều sai. Đếm tiếp ở đây để JSON ghi được số THẬT (R-14).
                result.TruncatedCounts.TryGetValue(kind, out int already);
                result.TruncatedCounts[kind] = (already == 0 ? MaximumEntriesPerKind : already) + 1;
                return;
            }
            entries.Add(new UxLayoutFinding(owner, entry));
        }

        // ================================================================================================ đo chữ (reflection)

        /// <summary>
        /// <c>MeasureTextSize</c> và <c>isElided</c> đổi chỗ giữa 2022.3 và 6000.6 (lúc public, lúc internal) — gọi thẳng là hỏng
        /// một trong hai bản, nên tra bằng reflection một lần rồi nhớ. Không tra được thì trả 0 / false, NHƯNG
        /// <c>UxGateSelfCheckTests</c> làm cổng đỏ ngay ở lượt đó: mất một loại phát hiện mà cổng vẫn xanh là đúng cái bẫy
        /// "cổng hỏng thành cổng xanh" mà đợt này mở ra để diệt (R-07).
        /// </summary>
        private static Vector2 MeasureText(TextElement text, string value, float width)
        {
            MethodInfo measure = ResolveMeasureTextSize();
            if (measure == null) return Vector2.zero;
            try
            {
                Type modeType = measure.GetParameters()[2].ParameterType;
                object undefined = Enum.Parse(modeType, "Undefined");
                object exactly = Enum.Parse(modeType, "Exactly");
                bool hasWidth = !float.IsNaN(width);
                object measured = measure.Invoke(text, new object[]
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

        private static MethodInfo ResolveMeasureTextSize()
        {
            if (_measureTextSize != null) return _measureTextSize;
            foreach (MethodInfo candidate in typeof(TextElement).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(candidate.Name, "MeasureTextSize", StringComparison.Ordinal)) continue;
                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length != 5 || parameters[0].ParameterType != typeof(string)) continue;
                _measureTextSize = candidate;
                break;
            }
            return _measureTextSize;
        }

        private static PropertyInfo ResolveIsElided()
        {
            if (_isElided == null) _isElided = FindProperty(typeof(TextElement), "isElided");
            return _isElided;
        }

        private static bool IsElided(TextElement text)
        {
            PropertyInfo property = ResolveIsElided();
            if (property == null) return false;
            try
            {
                return (bool)property.GetValue(text, null);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Element này có CẮT con của nó không.
        /// <para>
        /// Lối cũ là đọc <c>computedStyle.overflow</c> bằng reflection, và nó chưa từng chạy được một lần nào: ở 2022.3 và
        /// 6000.6 <c>VisualElement.computedStyle</c> là property TRẢ VỀ REF nên <c>PropertyInfo.GetValue</c> luôn ném, còn ở
        /// 6000.6 kiểu <c>ComputedStyle</c> cũng không còn tên đó nữa. Hậu quả: hàm trả "?" ở mọi element ⇒ "bị cha cắt" luôn
        /// false ⇒ <see cref="IsClippedByAncestor"/> là code chết. <c>UxGateSelfCheckTests</c> là thứ làm nó lộ ra (R-07).
        /// </para>
        /// <para>
        /// <c>VisualElement.ShouldClip()</c> (internal, có ở CẢ hai bản) trả lời thẳng câu hỏi cần hỏi và không đi qua struct
        /// style nào — hỏi đúng thứ mình cần thay vì đọc nguyên liệu rồi tự suy ra.
        /// </para>
        /// </summary>
        internal static bool ClipsChildren(VisualElement element)
        {
            MethodInfo shouldClip = ResolveShouldClip();
            if (shouldClip == null || element == null) return false;
            try
            {
                return (bool)shouldClip.Invoke(element, null);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static MethodInfo ResolveShouldClip()
        {
            if (_shouldClip == null)
            {
                _shouldClip = typeof(VisualElement).GetMethod(ShouldClipMethodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            }
            return _shouldClip;
        }

        /// <summary>
        /// <paramref name="ancestor"/> có phải khung nhìn của một <see cref="ScrollView"/> ĐANG cuộn dọc được không.
        /// <para>
        /// Vì sao phải hỏi riêng: UI Toolkit đặt <c>overflow: hidden</c> lên <c>#unity-content-viewport</c> chứ KHÔNG lên chính
        /// <c>ScrollView</c>, nên kẻ cắt mà vòng tìm cha gặp trước bao giờ cũng là viewport. Miễn trừ chỉ khớp
        /// <c>ancestor is ScrollView</c> vì thế không bao giờ chạy cho cây thật, và MỌI hàng nằm dưới lằn cuộn bị báo "bị cha
        /// cắt" — luật đo nói SAI SỰ THẬT, vì người dùng chỉ cần cuộn xuống là đọc được (RC-06a: 89 dòng ở 6000.6, 103 ở 2022.3,
        /// tất cả ở màn Luật lặp).
        /// </para>
        /// <para>
        /// Đòi thanh cuộn dọc ĐANG HIỆN chứ không chỉ "là ScrollView": một ScrollView KHÔNG cuộn được mà có con nằm ngoài khung
        /// nhìn thì con đó biến mất thật và không có cách nào kéo tới — đó vẫn là lỗi, không được miễn trừ.
        /// </para>
        /// </summary>
        private static bool IsScrollableContentViewport(VisualElement ancestor, Rect bound)
        {
            if (ancestor == null) return false;
            // Đi LÊN tìm ScrollView chứ không hỏi thẳng cha: đo thật trên 6000.6 thì cha của #unity-content-viewport là
            // #unity-content-and-vertical-scroll-container (khung trung gian UI Toolkit chèn để đặt thanh cuộn dọc cạnh nội
            // dung), không phải chính ScrollView — bản đầu của hàm này hỏi `hierarchy.parent as ScrollView` nên trả false ở
            // MỌI cây thật và 89 dòng RC-06a không giảm một dòng nào.
            for (VisualElement current = ancestor.hierarchy.parent; current != null; current = current.hierarchy.parent)
            {
                ScrollView scrollView = current as ScrollView;
                if (scrollView == null) continue;
                // Chỉ đúng KHUNG NHÌN của ScrollView gần nhất mới được miễn trừ: một element cắt con bất kỳ nằm lọt trong
                // ScrollView vẫn cắt thật, không mượn được lý do "cuộn xuống là thấy".
                if (scrollView.contentViewport != ancestor) return false;
                if (!IsSelfShown(scrollView.verticalScroller)) return false;
                return IsInsideScrollableContent(scrollView, bound);
            }
            return false;
        }

        /// <summary>
        /// Element có nằm trong TẦM CUỘN không. Cuộn hết cỡ thì đáy hộp nội dung chạm đáy khung nhìn và đỉnh hộp chạm đỉnh, nên
        /// thứ nằm ngoài hộp nội dung theo chiều dọc KHÔNG có nấc cuộn nào kéo vào được — vẫn là chữ bị cắt thật.
        /// <para>
        /// Vì sao không dừng ở "thanh cuộn dọc đang hiện": câu hỏi ấy là "có cuộn được không", không phải "cuộn tới có thấy
        /// không". Hàng cuối của form Luật lặp nằm dưới đáy hộp nội dung đúng 9px ở cả tám ảnh chụp và ở cả hai bản Unity; luật
        /// cũ không phân biệt được nó với một hàng nằm gọn trong tầm cuộn (phát hiện A-05).
        /// </para>
        /// </summary>
        private static bool IsInsideScrollableContent(ScrollView scrollView, Rect bound)
        {
            Rect content = scrollView.contentContainer.worldBound;
            return bound.yMax <= content.yMax + LayoutTolerance && bound.yMin >= content.yMin - LayoutTolerance;
        }

        /// <param name="rootBound">
        /// Khung của root màn, để kẻ cắt in ra CÙNG hệ toạ độ với element trong câu chẩn đoán. Bản cũ in
        /// <c>RectText(clip, clip)</c> nên mọi kẻ cắt đều ra "(0,0 rộngxcao)" — mất vị trí, và không người soát nào đối chiếu
        /// được một miễn trừ từ JSON (phát hiện A-04).
        /// </param>
        private static bool IsClippedByAncestor(VisualElement element, Rect bound, Rect rootBound, out string clipper)
        {
            clipper = null;
            // Đã đi qua một khung nhìn cuộn được: từ đây lên trên, vị trí DỌC của element không còn nói lên điều gì (cuộn một
            // nấc là nó đổi chỗ), nhưng vị trí NGANG thì vẫn đo được — cuộn dọc không dời element theo chiều ngang. Bản cũ
            // "return false" ngay tại chỗ miễn trừ nên không cha nào phía trên khung nhìn còn được kiểm cắt (phát hiện A-08).
            bool scrolledOutOfViewport = false;
            for (VisualElement ancestor = element.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
            {
                bool clips = ancestor is ScrollView || ClipsChildren(ancestor);
                if (!clips) continue;
                Rect clip = ancestor.worldBound;
                bool insideHorizontally = bound.xMax <= clip.xMax + LayoutTolerance && bound.xMin >= clip.xMin - LayoutTolerance;
                bool insideVertically = bound.yMax <= clip.yMax + LayoutTolerance && bound.yMin >= clip.yMin - LayoutTolerance;
                if (scrolledOutOfViewport)
                {
                    if (insideHorizontally) continue;
                    clipper = Describe(ancestor) + " " + RectText(clip, rootBound);
                    return true;
                }
                if (insideHorizontally && insideVertically) return false;
                // Chữ cuộn ra khỏi lằn cuộn theo chiều DỌC là bình thường khi ScrollView cuộn tới được; chỉ ngang mới là cắt.
                if (insideHorizontally && (ancestor is ScrollView || IsScrollableContentViewport(ancestor, bound)))
                {
                    scrolledOutOfViewport = true;
                    continue;
                }
                clipper = Describe(ancestor) + " " + RectText(clip, rootBound);
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

        private static string SizeJson(UxWindowSize size)
        {
            return "{\"width\": " + size.Width.ToString(CultureInfo.InvariantCulture)
                + ", \"height\": " + size.Height.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string TruncatedJson(UxLayoutAuditResult result)
        {
            if (result.TruncatedCounts.Count == 0) return "{}";
            StringBuilder builder = new StringBuilder("{");
            bool first = true;
            foreach (KeyValuePair<string, int> pair in result.TruncatedCounts)
            {
                if (!first) builder.Append(", ");
                builder.Append(Quote(pair.Key)).Append(": ").Append(pair.Value.ToString(CultureInfo.InvariantCulture));
                first = false;
            }
            return builder.Append('}').ToString();
        }

        internal static string Number(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? "0" : value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static void AppendProperty(StringBuilder json, string name, string value)
        {
            json.Append("  ").Append(Quote(name)).Append(": ").Append(Quote(value)).Append(",\n");
        }

        private static void AppendArray(StringBuilder json, string name, List<UxLayoutFinding> values)
        {
            json.Append(",\n  ").Append(Quote(name)).Append(": [");
            for (int index = 0; index < values.Count; index++)
            {
                json.Append(index == 0 ? "\n    " : ",\n    ").Append(Quote(values[index].Text));
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
