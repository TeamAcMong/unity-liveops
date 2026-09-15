namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Chữ vùng Findings (G-FINDINGTEXT, V-8): mọi mảnh câu nói về một phát hiện Kiểm lịch hoặc một hàng diff. Chỉ
    /// <see cref="LiveOpsFindingText"/> và <see cref="LiveOpsChangeText"/> ghép các hằng này — màn nào cũng lấy câu qua hai lớp đó,
    /// không đọc thẳng hằng ở đây, để một phát hiện nói cùng một câu ở rail, timeline, Lịch, Tổng quan, Kiểm lịch và Xuất JSON.
    /// Microcopy nguyên văn theo [SD2 §2.3], [SD2 §3.8], [SD1 §2.2], [FD §3.1]; câu cho biến thể thiết kế chưa vẽ theo khuôn
    /// hàng mẫu gần nhất (mục 6.1 cột Microcopy). <c>{n}</c> là chỗ giá trị đã bọc <c>&lt;noparse&gt;</c> hoặc giờ đã định dạng.
    /// </summary>
    internal static partial class LiveOpsHubStrings
    {
        // ----- Mảnh chung -----
        internal const string FindingPartSeparator = " · ";
        internal const string FindingValueJoin = " và ";
        internal const string FindingListJoin = ", ";
        internal const string FindingQuotedFormat = "\"{0}\"";
        internal const string FindingFoundFormat = "tìm thấy {0}";
        internal const string FindingExpectedFormat = "cần {0}";
        internal const string FindingRangeFormat = "{0} → {1}";
        internal const string FindingRangeUtcFormat = "{0} → {1} UTC";
        internal const string FindingRuleIdLineFormat = "{0} · {1}";
        internal const string FindingAlsoWrongPrefix = "cũng sai: ";
        internal const string FindingHoursFormat = "{0} giờ";

        // ----- Nút và link trên hàng -----
        internal const string FindingButtonSafeRepair = "Sửa";
        internal const string FindingButtonProposal = "Đề xuất…";
        internal const string FindingButtonDecision = "Quyết định…";
        internal const string FindingButtonIgnorable = "Bỏ qua cảnh báo…";
        internal const string FindingButtonViewDiff = "Xem diff";
        internal const string FindingButtonDeclareType = "Khai báo";
        internal const string FindingButtonPasteRunningJson = "Dán JSON đang chạy…";
        internal const string FindingButtonCopyError = "Copy lỗi";
        internal const string FindingLinkViewInCalendar = "Xem trong lịch";
        internal const string FindingLinkOpenRule = "Mở luật";
        internal const string FindingTooltipSafeRepair = "An toàn: chỉ chuẩn hoá chữ, không đổi điều người chơi thấy";
        internal const string FindingTooltipProposal = "Đổi điều người chơi thấy — áp từng cái";
        internal const string FindingTooltipIgnorable = "Chỉ khoảng này, ghi chú bắt buộc, lưu trong asset lịch";

        /// <summary>{0} = tên file asset lịch đang mở ("Main.asset") — đúng chữ tooltip [SD2 §2.3 hàng 5] khi nơi gọi biết asset.</summary>
        internal const string FindingTooltipIgnorableInAssetFormat = "Chỉ khoảng này, ghi chú bắt buộc, lưu trong {0}";
        internal const string FindingTooltipDecisionFormat = "Hai cách đúng: {0}, hoặc {1}";

        // ----- Lựa chọn sửa (popover Đề xuất…, menu Quyết định…, xem trước Sửa hàng loạt) -----
        internal const string FindingRepairNormalizeFormat = "Chuẩn hoá {0} → {1}";
        internal const string FindingRepairKeepStartFormat = "Giữ bắt đầu, dài {0}: tới {1}…";
        internal const string FindingRepairSwapFormat = "Đổi chỗ bắt đầu và kết thúc: {0}…";
        internal const string FindingRepairRenameFormat = "Đổi id thành {0}…";
        internal const string FindingRepairShiftStartFormat = "Dời tới {0} ({1})…";
        internal const string FindingRepairShiftWholeFormat = "Giữ {0}: tới {1}…";
        internal const string FindingRepairSetActiveFormat = "Chạy bằng chu kỳ: {0}…";
        internal const string FindingRepairRevertPrefixFormat = "Giữ tiền tố {0} (hoàn về)";
        internal const string FindingRepairRevertPublished = "Hoàn về bản đã đăng";
        internal const string FindingRepairDeferFormat = "Để sau khi {0} khép ({1})…";
        internal const string FindingDecisionKeepPrefixFormat = "giữ tiền tố {0}";
        internal const string FindingDecisionRevertPublished = "hoàn về bản đã đăng";
        internal const string FindingDecisionDeferFormat = "để sau khi {0} khép ({1})";

        // ----- Ghi chú bỏ qua / hẹn giờ (nhóm Đã bỏ qua, CC-VALB-4) -----
        internal const string FindingReminderUntilFormat = "hẹn tới {0}";
        internal const string FindingIgnoredFromFormat = "từ {0}";
        internal const string FindingIgnoredEveryRange = "mọi khoảng";
        internal const string FindingDueReminderTag = "đã tới hẹn";

        // ----- Tên field định danh -----
        internal const string FindingIdentifierFieldId = "id";
        internal const string FindingIdentifierFieldType = "loại";

        // ----- Luật 1 utc-time-format -----
        internal const string FindingUtcStartHeadlineFormat = "{0} có giờ bắt đầu sai định dạng";
        internal const string FindingUtcEndHeadlineFormat = "{0} có giờ kết thúc sai định dạng";
        internal const string FindingUtcBothHeadlineFormat = "{0} có giờ bắt đầu và giờ kết thúc sai định dạng";
        internal const string FindingUtcAnchorHeadlineFormat = "{0} có neo sai định dạng";
        internal const string FindingUtcStartShortLabel = "giờ bắt đầu sai định dạng";
        internal const string FindingUtcEndShortLabel = "giờ kết thúc sai định dạng";
        internal const string FindingUtcBothShortLabel = "giờ bắt đầu và kết thúc sai định dạng";
        internal const string FindingAnchorShortLabel = "neo sai định dạng";
        internal const string FindingDroppedEntryConsequenceFormat = "Người chơi không bao giờ thấy đợt {0}: game bỏ đợt khi đọc lịch.";

        // ----- Luật 2 end-before-start -----
        internal const string FindingEndBeforeStartHeadlineFormat = "{0} kết thúc trước khi bắt đầu";
        internal const string FindingEndEqualsStartHeadlineFormat = "{0} kết thúc đúng lúc bắt đầu";
        internal const string FindingEndBeforeStartShortLabel = "kết thúc trước khi bắt đầu";
        internal const string FindingEndEqualsStartShortLabel = "kết thúc bằng lúc bắt đầu";
        internal const string FindingEndAfterStartExpectedFormat = "cần kết thúc sau {0}";

        // ----- Luật 3 invalid-identifier -----
        internal const string FindingIdentifierEmptyHeadlineFormat = "{0} có {1} rỗng";
        internal const string FindingIdentifierEmptyWithoutIdHeadline = "Một đợt có id rỗng";
        internal const string FindingIdentifierHashHeadlineFormat = "{0} có {1} không hợp lệ: chứa '#'";
        internal const string FindingIdentifierNewlineHeadlineFormat = "{0} có {1} không hợp lệ: có xuống dòng";
        internal const string FindingIdentifierEmptyShortLabelFormat = "{0} rỗng";
        internal const string FindingIdentifierHashShortLabelFormat = "{0} chứa '#'";
        internal const string FindingIdentifierNewlineShortLabelFormat = "{0} có xuống dòng";
        internal const string FindingIdentifierMeta = "game bỏ đợt có id hoặc loại rỗng, chứa # hoặc xuống dòng";

        // ----- Luật 4 duplicate-event-id -----
        internal const string FindingDuplicateHeadlineFormat = "{0} trùng id với đợt khác — game giữ mục đứng trước";
        internal const string FindingDuplicateShortLabel = "trùng id";
        internal const string FindingDuplicateSuggestionFormat = "gợi ý đổi id thành {0}";
        internal const string FindingDuplicateConsequenceFormat = "Người chơi không thấy đợt này; đợt {0} đứng trước vẫn chạy.";

        // ----- Luật 5 overlap-same-type -----
        internal const string FindingOverlapHeadlineFormat = "{0} chồng {1} với {2}";
        internal const string FindingOverlapWithoutRangeHeadlineFormat = "{0} chồng giờ với {1}";
        internal const string FindingOverlapShortLabel = "chồng giờ";
        internal const string FindingOverlapKeepsEarlier = "game giữ đợt bắt đầu sớm hơn";
        internal const string FindingOverlapConsequenceFormat = "Không thấy đợt {0} ({1}); {2} chạy bình thường.";
        internal const string FindingOverlapWithoutAnchorConsequenceFormat = "Không thấy đợt {0}; {1} chạy bình thường.";
        internal const string FindingOverlapDetailFormat = "chồng {0} với {1}";

        // ----- Luật 6 recurring-rule-invalid -----
        internal const string FindingRecurringHeadlineFormat = "luật {0} không dựng được: {1}";
        internal const string FindingRecurringPeriodReason = "chu kỳ phải lớn hơn 0 giờ";
        internal const string FindingRecurringActiveReason = "thời gian chạy phải lớn hơn 0 giờ";
        internal const string FindingRecurringActiveLongerReason = "chạy lâu hơn chu kỳ";
        internal const string FindingRecurringPrefixReason = "tiền tố chứa '#' hoặc xuống dòng";
        internal const string FindingRecurringTypeReason = "loại rỗng, chứa '#' hoặc xuống dòng";
        internal const string FindingRecurringDuplicateTypeReason = "trùng loại với luật đứng trước";
        internal const string FindingRecurringHoursMetaFormat = "tìm thấy {0} · cần lớn hơn 0 giờ";
        internal const string FindingRecurringActiveLongerMetaFormat = "chạy {0} · chu kỳ {1} · đợt sau mở trước khi đợt trước khép";
        internal const string FindingRecurringIdentifierMetaFormat = "tìm thấy {0} · game bỏ cả luật, mất mọi lần lặp";
        internal const string FindingRecurringDuplicateTypeMeta = "game giữ luật cùng loại đứng trước, bỏ luật này";
        internal const string FindingRecurringConsequenceFormat = "Game bỏ cả luật {0}: không lần lặp nào tới người chơi.";

        // ----- Luật 7 shadowed-by-recurring -----
        internal const string FindingShadowedOverlapHeadlineFormat = "{0} bị luật lặp che — game giữ đợt {1} sinh từ luật";
        internal const string FindingShadowedIdHeadlineFormat = "{0} trùng id với lần lặp {1} — game giữ đợt sinh từ luật";
        internal const string FindingShadowedOverlapShortLabel = "bị luật lặp che";
        internal const string FindingShadowedIdShortLabel = "trùng id với lần lặp";
        internal const string FindingShadowedOverlapMetaDetail = "chồng giờ với lần lặp cùng loại";
        internal const string FindingShadowedIdMetaDetail = "trùng id với một lần lặp";
        internal const string FindingShadowedConsequenceFormat = "Người chơi không thấy đợt {0}; lần lặp {1} của luật chạy thay.";

        // ----- Luật 8 unknown-event-type -----
        internal const string FindingUnknownTypeDraftHeadlineFormat = "{0} đợt {1} thuộc loại chưa khai báo";
        internal const string FindingUnknownTypeRemoteHeadlineFormat = "{0} đợt {1} trong JSON đang chạy thuộc loại chưa khai báo";
        internal const string FindingUnknownTypeDraftShortLabel = "loại chưa khai báo";
        internal const string FindingUnknownTypeRemoteShortLabel = "loại chưa khai báo trong JSON đang chạy";
        internal const string FindingUnknownTypeDraftMeta = "game bỏ mọi đợt của loại chưa khai báo · khai báo ở Loại event";
        internal const string FindingRemoteSubjectMeta = "trong JSON đang chạy · không chặn Copy JSON của nháp";
        internal const string FindingUnknownTypeDraftConsequenceFormat = "Game build với asset này bỏ {0} đợt {1}: người chơi không thấy chúng.";
        internal const string FindingUnknownTypeRemoteConsequenceFormat = "{0}: có {1} đợt trong JSON đã dán nhưng chưa có loại — game sẽ bỏ";

        // ----- Luật 9 running-event-id-changed -----
        internal const string FindingRunningPrefixChangedHeadlineFormat = "{0} đổi tiền tố khi {1} đang chạy";
        internal const string FindingRunningRemovedRecurringHeadlineFormat = "luật {0} bị xoá khi {1} đang chạy";
        internal const string FindingRunningRemovedFixedHeadlineFormat = "{0} bị xoá hoặc bị game bỏ khi đang chạy";
        internal const string FindingRunningRetypedHeadlineFormat = "{0} đổi loại khi đang chạy";
        internal const string FindingRunningMovedOutFixedHeadlineFormat = "{0} dời ra sau {1} khi đang chạy";
        internal const string FindingRunningMovedOutRecurringHeadlineFormat = "{0} dời {1} ra sau {2} khi đang chạy";
        internal const string FindingRunningMovedEarlierFixedHeadlineFormat = "{0} dời sớm về {1} khi đang chạy";
        internal const string FindingRunningMovedEarlierRecurringHeadlineFormat = "{0} dời {1} sớm về {2} khi đang chạy";
        internal const string FindingRunningMovedOutWithoutTimeHeadlineFormat = "{0} dời {1} ra ngoài khung khi đang chạy";
        internal const string FindingRunningEndsNowFixedHeadlineFormat = "{0} khép ngay khi đang chạy";
        internal const string FindingRunningEndsNowRecurringHeadlineFormat = "{0} khép {1} ngay khi đang chạy";
        internal const string FindingRunningPrefixChangedShortLabel = "đổi tiền tố khi đang chạy";
        internal const string FindingRunningRemovedShortLabel = "bị xoá khi đang chạy";
        internal const string FindingRunningRetypedShortLabel = "đổi loại khi đang chạy";
        internal const string FindingRunningMovedOutShortLabel = "dời ra ngoài khung khi đang chạy";
        internal const string FindingRunningEndsNowShortLabel = "khép ngay khi đang chạy";
        internal const string FindingRunningUntilFormat = "đang chạy tới {0}";
        internal const string FindingRunningNoOccurrenceNow = "không còn lần lặp lúc này";
        internal const string FindingRunningNotFollowed = "bản ghi người chơi không tìm lại được đợt";
        internal const string FindingRunningRetypedDetailFormat = "loại {0} → {1}";
        internal const string FindingRunningNewWindowFormat = "khung mới {0}";
        internal const string FindingRunningNewEndFormat = "kết thúc mới {0}";
        internal const string FindingRunningManualFixMeta = "không hoàn về tự động được — sửa tay trong Lịch";
        internal const string FindingRunningManualFixRecurringMeta = "không hoàn về tự động được — sửa tay ở Luật lặp";
        internal const string FindingRunningRestartConsequenceFormat = "{0} đang chạy tới {1}; người chơi có điểm bắt đầu lại từ 0.";
        internal const string FindingRunningStopConsequenceFormat = "{0} đang chạy tới {1}; người chơi đang chơi dừng cộng điểm, đợt vẫn khép theo giờ đã lưu.";
        internal const string FindingRunningEndsNowConsequenceFormat = "{0} đang chạy bị khép ngay, không chạy tới {1}; người chơi đang chơi dở nhận kết quả với điểm hiện có.";

        /// <summary>
        /// (V-21 CC-VALB-3, điều kiện user đặt) Luật 9 không có lệnh hoàn về nào giữ được đợt: câu phải chỉ đúng việc tay cần làm, không
        /// chỉ báo "không sửa tự động được". {0} = id đợt đang chạy, {1} = khung đang chạy.
        /// </summary>
        internal const string FindingRunningManualFixFixedFormat =
            "Không lệnh hoàn về nào giữ được {0}: mục khác trong nháp đang chắn (vd trùng id hoặc chồng giờ). Mở Lịch, khôi phục đợt id {0} cùng loại với khung {1}, rồi đổi id hoặc dời mục đang chắn và kiểm lại.";

        /// <summary>{0} = id lần lặp đang chạy, {1} = loại của luật, {2} = khung đang chạy.</summary>
        internal const string FindingRunningManualFixRecurringFormat =
            "Không lệnh hoàn về nào giữ được {0}: nháp không sinh lại được lần lặp này (vd mục khác đang chắn). Mở luật {1}, trả tiền tố, neo và chu kỳ về như bản đã đăng để lần lặp đang chạy vẫn là {0} ({2}), gỡ mục đang chắn trong Lịch rồi kiểm lại.";

        // ----- Luật 10 config-key-missing -----
        internal const string FindingConfigKeyFixedHeadlineFormat = "{0} không tự khai configKey — xuất sẽ ghi {1}";
        internal const string FindingConfigKeyRecurringHeadlineFormat = "luật {0} không tự khai configKey — xuất sẽ ghi {1}";
        internal const string FindingConfigKeyEmptyFixedHeadlineFormat = "{0} không tự khai configKey và loại {1} không có mặc định — xuất sẽ ghi configKey rỗng";
        internal const string FindingConfigKeyEmptyRecurringHeadlineFormat = "luật {0} không tự khai configKey và loại không có mặc định — xuất sẽ ghi configKey rỗng";
        internal const string FindingConfigKeyShortLabel = "không tự khai configKey";
        internal const string FindingConfigKeyEmptyShortLabel = "configKey rỗng";
        internal const string FindingConfigKeyUpcomingFormat = "đợt chưa bắt đầu ({0})";
        internal const string FindingConfigKeyRunningFormat = "đợt đang chạy tới {0}";
        internal const string FindingConfigKeyStartsFormat = "đợt bắt đầu {0}";
        internal const string FindingConfigKeyRecurringSubject = "luật lặp";
        internal const string FindingConfigKeyPublishedUsesFormat = "bản đã đăng dùng {0}";
        internal const string FindingConfigKeyNotInPublished = "bản đã đăng chưa có mục này";
        internal const string FindingConfigKeyEmptyMeta = "game nhận configKey rỗng, không tra được cấu hình nào";
        internal const string FindingConfigKeyDiffersConsequenceFormat = "Người chơi nhận cấu hình {0} thay cho {1}.";
        internal const string FindingConfigKeyNewConsequenceFormat = "Người chơi nhận cấu hình {0} — mặc định của loại {1}.";
        internal const string FindingConfigKeyEmptyConsequenceFormat = "Game nhận configKey rỗng: người chơi không có cấu hình nào cho {0}.";

        // ----- Luật 11 long-gap-between-events -----
        internal const string FindingLongGapHeadlineFormat = "{0} trống {1} ({2} → {3})";
        internal const string FindingLongGapShortLabelFormat = "trống {0}";
        internal const string FindingLongGapShortLabelWithoutRange = "trống dài";
        internal const string FindingLongGapWithoutRangeHeadlineFormat = "{0} trống dài giữa hai đợt";
        internal const string FindingLongGapMetaFormat = "không có đợt {0} nào trong {1}";
        internal const string FindingLongGapConsequenceFormat = "Người chơi không có đợt {0} nào trong {1}.";

        // ----- Luật 12 remote-snapshot-drift -----
        internal const string FindingRemoteDiffersHeadlineFormat = "Bản remote khác dấu {0}: {1} mục";
        internal const string FindingRemoteDiffersWithoutStampHeadlineFormat = "Bản remote khác dấu đã đăng mới nhất: {0} mục";
        internal const string FindingRemoteNoStampHeadlineFormat = "Bản remote có {0} mục nhưng chưa có dấu đã đăng để so";
        internal const string FindingRemoteDiffersShortLabel = "bản remote khác dấu đã đăng";
        internal const string FindingRemoteNoStampShortLabel = "chưa có dấu đã đăng để so";
        internal const string FindingRemoteDiffersMetaFormat = "khác ở: {0} · hub không biết ai đã sửa trên console nên không đoán";
        internal const string FindingRemoteNoStampMeta = "ghi dấu đã đăng sau lần copy kế tiếp để có bản so";
        internal const string FindingRemoteNotBlockingCopy = "không chặn Copy JSON của nháp";
        internal const string FindingRemoteDiffersConsequenceFormat = "Người chơi đang nhận bản trên remote config, khác bản đã đăng ở {0} mục.";
        internal const string FindingRemoteNoStampConsequence = "Chưa biết bản đang chạy khác gì lần đăng: chưa có dấu đã đăng nào.";

        // ----- Hàng luật không ra phát hiện (Chưa kiểm / Không áp dụng / luật ném) -----
        internal const string FindingRemoteNotMeasuredHeadline = "So với bản đang chạy trên remote config";
        internal const string FindingRemoteNotPastedMeta = "Chưa dán JSON đang chạy · luật này không tính là đã qua";

        /// <summary>(V-21 CC-VALB-2) {0} = giờ đăng của dấu mới nhất.</summary>
        internal const string FindingLatestStampNotLoadedMetaFormat = "Chưa so được với dấu đã đăng mới nhất {0} — bản so đang chọn là dấu khác.";

        internal const string FindingLatestStampNotLoadedMeta = "Chưa so được với dấu đã đăng mới nhất — bản so đang chọn là dấu khác.";
        internal const string FindingRunningNotApplicableHeadline = "Đổi id đợt đang chạy so với bản đã đăng";
        internal const string FindingNoPublishedStampMeta = "Chưa có dấu đã đăng · không có đợt đang chạy nào để so";

        /// <summary>(PD-9) Nhãn của luật 9 trong card "Đã qua" khi chưa có dấu đã đăng — Không áp dụng, không đếm vào đã qua.</summary>
        internal const string FindingRunningNotApplicableLabel = "không áp dụng: chưa có dấu đã đăng";

        internal const string FindingRuleNotApplicableLabelFormat = "không áp dụng: lý do {0}";
        internal const string FindingRuleFailedHeadlineFormat = "Luật {0} không chạy được: {1}";
        internal const string FindingRuleFailedMetaFormat = "{0} · tính vào chưa kiểm";
        internal const string FindingRuleNotMeasuredHeadlineFormat = "Luật {0} chưa kiểm được";
        internal const string FindingRuleNotMeasuredMetaFormat = "lý do {0} · không tính là đã qua";
        internal const string FindingRuleNotApplicableHeadlineFormat = "Luật {0} không áp dụng";
        internal const string FindingRuleNotApplicableMetaFormat = "lý do {0}";

        // ----- VÌ SAO (pane Chi tiết) -----
        internal const string FindingWhyUtcTimeFormat = "Parser của game đọc giờ theo ISO 8601; chuỗi không đọc được thì bỏ cả đợt.";
        internal const string FindingWhyEndBeforeStart = "LiveEventInstance từ chối đợt kết thúc không sau lúc bắt đầu, nên parser bỏ đợt.";
        internal const string FindingWhyInvalidIdentifier = "Game dùng '#' làm dấu ngăn trong grant id, nên bỏ đợt có id hoặc loại rỗng, chứa # hoặc xuống dòng.";
        internal const string FindingWhyDuplicateEventId = "Hai đợt trùng id thì game giữ mục xuất hiện trước trong JSON và bỏ mục sau.";
        internal const string FindingWhyOverlapSameType = "FixedLiveEventCalendar bỏ đợt chồng giờ cùng loại và giữ đợt bắt đầu sớm hơn.";
        internal const string FindingWhyRecurringRuleInvalid = "Luật lặp không dựng được thì game bỏ cả luật, mất mọi lần lặp của loại đó.";
        internal const string FindingWhyShadowedByRecurring = "CompositeLiveEventCalendar ghép luật lặp trước đợt cố định và bỏ đợt cố định chồng giờ hoặc trùng id với lần lặp cùng loại.";
        internal const string FindingWhyUnknownEventType = "LiveOpsSystem chỉ đăng ký loại đã khai báo trong build; đợt của loại chưa khai báo bị bỏ.";
        internal const string FindingWhyRunningEventIdChanged = "Bản ghi người chơi tìm lại đợt theo id và loại trong khung giờ đã lưu; không thấy thì dừng cộng điểm giữa đợt.";
        internal const string FindingWhyConfigKeyMissing = "Đợt không tự khai configKey thì Xuất JSON ghi configKey mặc định của loại — khoá cấu hình tới người chơi đổi theo loại.";
        internal const string FindingWhyLongGapBetweenEvents = "Một loại cố định không có đợt nào trong khoảng dài bất thường — có thể là quên lên lịch.";
        internal const string FindingWhyRemoteSnapshotDrift = "Có người sửa thẳng trên console sau khi đăng, hoặc ghi dấu một sha khác sha đã copy.";

        // ----- Lý do bỏ mục (AlsoWrongText V-7, hàng diff Bị bỏ) -----
        internal const string FindingReasonEndNotAfterStart = "kết thúc không sau bắt đầu";
        internal const string FindingReasonInvalidRecurringRule = "luật lặp sai quy tắc";

        // ----- Hàng diff (LiveOpsChangeText) -----
        internal const string ChangeAddedRowFormat = "{0} (thêm mới)";
        internal const string ChangeRemovedRowFormat = "{0} (xoá)";
        internal const string ChangeRecurringSubjectFormat = "{0}:";
        internal const string ChangeFieldFormat = "{0} {1} → {2}";
        internal const string ChangeDroppedByOtherRowFormat = "{0} · bị bỏ khi game đọc lịch (do mục khác)";
        internal const string ChangeReturnedByOtherRowFormat = "{0} · quay lại lịch khi game đọc (do mục khác)";
        internal const string ChangeEndedRenameRowFormat = "{0} → {1} (đổi id đợt đã khép)";
        internal const string ChangeFieldLabelId = "id";
        internal const string ChangeFieldLabelType = "loại";
        internal const string ChangeFieldLabelStart = "bắt đầu";
        internal const string ChangeFieldLabelEnd = "kết thúc";
        internal const string ChangeFieldLabelAnchor = "neo";
        internal const string ChangeFieldLabelIdPrefix = "tiền tố";
        internal const string ChangeFieldLabelPeriod = "chu kỳ";
        internal const string ChangeFieldLabelActive = "thời gian chạy";
        internal const string ChangeFieldLabelDisplayName = "tên hiển thị";
        internal const string ChangeFieldLabelColorSlot = "ô màu";
        internal const string ChangeFieldLabelRequiresJoin = "phải bấm tham gia";
        internal const string ChangeFieldLabelDefaultConfigKey = "configKey mặc định";
        internal const string ChangeFieldLabelOrder = "thứ tự làn";
        internal const string ChangeClauseJoin = "; ";
        internal const string ChangeDroppedConsequence = "game bỏ mục này khi đọc lịch — người chơi không thấy nó";
        internal const string ChangeDroppedByOtherConsequence = "game bỏ mục này khi đọc lịch vì mục khác — người chơi không thấy nó";
        internal const string ChangeByOtherSuffix = " (do mục khác)";
        internal const string ChangeReturnedConsequence = "mục trước bị game bỏ nay quay lại lịch — người chơi bắt đầu thấy nó";
        internal const string ChangeNobodyLoses = "không ai mất gì";
        internal const string ChangeUpcomingSafe = "đợt chưa bắt đầu, không ai mất gì";
        internal const string ChangeRunningSafe = "đợt đang chạy kéo dài, không ai mất gì";
        internal const string ChangeEndedSafe = "đợt đã khép, không ai mất gì";
        internal const string ChangeAddedSafe = "mục mới, không ai mất gì";
        internal const string ChangeTypeDefinitionSafe = "chỉ đổi cách hub hiện loại, không ai mất gì";
        internal const string ChangeUpcomingPrefix = "đợt chưa bắt đầu";
        internal const string ChangeEndedPrefix = "đợt đã khép";
        internal const string ChangeNotRunningPrefix = "chưa ai đang chơi đợt này";
        internal const string ChangeRunningReviewFormat = "{0} đang chạy tới {1}; {2}";
        internal const string ChangeClauseRemoved = "đợt đã hứa trong bản đã đăng bị gỡ";
        internal const string ChangeClauseRuleRemoved = "các lần lặp sắp tới bị gỡ";
        internal const string ChangeClauseNewIdFormat = "người chơi sẽ thấy id {0}";
        internal const string ChangeClauseNewTypeFormat = "đợt chuyển sang loại {0}";
        internal const string ChangeClauseConfigKeyFormat = "xuất ghi configKey {0}";
        internal const string ChangeClauseInheritedConfigKey = "đợt không tự khai nên xuất ghi mặc định của loại";
        internal const string ChangeClauseRecurringIdsShift = "id các lần lặp sắp tới đổi";
        internal const string ChangeClauseReusedEndedId = "id này thuộc một đợt đã khép — người đã chơi không vào lại được";
        internal const string ChangeClauseReopenedEndedId = "mở lại id này cho tương lai — người đã chơi đợt cũ không vào lại được";
        internal const string ChangeClauseRequiresJoinOn = "người chơi phải bấm tham gia mới được tính";
        internal const string ChangeClauseRequiresJoinOff = "người chơi không còn phải bấm tham gia";
        internal const string ChangeClauseShortened = "người chơi còn ít thời gian hơn";
        internal const string ChangeClauseStartAfterNow = "người chơi tạm dừng cộng điểm tới lúc đợt mở lại";
        internal const string ChangeClauseRunningConfigKeyFormat = "người chơi đang chơi chuyển sang cấu hình {0}";
        internal const string ChangeClauseActiveHours = "khung lần lặp đang chạy đổi theo thời gian chạy mới";
        internal const string ChangeClauseWindowOrKey = "bản ghi người chơi theo khung hoặc khoá mới";
        internal const string ChangeEndedRenameConsequenceFormat = "đợt đã khép đổi id {0} → {1}; bản ghi người đã chơi vẫn mang id {0}";
        internal const string ChangedTooltipFormat = "Khác bản đã đăng: {0}";
        internal const string ChangeTooltipAdded = "thêm mới";
        internal const string ChangeTooltipRemoved = "đã xoá";
        internal const string ChangeTooltipDroppedByOther = "bị bỏ khi game đọc lịch (do mục khác)";
        internal const string ChangeTooltipReturnedByOther = "quay lại lịch (do mục khác)";
        internal const string ChangeTooltipEndedRenameFormat = "đổi id đợt đã khép {0} → {1}";
    }
}
