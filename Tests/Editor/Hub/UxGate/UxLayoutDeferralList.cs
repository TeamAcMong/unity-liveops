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
        public UxLayoutDeferralEntry(string deferralId, string screenId, int findingCount, string reason)
        {
            if (string.IsNullOrEmpty(deferralId)) throw new ArgumentNullException(nameof(deferralId));
            if (string.IsNullOrEmpty(screenId)) throw new ArgumentNullException(nameof(screenId));
            if (string.IsNullOrEmpty(reason)) throw new ArgumentNullException(nameof(reason));
            DeferralId = deferralId;
            ScreenId = screenId;
            FindingCount = findingCount;
            Reason = reason;
        }

        /// <summary>Mã phiếu của đợt W9 (dạng <c>W9-NN</c>) — chỗ duy nhất nối test đỏ với việc đã hẹn làm.</summary>
        public string DeferralId { get; }

        /// <summary>Màn của kiểm bố cục (<see cref="UxLayoutScreen.Id"/>) mà test đang dựng.</summary>
        public string ScreenId { get; }

        /// <summary>Số chỗ không dùng được đo được lúc hoãn — để lượt W9 biết ngay mình đang nhận việc to hay nhỏ.</summary>
        public int FindingCount { get; }

        public string Reason { get; }

        /// <summary>Câu người chạy test đọc được: mã phiếu đứng đầu để lọc, rồi số chỗ, rồi lý do.</summary>
        public string IgnoreMessage
        {
            get
            {
                // InvariantCulture vì đây là chuỗi MÁY đọc (người soát grep theo mã phiếu + con số), không phải chuỗi hiển
                // thị: máy chạy cổng đổi ngôn ngữ hệ thống thì con số phải vẫn y nguyên. Cùng quy ước với UxLayoutAuditor.
                return DeferralId + " — màn '" + ScreenId + "' còn " + FindingCount.ToString(CultureInfo.InvariantCulture)
                    + " chỗ không dùng được. " + Reason;
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
        private static readonly UxLayoutDeferralEntry[] Entries =
        {
            new UxLayoutDeferralEntry("W9-01", "export", 245,
                "Màn Xuất JSON nằm NGOÀI phạm vi đợt W8 (USER chốt 18/9/2026). 245 chỗ là khối lượng của một đợt riêng, "
                + "không phải việc kèm theo của một gói sửa hành trình. MẤT ĐỘ PHỦ: hoãn cả test nên 3/12 cặp cỡ × ngôn "
                + "ngữ đang SẠCH (1440x900 vi, 1920x1040 vi, 1920x1040 en) cũng ngừng được kiểm tới W9."),
            new UxLayoutDeferralEntry("W9-03", "overview", 126,
                "Màn Tổng quan nằm NGOÀI phạm vi đợt W8 (USER chốt 18/9/2026). 126 chỗ là bố cục của cả màn ở 6 cỡ × 2 "
                + "ngôn ngữ, phải đo lại và chia việc ở đợt riêng. MẤT ĐỘ PHỦ: 2/12 cặp đang SẠCH (1440x900 vi, "
                + "1920x1040 vi) cũng ngừng được kiểm tới W9."),
            new UxLayoutDeferralEntry("W9-04", "shell-rail-status", 126,
                "Khung chung (rail + status bar) nằm NGOÀI phạm vi đợt W8 theo chốt của USER 18/9/2026, dù sửa khung "
                + "đụng MỌI màn nên phải là đợt riêng. NGHIỆM THU CÒN NỢ: đây đúng là test 'L' của đầu việc UX-20 "
                + "(UX-FIX-PLAN.md:56, tên cũ Shell_StatusBar_NoOverlap) — gói E coi UX-20 là xong nhưng tiêu chí chưa "
                + "bao giờ xanh. MẤT ĐỘ PHỦ: 2/12 cặp đang SẠCH (1440x900 vi, 1920x1040 vi) ngừng được kiểm tới W9."),
            new UxLayoutDeferralEntry("W9-05", "event-types", 104,
                "Màn Loại event nằm NGOÀI phạm vi đợt W8 (USER chốt 18/9/2026). 104 chỗ là bố cục của cả màn, phải đo "
                + "lại và chia việc ở đợt riêng. MẤT ĐỘ PHỦ nặng nhất nhóm: lỗi chỉ ở ba cỡ hẹp, nên hoãn cả test làm "
                + "6/12 cặp đang SẠCH (1280x760, 1440x900, 1920x1040 — cả vi lẫn en) ngừng được kiểm tới W9."),
            new UxLayoutDeferralEntry("W9-06", "validation", 76,
                "Màn Kiểm lịch nằm NGOÀI phạm vi đợt W8 (USER chốt 18/9/2026). 76 chỗ là bố cục của cả màn ở 6 cỡ × 2 "
                + "ngôn ngữ, phải đo lại và chia việc ở đợt riêng. MẤT ĐỘ PHỦ: 0/12 cặp sạch — mọi cỡ đều đang đỏ nên "
                + "hoãn không che mất cặp nào đang xanh."),
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
        /// Dừng test với trạng thái Ignored kèm mã phiếu W9 nếu màn <paramref name="screenId"/> đang được hoãn; không
        /// được hoãn thì trả về và test chạy tiếp bình thường. Gọi ở ĐẦU thân test để không tốn một lượt mở hub.
        /// </summary>
        public static void IgnoreWhenDeferred(string screenId)
        {
            UxLayoutDeferralEntry entry = Find(screenId);
            if (entry == null) return;
            Assert.Ignore(entry.IgnoreMessage);
        }
    }
}
