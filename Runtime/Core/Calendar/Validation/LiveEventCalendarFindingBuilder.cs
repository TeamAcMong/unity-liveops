using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// (V-8) Cách DUY NHẤT dựng <see cref="LiveEventCalendarFinding"/> — luật core dùng, và assembly test Editor dựng phát hiện
    /// giả cho câu chữ mà không phải chạy validator hay chờ luật 9–12. <see cref="Build"/> ném khi mã biến thể không thuộc
    /// danh sách đóng của luật: phát hiện mang mã lạ sẽ không có câu ở Editor, nên chặn ngay ở nơi sinh ra.
    /// </summary>
    public sealed class LiveEventCalendarFindingBuilder
    {
        /// <summary>Ngăn cách khi <c>FoundText</c>/<c>ExpectedText</c> mang nhiều giá trị thô (vd giờ bắt đầu và kết thúc cùng hỏng).</summary>
        public const string ValueSeparator = " · ";

        private static readonly LiveEventCalendarRepair[] NoRepairs = Array.Empty<LiveEventCalendarRepair>();

        private readonly string _ruleId;
        private readonly string _detailCode;
        private readonly LiveEventCalendarConsequence _consequence;
        private readonly LiveEventCalendarTargetKind _targetKind;
        private readonly string _targetId;
        private string _targetEntryKey = string.Empty;
        private string _relatedId = string.Empty;
        private string _foundText = string.Empty;
        private string _expectedText = string.Empty;
        private DateTime? _rangeStartUtc;
        private DateTime? _rangeEndUtc;
        private DateTime? _anchorUtc;
        private LiveEventCalendarRepairKind _repairKind = LiveEventCalendarRepairKind.None;
        private LiveEventCalendarRepair[] _repairs = NoRepairs;
        private bool _isAboutRemoteSnapshot;

        public LiveEventCalendarFindingBuilder(string ruleId, string detailCode, LiveEventCalendarConsequence consequence,
            LiveEventCalendarTargetKind targetKind, string targetId)
        {
            _ruleId = ruleId ?? string.Empty;
            _detailCode = detailCode ?? string.Empty;
            _consequence = consequence;
            _targetKind = targetKind;
            _targetId = targetId ?? string.Empty;
        }

        public LiveEventCalendarFindingBuilder WithTargetEntryKey(string targetEntryKey)
        {
            _targetEntryKey = targetEntryKey ?? string.Empty;
            return this;
        }

        public LiveEventCalendarFindingBuilder WithRelatedId(string relatedId)
        {
            _relatedId = relatedId ?? string.Empty;
            return this;
        }

        public LiveEventCalendarFindingBuilder WithTexts(string foundText, string expectedText)
        {
            _foundText = foundText ?? string.Empty;
            _expectedText = expectedText ?? string.Empty;
            return this;
        }

        public LiveEventCalendarFindingBuilder WithRange(DateTime? rangeStartUtc, DateTime? rangeEndUtc)
        {
            _rangeStartUtc = AsUtc(rangeStartUtc);
            _rangeEndUtc = AsUtc(rangeEndUtc);
            return this;
        }

        public LiveEventCalendarFindingBuilder WithAnchor(DateTime? anchorUtc)
        {
            _anchorUtc = AsUtc(anchorUtc);
            return this;
        }

        public LiveEventCalendarFindingBuilder WithRepairs(LiveEventCalendarRepairKind repairKind, IEnumerable<LiveEventCalendarRepair> repairs)
        {
            var copied = new List<LiveEventCalendarRepair>();
            if (repairs != null)
            {
                foreach (LiveEventCalendarRepair repair in repairs)
                {
                    if (repair == null) throw new ArgumentException("Danh sách cách sửa không được chứa null.", nameof(repairs));
                    copied.Add(repair);
                }
            }
            _repairKind = repairKind;
            _repairs = copied.ToArray();
            return this;
        }

        public LiveEventCalendarFindingBuilder WithRemoteSnapshotSubject(bool isAboutRemoteSnapshot)
        {
            _isAboutRemoteSnapshot = isAboutRemoteSnapshot;
            return this;
        }

        public LiveEventCalendarFinding Build()
        {
            if (!LiveEventCalendarDetailCodes.IsDeclared(_ruleId, _detailCode))
            {
                throw new ArgumentException("DetailCode '" + _detailCode + "' không thuộc LiveEventCalendarDetailCodes.ForRule('" +
                    _ruleId + "') — thêm mã vào danh sách đóng trước khi luật phát ra.");
            }
            EnsureRepairCountMatchesKind(_repairKind, _repairs.Length);

            return new LiveEventCalendarFinding(_ruleId, _detailCode, _consequence, _targetKind, _targetId, _targetEntryKey,
                _relatedId, _foundText, _expectedText, _rangeStartUtc, _rangeEndUtc, _anchorUtc, _repairKind,
                (LiveEventCalendarRepair[])_repairs.Clone(), _isAboutRemoteSnapshot, null, null);
        }

        /// <summary>
        /// Số cách sửa phải khớp kiểu: nút "Sửa" áp đúng một lệnh; "Đề xuất…"/"Quyết định…" mở popover cần ít nhất một lựa chọn;
        /// None/Ignorable không có lệnh. Proposal cho phép MỘT lựa chọn vì mục 6.1 có ca chỉ một đề xuất hợp lệ (vd kết thúc
        /// bằng bắt đầu thì không có "đổi chỗ bắt đầu/kết thúc").
        /// </summary>
        private static void EnsureRepairCountMatchesKind(LiveEventCalendarRepairKind repairKind, int repairCount)
        {
            switch (repairKind)
            {
                case LiveEventCalendarRepairKind.None:
                case LiveEventCalendarRepairKind.Ignorable:
                    if (repairCount != 0) throw new ArgumentException("RepairKind " + repairKind + " không mang cách sửa.");
                    return;
                case LiveEventCalendarRepairKind.SafeRepair:
                    if (repairCount != 1) throw new ArgumentException("SafeRepair phải có đúng một cách sửa.");
                    return;
                default:
                    if (repairCount < 1) throw new ArgumentException("RepairKind " + repairKind + " cần ít nhất một cách sửa.");
                    return;
            }
        }

        private static DateTime? AsUtc(DateTime? value)
        {
            return value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : (DateTime?)null;
        }
    }
}
