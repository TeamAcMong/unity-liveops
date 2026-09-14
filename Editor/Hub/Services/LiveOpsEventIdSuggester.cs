using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Id đề xuất khi Thêm đợt / Nhân bản / đề xuất đổi tên đợt trùng id (PD-20). Người dùng vẫn sửa được — mục tiêu là id
    /// điền sẵn đúng thói quen đặt tên của team (09a → 09b, hunt-0914 → hunt-0921) để ít phải gõ lại, và không bao giờ trùng
    /// id đang có (trùng id = game bỏ đợt sau).
    /// <para>
    /// Luật, theo thứ tự: lấy id gốc (<c>basedOnEventId</c>, không có thì id đợt cố định cùng loại có giờ bắt đầu gần
    /// <c>startUtc</c> nhất); hậu tố <c>\d+[a-z]</c> → chữ kế; hậu tố <c>-MMdd</c> (tháng/ngày hợp lệ) → <c>-MMdd</c> của ngày bắt
    /// đầu mới; hậu tố <c>\d+</c> → +1 giữ độ rộng; không khớp → <c>id-2</c>; trùng thì tăng tiếp theo cùng mẫu. Loại chưa có
    /// đợt nào thì gốc là <c>loại-MMdd</c> theo mẫu thiết kế (<c>hunt-0921</c>).
    /// </para>
    /// </summary>
    internal static class LiveOpsEventIdSuggester
    {
        /// <summary>Trần số lần thử khi tìm id chưa dùng — tài liệu thật không có hàng nghìn id cùng gốc; trần chặn vòng vô hạn.</summary>
        private const int MaximumAttempts = 10000;

        private const string MonthDayFormat = "MMdd";

        public static string Suggest(LiveEventCalendarDocument document, string eventType, DateTime startUtc, string basedOnEventId)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            HashSet<string> existingIds = CollectExistingIds(document);
            string monthDay = startUtc.ToString(MonthDayFormat, CultureInfo.InvariantCulture);
            string baseId = string.IsNullOrEmpty(basedOnEventId) ? FindNearestSameTypeId(document, eventType, startUtc) : basedOnEventId;
            if (baseId == null)
            {
                return AppendCounterUntilFree(eventType + "-" + monthDay, existingIds, includeUnsuffixed: true);
            }

            if (TrySuggestNextLetter(baseId, existingIds, out string letterSuggestion)) return letterSuggestion;
            if (HasMonthDaySuffix(baseId))
            {
                return AppendCounterUntilFree(baseId.Substring(0, baseId.Length - MonthDayFormat.Length) + monthDay, existingIds, includeUnsuffixed: true);
            }
            if (TrySuggestNextNumber(baseId, existingIds, out string numberSuggestion)) return numberSuggestion;
            return AppendCounterUntilFree(baseId, existingIds, includeUnsuffixed: false);
        }

        private static HashSet<string> CollectExistingIds(LiveEventCalendarDocument document)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<FixedLiveEventEntry> fixedEvents = document.FixedEvents;
            for (int index = 0; index < fixedEvents.Count; index++) ids.Add(fixedEvents[index].EventId);
            return ids;
        }

        /// <summary>Đợt cùng loại có giờ bắt đầu gần nhất; bằng nhau thì đợt đứng trước; không đọc được giờ thì lấy đợt cuối cùng loại.</summary>
        private static string FindNearestSameTypeId(LiveEventCalendarDocument document, string eventType, DateTime startUtc)
        {
            string nearestId = null;
            long nearestDistance = long.MaxValue;
            string lastUnreadableId = null;
            IReadOnlyList<FixedLiveEventEntry> fixedEvents = document.FixedEvents;
            for (int index = 0; index < fixedEvents.Count; index++)
            {
                FixedLiveEventEntry entry = fixedEvents[index];
                if (!string.Equals(entry.EventType, eventType, StringComparison.Ordinal) || entry.EventId.Length == 0) continue;
                if (!entry.TryGetStartUtc(out DateTime entryStartUtc))
                {
                    lastUnreadableId = entry.EventId;
                    continue;
                }
                long distance = Math.Abs(entryStartUtc.Ticks - startUtc.Ticks);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestId = entry.EventId;
                }
            }
            return nearestId ?? lastUnreadableId;
        }

        private static bool TrySuggestNextLetter(string baseId, HashSet<string> existingIds, out string suggestion)
        {
            suggestion = null;
            int lastIndex = baseId.Length - 1;
            if (lastIndex < 1 || baseId[lastIndex] < 'a' || baseId[lastIndex] > 'z' || !IsDigit(baseId[lastIndex - 1])) return false;
            string stem = baseId.Substring(0, lastIndex);
            for (char letter = (char)(baseId[lastIndex] + 1); letter <= 'z'; letter++)
            {
                string candidate = stem + letter;
                if (!existingIds.Contains(candidate))
                {
                    suggestion = candidate;
                    return true;
                }
            }
            // Hết chữ (…z): không đoán sang "aa" — rơi về mẫu id-2 để người dùng tự thấy và đổi.
            return false;
        }

        private static bool HasMonthDaySuffix(string baseId)
        {
            int length = baseId.Length;
            if (length < MonthDayFormat.Length + 1 || baseId[length - MonthDayFormat.Length - 1] != '-') return false;
            for (int index = length - MonthDayFormat.Length; index < length; index++)
            {
                if (!IsDigit(baseId[index])) return false;
            }
            int month = (baseId[length - 4] - '0') * 10 + (baseId[length - 3] - '0');
            int day = (baseId[length - 2] - '0') * 10 + (baseId[length - 1] - '0');
            // Chỉ nhận là ngày khi tháng/ngày có thể có thật — "event-9999" là số thứ tự, không phải ngày.
            return month >= 1 && month <= 12 && day >= 1 && day <= 31;
        }

        private static bool TrySuggestNextNumber(string baseId, HashSet<string> existingIds, out string suggestion)
        {
            suggestion = null;
            int digitStart = baseId.Length;
            while (digitStart > 0 && IsDigit(baseId[digitStart - 1])) digitStart--;
            if (digitStart == baseId.Length) return false;
            string stem = baseId.Substring(0, digitStart);
            string digits = baseId.Substring(digitStart);
            for (int attempt = 0; attempt < MaximumAttempts; attempt++)
            {
                digits = IncrementDigits(digits);
                string candidate = stem + digits;
                if (!existingIds.Contains(candidate))
                {
                    suggestion = candidate;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Cộng 1 trên chuỗi chữ số, giữ độ rộng ("09" → "10", "099" → "100", "99" → "100") — không tràn với số rất dài.</summary>
        private static string IncrementDigits(string digits)
        {
            char[] characters = digits.ToCharArray();
            for (int index = characters.Length - 1; index >= 0; index--)
            {
                if (characters[index] != '9')
                {
                    characters[index]++;
                    return new string(characters);
                }
                characters[index] = '0';
            }
            return "1" + new string(characters);
        }

        /// <param name="includeUnsuffixed">true = thử chính <paramref name="stem"/> trước rồi mới -2, -3…</param>
        private static string AppendCounterUntilFree(string stem, HashSet<string> existingIds, bool includeUnsuffixed)
        {
            if (includeUnsuffixed && !existingIds.Contains(stem)) return stem;
            for (int counter = 2; counter < MaximumAttempts; counter++)
            {
                string candidate = stem + "-" + counter.ToString(CultureInfo.InvariantCulture);
                if (!existingIds.Contains(candidate)) return candidate;
            }
            return stem + "-" + MaximumAttempts.ToString(CultureInfo.InvariantCulture);
        }

        private static bool IsDigit(char character) => character >= '0' && character <= '9';
    }
}
