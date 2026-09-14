using System;
using System.Security.Cryptography;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// SHA-256 của đúng mảng byte xuất ra (PD-5). Chỉ bộ ghi JSON và Editor gọi; đường game (<c>Parse</c>,
    /// <c>ParseOrDefault</c>, <c>ToParseResult</c>) KHÔNG gọi, nên IL2CPP có strip <c>System.Security.Cryptography</c> khỏi
    /// bản build game cũng không làm game ném khi đọc lịch (R-28; <c>code-lint.py</c> chặn gọi từ <c>Runtime/Unity</c>).
    /// </summary>
    public static class LiveEventCalendarSha256
    {
        private const int DigestByteCount = 32;

        // Bảng tra thay cho byte.ToString("x2"): định dạng số đi qua culture của thread; bảng cố định không phụ thuộc
        // culture nên sha hiện ra giống nhau trên mọi máy (R-20).
        private const string LowercaseHexDigits = "0123456789abcdef";

        /// <summary>Hex thường, đúng 64 ký tự.</summary>
        public static string ComputeHex(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            byte[] digest;
            // SP-10 đã kiểm: SHA256.Create() có trong netstandard, chạy được trong assembly noEngineReferences ở cả
            // 2022.3 và 6000.6, khớp NIST và Python hashlib — không cần cài đặt thuần.
            using (SHA256 algorithm = SHA256.Create())
            {
                digest = algorithm.ComputeHash(bytes);
            }

            if (digest.Length != DigestByteCount)
            {
                throw new InvalidOperationException("SHA-256 phải trả 32 byte, nhận " + digest.Length + ".");
            }

            var characters = new char[DigestByteCount * 2];
            for (int index = 0; index < DigestByteCount; index++)
            {
                characters[index * 2] = LowercaseHexDigits[digest[index] >> 4];
                characters[index * 2 + 1] = LowercaseHexDigits[digest[index] & 0x0F];
            }
            return new string(characters);
        }
    }
}
