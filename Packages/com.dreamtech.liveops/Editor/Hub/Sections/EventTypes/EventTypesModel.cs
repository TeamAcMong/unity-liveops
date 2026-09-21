using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Model thuần của màn Loại event (7.2): hàng bảng theo thứ tự làn, câu trùng màu / ghi đè theo băm, khoá id loại, nhãn xoá
    /// loại và câu của loại chưa khai báo (V-17). Không <c>VisualElement</c>, không <c>UnityEditor</c> — mọi quyết định của màn
    /// nằm ở đây, view chỉ đọc và gắn class (7.0 "model thuần + view mỏng").
    /// <para>
    /// Hàng bảng dựng từ <b>tài liệu + bản dán</b> chứ không từ báo cáo kiểm: bảng phải đúng ngay cả trước lần kiểm đầu tiên.
    /// </para>
    /// <para>
    /// <see cref="Health"/> KHÔNG phải health mà màn báo lên rail: <c>EventTypesSection.GetHealth</c> trả
    /// <see cref="LiveOpsHubFindingRouting.ForSection"/> (nguồn chính thức, biết cả "chưa có asset" và "kết quả cũ" mà model
    /// không nhìn thấy). <see cref="Health"/> là bản tính ĐỘC LẬP của cùng kết luận, giữ để test
    /// <c>Health_StateMatchesFindingRouting</c> đối chiếu hai cách tính — hai bên lệch nhau là routing đã đổi mà màn chưa biết.
    /// </para>
    /// </summary>
    internal sealed class EventTypesModel
    {
        /// <summary>
        /// Số ô trống nêu tên trong câu gợi ý khi còn trùng màu. Nêu cả 8 ô làm câu dài hơn phần còn lại của inspector 300px và
        /// không giúp gì thêm — ba ô đầu đủ để bấm chọn (microcopy [SD1 §2.1] cũng nêu đúng ba ô).
        /// </summary>
        internal const int SuggestedFreeSlotCount = 3;

        private readonly LiveEventCalendarDocument _document;
        private readonly LiveEventCalendarCheckReport _report;
        private readonly List<EventTypeRow> _rows = new List<EventTypeRow>();
        private readonly Dictionary<string, EventTypeRow> _rowByTypeId = new Dictionary<string, EventTypeRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, LiveEventCalendarFinding> _unknownFindingByTypeId =
            new Dictionary<string, LiveEventCalendarFinding>(StringComparer.Ordinal);
        private readonly List<int> _freeSlots = new List<int>();

        private EventTypesModel(LiveEventCalendarDocument document, LiveEventCalendarCheckReport report)
        {
            _document = document;
            _report = report;
        }

        /// <param name="document">Nháp hiện tại — bắt buộc.</param>
        /// <param name="report">Báo cáo kiểm gần nhất; null khi chưa kiểm lần nào (bảng vẫn dựng đủ, health về Ok như rail).</param>
        /// <param name="remote">JSON đang chạy đã dán; null hoặc chưa dán = không có hàng "chỉ có trong bản remote" (V-17).</param>
        public static EventTypesModel Build(LiveEventCalendarDocument document, LiveEventCalendarCheckReport report,
            LiveOpsHubRemoteSnapshot remote)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            EventTypesModel model = new EventTypesModel(document, report);
            model.CollectUnknownFindings();
            model.BuildDeclaredRows();
            model.BuildUndeclaredRows(remote);
            model.CollectFreeSlots();
            return model;
        }

        /// <summary>Loại đã khai theo thứ tự làn (V-12: bảng theo thứ tự này khi chưa bật sort cột), rồi hàng chưa khai báo.</summary>
        public IReadOnlyList<EventTypeRow> Rows
        {
            get { return _rows; }
        }

        /// <summary>
        /// Bản tính độc lập của health màn: Blocked khi báo cáo kiểm còn loại chưa khai báo (kể cả loại chỉ có trong JSON đang
        /// chạy, V-17), Warning khi hai loại cùng ô màu (tính thẳng trên nháp, không luật nào lo), còn lại Ok. Badge để rỗng có
        /// chủ đích — badge của rail do <see cref="LiveOpsHubFindingRouting"/> dựng, màn không đếm lại.
        /// <para>
        /// KHÔNG được vẽ lên đâu cả: cái mà màn báo lên rail là <c>EventTypesSection.GetHealth</c> (routing). Đây chỉ là đầu
        /// vào của test đối chiếu — dùng nó để vẽ là mở lại đúng cái khe "màn và rail nói hai chuyện" mà routing sinh ra để bịt.
        /// </para>
        /// </summary>
        public SectionHealth Health
        {
            get { return BuildHealth(); }
        }

        /// <summary>"5 loại · config key của loại được ghi vào mọi đợt và mục recurring không tự khai khi Xuất JSON".</summary>
        public string FooterText
        {
            get
            {
                return LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.EventTypesFooterFormat),
                    _document.EventTypes.Count.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>Chưa có loại nào (kể cả chưa khai báo) — thân màn hiện trạng thái trống thay cho bảng.</summary>
        public bool IsEmpty
        {
            get { return _rows.Count == 0; }
        }

        /// <summary>Ô màu chưa loại nào dùng, tăng dần — chỉ để gợi ý, hub không bao giờ tự ghi (PD Q1 [SD1 Q1]).</summary>
        public IReadOnlyList<int> FreeSlots
        {
            get { return _freeSlots; }
        }

        /// <summary>Loại chưa khai báo đầu tiên (hàng đầu của dòng Blocked dưới bảng); "" khi không có.</summary>
        public string FirstUndeclaredTypeId
        {
            get
            {
                for (int index = 0; index < _rows.Count; index++)
                {
                    if (!_rows[index].IsDeclared) return _rows[index].TypeId;
                }
                return string.Empty;
            }
        }

        /// <summary>"Trùng ô 7 với treasure-hunt — chọn ô trống: 2, 3, 5"; "" khi ô màu của loại này không đụng loại nào.</summary>
        public string ColorCollisionText(string typeId)
        {
            if (!TryGetDeclaredType(typeId, out LiveEventTypeDefinition type)) return string.Empty;
            IReadOnlyList<LiveEventTypeDefinition> sharing = LiveEventTypeColorSlots.TypesSharingSlot(type, _document.EventTypes);
            if (sharing.Count == 0) return string.Empty;

            string slotText = SlotNumberText(type.ColorSlot);
            string others = JoinTypeIds(sharing);
            IReadOnlyList<int> suggestions = SuggestedFreeSlots(typeId);
            if (suggestions.Count == 0)
            {
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesColorCollisionNoFreeSlotFormat, slotText, others);
            }
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesColorCollisionFormat, slotText, others,
                JoinSlotNumbers(suggestions));
        }

        /// <summary>"Theo băm trùng treasure-hunt (ô 7) → đang ghi đè ô 6 steel"; "" khi loại đang dùng đúng ô theo băm.</summary>
        public string HashOverrideText(string typeId)
        {
            if (!TryGetDeclaredType(typeId, out LiveEventTypeDefinition type)) return string.Empty;
            int hashedSlot = LiveEventTypeColorSlots.DefaultSlotFor(type.TypeId);
            if (hashedSlot == type.ColorSlot) return string.Empty;

            // Chỉ nói "theo băm trùng X" khi ô theo băm THẬT SỰ đang có loại khác: ghi đè vì lý do khác (người dùng thích màu
            // này hơn) không phải chuyện phải giải thích, và câu "trùng" khi không trùng là câu sai.
            string occupant = FirstDeclaredTypeInSlot(hashedSlot, type.TypeId);
            if (occupant.Length == 0) return string.Empty;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesHashOverrideFormat, occupant,
                SlotNumberText(hashedSlot), SlotNamedText(type.ColorSlot));
        }

        /// <summary>Loại đang dùng ô khác ô theo băm — nút "Về màu theo băm" chỉ hiện khi có đường để về.</summary>
        public bool CanResetColorToHash(string typeId)
        {
            return TryGetDeclaredType(typeId, out LiveEventTypeDefinition type)
                && LiveEventTypeColorSlots.DefaultSlotFor(type.TypeId) != type.ColorSlot;
        }

        /// <summary>Ô theo băm của loại — nút "Về màu theo băm" ghi đúng ô này; -1 khi loại chưa khai báo.</summary>
        public int HashedSlotOf(string typeId)
        {
            return TryGetDeclaredType(typeId, out LiveEventTypeDefinition type)
                ? LiveEventTypeColorSlots.DefaultSlotFor(type.TypeId)
                : -1;
        }

        /// <summary>Ba ô trống đầu để gợi ý (viền focus + câu cảnh báo); rỗng khi loại không trùng màu hoặc hết ô trống.</summary>
        public IReadOnlyList<int> SuggestedFreeSlots(string typeId)
        {
            if (!TryGetDeclaredType(typeId, out LiveEventTypeDefinition type)) return Array.Empty<int>();
            if (LiveEventTypeColorSlots.TypesSharingSlot(type, _document.EventTypes).Count == 0) return Array.Empty<int>();

            List<int> suggestions = new List<int>();
            for (int index = 0; index < _freeSlots.Count && suggestions.Count < SuggestedFreeSlotCount; index++)
            {
                suggestions.Add(_freeSlots[index]);
            }
            return suggestions;
        }

        /// <summary>Id loại sửa được không — khoá khi loại đã có đợt hoặc luật ([SD1 §2.1]); <paramref name="reason"/> rỗng khi mở.</summary>
        public bool CanEditTypeId(string typeId, out string reason)
        {
            int usageCount = UsageCountOf(typeId);
            if (usageCount == 0)
            {
                reason = string.Empty;
                return true;
            }
            reason = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.EventTypesTypeIdLockedReasonFormat),
                usageCount.ToString(CultureInfo.InvariantCulture));
            return false;
        }

        /// <summary>
        /// Mục menu "Xoá loại" — bật khi không còn đợt/luật (bảng 7.0: không hỏi, toast Hoàn tác), tắt kèm nhãn
        /// "Xoá loại (còn 3 đợt)" và lý do khi còn. Đếm ở đây khớp <c>LiveOpsConfirmationPolicy</c> (ô "Xoá loại" của bảng 7.0);
        /// lệnh xoá thật vẫn đi qua policy — test <c>CanDeleteType_MatchesConfirmationPolicy</c> khoá hai bên với nhau.
        /// </summary>
        public bool CanDeleteType(string typeId, out string menuLabel, out string reason)
        {
            int usageCount = UsageCountOf(typeId);
            if (usageCount == 0)
            {
                menuLabel = LiveOpsHubStrings.EventTypesDeleteMenuItem;
                reason = string.Empty;
                return true;
            }
            string countText = usageCount.ToString(CultureInfo.InvariantCulture);
            menuLabel = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.EventTypesDeleteMenuItemInUseFormat), countText);
            reason = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.EventTypesDeleteInUseReasonFormat), countText);
            return false;
        }

        /// <summary>
        /// Câu Blocked dưới bảng cho loại chưa khai báo: "lucky-spin: có 2 đợt trong JSON đã dán nhưng chưa có loại — game sẽ bỏ"
        /// (V-17 — nguồn nháp và nguồn bản dán nói khác nhau vì hậu quả khác nhau). "" khi loại đã khai.
        /// </summary>
        public string UndeclaredText(string typeId)
        {
            if (typeId == null || !_rowByTypeId.TryGetValue(typeId, out EventTypeRow row) || row.IsDeclared) return string.Empty;
            string countText = row.UsageCount.ToString(CultureInfo.InvariantCulture);
            string key = row.IsFromRemoteSnapshotOnly
                ? nameof(LiveOpsHubStrings.EventTypesUnknownTypeInRemoteFormat)
                : nameof(LiveOpsHubStrings.EventTypesUnknownTypeInDraftFormat);
            return LiveOpsHubStringCatalog.Format(key, row.TypeId, countText);
        }

        /// <summary>
        /// Headline card tham chiếu: tiền tố "Kiểm lịch · " của vùng này ghép trước câu gốc của
        /// <see cref="LiveOpsFindingText"/> (V-22 CC-FT-2 (a) — nguồn câu không mang tiền tố). "" khi chưa kiểm hoặc loại đã khai.
        /// </summary>
        public string ReferenceHeadline(string typeId, LiveOpsHubFormat format)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            LiveEventCalendarFinding finding = ReferenceFinding(typeId);
            if (finding == null) return string.Empty;
            return LiveOpsHubStrings.EventTypesReferencePrefix + LiveOpsFindingText.Headline(finding, format);
        }

        /// <summary>Dòng id luật của card tham chiếu ("unknown-event-type · lucky-spin"); "" khi không có phát hiện.</summary>
        public string ReferenceRuleIdLine(string typeId)
        {
            LiveEventCalendarFinding finding = ReferenceFinding(typeId);
            return finding == null ? string.Empty : LiveOpsFindingText.RuleIdLine(finding);
        }

        /// <summary>Phát hiện luật 8 của loại này trong báo cáo gần nhất; null khi chưa kiểm hoặc loại đã khai.</summary>
        public LiveEventCalendarFinding ReferenceFinding(string typeId)
        {
            if (typeId == null) return null;
            return _unknownFindingByTypeId.TryGetValue(typeId, out LiveEventCalendarFinding finding) ? finding : null;
        }

        /// <summary>
        /// Card "Dùng ở đâu": "1 đợt cố định · đợt tới 3/10 00:00 UTC". Giờ và bộ định dạng là tham số (không nằm trong
        /// <see cref="Build"/>) vì câu này phụ thuộc đồng hồ, còn phần còn lại của model thì không — dựng lại model mỗi giây
        /// chỉ để đổi một câu là phí.
        /// </summary>
        public string UsageText(string typeId, DateTime nowUtc, LiveOpsHubFormat format)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (typeId == null || !_rowByTypeId.TryGetValue(typeId, out EventTypeRow row)) return string.Empty;
            if (row.UsageCount == 0) return LiveOpsHubStrings.EventTypesUsageNone;

            StringBuilder text = new StringBuilder();
            if (row.FixedEventCount > 0)
            {
                text.Append(LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.EventTypesUsageFixedCountFormat),
                    row.FixedEventCount.ToString(CultureInfo.InvariantCulture)));
            }
            if (row.HasRecurringRule)
            {
                if (text.Length > 0) text.Append(LiveOpsHubStrings.EventTypesPartSeparator);
                text.Append(LiveOpsHubStrings.EventTypesUsageRecurringRule);
            }
            if (TryFindNextFixedEvent(typeId, DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), out FixedLiveEventEntry nextEntry,
                out DateTime nextStartUtc))
            {
                if (text.Length > 0) text.Append(LiveOpsHubStrings.EventTypesPartSeparator);
                text.Append(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesUsageNextFormat,
                    format.ShortDateTimeUtc(nextStartUtc)));
            }
            return text.ToString();
        }

        /// <summary>Đợt cố định gần nhất chưa bắt đầu của loại — "Xem trên lịch" căn khung tới đúng đợt này.</summary>
        public bool TryFindNextFixedEvent(string typeId, DateTime nowUtc, out FixedLiveEventEntry nextEntry, out DateTime nextStartUtc)
        {
            nextEntry = null;
            nextStartUtc = default(DateTime);
            IReadOnlyList<FixedLiveEventEntry> fixedEvents = _document.FixedEvents;
            for (int index = 0; index < fixedEvents.Count; index++)
            {
                FixedLiveEventEntry entry = fixedEvents[index];
                if (!string.Equals(entry.EventType, typeId, StringComparison.Ordinal)) continue;
                if (!entry.TryGetStartUtc(out DateTime startUtc) || startUtc < nowUtc) continue;
                if (nextEntry == null || startUtc < nextStartUtc)
                {
                    nextEntry = entry;
                    nextStartUtc = startUtc;
                }
            }
            return nextEntry != null;
        }

        /// <summary>Id loại đã khai trong nháp — popover "Thêm loại" dùng để báo trùng id ngay khi gõ.</summary>
        public IReadOnlyList<string> DeclaredTypeIds()
        {
            List<string> typeIds = new List<string>();
            IReadOnlyList<LiveEventTypeDefinition> types = _document.EventTypes;
            for (int index = 0; index < types.Count; index++) typeIds.Add(types[index].TypeId);
            return typeIds;
        }

        /// <summary>Định nghĩa loại trong nháp; null khi loại chưa khai báo — lệnh sửa field dựng bản sao từ đây.</summary>
        public LiveEventTypeDefinition DeclaredType(string typeId)
        {
            return TryGetDeclaredType(typeId, out LiveEventTypeDefinition type) ? type : null;
        }

        /// <summary>"ô 6 steel" — dùng cho câu ghi đè và câu toast đổi màu.</summary>
        public static string SlotNamedText(int colorSlot)
        {
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesColorSlotNamedFormat,
                colorSlot.ToString(CultureInfo.InvariantCulture), SlotName(colorSlot));
        }

        /// <summary>"ô 7" — dùng khi câu đã nói tên loại nên không cần tên màu.</summary>
        public static string SlotNumberText(int colorSlot)
        {
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.EventTypesColorSlotNumberFormat,
                colorSlot.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Tên tám ô màu [FD §2.5]; "" khi số ô nằm ngoài [0, 7].</summary>
        public static string SlotName(int colorSlot)
        {
            switch (colorSlot)
            {
                case 0: return LiveOpsHubStrings.EventTypesColorSlotName0;
                case 1: return LiveOpsHubStrings.EventTypesColorSlotName1;
                case 2: return LiveOpsHubStrings.EventTypesColorSlotName2;
                case 3: return LiveOpsHubStrings.EventTypesColorSlotName3;
                case 4: return LiveOpsHubStrings.EventTypesColorSlotName4;
                case 5: return LiveOpsHubStrings.EventTypesColorSlotName5;
                case 6: return LiveOpsHubStrings.EventTypesColorSlotName6;
                case 7: return LiveOpsHubStrings.EventTypesColorSlotName7;
                default: return string.Empty;
            }
        }

        private SectionHealth BuildHealth()
        {
            // Loại chưa khai báo chỉ tính từ BÁO CÁO: đó là kết luận của luật 8, và rail đọc đúng nguồn đó — chưa kiểm thì rail
            // chưa biết, màn cũng không được tự kết luận thay. Trùng màu thì ngược lại: đọc thẳng nháp (không luật nào lo), nên
            // thấy được ngay cả khi chưa kiểm — đúng như LiveOpsHubFindingRouting.HealthFor làm.
            StringBuilder reason = new StringBuilder();
            bool hasUndeclared = false;
            if (_report != null)
            {
                foreach (LiveEventCalendarFinding finding in _report.Findings)
                {
                    if (finding.IsIgnored || !LiveOpsHubFindingRouting.BelongsTo(finding, LiveOpsHubHealthTarget.EventTypes)) continue;
                    hasUndeclared = true;
                    if (reason.Length > 0) reason.Append(LiveOpsHubStrings.EventTypesPartSeparator);
                    reason.Append(UndeclaredText(finding.TargetId));
                }
            }
            if (hasUndeclared) return SectionHealth.Blocked(string.Empty, reason.ToString());

            for (int index = 0; index < _rows.Count; index++)
            {
                string collision = ColorCollisionText(_rows[index].TypeId);
                if (collision.Length > 0) return SectionHealth.Warning(string.Empty, collision);
            }
            return SectionHealth.Ok();
        }

        private void CollectUnknownFindings()
        {
            if (_report == null) return;
            foreach (LiveEventCalendarFinding finding in _report.Findings)
            {
                if (!string.Equals(finding.RuleId, LiveEventCalendarRuleIds.UnknownEventType, StringComparison.Ordinal)) continue;
                if (finding.TargetId.Length == 0 || _unknownFindingByTypeId.ContainsKey(finding.TargetId)) continue;
                _unknownFindingByTypeId.Add(finding.TargetId, finding);
            }
        }

        private void BuildDeclaredRows()
        {
            IReadOnlyList<LiveEventTypeDefinition> types = _document.EventTypes;
            for (int index = 0; index < types.Count; index++)
            {
                LiveEventTypeDefinition type = types[index];
                CountUsages(_document, type.TypeId, out int fixedEventCount, out int recurringRuleCount);
                AddRow(new EventTypeRow(index, type.TypeId, type.DisplayName, type.ColorSlot, type.RequiresJoin,
                    type.DefaultConfigKey, fixedEventCount, recurringRuleCount, true, false));
            }
        }

        private void BuildUndeclaredRows(LiveOpsHubRemoteSnapshot remote)
        {
            // Nháp trước, bản dán sau: loại có ở CẢ HAI chỉ thành một hàng và hàng đó nói "trong lịch nháp" — đúng thứ tự ưu tiên
            // của luật 8 (V-17), vì loại đã có trong nháp thì hậu quả nặng hơn (chặn Copy) mới là câu phải hiện.
            AppendUndeclaredRows(_document, false);
            LiveEventCalendarDocument remoteDocument = remote != null ? remote.Document : null;
            if (remoteDocument != null) AppendUndeclaredRows(remoteDocument, true);
        }

        private void AppendUndeclaredRows(LiveEventCalendarDocument source, bool isFromRemoteSnapshotOnly)
        {
            IReadOnlyList<RecurringLiveEventRule> rules = source.RecurringRules;
            for (int index = 0; index < rules.Count; index++) TryAddUndeclaredRow(source, rules[index].EventType, isFromRemoteSnapshotOnly);
            IReadOnlyList<FixedLiveEventEntry> fixedEvents = source.FixedEvents;
            for (int index = 0; index < fixedEvents.Count; index++) TryAddUndeclaredRow(source, fixedEvents[index].EventType, isFromRemoteSnapshotOnly);
        }

        private void TryAddUndeclaredRow(LiveEventCalendarDocument source, string typeId, bool isFromRemoteSnapshotOnly)
        {
            // Loại sai quy tắc (rỗng, có '#', có xuống dòng) không thành hàng: mục đó đã bị bỏ và đã có phát hiện của luật 3/6 —
            // thêm một hàng "loại lạ ''" chỉ là nhiễu (cùng cách bỏ qua của luật 8).
            if (!IsUsableTypeId(typeId) || _rowByTypeId.ContainsKey(typeId)) return;

            CountUsages(source, typeId, out int fixedEventCount, out int recurringRuleCount);
            AddRow(new EventTypeRow(_rows.Count, typeId, LiveOpsHubStrings.EventTypesUndeclaredName, -1, false, string.Empty,
                fixedEventCount, recurringRuleCount, false, isFromRemoteSnapshotOnly));
        }

        /// <summary>Cùng bộ ký tự mà core cấm trong id (<c>LiveEventInstance.ValidateIdentifier</c>): rỗng, '#', xuống dòng.</summary>
        private static bool IsUsableTypeId(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return false;
            for (int index = 0; index < typeId.Length; index++)
            {
                char character = typeId[index];
                if (character == LiveEventInstance.ReservedSeparator || character == '\n' || character == '\r') return false;
            }
            return true;
        }

        private void AddRow(EventTypeRow row)
        {
            _rows.Add(row);
            _rowByTypeId[row.TypeId] = row;
        }

        private void CollectFreeSlots()
        {
            IReadOnlyList<int> free = LiveEventTypeColorSlots.FreeSlots(_document.EventTypes);
            for (int index = 0; index < free.Count; index++) _freeSlots.Add(free[index]);
        }

        /// <summary>
        /// Đếm mục dùng loại, ĐÚNG cách <c>LiveOpsConfirmationPolicy.DecideRemoveEventType</c> đếm: duyệt cả danh sách luật chứ
        /// không hỏi <c>TryGetRecurringRule</c> (tra bảng, tối đa ra một luật). Tài liệu dán vào CÓ THỂ có nhiều luật cùng loại
        /// — core ghi nhận và giữ luật đứng trước — nên hỏi bảng sẽ đếm thiếu, và con số đó đi thẳng vào chữ người dùng đọc
        /// ("Xoá loại (còn N đợt)", "đang có N đợt").
        /// </summary>
        private static void CountUsages(LiveEventCalendarDocument source, string typeId, out int fixedEventCount,
            out int recurringRuleCount)
        {
            fixedEventCount = 0;
            IReadOnlyList<FixedLiveEventEntry> fixedEvents = source.FixedEvents;
            for (int index = 0; index < fixedEvents.Count; index++)
            {
                if (string.Equals(fixedEvents[index].EventType, typeId, StringComparison.Ordinal)) fixedEventCount++;
            }
            recurringRuleCount = 0;
            IReadOnlyList<RecurringLiveEventRule> rules = source.RecurringRules;
            for (int index = 0; index < rules.Count; index++)
            {
                if (string.Equals(rules[index].EventType, typeId, StringComparison.Ordinal)) recurringRuleCount++;
            }
        }

        private int UsageCountOf(string typeId)
        {
            return typeId != null && _rowByTypeId.TryGetValue(typeId, out EventTypeRow row) ? row.UsageCount : 0;
        }

        private bool TryGetDeclaredType(string typeId, out LiveEventTypeDefinition type)
        {
            type = null;
            return typeId != null && _document.TryGetEventType(typeId, out type);
        }

        private string FirstDeclaredTypeInSlot(int colorSlot, string exceptTypeId)
        {
            IReadOnlyList<LiveEventTypeDefinition> types = _document.EventTypes;
            for (int index = 0; index < types.Count; index++)
            {
                LiveEventTypeDefinition candidate = types[index];
                if (string.Equals(candidate.TypeId, exceptTypeId, StringComparison.Ordinal)) continue;
                if (candidate.ColorSlot == colorSlot) return candidate.TypeId;
            }
            return string.Empty;
        }

        private static string JoinTypeIds(IReadOnlyList<LiveEventTypeDefinition> types)
        {
            StringBuilder text = new StringBuilder();
            for (int index = 0; index < types.Count; index++)
            {
                if (text.Length > 0) text.Append(LiveOpsHubStrings.EventTypesListSeparator);
                text.Append(types[index].TypeId);
            }
            return text.ToString();
        }

        private static string JoinSlotNumbers(IReadOnlyList<int> slots)
        {
            StringBuilder text = new StringBuilder();
            for (int index = 0; index < slots.Count; index++)
            {
                if (text.Length > 0) text.Append(LiveOpsHubStrings.EventTypesListSeparator);
                text.Append(slots[index].ToString(CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }
    }

    /// <summary>
    /// Một hàng bảng loại: giá trị tám cột [SD1 §2.1] đã thành chữ, cộng số liệu view cần để gắn class (hàng chưa khai báo mờ
    /// 0,7, swatch viền đứt, dấu Blocked). Bất biến — model dựng lại mỗi lần tài liệu đổi.
    /// </summary>
    internal sealed class EventTypeRow
    {
        /// <param name="recurringRuleCount">
        /// Số luật lặp khai loại này. Thường 0 hoặc 1, nhưng tài liệu dán vào có thể có nhiều luật cùng loại (core giữ luật
        /// đứng trước) — <see cref="UsageCount"/> phải đếm đủ để khớp <c>LiveOpsConfirmationPolicy</c>.
        /// </param>
        internal EventTypeRow(int laneIndex, string typeId, string displayName, int colorSlot, bool requiresJoin, string configKey,
            int fixedEventCount, int recurringRuleCount, bool isDeclared, bool isFromRemoteSnapshotOnly)
        {
            LaneIndex = laneIndex;
            TypeId = typeId;
            DisplayName = displayName ?? string.Empty;
            ColorSlot = colorSlot;
            RequiresJoin = requiresJoin;
            ConfigKey = configKey ?? string.Empty;
            FixedEventCount = fixedEventCount;
            RecurringRuleCount = recurringRuleCount;
            IsDeclared = isDeclared;
            IsFromRemoteSnapshotOnly = isFromRemoteSnapshotOnly;
        }

        /// <summary>Thứ tự làn trên Lịch — bảng sắp theo số này khi chưa bật sort cột (V-12).</summary>
        public int LaneIndex { get; }

        public string TypeId { get; }
        public string DisplayName { get; }

        /// <summary>-1 = chưa khai báo: swatch viền đứt, không màu (Hình 10b).</summary>
        public int ColorSlot { get; }

        public bool RequiresJoin { get; }
        public string ConfigKey { get; }
        public int FixedEventCount { get; }

        /// <summary>Số luật lặp khai loại này (0 hoặc 1 ở tài liệu lành; >1 khi bản dán trùng loại).</summary>
        public int RecurringRuleCount { get; }

        /// <summary>Cột "Nguồn", ô "Đợt" và card "Dùng ở đâu" chỉ cần CÓ hay KHÔNG — chữ của chúng không nêu số luật.</summary>
        public bool HasRecurringRule
        {
            get { return RecurringRuleCount > 0; }
        }

        public bool IsDeclared { get; }

        /// <summary>Loại chỉ xuất hiện trong JSON đang chạy đã dán (V-17) — câu Blocked nói "trong JSON đã dán".</summary>
        public bool IsFromRemoteSnapshotOnly { get; }

        /// <summary>Số mục dùng loại: đợt cố định + MỌI luật lặp cùng loại — cùng cách đếm của bảng 7.0 ô "Xoá loại".</summary>
        public int UsageCount
        {
            get { return FixedEventCount + RecurringRuleCount; }
        }

        /// <summary>Cột "Cách vào"; "" với hàng chưa khai báo (không có định nghĩa để đọc).</summary>
        public string EntryText
        {
            get
            {
                if (!IsDeclared) return string.Empty;
                return RequiresJoin ? LiveOpsHubStrings.EventTypesEntryRequiresJoin : LiveOpsHubStrings.EventTypesEntrySelfJoin;
            }
        }

        /// <summary>Cột "Nguồn": loại có luật lặp là "Luật lặp", còn lại "Cố định"; "" với hàng chưa khai báo.</summary>
        public string SourceText
        {
            get
            {
                if (!IsDeclared) return string.Empty;
                return HasRecurringRule ? LiveOpsHubStrings.EventTypesSourceRecurring : LiveOpsHubStrings.EventTypesSourceFixed;
            }
        }

        /// <summary>Cột "Đợt": số đợt cố định, hoặc chữ "luật" (opacity 0,7) khi loại sinh từ luật lặp.</summary>
        public string EventCountText
        {
            get
            {
                if (IsDeclared && HasRecurringRule) return LiveOpsHubStrings.EventTypesEventCountRule;
                return FixedEventCount.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Ô "Đợt" là chữ "luật" chứ không phải số — view hạ opacity theo cờ này, không so chuỗi.</summary>
        public bool IsEventCountRuleText
        {
            get { return IsDeclared && HasRecurringRule; }
        }
    }
}
