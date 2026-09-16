namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Overview — bản tiếng Việt là bản gốc (chép nguyên văn microcopy [SD1 §1.1–§1.4]), bản tiếng Anh dịch cùng
    /// lúc (G-I18N §1.2: không bao giờ thêm khoá ở bản này mà quên bản kia). Comment "vì sao" của từng câu ở lại
    /// <c>LiveOpsHubStrings.Overview.cs</c> cạnh property.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterOverview(LiveOpsHubStringTable table)
        {
            table.Add(nameof(LiveOpsHubStrings.OverviewRecheckAllButton),
                vietnamese: "Kiểm lại tất cả",
                english: "Re-check everything");
            table.AddShared(nameof(LiveOpsHubStrings.OverviewPartSeparator), " · ");

            table.Add(nameof(LiveOpsHubStrings.OverviewDroppedCountFormat),
                vietnamese: "{0} bị bỏ",
                english: "{0} dropped");
            table.Add(nameof(LiveOpsHubStrings.OverviewProgressLostCountFormat),
                vietnamese: "{0} mất tiến độ",
                english: "{0} progress lost");
            table.Add(nameof(LiveOpsHubStrings.OverviewShouldReviewCountFormat),
                vietnamese: "{0} nên xem",
                english: "{0} to review");

            table.Add(nameof(LiveOpsHubStrings.OverviewMetricRunningCaption),
                vietnamese: "ĐANG CHẠY",
                english: "RUNNING");
            table.Add(nameof(LiveOpsHubStrings.OverviewMetricRunningUnit),
                vietnamese: "đợt",
                english: "events");
            table.Add(nameof(LiveOpsHubStrings.OverviewMetricRunningEmptyFoot),
                vietnamese: "không có đợt nào đang chạy",
                english: "no event is running");

            table.Add(nameof(LiveOpsHubStrings.OverviewMetricNeedsActionCaption),
                vietnamese: "CẦN XỬ LÝ",
                english: "NEEDS ACTION");
            table.Add(nameof(LiveOpsHubStrings.OverviewMetricNeedsActionTooltip),
                vietnamese: "Đếm theo phát hiện, cùng bộ tổng hợp với Kiểm lịch",
                english: "Counted per finding, same summary as Check calendar");
            table.Add(nameof(LiveOpsHubStrings.OverviewMetricNeverCheckedFoot),
                vietnamese: "chưa kiểm",
                english: "not checked yet");
            table.Add(nameof(LiveOpsHubStrings.OverviewMetricCheckingFormat),
                vietnamese: "Đang kiểm {0}/{1} luật…",
                english: "Checking rule {0} of {1}…");
            table.Add(nameof(LiveOpsHubStrings.OverviewMetricNeedsActionNoneFoot),
                vietnamese: "không còn việc chặn",
                english: "nothing blocking left");

            table.Add(nameof(LiveOpsHubStrings.OverviewMetricNotCheckedCaption),
                vietnamese: "CHƯA KIỂM",
                english: "NOT MEASURED");
            table.Add(nameof(LiveOpsHubStrings.OverviewMetricNotCheckedRulesFormat),
                vietnamese: "{0} luật",
                english: "{0} rules");
            table.Add(nameof(LiveOpsHubStrings.OverviewMetricNotCheckedRemoteRulesFormat),
                vietnamese: "{0} luật (bản remote)",
                english: "{0} rules (remote snapshot)");
            table.Add(nameof(LiveOpsHubStrings.OverviewMetricNotCheckedNoneFoot),
                vietnamese: "mọi luật đã kiểm",
                english: "every rule measured");

            table.Add(nameof(LiveOpsHubStrings.OverviewMetricPublishedCaption),
                vietnamese: "ĐÃ ĐĂNG",
                english: "PUBLISHED");
            table.Add(nameof(LiveOpsHubStrings.OverviewMetricPublishedFootFormat),
                vietnamese: "sha {0} · {1} thay đổi từ đó",
                english: "sha {0} · {1} changes since");
            table.Add(nameof(LiveOpsHubStrings.OverviewMetricPublishedFootUnchangedFormat),
                vietnamese: "sha {0} · chưa đổi từ đó",
                english: "sha {0} · unchanged since");
            table.Add(nameof(LiveOpsHubStrings.OverviewMetricPublishedNoStampFoot),
                vietnamese: "chưa có dấu đã đăng",
                english: "no published stamp yet");

            table.Add(nameof(LiveOpsHubStrings.OverviewNeedsActionCardTitle),
                vietnamese: "Việc cần làm trước khi đăng",
                english: "To do before publishing");
            table.Add(nameof(LiveOpsHubStrings.OverviewNeedsActionCardSubtitle),
                vietnamese: "xấu nhất trước",
                english: "worst first");
            table.Add(nameof(LiveOpsHubStrings.OverviewNeedsActionCardNoBlockersSubtitle),
                vietnamese: "0 việc chặn",
                english: "0 blocking");
            table.Add(nameof(LiveOpsHubStrings.OverviewBlocksCopyLabel),
                vietnamese: "chặn Copy JSON",
                english: "blocks Copy JSON");
            table.Add(nameof(LiveOpsHubStrings.OverviewNoBlockersEmptyFormat),
                vietnamese: "Không còn việc chặn. {0} luật chưa kiểm — xem trước khi đăng.",
                english: "Nothing blocking left. {0} rules not measured — review before publishing.");
            table.Add(nameof(LiveOpsHubStrings.OverviewNoBlockersEmptyNoRules),
                vietnamese: "Không còn việc chặn.",
                english: "Nothing blocking left.");
            table.Add(nameof(LiveOpsHubStrings.OverviewNotCheckedStillListedCaption),
                vietnamese: "chưa kiểm vẫn được liệt kê",
                english: "not-measured items are still listed");

            table.Add(nameof(LiveOpsHubStrings.OverviewRowDroppedTitleFormat),
                vietnamese: "{0} đợt sẽ bị game bỏ khi đọc lịch",
                english: "{0} events will be dropped by the game when it reads the calendar");
            table.Add(nameof(LiveOpsHubStrings.OverviewRowDroppedDetail),
                vietnamese: "Kiểm lịch (tầng Kiểm) · sửa xong thì Copy JSON mở",
                english: "Check calendar (Check stage) · fix them and Copy JSON unlocks");
            table.Add(nameof(LiveOpsHubStrings.OverviewRowProgressLostDetailFormat),
                vietnamese: "{0} · người chơi mất tiến độ · phải xem ở Xuất JSON",
                english: "{0} · players lose progress · must be reviewed in Export JSON");
            table.Add(nameof(LiveOpsHubStrings.OverviewRowShouldReviewTitleFormat),
                vietnamese: "{0} phát hiện nên xem: {1}",
                english: "{0} findings to review: {1}");
            table.Add(nameof(LiveOpsHubStrings.OverviewRowRemoteTitle),
                vietnamese: "Bản remote chưa dán — không biết Firebase đang giữ gì",
                english: "Remote snapshot not pasted — nobody knows what Firebase is serving");
            table.Add(nameof(LiveOpsHubStrings.OverviewRowRemoteDetailFormat),
                vietnamese: "Kiểm lịch · luật {0}",
                english: "Check calendar · rule {0}");
            table.Add(nameof(LiveOpsHubStrings.OverviewRowStaleCheckEditedTitleFormat),
                vietnamese: "Kết quả kiểm đã cũ — lịch đổi lúc {0}",
                english: "Check result is stale — calendar changed at {0}");
            table.Add(nameof(LiveOpsHubStrings.OverviewRowStaleCheckMilestoneTitleFormat),
                vietnamese: "Kết quả kiểm đã cũ — đã qua mốc {0}",
                english: "Check result is stale — milestone {0} has passed");
            table.Add(nameof(LiveOpsHubStrings.OverviewRowStaleCheckTitle),
                vietnamese: "Kết quả kiểm đã cũ",
                english: "Check result is stale");
            table.Add(nameof(LiveOpsHubStrings.OverviewRowNoStampTitle),
                vietnamese: "Chưa có dấu đã đăng — chưa biết bản nào đang chạy",
                english: "No published stamp — nobody knows which version is live");

            table.Add(nameof(LiveOpsHubStrings.OverviewOpenValidationButton),
                vietnamese: "Mở Kiểm lịch",
                english: "Open Check calendar");
            table.Add(nameof(LiveOpsHubStrings.OverviewOpenValidationShouldReviewButton),
                vietnamese: "Mở Kiểm lịch · Nên xem",
                english: "Open Check calendar · To review");
            table.Add(nameof(LiveOpsHubStrings.OverviewOpenExportButton),
                vietnamese: "Mở Xuất JSON",
                english: "Open Export JSON");
            table.Add(nameof(LiveOpsHubStrings.OverviewOpenRuleButtonFormat),
                vietnamese: "Mở luật {0}",
                english: "Open rule {0}");
            table.Add(nameof(LiveOpsHubStrings.OverviewOpenEventButtonFormat),
                vietnamese: "Mở đợt {0}",
                english: "Open event {0}");
            table.Add(nameof(LiveOpsHubStrings.OverviewOpenEventTypesButton),
                vietnamese: "Mở Loại event",
                english: "Open Event types");
            table.Add(nameof(LiveOpsHubStrings.OverviewRecheckButton),
                vietnamese: "Kiểm lại (F5)",
                english: "Re-check (F5)");
            table.Add(nameof(LiveOpsHubStrings.OverviewPasteRunningJsonButton),
                vietnamese: "Dán JSON đang chạy…",
                english: "Paste running JSON…");

            table.Add(nameof(LiveOpsHubStrings.OverviewLocationEventTypes),
                vietnamese: "Loại event (tầng Cấu hình)",
                english: "Event types (Configure stage)");
            table.Add(nameof(LiveOpsHubStrings.OverviewLocationCalendar),
                vietnamese: "Lịch (tầng Lên lịch)",
                english: "Calendar (Schedule stage)");
            table.Add(nameof(LiveOpsHubStrings.OverviewLocationRecurring),
                vietnamese: "Luật lặp (tầng Lên lịch)",
                english: "Recurring rules (Schedule stage)");
            table.Add(nameof(LiveOpsHubStrings.OverviewLocationValidation),
                vietnamese: "Kiểm lịch (tầng Kiểm)",
                english: "Check calendar (Check stage)");
            table.Add(nameof(LiveOpsHubStrings.OverviewLocationExport),
                vietnamese: "Xuất JSON (tầng Xuất)",
                english: "Export JSON (Export stage)");

            table.Add(nameof(LiveOpsHubStrings.OverviewFlowCardTitle),
                vietnamese: "Đường đi của lịch",
                english: "How the calendar travels");
            table.Add(nameof(LiveOpsHubStrings.OverviewFlowCardSubtitle),
                vietnamese: "rê chuột lên từng tầng để xem lý do",
                english: "hover a stage to see why");
            table.Add(nameof(LiveOpsHubStrings.OverviewFlowNoteOk),
                vietnamese: "ổn",
                english: "fine");
            table.Add(nameof(LiveOpsHubStrings.OverviewFlowNoteStopsHere),
                vietnamese: "dừng ở đây",
                english: "stops here");
            table.Add(nameof(LiveOpsHubStrings.OverviewFlowNoteBlocked),
                vietnamese: "bị chặn",
                english: "blocked");
            table.Add(nameof(LiveOpsHubStrings.OverviewFlowNoteWarning),
                vietnamese: "cần xem",
                english: "needs a look");
            table.Add(nameof(LiveOpsHubStrings.OverviewFlowNoteNotMeasured),
                vietnamese: "chưa kiểm",
                english: "not measured");
            table.Add(nameof(LiveOpsHubStrings.OverviewFlowTooltipFormat),
                vietnamese: "{0}: {1}",
                english: "{0}: {1}");

            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingCardTitleDraft),
                vietnamese: "7 ngày tới theo lịch nháp (chưa đăng)",
                english: "Next 7 days in the draft calendar (not published)");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingCardTitlePublished),
                vietnamese: "7 ngày tới theo bản đã đăng",
                english: "Next 7 days in the published version");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingRangeFormat),
                vietnamese: "{0} → {1} UTC",
                english: "{0} → {1} UTC");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingTabDraft),
                vietnamese: "Nháp",
                english: "Draft");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingTabPublished),
                vietnamese: "Bản đã đăng",
                english: "Published");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingTabPublishedDisabledReason),
                vietnamese: "Chưa có dấu đã đăng",
                english: "No published stamp yet");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingColumnTime),
                vietnamese: "Lúc (UTC)",
                english: "When (UTC)");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingColumnEvent),
                vietnamese: "Đợt",
                english: "Event");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingColumnType),
                vietnamese: "Loại",
                english: "Type");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingColumnKind),
                vietnamese: "Sự kiện",
                english: "What happens");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingColumnNote),
                vietnamese: "Ghi chú",
                english: "Note");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingKindOpen),
                vietnamese: "mở",
                english: "opens");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingKindClose),
                vietnamese: "khép",
                english: "closes");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingKindDropped),
                vietnamese: "bị bỏ",
                english: "dropped");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingKindGroupedFormat),
                vietnamese: "{0} đợt",
                english: "{0} events");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingGroupedIdsFormat),
                vietnamese: "{0}…{1}",
                english: "{0}…{1}");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingGroupedTimeFormat),
                vietnamese: "{0} → {1} · {2} đợt",
                english: "{0} → {1} · {2} events");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingGroupedNoteFormat),
                vietnamese: "{0} mỗi ngày",
                english: "{0} each day");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingDraftOnlyTag),
                vietnamese: "chỉ trong nháp",
                english: "draft only");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingNotePendingResult),
                vietnamese: "kết quả chờ hiện",
                english: "results pending");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingNoteRequiresJoin),
                vietnamese: "phải bấm tham gia",
                english: "players must opt in");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingNoteClosesAtFormat),
                vietnamese: "khép {0}",
                english: "closes {0}");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingNotePublishedIdFormat),
                vietnamese: "bản đã đăng: {0}",
                english: "published version: {0}");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingNoteRunningIdChangedFormat),
                vietnamese: "đổi id khi đang chạy · bản đã đăng: {0}",
                english: "id changed while running · published version: {0}");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingEmptyTitle),
                vietnamese: "Không có đợt nào trong 7 ngày tới",
                english: "No event in the next 7 days");
            table.Add(nameof(LiveOpsHubStrings.OverviewUpcomingEmptyBody),
                vietnamese: "Trống không phải là đạt: lịch có thể chưa có đợt nào cho tuần tới.",
                english: "Empty is not a pass: the calendar may simply have nothing for next week.");

            table.Add(nameof(LiveOpsHubStrings.OverviewFooterNote),
                vietnamese: "Vòng rỗng không phải là đạt: màn đó chưa có dữ liệu để kết luận. Hub không gửi gì lên Firebase; \"đã đăng\" là dấu bạn tự ghi.",
                english: "An empty ring is not a pass: that screen has no data to conclude from. The hub sends nothing to Firebase; \"published\" is a stamp you write yourself.");

            table.Add(nameof(LiveOpsHubStrings.OverviewNoAssetTitle),
                vietnamese: "Chưa có lịch LiveOps trong project",
                english: "No LiveOps calendar in this project");
            table.Add(nameof(LiveOpsHubStrings.OverviewNoAssetBody),
                vietnamese: "Hub cần một LiveEventCalendarAsset để lưu lịch. Game dùng nó làm lịch mặc định khi remote config trống.",
                english: "The hub needs a LiveEventCalendarAsset to store the calendar. The game uses it as the default calendar when remote config is empty.");
            table.Add(nameof(LiveOpsHubStrings.OverviewNoAssetStepCreate),
                vietnamese: "Tạo asset lịch",
                english: "Create the calendar asset");
            table.Add(nameof(LiveOpsHubStrings.OverviewNoAssetStepDeclareTypes),
                vietnamese: "Khai báo loại event",
                english: "Declare the event types");
            table.Add(nameof(LiveOpsHubStrings.OverviewNoAssetStepFirstCheck),
                vietnamese: "Kiểm lần đầu",
                english: "Run the first check");
            table.Add(nameof(LiveOpsHubStrings.OverviewCreateAssetButton),
                vietnamese: "Tạo asset lịch…",
                english: "Create calendar asset…");
            table.Add(nameof(LiveOpsHubStrings.OverviewImportRunningJsonButton),
                vietnamese: "Nhập JSON đang chạy…",
                english: "Import running JSON…");
            table.Add(nameof(LiveOpsHubStrings.OverviewSelectAssetButton),
                vietnamese: "Chọn asset có sẵn…",
                english: "Pick an existing asset…");
            table.Add(nameof(LiveOpsHubStrings.OverviewCreateAssetDialogTitle),
                vietnamese: "Tạo asset lịch LiveOps",
                english: "Create LiveOps calendar asset");
            table.Add(nameof(LiveOpsHubStrings.OverviewCreateAssetFailedFormat),
                vietnamese: "Không tạo được asset lịch tại {0}",
                english: "Could not create the calendar asset at {0}");

            table.Add(nameof(LiveOpsHubStrings.OverviewMissingLayoutFormat),
                vietnamese: "Không nạp được UXML của màn Tổng quan: {0}",
                english: "Could not load the Overview screen UXML: {0}");

            table.Add(nameof(LiveOpsHubStrings.OverviewMultipleAssetsNoticeFormat),
                vietnamese: "Có {0} LiveEventCalendarAsset. Hub đang mở {1}.",
                english: "There are {0} LiveEventCalendarAsset files. The hub has {1} open.");
        }
    }
}
