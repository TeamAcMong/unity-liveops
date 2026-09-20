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
    /// Danh sách HOÃN của kiểm bố cục — nay RỖNG. Đợt W8-UX mở nó với đúng 5 màn NGOÀI phạm vi đợt mà USER chốt ngày
    /// 18/9/2026 (Xuất JSON, Tổng quan, Loại event, Kiểm lịch, khung); đợt W9 đóng nốt cả năm, phiếu cuối cùng là W9-01
    /// (màn Xuất JSON, gỡ ở cổng đợt W9 ngày 20/9/2026). Từ đây MỌI màn của ma trận chạy đầy đủ ở MỌI cỡ.
    /// <para>
    /// Giữ lại lớp này thay vì xoá vì nó là hợp đồng: nếu một đợt sau buộc phải hoãn một màn thì đường đi đã có sẵn và có
    /// gác (<c>UxLayoutAuditTests.DeferralList_EveryEntry_HasUniqueIdAndReason</c> khoá cả tập mã phiếu lẫn tập màn vào
    /// phần USER đã duyệt, nay là tập RỖNG — thêm một dòng ở đây mà không sửa hai mảng đó là test đỏ ngay).
    /// </para>
    /// <para>
    /// Luật của danh sách khi nó còn dùng tới: KHÔNG xoá test, KHÔNG nới <see cref="UxLayoutAllowList"/>, KHÔNG đổi
    /// ngưỡng. Hoãn nói "chỗ này là LỖI THẬT, chưa tới lượt sửa" chứ không đụng một câu assert nào.
    /// </para>
    /// </summary>
    internal static class UxLayoutDeferralList
    {
        /// <summary>Rỗng từ cổng đợt W9 — xem chú thích của lớp.</summary>
        private static readonly UxLayoutDeferralEntry[] Entries = new UxLayoutDeferralEntry[0];

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
