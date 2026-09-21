using System.Globalization;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    /// <summary>
    /// Gác GIỚI HẠN ĐỘ DÀI của W11 ở hai vế.
    /// <para>
    /// Vế một: con số suy ra phải còn suy ra được. <see cref="LiveOpsIdentifierLimits.MaxRecurringIdPrefixLength"/> và
    /// <see cref="LiveOpsIdentifierLimits.MaxGrantIdLength"/> đều là phép tính trên hằng khác — đổi một hằng nguồn mà quên
    /// vế kia thì phép tính vẫn biên dịch được và vẫn ra MỘT con số, chỉ là con số sai. Những ca ở đây đo lại phép tính ấy
    /// bằng dữ liệu thật (bề rộng chữ của <c>long.MinValue</c>, độ dài tiền tố grant id của <c>LiveOpsSystem</c>).
    /// </para>
    /// <para>
    /// Vế hai: bộ dữ liệu "xấu nhất" của cổng bố cục phải BẰNG ĐÚNG giới hạn, không hơn không kém. Ngắn hơn thì cổng đang
    /// đo một ngày tồi tệ giả (bố cục xanh mà dữ liệu thật vẫn làm vỡ); dài hơn thì cổng đang tự bịa nợ cho mình. Ca ở đây
    /// là chỗ DUY NHẤT nối hai file ấy với nhau, nên sửa một chuỗi của fixture mà quên giới hạn là đỏ ngay.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class LiveOpsIdentifierLimitsTests
    {
        [Test]
        public void MaxOccurrenceIndexLength_MatchesWidestLongValue()
        {
            // long.MinValue là giá trị viết ra dài nhất của kiểu (dấu trừ + 19 chữ số) — lịch lặp ghép đúng chuỗi này vào
            // sau tiền tố, nên đây là số đo, không phải ước lượng.
            Assert.AreEqual(LiveOpsIdentifierLimits.MaxOccurrenceIndexLength,
                long.MinValue.ToString(CultureInfo.InvariantCulture).Length,
                "bề rộng chữ của long.MinValue đổi thì MaxOccurrenceIndexLength phải đổi theo, nếu không tiền tố luật lặp "
                + "dài tối đa vẫn sinh ra id đợt vượt giới hạn");
        }

        [Test]
        public void MaxRecurringIdPrefixLength_LeavesRoomForOccurrenceIndex()
        {
            Assert.AreEqual(LiveOpsIdentifierLimits.MaxIdentifierLength,
                LiveOpsIdentifierLimits.MaxRecurringIdPrefixLength + LiveOpsIdentifierLimits.MaxOccurrenceIndexLength,
                "tiền tố cộng số thứ tự phải vừa đúng một định danh — dư ra là giới hạn tiền tố đang nới lỏng hơn ý định");
        }

        [Test]
        public void MaxGrantIdLength_MatchesRealClaimGrantIdComposition()
        {
            string longestEventId = new string('a', LiveOpsIdentifierLimits.MaxIdentifierLength);
            string longestClaimKey = new string('b', LiveOpsIdentifierLimits.MaxIdentifierLength);
            string grantId = LiveOpsSystem.ClaimGrantPrefix + longestEventId + LiveEventInstance.ReservedSeparator
                + longestClaimKey;

            Assert.AreEqual(LiveOpsIdentifierLimits.MaxGrantIdLength, grantId.Length,
                "MaxGrantIdLength phải là độ dài THẬT của grant id dài nhất — đổi tiền tố 'liveops.claim#' mà quên con số "
                + "này là game tính sai ngân sách khoá lưu");
        }

        [Test]
        public void WorstCaseSample_LongIdentifiers_SitExactlyAtIdentifierLimit()
        {
            AssertExactLength(LiveOpsWorstCaseSample.LongTypeId, LiveOpsIdentifierLimits.MaxIdentifierLength,
                nameof(LiveOpsWorstCaseSample.LongTypeId));
            AssertExactLength(LiveOpsWorstCaseSample.LongTypeConfigKey, LiveOpsIdentifierLimits.MaxIdentifierLength,
                nameof(LiveOpsWorstCaseSample.LongTypeConfigKey));
            AssertExactLength(LiveOpsWorstCaseSample.LongEventId, LiveOpsIdentifierLimits.MaxIdentifierLength,
                nameof(LiveOpsWorstCaseSample.LongEventId));
            AssertExactLength(LiveOpsWorstCaseSample.LongEntryKey, LiveOpsIdentifierLimits.MaxIdentifierLength,
                nameof(LiveOpsWorstCaseSample.LongEntryKey));
            AssertExactLength(LiveOpsWorstCaseSample.UndeclaredTypeId, LiveOpsIdentifierLimits.MaxIdentifierLength,
                nameof(LiveOpsWorstCaseSample.UndeclaredTypeId));
            AssertExactLength(LiveOpsWorstCaseSample.UndeclaredEventId, LiveOpsIdentifierLimits.MaxIdentifierLength,
                nameof(LiveOpsWorstCaseSample.UndeclaredEventId));
            AssertExactLength(LiveOpsWorstCaseSample.UndeclaredEntryKey, LiveOpsIdentifierLimits.MaxIdentifierLength,
                nameof(LiveOpsWorstCaseSample.UndeclaredEntryKey));
        }

        [Test]
        public void WorstCaseSample_RecurringIdPrefix_SitsExactlyAtPrefixLimit()
        {
            AssertExactLength(LiveOpsWorstCaseSample.LongIdPrefix, LiveOpsIdentifierLimits.MaxRecurringIdPrefixLength,
                nameof(LiveOpsWorstCaseSample.LongIdPrefix));
        }

        [Test]
        public void WorstCaseSample_DisplayNamesAndNote_SitExactlyAtTheirLimits()
        {
            AssertExactLength(LiveOpsWorstCaseSample.LongTypeDisplayName, LiveOpsIdentifierLimits.MaxDisplayNameLength,
                nameof(LiveOpsWorstCaseSample.LongTypeDisplayName));
            AssertExactLength(LiveOpsWorstCaseSample.LongPublisher, LiveOpsIdentifierLimits.MaxDisplayNameLength,
                nameof(LiveOpsWorstCaseSample.LongPublisher));
            AssertExactLength(LiveOpsWorstCaseSample.LongPublishedNote, LiveOpsIdentifierLimits.MaxNoteLength,
                nameof(LiveOpsWorstCaseSample.LongPublishedNote));
        }

        [Test]
        public void WorstCaseSample_GeneratedOccurrenceIds_StayWithinIdentifierLimit()
        {
            // Id một lần lặp = tiền tố + số thứ tự. Kiểm bằng số thứ tự RỘNG NHẤT chứ không bằng số thứ tự mà mẫu đang
            // sinh ra (0, 1): mẫu đổi mốc neo là số thứ tự đổi, mà giới hạn thì không được phụ thuộc mốc neo.
            string widestOccurrenceId = LiveOpsWorstCaseSample.LongIdPrefix
                + long.MinValue.ToString(CultureInfo.InvariantCulture);

            Assert.LessOrEqual(widestOccurrenceId.Length, LiveOpsIdentifierLimits.MaxIdentifierLength,
                "id lần lặp dài nhất mà mẫu sinh được đã vượt giới hạn định danh — tiền tố của mẫu đang dài quá phần "
                + "MaxRecurringIdPrefixLength cho phép");
        }

        private static void AssertExactLength(string value, int expectedLength, string constantName)
        {
            Assert.AreEqual(expectedLength, value.Length,
                "chuỗi '" + constantName + "' của bộ dữ liệu xấu nhất phải dài ĐÚNG bằng giới hạn ("
                + expectedLength.ToString(CultureInfo.InvariantCulture) + " ký tự). Ngắn hơn thì cổng bố cục đang đo một "
                + "ngày tồi tệ giả; dài hơn thì cổng đang tự bịa nợ.");
        }
    }
}
