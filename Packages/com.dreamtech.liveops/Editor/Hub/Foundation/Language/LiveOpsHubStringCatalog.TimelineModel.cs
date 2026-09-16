namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng TimelineModel — bản tiếng Việt là bản gốc (chép nguyên văn từ <c>LiveOpsHubStrings.TimelineModel.cs</c>, không đổi một ký tự),
    /// bản tiếng Anh do gói dịch G-I18N bước 2 điền vào chỗ <c>english: null</c>. Hai ngôn ngữ nằm trong cùng một lệnh đăng ký để
    /// chỗ giữ chỗ <c>{0}</c>/<c>{1}</c> soát được bằng mắt và không bao giờ thêm khoá ở bản này mà quên bản kia.
    /// Comment "vì sao" của từng câu ở lại <c>LiveOpsHubStrings.TimelineModel.cs</c> cạnh property — đó là chỗ người đọc code tìm tới.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterTimelineModel(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.TimelineRecurringLaneMetaFormat),
                vietnamese: "lặp mỗi {0} · chạy {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineFixedLaneMeta),
                vietnamese: "cố định",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineFixedLaneMetaCountFormat),
                vietnamese: "cố định · {0} đợt",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineFixedLaneMetaOutsideRangeFormat),
                vietnamese: "cố định · {0} đợt, ngoài khung",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineFixedLaneMetaEmpty),
                vietnamese: "cố định · chưa có đợt",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineLaneOverlapPolicyMeta),
                vietnamese: "Chồng giờ: giữ đợt sớm hơn",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.TimelineUntypedLaneMeta),
                vietnamese: "chưa ghi loại",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineUntypedLaneMetaCountFormat),
                vietnamese: "chưa ghi loại · {0} đợt",
                english: null);

            table.AddShared(nameof(LiveOpsHubStrings.TimelineStripIdSeparator), "…");
            table.AddShared(nameof(LiveOpsHubStrings.TimelineBarLabelEllipsis), "…");

            table.Add(nameof(LiveOpsHubStrings.TimelineRulerMonthFormat),
                vietnamese: "THÁNG {0} {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineRulerMondayPrefix),
                vietnamese: "T2",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.TimelineDocumentRequired),
                vietnamese: "Timeline cần tài liệu lịch — presenter phải truyền tài liệu nháp (kể cả tài liệu rỗng).",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineRangeInvalid),
                vietnamese: "Khoảng timeline phải có kết thúc sau bắt đầu — khoảng rỗng làm px/giờ vô hạn.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.TimelineTrackWidthInvalid),
                vietnamese: "Bề rộng track phải lớn hơn 0 — layout chưa xong (NaN/0) thì chưa được dựng model.",
                english: null);
        }
    }
}
