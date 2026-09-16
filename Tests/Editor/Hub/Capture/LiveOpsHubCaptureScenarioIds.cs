namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Hằng id của mọi kịch bản chụp ảnh (ma trận truy vết mục 9.5 của kế hoạch, đã gồm id thêm ở vá V-11/V-13/V-14:
    /// <c>ht-timeline-day-ruler</c>, <c>h09f-overview-import-json</c>, <c>h19-l-compare-remote</c>,
    /// <c>h28f-calendar-compare-disk</c>) — do G-SKELETON khai đủ từ W0 để mọi gói (dù chạy trước hay sau) tham
    /// chiếu cùng một id, không sửa về sau (V-5). Gói FIX chụp lại biến thể lệch thêm id mới ở file
    /// <c>LiveOpsHubCaptureScenarioIds.Fix&lt;Wn&gt;k.cs</c>; id hoãn (chưa kịp ở P1) ghi vào
    /// <c>LiveOpsHubCaptureDeferrals</c> kèm lý do — không xoá hằng ở đây. <c>CaptureCoverageTests</c> (G-ACCEPT)
    /// duyệt bằng reflection mọi hằng của lớp <c>partial</c> này (kể cả file vá) để chặn id chưa có kịch bản đăng ký.
    /// </summary>
    internal static partial class LiveOpsHubCaptureScenarioIds
    {
        internal const string H01CalendarDefault = "h01-calendar-default";
        internal const string H01aInspectorEmpty = "h01a-inspector-empty";
        internal const string H01bInspectorRecurring = "h01b-inspector-recurring";
        internal const string H01dInspectorUnreadable = "h01d-inspector-unreadable";
        internal const string H02StateMarks = "h02-state-marks";
        internal const string H03aComponentsControls = "h03a-components-controls";
        internal const string H03bComponentsShell = "h03b-components-shell";
        internal const string H04OverviewDefault = "h04-overview-default";
        internal const string H06aPaletteEmpty = "h06a-palette-empty";
        internal const string H06bPaletteTyping = "h06b-palette-typing";
        internal const string H06cPaletteNoMatch = "h06c-palette-no-match";
        internal const string H07aToastUndo = "h07a-toast-undo";
        internal const string H07bToastUndoDisabled = "h07b-toast-undo-disabled";
        internal const string H07cToastRedo = "h07c-toast-redo";
        internal const string HfToastBare = "hf-toast-bare";
        internal const string H07dOutcomeCopied = "h07d-outcome-copied";
        internal const string H07eOutcomeMarked = "h07e-outcome-marked";
        internal const string H08aConfirmShortenRunning = "h08a-confirm-shorten-running";
        internal const string H08fConfirmShortenRunningNoNumber = "h08f-confirm-shorten-running-no-number";
        internal const string H08bConfirmDeletePublished = "h08b-confirm-delete-published";
        internal const string H08cConfirmReplaceDraft = "h08c-confirm-replace-draft";
        internal const string H08dConfirmRestorePublished = "h08d-confirm-restore-published";
        internal const string H08eConfirmPrefixTypeToConfirm = "h08e-confirm-prefix-type-to-confirm";
        internal const string H08gConfirmRemoveStamp = "h08g-confirm-remove-stamp";
        internal const string HfConfirmLevel1Layout = "hf-confirm-level1-layout";
        internal const string HfConfirmLevel2Layout = "hf-confirm-level2-layout";
        internal const string H09aOverviewNoAsset = "h09a-overview-no-asset";
        internal const string H09cOverviewNoBlockers = "h09c-overview-no-blockers";
        internal const string H09bOverviewMultipleAssets = "h09b-overview-multiple-assets";
        internal const string H09dOverviewNeverChecked = "h09d-overview-never-checked";
        internal const string H09eOverviewChecking = "h09e-overview-checking";
        internal const string H10EventTypesDefault = "h10-event-types-default";
        internal const string H10cEventTypesColorCollision = "h10c-event-types-color-collision";
        internal const string H10bEventTypesUnknownType = "h10b-event-types-unknown-type";
        internal const string H11CalendarComparePane = "h11-calendar-compare-pane";
        internal const string H12Frame01 = "h12-frame-01";
        internal const string H12Frame03a = "h12-frame-03a";
        internal const string H12Frame03b = "h12-frame-03b";
        internal const string H12Frame04 = "h12-frame-04";
        internal const string H12Frame05 = "h12-frame-05";
        internal const string H12Frame06 = "h12-frame-06";
        internal const string H12Frame07 = "h12-frame-07";
        internal const string H12Frame08 = "h12-frame-08";
        internal const string H12Frame11 = "h12-frame-11";
        internal const string H12Frame13 = "h12-frame-13";
        internal const string H12Frame14 = "h12-frame-14";
        internal const string H12Frame02 = "h12-frame-02";
        internal const string H12Frame09 = "h12-frame-09";
        internal const string H12Frame12 = "h12-frame-12";
        internal const string HtTimelineFigure1 = "ht-timeline-figure1";
        internal const string HtTimelineMonth = "ht-timeline-month";
        internal const string HtTimelineDragOverlap = "ht-timeline-drag-overlap";
        internal const string H13CalendarNarrowDrawer = "h13-calendar-narrow-drawer";
        internal const string H13bAddEventStep1 = "h13b-add-event-step1";
        internal const string H13bAddEventStep2 = "h13b-add-event-step2";
        internal const string H13bAddEventStep3 = "h13b-add-event-step3";
        internal const string H13bAddEventOverlap = "h13b-add-event-overlap";
        internal const string H14aRecurringDefault = "h14a-recurring-default";
        internal const string H14bRecurringPrefixDraft = "h14b-recurring-prefix-draft";
        internal const string H14cRecurringAfterWrite = "h14c-recurring-after-write";
        internal const string H14dRecurringActiveLonger = "h14d-recurring-active-longer";
        internal const string H14eRecurringJsonError = "h14e-recurring-json-error";
        internal const string H15ValidationDefault = "h15-validation-default";
        internal const string H172aStaleEdited = "h17-2a-stale-edited";
        internal const string H172bStaleMilestone = "h17-2b-stale-milestone";
        internal const string H173Running = "h17-3-running";
        internal const string H174NoErrors = "h17-4-no-errors";
        internal const string H177ProposalPopover = "h17-7-proposal-popover";
        internal const string H178ToastAfterApply = "h17-8-toast-after-apply";
        internal const string H171SafeBulkPreview = "h17-1-safe-bulk-preview";
        internal const string H175IgnorePopover = "h17-5-ignore-popover";
        internal const string H176IgnoredGroup = "h17-6-ignored-group";
        internal const string H1710RuleFailed = "h17-10-rule-failed";
        internal const string H1711ShouldReviewOnly = "h17-11-should-review-only";
        internal const string H179RemoteDrift = "h17-9-remote-drift";
        internal const string H18ExportBlocked = "h18-export-blocked";
        internal const string H19BReady = "h19-b-ready";
        internal const string H19CCopied = "h19-c-copied";
        internal const string H19CprimeSavedFile = "h19-cprime-saved-file";
        internal const string H19DStale = "h19-d-stale";
        internal const string H19EMarked = "h19-e-marked";
        internal const string H19FNoChanges = "h19-f-no-changes";
        internal const string H19GFirstPublish = "h19-g-first-publish";
        internal const string H19HParserFailed = "h19-h-parser-failed";
        internal const string H19IRestoring = "h19-i-restoring";
        internal const string H19JFormat1 = "h19-j-format1";
        internal const string H19KReviewRequired = "h19-k-review-required";
        internal const string H19ParserMismatch = "h19-parser-mismatch";
        internal const string H20aMarkPublishedReady = "h20a-mark-published-ready";
        internal const string H20bMarkPublishedMissing = "h20b-mark-published-missing";
        internal const string H20cMarkPublishedDraftChanged = "h20c-mark-published-draft-changed";
        internal const string H21ExportJsonTop = "h21-export-json-top";
        internal const string H28aFailureSection = "h28a-failure-section";
        internal const string H28bMissingUxml = "h28b-missing-uxml";
        internal const string H28cCompiling = "h28c-compiling";
        internal const string H28dDiskChangedBanner = "h28d-disk-changed-banner";
        internal const string H28eConfirmOverwriteDisk = "h28e-confirm-overwrite-disk";
        internal const string HsShellSkeleton = "hs-shell-skeleton";
        internal const string HsShellWithSession = "hs-shell-with-session";
        internal const string HsShellNarrow820 = "hs-shell-narrow-820";
        internal const string HsShellCompact700 = "hs-shell-compact-700";
        internal const string HcControlsGallery = "hc-controls-gallery";
        internal const string HtTimelineDayRuler = "ht-timeline-day-ruler";
        internal const string H09fOverviewImportJson = "h09f-overview-import-json";
        internal const string H19LCompareRemote = "h19-l-compare-remote";
        internal const string H28fCalendarCompareDisk = "h28f-calendar-compare-disk";
        internal const string HoShortcutHelp = "ho-shortcut-help";

        // G-I18N (W3.5): mẫu tiếng Anh — không thuộc ma trận 9.5 gốc (chữ tiếng Việt), chỉ chụp để soát bản dịch
        // không làm vỡ layout (chuỗi tiếng Anh dài/ngắn khác tiếng Việt). Không đổi id nào ở trên.
        internal const string HsShellSkeletonEnglish = "hs-shell-skeleton-en";
        internal const string H28aFailureSectionEnglish = "h28a-failure-section-en";
    }
}
