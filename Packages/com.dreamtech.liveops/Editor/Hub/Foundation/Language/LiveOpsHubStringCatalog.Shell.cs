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
                english: "LiveOps Hub");
            table.Add(nameof(LiveOpsHubStrings.ShellReduceMotionMenu),
                vietnamese: "Tắt chuyển động",
                english: "Reduce motion");

            table.Add(nameof(LiveOpsHubStrings.ShellOverviewTitle),
                vietnamese: "Tổng quan",
                english: "Overview");
            table.Add(nameof(LiveOpsHubStrings.ShellOverviewSubtitle),
                vietnamese: "Lịch đang kẹt ở đâu và việc gì cần làm trước khi đăng",
                english: "What blocks the calendar and what to do before publishing");
            table.Add(nameof(LiveOpsHubStrings.ShellEventTypesTitle),
                vietnamese: "Loại event",
                english: "Event types");
            table.Add(nameof(LiveOpsHubStrings.ShellEventTypesSubtitle),
                vietnamese: "Mỗi loại một làn trên lịch: màu, cách vào, config mặc định",
                english: "One lane per type: color, entry, default config");
            table.Add(nameof(LiveOpsHubStrings.ShellCalendarTitle),
                vietnamese: "Lịch",
                english: "Calendar");
            table.Add(nameof(LiveOpsHubStrings.ShellCalendarSubtitle),
                vietnamese: "Mỗi loại một làn · kéo để dời · UTC là giờ thật của dữ liệu",
                english: "One lane per type · drag to move · UTC is the real time");
            table.Add(nameof(LiveOpsHubStrings.ShellRecurringRulesTitle),
                vietnamese: "Luật lặp",
                english: "Recurring rules");
            table.Add(nameof(LiveOpsHubStrings.ShellRecurringRulesSubtitle),
                vietnamese: "Đợt tự sinh theo chu kỳ — id, giờ mở và giờ khép",
                english: "Events generated on a cycle — id, open and end time");
            table.Add(nameof(LiveOpsHubStrings.ShellValidationTitle),
                vietnamese: "Kiểm lịch",
                english: "Validation");

            table.Add(nameof(LiveOpsHubStrings.ShellValidationSubtitle),
                vietnamese: "Game sẽ làm gì với lịch — nhóm theo hậu quả với người chơi",
                english: "What the game does with the calendar — by player impact");

            table.Add(nameof(LiveOpsHubStrings.ShellExportTitle),
                vietnamese: "Xuất JSON",
                english: "Export JSON");
            table.Add(nameof(LiveOpsHubStrings.ShellExportSubtitle),
                vietnamese: "JSON cho key liveops_calendar, so với lần đăng",
                english: "JSON for key liveops_calendar, compared to last publish");

            table.Add(nameof(LiveOpsHubStrings.InterimPlaceholderReason),
                vietnamese: "Màn này chưa dựng ở bản dev",
                english: "This section is not built in the dev build");

            table.Add(nameof(LiveOpsHubStrings.ShellRailCaption),
                vietnamese: "ĐƯỜNG ĐI CỦA LỊCH",
                english: "CALENDAR PATH");
            table.Add(nameof(LiveOpsHubStrings.ShellRailNotMeasuredBadge),
                vietnamese: "chưa kiểm",
                english: "not measured");
            table.Add(nameof(LiveOpsHubStrings.ShellRailStaleBadgePrefix),
                vietnamese: "cũ · ",
                english: "stale · ");
            table.AddShared(nameof(LiveOpsHubStrings.ShellRailPartSeparator), " · ");
            table.Add(nameof(LiveOpsHubStrings.ShellRailDroppedCountFormat),
                vietnamese: "{0} bị bỏ",
                english: "{0} dropped");
            table.Add(nameof(LiveOpsHubStrings.ShellRailProgressLostCountFormat),
                vietnamese: "{0} mất tiến độ",
                english: "{0} progress loss");
            table.Add(nameof(LiveOpsHubStrings.ShellRailShouldReviewCountFormat),
                vietnamese: "{0} nên xem",
                english: "{0} should review");
            table.Add(nameof(LiveOpsHubStrings.ShellRailNotMeasuredCountFormat),
                vietnamese: "{0} chưa kiểm",
                english: "{0} not measured");
            table.Add(nameof(LiveOpsHubStrings.ShellRailStaleTooltipPrefixFormat),
                vietnamese: "Kết quả kiểm lúc {0}, lịch đã đổi sau đó: ",
                english: "Check result from {0}, the calendar changed after that: ");
            table.Add(nameof(LiveOpsHubStrings.ShellRailStaleTooltipPrefixWithoutTime),
                vietnamese: "Kết quả kiểm đã cũ, lịch đã đổi sau đó: ",
                english: "Check result is stale, the calendar changed after that: ");
            table.Add(nameof(LiveOpsHubStrings.ShellRailBlockerTitleFormat),
                vietnamese: "Dừng ở {0}",
                english: "Stopped at {0}");
            table.Add(nameof(LiveOpsHubStrings.ShellRailBlockerDroppedDetailFormat),
                vietnamese: "{0} đợt sẽ bị game bỏ khi đọc lịch.",
                english: "The game will drop {0} events when it reads the calendar.");
            table.Add(nameof(LiveOpsHubStrings.ShellRailBlockerStaleDetailFormat),
                vietnamese: "Kết quả cũ có {0} đợt bị bỏ — F5 để kiểm lại.",
                english: "The stale result had {0} events dropped — press F5 to check again.");
            table.Add(nameof(LiveOpsHubStrings.ShellRailHealthCountMismatchFormat),
                vietnamese: "Rail nhận {0} health cho {1} màn — mỗi màn đúng một health theo cùng thứ tự registry.",
                english: "The rail got {0} health values for {1} sections — one health per section, in registry order.");

            table.Add(nameof(LiveOpsHubStrings.ShellHealthThrewReason),
                vietnamese: "Kiểm của màn này ném lỗi — xem Console",
                english: "The check for this section threw — see the Console");
            table.Add(nameof(LiveOpsHubStrings.ShellHealthThrewLogFormat),
                vietnamese: "LiveOps Hub: GetHealth của một màn ném {0}: {1} — màn hiện NotMeasured tới lần tính kế.\n{2}",
                english: "LiveOps Hub: GetHealth of a section threw {0}: {1} — the section shows NotMeasured until the next pass.\n{2}");

            table.Add(nameof(LiveOpsHubStrings.ShellSectionFailureTitleFormat),
                vietnamese: "Không hiện được màn {0}",
                english: "Cannot show the {0} section");
            table.Add(nameof(LiveOpsHubStrings.ShellSectionFailureFootnoteFormat),
                vietnamese: "Các màn khác vẫn mở được từ rail. Health của {0} là NotMeasured \"{1}\".",
                english: "The other sections still open from the rail. Health of {0} is NotMeasured \"{1}\".");
            table.Add(nameof(LiveOpsHubStrings.ShellSectionFailedHealthReason),
                vietnamese: "Màn ném lỗi khi dựng",
                english: "The section threw while building");
            table.Add(nameof(LiveOpsHubStrings.ShellSectionThrewLogFormat),
                vietnamese: "LiveOps Hub: màn '{0}' ném lỗi khi dựng — cửa sổ hiện card lỗi, các màn khác vẫn mở được.\n{1}",
                english: "LiveOps Hub: section '{0}' threw while building — the window shows an error card, the other sections still open.\n{1}");
            table.Add(nameof(LiveOpsHubStrings.ShellNullViewMessageFormat),
                vietnamese: "CreateView của màn '{0}' trả null — màn phải trả một VisualElement.",
                english: "CreateView of section '{0}' returned null — a section must return a VisualElement.");
            table.Add(nameof(LiveOpsHubStrings.ShellViewStateThrewLogFormat),
                vietnamese: "LiveOps Hub: màn '{0}' ném khi chụp trạng thái view — bỏ trạng thái của màn này: {1}",
                english: "LiveOps Hub: section '{0}' threw while capturing view state — dropping the state of this section: {1}");
            table.Add(nameof(LiveOpsHubStrings.ShellCopyErrorButton),
                vietnamese: "Copy lỗi",
                english: "Copy error");
            table.Add(nameof(LiveOpsHubStrings.ShellRetryBuildButton),
                vietnamese: "Thử dựng lại",
                english: "Retry build");

            table.Add(nameof(LiveOpsHubStrings.ShellMissingLayoutTitleFormat),
                vietnamese: "Không tải được bố cục LiveOps Hub: thiếu {0}",
                english: "Cannot load the LiveOps Hub layout: {0} is missing");
            table.Add(nameof(LiveOpsHubStrings.ShellMissingLayoutBodyFormat),
                vietnamese: "Tìm ở {0}. Package có thể đang import dở hoặc thư mục Editor bị xoá.",
                english: "Looked in {0}. The package may be half-imported or the Editor folder was deleted.");
            table.Add(nameof(LiveOpsHubStrings.ShellMissingElementsTitle),
                vietnamese: "Không dựng được khung LiveOps Hub: bố cục thiếu element",
                english: "Cannot build the LiveOps Hub shell: the layout is missing elements");
            table.Add(nameof(LiveOpsHubStrings.ShellMissingElementsBodyFormat),
                vietnamese: "Thiếu: {0}. Kiểm {1} — tên element phải khớp LiveOpsHubPaths.RequiredShellElementNames.",
                english: "Missing: {0}. Check {1} — element names must match LiveOpsHubPaths.RequiredShellElementNames.");
            table.Add(nameof(LiveOpsHubStrings.ShellOpenPackageFolderButton),
                vietnamese: "Mở thư mục package",
                english: "Open package folder");
            table.Add(nameof(LiveOpsHubStrings.ShellMissingStyleSheetWarningFormat),
                vietnamese: "LiveOps Hub: không nạp được stylesheet {0} — khung vẫn chạy nhưng thiếu style.",
                english: "LiveOps Hub: could not load stylesheet {0} — the shell still runs but has no style.");

            table.Add(nameof(LiveOpsHubStrings.ShellCompilingNote),
                vietnamese: "Unity đang biên dịch — hub sẽ dựng lại sau khi xong",
                english: "Unity is compiling — the hub rebuilds when it finishes");

            table.Add(nameof(LiveOpsHubStrings.ShellUnknownSectionWarningFormat),
                vietnamese: "LiveOps Hub: không có màn id '{0}'. Id hợp lệ ở LiveOpsHubSections.Ids — hiện Tổng quan.",
                english: "LiveOps Hub: there is no section with id '{0}'. Valid ids are in LiveOpsHubSections.Ids — showing Overview.");
        }
    }
}
