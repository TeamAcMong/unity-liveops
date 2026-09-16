namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Shell — bản tiếng Việt là bản gốc (chép nguyên văn từ <c>LiveOpsHubStrings.Shell.cs</c>, không đổi một ký tự),
    /// bản tiếng Anh do gói dịch G-I18N bước 2 điền vào chỗ <c>english: null</c>. Hai ngôn ngữ nằm trong cùng một lệnh đăng ký để
    /// chỗ giữ chỗ <c>{0}</c>/<c>{1}</c> soát được bằng mắt và không bao giờ thêm khoá ở bản này mà quên bản kia.
    /// Comment "vì sao" của từng câu ở lại <c>LiveOpsHubStrings.Shell.cs</c> cạnh property — đó là chỗ người đọc code tìm tới.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterShell(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.ShellWindowTitle),
                vietnamese: "LiveOps Hub",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellReduceMotionMenu),
                vietnamese: "Tắt chuyển động",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ShellOverviewTitle),
                vietnamese: "Tổng quan",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellOverviewSubtitle),
                vietnamese: "Lịch đang kẹt ở đâu và việc gì cần làm trước khi đăng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellEventTypesTitle),
                vietnamese: "Loại event",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellEventTypesSubtitle),
                vietnamese: "Mỗi loại một làn trên lịch: màu, cách vào, config mặc định",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellCalendarTitle),
                vietnamese: "Lịch",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellCalendarSubtitle),
                vietnamese: "Mỗi loại một làn · kéo để dời · UTC là giờ thật của dữ liệu",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRecurringRulesTitle),
                vietnamese: "Luật lặp",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRecurringRulesSubtitle),
                vietnamese: "Đợt tự sinh theo chu kỳ — id, giờ mở và giờ khép",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellValidationTitle),
                vietnamese: "Kiểm lịch",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ShellValidationSubtitle),
                vietnamese: "Game sẽ làm gì với lịch — nhóm theo hậu quả với người chơi",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ShellExportTitle),
                vietnamese: "Xuất JSON",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellExportSubtitle),
                vietnamese: "JSON cho key liveops_calendar, so với lần đăng",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.InterimPlaceholderReason),
                vietnamese: "Màn này chưa dựng ở bản dev",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ShellRailCaption),
                vietnamese: "ĐƯỜNG ĐI CỦA LỊCH",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRailNotMeasuredBadge),
                vietnamese: "chưa kiểm",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRailStaleBadgePrefix),
                vietnamese: "cũ · ",
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.ShellRailPartSeparator), " · ");
            table.Add(nameof(LiveOpsHubStrings.ShellRailDroppedCountFormat),
                vietnamese: "{0} bị bỏ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRailProgressLostCountFormat),
                vietnamese: "{0} mất tiến độ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRailShouldReviewCountFormat),
                vietnamese: "{0} nên xem",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRailNotMeasuredCountFormat),
                vietnamese: "{0} chưa kiểm",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRailStaleTooltipPrefixFormat),
                vietnamese: "Kết quả kiểm lúc {0}, lịch đã đổi sau đó: ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRailStaleTooltipPrefixWithoutTime),
                vietnamese: "Kết quả kiểm đã cũ, lịch đã đổi sau đó: ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRailBlockerTitleFormat),
                vietnamese: "Dừng ở {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRailBlockerDroppedDetailFormat),
                vietnamese: "{0} đợt sẽ bị game bỏ khi đọc lịch.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRailBlockerStaleDetailFormat),
                vietnamese: "Kết quả cũ có {0} đợt bị bỏ — F5 để kiểm lại.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRailHealthCountMismatchFormat),
                vietnamese: "Rail nhận {0} health cho {1} màn — mỗi màn đúng một health theo cùng thứ tự registry.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ShellHealthThrewReason),
                vietnamese: "Kiểm của màn này ném lỗi — xem Console",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellHealthThrewLogFormat),
                vietnamese: "LiveOps Hub: GetHealth của một màn ném {0}: {1} — màn hiện NotMeasured tới lần tính kế.\n{2}",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ShellSectionFailureTitleFormat),
                vietnamese: "Không hiện được màn {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellSectionFailureFootnoteFormat),
                vietnamese: "Các màn khác vẫn mở được từ rail. Health của {0} là NotMeasured \"{1}\".",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellSectionFailedHealthReason),
                vietnamese: "Màn ném lỗi khi dựng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellSectionThrewLogFormat),
                vietnamese: "LiveOps Hub: màn '{0}' ném lỗi khi dựng — cửa sổ hiện card lỗi, các màn khác vẫn mở được.\n{1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellNullViewMessageFormat),
                vietnamese: "CreateView của màn '{0}' trả null — màn phải trả một VisualElement.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellViewStateThrewLogFormat),
                vietnamese: "LiveOps Hub: màn '{0}' ném khi chụp trạng thái view — bỏ trạng thái của màn này: {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellCopyErrorButton),
                vietnamese: "Copy lỗi",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellRetryBuildButton),
                vietnamese: "Thử dựng lại",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ShellMissingLayoutTitleFormat),
                vietnamese: "Không tải được bố cục LiveOps Hub: thiếu {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellMissingLayoutBodyFormat),
                vietnamese: "Tìm ở {0}. Package có thể đang import dở hoặc thư mục Editor bị xoá.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellMissingElementsTitle),
                vietnamese: "Không dựng được khung LiveOps Hub: bố cục thiếu element",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellMissingElementsBodyFormat),
                vietnamese: "Thiếu: {0}. Kiểm {1} — tên element phải khớp LiveOpsHubPaths.RequiredShellElementNames.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellOpenPackageFolderButton),
                vietnamese: "Mở thư mục package",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ShellMissingStyleSheetWarningFormat),
                vietnamese: "LiveOps Hub: không nạp được stylesheet {0} — khung vẫn chạy nhưng thiếu style.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ShellCompilingNote),
                vietnamese: "Unity đang biên dịch — hub sẽ dựng lại sau khi xong",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ShellUnknownSectionWarningFormat),
                vietnamese: "LiveOps Hub: không có màn id '{0}'. Id hợp lệ ở LiveOpsHubSections.Ids — hiện Tổng quan.",
                english: null);
        }
    }
}
