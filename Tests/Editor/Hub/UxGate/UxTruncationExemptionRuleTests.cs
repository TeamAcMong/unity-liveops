using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Tự kiểm LUẬT rút gọn có điều kiện, không dựng cửa sổ: mọi nhánh của <see cref="UxTruncationExemption.Allows"/> trên cây
    /// element dựng tay, cộng ba câu khoá kỷ luật của bảng miễn trừ (mỗi mục phải có ca chứng minh, mỗi mục phải khai chỗ đọc
    /// đủ, và luật KHÔNG được lan sang loại phát hiện khác).
    /// <para>
    /// Vì sao tách khỏi <see cref="UxTruncationExemptionTests"/>: ca ở đây là ca LOGIC — chạy được dưới lượt
    /// <c>-testCategory "!LiveOpsHub.UI"</c>, không cần đồ hoạ. Nhét chung fixture với ca dựng cửa sổ là đúng cái bẫy R-07
    /// của đợt W10 (ca mang nhãn UI thì lượt Logic không bao giờ chạy nó).
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class UxTruncationExemptionRuleTests
    {
        private const string Screen = "event-types-worst-data";
        private const string FullText = "tournament-of-the-eternal-flame-season-twelve";

        /// <summary>Bản đã rút do CHÍNH hub cắt: chuỗi trong element mất đuôi, chỉ còn dấu "…".</summary>
        private const string ElidedText = "tournament-of-the-eter…";

        [Test]
        public void FixedWidthTableCell_WithFullTextTooltip_IsExempt()
        {
            Label cell = Cell(UxTruncationExemption.EventTypesTypeIdCellName, FullText, FullText);
            Assert.IsTrue(UxTruncationExemption.Allows(Screen, UxLayoutFindingKinds.TextCut, cell, out string note));
            Assert.AreEqual(string.Empty, note, "miễn trừ đạt thì không có gì để nói thêm vào dòng chẩn đoán");
        }

        [Test]
        public void FixedWidthTableCell_WithoutTooltip_IsNotExempt()
        {
            Label cell = Cell(UxTruncationExemption.EventTypesTypeIdCellName, FullText, string.Empty);
            Assert.IsFalse(UxTruncationExemption.Allows(Screen, UxLayoutFindingKinds.TextCut, cell, out string note));
            StringAssert.Contains("không mang tooltip", note);
        }

        /// <summary>Tooltip CÓ nhưng nói chuyện khác (một câu trạng thái) thì không phải đường đọc lại chữ đã rút.</summary>
        [Test]
        public void FixedWidthTableCell_WithUnrelatedTooltip_IsNotExempt()
        {
            Label cell = Cell(UxTruncationExemption.EventTypesTypeIdCellName, FullText, "loại chưa khai báo");
            Assert.IsFalse(UxTruncationExemption.Allows(Screen, UxLayoutFindingKinds.TextCut, cell, out string note));
            StringAssert.Contains("không chứa đủ chữ", note);
        }

        /// <summary>Hub tự cắt: element chỉ còn mẩu đầu cộng "…", nên phép so phải hỏi tooltip có DÀI HƠN mẩu ấy không.</summary>
        [Test]
        public void HubElidedText_WithLongerTooltip_IsExempt()
        {
            Label cell = Cell(UxTruncationExemption.EventTypesTypeIdCellName, ElidedText, FullText);
            Assert.IsTrue(UxTruncationExemption.Allows(Screen, UxLayoutFindingKinds.TextCut, cell, out _));
        }

        /// <summary>Tooltip chỉ chép lại đúng mẩu ĐÃ CẮT thì không thêm một ký tự nào cho người đọc.</summary>
        [Test]
        public void HubElidedText_WithTooltipEqualToVisiblePart_IsNotExempt()
        {
            Label cell = Cell(UxTruncationExemption.EventTypesTypeIdCellName, ElidedText, "tournament-of-the-eter");
            Assert.IsFalse(UxTruncationExemption.Allows(Screen, UxLayoutFindingKinds.TextCut, cell, out string note));
            StringAssert.Contains("không phải bản đầy đủ", note);
        }

        /// <summary>
        /// W9-25 được MIỄN cho ô thuộc diện, KHÔNG cần tooltip: ở đó chữ vẫn hiện đủ, chỉ khoảng dư mỏng — mà khoảng dư mỏng
        /// trong một ô bề rộng CỐ ĐỊNH là điều đương nhiên chứ không phải rủi ro cắt im lặng.
        /// </summary>
        [Test]
        public void TextTight_InExemptCell_IsExemptWithoutTooltip()
        {
            Label cell = Cell(UxTruncationExemption.EventTypesTypeIdCellName, FullText, string.Empty);
            Assert.IsTrue(UxTruncationExemption.Allows(Screen, UxLayoutFindingKinds.TextTight, cell, out _));
        }

        /// <summary>Ô KHÔNG thuộc bảng miễn trừ thì luật W9-25 áp nguyên như cũ, dù nó nằm trong một bảng.</summary>
        [Test]
        public void TextTight_OutsideExemptCell_IsNotExempt()
        {
            Label cell = Cell("display-name", FullText, FullText);
            Assert.IsFalse(UxTruncationExemption.Allows(Screen, UxLayoutFindingKinds.TextTight, cell, out _));
        }

        /// <summary>
        /// Luật chỉ nói về CHỮ bị rút. Mượn nó để tha tràn con hay chồng anh em là mở một cửa user không hề chốt.
        /// </summary>
        [Test]
        public void OtherFindingKinds_AreNeverExempt()
        {
            Label cell = Cell(UxTruncationExemption.EventTypesTypeIdCellName, FullText, FullText);
            Assert.IsFalse(UxTruncationExemption.Allows(Screen, UxLayoutFindingKinds.ChildOverflow, cell, out _));
            Assert.IsFalse(UxTruncationExemption.Allows(Screen, UxLayoutFindingKinds.SiblingOverlap, cell, out _));
            Assert.IsFalse(UxTruncationExemption.Allows(Screen, UxLayoutFindingKinds.UntitledColumn, cell, out _));
        }

        /// <summary>
        /// Chữ của một Ô NHẬP nằm sâu trong <c>unity-text-input</c>, còn tooltip đặt trên chính ô — luật phải đi LÊN tới ô
        /// khớp selector rồi mới dừng.
        /// </summary>
        [Test]
        public void InputFieldCell_TooltipOnTheFieldItself_IsExempt()
        {
            VisualElement field = new VisualElement { name = UxTruncationExemption.EventTypesTypeIdCellName, tooltip = FullText };
            VisualElement input = new VisualElement { name = "unity-text-input" };
            Label text = new Label(FullText);
            input.Add(text);
            field.Add(input);
            Assert.IsTrue(UxTruncationExemption.Allows(Screen, UxLayoutFindingKinds.TextCut, text, out _));
        }

        /// <summary>
        /// Tooltip của TỔ TIÊN ngoài ô (cả hàng, cả bảng) không được tính: nó nói chuyện của hàng, không phải bản đầy đủ của
        /// một ô, và tính nó là tha luôn mọi ô của hàng đó.
        /// </summary>
        [Test]
        public void TooltipAboveTheCell_IsNotCounted()
        {
            VisualElement row = new VisualElement { tooltip = FullText };
            Label cell = Cell(UxTruncationExemption.EventTypesTypeIdCellName, FullText, string.Empty);
            row.Add(cell);
            Assert.IsFalse(UxTruncationExemption.Allows(Screen, UxLayoutFindingKinds.TextCut, cell, out _));
        }

        // ============================================================================================ kỷ luật bảng

        /// <summary>
        /// MỖI mục miễn trừ phải có ca chứng minh mang đúng tên đã khai trong <see cref="UxTruncationExemptionTests"/>. Đây
        /// là câu chặn "khai miễn trừ suông": thêm một dòng vào bảng mà không viết ca đo hai điều kiện là ĐỎ ngay.
        /// </summary>
        [Test]
        public void Catalog_EveryEntry_HasProvingTest()
        {
            foreach (UxTruncationExemptionEntry entry in UxTruncationExemption.All)
            {
                MethodInfo method = typeof(UxTruncationExemptionTests).GetMethod(entry.TestName,
                    BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(method, "mục miễn trừ '" + entry.Selector + "' khai ca chứng minh '" + entry.TestName
                    + "' mà UxTruncationExemptionTests không có phương thức ấy");
                Assert.IsNotEmpty(method.GetCustomAttributes(typeof(UnityEngine.TestTools.UnityTestAttribute), false),
                    "ca chứng minh '" + entry.TestName + "' phải là [UnityTest] — hai điều kiện chỉ đo được trên hub thật");
            }
        }

        /// <summary>
        /// Mỗi mục phải khai chỗ đọc đủ, và câu lý do phải đủ dài để nói được VÌ SAO — một dòng "vì thiết kế" không cho người
        /// soát sau biết gì cả.
        /// </summary>
        [Test]
        public void Catalog_EveryEntry_DeclaresSurfaceAndReason()
        {
            Assert.IsNotEmpty(UxTruncationExemption.All, "bảng rỗng thì cả luật này là mã chết — xoá nó đi, đừng để nó đứng đó");
            List<string> seen = new List<string>();
            foreach (UxTruncationExemptionEntry entry in UxTruncationExemption.All)
            {
                string key = entry.ScreenId + "/" + entry.Selector;
                Assert.IsFalse(seen.Contains(key), "hai mục cùng màn cùng selector: '" + key + "'");
                seen.Add(key);
                Assert.Greater(entry.FullTextSurface.Length, 0, "mục '" + key + "' chưa khai chỗ đọc đủ");
                Assert.Greater(entry.Reason.Length, 80, "lý do của mục '" + key + "' quá ngắn để nói được vì sao");
            }
        }

        /// <summary>
        /// Bảng miễn trừ có điều kiện KHÔNG được dùng để dọn bớt <see cref="UxLayoutAllowList"/> theo chiều ngược lại: mục
        /// nào đã tha suông ở danh sách cũ thì không được xuất hiện lại ở đây như một mục "đã có điều kiện" mà không có ca đo.
        /// Câu này khoá cả hai bảng vào một chỗ đếm được.
        /// </summary>
        [Test]
        public void AllowList_AndConditionalCatalog_DoNotOverlap()
        {
            foreach (UxTruncationExemptionEntry conditional in UxTruncationExemption.All)
            {
                foreach (UxLayoutAllowEntry bare in UxLayoutAllowList.All)
                {
                    Assert.AreNotEqual(bare.Selector, conditional.Selector,
                        "selector '" + conditional.Selector + "' có mặt ở CẢ hai bảng — một chỗ tha suông và một chỗ tha có "
                        + "điều kiện thì điều kiện không bao giờ được hỏi tới");
                }
            }
        }

        private static Label Cell(string name, string text, string tooltip)
        {
            Label cell = new Label(text) { name = name, tooltip = tooltip };
            cell.AddToClassList(LiveOpsHubClassNames.EventTypesCell);
            return cell;
        }
    }
}
