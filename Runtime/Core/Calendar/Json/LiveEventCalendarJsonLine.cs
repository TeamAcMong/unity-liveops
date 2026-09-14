namespace DreamTech.LiveOps
{
    /// <summary>
    /// Một dòng của JSON đã định dạng kèm mục của tài liệu mà dòng đó thuộc về. Vì sao cần: lề lỗi, tint thay đổi và
    /// "dòng 54 · bị bỏ" của JSON viewer phải chỉ đúng dòng của đúng mục — dò lại bằng cách tìm chuỗi trong văn bản sẽ
    /// nhầm khi hai đợt trùng id hoặc trùng giờ.
    /// </summary>
    public sealed class LiveEventCalendarJsonLine
    {
        public LiveEventCalendarJsonLine(int number, string text, LiveEventCalendarItemKind? itemKind, string itemKey, string fieldName)
        {
            Number = number;
            Text = text ?? string.Empty;
            ItemKind = itemKind;
            ItemKey = itemKey ?? string.Empty;
            FieldName = fieldName ?? string.Empty;
        }

        /// <summary>1-based — đúng số dòng hiện ở cột số dòng của viewer và trong câu "Dòng N".</summary>
        public int Number { get; }

        /// <summary>Nội dung dòng, không gồm LF.</summary>
        public string Text { get; }

        /// <summary><c>null</c> với dòng của gốc (<c>{</c>, <c>"version"</c>, <c>"events": [</c>, <c>]</c>…).</summary>
        public LiveEventCalendarItemKind? ItemKind { get; }

        /// <summary>
        /// <see cref="FixedLiveEventEntry.EntryKey"/> với đợt cố định, <see cref="RecurringLiveEventRule.EventType"/> với luật
        /// lặp; "" với dòng của gốc.
        /// </summary>
        public string ItemKey { get; }

        /// <summary>
        /// Tên key JSON của dòng (<c>"endUtc"</c>…). "" với dòng <c>{</c> / <c>}</c> của mục và dòng đóng của gốc; dòng mở
        /// mảng của gốc mang tên mảng (<c>"recurring"</c>, <c>"events"</c>).
        /// </summary>
        public string FieldName { get; }
    }
}
