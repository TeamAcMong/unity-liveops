using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// So hai tài liệu lịch và xếp mỗi mục khác nhau theo HẬU QUẢ với người chơi (bảng 6.3): Bị bỏ → Mất tiến độ → Nên xem →
    /// An toàn. Hậu quả không suy từ "field nào đổi" mà mô phỏng đúng cách <c>LiveOpsSystem.SyncWindowWithCalendar</c> tìm
    /// lại bản ghi đang chạy (theo id + loại đã lưu, trong khung <c>[start cũ, max(end cũ, now)]</c>) trên lịch đã biên dịch
    /// của nháp — vì cùng một field (vd <c>endUtc</c>) có thể an toàn hoặc làm mất tiến độ tuỳ đợt đang chạy hay chưa.
    /// Không ném: tài liệu null coi như rỗng, mục hỏng đi qua bộ biên dịch (không bao giờ ném).
    /// </summary>
    public static class LiveEventCalendarDiff
    {
        private const string FieldId = "id";
        private const string FieldType = "type";
        private const string FieldStartUtc = "startUtc";
        private const string FieldEndUtc = "endUtc";
        private const string FieldConfigKey = "configKey";
        private const string FieldAnchorUtc = "anchorUtc";
        private const string FieldIdPrefix = "idPrefix";
        private const string FieldPeriodHours = "periodHours";
        private const string FieldActiveHours = "activeHours";
        private const string FieldDisplayName = "displayName";
        private const string FieldColorSlot = "colorSlot";
        private const string FieldRequiresJoin = "requiresJoin";
        private const string FieldDefaultConfigKey = "defaultConfigKey";
        private const string FieldOrder = "order";

        /// <summary>
        /// (V-3) So với bản đã đăng / bản trên đĩa / bản remote. Danh tính: đợt cố định theo id, luật lặp theo loại. CHỈ so
        /// thứ JSON mang — đợt cố định + luật lặp, configKey HIỆU LỰC và tiền tố HIỆU LỰC ở cả hai phía; không so định nghĩa
        /// loại, RemoteConfigKey, dấu đã đăng, cảnh báo đã bỏ qua. Vì sao: bản so dựng từ SnapshotJson (không có loại), còn
        /// loại tới người chơi qua build chứ không qua remote config — báo "loại khác bản đã đăng" là nói sai. Đổi
        /// defaultConfigKey của loại vẫn hiện đúng chỗ: field "configKey" đổi trên từng đợt/luật không tự khai.
        /// </summary>
        public static LiveEventCalendarDiffResult Compare(LiveEventCalendarDocument baseline, LiveEventCalendarDocument draft, DateTime nowUtc)
        {
            return Run(baseline, draft, nowUtc, compareByEntryKey: false);
        }

        /// <summary>
        /// "Chưa lưu" (PD-22): cùng bảng hậu quả, nhưng đợt cố định theo EntryKey để đổi id là MỘT thay đổi (field "id"),
        /// không phải xoá + thêm; CÓ so định nghĩa loại theo TypeId (displayName/colorSlot/requiresJoin/defaultConfigKey +
        /// "order" khi thứ tự làn đổi — V-12), vì cả hai phía đều là tài liệu đầy đủ của cùng một asset.
        /// </summary>
        public static LiveEventCalendarDiffResult CompareByEntryKey(LiveEventCalendarDocument saved, LiveEventCalendarDocument current, DateTime nowUtc)
        {
            return Run(saved, current, nowUtc, compareByEntryKey: true);
        }

        private static LiveEventCalendarDiffResult Run(LiveEventCalendarDocument before, LiveEventCalendarDocument after, DateTime nowUtc,
            bool compareByEntryKey)
        {
            var beforeSide = new ComparisonSide(before ?? LiveEventCalendarDocument.Empty, nowUtc);
            var afterSide = new ComparisonSide(after ?? LiveEventCalendarDocument.Empty, nowUtc);
            var pending = new List<PendingChange>();
            int keptCount = 0;

            keptCount += CompareFixedEvents(beforeSide, afterSide, nowUtc, compareByEntryKey, pending);
            keptCount += CompareRecurringRules(beforeSide, afterSide, nowUtc, pending);
            if (compareByEntryKey) keptCount += CompareEventTypes(beforeSide, afterSide, pending);

            // List<T>.Sort không ổn định — khoá hoà cuối là thứ tự xử lý (thứ tự xuất của nháp, rồi mục đã xoá), để hai lần
            // so cùng dữ liệu luôn ra cùng thứ tự hàng.
            pending.Sort(ComparePending);
            var changes = new List<LiveEventCalendarChange>(pending.Count);
            for (int index = 0; index < pending.Count; index++) changes.Add(pending[index].Change);
            return new LiveEventCalendarDiffResult(changes, keptCount);
        }

        // ----- Đợt cố định -----

        private static int CompareFixedEvents(ComparisonSide beforeSide, ComparisonSide afterSide, DateTime nowUtc, bool compareByEntryKey,
            List<PendingChange> pending)
        {
            Func<FixedLiveEventEntry, string> identityOf = compareByEntryKey
                ? (Func<FixedLiveEventEntry, string>)(entry => entry.EntryKey)
                : entry => entry.EventId;
            List<ItemPair<FixedLiveEventEntry>> pairs = PairItems(beforeSide.ExportOrderedFixedEvents, afterSide.ExportOrderedFixedEvents, identityOf);

            int keptCount = 0;
            for (int index = 0; index < pairs.Count; index++)
            {
                FixedLiveEventEntry beforeEntry = pairs[index].Before;
                FixedLiveEventEntry afterEntry = pairs[index].After;

                List<LiveEventCalendarFieldChange> fields = beforeEntry != null && afterEntry != null
                    ? FixedFieldChanges(beforeSide.Document, beforeEntry, afterSide.Document, afterEntry)
                    : new List<LiveEventCalendarFieldChange>();
                if (beforeEntry != null && afterEntry != null && fields.Count == 0)
                {
                    keptCount++;
                    continue;
                }

                LiveEventCalendarChangeKind kind = afterEntry == null ? LiveEventCalendarChangeKind.Removed
                    : beforeEntry == null ? LiveEventCalendarChangeKind.Added : LiveEventCalendarChangeKind.Changed;
                LiveEventCalendarChange change = ClassifyFixedEvent(beforeSide, afterSide, nowUtc, kind, beforeEntry, afterEntry, fields);

                FixedLiveEventEntry timeSource = afterEntry ?? beforeEntry;
                DateTime sortTime = timeSource.TryGetStartUtc(out DateTime startUtc) ? startUtc : DateTime.MaxValue;
                pending.Add(new PendingChange(change, sortTime, index));
            }
            return keptCount;
        }

        private static List<LiveEventCalendarFieldChange> FixedFieldChanges(LiveEventCalendarDocument beforeDocument, FixedLiveEventEntry beforeEntry,
            LiveEventCalendarDocument afterDocument, FixedLiveEventEntry afterEntry)
        {
            var fields = new List<LiveEventCalendarFieldChange>();
            AddIfDifferent(fields, FieldId, beforeEntry.EventId, afterEntry.EventId);
            AddIfDifferent(fields, FieldType, beforeEntry.EventType, afterEntry.EventType);
            AddIfDifferent(fields, FieldStartUtc, beforeEntry.StartUtcText, afterEntry.StartUtcText);
            AddIfDifferent(fields, FieldEndUtc, beforeEntry.EndUtcText, afterEntry.EndUtcText);
            // configKey HIỆU LỰC: JSON ghi khoá của loại cho đợt không tự khai, nên đổi mặc định của loại là đổi đợt này (V-3).
            AddIfDifferent(fields, FieldConfigKey, beforeDocument.EffectiveConfigKeyOf(beforeEntry), afterDocument.EffectiveConfigKeyOf(afterEntry));
            return fields;
        }

        private static LiveEventCalendarChange ClassifyFixedEvent(ComparisonSide beforeSide, ComparisonSide afterSide, DateTime nowUtc,
            LiveEventCalendarChangeKind kind, FixedLiveEventEntry beforeEntry, FixedLiveEventEntry afterEntry, List<LiveEventCalendarFieldChange> fields)
        {
            bool afterKept = afterEntry != null && afterSide.IsFixedEventKept(afterEntry);
            bool willBeDropped = afterEntry != null && !afterKept;

            LiveEventInstance runningBefore = beforeEntry != null ? beforeSide.FindRunningFixedInstance(beforeEntry) : null;
            string runningIdAfter = string.Empty;
            LiveEventInstance followed = null;
            if (runningBefore != null)
            {
                followed = afterSide.FindFollowedInstance(runningBefore);
                if (followed != null) runningIdAfter = followed.EventId;
                else if (afterKept && afterSide.IsRunningFixedEvent(afterEntry)) runningIdAfter = afterEntry.EventId;
            }

            LiveEventCalendarConsequence consequence = LiveEventCalendarConsequence.Safe;
            if (willBeDropped)
            {
                consequence = LiveEventCalendarConsequence.Dropped;
            }
            else
            {
                bool idChanged = HasField(fields, FieldId);
                bool typeChanged = HasField(fields, FieldType);
                bool configKeyChanged = HasField(fields, FieldConfigKey);
                FixedEventPhase beforePhase = beforeEntry != null ? beforeSide.PhaseOf(beforeEntry) : FixedEventPhase.NotInGame;

                if (runningBefore != null)
                {
                    consequence = Worst(consequence, ConsequenceOfFollowing(runningBefore, followed, nowUtc));
                }
                else if (beforePhase == FixedEventPhase.Upcoming)
                {
                    // Người chơi chưa có bản ghi, nhưng đợt đã được hứa trong bản so: mất đợt, đổi id/loại/khoá thưởng cần người xem.
                    if (afterEntry == null || idChanged || typeChanged || configKeyChanged) consequence = LiveEventCalendarConsequence.ShouldReview;
                }
                else if (beforePhase == FixedEventPhase.Ended && afterEntry != null)
                {
                    // Id đợt đã khép nằm trong RetiredEventIds: đổi sang id mới hoặc mở lại chính id đó cho tương lai đều khiến
                    // người đã chơi thấy hành vi khác người mới — xoá hay đổi giờ vẫn nằm trong quá khứ thì an toàn.
                    if (idChanged || !afterSide.HasEnded(afterEntry)) consequence = LiveEventCalendarConsequence.ShouldReview;
                }

                // Mục nháp dùng lại id của một đợt đã khép trong bản so: người đã chơi đợt cũ bị AlreadyFinished, không vào được.
                if (afterEntry != null && !afterSide.HasEnded(afterEntry) && beforeSide.IsEndedFixedEventId(afterEntry.EventId))
                {
                    consequence = Worst(consequence, LiveEventCalendarConsequence.ShouldReview);
                }
            }

            FixedLiveEventEntry identitySource = afterEntry ?? beforeEntry;
            ComparisonSide fingerprintSide = afterEntry != null ? afterSide : beforeSide;
            return new LiveEventCalendarChange(kind, LiveEventCalendarItemKind.FixedEvent, identitySource.EventId,
                afterEntry != null ? afterEntry.EntryKey : string.Empty, fields, consequence,
                runningBefore != null ? runningBefore.EventId : string.Empty, runningIdAfter,
                runningBefore != null ? runningBefore.EndUtc : (DateTime?)null, willBeDropped,
                FixedFingerprint(fingerprintSide.Document, identitySource));
        }

        private static string FixedFingerprint(LiveEventCalendarDocument document, FixedLiveEventEntry entry)
        {
            return entry.EventId + "\n" + entry.EventType + "\n" + entry.StartUtcText + "\n" + entry.EndUtcText + "\n" +
                   document.EffectiveConfigKeyOf(entry);
        }

        // ----- Luật lặp -----

        private static int CompareRecurringRules(ComparisonSide beforeSide, ComparisonSide afterSide, DateTime nowUtc, List<PendingChange> pending)
        {
            List<ItemPair<RecurringLiveEventRule>> pairs = PairItems(beforeSide.Document.RecurringRules, afterSide.Document.RecurringRules,
                rule => rule.EventType);

            int keptCount = 0;
            for (int index = 0; index < pairs.Count; index++)
            {
                RecurringLiveEventRule beforeRule = pairs[index].Before;
                RecurringLiveEventRule afterRule = pairs[index].After;

                List<LiveEventCalendarFieldChange> fields = beforeRule != null && afterRule != null
                    ? RecurringFieldChanges(beforeSide.Document, beforeRule, afterSide.Document, afterRule)
                    : new List<LiveEventCalendarFieldChange>();
                if (beforeRule != null && afterRule != null && fields.Count == 0)
                {
                    keptCount++;
                    continue;
                }

                LiveEventCalendarChangeKind kind = afterRule == null ? LiveEventCalendarChangeKind.Removed
                    : beforeRule == null ? LiveEventCalendarChangeKind.Added : LiveEventCalendarChangeKind.Changed;
                LiveEventCalendarChange change = ClassifyRecurringRule(beforeSide, afterSide, nowUtc, kind, beforeRule, afterRule, fields);

                RecurringLiveEventRule timeSource = afterRule ?? beforeRule;
                DateTime sortTime = timeSource.TryGetAnchorUtc(out DateTime anchorUtc) ? anchorUtc : DateTime.MaxValue;
                pending.Add(new PendingChange(change, sortTime, index));
            }
            return keptCount;
        }

        private static List<LiveEventCalendarFieldChange> RecurringFieldChanges(LiveEventCalendarDocument beforeDocument, RecurringLiveEventRule beforeRule,
            LiveEventCalendarDocument afterDocument, RecurringLiveEventRule afterRule)
        {
            var fields = new List<LiveEventCalendarFieldChange>();
            AddIfDifferent(fields, FieldAnchorUtc, beforeRule.AnchorUtcText, afterRule.AnchorUtcText);
            // Tiền tố HIỆU LỰC: "" và "weekly-pass-" cho cùng id đợt, JSON luôn ghi bản hiệu lực (mục 5.2 luật 6).
            AddIfDifferent(fields, FieldIdPrefix, beforeRule.EffectiveIdPrefix, afterRule.EffectiveIdPrefix);
            AddIfDifferent(fields, FieldPeriodHours, FormatInteger(beforeRule.PeriodHours), FormatInteger(afterRule.PeriodHours));
            AddIfDifferent(fields, FieldActiveHours, FormatInteger(beforeRule.ActiveHours), FormatInteger(afterRule.ActiveHours));
            AddIfDifferent(fields, FieldConfigKey, beforeDocument.EffectiveConfigKeyOf(beforeRule), afterDocument.EffectiveConfigKeyOf(afterRule));
            return fields;
        }

        private static LiveEventCalendarChange ClassifyRecurringRule(ComparisonSide beforeSide, ComparisonSide afterSide, DateTime nowUtc,
            LiveEventCalendarChangeKind kind, RecurringLiveEventRule beforeRule, RecurringLiveEventRule afterRule, List<LiveEventCalendarFieldChange> fields)
        {
            bool beforeKept = beforeRule != null && beforeSide.IsRecurringRuleKept(beforeRule);
            bool afterKept = afterRule != null && afterSide.IsRecurringRuleKept(afterRule);
            bool willBeDropped = afterRule != null && !afterKept;

            LiveEventInstance runningBefore = beforeKept ? beforeSide.FindRunningRecurringInstance(beforeRule.EventType) : null;
            string runningIdAfter = string.Empty;
            LiveEventInstance followed = null;
            if (runningBefore != null)
            {
                followed = afterSide.FindFollowedInstance(runningBefore);
                if (followed != null)
                {
                    runningIdAfter = followed.EventId;
                }
                else if (afterKept)
                {
                    // Bản ghi cũ mất đợt; câu hàng diff cần id người chơi sẽ thấy thay vào ("weekly-pass-35 → pass-35").
                    LiveEventInstance runningAfter = afterSide.FindRunningRecurringInstance(afterRule.EventType);
                    if (runningAfter != null) runningIdAfter = runningAfter.EventId;
                }
            }

            LiveEventCalendarConsequence consequence = LiveEventCalendarConsequence.Safe;
            if (willBeDropped)
            {
                consequence = LiveEventCalendarConsequence.Dropped;
            }
            else
            {
                // id = tiền tố + floor((t − neo) / chu kỳ): ba field này đổi id của lần lặp đang chạy và mọi lần sau.
                bool idShapeChanged = HasField(fields, FieldAnchorUtc) || HasField(fields, FieldIdPrefix) || HasField(fields, FieldPeriodHours);
                bool activeHoursChanged = HasField(fields, FieldActiveHours);
                bool configKeyChanged = HasField(fields, FieldConfigKey);

                if (runningBefore != null)
                {
                    consequence = Worst(consequence, ConsequenceOfFollowing(runningBefore, followed, nowUtc));
                    // Kể cả khi id đang chạy giữ được: đổi khung/khoá giữa đợt, hoặc id các lần sau lệch — cần người xem.
                    if (afterRule != null && (idShapeChanged || activeHoursChanged || configKeyChanged))
                    {
                        consequence = Worst(consequence, LiveEventCalendarConsequence.ShouldReview);
                    }
                }
                else if (beforeKept)
                {
                    // Không có lần lặp đang chạy nhưng lần sau đã được hứa: xoá luật hoặc đổi id/khoá của lần sắp tới.
                    if (afterRule == null || idShapeChanged || configKeyChanged) consequence = LiveEventCalendarConsequence.ShouldReview;
                }
            }

            RecurringLiveEventRule identitySource = afterRule ?? beforeRule;
            ComparisonSide fingerprintSide = afterRule != null ? afterSide : beforeSide;
            return new LiveEventCalendarChange(kind, LiveEventCalendarItemKind.RecurringRule, identitySource.EventType, string.Empty, fields,
                consequence, runningBefore != null ? runningBefore.EventId : string.Empty, runningIdAfter,
                runningBefore != null ? runningBefore.EndUtc : (DateTime?)null, willBeDropped,
                RecurringFingerprint(fingerprintSide.Document, identitySource));
        }

        private static string RecurringFingerprint(LiveEventCalendarDocument document, RecurringLiveEventRule rule)
        {
            return rule.EventType + "\n" + rule.AnchorUtcText + "\n" + rule.EffectiveIdPrefix + "\n" + FormatInteger(rule.PeriodHours) + "\n" +
                   FormatInteger(rule.ActiveHours) + "\n" + document.EffectiveConfigKeyOf(rule);
        }

        // ----- Định nghĩa loại (chỉ "chưa lưu") -----

        private static int CompareEventTypes(ComparisonSide beforeSide, ComparisonSide afterSide, List<PendingChange> pending)
        {
            IReadOnlyList<LiveEventTypeDefinition> beforeTypes = beforeSide.Document.EventTypes;
            IReadOnlyList<LiveEventTypeDefinition> afterTypes = afterSide.Document.EventTypes;
            List<ItemPair<LiveEventTypeDefinition>> pairs = PairItems(beforeTypes, afterTypes, type => type.TypeId);
            HashSet<string> movedTypeIds = FindMovedTypeIds(beforeTypes, afterTypes);

            int keptCount = 0;
            for (int index = 0; index < pairs.Count; index++)
            {
                LiveEventTypeDefinition beforeType = pairs[index].Before;
                LiveEventTypeDefinition afterType = pairs[index].After;

                var fields = new List<LiveEventCalendarFieldChange>();
                if (beforeType != null && afterType != null)
                {
                    AddIfDifferent(fields, FieldDisplayName, beforeType.DisplayName, afterType.DisplayName);
                    AddIfDifferent(fields, FieldColorSlot, FormatInteger(beforeType.ColorSlot), FormatInteger(afterType.ColorSlot));
                    AddIfDifferent(fields, FieldRequiresJoin, FormatBoolean(beforeType.RequiresJoin), FormatBoolean(afterType.RequiresJoin));
                    AddIfDifferent(fields, FieldDefaultConfigKey, beforeType.DefaultConfigKey, afterType.DefaultConfigKey);
                    if (movedTypeIds.Contains(afterType.TypeId))
                    {
                        // Vị trí 1-based như người dùng đếm làn trên Lịch.
                        fields.Add(new LiveEventCalendarFieldChange(FieldOrder, FormatInteger(IndexOfType(beforeTypes, beforeType.TypeId) + 1),
                            FormatInteger(IndexOfType(afterTypes, afterType.TypeId) + 1)));
                    }
                    if (fields.Count == 0)
                    {
                        keptCount++;
                        continue;
                    }
                }

                LiveEventCalendarChangeKind kind = afterType == null ? LiveEventCalendarChangeKind.Removed
                    : beforeType == null ? LiveEventCalendarChangeKind.Added : LiveEventCalendarChangeKind.Changed;
                // Loại đi theo build, không theo JSON: chỉ đổi cách vào (requiresJoin) làm người chơi gặp hành vi khác cần xem;
                // tên, màu, khoá mặc định (đã hiện trên từng đợt kế thừa) và thứ tự làn chỉ là cách hub hiển thị.
                LiveEventCalendarConsequence consequence = HasField(fields, FieldRequiresJoin)
                    ? LiveEventCalendarConsequence.ShouldReview
                    : LiveEventCalendarConsequence.Safe;

                LiveEventTypeDefinition identitySource = afterType ?? beforeType;
                var change = new LiveEventCalendarChange(kind, LiveEventCalendarItemKind.EventType, identitySource.TypeId, string.Empty, fields,
                    consequence, string.Empty, string.Empty, null, false, TypeFingerprint(identitySource));
                pending.Add(new PendingChange(change, DateTime.MinValue, index));
            }
            return keptCount;
        }

        /// <summary>
        /// Loại "đã dời làn" = loại chung hai phía KHÔNG nằm trong dãy con giữ thứ tự dài nhất. Vì sao: so chỉ số từng loại
        /// thì kéo một làn từ cuối lên đầu làm mọi làn khác đổi chỉ số — hộp "chưa lưu" sẽ báo 5 thay đổi cho một thao tác.
        /// Hai làn kề nhau đổi chỗ là ca mơ hồ (dời cái nào cũng đúng): hoà thì giữ loại có vị trí cũ nhỏ hơn.
        /// </summary>
        private static HashSet<string> FindMovedTypeIds(IReadOnlyList<LiveEventTypeDefinition> beforeTypes, IReadOnlyList<LiveEventTypeDefinition> afterTypes)
        {
            var beforeIndexById = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < beforeTypes.Count; index++)
            {
                if (!beforeIndexById.ContainsKey(beforeTypes[index].TypeId)) beforeIndexById.Add(beforeTypes[index].TypeId, index);
            }

            var commonIds = new List<string>();
            var beforePositions = new List<int>();
            for (int index = 0; index < afterTypes.Count; index++)
            {
                if (!beforeIndexById.TryGetValue(afterTypes[index].TypeId, out int beforeIndex)) continue;
                commonIds.Add(afterTypes[index].TypeId);
                beforePositions.Add(beforeIndex);
            }

            int count = beforePositions.Count;
            var lengths = new int[count];
            var predecessors = new int[count];
            for (int current = 0; current < count; current++)
            {
                lengths[current] = 1;
                predecessors[current] = -1;
                for (int previous = 0; previous < current; previous++)
                {
                    if (beforePositions[previous] >= beforePositions[current]) continue;
                    int candidateLength = lengths[previous] + 1;
                    bool longer = candidateLength > lengths[current];
                    bool tieWithSmallerPosition = candidateLength == lengths[current] && predecessors[current] >= 0 &&
                                                  beforePositions[previous] < beforePositions[predecessors[current]];
                    if (longer || tieWithSmallerPosition)
                    {
                        lengths[current] = candidateLength;
                        predecessors[current] = previous;
                    }
                }
            }

            int chainEnd = -1;
            for (int index = 0; index < count; index++)
            {
                if (chainEnd < 0 || lengths[index] > lengths[chainEnd] ||
                    (lengths[index] == lengths[chainEnd] && beforePositions[index] < beforePositions[chainEnd]))
                {
                    chainEnd = index;
                }
            }

            var inOrder = new bool[count];
            for (int index = chainEnd; index >= 0; index = predecessors[index]) inOrder[index] = true;

            var moved = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < count; index++)
            {
                if (!inOrder[index]) moved.Add(commonIds[index]);
            }
            return moved;
        }

        private static int IndexOfType(IReadOnlyList<LiveEventTypeDefinition> types, string typeId)
        {
            for (int index = 0; index < types.Count; index++)
            {
                if (string.Equals(types[index].TypeId, typeId, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        private static string TypeFingerprint(LiveEventTypeDefinition type)
        {
            return type.TypeId + "\n" + type.DisplayName + "\n" + FormatInteger(type.ColorSlot) + "\n" + FormatBoolean(type.RequiresJoin) + "\n" +
                   type.DefaultConfigKey;
        }

        // ----- Hậu quả với bản ghi đang chạy -----

        /// <summary>
        /// Mô phỏng <c>SyncWindowWithCalendar</c>: bản ghi tìm lại đợt theo id + loại đã lưu. Không thấy, hoặc thấy nhưng đã
        /// khép (end ≤ now) → người chơi dừng cộng điểm = Mất tiến độ. Thấy mà khung ngắn lại, bắt đầu dời ra sau now (treo
        /// điểm) hoặc đổi khoá thưởng → Nên xem. Kéo dài hay dời start vẫn trước now → An toàn.
        /// </summary>
        private static LiveEventCalendarConsequence ConsequenceOfFollowing(LiveEventInstance runningBefore, LiveEventInstance followed, DateTime nowUtc)
        {
            if (followed == null || followed.EndUtc <= nowUtc) return LiveEventCalendarConsequence.ProgressLost;

            if (followed.EndUtc < runningBefore.EndUtc || followed.StartUtc > nowUtc ||
                !string.Equals(followed.ConfigKey, runningBefore.ConfigKey, StringComparison.Ordinal))
            {
                return LiveEventCalendarConsequence.ShouldReview;
            }
            return LiveEventCalendarConsequence.Safe;
        }

        /// <summary>Enum xếp theo độ nặng: số nhỏ hơn = nặng hơn (Dropped = 0).</summary>
        private static LiveEventCalendarConsequence Worst(LiveEventCalendarConsequence first, LiveEventCalendarConsequence second)
        {
            return first <= second ? first : second;
        }

        // ----- Tiện ích chung -----

        private static List<ItemPair<TItem>> PairItems<TItem>(IReadOnlyList<TItem> beforeItems, IReadOnlyList<TItem> afterItems,
            Func<TItem, string> identityOf) where TItem : class
        {
            // Danh tính trùng (tài liệu dán vào có hai đợt cùng id, hai luật cùng loại) ghép theo thứ tự xuất hiện: mục thứ
            // N mang danh tính X của nháp ứng với mục thứ N mang X của bản so — không gộp, không ném.
            var beforeQueues = new Dictionary<string, Queue<int>>(StringComparer.Ordinal);
            for (int index = 0; index < beforeItems.Count; index++)
            {
                string identity = identityOf(beforeItems[index]);
                if (!beforeQueues.TryGetValue(identity, out Queue<int> queue))
                {
                    queue = new Queue<int>();
                    beforeQueues.Add(identity, queue);
                }
                queue.Enqueue(index);
            }

            var matchedBefore = new bool[beforeItems.Count];
            var pairs = new List<ItemPair<TItem>>(afterItems.Count + beforeItems.Count);
            for (int index = 0; index < afterItems.Count; index++)
            {
                TItem afterItem = afterItems[index];
                TItem beforeItem = null;
                if (beforeQueues.TryGetValue(identityOf(afterItem), out Queue<int> queue) && queue.Count > 0)
                {
                    int beforeIndex = queue.Dequeue();
                    matchedBefore[beforeIndex] = true;
                    beforeItem = beforeItems[beforeIndex];
                }
                pairs.Add(new ItemPair<TItem>(beforeItem, afterItem));
            }
            for (int index = 0; index < beforeItems.Count; index++)
            {
                if (!matchedBefore[index]) pairs.Add(new ItemPair<TItem>(beforeItems[index], null));
            }
            return pairs;
        }

        private static void AddIfDifferent(List<LiveEventCalendarFieldChange> fields, string fieldName, string beforeText, string afterText)
        {
            // So nguyên văn (Ordinal): JSON ghi đúng chuỗi này, nên hai cách viết cùng một giờ vẫn là byte khác ở bản xuất.
            if (!string.Equals(beforeText, afterText, StringComparison.Ordinal))
            {
                fields.Add(new LiveEventCalendarFieldChange(fieldName, beforeText, afterText));
            }
        }

        private static bool HasField(List<LiveEventCalendarFieldChange> fields, string fieldName)
        {
            for (int index = 0; index < fields.Count; index++)
            {
                if (string.Equals(fields[index].FieldName, fieldName, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static string FormatInteger(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string FormatBoolean(bool value) => value ? "true" : "false";

        private static int ComparePending(PendingChange left, PendingChange right)
        {
            int byConsequence = left.Change.Consequence.CompareTo(right.Change.Consequence);
            if (byConsequence != 0) return byConsequence;
            int byItemKind = left.Change.ItemKind.CompareTo(right.Change.ItemKind);
            if (byItemKind != 0) return byItemKind;
            int byTime = left.SortTime.CompareTo(right.SortTime);
            if (byTime != 0) return byTime;
            return left.SequenceIndex.CompareTo(right.SequenceIndex);
        }

        private static DateTime AddOneTickSaturating(DateTime value)
        {
            return value.Ticks < DateTime.MaxValue.Ticks ? value.AddTicks(1) : value;
        }

        private enum FixedEventPhase
        {
            NotInGame = 0,
            Upcoming = 1,
            Running = 2,
            Ended = 3,
        }

        private sealed class ItemPair<TItem> where TItem : class
        {
            public ItemPair(TItem before, TItem after)
            {
                Before = before;
                After = after;
            }

            public TItem Before { get; }
            public TItem After { get; }
        }

        private sealed class PendingChange
        {
            public PendingChange(LiveEventCalendarChange change, DateTime sortTime, int sequenceIndex)
            {
                Change = change;
                SortTime = sortTime;
                SequenceIndex = sequenceIndex;
            }

            public LiveEventCalendarChange Change { get; }
            public DateTime SortTime { get; }
            public int SequenceIndex { get; }
        }

        /// <summary>Một phía của phép so: tài liệu + lịch đã biên dịch theo thứ tự xuất (V-6) + kết quả từng mục tra nhanh.</summary>
        private sealed class ComparisonSide
        {
            private readonly DateTime _nowUtc;
            private readonly Dictionary<int, bool> _recurringKeptBySourceIndex = new Dictionary<int, bool>();
            private readonly HashSet<string> _endedFixedEventIds = new HashSet<string>(StringComparer.Ordinal);

            public ComparisonSide(LiveEventCalendarDocument document, DateTime nowUtc)
            {
                _nowUtc = nowUtc;
                Document = document;
                // Thứ tự xuất cho cả hai phía: bản so dựng từ JSON do bộ ghi sinh ra vốn đã theo thứ tự này (Apply không đổi
                // gì), còn bản trên đĩa / bản đã lưu là asset — phải sắp như lúc xuất thì "mục nào bị bỏ vì trùng/chồng" mới
                // khớp cái game thấy.
                LiveEventCalendarDocument exportOrdered = LiveEventCalendarExportOrder.Apply(document);
                ExportOrderedFixedEvents = exportOrdered.FixedEvents;
                Compilation = LiveEventCalendarCompiler.Compile(exportOrdered);

                for (int index = 0; index < Compilation.Entries.Count; index++)
                {
                    LiveEventCalendarEntryOutcome outcome = Compilation.Entries[index];
                    if (outcome.Kind == LiveEventCalendarEntryKind.RecurringRule) _recurringKeptBySourceIndex[outcome.SourceIndex] = outcome.IsKept;
                }

                for (int index = 0; index < ExportOrderedFixedEvents.Count; index++)
                {
                    FixedLiveEventEntry entry = ExportOrderedFixedEvents[index];
                    if (PhaseOf(entry) == FixedEventPhase.Ended) _endedFixedEventIds.Add(entry.EventId);
                }
            }

            public LiveEventCalendarDocument Document { get; }
            public IReadOnlyList<FixedLiveEventEntry> ExportOrderedFixedEvents { get; }
            public LiveEventCalendarCompilation Compilation { get; }

            public bool IsFixedEventKept(FixedLiveEventEntry entry)
            {
                return Compilation.TryGetFixedOutcome(entry.EntryKey, out LiveEventCalendarEntryOutcome outcome) && outcome.IsKept;
            }

            public bool IsRecurringRuleKept(RecurringLiveEventRule rule)
            {
                // Tra theo vị trí, không theo loại: luật đầu hỏng thì luật thứ hai cùng loại mới là luật được giữ.
                IReadOnlyList<RecurringLiveEventRule> rules = Document.RecurringRules;
                for (int index = 0; index < rules.Count; index++)
                {
                    if (ReferenceEquals(rules[index], rule)) return _recurringKeptBySourceIndex.TryGetValue(index, out bool kept) && kept;
                }
                return false;
            }

            /// <summary>Đợt chỉ "có trong game" khi bộ biên dịch giữ nó; mục bị bỏ ở phía này chưa từng tới người chơi.</summary>
            public FixedEventPhase PhaseOf(FixedLiveEventEntry entry)
            {
                if (!IsFixedEventKept(entry) || !entry.TryGetStartUtc(out DateTime startUtc) || !entry.TryGetEndUtc(out DateTime endUtc))
                {
                    return FixedEventPhase.NotInGame;
                }
                if (endUtc <= _nowUtc) return FixedEventPhase.Ended;
                return _nowUtc < startUtc ? FixedEventPhase.Upcoming : FixedEventPhase.Running;
            }

            public bool HasEnded(FixedLiveEventEntry entry)
            {
                return entry.TryGetEndUtc(out DateTime endUtc) && endUtc <= _nowUtc;
            }

            public bool IsRunningFixedEvent(FixedLiveEventEntry entry) => PhaseOf(entry) == FixedEventPhase.Running;

            public bool IsEndedFixedEventId(string eventId) => _endedFixedEventIds.Contains(eventId);

            /// <summary>Lấy đợt từ lịch ghép (không tự dựng) để configKey hiệu lực và luật che/trùng đúng như game đọc.</summary>
            public LiveEventInstance FindRunningFixedInstance(FixedLiveEventEntry entry)
            {
                if (PhaseOf(entry) != FixedEventPhase.Running) return null;
                return FindInstance(Compilation.Calendar.GetInstances(entry.EventType, _nowUtc, AddOneTickSaturating(_nowUtc)), entry.EventId);
            }

            /// <summary>Lần lặp đang chạy của luật được giữ cho loại này — chỉ khi lịch ghép cũng giữ nó (id không bị trùng).</summary>
            public LiveEventInstance FindRunningRecurringInstance(string eventType)
            {
                for (int index = 0; index < Compilation.RecurringCalendars.Count; index++)
                {
                    RecurringLiveEventCalendar calendar = Compilation.RecurringCalendars[index];
                    if (!string.Equals(calendar.EventType, eventType, StringComparison.Ordinal)) continue;

                    DateTime untilUtc = AddOneTickSaturating(_nowUtc);
                    IReadOnlyList<LiveEventInstance> occurrences = calendar.GetInstances(eventType, _nowUtc, untilUtc);
                    if (occurrences.Count == 0) return null;
                    return FindInstance(Compilation.Calendar.GetInstances(eventType, _nowUtc, untilUtc), occurrences[0].EventId);
                }
                return null;
            }

            /// <summary>
            /// Đợt mà bản ghi của <paramref name="runningBefore"/> sẽ theo trên lịch phía này — cùng id + loại, trong khung
            /// <c>[start cũ, max(end cũ, now) + 1 tick)</c> như <c>SyncWindowWithCalendar</c>. Dùng truy vấn chia khung của
            /// bản biên dịch để luật lặp hằng giờ không bị cắt ở 512 đợt.
            /// </summary>
            public LiveEventInstance FindFollowedInstance(LiveEventInstance runningBefore)
            {
                DateTime searchEndUtc = AddOneTickSaturating(runningBefore.EndUtc > _nowUtc ? runningBefore.EndUtc : _nowUtc);
                IReadOnlyList<LiveEventInstance> candidates = Compilation.GetInstancesInRange(runningBefore.EventType, runningBefore.StartUtc, searchEndUtc);
                return FindInstance(candidates, runningBefore.EventId);
            }

            private static LiveEventInstance FindInstance(IReadOnlyList<LiveEventInstance> instances, string eventId)
            {
                for (int index = 0; index < instances.Count; index++)
                {
                    if (string.Equals(instances[index].EventId, eventId, StringComparison.Ordinal)) return instances[index];
                }
                return null;
            }
        }
    }
}
