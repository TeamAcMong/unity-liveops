using System;
using System.Globalization;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class LiveEventCalendarSha256Tests
    {
        private static readonly UTF8Encoding Utf8WithoutByteOrderMark = new UTF8Encoding(false);

        // ----- Vector chuẩn (NIST FIPS 180-2) — SP-10 đã đo khớp ở cả 2022.3 và 6000.6 -----

        [Test]
        public void ComputeHex_EmptyInput_MatchesNistVector()
        {
            Assert.AreEqual("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                LiveEventCalendarSha256.ComputeHex(Utf8WithoutByteOrderMark.GetBytes(string.Empty)));
        }

        [Test]
        public void ComputeHex_Abc_MatchesNistVector()
        {
            Assert.AreEqual("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                LiveEventCalendarSha256.ComputeHex(Utf8WithoutByteOrderMark.GetBytes("abc")));
        }

        [Test]
        public void ComputeHex_VietnameseUtf8_MatchesPythonHashlib()
        {
            // Escape \u để nguồn test không phụ thuộc chuẩn hoá Unicode của trình soạn (NFC/NFD cho ra byte khác). Giá trị
            // chốt bằng Python: hashlib.sha256("Nhiệm vụ dung nham — mở lava-quest tháng 9".encode("utf-8")) — 51 byte.
            const string text = "Nhi\u1EC7m v\u1EE5 dung nham \u2014 m\u1EDF lava-quest th\u00E1ng 9";
            byte[] bytes = Utf8WithoutByteOrderMark.GetBytes(text);

            Assert.AreEqual(51, bytes.Length);
            Assert.AreEqual("4cc04d958e29c742e2bad24b60dbbfa8a9a939ef866f032c15c07ff795e4c1a8", LiveEventCalendarSha256.ComputeHex(bytes));
        }

        // ----- Dữ liệu mẫu -----

        [Test]
        public void ComputeHex_DesignSampleFormat2_MatchesPinnedValue()
        {
            // Chốt bằng Python hashlib trên plan/sample_format2.json bỏ LF cuối (1.601 byte) — mục 5.3 "mẫu = da4f831d840a…".
            byte[] bytes = Utf8WithoutByteOrderMark.GetBytes(LiveOpsDesignSample.ExpectedFormat2Json);

            Assert.AreEqual(1601, bytes.Length);
            Assert.AreEqual("da4f831d840a21b7b4c08f9d7ffdb515f4ba89cdea7fae2ca8cde03e8d6c7646", LiveEventCalendarSha256.ComputeHex(bytes));
        }

        [Test]
        public void ComputeHex_PublishedSnapshot_MatchesStampSha()
        {
            byte[] bytes = Utf8WithoutByteOrderMark.GetBytes(LiveOpsDesignSample.PublishedSnapshotJson);

            Assert.AreEqual(LiveOpsDesignSample.PublishedByteCount, bytes.Length);
            Assert.AreEqual(LiveOpsDesignSample.PublishedSha256Hex, LiveEventCalendarSha256.ComputeHex(bytes));
        }

        // ----- Hình dạng kết quả -----

        [Test]
        public void ComputeHex_ReturnsLowercase64Characters()
        {
            string hex = LiveEventCalendarSha256.ComputeHex(new byte[] { 0xFF, 0x00, 0x10 });

            Assert.AreEqual(64, hex.Length);
            foreach (char character in hex)
            {
                Assert.IsTrue((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'),
                    "Chỉ hex thường: '" + character + "'.");
            }
        }

        [Test]
        public void ComputeHex_IndependentOfThreadCulture()
        {
            byte[] bytes = Utf8WithoutByteOrderMark.GetBytes("abc");
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                // tr-TR đổi quy tắc hoa/thường của "I" — hex phải không đổi.
                foreach (string cultureName in new[] { "tr-TR", "fr-FR", "vi-VN" })
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureName);
                    Assert.AreEqual("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                        LiveEventCalendarSha256.ComputeHex(bytes), cultureName);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }

        [Test]
        public void ComputeHex_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => LiveEventCalendarSha256.ComputeHex(null));
        }
    }
}
