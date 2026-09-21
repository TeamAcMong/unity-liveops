using System;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Một cách sửa kèm phát hiện: lệnh sửa thuần (<see cref="LiveEventCalendarEdit"/>) + dữ liệu thô trước/sau để Editor dựng
    /// câu. Core không viết câu tiếng Việt ở đây — chỉ giá trị (mono), để câu chữ có đúng một nguồn phía Editor (V-8).
    /// </summary>
    public sealed class LiveEventCalendarRepair
    {
        public LiveEventCalendarRepair(string repairId, LiveEventCalendarRepairKind kind, LiveEventCalendarEdit edit,
            string beforeText, string afterText, DateTime? newStartUtc, DateTime? newEndUtc)
        {
            if (string.IsNullOrEmpty(repairId)) throw new ArgumentException("repairId không được rỗng.", nameof(repairId));
            // Cách sửa không có lệnh là nút bấm không làm gì — lỗi lập trình của luật, không phải dữ liệu lịch hỏng.
            if (edit == null) throw new ArgumentNullException(nameof(edit));

            RepairId = repairId;
            Kind = kind;
            Edit = edit;
            BeforeText = beforeText ?? string.Empty;
            AfterText = afterText ?? string.Empty;
            NewStartUtc = newStartUtc;
            NewEndUtc = newEndUtc;
        }

        /// <summary>"shift-start-keep-end", "shift-whole-keep-duration", "normalize-utc", "revert-prefix"…</summary>
        public string RepairId { get; }

        public LiveEventCalendarRepairKind Kind { get; }
        public LiveEventCalendarEdit Edit { get; }

        /// <summary>Dữ liệu thô trước khi sửa (mono) — câu tiếng Việt dựng ở Editor.</summary>
        public string BeforeText { get; }

        public string AfterText { get; }
        public DateTime? NewStartUtc { get; }
        public DateTime? NewEndUtc { get; }
    }
}
