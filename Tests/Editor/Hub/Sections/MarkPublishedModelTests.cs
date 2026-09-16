using System;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Ba biến thể của hộp Đánh dấu đã đăng [SD2 §3.11]. Trọng tâm là luật "chỉ ghi dấu thứ đã thật sự copy": sha nháp phải
    /// khớp <c>lastExportedSha</c>, và khi nháp đổi sau lần copy thì phần xác nhận KHOÁ — tick lúc đó là khai sai thứ đang
    /// nằm trên Firebase.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class MarkPublishedModelTests
    {
        private const string DraftSha = "5b0d93aa11223344556677889900aabbccddeeff00112233445566778899aabb";
        private const string OtherSha = "91aa02ff11223344556677889900aabbccddeeff00112233445566778899aabb";
        private const string DraftShortSha = "5b0d93";
        private const string OtherShortSha = "91aa02";
        private const string Note = "mở hunt-0916-bonus cho sự kiện giữa tháng";
        private const int ByteCount = 1612;
        private const string AssetFileName = "Main.asset";

        private static readonly DateTime CopiedUtc = new DateTime(2026, 9, 13, 9, 2, 0, DateTimeKind.Utc);
        private static readonly DateTime NowUtc = new DateTime(2026, 9, 13, 9, 4, 0, DateTimeKind.Utc);

        [Test]
        public void Ready_ShaMatchesTickedWithNote_CanMark()
        {
            MarkPublishedState state = MarkPublishedModel.Evaluate(Input(DraftSha).WithUserInput(true, Note));

            Assert.AreEqual(MarkPublishedVariant.Ready, state.Variant);
            Assert.IsTrue(state.CanMark, "sha khớp + đã tick + có ghi chú thì nút Ghi dấu bật");
            Assert.AreEqual(string.Empty, state.MissingText, "đủ điều kiện thì không còn chữ 'Còn thiếu'");
            Assert.IsTrue(state.HeadingText.Contains(DraftShortSha), "heading nêu sha sắp ghi");
            Assert.IsTrue(state.BodyText.Contains(AssetFileName), "thân nói rõ ghi vào file nào");
            Assert.AreEqual(HealthState.Ok, state.ShaState);
            Assert.IsFalse(state.HasCopyButton, "biến thể đủ điều kiện không mời copy lại");
        }

        [Test]
        public void MissingConfirmAndNote_ListsBothInOrder()
        {
            MarkPublishedState state = MarkPublishedModel.Evaluate(Input(DraftSha).WithUserInput(false, string.Empty));

            Assert.IsFalse(state.CanMark);
            Assert.AreEqual(LiveOpsHubStrings.ExportMarkMissingPrefix + LiveOpsHubStrings.ExportMarkMissingConfirm
                + LiveOpsHubStrings.ExportMarkMissingSeparator + LiveOpsHubStrings.ExportMarkMissingNote, state.MissingText,
                "liệt kê đúng phần còn thiếu, theo thứ tự đọc của hộp");
            Assert.IsTrue(state.ConfirmSectionEnabled, "sha vẫn khớp nên toggle và ô ghi chú mở");
        }

        [Test]
        public void WhitespaceNote_CountsAsMissing()
        {
            MarkPublishedState state = MarkPublishedModel.Evaluate(Input(DraftSha).WithUserInput(true, "   "));

            Assert.IsFalse(state.CanMark, "ghi chú toàn khoảng trắng không phải ghi chú");
            Assert.IsTrue(state.MissingText.Contains(LiveOpsHubStrings.ExportMarkMissingNote));
        }

        [Test]
        public void DraftChangedAfterCopy_LocksConfirmAndOffersCopy()
        {
            MarkPublishedState state = MarkPublishedModel.Evaluate(Input(OtherSha).WithUserInput(true, Note));

            Assert.AreEqual(MarkPublishedVariant.DraftChanged, state.Variant);
            Assert.IsFalse(state.CanMark, "sha lệch thì dù đã tick và có ghi chú vẫn không ghi dấu");
            Assert.IsFalse(state.ConfirmSectionEnabled, "phần xác nhận khoá tới khi copy bản mới");
            Assert.AreEqual(HealthState.Blocked, state.ShaState);
            Assert.IsTrue(state.HasCopyButton, "hộp mời copy đúng bản nháp hiện tại");
            Assert.IsTrue(state.CopyButtonText.Contains(DraftShortSha), "nút copy nêu sha NHÁP, không phải sha đã copy");
            Assert.IsTrue(state.ShaText.Contains(OtherShortSha) && state.ShaText.Contains(DraftShortSha), "dòng sha nêu cả hai bản");
            Assert.IsTrue(state.MissingText.Contains(LiveOpsHubStrings.ExportMarkMissingCopy));
        }

        [Test]
        public void CopiedInsideDialog_AsksToPasteAgainBeforeTicking()
        {
            MarkPublishedInput input = Input(OtherSha).WithUserInput(false, string.Empty)
                .WithCopiedInsideDialog(DraftSha, new DateTime(2026, 9, 13, 9, 6, 0, DateTimeKind.Utc));
            MarkPublishedState state = MarkPublishedModel.Evaluate(input);

            Assert.AreEqual(MarkPublishedVariant.Ready, state.Variant, "copy xong thì sha khớp lại");
            Assert.IsTrue(state.ConfirmSectionEnabled, "phần xác nhận mở khoá sau khi copy");
            Assert.AreEqual(HealthState.Ok, state.ShaState);
            Assert.IsTrue(state.ShaText.Contains(DraftShortSha), "câu nhắc dán lại nêu sha vừa copy");
        }

        [Test]
        public void NeverExported_SaysSoInsteadOfPretendingShaMismatch()
        {
            MarkPublishedState state = MarkPublishedModel.Evaluate(Input(string.Empty).WithUserInput(true, Note));

            Assert.AreEqual(MarkPublishedVariant.NeverExported, state.Variant);
            Assert.IsFalse(state.CanMark);
            Assert.AreEqual(LiveOpsHubStrings.ExportGateMarkTooltipNotExported, state.ShaText,
                "chưa copy lần nào thì nói thẳng, không in '→' giữa hai sha");
            Assert.IsFalse(state.HasCopyButton, "nút copy của hộp chỉ dành cho ca nháp đã đổi sau lần copy");
        }

        [Test]
        public void Evaluate_NullInput_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => MarkPublishedModel.Evaluate(null));
        }

        private static MarkPublishedInput Input(string lastExportedSha)
        {
            return new MarkPublishedInput(DraftSha, lastExportedSha, lastExportedSha.Length > 0 ? CopiedUtc : (DateTime?)null, NowUtc,
                ByteCount, AssetFileName, LiveOpsHubTestServices.PublisherName, LiveOpsHubTestServices.PublisherSource,
                new LiveOpsHubFormat(LiveOpsDesignSample.DeviceOffset));
        }
    }
}
