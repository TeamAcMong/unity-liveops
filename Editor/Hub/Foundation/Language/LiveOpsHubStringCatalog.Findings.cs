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
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.FindingListJoin), ", ");
            table.AddShared(nameof(LiveOpsHubStrings.FindingQuotedFormat), "\"{0}\"");
            table.Add(nameof(LiveOpsHubStrings.FindingFoundFormat),
                vietnamese: "tìm thấy {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingExpectedFormat),
                vietnamese: "cần {0}",
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.FindingRangeFormat), "{0} → {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingRangeUtcFormat),
                vietnamese: "{0} → {1} UTC",
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.FindingRuleIdLineFormat), "{0} · {1}");
            table.Add(nameof(LiveOpsHubStrings.FindingAlsoWrongPrefix),
                vietnamese: "cũng sai: ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingHoursFormat),
                vietnamese: "{0} giờ",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingButtonSafeRepair),
                vietnamese: "Sửa",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingButtonProposal),
                vietnamese: "Đề xuất…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingButtonDecision),
                vietnamese: "Quyết định…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingButtonIgnorable),
                vietnamese: "Bỏ qua cảnh báo…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingButtonViewDiff),
                vietnamese: "Xem diff",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingButtonDeclareType),
                vietnamese: "Khai báo",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingButtonPasteRunningJson),
                vietnamese: "Dán JSON đang chạy…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingButtonCopyError),
                vietnamese: "Copy lỗi",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingLinkViewInCalendar),
                vietnamese: "Xem trong lịch",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingLinkOpenRule),
                vietnamese: "Mở luật",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingTooltipSafeRepair),
                vietnamese: "An toàn: chỉ chuẩn hoá chữ, không đổi điều người chơi thấy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingTooltipProposal),
                vietnamese: "Đổi điều người chơi thấy — áp từng cái",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingTooltipIgnorable),
                vietnamese: "Chỉ khoảng này, ghi chú bắt buộc, lưu trong asset lịch",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingTooltipIgnorableInAssetFormat),
                vietnamese: "Chỉ khoảng này, ghi chú bắt buộc, lưu trong {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingTooltipDecisionFormat),
                vietnamese: "Hai cách đúng: {0}, hoặc {1}",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingRepairNormalizeFormat),
                vietnamese: "Chuẩn hoá {0} → {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRepairKeepStartFormat),
                vietnamese: "Giữ bắt đầu, dài {0}: tới {1}…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRepairSwapFormat),
                vietnamese: "Đổi chỗ bắt đầu và kết thúc: {0}…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRepairRenameFormat),
                vietnamese: "Đổi id thành {0}…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRepairShiftStartFormat),
                vietnamese: "Dời tới {0} ({1})…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRepairShiftWholeFormat),
                vietnamese: "Giữ {0}: tới {1}…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRepairSetActiveFormat),
                vietnamese: "Chạy bằng chu kỳ: {0}…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRepairRevertPrefixFormat),
                vietnamese: "Giữ tiền tố {0} (hoàn về)",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRepairRevertPublished),
                vietnamese: "Hoàn về bản đã đăng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRepairDeferFormat),
                vietnamese: "Để sau khi {0} khép ({1})…",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingDecisionKeepPrefixFormat),
                vietnamese: "giữ tiền tố {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingDecisionRevertPublished),
                vietnamese: "hoàn về bản đã đăng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingDecisionDeferFormat),
                vietnamese: "để sau khi {0} khép ({1})",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingReminderUntilFormat),
                vietnamese: "hẹn tới {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingIgnoredFromFormat),
                vietnamese: "từ {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingIgnoredEveryRange),
                vietnamese: "mọi khoảng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingDueReminderTag),
                vietnamese: "đã tới hẹn",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierFieldId),
                vietnamese: "id",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierFieldType),
                vietnamese: "loại",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingUtcStartHeadlineFormat),
                vietnamese: "{0} có giờ bắt đầu sai định dạng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingUtcEndHeadlineFormat),
                vietnamese: "{0} có giờ kết thúc sai định dạng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingUtcBothHeadlineFormat),
                vietnamese: "{0} có giờ bắt đầu và giờ kết thúc sai định dạng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingUtcAnchorHeadlineFormat),
                vietnamese: "{0} có neo sai định dạng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingUtcStartShortLabel),
                vietnamese: "giờ bắt đầu sai định dạng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingUtcEndShortLabel),
                vietnamese: "giờ kết thúc sai định dạng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingUtcBothShortLabel),
                vietnamese: "giờ bắt đầu và kết thúc sai định dạng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingAnchorShortLabel),
                vietnamese: "neo sai định dạng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingDroppedEntryConsequenceFormat),
                vietnamese: "Người chơi không bao giờ thấy đợt {0}: game bỏ đợt khi đọc lịch.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingEndBeforeStartHeadlineFormat),
                vietnamese: "{0} kết thúc trước khi bắt đầu",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingEndEqualsStartHeadlineFormat),
                vietnamese: "{0} kết thúc đúng lúc bắt đầu",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingEndBeforeStartShortLabel),
                vietnamese: "kết thúc trước khi bắt đầu",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingEndEqualsStartShortLabel),
                vietnamese: "kết thúc bằng lúc bắt đầu",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingEndAfterStartExpectedFormat),
                vietnamese: "cần kết thúc sau {0}",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierEmptyHeadlineFormat),
                vietnamese: "{0} có {1} rỗng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierEmptyWithoutIdHeadline),
                vietnamese: "Một đợt có id rỗng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierHashHeadlineFormat),
                vietnamese: "{0} có {1} không hợp lệ: chứa '#'",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierNewlineHeadlineFormat),
                vietnamese: "{0} có {1} không hợp lệ: có xuống dòng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierEmptyShortLabelFormat),
                vietnamese: "{0} rỗng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierHashShortLabelFormat),
                vietnamese: "{0} chứa '#'",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierNewlineShortLabelFormat),
                vietnamese: "{0} có xuống dòng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingIdentifierMeta),
                vietnamese: "game bỏ đợt có id hoặc loại rỗng, chứa # hoặc xuống dòng",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingDuplicateHeadlineFormat),
                vietnamese: "{0} trùng id với đợt khác — game giữ mục đứng trước",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingDuplicateShortLabel),
                vietnamese: "trùng id",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingDuplicateSuggestionFormat),
                vietnamese: "gợi ý đổi id thành {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingDuplicateConsequenceFormat),
                vietnamese: "Người chơi không thấy đợt này; đợt {0} đứng trước vẫn chạy.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingOverlapHeadlineFormat),
                vietnamese: "{0} chồng {1} với {2}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingOverlapWithoutRangeHeadlineFormat),
                vietnamese: "{0} chồng giờ với {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingOverlapShortLabel),
                vietnamese: "chồng giờ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingOverlapKeepsEarlier),
                vietnamese: "game giữ đợt bắt đầu sớm hơn",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingOverlapConsequenceFormat),
                vietnamese: "Không thấy đợt {0} ({1}); {2} chạy bình thường.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingOverlapWithoutAnchorConsequenceFormat),
                vietnamese: "Không thấy đợt {0}; {1} chạy bình thường.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingOverlapDetailFormat),
                vietnamese: "chồng {0} với {1}",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingRecurringHeadlineFormat),
                vietnamese: "luật {0} không dựng được: {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringPeriodReason),
                vietnamese: "chu kỳ phải lớn hơn 0 giờ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringActiveReason),
                vietnamese: "thời gian chạy phải lớn hơn 0 giờ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringActiveLongerReason),
                vietnamese: "chạy lâu hơn chu kỳ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringPrefixReason),
                vietnamese: "tiền tố chứa '#' hoặc xuống dòng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringTypeReason),
                vietnamese: "loại rỗng, chứa '#' hoặc xuống dòng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringDuplicateTypeReason),
                vietnamese: "trùng loại với luật đứng trước",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringHoursMetaFormat),
                vietnamese: "tìm thấy {0} · cần lớn hơn 0 giờ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringActiveLongerMetaFormat),
                vietnamese: "chạy {0} · chu kỳ {1} · đợt sau mở trước khi đợt trước khép",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringIdentifierMetaFormat),
                vietnamese: "tìm thấy {0} · game bỏ cả luật, mất mọi lần lặp",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringDuplicateTypeMeta),
                vietnamese: "game giữ luật cùng loại đứng trước, bỏ luật này",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRecurringConsequenceFormat),
                vietnamese: "Game bỏ cả luật {0}: không lần lặp nào tới người chơi.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingShadowedOverlapHeadlineFormat),
                vietnamese: "{0} bị luật lặp che — game giữ đợt {1} sinh từ luật",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingShadowedIdHeadlineFormat),
                vietnamese: "{0} trùng id với lần lặp {1} — game giữ đợt sinh từ luật",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingShadowedOverlapShortLabel),
                vietnamese: "bị luật lặp che",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingShadowedIdShortLabel),
                vietnamese: "trùng id với lần lặp",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingShadowedOverlapMetaDetail),
                vietnamese: "chồng giờ với lần lặp cùng loại",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingShadowedIdMetaDetail),
                vietnamese: "trùng id với một lần lặp",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingShadowedConsequenceFormat),
                vietnamese: "Người chơi không thấy đợt {0}; lần lặp {1} của luật chạy thay.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeDraftHeadlineFormat),
                vietnamese: "{0} đợt {1} thuộc loại chưa khai báo",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeRemoteHeadlineFormat),
                vietnamese: "{0} đợt {1} trong JSON đang chạy thuộc loại chưa khai báo",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeDraftShortLabel),
                vietnamese: "loại chưa khai báo",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeRemoteShortLabel),
                vietnamese: "loại chưa khai báo trong JSON đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeDraftMeta),
                vietnamese: "game bỏ mọi đợt của loại chưa khai báo · khai báo ở Loại event",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteSubjectMeta),
                vietnamese: "trong JSON đang chạy · không chặn Copy JSON của nháp",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeDraftConsequenceFormat),
                vietnamese: "Game build với asset này bỏ {0} đợt {1}: người chơi không thấy chúng.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingUnknownTypeRemoteConsequenceFormat),
                vietnamese: "{0}: có {1} đợt trong JSON đã dán nhưng chưa có loại — game sẽ bỏ",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingRunningPrefixChangedHeadlineFormat),
                vietnamese: "{0} đổi tiền tố khi {1} đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRemovedRecurringHeadlineFormat),
                vietnamese: "luật {0} bị xoá khi {1} đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRemovedFixedHeadlineFormat),
                vietnamese: "{0} bị xoá hoặc bị game bỏ khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRetypedHeadlineFormat),
                vietnamese: "{0} đổi loại khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningMovedOutFixedHeadlineFormat),
                vietnamese: "{0} dời ra sau {1} khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningMovedOutRecurringHeadlineFormat),
                vietnamese: "{0} dời {1} ra sau {2} khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningMovedEarlierFixedHeadlineFormat),
                vietnamese: "{0} dời sớm về {1} khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningMovedEarlierRecurringHeadlineFormat),
                vietnamese: "{0} dời {1} sớm về {2} khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningMovedOutWithoutTimeHeadlineFormat),
                vietnamese: "{0} dời {1} ra ngoài khung khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningEndsNowFixedHeadlineFormat),
                vietnamese: "{0} khép ngay khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningEndsNowRecurringHeadlineFormat),
                vietnamese: "{0} khép {1} ngay khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningPrefixChangedShortLabel),
                vietnamese: "đổi tiền tố khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRemovedShortLabel),
                vietnamese: "bị xoá khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRetypedShortLabel),
                vietnamese: "đổi loại khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningMovedOutShortLabel),
                vietnamese: "dời ra ngoài khung khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningEndsNowShortLabel),
                vietnamese: "khép ngay khi đang chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningUntilFormat),
                vietnamese: "đang chạy tới {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningNoOccurrenceNow),
                vietnamese: "không còn lần lặp lúc này",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningNotFollowed),
                vietnamese: "bản ghi người chơi không tìm lại được đợt",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRetypedDetailFormat),
                vietnamese: "loại {0} → {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningNewWindowFormat),
                vietnamese: "khung mới {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningNewEndFormat),
                vietnamese: "kết thúc mới {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningManualFixMeta),
                vietnamese: "không hoàn về tự động được — sửa tay trong Lịch",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningManualFixRecurringMeta),
                vietnamese: "không hoàn về tự động được — sửa tay ở Luật lặp",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningRestartConsequenceFormat),
                vietnamese: "{0} đang chạy tới {1}; người chơi có điểm bắt đầu lại từ 0.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningStopConsequenceFormat),
                vietnamese: "{0} đang chạy tới {1}; người chơi đang chơi dừng cộng điểm, đợt vẫn khép theo giờ đã lưu.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningEndsNowConsequenceFormat),
                vietnamese: "{0} đang chạy bị khép ngay, không chạy tới {1}; người chơi đang chơi dở nhận kết quả với điểm hiện có.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingRunningManualFixFixedFormat),
                vietnamese: "Không lệnh hoàn về nào giữ được {0}: mục khác trong nháp đang chắn (vd trùng id hoặc chồng giờ). Mở Lịch, khôi phục đợt id {0} cùng loại với khung {1}, rồi đổi id hoặc dời mục đang chắn và kiểm lại.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingRunningManualFixRecurringFormat),
                vietnamese: "Không lệnh hoàn về nào giữ được {0}: nháp không sinh lại được lần lặp này (vd mục khác đang chắn). Mở luật {1}, trả tiền tố, neo và chu kỳ về như bản đã đăng để lần lặp đang chạy vẫn là {0} ({2}), gỡ mục đang chắn trong Lịch rồi kiểm lại.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyFixedHeadlineFormat),
                vietnamese: "{0} không tự khai configKey — xuất sẽ ghi {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyRecurringHeadlineFormat),
                vietnamese: "luật {0} không tự khai configKey — xuất sẽ ghi {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyEmptyFixedHeadlineFormat),
                vietnamese: "{0} không tự khai configKey và loại {1} không có mặc định — xuất sẽ ghi configKey rỗng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyEmptyRecurringHeadlineFormat),
                vietnamese: "luật {0} không tự khai configKey và loại không có mặc định — xuất sẽ ghi configKey rỗng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyShortLabel),
                vietnamese: "không tự khai configKey",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyEmptyShortLabel),
                vietnamese: "configKey rỗng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyUpcomingFormat),
                vietnamese: "đợt chưa bắt đầu ({0})",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyRunningFormat),
                vietnamese: "đợt đang chạy tới {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyStartsFormat),
                vietnamese: "đợt bắt đầu {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyRecurringSubject),
                vietnamese: "luật lặp",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyPublishedUsesFormat),
                vietnamese: "bản đã đăng dùng {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyNotInPublished),
                vietnamese: "bản đã đăng chưa có mục này",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyEmptyMeta),
                vietnamese: "game nhận configKey rỗng, không tra được cấu hình nào",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyDiffersConsequenceFormat),
                vietnamese: "Người chơi nhận cấu hình {0} thay cho {1}.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyNewConsequenceFormat),
                vietnamese: "Người chơi nhận cấu hình {0} — mặc định của loại {1}.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingConfigKeyEmptyConsequenceFormat),
                vietnamese: "Game nhận configKey rỗng: người chơi không có cấu hình nào cho {0}.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingLongGapHeadlineFormat),
                vietnamese: "{0} trống {1} ({2} → {3})",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingLongGapShortLabelFormat),
                vietnamese: "trống {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingLongGapShortLabelWithoutRange),
                vietnamese: "trống dài",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingLongGapWithoutRangeHeadlineFormat),
                vietnamese: "{0} trống dài giữa hai đợt",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingLongGapMetaFormat),
                vietnamese: "không có đợt {0} nào trong {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingLongGapConsequenceFormat),
                vietnamese: "Người chơi không có đợt {0} nào trong {1}.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingRemoteDiffersHeadlineFormat),
                vietnamese: "Bản remote khác dấu {0}: {1} mục",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteDiffersWithoutStampHeadlineFormat),
                vietnamese: "Bản remote khác dấu đã đăng mới nhất: {0} mục",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNoStampHeadlineFormat),
                vietnamese: "Bản remote có {0} mục nhưng chưa có dấu đã đăng để so",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteDiffersShortLabel),
                vietnamese: "bản remote khác dấu đã đăng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNoStampShortLabel),
                vietnamese: "chưa có dấu đã đăng để so",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteDiffersMetaFormat),
                vietnamese: "khác ở: {0} · hub không biết ai đã sửa trên console nên không đoán",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNoStampMeta),
                vietnamese: "ghi dấu đã đăng sau lần copy kế tiếp để có bản so",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNotBlockingCopy),
                vietnamese: "không chặn Copy JSON của nháp",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteDiffersConsequenceFormat),
                vietnamese: "Người chơi đang nhận bản trên remote config, khác bản đã đăng ở {0} mục.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNoStampConsequence),
                vietnamese: "Chưa biết bản đang chạy khác gì lần đăng: chưa có dấu đã đăng nào.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNotMeasuredHeadline),
                vietnamese: "So với bản đang chạy trên remote config",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRemoteNotPastedMeta),
                vietnamese: "Chưa dán JSON đang chạy · luật này không tính là đã qua",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingLatestStampNotLoadedMetaFormat),
                vietnamese: "Chưa so được với dấu đã đăng mới nhất {0} — bản so đang chọn là dấu khác.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingLatestStampNotLoadedMeta),
                vietnamese: "Chưa so được với dấu đã đăng mới nhất — bản so đang chọn là dấu khác.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRunningNotApplicableHeadline),
                vietnamese: "Đổi id đợt đang chạy so với bản đã đăng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingNoPublishedStampMeta),
                vietnamese: "Chưa có dấu đã đăng · không có đợt đang chạy nào để so",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingRunningNotApplicableLabel),
                vietnamese: "không áp dụng: chưa có dấu đã đăng",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingRuleNotApplicableLabelFormat),
                vietnamese: "không áp dụng: lý do {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRuleFailedHeadlineFormat),
                vietnamese: "Luật {0} không chạy được: {1}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRuleFailedMetaFormat),
                vietnamese: "{0} · tính vào chưa kiểm",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRuleNotMeasuredHeadlineFormat),
                vietnamese: "Luật {0} chưa kiểm được",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRuleNotMeasuredMetaFormat),
                vietnamese: "lý do {0} · không tính là đã qua",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRuleNotApplicableHeadlineFormat),
                vietnamese: "Luật {0} không áp dụng",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingRuleNotApplicableMetaFormat),
                vietnamese: "lý do {0}",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingWhyUtcTimeFormat),
                vietnamese: "Parser của game đọc giờ theo ISO 8601; chuỗi không đọc được thì bỏ cả đợt.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingWhyEndBeforeStart),
                vietnamese: "LiveEventInstance từ chối đợt kết thúc không sau lúc bắt đầu, nên parser bỏ đợt.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingWhyInvalidIdentifier),
                vietnamese: "Game dùng '#' làm dấu ngăn trong grant id, nên bỏ đợt có id hoặc loại rỗng, chứa # hoặc xuống dòng.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingWhyDuplicateEventId),
                vietnamese: "Hai đợt trùng id thì game giữ mục xuất hiện trước trong JSON và bỏ mục sau.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingWhyOverlapSameType),
                vietnamese: "FixedLiveEventCalendar bỏ đợt chồng giờ cùng loại và giữ đợt bắt đầu sớm hơn.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingWhyRecurringRuleInvalid),
                vietnamese: "Luật lặp không dựng được thì game bỏ cả luật, mất mọi lần lặp của loại đó.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingWhyShadowedByRecurring),
                vietnamese: "CompositeLiveEventCalendar ghép luật lặp trước đợt cố định và bỏ đợt cố định chồng giờ hoặc trùng id với lần lặp cùng loại.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingWhyUnknownEventType),
                vietnamese: "LiveOpsSystem chỉ đăng ký loại đã khai báo trong build; đợt của loại chưa khai báo bị bỏ.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingWhyRunningEventIdChanged),
                vietnamese: "Bản ghi người chơi tìm lại đợt theo id và loại trong khung giờ đã lưu; không thấy thì dừng cộng điểm giữa đợt.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingWhyConfigKeyMissing),
                vietnamese: "Đợt không tự khai configKey thì Xuất JSON ghi configKey mặc định của loại — khoá cấu hình tới người chơi đổi theo loại.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingWhyLongGapBetweenEvents),
                vietnamese: "Một loại cố định không có đợt nào trong khoảng dài bất thường — có thể là quên lên lịch.",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingWhyRemoteSnapshotDrift),
                vietnamese: "Có người sửa thẳng trên console sau khi đăng, hoặc ghi dấu một sha khác sha đã copy.",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.FindingReasonEndNotAfterStart),
                vietnamese: "kết thúc không sau bắt đầu",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.FindingReasonInvalidRecurringRule),
                vietnamese: "luật lặp sai quy tắc",
                english: null);

            table.Add(nameof(LiveOpsHubStrings.ChangeAddedRowFormat),
                vietnamese: "{0} (thêm mới)",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeRemovedRowFormat),
                vietnamese: "{0} (xoá)",
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.ChangeRecurringSubjectFormat), "{0}:");
            table.AddShared(nameof(LiveOpsHubStrings.ChangeFieldFormat), "{0} {1} → {2}");
            table.Add(nameof(LiveOpsHubStrings.ChangeDroppedByOtherRowFormat),
                vietnamese: "{0} · bị bỏ khi game đọc lịch (do mục khác)",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeReturnedByOtherRowFormat),
                vietnamese: "{0} · quay lại lịch khi game đọc (do mục khác)",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeEndedRenameRowFormat),
                vietnamese: "{0} → {1} (đổi id đợt đã khép)",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelId),
                vietnamese: "id",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelType),
                vietnamese: "loại",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelStart),
                vietnamese: "bắt đầu",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelEnd),
                vietnamese: "kết thúc",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelAnchor),
                vietnamese: "neo",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelIdPrefix),
                vietnamese: "tiền tố",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelPeriod),
                vietnamese: "chu kỳ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelActive),
                vietnamese: "thời gian chạy",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelDisplayName),
                vietnamese: "tên hiển thị",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelColorSlot),
                vietnamese: "ô màu",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelRequiresJoin),
                vietnamese: "phải bấm tham gia",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelDefaultConfigKey),
                vietnamese: "configKey mặc định",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeFieldLabelOrder),
                vietnamese: "thứ tự làn",
                english: null);
            table.AddShared(nameof(LiveOpsHubStrings.ChangeClauseJoin), "; ");
            table.Add(nameof(LiveOpsHubStrings.ChangeDroppedConsequence),
                vietnamese: "game bỏ mục này khi đọc lịch — người chơi không thấy nó",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeDroppedByOtherConsequence),
                vietnamese: "game bỏ mục này khi đọc lịch vì mục khác — người chơi không thấy nó",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeByOtherSuffix),
                vietnamese: " (do mục khác)",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeReturnedConsequence),
                vietnamese: "mục trước bị game bỏ nay quay lại lịch — người chơi bắt đầu thấy nó",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeNobodyLoses),
                vietnamese: "không ai mất gì",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeUpcomingSafe),
                vietnamese: "đợt chưa bắt đầu, không ai mất gì",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeRunningSafe),
                vietnamese: "đợt đang chạy kéo dài, không ai mất gì",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeEndedSafe),
                vietnamese: "đợt đã khép, không ai mất gì",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeAddedSafe),
                vietnamese: "mục mới, không ai mất gì",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeTypeDefinitionSafe),
                vietnamese: "chỉ đổi cách hub hiện loại, không ai mất gì",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeUpcomingPrefix),
                vietnamese: "đợt chưa bắt đầu",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeEndedPrefix),
                vietnamese: "đợt đã khép",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeNotRunningPrefix),
                vietnamese: "chưa ai đang chơi đợt này",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeRunningReviewFormat),
                vietnamese: "{0} đang chạy tới {1}; {2}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseRemoved),
                vietnamese: "đợt đã hứa trong bản đã đăng bị gỡ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseRuleRemoved),
                vietnamese: "các lần lặp sắp tới bị gỡ",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseNewIdFormat),
                vietnamese: "người chơi sẽ thấy id {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseNewTypeFormat),
                vietnamese: "đợt chuyển sang loại {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseConfigKeyFormat),
                vietnamese: "xuất ghi configKey {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseInheritedConfigKey),
                vietnamese: "đợt không tự khai nên xuất ghi mặc định của loại",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseRecurringIdsShift),
                vietnamese: "id các lần lặp sắp tới đổi",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseReusedEndedId),
                vietnamese: "id này thuộc một đợt đã khép — người đã chơi không vào lại được",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseReopenedEndedId),
                vietnamese: "mở lại id này cho tương lai — người đã chơi đợt cũ không vào lại được",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseRequiresJoinOn),
                vietnamese: "người chơi phải bấm tham gia mới được tính",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseRequiresJoinOff),
                vietnamese: "người chơi không còn phải bấm tham gia",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseShortened),
                vietnamese: "người chơi còn ít thời gian hơn",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseStartAfterNow),
                vietnamese: "người chơi tạm dừng cộng điểm tới lúc đợt mở lại",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseRunningConfigKeyFormat),
                vietnamese: "người chơi đang chơi chuyển sang cấu hình {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseActiveHours),
                vietnamese: "khung lần lặp đang chạy đổi theo thời gian chạy mới",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeClauseWindowOrKey),
                vietnamese: "bản ghi người chơi theo khung hoặc khoá mới",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeEndedRenameConsequenceFormat),
                vietnamese: "đợt đã khép đổi id {0} → {1}; bản ghi người đã chơi vẫn mang id {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangedTooltipFormat),
                vietnamese: "Khác bản đã đăng: {0}",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeTooltipAdded),
                vietnamese: "thêm mới",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeTooltipRemoved),
                vietnamese: "đã xoá",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeTooltipDroppedByOther),
                vietnamese: "bị bỏ khi game đọc lịch (do mục khác)",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeTooltipReturnedByOther),
                vietnamese: "quay lại lịch (do mục khác)",
                english: null);
            table.Add(nameof(LiveOpsHubStrings.ChangeTooltipEndedRenameFormat),
                vietnamese: "đổi id đợt đã khép {0} → {1}",
                english: null);
        }
    }
}
