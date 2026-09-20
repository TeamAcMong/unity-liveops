using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Tự kiểm CỔNG (không kiểm sản phẩm): luật "mỗi màn của ma trận phải chạm tới dữ liệu XẤU NHẤT" phải là một câu ASSERT,
    /// không phải một khối chú thích.
    /// <para>
    /// Vì sao (soát W10 R-04): ba lỗi W9-29/30/31 lọt lưới vì 24 màn của bản W9 đều đứng trên tài liệu ĐẸP. Đợt W10 dựng 13
    /// màn dữ liệu xấu và viết luật ấy vào chú thích — nhưng chú thích không chặn được màn thứ 39 khai ra mà không có trạng
    /// thái xấu nào, tức lỗ hổng vừa vá xong đã có đường mở lại. Bốn ca dưới đây khoá đúng bốn đường: quên khai màn, khai
    /// section sai, lời khai trôi khỏi mã, và tập màn "chưa có" phình âm thầm.
    /// </para>
    /// <para>
    /// Không mở cửa sổ nào nên KHÔNG mang <c>[Category(UI)]</c>: bảng kê là dữ liệu tĩnh, chạy được cả dưới
    /// <c>-nographics</c> — và một nhãn Logic đặt trên fixture mang nhãn UI là nhãn CHẾT (soát W10 R-07).
    /// </para>
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class UxLayoutScreenCatalogTests
    {
        /// <summary>Độ dài tối thiểu của câu ghi chú — ngắn hơn là một lời khai không đọc ra được việc gì.</summary>
        private const int MinimumNoteLength = 40;

        /// <summary>Đầu câu bắt buộc của ghi chú màn còn nợ phiếu — ép lời khai nói THẲNG là còn thiếu, không mô tả lòng vòng.</summary>
        private const string MissingStatePrefix = "còn thiếu:";

        /// <summary>
        /// Sáu section của hub. Liệt kê tại chỗ vì <c>LiveOpsHubSections.Ids</c> không khai danh sách, và thêm một danh sách
        /// vào đó là sửa <c>Editor/Hub/**</c> — nằm ngoài quyền ghi của gói này. Thêm section thứ bảy mà quên dòng ở đây thì
        /// ca "mỗi section phải có màn dữ liệu xấu nhất" im lặng bỏ qua section mới; đó là giá của lời khai tay, ghi thẳng
        /// ra để người sau biết phải sửa hai chỗ.
        /// </summary>
        private static readonly string[] SectionIds =
        {
            LiveOpsHubSections.Ids.Overview,
            LiveOpsHubSections.Ids.EventTypes,
            LiveOpsHubSections.Ids.Calendar,
            LiveOpsHubSections.Ids.RecurringRules,
            LiveOpsHubSections.Ids.Validation,
            LiveOpsHubSections.Ids.Export,
        };

        [Test]
        public void Catalog_EveryEntry_HasUniqueIdSectionAndNote()
        {
            HashSet<string> seenIds = new HashSet<string>();
            HashSet<string> sectionIds = new HashSet<string>(SectionIds);
            foreach (UxLayoutScreenRegistration entry in UxLayoutScreenCatalog.All)
            {
                Assert.IsNotEmpty(entry.Id, "bảng kê có dòng không tên màn");
                Assert.IsTrue(seenIds.Add(entry.Id),
                    "màn '" + entry.Id + "' bị khai hai lần trong bảng kê — dòng thứ hai không bao giờ được tra tới");
                Assert.IsTrue(sectionIds.Contains(entry.SectionId),
                    "màn '" + entry.Id + "' khai section '" + entry.SectionId + "' không có thật");
                Assert.Greater(entry.Note.Length, MinimumNoteLength,
                    "màn '" + entry.Id + "' có ghi chú quá ngắn — phải nói RÕ nó nhìn thấy trạng thái xấu nào, hoặc còn "
                    + "thiếu trạng thái xấu nào");
            }
        }

        /// <summary>
        /// Mỗi section phải có ít nhất MỘT màn tự dựng services trên dữ liệu xấu nhất. Đây là câu ngắn nhất nói được "section
        /// này đã từng bị bày ra ở ngày tồi tệ nhất của nó" — thiếu nó thì cả một section quay về đúng trạng thái đã để ba lỗi
        /// W9-29/30/31 lọt.
        /// </summary>
        [Test]
        public void Catalog_EverySection_HasAtLeastOneWorstCaseScreen()
        {
            foreach (string sectionId in SectionIds)
            {
                bool found = false;
                foreach (UxLayoutScreenRegistration entry in UxLayoutScreenCatalog.All)
                {
                    if (entry.Coverage != UxWorstCaseCoverage.Services) continue;
                    if (entry.SectionId != sectionId) continue;
                    found = true;
                    break;
                }

                Assert.IsTrue(found,
                    "section '" + sectionId + "' không có màn nào của ma trận BỐ CỤC khai WithServices trên dữ liệu xấu "
                    + "nhất — section ấy mới chỉ được đo ở trạng thái đẹp, đúng chỗ ba lỗi W9-29/30/31 đã lọt");
            }
        }

        /// <summary>
        /// Màn khai "được màn khác phủ hộ" phải trỏ tới một màn CÓ THẬT và màn ấy phải thật sự đứng trên dữ liệu xấu. Không có
        /// câu này thì "được phủ hộ" là cách viết khác của "chưa đo" mà không ai đếm được.
        /// </summary>
        [Test]
        public void Catalog_CoveredEntries_PointAtARealWorstCaseScreen()
        {
            foreach (UxLayoutScreenRegistration entry in UxLayoutScreenCatalog.All)
            {
                if (entry.Coverage != UxWorstCaseCoverage.CoveredBy) continue;
                UxLayoutScreenRegistration covering = UxLayoutScreenCatalog.Find(entry.Reference);
                Assert.IsNotNull(covering,
                    "màn '" + entry.Id + "' khai được màn '" + entry.Reference + "' phủ hộ, nhưng màn ấy không có trong "
                    + "bảng kê — lời khai trỏ vào hư không");
                Assert.IsTrue(covering.Coverage == UxWorstCaseCoverage.Services
                    || covering.Coverage == UxWorstCaseCoverage.Selection,
                    "màn '" + entry.Id + "' khai được '" + entry.Reference + "' phủ hộ, nhưng màn ấy cũng KHÔNG đứng trên "
                    + "dữ liệu xấu — hai màn cùng đứng trên mẫu đẹp thì không màn nào phủ màn nào");
                Assert.AreEqual(entry.SectionId, covering.SectionId,
                    "màn '" + entry.Id + "' khai được '" + entry.Reference + "' phủ hộ nhưng hai màn ở hai section khác "
                    + "nhau — chúng không vẽ cùng một cây, nên không nói hộ nhau được");
            }
        }

        /// <summary>
        /// Tập màn CHƯA có biến thể xấu nhất phải khớp ĐÚNG phần đợt W10 đã đếm, và mỗi màn phải mang mã phiếu.
        /// <para>
        /// Khoá cả tập chứ không chỉ kiểm từng mục: kiểm từng mục cho phép dòng thứ mười ba xuất hiện kèm một ghi chú đủ dài
        /// và không ai thấy ma trận vừa hở thêm một chỗ. Muốn thêm một màn vào đây thì phải sửa
        /// <see cref="UxLayoutScreenCatalog.ScreensWithoutWorstCaseVariant"/>, và chỗ sửa đó buộc người sửa đọc phiếu.
        /// </para>
        /// </summary>
        [Test]
        public void Catalog_ScreensWithoutWorstCaseVariant_MatchTheCountedSet()
        {
            List<string> actual = new List<string>();
            foreach (UxLayoutScreenRegistration entry in UxLayoutScreenCatalog.All)
            {
                if (entry.Coverage != UxWorstCaseCoverage.Ticket) continue;
                StringAssert.IsMatch("^W10-[0-9][0-9]$", entry.Reference,
                    "màn '" + entry.Id + "' chưa có biến thể xấu nhất nhưng không mang mã phiếu dạng W10-NN nên không tra "
                    + "lại được");
                Assert.IsTrue(entry.Note.StartsWith(MissingStatePrefix, StringComparison.Ordinal),
                    "ghi chú của màn '" + entry.Id + "' phải bắt đầu bằng 'còn thiếu:' và nói THẲNG trạng thái xấu nào "
                    + "chưa được dựng — một câu chung chung thì phiếu không làm được");
                actual.Add(entry.Id);
            }

            CollectionAssert.AreEquivalent(UxLayoutScreenCatalog.ScreensWithoutWorstCaseVariant, actual,
                "tập màn CHƯA có biến thể dữ liệu xấu nhất lệch phần đợt W10 đã đếm. Thêm một màn vào tập là nới lỗ hổng "
                + "ma trận — thứ vừa tốn cả một đợt để vá; bớt một màn thì xoá luôn dòng ở ScreensWithoutWorstCaseVariant");
        }
    }
}
