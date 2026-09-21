using System;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class LiveEventUtcTextTests
    {
        [Test]
        public void TryParse_CanonicalFormat_Reads()
        {
            Assert.IsTrue(LiveEventUtcText.TryParse("2026-09-14T00:00:00Z", out DateTime utc));
            Assert.AreEqual(new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc), utc);
            Assert.AreEqual(DateTimeKind.Utc, utc.Kind);
        }

        [Test]
        public void TryParse_UnreadableText_ReturnsFalse()
        {
            Assert.IsFalse(LiveEventUtcText.TryParse("2026-10-3", out _));
            Assert.IsFalse(LiveEventUtcText.TryParse("", out _));
            Assert.IsFalse(LiveEventUtcText.TryParse(null, out _));
        }

        [Test]
        public void Format_AlwaysCanonical()
        {
            var utc = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Unspecified);
            Assert.AreEqual("2026-09-14T00:00:00Z", LiveEventUtcText.Format(utc));
        }

        [Test]
        public void TryNormalize_MissingLeadingZero_FillsInSameInstant()
        {
            Assert.IsTrue(LiveEventUtcText.TryNormalize("2026-10-3", out string canonicalText));
            Assert.AreEqual("2026-10-03T00:00:00Z", canonicalText);
        }

        [Test]
        public void TryNormalize_DateOnly_AssumesMidnight()
        {
            Assert.IsTrue(LiveEventUtcText.TryNormalize("2026-09-17", out string canonicalText));
            Assert.AreEqual("2026-09-17T00:00:00Z", canonicalText);
        }

        [Test]
        public void TryNormalize_AmbiguousText_ReturnsFalse()
        {
            Assert.IsFalse(LiveEventUtcText.TryNormalize("3/10/2026", out _));
        }

        [Test]
        public void TryNormalize_AlreadyCanonical_ReturnsSameInstant()
        {
            Assert.IsTrue(LiveEventUtcText.TryNormalize("2026-09-14T00:00:00Z", out string canonicalText));
            Assert.AreEqual("2026-09-14T00:00:00Z", canonicalText);
        }

        [Test]
        public void TryParseDateAndTime_CombinesBothFields()
        {
            Assert.IsTrue(LiveEventUtcText.TryParseDateAndTime("2026-09-14", "08:47", out DateTime utc));
            Assert.AreEqual(new DateTime(2026, 9, 14, 8, 47, 0, DateTimeKind.Utc), utc);
        }

        [Test]
        public void TryParseDateAndTime_MissingTime_AssumesMidnight()
        {
            Assert.IsTrue(LiveEventUtcText.TryParseDateAndTime("2026-09-14", "", out DateTime utc));
            Assert.AreEqual(new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc), utc);
        }

        [Test]
        public void TryParseDateAndTime_InvalidDate_ReturnsFalse()
        {
            Assert.IsFalse(LiveEventUtcText.TryParseDateAndTime("not-a-date", "08:47", out _));
        }

        [Test]
        public void TryParse_AcceptsEveryParser010Format()
        {
            // Đủ 6 định dạng của JsonLiveEventCalendarParser 0.1.0; không ghi múi = UTC, có múi thì đổi về UTC.
            var expected = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
            string[] readableTexts =
            {
                "2026-09-15T00:00:00Z",          // yyyy-MM-dd'T'HH:mm:ssK
                "2026-09-15T07:00:00+07:00",     // yyyy-MM-dd'T'HH:mm:ssK có múi
                "2026-09-15T00:00:00.0000000Z",  // yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK
                "2026-09-15T00:00Z",             // yyyy-MM-dd'T'HH:mmK
                "2026-09-15T00:00:00",           // yyyy-MM-dd'T'HH:mm:ss
                "2026-09-15T00:00:00.0",         // yyyy-MM-dd'T'HH:mm:ss.FFFFFFF
                "2026-09-15T00:00",              // yyyy-MM-dd'T'HH:mm
                "  2026-09-15T00:00:00Z  ",      // 0.1.0 Trim trước khi đọc
            };
            foreach (string text in readableTexts)
            {
                Assert.IsTrue(LiveEventUtcText.TryParse(text, out DateTime utc), text);
                Assert.AreEqual(expected, utc, text);
                Assert.AreEqual(DateTimeKind.Utc, utc.Kind, text);
            }
        }

        [Test]
        public void TryParse_RejectsDateOnlyAndSpaceSeparator()
        {
            Assert.IsFalse(LiveEventUtcText.TryParse("2026-09-15", out _), "Chỉ có ngày: 0.1.0 không đọc.");
            Assert.IsFalse(LiveEventUtcText.TryParse("2026-09-15 00:00:00", out _), "Dấu cách thay 'T': 0.1.0 không đọc.");
            Assert.IsFalse(LiveEventUtcText.TryParse("15/09/2026", out _));
        }

        [Test]
        public void TryNormalize_ConvertsOffsetToUtc()
        {
            Assert.IsTrue(LiveEventUtcText.TryNormalize("2026-09-15T07:00:00+07:00", out string canonicalText));
            Assert.AreEqual("2026-09-15T00:00:00Z", canonicalText, "Cùng thời điểm, viết lại bằng Z.");

            Assert.IsTrue(LiveEventUtcText.TryNormalize("2026-09-14T20:30-03:30", out string westernOffsetText));
            Assert.AreEqual("2026-09-15T00:00:00Z", westernOffsetText);
        }

        [TestCase("2026-10-3T07:00+07:00", "2026-10-03T00:00:00Z")]      // F7: thiếu số 0 + múi dương
        [TestCase("2026-10-3T07:00:30+07:00", "2026-10-03T00:00:30Z")]
        [TestCase("2026-9-30T20:30-03:30", "2026-10-01T00:00:00Z")]       // múi âm, qua ngày + qua tháng
        [TestCase("2026-1-1T05:00+07:00", "2025-12-31T22:00:00Z")]        // múi dương lùi về năm trước
        [TestCase("2026-10-3T07:00-00:00", "2026-10-03T07:00:00Z")]
        [TestCase("2026-10-3T23:59+14:00", "2026-10-03T09:59:00Z")]        // múi xa nhất DateTimeOffset nhận
        [TestCase("2026-10-3T07:00Z", "2026-10-03T07:00:00Z")]
        [TestCase("2026-10-3T07:00z", "2026-10-03T07:00:00Z")]
        [TestCase("  2026-10-3T07:00+07:00  ", "2026-10-03T00:00:00Z")]
        public void TryNormalize_MissingLeadingZeroWithZone_PadsAndConvertsToUtc(string text, string expectedCanonicalText)
        {
            Assert.IsTrue(LiveEventUtcText.TryNormalize(text, out string canonicalText), text);
            Assert.AreEqual(expectedCanonicalText, canonicalText, text);
            Assert.IsTrue(LiveEventUtcText.TryParse(canonicalText, out _), "Kết quả chuẩn hoá phải đọc lại được.");
        }

        [TestCase("2026-10-3T07:00+15:00")]   // quá ±14:00 — DateTimeOffset không có múi này
        [TestCase("2026-10-3T07:00+07")]
        [TestCase("2026-10-3T+07:00")]        // có múi mà không có giờ
        [TestCase("2026-10-3T07:00+07:00Z")]
        [TestCase("0001-1-1T00:00+01:00")]    // trừ múi ra ngoài khoảng DateTime: false, không ném
        [TestCase("3/10/2026T07:00+07:00")]
        public void TryNormalize_BadZone_ReturnsFalseNeverThrows(string text)
        {
            bool normalized = true;
            Assert.DoesNotThrow(() => normalized = LiveEventUtcText.TryNormalize(text, out _), text);
            Assert.IsFalse(normalized, text);
        }

        [Test]
        public void TryNormalize_EveryParser010Format_MatchesTryParse()
        {
            // Chuỗi parser 0.1.0 đọc được thì chuẩn hoá = Format(TryParse(...)) — không đổi thời điểm game đang thấy.
            string[] readableTexts =
            {
                "2026-09-15T00:00:00Z", "2026-09-15T07:00:00+07:00", "2026-09-14T20:30:00-03:30", "2026-09-15T00:00:00.0000000Z",
                "2026-09-15T00:00Z", "2026-09-15T00:00:00", "2026-09-15T00:00:00.0", "2026-09-15T00:00",
            };
            foreach (string text in readableTexts)
            {
                Assert.IsTrue(LiveEventUtcText.TryParse(text, out DateTime utc), text);
                Assert.IsTrue(LiveEventUtcText.TryNormalize(text, out string canonicalText), text);
                Assert.AreEqual(LiveEventUtcText.Format(utc), canonicalText, text);
            }
        }

        [Test]
        public void TryParseDateAndTime_TreatsInputAsUtc()
        {
            Assert.IsTrue(LiveEventUtcText.TryParseDateAndTime("2026-09-13", "08:47", out DateTime utc));
            Assert.AreEqual(DateTimeKind.Utc, utc.Kind);
            Assert.AreEqual("2026-09-13T08:47:00Z", LiveEventUtcText.Format(utc), "Ô giờ là giờ UTC, không cộng/trừ giờ máy.");
        }
    }
}
