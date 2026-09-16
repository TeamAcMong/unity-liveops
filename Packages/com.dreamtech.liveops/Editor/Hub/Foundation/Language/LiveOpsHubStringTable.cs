using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ của hub: một khoá → một câu cho mỗi ngôn ngữ. Bảng dựng đúng MỘT lần (lazy) trong
    /// <see cref="LiveOpsHubStringCatalog"/> rồi chỉ đọc, nên tra chữ là tra từ điển trong bộ nhớ — không I/O, không parse.
    /// <para>
    /// <see cref="Add"/> nhận hai ngôn ngữ trong cùng một lệnh để không bao giờ có chuyện thêm khoá ở bản này mà quên bản kia;
    /// <see cref="AddShared"/> dành cho ký hiệu trung tính (+ · — × {0} → {1}) mà dịch ra cũng y hệt.
    /// </para>
    /// </summary>
    internal sealed class LiveOpsHubStringTable
    {
        private static readonly int LanguageCount = Enum.GetValues(typeof(LiveOpsHubLanguageId)).Length;

        // Hai câu lỗi lập trình này KHÔNG đi qua catalog: chúng ném ra trong lúc chính bảng đang dựng, tra catalog lúc đó là
        // gọi lại BuildTable giữa chừng. Chúng cũng không bao giờ hiện trên UI — bảng hỏng là hub không dựng được.
        private const string ErrorKeyEmpty = "Khoá chữ không được rỗng — luôn truyền nameof(LiveOpsHubStrings.<Tên>).";
        private const string ErrorDuplicateKeyPrefix = "Khoá chữ bị khai hai lần, mỗi khoá chỉ thuộc một vùng: ";

        private readonly Dictionary<string, string[]> _textsByKey = new Dictionary<string, string[]>(StringComparer.Ordinal);
        private readonly HashSet<string> _sharedKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _keysInRegistrationOrder = new List<string>();

        /// <summary>Khoá theo đúng thứ tự đăng ký — bảng đối chiếu bản dịch đọc được từ trên xuống như file catalog.</summary>
        internal IReadOnlyList<string> Keys
        {
            get { return _keysInRegistrationOrder; }
        }

        /// <param name="key">Tên thành viên <see cref="LiveOpsHubStrings"/>, luôn lấy bằng <c>nameof</c>.</param>
        /// <param name="vietnamese">Bản gốc — bắt buộc có.</param>
        /// <param name="english">Bản dịch; <c>null</c> khi gói dịch chưa điền (tra chữ rơi về bản gốc, test liệt kê ra).</param>
        internal void Add(string key, string vietnamese, string english)
        {
            string[] texts = Claim(key);
            texts[(int)LiveOpsHubLanguageId.Vietnamese] = vietnamese;
            texts[(int)LiveOpsHubLanguageId.English] = english;
        }

        /// <summary>Một giá trị dùng cho mọi ngôn ngữ — vẫn tính là "có đủ hai ngôn ngữ".</summary>
        internal void AddShared(string key, string text)
        {
            string[] texts = Claim(key);
            for (int index = 0; index < texts.Length; index++) texts[index] = text;
            _sharedKeys.Add(key);
        }

        internal bool IsShared(string key)
        {
            return key != null && _sharedKeys.Contains(key);
        }

        internal bool ContainsKey(string key)
        {
            return key != null && _textsByKey.ContainsKey(key);
        }

        /// <summary>Trả false khi không có khoá, hoặc có khoá mà ngôn ngữ đó chưa có chữ (chưa dịch).</summary>
        internal bool TryGet(LiveOpsHubLanguageId language, string key, out string text)
        {
            text = null;
            if (key == null) return false;
            string[] texts;
            if (!_textsByKey.TryGetValue(key, out texts)) return false;
            int index = (int)language;
            if (index < 0 || index >= texts.Length) return false;
            string candidate = texts[index];
            if (string.IsNullOrEmpty(candidate)) return false;
            text = candidate;
            return true;
        }

        private string[] Claim(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException(ErrorKeyEmpty, "key");
            if (_textsByKey.ContainsKey(key))
            {
                // Khoá trùng là hai vùng cùng khai một tên: tra chữ sẽ im lặng lấy bản đăng ký sau, nên chặn ngay lúc dựng bảng.
                throw new ArgumentException(ErrorDuplicateKeyPrefix + key, "key");
            }
            string[] texts = new string[LanguageCount];
            _textsByKey.Add(key, texts);
            _keysInRegistrationOrder.Add(key);
            return texts;
        }
    }
}
