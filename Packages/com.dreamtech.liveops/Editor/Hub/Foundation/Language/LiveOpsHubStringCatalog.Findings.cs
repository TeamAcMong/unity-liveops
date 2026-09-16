namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bảng chữ vùng Findings — bản tiếng Việt là bản gốc (chép nguyên văn từ <c>LiveOpsHubStrings.Findings.cs</c>, không đổi một ký tự),
    /// bản tiếng Anh do gói dịch G-I18N bước 2 điền vào chỗ <c>english: null</c>. Hai ngôn ngữ nằm trong cùng một lệnh đăng ký để
    /// chỗ giữ chỗ <c>{0}</c>/<c>{1}</c> soát được bằng mắt và không bao giờ thêm khoá ở bản này mà quên bản kia.
    /// Comment "vì sao" của từng câu ở lại <c>LiveOpsHubStrings.Findings.cs</c> cạnh property — đó là chỗ người đọc code tìm tới.
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        static partial void RegisterFindings(LiveOpsHubStringTable table)
        {
            table.AddShared(nameof(LiveOpsHubStrings.FindingPartSeparator), " · ");
            table.Add(nameof(LiveOpsHubStrings.FindingValueJoin),
                vietnamese: " và ",
                english: " and ");
            table.AddShared(nameof(LiveOpsHubStrings.FindingListJoin), ", ");
            table.AddShared(nameof(LiveOpsHubStrings.FindingQuotedFormat), "\"{0}\"");
            table.Add(nameof(LiveOpsHubStrings.FindingFoundFormat),
                vietnamese: "tìm thấy {0}",
                english: "found {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingExpectedFormat),
                vietnamese: "cần {0}",
                english: "expected {0}");
            table.AddShared(nameof(LiveOpsHubStrings.FindingRangeFormat), "{0} → {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingRangeUtcFormat),
                vietnamese: "{0} → {1} UTC",
                english: "{0} → {1} UTC");
            table.AddShared(nameof(LiveOpsHubStrings.FindingRuleIdLineFormat), "{0} · {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingAlsoWrongPrefix),
                vietnamese: "cũng sai: ",
                english: "also wrong: ");
            table.Add(nameof(LiveOpsHubStrings.FindingHoursFormat),
                vietnamese: "{0} giờ",
                english: "{0} hours");

            table.Add(nameof(LiveOpsHubStrings.FindingButtonSafeRepair),
                vietnamese: "Sửa",
                english: "Fix");
            table.Add(nameof(LiveOpsHubStrings.FindingButtonProposal),
                vietnamese: "Đề xuất…",
                english: "Propose…");
            table.Add(nameof(LiveOpsHubStrings.FindingButtonDecision),
                vietnamese: "Quyết định…",
                english: "Decide…");
            table.Add(nameof(LiveOpsHubStrings.FindingButtonIgnorable),
                vietnamese: "Bỏ qua cảnh báo…",
                english: "Ignore warning…");
            table.Add(nameof(LiveOpsHubStrings.FindingButtonViewDiff),
                vietnamese: "Xem diff",
                english: "View diff");
            table.Add(nameof(LiveOpsHubStrings.FindingButtonDeclareType),
                vietnamese: "Khai báo",
                english: "Declare");
            table.Add(nameof(LiveOpsHubStrings.FindingButtonPasteRunningJson),
                vietnamese: "Dán JSON đang chạy…",
                english: "Paste running JSON…");
            table.Add(nameof(LiveOpsHubStrings.FindingButtonCopyError),
                vietnamese: "Copy lỗi",
                english: "Copy error");
            table.Add(nameof(LiveOpsHubStrings.FindingLinkViewInCalendar),
                vietnamese: "Xem trong lịch",
                english: "View in calendar");
            table.Add(nameof(LiveOpsHubStrings.FindingLinkOpenRule),
                vietnamese: "Mở luật",
                english: "Open rule");
            table.Add(nameof(LiveOpsHubStrings.FindingTooltipSafeRepair),
                vietnamese: "An toàn: chỉ chuẩn hoá chữ, không đổi điều người chơi thấy",
                english: "Safe: only normalizes text, does not change what players see");
            table.Add(nameof(LiveOpsHubStrings.FindingTooltipProposal),
                vietnamese: "Đổi điều người chơi thấy — áp từng cái",
                english: "Changes what players see — apply one at a time");
            table.Add(nameof(LiveOpsHubStrings.FindingTooltipIgnorable),
                vietnamese: "Chỉ khoảng này, ghi chú bắt buộc, lưu trong asset lịch",
                english: "This range only, note required, stored in the calendar asset");

            table.Add(nameof(LiveOpsHubStrings.FindingTooltipIgnorableInAssetFormat),
                vietnamese: "Chỉ khoảng này, ghi chú bắt buộc, lưu trong {0}",
                english: "This range only, note required, stored in {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingTooltipDecisionFormat),
                vietnamese: "Hai cách đúng: {0}, hoặc {1}",
                english: "Two right ways: {0}, or {1}");

            table.Add(nameof(LiveOpsHubStrings.FindingRepairNormalizeFormat),
                vietnamese: "Chuẩn hoá {0} → {1}",
                english: "Normalize {0} → {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingRepairKeepStartFormat),
                vietnamese: "Giữ bắt đầu, dài {0}: tới {1}…",
                english: "Keep start, length {0}: to {1}…");
            table.Add(nameof(LiveOpsHubStrings.FindingRepairSwapFormat),
                vietnamese: "Đổi chỗ bắt đầu và kết thúc: {0}…",
                english: "Swap start and end: {0}…");
            table.Add(nameof(LiveOpsHubStrings.FindingRepairRenameFormat),
                vietnamese: "Đổi id thành {0}…",
                english: "Rename id to {0}…");
            table.Add(nameof(LiveOpsHubStrings.FindingRepairShiftStartFormat),
                vietnamese: "Dời tới {0} ({1})…",
                english: "Move to {0} ({1})…");
            table.Add(nameof(LiveOpsHubStrings.FindingRepairShiftWholeFormat),
                vietnamese: "Giữ {0}: tới {1}…",
                english: "Keep {0}: to {1}…");
            table.Add(nameof(LiveOpsHubStrings.FindingRepairSetActiveFormat),
                vietnamese: "Chạy bằng chu kỳ: {0}…",
                english: "Set active time to period: {0}…");
            table.Add(nameof(LiveOpsHubStrings.FindingRepairRevertPrefixFormat),
                vietnamese: "Giữ tiền tố {0} (hoàn về)",
                english: "Keep id prefix {0} (revert)");
            table.Add(nameof(LiveOpsHubStrings.FindingRepairRevertPublished),
                vietnamese: "Hoàn về bản đã đăng",
                english: "Revert to published baseline");
            table.Add(nameof(LiveOpsHubStrings.FindingRepairDeferFormat),
                vietnamese: "Để sau khi {0} khép ({1})…",
                english: "Defer until {0} ends ({1})…");
            table.Add(nameof(LiveOpsHubStrings.FindingDecisionKeepPrefixFormat),
                vietnamese: "giữ tiền tố {0}",
                english: "keep id prefix {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingDecisionRevertPublished),
                vietnamese: "hoàn về bản đã đăng",
                english: "revert to published baseline");
            table.Add(nameof(LiveOpsHubStrings.FindingDecisionDeferFormat),
                vietnamese: "để sau khi {0} khép ({1})",
                english: "defer until {0} ends ({1})");

            table.Add(nameof(LiveOpsHubStrings.FindingReminderUntilFormat),
                vietnamese: "hẹn tới {0}",
                english: "reminder until {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingIgnoredFromFormat),
                vietnamese: "từ {0}",
                english: "from {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingIgnoredEveryRange),
                vietnamese: "mọi khoảng",
                english: "every range");
            table.Add(nameof(LiveOpsHubStrings.FindingDueReminderTag),
                vietnamese: "đã tới hẹn",
                english: "reminder due");

            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierFieldId),
                vietnamese: "id",
                english: "id");
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierFieldType),
                vietnamese: "loại",
                english: "type");

            table.Add(nameof(LiveOpsHubStrings.FindingUtcStartHeadlineFormat),
                vietnamese: "{0} có giờ bắt đầu sai định dạng",
                english: "{0} has a malformed start time");
            table.Add(nameof(LiveOpsHubStrings.FindingUtcEndHeadlineFormat),
                vietnamese: "{0} có giờ kết thúc sai định dạng",
                english: "{0} has a malformed end time");
            table.Add(nameof(LiveOpsHubStrings.FindingUtcBothHeadlineFormat),
                vietnamese: "{0} có giờ bắt đầu và giờ kết thúc sai định dạng",
                english: "{0} has a malformed start time and end time");
            table.Add(nameof(LiveOpsHubStrings.FindingUtcAnchorHeadlineFormat),
                vietnamese: "{0} có neo sai định dạng",
                english: "{0} has a malformed anchor");
            table.Add(nameof(LiveOpsHubStrings.FindingUtcStartShortLabel),
                vietnamese: "giờ bắt đầu sai định dạng",
                english: "malformed start time");
            table.Add(nameof(LiveOpsHubStrings.FindingUtcEndShortLabel),
                vietnamese: "giờ kết thúc sai định dạng",
                english: "malformed end time");
            table.Add(nameof(LiveOpsHubStrings.FindingUtcBothShortLabel),
                vietnamese: "giờ bắt đầu và kết thúc sai định dạng",
                english: "malformed start and end time");
            table.Add(nameof(LiveOpsHubStrings.FindingAnchorShortLabel),
                vietnamese: "neo sai định dạng",
                english: "malformed anchor");
            table.Add(nameof(LiveOpsHubStrings.FindingDroppedEntryConsequenceFormat),
                vietnamese: "Người chơi không bao giờ thấy đợt {0}: game bỏ đợt khi đọc lịch.",
                english: "Players never see event {0}: the game drops it when reading the calendar.");

            table.Add(nameof(LiveOpsHubStrings.FindingEndBeforeStartHeadlineFormat),
                vietnamese: "{0} kết thúc trước khi bắt đầu",
                english: "{0} ends before it starts");
            table.Add(nameof(LiveOpsHubStrings.FindingEndEqualsStartHeadlineFormat),
                vietnamese: "{0} kết thúc đúng lúc bắt đầu",
                english: "{0} ends at the same moment it starts");
            table.Add(nameof(LiveOpsHubStrings.FindingEndBeforeStartShortLabel),
                vietnamese: "kết thúc trước khi bắt đầu",
                english: "ends before it starts");
            table.Add(nameof(LiveOpsHubStrings.FindingEndEqualsStartShortLabel),
                vietnamese: "kết thúc bằng lúc bắt đầu",
                english: "ends at the start time");
            table.Add(nameof(LiveOpsHubStrings.FindingEndAfterStartExpectedFormat),
                vietnamese: "cần kết thúc sau {0}",
                english: "end must be after {0}");

            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierEmptyHeadlineFormat),
                vietnamese: "{0} có {1} rỗng",
                english: "{0} has an empty {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierEmptyWithoutIdHeadline),
                vietnamese: "Một đợt có id rỗng",
                english: "An event has an empty id");
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierHashHeadlineFormat),
                vietnamese: "{0} có {1} không hợp lệ: chứa '#'",
                english: "{0} has an invalid {1}: it contains '#'");
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierNewlineHeadlineFormat),
                vietnamese: "{0} có {1} không hợp lệ: có xuống dòng",
                english: "{0} has an invalid {1}: it contains a line break");
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierEmptyShortLabelFormat),
                vietnamese: "{0} rỗng",
                english: "empty {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierHashShortLabelFormat),
                vietnamese: "{0} chứa '#'",
                english: "{0} contains '#'");
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierNewlineShortLabelFormat),
                vietnamese: "{0} có xuống dòng",
                english: "{0} contains a line break");
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierMeta),
                vietnamese: "game bỏ đợt có id hoặc loại rỗng, chứa # hoặc xuống dòng",
                english: "the game drops events whose id or type is empty, contains # or contains a line break");

            table.Add(nameof(LiveOpsHubStrings.FindingDuplicateHeadlineFormat),
                vietnamese: "{0} trùng id với đợt khác — game giữ mục đứng trước",
                english: "{0} has the same id as another event — the game keeps the earlier entry");
            table.Add(nameof(LiveOpsHubStrings.FindingDuplicateShortLabel),
                vietnamese: "trùng id",
                english: "duplicate id");
            table.Add(nameof(LiveOpsHubStrings.FindingDuplicateSuggestionFormat),
                vietnamese: "gợi ý đổi id thành {0}",
                english: "suggested new id {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingDuplicateConsequenceFormat),
                vietnamese: "Người chơi không thấy đợt này; đợt {0} đứng trước vẫn chạy.",
                english: "Players do not see this event; the earlier event {0} still runs.");

            table.Add(nameof(LiveOpsHubStrings.FindingOverlapHeadlineFormat),
                vietnamese: "{0} chồng {1} với {2}",
                english: "{0} has {1} of overlap with {2}");
            table.Add(nameof(LiveOpsHubStrings.FindingOverlapWithoutRangeHeadlineFormat),
                vietnamese: "{0} chồng giờ với {1}",
                english: "{0} overlaps {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingOverlapShortLabel),
                vietnamese: "chồng giờ",
                english: "overlap");
            table.Add(nameof(LiveOpsHubStrings.FindingOverlapKeepsEarlier),
                vietnamese: "game giữ đợt bắt đầu sớm hơn",
                english: "the game keeps the event that starts earlier");
            table.Add(nameof(LiveOpsHubStrings.FindingOverlapConsequenceFormat),
                vietnamese: "Không thấy đợt {0} ({1}); {2} chạy bình thường.",
                english: "Event {0} is not shown ({1}); {2} runs normally.");
            table.Add(nameof(LiveOpsHubStrings.FindingOverlapWithoutAnchorConsequenceFormat),
                vietnamese: "Không thấy đợt {0}; {1} chạy bình thường.",
                english: "Event {0} is not shown; {1} runs normally.");
            table.Add(nameof(LiveOpsHubStrings.FindingOverlapDetailFormat),
                vietnamese: "chồng {0} với {1}",
                english: "{0} of overlap with {1}");

            table.Add(nameof(LiveOpsHubStrings.FindingRecurringHeadlineFormat),
                vietnamese: "luật {0} không dựng được: {1}",
                english: "recurring rule {0} cannot be built: {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringPeriodReason),
                vietnamese: "chu kỳ phải lớn hơn 0 giờ",
                english: "period must be more than 0 hours");
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringActiveReason),
                vietnamese: "thời gian chạy phải lớn hơn 0 giờ",
                english: "active time must be more than 0 hours");
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringActiveLongerReason),
                vietnamese: "chạy lâu hơn chu kỳ",
                english: "active time is longer than the period");
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringPrefixReason),
                vietnamese: "tiền tố chứa '#' hoặc xuống dòng",
                english: "id prefix contains '#' or a line break");
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringTypeReason),
                vietnamese: "loại rỗng, chứa '#' hoặc xuống dòng",
                english: "type is empty, contains '#' or a line break");
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringDuplicateTypeReason),
                vietnamese: "trùng loại với luật đứng trước",
                english: "same type as an earlier rule");
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringHoursMetaFormat),
                vietnamese: "tìm thấy {0} · cần lớn hơn 0 giờ",
                english: "found {0} · must be more than 0 hours");
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringActiveLongerMetaFormat),
                vietnamese: "chạy {0} · chu kỳ {1} · đợt sau mở trước khi đợt trước khép",
                english: "active {0} · period {1} · the next event opens before the previous one ends");
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringIdentifierMetaFormat),
                vietnamese: "tìm thấy {0} · game bỏ cả luật, mất mọi lần lặp",
                english: "found {0} · the game drops the whole rule and loses every occurrence");
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringDuplicateTypeMeta),
                vietnamese: "game giữ luật cùng loại đứng trước, bỏ luật này",
                english: "the game keeps the earlier rule of the same type and drops this one");
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringConsequenceFormat),
                vietnamese: "Game bỏ cả luật {0}: không lần lặp nào tới người chơi.",
                english: "The game drops the whole rule {0}: no occurrence reaches players.");

            table.Add(nameof(LiveOpsHubStrings.FindingShadowedOverlapHeadlineFormat),
                vietnamese: "{0} bị luật lặp che — game giữ đợt {1} sinh từ luật",
                english: "{0} is shadowed by a recurring rule — the game keeps event {1} generated by the rule");
            table.Add(nameof(LiveOpsHubStrings.FindingShadowedIdHeadlineFormat),
                vietnamese: "{0} trùng id với lần lặp {1} — game giữ đợt sinh từ luật",
                english: "{0} has the same id as occurrence {1} — the game keeps the event generated by the rule");
            table.Add(nameof(LiveOpsHubStrings.FindingShadowedOverlapShortLabel),
                vietnamese: "bị luật lặp che",
                english: "shadowed by a recurring rule");
            table.Add(nameof(LiveOpsHubStrings.FindingShadowedIdShortLabel),
                vietnamese: "trùng id với lần lặp",
                english: "same id as an occurrence");
            table.Add(nameof(LiveOpsHubStrings.FindingShadowedOverlapMetaDetail),
                vietnamese: "chồng giờ với lần lặp cùng loại",
                english: "overlaps an occurrence of the same type");
            table.Add(nameof(LiveOpsHubStrings.FindingShadowedIdMetaDetail),
                vietnamese: "trùng id với một lần lặp",
                english: "same id as an occurrence");
            table.Add(nameof(LiveOpsHubStrings.FindingShadowedConsequenceFormat),
                vietnamese: "Người chơi không thấy đợt {0}; lần lặp {1} của luật chạy thay.",
                english: "Players do not see event {0}; occurrence {1} of the rule runs instead.");

            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeDraftHeadlineFormat),
                vietnamese: "{0} đợt {1} thuộc loại chưa khai báo",
                english: "{0} events use undeclared type {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeRemoteHeadlineFormat),
                vietnamese: "{0} đợt {1} trong JSON đang chạy thuộc loại chưa khai báo",
                english: "{0} events in the running JSON use undeclared type {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeDraftShortLabel),
                vietnamese: "loại chưa khai báo",
                english: "undeclared type");
            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeRemoteShortLabel),
                vietnamese: "loại chưa khai báo trong JSON đang chạy",
                english: "undeclared type in the running JSON");
            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeDraftMeta),
                vietnamese: "game bỏ mọi đợt của loại chưa khai báo · khai báo ở Loại event",
                english: "the game drops every event of an undeclared type · declare it in Event types");
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteSubjectMeta),
                vietnamese: "trong JSON đang chạy · không chặn Copy JSON của nháp",
                english: "in the running JSON · does not block Copy JSON of the draft");
            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeDraftConsequenceFormat),
                vietnamese: "Game build với asset này bỏ {0} đợt {1}: người chơi không thấy chúng.",
                english: "A game built with this asset drops {0} events of type {1}: players never see them.");
            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeRemoteConsequenceFormat),
                vietnamese: "{0}: có {1} đợt trong JSON đã dán nhưng chưa có loại — game sẽ bỏ",
                english: "{0}: {1} events in the pasted JSON have no declared type — the game will drop them");

            table.Add(nameof(LiveOpsHubStrings.FindingRunningPrefixChangedHeadlineFormat),
                vietnamese: "{0} đổi tiền tố khi {1} đang chạy",
                english: "{0} changed its id prefix while {1} is running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRemovedRecurringHeadlineFormat),
                vietnamese: "luật {0} bị xoá khi {1} đang chạy",
                english: "rule {0} was removed while {1} is running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRemovedFixedHeadlineFormat),
                vietnamese: "{0} bị xoá hoặc bị game bỏ khi đang chạy",
                english: "{0} was removed or dropped by the game while running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRetypedHeadlineFormat),
                vietnamese: "{0} đổi loại khi đang chạy",
                english: "{0} changed type while running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningMovedOutFixedHeadlineFormat),
                vietnamese: "{0} dời ra sau {1} khi đang chạy",
                english: "{0} moved to after {1} while running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningMovedOutRecurringHeadlineFormat),
                vietnamese: "{0} dời {1} ra sau {2} khi đang chạy",
                english: "{0} moved {1} to after {2} while it is running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningMovedEarlierFixedHeadlineFormat),
                vietnamese: "{0} dời sớm về {1} khi đang chạy",
                english: "{0} moved earlier to {1} while running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningMovedEarlierRecurringHeadlineFormat),
                vietnamese: "{0} dời {1} sớm về {2} khi đang chạy",
                english: "{0} moved {1} earlier to {2} while it is running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningMovedOutWithoutTimeHeadlineFormat),
                vietnamese: "{0} dời {1} ra ngoài khung khi đang chạy",
                english: "{0} moved {1} outside its window while it is running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningEndsNowFixedHeadlineFormat),
                vietnamese: "{0} khép ngay khi đang chạy",
                english: "{0} ends immediately while running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningEndsNowRecurringHeadlineFormat),
                vietnamese: "{0} khép {1} ngay khi đang chạy",
                english: "{0} ends {1} immediately while it is running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningPrefixChangedShortLabel),
                vietnamese: "đổi tiền tố khi đang chạy",
                english: "id prefix changed while running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRemovedShortLabel),
                vietnamese: "bị xoá khi đang chạy",
                english: "removed while running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRetypedShortLabel),
                vietnamese: "đổi loại khi đang chạy",
                english: "type changed while running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningMovedOutShortLabel),
                vietnamese: "dời ra ngoài khung khi đang chạy",
                english: "moved outside the window while running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningEndsNowShortLabel),
                vietnamese: "khép ngay khi đang chạy",
                english: "ends immediately while running");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningUntilFormat),
                vietnamese: "đang chạy tới {0}",
                english: "running until {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningNoOccurrenceNow),
                vietnamese: "không còn lần lặp lúc này",
                english: "no occurrence right now");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningNotFollowed),
                vietnamese: "bản ghi người chơi không tìm lại được đợt",
                english: "player records can no longer find the event");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRetypedDetailFormat),
                vietnamese: "loại {0} → {1}",
                english: "type {0} → {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningNewWindowFormat),
                vietnamese: "khung mới {0}",
                english: "new window {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningNewEndFormat),
                vietnamese: "kết thúc mới {0}",
                english: "new end {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningManualFixMeta),
                vietnamese: "không hoàn về tự động được — sửa tay trong Lịch",
                english: "cannot be reverted automatically — fix by hand in Calendar");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningManualFixRecurringMeta),
                vietnamese: "không hoàn về tự động được — sửa tay ở Luật lặp",
                english: "cannot be reverted automatically — fix by hand in Recurring rules");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRestartConsequenceFormat),
                vietnamese: "{0} đang chạy tới {1}; người chơi có điểm bắt đầu lại từ 0.",
                english: "{0} is running until {1}; players who already have points restart from 0.");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningStopConsequenceFormat),
                vietnamese: "{0} đang chạy tới {1}; người chơi đang chơi dừng cộng điểm, đợt vẫn khép theo giờ đã lưu.",
                english: "{0} is running until {1}; players in progress stop earning points, and the event still ends at the stored time.");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningEndsNowConsequenceFormat),
                vietnamese: "{0} đang chạy bị khép ngay, không chạy tới {1}; người chơi đang chơi dở nhận kết quả với điểm hiện có.",
                english: "{0} is running and ends immediately instead of running until {1}; players in progress get their result with the points they have.");

            table.Add(nameof(LiveOpsHubStrings.FindingRunningManualFixFixedFormat),
                vietnamese: "Không lệnh hoàn về nào giữ được {0}: mục khác trong nháp đang chắn (vd trùng id hoặc chồng giờ). Mở Lịch, khôi phục đợt id {0} cùng loại với khung {1}, rồi đổi id hoặc dời mục đang chắn và kiểm lại.",
                english: "No revert command can keep {0}: another entry in the draft is blocking it (for example a duplicate id or an overlap). Open Calendar, restore the event with id {0}, the same type and the window {1}, then rename or move the blocking entry and validate again.");

            table.Add(nameof(LiveOpsHubStrings.FindingRunningManualFixRecurringFormat),
                vietnamese: "Không lệnh hoàn về nào giữ được {0}: nháp không sinh lại được lần lặp này (vd mục khác đang chắn). Mở luật {1}, trả tiền tố, neo và chu kỳ về như bản đã đăng để lần lặp đang chạy vẫn là {0} ({2}), gỡ mục đang chắn trong Lịch rồi kiểm lại.",
                english: "No revert command can keep {0}: the draft cannot generate this occurrence again (for example another entry is blocking it). Open rule {1}, set the id prefix, anchor and period back to the published baseline so the running occurrence is still {0} ({2}), remove the blocking entry in Calendar, then validate again.");

            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyFixedHeadlineFormat),
                vietnamese: "{0} không tự khai configKey — xuất sẽ ghi {1}",
                english: "{0} does not declare its own configKey — export will write {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyRecurringHeadlineFormat),
                vietnamese: "luật {0} không tự khai configKey — xuất sẽ ghi {1}",
                english: "rule {0} does not declare its own configKey — export will write {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyEmptyFixedHeadlineFormat),
                vietnamese: "{0} không tự khai configKey và loại {1} không có mặc định — xuất sẽ ghi configKey rỗng",
                english: "{0} does not declare its own configKey and type {1} has no default — export will write an empty configKey");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyEmptyRecurringHeadlineFormat),
                vietnamese: "luật {0} không tự khai configKey và loại không có mặc định — xuất sẽ ghi configKey rỗng",
                english: "rule {0} does not declare its own configKey and its type has no default — export will write an empty configKey");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyShortLabel),
                vietnamese: "không tự khai configKey",
                english: "no own configKey");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyEmptyShortLabel),
                vietnamese: "configKey rỗng",
                english: "empty configKey");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyUpcomingFormat),
                vietnamese: "đợt chưa bắt đầu ({0})",
                english: "event has not started ({0})");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyRunningFormat),
                vietnamese: "đợt đang chạy tới {0}",
                english: "event is running until {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyStartsFormat),
                vietnamese: "đợt bắt đầu {0}",
                english: "event starts {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyRecurringSubject),
                vietnamese: "luật lặp",
                english: "recurring rule");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyPublishedUsesFormat),
                vietnamese: "bản đã đăng dùng {0}",
                english: "published baseline uses {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyNotInPublished),
                vietnamese: "bản đã đăng chưa có mục này",
                english: "published baseline does not have this entry yet");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyEmptyMeta),
                vietnamese: "game nhận configKey rỗng, không tra được cấu hình nào",
                english: "the game gets an empty configKey and cannot look up any config");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyDiffersConsequenceFormat),
                vietnamese: "Người chơi nhận cấu hình {0} thay cho {1}.",
                english: "Players get config {0} instead of {1}.");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyNewConsequenceFormat),
                vietnamese: "Người chơi nhận cấu hình {0} — mặc định của loại {1}.",
                english: "Players get config {0} — the default for type {1}.");
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyEmptyConsequenceFormat),
                vietnamese: "Game nhận configKey rỗng: người chơi không có cấu hình nào cho {0}.",
                english: "The game gets an empty configKey: players have no config for {0}.");

            table.Add(nameof(LiveOpsHubStrings.FindingLongGapHeadlineFormat),
                vietnamese: "{0} trống {1} ({2} → {3})",
                english: "{0} has a gap of {1} ({2} → {3})");
            table.Add(nameof(LiveOpsHubStrings.FindingLongGapShortLabelFormat),
                vietnamese: "trống {0}",
                english: "{0} gap");
            table.Add(nameof(LiveOpsHubStrings.FindingLongGapShortLabelWithoutRange),
                vietnamese: "trống dài",
                english: "long gap");
            table.Add(nameof(LiveOpsHubStrings.FindingLongGapWithoutRangeHeadlineFormat),
                vietnamese: "{0} trống dài giữa hai đợt",
                english: "{0} has a long gap between two events");
            table.Add(nameof(LiveOpsHubStrings.FindingLongGapMetaFormat),
                vietnamese: "không có đợt {0} nào trong {1}",
                english: "no {0} event during {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingLongGapConsequenceFormat),
                vietnamese: "Người chơi không có đợt {0} nào trong {1}.",
                english: "Players get no {0} event during {1}.");

            table.Add(nameof(LiveOpsHubStrings.FindingRemoteDiffersHeadlineFormat),
                vietnamese: "Bản remote khác dấu {0}: {1} mục",
                english: "Remote differs from the {0} published stamp: {1} entries");
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteDiffersWithoutStampHeadlineFormat),
                vietnamese: "Bản remote khác dấu đã đăng mới nhất: {0} mục",
                english: "Remote differs from the latest published stamp: {0} entries");
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNoStampHeadlineFormat),
                vietnamese: "Bản remote có {0} mục nhưng chưa có dấu đã đăng để so",
                english: "Remote has {0} entries but there is no published stamp to compare against");
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteDiffersShortLabel),
                vietnamese: "bản remote khác dấu đã đăng",
                english: "remote differs from the published stamp");
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNoStampShortLabel),
                vietnamese: "chưa có dấu đã đăng để so",
                english: "no published stamp to compare against");
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteDiffersMetaFormat),
                vietnamese: "khác ở: {0} · hub không biết ai đã sửa trên console nên không đoán",
                english: "differs in: {0} · the hub does not know who edited it in the console, so it does not guess");
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNoStampMeta),
                vietnamese: "ghi dấu đã đăng sau lần copy kế tiếp để có bản so",
                english: "mark published after the next copy to get a baseline");
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNotBlockingCopy),
                vietnamese: "không chặn Copy JSON của nháp",
                english: "does not block Copy JSON of the draft");
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteDiffersConsequenceFormat),
                vietnamese: "Người chơi đang nhận bản trên remote config, khác bản đã đăng ở {0} mục.",
                english: "Players are getting the version in remote config, which differs from the published baseline in {0} entries.");
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNoStampConsequence),
                vietnamese: "Chưa biết bản đang chạy khác gì lần đăng: chưa có dấu đã đăng nào.",
                english: "It is not known how the running version differs from the last publish: there is no published stamp yet.");

            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNotMeasuredHeadline),
                vietnamese: "So với bản đang chạy trên remote config",
                english: "Compared with the version running in remote config");
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNotPastedMeta),
                vietnamese: "Chưa dán JSON đang chạy · luật này không tính là đã qua",
                english: "Running JSON not pasted · this rule does not count as passed");

            table.Add(nameof(LiveOpsHubStrings.FindingLatestStampNotLoadedMetaFormat),
                vietnamese: "Chưa so được với dấu đã đăng mới nhất {0} — bản so đang chọn là dấu khác.",
                english: "Not compared with the latest published stamp {0} — the selected baseline is a different stamp.");

            table.Add(nameof(LiveOpsHubStrings.FindingLatestStampNotLoadedMeta),
                vietnamese: "Chưa so được với dấu đã đăng mới nhất — bản so đang chọn là dấu khác.",
                english: "Not compared with the latest published stamp — the selected baseline is a different stamp.");
            table.Add(nameof(LiveOpsHubStrings.FindingRunningNotApplicableHeadline),
                vietnamese: "Đổi id đợt đang chạy so với bản đã đăng",
                english: "Running event id changed against the published baseline");
            table.Add(nameof(LiveOpsHubStrings.FindingNoPublishedStampMeta),
                vietnamese: "Chưa có dấu đã đăng · không có đợt đang chạy nào để so",
                english: "No published stamp · no running event to compare");

            table.Add(nameof(LiveOpsHubStrings.FindingRunningNotApplicableLabel),
                vietnamese: "không áp dụng: chưa có dấu đã đăng",
                english: "not applicable: no published stamp");

            table.Add(nameof(LiveOpsHubStrings.FindingRuleNotApplicableLabelFormat),
                vietnamese: "không áp dụng: lý do {0}",
                english: "not applicable: reason {0}");
            table.Add(nameof(LiveOpsHubStrings.FindingRuleFailedHeadlineFormat),
                vietnamese: "Luật {0} không chạy được: {1}",
                english: "Rule {0} could not run: {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingRuleFailedMetaFormat),
                vietnamese: "{0} · tính vào chưa kiểm",
                english: "{0} · counts as not measured");
            table.Add(nameof(LiveOpsHubStrings.FindingRuleNotMeasuredHeadlineFormat),
                vietnamese: "Luật {0} chưa kiểm được",
                english: "Rule {0} is not measured");
            table.Add(nameof(LiveOpsHubStrings.FindingRuleNotMeasuredMetaFormat),
                vietnamese: "lý do {0} · không tính là đã qua",
                english: "reason {0} · does not count as passed");
            table.Add(nameof(LiveOpsHubStrings.FindingRuleNotApplicableHeadlineFormat),
                vietnamese: "Luật {0} không áp dụng",
                english: "Rule {0} does not apply");
            table.Add(nameof(LiveOpsHubStrings.FindingRuleNotApplicableMetaFormat),
                vietnamese: "lý do {0}",
                english: "reason {0}");

            table.Add(nameof(LiveOpsHubStrings.FindingWhyUtcTimeFormat),
                vietnamese: "Parser của game đọc giờ theo ISO 8601; chuỗi không đọc được thì bỏ cả đợt.",
                english: "The game parser reads times as ISO 8601; a string it cannot read drops the whole event.");
            table.Add(nameof(LiveOpsHubStrings.FindingWhyEndBeforeStart),
                vietnamese: "LiveEventInstance từ chối đợt kết thúc không sau lúc bắt đầu, nên parser bỏ đợt.",
                english: "LiveEventInstance rejects an event whose end is not after its start, so the parser drops the event.");
            table.Add(nameof(LiveOpsHubStrings.FindingWhyInvalidIdentifier),
                vietnamese: "Game dùng '#' làm dấu ngăn trong grant id, nên bỏ đợt có id hoặc loại rỗng, chứa # hoặc xuống dòng.",
                english: "The game uses '#' as the separator in a grant id, so it drops events whose id or type is empty, contains # or contains a line break.");
            table.Add(nameof(LiveOpsHubStrings.FindingWhyDuplicateEventId),
                vietnamese: "Hai đợt trùng id thì game giữ mục xuất hiện trước trong JSON và bỏ mục sau.",
                english: "When two events share an id, the game keeps the one that appears first in the JSON and drops the later one.");
            table.Add(nameof(LiveOpsHubStrings.FindingWhyOverlapSameType),
                vietnamese: "FixedLiveEventCalendar bỏ đợt chồng giờ cùng loại và giữ đợt bắt đầu sớm hơn.",
                english: "FixedLiveEventCalendar drops an overlapping event of the same type and keeps the one that starts earlier.");
            table.Add(nameof(LiveOpsHubStrings.FindingWhyRecurringRuleInvalid),
                vietnamese: "Luật lặp không dựng được thì game bỏ cả luật, mất mọi lần lặp của loại đó.",
                english: "When a recurring rule cannot be built, the game drops the whole rule and loses every occurrence of that type.");
            table.Add(nameof(LiveOpsHubStrings.FindingWhyShadowedByRecurring),
                vietnamese: "CompositeLiveEventCalendar ghép luật lặp trước đợt cố định và bỏ đợt cố định chồng giờ hoặc trùng id với lần lặp cùng loại.",
                english: "CompositeLiveEventCalendar merges recurring rules before fixed events and drops a fixed event that overlaps or shares an id with an occurrence of the same type.");
            table.Add(nameof(LiveOpsHubStrings.FindingWhyUnknownEventType),
                vietnamese: "LiveOpsSystem chỉ đăng ký loại đã khai báo trong build; đợt của loại chưa khai báo bị bỏ.",
                english: "LiveOpsSystem only registers types declared in the build; events of an undeclared type are dropped.");
            table.Add(nameof(LiveOpsHubStrings.FindingWhyRunningEventIdChanged),
                vietnamese: "Bản ghi người chơi tìm lại đợt theo id và loại trong khung giờ đã lưu; không thấy thì dừng cộng điểm giữa đợt.",
                english: "Player records find the event again by id and type within the stored window; if it is not found, scoring stops mid-event.");
            table.Add(nameof(LiveOpsHubStrings.FindingWhyConfigKeyMissing),
                vietnamese: "Đợt không tự khai configKey thì Xuất JSON ghi configKey mặc định của loại — khoá cấu hình tới người chơi đổi theo loại.",
                english: "When an event does not declare its own configKey, Export JSON writes the type default — the config key that reaches players changes with the type.");
            table.Add(nameof(LiveOpsHubStrings.FindingWhyLongGapBetweenEvents),
                vietnamese: "Một loại cố định không có đợt nào trong khoảng dài bất thường — có thể là quên lên lịch.",
                english: "A fixed type has no event for an unusually long stretch — it may be a scheduling oversight.");
            table.Add(nameof(LiveOpsHubStrings.FindingWhyRemoteSnapshotDrift),
                vietnamese: "Có người sửa thẳng trên console sau khi đăng, hoặc ghi dấu một sha khác sha đã copy.",
                english: "Someone edited directly in the console after publishing, or marked published with a sha other than the one that was copied.");

            table.Add(nameof(LiveOpsHubStrings.FindingReasonEndNotAfterStart),
                vietnamese: "kết thúc không sau bắt đầu",
                english: "end is not after start");
            table.Add(nameof(LiveOpsHubStrings.FindingReasonInvalidRecurringRule),
                vietnamese: "luật lặp sai quy tắc",
                english: "recurring rule is invalid");

            table.Add(nameof(LiveOpsHubStrings.ChangeAddedRowFormat),
                vietnamese: "{0} (thêm mới)",
                english: "{0} (added)");
            table.Add(nameof(LiveOpsHubStrings.ChangeRemovedRowFormat),
                vietnamese: "{0} (xoá)",
                english: "{0} (removed)");
            table.AddShared(nameof(LiveOpsHubStrings.ChangeRecurringSubjectFormat), "{0}:");
            table.AddShared(nameof(LiveOpsHubStrings.ChangeFieldFormat), "{0} {1} → {2}");
            table.Add(nameof(LiveOpsHubStrings.ChangeDroppedByOtherRowFormat),
                vietnamese: "{0} · bị bỏ khi game đọc lịch (do mục khác)",
                english: "{0} · dropped when the game reads the calendar (because of another entry)");
            table.Add(nameof(LiveOpsHubStrings.ChangeReturnedByOtherRowFormat),
                vietnamese: "{0} · quay lại lịch khi game đọc (do mục khác)",
                english: "{0} · back in the calendar when the game reads it (because of another entry)");
            table.Add(nameof(LiveOpsHubStrings.ChangeEndedRenameRowFormat),
                vietnamese: "{0} → {1} (đổi id đợt đã khép)",
                english: "{0} → {1} (id changed on an ended event)");
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelId),
                vietnamese: "id",
                english: "id");
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelType),
                vietnamese: "loại",
                english: "type");
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelStart),
                vietnamese: "bắt đầu",
                english: "start");
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelEnd),
                vietnamese: "kết thúc",
                english: "end");
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelAnchor),
                vietnamese: "neo",
                english: "anchor");
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelIdPrefix),
                vietnamese: "tiền tố",
                english: "id prefix");
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelPeriod),
                vietnamese: "chu kỳ",
                english: "period");
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelActive),
                vietnamese: "thời gian chạy",
                english: "active time");
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelDisplayName),
                vietnamese: "tên hiển thị",
                english: "display name");
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelColorSlot),
                vietnamese: "ô màu",
                english: "color slot");
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelRequiresJoin),
                vietnamese: "phải bấm tham gia",
                english: "requires join");
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelDefaultConfigKey),
                vietnamese: "configKey mặc định",
                english: "default configKey");
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelOrder),
                vietnamese: "thứ tự làn",
                english: "lane order");
            table.AddShared(nameof(LiveOpsHubStrings.ChangeClauseJoin), "; ");
            table.Add(nameof(LiveOpsHubStrings.ChangeDroppedConsequence),
                vietnamese: "game bỏ mục này khi đọc lịch — người chơi không thấy nó",
                english: "the game drops this entry when reading the calendar — players do not see it");
            table.Add(nameof(LiveOpsHubStrings.ChangeDroppedByOtherConsequence),
                vietnamese: "game bỏ mục này khi đọc lịch vì mục khác — người chơi không thấy nó",
                english: "the game drops this entry when reading the calendar because of another entry — players do not see it");
            table.Add(nameof(LiveOpsHubStrings.ChangeByOtherSuffix),
                vietnamese: " (do mục khác)",
                english: " (because of another entry)");
            table.Add(nameof(LiveOpsHubStrings.ChangeReturnedConsequence),
                vietnamese: "mục trước bị game bỏ nay quay lại lịch — người chơi bắt đầu thấy nó",
                english: "an entry the game used to drop is back in the calendar — players start seeing it");
            table.Add(nameof(LiveOpsHubStrings.ChangeNobodyLoses),
                vietnamese: "không ai mất gì",
                english: "nobody loses anything");
            table.Add(nameof(LiveOpsHubStrings.ChangeUpcomingSafe),
                vietnamese: "đợt chưa bắt đầu, không ai mất gì",
                english: "event has not started, nobody loses anything");
            table.Add(nameof(LiveOpsHubStrings.ChangeRunningSafe),
                vietnamese: "đợt đang chạy kéo dài, không ai mất gì",
                english: "running event gets longer, nobody loses anything");
            table.Add(nameof(LiveOpsHubStrings.ChangeEndedSafe),
                vietnamese: "đợt đã khép, không ai mất gì",
                english: "event has ended, nobody loses anything");
            table.Add(nameof(LiveOpsHubStrings.ChangeAddedSafe),
                vietnamese: "mục mới, không ai mất gì",
                english: "new entry, nobody loses anything");
            table.Add(nameof(LiveOpsHubStrings.ChangeTypeDefinitionSafe),
                vietnamese: "chỉ đổi cách hub hiện loại, không ai mất gì",
                english: "only changes how the hub shows the type, nobody loses anything");
            table.Add(nameof(LiveOpsHubStrings.ChangeUpcomingPrefix),
                vietnamese: "đợt chưa bắt đầu",
                english: "event has not started");
            table.Add(nameof(LiveOpsHubStrings.ChangeEndedPrefix),
                vietnamese: "đợt đã khép",
                english: "event has ended");
            table.Add(nameof(LiveOpsHubStrings.ChangeNotRunningPrefix),
                vietnamese: "chưa ai đang chơi đợt này",
                english: "nobody is playing this event yet");
            table.Add(nameof(LiveOpsHubStrings.ChangeRunningReviewFormat),
                vietnamese: "{0} đang chạy tới {1}; {2}",
                english: "{0} is running until {1}; {2}");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseRemoved),
                vietnamese: "đợt đã hứa trong bản đã đăng bị gỡ",
                english: "an event promised in the published baseline is removed");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseRuleRemoved),
                vietnamese: "các lần lặp sắp tới bị gỡ",
                english: "upcoming occurrences are removed");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseNewIdFormat),
                vietnamese: "người chơi sẽ thấy id {0}",
                english: "players will see id {0}");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseNewTypeFormat),
                vietnamese: "đợt chuyển sang loại {0}",
                english: "the event moves to type {0}");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseConfigKeyFormat),
                vietnamese: "xuất ghi configKey {0}",
                english: "export writes configKey {0}");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseInheritedConfigKey),
                vietnamese: "đợt không tự khai nên xuất ghi mặc định của loại",
                english: "the event declares none, so export writes the type default");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseRecurringIdsShift),
                vietnamese: "id các lần lặp sắp tới đổi",
                english: "ids of upcoming occurrences change");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseReusedEndedId),
                vietnamese: "id này thuộc một đợt đã khép — người đã chơi không vào lại được",
                english: "this id belongs to an event that has ended — players who already played cannot get back in");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseReopenedEndedId),
                vietnamese: "mở lại id này cho tương lai — người đã chơi đợt cũ không vào lại được",
                english: "this id is reopened for the future — players who played the old event cannot get back in");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseRequiresJoinOn),
                vietnamese: "người chơi phải bấm tham gia mới được tính",
                english: "players must press join to be counted");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseRequiresJoinOff),
                vietnamese: "người chơi không còn phải bấm tham gia",
                english: "players no longer have to press join");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseShortened),
                vietnamese: "người chơi còn ít thời gian hơn",
                english: "players have less time left");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseStartAfterNow),
                vietnamese: "người chơi tạm dừng cộng điểm tới lúc đợt mở lại",
                english: "players stop earning points until the event opens again");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseRunningConfigKeyFormat),
                vietnamese: "người chơi đang chơi chuyển sang cấu hình {0}",
                english: "players in progress move to config {0}");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseActiveHours),
                vietnamese: "khung lần lặp đang chạy đổi theo thời gian chạy mới",
                english: "the running occurrence window changes with the new active time");
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseWindowOrKey),
                vietnamese: "bản ghi người chơi theo khung hoặc khoá mới",
                english: "player records follow the new window or key");
            table.Add(nameof(LiveOpsHubStrings.ChangeEndedRenameConsequenceFormat),
                vietnamese: "đợt đã khép đổi id {0} → {1}; bản ghi người đã chơi vẫn mang id {0}",
                english: "an ended event changed id {0} → {1}; records of players who played still carry id {0}");
            table.Add(nameof(LiveOpsHubStrings.ChangedTooltipFormat),
                vietnamese: "Khác bản đã đăng: {0}",
                english: "Differs from the published baseline: {0}");
            table.Add(nameof(LiveOpsHubStrings.ChangeTooltipAdded),
                vietnamese: "thêm mới",
                english: "added");
            table.Add(nameof(LiveOpsHubStrings.ChangeTooltipRemoved),
                vietnamese: "đã xoá",
                english: "removed");
            table.Add(nameof(LiveOpsHubStrings.ChangeTooltipDroppedByOther),
                vietnamese: "bị bỏ khi game đọc lịch (do mục khác)",
                english: "dropped when the game reads the calendar (because of another entry)");
            table.Add(nameof(LiveOpsHubStrings.ChangeTooltipReturnedByOther),
                vietnamese: "quay lại lịch (do mục khác)",
                english: "back in the calendar (because of another entry)");
            table.Add(nameof(LiveOpsHubStrings.ChangeTooltipEndedRenameFormat),
                vietnamese: "đổi id đợt đã khép {0} → {1}",
                english: "id changed on an ended event {0} → {1}");
        }
    }
}
