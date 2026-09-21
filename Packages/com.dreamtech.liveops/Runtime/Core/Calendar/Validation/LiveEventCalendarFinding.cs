using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Một phát hiện của Kiểm lịch — dữ liệu có mã, không có câu: Editor chọn câu theo cặp (<see cref="RuleId"/>,
    /// <see cref="DetailCode"/>). Chỉ dựng được qua <see cref="LiveEventCalendarFindingBuilder"/> (kiểm mã thuộc danh sách
    /// đóng); trạng thái "đã bỏ qua" do <see cref="LiveEventCalendarCheckRun"/> gắn sau khi luật chạy, vì luật không cần biết
    /// người dùng đã bỏ qua gì.
    /// </summary>
    public sealed class LiveEventCalendarFinding
    {
        internal LiveEventCalendarFinding(string ruleId, string detailCode, LiveEventCalendarConsequence consequence,
            LiveEventCalendarTargetKind targetKind, string targetId, string targetEntryKey, string relatedId, string foundText,
            string expectedText, DateTime? rangeStartUtc, DateTime? rangeEndUtc, DateTime? anchorUtc,
            LiveEventCalendarRepairKind repairKind, IReadOnlyList<LiveEventCalendarRepair> repairs, bool isAboutRemoteSnapshot,
            IgnoredCalendarWarning ignoredBy, IgnoredCalendarWarning dueReminder)
        {
            RuleId = ruleId;
            DetailCode = detailCode;
            Consequence = consequence;
            TargetKind = targetKind;
            TargetId = targetId;
            TargetEntryKey = targetEntryKey;
            RelatedId = relatedId;
            FoundText = foundText;
            ExpectedText = expectedText;
            RangeStartUtc = rangeStartUtc;
            RangeEndUtc = rangeEndUtc;
            AnchorUtc = anchorUtc;
            RepairKind = repairKind;
            Repairs = repairs;
            IsAboutRemoteSnapshot = isAboutRemoteSnapshot;
            IgnoredBy = ignoredBy;
            DueReminder = dueReminder;
            Fingerprint = BuildFingerprint(ruleId, targetId, rangeStartUtc, rangeEndUtc);
        }

        public string RuleId { get; }

        /// <summary>Biến thể trong luật, kebab ("end", "prefix-changed"…) — thuộc <see cref="LiveEventCalendarDetailCodes.ForRule"/>.</summary>
        public string DetailCode { get; }

        public LiveEventCalendarConsequence Consequence { get; }
        public LiveEventCalendarTargetKind TargetKind { get; }

        /// <summary>Id đợt / loại / "" (bản remote).</summary>
        public string TargetId { get; }

        /// <summary>EntryKey của đợt cố định, hoặc loại của luật lặp / định nghĩa loại.</summary>
        public string TargetEntryKey { get; }

        public string RelatedId { get; }

        /// <summary>Giá trị thô "tìm thấy" (mono). Nhiều giá trị nối bằng <see cref="LiveEventCalendarFindingBuilder.ValueSeparator"/>.</summary>
        public string FoundText { get; }

        /// <summary>Giá trị thô "cần" (mono); "" khi luật không có giá trị đúng để đề xuất.</summary>
        public string ExpectedText { get; }

        public DateTime? RangeStartUtc { get; }
        public DateTime? RangeEndUtc { get; }

        /// <summary>Để sắp thứ tự thời gian và căn khung timeline.</summary>
        public DateTime? AnchorUtc { get; }

        public LiveEventCalendarRepairKind RepairKind { get; }

        /// <summary>SafeRepair: đúng 1; Proposal/Decision: ≥ 1; None/Ignorable: 0.</summary>
        public IReadOnlyList<LiveEventCalendarRepair> Repairs { get; }

        /// <summary>Khớp một <see cref="IgnoredCalendarWarning"/> còn hiệu lực — nằm ở <see cref="LiveEventCalendarCheckReport.IgnoredFindings"/>.</summary>
        public bool IsIgnored => IgnoredBy != null;

        /// <summary><c>ruleId|targetId|rangeStart|rangeEnd</c> (giờ canonical, "" khi không có) — khoá "Bỏ qua" và "Đã xem".</summary>
        public string Fingerprint { get; }

        /// <summary>(V-17) Nói về JSON đang chạy đã dán, không về nháp — không đếm vào Bị bỏ/NeedsAction, không chặn Copy JSON.</summary>
        public bool IsAboutRemoteSnapshot { get; }

        /// <summary>(V-15) Mục cảnh báo đã khớp khi <see cref="IsIgnored"/>; <c>null</c> khi không — "Bỏ bỏ qua" dùng thẳng.</summary>
        public IgnoredCalendarWarning IgnoredBy { get; }

        /// <summary>
        /// Ghi chú hẹn giờ đã tới hạn khớp phát hiện này ("cờ nhắc" của mục 6.1): phát hiện hiện lại trong nhóm hậu quả của luật
        /// gốc, Editor gắn tag "đã tới hẹn" và nêu ghi chú. <c>null</c> khi không có hẹn nào tới hạn. Thêm ngoài chữ ký mục 3 (chỉ
        /// thêm): không có nó Editor phải tự dò lại danh sách cảnh báo theo chuỗi — đúng điều <see cref="IgnoredBy"/> sinh ra để tránh.
        /// </summary>
        public IgnoredCalendarWarning DueReminder { get; }

        /// <summary>Bản sao mang trạng thái bỏ qua — CheckRun gọi sau khi luật chạy.</summary>
        internal LiveEventCalendarFinding WithIgnoreState(IgnoredCalendarWarning ignoredBy, IgnoredCalendarWarning dueReminder)
        {
            if (ReferenceEquals(ignoredBy, IgnoredBy) && ReferenceEquals(dueReminder, DueReminder)) return this;
            return new LiveEventCalendarFinding(RuleId, DetailCode, Consequence, TargetKind, TargetId, TargetEntryKey, RelatedId,
                FoundText, ExpectedText, RangeStartUtc, RangeEndUtc, AnchorUtc, RepairKind, Repairs, IsAboutRemoteSnapshot,
                ignoredBy, dueReminder);
        }

        /// <summary>Canonical text của một mốc trong <see cref="Fingerprint"/> và khi so với khoảng của cảnh báo đã bỏ qua.</summary>
        internal static string FormatRangeBound(DateTime? value)
        {
            return value.HasValue ? LiveEventUtcText.Format(value.Value) : string.Empty;
        }

        private static string BuildFingerprint(string ruleId, string targetId, DateTime? rangeStartUtc, DateTime? rangeEndUtc)
        {
            return ruleId + "|" + targetId + "|" + FormatRangeBound(rangeStartUtc) + "|" + FormatRangeBound(rangeEndUtc);
        }
    }
}
