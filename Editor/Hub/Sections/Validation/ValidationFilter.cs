using System;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bộ lọc của màn Kiểm lịch (mục 7.5): nhóm hậu quả (tab), loại event (ToolbarMenu) và chữ tìm (ô 190px). Lớp thuần —
    /// không đụng UIElements — nên test lọc chạy ở nhóm Logic và điều hướng từ rail/palette chỉ là dựng một bộ lọc khác.
    /// <para>
    /// Bất biến: cùng một bộ lọc đi qua bus điều hướng, được section giữ trong trạng thái view và được test so sánh sau đó;
    /// sửa tại chỗ sẽ làm lệch cả ba (cùng lý do với <see cref="LiveOpsHubNavigation"/>).
    /// </para>
    /// </summary>
    internal sealed class ValidationFilter
    {
        /// <summary>Không lọc gì — trạng thái mặc định khi mở màn (tab "Tất cả").</summary>
        internal static readonly ValidationFilter All = new ValidationFilter(LiveOpsHubNavigation.FilterNone, string.Empty, string.Empty);

        internal ValidationFilter(string consequenceFilter, string eventType, string searchText)
        {
            ConsequenceFilter = consequenceFilter ?? LiveOpsHubNavigation.FilterNone;
            EventType = eventType ?? string.Empty;
            SearchText = searchText ?? string.Empty;
        }

        /// <summary>Một hằng <c>LiveOpsHubNavigation.Filter…</c>; "" = mọi nhóm.</summary>
        internal string ConsequenceFilter { get; }

        /// <summary>"" = mọi loại event.</summary>
        internal string EventType { get; }

        internal string SearchText { get; }

        internal bool IsUnfiltered => ConsequenceFilter.Length == 0 && EventType.Length == 0 && SearchText.Length == 0;

        internal ValidationFilter WithConsequence(string consequenceFilter)
        {
            return new ValidationFilter(consequenceFilter, EventType, SearchText);
        }

        internal ValidationFilter WithEventType(string eventType)
        {
            return new ValidationFilter(ConsequenceFilter, eventType, SearchText);
        }

        internal ValidationFilter WithSearch(string searchText)
        {
            return new ValidationFilter(ConsequenceFilter, EventType, searchText);
        }

        /// <summary>Tên hằng bộ lọc của một hậu quả — dùng để dựng tab và để so với <see cref="ConsequenceFilter"/>.</summary>
        internal static string FilterOf(LiveEventCalendarConsequence consequence)
        {
            switch (consequence)
            {
                case LiveEventCalendarConsequence.Dropped: return LiveOpsHubNavigation.FilterDropped;
                case LiveEventCalendarConsequence.ProgressLost: return LiveOpsHubNavigation.FilterProgressLost;
                case LiveEventCalendarConsequence.ShouldReview: return LiveOpsHubNavigation.FilterShouldReview;
                default: return LiveOpsHubNavigation.FilterNone;
            }
        }

        /// <summary>
        /// Phát hiện có lọt qua không. <paramref name="eventTypeOfFinding"/> do người gọi tra từ tài liệu (lớp này không đọc
        /// tài liệu để giữ tính thuần); "" khi phát hiện không thuộc loại nào (vd bản remote).
        /// </summary>
        internal bool Accepts(LiveEventCalendarFinding finding, string eventTypeOfFinding)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            if (ConsequenceFilter.Length > 0 && !string.Equals(ConsequenceFilter, FilterOf(finding.Consequence), StringComparison.Ordinal)) return false;
            if (EventType.Length > 0 && !string.Equals(EventType, eventTypeOfFinding ?? string.Empty, StringComparison.Ordinal)) return false;
            if (SearchText.Length == 0) return true;
            return Contains(finding.TargetId) || Contains(finding.RuleId) || Contains(finding.RelatedId);
        }

        /// <summary>
        /// Kết quả luật không đo được (hàng nhóm Chưa kiểm). Không có đích nên bộ lọc loại event luôn giấu nó đi: lọc theo
        /// loại là câu hỏi "loại này có vấn đề gì", mà một luật chưa chạy được thì không trả lời được câu đó.
        /// </summary>
        internal bool AcceptsRuleResult(LiveEventCalendarRuleResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (ConsequenceFilter.Length > 0 && !string.Equals(ConsequenceFilter, LiveOpsHubNavigation.FilterNotMeasured, StringComparison.Ordinal)) return false;
            if (EventType.Length > 0) return false;
            return SearchText.Length == 0 || Contains(result.RuleId);
        }

        private bool Contains(string value)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
