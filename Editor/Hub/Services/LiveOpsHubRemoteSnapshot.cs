using System;
using System.Text;
using DreamTech.LiveOps.Unity;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// JSON đang chạy trên remote config mà người dùng đã dán để so (Q-6): sống trong SessionState theo GUID asset — mất khi đóng
    /// Unity và KHÔNG làm asset bẩn, vì dán để so không phải là sửa lịch. Đọc bằng đúng parser của game
    /// (<see cref="JsonLiveEventCalendarParser.ParseDocument"/>) để luật 12 và bảng loại (V-17) thấy đúng thứ game sẽ thấy; sha tính
    /// trên byte UTF-8 NGUYÊN VĂN bản dán (đường nhanh của luật 12 so với sha của dấu đã đăng).
    /// </summary>
    internal sealed class LiveOpsHubRemoteSnapshot
    {
        internal const string PastedTextStoreName = "RemoteSnapshot";
        internal const string VerifiedUtcStoreName = "RemoteVerifiedUtc";

        private LiveOpsHubSessionStore _store;
        private string _pastedText = string.Empty;
        private LiveEventCalendarDocumentParseResult _parseResult;
        private string _sha256Hex = string.Empty;
        private DateTime? _verifiedUtc;

        internal LiveOpsHubRemoteSnapshot(LiveOpsHubSessionStore store)
        {
            Rebind(store);
        }

        /// <summary>Bản dán, đối chiếu hoặc xoá — màn Xuất/Kiểm lịch vẽ lại, phiên đánh dấu kiểm cũ.</summary>
        public event Action Changed;

        /// <summary>true khi đã dán một chuỗi không trắng.</summary>
        public bool HasSnapshot => _pastedText.Length > 0;

        /// <summary>Chuỗi dán nguyên văn; "" khi chưa dán.</summary>
        public string PastedText => _pastedText;

        /// <summary>Kết quả parser của game; null khi chưa dán.</summary>
        public LiveEventCalendarDocumentParseResult ParseResult => _parseResult;

        /// <summary>Parser đọc được bản dán (cú pháp đúng) — cùng tiêu chí "đọc được" của game.</summary>
        public bool IsReadable => _parseResult != null && _parseResult.IsReadable;

        /// <summary>Tài liệu đọc từ bản dán; null khi chưa dán hoặc không đọc được — luật 12 coi null là "chưa dán".</summary>
        public LiveEventCalendarDocument Document => IsReadable ? _parseResult.Document : null;

        /// <summary>SHA-256 hex của byte UTF-8 bản dán; "" khi chưa dán.</summary>
        public string Sha256Hex => _sha256Hex;

        /// <summary>Lúc dán/đối chiếu gần nhất ("đã đối chiếu 09:10", S-12); null khi chưa dán.</summary>
        public DateTime? VerifiedUtc => _verifiedUtc;

        /// <summary>Đường nhanh: bản dán trùng từng byte với JSON của dấu (không cần viết lại chuẩn như luật 12).</summary>
        public bool MatchesStampExactly(PublishedCalendarStamp stamp)
        {
            return stamp != null && HasSnapshot && string.Equals(stamp.Sha256Hex, _sha256Hex, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Dán (hoặc dán lại) JSON đang chạy. Chuỗi trắng = xoá, vì "đã dán rỗng" không phải bằng chứng gì về remote.</summary>
        public void Set(string pastedText, DateTime verifiedUtc)
        {
            if (string.IsNullOrWhiteSpace(pastedText))
            {
                Clear();
                return;
            }
            _store.SetString(PastedTextStoreName, pastedText);
            _store.SetString(VerifiedUtcStoreName, LiveEventUtcText.Format(DateTime.SpecifyKind(verifiedUtc, DateTimeKind.Utc)));
            Load();
            Changed?.Invoke();
        }

        /// <summary>Ghi lại giờ đối chiếu mà không đổi bản dán (bấm "Đối chiếu lại" với cùng chuỗi).</summary>
        public void MarkVerified(DateTime verifiedUtc)
        {
            if (!HasSnapshot) return;
            _store.SetString(VerifiedUtcStoreName, LiveEventUtcText.Format(DateTime.SpecifyKind(verifiedUtc, DateTimeKind.Utc)));
            _verifiedUtc = DateTime.SpecifyKind(verifiedUtc, DateTimeKind.Utc);
            Changed?.Invoke();
        }

        public void Clear()
        {
            bool hadSnapshot = HasSnapshot;
            _store.Erase(PastedTextStoreName);
            _store.Erase(VerifiedUtcStoreName);
            Load();
            if (hadSnapshot) Changed?.Invoke();
        }

        /// <summary>Đổi asset: bản dán thuộc về asset (khoá theo GUID) nên đọc lại theo kho của asset mới, không mang bản cũ sang.</summary>
        internal void Rebind(LiveOpsHubSessionStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            Load();
        }

        private void Load()
        {
            _pastedText = _store.GetString(PastedTextStoreName, string.Empty);
            if (_pastedText.Length == 0)
            {
                _parseResult = null;
                _sha256Hex = string.Empty;
                _verifiedUtc = null;
                return;
            }
            _parseResult = JsonLiveEventCalendarParser.ParseDocument(_pastedText);
            _sha256Hex = LiveEventCalendarSha256.ComputeHex(Encoding.UTF8.GetBytes(_pastedText));
            string verifiedText = _store.GetString(VerifiedUtcStoreName, string.Empty);
            _verifiedUtc = LiveEventUtcText.TryParse(verifiedText, out DateTime verified) ? verified : (DateTime?)null;
        }
    }
}
