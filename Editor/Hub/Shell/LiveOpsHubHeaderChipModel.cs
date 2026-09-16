using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ hai chip của header 26 px (8.3, [FD §3.3]) — model thuần, test được ở category Logic. Chip trái là asset đang mở; chip
    /// phải là "nháp", và nó có ĐÚNG bốn dạng (<see cref="DraftChipForm"/>) vì hub nói hai con số khác nhau về hai thứ khác nhau:
    /// <list type="bullet">
    /// <item><b>chưa lưu</b> = nháp trong Editor so với file trên đĩa — con số của câu hỏi lúc đóng cửa sổ.</item>
    /// <item><b>khác bản đã đăng</b> = nháp so với dấu đã đăng — con số của màn Xuất JSON.</item>
    /// </list>
    /// Trộn hai con số là lỗi đã có tên ([FD §3.3]): "5 thay đổi" trong hộp đóng cửa sổ sẽ làm người dùng lưu nhầm kỳ vọng.
    /// <para>
    /// Tab Unity có "*" <b>khi và chỉ khi</b> dạng (a) — <see cref="HasUnsavedChanges"/> là cùng một nguồn với dạng chip, nên
    /// không bao giờ có cảnh "tab sạch mà chip nói chưa lưu".
    /// </para>
    /// </summary>
    internal sealed class LiveOpsHubHeaderChipModel
    {
        /// <summary>Chưa có asset lịch — chip nháp chỉ còn một chữ dẫn về Tổng quan.</summary>
        public const int FormNoAsset = 0;

        /// <summary>(a) Còn thay đổi chưa lưu: hai phần "Chưa lưu · ⌘S" │ "N khác bản đã đăng".</summary>
        public const int FormUnsaved = 1;

        /// <summary>(b) Đã lưu nhưng khác bản đã đăng.</summary>
        public const int FormDiffersFromPublished = 2;

        /// <summary>(c) Chưa từng ghi dấu đã đăng — vòng rỗng, không phải trạng thái xấu.</summary>
        public const int FormNeverPublished = 3;

        /// <summary>Đã lưu và khớp dấu đã đăng.</summary>
        public const int FormMatchesPublished = 4;

        /// <summary>Số id nêu tên trong câu hỏi lúc đóng cửa sổ; phần còn lại gom thành "và N mục khác".</summary>
        internal const int MaximumListedIds = 3;

        private LiveOpsHubHeaderChipModel(string assetChipText, string assetChipTooltip, int draftChipForm, string draftLeftText,
            string draftRightText, bool hasUnsavedChanges, string saveChangesMessage)
        {
            AssetChipText = assetChipText ?? string.Empty;
            AssetChipTooltip = assetChipTooltip ?? string.Empty;
            DraftChipForm = draftChipForm;
            DraftLeftText = draftLeftText ?? string.Empty;
            DraftRightText = draftRightText ?? string.Empty;
            HasUnsavedChanges = hasUnsavedChanges;
            SaveChangesMessage = saveChangesMessage ?? string.Empty;
        }

        /// <param name="session">null = cửa sổ chưa có phiên (khung trần W2) — chip về dạng "chưa có lịch".</param>
        /// <param name="saveKeyLabel">Nhãn phím Lưu thật ("⌘S"); "" = không gán phím → chip chỉ ghi "Chưa lưu".</param>
        public static LiveOpsHubHeaderChipModel Build(LiveOpsHubCalendarSession session, LiveOpsHubFormat format, string saveKeyLabel)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (session == null || session.Asset == null)
            {
                return new LiveOpsHubHeaderChipModel(LiveOpsHubStrings.ShellChipNoCalendar, string.Empty, FormNoAsset,
                    LiveOpsHubStrings.ShellChipNoCalendarDraft, string.Empty, false, string.Empty);
            }

            string assetText = session.AssetFileName;
            string assetTooltip = session.AssetPath;
            bool hasUnsaved = session.HasUnsavedChanges;
            PublishedCalendarStamp stamp = session.Publish.ActiveStamp;
            int publishedChangeCount = stamp == null ? 0 : session.Publish.PublishedDiff.ChangeCount;

            if (hasUnsaved)
            {
                string left = saveKeyLabel.Length == 0
                    ? LiveOpsHubStrings.ShellChipUnsaved
                    : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellChipUnsavedWithKeyFormat, saveKeyLabel);
                // Phần phải chỉ có khi có dấu để so: không dấu thì "0 khác bản đã đăng" sẽ đọc thành "đã khớp bản đăng" — sai hẳn nghĩa.
                string right = stamp == null
                    ? string.Empty
                    : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellChipPublishedDiffFormat, publishedChangeCount);
                return new LiveOpsHubHeaderChipModel(assetText, assetTooltip, FormUnsaved, left, right, true,
                    BuildSaveChangesMessage(session));
            }

            if (stamp == null)
            {
                return new LiveOpsHubHeaderChipModel(assetText, assetTooltip, FormNeverPublished,
                    LiveOpsHubStrings.ShellChipNeverPublished, string.Empty, false, string.Empty);
            }

            if (publishedChangeCount > 0)
            {
                return new LiveOpsHubHeaderChipModel(assetText, assetTooltip, FormDiffersFromPublished,
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellChipDiffersFromPublishedFormat, publishedChangeCount),
                    string.Empty, false, string.Empty);
            }

            DateTime publishedUtc;
            string matched = stamp.TryGetPublishedUtc(out publishedUtc)
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ShellChipMatchesPublishedFormat, format.ShortDateTime(publishedUtc))
                : LiveOpsHubStrings.ShellChipMatchesPublishedWithoutTime;
            return new LiveOpsHubHeaderChipModel(assetText, assetTooltip, FormMatchesPublished, matched, string.Empty, false, string.Empty);
        }

        /// <summary>"Main.asset" | "Chưa có lịch".</summary>
        public string AssetChipText { get; }

        /// <summary>Đường dẫn asset (tooltip chip trái); "" khi chưa có asset.</summary>
        public string AssetChipTooltip { get; }

        /// <summary>Một trong <see cref="FormNoAsset"/> … <see cref="FormMatchesPublished"/>.</summary>
        public int DraftChipForm { get; }

        public string DraftLeftText { get; }

        /// <summary>"" = chip nháp chỉ có một phần (không vẽ vạch ngăn).</summary>
        public string DraftRightText { get; }

        /// <summary>= tab có "*"; đúng bằng điều kiện của dạng (a).</summary>
        public bool HasUnsavedChanges { get; }

        /// <summary>Câu Unity hỏi khi đóng tab có "*"; "" ở mọi dạng khác.</summary>
        public string SaveChangesMessage { get; }

        /// <summary>
        /// Câu hỏi lúc đóng cửa sổ: tên asset, số thay đổi CHƯA LƯU và vài id ngắn để người dùng nhận ra việc mình vừa làm.
        /// </summary>
        internal static string BuildSaveChangesMessage(LiveOpsHubCalendarSession session)
        {
            LiveEventCalendarDiffResult diff = session.UnsavedDiff;

            // PD-22: "chưa lưu" rộng hơn diff hậu quả (dấu đã đăng, cảnh báo đã bỏ qua, thứ tự mục). Diff rỗng thì KHÔNG có con số
            // thật để nêu — câu hỏi nói đúng cái nó biết thay vì bịa "1 thay đổi".
            if (diff.ChangeCount == 0)
            {
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesSaveChangesMessageNoCountFormat, session.AssetFileName);
            }

            // "… và N mục khác" đếm theo MỤC (id duy nhất), không theo số thay đổi: hai thay đổi trên cùng một đợt không phải hai mục.
            List<string> distinctItemIds = new List<string>();
            foreach (LiveEventCalendarChange change in diff.Changes)
            {
                if (!string.IsNullOrEmpty(change.ItemId) && !distinctItemIds.Contains(change.ItemId)) distinctItemIds.Add(change.ItemId);
            }
            int listedCount = Math.Min(distinctItemIds.Count, MaximumListedIds);
            string list = listedCount == 0
                ? LiveOpsHubStrings.ServicesSaveChangesDocumentFields
                : string.Join(LiveOpsHubStrings.ServicesHealthItemSeparator, distinctItemIds.GetRange(0, listedCount).ToArray());
            if (distinctItemIds.Count > listedCount)
            {
                list = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesSaveChangesMoreFormat, list,
                    distinctItemIds.Count - listedCount);
            }
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ServicesSaveChangesMessageFormat, session.AssetFileName,
                diff.ChangeCount, list);
        }
    }
}
