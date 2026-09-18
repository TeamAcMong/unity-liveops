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
        /// true = chạy lại <see cref="AfterOpen"/> sau MỖI lần đổi cỡ. Cần cho trạng thái mà đổi cỡ làm mất (popover là cửa sổ
        /// riêng, đóng ngay khi cửa sổ chủ đổi khung).
        /// </summary>
        public bool ReapplyAfterResize { get; private set; }

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
