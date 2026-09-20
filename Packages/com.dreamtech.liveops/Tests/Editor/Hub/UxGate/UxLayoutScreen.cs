using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Một màn của ma trận kiểm bố cục W8-UX: mở ở đâu, ở những cỡ nào, ở những ngôn ngữ nào, phần tử nào bắt buộc dùng được,
    /// vùng nào phải giãn, hai khối nào không được đè nhau, dấu màu nào phải đủ tương phản, và câu assert được phép đỏ vì phần
    /// nào của cây.
    /// <para>
    /// Tách khỏi <see cref="UxLayoutAuditTests"/> vì bảng màn là thứ dài nhất của cổng và là chỗ dễ khai THIẾU nhất: có một kiểu
    /// riêng thì thêm một màn là điền đủ ô, không phải nhớ thứ tự tám tham số.
    /// </para>
    /// </summary>
    // Category ở đây là dấu cho code-lint (luật test-ui-category) và cho người đọc: bảng màn cầm EditorWindow của popover/hộp
    // xác nhận nên nó chỉ có nghĩa khi Unity CÓ đồ hoạ.
    [NUnit.Framework.Category(LiveOpsHubTestCategories.UI)]
    internal sealed class UxLayoutScreen
    {
        public UxLayoutScreen(string id, string sectionId)
        {
            Id = id;
            SectionId = sectionId;
        }

        public string Id { get; }
        public string SectionId { get; }

        /// <summary>Thao tác THẬT chạy sau khi cửa sổ có layout (chọn đợt, mở popover); null = không có.</summary>
        public Func<UxHubWindowFixture, IEnumerator> AfterOpen { get; private set; }

        /// <summary>
        /// Services của màn; null = mẫu thiết kế (<c>LiveOpsHubTestServices.DesignSampleScenario</c>). Đây là đường DUY NHẤT
        /// để ma trận bố cục dựng được dữ liệu XẤU NHẤT (chuỗi dài nhất, giờ không đọc được, danh sách rỗng, số lớn): ba lỗi
        /// W9-29/30/31 lọt lưới vì mọi màn của ma trận đều đứng trên cùng một tài liệu ĐẸP, nên trạng thái xấu nhất của từng
        /// màn chưa bao giờ được bày ra để đo.
        /// </summary>
        public Func<LiveOpsHubServices> Services { get; private set; }

        /// <summary>
        /// Điều kiện "trạng thái của màn đã dựng xong" đọc ngay TRƯỚC lượt đo; null = không kiểm.
        /// <para>
        /// Vì sao cần bên cạnh <see cref="AfterOpen"/>: có trạng thái TỰ TẮT theo đồng hồ THẬT (toast sống đúng 6 giây kể từ
        /// lúc hiện — <c>LiveOpsToast.VisibleSeconds</c>, đếm theo <c>EditorApplication.timeSinceStartup</c>). Máy bận thì
        /// quãng giữa lúc dựng và lúc đo dài hơn quãng đó, trạng thái biến mất, và màn đỏ vì một lý do KHÔNG phải lỗi bố cục.
        /// Có điều kiện thì lượt kiểm chờ theo ĐIỀU KIỆN + hạn giờ rồi dựng lại một lần, thay vì tin là nó còn sống (W9-28).
        /// </para>
        /// <para>
        /// Khai đúng mức (soát W10 R-08): đây là HÀNG RÀO PHÒNG XA, chưa phải một gốc đã đo được. Trong mọi lượt đã chạy của
        /// đợt W10, đường dựng-lại-vì-hết-hạn CHƯA một lần nào kích hoạt (không log nào mang câu "dựng trạng thái hai lần vẫn
        /// không đạt điều kiện đo") — mỗi lượt đo cách lúc dựng khoảng nửa giây, ngắn hơn sáu giây cả chục lần. Gốc ĐÃ chứng
        /// minh của W9-28 là cú kéo cộng dồn, chữa bằng <see cref="RebuildPerSize"/>.
        /// </para>
        /// </summary>
        public Func<UxHubWindowFixture, bool> ReadyCondition { get; private set; }

        /// <summary>Hạn giờ chờ <see cref="ReadyCondition"/> sau mỗi lần dựng trạng thái (ms).</summary>
        public int ReadyTimeoutMilliseconds { get; private set; }

        /// <summary>
        /// true = chạy lại <see cref="AfterOpen"/> sau MỖI lần đổi cỡ. Cần cho trạng thái mà đổi cỡ làm mất (popover là cửa sổ
        /// riêng, đóng ngay khi cửa sổ chủ đổi khung).
        /// </summary>
        public bool ReapplyAfterResize { get; private set; }

        /// <summary>
        /// true = mở cửa sổ MỚI cho từng cỡ thay vì kéo mép một cửa sổ đang mở.
        /// <para>
        /// Vì sao cần: <see cref="ReapplyAfterResize"/> chạy lại thao tác trên CÙNG một phiên, nên thao tác nào để lại dấu
        /// trong tài liệu sẽ CỘNG DỒN. Màn <c>calendar-toast</c> kéo một thanh thêm 60px mỗi cỡ; bảy cỡ là bảy lần dời trên
        /// cùng một đợt và đợt trôi dần khỏi khoảng ngày trục đang vẽ — đúng mẫu đỏ "trục không vẽ thanh 'entry-hunt-0914'"
        /// từng thấy ở lượt TOÀN BỘ của cổng W9 (phiếu W9-28). Dựng lại phiên cho mỗi cỡ là chữa đúng gốc đó: mỗi cỡ đo trên
        /// một tài liệu nguyên vẹn, không có cỡ nào chịu hậu quả của cỡ trước.
        /// </para>
        /// <para>
        /// Giá phải trả là mở/đóng cửa sổ nhiều hơn (mỗi cỡ một lượt), nên chỉ bật cho màn có thao tác GHI vào tài liệu —
        /// chọn thanh hay mở popover không đổi tài liệu và không cần.
        /// </para>
        /// </summary>
        public bool RebuildPerSize { get; private set; }

        /// <summary>Cửa sổ được kiểm; null = chính cửa sổ hub. Dùng cho popover Thêm đợt và hộp xác nhận (cửa sổ RIÊNG).</summary>
        public Func<UxHubWindowFixture, EditorWindow> WindowPicker { get; private set; }

        /// <summary>Cỡ cửa sổ của màn này; null = cả sáu cỡ user chốt.</summary>
        public IReadOnlyList<UxWindowSize> Sizes { get; private set; }

        public IReadOnlyList<string> RequiredElements { get; private set; }
        public IReadOnlyList<UxLayoutStretchRule> StretchRules { get; private set; }
        public IReadOnlyList<UxLayoutContrastRule> ContrastRules { get; private set; }
        public IReadOnlyList<UxLayoutNoOverlapRule> NoOverlapRules { get; private set; }
        public IReadOnlyList<string> SubtreeSelectors { get; private set; }
        public bool ScreenRulesOnly { get; private set; }

        public UxLayoutScreen WithAfterOpen(Func<UxHubWindowFixture, IEnumerator> afterOpen, bool reapplyAfterResize = false)
        {
            AfterOpen = afterOpen;
            ReapplyAfterResize = reapplyAfterResize;
            return this;
        }

        /// <summary>Dựng lại phiên cho MỖI cỡ — xem <see cref="RebuildPerSize"/>.</summary>
        public UxLayoutScreen WithRebuildPerSize()
        {
            RebuildPerSize = true;
            return this;
        }

        /// <summary>Services riêng của màn (dữ liệu xấu nhất) — xem <see cref="Services"/>.</summary>
        public UxLayoutScreen WithServices(Func<LiveOpsHubServices> services)
        {
            Services = services;
            return this;
        }

        /// <summary>Điều kiện "trạng thái đã dựng xong" cùng hạn giờ chờ — xem <see cref="ReadyCondition"/>.</summary>
        public UxLayoutScreen WithReadyCondition(Func<UxHubWindowFixture, bool> readyCondition, int timeoutMilliseconds)
        {
            ReadyCondition = readyCondition;
            ReadyTimeoutMilliseconds = timeoutMilliseconds;
            return this;
        }

        public UxLayoutScreen WithWindowPicker(Func<UxHubWindowFixture, EditorWindow> windowPicker)
        {
            WindowPicker = windowPicker;
            return this;
        }

        public UxLayoutScreen WithSizes(params UxWindowSize[] sizes)
        {
            Sizes = sizes;
            return this;
        }

        public UxLayoutScreen WithRequiredElements(params string[] requiredElements)
        {
            RequiredElements = requiredElements;
            return this;
        }

        public UxLayoutScreen WithStretchRules(params UxLayoutStretchRule[] stretchRules)
        {
            StretchRules = stretchRules;
            return this;
        }

        public UxLayoutScreen WithContrastRules(params UxLayoutContrastRule[] contrastRules)
        {
            ContrastRules = contrastRules;
            return this;
        }

        public UxLayoutScreen WithNoOverlapRules(params UxLayoutNoOverlapRule[] noOverlapRules)
        {
            NoOverlapRules = noOverlapRules;
            return this;
        }

        /// <summary>
        /// Giới hạn phát hiện CHUNG vào các nhánh này và tắt phần còn lại khỏi câu assert. Dùng cho test tự nhận "tách một
        /// nguyên nhân": không giới hạn thì câu assert gom 225 chỗ của cả cửa sổ và không còn là tín hiệu của nguyên nhân nào
        /// (R-04).
        /// </summary>
        public UxLayoutScreen WithScreenRulesOnly(params string[] subtreeSelectors)
        {
            ScreenRulesOnly = true;
            SubtreeSelectors = subtreeSelectors != null && subtreeSelectors.Length > 0 ? subtreeSelectors : null;
            return this;
        }

        public UxLayoutRules Rules()
        {
            return new UxLayoutRules(RequiredElements, StretchRules, ContrastRules, NoOverlapRules, SubtreeSelectors, ScreenRulesOnly);
        }
    }
}
