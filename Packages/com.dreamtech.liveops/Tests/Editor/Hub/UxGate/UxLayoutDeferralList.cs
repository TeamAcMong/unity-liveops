using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Một mục HOÃN của kiểm bố cục: màn nào, phiếu W9 nào nhận, lúc hoãn đang có bao nhiêu chỗ không dùng được, và VÌ SAO
    /// đợt W8 không sửa. Ba trường đầu để người soát tìm lại đúng phiếu; lý do là bắt buộc vì hoãn là cách duy nhất cổng
    /// được phép đứng yên trước một màn đỏ — không có lý do thì lần sau không ai phân biệt được "đã hẹn sửa" với "đã quên".
    /// <para>
    /// Khác <see cref="UxLayoutAllowEntry"/> ở bản chất: miễn trừ nói "chỗ cắt này là THIẾT KẾ, đừng báo nữa"; hoãn nói
    /// "chỗ này là LỖI THẬT, chưa tới lượt sửa". Vì vậy hoãn không đụng gì tới luật kiểm — mọi câu assert của test giữ
    /// nguyên, gỡ một dòng khỏi danh sách này là test chạy lại đầy đủ ngay lượt sau.
    /// </para>
    /// </summary>
    internal sealed class UxLayoutDeferralEntry
    {
        public UxLayoutDeferralEntry(string deferralId, string screenId, int findingCount, string reason,
            params string[] deferredSizes)
        {
            if (string.IsNullOrEmpty(deferralId)) throw new ArgumentNullException(nameof(deferralId));
            if (string.IsNullOrEmpty(screenId)) throw new ArgumentNullException(nameof(screenId));
            if (string.IsNullOrEmpty(reason)) throw new ArgumentNullException(nameof(reason));
            DeferralId = deferralId;
            ScreenId = screenId;
            FindingCount = findingCount;
            Reason = reason;
            DeferredSizes = deferredSizes ?? new string[0];
        }

        /// <summary>Mã phiếu của đợt W9 (dạng <c>W9-NN</c>) — chỗ duy nhất nối test đỏ với việc đã hẹn làm.</summary>
        public string DeferralId { get; }

        /// <summary>Màn của kiểm bố cục (<see cref="UxLayoutScreen.Id"/>) mà test đang dựng.</summary>
        public string ScreenId { get; }

        /// <summary>Số chỗ không dùng được đo được lúc hoãn — để lượt W9 biết ngay mình đang nhận việc to hay nhỏ.</summary>
        public int FindingCount { get; }

        public string Reason { get; }

        /// <summary>
        /// Nhãn các cỡ cửa sổ ("700x560") còn hoãn. RỖNG = hoãn CẢ màn ở mọi cỡ (W9-18).
        /// <para>
        /// Vì sao hoãn theo cỡ chứ không theo màn: một mục hoãn theo màn dừng luôn những cặp cỡ × ngôn ngữ đang SẠCH của
        /// màn đó. Màn Loại event là ví dụ nặng nhất — lỗi chỉ ở ba cỡ hẹp, nhưng hoãn cả test làm 6/12 cặp đang xanh
        /// ngừng được kiểm, tức là một lần sửa làm hỏng ba cỡ rộng cũng sẽ KHÔNG ai thấy cho tới hết đợt W9.
        /// </para>
        /// </summary>
        public IReadOnlyList<string> DeferredSizes { get; }

        /// <summary>Cỡ <paramref name="sizeLabel"/> ("950x700") có đang được hoãn không; mục hoãn cả màn thì mọi cỡ đều có.</summary>
        public bool IsDeferredAt(string sizeLabel)
        {
            if (DeferredSizes.Count == 0) return true;
            for (int index = 0; index < DeferredSizes.Count; index++)
            {
                if (string.Equals(DeferredSizes[index], sizeLabel, StringComparison.Ordinal)) return true;
            }

            return false;
        }

        /// <summary>Câu người chạy test đọc được: mã phiếu đứng đầu để lọc, rồi số chỗ, rồi lý do.</summary>
        public string IgnoreMessage
        {
            get
            {
                // InvariantCulture vì đây là chuỗi MÁY đọc (người soát grep theo mã phiếu + con số), không phải chuỗi hiển
                // thị: máy chạy cổng đổi ngôn ngữ hệ thống thì con số phải vẫn y nguyên. Cùng quy ước với UxLayoutAuditor.
                string scope = DeferredSizes.Count == 0
                    ? " ở MỌI cỡ"
                    : " ở " + DeferredSizes.Count.ToString(CultureInfo.InvariantCulture) + " cỡ ("
                      + string.Join(", ", DeferredSizes as string[] ?? new List<string>(DeferredSizes).ToArray()) + ")";
                return DeferralId + " — màn '" + ScreenId + "' còn " + FindingCount.ToString(CultureInfo.InvariantCulture)
                    + " chỗ không dùng được" + scope + ". " + Reason;
            }
        }
    }

    /// <summary>
    /// Danh sách HOÃN sang đợt W9 của cổng W8-UX — ĐÚNG 5 màn NGOÀI phạm vi đợt mà USER đã chốt ngày 18/9/2026 (Xuất JSON,
    /// Tổng quan, Loại event, Kiểm lịch, khung). Không màn nào khác được vào đây: màn TRONG đợt (Lịch, trục, Luật lặp) mà
    /// đỏ thì phải sửa hoặc phải có câu trả lời của USER — hoãn nó là tự cấp phép cho chính mình, đúng loại "xanh giả" mà
    /// cổng sinh ra để diệt.
    /// <para>
    /// Luật của danh sách: KHÔNG xoá test, KHÔNG nới <see cref="UxLayoutAllowList"/>, KHÔNG đổi ngưỡng. Test của màn có
    /// tên ở đây gọi <see cref="IgnoreWhenDeferred"/> ở đầu thân test nên NUnit báo "Ignored" kèm mã phiếu — cổng nhìn ra
    /// ngay 5 màn đang nợ, chứ không thấy màu xanh giả. Gỡ một dòng ở đây là test đó chạy lại đầy đủ ngay lượt sau.
    /// </para>
    /// <para>
    /// Mã phiếu giữ nguyên số của bảng kế hoạch (<c>UX-PASS2-PLAN.md</c> mục 2(c)) nên dãy KHÔNG liền: W9-02 và
    /// W9-07…W9-15 là các màn TRONG đợt, đã bị rút khỏi danh sách này ở lượt sửa 19/9/2026 và trả về cho một gói sửa bố
    /// cục riêng. Đánh số lại sẽ làm mọi báo cáo cũ trỏ sai phiếu.
    /// </para>
    /// <para>
    /// Số chỗ ghi theo lượt EditMode 6000.6 toàn bộ của nhánh cổng (<c>full-6000-3.xml</c>, 18/9/2026) — gộp 6 cỡ cửa sổ ×
    /// 2 ngôn ngữ, nên một lỗi bố cục thật thường được đếm nhiều lần. MẤT ĐỘ PHỦ: một mục hoãn dừng CẢ test, tức dừng luôn
    /// những cặp cỡ × ngôn ngữ đang SẠCH của màn đó; mỗi lý do dưới đây khai đúng số cặp sạch bị mất để W9 biết mình đang
    /// mù ở đâu (việc tách test theo cỡ nằm trong phạm vi W9).
    /// </para>
    /// </summary>
    internal static class UxLayoutDeferralList
    {
        /// <summary>Nhãn cỡ của ma trận bố cục — viết một chỗ để mục hoãn không gõ sai "1024x700" thành "1024x760".</summary>
        private const string Size700 = "700x560";

        private const string Size820 = "820x560";
        /// <summary>Cỡ MỚI của W9-20. Nó nằm trong danh sách hoãn của mọi màn đang đỏ ở 820 và 1024 vì chưa ai đo nó,
        /// và coi một cỡ CHƯA ĐO là sạch thì mục hoãn lại thành chỗ trốn kiểm. Gói màn đo xong thì gỡ.</summary>
        private const string Size950 = "950x700";
        private const string Size1024 = "1024x700";
        private const string Size1280 = "1280x760";
        private const string Size1440 = "1440x900";

        private static readonly UxLayoutDeferralEntry[] Entries =
        {
            new UxLayoutDeferralEntry("W9-01", "export", 241,
                "Màn Xuất JSON nằm NGOÀI phạm vi đợt W8 (USER chốt 18/9/2026) và là việc của gói G-W9-EXPORT. Số chỗ đo "
                + "lại trên 6d73130 là 241 (sổ W9 ghi 245 theo lượt 2; lượt 3 sửa USS dùng chung nên kéo theo màn này). "
                + "HOÃN THEO CỠ từ đợt W9: 1920x1040 đo được 0 chỗ ở cả hai ngôn ngữ nên cỡ đó CHẠY ĐẦY ĐỦ ngay lượt này "
                + "— một lần sửa làm hỏng cỡ rộng sẽ đỏ ngay, không phải đợi hết đợt.",
                Size700, Size820, Size950, Size1024, Size1280, Size1440),
            new UxLayoutDeferralEntry("W9-03", "overview", 118,
                "Màn Tổng quan nằm NGOÀI phạm vi đợt W8 (USER chốt 18/9/2026) và là việc của gói G-W9-SCREENS. Gốc lỗi "
                + "R1: bảng 'đợt tới' đặt bề rộng cột cố định nên ở cỡ hẹp cột bị đẩy RA NGOÀI cửa sổ (ô đo được ở x=745 "
                + "trên cửa sổ rộng 700). KHÔNG cỡ nào sạch — đo lại trên 6d73130 thấy đỏ ở cả sáu cỡ (1920 còn 3 chỗ) — "
                + "nên mục này vẫn hoãn cả màn; hoãn theo cỡ ở đây không cứu được cặp nào.",
                new string[0]),
            new UxLayoutDeferralEntry("W9-04", "shell-rail-status", 118,
                "Khung chung (rail + status bar) nằm NGOÀI phạm vi đợt W8 theo chốt của USER 18/9/2026. NGHIỆM THU CÒN "
                + "NỢ, và nợ NẶNG HƠN sổ cũ ghi: trước W9-26 test này mở màn Tổng quan rồi để auditor duyệt CẢ cây, nên "
                + "118 chỗ của nó trùng 100% với 118 chỗ của màn Tổng quan và KHÔNG chỗ nào thuộc rail hay status bar — "
                + "tức UX-20 chưa từng có tiêu chí nghiệm thu thật. W9-26 đã thu phạm vi về đúng hai nhánh đó; con số 118 "
                + "vì vậy là số CŨ và phải đo lại, nên mục này giữ hoãn cả màn cho tới lượt đo của G-W9-SCREENS.",
                new string[0]),
            new UxLayoutDeferralEntry("W9-05", "event-types", 100,
                "Màn Loại event nằm NGOÀI phạm vi đợt W8 (USER chốt 18/9/2026) và là việc của gói G-W9-SCREENS. Gốc lỗi "
                + "R4/R5: ô bảng và tiêu đề cột rộng cố định 25–35px trong khi chữ cần 35–119px. ĐÃ THỬ hoãn theo cỡ và "
                + "PHẢI RÚT LẠI: bảng đo của W9-PLAN chỉ chạy trên 6000.6, nơi ba cỡ rộng đo được 0 chỗ — nhưng lượt kiểm "
                + "thật trên 2022.3 thấy 30 chỗ childOverflow ở ĐÚNG ba cỡ đó (ô nhập của form tràn khỏi hàng chứa). Không "
                + "cỡ nào sạch trên CẢ HAI bản Unity, nên hoãn cả màn. Đây là phát hiện của đợt W9, không phải của bảng đo: "
                + "mọi con số 'cỡ này sạch' của W9-PLAN đều là số 6000.6 và phải đo lại trên 2022.3 trước khi tin.",
                new string[0]),
            new UxLayoutDeferralEntry("W9-06", "validation", 40,
                "Màn Kiểm lịch nằm NGOÀI phạm vi đợt W8 (USER chốt 18/9/2026) và là việc của gói G-W9-SCREENS. Gốc lỗi "
                + "R8 đáng chú ý nhất: nút 'Kiểm lại' chật 4px ở MỌI cỡ kể cả 1920, tức là một nút dựng sai ngay từ đầu "
                + "chứ không phải lỗi của cửa sổ hẹp — nên KHÔNG cỡ nào sạch và hoãn theo cỡ không cứu được cặp nào.",
                new string[0]),
        };

        /// <summary>Mọi mục — dùng cho test gác danh sách và cho báo cáo của cổng người.</summary>
        public static IReadOnlyList<UxLayoutDeferralEntry> All
        {
            get { return Entries; }
        }

        /// <summary>Mục hoãn của màn <paramref name="screenId"/>, hoặc null nếu màn đó không được hoãn.</summary>
        public static UxLayoutDeferralEntry Find(string screenId)
        {
            foreach (UxLayoutDeferralEntry entry in Entries)
            {
                if (entry.ScreenId == screenId) return entry;
            }

            return null;
        }

        /// <summary>
        /// Dừng test với trạng thái Ignored kèm mã phiếu W9 nếu màn <paramref name="screenId"/> đang được hoãn ở MỌI cỡ;
        /// hoãn theo cỡ thì trả về và <c>UxLayoutAuditTests.RunScreen</c> tự bỏ đúng những cỡ đó.
        /// </summary>
        public static void IgnoreWhenDeferred(string screenId)
        {
            UxLayoutDeferralEntry entry = Find(screenId);
            if (entry == null || entry.DeferredSizes.Count > 0) return;
            Assert.Ignore(entry.IgnoreMessage);
        }
    }
}
