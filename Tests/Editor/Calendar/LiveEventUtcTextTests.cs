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

        [Test]
        public void TryParseDateAndTime_TreatsInputAsUtc()
        {
            Assert.IsTrue(LiveEventUtcText.TryParseDateAndTime("2026-09-13", "08:47", out DateTime utc));
            Assert.AreEqual(DateTimeKind.Utc, utc.Kind);
            Assert.AreEqual("2026-09-13T08:47:00Z", LiveEventUtcText.Format(utc), "Ô giờ là giờ UTC, không cộng/trừ giờ máy.");
        }
    }
}
