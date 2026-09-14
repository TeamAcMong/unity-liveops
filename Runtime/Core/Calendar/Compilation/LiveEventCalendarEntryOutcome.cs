using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>Loại mục đầu vào của bộ biên dịch — đợt cố định hay lần lặp của một luật.</summary>
    public enum LiveEventCalendarEntryKind
    {
        FixedEvent = 0,
        RecurringRule = 1,
    }

    /// <summary>
    /// Kết quả biên dịch của MỘT mục đầu vào (một đợt cố định, hoặc một luật lặp). Luôn có đúng một outcome cho mỗi
    /// mục trong tài liệu, dù mục đó bị giữ hay bị bỏ — validator dựa vào danh sách này để giải thích lý do, không
    /// tự đoán lại luật runtime.
    /// </summary>
    public sealed class LiveEventCalendarEntryOutcome
    {
        public LiveEventCalendarEntryOutcome(LiveEventCalendarEntryKind kind, int sourceIndex, string entryKey, string eventId,
            string eventType, bool isKept, LiveEventCalendarDropReason dropReason, IReadOnlyList<LiveEventCalendarDropReason> additionalItemReasons,
            string relatedEventId, DateTime? overlapStartUtc, DateTime? overlapEndUtc, string problemText)
        {
            Kind = kind;
            SourceIndex = sourceIndex;
            EntryKey = entryKey ?? string.Empty;
            EventId = eventId ?? string.Empty;
            EventType = eventType ?? string.Empty;
            IsKept = isKept;
            DropReason = dropReason;
            AdditionalItemReasons = additionalItemReasons ?? Array.Empty<LiveEventCalendarDropReason>();
            RelatedEventId = relatedEventId ?? string.Empty;
            OverlapStartUtc = overlapStartUtc;
            OverlapEndUtc = overlapEndUtc;
            ProblemText = problemText ?? string.Empty;
        }

        public LiveEventCalendarEntryKind Kind { get; }

        /// <summary>0-based trong mảng gốc (<c>FixedEvents</c> hoặc <c>RecurringRules</c>) — "mục thứ N" = SourceIndex + 1.</summary>
        public int SourceIndex { get; }

        /// <summary>Fixed: <see cref="FixedLiveEventEntry.EntryKey"/>; recurring: <see cref="RecurringLiveEventRule.EventType"/>.</summary>
        public string EntryKey { get; }

        /// <summary>Fixed: id của đợt. "" với recurring (một luật không có một id cố định — mỗi lần lặp có id riêng).</summary>
        public string EventId { get; }

        public string EventType { get; }
        public bool IsKept { get; }

        /// <summary>
        /// Lý do CHÍNH — đúng một mỗi mục bị bỏ, theo thứ tự kiểm của bộ biên dịch: UnreadableStartUtc →
        /// UnreadableEndUtc → InvalidIdentifier → EndNotAfterStart → (chỉ mục qua kiểm cấp mục) DuplicateEventId →
        /// OverlapsSameType → ShadowedByRecurring (fixed); InvalidRecurringRule (neo/chu kỳ/thời gian chạy/tiền tố/loại
        /// hỏng) → DuplicateRecurringType (recurring). <see cref="LiveEventCalendarDropReason.None"/> khi giữ.
        /// </summary>
        public LiveEventCalendarDropReason DropReason { get; }

        /// <summary>
        /// (V-7) Lỗi cấp mục khác của CÙNG mục (vd id có '#' khi lý do chính là giờ hỏng) — chỉ để inspector/hàng
        /// phát hiện nêu thêm ("cũng sai: …"), KHÔNG sinh phát hiện riêng ở validator.
        /// </summary>
        public IReadOnlyList<LiveEventCalendarDropReason> AdditionalItemReasons { get; }

        /// <summary>Đợt/luật được giữ lại khi mục này trùng id / chồng giờ / bị che.</summary>
        public string RelatedEventId { get; }

        public DateTime? OverlapStartUtc { get; }
        public DateTime? OverlapEndUtc { get; }

        /// <summary>Câu Problems tương thích 0.1.0 (chỉ để hiện, không parse).</summary>
        public string ProblemText { get; }
    }
}
