using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Một mục HOÃN của kiểm bố cục: màn nào, phiếu nào nhận (W9-NN hoặc W11-NN), lúc hoãn đang có bao nhiêu chỗ không
    /// dùng được, và VÌ SAO đợt đang chạy không sửa. Ba trường đầu để người soát tìm lại đúng phiếu; lý do là bắt buộc vì
    /// hoãn là cách duy nhất cổng được phép đứng yên trước một màn đỏ — không có lý do thì lần sau không ai phân biệt
    /// được "đã hẹn sửa" với "đã quên".
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

        /// <summary>Mã phiếu (dạng <c>W9-NN</c> hoặc <c>W11-NN</c>) — chỗ duy nhất nối test đỏ với việc đã hẹn làm.</summary>
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
    /// Danh sách HOÃN của kiểm bố cục. Đợt W8-UX mở nó với 5 màn NGOÀI phạm vi đợt (USER chốt 18/9/2026); đợt W9 đóng nốt
    /// cả năm và danh sách RỖNG trở lại ở cổng W9 (20/9/2026). Đợt W11 mở lại, lần này với 12 mục — USER chốt 21/9/2026:
    /// <b>"Sửa lỗi mắt thấy rồi phát hành; nợ bố cục dữ liệu xấu nhất để đợt W11. Ghi nợ thành id + số liệu rõ, KHÔNG giấu."</b>
    /// <para>
    /// 12 mục ở đây là ĐÚNG 12 ca đỏ mà cổng đợt W10 đã khai công khai trong <c>W10-GATE-REPORT-2.md</c> §5.2, đo lại một
    /// lần nữa sau khi gói G-W11-LIMITS dựng lại bộ dữ liệu xấu nhất bằng ĐÚNG giới hạn của
    /// <see cref="LiveOpsIdentifierLimits"/>. Không mục nào là lỗi mới: chín màn khớp số với bản đo độc lập của gói
    /// G-W10-CAL2, ba màn còn lại là màn do chính đợt W10 dựng thêm.
    /// </para>
    /// <para>
    /// VÌ SAO HOÃN CHỨ KHÔNG SỬA: đây là nợ BỐ CỤC — hàng ngang không xuống dòng, ô không co, cột cố định hẹp hơn dữ liệu
    /// hợp lệ dài nhất. Sửa nó là đổi hình dạng của sáu màn, tức một đợt thiết kế riêng, không phải một bản vá kèm theo
    /// đợt sửa lỗi mắt thấy. Hoãn ở đây KHÁC hẳn nới ngưỡng: không câu assert nào đổi, không ngưỡng nào dịch, và
    /// <see cref="UxLayoutAllowList"/> — chỗ tha SUÔNG — còn NGẮN ĐI một dòng (nhãn thanh trục chuyển sang
    /// <see cref="UxTruncationExemption"/>, nơi hai điều kiện được ĐO lại mỗi lượt). Gỡ một dòng khỏi mảng dưới là ca
    /// chạy lại đầy đủ ngay lượt sau và đỏ đúng con số đã khai.
    /// </para>
    /// <para>
    /// BỐN MỤC KHÔNG PHẢI NỢ "DỮ LIỆU DÀI": W11-09 (ô lỗi của inspector), W11-10 và W11-12 (màn RỖNG), W11-11 (chưa có
    /// dấu đã đăng) đứng trên dữ liệu ĐẸP hoặc dữ liệu RỖNG — số của chúng không nhúc nhích khi fixture đổi. Ghi rõ ở đây
    /// thay vì gộp chung một lý do cho cả 12, vì gộp là nói sai về bốn màn và đợt W11 sẽ tìm nhầm chỗ.
    /// </para>
    /// </summary>
    internal static class UxLayoutDeferralList
    {
        /// <summary>Số chỗ đo trên Unity 6000.6.0f1, lượt EditMode category UxGate ngày 21/9/2026, trên cây của G-W11-LIMITS.</summary>
        private static readonly UxLayoutDeferralEntry[] Entries =
        {
            new UxLayoutDeferralEntry("W11-01", "calendar-worst-data", 176,
                "Nợ bố cục dữ liệu dài, hẹn đợt W11. textCut 98 · childOverflow 70 · siblingOverlap 8. Màn Lịch trên dữ "
                + "liệu bằng đúng giới hạn: id đợt 64 ký tự không vừa hàng danh sách lẫn tiêu đề inspector ở mọi cỡ. Sửa "
                + "là đổi hình dạng hàng (xuống dòng hoặc cho co), tức việc của một đợt thiết kế."),
            new UxLayoutDeferralEntry("W11-02", "event-types-worst-data", 157,
                "Nợ bố cục dữ liệu dài, hẹn đợt W11. textCut 78 · childOverflow 79. Bảng Loại event có hai cột khai cứng "
                + "140px (Id loại, Config key) — hai cột ấy ĐÃ được miễn có điều kiện, phần còn lại là hàng của bảng tràn "
                + "và cột 'Tên hiển thị' GIÃN vẫn cắt chữ, tức lỗi bố cục thật."),
            new UxLayoutDeferralEntry("W11-03", "recurring-worst-data", 145,
                "Nợ bố cục dữ liệu dài, hẹn đợt W11. textCut 120 · childOverflow 25. Câu luật lặp ghép tiền tố id 44 ký "
                + "tự với chu kỳ 8760 giờ và một lần chạy 999 giờ — câu dài nhất mà dữ liệu hợp lệ dựng được, và nó không "
                + "xuống dòng."),
            new UxLayoutDeferralEntry("W11-04", "overview-worst-data", 98,
                "Nợ bố cục dữ liệu dài, hẹn đợt W11. textCut 56 · childOverflow 42. Thẻ tổng quan in id đợt và tên loại "
                + "trên cùng một hàng với số đếm, nên hàng vỡ ngay ở cỡ hẹp."),
            new UxLayoutDeferralEntry("W11-05", "export-worst-data", 56,
                "Nợ bố cục dữ liệu dài, hẹn đợt W11. textCut 56, toàn bộ là chữ bị cắt. Màn Xuất JSON in tên người đăng "
                + "64 ký tự, ghi chú 160 ký tự và sha 64 ký tự trên các hàng một dòng."),
            new UxLayoutDeferralEntry("W11-06", "calendar-multi-selection-worst-data", 42,
                "Nợ bố cục dữ liệu dài, hẹn đợt W11. textCut 28 · childOverflow 14. Thanh hành động hàng loạt của trạng "
                + "thái chọn nhiều đợt ghép id của các đợt đã chọn vào một câu."),
            new UxLayoutDeferralEntry("W11-07", "calendar-medium-drawer-worst-data", 20,
                "Nợ bố cục dữ liệu dài, hẹn đợt W11. textCut · childOverflow · siblingOverlap. Ở bậc --medium inspector "
                + "là DRAWER hẹp, nên id 64 ký tự vỡ sớm hơn hẳn so với bậc rộng."),
            new UxLayoutDeferralEntry("W11-08", "add-event-popover-worst-data", 16,
                "Nợ bố cục dữ liệu dài, hẹn đợt W11. textCut · childOverflow · siblingOverlap. Danh sách chọn loại trong "
                + "popover Thêm đợt rộng cố định 320px, id loại 64 ký tự làm danh sách sinh thanh cuộn NGANG."),
            new UxLayoutDeferralEntry("W11-09", "calendar-inspector-fielderror", 16,
                "KHÔNG phải nợ dữ liệu dài — hẹn đợt W11 cùng nhóm bố cục. textCut 8 · childOverflow 8. Màn đứng trên "
                + "mẫu ĐẸP: đợt có giờ kết thúc không đọc được làm hàng ô ngày giờ mọc thêm biểu tượng lỗi rồi đẩy nhau ra "
                + "khỏi hàng. Số không đổi khi bộ dữ liệu xấu nhất đổi, đúng như dự đoán."),
            new UxLayoutDeferralEntry("W11-10", "calendar-empty", 5,
                "KHÔNG phải nợ dữ liệu dài — hẹn đợt W11 cùng nhóm bố cục. textCut 5. Màn Lịch khi lịch RỖNG: câu 'chưa "
                + "có gì' và nhóm nút của trạng thái rỗng bị cắt ở cỡ hẹp."),
            new UxLayoutDeferralEntry("W11-11", "export-no-baseline", 3,
                "KHÔNG phải nợ dữ liệu dài — hẹn đợt W11 cùng nhóm bố cục. textCut 3. Màn Xuất JSON khi CHƯA có dấu đã "
                + "đăng: câu giải thích 'chưa có bản so' bị cắt."),
            new UxLayoutDeferralEntry("W11-12", "overview-empty", 1,
                "KHÔNG phải nợ dữ liệu dài — hẹn đợt W11 cùng nhóm bố cục. textCut 1, đúng một chỗ. Màn Tổng quan khi "
                + "không có đợt nào sắp diễn ra và không có việc cần làm."),
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
