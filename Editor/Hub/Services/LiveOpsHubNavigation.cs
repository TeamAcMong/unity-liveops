using System;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Section nhận điều hướng có tham số (lọc "Bị bỏ", chọn đợt, chớp hàng luật, nguồn bản so).</summary>
    internal interface IHubSectionNavigation
    {
        void ApplyNavigation(LiveOpsHubNavigation navigation);
    }

    /// <summary>Section có danh sách phát hiện đi được bằng F8 / Shift F8; trả false khi không còn phát hiện theo hướng đó.</summary>
    internal interface IHubSectionFindings
    {
        bool TryMoveToFinding(int direction);
    }

    /// <summary>(V-13) Bản so của pane So với (Lịch) và card diff (Xuất JSON). None = giữ nguyên nguồn đang chọn.</summary>
    internal enum LiveOpsHubCompareSource
    {
        None = 0,
        Published = 1,
        Disk = 2,
        Remote = 3,
    }

    /// <summary>
    /// Một yêu cầu điều hướng — bất biến, dựng bằng <see cref="To"/> rồi <c>With…</c>. Bất biến vì cùng một đối tượng đi qua
    /// bus, được cửa sổ giữ lại để áp khi section dựng xong, và có thể được test so sánh sau đó: sửa tại chỗ sẽ làm lệch cả ba.
    /// </summary>
    internal sealed class LiveOpsHubNavigation
    {
        public const string FilterNone = "";
        public const string FilterDropped = "dropped";
        public const string FilterProgressLost = "progress-lost";
        public const string FilterShouldReview = "should-review";
        public const string FilterNotMeasured = "not-measured";

        private LiveOpsHubNavigation(string sectionId, string filterConsequence, string ruleId, string entryKey, string eventType,
            bool flash, LiveOpsHubCompareSource compareSource)
        {
            SectionId = sectionId;
            FilterConsequence = filterConsequence;
            RuleId = ruleId;
            EntryKey = entryKey;
            EventType = eventType;
            Flash = flash;
            CompareSource = compareSource;
        }

        public string SectionId { get; }

        /// <summary>Một hằng <c>Filter…</c>; "" = không đổi bộ lọc.</summary>
        public string FilterConsequence { get; }

        public string RuleId { get; }
        public string EntryKey { get; }
        public string EventType { get; }

        /// <summary>Chớp hàng/làn đích một nhịp để mắt tìm được chỗ vừa nhảy tới.</summary>
        public bool Flash { get; }

        public LiveOpsHubCompareSource CompareSource { get; }

        public static LiveOpsHubNavigation To(string sectionId)
        {
            if (string.IsNullOrEmpty(sectionId)) throw new ArgumentException(LiveOpsHubStrings.KitErrorSectionIdEmpty, nameof(sectionId));
            return new LiveOpsHubNavigation(sectionId, FilterNone, string.Empty, string.Empty, string.Empty, false, LiveOpsHubCompareSource.None);
        }

        public LiveOpsHubNavigation WithFilter(string consequence)
        {
            string value = consequence ?? FilterNone;
            // Danh sách đóng: chuỗi gõ sai ở một màn sẽ lọc ra danh sách rỗng mà không ai biết — ném ngay ở chỗ dựng.
            if (!IsKnownFilter(value))
            {
                throw new ArgumentException(LiveOpsHubStrings.KitErrorUnknownFilterConsequence + value, nameof(consequence));
            }
            return new LiveOpsHubNavigation(SectionId, value, RuleId, EntryKey, EventType, Flash, CompareSource);
        }

        public LiveOpsHubNavigation WithRule(string ruleId)
        {
            return new LiveOpsHubNavigation(SectionId, FilterConsequence, ruleId ?? string.Empty, EntryKey, EventType, Flash, CompareSource);
        }

        public LiveOpsHubNavigation WithEntry(string entryKey)
        {
            return new LiveOpsHubNavigation(SectionId, FilterConsequence, RuleId, entryKey ?? string.Empty, EventType, Flash, CompareSource);
        }

        public LiveOpsHubNavigation WithEventType(string eventType, bool flash)
        {
            return new LiveOpsHubNavigation(SectionId, FilterConsequence, RuleId, EntryKey, eventType ?? string.Empty, flash, CompareSource);
        }

        /// <summary>(V-13) Lịch: mở pane So với với nguồn này; Xuất JSON: card diff so với nguồn này.</summary>
        public LiveOpsHubNavigation WithCompareSource(LiveOpsHubCompareSource source)
        {
            return new LiveOpsHubNavigation(SectionId, FilterConsequence, RuleId, EntryKey, EventType, Flash, source);
        }

        private static bool IsKnownFilter(string value)
        {
            return string.Equals(value, FilterNone, StringComparison.Ordinal)
                || string.Equals(value, FilterDropped, StringComparison.Ordinal)
                || string.Equals(value, FilterProgressLost, StringComparison.Ordinal)
                || string.Equals(value, FilterShouldReview, StringComparison.Ordinal)
                || string.Equals(value, FilterNotMeasured, StringComparison.Ordinal);
        }
    }
}
