using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Một hàng của card diff [SD2 §3.8] — hai dòng chữ, một ký hiệu, một hành động bên phải.</summary>
    internal sealed class ExportDiffRow
    {
        internal ExportDiffRow(LiveEventCalendarChange change, string symbolText, string titleText, string consequenceText,
            bool isReviewed, bool isReviewRequired, bool hasReviewToggle)
        {
            Change = change;
            SymbolText = symbolText ?? string.Empty;
            TitleText = titleText ?? string.Empty;
            ConsequenceText = consequenceText ?? string.Empty;
            IsReviewed = isReviewed;
            IsReviewRequired = isReviewRequired;
            HasReviewToggle = hasReviewToggle;
        }

        public LiveEventCalendarChange Change { get; }
        public string SymbolText { get; }
        public string TitleText { get; }
        public string ConsequenceText { get; }
        public bool IsReviewed { get; }

        /// <summary>Mục "Mất tiến độ" — mang tag "bắt buộc" và đếm vào dòng cổng 5.</summary>
        public bool IsReviewRequired { get; }

        /// <summary>
        /// Hàng Bị bỏ KHÔNG có Toggle: tick không mở khoá gì (cổng dòng 1 vẫn chặn), nên thay bằng link "Sửa ở Kiểm lịch"
        /// [SD2 §3.8]. Mọi hàng còn lại có Toggle.
        /// </summary>
        public bool HasReviewToggle { get; }

        public LiveEventCalendarConsequence Consequence => Change == null ? LiveEventCalendarConsequence.Safe : Change.Consequence;
    }

    /// <summary>Một nhóm hậu quả của card diff: header HOA + số đếm, rồi các hàng theo đúng thứ tự của diff.</summary>
    internal sealed class ExportDiffGroup
    {
        internal ExportDiffGroup(LiveEventCalendarConsequence consequence, string headerText, IReadOnlyList<ExportDiffRow> rows)
        {
            Consequence = consequence;
            HeaderText = headerText ?? string.Empty;
            Rows = rows ?? Array.Empty<ExportDiffRow>();
        }

        public LiveEventCalendarConsequence Consequence { get; }
        public string HeaderText { get; }
        public IReadOnlyList<ExportDiffRow> Rows { get; }
    }

    /// <summary>
    /// Card "So với bản đã đăng" dựng thành dữ liệu: header + meta + chip + nhóm hậu quả + hàng. Thuần, không đụng phiên —
    /// nơi gọi truyền diff, hai tài liệu vừa so (để <see cref="LiveOpsChangeText"/> viết câu có ngữ cảnh, V-22 CC-FT-1) và
    /// một hàm hỏi "mục này đã xem chưa".
    /// <para>
    /// (V-13) Nguồn bản so là Remote thì header đổi chữ và MỌI hàng mất Toggle "Đã xem": "Đã xem" luôn nói về dấu đã đăng,
    /// tick trên một bản so khác sẽ mở khoá cổng bằng thứ không liên quan.
    /// </para>
    /// </summary>
    internal sealed class ExportDiffViewModel
    {
        private static readonly LiveEventCalendarConsequence[] GroupOrder =
        {
            LiveEventCalendarConsequence.Dropped,
            LiveEventCalendarConsequence.ProgressLost,
            LiveEventCalendarConsequence.ShouldReview,
            LiveEventCalendarConsequence.Safe,
        };

        private ExportDiffViewModel(string headerText, string metaText, IReadOnlyList<string> chipTexts, IReadOnlyList<ExportDiffGroup> groups,
            string emptyText, bool hasReviewToggles, int reviewedCount, int reviewableCount, int reviewedRequiredCount, int requiredCount)
        {
            HeaderText = headerText ?? string.Empty;
            MetaText = metaText ?? string.Empty;
            ChipTexts = chipTexts ?? Array.Empty<string>();
            Groups = groups ?? Array.Empty<ExportDiffGroup>();
            EmptyText = emptyText ?? string.Empty;
            HasReviewToggles = hasReviewToggles;
            ReviewedCount = reviewedCount;
            ReviewableCount = reviewableCount;
            ReviewedRequiredCount = reviewedRequiredCount;
            RequiredCount = requiredCount;
        }

        public string HeaderText { get; }

        /// <summary>"Đã xem 0/3 · bắt buộc 0/1" — chỉ đếm mục ĐÁNH DẤU ĐƯỢC [SD2 §3.8]; "" khi không có mục nào như thế.</summary>
        public string MetaText { get; }

        public IReadOnlyList<string> ChipTexts { get; }
        public IReadOnlyList<ExportDiffGroup> Groups { get; }

        /// <summary>Câu thay cho danh sách khi không có thay đổi (f) hay chưa có dấu (g); "" khi có hàng để vẽ.</summary>
        public string EmptyText { get; }

        /// <summary>(V-13) false khi đang so với bản remote đã dán — card vẽ hàng nhưng không vẽ Toggle nào.</summary>
        public bool HasReviewToggles { get; }

        public int ReviewedCount { get; }
        public int ReviewableCount { get; }
        public int ReviewedRequiredCount { get; }
        public int RequiredCount { get; }

        public bool IsEmpty => Groups.Count == 0;

        public int RowCount
        {
            get
            {
                int count = 0;
                foreach (ExportDiffGroup group in Groups) count += group.Rows.Count;
                return count;
            }
        }

        public static ExportDiffViewModel Build(LiveEventCalendarDiffResult diff, LiveOpsChangeTextContext context, LiveOpsHubFormat format,
            LiveOpsHubCompareSource compareSource, string headerText, string emptyText, Func<LiveEventCalendarChange, bool> isReviewed)
        {
            if (format == null) throw new ArgumentNullException(nameof(format), LiveOpsHubStrings.ExportGateErrorFormatMissing);

            bool hasToggles = compareSource != LiveOpsHubCompareSource.Remote;
            List<ExportDiffGroup> groups = new List<ExportDiffGroup>();
            int reviewed = 0;
            int reviewable = 0;
            int reviewedRequired = 0;
            int required = 0;

            if (diff != null)
            {
                foreach (LiveEventCalendarConsequence consequence in GroupOrder)
                {
                    List<ExportDiffRow> rows = new List<ExportDiffRow>();
                    foreach (LiveEventCalendarChange change in diff.Changes)
                    {
                        if (change.Consequence != consequence) continue;
                        // "Xoá 0" chỉ đếm ở header, không vẽ thành một dòng thay đổi [SD2 §3.8].
                        if (change.Kind == LiveEventCalendarChangeKind.Removed || change.Kind == LiveEventCalendarChangeKind.Kept) continue;

                        bool rowReviewed = isReviewed != null && isReviewed(change);
                        bool rowHasToggle = hasToggles && consequence != LiveEventCalendarConsequence.Dropped;
                        rows.Add(new ExportDiffRow(change, SymbolOf(change), LiveOpsChangeText.RowText(change, context, format),
                            LiveOpsChangeText.ConsequenceSentence(change, context, format), rowReviewed, change.IsReviewRequired, rowHasToggle));

                        if (rowHasToggle)
                        {
                            reviewable++;
                            if (rowReviewed) reviewed++;
                        }
                        if (change.IsReviewRequired)
                        {
                            required++;
                            if (rowReviewed) reviewedRequired++;
                        }
                    }
                    if (rows.Count == 0) continue;
                    groups.Add(new ExportDiffGroup(consequence, string.Format(CultureInfo.InvariantCulture, GroupHeaderFormatOf(consequence),
                        format.Integer(rows.Count)), rows));
                }
            }

            string meta = reviewable > 0
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportDiffMetaFormat, format.Integer(reviewed),
                    format.Integer(reviewable), format.Integer(reviewedRequired), format.Integer(required))
                : string.Empty;

            return new ExportDiffViewModel(headerText, meta, BuildChips(diff, format), groups, groups.Count == 0 ? emptyText : string.Empty,
                hasToggles, reviewed, reviewable, reviewedRequired, required);
        }

        /// <summary>Chip đầu card: bốn số luôn hiện đủ, kể cả "Xoá 0" — người đọc cần biết số đó bằng 0, không phải đoán.</summary>
        private static IReadOnlyList<string> BuildChips(LiveEventCalendarDiffResult diff, LiveOpsHubFormat format)
        {
            int added = diff == null ? 0 : diff.AddedCount;
            int changed = diff == null ? 0 : diff.ChangedCount;
            int removed = diff == null ? 0 : diff.RemovedCount;
            int kept = diff == null ? 0 : diff.KeptCount;
            return new[]
            {
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportDiffChipAddedFormat, format.Integer(added)),
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportDiffChipChangedFormat, format.Integer(changed)),
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportDiffChipRemovedFormat, format.Integer(removed)),
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ExportDiffChipKeptFormat, format.Integer(kept)),
            };
        }

        /// <summary>Ký hiệu mono đầu hàng: Mất tiến độ luôn là "!" dù mục là thêm hay đổi [SD2 §3.8].</summary>
        internal static string SymbolOf(LiveEventCalendarChange change)
        {
            if (change == null) return string.Empty;
            if (change.Consequence == LiveEventCalendarConsequence.ProgressLost) return LiveOpsHubStrings.ExportDiffSymbolProgressLost;
            return change.Kind == LiveEventCalendarChangeKind.Added
                ? LiveOpsHubStrings.ExportDiffSymbolAdded
                : LiveOpsHubStrings.ExportDiffSymbolChanged;
        }

        private static string GroupHeaderFormatOf(LiveEventCalendarConsequence consequence)
        {
            switch (consequence)
            {
                case LiveEventCalendarConsequence.Dropped: return LiveOpsHubStrings.ExportDiffGroupDroppedFormat;
                case LiveEventCalendarConsequence.ProgressLost: return LiveOpsHubStrings.ExportDiffGroupProgressLostFormat;
                case LiveEventCalendarConsequence.ShouldReview: return LiveOpsHubStrings.ExportDiffGroupShouldReviewFormat;
                default: return LiveOpsHubStrings.ExportDiffGroupSafeFormat;
            }
        }
    }
}
