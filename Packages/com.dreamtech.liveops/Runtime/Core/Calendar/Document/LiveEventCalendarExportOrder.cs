using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// (V-6) MỘT nguồn cho thứ tự đợt cố định game sẽ nhận trong JSON đã xuất — bộ ghi JSON, bộ biên dịch tài liệu
    /// nháp (<see cref="LiveEventCalendarCompiler.CompileInExportOrder"/>), validator và asset đều gọi hàm này TRƯỚC
    /// khi biên dịch, để "mục thứ N" và kết quả trùng-id/chồng-giờ khớp đúng cái game sẽ thấy khi đọc JSON đã sắp.
    /// </summary>
    public static class LiveEventCalendarExportOrder
    {
        /// <summary>
        /// Trả tài liệu mới: <see cref="LiveEventCalendarDocument.FixedEvents"/> sắp ổn định theo <c>startUtc</c> đọc
        /// được tăng dần (hoà thì theo thứ tự trong asset); mục có <c>startUtc</c> không đọc được xếp CUỐI, theo thứ
        /// tự asset. <see cref="LiveEventCalendarDocument.RecurringRules"/> và mọi phần khác giữ nguyên. Lũy đẳng:
        /// <c>Apply(Apply(d))</c> luôn bằng <c>Apply(d)</c> (cùng thứ tự <see cref="FixedLiveEventEntry.EntryKey"/>).
        /// </summary>
        public static LiveEventCalendarDocument Apply(LiveEventCalendarDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            List<FixedLiveEventEntry> sorted = SortByExportOrder(document.FixedEvents);
            if (IsSameOrder(document.FixedEvents, sorted)) return document;

            return new LiveEventCalendarDocument(document.RemoteConfigKey, new List<LiveEventTypeDefinition>(document.EventTypes),
                new List<RecurringLiveEventRule>(document.RecurringRules), sorted,
                new List<PublishedCalendarStamp>(document.PublishedStamps), new List<IgnoredCalendarWarning>(document.IgnoredWarnings));
        }

        public static bool IsInExportOrder(LiveEventCalendarDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            List<FixedLiveEventEntry> sorted = SortByExportOrder(document.FixedEvents);
            return IsSameOrder(document.FixedEvents, sorted);
        }

        private static List<FixedLiveEventEntry> SortByExportOrder(IReadOnlyList<FixedLiveEventEntry> fixedEvents)
        {
            // List<T>.Sort không tất định khi hai phần tử "bằng nhau" (không stable) — giữ chỉ số gốc làm khoá hoà,
            // để mục có startUtc không đọc được luôn xếp cuối THEO ĐÚNG thứ tự asset thay vì thứ tự ngẫu nhiên.
            var indexed = new List<KeyValuePair<int, FixedLiveEventEntry>>(fixedEvents.Count);
            for (int index = 0; index < fixedEvents.Count; index++)
            {
                indexed.Add(new KeyValuePair<int, FixedLiveEventEntry>(index, fixedEvents[index]));
            }

            indexed.Sort((left, right) =>
            {
                bool leftReadable = left.Value.TryGetStartUtc(out DateTime leftStart);
                bool rightReadable = right.Value.TryGetStartUtc(out DateTime rightStart);
                if (leftReadable && rightReadable)
                {
                    int byStart = leftStart.CompareTo(rightStart);
                    return byStart != 0 ? byStart : left.Key.CompareTo(right.Key);
                }
                if (leftReadable != rightReadable) return leftReadable ? -1 : 1;
                return left.Key.CompareTo(right.Key);
            });

            var result = new List<FixedLiveEventEntry>(indexed.Count);
            for (int index = 0; index < indexed.Count; index++) result.Add(indexed[index].Value);
            return result;
        }

        private static bool IsSameOrder(IReadOnlyList<FixedLiveEventEntry> original, List<FixedLiveEventEntry> sorted)
        {
            for (int index = 0; index < original.Count; index++)
            {
                if (!ReferenceEquals(original[index], sorted[index])) return false;
            }
            return true;
        }
    }
}
