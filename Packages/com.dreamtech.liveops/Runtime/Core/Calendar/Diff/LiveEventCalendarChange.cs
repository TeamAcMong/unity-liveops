using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Một mục (đợt cố định, luật lặp hoặc định nghĩa loại) khác giữa bản so và nháp — MỘT hàng diff dù nhiều field đổi.
    /// Hậu quả là ô nặng nhất khớp với mục theo bảng 6.3, suy từ cách <c>LiveOpsSystem</c> tìm lại bản ghi của người chơi
    /// (không phải từ việc "field nào đổi"), để hàng diff nói đúng điều người chơi sẽ gặp.
    /// </summary>
    public sealed class LiveEventCalendarChange
    {
        private static readonly LiveEventCalendarFieldChange[] NoFields = Array.Empty<LiveEventCalendarFieldChange>();

        public LiveEventCalendarChange(LiveEventCalendarChangeKind kind, LiveEventCalendarItemKind itemKind, string itemId,
            string entryKey, IReadOnlyList<LiveEventCalendarFieldChange> fields, LiveEventCalendarConsequence consequence,
            string runningEventIdBefore, string runningEventIdAfter, DateTime? runningEventEndUtc, bool willBeDropped,
            string fingerprint)
        {
            // Mục giữ nguyên chỉ được đếm — cho phép dựng hàng Kept sẽ làm số "Đổi" và danh sách lệch nhau.
            if (kind == LiveEventCalendarChangeKind.Kept)
            {
                throw new ArgumentException("Mục giữ nguyên không phải một thay đổi — chỉ đếm ở KeptCount.", nameof(kind));
            }

            Kind = kind;
            ItemKind = itemKind;
            ItemId = itemId ?? string.Empty;
            EntryKey = entryKey ?? string.Empty;
            Fields = fields != null ? new List<LiveEventCalendarFieldChange>(fields) : (IReadOnlyList<LiveEventCalendarFieldChange>)NoFields;
            Consequence = consequence;
            RunningEventIdBefore = runningEventIdBefore ?? string.Empty;
            RunningEventIdAfter = runningEventIdAfter ?? string.Empty;
            RunningEventEndUtc = runningEventEndUtc;
            WillBeDropped = willBeDropped;
            Fingerprint = fingerprint ?? string.Empty;
        }

        public LiveEventCalendarChangeKind Kind { get; }
        public LiveEventCalendarItemKind ItemKind { get; }

        /// <summary>Id đợt cố định / loại của luật / id loại. Mục đổi id ("chưa lưu") mang id mới.</summary>
        public string ItemId { get; }

        /// <summary>EntryKey của đợt cố định trong nháp (để chọn + căn khung); "" với luật, loại và đợt đã xoá khỏi nháp.</summary>
        public string EntryKey { get; }

        /// <summary>Rỗng với <see cref="LiveEventCalendarChangeKind.Added"/> và <see cref="LiveEventCalendarChangeKind.Removed"/>.</summary>
        public IReadOnlyList<LiveEventCalendarFieldChange> Fields { get; }

        public LiveEventCalendarConsequence Consequence { get; }

        /// <summary>Mất tiến độ bắt buộc tick "Đã xem" trước khi đánh dấu đã đăng — người chơi thật mất điểm.</summary>
        public bool IsReviewRequired => Consequence == LiveEventCalendarConsequence.ProgressLost;

        /// <summary>Id đợt đang chạy (theo bản so) của mục lúc so, vd "weekly-pass-35"; "" khi mục không có đợt đang chạy.</summary>
        public string RunningEventIdBefore { get; }

        /// <summary>Id mà bản ghi đang chạy sẽ theo sau thay đổi (vd "pass-35"); "" nếu đợt đó biến mất khỏi lịch.</summary>
        public string RunningEventIdAfter { get; }

        /// <summary>Giờ kết thúc của đợt đang chạy theo bản so — bản ghi người chơi vẫn khép ở giờ này nếu mất đợt.</summary>
        public DateTime? RunningEventEndUtc { get; }

        /// <summary>Mục nháp bị bộ biên dịch bỏ — game sẽ không thấy; hàng này phải được sửa chứ không phải "đã xem".</summary>
        public bool WillBeDropped { get; }

        /// <summary>
        /// Nội dung JSON-tương-đương của mục nháp (mục đã xoá: của mục cũ), dùng làm khoá "Đã xem" — đổi nội dung là
        /// Toggle tự tắt. Gồm configKey HIỆU LỰC: đổi mặc định của loại làm đoạn JSON của đợt kế thừa đổi theo.
        /// </summary>
        public string Fingerprint { get; }

        public override string ToString() => Kind + " " + ItemKind + " '" + ItemId + "' (" + Consequence + ")";
    }
}
