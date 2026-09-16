using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DreamTech.LiveOps.Unity;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Trạng thái đăng của phiên: dấu đang là bản so, diff nháp ↔ bản so, nguồn bản so (V-13), JSON sẽ copy, lần xuất gần nhất, tick
    /// "Đã xem" và ghi/gỡ dấu. Mọi thứ không phải nội dung lịch (định dạng đang chọn, lần xuất, "Đã xem", dấu đang chọn) sống trong
    /// SessionState theo GUID asset (8.10) — không làm asset bẩn. Chữ ký đóng băng khi cổng W3 xanh (PD-35).
    /// </summary>
    internal sealed class LiveOpsHubPublishState
    {
        internal const string ExportFormatStoreName = "ExportFormat";
        internal const string LastExportedShaStoreName = "LastExportedSha";
        internal const string LastExportedUtcStoreName = "LastExportedUtc";
        internal const string LastExportedViaFileStoreName = "LastExportedViaFile";
        internal const string ReviewedStoreName = "Reviewed";
        internal const string ActiveBaselineStoreName = "ActiveBaseline";

        private const char ReviewedKeySeparator = '\n';
        private const char ReviewedKeyFieldSeparator = '|';

        private readonly LiveOpsHubCalendarSession _session;
        private LiveOpsHubCompareSource _requestedCompareSource = LiveOpsHubCompareSource.Published;

        private PublishedCalendarStamp _parsedActiveStamp;
        private LiveEventCalendarDocument _parsedActiveBaseline;
        private PublishedCalendarStamp _parsedLatestStamp;
        private LiveEventCalendarDocument _parsedLatestBaseline;

        private DiffCache _publishedDiff;
        private DiffCache _compareDiff;

        private int _jsonRevision = -1;
        private LiveEventCalendarJsonFormat _jsonFormat;
        private LiveEventCalendarJsonText _currentJson;

        internal LiveOpsHubPublishState(LiveOpsHubCalendarSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>Dấu đang là bản so: dấu người dùng chọn trong phiên nếu còn trong asset, không thì dấu mới nhất; null = chưa đăng lần nào.</summary>
        public PublishedCalendarStamp ActiveStamp
        {
            get
            {
                LiveEventCalendarDocument document = _session.Document;
                string selectedKey = _session.Store.GetString(ActiveBaselineStoreName, string.Empty);
                if (selectedKey.Length > 0)
                {
                    foreach (PublishedCalendarStamp stamp in document.PublishedStamps)
                    {
                        if (string.Equals(StampKey(stamp), selectedKey, StringComparison.Ordinal)) return stamp;
                    }
                }
                return document.LatestStamp;
            }
        }

        /// <summary>
        /// (V-3) <c>ParseDocument(ActiveStamp.SnapshotJson).Document</c> — <c>EventTypes</c> RỖNG theo chủ ý: bản đăng không mang định nghĩa
        /// loại, ghép loại của nháp vào sẽ làm diff nói dối. Chỉ dùng cho diff + luật 9/10. null khi không có dấu hoặc JSON dấu không đọc được.
        /// </summary>
        public LiveEventCalendarDocument ActiveBaseline
        {
            get
            {
                PublishedCalendarStamp stamp = ActiveStamp;
                if (!ReferenceEquals(stamp, _parsedActiveStamp))
                {
                    _parsedActiveStamp = stamp;
                    _parsedActiveBaseline = ParseStamp(stamp);
                }
                return _parsedActiveBaseline;
            }
        }

        /// <summary>(CC-VALB-1) Tài liệu của dấu MỚI NHẤT cho luật 12, độc lập với dấu đang chọn làm bản so; null khi không có/không đọc được.</summary>
        internal LiveEventCalendarDocument LatestStampBaseline
        {
            get
            {
                PublishedCalendarStamp stamp = _session.Document.LatestStamp;
                if (!ReferenceEquals(stamp, _parsedLatestStamp))
                {
                    _parsedLatestStamp = stamp;
                    _parsedLatestBaseline = ParseStamp(stamp);
                }
                return _parsedLatestBaseline;
            }
        }

        /// <summary>(V-13) Nguồn bản so đang hiệu lực: nguồn đã chọn nếu còn dữ liệu, không thì <see cref="LiveOpsHubCompareSource.Published"/>.</summary>
        public LiveOpsHubCompareSource ActiveCompareSource
        {
            get
            {
                switch (_requestedCompareSource)
                {
                    case LiveOpsHubCompareSource.Disk:
                        return _session.DiskConflict != null ? LiveOpsHubCompareSource.Disk : LiveOpsHubCompareSource.Published;
                    case LiveOpsHubCompareSource.Remote:
                        return _session.Remote.Document != null ? LiveOpsHubCompareSource.Remote : LiveOpsHubCompareSource.Published;
                    default:
                        return LiveOpsHubCompareSource.Published;
                }
            }
        }

        /// <summary>Bản so theo nguồn: <see cref="ActiveBaseline"/> | bản trên đĩa lúc xung đột | JSON đang chạy đã dán.</summary>
        public LiveEventCalendarDocument CompareDocument
        {
            get
            {
                switch (ActiveCompareSource)
                {
                    case LiveOpsHubCompareSource.Disk: return _session.DiskConflict.DiskDocument;
                    case LiveOpsHubCompareSource.Remote: return _session.Remote.Document;
                    default: return ActiveBaseline;
                }
            }
        }

        /// <summary><c>Compare(CompareDocument, nháp, now)</c>; bằng đúng <see cref="PublishedDiff"/> khi nguồn là Published.</summary>
        public LiveEventCalendarDiffResult CompareDiff
        {
            get
            {
                if (ActiveCompareSource == LiveOpsHubCompareSource.Published) return PublishedDiff;
                return ComputeDiff(ref _compareDiff, CompareDocument);
            }
        }

        /// <summary>Nguồn không có dữ liệu (xung đột đã giải, chưa dán remote) vẫn nhận lời chọn nhưng hiệu lực là Published — không ném.</summary>
        public void SelectCompareSource(LiveOpsHubCompareSource source)
        {
            LiveOpsHubCompareSource requested = source == LiveOpsHubCompareSource.None ? LiveOpsHubCompareSource.Published : source;
            if (requested == _requestedCompareSource) return;
            _requestedCompareSource = requested;
            _session.NotifyPublishStateChanged();
        }

        /// <summary>"5 khác bản đã đăng": nháp so với <see cref="ActiveBaseline"/> (chưa đăng lần nào thì so với lịch rỗng).</summary>
        public LiveEventCalendarDiffResult PublishedDiff => ComputeDiff(ref _publishedDiff, ActiveBaseline);

        /// <summary>Định dạng Xuất JSON đang chọn (SessionState); mặc định 2.</summary>
        public LiveEventCalendarJsonFormat SelectedFormat
        {
            get
            {
                int stored = _session.Store.GetInt(ExportFormatStoreName, (int)LiveEventCalendarJsonFormat.Version2);
                return stored == (int)LiveEventCalendarJsonFormat.Version1 ? LiveEventCalendarJsonFormat.Version1 : LiveEventCalendarJsonFormat.Version2;
            }
            set
            {
                if (value == SelectedFormat) return;
                _session.Store.SetInt(ExportFormatStoreName, (int)value);
                _session.NotifyPublishStateChanged();
            }
        }

        /// <summary>JSON đúng như Copy/Lưu file sẽ đưa đi — viết lại khi tài liệu hoặc định dạng đổi.</summary>
        public LiveEventCalendarJsonText CurrentJson
        {
            get
            {
                LiveEventCalendarJsonFormat format = SelectedFormat;
                if (_currentJson == null || _jsonRevision != _session.DocumentRevision || _jsonFormat != format)
                {
                    _currentJson = LiveEventCalendarJsonWriter.Write(_session.Document, format);
                    _jsonRevision = _session.DocumentRevision;
                    _jsonFormat = format;
                }
                return _currentJson;
            }
        }

        /// <summary>sha đủ 64 ký tự của lần Copy/Lưu file gần nhất trong phiên (SessionState); "" khi chưa.</summary>
        public string LastExportedSha256Hex => _session.Store.GetString(LastExportedShaStoreName, string.Empty);

        public DateTime? LastExportedUtc
        {
            get
            {
                string text = _session.Store.GetString(LastExportedUtcStoreName, string.Empty);
                return LiveEventUtcText.TryParse(text, out DateTime exported) ? exported : (DateTime?)null;
            }
        }

        /// <summary>Lần xuất gần nhất là Lưu file (outcome (c′)) hay Copy (c).</summary>
        internal bool LastExportedViaFile => _session.Store.GetInt(LastExportedViaFileStoreName, 0) == 1;

        public void RecordExport(LiveEventCalendarJsonText json, bool viaFile)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            _session.Store.SetString(LastExportedShaStoreName, json.Sha256Hex);
            _session.Store.SetString(LastExportedUtcStoreName, LiveEventUtcText.Format(_session.Clock.UtcNow));
            _session.Store.SetInt(LastExportedViaFileStoreName, viaFile ? 1 : 0);
            _session.NotifyPublishStateChanged();
        }

        /// <summary>Khoá = sha dấu đang so + mục + Fingerprint của thay đổi: mục đổi tiếp (Fingerprint khác) thì tick tự tắt.</summary>
        public bool IsReviewed(LiveEventCalendarChange change)
        {
            if (change == null) return false;
            string key = ReviewedKey(change);
            foreach (string stored in ReadReviewedKeys())
            {
                if (string.Equals(stored, key, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public void SetReviewed(LiveEventCalendarChange change, bool reviewed)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            string key = ReviewedKey(change);
            List<string> keys = ReadReviewedKeys();
            bool contains = keys.Contains(key);
            if (contains == reviewed) return;
            if (reviewed) keys.Add(key);
            else keys.Remove(key);
            _session.Store.SetString(ReviewedStoreName, string.Join(ReviewedKeySeparator.ToString(), keys));
            _session.NotifyPublishStateChanged();
        }

        /// <summary>Ghi dấu JSON đang hiện là bản đã đăng (AddPublishedStampEdit, một Undo group) rồi lưu asset — dấu phải nằm trong file để git thấy.</summary>
        public LiveOpsHubEditOutcome MarkPublished(string note)
        {
            if (_session.Asset == null) return LiveOpsHubEditOutcome.Failure(LiveOpsHubStrings.ServicesEditFailedNoAsset);
            LiveEventCalendarJsonText json = CurrentJson;
            string publisher = _session.PublisherIdentity.PublisherName;
            PublishedCalendarStamp stamp = new PublishedCalendarStamp(LiveEventUtcText.Format(_session.Clock.UtcNow),
                string.IsNullOrEmpty(publisher) ? LiveOpsHubStrings.ServicesStampNoPublisher : publisher, json.Sha256Hex, json.ByteCount,
                (int)json.Format, note ?? string.Empty, json.Text);
            string undoName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesUndoMarkPublishedFormat, json.ShortSha);
            LiveOpsHubEditOutcome outcome = _session.Apply(new AddPublishedStampEdit(stamp), undoName);
            if (!outcome.Applied) return outcome;
            SelectActiveStamp(null);
            if (_session.Save()) return outcome;
            // Lưu hỏng nhưng dấu ĐÃ nằm trong tài liệu (tab có *): trả thất bại MANG group của bước vừa áp để toast vẫn mời được Hoàn tác —
            // không thì người dùng đọc "Không lưu được…" và tin rằng chưa có gì xảy ra, trong khi lần ⌘S sau sẽ ghi cái dấu đó vào file.
            string failureText = _session.DiskConflict != null
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesSaveFailedDiskConflictFormat, _session.AssetFileName)
                : (_session.AssetPath.Length == 0
                    ? LiveOpsHubStrings.ServicesSaveFailedNoPath
                    : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesSaveFailedFormat, _session.AssetFileName));
            return LiveOpsHubEditOutcome.FailureAfterApply(outcome.UndoGroup, outcome.UndoName, failureText);
        }

        /// <summary>Gỡ dấu mới nhất (một Undo group, không tự lưu — gỡ nhầm thì Hoàn tác trước khi lưu).</summary>
        public LiveOpsHubEditOutcome RemoveLatestStamp()
        {
            PublishedCalendarStamp latest = _session.Document.LatestStamp;
            if (latest == null) return LiveOpsHubEditOutcome.Failure(LiveOpsHubStrings.ServicesEditFailedNotApplicable);
            string undoName = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesUndoRemoveLatestStampFormat, latest.ShortSha);
            LiveOpsHubEditOutcome outcome = _session.Apply(new RemoveLatestPublishedStampEdit(), undoName);
            if (outcome.Applied) SelectActiveStamp(null);
            return outcome;
        }

        /// <summary>Chọn dấu làm bản so trong phiên (card lịch sử của Xuất JSON); null = dấu mới nhất.</summary>
        internal void SelectActiveStamp(PublishedCalendarStamp stamp)
        {
            string key = stamp == null ? string.Empty : StampKey(stamp);
            if (string.Equals(_session.Store.GetString(ActiveBaselineStoreName, string.Empty), key, StringComparison.Ordinal)) return;
            if (key.Length == 0) _session.Store.Erase(ActiveBaselineStoreName);
            else _session.Store.SetString(ActiveBaselineStoreName, key);
            _session.NotifyPublishStateChanged();
        }

        /// <summary>Phiên đổi asset: kho SessionState khác, bỏ mọi cache của asset cũ; nguồn so về Published.</summary>
        internal void Rebind()
        {
            _requestedCompareSource = LiveOpsHubCompareSource.Published;
            _parsedActiveStamp = null;
            _parsedActiveBaseline = null;
            _parsedLatestStamp = null;
            _parsedLatestBaseline = null;
            _publishedDiff = null;
            _compareDiff = null;
            _currentJson = null;
            _jsonRevision = -1;
        }

        private LiveEventCalendarDiffResult ComputeDiff(ref DiffCache cache, LiveEventCalendarDocument baseline)
        {
            DateTime now = _session.Clock.UtcNow;
            // Hậu quả của diff phụ thuộc "bây giờ" (đợt đang chạy hay chưa): chỉ tính lại khi tài liệu/bản so đổi hoặc thời gian vượt một
            // mốc của nháp — health gọi mỗi giây không được biên dịch hai tài liệu mỗi giây.
            if (cache != null && cache.Revision == _session.DocumentRevision && ReferenceEquals(cache.Baseline, baseline)
                && now >= cache.ComputedAtUtc
                && !LiveOpsHubCheckState.FindEarliestMilestone(_session.Compilation, cache.ComputedAtUtc, now).HasValue)
            {
                return cache.Result;
            }
            cache = new DiffCache(_session.DocumentRevision, baseline, now, LiveEventCalendarDiff.Compare(baseline, _session.Document, now));
            return cache.Result;
        }

        private List<string> ReadReviewedKeys()
        {
            string text = _session.Store.GetString(ReviewedStoreName, string.Empty);
            List<string> keys = new List<string>();
            if (text.Length == 0) return keys;
            foreach (string key in text.Split(ReviewedKeySeparator))
            {
                if (key.Length > 0) keys.Add(key);
            }
            return keys;
        }

        private string ReviewedKey(LiveEventCalendarChange change)
        {
            PublishedCalendarStamp stamp = ActiveStamp;
            string identity = (stamp != null ? stamp.Sha256Hex : string.Empty) + ReviewedKeyFieldSeparator
                + ((int)change.ItemKind).ToString(CultureInfo.InvariantCulture) + ReviewedKeyFieldSeparator
                + change.ItemId + ReviewedKeyFieldSeparator + change.Fingerprint;
            // Băm về hex 64 ký tự trước khi lưu: Fingerprint là nội dung JSON-tương-đương của mục nên CÓ ký tự xuống dòng,
            // đúng bằng ký tự ngăn bản ghi trong SessionState — lưu thẳng thì mọi tick "Đã xem" của đợt cố định đọc lại
            // thành sai (khoá bị cắt làm nhiều mảnh). Hex không chứa ký tự ngăn nào nên hết hẳn lớp lỗi này.
            return LiveEventCalendarSha256.ComputeHex(Encoding.UTF8.GetBytes(identity));
        }

        private static string StampKey(PublishedCalendarStamp stamp)
        {
            return stamp.Sha256Hex + ReviewedKeyFieldSeparator + stamp.PublishedUtcText;
        }

        private static LiveEventCalendarDocument ParseStamp(PublishedCalendarStamp stamp)
        {
            if (stamp == null || string.IsNullOrWhiteSpace(stamp.SnapshotJson)) return null;
            LiveEventCalendarDocumentParseResult parsed = JsonLiveEventCalendarParser.ParseDocument(stamp.SnapshotJson);
            return parsed.IsReadable ? parsed.Document : null;
        }

        private sealed class DiffCache
        {
            public DiffCache(int revision, LiveEventCalendarDocument baseline, DateTime computedAtUtc, LiveEventCalendarDiffResult result)
            {
                Revision = revision;
                Baseline = baseline;
                ComputedAtUtc = computedAtUtc;
                Result = result;
            }

            public int Revision { get; }
            public LiveEventCalendarDocument Baseline { get; }
            public DateTime ComputedAtUtc { get; }
            public LiveEventCalendarDiffResult Result { get; }
        }
    }
}
