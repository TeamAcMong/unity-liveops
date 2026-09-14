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
    }
}
