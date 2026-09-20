using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

using DreamTech.LiveOps.Tests;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Nền mà một token màu được đo TRÊN. Vì sao phải khai thành kiểu chứ không mặc định "nền cửa sổ": chữ của hàng đang chọn
    /// nằm trên nền xanh chọn của Unity, đo nó trên nền cửa sổ là đo sai nền — mà bỏ không đo thì token ấy không được đo ở đâu
    /// cả, ở cả hai skin (phát hiện A-10). Có kiểu này thì "đo sai nền" và "không đo" không còn là hai lựa chọn duy nhất.
    /// </summary>
    internal enum UxColorTokenBackdrop
    {
        /// <summary>Nền cửa sổ Editor của skin — <c>UxLayoutAuditor.WindowBackground</c>.</summary>
        WindowBackground,

        /// <summary>Nền hàng đang chọn của Unity (<c>--unity-colors-highlight-background</c>).</summary>
        SelectionBackground,
    }

    /// <summary>
    /// Một token màu của <c>liveops-hub-theme.uss</c> cùng bậc tương phản WCAG mà nó phải đạt, và nền để đo.
    /// </summary>
    internal readonly struct UxColorTokenRule
    {
        public UxColorTokenRule(string propertyName, float minimumRatio, UxColorTokenBackdrop backdrop, string purpose)
        {
            PropertyName = propertyName;
            MinimumRatio = minimumRatio;
            Backdrop = backdrop;
            Purpose = purpose;
        }

        /// <summary>Tên custom property, đúng như khai trong <c>liveops-hub-theme.uss</c>.</summary>
        public string PropertyName { get; }

        /// <summary>Bậc phải đạt: 4,5:1 cho màu CHỮ, 3:1 cho hình khối (chốt của USER 19/9 theo WCAG 2.1).</summary>
        public float MinimumRatio { get; }

        /// <summary>Nền để đo — xem <see cref="UxColorTokenBackdrop"/>.</summary>
        public UxColorTokenBackdrop Backdrop { get; }

        /// <summary>Token này vẽ cái gì — để câu đỏ nói được vì sao con số ấy quan trọng, không chỉ đọc ra một tên biến.</summary>
        public string Purpose { get; }
    }

    /// <summary>
    /// Cổng tương phản WCAG cho TOÀN BỘ token màu của hub, ở CẢ HAI skin.
    /// <para>
    /// Vì sao cần một bộ test riêng bên cạnh luật <c>lowContrast</c> của <see cref="UxLayoutAuditor"/>: luật kia đo màu ĐÃ
    /// RESOLVE của skin ĐANG CHẠY, mà cổng chỉ chạy được một skin mỗi lượt (đổi skin Editor là việc riêng của
    /// <c>capture.sh</c>, SP-4). Khối <c>.liveops-hub--skin-light</c> vì thế CHƯA TỪNG được đo — ba token chữ dưới chuẩn nằm
    /// nguyên trong đó mà không lượt cổng nào bắt được. Ở đây thì khác: hai root dựng sẵn (một thường, một mang class skin
    /// sáng) cho cùng một stylesheet giải ra HAI bộ giá trị trong CÙNG một lượt, nên một lượt chạy phủ cả hai theme và chạy
    /// được ở bất kỳ skin Editor nào.
    /// </para>
    /// <para>
    /// Bậc theo chốt của USER 19/9: chữ thường ≥ 4,5:1; chữ to và hình khối (ô màu chú giải, đường kẻ, viền trạng thái) ≥ 3:1.
    /// Nền để đo lấy từ <see cref="UxLayoutAuditor.WindowBackground"/> — CÙNG hàm mà luật <c>lowContrast</c> dùng làm nền dự
    /// phòng, nên hai chỗ không thể nói hai con số khác nhau về cùng một cửa sổ (phát hiện A-01).
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    [Category(LiveOpsHubTestCategories.UxGate)]
    public sealed class UxContrastTokenTests
    {
        /// <summary>Bậc WCAG 2.1 AA cho chữ thường.</summary>
        private const float TextContrastRatio = UxComposedContrast.TextContrastRatio;

        /// <summary>Bậc WCAG 2.1 AA cho chữ to và cho thành phần đồ hoạ / thành phần giao diện.</summary>
        private const float ShapeContrastRatio = 3f;

        /// <summary>
        /// Nền hàng đang chọn của Unity, skin tối. Không phải lời khai suông: chính <c>liveops-hub-theme.uss</c> lấy màu này
        /// làm gốc cho <c>--liveops-hub-timeline-today</c> (rgba(44, 93, 135, 0.12)), và
        /// <see cref="BackdropConstants_MatchMeasuredBackdrops_InRunningSkin"/> đối chiếu hằng với
        /// <c>--unity-colors-highlight-background</c> đọc thật ở skin đang chạy.
        /// </summary>
        private const string DarkSelectionBackgroundHex = "#2C5D87";

        /// <summary>Nền hàng đang chọn của Unity, skin sáng — gốc của <c>--liveops-hub-timeline-today</c> khối skin sáng (rgba(58, 114, 176, 0.12)).</summary>
        private const string LightSelectionBackgroundHex = "#3A72B0";

        /// <summary>Sai số so hằng nền với số đo thật: nền vẽ qua 8 bit/kênh nên 1 nấc là làm tròn, không phải lệch bảng.</summary>
        private const float BackgroundToleranceChannels = 1.5f / 255f;

        /// <summary>Số khung chờ cho <c>CustomStyleResolvedEvent</c> chạy xong trên root mới gắn vào panel.</summary>
        private const int StyleResolveFrames = 4;

        private const string ThemeRelativePath = "Editor/Hub/UI/liveops-hub-theme.uss";
        private const string SkinLightBlockHeader = ".liveops-hub-root.liveops-hub--skin-light";
        private const string DarkSkinName = "skin tối";
        private const string LightSkinName = "skin sáng";
        private const string StyleSheetSearchPattern = "*.uss";

        /// <summary>
        /// Biến nền hàng đang chọn của Unity. Đo được nó bằng cách dựng một HÀNG CHỌN thật rồi đọc
        /// <c>resolvedStyle.backgroundColor</c> — xem <see cref="MeasureSelectionBackground"/>.
        /// </summary>
        private const string UnityHighlightBackgroundProperty = "--unity-colors-highlight-background";

        /// <summary>Thanh của đợt sinh từ luật trong lịch mẫu — chọn nó thì inspector dựng pane CHỈ ĐỌC (W9-21).</summary>
        private const string RecurringBarKey = "weekly-pass#35";

        /// <summary>
        /// (R-03 lượt soát 2) Thanh của một đợt CỐ ĐỊNH. Vì sao phải có cả hai: chọn đợt sinh từ luật thì inspector đi nhánh
        /// <c>BuildRecurring</c>, không dựng ô ngày giờ UTC nào — mà chính ô ấy mới mang <c>.liveops-hub-utc-field__zone</c>
        /// (opacity 0,7) và dòng giờ máy (opacity 0,82), tức đúng chủng loại lỗi mà luật màu-đã-hợp-thành sinh ra để bắt. Đo
        /// một nhánh rồi khai "pane inspector đạt chuẩn" là đo nửa màn.
        /// </summary>
        private const string FixedBarKey = LiveOpsDesignSample.HuntBonusEntryKey;

        /// <summary>Số khung chờ cho lượt style của element probe mới gắn vào cây hub.</summary>
        private const int ProbeResolveFrames = 2;

        /// <summary>
        /// Token màu CHỮ: phải đạt 4,5:1. <c>--liveops-hub-color-quiet</c> nằm ở đây chứ không ở nhóm hình khối vì chú thích của
        /// theme gọi nó là "vòng rỗng" nhưng thực tế nó là màu chữ phụ dùng khắp hub — kể cả tag lý do của popover Thêm đợt.
        /// Ca <see cref="TokenTier_MatchesUsage_EveryTokenPaintingTextIsInTextGroup"/> giữ cho việc xếp nhóm không còn là lời
        /// khai tay: token nào xuất hiện trong khai báo <c>color:</c> của USS thì BẮT BUỘC nằm ở nhóm này.
        /// </summary>
        private static readonly UxColorTokenRule[] TextTokenRules =
        {
            // (W10-06) Màu chữ nền tảng của hub: .liveops-hub-root của liveops-hub-shell.uss khai `color:` bằng token này, nên
            // MỌI chữ không tự khai màu đều kế thừa nó. Trước W10 chỗ này dùng thẳng --unity-colors-default-text và vì thế
            // không token nào của bảng nói được về màu chữ thường — bảng đo mọi màu phụ mà bỏ trống màu chính.
            new UxColorTokenRule("--liveops-hub-color-text", TextContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "màu chữ nền tảng, kế thừa xuống mọi chữ không tự khai màu"),
            new UxColorTokenRule("--liveops-hub-color-blocked-text", TextContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "câu lỗi và tag 'bị bỏ'"),
            new UxColorTokenRule("--liveops-hub-color-warning-text", TextContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "câu cảnh báo"),
            new UxColorTokenRule("--liveops-hub-color-quiet", TextContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "chữ phụ khắp hub, tag lý do của popover"),
            new UxColorTokenRule("--liveops-hub-color-focus", TextContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "viền tiêu điểm bàn phím, và màu chữ của chỗ đang nhận phím"),
            new UxColorTokenRule("--liveops-hub-color-link", TextContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "chữ liên kết"),
            new UxColorTokenRule("--liveops-hub-color-now", TextContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "nhãn mốc 'lúc này'"),
            new UxColorTokenRule("--liveops-hub-color-on-selection", TextContrastRatio, UxColorTokenBackdrop.SelectionBackground,
                "chữ của hàng đang chọn"),
            new UxColorTokenRule("--liveops-hub-json-key", TextContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "khoá JSON"),
            new UxColorTokenRule("--liveops-hub-json-string", TextContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "chuỗi JSON"),
            new UxColorTokenRule("--liveops-hub-json-number", TextContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "số JSON"),
            new UxColorTokenRule("--liveops-hub-json-literal", TextContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "true/false/null của JSON"),
        };

        /// <summary>Token HÌNH KHỐI: dải màu, chấm trạng thái, ô màu chú giải — bậc 3:1.</summary>
        private static readonly UxColorTokenRule[] ShapeTokenRules =
        {
            new UxColorTokenRule("--liveops-hub-color-blocked-fill", ShapeContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "dải màu trạng thái chặn, và viền ô chú giải 'chồng nhau'"),
            new UxColorTokenRule("--liveops-hub-color-warning-fill", ShapeContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "dải màu trạng thái cảnh báo"),
            new UxColorTokenRule("--liveops-hub-event-color-0", ShapeContrastRatio, UxColorTokenBackdrop.WindowBackground, "màu loại event 0"),
            new UxColorTokenRule("--liveops-hub-event-color-1", ShapeContrastRatio, UxColorTokenBackdrop.WindowBackground, "màu loại event 1"),
            new UxColorTokenRule("--liveops-hub-event-color-2", ShapeContrastRatio, UxColorTokenBackdrop.WindowBackground, "màu loại event 2"),
            new UxColorTokenRule("--liveops-hub-event-color-3", ShapeContrastRatio, UxColorTokenBackdrop.WindowBackground, "màu loại event 3"),
            new UxColorTokenRule("--liveops-hub-event-color-4", ShapeContrastRatio, UxColorTokenBackdrop.WindowBackground, "màu loại event 4"),
            new UxColorTokenRule("--liveops-hub-event-color-5", ShapeContrastRatio, UxColorTokenBackdrop.WindowBackground, "màu loại event 5"),
            new UxColorTokenRule("--liveops-hub-event-color-6", ShapeContrastRatio, UxColorTokenBackdrop.WindowBackground, "màu loại event 6"),
            new UxColorTokenRule("--liveops-hub-event-color-7", ShapeContrastRatio, UxColorTokenBackdrop.WindowBackground, "màu loại event 7"),
            // Ba dấu của dải chú giải trục. Chúng CHỈ xuất hiện trong khai báo `background-color` của liveops-hub-timeline.uss
            // (không chỗ nào dùng làm `color:`), nên đúng nhóm hình khối; và chốt của USER 19/9 gọi thẳng tên "ô màu chú giải"
            // là ví dụ của bậc 3:1. Để chúng ở đây chứ không ở bảng miễn trừ vì chúng là thứ DUY NHẤT phân biệt ba trạng thái
            // trong dải chú giải — không có chỗ nào khác mang thông tin thay, tức đúng nghĩa "thành phần đồ hoạ cần để hiểu
            // nội dung" của WCAG 2.1 §1.4.11.
            new UxColorTokenRule("--liveops-hub-legend-fixed", ShapeContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "dấu chú giải 'đợt cố định'"),
            new UxColorTokenRule("--liveops-hub-legend-recurring", ShapeContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "dấu chú giải 'đợt lặp'"),
            new UxColorTokenRule("--liveops-hub-legend-ended", ShapeContrastRatio, UxColorTokenBackdrop.WindowBackground,
                "dấu chú giải 'đã khép'"),
        };

        /// <summary>
        /// Token KHÔNG đo như màu vẽ ở phía trước, kèm lý do. Đây là danh sách miễn trừ DUY NHẤT của bộ test này: thêm một dòng
        /// vào đây là một quyết định phải giải thích được, không phải cách lấy màu xanh.
        /// <para>
        /// Bảy token ở đây đều khai màu CÓ ALPHA và đều là lớp nền phủ lên thứ khác, không phải thứ người dùng phải đọc. WCAG
        /// 2.1 §1.4.11 đòi 3:1 cho "thành phần đồ hoạ CẦN ĐỂ HIỂU nội dung" — thông tin của từng lớp nền này đều có một chỗ
        /// khác mang, và chỗ ấy CÓ được đo, nên lý do dưới đây phải nói rõ chỗ ấy là chỗ nào. Không có dòng nào trong bảy dòng
        /// này được phép trở thành cách né phép đo cho một màu CHỮ: ca <see cref="TokenTier_MatchesUsage_EveryTokenPaintingTextIsInTextGroup"/>
        /// bắt ngay nếu một token miễn trừ lại xuất hiện trong khai báo <c>color:</c> (phát hiện A-03).
        /// </para>
        /// </summary>
        private static readonly Dictionary<string, string> TokensNotMeasuredAsForeground = new Dictionary<string, string>
        {
            {
                "--liveops-hub-color-blocked-tint",
                "dải NỀN của dòng bị chặn, phủ lên nền cửa sổ — thông tin 'bị chặn' do chữ --liveops-hub-color-blocked-text và "
                + "viền --liveops-hub-color-blocked-fill mang, cả hai đều có trong bảng. Chỗ DUY NHẤT token này tự mang thông "
                + "tin là ô chú giải 'chồng nhau', và ở đó nó đã được luật lowContrast của UxLayoutAuditor đo tại chỗ, có hợp "
                + "thành alpha (màn timeline-legend-contrast)"
            },
            {
                "--liveops-hub-color-warning-tint",
                "dải NỀN của dòng cảnh báo — thông tin do chữ --liveops-hub-color-warning-text và dải "
                + "--liveops-hub-color-warning-fill mang, cả hai đều có trong bảng"
            },
            {
                "--liveops-hub-scrim-color",
                "lớp làm mờ nền sau popup: nhiệm vụ của nó là GIẢM tương phản của thứ phía sau để mắt dồn vào popup. Đòi nó "
                + "≥ 3:1 so với nền cửa sổ là đòi ngược đúng việc nó sinh ra để làm"
            },
            {
                "--liveops-hub-timeline-weekend",
                "vệt nền cuối tuần của làn — nhịp đọc, không mang dữ liệu: ngày nào là thứ Bảy/Chủ nhật đọc được ở nhãn thước "
                + "(chữ, đo theo màu chữ của Unity) chứ không phải đoán qua sắc nền"
            },
            {
                "--liveops-hub-timeline-today",
                "vệt nền cột hôm nay — mốc 'lúc này' do vạch và nhãn --liveops-hub-color-now mang, và token đó có trong bảng ở "
                + "bậc chữ 4,5:1"
            },
            {
                "--liveops-hub-timeline-grid",
                "đường kẻ lưới ngày: vạch nhịp sau nội dung, không phải đường phân giới phải nhìn ra mới hiểu được làn. Mốc "
                + "ngày đọc ở nhãn thước; đẩy vạch này lên 3:1 là biến nền làn thành lưới kẻ đậm hơn cả thanh sự kiện"
            },
            {
                "--liveops-hub-timeline-grid-monday",
                "đường kẻ mốc thứ Hai — cùng lý do với --liveops-hub-timeline-grid; nhãn thước là chỗ đọc mốc tuần"
            },
        };

        private TimelineTestPanel _panel;
        private UxHubWindowFixture _fixture;

        [TearDown]
        public void TearDown()
        {
            if (_panel != null) _panel.Dispose();
            _panel = null;
            if (_fixture != null) _fixture.Dispose();
            _fixture = null;
            UxHubWindowFixture.CloseStrayWindows();
        }

        /// <summary>
        /// Mọi token trong bảng phải đạt bậc của nó trên NỀN CỦA NÓ, ở CẢ HAI skin. Gom mọi chỗ trượt vào MỘT câu đỏ thay vì
        /// dừng ở chỗ đầu tiên: người sửa màu cần thấy trọn danh sách trong một lượt chạy, không phải bóc từng lớp hành.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryColorToken_MeetsWcagContrast_OnItsOwnBackdrop_InBothSkins()
        {
            _panel = TimelineTestPanel.Open();
            VisualElement darkRoot = _panel.CreateRoot(false);
            VisualElement lightRoot = _panel.CreateRoot(true);
            yield return TimelineTestPanel.WaitUntil(
                () => TimelineTestPanel.HasLayout(darkRoot) && TimelineTestPanel.HasLayout(lightRoot),
                "hai root token chưa có layout");
            for (int frame = 0; frame < StyleResolveFrames; frame++) yield return null;

            List<string> failures = new List<string>();
            CollectFailures(darkRoot, DarkSkinName, true, failures);
            CollectFailures(lightRoot, LightSkinName, false, failures);

            Assert.IsEmpty(failures,
                "token màu không đạt bậc tương phản WCAG (chữ ≥ " + Number(TextContrastRatio) + ":1, hình khối ≥ "
                + Number(ShapeContrastRatio) + ":1 — chốt của USER 19/9):" + Environment.NewLine
                + string.Join(Environment.NewLine, failures.ToArray()));
        }

        /// <summary>
        /// Bảng token ở trên phải phủ MỌI token khai màu trong <c>liveops-hub-theme.uss</c> — màu đục lẫn màu có alpha. Vì sao
        /// cần: một lượt sau thêm token màu mới (ví dụ thang màu riêng cho dấu chú giải) mà quên khai vào bảng thì token mới
        /// không được đo, và cổng tương phản im lặng tụt lại đúng chỗ vừa sửa.
        /// <para>
        /// Bản đầu chỉ dò token khai hex 6 số, nên BẢY token khai <c>rgba(…)</c> — đường kẻ lưới, nền dòng trạng thái, lớp làm
        /// mờ — không được đo ở đâu cả, mà "đường kẻ" và "viền trạng thái" đúng là hai thứ USER gọi tên trong chốt 3:1. Nay mọi
        /// token màu phải hoặc nằm trong bảng đo, hoặc nằm trong danh sách miễn trừ KÈM lý do (phát hiện A-03).
        /// </para>
        /// </summary>
        [Test]
        public void TokenTable_CoversEveryColorTokenOfTheme()
        {
            // Bỏ chú thích TRƯỚC khi dò mốc khối skin sáng: chú thích đầu file có nhắc nguyên văn tên selector ấy, nên
            // IndexOf trên văn bản thô dừng ở câu văn xuôi và khối "skin tối" thu lại còn mỗi lời tựa (đo thật: 0 token).
            string themeText = StripComments(File.ReadAllText(Path.Combine(PackageDirectory(), ThemeRelativePath)));
            int skinLightIndex = themeText.IndexOf(SkinLightBlockHeader, StringComparison.Ordinal);
            Assert.Greater(skinLightIndex, 0,
                "không tìm thấy khối " + SkinLightBlockHeader + " trong theme — bảng token không còn nói được về skin sáng");

            HashSet<string> declaredInDark = ColorTokensIn(themeText.Substring(0, skinLightIndex));
            HashSet<string> declaredInLight = ColorTokensIn(themeText.Substring(skinLightIndex));
            Assert.IsNotEmpty(declaredInDark,
                "không đọc được token màu nào ở khối skin tối — phép dò hỏng thì bảng phủ sóng thành lời khai suông");
            HashSet<string> measured = new HashSet<string>();
            foreach (UxColorTokenRule rule in AllTokenRules()) measured.Add(rule.PropertyName);

            List<string> mismatches = new List<string>();
            foreach (string token in declaredInDark)
            {
                if (measured.Contains(token)) continue;
                if (TokensNotMeasuredAsForeground.ContainsKey(token)) continue;
                mismatches.Add(token + " — khai màu trong theme mà không có trong bảng và cũng không có dòng miễn trừ, nên "
                    + "KHÔNG được đo tương phản ở đâu cả");
            }
            foreach (string token in declaredInDark)
            {
                if (declaredInLight.Contains(token)) continue;
                mismatches.Add(token + " — chỉ khai ở khối skin tối, khối skin sáng thiếu nên skin sáng dùng màu của skin tối");
            }
            foreach (string token in measured)
            {
                if (declaredInDark.Contains(token)) continue;
                mismatches.Add(token + " — bảng đo một token mà theme không còn khai, câu đỏ của nó sẽ nói về màu không tồn tại");
            }
            foreach (KeyValuePair<string, string> exemption in TokensNotMeasuredAsForeground)
            {
                if (measured.Contains(exemption.Key))
                {
                    mismatches.Add(exemption.Key + " — vừa có trong bảng đo vừa có dòng miễn trừ; hai lời khai ngược nhau thì "
                        + "không ai biết token này có được đo hay không");
                }
                if (declaredInDark.Contains(exemption.Key)) continue;
                mismatches.Add(exemption.Key + " — có dòng miễn trừ cho một token mà theme không còn khai; miễn trừ chết như "
                    + "thế che mất việc token thật sau này trùng tên lại được miễn theo");
            }

            Assert.IsEmpty(mismatches, "bảng token và theme không khớp:" + Environment.NewLine
                + string.Join(Environment.NewLine, mismatches.ToArray()));
        }

        /// <summary>
        /// Xếp nhóm phải theo CÁCH DÙNG THẬT, không theo lời khai tay: token nào xuất hiện trong khai báo <c>color:</c> của một
        /// file USS nào đó của package thì token ấy vẽ CHỮ, nên phải nằm ở nhóm 4,5:1.
        /// <para>
        /// Vì sao cần ca riêng: bảng ở trên khai bậc bằng tay, và ca phủ sóng chỉ kiểm token CÓ MẶT chứ không kiểm nó nằm đúng
        /// bậc. Dời một dòng từ nhóm chữ sang nhóm hình khối là hạ ngưỡng 4,5 xuống 3 mà không test nào đo — cửa hạ ngưỡng mở
        /// sẵn ngay trong file cổng (phát hiện A-06).
        /// </para>
        /// </summary>
        [Test]
        public void TokenTier_MatchesUsage_EveryTokenPaintingTextIsInTextGroup()
        {
            Dictionary<string, UxColorTokenRule> byName = new Dictionary<string, UxColorTokenRule>();
            foreach (UxColorTokenRule rule in AllTokenRules()) byName[rule.PropertyName] = rule;

            List<string> mismatches = new List<string>();
            int usageCount = 0;
            foreach (KeyValuePair<string, string> usage in TokensPaintingTextInStyleSheets())
            {
                usageCount++;
                string token = usage.Key;
                if (TokensNotMeasuredAsForeground.ContainsKey(token))
                {
                    mismatches.Add(token + " — vẽ CHỮ ở " + usage.Value + " nhưng đang nằm trong danh sách miễn trừ; miễn trừ "
                        + "chỉ dành cho lớp nền, không dành cho màu chữ");
                    continue;
                }
                if (!byName.TryGetValue(token, out UxColorTokenRule rule))
                {
                    mismatches.Add(token + " — vẽ CHỮ ở " + usage.Value + " mà không có trong bảng đo");
                    continue;
                }
                if (rule.MinimumRatio >= TextContrastRatio) continue;
                mismatches.Add(token + " — vẽ CHỮ ở " + usage.Value + " nhưng bảng xếp nó ở bậc " + Number(rule.MinimumRatio)
                    + ":1; chữ phải ở bậc " + Number(TextContrastRatio) + ":1");
            }

            Assert.Greater(usageCount, 0,
                "không đọc được khai báo color: var(--liveops-hub-…) nào trong USS của package — phép dò hỏng thì ca này thành "
                + "lời khai suông, không còn buộc bậc theo cách dùng nữa");
            Assert.IsEmpty(mismatches, "bậc tương phản không khớp cách dùng thật trong USS:" + Environment.NewLine
                + string.Join(Environment.NewLine, mismatches.ToArray()));
        }

        /// <summary>
        /// Tự kiểm hằng nền ở skin ĐANG CHẠY, cả nền cửa sổ lẫn nền hàng đang chọn. Hai chỗ đọc hai con số khác nhau thì mọi tỉ
        /// số ở trên nói về một cửa sổ không có thật, và không ai biết.
        /// <para>
        /// Ba nguồn phải khớp nhau: hằng của <see cref="UxLayoutAuditor.WindowBackground"/> (cũng là nền dự phòng của luật
        /// lowContrast), nền mà <see cref="UxLayoutAuditor.BackdropOf"/> đo thật sau lưng một dấu chú giải, và biến của chính
        /// Unity đọc qua probe skin của theme (<c>--liveops-hub-skin-probe</c> = <c>var(--unity-colors-window-background)</c>).
        /// Nguồn thứ ba là thứ bản trước thiếu: thiếu nó thì hằng #C8C8C8 của bảng và 0,8 (#CCCCCC) của auditor lệch nhau 4/255
        /// suốt mà không ca nào bắt (phát hiện A-01).
        /// </para>
        /// <para>
        /// Skin còn lại không tự kiểm được ở đây (đổi skin Editor là việc của <c>capture.sh</c>) — đó là giới hạn đã khai, và
        /// lượt cổng chạy ở skin sáng sẽ kiểm nốt nửa còn lại bằng chính ba phép so này.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator BackdropConstants_MatchMeasuredBackdrops_InRunningSkin()
        {
            _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, UxHubWindowFixture.AllSizes[4],
                LiveOpsHubLanguageId.Vietnamese);
            yield return _fixture.WaitForLayout();

            VisualElement sample = _fixture.Root.Q(className: LiveOpsHubClassNames.TimelineLegendSample);
            Assert.IsNotNull(sample,
                "không tìm thấy dấu chú giải nào — không có chỗ nào để đo nền thật, và hằng nền của bộ test thành lời khai suông");

            bool proSkin = UnityEditor.EditorGUIUtility.isProSkin;
            string skinName = proSkin ? DarkSkinName : LightSkinName;
            Color expectedWindow = UxLayoutAuditor.WindowBackground(proSkin);
            AssertSameColor(expectedWindow, UxLayoutAuditor.BackdropOf(sample), skinName,
                "nền đo thật sau lưng dấu chú giải (UxLayoutAuditor.BackdropOf)");

            // Đọc trên chính element mang class .liveops-hub-root: theme khai probe skin ở selector ấy, còn rootVisualElement
            // của cửa sổ là cha của nó và không khai token nào.
            VisualElement hubRoot = _fixture.Root.Q(className: LiveOpsHubClassNames.Root);
            Assert.IsNotNull(hubRoot, "không tìm thấy element mang class " + LiveOpsHubClassNames.Root
                + " — không có chỗ nào đọc được token của theme");

            Assert.IsTrue(hubRoot.customStyle.TryGetValue(new CustomStyleProperty<Color>(LiveOpsHubSkin.SkinProbePropertyName),
                    out Color probedWindow),
                "không đọc được " + LiveOpsHubSkin.SkinProbePropertyName + " trên root hub — probe skin của theme là đường DUY "
                + "NHẤT hỏi Unity nền cửa sổ thật, mất nó thì hằng nền quay về lời khai tay");
            AssertSameColor(expectedWindow, probedWindow, skinName,
                "biến nền cửa sổ của Unity đọc qua " + LiveOpsHubSkin.SkinProbePropertyName);

            // Nền hàng chọn KHÔNG đọc được bằng customStyle: biến --unity-colors-* của Unity không lộ ra ở đó (đo thật ở lượt
            // 6000.6 ngày 19/9: TryGetValue trả false, trong khi probe skin của theme trả đúng màu). Dựng một hàng chọn THẬT
            // rồi đọc màu nền đã resolve của nó là đường duy nhất hỏi được Unity con số ấy.
            VisualElement selectionProbe = new VisualElement();
            selectionProbe.AddToClassList(LiveOpsHubClassNames.PaletteRow);
            selectionProbe.AddToClassList(LiveOpsHubClassNames.PaletteRowSelected);
            hubRoot.Add(selectionProbe);
            for (int frame = 0; frame < ProbeResolveFrames; frame++) yield return null;
            Color probedSelection = selectionProbe.resolvedStyle.backgroundColor;
            selectionProbe.RemoveFromHierarchy();

            Assert.Greater(probedSelection.a, 1f - BackgroundToleranceChannels,
                "nền hàng chọn đo được không phải màu đục (" + HexText(probedSelection) + ", alpha " + Number(probedSelection.a)
                + ") — hàng chọn thật khai " + UnityHighlightBackgroundProperty
                + "; probe không ăn luật thì hằng nền chọn quay về lời khai tay");
            AssertSameColor(SelectionBackground(proSkin), probedSelection, skinName,
                "nền đo thật của một hàng đang chọn (" + UnityHighlightBackgroundProperty + ")");
        }

        /// <summary>
        /// (W9-21) Tương phản của màu ĐÃ HỢP THÀNH — gồm <c>opacity</c> của TỔ TIÊN — trên cây hub thật, ở CẢ HAI nhánh của
        /// inspector Lịch: đợt sinh từ luật (pane chỉ đọc) và đợt cố định (ô ngày giờ UTC).
        /// <para>
        /// Vì sao bảng token ở trên không đủ: nó đo TOKEN, tức màu trước khi Unity nhân opacity vào. Pane chỉ đọc của inspector
        /// Lịch khai màu chữ đạt chuẩn rồi bị <c>opacity</c> của <c>:disabled</c> kéo xuống 2,51–3,25:1 — không token nào sai,
        /// mà chữ vẫn không đọc nổi. Ca này đi ngược lại từ thứ người dùng NHÌN THẤY: màu chữ × mọi opacity của tổ tiên, hợp
        /// thành lên nền đục gần nhất.
        /// </para>
        /// <para>
        /// (R-03 lượt soát 2) Lặp qua CẢ HAI đợt. Bản đầu chỉ chọn đợt sinh từ luật, nên nhánh <c>BuildFixed</c> — nơi có
        /// <c>.liveops-hub-utc-field__zone</c> (opacity 0,7, chữ 10px) và dòng giờ máy (opacity 0,82) — chưa lần nào đi qua
        /// phép đo, dù đó đúng là chủng loại lỗi W9-21 dựng luật để bắt.
        /// </para>
        /// <para>
        /// Miễn trừ DUY NHẤT: phần tử không hoạt động (<c>enabledInHierarchy == false</c>) — WCAG 2.1 §1.4.3 nói thẳng chữ của
        /// "thành phần giao diện không hoạt động" không có yêu cầu tương phản. Chính vì miễn trừ đó mà ca này khẳng định RIÊNG
        /// một điều trước khi đo: pane chỉ đọc KHÔNG được là thành phần không hoạt động, vì nó là chỗ duy nhất đọc được giờ của
        /// lần lặp đang chọn. Thiếu khẳng định ấy thì chỉ cần khoá pane lại là đủ xanh — đúng cái lỗi W9-21 mở phiếu. Và mỗi
        /// lượt đòi ĐO ĐƯỢC ít nhất một đoạn chữ, kẻo một đổi tên class biến phép đo thành vòng lặp rỗng.
        /// </para>
        /// <para>
        /// PHẠM VI của riêng ca này (cập nhật 20/9/2026, soát W10 R-03): nó đo ở skin ĐANG CHẠY của lượt cổng — tức skin
        /// TỐI — và chỉ trên HAI nhánh inspector Lịch. Tám cảnh của cả hub, ở CẢ HAI skin, do hai ca đọc bằng chứng bên dưới
        /// phủ (<see cref="ComposedTextColor_MeetsWcagContrast_InLightSkin_FromCaptureEvidence"/> và
        /// <see cref="ComposedTextColor_MeetsWcagContrast_InDarkSkin_FromCaptureEvidence"/>). Giữ ca tại-chỗ này bên cạnh hai
        /// ca kia vì nó là chỗ DUY NHẤT đo trên cây element của chính lượt cổng, không qua file — mất nó thì luật hợp thành
        /// không còn một phép đo nào chạy cùng lúc với phần còn lại của cổng.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator ComposedTextColor_MeetsWcagContrast_InCalendarInspectorPanes()
        {
            string[] barKeys = { RecurringBarKey, FixedBarKey };
            for (int index = 0; index < barKeys.Length; index++)
            {
                string barKey = barKeys[index];
                bool isRecurring = string.Equals(barKey, RecurringBarKey, StringComparison.Ordinal);
                _fixture = UxHubWindowFixture.Open(LiveOpsHubSections.Ids.Calendar, UxHubWindowFixture.AllSizes[3],
                    LiveOpsHubLanguageId.Vietnamese);
                yield return _fixture.WaitForLayout();
                _fixture.Calendar.Presenter.SetSelectedBarKey(barKey);
                yield return null;
                yield return _fixture.WaitForLayout();

                VisualElement readOnlyPane = _fixture.Root.Q(className: LiveOpsHubClassNames.CalendarInspectorReadOnlyPane);
                if (isRecurring)
                {
                    Assert.IsNotNull(readOnlyPane,
                        "không tìm thấy khối field chỉ đọc của đợt sinh từ luật — không có chỗ nào để đo, và ca này thành lời khai suông");
                    Assert.IsTrue(readOnlyPane.enabledInHierarchy,
                        "khối field chỉ đọc đang là thành phần KHÔNG HOẠT ĐỘNG. WCAG 2.1 §1.4.3 miễn tương phản cho thứ không hoạt "
                        + "động, nên khoá khối này lại là cách làm cho phép đo dưới đây im lặng — trong khi đây là chỗ DUY NHẤT đọc "
                        + "được id, giờ lần lặp và độ dài của đợt đang chọn. Không sửa được phải giữ bằng isReadOnly + viền + ghi chú "
                        + "(W9-21)");
                }
                else
                {
                    Assert.IsNotNull(_fixture.Root.Q<LiveOpsUtcDateTimeField>(),
                        "đợt cố định phải dựng ô ngày giờ UTC — không có ô nào thì vòng lặp này không đo thêm được gì (R-03)");
                }

                List<string> failures = new List<string>();
                int measuredCount = 0;
                if (readOnlyPane != null)
                {
                    measuredCount += CollectComposedTextFailures(readOnlyPane, "khối field chỉ đọc của inspector Lịch", failures);
                }

                measuredCount += CollectComposedTextFailures(_fixture.Root.Q(className: LiveOpsHubClassNames.CalendarInspectorBody),
                    "thân inspector màn Lịch, đợt \"" + barKey + "\"", failures);

                Assert.Greater(measuredCount, 0,
                    "không đo được đoạn chữ nào ở đợt \"" + barKey + "\" — phép lọc hỏng thì ca này thành lời khai suông");
                Assert.IsEmpty(failures,
                    "màu chữ ĐÃ HỢP THÀNH (nhân opacity của tổ tiên) không đạt " + Number(TextContrastRatio) + ":1 ở skin "
                    + (UnityEditor.EditorGUIUtility.isProSkin ? DarkSkinName : LightSkinName) + ":" + Environment.NewLine
                    + string.Join(Environment.NewLine, failures.ToArray()));

                _fixture.Dispose();
                _fixture = null;
            }
        }

        /// <summary>
        /// (W9-27) Tương phản của màu ĐÃ HỢP THÀNH ở skin SÁNG, đọc từ BẰNG CHỨNG mà <c>capture.sh --contrast</c> ghi ra.
        /// <para>
        /// Vì sao không đo tại chỗ: cửa sổ hub thật vẽ theo skin ĐANG CHẠY của Editor và mọi lượt cổng chạy skin TỐI — đổi
        /// <c>EditorPrefs UserSkin</c> là việc riêng của <c>capture.sh</c> (SP-4). Không được suy sang skin sáng từ bảng
        /// token: bảng token đo TRƯỚC khi nhân opacity, đúng thứ ca màu-đã-hợp-thành chứng minh là không đủ, và hai skin
        /// khai token khác hẳn nhau (<c>--liveops-hub-color-quiet</c> #A3A3A3 tối / #4F4F4F sáng).
        /// </para>
        /// <para>
        /// Ca này ĐỎ khi: thiếu file bằng chứng · khuôn JSON khác đời · bằng chứng của bản Unity khác · bằng chứng của skin
        /// khác · dấu nguồn lệch (ai đó sửa <c>Editor/Hub/**</c> hoặc sửa chính cái thước sau lượt đo) · đo được 0 đoạn chữ ·
        /// DANH SÁCH CẢNH lệch · có bất kỳ dòng trượt nào — xem <see cref="AssertComposedContrastEvidence"/>.
        /// </para>
        /// <para>
        /// Cách lấy bằng chứng: <c>tools/liveops-hub/capture.sh --contrast --unity 6000|2022 --skins light --label &lt;nhãn&gt;</c>.
        /// </para>
        /// </summary>
        // KHÔNG gắn [Category(Logic)] (soát W10 R-07): fixture này mang [Category(UI)] + [Category(UxGate)], mà lượt Logic
        // lọc bằng `!LiveOpsHub.UI` — nhãn Logic trên một ca của fixture UI là nhãn CHẾT, ca không bao giờ chạy ở lượt Logic
        // và vẫn chạy ở lượt UI/UxGate. Ca này không mở cửa sổ nào, nhưng chỗ đứng của nó là cạnh phép đo tại chỗ ở trên.
        [Test]
        public void ComposedTextColor_MeetsWcagContrast_InLightSkin_FromCaptureEvidence()
        {
            AssertComposedContrastEvidence(UxContrastEvidence.LightSkin, LightSkinName, false);
        }

        /// <summary>
        /// (W9-27, soát W10 R-03) Cùng phép đo ấy ở skin TỐI, cũng đọc từ bằng chứng của <c>capture.sh --contrast</c>.
        /// <para>
        /// Vì sao cần dù lượt cổng vốn chạy skin tối: ca đo TẠI CHỖ ở trên chỉ đi qua hai nhánh inspector Lịch của MỘT cỡ và
        /// MỘT ngôn ngữ. Tám cảnh của cả hub ở skin tối — Tổng quan, Loại event, Luật lặp, Kiểm lịch, Xuất JSON — chưa lượt
        /// nào đo. Bằng chứng của hai skin sinh cùng một cách, đọc bằng cùng một câu, nên không skin nào là "nửa được tin".
        /// </para>
        /// <para>
        /// Cách lấy bằng chứng: <c>tools/liveops-hub/capture.sh --contrast --unity 6000|2022 --skins dark --label &lt;nhãn&gt;</c>.
        /// </para>
        /// </summary>
        [Test]
        public void ComposedTextColor_MeetsWcagContrast_InDarkSkin_FromCaptureEvidence()
        {
            AssertComposedContrastEvidence(UxContrastEvidence.DarkSkin, DarkSkinName, true);
        }

        /// <summary>
        /// Đọc một file bằng chứng đo tương phản và ĐỎ khi: thiếu file · khuôn JSON khác đời · bằng chứng của bản Unity khác ·
        /// bằng chứng của skin khác · cờ proSkin lúc đo không khớp skin khai · dấu nguồn lệch (ai đó sửa <c>Editor/Hub/**</c>
        /// hoặc sửa chính cái thước sau lượt đo) · đo được 0 đoạn chữ · DANH SÁCH CẢNH lệch · có bất kỳ dòng trượt nào.
        /// Thiếu câu nào trong số đó thì cổng tự lừa mình bằng một file cũ.
        /// </summary>
        private static void AssertComposedContrastEvidence(string skin, string skinName, bool expectedProSkin)
        {
            string path = UxContrastEvidence.PathFor(skin);
            UxContrastEvidenceData evidence = UxContrastEvidence.Read(path);
            Assert.IsNotNull(evidence,
                "không đọc được bằng chứng đo tương phản skin " + skinName + " ở " + path + " — chạy "
                + "tools/liveops-hub/capture.sh --contrast --unity <bản> --skins " + skin + " --label <nhãn> rồi chạy lại. "
                + "Mỗi skin là một nửa giao diện, và một nửa chưa đo là một nửa chưa ai nhìn (W9-27)");
            Assert.AreEqual(UxContrastEvidence.SchemaVersion, evidence.schema,
                "bằng chứng ở " + path + " theo khuôn đời " + evidence.schema + ", cổng đọc khuôn đời "
                + UxContrastEvidence.SchemaVersion + " — đo lại, đừng đọc file cũ bằng luật mới");
            Assert.AreEqual(skin, evidence.skin,
                "bằng chứng ở " + path + " là của skin '" + evidence.skin + "', không phải skin " + skinName);
            Assert.AreEqual(Application.unityVersion, evidence.unityVersion,
                "bằng chứng ở " + path + " đo trên Unity " + evidence.unityVersion + ", lượt này chạy "
                + Application.unityVersion + " — hai bản dựng cây element khác nhau nên số đo không dùng chung được");
            Assert.AreEqual(expectedProSkin, evidence.proSkin,
                "bằng chứng khai skin " + skinName + " nhưng Editor lúc đo báo proSkin = " + evidence.proSkin
                + " — lượt đo chạy sai skin");

            string digest = UxContrastEvidence.ComputeSourceDigest();
            Assert.IsNotEmpty(digest,
                "không băm được cây nguồn Editor/Hub + mã đo — không có dấu thì bằng chứng cũ tới mấy cũng qua được cổng");
            Assert.AreEqual(digest, evidence.sourceDigest,
                "bằng chứng ở " + path + " đo trên một cây nguồn KHÁC cây đang chạy (dấu " + evidence.sourceDigest
                + " ≠ " + digest + ") — Editor/Hub hoặc chính mã đo đã đổi sau lượt đo, đo lại bằng capture.sh --contrast");
            CollectionAssert.AreEqual(EvidenceSceneNames, evidence.scenes,
                "bằng chứng ở " + path + " đi qua một DANH SÁCH CẢNH khác cổng đang đòi. Cổng đối chiếu tên từng cảnh chứ "
                + "không đối chiếu phép đếm: bỏ một màn rồi thêm một màn khác vẫn giữ nguyên con số, và đúng màn bị bỏ là "
                + "màn không ai đo nữa. Cổng đòi: " + string.Join(" · ", EvidenceSceneNames) + ". Bằng chứng khai: "
                + (evidence.scenes == null ? "(không khai)" : string.Join(" · ", evidence.scenes)));
            Assert.AreEqual(EvidenceSceneNames.Length, evidence.sceneCount,
                "bằng chứng ở " + path + " khai sceneCount = " + evidence.sceneCount + " nhưng liệt kê "
                + (evidence.scenes == null ? 0 : evidence.scenes.Length) + " cảnh — file tự mâu thuẫn");
            Assert.Greater(evidence.measuredCount, 0,
                "bằng chứng đo được 0 đoạn chữ — phép lọc hỏng thì ca này thành lời khai suông");
            Assert.IsEmpty(evidence.failures,
                "màu chữ ĐÃ HỢP THÀNH (nhân opacity của tổ tiên) không đạt " + Number(evidence.minimumRatio) + ":1 ở "
                + skinName + " (" + evidence.measuredCount + " đoạn chữ đo trên " + evidence.sceneCount + " cảnh):"
                + Environment.NewLine + string.Join(Environment.NewLine, evidence.failures));
        }

        /// <summary>
        /// Tên TỪNG cảnh mà lệnh đo phải đi qua, đúng thứ tự — sáu màn của hub cộng hai nhánh của inspector Lịch.
        /// <para>
        /// Khai bằng TÊN chứ không bằng con số 8 (soát W10 R-02): một phép đếm khớp vẫn có thể là tám cảnh khác, nên bỏ đúng
        /// màn khó rồi thêm một màn dễ vẫn qua cổng. Danh sách này phải khớp từng chữ với <c>LiveOpsHubContrastCommand</c>;
        /// thêm cảnh ở đó thì thêm dòng ở đây, và lúc ấy bằng chứng cũ lệch — đúng như mong muốn.
        /// </para>
        /// </summary>
        private static readonly string[] EvidenceSceneNames =
        {
            "màn Tổng quan",
            "màn Loại event",
            "màn Lịch, chưa chọn đợt",
            "màn Lịch, đợt sinh từ luật",
            "màn Lịch, đợt cố định",
            "màn Luật lặp",
            "màn Kiểm lịch",
            "màn Xuất JSON",
        };

        // ------------------------------------------------------------------------------------------------- trợ giúp

        /// <summary>
        /// Phép đo màu ĐÃ HỢP THÀNH dùng chung với lệnh đo batchmode — xem <see cref="UxComposedContrast"/>. Giữ một bản
        /// DUY NHẤT của công thức vì từ W9-27 nó chạy ở hai chỗ (lượt EditMode ở skin đang chạy, lượt chụp ở skin sáng), và
        /// hai bản sao là hai con số khác nhau về cùng một cửa sổ.
        /// </summary>
        private static int CollectComposedTextFailures(VisualElement root, string place, List<string> failures)
        {
            return UxComposedContrast.CollectFailures(root, place, failures);
        }

        private static void CollectFailures(VisualElement root, string skinName, bool proSkin, List<string> failures)
        {
            foreach (UxColorTokenRule rule in AllTokenRules())
            {
                CustomStyleProperty<Color> property = new CustomStyleProperty<Color>(rule.PropertyName);
                if (!root.customStyle.TryGetValue(property, out Color declared))
                {
                    failures.Add(skinName + " · " + rule.PropertyName + " (" + rule.Purpose
                        + "): TryGetValue = false — theme không khai token này cho skin ấy, element đọc nó sẽ về màu dự phòng của C#");
                    continue;
                }

                Color background = BackdropFor(rule.Backdrop, proSkin);
                Color seen = UxLayoutAuditor.CompositeOver(declared, background);
                float ratio = UxLayoutAuditor.ContrastRatio(seen, background);
                if (ratio >= rule.MinimumRatio) continue;
                failures.Add(skinName + " · " + rule.PropertyName + " (" + rule.Purpose + "): " + Number(ratio)
                    + ":1 — cần ≥ " + Number(rule.MinimumRatio) + ":1; màu " + HexText(declared) + " trên nền "
                    + HexText(background) + " (" + BackdropName(rule.Backdrop) + ")");
            }
        }

        private static Color BackdropFor(UxColorTokenBackdrop backdrop, bool proSkin)
        {
            return backdrop == UxColorTokenBackdrop.SelectionBackground
                ? SelectionBackground(proSkin)
                : UxLayoutAuditor.WindowBackground(proSkin);
        }

        private static string BackdropName(UxColorTokenBackdrop backdrop)
        {
            return backdrop == UxColorTokenBackdrop.SelectionBackground ? "nền hàng đang chọn" : "nền cửa sổ";
        }

        private static Color SelectionBackground(bool proSkin)
        {
            return Hex(proSkin ? DarkSelectionBackgroundHex : LightSelectionBackgroundHex);
        }

        private static void AssertSameColor(Color expected, Color measured, string skinName, string sourceName)
        {
            string message = "hằng nền " + skinName + " của bộ test là " + HexText(expected) + " nhưng " + sourceName
                + " là " + HexText(measured)
                + " — mọi tỉ số ở bộ test này đang nói về một cửa sổ khác cửa sổ người dùng thấy";
            Assert.AreEqual(expected.r, measured.r, BackgroundToleranceChannels, message + " (kênh đỏ)");
            Assert.AreEqual(expected.g, measured.g, BackgroundToleranceChannels, message + " (kênh lục)");
            Assert.AreEqual(expected.b, measured.b, BackgroundToleranceChannels, message + " (kênh lam)");
        }

        private static IEnumerable<UxColorTokenRule> AllTokenRules()
        {
            for (int index = 0; index < TextTokenRules.Length; index++) yield return TextTokenRules[index];
            for (int index = 0; index < ShapeTokenRules.Length; index++) yield return ShapeTokenRules[index];
        }

        /// <summary>
        /// Mọi token xuất hiện trong khai báo <c>color:</c> của USS trong package, kèm chỗ dùng đầu tiên (file:dòng) để câu đỏ
        /// chỉ được tay vào chỗ phải sửa. Lookbehind loại đúng những họ hàng của <c>color</c> — <c>background-color</c>,
        /// <c>border-color</c>, <c>-unity-background-image-tint-color</c> — vốn KHÔNG vẽ chữ.
        /// </summary>
        private static IEnumerable<KeyValuePair<string, string>> TokensPaintingTextInStyleSheets()
        {
            Regex pattern = new Regex(@"(?<![-a-zA-Z])color\s*:\s*var\((--liveops-hub-[a-z0-9-]+)\)");
            HashSet<string> seen = new HashSet<string>();
            string[] files = Directory.GetFiles(PackageDirectory(), StyleSheetSearchPattern, SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);
            foreach (string file in files)
            {
                string[] lines = File.ReadAllLines(file);
                for (int index = 0; index < lines.Length; index++)
                {
                    Match match = pattern.Match(lines[index]);
                    if (!match.Success) continue;
                    string token = match.Groups[1].Value;
                    if (!seen.Add(token)) continue;
                    yield return new KeyValuePair<string, string>(token,
                        Path.GetFileName(file) + ":" + (index + 1).ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        /// <summary>
        /// Tên mọi token khai MÀU trong một khối USS: hex 6 số hoặc <c>rgba(…)</c>. Token khai <c>var(…)</c> (probe skin) và
        /// token khai px (nhịp giãn cách) không phải màu nên không tính.
        /// </summary>
        private static HashSet<string> ColorTokensIn(string block)
        {
            HashSet<string> tokens = new HashSet<string>();
            foreach (Match match in Regex.Matches(StripComments(block),
                @"(--liveops-hub-[a-z0-9-]+)\s*:\s*(#[0-9A-Fa-f]{6}|rgba?\([^)]*\))\s*;"))
            {
                tokens.Add(match.Groups[1].Value);
            }
            return tokens;
        }

        /// <summary>Bỏ chú thích USS trước khi dò: chú thích của theme có chép nguyên mã màu làm ví dụ.</summary>
        private static string StripComments(string text)
        {
            return Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        }

        private static string PackageDirectory()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UxContrastTokenTests).Assembly);
            Assert.IsNotNull(package,
                "không hỏi được gốc package từ assembly test — bộ test này phải chạy như một phần của com.dreamtech.liveops");
            return Path.GetFullPath(package.resolvedPath);
        }

        private static Color Hex(string value)
        {
            string digits = value.TrimStart('#');
            return new Color(
                int.Parse(digits.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f,
                int.Parse(digits.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f,
                int.Parse(digits.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f,
                1f);
        }

        private static string HexText(Color color)
        {
            StringBuilder builder = new StringBuilder("#");
            builder.Append(Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f).ToString("X2", CultureInfo.InvariantCulture));
            builder.Append(Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f).ToString("X2", CultureInfo.InvariantCulture));
            builder.Append(Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f).ToString("X2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static string Number(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
