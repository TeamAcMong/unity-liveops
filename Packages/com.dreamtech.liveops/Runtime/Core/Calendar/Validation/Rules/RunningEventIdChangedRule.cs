using System;
using System.Collections.Generic;
using System.Text;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Luật 9 <c>running-event-id-changed</c>: một phát hiện cho mỗi đợt ĐANG CHẠY lúc now trong bản đã đăng (cả lần lặp lẫn đợt
    /// cố định, tính bằng biên dịch bản so) mà nháp không còn đợt cùng id + loại trong khung <c>[start, max(end, now) + 1 tick)</c>
    /// — đúng cách <c>SyncWindowWithCalendar</c> tìm lại đợt của bản ghi [CODE §5.5]. Không thấy thì người chơi dừng cộng điểm
    /// giữa đợt; thấy nhưng đã khép (end ≤ now) thì Refresh khép ngay — cả hai là Mất tiến độ (bảng 6.3).
    /// <para>Giá trị thô theo <see cref="LiveEventCalendarFinding.DetailCode"/> (Editor dựng câu, không parse lại):</para>
    /// <list type="bullet">
    /// <item><c>prefix-changed</c> (chỉ lần lặp): Found = id lần lặp đang chạy theo nháp ("pass-35", "" khi nháp không có lần lặp
    /// lúc now), Expected = id đang chạy ("weekly-pass-35").</item>
    /// <item><c>removed</c>: Found = "", Expected = id đang chạy — nháp xoá mục, hoặc giữ mục nhưng game bỏ nó.</item>
    /// <item><c>retyped</c> (chỉ đợt cố định): Found = loại mới, Expected = loại cũ.</item>
    /// <item><c>moved-out</c>, <c>ends-now</c>: Found = giờ mới "start · end", Expected = giờ đang chạy "start · end".</item>
    /// </list>
    /// <c>RelatedId</c> = id đang chạy; <c>RangeStart/End</c> = khung đang chạy theo bản so (Fingerprint đổi khi sang đợt khác, nên
    /// "để sau khi khép" không che nhầm lần lặp sau).
    /// </summary>
    internal sealed class RunningEventIdChangedRule : ILiveEventCalendarRule
    {
        public const string NoPublishedStampReasonCode = "no-published-stamp";
        public const string RevertRepairId = "revert";
        public const string DeferUntilEndRepairId = "defer-until-end";

        // Tiền tố băm cho EntryKey của đợt khôi phục — tất định để hai lần kiểm cùng tài liệu ra cùng lệnh sửa (báo cáo so được).
        private const string RestoredEntryKeySeed = "liveops-restore-running-event\n";
        private const int EntryKeyLength = 32;

        private static readonly UTF8Encoding Utf8WithoutByteOrderMark = new UTF8Encoding(false, false);

        public string RuleId => LiveEventCalendarRuleIds.RunningEventIdChanged;
        public LiveEventCalendarConsequence Consequence => LiveEventCalendarConsequence.ProgressLost;

        public LiveEventCalendarRuleResult Evaluate(LiveEventCalendarCheckContext context)
        {
            LiveEventCalendarDocument baseline = context.PublishedBaseline;
            // PD-9: không có bản so thì không có "đợt đang chạy trong bản đã đăng" — không áp dụng, không đếm là đã qua.
            if (baseline == null) return LiveEventCalendarRuleResult.NotApplicable(RuleId, NoPublishedStampReasonCode);

            DateTime nowUtc = context.NowUtc;
            // Bản so luôn biên dịch ở thứ tự xuất: bản so dựng từ JSON do bộ ghi sinh ra vốn đã theo thứ tự này, còn bản dựng
            // tay/khác nguồn phải sắp như lúc xuất thì mục thắng khi trùng id mới là mục game đang chạy.
            LiveEventCalendarCompilation baselineCompilation = LiveEventCalendarCompiler.CompileInExportOrder(baseline);

            var findings = new List<LiveEventCalendarFinding>();
            List<string> baselineTypes = CollectTypes(baseline);
            for (int typeIndex = 0; typeIndex < baselineTypes.Count; typeIndex++)
            {
                string eventType = baselineTypes[typeIndex];
                if (!context.IncludesEventType(eventType)) continue;

                IReadOnlyList<LiveEventInstance> running = baselineCompilation.Calendar.GetInstances(eventType, nowUtc, AddOneTickSaturating(nowUtc));
                for (int index = 0; index < running.Count; index++)
                {
                    LiveEventInstance runningInstance = running[index];
                    if (runningInstance.PhaseAt(nowUtc) != LiveEventPhase.Active) continue;

                    LiveEventCalendarFinding finding = IsFromRecurringRule(baselineCompilation, runningInstance, nowUtc)
                        ? EvaluateRecurring(context, baseline, baselineCompilation, runningInstance)
                        : EvaluateFixed(context, baseline, runningInstance);
                    if (finding != null) findings.Add(finding);
                }
            }

            return findings.Count > 0 ? LiveEventCalendarRuleResult.Found(RuleId, findings) : LiveEventCalendarRuleResult.Passed(RuleId);
        }

        // ----- Lần lặp -----

        private LiveEventCalendarFinding EvaluateRecurring(LiveEventCalendarCheckContext context, LiveEventCalendarDocument baseline,
            LiveEventCalendarCompilation baselineCompilation, LiveEventInstance runningInstance)
        {
            DateTime nowUtc = context.NowUtc;
            LiveEventCalendarCompilation draftCompilation = context.Compilation;
            string eventType = runningInstance.EventType;
            LiveEventInstance followed = FindFollowedInstance(draftCompilation, runningInstance, nowUtc);
            if (followed != null && followed.EndUtc > nowUtc) return null;

            // Luật được giữ của bản so (không phải luật đầu tiên cùng loại): luật đầu hỏng thì luật sau mới sinh ra lần lặp đang chạy.
            RecurringLiveEventRule baselineRule = FindKeptRecurringRule(baseline, baselineCompilation, eventType);
            RecurringLiveEventRule draftRule = FindKeptRecurringRule(context.Document, draftCompilation, eventType);

            string detailCode;
            string foundText;
            string expectedText = runningInstance.EventId;
            if (followed != null)
            {
                detailCode = LiveEventCalendarDetailCodes.RunningEndsNow;
                foundText = TimeRangeText(followed.StartUtc, followed.EndUtc);
                expectedText = TimeRangeText(runningInstance.StartUtc, runningInstance.EndUtc);
            }
            else if (draftRule == null)
            {
                detailCode = LiveEventCalendarDetailCodes.RunningRemoved;
                foundText = string.Empty;
            }
            else if (draftCompilation.TryFindInstanceById(eventType, runningInstance.EventId, nowUtc, out LiveEventInstance movedInstance))
            {
                // Cùng id vẫn còn nhưng khung ra ngoài vùng bản ghi tìm (vd dời neo) — người chơi vẫn dừng cộng điểm lúc này.
                detailCode = LiveEventCalendarDetailCodes.RunningMovedOutOfWindow;
                foundText = TimeRangeText(movedInstance.StartUtc, movedInstance.EndUtc);
                expectedText = TimeRangeText(runningInstance.StartUtc, runningInstance.EndUtc);
            }
            else
            {
                detailCode = LiveEventCalendarDetailCodes.RunningIdChanged;
                foundText = RunningIdInDraft(draftCompilation, eventType, nowUtc);
            }

            // Hoàn về: giữ configKey của nháp (không phải lý do mất tiến độ), đưa mọi field quyết định id + khung về bản so.
            // Luật đã bị xoá/bị bỏ thì khôi phục nguyên luật của bản so.
            RecurringLiveEventRule revertedRule = draftRule != null
                ? draftRule.WithAnchor(baselineRule.AnchorUtcText)
                    .WithIdPrefix(SamePrefix(draftRule, baselineRule) ? draftRule.IdPrefix : baselineRule.IdPrefix)
                    .WithPeriodHours(baselineRule.PeriodHours)
                    .WithActiveHours(baselineRule.ActiveHours)
                : baselineRule;

            return BuildFinding(detailCode, LiveEventCalendarTargetKind.RecurringRule, eventType, eventType, runningInstance, foundText,
                expectedText, new SetRecurringRuleEdit(revertedRule));
        }

        /// <summary>
        /// Tiền tố "" của nháp và tiền tố ghi rõ <c>type-</c> của bản so (JSON luôn ghi tiền tố hiệu lực) là cùng một tiền tố — giữ
        /// "" để hoàn về không làm asset đổi field vô cớ.
        /// </summary>
        private static bool SamePrefix(RecurringLiveEventRule draftRule, RecurringLiveEventRule baselineRule)
        {
            return string.Equals(draftRule.EffectiveIdPrefix, baselineRule.EffectiveIdPrefix, StringComparison.Ordinal);
        }

        /// <summary>Luật lặp được giữ của loại trong nháp — luật đứng trước bị bỏ thì không có lần lặp nào tới người chơi.</summary>
        private static RecurringLiveEventRule FindKeptRecurringRule(LiveEventCalendarDocument document, LiveEventCalendarCompilation compilation,
            string eventType)
        {
            IReadOnlyList<LiveEventCalendarEntryOutcome> entries = compilation.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = entries[index];
                if (outcome.Kind != LiveEventCalendarEntryKind.RecurringRule || !outcome.IsKept) continue;
                if (!string.Equals(outcome.EntryKey, eventType, StringComparison.Ordinal)) continue;
                return document.RecurringRules[outcome.SourceIndex];
            }
            return null;
        }

        private static string RunningIdInDraft(LiveEventCalendarCompilation draftCompilation, string eventType, DateTime nowUtc)
        {
            IReadOnlyList<RecurringLiveEventCalendar> calendars = draftCompilation.RecurringCalendars;
            for (int index = 0; index < calendars.Count; index++)
            {
                if (!string.Equals(calendars[index].EventType, eventType, StringComparison.Ordinal)) continue;
                IReadOnlyList<LiveEventInstance> occurrences = calendars[index].GetInstances(eventType, nowUtc, AddOneTickSaturating(nowUtc));
                return occurrences.Count > 0 ? occurrences[0].EventId : string.Empty;
            }
            return string.Empty;
        }

        // ----- Đợt cố định -----

        private LiveEventCalendarFinding EvaluateFixed(LiveEventCalendarCheckContext context, LiveEventCalendarDocument baseline,
            LiveEventInstance runningInstance)
        {
            DateTime nowUtc = context.NowUtc;
            LiveEventCalendarCompilation draftCompilation = context.Compilation;
            LiveEventInstance followed = FindFollowedInstance(draftCompilation, runningInstance, nowUtc);
            if (followed != null && followed.EndUtc > nowUtc) return null;

            FixedLiveEventEntry baselineEntry = FindFixedEntry(baseline.FixedEvents, runningInstance.EventId, runningInstance.EventType);
            string baselineStartText = baselineEntry != null ? baselineEntry.StartUtcText : LiveEventUtcText.Format(runningInstance.StartUtc);
            string baselineEndText = baselineEntry != null ? baselineEntry.EndUtcText : LiveEventUtcText.Format(runningInstance.EndUtc);
            string runningRangeText = TimeRangeText(runningInstance.StartUtc, runningInstance.EndUtc);

            IReadOnlyList<FixedLiveEventEntry> draftEvents = context.Document.FixedEvents;
            FixedLiveEventEntry sameTypeEntry = FindFixedEntry(draftEvents, runningInstance.EventId, runningInstance.EventType);
            FixedLiveEventEntry anyTypeEntry = sameTypeEntry ?? FindFixedEntry(draftEvents, runningInstance.EventId, null);

            string detailCode;
            string foundText;
            string expectedText;
            LiveEventCalendarEdit revertEdit;
            if (followed != null)
            {
                detailCode = LiveEventCalendarDetailCodes.RunningEndsNow;
                foundText = TimeRangeText(followed.StartUtc, followed.EndUtc);
                expectedText = runningRangeText;
                revertEdit = RevertTimes(sameTypeEntry, baselineStartText, baselineEndText, runningInstance, context.Document, baselineEntry);
            }
            else if (sameTypeEntry != null && IsKept(draftCompilation, sameTypeEntry))
            {
                detailCode = LiveEventCalendarDetailCodes.RunningMovedOutOfWindow;
                foundText = sameTypeEntry.StartUtcText + LiveEventCalendarFindingBuilder.ValueSeparator + sameTypeEntry.EndUtcText;
                expectedText = runningRangeText;
                revertEdit = new ReplaceFixedEventEdit(sameTypeEntry.WithTimes(baselineStartText, baselineEndText));
            }
            else if (sameTypeEntry == null && anyTypeEntry != null)
            {
                detailCode = LiveEventCalendarDetailCodes.RunningRetyped;
                foundText = anyTypeEntry.EventType;
                expectedText = runningInstance.EventType;
                revertEdit = new ReplaceFixedEventEdit(anyTypeEntry.WithEventType(runningInstance.EventType));
            }
            else
            {
                // Không còn mục cùng id, hoặc còn nhưng game bỏ (giờ hỏng, bị chồng/che): với bản ghi đều là đợt biến mất.
                detailCode = LiveEventCalendarDetailCodes.RunningRemoved;
                foundText = string.Empty;
                expectedText = runningInstance.EventId;
                revertEdit = RevertTimes(sameTypeEntry, baselineStartText, baselineEndText, runningInstance, context.Document, baselineEntry);
            }

            string targetEntryKey = anyTypeEntry != null ? anyTypeEntry.EntryKey : string.Empty;
            return BuildFinding(detailCode, LiveEventCalendarTargetKind.FixedEvent, runningInstance.EventId, targetEntryKey, runningInstance,
                foundText, expectedText, revertEdit);
        }

        /// <summary>Đưa giờ của mục nháp về giờ bản so; nháp không còn mục cùng id + loại thì thêm lại đúng đợt của bản so.</summary>
        private static LiveEventCalendarEdit RevertTimes(FixedLiveEventEntry draftEntry, string baselineStartText, string baselineEndText,
            LiveEventInstance runningInstance, LiveEventCalendarDocument draft, FixedLiveEventEntry baselineEntry)
        {
            if (draftEntry != null) return new ReplaceFixedEventEdit(draftEntry.WithTimes(baselineStartText, baselineEndText));

            // configKey như game đang chạy (bản so ghi configKey hiệu lực), để bản ghi không đổi khoá thưởng khi khôi phục.
            string configKey = baselineEntry != null ? baselineEntry.ConfigKey : runningInstance.ConfigKey;
            var restored = new FixedLiveEventEntry(RestoredEntryKey(draft, runningInstance), runningInstance.EventId, runningInstance.EventType,
                baselineStartText, baselineEndText, configKey);
            return new AddFixedEventEdit(restored);
        }

        private static string RestoredEntryKey(LiveEventCalendarDocument draft, LiveEventInstance runningInstance)
        {
            byte[] seed = Utf8WithoutByteOrderMark.GetBytes(RestoredEntryKeySeed + runningInstance.EventType + "\n" + runningInstance.EventId);
            string entryKey = LiveEventCalendarSha256.ComputeHex(seed).Substring(0, EntryKeyLength);
            // Khoá băm đã có trong nháp (đã khôi phục một lần rồi đổi id tiếp): khoá mới ngẫu nhiên, không ghi đè mục khác.
            return draft.TryGetFixedEvent(entryKey, out _) ? FixedLiveEventEntry.CreateEntryKey() : entryKey;
        }

        private static bool IsKept(LiveEventCalendarCompilation compilation, FixedLiveEventEntry entry)
        {
            return compilation.TryGetFixedOutcome(entry.EntryKey, out LiveEventCalendarEntryOutcome outcome) && outcome.IsKept;
        }

        /// <summary>Mục đầu tiên (thứ tự tài liệu) mang id; <paramref name="eventType"/> null = mọi loại.</summary>
        private static FixedLiveEventEntry FindFixedEntry(IReadOnlyList<FixedLiveEventEntry> entries, string eventId, string eventType)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                FixedLiveEventEntry entry = entries[index];
                if (!string.Equals(entry.EventId, eventId, StringComparison.Ordinal)) continue;
                if (eventType != null && !string.Equals(entry.EventType, eventType, StringComparison.Ordinal)) continue;
                return entry;
            }
            return null;
        }

        // ----- Dùng chung -----

        /// <summary>
        /// Hai lựa chọn "Quyết định…" (6.1): hoàn về bản đã đăng, hoặc hoàn về + ghi chú hẹn giờ tới lúc đợt khép để làm lại thay đổi
        /// khi không còn ai đang chơi dở. Không tách thành "giữ thay đổi": giữ là không làm gì — Editor có sẵn "Bỏ qua".
        /// </summary>
        private LiveEventCalendarFinding BuildFinding(string detailCode, LiveEventCalendarTargetKind targetKind, string targetId,
            string targetEntryKey, LiveEventInstance runningInstance, string foundText, string expectedText, LiveEventCalendarEdit revertEdit)
        {
            string runningStartText = LiveEventUtcText.Format(runningInstance.StartUtc);
            string runningEndText = LiveEventUtcText.Format(runningInstance.EndUtc);
            var reminder = new IgnoredCalendarWarning(RuleId, targetId, runningStartText, runningEndText, string.Empty, runningEndText);

            var repairs = new[]
            {
                new LiveEventCalendarRepair(RevertRepairId, LiveEventCalendarRepairKind.Decision, revertEdit, foundText, expectedText,
                    runningInstance.StartUtc, runningInstance.EndUtc),
                new LiveEventCalendarRepair(DeferUntilEndRepairId, LiveEventCalendarRepairKind.Decision,
                    new CompositeCalendarEdit(new[] { revertEdit, new AddIgnoredWarningEdit(reminder) }), foundText, expectedText,
                    runningInstance.StartUtc, runningInstance.EndUtc),
            };

            return new LiveEventCalendarFindingBuilder(RuleId, detailCode, Consequence, targetKind, targetId)
                .WithTargetEntryKey(targetEntryKey)
                .WithRelatedId(runningInstance.EventId)
                .WithTexts(foundText, expectedText)
                .WithRange(runningInstance.StartUtc, runningInstance.EndUtc)
                .WithAnchor(runningInstance.StartUtc)
                .WithRepairs(LiveEventCalendarRepairKind.Decision, repairs)
                .Build();
        }

        /// <summary>
        /// Lần lặp khi luật lặp được giữ của loại sinh ra đúng id này lúc now. Lịch ghép xếp luật lặp trước đợt cố định và bỏ đợt
        /// cố định chồng/trùng lần lặp, nên đợt đang chạy của loại có luật lặp chỉ là đợt cố định khi luật lặp không có lần chạy lúc đó.
        /// </summary>
        private static bool IsFromRecurringRule(LiveEventCalendarCompilation compilation, LiveEventInstance runningInstance, DateTime nowUtc)
        {
            IReadOnlyList<RecurringLiveEventCalendar> calendars = compilation.RecurringCalendars;
            for (int index = 0; index < calendars.Count; index++)
            {
                if (!string.Equals(calendars[index].EventType, runningInstance.EventType, StringComparison.Ordinal)) continue;
                IReadOnlyList<LiveEventInstance> occurrences = calendars[index].GetInstances(runningInstance.EventType, nowUtc, AddOneTickSaturating(nowUtc));
                for (int occurrenceIndex = 0; occurrenceIndex < occurrences.Count; occurrenceIndex++)
                {
                    if (string.Equals(occurrences[occurrenceIndex].EventId, runningInstance.EventId, StringComparison.Ordinal)) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Mô phỏng <c>SyncWindowWithCalendar</c>: tìm cùng id + loại trong <c>[start cũ, max(end cũ, now) + 1 tick)</c> trên lịch ghép
        /// của nháp — truy vấn chia khung để luật lặp hằng giờ không bị cắt ở 512 đợt.
        /// </summary>
        private static LiveEventInstance FindFollowedInstance(LiveEventCalendarCompilation compilation, LiveEventInstance runningInstance, DateTime nowUtc)
        {
            DateTime searchEndUtc = AddOneTickSaturating(runningInstance.EndUtc > nowUtc ? runningInstance.EndUtc : nowUtc);
            IReadOnlyList<LiveEventInstance> candidates = compilation.GetInstancesInRange(runningInstance.EventType, runningInstance.StartUtc, searchEndUtc);
            for (int index = 0; index < candidates.Count; index++)
            {
                if (string.Equals(candidates[index].EventId, runningInstance.EventId, StringComparison.Ordinal)) return candidates[index];
            }
            return null;
        }

        /// <summary>Loại có mục trong bản so theo thứ tự gặp (luật lặp rồi đợt) — thứ tự phát hiện tất định giữa hai lần kiểm.</summary>
        private static List<string> CollectTypes(LiveEventCalendarDocument document)
        {
            var types = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < document.RecurringRules.Count; index++)
            {
                if (seen.Add(document.RecurringRules[index].EventType)) types.Add(document.RecurringRules[index].EventType);
            }
            for (int index = 0; index < document.FixedEvents.Count; index++)
            {
                if (seen.Add(document.FixedEvents[index].EventType)) types.Add(document.FixedEvents[index].EventType);
            }
            return types;
        }

        private static string TimeRangeText(DateTime startUtc, DateTime endUtc)
        {
            return LiveEventUtcText.Format(startUtc) + LiveEventCalendarFindingBuilder.ValueSeparator + LiveEventUtcText.Format(endUtc);
        }

        private static DateTime AddOneTickSaturating(DateTime value)
        {
            return value.Ticks < DateTime.MaxValue.Ticks ? value.AddTicks(1) : value;
        }
    }
}
